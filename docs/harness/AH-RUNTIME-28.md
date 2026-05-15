# AH-RUNTIME-28 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-28의 목표는 AH-RUNTIME-27에서 PollingPublishCoordinator가 추가된 이후, 외부 polling 결과를 RuntimePlcChannelState로 어떻게 변환할지 mapper 경계를 검토하는 것입니다.

이번 단계는 XgtDriverCore 연결, XgtChannelRunner 연결, FakePlc integration, 실제 polling scheduler 구현을 수행하는 단계가 아니라, polling result 또는 driver result가 RuntimePlcChannelState로 변환되는 책임 경계를 설계하는 Boundary Review입니다.

## 3. Scope

이번 단계에 포함된 검토 항목:

- polling result model 필요성 검토
- ChannelPollingResult 후보 검토
- ChannelPollingFailureKind 후보 검토
- RuntimePlcChannelStateMapper 후보 검토
- vendor-neutral / vendor-specific 책임 경계 검토
- success mapping policy 검토
- failure mapping policy 검토
- previous RuntimePlcChannelState 필요성 검토
- RuntimePlcChannelState readback boundary 필요성 검토
- telemetry 경계 검토

## 4. Decision

결정 사항:

- AH-RUNTIME-28은 계획 단계로 종료
- PollingChannelUpdate는 이미 RuntimePlcChannelState가 만들어진 뒤의 publish 입력임
- PollingChannelUpdate 앞단에 vendor-neutral polling result model이 필요함
- Runtime 내부에는 vendor-neutral ChannelPollingResult 계열 모델과 mapper만 둘 수 있음
- Runtime은 XgtDriverCore / XgtChannelRunner / FakePlc를 직접 알면 안 됨
- vendor-specific response classification은 Runtime 밖에서 vendor-neutral polling result로 변환해야 함
- RuntimePlcChannelState 생성 책임은 source마다 흩어지게 두기보다 Runtime의 vendor-neutral mapper 후보로 모으는 방향이 안전함
- mapper가 누적 필드를 정확히 계산하려면 previous RuntimePlcChannelState가 필요함
- 따라서 state readback boundary가 선결 검토 항목임
- telemetry aggregation은 이번 단계에서 제외함

## 5. Vendor-neutral Polling Result Direction

후속 후보:

- ChannelPollingResult
- ChannelPollingFailureKind
- RuntimePlcChannelStateMapper

ChannelPollingResult 후보 필드:

- string PlcId
- bool IsSuccess
- DateTimeOffset OccurredAt
- TimeSpan? RoundTripTime
- ChannelPollingFailureKind? FailureKind
- string? ErrorMessage

ChannelPollingFailureKind 후보:

- Timeout
- Disconnected
- ProtocolError
- Unknown

정책:

- FailureKind는 vendor-neutral enum으로 둠
- 기존 외부 transport enum을 Runtime에서 직접 참조하지 않음
- XGT-specific result / error classification은 Runtime 밖에서 처리

## 6. Mapping Policy Candidates

성공 result 후보:

- LinkState: connected / online 계열
- HealthSeverity: normal 계열
- PollingState: active / running 계열
- LastSuccessAt: OccurredAt
- LastFailureAt: previous 유지
- LastErrorMessage: null
- RoundTripTime: result 값 반영
- ConsecutiveFailures: 0
- ReconnectAttemptCount: previous 유지

실패 result 후보:

- LastFailureAt: OccurredAt
- LastErrorMessage: ErrorMessage
- LastSuccessAt: previous 유지
- ConsecutiveFailures: previous + 1
- RoundTripTime: null 또는 previous 유지 정책 검토 필요
- PollingState: failed / degraded 계열
- HealthSeverity: failure kind에 따라 warning / error 후보
- LinkState: failure kind에 따라 유지 또는 disconnected

실패 종류별 후보:

- Timeout: link는 즉시 disconnected로 단정하지 않고 polling failure / degraded 우선
- Disconnected: link disconnected 계열
- ProtocolError: link는 살아 있을 수 있으나 channel unhealthy / error
- Unknown: conservative degraded / error

주의:

- 정확한 enum 값은 후속 승인 후 Contracts 기준으로 확인

## 7. Previous State Requirement

결정:

- RuntimePlcChannelState에는 누적 상태가 포함되어 있으므로 stateless mapper는 정보 손실 위험이 있음
- mapper는 previous RuntimePlcChannelState를 받는 방향이 자연스러움

후보 signature:

- RuntimePlcChannelState Map(RuntimePlcChannelState previous, ChannelPollingResult result)
- RuntimePlcChannelState MapSuccess(RuntimePlcChannelState previous, ChannelPollingResult result)
- RuntimePlcChannelState MapFailure(RuntimePlcChannelState previous, ChannelPollingResult result)

선결 문제:

- 현재 writable channel 경계는 ReplaceState 중심임
- internal RuntimePlcChannelState readback 경계가 명확하지 않음
- mapper 구현 전에 previous state readback boundary를 먼저 검토해야 함

## 8. State Readback Boundary Direction

후속 검토 후보:

- IWritableRuntimePlcChannel 쪽 readback 확장 여부
- 별도 internal state reader interface 추가 여부
- concrete InMemoryRuntimePlcChannel에만 read API를 둘지 여부
- mapper는 previous state를 외부에서 받게 하고 channel은 관여하지 않을지 여부

정책:

- AH-RUNTIME-28에서는 readback API를 구현하지 않음
- AH-RUNTIME-29 후보로 RuntimePlcChannelState Readback Boundary Review를 분리함

## 9. Telemetry Boundary

결정:

- AH-RUNTIME-28에서는 telemetry aggregation으로 확장하지 않음
- RuntimePlcChannelState의 existing fields에 들어갈 수 있는 최소값만 후속 mapper 후보로 다룸
- 장기 telemetry model은 별도 후속 과제로 분리함

## 10. Expected Follow-up Direction

추천 다음 단계:

- AH-RUNTIME-29: RuntimePlcChannelState Readback Boundary Review

그 이후 후보:

- ChannelPollingResult / ChannelPollingFailureKind skeleton
- RuntimePlcChannelStateMapper skeleton
- polling result -> RuntimePlcChannelState mapping tests
- Xgt-specific result -> ChannelPollingResult adapter boundary review

## 11. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- 코드 수정
- 파일 생성/수정, Closeout 문서 제외
- 테스트 추가
- 명령 실행
- XgtDriverCore 참조 추가
- XgtChannelRunner 참조 추가
- FakePlc 참조 추가
- actual polling read
- PollingScheduler timer / loop
- Runtime Event Bridge
- telemetry aggregation
- command dispatcher
- WPF 변경
- DI / App wiring
- Contracts / DTO 변경
- vendor-specific mapper implementation
- ChannelPollingResult 구현
- RuntimePlcChannelStateMapper 구현
- RuntimePlcChannelState readback API 구현

## 12. Validation

이번 단계는 계획 / Boundary Review 단계입니다.

검증 기준:

- 코드 수정 없음
- 파일 생성은 Closeout 문서만 허용
- vendor-neutral / vendor-specific mapping 경계가 historical record에 남음
- previous state 필요성이 historical record에 남음
- RuntimePlcChannelState readback boundary 필요성이 historical record에 남음
- 후속 구현 전에 readback boundary를 먼저 검토하는 방향이 기록됨

## 13. ACCEPT Decision

ACCEPT

이유:

- polling result model 필요성이 정리됨
- mapper 위치가 정리됨
- vendor-neutral vs XGT-specific mapping 경계가 정리됨
- success / failure mapping policy 후보가 정리됨
- previous state 필요성이 정리됨
- RuntimePlcChannelState readback 필요성이 정리됨
- 후속 구현 또는 선결 boundary 후보가 정리됨

## 14. Risks / Follow-up Candidates

AH-RUNTIME-29 후보:

- RuntimePlcChannelState Readback Boundary Review
- IWritableRuntimePlcChannel readback 확장 여부
- 별도 state reader interface 후보
- InMemoryRuntimePlcChannel concrete state read API 후보
- mapper previous state 공급 방식 검토

추가 후속 후보:

- ChannelPollingResult skeleton
- ChannelPollingFailureKind skeleton
- RuntimePlcChannelStateMapper skeleton
- XgtRuntimePlcChannelAdapter boundary review
- FakePlc integration boundary review
- Runtime telemetry contract
- command dispatcher skeleton

## 15. Next Step

다음 단계는 AH-RUNTIME-29: RuntimePlcChannelState Readback Boundary Review입니다.

단, AH-RUNTIME-29에서도 바로 API 구현으로 가지 말고, mapper가 previous state를 어떻게 받을지 경계를 먼저 정리하는 것이 안전합니다.
