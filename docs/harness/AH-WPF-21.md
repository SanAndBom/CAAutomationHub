# AH-WPF-21 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- Secure a safe orchestration unit before wiring Runtime push refresh directly into `DashboardViewModel`.
- Verify UI thread marshal, coalescing, and reentrancy policy for future `SnapshotChanged` event handling.
- Prepare for later event-based refresh while preserving the existing `DispatcherTimer` refresh model.

## 3. Implemented Scope
- Added `IUiDispatcher`.
- Added `WpfUiDispatcher`.
- Added `DashboardRefreshOrchestrator`.
- Added `SubmitSnapshot(DashboardSnapshot snapshot)`.
- Implemented latest-only coalescing.
- Prevented duplicate dispatcher apply scheduling.
- Preserved a pending snapshot when a snapshot is submitted during apply.
- Scheduled one more dispatcher pass after apply when a pending snapshot remains.
- Implemented stale snapshot protection using `DashboardSnapshot.Health.SnapshotTime`.
- Kept `EventReceived` outside the orchestrator scope.

## 4. Changed Files
- `src/CAAutomationHub.Wpf/Services/IUiDispatcher.cs`
- `src/CAAutomationHub.Wpf/Services/WpfUiDispatcher.cs`
- `src/CAAutomationHub.Wpf/Services/DashboardRefreshOrchestrator.cs`
- `tests/CAAutomationHub.Wpf.Tests/Services/DashboardRefreshOrchestratorTests.cs`

## 5. Orchestration Policy

### 5.1 Dispatcher Marshal
- The orchestrator does not depend directly on the WPF `Dispatcher`.
- UI apply work is scheduled through `IUiDispatcher.Post(Action)`.
- `WpfUiDispatcher` wraps `Dispatcher.BeginInvoke`.
- Actual `DashboardViewModel` wiring remains a future scenario.

### 5.2 Latest-only Coalescing
- When multiple snapshots arrive in a short window, only the last accepted snapshot is retained.
- If a dispatcher apply is already scheduled, the orchestrator does not schedule a duplicate apply.
- Dashboard snapshot UI follows the policy that the latest state matters more than every intermediate state.

### 5.3 Reentrancy Guard
- If a new snapshot is submitted while apply is running, it is retained as pending.
- After the active apply completes, the orchestrator schedules one more dispatcher pass when a pending snapshot exists.

### 5.4 Stale Snapshot Policy
- Stale detection uses `DashboardSnapshot.Health.SnapshotTime`.
- Older snapshots do not overwrite newer snapshots.
- If timestamps are equal, the last submitted snapshot wins.

### 5.5 EventReceived Separation
- `EventReceived` uses a different policy from snapshot refresh.
- Snapshot refresh is latest-only.
- Runtime events should be handled later through a runtime event bridge and rolling buffer policy.
- `IEventStreamService` and `RuntimeEventLogItemMapper` were not connected in this scenario.

## 6. Tests Added
Test file:
- `tests/CAAutomationHub.Wpf.Tests/Services/DashboardRefreshOrchestratorTests.cs`

Policies fixed by tests:
- `SubmitSnapshot(null)` throws `ArgumentNullException`.
- Snapshot submit calls dispatcher `Post`.
- Consecutive submits are coalesced into one dispatcher reservation.
- Dispatcher flush applies only the last snapshot.
- A snapshot submitted during apply is applied in a second dispatcher pass.
- An older snapshot does not overwrite a newer snapshot.
- `EventReceived` handling remains outside the orchestrator public surface.

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
- `DashboardViewModel` was not changed.
- `RuntimeDashboardAdapter` was not changed.
- `FakeDashboardRuntimeAdapter` was not changed.
- `IRuntimeDashboardAdapter` was not changed.
- `IAsyncRuntimeDashboardAdapter` was not changed.
- `IRuntimeSnapshotProvider` was not changed.
- UI was not changed.
- `DispatcherTimer` was not removed.
- `SnapshotChanged` actual push was not implemented.
- `EventReceived` actual handling was not implemented.
- Add/Edit/Delete runtime command conversion was not implemented.
- Runtime integration was not added.
- Supervisor implementation was not added.
- `XgtDriverCore` was not referenced.
- `XgtChannelRunner` was not referenced.
- `FakePlc` was not referenced.

## 9. Known Limitations / Notes
- `DashboardRefreshOrchestrator` is not wired into `DashboardViewModel` yet.
- This scenario only adds the policy skeleton and unit tests.
- Stale snapshot protection currently uses timestamp comparison.
- If real Supervisor snapshot publication cannot guarantee clock/revision ordering, a revision-based policy should be reviewed.
- Pull refresh and push snapshot ordering after Add/Edit/Delete must be revalidated during the future ViewModel wiring scenario.
- `EventReceived` should likely remain separate through a future `RuntimeDashboardEventBridgeService`.

## 10. Next Scenario Candidates
1. AH-WPF-22: DashboardViewModel Event Refresh Wiring Review
   - Keep the existing `DispatcherTimer`.
   - Detect optional `IRuntimeDashboardEventSource`.
   - Connect `SnapshotChanged` through `DashboardRefreshOrchestrator`.
   - Unsubscribe during dispose.
   - Review Add/Edit/Delete and push ordering.

2. AH-WPF-23: Runtime Event Bridge Skeleton
   - Map `EventReceived` to `RuntimeDashboardEvent`.
   - Convert `RuntimeDashboardEvent` to `RuntimeEventLogItem`.
   - Review rolling buffer and UI marshal policy.

3. AH-WPF-24: Runtime Snapshot Revision Policy
   - Review adding Revision or Sequence beyond `SnapshotTime`.
   - Strengthen stale snapshot protection.

4. AH-RUNTIME-01: Supervisor Skeleton
   - Introduce `IAutomationHubSupervisor`.
   - Add an in-memory/fake supervisor.
   - Do not connect real PLCs.
