# AH-RUNTIME-29 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-29의 목표는 AH-RUNTIME-28에서 확인된 previous RuntimePlcChannelState 필요성을 바탕으로, mapper 또는 future polling orchestration이 이전 RuntimePlcChannelState를 어디서 어떻게 얻을지 readback 경계를 검토하는 것입니다.

이번 단계는 RuntimePlcChannelState readback API를 바로 구현하거나, ChannelPollingResult / RuntimePlcChannelStateMapper를 구현하는 단계가 아니라, previous state 공급 경계를 설계하는 Boundary Review입니다.

## 3. Scope

이번 단계에 포함된 검토 항목:

- previous RuntimePlcChannelState 공급 경계 검토
- IRuntimePlcChannel read-only 유지 검토
- IWritableRuntimePlcChannel readback 확장 후보 검토
- 별도 reader interface 후보 검토
- concrete-only readback 후보 검토
- RuntimeChannelRegistry lookup-only 유지 검토
- GetState(capturedAt)와 GetRuntimeState() 의미 분리 검토
- thread-safety / snapshot 반환 정책 검토
- AH-RUNTIME-30 구현 후보 정리

## 4. Decision

결정 사항:

- AH-RUNTIME-29는 계획 단계로 종료
- IRuntimePlcChannel에는 RuntimePlcChannelState readback API를 추가하지 않음
- IRuntimePlcChannel은 publish snapshot용 read-only contract로 유지
- IAutomationHubSupervisor public contract는 확장하지 않음
- RuntimeChannelRegistry는 readback / update / publish 중개자로 확장하지 않음
- RuntimeChannelRegistry는 lookup-only 경계를 유지
- concrete-only GetRuntimeState는 장기 경계로 비추천
- 별도 IRuntimePlcChannelStateReader는 장기적으로 깔끔한 후보이나 현재 단계에서는 interface 증가 부담이 있음
- AH-RUNTIME-30 구현 후보는 IWritableRuntimePlcChannel.GetRuntimeState() 추가
- 구현 시 GetState(capturedAt)와 GetRuntimeState() 의미 차이를 XML doc 또는 주석으로 명시해야 함

## 5. Candidate A: IWritableRuntimePlcChannel Readback

후보 API:

```csharp
RuntimePlcChannelState GetRuntimeState();
```

장점:

- ReplaceState(...)와 previous state readback이 같은 runtime channel mutation boundary 안에 모임
- 후속 mapper / polling orchestration 흐름이 단순해짐
- concrete cast 없이 writable channel에서 previous state를 얻을 수 있음

예상 흐름:

- registry.TryGetChannel(plcId, out channel)
- channel is IWritableRuntimePlcChannel writable
- previous = writable.GetRuntimeState()
- next = mapper.Map(previous, pollingResult)
- writable.ReplaceState(next)
- publish coordinator

주의:

- 이름상 Writable interface가 readback도 포함하게 됨
- 다만 현재 목적이 "read current runtime state and replace with next runtime state"라면 하나의 mutable runtime channel boundary로 볼 수 있음

## 6. Candidate B: Separate Reader Interface

후보 API:

```csharp
public interface IRuntimePlcChannelStateReader
{
    RuntimePlcChannelState GetRuntimeState();
}
```

장점:

- readback 책임이 명확히 분리됨
- 어떤 channel은 readback만 제공할 수도 있음
- mapper가 previous state를 요구할 때 의미가 명확함

단점:

- interface가 늘어남
- IRuntimePlcChannel과 역할이 헷갈릴 수 있음
- 현재 단계에서는 과한 abstraction일 수 있음

결론:

- 장기 후보로 남김
- AH-RUNTIME-30에서는 우선 A안이 더 작고 자연스러움

## 7. Candidate C: Concrete-only Readback

후보:

- InMemoryRuntimePlcChannel.GetRuntimeState()

장점:

- 가장 작음
- IRuntimePlcChannel / IWritableRuntimePlcChannel 변경 없음

단점:

- polling orchestration이 concrete cast를 해야 할 수 있음
- AH-RUNTIME-25에서 만든 writable boundary의 의미가 약해짐
- XgtRuntimePlcChannelAdapter로 확장하기 어려움

결론:

- 현재 경계에는 비추천

## 8. GetState / GetRuntimeState Meaning

정책:

- IRuntimePlcChannel.GetState(capturedAt)는 publish snapshot용 ChannelRuntimeState를 반환함
- IWritableRuntimePlcChannel.GetRuntimeState는 Runtime 내부 orchestration용 RuntimePlcChannelState를 반환함
- GetState(capturedAt)는 Contracts DTO 경계
- GetRuntimeState는 Runtime internal state 경계
- 두 API의 의미 차이를 XML doc 또는 주석으로 명확히 기록해야 함

## 9. Thread-safety / Snapshot Policy

정책 후보:

- GetRuntimeState()는 current runtime state snapshot을 반환해야 함
- RuntimePlcChannelState가 immutable이면 lock 안에서 reference를 읽어 반환해도 안전
- mutable이면 defensive copy 필요
- ReplaceState(...)와 GetRuntimeState()는 동일 channel-level lock 정책을 공유하는 것이 적절
- state object를 외부에서 mutate하는 API는 두지 않는 방향 유지

후속 구현 전 확인:

- RuntimePlcChannelState가 immutable인지 확인
- immutable이면 GetRuntimeState는 current state reference 반환 가능
- mutable이면 copy 반환 필요

## 10. Expected Follow-up Implementation Scope

AH-RUNTIME-30 후보:

- IWritableRuntimePlcChannel.GetRuntimeState() 추가
- InMemoryRuntimePlcChannel.GetRuntimeState() 구현
- ReplaceState 후 GetRuntimeState가 새 state 반환
- IRuntimePlcChannel에는 GetRuntimeState가 없음
- GetState(capturedAt)와 GetRuntimeState 의미 분리 테스트
- Runtime project는 계속 CAAutomationHub.Contracts만 참조
- WPF / Contracts / Supervisor public contract 변경 없음

예상 변경 파일:

- src/CAAutomationHub.Runtime/Channels/IWritableRuntimePlcChannel.cs
- src/CAAutomationHub.Runtime/Channels/InMemoryRuntimePlcChannel.cs
- tests/CAAutomationHub.Runtime.Tests/Channels/InMemoryRuntimePlcChannelTests.cs

## 11. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- 코드 수정
- 파일 생성/수정, Closeout 문서 제외
- 테스트 추가
- 명령 실행
- ChannelPollingResult 구현
- RuntimePlcChannelStateMapper 구현
- polling scheduler 구현
- XgtDriverCore 참조 추가
- XgtChannelRunner 참조 추가
- FakePlc 참조 추가
- RuntimeChannelRegistry readback / update API 추가
- IAutomationHubSupervisor 변경
- WPF 변경
- Contracts / DTO 변경
- telemetry aggregation
- command dispatcher

## 12. Validation

이번 단계는 계획 / Boundary Review 단계입니다.

검증 기준:

- 코드 수정 없음
- 파일 생성은 Closeout 문서만 허용
- previous RuntimePlcChannelState 공급 경계가 historical record에 남음
- IWritableRuntimePlcChannel readback 확장 후보가 기록됨
- 별도 reader interface 후보가 기록됨
- concrete-only readback 후보가 검토됨
- GetState(capturedAt)와 GetRuntimeState 의미 차이가 기록됨
- 후속 AH-RUNTIME-30 구현 범위가 정리됨

## 13. ACCEPT Decision

ACCEPT

이유:

- previous RuntimePlcChannelState 공급 경계가 정리됨
- IWritableRuntimePlcChannel readback 확장 여부가 정리됨
- 별도 reader interface 필요성이 정리됨
- concrete-only readback 후보가 검토됨
- GetState(capturedAt)와 GetRuntimeState 의미 차이가 정리됨
- 후속 AH-RUNTIME-30 구현 범위가 결정될 수 있을 만큼 예상 파일, 테스트, 제외 범위가 정리됨

## 14. Risks / Follow-up Candidates

AH-RUNTIME-30 후보:

- IWritableRuntimePlcChannel.GetRuntimeState implementation
- InMemoryRuntimePlcChannel.GetRuntimeState implementation
- GetState(capturedAt) / GetRuntimeState semantic tests
- previous state readback for future mapper

추가 후속 후보:

- ChannelPollingResult skeleton
- ChannelPollingFailureKind skeleton
- RuntimePlcChannelStateMapper skeleton
- PollingScheduler publish path
- XgtRuntimePlcChannelAdapter boundary review
- FakePlc integration boundary review

## 15. Next Step

다음 단계는 AH-RUNTIME-30: IWritableRuntimePlcChannel.GetRuntimeState Implementation입니다.

단, AH-RUNTIME-30에서도 ChannelPollingResult / RuntimePlcChannelStateMapper / polling scheduler / XGT / FakePlc는 제외하고, readback API와 테스트까지만 작게 진행하는 것이 안전합니다.
