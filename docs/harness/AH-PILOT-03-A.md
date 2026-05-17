# AH-PILOT-03-A Closeout - WorkStart LOT ID Read Block Interpreter

## 1. Summary

AH-PILOT-03-A는 `XgtChannelRunner`의 `WorkStartPilotService.RunOnceAsync(...)`에서 확인한 WorkStart read block 판단 규칙을 `CAAutomationHub.PilotFlows.WorkStart` 계층의 순수 helper로 재구성한 구현 작업이다.

추가한 helper는 이미 읽힌 `byte[]` read block만 입력으로 받는다. PLC read, XGT parser, DB query, Flow Executor, ACK writer, error writer, Runtime snapshot 연결은 수행하지 않는다.

기존 source에서 확인한 규칙은 다음과 같다.

- start signal word index 기본값은 `80`.
- start signal word는 1 word = 2 bytes 기준 offset으로 읽고, word 값이 `0`이 아니면 active.
- positive out-of-range start signal index는 inactive.
- LOT ID 1 word offset은 `0`.
- LOT ID 2 word offset은 `10`.
- LOT ID word length는 `6`.
- LOT ID는 ASCII decoding 후 leading/trailing `\0`과 space를 trim.
- positive out-of-range LOT ID range는 empty.
- LOT ID selection은 LOT ID 1 우선, 없으면 LOT ID 2, 둘 다 empty이면 no selection.

## 2. 변경 파일 목록

- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartReadBlockLayout.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartReadBlockInterpreter.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartLotIdExtractionResult.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartLotIdSelectionResult.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartLotIdSelectionSource.cs`
- `tests/CAAutomationHub.PilotFlows.Tests/WorkStart/WorkStartReadBlockInterpreterTests.cs`
- `docs/harness/AH-PILOT-03-A.md`

## 3. 추가한 helper / model 요약

`WorkStartReadBlockInterpreter`

- read block `byte[]`에서 start signal active 여부를 판단한다.
- read block `byte[]`에서 지정 word offset / word length 기준 LOT ID를 추출한다.
- LOT ID 1 / LOT ID 2 selection rule을 수행한다.
- XGT raw frame, parser, channel, DB, Runtime type을 모른다.

`WorkStartReadBlockLayout`

- 기존 `PilotScenarioConfig`에서 확인한 default read block layout만 PilotFlows-local constants로 둔다.
- `%DB10000` 같은 PLC address는 포함하지 않는다.

`WorkStartLotIdExtractionResult`

- 추출된 `LotId`, `WordOffset`, `WordLength`, `IsInRange`를 담는다.
- out-of-range는 `LotId = string.Empty`, `IsInRange = false`로 표현한다.

`WorkStartLotIdSelectionResult` / `WorkStartLotIdSelectionSource`

- 선택된 LOT ID와 source(`LotId1`, `LotId2`, `None`)를 담는다.
- 둘 다 empty이면 `SelectedLotId = null`, `Source = None`이다.

## 4. 기존 WorkStartPilotService에서 확인한 규칙

확인한 sibling repo 파일:

- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\WorkStartPilotService.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\PilotScenarioConfig.cs`

확인한 기존 method:

- `IsStartSignalActive(byte[] readBytes, PilotScenarioConfig config)`
- `ExtractLotId(byte[] readBytes, int wordOffset, int wordLength)`
- `SelectLotId(string lotId1, string lotId2)`

기존 implementation은 `BitConverter.ToUInt16(readBytes, offset)`와 `Encoding.ASCII.GetString(readBytes, byteOffset, byteLength).Trim('\0', ' ')`를 사용한다. CAAutomationHub helper는 XGT parser 없이 같은 byte-level 의미를 explicit little-endian read와 ASCII decoding으로 재구성했다.

## 5. start signal 판단 규칙

- 입력: `byte[] readBlockBytes`, `int startSignalWordIndex`.
- `startSignalWordIndex * 2`를 byte offset으로 사용한다.
- 2 bytes를 unsigned 16-bit little-endian word로 읽는다.
- 값이 `0`이 아니면 active.
- range 밖이면 inactive.
- `null` input은 `ArgumentNullException`.

## 6. LOT ID extraction 규칙

- 입력: `byte[] readBlockBytes`, `int wordOffset`, `int wordLength`.
- `wordOffset * 2`를 byte offset으로 사용한다.
- `wordLength * 2` bytes를 ASCII 문자열로 해석한다.
- 문자열 양 끝의 `\0`과 space를 trim한다.
- range 밖이면 empty result로 처리한다.
- `null` input은 `ArgumentNullException`.

## 7. LOT ID selection 규칙

- LOT ID 1이 non-empty / non-whitespace이면 LOT ID 1을 선택한다.
- LOT ID 1이 empty이고 LOT ID 2가 non-empty이면 LOT ID 2를 선택한다.
- 둘 다 empty이면 no selection이다.
- 선택 실패는 DB query나 error write를 수행하지 않고 pure result로만 표현한다.

## 8. 이식하지 않은 범위

이번 작업에서는 다음을 구현하지 않았다.

- WorkStartPilotService
- PilotFlowService
- ProcessDataPayloadBuilder 수정
- DB query abstraction / SQL text / connection string
- XGT operation adapter
- XGT frame builder / parser
- PlcChannel / XgtDriverCore / XgtChannelRunner reference
- FakePlc reference
- actual PLC read/write
- ACK writer
- error code writer
- Flow Executor
- FLOW.JSON / JSON schema / parser
- RuntimeSnapshot / ChannelPollingResult 연결
- Runtime project 수정
- FlowDefinitions project 수정

## 9. 테스트 결과

TDD RED:

- 명령: `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- 결과: expected compile failure.
- 근거: `WorkStartReadBlockLayout`, `WorkStartReadBlockInterpreter`, `WorkStartLotIdSelectionSource` type 미존재.

Focused GREEN:

- 명령: `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- 결과: pass.
- 15 tests passed, 0 failed, 0 skipped.

기존 Runtime tests:

- 명령: `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
- 결과: pass.
- 142 tests passed, 0 failed, 0 skipped.

## 10. 빌드 결과

- 명령: `dotnet build CAAutomationHub.sln`
- 결과: pass.
- warnings: 0.
- errors: 0.

## 11. boundary scan 결과

Boundary scan:

- 명령: `rg -n "Xgt|FakePlc|XgtChannelRunner|PlcChannel|XgtFrame|TcpTransport|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.PilotFlows tests\CAAutomationHub.PilotFlows.Tests`
- 결과: no matches.
- `rg` exit code 1은 no-match 의미로 해석.

Project reference scan:

- 명령: `rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"`
- 결과:
  - `src/CAAutomationHub.PilotFlows/CAAutomationHub.PilotFlows.csproj`에는 ProjectReference / PackageReference 없음.
  - `tests/CAAutomationHub.PilotFlows.Tests`는 xUnit test packages와 `CAAutomationHub.PilotFlows`만 참조.
  - Runtime / FlowDefinitions 기존 reference 구조 변경 없음.

Runtime / FlowDefinitions diff check:

- 명령: `git diff -- src\CAAutomationHub.Runtime src\CAAutomationHub.FlowDefinitions`
- 결과: no diff.

## 12. 다음 후보

- AH-PILOT-03-B: WorkStart flow result / error code policy skeleton review.
- AH-PILOT-04 후보: `IWorkStartPlcOperations` / `IWorkStartDataQuery` boundary review.
- 후속 구현 후보: read block helper와 payload builder를 사용하는 minimal WorkStart flow skeleton. 단, XGT / DB implementation은 별도 adapter 단계로 유지한다.

## 13. Self-Check

판정: `ACCEPT`

근거:

- WorkStart read block interpretation helper를 PilotFlows-local pure helper로 추가했다.
- start signal, LOT ID 1 / LOT ID 2 extraction, LOT ID selection rule을 tests로 고정했다.
- 기존 `WorkStartPilotService`를 source copy하지 않았다.
- Runtime project와 FlowDefinitions project를 수정하지 않았다.
- XgtDriverCore / FakePlc / XgtChannelRunner reference를 추가하지 않았다.
- DB query, Flow Executor, ACK writer, error writer, actual PLC read/write를 구현하지 않았다.
- Focused tests, Runtime tests, solution build, boundary scan, project reference scan을 실행했다.

주의:

- Codex Self-Check는 작업자 자기검증이며 최종 승인 여부는 사용자 검토 후 결정된다.
