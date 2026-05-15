# AH-RUNTIME-27 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-27의 목표는 polling result 또는 향후 polling cycle output이 Runtime channel state update와 `RuntimeSnapshot` publish로 이어지는 최소 publish path coordinator를 추가하는 것입니다.

이번 단계는 actual `PollingScheduler` timer / loop, `XgtDriverCore` 연결, `XgtChannelRunner` 연결, `FakePlc` integration, real PLC polling, command dispatcher, Runtime Event Bridge, telemetry aggregation을 구현하는 단계가 아니라, Runtime 내부 publish path coordinator를 추가하는 단계입니다.

## 3. Scope

이번 단계에 포함된 항목:

- `PollingChannelUpdate` 추가
- `PollingPublishResult` 추가
- `PollingPublishCoordinator` 추가
- `RuntimeChannelRegistry.TryGetChannel(...)` 기반 lookup
- `IWritableRuntimePlcChannel.ReplaceState(...)` 기반 update
- update all then publish once 정책 구현
- `UpdatedCount > 0`일 때만 `refreshSnapshotAsync` 1회 호출
- missing channel result 기록
- non-writable channel result 기록
- update failure result 기록
- publish failure result 기록
- cancellation 전파 정책 구현
- Runtime 단위 테스트 추가

## 4. Result

구현 결과:

- `PollingChannelUpdate`는 `PlcId`와 `RuntimePlcChannelState`를 묶는 publish coordinator 입력 모델로 추가됨
- `PollingChannelUpdate`는 `plcId` null / empty / whitespace guard를 가짐
- `PollingChannelUpdate`는 `state` null guard를 가짐
- `PollingPublishResult`는 `RequestedCount`, `UpdatedCount`, `MissingChannelIds`, `NonWritableChannelIds`, `UpdateFailures`, `PublishAttempted`, `PublishSucceeded`, `PublishException`을 제공함
- `PollingPublishCoordinator`는 `RuntimeChannelRegistry`와 `Func<CancellationToken, Task<RuntimeSnapshot>>` refresh delegate만 주입받음
- `PollingPublishCoordinator`는 `RuntimeChannelRegistry.TryGetChannel(...)`로 channel을 찾음
- writable channel에만 `ReplaceState(...)`를 호출함
- 모든 update를 먼저 처리한 후 `UpdatedCount > 0`일 때 refresh delegate를 최대 1회 호출함
- missing / non-writable / update failure는 result에 기록하고 나머지 처리를 계속함
- publish failure는 non-cancellation exception이면 `PublishException`에 기록하고 throw하지 않음
- cancellation은 `OperationCanceledException` 계열로 전파함
- actual scheduler / timer / loop는 추가하지 않음

## 5. Changed Files

아래 변경 파일을 기록합니다.

- `src/CAAutomationHub.Runtime/Polling/PollingChannelUpdate.cs`
- `src/CAAutomationHub.Runtime/Polling/PollingPublishCoordinator.cs`
- `src/CAAutomationHub.Runtime/Polling/PollingPublishResult.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingPublishCoordinatorTests.cs`

## 6. Boundary

이번 단계에서 유지된 경계:

- actual `PollingScheduler` timer / loop 없음
- `BackgroundService` 없음
- `DispatcherTimer` 없음
- Task loop / `PeriodicTimer` 없음
- interval / cadence / reentrancy / shutdown 정책 없음
- `XgtDriverCore` 참조 추가 없음
- `XgtChannelRunner` 참조 추가 없음
- `FakePlc` 참조 추가 없음
- real PLC polling 없음
- command dispatcher 구현 없음
- Runtime Event Bridge 구현 없음
- telemetry aggregation 없음
- WPF 변경 없음
- DI / `App.xaml.cs` wiring 없음
- `DashboardViewModel` 변경 없음
- `RuntimeDashboardAdapter` 변경 없음
- `IAutomationHubSupervisor` public contract 변경 없음
- `RuntimeChannelRegistry` update / writable API 추가 없음
- Contracts / DTO 변경 없음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- actual `PollingScheduler` timer / loop
- `BackgroundService` / `DispatcherTimer` / Task loop
- interval / cadence / concurrent cycle 방지 / shutdown lifecycle
- `XgtDriverCore` 참조 추가
- `XgtChannelRunner` 참조 추가
- `FakePlc` 참조 추가
- real PLC polling
- polling result mapper
- XGT response mapper
- command dispatcher
- Runtime Event Bridge
- telemetry aggregation
- WPF 변경
- DI / App wiring
- `DashboardViewModel` 변경
- `RuntimeDashboardAdapter` 변경
- `IAutomationHubSupervisor` public contract 확장
- `RuntimeChannelRegistry` update API
- `RuntimeChannelRegistry.TryGetWritableChannel`
- `RuntimeChannelRegistry.GetChannel` / `Contains`
- Contracts / DTO 변경

## 8. Validation

실행한 검증:

- `git status --short -uall`
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter PollingPublishCoordinatorTests`
- TDD RED: `CAAutomationHub.Runtime.Polling` 없음으로 실패 확인
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter PollingPublishCoordinatorTests`
- `dotnet build CAAutomationHub.sln`
- `dotnet test CAAutomationHub.sln`
- `dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference`
- `git status --short -uall`
- production Runtime forbidden reference / scheduler / timer 검색
- `IAutomationHubSupervisor` / `RuntimeChannelRegistry` / Contracts diff 확인

검증 결과:

- `PollingPublishCoordinatorTests` 22개 통과
- build 성공
- 전체 test 성공
- Runtime.Tests 82개 통과
- Wpf.Tests 214개 통과
- Runtime 프로젝트 참조는 `CAAutomationHub.Contracts` 하나뿐임
- 실제 변경 파일 4개 확인
- production Runtime 코드에 `PollingScheduler` / `BackgroundService` / `DispatcherTimer` / `Task.Run` / `PeriodicTimer` / loop 구현 추가 없음
- `XgtDriverCore` / `XgtChannelRunner` / `FakePlc` / WPF 참조 추가 없음
- `IAutomationHubSupervisor` 변경 없음
- `RuntimeChannelRegistry` public contract 변경 없음
- Contracts / DTO 변경 없음

## 9. ACCEPT Decision

ACCEPT

이유:

- AH-RUNTIME-27 목표였던 `PollingPublishCoordinator` skeleton이 추가됨
- polling result 또는 future cycle output을 `RuntimeSnapshot` publish path로 연결하는 최소 Runtime 내부 coordinator가 생김
- update all then publish once 정책이 구현됨
- missing / non-writable / update failure / publish failure가 report-first로 처리됨
- cancellation 전파 정책이 구현됨
- actual scheduler / timer / XGT / `FakePlc` / WPF 변경이 섞이지 않음
- Runtime -> Contracts 단일 참조 경계가 유지됨
- 빌드와 전체 테스트가 통과함

## 10. Risks / Follow-up Candidates

AH-RUNTIME-28 후보:

- `PollingScheduler` timer / loop Boundary Review
- `XgtRuntimePlcChannelAdapter` boundary review
- polling result mapper
- `FakePlc` integration boundary review
- Runtime telemetry contract
- command dispatcher skeleton

추가 후속 후보:

- `PollingPublishCoordinator` refresh failure policy 세분화
- `PollingPublishResult` reporting granularity 조정
- polling result -> `RuntimePlcChannelState` mapping
- scheduler reentrancy / shutdown policy

## 11. Next Step

다음 단계는 바로 actual `PollingScheduler` timer / loop로 들어가기보다, `XgtRuntimePlcChannelAdapter` 또는 polling result mapper 경계를 먼저 검토하는 것이 안전합니다.

후보:

- AH-RUNTIME-28: `XgtRuntimePlcChannelAdapter` Boundary Review
- AH-RUNTIME-28: Polling Result Mapper Boundary Review
- AH-RUNTIME-28: `PollingScheduler` timer / loop Boundary Review
