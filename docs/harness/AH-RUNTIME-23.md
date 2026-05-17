# AH-RUNTIME-23 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-23의 목표는 AH-RUNTIME-22에서 `RuntimeChannelRegistry.TryGetChannel(plcId, out IRuntimePlcChannel channel)`이 추가된 이후, polling scheduler나 runtime orchestration이 channel state를 안전하게 update할 수 있는 writable boundary가 필요한지 검토하는 것입니다.

이번 단계는 `IWritableRuntimePlcChannel`을 바로 구현하거나, polling scheduler / `XgtDriverCore` / `XgtChannelRunner` / FakePlc integration을 구현하는 단계가 아니라, read-only `IRuntimePlcChannel`과 writable channel contract를 분리할지 설계하는 Boundary Review입니다.

## 3. Scope

이번 단계에 포함된 검토 항목:

- `IRuntimePlcChannel` read-only 유지 여부 검토
- `IWritableRuntimePlcChannel` 필요성 검토
- `InMemoryRuntimePlcChannelState` 일반화 필요성 검토
- `RuntimePlcChannelState` 후보 검토
- `RuntimePlcChannelUpdate` 후보 검토
- `ReplaceState` / `ApplyUpdate` 의미 비교
- `RuntimeChannelRegistry` lookup과 writable boundary 관계 검토
- `PollingScheduler` 후속 흐름 검토
- `XgtRuntimePlcChannelAdapter`와 writable boundary 관계 검토

## 4. Decision

결정 사항:

- AH-RUNTIME-23은 계획 단계로 종료
- `IRuntimePlcChannel`은 read-only 계약으로 유지
- `IRuntimePlcChannel`에 `ReplaceState`를 추가하지 않음
- `IAutomationHubSupervisor` public contract는 확장하지 않음
- `RuntimeChannelRegistry`는 lookup 책임만 유지
- `RuntimeChannelRegistry`는 update / publish 책임을 갖지 않음
- `IWritableRuntimePlcChannel`은 후속 후보로 검토
- concrete cast를 피하기 위한 선택적 writable boundary로 `IWritableRuntimePlcChannel`을 검토
- `InMemoryRuntimePlcChannelState`는 writable interface input으로 쓰기에는 concrete 이름이 강함
- `RuntimePlcChannelState` 일반화 여부는 AH-RUNTIME-24 후보로 분리
- `ApplyUpdate`는 telemetry / driver 단계 이후 후보로 보류
- 초기 polling scheduler 흐름에는 `ReplaceState`가 더 적합한 후보로 보임
- `TryGetWritableChannel`은 실제 필요성이 확인된 뒤 검토

## 5. Writable Boundary Direction

후속 후보 interface:

```csharp
public interface IWritableRuntimePlcChannel : IRuntimePlcChannel
{
    void ReplaceState(RuntimePlcChannelState state);
}
```

검토 방향:

- `IWritableRuntimePlcChannel`은 `IRuntimePlcChannel`을 확장하는 선택적 writable boundary 후보
- 모든 channel 구현체가 writable이어야 한다는 의미는 아님
- 필요한 구현체만 선택적으로 구현할 수 있는 경계로 검토
- `InMemoryRuntimePlcChannel`은 후속 구현에서 이 interface를 구현할 후보
- `XgtRuntimePlcChannelAdapter`는 반드시 구현한다고 확정하지 않음

## 6. State Model Direction

현재:

- `InMemoryRuntimePlcChannelState`

문제:

- 이름이 InMemory 구현체에 묶여 있음
- polling scheduler나 writable interface input으로 사용하기에는 부자연스러움

후속 후보:

- `RuntimePlcChannelState`로 일반화
- `RuntimePlcChannelUpdate` 별도 도입은 장기 후보
- AH-RUNTIME-24에서 `RuntimePlcChannelState` 일반화 여부 검토

정책:

- `ChannelRuntimeState`를 update input으로 직접 사용하지 않음
- Contracts DTO와 Runtime internal/update state를 분리
- AH-RUNTIME-17~18에서 정리한 timestamp 의미 분리 유지

## 7. Update / Publish Separation

정책:

- channel update는 state 교체만 담당
- channel update만으로 `SnapshotChanged` 발생 없음
- channel update가 `RefreshSnapshotAsync`를 자동 호출하지 않음
- publish는 caller가 명시적으로 `InMemoryAutomationHubSupervisor.RefreshSnapshotAsync(...)`를 호출
- 향후 `PollingScheduler`가 update -> refresh 순서의 orchestration 책임 후보
- `RuntimeChannelRegistry`는 publish를 알지 않음
- channel은 supervisor를 알지 않음

## 8. Registry / Writable Lookup Direction

정책:

- `RuntimeChannelRegistry.TryGetChannel`은 계속 `IRuntimePlcChannel`을 반환
- `RuntimeChannelRegistry`가 concrete `InMemoryRuntimePlcChannel`을 반환하지 않음
- `TryGetWritableChannel`은 아직 추가하지 않음
- caller가 필요 시 `IWritableRuntimePlcChannel` pattern matching을 검토할 수 있음
- registry update API는 추가하지 않음

## 9. PollingScheduler Follow-up Direction

후속 예상 흐름:

```text
PollingScheduler
registry.TryGetChannel(plcId, out channel)
channel is IWritableRuntimePlcChannel writable
writable.ReplaceState(newState)
supervisor.RefreshSnapshotAsync(...)
```

주의:

- AH-RUNTIME-23에서는 `PollingScheduler`를 구현하지 않음
- XGT / FakePlc / driver adapter는 직접 참조하지 않음
- polling result mapper는 후속 후보

## 10. XGT Adapter Direction

정책:

- `XgtRuntimePlcChannelAdapter`가 반드시 writable interface를 구현한다고 확정하지 않음
- adapter가 자체적으로 driver result를 반영하고 `GetState`만 제공하는 모델도 가능
- `IWritableRuntimePlcChannel`은 필요한 구현체만 선택적으로 구현할 수 있는 경계로 둠
- XGT adapter boundary는 후속 별도 Review로 분리

## 11. Expected Follow-up Direction

AH-RUNTIME-24 후보:

- `RuntimePlcChannelState` 일반화 검토 또는 구현
- `IWritableRuntimePlcChannel` skeleton 검토 또는 구현
- `InMemoryRuntimePlcChannel`이 선택적으로 writable 구현
- `ReplaceState` 후 `GetState` 반영 검증
- update와 publish 분리 검증

보류 후보:

- `RuntimeChannelRegistry.TryGetWritableChannel`
- `RuntimeChannelRegistry` update API
- `PollingScheduler`
- `XgtRuntimePlcChannelAdapter`
- FakePlc integration

## 12. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- 코드 수정
- 파일 생성/수정, Closeout 문서 제외
- 테스트 추가
- 명령 실행
- `IRuntimePlcChannel` 변경
- `IWritableRuntimePlcChannel` 구현
- `RuntimePlcChannelState` rename/generalization 구현
- `RuntimeChannelRegistry` update API
- `RuntimeChannelRegistry.TryGetWritableChannel`
- `PollingScheduler` 구현
- `XgtDriverCore` 참조 추가
- `XgtChannelRunner` 참조 추가
- FakePlc 참조 추가
- command dispatcher 구현
- Runtime Event Bridge 구현
- telemetry 구현
- WPF 변경
- DI / App wiring
- `DashboardViewModel` 변경
- `RuntimeDashboardAdapter` 변경

## 13. Validation

이번 단계는 계획 / Boundary Review 단계입니다.

검증 기준:

- 코드 수정 없음
- 파일 생성은 Closeout 문서만 허용
- `IRuntimePlcChannel` read-only 유지 결정이 historical record에 남음
- `IWritableRuntimePlcChannel` 후보가 historical record에 남음
- `InMemoryRuntimePlcChannelState` 일반화 필요성이 기록됨
- update와 publish 분리 정책이 재확인됨
- `PollingScheduler` / XGT adapter 후속 관계가 기록됨

## 14. ACCEPT Decision

ACCEPT

이유:

- writable boundary 필요성이 정리됨
- `IRuntimePlcChannel` read-only 유지 여부가 재확인됨
- state model 이름 일반화 필요성이 검토됨
- `IWritableRuntimePlcChannel` 후보가 정리됨
- registry lookup과 writable boundary 관계가 정리됨
- polling scheduler와 XGT adapter 후속 관계가 정리됨
- 후속 구현 지시문을 만들 수 있을 만큼 예상 파일, 테스트, 명령, 제외 범위가 정리됨

## 15. Risks / Follow-up Candidates

AH-RUNTIME-24 후보:

- `RuntimePlcChannelState` generalization
- `IWritableRuntimePlcChannel` skeleton
- `InMemoryRuntimePlcChannel` writable implementation
- `ReplaceState` 후 `GetState` 반영 테스트
- update와 publish 분리 테스트

추가 후속 후보:

- `RuntimeChannelRegistry.TryGetWritableChannel` 필요성 재검토
- `PollingScheduler` publish path
- `XgtRuntimePlcChannelAdapter` boundary review
- FakePlc integration boundary review
- Runtime telemetry contract

## 16. Next Step

다음 단계는 AH-RUNTIME-24: `RuntimePlcChannelState` / `IWritableRuntimePlcChannel` Skeleton 계획입니다.

단, AH-RUNTIME-24에서도 polling scheduler, `XgtDriverCore`, `XgtChannelRunner`, FakePlc, WPF wiring은 제외하고, Runtime 내부 writable boundary와 테스트까지만 작게 진행하는 것이 안전합니다.
