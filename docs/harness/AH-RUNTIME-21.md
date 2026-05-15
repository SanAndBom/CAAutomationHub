# AH-RUNTIME-21 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-21의 목표는 AH-RUNTIME-20에서 `InMemoryRuntimePlcChannel.ReplaceState(...)` concrete API가 추가된 이후, 외부 orchestration이 특정 `PlcId`의 runtime channel을 어떻게 찾을지 `RuntimeChannelRegistry` lookup 경계를 검토하는 것입니다.

이번 단계는 `RuntimeChannelRegistry` lookup API를 바로 구현하는 단계가 아니라, `TryGetChannel` / `GetChannel` / `Contains` 같은 lookup API의 필요성과 범위를 설계하는 Boundary Review입니다.

## 3. Scope

이번 단계에 포함된 검토 항목:

- `RuntimeChannelRegistry` lookup API 필요성 검토
- `TryGetChannel` 후보 검토
- `GetChannel` convenience API 보류 검토
- `Contains` API 보류 검토
- `plcId` validation 정책 검토
- lookup 반환 타입 검토
- thread-safety 경계 검토
- collection 구조 영향 검토
- polling scheduler와의 후속 관계 검토
- update/publish 책임 분리 재확인

## 4. Decision

결정 사항:

- AH-RUNTIME-21은 계획 단계로 종료
- `RuntimeChannelRegistry`에는 lookup API가 필요함
- 우선 후보는 `bool TryGetChannel(string plcId, out IRuntimePlcChannel channel)`
- lookup API는 찾기 책임까지만 가짐
- `RuntimeChannelRegistry`는 update 책임을 갖지 않음
- `RuntimeChannelRegistry`는 publish 책임을 갖지 않음
- `GetChannel(plcId)`는 후속 convenience API 후보로 보류
- `Contains(plcId)`는 보류
- lookup API 반환 타입은 `IRuntimePlcChannel`
- concrete `InMemoryRuntimePlcChannel` 반환은 금지
- writable/update 문제는 AH-RUNTIME-22 이후 `IWritableRuntimePlcChannel` 또는 adapter 경계로 분리
- `PollingScheduler` / Driver / `FakePlc` / WPF wiring은 AH-RUNTIME-21에서 제외

## 5. Lookup API Direction

우선 후보:

- `bool TryGetChannel(string plcId, out IRuntimePlcChannel channel)`

이유:

- missing channel을 예외가 아닌 `false`로 처리 가능
- scheduler 흐름에 적합
- duplicate `PlcId` 정책과 동일한 비교 기준을 registry 내부에서 적용 가능
- caller가 `GetChannels()` 후 직접 검색하는 방식보다 identity 정책 누수가 적음
- registry가 concrete update API를 알 필요가 없음

보류 후보:

- `GetChannel(plcId)`
- `Contains(plcId)`

## 6. Validation Policy

후속 구현 시 권장 validation:

- `plcId == null` -> `ArgumentNullException`
- `plcId == ""` 또는 whitespace -> `ArgumentException`
- missing but valid `plcId` -> `false`
- existing `plcId` -> `true` 및 channel reference 반환
- 비교 기준은 duplicate detection과 동일하게 유지
- 현재 전제상 `StringComparer.Ordinal` 계열이 자연스러움
- case-sensitive 여부는 기존 duplicate 정책 확인 후 고정

## 7. Return Type Policy

정책:

- lookup API는 `IRuntimePlcChannel`을 반환
- registry가 concrete `InMemoryRuntimePlcChannel`을 반환하지 않음
- registry는 concrete update API를 알지 않음
- caller가 update 가능성을 다루는 문제는 후속 writable/update boundary에서 검토
- AH-RUNTIME-21에서는 `IWritableRuntimePlcChannel`을 추가하지 않음

## 8. Thread-safety Policy

정책:

- `TryGetChannel`은 registry 내부 lock 안에서 collection을 조회하고 channel reference만 반환
- 반환된 channel의 내부 상태 동기화는 channel 책임
- registry lock을 잡은 채 `channel.GetState(...)`를 호출하지 않는 기존 방향 유지
- `GetChannels()`의 snapshot copy 정책은 그대로 유지
- collection 구조는 후속 구현 직전 확인 필요

## 9. RuntimeChannelRegistry Responsibility

정책:

- `RuntimeChannelRegistry`는 channel 목록 보관, 중복 검증, snapshot-safe read, lookup 책임까지만 가짐
- update 책임 없음
- publish 책임 없음
- supervisor를 알지 않음
- `InMemoryRuntimePlcChannel` concrete API를 알지 않음
- polling scheduler 구현 없음

## 10. Future Scheduler Flow

후속 후보 흐름:

- `PollingScheduler`
- `RuntimeChannelRegistry.TryGetChannel(plcId, out channel)`
- writable 경계 확인 또는 adapter 사용
- channel state update
- `InMemoryAutomationHubSupervisor.RefreshSnapshotAsync(...)`

주의:

- AH-RUNTIME-21에서는 `PollingScheduler`를 구현하지 않음
- writable interface도 구현하지 않음
- driver adapter도 구현하지 않음

## 11. Expected Follow-up Direction

후속 구현 후보:

- `RuntimeChannelRegistry.TryGetChannel(...)` 추가
- validation 정책 반영
- duplicate 비교 기준과 lookup 비교 기준 일치
- thread-safety 유지
- Runtime 단위 테스트 추가
- `IRuntimePlcChannel` / `IAutomationHubSupervisor` / Contracts / WPF 변경 없음

후속 추가 후보:

- `IWritableRuntimePlcChannel` 필요성 재검토
- `RuntimeChannelRegistry` lookup API 구현 후 polling scheduler boundary review
- `XgtRuntimePlcChannelAdapter` boundary review

## 12. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- 코드 수정
- 파일 생성/수정, Closeout 문서 제외
- 테스트 추가
- 명령 실행
- `IRuntimePlcChannel` 변경
- `IAutomationHubSupervisor` 변경
- `InMemoryRuntimePlcChannel` 변경
- `IWritableRuntimePlcChannel` 추가
- `RuntimeChannelRegistry` update API
- `GetChannel` convenience API 구현
- `Contains` API 구현
- polling scheduler 구현
- `XgtDriverCore` 참조 추가
- `XgtChannelRunner` 참조 추가
- `FakePlc` 참조 추가
- command dispatcher 구현
- Runtime Event Bridge 구현
- telemetry 구현
- WPF 변경
- `App.xaml.cs` wiring
- DI 변경
- `DashboardViewModel` 변경
- `RuntimeDashboardAdapter` 변경

## 13. Validation

이번 단계는 계획 / Boundary Review 단계입니다.

검증 기준:

- 코드 수정 없음
- 파일 생성은 Closeout 문서만 허용
- `RuntimeChannelRegistry` lookup 필요성이 historical record에 남음
- `TryGetChannel` 우선 후보가 기록됨
- update/publish 책임 제외가 기록됨
- concrete channel 반환 금지와 `IRuntimePlcChannel` 반환 정책이 기록됨
- 후속 구현 후보가 `RuntimeChannelRegistry.TryGetChannel`으로 제한됨

## 14. ACCEPT Decision

ACCEPT

이유:

- lookup API 필요성이 정리됨
- `TryGetChannel` 우선 후보가 정리됨
- `GetChannel` / `Contains` 보류가 정리됨
- `plcId` validation 정책이 정리됨
- return type이 `IRuntimePlcChannel`이어야 함이 정리됨
- registry update 책임 제외가 재확인됨
- 후속 구현 지시문을 만들 수 있을 만큼 예상 파일, 테스트, 명령, 제외 범위가 정리됨

## 15. Risks / Follow-up Candidates

AH-RUNTIME-22 후보:

- `RuntimeChannelRegistry.TryGetChannel` implementation
- `RuntimeChannelRegistry` lookup validation tests
- duplicate comparison / lookup comparison 일치 검증
- `GetChannels` / `GetStates` 기존 동작 유지 검증

추가 후속 후보:

- `IWritableRuntimePlcChannel` boundary review
- `PollingScheduler` publish path
- `XgtRuntimePlcChannelAdapter` boundary review
- `FakePlc` integration boundary review

## 16. Next Step

다음 단계는 AH-RUNTIME-22: `RuntimeChannelRegistry TryGetChannel Implementation`입니다.

단, AH-RUNTIME-22에서도 update API, writable interface, polling scheduler, driver adapter, `FakePlc` integration은 제외하고, lookup API와 테스트까지만 작게 진행하는 것이 안전합니다.
