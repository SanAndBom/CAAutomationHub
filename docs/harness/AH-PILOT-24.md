# AH-PILOT-24 Closeout - Pilot Execution Path Boundary Audit

## 1. Summary

AH-PILOT-24는 AH-PILOT-22와 AH-PILOT-WPF-07 이후 pilot execution path가 boundary를 유지하는지 확인한 read-only audit stage다.

감사 결과 판정은 `ACCEPT`다.

확인한 핵심:

- WPF ViewModel은 `IWorkStartExecutionService`만 호출한다.
- WPF는 XGT / FakePlc / DB concrete를 직접 참조하지 않는다.
- PilotApp production project는 Runtime을 참조하지 않는다.
- PilotFlows core는 XGT를 모른다.
- XGT concrete는 `PilotFlows.Xgt`와 test harness에만 있다.
- FakePlc는 test-only reference로 유지된다.
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult는 이번 Mini Program에서 수정되지 않았다.
- full fake transaction evidence가 PilotApp level까지 추가되었다.
- command boundary는 XAML / DI 없이 ViewModel tests로 고정되었다.

## 2. Audit 대상

Audit 대상:

- `src/CAAutomationHub.Wpf/ViewModels/Pilot/WorkStartPilotViewModel.cs`
- `tests/CAAutomationHub.Wpf.Tests/ViewModels/WorkStartPilotViewModelTests.cs`
- `src/CAAutomationHub.PilotApp/WorkStart/*`
- `tests/CAAutomationHub.PilotApp.Tests/WorkStart/WorkStartExecutionServiceFakePlcIntegrationTests.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/*`
- `src/CAAutomationHub.PilotFlows.Xgt/WorkStart/*`
- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/WorkStart/*`
- project/package reference graph
- Runtime / WPF dashboard snapshot and polling contracts

## 3. Execution Path 확인

현재 검증된 path:

```text
WPF WorkStartPilotViewModel
  -> IWorkStartExecutionService
  -> WorkStartExecutionService
  -> WorkStartFlowServiceRunner
  -> WorkStartFlowService
  -> IWorkStartPlcOperations
  -> WorkStartXgtPlcOperations
  -> XgtSession / TcpTransport
  -> in-process FakePlc harness
```

WPF-level:

- `WorkStartPilotViewModel.ExecuteOnceAsync`는 `IWorkStartExecutionService.ExecuteOnceAsync`만 호출한다.
- `ExecuteOnceCommand`는 기존 `ExecuteOnceAsync`를 시작하는 command boundary일 뿐이다.
- XAML binding은 아직 없다.

PilotApp-level:

- `WorkStartExecutionService`는 `IWorkStartFlowRunner`만 호출한다.
- `WorkStartFlowServiceRunner`가 `WorkStartFlowService`로 application boundary를 연결한다.
- PilotApp production project는 XGT, FakePlc, Runtime, DB concrete를 직접 참조하지 않는다.

PilotFlows-level:

- `WorkStartFlowService`는 `IWorkStartPlcOperations`와 `IWorkStartDataQuery` abstraction만 사용한다.
- PilotFlows core는 XGT / FakePlc / DB concrete를 모른다.

PilotFlows.Xgt-level:

- `WorkStartXgtPlcOperations`만 XGT driver protocol / transport boundary를 사용한다.
- FakePlc는 production `PilotFlows.Xgt` project에 reference되지 않는다.

## 4. Project Reference Boundary

Project reference scan:

```text
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
```

확인 결과:

- `src/CAAutomationHub.Wpf`는 `CAAutomationHub.Contracts`, `CAAutomationHub.PilotApp`, `CAAutomationHub.Runtime`를 참조한다.
- `src/CAAutomationHub.Wpf`는 `CAAutomationHub.PilotFlows.Xgt`, `AutomationHub.XgtDriverCore`, `AutomationHub.XgtDriverCore.FakePlc`, `XgtChannelRunner`, `Microsoft.Data.SqlClient`를 참조하지 않는다.
- `src/CAAutomationHub.PilotApp`는 `CAAutomationHub.PilotFlows`만 참조한다.
- `src/CAAutomationHub.PilotFlows`는 vendor concrete를 참조하지 않는다.
- `src/CAAutomationHub.PilotFlows.Xgt`만 `AutomationHub.XgtDriverCore`를 참조한다.
- `tests/CAAutomationHub.PilotApp.Tests`와 `tests/CAAutomationHub.PilotFlows.Xgt.Tests`만 FakePlc test harness project를 참조한다.
- Runtime project reference 변경 없음.
- FlowDefinitions project reference 변경 없음.
- Microsoft.Data.SqlClient package 추가 없음.

## 5. Runtime / WPF / Pilot Separation

유지된 분리:

- Runtime은 canonical state / polling state path를 유지한다.
- Pilot business transaction detail은 Runtime polling contract에 섞이지 않았다.
- `RuntimeSnapshot`, `DashboardSnapshot`, `ChannelPollingResult`는 이번 Mini Program에서 수정되지 않았다.
- WPF DashboardView / DashboardViewModel / App.xaml / DI composition root는 수정되지 않았다.
- WPF Pilot ViewModel은 PilotApp application service boundary만 본다.
- 실제 현장 버튼 연결은 아직 하지 않았다.

## 6. FakePlc Test-only 확인

FakePlc reference 확인:

- `tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj`
- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj`

FakePlc source usage:

- PilotApp-level full transaction harness
- PilotFlows.Xgt-level driver/operation integration harness

Production source에는 FakePlc reference가 없다.

## 7. Tests / Build 결과

실행:

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj
dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj
dotnet test tests/CAAutomationHub.PilotFlows.Tests/CAAutomationHub.PilotFlows.Tests.csproj
dotnet test tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj
dotnet test tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj
```

결과:

```text
WPF tests: pass, failed 0, passed 222, skipped 0
PilotApp tests: pass, failed 0, passed 8, skipped 0
PilotFlows tests: pass, failed 0, passed 40, skipped 0
PilotFlows.Xgt tests: pass, failed 0, passed 39, skipped 0
Runtime tests: pass, failed 0, passed 142, skipped 0
```

참고:

- 최초 병렬 test run 중 PilotApp test restore/build에서 MSBuild cache file warning이 1회 발생했다.
- `dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj`를 단독 재실행했고 warning 없이 통과했다.

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
git diff --check
```

결과:

```text
exit code 0
output 없음
```

실행:

```text
git status --short
```

결과:

```text
output 없음
```

실행:

```text
rg -n "XgtDriverCore|FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|DashboardSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src tests -g '!bin/**' -g '!obj/**'
```

결과 요약:

- XGT driver usage는 `src/CAAutomationHub.PilotFlows.Xgt`와 관련 tests에 한정된다.
- FakePlc usage는 tests에 한정된다.
- `XgtChannelRunner`, `SqlConnection`, `Microsoft.Data.SqlClient`, `FlowExecutor`, `FLOW.JSON` 신규 오염 없음.
- RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult hit는 기존 Runtime / WPF dashboard / polling contracts와 tests에서 존재한다.
- PilotApp production code와 WPF Pilot ViewModel에는 RuntimeSnapshot / DashboardSnapshot / ChannelPollingResult coupling 없음.

## 9. 남은 리스크

남은 리스크:

- `ExecuteOnceCommand`는 command boundary만 제공하며 아직 XAML button에 binding되지 않았다.
- DI composition root가 없으므로 실제 app 실행 시 `IWorkStartExecutionService` concrete wiring은 아직 없다.
- PilotApp-level FakePlc harness는 actual PLC / real DB를 검증하지 않는다.
- 실제 UI wiring 전에 user operation log / transaction history boundary를 별도 검토할 필요가 있다.
- 향후 real PLC 준비 단계에서 actual transport / timeout / recovery policy가 PilotApp으로 누수되지 않도록 boundary review가 필요하다.

## 10. 다음 후보

권장 다음 후보:

- AH-PILOT-WPF-08: XAML button binding boundary review 또는 minimal binding stage
- AH-PILOT-25: PilotApp/FakePlc failure scenario enhancement
- AH-PILOT-DI-01: DI composition boundary review
- AH-PILOT-LOG-01: Pilot transaction history / operation log boundary review

아직 하지 않을 것:

- actual PLC
- real DB
- DI wiring
- RuntimeSnapshot 반영
- FLOW.JSON executor
- Dashboard 대규모 수정

## 11. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-22와 AH-PILOT-WPF-07이 각각 closeout/commit 이후 clean state로 유지되었다.
- fresh tests와 solution build를 실행했다.
- boundary scan과 project reference scan을 실행했다.
- WPF -> PilotApp -> PilotFlows -> PilotFlows.Xgt -> FakePlc harness path가 증거로 연결되었다.
- Runtime / WPF / Pilot / XGT / FakePlc 책임 경계가 유지되었다.
- FakePlc는 test-only harness로 유지된다.
- ContextPublisher 자동 publish는 재도입하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
