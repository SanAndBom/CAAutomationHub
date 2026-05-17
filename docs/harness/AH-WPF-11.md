# AH-WPF-11 Closeout

## 1. Status

```text
ACCEPT
```

## 2. Scenario Goal

AH-WPF-11의 목표는 `+ PLC 추가` 기능을 실제 Fake Dashboard configuration에 반영하는 것이다.

목표 범위:

```text
+ PLC 추가 기능을 실제 Fake Dashboard configuration에 반영
Add/Edit/Delete 조작 3축 완성
카드 구성 조작을 통해 화면 장악력 강화
카드와 Trend series 대응 관계 확인성 개선
```

## 3. Implemented Scope

완료된 범위:

```text
1. IPlcDashboardConfigurationService Add/default 생성 계약 추가
2. CreateDefaultPlcConfiguration 추가
3. AddPlc 추가
4. FakeDashboardRuntimeAdapter에서 초기 Fake PLC 수 5개로 축소
5. 기본 PLC: PLC-01 ~ PLC-05
6. 기본 IP: 192.168.0.21 ~ 192.168.0.25
7. 최초 Add: PLC-06 / 192.168.0.26
8. Add 반복 시 최대 번호 + 1 정책
9. 삭제된 번호 재사용하지 않음
10. + PLC 추가 버튼을 실제 Add flow에 연결
11. Add Dialog에 service 생성 기본값 주입
12. Save 시 새 configuration 추가
13. Cancel 시 변화 없음
14. Add 후 Snapshot reload
15. Add 후 새 PLC 자동 선택
16. Add 후 Detail Pane 열림
17. Add 후 Selected Trend 전환
18. Add 후 Summary Count 갱신
19. Add 후 Overview Trend series 갱신
20. Fake Live Update 이후 Add 결과 유지
21. Summary Header에 InactiveCount 추가
22. RuntimeHealthSnapshot에 InactiveCount 추가
23. TotalCount와 상태별 Count 합계 일치
```

구성 관리 경계:

```text
Dialog는 service/adapter를 직접 수정하지 않는다.
View는 Dialog 표시와 결과 전달을 담당한다.
DashboardViewModel은 IPlcDashboardConfigurationService 추상으로 Add/default 생성/refresh/selection을 처리한다.
FakeDashboardRuntimeAdapter는 내부 fake configuration list를 기준으로 Snapshot/Trend/Summary를 생성한다.
```

## 4. Changed Files

변경 파일:

```text
src/CAAutomationHub.Wpf/Services/IPlcDashboardConfigurationService.cs
src/CAAutomationHub.Wpf/Adapters/FakeDashboardRuntimeAdapter.cs
src/CAAutomationHub.Wpf/ViewModels/DashboardViewModel.cs
src/CAAutomationHub.Wpf/Views/DashboardView.xaml
src/CAAutomationHub.Wpf/Views/DashboardView.xaml.cs
src/CAAutomationHub.Wpf/Models/Dashboard/RuntimeHealthSnapshot.cs
tests/CAAutomationHub.Wpf.Tests/ViewModels/DashboardViewModelConfigurationTests.cs
tests/CAAutomationHub.Wpf.Tests/ViewModels/PlcEditorDialogViewModelTests.cs
docs/harness/AH-WPF-11.md
```

각 파일의 역할:

```text
IPlcDashboardConfigurationService.cs
- Add용 default configuration 생성 계약 추가
- 새 PLC 추가 계약 추가

FakeDashboardRuntimeAdapter.cs
- 초기 Fake PLC 구성을 5개로 축소
- PLC-## 최대 번호 + 1 방식의 Add 기본값 생성
- AddPlc 구현
- 중복 PlcId 요청 시 service가 새 unique PlcId로 정규화
- InactiveCount 계산 포함

DashboardViewModel.cs
- AddPlcCommand 추가
- CreateDefaultPlcConfiguration 경로 추가
- Add 후 Snapshot reload
- Add 후 새 PLC 자동 선택
- Add 후 Detail Pane 열림
- InactiveCount 표시 속성 추가

DashboardView.xaml
- Summary Header에 비활성 Count 표시 추가

DashboardView.xaml.cs
- + PLC 추가 버튼을 실제 Add flow에 연결
- service 생성 기본값으로 Add Dialog 열기
- Dialog Save 결과를 AddPlcCommand로 전달

RuntimeHealthSnapshot.cs
- InactiveCount 계약 추가
- 기존 생성자 호출 호환을 위해 기본값 0 유지

DashboardViewModelConfigurationTests.cs
- 초기 5개 PLC 정책 테스트
- Add 기본값 / Add 후 선택 / Summary / Trend / Fake Live Update 정책 테스트
- InactiveCount 및 상태별 Count 합계 정책 테스트

PlcEditorDialogViewModelTests.cs
- Add 모드 제목/헤더 및 기본값 주입 테스트
```

## 5. Final Behavior

### 5.1 Initial Fake Dashboard

최종 초기 Dashboard 동작:

```text
최초 실행 시 PLC Card 5개 표시
기본 PlcId는 PLC-01 ~ PLC-05
기본 IP는 192.168.0.21 ~ 192.168.0.25
Summary Total Count는 5 기준
Overview Trend series는 5개 기준
```

### 5.2 Add PLC

최종 Add 동작:

```text
1. + PLC 추가 클릭
2. Add Dialog 열림
3. service가 생성한 기본값 주입
4. Save 시 새 PLC 추가
5. Cancel 시 변화 없음
6. Add 후 새 PLC 자동 선택
7. Add 후 Detail Pane 열림
8. Add 후 Selected Trend 전환
9. Add 후 Summary Count +1
10. Add 후 Overview Trend series +1
11. Fake Live Update 후에도 유지
```

ID/IP 정책:

```text
최초 Add 기본값: PLC-06 / 192.168.0.26
Add 반복 시 PLC-07, PLC-08 순으로 증가
삭제된 번호는 재사용하지 않음
PlcId는 표시 이름과 분리됨
View/Dialog/ViewModel은 ID 생성 규칙을 알지 않음
```

### 5.3 Summary Count

최종 Summary Count 동작:

```text
Header에 비활성 Count 표시
표시 순서: 전체 / 정상 / 주의 / 정체 / 오류 / 비활성
TotalCount = Healthy + Warning + Congested + Error + Inactive
비활성 Count는 0이어도 표시
```

## 6. Validation

Build 확인:

```powershell
dotnet build CAAutomationHub.sln
```

결과:

```text
성공
warning 0
error 0
```

Test 확인:

```powershell
dotnet test CAAutomationHub.sln
```

결과:

```text
성공
79 passed
0 failed
```

수동 확인:

```text
최초 PLC Card 5개 확인
Add Dialog Add 모드 확인
PLC-06 / 192.168.0.26 기본값 확인
Save 시 Card 추가 확인
Cancel 시 변화 없음 확인
Add 후 자동 선택 확인
Add 후 Detail Pane 열림 확인
Add 후 Selected Trend 전환 확인
Add 후 Summary Count 증가 확인
비활성 Count 표시 확인
전체 Count와 상태별 Count 합계 일치 확인
Add 후 Overview series 증가 확인
Fake Live Update 후 유지 확인
기존 Edit/Delete 기능 유지 확인
기존 Trend 표현 유지 확인
```

## 7. Tests Added / Updated

추가/갱신된 테스트 정책:

```text
초기 Fake Snapshot PlcCards.Count == 5
초기 Overview Trend Series.Count == 5
최초 default Add config = PLC-06 / 192.168.0.26
Add 후 configuration count 증가
Add 반복 시 PlcId 중복 없음
Add 후 카드 추가
Add 후 Summary Total 증가
Add 후 새 PLC 자동 선택
Add 후 Detail Pane open
Add 후 selected trend 전환
Fake Live Update 후 Add 결과 유지
Dialog Add 모드 제목/헤더 및 기본값 주입
InactiveCount 표시/계산
TotalCount == Healthy + Warning + Congested + Error + Inactive
```

테스트 파일:

```text
tests/CAAutomationHub.Wpf.Tests/ViewModels/DashboardViewModelConfigurationTests.cs
tests/CAAutomationHub.Wpf.Tests/ViewModels/PlcEditorDialogViewModelTests.cs
```

## 8. Boundary Rules

유지된 경계 규칙:

```text
실제 Runtime 연결 없음
실제 PLC 연결 없음
FakePlc 연결 없음
XgtDriverCore 참조 없음
XgtChannelRunner 참조 없음
DB 저장 없음
JSON 저장 없음
사용자 권한/로그인 없음
영구 저장 없음
실제 통신 재연결 처리 없음
Runtime Channel 생성/삭제 없음
고급 Context Menu UX 없음
Dashboard 대규모 레이아웃 변경 없음
Communication Trend 재설계 없음
Mini Trend 재설계 없음
복잡한 validation 없음
중복 IP/중복 PLC 이름 검증 고도화 없음
```

구조 경계:

```text
DashboardViewModel은 FakeDashboardRuntimeAdapter 구체 타입에 의존하지 않는다.
DashboardViewModel은 IRuntimeDashboardAdapter와 IPlcDashboardConfigurationService 추상에 의존한다.
Dialog는 configuration 결과만 반환하고 adapter/service를 직접 수정하지 않는다.
Add/Edit/Delete는 Fake Dashboard configuration에만 반영된다.
```

## 9. Known Limitations / Notes

남은 제약 및 메모:

```text
Add 입력 validation은 아직 Prototype 수준이다.
빈 이름, 숫자 범위, 중복 IP/이름 검증은 AH-WPF-12에서 다루는 편이 좋다.
Add Dialog 기본 생성자에는 Prototype default가 남아 있지만, 실제 Add flow에서는 service 생성 configuration을 주입한다.
현재 Add/Edit/Delete는 Fake Dashboard configuration에만 반영된다.
영구 저장은 없다.
초기 5개 구성은 테스트 편의와 화면 장악력을 위한 Pilot 설정이다.
```

추가 메모:

```text
InactiveCount는 Header 표시와 Count 합계 정합성을 위해 RuntimeHealthSnapshot 계약에 추가되었다.
기존 RuntimeDashboardAdapter Skeleton은 InactiveCount 기본값 0을 사용한다.
Real Runtime 연동 시 Health Snapshot 산식에서 비활성 상태 정의를 다시 확정해야 한다.
```

## 10. Next Scenario Candidates

추천 순서:

```text
1. AH-WPF-12: PlcEditorDialog Validation Polish
   - 빈 이름 검증
   - IP/Port/Polling/Timeout 숫자 검증
   - 범위 검증
   - 중복 이름/IP 검토

2. AH-WPF-13: Mini Trend Role Review
   - Card Mini Trend 유지/축소/제거 판단
   - 최근 3~5분 Sparkline으로 의미 축소 가능성 검토
   - 하단 Trend와 역할 분리

3. AH-WPF-14: Trend Consistency Review
   - 카드 구성과 Trend series 일치성 최종 검토
   - Overview/Selected Trend 표현 재검토

4. AH-WPF-15: Trend Refactor Review
   - AH-WPF-09에서 누적된 Trend 코드 구조 점검
   - TrendRenderControl / Fake Trend 생성 / 모델 계약 정리
```
