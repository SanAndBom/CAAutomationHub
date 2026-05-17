# AH-WPF-21 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- Runtime push refresh를 `DashboardViewModel`에 직접 연결하기 전에 안전한 orchestration unit을 확보한다.
- 향후 `SnapshotChanged` event handling을 위한 UI thread marshal, coalescing, reentrancy 정책을 검증한다.
- 기존 `DispatcherTimer` refresh model을 유지하면서 이후 event-based refresh를 준비한다.

## 3. Implemented Scope
- `IUiDispatcher` 추가
- `WpfUiDispatcher` 추가
- `DashboardRefreshOrchestrator` 추가
- `SubmitSnapshot(DashboardSnapshot snapshot)` 추가
- latest-only coalescing 구현
- 중복 dispatcher apply scheduling 방지
- apply 중 snapshot이 submit되면 pending snapshot으로 보존
- apply 후 pending snapshot이 남아 있으면 dispatcher pass를 한 번 더 예약
- `DashboardSnapshot.Health.SnapshotTime` 기준 stale snapshot protection 구현
- `EventReceived`는 orchestrator 범위 밖에 유지

## 4. Changed Files
- `src/CAAutomationHub.Wpf/Services/IUiDispatcher.cs`
- `src/CAAutomationHub.Wpf/Services/WpfUiDispatcher.cs`
- `src/CAAutomationHub.Wpf/Services/DashboardRefreshOrchestrator.cs`
- `tests/CAAutomationHub.Wpf.Tests/Services/DashboardRefreshOrchestratorTests.cs`

## 5. Orchestration Policy

### 5.1 Dispatcher Marshal
- Orchestrator는 WPF `Dispatcher`에 직접 의존하지 않는다.
- UI apply work는 `IUiDispatcher.Post(Action)`으로 예약한다.
- `WpfUiDispatcher`는 `Dispatcher.BeginInvoke`를 감싼다.
- AH-WPF-21 기준 실제 `DashboardViewModel` wiring은 future scenario로 남긴다.

### 5.2 Latest-only Coalescing
- 짧은 시간에 여러 snapshot이 도착하면 마지막 accepted snapshot만 유지한다.
- dispatcher apply가 이미 예약되어 있으면 중복 apply를 예약하지 않는다.
- Dashboard snapshot UI는 모든 중간 상태보다 최신 상태를 우선한다.

### 5.3 Reentrancy Guard
- apply 실행 중 새 snapshot이 submit되면 pending으로 보존한다.
- active apply가 끝난 뒤 pending snapshot이 있으면 dispatcher pass를 한 번 더 예약한다.

### 5.4 Stale Snapshot Policy
- stale detection은 `DashboardSnapshot.Health.SnapshotTime`을 사용한다.
- older snapshot은 newer snapshot을 덮어쓰지 않는다.
- timestamp가 같으면 마지막으로 submit된 snapshot이 이긴다.

### 5.5 EventReceived Separation
- `EventReceived`는 snapshot refresh와 다른 정책을 사용한다.
- Snapshot refresh는 latest-only다.
- Runtime event는 이후 runtime event bridge와 rolling buffer 정책으로 처리해야 한다.
- 이 scenario에서는 `IEventStreamService`와 `RuntimeEventLogItemMapper`를 연결하지 않았다.

## 6. Tests Added
Test file:
- `tests/CAAutomationHub.Wpf.Tests/Services/DashboardRefreshOrchestratorTests.cs`

Tests로 고정한 정책:
- `SubmitSnapshot(null)`은 `ArgumentNullException`을 발생시킨다.
- Snapshot submit은 dispatcher `Post`를 호출한다.
- 연속 submit은 dispatcher reservation 하나로 coalescing된다.
- dispatcher flush는 마지막 snapshot만 apply한다.
- apply 중 submit된 snapshot은 두 번째 dispatcher pass에서 apply된다.
- older snapshot은 newer snapshot을 덮어쓰지 않는다.
- `EventReceived` handling은 orchestrator public surface 밖에 남는다.

## 7. Validation
Focused tests:
- `DashboardRefreshOrchestratorTests`: 7 passed

`dotnet build CAAutomationHub.sln`

Result:
- Succeeded
- Warning 0
- Error 0

`dotnet test CAAutomationHub.sln`

Result:
- Succeeded
- 169 passed
- 0 failed

## 8. Boundary Rules
- `DashboardViewModel`은 변경하지 않았다.
- `RuntimeDashboardAdapter`는 변경하지 않았다.
- `FakeDashboardRuntimeAdapter`는 변경하지 않았다.
- `IRuntimeDashboardAdapter`는 변경하지 않았다.
- `IAsyncRuntimeDashboardAdapter`는 변경하지 않았다.
- `IRuntimeSnapshotProvider`는 변경하지 않았다.
- UI는 변경하지 않았다.
- `DispatcherTimer`는 제거하지 않았다.
- `SnapshotChanged` actual push는 구현하지 않았다.
- `EventReceived` actual handling은 구현하지 않았다.
- Add/Edit/Delete runtime command conversion은 구현하지 않았다.
- Runtime integration은 추가하지 않았다.
- Supervisor implementation은 추가하지 않았다.
- `XgtDriverCore`는 참조하지 않았다.
- `XgtChannelRunner`는 참조하지 않았다.
- `FakePlc`는 참조하지 않았다.

## 9. Known Limitations / Notes
- AH-WPF-21 기준 `DashboardRefreshOrchestrator`는 아직 `DashboardViewModel`에 연결되어 있지 않다.
- 이 scenario는 policy skeleton과 unit test만 추가한다.
- Stale snapshot protection은 현재 timestamp comparison을 사용한다.
- 실제 Supervisor snapshot publication이 clock/revision ordering을 보장하지 못하면 revision 기반 정책을 검토해야 한다.
- Add/Edit/Delete 이후 pull refresh와 push snapshot ordering은 향후 ViewModel wiring scenario에서 재검증해야 한다.
- `EventReceived`는 향후 `RuntimeDashboardEventBridgeService`를 통해 별도로 유지하는 것이 적절하다.
- 이 문서는 AH-WPF-21 당시 Closeout 기준 기록이다. `DashboardViewModel` 연결은 이후 AH-WPF-22에서 수행되었다. 최신 기준은 `docs/context/WPF_RUNTIME_BRIDGE_CURRENT_STATE.md`를 우선한다.

## 10. Next Scenario Candidates
1. AH-WPF-22: DashboardViewModel Event Refresh Wiring Review
   - 기존 `DispatcherTimer`를 유지한다.
   - optional `IRuntimeDashboardEventSource`를 감지한다.
   - `SnapshotChanged`를 `DashboardRefreshOrchestrator`로 연결한다.
   - dispose 중 unsubscribe한다.
   - Add/Edit/Delete와 push ordering을 검토한다.

2. AH-WPF-23: Runtime Event Bridge Skeleton
   - `EventReceived`를 `RuntimeDashboardEvent`로 map한다.
   - `RuntimeDashboardEvent`를 `RuntimeEventLogItem`으로 변환한다.
   - rolling buffer와 UI marshal 정책을 검토한다.

3. AH-WPF-24: Runtime Snapshot Revision Policy
   - `SnapshotTime` 외 Revision 또는 Sequence 추가를 검토한다.
   - stale snapshot protection을 강화한다.

4. AH-RUNTIME-01: Supervisor Skeleton
   - `IAutomationHubSupervisor`를 도입한다.
   - in-memory/fake supervisor를 추가한다.
   - 실제 PLC는 연결하지 않는다.

Note:
- 위 Next Scenario Candidates는 AH-WPF-21 당시 후보 기록이다. 현재 번호 체계와 최신 다음 단계는 `docs/context/WPF_RUNTIME_BRIDGE_CURRENT_STATE.md`를 우선한다.
