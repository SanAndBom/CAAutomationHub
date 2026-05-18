# AH-PILOT-LIVE-11 Closeout - Pilot Polling Trend Display

## 1. Summary

AH-PILOT-LIVE-11은 WPF에서 실제 polling cycle이 진행되고 있음을 관찰할 수 있도록 Pilot 전용 trend display를 추가한 작업이다.

변경 내용:

- `PilotPollingTrendPoint`를 추가했다.
- `PilotPollingSnapshot`에 최근 polling trend point 목록을 포함했다.
- `PilotPollingService`가 `PollOnceAsync` 결과마다 sequence, timestamp, success/failure, request kind, LOT ID, result/error를 trend point로 append한다.
- `PilotPollingOptions.MaxTrendPoints`로 최근 N개만 유지한다.
- WPF `PilotPollingViewModel`과 `PilotPollingView`가 trend list를 표시한다.

목적:

- 외부 chart package 없이 최근 polling 흐름을 WPF에서 볼 수 있게 한다.
- PollOnce 반복 또는 polling cycle마다 화면에 누적 흔적을 남긴다.
- Runtime trend model 또는 Dashboard graph와 섞지 않고 Pilot 전용 display로 시작한다.

영향:

- 변경 범위는 PilotApp snapshot/display와 WPF Pilot view에 한정된다.
- `RuntimeSnapshot`, `ChannelPollingResult`, Dashboard trend model은 수정하지 않았다.
- 외부 chart package를 추가하지 않았다.

## 2. Changed Files

- `src/CAAutomationHub.PilotApp/Polling/PilotPollingTrendPoint.cs`
  - Pilot polling trend display point 추가.
- `src/CAAutomationHub.PilotApp/Polling/PilotPollingOptions.cs`
  - `MaxTrendPoints` 추가.
- `src/CAAutomationHub.PilotApp/Polling/PilotPollingSnapshot.cs`
  - `TrendPoints` 추가.
- `src/CAAutomationHub.PilotApp/Polling/PilotPollingService.cs`
  - PollOnce 결과마다 trend point append 및 max count trim.
- `src/CAAutomationHub.Wpf/ViewModels/Pilot/PilotPollingViewModel.cs`
  - `TrendPoints` 표시 속성 추가.
- `src/CAAutomationHub.Wpf/Views/Pilot/PilotPollingView.xaml`
  - lightweight `ItemsControl` 기반 trend list 표시.
- `tests/CAAutomationHub.PilotApp.Tests/Polling/PilotPollingServiceTests.cs`
  - trend append / trim 검증.
- `tests/CAAutomationHub.Wpf.Tests/ViewModels/PilotPollingViewModelTests.cs`
  - ViewModel trend 노출 검증.
- `tests/CAAutomationHub.Wpf.Tests/Views/PilotPollingViewBindingTests.cs`
  - XAML trend binding 검증.

## 3. Validation

실행:

```text
dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj --filter "FullyQualifiedName~PilotPollingServiceTests.PollOnceAsync_AppendsTrendPoint|FullyQualifiedName~PilotPollingServiceTests.PollOnceAsync_TrimsTrendPointsToMaxCount"
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj --filter "FullyQualifiedName~PilotPollingViewBindingTests.PollingTrend_BindsToTrendPoints|FullyQualifiedName~PilotPollingViewModelTests.StartStopAndPollCommands_InvokePollingServiceAndExposeSnapshot"
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj
dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj
dotnet build CAAutomationHub.sln
git diff --check
git status --short
rg -n "XgtChannelRunner|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON" src tests docs tools
```

결과:

```text
RED: TrendPoints/MaxTrendPoints absence compile failure.
Targeted PilotApp tests: failed 0, passed 2, skipped 0, total 2
Targeted WPF tests: failed 0, passed 2, skipped 0, total 2
WPF tests: failed 0, passed 231, skipped 0, total 231
PilotApp tests: failed 0, passed 18, skipped 0, total 18
Build: warning 0, error 0
git diff --check: exit 0, line-ending warnings only
Boundary scan: historical/existing references only; no new RuntimeSnapshot/ChannelPollingResult/FLOW.JSON/XgtChannelRunner production contamination found.
```

## 4. Harness / Boundary

- Harness: PilotApp trend append/trim tests, WPF ViewModel/XAML binding tests.
- Boundary: Pilot polling display snapshot 내부의 UI 관찰용 trend다.
- Runtime shared execution path: 변경 없음.
- RuntimeSnapshot 오염: 없음.
- ChannelPollingResult 오염: 없음.
- Dashboard graph 병합: 없음.
- 외부 chart package: 없음.
- WPF direct XGT/FakePlc/SqlClient reference: 추가 없음.
- FakePlc map 수정: 없음.
- Real PLC 접속/write: 없음.
- DB connection string 기록: 없음.

## 5. STOP Check

다음 STOP 조건은 발생하지 않았다.

- 외부 chart package 필요 없음.
- UI style 대규모 수정 없음.
- Dashboard graph 병합 없음.
- Runtime trend model 수정 없음.

## 6. Self-Check

```text
Historical record: PASS
Trend point append: PASS
Trend max count trim: PASS
WPF trend display binding: PASS
Runtime boundary preserved: PASS
Validation evidence: PASS
ACCEPT judgment: ACCEPT
```
