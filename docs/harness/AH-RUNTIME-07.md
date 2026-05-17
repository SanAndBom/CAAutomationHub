# AH-RUNTIME-07 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-07의 목표는 actual App.xaml.cs wiring이나 DI 변경 없이, Fake mode와 InMemoryRuntime mode를 조립할 수 있는 DashboardRuntimeCompositionFactory skeleton을 추가하는 것입니다.

구체적으로는:

- DashboardRuntimeMode 추가
- DashboardRuntimeComposition 추가
- DashboardRuntimeCompositionFactory 추가
- Fake mode는 createFakeAdapter delegate로 기존 fake 생성 흐름 보호
- InMemoryRuntime mode는 Runtime rail 객체를 생성만 함
- 생성과 StartAsync / RefreshAsync / StopAsync 호출 분리
- App.xaml.cs wiring / DI 변경 / 기본 Runtime 전환은 제외

## 3. Scope

이번 단계에 포함된 항목:

- src/CAAutomationHub.Wpf/Composition/DashboardRuntimeMode.cs 추가
- src/CAAutomationHub.Wpf/Composition/DashboardRuntimeComposition.cs 추가
- src/CAAutomationHub.Wpf/Composition/DashboardRuntimeCompositionFactory.cs 추가
- tests/CAAutomationHub.Wpf.Tests/Composition/DashboardRuntimeCompositionFactoryTests.cs 추가
- Fake / InMemoryRuntime mode enum 정의
- composition result 객체 정의
- composition result IDisposable 정책 정의
- fake adapter delegate 기반 Fake mode 조립
- InMemoryRuntime mode runtime rail 조립
- factory 생성 시 lifecycle / refresh / stop 미호출 검증

## 4. Result

구현 결과:

- DashboardRuntimeMode가 추가됨
- DashboardRuntimeMode 값은 Fake, InMemoryRuntime 두 개뿐임
- RealRuntime mode는 추가하지 않음
- DashboardRuntimeComposition이 추가됨
- DashboardRuntimeComposition은 Mode, Adapter, optional Lifecycle을 노출함
- DashboardRuntimeComposition은 IDisposable을 구현함
- Dispose는 내부 IDisposable 동기 cleanup만 수행함
- Dispose는 Lifecycle.StopAsync를 호출하지 않음
- DashboardRuntimeCompositionFactory가 추가됨
- factory constructor는 Func<IRuntimeDashboardAdapter> createFakeAdapter를 받음
- Create(Fake)는 createFakeAdapter delegate가 반환한 adapter를 사용함
- Create(Fake)의 Lifecycle은 null임
- Create(InMemoryRuntime)는 InMemoryAutomationHubSupervisor, SupervisorRuntimeSnapshotProvider, RuntimeDashboardAdapter, SupervisorRuntimeDashboardLifecycle을 조립함
- factory.Create(...)는 StartAsync / RefreshAsync / StopAsync / GetSnapshotAsync를 호출하지 않음
- App.xaml.cs는 변경하지 않음
- DI 구성은 변경하지 않음
- DashboardViewModel은 변경하지 않음
- RuntimeDashboardAdapter는 변경하지 않음
- FakeDashboardRuntimeAdapter는 변경하지 않음

## 5. Changed Files

아래 변경 파일을 기록합니다.

- src/CAAutomationHub.Wpf/Composition/DashboardRuntimeMode.cs
- src/CAAutomationHub.Wpf/Composition/DashboardRuntimeComposition.cs
- src/CAAutomationHub.Wpf/Composition/DashboardRuntimeCompositionFactory.cs
- tests/CAAutomationHub.Wpf.Tests/Composition/DashboardRuntimeCompositionFactoryTests.cs

## 6. Boundary

이번 단계에서 유지된 경계:

- Composition factory는 WPF 프로젝트 내부에 위치함
- Runtime 프로젝트는 WPF를 참조하지 않음
- Runtime 프로젝트는 Contracts만 참조함
- FakeDashboardRuntimeAdapter 동작을 변경하지 않음
- RuntimeDashboardAdapter 동작을 변경하지 않음
- DashboardViewModel을 변경하지 않음
- App.xaml.cs를 변경하지 않음
- DI container를 변경하지 않음
- app setting 기반 mode switch를 구현하지 않음
- 실제 앱 기본 모드를 Runtime으로 바꾸지 않음
- factory.Create(...)는 StartAsync를 호출하지 않음
- factory.Create(...)는 RefreshAsync를 호출하지 않음
- factory.Create(...)는 StopAsync를 호출하지 않음
- DashboardRuntimeComposition.Dispose는 StopAsync를 호출하지 않음
- lifecycle 호출은 후속 actual App wiring 단계로 남김
- Add/Edit/Delete runtime command 전환은 하지 않음
- Runtime Event Bridge는 만들지 않음
- EventReceived는 연결하지 않음
- PLC / XGT / FakePlc 연결은 하지 않음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- actual App.xaml.cs wiring
- DI container 구성 변경
- app setting 기반 mode switch 구현
- command line 기반 mode switch 구현
- 실제 앱 기본 모드를 Runtime으로 변경
- DashboardViewModel 변경
- RuntimeDashboardAdapter 변경
- FakeDashboardRuntimeAdapter 동작 변경
- Add/Edit/Delete runtime command 전환
- Runtime command dispatcher 구현
- Runtime Event Bridge 구현
- EventReceived 연결
- Runtime telemetry 구현
- PLC / XGT / FakePlc 연결
- polling scheduler 구현
- channel registry 구현
- lifecycle StartAsync actual 호출
- app shutdown StopAsync actual 호출
- UI 변경
- sample PLC card 생성
- fake dashboard replacement
- RealRuntime mode 추가

## 8. Validation

실행한 검증:

- dotnet test tests\CAAutomationHub.Wpf.Tests\CAAutomationHub.Wpf.Tests.csproj --filter DashboardRuntimeCompositionFactoryTests
- dotnet build CAAutomationHub.sln
- dotnet test CAAutomationHub.sln
- dotnet list src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj reference
- git status --short -uall
- App.xaml.cs / DI / DashboardViewModel / RuntimeDashboardAdapter / FakeDashboardRuntimeAdapter 변경 확인
- Create(InMemoryRuntime)에서 StartAsync / RefreshAsync / StopAsync / GetSnapshotAsync 미호출 확인
- RealRuntime 미추가 확인

검증 결과:

- DashboardRuntimeCompositionFactoryTests 8개 통과
- build 성공
- 전체 test 성공
- Runtime.Tests 19개 통과
- Wpf.Tests 207개 통과
- Runtime 프로젝트 참조는 CAAutomationHub.Contracts 하나뿐임
- 실제 변경 파일 4개 확인
- App.xaml.cs 변경 없음
- DI 관련 변경 없음
- DashboardViewModel 변경 없음
- RuntimeDashboardAdapter 변경 없음
- FakeDashboardRuntimeAdapter 변경 없음
- DashboardRuntimeMode 값은 Fake, InMemoryRuntime 두 개뿐임
- repo 전체 RealRuntime match 없음
- UI/보고상 중복 표시는 단순 중복 노출로 확인

## 9. ACCEPT Decision

ACCEPT

이유:

- AH-RUNTIME-07 목표였던 composition factory skeleton이 추가됨
- Fake mode와 InMemoryRuntime mode의 조립 경계가 분리됨
- Fake mode는 createFakeAdapter delegate로 기존 fake 생성 흐름을 보호함
- InMemoryRuntime mode는 runtime rail 객체를 생성만 함
- 생성과 lifecycle actual invocation이 분리됨
- App.xaml.cs / DI / ViewModel / Adapter 변경이 섞이지 않음
- RealRuntime mode를 성급히 추가하지 않음
- Runtime -> Contracts 단일 참조 경계가 유지됨
- 빌드와 전체 테스트가 통과함

## 10. Risks / Follow-up Candidates

AH-RUNTIME-08 후보:

- actual App.xaml.cs wiring
- app setting 기반 runtime mode opt-in
- lifecycle StartAsync actual invocation
- shutdown StopAsync / Dispose ordering
- Runtime Event Bridge
- command dispatcher skeleton
- telemetry contract
- polling scheduler
- channel registry
- Runtime mode에서 Add/Edit/Delete UI 의미 충돌 검토

## 11. Next Step

다음 단계는 composition factory를 실제 앱에 물릴지, 또는 Runtime Event / Command / Telemetry skeleton을 먼저 정리할지 결정하는 것입니다.

우선 후보:

- AH-RUNTIME-08: actual App.xaml.cs wiring 계획
- AH-RUNTIME-08: app setting 기반 runtime mode opt-in 계획
- AH-RUNTIME-08: Runtime Event Bridge skeleton 계획
- AH-RUNTIME-08: command dispatcher skeleton 계획
