# AH-PILOT-02 Closeout - WorkStart Process Data Payload Builder

## 1. Summary

AH-PILOT-02는 `XgtChannelRunner`의 `ProcessDataPayloadBuilder`를 source copy하지 않고, 검증된 WorkStart process data payload packing rule만 `CAAutomationHub.PilotFlows` 계층에 선택 이식한 구현 작업이다.

새 `CAAutomationHub.PilotFlows` project와 `CAAutomationHub.PilotFlows.Tests` project를 추가했다. helper는 LOT ID와 process data를 PLC write payload byte array로 packing하지만, write address, XGT channel/session, DB query, Runtime snapshot, Flow Executor, ACK/error writer는 소유하지 않는다.

TDD 흐름으로 먼저 CAAutomationHub 기준 payload tests를 작성했고, RED는 `CAAutomationHub.PilotFlows.WorkStart` namespace와 `WorkStartProcessData` type 미존재 compile failure로 확인했다. 이후 최소 model/helper를 구현해 GREEN을 확인했다.

## 2. 변경 파일 목록

- `CAAutomationHub.sln`
- `src/CAAutomationHub.PilotFlows/CAAutomationHub.PilotFlows.csproj`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartProcessData.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartPayloadBuildOptions.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartPayloadBuildResult.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartPayloadField.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartProcessDataPayloadBuilder.cs`
- `tests/CAAutomationHub.PilotFlows.Tests/CAAutomationHub.PilotFlows.Tests.csproj`
- `tests/CAAutomationHub.PilotFlows.Tests/WorkStart/WorkStartProcessDataPayloadBuilderTests.cs`
- `docs/harness/AH-PILOT-02.md`

## 3. Project / Namespace 배치 이유

배치는 `src/CAAutomationHub.PilotFlows`와 `tests/CAAutomationHub.PilotFlows.Tests`를 선택했다.

이유:

- Runtime core에 LOT ID / payload layout / business transaction detail을 넣지 않기 위함.
- FlowDefinitions project를 payload implementation으로 오염시키지 않기 위함.
- Pilot business flow 구현 라인을 Runtime polling state path와 분리하기 위함.
- 새 production project가 Runtime / WPF / FlowDefinitions / XGT / FakePlc / XgtChannelRunner reference를 갖지 않는다는 점을 project reference scan으로 직접 확인하기 위함.

Namespace는 `CAAutomationHub.PilotFlows.WorkStart`를 사용했다. 이번 helper는 WorkStart process data payload에 한정되므로 `Payloads` 같은 더 넓은 namespace보다 현재 의미가 선명하다.

## 4. 추가한 모델 / Helper 요약

- `WorkStartProcessData`
  - LOT ID와 WorkStart process data field를 담는 input model.
  - DB, SQL, XGT, Runtime dependency 없음.
- `WorkStartPayloadBuildOptions`
  - 현재는 payload `WordCount`만 소유.
  - 기본값은 70 words / 140 bytes.
  - write address는 포함하지 않음.
- `WorkStartProcessDataPayloadBuilder`
  - static pure helper 성격.
  - input model과 options로 `WorkStartPayloadBuildResult` 생성.
  - PLC write, DB query, Runtime state publish, ACK/error write를 수행하지 않음.
- `WorkStartPayloadBuildResult`
  - `PayloadBytes`, `WordCount`, field metadata를 담음.
- `WorkStartPayloadField`
  - field name, start word offset, word length, packed bytes metadata.

## 5. 기존 XgtChannelRunner에서 가져온 Rule

기존 `ProcessDataPayloadBuilderTests`와 builder source에서 확인한 아래 packing rule만 재구성했다.

- payload length: `WordCount * 2`, 기본 70 words / 140 bytes.
- `LOTID_1`: byte offset 0, 12 bytes, 6 words, fixed ASCII.
- `LOTID_2`: byte offset 20, 12 bytes, 6 words, 현재 empty fixed ASCII.
- `PROFILE`: byte offset 80, 18 bytes, 9 words, fixed ASCII.
- `TBLR`: byte offset 100, 1 word, first trimmed ASCII byte + zero byte.
- `WIN_TYPE`: byte offset 104, 1 word, first trimmed ASCII byte + zero byte.
- `CUT_SIZE`: byte offset 108, two-word little-endian Int32.
- `LR`: byte offset 112, 1 word, first trimmed ASCII byte + zero byte.
- `RollerYN`: byte offset 116, 1 word, first trimmed ASCII byte + zero byte.
- `ROLLER_HOLE_POS`: byte offset 120, two-word little-endian Int32.
- `ROLLER_HOLE_WIDTH`: byte offset 124, two-word little-endian Int32.
- `ROLLER_HOLE_LENGTH`: byte offset 128, two-word little-endian Int32.
- `ROLLER_TYPE`: byte offset 132, 1 word, first trimmed ASCII byte + zero byte.
- `CUT_DEGREE`: byte offset 136, two-word little-endian Int32.
- fixed ASCII field는 trim 후 field length까지 copy하고 남은 bytes는 zero padding.
- fixed ASCII field가 field length보다 길면 field length 기준 truncate.
- null / blank string은 zero bytes로 packing.
- `CUT_SIZE`는 기존 source의 TODO를 유지해 별도 scaling을 추가하지 않았다.

## 6. 의도적으로 가져오지 않은 것

- `PilotScenarioConfig`
- `%DB11000` write target address
- XGT data type / count / session / raw frame
- `PlcChannel`
- `XgtDriverCore`
- `FakePlc`
- `XgtChannelRunner`
- DB query service / SQL text / connection string
- `WorkStartPilotService`
- ACK writer
- error code writer
- Flow Executor
- FLOW.JSON / JSON schema / parser
- RuntimeSnapshot / ChannelPollingResult 연결

## 7. 테스트 결과

TDD RED:

- `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- 결과: expected compile failure
- 근거: `CAAutomationHub.PilotFlows.WorkStart` namespace와 `WorkStartProcessData` type 미존재.

Focused GREEN:

- `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- 결과: pass
- 6 tests passed, 0 failed, 0 skipped.

기존 Runtime tests:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
- 결과: pass
- 142 tests passed, 0 failed, 0 skipped.

## 8. 빌드 결과

- `dotnet build CAAutomationHub.sln`
- 결과: pass
- warnings: 0
- errors: 0

## 9. Boundary Scan 결과

Scoped new-path scan:

- `rg -n "Xgt|FakePlc|XgtChannelRunner|PlcChannel|XgtFrame|TcpTransport|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.PilotFlows tests\CAAutomationHub.PilotFlows.Tests`
- 결과: no matches
- exit code 1은 ripgrep의 no-match 의미로 해석.

Requested broad scan:

- `rg -n "Xgt|FakePlc|XgtChannelRunner|PlcChannel|XgtFrame|TcpTransport|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.PilotFlows tests`
- 결과: existing Runtime/WPF test tree에서 `RuntimeSnapshot`, `ChannelPollingResult`, 기존 boundary test 문자열 등 다수 match.
- AH-PILOT-02 신규 production source와 신규 PilotFlows tests에는 match 없음.

Project reference scan:

- `rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"`
- 결과:
  - `src/CAAutomationHub.PilotFlows/CAAutomationHub.PilotFlows.csproj`에는 ProjectReference / PackageReference 없음.
  - `tests/CAAutomationHub.PilotFlows.Tests`는 xUnit test packages와 `CAAutomationHub.PilotFlows`만 참조.
  - Runtime / FlowDefinitions 기존 reference 구조 변경 없음.

Runtime / FlowDefinitions diff check:

- `git diff -- src\CAAutomationHub.Runtime src\CAAutomationHub.FlowDefinitions tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj src\CAAutomationHub.FlowDefinitions\CAAutomationHub.FlowDefinitions.csproj`
- 결과: no diff.

## 10. 제외한 범위

이번 작업에서는 다음을 구현하지 않았다.

- WorkStartPilotService
- PilotFlowService
- DB query abstraction
- XGT operation adapter
- ACK writer
- error code writer
- 실제 PLC write
- FLOW.JSON 연결
- Flow Executor 연결
- RuntimeSnapshot 연결
- ChannelPollingResult 연결
- XgtDriverCore / FakePlc / XgtChannelRunner reference
- SQL connection string / SQL text
- WinForms UI code

## 11. 다음 후보

- AH-PILOT-03 후보 A: LOT ID extraction / selection helper + tests.
- AH-PILOT-03 후보 B: WorkStart flow result / error code policy skeleton review.
- AH-PILOT-03 후보 C: `IWorkStartPlcOperations` 또는 `IWorkStartDataQuery` boundary review. 단, 실제 XGT / DB 구현은 별도 단계로 유지.

## 12. Self-Check

판정: `ACCEPT`

근거:

- ProcessDataPayloadBuilder를 source copy하지 않고 packing rule만 PilotFlows-local helper로 재구성했다.
- Runtime project와 FlowDefinitions project를 수정하지 않았다.
- ChannelPollingTarget / ChannelPollingResult / RuntimeSnapshot을 건드리지 않았다.
- XgtDriverCore / FakePlc / XgtChannelRunner reference를 추가하지 않았다.
- DB query, Flow Executor, PLC write, ACK/error writer를 구현하지 않았다.
- 기존 XgtChannelRunner tests에서 확인된 ASCII, single-word char, little-endian Int32, padding/truncation, payload length rule을 CAAutomationHub tests로 고정했다.
- Runtime tests, PilotFlows tests, solution build, diff check, boundary scan, project reference scan을 실행했다.

주의:

- Codex Self-Check는 작업자 자기검증이며 최종 승인 여부는 사용자 검토 후 결정된다.
