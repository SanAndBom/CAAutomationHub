# AH-RUNTIME-19 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-19의 목표는 AH-RUNTIME-18에서 timestamp policy repair가 완료된 이후, InMemoryRuntimePlcChannel의 상태를 어떻게 갱신할지 concrete update API 경계를 검토하는 것입니다.

이번 단계는 실제 update API 구현, polling scheduler, XgtDriverCore 연결, XgtChannelRunner 연결, FakePlc integration을 수행하는 단계가 아니라, InMemoryRuntimePlcChannel에 어떤 형태의 concrete update API를 둘지 설계하는 Boundary Review입니다.

## 3. Scope

이번 단계에 포함된 검토 항목:
- IRuntimePlcChannel read-only 유지 여부 검토
- InMemoryRuntimePlcChannel concrete update API 위치 검토
- IWritableRuntimePlcChannel 도입 여부 검토
- UpdateState / ReplaceState API 형태 검토
- update 입력 모델 검토
- internal state model 필요성 검토
- timestamp 정책 검토
- update와 publish 분리 원칙 검토
- RuntimeChannelRegistry lookup/update API 보류 검토
- thread-safety 경계 검토

## 4. Decision

결정 사항:
- AH-RUNTIME-19는 계획 단계로 종료
- IRuntimePlcChannel은 read-only 계약으로 유지
- IAutomationHubSupervisor public contract는 확장하지 않음
- update API는 우선 InMemoryRuntimePlcChannel concrete API 후보로만 검토
- IWritableRuntimePlcChannel 또는 IUpdatableRuntimePlcChannel 도입은 보류
- RuntimeChannelRegistry lookup/update API는 AH-RUNTIME-19에서 추가하지 않음
- ChannelRuntimeState를 update 입력으로 직접 받는 방식은 지양
- Runtime 내부 전용 state model 후보를 검토
- update와 publish는 분리
- update만으로 SnapshotChanged를 발생시키지 않음
- channel update가 RefreshSnapshotAsync를 자동 호출하지 않음
- polling scheduler가 나중에 update -> refresh 순서의 orchestration 책임 후보가 됨

## 5. Update API Direction

정책:
- 전체 상태 교체 의미라면 ReplaceState(...)를 우선 후보로 검토
- 부분 갱신 의미가 필요하면 UpdateState(...)를 후보로 둠
- 현재 단계에서는 timestamp policy 혼선을 줄이기 위해 ReplaceState(...) 쪽이 더 명확해 보임
- InMemoryRuntimePlcChannel concrete API로 시작하는 것이 안전함
- IRuntimePlcChannel에 update API를 추가하지 않음

## 6. Input Model Direction

정책:
- ChannelRuntimeState를 update 입력으로 직접 받지 않음
- 이유는 capturedAt과 event timestamp 의미가 다시 섞일 위험이 있기 때문
- 후속 구현 후보로 InMemoryRuntimePlcChannelState 같은 Runtime 내부 전용 state model을 검토
- 이 state model은 Contracts DTO가 아니라 InMemoryRuntimePlcChannel의 내부 저장 상태를 표현하는 용도
- GetState(capturedAt)는 내부 state를 ChannelRuntimeState로 변환하는 역할을 유지

## 7. Timestamp Policy

정책:
- LastSuccessAt / LastFailureAt은 update API가 받은 event timestamp로 유지
- GetState(capturedAt)은 capturedAt을 LastSuccessAt / LastFailureAt에 대입하지 않음
- capturedAt은 update API 입력과 분리
- update 시점이 곧 통신 성공/실패 시점이라는 암묵 정책을 두지 않음
- AH-RUNTIME-17~18에서 분리한 timestamp 의미를 유지

## 8. Update / Publish Separation

정책:
- InMemoryRuntimePlcChannel.ReplaceState 또는 UpdateState는 SnapshotChanged를 발생시키지 않음
- channel update가 InMemoryAutomationHubSupervisor.RefreshSnapshotAsync를 자동 호출하지 않음
- update 후 publish는 caller가 명시적으로 RefreshSnapshotAsync를 호출하는 구조로 둠
- 향후 polling scheduler가 update -> refresh 순서의 orchestration 책임 후보가 됨
- channel event model / auto publish / coalescing 정책은 후속 단계로 분리

## 9. Thread-safety Direction

정책:
- InMemoryRuntimePlcChannel 내부 상태 read/write는 channel-level lock으로 보호하는 방향을 검토
- GetState(capturedAt)는 lock 안에서 내부 state를 안정적으로 읽고 DTO를 생성
- RuntimeChannelRegistry는 collection snapshot 보호에 집중
- RuntimeChannelRegistry는 channel mutation 책임을 갖지 않음
- registry lock과 channel lock의 중첩을 피하는 설계를 유지

## 10. RuntimeChannelRegistry Direction

정책:
- AH-RUNTIME-19에서는 RuntimeChannelRegistry lookup/update API를 추가하지 않음
- GetChannel / TryGetChannel / registry-level update API는 후속 Boundary Review 또는 AH-RUNTIME-20 이후 후보로 분리
- 이번 단계의 중심은 channel concrete update API 경계임

## 11. Expected Follow-up Direction

후속 구현 후보:
- InMemoryRuntimePlcChannel concrete ReplaceState(...) 또는 UpdateState(...)
- Runtime 내부 전용 state model
- IRuntimePlcChannel read-only 유지 검증
- update 후 GetState(capturedAt) 결과 검증
- update만으로 SnapshotChanged가 발생하지 않는 정책 검증
- 필요 시 RefreshSnapshotAsync 호출 후 snapshot 반영 검증

보류 후보:
- IWritableRuntimePlcChannel
- RuntimeChannelRegistry lookup API
- RuntimeChannelRegistry update API
- polling scheduler
- XgtRuntimePlcChannelAdapter
- FakePlc integration

## 12. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:
- 코드 수정
- 파일 생성/수정, Closeout 문서 제외
- 테스트 추가
- 명령 실행
- Contracts / DTO 변경
- IRuntimePlcChannel 변경
- IAutomationHubSupervisor 변경
- IWritableRuntimePlcChannel 추가
- RuntimeChannelRegistry lookup/update API
- polling scheduler
- driver adapter
- FakePlc integration
- WPF 변경
- DI 변경
- Dashboard 변경
- auto publish
- Runtime Event Bridge
- telemetry 구현
- command dispatcher 구현

## 13. Validation

이번 단계는 계획 / Boundary Review 단계입니다.

검증 기준:
- 코드 수정 없음
- 파일 생성은 Closeout 문서만 허용
- IRuntimePlcChannel read-only 유지
- IAutomationHubSupervisor public contract 미확장
- update와 publish 분리 정책이 historical record에 남음
- ChannelRuntimeState를 update input으로 직접 쓰지 않는 결정이 기록됨
- 후속 구현 후보가 InMemoryRuntimePlcChannel concrete API로 제한됨

## 14. ACCEPT Decision

ACCEPT

이유:
- update API 위치가 정리됨
- IRuntimePlcChannel read-only 유지가 재확인됨
- concrete InMemoryRuntimePlcChannel update 후보가 정리됨
- internal state model 필요성이 정리됨
- UpdateState / ReplaceState 후보가 정리됨
- update와 publish 분리 원칙이 재확인됨
- registry lookup/update API 제외가 정리됨
- 후속 구현 지시문을 만들 수 있을 만큼 예상 파일, 테스트, 명령, 제외 범위가 정리됨

## 15. Risks / Follow-up Candidates

AH-RUNTIME-20 후보:
- InMemoryRuntimePlcChannel concrete ReplaceState API 구현
- InMemoryRuntimePlcChannelState internal model
- update 후 GetState(capturedAt) 반영 테스트
- update와 publish 분리 테스트
- RefreshSnapshotAsync 호출 후 snapshot 반영 테스트

추가 후속 후보:
- IWritableRuntimePlcChannel 필요성 재검토
- RuntimeChannelRegistry lookup API
- PollingScheduler publish path
- XgtRuntimePlcChannelAdapter boundary review
- FakePlc integration boundary review

## 16. Next Step

다음 단계는 AH-RUNTIME-20: InMemoryRuntimePlcChannel Concrete ReplaceState API 구현 계획입니다.

단, AH-RUNTIME-20에서도 IRuntimePlcChannel read-only 계약은 유지하고, update API는 우선 InMemoryRuntimePlcChannel concrete 수준에서 작게 시작하는 것이 안전합니다.
