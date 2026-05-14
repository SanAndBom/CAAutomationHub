# AH-RUNTIME-09 Closeout

## 1. Status

ACCEPT

## 2. Scenario Goal

AH-RUNTIME-09의 목표는 InMemoryRuntime mode를 실제 WPF 앱 흐름에 연결하기 전에, 기존 Add/Edit/Delete UI와 Runtime mode 사이의 의미 충돌을 정책으로 정리하는 것입니다.

이번 단계는 actual App.xaml.cs wiring이나 DI 변경을 수행하는 단계가 아니라, Runtime mode에서 Dashboard를 read-only로 볼지, Add/Edit/Delete를 disable해야 할지, hidden/testing opt-in으로 제한할지 정책을 확정하는 Boundary Review입니다.

## 3. Scope

이번 단계에 포함된 검토 항목:

- Fake mode와 Runtime mode의 Add/Edit/Delete 책임 차이 검토
- Fake mode의 기존 Add/Edit/Delete 흐름 유지 결정
- InMemoryRuntime mode의 read-only dashboard 정책 검토
- Runtime command dispatcher 전까지 mutation command 보류 결정
- unsupported 메시지 방식과 UI disable 방식 비교
- DashboardViewModel이 DashboardRuntimeMode를 직접 알지 않는 경계 검토
- composition-level capability 후보 검토
- AH-RUNTIME-10 후보를 DashboardRuntimeCapabilities skeleton으로 분리

## 4. Decision

결정 사항:

- AH-RUNTIME-09는 계획 단계로 종료
- Fake mode는 기존 Add/Edit/Delete 흐름을 유지
- FakeDashboardRuntimeAdapter 기반 prototype/configuration 조작 흐름은 유지
- InMemoryRuntime mode는 command dispatcher가 생기기 전까지 read-only dashboard로 정의
- Runtime mode에서 Add/Edit/Delete는 우선 비활성화 방향이 안전함
- 클릭 후 unsupported 메시지를 보여주는 방식은 사용자 혼란 가능성이 있어 기본안으로 두지 않음
- 실제 Add/Edit/Delete button disable 구현은 이번 단계에서 하지 않음
- Runtime command dispatcher 구현 전까지 Add/Edit/Delete runtime command 전환은 보류
- Runtime mode는 hidden/testing opt-in 후보로 유지
- actual App.xaml.cs wiring, DI 변경, app setting 기반 mode switch는 계속 제외

## 5. Capability Direction

정책 표현 방향:

- DashboardViewModel이 DashboardRuntimeMode enum을 직접 아는 구조는 피함
- ViewModel에는 mode 이름이 아니라 capability 또는 command availability만 전달하는 방향을 검토
- DashboardSnapshot에 UI capability를 넣는 것은 snapshot 데이터와 UI 정책이 섞일 수 있어 비추천
- RuntimeDashboardAdapter가 editing capability 책임까지 맡는 것도 비추천
- 가장 안전한 후보는 composition-level capability

후속 후보:

- DashboardRuntimeCapabilities 추가
- CanAddPlc / CanEditPlc / CanDeletePlc
- 또는 단순한 CanEditConfiguration
- DashboardRuntimeComposition에 capability 포함
- DashboardRuntimeCompositionFactory가 mode별 capability 결정
- Fake mode: editing capability true
- InMemoryRuntime mode: editing capability false

## 6. Boundary

이번 단계에서 유지된 경계:

- App.xaml.cs 변경 없음
- DI 변경 없음
- DashboardViewModel 변경 없음
- RuntimeDashboardAdapter 변경 없음
- FakeDashboardRuntimeAdapter 변경 없음
- 실제 Add/Edit/Delete button disable 구현 없음
- Runtime command dispatcher 구현 없음
- Add/Edit/Delete runtime command 전환 없음
- Runtime Event Bridge 없음
- EventReceived 연결 없음
- Runtime telemetry 없음
- PLC / XGT / FakePlc 연결 없음
- polling scheduler 없음
- channel registry 없음
- actual App wiring 없음
- app setting / command line mode switch 없음

## 7. Excluded Scope

이번 단계에서 의도적으로 제외한 항목:

- 코드 수정
- 파일 생성/수정, Closeout 문서 제외
- 테스트 추가
- 명령 실행
- actual App.xaml.cs wiring
- DI container 구성 변경
- app setting 기반 mode switch 구현
- command line 기반 mode switch 구현
- 실제 앱 기본 모드를 Runtime으로 변경
- DashboardViewModel 변경
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
- Runtime mode에서 Add/Edit/Delete 의미 충돌 리스크가 historical record에 남음
- AH-RUNTIME-10 후보가 capability skeleton으로 분리됨

## 9. ACCEPT Decision

ACCEPT

이유:

- Fake mode의 기존 Add/Edit/Delete 흐름 유지 원칙이 확정됨
- InMemoryRuntime mode는 command dispatcher 전까지 read-only dashboard로 정의됨
- Runtime mode에서 mutation command를 성급히 허용하지 않기로 결정함
- DashboardViewModel이 DashboardRuntimeMode를 직접 알지 않는 경계가 유지됨
- composition-level capability 후보가 정리됨
- actual App wiring과 UI disable 구현이 후속 단계로 분리됨
- Runtime command dispatcher 전까지 Add/Edit/Delete runtime command 전환을 보류하기로 정리됨

## 10. Risks / Follow-up Candidates

AH-RUNTIME-10 후보:

- DashboardRuntimeCapabilities skeleton 추가
- DashboardRuntimeComposition에 capability 포함
- DashboardRuntimeCompositionFactory에서 mode별 capability 결정
- Fake mode editing capability true
- InMemoryRuntime editing capability false
- DashboardViewModel은 아직 변경하지 않음
- 실제 button disable UI 구현은 후속으로 분리

추가 후속 후보:

- hidden/testing InMemoryRuntime app wiring 재검토
- capability 전달 경로 검토
- ViewModel command enable/disable 연결
- Runtime command dispatcher 설계
- Add/Edit/Delete runtime command 전환 검토

## 11. Next Step

다음 단계는 AH-RUNTIME-10: DashboardRuntimeCapabilities Skeleton입니다.

단, AH-RUNTIME-10에서도 실제 UI button disable이나 ViewModel command wiring은 하지 않고, composition/factory 수준에서 capability를 표현하는 skeleton과 테스트까지만 진행하는 것이 안전합니다.
