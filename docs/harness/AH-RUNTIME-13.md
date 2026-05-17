# AH-RUNTIME-13 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-13의 목표는 AH-RUNTIME-12에서 추가한 RuntimeChannelRegistry를 InMemoryAutomationHubSupervisor에 연결해, registry channel states 기반 RuntimeSnapshot을 publish할 수 있도록 하는 것입니다.

이번 단계는 실제 PLC 연결, XgtDriverCore 연결, XgtChannelRunner 연결, FakePlc integration, PollingScheduler, command dispatcher, telemetry 구현이 아니라, RuntimeChannelRegistry states를 RuntimeSnapshot으로 통합하는 단계입니다.

## 3. Scope

이번 단계에 포함된 항목:

- InMemoryAutomationHubSupervisor에 RuntimeChannelRegistry constructor injection 추가
- 기본 생성자 empty RuntimeChannelRegistry 정책 유지
- null registry guard 추가
- StartAsync에서 registry 기반 RuntimeSnapshot publish
- RuntimeHealthState 최소 계산
- SnapshotChanged registry snapshot 포함
- GetSnapshotAsync current snapshot 유지
- StopAsync 마지막 snapshot 유지
- registry integration Runtime 단위 테스트 추가

## 4. Result

구현 결과:

- InMemoryAutomationHubSupervisor가 RuntimeChannelRegistry를 constructor injection으로 받을 수 있게 됨
- 기본 생성자는 new RuntimeChannelRegistry()를 사용함
- null RuntimeChannelRegistry 주입 시 ArgumentNullException 발생
- StartAsync stopped -> started 전환 시 _channelRegistry.GetStates(capturedAt)를 호출함
- registry channel states가 RuntimeSnapshot에 포함됨
- RuntimeSnapshot.CapturedAt과 RuntimeHealthState.CapturedAt이 같은 값을 사용함
- SnapshotChanged.OccurredAt과 RuntimeSnapshot.CapturedAt이 같은 값을 사용함
- RuntimeHealthState는 channel states 기반 최소 count만 계산함
- GetSnapshotAsync는 current snapshot cache만 반환함
- StopAsync는 registry를 호출하지 않음
- StopAsync는 마지막 snapshot을 유지함
- StopAsync는 SnapshotChanged를 발생시키지 않음
- public refresh API를 추가하지 않음
- channel update API를 추가하지 않음

## 5. Changed Files

- src/CAAutomationHub.Runtime/InMemoryAutomationHubSupervisor.cs
- tests/CAAutomationHub.Runtime.Tests/InMemoryAutomationHubSupervisorChannelRegistryTests.cs

## 6. Boundary

이번 단계에서 유지된 경계:

- Runtime 프로젝트는 CAAutomationHub.Contracts만 참조함
- XgtDriverCore 참조 추가 없음
- XgtChannelRunner 참조 추가 없음
- FakePlc 참조 추가 없음
- 실제 PLC 연결 없음
- polling scheduler 구현 없음
- command dispatcher 구현 없음
- Runtime Event Bridge 구현 없음
- telemetry 구현 없음
- WPF 변경 없음
- App.xaml.cs wiring 없음
- DI 변경 없음
- DashboardViewModel 변경 없음
- RuntimeDashboardAdapter 변경 없음
- public RefreshSnapshot API 추가 없음
- channel update API 추가 없음
- RuntimeSnapshot DTO 변경 없음
- ChannelRuntimeState DTO 변경 없음
- Contracts 변경 없음
- sample PLC card 생성 없음
- fake dashboard replacement 없음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

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
- public refresh API
- channel update API
- RuntimeSnapshot DTO 변경
- ChannelRuntimeState DTO 변경
- Contracts 변경
- sample PLC card 생성
- fake dashboard replacement
- stopped snapshot publish
- RuntimeEventRaised 추가 발생
- Runtime command execution

## 8. Validation

실행한 검증:

- dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter InMemoryAutomationHubSupervisorChannelRegistryTests
- dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter InMemoryAutomationHubSupervisorTests
- dotnet build CAAutomationHub.sln
- dotnet test CAAutomationHub.sln
- dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference
- git status --short -uall

검증 결과:

- InMemoryAutomationHubSupervisorChannelRegistryTests 7/7 통과
- InMemoryAutomationHubSupervisorTests 10/10 통과
- build 성공
- 전체 test 성공
- Runtime.Tests 35/35 통과
- Wpf.Tests 214/214 통과
- Runtime 프로젝트 참조는 CAAutomationHub.Contracts 하나뿐임
- 변경 파일 2개 확인
- WPF 파일 변경 없음
- DTO / Contracts 변경 없음

## 9. Timestamp Policy Note

중요 기록:

- ChannelRuntimeState에는 channel-level CapturedAt 전용 필드가 없음
- AH-RUNTIME-13은 AH-RUNTIME-12의 임시 timestamp 정책을 유지함
- 즉, registry.GetStates(capturedAt)에 전달된 capturedAt은 InMemoryRuntimePlcChannel이 기존 timestamp 필드인 LastSuccessAt / 조건부 LastFailureAt에 반영하는 구조를 유지함
- 이는 skeleton 단계의 timestamp 전달 검증용 정책임
- 실제 polling 연결 이후에는 snapshot captured time과 last success/failure time의 의미가 분리되어야 할 수 있음
- 후속 단계에서 ChannelRuntimeState timestamp policy 재검토 필요

## 10. ACCEPT Decision

ACCEPT

이유:

- AH-RUNTIME-13 목표였던 InMemoryAutomationHubSupervisor registry integration이 완료됨
- RuntimeChannelRegistry states가 RuntimeSnapshot에 포함될 수 있게 됨
- SnapshotChanged가 registry 기반 snapshot을 포함하도록 보강됨
- RuntimeSnapshot.CapturedAt / RuntimeHealthState.CapturedAt / SnapshotChanged.OccurredAt 일치 정책이 유지됨
- RuntimeHealthState 최소 계산이 추가됨
- GetSnapshotAsync cache-only 정책이 유지됨
- StopAsync 마지막 snapshot 유지 정책이 유지됨
- XGT / FakePlc / polling / command / telemetry / WPF 변경이 섞이지 않음
- 빌드와 전체 테스트가 통과함

## 11. Risks / Follow-up Candidates

AH-RUNTIME-14 후보:

- public refresh/publish API boundary review
- channel update API boundary review
- RuntimeHealthState aggregation policy
- ChannelRuntimeState timestamp policy
- XgtRuntimePlcChannelAdapter boundary review
- PollingScheduler skeleton
- command dispatcher skeleton

## 12. Next Step

다음 단계는 RuntimeChannelRegistry가 연결된 이후, channel state 변화를 어떻게 publish할 것인지 결정하는 것입니다.

추천 후보:

- AH-RUNTIME-14: public Refresh/Publish Snapshot API Boundary Review
- 또는 AH-RUNTIME-14: Channel Update API Boundary Review
