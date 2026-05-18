# AH-PILOT-WPF-05 Closeout - WorkStart Pilot UI Shell

## 1. Summary

AH-PILOT-WPF-05는 `WorkStartPilotViewModel`을 담을 최소 WPF UI shell을 추가한 작업이다.

새 `WorkStartPilotView` UserControl은 외부에서 DataContext가 주입된다고 가정하고, 기존 `WorkStartPilotViewModel`의 display property만 binding한다. 실제 Dashboard placement, `DashboardViewModel` 연결, command property 추가, DI composition, real `IWorkStartExecutionService` 연결은 구현하지 않았다.

이번 작업은 WPF shell skeleton에 한정했다. Runtime project, RuntimeSnapshot, DashboardSnapshot, ChannelPollingResult, FlowDefinitions, PilotFlows, PilotFlows.Xgt, XGT concrete, FakePlc, DB concrete, FLOW.JSON, JSON parser, Flow Executor는 수정하지 않았다.

## 2. 변경 파일 목록

- `src/CAAutomationHub.Wpf/Views/Pilot/WorkStartPilotView.xaml`
- `src/CAAutomationHub.Wpf/Views/Pilot/WorkStartPilotView.xaml.cs`
- `docs/harness/AH-PILOT-WPF-05.md`

## 3. 추가한 UI shell

`WorkStartPilotView`는 `src/CAAutomationHub.Wpf/Views/Pilot` 아래에 추가했다.

역할:

- WorkStart Pilot 상태 표시 shell 제공
- 외부 DataContext 주입 전제 유지
- 기존 WPF `CardBorderStyle`, `SecondaryButtonStyle`, `TextMutedBrush` 사용
- code-behind는 `InitializeComponent()`만 수행

의도적으로 하지 않은 것:

- `DashboardView.xaml`에 배치하지 않음
- `DashboardViewModel` property 추가 없음
- `MainWindowViewModel` 연결 없음
- `App.xaml.cs` 또는 host/DI 수정 없음
- service instance 생성 없음
- execution command 호출 없음

## 4. binding 대상

XAML binding 대상은 AH-PILOT-WPF-03에서 추가된 `WorkStartPilotViewModel` property에 맞췄다.

- `TargetId`
- `IsBusy`
- `LastSucceeded`
- `LastStatus`
- `LastStep`
- `LastErrorCode`
- `LastErrorCodeName`
- `LastMessage`
- `SelectedLotId`
- `LastErrorWriteExpected`
- `LastStartedAt`
- `LastCompletedAt`
- `LastDuration`

이 binding들은 display-only다. `RuntimeSnapshot`, `DashboardSnapshot`, `ChannelPollingResult`로 result를 승격하지 않는다.

## 5. command / button 미구현 범위

ViewModel에는 아직 `ICommand` property가 없다.

따라서 shell의 "착공 실행" button은 disabled placeholder로만 둔다. 실제 button command binding을 위해 ViewModel command property를 추가하거나 `ExecuteOnceAsync`를 command adapter로 감싸는 작업은 AH-PILOT-WPF-05 범위가 아니므로 구현하지 않았다.

후속 command 작업 전에는 다음을 먼저 결정해야 한다.

- command property를 ViewModel에 둘지
- 별도 command adapter를 둘지
- cancellation / retry / busy duplicate 표시를 UI command가 어떻게 반영할지
- operation log/history와 command result를 어떻게 분리할지

## 6. Dashboard 미연결 범위

`DashboardView`와 `DashboardViewModel`은 수정하지 않았다.

이유:

- Dashboard PLC card는 Runtime channel health / polling state 중심이어야 한다.
- WorkStart transaction 상태를 DashboardViewModel에 직접 넣으면 Runtime dashboard 책임과 Pilot business command 책임이 섞인다.
- AH-PILOT-WPF-05의 목적은 화면 완성이 아니라 별도 shell 위치를 만드는 것이다.

후속에서 Dashboard 안에 placeholder region을 둘 수는 있지만, 그때도 `DashboardViewModel` 직접 소유가 아니라 별도 Pilot panel ViewModel 또는 composition boundary를 먼저 검토한다.

## 7. DI / real service 미구현 범위

다음은 구현하지 않았다.

- full DI composition
- App startup service registration
- real `IWorkStartExecutionService` implementation 연결
- XGT adapter concrete composition
- DB query concrete composition
- FakePlc design-time service 연결
- Runtime command dispatcher 연결

`WorkStartPilotView`는 DataContext를 만들지 않는다. 따라서 WPF가 `WorkStartFlowService`, `PilotFlows.Xgt`, `XgtDriverCore`, `FakePlc`, DB concrete를 직접 조립하는 경로가 생기지 않았다.

## 8. 테스트 결과

실행:

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj
```

결과:

```text
pass, failed 0, passed 219, skipped 0
```

실행:

```text
dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj
```

결과:

```text
pass, failed 0, passed 6, skipped 0
```

실행:

```text
dotnet test tests/CAAutomationHub.PilotFlows.Tests/CAAutomationHub.PilotFlows.Tests.csproj
```

결과:

```text
pass, failed 0, passed 40, skipped 0
```

실행:

```text
dotnet test tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj
```

결과:

```text
pass, failed 0, passed 39, skipped 0
```

실행:

```text
dotnet test tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj
```

결과:

```text
pass, failed 0, passed 142, skipped 0
```

별도 XAML UI test는 추가하지 않았다. 이번 변경은 behavior 추가가 아니라 isolated display shell skeleton이며, WPF test project build/test로 XAML compile을 검증했다.

## 9. 빌드 결과

실행:

```text
dotnet build CAAutomationHub.sln
```

결과:

```text
build passed
warnings 0
errors 0
```

## 10. boundary scan 결과

Full requested scan:

```text
rg -n "XgtDriverCore|FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src/CAAutomationHub.Wpf tests/CAAutomationHub.Wpf.Tests src/CAAutomationHub.PilotApp tests/CAAutomationHub.PilotApp.Tests -g '!bin/**' -g '!obj/**'
```

결과:

- exit code `0`
- 기존 WPF RuntimeSnapshot adapter / mapper / tests와 `DashboardLayoutSettingsService` JSON settings persistence hit가 존재한다.
- 새 `Views/Pilot/WorkStartPilotView` 관련 금지 concrete hit는 없다.

Narrow new-boundary scan:

```text
rg -n "XgtDriverCore|FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src/CAAutomationHub.Wpf/Views/Pilot src/CAAutomationHub.Wpf/ViewModels/Pilot tests/CAAutomationHub.Wpf.Tests/ViewModels/WorkStartPilotViewModelTests.cs src/CAAutomationHub.PilotApp tests/CAAutomationHub.PilotApp.Tests -g '!bin/**' -g '!obj/**'
```

결과:

- exit code `1`
- output 없음
- 새 Pilot WPF shell / ViewModel / PilotApp boundary에 금지 keyword hit 없음

Project reference scan:

```text
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
```

결과:

- 이번 작업에서 project/package reference 변경 없음
- `CAAutomationHub.Wpf -> CAAutomationHub.PilotApp` reference는 AH-PILOT-WPF-03의 기존 상태
- WPF가 `PilotFlows.Xgt`, `XgtDriverCore`, `FakePlc`, `XgtChannelRunner`, `Microsoft.Data.SqlClient`를 직접 참조하지 않음
- Runtime project reference 변경 없음

Whitespace:

```text
git diff --check
```

결과:

```text
```

판정:

- exit code `0`
- 출력 없음
- whitespace error 없음

## 11. 다음 후보

- AH-PILOT-WPF-06: WPF Pilot Wiring Boundary Audit
- 후속 UI wiring review: `WorkStartPilotView`를 Dashboard 밖 panel, Dashboard tab, 또는 별도 Pilot section에 표시할 위치 결정
- 후속 command boundary: `WorkStartPilotViewModel` command property 또는 command adapter 추가 여부 검토
- 후속 DI composition boundary: real `IWorkStartExecutionService`를 어디에서 조립할지 결정
- Pilot operation log/history boundary review

## 12. Self-Check

판정: `ACCEPT`

근거:

- `WorkStartPilotView` UserControl shell만 추가했다.
- ViewModel 구조 변경, command 추가, Dashboard 연결, DI composition을 하지 않았다.
- RuntimeSnapshot, DashboardSnapshot, ChannelPollingResult, Runtime, FlowDefinitions, PilotFlows, PilotFlows.Xgt를 수정하지 않았다.
- WPF가 XGT/FakePlc/DB concrete를 직접 참조하는 경로를 만들지 않았다.
- 요청된 WPF, PilotApp, PilotFlows, PilotFlows.Xgt, Runtime tests와 solution build를 실행했고 모두 통과했다.
- boundary scan과 project reference scan으로 금지 경계 침범이 없음을 확인했다.
- ContextPublisher 자동 publish는 재도입하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
