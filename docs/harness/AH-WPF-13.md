# AH-WPF-13 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- Redefine the PLC Card Mini Trend role.
- Prepare the PLC Card for future Runtime Service integration with a Current Sequence display.
- Extend the card toward a runtime-ready monitoring card without directly referencing Runtime internals.
- Separate the roles of the lower Communication Trend and the Card Mini Trend:
  - Lower Communication Trend: recent 30-minute communication RTT quality.
  - Card Mini Trend: recent 5-minute sequence response latency.

## 3. Implemented Scope
- Added `RuntimeSequenceStatus`.
- Added `PlcRuntimeSignalSnapshot`.
- Added `SequenceResponseLatencyBucket`.
- Included `RuntimeSignal` in `PlcCardSnapshot`.
- Added null-safe `PlcRuntimeSignalSnapshot.Empty` handling for `RuntimeSignal`.
- Generated state-based fake Runtime Signal data in `FakeDashboardRuntimeAdapter`.
- Mapped Healthy / Warning / Congested / Error / Inactive PLC states to distinct sequence/status values.
- Added a one-line Current Sequence display to each PLC Card.
- Added `CurrentSequenceText`, `CurrentSequenceStatusText`, and `CurrentSequenceElapsedText` ViewModel properties.
- Removed the previous static Mini Trend bars.
- Changed Mini Trend to a recent 5-minute sequence response latency bar chart.
- Implemented a 30-second x 10-bucket Mini Trend structure.
- Displayed start/completion response latency bars side by side in each bucket.
- Expanded the Mini Trend horizontal usage area.
- Added minimal Runtime Signal information to the Detail Pane.
- Added a GridSplitter for Communication Trend height adjustment.
- Added `DashboardLayoutSettings`.
- Added `IDashboardLayoutSettingsService`.
- Added `DashboardLayoutSettingsService`.
- Added LocalAppData `dashboard-layout.json` save/restore for dashboard layout height.
- Applied Trend height bounds:
  - Default: 300
  - Min: 220
  - Max: 420
- Added GridSplitter `DragStarted` / `DragDelta` / `DragCompleted` handling.
- Saved and restored the last Communication Trend height.

## 4. Changed Files
- `src/CAAutomationHub.Wpf/Adapters/FakeDashboardRuntimeAdapter.cs`
- `src/CAAutomationHub.Wpf/Controls/PlcStatusCard.xaml`
- `src/CAAutomationHub.Wpf/Controls/PlcDetailPane.xaml`
- `src/CAAutomationHub.Wpf/Models/Dashboard/PlcCardSnapshot.cs`
- `src/CAAutomationHub.Wpf/Models/Dashboard/PlcRuntimeSignalSnapshot.cs`
- `src/CAAutomationHub.Wpf/Models/Dashboard/RuntimeSequenceStatus.cs`
- `src/CAAutomationHub.Wpf/Models/Dashboard/SequenceResponseLatencyBucket.cs`
- `src/CAAutomationHub.Wpf/ViewModels/PlcStatusCardViewModel.cs`
- `src/CAAutomationHub.Wpf/Views/DashboardView.xaml`
- `src/CAAutomationHub.Wpf/Views/DashboardView.xaml.cs`
- `src/CAAutomationHub.Wpf/Models/Settings/DashboardLayoutSettings.cs`
- `src/CAAutomationHub.Wpf/Services/IDashboardLayoutSettingsService.cs`
- `src/CAAutomationHub.Wpf/Services/DashboardLayoutSettingsService.cs`
- `tests/CAAutomationHub.Wpf.Tests/ViewModels/DashboardRuntimeSignalTests.cs`
- `tests/CAAutomationHub.Wpf.Tests/Services/DashboardLayoutSettingsServiceTests.cs`
- `docs/harness/AH-WPF-13.md`

## 5. Final Behavior

### 5.1 Current Sequence
- The PLC Card displays the current Runtime Sequence in one line.
- Example displays:
  - `현재: 폴링`
  - `현재: DB조회 · 지연 12s`
  - `현재: 데이터전송 · 오류`
- Fake sequence/status display differs by PLC state.

### 5.2 Mini Trend
- Removed the previous static Mini Trend bars.
- Displays a recent 5-minute sequence response latency bar chart.
- Uses 10 buckets.
- Each bucket displays start/completion response latency bars.
- No legend is shown inside the card.
- `TX / RX` remains on the card.

### 5.3 Communication Trend Splitter
- A GridSplitter is displayed between the Card area and the Communication Trend area.
- Users can adjust the Communication Trend height with the mouse.
- The height remains after MouseUp.
- The last height is restored after app restart.
- Height is clamped to 220-420.

## 6. Validation
`dotnet build CAAutomationHub.sln`

Result:
- Success
- Warning 0
- Error 0

`dotnet test CAAutomationHub.sln`

Result:
- Success
- 121 passed

Manual verification:
- Current Sequence display confirmed.
- Mini Trend display confirmed.
- Mini Trend horizontal area expansion confirmed.
- `TX / RX` retention confirmed.
- Splitter adjustment confirmed.
- Height retention after MouseUp confirmed.
- Last height restore after app restart confirmed.
- Add/Edit/Delete behavior confirmed.
- Selection / re-click deselect / Drag Scroll / Shift Wheel behavior confirmed.

## 7. Tests Added / Updated
- Every Fake PLC card provides a non-null `RuntimeSignal`.
- RuntimeSignal creates 10 buckets.
- Healthy / Warning / Congested / Error / Inactive states map to sequence statuses.
- `PlcStatusCardViewModel.UpdateSnapshot` updates `CurrentSequenceText`.
- Add/Edit/Delete keeps or removes RuntimeSignal paths correctly.
- Snapshot refresh updates RuntimeSignal.
- Missing `DashboardLayoutSettings` file returns default height 300.
- Save then Load restores the saved height.
- Min/Max height clamp is applied.
- Broken JSON falls back to default.
- Save failure does not throw.

## 8. Boundary Rules
- No actual Runtime connection.
- No actual PLC connection.
- No FakePlc connection.
- No `XgtDriverCore` reference.
- No `XgtChannelRunner` reference.
- No DB persistence.
- JSON persistence is limited to layout height settings only.
- No Runtime Channel creation/deletion.
- No actual sequence execution.
- No actual DB query/data transfer integration.
- No direct Flow Runtime reference.
- No Event Log and Sequence integration.
- No large Dashboard layout redesign.
- No Mini Trend legend added.
- `TX / RX` was not removed.

## 9. Known Limitations / Notes
- Current Sequence and Mini Trend are based on Fake Runtime Signal prototype data.
- When actual Runtime is connected, sequence/status/elapsed calculation must be finalized in `RuntimeDashboardAdapter`.
- Mini Trend response latency must be interpreted as request-to-response latency, not as definitive DB processing time.
- Card height remains 420.
- Slight card height expansion and card exterior spacing cleanup remain candidates for later UI polish.
- Splitter hit area is currently verified as working, but it may be adjusted from 8px to 10px if needed.
- The Trend height settings file is stored under LocalAppData.
- If the settings file is missing or broken, the default height is used.

## 10. Next Scenario Candidates
1. AH-WPF-14: PLC Card Layout Polish
   - Review slight card height expansion from 420 to 440.
   - Clean up card exterior spacing.
   - Further tune Mini Trend height/readability.
   - Defer `TX / RX` retention/removal decisions.

2. AH-WPF-15: Mini Trend Semantics Review
   - Validate start/completion bar meaning.
   - Define sequence response latency calculation before actual Runtime connection.
   - Improve Detail Pane explanation.

3. AH-WPF-16: Trend Refactor Review
   - Review accumulated Trend code from AH-WPF-09/13.
   - Clean up `TrendRenderControl`, Fake Trend generation, and Runtime Signal model boundaries.

4. AH-WPF-17: Runtime Adapter Contract Review
   - Review the Fake Snapshot contract before connecting it to the real `RuntimeDashboardAdapter`.
