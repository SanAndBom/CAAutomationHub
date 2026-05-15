# AH-RUNTIME-31 Closeout

## 1. Summary

AH-RUNTIME-31은 Runtime 내부 polling event 결과를 `RuntimePlcChannelState`로 변환하기 위한 최소 skeleton 구현 단계다.

이번 단계에서 `ChannelPollingResult`, `ChannelPollingFailureKind`, `RuntimePlcChannelStateMapper`를 추가해 다음 내부 boundary를 고정했다.

    ChannelPollingResult
            ↓
    RuntimePlcChannelStateMapper
            ↓
    RuntimePlcChannelState

이번 작업은 실제 polling loop 구현이 아니다. 핵심은 polling success/failure event를 Runtime 내부에서 vendor-neutral하게 표현하고, event 발생 시각인 `OccurredAt`을 `RuntimeSnapshot.CapturedAt`과 분리하며, previous `RuntimePlcChannelState` 기반으로 next `RuntimePlcChannelState`를 계산하는 것이다.

## 2. Goal

목표는 `PollingPublishCoordinator` 앞단에 놓일 mapper boundary를 최소 구현하는 것이다.

포함된 목표:

- polling success/failure event를 표현하는 Runtime 내부 vendor-neutral 모델 추가
- polling event time과 snapshot capture time 분리
- previous `RuntimePlcChannelState` 기반 누적 상태 갱신
- `RuntimePlcChannelState`를 완성한 뒤 `PollingChannelUpdate`로 넘길 수 있는 앞단 boundary 고정
- Registry / Supervisor / WPF / XGT / FakePlc / Scheduler 책임 침범 방지

## 3. Boundary Review 결론

AH-RUNTIME-31 Boundary Review 결론은 다음과 같다.

- `PollingChannelUpdate`는 이미 완성된 `RuntimePlcChannelState`를 담는 publish 입력 모델이다.
- polling event success/failure 의미는 `PollingChannelUpdate` 앞단에서 별도 Runtime 내부 모델로 표현해야 한다.
- `ChannelPollingResult`는 XGT / FakePlc / raw frame / driver exception을 노출하지 않는 vendor-neutral Runtime 내부 모델이어야 한다.
- `RuntimePlcChannelStateMapper`는 previous `RuntimePlcChannelState`와 `ChannelPollingResult`만 받아 next `RuntimePlcChannelState`를 계산해야 한다.
- mapper는 registry lookup, state replacement, snapshot refresh, publish, reconnect, scheduler loop를 수행하지 않는다.

## 4. 구현 결과

### 4.1 추가 파일

- `src/CAAutomationHub.Runtime/Polling/ChannelPollingFailureKind.cs`
- `src/CAAutomationHub.Runtime/Polling/ChannelPollingResult.cs`
- `src/CAAutomationHub.Runtime/Polling/RuntimePlcChannelStateMapper.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/RuntimePlcChannelStateMapperTests.cs`

### 4.2 추가 타입

`ChannelPollingFailureKind`

- Runtime 내부 vendor-neutral polling failure classification이다.
- 값:
  - `Timeout`
  - `Connection`
  - `Protocol`
  - `UnexpectedResponse`
  - `Cancelled`
  - `Unknown`
- 이 enum은 Contracts enum이 아니며, XGT 전용 error code 또는 FakePlc scenario id를 직접 표현하지 않는다.

`ChannelPollingResult`

- Runtime 내부 polling event result 모델이다.
- 필드:
  - `PlcId`
  - `OccurredAt`
  - `IsSuccess`
  - `ResponseTimeMs`
  - `FailureKind`
  - `ErrorMessage`
- `Success(...)` factory와 `Failure(...)` factory를 제공한다.
- `CapturedAt` 필드는 두지 않았다.

`RuntimePlcChannelStateMapper`

- `Map(previous, result)`를 제공한다.
- 입력은 previous `RuntimePlcChannelState`와 `ChannelPollingResult`다.
- 출력은 next `RuntimePlcChannelState`다.
- `PlcId` mismatch는 `ArgumentException`으로 거부한다.
- mapper는 pure function에 가까운 계산 boundary로 유지한다.

### 4.3 Runtime 내부 흐름

이번 단계에서 구현된 흐름:

    ChannelPollingResult
            ↓
    RuntimePlcChannelStateMapper.Map(previous, result)
            ↓
    RuntimePlcChannelState

후속 단계에서 연결될 수 있는 전체 후보 흐름:

    미래의 PollingScheduler / Driver Adapter
            ↓
    ChannelPollingResult
            ↓
    RuntimePlcChannelStateMapper
            ↓
    RuntimePlcChannelState
            ↓
    PollingChannelUpdate
            ↓
    PollingPublishCoordinator
            ↓
    IWritableRuntimePlcChannel.ReplaceState
            ↓
    RefreshSnapshotAsync
            ↓
    RuntimeSnapshot / SnapshotChanged

AH-RUNTIME-31에서는 위 후보 흐름 중 `ChannelPollingResult`에서 `RuntimePlcChannelState`까지의 내부 mapper boundary만 구현했다.

## 5. Mapping 규칙

### 5.1 Success mapping

성공 result 처리 규칙:

- `LinkState = PlcLinkState.Online`
- `HealthSeverity = PlcHealthSeverity.Healthy`
- `PollingState = PlcPollingState.Polling`
- `LastSuccessAt = result.OccurredAt`
- `ConsecutiveFailures = 0`
- `LastResponseMs = result.ResponseTimeMs ?? previous.LastResponseMs`
- `LastError = null`
- 나머지 identity / 누적 필드는 previous 유지

의미:

- polling 성공은 최근 통신 성공을 의미하므로 `LastError`를 clear한다.
- success event는 previous failure streak를 종료하므로 `ConsecutiveFailures`를 0으로 reset한다.
- `ResponseTimeMs`가 없으면 기존 `LastResponseMs`를 보존한다.

### 5.2 Failure mapping

실패 result 처리 규칙:

- `LinkState = previous.LinkState`
- `HealthSeverity = PlcHealthSeverity.Warning`
- `PollingState = PlcPollingState.Delayed`
- `LastFailureAt = result.OccurredAt`
- `ConsecutiveFailures = previous.ConsecutiveFailures + 1`
- `LastSuccessAt = previous.LastSuccessAt`
- `LastResponseMs = result.ResponseTimeMs ?? previous.LastResponseMs`
- `LastError = result.ErrorMessage`

의미:

- polling 1회 실패만으로 `LinkState`를 `Faulted` 또는 `Reconnecting`으로 끌고 가지 않는다.
- failure는 우선 channel health / polling state 수준에서 보수적으로 반영한다.
- repeated failure threshold, `Warning`에서 `Error`로의 승격, reconnect 판단은 mapper skeleton 책임이 아니다.
- reconnect 수행은 mapper 책임이 아니다.

## 6. OccurredAt / CapturedAt 분리

AH-RUNTIME-31에서는 polling event time과 snapshot capture time을 분리했다.

- `ChannelPollingResult`에는 `CapturedAt`을 두지 않았다.
- `ChannelPollingResult`는 `OccurredAt`을 사용한다.
- `OccurredAt`은 polling success/failure event가 발생한 시각이다.
- `CapturedAt`은 `RuntimeSnapshot` frame 수집 시각이다.
- `RuntimePlcChannelStateMapper`는 snapshot capture time을 받지 않는다.
- 따라서 polling event time과 snapshot capture time이 섞이지 않는다.

이 결정은 기존 `InMemoryRuntimePlcChannel.GetState(capturedAt)` 의미와도 맞다. 현재 `GetState(capturedAt)`는 `capturedAt`으로 `LastSuccessAt` 또는 `LastFailureAt`을 덮어쓰지 않고, 내부 `RuntimePlcChannelState`의 event timestamp를 publish DTO로 변환한다.

## 7. 유지한 경계

AH-RUNTIME-31 mapper는 다음을 호출하지 않는다.

- `RuntimeChannelRegistry`
- `IWritableRuntimePlcChannel.ReplaceState`
- `InMemoryAutomationHubSupervisor.RefreshSnapshotAsync`
- `SnapshotChanged`
- `PollingPublishCoordinator`
- WPF mapper
- `XgtDriverCore`
- `FakePlc`
- `XgtChannelRunner`
- `PollingScheduler`
- reconnect API
- `ContextPublisher`

이번 단계에서 유지한 핵심 boundary:

- `IRuntimePlcChannel`은 read-only state provider로 유지
- `IWritableRuntimePlcChannel`은 optional writable boundary로 유지
- `RuntimeChannelRegistry`는 lookup-only / collection 관리 책임 유지
- update와 publish 분리 유지
- `PollingPublishCoordinator` 책임 확장 없음
- Runtime project는 WPF / XGT / FakePlc를 참조하지 않음
- ContextPublisher 자동 publish는 사용하지 않음

## 8. 제외한 범위

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
- `ContextPublisher` 자동 publish

이번 작업에서 구현하지 않은 항목:

- 실제 polling loop
- driver adapter
- XGT integration
- FakePlc integration
- reconnect decision
- publish orchestration 확장
- WPF bridge 연결
- ContextPublisher 재도입

## 9. 테스트 및 검증 결과

TDD 흐름:

- RED 확인: `RuntimePlcChannelStateMapperTests`가 missing type compile error로 실패
- 실패 원인:
  - `ChannelPollingResult` 미정의
  - `ChannelPollingFailureKind` 미정의
  - `RuntimePlcChannelStateMapper` 미정의

실행한 검증:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter RuntimePlcChannelStateMapperTests`
  - 11 passed
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
  - 99 passed
- `dotnet build CAAutomationHub.sln`
  - success
  - warnings 0
  - errors 0
- `git diff --check`
  - exit 0
  - output 없음

검증 의미:

- mapper tests가 success/failure event timestamp mapping을 검증함
- success mapping이 `LastSuccessAt`, `ConsecutiveFailures`, `LastResponseMs`, `LastError`, 상태 enum mapping을 검증함
- failure mapping이 `LastFailureAt`, `ConsecutiveFailures`, `LastSuccessAt` 보존, `LastResponseMs` 보존, 보수적 상태 mapping을 검증함
- mapper public input이 `RuntimePlcChannelState`와 `ChannelPollingResult`만 요구함을 검증함

## 10. 변경 파일 목록

AH-RUNTIME-31 skeleton 구현 변경 파일:

- `src/CAAutomationHub.Runtime/Polling/ChannelPollingFailureKind.cs`
- `src/CAAutomationHub.Runtime/Polling/ChannelPollingResult.cs`
- `src/CAAutomationHub.Runtime/Polling/RuntimePlcChannelStateMapper.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/RuntimePlcChannelStateMapperTests.cs`

AH-RUNTIME-31 closeout 문서:

- `docs/harness/AH-RUNTIME-31.md`

## 11. 다음 단계 후보

다음 후보는 AH-RUNTIME-32로 분리하는 것이 안전하다.

후보 1:

- `ChannelPollingResult`를 `PollingChannelUpdate`로 변환하는 orchestration boundary 검토
- 단, `PollingPublishCoordinator` 책임 확장은 신중히 검토

후보 2:

- `PollingScheduler` / Driver Adapter가 나중에 `ChannelPollingResult`를 생성하는 위치 검토
- 단, 실제 scheduler loop 구현은 별도 단계

후보 3:

- `RuntimePlcChannelStateMapper` mapping 정책 보강
- 예: `ConsecutiveFailures` threshold 기반 `HealthSeverity` 승격
- 예: timeout repeated failure 시 `Warning`에서 `Error`로 전환하는 정책
- 단, reconnect 수행은 mapper 책임이 아님

후보 4:

- AH-RUNTIME-31 결과를 WPF bridge로 바로 연결하지 않음
- Runtime publish path와 별도 검증 후 WPF bridge 연결 여부를 판단

## 12. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-31 목표였던 Runtime 내부 polling event result와 mapper skeleton이 추가됨
- `OccurredAt`과 `CapturedAt` 분리가 유지됨
- previous `RuntimePlcChannelState` 기반으로 next state를 계산함
- `PollingPublishCoordinator` 앞단 mapper boundary가 고정됨
- Registry / Supervisor / WPF / XGT / FakePlc / Scheduler / ContextPublisher 경계를 침범하지 않음
- focused mapper tests, Runtime tests, solution build, diff check가 통과함

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
