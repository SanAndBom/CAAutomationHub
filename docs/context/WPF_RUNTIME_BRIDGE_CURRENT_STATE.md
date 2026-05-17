# WPF Runtime Bridge 현재 상태

## 0. Historical Anchor Notice
- 이 문서는 AH-WPF-24 시점의 WPF -> Runtime 진입 전 anchor다.
- 현재 Runtime 진행 상태는 이 문서가 아니라 docs/harness/AH-RUNTIME-xx.md Closeout과 최신 Handoff를 우선한다.
- 이 문서 안의 "CAAutomationHub.Runtime 프로젝트 없음", "다음 권장 단계 AH-RUNTIME-01" 같은 내용은 historical context로만 해석한다.
- 최신 Runtime anchor는 AH-RUNTIME-50 / commit `6c027d8`이다.
- 다음 Runtime 후보는 AH-RUNTIME-51 Template / Binding Validation Rule Review다.
- 새 채팅방 검증 시 이 문서를 최신 Runtime 상태의 source of truth로 사용하지 않는다.

## 1. 목적
- 이 문서는 CAAutomationHub WPF Dashboard가 Fake 기반 Prototype에서 Real Runtime Bridge로 넘어가기 직전의 현재 상태를 요약한다.
- 채팅방 전환이나 다음 Runtime 작업 진입 시 현재 설계 기준을 복원하기 위한 handoff anchor 문서다.

## 2. 현재 마일스톤
- AH-WPF-09: Communication Trend Prototype 완료
- AH-WPF-10: PLC Card Edit/Delete 완료
- AH-WPF-11: PLC Add Actual Apply 완료
- AH-WPF-12: PlcEditorDialog Validation 완료
- AH-WPF-13: PLC Card Runtime Signal / Mini Trend 완료
- AH-WPF-14: Dashboard Source Refactor 완료
- AH-WPF-15: Runtime Bridge Contract Review 완료
- AH-WPF-16: Contracts Runtime Skeleton 완료
- AH-WPF-17: RuntimeSnapshot to DashboardSnapshot Mapper 완료
- AH-WPF-18: RuntimeDashboardAdapter Provider Skeleton 완료
- AH-WPF-19: Async/Event Contract Review 완료
- AH-WPF-20: Optional Runtime Adapter Interfaces 완료
- AH-WPF-21: Dashboard Refresh Orchestrator 완료
- AH-WPF-22: DashboardViewModel Event Refresh Wiring 완료
- AH-WPF-23: Supervisor Boundary Review 완료
- AH-WPF-24: WPF Runtime Bridge Current State Audit 완료 및 커밋 완료

## 3. 현재 아키텍처 요약

AH-WPF-24 Audit 결과:
- Runtime 진입을 막는 코드 구조 불일치는 발견되지 않았다.
- AH-RUNTIME-01 전에 필수 코드 정돈은 필요하지 않다.
- 프로젝트 구조는 정상이다. Contracts, WPF, WPF.Tests가 존재하며 의도한 방향으로 참조된다.
- 전체 테스트 기준선은 177 passed다.
- 이 context anchor 보정 후 다음 구현 단계는 AH-RUNTIME-01을 권장한다.

현재 WPF dashboard snapshot 흐름:

```text
WPF UI
-> DashboardViewModel
-> IRuntimeDashboardAdapter
-> RuntimeDashboardAdapter
-> IRuntimeSnapshotProvider
-> RuntimeDashboardSnapshotMapper
-> DashboardSnapshot
```

Real Runtime 방향:

```text
RuntimeDashboardAdapter
-> Provider / Bridge
-> IAutomationHubSupervisor
-> RuntimeSnapshot
-> ChannelRuntimeState
-> Channel / Session / Transport / PLC
```

## 4. 현재 WPF Dashboard 기능
- PLC Card 표시
- Add/Edit/Delete Fake configuration 반영
- PlcEditorDialog validation
- Detail Pane
- Communication Trend
  - Overview: PLC별 RTT overlap
  - Selected: 선택 PLC 단일 RTT trend
- Card Runtime Signal
  - Current Sequence
  - 최근 5분 sequence response latency Mini Trend
- Realtime Event Log Prototype
- Layout settings
  - Communication Trend height splitter
  - LocalAppData save/restore
- Push refresh 준비
  - `IRuntimeDashboardEventSource`
  - `DashboardRefreshOrchestrator`
  - `DashboardViewModel` `SnapshotChanged` wiring

## 5. Runtime Bridge 계약

Contracts 프로젝트:
- `CAAutomationHub.Contracts`

Runtime DTO:
- `RuntimeSnapshot`
- `ChannelRuntimeState`
- `RuntimeHealthState`
- `RuntimeEvent`
- `RuntimeDashboardCommand`
- `RuntimeDashboardCommandResult`

Runtime 상태:
- `PlcLinkState`
- `PlcHealthSeverity`
- `PlcPollingState`
- `RuntimeSequenceState`

Mapping:
- `RuntimeDashboardSnapshotMapper`
- `RuntimeSnapshot` -> `DashboardSnapshot`
- `RuntimeHealthState` -> `RuntimeHealthSnapshot`
- `ChannelRuntimeState` -> `PlcCardSnapshot`
- `PlcHealthSeverity` -> `PlcConnectionState`
- `RuntimeEvent` -> `RuntimeDashboardEvent`

## 6. 확정된 경계 규칙
- 현재 솔루션에는 `CAAutomationHub.Contracts`, `CAAutomationHub.Wpf`, `CAAutomationHub.Wpf.Tests`가 있다.
- `CAAutomationHub.Wpf`는 `CAAutomationHub.Contracts`를 참조한다.
- `CAAutomationHub.Contracts`는 WPF, Runtime, XGT, FakePlc 구현 프로젝트를 참조하지 않는다.
- `CAAutomationHub.Runtime`은 아직 없다.
- AH-RUNTIME-01에서는 Runtime 프로젝트를 만들 때 `CAAutomationHub.Contracts`만 참조하게 하고 `CAAutomationHub.Wpf`는 참조하지 않도록 해야 한다.
- WPF UI는 `PlcChannel`을 모른다.
- WPF UI는 `XgtSession`을 모른다.
- WPF UI는 `TcpTransport`를 모른다.
- WPF UI는 XGT Protocol DTO를 모른다.
- WPF UI는 `DashboardSnapshot`, `PlcCardSnapshot`, `RuntimeDashboardEvent` 같은 UI DTO만 소비한다.
- `RuntimeSnapshot`과 `DashboardSnapshot`은 분리되어 있다.
- `RuntimeDashboardAdapter` 또는 Mapper가 `RuntimeSnapshot`을 `DashboardSnapshot`으로 변환한다.
- `RuntimeDashboardAdapter`는 Supervisor 또는 Provider boundary만 본다.
- Supervisor는 Runtime Control Plane이다.
- Channel은 PLC 1개의 connection, request, recovery를 소유한다.
- Session은 XGT request/response serialization을 소유한다.
- DriverCore는 XGT protocol, frame, Transport를 소유한다.

## 7. 현재 Refresh 전략

Pull:
- `DispatcherTimer` 1초
- `adapter.GetSnapshot()`
- `ApplySnapshot()`

Push:
- `IRuntimeDashboardEventSource.SnapshotChanged`
- `DashboardRefreshOrchestrator`
- latest-only coalescing
- UI dispatcher marshal
- `ApplySnapshot()`

중요:
- Timer refresh는 유지한다.
- `SnapshotChanged`는 빠른 apply 지원 경로다.
- `EventReceived`는 아직 연결하지 않았다.
- `EventReceived`는 별도 Runtime Event Bridge 후보로 남긴다.

## 8. Supervisor Boundary 결정
- Supervisor와 WPF Adapter 사이에는 별도 provider/bridge adapter를 둔다.
- `IAutomationHubSupervisor`는 WPF의 `IRuntimeSnapshotProvider`를 직접 구현하지 않는다.
- `RuntimeDashboardAdapter`는 `IAutomationHubSupervisor`를 깊게 알지 않는다.
- sync-over-async는 금지한다.
- 실제 Runtime 연결 이후 `GetSnapshot()`은 마지막 성공 `DashboardSnapshot` cache를 반환하는 방향이 유력하다.
- `RuntimeSnapshotChangedEventArgs`가 필요하다.
- `Revision`은 장기적으로 유용하지만 현재 `RuntimeSnapshot` 또는 `DashboardSnapshot`에 직접 추가하지 않는다.
- AH-RUNTIME-01은 같은 snapshot에 대해 `RuntimeSnapshot.CapturedAt`과 `RuntimeHealthState.CapturedAt`이 일치해야 한다는 invariant를 명시해야 한다.

## 9. Real Runtime 전 알려진 Gap
- `CAAutomationHub.Runtime` 프로젝트 없음
- `IAutomationHubSupervisor` 없음
- `RuntimeSnapshotChangedEventArgs` 없음
- Supervisor 구현 없음
- RuntimeSnapshot provider bridge 없음
- Runtime Event Bridge 없음
- Runtime telemetry contract 없음
- `XgtDriverCore` / `XgtChannelRunner` 연결 없음
- 실제 PLC 연결 없음
- `PollingScheduler` 없음
- `BalanceController` 없음
- Runtime command execution 없음

## 10. 대체된 오래된 전제
다음 오래된 전제는 현재 거짓이며 Runtime 진입 기준으로 사용하면 안 된다.
- `DashboardViewModel`이 `PlcCards`를 clear하고 모든 card view model을 재생성한다.
- `RuntimeHealthSnapshot`에 `InactiveCount`가 없다.
- Card Mini Trend는 작은 RTT chart일 뿐이다.
- `RuntimeDashboardAdapter`가 직접 빈 `DashboardSnapshot`을 반환한다.
- Contracts 프로젝트가 없다.
- `RuntimeSnapshot` -> `DashboardSnapshot` mapper가 없다.
- `SnapshotChanged` push refresh가 `DashboardViewModel`에 연결되어 있지 않다.
- `FakeDashboardRuntimeAdapter`가 trend와 runtime signal 생성을 모두 직접 담당한다.

현재 기준:
- `DashboardViewModel`은 PlcId 기준 remove/merge/update로 card 상태를 갱신한다.
- WPF health snapshot과 Runtime health contract에 `InactiveCount`가 있다.
- Card Mini Trend는 최근 5분 sequence response latency를 표현한다.
- `RuntimeDashboardAdapter`는 `IRuntimeSnapshotProvider`와 `RuntimeDashboardSnapshotMapper`를 사용한다.
- `CAAutomationHub.Contracts`가 있다.
- `RuntimeDashboardSnapshotMapper`가 있다.
- `DashboardViewModel`은 optional `IRuntimeDashboardEventSource`를 감지하고 `SnapshotChanged`를 `DashboardRefreshOrchestrator`로 연결한다.
- Fake trend 생성은 `FakeCommunicationTrendFactory`로 분리되어 있다.
- Fake runtime signal 생성은 `FakeRuntimeSignalFactory`로 분리되어 있다.

## 11. 다음 권장 단계
1. AH-RUNTIME-01: Runtime Project + Supervisor Interface Skeleton
   - `CAAutomationHub.Runtime` 생성
   - `CAAutomationHub.Contracts` 참조
   - `IAutomationHubSupervisor` 추가
   - `RuntimeSnapshotChangedEventArgs` 추가
   - `RuntimeSnapshot.CapturedAt` / `RuntimeHealthState.CapturedAt` 일치 정책 정의
   - Runtime 프로젝트가 `CAAutomationHub.Wpf`를 참조하지 않는지 검증
   - 실제 PLC, XGT, FakePlc 연결 없음

2. AH-RUNTIME-02: Supervisor Runtime Snapshot Provider Bridge
   - `IAsyncRuntimeSnapshotProvider` 검토
   - sync `GetSnapshot()` cache 정책 정의
   - sync-over-async 금지 유지
   - Supervisor snapshot을 `RuntimeDashboardAdapter`로 전달하는 bridge 설계

3. AH-WPF-25 또는 AH-RUNTIME-03: Runtime Event Bridge Skeleton
   - `RuntimeEventRaised`를 `RuntimeDashboardEvent`와 `RuntimeEventLogItem`으로 연결
   - rolling buffer와 UI marshal 정책 정의

4. AH-RUNTIME-04: InMemory Supervisor Skeleton
   - `RuntimeSnapshot` 생성
   - `SnapshotChanged` 발생
   - 실제 PLC 연결 없음

5. AH-RUNTIME-05 이후:
   - `ChannelRegistry`
   - `XgtSession` / `XgtDriverCore` 연결
   - `TestConnection` / `HealthProbe`
   - `PollingScheduler`
   - `TelemetryBuffer`
   - Recovery policy
   - Balance policy

## 12. 현재 Git Anchor
- AH-WPF-22 commit: `fb7bf1b`
- AH-WPF-23 commit: `6f855f7`
- AH-WPF-24 commit: `6eb1fa08c60e8b627ae6517c886f57cdbb6bbc23`
- Working tree: clean
- Historical note: 위 anchor는 WPF -> Runtime 진입 전 상태를 설명한다.
- 최신 전체 anchor: DOCS-REVIEW-01 / commit `fe33af8`
- 최신 Runtime anchor: AH-RUNTIME-50 / commit `6c027d8`
- 다음 Runtime 후보: AH-RUNTIME-51 Template / Binding Validation Rule Review

## 13. Notes
- 이 문서는 AH-WPF-24 Audit 이후 WPF -> Runtime 진입 전 handoff anchor다.
- 상세 WPF 구현 이력은 `docs/harness/AH-WPF-xx.md`를 참고한다.
- Runtime 진행 상태의 source of truth는 `docs/harness/AH-RUNTIME-xx.md` Closeout과 최신 Handoff다.
- 이 문서와 최신 AH-RUNTIME Closeout이 충돌하면 최신 AH-RUNTIME Closeout을 우선한다.
- 새 채팅방 Cognitive Sync 검증 시 이 문서를 최신 Runtime 상태의 source of truth로 사용하지 않는다.
- "CAAutomationHub.Runtime 프로젝트 없음", "다음 권장 단계 AH-RUNTIME-01" 같은 본문 내용은 AH-WPF-24 시점의 historical context로만 해석한다.
- 최신 Runtime anchor는 AH-RUNTIME-50 / commit `6c027d8`이며, 다음 Runtime 후보는 AH-RUNTIME-51 Template / Binding Validation Rule Review다.
