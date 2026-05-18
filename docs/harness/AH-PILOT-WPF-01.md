# AH-PILOT-WPF-01 Closeout - PilotFlow Runtime/WPF Bridge Boundary Review

## 1. Summary

AH-PILOT-WPF-01은 WorkStart PilotFlow 결과를 Runtime/WPF에 어떻게 연결할지 결정하기 위한 read-only Boundary Review다.

결론은 `WorkStartFlowResult`를 `RuntimeSnapshot` 또는 `ChannelPollingResult`에 직접 넣지 않는 것이다. `RuntimeSnapshot`은 PLC channel state / health / polling snapshot 중심의 canonical Runtime state로 유지하고, Pilot transaction 결과는 별도 Pilot operation log / transaction result stream / WPF display model 경로로 발행하는 것이 안전하다.

초기 WPF 연결은 `WPF ViewModel -> Pilot Application Service -> WorkStartFlowService` 구조가 가장 작고 안전하다. WPF가 `WorkStartFlowService`, `WorkStartXgtPlcOperations`, XGT session, FakePlc, DB concrete를 직접 조립하지 않도록 얇은 application service boundary를 먼저 두는 방향을 권장한다.

이번 작업에서는 production code, test code, WPF ViewModel, Runtime Snapshot, PilotFlow event model, csproj, solution, reference, DI wiring을 수정하지 않았다. 산출물은 이 closeout 문서뿐이다.

## 2. 현재 Runtime / WPF / PilotFlow 상태

### Runtime

- `RuntimeSnapshot`은 `CapturedAt`, `Health`, `Channels`, `RecentEvents`를 가진다.
- `ChannelRuntimeState`는 PLC channel 관측 상태, health, polling, latency, failure count, reconnect count 중심이다.
- `ChannelPollingResult`는 PLC-level vendor-neutral polling event result이며 `PlcId`, `OccurredAt`, success/failure, response time, failure kind, error message만 가진다.
- `PollingCycleCoordinator`는 이미 만들어진 `ChannelPollingResult` batch를 single-writer publish path로 넘기는 boundary다.
- `PollingResultStateOrchestrator`는 `ChannelPollingResult`를 `RuntimePlcChannelState`로 낮춘 뒤 `PollingPublishCoordinator`에 위임한다.
- Runtime core는 XGT, FakePlc, DB, payload, LOT ID, ACK/error write policy detail을 소유하지 않는다.

### WPF

- 현재 Dashboard 흐름은 `DashboardViewModel -> IRuntimeDashboardAdapter -> RuntimeDashboardAdapter -> IRuntimeSnapshotProvider -> RuntimeDashboardSnapshotMapper -> DashboardSnapshot`이다.
- `RuntimeSnapshot`과 `DashboardSnapshot`은 분리되어 있다.
- `DashboardViewModel`은 optional `IRuntimeDashboardEventSource.SnapshotChanged`만 구독하고, `EventReceived`는 구독하지 않는다.
- `RealtimeEventLogViewModel`은 현재 `IEventStreamService`를 통해 `RuntimeEventLogItem`을 받으며, 기본 구현은 `FakeEventStreamService`다.
- `RuntimeEventLogItemMapper`는 `RuntimeDashboardEvent`를 WPF log item으로 변환할 수 있다.
- WPF에는 아직 Pilot command button, WorkStart result panel, Pilot transaction history model이 없다.

### PilotFlow

- `WorkStartFlowService`는 `IWorkStartPlcOperations`, `IWorkStartDataQuery`, payload builder, flow options를 조합해 WorkStart business transaction을 실행한다.
- `WorkStartFlowResult`는 `Status`, `Step`, `ErrorCode`, `Message`, `SelectedLotId`, `ErrorWriteExpected`를 제공한다.
- `PilotFlows.Xgt`는 XGT-specific adapter layer이며 `WorkStartXgtPlcOperations`가 XGT session read/write를 `IWorkStartPlcOperations`로 낮춘다.
- FakePlc integration은 test-only 영역에서 happy transaction, DB failure transaction, read NAK / 1101 failure를 검증했다.
- PilotFlows core는 `RuntimeSnapshot` / `ChannelPollingResult`를 참조하지 않는다.

## 3. WorkStartFlowResult와 RuntimeSnapshot 관계

판정:

- `WorkStartFlowResult`를 `RuntimeSnapshot`에 직접 넣지 않는다.
- `WorkStartFlowResult`를 `ChannelPollingResult`에 넣지 않는다.
- `DashboardSnapshot`에도 WorkStart domain result를 직접 흡수하지 않는 편이 안전하다.

이유:

- `RuntimeSnapshot`은 canonical Runtime state이지만, 그 의미는 PLC channel state / health / polling snapshot 중심이다.
- `WorkStartFlowResult`는 하나의 business transaction 실행 결과다. DB result, LOT ID, ACK/error write policy, selected step 같은 Pilot business detail을 포함한다.
- 이를 `RuntimeSnapshot`에 넣으면 Dashboard PLC card 상태와 업무 transaction 상태가 섞인다.
- 이를 `ChannelPollingResult`에 넣으면 PLC-level polling result가 business transaction result로 변질된다.
- 기존 AH-RUNTIME-31~40, AH-RUNTIME-48~50, AH-RUNTIME-57~60 흐름은 Runtime polling path와 Pilot business flow path를 반복해서 분리해 왔다.

허용 후보:

- Runtime `RecentEvents` 또는 WPF `RuntimeDashboardEvent`와 유사한 display event shape를 참고할 수는 있다.
- 그러나 PilotFlow event는 Runtime core event와 직접 동일 모델로 섞기 전에 별도 Pilot operation event boundary를 먼저 둬야 한다.

## 4. Dashboard PLC Card와 Pilot transaction 상태 분리

판정:

- Dashboard PLC Card는 Runtime channel health 중심으로 유지한다.
- Pilot transaction 상태는 별도 panel / log / transaction history로 표시한다.

PLC Card에 남겨야 할 정보:

- connected / disconnected / inactive / warning / error 같은 channel health
- RTT / last response / polling interval / failure count
- last success / last failure / reconnect count
- Runtime sequence fallback state

Pilot transaction에 남겨야 할 정보:

- WorkStart requested / running / succeeded / failed
- selected LOT ID
- failed step
- error code
- display message
- error write expected 여부
- transaction timestamp / duration / plc id / command id

주의:

- DB not found, DB multiple rows, payload build failure는 PLC channel 장애로 표시하면 운영자가 원인을 오해할 수 있다.
- transport failure로 WorkStart가 실패한 경우에는 Runtime channel health에 간접 영향이 있을 수 있다. 그래도 Pilot result 자체는 PLC Card state를 직접 덮어쓰지 않는다.

## 5. Realtime Log / Operation Log 경로

권장 경로:

```text
WorkStartFlowService
    -> Pilot Application Service
    -> PilotOperationEvent / PilotTransactionRecord
    -> WPF Pilot operation stream adapter
    -> Realtime Log 또는 WorkStart Transaction panel
```

Realtime Log 표시 후보:

- `WorkStartRequested`
- `ReadBlockStarted`
- `LotIdSelected`
- `DbQueryStarted`
- `DbQueryFailed`
- `PayloadWriteSucceeded`
- `AckWriteSucceeded`
- `ErrorCodeWritten`
- `WorkStartSucceeded`
- `WorkStartFailed`

판정:

- 위 이벤트는 Runtime polling event가 아니라 Pilot operation event다.
- `IRuntimeDashboardEventSource.EventReceived`에 바로 끼워 넣기보다, `IPilotOperationEventSource` 또는 `IWorkStartOperationLog` 같은 별도 boundary 후보를 먼저 검토한다.
- WPF Realtime Log에 같이 보여야 한다면 mapper layer에서 `PilotOperationEvent -> RuntimeEventLogItem` 또는 별도 `PilotEventLogItem`으로 변환한다.

이유:

- snapshot refresh는 latest-only coalescing이 맞지만, operation log는 ordering / rolling buffer / duplicate policy가 필요하다.
- AH-WPF-21~23도 `SnapshotChanged`와 `EventReceived`는 같은 orchestrator에 섞으면 안 된다고 기록했다.

## 6. WPF command boundary

검토 후보:

### 후보 A: WPF ViewModel -> Pilot Application Service -> WorkStartFlowService

판정: 초기 pilot 권장.

장점:

- 구현 단위가 작다.
- Runtime core를 오염시키지 않는다.
- WPF가 concrete XGT adapter / DB query / payload detail을 직접 조립하지 않는다.
- manual "착공 1회 실행" 버튼에 맞다.

위험:

- 장기 command audit / scheduling / supervisor coordination은 아직 약하다.
- busy / duplicate execution / cancellation 정책을 application service가 명확히 가져야 한다.

### 후보 B: WPF ViewModel -> Runtime Command Dispatcher -> Pilot Application Service

판정: 장기 후보.

장점:

- command audit, cancellation, concurrency, authorization, lifecycle 통합에 유리하다.
- Runtime/Supervisor 중심 운영 모델과 맞을 수 있다.

위험:

- 현재 manual pilot에는 무겁다.
- Runtime command dispatcher를 너무 빨리 만들면 Pilot business flow가 Runtime core로 빨려 들어갈 수 있다.

### 후보 C: WPF ViewModel -> WorkStartFlowService 직접 호출

판정: 비권장.

이유:

- WPF가 service construction, XGT operations, DB query, options, busy state를 직접 알게 된다.
- WPF가 `WorkStartXgtPlcOperations` concrete를 생성할 압력이 생긴다.
- UI가 adapter / driver / DB 책임을 침범하기 쉽다.

### 후보 D: HostedService / scheduler가 WorkStartFlowService 호출, WPF는 trigger만 요청

판정: 자동 운영 flow 후보.

장점:

- polling request detection, queue, scheduler, recovery와 맞다.

위험:

- 초기 manual "착공 1회 실행"에는 과하다.
- Runtime HostedService로 바로 넣으면 Runtime polling path와 Pilot business transaction path가 섞일 수 있다.

## 7. Pilot Application Service 후보

필요성:

- `WorkStartFlowService`는 business transaction orchestration service다.
- WPF가 XGT operations / DB query / options / log publishing / result mapping을 직접 조립하지 않도록 application boundary가 필요하다.

후보 이름:

- `WorkStartPilotApplicationService`
- `WorkStartExecutionService`
- `PilotCommandService`
- `IWorkStartExecutionService`

권장 초기 shape:

```text
IWorkStartExecutionService
    ExecuteOnceAsync(WorkStartExecutionRequest request, CancellationToken)
        -> WorkStartExecutionResult
        -> emits PilotOperationEvent records
```

초기 책임:

- duplicate execution / busy guard
- cancellation propagation
- selected PLC / target context 해석
- `WorkStartFlowService` 호출
- duration / requestedAt / completedAt / commandId / plcId 부여
- `WorkStartFlowResult`를 WPF-friendly result로 변환
- operation log event 발행

가지면 안 되는 책임:

- Runtime polling state update
- `ChannelPollingResult` 생성
- `RuntimeSnapshot` 수정
- WPF control 직접 조작
- FakePlc 직접 참조
- XGT protocol primitive 직접 구현

## 8. WPF result DTO / ViewModel 후보

판정:

- `WorkStartFlowResult`는 domain result로 유지한다.
- WPF는 별도 display model 또는 ViewModel로 변환한다.

후보 DTO:

- `WorkStartExecutionResult`
- `PilotTransactionResult`
- `WorkStartResultViewModel`
- `PilotTransactionHistoryItem`

필드 후보:

- `CommandId`
- `PlcId`
- `RequestedAt`
- `CompletedAt`
- `Duration`
- `Status`
- `Step`
- `ErrorCode`
- `Message`
- `SelectedLotId`
- `ErrorWriteExpected`
- `DisplaySeverity`
- `DisplayStatus`

이유:

- `WorkStartFlowResult`에는 timestamp / duration / plc id / operator action id가 없다.
- WPF는 localized display text, severity, filtering, history ordering이 필요하다.
- application layer가 domain result에 operation metadata를 붙이고, WPF mapper가 display model로 낮추는 구조가 안전하다.

## 9. AH-PILOT-WPF-02 후보

후보 A: PilotFlow Runtime/WPF Bridge 문서 상세화

- 가능하지만 AH-PILOT-WPF-01에서 이미 핵심 방향을 정리했다.

후보 B: WorkStartExecutionResult DTO / display model skeleton

- 유용하지만 service boundary 없이 DTO부터 만들면 WPF display shape가 먼저 굳을 수 있다.

후보 C: Pilot Application Service boundary skeleton

- 권장.
- `IWorkStartExecutionService`, request/result DTO, fake implementation 또는 unit seam을 먼저 세우면 WPF가 호출할 최소 연결점이 생긴다.
- 실제 UI 구현 없이 boundary test를 만들 수 있다.

후보 D: WPF fake ViewModel test with fake application service

- C 이후에 적절하다.
- ViewModel busy/cancellation/result display 정책을 검증할 때 사용한다.

후보 E: RealtimeLog mapper skeleton

- 필요하지만 event model이 먼저 정리되어야 한다.

권장 AH-PILOT-WPF-02:

- `Pilot Application Service Boundary Skeleton`
- 범위는 service interface / request-result DTO / fake or test double 기반 boundary test까지로 제한한다.
- XGT concrete, DB concrete, DI wiring, WPF UI button은 후속으로 미룬다.

## 10. 권장안

1. `RuntimeSnapshot`에 `WorkStartFlowResult`를 직접 넣지 않는다.
2. `ChannelPollingResult`에 LOT ID, DB result, ACK policy, Pilot transaction result를 넣지 않는다.
3. Dashboard PLC Card는 Runtime channel health / polling state 중심으로 유지한다.
4. Pilot transaction은 별도 operation log / transaction result stream / WorkStart result panel로 WPF에 제공한다.
5. 초기 manual pilot은 `WPF -> Pilot Application Service -> WorkStartFlowService` 구조로 간다.
6. WPF는 `WorkStartXgtPlcOperations` concrete, XGT session, FakePlc, DB concrete를 직접 만들지 않는다.
7. Pilot Application Service가 busy / duplicate execution / cancellation / timestamp / duration / result mapping을 담당한다.
8. Realtime Log 연결은 Runtime event bridge와 직접 섞지 않고 Pilot operation event adapter로 검토한다.
9. 장기적으로 Runtime Command Dispatcher 또는 Flow Executor와 통합할 수 있지만, AH-PILOT-WPF-02에서는 아직 구현하지 않는다.
10. AH-PILOT-WPF-02는 Pilot Application Service boundary skeleton이 가장 안전하다.

## 11. 제외한 범위

이번 작업에서 제외했다.

- RuntimeSnapshot 수정
- ChannelPollingResult 수정
- DashboardSnapshot 수정
- WPF ViewModel 수정
- WPF UI button 구현
- PilotFlow event model 추가
- Pilot Application Service 구현
- Runtime Command Dispatcher 구현
- Runtime HostedService 구현
- DI wiring 수정
- csproj / solution 수정
- ProjectReference 추가
- PackageReference 추가
- FLOW.JSON / Flow Executor 연결
- 테스트 수정
- 빌드 / 테스트 실행
- commit

## 12. 실행한 명령

Context / precheck:

- `git log --oneline -10`
- `git status --short`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content docs\context\WPF_RUNTIME_BRIDGE_CURRENT_STATE.md`
- `rg --files docs\harness | rg "AH-(WPF|RUNTIME|PILOT)"`
- `rg --files src\CAAutomationHub.Runtime src\CAAutomationHub.Contracts src\CAAutomationHub.Wpf src\CAAutomationHub.PilotFlows src\CAAutomationHub.PilotFlows.Xgt`
- `rg --files tests\CAAutomationHub.Wpf.Tests tests\CAAutomationHub.Runtime.Tests tests\CAAutomationHub.PilotFlows.Tests tests\CAAutomationHub.PilotFlows.Xgt.Tests`

Search / review:

- `rg -n "DashboardSnapshot|RuntimeSnapshot|SnapshotChanged|RealtimeLog|Log|Event|Publisher|Dashboard|ViewModel|Command|RuntimeHostedService|Pilot|WorkStart|Operation" src tests docs\harness docs\context`
- `rg -n "ChannelPollingResult|WorkStartFlowResult|RuntimePlcChannelState|PollingCycleCoordinator|RuntimeSnapshot" src tests docs\harness`
- `Select-String -Path docs\harness\AH-RUNTIME-31.md ... docs\harness\AH-RUNTIME-40.md -Pattern "Summary|결론|RuntimeSnapshot|DashboardSnapshot|ChannelPollingResult|PollingCycleCoordinator|Boundary|Self-Check|ACCEPT|publish|snapshot" -Context 1,2`
- `Get-Content docs\harness\AH-RUNTIME-50.md`
- `Get-Content docs\harness\AH-RUNTIME-57.md`
- `Get-Content docs\harness\AH-RUNTIME-58.md`
- `Get-Content docs\harness\AH-RUNTIME-59.md`
- `Get-Content docs\harness\AH-RUNTIME-60.md`
- `Get-Content docs\harness\AH-WPF-23.md`
- `Get-Content docs\harness\AH-WPF-24.md`
- `Get-Content docs\harness\AH-PILOT-05.md`
- `Get-Content docs\harness\AH-PILOT-08.md`
- `Get-Content docs\harness\AH-PILOT-12.md`
- `Get-Content docs\harness\AH-PILOT-18-A.md`
- `Get-Content docs\harness\AH-PILOT-18-B.md`
- `Get-Content docs\harness\AH-PILOT-20.md`
- `Get-Content docs\harness\AH-PILOT-21.md`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowResult.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowService.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\IWorkStartPlcOperations.cs`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtPlcOperations.cs`
- `Get-Content src\CAAutomationHub.Contracts\Runtime\RuntimeSnapshot.cs`
- `Get-Content src\CAAutomationHub.Contracts\Runtime\ChannelRuntimeState.cs`
- `Get-Content src\CAAutomationHub.Contracts\Runtime\Events\RuntimeEvent.cs`
- `Get-Content src\CAAutomationHub.Contracts\Runtime\Commands\RuntimeDashboardCommand.cs`
- `Get-Content src\CAAutomationHub.Contracts\Runtime\Commands\RuntimeDashboardCommandResult.cs`
- `Get-Content src\CAAutomationHub.Runtime\Polling\ChannelPollingResult.cs`
- `Get-Content src\CAAutomationHub.Runtime\Polling\PollingCycleCoordinator.cs`
- `Get-Content src\CAAutomationHub.Runtime\Polling\PollingResultStateOrchestrator.cs`
- `Get-Content src\CAAutomationHub.Runtime\IAutomationHubSupervisor.cs`
- `Get-Content src\CAAutomationHub.Wpf\Models\Dashboard\DashboardSnapshot.cs`
- `Get-Content src\CAAutomationHub.Wpf\Models\Dashboard\PlcCardSnapshot.cs`
- `Get-Content src\CAAutomationHub.Wpf\Models\Dashboard\RuntimeDashboardEvent.cs`
- `Get-Content src\CAAutomationHub.Wpf\Models\Dashboard\RuntimeEventLogItem.cs`
- `Get-Content src\CAAutomationHub.Wpf\Adapters\IRuntimeDashboardEventSource.cs`
- `Get-Content src\CAAutomationHub.Wpf\Adapters\RuntimeDashboardAdapter.cs`
- `Get-Content src\CAAutomationHub.Wpf\Mappers\RuntimeDashboardSnapshotMapper.cs`
- `Get-Content src\CAAutomationHub.Wpf\Mappers\RuntimeEventLogItemMapper.cs`
- `Get-Content src\CAAutomationHub.Wpf\ViewModels\DashboardViewModel.cs`
- `Get-Content src\CAAutomationHub.Wpf\ViewModels\RealtimeEventLogViewModel.cs`
- `Get-Content src\CAAutomationHub.Wpf\Services\IEventStreamService.cs`
- `Get-Content src\CAAutomationHub.Wpf\Services\FakeEventStreamService.cs`
- `Get-Content tests\CAAutomationHub.Wpf.Tests\Mappers\RuntimeEventLogItemMapperTests.cs`
- `Get-Content tests\CAAutomationHub.Wpf.Tests\ViewModels\DashboardViewModelEventRefreshTests.cs`
- `rg -n "WorkStart|Pilot|RuntimeDashboardCommand|ExecuteAsync|EventReceived|RealtimeEventLog|RefreshCommand|Command" src\CAAutomationHub.Wpf tests\CAAutomationHub.Wpf.Tests src\CAAutomationHub.Runtime tests\CAAutomationHub.Runtime.Tests`
- `rg -n "<ProjectReference|<PackageReference" src tests -g "*.csproj"`

Validation:

- `git diff -- docs/harness/AH-PILOT-WPF-01.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-WPF-01.md`

## 13. git diff --check 결과

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

- `git diff -- docs/harness/AH-PILOT-WPF-01.md`도 출력 없음.
- 이유: `docs/harness/AH-PILOT-WPF-01.md`는 신규 untracked 파일이라 plain `git diff -- <path>`에는 표시되지 않는다.
- 문서 내용 확인은 `Get-Content docs\harness\AH-PILOT-WPF-01.md`로 수행했다.

## 14. git status --short 결과

실행:

```text
git status --short
```

결과:

```text
?? docs/harness/AH-PILOT-WPF-01.md
```

## 15. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-WPF-01 목표인 Runtime/WPF/PilotFlow bridge boundary를 read-only로 검토했다.
- `WorkStartFlowResult`를 `RuntimeSnapshot` / `ChannelPollingResult`에 넣지 않는 결론을 기록했다.
- Dashboard PLC Card와 Pilot transaction 상태 분리 결론을 기록했다.
- Realtime Log / Operation Log 후보 경로를 Runtime event bridge와 분리해 기록했다.
- WPF command boundary 후보 A/B/C/D를 비교하고 초기 권장안을 정했다.
- Pilot Application Service와 WPF result DTO 후보를 정리했다.
- AH-PILOT-WPF-02 후보와 권장 다음 단계를 기록했다.
- production code, test code, WPF ViewModel, Runtime Snapshot, csproj, solution, DI wiring, references를 수정하지 않았다.
- ContextPublisher 자동 publish를 재도입하지 않았다.
- `git diff --check` 통과와 `git status --short` 결과를 기록했다.
- 문서 작성 작업이므로 테스트 / 빌드는 실행하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
