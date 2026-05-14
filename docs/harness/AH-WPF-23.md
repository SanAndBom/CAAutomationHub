# AH-WPF-23 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- WPF Runtime Bridge가 실제 Runtime/Supervisor에 연결되기 전 Supervisor Boundary를 검토한다.
- `RuntimeDashboardAdapter`, `IRuntimeSnapshotProvider`, `IRuntimeDashboardEventSource`가 어떤 Supervisor boundary를 봐야 하는지 결정한다.
- Runtime project skeleton scenario로 들어가기 전 책임 경계를 확정한다.

## 3. Current Structure Reviewed

### 3.1 IRuntimeSnapshotProvider
- `IRuntimeSnapshotProvider`는 WPF adapter-facing synchronous provider다.
- 현재 계약:
  - `RuntimeSnapshot GetSnapshot()`

### 3.2 RuntimeDashboardAdapter
- `RuntimeDashboardAdapter`는 다음을 구현한다.
  - `IRuntimeDashboardAdapter`
  - `IAsyncRuntimeDashboardAdapter`
- 현재 snapshot flow:
  - `IRuntimeSnapshotProvider.GetSnapshot()`을 호출한다.
  - 반환된 `RuntimeSnapshot`을 `RuntimeDashboardSnapshotMapper.Map(...)`으로 mapping한다.
  - `DashboardSnapshot`을 반환한다.
- 현재 async path는 synchronous path를 감싸며, 실제 async Runtime boundary가 연결되기 전까지만 유효하다.

### 3.3 IRuntimeDashboardEventSource
- `IRuntimeDashboardEventSource`는 현재 다음을 선언한다.
  - `SnapshotChanged`
  - `EventReceived`
- `RuntimeDashboardAdapter`는 아직 이 interface를 구현하지 않는다.

### 3.4 Contracts
- Runtime contracts는 현재 다음을 포함한다.
  - `RuntimeSnapshot`
  - `RuntimeEvent`
  - `RuntimeDashboardCommand`
  - `RuntimeDashboardCommandResult`
- Runtime contracts는 아직 snapshot revision 또는 sequence number를 포함하지 않는다.

### 3.5 DashboardViewModel
- `DashboardViewModel`은 optional `IRuntimeDashboardEventSource`를 감지한다.
- `SnapshotChanged`만 `DashboardRefreshOrchestrator`로 연결한다.
- `EventReceived`는 구독하거나 처리하지 않는다.
- 기존 `DispatcherTimer` pull refresh는 유지한다.

## 4. Supervisor Boundary Decision
- B option을 채택했다.
- Supervisor와 WPF adapter 사이에 별도 provider/bridge adapter를 둔다.
- `IAutomationHubSupervisor`는 WPF `IRuntimeSnapshotProvider`를 직접 구현하지 않는다.
- `RuntimeDashboardAdapter`는 `IAutomationHubSupervisor`를 깊게 알지 않는다.
- Supervisor는 Runtime layer의 async boundary로 남는다.
- WPF `RuntimeDashboardAdapter`는 Runtime snapshot을 Dashboard snapshot으로 변환하는 책임을 유지한다.

## 5. IAutomationHubSupervisor Candidate Contract

Candidate methods:
- `Task StartAsync(CancellationToken cancellationToken)`
- `Task StopAsync(CancellationToken cancellationToken)`
- `Task<RuntimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)`
- `Task<RuntimeDashboardCommandResult> ExecuteAsync(RuntimeDashboardCommand command, CancellationToken cancellationToken)`

Candidate events:
- `event EventHandler<RuntimeSnapshotChangedEventArgs>? SnapshotChanged`
- `event EventHandler<RuntimeEvent>? RuntimeEventRaised`

Policy:
- Supervisor는 `RuntimeSnapshot`을 노출하고 `DashboardSnapshot`은 노출하지 않는다.
- Supervisor는 Dashboard model을 모른다.
- Supervisor는 `RuntimeEvent`를 노출하고 `RuntimeEventLogItem`은 노출하지 않는다.
- Supervisor는 WPF event log model을 모른다.
- Lifecycle 책임은 Supervisor에 둘 수 있다.
- Process hosting과 DI lifetime은 필요하면 이후 `RuntimeHost`로 분리할 수 있다.

## 6. RuntimeSnapshotChangedEventArgs / Revision Policy
- Runtime/Supervisor boundary에는 `RuntimeSnapshotChangedEventArgs`가 필요하다.
- Candidate fields:
  - `RuntimeSnapshot Snapshot`
  - `DateTimeOffset OccurredAt`
  - `long? Revision`
- `Snapshot` payload는 필수다.
- `OccurredAt`은 event raise time을 의미한다.
- `Revision`은 장기적으로 유용하지만 `RuntimeSnapshot` 또는 `DashboardSnapshot`에 직접 추가하는 것은 보류한다.
- 현재 WPF stale protection은 `DashboardSnapshot.Health.SnapshotTime`을 사용한다.
- Runtime은 `RuntimeSnapshot.CapturedAt`과 `RuntimeHealthState.CapturedAt`을 일관되게 유지해야 한다.

## 7. RuntimeDashboardAdapter Event Source Direction
- 이 scenario에서는 event source 구현을 추가하지 않는다.
- 후속 구현에서는 `SnapshotChanged`만 먼저 연결하는 경로가 더 안전하다.
- 후속 설계에서는 `RuntimeDashboardAdapter`가 Supervisor `SnapshotChanged`를 구독하고, `RuntimeSnapshot`을 `DashboardSnapshot`으로 mapping한 뒤 `DashboardSnapshotChangedEventArgs`를 raise하는 구조를 검토해야 한다.
- `EventReceived`는 보류한다.
- Adapter event는 UI thread affinity를 보장하지 않는다.
- UI marshaling은 `DashboardRefreshOrchestrator`와 `IUiDispatcher` 책임으로 유지한다.

## 8. Runtime Command Boundary
- `ExecuteAsync(RuntimeDashboardCommand, CancellationToken)`은 `IAutomationHubSupervisor` candidate contract에 포함할 수 있다.
- `RuntimeDashboardAdapter` command executor exposure는 아직 구현하지 않는다.
- Add/Edit/Delete는 현재 fake configuration service 책임으로 유지한다.
- Add/Edit/Delete는 Runtime config repository 또는 Supervisor config boundary가 생긴 뒤 Runtime command로 이동해야 한다.
- `TestConnection`, `ResetConnection`, `ManualReconnect`는 Runtime command 후보로 남긴다.

## 9. Runtime Project Strategy
- AH-WPF-23은 Runtime project를 생성하지 않는다.
- 다음 단계는 accepted B option으로 진행하는 것이 적절하다.
- Next step candidates:
  - `src/CAAutomationHub.Runtime` 생성
  - `IAutomationHubSupervisor` 추가
  - `RuntimeSnapshotChangedEventArgs` 추가
- 다음 skeleton 단계에서도 제외:
  - `InMemoryAutomationHubSupervisor`
  - PLC connection
  - Driver 또는 ChannelRunner connection

## 10. Sync/Async Snapshot Bridge Policy
- sync-over-async는 금지한다.
- async Supervisor를 synchronous `IRuntimeSnapshotProvider`에 직접 연결하면 blocking risk가 있다.
- 실제 Runtime 연결 이후 `GetSnapshot()`은 마지막 성공 `DashboardSnapshot` cache를 반환하는 방향이 유력하다.
- async path 또는 Supervisor event가 그 cache를 자연스럽게 갱신할 수 있다.
- 현재 `Task.FromResult(GetSnapshot())` 구조는 실제 async Runtime connection 전까지만 유효하다.
- `IAsyncRuntimeSnapshotProvider` 추가는 후속 후보로 남긴다.

## 11. Event Log Bridge Relationship
- Event bridge 작업은 Snapshot bridge 작업과 분리한다.
- Snapshot refresh는 latest-only coalescing을 사용한다.
- Event log handling에는 별도 rolling buffer, ordering, duplicate removal, coalescing 정책이 필요하다.
- 예상 future flow:
  - Supervisor `RuntimeEventRaised`
  - Adapter 또는 bridge가 `RuntimeDashboardSnapshotMapper.MapEvent` 호출
  - `IRuntimeDashboardEventSource.EventReceived`
  - `RuntimeDashboardEventBridgeService`
  - `RuntimeEventLogItemMapper`
  - `RealtimeEventLogViewModel` 또는 `IEventStreamService`

## 12. Implementation Decision
- AH-WPF-23에서는 A option을 채택했다.
- 이 scenario는 planning과 boundary review만 수행했다.
- Design scenario의 일부로 production code, test code, project skeleton은 변경하지 않았다.

Next step recommendation:
- B option
- Runtime project skeleton 생성
- `IAutomationHubSupervisor` 추가
- `RuntimeSnapshotChangedEventArgs` 추가
- 구현 없음 또는 최소 interface skeleton만 추가
- 실제 PLC는 연결하지 않음

## 13. Excluded Scope
- Actual Runtime implementation
- Supervisor implementation
- Runtime project creation
- `XgtDriverCore` reference
- `XgtChannelRunner` reference
- `FakePlc` reference
- Actual PLC connection
- Runtime command execution implementation
- Add/Edit/Delete Runtime command conversion
- UI changes
- Communication Trend changes
- Mini Trend changes
- `BalanceController` implementation
- `PollingScheduler` implementation

## 14. Risks / Notes
- `RuntimeSnapshot.CapturedAt`과 `RuntimeHealthState.CapturedAt` 중 어떤 값을 ordering source로 볼지 명확히 해야 한다.
- 현재 WPF stale protection은 mapping된 `Health.SnapshotTime`에 의존하며, 이 값은 현재 `RuntimeHealthState.CapturedAt`에서 온다.
- `Revision`이 EventArgs에만 있으면 WPF stale protection은 당분간 time-based로 남는다.
- `IRuntimeSnapshotProvider`가 synchronous이므로 async Supervisor에 직접 연결하면 blocking risk가 있다.
- `EventReceived`는 `SnapshotChanged`와 다른 정책이 필요하므로 같은 orchestrator에 섞으면 안 된다.
- 이 문서는 AH-WPF-23 당시 Closeout 기준 기록이다. 최신 기준과 다음 단계는 `docs/context/WPF_RUNTIME_BRIDGE_CURRENT_STATE.md`를 우선한다.

## 15. Next Scenario Candidates
1. AH-RUNTIME-01: Runtime Project + Supervisor Interface Skeleton
   - `CAAutomationHub.Runtime` 생성
   - Contracts 참조
   - `IAutomationHubSupervisor` 추가
   - `RuntimeSnapshotChangedEventArgs` 추가
   - 실제 PLC 연결 없음

2. AH-RUNTIME-02: Supervisor Runtime Snapshot Provider Bridge
   - `IAsyncRuntimeSnapshotProvider` 검토
   - Supervisor snapshot을 `RuntimeDashboardAdapter`로 전달하는 bridge 설계
   - sync `GetSnapshot()` cache 정책 검토

3. AH-WPF-24: Runtime Event Bridge Skeleton
   - `RuntimeEventRaised`를 `RuntimeDashboardEvent`와 `RuntimeEventLogItem`으로 연결
   - rolling buffer와 UI marshal 정책 정의

4. AH-RUNTIME-03: InMemory Supervisor Skeleton
   - `RuntimeSnapshot` 생성
   - `SnapshotChanged` 발생
   - 실제 PLC 연결 없음

Note:
- 위 Next Scenario Candidates는 AH-WPF-23 당시 후보 기록이다. 현재 기준에서 AH-WPF-24는 Current State Audit이며, Runtime Event Bridge는 AH-WPF-25 또는 AH-RUNTIME-03 후보로 본다.
