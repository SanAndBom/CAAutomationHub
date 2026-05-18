# AH-PILOT-18-A Closeout - FakePlc WorkStart Full Transaction Harness

## 1. Summary

AH-PILOT-18-A에서는 `CAAutomationHub.PilotFlows.Xgt.Tests`에 FakePlc 기반 WorkStart happy path full transaction harness를 추가했다.

검증 흐름:

1. FakePlc `%DB10000` read block을 in-process XGT session으로 읽는다.
2. test-specific `StartSignalWordIndex = 83` override로 start signal active를 해석한다.
3. LOT ID 1 `"S0007652610B"`를 추출하고 선택한다.
4. test-local fake DB query가 selected LOT ID로 호출되고 `WorkStartProcessData`를 반환한다.
5. `WorkStartFlowService`가 `WorkStartProcessDataPayloadBuilder`로 payload를 만든다.
6. `WorkStartXgtPlcOperations.WriteProcessPayloadAsync(...)`가 `%DB11000`에 payload를 쓴다.
7. `WorkStartXgtPlcOperations.WriteStartAckAsync(...)`가 `%DB11416`에 ACK value `1`을 쓴다.
8. `WorkStartFlowResult.Success`가 반환된다.
9. FakePlc memory / write records로 payload, ACK, error non-write를 검증한다.

영향:

- production code는 수정하지 않았다.
- Runtime / FlowDefinitions / PilotFlows core는 수정하지 않았다.
- PilotFlows.Xgt production source는 수정하지 않았다.
- FakePlc reference는 test project에만 유지했다.
- FakePlc map 파일은 수정하지 않았다.
- actual PLC / actual DB / FLOW.JSON / Flow Executor 범위로 확장하지 않았다.

## 2. 변경 파일 목록

- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/WorkStart/WorkStartXgtFakePlcIntegrationTests.cs`
  - `RunAsync_CompletesHappyPath_WithFakePlcAndFakeDb` integration-style test를 추가했다.
  - test-local `FakeWorkStartDataQuery`를 추가했다.
  - test-local `CreateSampleProcessData(...)` fixture를 추가했다.
- `docs/harness/AH-PILOT-18-A.md`
  - AH-PILOT-18-A historical record를 추가했다.

## 3. FakePlc Full WorkStart Transaction 방식

기존 AH-PILOT-14-B / 17의 in-process FakePlc harness를 재사용했다.

test flow:

1. `CreateRuntime()`으로 `%DB10000`, `%DB11000`, `%DB11410`, `%DB11416`, `%DB11418` memory image를 구성한다.
2. `FakePlcScenarioInitializer.CreateMemoryImage(...)`로 LOT ID / start signal을 초기화한다.
3. `FakePlcRuntime`과 `FakePlcProtocolHandler`를 `InProcessFakePlcServer`에 연결한다.
4. `TcpTransport` + `XgtSession`으로 loopback FakePlc에 연결한다.
5. `WorkStartXgtPlcOperations(session, WorkStartXgtReadOptions.Default, WorkStartXgtWriteOptions.Default)`를 생성한다.
6. `WorkStartFlowService`에 real XGT operations와 test-local fake DB query를 주입한다.
7. `RunAsync()`를 실행한다.
8. result, fake query call, FakePlc records, FakePlc memory read-back을 검증한다.

## 4. Test-Specific Layout Override 사용 여부

사용했다.

Test-specific layout:

- `StartSignalWordIndex = 83`
- `LotId1WordOffset = 0`
- `LotId2WordOffset = 10`
- `LotIdWordLength = 6`

의미:

- current FakePlc initializer의 start signal은 `D5083`에 있다.
- `WorkStartXgtReadOptions.Default`는 `%DB10000` / `90` words를 읽는다.
- `%DB10000` 기준 index `83`은 `D5083`에 대응한다.
- CAAutomationHub default `StartSignalWordIndex = 80`은 수정하지 않았다.

## 5. Fake DB Query 사용 여부

사용했다.

구현:

- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/WorkStart/WorkStartXgtFakePlcIntegrationTests.cs` 내부의 test-local `FakeWorkStartDataQuery`
- `IWorkStartDataQuery`만 구현한다.
- actual DB connection, SQL query, `Microsoft.Data.SqlClient` reference는 추가하지 않았다.

검증:

- fake query가 selected LOT ID `"S0007652610B"`로 정확히 1회 호출됨을 `QueriedLotIds`로 확인했다.
- query result의 `WorkStartProcessData.LotId`가 null이어도 service가 selected LOT ID로 payload data를 정렬하는 기존 contract를 검증했다.

## 6. 검증한 Read / LOT ID / DB / Payload / ACK 흐름

검증한 result:

- `result.Succeeded == true`
- `result.Step == WorkStartStep.Completed`
- `result.ErrorCode == WorkStartErrorCode.None`
- `result.SelectedLotId == "S0007652610B"`
- `result.ErrorWriteExpected == false`

검증한 transaction:

- FakePlc read block에서 start signal active를 해석했다.
- LOT ID 1을 selected LOT ID로 선택했다.
- fake DB query가 selected LOT ID로 호출됐다.
- `WorkStartProcessDataPayloadBuilder.Build(...)` 결과와 FakePlc `%DB11000` write payload가 동일했다.
- ACK value `1`이 `%DB11416`에 little-endian 1 word로 기록됐다.

## 7. 검증한 FakePlc Memory / Records

사용한 FakePlc evidence:

- `FakePlcRuntime.LastBulkWrite`
- `FakePlcRuntime.LastAckValue`
- `FakePlcRuntime.LastErrorCode`
- `FakePlcRuntime.ReadContinuous(...)`

검증:

- `LastBulkWrite == expectedPayload`
- `ReadContinuous("%DB11000", expectedPayload.Length) == expectedPayload`
- `LastAckValue == 1`
- `ReadContinuous("%DB11416", 2) == { 0x01, 0x00 }`
- `LastErrorCode == null`
- `ReadContinuous("%DB11410", 2) == { 0x00, 0x00 }`

## 8. Error Write 미호출 확인 여부

확인했다.

근거:

- happy path result의 `ErrorWriteExpected`가 false다.
- `FakePlcRuntime.LastErrorCode`가 null이다.
- FakePlc error target `%DB11410` read-back 값이 `{ 0x00, 0x00 }`으로 유지된다.

## 9. 이식하지 않은 범위

이번 작업에서 제외했다.

- Runtime project 수정
- FlowDefinitions project 수정
- PilotFlows core 수정
- PilotFlows.Xgt production source 수정
- production project FakePlc reference 추가
- XgtChannelRunner reference 추가
- Microsoft.Data.SqlClient 추가
- actual DB query 구현
- actual PLC read/write 테스트
- FLOW.JSON 연결
- JSON parser / schema 구현
- Flow Executor 구현
- RuntimeSnapshot / ChannelPollingResult 참조
- FakePlc map 파일 수정
- WorkStartPilotService source copy
- DB failure integration
- payload failure integration
- ACK failure integration
- error write failure integration
- read NAK / malformed integration

## 10. 테스트 결과

Focused transaction test:

```text
dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj --filter RunAsync_CompletesHappyPath_WithFakePlcAndFakeDb
```

결과:

```text
failed 0, passed 1, skipped 0, total 1
```

Required tests:

```text
dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj
dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj
dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj
```

결과:

```text
CAAutomationHub.PilotFlows.Xgt.Tests:
  failed 0, passed 33, skipped 0, total 33

CAAutomationHub.PilotFlows.Tests:
  failed 0, passed 40, skipped 0, total 40

CAAutomationHub.Runtime.Tests:
  failed 0, passed 142, skipped 0, total 142
```

TDD note:

- 이번 작업은 production implementation 추가가 아니라 existing WorkStartFlowService / XGT operations / FakePlc write support를 full transaction harness로 연결한 test-only 단계다.
- 신규 production code는 작성하지 않았다.
- 새 transaction test의 첫 실행은 통과했다. 이는 AH-PILOT-05 / 16 / 17의 구현과 FakePlc support가 이미 준비되어 있었기 때문이다.

## 11. 빌드 결과

실행:

```text
dotnet build CAAutomationHub.sln
```

결과:

```text
Build succeeded.
0 warnings
0 errors
```

## 12. Boundary Scan 결과

실행:

```text
git diff --check
rg -n "XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src tests
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
```

결과:

- `git diff --check`는 통과했다.
- 전체 `src tests` boundary scan은 기존 Runtime / WPF contract와 tests의 `RuntimeSnapshot`, `ChannelPollingResult`, `Json` hit를 출력했다.
- 이번 변경 파일에는 금지 boundary hit가 없다.
- project reference scan에서 FakePlc reference는 기존 `tests/CAAutomationHub.PilotFlows.Xgt.Tests` test project에만 존재한다.
- Runtime / FlowDefinitions / PilotFlows core에는 FakePlc reference가 없다.
- PilotFlows.Xgt production project에는 FakePlc reference가 없다.
- XgtChannelRunner reference는 없다.
- Microsoft.Data.SqlClient / SqlConnection reference는 없다.

## 13. 다음 후보

- AH-PILOT-18-B: FakePlc 기반 WorkStart failure transaction 후보
  - DB not found / multiple rows / exception
  - payload build failure
  - ACK write failure
  - error write best-effort evidence
- AH-PILOT-19: read NAK / malformed response / deterministic FakePlc failure injection boundary review
- ACK 후 `ClearStartSignalOnAck`에 따른 `D5083` clear 확인을 별도 WorkStart-specific integration으로 분리 가능

## 14. 실행한 명령

Precheck:

- `git log --oneline -8`
- `git status --short`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content docs\harness\AH-PILOT-14-B.md`
- `Get-Content docs\harness\AH-PILOT-16.md`
- `Get-Content docs\harness\AH-PILOT-17.md`
- `Get-ChildItem src\CAAutomationHub.PilotFlows\WorkStart -Recurse`
- `Get-ChildItem src\CAAutomationHub.PilotFlows.Xgt\WorkStart -Recurse`
- `Get-ChildItem tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart -Recurse`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowService.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartProcessData.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartProcessDataPayloadBuilder.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\IWorkStartDataQuery.cs`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtPlcOperations.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart\WorkStartXgtFakePlcIntegrationTests.cs`
- `rg -n "LastBulkWrite|LastAckValue|LastErrorCode|ReadContinuous|SnapshotBlocks|FakePlc" tests src`
- `rg -n "WorkStartFlowService|IWorkStartDataQuery|WorkStartProcessData|WorkStartXgtPlcOperations" tests src`

Sibling repo read-only:

- `rg -n "LastBulkWrite|LastAckValue|LastErrorCode|ReadContinuous|SnapshotBlocks|FakePlcScenarioServer|FakePlcProtocolHandler|ReadContinuous" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcRuntime.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcMemoryImage.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcProtocolHandler.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcRuleConfig.cs`

Implementation / validation:

- `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj --filter RunAsync_CompletesHappyPath_WithFakePlcAndFakeDb`
- `git diff -- tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart\WorkStartXgtFakePlcIntegrationTests.cs`
- `git status --short`
- `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
- `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
- `dotnet build CAAutomationHub.sln`
- `git diff --check`
- `rg -n "XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src tests`
- `rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"`
- `git status --short`
- `git diff --cached --check`
- `git diff --cached --name-only`
- `git commit -m "AH-PILOT-18-A add fakeplc workstart transaction harness"`
- `git log --oneline -5`

## 15. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-18-A 목표인 FakePlc 기반 full WorkStart happy path transaction harness를 test-only 영역에 추가했다.
- service orchestration은 real `WorkStartXgtPlcOperations`와 in-process FakePlc protocol handler를 통해 검증했다.
- test-specific `StartSignalWordIndex = 83` override를 사용했고 production default는 수정하지 않았다.
- fake DB query는 test-local `IWorkStartDataQuery` 구현으로 제한했다.
- selected LOT ID, DB query call, process payload write, ACK write, success result를 하나의 integration-style test로 검증했다.
- error write 미호출을 `LastErrorCode == null`과 `%DB11410` read-back으로 확인했다.
- Runtime / FlowDefinitions / PilotFlows core / PilotFlows.Xgt production source를 수정하지 않았다.
- FakePlc map을 수정하지 않았다.
- FakePlc reference는 test project에만 유지했다.
- XgtChannelRunner / DB / SqlClient / FLOW.JSON / JSON parser / FlowExecutor / RuntimeSnapshot / ChannelPollingResult 범위로 확장하지 않았다.
- 요구된 tests, build, diff check, boundary scan evidence를 남겼다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
