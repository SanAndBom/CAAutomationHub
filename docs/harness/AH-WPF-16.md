# AH-WPF-16 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- Real Runtime Bridge를 위한 최소 Contracts 프로젝트 생성
- Runtime 내부 상태 DTO skeleton 추가
- 기존 WPF UI DTO와 Runtime 계약 DTO 분리 시작
- WPF DTO 이동 없이 신규 Runtime 계약만 추가

## 3. Implemented Scope
- `src/CAAutomationHub.Contracts` 프로젝트 생성
- TargetFramework: `net10.0`
- `CAAutomationHub.sln`에 Contracts 프로젝트 추가
- Runtime 상태 enum 추가
  - `PlcLinkState`
  - `PlcHealthSeverity`
  - `PlcPollingState`
  - `RuntimeSequenceState`
- Runtime Snapshot 계약 추가
  - `RuntimeSnapshot`
  - `ChannelRuntimeState`
  - `RuntimeHealthState`
- Runtime Event 계약 추가
  - `RuntimeEvent`
  - `RuntimeEventSeverity`
  - `RuntimeEventCategory`
- Runtime Command / Result 계약 추가
  - `RuntimeDashboardCommand`
  - `RuntimeDashboardCommandKind`
  - `RuntimeDashboardCommandResult`
- `RuntimeSnapshot.Empty` 추가
- `RuntimeHealthState.Empty` 추가
- `RuntimeSnapshot` null-safe collection 보정
- `RuntimeDashboardCommand.Parameters` null 보정
- Contracts null-safe 최소 테스트 추가

## 4. Changed Files
- `CAAutomationHub.sln`
- `src/CAAutomationHub.Contracts/CAAutomationHub.Contracts.csproj`
- `src/CAAutomationHub.Contracts/Runtime/PlcLinkState.cs`
- `src/CAAutomationHub.Contracts/Runtime/PlcHealthSeverity.cs`
- `src/CAAutomationHub.Contracts/Runtime/PlcPollingState.cs`
- `src/CAAutomationHub.Contracts/Runtime/RuntimeSequenceState.cs`
- `src/CAAutomationHub.Contracts/Runtime/RuntimeSnapshot.cs`
- `src/CAAutomationHub.Contracts/Runtime/ChannelRuntimeState.cs`
- `src/CAAutomationHub.Contracts/Runtime/RuntimeHealthState.cs`
- `src/CAAutomationHub.Contracts/Runtime/Events/RuntimeEvent.cs`
- `src/CAAutomationHub.Contracts/Runtime/Events/RuntimeEventSeverity.cs`
- `src/CAAutomationHub.Contracts/Runtime/Events/RuntimeEventCategory.cs`
- `src/CAAutomationHub.Contracts/Runtime/Commands/RuntimeDashboardCommand.cs`
- `src/CAAutomationHub.Contracts/Runtime/Commands/RuntimeDashboardCommandKind.cs`
- `src/CAAutomationHub.Contracts/Runtime/Commands/RuntimeDashboardCommandResult.cs`
- `tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj`
- `tests/CAAutomationHub.Wpf.Tests/Contracts/RuntimeSnapshotTests.cs`

## 5. Runtime State Enums

### PlcLinkState
- `Offline`
- `Connecting`
- `Online`
- `Reconnecting`
- `Faulted`

### PlcHealthSeverity
- `Healthy`
- `Warning`
- `Congested`
- `Error`
- `Inactive`

### PlcPollingState
- `Idle`
- `Polling`
- `Delayed`
- `Suspended`
- `Resetting`

### RuntimeSequenceState
- `Idle`
- `Running`
- `Waiting`
- `Delayed`
- `Failed`
- `Completed`

## 6. Runtime Snapshot Contracts

### RuntimeSnapshot
- `CapturedAt`
- `Health`
- `Channels`
- `RecentEvents`
- `Empty`
- null-safe collection 보정
- `Health` null 입력 시 `RuntimeHealthState.Empty`로 보정
- `Channels` null 입력 시 `Array.Empty<ChannelRuntimeState>()`로 보정
- `RecentEvents` null 입력 시 `Array.Empty<RuntimeEvent>()`로 보정

### ChannelRuntimeState
- `PlcId`
- `PlcName`
- `LineName`
- `IsEnabled`
- `IpAddress`
- `Port`
- `LinkState`
- `HealthSeverity`
- `PollingState`
- `SequenceState`
- `ConfiguredPollingIntervalMs`
- `EffectivePollingIntervalMs`
- `LastResponseMs`
- `ConsecutiveFailures`
- `ReconnectCount`
- `SuccessRate`
- `LastSuccessAt`
- `LastFailureAt`
- `LastError`

### RuntimeHealthState
- `TotalPlcs`
- `OnlineCount`
- `ReconnectingCount`
- `HealthyCount`
- `WarningCount`
- `CongestedCount`
- `ErrorCount`
- `InactiveCount`
- `CapturedAt`
- `Empty`

## 7. Runtime Event Contracts

### RuntimeEvent
- `EventId`
- `OccurredAt`
- `PlcId`
- `Severity`
- `Category`
- `Message`
- `Status`
- `Detail`

### RuntimeEventSeverity
- `Info`
- `Warning`
- `Error`
- `Critical`

### RuntimeEventCategory
- `General`
- `Connection`
- `Polling`
- `Latency`
- `Traffic`
- `Recovery`
- `Configuration`
- `Command`
- `Supervisor`
- `Balance`

`RuntimeEventCategory.General` fallback category를 포함했다.

## 8. Runtime Command Contracts

### RuntimeDashboardCommandKind
- `TestConnection`
- `AddOrUpdatePlc`
- `DeletePlc`
- `StartPlc`
- `StopPlc`
- `ResetConnection`
- `ManualReconnect`

### RuntimeDashboardCommand
- `CommandId`
- `Kind`
- `PlcId`
- `RequestedAt`
- `Parameters`
- `Parameters` null-safe empty dictionary 보정

### RuntimeDashboardCommandResult
- `CommandId`
- `Success`
- `Status`
- `Message`
- `PlcId`
- `ErrorCode`
- `CompletedAt`

Note:
- `Status`는 이번 skeleton에서는 `string`으로 유지했다.
- 향후 `Accepted`, `Rejected`, `Succeeded`, `Failed`, `TimedOut` 등 enum화 후보로 남긴다.

## 9. Tests Added / Updated
- 테스트 프로젝트가 Contracts 프로젝트를 참조하도록 `tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj`를 업데이트했다.
- `RuntimeSnapshotTests.Empty_ProvidesNullSafeRuntimeSnapshot`
  - `RuntimeSnapshot.Empty`를 검증한다.
  - `Health`가 `RuntimeHealthState.Empty`인지 검증한다.
  - `Channels`가 empty collection인지 검증한다.
  - `RecentEvents`가 empty collection인지 검증한다.
- `RuntimeSnapshotTests.Constructor_ReplacesNullCollectionsAndHealthWithEmptyValues`
  - 생성자에 null `Health`를 전달하면 `RuntimeHealthState.Empty`로 보정되는지 검증한다.
  - 생성자에 null `Channels`를 전달하면 empty collection으로 보정되는지 검증한다.
  - 생성자에 null `RecentEvents`를 전달하면 empty collection으로 보정되는지 검증한다.
  - `CapturedAt` 값이 유지되는지 검증한다.
- `RuntimeDashboardCommand.Parameters` null 보정은 구현되었지만, 이번 최소 테스트 파일에는 별도 테스트로 추가하지 않았다.

## 10. Validation
사전 RED 확인:
- Contracts 프로젝트가 없던 상태에서 관련 테스트/빌드 실패를 확인했다.
- 실패 원인: `CAAutomationHub.Contracts` 네임스페이스 및 프로젝트 부재.

최종 검증:

`dotnet build CAAutomationHub.sln`

결과:
- 성공
- warning 0
- error 0

`dotnet test CAAutomationHub.sln`

결과:
- 성공
- 123 passed
- 0 failed

## 11. Boundary Rules
- 기존 WPF `DashboardSnapshot` 이동 없음
- 기존 WPF `PlcCardSnapshot` 이동 없음
- 기존 WPF `RuntimeDashboardEvent` 이동 없음
- WPF 프로젝트에서 Contracts 타입 사용 없음
- WPF UI 변경 없음
- `DashboardViewModel` 변경 없음
- `RuntimeDashboardAdapter` async/event 구현 없음
- `IAutomationHubSupervisor` 구현 없음
- Supervisor skeleton 구현 없음
- Runtime 프로젝트 생성 없음
- XgtDriverCore 참조 없음
- XgtChannelRunner 참조 없음
- FakePlc 참조 없음
- 실제 PLC 연결 없음
- Add/Edit/Delete runtime command 전환 없음
- Trend/RuntimeSignal mapping 구현 없음
- BalanceController 구현 없음
- Telemetry DTO 추가 없음

## 12. Known Limitations / Notes
- Contracts 프로젝트는 현재 Runtime 계약 skeleton이다.
- 아직 WPF 프로젝트가 Contracts를 참조하지 않는다.
- 기존 WPF UI DTO는 아직 WPF 프로젝트에 남아 있다.
- `RuntimeSnapshot` -> `DashboardSnapshot` mapping은 아직 없다.
- `IAutomationHubSupervisor`는 아직 없다.
- `RuntimeDashboardAdapter` async/event 계약은 아직 없다.
- `RuntimeDashboardCommandResult.Status`는 `string`이며 향후 enum화 후보이다.
- Telemetry 계약은 AH-WPF-17 이후로 보류했다.

## 13. Next Scenario Candidates
1. AH-WPF-17: RuntimeSnapshot to DashboardSnapshot Mapper Skeleton
   - WPF 프로젝트가 Contracts 참조 여부 검토
   - `RuntimeSnapshot` -> `DashboardSnapshot` 변환
   - `PlcHealthSeverity` -> `PlcConnectionState` 매핑
   - `RuntimeEvent` -> `RuntimeDashboardEvent` 매핑

2. AH-WPF-18: RuntimeDashboardAdapter Async/Event Contract Skeleton
   - `StartAsync` / `StopAsync` / `GetSnapshotAsync`
   - `SnapshotChanged` / `EventReceived`
   - 기존 `GetSnapshot` 호환 유지

3. AH-RUNTIME-01: Supervisor Skeleton
   - `IAutomationHubSupervisor`
   - `FakeAutomationHubSupervisor` 또는 InMemory supervisor
   - 실제 PLC 연결 없음

4. AH-RUNTIME-02: Runtime Telemetry Contract
   - `RuntimeTelemetryPoint`
   - `PollingCycleResult`
   - Main Trend / Mini Trend 원천 정리
