# AH-RUNTIME-36 Closeout

## 1. Summary

AH-RUNTIME-36은 Runtime polling publish path의 동시성 / lost update 리스크를 Boundary Review로 정리한 단계다.

검토한 핵심 흐름은 다음과 같다.

    GetRuntimeState()
            ↓
    RuntimePlcChannelStateMapper.Map(...)
            ↓
    PollingPublishCoordinator.PublishAsync(...)
            ↓
    IWritableRuntimePlcChannel.ReplaceState(...)

검토 결과, 현재 `InMemoryRuntimePlcChannel`의 `GetRuntimeState`, `ReplaceState`, `GetState`는 각각 lock 보호를 받으며 method 단위로 thread-safe하다. 하지만 `GetRuntimeState → Map → PublishAsync → ReplaceState` 전체 구간은 하나의 원자적 update로 보호되지 않는다.

따라서 같은 PLC에 대해 동시에 publish가 들어오면 stale previous state 기반 update가 발생할 수 있고, 특히 `ConsecutiveFailures` 같은 누적 필드에서 lost update가 가능하다.

이번 단계에서는 production code, test code, skeleton, interface를 수정하지 않았다. AH-RUNTIME-37 `PollingScheduler` Boundary Review에서 Runtime publish path의 single-writer invariant를 핵심 계약으로 올리는 것을 1순위 권장안으로 정리한다.

`ContextPublisher` 자동 publish는 재도입하지 않았다. Runtime 작업 기록은 `docs/harness/AH-RUNTIME-xx.md` Closeout 문서를 primary historical record로 사용한다.

## 2. Goal

AH-RUNTIME-36의 목표는 AH-RUNTIME-35까지 검증된 Runtime 내부 polling publish path가 동시에 호출될 때 발생할 수 있는 동시성 / lost update 리스크를 실제 타입과 현재 lock / gate 구조 기준으로 검토하는 것이다.

이번 작업은 구현 단계가 아니다.

목표는 다음 질문에 답하는 것이었다.

- 현재 `GetRuntimeState → Map → PublishAsync → ReplaceState` 구간이 원자적인가?
- 아니라면 어떤 상황에서 lost update가 발생할 수 있는가?
- Scheduler가 들어오기 전에 반드시 구현으로 해결해야 하는가?
- 해결한다면 어느 boundary가 적절한가?
- 현재 단계에서는 리스크 문서화 후 `PollingScheduler` Boundary Review로 넘어가는 것이 안전한가?

Boundary Review 결론은 다음과 같다.

- 각 channel method는 thread-safe하다.
- readback → map → publish → replace 전체 흐름은 원자적이지 않다.
- 같은 PLC에 대한 concurrent publish는 lost update를 만들 수 있다.
- 지금은 atomic update API나 semaphore skeleton을 구현하지 않는다.
- AH-RUNTIME-37에서 Scheduler single-writer invariant를 먼저 확정한다.

## 3. Background

AH-RUNTIME-35까지 Runtime 내부 polling publish path는 single event와 batch event 모두 검증되었다.

단일 event 흐름:

    ChannelPollingResult
            ↓
    PollingResultStateOrchestrator.PublishAsync(...)
            ↓
    PollingPublishCoordinator
            ↓
    RuntimeSnapshot

batch event 흐름:

    IReadOnlyCollection<ChannelPollingResult>
            ↓
    PollingResultStateOrchestrator.PublishBatchAsync(...)
            ↓
    PollingChannelUpdate batch
            ↓
    PollingPublishCoordinator.PublishAsync(batch)
            ↓
    RuntimeSnapshot / SnapshotChanged 1회

AH-RUNTIME-31에서는 `ChannelPollingResult`를 `RuntimePlcChannelState`로 변환하는 mapper boundary를 추가했다.

    ChannelPollingResult
            ↓
    RuntimePlcChannelStateMapper
            ↓
    RuntimePlcChannelState

AH-RUNTIME-33에서는 mapper 결과를 `PollingChannelUpdate`로 감싸고 `PollingPublishCoordinator`에 위임하는 orchestration boundary를 추가했다.

    ChannelPollingResult
            ↓
    PollingResultStateOrchestrator
            ↓
    PollingChannelUpdate
            ↓
    PollingPublishCoordinator

AH-RUNTIME-34에서는 in-memory 구성으로 `ChannelPollingResult`가 `RuntimeSnapshot`까지 반영되는지 end-to-end로 검증했다.

AH-RUNTIME-35에서는 batch `ChannelPollingResult`를 한 번의 `PollingPublishCoordinator.PublishAsync(batch)` 호출로 묶고, `RuntimeSnapshot / SnapshotChanged`가 batch당 1회 발생하는 흐름을 검증했다.

AH-RUNTIME-36은 이 흐름이 동시에 호출될 때 발생할 수 있는 lost update, snapshot ordering, event time ordering 리스크를 구현 없이 검토했다.

## 4. 확인한 lock / gate / thread-safety 구조

`InMemoryRuntimePlcChannel`:

- `_gate`를 보유한다.
- `GetRuntimeState()`는 lock 안에서 `_state`를 반환한다.
- `ReplaceState(...)`는 `PlcId` 검증 후 lock 안에서 `_state`를 교체한다.
- `GetState(capturedAt)`는 lock 안에서 publish DTO인 `ChannelRuntimeState`로 변환한다.
- 따라서 각 method 단위는 thread-safe하다.
- 하지만 readback → map → replace 전체를 하나의 lock으로 묶는 API는 없다.

`IWritableRuntimePlcChannel`:

- `GetRuntimeState()`를 제공한다.
- `ReplaceState(RuntimePlcChannelState state)`를 제공한다.
- atomic update 또는 transform API는 없다.

`RuntimePlcChannelState`:

- record 기반 Runtime-local state다.
- init property 중심이라 immutable에 가까운 구조다.
- `Version`, `Revision`, expected sequence 같은 optimistic concurrency 필드는 없다.
- `ConsecutiveFailures`, `LastSuccessAt`, `LastFailureAt`, `LastResponseMs`, `LastError` 같은 previous 기반 누적 / 보존 필드가 존재한다.

`RuntimePlcChannelStateMapper`:

- previous `RuntimePlcChannelState`와 `ChannelPollingResult`를 받아 next `RuntimePlcChannelState`를 계산한다.
- side-effect 없는 mapper에 가깝다.
- `ConsecutiveFailures` 증감, success reset, event timestamp 반영 같은 state transition 계산을 담당한다.
- 동시성 문제를 직접 해결할 위치는 아니다.

`PollingResultStateOrchestrator`:

- single `PublishAsync`와 batch `PublishBatchAsync`를 제공한다.
- item별 `GetRuntimeState()` 호출 후 `RuntimePlcChannelStateMapper.Map(...)`을 호출한다.
- `PollingPublishCoordinator`에 `PollingChannelUpdate` 또는 batch를 전달한다.
- batch 내부 duplicate `PlcId`는 막는다.
- 내부 lock / semaphore는 없다.
- 같은 orchestrator instance에 동시 `PublishAsync` / `PublishBatchAsync` 호출 가능성이 있다.

`PollingPublishCoordinator`:

- `PollingChannelUpdate` batch를 받아 channel lookup 후 `ReplaceState`를 호출한다.
- 성공 update가 1개 이상이면 `refreshSnapshotAsync`를 호출한다.
- batch 내 update 적용은 현재 for loop로 순차 처리한다.
- 내부 lock / semaphore는 없다.
- 같은 coordinator instance에 동시 `PublishAsync` 호출 가능성이 있다.

`RuntimeChannelRegistry`:

- channel lookup / collection 관리 책임을 가진다.
- `Add`, `TryGetChannel`, `GetChannels`, `GetStates`는 registry `_gate`로 collection 접근을 보호한다.
- `GetStates`는 lock 안에서 channel array snapshot을 만든 뒤, lock 밖에서 각 channel `GetState(capturedAt)`를 호출한다.
- lookup-only 책임을 유지한다.
- registry-level lock은 channel state update 원자성을 보장하지 않는다.

`InMemoryAutomationHubSupervisor`:

- `_gate`를 보유한다.
- `RefreshSnapshotAsync`는 registry state를 읽고 `RuntimeSnapshot`을 만든 뒤 `_gate` 안에서 `_currentSnapshot`과 `_revision`을 갱신한다.
- `SnapshotChanged`는 lock 밖에서 발생한다.
- `GetSnapshotAsync`는 `_gate` 안에서 cache를 읽는다.
- concurrent `RefreshSnapshotAsync` 호출 가능성이 있다.
- snapshot cache는 마지막 refresh 결과로 덮일 수 있다.

## 5. 현재 readback → map → publish → replace 흐름 분석

현재 single publish 흐름은 다음과 같다.

    PollingResultStateOrchestrator.PublishAsync(result)
            ↓
    PublishBatchAsync([result])
            ↓
    RuntimeChannelRegistry.TryGetChannel(result.PlcId)
            ↓
    channel as IWritableRuntimePlcChannel
            ↓
    writable.GetRuntimeState()
            ↓
    RuntimePlcChannelStateMapper.Map(previous, result)
            ↓
    new PollingChannelUpdate(result.PlcId, next)
            ↓
    PollingPublishCoordinator.PublishAsync(updates)
            ↓
    writable.ReplaceState(update.State)
            ↓
    refreshSnapshotAsync(...)

현재 batch publish 흐름은 다음과 같다.

    PollingResultStateOrchestrator.PublishBatchAsync(results)
            ↓
    item별 duplicate check
            ↓
    item별 registry lookup / writable check
            ↓
    item별 GetRuntimeState()
            ↓
    item별 RuntimePlcChannelStateMapper.Map(...)
            ↓
    PollingChannelUpdate batch 생성
            ↓
    PollingPublishCoordinator.PublishAsync(batch)
            ↓
    update별 ReplaceState(...)
            ↓
    refreshSnapshotAsync(...) 1회

문제의 핵심은 `GetRuntimeState()`와 `ReplaceState(...)` 각각은 lock 보호되지만, `GetRuntimeState → RuntimePlcChannelStateMapper.Map → PollingPublishCoordinator.PublishAsync → ReplaceState` 전체가 하나의 atomic transaction이 아니라는 점이다.

즉, 두 caller가 같은 PLC의 동일한 previous state를 읽고 서로 다른 next state를 만든 뒤 순서대로 replace할 수 있다. 이 경우 later replace가 earlier replace를 덮어쓴다.

현재 API만으로는 channel 내부 gate 안에서 previous readback과 next replace를 한 번에 수행할 수 없다.

## 6. Lost update 가능 시나리오

### 시나리오 A: 같은 PLC에 failure result 동시 입력

초기 상태:

    previous.ConsecutiveFailures = 0

Task A:

    GetRuntimeState() → ConsecutiveFailures = 0
    failure map → ConsecutiveFailures = 1

Task B:

    GetRuntimeState() → ConsecutiveFailures = 0
    failure map → ConsecutiveFailures = 1

적용 순서:

    Task A ReplaceState(1)
    Task B ReplaceState(1)

기대값이 2라면 실제 최종값은 1이 될 수 있다.

판정:

- 이것은 lost update로 보는 것이 맞다.
- 원인은 `GetRuntimeState`와 `ReplaceState` 각각은 lock 보호되지만, readback → map → replace 전체가 원자적이지 않기 때문이다.

### 시나리오 B: 같은 PLC에 success / failure 동시 입력

입력:

    Task A:
        success result OccurredAt = T1

    Task B:
        failure result OccurredAt = T2

리스크:

- `LastSuccessAt` / `LastFailureAt`은 각각 보존될 수 있으나, 최종 `HealthSeverity` / `PollingState` / `LastError`는 마지막 `ReplaceState` 기준으로 덮일 수 있다.
- 실제 event 발생 순서와 publish 적용 순서가 다를 수 있다.
- latest occurred event와 latest applied state가 항상 같은 의미는 아니다.
- success가 더 늦게 replace되면 failure의 `LastError`와 warning state가 clear될 수 있다.
- failure가 더 늦게 replace되면 success의 healthy/polling state가 warning/delayed state로 덮일 수 있다.

### 시나리오 C: 서로 다른 PLC batch 동시 입력

입력:

    Batch A:
        PLC-1, PLC-2

    Batch B:
        PLC-3, PLC-4

리스크:

- 서로 다른 channel이면 channel-level lost update 가능성은 낮다.
- 하지만 snapshot refresh가 동시에 여러 번 발생할 수 있다.
- `SnapshotChanged`가 여러 번 발생할 수 있다.
- snapshot cache는 마지막 refresh 결과로 갱신될 수 있다.
- `RuntimeSnapshot.CapturedAt` ordering은 refresh 호출 시작 시각과 cache overwrite 시점이 다를 수 있어 엄격한 global ordering 의미를 보장하지 않는다.

### 시나리오 D: overlapping PLC batch 동시 입력

입력:

    Batch A:
        PLC-1, PLC-2

    Batch B:
        PLC-2, PLC-3

리스크:

- `PLC-2`는 양쪽 batch에서 동시에 readback될 수 있다.
- batch 내부 duplicate `PlcId`는 AH-RUNTIME-35에서 막았지만, batch 간 duplicate는 막지 못한다.
- `PLC-2`의 previous state 기준이 모호해질 수 있다.
- lost update 가능성이 있다.
- batch 전체가 transaction이 아니므로 overlapping batch에 대한 all-or-nothing semantics도 없다.

## 7. Snapshot refresh / SnapshotChanged 동시성 리스크

`InMemoryAutomationHubSupervisor.RefreshSnapshotAsync`는 registry state를 읽고 snapshot을 만든 뒤 `_gate` 안에서 `_currentSnapshot`과 `_revision`을 갱신한다.

이 구조가 보장하는 것은 다음과 같다.

- `_currentSnapshot` field write는 `_gate`로 보호된다.
- `_revision` increment는 `_gate`로 보호된다.
- `GetSnapshotAsync` cache read는 `_gate`로 보호된다.
- `SnapshotChanged`는 `RefreshSnapshotAsync` 흐름에서만 발생한다.
- `ReplaceState`는 `SnapshotChanged`를 직접 발생시키지 않는다.

하지만 다음은 보장하지 않는다.

- concurrent `RefreshSnapshotAsync` 전체 serialize
- refresh 시작 순서와 cache overwrite 순서의 일치
- `SnapshotChanged` delivery 순서와 `CapturedAt` 순서의 일치
- 동시 publish caller 사이의 snapshot frame ordering

서로 다른 PLC batch가 동시에 publish되어도 channel-level lost update는 낮을 수 있지만, snapshot refresh는 여러 번 발생할 수 있다. 이 문제는 Scheduler가 polling cycle을 serialize하고 batch publish를 cycle 단위로 하나씩 수행하면 크게 줄일 수 있다.

## 8. 후보 A: 현재 구조 유지 + Scheduler single-writer 보장

판정:

- 1순위 권장안

내용:

- Runtime 내부 API는 지금 당장 변경하지 않는다.
- Scheduler가 polling cycle publish를 한 번에 하나만 수행하도록 보장한다.
- 같은 Runtime publish path는 single-writer로 사용한다.
- 같은 PLC는 한 polling cycle에 result 1개만 생성한다.
- timer reentrancy를 금지한다.
- cancellation 중 partial publish 정책을 명확히 한다.
- batch publish와 snapshot refresh ordering을 Scheduler boundary에서 검토한다.

장점:

- Runtime 구조 변경 최소
- interface 확장 없음
- 현재 테스트 유지
- Scheduler 설계에서 cycle ordering을 통제할 수 있음
- 아직 실제 `PollingScheduler`가 없는 현재 단계에 적합

위험:

- Runtime API 자체는 arbitrary concurrent caller에 완전 안전하지 않음
- 외부 caller가 orchestrator를 직접 동시에 호출하면 lost update 가능
- single-writer assumption이 문서화되지 않으면 나중에 깨질 수 있음

결론:

- AH-RUNTIME-37 `PollingScheduler` Boundary Review의 핵심 invariant로 넘기는 것이 적절하다.

## 9. 후보 B: Orchestrator semaphore

판정:

- 지금은 보류
- Scheduler 설계 후에도 runtime-level defensive guard가 필요하면 재검토

내용:

- `PollingResultStateOrchestrator` 내부에 `SemaphoreSlim`을 두고 `PublishAsync` / `PublishBatchAsync`를 serialize하는 방식이다.

장점:

- 같은 orchestrator instance에 대해서는 readback → map → publish 구간을 serialize할 수 있다.
- 구현이 비교적 단순하다.
- 임시 안전장치로는 가능성이 있다.

위험:

- orchestrator instance가 여러 개면 보장이 깨진다.
- 전체 batch publish가 serialize되어 throughput 저하 가능성이 있다.
- `RefreshSnapshotAsync`까지 semaphore 범위에 들어가면 lock scope가 커진다.
- channel 단의 근본 원자성을 해결하지는 못한다.
- Scheduler single-writer invariant를 대체하지 못한다.

결론:

- 지금 단계에서 바로 적용하지 않는다.
- AH-RUNTIME-37 이후 필요성이 남으면 별도 skeleton 후보로 재검토한다.

## 10. 후보 C: Atomic channel update API

판정:

- 정합성은 가장 좋지만 현재는 이른 설계

후보 API:

    UpdateState(Func<RuntimePlcChannelState, RuntimePlcChannelState> updater)

내용:

- channel 내부 gate 안에서 previous readback과 next replace를 한 번에 수행하는 방식이다.

장점:

- same PLC lost update를 직접 줄일 수 있다.
- channel state update 원자성에 가장 직접적인 해결책이다.
- Scheduler 외 caller에도 더 안전하다.

위험:

- `IWritableRuntimePlcChannel` interface 확장 필요
- 구현체와 테스트 영향 큼
- mapper가 gate 안에서 실행될 경우 lock scope 증가
- future mapper가 무거워지면 위험
- update 후 publish / refresh와의 관계는 여전히 별도
- batch 전체 원자성은 해결하지 못함

결론:

- 지금 Scheduler 전 단계에서 바로 확장하기에는 이르다.
- multi-writer Runtime API 요구가 확정되면 별도 단계에서 검토한다.

## 11. 후보 D: Optimistic concurrency version

판정:

- 현재 in-memory Runtime 단계에서는 과함

내용:

- `RuntimePlcChannelState`에 `Version` / `Revision` / `Sequence`를 추가하고, `ReplaceState` 시 expected version을 확인하는 방식이다.

장점:

- lost update 감지 가능
- 동시성 충돌을 명시적으로 표현 가능
- distributed / multi-writer 구조에 유리할 수 있음

위험:

- `RuntimePlcChannelState` 모델 변경
- mapper / tests / snapshot 영향
- retry / conflict 정책 필요
- batch partial failure와 결합되면 복잡도 증가
- 현재 단계에서는 과한 설계

결론:

- distributed / multi-writer 요구가 실제로 올라올 때 후순위로 검토한다.

## 12. 후보 E: Coordinator per-channel serialize

판정:

- 비권장

내용:

- `PollingPublishCoordinator`에서 channel별 lock 또는 serialize를 담당하는 방식이다.

장점:

- publish path 중앙에서 제어 가능
- batch update 처리와 가까움

위험:

- coordinator 시점에는 이미 orchestrator에서 previous readback과 map이 끝난 뒤다.
- stale previous 문제를 근본적으로 해결하지 못한다.
- coordinator 책임이 state consistency lock 관리까지 커진다.
- `PollingPublishCoordinator`는 `PollingChannelUpdate` batch publish 조율자로 유지해야 한다.
- `ChannelPollingResult` 해석 책임을 coordinator에 넣지 않는 기존 boundary와도 맞지 않는다.

결론:

- coordinator는 해결 지점으로 적절하지 않다.

## 13. 권장안

AH-RUNTIME-36에서는 구현하지 않는다.

권장 흐름:

1. 현재 lost update 리스크를 명확히 문서화한다.
2. Runtime API가 arbitrary concurrent caller에 대해 완전 원자적이지 않음을 명시한다.
3. AH-RUNTIME-37 `PollingScheduler` Boundary Review에서 single-writer invariant를 핵심 계약으로 정의한다.
4. Scheduler가 polling cycle overlap을 금지하고, Runtime publish path를 serialize하는지 검토한다.
5. Scheduler 설계 후에도 defensive guard가 필요하면 Orchestrator semaphore 또는 atomic channel update API를 별도 단계에서 재검토한다.

AH-RUNTIME-37에 넘길 핵심 invariant:

- polling cycle overlap 금지
- Runtime publish path single-writer 보장
- 같은 PLC는 한 cycle에 result 1개
- timer reentrancy 금지
- cancellation 중 partial publish 정책 명시
- batch publish와 snapshot refresh ordering 명시
- Scheduler가 Runtime publish path에 대한 유일한 writer인지 확인

## 14. OccurredAt / CapturedAt 관점 리스크

동시성 검토에서도 `OccurredAt`과 `CapturedAt` 의미는 분리해야 한다.

- `OccurredAt`은 polling event 발생 시각이다.
- `CapturedAt`은 `RuntimeSnapshot` frame 수집 시각이다.
- 동시 event가 들어오면 publish 적용 순서와 event 발생 순서가 다를 수 있다.
- `LastSuccessAt` / `LastFailureAt`은 event time이다.
- `HealthSeverity` / `PollingState`는 latest applied state일 수 있다.
- latest occurred event와 latest applied state가 항상 같은 의미는 아닐 수 있다.
- success와 failure가 동시에 들어오면 event time은 각각 남을 수 있어도 current health / polling state는 마지막 replace 기준으로 결정될 수 있다.
- Scheduler가 event ordering / cycle ordering을 어떻게 보장할지 AH-RUNTIME-37에서 다뤄야 한다.

## 15. AH-RUNTIME-37 후보 및 우선순위

1순위:

- AH-RUNTIME-37 `PollingScheduler` Boundary Review
- Scheduler가 single-writer invariant를 보장하는 방식 검토
- polling cycle ordering / cancellation / batch publish serialize 검토
- timer reentrancy 금지 검토
- Runtime publish path의 writer ownership 검토

2순위:

- Orchestrator semaphore skeleton
- Scheduler 설계 후에도 runtime-level defensive guard가 필요하면 검토
- 같은 orchestrator instance에 대한 serialize guard만으로 충분한지 판단

3순위:

- Atomic channel update skeleton
- multi-writer Runtime API 지원이 실제 요구로 확정되면 검토
- `IWritableRuntimePlcChannel.UpdateState(...)` 같은 transform API 후보 검토

4순위:

- Optimistic concurrency Boundary Review
- `Version` / `Revision` 기반 replace conflict 감지 검토
- distributed / multi-writer 성격이 올라올 때 후순위 검토

## 16. 제외한 범위

이번 AH-RUNTIME-36에서는 다음을 하지 않았다.

- production code 수정
- test code 수정
- skeleton 추가
- interface 확장
- enum 추가
- `Contracts` DTO 수정
- WPF 수정
- `XgtDriverCore` 연결
- `FakePlc` 연결
- `XgtChannelRunner` 연결
- `PollingScheduler` timer / loop 구현
- reconnect 정책 구현
- `ContextPublisher` 자동 publish 재도입
- Closeout 생성 외 작업
- commit

## 17. 실행한 명령

AH-RUNTIME-36 Boundary Review 당시 실행한 명령:

- `git status --short`
- `rg "_gate|lock|SemaphoreSlim|Monitor|Concurrent|ReaderWriter" src/CAAutomationHub.Runtime tests/CAAutomationHub.Runtime.Tests`
- `rg "GetRuntimeState|ReplaceState|GetState" src/CAAutomationHub.Runtime tests/CAAutomationHub.Runtime.Tests`
- `rg "PollingResultStateOrchestrator" src tests`
- `rg "PollingPublishCoordinator" src tests`
- `rg "RefreshSnapshotAsync|SnapshotChanged|GetSnapshotAsync" src tests`
- `rg "RuntimePlcChannelState" src tests`
- `rg "Version|Revision|Sequence|UpdatedAt" src/CAAutomationHub.Runtime tests/CAAutomationHub.Runtime.Tests`
- `rg "Task.WhenAll|Parallel|concurrent|race|lost update" tests`
- 관련 파일 `Get-Content` 확인

보조 검색 중 Windows wildcard 경로 오류가 난 non-required `rg`가 한 번 있었고, 이후 직접 파일 / 디렉터리 기준으로 확인을 완료했다.

AH-RUNTIME-36 Closeout 작성 후 실행한 검증:

- `git diff -- docs/harness/AH-RUNTIME-36.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-36.md`

## 18. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-36 Boundary Review 결론을 closeout 문서로 기록함
- 실제 lock / gate / thread-safety 구조를 기록함
- `GetRuntimeState → Map → PublishAsync → ReplaceState` 전체 구간이 원자적이지 않음을 기록함
- same PLC single publish 동시 호출 lost update 시나리오를 기록함
- batch 동시 호출 및 overlapping PLC batch 리스크를 기록함
- snapshot refresh / `SnapshotChanged` 동시성 리스크를 기록함
- 후보 A / B / C / D / E 검토 결과를 기록함
- AH-RUNTIME-37 `PollingScheduler` Boundary Review를 1순위 다음 단계로 기록함
- `OccurredAt` / `CapturedAt` 의미 분리와 ordering 리스크를 기록함
- 제외한 범위를 기록함
- `ContextPublisher` 자동 publish 미사용 정책을 유지함

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
