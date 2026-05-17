# AH-RUNTIME-51 Closeout

## 1. Summary

AH-RUNTIME-51은 AH-RUNTIME-50에서 제안한 PLC별 FLOW.JSON schema draft 후보를 바탕으로 Template / Binding Validation Rule을 검토한 Boundary Review다.

이번 단계의 핵심 결론은 validation rule을 Structural Validation, Binding Validation, Policy Validation으로 나누는 것이다.

- Structural Validation은 schemaVersion, flow.id, flow.kind, flow.initialState, flow.steps, duplicate step id, unknown transition target 같은 구조 무결성을 검증한다.
- Binding Validation은 flow가 참조하는 signal, layout, dbQueryPolicyRef, payloadLayoutRef, errorCode binding이 PLC별 bindings에 존재하는지 검증한다.
- Policy Validation은 startRequest / completeRequest 동시 ON, WaitRequestOff timeout, MissingPolicy, ErrorWriteExpected, ACK policy 같은 업무 정책 미정 상태를 검출한다.

이번 단계에서는 production code, test code, FLOW.JSON 파일, JSON schema, parser, executor, C# model, XGT Adapter, DB Query, Payload Builder, project reference, WPF, Contracts, ContextPublisher 수정은 하지 않는다.

## 2. Goal

AH-RUNTIME-51의 목표는 실제 schema draft나 executor로 넘어가기 전에 validation rule 체계를 정리하는 것이다.

핵심 질문은 다음이다.

- required binding 누락을 어떻게 검출할 것인가?
- duplicate step id와 unknown transition target을 어떻게 분류할 것인가?
- MissingPolicy는 error, warning, blockReview 중 무엇으로 볼 것인가?
- AckAction이 있는데 signal binding이 없으면 어떻게 처리할 것인가?
- ErrorWriteExpected=true인데 errorCode signal이 없으면 어떻게 처리할 것인가?
- QueryBusinessData step과 dbQueryPolicyRef의 관계는 무엇인가?
- BuildPayload / WritePayload step과 payloadLayoutRef의 관계는 무엇인가?
- startRequest / completeRequest 동시 ON policy는 어디에서 검증할 것인가?
- WaitRequestOff step과 timeout policy의 관계는 무엇인가?
- flow.kind와 required bindings 불일치를 어떻게 검증할 것인가?

## 3. Boundary

Runtime core는 여전히 vendor-neutral이다.

Runtime core는 다음을 소유하지 않는다.

- FLOW.JSON parser
- XGT execution
- DB query
- payload layout
- PLC별 address
- SQL policy
- Flow Executor
- XGT Adapter
- Payload Builder

Validation Rule은 아직 실행 코드가 아니라 design-time / authoring-time 검토 규칙이다.

## 4. Validation Categories

### 4.1 Structural Validation

- S-01 schemaVersion 누락 금지
- S-02 flow.id 누락 금지
- S-03 flow.kind 누락 금지
- S-04 flow.initialState 누락 금지
- S-05 flow.steps 비어 있음 금지
- S-06 duplicate step id 금지
- S-07 unknown transition target 금지
- S-08 initialState가 상태 후보에 존재하는지 확인
- S-09 step.action이 허용된 action set에 속하는지 확인

### 4.2 Binding Validation

- B-01 bindings.plcId 누락 금지
- B-02 startRequest signal 누락 금지
- B-03 startAck signal 누락 금지
- B-04 WorkStartComplete이면 completeRequest / completeAck 필요
- B-05 AckAction이 있으면 대응 signal binding 필요
- B-06 ErrorWriteExpected=true이면 errorCode signal 필요
- B-07 QueryBusinessData step이면 dbQueryPolicyRef 필요
- B-08 BuildPayload / WritePayload step이면 payloadLayoutRef 필요
- B-09 ExtractLotId step이면 lotLayout 필요
- B-10 WaitRequestOff step이면 request signal binding 필요

### 4.3 Policy Validation

- P-01 startRequest / completeRequest 동시 ON policy 필요
- P-02 WaitRequestOff step이면 timeout policy 필요
- P-03 MissingPolicy는 warning / blockReview / error 중 하나로 분류
- P-04 errorPolicy.writeExpected=true인데 targetRef 누락 금지
- P-05 errorPolicy.bestEffort 여부 명시 권장
- P-06 ackPolicy.value 누락 금지
- P-07 flow.kind와 required bindings 일치 확인
- P-08 완공 flow가 없는 PLC에서는 complete 계열 binding 누락을 error로 보지 않음
- P-09 payload write가 없는 flow에서는 payloadLayoutRef 누락을 error로 보지 않음
- P-10 error write policy가 없는 경우 허용 여부를 명시해야 함

## 5. Severity

- ERROR: 실행 불가능. 반드시 차단.
- BLOCK_REVIEW: 실행 가능성은 있으나 정책 미정. 사람 검토 필요.
- WARNING: 실행 가능하지만 장기 유지보수 위험.
- INFO: 참고 정보.

## 6. flow.kind별 Required Binding

### WorkStartOnly

Required:

- startRequest
- startAck
- lotLayout
- dbQueryPolicyRef
- payloadLayoutRef

Conditionally required:

- errorCode
- timeoutPolicyRef

Not required:

- completeRequest
- completeAck

### WorkCompleteOnly

Required:

- completeRequest
- completeAck

Conditionally required:

- completeLotIdOffset
- timeoutPolicyRef
- errorCode

Not required:

- startRequest
- startAck
- dbQueryPolicyRef
- payloadLayoutRef

### WorkStartComplete

Required:

- startRequest
- startAck
- completeRequest
- completeAck
- lotLayout
- dbQueryPolicyRef
- payloadLayoutRef
- priorityPolicy.whenBothRequestsOn

Conditionally required:

- errorCode
- timeoutPolicyRef

## 7. Excluded Scope

이번 AH-RUNTIME-51에서는 다음을 하지 않았다.

- production code 수정
- test code 수정
- FLOW.JSON 파일 생성
- JSON schema 생성
- parser 구현
- executor 구현
- C# model 생성
- XGT Adapter 구현
- DB Query 구현
- Payload Builder 이식
- project reference 추가
- WPF 수정
- Contracts 수정
- ContextPublisher 수정
- commit

## 8. Next Candidate

추천 다음 단계:

AH-RUNTIME-52 Validation Rule Matrix Documentation

목표:
RuleId / Category / Severity / Condition / Required Field / Failure Meaning / Suggested Message 형태로 validation rule matrix를 문서화한다.

## 9. Self-Check

판정: ACCEPTABLE_DIRECTION

이유:

- AH-RUNTIME-50의 schema draft 후보를 구현으로 비약하지 않았다.
- Runtime core vendor-neutral boundary를 유지했다.
- FLOW.JSON을 XGT command list가 아니라 Business Flow Definition으로 유지했다.
- flow / bindings / metadata 분리를 보존했다.
- Template / Binding Validation Rule을 Structural / Binding / Policy 세 범주로 분류했다.
- 다음 단계가 validator 구현이 아니라 Validation Rule Matrix 문서화임을 제안했다.
