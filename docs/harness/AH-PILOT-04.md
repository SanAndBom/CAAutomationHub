# AH-PILOT-04 Closeout - WorkStart DB Query Boundary Review

## 1. Summary

AH-PILOT-04는 `WorkStartFlowService` 구현 전에 필요한 외부 의존성 seam을 확정하기 위한 Boundary Review다.

현재 `CAAutomationHub.PilotFlows.WorkStart`에는 payload packing, read block interpretation, result / error policy가 순수 helper와 model로 분리되어 있다. 이번 review의 결론은 다음 단계의 orchestration skeleton은 `CAAutomationHub.PilotFlows.WorkStart` 안에 둘 수 있지만, 실제 PLC read/write, ACK/error write, DB query, XGT address, SQL text, Runtime state는 interface seam 또는 adapter/binding/options 계층 밖에 남겨야 한다는 것이다.

권장 AH-PILOT-05 범위는 `WorkStartFlowService` skeleton + `IWorkStartPlcOperations` + `IWorkStartDataQuery` + fake tests다. 단, happy path와 대표 failure 2~3개로 제한하고, XGT / DB concrete, FLOW.JSON, Runtime / FlowDefinitions 연결은 계속 제외한다.

이번 작업은 read-only 조사와 closeout 문서 작성만 수행했다. production code, test code, interface, service, adapter, DB query model, csproj / solution, reference, commit은 수정하지 않았다.

## 2. 확인한 기존 WorkStartPilotService 근거

확인한 sibling repo 파일:

- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\WorkStartPilotService.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\WorkStartPilotResult.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\PilotScenarioConfig.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\LotDataQueryService.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\ProcessDataPayloadBuilder.cs`

확인한 기존 sequence:

1. `PlcChannel.EnsureConnectedAsync`
2. `%DB10000`, 90 words continuous read
3. start signal word index `80` 확인
4. LOT ID 1 word offset `0`, LOT ID 2 word offset `10`, length `6` words 추출
5. LOT ID 1 우선 선택, 없으면 LOT ID 2 선택
6. selected LOT ID로 SQL DB query
7. process data payload build
8. `%DB11000` process payload bulk write
9. `%DB11416` Start ACK value `1` write
10. 일부 실패 시 `%DB11410` error code best-effort write

기존 error write 수행 code:

- `2201`
- `2300`
- `2301`
- `2302`
- `2303`
- `2400`
- `2501`
- `2601`

기존 error write 미수행 code:

- `1101`
- `1102`
- `1200`
- `2999`
- `None`

기존 `WorkStartPilotResult`에는 request / response hex, elapsed time, reconnect attempt count가 들어 있지만, AH-PILOT-03-B의 `WorkStartFlowResult`는 이 diagnostics를 의도적으로 제외했다. AH-PILOT-05도 이 방향을 유지해야 한다.

## 3. WorkStart service orchestration 위치 검토

후보:

- `WorkStartPilotFlowService`
- `WorkStartFlowService`
- `WorkStartProcessHandoffService`
- `WorkStartPilotOrchestrator`

판단:

- 권장 이름은 `WorkStartFlowService`다.
- 위치는 `CAAutomationHub.PilotFlows.WorkStart`가 적절하다.
- 이유는 현재 순수 helper가 이미 이 namespace에 있고, WorkStart business transaction을 Runtime polling state path와 분리할 수 있기 때문이다.
- service는 `RuntimeSnapshot`, `ChannelPollingResult`, `ChannelPollingTarget`, FlowDefinitions concrete, FLOW.JSON parser를 몰라야 한다.
- service는 XGT / DB concrete가 아니라 최소 interface seam만 바라봐야 한다.
- Runtime core가 이 service를 소유하면 안 된다.

Boundary:

- `CAAutomationHub.Runtime`: canonical state / polling / snapshot owner로 유지.
- `CAAutomationHub.FlowDefinitions`: neutral definition candidate / validator boundary로 유지.
- `CAAutomationHub.PilotFlows`: WorkStart business flow policy와 pure helper, future orchestration skeleton 소유 가능.
- XGT / DB implementation: 후속 adapter project 또는 adapter-adjacent 계층 책임.

## 4. PLC operation interface 후보

후보 이름:

- `IWorkStartPlcOperations`
- `IPilotPlcOperations`
- `IWorkStartPlcClient`
- `IWorkStartOperationPort`

권장 이름:

- `IWorkStartPlcOperations`

필요 operation 후보:

- `EnsureConnectedAsync(CancellationToken cancellationToken)`
- `ReadWorkStartBlockAsync(CancellationToken cancellationToken)`
- `WriteProcessPayloadAsync(byte[] payloadBytes, CancellationToken cancellationToken)`
- `WriteStartAckAsync(CancellationToken cancellationToken)`
- `WriteErrorCodeBestEffortAsync(WorkStartErrorCode errorCode, CancellationToken cancellationToken)`

검토 결과:

- `EnsureConnectedAsync`는 포함하는 편이 좋다. 기존 runner sequence의 첫 단계이며, service skeleton이 기존 flow order를 테스트할 수 있다.
- read block 반환은 우선 `byte[]`가 가장 단순하다. 이미 `WorkStartReadBlockInterpreter`가 `byte[]` 기반 pure helper이기 때문이다.
- write payload input은 `WorkStartPayloadBuildResult`보다 `byte[]`가 더 안전하다. PLC operation seam은 payload field metadata를 알 필요가 없고, 실제 target address는 adapter/binding/options 책임으로 남겨야 한다.
- ACK write와 error code write는 초기에는 같은 `IWorkStartPlcOperations`에 포함하는 편이 좋다. 둘 다 WorkStart PLC side effect이고, 별도 seam으로 쪼개면 AH-PILOT-05에서 과설계 위험이 커진다.
- error write는 best-effort 정책이므로 service가 `WorkStartErrorWritePolicy`로 수행 여부를 판단하고 operation에 요청한다.
- `WriteErrorCodeBestEffortAsync`는 실패를 `WorkStartFlowResult`에 반영하지 않는 기존 정책을 유지한다. 반환값은 초기 skeleton에서는 불필요하며, diagnostics가 필요해지면 future output으로 분리한다.

금지:

- interface에 XGT address type을 넣지 않는다.
- interface에 `XgtDriverCore`, `XgtChannelRunner`, `PlcChannel`, raw frame type을 넣지 않는다.
- interface에 `%DB10000`, `%DB11000`, `%DB11416`, `%DB11410` 같은 address를 넣지 않는다.
- actual PLC read/write 구현은 AH-PILOT-05에서도 adapter 구현 단계가 아니면 제외한다.

## 5. DB query abstraction 후보

후보 이름:

- `IWorkStartDataQuery`
- `IWorkStartProcessDataQuery`
- `ILotProcessDataQuery`
- `IWorkStartDataProvider`

권장 이름:

- `IWorkStartDataQuery`

필요 operation 후보:

- `QueryAsync(string lotId, CancellationToken cancellationToken)`

반환 result 후보:

- success with `WorkStartProcessData`
- not found
- multiple rows
- failed
- exception captured

검토 결과:

- DB not found / multiple rows / failed는 service가 `WorkStartErrorCode.DbNotFound`, `DbMultipleRows`, `DbFailed`로 매핑할 수 있어야 한다.
- DB exception은 두 방식이 가능하다.
  - query implementation이 exception을 result로 캡처한다.
  - service가 `QueryAsync` exception을 catch해서 `DbException`으로 매핑한다.
- AH-PILOT-05 skeleton에서는 result model을 최소로 두고 exception catch도 service에서 방어하는 방향이 안전하다.
- SQL connection string, SQL text, timeout, parameter binding, row mapping은 PilotFlows core에 들어오면 안 된다.
- `Microsoft.Data.SqlClient` reference를 추가하지 않는다.

주의:

- AH-PILOT-04에서는 DB query interface나 result model을 만들지 않았다.
- AH-PILOT-05에서 만들 경우에도 concrete DB query 구현은 제외해야 한다.
- DB result model은 `WorkStartProcessData`를 반환하는 PilotFlows-local shape로 충분하며, sibling repo의 `LotProcessData` / SQL column naming을 그대로 끌어오지 않는다.

## 6. Payload builder seam 후보

현재 helper:

- `WorkStartProcessDataPayloadBuilder`
- `WorkStartProcessData`
- `WorkStartPayloadBuildOptions`
- `WorkStartPayloadBuildResult`
- `WorkStartPayloadField`

검토 결과:

- AH-PILOT-05에서는 concrete helper 직접 호출이 더 단순하다.
- `WorkStartProcessDataPayloadBuilder.Build(...)`는 static pure helper이고 external boundary dependency가 없다.
- `IWorkStartPayloadBuilder`를 지금 두면 테스트 seam은 좋아지지만, 아직 layout variation 요구가 확정되지 않아 과설계 가능성이 있다.
- payload layout variation이 빠르게 들어오거나 PLC별 payload binding이 생기면 그때 thin interface 또는 layout strategy를 검토한다.

권장:

- AH-PILOT-05 service skeleton은 concrete helper 직접 호출로 시작한다.
- payload build exception은 service가 catch해 `PayloadBuildFailed = 2400`으로 매핑한다.
- payload write operation에는 `PayloadBytes`만 넘긴다.
- payload field metadata와 diagnostics는 result에 넣지 않는다.

## 7. ACK / Error writer seam 검토

검토 결과:

- ACK write와 error write는 초기에는 `IWorkStartPlcOperations` 안에 포함하는 편이 좋다.
- ACK write 실패는 기존 정책대로 `AckWriteFailed = 2601` failure result로 조립한다.
- error write는 `WorkStartErrorWritePolicy.ShouldWriteErrorCode(...)`가 true인 실패에 대해서만 best-effort로 수행한다.
- error write 실패는 기존 정책대로 main `WorkStartFlowResult`의 성공/실패를 바꾸지 않는다.
- error write target address `%DB11410`은 adapter/binding/options 계층 책임이다.
- ACK target address `%DB11416`과 ACK value `1`도 adapter/binding/options 계층 책임이다.

별도 seam 보류:

- `IPilotAckWriter`
- `IPilotErrorWriter`

보류 이유:

- AH-PILOT-05에서 ACK/error writer를 별도 seam으로 분리하면 service skeleton의 constructor와 test setup이 빨리 커진다.
- 현재는 WorkStart 단일 flow의 PLC side effect로 묶어도 boundary가 깨지지 않는다.
- Complete ACK OFF 등 후속 flow와 공통화 요구가 생기면 별도 seam으로 분리한다.

## 8. WorkStartFlowResult 조립 책임

판단:

- `WorkStartFlowResult`는 `WorkStartFlowService`가 조립한다.
- 이유는 service가 step transition, selected LOT ID, DB result, payload build, PLC operation failure를 모두 관찰하는 orchestration 계층이기 때문이다.

조립 원칙:

- success는 `WorkStartFlowResult.Success(selectedLotId)`로 조립한다.
- failure는 `WorkStartFlowResult.Failure(step, errorCode, message, selectedLotId)`로 조립한다.
- selected LOT ID는 LOT ID 선택 이후 실패부터 포함한다.
- LOT ID empty, start signal inactive, read failure, parse failure는 selected LOT ID를 null로 유지한다.
- DB query / payload build / bulk write / ACK write failure는 selected LOT ID를 포함한다.
- DB result object, payload bytes, request / response hex, raw diagnostics는 result에 넣지 않는다.
- message는 user-facing 상세 로그가 아니라 짧은 failure reason 수준으로 제한한다.

Mapping 후보:

- read operation failure: `GroupRead` / `ReadFailed = 1101`
- empty or invalid read payload: `GroupReadParse` / `ReadParseFailed = 1102`
- start signal inactive: `StartSignal` / `StartSignalInactive = 1200`
- both LOT IDs empty: `LotId` / `LotIdEmpty = 2201`
- DB exception: `DbQuery` / `DbException = 2300`
- DB not found: `DbQuery` / `DbNotFound = 2301`
- DB multiple rows: `DbQuery` / `DbMultipleRows = 2302`
- DB failed result: `DbQuery` / `DbFailed = 2303`
- payload build exception: `PayloadBuild` / `PayloadBuildFailed = 2400`
- payload write failure: `BulkWrite` / `BulkWriteFailed = 2501`
- ACK write failure: `AckWrite` / `AckWriteFailed = 2601`
- unexpected exception: `Exception` / `UnexpectedException = 2999`

## 9. AH-PILOT-05 첫 service skeleton 후보

### 후보 A: WorkStart service skeleton + fake interfaces only

내용:

- `IWorkStartPlcOperations`
- `IWorkStartDataQuery`
- `WorkStartFlowService`
- fake implementations in tests
- happy path only

판정:

- 좋은 출발점이지만 happy path only이면 error write policy 연결 검증이 약하다.

### 후보 B: WorkStart service skeleton + failure path 일부

내용:

- LOT ID empty
- DB not found
- payload build failure
- bulk write failure
- ACK write failure

판정:

- 권장.
- 기존 helper 활용도가 높고 service orchestration 의미가 바로 검증된다.
- 단, 모든 failure를 한 번에 넣으면 범위가 커지므로 대표 failure 2~3개로 제한한다.

### 후보 C: 먼저 operation / query result model만 추가

내용:

- query result model
- PLC operation result model
- no service

판정:

- 보류.
- service 없이 result model만 먼저 만들면 추상 DTO가 굳고 실제 sequence 검증이 늦어진다.

### 후보 D: ACK/Error writer boundary only

내용:

- `IPilotAckWriter`
- `IPilotErrorWriter`
- no service

판정:

- 보류.
- 현재는 `IWorkStartPlcOperations`에 포함해도 충분하다.

### 후보 E: XGT adapter seam review

내용:

- no code
- adapter boundary only

판정:

- 후속 단계로 보류.
- AH-PILOT-05에서는 PilotFlows-local fake tests가 먼저 필요하다.

## 10. 권장안

AH-PILOT-05 권장 범위:

- `WorkStartFlowService`
- `IWorkStartPlcOperations`
- `IWorkStartDataQuery`
- query result model 최소 skeleton
- PLC operation result model 최소 skeleton 또는 bool/error-message 수준
- fake tests

권장 초기 tests:

- `RunAsync_Succeeds_WhenReadLotDbPayloadWriteAckAllSucceed`
- `RunAsync_ReturnsLotIdError_WhenBothLotIdsEmpty`
- `RunAsync_ReturnsDbNotFound_WhenQueryReturnsNotFound`
- `RunAsync_ReturnsBulkWriteFailure_WhenPayloadWriteFails`
- `RunAsync_WritesErrorCodeBestEffort_WhenPolicyRequiresIt`

범위 제한:

- happy path + 대표 failure 2~3개로 시작한다.
- XGT adapter 구현은 하지 않는다.
- DB concrete 구현은 하지 않는다.
- ACK/error target address는 넣지 않는다.
- FLOW.JSON / Flow Executor 연결은 하지 않는다.
- Runtime / FlowDefinitions project는 수정하지 않는다.

추가 판단:

- payload builder는 AH-PILOT-05에서 concrete helper 직접 호출을 권장한다.
- ACK/error writer는 초기에는 `IWorkStartPlcOperations`에 포함한다.
- `WorkStartFlowResult`는 service가 조립한다.
- service는 RuntimeSnapshot / ChannelPollingResult를 몰라야 한다.

## 11. 제외한 범위

이번 AH-PILOT-04에서는 다음을 하지 않았다.

- production code 수정
- test code 수정
- interface 추가
- `WorkStartFlowService` 또는 `WorkStartPilotService` 구현
- XGT Adapter 구현
- DB Query 구현
- ACK writer / error writer 구현
- actual PLC read/write 구현
- FLOW.JSON 파일 생성 또는 연결
- Flow Executor 구현
- csproj / solution 수정
- ProjectReference / PackageReference 추가
- Runtime project 수정
- FlowDefinitions project 수정
- RuntimeSnapshot 참조
- ChannelPollingTarget / ChannelPollingResult 수정
- SQL connection string / SQL text 추가
- XGT address를 Runtime / FlowDefinitions / PilotFlows model에 추가
- `WorkStartPilotService` source copy
- commit

## 12. 실행한 명령

현재 repo:

- `git log --oneline -8`
- `git status --short`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content docs\harness\AH-PILOT-01.md`
- `Get-Content docs\harness\AH-PILOT-02.md`
- `Get-Content docs\harness\AH-PILOT-03-A.md`
- `Get-Content docs\harness\AH-PILOT-03-B.md`
- `rg --files src\CAAutomationHub.PilotFlows\WorkStart`
- `rg --files tests\CAAutomationHub.PilotFlows.Tests\WorkStart`
- `rg --files src\CAAutomationHub.Runtime`
- `rg --files src\CAAutomationHub.FlowDefinitions`
- `Get-Content` for selected WorkStart helper/model/test files
- `Get-Content src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj`
- `Get-Content src\CAAutomationHub.FlowDefinitions\CAAutomationHub.FlowDefinitions.csproj`
- `rg -n "Xgt|FakePlc|XgtChannelRunner|PlcChannel|XgtFrame|TcpTransport|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.PilotFlows tests\CAAutomationHub.PilotFlows.Tests`
- `rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"`
- `git diff -- src\CAAutomationHub.Runtime src\CAAutomationHub.FlowDefinitions`
- `Test-Path docs\harness\AH-PILOT-04.md`

Sibling repo:

- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\WorkStartPilotService.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\WorkStartPilotResult.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\PilotScenarioConfig.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\LotDataQueryService.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\ProcessDataPayloadBuilder.cs`
- `rg -n "class LotProcessData|class ContinuousWritePayloadModel|class PlcWriteFieldValue|TryWriteErrorCodeAsync|AckWriteVariable|ErrorCodeWriteVariable|WriteStartVariable|LotReadStartVariable" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner`

Validation:

- `git diff -- docs/harness/AH-PILOT-04.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-04.md`

테스트 / 빌드:

- 문서 작성만 수행했으므로 실행하지 않았다.

## 13. git diff --check 결과

실행:

- `git diff --check`

결과:

- pass
- whitespace error 없음

주의:

- `docs/harness/AH-PILOT-04.md`는 untracked 파일이다.
- 따라서 `git diff --check`는 tracked diff만 검사한다.

## 14. git status --short 결과

실행:

- `git status --short`

결과:

```text
?? docs/harness/AH-PILOT-04.md
```

## 15. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-04 목표인 WorkStart 외부 의존성 seam Boundary Review를 closeout 문서로 남겼다.
- 기존 `WorkStartPilotService.RunOnceAsync(...)`의 순서, address anchor, DB query, ACK/error write 근거를 확인했다.
- `WorkStartFlowService` orchestration 위치를 PilotFlows-local로 검토했다.
- `IWorkStartPlcOperations`와 `IWorkStartDataQuery` 후보를 검토하고 권장안을 남겼다.
- payload builder는 AH-PILOT-05에서 concrete helper 직접 호출을 권장했다.
- ACK/error writer는 초기에는 PLC operations seam에 포함하고, address는 adapter/binding/options 책임으로 남겼다.
- `WorkStartFlowResult` 조립 책임을 service로 정리했다.
- AH-PILOT-05 첫 skeleton 범위를 구현 가능한 최소 단위로 제안했다.
- Runtime project와 FlowDefinitions project를 수정하지 않았다.
- XgtDriverCore / FakePlc / XgtChannelRunner reference를 추가하지 않았다.
- DB query, XGT adapter, ACK/error writer, actual PLC read/write, Flow Executor, FLOW.JSON을 구현하지 않았다.
- requested validation commands를 실행했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
