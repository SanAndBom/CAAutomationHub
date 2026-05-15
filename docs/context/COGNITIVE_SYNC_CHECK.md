# Cognitive Sync Check

## 1. Purpose

이 문서는 새 채팅방 또는 새 Codex 세션에서 AutomationHub Runtime 장기 맥락이 복원되었는지 확인하는 cognitive checksum 문서다.

목적은 Codex/GPT가 문서 입자만 읽고 연결선을 놓치는지 점검하고, Core Invariant / Boundary / Harness / Current Goal 복원이 되었는지 확인하는 것이다. 새 채팅에서 다음 새 채팅으로 넘어갈 때도 같은 방식으로 반복 사용한다.

## 2. When to Use

다음 시점에 사용한다.

- 새 채팅방 시작
- 새 Codex 세션 시작
- 큰 챕터 전환
- Runtime/WPF/Driver/FakePlc 경계 변경 전
- Codex가 엉뚱한 repo / 문서 / 목표를 참조한다고 느껴질 때
- 작업 지시문을 경량화하기 전
- handoff 직전

## 3. Required Context Files

### 필수

- AGENTS.md
- docs/context/META_IPRO_CODEX_COGNITIVE_INTERFACE.md
- docs/context/COGNITIVE_SYNC_CHECK.md
- 최신 docs/harness/AH-RUNTIME-xx.md Closeout
- 새 채팅 handoff summary

### 보조

- docs/context/WPF_RUNTIME_BRIDGE_CURRENT_STATE.md
- 관련 AH-RUNTIME Closeout
- 관련 WPF / Runtime tests

## 4. Cognitive Check Questions

새 채팅 시작 시 아래 질문에 답할 수 있어야 한다.

### 1. ContextPublisher 자동 publish는 현재 사용하는가?

정답: 아니오. 현재 사용하지 않음. docs/harness Closeout이 primary historical record.

### 2. IRuntimePlcChannel과 IWritableRuntimePlcChannel의 차이는?

정답: IRuntimePlcChannel은 read-only publish state provider. IWritableRuntimePlcChannel은 선택적 writable boundary이며 ReplaceState / GetRuntimeState 제공.

### 3. update와 publish는 분리되어 있는가?

정답: 예. ReplaceState는 SnapshotChanged를 발생시키지 않고 RefreshSnapshotAsync를 호출하지 않음. publish는 caller/coordinator가 명시적으로 refreshSnapshotAsync 호출.

### 4. RuntimeChannelRegistry의 책임은?

정답: channel collection 관리, duplicate validation, GetChannels, GetStates, TryGetChannel. update/publish 책임 없음.

### 5. GetState(capturedAt)와 GetRuntimeState()의 차이는?

정답: GetState(capturedAt)는 ChannelRuntimeState publish DTO 반환. GetRuntimeState()는 RuntimePlcChannelState internal state 반환.

### 6. 다음 Runtime 목표는?

정답: AH-RUNTIME-31: ChannelPollingResult / ChannelPollingFailureKind / RuntimePlcChannelStateMapper Skeleton.

### 7. XgtDriverCore / FakePlc는 지금 Runtime core에 연결되었는가?

정답: 아니오. 아직 연결하지 않음. 후속 adapter/integration 후보.

### 8. PollingScheduler timer/loop는 구현되었는가?

정답: 아니오. PollingPublishCoordinator까지만 있음.

### 9. ACCEPT는 무엇을 뜻하는가?

정답: 기능 성공뿐 아니라 의미, 경계, 검증이 유지되었음을 뜻함.

### 10. Verification evidence 없는 완료 판정이 가능한가?

정답: 불가.

## 5. Pass Criteria

새 채팅 동기화 성공 기준:

- ContextPublisher 자동 publish 미사용 정책을 알고 있음
- AH-RUNTIME-30-3까지 완료 상태를 알고 있음
- 다음 후보가 AH-RUNTIME-31임을 알고 있음
- RuntimeChannelRegistry / IRuntimePlcChannel / IWritableRuntimePlcChannel / RuntimePlcChannelState / PollingPublishCoordinator 관계를 설명할 수 있음
- update와 publish 분리를 설명할 수 있음
- XGT/FakePlc/WPF wiring이 아직 제외임을 알고 있음
- docs/harness Closeout이 primary historical record임을 알고 있음

## 6. Failure Signals

다음이 발생하면 인지 복원 실패 가능성이 있음:

- ContextPublisher 자동 publish를 다시 필수라고 말함
- IRuntimePlcChannel에 ReplaceState가 있다고 착각함
- RuntimeChannelRegistry가 update/publish 책임을 갖는다고 말함
- PollingScheduler timer/loop가 이미 구현되었다고 말함
- XgtDriverCore가 Runtime에 이미 연결되었다고 말함
- AH-RUNTIME-31 대신 엉뚱한 next goal로 이동함
- WPF가 Runtime polling 책임을 가져야 한다고 말함
- Closeout 없이 완료/커밋을 권장함

## 7. Recovery Protocol

복원 실패 시:

1. META_IPRO_CODEX_COGNITIVE_INTERFACE.md 다시 읽기
2. 최신 AH-RUNTIME Closeout 확인
3. 새 채팅 handoff summary 재붙여넣기
4. Cognitive Check Questions 재수행
5. 여전히 불일치하면 이전 긴 맥락 채팅으로 돌아와 보정

## 8. New Chat Bootstrap Template

새 채팅 첫 메시지 템플릿:

```text
CAAutomationHub Runtime 전환 작업을 이어갑니다.
현재 AH-RUNTIME-30-3까지 완료했고, working tree는 clean입니다.
ContextPublisher 자동 publish는 현재 사용하지 않으며, docs/harness/AH-RUNTIME-xx.md Closeout을 primary historical record로 사용합니다.
다음 목표는 AH-RUNTIME-31: ChannelPollingResult / ChannelPollingFailureKind / RuntimePlcChannelStateMapper Boundary Review 또는 Skeleton입니다.

먼저 AGENTS.md, docs/context/META_IPRO_CODEX_COGNITIVE_INTERFACE.md, docs/context/COGNITIVE_SYNC_CHECK.md 기준으로 현재 인지 상태를 맞추고, 아래 질문에 답해 주세요.

1. IRuntimePlcChannel과 IWritableRuntimePlcChannel의 차이
2. update와 publish 분리 정책
3. RuntimeChannelRegistry의 책임
4. ContextPublisher 자동 publish 정책
5. 다음 Runtime 목표
```

## 9. Next Chat Exit Protocol

다음 채팅도 무거워지면:

1. 마지막 완료 AH ID 확인
2. 마지막 commit hash 확인
3. working tree clean 확인
4. Implemented / Not Yet / Risk 업데이트
5. 다음 AH 후보 정리
6. 새 handoff summary 생성
7. COGNITIVE_SYNC_CHECK.md 질문 유지 또는 업데이트
8. 새 채팅으로 이동
