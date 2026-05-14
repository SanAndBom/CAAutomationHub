# AH-WPF-22 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- Connect an optional `SnapshotChanged` push refresh path to `DashboardViewModel`.
- Preserve the existing `DispatcherTimer` pull refresh path.
- Apply `DashboardRefreshOrchestrator` UI dispatcher marshal, coalescing, and stale snapshot protection policy.
- Verify push refresh wiring safety before any real Runtime connection.

## 3. Implemented Scope
- Detected `IRuntimeDashboardEventSource` in `DashboardViewModel` with `adapter as IRuntimeDashboardEventSource`.
- Subscribed to `SnapshotChanged` when the adapter implements the optional event source interface.
- Added a `SnapshotChanged` handler in `DashboardViewModel`.
- Connected `SnapshotChanged` payloads to `DashboardRefreshOrchestrator`.
- Added a test-oriented `IUiDispatcher` overload while preserving existing constructor compatibility.
- Used `WpfUiDispatcher` in the default production path.
- Called `MarkApplied(snapshot)` after `LoadSnapshot()` applies a pull snapshot.
- Added `DashboardRefreshOrchestrator.MarkApplied`.
- Unsubscribed from `SnapshotChanged` in `Dispose`.
- Left `EventReceived` unsubscribed and unhandled.
- Preserved the existing `DispatcherTimer` refresh.

## 4. Changed Files
- `src/CAAutomationHub.Wpf/ViewModels/DashboardViewModel.cs`
- `src/CAAutomationHub.Wpf/Services/DashboardRefreshOrchestrator.cs`
- `tests/CAAutomationHub.Wpf.Tests/Services/DashboardRefreshOrchestratorTests.cs`
- `tests/CAAutomationHub.Wpf.Tests/ViewModels/DashboardViewModelEventRefreshTests.cs`

## 5. Final Refresh Behavior

### 5.1 Pull Refresh
- `DispatcherTimer` 1-second refresh remains in place.
- `RefreshCommand` remains in place.
- Add/Edit/Delete still use the existing `LoadSnapshot` flow.
- `LoadSnapshot` applies the snapshot and then calls `MarkApplied(snapshot)`.

### 5.2 Push Refresh
- If the adapter implements `IRuntimeDashboardEventSource`, `DashboardViewModel` subscribes to `SnapshotChanged`.
- The `DashboardSnapshot` from the `SnapshotChanged` payload is used directly.
- The event handler does not call `GetSnapshot()` again.
- `DashboardRefreshOrchestrator` performs dispatcher marshal before applying the snapshot.
- The existing latest-only coalescing and reentrancy guard policy applies to push snapshots.

### 5.3 Stale Snapshot Protection
- Older push snapshots are ignored after a newer manual pull snapshot has already been applied.
- `MarkApplied` records the latest applied snapshot time.
- Stale snapshot detection uses `DashboardSnapshot.Health.SnapshotTime`.

### 5.4 Dispose
- Timer stop behavior is preserved.
- Timer tick unsubscribe behavior is preserved.
- `SnapshotChanged` unsubscribe was added.
- Push events raised after `Dispose` do not update the ViewModel.

### 5.5 EventReceived
- `EventReceived` is not handled in this scenario.
- Event Log bridge work remains separated into a future scenario.

## 6. Tests Added / Updated
- Event source adapters are subscribed through `SnapshotChanged`.
- `SnapshotChanged` payloads are reflected in `DashboardViewModel` state.
- Event handling does not call `GetSnapshot()` again.
- `RefreshCommand` pull refresh still works when an event source is present.
- `SnapshotChanged` after `Dispose` does not update the ViewModel.
- Handler count decreases after `Dispose`.
- `EventReceived` is not subscribed.
- An older push snapshot does not overwrite a newer manual pull snapshot.
- `DashboardRefreshOrchestrator.MarkApplied` protects against stale submitted snapshots.

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
- One WPF obj dll file lock collision occurred during an initial parallel targeted test run. Sequential reruns passed.

## 8. Boundary Rules
- Actual Runtime connection was not added.
- Supervisor implementation was not added.
- `IAutomationHubSupervisor` implementation was not added.
- Runtime project was not created.
- `XgtDriverCore` was not referenced.
- `XgtChannelRunner` was not referenced.
- `FakePlc` was not referenced.
- Actual PLC connection was not added.
- Runtime command execution was not added.
- Add/Edit/Delete runtime command conversion was not implemented.
- UI was not changed.
- Communication Trend was not changed.
- Mini Trend was not changed.
- Runtime telemetry was not implemented.
- `BalanceController` was not implemented.
- Timer was not removed.
- `EventReceived` was not handled.
- Event Log bridge was not implemented.

## 9. Known Limitations / Notes
- `RuntimeDashboardAdapter` does not yet implement `IRuntimeDashboardEventSource`.
- Actual push refresh is currently verified only through test doubles.
- `EventReceived` is not connected to Event Log yet.
- Stale snapshot detection uses `SnapshotTime`.
- If real Runtime snapshot clock or revision guarantees are weak, a revision-based stale policy should be reviewed later.
- Add/Edit/Delete and push snapshot ordering now have first-line protection through `MarkApplied`, but should be revalidated when the real Runtime connection is introduced.

## 10. Next Scenario Candidates
1. AH-WPF-23: Runtime Event Bridge Skeleton
   - `EventReceived` to `RuntimeDashboardEvent` to `RuntimeEventLogItem`.
   - Runtime event bridge service.
   - Rolling buffer and UI marshal policy review.

2. AH-WPF-24: RuntimeDashboardAdapter EventSource Skeleton
   - Review whether `RuntimeDashboardAdapter` should implement `IRuntimeDashboardEventSource`.
   - Add skeleton before provider/supervisor event wiring.

3. AH-WPF-25: Runtime Snapshot Revision Policy
   - Review adding Revision or Sequence beyond `SnapshotTime`.
   - Strengthen stale snapshot protection.

4. AH-RUNTIME-01: Supervisor Skeleton
   - Add `IAutomationHubSupervisor`.
   - Add an in-memory/fake supervisor.
   - Do not connect real PLCs.
