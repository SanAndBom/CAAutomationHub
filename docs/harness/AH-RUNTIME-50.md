# AH-RUNTIME-50 Closeout

## 1. Summary

AH-RUNTIME-50은 AH-RUNTIME-49에서 확정한 PLC별 단일 `FLOW.JSON` 내부 `flow` / `bindings` / `metadata` 구조를 바탕으로 Pilot Flow Schema Draft를 문서 수준에서 검토한 Boundary Review다.

핵심 결론은 schema draft를 실제 파일이나 코드로 고정하지 않고, 문서 수준의 후보로만 제안하는 것이다. 실제 `FLOW.JSON` 파일, JSON schema 파일, parser, executor, C# model, `PilotFlow` class는 만들지 않는다.

권장 최상위 구조는 `schemaVersion` / `flow` / `bindings` / `metadata`다. `flow`는 business definition, `bindings`는 PLC별 연결 정보, `metadata`는 authoring / review / validation 정보를 담당한다.

AH-RUNTIME-47 Matrix와 schema draft는 연결하되, `EvidenceLevel`, `PolicyStatus`, `Source`가 runtime execution 필수값으로 섞이지 않도록 metadata / review 영역에 제한한다.

이번 작업은 AH-RUNTIME-50 Boundary Review 결과를 historical record로 남기는 문서 작성 단계다. production code, test code, 실제 `FLOW.JSON`, JSON schema, parser, executor, C# model, XGT Adapter, DB Query, Payload Builder, project reference, WPF, Contracts, ContextPublisher 수정, commit은 수행하지 않았다.

## 2. Goal

AH-RUNTIME-50의 목표는 Pilot Flow Schema Draft Boundary Review다.

핵심 질문은 다음이었다.

- PLC별 `FLOW.JSON`의 초안 구조는 어떻게 생겨야 하는가?
- `flow` / `bindings` / `metadata`에는 각각 어떤 필드가 들어가야 하는가?
- required / optional 필드는 무엇인가?
- validation rule 후보는 무엇인가?
- AH-RUNTIME-47 Scenario Matrix와 어떻게 연결되는가?
- 장기적으로 template + binding 분리로 전환할 수 있는 구조인가?

이번 단계에서는 실제 JSON 파일이나 schema 파일을 만들지 않고, 문서로 schema draft 후보만 제안했다.

## 3. Background

AH-RUNTIME-47에서는 Pilot Flow Scenario Matrix를 작성했다.

Matrix는 다음을 포함한다.

- Polling / Request Detection
- WorkStart Verified Flow
- WorkStart Ack-Off / User Business Anchor
- WorkComplete / User Business Anchor
- Common Failure / Future Policy
- Error Write Policy
- ACK ON / OFF Policy
- `EvidenceLevel` / `PolicyStatus`
- `FLOW.JSON` / Business Flow Definition 연결 후보

AH-RUNTIME-48에서는 `FLOW.JSON`이 XGT command list가 아니라 PLC별 Business Flow Definition이라는 점을 확정했다.

AH-RUNTIME-49에서는 초기 구조를 PLC별 단일 `FLOW.JSON`으로 유지하되, 내부를 `flow`, `bindings`, `metadata`로 나누는 구조를 권장안으로 확정했다.

AH-RUNTIME-50은 이 구조를 schema draft 후보로 더 구체화했다.

고정 원칙은 다음과 같다.

- `FLOW.JSON`은 XGT command list가 아니다.
- `FLOW.JSON`은 PLC별 Business Flow Definition이다.
- Runtime core는 `FLOW.JSON`, parser, XGT execution, PLC address, payload layout, SQL policy를 모른다.
- Business flow 실행은 Flow Executor / Adapter / DB Query / Payload Builder 계층이 담당해야 한다.
- ContextPublisher 자동 publish는 현재 사용하지 않는다.
- Runtime 작업 기록은 `docs/harness/AH-RUNTIME-xx.md` Closeout을 primary historical record로 사용한다.

## 4. 확인한 AH-RUNTIME-49 기준

AH-RUNTIME-49 기준:

- PLC별 단일 `FLOW.JSON` 내부를 `flow` / `bindings` / `metadata`로 나눈다.
- `flow`는 business step / transition / default policy를 담당한다.
- `bindings`는 `plcId` / signals / addresses / `lotLayout` / `payloadLayoutRef` / `dbQueryPolicyRef`를 담당한다.
- `metadata`는 evidence / `policyStatus` / notes를 담당한다.
- Runtime core는 `FLOW.JSON`, parser, XGT execution, PLC address, payload layout, SQL policy를 모른다.

판단:

- 사용자 의도인 PLC별 한 파일 수정 가능성을 유지한다.
- 장기적으로 `flow`는 공통 template으로, `bindings`는 PLC별 binding으로 분리할 수 있다.
- Runtime core vendor-neutral boundary를 유지한다.

## 5. 최상위 schema 구조 후보

권장 구조:

```json
{
  "schemaVersion": "0.1",
  "flow": {},
  "bindings": {},
  "metadata": {}
}
```

판단:

- `schemaVersion`은 top-level required가 적절하다.
- `flow.version`은 업무 flow 정의의 version이다.
- `schemaVersion`은 문서 구조 version이다.
- `plcId`는 `bindings`에 둔다.
- `metadata`는 authoring / review 용도이며 runtime execution 필수값으로 보지 않는 편이 안전하다.

## 6. flow 섹션 후보

`flow` 섹션은 business definition이다.

필드 후보:

- `id`
- `kind`
  - `WorkStartComplete`
  - `WorkStartOnly`
  - `WorkCompleteOnly`
- `version`
- `description`
- `initialState`
- `requestDetection`
- `states`
- `steps`
- `transitions`
- `defaultPolicies`
- `errorPolicies`
- `ackPolicies`

판단:

- `flow`에는 XGT address를 넣지 않는다.
- `flow`에는 SQL text를 넣지 않는다.
- `flow`에는 payload field offset detail을 넣지 않는다.
- `flow`는 step order와 transition을 선언한다.
- `requestDetection`은 `flow` 안에 어떤 business trigger를 감지한다는 의미로 둘 수 있다.
- 실제 signal / address는 `bindings`에서 참조하는 구조가 좋다.
- `states`는 명시하되 최소 상태 집합만 권장한다.

최소 상태 후보:

- `Waiting`
- `WorkStartRequested`
- `WaitingStartRequestOff`
- `WorkCompleteRequested`
- `WaitingCompleteRequestOff`
- `Failed`

## 7. bindings 섹션 후보

`bindings` 섹션은 PLC별 연결 정보다.

필드 후보:

- `plcId`
- `targetId` 또는 `channelId`
- `signals`
  - `startRequest`
  - `completeRequest`
  - `startAck`
  - `completeAck`
  - `errorCode`
- `wordBlocks`
  - `workStartRead`
  - `workStartWrite`
- `lotLayout`
  - `startSignalIndex`
  - `lotId1Offset`
  - `lotId2Offset`
  - `lotIdWordLength`
  - `completeLotIdOffset` 후보
- `payloadLayoutRef`
- `dbQueryPolicyRef`
- `diagnosticsPolicyRef`
- `timeoutPolicyRef`
- `retryPolicyRef`
- `adapter`
  - `kind`
  - `profile` 또는 protocol hint

판단:

- XGT address는 `bindings`에 둘 수 있지만 Runtime core 소비 대상이 아니다.
- 장기 분리를 위해 `adapter.kind: XGT`와 adapter-specific address block을 둘 수 있다.
- `targetId` / `channelId` / `plcId` 중 Runtime channel lookup이면 `targetId`, PLC 업무 식별이면 `plcId`가 적절하다.

## 8. metadata 섹션 후보

`metadata`는 authoring / review / validation 용도다.

필드 후보:

- `name`
- `description`
- `owner`
- `source`
  - `VerifiedByExistingCode`
  - `UserBusinessAnchor`
  - `FutureAnalysisRequired`
- `evidenceLevel`
- `policyStatus`
- `notes`
- `createdAt`
- `updatedAt`
- `review`
- `relatedHarness`
- `basedOn`
  - `AH-RUNTIME-47`
  - `WorkStartPilotService.RunOnceAsync`

판단:

- `metadata`는 runtime execution에 필수로 두지 않는다.
- `evidenceLevel`은 runtime schema에 직접 필요한 값이 아니다.
- `policyStatus`는 design-time validation에 유용하다.
- `metadata`를 운영 `FLOW.JSON`에 포함할 수는 있으나 executor가 의존하지 않아야 한다.

## 9. step schema 후보

step 필드 후보:

- `id`
- `action`
- `input`
- `output`
- `usesBinding`
- `usesPolicy`
- `onSuccess`
- `onFailure`
- `errorCode`
- `errorWritePolicy`
- `ackAction`
- `dependencyCategory`
- `notes`

`action` 후보:

- `DetectRequest`
- `ExtractLotId`
- `QueryBusinessData`
- `BuildPayload`
- `WritePayload`
- `WriteAck`
- `WriteErrorCode`
- `WaitRequestOff`
- `ReturnState`

판단:

- `action`은 business-oriented 이름을 우선한다.
- direct C# handler name은 금지한다.
- action kind만 둔다.
- `ReadWords`처럼 너무 operation-oriented한 이름은 조심한다.
- `WriteAck` / `WriteErrorCode`는 business action이면서 operation handler로 연결될 수 있다.
- `WaitRequestOff`는 step이면서 state transition과 연결될 수 있다.

## 10. transition schema 후보

transition 필드 후보:

- `from`
- `to`
- `on`
  - `success`
  - `failure`
  - `signalOn`
  - `signalOff`
  - `timeout`
- `condition`
- `action`
- `failurePolicy`
- `returnState`

판단:

- 간단한 flow는 step 내부 `onSuccess` / `onFailure`로 충분할 수 있다.
- 복잡한 state machine은 별도 `transitions` 배열이 더 적절하다.
- ACK OFF 대기는 `WaitRequestOff` step이면서 `WaitingStartRequestOff` -> `Waiting` transition으로 표현할 수 있다.
- 상태 전이는 별도 `transitions`에 두는 편이 검증이 쉽다.

## 11. policy schema 후보

정책 후보:

- `errorPolicy`
  - `code`
  - `writeExpected`
  - `targetRef`
  - `bestEffort`
  - `resultAffectsFinalStatus`
- `ackPolicy`
  - `action`
  - `signalRef`
  - `value`
- `timeoutPolicy`
  - `waitRequestOffTimeoutMs`
  - `commandTimeoutMs`
- `priorityPolicy`
  - `whenBothRequestsOn`
- `missingPolicyHandling`
  - `error`
  - `warning`
  - `blockReview`

판단:

- Error code mapping은 flow default + step override가 좋다.
- Error write target은 binding reference여야 한다.
- ACK action은 step action으로 선언하고 signal / value는 policy 또는 binding에서 참조한다.
- both requests ON은 `priorityPolicy.whenBothRequestsOn`으로 둬야 한다.

## 12. required / optional 후보

Required 후보:

- `schemaVersion`
- `flow.id`
- `flow.kind`
- `flow.initialState`
- `flow.steps`
- `bindings.plcId`
- `bindings.signals.startRequest`
- `bindings.signals.startAck`

Conditionally required 후보:

- `bindings.signals.completeRequest`
- `bindings.signals.completeAck`
- `bindings.signals.errorCode`
- `bindings.payloadLayoutRef`
- `bindings.dbQueryPolicyRef`
- `lotLayout`
- `errorPolicy`

Optional 후보:

- `metadata`
- `diagnosticsPolicyRef`
- `retryPolicyRef`
- `timeoutPolicyRef`
- `description`
- `notes`

검토 포인트:

- `WorkStartOnly`와 `WorkStartComplete`에 따라 required가 달라질 수 있다.
- 완공 flow가 없는 PLC도 있을 수 있다.
- payload write가 없는 flow도 있을 수 있다.
- error write policy가 없는 경우 허용할지 결정이 필요하다.

## 13. validation rule 후보

검증 규칙 후보:

- duplicate step id 금지
- unknown transition target 금지
- required binding 누락 금지
- `MissingPolicy`는 warning 또는 review-blocker
- `AckAction`이 있으면 signal binding 필요
- `ErrorWriteExpected=true`이면 errorCode signal 필요
- `QueryDb` step이면 `dbQueryPolicyRef` 필요
- `BuildPayload` / `WritePayload` step이면 `payloadLayoutRef` 필요
- `startRequest` / `completeRequest` 동시 ON policy 필요
- `WaitRequestOff` step이면 timeout policy 검토 필요
- `flow.kind`와 required bindings 일치 확인

이번 단계에서는 validation 구현하지 않고 후보만 기록했다.

## 14. 예시 JSON fragment 후보

아래 fragment는 docs 보고서 안의 설명용이다. 실제 `FLOW.JSON` 파일 생성 대상이 아니다. XGT raw frame, SQL text, C# code는 포함하지 않는다.

```json
{
  "schemaVersion": "0.1",
  "flow": {
    "id": "work-start-complete",
    "kind": "WorkStartComplete",
    "version": "0.1",
    "initialState": "Waiting",
    "steps": [
      {
        "id": "detect-start-request",
        "action": "DetectRequest",
        "onSuccess": "query-work-start-data"
      }
    ]
  },
  "bindings": {
    "plcId": "PLC-01",
    "signals": {
      "startRequest": "StartRequest",
      "startAck": "StartAck"
    },
    "payloadLayoutRef": "work-start-payload-v1",
    "dbQueryPolicyRef": "work-start-query-v1"
  },
  "metadata": {
    "policyStatus": "Draft",
    "basedOn": ["AH-RUNTIME-47", "WorkStartPilotService.RunOnceAsync"]
  }
}
```

## 15. 후보 A: schema draft를 문서로만 작성

판정:

- 권장

장점:

- 안전하다.
- 코드 오염이 없다.
- 미결정 정책을 `Draft` / `MissingPolicy`로 남길 수 있다.
- 다음 단계 검토가 쉽다.

위험:

- 구현은 아직 시작되지 않는다.

결론:

- AH-RUNTIME-50에는 가장 적절하다.

## 16. 후보 B: JSON Schema 파일까지 생성

판정:

- 비권장

이유:

- 아직 ACK OFF 실패, both requests ON, timeout / retry, missing policy 처리 기준이 흔들린다.
- schema를 너무 빨리 고정할 수 있다.
- 이번 지시 범위도 위반한다.

## 17. 후보 C: C# Flow Definition Model 먼저

판정:

- 후순위

장점:

- type-safe하다.
- executor 구현으로 이어지기 쉽다.
- unit test 작성이 쉽다.

위험:

- 사용자 의도인 `FLOW.JSON` 중심 설계보다 코드 shape가 먼저 굳을 수 있다.
- schema와 model이 엇갈릴 수 있다.

결론:

- schema 방향 확정 뒤 후보로 남긴다.

## 18. 후보 D: Schema + Matrix 동시 관리

판정:

- 유용

장점:

- evidence와 schema의 연결이 명확하다.
- review-friendly하다.

위험:

- matrix metadata와 runtime schema가 섞일 수 있다.
- `EvidenceLevel` 같은 항목이 runtime JSON에 들어갈 위험이 있다.

결론:

- metadata 분리 원칙을 유지하면 유용하다.
- `EvidenceLevel`, `PolicyStatus`, `Source`는 metadata / review 영역에 제한한다.

## 19. 권장안

AH-RUNTIME-50 권장안은 후보 A + 후보 D의 절제된 결합이다.

즉:

- schema draft는 문서로만 제안한다.
- AH-RUNTIME-47 Matrix와 연결한다.
- runtime 실행 필드와 metadata를 분리한다.
- 실제 JSON schema 파일은 만들지 않는다.
- 실제 `FLOW.JSON` 파일도 만들지 않는다.
- parser / executor / C# model도 만들지 않는다.

이 구조는 다음을 보존한다.

- AH-RUNTIME-49의 PLC별 한 파일 편집 가능성
- 장기 template + binding 분리 가능성
- Runtime core vendor-neutral boundary
- `FLOW.JSON` business definition 원칙
- Matrix evidence와 schema direction의 연결성

## 20. AH-RUNTIME-51 후보 및 우선순위

추천 우선순위:

1. Template / Binding Validation Rule Review
   - required binding, missing policy, duplicate step, invalid transition 규칙 정리

2. Flow Executor Boundary Review
   - schema를 어떻게 실행할지 검토

3. Example FLOW.JSON Draft
   - 실제 파일이 아니라 문서 안 예시를 조금 더 구체화

4. Pure Helper Extraction Review
   - `ProcessDataPayloadBuilder` / LOTID extraction / error mapping 이식 후보 검토

5. Flow Definition Model Skeleton
   - C# record / model 정의는 schema 방향이 더 굳은 뒤 진행

## 21. 제외한 범위

이번 AH-RUNTIME-50에서는 다음을 하지 않았다.

- production code 수정
- test code 수정
- 문서 생성 외 작업
- 실제 `FLOW.JSON` 생성
- JSON schema 생성
- parser 구현
- executor 구현
- C# model 생성
- `PilotFlow` class 추가
- XGT Adapter 구현
- DB Query 구현
- Payload Builder 이식
- project reference 추가
- WPF 수정
- Contracts 수정
- ContextPublisher 수정
- commit

## 22. 실행한 명령

AH-RUNTIME-50 Boundary Review 당시 실행한 명령:

현재 repo:

- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-49.md`
- `Get-Content docs\harness\AH-RUNTIME-48.md`
- `Get-Content docs\harness\AH-RUNTIME-47.md`
- `Get-Content docs\harness\AH-RUNTIME-46.md`
- `Get-Content docs\harness\AH-RUNTIME-45.md`
- `rg "FLOW.JSON|flow / bindings / metadata|payloadLayoutRef|dbQueryPolicyRef|EvidenceLevel|PolicyStatus|schema" docs/harness docs/context src tests`
- Runtime polling / test files read-only 확인
- cognitive docs read-only 확인

Sibling repo:

- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short`
- `rg -n "PilotScenarioConfig|ProcessDataPayloadBuilder|LotDataQueryService|WorkStartPilotService|WorkStartPilotResult" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- sibling pilot files read-only 확인

AH-RUNTIME-50 closeout 작성 시 실행한 명령:

- `git status --short`
- `Test-Path docs\harness\AH-RUNTIME-50.md`
- `Get-ChildItem docs\harness -Filter AH-RUNTIME-50.md`

작업 후 validation 명령:

- `git diff -- docs/harness/AH-RUNTIME-50.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-50.md`

## 23. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-50 Boundary Review 결과를 `docs/harness/AH-RUNTIME-50.md` closeout 문서로 기록했다.
- AH-RUNTIME-49 기준인 PLC별 단일 `FLOW.JSON` 내부 `flow` / `bindings` / `metadata` 구조를 기록했다.
- 최상위 schema 구조 후보로 `schemaVersion` / `flow` / `bindings` / `metadata`를 기록했다.
- `flow`, `bindings`, `metadata`, step, transition, policy schema 후보를 기록했다.
- required / optional / conditionally required 후보를 기록했다.
- validation rule 후보를 기록하되 validation 구현은 하지 않았다.
- 설명용 JSON fragment를 문서 안에만 기록했고 실제 `FLOW.JSON` 파일은 생성하지 않았다.
- 후보 A / B / C / D를 검토하고 후보 A + 후보 D의 절제된 결합을 권장안으로 기록했다.
- AH-RUNTIME-51 후보 및 우선순위를 기록했다.
- Runtime core vendor-neutral boundary와 `FLOW.JSON` business definition 원칙을 보존했다.
- AH-RUNTIME-47 Matrix와 schema draft를 연결하되 `EvidenceLevel`, `PolicyStatus`, `Source`를 metadata / review 영역에 제한한다는 판단을 기록했다.
- production code, test code, 실제 `FLOW.JSON`, JSON schema, parser, executor, C# model, `PilotFlow` class, XGT Adapter, DB Query, Payload Builder, project reference, WPF, Contracts, ContextPublisher 수정, commit은 수행하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
