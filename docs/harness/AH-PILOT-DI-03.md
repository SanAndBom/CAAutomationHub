# AH-PILOT-DI-03 Closeout - Composition Boundary Audit

## 1. Summary

AH-PILOT-DI-03은 AH-PILOT-DI-02에서 추가된 `CAAutomationHub.PilotComposition` skeleton이 WPF / Runtime / XGT / FakePlc / DB 경계를 지켰는지 확인한 read-only audit stage다.

판정은 `ACCEPT`다. `PilotComposition` production project는 `CAAutomationHub.PilotApp`과 `CAAutomationHub.PilotFlows`만 참조한다. `PilotFlows.Xgt`, `AutomationHub.XgtDriverCore`, `FakePlc`, `XgtChannelRunner`, `Microsoft.Data.SqlClient`를 참조하지 않는다.

이번 stage에서는 production code, tests, project reference, App.xaml, Runtime, WPF를 수정하지 않았다. 산출물은 이 closeout 문서뿐이다.

## 2. Audit 결과

### 2.1 PilotComposition project reference

확인:

```text
src/CAAutomationHub.PilotComposition/CAAutomationHub.PilotComposition.csproj
    -> CAAutomationHub.PilotApp
    -> CAAutomationHub.PilotFlows
```

판정:

- ACCEPT
- DI-02 의도대로 composition skeleton은 application boundary와 flow core만 참조한다.

### 2.2 XGT / FakePlc / SqlClient 미참조

확인:

- `PilotComposition` production project는 `PilotFlows.Xgt`를 참조하지 않는다.
- `PilotComposition` production project는 `AutomationHub.XgtDriverCore`를 참조하지 않는다.
- `PilotComposition` production project는 `AutomationHub.XgtDriverCore.FakePlc`를 참조하지 않는다.
- `PilotComposition` production project는 `XgtChannelRunner`를 참조하지 않는다.
- `PilotComposition` production project는 `Microsoft.Data.SqlClient` package/reference를 가지지 않는다.

판정:

- ACCEPT
- DI-02는 demo/fake profile skeleton이며 real XGT / real DB / FakePlc composition으로 확장되지 않았다.

### 2.3 WPF project 미참조

확인:

```text
src/CAAutomationHub.Wpf/CAAutomationHub.Wpf.csproj
    -> CAAutomationHub.Contracts
    -> CAAutomationHub.PilotApp
    -> CAAutomationHub.Runtime
```

판정:

- ACCEPT
- WPF는 아직 `PilotComposition`을 참조하지 않는다.
- WPF는 `PilotFlows.Xgt`, `XgtDriverCore`, `FakePlc`, `SqlClient`도 직접 참조하지 않는다.

### 2.4 App.xaml / DI wiring 변경 없음

확인:

- `src/CAAutomationHub.Wpf/App.xaml` 수정 없음
- `src/CAAutomationHub.Wpf/App.xaml.cs` 수정 없음
- service registration / HostBuilder / App startup DI wiring 없음

판정:

- ACCEPT
- DI-02는 composition boundary skeleton만 추가했고 actual WPF wiring은 하지 않았다.

### 2.5 RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult 변경 없음

확인:

- Runtime project 변경 없음
- WPF Dashboard model 변경 없음
- `RuntimeSnapshot`, `DashboardSnapshot`, `ChannelPollingResult` 변경 없음
- Pilot transaction result를 Runtime canonical state 또는 polling result에 넣지 않았다.

판정:

- ACCEPT
- Runtime polling state path와 Pilot business command path가 섞이지 않았다.

### 2.6 demo/fake profile 오해 위험

현재 `WorkStartDemoComposition`은 `WorkStartDemoOptions`와 deterministic fake runner로 demo result를 만든다.

위험:

- 후속 WPF demo wiring에서 real execution처럼 보이면 운영자가 실제 PLC/DB 연결로 오해할 수 있다.
- `WorkStartDemoComposition` 이름에는 Demo가 들어가지만, UI 표시 단계에서는 mode label / disabled real action 표시가 필요할 수 있다.

판정:

- ACCEPT_WITH_CAUTION
- DI-02 자체는 안전하지만, WPF demo wiring 단계에서는 demo/fake mode 표시 정책을 별도로 검토해야 한다.

### 2.7 향후 real XGT composition 위치

후보:

```text
CAAutomationHub.PilotComposition
    -> RealXgtFakeDb profile candidate
    -> PilotFlows.Xgt reference candidate later
    -> DB adapter project candidate later
```

판정:

- 현재는 보류.
- real XGT composition은 DI-02 skeleton에 섞지 않는 것이 맞다.
- real XGT / fake DB / real DB profile은 safety gate와 profile selection review 이후 별도 stage에서 열어야 한다.

### 2.8 다음 단계 판단

다음 단계는 AH-PILOT-DI-04 Decision Review로 진행하는 것이 적절하다.

검토할 후보:

- WPF demo wiring
- Real XGT/Fake DB composition boundary
- FakePlc failure enhancement 복귀
- Real PLC readiness audit

판정:

- DI-03에서 바로 WPF wiring을 구현하지 않는다.
- 먼저 DI-04에서 다음 wiring 방향을 문서로 닫는다.

## 3. 실행한 명령

```powershell
dotnet test tests/CAAutomationHub.PilotComposition.Tests/CAAutomationHub.PilotComposition.Tests.csproj
dotnet build CAAutomationHub.sln
git diff --check
git status --short
rg -n "XgtDriverCore|FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|DashboardSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src/CAAutomationHub.PilotComposition tests/CAAutomationHub.PilotComposition.Tests src/CAAutomationHub.Wpf src/CAAutomationHub.Runtime src/CAAutomationHub.FlowDefinitions -g "!bin/**" -g "!obj/**"
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
```

## 4. 테스트 결과

```text
dotnet test tests/CAAutomationHub.PilotComposition.Tests/CAAutomationHub.PilotComposition.Tests.csproj
```

결과:

- pass
- failed 0, passed 3, skipped 0, total 3

## 5. 빌드 결과

```text
dotnet build CAAutomationHub.sln
```

결과:

- build succeeded
- warnings 0
- errors 0

## 6. boundary scan 결과

Boundary scan 결과:

- `src/CAAutomationHub.PilotComposition` production hit 없음
- `tests/CAAutomationHub.PilotComposition.Tests`에는 forbidden reference boundary를 검증하는 test method/assertion string hit만 존재
- `src/CAAutomationHub.Wpf`의 RuntimeSnapshot/DashboardSnapshot/Json hits는 기존 dashboard/runtime bridge 및 layout settings 경로다.
- `src/CAAutomationHub.Runtime`의 RuntimeSnapshot/ChannelPollingResult hits는 기존 Runtime canonical state / polling path다.
- PilotComposition이 RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult를 새로 참조하지 않는다.

Project reference scan 결과:

- `src/CAAutomationHub.PilotComposition`은 `CAAutomationHub.PilotApp`, `CAAutomationHub.PilotFlows`만 참조한다.
- `tests/CAAutomationHub.PilotComposition.Tests`는 `CAAutomationHub.PilotComposition`과 xUnit packages만 참조한다.
- `src/CAAutomationHub.Wpf`는 `PilotComposition`을 참조하지 않는다.
- 기존 XGT/FakePlc references는 `PilotFlows.Xgt`와 관련 test harness에 한정된다.

## 7. git 상태

Audit 명령 실행 후 closeout 작성 전:

```text
git status --short
```

결과:

```text
output 없음
```

Closeout 작성 후 예상:

```text
?? docs/harness/AH-PILOT-DI-03.md
```

## 8. 제외한 범위

이번 stage에서 제외한 범위:

- production code 수정
- test code 수정
- project reference 수정
- WPF project 수정
- App.xaml / App.xaml.cs DI wiring
- Runtime project 수정
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult 수정
- FlowDefinitions project 수정
- `PilotFlows.Xgt` / XgtDriverCore / FakePlc / SqlClient composition
- real PLC / real DB 연결
- FLOW.JSON / parser / schema / Flow Executor 구현

## 9. 다음 후보

권장 다음 stage:

- AH-PILOT-DI-04: Next Wiring Decision Review

DI-04에서 결정할 후보:

1. WPF demo wiring
2. Real XGT/Fake DB composition boundary
3. FakePlc failure enhancement 복귀
4. Real PLC readiness audit

## 10. Self-Check

판정: `ACCEPT`

확인:

- DI-03은 read-only audit + closeout만 수행했다.
- `PilotComposition` production project는 `PilotApp` / `PilotFlows`만 참조한다.
- WPF는 `PilotComposition`을 아직 참조하지 않는다.
- App.xaml / DI wiring은 수정되지 않았다.
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult는 수정되지 않았다.
- demo/fake profile은 real PLC로 오해될 위험을 DI-04 검토 항목으로 넘겼다.
