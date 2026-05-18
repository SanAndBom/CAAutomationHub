# AH-PILOT-WPF-10 Closeout - WPF Pilot Command Wiring Audit

## 1. Summary

AH-PILOT-WPF-10은 AH-PILOT-WPF-09 이후 WPF pilot command shell이 경계를 지켰는지 확인한 read-only audit stage다.

판정은 `ACCEPT`다.

확인한 핵심:

- `WorkStartPilotView.xaml`의 execute button은 `ExecuteOnceCommand`만 binding한다.
- button은 더 이상 hard-disabled placeholder가 아니며, busy disable은 command `CanExecute`가 담당한다.
- Dashboard / App.xaml / DI composition은 수정되지 않았다.
- WPF project는 기존처럼 PilotApp boundary를 참조하지만 PilotFlows.Xgt, XgtDriverCore, FakePlc, SqlClient를 직접 참조하지 않는다.
- RuntimeSnapshot, DashboardSnapshot, ChannelPollingResult는 AH-PILOT-WPF-09에서 수정되지 않았다.
- WPF tests와 solution build가 통과했다.

이번 stage에서는 source/test 코드를 수정하지 않았다. Closeout 문서만 추가했다.

## 2. Audit 대상

Audit 대상:

- `src/CAAutomationHub.Wpf/Views/Pilot/WorkStartPilotView.xaml`
- `src/CAAutomationHub.Wpf/ViewModels/Pilot/WorkStartPilotViewModel.cs`
- `tests/CAAutomationHub.Wpf.Tests/Views/WorkStartPilotViewBindingTests.cs`
- `tests/CAAutomationHub.Wpf.Tests/ViewModels/WorkStartPilotViewModelTests.cs`
- AH-PILOT-WPF-09 commit diff
- project/package reference graph
- boundary scan 결과

## 3. WorkStartPilotView Command Binding 확인

현재 execute button:

```xml
<Button Grid.Column="1"
        Content="착공 실행"
        Command="{Binding ExecuteOnceCommand}"
        Style="{StaticResource SecondaryButtonStyle}"
        MinWidth="92"
        VerticalAlignment="Top"/>
```

확인 결과:

- `Command="{Binding ExecuteOnceCommand}"`만 추가되었다.
- `CommandParameter`는 없다.
- `IsEnabled="False"` placeholder는 제거되었다.
- `IsEnabled` binding / converter도 추가되지 않았다.
- XAML에서 service, concrete driver, DB, FakePlc를 만들지 않는다.

## 4. Dashboard / App.xaml / DI 변경 없음

AH-PILOT-WPF-09 commit diff:

```text
docs/harness/AH-PILOT-WPF-09.md
src/CAAutomationHub.Wpf/Views/Pilot/WorkStartPilotView.xaml
tests/CAAutomationHub.Wpf.Tests/Views/WorkStartPilotViewBindingTests.cs
```

따라서 다음 파일/영역은 수정되지 않았다.

- `src/CAAutomationHub.Wpf/Views/DashboardView.xaml`
- `src/CAAutomationHub.Wpf/ViewModels/DashboardViewModel.cs`
- `src/CAAutomationHub.Wpf/App.xaml`
- `src/CAAutomationHub.Wpf/App.xaml.cs`
- `src/CAAutomationHub.Wpf/MainWindow.xaml`
- `src/CAAutomationHub.Wpf/MainWindow.xaml.cs`
- service registration / DI composition root

## 5. WPF Project Reference Boundary

Project reference scan:

```text
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
```

확인 결과:

- `src/CAAutomationHub.Wpf`는 `CAAutomationHub.Contracts`, `CAAutomationHub.PilotApp`, `CAAutomationHub.Runtime`를 참조한다.
- `src/CAAutomationHub.Wpf`는 `CAAutomationHub.PilotFlows.Xgt`, `AutomationHub.XgtDriverCore`, `AutomationHub.XgtDriverCore.FakePlc`, `XgtChannelRunner`, `Microsoft.Data.SqlClient`를 직접 참조하지 않는다.
- `src/CAAutomationHub.PilotApp`는 `CAAutomationHub.PilotFlows`만 참조한다.
- XGT concrete reference는 `src/CAAutomationHub.PilotFlows.Xgt`에 한정된다.
- FakePlc reference는 `tests/CAAutomationHub.PilotApp.Tests`, `tests/CAAutomationHub.PilotFlows.Xgt.Tests`에 한정된다.
- 이번 audit에서 project/package reference 변경 없음.

## 6. RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult 확인

확인 결과:

- AH-PILOT-WPF-09 commit은 RuntimeSnapshot, DashboardSnapshot, ChannelPollingResult source files를 수정하지 않았다.
- `WorkStartPilotView.xaml` command binding은 Runtime dashboard snapshot contract를 확장하지 않는다.
- WorkStart pilot transaction detail은 `WorkStartPilotViewModel` display properties에 머문다.
- `ChannelPollingResult`에 business transaction detail을 추가하지 않았다.

기존 WPF dashboard adapter / mapper / tests에는 RuntimeSnapshot/DashboardSnapshot hit가 존재하지만, 이는 기존 Runtime dashboard bridge 경로다.

## 7. Tests / Build 결과

실행:

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj
```

결과:

```text
passed, failed 0, passed 223, skipped 0
```

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

## 8. Boundary Scan 결과

실행:

```text
rg -n "XgtDriverCore|FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src/CAAutomationHub.Wpf tests/CAAutomationHub.Wpf.Tests src/CAAutomationHub.PilotApp tests/CAAutomationHub.PilotApp.Tests -g '!bin/**' -g '!obj/**'
```

결과 요약:

- `tests/CAAutomationHub.PilotApp.Tests`의 FakePlc / XgtDriverCore hit는 기존 AH-PILOT-22 test-only full transaction harness다.
- WPF Runtime adapter / mapper / tests의 `RuntimeSnapshot` hit는 기존 Dashboard runtime bridge 경로다.
- `DashboardLayoutSettingsService`의 `System.Text.Json` hit는 기존 layout settings persistence다.
- 새 `WorkStartPilotView.xaml` command binding에는 XGT/FakePlc/DB concrete hit가 없다.
- 새 `WorkStartPilotViewBindingTests`는 XAML binding만 검사하며 concrete driver/DB reference가 없다.
- `XgtChannelRunner`, `SqlConnection`, `Microsoft.Data.SqlClient`, `FlowExecutor`, `FLOW.JSON` 신규 오염 없음.

Whitespace/status:

```text
git diff --check
```

결과:

```text
exit code 0
output 없음
```

```text
git status --short
```

결과:

```text
output 없음
```

위 status는 AH-PILOT-WPF-10 closeout 문서 생성 전 read-only audit 시점 기준이다.

## 9. 다음 단계 판단

권장 다음 후보:

- AH-PILOT-DI-01: Pilot DI composition boundary review

이유:

- WPF shell command binding까지는 완료되었다.
- 실제 실행 경로로 넘어가기 전에는 `IWorkStartExecutionService` concrete를 어디에서 만들고, WPF가 어떤 abstraction만 보게 할지 먼저 검토해야 한다.
- App.xaml service registration과 actual service wiring은 아직 금지 범위였으므로, 다음에도 구현 전에 boundary review가 필요하다.

대안 후보:

- AH-PILOT-25: PilotApp/FakePlc failure injection scenario enhancement

이유:

- DI composition을 늦추고 PilotApp execution harness 강도를 먼저 높이는 경로다.
- real wiring 전에 timeout/malformed/recovery 의미를 더 고정하려면 이 방향도 가능하다.

판단:

- WPF bridge 흐름을 계속 밀면 AH-PILOT-DI-01이 자연스럽다.
- Harness 강화를 우선하면 AH-PILOT-25로 돌아간다.
- 어느 쪽이든 actual PLC / real DB / App.xaml DI 구현은 다음 첫 단계가 아니라 boundary review 후 진행해야 한다.

## 10. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-WPF-10은 read-only audit로 수행했다.
- source/test 코드를 수정하지 않았다.
- `WorkStartPilotView.xaml`은 `ExecuteOnceCommand`만 binding한다.
- Dashboard / App.xaml / DI는 수정되지 않았다.
- WPF project가 PilotFlows.Xgt / XgtDriverCore / FakePlc / SqlClient를 직접 참조하지 않는다.
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult 수정이 없다.
- WPF tests와 solution build를 fresh 실행했고 통과했다.
- boundary scan과 project reference scan을 실행했다.
- ContextPublisher 자동 publish는 재도입하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
