# AH-PILOT-18-B Closeout - FakePlc WorkStart Failure Transaction Harness

## 1. Summary

AH-PILOT-18-B에서는 `CAAutomationHub.PilotFlows.Xgt.Tests`의 existing in-process FakePlc WorkStart harness에 failure transaction 검증을 추가했다.

검증 흐름:

1. FakePlc `%DB10000` read block을 `WorkStartXgtPlcOperations`가 읽는다.
2. test-specific `StartSignalWordIndex = 83` layout override로 start signal과 LOT ID를 해석한다.
3. test-local fake DB query가 `NotFound`, `MultipleRows`, `Failed` 상태를 반환한다.
4. `WorkStartFlowService`가 `WorkStartStep.DbQuery` failure result를 반환한다.
5. `WorkStartErrorWritePolicy`가 error write expected 상태를 결정한다.
6. `WriteErrorCodeBestEffortAsync(...)`가 `%DB11410`에 error code를 쓴다.
7. FakePlc `LastErrorCode`와 `%DB11410` read-back으로 little-endian error code write를 검증한다.

영향:

- production code는 수정하지 않았다.
- Runtime / FlowDefinitions / PilotFlows core는 수정하지 않았다.
- PilotFlows.Xgt production source는 수정하지 않았다.
- FakePlc reference는 test project에만 유지했다.
- FakePlc map 파일은 수정하지 않았다.
- actual PLC / actual DB / FLOW.JSON / Flow Executor 범위로 확장하지 않았다.

## 2. 변경 파일 목록

- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/WorkStart/WorkStartXgtFakePlcIntegrationTests.cs`
  - DB not found / multiple rows / failed failure transaction test를 추가했다.
  - start signal inactive no-error-write test를 추가했다.
  - `RunDbFailureTransactionAsync(...)` helper와 `CreateService(...)` helper를 추가했다.
  - `CreateRuntime(bool startSignal = true)`로 start signal inactive fixture를 만들 수 있게 했다.
- `docs/harness/AH-PILOT-18-B.md`
  - AH-PILOT-18-B historical record를 추가했다.

## 3. FakePlc Failure Transaction 방식

AH-PILOT-18-A의 in-process FakePlc full transaction harness를 재사용했다.

test flow:

1. `CreateRuntime()`으로 `%DB10000`, `%DB11000`, `%DB11410`, `%DB11416`, `%DB11418` memory image를 구성한다.
2. `FakePlcScenarioInitializer.CreateMemoryImage(...)`로 LOT ID / start signal을 초기화한다.
3. `FakePlcRuntime`과 `FakePlcProtocolHandler`를 `InProcessFakePlcServer`에 연결한다.
4. `TcpTransport` + `XgtSession`으로 loopback FakePlc에 연결한다.
5. `WorkStartXgtPlcOperations(session, WorkStartXgtReadOptions.Default, WorkStartXgtWriteOptions.Default)`를 생성한다.
6. `WorkStartFlowService`에 real XGT operations와 test-local fake DB query를 주입한다.
7. `RunAsync()`를 실행한다.
8. result, fake query call, FakePlc records, FakePlc memory read-back을 검증한다.

## 4. DB Not Found Error Write 검증

추가 test:

- `RunAsync_WritesDbNotFoundErrorCode_WhenFakeDbReturnsNotFound`

검증:

- `result.Succeeded == false`
- `result.Step == WorkStartStep.DbQuery`
- `result.ErrorCode == WorkStartErrorCode.DbNotFound`
- `result.ErrorWriteExpected == true`
- `runtime.LastErrorCode == 2301`
- `%DB11410` read-back == `{ 0xFD, 0x08 }`
- payload write / ACK write는 호출되지 않음

## 5. DB Multiple Rows Error Write 검증

추가 test:

- `RunAsync_WritesDbMultipleRowsErrorCode_WhenFakeDbReturnsMultipleRows`

검증:

- `result.Succeeded == false`
- `result.Step == WorkStartStep.DbQuery`
- `result.ErrorCode == WorkStartErrorCode.DbMultipleRows`
- `result.ErrorWriteExpected == true`
- `runtime.LastErrorCode == 2302`
- `%DB11410` read-back == `{ 0xFE, 0x08 }`
- payload write / ACK write는 호출되지 않음

## 6. DB Failed Error Write 검증

추가 test:

- `RunAsync_WritesDbFailedErrorCode_WhenFakeDbReturnsFailed`

검증:

- `result.Succeeded == false`
- `result.Step == WorkStartStep.DbQuery`
- `result.ErrorCode == WorkStartErrorCode.DbFailed`
- `result.ErrorWriteExpected == true`
- `runtime.LastErrorCode == 2303`
- `%DB11410` read-back == `{ 0xFF, 0x08 }`
- payload write / ACK write는 호출되지 않음

## 7. Start Signal Inactive No-Error-Write 검증 여부

포함했다.

추가 test:

- `RunAsync_DoesNotWriteErrorCode_WhenStartSignalInactive`

검증:

- FakePlc scenario를 `StartSignal = false`로 준비한다.
- `result.Succeeded == false`
- `result.Step == WorkStartStep.StartSignal`
- `result.ErrorCode == WorkStartErrorCode.StartSignalInactive`
- `result.ErrorWriteExpected == false`
- fake DB query는 호출되지 않는다.
- `runtime.LastErrorCode == null`
- `%DB11410` read-back == `{ 0x00, 0x00 }`
- payload write / ACK write는 호출되지 않음

## 8. Error Code Read-Back 검증

사용한 evidence:

- `FakePlcRuntime.LastErrorCode`
- `FakePlcRuntime.ReadContinuous(FakePlcMemoryImage.Db11410, 2)`

검증한 little-endian encoding:

- `2301` decimal = `0x08FD` -> `{ 0xFD, 0x08 }`
- `2302` decimal = `0x08FE` -> `{ 0xFE, 0x08 }`
- `2303` decimal = `0x08FF` -> `{ 0xFF, 0x08 }`

Start signal inactive의 `1200`은 write expected false이므로 `%DB11410`에 쓰지 않고 `{ 0x00, 0x00 }` 유지로 검증했다.

## 9. 이식하지 않은 범위

이번 작업에서 제외했다.

- Runtime project 수정
- FlowDefinitions project 수정
- PilotFlows core 수정
- PilotFlows.Xgt production source 수정
- production project FakePlc reference 추가
- XgtChannelRunner reference 추가
- Microsoft.Data.SqlClient 추가
- actual DB query 구현
- actual PLC read/write 테스트
- FLOW.JSON 연결
- JSON parser / schema 구현
- Flow Executor 구현
- RuntimeSnapshot / ChannelPollingResult 참조
- FakePlc map 파일 수정
- WorkStartPilotService source copy
- payload build failure 2400
- bulk write failure 2501
- ACK write failure 2601
- read NAK / malformed / parse failure 1101 / 1102

## 10. 테스트 결과

Focused failure transaction tests:

```text
dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj --filter "FullyQualifiedName~RunAsync_WritesDbNotFoundErrorCode|FullyQualifiedName~RunAsync_WritesDbMultipleRowsErrorCode|FullyQualifiedName~RunAsync_WritesDbFailedErrorCode|FullyQualifiedName~RunAsync_DoesNotWriteErrorCode"
```

결과:

```text
failed 0, passed 4, skipped 0, total 4
```

Required tests:

```text
dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj
dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj
dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj
```

결과:

```text
CAAutomationHub.PilotFlows.Xgt.Tests:
  failed 0, passed 37, skipped 0, total 37

CAAutomationHub.PilotFlows.Tests:
  failed 0, passed 40, skipped 0, total 40

CAAutomationHub.Runtime.Tests:
  failed 0, passed 142, skipped 0, total 142
```

TDD note:

- 이번 작업은 production implementation 추가가 아니라 existing WorkStartFlowService / WorkStartXgtPlcOperations / FakePlc write support를 failure transaction harness로 검증한 test-only 단계다.
- 신규 production code는 작성하지 않았다.
- 새 failure transaction tests의 첫 focused 실행은 통과했다. 이는 AH-PILOT-16 / 17 / 18-A에서 service error policy, XGT error writer, FakePlc write evidence가 이미 준비되어 있었기 때문이다.

## 11. 빌드 결과

실행:

```text
dotnet build CAAutomationHub.sln
```

결과:

```text
Build succeeded.
0 warnings
0 errors
```

## 12. Boundary Scan 결과

실행:

```text
git diff --check
rg -n "XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src tests
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
rg -n "FakePlc" src tests -g "*.csproj"
rg -n "XgtChannelRunner" src tests -g "*.csproj"
rg -n "Microsoft.Data.SqlClient|SqlConnection" src tests -g "*.csproj" -g "*.cs"
rg -n "FakePlc" src\CAAutomationHub.PilotFlows.Xgt -g "*.cs" -g "*.csproj"
```

결과:

- `git diff --check`는 exit code `0`으로 통과했다. Git line-ending warning만 출력됐다.
- 전체 `src tests` boundary scan은 기존 Runtime / WPF contract와 tests의 `RuntimeSnapshot`, `ChannelPollingResult`, `Json` hit를 출력했다.
- 이번 변경 파일에는 금지 boundary hit가 없다.
- FakePlc reference는 `tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`에만 존재한다.
- `src\CAAutomationHub.PilotFlows.Xgt`에는 FakePlc hit가 없다.
- XgtChannelRunner project reference 없음.
- Microsoft.Data.SqlClient / SqlConnection hit 없음.

## 13. 다음 후보

- AH-PILOT-19 후보:
  - payload build failure 2400
  - bulk write failure 2501
  - ACK write failure 2601
  - read NAK / malformed / parse failure 1101 / 1102
  - deterministic FakePlc failure injection boundary review
- ACK 후 `ClearStartSignalOnAck`에 따른 `D5083` clear 확인은 별도 WorkStart-specific integration으로 분리 가능하다.

## 14. 실행한 명령

Precheck:

- `git log --oneline -8`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-18-A.md`
- `Get-Content docs\harness\AH-PILOT-17.md`
- `Get-Content docs\harness\AH-PILOT-16.md`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart\WorkStartXgtFakePlcIntegrationTests.cs`
- `Get-ChildItem src\CAAutomationHub.PilotFlows\WorkStart -Force`
- `Get-ChildItem src\CAAutomationHub.PilotFlows.Xgt\WorkStart -Force`
- `rg -n "LastErrorCode|ReadContinuous|WorkStartFlowService|FakeDb|DbNotFound|DbMultiple|DbFailed|StartSignal" tests\CAAutomationHub.PilotFlows.Xgt.Tests src\CAAutomationHub.PilotFlows`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowService.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowResult.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\IWorkStartDataQuery.cs`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtPlcOperations.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartDataQueryResult.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartDataQueryStatus.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartErrorCode.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartErrorWritePolicy.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartStep.cs`

Sibling repo read-only:

- `rg -n "class FakePlcRuntime|LastBulkWrite|LastAckValue|LastErrorCode|ReadContinuous" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcRuntime.cs`

Implementation / validation:

- `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj --filter "FullyQualifiedName~RunAsync_WritesDbNotFoundErrorCode|FullyQualifiedName~RunAsync_WritesDbMultipleRowsErrorCode|FullyQualifiedName~RunAsync_WritesDbFailedErrorCode|FullyQualifiedName~RunAsync_DoesNotWriteErrorCode"`
- `git diff -- tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart\WorkStartXgtFakePlcIntegrationTests.cs`
- `git status --short`
- `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
- `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
- `dotnet build CAAutomationHub.sln`
- `git diff --check`
- `rg -n "XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src tests`
- `rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"`
- `rg -n "FakePlc" src tests -g "*.csproj"`
- `rg -n "XgtChannelRunner" src tests -g "*.csproj"`
- `rg -n "Microsoft.Data.SqlClient|SqlConnection" src tests -g "*.csproj" -g "*.cs"`
- `rg -n "FakePlc" src\CAAutomationHub.PilotFlows.Xgt -g "*.cs" -g "*.csproj"`

## 15. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-18-B 목표인 FakePlc 기반 WorkStart failure transaction harness를 test-only 영역에 추가했다.
- DB not found / multiple rows / failed가 `WorkStartStep.DbQuery` failure로 반환됨을 검증했다.
- `WorkStartErrorWritePolicy`에 따라 error write expected true가 유지됨을 검증했다.
- `%DB11410` error code write를 `LastErrorCode`와 read-back bytes로 검증했다.
- start signal inactive는 error write expected false이고 `%DB11410`이 변경되지 않음을 검증했다.
- failure path에서 payload write와 ACK write가 발생하지 않음을 검증했다.
- Runtime / FlowDefinitions / PilotFlows core / PilotFlows.Xgt production source를 수정하지 않았다.
- FakePlc map을 수정하지 않았다.
- FakePlc reference는 test project에만 유지했다.
- XgtChannelRunner / DB / SqlClient / FLOW.JSON / JSON parser / FlowExecutor / RuntimeSnapshot / ChannelPollingResult 범위로 확장하지 않았다.
- 요구된 tests, build, diff check, boundary scan evidence를 남겼다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
