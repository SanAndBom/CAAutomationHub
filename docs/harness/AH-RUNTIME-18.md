# AH-RUNTIME-18 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-18의 목표는 AH-RUNTIME-12~15에서 skeleton 검증을 위해 사용했던 `InMemoryRuntimePlcChannel`의 임시 timestamp 정책을 제거하는 것입니다.

이번 단계는 Contracts 변경, `ChannelRuntimeState` DTO 변경, WPF mapper 변경, update API 구현, polling scheduler 구현이 아니라, `capturedAt` / `LastSuccessAt` / `LastFailureAt`의 의미를 분리하는 timestamp policy repair 작업입니다.

## 3. Scope

이번 단계에 포함된 항목:

- `InMemoryRuntimePlcChannel.GetState(capturedAt)`의 임시 timestamp 정책 제거
- `LastSuccessAt` / `LastFailureAt` 내부 event timestamp 상태 추가
- `capturedAt`이 `LastSuccessAt` / `LastFailureAt`을 덮어쓰지 않도록 수정
- 미설정 timestamp `null` 유지
- `RuntimeChannelRegistry.GetStates(capturedAt)`가 `capturedAt`을 전달하되 event timestamp를 변경하지 않는지 검증
- `InMemoryAutomationHubSupervisor` registry snapshot에서 `RuntimeSnapshot.CapturedAt`과 channel event timestamp가 서로 다를 수 있음을 검증
- Runtime 단위 테스트 보강

## 4. Result

구현 결과:

- `InMemoryRuntimePlcChannel.GetState(capturedAt)`는 `LastSuccessAt`을 `capturedAt`으로 덮어쓰지 않음
- `InMemoryRuntimePlcChannel.GetState(capturedAt)`는 `LastFailureAt`을 `capturedAt`으로 덮어쓰지 않음
- `LastSuccessAt`은 constructor 또는 내부 state에 저장된 event timestamp를 그대로 반환함
- `LastFailureAt`은 constructor 또는 내부 state에 저장된 event timestamp를 그대로 반환함
- `LastSuccessAt` / `LastFailureAt` 미설정 시 `null` 유지
- `capturedAt`은 현재 `ChannelRuntimeState`에 전용 필드가 없어 discard 처리함
- `RuntimeSnapshot.CapturedAt == RuntimeHealthState.CapturedAt` 정책은 유지됨
- Contracts / DTO / WPF / mapper / interface는 변경하지 않음
- update API는 추가하지 않음

## 5. Changed Files

아래 변경 파일을 기록합니다.

- `src/CAAutomationHub.Runtime/Channels/InMemoryRuntimePlcChannel.cs`
- `tests/CAAutomationHub.Runtime.Tests/Channels/InMemoryRuntimePlcChannelTests.cs`
- `tests/CAAutomationHub.Runtime.Tests/Channels/RuntimeChannelRegistryTests.cs`
- `tests/CAAutomationHub.Runtime.Tests/InMemoryAutomationHubSupervisorChannelRegistryTests.cs`

## 6. Boundary

이번 단계에서 유지된 경계:

- Contracts 변경 없음
- `ChannelRuntimeState` 변경 없음
- `RuntimeSnapshot` 변경 없음
- `RuntimeHealthState` 변경 없음
- WPF 변경 없음
- `RuntimeDashboardSnapshotMapper` 변경 없음
- `DashboardViewModel` 변경 없음
- `RuntimeDashboardAdapter` 변경 없음
- `IRuntimePlcChannel` interface 변경 없음
- `IAutomationHubSupervisor` interface 변경 없음
- `InMemoryAutomationHubSupervisor` public API 변경 없음
- channel update API 추가 없음
- polling scheduler 구현 없음
- `XgtDriverCore` 참조 추가 없음
- `XgtChannelRunner` 참조 추가 없음
- `FakePlc` 참조 추가 없음
- telemetry 구현 없음
- command dispatcher 구현 없음
- UI 변경 없음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- Contracts 변경
- `ChannelRuntimeState.CapturedAt` 추가
- `RuntimeSnapshot` DTO 변경
- `RuntimeHealthState` DTO 변경
- WPF mapper 변경
- `RuntimeDashboardSnapshotMapper` 변경
- update API 구현
- `InMemoryRuntimePlcChannel.UpdateState` / `ReplaceState` 추가
- `IWritableRuntimePlcChannel` 추가
- `RuntimeChannelRegistry` update API 추가
- polling scheduler 구현
- `XgtDriverCore` 참조 추가
- `XgtChannelRunner` 참조 추가
- `FakePlc` 참조 추가
- telemetry 구현
- command dispatcher 구현
- UI 변경
- `App.xaml.cs` wiring
- DI 변경
- `DashboardViewModel` 변경
- `RuntimeDashboardAdapter` 변경
- sample PLC card 생성
- fake dashboard replacement

## 8. Validation

실행한 검증:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter InMemoryRuntimePlcChannelTests`
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter RuntimeChannelRegistryTests`
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter InMemoryAutomationHubSupervisorChannelRegistryTests`
- `dotnet build CAAutomationHub.sln`
- `dotnet test CAAutomationHub.sln`
- `dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference`
- `git status --short -uall`
- forbidden reference 검색

검증 결과:

- `InMemoryRuntimePlcChannelTests` 5개 통과
- `RuntimeChannelRegistryTests` 6개 통과
- `InMemoryAutomationHubSupervisorChannelRegistryTests` 11개 통과
- build 성공
- 전체 test 성공
- Runtime.Tests 42개 통과
- Wpf.Tests 214개 통과
- Runtime 프로젝트 참조는 `CAAutomationHub.Contracts` 하나뿐임
- 변경 파일 4개 확인
- Contracts / WPF / DTO / interface 파일 변경 없음
- `XgtDriverCore` / `XgtChannelRunner` / `FakePlc` Runtime 구현 참조 추가 없음

## 9. Timestamp Policy Decision

정리된 timestamp 정책:

- `capturedAt`은 snapshot 관찰 / 수집 시각
- `LastSuccessAt`은 실제 통신 성공 이벤트 시각
- `LastFailureAt`은 실제 통신 실패 이벤트 시각
- `InMemoryRuntimePlcChannel.GetState(capturedAt)`는 `capturedAt`을 event timestamp 필드에 대입하지 않음
- `RuntimeSnapshot.CapturedAt` / `RuntimeHealthState.CapturedAt`은 snapshot frame timestamp로 유지
- `ChannelRuntimeState`에는 아직 channel-level `CapturedAt` 전용 필드를 추가하지 않음

## 10. ACCEPT Decision

ACCEPT

이유:

- AH-RUNTIME-18 목표였던 임시 timestamp 정책 제거가 완료됨
- `capturedAt` / `LastSuccessAt` / `LastFailureAt` 의미가 코드와 테스트에서 분리됨
- `RuntimeSnapshot.CapturedAt` / `RuntimeHealthState.CapturedAt` 일치 정책이 유지됨
- Contracts / DTO / WPF / mapper / interface 변경 없이 repair가 완료됨
- update API / polling scheduler / driver integration이 섞이지 않음
- 빌드와 전체 테스트가 통과함

## 11. Risks / Follow-up Candidates

AH-RUNTIME-19 후보:

- `InMemoryRuntimePlcChannel` concrete update API
- `IWritableRuntimePlcChannel` 필요성 재검토
- `RuntimeChannelRegistry` lookup API
- `PollingScheduler` publish path
- `XgtRuntimePlcChannelAdapter` boundary review
- `ChannelRuntimeState.CapturedAt` 추가 필요성 재검토

추가 후속 후보:

- `RuntimeDashboardSnapshotMapper` timestamp usage review
- WPF last seen / stale 표시 정책 검토
- Runtime telemetry timestamp policy
- command dispatcher skeleton

## 12. Next Step

다음 단계는 AH-RUNTIME-19: `InMemoryRuntimePlcChannel` Concrete Update API Boundary Review 또는 구현 계획입니다.

단, AH-RUNTIME-19에서도 `IRuntimePlcChannel` read-only 계약은 유지하고, update API는 우선 `InMemoryRuntimePlcChannel` concrete 수준에서 작게 시작하는 것이 안전합니다.
