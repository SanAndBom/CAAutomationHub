# AH-RUNTIME-56 Closeout - Flow Definition Model Placement Review

## 1. Summary

AH-RUNTIME-56은 AH-RUNTIME-55에서 후보로 정리한 `FlowValidationDraft` / `FlowExecutionCandidate` 같은 neutral validation input model을 어느 계층에 둘지 검토한 Boundary Review다.

핵심 결론은 `ValidationResult` 계열은 현재처럼 `CAAutomationHub.Contracts`에 두는 것이 적절하지만, `FlowValidationDraft` / `FlowExecutionCandidate` 계열 input model은 Contracts에 바로 넣기보다 별도 Runtime-neutral Definition project 방향을 장기 권장안으로 잡는 것이 안전하다는 점이다.

Runtime project에 input model을 두는 것은 비권장이다. Runtime은 flow definition owner가 아니라 preflight consumer 역할에 머무는 것이 안전하다. Draft model은 authoring / parser layer에 가깝고, Execution Candidate는 shared runtime-neutral definition boundary에 가깝다. WPF editor model과 shared neutral model은 분리해야 한다.

이번 작업은 AH-RUNTIME-56 Boundary Review 결과를 historical record로 남기는 문서 작성 단계다. 코드, 테스트, C# model, validator interface, FLOW.JSON, JSON schema, parser, validator, Flow Executor, XGT Adapter, DB Query, Payload Builder, csproj, ProjectReference, PackageReference, ContextPublisher 자동 publish, commit은 수행하지 않는다.

Self-Check 판정은 `ACCEPT_WITH_CORRECTION`이다. 이유는 Boundary Review 자체는 완료했고 금지 범위도 지켰지만, `COGNITIVE_SYNC_CHECK.md`와 `META_IPRO_CODEX_COGNITIVE_INTERFACE.md`의 current anchor 일부가 AH-RUNTIME-51/52 기준으로 오래되어 있어 git log와 AH-RUNTIME-55 closeout을 최신 기준으로 보정해 사용했기 때문이다.

## 2. Goal

AH-RUNTIME-56의 목표는 Flow Definition Model Placement Review다.

핵심 질문은 다음이었다.

```text
FlowValidationDraft / FlowExecutionCandidate 같은 neutral validation input model을
어느 계층에 둘 것인가?
```

검토 대상은 다음과 같다.

- Contracts
- Runtime
- 별도 Runtime-neutral Definition project
- Authoring / Parser layer
- Contracts + 별도 project 절충
- Draft vs Candidate 분리
- 최소 model skeleton 가능성

이번 단계에서는 구현하지 않고 placement 후보와 권장안을 정리했다.

## 3. Context

AH-RUNTIME-55의 핵심 결론은 다음이다.

- validator의 직접 입력은 raw `FLOW.JSON`이 아니다.
- validator의 직접 입력은 JSON parser 결과물 자체도 아니다.
- validator의 입력 후보는 parsed neutral draft model 또는 parsed neutral executable candidate다.
- Authoring-time validation과 Runtime preflight validation은 목적이 다르다.
- 장기적으로는 Draft와 Executable Candidate를 구분하는 방향이 안전하다.

AH-RUNTIME-56은 이 후보 모델들을 어디에 둘지 검토했다.

유지해야 하는 주요 boundary:

- `CAAutomationHub.Runtime` core는 vendor-neutral이어야 한다.
- Runtime core는 raw `FLOW.JSON` parser를 소유하지 않는다.
- Runtime core는 JSON schema validator를 소유하지 않는다.
- Runtime core는 XGT-specific flow execution을 소유하지 않는다.
- Runtime core는 DB query를 소유하지 않는다.
- Runtime core는 payload layout implementation을 소유하지 않는다.
- Runtime core는 PLC별 address / SQL policy를 알지 않는다.
- Contracts에 XGT address / SQL text / payload byte offset implementation을 넣지 않는다.
- Contracts가 `FLOW.JSON` raw schema model 저장소로 비대해지면 안 된다.
- Pilot business flow와 Runtime polling state path는 분리한다.
- ContextPublisher 자동 publish는 재도입하지 않는다.

## 4. 확인한 현재 프로젝트 / reference / validation model 상태

현재 HEAD:

- `6dfdf00`
- `docs: close out AH-RUNTIME-55 validation input boundary review`

현재 source project:

- `CAAutomationHub.Contracts`
- `CAAutomationHub.Runtime`
- `CAAutomationHub.Wpf`

현재 Runtime project reference 상태:

- `CAAutomationHub.Runtime`은 `CAAutomationHub.Contracts`만 참조한다.
- `src/CAAutomationHub.Runtime/CAAutomationHub.Runtime.csproj`에는 Contracts `ProjectReference`만 있다.

Boundary test:

- `tests/CAAutomationHub.Runtime.Tests/RuntimeProjectReferenceBoundaryTests.cs`는 Runtime이 Contracts만 참조하고 WPF / Xgt / FakePlc를 참조하지 않는 것을 검증한다.

현재 validation result model 위치:

- `src/CAAutomationHub.Contracts/Runtime/Validation`

현재 존재하는 validation result model:

- `ValidationSeverity`
- `ValidationCategory`
- `ValidationIssue`
- `ValidationResult`

현재 존재하지 않는 타입:

- `FlowValidationDraft`
- `FlowExecutionCandidate`
- `FlowStepCandidate`
- `FlowBindingCandidate`
- `FlowPolicyCandidate`
- `IFlowDefinitionValidator`
- `IValidationRuleProvider`

주의:

- `docs/context/COGNITIVE_SYNC_CHECK.md`와 `docs/context/META_IPRO_CODEX_COGNITIVE_INTERFACE.md`의 current anchor가 일부 AH-RUNTIME-51/52 기준으로 오래되어 있다.
- 이번 판단에서는 `git log`와 AH-RUNTIME-55 closeout을 최신 기준으로 사용했다.
- 이 사유로 Self-Check는 `ACCEPT`가 아니라 `ACCEPT_WITH_CORRECTION`이다.

## 5. Contracts 배치 후보 검토

### 장점

- WPF authoring, Runtime preflight, external parser, adapter extension이 같은 타입을 참조하기 쉽다.
- `ValidationResult` 계열이 이미 Contracts에 있으므로 공유성이 좋다.
- project reference 추가가 적다.

### 위험

- `FlowDefinition`, `FlowStep`, `Binding`, `Policy`가 Contracts에 들어가면 Contracts가 `FLOW.JSON` schema model 저장소처럼 비대해질 수 있다.
- XGT / DB / payload detail 유입을 강하게 막아야 한다.
- versioning 부담이 커질 수 있다.

### Contracts에 허용 가능한 범위

- 아주 작은 runtime-neutral candidate
- `flowId`
- `flowKind`
- `stepId`
- `actionKind`
- `transitionRef`
- `bindingRef`
- `policyRef`
- `policyStatus`

### Contracts에 넣으면 안 되는 범위

- raw JSON AST
- XGT address
- SQL text
- connection string
- payload byte offset
- `RuntimeSnapshot`
- `ChannelPollingResult`

### 판정

최소 runtime-neutral candidate만이면 가능하다. 그러나 FlowDefinition 전체를 Contracts에 바로 넣는 것은 신중해야 한다.

## 6. Runtime 배치 후보 검토

### 장점

- Runtime preflight와 가깝다.
- Runtime tests에서 빠르게 검증 가능하다.
- 새 project가 필요 없다.

### 위험

- Runtime core가 flow definition owner처럼 보일 수 있다.
- parser / binding / policy / executor 책임이 Runtime으로 빨려 들어올 수 있다.
- WPF authoring / external parser와 공유가 어려워진다.
- Runtime이 `FLOW.JSON` boundary를 흡수할 위험이 있다.

### 판정

비권장이다.

Runtime은 `FlowExecutionCandidate`의 consumer 또는 preflight gate 역할까지만 맡는 것이 안전하다. Runtime project가 input model을 소유하면 Runtime core vendor-neutral boundary가 흐려지고, future parser / executor / binding detail이 Runtime으로 이동할 가능성이 커진다.

## 7. 별도 Runtime-neutral Definition project 후보 검토

후보 이름:

- `CAAutomationHub.FlowDefinitions`
- `CAAutomationHub.FlowContracts`
- `CAAutomationHub.BusinessFlows`

선호 후보:

- `CAAutomationHub.FlowDefinitions`
- `CAAutomationHub.FlowContracts`

### 장점

- Contracts 비대화 방지
- Runtime vendor-neutral 유지
- WPF / parser / Runtime preflight / adapter extension이 공유 가능
- `FLOW.JSON` business definition 계층을 Runtime polling state path와 분리 가능

### 위험

- project 수 증가
- solution / reference 정책 추가 필요
- 초기 skeleton에는 과설계처럼 보일 수 있음

### 판정

장기적으로 가장 깨끗한 방향이다.

다만 AH-RUNTIME-57에서 바로 project를 만들기보다 최소 모델 범위를 먼저 확정하는 편이 안전하다. project 생성은 reference direction과 solution 관리 부담을 동반하므로, candidate shape가 최소 수준으로 고정된 뒤 수행하는 것이 좋다.

## 8. Authoring / Parser layer 배치 후보 검토

### FlowValidationDraft

- authoring-time model
- parser / authoring layer에 가까움
- metadata / evidence / policyStatus / review trace를 많이 가질 수 있음
- raw JSON source mapping은 가능하되 shared candidate 내부에는 넣지 않는 편이 안전

### FlowExecutionCandidate

- runtime preflight model
- shared runtime-neutral layer에 가까움
- execution 전 blocking issue 확인 대상
- adapter-specific detail 없음

### WPF editor model

- shared neutral model과 분리해야 함
- WPF view model을 validator가 직접 참조하면 UI가 validation boundary를 소유하게 됨

### 판정

Draft는 authoring / parser 쪽, ExecutionCandidate는 shared runtime-neutral definition 쪽이 장기적으로 적절하다.

WPF는 shared validation result와 shared neutral candidate를 표시하거나 변환하는 consumer가 적절하다. WPF editor model 자체를 validator input contract로 삼으면 UI layer가 validation boundary를 소유하는 구조가 된다.

## 9. Contracts + 별도 project 절충 후보 검토

가장 균형 잡힌 구조:

- Contracts:
  - `ValidationResult`
  - `ValidationIssue`
  - `ValidationSeverity`
  - `ValidationCategory`

- 별도 Flow Definition project:
  - neutral input candidate model

- Runtime:
  - preflight consumer

- WPF / parser / adapter extension:
  - shared candidate와 validation result를 참조

### 판정

장기 권장안이다.

지금은 문서상 방향만 잡고 project 생성은 후속 skeleton에서 판단한다. 이 절충안은 `ValidationResult`처럼 이미 안정된 result shape는 Contracts에 유지하고, drift 위험이 큰 flow definition input model은 별도 shared definition layer로 분리한다.

## 10. Draft vs Candidate 배치 차이

### FlowValidationDraft

- authoring-time
- parser / authoring layer 가까움
- metadata / evidence / policyStatus 보존
- review feedback 풍부

### FlowExecutionCandidate

- runtime preflight
- shared runtime-neutral layer 가까움
- execution 전 blocking issue 확인 대상
- adapter-specific detail 없음

### 초기 전략

처음부터 둘 다 만들면 모델 수가 늘어난다. 초기에는 `FlowDefinitionCandidate` 하나로 시작하고, Draft / Executable 분리는 후속 요구가 구체화될 때 나누는 전략도 가능하다.

단일 `FlowDefinitionCandidate`로 시작하더라도 raw JSON, source span, XGT address, SQL text, payload layout implementation 같은 세부 정보는 포함하지 않아야 한다.

## 11. 최소 model skeleton 가능성

다음 skeleton에서 안전한 최소 범위:

- `FlowDefinitionCandidate`
- `FlowStepCandidate`
- 최소한의 `BindingRef` / `PolicyRef` 표현

보류 권장:

- rich metadata / evidence
- raw JSON path / source span
- XGT / DB / payload detail
- executor handler mapping
- validator interface

주의:

- `FlowBindingCandidate` / `FlowPolicyCandidate`까지 한 번에 만들 수는 있지만, AH-RUNTIME-50 schema draft와 과하게 결합될 수 있다.
- 따라서 reference 수준부터 시작하는 것이 안전하다.

## 12. 후보 A / B / C / D / E 검토

### 후보 A: Contracts에 input model 추가

판정:

- 조건부 가능

장점:

- WPF / Runtime / external validation이 공유하기 쉽다.
- `ValidationResult`가 이미 Contracts에 있으므로 result와 input model이 같은 shared layer에 있다.
- project reference 추가가 적다.

위험:

- Contracts가 `FLOW.JSON` schema model 저장소로 비대해질 수 있다.
- XGT / DB / payload detail 유입을 막아야 한다.
- versioning 부담이 커질 수 있다.

결론:

- 최소 runtime-neutral candidate까지만 가능.
- FlowDefinition 전체를 Contracts에 바로 넣는 것은 신중.

### 후보 B: Runtime project에 input model 추가

판정:

- 비권장

장점:

- Runtime preflight와 가깝다.
- Runtime tests에서 빠르게 검증 가능하다.
- 새 project가 필요 없다.

위험:

- Runtime core가 definition owner로 drift할 위험이 큼.
- WPF authoring / external parser와 공유가 어렵다.
- Runtime이 `FLOW.JSON` boundary를 흡수할 위험이 있다.

결론:

- Runtime은 consumer 또는 preflight validator 위치가 적절.

### 후보 C: 별도 Runtime-neutral Definition project

판정:

- 장기 권장

장점:

- Contracts 비대화 방지
- Runtime vendor-neutral 유지
- WPF / Runtime / parser / adapter extension이 공통 모델을 참조 가능
- `FLOW.JSON` / Business Flow Definition 계층을 명확히 분리

위험:

- project 수 증가
- 초기 구현 부담 증가
- reference policy를 새로 정해야 함

결론:

- 장기적으로 가장 깨끗함.
- 단, 지금 바로 project 생성은 이른지 검토 필요.

### 후보 D: Draft는 Authoring / Parser, Candidate는 shared layer

판정:

- 장기 구조로 적절

장점:

- authoring metadata와 runtime preflight field가 섞이지 않음
- Draft와 Executable Candidate 목적이 분리됨
- AH-RUNTIME-55 결론과 잘 맞음

위험:

- 모델 수 증가
- 변환 boundary 필요
- 초기에는 과설계 가능

결론:

- 장기 방향으로 적절.
- 초기 skeleton에서는 최소 shared candidate부터 시작 가능.

### 후보 E: placement 미확정 유지

판정:

- AH-RUNTIME-56 자체에는 가능하지만 다음 skeleton을 위해 최소 방향은 정해야 함

장점:

- 과설계 방지
- 실제 Flow Executor / parser 요구가 더 명확해질 때까지 유예 가능

위험:

- validation model skeleton 이후 다음 구현이 지연됨
- validator interface / input model이 계속 미뤄짐
- 구현 가속이 어려움

결론:

- AH-RUNTIME-56에서는 문서 검토로 충분하지만, AH-RUNTIME-57을 위해 최소 방향은 제시해야 한다.

## 13. 권장안

AH-RUNTIME-56 권장안은 후보 C + 후보 D의 절제된 결합이다.

장기 방향:

- `ValidationResult` 계열은 Contracts에 유지한다.
- neutral flow input candidate는 별도 Runtime-neutral Definition project로 분리한다.
- `FlowValidationDraft`는 authoring / parser boundary에 가깝게 둔다.
- `FlowExecutionCandidate` 또는 최소 `FlowDefinitionCandidate`는 shared definition boundary에 둔다.
- Runtime은 candidate를 직접 소유하지 않고 preflight consumer로 둔다.

단기 방향:

- AH-RUNTIME-57에서는 project 생성 전에 최소 skeleton 범위를 확정한다.
- 처음부터 Draft / Executable을 모두 만들기보다 `FlowDefinitionCandidate` 중심의 최소 neutral model부터 검토한다.

이 권장안은 다음 기준을 가장 잘 만족한다.

- Runtime core vendor-neutral 유지
- Contracts 비대화 방지
- WPF authoring / Runtime preflight / external parser / adapter extension 공유 가능성
- `FLOW.JSON` business definition 원칙 유지
- raw JSON parser / JSON schema 책임 분리
- Draft / Executable Candidate 목적 분리 가능성
- 향후 Flow Executor / Validator interface로 이어질 수 있는 구조

## 14. 다음 단계 후보

1. AH-RUNTIME-57 Neutral Flow Definition Candidate Skeleton Scope Review
   - project 생성 여부
   - namespace
   - 최소 타입 범위 확정

2. AH-RUNTIME-58 Minimal Candidate Model Skeleton
   - parser / validator / executor 없이 model-only 추가

3. AH-RUNTIME-59 Validator Interface Skeleton
   - input model이 고정된 뒤 `Validate(candidate)` 형태 검토

## 15. 제외한 범위

이번 AH-RUNTIME-56에서는 다음을 하지 않았다.

- code 수정
- test 수정
- docs/harness 생성 / 수정 외 작업
- C# model 추가
- validator interface 추가
- FLOW.JSON 파일 생성
- JSON schema 파일 생성
- parser 구현
- validator 구현
- Flow Executor 구현
- XGT Adapter 구현
- DB Query 구현
- Payload Builder 이식
- csproj 수정
- ProjectReference 추가
- PackageReference 추가
- ContextPublisher 자동 publish 재도입
- commit

## 16. 실행한 명령

AH-RUNTIME-56 Boundary Review 당시 실행한 명령:

- `git log --oneline -12`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-50.md`
- `Get-Content docs\harness\AH-RUNTIME-51.md`
- `Get-Content docs\harness\AH-RUNTIME-52.md`
- `Get-Content docs\harness\AH-RUNTIME-53.md`
- `Get-Content docs\harness\AH-RUNTIME-54.md`
- `Get-Content docs\harness\AH-RUNTIME-55.md`
- `Get-ChildItem src\CAAutomationHub.Contracts\Runtime\Validation`
- `Get-Content src\CAAutomationHub.Contracts\CAAutomationHub.Contracts.csproj`
- `Get-Content src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj`
- `Get-Content tests\CAAutomationHub.Runtime.Tests\RuntimeProjectReferenceBoundaryTests.cs`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `rg "ValidationResult|ValidationIssue|ValidationSeverity|ValidationCategory" src tests`
- `rg "FlowValidation|FlowExecution|FlowDefinition|FlowStep|FlowBinding|FlowPolicy|IFlowDefinitionValidator|IValidationRuleProvider" src tests docs/harness docs/context`
- `rg "ProjectReference|PackageReference" src tests -g "*.csproj"`
- `rg "Xgt|FakePlc|Db|DB|Sql|SQL|Json|JSON|Executor|Wpf|ProjectReference|PackageReference" src/CAAutomationHub.Contracts/Runtime/Validation tests/CAAutomationHub.Runtime.Tests/Validation`

테스트 / 빌드는 read-only Boundary Review였으므로 실행하지 않았다.

AH-RUNTIME-56 closeout 문서 작성 후 validation 명령:

- `git diff -- docs/harness/AH-RUNTIME-56.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-56.md`

## 17. Self-Check

판정: ACCEPT_WITH_CORRECTION

이유:

- AH-RUNTIME-56 Boundary Review 결과를 `docs/harness/AH-RUNTIME-56.md` closeout 문서로 기록했다.
- `ValidationResult` 계열은 현재처럼 Contracts에 두는 것이 적절하다는 결론을 기록했다.
- `FlowValidationDraft` / `FlowExecutionCandidate` 계열 input model은 Contracts에 바로 넣기보다 별도 Runtime-neutral Definition project 방향을 장기 권장안으로 삼는 것이 안전하다는 결론을 기록했다.
- Runtime project에 input model을 두는 것은 비권장이라고 기록했다.
- Runtime은 flow definition owner가 아니라 preflight consumer 역할에 머무는 것이 안전하다고 기록했다.
- Draft model은 authoring / parser layer에 가깝고, Execution Candidate는 shared runtime-neutral definition boundary에 가깝다고 기록했다.
- WPF editor model과 shared neutral model은 분리해야 한다고 기록했다.
- 다음 skeleton에서 바로 project 생성까지 가기보다는 최소 candidate shape를 한 번 더 고정한 뒤 진행하는 편이 안전하다고 기록했다.
- 장기 권장안은 Contracts + 별도 Runtime-neutral Definition project의 절충 구조라고 기록했다.
- 다음 단계 후보로 AH-RUNTIME-57 Neutral Flow Definition Candidate Skeleton Scope Review를 기록했다.
- 코드, 테스트, C# model, validator interface, FLOW.JSON, JSON schema, parser, validator, Flow Executor, XGT Adapter, DB Query, Payload Builder, csproj, ProjectReference, PackageReference, ContextPublisher 자동 publish, commit은 수행하지 않았다.

Correction:

- `docs/context/COGNITIVE_SYNC_CHECK.md`와 `docs/context/META_IPRO_CODEX_COGNITIVE_INTERFACE.md`의 current anchor 일부가 AH-RUNTIME-51/52 기준으로 오래되어 있다.
- 이번 AH-RUNTIME-56 판단에서는 `git log`와 AH-RUNTIME-55 closeout을 최신 기준으로 사용했다.
- 이 correction은 문서 기준점 보정이며 코드나 Runtime boundary 변경은 아니다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
