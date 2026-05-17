# AH-PILOT-05 Closeout - WorkStartFlowService Skeleton + Fake Tests

## 1. Summary

AH-PILOT-05는 기존 `WorkStartPilotService.RunOnceAsync(...)`의 핵심 업무 흐름을 `CAAutomationHub.PilotFlows.WorkStart` 내부의 service skeleton으로 재구성했다.

추가한 범위는 `WorkStartFlowService`, PLC operation seam, DB query seam, query result model, flow options, fake 기반 테스트다. 실제 XGT adapter, DB concrete, ACK/error writer concrete, Runtime/FlowDefinitions 연결, FLOW 정의 파일, executor 구현은 추가하지 않았다.

이 변경은 PilotFlows-local business orchestration을 고정하기 위한 단계이며, Runtime shared execution path와 Runtime canonical state 계약에는 영향을 주지 않는다. 실패 result는 기존 `WorkStartFlowResult`와 `WorkStartErrorWritePolicy`를 사용해 조립하고, error write는 policy가 요구하는 실패에 대해서만 best-effort로 호출한다.

## 2. 변경 파일 목록

- `src/CAAutomationHub.PilotFlows/WorkStart/IWorkStartPlcOperations.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/IWorkStartDataQuery.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartDataQueryStatus.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartDataQueryResult.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartFlowOptions.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartFlowService.cs`
- `tests/CAAutomationHub.PilotFlows.Tests/WorkStart/WorkStartFlowServiceTests.cs`
- `docs/harness/AH-PILOT-05.md`

## 3. 추가한 service / interface / query result 요약

### WorkStartFlowService

- `RunAsync(CancellationToken)` orchestration skeleton을 추가했다.
- PLC connect/read, read block interpretation, LOT ID selection, DB query, payload build, payload write, ACK write 순서를 고정했다.
- 실패 시 `WorkStartFlowResult.Failure(...)`를 조립하고, `ErrorWriteExpected`가 true인 경우에만 `WriteErrorCodeBestEffortAsync(...)`를 호출한다.
- error write 실패는 최종 result에 반영하지 않는다.

### IWorkStartPlcOperations

- `EnsureConnectedAsync`
- `ReadWorkStartBlockAsync`
- `WriteProcessPayloadAsync`
- `WriteStartAckAsync`
- `WriteErrorCodeBestEffortAsync`

주소, ACK value, error target, protocol frame, request/response hex는 포함하지 않았다.

### IWorkStartDataQuery / WorkStartDataQueryResult

- `QueryAsync(string lotId, CancellationToken)` seam을 추가했다.
- `Succeeded`, `NotFound`, `MultipleRows`, `Failed`, `Exception` 상태를 PilotFlows-local result model로 표현한다.
- SQL text, connection string, provider-specific exception type, DB concrete 구현은 포함하지 않았다.

### WorkStartFlowOptions

- read block interpretation index/offset/length와 payload build options만 포함한다.
- PLC address나 writer target은 포함하지 않았다.

## 4. 구현한 WorkStart flow 범위

구현한 최소 흐름:

1. PLC connection ensure
2. WorkStart read block read
3. start signal active 확인
4. LOT ID 1 / LOT ID 2 추출
5. LOT ID 1 우선 선택, 없으면 LOT ID 2 선택
6. selected LOT ID로 data query
7. query success data에 selected LOT ID를 반영해 payload build
8. process payload write
9. start ACK write
10. success result 반환

## 5. 구현한 happy path / failure path

Happy path:

- read/query/payload write/ACK write가 모두 성공하면 `WorkStartFlowResult.Success(selectedLotId)`를 반환한다.
- payload에는 selected LOT ID가 반영된다.
- error write는 호출하지 않는다.

Failure path:

- LOT ID empty: `LotIdEmpty = 2201`, `WorkStartStep.LotId`, error write expected true
- DB not found: `DbNotFound = 2301`, `WorkStartStep.DbQuery`, selected LOT ID 유지, error write expected true
- bulk write failure: `BulkWriteFailed = 2501`, `WorkStartStep.BulkWrite`, error write expected true
- start signal inactive: `StartSignalInactive = 1200`, `WorkStartStep.StartSignal`, error write expected false
- best-effort error write exception: primary failure result 유지

## 6. error write best-effort 처리

`WorkStartFlowService`는 실패 result를 먼저 만든 뒤 `result.ErrorWriteExpected`를 확인한다.

- true이면 `IWorkStartPlcOperations.WriteErrorCodeBestEffortAsync(...)`를 호출한다.
- false이면 호출하지 않는다.
- 호출 중 exception이 발생해도 최종 `WorkStartFlowResult`는 변경하지 않는다.

## 7. 이식하지 않은 범위

이번 작업에서 의도적으로 제외한 범위:

- XGT adapter concrete
- DB concrete
- SQL connection string / SQL text
- Microsoft.Data.SqlClient reference
- actual PLC read/write 구현
- ACK writer / error writer concrete
- FLOW 정의 파일 / schema / parser
- Flow Executor
- Runtime / FlowDefinitions 연결
- RuntimeSnapshot / ChannelPollingResult 참조
- WorkStartPilotService source copy
- request/response hex, elapsed time, reconnect diagnostics
- PLC address / ACK value / error target service model 포함

## 8. 테스트 결과

RED 확인:

- `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- 결과: 실패
- 이유: `IWorkStartPlcOperations`, `IWorkStartDataQuery`, `WorkStartDataQueryResult` 등 신규 타입 부재

GREEN 확인:

- `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- 결과: 통과
- 실패 0, 통과 30, 건너뜀 0

Runtime regression:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
- 결과: 통과
- 실패 0, 통과 142, 건너뜀 0

## 9. 빌드 결과

- `dotnet build CAAutomationHub.sln`
- 결과: 통과
- 경고 0, 오류 0

## 10. boundary scan 결과

실행:

- `rg -n "Xgt|FakePlc|XgtChannelRunner|PlcChannel|XgtFrame|TcpTransport|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.PilotFlows tests\CAAutomationHub.PilotFlows.Tests`

결과:

- 출력 없음
- 금지 boundary 문자열 미검출

Project reference scan:

- `rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"`

확인:

- `src/CAAutomationHub.PilotFlows/CAAutomationHub.PilotFlows.csproj`는 추가 reference 없음
- `tests/CAAutomationHub.PilotFlows.Tests/CAAutomationHub.PilotFlows.Tests.csproj`는 PilotFlows project만 참조
- Runtime / FlowDefinitions project는 수정하지 않음

## 11. 다음 후보

- DB multiple rows / DB failed / DB exception / ACK write failure 테스트 추가
- read failure / parse failure 테스트 추가
- payload build failure를 options 기반으로 재현할 수 있는 테스트 추가
- WorkStart operation adapter boundary review
- WorkStart DB query concrete boundary review
- ACK/error writer binding/options boundary review

## 12. Self-Check

판정: `ACCEPT`

근거:

- WorkStartFlowService skeleton과 PLC/DB seam을 PilotFlows-local로 추가했다.
- fake 기반 happy path와 대표 failure path를 고정했다.
- error write best-effort 정책을 service orchestration에 연결했다.
- Runtime / FlowDefinitions / WPF project를 수정하지 않았다.
- XGT / FakePlc / XgtChannelRunner / SqlClient reference를 추가하지 않았다.
- actual DB query, actual PLC read/write, adapter, executor, FLOW 정의 파일을 구현하지 않았다.
- 테스트, Runtime regression, solution build, boundary scan evidence를 확보했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
