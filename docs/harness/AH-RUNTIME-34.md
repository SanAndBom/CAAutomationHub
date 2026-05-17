# AH-RUNTIME-34 Closeout

## 1. Summary

AH-RUNTIME-34는 Fake polling event 기반 end-to-end Runtime publish test를 추가한 단계다.

이번 단계에서 실제 `PollingScheduler`, `XgtDriverCore`, `FakePlc`, Real PLC, WPF 연결 없이 in-memory 구성만으로 `ChannelPollingResult`가 `RuntimeSnapshot`까지 반영되는지 검증했다.

검증한 Runtime 내부 흐름:

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
    RuntimeSnapshot

이번 작업은 production code 수정 없이 테스트만 추가했다. `ContextPublisher` 자동 publish는 재도입하지 않았고, Runtime 작업 기록은 이 Closeout 문서를 primary historical record로 남긴다.

## 2. Goal

목표는 in-memory 구성만으로 fake polling event가 Runtime 내부 publish path를 타고 `RuntimeSnapshot`에 반영되는지 end-to-end로 검증하는 것이다.

포함된 목표:

- success `ChannelPollingResult`가 `RuntimeSnapshot`에 반영되는지 검증
- failure `ChannelPollingResult`가 `RuntimeSnapshot`에 반영되는지 검증
- `LastSuccessAt` / `LastFailureAt`이 `ChannelPollingResult.OccurredAt` 기준으로 반영되는지 검증
- `RuntimeSnapshot.CapturedAt`이 snapshot frame time으로 유지되는지 검증
- `OccurredAt`과 `CapturedAt`이 섞이지 않는지 검증
- `SnapshotChanged`가 `RefreshSnapshotAsync` 흐름에서 발생하는지 검증
- `PollingResultStateOrchestrator`가 snapshot refresh를 직접 수행하지 않고 `PollingPublishCoordinator`를 통해 publish path를 타는 기존 구조를 유지하는지 확인

## 3. Background

AH-RUNTIME-31에서는 `ChannelPollingResult`를 `RuntimePlcChannelState`로 바꾸는 mapper boundary를 추가했다.

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

기존 publish / snapshot 흐름은 다음과 같다.

    PollingPublishCoordinator
            ↓
    IWritableRuntimePlcChannel.ReplaceState
            ↓
    refreshSnapshotAsync
            ↓
    RuntimeSnapshot / SnapshotChanged

AH-RUNTIME-34는 위 경계들이 실제 in-memory 조립 상태에서 end-to-end로 닫히는지 확인했다.

## 4. 구현 결과

### 4.1 추가 파일

- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingResultRuntimeSnapshotEndToEndTests.cs`

### 4.2 추가 테스트

- `PublishSuccess_UpdatesRuntimeSnapshotWithOccurredAt`
- `PublishFailure_UpdatesRuntimeSnapshotWithFailureOccurredAt`

### 4.3 Runtime 내부 흐름

테스트에서 사용한 in-memory 구성:

    registry = new RuntimeChannelRegistry()
    channel = new InMemoryRuntimePlcChannel(...)
    registry.Add(channel)

    supervisor = new InMemoryAutomationHubSupervisor(registry)

    coordinator = new PollingPublishCoordinator(
        registry,
        supervisor.RefreshSnapshotAsync)

    orchestrator = new PollingResultStateOrchestrator(
        registry,
        coordinator)

이 구성은 실제 Scheduler, Driver, `FakePlc`, WPF bridge 없이 Runtime 내부 publish path만 검증한다.

## 5. Success event end-to-end 검증

`ChannelPollingResult.Success(...)`가 `PollingResultStateOrchestrator`와 `PollingPublishCoordinator`를 거쳐 channel runtime state와 `RuntimeSnapshot.Channels`에 반영되는지 확인했다.

검증한 핵심 assertion:

- orchestration `result.Succeeded == true`
- `PublishResult.PublishSucceeded == true`
- `UpdatedCount == 1`
- `channel.GetRuntimeState().LastSuccessAt == occurredAt`
- `channel.GetRuntimeState().ConsecutiveFailures == 0`
- `channel.GetRuntimeState().LastResponseMs == 25`
- `channel.GetRuntimeState().LastError == null`
- `RuntimeSnapshot` 내 해당 channel `LastSuccessAt == occurredAt`
- `RuntimeSnapshot` 내 해당 channel `ConsecutiveFailures == 0`
- `RuntimeSnapshot` 내 해당 channel `LastResponseMs == 25`
- `LinkState == Online`
- `HealthSeverity == Healthy`
- `PollingState == Polling`

의미:

- success polling event의 event time이 channel state와 snapshot state에 동일하게 반영됨
- success mapping이 실패 누적값과 마지막 오류를 정리함
- snapshot publish는 `PollingPublishCoordinator`가 주입받은 `RefreshSnapshotAsync` 흐름을 통해 수행됨

## 6. Failure event end-to-end 검증

`ChannelPollingResult.Failure(...)`가 `PollingResultStateOrchestrator`와 `PollingPublishCoordinator`를 거쳐 channel runtime state와 `RuntimeSnapshot.Channels`에 반영되는지 확인했다.

검증한 핵심 assertion:

- orchestration `result.Succeeded == true`
- `PublishResult.PublishSucceeded == true`
- `UpdatedCount == 1`
- `channel.GetRuntimeState().LastFailureAt == occurredAt`
- `channel.GetRuntimeState().LastSuccessAt == previousLastSuccessAt`
- `channel.GetRuntimeState().ConsecutiveFailures == 3`
- `channel.GetRuntimeState().LastResponseMs == 30`
- `channel.GetRuntimeState().LastError == "polling failed"`
- `RuntimeSnapshot` 내 해당 channel `LastFailureAt == occurredAt`
- `RuntimeSnapshot` 내 해당 channel `LastSuccessAt == previousLastSuccessAt`
- `RuntimeSnapshot` 내 해당 channel `ConsecutiveFailures == 3`
- `RuntimeSnapshot` 내 해당 channel `LastResponseMs == 30`
- `RuntimeSnapshot` 내 해당 channel `LastError == "polling failed"`
- `HealthSeverity == Warning`
- `PollingState == Delayed`
- `LinkState`는 previous 유지

의미:

- failure polling event의 event time이 `LastFailureAt`에 반영됨
- 이전 success timestamp는 failure event로 덮어쓰지 않음
- failure response time이 null이면 이전 `LastResponseMs`를 유지함
- failure mapping은 health / polling 상태를 경고와 지연 상태로 조정하지만 link state는 현재 mapper 정책상 previous 값을 유지함

## 7. SnapshotChanged 검증

AH-RUNTIME-34 테스트에는 `SnapshotChanged` 검증을 포함했다.

검증한 내용:

- `SnapshotChanged`가 1회 발생
- event snapshot이 현재 snapshot과 동일
- `SnapshotChanged.OccurredAt == snapshot.CapturedAt`

의미:

- `ReplaceState` 자체가 `SnapshotChanged`를 발생시키는 것이 아님
- `RefreshSnapshotAsync` 흐름에서 snapshot refresh와 event publish가 발생함
- `SnapshotChanged.OccurredAt`은 polling event time이 아니라 snapshot frame time과 일치함

## 8. OccurredAt / CapturedAt 분리

AH-RUNTIME-34에서 polling event time과 snapshot capture time 분리를 고정했다.

- `ChannelPollingResult.OccurredAt`은 polling event 발생 시각이다.
- `RuntimeSnapshot.CapturedAt`은 snapshot frame 수집 시각이다.
- `occurredAt`은 `2026-01-01T00:00:00Z` 고정값으로 사용했다.
- `LastSuccessAt` / `LastFailureAt`은 `occurredAt` 기준으로 반영된다.
- `RuntimeSnapshot.CapturedAt`은 `occurredAt`과 다르다.
- `snapshot.Health.CapturedAt == snapshot.CapturedAt`이다.
- `SnapshotChanged.OccurredAt == snapshot.CapturedAt`이다.

따라서 polling event time과 snapshot capture time이 섞이지 않았다.

## 9. 동시성 리스크

AH-RUNTIME-33에서 남긴 리스크는 여전히 유효하다.

    GetRuntimeState()
            ↓
    RuntimePlcChannelStateMapper.Map(...)
            ↓
    PollingPublishCoordinator.PublishAsync(...)
            ↓
    ReplaceState(...)

이 전체 구간은 아직 원자적이지 않다.

AH-RUNTIME-34에서는 이 문제를 해결하지 않았다.

이번 작업의 처리 기준:

- 단일 fake polling event end-to-end 검증에 집중
- registry lock 확장하지 않음
- channel lock 확장하지 않음
- `IWritableRuntimePlcChannel` atomic update API 추가하지 않음
- concurrent stress test 추가하지 않음
- batch 처리 정책 확정하지 않음

동시성 문제는 별도 Boundary Review 후보로 남긴다.

## 10. 제외한 범위

이번 작업에서 수정하지 않은 영역:

- production code
- Contracts DTO
- WPF
- `XgtDriverCore`
- `FakePlc`
- `XgtChannelRunner`
- Real PLC
- `PollingScheduler` loop / timer
- reconnect 정책
- batch 처리
- `ContextPublisher`
- commit

이번 작업에서 구현하지 않은 항목:

- 실제 polling loop
- driver adapter
- XGT integration
- `FakePlc` integration
- reconnect decision
- WPF bridge 연결
- batch `ChannelPollingResult` 처리
- readback → map → publish 구간의 atomic update 보장
- `ContextPublisher` 자동 publish 재도입

## 11. 테스트 및 검증 결과

실행한 검증:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter PollingResultRuntimeSnapshotEndToEndTests`
  - 통과: 2
  - 실패: 0
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
  - 통과: 109
  - 실패: 0
- `dotnet build CAAutomationHub.sln`
  - 성공
  - 경고 0
  - 오류 0
- `git diff --check`
  - 출력 없음
  - exit 0

검증 의미:

- success fake polling event가 Runtime snapshot까지 반영됨
- failure fake polling event가 Runtime snapshot까지 반영됨
- `OccurredAt`이 `LastSuccessAt` 또는 `LastFailureAt`에 반영됨
- `CapturedAt`은 snapshot frame time으로 유지됨
- `SnapshotChanged`는 `RefreshSnapshotAsync` 흐름에서 발생함
- production code 수정 없이 기존 Runtime 내부 경계 조립만으로 end-to-end 검증이 가능함

## 12. 변경 파일 목록

AH-RUNTIME-34 skeleton 테스트 구현 변경 파일:

- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingResultRuntimeSnapshotEndToEndTests.cs`

AH-RUNTIME-34 closeout 문서:

- `docs/harness/AH-RUNTIME-34.md`

## 13. 다음 단계 후보

후보 1:

- AH-RUNTIME-34 commit 전 최종 검증
- 변경 파일 2개 또는 실제 생성 파일 확인
- Runtime tests / build / `git diff --check` 재확인
- working tree 상태 확인

후보 2:

- AH-RUNTIME-35: batch `ChannelPollingResult` 처리 Boundary Review
- 단일 event 처리에서 batch event 처리로 확장할지 검토
- `PollingPublishCoordinator` batch update 흐름과 일관성 확인

후보 3:

- AH-RUNTIME-35: `PollingScheduler` Boundary Review
- timer loop / interval / cancellation / batch cycle 검토
- 단, 실제 scheduler 구현은 이후 단계로 분리

후보 4:

- 동시성 / lost update 리스크 Boundary Review
- readback → map → publish 구간의 원자성 문제 검토
- atomic update API 필요 여부는 별도 단계에서 판단

후보 5:

- `XgtDriverCore` / `FakePlc` adapter Boundary Review
- 실제 driver event를 `ChannelPollingResult`로 변환하는 위치 검토
- 단, AH-RUNTIME-34 Closeout 단계에서는 연결하지 않음

## 14. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-34 목표였던 fake polling event 기반 Runtime publish end-to-end 검증 결과를 closeout 문서로 기록함
- success / failure event가 `RuntimeSnapshot`까지 반영되는 검증 내용을 기록함
- `SnapshotChanged`가 `RefreshSnapshotAsync` 흐름에서 발생한다는 의미를 기록함
- `OccurredAt` / `CapturedAt` 분리 의미를 기록함
- readback → map → publish 구간의 lost update 가능성을 리스크로 기록함
- 테스트, 빌드, `git diff --check` 검증 결과를 기록함
- `ContextPublisher` 자동 publish 미사용 정책을 유지함

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
