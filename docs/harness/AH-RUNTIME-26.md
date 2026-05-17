# AH-RUNTIME-26 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-26의 목표는 AH-RUNTIME-25에서 `IWritableRuntimePlcChannel`과 `RuntimePlcChannelState`가 추가된 이후, polling 결과가 Runtime channel state update와 `RuntimeSnapshot` publish로 이어지는 orchestration 경계를 어떻게 둘지 검토하는 것이다.

이번 단계는 `PollingScheduler`를 바로 구현하거나, `XgtDriverCore` / `XgtChannelRunner` / `FakePlc` integration을 구현하는 단계가 아니라, polling result -> channel state update -> `supervisor.RefreshSnapshotAsync(...)` publish path를 설계하는 Boundary Review이다.

## 3. Scope

이번 단계에 포함된 검토 항목:

- `PollingScheduler` 도입 여부 검토
- `PollingPublishCoordinator` 후보 검토
- `PollingCycleRunner` 후보 검토
- polling result model 도입 여부 검토
- update all then publish once 정책 검토
- writable channel 처리 정책 검토
- `refreshSnapshotAsync` delegate 주입 후보 검토
- missing / non-writable / update failure / refresh failure 결과 처리 방향 검토
- timer / loop 제외 여부 검토

## 4. Decision

결정 사항:

- AH-RUNTIME-26은 계획 단계로 종료
- `PollingScheduler`는 아직 도입하지 않음
- timer / interval / background loop / reentrancy / shutdown 정책은 보류
- 현재 관심사는 scheduling이 아니라 publish path boundary
- 후속 최소 구현 후보는 `PollingPublishCoordinator`
- `PollingCycleRunner`는 실제 polling source와 result mapper가 생긴 뒤 후보로 둠
- XGT / `FakePlc` / actual polling read는 제외
- `RuntimeChannelRegistry`는 lookup-only 유지
- `RuntimeChannelRegistry` update API는 추가하지 않음
- `IAutomationHubSupervisor` public contract는 확장하지 않음
- WPF / DI / App wiring은 변경하지 않음

## 5. Publish Path Direction

후속 skeleton 기준 흐름:

```text
IReadOnlyList<PollingChannelUpdate>
-> RuntimeChannelRegistry.TryGetChannel(plcId, out channel)
-> channel is IWritableRuntimePlcChannel writable
-> writable.ReplaceState(RuntimePlcChannelState)
-> all updates processed
-> refreshSnapshotAsync(cancellationToken)
-> result/report 반환
```

정책:

- channel마다 publish하지 않음
- cycle 단위로 update를 모은 뒤 한 번 publish
- `SnapshotChanged` event storm을 피함
- WPF rail은 한 cycle당 한 번 갱신되는 방향을 기본으로 함

## 6. Publish Caller Direction

결정:

- `IAutomationHubSupervisor` public contract는 확장하지 않음
- `InMemoryAutomationHubSupervisor` 직접 주입은 concrete coupling이 생기므로 우선순위 낮음
- `refreshSnapshotAsync` delegate 주입을 우선 후보로 둠

후속 후보:

```csharp
Func<CancellationToken, Task<RuntimeSnapshot>> refreshSnapshotAsync
```

이유:

- `InMemoryAutomationHubSupervisor.RefreshSnapshotAsync(...)` concrete 의존을 coordinator 밖으로 밀어낼 수 있음
- `IAutomationHubSupervisor` 확장 없이 publish path를 검증할 수 있음
- 테스트하기 쉬움

## 7. Writable Handling Policy

정책 후보:

- missing channel은 result에 기록하고 cycle 계속 진행
- non-writable channel은 result에 기록하고 cycle 계속 진행
- update failure는 해당 channel failure로 기록
- refresh failure는 publish failure로 기록하거나 exception propagate 정책을 후속에서 선택

권장:

- exception-first보다 report-first가 초기 skeleton에 적합
- 가능한 update는 수행하고, 마지막에 단일 publish를 시도하며, 결과 보고를 반환하는 쪽이 테스트하기 쉬움

## 8. Result Model Direction

후속 후보:

- `PollingChannelUpdate`
  - `PlcId`
  - `RuntimePlcChannelState`
- `PollingPublishResult`
  - updated count
  - missing channels
  - non-writable channels
  - failed updates
  - publish success / failure

보류 후보:

- `PollingCycleResult`
  - 실제 polling source 결과와 publish 결과를 함께 묶을 필요가 생길 때 도입

정책:

- AH-RUNTIME-26에서는 result model을 구현하지 않음
- 후속 최소 구현은 `RuntimePlcChannelState` batch input으로 시작 가능

## 9. Timer / Loop Boundary

제외:

- `BackgroundService`
- `DispatcherTimer`
- `Task` loop
- interval / cadence
- concurrent cycle 방지
- shutdown / cancellation lifecycle

이유:

- timer가 들어오면 cancellation, concurrency, reentrancy, interval, shutdown 정책이 함께 필요함
- 먼저 update / publish path를 검증해야 함

## 10. Expected Follow-up Implementation Scope

후속 구현 권장 범위:

- `PollingPublishCoordinator` skeleton
- `PollingChannelUpdate`
- `PollingPublishResult`
- `RuntimeChannelRegistry` lookup 사용
- `IWritableRuntimePlcChannel.ReplaceState` 사용
- `refreshSnapshotAsync` delegate 호출
- update all then publish once
- Runtime 단위 테스트

예상 파일 후보:

- `src/CAAutomationHub.Runtime/Polling/PollingPublishCoordinator.cs`
- `src/CAAutomationHub.Runtime/Polling/PollingPublishResult.cs`
- `src/CAAutomationHub.Runtime/Polling/PollingChannelUpdate.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingPublishCoordinatorTests.cs`

## 11. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- 코드 수정
- 파일 생성/수정, Closeout 문서 제외
- 테스트 추가
- 명령 실행
- actual `PollingScheduler` timer / loop
- `XgtDriverCore` 참조 추가
- `XgtChannelRunner` 참조 추가
- `FakePlc` 참조 추가
- real PLC polling
- command dispatcher
- Runtime Event Bridge
- telemetry aggregation
- WPF 변경
- DI / App wiring
- `DashboardViewModel` 변경
- `RuntimeDashboardAdapter` 변경
- `IAutomationHubSupervisor` interface 확장
- `RuntimeChannelRegistry` update API

## 12. Validation

이번 단계는 계획 / Boundary Review 단계이다.

검증 기준:

- 코드 수정 없음
- 파일 생성은 Closeout 문서만 허용
- `PollingScheduler`는 아직 보류
- `PollingPublishCoordinator` skeleton이 다음 최소 구현 후보로 기록됨
- update all then publish once 정책이 historical record에 남음
- registry lookup-only 유지가 기록됨
- writable 여부는 coordinator가 판별하는 방향이 기록됨
- publish는 `RefreshSnapshotAsync` delegate 주입 우선 검토로 기록됨
- Runtime은 계속 `CAAutomationHub.Contracts`만 참조한다는 경계가 유지됨

## 13. ACCEPT Decision

ACCEPT

이유:

- `PollingScheduler`와 `PollingCycleRunner` / `PublishCoordinator` 책임 차이가 정리됨
- timer / loop 제외 여부가 정리됨
- update all then publish once 정책이 정리됨
- registry lookup 사용 방식이 정리됨
- writable channel 처리 방식이 정리됨
- refresh publish 호출 방식이 정리됨
- 후속 구현 범위가 `PollingPublishCoordinator` skeleton 수준으로 제한됨
- 후속 구현 지시문을 만들 수 있을 만큼 예상 파일, 테스트, 명령, 제외 범위가 정리됨

## 14. Risks / Follow-up Candidates

AH-RUNTIME-27 후보:

- `PollingPublishCoordinator` skeleton
- `PollingChannelUpdate`
- `PollingPublishResult`
- update all then publish once 테스트
- missing / non-writable / failed update result policy
- refresh delegate failure policy

추가 후속 후보:

- actual `PollingScheduler` timer / loop
- `XgtRuntimePlcChannelAdapter` boundary review
- `FakePlc` integration boundary review
- Runtime telemetry contract
- command dispatcher skeleton

## 15. Next Step

다음 단계는 AH-RUNTIME-27: PollingPublishCoordinator Skeleton Implementation이다.

단, AH-RUNTIME-27에서도 actual `PollingScheduler` timer / loop, XGT / `FakePlc` / WPF / DI / App wiring은 제외하고, Runtime 내부 publish path coordinator와 테스트까지만 작게 진행하는 것이 안전하다.
