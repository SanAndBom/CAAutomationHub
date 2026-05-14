# AH-RUNTIME-05 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-05의 목표는 `CAAutomationHub.Runtime` 프로젝트 안에 `IAutomationHubSupervisor`의 첫 production skeleton 구현체인 `InMemoryAutomationHubSupervisor`를 추가하는 것입니다.

구체적으로는:
- `InMemoryAutomationHubSupervisor` 추가
- `IAutomationHubSupervisor` 구현
- memory-backed `RuntimeSnapshot` source 제공
- `StartAsync` / `StopAsync` / `GetSnapshotAsync` 최소 구현
- `SnapshotChanged` 이벤트 발생
- `CapturedAt` 일치 정책 유지
- `ExecuteAsync` 미지원 정책 명시
- `RuntimeEventRaised`는 아직 발생시키지 않음
- 실제 PLC / polling / command dispatcher / telemetry / WPF 변경은 제외

## 3. Scope

이번 단계에 포함된 항목:
- `src/CAAutomationHub.Runtime/InMemoryAutomationHubSupervisor.cs` 추가
- `tests/CAAutomationHub.Runtime.Tests/InMemoryAutomationHubSupervisorTests.cs` 추가
- initial snapshot 정책 구현
- `StartAsync` idempotent 정책 구현
- `StopAsync` idempotent 정책 구현
- `GetSnapshotAsync` current snapshot 반환 정책 구현
- `SnapshotChanged` 발생 정책 구현
- 내부 revision counter 정책 구현
- `CapturedAt` / `Health.CapturedAt` / `EventArgs.OccurredAt` 일치 정책 검증
- `ExecuteAsync` unsupported failure 결과 반환
- `RuntimeEventRaised` 미발생 검증

## 4. Result

구현 결과:
- `InMemoryAutomationHubSupervisor`가 추가됨
- `InMemoryAutomationHubSupervisor`는 `IAutomationHubSupervisor`를 구현함
- initial snapshot은 `RuntimeSnapshot.Empty`를 사용함
- `StartAsync`는 idempotent이며 stopped -> started 전환 시에만 새 snapshot을 publish함
- `StopAsync`는 idempotent이며 Contracts에 stopped 상태 표현이 없어 마지막 snapshot을 유지함
- `GetSnapshotAsync`는 lock 안에서 current snapshot 참조만 읽고 `Task.FromResult`로 반환함
- `SnapshotChanged`는 snapshot publish 시 lock 밖에서 발생함
- `RuntimeSnapshot.CapturedAt`, `RuntimeSnapshot.Health.CapturedAt`, `RuntimeSnapshotChangedEventArgs.OccurredAt`은 같은 값 사용
- `Revision`은 내부 `_revision` counter로 증가함
- `ExecuteAsync`는 `Success=false`, `Status="Unsupported"`, `ErrorCode="COMMAND_UNSUPPORTED"` 결과를 반환함
- `RuntimeEventRaised`는 선언/구독 가능하지만 Start/Stop에서 발생시키지 않음
- `_gate` lock으로 `_currentSnapshot`, `_started`, `_revision`, runtime event subscription 상태를 보호함

## 5. Changed Files

- `src/CAAutomationHub.Runtime/InMemoryAutomationHubSupervisor.cs`
- `tests/CAAutomationHub.Runtime.Tests/InMemoryAutomationHubSupervisorTests.cs`

## 6. Boundary

이번 단계에서 유지된 경계:
- Runtime 프로젝트는 Contracts만 참조함
- Runtime 프로젝트는 WPF를 참조하지 않음
- Runtime 프로젝트는 `XgtDriverCore`를 참조하지 않음
- Runtime 프로젝트는 `XgtChannelRunner`를 참조하지 않음
- Runtime 프로젝트는 `FakePlc`를 참조하지 않음
- `InMemoryAutomationHubSupervisor`는 실제 PLC fake가 아님
- `InMemoryAutomationHubSupervisor`는 `FakeDashboardRuntimeAdapter` 대체가 아님
- sample PLC channel을 만들지 않음
- `RuntimeDashboardAdapter`를 변경하지 않음
- `SupervisorRuntimeSnapshotProvider`를 변경하지 않음
- `SupervisorRuntimeDashboardLifecycle`을 변경하지 않음
- `DashboardViewModel`을 변경하지 않음
- `App.xaml.cs`를 변경하지 않음
- DI container를 변경하지 않음
- command dispatcher를 만들지 않음
- Runtime Event Bridge를 만들지 않음
- Runtime telemetry를 만들지 않음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:
- 실제 PLC 연결
- `XgtDriverCore` 연결
- `XgtChannelRunner` 연결
- `FakePlc` 연결
- `PollingScheduler` 구현
- `ChannelRegistry` 구현
- `PlcChannel` 구현
- `XgtSession` 구현
- `BalanceController` 구현
- Runtime command dispatcher 구현
- `RuntimeDashboardCommand` 실제 실행
- Runtime Event Bridge 구현
- `EventReceived` 연결
- Runtime telemetry 구현
- WPF UI 변경
- `DashboardViewModel` 변경
- `RuntimeDashboardAdapter` 변경
- `SupervisorRuntimeSnapshotProvider` 변경
- `SupervisorRuntimeDashboardLifecycle` 변경
- `App.xaml.cs` wiring
- DI container 변경
- sample PLC card 생성
- fake dashboard replacement
- `RuntimeSnapshot` DTO Revision 추가
- `DashboardSnapshot` DTO Revision 추가
- Runtime health/degraded policy 구현

## 8. Validation

실행한 검증:
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter InMemoryAutomationHubSupervisorTests`
- `dotnet build CAAutomationHub.sln`
- `dotnet test CAAutomationHub.sln`
- `dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference`
- `git status --short -uall`
- `git status --short -uall -- src\CAAutomationHub.Wpf tests\CAAutomationHub.Wpf.Tests`

검증 결과:
- `InMemoryAutomationHubSupervisorTests` 10개 통과
- build 성공
- 전체 test 성공
- Runtime.Tests 19개 통과
- Wpf.Tests 199개 통과
- Runtime 프로젝트 참조는 `CAAutomationHub.Contracts` 하나뿐임
- 실제 변경 파일 2개 확인
- WPF / Adapter / Provider / Lifecycle / ViewModel / `App.xaml.cs` 변경 없음
- `ExecuteAsync`는 no-op success가 아니라 unsupported failure 결과 반환
- `RuntimeEventRaised`는 Start/Stop에서 발생하지 않음
- UI/보고상 중복 표시는 단순 중복 노출로 확인

## 9. ACCEPT Decision

ACCEPT

이유:
- AH-RUNTIME-05 목표였던 첫 production Runtime source skeleton이 추가됨
- `IAutomationHubSupervisor` 구현체가 생김
- `RuntimeSnapshot`을 memory-backed로 생산할 수 있게 됨
- `StartAsync` / `StopAsync` / `GetSnapshotAsync` 최소 정책이 구현됨
- `SnapshotChanged`와 `CapturedAt` 일치 정책이 검증됨
- `ExecuteAsync`가 no-op success를 반환하지 않음
- `RuntimeEventRaised`가 아직 발생하지 않음
- 실제 PLC / polling / command / telemetry / WPF 변경이 섞이지 않음
- Runtime -> Contracts 단일 참조 경계가 유지됨
- 빌드와 전체 테스트가 통과함

## 10. Risks / Follow-up Candidates

AH-RUNTIME-06 후보:
- actual composition root wiring
- `InMemoryAutomationHubSupervisor`를 WPF lifecycle rail에 연결
- Runtime Event Bridge skeleton
- command dispatcher skeleton
- runtime telemetry contract
- polling scheduler skeleton
- channel registry skeleton
- runtime health/degraded policy
- stopped 상태를 Contracts로 표현할지 여부 검토

## 11. Next Step

다음 단계는 `InMemoryAutomationHubSupervisor`를 실제 WPF lifecycle rail에 연결할지, 아니면 command/event/telemetry 쪽 skeleton을 먼저 정리할지 결정하는 것입니다.

우선 후보:
- AH-RUNTIME-06: actual composition root wiring 계획
- AH-RUNTIME-06: Runtime Event Bridge skeleton 계획
- AH-RUNTIME-06: Runtime command dispatcher skeleton 계획
