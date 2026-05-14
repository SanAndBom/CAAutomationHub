# AH-WPF-20 Closeout

## 1. Status
- ACCEPT

## 2. Scenario Goal
- 안정적인 동기 `IRuntimeDashboardAdapter` 계약을 유지한다.
- 향후 Real Runtime 연동을 위해 optional async / lifecycle / event interface skeleton을 추가한다.
- `RuntimeDashboardAdapter`에 최소 async snapshot bridge를 추가한다.
- `DashboardViewModel`과 `FakeDashboardRuntimeAdapter`에 영향을 주지 않고 확장 지점을 준비한다.

## 3. Implemented Scope
- `IAsyncRuntimeDashboardAdapter` 추가
- `IRuntimeDashboardLifecycle` 추가
- `IRuntimeDashboardEventSource` 추가
- `DashboardSnapshotChangedEventArgs` 추가
- `RuntimeDashboardAdapter`가 `IAsyncRuntimeDashboardAdapter`를 구현하도록 변경
- `GetSnapshotAsync`를 synchronous provider bridge로 구현
- 기존 `IRuntimeDashboardAdapter` 계약은 변경하지 않음
- command executor interface는 제외

## 4. Changed Files
- `src/CAAutomationHub.Wpf/Adapters/IAsyncRuntimeDashboardAdapter.cs`
- `src/CAAutomationHub.Wpf/Adapters/IRuntimeDashboardLifecycle.cs`
- `src/CAAutomationHub.Wpf/Adapters/IRuntimeDashboardEventSource.cs`
- `src/CAAutomationHub.Wpf/Adapters/RuntimeDashboardAdapter.cs`
- `src/CAAutomationHub.Wpf/Models/Dashboard/DashboardSnapshotChangedEventArgs.cs`
- `tests/CAAutomationHub.Wpf.Tests/Adapters/RuntimeDashboardAdapterOptionalInterfacesTests.cs`

## 5. Final Contracts

### 5.1 IRuntimeDashboardAdapter
- 기존 계약을 보존했다.
- `DashboardSnapshot GetSnapshot()`만 선언한다.
- async, lifecycle, event member는 추가하지 않았다.

### 5.2 IAsyncRuntimeDashboardAdapter
- `Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)`을 선언한다.
- 별도 optional extension interface로 둔다.

### 5.3 IRuntimeDashboardLifecycle
- `Task StartAsync(CancellationToken cancellationToken)`을 선언한다.
- `Task StopAsync(CancellationToken cancellationToken)`을 선언한다.
- AH-WPF-20 기준 `RuntimeDashboardAdapter`는 이 interface를 구현하지 않는다.

### 5.4 IRuntimeDashboardEventSource
- `SnapshotChanged`를 선언한다.
- `EventReceived`를 선언한다.
- UI thread affinity를 보장하지 않는다.
- Dispatcher marshaling은 향후 UI orchestration layer의 책임으로 남긴다.

### 5.5 DashboardSnapshotChangedEventArgs
- `Snapshot`을 노출한다.
- `OccurredAt`을 노출한다.
- null `Snapshot`은 `ArgumentNullException`으로 방어한다.

## 6. RuntimeDashboardAdapter Behavior
- 기존 `GetSnapshot()` 동작을 보존했다.
- `RuntimeDashboardAdapter`는 이제 `IAsyncRuntimeDashboardAdapter`를 구현한다.
- `GetSnapshotAsync` 동작:
  - `cancellationToken.ThrowIfCancellationRequested()`를 호출한다.
  - `Task.FromResult(GetSnapshot())`을 반환한다.
  - `.Result`, `.Wait()`, `Task.Run`을 사용하지 않는다.
- 현재 동작은 동기 `IRuntimeSnapshotProvider` 위의 bridge다.
- 실제 async source는 향후 Runtime provider scenario에서 다시 검토해야 한다.

## 7. Tests Added
Test file:
- `tests/CAAutomationHub.Wpf.Tests/Adapters/RuntimeDashboardAdapterOptionalInterfacesTests.cs`

Exact tests:
- `RuntimeDashboardAdapterContract_OnlyDeclaresGetSnapshot`
- `RuntimeDashboardAdapter_ImplementsAsyncOptionalInterface`
- `GetSnapshotAsync_ReturnsMappedDashboardSnapshot`
- `GetSnapshotAsync_ThrowsWhenTokenIsAlreadyCancelled`
- `DashboardSnapshotChangedEventArgs_PreservesSnapshotAndOccurredAt`
- `DashboardSnapshotChangedEventArgs_ThrowsWhenSnapshotIsNull`
- `RuntimeDashboardLifecycleContract_DeclaresStartAndStopAsync`
- `RuntimeDashboardEventSourceContract_DeclaresSnapshotAndEventEvents`
- `FakeDashboardRuntimeAdapter_IsNotForcedToImplementOptionalInterfaces`

Tests로 고정한 정책:
- 기존 `IRuntimeDashboardAdapter`는 `GetSnapshot()`만 선언한다.
- `RuntimeDashboardAdapter`는 `IAsyncRuntimeDashboardAdapter`를 구현한다.
- `GetSnapshotAsync`는 기존 mapper/provider 흐름을 사용한다.
- 이미 취소된 token은 provider 접근 전에 `OperationCanceledException`을 발생시킨다.
- lifecycle/event skeleton signature가 존재한다.
- `FakeDashboardRuntimeAdapter`는 optional interface 구현을 강제받지 않는다.
- `DashboardSnapshotChangedEventArgs`는 값을 보존하고 null snapshot을 거부한다.

## 8. Validation
Targeted tests:
- `RuntimeDashboardAdapterOptionalInterfacesTests`: 9 passed

`dotnet build CAAutomationHub.sln`

Result:
- Succeeded
- Warning 0
- Error 0

`dotnet test CAAutomationHub.sln`

Result:
- Succeeded
- 162 passed
- 0 failed

## 9. Boundary Rules
- 기존 `IRuntimeDashboardAdapter`는 변경하지 않았다.
- `DashboardViewModel`은 변경하지 않았다.
- `FakeDashboardRuntimeAdapter`는 변경하지 않았다.
- UI는 변경하지 않았다.
- Command executor는 추가하지 않았다.
- `ExecuteAsync`는 구현하지 않았다.
- `StartAsync` / `StopAsync` no-op 구현은 추가하지 않았다.
- `SnapshotChanged` actual push는 구현하지 않았다.
- `EventReceived` actual push는 구현하지 않았다.
- Runtime / Supervisor / PLC integration은 추가하지 않았다.
- `XgtDriverCore`는 참조하지 않았다.
- `XgtChannelRunner`는 참조하지 않았다.
- `FakePlc`는 참조하지 않았다.
- Runtime telemetry는 구현하지 않았다.
- `BalanceController`는 구현하지 않았다.

## 10. Known Limitations / Notes
- `GetSnapshotAsync`는 실제 async I/O가 아니라 현재 동기 provider 위의 bridge다.
- 실제 Runtime/Supervisor provider가 생기면 async source 또는 async provider contract를 다시 검토해야 한다.
- `IRuntimeDashboardEventSource`는 event contract만 제공하며 AH-WPF-20 기준 event를 raise하지 않는다.
- Adapter event는 UI thread에서 raise된다고 보장하지 않는다.
- Event-based refresh에는 Dispatcher marshaling, reentrancy protection, latest-only coalescing 정책이 필요하다.
- Command executor 도입은 Supervisor `CommandDispatcher` scenario 이후로 미룬다.
- 이 문서는 AH-WPF-20 당시 Closeout 기준 기록이다. 이후 `SnapshotChanged` wiring은 AH-WPF-22에서 진행되었으며, 최신 진행 상태는 `docs/context/WPF_RUNTIME_BRIDGE_CURRENT_STATE.md`를 우선한다.

## 11. Next Scenario Candidates
1. AH-WPF-21: Runtime Dashboard Event/Refresh Orchestration Review
   - `SnapshotChanged`를 UI에 안전하게 연결하는 방식을 검토한다.
   - Dispatcher marshal 정책을 정의한다.
   - reentrancy 정책을 정의한다.
   - latest-only coalescing 정책을 정의한다.

2. AH-WPF-22: Runtime Provider / Supervisor Boundary Review
   - `IRuntimeSnapshotProvider`를 `IAutomationHubSupervisor`와 어떻게 연결할지 검토한다.
   - supervisor snapshot provider adapter를 설계한다.

3. AH-RUNTIME-01: Supervisor Skeleton
   - `IAutomationHubSupervisor`를 도입한다.
   - in-memory/fake supervisor를 추가한다.
   - 실제 PLC는 연결하지 않는다.

4. AH-RUNTIME-02: Runtime Command Executor Skeleton
   - `ExecuteAsync`를 도입한다.
   - `RuntimeDashboardCommandResult`를 반환한다.
   - `TestConnection` / `ResetConnection` / `ManualReconnect` skeleton을 추가한다.
