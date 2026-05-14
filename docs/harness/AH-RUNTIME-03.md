# AH-RUNTIME-03 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-03의 목표는 `SupervisorRuntimeSnapshotProvider`에 명시적 async refresh 경로와 cached snapshot failure policy skeleton을 추가하는 것입니다.

구체적으로는:

- `RefreshAsync(CancellationToken)` 추가
- `GetSnapshot()` cache-only 정책 유지
- `RefreshAsync`에서만 `IAutomationHubSupervisor.GetSnapshotAsync` 호출
- refresh 성공 시 cache 갱신
- refresh 실패 시 기존 cache 유지
- refresh 실패 예외는 호출자에게 전달
- `SnapshotChanged`와 `RefreshAsync`가 동일한 cache update 경로 사용
- `CapturedAt` 기반 latest-only update 적용
- lock gate 기반 최소 thread-safety 적용
- lifecycle / Runtime Event Bridge / production `InMemorySupervisor` / degraded snapshot은 제외

## 3. Scope

이번 단계에 포함된 항목:

- `SupervisorRuntimeSnapshotProvider.RefreshAsync(CancellationToken)` 추가
- `GetSnapshot()` cache-only 유지
- `RefreshAsync` 성공 cache 갱신
- `RefreshAsync` 실패 시 기존 cache 유지
- 예외 rethrow 정책
- `UpdateSnapshotIfLatest` 공통 cache update 경로
- `RuntimeSnapshot.CapturedAt` 기반 latest-only update
- `_gate` lock 기반 cache read/compare/write 보호
- `SupervisorRuntimeSnapshotProviderTests` 보강

## 4. Result

구현 결과:

- `RefreshAsync`는 `_supervisor.GetSnapshotAsync(cancellationToken)`를 호출함
- supervisor `GetSnapshotAsync` 호출 중에는 lock을 잡지 않음
- `RefreshAsync` 성공 시 받은 `RuntimeSnapshot`을 cache에 반영함
- `RefreshAsync`는 cache에 반영된 `RuntimeSnapshot`을 반환함
- `RefreshAsync` 실패 시 cache를 `RuntimeSnapshot.Empty`로 되돌리지 않음
- `RefreshAsync` 실패 시 기존 cache를 유지함
- `RefreshAsync` 실패 예외는 호출자에게 전달함
- `SnapshotChanged`와 `RefreshAsync`는 동일한 latest-only cache update 경로를 사용함
- incoming `RuntimeSnapshot.CapturedAt`이 current보다 오래되면 cache를 덮어쓰지 않음
- 동일 `CapturedAt`은 마지막 도착 값을 허용함
- `GetSnapshot()`은 cache만 반환함
- `RuntimeSnapshot` -> `DashboardSnapshot` 변환은 하지 않음
- `RuntimeDashboardSnapshotMapper`를 호출하지 않음
- `RuntimeDashboardAdapter`는 변경하지 않음

## 5. Changed Files

- `src/CAAutomationHub.Wpf/Adapters/SupervisorRuntimeSnapshotProvider.cs`
- `tests/CAAutomationHub.Wpf.Tests/Adapters/SupervisorRuntimeSnapshotProviderTests.cs`

## 6. Boundary

이번 단계에서 유지된 경계:

- Runtime 프로젝트는 WPF를 참조하지 않음
- Runtime 프로젝트는 Contracts만 참조함
- `SupervisorRuntimeSnapshotProvider`는 WPF 내부 bridge임
- bridge는 `RuntimeSnapshot`만 공급함
- `RuntimeSnapshot` -> `DashboardSnapshot` 변환은 기존 `RuntimeDashboardAdapter` + `RuntimeDashboardSnapshotMapper`가 담당함
- `GetSnapshot()`은 sync-over-async를 하지 않음
- `RefreshAsync`에서만 supervisor `GetSnapshotAsync`를 호출함
- lifecycle `StartAsync` / `StopAsync` 연결은 하지 않음
- Runtime Event Bridge는 만들지 않음
- `EventReceived`는 연결하지 않음
- production `InMemorySupervisor`는 만들지 않음
- degraded `RuntimeSnapshot`은 만들지 않음
- DTO Revision은 추가하지 않음

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
- degraded `RuntimeSnapshot` 생성
- `IAsyncRuntimeSnapshotProvider` 추가
- `RefreshResult` 타입 추가
- public `LastRefreshError` / `LastRefreshFailedAt` 추가
- `RuntimeDashboardAdapter` 변경
- `RuntimeDashboardSnapshotMapper` 변경

## 8. Validation

실행한 검증:

- `dotnet test tests\CAAutomationHub.Wpf.Tests\CAAutomationHub.Wpf.Tests.csproj --filter SupervisorRuntimeSnapshotProviderTests`
- `dotnet build CAAutomationHub.sln`
- `dotnet test CAAutomationHub.sln`
- `dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference`
- `git status --short -uall`

검증 결과:

- `SupervisorRuntimeSnapshotProviderTests` 13개 통과
- build 성공
- 전체 test 성공
- Runtime.Tests 9개 통과
- Wpf.Tests 190개 통과
- Runtime 프로젝트 참조는 `CAAutomationHub.Contracts` 하나뿐임
- 변경 파일 2개 확인
- sync-over-async 패턴 없음
- `DashboardSnapshot` 생성 없음
- `RuntimeDashboardSnapshotMapper` 호출 없음
- lifecycle 호출 없음
- Runtime Event Bridge 없음
- production supervisor 추가 없음

## 9. ACCEPT Decision

ACCEPT

이유:

- AH-RUNTIME-03 목표였던 `RefreshAsync` + cached snapshot failure policy skeleton이 추가됨
- `GetSnapshot()` cache-only 정책이 유지됨
- refresh 실패 시 기존 cache 유지 정책이 구현됨
- `SnapshotChanged`와 `RefreshAsync`가 동일 cache update 경로를 사용함
- `CapturedAt` 기반 stale rollback 방지 정책이 추가됨
- thread-safety lock gate가 적용됨
- lifecycle / `EventReceived` / Runtime Event Bridge / `InMemorySupervisor`가 섞이지 않음
- Runtime -> Contracts 단일 참조 경계가 유지됨
- 빌드와 전체 테스트가 통과함

## 10. Risks / Follow-up Candidates

AH-RUNTIME-04 후보:

- lifecycle `StartAsync` / `StopAsync` 연결
- provider `RefreshAsync` 호출 주체 결정
- initial refresh composition root 연결
- cached snapshot degraded policy
- `RuntimeDashboardAdapter` event source 연동
- Runtime Event Bridge
- production `InMemorySupervisor`
- Runtime command dispatcher
- Runtime telemetry contract

## 11. Next Step

다음 단계는 `RefreshAsync`를 누가 호출할지 결정하는 것입니다.

우선 후보:

- AH-RUNTIME-04: lifecycle `StartAsync` / `StopAsync` 연결 계획
- AH-RUNTIME-04: initial refresh composition root 연결 계획
- AH-RUNTIME-04: production `InMemorySupervisor` Skeleton 계획
