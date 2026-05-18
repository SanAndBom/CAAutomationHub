# AH-PILOT-20 Closeout - FakePlc Read NAK Failure Harness

## 1. Summary

AH-PILOT-20에서는 `CAAutomationHub.PilotFlows.Xgt.Tests`의 in-process production FakePlc harness에 unsupported read address 기반 read NAK integration test를 추가했다.

검증 흐름:

1. `WorkStartXgtReadOptions`를 test-specific으로 구성해 read start variable을 `%DB99990`으로 지정한다.
2. `WorkStartXgtPlcOperations`가 real `XgtSession`으로 FakePlc에 continuous read를 보낸다.
3. FakePlc `FakePlcMemoryImage.ReadContinuous(...)`가 등록되지 않은 byte offset을 validation failure로 판정한다.
4. `FakePlcProtocolHandler`가 read-side `InvalidOperationException`을 XGT read NAK로 낮춘다.
5. `WorkStartXgtPlcOperations.ReadWorkStartBlockAsync(...)`가 NAK를 `OperationFailed`로 매핑한다.
6. `WorkStartFlowService.RunAsync(...)`가 `WorkStartStep.GroupRead`, `WorkStartErrorCode.ReadFailed(1101)` failure를 반환한다.
7. `ReadFailed(1101)`은 error write 대상이 아니므로 `%DB11410` error code write가 발생하지 않는다.

영향:

- production code는 수정하지 않았다.
- Runtime / FlowDefinitions / PilotFlows core / PilotFlows.Xgt production source는 수정하지 않았다.
- FakePlc map 파일은 수정하지 않았다.
- `WorkStartXgtReadOptions.Default`와 `WorkStartReadBlockLayout.Default`는 수정하지 않았다.
- FakePlc reference는 기존 test project reference만 사용했다.
- actual PLC / actual DB / FLOW.JSON / JSON parser / Flow Executor 범위로 확장하지 않았다.

## 2. 변경 파일 목록

- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/WorkStart/WorkStartXgtFakePlcIntegrationTests.cs`
  - `RunAsync_ReturnsReadFailed_WhenFakePlcRejectsWorkStartReadAddress` service-level integration test를 추가했다.
  - `ReadWorkStartBlockAsync_ReturnsOperationFailed_WhenFakePlcRejectsReadAddress` adapter-level integration test를 추가했다.
  - `CreateService(...)`가 test-specific `WorkStartXgtReadOptions`를 받을 수 있게 optional parameter를 추가했다.
- `docs/harness/AH-PILOT-20.md`
  - AH-PILOT-20 historical record를 추가했다.

## 3. unsupported read address 선택 근거

선택 주소:

- `%DB99990`

근거:

- 현재 test runtime의 FakePlc memory range는 `%DB10000`, `%DB11000`, `%DB11410`, `%DB11416`, `%DB11418`로 구성된다.
- `%DB99990`은 XGT continuous read request builder가 만들 수 있는 byte-direct DB variable이다.
- `ReadWordCount = 90`이면 continuous byte length는 `180`으로 `XgtReadRequest.MaxContinuousByteLength` 범위 안에 있다.
- 따라서 local request builder 단계에서 실패하지 않고 FakePlc까지 요청이 도달한다.
- FakePlc runtime은 `%DB99990` absolute byte offset을 등록된 memory block range에서 찾지 못해 validation failure를 발생시킨다.
- production FakePlc protocol handler는 read request의 `InvalidOperationException`을 catch해 XGT read NAK를 반환한다.

## 4. FakePlc read NAK / OperationFailed 검증 방식

추가 test:

- `ReadWorkStartBlockAsync_ReturnsOperationFailed_WhenFakePlcRejectsReadAddress`

검증:

- in-process `FakePlcProtocolHandler`와 real `TcpTransport` / `XgtSession`을 사용한다.
- test-specific `WorkStartXgtReadOptions("%DB99990", 90)`을 사용한다.
- `ReadWorkStartBlockAsync()` 결과가 `WorkStartReadBlockOperationStatus.OperationFailed`임을 확인한다.
- `result.Data == null`임을 확인한다.

의미:

- FakePlc unsupported read validation failure가 XGT NAK로 내려오고, adapter boundary에서 WorkStart operation failure로 낮아짐을 검증했다.

## 5. WorkStartFlowService ReadFailed / 1101 검증

추가 test:

- `RunAsync_ReturnsReadFailed_WhenFakePlcRejectsWorkStartReadAddress`

검증:

- `result.Succeeded == false`
- `result.Step == WorkStartStep.GroupRead`
- `result.ErrorCode == WorkStartErrorCode.ReadFailed`
- `(int)result.ErrorCode == 1101`
- `result.SelectedLotId == null`
- test-local fake DB query가 호출되지 않음

의미:

- WorkStart service boundary에서 read operation failure가 DB / payload / ACK 단계로 진행되지 않고 `GroupRead / ReadFailed / 1101`로 종료됨을 검증했다.

## 6. error write 미수행 검증

검증:

- `result.ErrorWriteExpected == false`
- `runtime.LastErrorCode == null`
- `%DB11410` read-back == `{ 0x00, 0x00 }`
- `runtime.LastBulkWrite == null`
- `runtime.LastAckValue == null`
- `%DB11000` read-back은 zero payload 유지
- `%DB11416` read-back == `{ 0x00, 0x00 }`
- fake DB query 호출 없음

의미:

- `ReadFailed(1101)`은 error write 미수행 정책을 유지한다.
- DB query, payload write, ACK write, error write가 모두 차단됨을 FakePlc runtime evidence로 확인했다.

## 7. 이식하지 않은 범위

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
- WorkStartXgtReadOptions.Default 수정
- WorkStartReadBlockLayout.Default 수정
- malformed / timeout / no response / forced close injection
- address-specific write NAK injection

## 8. 테스트 결과

Focused read NAK tests:

```text
dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj --filter "FullyQualifiedName~RunAsync_ReturnsReadFailed_WhenFakePlcRejectsWorkStartReadAddress|FullyQualifiedName~ReadWorkStartBlockAsync_ReturnsOperationFailed_WhenFakePlcRejectsReadAddress"
```

결과:

```text
failed 0, passed 2, skipped 0, total 2
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
  failed 0, passed 39, skipped 0, total 39

CAAutomationHub.PilotFlows.Tests:
  failed 0, passed 40, skipped 0, total 40

CAAutomationHub.Runtime.Tests:
  failed 0, passed 142, skipped 0, total 142
```

TDD note:

- 이번 작업은 production implementation 추가가 아니라 existing WorkStart / XGT / FakePlc behavior를 integration harness로 고정한 test-only 단계다.
- 신규 production code는 작성하지 않았다.
- focused read NAK tests의 첫 실행은 통과했다. 이는 AH-PILOT-19에서 확인한 production FakePlc read NAK path와 기존 WorkStart failure mapping이 이미 준비되어 있었기 때문이다.

## 9. 빌드 결과

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

## 10. boundary scan 결과

실행:

```text
git diff --check
rg -n "XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src tests
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
rg -n "FakePlc" src tests -g "*.csproj"
rg -n "XgtChannelRunner" src tests -g "*.csproj"
rg -n "Microsoft.Data.SqlClient|SqlConnection" src tests -g "*.csproj" -g "*.cs"
```

결과:

- `git diff --check`는 exit code `0`으로 통과했다. Git line-ending warning만 출력됐다.
- 전체 `src tests` boundary scan은 기존 Runtime / WPF contract와 tests의 `RuntimeSnapshot`, `ChannelPollingResult`, `Json` hit를 출력했다.
- 이번 변경 파일에는 금지 boundary hit가 없다.
- FakePlc project reference는 기존 `tests\CAAutomationHub.PilotFlows.Xgt.Tests` test project에만 있다.
- Runtime / FlowDefinitions / PilotFlows core에는 FakePlc reference가 없다.
- PilotFlows.Xgt production project에는 FakePlc reference가 없다.
- XgtChannelRunner project reference 없음.
- Microsoft.Data.SqlClient / SqlConnection hit 없음.

## 11. 다음 후보

- production FakePlc failure injection enhancement:
  - malformed response
  - timeout / no response
  - forced close
  - address-specific write NAK
- WorkStart write failure integration:
  - process payload write failure 2501
  - ACK write failure 2601
  - error write best-effort failure
- AH-PILOT-18-A / 18-B / 19 / 20 evidence를 묶은 WorkStart Pilot readiness matrix.

## 12. 실행한 명령

Precheck / context:

- `git log --oneline -8`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-19.md`
- `Get-Content docs\harness\AH-PILOT-18-B.md`
- `Get-Content docs\harness\AH-PILOT-18-A.md`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart\WorkStartXgtFakePlcIntegrationTests.cs`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtReadOptions.cs`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtPlcOperations.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowService.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartErrorCode.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartStep.cs`

Sibling repo read-only:

- `rg -n "unsupported|NAK|Nak|Validate|validation|ReadContinuous|ReadAsync|FakePlc" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- `rg -n "OperationFailed|ReadFailed|GroupRead|ReadWorkStartBlockAsync|WorkStartXgtReadOptions" C:\AutomationHub.Rebuild\CAAutomationHub\src C:\AutomationHub.Rebuild\CAAutomationHub\tests`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcRuntime.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcMemoryImage.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcProtocolHandler.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\Channels\PlcChannelFakePlcValidationTests.cs`

Implementation / validation:

- `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj --filter "FullyQualifiedName~RunAsync_ReturnsReadFailed_WhenFakePlcRejectsWorkStartReadAddress|FullyQualifiedName~ReadWorkStartBlockAsync_ReturnsOperationFailed_WhenFakePlcRejectsReadAddress"`
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

## 13. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-20 목표인 FakePlc read NAK / 1101 failure harness를 test-only 영역에 추가했다.
- unsupported read address는 FakePlc map 수정 없이 `%DB99990` test-specific read option으로 제한했다.
- FakePlc unsupported read validation failure가 XGT NAK로 반환되고 `OperationFailed`로 낮아짐을 검증했다.
- `WorkStartFlowService`가 `GroupRead / ReadFailed / 1101` failure를 반환함을 검증했다.
- `ErrorWriteExpected == false`와 `%DB11410` unchanged evidence로 error write 미수행을 검증했다.
- DB query, payload write, ACK write가 호출되지 않음을 검증했다.
- Runtime / FlowDefinitions / PilotFlows core / PilotFlows.Xgt production source를 수정하지 않았다.
- FakePlc map, default read options, default read block layout을 수정하지 않았다.
- XgtChannelRunner / SqlClient / actual DB / actual PLC / FLOW.JSON / JSON parser / Flow Executor / RuntimeSnapshot / ChannelPollingResult 범위로 확장하지 않았다.
- 요구된 tests, build, diff check, boundary scan evidence를 남겼다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
