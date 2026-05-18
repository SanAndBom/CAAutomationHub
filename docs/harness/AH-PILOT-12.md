# AH-PILOT-12 WorkStart XGT Read Adapter Skeleton

## Summary

AH-PILOT-12에서는 WorkStart read path를 XGT-specific adapter project로 분리하는 skeleton을 추가했다.

변경 내용:

- `CAAutomationHub.PilotFlows.Xgt` project를 추가했다.
- `CAAutomationHub.PilotFlows.Xgt` project만 `CAAutomationHub.PilotFlows`와 sibling `AutomationHub.XgtDriverCore`를 참조한다.
- `WorkStartXgtPlcOperations`를 추가해 `IWorkStartPlcOperations`의 connect/read path를 XGT session으로 낮췄다.
- XGT ACK + data는 `WorkStartReadBlockOperationResult.Success(data)`로 mapping한다.
- XGT NAK 또는 read 예외는 `OperationFailed(message)`로 mapping한다.
- XGT ACK이지만 data block bytes를 만들 수 없는 경우는 `ParseFailed(message)`로 mapping한다.
- write / ACK / error writer path는 AH-PILOT-12 범위 밖이므로 명시적으로 `NotSupportedException`을 던진다.
- `CAAutomationHub.PilotFlows.Xgt.Tests` project를 추가해 fake `IXgtSession` 기반 mapping test를 고정했다.

영향:

- Runtime / FlowDefinitions / WPF project는 수정하지 않았다.
- `CAAutomationHub.PilotFlows` project에는 XgtDriverCore reference를 추가하지 않았다.
- FakePlc / XgtChannelRunner / DB / SqlClient dependency는 추가하지 않았다.
- sibling repo ProjectReference는 단기 local pilot용이다. 장기적으로는 subtree 또는 package 전환을 재검토해야 한다.

## Sibling Repo Pre-Check

명령:

- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore log --oneline -5`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore branch --show-current`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\AutomationHub.XgtDriverCore.csproj`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short -- src/AutomationHub.XgtDriverCore`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short -- src/AutomationHub.XgtDriverCore/AutomationHub.XgtDriverCore.csproj`

결과:

- branch: `main`
- latest anchor: `fa0ab4f Merge pull request #119 from SanAndBom/codex/add-uint32-and-int32-display-formats`
- sibling dirty 파일:
  - `tools/AutomationHub.XgtDriverCore.FakePlc/appsettings/fakeplc.map.json`
  - `context-events/pending/*.json`
- `src/AutomationHub.XgtDriverCore` source tree: clean
- `src/AutomationHub.XgtDriverCore/AutomationHub.XgtDriverCore.csproj`: clean
- `AutomationHub.XgtDriverCore.csproj` 내용은 SDK project이며 assembly/root namespace만 지정한다.

판단:

- AH-PILOT-11에서 확인한 `fa0ab4f` anchor와 일치한다.
- dirty 범위는 FakePlc map/context-events pending이며 이번 adapter ProjectReference와 직접 충돌하지 않는다.
- STOP 조건에 해당하지 않아 진행했다.

## Project / Reference Structure

추가 project:

- `src/CAAutomationHub.PilotFlows.Xgt/CAAutomationHub.PilotFlows.Xgt.csproj`
- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj`

`CAAutomationHub.PilotFlows.Xgt` references:

- `..\CAAutomationHub.PilotFlows\CAAutomationHub.PilotFlows.csproj`
- `..\..\..\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\AutomationHub.XgtDriverCore.csproj`

`CAAutomationHub.PilotFlows.Xgt.Tests` references:

- `..\..\src\CAAutomationHub.PilotFlows.Xgt\CAAutomationHub.PilotFlows.Xgt.csproj`

Solution update:

- `CAAutomationHub.sln`에 `CAAutomationHub.PilotFlows.Xgt`와 `CAAutomationHub.PilotFlows.Xgt.Tests`를 추가했다.
- sibling `AutomationHub.XgtDriverCore`는 solution project로 남기지 않고 adapter project의 local ProjectReference로만 유지했다.

## Adapter / Mapper Types

추가 타입:

- `CAAutomationHub.PilotFlows.Xgt.WorkStart.WorkStartXgtPlcOperations`
- `CAAutomationHub.PilotFlows.Xgt.WorkStart.WorkStartXgtReadResultMapper`

선택:

- AH-PILOT-12에서는 `IWorkStartPlcOperations` 전체 구현체 후보를 만들되, 실제 동작은 connect/read path로 제한했다.
- `IXgtSession` API가 단순하고 `ConnectAsync`, `ReadAsync(XgtReadRequest, CancellationToken)`가 명확해 full interface skeleton이 과도하지 않다고 판단했다.
- write / ACK / error writer는 no-op로 두지 않고 명시적 unsupported policy로 고정했다.

## Read Mapping Scope

Success:

- 조건: XGT ACK, variable block 존재, 첫 번째 block의 `Data`가 null/empty가 아님
- 결과: `WorkStartReadBlockOperationResult.Success(data)`

OperationFailed:

- 조건: XGT NAK
- 조건: `IXgtSession.ReadAsync`에서 예외 발생
- 결과: `WorkStartReadBlockOperationResult.OperationFailed(message)`
- caller cancellation은 operation failure로 삼지 않고 그대로 전파한다.

ParseFailed:

- 조건: XGT ACK이지만 variable block 없음
- 조건: 첫 번째 variable block `Data`가 null 또는 empty
- 결과: `WorkStartReadBlockOperationResult.ParseFailed(message)`

금지 항목 유지:

- XGT classification enum을 PilotFlows public model로 노출하지 않았다.
- raw request / response hex를 `WorkStartReadBlockOperationResult`에 넣지 않았다.
- `TransportException` type을 PilotFlows 결과로 넘기지 않았다.
- `ChannelPollingFailureKind`를 참조하지 않았다.

## Not Implemented

AH-PILOT-12에서 이식하지 않은 범위:

- write / ACK / error writer 구현
- FakePlc integration
- XgtChannelRunner integration
- DB concrete / SQL query 구현
- `Microsoft.Data.SqlClient` dependency
- FLOW.JSON 연결
- Flow Executor 구현
- RuntimeSnapshot / ChannelPollingResult 연결
- WorkStartPilotService source copy
- Runtime project 수정
- FlowDefinitions project 수정

## Test Strategy

선택한 전략:

- 후보 B: fake `IXgtSession` 기반 adapter test

이유:

- FakePlc reference 없이 adapter class 자체를 검증할 수 있다.
- 실제 XgtDriverCore public API인 `IXgtSession`, `XgtReadRequest`, `XgtReadResponse`에 맞춰 ProjectReference boundary를 검증한다.
- integration 검증은 AH-PILOT-13 이후로 보류한다.

추가 테스트:

- `EnsureConnectedAsync_ConnectsDisconnectedSession`
- `ReadWorkStartBlockAsync_MapsAckReadWithDataToSuccess`
- `ReadWorkStartBlockAsync_MapsNakToOperationFailed`
- `ReadWorkStartBlockAsync_MapsReadExceptionToOperationFailed`
- `ReadWorkStartBlockAsync_MapsAckWithoutDataToParseFailed`
- `WorkStartReadBlockOperationResult_DoesNotExposeXgtClassification`
- `WriteMethods_AreExplicitlyUnsupportedInReadSkeleton`

TDD evidence:

- RED: `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
  - 실패 이유: `WorkStartXgtPlcOperations` / `CAAutomationHub.PilotFlows.Xgt.WorkStart` 미존재
- GREEN: 같은 명령 재실행
  - 결과: 실패 0, 통과 7, 전체 7

## Validation Results

실행 명령:

- `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
  - 결과: 실패 0, 통과 40, 전체 40
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
  - 결과: 실패 0, 통과 142, 전체 142
- `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
  - 결과: 실패 0, 통과 7, 전체 7
- `dotnet build CAAutomationHub.sln`
  - 결과: 경고 0, 오류 0
- `git diff --check`
  - 결과: 출력 없음

## Boundary Scan

명령:

- `rg -n "FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.PilotFlows.Xgt tests\CAAutomationHub.PilotFlows.Xgt.Tests`

결과:

- match 없음

의미:

- 새 adapter/test project에 FakePlc, XgtChannelRunner, SQL/DB, Runtime snapshot/polling, FLOW.JSON/JSON execution 의존을 추가하지 않았다.

## Project Reference Verification

명령:

- `rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"`

확인 결과:

- `src\CAAutomationHub.PilotFlows.Xgt\CAAutomationHub.PilotFlows.Xgt.csproj`만 sibling `AutomationHub.XgtDriverCore`를 참조한다.
- `src\CAAutomationHub.PilotFlows\CAAutomationHub.PilotFlows.csproj`에는 XgtDriverCore reference가 없다.
- `src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj`에는 XgtDriverCore / FakePlc / XgtChannelRunner reference가 없다.
- `src\CAAutomationHub.FlowDefinitions\CAAutomationHub.FlowDefinitions.csproj`에는 XgtDriverCore / FakePlc / XgtChannelRunner reference가 없다.
- 새 test project는 `CAAutomationHub.PilotFlows.Xgt`만 참조한다.
- FakePlc / XgtChannelRunner / `Microsoft.Data.SqlClient` PackageReference는 없다.

## Changed Files

- `CAAutomationHub.sln`
  - 새 adapter project와 adapter test project를 solution에 등록했다.
- `src/CAAutomationHub.PilotFlows.Xgt/CAAutomationHub.PilotFlows.Xgt.csproj`
  - PilotFlows와 sibling XgtDriverCore를 참조하는 XGT-specific adapter project를 추가했다.
- `src/CAAutomationHub.PilotFlows.Xgt/WorkStart/WorkStartXgtPlcOperations.cs`
  - `IWorkStartPlcOperations` connect/read skeleton을 추가했다.
- `src/CAAutomationHub.PilotFlows.Xgt/WorkStart/WorkStartXgtReadResultMapper.cs`
  - XGT read response를 PilotFlows-local `WorkStartReadBlockOperationResult`로 낮추는 mapper를 추가했다.
- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
  - XGT adapter 전용 test project를 추가했다.
- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/WorkStart/WorkStartXgtPlcOperationsTests.cs`
  - fake `IXgtSession` 기반 read mapping / unsupported write policy test를 추가했다.

## Next Candidates

- AH-PILOT-13: WorkStart XGT read request configuration policy 검토
- AH-PILOT-13 또는 이후: FakePlc integration test를 별도 integration boundary에서 추가
- 이후 후보: write / ACK / error writer adapter 확장
- 이후 후보: sibling XgtDriverCore reference를 subtree/package로 전환할지 결정

## Self-Check

판정: ACCEPT

근거:

- sibling repo source/csproj clean 조건을 확인했다.
- XgtDriverCore dependency는 `CAAutomationHub.PilotFlows.Xgt` project에만 추가했다.
- Runtime / FlowDefinitions / PilotFlows project를 XGT dependency로 오염시키지 않았다.
- FakePlc / XgtChannelRunner / DB / SqlClient dependency를 추가하지 않았다.
- WorkStart read mapping Success / OperationFailed / ParseFailed를 테스트로 고정했다.
- write / ACK / error writer는 명시적 unsupported로 남겨 no-op 오해를 피했다.
- 지정 테스트와 solution build가 통과했다.
- boundary scan과 project reference scan으로 금지 범위 미침범을 확인했다.
