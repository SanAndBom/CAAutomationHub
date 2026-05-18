# AH-PILOT-DI-01 Closeout - Pilot DI Composition Boundary Review

## 1. Summary

AH-PILOT-DI-01은 실제 `IWorkStartExecutionService` 구현체를 어디에서 조립할지 검토한 Boundary Review다.

판정은 `ACCEPT`다. 현재 repo 상태에서는 WPF App.xaml / App startup에서 `WorkStartXgtPlcOperations`, XGT session, DB query concrete, FakePlc를 직접 조립하지 않는 것이 맞다. 실제 concrete assembly는 별도 Pilot Composition 계층 후보로 분리하는 것이 가장 안전하다.

이번 단계에서는 코드, 테스트, DI 등록, App.xaml, ViewModel, Dashboard, csproj, solution, reference를 수정하지 않았다. 산출물은 이 closeout 문서뿐이다.

핵심 결론:

1. WPF는 계속 `CAAutomationHub.PilotApp` application boundary만 직접 사용한다.
2. WPF가 `CAAutomationHub.PilotFlows.Xgt`, `AutomationHub.XgtDriverCore`, `FakePlc`, `XgtChannelRunner`, `Microsoft.Data.SqlClient`, DB concrete를 직접 참조하거나 조립하지 않는다.
3. 실제 Pilot concrete composition은 후속 별도 `CAAutomationHub.PilotComposition` 계층 후보에서 조립한다.
4. `PilotApp`은 application contract / execution service boundary로 유지하고, XGT / DB concrete composition을 내부로 끌어들이지 않는다.
5. Runtime command dispatcher 통합은 장기 후보로 남기되, 현재 manual pilot DI 단계에서는 보류한다.
6. AH-PILOT-DI-02는 real DI wiring이 아니라 Pilot Composition Boundary Skeleton 또는 demo/fake composition skeleton으로 진행하는 것이 안전하다.

## 2. 현재 WPF / PilotApp / PilotFlows / PilotFlows.Xgt reference 상태

현재 production project reference 상태:

```text
CAAutomationHub.Wpf
    -> CAAutomationHub.Contracts
    -> CAAutomationHub.PilotApp
    -> CAAutomationHub.Runtime

CAAutomationHub.PilotApp
    -> CAAutomationHub.PilotFlows

CAAutomationHub.PilotFlows
    -> no project reference

CAAutomationHub.PilotFlows.Xgt
    -> CAAutomationHub.PilotFlows
    -> AutomationHub.XgtDriverCore

CAAutomationHub.Runtime
    -> CAAutomationHub.Contracts

CAAutomationHub.FlowDefinitions
    -> CAAutomationHub.Contracts
```

확인 결과:

- WPF production project는 `PilotApp`을 참조하지만 `PilotFlows.Xgt`, `XgtDriverCore`, `FakePlc`, `XgtChannelRunner`, `Microsoft.Data.SqlClient`를 직접 참조하지 않는다.
- `WorkStartPilotViewModel`은 `IWorkStartExecutionService`, `WorkStartExecutionRequest`, `WorkStartExecutionResult` application boundary만 사용한다.
- `WorkStartPilotView`는 `ExecuteOnceCommand` binding을 가지지만 DataContext와 service instance를 만들지 않는다.
- `PilotApp`은 `PilotFlows`만 참조한다.
- `PilotFlows` core는 XGT-free / Runtime-free / FakePlc-free / DB concrete-free 상태다.
- XGT concrete는 `PilotFlows.Xgt` production project에 한정된다.
- FakePlc reference는 `tests/CAAutomationHub.PilotApp.Tests`, `tests/CAAutomationHub.PilotFlows.Xgt.Tests`에 한정된다.
- Runtime / FlowDefinitions production project는 Pilot concrete나 XGT/FakePlc/DB concrete를 참조하지 않는다.

## 3. WPF App.xaml 직접 조립 후보 검토

후보 A는 WPF `App.xaml.cs` 또는 startup code에서 다음 객체를 직접 조립하는 방식이다.

```text
WPF App startup
    creates XGT session / WorkStartXgtPlcOperations
    creates DB query concrete
    creates WorkStartFlowService
    creates WorkStartFlowServiceRunner
    creates WorkStartExecutionService
    injects WorkStartPilotViewModel
```

장점:

- 가장 빠르게 실제 버튼 경로를 만들 수 있다.
- WPF 화면과 service instance 연결 위치가 가깝다.

위험:

- WPF project reference가 `PilotFlows.Xgt`, `XgtDriverCore`, DB concrete, SqlClient로 확장될 가능성이 크다.
- WPF가 XGT transport, DB query, recovery primitive, test/demo harness 선택 책임을 갖게 된다.
- App startup이 Dashboard runtime composition, Pilot business command composition, future host/profile selection을 모두 떠안아 비대해질 수 있다.
- 현재 `WorkStartPilotViewModel`이 application service boundary만 바라보도록 만든 의미가 약해진다.

판정:

- 비권장.
- 실제 App.xaml wiring은 AH-PILOT-DI-03 이후에도 별도 composition boundary를 통과하는 최소 호출 수준으로 제한해야 한다.
- WPF App startup이 직접 concrete graph를 아는 구조는 현재 Boundary Invariant와 맞지 않는다.

## 4. 별도 Pilot Composition project 후보 검토

후보 B는 별도 Pilot Composition project를 두는 방식이다.

후보 이름:

- `CAAutomationHub.PilotComposition`
- `CAAutomationHub.PilotApp.Composition`
- `CAAutomationHub.Composition.Pilot`
- `CAAutomationHub.PilotRuntime`

권장 이름은 `CAAutomationHub.PilotComposition`이다. 역할이 명확하고 Runtime project와 혼동이 적다.

구조 후보:

```text
CAAutomationHub.PilotComposition
    -> CAAutomationHub.PilotApp
    -> CAAutomationHub.PilotFlows
    -> CAAutomationHub.PilotFlows.Xgt
    -> AutomationHub.XgtDriverCore
    -> DB adapter project later

CAAutomationHub.Wpf
    -> CAAutomationHub.PilotApp
    -> composition boundary only when actual app wiring is approved
```

장점:

- WPF가 XGT / DB concrete를 직접 알지 않도록 막을 수 있다.
- `PilotApp`은 application contract와 WPF-friendly execution DTO 책임만 유지한다.
- `PilotComposition`은 concrete assembly, profile selection, options binding, safety gate 책임을 분리해서 가진다.
- 향후 WPF 외 CLI / service host / harness executable에서도 재사용 가능하다.
- Fake/demo/real profile을 같은 application contract로 갈아끼울 수 있다.

위험:

- project 수가 늘어난다.
- WPF가 composition project를 참조하는 순간, composition project의 concrete dependency가 WPF app dependency graph에 들어온다.
- 따라서 WPF에서 사용할 public surface는 narrow factory 또는 profile selector로 제한해야 한다.

판정:

- 장기적으로 가장 안전한 후보다.
- 단, AH-PILOT-DI-02에서는 project 생성 자체가 아니라 Boundary Skeleton 계획 또는 최소 skeleton 구현 단계로 작게 가야 한다.
- 실제 WPF App.xaml wiring은 composition project가 생긴 뒤에도 즉시 하지 않고 별도 stage에서 검토한다.

## 5. PilotApp factory/composition helper 후보 검토

후보 C는 `PilotApp` 안에 factory 또는 composition helper를 두는 방식이다.

가능한 안전 범위:

- `IWorkStartExecutionService` contract
- `IWorkStartFlowRunner` abstraction
- request/result DTO
- clock / busy guard / result mapping
- concrete-free factory abstraction definition

위험한 범위:

- `WorkStartXgtPlcOperations` 생성
- XGT session / transport 생성
- DB concrete 생성
- FakePlc endpoint 생성
- SqlClient 또는 DB adapter 직접 참조

판정:

- `PilotApp` 안에는 XGT / DB concrete composition을 넣지 않는다.
- `PilotApp -> PilotFlows.Xgt` reference가 생기면 현재 layered boundary가 흐려진다.
- `PilotApp`은 application service boundary로 유지하고, concrete graph assembly는 별도 composition 계층에 둔다.
- factory abstraction까지는 가능하지만, 실제 concrete factory 구현은 `PilotComposition` 쪽이 맞다.

## 6. Runtime command dispatcher 후보 검토

후보 D는 WPF가 Runtime command dispatcher로 command를 보내고 Runtime이 Pilot execution을 조정하는 방식이다.

장점:

- 장기 운영 구조에서 busy, cancellation, scheduling, audit, command policy를 Runtime 쪽에서 통합할 수 있다.
- command execution history나 운영 안정성 정책을 일관되게 만들 수 있다.

위험:

- 현재 Runtime은 vendor-neutral polling/snapshot 중심이다.
- Pilot transaction detail이 RuntimeSnapshot / ChannelPollingResult / Runtime command result에 빨려 들어갈 위험이 있다.
- Runtime project가 `PilotFlows.Xgt` 또는 DB concrete를 알게 되면 Runtime vendor-neutral invariant가 깨진다.
- 지금 단계에서는 manual pilot WPF bridge보다 범위가 크다.

판정:

- 장기 후보로 보류.
- AH-PILOT-DI-01 / DI-02에서는 Runtime command dispatcher를 구현하지 않는다.
- Runtime과 Pilot DI composition은 현재 분리한다.

## 7. Test/Demo composition 후보 검토

후보 E는 test/demo composition만 먼저 추가하는 방식이다.

가능한 형태:

```text
PilotComposition Demo/Fake profile
    creates fake/demo IWorkStartExecutionService
    no real XGT
    no real DB
    no App.xaml wiring
```

장점:

- WPF shell을 실제 service instance와 연결할 수 있는 길을 열 수 있다.
- real PLC / real DB 연결 전에도 UI command state를 검증할 수 있다.
- 안전 profile 명칭을 명확히 하면 production concrete와 혼동을 줄일 수 있다.

위험:

- demo/fake가 production path처럼 보이면 운영 위험이 생긴다.
- FakePlc를 WPF production dependency로 넣으면 test harness 경계가 흐려진다.

판정:

- AH-PILOT-DI-02 최소 후보로 적절하다.
- 단, FakePlc 직접 dependency가 아니라 fake/demo `IWorkStartExecutionService` 또는 fake `IWorkStartFlowRunner` 수준으로 시작한다.
- FakePlc를 쓰는 full transaction harness는 계속 test project 또는 별도 dev harness executable 후보에 남긴다.

## 8. Profile 구분 후보

Profile 후보:

1. `FakeProfile`
   - fake service 또는 fake runner
   - fake DB
   - WPF UI demo / unit test
   - FakePlc 직접 포함은 기본값으로 두지 않는다.

2. `LocalXgtFakeDbProfile`
   - real XGT adapter 또는 XGT session graph
   - fake DB query
   - FakePlc를 사용할 경우 production WPF dependency가 아니라 별도 harness/dev executable 또는 clearly marked local harness path로 제한한다.

3. `RealPilotProfile`
   - real XGT adapter
   - real DB query
   - safety gated
   - opt-in config, visible risk label, environment guard가 필요하다.

Profile selection 후보:

- appsettings: 장기적으로 가능하지만, 현재 단계에서는 구현하지 않는다.
- command-line: dev/harness executable에는 유용하다.
- environment variable: local/demo opt-in에는 가능하지만 오작동 방지를 위한 명확한 default가 필요하다.
- compile flag: profile drift 위험이 있으므로 primary selector로는 비권장.

판정:

- 지금은 profile 구현을 하지 않고 후보만 기록한다.
- 기본 원칙은 fake/demo default, real profile explicit opt-in이다.
- real PLC / real DB profile은 safety gate 없이는 WPF에서 활성화하지 않는다.

## 9. DB concrete 위치 판단

현재 DB concrete는 없다. `PilotFlows` core에는 `IWorkStartDataQuery` interface만 있다.

판정:

- DB query implementation은 `PilotApp`이나 WPF가 아니라 별도 adapter/concrete project에 둔다.
- 후보 이름은 `CAAutomationHub.PilotFlows.Db`, `CAAutomationHub.PilotData.Sql`, `CAAutomationHub.PilotComposition.Sql` 등이 될 수 있다.
- `PilotFlows` core는 DB-free를 유지한다.
- `PilotApp`은 DB concrete를 알지 않는다.
- WPF는 DB concrete와 `Microsoft.Data.SqlClient`를 알지 않는다.
- `PilotComposition`은 장기적으로 DB adapter project를 조립할 수 있다.

## 10. FakePlc와 WPF 관계

현재 FakePlc reference는 test project에만 있다.

판정:

- FakePlc는 production WPF dependency로 넣지 않는다.
- WPF demo mode에는 fake `IWorkStartExecutionService` 또는 fake runner를 사용할 수 있다.
- FakePlc 기반 full transaction은 test harness 또는 별도 dev harness executable에서 유지하는 것이 안전하다.
- LocalXgt/FakePlc를 WPF 앱에서 직접 실행하는 구조는 production artifact와 혼동될 수 있으므로 현재 단계에서는 보류한다.

## 11. AH-PILOT-DI-02 후보

후보 1: Pilot Composition Boundary Skeleton

- 새 composition boundary shape를 정의한다.
- no real XGT / no real DB.
- fake/demo `IWorkStartExecutionService` factory 또는 profile model을 최소로 둔다.
- WPF App.xaml wiring은 하지 않는다.

판정: 권장.

후보 2: PilotApp fake composition helper

- project 수 증가 없음.
- WPF가 `PilotApp`만 참조하는 상태를 유지하기 쉽다.
- 하지만 fake composition이 application boundary에 섞인다.

판정: 제한적 후보. concrete-free test helper 수준이 아니면 비권장.

후보 3: WPF test-only composition

- production code 변경이 없다.
- 현재 WPF tests의 fake service 방식과 잘 맞는다.
- 실제 app wiring으로 이어지는 길은 약하다.

판정: 보조 후보.

후보 4: 바로 App.xaml DI wiring

- 실제 화면 연결까지 빠르다.
- XGT/DB/FakePlc concrete boundary가 동시에 열린다.

판정: 비권장. AH-PILOT-DI-02에서는 제외한다.

## 12. 권장안

권장 방향:

1. WPF는 계속 `PilotApp` application contract를 중심으로 둔다.
2. WPF가 `PilotFlows.Xgt`, `XgtDriverCore`, `FakePlc`, DB concrete, SqlClient를 직접 참조하지 않는다.
3. 실제 concrete composition은 별도 `CAAutomationHub.PilotComposition` 계층 후보로 분리한다.
4. `PilotComposition`은 장기적으로 `PilotApp`, `PilotFlows`, `PilotFlows.Xgt`, XGT driver, DB adapter를 조립한다.
5. `PilotApp`은 application boundary / DTO / execution service 책임만 유지한다.
6. Runtime command dispatcher는 장기 후보로 남기되 현재 manual pilot DI composition에서는 분리한다.
7. AH-PILOT-DI-02는 real DI가 아니라 Pilot Composition Boundary Skeleton 또는 Demo/Fake composition skeleton으로 진행한다.
8. App.xaml / actual DI wiring은 AH-PILOT-DI-03 이후로 미룬다.
9. FakePlc는 production WPF dependency가 아니라 test / demo / harness profile에만 둔다.
10. RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult에는 Pilot transaction detail을 넣지 않는다.

## 13. 제외한 범위

이번 단계에서 제외한 범위:

- DI registration 구현
- App.xaml / App.xaml.cs 수정
- WPF ViewModel 수정
- Dashboard 연결
- PilotComposition project 생성
- ProjectReference 추가
- PackageReference 추가
- solution 수정
- real XGT / DB concrete 조립
- FakePlc WPF production dependency 추가
- Runtime command dispatcher 구현
- RuntimeSnapshot 수정
- DashboardSnapshot 수정
- ChannelPollingResult 수정
- FlowDefinitions 수정
- FLOW.JSON / Flow Executor 연결
- ContextPublisher 자동 publish 재도입
- commit 생성

## 14. 실행한 명령

조사 명령:

```powershell
git log --oneline -10
git status --short
rg -n "ServiceProvider|Host|IHost|HostBuilder|ConfigureServices|AddSingleton|AddTransient|AddScoped|DI|Composition|App.xaml|Startup|Application" src tests docs/harness
rg -n "IWorkStartExecutionService|WorkStartExecutionService|WorkStartFlowServiceRunner|WorkStartXgtPlcOperations|IWorkStartDataQuery|WorkStartPilotViewModel" src tests
rg -n "XgtDriverCore|FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient" src/CAAutomationHub.Wpf src/CAAutomationHub.PilotApp src/CAAutomationHub.Runtime src/CAAutomationHub.FlowDefinitions
Get-Content src\CAAutomationHub.Wpf\CAAutomationHub.Wpf.csproj
Get-Content src\CAAutomationHub.PilotApp\CAAutomationHub.PilotApp.csproj
Get-Content src\CAAutomationHub.PilotFlows\CAAutomationHub.PilotFlows.csproj
Get-Content src\CAAutomationHub.PilotFlows.Xgt\CAAutomationHub.PilotFlows.Xgt.csproj
Get-Content src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj
Get-Content src\CAAutomationHub.FlowDefinitions\CAAutomationHub.FlowDefinitions.csproj
Get-Content src\CAAutomationHub.Wpf\App.xaml.cs
Get-Content src\CAAutomationHub.Wpf\App.xaml
Get-Content src\CAAutomationHub.Wpf\ViewModels\Pilot\WorkStartPilotViewModel.cs
Get-Content src\CAAutomationHub.Wpf\Views\Pilot\WorkStartPilotView.xaml
Get-Content src\CAAutomationHub.PilotApp\WorkStart\WorkStartExecutionService.cs
Get-Content src\CAAutomationHub.PilotApp\WorkStart\WorkStartFlowServiceRunner.cs
Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowService.cs
Get-Content src\CAAutomationHub.PilotFlows\WorkStart\IWorkStartDataQuery.cs
Get-Content src\CAAutomationHub.PilotFlows\WorkStart\IWorkStartPlcOperations.cs
Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtPlcOperations.cs
Get-Content tests\CAAutomationHub.PilotApp.Tests\CAAutomationHub.PilotApp.Tests.csproj
Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj
rg -n "Summary|판정|권장|DI|composition|App.xaml|PilotApp|PilotFlows.Xgt|FakePlc|RuntimeSnapshot|ChannelPollingResult|Dashboard" docs/harness/AH-PILOT-WPF-01.md docs/harness/AH-PILOT-WPF-02.md docs/harness/AH-PILOT-WPF-03.md docs/harness/AH-PILOT-WPF-04.md docs/harness/AH-PILOT-WPF-05.md docs/harness/AH-PILOT-WPF-06.md
rg -n "Summary|판정|권장|DI|composition|App.xaml|PilotApp|PilotFlows.Xgt|FakePlc|RuntimeSnapshot|ChannelPollingResult|Dashboard" docs/harness/AH-PILOT-WPF-07.md docs/harness/AH-PILOT-WPF-08.md docs/harness/AH-PILOT-WPF-09.md docs/harness/AH-PILOT-WPF-10.md docs/harness/AH-PILOT-22.md docs/harness/AH-PILOT-24.md
rg -n "ProjectReference|PackageReference" src/CAAutomationHub.Wpf src/CAAutomationHub.PilotApp src/CAAutomationHub.PilotFlows src/CAAutomationHub.PilotFlows.Xgt src/CAAutomationHub.Runtime src/CAAutomationHub.FlowDefinitions tests/CAAutomationHub.Wpf.Tests tests/CAAutomationHub.PilotApp.Tests tests/CAAutomationHub.PilotFlows.Tests tests/CAAutomationHub.PilotFlows.Xgt.Tests tests/CAAutomationHub.Runtime.Tests
rg -n "DashboardRuntimeCompositionFactory|DashboardRuntimeComposition|DashboardRuntimeMode" src/CAAutomationHub.Wpf tests/CAAutomationHub.Wpf.Tests
Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md
Get-Content docs\context\COGNITIVE_SYNC_CHECK.md
Test-Path docs\harness\AH-PILOT-DI-01.md
rg -n "WorkStart|Pilot|XgtDriverCore|FakePlc|SqlClient|RuntimeSnapshot|ChannelPollingResult" src\CAAutomationHub.Runtime src\CAAutomationHub.FlowDefinitions
```

Validation 명령:

```powershell
git diff -- docs/harness/AH-PILOT-DI-01.md
git diff --check
git status --short
Get-Content docs/harness/AH-PILOT-DI-01.md
```

Validation 관찰:

- `git diff -- docs/harness/AH-PILOT-DI-01.md`: output 없음. 파일이 untracked 상태라 content diff는 표시되지 않았다.
- `Get-Content docs/harness/AH-PILOT-DI-01.md`: closeout 문서 내용 출력 확인.

테스트 / 빌드:

- 문서 작성만 수행했으므로 실행하지 않았다.

## 15. git diff --check 결과

결과: PASS

```text
exit code 0, output 없음
```

## 16. git status --short 결과

결과:

```text
?? docs/harness/AH-PILOT-DI-01.md
```

## 17. Self-Check

판정: `ACCEPT`

확인:

- Closeout 문서만 추가했다.
- App.xaml / App.xaml.cs를 수정하지 않았다.
- DI registration을 구현하지 않았다.
- PilotComposition project를 생성하지 않았다.
- WPF project reference를 수정하지 않았다.
- PackageReference를 추가하지 않았다.
- 실제 XGT / DB concrete를 조립하지 않았다.
- Runtime command dispatcher를 구현하지 않았다.
- FakePlc를 WPF production dependency로 넣지 않았다.
- WPF는 계속 `PilotApp` boundary만 직접 바라본다.
- `PilotApp`은 `PilotFlows`만 참조한다.
- `PilotFlows` core는 XGT-free / Runtime-free / DB concrete-free 상태를 유지한다.
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult를 수정하지 않았다.
- Harness / Boundary / Validation 흐름은 문서 closeout과 검증 명령으로 유지했다.

남은 리스크:

- AH-PILOT-DI-02에서 실제 project skeleton을 만들 경우 WPF가 composition project를 참조하는 방식과 public API surface를 다시 좁게 검토해야 한다.
- DB concrete project 이름과 ownership은 아직 확정하지 않았다.
- real profile safety gate 정책은 아직 구현 전 후보 상태다.
