# AH-RUNTIME-47 Pilot Flow Scenario Matrix

## 1. Summary

AH-RUNTIME-47은 AH-RUNTIME-45 / AH-RUNTIME-46에서 정리한 Pilot Flow Boundary Review 결과를 실제 Scenario Matrix 문서로 고정한다.

핵심은 `WorkStartPilotService.RunOnceAsync(...)`에서 확인된 WorkStart verified flow와 사용자가 정의한 착공 / 완공 / ACK ON / ACK OFF / 대기 복귀 business anchor를 하나의 matrix에 함께 담는 것이다.

단, 모든 row가 같은 검증 수준을 갖는 것은 아니므로 `Source`, `EvidenceLevel`, `PolicyStatus`로 기존 코드 evidence, 사용자 업무 anchor, future analysis, 미결정 정책을 분리한다.

이번 작업은 문서 작성만 수행했다. production code, test code, JSON, `FLOW.JSON`, schema, parser, Flow Executor, adapter, project / solution 변경은 수행하지 않았다.

## 2. Goal

AH-RUNTIME-47의 목표는 `docs/harness/AH-RUNTIME-47.md`에 Pilot Flow Scenario Matrix를 작성하는 것이다.

이 matrix는 향후 `FLOW.JSON` / Business Flow Definition / Flow Executor 설계의 기준 계약으로 사용될 수 있어야 한다.

이번 문서는 실행 구현이 아니라 업무 흐름 시나리오 기준 문서다.

## 3. Background

AH-RUNTIME-45에서는 `WorkStartPilotService.RunOnceAsync(...)` 흐름을 Runtime polling path가 아니라 LOTID 기반 business transaction / pilot command flow / process handoff scenario로 판정했다.

AH-RUNTIME-46에서는 Pilot Flow Scenario Matrix의 범위, 컬럼, naming, error write policy, ACK ON / OFF policy, `FLOW.JSON` 연결 후보를 정리했다.

AH-RUNTIME-46의 핵심 결론은 다음과 같다.

- WorkStart verified flow와 사용자 business anchor를 하나의 matrix에 함께 담는다.
- 단, `Source`, `EvidenceLevel`, `PolicyStatus`로 검증 수준과 정책 상태를 분리한다.
- `FLOW.JSON`은 XGT 명령 목록이 아니라 업무 흐름 정의다.
- XGT address, DB query, payload layout, ACK / error policy는 reference / policy로 포함될 수 있지만 실행은 Flow Executor와 Adapter 계층이 담당한다.

## 4. Matrix 작성 원칙

1. WorkStart verified flow와 사용자 business anchor를 하나의 matrix에 함께 담는다.
2. 검증 수준과 정책 상태를 구분한다.
3. 기존 `WorkStartPilotService.RunOnceAsync(...)`에서 확인된 흐름은 `Source = VerifiedByExistingCode` 또는 `EvidenceLevel = CodePath / ExistingTest`로 표시한다.
4. 사용자가 정의한 착공ACK OFF, 완공ACK ON/OFF, 대기 복귀 흐름은 `Source = UserBusinessAnchor` 또는 `EvidenceLevel = BusinessAnchor / FutureAnalysis`로 표시한다.
5. 아직 error code, retry, error write 여부가 정해지지 않은 항목은 `PolicyStatus = MissingPolicy` 또는 `NeedsDecision`으로 표시한다.
6. Matrix는 향후 `FLOW.JSON` schema의 기초 자료가 되어야 한다.
7. Matrix는 XGT 명령 목록이 아니라 업무 흐름 시나리오 목록이다.

## 5. Matrix 컬럼 정의

| Column | Meaning |
| --- | --- |
| `ScenarioId` | 시나리오를 식별하는 안정적인 ID다. |
| `Area` | 시나리오가 속한 영역이다. 예: `Polling`, `WorkStart`, `WorkStartAckOff`, `WorkComplete`, `WorkCompleteAckOff`, `CommonFailure`. |
| `Given` | 시나리오 시작 전 조건이다. |
| `When` | 트리거 또는 발생 이벤트다. |
| `Then` | 기대되는 업무 동작이다. |
| `ExpectedStep` | 기존 코드 또는 향후 Flow Executor 기준의 step 이름이다. |
| `ExpectedResult` | 기대 결과다. 예: `Success`, `Failure`, `NoAction`, `NeedsDecision`. |
| `ExpectedErrorCode` | 실패 시 기대되는 업무 error code다. 없으면 `None` 또는 `N/A`로 표시한다. |
| `ErrorWriteExpected` | error code를 PLC에 write해야 하는지 여부다. |
| `ErrorWriteTarget` | error code write 대상 주소다. 현재 WorkStart verified flow 기준은 `%DB11410`이다. |
| `AckAction` | ACK 관련 동작이다. 예: `None`, `StartAckOn`, `StartAckOff`, `CompleteAckOn`, `CompleteAckOff`. |
| `ReturnState` | 시나리오 종료 후 기대 상태다. |
| `Source` | 이 시나리오가 기존 코드에서 확인된 것인지, 사용자 업무 anchor인지 구분한다. |
| `EvidenceLevel` | `ExistingTest`, `CodePath`, `BusinessAnchor`, `FutureAnalysis` 중 하나로 구분한다. |
| `DependencyCategory` | 시나리오가 의존하는 책임 범주다. 예: `XGT operation`, `DB query`, `Payload builder`, `Business decision`, `Flow control`, `Diagnostics`. |
| `PolicyStatus` | `Defined`, `Inherited`, `MissingPolicy`, `NeedsDecision` 중 하나로 구분한다. |
| `Notes` | 검증 범위, 미결정 사항, 후속 설계 메모를 기록한다. |

## 6. ScenarioId Naming 규칙

일반 시나리오:

```text
AREA-CONDITION-EXPECTED
```

실패 시나리오:

```text
AREA-FAILURE-ERRORCODE
```

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

## 7. Pilot Flow Scenario Matrix

### 7.1 Polling / Request Detection

| ScenarioId | Area | Given | When | Then | ExpectedStep | ExpectedResult | ExpectedErrorCode | ErrorWriteExpected | ErrorWriteTarget | AckAction | ReturnState | Source | EvidenceLevel | DependencyCategory | PolicyStatus | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `POLL-IDLE-NO-REQUEST` | `Polling` | 착공요청과 완공요청이 모두 OFF | polling tick에서 request signal을 관측함 | 업무 flow를 시작하지 않고 대기 유지 | `request-detection` | `NoAction` | `None` | No | N/A | `None` | `Waiting` | `UserBusinessAnchor` | `BusinessAnchor` | `Flow control` | `Defined` | 대기 상태의 기본 anchor다. |
| `POLL-START-ON` | `Polling` | 착공요청 ON, 완공요청 OFF | polling tick에서 착공요청과 LOTID를 관측함 | WorkStart flow 후보로 전이 | `request-detection` | `Success` | `None` | No | N/A | `None` | `WorkStartRequested` | `UserBusinessAnchor` | `BusinessAnchor` | `Flow control` | `Defined` | 기존 WorkStart verified flow로 이어지는 trigger anchor다. |
| `POLL-COMPLETE-ON` | `Polling` | 착공요청 OFF, 완공요청 ON | polling tick에서 완공요청과 LOTID를 관측함 | WorkComplete flow 후보로 전이 | `request-detection` | `Success` | `None` | No | N/A | `None` | `WorkCompleteRequested` | `UserBusinessAnchor` | `BusinessAnchor` | `Flow control` | `Defined` | 완공 flow는 아직 기존 코드 분석 범위 밖이다. |
| `POLL-BOTH-REQUESTS-ON` | `Polling` | 착공요청과 완공요청이 동시에 ON | polling tick에서 두 request를 함께 관측함 | 우선순위 또는 error 정책 결정 필요 | `request-detection` | `NeedsDecision` | `TBD` | TBD | TBD | `None` | `NeedsDecision` | `UserBusinessAnchor` | `FutureAnalysis` | `Flow control` | `NeedsDecision` | 착공 우선, 완공 우선, PLC별 priority, error 처리 중 하나를 결정해야 한다. |

### 7.2 WorkStart Verified Flow

| ScenarioId | Area | Given | When | Then | ExpectedStep | ExpectedResult | ExpectedErrorCode | ErrorWriteExpected | ErrorWriteTarget | AckAction | ReturnState | Source | EvidenceLevel | DependencyCategory | PolicyStatus | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `START-HAPPY-PATH` | `WorkStart` | 착공요청 ON, 유효 LOTID, DB row 1건, payload build 가능, bulk write 가능 | WorkStart flow 실행 | 착공 데이터를 PLC에 write하고 Start ACK를 ON | `completed` | `Success` | `None` | No | N/A | `StartAckOn` | `WaitingStartRequestOff` | `VerifiedByExistingCode` | `ExistingTest` | `Flow control` | `Defined` | `WorkStartPilotService.RunOnceAsync(...)`의 verified success anchor다. |
| `START-READ-NAK-1101` | `WorkStart` | 착공 block read 대상이 설정됨 | PLC read 응답이 NAK / malformed / classified failure | flow 실패, error write 없음 | `group-read` | `Failure` | `1101` | No | N/A | `None` | `Failed` | `VerifiedByExistingCode` | `CodePath` | `XGT operation` | `Defined` | 현재 코드 기준 `1101`은 error write를 수행하지 않는다. |
| `START-READ-PARSE-1102` | `WorkStart` | PLC read 응답을 수신함 | parsed read response가 ACK가 아니거나 variable block / data가 없음 | flow 실패, error write 없음 | `group-read-parse` | `Failure` | `1102` | No | N/A | `None` | `Failed` | `VerifiedByExistingCode` | `CodePath` | `XGT parser / adapter` | `Defined` | parse failure는 business error code만 반환하고 PLC error write는 하지 않는다. |
| `START-SIGNAL-INACTIVE-1200` | `WorkStart` | read bytes가 존재함 | start signal word가 inactive이거나 index가 범위를 벗어남 | flow 실패, error write 없음 | `start-signal` | `Failure` | `1200` | No | N/A | `None` | `Failed` | `VerifiedByExistingCode` | `CodePath` | `Business decision` | `Defined` | 요청 신호가 유효하지 않은 상태다. |
| `START-LOTID-EMPTY-2201` | `WorkStart` | start signal active | LOT ID 1과 LOT ID 2가 모두 empty | flow 실패, error code best-effort write 시도 | `lotid` | `Failure` | `2201` | Yes | `%DB11410` | `None` | `Failed` | `VerifiedByExistingCode` | `CodePath` | `Business decision` | `Defined` | `TryWriteErrorCodeAsync` 대상이다. |
| `START-DB-EXCEPTION-2300` | `WorkStart` | selected LOTID가 존재함 | DB query 실행 중 exception 발생 | flow 실패, error code best-effort write 시도 | `db-query` | `Failure` | `2300` | Yes | `%DB11410` | `None` | `Failed` | `VerifiedByExistingCode` | `CodePath` | `DB query` | `Defined` | DB exception path다. |
| `START-DB-NOT-FOUND-2301` | `WorkStart` | selected LOTID가 존재함 | DB query 결과가 not found | flow 실패, error code best-effort write 시도 | `db-query` | `Failure` | `2301` | Yes | `%DB11410` | `None` | `Failed` | `VerifiedByExistingCode` | `ExistingTest` | `DB query` | `Defined` | 기존 테스트 coverage가 있는 anchor로 기록한다. |
| `START-DB-MULTIPLE-2302` | `WorkStart` | selected LOTID가 존재함 | DB query 결과가 multiple rows | flow 실패, error code best-effort write 시도 | `db-query` | `Failure` | `2302` | Yes | `%DB11410` | `None` | `Failed` | `VerifiedByExistingCode` | `CodePath` | `DB query` | `Defined` | DB policy상 단일 row만 success다. |
| `START-DB-FAILED-2303` | `WorkStart` | selected LOTID가 존재함 | DB query가 기타 non-success result를 반환 | flow 실패, error code best-effort write 시도 | `db-query` | `Failure` | `2303` | Yes | `%DB11410` | `None` | `Failed` | `VerifiedByExistingCode` | `CodePath` | `DB query` | `Defined` | not found / multiple / exception 외 DB failure다. |
| `START-PAYLOAD-BUILD-2400` | `WorkStart` | DB query가 process data를 반환함 | payload builder가 exception을 던짐 | flow 실패, error code best-effort write 시도 | `payload-build` | `Failure` | `2400` | Yes | `%DB11410` | `None` | `Failed` | `VerifiedByExistingCode` | `CodePath` | `Payload builder` | `Defined` | payload packing 자체의 일부 검증은 기존 tests가 있으나 이 failure row는 code path 기준이다. |
| `START-BULK-WRITE-2501` | `WorkStart` | payload build 성공 | process payload bulk write가 실패 | flow 실패, error code best-effort write 시도 | `bulk-write` | `Failure` | `2501` | Yes | `%DB11410` | `None` | `Failed` | `VerifiedByExistingCode` | `ExistingTest` | `XGT operation` | `Defined` | bulk write NAK coverage가 있는 anchor로 기록한다. |
| `START-ACK-WRITE-2601` | `WorkStart` | process payload bulk write 성공 | Start ACK write가 실패 | flow 실패, error code best-effort write 시도 | `ack-write` | `Failure` | `2601` | Yes | `%DB11410` | `StartAckOn` | `Failed` | `VerifiedByExistingCode` | `CodePath` | `XGT operation` | `Defined` | ACK ON write 자체가 실패한 path다. |
| `START-UNEXPECTED-2999` | `WorkStart` | WorkStart flow 실행 중 | outer catch에 잡히는 예상 외 exception 발생 | flow 실패, error write 없음 | `exception` | `Failure` | `2999` | No | N/A | `None` | `Failed` | `VerifiedByExistingCode` | `CodePath` | `Diagnostics` | `Defined` | 현재 코드 기준 `2999`는 error write를 수행하지 않는다. |

### 7.3 WorkStart Ack-Off / User Business Anchor

| ScenarioId | Area | Given | When | Then | ExpectedStep | ExpectedResult | ExpectedErrorCode | ErrorWriteExpected | ErrorWriteTarget | AckAction | ReturnState | Source | EvidenceLevel | DependencyCategory | PolicyStatus | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `START-REQ-OFF-ACK-OFF` | `WorkStartAckOff` | Start ACK ON 이후 `WaitingStartRequestOff` 상태 | 착공요청 OFF가 관측됨 | Start ACK를 OFF하고 대기 상태로 복귀 | `start-ack-off` | `Success` | `None` | No | N/A | `StartAckOff` | `Waiting` | `UserBusinessAnchor` | `BusinessAnchor` | `Flow control` | `Defined` | ACK OFF는 request OFF 감지 이후 별도 scenario로 둔다. |
| `START-ACK-OFF-FAILED` | `WorkStartAckOff` | 착공요청 OFF가 관측되고 Start ACK OFF write 필요 | Start ACK OFF write가 실패 | failure 처리 정책 결정 필요 | `start-ack-off` | `Failure` | `TBD` | TBD | TBD | `StartAckOff` | `NeedsDecision` | `UserBusinessAnchor` | `FutureAnalysis` | `XGT operation` | `MissingPolicy` | error code, retry, error write 여부가 아직 없다. |

### 7.4 WorkComplete / User Business Anchor

| ScenarioId | Area | Given | When | Then | ExpectedStep | ExpectedResult | ExpectedErrorCode | ErrorWriteExpected | ErrorWriteTarget | AckAction | ReturnState | Source | EvidenceLevel | DependencyCategory | PolicyStatus | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `COMPLETE-REQ-ON-ACK-ON` | `WorkComplete` | 완공요청 ON이 관측됨 | WorkComplete flow 실행 | Complete ACK를 ON하고 완공요청 OFF 대기 상태로 전이 | `complete-ack-on` | `Success` | `None` | No | N/A | `CompleteAckOn` | `WaitingCompleteRequestOff` | `UserBusinessAnchor` | `BusinessAnchor` | `Flow control` | `Defined` | 완공 flow는 현재 `WorkStartPilotService.RunOnceAsync(...)` 직접 분석 범위가 아니다. |
| `COMPLETE-ACK-ON-FAILED` | `WorkComplete` | 완공요청 ON이 관측되고 Complete ACK ON write 필요 | Complete ACK ON write가 실패 | failure 처리 정책 결정 필요 | `complete-ack-on` | `Failure` | `TBD` | TBD | TBD | `CompleteAckOn` | `NeedsDecision` | `FutureAnalysisRequired` | `FutureAnalysis` | `XGT operation` | `MissingPolicy` | error code, retry, error write 여부가 아직 없다. |
| `COMPLETE-REQ-OFF-ACK-OFF` | `WorkCompleteAckOff` | Complete ACK ON 이후 `WaitingCompleteRequestOff` 상태 | 완공요청 OFF가 관측됨 | Complete ACK를 OFF하고 대기 상태로 복귀 | `complete-ack-off` | `Success` | `None` | No | N/A | `CompleteAckOff` | `Waiting` | `UserBusinessAnchor` | `BusinessAnchor` | `Flow control` | `Defined` | 업무 데이터 전송 성공과 handshake 종료를 섞지 않는다. |
| `COMPLETE-ACK-OFF-FAILED` | `WorkCompleteAckOff` | 완공요청 OFF가 관측되고 Complete ACK OFF write 필요 | Complete ACK OFF write가 실패 | failure 처리 정책 결정 필요 | `complete-ack-off` | `Failure` | `TBD` | TBD | TBD | `CompleteAckOff` | `NeedsDecision` | `FutureAnalysisRequired` | `FutureAnalysis` | `XGT operation` | `MissingPolicy` | error code, retry, error write 여부가 아직 없다. |

### 7.5 Common Failure / Future Policy

| ScenarioId | Area | Given | When | Then | ExpectedStep | ExpectedResult | ExpectedErrorCode | ErrorWriteExpected | ErrorWriteTarget | AckAction | ReturnState | Source | EvidenceLevel | DependencyCategory | PolicyStatus | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `FLOW-UNKNOWN-EXCEPTION` | `CommonFailure` | 임의 flow 실행 중 | 분류되지 않은 exception 발생 | 공통 exception handling 정책 결정 필요 | `flow-exception` | `Failure` | `TBD` | TBD | TBD | `None` | `Failed` | `FutureAnalysisRequired` | `FutureAnalysis` | `Diagnostics` | `NeedsDecision` | WorkStart의 `2999` 정책을 상속할지 별도 공통 정책을 둘지 결정 필요. |
| `FLOW-CONCURRENT-REQUEST` | `CommonFailure` | 하나의 PLC flow가 진행 중 | 다른 request가 동시에 또는 중복 발생 | concurrency / priority 정책 결정 필요 | `flow-concurrency` | `NeedsDecision` | `TBD` | TBD | TBD | `None` | `NeedsDecision` | `FutureAnalysisRequired` | `FutureAnalysis` | `Flow control` | `NeedsDecision` | `POLL-BOTH-REQUESTS-ON`과 함께 우선순위 정책 검토 필요. |
| `FLOW-CONFIG-MISSING` | `CommonFailure` | PLC별 flow definition 필요 | flow config를 찾을 수 없음 | config missing 처리 정책 결정 필요 | `flow-config` | `Failure` | `TBD` | TBD | TBD | `None` | `Failed` | `FutureAnalysisRequired` | `FutureAnalysis` | `Flow control` | `MissingPolicy` | `FLOW.JSON` boundary review 이후 error policy를 정해야 한다. |
| `FLOW-PAYLOAD-LAYOUT-MISSING` | `CommonFailure` | payload build가 필요한 flow | payload layout reference를 찾을 수 없음 | layout missing 처리 정책 결정 필요 | `payload-layout` | `Failure` | `TBD` | TBD | TBD | `None` | `Failed` | `FutureAnalysisRequired` | `FutureAnalysis` | `Payload builder` | `MissingPolicy` | WorkStart `2400`과 같은 code를 쓸지 별도 code를 둘지 확인 필요. |
| `FLOW-DB-POLICY-MISSING` | `CommonFailure` | DB query가 필요한 flow | DB query policy reference를 찾을 수 없음 | DB policy missing 처리 정책 결정 필요 | `db-policy` | `Failure` | `TBD` | TBD | TBD | `None` | `Failed` | `FutureAnalysisRequired` | `FutureAnalysis` | `DB query` | `MissingPolicy` | WorkStart `2300` family와의 관계 확인 필요. |

Future Policy 후보로 남긴 항목:

- `FLOW-CANCELLED-BEFORE-PUBLISH`
- PLC별 branch / fallback missing
- 요청 OFF timeout
- ACK retry exhausted

## 8. Error Write Policy 요약

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

정책:

- best-effort
- write 결과는 최종 result에 반영하지 않음

현재 코드 기준 error write 미수행:

- `1101`
- `1102`
- `1200`
- `2999`

아직 정책이 없는 항목:

- StartAckOff failure
- CompleteAckOn failure
- CompleteAckOff failure
- Both requests ON
- Flow config missing
- Payload layout missing
- DB policy missing

이 항목들은 matrix에서 `MissingPolicy` 또는 `NeedsDecision`으로 표시한다.

## 9. ACK ON / OFF Policy 요약

ACK family:

- `AckWrite`

ACK action kind:

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

중요:

- ACK OFF는 request OFF 감지 이후 별도 scenario로 둔다.
- 업무 데이터 전송 성공과 handshake 종료 / 대기 복귀를 섞지 않는다.

## 10. FLOW.JSON / Business Flow Definition 연결 후보

Matrix column은 향후 `FLOW.JSON` schema 후보로 이어진다.

| Matrix column / information | FLOW.JSON / Business Flow Definition candidate |
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
| `PolicyStatus` | policy status |
| `EvidenceLevel` | evidence level, 단 실제 `FLOW.JSON`에는 들어가지 않을 수도 있음 |

중요:

- `FLOW.JSON`은 XGT command list가 아니다.
- `FLOW.JSON`은 business flow definition이다.
- XGT address, DB query, payload layout은 reference / policy로 가질 수 있지만 실행은 Flow Executor / Adapter / DB Query / Payload Builder가 맡는다.
- Runtime core는 vendor-neutral state / snapshot 중심을 유지하고 PLC별 업무 flow 실행 세부사항을 알지 않는다.

## 11. EvidenceLevel / PolicyStatus 해석

`EvidenceLevel`:

- `ExistingTest`
  - 기존 테스트로 검증된 시나리오

- `CodePath`
  - 기존 코드 경로에서 확인된 시나리오

- `BusinessAnchor`
  - 사용자 업무 설명으로 확인된 시나리오

- `FutureAnalysis`
  - 아직 코드 / 정책 분석이 필요한 시나리오

`PolicyStatus`:

- `Defined`
  - 현재 정책이 명확함

- `Inherited`
  - 기존 flow 또는 유사 정책에서 파생 가능

- `MissingPolicy`
  - 정책이 없음

- `NeedsDecision`
  - 사용자 / 현장 / 설계 결정이 필요함

## 12. 아직 결정되지 않은 정책

아직 결정되지 않은 정책 후보:

- 착공요청과 완공요청이 동시에 ON일 때 우선순위
- 착공ACK OFF 실패 시 error code / retry / error write 여부
- 완공ACK ON 실패 시 error code / retry / error write 여부
- 완공ACK OFF 실패 시 error code / retry / error write 여부
- error write 실패를 최종 result에 반영할지 여부
- ACK OFF 대기 timeout이 필요한지 여부
- 요청 신호가 OFF 되지 않을 때 timeout / alarm 정책
- PLC별 `FLOW.JSON`에서 priority / branch / fallback을 어떻게 표현할지
- DB query policy missing 시 처리
- payload layout missing 시 처리

## 13. 다음 단계 후보

1. AH-RUNTIME-48 Business Flow Definition / `FLOW.JSON` Boundary Review
   - matrix를 기반으로 `FLOW.JSON`의 책임 / 위치 / schema 방향 검토

2. AH-RUNTIME-49 Pilot Flow Schema Draft
   - schema 초안만 작성
   - JSON / parser 구현은 별도

3. AH-RUNTIME-50 Flow Executor Boundary Review
   - executor, adapter, DB query, payload builder 책임 분리

4. AH-RUNTIME-51 Pure Helper Extraction Review
   - `ProcessDataPayloadBuilder`, LOTID extraction, error mapping 이식 후보 검토

번호는 실제 진행 상황에 따라 조정 가능하다.

## 14. 제외한 범위

이번 작업에서 수행하지 않은 것:

- production code 수정
- test code 수정
- JSON 생성
- `FLOW.JSON` 생성
- schema 구현
- parser 구현
- Flow Executor 구현
- PilotFlow class 추가
- adapter 구현
- project / solution / csproj 수정
- project reference 추가
- package reference 추가
- source copy
- `XgtDriverCore` 연결
- `FakePlc` 연결
- `XgtChannelRunner` 연결
- WPF 수정
- Contracts 수정
- ContextPublisher 수정
- commit

## 15. 실행한 명령

AH-RUNTIME-47 작성 전 context 확인:

- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content docs\harness\AH-RUNTIME-45.md`
- `Get-Content docs\harness\AH-RUNTIME-46.md`
- `git status --short`
- `Test-Path docs\harness\AH-RUNTIME-47.md`

AH-RUNTIME-47 작성 후 validation:

- `git diff -- docs/harness/AH-RUNTIME-47.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-47.md`
- `Select-String -Path docs\harness\AH-RUNTIME-47.md -Pattern 'ScenarioId','EvidenceLevel','PolicyStatus','START-HAPPY-PATH','START-READ-NAK-1101','START-REQ-OFF-ACK-OFF','COMPLETE-REQ-ON-ACK-ON','FLOW.JSON','업무 흐름 정의'`
- `git diff --no-index --check -- NUL docs\harness\AH-RUNTIME-47.md`
- `git diff --no-index -- NUL docs\harness\AH-RUNTIME-47.md`

## 16. Self-Check

판정: ACCEPT

이유:

- `docs/harness/AH-RUNTIME-47.md`에 Pilot Flow Scenario Matrix를 작성했다.
- WorkStart verified flow와 사용자 business anchor를 하나의 matrix에 함께 담았다.
- `Source`, `EvidenceLevel`, `PolicyStatus`로 검증 수준과 정책 상태를 분리했다.
- Matrix 컬럼 정의와 ScenarioId naming 규칙을 기록했다.
- `START-HAPPY-PATH`, `START-READ-NAK-1101`, `START-REQ-OFF-ACK-OFF`, `COMPLETE-REQ-ON-ACK-ON`을 포함했다.
- 현재 코드 기준 error write 수행 / 미수행 정책과 `%DB11410` target을 기록했다.
- ACK ON / OFF policy를 Start / Complete action kind로 분리했다.
- `FLOW.JSON` / Business Flow Definition 연결 후보를 기록했다.
- 아직 결정되지 않은 정책과 제외한 범위를 기록했다.
- production code, test code, JSON / schema / parser / executor 구현, commit은 수행하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
