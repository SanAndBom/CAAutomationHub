# AH-WPF-22 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- optional `SnapshotChanged` push refresh 경로를 `DashboardViewModel`에 연결한다.
- 기존 `DispatcherTimer` pull refresh 경로를 유지한다.
- `DashboardRefreshOrchestrator`의 UI dispatcher marshal, coalescing, stale snapshot protection 정책을 적용한다.
- 실제 Runtime 연결 전 push refresh wiring 안전성을 검증한다.

## 3. Implemented Scope
- `DashboardViewModel`에서 `adapter as IRuntimeDashboardEventSource`로 `IRuntimeDashboardEventSource`를 감지했다.
- Adapter가 optional event source interface를 구현하면 `SnapshotChanged`를 구독한다.
- `DashboardViewModel`에 `SnapshotChanged` handler를 추가했다.
- `SnapshotChanged` payload를 `DashboardRefreshOrchestrator`에 연결했다.
- 기존 constructor compatibility를 유지하면서 test-oriented `IUiDispatcher` overload를 추가했다.
- 기본 production path에서 `WpfUiDispatcher`를 사용했다.
- `LoadSnapshot()`이 pull snapshot을 apply한 뒤 `MarkApplied(snapshot)`을 호출한다.
- `DashboardRefreshOrchestrator.MarkApplied`를 추가했다.
- `Dispose`에서 `SnapshotChanged` 구독을 해제한다.
- `EventReceived`는 구독하지 않고 처리하지 않는다.
- 기존 `DispatcherTimer` refresh를 유지했다.

## 4. Changed Files
- `src/CAAutomationHub.Wpf/ViewModels/DashboardViewModel.cs`
- `src/CAAutomationHub.Wpf/Services/DashboardRefreshOrchestrator.cs`
- `tests/CAAutomationHub.Wpf.Tests/Services/DashboardRefreshOrchestratorTests.cs`
- `tests/CAAutomationHub.Wpf.Tests/ViewModels/DashboardViewModelEventRefreshTests.cs`

## 5. Final Refresh Behavior

### 5.1 Pull Refresh
- `DispatcherTimer` 1초 refresh는 유지한다.
- `RefreshCommand`는 유지한다.
- Add/Edit/Delete는 기존 `LoadSnapshot` 흐름을 계속 사용한다.
- `LoadSnapshot`은 snapshot을 apply한 뒤 `MarkApplied(snapshot)`을 호출한다.

### 5.2 Push Refresh
- Adapter가 `IRuntimeDashboardEventSource`를 구현하면 `DashboardViewModel`은 `SnapshotChanged`를 구독한다.
- `SnapshotChanged` payload의 `DashboardSnapshot`을 직접 사용한다.
- Event handler는 `GetSnapshot()`을 다시 호출하지 않는다.
- `DashboardRefreshOrchestrator`가 snapshot apply 전에 dispatcher marshal을 수행한다.
- 기존 latest-only coalescing과 reentrancy guard 정책이 push snapshot에도 적용된다.

### 5.3 Stale Snapshot Protection
- newer manual pull snapshot이 이미 apply된 뒤에는 older push snapshot을 무시한다.
- `MarkApplied`는 마지막으로 apply된 snapshot time을 기록한다.
- Stale snapshot detection은 `DashboardSnapshot.Health.SnapshotTime`을 사용한다.

### 5.4 Dispose
- Timer stop 동작은 유지한다.
- Timer tick unsubscribe 동작은 유지한다.
- `SnapshotChanged` unsubscribe를 추가했다.
- `Dispose` 이후 raise된 push event는 ViewModel을 갱신하지 않는다.

### 5.5 EventReceived
- 이 scenario에서는 `EventReceived`를 처리하지 않는다.
- Event Log bridge 작업은 향후 scenario로 분리한다.

## 6. Tests Added / Updated
- Event source adapter는 `SnapshotChanged`를 통해 구독된다.
- `SnapshotChanged` payload가 `DashboardViewModel` state에 반영된다.
- Event handling은 `GetSnapshot()`을 다시 호출하지 않는다.
- Event source가 있어도 `RefreshCommand` pull refresh는 계속 동작한다.
- `Dispose` 이후 `SnapshotChanged`는 ViewModel을 갱신하지 않는다.
- `Dispose` 후 handler count가 감소한다.
- `EventReceived`는 구독하지 않는다.
- older push snapshot은 newer manual pull snapshot을 덮어쓰지 않는다.
- `DashboardRefreshOrchestrator.MarkApplied`는 stale submitted snapshot을 방지한다.

## 7. Validation
Targeted tests:
- `DashboardViewModelEventRefreshTests`: passed
- `DashboardRefreshOrchestratorTests`: passed

`dotnet build CAAutomationHub.sln`

Result:
- Succeeded
- Warning 0
- Error 0

`dotnet test CAAutomationHub.sln`

Result:
- Succeeded
- 177 passed
- 0 failed

Note:
- 초기 parallel targeted test run 중 WPF obj dll file lock collision이 한 번 발생했다. 이후 sequential rerun은 통과했다.

## 8. Boundary Rules
- Actual Runtime connection은 추가하지 않았다.
- Supervisor implementation은 추가하지 않았다.
- `IAutomationHubSupervisor` implementation은 추가하지 않았다.
- Runtime project는 생성하지 않았다.
- `XgtDriverCore`는 참조하지 않았다.
- `XgtChannelRunner`는 참조하지 않았다.
- `FakePlc`는 참조하지 않았다.
- Actual PLC connection은 추가하지 않았다.
- Runtime command execution은 추가하지 않았다.
- Add/Edit/Delete runtime command conversion은 구현하지 않았다.
- UI는 변경하지 않았다.
- Communication Trend는 변경하지 않았다.
- Mini Trend는 변경하지 않았다.
- Runtime telemetry는 구현하지 않았다.
- `BalanceController`는 구현하지 않았다.
- Timer는 제거하지 않았다.
- `EventReceived`는 처리하지 않았다.
- Event Log bridge는 구현하지 않았다.

## 9. Known Limitations / Notes
- AH-WPF-22 기준 `RuntimeDashboardAdapter`는 아직 `IRuntimeDashboardEventSource`를 구현하지 않는다.
- Actual push refresh는 현재 test double을 통해서만 검증했다.
- `EventReceived`는 아직 Event Log에 연결하지 않았다.
- Stale snapshot detection은 `SnapshotTime`을 사용한다.
- 실제 Runtime snapshot clock 또는 revision guarantee가 약하면 revision 기반 stale policy를 이후 검토해야 한다.
- Add/Edit/Delete와 push snapshot ordering은 `MarkApplied`로 1차 보호를 갖지만, 실제 Runtime 연결 시 다시 검증해야 한다.
- 이 문서는 AH-WPF-22 당시 Closeout 기준 기록이다. 최신 진행 상태와 다음 단계 번호 체계는 `docs/context/WPF_RUNTIME_BRIDGE_CURRENT_STATE.md`를 우선한다.

## 10. Next Scenario Candidates
1. AH-WPF-23: Runtime Event Bridge Skeleton
   - `EventReceived`를 `RuntimeDashboardEvent`와 `RuntimeEventLogItem`으로 연결한다.
   - Runtime event bridge service
   - Rolling buffer와 UI marshal policy review

2. AH-WPF-24: RuntimeDashboardAdapter EventSource Skeleton
   - `RuntimeDashboardAdapter`가 `IRuntimeDashboardEventSource`를 구현해야 하는지 검토한다.
   - provider/supervisor event wiring 전에 skeleton을 추가한다.

3. AH-WPF-25: Runtime Snapshot Revision Policy
   - `SnapshotTime` 외 Revision 또는 Sequence 추가를 검토한다.
   - stale snapshot protection을 강화한다.

4. AH-RUNTIME-01: Supervisor Skeleton
   - `IAutomationHubSupervisor`를 추가한다.
   - in-memory/fake supervisor를 추가한다.
   - 실제 PLC는 연결하지 않는다.

Note:
- 위 Next Scenario Candidates는 AH-WPF-22 당시 후보 기록이다. 현재 기준에서 AH-WPF-24는 Current State Audit이며, Runtime Event Bridge는 AH-WPF-25 또는 AH-RUNTIME-03 후보로 본다.
