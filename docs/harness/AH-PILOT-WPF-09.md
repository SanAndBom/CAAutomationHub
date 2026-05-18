# AH-PILOT-WPF-09 Closeout - WorkStart Pilot Command View Shell

## 1. Summary

AH-PILOT-WPF-09는 isolated `WorkStartPilotView.xaml` shell에 최소 execute button command binding을 추가한 stage다.

판정은 `ACCEPT`다.

변경 핵심:

- `착공 실행` button의 disabled placeholder를 `Command="{Binding ExecuteOnceCommand}"` binding으로 전환했다.
- button disable 정책은 별도 XAML `IsEnabled` binding 없이 ViewModel command `CanExecute`에 맡겼다.
- 기존 result display binding은 유지했다.
- XAML command binding smoke test를 추가해 button이 `ExecuteOnceCommand`를 바라보고 hard-disabled 상태가 아님을 검증했다.

Dashboard, `DashboardViewModel`, `App.xaml`, DI composition, real service wiring, XGT/FakePlc/DB concrete 연결은 수정하지 않았다.

## 2. 변경 파일 목록

- `src/CAAutomationHub.Wpf/Views/Pilot/WorkStartPilotView.xaml`
  - execute button에 `Command="{Binding ExecuteOnceCommand}"`를 추가했다.
  - 기존 `IsEnabled="False"` placeholder를 제거했다.
- `tests/CAAutomationHub.Wpf.Tests/Views/WorkStartPilotViewBindingTests.cs`
  - XAML을 `XDocument`로 파싱해 execute button command binding을 확인하는 smoke test를 추가했다.
- `docs/harness/AH-PILOT-WPF-09.md`
  - 본 closeout 문서다.

## 3. 추가한 XAML Binding

추가한 binding:

```xml
Command="{Binding ExecuteOnceCommand}"
```

범위:

- `WorkStartPilotView.xaml`의 기존 `착공 실행` button에만 추가했다.
- ViewModel, service, Dashboard, DI composition root는 수정하지 않았다.

## 4. Execute Button Command Binding

button은 `WorkStartPilotViewModel.ExecuteOnceCommand`만 바라본다.

정책:

- `IsBusy`에 따른 disable은 `ExecuteOnceCommand.CanExecute`가 담당한다.
- XAML에는 별도 `IsEnabled="{Binding IsBusy}"` 또는 converter를 추가하지 않았다.
- command는 AH-PILOT-WPF-07에서 추가된 기존 ViewModel command boundary를 재사용한다.

추가 test:

```text
WorkStartPilotViewBindingTests.ExecuteButton_BindsToExecuteOnceCommand
```

검증 내용:

- `WorkStartPilotView.xaml`을 XML로 파싱한다.
- `Content="착공 실행"` button을 찾는다.
- `Command="{Binding ExecuteOnceCommand}"`인지 확인한다.
- hard-disabled placeholder인 `IsEnabled` attribute가 없는지 확인한다.

RED evidence:

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj --filter ExecuteButton_BindsToExecuteOnceCommand
failed, failed 1, passed 0
Expected: "{Binding ExecuteOnceCommand}"
Actual:   null
```

GREEN evidence:

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj --filter ExecuteButton_BindsToExecuteOnceCommand
passed, failed 0, passed 1, skipped 0
```

## 5. Result Display Binding

기존 display binding은 유지했다.

유지한 주요 binding:

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

새 DTO, converter, RuntimeSnapshot/DashboardSnapshot 확장, ChannelPollingResult 확장은 추가하지 않았다.

## 6. Dashboard / DI 미연결 범위

수정하지 않은 범위:

- `DashboardView.xaml`
- `DashboardViewModel`
- `MainWindow`
- `App.xaml`
- App startup service registration
- real `IWorkStartExecutionService` composition
- real DB query
- actual PLC read/write
- FakePlc runtime wiring in WPF
- FLOW.JSON parser / schema / executor

`WorkStartPilotView`는 여전히 isolated UserControl shell이다. DataContext는 외부 composition 단계에서 주입되어야 하며, 이번 stage에서는 그 composition을 구현하지 않았다.

## 7. 테스트 결과

Focused RED:

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj --filter ExecuteButton_BindsToExecuteOnceCommand
failed, failed 1, passed 0
```

Focused GREEN:

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj --filter ExecuteButton_BindsToExecuteOnceCommand
passed, failed 0, passed 1, skipped 0
```

Full tests:

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj
passed, failed 0, passed 223, skipped 0
```

```text
dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj
passed, failed 0, passed 8, skipped 0
```

```text
dotnet test tests/CAAutomationHub.PilotFlows.Tests/CAAutomationHub.PilotFlows.Tests.csproj
passed, failed 0, passed 40, skipped 0
```

```text
dotnet test tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj
passed, failed 0, passed 39, skipped 0
```

```text
dotnet test tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj
passed, failed 0, passed 142, skipped 0
```

## 8. 빌드 결과

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

## 9. Boundary Scan 결과

실행:

```text
rg -n "XgtDriverCore|FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src/CAAutomationHub.Wpf tests/CAAutomationHub.Wpf.Tests src/CAAutomationHub.PilotApp tests/CAAutomationHub.PilotApp.Tests -g '!bin/**' -g '!obj/**'
```

결과 요약:

- 기존 WPF Runtime adapter / mapper / tests의 `RuntimeSnapshot` hit가 존재한다.
- 기존 dashboard settings persistence의 `System.Text.Json` hit가 존재한다.
- AH-PILOT-22 계열 PilotApp test-only FakePlc / XgtDriverCore hit가 존재한다.
- 새 `WorkStartPilotView.xaml` command binding과 새 binding smoke test에는 XGT/FakePlc/DB concrete reference가 없다.
- `XgtChannelRunner`, `SqlConnection`, `Microsoft.Data.SqlClient`, `FlowExecutor`, `FLOW.JSON` 신규 오염 없음.

Project reference scan:

```text
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
```

결과 요약:

- 이번 stage에서 project/package reference 변경 없음.
- WPF project는 기존처럼 `CAAutomationHub.Contracts`, `CAAutomationHub.PilotApp`, `CAAutomationHub.Runtime`를 참조한다.
- WPF project는 `CAAutomationHub.PilotFlows.Xgt`, `AutomationHub.XgtDriverCore`, `AutomationHub.XgtDriverCore.FakePlc`, `XgtChannelRunner`, `Microsoft.Data.SqlClient`를 직접 참조하지 않는다.
- FakePlc reference는 test projects에 한정된다.

Whitespace:

```text
git diff --check
```

결과:

```text
exit code 0
whitespace error 없음
```

참고:

```text
warning: in the working copy of 'src/CAAutomationHub.Wpf/Views/Pilot/WorkStartPilotView.xaml', LF will be replaced by CRLF the next time Git touches it
```

line ending warning만 존재했다.

## 10. 다음 후보

다음 stage:

- AH-PILOT-WPF-10: WPF Pilot Command Wiring Audit

후속 후보:

- AH-PILOT-DI-01: Pilot DI composition boundary review
- AH-PILOT-25: PilotApp/FakePlc failure injection scenario enhancement
- AH-PILOT-LOG-01: Pilot transaction history / operation log boundary review

아직 하지 않을 것:

- Dashboard placement
- App.xaml DI registration
- real service composition
- actual PLC / actual DB execution
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult 반영
- FLOW.JSON parser / Flow Executor 구현

## 11. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-WPF-08 권장안대로 isolated `WorkStartPilotView.xaml` shell만 수정했다.
- execute button은 `ExecuteOnceCommand`만 binding한다.
- hard-disabled placeholder를 제거했고 disable 정책은 ViewModel command `CanExecute`에 맡겼다.
- result display binding은 기존 ViewModel display properties 중심으로 유지했다.
- Dashboard / DI / real service wiring은 수정하지 않았다.
- WPF가 PilotFlows.Xgt / XgtDriverCore / FakePlc / SqlClient를 직접 참조하지 않는다.
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult를 수정하지 않았다.
- focused RED/GREEN, full tests, solution build, boundary scan, project reference scan, diff check를 실행했다.
- ContextPublisher 자동 publish는 재도입하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
