# AH-WPF-18 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- `RuntimeDashboardAdapter`를 provider 기반 skeleton으로 전환
- `RuntimeSnapshot` provider와 `RuntimeDashboardSnapshotMapper` 사이 경계 확보
- 실제 Runtime 연결 없이 Adapter가 `RuntimeSnapshot` -> `DashboardSnapshot` 변환 흐름을 갖도록 준비

## 3. Implemented Scope
- `IRuntimeSnapshotProvider` 추가
- `EmptyRuntimeSnapshotProvider` 추가
- `RuntimeDashboardAdapter` provider constructor 추가
- parameterless constructor 유지
- `RuntimeDashboardAdapter.GetSnapshot()`을 provider + mapper 흐름으로 변경
- provider null guard 추가
- provider exception 전파 정책 유지
- `IRuntimeDashboardAdapter` 기존 계약 유지

## 4. Changed Files
- `src/CAAutomationHub.Wpf/Adapters/IRuntimeSnapshotProvider.cs`
- `src/CAAutomationHub.Wpf/Adapters/EmptyRuntimeSnapshotProvider.cs`
- `src/CAAutomationHub.Wpf/Adapters/RuntimeDashboardAdapter.cs`
- `tests/CAAutomationHub.Wpf.Tests/Adapters/RuntimeDashboardAdapterTests.cs`

## 5. Final Adapter Flow
```text
RuntimeDashboardAdapter.GetSnapshot()
-> IRuntimeSnapshotProvider.GetSnapshot()
-> RuntimeDashboardSnapshotMapper.Map(runtimeSnapshot)
-> DashboardSnapshot
```

기본 생성자:

```text
RuntimeDashboardAdapter()
-> EmptyRuntimeSnapshotProvider
-> RuntimeSnapshot.Empty
```

## 6. Tests Added
- default constructor returns null-safe `DashboardSnapshot`
- null provider throws `ArgumentNullException`
- custom provider maps Runtime health counts
- custom provider maps `ChannelRuntimeState` to `PlcCardSnapshot`
- `RuntimeSnapshot.Empty` provider produces non-null `CommunicationTrend`
- provider exception is propagated
- `GetSnapshot()` calls provider once

## 7. Validation
Targeted tests:
- `RuntimeDashboardAdapterTests`: 7 passed

`dotnet build CAAutomationHub.sln`

결과:
- 성공
- warning 0
- error 0

`dotnet test CAAutomationHub.sln`

결과:
- 성공
- 153 passed

## 8. Boundary Rules
- `IRuntimeDashboardAdapter` 변경 없음
- `DashboardViewModel` 변경 없음
- `FakeDashboardRuntimeAdapter` 변경 없음
- `RuntimeDashboardSnapshotMapper` mapping policy 변경 없음
- UI 변경 없음
- Add/Edit/Delete 변경 없음
- async/event/lifecycle 구현 없음
- Runtime command execution 없음
- Supervisor 구현 없음
- `IAutomationHubSupervisor` 구현 없음
- `XgtDriverCore` 참조 없음
- `XgtChannelRunner` 참조 없음
- `FakePlc` 참조 없음
- 실제 PLC 연결 없음

## 9. Known Limitations / Notes
- `EmptyRuntimeSnapshotProvider`는 Fake Dashboard simulator가 아니라 `RuntimeDashboardAdapter` 기본 skeleton provider입니다.
- `RuntimeDashboardAdapter()` 기본 생성자의 empty snapshot time은 `RuntimeSnapshot.Empty` 기준입니다.
- 현재 기본 UI 경로는 `FakeDashboardRuntimeAdapter`라 영향은 제한적입니다.
- 실제 Runtime provider 연결 시 provider exception에 대한 UI fallback/error 표시 정책이 필요합니다.
- async/event/lifecycle은 AH-WPF-19 이후 `DashboardViewModel` refresh 정책과 함께 검토합니다.

## 10. Next Scenario Candidates
1. AH-WPF-19: RuntimeDashboardAdapter Async/Event Contract Review
   - `GetSnapshotAsync`
   - `StartAsync` / `StopAsync`
   - `SnapshotChanged` / `EventReceived`
   - `DashboardViewModel` refresh 방식과 함께 검토

2. AH-WPF-20: Runtime Provider / Supervisor Boundary Review
   - `IRuntimeSnapshotProvider`를 실제 Supervisor 경계와 어떻게 연결할지 검토
   - `IAutomationHubSupervisor` 도입 여부 검토

3. AH-RUNTIME-01: Supervisor Skeleton
   - `IAutomationHubSupervisor`
   - InMemory/Fake supervisor
   - 실제 PLC 연결 없음

4. AH-RUNTIME-02: Runtime Telemetry Contract
   - Tx/Rx
   - ErrorCount / RecentErrorCount
   - Main Trend / Mini Trend 원천 계약 검토
