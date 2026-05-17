# AH-RUNTIME-45 Closeout

## 1. Summary

AH-RUNTIME-45는 `WorkStartPilotService.RunOnceAsync(...)`의 검증된 pilot business flow를 `CAAutomationHub` 기준의 Pilot Flow Documentation / Scenario Harness boundary로 정식화하기 위한 read-only Boundary Review다.

핵심 결론은 이 흐름이 Runtime canonical polling path가 아니라 LOTID 기반 business transaction / pilot command flow / process handoff scenario에 가깝다는 점이다. 따라서 `ChannelPollingTarget` / `ChannelPollingResult`에 억지로 흡수하지 않고, Runtime core 밖의 Business Flow Definition과 Flow Executor boundary로 분리해야 한다.

추가 사용자 의견을 반영해, `WorkStartPilotService.RunOnceAsync(...)`는 단일 hard-coded service가 아니라 향후 PLC별 `FLOW.JSON` / Business Flow Definition schema의 원형으로 해석한다. AH-RUNTIME-45의 scenario step / input / output / failure matrix는 후속 PLC별 flow definition 설계의 기초 자료다.

이번 작업은 조사와 closeout 기록만 수행했다. production code, test code, scenario JSON, schema 구현, project reference, source copy, XGT/FakePlc/Runner 연결, WPF 수정, commit, `ContextPublisher` 자동 publish는 수행하지 않았다.

## 2. 확인한 pilot flow 관련 파일

Sibling repo:

- `XgtChannelRunner\MainForm.cs`
- `XgtChannelRunner\MainForm.Designer.cs`
- `XgtChannelRunner\Services\WorkStartPilotService.cs`
- `XgtChannelRunner\Models\PilotScenarioConfig.cs`
- `XgtChannelRunner\Models\WorkStartPilotResult.cs`
- `XgtChannelRunner\Services\ProcessDataPayloadBuilder.cs`
- `XgtChannelRunner\Services\LotDataQueryService.cs`
- `XgtChannelRunner\Models\ContinuousWritePayloadModel.cs`
- `XgtChannelRunner\Models\LotProcessData.cs`
- `XgtChannelRunner\Models\PlcWriteFieldValue.cs`
- `XgtChannelRunner\Services\README.md`
- `XgtChannelRunner\Models\README.md`
- `tests\AutomationHub.XgtChannelRunner.Tests\Services\ProcessDataPayloadBuilderTests.cs`

Current repo:

- `docs\harness\AH-RUNTIME-42.md`
- `docs\harness\AH-RUNTIME-43.md`
- `docs\harness\AH-RUNTIME-44.md`
- `docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `docs\context\COGNITIVE_SYNC_CHECK.md`
- `src\CAAutomationHub.Runtime\Polling`
- `tests\CAAutomationHub.Runtime.Tests`
- `tests\CAAutomationHub.Runtime.Tests\RuntimeProjectReferenceBoundaryTests.cs`

## 3. pilot flow 성격 판단

`WorkStartPilotService.RunOnceAsync(...)`는 단순 polling result production이 아니다.

판정:

- business transaction
- pilot command flow
- process handoff scenario
- integration scenario anchor

근거:

- PLC block read로 시작하지만, 목적은 Runtime state polling update가 아니라 LOTID 기반 process data handoff다.
- 성공 조건은 `LOTID read -> DB query -> process payload write -> ACK write` 완료다.
- 실패 조건은 step별 business error code와 error write policy를 가진다.
- `RequestHex`, `ResponseHex`, selected LOTID, DB result, payload build, ACK/error write는 `ChannelPollingResult`에 넣기에는 business transaction detail이다.
- Runtime polling path는 vendor-neutral `PlcId`, success/failure, failure kind, response time 중심의 canonical state update path다.

따라서 Pilot business flow는 Runtime polling state path와 구분해야 한다.

## 4. scenario step 분해

| Step | Input | Output | Success condition | Failure condition | Error code | Error write | Dependency category |
| --- | --- | --- | --- | --- | --- | --- | --- |
| EnsureConnected | channel, cancellation | connected channel | no exception | exception | `2999` | No | XGT operation |
| ReadWorkStartBlock | read start variable, word count | raw read response | classified success | NAK / malformed | `1101` | No | XGT operation |
| ParseReadWords | response bytes | read word bytes | ACK + first block data exists | empty / invalid read payload | `1102` | No | XGT parser / adapter |
| CheckStartSignal | read bytes, start signal index | active bool | status word nonzero | inactive or out-of-range | `1200` | No | business decision |
| ExtractLotIds | read bytes, offsets, length | LOT ID 1, LOT ID 2 | strings extracted | out-of-range yields empty | N/A | N/A | pure helper |
| SelectLotId | LOT ID 1, LOT ID 2 | selected LOT ID | LOT ID 1 first, else LOT ID 2 | both empty | `2201` | Yes | business decision |
| QueryProcessData | selected LOT ID, query policy | process data | exactly one row | exception / not found / multiple / failed | `2300`-`2303` | Yes | DB query |
| BuildProcessPayload | process data, payload layout | write payload | payload built | builder exception | `2400` | Yes | pure helper |
| WriteProcessPayload | write start variable, payload | write result | write success | write failure | `2501` | Yes | XGT operation |
| WriteAck | ACK address, ACK value | ACK result | write success | ACK write failure | `2601` | Yes | XGT operation |
| WriteErrorCodeBestEffort | error code address, error code | ignored write result | attempted where policy says yes | ignored | original code | best-effort | diagnostics / operation |
| Complete | final step state | success result | all required writes succeeded | N/A | N/A | No | result assembly |

## 5. scenario input contract 후보

현재 `PilotScenarioConfig`에서 scenario input으로 볼 수 있는 값:

- `LotReadStartVariable`
- `LotReadWordCount`
- `StartSignalWordIndexInReadBlock`
- `LotId1WordOffset`
- `LotId2WordOffset`
- `LotIdWordLength`
- `WriteStartVariable`
- `WriteWordCount`
- `AckWriteVariable`
- `AckValue`
- `ErrorCodeWriteVariable`
- `ConnectionString`
- `SqlText`
- `QueryTimeoutSeconds`
- `CommandTimeoutMs`

현재 config에 있으나 `RunOnceAsync(...)`에서 사용하지 않는 값:

- `CompleteAckWriteVariable`
- `CompleteSignalWordIndexInReadBlock`
- `PollingIntervalMs`

후속 contract로 분리해야 할 값:

- step names
- error code mapping
- error write policy
- payload field layout
- DB query id 또는 query policy
- XGT-specific address policy
- diagnostics capture policy

주의:

- 이 값을 지금 CAAutomationHub 타입으로 만들지 않는다.
- Runtime core의 `ChannelPollingTarget`에는 XGT address / datatype / count를 추가하지 않는다.
- PLC별 flow variation은 Runtime core가 아니라 Business Flow Definition 또는 adapter-adjacent configuration으로 다룬다.

## 6. scenario output contract 후보

Business output:

- succeeded / failed
- selected LOT ID
- final step
- business error code

Diagnostic output:

- message
- elapsed milliseconds
- reconnect attempt count
- request hex
- response hex
- response classification 후보

XGT adapter output:

- read/write operation status
- adapter failure classification
- raw request / response hex if diagnostics policy enables it

UI display output:

- success/failure message text
- textbox / label / MessageBox projection
- button state

Test assertion output:

- expected step
- expected error code
- selected LOT ID
- exchange call count
- whether error write was attempted
- payload bytes and field layout

판단:

- UI display output은 scenario contract로 가져오지 않는다.
- XGT raw diagnostics는 adapter / diagnostics output으로 분리한다.
- business output은 후속 `PilotFlowResult` 또는 flow execution result 후보가 될 수 있다.

## 7. failure / error code matrix

| Error code | Condition | Step | Error write attempted | Error write target | Best-effort | Retry |
| --- | --- | --- | --- | --- | --- | --- |
| `1101` | read NAK / malformed / classified failure | `group-read` | No | N/A | N/A | channel recovery internal only |
| `1102` | parsed read response is not ACK, has no variable block, or data is null | `group-read-parse` | No | N/A | N/A | No |
| `1200` | start signal inactive or index outside read bytes | `start-signal` | No | N/A | N/A | No |
| `2201` | LOT ID 1 and LOT ID 2 are both empty | `lotid` | Yes | `%DB11410` | Yes | No |
| `2300` | DB query throws exception | `db-query` | Yes | `%DB11410` | Yes | No |
| `2301` | DB not found | `db-query` | Yes | `%DB11410` | Yes | No |
| `2302` | DB multiple rows | `db-query` | Yes | `%DB11410` | Yes | No |
| `2303` | DB query failed for other non-success result | `db-query` | Yes | `%DB11410` | Yes | No |
| `2400` | payload builder throws | `payload-build` | Yes | `%DB11410` | Yes | No |
| `2501` | process bulk write fails | `bulk-write` | Yes | `%DB11410` | Yes | No |
| `2601` | ACK write fails | `ack-write` | Yes | `%DB11410` | Yes | No |
| `2999` | unexpected exception caught by outer catch | `exception` | No | N/A | N/A | No |

중요 보정:

- 모든 실패가 error code write를 수행하지 않는다.
- `2201`, `2300`-`2303`, `2400`, `2501`, `2601`만 현재 `TryWriteErrorCodeAsync`를 호출한다.
- `1101`, `1102`, `1200`, `2999`는 현재 error code write를 수행하지 않는다.
- `TryWriteErrorCodeAsync` 자체의 성공/실패는 result에 반영되지 않고 무시된다.

## 8. pure helper 후보

`ProcessDataPayloadBuilder`는 pure helper 후보로 볼 수 있다.

근거:

- 외부 DB dependency 없음
- XGT session / channel dependency 없음
- UI dependency 없음
- 입력은 config, LOT ID, process data
- 출력은 payload bytes와 field metadata
- payload packing tests가 존재함

다만 그대로 source copy는 비권장이다.

이유:

- `PilotScenarioConfig.WriteStartVariable`, `WriteWordCount`에 의존한다.
- field offset / length / name이 hard-coded 되어 있다.
- `CUT_SIZE` scaling policy가 TODO 상태다.
- payload layout은 PLC별로 달라질 수 있으므로 option / layout contract가 먼저 필요하다.

후속 후보:

- LOT ID extraction rule
- start signal interpretation rule
- ASCII fixed field writer
- single ASCII word writer
- little-endian int32 two-word writer
- process payload field layout table
- error code mapping table

## 9. DB query abstraction 후보

`LotDataQueryService`를 직접 가져오지 않는다.

필요 abstraction 후보:

- `IWorkStartDataQuery`
- `ILotProcessDataQuery`
- `IProcessDataQueryService`

필요한 result shape:

- success with process data
- not found
- multiple rows
- failed with message
- exception path

분리해야 할 항목:

- connection string
- SQL text
- query timeout
- query id 또는 query policy
- DB schema to process data mapping
- `@LotId` parameter binding policy

판단:

- Runtime core에는 SQL connection string / SQL text를 넣지 않는다.
- Business Flow Definition에는 query id 또는 query policy reference를 둘 수 있으나, 실제 SQL execution은 DB Query Abstraction 구현이 맡는다.

## 10. XGT operation abstraction 후보

현재 `WorkStartPilotService`는 `PlcChannel`, `XgtFrameBuilder`, `XgtFrameParser`, raw exchange / classifier를 직접 사용한다.

후속 abstraction 후보:

- `IWorkStartPlcOperations`
- `IPilotPlcOperations`
- `IPlcWordBlockClient`
- XGT-specific 구현 후보: `IXgtWorkStartOperations`

필요 operation:

- `EnsureConnectedAsync`
- `ReadWordsAsync`
- `WriteWordsAsync`
- `WriteAckAsync`
- `WriteErrorCodeBestEffortAsync`

판단:

- Business Flow Definition과 Flow Executor는 raw XGT frame을 몰라야 한다.
- XGT address / protocol / parser / classifier는 XGT Operation Adapter 내부에 둔다.
- Runtime core는 이 operation abstraction을 직접 소유하지 않는다.

## 11. PLC별 FLOW.JSON / Business Flow Definition anchor

사용자 의견을 반영해, `WorkStartPilotService.RunOnceAsync(...)`는 단일 hard-coded service로만 보지 않는다.

해석:

    WorkStartPilotService.RunOnceAsync(...)
        = 현재 검증된 hard-coded pilot sequence
        = 향후 PLC별 Business Flow Definition / FLOW.JSON schema의 원형

즉, AH-RUNTIME-45의 scenario step / input / output / failure matrix는 후속 `FLOW.JSON` 설계의 기초 자료다.

`FLOW.JSON`은 XGT 명령 목록이 아니라 업무 흐름 정의다.

`XGT address`, DB query, payload layout, ACK / error policy는 `FLOW.JSON` 또는 그 하위 참조 설정에 포함될 수 있다. 하지만 그 실행은 `FLOW.JSON` 자체가 직접 수행하는 것이 아니라 Flow Executor와 Adapter 계층이 담당한다.

즉, `FLOW.JSON`은 "무엇을 어떤 조건과 순서로 처리할지"를 선언하고, 실제 PLC read / write, DB query, payload build, ACK / error write는 각 실행 계층이 수행한다.

PLC별로 달라질 수 있는 항목:

- read start address
- read word count
- start signal index
- LOT ID offsets
- LOT ID length
- DB query id 또는 query policy
- payload layout
- write start address
- ACK address
- ACK value
- error code address
- error code mapping
- error write policy
- diagnostics capture policy
- step enable / skip policy
- failure stop / continue policy

분리해야 할 책임:

- Business Flow Definition: PLC별 업무 흐름을 선언한다. 요청 감지, 조건, step 순서, ACK ON / OFF, error policy, 종료 상태를 정의한다.
- Flow Executor: Business Flow Definition을 해석하고 step을 순서대로 실행한다. 조건에 따라 착공 flow, 완공 flow, ACK OFF flow를 선택한다.
- XGT Operation Adapter: PLC read / write, ACK write, ACK OFF write, error code write를 수행한다. XGT address, datatype, raw frame, parser, classifier는 이 계층에 머문다.
- DB Query Abstraction: LOTID 기반으로 착공데이터 또는 필요한 업무 데이터를 조회한다.
- Payload Builder: DB result를 PLC write payload로 변환한다. PLC별 payload layout 차이는 definition 또는 layout config로부터 받는다.
- ACK / Error Writer: ACK ON / OFF와 error code write를 operation adapter를 통해 수행하고 best-effort policy를 따른다.
- Runtime Core: XGT, DB, business flow 세부사항을 모른다. vendor-neutral state / snapshot / polling result 경계를 유지한다.

중요 boundary:

- Runtime core에 `FLOW.JSON` parser를 넣지 않는다.
- Runtime core에 XGT-specific flow execution을 넣지 않는다.
- Runtime core에 PLC별 address / payload layout / SQL policy를 넣지 않는다.
- `FLOW.JSON`은 후속 설계 후보이며, AH-RUNTIME-45에서는 JSON 파일 또는 schema를 생성하지 않는다.

### 11.1 현재 비즈니스 로직 전체 anchor

현재 사용자 정의 비즈니스 로직은 착공요청 ON 시나리오만으로 닫히지 않는다. PLC별 업무 flow는 polling을 통한 request detection, 착공요청 처리, 착공ACK OFF, 완공요청 처리, 완공ACK OFF, 대기 복귀까지 포함해야 한다.

Polling 단계:

- PLC의 착공요청이 있는지 확인한다.
- 착공요청이 있다면 해당 LOTID가 무엇인지 확인한다.
- PLC의 완공요청이 있는지 확인한다.
- 완공요청이 있다면 해당 LOTID가 무엇인지 확인한다.
- 이 단계는 단순 상태 관측이 아니라, 이후 업무 flow를 선택하기 위한 request detection 단계다.

착공요청 ON 시나리오:

1. LOTID를 활용해 DB에서 착공데이터를 조회한다.
2. 조회한 착공데이터를 PLC에 전송한다.
3. 착공ACK 신호를 ON한다.
4. 이후 PLC의 착공요청 신호가 OFF 되는지 감시한다.
5. PLC의 착공요청 신호가 OFF 되면 착공ACK 신호도 OFF 한다.
6. 다시 착공요청 또는 완공요청 대기 상태로 돌아간다.

기존 `WorkStartPilotService.RunOnceAsync(...)` 분석은 주로 1~3번, 즉 착공데이터 조회 / 전송 / ACK ON에 가까운 pilot flow로 볼 수 있다. 착공요청 OFF 감지 후 착공ACK OFF 하는 흐름은 아직 별도 정리가 필요하다.

완공요청 ON 시나리오:

1. 완공ACK 신호를 ON한다.
2. 이후 PLC의 완공요청 신호가 OFF 되는지 감시한다.
3. PLC의 완공요청 신호가 OFF 되면 완공ACK 신호도 OFF 한다.
4. 다시 착공요청 또는 완공요청 대기 상태로 돌아간다.

완공요청 흐름은 현재 `WorkStartPilotService.RunOnceAsync(...)`에서 직접 분석된 범위는 아니지만, 향후 `FLOW.JSON` / Business Flow Definition에 반드시 포함될 업무 흐름 anchor로 남긴다.

### 11.2 착공 / 완공 요청 상태 전이 anchor

상태 전이 관점의 후보:

1. 대기 상태
   - 착공요청 또는 완공요청을 기다린다.

2. 착공요청 감지
   - 착공요청 ON과 LOTID를 확인한다.

3. 착공처리
   - DB에서 착공데이터를 조회하고 PLC에 데이터를 write한다.

4. 착공ACK ON
   - 착공ACK 신호를 ON한다.

5. 착공요청 OFF 대기
   - PLC의 착공요청 신호가 OFF 될 때까지 기다린다.

6. 착공ACK OFF
   - 착공요청이 OFF 되면 착공ACK도 OFF한다.

7. 완공요청 감지
   - 완공요청 ON과 LOTID를 확인한다.

8. 완공ACK ON
   - 완공ACK 신호를 ON한다.

9. 완공요청 OFF 대기
   - PLC의 완공요청 신호가 OFF 될 때까지 기다린다.

10. 완공ACK OFF
    - 완공요청이 OFF 되면 완공ACK도 OFF한다.

11. 다시 대기
    - 착공 또는 완공요청 대기 상태로 돌아간다.

### 11.3 FLOW.JSON 후보 의미

향후 PLC별 `FLOW.JSON`은 다음을 표현할 수 있어야 한다.

- 어떤 요청 신호를 polling할지
- 요청 신호가 ON일 때 어떤 flow를 실행할지
- LOTID를 어디서 어떻게 추출할지
- DB query 정책은 무엇인지
- 어떤 payload layout으로 PLC에 데이터를 쓸지
- 어떤 ACK 신호를 ON할지
- 요청 신호가 OFF 되면 어떤 ACK 신호를 OFF할지
- 실패 시 어떤 error code를 쓸지
- 어떤 실패는 error write를 시도하고, 어떤 실패는 시도하지 않을지
- flow 종료 후 어떤 대기 상태로 돌아갈지

단, `FLOW.JSON`은 직접 XGT frame을 만들거나 DB를 실행하지 않는다.

실행은 아래 계층이 담당한다.

- Flow Executor
- XGT Operation Adapter
- DB Query Abstraction
- Payload Builder
- ACK / Error Writer
- Runtime Core는 vendor-neutral state / snapshot만 담당

## 12. 후보 A: 문서만 작성 검토

판정: AH-RUNTIME-45 자체에는 충분하고 적절하다.

장점:

- 코드 오염이 없다.
- Runtime core vendor-neutral boundary를 지킨다.
- 검증된 business sequence를 먼저 의미 계약으로 고정한다.
- 후속 `FLOW.JSON` / Business Flow Definition 설계의 기준이 된다.

위험:

- 실행 가능한 harness는 아직 없다.
- 문서만으로는 regression을 막을 수 없다.

결론:

- AH-RUNTIME-45에서는 문서 closeout까지가 적절하다.
- 다음 단계에서 scenario matrix와 flow definition boundary를 구체화한다.

## 13. 후보 B: Scenario Harness 문서 + 테스트 케이스 목록 검토

판정: AH-RUNTIME-46 최우선 후보로 적절하다.

개념:

- pilot scenario matrix 작성
- given / when / then 정리
- expected step / error code / error write attempted 여부 정리
- 아직 테스트 코드는 작성하지 않음

장점:

- 구현 전 테스트 설계가 명확해진다.
- `ProcessDataPayloadBuilder` 이식 전 필요한 cases가 정리된다.
- `FLOW.JSON` step/failure schema 후보를 실제 scenario 기준으로 볼 수 있다.

위험:

- 여전히 실행 가능한 테스트는 아니다.

결론:

- AH-RUNTIME-46 후보로 가장 자연스럽다.

## 14. 후보 C: Pure helper extraction 검토

판정: AH-RUNTIME-46 후반 또는 AH-RUNTIME-47 후보로 적절하다.

장점:

- `ProcessDataPayloadBuilder` packing rule은 검증된 재사용 가치가 있다.
- payload tests 일부를 CAAutomationHub 기준으로 재구성할 수 있다.
- Runtime core 오염 없이 PilotFlow / adapter-adjacent helper로 분리 가능하다.

위험:

- scenario contract 없이 helper부터 가져오면 맥락이 흐려진다.
- payload layout이 PLC별로 달라질 수 있어 `FLOW.JSON` / layout definition과 먼저 맞춰야 한다.

결론:

- scenario matrix와 Business Flow Definition boundary review 이후 진행한다.

## 15. 후보 D: PilotFlow module skeleton 검토

판정: AH-RUNTIME-47 이후 후보로 적절하다.

장점:

- 사용자 선호인 "`CAAutomationHub` 안에 녹이기"에 가깝다.
- Business Flow Definition, Flow Executor, Adapter abstraction을 나눌 수 있다.

위험:

- 너무 빨리 code shape를 고정할 수 있다.
- DB / XGT / payload layout abstraction이 불명확하면 재작업 가능성이 크다.
- Runtime project 내부에 두면 vendor-neutral boundary가 흔들릴 수 있다.

결론:

- 먼저 schema/definition boundary와 helper extraction contract를 세운 뒤 검토한다.

## 16. 권장안

권장 순서:

1. Pilot Flow Scenario Matrix 작성
2. Business Flow Definition / `FLOW.JSON` Boundary Review
3. Pilot Flow Schema Draft
4. Flow Executor Boundary Review
5. Pure Helper Extraction Review
6. PilotFlow module / project boundary review
7. XGT Operation Adapter와 DB Query Abstraction 상세 review

판단 이유:

- 지금 구현보다 scenario 계약과 flow definition boundary가 먼저 필요하다.
- `WorkStartPilotService.RunOnceAsync(...)`의 검증된 흐름을 누락 없이 보존해야 한다.
- Runtime core vendor-neutral boundary를 지켜야 한다.
- PLC별로 flow가 달라질 수 있으므로 hard-coded service copy보다 definition-driven approach가 안전하다.
- `FLOW.JSON`은 XGT 명령 목록이 아니라 착공 / 완공을 포함한 업무 흐름 정의로 다뤄야 한다.
- 기존 `WorkStartPilotService.RunOnceAsync(...)` 분석은 착공요청 ON 시나리오의 착공데이터 조회 / 전송 / ACK ON 부분으로 위치를 보정해야 한다.
- 후속 `XgtAdapter` / `PilotFlow` 구현에 필요한 step, input, output, failure matrix를 먼저 고정할 수 있다.

## 17. AH-RUNTIME-46 후보 및 우선순위

추천 우선순위:

1. Pilot Flow Scenario Matrix
   - 기존 `WorkStartPilotService` flow만이 아니라 polling 단계, 착공요청 ON 시나리오, 완공요청 ON 시나리오를 함께 포함한다.
   - Polling 단계: 착공요청 감지, 완공요청 감지, LOTID 추출
   - 착공요청 ON 시나리오: 착공데이터 DB 조회, 착공데이터 PLC write, 착공ACK ON, 착공요청 OFF 감지, 착공ACK OFF, 대기 복귀
   - 완공요청 ON 시나리오: 완공ACK ON, 완공요청 OFF 감지, 완공ACK OFF, 대기 복귀
   - error code / step / given-when-then / expected action / error write policy 정리

2. Business Flow Definition / `FLOW.JSON` Boundary Review
   - `FLOW.JSON`은 XGT 명령 목록이 아니라 업무 흐름 정의임을 다시 확인한다.
   - PLC별 flow definition의 책임, 위치, Runtime core와의 dependency boundary 검토

3. Pilot Flow Schema Draft
   - step, input, output, failure, error mapping, payload layout reference의 초안 작성
   - 실제 JSON 파일 생성이나 parser 구현은 별도 단계

4. Flow Executor Boundary Review
   - definition-driven step execution 책임과 operation adapter / DB query / payload builder 호출 boundary 검토

5. Pure Helper Extraction Review
   - `ProcessDataPayloadBuilder`, LOT ID extraction, start signal, error mapping 이식 후보 검토

6. XGT Operation Abstraction Boundary Review
   - PLC read/write/ACK/error operation interface 검토

7. DB Query Abstraction Boundary Review
   - `LotDataQueryService` 직접 이식 없이 LOT query contract 검토

## 18. 제외한 범위

이번 AH-RUNTIME-45에서는 다음을 하지 않았다.

- production code 수정
- test code 수정
- scenario JSON 생성
- `FLOW.JSON` 파일 생성
- schema 구현
- parser 구현
- Flow Executor 구현
- PilotFlow class 추가
- project / solution / csproj 수정
- `ProjectReference` / `PackageReference` 추가
- source copy
- `XgtDriverCore` 연결
- `FakePlc` 연결
- `XgtChannelRunner` 연결
- adapter skeleton 추가
- WPF 수정
- Contracts 수정
- `ContextPublisher` 수정
- commit

이번 단계는 조사와 closeout 기록만 수행했다.

## 19. 실행한 명령

현재 repo:

- `git status --short`
- `rg "WorkStart|Pilot|Lot|Payload|ProcessData|ErrorCode|Scenario" src tests docs/harness docs/context`
- `rg --files src/CAAutomationHub.Runtime/Polling tests/CAAutomationHub.Runtime.Tests | rg "Polling|RuntimeProjectReferenceBoundaryTests"`
- `Get-Content` for:
  - `docs\harness\AH-RUNTIME-42.md`
  - `docs\harness\AH-RUNTIME-43.md`
  - `docs\harness\AH-RUNTIME-44.md`
  - `docs\context\COGNITIVE_SYNC_CHECK.md`
  - `docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
  - Runtime polling files
  - `RuntimeProjectReferenceBoundaryTests.cs`

Sibling repo:

- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short`
- `rg -n "WorkStartPilotService|RunOnceAsync|ProcessDataPayloadBuilder|LotDataQueryService|WorkStartPilotResult|PilotScenarioConfig|TryWriteErrorCodeAsync|ErrorCode" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- `rg -n -C 35 "btnRunPilotOnce_Click|btnRunPilotOnce" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\MainForm.cs`
- `rg -n -C 8 "btnRunPilotOnce" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\MainForm.Designer.cs`
- `Get-Content` for:
  - `XgtChannelRunner\Services\WorkStartPilotService.cs`
  - `XgtChannelRunner\Models\PilotScenarioConfig.cs`
  - `XgtChannelRunner\Models\WorkStartPilotResult.cs`
  - `XgtChannelRunner\Services\ProcessDataPayloadBuilder.cs`
  - `XgtChannelRunner\Services\LotDataQueryService.cs`
  - `XgtChannelRunner\Models\ContinuousWritePayloadModel.cs`
  - `XgtChannelRunner\Models\LotProcessData.cs`
  - `XgtChannelRunner\Models\PlcWriteFieldValue.cs`
  - `XgtChannelRunner\Services\README.md`
  - `XgtChannelRunner\Models\README.md`
  - `tests\AutomationHub.XgtChannelRunner.Tests\Services\ProcessDataPayloadBuilderTests.cs`

Closeout 작성 시 추가 확인:

- `git status --short`
- `Test-Path docs\harness\AH-RUNTIME-45.md`
- `Get-ChildItem docs\harness -Filter AH-RUNTIME-4*.md`
- `Get-Content docs\harness\AH-RUNTIME-44.md`

FLOW.JSON / 착공 / 완공 anchor 보강 후 검증:

- `git diff -- docs/harness/AH-RUNTIME-45.md`
- `git diff --check -- docs/harness/AH-RUNTIME-45.md`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-45.md`

## 20. git status 결과

Current repo `C:\AutomationHub.Rebuild\CAAutomationHub` initial status before closeout creation:

- `git status --short`: clean

Current repo status after closeout creation:

```text
?? docs/harness/AH-RUNTIME-45.md
```

Sibling repo `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore` status:

```text
 M tools/AutomationHub.XgtDriverCore.FakePlc/appsettings/fakeplc.map.json
?? context-events/pending/evt_20260507_234052_receiveframeasync-whenresponsearrivesinp.json
?? context-events/pending/evt_20260507_234052_tcptransport-basic-request-response.json
?? context-events/pending/evt_20260507_234052_tcptransport-timeout-handling.json
?? context-events/pending/evt_20260507_234052_xgtsession-basic-exchange.json
?? context-events/pending/evt_20260516_005654_iwritableruntimeplcchannel-runtime-state.json
```

Sibling repo diff stat:

```text
tools/AutomationHub.XgtDriverCore.FakePlc/appsettings/fakeplc.map.json | 4 ++--
```

## 21. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-45 Boundary Review 결과를 `docs/harness/AH-RUNTIME-45.md` closeout 문서로 기록했다.
- `WorkStartPilotService.RunOnceAsync(...)`를 Runtime polling path가 아니라 business transaction / pilot command flow / process handoff scenario로 판정했다.
- scenario step / input / output / failure matrix를 실제 코드 기준으로 기록했다.
- 모든 실패가 error code write를 수행하지 않는다는 보정 사항을 기록했다.
- `2201`, `2300`-`2303`, `2400`, `2501`, `2601`만 best-effort error write를 수행함을 기록했다.
- PLC별 `FLOW.JSON` / Business Flow Definition 관점을 설계 anchor로 반영했다.
- `FLOW.JSON`은 XGT 명령 목록이 아니라 업무 흐름 정의라는 점을 기록했다.
- 착공요청 / 완공요청 / ACK ON / ACK OFF / 대기 복귀 흐름을 현재 비즈니스 로직 전체 anchor로 기록했다.
- 기존 `WorkStartPilotService.RunOnceAsync(...)` 분석이 전체 flow 중 착공요청 ON 시나리오의 착공데이터 조회 / 전송 / ACK ON 부분에 해당한다는 위치 보정을 기록했다.
- PLC별로 달라질 수 있는 항목을 분리해 기록했다.
- Business Flow Definition, Flow Executor, XGT Operation Adapter, DB Query Abstraction, Payload Builder, ACK / Error Writer, Runtime Core 책임을 분리해 기록했다.
- Runtime core에 `FLOW.JSON` parser나 XGT-specific flow execution을 넣지 않는 boundary를 기록했다.
- AH-RUNTIME-46 후보로 Pilot Flow Scenario Matrix, Business Flow Definition / `FLOW.JSON` Boundary Review, Pilot Flow Schema Draft, Flow Executor Boundary Review를 포함했다.
- production code, test code, scenario JSON, schema/parser/executor 구현, source copy, project reference, XGT/FakePlc/Runner 연결, WPF 수정, commit, `ContextPublisher` 자동 publish는 수행하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
