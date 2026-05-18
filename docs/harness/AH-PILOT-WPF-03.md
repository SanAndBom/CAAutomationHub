# AH-PILOT-WPF-03 Closeout - WPF WorkStart ViewModel Boundary

## 1. Summary

AH-PILOT-WPF-03은 WPF가 WorkStart pilot 실행을 `IWorkStartExecutionService` application boundary를 통해 요청하고, 실행 결과를 표시 상태로 낮추는 최소 ViewModel skeleton을 추가한 작업이다.

새 `WorkStartPilotViewModel`은 `IWorkStartExecutionService`와 `targetId`만 생성자에서 받는다. `ExecuteOnceAsync`는 `WorkStartExecutionRequest(TargetId: targetId)`를 만들어 service에 전달하고, `WorkStartExecutionResult`의 success / failure display fields를 ViewModel property로 반영한다.

이번 작업은 WPF ViewModel boundary와 fake service 기반 tests에 한정했다. XAML, button command binding, App.xaml / DI composition, DashboardViewModel 연결, RuntimeSnapshot, DashboardSnapshot, ChannelPollingResult, Runtime project, PilotFlows, PilotFlows.Xgt, FlowDefinitions, XGT concrete, DB concrete, FLOW.JSON, JSON parser, Flow Executor는 수정하지 않았다.

## 2. 변경 파일 목록

- `src/CAAutomationHub.Wpf/CAAutomationHub.Wpf.csproj`
- `src/CAAutomationHub.Wpf/ViewModels/Pilot/WorkStartPilotViewModel.cs`
- `tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj`
- `tests/CAAutomationHub.Wpf.Tests/ViewModels/WorkStartPilotViewModelTests.cs`
- `docs/harness/AH-PILOT-WPF-03.md`

## 3. 추가한 ViewModel / boundary 요약

`WorkStartPilotViewModel`을 `src/CAAutomationHub.Wpf/ViewModels/Pilot` 아래에 추가했다.

역할:

- WorkStart 1회 실행 요청을 WPF-facing method로 제공한다.
- 실행 중 상태를 `IsBusy`로 표시한다.
- 마지막 실행 결과를 `LastSucceeded`, `LastStatus`, `LastStep`, `LastErrorCode`, `LastErrorCodeName`, `LastMessage`, `SelectedLotId`, `LastErrorWriteExpected`, `LastStartedAt`, `LastCompletedAt`, `LastDuration`으로 표시한다.
- 실제 UI command, XAML button, DI wiring은 만들지 않는다.

## 4. IWorkStartExecutionService 의존 구조

WPF project는 `CAAutomationHub.PilotApp` project reference를 추가했다.

의존 방향:

```text
CAAutomationHub.Wpf
    -> CAAutomationHub.PilotApp
        -> CAAutomationHub.PilotFlows
```

`WorkStartPilotViewModel`은 `IWorkStartExecutionService`, `WorkStartExecutionRequest`, `WorkStartExecutionResult`만 참조한다. `WorkStartFlowService`, `PilotFlows.Xgt`, XGT driver, FakePlc, XgtChannelRunner, DB concrete, SqlClient를 직접 참조하지 않는다.

## 5. success / failure display state mapping

Success mapping:

- `Succeeded` -> `LastSucceeded`
- `Status` -> `LastStatus`
- `Step` -> `LastStep`
- `ErrorCode` -> `LastErrorCode`
- `ErrorCodeName` -> `LastErrorCodeName`
- `Message` -> `LastMessage`
- `SelectedLotId` -> `SelectedLotId`
- `ErrorWriteExpected` -> `LastErrorWriteExpected`
- `StartedAt`, `CompletedAt`, `Duration` -> matching timestamp / duration properties

Failure mapping도 같은 경로를 사용한다. failure result가 `DbNotFound`, `db-query`, error code `2301`, message, selected LOT ID, error write expected를 제공하면 ViewModel이 그대로 display state에 반영한다.

서비스가 예외를 던지는 경우에는 ViewModel이 user-facing failure state로 낮춘다. 이 skeleton은 `LastStatus = "Failed"`, `LastStep = "exception"`, `LastSucceeded = false`, `LastMessage = exception.Message`, `LastErrorWriteExpected = false`로 표시한다. Cancellation token으로 요청된 `OperationCanceledException`은 전파한다.

## 6. TargetId request 전달

`WorkStartPilotViewModel(IWorkStartExecutionService executionService, string targetId)` 형태로 target id를 required constructor argument로 받는다.

`ExecuteOnceAsync`는 다음 request만 생성한다.

```text
new WorkStartExecutionRequest(TargetId: targetId)
```

UI target selection은 후속 단계로 남겼다. `"Pilot-Default"` 같은 implicit test value는 ViewModel 내부에 넣지 않았다.

## 7. busy / duplicate policy

초기 WPF boundary duplicate policy는 block / ignore 방식이다.

- `IsBusy == true`이면 두 번째 `ExecuteOnceAsync`는 service를 다시 호출하지 않고 즉시 return한다.
- 기존 실행 상태와 진행 중 display state를 유지한다.
- queue, scheduler, hosted service, command audit는 구현하지 않았다.

이 정책은 `ExecuteOnceAsync_BlocksDuplicateExecution_WhenBusy` test로 고정했다.

## 8. WPF ViewModel / XAML / DI 미구현 범위

이번 작업에서 구현하지 않은 범위:

- XAML 수정
- 실제 button command binding
- command property 추가
- App.xaml / App.xaml.cs / DI composition 수정
- DashboardViewModel 연결
- RealtimeLog / operation history storage
- actual WorkStartFlowService composition
- XGT adapter concrete composition
- DB concrete composition
- FLOW.JSON / Flow Executor 연결

## 9. RuntimeSnapshot / PLC Card 비오염 확인

수정하지 않은 대상:

- Runtime project
- RuntimeSnapshot
- DashboardSnapshot
- ChannelPollingResult
- FlowDefinitions project
- PilotFlows project
- PilotFlows.Xgt project

Dashboard PLC Card 상태에는 WorkStart result를 넣지 않았다. Pilot transaction display state는 새 ViewModel 내부 property로만 보관한다.

## 10. 테스트 결과

RED evidence:

- `dotnet test tests\CAAutomationHub.Wpf.Tests\CAAutomationHub.Wpf.Tests.csproj`
- result: expected compile failure
- failure: `CAAutomationHub.Wpf.ViewModels` namespace에 `Pilot` namespace / `WorkStartPilotViewModel`이 없어서 실패

GREEN / validation:

- `dotnet test tests\CAAutomationHub.Wpf.Tests\CAAutomationHub.Wpf.Tests.csproj`
  - result: pass
  - failed 0, passed 219, skipped 0
- `dotnet test tests\CAAutomationHub.PilotApp.Tests\CAAutomationHub.PilotApp.Tests.csproj`
  - result: pass
  - failed 0, passed 6, skipped 0
- `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
  - result: pass
  - failed 0, passed 40, skipped 0
- `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
  - result: pass
  - failed 0, passed 39, skipped 0
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
  - result: pass
  - failed 0, passed 142, skipped 0

## 11. 빌드 결과

- `dotnet build CAAutomationHub.sln`
  - result: pass
  - warnings 0
  - errors 0

## 12. boundary scan 결과

Full requested scan:

- `rg -n "XgtDriverCore|FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.Wpf tests\CAAutomationHub.Wpf.Tests src\CAAutomationHub.PilotApp tests\CAAutomationHub.PilotApp.Tests`
- result: exit code 0 with existing hits in WPF RuntimeSnapshot adapter / mapper tests and existing `DashboardLayoutSettingsService` JSON settings path.
- 판단: 기존 WPF runtime dashboard bridge와 settings persistence hit이며, 이번 WorkStart ViewModel boundary contamination은 아니다.

Narrow new-boundary scan:

- `rg -n "XgtDriverCore|FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.Wpf\ViewModels\Pilot tests\CAAutomationHub.Wpf.Tests\ViewModels\WorkStartPilotViewModelTests.cs src\CAAutomationHub.PilotApp tests\CAAutomationHub.PilotApp.Tests`
- result: exit code 1, output 없음.
- 판단: 새 ViewModel / 새 WPF tests / PilotApp boundary에 금지 concrete keyword hit 없음.

Project reference scan:

- `rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"`
- result: `CAAutomationHub.Wpf -> CAAutomationHub.PilotApp` reference와 `CAAutomationHub.Wpf.Tests -> CAAutomationHub.PilotApp` reference가 추가됨.
- WPF가 `PilotFlows.Xgt`, `XgtDriverCore`, `FakePlc`, `XgtChannelRunner`, `Microsoft.Data.SqlClient`를 참조하지 않는다.
- Runtime project reference는 수정되지 않았다.

Whitespace:

- `git diff --check`
  - result: exit code 0
  - output 없음

## 13. 다음 후보

- AH-PILOT-WPF-04: WorkStart pilot command / button UI boundary review 또는 XAML 연결 전 설계.
- Pilot composition boundary review: WPF가 concrete XGT / DB를 조립하지 않도록 composition root 위치를 먼저 결정.
- Pilot operation log / transaction history boundary review: Runtime event stream과 섞지 않는 별도 operation event path 검토.
- Exception / cancellation display policy 상세화: 운영 UI 문구와 cancellation behavior를 별도 테스트로 고정.

## 14. Self-Check

판정: `ACCEPT`

근거:

- WPF ViewModel이 `IWorkStartExecutionService`만 바라보는 최소 boundary를 추가했다.
- fake service 기반 tests로 success display state, failure display state, TargetId 전달, busy duplicate block, forbidden type/reference absence를 검증했다.
- WPF project는 허용된 `CAAutomationHub.PilotApp`만 새로 참조했다.
- WPF가 `PilotFlows.Xgt`, XGT driver, FakePlc, XgtChannelRunner, DB concrete, SqlClient를 직접 참조하지 않는다.
- RuntimeSnapshot, DashboardSnapshot, ChannelPollingResult, Runtime project, FlowDefinitions, PilotFlows, PilotFlows.Xgt를 수정하지 않았다.
- XAML, button wiring, App.xaml / DI composition, DashboardViewModel 연결을 구현하지 않았다.
- 요청된 테스트, solution build, diff check, boundary scan을 수행했다.
- ContextPublisher 자동 publish는 재도입하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
