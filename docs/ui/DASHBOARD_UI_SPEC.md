# DASHBOARD_UI_SPEC.md

# AutomationHub Dashboard UI 설계 기준

## 1. 화면 범위

1차 Pilot UI의 중심은 PLC 통신 관제 Dashboard다.

Flow Designer, 사용자 관리, 실제 PLC 연결 설정 저장 로직은 1차 범위에서 제외한다.

---

## 2. 최종 화면 목록

```text
1. MainDashboard_Overview
2. MainDashboard_WithDetail
3. AddPlcPopup_BeforeTest
4. AddPlcPopup_TestSuccess
5. AddPlcPopup_TestFailed
6. RealtimeEventLogPopup
```

AH-WPF-01에서는 1번과 2번의 구조 Skeleton만 우선 구현한다.

---

## 3. 공통 디자인 톤

```text
Dark Industrial Dashboard
PLC Communication Monitor
Control Room Style
WPF Desktop Application Feel
```

---

## 4. 공통 색상 의미

```text
매우 원활: Blue / Cyan
원활: Green
주의: Yellow
정체: Orange
오류: Red
비활성 / 테스트 전: Gray
```

---

## 5. MainDashboard_Overview

우측 Detail Pane이 닫힌 상태다.

구성:

```text
Top Header
Dashboard Summary Header
PLC Card Horizontal Strip
Horizontal Scrollbar
Communication Trend Chart
```

목적:

```text
다수 PLC 상태를 한눈에 보기
카드 영역을 최대 폭으로 사용
PLC가 많을 때 횡 스크롤 제공
```

---

## 6. MainDashboard_WithDetail

PLC Card 하나를 선택했을 때 우측 Detail Pane이 열린 상태다.

구성:

```text
Top Header
Dashboard Summary Header
PLC Card Area
Right Detail Pane
Communication Trend Chart
```

우측 Detail Pane은 선택된 PLC의 원인 분석 출발점이다.

---

## 7. PlcStatusCard UserControl

PLC Card는 반드시 하나의 UserControl로 분리한다.

구성:

```text
PlcStatusCard
├── Header
│   ├── PLC Name
│   └── Line Name
├── Status Ring 또는 Badge
├── Connection State
├── Network Summary
│   ├── IP / Port
│   ├── Polling Interval
│   └── Last Response
├── Packet Summary
│   ├── TX / RX
│   └── Error Count
└── Mini Trend Placeholder
```

개발자는 `PlcStatusCard.xaml` 하나만 관리한다.
PLC 개수가 늘어나면 ItemsControl이 같은 Card를 반복 생성한다.

---

## 8. Dashboard Summary Header

표시 항목:

```text
PLC 통신 상태 개요
전체 N개
정상 N
주의 N
정체 N
오류 N
[새로고침]
[+ PLC 추가]
```

---

## 9. Right Detail Pane

표시 조건:

```text
PLC Card 클릭 시 표시
X 클릭 시 닫힘
닫히면 카드 영역 확장
```

구성:

```text
SelectedPlcDetailPane
├── Header
│   ├── Title: 선택된 PLC 상세 정보
│   └── Close Button X
├── Selected PLC Identity
├── Network Info Section
├── Packet / Error Summary Section
├── Recent Events Section
├── Control Section
└── Flow Control Footer
```

---

## 10. Communication Trend Chart

AH-WPF-01에서는 Placeholder로 구현해도 된다.

구성:

```text
통신 트렌드 (최근 30분)
Legend
Y Axis: 평균 지연(ms)
X Axis: 시간
Multi Series Lines 또는 Placeholder
```

---

## 11. RealtimeEventLogPopup 단순화 방향

기존 복잡한 이벤트 전체보기 화면은 1차에서 단순화한다.

방향:

```text
페이징 중심 이벤트 관리 화면
❌

Rolling Buffer + Auto Scroll 실시간 로그 화면
⭕
```

구성:

```text
RealtimeEventLogPopup
├── Header
│   ├── Title: 최근 이벤트 로그
│   ├── Auto Scroll Toggle
│   ├── Pause / Resume Button
│   └── Close Button
├── Compact Filter Bar
│   ├── Severity Quick Filter
│   ├── PLC Filter
│   └── Search Box
├── Realtime Event List
└── Footer
    ├── 표시 개수
    ├── Clear View
    └── Export
```

AH-WPF-01에서는 구현하지 않는다.
후속 AH-WPF-04에서 Static Prototype으로 진행한다.

---

## 12. 사용자 관리 기능 제외

1차 Pilot에서는 다음을 구현하지 않는다.

```text
사용자 로그인
사용자 권한 관리
User Manager
계정별 Role / Permission
Ack User 관리
```

상단 admin 표시는 제거하거나 임시 고정 표시로만 처리한다.
