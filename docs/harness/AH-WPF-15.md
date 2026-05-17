# AH-WPF-15 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- Confirm the contract direction before moving from Fake Dashboard to the Real Runtime Bridge.
- Review the relationship between `RuntimeDashboardAdapter`, Supervisor, `RuntimeSnapshot`, and `DashboardSnapshot`.
- Design the WPF-to-Real-Runtime boundary before any actual Runtime connection work.

## 3. Reviewed Current Structure
- `IRuntimeDashboardAdapter` currently exposes one synchronous snapshot method:
  - `DashboardSnapshot GetSnapshot()`
- `RuntimeDashboardAdapter` is still a skeleton.
  - It does not connect to the real Runtime.
  - It returns an empty, null-safe `DashboardSnapshot`.
- `DashboardSnapshot` / `PlcCardSnapshot` are WPF display DTOs.
  - `DashboardSnapshot` contains health, PLC cards, and communication trend data.
  - `PlcCardSnapshot` contains card display fields, current UI state, traffic counters, error count, and runtime signal data.
- `FakeDashboardRuntimeAdapter` remains the active fake runtime source.
  - It also currently implements `IPlcDashboardConfigurationService`.
  - AH-WPF-14 split fake trend generation into `FakeCommunicationTrendFactory`.
  - AH-WPF-14 split fake runtime signal / mini trend generation into `FakeRuntimeSignalFactory`.
- `DashboardViewModel` refreshes through a `DispatcherTimer`.
  - The refresh interval is 1 second.
  - Each tick calls `adapter.GetSnapshot()`.
- Current project structure:
  - `CAAutomationHub.Wpf`
  - `CAAutomationHub.Wpf.Tests`
  - No `CAAutomationHub.Contracts` project yet.
  - No `CAAutomationHub.Runtime` project yet.

## 4. Accepted Runtime Bridge Direction
Accepted target flow:

```text
WPF UI
-> RuntimeDashboardAdapter
-> AutomationHubSupervisor
-> ChannelRegistry / PollingScheduler / RuntimeStateStore
-> PlcChannel
-> IXgtSession
-> ITransport
-> PLC
```

Accepted principles:
- WPF UI does not know `PlcChannel`.
- WPF UI does not know `XgtSession`.
- WPF UI does not know `TcpTransport`.
- WPF UI does not know XGT Protocol DTOs.
- WPF UI consumes only UI DTOs such as `DashboardSnapshot`, `PlcCardSnapshot`, and `RuntimeDashboardEvent`.
- `RuntimeDashboardAdapter` is a translator, not the runtime connection owner.
- Supervisor is the Runtime Control Plane.

## 5. RuntimeSnapshot / DashboardSnapshot Decision
Accepted decision:
- `RuntimeSnapshot` and `DashboardSnapshot` are separate models.
- `RuntimeSnapshot` is a Runtime internal state DTO.
- `DashboardSnapshot` is a WPF display DTO.
- `RuntimeDashboardAdapter` or `DashboardSnapshotMapper` translates `RuntimeSnapshot` into `DashboardSnapshot`.
- Option A, putting Runtime internal state directly into `DashboardSnapshot`, is rejected.
- Existing `DashboardSnapshot` DTOs stay in WPF for now.

`RuntimeSnapshot` minimum field candidates:
- `CapturedAt`
- `Channels`
- `Health`
- `RecentEvents`
- optional `Telemetry`

`ChannelRuntimeState` minimum field candidates:
- `PlcId`
- `PlcName`
- `LineName`
- `IsEnabled`
- `Endpoint` / `IpAddress` / `Port`
- `LinkState`
- `HealthSeverity`
- `PollingState`
- `SequenceState`
- `ConfiguredPollingIntervalMs`
- `EffectivePollingIntervalMs`
- `LastResponseMs`
- `ConsecutiveFailures`
- `LastSuccessAt`
- `LastFailureAt`
- `LastError`

## 6. State Model Decision
Accepted enum direction:

`PlcLinkState`
- `Offline`
- `Connecting`
- `Online`
- `Reconnecting`
- `Faulted`

`PlcHealthSeverity`
- `Healthy`
- `Warning`
- `Congested`
- `Error`
- `Inactive`

`PlcPollingState`
- `Idle`
- `Polling`
- `Delayed`
- `Suspended`
- `Resetting`

`RuntimeSequenceState` or `PlcSequenceState`
- `Idle`
- `Running`
- `Waiting`
- `Delayed`
- `Failed`
- `Completed`

Accepted decision:
- The current WPF `PlcConnectionState` stays for now.
- In practice, current `PlcConnectionState` is treated as a UI health severity state.
- A mapper will later define `PlcHealthSeverity` -> `PlcConnectionState`.

## 7. RuntimeDashboardAdapter Contract Direction
Candidate contract members:
- `StartAsync`
- `StopAsync`
- `GetSnapshotAsync`
- `SnapshotChanged`
- `EventReceived`
- `ExecuteAsync`

Accepted direction:
- Existing `GetSnapshot()` may remain for compatibility during the transition.
- Async, event, and command contracts will be considered as skeleton work in the next implementation stages.
- `DashboardViewModel` will not move directly to event-only refresh yet.
- The current `DispatcherTimer` refresh can remain while event-based refresh is introduced gradually.
- Avoid pushing raw `TelemetryReceived` events directly into the UI.
- Telemetry should be coalesced into Snapshot / Trend data before reaching WPF.

## 8. Supervisor Interface Direction
Recommended initial shape:
- Start with one `IAutomationHubSupervisor` interface.

Candidate members:
- `StartAsync`
- `StopAsync`
- `GetSnapshotAsync`
- `ExecuteAsync`
- `SnapshotChanged`
- `RuntimeEventRaised`

Accepted decision:
- Do not split immediately into `IRuntimeStateProvider`, `IRuntimeCommandGateway`, and `IRuntimeEventProvider`.
- Keep method names and responsibilities shaped so state, command, and event responsibilities can be split later if the interface grows.

## 9. Contracts / Runtime Project Direction
Accepted decision:
- Do not move existing WPF DTOs immediately.
- In the next implementation stage, create `CAAutomationHub.Contracts`.
- Add new Runtime contracts first:
  - `RuntimeSnapshot`
  - `ChannelRuntimeState`
  - Runtime state enums
  - command/result/event contracts
- Keep existing `DashboardSnapshot` DTOs in WPF for now.
- Consider `CAAutomationHub.Runtime` only after the Contracts layer stabilizes.

## 10. Command / Event / Telemetry Direction
`RuntimeDashboardCommand` candidates:
- `TestConnectionCommand`
- `AddOrUpdatePlcCommand`
- `DeletePlcCommand`
- `StartPlcCommand`
- `StopPlcCommand`
- `ResetConnectionCommand`
- `ManualReconnectCommand`

`RuntimeDashboardCommandResult` candidates:
- `CommandId`
- `Success`
- `Status`
- `Message`
- `PlcId`
- `ErrorCode`
- `OccurredAt`

Event direction:
- Keep `RuntimeEvent` and `RuntimeDashboardEvent` separate.
- `RuntimeEvent` is the Runtime-origin event.
- `RuntimeDashboardEvent` is the UI event/log DTO.
- `RuntimeDashboardEvent` may need an `EventId` field.
- Rolling buffers should be separated:
  - Runtime source buffer for real events.
  - UI visible buffer for filtering and display.

Telemetry direction:
- Runtime owns raw telemetry buffer or compacted telemetry.
- Adapter/mapper translates telemetry into:
  - Recent 30-minute communication trend.
  - Recent 5-minute card mini trend.
- Current UI trend DTO shape can remain for the first bridge stages.

## 11. Implementation Recommendation
Accepted AH-WPF-15 implementation scope:
- Option A accepted.
- AH-WPF-15 performs planning and contract review only.
- Implementation is separated into the next scenario.

Recommended next implementation:
- AH-WPF-16: Contracts Runtime Skeleton

Recommended AH-WPF-16 scope:
- Create `CAAutomationHub.Contracts`.
- Add `RuntimeSnapshot`.
- Add `ChannelRuntimeState`.
- Add `RuntimeHealthState`.
- Add `RuntimeEvent`.
- Add `RuntimeDashboardCommand`.
- Add `RuntimeDashboardCommandResult`.
- Add Runtime state enums.
- Do not move existing `DashboardSnapshot` DTOs.
- Do not connect the real Runtime.

## 12. Excluded Scope
Excluded from AH-WPF-15:
- Real Runtime connection.
- Real PLC connection.
- FakePlc connection.
- `XgtDriverCore` reference.
- `XgtChannelRunner` reference.
- Real `PollingScheduler` implementation.
- Real `ChannelRegistry` implementation.
- Real `PlcChannel` implementation.
- Real `XgtSession` implementation.
- DB persistence.
- JSON settings expansion.
- Windows Service split.
- Real Write / BulkWrite / ACK commands.
- `BalanceController` policy application.
- UI layout changes.
- Card / Trend / Dialog UI changes.

## 13. Risks / Notes
- `PlcConnectionState` may cause long-term naming confusion because it currently acts like `HealthSeverity`.
- `FakeDashboardRuntimeAdapter` implements both adapter and configuration service responsibilities, so real command transition needs a separation point.
- `FakeEventStreamService` emits `RuntimeEventLogItem` directly, which differs from the future real `RuntimeEvent` -> `RuntimeDashboardEvent` -> UI log flow.
- Moving dashboard DTOs into Contracts immediately would create broad test impact.
- Separating new Runtime contracts first is the safer path.

## 14. Next Scenario Candidates
1. AH-WPF-16: Contracts Runtime Skeleton
   - Create `CAAutomationHub.Contracts`.
   - Add `RuntimeSnapshot` / `ChannelRuntimeState` / Runtime enums / command result skeleton.
   - Do not move existing WPF DTOs.

2. AH-WPF-17: RuntimeSnapshot to DashboardSnapshot Mapper Skeleton
   - Add `RuntimeSnapshot` -> `DashboardSnapshot` mapping tests.
   - Add `PlcHealthSeverity` -> `PlcConnectionState` mapping.
   - Define LinkState / PollingState / SequenceState display conversion policies.

3. AH-WPF-18: RuntimeDashboardAdapter Async/Event Skeleton
   - Add `StartAsync` / `StopAsync` / `GetSnapshotAsync`.
   - Add `SnapshotChanged` / `EventReceived`.
   - Preserve existing `GetSnapshot` compatibility.

4. AH-RUNTIME-01: Supervisor Skeleton
   - Add `IAutomationHubSupervisor`.
   - Add `FakeAutomationHubSupervisor` or in-memory supervisor.
   - Do not connect a real PLC.
