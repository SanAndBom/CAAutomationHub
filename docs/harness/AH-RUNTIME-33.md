# AH-RUNTIME-33 Closeout

## 1. Summary

AH-RUNTIME-33은 AH-RUNTIME-32 Boundary Review에서 채택한 별도 orchestration service 권장안을 최소 skeleton으로 구현한 단계다.

이번 단계에서 `PollingResultStateOrchestrator`와 `PollingResultStateOrchestrationResult`를 추가해 다음 Runtime 내부 흐름을 연결했다.

    ChannelPollingResult
            ↓
    PollingResultStateOrchestrator
            ↓
    previous RuntimePlcChannelState readback
            ↓
    RuntimePlcChannelStateMapper.Map(previous, result)
            ↓
    PollingChannelUpdate
            ↓
    PollingPublishCoordinator

이번 작업은 실제 polling loop 구현이 아니다. 핵심은 `ChannelPollingResult`를 Runtime publish 가능한 `PollingChannelUpdate`로 조립하고, 기존 `PollingPublishCoordinator`에 publish를 위임하는 orchestration boundary를 만드는 것이다.

## 2. Goal

목표는 AH-RUNTIME-31에서 추가한 mapper boundary와 기존 publish boundary 사이에 최소 orchestration boundary를 추가하는 것이다.

포함된 목표:

- `ChannelPollingResult`를 직접 `PollingPublishCoordinator`에 넣지 않음
- `PollingPublishCoordinator`에 mapper 책임을 추가하지 않음
- `RuntimeChannelRegistry`는 lookup-only로만 사용
- previous `RuntimePlcChannelState`는 `IWritableRuntimePlcChannel.GetRuntimeState()`로 readback
- `RuntimePlcChannelStateMapper.Map(previous, result)` 호출
- `PollingChannelUpdate` 생성
- `PollingPublishCoordinator.PublishAsync(...)`에 위임
- `RefreshSnapshotAsync` 직접 호출 금지
- `SnapshotChanged` 직접 발생 금지

## 3. Background

AH-RUNTIME-31에서는 Runtime 내부 polling result를 `RuntimePlcChannelState`로 바꾸는 mapper boundary를 추가했다.

    ChannelPollingResult
            ↓
    RuntimePlcChannelStateMapper
            ↓
    RuntimePlcChannelState

기존 publish 흐름은 이미 다음 형태로 존재했다.

    PollingChannelUpdate
            ↓
    PollingPublishCoordinator
            ↓
    IWritableRuntimePlcChannel.ReplaceState
            ↓
    refreshSnapshotAsync
            ↓
    RuntimeSnapshot / SnapshotChanged

AH-RUNTIME-32 Boundary Review에서는 `RuntimePlcChannelStateMapper` 결과를 `PollingChannelUpdate`로 감싸고 `PollingPublishCoordinator`에 넘기는 책임을 어디에 둘지 검토했다. 결론은 `PollingPublishCoordinator` 확장보다 별도 orchestration service가 안전하다는 것이었다.

AH-RUNTIME-33은 이 결론에 따라 `PollingResultStateOrchestrator` skeleton을 추가했다.

## 4. 구현 결과

### 4.1 추가 파일

- `src/CAAutomationHub.Runtime/Polling/PollingResultStateOrchestrator.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingResultStateOrchestratorTests.cs`

### 4.2 추가 타입

`PollingResultStateOrchestrator`

- Runtime 내부 polling result orchestration service다.
- public API:
  - `PublishAsync(ChannelPollingResult result, CancellationToken cancellationToken = default)`
- `ChannelPollingResult`를 받아 previous state readback, mapper 호출, `PollingChannelUpdate` 생성, `PollingPublishCoordinator` 위임을 수행한다.

`PollingResultStateOrchestrationResult`

- orchestration 결과를 표현하는 Runtime 내부 result 타입이다.
- 필드:
  - `PlcId`
  - `Succeeded`
  - `ErrorMessage`
  - `PublishResult`
- 성공 시 `PublishResult`를 포함한다.
- channel missing 또는 non-writable 등 coordinator까지 도달하지 못한 실패는 `PublishResult = null`로 표현한다.

### 4.3 Runtime 내부 흐름

AH-RUNTIME-33에서 구현된 흐름:

    ChannelPollingResult
            ↓
    RuntimeChannelRegistry.TryGetChannel(result.PlcId, out channel)
            ↓
    IWritableRuntimePlcChannel.GetRuntimeState()
            ↓
    RuntimePlcChannelStateMapper.Map(previous, result)
            ↓
    new PollingChannelUpdate(result.PlcId, nextState)
            ↓
    PollingPublishCoordinator.PublishAsync([update], cancellationToken)
            ↓
    PollingResultStateOrchestrationResult

이번 단계에서는 single `ChannelPollingResult` 처리만 구현했다. batch `ChannelPollingResult` 처리는 후속 Boundary Review 후보로 남겼다.

## 5. PollingResultStateOrchestrator 책임

`PollingResultStateOrchestrator`는 다음 순서로 동작한다.

1. `ChannelPollingResult`를 받는다.
2. `RuntimeChannelRegistry.TryGetChannel(result.PlcId, out channel)`로 channel을 조회한다.
3. channel이 없으면 실패 결과를 반환한다.
4. channel이 `IWritableRuntimePlcChannel`이 아니면 실패 결과를 반환한다.
5. `writable.GetRuntimeState()`로 previous `RuntimePlcChannelState`를 읽는다.
6. `RuntimePlcChannelStateMapper.Map(previous, result)`를 호출한다.
7. `PollingChannelUpdate`를 생성한다.
8. `PollingPublishCoordinator.PublishAsync([update], cancellationToken)`에 위임한다.
9. `PollingPublishResult`를 포함한 `PollingResultStateOrchestrationResult`를 반환한다.

중요한 의미:

- Orchestrator는 Runtime publish 흐름 앞단의 조립자다.
- Orchestrator는 상태를 직접 교체하지 않는다.
- Orchestrator는 snapshot refresh를 직접 수행하지 않는다.
- Orchestrator는 `SnapshotChanged`를 직접 발생시키지 않는다.
- Orchestrator는 XGT, `FakePlc`, Scheduler, WPF를 알지 못한다.

## 6. 유지한 Boundary

AH-RUNTIME-33에서 `PollingResultStateOrchestrator`는 다음을 직접 호출하지 않는다.

- `IWritableRuntimePlcChannel.ReplaceState`
- `InMemoryAutomationHubSupervisor.RefreshSnapshotAsync`
- `SnapshotChanged`
- WPF mapper
- `XgtDriverCore`
- `FakePlc`
- `XgtChannelRunner`
- `PollingScheduler`
- reconnect API
- `ContextPublisher`

AH-RUNTIME-33에서 유지한 핵심 boundary:

- `RuntimeChannelRegistry`는 lookup-only로만 사용
- `PollingPublishCoordinator`는 `PollingChannelUpdate` publish 조율자로 유지
- `PollingPublishCoordinator`에 `ChannelPollingResult` 해석 책임을 추가하지 않음
- `RuntimePlcChannelStateMapper`는 previous state와 result를 받아 next state만 계산
- `RefreshSnapshotAsync`는 `PollingPublishCoordinator` 내부 위임 흐름에서만 수행
- Runtime project는 `CAAutomationHub.Contracts`만 참조
- `ContextPublisher` 자동 publish는 재도입하지 않음

## 7. 제외한 범위

이번 작업에서 수정하지 않은 영역:

- `IAutomationHubSupervisor`
- `IRuntimePlcChannel`
- `IWritableRuntimePlcChannel`
- `RuntimeChannelRegistry`
- `PollingPublishCoordinator`
- Contracts DTO
- WPF
- `XgtDriverCore`
- `FakePlc`
- `XgtChannelRunner`
- `PollingScheduler` timer / loop
- reconnect 정책
- `ContextPublisher` 자동 publish

이번 작업에서 구현하지 않은 항목:

- 실제 polling loop
- driver adapter
- XGT integration
- `FakePlc` integration
- reconnect decision
- WPF bridge 연결
- batch `ChannelPollingResult` 처리
- readback → map → publish 구간의 atomic update 보장
- `ContextPublisher` 재도입

## 8. OccurredAt / CapturedAt 분리

AH-RUNTIME-33에서도 polling event time과 snapshot capture time을 분리했다.

- `PollingResultStateOrchestrator` API는 `CapturedAt`을 받지 않는다.
- `ChannelPollingResult.OccurredAt`은 polling event 발생 시각이다.
- `RuntimeSnapshot.CapturedAt`은 snapshot frame 수집 시각이다.
- `PollingResultStateOrchestrator`는 snapshot frame time을 만들지 않는다.
- `LastSuccessAt` / `LastFailureAt` 갱신은 `RuntimePlcChannelStateMapper`를 통해 `ChannelPollingResult.OccurredAt` 기준으로만 수행된다.
- 따라서 polling event time과 snapshot capture time이 섞이지 않는다.

`PollingChannelUpdate`는 상태 변경 package이며 snapshot frame이 아니다. Snapshot capture는 `PollingPublishCoordinator`가 `refreshSnapshotAsync`를 호출하는 publish 흐름에서 발생한다.

## 9. 동시성 리스크

현재 흐름은 다음 순서로 수행된다.

    GetRuntimeState()
            ↓
    RuntimePlcChannelStateMapper.Map(...)
            ↓
    PollingPublishCoordinator.PublishAsync(...)
            ↓
    ReplaceState(...)

`GetRuntimeState`와 `ReplaceState`는 각각 channel 내부 gate로 보호될 수 있지만, readback → map → publish 전체 과정은 아직 원자적이지 않다.

따라서 readback과 replace 사이에 다른 update가 들어오면 lost update 가능성이 있다.

AH-RUNTIME-33에서는 이 문제를 해결하지 않았다.

이번 skeleton의 처리 기준:

- 단일 `ChannelPollingResult` 처리 범위로 제한
- registry lock / channel lock 구조 확장하지 않음
- `IWritableRuntimePlcChannel`에 atomic update API 추가하지 않음
- 동시성 문제는 다음 단계 후보 또는 별도 Boundary Review 대상으로 남김

## 10. 테스트 및 검증 결과

TDD 흐름:

- RED 확인: 새 테스트가 `PollingResultStateOrchestrator` / result 타입 미존재로 실패 확인

실행한 검증:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter PollingResultStateOrchestratorTests`
  - 통과: 8
  - 실패: 0
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
  - 통과: 107
  - 실패: 0
- `dotnet build CAAutomationHub.sln`
  - 성공
  - 경고 0
  - 오류 0
- `git diff --check`
  - exit 0
  - 출력 없음

검증 의미:

- missing channel은 coordinator 호출 없이 orchestration failure로 반환됨
- non-writable channel은 coordinator 호출 없이 orchestration failure로 반환됨
- writable channel은 previous state readback 후 mapper 결과를 publish coordinator에 위임함
- `OccurredAt`이 `LastSuccessAt` 또는 `LastFailureAt`에 반영됨
- orchestrator API가 `CapturedAt`을 받지 않음을 확인함
- coordinator의 `PollingPublishResult`가 orchestration result에 포함됨

## 11. 변경 파일 목록

AH-RUNTIME-33 skeleton 구현 변경 파일:

- `src/CAAutomationHub.Runtime/Polling/PollingResultStateOrchestrator.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingResultStateOrchestratorTests.cs`

AH-RUNTIME-33 closeout 문서:

- `docs/harness/AH-RUNTIME-33.md`

## 12. 다음 단계 후보

다음 후보는 AH-RUNTIME-34 또는 별도 Boundary Review로 분리하는 것이 안전하다.

후보 1:

- AH-RUNTIME-33 commit 전 최종 검증
- 변경 파일 3개 또는 실제 생성 파일 확인
- tests/build/git diff --check 재확인
- working tree 상태 확인

후보 2:

- AH-RUNTIME-34: Fake polling event 기반 end-to-end Runtime publish test
- `ChannelPollingResult` → `PollingResultStateOrchestrator` → `PollingPublishCoordinator` → `RuntimeSnapshot`까지 테스트
- 실제 Scheduler / XGT / `FakePlc` 연결 없이 in-memory 경로만 검증

후보 3:

- batch `ChannelPollingResult` 처리 Boundary Review
- 단일 result 처리에서 batch result 처리로 확장할지 검토
- `PollingPublishCoordinator`의 batch update 흐름과 일관성 확인

후보 4:

- 동시성 / lost update 리스크 Boundary Review
- readback → map → publish 구간의 원자성 문제 검토
- atomic update API 필요 여부는 별도 단계에서 판단

후보 5:

- `PollingScheduler` Boundary Review
- timer loop / interval / cancellation / batch cycle 검토
- 단, 실제 scheduler 구현은 이후 단계로 분리

후보 6:

- `XgtDriverCore` / `FakePlc` adapter Boundary Review
- 실제 driver event를 `ChannelPollingResult`로 변환하는 위치 검토
- 단, AH-RUNTIME-33 Closeout 단계에서는 연결하지 않음

## 13. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-33 목표였던 Runtime 내부 orchestration skeleton 결과가 closeout 문서로 기록됨
- `PollingResultStateOrchestrator`의 책임과 제외 범위를 명확히 기록함
- `RuntimeChannelRegistry`, `PollingPublishCoordinator`, mapper, snapshot refresh boundary를 분리해 기록함
- `OccurredAt` / `CapturedAt` 분리 의미를 기록함
- readback → map → publish 구간의 lost update 가능성을 리스크로 기록함
- 테스트, 빌드, `git diff --check` 검증 결과를 기록함
- `ContextPublisher` 자동 publish 미사용 정책을 유지함

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
