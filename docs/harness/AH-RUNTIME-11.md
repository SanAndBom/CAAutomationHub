# AH-RUNTIME-11 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-11의 목표는 Runtime 내부 핵심 구조로 들어가기 전에, Supervisor 아래에서 PLC channel들을 어떻게 관리하고 RuntimeSnapshot으로 올릴지 경계를 검토하는 것입니다.

이번 단계는 실제 PLC 연결, XgtDriverCore 연결, XgtChannelRunner 연결, FakePlc integration, PollingScheduler 구현을 수행하는 단계가 아니라, Runtime 내부의 ChannelRegistry / RuntimePlcChannel abstraction / ChannelRuntimeState 생성 책임을 설계하는 Boundary Review입니다.

## 3. Scope

이번 단계에 포함된 검토 항목:

- RuntimePlcChannel abstraction 필요성 검토
- IRuntimePlcChannel 후보 검토
- RuntimeChannelRegistry 필요성 검토
- ChannelRuntimeState 생성 책임 검토
- RuntimeSnapshot 생성 책임 검토
- InMemoryAutomationHubSupervisor 확장 방향 검토
- XgtDriverCore / XgtChannelRunner 재사용 경계 검토
- FakePlc integration 시점 검토
- 후속 구현 범위 B안 검토

## 4. Decision

결정 사항:

- AH-RUNTIME-11은 계획 단계로 종료
- RuntimePlcChannel abstraction은 필요함
- 초기 후보는 IRuntimePlcChannel
- RuntimeChannelRegistry는 필요함
- Supervisor가 channel collection을 직접 소유하지 않도록 분리하는 방향이 안전함
- 초기 ChannelRuntimeState 생성은 IRuntimePlcChannel.GetState(capturedAt) 방식이 적절함
- Channel은 WPF DTO가 아니라 Contracts의 ChannelRuntimeState만 반환해야 함
- RuntimeSnapshot 최종 생성 책임은 IAutomationHubSupervisor 구현체가 유지함
- RuntimeChannelRegistry는 channel states 제공 책임만 가짐
- Supervisor가 단일 capturedAt을 만들고 RuntimeSnapshot.CapturedAt / RuntimeHealthState.CapturedAt / ChannelRuntimeState timestamp 일치 정책을 유지하는 방향이 적절함
- XgtDriverCore / XgtChannelRunner / FakePlc는 AH-RUNTIME-11에서 직접 참조하지 않음
- 후속 구현은 B안: RuntimeChannelRegistry + InMemoryRuntimePlcChannel skeleton이 안전함

## 5. RuntimePlcChannel Direction

정책:

- PLC 1대의 runtime 상태를 표현하는 abstraction이 필요함
- 초기 책임은 작게 제한함
- channel identity 보유
- 현재 runtime state 제공
- ChannelRuntimeState 생성 또는 반환
- StartAsync / StopAsync / polling / driver lifecycle은 아직 제외함

후속 구현 후보:

- src/CAAutomationHub.Runtime/Channels/IRuntimePlcChannel.cs
- src/CAAutomationHub.Runtime/Channels/InMemoryRuntimePlcChannel.cs

## 6. RuntimeChannelRegistry Direction

정책:

- RuntimeChannelRegistry는 여러 PLC channel collection을 보관함
- snapshot-safe enumeration을 제공함
- null channel을 거부함
- 중복 id 정책을 정리함
- Add/Remove/Update command flow는 아직 WPF나 command dispatcher와 연결하지 않음
- Supervisor가 channel collection 세부를 직접 소유하지 않도록 분리함

후속 구현 후보:

- src/CAAutomationHub.Runtime/Channels/RuntimeChannelRegistry.cs

## 7. ChannelRuntimeState / RuntimeSnapshot Responsibility

정책:

- 초기에는 IRuntimePlcChannel.GetState(capturedAt) 방식이 가장 단순함
- channel은 Contracts의 ChannelRuntimeState를 반환함
- channel은 WPF DTO를 만들지 않음
- DashboardSnapshot 변환은 기존 RuntimeDashboardSnapshotMapper 책임으로 유지함
- RuntimeSnapshot 생성의 최종 책임은 Supervisor가 유지함
- Registry는 channel states를 제공함
- Supervisor는 단일 capturedAt을 만들고 RuntimeSnapshot / RuntimeHealthState / ChannelRuntimeState timestamp 일치 정책을 유지함
- RuntimeHealthState 계산은 초기에는 최소 정책으로 둠

## 8. InMemoryAutomationHubSupervisor Direction

후속 방향:

- InMemoryAutomationHubSupervisor가 RuntimeChannelRegistry를 주입받는 방향을 검토함
- 기존 기본 생성자 호환성이 필요하면 empty registry를 기본값으로 둘 수 있음
- Start/Refresh 시 registry 기반 snapshot publish를 연결할 수 있음
- 실제 PLC channel 생성, sample PLC card 생성, fake dashboard replacement는 제외함

## 9. XgtDriverCore / XgtChannelRunner Reuse Boundary

결정:

- 기존 XgtDriverCore / XgtChannelRunner 자산은 재사용 가능성이 있음
- 하지만 AH-RUNTIME-11에서는 직접 참조하지 않음
- Runtime core는 XgtDriverCore, XgtChannelRunner, FakePlc를 몰라야 함
- 후속 단계에서 adapter 후보를 검토함

후속 후보:

- XgtRuntimePlcChannelAdapter
- XgtChannelRunnerRuntimeAdapter

피해야 할 구조:

- ChannelRegistry가 DriverCore를 직접 아는 구조
- Supervisor가 DriverCore를 직접 아는 구조
- WPF가 DriverCore / ChannelRunner를 아는 구조

## 10. FakePlc Integration Direction

결정:

- AH-RUNTIME-11/12에서는 FakePlc integration 제외
- 먼저 RuntimeChannelRegistry + InMemoryRuntimePlcChannel로 snapshot flow를 검증
- 그 다음 별도 AH 단계에서 FakePlc + XgtDriverCore adapter integration 검토

## 11. Expected Follow-up Implementation Scope

후속 구현 권장 범위:

- IRuntimePlcChannel
- InMemoryRuntimePlcChannel
- RuntimeChannelRegistry
- Runtime 단위 테스트

후속 확장 후보:

- InMemoryAutomationHubSupervisor가 registry 기반 RuntimeSnapshot 생성

비추천:

- XgtDriverCore adapter 연결
- XgtChannelRunner 연결
- FakePlc integration
- PollingScheduler 구현

## 12. Expected Files

후속 skeleton 구현 후보:

- src/CAAutomationHub.Runtime/Channels/IRuntimePlcChannel.cs
- src/CAAutomationHub.Runtime/Channels/InMemoryRuntimePlcChannel.cs
- src/CAAutomationHub.Runtime/Channels/RuntimeChannelRegistry.cs
- tests/CAAutomationHub.Runtime.Tests/Channels/RuntimeChannelRegistryTests.cs
- tests/CAAutomationHub.Runtime.Tests/Channels/InMemoryRuntimePlcChannelTests.cs

선택 후보:

- tests/CAAutomationHub.Runtime.Tests/InMemoryAutomationHubSupervisorChannelRegistryTests.cs

## 13. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- 코드 수정
- 파일 생성/수정, Closeout 문서 제외
- 테스트 추가
- 명령 실행
- XgtDriverCore 참조 추가
- XgtChannelRunner 참조 추가
- FakePlc 참조 추가
- 실제 PLC 연결
- polling scheduler 구현
- command dispatcher 구현
- Runtime Event Bridge 구현
- telemetry 구현
- WPF 변경
- App.xaml.cs wiring
- DI 변경
- DashboardViewModel 변경
- RuntimeDashboardAdapter 변경
- sample PLC card 생성
- fake dashboard replacement

## 14. Validation

이번 단계는 계획 / Boundary Review 단계입니다.

검증 기준:

- 코드 수정 없음
- 파일 생성은 Closeout 문서만 허용
- XgtDriverCore 참조 추가 없음
- XgtChannelRunner 참조 추가 없음
- FakePlc 참조 추가 없음
- WPF 변경 없음
- Runtime 내부 channel boundary가 historical record에 남음
- 후속 구현 범위가 RuntimeChannelRegistry + InMemoryRuntimePlcChannel skeleton으로 분리됨

## 15. ACCEPT Decision

ACCEPT

이유:

- RuntimePlcChannel abstraction 필요성이 정리됨
- RuntimeChannelRegistry 필요성이 정리됨
- ChannelRuntimeState 생성 책임이 정리됨
- RuntimeSnapshot 생성 책임이 정리됨
- InMemoryAutomationHubSupervisor 확장 방향이 정리됨
- XgtDriverCore / XgtChannelRunner 재사용 경계가 정리됨
- FakePlc integration 시점이 정리됨
- 후속 구현 지시문을 만들 수 있을 만큼 예상 파일, 테스트, 명령, 제외 범위가 정리됨

## 16. Risks / Follow-up Candidates

AH-RUNTIME-12 후보:

- RuntimeChannelRegistry skeleton
- IRuntimePlcChannel skeleton
- InMemoryRuntimePlcChannel skeleton
- Runtime channel tests
- InMemoryAutomationHubSupervisor registry integration plan

추가 후속 후보:

- XgtRuntimePlcChannelAdapter boundary review
- XgtChannelRunnerRuntimeAdapter boundary review
- FakePlc integration boundary review
- PollingScheduler skeleton
- Runtime command dispatcher skeleton
- Runtime telemetry contract

## 17. Next Step

다음 단계는 AH-RUNTIME-12: RuntimeChannelRegistry + InMemoryRuntimePlcChannel Skeleton입니다.

단, AH-RUNTIME-12에서도 XgtDriverCore / XgtChannelRunner / FakePlc / 실제 PLC 연결은 제외하고, Runtime 프로젝트 내부 skeleton과 테스트까지만 진행하는 것이 안전합니다.
