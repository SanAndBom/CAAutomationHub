# AH-RUNTIME-12 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-12의 목표는 Runtime 프로젝트 내부에 PLC channel 관리의 첫 skeleton을 추가하는 것입니다.

이번 단계는 실제 PLC 연결, XgtDriverCore 연결, XgtChannelRunner 연결, FakePlc integration, PollingScheduler 구현을 수행하는 단계가 아니라, Runtime 내부 channel building block을 추가하는 단계입니다.

구체적으로는:

- IRuntimePlcChannel 추가
- InMemoryRuntimePlcChannel 추가
- RuntimeChannelRegistry 추가
- ChannelRuntimeState 생성 흐름 검증
- capturedAt 전달 정책 검증
- null channel / duplicate PlcId 정책 검증
- Runtime 프로젝트 참조 경계 유지

## 3. Scope

이번 단계에 포함된 항목:

- src/CAAutomationHub.Runtime/Channels/IRuntimePlcChannel.cs 추가
- src/CAAutomationHub.Runtime/Channels/InMemoryRuntimePlcChannel.cs 추가
- src/CAAutomationHub.Runtime/Channels/RuntimeChannelRegistry.cs 추가
- tests/CAAutomationHub.Runtime.Tests/Channels/InMemoryRuntimePlcChannelTests.cs 추가
- tests/CAAutomationHub.Runtime.Tests/Channels/RuntimeChannelRegistryTests.cs 추가
- IRuntimePlcChannel 최소 계약 정의
- InMemoryRuntimePlcChannel의 ChannelRuntimeState 생성
- RuntimeChannelRegistry의 channel collection 관리
- null channel 거부
- duplicate PlcId 거부
- snapshot-safe GetChannels / GetStates 구현
- Runtime 단위 테스트 추가

## 4. Result

구현 결과:

- IRuntimePlcChannel은 PlcId와 GetState(DateTimeOffset capturedAt)만 제공함
- StartAsync / StopAsync / PollAsync / driver lifecycle / command dispatch API는 추가하지 않음
- InMemoryRuntimePlcChannel은 constructor로 identity와 초기 runtime 상태 값을 받음
- InMemoryRuntimePlcChannel.GetState(capturedAt)는 ChannelRuntimeState를 생성함
- RuntimeChannelRegistry.Add(null)은 ArgumentNullException을 발생시킴
- duplicate PlcId는 StringComparer.Ordinal 기준으로 InvalidOperationException을 발생시킴
- RuntimeChannelRegistry 내부 collection은 lock으로 보호됨
- GetChannels()와 GetStates()는 snapshot copy 기반으로 동작함
- GetStates(capturedAt)는 registry가 DTO를 직접 조립하지 않고 각 channel의 GetState(capturedAt)에 위임함

## 5. Changed Files

아래 변경 파일을 기록합니다.

- src/CAAutomationHub.Runtime/Channels/IRuntimePlcChannel.cs
- src/CAAutomationHub.Runtime/Channels/InMemoryRuntimePlcChannel.cs
- src/CAAutomationHub.Runtime/Channels/RuntimeChannelRegistry.cs
- tests/CAAutomationHub.Runtime.Tests/Channels/InMemoryRuntimePlcChannelTests.cs
- tests/CAAutomationHub.Runtime.Tests/Channels/RuntimeChannelRegistryTests.cs

## 6. Boundary

이번 단계에서 유지된 경계:

- Runtime 프로젝트는 CAAutomationHub.Contracts만 참조함
- XgtDriverCore 참조 추가 없음
- XgtChannelRunner 참조 추가 없음
- FakePlc 참조 추가 없음
- 실제 PLC 연결 없음
- WPF 프로젝트 변경 없음
- App.xaml.cs wiring 없음
- DI 변경 없음
- DashboardViewModel 변경 없음
- RuntimeDashboardAdapter 변경 없음
- InMemoryAutomationHubSupervisor 변경 없음
- RuntimeSnapshot 생성 통합 없음
- Supervisor SnapshotChanged 변경 없음
- sample PLC card 생성 없음
- fake dashboard replacement 없음
- polling scheduler 구현 없음
- command dispatcher 구현 없음
- Runtime Event Bridge 구현 없음
- telemetry 구현 없음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- 실제 PLC 연결
- XgtDriverCore 참조 추가
- XgtChannelRunner 참조 추가
- FakePlc 참조 추가
- polling scheduler 구현
- command dispatcher 구현
- Runtime Event Bridge 구현
- telemetry 구현
- WPF 변경
- App.xaml.cs wiring
- DI 변경
- DashboardViewModel 변경
- RuntimeDashboardAdapter 변경
- InMemoryAutomationHubSupervisor 변경
- sample PLC card 생성
- fake dashboard replacement
- RuntimeSnapshot 생성 통합
- Supervisor SnapshotChanged 변경
- Remove / Update / Replace channel API
- channel lifecycle StartAsync / StopAsync
- PollAsync
- driver lifecycle
- Runtime command execution

## 8. Validation

실행한 검증:

- dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter InMemoryRuntimePlcChannelTests
- dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter RuntimeChannelRegistryTests
- dotnet build CAAutomationHub.sln
- dotnet test CAAutomationHub.sln
- dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference
- git status --short -uall
- git status --short -uall src\CAAutomationHub.Wpf tests\CAAutomationHub.Wpf.Tests
- git status --short -uall src\CAAutomationHub.Runtime\InMemoryAutomationHubSupervisor.cs
- rg 기반 forbidden reference 확인

검증 결과:

- InMemoryRuntimePlcChannelTests 3/3 통과
- RuntimeChannelRegistryTests 6/6 통과
- build 성공
- 전체 test 성공
- Runtime.Tests 28/28 통과
- Wpf.Tests 214/214 통과
- Runtime 프로젝트 참조는 CAAutomationHub.Contracts 하나뿐임
- 실제 변경 파일 5개 확인
- WPF 관련 파일 변경 없음
- InMemoryAutomationHubSupervisor 변경 없음
- XgtDriverCore / XgtChannelRunner / FakePlc production 참조 추가 없음
- forbidden reference 검색 결과는 기존 boundary test의 금지 문자열 검증용 InlineData만 확인됨

## 9. Timestamp Policy Note

중요 기록:

- 현재 ChannelRuntimeState에는 channel-level CapturedAt 전용 필드가 없음
- AH-RUNTIME-12 skeleton에서는 capturedAt 전달 검증을 위해 LastSuccessAt / 조건부 LastFailureAt에 capturedAt을 반영함
- 이는 AH-RUNTIME-12 skeleton 한정 임시 정책임
- 실제 polling 연결 이후에는 snapshot captured time과 last success/failure time의 의미가 분리될 수 있음
- 후속 AH-RUNTIME-13 또는 RuntimeSnapshot timestamp policy 단계에서 ChannelRuntimeState timestamp 정책 재검토가 필요함

## 10. ACCEPT Decision

ACCEPT

이유:

- AH-RUNTIME-12 목표였던 Runtime 내부 channel building block skeleton이 추가됨
- IRuntimePlcChannel / InMemoryRuntimePlcChannel / RuntimeChannelRegistry가 추가됨
- ChannelRuntimeState 생성 흐름이 검증됨
- RuntimeChannelRegistry의 null / duplicate / snapshot copy / GetState 위임 정책이 검증됨
- Runtime -> Contracts 단일 참조 경계가 유지됨
- WPF / Supervisor / Driver / FakePlc / Snapshot integration이 섞이지 않음
- 빌드와 전체 테스트가 통과함
- timestamp 리스크가 후속 정책 검토 항목으로 식별됨

## 11. Risks / Follow-up Candidates

AH-RUNTIME-13 후보:

- InMemoryAutomationHubSupervisor registry integration
- registry states 기반 RuntimeSnapshot 생성
- RuntimeHealthState 최소 계산
- SnapshotChanged registry 기반 publish
- ChannelRuntimeState timestamp policy 재검토
- RuntimeSnapshot / RuntimeHealthState / ChannelRuntimeState timestamp 일치 정책 정리

추가 후속 후보:

- XgtRuntimePlcChannelAdapter boundary review
- XgtChannelRunnerRuntimeAdapter boundary review
- FakePlc integration boundary review
- PollingScheduler skeleton
- command dispatcher skeleton
- Runtime telemetry contract

## 12. Next Step

다음 단계는 AH-RUNTIME-13: InMemoryAutomationHubSupervisor Registry Integration 계획입니다.

단, AH-RUNTIME-13에서도 XgtDriverCore / XgtChannelRunner / FakePlc / 실제 PLC 연결은 제외하고, RuntimeChannelRegistry states를 RuntimeSnapshot으로 통합하는 범위까지만 검토하는 것이 안전합니다.
