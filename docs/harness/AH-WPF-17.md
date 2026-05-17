# AH-WPF-17 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- Contracts `RuntimeSnapshot`을 WPF `DashboardSnapshot`으로 변환하는 Mapper Skeleton 추가
- Real Runtime Bridge 연결 전 Runtime DTO -> UI DTO 변환 경계 확보
- 실제 Runtime 연결 없이 mapper 정책을 테스트로 고정

## 3. Implemented Scope
- WPF 프로젝트에서 Contracts 프로젝트 단방향 참조 추가
- `RuntimeDashboardSnapshotMapper` 추가
- `RuntimeSnapshot` -> `DashboardSnapshot` 변환
- `RuntimeHealthState` -> `RuntimeHealthSnapshot` 변환
- `ChannelRuntimeState` -> `PlcCardSnapshot` 변환
- `PlcHealthSeverity` -> `PlcConnectionState` 변환
- `RuntimeSequenceState` -> `PlcRuntimeSignalSnapshot` fallback 변환
- `RuntimeEvent` -> `RuntimeDashboardEvent` 변환
- `RuntimeSnapshot.RecentEvents` -> `MapEvents` 변환 제공
- `CommunicationTrendSetSnapshot.Empty` fallback 적용

## 4. Changed Files
- `src/CAAutomationHub.Wpf/CAAutomationHub.Wpf.csproj`
- `src/CAAutomationHub.Wpf/Mappers/RuntimeDashboardSnapshotMapper.cs`
- `tests/CAAutomationHub.Wpf.Tests/Mappers/RuntimeDashboardSnapshotMapperTests.cs`

## 5. Mapping Policies

### 5.1 RuntimeSnapshot -> DashboardSnapshot
- `Health` 변환
- `Channels` 변환
- `CommunicationTrend`는 Empty fallback
- `RecentEvents`는 `DashboardSnapshot`에 넣지 않고 별도 `MapEvents` 제공

### 5.2 RuntimeHealthState -> RuntimeHealthSnapshot
- `TotalPlcs`
- `HealthyCount`
- `WarningCount`
- `CongestedCount`
- `ErrorCount`
- `InactiveCount`
- `CapturedAt` -> `SnapshotTime`
- `OnlineCount` / `ReconnectingCount`는 현재 WPF DTO에 없어 보류

### 5.3 ChannelRuntimeState -> PlcCardSnapshot
- `PlcId` / `PlcName` / `LineName` / `IpAddress` / `Port` / `LastResponseMs` 매핑
- `HealthSeverity` -> `ConnectionState`
- `PollingIntervalMs`
  - `EffectivePollingIntervalMs > 0`이면 Effective 사용
  - 아니면 `ConfiguredPollingIntervalMs` 사용
- `TxPerMinute = 0`
- `RxPerMinute = 0`
- `ErrorCount = ConsecutiveFailures` 임시 매핑
- `RuntimeSignal`은 `SequenceState` 기반 fallback

### 5.4 Health Severity Mapping
- `Healthy` -> `Healthy`
- `Warning` -> `Warning`
- `Congested` -> `Congested`
- `Error` -> `Error`
- `Inactive` -> `Inactive`

### 5.5 Runtime Sequence Fallback
- `Idle` -> `대기`
- `Running` -> `실행 중`
- `Waiting` -> `대기 중`
- `Delayed` -> `지연`
- `Failed` -> `실패`
- `Completed` -> `완료`
- Buckets는 empty collection

### 5.6 RuntimeEvent -> RuntimeDashboardEvent
- `OccurredAt` -> `OccurredAt`
- `Severity.ToString()` -> `Severity`
- `Message` -> `Message`
- `PlcId` -> `PlcId`
- `Category.ToString()` -> `Category`
- `Status` -> `Status`
- `Source`
  - `PlcId`가 있으면 `PlcId`
  - 없으면 `Runtime`
- `PlcName = null`
- `EventId` / `Detail`은 WPF DTO에 없어 보류

## 6. Tests Added
- `Map(null)` throws `ArgumentNullException`
- `RuntimeSnapshot.Empty` -> null 없는 `DashboardSnapshot`
- health count / captured time mapping
- `PlcHealthSeverity` 전체 mapping
- channel 기본 필드 mapping
- card count 보존
- polling interval fallback
- `ErrorCount == ConsecutiveFailures`
- `CommunicationTrend` null 방지
- `RuntimeSignal` null 방지
- `RuntimeSequenceState` 전체 fallback mapping
- `RuntimeEvent` mapping
- `RuntimeSnapshot.RecentEvents` -> `MapEvents` count 보존

## 7. Validation

Targeted tests:
- `RuntimeDashboardSnapshotMapperTests`: 23 passed

`dotnet build CAAutomationHub.sln`

결과:
- 성공
- warning 0
- error 0

`dotnet test CAAutomationHub.sln`

결과:
- 성공
- 146 passed
- 0 failed

## 8. Boundary Rules
- 실제 Runtime 연결 없음
- Supervisor 구현 없음
- `RuntimeDashboardAdapter` async/event 구현 없음
- XgtDriverCore 참조 없음
- XgtChannelRunner 참조 없음
- FakePlc 참조 없음
- 실제 PLC 연결 없음
- Telemetry DTO 추가 없음
- Trend mapping 실제 구현 없음
- Mini Trend 실제 Runtime mapping 없음
- UI 변경 없음
- `DashboardViewModel` refresh 방식 변경 없음
- 기존 WPF DTO 이동 없음
- Runtime command execution 구현 없음
- `FakeDashboardRuntimeAdapter` 동작 변경 없음

## 9. Known Limitations / Notes
- `ConsecutiveFailures`는 누적 `ErrorCount`가 아니라 현재 연속 실패 수입니다.
- 후속 Runtime 계약에 누적 `ErrorCount` 또는 `RecentErrorCount`가 필요할 수 있습니다.
- Runtime 계약에는 아직 Tx/Rx telemetry가 없어 `TxPerMinute` / `RxPerMinute`는 0 fallback입니다.
- Runtime 계약에는 아직 Trend telemetry가 없어 `CommunicationTrend`는 Empty fallback입니다.
- Runtime 계약에는 아직 sequence name/elapsed/bucket 상세가 없어 `RuntimeSequenceState` 기반 최소 fallback을 사용합니다.
- `EventId` / `Detail`은 WPF `RuntimeDashboardEvent`에 없어 이번 mapper에서는 보류했습니다.

## 10. Next Scenario Candidates
1. AH-WPF-18: RuntimeDashboardAdapter Provider Skeleton
   - `RuntimeSnapshot` provider interface 추가
   - `RuntimeDashboardAdapter`가 provider + mapper를 사용
   - 실제 Runtime 연결 없음
   - 기존 `GetSnapshot` 호환 유지

2. AH-WPF-19: RuntimeEvent Mapping Extension
   - WPF `RuntimeDashboardEvent`에 `EventId` / `Detail` 추가 여부 검토
   - `RuntimeEvent` -> `RuntimeDashboardEvent` -> `RuntimeEventLogItem` 흐름 강화

3. AH-WPF-20: Runtime Telemetry Contract Review
   - Tx/Rx
   - `ErrorCount` / `RecentErrorCount`
   - Main Trend
   - Mini Trend 원천 계약 검토

4. AH-RUNTIME-01: Supervisor Skeleton
   - `IAutomationHubSupervisor`
   - InMemory/Fake supervisor
   - 실제 PLC 연결 없음
