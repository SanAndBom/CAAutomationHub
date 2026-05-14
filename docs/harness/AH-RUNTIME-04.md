# AH-RUNTIME-04 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-04의 목표는 Runtime lifecycle과 initial refresh 호출 주체를 `RuntimeDashboardAdapter`나 `DashboardViewModel`에 섞지 않고, 별도 lifecycle bridge service로 분리하는 것이다.

구체적으로는 다음을 목표로 했다.

- `SupervisorRuntimeDashboardLifecycle` 추가
- `IAutomationHubSupervisor.StartAsync` 호출
- `SupervisorRuntimeSnapshotProvider.RefreshAsync` 호출
- `IAutomationHubSupervisor.StopAsync` 호출
- `StartAsync` 순서 고정: `supervisor.StartAsync` -> `provider.RefreshAsync`
- `provider.RefreshAsync` 실패는 tolerant하게 처리
- `RuntimeDashboardAdapter`는 lifecycle을 직접 맡지 않음
- `DashboardViewModel`은 runtime lifecycle을 알지 않음
- actual `App.xaml.cs` wiring / DI 변경은 제외

## 3. Scope

이번 단계에 포함된 항목은 다음과 같다.

- `src/CAAutomationHub.Wpf/Adapters/SupervisorRuntimeDashboardLifecycle.cs` 추가
- `tests/CAAutomationHub.Wpf.Tests/Adapters/SupervisorRuntimeDashboardLifecycleTests.cs` 추가
- `supervisor.StartAsync` -> `provider.RefreshAsync` 호출 순서 테스트
- `supervisor.StartAsync` 실패 시 `provider.RefreshAsync` 미호출 테스트
- `provider.RefreshAsync` 실패 tolerant 처리 테스트
- `StopAsync`가 `supervisor.StopAsync`만 호출하는지 테스트
- `RuntimeDashboardAdapter`가 lifecycle을 직접 구현하지 않는지 검증
- `DashboardViewModel` / `App.xaml.cs` / DI 변경 없음 확인

## 4. Result

구현 결과는 다음과 같다.

- `SupervisorRuntimeDashboardLifecycle`이 추가됨
- `SupervisorRuntimeDashboardLifecycle`은 `IRuntimeDashboardLifecycle`을 구현함
- `StartAsync`는 `supervisor.StartAsync`를 먼저 호출함
- `supervisor.StartAsync` 성공 후 `snapshotProvider.RefreshAsync`를 호출함
- `supervisor.StartAsync` 실패는 삼키지 않고 전파함
- `snapshotProvider.RefreshAsync` 실패는 tolerant하게 처리하여 `StartAsync`를 완료함
- `StopAsync`는 `supervisor.StopAsync`만 호출함
- `StopAsync`에서 `provider.Dispose`를 호출하지 않음
- `provider.Dispose`는 event 구독 해제 책임으로 분리됨
- `RuntimeDashboardAdapter`는 변경하지 않음
- `DashboardViewModel`은 변경하지 않음
- `App.xaml.cs`는 변경하지 않음
- DI 구성은 변경하지 않음

## 5. Changed Files

변경 파일은 다음과 같다.

- `src/CAAutomationHub.Wpf/Adapters/SupervisorRuntimeDashboardLifecycle.cs`
- `tests/CAAutomationHub.Wpf.Tests/Adapters/SupervisorRuntimeDashboardLifecycleTests.cs`

## 6. Boundary

이번 단계에서 유지된 경계는 다음과 같다.

- `RuntimeDashboardAdapter`는 lifecycle을 직접 맡지 않음
- `RuntimeDashboardAdapter`는 `RuntimeSnapshot` -> `DashboardSnapshot` 변환 역할을 유지함
- `DashboardViewModel`은 `IAutomationHubSupervisor`를 알지 않음
- `DashboardViewModel`은 `SupervisorRuntimeSnapshotProvider.RefreshAsync`를 알지 않음
- `DashboardViewModel`은 runtime lifecycle을 알지 않음
- lifecycle service는 WPF 내부 bridge service임
- lifecycle service는 actual app wiring이 아님
- lifecycle service는 production supervisor 구현체가 아님
- Runtime 프로젝트는 WPF를 참조하지 않음
- Runtime 프로젝트는 Contracts만 참조함
- WPF -> Runtime 참조 방향은 기존 AH-RUNTIME-02 정책대로 유지됨
- Runtime Event Bridge는 만들지 않음
- `EventReceived`는 연결하지 않음
- retry policy는 만들지 않음
- degraded `RuntimeSnapshot`은 만들지 않음
- production `InMemorySupervisor`는 만들지 않음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목은 다음과 같다.

- 실제 Runtime 구현
- production `InMemorySupervisor` 구현
- actual `App.xaml.cs` wiring
- DI container 구성 변경
- `RuntimeDashboardAdapter` lifecycle 구현
- `DashboardViewModel` lifecycle 인식 추가
- Runtime Event Bridge 구현
- `EventReceived` 연결
- retry policy 구현
- degraded `RuntimeSnapshot` 생성
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

## 8. Validation

실행한 검증은 다음과 같다.

- `dotnet test tests\CAAutomationHub.Wpf.Tests\CAAutomationHub.Wpf.Tests.csproj --filter SupervisorRuntimeDashboardLifecycleTests`
- `dotnet build CAAutomationHub.sln`
- `dotnet test CAAutomationHub.sln`
- `dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference`
- `git status --short -uall`
- `RuntimeDashboardAdapter` / `DashboardViewModel` / `App.xaml.cs` / `CAAutomationHub.Wpf.csproj` diff 확인
- DI 관련 패턴 검색 확인

검증 결과는 다음과 같다.

- `SupervisorRuntimeDashboardLifecycleTests` 9개 통과
- build 성공
- 전체 test 성공
- `Runtime.Tests` 9개 통과
- `Wpf.Tests` 199개 통과
- Runtime 프로젝트 참조는 `CAAutomationHub.Contracts` 하나뿐임
- 실제 변경 파일 2개 확인
- `RuntimeDashboardAdapter` 변경 없음
- `DashboardViewModel` 변경 없음
- `App.xaml.cs` 변경 없음
- `CAAutomationHub.Wpf.csproj` 변경 없음
- DI 관련 변경 없음
- UI 중복 표시는 단순 표시 이슈로 확인

## 9. ACCEPT Decision

ACCEPT

이유는 다음과 같다.

- AH-RUNTIME-04 목표였던 lifecycle 호출 주체 분리가 완료됨
- `RuntimeDashboardAdapter`가 lifecycle 책임을 떠안지 않음
- `DashboardViewModel`이 runtime lifecycle을 알지 않음
- `supervisor.StartAsync` -> `provider.RefreshAsync` 순서가 테스트로 검증됨
- `provider.RefreshAsync` 실패 tolerant 정책이 검증됨
- `StopAsync`와 `provider.Dispose` 책임이 분리됨
- actual App wiring / DI / production supervisor가 섞이지 않음
- Runtime -> Contracts 단일 참조 경계가 유지됨
- 빌드와 전체 테스트가 통과함

## 10. Risks / Follow-up Candidates

AH-RUNTIME-05 후보는 다음과 같다.

- actual composition root wiring
- production `InMemorySupervisor`
- `RuntimeDashboardAdapter` event source 연동
- Runtime Event Bridge
- cached snapshot degraded policy
- Runtime command dispatcher
- Runtime telemetry contract
- lifecycle logging / failure reporting policy
- initial refresh failure reporting policy

## 11. Next Step

다음 단계는 실제 App wiring으로 바로 들어가기보다, production supervisor가 없는 상태에서 어떤 runtime source를 먼저 만들지 결정하는 것이다.

우선 후보는 다음과 같다.

- AH-RUNTIME-05: production `InMemorySupervisor` Skeleton 계획
- AH-RUNTIME-05: actual composition root wiring 계획
- AH-RUNTIME-05: `RuntimeDashboardAdapter` event source 연동 계획
