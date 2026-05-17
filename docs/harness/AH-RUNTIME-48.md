# AH-RUNTIME-48 Closeout

## 1. Summary

AH-RUNTIME-48은 AH-RUNTIME-47 Pilot Flow Scenario Matrix를 기준으로 향후 PLC별 `FLOW.JSON` / Business Flow Definition이 어떤 책임을 가져야 하는지 검토한 Boundary Review다.

핵심 결론은 `FLOW.JSON`이 XGT command list가 아니라 PLC별 업무 흐름 정의라는 점이다. `FLOW.JSON`은 무엇을 어떤 조건과 순서로 처리할지 선언할 수 있지만, 실제 PLC read / write, DB query, payload build, ACK / error write는 Flow Executor / Adapter / DB Query / Payload Builder 계층이 수행해야 한다.

Runtime core는 계속 vendor-neutral polling / snapshot state path를 유지한다. Runtime core는 `FLOW.JSON` parser, XGT-specific flow execution, PLC별 address / payload layout / SQL policy를 소유하지 않는다.

이번 작업은 Boundary Review 결과를 closeout 문서로 기록하는 단계다. production code, test code, `FLOW.JSON`, schema, parser, Flow Executor, PilotFlow class, XGT Adapter, DB Query, project reference, source copy, WPF, Contracts, ContextPublisher 수정, commit은 수행하지 않았다.

## 2. Goal

AH-RUNTIME-48의 목표는 AH-RUNTIME-47 Matrix를 바탕으로 `FLOW.JSON` / Business Flow Definition boundary를 검토하는 것이었다.

핵심 질문은 다음이었다.

- `FLOW.JSON`은 무엇을 표현해야 하는가?
- `FLOW.JSON`은 무엇을 표현하면 안 되는가?
- AH-RUNTIME-47 Matrix의 어떤 컬럼이 `FLOW.JSON` schema 후보로 이어지는가?
- Runtime core와 Flow Executor의 경계는 어디인가?
- PLC별 flow variation은 어떻게 표현해야 하는가?

이번 단계에서는 구현하지 않고 `FLOW.JSON`의 책임, 위치, 경계, schema 후보 방향만 검토했다.

## 3. Background

AH-RUNTIME-45에서는 `WorkStartPilotService.RunOnceAsync(...)` 흐름을 Runtime canonical polling path가 아니라 LOTID 기반 business transaction / pilot command flow / process handoff scenario로 판정했다.

AH-RUNTIME-46에서는 Pilot Flow Scenario Matrix의 범위, 컬럼, naming, error write policy, ACK ON / OFF policy, `FLOW.JSON` 연결 후보를 정리했다.

AH-RUNTIME-47에서는 WorkStart verified flow와 사용자 business anchor를 하나의 Pilot Flow Scenario Matrix로 정리했다. 특히 `Source`, `EvidenceLevel`, `PolicyStatus`로 검증 수준과 정책 상태를 분리했다.

AH-RUNTIME-48은 이 matrix를 바탕으로 `FLOW.JSON` / Business Flow Definition의 책임과 Runtime boundary를 검토했다.

고정 원칙은 다음과 같다.

- `FLOW.JSON`은 XGT 명령 목록이 아니다.
- `FLOW.JSON`은 PLC별 Business Flow Definition이다.
- Runtime core는 `FLOW.JSON`을 모르고 vendor-neutral polling / snapshot state path만 유지한다.
- Business flow 실행은 Runtime core 밖의 Flow Executor / Adapter / Handler 계층이 담당한다.
- ContextPublisher 자동 publish는 현재 사용하지 않는다.
- Runtime 작업 기록은 `docs/harness/AH-RUNTIME-xx.md` Closeout을 primary historical record로 사용한다.

## 4. 확인한 AH-RUNTIME-47 Matrix 기준

AH-RUNTIME-47 Matrix는 다음 영역을 기준으로 한다.

- Polling / Request Detection
- WorkStart Verified Flow
- WorkStart Ack-Off / User Business Anchor
- WorkComplete / User Business Anchor
- Common Failure / Future Policy

핵심 컬럼은 다음과 같다.

- `ScenarioId`
- `Area`
- `Given`
- `When`
- `Then`
- `ExpectedStep`
- `ExpectedResult`
- `ExpectedErrorCode`
- `ErrorWriteExpected`
- `ErrorWriteTarget`
- `AckAction`
- `ReturnState`
- `Source`
- `EvidenceLevel`
- `DependencyCategory`
- `PolicyStatus`
- `Notes`

판단:

- Matrix의 모든 컬럼이 runtime `FLOW.JSON` schema에 들어갈 필요는 없다.
- `EvidenceLevel`은 실행 정의가 아니라 검증 근거이므로 authoring matrix / review metadata로 분리하는 것이 좋다.
- `PolicyStatus`는 design-time validation metadata로 유용하다.

## 5. FLOW.JSON 책임 후보

`FLOW.JSON`에 들어갈 수 있는 항목은 다음과 같다.

- `flowId`
- `flowVersion`
- `plcId` 또는 target selector
- trigger signal
- trigger condition
- request detection rule
- lot source
- step order
- action kind
- business condition
- `onSuccess` transition
- `onFailure` transition
- `returnState`
- error code
- error write policy
- ACK action
- payload layout reference
- DB query policy reference
- diagnostics policy reference
- schema version
- policy status metadata

별도 reference로 빼는 편이 좋은 항목은 다음과 같다.

- PLC address binding
- payload layout detail
- SQL text / connection policy
- diagnostics capture shape
- PLC별 override map

runtime execution result에만 있어야 할 항목은 다음과 같다.

- selected LOT ID
- actual request / response hex
- elapsed ms
- reconnect attempt count
- actual handler exception message
- final execution trace

판단:

- `FLOW.JSON`은 업무 흐름과 정책을 선언한다.
- XGT address, DB query, payload layout, ACK / error policy는 reference / policy로 가질 수 있다.
- 실행 결과, raw diagnostics, 실제 exception detail은 definition이 아니라 runtime execution result 또는 diagnostics output에 속한다.

## 6. FLOW.JSON 금지 항목

`FLOW.JSON`에는 아래를 넣지 않는다.

- XGT raw frame
- TCP transport 설정 detail
- `XgtFrameBuilder` logic
- `XgtFrameParser` logic
- SQL 실행 코드
- C# method body
- UI control state
- `MessageBox`
- thread / timer control
- `RuntimeSnapshot` 직접 제어
- `PollingPublishCoordinator` 호출
- `XgtDriverCore` concrete type name
- FakePlc scenario id
- `ChannelPollingTarget` / `ChannelPollingResult` 확장용 business transaction detail

판단:

- `FLOW.JSON`은 실행 가능한 코드가 아니다.
- `FLOW.JSON`은 UI 동작, Runtime snapshot publish, XGT frame 생성, SQL 실행을 직접 표현하지 않는다.
- FakePlc scenario id는 검증 harness metadata이지 production flow definition에 직접 들어갈 항목이 아니다.

## 7. Matrix column과 FLOW.JSON 후보 연결

Matrix column과 `FLOW.JSON` 후보 연결은 다음과 같다.

- `ScenarioId` -> authoring / test metadata의 `scenarioId`, runtime flow에는 선택적
- `Area` -> `flowId` 또는 flow group
- `Given` / `When` -> trigger condition, request detection rule
- `Then` -> expected business action 또는 transition
- `ExpectedStep` -> step id
- `ExpectedErrorCode` -> error policy
- `ErrorWriteExpected` / `ErrorWriteTarget` -> error write policy / target reference
- `AckAction` -> action kind
- `ReturnState` -> transition target
- `DependencyCategory` -> action handler category
- `PolicyStatus` -> design-time validation metadata로 유용
- `EvidenceLevel` -> runtime JSON보다는 authoring matrix / review metadata로 분리 권장

판단:

- Matrix의 모든 컬럼이 `FLOW.JSON` runtime schema에 들어갈 필요는 없다.
- `ScenarioId`, `Source`, `EvidenceLevel`, `Notes`는 authoring / harness / review 문맥에서 더 자연스럽다.
- `PolicyStatus`는 runtime execution에는 필요 없을 수 있으나 schema authoring과 validation 단계에서는 유용하다.
- `ExpectedStep`, `AckAction`, `ReturnState`, error policy 관련 컬럼은 flow definition 후보로 자연스럽게 이어진다.

## 8. PLC별 variation 표현 방식

초기 후보는 PLC별 단일 `FLOW.JSON`이다.

예:

- `flows/PLC-01.flow.json`
- `flows/PLC-02.flow.json`

장점:

- 이해하기 쉽다.
- 신규 PLC 추가 시 파일 하나를 복사 / 수정하기 쉽다.
- 사용자 의도인 "PLC별로 한 곳에서 수정"에 가장 가깝다.

위험:

- 중복이 많아질 수 있다.
- 공통 flow 변경 시 여러 파일 수정이 필요할 수 있다.

장기 후보는 공통 Flow Template + PLC별 Binding이다.

예:

- `flowTemplates/work-start-complete.json`
- `plcBindings/PLC-01.binding.json`
- `plcBindings/PLC-02.binding.json`

Template이 가질 수 있는 항목:

- step order
- condition
- transition
- default policy

Binding이 가질 수 있는 항목:

- PLC address
- LOT offset
- ACK address
- payload layout reference
- DB query policy reference

권장:

- 초기에는 PLC별 단일 `FLOW.JSON`으로 시작한다.
- 단, schema 초안에는 나중에 template + binding으로 분리 가능한 경계를 남긴다.

## 9. Business Flow Definition / Flow Executor / Adapter / Runtime Core 경계

Business Flow Definition 책임:

- 무엇을 어떤 조건과 순서로 처리할지 선언
- request detection
- step order
- ACK ON / OFF
- error policy
- transition / return state

Flow Executor 책임:

- definition을 읽어 실행
- step handler 호출
- result 수집
- branch / transition 처리
- error policy 적용
- ACK / error writer 호출

Adapter / Handler 책임:

- XGT read / write
- DB query
- payload build
- diagnostics capture

Runtime Core 책임:

- vendor-neutral polling target / result / snapshot
- business flow detail을 모름
- `FLOW.JSON` parser를 소유하지 않음

판단:

- Runtime core는 `FLOW.JSON` parser를 소유하지 않는다.
- Runtime core는 XGT-specific flow execution을 소유하지 않는다.
- Runtime core는 PLC별 address / payload layout / SQL policy를 알지 않는다.
- Flow Executor는 Business Flow Definition을 실행하되, 구체적인 XGT / DB / payload 작업은 Adapter / Handler에 위임해야 한다.

## 10. FLOW.JSON과 Runtime Polling 관계

판단:

- `FLOW.JSON`은 business flow layer의 정의다.
- Runtime canonical polling path와 직접 동일하지 않다.
- request detection은 business flow trigger source로 볼 수 있다.
- `PollingCycleCoordinator`와 `FLOW.JSON`이 직접 연결되면 안 된다.
- `ChannelPollingTarget`은 `PlcId`만 갖고, `ChannelPollingResult`는 success / failure, time, failure kind, error message 중심으로 유지한다.
- XGT address, LOTID, DB result, ACK policy를 `ChannelPollingTarget` / `ChannelPollingResult`에 넣지 않는다.

근거:

- 현재 `ChannelPollingTarget`은 vendor-neutral polling target으로 `PlcId`만 가진다.
- 현재 `ChannelPollingResult`는 vendor-neutral polling event result로 success / failure, occurred time, response time, failure kind, error message를 가진다.
- `PollingCycleCoordinator`는 이미 생성된 `ChannelPollingResult` batch를 Runtime publish path로 넘기는 역할에 가깝다.
- business request detection과 flow execution은 Runtime polling result publication과 다른 계층으로 분리해야 한다.

## 11. 후보 A: PLC별 FLOW.JSON 단일 파일

판정:

- 초기 단계에 적절하다.

장점:

- 이해하기 쉽다.
- 신규 PLC 추가 시 파일 하나를 복사 / 수정하기 쉽다.
- 사용자 의도인 "PLC별로 한 곳에서 수정"에 가장 가깝다.

위험:

- 중복이 많아질 수 있다.
- 공통 flow 변경 시 여러 파일 수정이 필요하다.

결론:

- AH-RUNTIME-49 또는 초기 schema draft에는 단일 파일 방식을 기본 모델로 삼는다.
- 단, template 분리 가능성을 열어둔다.

## 12. 후보 B: 공통 Flow Template + PLC별 Binding

판정:

- 장기 방향으로 적절하다.

장점:

- 공통 업무 흐름 재사용이 가능하다.
- PLC별 차이만 binding으로 분리할 수 있다.
- 신규 PLC 추가에 강하다.
- 장기 유지보수에 좋다.

위험:

- 구조가 복잡해진다.
- 초기 구현 부담이 크다.
- template / binding merge 규칙이 필요하다.
- override 우선순위와 validation rule이 필요하다.

결론:

- 장기 목표로 남긴다.
- 초기에는 단일 `FLOW.JSON`에서 binding 성격의 필드를 명확히 표시하는 방식이 좋다.

## 13. 후보 C: Matrix-first, JSON later

판정:

- AH-RUNTIME-48에는 충분하고 적절하다.

장점:

- 성급한 schema 고정을 피한다.
- 업무 흐름 이해를 더 쌓을 수 있다.
- 사용자 / 현장 검토가 쉽다.

위험:

- 코드 구현이 늦어진다.
- JSON 설계가 계속 미뤄질 수 있다.

결론:

- AH-RUNTIME-48에서는 matrix 기반 boundary review가 적절하다.
- AH-RUNTIME-49에서 template / binding boundary를 검토한 뒤 schema draft로 이어가는 것이 좋다.

## 14. 후보 D: JSON schema 초안까지 바로 작성

판정:

- 방향 제안은 가능하지만 실제 schema 파일 작성은 아직 이르다.

장점:

- 다음 구현으로 빠르게 이어질 수 있다.
- 구조가 구체화된다.

위험:

- 아직 미결정 정책이 많다.
- 착공 / 완공 동시 요청, ACK OFF 실패, 완공 ACK 실패, timeout / retry 정책이 아직 흔들린다.
- schema를 너무 빨리 고정할 수 있다.

결론:

- AH-RUNTIME-48에서는 schema 방향까지만 제안한다.
- 실제 draft는 AH-RUNTIME-49 이후가 적절하다.

## 15. 후보 E: C# DSL / fluent builder

판정:

- 보조 도구로는 가능하지만 primary definition으로는 부적절하다.

장점:

- type-safe하다.
- 리팩터링이 쉽다.
- 컴파일 타임 검증이 가능하다.
- JSON parser가 필요 없다.

위험:

- 사용자 의도인 PLC별 JSON 관리와 다르다.
- 비개발자가 수정하기 어렵다.
- 신규 PLC 대응 시 코드 수정이 필요하다.

결론:

- 내부 테스트 / authoring helper 후보는 가능하다.
- primary definition은 `FLOW.JSON` 쪽이 맞다.

## 16. 권장안

권장안:

- 초기 전략은 PLC별 단일 `FLOW.JSON` 개념으로 시작한다.
- 단, schema 초안에는 나중에 template + binding으로 분리 가능한 경계를 남긴다.
- flow definition 안에서 업무 순서와 정책을 표현한다.
- XGT address, payload layout, DB query는 실행 코드가 아니라 reference / binding으로 취급한다.
- Runtime core는 `FLOW.JSON`을 모르게 유지한다.

이유:

- 사용자 의도인 PLC별 `FLOW.JSON` 관리 가능성과 잘 맞는다.
- 신규 PLC 연결 시 수정 지점이 한 곳에 모인다.
- Runtime core vendor-neutral 경계를 유지할 수 있다.
- XGT / DB / payload / ACK 실행 책임을 분리할 수 있다.
- AH-RUNTIME-47 Matrix와 자연스럽게 연결된다.
- 너무 빠른 schema 고정을 피하면서도 다음 schema draft로 이어질 수 있다.
- 초기 구현 난이도와 장기 유지보수 사이 균형이 좋다.

## 17. AH-RUNTIME-49 후보 및 우선순위

추천 우선순위:

1. `FLOW.JSON` Template vs Binding Boundary Review
   - 단일 파일 방식과 template + binding 방식 중 초기 전략을 더 구체화한다.

2. Pilot Flow Schema Draft
   - 실제 JSON schema 초안을 작성한다.
   - 단 parser / executor 구현은 하지 않는다.

3. Flow Executor Boundary Review
   - definition을 어떻게 실행할지 검토한다.

4. Pure Helper Extraction Review
   - `ProcessDataPayloadBuilder` / LOTID extraction 등 선택 이식 후보를 검토한다.

5. Flow Definition Model Skeleton
   - C# record / model로 중간 모델을 정의한다.
   - JSON은 나중에 serialize / deserialize한다.
   - 실제 executor는 아직 구현하지 않는다.

판단 이유:

- schema draft 전에 단일 파일과 template / binding의 초기 전략을 먼저 확정해야 schema가 덜 흔들린다.
- `FLOW.JSON`이 업무 흐름 정의라는 의미를 유지하면서도, PLC별 binding 차이를 장기적으로 흡수할 경계를 먼저 잡아야 한다.

## 18. 제외한 범위

이번 AH-RUNTIME-48에서는 다음을 하지 않았다.

- production code 수정
- test code 수정
- docs / harness 문서 생성 외 작업
- `FLOW.JSON` 생성
- schema 생성
- parser 구현
- Flow Executor 구현
- PilotFlow class 추가
- XGT Adapter 구현
- DB Query 구현
- project reference 추가
- package reference 추가
- source copy
- WPF 수정
- Contracts 수정
- ContextPublisher 수정
- commit

## 19. 실행한 명령

AH-RUNTIME-48 Boundary Review 당시 실행한 명령은 다음과 같다.

현재 repo:

- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-47.md`
- `Get-Content docs\harness\AH-RUNTIME-46.md`
- `Get-Content docs\harness\AH-RUNTIME-45.md`
- `Get-Content docs\harness\AH-RUNTIME-44.md`
- `rg "FLOW.JSON|Business Flow Definition|ScenarioId|EvidenceLevel|PolicyStatus|StartAck|CompleteAck|착공|완공" docs/harness docs/context src tests`
- `rg "ChannelPollingTarget|ChannelPollingResult|PollingCycleCoordinator|RuntimeSnapshot" src tests`
- Runtime polling files read-only 확인
- cognitive context docs read-only 확인

Sibling repo:

- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short`
- `rg -n "WorkStartPilotService|PilotScenarioConfig|ProcessDataPayloadBuilder|LotDataQueryService|WorkStartPilotResult" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- sibling pilot files read-only 확인

AH-RUNTIME-48 closeout 작성 시 실행한 명령:

- `git status --short`
- `Test-Path docs\harness\AH-RUNTIME-48.md`
- `Get-ChildItem docs\harness -Filter AH-RUNTIME-48.md`

작업 후 validation 명령:

- `git diff -- docs/harness/AH-RUNTIME-48.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-48.md`

## 20. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-48 Boundary Review 결과를 `docs/harness/AH-RUNTIME-48.md` closeout 문서로 기록했다.
- `FLOW.JSON`은 XGT command list가 아니라 PLC별 Business Flow Definition이라는 결론을 기록했다.
- Runtime core가 `FLOW.JSON` parser, XGT-specific flow execution, PLC별 address / payload layout / SQL policy를 소유하지 않는 boundary를 기록했다.
- AH-RUNTIME-47 Matrix 기준 영역과 핵심 컬럼을 기록했다.
- `FLOW.JSON` 책임 후보, 금지 항목, Matrix column과 `FLOW.JSON` 후보 연결을 기록했다.
- PLC별 단일 `FLOW.JSON`과 공통 Flow Template + PLC별 Binding 후보를 비교했다.
- Business Flow Definition / Flow Executor / Adapter / Runtime Core 책임을 분리해 기록했다.
- `FLOW.JSON`과 Runtime polling path가 직접 동일하지 않으며 `PollingCycleCoordinator`와 직접 연결되면 안 된다는 판단을 기록했다.
- 후보 A / B / C / D / E를 검토하고 권장안을 기록했다.
- AH-RUNTIME-49 후보 및 우선순위를 기록했다.
- production code, test code, `FLOW.JSON`, schema, parser, Flow Executor, PilotFlow class, XGT Adapter, DB Query, project reference, source copy, WPF, Contracts, ContextPublisher 수정, commit은 수행하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
