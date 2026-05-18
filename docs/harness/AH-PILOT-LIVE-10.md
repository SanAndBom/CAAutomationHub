# AH-PILOT-LIVE-10 Closeout - FakePlc Pilot Card Display

## 1. Summary

AH-PILOT-LIVE-10은 FakePlc `localhost:2004`를 WPF Pilot 화면에서 PLC Card 1개처럼 관찰할 수 있도록 연결한 작업이다.

변경 내용:

- PilotApp 전용 display model `PilotPlcCardStatus`와 `PilotPlcConnectionStatus`를 추가했다.
- `PilotPollingSnapshot`에 Runtime이 아닌 Pilot 전용 `PlcCardStatus`를 포함했다.
- `PilotPollingService`가 request read 성공/실패를 기준으로 PLC card connection/read 상태를 갱신한다.
- `PilotLocalComposition`이 `TargetId`와 loopback `TargetLabel`을 polling options에 전달한다.
- `PilotPollingViewModel`과 `PilotPollingView`가 target, connection, last read를 표시한다.

목적:

- WPF에서 `fakeplc-local` 한 대를 명시적으로 볼 수 있게 한다.
- 민감정보가 아닌 `localhost:2004`만 target label로 표시한다.
- WorkStart/WorkComplete request/ACK 상태와 polling result는 기존 PilotPollingView 표시를 유지한다.

영향:

- 변경은 PilotApp / PilotComposition / WPF display path에 한정된다.
- `RuntimeSnapshot`, `ChannelPollingResult`, Runtime polling path는 수정하지 않았다.
- 기존 Dashboard PLC Card와 병합하지 않고 Pilot 전용 card display로 시작했다.

## 2. Changed Files

- `src/CAAutomationHub.PilotApp/Polling/PilotPlcConnectionStatus.cs`
  - Pilot 전용 PLC card 연결 상태 enum 추가.
- `src/CAAutomationHub.PilotApp/Polling/PilotPlcCardStatus.cs`
  - Pilot 전용 PLC card display snapshot 추가.
- `src/CAAutomationHub.PilotApp/Polling/PilotPollingOptions.cs`
  - target label 옵션 추가.
- `src/CAAutomationHub.PilotApp/Polling/PilotPollingSnapshot.cs`
  - Pilot 전용 `PlcCardStatus` 추가.
- `src/CAAutomationHub.PilotApp/Polling/PilotPollingService.cs`
  - read 성공 시 `Connected/Succeeded`, read 실패 시 `Failed/ReadFailed` card 상태 반영.
- `src/CAAutomationHub.PilotComposition/Polling/PilotLocalComposition.cs`
  - `localhost:2004` target label 구성.
- `src/CAAutomationHub.Wpf/ViewModels/Pilot/PilotPollingViewModel.cs`
  - PLC card 표시 속성 노출.
- `src/CAAutomationHub.Wpf/Views/Pilot/PilotPollingView.xaml`
  - Pilot 전용 PLC card area 추가.
- `tests/CAAutomationHub.PilotApp.Tests/Polling/PilotPollingServiceTests.cs`
  - 성공/읽기 실패 snapshot card 상태 검증.
- `tests/CAAutomationHub.Wpf.Tests/ViewModels/PilotPollingViewModelTests.cs`
  - ViewModel card 표시 속성 검증.
- `tests/CAAutomationHub.Wpf.Tests/Views/PilotPollingViewBindingTests.cs`
  - XAML binding 검증.

## 3. Validation

실행:

```text
dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj --filter "FullyQualifiedName~PilotPollingServiceTests.PollOnceAsync_ProcessesWorkStartRequest_WhenStartRequestOn|FullyQualifiedName~PilotPollingServiceTests.PollOnceAsync_ReturnsFailedSnapshot_WhenRequestStateReadThrows"
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj --filter "FullyQualifiedName~PilotPollingViewBindingTests.PilotPlcCard_BindsToPilotCardDisplayProperties|FullyQualifiedName~PilotPollingViewModelTests.StartStopAndPollCommands_InvokePollingServiceAndExposeSnapshot"
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj
dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj
dotnet test tests/CAAutomationHub.PilotComposition.Tests/CAAutomationHub.PilotComposition.Tests.csproj
dotnet build CAAutomationHub.sln
git diff --check
git status --short
rg -n "XgtChannelRunner|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON" src tests docs tools
```

결과:

```text
RED: PilotApp PlcCardStatus compile failure, WPF card binding assertion failure.
Targeted PilotApp tests: failed 0, passed 2, skipped 0, total 2
Targeted WPF tests: failed 0, passed 2, skipped 0, total 2
WPF tests: failed 0, passed 230, skipped 0, total 230
PilotApp tests: failed 0, passed 16, skipped 0, total 16
PilotComposition tests: failed 0, passed 15, skipped 0, total 15
Build: warning 0, error 0
git diff --check: exit 0, line-ending warnings only
Boundary scan: historical/existing references only; no new RuntimeSnapshot/ChannelPollingResult/FLOW.JSON/XgtChannelRunner production contamination found.
```

## 4. Harness / Boundary

- Harness: PilotApp service tests, WPF ViewModel tests, WPF XAML binding tests.
- Boundary: Pilot display model only. Runtime canonical snapshot path와 분리했다.
- Runtime shared execution path: 변경 없음.
- RuntimeSnapshot 오염: 없음.
- ChannelPollingResult 오염: 없음.
- Existing Dashboard PLC Card 재사용: 없음.
- WPF direct XGT/FakePlc/SqlClient reference: 추가 없음.
- FakePlc map 수정: 없음.
- Real PLC 접속/write: 없음.
- DB connection string 기록: 없음.

## 5. STOP Check

다음 STOP 조건은 발생하지 않았다.

- 기존 Dashboard PLC Card 병합 필요 없음.
- Runtime DashboardSnapshot 수정 없음.
- RuntimeSnapshot 수정 없음.
- external chart/package 추가 없음.
- WPF가 XGT/FakePlc/SqlClient를 직접 참조하지 않음.

## 6. Self-Check

```text
Historical record: PASS
FakePlc local card display: PASS
localhost-only target label: PASS
Connection/read status display: PASS
WorkStart/WorkComplete request display preserved: PASS
Runtime boundary preserved: PASS
Validation evidence: PASS
ACCEPT judgment: ACCEPT
```
