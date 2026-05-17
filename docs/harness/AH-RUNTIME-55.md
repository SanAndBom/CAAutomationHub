# AH-RUNTIME-55 Closeout - Neutral Validation Input Model Boundary Review

## 1. Summary

AH-RUNTIME-55는 AH-RUNTIME-54에서 `ValidationResult` / `ValidationIssue` / `ValidationSeverity` / `ValidationCategory` result model skeleton이 추가된 뒤, validator가 무엇을 입력으로 받아야 하는지 검토한 Boundary Review다.

핵심 결론은 validator의 직접 입력이 raw `FLOW.JSON` 문자열이나 JSON parser 결과물 자체가 아니라, parsed neutral draft model 또는 parsed neutral executable candidate여야 한다는 점이다.

Authoring-time validation과 Runtime preflight validation은 목적이 다르다. 장기적으로는 Draft와 Executable Candidate를 구분하는 방향이 안전하다. raw JSON parsing과 JSON schema validation은 external parser / authoring layer 책임으로 두고, Runtime core는 raw JSON parser, JSON schema validator, XGT execution, DB query, payload layout implementation을 소유하지 않는다.

Contracts에는 `ValidationResult` 계열처럼 runtime-neutral shared result model은 둘 수 있다. 다만 `FlowDefinition` / `FlowStep` / `Binding` / `Policy` 같은 input model까지 곧바로 Contracts에 넣는 것은 신중해야 한다.

이번 작업은 Boundary Review 결과를 historical record로 남기는 문서 작성 단계다. 코드, 테스트, FLOW.JSON, JSON schema, parser, validator, validator interface, FlowDefinition / FlowStep / Binding / Policy C# model, Flow Executor, XGT Adapter, DB Query, Payload Builder, csproj, project reference, package reference, commit은 수행하지 않았다.

## 2. Goal

AH-RUNTIME-55의 목표는 Neutral Validation Input Model Boundary Review다.

핵심 질문은 다음이었다.

```text
ValidationResult 모델은 생겼지만,
validator가 무엇을 입력으로 받을지 아직 정하지 않았다.
```

이번 단계에서는 validator input boundary 후보와 권장안을 정리했다.

검토 질문:

- validator가 raw `FLOW.JSON` 문자열을 직접 받아야 하는가?
- validator가 JSON parser 결과를 받아야 하는가?
- validator가 parsed neutral draft model을 받아야 하는가?
- validator가 executable candidate를 받아야 하는가?
- authoring-time validation input과 runtime preflight validation input은 같은가?
- Contracts에 `ValidationResult`는 있어도 되지만, FlowDefinition / FlowStep / Binding / Policy input model까지 넣어도 되는가?
- Runtime core가 알아도 되는 neutral input 필드는 무엇인가?
- Runtime core가 몰라야 하는 adapter-specific / DB-specific / payload-specific 필드는 무엇인가?
- WPF authoring, external parser, Runtime preflight, adapter extension rule provider가 같은 input model을 공유해야 하는가?
- 다음 skeleton 단계에서 어떤 model 또는 interface까지 추가해도 안전한가?

## 3. Context

AH-RUNTIME-54에서 추가된 모델은 validation result shape다.

추가된 모델:

- `ValidationSeverity`
- `ValidationCategory`
- `ValidationIssue`
- `ValidationResult`

중요 결정:

- `RuleId`는 enum이 아니라 string code다.
- `BlocksExecution`은 `Severity`에서 자동 유도하지 않고 명시 필드다.
- `ValidationResult.IsValid`은 issue가 없을 때만 true다.
- `ValidationResult.BlocksExecution`은 `BlocksExecution=true` issue가 하나라도 있을 때 true다.
- Contracts에 runtime-neutral validation result model만 추가했다.
- parser / schema / validator / executor / XGT / DB / WPF / FakePlc 연결은 추가하지 않았다.

이번 AH-RUNTIME-55에서 검토한 대상은 result model이 아니라 input model이다. 즉 validator가 어떤 neutral candidate를 받아야 하는지 검토했다.

유지해야 하는 경계:

- CAAutomationHub.Runtime core는 vendor-neutral이어야 한다.
- Runtime core는 raw `FLOW.JSON` parser를 소유하지 않는다.
- Runtime core는 JSON schema validator를 소유하지 않는다.
- Runtime core는 XGT-specific flow execution을 소유하지 않는다.
- Runtime core는 DB query를 소유하지 않는다.
- Runtime core는 payload layout implementation을 소유하지 않는다.
- Runtime core는 PLC별 address / SQL policy를 알지 않는다.
- Contracts에 XGT address / SQL text / payload byte offset implementation을 넣지 않는다.
- ChannelPollingTarget에 XGT address / datatype / count를 넣지 않는다.
- ChannelPollingResult에 LOTID / DB result / ACK policy 같은 business transaction detail을 넣지 않는다.
- Pilot business flow와 Runtime polling state path는 분리한다.
- WorkStartPilotService.RunOnceAsync(...)를 Runtime core에 복사하지 않는다.
- XgtDriverCore / FakePlc / XgtChannelRunner 직접 참조를 추가하지 않는다.
- ContextPublisher 자동 publish는 재도입하지 않는다.

## 4. 확인한 현재 validation model / closeout 근거

확인한 closeout:

- AH-RUNTIME-50
  - `schemaVersion` / `flow` / `bindings` / `metadata`는 문서 후보이며 parser / schema / model 구현이 아니다.
- AH-RUNTIME-51
  - Structural / Binding / Policy validation rule을 검토했다.
- AH-RUNTIME-52
  - `RuleId` / Severity / Blocks Execution matrix를 문서화했다.
- AH-RUNTIME-53
  - validator는 raw JSON parser가 아니라 parsed neutral model / draft candidate를 입력으로 받는 방향을 기록했다.
- AH-RUNTIME-54
  - `ValidationResult`, `ValidationIssue`, `ValidationSeverity`, `ValidationCategory`만 Contracts에 추가했다.

현재 validation code 위치:

- `src/CAAutomationHub.Contracts/Runtime/Validation`

현재 validation test 위치:

- `tests/CAAutomationHub.Runtime.Tests/Validation`

현재 validation model은 result shape만 제공한다. validator input model, validator interface, rule implementation, parser, schema, executor는 아직 없다.

## 5. raw JSON input 후보 검토

판정:

- 비권장.

이유:

- validator가 raw JSON 문자열 또는 JSON file path를 직접 받으면 parser 책임이 validator / Runtime 쪽으로 이동할 위험이 있다.
- JSON schema validation과 business rule validation이 섞일 수 있다.
- Runtime core가 raw `FLOW.JSON` format detail을 알게 되어 vendor-neutral boundary를 침범할 가능성이 크다.
- raw JSON location, source span, syntax error 같은 authoring detail은 Runtime preflight의 직접 책임이 아니다.

판단:

- raw JSON parsing은 external parser / authoring layer 책임으로 둔다.
- JSON schema validation도 Runtime core 밖의 schema/parser layer 책임으로 둔다.
- validator core의 직접 입력은 raw JSON이 아니라 parser 또는 authoring tool이 만든 neutral candidate여야 한다.

## 6. parsed neutral draft model 후보 검토

판정:

- 권장 후보.

이유:

- raw JSON parser와 validator를 분리할 수 있다.
- authoring-time validation에 적합하다.
- metadata / evidence / policyStatus를 함께 유지할 수 있다.
- runtime-neutral field만 유지하면 Contracts / Runtime boundary와 충돌하지 않는다.
- WPF authoring, external tool, CLI validation이 같은 neutral draft shape를 공유할 수 있다.

허용 가능한 neutral 정보:

- flow identity
- flow kind
- initial state
- step id
- action kind
- transition reference
- signal reference
- policy reference
- binding reference
- policyStatus
- optional metadata / evidence

금지되는 정보:

- XGT address syntax
- SQL text
- connection string
- payload byte offset implementation
- raw JSON source location
- XgtDriverCore type
- FakePlc scenario id
- RuntimeSnapshot
- ChannelPollingResult

주의:

- draft model을 어디에 둘지 결정해야 한다.
- Contracts에 넣을 경우 FLOW.JSON schema model 저장소로 비대해지지 않도록 최소 runtime-neutral candidate로 제한해야 한다.

## 7. runtime executable candidate 후보 검토

판정:

- Runtime preflight에는 적절하다.

이유:

- parser / authoring validation을 이미 지난 실행 후보를 검증하기 좋다.
- Runtime execution boundary로 승격하기 전에 blocking issue가 남아 있는지 확인할 수 있다.
- Runtime preflight는 raw authoring feedback보다 execution gate에 집중할 수 있다.

주의:

- authoring feedback에는 정보가 부족할 수 있다.
- executable candidate에 adapter-specific detail이 들어가면 Runtime boundary가 깨질 수 있다.
- executable candidate도 neutral reference만 포함해야 한다.
- XGT address, SQL text, payload offset implementation, driver-specific type은 포함하지 않는다.

판단:

- Runtime preflight input은 parsed neutral executable candidate 후보가 적절하다.
- 이 candidate는 parser / authoring / schema validation을 통과한 모델이어야 한다.
- adapter-specific validation은 extension provider가 별도 external context를 보고 수행하고, 결과만 `ValidationIssue`로 합산하는 구조가 안전하다.

## 8. authoring-time vs runtime preflight 구분

Authoring-time validation과 Runtime preflight validation은 목적이 다르다.

Authoring-time validation:

- WPF editor, external authoring tool, CLI tool, parser layer에서 사용된다.
- raw `FLOW.JSON`을 neutral draft model로 변환한 뒤 검증한다.
- Structural / Binding / Policy issue를 가능한 한 풍부하게 보여준다.
- `Warning`, `Info`, `ReviewRequired`를 authoring feedback으로 제공한다.
- metadata / evidence / policyStatus / notes / review trace를 보존할 수 있다.

Runtime preflight validation:

- 이미 parsed된 neutral executable candidate가 runtime execution boundary로 들어오기 전에 사용된다.
- 실행 후보가 Structural Error를 포함하지 않는지 확인한다.
- runtime-neutral binding reference가 끊기지 않았는지 확인한다.
- `ReviewRequired` 또는 `BlocksExecution=true` issue가 남아 있으면 execution candidate 승격을 막는다.
- adapter-specific detail이 필요한 rule은 extension result를 받아 합산한다.

판단:

- 두 입력은 같은 뿌리를 공유할 수 있지만 같은 모델로 강제로 합치면 metadata와 runtime execution 필드가 섞일 위험이 있다.
- 장기적으로는 Draft와 Executable Candidate를 구분하는 방향이 안전하다.

## 9. Contracts에 둘 수 있는 것과 없는 것

Contracts에 둘 수 있는 것:

- `ValidationResult`
- `ValidationIssue`
- `ValidationSeverity`
- `ValidationCategory`
- runtime-neutral minimal candidate 후보

Contracts에 바로 넣기 신중해야 하는 것:

- `FlowDefinition`
- `FlowStep`
- `Binding`
- `Policy`

금지 대상:

- XGT address syntax
- SQL text
- connection string
- payload byte offset implementation
- raw JSON location
- XgtDriverCore type
- FakePlc scenario id
- RuntimeSnapshot
- ChannelPollingResult

판단:

- `ValidationResult` 계열은 result boundary contract에 가깝기 때문에 Contracts에 둘 수 있다.
- input model은 result model보다 훨씬 더 schema / parser / executor 방향으로 drift할 위험이 크다.
- Contracts에 input model을 넣는다면 runtime-neutral minimal candidate만 허용해야 한다.
- Contracts가 FLOW.JSON schema model 저장소로 비대해지면 안 된다.

## 10. input model 최소 후보

아직 구현하지 않은 후보 개념:

- `FlowValidationDraft`
- `FlowExecutionCandidate`
- `FlowStepCandidate`
- `FlowBindingCandidate`
- `FlowPolicyCandidate`

허용 가능한 neutral 필드 후보:

- `flowId`
- `flowKind`
- `initialState`
- `stepIds`
- `actionKinds`
- `transitionRefs`
- `signalRefs`
- `policyRefs`
- `bindingRefs`
- `policyStatus`
- optional metadata / evidence

금지 필드 후보:

- XGT address syntax
- SQL text
- connection string
- payload byte offset implementation
- raw JSON location
- XgtDriverCore type
- FakePlc scenario id
- RuntimeSnapshot
- ChannelPollingResult

주의:

- 다음 skeleton에서 위 후보를 한 번에 모두 만들 필요는 없다.
- model placement를 먼저 확정하고, 가장 작은 neutral candidate부터 시작하는 것이 안전하다.

## 11. extension rule provider와 input model 관계

판단:

- Extension provider는 shared `ValidationIssue`로 결과를 합산하는 구조가 맞다.
- 입력은 공통 neutral candidate + provider별 external context가 좋다.
- XGT provider는 neutral `signalRef`와 별도 adapter binding registry를 보고 address format을 검증할 수 있다.
- DB provider는 `dbQueryPolicyRef`로 외부 query policy registry를 검증할 수 있다.
- Payload provider는 `payloadLayoutRef`로 외부 layout registry를 검증할 수 있다.
- Runtime core가 XGT / DB / payload detail을 직접 해석하면 안 된다.

구조적 의미:

- Runtime-neutral validator는 common candidate의 structural / binding reference / policy presence consistency까지만 다룬다.
- Extension provider는 vendor-specific 또는 integration-specific detail을 Runtime core 밖에서 검증한다.
- extension 결과는 `ValidationIssue`로 합산할 수 있다.
- `ValidationCategory.Extension`은 extension provider 결과를 수용하는 데 사용할 수 있다.

## 12. 후보 A / B / C / D / E 검토

### 후보 A: raw JSON input

판정:

- 비권장.

이유:

- parser 책임이 validator / Runtime 쪽으로 이동할 위험이 있다.
- JSON schema와 business rule validation이 섞인다.
- Runtime core vendor-neutral boundary를 침범할 가능성이 크다.

### 후보 B: parsed neutral draft model

판정:

- 권장 후보.

이유:

- raw JSON parser와 validator를 분리할 수 있다.
- authoring-time validation에 적합하다.
- metadata / evidence / policyStatus를 함께 유지할 수 있다.
- runtime-neutral field만 유지하면 Contracts / Runtime boundary와 충돌하지 않는다.

### 후보 C: runtime executable candidate

판정:

- runtime preflight에는 적절하다.

이유:

- parser / authoring validation을 이미 지난 실행 후보를 검증하기 좋다.

주의:

- authoring feedback에는 정보가 부족할 수 있다.
- adapter-specific detail이 들어가면 Runtime boundary가 깨질 수 있다.

### 후보 D: Draft와 Executable Candidate 분리

판정:

- 장기 권장안.

구조 후보:

- `FlowValidationDraft`
  - authoring-time
  - metadata / evidence / policyStatus 포함
  - review feedback 풍부
- `FlowExecutionCandidate`
  - runtime preflight
  - 실행에 필요한 neutral reference만 포함
  - parser / authoring을 통과한 후보

장점:

- authoring과 runtime preflight 목적을 분리한다.
- metadata와 runtime execution 필드 혼합을 방지한다.

위험:

- 초기 모델 수가 많아진다.
- 변환 boundary가 필요하다.

### 후보 E: input model 없이 validator interface 먼저 설계

판정:

- 비권장.

이유:

- validator interface는 input type이 없으면 추상적이다.
- `object` / `dynamic` / dictionary input으로 흐를 위험이 있다.
- input model boundary가 먼저 필요하다.

## 13. 권장안

AH-RUNTIME-55 권장안:

1. 후보 D를 장기 방향으로 채택한다.
2. 단, 다음 skeleton은 후보 B의 최소 neutral draft / candidate model placement를 먼저 확정한다.
3. raw JSON은 external parser 책임으로 둔다.
4. validator는 neutral candidate만 받는다.
5. validator interface skeleton은 input model이 정리된 뒤 진행한다.

즉, 다음 단계에서 바로 validator interface를 만들기보다, Flow Definition Model Placement Review를 먼저 수행하는 것이 안전하다.

## 14. 다음 단계 후보

추천 우선순위:

1. AH-RUNTIME-56 Flow Definition Model Placement Review
   - `FlowValidationDraft` / `FlowExecutionCandidate` / `FlowStepCandidate` / `FlowBindingCandidate` / `FlowPolicyCandidate`를 어디에 둘지 검토한다.
   - Contracts / Runtime / 별도 Runtime-neutral Definition project / Authoring Parser layer 중 위치를 판단한다.

2. AH-RUNTIME-57 Neutral Validation Input Model Skeleton
   - placement review 이후 최소 model만 추가한다.
   - parser / executor / validator rule 구현은 하지 않는다.

3. AH-RUNTIME-58 Validator Interface Skeleton
   - input model이 정리된 뒤 `Validate(candidate)` 형태를 검토한다.

4. AH-RUNTIME-59 Extension Rule Provider Boundary Review
   - XGT / DB / Payload validation provider의 input/output 경계를 검토한다.

## 15. 제외한 범위

이번 AH-RUNTIME-55에서는 다음을 하지 않았다.

- 코드 수정
- 테스트 수정
- FLOW.JSON 파일 생성
- JSON schema 파일 생성
- parser 구현
- validator 구현
- validator interface 추가
- FlowDefinition C# model 추가
- FlowStep C# model 추가
- Binding C# model 추가
- Policy C# model 추가
- Flow Executor 구현
- XGT Adapter 구현
- DB Query 구현
- Payload Builder 이식
- csproj 수정
- ProjectReference 추가
- PackageReference 추가
- commit

## 16. 실행한 명령

AH-RUNTIME-55 Boundary Review 당시 실행한 명령:

- `git log --oneline -12`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-50.md`
- `Get-Content docs\harness\AH-RUNTIME-51.md`
- `Get-Content docs\harness\AH-RUNTIME-52.md`
- `Get-Content docs\harness\AH-RUNTIME-53.md`
- `Get-Content docs\harness\AH-RUNTIME-54.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-ChildItem src\CAAutomationHub.Contracts\Runtime\Validation`
- `Get-ChildItem tests\CAAutomationHub.Runtime.Tests\Validation`
- `rg "ValidationResult|ValidationIssue|ValidationSeverity|ValidationCategory" src tests`
- `rg "FLOW.JSON|flow|bindings|metadata|schemaVersion|Validation|Validator|RuleId|BlocksExecution" docs/harness docs/context src tests`
- `rg "Xgt|FakePlc|Db|DB|Sql|SQL|Json|JSON|Executor|Wpf|ProjectReference|PackageReference" src/CAAutomationHub.Contracts/Runtime/Validation tests/CAAutomationHub.Runtime.Tests/Validation`

테스트 / 빌드는 이번 작업이 read-only Boundary Review였으므로 실행하지 않았다.

AH-RUNTIME-55 closeout 문서 작성 후 validation 명령:

- `git diff -- docs/harness/AH-RUNTIME-55.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-55.md`

## 17. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-55 Boundary Review 결과를 `docs/harness/AH-RUNTIME-55.md` closeout 문서로 기록했다.
- validator의 직접 입력은 raw `FLOW.JSON` 문자열이 아니라는 결론을 기록했다.
- validator의 직접 입력은 JSON parser 결과물 자체도 아니라는 결론을 기록했다.
- validator input 후보는 parsed neutral draft model 또는 parsed neutral executable candidate라는 결론을 기록했다.
- Authoring-time validation과 Runtime preflight validation의 목적 차이를 기록했다.
- 장기적으로 Draft와 Executable Candidate를 구분하는 방향이 안전하다는 판단을 기록했다.
- raw JSON parsing과 JSON schema validation은 external parser / authoring layer 책임이라는 판단을 기록했다.
- Runtime core가 raw JSON parser, JSON schema validator, XGT execution, DB query, payload layout implementation을 소유하지 않는다는 boundary를 기록했다.
- Contracts에는 `ValidationResult` 계열처럼 runtime-neutral shared result model은 둘 수 있지만, FlowDefinition / FlowStep / Binding / Policy input model까지 곧바로 넣는 것은 신중해야 한다고 기록했다.
- 후보 A / B / C / D / E를 검토했다.
- extension rule provider는 shared `ValidationIssue`로 결과를 합산하고 provider별 external context를 사용하는 구조가 적절하다고 기록했다.
- 다음 단계 후보로 AH-RUNTIME-56 Flow Definition Model Placement Review를 우선 추천했다.
- 코드, 테스트, FLOW.JSON, JSON schema, parser, validator, validator interface, C# input model, Flow Executor, XGT Adapter, DB Query, Payload Builder, csproj, project reference, package reference, commit은 수행하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
