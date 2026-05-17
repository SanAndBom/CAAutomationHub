# AH-RUNTIME-53 Closeout - Validator Skeleton Boundary Review

## 1. Summary

AH-RUNTIME-53은 AH-RUNTIME-52의 Validation Rule Matrix를 기반으로, 실제 validator skeleton 구현 전에 Validation 모델과 책임 경계를 검토한 Boundary Review다.

이번 작업의 핵심 결론은 validator가 raw JSON parser나 JSON schema validator가 아니라, parsed neutral flow model 또는 draft/executable candidate를 입력으로 받는 runtime-neutral validation boundary여야 한다는 점이다.

Validation 모델은 `ValidationIssue`, `ValidationResult`, `ValidationSeverity`, `ValidationCategory` 중심의 shared contract 후보로 보고, `RuleId`는 enum보다 string code를 우선 검토한다. `BlocksExecution`은 severity에서 자동 유도하지 않고 명시 필드로 두는 방향이 안전하다.

Runtime core는 계속 vendor-neutral이어야 하며 XGT-specific rule, DB-specific rule, payload layout implementation, PLC address mapping implementation은 adapter 또는 extension rule provider boundary로 분리한다. 이번 작업은 문서 작업만 수행했고 production code, test code, csproj, parser, schema, validator skeleton 구현은 하지 않는다.

## 2. Goal

AH-RUNTIME-53의 목표는 validator skeleton을 만들기 전에 다음 책임 경계를 문서로 확정하는 것이다.

- `ValidationResult` / `ValidationIssue`를 어디에 둘 것인가
- `ValidationRuleId`를 enum으로 둘 것인가 string code로 둘 것인가
- severity 모델은 어떻게 둘 것인가
- `BlocksExecution`은 rule definition의 속성인지 validation result의 속성인지
- validator가 입력으로 받을 대상은 무엇인지
- Runtime core가 알아도 되는 정보와 몰라야 하는 정보는 무엇인지
- XGT-specific / DB-specific validation rule을 어디에 둘 것인지

이번 단계는 Boundary Review이며 validator 구현 단계가 아니다.

## 3. Context

AH-RUNTIME-51은 Template / Binding Validation Rule을 Structural Validation, Binding Validation, Policy Validation으로 분류했다.

AH-RUNTIME-52는 이 분류를 Validation Rule Matrix로 문서화하고, 각 rule의 severity와 execution blocking 여부를 정리했다.

AH-RUNTIME-53은 AH-RUNTIME-52의 matrix를 구현으로 옮기기 전 단계다. validator skeleton을 만들기 전에 validation result 모델, rule id 형태, severity 정책, input boundary, Runtime-neutral allowed/forbidden knowledge를 먼저 검토한다.

중요한 전제:

- FLOW.JSON은 XGT command list가 아니라 PLC별 Business Flow Definition이다.
- Runtime core는 FLOW.JSON parser, JSON schema, XGT execution, DB query, payload layout, PLC address, SQL policy를 소유하지 않는다.
- `ChannelPollingResult` / `PollingCycleCoordinator` / `RuntimeSnapshot` path와 Pilot Business Flow path는 섞지 않는다.

## 4. Boundary

Runtime core는 계속 vendor-neutral이어야 한다.

Runtime core는 다음을 직접 소유하지 않는다.

- XGT-specific validation rule
- FakePlc scenario-specific validation rule
- DB-specific validation rule
- SQL text validation
- payload layout implementation validation
- PLC address mapping implementation validation
- FLOW.JSON raw parser
- JSON schema validator
- executor implementation
- XGT Adapter implementation
- DB Query implementation
- Payload Builder implementation
- ACK/Error Writer implementation

이번 작업은 production code와 test code를 수정하지 않는다. validator skeleton 구현은 다음 단계로 미룬다.

## 5. Validation Responsibility Split

### Authoring-time validation

Authoring-time validation은 WPF editor, external authoring tool, CLI tool, 또는 별도 schema/parser layer에서 사용되는 검증이다.

주요 책임:

- raw JSON parse result를 neutral draft model로 변환한 뒤 검증한다.
- Structural / Binding / Policy rule을 가능한 한 폭넓게 보여준다.
- `Warning`, `Info`, `ReviewRequired`를 authoring feedback으로 제공한다.
- `metadata.policyStatus`, review trace, target path, evidence를 보존한다.

금지:

- Runtime core 내부에서 raw JSON parsing을 수행하지 않는다.
- XGT address, SQL text, payload offset을 Runtime core의 지식으로 만들지 않는다.

### Runtime preflight validation

Runtime preflight validation은 이미 parsed된 neutral executable candidate가 runtime execution boundary로 들어오기 전에 수행하는 검증이다.

주요 책임:

- 실행 후보가 Structural Error를 포함하지 않는지 확인한다.
- runtime-neutral binding reference가 끊기지 않았는지 확인한다.
- `ReviewRequired` 또는 `BlocksExecution=true` issue가 남아 있으면 execution candidate 승격을 막는다.
- adapter-specific detail이 필요한 rule은 extension result를 받아 합산한다.

허용:

- Runtime core가 neutral model의 step id, transition, signalRef, policyRef 같은 추상 reference consistency를 확인하는 것은 가능하다.

금지:

- Runtime core가 raw JSON, XGT address, DB query, payload layout implementation을 직접 해석하지 않는다.

### Execution-time guard

Execution-time guard는 validator의 대체물이 아니다. 실행 중 발생하는 불일치, adapter failure, timeout, recovery event를 안전하게 차단하거나 실패로 전환하는 방어층이다.

주요 책임:

- preflight에서 보장한 contract가 실행 중 깨졌을 때 fail fast 또는 safe failure로 전환한다.
- missing handler, unknown adapter result, timeout, driver failure를 execution failure로 다룬다.
- Supervisor / Driver / Adapter 책임을 섞지 않고 각 boundary의 failure primitive를 보존한다.

금지:

- execution-time guard가 authoring-time missing policy를 임의 기본값으로 보정하지 않는다.
- execution-time guard가 validator를 우회해 FLOW.JSON 오류를 실행 중에 해석하려 하지 않는다.

## 6. Candidate Model Placement

### Contracts

장점:

- WPF authoring, Runtime preflight, adapter extension, external validation tool이 동일한 result 모델을 공유할 수 있다.
- `ValidationIssue` / `ValidationResult` / `ValidationSeverity` / `ValidationCategory`처럼 vendor-neutral한 타입에 적합하다.
- Runtime project가 이미 Contracts를 참조한다는 기존 boundary와 충돌하지 않는다.

위험:

- Contracts에 너무 많은 flow definition detail이 들어가면 Contracts가 schema/parser 모델로 비대해질 수 있다.
- XGT-specific / DB-specific 속성이 섞이면 vendor-neutral contract가 깨진다.

판단:

- `ValidationIssue`, `ValidationResult`, `ValidationSeverity`, `ValidationCategory`는 Contracts 또는 Runtime-neutral shared layer 후보가 적절하다.
- 단, 실제 FLOW.JSON AST, adapter-specific binding shape, SQL/payload detail은 Contracts에 넣지 않는다.

### Runtime

장점:

- Runtime preflight guard와 가깝다.
- execution candidate 승격 여부 판단을 구현하기 쉽다.

위험:

- Runtime core가 validation model을 소유하면 parser, binding, policy detail까지 흡수하려는 drift가 생길 수 있다.
- WPF authoring이나 external schema/parser layer와 result model이 중복될 수 있다.

판단:

- Runtime에는 validator execution orchestration 또는 preflight consumer가 있을 수 있지만, shared validation result model의 1차 위치로는 신중해야 한다.
- Runtime core에 두더라도 runtime-neutral model만 허용하고 raw JSON / XGT / DB / payload detail은 금지한다.

### WPF Authoring

장점:

- 사용자 편집 피드백, target path, field-level message를 표현하기 쉽다.
- Draft / ReviewRequired 상태와 UI 표시를 자연스럽게 연결할 수 있다.

위험:

- ValidationResult가 WPF 전용 모델이 되면 Runtime preflight와 external tooling에서 재사용하기 어렵다.
- UI가 Runtime 내부 책임을 침범할 가능성이 있다.

판단:

- WPF는 shared validation result를 표시하는 consumer가 적절하다.
- WPF 전용 view model은 둘 수 있으나 core validation model을 WPF에 가두지 않는다.

### External Schema/Parser Layer

장점:

- raw JSON parsing, JSON schema validation, authoring format detail을 Runtime core 밖에 유지할 수 있다.
- vendor-specific extension provider와 조합하기 쉽다.

위험:

- Runtime preflight와 다른 result model을 쓰면 validation evidence가 분산된다.
- parser layer가 executor 책임까지 흡수할 수 있다.

판단:

- raw JSON과 JSON schema는 external schema/parser layer가 담당한다.
- 이 layer는 parsed neutral draft model과 shared validation result를 Runtime boundary로 넘기는 역할이 적절하다.

## 7. Validation Model Decision Candidates

### ValidationResult

후보 의미:

- 단일 flow draft 또는 executable candidate에 대한 validation summary.
- issue 목록, aggregate status, blocking 여부 계산 결과를 가진다.

권장 방향:

- Contracts 또는 Runtime-neutral shared layer 후보.
- Runtime-only 타입으로 시작하면 WPF authoring / external parser와 중복될 위험이 있다.
- `IsValid`만 두지 말고 `Issues`, `HasBlockingIssues`, `MaxSeverity` 같은 aggregate 개념을 분리해 검토한다.

Q1 판단:

- `ValidationResult`는 Runtime보다 Contracts 또는 Runtime-neutral shared layer에 두는 방향이 우선이다.
- 이유는 WPF authoring, Runtime preflight, adapter extension rule provider가 같은 result shape를 공유해야 하기 때문이다.

### ValidationIssue

후보 의미:

- AH-RUNTIME-52의 rule 하나가 발생한 결과.
- `RuleId`, `Category`, `Severity`, `TargetPath`, `Message`, `BlocksExecution`, `Evidence`를 포함할 수 있다.

권장 방향:

- `ValidationResult`와 같은 shared layer 후보.
- issue는 rule definition 자체가 아니라 특정 input에서 발생한 evidence다.

### ValidationSeverity

후보 값:

- `Error`
- `Warning`
- `Info`
- `ReviewRequired`

Q3 판단:

- AH-RUNTIME-52 기준으로 초기 skeleton에는 네 값이면 충분하다.
- AH-RUNTIME-51의 `BLOCK_REVIEW`는 AH-RUNTIME-52의 `ReviewRequired`로 정리된 것으로 본다.
- 향후 `Fatal`, `Deprecated`, `Advisory` 같은 값을 추가하기 전에 실제 rule matrix evidence가 필요하다.

### ValidationRuleId

후보:

- enum
- string code

Q2 판단:

- 초기에는 enum보다 string code를 우선 검토한다.
- 이유는 adapter extension rule, DB-specific rule, vendor-specific rule이 Runtime core 밖에서 추가될 수 있기 때문이다.
- string code 예시는 `Structural.MissingSchemaVersion`, `Binding.MissingSignalBinding`, `Policy.PolicyStatusDraftBlocksExecution`, `Xgt.AddressFormatInvalid`, `Db.QueryPolicyNotFound` 같은 namespace-style code가 적절하다.
- enum은 compile-time 안정성이 있지만 extension rule provider와 versioned rule compatibility를 어렵게 만들 수 있다.

### ValidationCategory

후보 값:

- `Structural`
- `Binding`
- `Policy`
- `AdapterSpecific`
- `External`

권장 방향:

- AH-RUNTIME-52의 기본 세 범주를 유지한다.
- XGT / DB extension rule을 수용하려면 `AdapterSpecific` 또는 `External` 범주 후보를 열어둔다.
- Runtime core가 adapter-specific rule 내용을 알 필요는 없다.

### BlocksExecution

후보:

- severity에서 자동 유도
- rule definition의 고정 속성
- validation issue의 명시 필드

Q4 판단:

- `BlocksExecution`은 severity에서 자동 유도하지 말고 명시 필드로 둔다.
- 이유는 `ReviewRequired` 대부분은 blocking이지만 authoring mode에서는 non-blocking feedback으로 표시될 수 있고, `Warning` 중에서도 deployment gate에서는 blocking으로 승격될 수 있기 때문이다.
- rule definition에는 default blocking policy를 둘 수 있지만, 최종 `ValidationIssue.BlocksExecution`은 validation context와 mode를 반영한 결과여야 한다.

### Evidence / TargetPath

후보 의미:

- `TargetPath`: issue가 발생한 neutral model path 또는 authoring field path.
- `Evidence`: 관련 값, 참조한 rule matrix, source anchor, policy status 등.

권장 방향:

- `TargetPath`는 raw JSON path가 아니라 neutral model path를 우선한다.
- authoring tool은 raw JSON location을 별도 mapping으로 보존할 수 있다.
- `Evidence`는 string dictionary나 작은 key-value bag 후보로 두되, Runtime core가 vendor-specific value를 해석하지 않도록 한다.

## 8. Validator Input Boundary

### raw JSON input 여부

Q5 판단:

- Validator는 raw JSON을 직접 읽지 않는 방향이 우선이다.
- raw JSON parsing과 JSON schema validation은 external schema/parser layer 책임이다.
- Runtime core validator가 raw JSON을 읽기 시작하면 parser ownership과 schema ownership이 Runtime core로 이동할 위험이 있다.

### parsed neutral flow model 여부

권장:

- validator의 기본 입력 후보는 parsed neutral flow model이다.
- 이 모델은 step id, action kind, transition, signalRef, policyRef, bindingRef, flow kind 같은 vendor-neutral 요소만 포함해야 한다.
- XGT address, SQL text, payload byte/word offset implementation은 포함하지 않는다.

### WPF editor draft model 여부

판단:

- WPF editor draft model은 authoring-time validation 입력 후보가 될 수 있다.
- 단 shared validator가 WPF type을 직접 참조하면 안 된다.
- WPF draft model은 neutral draft model로 변환한 뒤 validation boundary로 넘기는 구조가 적절하다.

### runtime executable model 여부

판단:

- runtime executable model은 Runtime preflight validation 입력 후보가 될 수 있다.
- 이 모델은 이미 parser와 authoring format을 통과한 실행 후보여야 한다.
- executable model이 adapter-specific implementation detail을 포함한다면 Runtime core validator 입력으로 부적절하다.

## 9. Runtime-Neutral Allowed Knowledge

Runtime-neutral validator 또는 Runtime preflight가 알아도 되는 정보:

- schemaVersion 값의 존재와 지원 범위
- flow id / flow kind / initial state
- step id와 duplicate 여부
- step action kind의 neutral identifier
- transition source/target consistency
- required binding reference의 존재 여부
- binding reference type의 neutral category
- policy reference의 존재 여부
- severity / category / rule code
- `BlocksExecution` aggregate status
- `metadata.policyStatus`가 Draft인지 Approved인지 같은 review gate signal
- target path와 neutral evidence key

조건부 허용:

- `Binding Validation`은 Runtime core가 수행해도 되지만 runtime-neutral reference consistency까지만 허용한다.
- `Policy Validation`은 Runtime core가 수행해도 되지만 runtime-neutral policy presence / draft status / blocking status까지만 허용한다.

Q6 판단:

- Binding Validation은 Runtime core가 수행할 수 있다. 단 signalRef, payloadLayoutRef, dbQueryPolicyRef 같은 추상 reference 존재 여부와 type category consistency까지만이다.
- PLC address mapping implementation, XGT device syntax, payload offset 계산은 Runtime core 밖이다.

Q7 판단:

- Policy Validation은 Runtime core가 수행할 수 있다. 단 both request ON policy 존재 여부, timeout policy reference 존재 여부, recovery policy status 같은 neutral consistency까지만이다.
- 현장 정책 의미 결정, DB fallback, adapter recovery detail은 Runtime core 밖이다.

## 10. Runtime-Neutral Forbidden Knowledge

Runtime core validator가 몰라야 하는 정보:

- XGT device address syntax
- XGT command frame
- XgtDriverCore API detail
- FakePlc scenario hook
- DB connection string
- SQL text
- DB query execution result shape
- payload byte/word offset implementation
- LOTID extraction implementation
- PLC memory map implementation
- ACK/Error Writer implementation detail
- adapter-specific retry primitive
- vendor-specific timeout primitive

Q8 판단:

- XGT-specific rule은 Runtime core 밖의 XGT adapter validation provider 또는 vendor extension rule provider에 둔다.
- 예: XGT address format, device range, command compatibility, frame-specific restriction.

Q9 판단:

- DB-specific rule은 Runtime core 밖의 DB query policy validation provider 또는 external integration validation layer에 둔다.
- 예: query policy key existence, SQL text validation, parameter binding, database schema compatibility.

## 11. Recommended Skeleton Direction

권장 방향:

1. Validation result model은 Contracts 또는 Runtime-neutral shared layer 후보로 둔다.
2. `ValidationIssue` / `ValidationResult` / `ValidationSeverity` / `ValidationCategory`를 최소 모델 후보로 본다.
3. `RuleId`는 enum보다 string code를 우선 검토한다.
4. `BlocksExecution`은 severity에서 자동 유도하지 않고 issue의 명시 필드로 둔다.
5. validator는 raw JSON을 직접 읽지 않고 parsed neutral draft/executable model을 받는다.
6. Runtime core는 structural consistency와 runtime-neutral binding/policy consistency까지만 담당한다.
7. XGT-specific / DB-specific validation은 adapter 또는 extension rule provider로 분리한다.
8. WPF는 validation model owner가 아니라 consumer / display adapter로 둔다.

Q10 판단:

- 다음 단계에서 만들 수 있는 최소 skeleton은 model-only 또는 boundary-only skeleton이다.
- 최소 후보:
  - `ValidationSeverity`
  - `ValidationCategory`
  - `ValidationIssue`
  - `ValidationResult`
  - optional `IFlowDefinitionValidator` 또는 `IValidationRuleProvider` interface
- 단 다음 단계에서도 raw JSON parser, JSON schema validator, C# FLOW.JSON model, executor, XGT Adapter, DB Query, Payload Builder 구현은 제외해야 한다.

권장 next step은 `Validator Model Skeleton`보다 먼저 `Validator Skeleton Scope Confirmation` 또는 `Validator Model Skeleton` 중 하나를 선택하는 것이다. 구현을 시작한다면 모델과 interface만 만들고, rule implementation은 AH-RUNTIME-52 matrix의 일부 structural consistency부터 작게 시작해야 한다.

## 12. Excluded Scope

이번 AH-RUNTIME-53에서는 다음을 하지 않는다.

- production code 수정
- test code 수정
- csproj 수정
- C# model 생성
- validator skeleton 구현
- validator rule 구현
- FLOW.JSON parser 구현
- JSON schema 구현
- executor 구현
- XGT Adapter 구현
- DB Query 구현
- Payload Builder 구현
- ACK/Error Writer 구현
- 실제 FLOW.JSON 파일 생성
- XGT-specific rule 구현
- DB-specific rule 구현
- payload layout implementation 검증 구현
- ContextPublisher 자동 publish 재도입

## 13. Next Candidate

다음 후보:

1. Validator Model Skeleton
   - Contracts 또는 Runtime-neutral shared layer에 최소 validation result model 후보를 만든다.
   - `ValidationSeverity`, `ValidationCategory`, `ValidationIssue`, `ValidationResult`만 우선 검토한다.
   - string `RuleId`와 explicit `BlocksExecution`을 유지한다.

2. Validator Skeleton Scope Confirmation
   - 실제 코드를 만들기 전에 어느 project에 둘지, Contracts 변경이 필요한지, Runtime project reference boundary가 유지되는지 한 번 더 확인한다.

3. Structural Validator Skeleton
   - parser 없이 neutral in-memory draft model을 입력으로 받는 최소 structural consistency skeleton만 검토한다.
   - 이 후보는 model placement가 확정된 뒤에만 진행한다.

비권장 next step:

- raw JSON parser 구현
- JSON schema 파일 생성
- executor 구현
- XGT-specific validation 구현
- DB-specific validation 구현
- payload layout validator 구현

## 14. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-53 closeout 문서를 생성했다.
- validator skeleton 구현 전에 Validation 모델과 책임 경계를 문서화했다.
- `ValidationResult` placement 후보를 Contracts / Runtime / WPF Authoring / External Schema/Parser Layer로 비교했다.
- `RuleId`는 enum보다 string code를 우선 검토하는 결론을 기록했다.
- `Severity`는 Error / Warning / Info / ReviewRequired로 초기 충분하다고 판단했다.
- `BlocksExecution`은 severity와 분리된 명시 필드가 안전하다고 판단했다.
- validator는 raw JSON을 직접 읽지 않고 parsed neutral draft/executable model을 받는 방향을 기록했다.
- Binding / Policy Validation은 Runtime core가 수행할 수 있더라도 runtime-neutral consistency까지만 허용한다고 정리했다.
- XGT-specific rule과 DB-specific rule은 Runtime core 밖 adapter 또는 extension rule provider 후보로 분리했다.
- Runtime core vendor-neutral boundary를 유지했다.
- production code, test code, csproj, parser, schema, validator, executor, XGT Adapter, DB Query, Payload Builder는 수정하지 않았다.

Validation evidence:

- `git diff --check`: PASS. whitespace error 없음.
- `git status --short`: `?? docs/harness/AH-RUNTIME-53.md`.
- 변경 파일 scope 확인: AH-RUNTIME-53 문서 1개로 제한됨. production code, test code, csproj 변경 없음.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
