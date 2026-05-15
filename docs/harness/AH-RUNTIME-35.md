# AH-RUNTIME-35 Closeout

## 1. Summary

AH-RUNTIME-35는 batch `ChannelPollingResult` 처리 boundary를 검토하고, 그 결론에 따라 Runtime 내부 Polling orchestration skeleton을 추가한 단계다.

이번 단계에서 확인하고 구현한 핵심 흐름은 다음과 같다.

    여러 ChannelPollingResult
            ↓
    PollingResultStateOrchestrator.PublishBatchAsync(...)
            ↓
    item별 registry lookup / writable check
            ↓
    item별 previous RuntimePlcChannelState readback
            ↓
    item별 RuntimePlcChannelStateMapper.Map(...)
            ↓
    PollingChannelUpdate batch 생성
            ↓
    PollingPublishCoordinator.PublishAsync(batch)
            ↓
    snapshot refresh 1회

`PollingPublishCoordinator`는 계속 `PollingChannelUpdate` batch publish 조율자로 유지했고, `ChannelPollingResult` 해석 책임은 `PollingResultStateOrchestrator`에 남겼다.

이번 작업은 실제 `PollingScheduler`, `XgtDriverCore`, `FakePlc`, Real PLC, WPF 연결이 아니다. `ContextPublisher` 자동 publish도 재도입하지 않았다. Runtime 작업 기록은 `docs/harness/AH-RUNTIME-xx.md` Closeout 문서를 primary historical record로 사용한다.

## 2. Goal

AH-RUNTIME-35의 목표는 단일 `ChannelPollingResult` 처리에서 여러 `ChannelPollingResult`를 한 번의 orchestration batch로 처리할 수 있는 Runtime 내부 boundary를 확정하고 skeleton으로 고정하는 것이다.

구체적인 목표는 다음과 같았다.

- 여러 `ChannelPollingResult`를 한 번에 처리할 수 있게 한다.
- 성공적으로 변환된 item만 `PollingChannelUpdate`로 만들어 `PollingPublishCoordinator`에 batch로 전달한다.
- `PollingPublishCoordinator`는 `PollingChannelUpdate` batch publish 조율자 역할만 유지한다.
- `PollingPublishCoordinator`에 `ChannelPollingResult` 해석 책임을 넣지 않는다.
- snapshot refresh는 성공 update가 1개 이상일 때 batch당 1회만 발생하도록 기존 coordinator 흐름을 활용한다.
- missing / non-writable / duplicate `PlcId`는 item-level failure 또는 skipped result로 남긴다.
- partial success를 허용한다.
- `OccurredAt` / `CapturedAt` 의미 분리를 유지한다.

## 3. Scope 보정

초기 skeleton 구현 보고에서 batch `ChannelPollingResult` skeleton 구현이 AH-RUNTIME-36으로 표현된 부분이 있었다.

하지만 사용자 판단에 따라 batch skeleton은 AH-RUNTIME-35 Boundary Review의 구현 단계로 통합한다.

따라서 본 Closeout은 AH-RUNTIME-35 Batch Boundary Review와 Batch Skeleton 구현 결과를 함께 기록한다.

AH-RUNTIME-36 번호는 다음 별도 주제를 위해 보존한다.

## 4. Background

AH-RUNTIME-34까지 Runtime 내부 publish path는 in-memory end-to-end 테스트로 검증되었다.

    ChannelPollingResult
            ↓
    PollingResultStateOrchestrator
            ↓
    PollingPublishCoordinator
            ↓
    InMemoryRuntimePlcChannel
            ↓
    InMemoryAutomationHubSupervisor.RefreshSnapshotAsync
            ↓
    RuntimeSnapshot / SnapshotChanged

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

AH-RUNTIME-35는 이 흐름을 여러 polling result가 같은 cycle에 들어오는 경우로 확장할 때 어떤 boundary가 안전한지 검토하고, 최소 skeleton으로 고정했다.

## 5. Boundary Review 결론

AH-RUNTIME-35 Boundary Review의 권장안은 `PollingResultStateOrchestrator`에 batch API를 추가하는 것이었다.

검토 결론은 다음과 같다.

- batch `ChannelPollingResult` 처리는 `PollingResultStateOrchestrator`에 추가하는 것이 가장 안전하다.
- `PollingPublishCoordinator`는 계속 `PollingChannelUpdate` batch publish 조율자로 유지한다.
- `PollingPublishCoordinator`에 `ChannelPollingResult` 해석 책임을 넣지 않는다.
- 단일 result 반복 호출은 구현은 쉽지만, snapshot refresh가 result 수만큼 발생할 수 있어 polling cycle 의미를 잃을 수 있다.
- 별도 Batch Orchestrator는 아직 `PollingScheduler` / polling cycle model이 없어서 과한 추상화다.
- Batch Update Builder는 helper로는 유용할 수 있지만, registry lookup / writable check / publish 위임 책임을 해결하지 못하므로 1차 skeleton 후보로는 부족하다.
- duplicate `PlcId`는 초기 skeleton에서 허용하지 않는 것이 안전하다.
- partial success는 허용하는 방향이 현재 `PollingPublishCoordinator` semantics와 잘 맞는다.

이 결론에 따라 구현은 `PollingResultStateOrchestrator.PublishBatchAsync(...)` 중심으로 진행했고, `PollingPublishCoordinator`의 책임은 변경하지 않았다.

## 6. 구현 결과

### 6.1 변경 파일

- `src/CAAutomationHub.Runtime/Polling/PollingResultStateOrchestrator.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingResultStateOrchestratorBatchTests.cs`

### 6.2 추가 / 수정 타입

`PollingResultStateOrchestrator`

- `PublishBatchAsync(IReadOnlyCollection<ChannelPollingResult>, CancellationToken)` 추가
- 기존 single `PublishAsync(ChannelPollingResult, CancellationToken)`는 batch path를 사용하도록 정리
- item별 lookup, writable check, readback, mapping, update 생성 책임 유지
- `PollingPublishCoordinator.PublishAsync(...)`에는 `PollingChannelUpdate` batch만 전달

`PollingResultStateOrchestrationBatchResult`

- batch orchestration 결과를 표현하는 Runtime 내부 result 타입
- `Contracts` DTO가 아니며 WPF 표시용 DTO도 아니다.
- XGT / FakePlc specific failure를 포함하지 않는다.

`PollingResultStateOrchestrationResult`

- duplicate skipped 표현을 위해 `Skipped` 속성 추가
- 기존 `PlcId`, `Succeeded`, `ErrorMessage`, `PublishResult` 의미는 유지

### 6.3 Batch API 흐름

`PublishBatchAsync`는 다음 흐름으로 동작한다.

1. 여러 `ChannelPollingResult`를 입력으로 받는다.
2. `results`가 empty이면 coordinator를 호출하지 않고 update 0개 결과를 반환한다.
3. item별로 `PlcId` duplicate 여부를 확인한다.
4. `RuntimeChannelRegistry.TryGetChannel`으로 channel을 lookup한다.
5. channel이 없으면 item failure로 기록한다.
6. channel이 `IWritableRuntimePlcChannel`이 아니면 item failure로 기록한다.
7. `writable.GetRuntimeState()`로 previous `RuntimePlcChannelState`를 읽는다.
8. `RuntimePlcChannelStateMapper.Map(previous, result)`를 호출한다.
9. 성공적으로 변환된 item만 `PollingChannelUpdate`로 만든다.
10. `PollingChannelUpdate` batch를 `PollingPublishCoordinator.PublishAsync(...)`에 1회 전달한다.
11. 성공 update가 0개이면 coordinator를 호출하지 않는다.
12. coordinator가 호출되는 경우 snapshot refresh는 기존 coordinator 흐름에 의해 batch당 1회 발생한다.

중요한 경계는 다음과 같다.

- `PollingPublishCoordinator`는 `ChannelPollingResult`를 받지 않는다.
- `PollingPublishCoordinator`는 `RuntimePlcChannelStateMapper`를 호출하지 않는다.
- `PollingPublishCoordinator`는 previous state readback을 하지 않는다.
- `PollingPublishCoordinator`는 `PollingChannelUpdate` batch publish만 담당한다.

## 7. Batch result 타입

`PollingResultStateOrchestrationBatchResult` 필드는 다음과 같다.

- `Succeeded`
- `TotalCount`
- `SucceededCount`
- `FailedCount`
- `SkippedCount`
- `UpdatesCount`
- `PublishResult`
- `ItemResults`

각 필드의 의미는 다음과 같다.

- `TotalCount`: 입력 `ChannelPollingResult` 개수
- `SucceededCount`: item-level 성공 개수
- `FailedCount`: missing / non-writable / duplicate 등 실패 개수
- `SkippedCount`: duplicate 등 정책상 publish 대상에서 제외된 개수
- `UpdatesCount`: `PollingPublishCoordinator`에 전달한 `PollingChannelUpdate` 개수
- `PublishResult`: coordinator publish 결과. 성공 update가 없으면 null 가능
- `ItemResults`: item별 orchestration 결과
- `Succeeded`: 전체 batch 성공 여부. partial success는 false일 수 있음

주의 사항은 다음과 같다.

- 이 타입은 Runtime 내부 result 타입이다.
- `Contracts` DTO가 아니다.
- WPF 표시용 DTO가 아니다.
- XGT / FakePlc specific failure를 포함하지 않는다.

## 8. duplicate PlcId 처리 정책

초기 skeleton에서는 duplicate `PlcId`를 허용하지 않는다.

정책은 다음과 같다.

- batch 안에서 같은 `PlcId`가 두 번째 이상 등장하면 duplicate item은 `Skipped == true` item result로 남긴다.
- duplicate item은 `PollingChannelUpdate`로 만들지 않는다.
- 첫 번째 `PlcId` item만 처리한다.
- last-wins 정책은 사용하지 않는다.
- same-batch sequential mapping은 구현하지 않는다.
- temporary state map은 구현하지 않는다.

이유는 다음과 같다.

- 같은 batch 안에서 순차 mapping을 하려면 publish 전 temporary state map이 필요하다.
- last-wins는 `ConsecutiveFailures` 같은 누적값을 왜곡할 수 있다.
- scheduler cycle 기준으로는 PLC당 result 1개가 자연스럽다.

## 9. partial success 정책

partial success를 허용한다.

정책은 다음과 같다.

- 변환 가능한 item만 `PollingChannelUpdate`로 만들어 coordinator에 전달한다.
- missing / non-writable / duplicate item은 `ItemResults`에 failure 또는 skipped로 기록한다.
- 성공 update가 1개 이상이면 coordinator를 1회 호출한다.
- 성공 update가 0개이면 coordinator를 호출하지 않는다.
- 성공 update가 1개 이상이면 snapshot refresh는 coordinator 내부에서 1회만 발생한다.
- partial batch는 `Succeeded == false`이면서 `PublishResult.PublishSucceeded == true`일 수 있다.

의미는 다음과 같다.

- batch 전체가 완전 성공하지 않아도 성공 가능한 update는 publish할 수 있다.
- 하지만 item failure가 있으면 batch result의 전체 성공 여부는 false로 표현될 수 있다.

## 10. OccurredAt / CapturedAt 분리

Batch 처리에서도 시간 의미 분리를 유지했다.

- 각 `ChannelPollingResult.OccurredAt`은 개별 polling event 발생 시각이다.
- 각 channel의 `LastSuccessAt` / `LastFailureAt`은 각 result의 `OccurredAt`을 사용한다.
- `RuntimeSnapshot.CapturedAt`은 batch publish 후 snapshot frame 수집 시각이다.
- `SnapshotChanged.OccurredAt`은 `snapshot.CapturedAt`과 일치한다.
- 여러 result가 서로 다른 `OccurredAt`을 가질 수 있다.
- `RuntimeSnapshot.CapturedAt`을 `result.OccurredAt`으로 덮어쓰지 않는다.

테스트에서는 서로 다른 `occurredAt` 값을 가진 여러 result를 사용해 각 channel state가 자신의 `occurredAt`을 유지하는지 검증했다.

## 11. 동시성 리스크

AH-RUNTIME-35에서도 기존 동시성 리스크는 해결하지 않았다.

현재 리스크는 다음 흐름에 있다.

    GetRuntimeState()
            ↓
    RuntimePlcChannelStateMapper.Map(...)
            ↓
    PollingPublishCoordinator.PublishAsync(...)
            ↓
    ReplaceState(...)

batch에서는 다음 리스크가 추가된다.

- 여러 channel readback 후 publish 전에 state가 바뀔 수 있다.
- 같은 `plcId`가 중복될 경우 previous state 기준이 모호해질 수 있다.
- 여러 batch가 동시에 들어올 경우 lost update 가능성이 증가한다.
- batch 전체가 원자적 transaction은 아니다.

이번 작업에서 하지 않은 것은 다음과 같다.

- `RuntimeChannelRegistry` lock 확장
- channel lock 확장
- `IWritableRuntimePlcChannel` atomic update API 추가
- concurrent stress test 추가
- batch transaction semantics 도입

동시성 문제는 별도 Boundary Review 후보로 남긴다.

## 12. 테스트 및 검증 결과

Batch RED:

- 최초 실행 시 `PublishBatchAsync` / batch result 타입 없음으로 compile failure 확인

Focused tests:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter PollingResultStateOrchestratorBatchTests`
  - 통과: 7
  - 실패: 0

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter PollingResultStateOrchestratorTests`
  - 통과: 8
  - 실패: 0

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter PollingResultRuntimeSnapshotEndToEndTests`
  - 통과: 2
  - 실패: 0

Runtime 전체 tests:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
  - 통과: 116
  - 실패: 0

Build:

- `dotnet build CAAutomationHub.sln`
  - 성공
  - 경고 0
  - 오류 0

`git diff --check`:

- exit 0
- whitespace error 없음
- 단, modified orchestrator file의 LF가 다음 Git touch 시 CRLF로 대체될 수 있다는 Git line-ending warning이 있었음

검증 의미:

- batch `ChannelPollingResult`가 한 번의 orchestration batch로 처리됨
- 성공 update만 `PollingPublishCoordinator`에 전달됨
- 여러 writable result가 한 번의 snapshot refresh로 묶임
- empty batch와 update 0개 batch는 coordinator를 호출하지 않음
- duplicate `PlcId`는 second item을 skipped로 남기고 publish하지 않음
- 기존 single `PublishAsync(...)` behavior는 유지됨
- `OccurredAt` / `CapturedAt` 분리는 유지됨

## 13. 제외한 범위

이번 작업에서 제외한 범위는 다음과 같다.

- `Contracts` DTO 수정
- WPF 수정
- `XgtDriverCore` 연결
- `FakePlc` 연결
- `XgtChannelRunner` 연결
- Real PLC 연결
- `PollingScheduler` timer / loop 구현
- reconnect 정책 구현
- `ContextPublisher` 자동 publish 재도입
- batch transaction semantics
- atomic update API
- concurrent stress test
- commit

## 14. 변경 파일 목록

AH-RUNTIME-35 batch skeleton 구현 변경 파일:

- `src/CAAutomationHub.Runtime/Polling/PollingResultStateOrchestrator.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingResultStateOrchestratorBatchTests.cs`

AH-RUNTIME-35 closeout 문서:

- `docs/harness/AH-RUNTIME-35.md`

## 15. 다음 단계 후보

후보 1:

- AH-RUNTIME-35 commit 전 최종 검증
- 변경 파일 확인
- Runtime tests / build / `git diff --check` 재확인
- working tree 상태 확인

후보 2:

- AH-RUNTIME-36: `PollingScheduler` Boundary Review
- timer loop / interval / cancellation / batch cycle 검토
- 단, 실제 scheduler 구현은 이후 단계로 분리

후보 3:

- AH-RUNTIME-36 또는 별도 번호: 동시성 / lost update 리스크 Boundary Review
- readback → map → publish 구간의 원자성 문제 검토
- atomic update API 필요 여부 판단
- concurrent stress test는 별도 단계

후보 4:

- `XgtDriverCore` / `FakePlc` adapter Boundary Review
- 실제 driver event를 `ChannelPollingResult`로 변환하는 위치 검토
- 단, AH-RUNTIME-35 Closeout 단계에서는 연결하지 않음

## 16. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-35 Batch Boundary Review 결론을 기록함
- AH-RUNTIME-35 범위 보정 내용을 명시함
- batch skeleton 구현 결과를 AH-RUNTIME-35 historical record로 통합함
- `PollingResultStateOrchestrator` / `PollingPublishCoordinator` 책임 경계를 기록함
- duplicate `PlcId` 처리 정책을 기록함
- partial success 정책을 기록함
- `OccurredAt` / `CapturedAt` 분리 의미를 기록함
- 동시성 리스크와 제외 범위를 기록함
- 테스트, 빌드, `git diff --check` 검증 결과를 기록함
- `ContextPublisher` 자동 publish 미사용 정책을 유지함

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
