# AH-RUNTIME-24 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-24의 목표는 AH-RUNTIME-23에서 정리한 writable channel boundary를 바탕으로, InMemoryRuntimePlcChannelState를 일반 RuntimePlcChannelState로 확장할지, 그리고 IWritableRuntimePlcChannel을 추가할지 검토하는 것입니다.

이번 단계는 polling scheduler, XgtDriverCore 연결, XgtChannelRunner 연결, FakePlc integration을 구현하는 단계가 아니라, Runtime 내부에서 "읽기 전용 channel 계약"과 "쓰기 가능한 channel 계약"을 어떻게 분리할지 설계하는 Boundary Review입니다.

## 3. Scope

이번 단계에 포함된 검토 항목:

- InMemoryRuntimePlcChannelState 일반화 여부 검토
- RuntimePlcChannelState 후보 검토
- IWritableRuntimePlcChannel 필요성 검토
- IWritableRuntimePlcChannel과 IRuntimePlcChannel 관계 검토
- ReplaceState(RuntimePlcChannelState state) contract 검토
- RuntimeChannelRegistry와 writable boundary 관계 검토
- PollingScheduler 후속 흐름 검토
- XgtRuntimePlcChannelAdapter 후속 구현 선택지 검토

## 4. Decision

결정 사항:

- AH-RUNTIME-24는 계획 단계로 종료
- IRuntimePlcChannel은 read-only 계약으로 유지
- InMemoryRuntimePlcChannelState는 후속 구현에서 RuntimePlcChannelState로 일반화하는 방향이 자연스러움
- RuntimePlcChannelState는 Contracts DTO가 아니라 Runtime 내부 공통 state model로 정의하는 방향이 적절함
- IWritableRuntimePlcChannel 추가를 후속 구현 후보로 정리
- IWritableRuntimePlcChannel은 IRuntimePlcChannel을 확장하는 선택적 writable boundary 후보
- writable contract에는 ReplaceState(RuntimePlcChannelState state)만 두는 것이 적절함
- InMemoryRuntimePlcChannel은 후속 구현에서 IWritableRuntimePlcChannel 구현체가 되는 것이 자연스러움
- RuntimeChannelRegistry.TryGetChannel은 계속 IRuntimePlcChannel을 반환
- TryGetWritableChannel은 아직 추가하지 않음
- update와 publish는 계속 분리
- IAutomationHubSupervisor public contract는 확장하지 않음
- Runtime 프로젝트는 계속 CAAutomationHub.Contracts만 참조해야 함

## 5. RuntimePlcChannelState Direction

정책:

- RuntimePlcChannelState는 Runtime 내부 공통 state model로 정의
- Contracts DTO가 아님
- ChannelRuntimeState publish 전 단계의 보관/교체 단위
- GetState(capturedAt) 호출 시 Contracts DTO로 변환되는 source state
- polling scheduler, in-memory channel, future adapter가 공유할 수 있는 최소 Runtime boundary

포함 후보 필드:

- PlcId
- DisplayName
- PlcLinkState
- PlcHealthSeverity
- PlcPollingState
- RuntimeSequenceState
- LastSuccessAt
- LastFailureAt
- LastErrorMessage
- RoundTripTimeMs
- ConsecutiveFailures
- ReconnectAttemptCount

주의:

- ChannelRuntimeState에 없는 새 의미를 억지로 추가하지 않음
- Contracts / DTO 변경은 AH-RUNTIME-24 범위 밖

## 6. IWritableRuntimePlcChannel Direction

후속 후보 interface:

```csharp
public interface IWritableRuntimePlcChannel : IRuntimePlcChannel
{
    void ReplaceState(RuntimePlcChannelState state);
}
```

의미:

- 모든 runtime channel이 writable이라는 뜻이 아님
- 일부 runtime channel은 외부 runtime loop가 state를 교체할 수 있다는 선택적 경계
- read-only IRuntimePlcChannel 계약을 유지하면서 writable 가능 여부를 interface로 표현

장점:

- polling scheduler의 concrete cast 제거
- writable 가능 여부를 interface로 표현
- registry는 lookup 책임만 유지
- future adapter가 writable 구현 여부를 선택 가능

## 7. ReplaceState Contract

정책:

- ReplaceState(RuntimePlcChannelState state)는 전체 state 교체 의미
- state == null이면 ArgumentNullException
- state.PlcId가 channel의 PlcId와 다르면 ArgumentException
- internal state만 교체
- SnapshotChanged 발생 없음
- RefreshSnapshotAsync 호출 없음
- publish는 caller 또는 supervisor refresh flow의 책임
- update와 publish 분리 원칙은 XML doc과 테스트로 고정

## 8. RuntimeChannelRegistry Boundary

정책:

- RuntimeChannelRegistry는 계속 lookup-only boundary로 유지
- TryGetChannel(string plcId, out IRuntimePlcChannel channel) 유지
- 반환 타입은 계속 IRuntimePlcChannel
- concrete channel 반환 금지
- update API 추가 금지
- publish 책임 추가 금지
- TryGetWritableChannel은 보류

후속 사용 흐름:

```csharp
if (registry.TryGetChannel(plcId, out var channel) &&
    channel is IWritableRuntimePlcChannel writable)
{
    writable.ReplaceState(state);
}
```

이후 publish가 필요하면 별도로 RefreshSnapshotAsync(...)를 호출하는 구조 유지

## 9. PollingScheduler Follow-up Direction

후속 예상 흐름:

- polling result 수집
- RuntimePlcChannelState 생성
- registry에서 IRuntimePlcChannel lookup
- IWritableRuntimePlcChannel pattern matching
- ReplaceState(state) 호출
- 필요 시 supervisor refresh로 snapshot publish

주의:

- AH-RUNTIME-24에서는 PollingScheduler를 구현하지 않음

## 10. XGT / FakePlc Boundary

정책:

- AH-RUNTIME-24에서는 XgtDriverCore, XgtChannelRunner, FakePlc를 직접 참조하지 않음
- XgtRuntimePlcChannelAdapter가 반드시 IWritableRuntimePlcChannel을 구현한다고 확정하지 않음
- adapter가 IRuntimePlcChannel만 구현할 수도 있음
- adapter가 IWritableRuntimePlcChannel도 구현할 수도 있음
- 별도 update adapter를 둘 수도 있음
- XGT adapter 구현 방식은 후속 별도 Boundary Review로 분리

## 11. Expected Follow-up Implementation Scope

후속 구현 후보:

- InMemoryRuntimePlcChannelState를 RuntimePlcChannelState로 rename/generalize
- IWritableRuntimePlcChannel 추가
- InMemoryRuntimePlcChannel이 IWritableRuntimePlcChannel 구현
- ReplaceState signature를 RuntimePlcChannelState 기준으로 변경
- 기존 테스트를 rename/generalization에 맞춰 조정
- update-only / publish-separation 테스트 보강

후속 테스트 후보:

- IRuntimePlcChannel에는 ReplaceState가 없음
- InMemoryRuntimePlcChannel은 IWritableRuntimePlcChannel
- IWritableRuntimePlcChannel.ReplaceState(RuntimePlcChannelState state)
- ReplaceState(null) guard 유지
- PlcId mismatch guard 유지
- ReplaceState 후 GetState에 반영
- ReplaceState만으로 SnapshotChanged 발생 없음
- RefreshSnapshotAsync 후 snapshot 반영
- registry 반환 타입은 여전히 IRuntimePlcChannel
- caller가 pattern matching으로 writable 확인 가능
- Runtime project reference는 Contracts only 유지
- WPF / Contracts / Supervisor public contract 변경 없음

## 12. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- 코드 수정
- 파일 생성/수정, Closeout 문서 제외
- 테스트 추가
- 명령 실행
- PollingScheduler 구현
- XGT driver/runner 연결
- FakePlc integration
- command dispatcher
- Runtime Event Bridge
- telemetry
- WPF 변경
- DI / App wiring
- RuntimeChannelRegistry.TryGetWritableChannel
- registry update/publish API
- IAutomationHubSupervisor 변경
- Contracts / DTO 변경

## 13. Validation

이번 단계는 계획 / Boundary Review 단계입니다.

검증 기준:

- 코드 수정 없음
- 파일 생성은 Closeout 문서만 허용
- RuntimePlcChannelState 일반화 방향이 historical record에 남음
- IWritableRuntimePlcChannel 선택적 writable boundary 후보가 historical record에 남음
- IRuntimePlcChannel read-only 유지 결정이 historical record에 남음
- RuntimeChannelRegistry lookup-only 책임 유지가 기록됨
- update와 publish 분리 정책이 재확인됨

## 14. ACCEPT Decision

ACCEPT

이유:

- RuntimePlcChannelState 일반화 여부가 정리됨
- IWritableRuntimePlcChannel 추가 여부가 정리됨
- InMemoryRuntimePlcChannel이 writable 구현체가 될지 정리됨
- ReplaceState contract가 정리됨
- Registry와 writable boundary 관계가 정리됨
- PollingScheduler / XGT adapter 후속 관계가 정리됨
- 후속 구현 지시문을 만들 수 있을 만큼 예상 파일, 테스트, 명령, 제외 범위가 정리됨

## 15. Risks / Follow-up Candidates

AH-RUNTIME-25 후보:

- RuntimePlcChannelState generalization implementation
- IWritableRuntimePlcChannel skeleton
- InMemoryRuntimePlcChannel writable implementation
- ReplaceState 후 GetState 반영 테스트
- update와 publish 분리 테스트

추가 후속 후보:

- RuntimeChannelRegistry TryGetWritableChannel 필요성 재검토
- PollingScheduler publish path
- XgtRuntimePlcChannelAdapter boundary review
- FakePlc integration boundary review
- Runtime telemetry contract

## 16. Next Step

다음 단계는 AH-RUNTIME-25: RuntimePlcChannelState / IWritableRuntimePlcChannel Skeleton Implementation입니다.

단, AH-RUNTIME-25에서도 PollingScheduler, XgtDriverCore, XgtChannelRunner, FakePlc, WPF wiring은 제외하고, Runtime 내부 writable boundary와 테스트까지만 작게 진행하는 것이 안전합니다.
