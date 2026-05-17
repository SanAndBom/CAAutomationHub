# AH-RUNTIME-32 Closeout

## 1. Summary

AH-RUNTIME-32는 AH-RUNTIME-31에서 추가한 `RuntimePlcChannelStateMapper` 결과를 기존 Runtime publish 흐름에 어떻게 연결할지 검토한 Boundary Review 단계다.

이번 단계에서는 코드 수정, 테스트 수정, skeleton 추가를 수행하지 않았다. 실제 타입과 현재 흐름을 확인한 뒤, `ChannelPollingResult` 이후 orchestration 책임을 어디에 둘지 검토했다.

검토 결과 권장안은 후보 B: 별도 orchestration service다. 권장 이름은 `PollingResultStateOrchestrator`이며, 이 service는 `ChannelPollingResult`를 받아 previous `RuntimePlcChannelState`를 readback하고, `RuntimePlcChannelStateMapper.Map(previous, result)`를 호출한 뒤, 결과를 `PollingChannelUpdate`로 감싸 `PollingPublishCoordinator`에 위임하는 앞단 조립자 역할을 맡는다.

## 2. Goal

목표는 다음 흐름의 연결 책임을 설계 수준에서 확정하는 것이었다.

    ChannelPollingResult
            ↓
    previous RuntimePlcChannelState 조회
            ↓
    RuntimePlcChannelStateMapper.Map(previous, result)
            ↓
    PollingChannelUpdate 생성
            ↓
    PollingPublishCoordinator 전달

핵심 질문:

- 누가 `RuntimePlcChannelStateMapper.Map(previous, result)`를 호출할 것인가?
- 누가 previous `RuntimePlcChannelState`를 가져올 것인가?
- 누가 `RuntimePlcChannelState`를 `PollingChannelUpdate`로 감쌀 것인가?
- 누가 `PollingPublishCoordinator`에 전달할 것인가?
- `PollingPublishCoordinator` 책임을 확장해야 하는가?
- 아니면 앞단 orchestration service가 필요한가?

## 3. Background

AH-RUNTIME-31에서는 Runtime 내부 polling event result를 `RuntimePlcChannelState`로 변환하는 mapper boundary를 추가했다.

    ChannelPollingResult
            ↓
    RuntimePlcChannelStateMapper
            ↓
    RuntimePlcChannelState

기존 publish 흐름은 다음과 같다.

    PollingChannelUpdate
            ↓
    PollingPublishCoordinator
            ↓
    IWritableRuntimePlcChannel.ReplaceState
            ↓
    refreshSnapshotAsync
            ↓
    RuntimeSnapshot / SnapshotChanged

AH-RUNTIME-32의 본질은 위 두 흐름 사이에 어느 boundary를 둘지 검토하는 것이었다. 특히 `PollingPublishCoordinator`에 mapper/readback 책임을 추가할지, 아니면 별도 orchestration service를 둘지 검토했다.

## 4. 확인한 실제 타입 / 메서드

`ChannelPollingResult`

- `sealed record`
- namespace: `CAAutomationHub.Runtime.Polling`
- 필드:
  - `PlcId`
  - `OccurredAt`
  - `IsSuccess`
  - `ResponseTimeMs`
  - `FailureKind`
  - `ErrorMessage`
- `Success(...)` factory와 `Failure(...)` factory를 제공한다.
- `CapturedAt`은 없다.

`RuntimePlcChannelStateMapper`

- `static class`
- public method:
  - `Map(RuntimePlcChannelState previous, ChannelPollingResult result)`
- 반환 타입:
  - `RuntimePlcChannelState`
- mapper는 previous state와 polling result만 받아 next state를 계산한다.
- registry, supervisor, publish, snapshot refresh dependency를 요구하지 않는다.

`RuntimePlcChannelState`

- Runtime-local state record
- 주요 필드:
  - `PlcId`
  - `PlcName`
  - `LineName`
  - `IsEnabled`
  - `IpAddress`
  - `Port`
  - `LinkState`
  - `HealthSeverity`
  - `PollingState`
  - `SequenceState`
  - `ConfiguredPollingIntervalMs`
  - `EffectivePollingIntervalMs`
  - `LastResponseMs`
  - `ConsecutiveFailures`
  - `ReconnectCount`
  - `SuccessRate`
  - `LastSuccessAt`
  - `LastFailureAt`
  - `LastError`
- `ToChannelRuntimeState()`를 통해 publish DTO인 `ChannelRuntimeState`로 변환된다.

`PollingChannelUpdate`

- namespace: `CAAutomationHub.Runtime.Polling`
- `PlcId`와 `RuntimePlcChannelState`를 담는 단순 package다.
- `PollingPublishCoordinator`의 입력 모델과 맞는다.

`PollingPublishCoordinator`

- `PollingChannelUpdate` batch를 입력으로 받는다.
- `RuntimeChannelRegistry.TryGetChannel`로 channel을 조회한다.
- `IRuntimePlcChannel`을 `IWritableRuntimePlcChannel`로 pattern matching한다.
- writable channel이면 `ReplaceState(update.State)`를 호출한다.
- 적어도 하나의 update가 적용되면 `_refreshSnapshotAsync(cancellationToken)`을 한 번 호출한다.
- `PollingPublishResult`를 반환한다.
- missing channel, non-writable channel, update failure, publish failure를 결과에 기록한다.

`IWritableRuntimePlcChannel`

- `IRuntimePlcChannel`을 확장하는 optional writable boundary다.
- `GetRuntimeState()`를 제공한다.
- `ReplaceState(RuntimePlcChannelState state)`를 제공한다.
- `GetRuntimeState`와 `ReplaceState`는 snapshot publish나 `SnapshotChanged` 발생 책임을 갖지 않는다.

`RuntimeChannelRegistry`

- `TryGetChannel(plcId, out IRuntimePlcChannel channel)`을 제공한다.
- channel collection 관리와 lookup 책임만 갖는다.
- update/publish 책임은 없다.

## 5. 현재 흐름 요약

현재 mapper 흐름:

    ChannelPollingResult
            ↓
    RuntimePlcChannelStateMapper.Map(previous, result)
            ↓
    RuntimePlcChannelState

현재 publish 흐름:

    PollingChannelUpdate
            ↓
    PollingPublishCoordinator.PublishAsync(...)
            ↓
    RuntimeChannelRegistry.TryGetChannel
            ↓
    IWritableRuntimePlcChannel.ReplaceState
            ↓
    refreshSnapshotAsync
            ↓
    RuntimeSnapshot / SnapshotChanged

AH-RUNTIME-32에서 검토한 연결 후보:

    ChannelPollingResult
            ↓
    previous RuntimePlcChannelState readback
            ↓
    RuntimePlcChannelStateMapper.Map(previous, result)
            ↓
    PollingChannelUpdate
            ↓
    PollingPublishCoordinator

## 6. 핵심 질문 답변

1. 현재 `ChannelPollingResult`는 어떤 정보를 가지고 있는가?

`PlcId`, `OccurredAt`, `IsSuccess`, `ResponseTimeMs`, `FailureKind`, `ErrorMessage`를 가진다. `CapturedAt`은 없다.

2. `RuntimePlcChannelStateMapper.Map(...)`은 어떤 입력과 출력을 가지는가?

입력은 previous `RuntimePlcChannelState`와 `ChannelPollingResult`다. 출력은 next `RuntimePlcChannelState`다.

3. `PollingChannelUpdate`는 현재 어떤 구조인가?

`PlcId`와 `RuntimePlcChannelState`를 담는 단순 package다.

4. `PollingPublishCoordinator`는 현재 어느 책임까지 가지고 있는가?

`PollingChannelUpdate` batch를 받아 channel lookup, writable check, `ReplaceState`, refresh delegate 호출, publish result 생성까지 담당한다.

5. previous `RuntimePlcChannelState`는 어디서 가져오는 것이 가장 안전한가?

`RuntimeChannelRegistry.TryGetChannel`로 channel을 조회한 뒤, channel이 `IWritableRuntimePlcChannel`이면 `GetRuntimeState()`에서 readback하는 것이 가장 안전하다.

6. `ChannelPollingResult`를 직접 `PollingPublishCoordinator`에 넣는 것이 적절한가?

부적절하다. `PollingPublishCoordinator`의 입력 모델은 이미 `PollingChannelUpdate`로 정리되어 있으며, `ChannelPollingResult` 해석 책임을 추가하면 boundary가 흐려진다.

7. `PollingPublishCoordinator`에 mapper 책임을 추가하면 어떤 문제가 생기는가?

`ChannelPollingResult` 해석, previous state readback, mapper 호출, publish 조율, snapshot refresh가 한 클래스에 섞인다. 향후 scheduler / driver adapter와 coordinator 결합도도 높아질 수 있다.

8. 별도 orchestration service가 필요하다면 이름과 책임은 무엇이 적절한가?

이름은 `PollingResultStateOrchestrator`가 가장 적절하다. 책임은 `ChannelPollingResult`를 Runtime publish 가능한 `PollingChannelUpdate`로 조립하고 기존 `PollingPublishCoordinator`에 위임하는 것이다.

9. 그 service는 `RuntimeChannelRegistry`를 직접 참조해도 되는가?

lookup-only 사용이라면 허용 가능하다. 단, registry에 update/publish 책임을 추가하지 않아야 한다.

10. 그 service는 `RefreshSnapshotAsync`를 직접 호출해도 되는가?

비권장이다. refresh는 `PollingPublishCoordinator`의 기존 publish 흐름 안에서 수행되어야 한다.

11. 그 service는 `PollingPublishCoordinator`를 호출해도 되는가?

적절하다. service는 조립자이고, 실제 state replacement와 snapshot refresh 위임은 coordinator가 맡는다.

12. `ChannelPollingResult` batch를 지원해야 하는가, single result부터 시작해야 하는가?

AH-RUNTIME-33 skeleton은 single result부터 시작하는 것이 안전하다. batch 처리는 후속 Boundary Review 후보로 남긴다.

13. AH-RUNTIME-33 skeleton 구현 시 최소 변경 범위는 무엇인가?

`PollingResultStateOrchestrator`와 해당 tests 추가가 최소 범위다. 기존 coordinator, registry, channel interfaces는 변경하지 않는다.

14. AH-RUNTIME-33에서도 제외해야 할 범위는 무엇인가?

Scheduler loop, Driver adapter, XGT, `FakePlc`, WPF, reconnect 정책, public supervisor contract 확장, `ContextPublisher` 자동 publish는 제외한다.

## 7. 후보 A: PollingPublishCoordinator 확장 검토

판정: 비권장

장점:

- 호출 지점이 단순해질 수 있음
- publish 흐름이 한 곳에 모일 수 있음

위험:

- `PollingPublishCoordinator`가 `ChannelPollingResult` 해석 책임까지 갖게 됨
- previous state readback 책임이 섞임
- mapper 호출 책임이 섞임
- `PollingChannelUpdate`와 `ChannelPollingResult` 두 입력 모델이 한 클래스에 혼재됨
- 향후 scheduler / driver adapter와 coordinator 결합도가 높아질 수 있음
- update/publish 조율자라는 기존 책임이 흐려짐

결론:

- `PollingPublishCoordinator`는 기존처럼 `PollingChannelUpdate` publish 책임에 머무르는 것이 안전하다.

## 8. 후보 B: 별도 Orchestration Service 검토

판정: 권장

후보 이름:

- `PollingResultStateOrchestrator`

후보 흐름:

    ChannelPollingResult
            ↓
    RuntimeChannelRegistry.TryGetChannel
            ↓
    IWritableRuntimePlcChannel.GetRuntimeState()
            ↓
    RuntimePlcChannelStateMapper.Map(previous, result)
            ↓
    PollingChannelUpdate 생성
            ↓
    PollingPublishCoordinator.PublishAsync

장점:

- mapper와 publish coordinator 사이의 orchestration 책임이 분리됨
- `PollingPublishCoordinator`는 `PollingChannelUpdate` publish 책임만 유지 가능
- `ChannelPollingResult`는 coordinator에 직접 들어가지 않음
- `RuntimeChannelRegistry`는 lookup-only로만 사용됨
- 향후 scheduler / driver adapter가 붙을 entry point가 생김
- AH-RUNTIME-33 Skeleton 구현 후보로 자연스럽게 이어짐

위험:

- service가 registry와 coordinator를 모두 참조하게 됨
- readback과 publish 사이의 race condition 가능성이 있음
- batch 처리 정책이 아직 확정되지 않음
- service 이름과 책임이 불명확하면 또 다른 god object가 될 수 있음

결론:

- AH-RUNTIME-33에서는 `PollingResultStateOrchestrator` skeleton으로 진행하는 것이 안전하다.

## 9. 후보 C: Update Builder 검토

판정: 부분적으로 유용하지만 단독으로는 부족함

후보 흐름:

    previous RuntimePlcChannelState
            +
    ChannelPollingResult
            ↓
    PollingChannelUpdateBuilder
            ↓
    PollingChannelUpdate

장점:

- 가장 순수한 변환 경계
- 테스트가 쉬움
- Registry / Supervisor / Coordinator와 분리 가능

위험:

- 누가 previous state를 가져오는지 해결하지 못함
- 누가 `PollingPublishCoordinator`에 전달하는지 해결하지 못함
- 결국 별도 orchestration layer가 추가로 필요할 가능성이 큼

결론:

- 향후 내부 helper로는 가능하지만, AH-RUNTIME-32 핵심 질문의 주답은 아니다.
- 현재 권장안은 후보 B다.

## 10. 권장안

권장안은 후보 B: 별도 orchestration service다.

권장 service:

- `PollingResultStateOrchestrator`

권장 책임:

- `ChannelPollingResult`를 받는다.
- `RuntimeChannelRegistry.TryGetChannel`로 channel을 조회한다.
- channel이 없거나 writable이 아니면 orchestration failure로 반환한다.
- `IWritableRuntimePlcChannel.GetRuntimeState()`로 previous state를 읽는다.
- `RuntimePlcChannelStateMapper.Map(previous, result)`를 호출한다.
- `PollingChannelUpdate`를 생성한다.
- `PollingPublishCoordinator.PublishAsync(...)`에 위임한다.
- `PollingPublishResult`를 포함한 orchestration result를 반환한다.

권장하지 않는 것:

- `PollingPublishCoordinator`에 `ChannelPollingResult` 해석 책임 추가
- `PollingPublishCoordinator`에 mapper 책임 추가
- orchestration service에서 `RefreshSnapshotAsync` 직접 호출
- orchestration service에서 `ReplaceState` 직접 호출
- orchestration service에서 `SnapshotChanged` 직접 발생

## 11. OccurredAt / CapturedAt 분리

AH-RUNTIME-32 설계에서도 polling event time과 snapshot capture time은 분리되어야 한다.

- `ChannelPollingResult.OccurredAt`은 polling event 발생 시각이다.
- `RuntimeSnapshot.CapturedAt`은 snapshot frame 수집 시각이다.
- `PollingChannelUpdate`는 snapshot frame이 아니라 상태 변경 package다.
- `PollingPublishCoordinator`가 `refreshSnapshotAsync`를 호출할 때 snapshot capture가 발생한다.
- Orchestration service는 `CapturedAt`을 만들거나 받지 않는 방향이 안전하다.
- `ChannelPollingResult.OccurredAt`은 mapper를 통해 `LastSuccessAt` 또는 `LastFailureAt`으로만 반영된다.
- 따라서 polling event time과 snapshot capture time이 섞이지 않는다.

## 12. 동시성 / Race Condition 리스크

예상 흐름:

    GetRuntimeState()
            ↓
    RuntimePlcChannelStateMapper.Map(...)
            ↓
    PollingPublishCoordinator.PublishAsync(...)
            ↓
    ReplaceState(...)

`GetRuntimeState`와 `ReplaceState`는 각각 channel 내부 gate로 보호될 수 있지만, readback → map → publish 전체 과정은 아직 원자적이지 않다.

따라서 readback과 replace 사이에 다른 update가 들어오면 lost update 가능성이 있다.

AH-RUNTIME-32에서는 이 문제를 해결하지 않는다.

처리 기준:

- 리스크로 명시
- AH-RUNTIME-33 skeleton에서는 단일 result 처리 범위로 제한
- `RuntimeChannelRegistry` lock 확장 금지
- channel lock 구조 확장 금지
- `IWritableRuntimePlcChannel`에 atomic update API 추가 금지
- 동시성 해결은 별도 Boundary Review 후보로 남김

## 13. AH-RUNTIME-33 Skeleton 후보

권장 후보:

- `PollingResultStateOrchestrator` skeleton

추가 파일 후보:

- `src/CAAutomationHub.Runtime/Polling/PollingResultStateOrchestrator.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingResultStateOrchestratorTests.cs`

최소 skeleton 범위:

- constructor null guard
- single `ChannelPollingResult` 처리
- missing channel 처리
- non-writable channel 처리
- writable channel에서 `GetRuntimeState` readback
- `RuntimePlcChannelStateMapper.Map` 호출
- `PollingChannelUpdate` 생성
- `PollingPublishCoordinator.PublishAsync` 위임
- orchestration result 반환

제외할 범위:

- Scheduler loop
- Driver adapter
- `XgtDriverCore`
- `FakePlc`
- WPF
- reconnect 정책
- public supervisor contract 확장
- `ContextPublisher` 자동 publish

## 14. 제외한 범위

AH-RUNTIME-32 Boundary Review에서는 다음을 수행하지 않았다.

- production code 수정
- test code 수정
- skeleton class 추가
- interface 확장
- enum 추가
- Contracts DTO 수정
- WPF 수정
- `XgtDriverCore` 연결
- `FakePlc` 연결
- `XgtChannelRunner` 연결
- `PollingScheduler` timer / loop 구현
- reconnect 정책 구현
- `ContextPublisher` 자동 publish 재도입
- commit

## 15. 실행한 명령

AH-RUNTIME-32 Boundary Review 당시 실행한 명령:

- `git status --short`
- `rg "ChannelPollingResult" src tests`
- `rg "RuntimePlcChannelStateMapper" src tests`
- `rg "PollingChannelUpdate" src tests`
- `rg "PollingPublishCoordinator" src tests`
- `rg "GetRuntimeState" src tests`
- `rg "TryGetChannel" src tests`
- 관련 `Get-Content`
- line 확인용 `rg -n`

검증 당시 의미:

- `git status --short` 출력 없음으로 작업 전 working tree clean 확인
- `ChannelPollingResult`, `RuntimePlcChannelStateMapper`, `PollingChannelUpdate`, `PollingPublishCoordinator`, `GetRuntimeState`, `TryGetChannel`의 실제 타입과 사용 지점 확인
- `CAAutomationHub.Runtime.csproj`가 `CAAutomationHub.Contracts`만 참조함을 확인

## 16. 다음 단계 후보

후보 1:

- AH-RUNTIME-33: `PollingResultStateOrchestrator` skeleton 구현
- `ChannelPollingResult`를 Runtime publish 가능한 `PollingChannelUpdate`로 조립
- `PollingPublishCoordinator`에 위임
- Scheduler / Driver / XGT / `FakePlc` / WPF는 제외

후보 2:

- fake polling event 기반 Runtime publish end-to-end test
- `ChannelPollingResult` → `PollingResultStateOrchestrator` → `PollingPublishCoordinator` → `RuntimeSnapshot` 흐름 검증
- 실제 Scheduler / XGT / `FakePlc` 연결 없이 in-memory 경로만 검증

후보 3:

- batch `ChannelPollingResult` 처리 Boundary Review
- `PollingPublishCoordinator`의 batch update 흐름과 일관성 확인

후보 4:

- 동시성 / lost update 리스크 Boundary Review
- readback → map → publish 구간의 원자성 검토
- atomic update API 필요 여부는 별도 단계에서 판단

후보 5:

- `PollingScheduler` Boundary Review
- timer loop / interval / cancellation / batch cycle 검토
- 실제 scheduler 구현은 이후 단계로 분리

후보 6:

- `XgtDriverCore` / `FakePlc` adapter Boundary Review
- driver event를 `ChannelPollingResult`로 변환하는 위치 검토

## 17. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-32 목표였던 Runtime polling result 이후 orchestration boundary review 결과가 closeout 문서로 기록됨
- 후보 A / B / C 검토와 권장안을 명확히 남김
- 권장안이 별도 `PollingResultStateOrchestrator`임을 기록함
- `PollingPublishCoordinator` 책임을 확장하지 않는 이유를 기록함
- `RuntimeChannelRegistry` lookup-only 사용 원칙을 기록함
- `OccurredAt` / `CapturedAt` 분리 의미를 기록함
- readback → map → publish 구간의 race condition / lost update 리스크를 기록함
- AH-RUNTIME-33 skeleton 후보와 제외 범위를 기록함
- `ContextPublisher` 자동 publish 미사용 정책을 유지함

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
