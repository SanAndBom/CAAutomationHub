# AH-WPF-23 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- Review the Supervisor boundary before the WPF Runtime Bridge connects to an actual Runtime/Supervisor.
- Decide which Supervisor boundary should be seen by `RuntimeDashboardAdapter`, `IRuntimeSnapshotProvider`, and `IRuntimeDashboardEventSource`.
- Confirm responsibility boundaries before entering the Runtime project skeleton scenario.

## 3. Current Structure Reviewed

### 3.1 IRuntimeSnapshotProvider
- `IRuntimeSnapshotProvider` is a WPF adapter-facing synchronous provider.
- Current contract:
  - `RuntimeSnapshot GetSnapshot()`

### 3.2 RuntimeDashboardAdapter
- `RuntimeDashboardAdapter` implements:
  - `IRuntimeDashboardAdapter`
  - `IAsyncRuntimeDashboardAdapter`
- Current snapshot flow:
  - Calls `IRuntimeSnapshotProvider.GetSnapshot()`.
  - Maps the returned `RuntimeSnapshot` through `RuntimeDashboardSnapshotMapper.Map(...)`.
  - Returns a `DashboardSnapshot`.
- Current async path wraps the synchronous path and is valid only before a real async Runtime boundary is connected.

### 3.3 IRuntimeDashboardEventSource
- `IRuntimeDashboardEventSource` currently declares:
  - `SnapshotChanged`
  - `EventReceived`
- `RuntimeDashboardAdapter` does not implement this interface yet.

### 3.4 Contracts
- Runtime contracts currently include:
  - `RuntimeSnapshot`
  - `RuntimeEvent`
  - `RuntimeDashboardCommand`
  - `RuntimeDashboardCommandResult`
- Runtime contracts do not currently include a snapshot revision or sequence number.

### 3.5 DashboardViewModel
- `DashboardViewModel` detects an optional `IRuntimeDashboardEventSource`.
- Only `SnapshotChanged` is connected to `DashboardRefreshOrchestrator`.
- `EventReceived` is not subscribed or handled.
- The existing `DispatcherTimer` pull refresh remains in place.

## 4. Supervisor Boundary Decision
- B option was accepted.
- A separate provider/bridge adapter will sit between Supervisor and the WPF adapter.
- `IAutomationHubSupervisor` will not directly implement the WPF `IRuntimeSnapshotProvider`.
- `RuntimeDashboardAdapter` should not deeply know `IAutomationHubSupervisor`.
- Supervisor remains the Runtime layer's async boundary.
- WPF `RuntimeDashboardAdapter` keeps the responsibility for converting Runtime snapshots into Dashboard snapshots.

## 5. IAutomationHubSupervisor Candidate Contract

Candidate methods:
- `Task StartAsync(CancellationToken cancellationToken)`
- `Task StopAsync(CancellationToken cancellationToken)`
- `Task<RuntimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)`
- `Task<RuntimeDashboardCommandResult> ExecuteAsync(RuntimeDashboardCommand command, CancellationToken cancellationToken)`

Candidate events:
- `event EventHandler<RuntimeSnapshotChangedEventArgs>? SnapshotChanged`
- `event EventHandler<RuntimeEvent>? RuntimeEventRaised`

Policy:
- Supervisor exposes `RuntimeSnapshot`, not `DashboardSnapshot`.
- Supervisor does not know Dashboard models.
- Supervisor exposes `RuntimeEvent`, not `RuntimeEventLogItem`.
- Supervisor does not know WPF event log models.
- Lifecycle responsibility can sit on Supervisor.
- Process hosting and DI lifetime can be separated later into a `RuntimeHost` if needed.

## 6. RuntimeSnapshotChangedEventArgs / Revision Policy
- `RuntimeSnapshotChangedEventArgs` is needed for the Runtime/Supervisor boundary.
- Candidate fields:
  - `RuntimeSnapshot Snapshot`
  - `DateTimeOffset OccurredAt`
  - `long? Revision`
- The `Snapshot` payload is required.
- `OccurredAt` represents the event raise time.
- `Revision` is useful long term, but adding it directly to `RuntimeSnapshot` or `DashboardSnapshot` is deferred.
- Current WPF stale protection uses `DashboardSnapshot.Health.SnapshotTime`.
- Runtime must keep `RuntimeSnapshot.CapturedAt` and `RuntimeHealthState.CapturedAt` consistent.

## 7. RuntimeDashboardAdapter Event Source Direction
- No event source implementation is added in this scenario.
- In a follow-up implementation, connecting only `SnapshotChanged` first is the safer path.
- The follow-up design should consider `RuntimeDashboardAdapter` subscribing to Supervisor `SnapshotChanged`, mapping the `RuntimeSnapshot` into `DashboardSnapshot`, and raising `DashboardSnapshotChangedEventArgs`.
- `EventReceived` remains deferred.
- Adapter events do not guarantee UI thread affinity.
- UI marshaling remains the responsibility of `DashboardRefreshOrchestrator` and `IUiDispatcher`.

## 8. Runtime Command Boundary
- `ExecuteAsync(RuntimeDashboardCommand, CancellationToken)` can be included in the `IAutomationHubSupervisor` candidate contract.
- `RuntimeDashboardAdapter` command executor exposure is not implemented yet.
- Add/Edit/Delete remain the responsibility of the current fake configuration service.
- Add/Edit/Delete should move to Runtime commands only after a Runtime config repository or Supervisor config boundary exists.
- `TestConnection`, `ResetConnection`, and `ManualReconnect` remain Runtime command candidates.

## 9. Runtime Project Strategy
- AH-WPF-23 does not create a Runtime project.
- The next step should proceed with the accepted B option.
- Next step candidates:
  - Create `src/CAAutomationHub.Runtime`.
  - Add `IAutomationHubSupervisor`.
  - Add `RuntimeSnapshotChangedEventArgs`.
- Still excluded in the next skeleton step:
  - `InMemoryAutomationHubSupervisor`
  - PLC connection
  - Driver or ChannelRunner connection

## 10. Sync/Async Snapshot Bridge Policy
- Sync-over-async is prohibited.
- Directly connecting an async Supervisor to the synchronous `IRuntimeSnapshotProvider` has blocking risk.
- After a real Runtime connection exists, `GetSnapshot()` should likely return the last successful cached `DashboardSnapshot`.
- The async path or Supervisor event can naturally update that cache.
- The current `Task.FromResult(GetSnapshot())` structure is valid only before the actual async Runtime connection.
- A follow-up candidate is adding `IAsyncRuntimeSnapshotProvider`.

## 11. Event Log Bridge Relationship
- Event bridge work remains separate from Snapshot bridge work.
- Snapshot refresh uses latest-only coalescing.
- Event log handling needs separate rolling buffer, ordering, duplicate removal, and coalescing policies.
- Expected future flow:
  - Supervisor `RuntimeEventRaised`
  - Adapter or bridge calls `RuntimeDashboardSnapshotMapper.MapEvent`
  - `IRuntimeDashboardEventSource.EventReceived`
  - `RuntimeDashboardEventBridgeService`
  - `RuntimeEventLogItemMapper`
  - `RealtimeEventLogViewModel` or `IEventStreamService`

## 12. Implementation Decision
- A option was accepted for AH-WPF-23.
- This scenario performed planning and boundary review only.
- No production code, test code, or project skeleton was changed as part of the design scenario.

Next step recommendation:
- B option.
- Create a Runtime project skeleton.
- Add `IAutomationHubSupervisor`.
- Add `RuntimeSnapshotChangedEventArgs`.
- Add no implementation, or only minimal interface skeleton.
- Do not connect actual PLCs.

## 13. Excluded Scope
- Actual Runtime implementation
- Supervisor implementation
- Runtime project creation
- `XgtDriverCore` reference
- `XgtChannelRunner` reference
- `FakePlc` reference
- Actual PLC connection
- Runtime command execution implementation
- Add/Edit/Delete Runtime command conversion
- UI changes
- Communication Trend changes
- Mini Trend changes
- `BalanceController` implementation
- `PollingScheduler` implementation

## 14. Risks / Notes
- The ordering source between `RuntimeSnapshot.CapturedAt` and `RuntimeHealthState.CapturedAt` must be clarified.
- Current WPF stale protection depends on mapped `Health.SnapshotTime`, which currently comes from `RuntimeHealthState.CapturedAt`.
- If `Revision` exists only on EventArgs, WPF stale protection remains time-based for now.
- Because `IRuntimeSnapshotProvider` is synchronous, directly connecting it to an async Supervisor risks blocking.
- `EventReceived` needs a different policy from `SnapshotChanged`, so it should not be mixed into the same orchestrator.

## 15. Next Scenario Candidates
1. AH-RUNTIME-01: Runtime Project + Supervisor Interface Skeleton
   - Create `CAAutomationHub.Runtime`.
   - Reference Contracts.
   - Add `IAutomationHubSupervisor`.
   - Add `RuntimeSnapshotChangedEventArgs`.
   - Do not connect actual PLCs.

2. AH-RUNTIME-02: Supervisor Runtime Snapshot Provider Bridge
   - Review `IAsyncRuntimeSnapshotProvider`.
   - Design the bridge that passes Supervisor snapshots into `RuntimeDashboardAdapter`.
   - Review sync `GetSnapshot()` cache policy.

3. AH-WPF-24: Runtime Event Bridge Skeleton
   - Connect `RuntimeEventRaised` to `RuntimeDashboardEvent` to `RuntimeEventLogItem`.
   - Define rolling buffer and UI marshal policy.

4. AH-RUNTIME-03: InMemory Supervisor Skeleton
   - Create `RuntimeSnapshot`.
   - Raise `SnapshotChanged`.
   - Do not connect actual PLCs.
