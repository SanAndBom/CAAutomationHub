# CODEX_RULES.md

# AutomationHub Codex 작업 공통 규칙

## 1. 목적

이 문서는 AutomationHub 프로젝트에서 Codex가 작업할 때 반드시 따라야 하는 공통 규칙이다.

Codex는 단순히 코드를 많이 작성하는 역할이 아니라, 정해진 시나리오의 목표와 경계를 지키며 작은 단위로 안전하게 구현하는 역할을 가진다.

---

## 2. 기본 원칙

### 2.1 Think Before Coding

코딩을 시작하기 전에 먼저 다음을 확인한다.

- 현재 레포 구조
- 관련 프로젝트 위치
- 기존 코드 스타일
- 이번 작업의 포함 범위
- 이번 작업의 제외 범위
- 변경 예상 파일 목록
- Build 가능성

작업 지시에서 “계획 먼저”라고 요청받은 경우, 절대 바로 코딩하지 않는다.

---

### 2.2 Simplicity First

현재 시나리오 통과에 필요한 최소 구조만 만든다.

금지:

- 필요 이상의 추상화
- 과도한 프레임워크 도입
- 불필요한 디자인 시스템 확장
- 실제 통신까지 미리 연결
- 아직 요청하지 않은 기능 선구현

---

### 2.3 Surgical Changes

이번 작업과 직접 관련된 파일만 수정한다.

금지:

- 기존 통신 계층 public API 변경
- FakePlc 프로토콜 변경
- Runtime 내부 구조 변경
- unrelated refactoring
- 전체 구조 대공사

---

### 2.4 Goal-Driven Execution

작업은 반드시 Scenario ID 기준으로 수행한다.

예:

```text
AH-WPF-01 Dashboard Shell + PLC Card Static Prototype
```

Codex Summary에도 Scenario ID를 반드시 포함한다.

---

## 3. 계층 경계 규칙

AutomationHub의 계층 경계는 다음과 같다.

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

WPF UI는 다음을 직접 참조하지 않는다.

```text
FakePlc
XgtDriverCore
Runtime 내부 구현체
```

UI는 다음 경로로만 상태를 받는다.

```text
IRuntimeDashboardAdapter
→ DashboardSnapshot
→ DashboardViewModel
→ PlcStatusCardViewModel
→ PlcStatusCard UserControl
```

---

## 4. 금지 사항

Codex는 명시적 지시가 없는 한 다음 작업을 하지 않는다.

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
사용자 로그인 / 사용자 관리 / 권한 관리
과도한 MVVM 프레임워크 도입
과도한 애니메이션 / 디자인 과설계
기존 통신 프로젝트 public API 변경
FakePlc 프로토콜 변경
Runtime 내부 구조 변경
```

---

## 5. Summary 제출 규칙

Codex는 작업 완료 후 반드시 아래 형식으로 Summary를 제출한다.

```text
1. Summary
2. 변경 파일 목록
3. 각 파일 변경 이유
4. 실행한 명령
5. Build/Test 결과
6. Scenario 완료 기준 충족 여부
7. 제외 범위 미침범 여부
8. 경계 규칙 준수 여부
9. 남은 리스크
10. 다음 단계 제안
```

Summary는 완료 증명이 아니라 리뷰 시작 자료다.
Build 결과와 변경 파일 목록은 반드시 포함한다.
