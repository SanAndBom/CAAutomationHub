# AH-RUNTIME-06 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-06의 목표는 AH-RUNTIME-01~05에서 만든 Runtime source / provider / lifecycle / adapter 레일을 실제 WPF 앱 조립 흐름에 어떻게 연결할지 검토하는 것입니다.

이번 단계는 actual App.xaml.cs wiring이나 DI 변경을 수행하는 단계가 아니라, WPF Composition Root와 Runtime Mode 전환 경계를 설계하는 Boundary Review입니다.

## 3. Scope

이번 단계에 포함된 검토 항목:

- WPF Composition Root 위치 검토
- Fake mode / InMemoryRuntime mode 공존 전략 검토
- DashboardRuntimeCompositionFactory 후보 검토
- DashboardRuntimeMode 후보 검토
- DashboardRuntimeComposition result 후보 검토
- lifecycle 생성과 StartAsync 호출 분리 검토
- FakeDashboardRuntimeAdapter 기본 흐름 유지 검토
- RuntimeDashboardAdapter 책임 유지 검토
- actual App.xaml.cs wiring 제외 결정
- DI container 변경 제외 결정

## 4. Decision

결정 사항:

- AH-RUNTIME-06은 계획 단계로 종료
- 후속 구현은 AH-RUNTIME-07로 분리
- 후속 구현 범위는 Composition factory skeleton까지만 권장
- 기본 모드는 Fake로 유지
- InMemoryRuntime mode는 opt-in 후보로만 둠
- actual App.xaml.cs wiring은 제외
- DI container 변경은 제외
- app setting 기반 mode switch는 제외
- DashboardViewModel 변경은 제외
- RuntimeDashboardAdapter 변경은 제외
- FakeDashboardRuntimeAdapter 동작 변경은 제외
- lifecycle StartAsync / StopAsync actual 호출은 제외

## 5. Recommended Next Implementation Scope

AH-RUNTIME-07 후보:
DashboardRuntimeCompositionFactory Skeleton

후속 구현 후보 파일:

- src/CAAutomationHub.Wpf/Composition/DashboardRuntimeMode.cs
- src/CAAutomationHub.Wpf/Composition/DashboardRuntimeComposition.cs
- src/CAAutomationHub.Wpf/Composition/DashboardRuntimeCompositionFactory.cs
- tests/CAAutomationHub.Wpf.Tests/Composition/DashboardRuntimeCompositionFactoryTests.cs

후속 구현 범위:

- Fake mode에서 FakeDashboardRuntimeAdapter 조립 가능성 검증
- InMemoryRuntime mode에서 InMemoryAutomationHubSupervisor / SupervisorRuntimeSnapshotProvider / SupervisorRuntimeDashboardLifecycle / RuntimeDashboardAdapter 조립 가능성 검증
- factory는 StartAsync를 호출하지 않음
- composition result가 adapter / lifecycle / disposable 책임을 분리해 노출
- actual App.xaml.cs wiring은 하지 않음
- DI 변경은 하지 않음

## 6. Boundary

이번 단계에서 유지된 경계:

- FakeDashboardRuntimeAdapter 기본 흐름 유지
- RuntimeDashboardAdapter는 mapping 역할 유지
- DashboardViewModel은 runtime lifecycle을 모름
- App.xaml.cs는 변경하지 않음
- DI container는 변경하지 않음
- Runtime mode를 앱 기본값으로 전환하지 않음
- Add/Edit/Delete runtime command 전환 없음
- Runtime command dispatcher 없음
- Runtime Event Bridge 없음
- EventReceived 연결 없음
- PLC / XGT / FakePlc 연결 없음
- polling scheduler 없음
- channel registry 없음
- telemetry 없음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- actual App.xaml.cs wiring
- DI container 구성 변경
- app setting 기반 mode switch 구현
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

## 8. Validation

이번 단계는 계획 / Boundary Review 단계입니다.

검증 기준:

- 코드 수정 없음
- 파일 생성은 Closeout 문서만 허용
- actual App wiring 없음
- DI 변경 없음
- DashboardViewModel 변경 없음
- RuntimeDashboardAdapter 변경 없음
- FakeDashboardRuntimeAdapter 변경 없음
- Runtime mode 기본 전환 없음

## 9. ACCEPT Decision

ACCEPT

이유:

- composition root 위치 전략이 factory/service 후보 중심으로 정리됨
- Fake mode 기본 유지 원칙이 확정됨
- Runtime mode는 opt-in 후보로만 설계됨
- actual App.xaml.cs wiring 제외가 명확해짐
- DI 변경 제외가 명확해짐
- DashboardViewModel과 RuntimeDashboardAdapter 책임이 유지됨
- lifecycle 호출은 후속 App wiring 단계로 분리됨
- 후속 AH-RUNTIME-07 구현 범위가 Composition factory skeleton으로 정리됨

## 10. Risks / Follow-up Candidates

AH-RUNTIME-07 후보:

- DashboardRuntimeMode
- DashboardRuntimeComposition
- DashboardRuntimeCompositionFactory
- Composition factory tests
- Fake mode / InMemoryRuntime mode 조립 검증
- 생성과 StartAsync 호출 분리 검증
- actual App.xaml.cs wiring 제외 검증

추가 후속 후보:

- actual App wiring
- app setting 기반 Runtime mode opt-in
- Runtime Event Bridge
- Runtime command dispatcher
- telemetry contract
- polling scheduler
- channel registry

## 11. Next Step

다음 단계는 AH-RUNTIME-07: DashboardRuntimeCompositionFactory Skeleton 구현 계획입니다.

단, AH-RUNTIME-07에서도 actual App.xaml.cs wiring과 DI 변경은 제외하고, factory skeleton과 테스트까지만 진행하는 것이 안전합니다.
