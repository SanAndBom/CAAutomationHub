# AH-PILOT-WPF-08 Closeout - WorkStart Pilot XAML Command Binding Review

## 1. Summary

AH-PILOT-WPF-08은 `WorkStartPilotView.xaml`에 `ExecuteOnceCommand` binding을 추가해도 되는지 검토한 read-only boundary review다.

판정은 `ACCEPT`다.

검토 결과, 다음 stage에서 isolated `WorkStartPilotView` shell에만 execute button command binding을 추가하는 것은 가능하다. Dashboard placement, `App.xaml`/DI composition, real service wiring, XGT/FakePlc/DB concrete 연결은 아직 하지 않는다. Busy disable 정책은 XAML `IsEnabled` binding이 아니라 `ExecuteOnceCommand.CanExecute`에 맡기는 것이 현재 ViewModel command boundary와 맞다.

이번 stage에서는 코드, 테스트, XAML을 수정하지 않았다. Closeout 문서만 추가했다.

## 2. 현재 WorkStartPilotView / ViewModel 상태

현재 `WorkStartPilotView` 상태:

- `WorkStartPilotView`는 isolated UserControl shell이다.
- 외부 DataContext 주입을 전제로 하며 자체 ViewModel 또는 service instance를 생성하지 않는다.
- 현재 button은 `IsEnabled="False"` placeholder다.
- `TargetId`, `IsBusy`, `LastSucceeded`, `LastStatus`, `LastStep`, `LastErrorCode`, `LastErrorCodeName`, `LastMessage`, `SelectedLotId`, `LastErrorWriteExpected`, `LastStartedAt`, `LastCompletedAt`, `LastDuration` display binding이 존재한다.

현재 `WorkStartPilotViewModel` 상태:

- `IWorkStartExecutionService`만 의존한다.
- `ExecuteOnceCommand`를 제공한다.
- command execute는 기존 `ExecuteOnceAsync()` 경로를 사용한다.
- command `CanExecute`는 `!IsBusy`를 반영한다.
- `IsBusy` 변경 시 `RelayCommand.RaiseCanExecuteChanged()`를 호출한다.
- RuntimeSnapshot, DashboardSnapshot, ChannelPollingResult, XGT/FakePlc/DB concrete를 ViewModel property로 노출하지 않는다.

## 3. Execute Button Binding 후보 검토

### 후보 A: Execute button binding 추가

예상 변경:

```xml
<Button Command="{Binding ExecuteOnceCommand}" ... />
```

판정: 권장 가능.

이유:

- AH-PILOT-WPF-07에서 command boundary가 이미 ViewModel test로 검증되었다.
- command는 `IWorkStartExecutionService`만 호출하는 기존 ViewModel 경로를 재사용한다.
- WPF가 `PilotFlows.Xgt`, `XgtDriverCore`, `FakePlc`, DB concrete를 직접 참조할 필요가 없다.
- 실제 service / DI가 없어도 binding skeleton은 isolated shell 수준에서 유지할 수 있다.

위험:

- 실제 app 실행 시 DataContext가 없으면 command는 실행되지 않는다.
- 아직 Dashboard에 붙지 않으므로 실제 사용자 경로가 아니라 isolated shell에 머문다.

대응:

- AH-PILOT-WPF-09에서는 `WorkStartPilotView.xaml`만 수정한다.
- Dashboard / DI / App.xaml service registration은 계속 제외한다.

### 후보 B: Button placeholder disabled 유지

판정: 보수적 후보이나 이번 다음 단계 목적에는 약하다.

이유:

- 현재 안전성은 유지되지만 AH-PILOT-WPF-07의 command boundary가 실제 XAML binding으로 이어지는지 확인하지 못한다.
- WPF shell command wiring audit로 넘어갈 evidence가 부족하다.

### 후보 C: Dashboard에 즉시 배치

판정: 이번 stage에서는 비권장.

이유:

- Dashboard layout, DashboardViewModel, runtime dashboard state와 pilot transaction command의 책임이 섞일 수 있다.
- DataContext composition과 DI 문제가 동시에 열려 범위가 커진다.
- 이번 Mini Program의 목표는 isolated shell command binding까지이며, actual app wiring은 아니다.

## 4. Display Binding 후보 검토

현재 display binding은 다음 stage의 skeleton 목적에 충분하다.

- `LastSucceeded`
- `LastStatus`
- `LastStep`
- `LastErrorCode`
- `LastErrorCodeName`
- `LastMessage`
- `SelectedLotId`
- `LastDuration`
- 보조 표시: `TargetId`, `IsBusy`, `LastErrorWriteExpected`, `LastStartedAt`, `LastCompletedAt`

새 DTO, converter, RuntimeSnapshot/DashboardSnapshot 확장, ChannelPollingResult 확장은 필요하지 않다. Pilot transaction detail은 `WorkStartPilotViewModel` display property 안에 머물러야 한다.

## 5. Dashboard 미연결 판단

Dashboard에는 아직 붙이지 않는다.

이유:

- Dashboard는 Runtime canonical state와 channel health/polling 상태를 표시하는 UI boundary다.
- WorkStart pilot command는 PilotApp application service boundary를 향한 business command shell이다.
- Dashboard에 배치하려면 DataContext ownership, layout placement, command visibility, operation risk indicator를 별도로 검토해야 한다.

따라서 AH-PILOT-WPF-09의 범위는 `WorkStartPilotView.xaml` isolated shell에 한정한다.

## 6. DI 미구현 판단

DI / App.xaml composition은 아직 구현하지 않는다.

이유:

- real `IWorkStartExecutionService` composition은 PilotApp -> PilotFlows -> PilotFlows.Xgt -> XgtDriverCore/transport boundary를 실제로 여는 작업이다.
- 이번 stage의 목적은 XAML binding skeleton이지 실제 현장 실행 wiring이 아니다.
- `WorkStartPilotView`가 DataContext를 직접 만들면 WPF가 service construction 책임을 침범할 수 있다.

따라서 service instance 생성, App.xaml service registration, actual PLC/DB wiring은 제외한다.

## 7. 권장안

권장안:

- AH-PILOT-WPF-09에서 `WorkStartPilotView.xaml`의 placeholder button을 `Command="{Binding ExecuteOnceCommand}"`로 전환한다.
- `IsEnabled`는 별도 `IsBusy` binding으로 제어하지 않고 command `CanExecute`에 맡긴다.
- display binding은 현재 ViewModel properties 중심으로 유지한다.
- Dashboard, DashboardViewModel, App.xaml, DI composition root는 수정하지 않는다.
- ViewModel 구조 변경은 하지 않는다. 필요한 경우 기존 ViewModel command tests만 유지/보강한다.

## 8. AH-PILOT-WPF-09 후보

다음 stage 후보:

- `WorkStartPilotView.xaml` execute button에 `Command="{Binding ExecuteOnceCommand}"` 추가
- 기존 `IsEnabled="False"` 제거
- 필요하면 `IsBusy` 표시 텍스트는 display-only로 유지
- result display binding은 기존 property만 사용
- WPF test, PilotApp/PilotFlows/PilotFlows.Xgt/Runtime test, solution build, boundary scan 실행

STOP 조건:

- DashboardView 수정이 필요해지는 경우
- DI wiring이 필요해지는 경우
- ViewModel 구조를 크게 바꿔야 하는 경우
- XAML command binding이 기존 `ICommand`와 맞지 않는 경우
- WPF가 XGT/FakePlc/DB concrete를 직접 참조해야 하는 경우

## 9. 제외한 범위

이번 stage에서 제외한 범위:

- XAML 수정
- ViewModel 수정
- 테스트 수정
- DashboardView / DashboardViewModel 수정
- App.xaml / DI composition
- real `IWorkStartExecutionService` composition
- actual DB Query
- actual PLC read/write
- FLOW.JSON 연결
- JSON parser / schema
- Flow Executor
- Runtime project 수정
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult 수정
- FlowDefinitions / PilotFlows core / PilotFlows.Xgt production 수정

## 10. 실행한 명령

```text
git rev-parse --git-dir
git rev-parse --git-common-dir
git rev-parse --show-superproject-working-tree
git branch --show-current
git status --short
Get-Content docs/context/META_IPRO_CODEX_COGNITIVE_INTERFACE.md
Get-Content docs/context/COGNITIVE_SYNC_CHECK.md
git log --oneline -8
Get-Content docs/harness/AH-PILOT-WPF-05.md
Get-Content docs/harness/AH-PILOT-WPF-07.md
Get-Content docs/harness/AH-PILOT-24.md
Get-Content src/CAAutomationHub.Wpf/Views/Pilot/WorkStartPilotView.xaml
Get-Content src/CAAutomationHub.Wpf/ViewModels/Pilot/WorkStartPilotViewModel.cs
Get-Content tests/CAAutomationHub.Wpf.Tests/ViewModels/WorkStartPilotViewModelTests.cs
rg -n "WorkStartPilotView|ExecuteOnceCommand|IsBusy|Button|Command|SelectedLotId|LastSucceeded|LastStatus|LastErrorCode" src/CAAutomationHub.Wpf tests/CAAutomationHub.Wpf.Tests
git diff -- docs/harness/AH-PILOT-WPF-08.md
git diff --check
git status --short
Get-Content docs/harness/AH-PILOT-WPF-08.md
```

## 11. git diff --check 결과

실행:

```text
git diff --check
```

결과:

```text
exit code 0
output 없음
```

참고:

```text
git diff -- docs/harness/AH-PILOT-WPF-08.md
```

결과:

```text
output 없음
```

이 파일은 validation 시점에 untracked 상태였으므로 `git diff -- <path>`에는 표시되지 않았다. 문서 내용은 `Get-Content docs/harness/AH-PILOT-WPF-08.md`로 확인했다.

## 12. git status 결과

실행:

```text
git status --short
```

결과:

```text
?? docs/harness/AH-PILOT-WPF-08.md
```

## 13. Self-Check

판정: `ACCEPT`

근거:

- read-only review 목적에 맞게 코드, 테스트, XAML을 수정하지 않았다.
- `WorkStartPilotView`의 다음 변경 후보는 isolated shell command binding에 한정된다.
- `ExecuteOnceCommand`는 AH-PILOT-WPF-07에서 이미 ViewModel command boundary로 검증된 surface다.
- Busy disable은 command `CanExecute`에 맡기는 것으로 판단했다.
- result display는 현재 ViewModel display properties로 충분하다고 판단했다.
- Dashboard / DI / App.xaml / actual service wiring은 다음 stage에도 제외한다.
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult 오염 필요가 없다.
- ContextPublisher 자동 publish는 재도입하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
