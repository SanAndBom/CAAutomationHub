# AH-RUNTIME-17 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-17의 목표는 `ChannelRuntimeState`의 timestamp 의미를 확정하는 것입니다.

이번 단계는 Contracts나 DTO를 바로 수정하는 구현 단계가 아니라, `capturedAt`, `LastSuccessAt`, `LastFailureAt`의 책임을 분리하는 Boundary Review입니다.

AH-RUNTIME-16에서 정리한 channel update API 이전 선결 과제로 다룹니다.

## 3. Scope

이번 단계에 포함된 검토 항목:

- `capturedAt` 의미 정의
- `LastSuccessAt` 의미 정의
- `LastFailureAt` 의미 정의
- AH-RUNTIME-12~15의 임시 timestamp 정책 리스크 정리
- `ChannelRuntimeState.CapturedAt` 추가 후보 검토
- Contracts 변경 없는 정책 후보 검토
- Runtime internal metadata 후보 검토
- `InMemoryRuntimePlcChannel` 후속 정책 검토
- update API와 timestamp 관계 검토
- WPF mapper 영향 검토 계획 정리

## 4. Decision

결정 사항:

- AH-RUNTIME-17은 계획 단계로 종료
- `capturedAt`은 snapshot 생성을 위해 Runtime이 channel 상태를 관찰한 시각으로 정의
- `LastSuccessAt`은 실제 PLC 통신 성공 이벤트가 발생한 시각으로 정의
- `LastFailureAt`은 실제 PLC 통신 실패 이벤트가 발생한 시각으로 정의
- `capturedAt`을 `LastSuccessAt` / `LastFailureAt`에 대입하지 않는 방향을 기본 원칙으로 함
- AH-RUNTIME-12~15에서 사용한 임시 정책은 후속 구현에서 제거 또는 대체 대상으로 분류
- Contracts 변경은 AH-RUNTIME-17에서 하지 않음
- `ChannelRuntimeState.CapturedAt` 추가는 후속 재검토 후보로 둠
- 1차 후속 구현 후보는 Contracts 변경 없이 기존 임시 정책을 제거하는 C안
- channel-level `CapturedAt` 필드 추가는 WPF mapper와 DTO 독립 사용 필요성이 확인된 뒤 결정

## 5. Timestamp Definitions

정의:

- `capturedAt`: `RuntimeSnapshot`을 만들기 위해 channel 상태를 읽은 snapshot 관찰 시각
- `LastSuccessAt`: 마지막 통신 성공이 실제로 발생한 시각
- `LastFailureAt`: 마지막 통신 실패가 실제로 발생한 시각

정책:

- `capturedAt`은 `RuntimeSnapshot.CapturedAt` / `RuntimeHealthState.CapturedAt`의 기준으로 유지
- `LastSuccessAt` / `LastFailureAt`은 실제 통신 event timestamp로 유지
- snapshot 관찰 시각과 통신 이벤트 시각을 섞지 않음

## 6. Temporary Policy Risk

중요 기록:

- AH-RUNTIME-12~15 skeleton에서는 `ChannelRuntimeState`에 channel-level `CapturedAt` 전용 필드가 없어 `capturedAt`을 `LastSuccessAt` / 조건부 `LastFailureAt`에 반영했음
- 이 정책은 snapshot 관찰 시각과 실제 통신 이벤트 시각을 섞는 임시 정책임
- polling scheduler와 update API가 들어오면 UI, 테스트, telemetry에서 "마지막 통신 성공 시각"을 잘못 해석할 위험이 있음
- 따라서 기존 임시 정책은 후속 구현에서 제거 또는 대체해야 함

## 7. Contracts Change Candidates

후보 A:

- `ChannelRuntimeState.CapturedAt` 추가
- 장점: channel DTO 단독으로 생성/관찰 시각을 알 수 있음
- 단점: Contracts, Runtime tests, WPF mapper 영향이 생길 수 있음

후보 B:

- Contracts 변경 없음
- `RuntimeSnapshot.CapturedAt`을 snapshot frame timestamp로 사용
- `LastSuccessAt` / `LastFailureAt`은 event timestamp로 유지
- 장점: ripple이 작음
- 단점: `ChannelRuntimeState`만 독립적으로 볼 때 관찰 시각을 알 수 없음

후보 C:

- Runtime internal metadata로 `capturedAt` 유지
- public DTO 변경을 피할 수 있으나 구조가 복잡해질 수 있음

권장:

- AH-RUNTIME-17에서는 Contracts 변경을 성급히 결정하지 않음
- 1차 후보는 B안, 즉 Contracts 변경 없이 `RuntimeSnapshot.CapturedAt`을 snapshot frame timestamp로 유지하는 방향
- 단, WPF mapper나 dashboard가 channel DTO를 독립적으로 소비해 stale/order 판단을 해야 한다면 A안 필요성을 재검토

## 8. InMemoryRuntimePlcChannel Direction

후속 C안 후보:

- `GetState(capturedAt)`가 `LastSuccessAt` / `LastFailureAt`을 덮어쓰지 않음
- `capturedAt`은 `RuntimeSnapshot.CapturedAt` / `RuntimeHealthState.CapturedAt` frame timestamp로만 유지
- `LastSuccessAt` / `LastFailureAt`은 constructor 또는 향후 update API로 설정된 internal event timestamp를 그대로 반영
- 기존 임시 정책 유지는 장기적으로 비추천

## 9. Update API Relationship

정책:

- 향후 update API는 polling success/failure의 실제 발생 시각을 `LastSuccessAt` / `LastFailureAt`에 반영할 수 있음
- `RefreshSnapshotAsync`는 snapshot frame의 `capturedAt`을 생성함
- update timestamp와 publish timestamp는 서로 다를 수 있음
- update와 publish 분리 원칙은 AH-RUNTIME-16 결정 그대로 유지함

## 10. WPF Mapper Review Direction

후속 확인 필요:

- `RuntimeDashboardSnapshotMapper`가 `LastSuccessAt` / `LastFailureAt`을 어떻게 사용하는지 확인
- WPF `PlcCardSnapshot`이 last seen / last updated / stale 판단을 어디서 가져오는지 확인
- UI가 `LastSuccessAt`을 "마지막 통신 성공"으로 표시한다면 `capturedAt`을 넣으면 안 됨
- stale / ordering 판단은 가능하면 `RuntimeSnapshot.CapturedAt` 기준으로 유지

## 11. Recommended Follow-up Direction

후속 후보:

- Contracts 변경 없는 C안 구현
- `InMemoryRuntimePlcChannel.GetState(capturedAt)`가 `LastSuccessAt`을 `capturedAt`으로 덮어쓰지 않도록 수정
- `LastSuccessAt` / `LastFailureAt`은 internal event timestamp로 유지
- `RuntimeSnapshot.CapturedAt` / `RuntimeHealthState.CapturedAt` 일치 정책은 유지

보류 후보:

- `ChannelRuntimeState.CapturedAt` 추가
- Contracts 변경
- WPF mapper 변경
- `RuntimeDashboardSnapshotMapper` 변경

## 12. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- 코드 수정
- 파일 생성/수정, Closeout 문서 제외
- 테스트 추가
- 명령 실행
- Contracts 변경
- `ChannelRuntimeState` 변경
- WPF mapper 변경
- `RuntimeDashboardSnapshotMapper` 변경
- update API 구현
- polling scheduler 구현
- XgtDriverCore 참조 추가
- XgtChannelRunner 참조 추가
- FakePlc 참조 추가
- telemetry 구현
- command dispatcher 구현
- UI 변경

## 13. Validation

이번 단계는 계획 / Boundary Review 단계입니다.

검증 기준:

- 코드 수정 없음
- 파일 생성은 Closeout 문서만 허용
- Contracts 변경 없음
- `ChannelRuntimeState` 변경 없음
- WPF mapper 변경 없음
- `capturedAt` / `LastSuccessAt` / `LastFailureAt` 의미 분리 정책이 historical record에 남음
- 기존 임시 timestamp 정책의 리스크가 historical record에 남음
- 후속 구현 후보가 Contracts 변경 없는 C안으로 정리됨

## 14. ACCEPT Decision

ACCEPT

이유:

- `capturedAt` 의미가 정리됨
- `LastSuccessAt` / `LastFailureAt` 의미가 정리됨
- 기존 임시 정책의 리스크가 정리됨
- Contracts 변경 여부 후보가 정리됨
- 후속 1차 구현 후보가 Contracts 변경 없는 C안으로 정리됨
- update API 전 timestamp policy 선결 필요성이 historical record에 남음
- 후속 구현 지시문을 만들 수 있을 만큼 예상 파일, 테스트, 명령, 제외 범위가 정리됨

## 15. Risks / Follow-up Candidates

AH-RUNTIME-18 후보:

- Contracts 변경 없이 `InMemoryRuntimePlcChannel` timestamp 임시 정책 제거
- `GetState(capturedAt)`가 `LastSuccessAt` / `LastFailureAt`을 덮어쓰지 않도록 수정
- Runtime channel tests 보강
- `RuntimeSnapshot.CapturedAt` / `RuntimeHealthState.CapturedAt` 일치 정책 유지

추가 후속 후보:

- `ChannelRuntimeState.CapturedAt` 추가 필요성 재검토
- `RuntimeDashboardSnapshotMapper` timestamp usage review
- WPF last seen / stale 표시 정책 검토
- update API 구현
- polling scheduler skeleton
- `XgtRuntimePlcChannelAdapter` boundary review

## 16. Next Step

다음 단계는 AH-RUNTIME-18: `InMemoryRuntimePlcChannel` Timestamp Policy Repair입니다.

단, AH-RUNTIME-18에서도 Contracts 변경 없이 기존 임시 정책을 제거하는 C안부터 작게 진행하는 것이 안전합니다.
