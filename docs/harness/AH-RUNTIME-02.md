# AH-RUNTIME-02 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-02의 목표는 AH-RUNTIME-01에서 추가한 `IAutomationHubSupervisor`와 기존 WPF `RuntimeDashboardAdapter` 사이에 최소 bridge skeleton을 추가하는 것입니다.

구체적으로는:

- WPF 프로젝트 내부에 `SupervisorRuntimeSnapshotProvider` 추가
- `SupervisorRuntimeSnapshotProvider`가 `IRuntimeSnapshotProvider`를 구현
- `IAutomationHubSupervisor.SnapshotChanged`를 통해 `RuntimeSnapshot` cache 갱신
- `GetSnapshot()`은 cache된 `RuntimeSnapshot`만 반환
- sync-over-async 금지
- `RuntimeSnapshot` -> `DashboardSnapshot` 변환은 기존 `RuntimeDashboardAdapter` + `RuntimeDashboardSnapshotMapper`에 유지
- lifecycle / Runtime Event Bridge / production `InMemorySupervisor`는 제외

## 3. Scope

이번 단계에 포함된 항목:

- WPF -> Runtime project reference 추가
- `src/CAAutomationHub.Wpf/Adapters/SupervisorRuntimeSnapshotProvider.cs` 추가
- `tests/CAAutomationHub.Wpf.Tests/Adapters/SupervisorRuntimeSnapshotProviderTests.cs` 추가
- constructor null guard 테스트
- 초기 empty snapshot 테스트
- `SnapshotChanged` 기반 cache 갱신 테스트
- `GetSnapshot()` sync-over-async 방지 테스트
- `Dispose` 구독 해제 테스트
- `DashboardSnapshot` 변환 미수행 확인

## 4. Result

구현 결과:

- `SupervisorRuntimeSnapshotProvider`는 `IRuntimeSnapshotProvider`와 `IDisposable`을 구현함
- constructor로 `IAutomationHubSupervisor`를 받음
- 초기 cache는 `RuntimeSnapshot.Empty`를 사용함
- `GetSnapshot()`은 cache된 `RuntimeSnapshot`만 반환함
- supervisor `SnapshotChanged` 발생 시 cache가 `e.Snapshot`으로 갱신됨
- `Dispose()`에서 `SnapshotChanged` 구독을 해제함
- `GetSnapshot()` 내부에서 supervisor `GetSnapshotAsync`를 호출하지 않음
- `DashboardSnapshot`을 만들지 않음
- `RuntimeDashboardSnapshotMapper`를 호출하지 않음
- `RuntimeDashboardAdapter` 구조를 변경하지 않음
- `DashboardViewModel`을 변경하지 않음
- `EventReceived`를 연결하지 않음

## 5. Changed Files

- `src/CAAutomationHub.Wpf/CAAutomationHub.Wpf.csproj`
- `src/CAAutomationHub.Wpf/Adapters/SupervisorRuntimeSnapshotProvider.cs`
- `tests/CAAutomationHub.Wpf.Tests/Adapters/SupervisorRuntimeSnapshotProviderTests.cs`

## 6. Boundary

이번 단계에서 유지된 경계:

- Runtime 프로젝트는 WPF를 참조하지 않음
- Runtime 프로젝트는 Contracts만 참조함
- WPF 프로젝트가 Runtime 프로젝트를 참조하는 것은 bridge composition 방향으로 허용함
- `SupervisorRuntimeSnapshotProvider`는 WPF 내부 bridge임
- bridge는 `RuntimeSnapshot`만 공급함
- `RuntimeSnapshot` -> `DashboardSnapshot` 변환은 기존 `RuntimeDashboardAdapter` + `RuntimeDashboardSnapshotMapper`가 담당함
- bridge는 `DashboardSnapshot`을 만들지 않음
- bridge는 `RuntimeDashboardSnapshotMapper`를 호출하지 않음
- lifecycle `StartAsync` / `StopAsync` 연결은 하지 않음
- Runtime Event Bridge는 만들지 않음
- production `InMemorySupervisor`는 만들지 않음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- 실제 Runtime 구현
- production `InMemorySupervisor` 구현
- lifecycle `StartAsync` / `StopAsync` 연결
- Runtime Event Bridge 구현
- `EventReceived` 연결
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
- `DashboardViewModel` UI 동작 변경
- Communication Trend 변경
- Mini Trend 변경
- `RuntimeSnapshot` DTO Revision 추가
- `DashboardSnapshot` DTO Revision 추가
- 별도 composition project 생성
- `IAsyncRuntimeSnapshotProvider` 추가

## 8. Validation

실행한 검증:

- `dotnet test tests\CAAutomationHub.Wpf.Tests\CAAutomationHub.Wpf.Tests.csproj --filter SupervisorRuntimeSnapshotProviderTests`
- `dotnet build CAAutomationHub.sln`
- `dotnet test CAAutomationHub.sln`
- `dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference`
- `dotnet list src\CAAutomationHub.Wpf\CAAutomationHub.Wpf.csproj reference`
- `git status --short -uall`

검증 결과:

- `SupervisorRuntimeSnapshotProviderTests` 6개 통과
- build 성공
- 전체 test 성공
- Runtime.Tests 9개 통과
- Wpf.Tests 183개 통과
- Runtime 프로젝트 참조는 `CAAutomationHub.Contracts` 하나뿐임
- WPF 프로젝트 참조는 `CAAutomationHub.Contracts`, `CAAutomationHub.Runtime`
- 실제 변경 파일 3개 확인
- `SupervisorRuntimeSnapshotProviderTests.cs` 중복 표시는 단순 UI 표시 문제로 확인

## 9. ACCEPT Decision

ACCEPT

이유:

- AH-RUNTIME-02 목표였던 `SupervisorRuntimeSnapshotProvider` bridge skeleton이 추가됨
- 기존 `RuntimeDashboardAdapter` -> `IRuntimeSnapshotProvider` -> `RuntimeDashboardSnapshotMapper` 흐름이 유지됨
- bridge는 `RuntimeSnapshot`만 공급함
- sync-over-async가 없음
- lifecycle / `EventReceived` / Runtime Event Bridge / `InMemorySupervisor`가 섞이지 않음
- Runtime -> Contracts 단일 참조 경계가 유지됨
- 빌드와 전체 테스트가 통과함

## 10. Risks / Follow-up Candidates

AH-RUNTIME-03 후보:

- lifecycle `StartAsync` / `StopAsync` 연결
- initial async snapshot refresh policy
- cached snapshot failure/degraded policy
- `RuntimeDashboardAdapter` event source 연동
- Runtime Event Bridge
- production `InMemorySupervisor`
- Runtime command dispatcher
- Runtime telemetry contract
- `RuntimeSnapshot` revision / ordering policy

## 11. Next Step

다음 단계는 바로 실제 PLC 연결이 아니라, bridge 이후 Runtime 연결 흐름을 어디까지 확장할지 결정하는 것입니다.

우선 후보:

- AH-RUNTIME-03: lifecycle `StartAsync` / `StopAsync` 연결 계획
- AH-RUNTIME-03: `InMemorySupervisor` Skeleton 계획
- AH-RUNTIME-03: initial async snapshot refresh / cached snapshot policy 계획
