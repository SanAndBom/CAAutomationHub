# AH-WPF-19 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- RuntimeDashboardAdapter의 async/event/lifecycle/command 계약 확장 방향 검토
- 실제 Supervisor 연결 전 adapter interface 확장 범위 판단
- 기존 WPF refresh 안정성을 깨지 않는 전환 전략 수립

## 3. Current Structure Reviewed
- `IRuntimeDashboardAdapter`
  - `DashboardSnapshot GetSnapshot()`
- `RuntimeDashboardAdapter`
  - `IRuntimeSnapshotProvider.GetSnapshot()`
  - `RuntimeDashboardSnapshotMapper.Map(...)`
- `IRuntimeSnapshotProvider`
  - `RuntimeSnapshot GetSnapshot()`
- `DashboardViewModel`
  - `DispatcherTimer` 1초 refresh
  - `_adapter.GetSnapshot()` 호출
- `FakeDashboardRuntimeAdapter`
  - `IRuntimeDashboardAdapter`와 `IPlcDashboardConfigurationService` 구현
- `RuntimeDashboardSnapshotMapper`
  - `RuntimeSnapshot` -> `DashboardSnapshot`
- Contracts
  - `RuntimeDashboardCommand` / `RuntimeDashboardCommandResult` 존재

## 4. Async Snapshot Decision
- 실제 Supervisor/Runtime 연동에서는 `GetSnapshotAsync(CancellationToken)`가 자연스럽다.
- 하지만 현재 `DashboardViewModel`과 fake adapter는 동기 `GetSnapshot()` 구조에 안정적으로 맞춰져 있다.
- sync `GetSnapshot()`이 내부에서 async를 blocking 호출하는 구조는 피한다.
- 이번 단계에서는 기존 `GetSnapshot()`을 유지한다.
- 후속에서 `IAsyncRuntimeDashboardAdapter` 또는 `IAsyncRuntimeSnapshotProvider` 도입을 검토한다.

## 5. Lifecycle Decision
- `StartAsync(CancellationToken)`, `StopAsync(CancellationToken)`, `DisposeAsync()` 후보는 인정한다.
- 하지만 Adapter가 Supervisor lifecycle host가 되는 것은 신중해야 한다.
- `App`/`MainWindow` composition root 또는 별도 runtime host가 Supervisor lifecycle을 관리하는 방향이 자연스럽다.
- `DashboardViewModel` `Loaded`/`Unloaded`와 runtime lifecycle을 바로 묶지 않는다.
- `FakeDashboardRuntimeAdapter`에는 lifecycle이 필요하지 않다.
- 후속 skeleton은 optional interface 분리가 적절하다.

## 6. SnapshotChanged Event Decision
- `SnapshotChanged` event는 장기적으로 필요할 수 있다.
- 하지만 현재 UI는 `DispatcherTimer` 기반이다.
- event 기반으로 바로 전환하면 thread marshal, reentrancy, 중복 refresh 제어가 필요하다.
- Adapter event는 UI thread에서 발생한다고 보장하지 않는다.
- 1차는 `DispatcherTimer` polling을 유지한다.
- 후속 event 도입 시 latest-only coalescing 또는 refresh requested flag를 검토한다.

## 7. EventReceived Decision
- `RuntimeEvent` -> `RuntimeDashboardEvent` -> `RuntimeEventLogItem` 흐름을 유지한다.
- `IRuntimeDashboardAdapter.EventReceived` 직접 노출은 후속 검토한다.
- UI log service가 adapter event를 구독하는 bridge service를 둘 수 있다.
- Runtime raw event를 UI에 과도하게 push하지 않는다.
- Runtime canonical buffer와 UI visible buffer를 분리하는 방향이 좋다.

## 8. ExecuteAsync Command Decision
- `RuntimeDashboardCommand` / `RuntimeDashboardCommandResult`는 Contracts에 준비되어 있다.
- `ExecuteAsync(RuntimeDashboardCommand, CancellationToken)`는 실제 Supervisor `CommandDispatcher`가 생길 때 도입한다.
- Add/Edit/Delete는 현재 fake configuration service 흐름을 유지한다.
- `TestConnection` / `ResetConnection` / `ManualReconnect`는 후속 command 후보이다.
- command 실패는 가능하면 `RuntimeDashboardCommandResult`로 표현하고, fatal runtime error만 exception 후보로 둔다.

## 9. Interface Extension Strategy
- `IRuntimeDashboardAdapter` 직접 확장은 지금 피한다.
- 기존 interface는 최소 동기 snapshot 계약으로 유지한다.
- 새 기능은 optional extension interface로 분리한다.

후속 후보:
- `IAsyncRuntimeDashboardAdapter`
- `IRuntimeDashboardLifecycle`
- `IRuntimeDashboardEventSource`

주의:
- default interface method는 구현 누락을 숨길 수 있으므로 권장하지 않는다.

## 10. Error / Fallback Policy
- 현재 `RuntimeDashboardAdapter`는 provider exception을 전파한다.
- 실제 Runtime 연결 시 fallback 정책이 필요할 수 있다.
- 후보:
  - 마지막 성공 `DashboardSnapshot` 유지
  - Empty snapshot 반환
  - Runtime error event 발생
  - Header/Health 상태를 Error로 표시
- 이번 단계에서는 정책 후보만 문서화하고 구현은 보류한다.

## 11. Implementation Decision
- A안 채택
- 이번에는 계획만 하고 구현은 다음 단계로 분리한다.

후속 구현 출발점:
- B안
- 기존 `IRuntimeDashboardAdapter` 유지
- optional async/lifecycle/event interface skeleton 추가

## 12. Excluded Scope
- 실제 Runtime 연결
- Supervisor 구현
- `IAutomationHubSupervisor` 구현
- Runtime 프로젝트 생성
- `XgtDriverCore` 참조
- `XgtChannelRunner` 참조
- `FakePlc` 참조
- 실제 PLC 연결
- `DashboardViewModel` event refresh 실제 전환
- Add/Edit/Delete runtime command 전환
- UI 변경
- Communication Trend 변경
- Mini Trend 변경
- Runtime telemetry 구현
- BalanceController 구현

## 13. Risks / Notes
- async provider 도입 시점을 아직 정해야 한다.
- Adapter async만 먼저 둘지, provider async까지 같이 둘지 후속 결정이 필요하다.
- Adapter가 runtime host인지 facade인지 lifecycle 책임을 명확히 해야 한다.
- `SnapshotChanged` event는 background thread에서 올 수 있으므로 UI marshal 정책이 필요하다.
- provider 실패 시 예외 전파 유지/마지막 snapshot 유지 정책을 후속 결정해야 한다.
- command 실패 표시 위치는 아직 미정이다.

## 14. Next Scenario Candidates
1. AH-WPF-20: Optional Runtime Adapter Interfaces Skeleton
   - `IAsyncRuntimeDashboardAdapter`
   - `IRuntimeDashboardLifecycle`
   - `IRuntimeDashboardEventSource`
   - 기존 `IRuntimeDashboardAdapter` 유지
   - `DashboardViewModel` 변경 없음

2. AH-WPF-21: Runtime Provider / Supervisor Boundary Review
   - `IRuntimeSnapshotProvider`를 Supervisor와 어떻게 연결할지 검토
   - `IAutomationHubSupervisor` 도입 여부 검토

3. AH-RUNTIME-01: Supervisor Skeleton
   - `IAutomationHubSupervisor`
   - InMemory/Fake supervisor
   - 실제 PLC 연결 없음

4. AH-RUNTIME-02: Runtime Event Bridge Skeleton
   - `RuntimeEvent` -> `RuntimeDashboardEvent` -> `RuntimeEventLogItem` 흐름 연결
   - Event coalescing / rolling buffer 검토
