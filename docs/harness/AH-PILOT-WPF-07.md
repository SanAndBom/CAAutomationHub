# AH-PILOT-WPF-07 Closeout - WorkStart ViewModel Command Boundary

## 1. Summary

AH-PILOT-WPF-07은 `WorkStartPilotViewModel`에 실제 XAML button wiring 전 단계의 command boundary를 추가한 stage다.

기존 `ExecuteOnceAsync` application service 호출 경로는 유지하고, WPF binding이 사용할 수 있는 `ExecuteOnceCommand`를 최소 범위로 추가했다. Command는 기존 WPF project의 `RelayCommand`를 재사용한다.

판정은 `ACCEPT`다. XAML, DI composition, actual service composition, XGT/DB/FakePlc concrete, RuntimeSnapshot, DashboardSnapshot, ChannelPollingResult는 수정하지 않았다.

## 2. 변경 파일 목록

- `src/CAAutomationHub.Wpf/ViewModels/Pilot/WorkStartPilotViewModel.cs`
  - `ICommand ExecuteOnceCommand` property를 추가했다.
  - command execute는 기존 `ExecuteOnceAsync`를 호출한다.
  - command `CanExecute`는 `!IsBusy`를 반영한다.
  - `IsBusy` 변경 시 기존 `RelayCommand.RaiseCanExecuteChanged()`를 호출한다.
- `tests/CAAutomationHub.Wpf.Tests/ViewModels/WorkStartPilotViewModelTests.cs`
  - command invocation, busy duplicate policy, CanExecute reflection tests를 추가했다.
- `docs/harness/AH-PILOT-WPF-07.md`
  - 본 closeout 문서다.

## 3. Command Boundary 선택 이유

선택: 기존 `RelayCommand` 재사용.

이유:

- WPF project에 이미 `RelayCommand`와 `ICommand` property pattern이 있다.
- 별도 async command helper를 새로 만들면 이번 stage 범위를 넘는다.
- 기존 `ExecuteOnceAsync`가 busy guard와 exception display mapping을 이미 갖고 있으므로 command는 얇은 boundary만 제공하면 된다.
- XAML binding은 아직 하지 않지만 다음 stage에서 command binding을 붙일 수 있는 표면은 준비된다.

## 4. 추가한 Command Behavior

추가 property:

```text
public ICommand ExecuteOnceCommand { get; }
```

동작:

- `Execute` 호출 시 `ExecuteOnceAsync()`를 시작한다.
- service 호출은 여전히 `IWorkStartExecutionService`만 사용한다.
- command는 target id / display state mapping을 직접 다루지 않고 기존 method 경로를 재사용한다.

추가 테스트:

- `ExecuteOnceCommand_InvokesExecutionService`
- `ExecuteOnceCommand_RespectsBusyState`
- `ExecuteOnceCommand_CanExecuteReflectsBusyState`

RED evidence:

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj
failed with CS1061: WorkStartPilotViewModel has no ExecuteOnceCommand
```

GREEN evidence:

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj
pass, failed 0, passed 222, skipped 0
```

## 5. Busy / Duplicate Policy

Busy policy:

- `ExecuteOnceAsync`는 기존처럼 `IsBusy`가 true이면 즉시 return한다.
- `ExecuteOnceCommand.CanExecute(null)`는 idle이면 true, busy이면 false다.
- `IsBusy` 변경 시 `CanExecuteChanged`가 raise될 수 있도록 `RelayCommand.RaiseCanExecuteChanged()`를 호출한다.
- command가 직접 중복 호출되어도 service call count는 1로 유지된다.

## 6. XAML / DI 미구현 범위

이번 stage에서 하지 않은 것:

- XAML button binding 없음
- `WorkStartPilotView.xaml` 수정 없음
- App.xaml / DI composition root 수정 없음
- actual `IWorkStartExecutionService` composition 없음
- XGT / FakePlc / DB concrete 연결 없음
- DashboardView / DashboardViewModel wiring 없음
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult 반영 없음

## 7. 테스트 결과

실행:

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj
```

결과:

```text
pass, failed 0, passed 222, skipped 0
```

실행:

```text
dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj
dotnet test tests/CAAutomationHub.PilotFlows.Tests/CAAutomationHub.PilotFlows.Tests.csproj
dotnet test tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj
dotnet test tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj
```

결과:

```text
PilotApp tests: pass, failed 0, passed 8, skipped 0
PilotFlows tests: pass, failed 0, passed 40, skipped 0
PilotFlows.Xgt tests: pass, failed 0, passed 39, skipped 0
Runtime tests: pass, failed 0, passed 142, skipped 0
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
git diff --check
```

결과:

```text
exit code 0
whitespace error 없음
```

참고:

```text
line ending warning만 존재
```

실행:

```text
rg -n "XgtDriverCore|FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src/CAAutomationHub.Wpf tests/CAAutomationHub.Wpf.Tests src/CAAutomationHub.PilotApp tests/CAAutomationHub.PilotApp.Tests -g '!bin/**' -g '!obj/**'
```

결과:

- 기존 WPF Runtime adapter / mapper / tests의 RuntimeSnapshot hit 존재
- 기존 dashboard settings JSON hit 존재
- AH-PILOT-22의 PilotApp test-only FakePlc / XgtDriverCore hit 존재
- 새 `WorkStartPilotViewModel` command boundary와 WPF ViewModel test에는 forbidden keyword hit 없음

Narrow scan:

```text
rg -n "XgtDriverCore|FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src/CAAutomationHub.Wpf/ViewModels/Pilot tests/CAAutomationHub.Wpf.Tests/ViewModels/WorkStartPilotViewModelTests.cs -g '!bin/**' -g '!obj/**'
```

결과:

```text
exit code 1
output 없음
```

Project reference scan:

```text
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
```

결과:

- 이번 stage에서 project/package reference 변경 없음
- WPF project는 기존처럼 `CAAutomationHub.Contracts`, `CAAutomationHub.PilotApp`, `CAAutomationHub.Runtime`만 참조
- WPF는 `CAAutomationHub.PilotFlows.Xgt`, `AutomationHub.XgtDriverCore`, `AutomationHub.XgtDriverCore.FakePlc`, `XgtChannelRunner`, `Microsoft.Data.SqlClient`를 직접 참조하지 않음

## 10. 다음 후보

다음 stage는 AH-PILOT-24 read-only boundary audit이다.

후속 후보:

- XAML button binding boundary review
- DI composition boundary review
- Pilot transaction history / operation log boundary review
- FakePlc failure scenario enhancement

## 11. Self-Check

판정: `ACCEPT`

근거:

- ViewModel command boundary를 기존 command pattern으로 최소 구현했다.
- Command behavior는 fake service 기반 WPF ViewModel tests로 고정했다.
- Busy duplicate policy와 `CanExecute` 반영을 검증했다.
- XAML / DI / actual concrete wiring은 하지 않았다.
- WPF가 XGT / FakePlc / DB concrete를 직접 보지 않는 경계를 유지했다.
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult를 수정하지 않았다.
- 지시된 tests, solution build, diff check, boundary scan, project reference scan을 실행했다.
- ContextPublisher 자동 publish는 재도입하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
