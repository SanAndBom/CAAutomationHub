# AH-WPF-03 PlcEditorDialog Static Prototype

## 1. Scenario Overview

```text
Scenario ID: AH-WPF-03
Name: PlcEditorDialog Static Prototype
Type: WPF UI / Dialog Static Prototype
Final Status: ACCEPT_WITH_MINOR_RISK
```

AH-WPF-03의 목표는 실제 PLC 추가 기능 구현이 아니라, `+ PLC 추가` 버튼에서 `PlcEditorDialogWindow`를 열고, PLC 추가 Dialog의 정적 UI 구조와 테스트 전 / 연결 성공 / 연결 실패 상태 표현을 검증하는 것이다.

이 시나리오는 Dashboard에서 PLC 추가 진입점을 확인하고, 향후 Add/Edit 공용 Dialog로 확장할 수 있는 화면 구조를 먼저 고정한다. 실제 PLC 연결, 설정 저장, Dashboard Card 추가, DB/Runtime/Flow 연동은 이번 범위에 포함하지 않는다.

## 2. Final Status

최종 판정은 `ACCEPT_WITH_MINOR_RISK`이다.

검증 결과:

```text
Build 성공
Run 성공
+ PLC 추가 버튼 클릭 시 Dialog 표시 확인
테스트 전 상태 표시 확인
연결 성공 상태 표시 확인
연결 실패 상태 표시 확인
테스트 연결 버튼 클릭 시 상태 순환 확인
Protocol / 제조사 선택 필드 없음 확인
실제 PLC 연결 없이 UI 상태만 변경 확인
저장 / 취소 버튼 표시 확인
실제 PLC 설정 저장 없음 확인
Dashboard Card 실제 추가 없음 확인
```

판정 사유:

```text
기능 및 범위는 충족했다.
다만 디자인은 아직 정적 Prototype 수준이며, 향후 시각적 정리 여지가 있다.
```

## 3. Completed Scope

완료된 범위:

```text
1. PlcEditorDialogWindow.xaml 추가
2. PlcEditorDialogWindow.xaml.cs 추가
3. PlcEditorDialogViewModel.cs 추가
4. DashboardView의 + PLC 추가 버튼 연결
5. DashboardView.xaml.cs에서 Dialog 오픈 코드 추가
6. 추후 IDialogService 또는 ViewModel Command로 이동 예정이라는 주석 추가
7. 테스트 전 / 연결 성공 / 연결 실패 상태 표현
8. 테스트 연결 버튼의 UI 상태 순환
9. 저장 / 취소 버튼의 Dialog 닫기 동작
10. 실제 PLC 설정 저장 없음
11. Dashboard PlcCards 실제 추가 없음
12. 제조사 / Protocol 선택 UI 미제공
13. Build 성공
14. 로컬 실행 확인
```

이번 작업에서 고정된 구조:

```text
src/CAAutomationHub.Wpf/Dialogs/PlcEditorDialogWindow.xaml
src/CAAutomationHub.Wpf/Dialogs/PlcEditorDialogWindow.xaml.cs
src/CAAutomationHub.Wpf/ViewModels/PlcEditorDialogViewModel.cs
src/CAAutomationHub.Wpf/Views/DashboardView.xaml
src/CAAutomationHub.Wpf/Views/DashboardView.xaml.cs
```

## 4. Dialog Field Structure

Dialog 필드 구성:

```text
[기본 정보]
PLC 이름: 롤러홀개공기
라인 이름: SF절단라인
설명: 절단기 보조 PLC

[네트워크 / 연결 설정]
IP 주소: 192.168.0.133
Port: 2004
사용 여부: ON / 사용

[폴링 / 재연결 설정]
Polling Interval: 200 ms
Timeout: 800 ms
재연결 간격: 5 sec
최대 재시도: 5 회
Auto Reconnect: ON

[동작 옵션]
시작 시 자동 연결: checked

[연결 테스트 결과]
테스트 전
연결 성공
연결 실패

[설정 요약]
PLC 이름
IP 주소
Port
Polling Interval
Auto Reconnect
테스트 상태
```

버튼 구성:

```text
취소
테스트 연결
저장
```

## 5. Implementation Summary

현재 구현 요약:

```text
DashboardView
└── + PLC 추가 Button
    └── DashboardView.xaml.cs OnAddPlcClick()
        └── PlcEditorDialogWindow.ShowDialog()

PlcEditorDialogWindow
└── PlcEditorDialogViewModel
    ├── 기본 정보 표시 값
    ├── 네트워크 / 연결 설정 표시 값
    ├── 폴링 / 재연결 설정 표시 값
    ├── 동작 옵션 표시 값
    ├── TestConnectionCommand
    └── 테스트 전 / 연결 성공 / 연결 실패 상태 표시
```

핵심 동작:

```text
1. Dashboard의 + PLC 추가 버튼을 클릭하면 PlcEditorDialogWindow가 열린다.
2. Dialog 제목은 PLC 추가로 표시된다.
3. Dialog는 제조사 / Protocol 선택 필드를 제공하지 않는다.
4. Dialog는 요청된 기본값을 표시한다.
5. 테스트 연결 버튼은 실제 통신 없이 ViewModel 내부 표시 상태만 순환한다.
6. 상태 순환은 테스트 전 -> 연결 성공 -> 연결 실패 -> 테스트 전 순서로 동작한다.
7. 저장 / 취소 버튼은 Dialog 닫기 동작만 수행한다.
8. Dashboard PlcCards 컬렉션에는 실제 Card를 추가하지 않는다.
9. 실제 PLC 설정 저장은 수행하지 않는다.
```

`DashboardView.xaml.cs`의 Dialog 오픈 코드는 AH-WPF-03 정적 Prototype 범위에서 허용된 임시 연결이다. 향후 실제 Add/Edit 흐름으로 확장할 때 `IDialogService` 또는 ViewModel Command 기반 구조로 이동할 예정이다.

## 6. Verification Evidence

Build 확인:

```powershell
dotnet build CAAutomationHub.sln
```

결과:

```text
Build 성공
경고 0개
오류 0개
```

로컬 실행 검증 결과:

```text
1. + PLC 추가 버튼 클릭 시 PLC 추가 Dialog 표시 확인
2. 테스트 전 상태 표시 확인
3. 연결 성공 상태 표시 확인
4. 연결 실패 상태 표시 확인
5. 테스트 연결 버튼 클릭 시 상태 순환 확인
6. Protocol / 제조사 선택 필드 없음 확인
7. 실제 PLC 연결 없이 UI 상태만 변경 확인
8. 저장 / 취소 버튼 표시 확인
```

범위 확인:

```text
실제 PLC 설정 저장 없음
Dashboard Card 실제 추가 없음
실제 네트워크 호출 없음
```

## 7. Boundary Rules Maintained

현재 유지 중인 경계 규칙:

```text
WPF UI는 FakePlc를 직접 참조하지 않는다.
WPF UI는 XgtDriverCore를 직접 참조하지 않는다.
WPF UI는 Runtime 내부 구현체를 직접 참조하지 않는다.
실제 PLC 연결 테스트는 아직 구현하지 않는다.
테스트 연결 버튼은 실제 네트워크 호출을 하지 않는다.
실제 PLC 설정 저장은 아직 구현하지 않는다.
Dashboard PlcCards에는 실제 추가하지 않는다.
Protocol / 제조사 선택 UI는 제공하지 않는다.
사용자 관리 기능은 구현하지 않는다.
```

명시적 제외 범위:

```text
실제 PLC 연결
FakePlc 직접 연결
XgtDriverCore 직접 호출
Runtime 연결
실제 연결 테스트
PLC 설정 저장
Dashboard PlcCards 실제 추가
DB 연동
Flow Runtime 연동
사용자 관리 기능
이벤트 로그 Popup 구현
AH-WPF-04 진행
```

## 8. Remaining Risks / Follow-up Items

AH-WPF-03 완료를 막는 요소는 아니지만 후속 작업 후보로 남긴다.

```text
Dialog 디자인을 최종 시안 수준으로 시각적 정리
오른쪽 연결 테스트 결과 영역에 아이콘 / 라인 그래픽 추가
성공 / 실패 / 테스트 전 상태 카드 위계 개선
하단 버튼 스타일 고도화
DialogService 또는 ViewModel Command 기반 Dialog 오픈 구조로 전환
Add/Edit 공용 모드 분리
실제 PLC 설정 저장 모델 설계
Dashboard Card 실제 추가 흐름 설계
실제 연결 테스트는 Runtime Adapter 경계 이후 별도 시나리오로 분리
```

## 9. Next Scenario Candidates

권장 다음 순서:

```text
1. AH-WPF-04: RealtimeEventLogPopup Static Prototype
2. AH-WPF-05: RuntimeDashboardAdapter Skeleton
3. AH-WPF-06: Fake Trend Chart Binding Prototype
4. AH-WPF-07: PlcEditorDialog Add/Edit Model Preparation
5. AH-WPF-08: Plc Configuration Save Prototype
```
