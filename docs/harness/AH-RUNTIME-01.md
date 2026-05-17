# AH-RUNTIME-01 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-01의 목표는 Runtime 계층의 첫 진입점 skeleton을 추가하는 것입니다.

구체적으로는:

- `CAAutomationHub.Runtime` 프로젝트 추가
- Runtime -> Contracts 단일 참조 경계 생성
- `IAutomationHubSupervisor` 계약 추가
- `RuntimeSnapshotChangedEventArgs` 계약 추가
- `CapturedAt` 일치 정책 명시
- 실제 Supervisor 구현 / Provider Bridge / Runtime Event Bridge / Driver 연결은 제외

## 3. Scope

이번 단계에 포함된 항목:

- `src/CAAutomationHub.Runtime` 프로젝트 추가
- `CAAutomationHub.sln`에 Runtime 프로젝트 포함
- `IAutomationHubSupervisor` 추가
- `RuntimeSnapshotChangedEventArgs` 추가
- Runtime 전용 테스트 프로젝트 추가
- `RuntimeSnapshotChangedEventArgs` 테스트 추가
- Supervisor contract shape 테스트 추가
- Runtime project reference boundary 테스트 추가

## 4. Result

구현 결과:

- `CAAutomationHub.Runtime`은 `net10.0` 프로젝트로 추가됨
- Runtime 프로젝트는 `CAAutomationHub.Contracts`만 참조함
- `IAutomationHubSupervisor`는 lifecycle / snapshot / runtime event / command intake 계약만 정의함
- `RuntimeSnapshotChangedEventArgs`는 `Snapshot` / `OccurredAt` / optional `Revision`을 보존함
- snapshot null 전달 시 `ArgumentNullException` 정책을 가짐
- `CapturedAt` 정책은 XML doc에 명시됨
- `RuntimeSnapshot` DTO와 `DashboardSnapshot` DTO에는 `Revision`을 추가하지 않음

## 5. Changed Files

- `CAAutomationHub.sln`
- `src/CAAutomationHub.Runtime/CAAutomationHub.Runtime.csproj`
- `src/CAAutomationHub.Runtime/IAutomationHubSupervisor.cs`
- `src/CAAutomationHub.Runtime/RuntimeSnapshotChangedEventArgs.cs`
- `tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj`
- `tests/CAAutomationHub.Runtime.Tests/AutomationHubSupervisorContractTests.cs`
- `tests/CAAutomationHub.Runtime.Tests/RuntimeProjectReferenceBoundaryTests.cs`
- `tests/CAAutomationHub.Runtime.Tests/RuntimeSnapshotChangedEventArgsTests.cs`

## 6. Boundary

이번 단계에서 유지된 경계:

- Runtime은 WPF를 참조하지 않음
- Runtime은 `XgtDriverCore`를 참조하지 않음
- Runtime은 `XgtChannelRunner`를 참조하지 않음
- Runtime은 `FakePlc`를 참조하지 않음
- Runtime은 Contracts만 참조함
- Supervisor는 Control Plane 계약일 뿐 실제 `PlcChannel` 구현체가 아님
- `RuntimeDashboardAdapter` / `DashboardViewModel` / WPF UI는 변경하지 않음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- 실제 Runtime 구현
- Supervisor 구현
- `InMemorySupervisor` 구현
- `RuntimeSnapshot` provider bridge 구현
- Runtime Event Bridge 구현
- Runtime telemetry 구현
- `XgtDriverCore` 연결
- `XgtChannelRunner` 연결
- `FakePlc` 연결
- 실제 PLC 연결
- `PollingScheduler` 구현
- `ChannelRegistry` 구현
- `PlcChannel` 구현
- `XgtSession` 구현
- `BalanceController` 구현
- Runtime command execution 구현
- Add/Edit/Delete runtime command 전환
- `TestConnection` / `ResetConnection` / `ManualReconnect` 구현
- UI 변경
- `DashboardViewModel` 변경
- `RuntimeDashboardAdapter` 변경
- `RuntimeSnapshot` DTO `Revision` 추가
- `DashboardSnapshot` DTO `Revision` 추가

## 8. Validation

실행한 검증:

- `dotnet build CAAutomationHub.sln`
- `dotnet test CAAutomationHub.sln`
- `dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference`
- `rg` forbidden dependency checks
- `git status --short -uall`

검증 결과:

- build 성공
- 전체 test 성공
- Runtime.Tests 9개 통과
- Wpf.Tests 177개 통과
- Runtime 프로젝트 참조는 `CAAutomationHub.Contracts` 하나뿐임
- forbidden dependency match 없음
- 변경 파일 8개 확인

## 9. ACCEPT Decision

ACCEPT

이유:

- AH-RUNTIME-01의 목표였던 Runtime supervisor boundary skeleton이 추가됨
- Runtime -> Contracts 단일 참조 경계가 유지됨
- 실제 구현/bridge/driver 연결이 섞이지 않음
- 기존 WPF Dashboard 흐름이 변경되지 않음
- 빌드와 전체 테스트가 통과함

## 10. Risks / Follow-up Candidates

AH-RUNTIME-02 후보:

- `SupervisorRuntimeSnapshotProvider` bridge
- `InMemorySupervisor`
- Runtime Event Bridge
- cached snapshot policy
- Runtime command dispatcher
- Runtime telemetry contract
- `RuntimeSnapshot` revision / ordering policy
- Runtime project dependency guard test 강화

## 11. Next Step

다음 단계는 바로 실제 PLC 연결이 아니라, Runtime과 WPF adapter 사이를 어떻게 연결할지 결정하는 것입니다.

우선 후보:

- AH-RUNTIME-02: `SupervisorRuntimeSnapshotProvider` Bridge Plan
- AH-RUNTIME-02: `InMemorySupervisor` Skeleton Plan
