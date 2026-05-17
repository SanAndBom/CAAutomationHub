# Cognitive Sync Check

## 1. Purpose

이 문서는 새 채팅방 또는 새 Codex 세션에서 AutomationHub Runtime 장기 맥락이 복원되었는지 확인하는 cognitive checksum 문서다.

목적은 Codex/GPT가 handoff 요약만 반복하지 않고, 실제 repo 문서와 git 상태를 대조해 Core Invariant / Boundary / Harness / Current Anchor / Current Goal을 같은 의미로 복원했는지 검증하는 것이다.

## 2. When to Use

다음 시점에 사용한다.

- 새 채팅방 시작
- 새 Codex 세션 시작
- 큰 챕터 전환
- Runtime / WPF / Driver / FakePlc / Harness 경계 변경 전
- Codex가 엉뚱한 repo / 문서 / 목표를 참조한다고 느껴질 때
- 작업 지시문을 경량화하기 전
- handoff 직전 또는 handoff 검증 직후

## 3. Required Context Files

### 필수

- AGENTS.md
- docs/context/META_IPRO_CODEX_COGNITIVE_INTERFACE.md
- docs/context/COGNITIVE_SYNC_CHECK.md
- docs/harness/AH-RUNTIME-50.md
- 최신 docs/harness/AH-RUNTIME-xx.md Closeout
- 새 채팅 handoff summary

### 보조

- docs/context/WPF_RUNTIME_BRIDGE_CURRENT_STATE.md
- docs/harness/AH-RUNTIME-49.md
- docs/harness/AH-RUNTIME-48.md
- docs/harness/AH-RUNTIME-47.md
- docs/project-document-map.html
- docs/agents-review.html
- 관련 WPF / Runtime tests

## 4. Current Anchors

- 최신 전체 anchor: DOCS-REVIEW-01
  - commit: fe33af8
  - message: docs: add project documentation review boards
  - 의미: project-document-map.html, agents-review.html 추가
- 최신 Runtime anchor: AH-RUNTIME-51
  - commit: 3424507
  - message: docs: close out AH-RUNTIME-51 template binding validation review
  - 의미: Template / Binding Validation Rule Review closeout
- 다음 Runtime 목표: AH-RUNTIME-52
  - 의미: Validation Rule Matrix Documentation

## 5. Cognitive Check Questions

새 채팅 시작 시 아래 질문에 답할 수 있어야 한다.

### 1. 최신 전체 anchor는 무엇인가?

정답: DOCS-REVIEW-01 / commit fe33af8. project-document-map.html, agents-review.html 문서 리뷰 보드가 추가된 anchor다.

### 2. 최신 Runtime anchor는 무엇인가?

정답: AH-RUNTIME-51 / commit 3424507. Template / Binding Validation Rule Review closeout이며 validation rule을 Structural Validation, Binding Validation, Policy Validation으로 분류했다.

### 3. 다음 Runtime 목표는 무엇인가?

정답: AH-RUNTIME-52: Validation Rule Matrix Documentation.

### 4. ContextPublisher 자동 publish는 현재 사용하는가?

정답: 아니오. 현재 사용하지 않는다. Runtime 작업의 primary historical record는 docs/harness/AH-RUNTIME-xx.md Closeout이다.

### 5. FLOW.JSON은 무엇인가?

정답: FLOW.JSON은 XGT command list가 아니라 PLC별 Business Flow Definition이다. 초기 구조는 PLC별 단일 FLOW.JSON이며, 단일 파일 내부는 schemaVersion / flow / bindings / metadata 구조를 가진다.

### 6. FLOW.JSON의 장기 분리 방향은 무엇인가?

정답: 장기적으로 flow는 공통 template으로, bindings는 PLC별 binding으로 분리 가능해야 한다. AH-RUNTIME-51은 이 Template / Binding Validation Rule을 검토했고, AH-RUNTIME-52는 validator 구현 전에 rule matrix를 문서화하는 단계다.

### 7. Runtime core는 FLOW.JSON을 직접 소유하는가?

정답: 아니오. Runtime core는 FLOW.JSON parser, XGT-specific flow execution, DB query, payload layout, PLC별 address, SQL policy를 소유하지 않는다.

### 8. Business flow의 실제 실행 계층은 어디인가?

정답: Flow Executor / Adapter / DB Query / Payload Builder / ACK/Error Writer 계층이 실제 실행을 담당한다.

### 9. Runtime core의 최신 vendor boundary는 무엇인가?

정답: CAAutomationHub.Runtime core는 vendor-neutral이어야 한다. Runtime core는 XgtDriverCore, FakePlc, XgtChannelRunner를 직접 참조하지 않는다.

### 10. ChannelPollingTarget과 ChannelPollingResult의 의미는 무엇인가?

정답: ChannelPollingTarget은 PLC-level / PlcId 중심 target model이다. ChannelPollingResult는 PLC-level / vendor-neutral polling event result이며 LOTID, DB result, ACK policy 같은 business transaction detail을 넣지 않는다.

### 11. PollingCycleCoordinator의 책임은 무엇인가?

정답: ChannelPollingResult batch를 single-writer로 publish하는 boundary다. Runtime polling state path를 보존하며 pilot business flow와 섞지 않는다.

### 12. WorkStartPilotService.RunOnceAsync(...)는 어떤 의미인가?

정답: 직접 복사 / 직접 reference 대상이 아니라 검증된 pilot business flow anchor다. 기존 분석 흐름은 전체 업무 flow 중 착공요청 ON 시나리오의 일부로 해석한다.

### 13. 전체 pilot business flow에는 무엇이 포함되는가?

정답: Polling request detection, 착공요청 ON, 착공ACK OFF, 완공요청 ON, 완공ACK OFF, 대기 복귀가 포함된다.

### 14. Pilot Flow Scenario Matrix는 어떻게 읽어야 하는가?

정답: WorkStart verified flow와 사용자 business anchor를 함께 담되 Source / EvidenceLevel / PolicyStatus로 구분한다.

### 15. 새 채팅 Cognitive Sync는 어떻게 PASS 판정하는가?

정답: Handoff 요약만으로 PASS를 주지 않는다. 실제 repo 문서와 git 상태를 대조한 뒤 PASS / PARTIAL / FAIL로 판정한다.

### 16. Codex 진행 설명 / Closeout / 지시문은 어떤 언어로 작성하는가?

정답: 한국어로 작성한다.

### 17. ACCEPT는 무엇을 뜻하는가?

정답: 기능 성공뿐 아니라 의미, 경계, 검증이 유지되었음을 뜻한다.

### 18. Verification evidence 없는 완료 판정이 가능한가?

정답: 불가.

## 6. Pass Criteria

새 채팅 동기화 성공 기준:

- 최신 전체 anchor가 DOCS-REVIEW-01 / fe33af8임을 알고 있음
- 최신 Runtime anchor가 AH-RUNTIME-51 / 3424507임을 알고 있음
- 다음 목표가 AH-RUNTIME-52 Validation Rule Matrix Documentation임을 알고 있음
- ContextPublisher 자동 publish 미사용 정책을 알고 있음
- docs/harness/AH-RUNTIME-xx.md Closeout이 primary historical record임을 알고 있음
- FLOW.JSON은 XGT command list가 아니라 PLC별 Business Flow Definition임을 알고 있음
- 초기 FLOW.JSON 구조가 schemaVersion / flow / bindings / metadata 후보임을 알고 있음
- Runtime core vendor-neutral 경계를 알고 있음
- Runtime core가 FLOW.JSON parser / XGT execution / DB query / payload layout을 소유하지 않는다는 것을 알고 있음
- Runtime core가 XgtDriverCore / FakePlc / XgtChannelRunner를 직접 참조하지 않는다는 것을 알고 있음
- ChannelPollingResult가 PLC-level / vendor-neutral event result이며 business transaction detail을 담지 않는다는 것을 알고 있음
- WorkStartPilotService.RunOnceAsync(...)를 직접 복사 대상이 아니라 business flow anchor로 이해함
- Pilot business flow와 Runtime polling state path를 구분함
- Boundary Review -> Closeout -> Commit 흐름을 유지함
- Codex 진행 설명 / Closeout / 지시문은 한국어로 작성한다는 정책을 알고 있음
- 새 채팅 Cognitive Sync는 handoff 요약만이 아니라 실제 repo 문서와 git 상태를 대조해야 함을 알고 있음

## 7. Failure Signals

다음이 발생하면 인지 복원 실패 가능성이 있음:

- ContextPublisher 자동 publish를 다시 필수라고 말함
- docs/harness/AH-RUNTIME-xx.md Closeout을 primary historical record로 보지 않음
- 최신 전체 anchor를 DOCS-REVIEW-01 / fe33af8로 복원하지 못함
- 최신 Runtime anchor를 AH-RUNTIME-51 / 3424507로 복원하지 못함
- AH-RUNTIME-52 대신 AH-RUNTIME-31이나 구현 단계로 바로 넘어가려 함
- FLOW.JSON을 XGT command list로 설명함
- Runtime core에 FLOW.JSON parser를 넣으려 함
- Runtime core에 XGT address / DB query / payload layout을 넣으려 함
- Runtime core가 XgtDriverCore / FakePlc / XgtChannelRunner를 직접 참조해도 된다고 말함
- ChannelPollingResult에 LOTID / DB result / ACK policy를 넣으려 함
- Pilot business flow와 Runtime polling state path를 섞음
- WorkStartPilotService를 Runtime core에 그대로 복사하려 함
- Handoff만 요약하고 git / 문서 대조 없이 PASS를 줌
- WPF_RUNTIME_BRIDGE_CURRENT_STATE.md를 최신 Runtime source of truth로 사용함
- Closeout 없이 완료/커밋을 권장함

## 8. Cross-Chat Cognitive Sync Verification Routine

새 채팅방의 Cognitive Sync 응답은 그 자체로 완료 판정이 아니다. PASS 판정은 handoff 요약만으로 내리지 않는다.

기본 흐름:

1. 새 채팅방에 Handoff 메시지를 입력한다.
2. 새 채팅방은 Handoff 이후 Cognitive Sync 응답을 생성한다.
3. 새 채팅방 또는 기준 채팅방은 실제 repo 문서와 git 상태를 대조한다.
4. 사용자는 필요하면 그 응답을 이전 채팅방 또는 기준 채팅방에 되가져와 검증한다.
5. 완료 상태, 금지 범위, 다음 목표, Runtime / WPF / Driver / FakePlc / Harness 경계, commit / 문서 anchor 유지 여부를 기준으로 응답을 점검한다.
6. 검증 결과를 PASS / PARTIAL / FAIL로 표현한다.
7. PARTIAL 또는 FAIL이면 Handoff 메시지나 context 문서를 보정한다.
8. 보정된 내용을 다음 채팅방 전환에 반영해 전환 품질을 높인다.

확인 명령 후보:

- git log --oneline -5
- git status --short
- Get-Content AGENTS.md
- Get-Content docs/context/META_IPRO_CODEX_COGNITIVE_INTERFACE.md
- Get-Content docs/context/COGNITIVE_SYNC_CHECK.md
- Get-Content docs/harness/AH-RUNTIME-50.md
- Get-Content docs/harness/AH-RUNTIME-49.md
- Get-Content docs/harness/AH-RUNTIME-48.md
- Get-Content docs/harness/AH-RUNTIME-47.md
- Get-Content docs/project-document-map.html
- Get-Content docs/agents-review.html

판정 기준:

- PASS: 실제 repo 문서와 git 상태 대조 후 완료 상태, 금지 범위, 다음 목표, Runtime / WPF / Driver / FakePlc / Harness 경계, commit / 문서 anchor가 모두 유지됨.
- PARTIAL: 핵심 방향은 맞지만 일부 완료 상태, 금지 범위, 다음 목표, anchor가 누락되거나 모호함.
- FAIL: ContextPublisher 정책, Runtime shared execution path, FLOW.JSON boundary, Runtime core vendor-neutral boundary, Harness, ACCEPT 의미, 다음 목표 중 하나 이상을 잘못 복원함.

이 루틴은 장기 프로젝트 맥락의 cognitive checksum 역할을 한다. 목적은 새 채팅방이 문서의 단편을 읽었는지가 아니라, 장기 계약의 연결선을 실제로 복원했는지 확인하는 것이다.

## 9. Recovery Protocol

복원 실패 시:

1. git log --oneline -5 확인
2. git status --short 확인
3. META_IPRO_CODEX_COGNITIVE_INTERFACE.md 다시 읽기
4. COGNITIVE_SYNC_CHECK.md 다시 읽기
5. 최신 AH-RUNTIME Closeout 확인
6. 새 채팅 handoff summary 재검토
7. Cognitive Check Questions 재수행
8. 여전히 불일치하면 이전 긴 맥락 채팅으로 돌아와 보정

## 10. New Chat Bootstrap Template

새 채팅 첫 메시지 템플릿:

```text
CAAutomationHub Runtime 전환 작업을 이어갑니다.

현재 최신 전체 anchor는 DOCS-REVIEW-01 / commit fe33af8입니다.
현재 최신 Runtime anchor는 AH-RUNTIME-51 / commit 3424507입니다.
AH-RUNTIME-51은 Template / Binding Validation Rule Review closeout이며, validation rule을 Structural Validation, Binding Validation, Policy Validation으로 분류했습니다.
다음 Runtime 목표는 AH-RUNTIME-52: Validation Rule Matrix Documentation입니다.

ContextPublisher 자동 publish는 현재 사용하지 않습니다.
Runtime 작업의 primary historical record는 docs/harness/AH-RUNTIME-xx.md Closeout입니다.
새 채팅 Cognitive Sync는 Handoff 요약만으로 PASS 판정하지 말고 실제 repo 문서와 git 상태를 대조해야 합니다.

먼저 AGENTS.md, docs/context/META_IPRO_CODEX_COGNITIVE_INTERFACE.md, docs/context/COGNITIVE_SYNC_CHECK.md, docs/harness/AH-RUNTIME-50.md, docs/harness/AH-RUNTIME-51.md 기준으로 현재 인지 상태를 맞추고, 아래 질문에 답해 주세요.

1. 최신 전체 anchor와 최신 Runtime anchor
2. 다음 Runtime 목표
3. ContextPublisher 자동 publish 정책
4. FLOW.JSON이 XGT command list가 아니라 PLC별 Business Flow Definition이라는 계약
5. Runtime core vendor-neutral 경계
6. Runtime core가 FLOW.JSON parser / XGT execution / DB query / payload layout을 소유하지 않는다는 계약
7. WorkStartPilotService.RunOnceAsync(...)를 직접 복사 대상이 아니라 business flow anchor로 보는 이유
8. PASS 판정 전 repo 문서와 git 상태를 대조하는 방법
```

## 11. Next Chat Exit Protocol

다음 채팅도 무거워지면:

1. 마지막 완료 AH ID 확인
2. 마지막 commit hash 확인
3. working tree clean 확인
4. Implemented / Not Yet / Risk 업데이트
5. 다음 AH 후보 정리
6. 새 handoff summary 생성
7. COGNITIVE_SYNC_CHECK.md 질문 유지 또는 업데이트
8. 실제 repo 문서와 git 상태 대조 기준 포함
9. 새 채팅으로 이동
