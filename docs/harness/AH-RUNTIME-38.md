# AH-RUNTIME-38 Closeout

## 1. Summary

AH-RUNTIME-38은 기존 Runtime polling publish path 앞단에 manual cycle boundary skeleton을 추가한 단계다.

이번 작업에서 추가한 핵심 타입은 `PollingCycleCoordinator`다. 이 타입은 외부에서 받은 `IReadOnlyCollection<ChannelPollingResult>` batch를 cycle 단위로 받아, Runtime publish path에 동시에 여러 `PublishBatchAsync(...)` 호출이 들어가지 않도록 보호한다.

AH-RUNTIME-38은 실제 timer 기반 `PollingScheduler` 구현이 아니다. `PeriodicTimer`, `HostedService`, polling target discovery, XGT, FakePlc, real PLC, WPF 연결은 추가하지 않았다.

핵심 의미는 다음과 같다.

- Runtime publish path 앞단에 manual cycle boundary를 세움
- cycle publish 중 overlap이 발생하면 wait / queue 없이 skip 처리
- accepted cycle만 `IPollingResultBatchPublisher.PublishBatchAsync(...)`로 전달
- Runtime state mapping / publish / snapshot refresh 세부 책임은 기존 orchestration path에 유지
- publish 시작 전 cancellation은 discard / cancelled result로 고정
- publish 시작 후에는 graceful completion 정책을 적용

## 2. Goal

AH-RUNTIME-38의 목표는 실제 scheduler loop가 도입되기 전에 Runtime publish path의 single-writer invariant를 manual cycle boundary에서 먼저 고정하는 것이다.

목표 흐름은 다음이다.

    IReadOnlyCollection<ChannelPollingResult>
            ↓
    PollingCycleCoordinator.PublishCycleAsync(...)
            ↓
    in-flight cycle guard
            ↓
    overlap이면 skip
            ↓
    IPollingResultBatchPublisher.PublishBatchAsync(...)
            ↓
    PollingResultStateOrchestrationBatchResult

이번 단계에서 보장하려는 핵심 계약은 다음이다.

- `PublishCycleAsync(...)` 실행 중 다른 `PublishCycleAsync(...)`는 실제 publish를 시작하지 못함
- overlap 발생 시 queue에 넣지 않고 skip 결과 반환
- `PollingCycleCoordinator`는 `PollingPublishCoordinator`, mapper, `ReplaceState`, `RefreshSnapshotAsync`를 직접 호출하지 않음
- `PollingCycleCoordinator`는 Runtime publish path의 안전한 entry point인 batch publisher boundary만 호출함

## 3. Background

AH-RUNTIME-36에서는 다음 구간이 하나의 원자적 update가 아님을 확인했다.

    GetRuntimeState()
            ↓
    RuntimePlcChannelStateMapper.Map(...)
            ↓
    PollingPublishCoordinator.PublishAsync(...)
            ↓
    IWritableRuntimePlcChannel.ReplaceState(...)

각 channel method는 method 단위 lock 보호를 받지만, readback → map → publish → replace 전체 구간은 atomic transaction이 아니다. 따라서 같은 Runtime publish path에 concurrent publish가 들어오면 stale previous state 기반 lost update가 발생할 수 있다.

AH-RUNTIME-37에서는 이 리스크를 Runtime API 자체의 atomic multi-writer 확장으로 바로 해결하지 않고, scheduler / cycle boundary에서 single-writer invariant를 보장하는 방향으로 정리했다.

AH-RUNTIME-38은 그 결론에 따라 실제 timer scheduler보다 작은 단위인 manual `PollingCycleCoordinator` skeleton을 먼저 구현했다.

## 4. 구현 결과

### 4.1 변경 파일

- `src/CAAutomationHub.Runtime/Polling/PollingCycleCoordinator.cs`
- `src/CAAutomationHub.Runtime/Polling/PollingCyclePublishResult.cs`
- `src/CAAutomationHub.Runtime/Polling/IPollingResultBatchPublisher.cs`
- `src/CAAutomationHub.Runtime/Polling/PollingResultStateOrchestrator.cs`
- `src/CAAutomationHub.Runtime/Properties/AssemblyInfo.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingCycleCoordinatorTests.cs`
- `docs/harness/AH-RUNTIME-38.md`

### 4.2 추가 타입

`PollingCycleCoordinator`

- Runtime publish path 앞단의 manual cycle boundary
- 외부 batch 입력을 받아 in-flight guard를 통과한 경우에만 publisher에 전달
- overlap이면 skip result 반환
- publish 시작 전 cancellation이면 cancelled result 반환

`PollingCyclePublishResult`

- cycle publish 결과를 표현하는 Runtime 내부 polling result 타입
- publish 성공, overlap skip, before-start cancellation, cycle start / complete time, requested count, batch result를 함께 기록

`IPollingResultBatchPublisher`

- Runtime 내부 `Polling` namespace에 추가한 최소 publisher abstraction
- deterministic overlap / cancellation 테스트를 가능하게 하기 위한 내부 boundary
- `PublishBatchAsync(IReadOnlyCollection<ChannelPollingResult>, CancellationToken)` 하나만 가진다

### 4.3 수정 타입

`PollingResultStateOrchestrator`

- `IPollingResultBatchPublisher`를 구현하도록 수정했다.
- 기존 `PublishBatchAsync(...)` 경로와 동작은 유지했다.
- Runtime state mapping / publish / snapshot refresh orchestration 책임은 기존 위치에 남겨 두었다.

`AssemblyInfo.cs`

- `CAAutomationHub.Runtime.Tests`가 internal publisher boundary를 사용할 수 있도록 `InternalsVisibleTo`를 추가했다.
- 이 변경은 테스트 가능성을 위한 것이며 external Contracts API 확장이 아니다.

## 5. PollingCycleCoordinator 책임

`PollingCycleCoordinator`는 Runtime publish path 앞단의 manual cycle boundary다.

책임은 다음으로 제한한다.

1. 외부에서 `ChannelPollingResult` batch를 받는다.
2. cycle publish가 이미 진행 중인지 확인한다.
3. 이미 in-flight cycle이 있으면 새 cycle은 skip 결과로 반환한다.
4. publish 시작 전 cancellation이 요청되어 있으면 discard / cancelled 결과로 반환한다.
5. in-flight가 아니면 cycle publish를 시작한다.
6. `IPollingResultBatchPublisher.PublishBatchAsync(...)`를 호출한다.
7. 결과를 `PollingCyclePublishResult`로 감싸 반환한다.
8. `finally`에서 in-flight 상태를 해제한다.

`PollingCycleCoordinator`가 하지 않는 것은 다음이다.

- timer loop 생성
- `PeriodicTimer` 사용
- `HostedService` 구현
- polling target discovery
- driver polling 실행
- `XgtDriverCore` 호출
- `FakePlc` 호출
- `PollingPublishCoordinator` 직접 호출
- `RuntimePlcChannelStateMapper` 직접 호출
- `IWritableRuntimePlcChannel.ReplaceState(...)` 직접 호출
- `InMemoryAutomationHubSupervisor.RefreshSnapshotAsync(...)` 직접 호출
- `SnapshotChanged` 직접 발생
- WPF 업데이트

## 6. PollingCyclePublishResult 요약

`PollingCyclePublishResult`는 manual cycle publish 결과를 표현한다.

필드 의미는 다음과 같다.

- `Succeeded`
  - cycle publish가 실행되고 batch publish도 성공한 경우 `true`

- `Skipped`
  - overlap 등으로 cycle이 실행되지 않은 경우 `true`

- `Cancelled`
  - publish 시작 전 cancellation으로 discard된 경우 `true`

- `SkipReason`
  - overlap 또는 cancellation 이유

- `CycleStartedAt`
  - cycle boundary에 들어온 시각

- `CycleCompletedAt`
  - cycle publish가 완료된 시각
  - skip / cancel result에서는 `null`

- `RequestedCount`
  - 입력 `ChannelPollingResult` 개수

- `BatchResult`
  - 실제 `PublishBatchAsync(...)` 결과
  - skip / cancel이면 `null`

## 7. single-writer / overlap skip 정책

AH-RUNTIME-38의 핵심 정책은 다음이다.

- `PublishCycleAsync(...)` 실행 중 다른 `PublishCycleAsync(...)`는 실제 publish를 시작하지 못한다.
- overlap 발생 시 wait하지 않는다.
- overlap 발생 시 queue에 넣지 않는다.
- overlap 발생 시 skip 결과를 반환한다.
- cycle N publish 완료 전 cycle N+1은 시작하지 않는다.
- 이로써 Runtime publish path에 동시 `PublishBatchAsync(...)` 호출이 들어가지 않도록 보호한다.

구현 방식:

- `Interlocked.CompareExchange` 기반 non-blocking in-flight guard 사용
- overlap 시 `Skipped == true` 결과 반환
- fake / test publisher 호출 횟수로 overlap skip 검증

이 정책은 queue 방식이 stale polling result를 만들 수 있다는 AH-RUNTIME-37 판단을 반영한다.

## 8. cancellation 정책

AH-RUNTIME-38 cancellation 정책은 다음이다.

- publish 시작 전 cancellation이 요청되면 discard / cancelled result를 반환한다.
- 이 경우 `PublishBatchAsync(...)`를 호출하지 않는다.
- publish가 시작된 뒤에는 `CancellationToken.None`으로 batch publisher를 호출한다.
- 이유는 일부 `ReplaceState(...)` 후 refresh 전에 cancellation이 발생해 snapshot refresh가 누락되는 상황을 피하기 위함이다.
- force cancellation은 AH-RUNTIME-38 범위에서 제외했다.

이 정책은 "publish started 이후 graceful completion" 정책이다.

현재 `PollingResultStateOrchestrator.PublishBatchAsync(...)`와 `PollingPublishCoordinator.PublishAsync(...)`는 cancellation token을 받을 수 있다. 하지만 publish 시작 후 동일 token을 그대로 넘기면 일부 channel state가 교체된 뒤 `RefreshSnapshotAsync(...)` 전에 cancellation될 수 있다. AH-RUNTIME-38에서는 이 리스크를 피하기 위해 accepted cycle publish에는 non-cancelled token을 전달한다.

## 9. 의존성 / 테스트 가능성 판단

concrete `PollingResultStateOrchestrator`만 의존하면 overlap / cancellation 테스트를 deterministic하게 작성하기 어렵다.

이유:

- 기존 `PollingResultStateOrchestrator`는 빠르게 완료된다.
- overlap 상태를 안정적으로 재현하기 어렵다.
- `PollingResultStateOrchestrator`는 sealed class다.

따라서 Runtime 내부 `Polling` namespace에 최소 interface를 추가했다.

추가한 interface:

- `IPollingResultBatchPublisher`

역할:

    PublishBatchAsync(IReadOnlyCollection<ChannelPollingResult>, CancellationToken)

판단:

- 이 interface는 Runtime 내부 테스트 가능성을 위한 최소 abstraction이다.
- Contracts DTO가 아니다.
- WPF API가 아니다.
- 외부 public API로 확장하지 않았다.
- `PublishBatchAsync(...)` 하나만 갖는 최소 interface다.
- production public constructor는 여전히 `PollingResultStateOrchestrator`를 받는다.

## 10. OccurredAt / CapturedAt / cycle time 분리

시간 의미는 계속 분리했다.

- `ChannelPollingResult.OccurredAt`
  - 개별 polling operation 성공 / 실패 시각

- `RuntimeSnapshot.CapturedAt`
  - refresh 후 snapshot frame 수집 시각

- `CycleStartedAt`
  - `PollingCycleCoordinator`가 cycle publish 요청을 받은 시각

- `CycleCompletedAt`
  - cycle publish가 완료된 시각

중요:

- `CycleStartedAt` / `CycleCompletedAt`은 `LastSuccessAt` / `LastFailureAt`에 사용하지 않는다.
- `RuntimeSnapshot.CapturedAt`을 `ChannelPollingResult.OccurredAt`으로 덮어쓰지 않는다.
- cycle time과 event time과 snapshot time은 서로 다른 의미다.

## 11. 테스트 및 검증 결과

RED:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter PollingCycleCoordinatorTests`
- 최초 실행 시 `IPollingResultBatchPublisher` 미존재로 실패 확인

GREEN:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter PollingCycleCoordinatorTests`
  - Passed 4 / 4

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter PollingResultStateOrchestratorBatchTests`
  - Passed 7 / 7

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
  - Passed 120 / 120

- `dotnet build CAAutomationHub.sln`
  - 성공
  - warnings 0
  - errors 0

- `git diff --check`
  - whitespace error 없음
  - touched orchestrator file에 LF → CRLF warning만 있음

## 12. 제외한 범위

이번 AH-RUNTIME-38에서는 다음을 하지 않았다.

- timer loop
- `PeriodicTimer`
- `HostedService`
- polling target discovery
- `IPollingOperation` / PollingSource 본격 구현
- `XgtDriverCore` 연결
- `FakePlc` 연결
- Real PLC 연결
- WPF 수정
- Contracts DTO 수정
- `PollingPublishCoordinator` 책임 확장
- `RuntimePlcChannelStateMapper` 수정
- `IWritableRuntimePlcChannel` atomic update API 추가
- optimistic concurrency version 추가
- ContextPublisher 자동 publish 재도입
- commit

## 13. 변경 파일 목록

AH-RUNTIME-38 구현 변경 파일:

- `src/CAAutomationHub.Runtime/Polling/PollingCycleCoordinator.cs`
- `src/CAAutomationHub.Runtime/Polling/PollingCyclePublishResult.cs`
- `src/CAAutomationHub.Runtime/Polling/IPollingResultBatchPublisher.cs`
- `src/CAAutomationHub.Runtime/Polling/PollingResultStateOrchestrator.cs`
- `src/CAAutomationHub.Runtime/Properties/AssemblyInfo.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingCycleCoordinatorTests.cs`
- `docs/harness/AH-RUNTIME-38.md`

## 14. 다음 단계 후보

후보 1: AH-RUNTIME-38 commit 전 최종 검증

- 변경 파일 확인
- Runtime tests / build / `git diff --check` 재확인
- working tree 상태 확인

후보 2: AH-RUNTIME-39 PollingSource / Driver Adapter Boundary Review

- `ChannelPollingResult`를 실제 polling source가 어떻게 생성할지 검토
- 아직 `XgtDriverCore` / `FakePlc` 직접 연결은 금지

후보 3: AH-RUNTIME-39 Polling Target Model Boundary Review

- PLC별 polling target 목록 검토
- cycle input 모델 검토
- address-level detail 분리 검토

후보 4: 실제 timer scheduler Boundary Review

- `PeriodicTimer` / HostedService-style async loop 검토
- 단, `PollingCycleCoordinator` 위에 얹는 방식으로 검토

## 15. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-38 Closeout을 한글 중심 문서로 재작성했다.
- `PollingCycleCoordinator` / `PollingCyclePublishResult` / `IPollingResultBatchPublisher` 구현 의미를 기록했다.
- single-writer / overlap skip / cancellation-before-start discard / after-start graceful completion 정책을 기록했다.
- `OccurredAt` / `CapturedAt` / cycle time 분리 의미를 기록했다.
- timer scheduler, XGT, FakePlc, WPF, Contracts, ContextPublisher가 제외 범위였음을 기록했다.
- RED / GREEN / build / `git diff --check` 검증 결과를 기록했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
