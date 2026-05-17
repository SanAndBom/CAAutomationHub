# AH-RUNTIME-46 Closeout

## 1. Summary

AH-RUNTIME-46은 Pilot Flow Scenario Matrix를 만들기 전에 matrix의 범위, 컬럼, naming, error write policy, ACK ON / OFF policy, `FLOW.JSON` 연결 정보를 먼저 결정한 read-only Boundary Review다.

핵심 결론은 `WorkStartPilotService.RunOnceAsync(...)`로 확인된 WorkStart verified flow와 사용자 business anchor를 한 matrix에 함께 담되, `Source`, `EvidenceLevel`, `PolicyStatus`로 검증 수준과 정책 상태를 분리하는 것이다.

이 방식은 기존 코드로 확인된 착공 데이터 조회 / 전송 / Start ACK ON 흐름과 아직 코드로 직접 분석되지 않은 착공ACK OFF / 완공ACK ON / 완공ACK OFF / 대기 복귀 흐름을 동시에 보존한다. 또한 후속 `FLOW.JSON` / Business Flow Definition이 XGT 명령 목록이 아니라 업무 흐름 정의로 이어지도록 한다.

이번 작업은 조사와 closeout 기록만 수행했다. production code, test code, scenario matrix 별도 문서, scenario JSON, `FLOW.JSON`, schema, parser, Flow Executor, PilotFlow class, adapter, project reference, source copy, XGT / FakePlc / XgtChannelRunner 연결, WPF / Contracts / ContextPublisher 수정, commit은 수행하지 않았다.

## 2. Goal

AH-RUNTIME-46의 목표는 Pilot Flow Scenario Matrix 설계 전 Boundary Review 결과를 고정하는 것이다.

핵심 질문은 다음이었다.

- 착공 / 완공 업무 흐름을 향후 `FLOW.JSON` / Business Flow Definition으로 옮기기 전에 어떤 scenario matrix로 고정해야 하는가?
- 어떤 시나리오를 matrix에 포함해야 하는가?
- 각 scenario를 given / when / then, expected step, expected error code, ACK action, return state로 어떻게 표현해야 하는가?
- error write를 시도해야 하는 failure와 시도하지 않아야 하는 failure를 어떻게 분리해야 하는가?
- 기존 `WorkStartPilotService.RunOnceAsync(...)` 분석 범위와 아직 분석되지 않은 착공ACK OFF / 완공ACK ON / 완공ACK OFF 흐름을 어떻게 구분해야 하는가?
- `FLOW.JSON` schema로 넘어가기 위해 scenario matrix가 어떤 정보를 포함해야 하는가?

이번 단계는 구현이 아니라 Boundary Review다. 따라서 AH-RUNTIME-46에서는 matrix 구조와 항목을 제안하고, 실제 Pilot Flow Scenario Matrix 문서 작성은 AH-RUNTIME-47 후보로 남긴다.

## 3. Background

AH-RUNTIME-45에서는 `WorkStartPilotService.RunOnceAsync(...)` 흐름을 Runtime polling path가 아니라 LOTID 기반 business transaction / pilot command flow / process handoff scenario로 판정했다.

또한 다음 방향을 고정했다.

- `WorkStartPilotService.RunOnceAsync(...)`는 기존 hard-coded pilot flow다.
- AH-RUNTIME-45 Scenario Boundary는 업무 흐름 계약이다.
- 향후 `FLOW.JSON`은 PLC별 업무 흐름 정의다.
- Flow Executor / Adapter / DB Query / Payload Builder는 실제 실행 계층이다.
- Runtime Core는 vendor-neutral state / snapshot을 유지한다.

특히 아래 원칙을 유지한다.

- `FLOW.JSON`은 XGT 명령 목록이 아니다.
- `FLOW.JSON`은 업무 흐름 정의다.
- XGT address, DB query, payload layout, ACK / error policy는 포함될 수 있지만 실행은 Flow Executor와 Adapter 계층이 담당한다.
- Runtime Core에 `FLOW.JSON` parser, XGT-specific flow execution, PLC별 address / payload layout / SQL policy를 넣지 않는다.
- `ChannelPollingTarget`과 `ChannelPollingResult`에 business transaction detail을 넣지 않는다.

## 4. 확인한 기존 AH-RUNTIME-45 / WorkStart flow 근거

확인한 현재 repo 문서:

- `docs/harness/AH-RUNTIME-45.md`
- `docs/harness/AH-RUNTIME-44.md`
- `docs/harness/AH-RUNTIME-42.md`

확인한 Runtime 경계:

- `ChannelPollingTarget`
- `ChannelPollingResult`
- `ChannelPollingFailureKind`
- `RuntimeProjectReferenceBoundaryTests`

확인한 sibling repo 파일:

- `XgtChannelRunner\Services\WorkStartPilotService.cs`
- `XgtChannelRunner\Models\PilotScenarioConfig.cs`
- `XgtChannelRunner\Models\WorkStartPilotResult.cs`
- `XgtChannelRunner\Services\ProcessDataPayloadBuilder.cs`
- `XgtChannelRunner\Services\LotDataQueryService.cs`
- `tests\AutomationHub.XgtChannelRunner.Tests\Services\ProcessDataPayloadBuilderTests.cs`
- 관련 `README.md`

기존 코드 기준 WorkStart step / error anchor:

| Scenario | ExpectedStep | ExpectedErrorCode | ErrorWriteExpected |
| --- | --- | --- | --- |
| 정상 착공 | `completed` | 없음 | No |
| read NAK / malformed | `group-read` | `1101` | No |
| read parse / empty payload | `group-read-parse` | `1102` | No |
| start signal inactive | `start-signal` | `1200` | No |
| LOT ID empty | `lotid` | `2201` | Yes |
| DB exception | `db-query` | `2300` | Yes |
| DB not found | `db-query` | `2301` | Yes |
| DB multiple rows | `db-query` | `2302` | Yes |
| DB other failed result | `db-query` | `2303` | Yes |
| payload build exception | `payload-build` | `2400` | Yes |
| bulk write failure | `bulk-write` | `2501` | Yes |
| ACK write failure | `ack-write` | `2601` | Yes |
| unexpected exception | `exception` | `2999` | No |

주의:

- existing tests가 직접 확인하는 것은 happy path, DB not found, bulk write NAK, transport exception, payload packing 일부다.
- 나머지는 코드 경로로 확인된 behavior다.
- 따라서 matrix에서는 `VerifiedByExistingCode`와 `ExistingTest` / `CodePath` 수준을 분리하는 편이 좋다.

## 5. Matrix에 포함해야 할 범위

Pilot Flow Scenario Matrix는 다음 세 층을 포함해야 한다.

1. Polling / request detection anchor

- 착공요청 ON
- 완공요청 ON
- LOTID 관측

2. WorkStart verified flow

- LOTID read
- DB query
- payload build
- bulk write
- Start ACK ON

3. User business anchor / future analysis

- 착공요청 OFF 감지
- Start ACK OFF
- 완공요청 ON
- Complete ACK ON
- 완공요청 OFF 감지
- Complete ACK OFF
- 대기 복귀

판단:

- `WorkStartPilotService.RunOnceAsync(...)` 직접 분석 범위는 착공요청 ON 이후 착공 데이터 조회 / 전송 / 착공ACK ON까지다.
- ACK OFF와 완공 flow는 `PilotScenarioConfig`에 일부 anchor가 있으나 실행 코드로는 아직 정식 분석 / 구현된 흐름이 아니다.
- 따라서 이 항목들은 `FutureAnalysisRequired`로 구분해야 한다.

## 6. Matrix row 컬럼 후보

권장 컬럼:

- `ScenarioId`
- `Area`
  - `Polling`
  - `WorkStart`
  - `WorkStartAckOff`
  - `WorkComplete`
  - `WorkCompleteAckOff`
  - `CommonFailure`
- `Given`
- `When`
- `Then`
- `ExpectedStep`
- `ExpectedResult`
- `ExpectedErrorCode`
- `ErrorWriteExpected`
- `ErrorWriteTarget`
- `AckAction`
  - `None`
  - `StartAckOn`
  - `StartAckOff`
  - `CompleteAckOn`
  - `CompleteAckOff`
- `ReturnState`
  - `Waiting`
  - `WaitingStartRequestOff`
  - `WaitingCompleteRequestOff`
  - `Failed`
  - `Completed`
- `Source`
  - `VerifiedByExistingCode`
  - `UserBusinessAnchor`
  - `FutureAnalysisRequired`
- `EvidenceLevel`
  - `CodePath`
  - `ExistingTest`
  - `BusinessAnchor`
  - `FutureAnalysis`
- `DependencyCategory`
  - `XGT operation`
  - `DB query`
  - `Payload builder`
  - `Business decision`
  - `Flow control`
  - `Diagnostics`
- `PolicyStatus`
  - `Defined`
  - `Inherited`
  - `MissingPolicy`
  - `NeedsDecision`
- `Notes`

추가 판단:

- `EvidenceLevel`과 `PolicyStatus`를 추가하는 것이 좋다.
- 이 두 컬럼은 검증된 사실과 아직 정책 결정이 필요한 항목을 구분하는 데 필요하다.
- `Source`는 row의 출처를 표시하고, `EvidenceLevel`은 검증 강도를 표시하며, `PolicyStatus`는 error / ACK / return policy의 확정 여부를 표시한다.

## 7. ScenarioId naming 규칙

권장 규칙:

- 일반 시나리오: `AREA-CONDITION-EXPECTED`
- 실패 시나리오: `AREA-FAILURE-ERRORCODE`

권장 예:

- `POLL-START-ON`
- `POLL-COMPLETE-ON`
- `START-HAPPY-PATH`
- `START-READ-NAK-1101`
- `START-READ-PARSE-1102`
- `START-SIGNAL-INACTIVE-1200`
- `START-LOTID-EMPTY-2201`
- `START-DB-EXCEPTION-2300`
- `START-DB-NOT-FOUND-2301`
- `START-DB-MULTIPLE-2302`
- `START-DB-FAILED-2303`
- `START-PAYLOAD-BUILD-2400`
- `START-BULK-WRITE-2501`
- `START-ACK-WRITE-2601`
- `START-UNEXPECTED-2999`
- `START-REQ-OFF-ACK-OFF`
- `COMPLETE-REQ-ON-ACK-ON`
- `COMPLETE-REQ-OFF-ACK-OFF`

판단:

- `START-PARSE-EMPTY-1102`보다는 실제 step과 맞춘 `START-READ-PARSE-1102`가 더 좋다.
- ScenarioId는 PLC별 flow variation이 추가되어도 grouping이 깨지지 않도록 Area와 condition을 앞에 둔다.

## 8. Error write policy

Matrix에서 error write는 독립 policy로 반드시 분리해야 한다.

현재 코드 기준 error write 수행:

- `2201`
- `2300`
- `2301`
- `2302`
- `2303`
- `2400`
- `2501`
- `2601`

Error write target:

- `%DB11410`

Policy:

- best-effort
- write 결과는 최종 result에 반영하지 않음

현재 코드 기준 error write 미수행:

- `1101`
- `1102`
- `1200`
- `2999`

Matrix row마다 아래 컬럼을 둬야 한다.

- `ErrorWriteExpected`
- `ErrorWriteTarget`
- `PolicyStatus`

아직 정책이 없는 항목:

- ACK OFF 실패
- 완공ACK ON 실패
- 완공ACK OFF 실패

이 항목들은 `FutureAnalysisRequired` + `MissingPolicy`로 표시해야 한다.

## 9. ACK ON / OFF policy

ACK action은 같은 action family로 보되, action kind는 분리한다.

Action family:

- `AckWrite`

Action kind:

- `StartAckOn`
- `StartAckOff`
- `CompleteAckOn`
- `CompleteAckOff`

착공 흐름:

- Start request ON + data handoff success -> `StartAckOn`
- Start request OFF observed -> `StartAckOff`
- ReturnState: `WaitingStartRequestOff` -> `Waiting`

완공 흐름:

- Complete request ON -> `CompleteAckOn`
- Complete request OFF observed -> `CompleteAckOff`
- ReturnState: `WaitingCompleteRequestOff` -> `Waiting`

판단:

- ACK OFF는 request OFF 감지 이후 별도 scenario로 두는 것이 좋다.
- 이렇게 해야 업무 데이터 전송 성공과 handshake 종료 / 대기 복귀가 섞이지 않는다.
- ACK OFF 실패, 완공ACK ON 실패, 완공ACK OFF 실패의 error policy는 아직 `MissingPolicy`로 남겨야 한다.

## 10. FLOW.JSON schema로 이어질 정보

이번 단계에서 schema를 만들지는 않는다.

다만 matrix column은 다음 schema 후보로 연결되어야 한다.

| Matrix information | Schema candidate |
| --- | --- |
| `Area` / flow grouping | `flowId` |
| `ScenarioId` | `scenarioId` |
| `Given` / `When` | trigger signal |
| `Given` / `When` | trigger condition |
| Polling / WorkStart LOTID extraction | lot source |
| `ExpectedStep` / `DependencyCategory` | steps |
| `AckAction` | action kind |
| `Then` / `ReturnState` | onSuccess transition |
| `Then` / `ReturnState` | onFailure transition |
| `ExpectedErrorCode` | error code |
| `ErrorWriteExpected` | error write policy |
| Payload builder dependency | payload layout reference |
| DB query dependency | db query policy reference |

핵심:

- `FLOW.JSON`은 XGT command list가 아니라 business flow definition이다.
- XGT address, DB query, payload layout은 reference / policy로 가질 수 있지만 실행은 Flow Executor / Adapter / DB Query / Payload Builder가 맡아야 한다.
- Runtime Core는 vendor-neutral state / snapshot 중심을 유지하고, PLC별 업무 flow 실행 세부사항을 알지 않는다.

## 11. 후보 A: Matrix 구조만 보고

판정:

- 안전하지만 다음 단계 산출물이 늦어진다.

내용:

- AH-RUNTIME-46에서는 scenario matrix의 컬럼 / 범위 / naming / policy만 확정한다.
- 실제 docs / harness matrix 문서는 AH-RUNTIME-47에서 작성한다.

장점:

- Boundary Review 원칙에 충실하다.
- 성급한 문서 생성을 방지한다.

위험:

- 실제 matrix 작성이 한 단계 늦어진다.

## 12. 후보 B: 바로 Matrix 문서 작성

판정:

- 이번 지시와는 맞지 않는다.

이유:

- 결과물은 빠르게 남을 수 있다.
- 하지만 AH-RUNTIME-46 지시는 read-only Boundary Review이며, 문서 생성을 금지했다.
- 따라서 AH-RUNTIME-46 Boundary Review에서는 제외해야 한다.

보정:

- AH-RUNTIME-46 ACCEPT 이후 closeout 문서 작성은 허용된 별도 단계다.
- 실제 matrix 문서 작성은 AH-RUNTIME-47에서 수행하는 것이 적절하다.

## 13. 후보 C: Matrix와 FLOW.JSON Boundary를 함께 다룸

판정:

- 일부만 다루는 것이 적절하다.

판단:

- Matrix column이 `FLOW.JSON` 후보로 어떻게 이어지는지까지는 이번 보고 범위에 포함할 수 있다.
- 하지만 schema shape, JSON 파일, parser, executor 책임 확정까지 가면 범위 초과다.

## 14. 후보 D: 기존 WorkStart Verified Flow만 먼저 Matrix화

판정:

- 정확도는 높지만 업무 전체 흐름을 놓칠 위험이 있다.

장점:

- `WorkStartPilotService` 기준이라 가장 단단하다.
- 범위 통제가 쉽다.

위험:

- 착공ACK OFF / 완공ACK ON/OFF / 대기 복귀가 다시 뒤로 밀릴 수 있다.
- `FLOW.JSON` 방향성이 부분 flow에 갇힐 수 있다.

## 15. 후보 E: WorkStart Verified Flow + 사용자 Business Anchor 함께 Matrix화

판정:

- 권장

판단:

- 한 matrix 안에서 `Source`와 `EvidenceLevel`로 구분하면 기존 코드 evidence와 사용자 business anchor를 동시에 보존할 수 있다.
- 신규 PLC별 flow variation도 이 구조가 가장 잘 받는다.

장점:

- 기존 WorkStart code evidence를 잃지 않는다.
- 착공 / 완공 / ACK OFF / 대기 복귀 전체 업무 흐름을 누락하지 않는다.
- 아직 검증되지 않은 항목을 `FutureAnalysisRequired`로 명확히 분리할 수 있다.
- Runtime core vendor-neutral boundary를 유지한다.
- `FLOW.JSON` / Business Flow Definition으로 자연스럽게 이어진다.
- 신규 PLC 연결 시 trigger, LOT source, payload layout, DB policy, ACK / error policy 변경 지점을 한 matrix에서 볼 수 있다.

위험:

- 일부 row는 아직 코드 검증 evidence가 없다.
- future analysis 항목이 섞여 matrix가 커질 수 있다.

대응:

- `Source`, `EvidenceLevel`, `PolicyStatus`로 검증 수준과 정책 상태를 분리한다.
- AH-RUNTIME-47에서는 matrix row 작성 시 verified row와 future-analysis row를 명확히 구분한다.

## 16. 권장안

권장안:

- 후보 E를 채택한다.
- WorkStart verified flow와 사용자 business anchor를 한 matrix에 함께 담는다.
- 단, `Source` / `EvidenceLevel` / `PolicyStatus`로 검증 수준과 정책 상태를 분리한다.

이유:

- 기존 코드 검증 evidence를 보존한다.
- 사용자 business flow 전체를 누락하지 않는다.
- `FLOW.JSON` / Business Flow Definition으로 자연스럽게 이어진다.
- 아직 검증되지 않은 항목을 명확히 표시할 수 있다.
- Runtime core vendor-neutral boundary를 지킨다.
- 다음 단계에서 실행 가능한 문서 / harness로 이어질 수 있다.
- 신규 PLC 연결 시 수정해야 할 맥락을 한 곳에 모을 수 있다.

## 17. AH-RUNTIME-47 후보 및 우선순위

추천 우선순위:

1. Pilot Flow Scenario Matrix 문서 작성
   - AH-RUNTIME-46에서 정한 구조로 실제 matrix 작성

2. Business Flow Definition / `FLOW.JSON` Boundary Review
   - matrix를 기반으로 `FLOW.JSON` 책임 / 위치 / schema 방향 검토

3. Pilot Flow Schema Draft
   - schema 초안만 작성
   - JSON / parser 구현은 별도

4. Flow Executor Boundary Review
   - executor, adapter, DB query, payload builder 책임 분리

5. Pure Helper Extraction Review
   - `ProcessDataPayloadBuilder`, LOTID extraction, error mapping 이식 후보 검토

## 18. 제외한 범위

이번 AH-RUNTIME-46에서는 다음을 하지 않았다.

- production code 수정
- test code 수정
- docs / harness 문서 생성 외 작업
- scenario matrix 문서 생성
- scenario JSON 생성
- `FLOW.JSON` 생성
- schema 생성
- parser 구현
- Flow Executor 구현
- PilotFlow class 추가
- adapter 구현
- project / solution / csproj 수정
- project reference 추가
- package reference 추가
- source copy
- XgtDriverCore 연결
- FakePlc 연결
- XgtChannelRunner 연결
- WPF 수정
- Contracts 수정
- ContextPublisher 수정
- commit

이번 closeout 단계에서는 `docs/harness/AH-RUNTIME-46.md` 문서 생성만 수행했다.

## 19. 실행한 명령

AH-RUNTIME-46 Boundary Review 당시 실행한 명령:

현재 repo:

- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-45.md`
- `Get-Content docs\harness\AH-RUNTIME-44.md`
- `Get-Content docs\harness\AH-RUNTIME-42.md`
- `rg "FLOW.JSON|Business Flow Definition|착공|완공|ACK|Scenario Matrix|WorkStart" docs/harness docs/context src tests`
- `rg --files src\CAAutomationHub.Runtime\Polling`
- `Get-Content tests\CAAutomationHub.Runtime.Tests\RuntimeProjectReferenceBoundaryTests.cs`
- `Get-Content src\CAAutomationHub.Runtime\Polling\ChannelPollingTarget.cs`
- `Get-Content src\CAAutomationHub.Runtime\Polling\ChannelPollingResult.cs`
- `Get-Content src\CAAutomationHub.Runtime\Polling\ChannelPollingFailureKind.cs`

Sibling repo:

- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short`
- `rg -n "WorkStartPilotService|RunOnceAsync|TryWriteErrorCodeAsync|ErrorCode|Ack|LOT|Lot|StartSignal|ProcessDataPayloadBuilder" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- WorkStart / config / result / payload / query / test / readme files `Get-Content`

AH-RUNTIME-46 closeout 작성 시 실행한 명령:

- `git status --short`
- `Test-Path docs\harness\AH-RUNTIME-46.md`
- `Get-ChildItem docs\harness -Filter AH-RUNTIME-46.md`
- `git diff -- docs/harness/AH-RUNTIME-46.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-46.md`

테스트 / 빌드는 이번 지시가 조사 및 문서화 전용이므로 실행하지 않았다.

## 20. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-46 Boundary Review 결과를 `docs/harness/AH-RUNTIME-46.md` closeout 문서로 기록했다.
- WorkStart verified flow와 사용자 business anchor를 한 matrix에 함께 담되 `Source`, `EvidenceLevel`, `PolicyStatus`로 구분하는 권장안을 기록했다.
- 기존 `WorkStartPilotService.RunOnceAsync(...)` 기준 step / error code / error write 여부를 기록했다.
- existing test coverage와 code path evidence를 분리해야 한다는 주의사항을 기록했다.
- Matrix 범위를 Polling / request detection, WorkStart verified flow, User business anchor / future analysis 세 층으로 기록했다.
- Matrix row 컬럼 후보, ScenarioId naming 규칙, error write policy, ACK ON / OFF policy를 기록했다.
- `FLOW.JSON` schema 후보로 이어질 정보를 기록하되 schema / JSON / parser / executor 구현으로 넘어가지 않았다.
- 후보 A / B / C / D / E를 검토하고 후보 E를 권장안으로 기록했다.
- AH-RUNTIME-47 후보 및 우선순위를 기록했다.
- production code, test code, scenario matrix 별도 문서, scenario JSON, `FLOW.JSON`, schema, parser, Flow Executor, PilotFlow class, adapter, project reference, source copy, XGT / FakePlc / XgtChannelRunner 연결, WPF / Contracts / ContextPublisher 수정, commit은 수행하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
