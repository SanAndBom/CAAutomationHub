# AH-PILOT-17 Closeout - FakePlc WorkStart Write Verification Harness

## 1. Summary

AH-PILOT-17에서는 `CAAutomationHub.PilotFlows.Xgt.Tests`의 기존 in-process FakePlc integration harness에 WorkStart XGT write 검증을 추가했다.

검증 내용:

- `WriteProcessPayloadAsync(...)`가 default process payload target `%DB11000`에 payload를 쓰는지 확인했다.
- `WriteStartAckAsync(...)`가 default ACK target `%DB11416`에 `ushort` 값 `1`을 little-endian 1 word로 쓰는지 확인했다.
- `WriteErrorCodeBestEffortAsync(...)`가 default error target `%DB11410`에 `WorkStartErrorCode.DbNotFound = 2301`을 little-endian 1 word로 쓰는지 확인했다.

영향:

- production code는 수정하지 않았다.
- Runtime / FlowDefinitions / PilotFlows core는 수정하지 않았다.
- FakePlc reference는 test project에만 유지했다.
- FakePlc map 파일은 수정하지 않았다.
- XgtChannelRunner reference, DB / SqlClient, FLOW.JSON / parser / executor, RuntimeSnapshot / ChannelPollingResult 참조를 추가하지 않았다.

## 2. 변경 파일 목록

- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/WorkStart/WorkStartXgtFakePlcIntegrationTests.cs`
  - FakePlc write verification test 3개를 추가했다.
- `docs/harness/AH-PILOT-17.md`
  - AH-PILOT-17 historical record를 추가했다.

## 3. FakePlc Write Verification 방식

검증 방식은 후보 A인 FakePlc memory inspection을 사용했다.

사용한 FakePlc API:

- `FakePlcRuntime.LastBulkWrite`
- `FakePlcRuntime.LastAckValue`
- `FakePlcRuntime.LastErrorCode`
- `FakePlcRuntime.ReadContinuous(...)`

test flow:

1. `FakePlcMapConfig`로 `%DB10000`, `%DB11000`, `%DB11410`, `%DB11416`, `%DB11418` memory image를 구성한다.
2. `FakePlcRuntime`과 `FakePlcProtocolHandler`를 in-process `TcpListener`에 연결한다.
3. `TcpTransport` + `XgtSession`으로 loopback FakePlc에 연결한다.
4. `WorkStartXgtPlcOperations(session)`을 default read/write options로 생성한다.
5. `EnsureConnectedAsync()` 후 write adapter method를 호출한다.
6. FakePlc runtime 기록과 `ReadContinuous(...)` read-back으로 memory 반영을 확인한다.

선택 이유:

- FakePlc가 이미 `LastBulkWrite`, `LastAckValue`, `LastErrorCode`, `ReadContinuous`를 제공한다.
- actual PLC write가 아니다.
- XGT protocol detail이나 XgtChannelRunner를 test에 가져오지 않아도 된다.

## 4. Process Payload Write 검증 결과

추가 test:

- `WriteProcessPayloadAsync_WithFakePlc_WritesPayloadToBulkTarget`

검증:

- `WriteProcessPayloadAsync(payload)` 결과가 `true`다.
- `runtime.LastBulkWrite`가 test payload와 동일하다.
- `runtime.ReadContinuous("%DB11000", payload.Length)`가 test payload와 동일하다.

payload:

- 140 bytes
- WorkStart process payload default 70 words와 같은 byte length다.

판정:

- `%DB11000` process payload write가 FakePlc memory와 write 기록에 반영됨을 확인했다.

## 5. ACK Write 검증 결과

추가 test:

- `WriteStartAckAsync_WithFakePlc_WritesAckValueToAckTarget`

검증:

- `WriteStartAckAsync()` 결과가 `true`다.
- `runtime.LastAckValue == 1`이다.
- `runtime.ReadContinuous("%DB11416", 2)`가 `{ 0x01, 0x00 }`이다.

판정:

- ACK value `1`이 `%DB11416`에 `ushort` little-endian 1 word로 반영됨을 확인했다.

## 6. Error Code Write 검증 결과

추가 test:

- `WriteErrorCodeBestEffortAsync_WithFakePlc_WritesErrorCodeToErrorTarget`

검증:

- `WriteErrorCodeBestEffortAsync(WorkStartErrorCode.DbNotFound)`를 호출한다.
- `runtime.LastErrorCode == 2301`이다.
- `runtime.ReadContinuous("%DB11410", 2)`가 `{ 0xFD, 0x08 }`이다.

판정:

- error code `2301`이 `%DB11410`에 `ushort` little-endian 1 word로 반영됨을 확인했다.

## 7. Best-Effort Failure 검증 여부

이번 AH-PILOT-17에서는 FakePlc failure injection test를 추가하지 않았다.

이유:

- 현재 in-process `FakePlcProtocolHandler` write path는 unsupported write를 안정적인 NAK로 낮추는 test helper가 아니라 handler exception과 connection close로 이어질 수 있다.
- 이 경로를 억지로 사용하면 network timing과 disposal fault에 묶인 flaky integration test가 될 위험이 있다.
- AH-PILOT-17 범위는 FakePlc write success verification harness로 제한했다.

기존 coverage:

- `WorkStartXgtPlcOperationsTests.WriteErrorCodeBestEffortAsync_DoesNotThrow_WhenSessionWriteFails`
- fake `IXgtSession`에서 write exception이 발생해도 `WriteErrorCodeBestEffortAsync(...)`가 외부로 exception을 던지지 않음을 검증한다.

판정:

- best-effort failure 정책은 기존 unit test coverage로 유지했다.
- FakePlc 기반 failure injection은 후속 후보로 남긴다.

## 8. 이식하지 않은 범위

이번 작업에서 제외했다.

- Runtime project 수정
- FlowDefinitions project 수정
- PilotFlows core 수정
- PilotFlows.Xgt production source 수정
- production project FakePlc reference 추가
- XgtChannelRunner reference 추가
- DB concrete 구현
- Microsoft.Data.SqlClient 추가
- FLOW.JSON 연결
- JSON parser / schema 구현
- Flow Executor 구현
- RuntimeSnapshot / ChannelPollingResult 참조
- actual PLC write test
- FakePlc map 파일 수정
- WorkStartPilotService source copy
- FakePlc write failure injection
- `WorkStartXgtWriteOptions.Default` 변경

## 9. 테스트 결과

실행:

```text
dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj
dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj
dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj
```

결과:

```text
CAAutomationHub.PilotFlows.Xgt.Tests:
  failed 0, passed 32, skipped 0, total 32

CAAutomationHub.PilotFlows.Tests:
  failed 0, passed 40, skipped 0, total 40

CAAutomationHub.Runtime.Tests:
  failed 0, passed 142, skipped 0, total 142
```

TDD note:

- AH-PILOT-17은 production implementation 추가가 아니라 AH-PILOT-16에서 이미 구현된 write adapter를 FakePlc harness로 검증하는 단계다.
- 신규 production code는 작성하지 않았다.
- 신규 harness test의 첫 실행은 통과했다. 이는 AH-PILOT-16 implementation이 이미 존재하고 FakePlc write support가 준비되어 있었기 때문이다.

## 10. 빌드 결과

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

## 11. Boundary Scan 결과

실행:

```text
git diff --check
rg -n "XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src tests
rg -n "XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.PilotFlows.Xgt tests\CAAutomationHub.PilotFlows.Xgt.Tests
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
rg -n "FakePlc" src tests -g "*.csproj"
rg -n "XgtChannelRunner" src tests -g "*.csproj"
rg -n "Microsoft.Data.SqlClient|SqlConnection" src tests -g "*.csproj" -g "*.cs"
rg -n "FakePlc" src\CAAutomationHub.PilotFlows.Xgt -g "*.cs" -g "*.csproj"
```

결과:

- `git diff --check` exit code `0`
- line-ending warning만 출력됨
- 전체 `src tests` boundary scan은 기존 Runtime / WPF contract와 tests의 `RuntimeSnapshot`, `ChannelPollingResult`, `Json` hit를 출력했다.
- targeted scan `src\CAAutomationHub.PilotFlows.Xgt tests\CAAutomationHub.PilotFlows.Xgt.Tests`는 금지 boundary hit 없음, exit code `1`
- FakePlc reference는 `tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`에만 존재한다.
- `src\CAAutomationHub.PilotFlows.Xgt`에는 FakePlc hit 없음, exit code `1`
- XgtChannelRunner project reference 없음, exit code `1`
- Microsoft.Data.SqlClient / SqlConnection hit 없음, exit code `1`

판정:

- Runtime / FlowDefinitions / PilotFlows core boundary 오염 없음.
- PilotFlows.Xgt production source 변경 없음.
- FakePlc dependency는 test project에만 유지됨.
- XgtChannelRunner / DB / SqlClient / FLOW.JSON / JSON parser / FlowExecutor 범위로 확장하지 않음.

## 12. 다음 후보

- FakePlc write failure injection이 필요하면 FakePlc protocol handler가 deterministic write NAK scenario를 제공하도록 sibling repo에서 별도 review한다.
- ACK write 후 FakePlc `ClearStartSignalOnAck`에 의해 `D5083` start signal이 clear되는지 WorkStart-specific integration으로 별도 검증할 수 있다.
- WorkStart process payload 실제 field-level content 검증은 DB/payload scenario가 고정된 뒤 별도 harness로 분리한다.

## 13. 실행한 명령

Precheck:

- `git log --oneline -5`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-14-B.md`
- `Get-Content docs\harness\AH-PILOT-15.md`
- `Get-Content docs\harness\AH-PILOT-16.md`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtWriteOptions.cs`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtPlcOperations.cs`
- `Get-ChildItem tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart -Force`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart\WorkStartXgtFakePlcIntegrationTests.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart\WorkStartXgtPlcOperationsTests.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`

Sibling repo read-only:

- `rg -n "LastBulkWrite|LastAckValue|LastErrorCode|ReadContinuous|SnapshotBlocks|WriteAsync|WriteContinuous|D11416|D11410|DB11000|%DB11000|%DB11416|%DB11410" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcRuntime.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcMemoryImage.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcProtocolHandler.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcRuleConfig.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtDriverCore.Tests\FakePlcWordSignalTests.cs`

Implementation / validation:

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
- `rg -n "XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.PilotFlows.Xgt tests\CAAutomationHub.PilotFlows.Xgt.Tests`
- `rg -n "FakePlc" src\CAAutomationHub.PilotFlows.Xgt -g "*.cs" -g "*.csproj"`

## 14. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-17 목표인 FakePlc 기반 WorkStart XGT write / ACK / error writer 검증을 test-only harness에 추가했다.
- process payload write는 `%DB11000` runtime record와 read-back으로 검증했다.
- ACK write는 `%DB11416` `ushort` little-endian 값 `1`로 검증했다.
- error code write는 `%DB11410` `WorkStartErrorCode.DbNotFound = 2301` little-endian 값으로 검증했다.
- best-effort failure 정책은 기존 adapter unit test coverage로 유지했다.
- Runtime / FlowDefinitions / PilotFlows core / PilotFlows.Xgt production source를 수정하지 않았다.
- FakePlc map을 수정하지 않았다.
- FakePlc reference는 test project에만 유지했다.
- XgtChannelRunner / DB / SqlClient / FLOW.JSON / JSON parser / FlowExecutor / RuntimeSnapshot / ChannelPollingResult 범위로 확장하지 않았다.
- 요구된 tests, build, diff check, boundary scan을 실행했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
