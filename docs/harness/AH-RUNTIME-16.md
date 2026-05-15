# AH-RUNTIME-16 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-16의 목표는 AH-RUNTIME-15에서 `InMemoryAutomationHubSupervisor.RefreshSnapshotAsync(CancellationToken)` concrete API가 추가된 이후, Runtime channel 상태를 어떻게 변경하고 그 변경을 snapshot publish 흐름으로 연결할지 검토하는 것입니다.

이번 단계는 실제 channel update API, polling scheduler, XgtDriverCore 연결, XgtChannelRunner 연결, FakePlc integration을 구현하는 단계가 아니라, Runtime channel 상태 변경 경계를 어떻게 둘지 설계하는 Boundary Review입니다.

## 3. Scope

이번 단계에 포함된 검토 항목:

- channel update API 위치 검토
- `IRuntimePlcChannel` read-only 유지 여부 검토
- `InMemoryRuntimePlcChannel` concrete update API 후보 검토
- `IWritableRuntimePlcChannel` / `IUpdatableRuntimePlcChannel` 후보 검토
- `RuntimeChannelRegistry` update 책임 검토
- update와 publish 책임 분리 검토
- timestamp policy 선결 필요성 검토
- thread-safety 경계 검토
- polling scheduler / driver adapter / FakePlc integration 제외 결정

## 4. Decision

결정 사항:

- AH-RUNTIME-16은 계획 단계로 종료
- `IAutomationHubSupervisor` public contract는 확장하지 않음
- `InMemoryAutomationHubSupervisor.RefreshSnapshotAsync(CancellationToken)`는 concrete API로 유지
- `IRuntimePlcChannel`은 우선 read-only state provider로 유지
- `IRuntimePlcChannel`에 update API를 추가하지 않음
- update API는 우선 `InMemoryRuntimePlcChannel` concrete 후보로 검토
- `IWritableRuntimePlcChannel` 또는 `IUpdatableRuntimePlcChannel`은 보류 후보로 유지
- `RuntimeChannelRegistry`가 직접 update와 publish를 담당하는 구조는 비추천
- update와 publish는 분리
- channel update가 자동으로 `RefreshSnapshotAsync`를 호출하지 않음
- channel event model / auto publish / coalescing 정책은 후속 단계로 분리
- Runtime 프로젝트는 계속 `CAAutomationHub.Contracts`만 참조해야 함

## 5. Update API Direction

정책:

- 1순위 후보는 `InMemoryRuntimePlcChannel` concrete에 `ReplaceState` 또는 `UpdateState` 계열 API를 추가하는 것
- `IRuntimePlcChannel`은 read-only 계약으로 유지
- 실제 driver adapter에 update API를 강제하지 않음
- registry-level `UpdateChannelState(...)`는 초기 구현 후보에서 낮은 우선순위로 둠
- registry는 channel 목록 보관과 state read 경계를 유지

보류 후보:

- `IWritableRuntimePlcChannel`
- `IUpdatableRuntimePlcChannel`
- `RuntimeChannelRegistry.TryGetChannel(plcId, out IRuntimePlcChannel channel)`

## 6. Timestamp Policy Risk

중요 기록:

- `capturedAt`은 snapshot 수집 시각임
- `LastSuccessAt` / `LastFailureAt`은 실제 통신 성공/실패 시각이어야 함
- AH-RUNTIME-12~15의 skeleton에서는 `ChannelRuntimeState`에 `CapturedAt` 전용 필드가 없어 `capturedAt`을 기존 timestamp 필드에 반영하는 임시 정책을 사용함
- channel update API가 들어오면 이 임시 정책의 리스크가 커짐
- 따라서 AH-RUNTIME-17에서 timestamp policy review를 먼저 수행하는 것을 강하게 권장함
- AH-RUNTIME-16에서는 Contracts 변경을 하지 않음

## 7. Thread-safety Direction

정책:

- channel 내부 상태 변경과 `GetState(capturedAt)`는 동시에 호출될 수 있음
- channel-level lock 또는 immutable snapshot replacement가 필요함
- registry는 channel collection snapshot만 보호함
- registry lock을 잡은 상태에서 channel state를 읽지 않는 방향을 유지함
- registry lock과 channel lock의 중첩을 피하는 설계를 유지함

## 8. Update / Publish Separation

정책:

- channel update는 상태 변경만 담당
- `supervisor.RefreshSnapshotAsync`는 별도 호출
- polling scheduler 또는 caller가 나중에 channel update -> `supervisor.RefreshSnapshotAsync` 순서를 명시적으로 수행
- registry가 supervisor를 알게 하지 않음
- channel이 supervisor를 알게 하지 않음
- auto publish는 후속 단계로 분리

## 9. RuntimeChannelRegistry Responsibility

정책:

- `RuntimeChannelRegistry`는 channel 목록 보관과 snapshot-safe read 경계로 유지
- registry-level update는 초기 후보에서 낮은 우선순위
- `TryGetChannel` / `GetChannel` / `Contains` 같은 lookup API는 후속 후보로 검토
- Remove / Update / Replace command flow는 아직 도입하지 않음

## 10. Expected Follow-up Direction

권장 후속 흐름:

- AH-RUNTIME-17: `ChannelRuntimeState` Timestamp Policy Boundary Review
- AH-RUNTIME-18 후보: `InMemoryRuntimePlcChannel` concrete update API
- 이후 후보: `IWritableRuntimePlcChannel` 필요성 재검토
- 이후 후보: PollingScheduler publish path
- 이후 후보: `XgtRuntimePlcChannelAdapter` boundary review

## 11. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- 코드 수정
- 파일 생성/수정, Closeout 문서 제외
- 테스트 추가
- 명령 실행
- `IAutomationHubSupervisor` interface 변경
- `IRuntimePlcChannel` interface 변경
- `InMemoryRuntimePlcChannel` update API 구현
- `RuntimeChannelRegistry` update API 구현
- supervisor auto publish
- channel event model
- polling scheduler
- XgtDriverCore 참조 추가
- XgtChannelRunner 참조 추가
- FakePlc 참조 추가
- command dispatcher 구현
- Runtime Event Bridge 구현
- telemetry 구현
- WPF 변경
- App.xaml.cs wiring
- DI 변경
- DashboardViewModel 변경
- RuntimeDashboardAdapter 변경
- auto `RefreshSnapshotAsync` 호출
- Contracts 변경

## 12. Validation

이번 단계는 계획 / Boundary Review 단계입니다.

검증 기준:

- 코드 수정 없음
- 파일 생성은 Closeout 문서만 허용
- `IAutomationHubSupervisor` contract 확장 없음
- `IRuntimePlcChannel` read-only 유지
- update와 publish 분리 정책이 historical record에 남음
- timestamp policy 리스크가 historical record에 남음
- 후속 AH-RUNTIME-17 후보가 timestamp policy review로 분리됨

## 13. ACCEPT Decision

ACCEPT

이유:

- channel update API 위치 후보가 정리됨
- `IRuntimePlcChannel` read-only 유지 원칙이 정리됨
- `InMemoryRuntimePlcChannel` concrete update 후보가 정리됨
- update와 publish 분리 원칙이 정리됨
- registry update 책임 보류가 정리됨
- timestamp policy 선결 필요성이 정리됨
- scheduler / driver / FakePlc / WPF wiring 제외가 명확해짐
- 후속 구현 전 timestamp policy를 먼저 다룰 필요성이 명확해짐

## 14. Risks / Follow-up Candidates

AH-RUNTIME-17 후보:

- `ChannelRuntimeState` Timestamp Policy Boundary Review
- `capturedAt`과 `LastSuccessAt` / `LastFailureAt` 의미 분리
- `ChannelRuntimeState`에 `CapturedAt` 전용 필드 필요 여부 검토
- Contracts 변경 여부 검토
- update API 이전 timestamp 기준 확정

추가 후속 후보:

- `InMemoryRuntimePlcChannel` concrete update API
- `IWritableRuntimePlcChannel` 필요성 검토
- `RuntimeChannelRegistry` lookup API
- PollingScheduler publish path
- `XgtRuntimePlcChannelAdapter` boundary review

## 15. Next Step

다음 단계는 AH-RUNTIME-17: `ChannelRuntimeState` Timestamp Policy Boundary Review입니다.

단, AH-RUNTIME-17에서도 바로 Contracts 변경을 하기보다, `capturedAt`과 `LastSuccessAt` / `LastFailureAt` 의미를 먼저 분리하고, Contracts 변경이 필요한지 검토하는 계획 단계로 진행하는 것이 안전합니다.
