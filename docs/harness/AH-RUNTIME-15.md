# AH-RUNTIME-15 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-15의 목표는 `InMemoryAutomationHubSupervisor`에 명시적 `RuntimeSnapshot` refresh/publish concrete API를 추가하는 것입니다.

이번 단계는 `IAutomationHubSupervisor` interface 확장, channel update API, polling scheduler, command dispatcher, Runtime Event Bridge, telemetry, XGT/FakePlc integration을 구현하는 단계가 아니라, `InMemoryAutomationHubSupervisor` concrete 수준에서 `RuntimeSnapshot` 재발행 경계를 추가하는 단계입니다.

## 3. Scope

이번 단계에 포함된 항목:

- `InMemoryAutomationHubSupervisor.RefreshSnapshotAsync(CancellationToken)` 추가
- `RefreshSnapshotAsync` 반환형 `Task<RuntimeSnapshot>`
- registry states 기반 `RuntimeSnapshot` 생성
- current snapshot cache 갱신
- revision 증가
- `SnapshotChanged` publish
- publish된 `RuntimeSnapshot` 반환
- 실패 시 기존 cache 유지
- 실패 시 revision 미증가
- 실패 시 `SnapshotChanged` 미발생
- 실패 시 예외 전파
- `GetSnapshotAsync` cache-only 정책 유지
- `IAutomationHubSupervisor` interface 미변경 검증
- `RuntimeEventRaised` 미발생 유지

## 4. Result

구현 결과:

- `RefreshSnapshotAsync`는 `InMemoryAutomationHubSupervisor` concrete public API로 추가됨
- `IAutomationHubSupervisor`에는 `RefreshSnapshotAsync`를 추가하지 않음
- `RefreshSnapshotAsync`는 단일 `capturedAt`을 생성함
- `RefreshSnapshotAsync`는 `_channelRegistry.GetStates(capturedAt)`를 호출함
- `RuntimeSnapshot.CapturedAt`과 `RuntimeHealthState.CapturedAt`이 같은 값을 사용함
- `SnapshotChanged.OccurredAt`과 `RuntimeSnapshot.CapturedAt`이 같은 값을 사용함
- `RuntimeSnapshot` 생성이 성공한 경우에만 `_currentSnapshot`을 갱신함
- `RuntimeSnapshot` 생성이 성공한 경우에만 `_revision`을 증가시킴
- `RuntimeSnapshot` 생성이 성공한 경우에만 `SnapshotChanged`를 publish함
- `RefreshSnapshotAsync`는 publish된 `RuntimeSnapshot`을 반환함
- refresh 실패 시 기존 cache를 유지함
- refresh 실패 시 revision을 증가시키지 않음
- refresh 실패 시 `SnapshotChanged`를 발생시키지 않음
- refresh 실패 예외는 caller에게 전파함
- `GetSnapshotAsync`는 registry refresh 없이 current cache만 반환함
- `RuntimeEventRaised`는 refresh 과정에서 발생시키지 않음

## 5. Changed Files

아래 변경 파일을 기록합니다.

- `src/CAAutomationHub.Runtime/InMemoryAutomationHubSupervisor.cs`
- `tests/CAAutomationHub.Runtime.Tests/AutomationHubSupervisorContractTests.cs`
- `tests/CAAutomationHub.Runtime.Tests/InMemoryAutomationHubSupervisorChannelRegistryTests.cs`
- `tests/CAAutomationHub.Runtime.Tests/InMemoryAutomationHubSupervisorTests.cs`

## 6. Boundary

이번 단계에서 유지된 경계:

- `IAutomationHubSupervisor` interface 변경 없음
- Contracts / DTO 변경 없음
- WPF 변경 없음
- DI / App wiring 변경 없음
- `DashboardViewModel` 변경 없음
- `RuntimeDashboardAdapter` 변경 없음
- channel update API 추가 없음
- polling scheduler 구현 없음
- command dispatcher 구현 없음
- Runtime Event Bridge 구현 없음
- telemetry 구현 없음
- `XgtDriverCore` 참조 추가 없음
- `XgtChannelRunner` 참조 추가 없음
- FakePlc 참조 추가 없음
- 실제 PLC 연결 없음
- partial snapshot policy 없음
- degraded snapshot policy 없음
- `RuntimeHealthState` aggregation 고도화 없음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- `IAutomationHubSupervisor` interface 변경
- channel update API
- polling scheduler 구현
- `XgtDriverCore` 참조 추가
- `XgtChannelRunner` 참조 추가
- FakePlc 참조 추가
- 실제 PLC 연결
- command dispatcher 구현
- Runtime Event Bridge 구현
- telemetry 구현
- partial snapshot policy
- degraded snapshot policy
- `RuntimeHealthState` aggregation 고도화
- WPF 변경
- `App.xaml.cs` wiring
- DI 변경
- `DashboardViewModel` 변경
- `RuntimeDashboardAdapter` 변경
- Contracts / DTO 변경
- `RuntimeSnapshot` DTO 변경
- `ChannelRuntimeState` DTO 변경
- `RuntimeEventRaised` 추가 발생

## 8. Validation

실행한 검증:

- `git status --short -uall`
- TDD RED 확인: `RefreshSnapshotAsync` 없음 `CS1061`
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter InMemoryAutomationHubSupervisorChannelRegistryTests`
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter InMemoryAutomationHubSupervisorTests`
- `dotnet build CAAutomationHub.sln`
- `dotnet test CAAutomationHub.sln`
- `dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference`
- `git status --short -uall`

검증 결과:

- `InMemoryAutomationHubSupervisorChannelRegistryTests` 11개 통과
- `InMemoryAutomationHubSupervisorTests` 11개 통과
- build 성공
- 전체 test 성공
- `Runtime.Tests` 40개 통과
- `Wpf.Tests` 214개 통과
- Runtime 프로젝트 참조는 `CAAutomationHub.Contracts` 하나뿐임
- `IAutomationHubSupervisor` 변경 없음
- Contracts / DTO 변경 없음
- WPF diff 없음
- `XgtDriverCore` / `XgtChannelRunner` / FakePlc 참조 추가 없음
- 변경 파일 4개 확인

## 9. ACCEPT Decision

ACCEPT

이유:

- AH-RUNTIME-15 목표였던 concrete `RefreshSnapshotAsync` API가 추가됨
- `IAutomationHubSupervisor` contract를 넓히지 않음
- `GetSnapshotAsync` cache-only 정책이 유지됨
- refresh 성공 시 `RuntimeSnapshot` publish 경로가 생김
- refresh 실패 시 기존 cache 유지 / 예외 전파 정책이 테스트로 고정됨
- `SnapshotChanged` revision / occurredAt 정책이 유지됨
- `RuntimeEventRaised`는 여전히 발생하지 않음
- Runtime -> Contracts 단일 참조 경계가 유지됨
- WPF / Driver / FakePlc / Scheduler / Command / Telemetry가 섞이지 않음
- 빌드와 전체 테스트가 통과함

## 10. Risks / Follow-up Candidates

AH-RUNTIME-16 후보:

- channel update API boundary review
- polling scheduler publish path
- command dispatcher publish path
- `RuntimeHealthState` aggregation policy
- `ChannelRuntimeState` timestamp policy
- `XgtRuntimePlcChannelAdapter` boundary review

추가 후속 후보:

- `RefreshSnapshotAsync` interface 승격 여부 재검토
- partial snapshot / degraded snapshot policy
- `RuntimeEventRaised` publish policy
- runtime telemetry contract

## 11. Next Step

다음 단계는 channel 상태가 실제로 바뀌었을 때 `RefreshSnapshotAsync`를 호출할 수 있는 경계를 정리하는 것입니다.

추천 후보:

- AH-RUNTIME-16: Channel Update API Boundary Review
- AH-RUNTIME-16: Polling Scheduler Publish Path Boundary Review
