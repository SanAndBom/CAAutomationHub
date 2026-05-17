# AH-RUNTIME-49 Closeout

## 1. Summary

AH-RUNTIME-49는 `FLOW.JSON` Template vs Binding Boundary Review 결과를 정리한 closeout 문서다.

핵심 결론은 후보 B, 즉 PLC별 단일 `FLOW.JSON`을 유지하되 내부를 `flow`, `bindings`, `metadata`로 나누는 구조를 초기 권장안으로 삼는 것이다.

이 구조는 사용자 의도인 "PLC별로 한 곳에서 수정"하는 사용성을 유지한다. 동시에 장기적으로 `flow`는 공통 Flow Template으로, `bindings`는 PLC별 Binding으로 분리할 수 있는 전환 경계를 남긴다.

Runtime core는 계속 `FLOW.JSON`을 모르고 vendor-neutral polling / snapshot state boundary를 유지한다. Runtime core는 parser, XGT-specific flow execution, PLC address, payload layout, SQL policy를 소유하지 않는다.

이번 작업은 AH-RUNTIME-49 Boundary Review 결과를 문서화하는 단계다. production code, test code, `FLOW.JSON`, JSON schema, parser, Flow Executor, PilotFlow class, XGT Adapter, DB Query, payload 이식, project reference, ContextPublisher, commit은 수행하지 않았다.

## 2. Goal

AH-RUNTIME-49의 목표는 초기 `FLOW.JSON`을 PLC별 단일 파일로 시작할지, 장기적으로 공통 Flow Template + PLC별 Binding 구조를 어떻게 열어둘지 검토하는 것이었다.

핵심 질문은 다음이었다.

- 초기에는 PLC별 단일 `FLOW.JSON`으로 갈 것인가?
- 장기적으로 공통 Flow Template + PLC별 Binding 구조를 어떻게 열어둘 것인가?
- 어떤 항목은 flow definition에 남고, 어떤 항목은 binding / config / policy reference로 빠져야 하는가?
- Runtime core vendor-neutral boundary를 유지하면서 PLC별 business flow를 어떻게 정의할 것인가?

이번 단계에서는 구현하지 않고 `FLOW.JSON` 구조 전략과 분리 기준만 검토했다.

## 3. Background

AH-RUNTIME-47에서는 Pilot Flow Scenario Matrix를 작성했다.

Matrix는 다음 영역을 포함했다.

- Polling / Request Detection
- WorkStart Verified Flow
- WorkStart Ack-Off / User Business Anchor
- WorkComplete / User Business Anchor
- Common Failure / Future Policy
- Error Write Policy
- ACK ON / OFF Policy
- EvidenceLevel / PolicyStatus
- `FLOW.JSON` / Business Flow Definition 연결 후보

AH-RUNTIME-48에서는 `FLOW.JSON`이 XGT command list가 아니라 PLC별 Business Flow Definition이라는 점을 확정했다.

AH-RUNTIME-48의 권장안은 다음이었다.

- 초기 전략은 PLC별 단일 `FLOW.JSON` 개념으로 시작한다.
- 단, schema 초안에는 나중에 template + binding으로 분리 가능한 경계를 남긴다.

AH-RUNTIME-49는 이 권장안을 더 구체화했다.

## 4. 확인한 AH-RUNTIME-48 / 47 기준

AH-RUNTIME-48 기준:

- `FLOW.JSON`은 XGT command list가 아니다.
- `FLOW.JSON`은 PLC별 Business Flow Definition이다.
- Runtime core는 parser, XGT execution, PLC address, payload layout, SQL policy를 소유하지 않는다.
- Runtime core는 `FLOW.JSON`을 모르고 vendor-neutral polling / snapshot state path를 유지한다.

AH-RUNTIME-47 기준:

- Matrix는 `ScenarioId`, `EvidenceLevel`, `PolicyStatus`, ACK / Error policy, payload layout reference, DB query policy reference 후보를 포함한다.
- Matrix의 모든 컬럼이 runtime schema에 들어갈 필요는 없다.
- `EvidenceLevel`은 실행 정의가 아니라 검증 근거다.
- `PolicyStatus`는 design-time validation metadata로 유용하다.

## 5. 단일 FLOW.JSON 책임 후보

초기 단일 `FLOW.JSON`에는 다음 항목을 둘 수 있다.

- `flowId`
- `flowVersion`
- target selector
- request detection
- trigger condition
- LOT source
- step order
- action kind
- transition
- return state
- error policy reference
- ACK policy reference
- payload layout reference
- DB query policy reference
- diagnostics policy reference

판단:

- 단일 `FLOW.JSON`은 업무 흐름과 PLC별 binding을 한 파일 안에 함께 둘 수 있다.
- 단, XGT address, payload layout detail, SQL text는 직접 실행 내용이 아니라 `bindings` 또는 reference로 분리해야 한다.
- 초기 단일 파일에서도 `flow`, `bindings`, `metadata` naming을 사용하면 장기 template / binding 분리가 쉬워진다.

## 6. Flow Template 책임 후보

장기적으로 Flow Template은 다음 항목을 가질 수 있다.

- flow kind
- step order
- business condition
- transition
- default ACK / Error policy
- default return states
- required binding keys
- required handler names
- validation rules

Template은 다음을 몰라야 한다.

- PLC address
- payload byte offset detail
- SQL text
- concrete adapter implementation

판단:

- Template은 PLC 주소를 몰라야 한다.
- Template은 payload layout detail을 몰라야 한다.
- Template은 DB query text를 몰라야 한다.
- Template은 "어떤 binding key가 필요하다"까지만 정의하는 것이 안전하다.

## 7. PLC Binding 책임 후보

Binding은 다음 항목을 가질 수 있다.

- `plcId`
- signal binding
- word block binding
- LOT offset / length
- ACK / error signal
- payload layout reference
- DB query policy reference
- timeout / retry / wait policy
- PLC-specific override

판단:

- XGT address는 Binding에 둘 수 있다.
- 단, XGT address는 Runtime core가 아니라 Flow Executor / Adapter-adjacent config가 소비해야 한다.
- Binding이 XGT-specific detail을 가져도 Runtime core가 이를 소유하거나 참조하지 않으면 vendor-neutral boundary는 유지된다.
- 장기적으로는 generic binding과 adapter-specific binding을 나누는 편이 안전하다.

## 8. Payload Layout 위치 판단

`ProcessDataPayloadBuilder`는 byte offset, word length, ASCII / int32 encoding, scaling TODO를 포함한다.

따라서 payload layout은 PLC별로 달라질 가능성이 크다.

권장:

- `FLOW.JSON` 안에는 `payloadLayoutRef`만 둔다.
- layout detail은 별도 layout definition, adapter / helper config, 또는 초기 단계의 C# helper 내부에 둔다.
- 초기 schema에는 `payloadLayoutRef` boundary를 남긴다.

판단:

- payload layout detail이 flow definition에 직접 들어가면 문서가 커지고 business flow 의미가 흐려질 수 있다.
- payload layout은 reference로 두고 별도 layout definition 또는 helper/config 쪽으로 분리하는 것이 장기적으로 안전하다.

## 9. DB Query Policy 위치 판단

SQL text와 connection string은 `FLOW.JSON`에 직접 넣지 않는 편이 안전하다.

권장:

- `dbQueryPolicyRef`
- `queryId`

SQL text와 DB schema mapping은 아래가 담당한다.

- DB query service
- 별도 DB query policy file
- environment-specific configuration

판단:

- SQL text를 `FLOW.JSON`에 직접 넣으면 business flow definition에 DB 실행 detail과 credential 위험이 유입된다.
- `FLOW.JSON`은 query id 또는 query policy reference만 가져야 한다.
- DB schema mapping은 DB query abstraction 또는 policy file이 담당해야 한다.

## 10. Error / ACK Policy 분리 판단

ACK action kind는 template default로 둘 수 있다.

후보:

- `StartAckOn`
- `StartAckOff`
- `CompleteAckOn`
- `CompleteAckOff`

판단:

- ACK address / value와 error code write target은 PLC binding 책임이다.
- Error code mapping은 일부 업무 공통일 수 있다.
- 하지만 target address와 best-effort write 여부는 PLC별 override 가능성을 열어야 한다.
- ACK OFF 실패 정책은 아직 `MissingPolicy`로 남기는 것이 맞다.

## 11. EvidenceLevel / PolicyStatus 처리 판단

`EvidenceLevel`:

- runtime schema가 아니라 authoring / review / harness metadata가 적절하다.
- 실행 정의가 아니라 검증 근거를 나타낸다.

`PolicyStatus`:

- runtime execution 필수값은 아니다.
- 하지만 design-time validation metadata로 유용하다.
- `MissingPolicy`는 schema validation에서 warning 또는 review-blocker로 다룰 후보다.

판단:

- 두 항목 모두 production execution path에 강하게 묶기보다 authoring / validation / harness 문맥에서 먼저 다루는 것이 안전하다.

## 12. 후보 A: 완전한 PLC별 단일 FLOW.JSON

판정:

- 초기 skeleton에는 가장 단순하고 직관적이다.
- 하지만 장기 전환성은 낮다.

장점:

- 신규 PLC 추가 시 하나의 파일만 보면 된다.
- 사용자 의도에 가장 직관적이다.
- 초기 구현이 단순하다.

위험:

- flow, address, layout, query policy가 한 덩어리로 섞인다.
- 중복과 파일 비대화가 빨리 온다.
- 장기적으로 template / binding 분리가 어려워질 수 있다.

## 13. 후보 B: PLC별 FLOW.JSON 내부 flow / bindings / metadata 구조

판정:

- 초기 권장안이다.

구조:

    PLC별 단일 FLOW.JSON
        flow:
            business step / transition / default policy

        bindings:
            plcId / signals / addresses / lotLayout / payloadLayoutRef / dbQueryPolicyRef

        metadata:
            evidence / policyStatus / notes

장점:

- 파일은 하나지만 내부적으로 business definition과 PLC binding을 분리한다.
- 사용자 의도인 "한 곳에서 수정"에 맞다.
- 나중에 template + binding으로 분리하기 쉽다.
- Runtime core vendor-neutral boundary를 유지하기 쉽다.

위험:

- schema가 완전 단일 파일보다 조금 복잡하다.
- flow와 binding 간 reference validation이 필요하다.

## 14. 후보 C: 공통 Flow Template + PLC별 Binding 분리

판정:

- 장기 목표로 적절하다.
- 지금 바로 적용하기에는 이르다.

장점:

- 장기 유지보수에 좋다.
- 공통 업무 흐름 재사용이 가능하다.
- 신규 PLC 추가가 binding 중심으로 쉬워진다.

위험:

- 초기 구조가 복잡하다.
- merge / override / validation rule이 필요하다.
- 사용자가 한 파일에서 보기 어렵다고 느낄 수 있다.

## 15. 후보 D: FLOW.JSON 순수 business flow + 별도 config

판정:

- 경계는 가장 깨끗하지만 초기 사용성이 떨어질 수 있다.

장점:

- business flow가 깨끗해진다.
- vendor-specific binding 분리에 강하다.
- template 구조로 가기 쉽다.

위험:

- 신규 PLC 연결 시 여러 파일을 수정해야 한다.
- 사용자 의도인 "한 곳에서 수정"과 멀어질 수 있다.

## 16. 후보 E: C# DSL / fluent builder

판정:

- Primary definition으로는 비권장이다.
- 테스트 helper 또는 schema generation helper로는 가능성이 있다.

장점:

- type-safe하다.
- refactoring에 강하다.
- compile-time validation이 가능하다.

위험:

- PLC별 JSON 관리 의도와 맞지 않는다.
- 신규 PLC마다 코드 수정이 필요하다.
- 비개발자 / 현장 수정이 어렵다.

## 17. 권장안

AH-RUNTIME-49 권장안은 후보 B다.

권장 구조:

    PLC별 단일 FLOW.JSON
        flow:
            business step / transition / default policy

        bindings:
            plcId / signals / addresses / lotLayout / payloadLayoutRef / dbQueryPolicyRef

        metadata:
            evidence / policyStatus / notes

판단 이유:

- 사용자 의도인 PLC별로 한 곳에서 수정 가능성을 만족한다.
- 초기 구현 난이도가 너무 높지 않다.
- 장기적으로 template + binding 전환 가능성을 남긴다.
- Runtime core vendor-neutral boundary를 유지한다.
- XGT / DB / payload 실행 책임을 분리한다.
- `FLOW.JSON`이 command list가 아닌 business definition으로 유지된다.
- 신규 PLC 추가 시 실용성이 좋다.
- schema validation으로 연결하기 쉽다.

## 18. AH-RUNTIME-50 후보 및 우선순위

추천 우선순위:

1. Pilot Flow Schema Draft
   - AH-RUNTIME-49 권장 구조를 바탕으로 schema 초안을 작성한다.
   - parser / executor 구현은 제외한다.

2. Template / Binding Validation Rule Review
   - required binding, missing policy, duplicate step, invalid transition 규칙을 정리한다.

3. Flow Executor Boundary Review
   - definition 실행 경계와 adapter / handler 호출 경계를 검토한다.

4. Pure Helper Extraction Review
   - payload builder, LOTID extraction, error mapping 이식 가능성을 검토한다.

5. Flow Definition Model Skeleton
   - JSON 이전 C# record / model은 schema 방향이 잡힌 뒤가 낫다.

## 19. 제외한 범위

이번 AH-RUNTIME-49에서는 다음을 하지 않았다.

- 코드 수정
- 테스트 수정
- closeout 문서 생성 외 작업
- `FLOW.JSON` 생성
- JSON schema 생성
- parser / executor 구현
- PilotFlow class 추가
- XGT / DB / payload 이식
- project reference 추가
- package reference 추가
- source copy
- ContextPublisher 수정
- ContextPublisher 자동 publish 재도입
- commit

## 20. 실행한 명령

AH-RUNTIME-49 Boundary Review 당시 실행한 명령:

- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-48.md`
- `Get-Content docs\harness\AH-RUNTIME-47.md`
- `Get-Content docs\harness\AH-RUNTIME-46.md`
- `Get-Content docs\harness\AH-RUNTIME-45.md`
- `rg "FLOW.JSON|Business Flow Definition|Template|Binding|ScenarioId|EvidenceLevel|PolicyStatus|payload layout|DB query|ACK" docs/harness docs/context src tests`
- `rg "ChannelPollingTarget|ChannelPollingResult|RuntimeSnapshot|PollingCycleCoordinator" src tests`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short`
- `rg -n "PilotScenarioConfig|ProcessDataPayloadBuilder|LotDataQueryService|WorkStartPilotService|WorkStartPilotResult" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- Runtime polling files 확인
- `RuntimeProjectReferenceBoundaryTests.cs` 확인
- sibling pilot 관련 파일 read-only 확인

AH-RUNTIME-49 closeout 작성 시 실행한 명령:

- `git status --short`
- `Test-Path docs\harness\AH-RUNTIME-49.md`
- `Get-ChildItem docs\harness -Filter AH-RUNTIME-49.md`

작업 후 validation 명령:

- `git diff -- docs/harness/AH-RUNTIME-49.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-49.md`

## 21. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-49 Boundary Review 결과를 `docs/harness/AH-RUNTIME-49.md` closeout 문서로 기록했다.
- 권장안이 후보 B, 즉 PLC별 단일 `FLOW.JSON` 내부 `flow` / `bindings` / `metadata` 구조라는 점을 기록했다.
- 사용자 의도인 "PLC별로 한 곳에서 수정"과 장기 Template + Binding 전환 가능성을 함께 보존했다.
- AH-RUNTIME-48 / AH-RUNTIME-47 기준을 반영했다.
- 단일 `FLOW.JSON`, Flow Template, PLC Binding의 책임 후보를 기록했다.
- Payload Layout, DB Query Policy, Error / ACK Policy, `EvidenceLevel` / `PolicyStatus` 처리 판단을 기록했다.
- 후보 A / B / C / D / E를 검토하고 권장안을 기록했다.
- AH-RUNTIME-50 후보 및 우선순위를 기록했다.
- Runtime core vendor-neutral boundary를 유지해야 한다는 판단을 기록했다.
- production code, test code, `FLOW.JSON`, schema, parser, executor, PilotFlow class, XGT Adapter, DB Query, payload 이식, project reference, ContextPublisher 수정, commit은 수행하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
