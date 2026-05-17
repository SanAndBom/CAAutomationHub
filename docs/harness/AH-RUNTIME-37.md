# AH-RUNTIME-37 Closeout

## 1. Summary

AH-RUNTIME-37은 `PollingScheduler`를 구현하기 전에 Runtime polling publish path에 필요한 scheduler / cycle boundary를 검토한 Boundary Review 단계다.

검토 결과, 현재 Runtime project에는 실제 `PollingScheduler`, `PollingCycle`, `PeriodicTimer`, `HostedService` 기반 scheduler 타입이 없다. 지금 필요한 것은 실제 timer loop가 아니라 Runtime publish path의 single-writer invariant를 보장하는 cycle boundary다.

따라서 AH-RUNTIME-38은 `PollingScheduler` timer loop가 아니라 manual trigger 가능한 `PollingCycleCoordinator` 또는 `PollingCyclePublisher` skeleton부터 시작하는 것이 안전하다고 판단한다.

이번 단계에서는 production code, test code, scheduler skeleton, timer loop, interface, enum, Contracts DTO를 수정하지 않았다. `ContextPublisher` 자동 publish도 재도입하지 않았다. Runtime 작업 기록은 `docs/harness/AH-RUNTIME-xx.md` Closeout 문서를 primary historical record로 사용한다.

## 2. Goal

AH-RUNTIME-37의 목표는 `PollingScheduler`의 책임과 경계를 구현 전에 검토하는 것이다.

핵심 질문은 다음과 같았다.

- 나중에 여러 PLC를 주기적으로 polling할 때 Runtime publish path single-writer invariant를 어떻게 보장할 것인가?
- Scheduler가 `PollingResultStateOrchestrator.PublishBatchAsync(...)`를 직접 호출하는 것이 적절한가?
- Scheduler가 `PollingPublishCoordinator`, `RuntimePlcChannelStateMapper`, `ReplaceState`, `RefreshSnapshotAsync`를 직접 알아도 되는가?
- 실제 timer loop보다 cycle boundary를 먼저 고정해야 하는가?
- cancellation, overlap, reentrancy, same PLC one result per cycle 정책을 어디서 보장해야 하는가?

이번 작업은 구현 단계가 아니라 Boundary Review 결과를 정리하는 단계다.

## 3. Background

AH-RUNTIME-36에서는 Runtime polling publish path의 동시성 / lost update 리스크를 검토했다.

문제의 핵심 흐름은 다음과 같다.

    GetRuntimeState()
            ↓
    RuntimePlcChannelStateMapper.Map(...)
            ↓
    PollingPublishCoordinator.PublishAsync(...)
            ↓
    IWritableRuntimePlcChannel.ReplaceState(...)

`InMemoryRuntimePlcChannel.GetRuntimeState`, `ReplaceState`, `GetState`는 각각 method 단위 lock 보호를 받는다. 하지만 readback -> map -> publish -> replace 전체 구간은 하나의 원자적 update가 아니다.

AH-RUNTIME-36에서 AH-RUNTIME-37로 넘긴 핵심 invariant는 다음이다.

- polling cycle overlap 금지
- Runtime publish path single-writer 보장
- 같은 PLC는 한 cycle에 result 1개
- timer reentrancy 금지
- cancellation 중 partial publish 정책 명시
- batch publish와 snapshot refresh ordering 명시
- Scheduler가 Runtime publish path에 대한 유일한 writer인지 확인

AH-RUNTIME-37은 이 invariant를 기준으로 `PollingScheduler`가 실제로 필요한지, 아니면 더 작은 cycle boundary가 먼저 필요한지 검토했다.

## 4. 확인한 기존 Scheduler / Polling 관련 타입

확인 결과:

- Runtime project에는 실제 `PollingScheduler` 타입이 없다.
- Runtime project에는 `PollingCycle` 타입이 없다.
- Runtime project에는 `PeriodicTimer` 기반 scheduler가 없다.
- Runtime project에는 `HostedService` 기반 scheduler가 없다.
- WPF에는 `DispatcherTimer`가 있으나 Dashboard UI refresh / fake event stream 용도이며 Runtime polling scheduler가 아니다.
- tests에는 `PollingPublishCoordinator_DoesNotExposeSchedulerOrLoopMembers`가 있어 Coordinator가 scheduler / timer 책임을 갖지 않는 계약을 이미 확인한다.

현재 존재하는 Runtime polling 타입:

- `PollingResultStateOrchestrator`
- `PollingPublishCoordinator`
- `ChannelPollingResult`
- `ChannelPollingFailureKind`
- `RuntimePlcChannelStateMapper`
- `PollingChannelUpdate`
- `PollingPublishResult`

## 5. 현재 Runtime publish path와 Scheduler 연결 가능성

현재 Runtime 내부 publish path는 다음과 같다.

    IReadOnlyCollection<ChannelPollingResult>
            ↓
    PollingResultStateOrchestrator.PublishBatchAsync(...)
            ↓
    item별 registry lookup / writable check
            ↓
    item별 previous RuntimePlcChannelState readback
            ↓
    RuntimePlcChannelStateMapper.Map(...)
            ↓
    PollingChannelUpdate batch
            ↓
    PollingPublishCoordinator.PublishAsync(batch)
            ↓
    IWritableRuntimePlcChannel.ReplaceState(...)
            ↓
    InMemoryAutomationHubSupervisor.RefreshSnapshotAsync(...)
            ↓
    RuntimeSnapshot / SnapshotChanged

Scheduler 또는 Cycle boundary가 호출해야 할 안전한 entry point는 `PollingResultStateOrchestrator.PublishBatchAsync(...)`다.

Scheduler가 직접 호출하면 안 되는 대상:

- `PollingPublishCoordinator`
- `RuntimePlcChannelStateMapper`
- `ReplaceState`
- `RefreshSnapshotAsync`
- `SnapshotChanged`

의미:

- Scheduler는 Runtime state mapping과 snapshot refresh details를 몰라야 한다.
- Scheduler는 polling cycle ordering과 single-writer guard에 집중해야 한다.
- Runtime publish path 내부 책임은 기존 orchestration boundary에 위임해야 한다.

## 6. AH-RUNTIME-36 single-writer invariant 반영

AH-RUNTIME-36의 single-writer invariant는 AH-RUNTIME-37에서도 그대로 유효하다.

현재 `PollingResultStateOrchestrator`는 single `PublishAsync`와 batch `PublishBatchAsync`를 제공하지만 내부 lock / semaphore는 없다. 같은 orchestrator instance에 동시 `PublishAsync` / `PublishBatchAsync` 호출이 가능하다.

현재 `PollingPublishCoordinator`도 내부 lock / semaphore가 없다. 같은 coordinator instance에 동시 `PublishAsync` 호출이 가능하다.

따라서 Scheduler / Cycle boundary가 다음을 보장해야 한다.

- 같은 Runtime publish path에 동시 `PublishBatchAsync` 호출 금지
- cycle N publish 완료 후 cycle N+1 publish 시작
- 같은 PLC는 한 cycle에 `ChannelPollingResult` 1개 이하
- publish 중 다음 cycle overlap 금지
- `SnapshotChanged` ordering은 cycle publish ordering을 따르도록 운영

## 7. 후보 A: Scheduler가 Orchestrator.PublishBatchAsync만 호출

판정:

- 권장

내용:

Scheduler / Cycle boundary는 result 수집과 cycle ordering에 집중하고, state 해석과 publish는 `PollingResultStateOrchestrator`에 맡기는 방식이다.

후보 흐름:

    PollingScheduler 또는 PollingCycleCoordinator
            ↓
    polling target list
            ↓
    polling operation 실행 또는 외부 batch 수신
            ↓
    ChannelPollingResult batch 생성
            ↓
    PollingResultStateOrchestrator.PublishBatchAsync(batch)

장점:

- 기존 Runtime orchestration boundary를 그대로 활용한다.
- Scheduler는 event 생성과 cycle ordering에 집중할 수 있다.
- State mapping / publish / snapshot refresh는 기존 경로에 위임된다.
- Runtime publish path single-writer 보장을 Scheduler / Cycle boundary가 수행할 수 있다.
- Scheduler가 `PollingPublishCoordinator`나 `RuntimePlcChannelStateMapper`를 알 필요가 없다.

위험:

- Scheduler가 result 생성까지 책임지면 driver adapter boundary가 섞일 수 있다.
- polling operation abstraction이 필요할 수 있다.
- target discovery source가 아직 불명확하다.
- cancellation 중 partial result 정책을 명확히 해야 한다.

결론:

- AH-RUNTIME-38 skeleton 후보로 적절하다.
- 단, 실제 timer loop가 아니라 manual trigger 가능한 cycle boundary부터 시작하는 것이 안전하다.

## 8. 후보 B: Scheduler가 Coordinator를 직접 호출

판정:

- 비권장

이유:

- Scheduler가 state mapping 책임을 알게 된다.
- Scheduler가 previous state readback / duplicate / partial result 정책을 알게 될 위험이 있다.
- `PollingResultStateOrchestrator` boundary를 우회한다.
- Scheduler가 Runtime 내부 구조를 너무 많이 알게 된다.

결론:

- 특별한 이유가 없으면 Scheduler는 `PollingPublishCoordinator`를 직접 호출하지 않는다.

## 9. 후보 C: PollingOperation / PollingSource abstraction

판정:

- 필요하지만 AH-RUNTIME-38에 바로 넣기에는 이르다.
- AH-RUNTIME-39 후보가 더 안전하다.

후보 개념:

    IPollingOperation
        ExecuteAsync(PollingTarget target, CancellationToken token)
        → ChannelPollingResult

또는:

    IChannelPollingSource
        PollAsync(PollingTarget target, CancellationToken token)
        → ChannelPollingResult

장점:

- Scheduler가 XGT / FakePlc / real driver를 모르게 할 수 있다.
- 테스트에서 fake polling source로 cycle 테스트가 가능하다.
- 나중에 `XgtDriverCore` adapter가 `ChannelPollingResult` 생성자로 들어올 위치가 생긴다.

위험:

- 아직 target model이 불명확하다.
- 실제 driver integration 전이라 추상화가 이를 수 있다.
- Scheduler skeleton 전에 target / source boundary review가 필요할 수 있다.

결론:

- AH-RUNTIME-38에서는 source abstraction을 최소화하거나 제외한다.
- AH-RUNTIME-39에서 PollingSource / Driver Adapter boundary를 별도로 검토하는 편이 안전하다.

## 10. 후보 D: CyclePublisher 형태

판정:

- 현재 단계에서 가장 정확한 방향

내용:

현재 필요한 것은 timer scheduler가 아니라 외부에서 받은 batch를 single-writer guard 아래 `PublishBatchAsync`로 넘기는 수동 cycle boundary다.

후보 흐름:

    external polling producer 또는 test
            ↓
    PollingCycleCoordinator / PollingCyclePublisher
            ↓
    ChannelPollingResult batch
            ↓
    PollingResultStateOrchestrator.PublishBatchAsync

장점:

- Scheduler와 driver polling을 완전히 분리할 수 있다.
- Runtime publish single-writer guard에 집중할 수 있다.
- fake event 기반 테스트가 쉽다.
- timer loop 없이 cycle overlap / publish ordering 정책을 먼저 고정할 수 있다.

위험:

- 이름이 `PollingScheduler`보다는 `PollingCyclePublisher` / `PollingCycleCoordinator`에 가깝다.
- 실제 polling timer 역할은 아직 빠져 있다.
- 나중에 진짜 Scheduler가 추가로 필요할 수 있다.

결론:

- AH-RUNTIME-38은 `PollingCycleCoordinator` 또는 `PollingCyclePublisher` skeleton이 적절하다.
- manual `TriggerCycleAsync` 또는 `PublishCycleAsync` 형태를 우선 검토한다.

## 11. 후보 E: Orchestrator semaphore 선적용

판정:

- 지금은 보류

이유:

- 같은 orchestrator instance만 보호한다.
- scheduler cycle semantics를 대체하지 못한다.
- AH-RUNTIME-36에서 1순위로 보지 않았던 후보다.
- Scheduler 설계 후에도 외부 direct caller 리스크가 남으면 defensive guard로 재검토한다.

결론:

- AH-RUNTIME-38 이전에 적용하지 않는다.
- Scheduler / Cycle boundary의 single-writer invariant가 우선이다.

## 12. 권장안

AH-RUNTIME-38은 `PollingCycleCoordinator` 또는 `PollingCyclePublisher` skeleton으로 진행하는 것을 권장한다.

권장 범위:

- timer 없음
- driver 없음
- source abstraction 없음 또는 최소화
- 외부 batch 입력
- in-flight cycle 1개 이하
- overlap 발생 시 skip
- `PollingResultStateOrchestrator.PublishBatchAsync`만 호출
- publish 시작 후 cycle N 완료 전 cycle N+1 시작 금지
- Scheduler가 Coordinator / Mapper / `ReplaceState` / `RefreshSnapshotAsync` 직접 호출 금지

이름 후보:

- `PollingCycleCoordinator`
- `PollingCyclePublisher`

현재 단계에서는 `PollingScheduler`보다 위 이름들이 더 정확하다.

## 13. Polling cycle overlap 정책

초기 정책:

- overlap 발생 시 skip
- queue에 넣지 않음
- 이전 cycle publish가 완료되어야 다음 cycle 시작
- cycle reentrancy 금지
- skip된 cycle은 result로 표현하거나 count로 남길 수 있음

이유:

- queue 방식은 stale polling result를 만들 수 있다.
- 초기 skeleton에서는 단순하고 안전한 skip 정책이 적절하다.

## 14. Timer reentrancy 정책

AH-RUNTIME-38에서는 timer를 구현하지 않는다.

이유:

- 아직 실제 polling source / target model이 없다.
- timer loop보다 cycle boundary가 먼저 필요하다.
- timer 구현은 reentrancy, cancellation, shutdown, interval drift를 함께 다뤄야 해서 이르다.

향후 후보:

- `PeriodicTimer`
- HostedService-style async loop

주의:

- callback 재진입 가능한 `System.Threading.Timer` 방식은 초기 방향으로 비권장이다.

## 15. Same PLC one result per cycle 정책

- Scheduler / Cycle boundary가 같은 PLC는 한 cycle에 `ChannelPollingResult` 1개 이하를 보장해야 한다.
- Orchestrator의 duplicate `PlcId` skip은 방어 장치일 뿐 primary 정책은 Scheduler / Cycle boundary에 있어야 한다.
- address-level 상세 병합은 아직 제외한다.
- PLC-level result 1개를 유지한다.

## 16. Cancellation / partial publish 정책

중요 리스크:

- 현재 `PollingPublishCoordinator.PublishAsync`는 update loop 중 cancellation을 확인한다.
- 일부 `ReplaceState` 후 refresh 전에 cancellation이 발생할 수 있다.
- 이 경우 state는 바뀌었지만 snapshot refresh가 안 될 수 있다.

권장 정책:

- publish 시작 전 cancellation: 수집된 partial results는 버림
- publish 시작 후 graceful cancellation: publish와 refresh를 끝까지 완료
- force cancellation: 별도 정책 전까지 AH-RUNTIME-38에서 제외

AH-RUNTIME-38에서는 cancellation 정책을 cycle boundary에서 어떻게 보장할지 검토해야 한다.

## 17. Failure result 생성 정책

- polling operation exception은 vendor-neutral `ChannelPollingResult.Failure(...)`로 변환하는 방향이 적절하다.
- timeout / connection / protocol / unexpected / unknown은 `ChannelPollingFailureKind`로 표현 가능하다.
- cancellation은 기본적으로 failure result가 아니라 skipped cycle로 보는 것이 안전하다.
- XGT specific exception은 아직 끌어오지 않는다.

## 18. Runtime publish single-writer invariant

Scheduler / Cycle boundary가 보장해야 할 invariant:

- publish in-flight는 1개 이하
- 동시 `PublishBatchAsync` 호출 금지
- cycle N publish 완료 후 cycle N+1 시작
- 같은 PLC는 cycle당 result 1개 이하
- snapshot refresh는 성공 update가 있으면 cycle당 최대 1회
- Scheduler 외부 direct publish caller를 운영 계약상 금지
- Scheduler / Cycle boundary는 Runtime publish path의 유일한 writer가 되어야 함

## 19. OccurredAt / CapturedAt / cycle time 분리

시간 계층:

- `ChannelPollingResult.OccurredAt`: 개별 polling operation 성공 / 실패 시각
- `RuntimeSnapshot.CapturedAt`: refresh 후 snapshot frame 수집 시각
- cycle start time: cycle 시작 시각
- cycle end time: cycle 종료 시각
- publish start time: Runtime publish 시작 시각
- publish end time: Runtime publish 종료 시각

중요:

- cycle time, `OccurredAt`, `CapturedAt`은 서로 다르다.
- 여러 PLC result는 서로 다른 `OccurredAt`을 가질 수 있다.
- batch snapshot은 하나의 `CapturedAt`을 가진다.
- Scheduler가 cycle timestamp를 도입한다면 `OccurredAt` / `CapturedAt`과 혼동하지 않아야 한다.

## 20. AH-RUNTIME-38 Skeleton 후보

권장 후보:

- `PollingCycleCoordinator` 또는 `PollingCyclePublisher` skeleton

후보 파일:

- `src/CAAutomationHub.Runtime/Polling/PollingCycleCoordinator.cs`
- `src/CAAutomationHub.Runtime/Polling/PollingCyclePublishResult.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingCycleCoordinatorTests.cs`

초기 skeleton 범위:

- timer 없음
- driver 없음
- source abstraction 없음 또는 최소화
- manual `TriggerCycleAsync` 또는 `PublishCycleAsync`
- 외부에서 `ChannelPollingResult` batch 입력
- in-flight cycle guard
- overlap 발생 시 skip
- `PollingResultStateOrchestrator.PublishBatchAsync`만 호출
- cancellation before publish는 discard
- publish started 이후에는 refresh까지 완료하는 정책 검토

테스트 후보:

- concurrent manual trigger 중 하나만 publish
- overlap 발생 시 skip
- `PublishBatchAsync`만 호출
- duplicate PLC는 Cycle boundary에서 거부 또는 skip
- cancellation before publish는 discard
- publish started 이후에는 refresh까지 완료

제외할 범위:

- timer loop
- `PeriodicTimer` / `HostedService`
- `XgtDriverCore`
- `FakePlc`
- Real PLC
- WPF
- reconnect 정책
- driver adapter
- target / source abstraction 본격 구현

## 21. 제외한 범위

이번 AH-RUNTIME-37에서는 다음을 하지 않았다.

- production code 수정
- test code 수정
- scheduler skeleton 추가
- timer loop 구현
- interface 확장
- enum 추가
- Contracts DTO 수정
- WPF 수정
- `XgtDriverCore` 연결
- `FakePlc` 연결
- `XgtChannelRunner` 연결
- reconnect 정책 구현
- `ContextPublisher` 자동 publish 재도입
- Closeout 생성 외 작업
- commit

## 22. 실행한 명령

AH-RUNTIME-37 Boundary Review 당시 실행한 명령:

- `git status --short`
- `rg "PollingScheduler|Scheduler|PollingCycle|Cycle|PeriodicTimer|Timer|HostedService" src tests`
- `rg "PollingResultStateOrchestrator" src tests`
- `rg "PublishBatchAsync|PublishAsync" src/CAAutomationHub.Runtime tests/CAAutomationHub.Runtime.Tests`
- `rg "ChannelPollingResult" src tests`
- `rg "PollingChannelUpdate|PollingPublishCoordinator" src tests`
- `rg "CancellationToken|Cancel|Cancelled" src/CAAutomationHub.Runtime tests/CAAutomationHub.Runtime.Tests`
- `rg "SnapshotChanged|CapturedAt|OccurredAt" src tests`
- `rg "SemaphoreSlim|lock|_gate|reentrancy|overlap|single-writer" src tests docs/harness`
- 관련 Runtime source / tests / closeout `Get-Content`

AH-RUNTIME-37 Closeout 작성 후 실행한 검증:

- `git diff -- docs/harness/AH-RUNTIME-37.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-37.md`

## 23. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-37 Boundary Review 결과를 closeout 문서로 기록함
- 현재 Runtime project에 실제 scheduler 타입이 없음을 기록함
- 현재 Runtime publish path와 안전한 entry point를 기록함
- Scheduler가 직접 호출하면 안 되는 boundary를 기록함
- AH-RUNTIME-36에서 넘긴 single-writer invariant를 반영함
- 후보 A / B / C / D / E 검토 결과를 기록함
- AH-RUNTIME-38 권장안을 `PollingCycleCoordinator` 또는 `PollingCyclePublisher` skeleton으로 기록함
- overlap / reentrancy / same PLC one result per cycle / cancellation / failure result 정책을 기록함
- `OccurredAt` / `CapturedAt` / cycle time 분리 의미를 기록함
- 제외한 범위와 실행 명령을 기록함
- `ContextPublisher` 자동 publish 미사용 정책을 유지함

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
