# AH-PILOT-LIVE-12 Closeout - WorkStart Scenario UI Observation Path

## 1. Summary

AH-PILOT-LIVE-12는 WorkStart full scenario를 WPF에서 더 직접적으로 관찰할 수 있도록 PilotPollingView에 scenario observation status line을 추가한 작업이다.

관찰 대상 흐름:

```text
WorkStart Request ON
-> LOT ID read
-> DB/fake DB query
-> PLC bulk write
-> Start ACK ON
-> WorkStart Request OFF
-> Start ACK OFF
```

변경 내용:

- `PilotPollingViewModel`에 `ScenarioObservation` 표시 속성을 추가했다.
- WorkStart일 때 `WorkStart <Status> Start ACK: <state>` 형태로 ACK ON/OFF 관찰 상태를 노출한다.
- WorkComplete일 때도 같은 형식으로 Complete ACK 상태를 표시할 수 있게 했다.
- `PilotPollingView`에 scenario line binding을 추가했다.
- WPF ViewModel test로 WorkStart ACK ON 이후 request OFF/ACK OFF 표시 흐름을 고정했다.

목적:

- 기존 `LastStatus`, `LastStartAckState`를 사람이 한눈에 읽을 수 있는 scenario observation line으로 묶는다.
- backend harness에 이미 존재하는 FakePlc WorkStart/WorkComplete ON/OFF cycle을 WPF 관찰 표면과 연결한다.

영향:

- production flow execution, DB query, payload write, ACK writer 구현은 새로 만들지 않았다.
- 기존 `PilotPollingService`, WorkStart/ACK OFF service, FakePlc harness를 재사용했다.
- RuntimeSnapshot, ChannelPollingResult, FLOW.JSON, parser/executor는 수정하지 않았다.

## 2. Changed Files

- `src/CAAutomationHub.Wpf/ViewModels/Pilot/PilotPollingViewModel.cs`
  - `ScenarioObservation` 속성 추가.
  - WorkStart/WorkComplete ACK state를 status line으로 변환.
- `src/CAAutomationHub.Wpf/Views/Pilot/PilotPollingView.xaml`
  - scenario observation line 표시.
- `tests/CAAutomationHub.Wpf.Tests/ViewModels/PilotPollingViewModelTests.cs`
  - WorkStart ACK ON -> ACK OFF display sequence 검증.
- `tests/CAAutomationHub.Wpf.Tests/Views/PilotPollingViewBindingTests.cs`
  - scenario observation binding 검증.

## 3. Validation

실행:

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj --filter "FullyQualifiedName~PilotPollingViewModelTests.WorkStartScenario_ShowsAckOffObservationAfterRequestOff|FullyQualifiedName~PilotPollingViewBindingTests.ScenarioObservation_BindsToScenarioObservationStatusLine"
dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj --filter "FullyQualifiedName~PilotPollingServiceFakePlcIntegrationTests.PollOnceAsync_WithFakePlc_ProcessesStartAndCompleteOnOffCycle"
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj
dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj
dotnet build CAAutomationHub.sln
git diff --check
git status --short
rg -n "XgtChannelRunner|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON" src tests docs tools
```

결과:

```text
RED: ScenarioObservation absence compile failure.
Targeted WPF tests: failed 0, passed 2, skipped 0, total 2
FakePlc WorkStart/WorkComplete ON/OFF integration: failed 0, passed 1, skipped 0, total 1
WPF tests: failed 0, passed 233, skipped 0, total 233
PilotApp tests: failed 0, passed 18, skipped 0, total 18
Build: warning 0, error 0
git diff --check: exit 0, line-ending warnings only
Boundary scan: historical/existing references only; no new RuntimeSnapshot/ChannelPollingResult/FLOW.JSON/XgtChannelRunner production contamination found.
```

## 4. Harness / Boundary

- Harness: WPF ViewModel sequence test, WPF binding test, existing in-process FakePlc service integration test.
- Boundary: WPF observation surface only. Business flow execution path는 기존 PilotApp/PilotFlows path를 유지한다.
- Runtime shared execution path: 변경 없음.
- RuntimeSnapshot 오염: 없음.
- ChannelPollingResult 오염: 없음.
- WorkStartPilotService source copy: 없음.
- WPF direct XGT/FakePlc/SqlClient reference: 추가 없음.
- FakePlc map 수정: 없음.
- Real PLC 접속/write: 없음.
- DB connection string 기록: 없음.

## 5. WorkStart Scenario Coverage

확인된 backend harness:

- request ON -> WorkStartProcessed -> ACK ON
- request OFF -> WorkStartAckOffWritten -> ACK OFF
- complete request ON/OFF cycle도 기존 FakePlc integration test에서 함께 확인

확인된 WPF observation:

- `ScenarioObservation = WorkStart WorkStartProcessed Start ACK: True`
- `ScenarioObservation = WorkStart WorkStartAckOffWritten Start ACK: False`
- trend point 2개로 ACK ON/OFF cycle 흔적 유지

## 6. STOP Check

다음 STOP 조건은 발생하지 않았다.

- PilotPollingService 구조 대규모 변경 없음.
- FakePlc map 수정 없음.
- actual PLC 필요 없음.
- real DB 필수 아님.
- WPF direct XGT/FakePlc/SqlClient reference 없음.

## 7. Self-Check

```text
Historical record: PASS
WorkStart ACK ON/OFF UI observation: PASS
FakePlc backend harness reused: PASS
Runtime boundary preserved: PASS
Validation evidence: PASS
ACCEPT judgment: ACCEPT
```
