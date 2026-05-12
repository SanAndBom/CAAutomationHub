# SUMMARY_REVIEW_RUBRIC.md

# Codex Summary 검토 기준

## 1. 목적

이 문서는 Codex 작업 Summary를 검토할 때 사용하는 판정 기준이다.

Summary는 완료 증명이 아니다.
Summary는 리뷰를 시작하기 위한 자료다.

---

## 2. 기본 판정

Codex 작업 결과는 아래 네 가지 중 하나로 판정한다.

```text
ACCEPT
ACCEPT_WITH_RISK
REPAIR_REQUIRED
REJECT_AND_REPLAN
```

---

## 3. ACCEPT

다음 조건을 만족하면 ACCEPT로 판정한다.

```text
작업 목표와 일치한다.
포함 범위만 구현했다.
제외 범위를 건드리지 않았다.
Build가 성공했다.
계층 경계 규칙을 지켰다.
변경 파일 목록과 변경 이유가 명확하다.
다음 작업으로 진행 가능하다.
```

---

## 4. ACCEPT_WITH_RISK

다음 경우 ACCEPT_WITH_RISK로 판정한다.

```text
주요 목표는 충족했다.
Build는 성공했다.
다만 일부 구조적 리스크나 보완 항목이 있다.
다음 작업 진행은 가능하지만 주의가 필요하다.
```

예:

```text
스타일 구조가 다소 미흡함
Placeholder가 너무 단순함
일부 명칭 정리가 필요함
테스트 프로젝트는 없음
```

---

## 5. REPAIR_REQUIRED

다음 경우 REPAIR_REQUIRED로 판정한다.

```text
구조 방향은 맞다.
하지만 Build 실패 또는 핵심 일부 누락이 있다.
수리 지시를 통해 회복 가능하다.
```

예:

```text
Build 오류
XAML 바인딩 오류
Detail Pane 닫기 미동작
횡 스크롤 미표시
Summary 형식 일부 누락
```

---

## 6. REJECT_AND_REPLAN

다음 경우 REJECT_AND_REPLAN으로 판정한다.

```text
목표에서 벗어났다.
계층 경계를 위반했다.
실제 통신을 붙였다.
FakePlc 또는 XgtDriverCore를 UI에서 직접 참조했다.
Runtime 내부 구조를 바꿨다.
과도한 프레임워크를 도입했다.
대규모 구조 변경을 했다.
```

---

## 7. AH-WPF-01 전용 검토 체크리스트

```text
[ ] Scenario ID가 AH-WPF-01인가?
[ ] WPF 프로젝트가 Build 되는가?
[ ] MainWindow / DashboardView가 존재하는가?
[ ] PlcStatusCard가 UserControl로 분리되었는가?
[ ] DashboardView가 ItemsControl + ObservableCollection 기반인가?
[ ] PLC Card가 8~12개 표시되는가?
[ ] Connected / Warning / Disconnected / Congested 상태가 구분되는가?
[ ] 우측 Detail Pane이 열리는가?
[ ] 우측 Detail Pane이 닫히는가?
[ ] Detail Pane 닫힘 시 카드 영역이 확장되는가?
[ ] 카드 영역에 횡 스크롤이 표시되는가?
[ ] Trend Chart Placeholder가 있는가?
[ ] IRuntimeDashboardAdapter가 있는가?
[ ] FakeDashboardRuntimeAdapter가 정적 Snapshot을 반환하는가?
[ ] WPF UI가 FakePlc를 직접 참조하지 않는가?
[ ] WPF UI가 XgtDriverCore를 직접 참조하지 않는가?
[ ] WPF UI가 Runtime 내부 구현체를 직접 참조하지 않는가?
[ ] 사용자 관리 기능이 추가되지 않았는가?
[ ] Build 명령과 결과가 Summary에 포함되었는가?
[ ] 남은 리스크가 명시되었는가?
```

---

## 8. 수리 지시 작성 원칙

REPAIR_REQUIRED인 경우 수리 지시는 짧고 명확하게 작성한다.

```text
수리 대상
수리 이유
수정 허용 파일
수정 금지 파일
재검증 기준
```

예:

```text
AH-WPF-01 Repair-01

문제:
Detail Pane X 버튼 클릭 시 IsDetailPaneVisible이 false로 변경되지 않습니다.

수정 허용:
DashboardViewModel.cs
PlcDetailPane.xaml
PlcDetailPane.xaml.cs

수정 금지:
Adapters/
Models/Dashboard/
XgtDriverCore/
FakePlc/

완료 기준:
X 버튼 클릭 시 Detail Pane이 닫히고 카드 영역이 확장되어야 합니다.
Build 결과를 다시 제출하세요.
```
