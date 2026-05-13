# AH-WPF-14 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- Review the WPF Dashboard source structure before Runtime work.
- Clean up accumulated Fake Dashboard / Trend / RuntimeSignal responsibilities.
- Stabilize the structure with a small behavior-preserving refactor before Runtime integration.

## 3. Implemented Scope
- Reduced `FakeDashboardRuntimeAdapter` responsibilities.
- Added `FakeCommunicationTrendFactory`.
- Added `FakeRuntimeSignalFactory`.
- Separated fake RTT Trend generation responsibility.
- Separated fake RuntimeSignal / MiniTrend bucket generation responsibility.
- Split `DashboardViewModel.ApplySnapshot` into private helpers.
- Removed clearly unused private helpers from `TrendRenderControl`.

## 4. Changed Files
- `src/CAAutomationHub.Wpf/Adapters/FakeDashboardRuntimeAdapter.cs`
- `src/CAAutomationHub.Wpf/Adapters/FakeCommunicationTrendFactory.cs`
- `src/CAAutomationHub.Wpf/Adapters/FakeRuntimeSignalFactory.cs`
- `src/CAAutomationHub.Wpf/ViewModels/DashboardViewModel.cs`
- `src/CAAutomationHub.Wpf/Controls/TrendRenderControl.cs`

## 5. Refactor Details

### 5.1 FakeDashboardRuntimeAdapter
- Refocused the class around configuration and adapter responsibilities.
- Preserved Add/Edit/Delete public behavior.
- Preserved Snapshot generation behavior.
- Delegated Trend and RuntimeSignal generation to factories.

### 5.2 FakeCommunicationTrendFactory
- Generates `CommunicationTrendSetSnapshot`.
- Generates Overview and PLC trends.
- Preserves series, threshold, marker, and worst-series policies.

### 5.3 FakeRuntimeSignalFactory
- Generates `PlcRuntimeSignalSnapshot`.
- Preserves state-based sequence/status mapping.
- Preserves the 30-second x 10-bucket Mini Trend data policy.

### 5.4 DashboardViewModel
`ApplySnapshot` was split into private helpers:
- `ApplyTrend`
- `RemoveMissingCards`
- `ClearSelectionIfMissing`
- `MergeOrUpdateCards`
- `SyncSelectedPlcAfterSnapshot`
- `ApplyHealth`

### 5.5 TrendRenderControl
Removed clearly unused private helpers:
- `DrawWorstSeriesHighlight`
- `DrawResponseLine`
- `DrawMarkers`
- `GetMarkerIndexes`

## 6. Behavior Preserved
- Add/Edit/Delete snapshot and trend consistency.
- Selection and re-click deselect.
- Return to Overview when a selected card is deleted.
- Fake communication trend threshold / series / worst policy.
- RuntimeSignal 10-bucket generation.
- State-based `RuntimeSequenceStatus` mapping.
- Trend rendering priority / threshold / style policy.
- Communication Trend UI behavior.
- Card Runtime Signal / Mini Trend display.
- GridSplitter layout settings save/restore.

## 7. Validation
Targeted tests:
- Dashboard/fake targeted tests: 39 passed.
- Trend/dashboard targeted tests: 48 passed.

`dotnet build CAAutomationHub.sln`

Result:
- Success
- Warning 0
- Error 0

`dotnet test CAAutomationHub.sln`

Result:
- Success
- 121 passed

## 8. Boundary Rules
- No real Runtime connection.
- No FakePlc reference.
- No `XgtDriverCore` reference.
- No `XgtChannelRunner` reference.
- No external Chart library added.
- No Snapshot contract changes.
- No UI changes.
- No Dialog validation structure changes.
- No Card layout changes.
- No Mini Trend UI redesign.
- No Communication Trend UI redesign.

## 9. Known Limitations / Notes
- Fake factories currently use an `internal static` structure.
- Policies are verified through existing Adapter/ViewModel tests rather than direct factory unit tests.
- If fake generation policy grows further, `InternalsVisibleTo` or service-based factories can be considered.
- Large `TrendRenderControl` structure splitting remains deferred.
- Card layout polish should be judged again after Runtime connection work.

## 10. Next Scenario Candidates
1. AH-WPF-15: RuntimeDashboardAdapter Contract Review
   - Review boundaries before connecting the Fake Snapshot contract to actual Runtime state.
   - Design how Runtime / Channel / Driver state should be translated into Dashboard Snapshots.

2. AH-WPF-16: RuntimeDashboardAdapter Skeleton Extension
   - Add candidate provider interfaces for real Runtime state.
   - Extend adapter boundaries without connecting to actual PLCs yet.

3. AH-WPF-17: Trend / RuntimeSignal Mapping Review
   - Review how real Runtime event/channel state maps to Trend and RuntimeSignal.

4. AH-WPF-18: Real Runtime Integration Pilot
   - First pilot connecting part of XgtChannelRunner / Runtime state to Dashboard Snapshots.
