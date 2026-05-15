# AGENTS.md

이 문서는 AutomationHub Rebuild 프로젝트에서 Codex가 작업 시작 전 확인해야 하는 최상위 실행 계약이다.

목적은 단순 코드 생성이 아니라, 장기 프로젝트의 맥락, 구현 이벤트, 검증 흐름, 하네스 원칙, Boundary 계약을 보존하는 것이다.

---

## 1. Implementation Event Rule

### When to emit

다음 상황에서 반드시 Implementation 이벤트를 생성한다.

* 코드 구조 변경 (refactor)
* 기능 추가 또는 수정
* 로직 흐름 변경
* 예외 처리 변경
* 테스트 보강 또는 시나리오 추가
* Runtime / Driver / WPF / Harness 경계에 영향을 주는 변경

다음 경우는 제외 가능하다.

* 단순 포맷팅
* 주석만 변경
* 의미 없는 변수명 변경
* 문서 typo 수정

---

### How to emit

코드 변경이 완료되면 다음 명령을 실행한다.

```bash
AutomationHub.ContextPublisher.exe --emit-implementation --title "<short title>" --summary "<what changed and why>" --status DONE
```

### Title Rule

짧고 명확하게 작성한다.
변경 대상과 핵심 내용을 포함한다.

예:

* "PlcChannel timeout handling refactor"
* "PollingWorker retry logic update"
* "FakePlc malformed response scenario added"
* "DashboardSnapshot runtime bridge contract update"

### Summary Rule

Summary에는 다음을 포함한다.

* 무엇을 변경했는지
* 왜 변경했는지
* 어떤 영향이 있는지
* 관련 Harness / Boundary / Validation 영향이 있는지

예:

* "PlcChannel timeout 처리 경로를 정리하고 예외 흐름을 단순화함"
* "PollingWorker 재시도 조건을 분리해 실패 처리 흐름을 명확히 함"
* "DashboardSnapshot 매핑 책임을 Runtime boundary 안에서 정렬함"

## 2. Required Behavior

Codex는 코드 변경 작업을 완료한 후 반드시 Implementation 이벤트를 생성해야 한다.

이벤트를 생성하지 않고 작업을 종료하면 안 된다.

다만 이번 작업처럼 AGENTS.md 생성만 수행하는 문서 선행 작업에서는 Implementation 이벤트 실행은 생략 가능하다.
이 경우 마지막 응답에 "문서 선행 작업이므로 Implementation 이벤트 생략"이라고 명시한다.

## 3. Relation to Verification

Implementation 이벤트 후 관련 테스트가 있다면 실행한다.

테스트 결과는 가능하면 Verification 이벤트로 기록한다.

작업 완료 전에는 다음을 확인해야 한다.

* 빌드 또는 테스트가 필요한 변경인지
* 실행한 명령과 결과
* 변경 파일 목록
* Harness / Boundary / Validation 영향
* ACCEPT 또는 ACCEPT_WITH_CORRECTION 판정 가능 여부

## 4. Cognitive Contract Reference

AutomationHub는 단순 기능 모음이 아니라 기존 PLC 중계 구조를 대체하는 장기 Runtime 플랫폼이다.

중요한 것은 코드를 빠르게 생성하는 것보다, 장기 계약과 경계를 깨지 않는 것이다.

향후 다음 문서가 생성되면 Codex는 관련 작업 전 반드시 참조해야 한다.

* docs/context/META_IPRO_CODEX_COGNITIVE_INTERFACE.md
* docs/context/COGNITIVE_SYNC_CHECK.md

위 문서가 아직 없을 경우, Codex는 해당 문서가 없음을 보고하고, 현재 AGENTS.md의 Core Invariants를 기준으로 작업한다.

## 5. Core Invariants

다음 원칙은 AutomationHub 작업에서 우선 보존되어야 한다.

* FlowTest는 독립 실행자가 아니다.
* FlowTest / Runtime / Simulation은 가능한 한 동일한 실행 경로를 공유해야 한다.
* Runtime shared execution path를 깨뜨리면 안 된다.
* Runtime은 canonical state의 중심이다.
* UI는 Runtime 내부 polling, reconnect, driver 세부 책임을 침범하지 않는다.
* Driver는 protocol / transport / recovery primitive 책임에 집중한다.
* Supervisor는 재연결, 회복, 균형, 운영 안정성의 상위 조정자다.
* FakePlc는 단순 mock이 아니라 통신/장애 시나리오 검증 하네스다.
* Harness 없는 완료는 완료가 아니다.
* Verification evidence 없는 완료는 완료가 아니다.
* ACCEPT는 단순 기능 성공이 아니라 의미, 경계, 검증이 유지되었음을 뜻한다.

## 6. Work Instruction Shape

Codex 작업 지시문은 가능하면 다음 구조를 따른다.

* Goal
* Context
* Constraints
* Done when

AutomationHub 작업에서는 여기에 다음 흐름도 함께 보존한다.

* Harness
* Boundary
* Validation
* ACCEPT

작업 응답에는 가능하면 다음을 포함한다.

1. Summary
2. 변경 파일 목록
3. 각 파일 변경 이유
4. 실행한 명령
5. 테스트/빌드 결과
6. Harness / Boundary 영향
7. ACCEPT 판정 또는 남은 리스크

## 7. Boundary Awareness

Runtime, WPF Dashboard, FlowTest, FakePlc, XgtDriverCore, Supervisor 관련 작업 전에는 다음을 점검한다.

* 이 변경이 어느 Boundary에 영향을 주는가?
* 이 변경이 Runtime shared execution path를 깨뜨리는가?
* UI가 Runtime 내부 책임을 침범하는가?
* Driver 책임과 Supervisor 책임이 섞이는가?
* Harness 검증 경로가 운영 경로와 diverge하는가?

불확실하면 구현하지 말고 Boundary Review로 전환한다.

## 8. Cognitive Sync Awareness

채팅방 전환, Codex 세션 전환, 큰 작업 챕터 전환 전에는 장기 맥락 손실 가능성을 고려한다.

향후 COGNITIVE_SYNC_CHECK.md가 생성되면 새 세션은 해당 문서의 Cognitive Checksum을 통해 맥락 복원 여부를 검증한다.

현재 최소 Cognitive Checksum은 다음과 같다.

1. FlowTest는 Runtime shared execution path를 검증해야 한다.
2. Runtime은 canonical state의 중심이다.
3. UI는 Runtime 내부 polling/reconnect 책임을 침범하지 않는다.
4. XgtDriverCore는 protocol/transport/recovery primitive 책임에 집중한다.
5. Supervisor는 회복/재연결/운영 안정성 조정자다.
6. Harness 없는 완료는 완료가 아니다.
7. Verification evidence 없는 완료는 완료가 아니다.
8. Snapshot은 임시 DTO가 아니라 Runtime 상태 계약이다.
9. ACCEPT는 기능 성공이 아니라 의미/경계/검증이 유지된 상태다.
10. 불확실한 내용은 단정하지 말고 확인 필요로 표시한다.

## 9. Memory Hygiene Rules

장기 맥락 문서에는 확인된 계약과 임시 아이디어를 섞지 않는다.

* 확인된 계약만 Core Invariant에 기록한다.
* 불확실한 내용은 확인 필요로 표시한다.
* 폐기된 방향은 폐기됨으로 표시한다.
* 임시 아이디어와 확정 계약을 분리한다.
* Active Goal과 Archived Goal을 구분한다.
* 오래된 정보가 현재 작업을 오염시키지 않도록 주의한다.

## 10. Project Goal

이 규칙의 목적은 다음 흐름을 유지하는 것이다.

Code Change
→ Implementation Event
→ Test / Build
→ Verification Evidence
→ Context Document
→ Cognitive Contract 유지
→ ACCEPT 판정

Codex는 국소 구현을 수행하되, 장기 계약과 하네스 경계를 훼손하지 않아야 한다.
