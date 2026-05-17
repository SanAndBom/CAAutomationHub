# AH-RUNTIME-41 Closeout

## 1. Summary

AH-RUNTIME-41은 `ChannelPollingTarget` 목록에서 `ChannelPollingResult` batch가 만들어지는 production boundary를 검토한 Boundary Review 단계다.

AH-RUNTIME-40까지 Runtime에는 PLC-level polling target model과 provider boundary가 추가되었다. 현재 Runtime에는 `IPollingTargetProvider.GetTargetsAsync(...)`가 `IReadOnlyCollection<ChannelPollingTarget>`을 반환하는 target boundary가 있고, AH-RUNTIME-38에서 추가한 `PollingCycleCoordinator.PublishCycleAsync(...)`가 `IReadOnlyCollection<ChannelPollingResult>` batch를 single-writer로 publish하는 cycle boundary가 있다.

아직 비어 있는 구간은 다음이다.

    IReadOnlyCollection<ChannelPollingTarget>
            -> target별 polling operation
            -> IReadOnlyCollection<ChannelPollingResult>

검토 결과, `PollingCycleCoordinator`를 확장하지 않는 것이 안전하다. `PollingCycleCoordinator`는 `ChannelPollingResult` batch publish single-writer boundary로 유지하고, target discovery, operation execution, result assembly는 별도 producer 계열 service가 담당하는 방향이 적절하다.

권장 이름은 `PollingResultProducer` 또는 `ChannelPollingResultProducer`다. `IChannelPollingSource` / Driver Adapter skeleton은 AH-RUNTIME-42 Boundary Review 또는 skeleton 단계로 넘기는 것이 안전하다.

이번 작업은 Boundary Review 결과를 문서화한 단계이며 production code, test code, Contracts DTO, WPF, XGT, FakePlc, XgtChannelRunner, scheduler, source, driver adapter는 수정하지 않았다. `ContextPublisher` 자동 publish도 재도입하지 않았다.

## 2. Goal

AH-RUNTIME-41의 목표는 target 목록에서 `ChannelPollingResult` batch가 만들어지는 production boundary를 검토하는 것이다.

핵심 질문은 다음이었다.

- `IPollingTargetProvider`가 제공한 `ChannelPollingTarget` 목록을 누가 읽을 것인가?
- target별 polling operation은 누가 수행할 것인가?
- polling success / failure를 누가 `ChannelPollingResult`로 변환할 것인가?
- 만들어진 `ChannelPollingResult` batch를 누가 `PollingCycleCoordinator`에 넘길 것인가?

이번 단계는 구현이 아니라 Boundary Review다. 따라서 `PollingSource` interface, Driver Adapter, PollingOperation skeleton, scheduler timer loop, XGT / FakePlc 연결은 추가하지 않았다.

## 3. Background

AH-RUNTIME-38까지 Runtime publish path 앞단은 다음과 같이 정리되어 있다.

    외부 ChannelPollingResult batch
            -> PollingCycleCoordinator
            -> single-writer / overlap guard
            -> IPollingResultBatchPublisher.PublishBatchAsync(...)
            -> PollingResultStateOrchestrator
            -> PollingPublishCoordinator
            -> RuntimeSnapshot / SnapshotChanged

AH-RUNTIME-40에서는 Runtime 내부 target boundary가 추가되었다.

    IPollingTargetProvider.GetTargetsAsync(...)
            -> IReadOnlyCollection<ChannelPollingTarget>

하지만 다음 구간은 아직 구현되지 않았다.

    IReadOnlyCollection<ChannelPollingTarget>
            -> target별 polling operation
            -> IReadOnlyCollection<ChannelPollingResult>
            -> PollingCycleCoordinator.PublishCycleAsync(...)

AH-RUNTIME-41은 이 사이의 책임 경계를 실제 Driver Adapter / `XgtDriverCore` / `FakePlc` 연결 전에 검토했다.

## 4. 확인한 target / cycle / result 관련 타입

`ChannelPollingTarget`

- Runtime 내부 `CAAutomationHub.Runtime.Polling` namespace 타입이다.
- `sealed record`다.
- `PlcId` 단일 public property만 가진다.
- null / empty / whitespace `PlcId`는 `ArgumentException`으로 거부한다.
- XGT address / datatype / count를 포함하지 않는다.
- Contracts DTO가 아니며 WPF DTO도 아니다.

`IPollingTargetProvider`

- `GetTargetsAsync(CancellationToken)` signature를 가진다.
- 반환 타입은 `ValueTask<IReadOnlyCollection<ChannelPollingTarget>>`이다.
- `ChannelPollingResult`를 반환하지 않는다.
- polling execution 책임을 갖지 않는다.

`RuntimeChannelPollingTargetProvider`

- `RuntimeChannelRegistry.GetChannels()`를 읽는다.
- 각 channel의 `PlcId`만 `ChannelPollingTarget`으로 투영한다.
- registry를 수정하지 않는다.
- channel state를 읽지 않는다.
- polling execution을 수행하지 않는다.

`PollingCycleCoordinator`

- `PublishCycleAsync(IReadOnlyCollection<ChannelPollingResult>, CancellationToken)` signature를 가진다.
- target model을 모른다.
- target provider를 모른다.
- source / driver / registry discovery 책임을 모른다.
- single-writer / overlap skip boundary로 동작한다.

`ChannelPollingResult`

- `PlcId`
- `OccurredAt`
- `IsSuccess`
- `ResponseTimeMs`
- `FailureKind`
- `ErrorMessage`
- target identity를 별도로 직접 포함하지 않는다.
- PLC-level / vendor-neutral polling event result다.

`ChannelPollingFailureKind`

- `Timeout`
- `Connection`
- `Protocol`
- `UnexpectedResponse`
- `Cancelled`
- `Unknown`
- XGT / FakePlc specific value가 없다.

확인 결과:

- Runtime core 안에는 `PollingSource` / `PollingOperation` / `PollingResultProducer` / `PollingCycleProducer` / `DriverAdapter` / `IChannelPollingSource` 유사 타입이 없다.
- Xgt side에는 read / write / session / polling worker 자산이 있지만 Runtime core에는 직접 연결되어 있지 않다.
- `CAAutomationHub.Runtime` project는 `CAAutomationHub.Contracts`만 참조한다.

## 5. 현재 비어 있는 구간

현재 Runtime에서 아직 구현되지 않은 구간은 다음이다.

    IPollingTargetProvider.GetTargetsAsync(...)
            -> IReadOnlyCollection<ChannelPollingTarget>
            -> target별 polling operation
            -> IReadOnlyCollection<ChannelPollingResult>
            -> PollingCycleCoordinator.PublishCycleAsync(...)

AH-RUNTIME-41의 목적은 이 구간의 책임을 누구에게 줄지 검토하는 것이다.

## 6. 후보 A: CycleCoordinator가 target provider와 source를 모두 호출

판정: 비권장

이 후보는 `PollingCycleCoordinator`가 target provider를 호출하고, target별 polling operation을 실행한 뒤, `ChannelPollingResult` batch를 만들어 publish까지 수행하는 구조다.

장점:

- cycle boundary 안에서 전체 흐름이 단순해 보인다.
- target fetch부터 publish까지 한 곳에서 관리할 수 있다.
- single-writer 정책을 중앙에서 잡을 수 있다.

위험:

- `PollingCycleCoordinator` 책임이 커진다.
- AH-RUNTIME-38에서 정의한 manual result batch publisher 역할을 넘어선다.
- target discovery / source execution / publish boundary가 한 클래스에 섞인다.
- 향후 timer scheduler까지 들어오면 god object가 될 위험이 크다.
- driver adapter coupling 가능성이 커진다.

결론:

- 현재 단계에서 부적절하다.
- `PollingCycleCoordinator`는 `ChannelPollingResult` batch publish boundary로 유지한다.

## 7. 후보 B: 별도 PollingCycleProducer / PollingResultProducer

판정: 권장 방향

후보 이름:

- `PollingResultProducer`
- `ChannelPollingResultProducer`
- `PollingTargetResultProducer`

판단:

- `PollingResultProducer` 또는 `ChannelPollingResultProducer`가 더 정확하다.
- `PollingCycleProducer`는 cycle time / scheduler 의미가 섞일 수 있다.

후보 흐름:

    IPollingTargetProvider
            -> PollingResultProducer
            -> target별 polling operation
            -> ChannelPollingResult batch
            -> PollingCycleCoordinator.PublishCycleAsync(...)

장점:

- target discovery와 result production 책임을 분리할 수 있다.
- `PollingCycleCoordinator`는 publish single-writer boundary로 유지된다.
- source / driver adapter를 producer 뒤에 붙이기 쉽다.
- fake source 테스트가 가능해진다.
- Scheduler는 나중에 producer와 coordinator를 조율하는 상위 레이어가 될 수 있다.

위험:

- 아직 source abstraction이 없으면 producer가 빈 껍데기가 될 수 있다.
- target별 operation model을 정해야 한다.
- producer와 source / adapter boundary를 더 나눌 필요가 생길 수 있다.

결론:

- AH-RUNTIME-42 후보로 적절하다.
- AH-RUNTIME-41에서는 production boundary 이름과 책임을 정리하고 skeleton은 미룬다.

## 8. 후보 C: IPollingSource / IChannelPollingSource abstraction

판정:

- 방향성은 맞지만 AH-RUNTIME-41에서 바로 만들기는 이르다.
- AH-RUNTIME-42 Boundary Review로 넘기는 것이 안전하다.

후보 interface:

    IChannelPollingSource
        PollAsync(ChannelPollingTarget target, CancellationToken token)
            -> ChannelPollingResult

또는 batch 형태:

    IChannelPollingSource
        PollAsync(IReadOnlyCollection<ChannelPollingTarget> targets, CancellationToken token)
            -> IReadOnlyCollection<ChannelPollingResult>

장점:

- target을 실제 result로 바꾸는 가장 직접적인 boundary다.
- `XgtDriverCore` / `FakePlc` adapter가 나중에 구현할 위치가 생긴다.
- producer / scheduler는 source interface만 알면 된다.
- 테스트 fake source를 만들기 쉽다.

위험:

- source abstraction을 지금 만들면 Driver Adapter 설계를 너무 빨리 고정할 수 있다.
- single target poll vs batch poll 정책을 정해야 한다.
- partial success / cancellation / exception mapping 정책이 필요하다.
- source가 `ChannelPollingFailureKind` mapping을 알아야 할 수 있다.

결론:

- AH-RUNTIME-42에서 Polling Source / Result Producer Boundary Review 대상으로 넘긴다.

## 9. 후보 D: 외부 ChannelPollingResult batch 유지

판정:

- 임시 유지 가능하지만 다음 Runtime 전환 단계에는 부족하다.

장점:

- 현재 구조를 유지한다.
- 추가 추상화가 없다.
- fake event 기반 테스트는 계속 쉽다.

위험:

- target provider가 생겼지만 사용처가 없다.
- "무엇을 polling할지"에서 "어떻게 result를 만들지"로 이어지지 않는다.
- 실제 Scheduler / Driver Adapter로 가기 어렵다.
- AH-RUNTIME-40의 target model이 고립될 수 있다.

결론:

- manual harness에는 유효하지만, 다음 단계에는 producer / source boundary가 필요하다.

## 10. 후보 E: Scheduler가 target provider와 source를 조율

판정:

- 장기 최종 구조로는 가능하지만 지금은 이르다.

후보 흐름:

    PollingScheduler
            -> IPollingTargetProvider
            -> IChannelPollingSource
            -> ChannelPollingResult batch
            -> PollingCycleCoordinator

장점:

- 실제 scheduler 최종 구조에 가깝다.
- timer / interval / lifecycle / cancellation policy를 상위 계층에서 조율할 수 있다.
- `PollingCycleCoordinator`는 single-writer publish boundary로 유지할 수 있다.

위험:

- 아직 실제 timer scheduler 단계가 아니다.
- Scheduler가 너무 많은 책임을 가질 수 있다.
- target provider / source / cycle coordinator를 묶는 상위 orchestrator가 필요할 수 있다.
- 지금은 AH-RUNTIME-41에서 결론만 내고 구현은 미루는 것이 안전하다.

결론:

- Scheduler는 장기적으로 producer와 coordinator를 호출하는 상위 조율자가 될 수 있다.
- AH-RUNTIME-42는 Scheduler보다 Source / Adapter boundary를 먼저 보는 편이 안전하다.

## 11. 권장안

권장 흐름:

    PollingScheduler 또는 manual caller
            -> PollingResultProducer
            -> IPollingTargetProvider.GetTargetsAsync(...)
            -> IChannelPollingSource 또는 driver adapter boundary
            -> IReadOnlyCollection<ChannelPollingResult>
            -> PollingCycleCoordinator.PublishCycleAsync(...)

핵심 책임:

`IPollingTargetProvider`

- target 목록만 제공한다.

`PollingResultProducer`

- target fetch를 수행한다.
- duplicate normalization을 수행한다.
- operation execution orchestration을 수행한다.
- result batch assembly를 수행한다.

`IChannelPollingSource`

- target 1개 또는 batch를 실제 polling한다.
- Runtime-neutral `ChannelPollingResult`로 변환한다.

`PollingCycleCoordinator`

- completed result batch를 single-writer로 publish한다.

## 12. target empty 정책

초기 권장:

- producer 단계에서 empty target을 명시적으로 empty result batch로 다룬다.
- `PublishCycleAsync(empty)` 호출 여부는 producer policy로 둔다.

판단:

- 현재 하위 path는 empty batch를 처리할 수 있고 refresh하지 않는다.
- 하지만 target이 0개면 실제 polling operation이 없으므로 기본은 no-op cycle이 더 자연스럽다.
- cycle-level telemetry가 필요해지면 `PublishCycleAsync(empty)` 호출 정책을 별도로 고정할 수 있다.

## 13. target fetch failure 정책

target fetch failure는 `ChannelPollingResult.Failure`가 아니다.

이유:

- 특정 PLC polling failure가 아니다.
- target 목록 자체를 얻지 못한 production / cycle-level failure다.
- `ChannelPollingResult`에 가짜 `PlcId`를 넣어 표현하면 PLC-level event 계약이 깨진다.

권장:

- 후속 skeleton이 필요하다면 `PollingResultProductionResult` 같은 wrapper가 필요하다.

## 14. operation failure -> ChannelPollingResult 정책

target이 존재하고 해당 target polling operation이 실패했다면 `ChannelPollingResult.Failure(...)`로 변환하는 것이 맞다.

예:

- timeout
- connection failure
- protocol failure
- unexpected response
- unknown failure

주의:

- XGT exception은 source / adapter 내부에서 숨긴다.
- raw frame classification은 source / adapter 내부에서 Runtime-neutral `ChannelPollingFailureKind`로 매핑한다.
- FakePlc scenario id는 Runtime result에 노출하지 않는다.

## 15. cancellation 정책

권장 정책:

target fetch 전 / 중 cancellation:

- production cancelled
- publish 호출 없음

publish 시작 전 cancellation:

- AH-RUNTIME-38처럼 cancelled / skipped cycle 처리 가능

publish 시작 후 cancellation:

- 현재 coordinator 정책대로 graceful completion

operation 중 cancellation:

- 기본은 PLC failure result가 아니라 production-level cancelled
- `ChannelPollingFailureKind.Cancelled`는 존재하지만 사용자 요청 / loop shutdown cancellation을 PLC 장애로 기록하는 것은 신중해야 함

partial results가 이미 있더라도 cancellation이 들어오면 publish할지 버릴지 AH-RUNTIME-42에서 명시 정책이 필요하다.

- 초기 안전안은 publish 전 cancellation이면 discard

## 16. duplicate target 정책

same PLC one result per cycle invariant는 producer에서 1차 보장해야 한다.

`PollingResultStateOrchestrator`의 duplicate `PlcId` skip은 방어 장치다.

권장:

- `PollingResultProducer`가 `PlcId` 기준 `StringComparer.Ordinal`로 duplicate target을 제거하거나 production-level warning / skip으로 남긴다.
- 같은 cycle에서 같은 `PlcId` result를 2개 만들지 않는 것이 primary invariant다.

## 17. 시간 의미 구분

`target configured time`

- target / source configuration이 만들어진 시각
- 현재 model에는 없음

`target fetch time`

- provider가 target list를 읽은 시각
- `OccurredAt` 아님

`operation start time`

- 개별 polling operation 시작 시각
- response time 계산용 후보

`operation occurred time`

- success / failure가 확정된 시각
- `ChannelPollingResult.OccurredAt`

`result production completed time`

- batch assembly 완료 시각
- 필요하면 production result metadata

`cycle publish start time`

- `PollingCyclePublishResult.CycleStartedAt`

`RuntimeSnapshot.CapturedAt`

- publish 후 snapshot frame time

중요:

- `OccurredAt`은 operation 결과 확정 시각이다.
- target fetch time이나 cycle publish time을 `OccurredAt`으로 사용하면 안 된다.
- `RuntimeSnapshot.CapturedAt`은 publish 후 snapshot frame time이다.

## 18. AH-RUNTIME-42 후보

권장 후보명:

- AH-RUNTIME-42 Polling Source / Result Producer Boundary Review

추가 파일 후보:

- `src/CAAutomationHub.Runtime/Polling/PollingResultProducer.cs`
- `src/CAAutomationHub.Runtime/Polling/PollingResultProductionResult.cs`
- `src/CAAutomationHub.Runtime/Polling/IChannelPollingSource.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingResultProducerTests.cs`

최소 skeleton 후보:

- producer가 target provider를 호출
- empty / duplicate / fetch failure / cancellation 정책 결과를 표현
- fake source test double로 success / failure / partial batch 검증
- 실제 XGT / FakePlc 연결 없음

예상 리스크:

- source interface를 너무 일찍 XGT shape로 고정
- producer와 scheduler 책임 혼합
- cancellation을 PLC failure로 오해
- duplicate 방어를 orchestrator에만 의존

## 19. 제외한 범위

이번 AH-RUNTIME-41에서는 다음을 하지 않았다.

- production code 수정
- test code 수정
- 문서 생성 외 작업
- interface 추가
- enum 추가
- scheduler / timer loop
- source / driver adapter skeleton
- `XgtDriverCore` 연결
- `FakePlc` 연결
- `XgtChannelRunner` 연결
- WPF 수정
- Contracts 수정
- Closeout 생성 외 작업
- commit

## 20. 실행한 명령

AH-RUNTIME-41 Boundary Review 당시 실행한 명령:

- `git status --short`
- `rg "ChannelPollingTarget|IPollingTargetProvider|RuntimeChannelPollingTargetProvider" src tests docs/harness`
- `rg "PollingCycleCoordinator|PublishCycleAsync|PollingCyclePublishResult" src tests`
- `rg "ChannelPollingResult|ChannelPollingFailureKind" src tests`
- `rg "PollingSource|PollingOperation|PollingResultProducer|PollingCycleProducer|DriverAdapter|Adapter" src tests docs/harness docs/context`
- `rg "XgtDriverCore|FakePlc|XgtChannelRunner" src/CAAutomationHub.Runtime docs/harness docs/context`
- `rg "CancellationToken|Cancelled|Timeout|Connection|Protocol|UnexpectedResponse|Unknown" src/CAAutomationHub.Runtime tests/CAAutomationHub.Runtime.Tests`
- `rg "OccurredAt|CapturedAt|CycleStartedAt|CycleCompletedAt" src tests docs/harness`
- `rg "PlcId|Address|DataType|Count|OutputKey" src/CAAutomationHub.Runtime tests/CAAutomationHub.Runtime.Tests docs/harness docs/context`
- 관련 source / test / docs `Get-Content`
- sibling `AutomationHub.XgtDriverCore`의 `IXgtSession`, `PollingWorker`, `MultiChannelPollingCoordinator`, `PollingCycleResult` read-only 확인

테스트 / 빌드는 이번 지시가 조사 전용이라 실행하지 않았다.

AH-RUNTIME-41 Closeout 문서 작성 후 실행한 검증:

- `git diff -- docs/harness/AH-RUNTIME-41.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-41.md`

## 21. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-41 Boundary Review 결과를 closeout 문서로 기록했다.
- 현재 Runtime에 `PollingSource` / `PollingOperation` / `PollingResultProducer` / `DriverAdapter` 계열 타입이 없음을 기록했다.
- `IPollingTargetProvider`와 `PollingCycleCoordinator` 사이의 target-to-result production 구간이 아직 비어 있음을 기록했다.
- `PollingCycleCoordinator`를 확장하지 않고 `ChannelPollingResult` batch publish single-writer boundary로 유지해야 함을 기록했다.
- target discovery, operation execution, result assembly는 별도 producer 계열 service가 담당하는 것이 안전하다는 결론을 기록했다.
- 후보 A / B / C / D / E 검토와 권장안을 기록했다.
- target empty, target fetch failure, operation failure, cancellation, duplicate target 정책을 기록했다.
- `OccurredAt` / `CapturedAt` / cycle publish time / operation time 분리 의미를 기록했다.
- AH-RUNTIME-42 Polling Source / Result Producer Boundary Review 후보와 제외 범위, 리스크를 기록했다.
- `ContextPublisher` 자동 publish 미사용 정책을 유지했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
