# AH-WPF-20 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- Keep the stable synchronous `IRuntimeDashboardAdapter` contract.
- Add optional async / lifecycle / event interface skeletons for future Real Runtime integration.
- Add a minimal async snapshot bridge to `RuntimeDashboardAdapter`.
- Provide extension points without impacting `DashboardViewModel` or `FakeDashboardRuntimeAdapter`.

## 3. Implemented Scope
- Added `IAsyncRuntimeDashboardAdapter`.
- Added `IRuntimeDashboardLifecycle`.
- Added `IRuntimeDashboardEventSource`.
- Added `DashboardSnapshotChangedEventArgs`.
- Updated `RuntimeDashboardAdapter` to implement `IAsyncRuntimeDashboardAdapter`.
- Implemented `GetSnapshotAsync` as a synchronous-provider bridge.
- Left the existing `IRuntimeDashboardAdapter` unchanged.
- Excluded command executor interfaces.

## 4. Changed Files
- `src/CAAutomationHub.Wpf/Adapters/IAsyncRuntimeDashboardAdapter.cs`
- `src/CAAutomationHub.Wpf/Adapters/IRuntimeDashboardLifecycle.cs`
- `src/CAAutomationHub.Wpf/Adapters/IRuntimeDashboardEventSource.cs`
- `src/CAAutomationHub.Wpf/Adapters/RuntimeDashboardAdapter.cs`
- `src/CAAutomationHub.Wpf/Models/Dashboard/DashboardSnapshotChangedEventArgs.cs`
- `tests/CAAutomationHub.Wpf.Tests/Adapters/RuntimeDashboardAdapterOptionalInterfacesTests.cs`

## 5. Final Contracts

### 5.1 IRuntimeDashboardAdapter
- Existing contract preserved.
- Declares `DashboardSnapshot GetSnapshot()`.
- No async, lifecycle, or event members were added.

### 5.2 IAsyncRuntimeDashboardAdapter
- Declares `Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)`.
- Exists as a separate optional extension interface.

### 5.3 IRuntimeDashboardLifecycle
- Declares `Task StartAsync(CancellationToken cancellationToken)`.
- Declares `Task StopAsync(CancellationToken cancellationToken)`.
- `RuntimeDashboardAdapter` does not implement this interface in AH-WPF-20.

### 5.4 IRuntimeDashboardEventSource
- Declares `SnapshotChanged`.
- Declares `EventReceived`.
- Does not guarantee UI thread affinity.
- Dispatcher marshaling remains the responsibility of a future UI orchestration layer.

### 5.5 DashboardSnapshotChangedEventArgs
- Exposes `Snapshot`.
- Exposes `OccurredAt`.
- Guards against a null `Snapshot` with `ArgumentNullException`.

## 6. RuntimeDashboardAdapter Behavior
- Existing `GetSnapshot()` behavior is preserved.
- `RuntimeDashboardAdapter` now implements `IAsyncRuntimeDashboardAdapter`.
- `GetSnapshotAsync` behavior:
  - Calls `cancellationToken.ThrowIfCancellationRequested()`.
  - Returns `Task.FromResult(GetSnapshot())`.
  - Does not use `.Result`, `.Wait()`, or `Task.Run`.
- Current behavior is a bridge over the synchronous `IRuntimeSnapshotProvider`.
- A real async source should be reviewed during a future Runtime provider scenario.

## 7. Tests Added
Test file:
- `tests/CAAutomationHub.Wpf.Tests/Adapters/RuntimeDashboardAdapterOptionalInterfacesTests.cs`

Exact tests:
- `RuntimeDashboardAdapterContract_OnlyDeclaresGetSnapshot`
- `RuntimeDashboardAdapter_ImplementsAsyncOptionalInterface`
- `GetSnapshotAsync_ReturnsMappedDashboardSnapshot`
- `GetSnapshotAsync_ThrowsWhenTokenIsAlreadyCancelled`
- `DashboardSnapshotChangedEventArgs_PreservesSnapshotAndOccurredAt`
- `DashboardSnapshotChangedEventArgs_ThrowsWhenSnapshotIsNull`
- `RuntimeDashboardLifecycleContract_DeclaresStartAndStopAsync`
- `RuntimeDashboardEventSourceContract_DeclaresSnapshotAndEventEvents`
- `FakeDashboardRuntimeAdapter_IsNotForcedToImplementOptionalInterfaces`

Policies fixed by tests:
- Existing `IRuntimeDashboardAdapter` declares only `GetSnapshot()`.
- `RuntimeDashboardAdapter` implements `IAsyncRuntimeDashboardAdapter`.
- `GetSnapshotAsync` uses the existing mapper/provider flow.
- A pre-cancelled token throws `OperationCanceledException` before provider access.
- Lifecycle/event skeleton signatures are present.
- `FakeDashboardRuntimeAdapter` is not forced to implement optional interfaces.
- `DashboardSnapshotChangedEventArgs` preserves values and rejects null snapshots.

## 8. Validation
Targeted tests:
- `RuntimeDashboardAdapterOptionalInterfacesTests`: 9 passed

`dotnet build CAAutomationHub.sln`

Result:
- Succeeded
- Warning 0
- Error 0

`dotnet test CAAutomationHub.sln`

Result:
- Succeeded
- 162 passed
- 0 failed

## 9. Boundary Rules
- Existing `IRuntimeDashboardAdapter` was not changed.
- `DashboardViewModel` was not changed.
- `FakeDashboardRuntimeAdapter` was not changed.
- UI was not changed.
- Command executor was not added.
- `ExecuteAsync` was not implemented.
- `StartAsync` / `StopAsync` no-op implementations were not added.
- `SnapshotChanged` actual push was not implemented.
- `EventReceived` actual push was not implemented.
- Runtime / Supervisor / PLC integration was not added.
- `XgtDriverCore` was not referenced.
- `XgtChannelRunner` was not referenced.
- `FakePlc` was not referenced.
- Runtime telemetry was not implemented.
- `BalanceController` was not implemented.

## 10. Known Limitations / Notes
- `GetSnapshotAsync` is not real async I/O; it is currently a bridge over the synchronous provider.
- When a real Runtime/Supervisor provider exists, an async source or async provider contract should be reviewed again.
- `IRuntimeDashboardEventSource` only provides the event contract; no events are raised in AH-WPF-20.
- Adapter events are not guaranteed to be raised on the UI thread.
- Event-based refresh needs Dispatcher marshaling, reentrancy protection, and latest-only coalescing policy.
- Command executor introduction should wait until after a Supervisor `CommandDispatcher` scenario.

## 11. Next Scenario Candidates
1. AH-WPF-21: Runtime Dashboard Event/Refresh Orchestration Review
   - Review how `SnapshotChanged` can be safely connected to the UI.
   - Define Dispatcher marshal policy.
   - Define reentrancy policy.
   - Define latest-only coalescing policy.

2. AH-WPF-22: Runtime Provider / Supervisor Boundary Review
   - Review how `IRuntimeSnapshotProvider` should connect to `IAutomationHubSupervisor`.
   - Design a supervisor snapshot provider adapter.

3. AH-RUNTIME-01: Supervisor Skeleton
   - Introduce `IAutomationHubSupervisor`.
   - Add an in-memory/fake supervisor.
   - Do not connect real PLCs.

4. AH-RUNTIME-02: Runtime Command Executor Skeleton
   - Introduce `ExecuteAsync`.
   - Return `RuntimeDashboardCommandResult`.
   - Add `TestConnection` / `ResetConnection` / `ManualReconnect` skeletons.
