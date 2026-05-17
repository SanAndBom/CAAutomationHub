# AH-RUNTIME-08 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-08의 목표는 AH-RUNTIME-07에서 추가한 DashboardRuntimeCompositionFactory를 실제 WPF 앱 시작 흐름에 언제, 어떻게 연결할지 검토하는 것입니다.

이번 단계는 actual App.xaml.cs wiring이나 DI 변경을 수행하는 단계가 아니라, Fake mode 기본 흐름을 유지하면서 InMemoryRuntime mode를 opt-in으로 연결할 수 있는 경계와 정책을 설계하는 Boundary Review입니다.

## 3. Scope

이번 단계에 포함된 검토 항목:

- Runtime mode opt-in 정책 검토
- Fake mode 기본 유지 원칙 재확인
- App.xaml.cs actual wiring 제외 결정
- DI container 변경 제외 결정
- DashboardViewModel 책임 경계 검토
- lifecycle startup/shutdown 호출 정책 검토
- StopAsync 후 Dispose 순서 검토
- InMemoryRuntime mode의 Add/Edit/Delete 의미 충돌 리스크 검토
- AH-RUNTIME-09 후보 분리

## 4. Decision

결정 사항:

- AH-RUNTIME-08은 계획 단계로 종료
- actual App.xaml.cs wiring은 하지 않음
- DI container 변경은 하지 않음
- 기본 mode는 DashboardRuntimeMode.Fake로 유지
- InMemoryRuntime은 명시적 opt-in 후보로만 둠
- appsettings 기반 mode switch는 아직 도입하지 않음
- command line 기반 mode switch는 아직 도입하지 않음
- compile constant 기반 mode switch는 아직 도입하지 않음
- DashboardViewModel은 IRuntimeDashboardAdapter만 알도록 유지
- DashboardViewModel이 DashboardRuntimeComposition, IRuntimeDashboardLifecycle, DashboardRuntimeMode를 알게 하지 않음
- lifecycle StartAsync / StopAsync actual 호출은 AH-RUNTIME-09 이후로 분리
- Runtime mode actual wiring 전에 Add/Edit/Delete 의미 충돌을 먼저 다루는 것이 안전함

## 5. Runtime Mode Opt-in Policy

정책:

- 기본값은 항상 Fake
- 사용자가 아무 설정 없이 앱을 실행하면 기존 FakeDashboardRuntimeAdapter 흐름이 유지되어야 함
- InMemoryRuntime은 명시적 opt-in 후보
- AH-RUNTIME-08에서는 opt-in 설정 구현을 하지 않음
- appsettings / command line / compile constant 전환은 후속 후보로 남김

## 6. Lifecycle / Shutdown Policy

검토된 순서:

- startup: composition.Lifecycle?.StartAsync(...)
- shutdown: composition.Lifecycle?.StopAsync(...)
- cleanup: composition.Dispose()

결정:

- StopAsync와 Dispose 책임은 분리함
- StopAsync는 Runtime 정지 책임
- Dispose는 event subscription 해제 등 동기 cleanup 책임
- StopAsync 실패 여부와 관계없이 Dispose는 finally에서 호출하는 방향이 안전함
- fire-and-forget startup은 피하는 방향으로 검토
- WPF startup에서 async 호출을 어떻게 await할지는 후속 단계에서 결정

## 7. Add/Edit/Delete Risk

리스크:

- 현재 Add/Edit/Delete는 FakeDashboardRuntimeAdapter 기반 configuration 흐름에 속함
- RuntimeDashboardAdapter는 snapshot mapping 중심임
- Runtime command dispatcher가 아직 없음
- InMemoryRuntime mode를 실제 UI에 연결하면 Add/Edit/Delete 의미 충돌 가능성이 있음

후속 결정 후보:

- InMemoryRuntime mode에서는 read-only dashboard로 제한
- Add/Edit/Delete 비활성화
- Runtime command 책임이 생길 때까지 actual app opt-in 보류
- hidden/testing mode에서만 허용

## 8. Boundary

이번 단계에서 유지된 경계:

- App.xaml.cs 변경 없음
- DI 변경 없음
- DashboardViewModel 변경 없음
- RuntimeDashboardAdapter 변경 없음
- FakeDashboardRuntimeAdapter 변경 없음
- 실제 앱 기본 모드를 Runtime으로 변경하지 않음
- lifecycle StartAsync actual 호출 없음
- app shutdown StopAsync actual 호출 없음
- Runtime Event Bridge 없음
- Runtime command dispatcher 없음
- EventReceived 연결 없음
- PLC / XGT / FakePlc 연결 없음
- polling scheduler 없음
- channel registry 없음
- telemetry 없음
- UI 변경 없음
- fake dashboard replacement 없음

## 9. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- actual App.xaml.cs wiring
- DI container 구성 변경
- app setting 기반 mode switch 구현
- command line 기반 mode switch 구현
- compile constant 기반 mode switch 구현
- 실제 앱 기본 모드를 Runtime으로 변경
- DashboardViewModel 변경
- RuntimeDashboardAdapter 변경
- FakeDashboardRuntimeAdapter 동작 변경
- Add/Edit/Delete runtime command 전환
- Add/Edit/Delete disable 정책 구현
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

## 10. Validation

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
- Add/Edit/Delete 의미 충돌 리스크가 historical record에 남음

## 11. ACCEPT Decision

ACCEPT

이유:

- actual App wiring을 지금 하지 않기로 결정함
- Fake 기본값 유지 원칙을 재확인함
- InMemoryRuntime은 opt-in 후보로만 유지함
- DashboardViewModel이 lifecycle / mode / composition을 모르도록 경계를 유지함
- startup/shutdown lifecycle 정책 초안을 정리함
- StopAsync 후 Dispose 순서 원칙을 정리함
- Add/Edit/Delete UI 의미 충돌 리스크를 기록함
- AH-RUNTIME-09를 actual wiring 또는 그 전 정책 확정 단계로 분리함

## 12. Risks / Follow-up Candidates

AH-RUNTIME-09 후보:

- Runtime mode에서 Add/Edit/Delete를 read-only / disabled / hidden testing 중 어떻게 처리할지 정책 확정
- actual App.xaml.cs wiring 계획
- app setting 기반 runtime mode opt-in 계획
- WpfApplicationRuntimeComposition service 설계
- startup lifecycle StartAsync actual invocation
- shutdown StopAsync / Dispose ordering
- Fake fallback 정책
- Runtime command dispatcher skeleton
- Runtime Event Bridge
- telemetry contract
- polling scheduler
- channel registry

## 13. Next Step

추천 다음 단계:
AH-RUNTIME-09는 바로 actual wiring으로 가기보다, Runtime mode에서 Add/Edit/Delete 의미 충돌을 먼저 정책으로 확정하는 단계가 안전합니다.

후보:

- AH-RUNTIME-09: Runtime Mode Read-only / Add/Edit/Delete Boundary Review
- AH-RUNTIME-09: WpfApplicationRuntimeComposition Service Plan
- AH-RUNTIME-09: actual App.xaml.cs wiring Plan
