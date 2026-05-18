# AH-PILOT-WPF-04 Closeout - WorkStart Pilot UI Wiring Boundary Review

## 1. Summary

AH-PILOT-WPF-04는 `WorkStartPilotViewModel`을 WPF UI에 노출하기 전 위치와 wiring boundary를 정리한 read-only Boundary Review다.

결론은 `WorkStartPilotViewModel`을 `DashboardViewModel`에 직접 섞지 않고, 별도 WorkStart Pilot panel 또는 UserControl shell에 둔 뒤 후속 단계에서 외부 DataContext 주입으로 표시하는 것이다. Dashboard PLC Card는 Runtime channel health / polling state 중심으로 유지하고, WorkStart transaction 상태는 별도 Pilot display state와 future operation log/history 경로로 분리한다.

이번 단계에서는 production code, test code, XAML, DI, App startup, project reference, Runtime, Snapshot, PilotFlows, FakePlc, DB concrete를 수정하지 않았다. 산출물은 이 closeout 문서뿐이다.

## 2. 현재 WPF / PilotApp / PilotFlow 상태

현재 WPF는 `DashboardView`를 중심으로 PLC card, communication trend, detail pane을 표시한다. `DashboardView`는 자체 `DashboardViewModel` DataContext를 생성하고, `MainWindow`는 `MainWindowViewModel`을 DataContext로 가진다. `App.xaml.cs`는 별도 host 또는 DI composition을 구성하지 않는다.

`RealtimeEventLogViewModel`은 `IEventStreamService`와 `RuntimeEventLogItem`을 사용하며 기본 구현은 `FakeEventStreamService`다. 이 경로는 Runtime event/log 표시 후보이며, WorkStart transaction command state나 busy state를 단독으로 표현하기에는 부족하다.

AH-PILOT-WPF-02에서 `CAAutomationHub.PilotApp`의 `IWorkStartExecutionService` boundary가 만들어졌고, AH-PILOT-WPF-03에서 WPF `WorkStartPilotViewModel`이 추가되었다. 해당 ViewModel은 생성자에서 `IWorkStartExecutionService`와 `TargetId`만 받으며, `WorkStartFlowService`, `PilotFlows.Xgt`, `XgtDriverCore`, `FakePlc`, DB concrete를 직접 참조하지 않는다.

## 3. Dashboard 직접 배치 후보 검토

후보 A는 `DashboardViewModel` 안에 `WorkStartPilotViewModel`을 직접 property로 포함하고 `DashboardView`에 command UI를 붙이는 방식이다.

장점은 빠르게 화면에 노출할 수 있다는 점이다. 하지만 Dashboard가 Pilot transaction state를 직접 소유하게 되고, PLC health UI와 business transaction UI가 섞인다. 장기적으로 WorkComplete, transaction history, operation log가 붙으면 `DashboardViewModel`이 Runtime dashboard 책임과 Pilot business command 책임을 동시에 가지게 된다.

판정은 비권장이다. 임시 연결이라도 `DashboardViewModel` 직접 소유는 피한다.

## 4. 별도 Pilot panel 후보 검토

후보 B는 별도 Pilot panel 또는 WorkStart Pilot section을 두고 `WorkStartPilotViewModel`을 그 shell의 DataContext로 사용하는 방식이다.

장점은 Pilot transaction 상태, busy 표시, 마지막 결과 표시, future transaction history를 Dashboard PLC card 책임과 분리할 수 있다는 점이다. WPF는 계속 `IWorkStartExecutionService` boundary만 바라보고, 실제 service implementation과 DI composition은 후속 단계에서 결정할 수 있다.

위험은 View/UserControl 하나가 추가되고, 실제 화면 배치와 DataContext wiring을 후속에서 다시 결정해야 한다는 점이다. 그러나 이번 Mini Program의 목적이 "문을 만들되 전원을 연결하지 않는 것"이므로 이 위험은 허용 가능하다.

판정은 권장이다. AH-PILOT-WPF-05에서는 Dashboard에 연결하지 않는 `WorkStartPilotView.xaml` UserControl skeleton이 가장 작고 안전하다.

## 5. Realtime Log 연계 후보 검토

Realtime Log에 Pilot 결과를 표시하는 후보는 보조 경로로는 적절하다. operation event stream이나 transaction history가 생기면 `PilotOperationEvent -> log item` mapper 후보를 검토할 수 있다.

하지만 Realtime Log만으로는 WorkStart command의 busy 상태, 마지막 성공/실패 상태, selected LOT ID, failed step, error code를 사용자가 한눈에 보기 어렵다. 따라서 단독 UI로는 부족하다.

판정은 보조 경로다. Runtime event stream과 직접 섞지 않고, 후속 Pilot operation log boundary review 이후 연결한다.

## 6. WPF command / button wiring 범위

AH-PILOT-WPF-04 기준으로 실제 XAML button wiring은 하지 않는다.

AH-PILOT-WPF-05에서 허용 가능한 범위는 display binding 중심의 UserControl skeleton이다. `WorkStartPilotViewModel`에는 아직 `ICommand` property가 없으므로 실행 버튼을 실제 command에 연결하려면 ViewModel command 추가가 필요하다. 이는 이번 Mini Program의 금지/STOP 조건에 가깝다.

따라서 AH-PILOT-WPF-05에서 button을 넣는다면 disabled placeholder로 제한하거나, 아예 command surface 없이 status display shell만 둔다. 실제 실행 버튼 wiring은 별도 후속 단계에서 command boundary review 후 진행한다.

## 7. DI / real service 연결 범위

현재 `App.xaml.cs`와 `MainWindow.xaml.cs`에는 full DI composition이 없다. AH-PILOT-WPF-05에서는 App startup, service registration, host creation, real `IWorkStartExecutionService` implementation 연결을 하지 않는다.

실제 service 연결은 후속 단계에서 composition root를 먼저 정해야 한다. WPF가 XGT adapter, FakePlc, DB query, `WorkStartFlowService`, `WorkStartXgtPlcOperations` concrete를 직접 조립하지 않는 것이 핵심이다.

현재 단계에서는 fake service 또는 design-time service만 안전하다. 다만 AH-PILOT-WPF-05의 추천 범위는 View/UserControl shell이므로 fake service도 production wiring으로 넣지 않는다.

## 8. 권장안

권장안은 다음과 같다.

1. `WorkStartPilotViewModel`을 `DashboardViewModel`에 직접 포함하지 않는다.
2. 별도 `WorkStartPilotView` UserControl shell을 만든다.
3. shell은 외부에서 DataContext가 주입된다고 가정한다.
4. AH-PILOT-WPF-05에서는 display binding만 둔다.
5. 실제 execute command, Dashboard placement, DI composition, real service 연결은 후속으로 미룬다.
6. Pilot transaction result는 `RuntimeSnapshot`, `DashboardSnapshot`, `ChannelPollingResult`에 넣지 않는다.
7. Realtime Log 연계는 future Pilot operation log/event boundary 이후 보조 경로로 검토한다.

## 9. AH-PILOT-WPF-05 후보

AH-PILOT-WPF-05 후보는 `src/CAAutomationHub.Wpf/Views/Pilot/WorkStartPilotView.xaml` UserControl skeleton 추가다.

예상 binding 대상:

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

후속에서 필요한 code-behind는 `InitializeComponent()`만 둔다. Dashboard 연결, DataContext 생성, DI registration, command wiring은 하지 않는다.

## 10. 제외한 범위

이번 단계에서 제외했다.

- Runtime project 수정
- RuntimeSnapshot 수정
- DashboardSnapshot 수정
- ChannelPollingResult 수정
- FlowDefinitions project 수정
- WPF ViewModel 수정
- XAML 수정
- tests 수정
- DI / App.xaml / App.xaml.cs 수정
- DashboardViewModel 연결
- real `IWorkStartExecutionService` implementation 연결
- XGT / FakePlc / DB concrete 연결
- Runtime command dispatcher
- FLOW.JSON / JSON parser / Flow Executor
- commit

## 11. 실행한 명령

Precheck / context:

- `git log --oneline -10`
- `git status --short`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content docs\harness\AH-PILOT-WPF-01.md`
- `Get-Content docs\harness\AH-PILOT-WPF-02.md`
- `Get-Content docs\harness\AH-PILOT-WPF-03.md`

WPF source review:

- `Get-ChildItem -Path src\CAAutomationHub.Wpf -Recurse -File | Select-Object -ExpandProperty FullName`
- `Get-ChildItem -Path tests\CAAutomationHub.Wpf.Tests -Recurse -File | Select-Object -ExpandProperty FullName`
- `rg -n "Dashboard|ViewModel|UserControl|Command|AsyncCommand|RelayCommand|RealtimeLog|Pilot|WorkStart|DataContext|ServiceProvider|Host|DI" src/CAAutomationHub.Wpf tests/CAAutomationHub.Wpf.Tests docs/harness docs/context`
- `rg --files src/CAAutomationHub.Wpf tests/CAAutomationHub.Wpf.Tests | rg "(Dashboard|Realtime|App|ViewModel|View|Xaml|xaml|csproj)$"`
- `Get-Content src\CAAutomationHub.Wpf\ViewModels\Pilot\WorkStartPilotViewModel.cs`
- `Get-Content src\CAAutomationHub.Wpf\Views\DashboardView.xaml`
- `Get-Content src\CAAutomationHub.Wpf\App.xaml.cs`
- `Get-Content src\CAAutomationHub.Wpf\MainWindow.xaml.cs`
- `Get-Content src\CAAutomationHub.Wpf\MainWindow.xaml`
- `Get-Content src\CAAutomationHub.Wpf\ViewModels\MainWindowViewModel.cs`
- `Get-Content src\CAAutomationHub.Wpf\ViewModels\DashboardViewModel.cs`
- `Get-Content src\CAAutomationHub.Wpf\ViewModels\RealtimeEventLogViewModel.cs`
- `Get-Content src\CAAutomationHub.Wpf\Themes\CardStyles.xaml`
- `Get-Content src\CAAutomationHub.Wpf\Themes\ButtonStyles.xaml`
- `Get-Content src\CAAutomationHub.Wpf\Themes\Brushes.xaml`
- `Get-Content src\CAAutomationHub.Wpf\Controls\PlcDetailPane.xaml`

Validation:

- `git diff -- docs/harness/AH-PILOT-WPF-04.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-WPF-04.md`

## 12. git diff --check 결과

실행:

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

참고:

- `git diff -- docs/harness/AH-PILOT-WPF-04.md`는 출력 없음.
- 이유: `docs/harness/AH-PILOT-WPF-04.md`는 신규 untracked 파일이라 plain `git diff -- <path>`에는 표시되지 않는다.
- 문서 내용 확인은 `Get-Content docs\harness\AH-PILOT-WPF-04.md`로 수행했다.

## 13. git status 결과

실행:

```text
git status --short
```

결과:

```text
?? docs/harness/AH-PILOT-WPF-04.md
```

## 14. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-WPF-04 목표인 WPF UI wiring boundary를 read-only로 검토했다.
- `DashboardViewModel`에 WorkStart Pilot transaction state를 직접 섞지 않는 결론을 기록했다.
- 별도 `WorkStartPilotView` UserControl shell 후보를 AH-PILOT-WPF-05 권장안으로 정리했다.
- Realtime Log는 보조 경로이며 단독 UI로는 부족하다고 판정했다.
- XAML button command wiring, DI composition, real service 연결은 후속으로 미뤘다.
- RuntimeSnapshot, DashboardSnapshot, ChannelPollingResult, Runtime, PilotFlows, PilotFlows.Xgt, FlowDefinitions, XGT, FakePlc, DB concrete를 수정하지 않았다.
- ContextPublisher 자동 publish는 재도입하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
