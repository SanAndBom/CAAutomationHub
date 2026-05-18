# AH-PILOT-DI-04 Closeout - Next Wiring Decision Review

## 1. Summary

AH-PILOT-DI-04는 AH-PILOT-DI-02~03에서 만든 `CAAutomationHub.PilotComposition` demo/fake skeleton 이후, 다음 실제 wiring 방향을 결정하기 위한 문서 stage다.

판정은 `ACCEPT`다. 다음 구현 후보는 바로 real XGT/DB wiring이 아니라 `WPF demo wiring`을 우선 검토하는 것이 가장 자연스럽다. 다만 WPF demo wiring도 App.xaml full DI가 아니라, demo/fake profile임이 명확한 좁은 UI 연결 stage로 제한해야 한다.

이번 stage에서는 production code, tests, project reference, App.xaml, Runtime, WPF를 수정하지 않았다. 산출물은 이 closeout 문서뿐이다.

## 2. 현재 PilotComposition 상태

현재 상태:

```text
CAAutomationHub.PilotComposition
    -> CAAutomationHub.PilotApp
    -> CAAutomationHub.PilotFlows
```

추가된 boundary:

- `IWorkStartExecutionServiceFactory`
- `WorkStartDemoComposition`
- `WorkStartDemoOptions`

현재 demo/fake composition:

```text
WorkStartDemoComposition
    -> DemoWorkStartFlowRunner
    -> WorkStartExecutionService
    -> IWorkStartExecutionService
```

유지된 경계:

- `PilotComposition`은 `PilotFlows.Xgt`를 참조하지 않는다.
- `PilotComposition`은 `XgtDriverCore`를 참조하지 않는다.
- `PilotComposition`은 `FakePlc`를 참조하지 않는다.
- `PilotComposition`은 `Microsoft.Data.SqlClient`를 참조하지 않는다.
- WPF는 아직 `PilotComposition`을 참조하지 않는다.
- App.xaml / App.xaml.cs DI wiring은 아직 없다.
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult는 Pilot transaction detail로 오염되지 않았다.

## 3. WPF demo wiring 후보

후보 A는 WPF가 `PilotComposition` demo profile을 사용해 `WorkStartPilotView`에서 demo result를 볼 수 있게 하는 방향이다.

가능한 형태:

```text
WPF demo wiring
    -> WorkStartDemoComposition
    -> IWorkStartExecutionService
    -> WorkStartPilotViewModel
    -> WorkStartPilotView
```

장점:

- 실제 XGT/DB/FakePlc 없이 WPF shell의 end-to-end command display를 검증할 수 있다.
- `WorkStartPilotViewModel`이 이미 `IWorkStartExecutionService` boundary만 의존하므로 concrete 누수를 막기 쉽다.
- demo/fake profile을 명확히 표시하면 현장 연결 전 UI 흐름을 안전하게 볼 수 있다.

위험:

- WPF가 `PilotComposition` project를 참조하게 되면 composition dependency가 WPF app graph에 들어온다.
- demo mode가 실제 현장 실행처럼 보이면 운영 오해 위험이 있다.
- App.xaml full DI 또는 Dashboard placement까지 동시에 열면 범위가 커진다.

권장 제한:

- 첫 WPF demo wiring stage는 real XGT/DB/FakePlc 없이 demo/fake service만 사용한다.
- App.xaml full DI container가 아니라 narrow composition entry 또는 isolated shell host 후보로 검토한다.
- UI에는 demo/fake mode임을 표시하는 정책을 함께 검토한다.
- Dashboard canonical Runtime state와 Pilot transaction display state를 섞지 않는다.

판정:

- 다음 구현 후보로 권장.
- 단, 별도 boundary review 또는 작은 skeleton stage로 진행한다.

## 4. Real XGT/Fake DB composition 후보

후보 B는 `PilotComposition`에 Real XGT / Fake DB profile을 추가하는 방향이다.

가능한 형태:

```text
PilotComposition RealXgtFakeDb profile
    -> WorkStartXgtPlcOperations
    -> XGT session / transport
    -> fake IWorkStartDataQuery
    -> WorkStartFlowService
    -> WorkStartExecutionService
```

장점:

- 실제 XGT adapter 조립 위치를 `PilotComposition` 안으로 제한할 수 있다.
- DB를 fake로 두면 PLC side composition만 단계적으로 검증할 수 있다.
- WPF는 계속 XGT/DB concrete를 직접 알지 않을 수 있다.

위험:

- `PilotComposition` production project가 `PilotFlows.Xgt`와 `XgtDriverCore`를 참조하게 된다.
- actual PLC / FakePlc / transport 선택 정책이 열리며 safety gate가 필요하다.
- DB fake와 real DB boundary가 섞일 수 있다.

판정:

- 장기 후보.
- 지금 바로 구현하기에는 이르다.
- WPF demo wiring 또는 FakePlc failure enhancement로 한 단계 더 evidence를 쌓은 뒤 검토한다.

## 5. FakePlc failure enhancement 복귀 후보

후보 C는 AH-PILOT-22 계열로 돌아가 FakePlc failure scenario를 보강하는 방향이다.

가능한 범위:

- address-specific write NAK
- ACK write failure
- process payload write failure
- malformed read block
- timeout / reconnect 의미 검증

장점:

- FakePlc는 production dependency가 아니라 test/harness profile로 유지된다.
- real XGT/DB composition 전에 WorkStart transaction failure semantics를 강화할 수 있다.
- PilotApp-level full transaction evidence를 더 단단히 만들 수 있다.

위험:

- WPF demo wiring 흐름은 늦어진다.
- DI composition skeleton과 직접 연결되는 사용자-visible progress는 작다.

판정:

- 매우 안전한 보조 후보.
- 다음 stage가 WPF demo wiring으로 가더라도, 이후 failure injection enhancement로 돌아올 가치가 있다.

## 6. Real PLC readiness 후보

후보 D는 실제 PLC test 준비 상태를 audit하는 방향이다.

검토 항목:

- 실제 PLC endpoint / safety window / operator approval
- read/write address policy
- timeout / retry / recovery policy
- DB fake/real 구분
- rollback / emergency stop / manual disable 정책
- 실행 로그 / evidence 보존 방식

장점:

- 현장 연결 전 안전 요구를 드러낼 수 있다.
- real profile safety gate의 기준을 먼저 정할 수 있다.

위험:

- 아직 WPF demo wiring, RealXgtFakeDb composition boundary가 열리지 않았다.
- 구현 없이 audit만 진행하면 현재 Mini Program의 UI bridge momentum과 거리가 있다.

판정:

- 아직 보류.
- real PLC 연결 직전 별도 readiness audit로 진행하는 것이 맞다.

## 7. 권장안

권장 순서:

1. AH-PILOT-DI-05 후보: WPF demo wiring boundary review 또는 skeleton
   - real XGT/DB/FakePlc 없이 demo `IWorkStartExecutionService`를 WPF shell에 연결하는 가장 작은 경로를 검토한다.
   - App.xaml full DI는 아직 피한다.
   - WPF가 `PilotComposition`을 참조해도 되는지, 또는 demo host/test support를 둘지 결정한다.

2. AH-PILOT-25 후보: FakePlc failure injection enhancement
   - DI wiring 전에 transaction failure semantics를 보강한다.

3. AH-PILOT-DI-06 이후 후보: Real XGT/Fake DB composition boundary review
   - `PilotComposition`이 `PilotFlows.Xgt`를 참조하는 시점과 profile naming/safety gate를 별도로 닫는다.

4. Real PLC readiness audit
   - actual PLC 연결 전 별도 stage로 진행한다.

현재 최우선 권장:

- `WPF demo wiring boundary review`
- 이유: DI-02에서 만든 demo/fake composition boundary가 실제로 WPF shell과 만나는 방식을 검토해야 하며, 이 단계도 real DI/App.xaml/actual PLC로 비약하지 않도록 먼저 문서로 닫는 편이 안전하다.

## 8. 다음 Mini Program 후보

후보 이름:

- `AH-PILOT-DI-05 WPF Demo Wiring Boundary Review`
- `AH-PILOT-DI-06 WPF Demo Wiring Skeleton`
- `AH-PILOT-25 FakePlc WorkStart Failure Injection Enhancement`
- `AH-PILOT-DI-07 RealXgtFakeDb Composition Boundary Review`

권장 Mini Program:

```text
AH-PILOT-DI-05
    WPF Demo Wiring Boundary Review

AH-PILOT-DI-06
    WPF Demo Wiring Skeleton

AH-PILOT-DI-07
    WPF Demo Wiring Audit
```

단, FakePlc failure semantics를 먼저 강화하고 싶다면 AH-PILOT-25로 복귀하는 것도 안전하다.

## 9. 제외한 범위

이번 stage에서 제외한 범위:

- production code 수정
- test code 수정
- project reference 수정
- WPF project 수정
- WPF가 `PilotComposition`을 참조하도록 변경
- App.xaml / App.xaml.cs DI registration
- Dashboard placement
- actual XGT/DB/FakePlc composition
- real PLC read/write
- real DB concrete
- SqlClient package/reference
- Runtime project 수정
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult 수정
- FlowDefinitions project 수정
- FLOW.JSON / parser / schema / Flow Executor 구현
- ContextPublisher 자동 publish 재도입

## 10. Self-Check

판정: `ACCEPT`

확인:

- DI-04는 문서 decision review만 수행했다.
- WPF / Runtime / FlowDefinitions / PilotApp / PilotFlows / PilotComposition code를 수정하지 않았다.
- 다음 후보는 WPF demo wiring으로 권장하되, real DI / actual PLC / real DB로 비약하지 않도록 boundary review를 먼저 권장했다.
- FakePlc는 production dependency가 아니라 failure enhancement harness 후보로 유지했다.
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult에 Pilot transaction detail을 넣지 않는 원칙을 유지했다.
