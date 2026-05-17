# AH-RUNTIME-20 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-20의 목표는 InMemoryRuntimePlcChannel에 concrete ReplaceState API를 추가하는 것입니다.

이번 단계는 IRuntimePlcChannel interface 변경, IAutomationHubSupervisor public contract 변경, polling scheduler 구현, XgtDriverCore 연결, XgtChannelRunner 연결, FakePlc integration이 아니라, InMemoryRuntimePlcChannel 내부 상태를 명시적으로 교체할 수 있는 concrete API를 추가하는 단계입니다.

## 3. Scope

이번 단계에 포함된 항목:
- InMemoryRuntimePlcChannelState 추가
- InMemoryRuntimePlcChannel internal state model 적용
- InMemoryRuntimePlcChannel.ReplaceState(...) 추가
- ReplaceState null guard
- ReplaceState PlcId mismatch guard
- channel-level _gate lock 기반 state read/write 보호
- GetState(capturedAt)의 internal state -> ChannelRuntimeState 변환 유지
- timestamp event policy 유지
- update와 publish 분리 테스트
- ReplaceState 후 RefreshSnapshotAsync 호출 시 RuntimeSnapshot 반영 검증

## 4. Result

구현 결과:
- InMemoryRuntimePlcChannelState가 Runtime 프로젝트 내부 타입으로 추가됨
- ChannelRuntimeState를 update input으로 직접 사용하지 않음
- InMemoryRuntimePlcChannel은 runtime 내부 state model을 보관함
- ReplaceState는 InMemoryRuntimePlcChannel concrete API로 추가됨
- ReplaceState(null)은 ArgumentNullException을 발생시킴
- ReplaceState는 다른 PlcId state를 받으면 ArgumentException을 발생시킴
- InMemoryRuntimePlcChannel은 _gate lock으로 state read/write를 보호함
- GetState(capturedAt)는 lock 안에서 internal state를 ChannelRuntimeState로 변환함
- capturedAt은 LastSuccessAt / LastFailureAt에 덮어쓰지 않음
- LastSuccessAt / LastFailureAt은 state의 event timestamp를 그대로 유지함
- IRuntimePlcChannel에는 ReplaceState를 추가하지 않음
- ReplaceState는 SnapshotChanged를 발생시키지 않음
- ReplaceState는 RefreshSnapshotAsync를 호출하지 않음
- ReplaceState 후 caller가 InMemoryAutomationHubSupervisor.RefreshSnapshotAsync(...)를 명시 호출해야 publish됨
- 실제 ChannelRuntimeState에 없는 CurrentSequence 필드는 추가하지 않음

## 5. Changed Files

아래 변경 파일을 기록합니다.

- src/CAAutomationHub.Runtime/Channels/InMemoryRuntimePlcChannel.cs
- src/CAAutomationHub.Runtime/Channels/InMemoryRuntimePlcChannelState.cs
- tests/CAAutomationHub.Runtime.Tests/Channels/InMemoryRuntimePlcChannelTests.cs
- tests/CAAutomationHub.Runtime.Tests/InMemoryAutomationHubSupervisorChannelRegistryTests.cs

## 6. Boundary

이번 단계에서 유지된 경계:
- IRuntimePlcChannel interface 변경 없음
- IAutomationHubSupervisor public contract 변경 없음
- Contracts / DTO 변경 없음
- WPF 변경 없음
- RuntimeChannelRegistry lookup/update API 추가 없음
- polling scheduler 구현 없음
- XgtDriverCore 참조 추가 없음
- XgtChannelRunner 참조 추가 없음
- FakePlc 참조 추가 없음
- command dispatcher 구현 없음
- Runtime Event Bridge 구현 없음
- telemetry 구현 없음
- DI / App.xaml.cs / DashboardViewModel wiring 없음
- auto RefreshSnapshotAsync 호출 없음
- channel event model 추가 없음
- auto publish 없음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:
- IRuntimePlcChannel interface 변경
- IAutomationHubSupervisor interface 변경
- IWritableRuntimePlcChannel 추가
- RuntimeChannelRegistry lookup API
- RuntimeChannelRegistry update API
- polling scheduler 구현
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
- dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter InMemoryRuntimePlcChannelTests
- dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter InMemoryAutomationHubSupervisorChannelRegistryTests
- dotnet build CAAutomationHub.sln
- dotnet test CAAutomationHub.sln
- dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference
- git status --short -uall

검증 결과:
- InMemoryRuntimePlcChannelTests 9개 통과
- InMemoryAutomationHubSupervisorChannelRegistryTests 12개 통과
- build 성공
- 전체 test 성공
- Runtime.Tests 47개 통과
- Wpf.Tests 214개 통과
- Runtime 프로젝트 참조는 CAAutomationHub.Contracts 하나뿐임
- Contracts / DTO 변경 없음
- WPF / WPF.Tests 변경 없음
- IRuntimePlcChannel 변경 없음
- IAutomationHubSupervisor 변경 없음
- XgtDriverCore / XgtChannelRunner / FakePlc Runtime 구현 참조 추가 없음
- 변경 파일 4개 확인

## 9. ACCEPT Decision

ACCEPT

이유:
- AH-RUNTIME-20 목표였던 concrete ReplaceState API가 추가됨
- ChannelRuntimeState를 update input으로 직접 사용하지 않음
- Runtime 내부 전용 state model이 추가됨
- IRuntimePlcChannel read-only 계약이 유지됨
- update와 publish 분리 원칙이 코드와 테스트로 고정됨
- timestamp event policy가 유지됨
- ReplaceState 후 명시적 RefreshSnapshotAsync 호출 시 snapshot 반영이 검증됨
- Contracts / DTO / WPF / Scheduler / Driver / FakePlc가 섞이지 않음
- 빌드와 전체 테스트가 통과함

## 10. Risks / Follow-up Candidates

AH-RUNTIME-21 후보:
- RuntimeChannelRegistry lookup API
- PollingScheduler publish path
- IWritableRuntimePlcChannel 필요성 재검토
- XgtRuntimePlcChannelAdapter boundary review
- FakePlc integration boundary review
- Runtime Event Bridge
- telemetry contract

추가 후속 후보:
- RuntimeChannelRegistry.TryGetChannel
- polling result -> channel.ReplaceState -> supervisor.RefreshSnapshotAsync orchestration
- RuntimeHealthState aggregation policy
- ChannelRuntimeState.CapturedAt 추가 필요성 재검토

## 11. Next Step

다음 단계는 AH-RUNTIME-21: RuntimeChannelRegistry Lookup API Boundary Review가 자연스럽습니다.

이유:
- ReplaceState는 concrete channel API로 추가됨
- 그러나 외부 orchestration이 특정 PlcId의 channel을 찾을 공식 경계가 아직 없음
- PollingScheduler가 들어오기 전 RuntimeChannelRegistry에서 channel lookup을 어떻게 제공할지 결정해야 함
