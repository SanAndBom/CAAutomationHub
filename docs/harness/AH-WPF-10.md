# AH-WPF-10 Closeout

## 1. Status

```text
ACCEPT
```

## 2. Scenario Goal

AH-WPF-10의 목표는 PLC Card 수정/삭제 Interaction Prototype을 구현하는 것이다.

목표 범위:

```text
PLC Card 수정/삭제 Interaction Prototype 구현
Fake Dashboard 구성을 조작해 화면 장악력 강화
카드와 Trend series 대응 관계를 더 쉽게 확인할 수 있는 기반 마련
```

## 3. Implemented Scope

완료된 범위:

```text
1. IPlcDashboardConfigurationService 추가
2. PlcDashboardConfiguration 모델 추가
3. FakeDashboardRuntimeAdapter 내부 fake configuration list 기준 Snapshot/Trend/Summary 생성
4. Detail Pane 수정/삭제 버튼 추가
5. PlcEditorDialog Edit 모드 확장
6. Edit 시 기존 PLC 값 주입
7. Edit 결과로 PlcId 유지한 configuration 반환
8. Edit 후 카드/Detail/Selected Trend 갱신
9. Delete 시 MessageBox 확인
10. Delete 후 카드 제거
11. 선택 PLC 삭제 시 SelectedPlc = null
12. 선택 PLC 삭제 시 Detail Pane 닫힘
13. 선택 PLC 삭제 시 Overview Trend 복귀
14. Delete 후 Summary Count 갱신
15. Delete 후 Overview Trend series에서 해당 PlcId 제거
16. ApplySnapshot missing card 제거 보완
17. Fake Live Update 이후 Edit/Delete 유지
18. 카드 제목 폰트 보정
19. 카드 제목 TextTrimming 적용
20. 정상 blue trend 가시성 보정
```

구성 관리 경계:

```text
IRuntimeDashboardAdapter는 Snapshot 읽기 전용 역할을 유지한다.
PLC 구성 수정/삭제는 IPlcDashboardConfigurationService가 담당한다.
DashboardViewModel은 FakeDashboardRuntimeAdapter 구체 타입에 의존하지 않는다.
현재 수정/삭제 결과는 Fake Dashboard configuration에만 반영된다.
```

## 4. Changed Files

변경 파일:

```text
src/CAAutomationHub.Wpf/Models/Dashboard/PlcDashboardConfiguration.cs
src/CAAutomationHub.Wpf/Services/IPlcDashboardConfigurationService.cs
src/CAAutomationHub.Wpf/Adapters/FakeDashboardRuntimeAdapter.cs
src/CAAutomationHub.Wpf/ViewModels/DashboardViewModel.cs
src/CAAutomationHub.Wpf/ViewModels/PlcEditorDialogViewModel.cs
src/CAAutomationHub.Wpf/Dialogs/PlcEditorDialogWindow.xaml
src/CAAutomationHub.Wpf/Dialogs/PlcEditorDialogWindow.xaml.cs
src/CAAutomationHub.Wpf/Controls/PlcDetailPane.xaml
src/CAAutomationHub.Wpf/Controls/PlcDetailPane.xaml.cs
src/CAAutomationHub.Wpf/Controls/PlcStatusCard.xaml
src/CAAutomationHub.Wpf/Controls/TrendRenderControl.cs
src/CAAutomationHub.Wpf/Views/DashboardView.xaml
src/CAAutomationHub.Wpf/Views/DashboardView.xaml.cs
tests/CAAutomationHub.Wpf.Tests/ViewModels/DashboardViewModelConfigurationTests.cs
tests/CAAutomationHub.Wpf.Tests/ViewModels/PlcEditorDialogViewModelTests.cs
tests/CAAutomationHub.Wpf.Tests/Controls/TrendRenderControlPolicyTests.cs
docs/harness/AH-WPF-10.md
```

각 파일의 역할:

```text
PlcDashboardConfiguration.cs
- Fake Dashboard PLC 구성 모델
- Snapshot에 없는 Description / Timeout / Reconnect / Retry / AutoReconnect / ConnectOnStartup / IsEnabled 보존

IPlcDashboardConfigurationService.cs
- PLC 구성 조회/수정/삭제 추상 인터페이스
- ViewModel이 Fake adapter 구체 타입을 알지 않도록 경계 제공

FakeDashboardRuntimeAdapter.cs
- 내부 fake configuration list 보유
- configuration 기준 PlcCardSnapshot 생성
- configuration 기준 CommunicationTrend 생성
- configuration 기준 Summary Count 생성
- Edit/Delete 이후 Fake Live Update에서도 변경 유지

DashboardViewModel.cs
- IPlcDashboardConfigurationService 주입
- EditSelectedPlcCommand / DeleteSelectedPlcCommand 추가
- selected PLC configuration 조회 경로 추가
- ApplySnapshot missing card 제거
- 삭제된 선택 PLC의 선택/Detail/Trend 정합성 처리

PlcEditorDialogViewModel.cs
- Add/Edit 공통 Dialog ViewModel
- Edit 모드 초기값 주입
- DialogTitle / HeaderTitle / HeaderSubtitle 지원
- Save 결과 PlcDashboardConfiguration 반환

PlcEditorDialogWindow.xaml
- Dialog title/header 바인딩
- 초기 Height 보정
- Add/Edit 공통 Window 유지

PlcEditorDialogWindow.xaml.cs
- Add/Edit 생성자 분리
- Save 시 ResultConfiguration 반환
- Dialog가 service/adapter를 직접 수정하지 않도록 유지

PlcDetailPane.xaml
- 수정/삭제 버튼 추가

PlcDetailPane.xaml.cs
- EditRequested / DeleteRequested routed event 추가
- Detail Pane에서 직접 ViewModel/service를 수정하지 않고 요청 이벤트만 발생

PlcStatusCard.xaml
- 카드 제목 폰트 크기 보정
- 카드 제목 TextTrimming 적용

TrendRenderControl.cs
- 정상 blue trend line 가시성 보정
- GetSegmentRenderStyle로 line style 정책 테스트 가능화

DashboardView.xaml
- Detail Pane edit/delete event 연결

DashboardView.xaml.cs
- Edit Dialog 호출
- Delete MessageBox 확인
- Dialog 결과를 DashboardViewModel command로 전달

DashboardViewModelConfigurationTests.cs
- Edit/Delete/Fake Live Update/ApplySnapshot missing card 정책 테스트

PlcEditorDialogViewModelTests.cs
- Dialog Edit 모드 초기값 주입 및 PlcId 보존 결과 테스트

TrendRenderControlPolicyTests.cs
- 기존 Trend threshold/priority 정책 유지
- Healthy line visibility policy 회귀 방지
```

## 5. Final Behavior

### 5.1 Edit

최종 Edit 동작:

```text
1. Detail Pane 수정 버튼 클릭
2. 선택 PLC 값이 Dialog에 주입됨
3. Save 시 같은 PlcId 기준으로 configuration 업데이트
4. 카드 표시 갱신
5. Detail Pane 표시 갱신
6. Selected Trend 대상명 갱신
7. Fake Live Update 후에도 수정 결과 유지
```

세부 정책:

```text
Edit 시 PlcId는 유지한다.
Dialog는 service/adapter를 직접 수정하지 않는다.
Dialog는 수정된 PlcDashboardConfiguration만 반환한다.
DashboardViewModel이 IPlcDashboardConfigurationService.UpdatePlc를 통해 수정한다.
수정 후 Snapshot reload로 Card / Detail / Trend를 동기화한다.
```

### 5.2 Delete

최종 Delete 동작:

```text
1. Detail Pane 삭제 버튼 클릭
2. 확인 MessageBox 표시
3. 취소 시 변화 없음
4. 확인 시 configuration에서 PlcId 삭제
5. 카드 목록에서 제거
6. 선택 중인 PLC 삭제 시 Detail Pane 닫힘
7. Overview Trend 복귀
8. Summary Count 갱신
9. Overview Trend series에서 삭제된 PlcId 제거
10. Fake Live Update 후에도 삭제 결과 유지
```

세부 정책:

```text
Delete는 PlcId 기준으로 수행한다.
선택 중인 PLC가 삭제되면 SelectedPlc = null로 변경한다.
선택 중인 PLC가 삭제되면 IsDetailPaneOpen = false로 변경한다.
삭제 후 모든 카드 선택 상태를 다시 동기화한다.
삭제된 PlcId는 Overview series와 PLC별 Trend 목록에서 사라진다.
```

### 5.3 ApplySnapshot

최종 ApplySnapshot 정책:

```text
Snapshot에 없는 PlcId는 ObservableCollection에서 제거
삭제된 PlcId가 선택 중이면 선택 해제 처리
삭제된 PlcId가 선택 중이면 Detail Pane 닫힘
선택/Trend/Detail 상태 정합성 유지
Snapshot에 남아 있는 같은 PlcId는 기존 PlcStatusCardViewModel.UpdateSnapshot으로 갱신
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
72 passed
0 failed
```

수동 확인:

```text
Detail Pane 수정/삭제 버튼 확인
수정 Dialog 초기값 주입 확인
수정 후 카드/Detail/Trend 갱신 확인
삭제 확인 Dialog 확인
삭제 후 카드 제거 확인
선택 PLC 삭제 시 Detail Pane 닫힘 및 Overview 복귀 확인
카드 제목 폰트 보정 확인
정상 blue trend 가시성 개선 확인
기존 Trend 정책 유지 확인
```

## 7. Tests Added / Updated

추가/갱신된 테스트 정책:

```text
Edit 후 카드/Detail 표시값 갱신
Edit 후 selected Trend 이름 갱신
Fake Live Update 후 Edit 결과 유지
Delete selected PLC 후 선택 해제
Delete selected PLC 후 Detail Pane 닫힘
Delete selected PLC 후 Overview 복귀
Delete unselected PLC 후 현재 선택 유지
Delete 후 Summary Count 갱신
Delete 후 Overview series에서 해당 PlcId 제거
ApplySnapshot이 Snapshot에 없는 PlcId 제거
Dialog Edit 모드 초기값 주입
Dialog 결과에서 PlcId 보존
Healthy line visibility policy 회귀 방지
```

테스트 파일:

```text
tests/CAAutomationHub.Wpf.Tests/ViewModels/DashboardViewModelConfigurationTests.cs
tests/CAAutomationHub.Wpf.Tests/ViewModels/PlcEditorDialogViewModelTests.cs
tests/CAAutomationHub.Wpf.Tests/Controls/TrendRenderControlPolicyTests.cs
```

## 8. Boundary Rules

유지된 경계 규칙:

```text
Add PLC 실제 반영 없음
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
카드 Context Menu 고도화 없음
Communication Trend 재설계 없음
Mini Trend 재설계 없음
```

구조 경계:

```text
DashboardViewModel은 FakeDashboardRuntimeAdapter 구체 타입을 직접 참조하지 않는다.
DashboardViewModel은 IRuntimeDashboardAdapter와 IPlcDashboardConfigurationService 추상에 의존한다.
PlcEditorDialogWindow는 configuration 결과만 반환하고 adapter/service를 직접 수정하지 않는다.
```

## 9. Known Limitations / Notes

남은 제약 및 메모:

```text
Dialog validation은 아직 Prototype 수준이다.
잘못된 숫자/빈 문자열/중복 IP 검증은 후속 범위가 좋다.
Add PLC 실제 반영은 아직 구현하지 않았다.
현재 수정/삭제는 Fake Dashboard configuration에만 반영된다.
영구 저장은 없다.
카드 제목은 한 줄 유지, 긴 이름은 ellipsis 처리한다.
Mini Trend 역할은 아직 정리되지 않았다.
```

추가 메모:

```text
이번 시나리오는 실제 Runtime/PLC 설정 관리가 아니다.
Fake Dashboard 구성을 조작해 화면 장악력과 Trend 대응 확인성을 높이는 Prototype이다.
Real Runtime 연동 시에는 같은 추상 인터페이스를 실제 구성 저장/관리 구현으로 교체하는 방향이 자연스럽다.
```

## 10. Next Scenario Candidates

추천 순서:

```text
1. AH-WPF-11: Add PLC Actual Apply Prototype
   - + PLC 추가가 실제 Fake configuration에 반영
   - 새 카드 추가
   - Overview Trend series 추가
   - Summary Count 갱신

2. AH-WPF-12: PlcEditorDialog Validation Polish
   - IP/Port/Polling/Timeout 숫자 검증
   - 빈 이름 검증
   - 중복 PLC 이름/IP 검토

3. AH-WPF-13: Mini Trend Role Review
   - Card Mini Trend 유지/축소/제거 판단
   - 최근 3~5분 Sparkline으로 의미 축소 가능성 검토

4. AH-WPF-14: Trend Refactor Review
   - AH-WPF-09에서 누적된 Trend 코드 구조 점검
   - TrendRenderControl / Fake Trend 생성 / 모델 계약 정리
```
