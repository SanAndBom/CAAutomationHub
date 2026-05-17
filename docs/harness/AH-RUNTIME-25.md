# AH-RUNTIME-25 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-25의 목표는 AH-RUNTIME-24에서 정리한 writable channel boundary를 바탕으로, InMemoryRuntimePlcChannelState를 Runtime 내부 공통 state model인 RuntimePlcChannelState로 일반화하고, 선택적 writable boundary인 IWritableRuntimePlcChannel을 추가하는 것입니다.

이번 단계는 PollingScheduler, XgtDriverCore 연결, XgtChannelRunner 연결, FakePlc integration, WPF wiring을 구현하는 단계가 아니라, Runtime 내부 writable channel skeleton을 추가하는 단계입니다.

## 3. Scope

이번 단계에 포함된 항목:

- InMemoryRuntimePlcChannelState 제거
- RuntimePlcChannelState 추가
- IWritableRuntimePlcChannel 추가
- IWritableRuntimePlcChannel : IRuntimePlcChannel 구조 추가
- IWritableRuntimePlcChannel.ReplaceState(RuntimePlcChannelState state) 추가
- InMemoryRuntimePlcChannel이 IWritableRuntimePlcChannel 구현
- InMemoryRuntimePlcChannel.ReplaceState signature를 RuntimePlcChannelState 기준으로 변경
- IRuntimePlcChannel read-only 유지
- RuntimeChannelRegistry lookup-only 유지
- update와 publish 분리 테스트 보강

## 4. Result

구현 결과:

- RuntimePlcChannelState가 Runtime 내부 공통 state model로 추가됨
- 기존 InMemoryRuntimePlcChannelState는 제거됨
- RuntimePlcChannelState는 Contracts DTO가 아님
- RuntimePlcChannelState는 ChannelRuntimeState publish 전 단계의 보관/교체 단위로 사용됨
- IWritableRuntimePlcChannel이 추가됨
- IWritableRuntimePlcChannel은 IRuntimePlcChannel을 확장함
- IWritableRuntimePlcChannel은 ReplaceState(RuntimePlcChannelState state)를 제공함
- InMemoryRuntimePlcChannel은 IWritableRuntimePlcChannel을 구현함
- IRuntimePlcChannel에는 ReplaceState를 추가하지 않음
- ReplaceState(null) guard 유지
- PlcId mismatch guard 유지
- ReplaceState 후 GetState(capturedAt)에 변경 상태 반영
- ReplaceState만으로 SnapshotChanged 발생 없음
- ReplaceState는 RefreshSnapshotAsync를 호출하지 않음
- ReplaceState 후 명시적 RefreshSnapshotAsync 호출 시 RuntimeSnapshot에 변경 상태 반영
- RuntimeChannelRegistry.TryGetChannel은 계속 IRuntimePlcChannel을 반환함
- RuntimeChannelRegistry에는 writable/update API를 추가하지 않음

## 5. Changed Files

- src/CAAutomationHub.Runtime/Channels/InMemoryRuntimePlcChannel.cs
- src/CAAutomationHub.Runtime/Channels/InMemoryRuntimePlcChannelState.cs
- src/CAAutomationHub.Runtime/Channels/RuntimePlcChannelState.cs
- src/CAAutomationHub.Runtime/Channels/IWritableRuntimePlcChannel.cs
- tests/CAAutomationHub.Runtime.Tests/Channels/InMemoryRuntimePlcChannelTests.cs
- tests/CAAutomationHub.Runtime.Tests/Channels/RuntimeChannelRegistryTests.cs
- tests/CAAutomationHub.Runtime.Tests/InMemoryAutomationHubSupervisorChannelRegistryTests.cs

## 6. Boundary

이번 단계에서 유지된 경계:

- IRuntimePlcChannel read-only 계약 유지
- IAutomationHubSupervisor interface 변경 없음
- RuntimeChannelRegistry update API 추가 없음
- RuntimeChannelRegistry TryGetWritableChannel 추가 없음
- RuntimeChannelRegistry GetChannel / Contains 추가 없음
- PollingScheduler 구현 없음
- XgtDriverCore 참조 추가 없음
- XgtChannelRunner 참조 추가 없음
- FakePlc 참조 추가 없음
- command dispatcher 구현 없음
- Runtime Event Bridge 구현 없음
- telemetry 구현 없음
- WPF 변경 없음
- App.xaml.cs wiring 없음
- DI 변경 없음
- DashboardViewModel 변경 없음
- RuntimeDashboardAdapter 변경 없음
- auto RefreshSnapshotAsync 호출 없음
- Contracts 변경 없음
- DTO 변경 없음
- ChannelRuntimeState.CapturedAt 추가 없음
- RuntimeSnapshot 변경 없음
- RuntimeHealthState 변경 없음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- IRuntimePlcChannel에 ReplaceState 추가
- IAutomationHubSupervisor interface 변경
- RuntimeChannelRegistry TryGetWritableChannel
- RuntimeChannelRegistry update API
- RuntimeChannelRegistry GetChannel / Contains
- PollingScheduler 구현
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
- auto RefreshSnapshotAsync 호출
- Contracts 변경
- DTO 변경
- ChannelRuntimeState.CapturedAt 추가
- RuntimeSnapshot 변경
- RuntimeHealthState 변경
- sample PLC card 생성
- fake dashboard replacement

## 8. Validation

실행한 검증:

- git status --short -uall
- dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter InMemoryRuntimePlcChannelTests
- dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter InMemoryAutomationHubSupervisorChannelRegistryTests
- dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter RuntimeChannelRegistryTests
- dotnet build CAAutomationHub.sln
- dotnet test CAAutomationHub.sln
- dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference
- git status --short -uall

검증 결과:

- InMemoryRuntimePlcChannelTests 12개 통과
- InMemoryAutomationHubSupervisorChannelRegistryTests 12개 통과
- RuntimeChannelRegistryTests 16개 통과
- build 성공
- 전체 test 성공
- Runtime.Tests 60개 통과
- Wpf.Tests 214개 통과
- Runtime 프로젝트 참조는 CAAutomationHub.Contracts 하나뿐임
- IRuntimePlcChannel 변경 없음
- IAutomationHubSupervisor 변경 없음
- Contracts / DTO 변경 없음
- WPF 변경 없음
- RuntimeChannelRegistry에는 writable/update API 추가 없음
- XgtDriverCore / XgtChannelRunner / FakePlc 참조 추가 없음

## 9. ACCEPT Decision

ACCEPT

이유:

- AH-RUNTIME-25 목표였던 RuntimePlcChannelState generalization이 완료됨
- IWritableRuntimePlcChannel skeleton이 추가됨
- InMemoryRuntimePlcChannel이 선택적 writable 구현체가 됨
- IRuntimePlcChannel read-only 계약이 유지됨
- ReplaceState contract가 RuntimePlcChannelState 기준으로 정리됨
- update와 publish 분리 원칙이 유지됨
- RuntimeChannelRegistry lookup-only 경계가 유지됨
- Contracts / DTO / WPF / Driver / Scheduler가 섞이지 않음
- 빌드와 전체 테스트가 통과함

## 10. Risks / Follow-up Candidates

AH-RUNTIME-26 후보:

- PollingScheduler publish path Boundary Review
- RuntimeChannelRegistry TryGetWritableChannel 필요성 재검토
- XgtRuntimePlcChannelAdapter boundary review
- FakePlc integration boundary review
- Runtime telemetry contract
- command dispatcher skeleton

추가 후속 후보:

- polling result -> RuntimePlcChannelState mapping
- IWritableRuntimePlcChannel usage guideline
- RuntimeHealthState aggregation policy
- Runtime Event Bridge

## 11. Next Step

다음 단계는 AH-RUNTIME-26: PollingScheduler Publish Path Boundary Review가 자연스럽습니다.

이유:

- RuntimeChannelRegistry lookup API가 있음
- IWritableRuntimePlcChannel writable boundary가 있음
- InMemoryRuntimePlcChannel.ReplaceState가 있음
- InMemoryAutomationHubSupervisor.RefreshSnapshotAsync가 있음
- 이제 polling result가 channel state를 갱신하고 snapshot publish로 이어지는 orchestration 경계를 검토할 수 있음
