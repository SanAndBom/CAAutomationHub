# AH-PILOT-06 Closeout - WorkStartFlowService Failure Path Expansion

## 1. Summary

AH-PILOT-06은 `WorkStartFlowService`의 fake 기반 failure path tests를 기존 `WorkStartPilotService.RunOnceAsync(...)`의 대표 실패 정책에 맞춰 확장한 구현 작업이다.

이번 변경은 `tests/CAAutomationHub.PilotFlows.Tests/WorkStart/WorkStartFlowServiceTests.cs` 보강에 한정했다. production service, Runtime project, FlowDefinitions project, project reference, XGT/DB concrete, FLOW.JSON, Flow Executor는 수정하지 않았다.

핵심 영향:

- DB multiple rows / failed / thrown exception failure path를 `DbQuery` step과 `2302` / `2303` / `2300` error code로 고정했다.
- ACK write false path를 `AckWriteFailed = 2601`로 고정했다.
- invalid payload build options로 자연스러운 payload build exception을 유도해 `PayloadBuildFailed = 2400`을 고정했다.
- read operation exception을 `GroupRead` / `ReadFailed = 1101`로 고정했다.
- error write 대상 failure에서 `WriteErrorCodeBestEffortAsync`가 호출되고, best-effort exception은 primary failure를 바꾸지 않는 기존 정책을 유지했다.

## 2. 변경 파일 목록

- `tests/CAAutomationHub.PilotFlows.Tests/WorkStart/WorkStartFlowServiceTests.cs`
- `docs/harness/AH-PILOT-06.md`

## 3. 추가 / 보강한 failure path

추가한 tests:

- `RunAsync_ReturnsDbMultipleRows_WhenQueryReturnsMultipleRows`
- `RunAsync_ReturnsDbFailed_WhenQueryReturnsFailed`
- `RunAsync_ReturnsDbException_WhenQueryThrows`
- `RunAsync_ReturnsPayloadBuildFailed_WhenPayloadOptionsAreInvalid`
- `RunAsync_ReturnsAckWriteFailed_WhenAckWriteFails`
- `RunAsync_ReturnsReadFailed_WhenReadThrows`

보강한 fake controls:

- `FakeWorkStartDataQuery.QueryException`
- `FakeWorkStartPlcOperations.ThrowOnRead`

기존 AH-PILOT-05의 `RunAsync_KeepsFailureResult_WhenBestEffortErrorWriteThrows`는 primary failure 보존 정책을 이미 검증하므로 중복 test를 추가하지 않았다.

## 4. DB multiple rows / failed / exception 처리

DB multiple rows:

- query result: `WorkStartDataQueryResult.MultipleRows(...)`
- result step: `WorkStartStep.DbQuery`
- error code: `WorkStartErrorCode.DbMultipleRows` / `2302`
- selected LOT ID 유지
- `ErrorWriteExpected = true`
- `WriteErrorCodeBestEffortAsync(DbMultipleRows)` 호출
- payload write / ACK write 미호출

DB failed:

- query result: `WorkStartDataQueryResult.Failed(...)`
- result step: `WorkStartStep.DbQuery`
- error code: `WorkStartErrorCode.DbFailed` / `2303`
- message 유지
- selected LOT ID 유지
- `ErrorWriteExpected = true`
- `WriteErrorCodeBestEffortAsync(DbFailed)` 호출
- payload write / ACK write 미호출

DB exception:

- `IWorkStartDataQuery.QueryAsync(...)`가 exception throw
- result step: `WorkStartStep.DbQuery`
- error code: `WorkStartErrorCode.DbException` / `2300`
- exception message 유지
- selected LOT ID 유지
- service가 exception을 밖으로 던지지 않고 failure result 반환
- `WriteErrorCodeBestEffortAsync(DbException)` 호출
- payload write / ACK write 미호출

## 5. ACK write failure 처리

ACK write failure:

- read, query, payload build, payload write까지 성공
- `WriteStartAckAsync(...)`가 false 반환
- result step: `WorkStartStep.AckWrite`
- error code: `WorkStartErrorCode.AckWriteFailed` / `2601`
- selected LOT ID 유지
- `ErrorWriteExpected = true`
- `WriteErrorCodeBestEffortAsync(AckWriteFailed)` 호출
- ACK write call count 1회 확인

## 6. payload build failure 처리 여부

처리함.

payload builder를 왜곡하거나 production seam을 추가하지 않고, `WorkStartPayloadBuildOptions.WordCount = 1`로 payload buffer를 너무 작게 만들어 기존 builder의 자연스러운 write failure를 유도했다.

검증 결과:

- result step: `WorkStartStep.PayloadBuild`
- error code: `WorkStartErrorCode.PayloadBuildFailed` / `2400`
- selected LOT ID 유지
- `ErrorWriteExpected = true`
- `WriteErrorCodeBestEffortAsync(PayloadBuildFailed)` 호출
- payload write / ACK write 미호출

## 7. read failure / parse failure 처리 여부

read failure는 처리함.

- `ReadWorkStartBlockAsync(...)` exception throw
- result step: `WorkStartStep.GroupRead`
- error code: `WorkStartErrorCode.ReadFailed` / `1101`
- selected LOT ID 없음
- `ErrorWriteExpected = false`
- DB query / payload write / ACK write / error write 미호출

read parse failure는 AH-PILOT-06에서 억지로 추가하지 않았다.

현재 `IWorkStartPlcOperations.ReadWorkStartBlockAsync(...)` seam은 이미 해석된 `byte[]`만 반환한다. 이 구조에서는 기존 `WorkStartPilotService`의 read NAK / malformed response / empty response parse failure 의미를 명확히 구분하는 operation result가 없다. empty or short byte block은 현재 read block interpreter 계약상 start signal inactive로 해석될 수 있으므로, fake test로 `1102`를 강제하면 의미가 흐려질 위험이 있다.

다음 단계 후보로 PLC operation result model review를 남긴다.

## 8. error write best-effort 처리

이번에 추가한 DB multiple rows / DB failed / DB exception / payload build failure / ACK write failure tests는 모두 `ErrorWriteExpected = true`와 `WrittenErrorCodes` 호출을 확인한다.

기존 AH-PILOT-05 test는 `WriteErrorCodeBestEffortAsync(...)`가 exception을 던져도 primary failure result가 유지됨을 검증한다.

이번 작업에서 error write concrete, target address, retry policy, diagnostics field는 추가하지 않았다.

## 9. 이식하지 않은 범위

이번 AH-PILOT-06에서 의도적으로 제외한 범위:

- `CAAutomationHub.Runtime` project 수정
- `CAAutomationHub.FlowDefinitions` project 수정
- XGT adapter concrete
- DB concrete
- `Microsoft.Data.SqlClient` reference
- actual PLC read/write 구현
- ACK writer concrete
- error writer concrete
- FLOW.JSON 파일 / schema / parser
- Flow Executor
- RuntimeSnapshot / ChannelPollingResult 참조
- ChannelPollingTarget / ChannelPollingResult 수정
- SQL connection string / SQL text
- XGT address / ACK value / error write target service model 추가
- WorkStartPilotService source copy

## 10. 테스트 결과

RED 확인:

- 명령: `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- 결과: expected compile failure
- 이유:
  - `FakeWorkStartDataQuery`에 `QueryException` 없음
  - `FakeWorkStartPlcOperations`에 `ThrowOnRead` 없음

GREEN 확인:

- 명령: `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- 결과: 통과
- 실패 0, 통과 36, 건너뜀 0

Runtime regression:

- 명령: `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
- 결과: 통과
- 실패 0, 통과 142, 건너뜀 0

## 11. 빌드 결과

- 명령: `dotnet build CAAutomationHub.sln`
- 결과: 통과
- 경고 0, 오류 0

## 12. boundary scan 결과

Boundary scan:

- 명령: `rg -n "Xgt|FakePlc|XgtChannelRunner|PlcChannel|XgtFrame|TcpTransport|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.PilotFlows tests\CAAutomationHub.PilotFlows.Tests`
- 결과: 출력 없음
- `rg` exit code 1은 no-match 의미로 해석

Project reference scan:

- 명령: `rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"`
- 확인:
  - `src/CAAutomationHub.PilotFlows/CAAutomationHub.PilotFlows.csproj`에는 `ProjectReference` / `PackageReference` 없음
  - `tests/CAAutomationHub.PilotFlows.Tests`는 xUnit / test SDK / coverlet packages와 `CAAutomationHub.PilotFlows`만 참조
  - Runtime / FlowDefinitions reference 구조 변경 없음

Runtime / FlowDefinitions diff check:

- 명령: `git diff -- src\CAAutomationHub.Runtime src\CAAutomationHub.FlowDefinitions`
- 결과: no diff

## 13. 다음 후보

- AH-PILOT-07 후보 A: PLC operation result model review
  - read operation failure / malformed / parse failure를 `byte[]` 반환만으로 표현할지, operation result model로 분리할지 검토
- AH-PILOT-07 후보 B: payload build options validation review
  - invalid payload word count를 사전에 명확한 validation failure로 만들지 검토
- AH-PILOT-07 후보 C: WorkStart operation adapter boundary review
  - XGT concrete 구현 전 address / ACK value / error target binding 위치 검토

## 14. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-06 목표인 WorkStartFlowService failure path expansion을 fake tests로 고정했다.
- DB multiple rows / failed / thrown exception이 기존 policy code `2302` / `2303` / `2300`으로 매핑됨을 검증했다.
- ACK write failure가 `2601`로 매핑되고 error write가 호출됨을 검증했다.
- payload build failure는 production code 왜곡 없이 invalid options로 검증했다.
- read failure `1101`은 검증했고, read parse failure `1102`는 현재 seam 의미상 억지 구현하지 않고 후속 review로 남겼다.
- Runtime / FlowDefinitions project를 수정하지 않았다.
- XgtDriverCore / FakePlc / XgtChannelRunner / SqlClient reference를 추가하지 않았다.
- actual DB query, actual PLC read/write, adapter, executor, FLOW.JSON을 구현하지 않았다.
- PilotFlows tests, Runtime tests, solution build, boundary scan, project reference scan evidence를 확보했다.

주의:

- Codex Self-Check는 작업자 자기검증이며 최종 승인 여부는 사용자 검토 후 결정된다.
