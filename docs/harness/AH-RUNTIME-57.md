# AH-RUNTIME-57 Closeout - Neutral Flow Definition Candidate Skeleton Scope Review

## 1. Summary

AH-RUNTIME-57은 AH-RUNTIME-56의 placement review 결론을 이어받아, 다음 skeleton에서 만들 최소 neutral flow definition candidate model의 범위와 배치 위치를 확정한 Boundary Review다.

결론은 `ValidationResult` 계열은 계속 `CAAutomationHub.Contracts`에 유지하되, neutral flow input candidate는 새 Runtime-neutral definition project인 `CAAutomationHub.FlowDefinitions`에 두는 방향이 가장 안전하다는 것이다.

이번 단계에서는 코드, 테스트, project, csproj, model class, interface를 만들지 않았다. AH-RUNTIME-58에서 최소 project와 model-only skeleton을 추가할 수 있도록 범위만 고정했다.

Self-Check 판정은 `ACCEPT`다. 코드 변경 필요, Runtime owner화 압력, Contracts schema 비대화 압력, XGT / DB / payload detail 유입 압력은 확인되지 않았다.

## 2. Goal

AH-RUNTIME-57의 목표는 다음 질문에 답하는 것이었다.

- model을 어느 project에 둘 것인가?
- 새 project를 만들 것인가?
- 기존 Contracts에 최소 model만 둘 것인가?
- Runtime에 둘 것인가?
- namespace는 어떻게 잡을 것인가?
- AH-RUNTIME-58에서 만들 최소 타입은 무엇인가?
- FlowValidationDraft와 FlowExecutionCandidate를 지금 분리할 것인가?
- 아니면 FlowDefinitionCandidate 하나부터 시작할 것인가?

이번 단계는 read-only review와 closeout 작성만 수행했다.

## 3. 검토 기준

AH-RUNTIME-56 결론을 기준으로 검토했다.

- `ValidationResult` 계열은 Contracts에 유지한다.
- neutral flow input candidate는 장기적으로 별도 Runtime-neutral Definition project 방향이 안전하다.
- Runtime은 flow definition owner가 아니라 preflight consumer다.
- Authoring / Parser layer는 `FlowValidationDraft`에 가깝다.
- shared definition boundary는 `FlowExecutionCandidate` 또는 최소 `FlowDefinitionCandidate` 후보가 적절하다.
- AH-RUNTIME-57에서는 project 생성 여부, namespace, 최소 타입 범위를 먼저 확정한다.

## 4. 후보 A - Contracts에 최소 FlowDefinitionCandidate 추가

판정:

- 조건부 가능하지만 이번 skeleton의 권장안은 아니다.

장점:

- `ValidationResult`와 같은 shared layer에 둘 수 있다.
- Runtime project reference 변경이 적다.
- WPF / Runtime / parser / adapter extension이 접근하기 쉽다.

위험:

- Contracts가 `FLOW.JSON` schema model 저장소로 비대해질 수 있다.
- FlowDefinition / FlowStep / Binding / Policy 전체 구조가 Contracts로 밀려 들어올 수 있다.
- Contracts에 XGT address, SQL text, payload offset 같은 detail 유입을 장기적으로 계속 막아야 한다.

결론:

- `ValidationResult`처럼 안정된 result shape는 Contracts가 적절하다.
- 그러나 input candidate는 result보다 drift 위험이 크므로 별도 definition layer가 더 안전하다.

## 5. 후보 B - Runtime project에 FlowDefinitionCandidate 추가

판정:

- 비권장.

장점:

- Runtime preflight와 가까워 보인다.
- Runtime.Tests에서 빠르게 검증할 수 있다.

위험:

- Runtime core가 flow definition owner처럼 보일 수 있다.
- parser / validator rule / executor / binding detail이 Runtime으로 빨려 들어올 수 있다.
- WPF authoring, external parser, adapter extension과 공유하기 어렵다.
- Runtime shared execution path와 flow definition authoring boundary가 섞일 위험이 있다.

결론:

- Runtime은 candidate consumer 또는 preflight gate 역할에 머물러야 한다.
- Runtime project가 model owner가 되어야 한다는 결론은 나오지 않았다.

## 6. 후보 C - 새 project CAAutomationHub.FlowDefinitions 생성

판정:

- AH-RUNTIME-58 권장 placement.

장점:

- Runtime core vendor-neutral boundary를 보존한다.
- Contracts 비대화를 막는다.
- WPF / Runtime / parser / adapter extension이 공유할 수 있는 Runtime-neutral definition boundary가 생긴다.
- `FLOW.JSON` parser, JSON schema, executor, XGT / DB / payload detail과 분리하기 쉽다.
- 이후 validator interface가 neutral candidate를 입력으로 받는 구조와 잘 맞는다.

위험:

- project 수가 증가한다.
- solution과 project reference를 추가해야 한다.
- 초기 skeleton에는 약간의 구조 비용이 생긴다.

결론:

- 장기 계약과 AH-RUNTIME-56 결론에 가장 잘 맞는다.
- AH-RUNTIME-58에서 최소 project 생성과 model-only skeleton을 함께 수행하는 것이 적절하다.

## 7. 후보 D - 이번에는 project 생성 없이 문서로 범위만 고정

판정:

- AH-RUNTIME-57 자체에는 적절했고 실제로 이 방식으로 완료했다.

장점:

- 코드 변경 없이 placement 압력을 검토할 수 있다.
- STOP CHECK를 먼저 통과할 수 있다.
- AH-RUNTIME-58의 project 생성 여부와 최소 타입 범위를 분명히 넘겨줄 수 있다.

위험:

- 다음 단계에서 다시 placement가 흔들리면 skeleton이 지연될 수 있다.

결론:

- AH-RUNTIME-57 산출물은 문서만 생성한다.
- AH-RUNTIME-58에서는 후보 C를 실행 후보로 사용한다.

## 8. 후보 E - Draft / Executable Candidate를 처음부터 분리

판정:

- 장기 방향으로는 적절하지만 AH-RUNTIME-58 skeleton에는 과하다.

장점:

- Authoring-time validation과 Runtime preflight 목적을 분리할 수 있다.
- `FlowValidationDraft`와 `FlowExecutionCandidate`의 책임을 명확히 할 수 있다.

위험:

- 초기 타입 수가 늘어난다.
- parser / authoring / runtime 승격 경계가 아직 구현되지 않았는데 모델이 먼저 커질 수 있다.
- Flow Executor 또는 parser 구현으로 오해될 수 있다.

결론:

- AH-RUNTIME-58에서는 `FlowDefinitionCandidate` 하나로 시작한다.
- Draft / Executable 분리는 후속 parser 또는 preflight 승격 요구가 생길 때 검토한다.

## 9. 권장 Placement

AH-RUNTIME-58 권장 placement:

- project: `src/CAAutomationHub.FlowDefinitions/CAAutomationHub.FlowDefinitions.csproj`
- namespace: `CAAutomationHub.FlowDefinitions`
- tests: 기존 `tests/CAAutomationHub.Runtime.Tests`에 compile / model behavior test를 추가한다.

판단 이유:

- Runtime core가 flow definition owner가 되지 않는다.
- Contracts가 input schema model 저장소로 비대해지지 않는다.
- Runtime, WPF, parser, adapter extension이 장기적으로 같은 neutral candidate를 공유할 수 있다.
- `ValidationResult`는 Contracts에 유지하고, input candidate는 FlowDefinitions가 소유하는 역할 분리가 자연스럽다.
- raw JSON / XGT / DB / payload detail이 candidate model에 들어올 이유가 없다.

## 10. AH-RUNTIME-58 최소 타입 범위

AH-RUNTIME-58에서 만들 최소 타입 후보:

- `FlowDefinitionCandidate`
- `FlowStepCandidate`
- `FlowReference`
- `FlowPolicyReference`

최소 필드 후보:

`FlowDefinitionCandidate`

- `string FlowId`
- `string FlowKind`
- `string InitialState`
- `IReadOnlyList<FlowStepCandidate> Steps`
- `IReadOnlyDictionary<string, FlowReference>? Bindings`
- `IReadOnlyDictionary<string, FlowPolicyReference>? Policies`

`FlowStepCandidate`

- `string StepId`
- `string ActionKind`
- `string? OnSuccess`
- `string? OnFailure`
- `IReadOnlyList<string>? RequiredBindingRefs`
- `IReadOnlyList<string>? RequiredPolicyRefs`

`FlowReference`

- `string Key`
- `string Kind`

`FlowPolicyReference`

- `string Key`
- `string Kind`
- `string? Status`

범위 제한:

- validation rule implementation 없음
- validator interface 없음
- parser 없음
- executor 없음
- JSON schema 없음
- FLOW.JSON 파일 없음
- XGT / DB / payload detail 없음

## 11. 금지 범위 유지 여부

유지했다.

이번 AH-RUNTIME-57에서는 다음을 하지 않았다.

- actual FLOW.JSON 파일 생성
- JSON schema 파일 생성
- raw JSON parser 구현
- Flow Executor 구현
- XGT Adapter 구현
- DB Query 구현
- Payload Builder 이식
- ACK/Error Writer 구현
- WorkStartPilotService source copy
- XgtDriverCore 참조 추가
- FakePlc 참조 추가
- XgtChannelRunner 참조 추가
- WPF wiring
- Runtime core에 XGT / DB / payload detail 추가
- ChannelPollingTarget에 XGT address / datatype / count 추가
- ChannelPollingResult에 LOTID / DB result / ACK policy 추가
- ContextPublisher 자동 publish 재도입
- code 생성
- test 생성
- csproj 수정
- project 생성
- model class 생성
- interface 생성

## 12. STOP CHECK

STOP CHECK 결과:

- code를 만들 필요가 생김: 발생하지 않음
- project를 지금 바로 만들어야 한다는 결론이 강하게 필요함: 발생하지 않음
- Contracts에 큰 FlowDefinition schema model이 들어가야 한다는 결론: 발생하지 않음
- Runtime project가 model owner가 되어야 한다는 결론: 발생하지 않음
- XGT / DB / payload detail이 candidate model에 들어가야 한다는 압력: 발생하지 않음

따라서 AH-RUNTIME-58로 진행 가능하다.

## 13. 실행한 명령

- `git log --oneline -12`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-56.md`
- `Get-Content docs\harness\AH-RUNTIME-55.md`
- `Get-Content docs\harness\AH-RUNTIME-54.md`
- `Get-Content src\CAAutomationHub.Contracts\CAAutomationHub.Contracts.csproj`
- `Get-Content src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj`
- `Get-Content tests\CAAutomationHub.Runtime.Tests\RuntimeProjectReferenceBoundaryTests.cs`
- `rg "FlowValidation|FlowExecution|FlowDefinition|FlowStep|FlowBinding|FlowPolicy" src tests docs/harness docs/context`
- `git diff --check`

AH-RUNTIME-57 closeout 작성 전 `git status --short` 결과:

```text
```

`git diff --check` 결과:

```text
```

## 14. Self-Check

판정: ACCEPT

이유:

- 후보 A부터 E까지 검토했다.
- 권장 placement를 `CAAutomationHub.FlowDefinitions` 새 Runtime-neutral Definition project로 확정했다.
- AH-RUNTIME-58에서 만들 최소 타입 범위를 `FlowDefinitionCandidate`, `FlowStepCandidate`, `FlowReference`, `FlowPolicyReference`로 제한했다.
- Draft / Executable Candidate는 지금 분리하지 않고 `FlowDefinitionCandidate` 하나로 시작하는 방향을 정했다.
- Runtime project가 model owner가 되지 않는다는 boundary를 유지했다.
- Contracts 비대화 방지 원칙을 유지했다.
- XGT / DB / payload / raw JSON / parser / executor detail을 candidate model에 넣지 않는 원칙을 유지했다.
- AH-RUNTIME-57 산출물은 closeout 문서 하나로 제한했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
