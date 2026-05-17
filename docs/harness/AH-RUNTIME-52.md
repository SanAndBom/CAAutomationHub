# AH-RUNTIME-52 Closeout - Validation Rule Matrix Documentation

## 1. Summary

AH-RUNTIME-52는 AH-RUNTIME-51에서 분류한 Template / Binding Validation Rule을 validator 구현 전에 사용할 Validation Rule Matrix로 고정한 문서 작업이다.

이번 작업에서는 validation rule을 Structural Validation, Binding Validation, Policy Validation 세 범주로 나누고, 각 rule의 `RuleId`, 대상, 조건, severity, message intent, execution blocking 여부를 문서화했다.

핵심 영향은 이후 Validator Skeleton Boundary Review 또는 Validator Model Skeleton 단계에서 rule 의미와 severity 판단을 재해석하지 않도록 기준점을 제공하는 것이다. Runtime core vendor-neutral boundary는 유지되며, production code, test code, parser, JSON schema, C# model, executor, XGT Adapter, DB Query, Payload Builder 구현은 수행하지 않는다.

## 2. Goal

AH-RUNTIME-52의 목표는 AH-RUNTIME-51의 Template / Binding Validation Rule Review 결과를 바탕으로 validator 구현 전 단계의 rule matrix를 문서화하는 것이다.

이 문서는 다음 질문에 답한다.

- 어떤 validation rule이 Structural / Binding / Policy 범주에 속하는가?
- 각 rule은 Error / Warning / Info / ReviewRequired 중 어떤 severity를 가지는가?
- 어떤 rule이 execution을 차단해야 하는가?
- validator skeleton으로 넘어가기 전에 어떤 evidence와 message intent를 유지해야 하는가?

이번 단계는 rule matrix documentation이며 validator 구현 단계가 아니다.

## 3. Context

최신 Runtime anchor는 AH-RUNTIME-51 / commit `3424507`이다.

AH-RUNTIME-51은 AH-RUNTIME-50의 FLOW.JSON schema draft 후보를 기반으로 Template / Binding Validation Rule을 검토했고, validation rule을 다음 세 범주로 분류했다.

- Structural Validation: schemaVersion, flow identity, initial state, step, transition 같은 구조 무결성.
- Binding Validation: flow가 참조하는 signal, layout, DB query policy, payload layout, ACK, errorCode binding의 존재와 일관성.
- Policy Validation: both request ON, request OFF wait, ACK OFF, error write, timeout, recovery, draft policy 같은 업무 정책 준비 상태.

AH-RUNTIME-52는 이 분류를 구현으로 옮기지 않고 문서상 matrix로 고정한다.

## 4. Boundary

Runtime core는 계속 vendor-neutral이어야 한다.

Runtime core는 다음 책임을 소유하지 않는다.

- FLOW.JSON parser
- JSON schema
- XGT-specific execution
- DB query
- payload layout
- PLC address
- SQL policy
- Flow Executor
- XGT Adapter
- Payload Builder
- ACK/Error Writer implementation

Runtime core는 `XgtDriverCore`, `FakePlc`, `XgtChannelRunner`를 직접 참조하지 않는다.

`ChannelPollingResult` / `PollingCycleCoordinator` / `RuntimeSnapshot` path와 Pilot Business Flow path는 섞지 않는다. Validation Rule Matrix는 business flow definition authoring boundary에 속하며 Runtime polling state path의 contract를 변경하지 않는다.

이번 작업은 문서 작업만 수행한다.

## 5. Validation Rule Matrix

### Structural Validation

Structural Validation은 FLOW.JSON 후보 문서의 기본 구조와 flow graph의 무결성을 확인한다.

이 범주의 rule은 대체로 실행 전 차단 대상이다. 구조가 깨진 상태에서는 binding이나 policy 검토 결과도 신뢰하기 어렵기 때문이다.

포함 rule:

- MissingSchemaVersion
- UnsupportedSchemaVersion
- MissingFlowId
- MissingFlowKind
- MissingInitialState
- MissingSteps
- DuplicateStepId
- UnknownTransitionTarget
- MissingStepType
- InvalidStepTransition

### Binding Validation

Binding Validation은 template 또는 flow step이 요구하는 binding이 PLC별 binding 영역에 존재하는지 확인한다.

이 범주의 rule은 실행 가능성과 직접 연결된다. 다만 `UnusedBinding`처럼 실행 차단보다 유지보수 경고에 가까운 rule도 있다.

포함 rule:

- MissingSignalBinding
- MissingPayloadLayoutBinding
- MissingDbQueryPolicyBinding
- MissingAckBinding
- MissingErrorCodeBinding
- UnusedBinding
- BindingTypeMismatch
- BindingReferencesUnknownStep

### Policy Validation

Policy Validation은 flow가 현장 운전 정책을 충분히 명시했는지 확인한다.

이 범주의 rule은 Error와 ReviewRequired를 구분해야 한다. 구조나 binding은 존재하지만 업무 정책이 미정이면 validator는 구현자 임의 판단으로 실행을 허용하지 말고 ReviewRequired로 남겨야 한다.

포함 rule:

- StartAndCompleteRequestBothOn
- MissingWaitRequestOffPolicy
- MissingAckOffPolicy
- MissingErrorWritePolicy
- MissingTimeoutPolicy
- MissingRecoveryPolicy
- ErrorWriteExpectedButNoErrorCodeBinding
- CompleteRequestWithoutStartContext
- PolicyStatusDraftBlocksExecution

## 6. Severity Policy

### Error

Error는 validator가 execution을 차단해야 하는 상태다.

대표 기준:

- 필수 구조가 없다.
- flow graph가 유효하지 않다.
- step이 요구하는 binding이 없다.
- type mismatch로 handler 또는 adapter가 안전하게 해석할 수 없다.
- error write가 필요하다고 선언했지만 errorCode binding이 없다.

### Warning

Warning은 execution을 즉시 차단하지는 않지만 장기 유지보수, authoring 품질, drift 가능성을 기록해야 하는 상태다.

대표 기준:

- 정의된 binding이 flow에서 사용되지 않는다.
- optional policy가 권장되지만 현재 flow kind에서는 즉시 필수로 확정되지 않았다.
- 향후 template/binding 분리 시 정리가 필요한 흔적이 있다.

### Info

Info는 validator가 참고 evidence로 남길 수 있는 상태다.

대표 기준:

- 명시적 정책이 기본값과 일치한다.
- optional binding이 현재 flow kind에서 사용되지 않지만 의도적으로 남겨진 경우다.
- review trace를 남기는 것이 유용하지만 오류나 경고로 볼 수 없다.

### ReviewRequired

ReviewRequired는 구조적으로 실행 가능해 보이더라도 사람의 정책 승인 없이는 production execution으로 넘기면 안 되는 상태다.

대표 기준:

- both requests ON 처리 정책이 미정이다.
- WaitRequestOff, ACK OFF, timeout, recovery 정책이 아직 Draft 또는 Missing 상태다.
- `metadata.policyStatus`가 Draft이거나 review 승인 전 상태다.

ReviewRequired는 authoring iteration은 허용하되, validated runtime execution candidate로 승격하는 것은 차단한다.

## 7. Rule Table

| RuleId | Category | Target | Condition | Severity | Message Intent | Blocks Execution | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| MissingSchemaVersion | Structural Validation | top-level `schemaVersion` | `schemaVersion`이 없거나 비어 있음 | Error | 문서 구조 version을 확인할 수 없음을 알림 | Yes | schema version 없이는 호환성 판단 불가 |
| UnsupportedSchemaVersion | Structural Validation | top-level `schemaVersion` | validator가 지원하지 않는 schema version | Error | 지원되지 않는 schema version임을 알림 | Yes | 향후 version negotiation 대상 |
| MissingFlowId | Structural Validation | `flow.id` | flow id가 없거나 비어 있음 | Error | business flow identity가 없음을 알림 | Yes | template 분리 시 flow id는 추적 기준 |
| MissingFlowKind | Structural Validation | `flow.kind` | flow kind가 없거나 허용 후보가 아님 | Error | flow kind를 판단할 수 없음을 알림 | Yes | WorkStartOnly / WorkCompleteOnly / WorkStartComplete 기준 |
| MissingInitialState | Structural Validation | `flow.initialState` | initial state가 없거나 비어 있음 | Error | 시작 상태가 정의되지 않았음을 알림 | Yes | state transition 검증의 시작점 |
| MissingSteps | Structural Validation | `flow.steps` | steps가 없거나 빈 배열 | Error | 실행할 business step이 없음을 알림 | Yes | 빈 flow는 validation candidate가 아님 |
| DuplicateStepId | Structural Validation | `flow.steps[*].id` | 동일 step id가 2회 이상 등장 | Error | step id가 중복되어 transition target이 모호함을 알림 | Yes | graph resolution 차단 |
| UnknownTransitionTarget | Structural Validation | `onSuccess`, `onFailure`, `transitions[*].to` | transition target이 정의된 state 또는 step에 없음 | Error | transition target을 찾을 수 없음을 알림 | Yes | runtime fallback으로 보정하지 않음 |
| MissingStepType | Structural Validation | `flow.steps[*].type` 또는 `flow.steps[*].action` | step type/action이 없음 | Error | step 의미를 해석할 수 없음을 알림 | Yes | AH-RUNTIME-50의 action 후보와 연결 |
| InvalidStepTransition | Structural Validation | step transition graph | 종료 불가, 순환, 금지 transition, kind 불일치 등 | Error | flow transition graph가 유효하지 않음을 알림 | Yes | 세부 규칙은 skeleton review에서 분리 가능 |
| MissingSignalBinding | Binding Validation | `bindings.signals` | step 또는 policy가 참조하는 signal binding 없음 | Error | 필요한 PLC signal binding이 없음을 알림 | Yes | Runtime core가 address를 소유한다는 뜻이 아님 |
| MissingPayloadLayoutBinding | Binding Validation | `bindings.payloadLayoutRef` | BuildPayload 또는 WritePayload step이 있는데 layout binding 없음 | Error | payload layout reference가 없음을 알림 | Yes | 실제 payload layout 해석은 Runtime core 밖 |
| MissingDbQueryPolicyBinding | Binding Validation | `bindings.dbQueryPolicyRef` | QueryBusinessData step이 있는데 DB query policy binding 없음 | Error | DB query policy reference가 없음을 알림 | Yes | SQL text 또는 query 실행은 scope 밖 |
| MissingAckBinding | Binding Validation | `bindings.signals.startAck`, `bindings.signals.completeAck` | ACK action이 있는데 대응 ACK signal 없음 | Error | ACK write 대상 binding이 없음을 알림 | Yes | ACK writer 구현은 scope 밖 |
| MissingErrorCodeBinding | Binding Validation | `bindings.signals.errorCode` | error code write step/policy가 있는데 errorCode signal 없음 | Error | errorCode write 대상 binding이 없음을 알림 | Yes | ErrorWriteExpectedButNoErrorCodeBinding과 연결 |
| UnusedBinding | Binding Validation | `bindings.*` | 정의된 binding이 flow step 또는 policy에서 참조되지 않음 | Warning | 사용되지 않는 binding이 있음을 알림 | No | template/binding 분리 전 정리 후보 |
| BindingTypeMismatch | Binding Validation | referenced binding | signal/layout/policy binding type이 step 요구 type과 맞지 않음 | Error | binding type이 step 요구사항과 맞지 않음을 알림 | Yes | adapter-specific type detail은 validator model에서 추상화 |
| BindingReferencesUnknownStep | Binding Validation | binding metadata or step refs | binding이 존재하지 않는 step id를 참조 | Error | binding reference target step을 찾을 수 없음을 알림 | Yes | authoring trace metadata도 같은 기준 적용 |
| StartAndCompleteRequestBothOn | Policy Validation | priority policy | startRequest와 completeRequest가 동시에 ON일 때 처리 정책 없음 | ReviewRequired | 동시 요청 우선순위 정책이 필요함을 알림 | Yes | 현장 정책 승인 전 production execution 차단 |
| MissingWaitRequestOffPolicy | Policy Validation | WaitRequestOff step/policy | request OFF 대기 정책이 없음 | ReviewRequired | request OFF 대기 정책이 필요함을 알림 | Yes | timeout과 recovery policy로 이어짐 |
| MissingAckOffPolicy | Policy Validation | ACK OFF policy | ACK OFF 조건 또는 시점 정책이 없음 | ReviewRequired | ACK OFF 정책이 필요함을 알림 | Yes | ACK ON/OFF 생명주기 명확화 필요 |
| MissingErrorWritePolicy | Policy Validation | error policy | failure path가 있는데 error write 여부 정책 없음 | ReviewRequired | 실패 시 error write 정책을 결정해야 함을 알림 | Yes | error write를 하지 않는 것도 명시 정책이어야 함 |
| MissingTimeoutPolicy | Policy Validation | timeout policy | wait, command, recovery 관련 timeout이 없음 | ReviewRequired | timeout 기준이 필요함을 알림 | Yes | 무한 대기 drift 방지 |
| MissingRecoveryPolicy | Policy Validation | recovery policy | timeout/failure 후 recovery 또는 final status 정책 없음 | ReviewRequired | failure recovery 정책이 필요함을 알림 | Yes | Supervisor 책임과 driver primitive 책임을 섞지 않도록 주의 |
| ErrorWriteExpectedButNoErrorCodeBinding | Policy Validation | error policy + bindings | error write expected가 true인데 errorCode binding 없음 | Error | error write 정책과 binding이 불일치함을 알림 | Yes | binding validation과 policy validation의 교차 rule |
| CompleteRequestWithoutStartContext | Policy Validation | `flow.kind`, complete path | complete request 처리에 필요한 start context 또는 lot context 정책 없음 | ReviewRequired | 완공 요청 처리에 필요한 context 정책을 확인해야 함을 알림 | Yes | WorkCompleteOnly 허용 여부와 별도 검토 |
| PolicyStatusDraftBlocksExecution | Policy Validation | `metadata.policyStatus` | policy status가 Draft 또는 review 미승인 상태 | ReviewRequired | draft 정책 상태에서는 실행 후보로 승격할 수 없음을 알림 | Yes | metadata는 runtime 필수값이 아니지만 review gate로 사용 가능 |

## 8. Excluded Scope

이번 AH-RUNTIME-52에서는 다음을 하지 않는다.

- production code 수정
- test code 수정
- csproj 수정
- 실제 FLOW.JSON 파일 생성
- JSON schema 파일 생성
- FLOW.JSON parser 구현
- C# model 구현
- validator 구현
- validator skeleton 구현
- executor 구현
- XGT Adapter 구현
- DB Query 구현
- Payload Builder 구현 또는 이식
- ACK/Error Writer 구현
- XgtDriverCore, FakePlc, XgtChannelRunner 참조 추가
- ChannelPollingResult / PollingCycleCoordinator / RuntimeSnapshot path 변경
- ContextPublisher 자동 publish 재도입

## 9. Next Candidate

AH-RUNTIME-51의 다음 방향을 유지한다.

AH-RUNTIME-52 이후 후보는 다음 중 하나다.

1. Validator Skeleton Boundary Review
   - rule matrix를 실제 skeleton으로 옮기기 전에 validator가 Runtime core에 속해야 하는지, 별도 authoring/definition validation boundary에 속해야 하는지 검토한다.

2. Validator Model Skeleton
   - parser나 executor 없이 validation result model, severity enum, rule id enum 같은 최소 model 후보만 검토한다.

다음 단계는 validator 구현이 아니다. parser, JSON schema, FLOW.JSON 파일, executor, XGT Adapter, DB Query, Payload Builder 구현으로 바로 넘어가지 않는다.

## 10. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-52 closeout 문서를 생성했다.
- Structural / Binding / Policy Validation rule을 분리했다.
- 각 rule의 severity와 execution blocking 여부를 명시했다.
- Runtime core vendor-neutral boundary를 유지했다.
- Runtime core가 FLOW.JSON parser, JSON schema, XGT execution, DB query, payload layout, PLC address, SQL policy를 소유하지 않는다는 계약을 유지했다.
- `ChannelPollingResult` / `PollingCycleCoordinator` / `RuntimeSnapshot` path와 Pilot Business Flow path를 섞지 않았다.
- production code, test code, csproj, parser, JSON schema, C# model, executor, XGT Adapter, DB Query, Payload Builder는 수정하지 않았다.

Validation evidence:

- `git diff --check`: PASS. whitespace error 없음. 단, `docs/context/COGNITIVE_SYNC_CHECK.md`에 대해 Git의 LF -> CRLF working copy 경고가 출력됨.
- `git status --short`: `M docs/context/COGNITIVE_SYNC_CHECK.md`, `?? docs/harness/AH-RUNTIME-52.md`.
- 변경 파일 scope 확인: 문서 파일 2개로 제한됨. production code, test code, csproj 변경 없음.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
