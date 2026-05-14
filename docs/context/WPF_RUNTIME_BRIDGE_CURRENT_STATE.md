# WPF Runtime Bridge Current State

## 1. Purpose
- This document summarizes the current state of the CAAutomationHub WPF Dashboard just before it moves from the Fake-based prototype into the Real Runtime Bridge.
- It is a state anchor document for restoring the current design center when switching chat windows or entering the next Runtime work.

## 2. Current Milestone
- AH-WPF-09: Communication Trend Prototype complete
- AH-WPF-10: PLC Card Edit/Delete complete
- AH-WPF-11: PLC Add Actual Apply complete
- AH-WPF-12: PlcEditorDialog Validation complete
- AH-WPF-13: PLC Card Runtime Signal / Mini Trend complete
- AH-WPF-14: Dashboard Source Refactor complete
- AH-WPF-15: Runtime Bridge Contract Review complete
- AH-WPF-16: Contracts Runtime Skeleton complete
- AH-WPF-17: RuntimeSnapshot to DashboardSnapshot Mapper complete
- AH-WPF-18: RuntimeDashboardAdapter Provider Skeleton complete
- AH-WPF-19: Async/Event Contract Review complete
- AH-WPF-20: Optional Runtime Adapter Interfaces complete
- AH-WPF-21: Dashboard Refresh Orchestrator complete
- AH-WPF-22: DashboardViewModel Event Refresh Wiring complete
- AH-WPF-23: Supervisor Boundary Review complete

## 3. Current Architecture Summary

Current WPF dashboard snapshot flow:

```text
WPF UI
-> DashboardViewModel
-> IRuntimeDashboardAdapter
-> RuntimeDashboardAdapter
-> IRuntimeSnapshotProvider
-> RuntimeDashboardSnapshotMapper
-> DashboardSnapshot
```

Real Runtime direction:

```text
RuntimeDashboardAdapter
-> Provider / Bridge
-> IAutomationHubSupervisor
-> RuntimeSnapshot
-> ChannelRuntimeState
-> Channel / Session / Transport / PLC
```

## 4. Current WPF Dashboard Capabilities
- PLC Card display
- Add/Edit/Delete Fake configuration apply
- PlcEditorDialog validation
- Detail Pane
- Communication Trend
  - Overview: per-PLC RTT overlap
  - Selected: selected PLC single RTT trend
- Card Runtime Signal
  - Current Sequence
  - Last 5 minutes sequence response latency Mini Trend
- Realtime Event Log Prototype
- Layout settings
  - Communication Trend height splitter
  - LocalAppData save/restore
- Push refresh preparation
  - `IRuntimeDashboardEventSource`
  - `DashboardRefreshOrchestrator`
  - `DashboardViewModel` `SnapshotChanged` wiring

## 5. Runtime Bridge Contracts

Contracts project:
- `CAAutomationHub.Contracts`

Runtime DTO:
- `RuntimeSnapshot`
- `ChannelRuntimeState`
- `RuntimeHealthState`
- `RuntimeEvent`
- `RuntimeDashboardCommand`
- `RuntimeDashboardCommandResult`

Runtime states:
- `PlcLinkState`
- `PlcHealthSeverity`
- `PlcPollingState`
- `RuntimeSequenceState`

Mapping:
- `RuntimeDashboardSnapshotMapper`
- `RuntimeSnapshot` -> `DashboardSnapshot`
- `RuntimeHealthState` -> `RuntimeHealthSnapshot`
- `ChannelRuntimeState` -> `PlcCardSnapshot`
- `PlcHealthSeverity` -> `PlcConnectionState`
- `RuntimeEvent` -> `RuntimeDashboardEvent`

## 6. Confirmed Boundary Rules
- WPF UI does not know `PlcChannel`.
- WPF UI does not know `XgtSession`.
- WPF UI does not know `TcpTransport`.
- WPF UI does not know XGT Protocol DTOs.
- WPF UI consumes only UI DTOs such as `DashboardSnapshot`, `PlcCardSnapshot`, and `RuntimeDashboardEvent`.
- `RuntimeSnapshot` and `DashboardSnapshot` are separated.
- `RuntimeDashboardAdapter` or Mapper translates `RuntimeSnapshot` into `DashboardSnapshot`.
- `RuntimeDashboardAdapter` sees only the Supervisor or Provider boundary.
- Supervisor is the Runtime Control Plane.
- Channel owns connection, request, and recovery for one PLC.
- Session owns XGT request/response serialization.
- DriverCore owns XGT protocol, frame, and Transport.

## 7. Current Refresh Strategy

Pull:
- `DispatcherTimer` 1 second
- `adapter.GetSnapshot()`
- `ApplySnapshot()`

Push:
- `IRuntimeDashboardEventSource.SnapshotChanged`
- `DashboardRefreshOrchestrator`
- latest-only coalescing
- UI dispatcher marshal
- `ApplySnapshot()`

Important:
- Timer refresh remains in place.
- `SnapshotChanged` is a fast apply support path.
- `EventReceived` is not connected yet.
- `EventReceived` remains a separate Runtime Event Bridge candidate.

## 8. Supervisor Boundary Decision
- A separate provider/bridge adapter sits between Supervisor and the WPF Adapter.
- `IAutomationHubSupervisor` does not directly implement the WPF `IRuntimeSnapshotProvider`.
- `RuntimeDashboardAdapter` should not deeply know `IAutomationHubSupervisor`.
- Sync-over-async is prohibited.
- After a real Runtime connection exists, `GetSnapshot()` should likely return the last successful cached `DashboardSnapshot`.
- `RuntimeSnapshotChangedEventArgs` is needed.
- `Revision` is useful long term, but is not added directly to `RuntimeSnapshot` or `DashboardSnapshot` yet.

## 9. Known Gaps Before Real Runtime
- No `CAAutomationHub.Runtime` project
- No `IAutomationHubSupervisor`
- No `RuntimeSnapshotChangedEventArgs`
- No Supervisor implementation
- No RuntimeSnapshot provider bridge
- No Runtime Event Bridge
- No Runtime telemetry contract
- No `XgtDriverCore` / `XgtChannelRunner` connection
- No actual PLC connection
- No `PollingScheduler`
- No `BalanceController`
- No Runtime command execution

## 10. Next Recommended Steps
1. AH-RUNTIME-01: Runtime Project + Supervisor Interface Skeleton
   - Create `CAAutomationHub.Runtime`.
   - Reference Contracts.
   - Add `IAutomationHubSupervisor`.
   - Add `RuntimeSnapshotChangedEventArgs`.
   - Do not connect actual PLCs.

2. AH-RUNTIME-02: Supervisor Runtime Snapshot Provider Bridge
   - Design the bridge that passes Supervisor snapshots into `RuntimeDashboardAdapter`.
   - Review `IAsyncRuntimeSnapshotProvider`.
   - Review sync `GetSnapshot()` cache policy.

3. AH-WPF-24 or AH-RUNTIME-03: Runtime Event Bridge Skeleton
   - Connect `RuntimeEventRaised` to `RuntimeDashboardEvent` to `RuntimeEventLogItem`.
   - Define rolling buffer and UI marshal policy.

4. AH-RUNTIME-04: InMemory Supervisor Skeleton
   - Create `RuntimeSnapshot`.
   - Raise `SnapshotChanged`.
   - Do not connect actual PLCs.

5. AH-RUNTIME-05 and later:
   - `ChannelRegistry`
   - `XgtSession` / `XgtDriverCore` connection
   - `TestConnection` / `HealthProbe`
   - `PollingScheduler`
   - `TelemetryBuffer`
   - Recovery policy
   - Balance policy

## 11. Current Git Anchor
- AH-WPF-22 commit: `fb7bf1b`
- AH-WPF-23 closeout pending commit
- Working tree should contain:
  - `docs/harness/AH-WPF-23.md`
  - `docs/context/WPF_RUNTIME_BRIDGE_CURRENT_STATE.md`

## 12. Notes
- This document is a latest-state summary. For detailed implementation history, see `docs/harness/AH-WPF-xx.md`.
- If this document conflicts with older design documents, treat this document as the latest current-state anchor.
- This document can be updated periodically after Runtime implementation begins.
