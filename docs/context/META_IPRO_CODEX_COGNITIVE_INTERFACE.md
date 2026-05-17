# META IPRO CODEX Cognitive Interface

## 1. Purpose

이 문서는 메타몽, 이프로, Codex가 CAAutomationHub Runtime 전환 작업에서 공유해야 하는 장기 인지 계약이다.

목적은 새 채팅방이나 새 Codex 세션으로 이동할 때 장기 맥락이 손실되지 않도록 하고, Codex/GPT가 국소 구현에 매몰되어 AutomationHub의 Runtime / WPF / Driver / FakePlc / Harness 경계를 잃지 않도록 돕는 것이다.

이 문서는 단순 요약이 아니다. 새 채팅방에서도 관계, 책임, 계약, 검증 흐름을 복원하기 위한 기억 저장소다. 반복되는 긴 지시문은 줄이고, 고정 계약은 문서에 저장하며, 작업 지시문은 현재 Goal / Context / Constraints / Done When 호출에 집중한다.

AutomationHub 작업은 다음 구조를 유지한다.

- Goal
- Context
- Constraints
- Done When
- Harness
- Boundary
- Validation
- ACCEPT

## 2. Participant Roles

### 이프로

- 최종 의미 판단자
- 우선순위 결정자
- 현장 리스크 판단자
- 계약 승인자
- 프로젝트의 현실 장력 보존자

### 메타몽

- 장기 맥락 정렬자
- Cognitive Map 작성/정리자
- drift 감지자
- 지시문/검증문서 설계자
- 이프로의 사고를 구조화하는 동반자

### Codex

- 계약 기반 실행자
- 코드 수정/테스트/빌드 수행자
- Evidence 생성자
- 변경 diff 보고자
- Core Invariant / Boundary / Harness Contract 기준 작업자

Codex는 독립적으로 장기 철학을 재해석하지 않는다. Codex는 명시된 계약, closeout 기록, 현재 작업 지시를 기준으로 구현, 검증, 보고한다. 불확실한 내용은 단정하지 않고 "확인 필요"로 표시한다.

## 3. Core Philosophy

AutomationHub는 단순 기능 모음이 아니라 기존 PLC별 1:1 중계 구조를 대체하는 장기 Runtime 플랫폼이다.

중요한 것은 코드를 빠르게 생성하는 것이 아니라 무엇을 절대 깨지 않는가를 지키는 것이다.

- 언어보다 계약이 중요하다.
- 문서보다 연결선이 중요하다.
- 완료보다 검증된 완료가 중요하다.
- AI/Codex는 국소 구현은 강하지만 장기 맥락을 잃을 수 있다.
- Harness는 단순 테스트 도구가 아니라 장기 맥락과 계약을 보존하는 장치다.
- 긴 지시문 반복은 임시 보호장치였다.
- 앞으로는 고정 계약은 문서화하고, 지시문은 현재 작업 호출 중심으로 경량화한다.

## 4. Core Invariants

AutomationHub 작업에서 아래 원칙은 우선 보존되어야 한다.

- FlowTest는 독립 실행자가 아니다.
- FlowTest / Runtime / Simulation은 가능한 한 동일한 실행 경로를 공유해야 한다.
- Runtime shared execution path를 깨뜨리면 안 된다.
- Runtime은 canonical state의 중심이다.
- UI는 Runtime 내부 polling, reconnect, driver 세부 책임을 침범하지 않는다.
- Driver는 protocol / transport / recovery primitive 책임에 집중한다.
- Supervisor는 재연결, 회복, 균형, 운영 안정성의 상위 조정자다.
- FakePlc는 단순 mock이 아니라 통신/장애 시나리오 검증 하네스다.
- Harness 없는 완료는 완료가 아니다.
- Verification evidence 없는 완료는 완료가 아니다.
- ACCEPT는 단순 기능 성공이 아니라 의미, 경계, 검증이 유지되었음을 뜻한다.

현재 AH-RUNTIME 맥락의 추가 invariants:

- IRuntimePlcChannel은 read-only state provider다.
- IWritableRuntimePlcChannel은 선택적 writable boundary다.
- RuntimeChannelRegistry는 lookup-only / collection 관리 책임을 갖는다.
- update와 publish는 분리한다.
- GetSnapshotAsync는 cache-only다.
- RefreshSnapshotAsync는 InMemoryAutomationHubSupervisor concrete side-effect API다.
- Runtime 프로젝트는 CAAutomationHub.Contracts만 참조한다.
- CAAutomationHub.Runtime core는 vendor-neutral이어야 한다.
- Runtime core는 XgtDriverCore를 직접 참조하지 않는다.
- Runtime core는 FakePlc를 직접 참조하지 않는다.
- Runtime core는 XgtChannelRunner를 직접 참조하지 않는다.
- ChannelPollingTarget은 PLC-level / PlcId 중심 target model이다.
- ChannelPollingResult는 PLC-level / vendor-neutral polling event result이다.
- ChannelPollingResult에 business transaction detail을 넣지 않는다.
- PollingCycleCoordinator는 ChannelPollingResult batch를 single-writer로 publish하는 boundary다.
- Pilot business flow는 Runtime polling state path와 구분한다.
- FLOW.JSON은 XGT command list가 아니라 PLC별 Business Flow Definition이다.
- 초기 FLOW.JSON 구조는 schemaVersion / flow / bindings / metadata 후보를 따른다.
- 장기적으로 flow는 공통 template으로, bindings는 PLC별 binding으로 분리 가능해야 한다.
- Runtime core는 FLOW.JSON parser / XGT execution / DB query / payload layout을 소유하지 않는다.
- Runtime core는 PLC별 address / payload layout / SQL policy를 알지 않는다.
- Business flow execution은 Flow Executor / Adapter / Handler 계층에서 담당한다.
- Flow Executor / Adapter / DB Query / Payload Builder / ACK/Error Writer가 실제 실행 계층이다.
- WorkStartPilotService.RunOnceAsync(...)는 직접 복사 / 직접 reference 대상이 아니라 검증된 pilot business flow anchor다.
- ContextPublisher 자동 publish는 현재 사용하지 않는다.
- docs/harness/AH-RUNTIME-xx.md Closeout이 primary historical record다.
- 새 채팅방 Cognitive Sync는 Handoff 요약만이 아니라 실제 repo 문서와 git 상태를 대조해야 한다.

## 5. Current Cognitive Map

```text
[AutomationHub Core Purpose]
        ↓
[기존 PLC별 1:1 중계 구조 대체]
        ↓
[Runtime Shared Execution Path]
        ↓
[Runtime / Supervisor / ChannelRegistry]
        ↓
[ChannelPollingTarget / ChannelPollingResult]
        ↓
[PollingCycleCoordinator]
        ↓
[RuntimePlcChannelState]
        ↓
[RuntimeSnapshot]
        ↓
[SupervisorRuntimeSnapshotProvider]
        ↓
[RuntimeDashboardAdapter / RuntimeDashboardSnapshotMapper]
        ↓
[DashboardSnapshot]
        ↓
[WPF Dashboard UI]
```

```text
[PLC별 Business Flow Definition: FLOW.JSON]
        ↓
[schemaVersion / flow / bindings / metadata]
        ↓
[Template / Binding Validation Rule]
        ↓
[Flow Executor / Adapter / Handler]
        ↓
[DB Query / Payload Builder / ACK/Error Writer]
        ↓
[Vendor-specific driver boundary]
```

```text
Goal
 ↕
Context
 ↕
Boundary
 ↕
Harness
 ↕
Validation
 ↕
Evidence
 ↕
ACCEPT
```

## 6. Boundary Map

### WPF UI

- Boundary: WPF UI ↔ Adapter / Snapshot
- 허용되는 책임: Runtime 상태를 DashboardSnapshot 형태로 표현하고 사용자에게 표시한다.
- 금지되는 책임: Runtime 내부 polling, reconnect, driver 세부 책임을 직접 수행하거나 침범하지 않는다.
- 검증 방법: WPF 테스트와 snapshot mapper/adapter 계약 검증.
- 관련 Harness: WPF Dashboard tests, RuntimeDashboardAdapter 관련 테스트.
- 확인 필요: 실제 App.xaml.cs wiring 및 runtime mode opt-in은 아직 후속 작업이다.

### Runtime

- Boundary: Runtime ↔ Channel / Supervisor / Polling coordination
- 허용되는 책임: canonical state 관리, channel registry 관리, snapshot cache/publish orchestration, supervisor lifecycle coordination, vendor-neutral polling event publish coordination.
- 금지되는 책임: XGT protocol/transport primitive 직접 구현, WPF UI 책임 수행, FLOW.JSON parser 소유, XGT-specific flow execution 소유, PLC별 address / payload layout / SQL policy / DB query 소유.
- 검증 방법: Runtime unit tests, supervisor/channel registry tests, polling coordinator tests, project reference 확인.
- 관련 Harness: CAAutomationHub.Runtime.Tests, docs/harness/AH-RUNTIME-xx.md Closeout.
- 확인 필요: AH-RUNTIME-51 이후 Template / Binding Validation Rule이 parser/executor 구현으로 너무 빨리 비약하지 않도록 단계 분리 필요.

### Business Flow Definition / FLOW.JSON

- Boundary: Business Flow Definition ↔ Flow Executor / Adapter / Handler
- 허용되는 책임: PLC별 업무 흐름을 schemaVersion / flow / bindings / metadata 구조로 표현한다.
- 금지되는 책임: XGT command list로 축소, Runtime core 내부 parser로 흡수, Runtime polling state path와 혼합.
- 검증 방법: Boundary Review, Pilot Flow Scenario Matrix, Template / Binding Validation Rule Review.
- 관련 Harness: docs/harness/AH-RUNTIME-47.md, AH-RUNTIME-48.md, AH-RUNTIME-49.md, AH-RUNTIME-50.md.
- 확인 필요: 실제 FLOW.JSON 파일, JSON schema file, parser, executor는 아직 구현하지 않았다.

### Pilot Business Flow

- Boundary: Pilot business flow ↔ Runtime polling state path
- 허용되는 책임: WorkStart verified flow와 사용자 business anchor를 Source / EvidenceLevel / PolicyStatus로 구분해 pilot scenario matrix에 남긴다.
- 금지되는 책임: WorkStartPilotService.RunOnceAsync(...)를 Runtime core에 그대로 source copy하거나 직접 reference 대상으로 삼지 않는다.
- 검증 방법: Pilot Flow Scenario Matrix와 AH-RUNTIME-50 closeout 대조.
- 관련 Harness: docs/harness/AH-RUNTIME-47.md, AH-RUNTIME-50.md.
- 확인 필요: 전체 업무 흐름은 Polling request detection, 착공요청 ON, 착공ACK OFF, 완공요청 ON, 완공ACK OFF, 대기 복귀를 포함한다.

### XgtDriverCore

- Boundary: XgtDriverCore ↔ Transport / Protocol
- 허용되는 책임: protocol, transport, recovery primitive 보존.
- 금지되는 책임: Runtime canonical state를 직접 소유하거나 WPF 표시 계약을 결정하지 않는다.
- 검증 방법: XgtDriverCore 기존 테스트와 후속 Runtime adapter integration 테스트.
- 관련 Harness: XgtDriverCore tests, FakePlc scenario harness.
- 확인 필요: XgtRuntimePlcChannelAdapter, XgtChannelRunner integration은 아직 구현되지 않았다.

### FakePlc / Real PLC

- Boundary: FakePlc / Real PLC ↔ protocol/transport boundary
- 허용되는 책임: 실제 PLC 또는 통신/장애 시나리오를 재현하는 검증 대상/하네스 역할.
- 금지되는 책임: Runtime 내부 상태 계약을 우회하거나 테스트 전용 실행 경로를 운영 경로와 diverge시키지 않는다.
- 검증 방법: FakePlc scenario tests, timeout/malformed response/reconnect scenarios.
- 관련 Harness: FakePlc, FlowTest, XgtDriverCore validation.
- 확인 필요: FP-09 timeout 의미가 Runtime integration에서 어떻게 유지될지 후속 검토 필요.

## 7. Harness / Validation Contract

- FakePlc는 단순 mock이 아니라 통신/장애 시나리오 검증 장치다.
- XgtDriverCore는 protocol / transport / recovery 의미를 보존해야 한다.
- Runtime harness는 실제 운영 경로와 divergence가 생기면 안 된다.
- WPF 테스트는 Runtime 상태를 UI에 표현하는 계약을 검증한다.
- FLOW.JSON 관련 작업은 Boundary Review와 Closeout을 통해 Business Flow Definition / Template / Binding 경계를 먼저 고정한다.
- 새 채팅방 Cognitive Sync는 Handoff 요약만이 아니라 실제 repo 문서와 git 상태를 대조한다.
- RED -> GREEN -> Closeout 흐름을 유지한다.
- Verification-before-completion을 유지한다.
- Evidence 없이 완료 판정은 금지한다.
- 판정 언어는 ACCEPT / ACCEPT_WITH_CORRECTION / REPAIR_REQUIRED / HOLD를 유지한다.

## 8. Active Goals / Recent Anchors

### DOCS-REVIEW-01

- Goal ID: DOCS-REVIEW-01
- 상태: 최신 전체 anchor
- commit: fe33af8
- message: docs: add project documentation review boards
- 핵심 의미: project-document-map.html, agents-review.html 추가.
- 역할: 문서 지도 / AGENTS 리뷰 보드.
- 연결된 계약: 새 채팅방은 Handoff 요약만 보지 말고 실제 repo 문서와 git 상태를 대조해야 한다.

### AH-RUNTIME-50

- Goal ID: AH-RUNTIME-50
- 상태: 최신 Runtime anchor
- commit: 6c027d8
- message: docs: close out AH-RUNTIME-50 pilot flow schema draft review
- 핵심 의미: Pilot Flow Schema Draft Boundary Review closeout.
- 핵심 구조: schemaVersion / flow / bindings / metadata.
- 연결된 계약: FLOW.JSON은 XGT command list가 아니라 PLC별 Business Flow Definition이다. Runtime core는 parser / XGT execution / DB query / payload layout을 소유하지 않는다.
- 다음 연결: AH-RUNTIME-51 Template / Binding Validation Rule Review.

### AH-RUNTIME-51

- Goal ID: AH-RUNTIME-51
- 상태: 다음 Runtime 목표
- 핵심 의미: Template / Binding Validation Rule Review.
- 연결된 계약: flow template과 PLC별 bindings의 분리 가능성, validation rule, source/evidence/policy status 경계를 검토한다.
- 주의: schema/parser/executor 구현으로 바로 뛰어들지 않는다.

### Archived / Historical: AH-RUNTIME-31

- Goal ID: AH-RUNTIME-31
- 상태: 완료된 과거 작업
- 과거 의미: ChannelPollingResult / ChannelPollingFailureKind / RuntimePlcChannelStateMapper Skeleton.
- 현재 의미: 더 이상 next goal로 사용하지 않는다.
- 주의: 새 채팅방이 AH-RUNTIME-31을 다음 목표로 제시하면 오래된 문맥을 복원한 것으로 보고 보정한다.

## 9. Implemented / Not Yet Implemented / Risk Map

### Implemented

- Runtime project skeleton
- IAutomationHubSupervisor
- RuntimeSnapshotChangedEventArgs
- SupervisorRuntimeSnapshotProvider
- SupervisorRuntimeDashboardLifecycle
- InMemoryAutomationHubSupervisor
- RuntimeChannelRegistry
- IRuntimePlcChannel
- IWritableRuntimePlcChannel
- RuntimePlcChannelState
- InMemoryRuntimePlcChannel
- ReplaceState
- GetRuntimeState
- RefreshSnapshotAsync
- PollingPublishCoordinator
- PollingChannelUpdate
- PollingPublishResult
- ChannelPollingResult
- ChannelPollingFailureKind
- RuntimePlcChannelStateMapper
- PollingResultStateOrchestrator
- PollingResultStateOrchestrator batch support
- PollingCycleCoordinator
- ChannelPollingTarget
- IPollingTargetProvider
- RuntimeChannelPollingTargetProvider
- DashboardRuntimeCompositionFactory
- DashboardRuntimeCapabilities
- Pilot Flow Scenario Matrix 문서화
- FLOW.JSON Boundary Review
- FLOW.JSON Template vs Binding Boundary Review
- Pilot Flow Schema Draft Boundary Review
- Documentation review boards
- ContextPublisher 자동 publish 미사용 정책

### Not Yet Implemented

- actual PollingScheduler timer / loop
- XgtRuntimePlcChannelAdapter 또는 XgtAdapter
- XgtDriverCore integration
- XgtChannelRunner integration
- FakePlc integration
- Runtime telemetry contract
- Runtime command dispatcher
- Runtime Event Bridge
- WPF actual App wiring
- runtime mode opt-in
- DashboardViewModel capability injection
- FLOW.JSON parser
- Flow Executor
- PilotFlow class
- JSON schema file
- actual FLOW.JSON files
- DB Query Abstraction implementation
- XGT Operation Adapter implementation
- Payload Builder extraction
- ACK/Error Writer implementation

### Risk / Drift Zone

- FlowTest가 독립 executor처럼 변질
- UI가 Runtime 내부 polling 책임을 침범
- Snapshot DTO가 canonical state가 아닌 임시 DTO로 흐려짐
- Supervisor와 Driver 책임 혼재
- FP-09 timeout 의미가 단순 timeout test로 축소
- Closeout 문서가 단순 작업 기록으로만 남고 WHY가 사라짐
- Codex가 국소 구현에 성공했지만 장기 계약을 놓치는 상황
- 새 채팅방에서 문서의 입자만 남고 연결선이 끊기는 상황
- 지시문이 너무 길어져 핵심 지시가 묻히는 상황
- 지시문이 너무 짧아져 Core Invariant가 누락되는 상황
- ContextPublisher 실패를 Runtime 코드 실패로 오해하는 상황
- PollingScheduler를 너무 일찍 timer/loop로 구현하는 상황
- RuntimePlcChannelStateMapper가 previous state 없이 stateless로 구현되어 누적 상태가 깨지는 상황
- FLOW.JSON이 XGT command list로 변질
- Runtime core가 FLOW.JSON parser를 소유
- Runtime core가 XGT address / DB query / payload layout을 소유
- ChannelPollingResult에 business transaction detail이 섞임
- Pilot business flow와 Runtime polling state path가 섞임
- WorkStartPilotService를 그대로 source copy
- Handoff 검증이 문서/깃 대조 없이 요약으로만 끝남
- AH-RUNTIME-51 전에 schema/parser/executor 구현으로 뛰어듦
- WPF_RUNTIME_BRIDGE_CURRENT_STATE.md를 최신 Runtime source of truth로 오해

## 10. Memory Hygiene Rules

- 확인된 계약만 Core Invariant에 기록한다.
- 불확실한 내용은 확인 필요로 표시한다.
- 폐기된 방향은 폐기됨으로 표시한다.
- 임시 아이디어와 확정 계약을 섞지 않는다.
- Active Goal과 Archived Goal을 구분한다.
- 오래된 정보가 현재 작업을 오염시키지 않도록 한다.
- 문서가 길어져 핵심 계약이 희석되지 않도록 한다.
- 인지맵은 모든 히스토리 저장소가 아니라 핵심 연결선 보존 장치다.
- Codex 지시문에는 매번 모든 히스토리를 넣지 않는다.
- 고정 계약은 문서에 저장하고, 지시문에는 현재 작업 호출만 남긴다.
- ContextPublisher 자동 publish는 현재 사용하지 않는다.
- Runtime 작업 기록은 docs/harness/AH-RUNTIME-xx.md Closeout 기준이다.

## 11. Chat Transition Protocol

새 채팅방 이동 전 절차:

1. 현재 Goal 상태 확인
2. 최신 전체 anchor와 최신 Runtime anchor 확인
3. git log --oneline -5 확인
4. git status --short 확인
5. 변경 파일 목록 확인
6. 테스트/빌드 결과 확인
7. ACCEPT 또는 ACCEPT_WITH_CORRECTION 판정
8. 구현된 것 / 미구현 / 위험영역 분리
9. Cognitive Map 갱신
10. 다음 채팅방용 Bootstrap Summary 생성
11. COGNITIVE_SYNC_CHECK.md 기준으로 복원 검증
12. Handoff만 요약하지 말고 실제 문서 / git 상태를 대조
13. 검증 결과를 PASS / PARTIAL / FAIL로 작성
14. PASS 전에는 AH-RUNTIME-51 구현으로 넘어가지 않음
15. 실패 시 기존 긴 맥락 채팅방으로 돌아와 보정

## 12. Cross-Chat Recovery Contract

새 채팅방의 Cognitive Sync 응답은 장기 맥락 복원 결과물이며, 필요하면 이전 채팅방 또는 기준 채팅방으로 되가져와 검증한다.

이 검증은 단순 문답 확인이 아니라 cognitive checksum이다. 새 채팅방이 완료 상태, 금지 범위, 다음 목표, Runtime / WPF / Driver / FakePlc / Harness 경계, commit / 문서 anchor를 같은 의미로 복원했는지 확인한다.

판정은 PASS / PARTIAL / FAIL로 표현한다. PASS는 Handoff 요약만으로 줄 수 없고, 실제 repo 문서와 git 상태 대조가 필요하다. PARTIAL 또는 FAIL이면 Handoff 메시지나 context 문서를 보정하고, 보정된 내용을 다음 전환에 반영한다.

이 루틴은 Codex가 국소 문서 입자만 읽고 장기 계약의 연결선을 잃는 drift를 줄이기 위한 recovery contract다. Compact Mode / Full Context Mode / Delta Mode 정책을 바꾸지 않으며, ContextPublisher 자동 publish 재도입을 뜻하지 않는다.

## 13. Codex Instruction Length Policy

### 1층: 고정 계약

- AGENTS.md
- META_IPRO_CODEX_COGNITIVE_INTERFACE.md
- COGNITIVE_SYNC_CHECK.md
- Core Invariants / Boundary / Harness / ACCEPT / Memory Hygiene

### 2층: 현재 목표 맥락

- 현재 Goal
- 관련 commit
- 관련 문서
- 이번 챕터의 anchor

### 3층: 이번 작업 지시

- Goal
- Context
- Constraints
- Boundary
- Harness / Validation
- Commands
- Report
- Done When

### 운영 모드

- Compact Mode: 기본값
- Full Context Mode: 새 채팅방, 큰 전환, 문서 최초 생성, Core Invariant 변경 가능성
- Delta Mode: 수정/검증/보정

### 최소 안전 문구

"AGENTS.md의 Core Invariants / Boundary / Harness / ACCEPT 규칙을 따른다.
ContextPublisher 자동 publish는 현재 사용하지 않는다.
작업 기록은 docs/harness/AH-RUNTIME-xx.md Closeout을 primary historical record로 사용한다.
Verification evidence 없는 완료 판정은 금지한다.
불확실한 내용은 확인 필요로 표시한다.
Codex Self-Check 판정은 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정한다."

## 14. Current Next Step

다음 Runtime:

- AH-RUNTIME-51: Template / Binding Validation Rule Review

현재 금지된 비약:

- FLOW.JSON parser 구현
- Flow Executor 구현
- JSON schema file 생성
- actual FLOW.JSON files 생성
- XGT Adapter 구현
- DB Query 구현
- Payload Builder extraction
- WorkStartPilotService source copy
