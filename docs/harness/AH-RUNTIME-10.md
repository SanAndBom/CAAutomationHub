# AH-RUNTIME-10 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-10의 목표는 AH-RUNTIME-09에서 정리한 Runtime mode read-only 정책을 WPF composition result에 안전하게 표현하는 것입니다.

이번 단계는 실제 UI button disable이나 DashboardViewModel command wiring을 수행하는 단계가 아니라, Fake mode와 InMemoryRuntime mode의 editing capability를 composition result에 표현하는 skeleton 단계입니다.

## 3. Scope

이번 단계에 포함된 항목:

- DashboardRuntimeCapabilities 추가
- Editable capability 추가
- ReadOnly capability 추가
- CanAddPlc / CanEditPlc / CanDeletePlc 세분화
- CanEditConfiguration 계산 property 추가
- DashboardRuntimeComposition에 Capabilities 필수 속성 추가
- DashboardRuntimeComposition constructor null guard 추가
- DashboardRuntimeCompositionFactory mode별 capability 설정
- Fake mode는 Editable capability 반환
- InMemoryRuntime mode는 ReadOnly capability 반환
- DashboardRuntimeCompositionFactoryTests 보강

## 4. Result

구현 결과:

- DashboardRuntimeCapabilities가 WPF Composition 영역에 추가됨
- DashboardRuntimeCapabilities.Editable은 CanAddPlc, CanEditPlc, CanDeletePlc 모두 true
- DashboardRuntimeCapabilities.ReadOnly는 CanAddPlc, CanEditPlc, CanDeletePlc 모두 false
- CanEditConfiguration은 저장값이 아니라 계산 property로 구현됨
- DashboardRuntimeComposition.Capabilities가 추가됨
- DashboardRuntimeComposition은 Capabilities를 필수 constructor 인자로 받음
- capabilities null guard가 추가됨
- Create(Fake)는 DashboardRuntimeCapabilities.Editable을 사용함
- Create(InMemoryRuntime)는 DashboardRuntimeCapabilities.ReadOnly를 사용함
- createFakeAdapter delegate 흐름은 유지됨
- factory.Create(...)는 StartAsync / RefreshAsync / StopAsync / GetSnapshotAsync를 호출하지 않음

## 5. Changed Files

아래 변경 파일을 기록합니다.

- src/CAAutomationHub.Wpf/Composition/DashboardRuntimeCapabilities.cs
- src/CAAutomationHub.Wpf/Composition/DashboardRuntimeComposition.cs
- src/CAAutomationHub.Wpf/Composition/DashboardRuntimeCompositionFactory.cs
- tests/CAAutomationHub.Wpf.Tests/Composition/DashboardRuntimeCompositionFactoryTests.cs

## 6. Boundary

이번 단계에서 유지된 경계:

- capability는 WPF Composition 영역에만 존재함
- Contracts 프로젝트에 capability를 추가하지 않음
- Runtime 프로젝트에 capability를 추가하지 않음
- DashboardSnapshot에 capability를 추가하지 않음
- RuntimeSnapshot에 capability를 추가하지 않음
- RuntimeDashboardAdapter에 capability 책임을 추가하지 않음
- FakeDashboardRuntimeAdapter에 capability 책임을 추가하지 않음
- DashboardViewModel을 변경하지 않음
- UI button disable 구현을 하지 않음
- Add/Edit/Delete runtime command 전환을 하지 않음
- Runtime command dispatcher를 만들지 않음
- actual App.xaml.cs wiring을 하지 않음
- DI 변경을 하지 않음
- app setting / command line mode switch를 구현하지 않음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- actual App.xaml.cs wiring
- DI container 구성 변경
- app setting 기반 mode switch 구현
- command line 기반 mode switch 구현
- 실제 앱 기본 모드를 Runtime으로 변경
- DashboardViewModel 변경
- ViewModel command enable/disable wiring
- UI button disable
- RuntimeDashboardAdapter 변경
- FakeDashboardRuntimeAdapter 동작 변경
- Add/Edit/Delete runtime command 전환
- Add/Edit/Delete disable 실제 UI 구현
- Runtime command dispatcher 구현
- Runtime Event Bridge 구현
- EventReceived 연결
- Runtime telemetry 구현
- PLC / XGT / FakePlc 연결
- polling scheduler 구현
- channel registry 구현
- lifecycle StartAsync actual 호출
- app shutdown StopAsync actual 호출
- sample PLC card 생성
- fake dashboard replacement
- DashboardSnapshot capability 추가
- RuntimeSnapshot capability 추가
- Contracts capability 추가
- Runtime project capability 추가

## 8. Validation

실행한 검증:

- dotnet test tests\CAAutomationHub.Wpf.Tests\CAAutomationHub.Wpf.Tests.csproj --filter DashboardRuntimeCompositionFactoryTests
- dotnet build CAAutomationHub.sln
- dotnet test CAAutomationHub.sln
- dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference
- git status --short -uall

검증 결과:

- DashboardRuntimeCompositionFactoryTests 15개 통과
- build 성공
- 전체 test 성공
- Wpf.Tests 214개 통과
- Runtime.Tests 19개 통과
- Runtime 프로젝트 참조는 CAAutomationHub.Contracts 하나뿐임
- 변경 파일은 WPF Composition과 해당 WPF 테스트로 제한됨
- DashboardViewModel 변경 없음
- RuntimeDashboardAdapter 변경 없음
- FakeDashboardRuntimeAdapter 변경 없음
- DashboardSnapshot 변경 없음
- RuntimeSnapshot 변경 없음
- Contracts 변경 없음

## 9. ACCEPT Decision

ACCEPT

이유:

- AH-RUNTIME-10 목표였던 composition-level capability skeleton이 추가됨
- Fake mode와 InMemoryRuntime mode의 editing policy가 composition result에 표현됨
- Fake mode는 Editable, InMemoryRuntime mode는 ReadOnly로 명확히 분리됨
- DashboardViewModel이 DashboardRuntimeMode를 직접 알지 않도록 하는 후속 경로가 유지됨
- capability가 Runtime DTO / Snapshot / Contracts로 새지 않음
- ViewModel/UI 연결 없이 composition-level skeleton까지만 구현됨
- 빌드와 전체 테스트가 통과함

## 10. Risks / Follow-up Candidates

AH-RUNTIME-11 후보:

- DashboardViewModel capability injection plan
- Add/Edit/Delete command enable/disable wiring
- hidden InMemoryRuntime app wiring
- app setting runtime mode opt-in
- Runtime command dispatcher skeleton
- Runtime Event Bridge
- telemetry contract

## 11. Next Step

다음 단계는 실제 UI button disable로 바로 가지 말고, DashboardViewModel에 capability를 어떻게 전달할지 먼저 검토하는 것이 안전합니다.

추천 후보:

- AH-RUNTIME-11: DashboardViewModel Capability Injection Boundary Review
- AH-RUNTIME-11: Add/Edit/Delete Command Enable Policy Plan
