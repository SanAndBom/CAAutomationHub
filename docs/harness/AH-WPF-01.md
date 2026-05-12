# AH-WPF-01.md

# AH-WPF-01 Dashboard Shell + PLC Card Static Prototype

## 1. 작업 식별자

```text
Scenario ID: AH-WPF-01
Name: Dashboard Shell + PLC Card Static Prototype
Type: WPF UI / Runtime Boundary Harness
```

---

## 2. 작업 목적

AutomationHub WPF Dashboard의 첫 화면 구조를 만든다.

이번 작업은 최종 제품 UI 완성이 아니다.

핵심 목적은 다음이다.

```text
Runtime/UI 연결 경계 검증
```

즉, 실제 PLC 통신이나 Runtime 연결 없이, `FakeDashboardRuntimeAdapter`가 제공하는 정적 `DashboardSnapshot`을 사용해 PLC Card 목록을 표시한다.

---

## 3. 현재까지 고정된 설계 기준

```text
Dashboard는 PLC 통신 상태 관제 중심 화면이다.
Popup은 설정/추가/테스트 같은 작업성 행위를 담당한다.
PLC Card는 반드시 UserControl로 분리한다.
DashboardView는 ItemsControl + ObservableCollection 기반으로 PlcStatusCard를 반복 표시한다.
개발자가 여러 개의 PLC Card XAML을 직접 반복 작성하면 안 된다.
우측 Detail Pane은 선택된 PLC의 상세 정보 표시용이다.
Detail Pane이 닫히면 PLC Card 영역이 확장된다.
PLC Card 영역에는 횡 스크롤이 제공되어야 한다.
사용자 관리 기능은 1차 범위에서 제외한다.
Flow Designer는 1차 범위에서 제외한다.
```

---

## 4. 포함 범위

다음 항목만 구현한다.

```text
1. WPF 프로젝트 생성 또는 정비
2. MainWindow 기본 Shell 구성
3. Theme / ResourceDictionary 최소 구조 생성
4. DashboardView 생성
5. PlcStatusCard UserControl 생성
6. PlcDetailPane UserControl 자리 생성
7. CommunicationTrendChart Placeholder 생성
8. DashboardSnapshot / PlcCardSnapshot / RuntimeHealthSnapshot / PlcConnectionState DTO 생성
9. IRuntimeDashboardAdapter 인터페이스 생성
10. FakeDashboardRuntimeAdapter 생성
11. DashboardViewModel에서 Fake Adapter를 통해 Snapshot 로드
12. PLC Card 8~12개 정적 표시
13. 우측 Detail Pane 열기/닫기
14. Detail Pane 닫힘 시 카드 영역 확장
15. PLC Card 영역 횡 스크롤 표시
16. Build 가능 상태 확보
```

---

## 5. 제외 범위

다음 항목은 절대 구현하지 않는다.

```text
실제 PLC 연결
FakePlc 직접 연결
XgtDriverCore 직접 호출
실제 Runtime 연결
Polling Loop 구현
PLC Read / Write 명령 구현
Reconnect 실제 구현
Timeout 실제 처리
DB 연동
Flow Runtime 연동
Flow Designer 구현
PLC 추가 저장 로직 완성
연결 테스트 실제 통신
실시간 Event Stream 실제 구현
사용자 로그인 / 사용자 관리 / 권한 관리
과도한 MVVM 프레임워크 도입
과도한 애니메이션 / 디자인 과설계
XgtDriverCore public API 변경
FakePlc 프로토콜 변경
Runtime 내부 구조 변경
```

---

## 6. 계층 경계 규칙

```text
FakePlc
↕
XgtDriverCore
↕
Runtime / ChannelRunner
↕
RuntimeDashboardAdapter
↕
WPF UI
```

반드시 지켜야 할 규칙:

```text
WPF UI는 FakePlc를 직접 참조하지 않는다.
WPF UI는 XgtDriverCore를 직접 참조하지 않는다.
WPF UI는 Runtime 내부 구현체를 직접 참조하지 않는다.
WPF UI는 IRuntimeDashboardAdapter를 통해 DashboardSnapshot을 받는다.
FakeDashboardRuntimeAdapter는 순수 정적 데이터를 반환한다.
DashboardSnapshot / PlcCardSnapshot은 UI 표시용 DTO이며 Runtime 내부 객체가 아니다.
Runtime 내부 타입, XgtDriverCore 타입, FakePlc 타입을 DTO에 포함하지 않는다.
```

---

## 7. 예상 파일 구조

실제 레포 구조에 맞게 조정하되, 역할 분리는 유지한다.

```text
src/AutomationHub.Wpf 또는 samples/AutomationHub.WpfShell
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── Themes
│   ├── Theme.Dark.xaml
│   ├── Colors.xaml
│   ├── Brushes.xaml
│   ├── ButtonStyles.xaml
│   ├── CardStyles.xaml
│   ├── StatusStyles.xaml
│   └── ScrollBarStyles.xaml
├── Views
│   ├── DashboardView.xaml
│   └── DashboardView.xaml.cs
├── Controls
│   ├── PlcStatusCard.xaml
│   ├── PlcStatusCard.xaml.cs
│   ├── PlcDetailPane.xaml
│   ├── PlcDetailPane.xaml.cs
│   ├── CommunicationTrendChart.xaml
│   └── CommunicationTrendChart.xaml.cs
├── ViewModels
│   ├── ViewModelBase.cs
│   ├── MainWindowViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── PlcStatusCardViewModel.cs
│   └── PlcDetailPaneViewModel.cs
├── Models
│   └── Dashboard
│       ├── DashboardSnapshot.cs
│       ├── RuntimeHealthSnapshot.cs
│       ├── PlcCardSnapshot.cs
│       ├── PlcConnectionState.cs
│       └── RuntimeDashboardEvent.cs
└── Adapters
    ├── IRuntimeDashboardAdapter.cs
    └── FakeDashboardRuntimeAdapter.cs
```

---

## 8. 구현 순서

```text
Step 1. 현재 솔루션 / 프로젝트 구조 확인
Step 2. Theme / ResourceDictionary 최소 구조 생성
Step 3. Dashboard DTO 생성
Step 4. Adapter 인터페이스 생성
Step 5. FakeDashboardRuntimeAdapter 생성
Step 6. ViewModel 구현
Step 7. PlcStatusCard UserControl 구현
Step 8. DashboardView 구현
Step 9. PlcDetailPane UserControl 자리 구현
Step 10. Detail Pane 열기/닫기 구현
Step 11. Build 실행
```

---

## 9. 화면 / 컨트롤 구현 기준

```text
MainWindow
├── Top Header
└── DashboardView

DashboardView
├── Dashboard Summary Header
│   ├── Title: PLC 통신 상태 개요
│   ├── 전체 개수
│   ├── 정상 개수
│   ├── 주의 개수
│   ├── 정체 개수
│   ├── 오류 개수
│   ├── 새로고침 Button
│   └── + PLC 추가 Button 자리
├── PLC Card Horizontal Strip
│   ├── ItemsControl
│   └── Horizontal Scrollbar
├── Right Detail Pane
└── Communication Trend Chart Placeholder

PlcStatusCard
├── PLC Name
├── Line Name
├── Status Ring 또는 상태 Badge
├── Connection State
├── IP / Port
├── Polling Interval
├── Last Response
├── TX / RX
├── Error Count
└── Mini Trend Placeholder

PlcDetailPane
├── Header: 선택된 PLC 상세 정보
├── Close Button X
├── Selected PLC Identity
├── Network Info
├── Packet / Error Summary
├── Recent Events Placeholder
├── Control Buttons Placeholder
└── Flow Control Note
```

---

## 10. ViewModel / DTO 기준

### DashboardViewModel 최소 구조

```text
ObservableCollection<PlcStatusCardViewModel> PlcCards
PlcStatusCardViewModel? SelectedPlc
bool IsDetailPaneVisible
DashboardSummary 또는 Summary Count 속성
ICommand RefreshCommand
ICommand OpenAddPlcCommand
ICommand CloseDetailPaneCommand
Snapshot 로드 메서드
```

### PlcStatusCardViewModel 최소 구조

```text
PlcId
PlcName
LineName
IpAddress
Port
PollingIntervalMs
ConnectionState
CommunicationStatusText
LastResponseText 또는 LastResponseMs
TxCount
RxCount
ErrorCount
IsSelected
SelectCommand 또는 DashboardViewModel에서 Selection 처리
```

### PlcDetailPaneViewModel 최소 구조

```text
SelectedPlc
CloseCommand
표시용 Network Info
표시용 Packet/Error Summary
```

---

## 11. 검증 기준

```text
1. WPF 프로젝트가 Build 되는가?
2. MainWindow가 실행 가능한 구조인가?
3. DashboardView가 표시되는가?
4. PLC Card가 8~12개 표시되는가?
5. Connected / Warning / Disconnected / Congested 상태가 시각적으로 구분되는가?
6. PLC Card가 UserControl로 분리되어 있는가?
7. DashboardView가 ItemsControl + ObservableCollection 기반으로 카드를 표시하는가?
8. PLC Card를 클릭하면 Detail Pane이 표시되는가?
9. Detail Pane의 X 버튼을 누르면 Detail Pane이 닫히는가?
10. Detail Pane이 닫히면 카드 영역이 확장되는가?
11. 카드 영역에 횡 스크롤이 표시되는가?
12. Trend Chart Placeholder가 표시되는가?
13. UI가 IRuntimeDashboardAdapter를 통해 DashboardSnapshot을 받는가?
14. FakePlc / XgtDriverCore / 실제 Runtime을 직접 참조하지 않는가?
15. 사용자 관리 기능이 추가되지 않았는가?
```

---

## 12. Build / Test 실행 기준

가능하면 다음을 실행한다.

```bash
dotnet build AutomationHub.sln
```

솔루션 구조상 불가능하면 실제 WPF 프로젝트 기준으로 Build한다.

Summary에 반드시 포함할 것:

```text
실행한 명령
Build 성공/실패 여부
실패 시 오류 메시지 요약
테스트 실행 여부
```

---

## 13. 완료 판정 기준

```text
ACCEPT
- 주요 목표 충족
- Build 성공
- 경계 위반 없음
- 다음 단계 진행 가능

ACCEPT_WITH_RISK
- 주요 목표는 충족
- 일부 미완성 또는 리스크 존재
- 보완 항목이 명확함

REPAIR_REQUIRED
- 구조 방향은 맞음
- Build 실패 또는 핵심 일부 누락
- 수리 지시 필요

REJECT_AND_REPLAN
- 목표 이탈
- 경계 위반
- 과도한 구조 변경
- 재계획 필요
```

---

## 14. Codex Summary 형식

작업 완료 후 반드시 아래 형식으로 Summary를 제출한다.

```text
1. Summary
2. 변경 파일 목록
3. 각 파일 변경 이유
4. 실행한 명령
5. Build/Test 결과
6. AH-WPF-01 완료 기준 충족 여부
7. 제외 범위 미침범 여부
8. 경계 규칙 준수 여부
9. 남은 리스크
10. 다음 단계 제안
```

Summary는 완료 증명이 아니라 리뷰 시작 자료다.
Build 결과와 변경 파일을 반드시 포함해야 한다.

---

## AH-WPF-01 Closeout

### 최종 판정

```text
최종 판정: ACCEPT

검증 결과
- Build 성공
- Run 성공
- Dashboard Shell 표시 성공
- PLC Card List 표시 성공
- Status Ring / Badge 표시 성공
- Detail Pane 열기/닫기 성공
- Detail Pane 닫힘 시 카드 영역 확장 성공
- Summary Header / Card List / Trend Panel 우측 경계 정렬 성공
- 횡 스크롤 표시 성공
```

### 완료 항목(고정 구조)

```text
- MainWindow
- DashboardView
- PlcStatusCard UserControl
- PlcDetailPane UserControl
- CommunicationTrendChart Placeholder
- DashboardSnapshot DTO
- PlcCardSnapshot DTO
- RuntimeHealthSnapshot DTO
- PlcConnectionState
- RuntimeDashboardEvent Skeleton
- IRuntimeDashboardAdapter
- FakeDashboardRuntimeAdapter
- DashboardViewModel
- PlcStatusCardViewModel
- PlcDetailPaneViewModel
- Theme / ResourceDictionary 최소 구조
```

### Repair History

```text
Repair-01: Dashboard Visual Structure Alignment
- 카드 밀도, Status Ring / Badge, Trend Placeholder, Detail Pane 구조 보정
- 일부 레이아웃 이슈 발견

Repair-02: Detail Pane Column Collapse Fix
- Detail Pane 닫힘 시 컬럼 폭 0 처리
- DetailPaneColumnWidth 추가

Repair-03: Main Grid Layout Alignment
- Detail Pane이 Card List + Trend 영역 전체 높이를 차지하도록 Grid.RowSpan=2 구조로 변경

Repair-04: Dashboard Width Boundary Alignment
- DetailPaneGapWidth 추가
- Detail Pane 닫힘 시 gap도 0으로 축소
- Summary Header / Card List / Trend Panel 경계 정렬 완료
```

### 유지 중인 경계 규칙

```text
- WPF UI는 FakePlc를 직접 참조하지 않는다.
- WPF UI는 XgtDriverCore를 직접 참조하지 않는다.
- WPF UI는 Runtime 내부 구현체를 직접 참조하지 않는다.
- UI는 IRuntimeDashboardAdapter를 통해 DashboardSnapshot을 받는다.
- FakeDashboardRuntimeAdapter는 정적 Snapshot만 반환한다.
- 실제 PLC 통신은 아직 연결하지 않는다.
```

### 남은 리스크 / 미세 조정 후보

```text
- 카드 내부 텍스트 잘림 보정
- PLC 이름 / 라인 이름 표시 폭 정리
- Detail Pane 실제 데이터 밀도 개선
- Trend Chart 실제 데이터 연결 전 Placeholder 개선
- 창 크기 변경 / DPI별 레이아웃 확인
- 색상 / 폰트 / 여백 세부 튜닝
- 선택된 카드 하이라이트 개선
```

### 다음 시나리오 후보(권장 순서)

```text
1) AH-WPF-02: Fake Live Update Prototype
2) AH-WPF-03: PlcEditorDialog Static Prototype
3) AH-WPF-04: RealtimeEventLogPopup Static Prototype
4) AH-WPF-05: RuntimeDashboardAdapter Skeleton
```
