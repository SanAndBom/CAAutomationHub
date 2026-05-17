# AH-RUNTIME-14 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-14의 목표는 AH-RUNTIME-13에서 `RuntimeChannelRegistry`가 `InMemoryAutomationHubSupervisor`에 연결된 이후, channel 상태 변화가 있을 때 `RuntimeSnapshot`을 다시 publish하는 API 경계를 어떻게 둘지 검토하는 것입니다.

이번 단계는 실제 channel update API, polling scheduler, `XgtDriverCore` 연결, `XgtChannelRunner` 연결, FakePlc integration을 구현하는 단계가 아니라, `RuntimeSnapshot` 재발행을 누가 어떤 API로 요청할 수 있는지 설계하는 Boundary Review입니다.

## 3. Scope

이번 단계에 포함된 검토 항목:

- Refresh / Publish Snapshot API 필요성 검토
- `IAutomationHubSupervisor` contract 확장 여부 검토
- `InMemoryAutomationHubSupervisor` concrete API 후보 검토
- `RefreshSnapshotAsync` 이름 후보 검토
- `GetSnapshotAsync` cache-only 정책 재확인
- `SnapshotChanged` publish 정책 검토
- refresh 실패 시 cache 유지 정책 검토
- thread-safety 정책 검토
- WPF 직접 호출 금지 경계 검토
- 후속 channel update / polling / command 호출 주체 후보 검토

## 4. Decision

결정 사항:

- AH-RUNTIME-14는 계획 단계로 종료
- `IAutomationHubSupervisor` public contract는 아직 확장하지 않음
- `RefreshSnapshotAsync`를 interface에 추가하지 않음
- interface 승격은 후속 검토 후보로만 기록
- 후속 구현이 승인된다면 `InMemoryAutomationHubSupervisor` concrete API 후보로 `RefreshSnapshotAsync(CancellationToken)`를 검토
- WPF나 app wiring이 concrete `RefreshSnapshotAsync`를 직접 호출하는 구조는 금지
- `GetSnapshotAsync`는 계속 cache-only 조회 API로 유지
- `GetSnapshotAsync`는 `registry.GetStates`를 호출하지 않음
- `GetSnapshotAsync`는 `SnapshotChanged`를 발생시키지 않음
- snapshot 재생성/재발행은 explicit refresh/publish 경계에서만 수행
- channel update API, polling scheduler, command dispatcher는 이번 단계에서 제외

## 5. RefreshSnapshotAsync Candidate Policy

후속 구현 후보:

- `InMemoryAutomationHubSupervisor.RefreshSnapshotAsync(CancellationToken)`
- 반환형 후보는 `Task<RuntimeSnapshot>`
- `cancellationToken` 확인
- 단일 `capturedAt` 생성
- `registry.GetStates(capturedAt)` 호출
- `RuntimeHealthState` 최소 계산
- `RuntimeSnapshot` 생성
- current snapshot cache 갱신
- revision 증가
- `SnapshotChanged` 발생
- publish된 `RuntimeSnapshot` 반환

의미:

- `RefreshSnapshotAsync`는 registry 상태를 다시 읽어 `RuntimeSnapshot` cache를 갱신하고 publish하는 명시적 API
- `GetSnapshotAsync`와 달리 side effect가 있는 refresh/publish API
- `PublishSnapshotAsync`보다 `RefreshSnapshotAsync` 이름이 caller 관점에서 더 자연스러운 후보

## 6. Failure Policy

결정:

- refresh 실패 시 기존 current snapshot 유지
- 예외는 caller에게 전파
- partial snapshot 생성은 제외
- degraded snapshot 생성은 제외
- channel별 failure aggregation은 제외
- `RuntimeHealthState` degraded policy는 후속 단계로 분리

이유:

- 기존 `SupervisorRuntimeSnapshotProvider.RefreshAsync` 실패 정책과 일관됨
- 실패 시 잘못된 snapshot으로 cache를 덮어쓰지 않기 위함

## 7. SnapshotChanged Policy

후속 구현 후보:

- `RefreshSnapshotAsync` 성공 시 `SnapshotChanged` 발생
- `RuntimeSnapshotChangedEventArgs.Snapshot`은 새로 publish된 snapshot
- `RuntimeSnapshotChangedEventArgs.OccurredAt`은 `snapshot.CapturedAt`
- `RuntimeSnapshotChangedEventArgs.Revision`은 기존 내부 revision counter 증가
- event invocation은 lock 밖에서 수행

정책:

- explicit `RefreshSnapshotAsync`가 성공하면 새 `capturedAt` 기반 snapshot으로 보고 publish
- latest-only 판단은 provider 쪽에도 있으므로 supervisor 내부에서는 단일 publish 흐름 유지

## 8. Thread-safety Policy

정책 후보:

- registry state 조회는 가능하면 lock 밖에서 수행
- current snapshot, revision, started 상태 갱신은 lock 안에서 수행
- event invocation은 lock 밖에서 수행
- `StartAsync`, `StopAsync`, `GetSnapshotAsync`와 동시 호출될 수 있음을 전제로 설계
- 기존 `InMemoryAutomationHubSupervisor` locking 정책과 일관성 유지

## 9. Caller Boundary

결정:

- AH-RUNTIME-14에서는 실제 호출 주체를 구현하지 않음
- 후속 호출 후보는 Runtime 내부 channel update API, polling scheduler, command dispatcher, 테스트 코드
- WPF가 `InMemoryAutomationHubSupervisor.RefreshSnapshotAsync`를 직접 호출하는 구조는 피함
- WPF는 기존 `SupervisorRuntimeSnapshotProvider` / lifecycle / `RuntimeDashboardAdapter` 경계를 통해 snapshot을 관찰해야 함

## 10. Boundary

이번 단계에서 유지된 경계:

- `IAutomationHubSupervisor` interface 변경 없음
- `InMemoryAutomationHubSupervisor` 코드 변경 없음
- Runtime project 코드 변경 없음
- WPF 변경 없음
- `App.xaml.cs` wiring 없음
- DI 변경 없음
- `DashboardViewModel` 변경 없음
- `RuntimeDashboardAdapter` 변경 없음
- Contracts / DTO 변경 없음
- channel update API 없음
- polling scheduler 없음
- command dispatcher 없음
- Runtime Event Bridge 없음
- telemetry 없음
- `XgtDriverCore` / `XgtChannelRunner` / FakePlc 참조 없음

## 11. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- 코드 수정
- 파일 생성/수정, Closeout 문서 제외
- 테스트 추가
- 명령 실행
- `IAutomationHubSupervisor` interface 변경
- `InMemoryAutomationHubSupervisor` implementation 변경
- channel update API
- polling scheduler 구현
- `XgtDriverCore` 참조 추가
- `XgtChannelRunner` 참조 추가
- FakePlc 참조 추가
- 실제 PLC 연결
- command dispatcher 구현
- Runtime Event Bridge 구현
- telemetry 구현
- partial snapshot policy
- degraded snapshot policy
- `RuntimeHealthState` aggregation 고도화
- WPF 변경
- `App.xaml.cs` wiring
- DI 변경
- `DashboardViewModel` 변경
- `RuntimeDashboardAdapter` 변경
- Contracts / DTO 변경

## 12. Validation

이번 단계는 계획 / Boundary Review 단계입니다.

검증 기준:

- 코드 수정 없음
- 파일 생성은 Closeout 문서만 허용
- `IAutomationHubSupervisor` contract 확장 없음
- `InMemoryAutomationHubSupervisor` implementation 변경 없음
- WPF 변경 없음
- explicit refresh/publish API 경계가 historical record에 남음
- 후속 구현 후보가 `InMemoryAutomationHubSupervisor` concrete `RefreshSnapshotAsync`로 제한됨

## 13. ACCEPT Decision

ACCEPT

이유:

- Refresh / Publish Snapshot API 필요성이 정리됨
- `IAutomationHubSupervisor` interface를 아직 확장하지 않기로 결정함
- concrete `InMemoryAutomationHubSupervisor` API 후보가 정리됨
- `GetSnapshotAsync` cache-only 정책이 재확인됨
- `SnapshotChanged` publish 정책이 정리됨
- 실패 시 cache 유지 / 예외 전파 정책이 정리됨
- thread-safety 정책이 정리됨
- WPF 직접 호출 금지 경계가 정리됨
- 후속 구현 지시문을 만들 수 있을 만큼 예상 파일, 테스트, 명령, 제외 범위가 정리됨

## 14. Risks / Follow-up Candidates

AH-RUNTIME-15 후보:

- `InMemoryAutomationHubSupervisor.RefreshSnapshotAsync` concrete API 구현
- refresh 성공 시 `RuntimeSnapshot` 반환
- refresh 성공 시 `SnapshotChanged` 발생
- refresh 실패 시 기존 cache 유지
- `GetSnapshotAsync` cache-only 유지 검증
- `IAutomationHubSupervisor` interface 미변경 검증

추가 후속 후보:

- channel update API boundary review
- polling scheduler publish path
- command dispatcher publish path
- `RuntimeHealthState` aggregation policy
- `ChannelRuntimeState` timestamp policy
- `XgtRuntimePlcChannelAdapter` boundary review

## 15. Next Step

다음 단계는 AH-RUNTIME-15: `InMemoryAutomationHubSupervisor` `RefreshSnapshotAsync` Concrete API 구현 계획입니다.

단, AH-RUNTIME-15에서도 `IAutomationHubSupervisor` interface 변경, channel update API, polling scheduler, command dispatcher, XGT/FakePlc integration은 제외하고 concrete API와 테스트까지만 진행하는 것이 안전합니다.
