# AH-PILOT-DI-02 Closeout - Pilot Demo Composition Skeleton

## 1. Summary

AH-PILOT-DI-02는 실제 App.xaml DI wiring이 아니라, Pilot 실행 구성 요소를 조립할 별도 composition boundary skeleton을 추가한 stage다.

판정은 `ACCEPT`다. 새 `CAAutomationHub.PilotComposition` production project는 `CAAutomationHub.PilotApp`과 `CAAutomationHub.PilotFlows`만 참조한다. `PilotFlows.Xgt`, `AutomationHub.XgtDriverCore`, `FakePlc`, `XgtChannelRunner`, `Microsoft.Data.SqlClient`는 참조하지 않는다.

`WorkStartDemoComposition`은 demo/fake profile용 `IWorkStartExecutionService`를 생성한다. 내부 구성은 fake `IWorkStartFlowRunner` + `WorkStartExecutionService`이며, 실제 XGT / FakePlc / real DB / App.xaml DI registration은 연결하지 않았다.

## 2. 변경 파일 목록

- `CAAutomationHub.sln`
  - `CAAutomationHub.PilotComposition`, `CAAutomationHub.PilotComposition.Tests` project를 solution에 추가했다.
- `src/CAAutomationHub.PilotComposition/CAAutomationHub.PilotComposition.csproj`
  - Pilot composition production skeleton project를 추가했다.
- `src/CAAutomationHub.PilotComposition/WorkStart/IWorkStartExecutionServiceFactory.cs`
  - `IWorkStartExecutionService` 생성 boundary를 추가했다.
- `src/CAAutomationHub.PilotComposition/WorkStart/WorkStartDemoComposition.cs`
  - demo/fake profile용 service composition을 추가했다.
- `src/CAAutomationHub.PilotComposition/WorkStart/WorkStartDemoOptions.cs`
  - demo result 구성을 위한 최소 options를 추가했다.
- `tests/CAAutomationHub.PilotComposition.Tests/CAAutomationHub.PilotComposition.Tests.csproj`
  - PilotComposition tests project를 추가했다.
- `tests/CAAutomationHub.PilotComposition.Tests/WorkStart/WorkStartDemoCompositionTests.cs`
  - demo composition 생성 / 실행 / project reference boundary tests를 추가했다.
- `docs/harness/AH-PILOT-DI-02.md`
  - 이 closeout 문서다.

## 3. 선택한 project / namespace 배치 이유

선택 project:

- production: `src/CAAutomationHub.PilotComposition`
- tests: `tests/CAAutomationHub.PilotComposition.Tests`

namespace:

- `CAAutomationHub.PilotComposition`
- `CAAutomationHub.PilotComposition.WorkStart`

선택 이유:

- `PilotApp`은 WPF-friendly application service contract와 execution DTO 책임만 유지해야 한다.
- concrete assembly나 profile selection 책임을 `PilotApp`에 넣으면 application boundary가 흐려진다.
- `PilotComposition`이라는 별도 project 이름은 Runtime project와 혼동이 적고, 향후 WPF / CLI / service host에서 재사용 가능한 composition boundary로 확장하기 쉽다.
- 이번 DI-02에서는 real XGT/DB/FakePlc가 아니라 demo/fake skeleton만 필요하므로 `PilotFlows.Xgt` reference를 넣지 않았다.

## 4. 추가한 composition 타입

추가 타입:

- `IWorkStartExecutionServiceFactory`
- `WorkStartDemoComposition`
- `WorkStartDemoOptions`

`IWorkStartExecutionServiceFactory`를 선택한 이유:

- 현재 필요한 책임은 매번 `IWorkStartExecutionService` instance를 생성하는 명시적인 composition boundary다.
- `Provider` 명칭은 cached singleton 또는 service locator처럼 읽힐 수 있어, 이번 skeleton에는 `Factory`가 더 좁고 명확하다.
- WPF가 후속 stage에서 이 boundary를 보더라도 XGT/DB concrete를 직접 알 필요가 없다.

## 5. demo/fake profile 동작

`WorkStartDemoComposition` 동작:

```text
WorkStartDemoComposition
    -> DemoWorkStartFlowRunner
    -> WorkStartExecutionService
    -> IWorkStartExecutionService
```

기본 demo result:

- `Succeeded`: true
- `Status`: `Succeeded`
- `Step`: `completed`
- `SelectedLotId`: `DEMO-LOT-0001` 또는 options의 `SimulatedLotId`
- `Duration`: 1초

`WorkStartDemoOptions`는 최소 후보만 가진다.

- `SimulatedLotId`
- `ShouldSucceed`
- `StartedAt`
- `FailureMessage`

이번 stage에서는 UI 표시용 deterministic demo result만 제공한다. 실제 PLC read/write, FakePlc TCP endpoint, DB query는 없다.

## 6. 실제 XGT/DB/FakePlc 미연결 범위

미연결 범위:

- `WorkStartXgtPlcOperations` 조립 없음
- XGT session / transport 생성 없음
- `AutomationHub.XgtDriverCore` reference 없음
- `FakePlc` reference 없음
- `XgtChannelRunner` reference 없음
- real DB query concrete 없음
- `Microsoft.Data.SqlClient` package/reference 없음
- WPF project 수정 없음
- App.xaml / App.xaml.cs DI registration 없음
- Runtime project 수정 없음
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult 수정 없음
- FlowDefinitions project 수정 없음
- FLOW.JSON / JSON parser / Flow Executor 연결 없음

## 7. 테스트 결과

TDD RED:

```text
dotnet test tests/CAAutomationHub.PilotComposition.Tests/CAAutomationHub.PilotComposition.Tests.csproj
```

결과:

- exit code 1
- production project와 `WorkStartDemoComposition` 타입이 없어 compile failure 발생

GREEN / Validation:

```text
dotnet test tests/CAAutomationHub.PilotComposition.Tests/CAAutomationHub.PilotComposition.Tests.csproj
```

결과:

- pass
- failed 0, passed 3, skipped 0, total 3

기존 regression tests:

```text
dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj
```

- pass
- failed 0, passed 8, skipped 0, total 8

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj
```

- pass
- failed 0, passed 223, skipped 0, total 223

```text
dotnet test tests/CAAutomationHub.PilotFlows.Tests/CAAutomationHub.PilotFlows.Tests.csproj
```

- pass
- failed 0, passed 40, skipped 0, total 40

```text
dotnet test tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj
```

- pass
- failed 0, passed 39, skipped 0, total 39

```text
dotnet test tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj
```

- pass
- failed 0, passed 142, skipped 0, total 142

## 8. 빌드 결과

```text
dotnet build CAAutomationHub.sln
```

결과:

- build succeeded
- warnings 0
- errors 0

## 9. boundary scan 결과

Boundary scan:

```text
rg -n "XgtDriverCore|FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src/CAAutomationHub.PilotComposition tests/CAAutomationHub.PilotComposition.Tests
```

결과:

- production `src/CAAutomationHub.PilotComposition` hit 없음
- test file에는 forbidden reference를 검증하기 위한 test method name / assertion string hit만 존재
- 실제 production reference 오염 없음

Project reference scan:

```text
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
```

확인:

- `src/CAAutomationHub.PilotComposition`은 `CAAutomationHub.PilotApp`, `CAAutomationHub.PilotFlows`만 참조한다.
- `tests/CAAutomationHub.PilotComposition.Tests`는 `CAAutomationHub.PilotComposition`과 xUnit packages만 참조한다.
- WPF project는 수정되지 않았고 `PilotComposition`을 참조하지 않는다.
- XGT/FakePlc references는 기존 `PilotFlows.Xgt` 및 관련 test harness에만 남아 있다.

Whitespace / status:

```text
git diff --check
```

- exit code 0
- output 없음

```text
git status --short
```

- `M CAAutomationHub.sln`
- `?? src/CAAutomationHub.PilotComposition/`
- `?? tests/CAAutomationHub.PilotComposition.Tests/`
- `?? docs/harness/AH-PILOT-DI-02.md`

## 10. 다음 후보

다음 후보:

1. AH-PILOT-DI-03: Composition Boundary Audit
   - DI-02 skeleton이 WPF / Runtime / XGT / FakePlc / SqlClient 경계를 지켰는지 read-only audit한다.
2. AH-PILOT-DI-04: Next Wiring Decision Review
   - WPF demo wiring, Real XGT/Fake DB composition, FakePlc failure enhancement 복귀, Real PLC readiness audit 중 다음 경로를 결정한다.

## 11. Self-Check

판정: `ACCEPT`

확인:

- `CAAutomationHub.PilotComposition` project skeleton을 추가했다.
- demo/fake `IWorkStartExecutionService` composition을 추가했다.
- TDD RED/GREEN evidence를 남겼다.
- WPF project를 수정하지 않았다.
- App.xaml / App.xaml.cs를 수정하지 않았다.
- Runtime project를 수정하지 않았다.
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult를 수정하지 않았다.
- FlowDefinitions project를 수정하지 않았다.
- `PilotComposition` production project는 XgtDriverCore / FakePlc / XgtChannelRunner / SqlClient를 참조하지 않는다.
- real PLC / real DB / FakePlc production wiring을 추가하지 않았다.
