# AH-RUNTIME-22 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-22의 목표는 `RuntimeChannelRegistry`에 특정 `PlcId`의 runtime channel을 찾는 lookup API를 추가하는 것입니다.

이번 단계는 update API, writable interface, polling scheduler, `XgtDriverCore` 연결, `XgtChannelRunner` 연결, `FakePlc` integration을 구현하는 단계가 아니라, `RuntimeChannelRegistry`의 lookup 경계를 작게 추가하는 단계입니다.

## 3. Scope

이번 단계에 포함된 항목:

- `RuntimeChannelRegistry.TryGetChannel(string plcId, out IRuntimePlcChannel channel)` 추가
- `plcId` validation 정책 추가
- existing `plcId` lookup 정책 추가
- missing `plcId` false 반환 정책 추가
- `StringComparer.Ordinal` lookup 정책 유지
- concrete channel 반환 금지
- `channel.GetState(...)` 미호출 검증
- `GetChannels` / `GetStates` 기존 동작 유지 검증
- `RuntimeChannelRegistryTests` 보강

## 4. Result

구현 결과:

- `RuntimeChannelRegistry.TryGetChannel` API가 추가됨
- `plcId == null`은 `ArgumentNullException` 발생
- empty / whitespace `plcId`는 `ArgumentException` 발생
- existing `plcId`는 `true`를 반환함
- existing `plcId`는 등록된 `IRuntimePlcChannel` reference를 반환함
- missing valid `plcId`는 `false`를 반환함
- missing valid `plcId`는 out channel에 `null`을 반환함
- 기존 `_channelsByPlcId`의 `StringComparer.Ordinal` 정책을 그대로 사용함
- 반환 타입은 concrete가 아닌 `IRuntimePlcChannel`
- registry lock 안에서는 dictionary lookup만 수행함
- `TryGetChannel`은 `channel.GetState(...)`를 호출하지 않음
- `TryGetChannel`은 update를 수행하지 않음
- `TryGetChannel`은 publish를 수행하지 않음
- 기존 `GetChannels()` snapshot copy와 `GetStates(capturedAt)` 동작은 유지됨

## 5. Changed Files

아래 변경 파일을 기록합니다.

- `src/CAAutomationHub.Runtime/Channels/RuntimeChannelRegistry.cs`
- `tests/CAAutomationHub.Runtime.Tests/Channels/RuntimeChannelRegistryTests.cs`

## 6. Boundary

이번 단계에서 유지된 경계:

- `IRuntimePlcChannel` interface 변경 없음
- `IAutomationHubSupervisor` interface 변경 없음
- `InMemoryRuntimePlcChannel` 변경 없음
- `IWritableRuntimePlcChannel` 추가 없음
- `RuntimeChannelRegistry` update API 추가 없음
- `GetChannel` convenience API 추가 없음
- `Contains` API 추가 없음
- polling scheduler 구현 없음
- `XgtDriverCore` 참조 추가 없음
- `XgtChannelRunner` 참조 추가 없음
- `FakePlc` 참조 추가 없음
- command dispatcher 구현 없음
- Runtime Event Bridge 구현 없음
- telemetry 구현 없음
- WPF 변경 없음
- `App.xaml.cs` wiring 없음
- DI 변경 없음
- `DashboardViewModel` 변경 없음
- `RuntimeDashboardAdapter` 변경 없음
- Contracts / DTO 변경 없음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- `IRuntimePlcChannel` 변경
- `IAutomationHubSupervisor` 변경
- `InMemoryRuntimePlcChannel` 변경
- `IWritableRuntimePlcChannel` 추가
- `RuntimeChannelRegistry` update API
- `GetChannel` convenience API
- `Contains` API
- polling scheduler 구현
- `XgtDriverCore` 참조 추가
- `XgtChannelRunner` 참조 추가
- `FakePlc` 참조 추가
- command dispatcher 구현
- Runtime Event Bridge 구현
- telemetry 구현
- WPF 변경
- `App.xaml.cs` wiring
- DI 변경
- `DashboardViewModel` 변경
- `RuntimeDashboardAdapter` 변경
- channel update 구현
- auto publish
- `RefreshSnapshotAsync` 자동 호출

## 8. Validation

실행한 검증:

- `git status --short -uall`
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter RuntimeChannelRegistryTests`
- TDD RED: `TryGetChannel` 미정의 CS1061 실패 확인
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter RuntimeChannelRegistryTests`
- `dotnet build CAAutomationHub.sln`
- `dotnet test CAAutomationHub.sln`
- `dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference`
- `git status --short -uall`

검증 결과:

- `RuntimeChannelRegistryTests` 13개 통과
- build 성공
- 전체 test 성공
- Runtime.Tests 54개 통과
- Wpf.Tests 214개 통과
- Runtime 프로젝트 참조는 `CAAutomationHub.Contracts` 하나뿐임
- 변경 파일 2개 확인
- WPF 변경 없음
- Contracts / DTO 변경 없음
- `IRuntimePlcChannel` 변경 없음
- `IAutomationHubSupervisor` 변경 없음
- `InMemoryRuntimePlcChannel` 변경 없음

## 9. ACCEPT Decision

ACCEPT

이유:

- AH-RUNTIME-22 목표였던 `RuntimeChannelRegistry` lookup API가 추가됨
- `TryGetChannel`이 lookup 책임만 수행함
- update / publish 책임이 registry에 섞이지 않음
- 반환 타입이 `IRuntimePlcChannel`로 유지됨
- concrete channel 반환을 피함
- duplicate detection과 lookup comparison policy가 일치함
- 기존 `GetChannels` / `GetStates` 동작이 유지됨
- Runtime -> Contracts 단일 참조 경계가 유지됨
- WPF / Driver / `FakePlc` / Scheduler / Command / Telemetry가 섞이지 않음
- 빌드와 전체 테스트가 통과함

## 10. Risks / Follow-up Candidates

AH-RUNTIME-23 후보:

- `IWritableRuntimePlcChannel` Boundary Review
- `PollingScheduler` publish path
- `XgtRuntimePlcChannelAdapter` boundary review
- `FakePlc` integration boundary review
- `RuntimeChannelRegistry` GetChannel convenience API
- `RuntimeChannelRegistry` Contains API

추가 후속 후보:

- polling result -> `registry.TryGetChannel` -> writable channel update -> `supervisor.RefreshSnapshotAsync` orchestration
- Runtime Event Bridge
- telemetry contract
- command dispatcher skeleton

## 11. Next Step

다음 단계는 AH-RUNTIME-23: `IWritableRuntimePlcChannel Boundary Review`가 자연스럽습니다.

이유:

- `TryGetChannel`은 `IRuntimePlcChannel`을 반환함
- `IRuntimePlcChannel`은 read-only 계약임
- `InMemoryRuntimePlcChannel.ReplaceState`는 concrete API임
- polling scheduler가 channel을 안전하게 update하려면 writable boundary가 필요한지 검토해야 함
