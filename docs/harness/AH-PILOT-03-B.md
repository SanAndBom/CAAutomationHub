# AH-PILOT-03-B Closeout - WorkStart Result and Error Policy Skeleton

## 1. Summary

AH-PILOT-03-B는 `XgtChannelRunner`의 `WorkStartPilotService.RunOnceAsync(...)`에서 확인한 WorkStart step / error code / error write policy를 `CAAutomationHub.PilotFlows.WorkStart` 계층의 runtime-independent business result model로 재구성한 구현 작업이다.

이번 변경은 model / policy helper / tests에만 한정했다. XGT channel, DB query, Runtime, FlowExecutor, ACK writer, error writer, PLC read/write, SQL text, XGT address는 추가하지 않았다.

핵심 영향:

- WorkStart flow step closed set을 `WorkStartStep` enum으로 표현했다.
- 기존 원본 string step code는 `WorkStartStepExtensions.ToCode()`로 보존했다.
- WorkStart error code closed set을 numeric value가 고정된 `WorkStartErrorCode` enum으로 표현했다.
- error code별 error write 수행 여부를 `WorkStartErrorWritePolicy.ShouldWriteErrorCode(...)`로 고정했다.
- `WorkStartFlowResult`는 success/failure skeleton만 담고 raw diagnostic hex, reconnect count, DB result, payload bytes, Runtime state는 담지 않는다.

## 2. 변경 파일 목록

- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartStep.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartStepExtensions.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartErrorCode.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartErrorWritePolicy.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartFlowStatus.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartFlowResult.cs`
- `tests/CAAutomationHub.PilotFlows.Tests/WorkStart/WorkStartFlowResultPolicyTests.cs`
- `docs/harness/AH-PILOT-03-B.md`

## 3. 추가한 model / policy helper 요약

`WorkStartStep`

- WorkStart flow의 현재 closed set step을 enum으로 표현한다.
- 원본 step string 자체를 enum 이름으로 직접 encode하지 않고, `ToCode()`에서 `group-read`, `db-query` 같은 외부 code를 제공한다.

`WorkStartErrorCode`

- 원본 numeric error code를 enum value로 고정한다.
- 업무 policy에서 code identity가 중요하므로 `int` cast로 원본 numeric code를 바로 얻을 수 있다.

`WorkStartErrorWritePolicy`

- error write 대상인지 여부만 판단한다.
- `%DB11410` 같은 target address는 포함하지 않는다.
- actual write와 best-effort write는 수행하지 않는다.

`WorkStartFlowStatus`

- result status를 `Succeeded` / `Failed`로 최소 표현한다.

`WorkStartFlowResult`

- `Succeeded`, `Status`, `Step`, `ErrorCode`, `Message`, `SelectedLotId`, `ErrorWriteExpected`만 가진다.
- `Success(...)` / `Failure(...)` factory로 completed / failed result를 만들 수 있다.
- `ErrorWriteExpected`는 `WorkStartErrorWritePolicy`에서 계산한다.

## 4. 기존 WorkStartPilotService에서 확인한 step / error code / error write policy

| Condition | Step | Error Code | Error Write |
| --- | --- | --- | --- |
| read NAK / malformed | `group-read` | `1101` | No |
| read response parse failure / empty | `group-read-parse` | `1102` | No |
| start signal inactive / index out of range | `start-signal` | `1200` | No |
| LOT ID 1/2 both empty | `lotid` | `2201` | Yes |
| DB exception | `db-query` | `2300` | Yes |
| DB not found | `db-query` | `2301` | Yes |
| DB multiple rows | `db-query` | `2302` | Yes |
| DB other failed result | `db-query` | `2303` | Yes |
| payload build failure | `payload-build` | `2400` | Yes |
| bulk write failure | `bulk-write` | `2501` | Yes |
| ACK write failure | `ack-write` | `2601` | Yes |
| unexpected exception | `exception` | `2999` | No |
| success | `completed` | `None` | No |

원본 runner의 error write target은 `%DB11410`이지만, 이번 단계에서는 address를 model에 넣지 않았다. target address는 adapter / binding / options 계층 책임으로 남긴다.

## 5. WorkStart step 표현 방식

`WorkStartStep`은 enum으로 선택했다.

선택 이유:

- 현재 step은 `WorkStartPilotService.RunOnceAsync(...)`에서 확인된 closed set이다.
- step별 branching / result assertion에 문자열 오타가 끼어들 가능성을 줄일 수 있다.
- 원본 runner의 string step code는 `ToCode()`로 보존해 historical anchor와 로그 projection에 쓸 수 있다.
- 확장 가능성이 생겨도 enum member 추가와 `ToCode()` test 추가로 변경 범위를 명확하게 만들 수 있다.

## 6. WorkStart error code 표현 방식

`WorkStartErrorCode`는 numeric enum으로 선택했다.

선택 이유:

- error code 자체가 WorkStart business policy identity에 가깝다.
- tests에서 `(int)WorkStartErrorCode.DbNotFound == 2301`처럼 원본 numeric code를 고정할 수 있다.
- PLC별 override / binding 계층이 필요해지더라도 이번 enum은 verified default policy anchor로 유지할 수 있다.
- string/int loose model보다 closed set의 의미가 선명하다.

## 7. Error write policy

Error write 수행:

- `LotIdEmpty = 2201`
- `DbException = 2300`
- `DbNotFound = 2301`
- `DbMultipleRows = 2302`
- `DbFailed = 2303`
- `PayloadBuildFailed = 2400`
- `BulkWriteFailed = 2501`
- `AckWriteFailed = 2601`

Error write 미수행:

- `None = 0`
- `ReadFailed = 1101`
- `ReadParseFailed = 1102`
- `StartSignalInactive = 1200`
- `UnexpectedException = 2999`

정책은 `WorkStartErrorWritePolicy.ShouldWriteErrorCode(...)`로 고정했다. 이번 단계에서는 error write address, actual write, best-effort retry, adapter side effect를 모두 제외했다.

## 8. WorkStart result skeleton

`WorkStartFlowResult` 최소 필드:

- `Status`
- `Succeeded`
- `Step`
- `ErrorCode`
- `Message`
- `SelectedLotId`
- `ErrorWriteExpected`

추가한 factory:

- `WorkStartFlowResult.Success(string? selectedLotId)`
- `WorkStartFlowResult.Failure(WorkStartStep step, WorkStartErrorCode errorCode, string? message, string? selectedLotId)`

의도적으로 제외한 필드:

- `RequestHex`
- `ResponseHex`
- `ElapsedMs`
- `ReconnectAttemptCount`
- DB result object
- payload bytes
- RuntimeSnapshot
- ChannelPollingResult

## 9. 이식하지 않은 범위

이번 작업에서는 다음을 구현하지 않았다.

- WorkStartPilotService
- PilotFlowService
- ProcessDataPayloadBuilder 수정
- WorkStartReadBlockInterpreter 수정
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
- WinForms UI code 이식

## 10. 테스트 결과

TDD RED:

- 명령: `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- 결과: expected compile failure.
- 근거: `WorkStartErrorCode`, `WorkStartErrorWritePolicy`, `WorkStartStep`, `WorkStartFlowResult`, `WorkStartFlowStatus` type 미존재.

Focused GREEN:

- 명령: `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- 결과: pass.
- 23 tests passed, 0 failed, 0 skipped.

기존 Runtime tests:

- 명령: `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
- 결과: pass.
- 142 tests passed, 0 failed, 0 skipped.

## 11. 빌드 결과

- 명령: `dotnet build CAAutomationHub.sln`
- 결과: pass.
- warnings: 0.
- errors: 0.

## 12. boundary scan 결과

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

## 13. git diff --check 결과

- 명령: `git diff --check`
- 결과: pass.
- whitespace error 없음.

주의:

- 이번 변경 파일은 아직 untracked 상태이므로 `git diff --check`는 tracked diff만 검사한다.

## 14. git status --short 결과

실행:

- `git status --short`

결과:

```text
?? docs/harness/AH-PILOT-03-B.md
?? src/CAAutomationHub.PilotFlows/WorkStart/WorkStartErrorCode.cs
?? src/CAAutomationHub.PilotFlows/WorkStart/WorkStartErrorWritePolicy.cs
?? src/CAAutomationHub.PilotFlows/WorkStart/WorkStartFlowResult.cs
?? src/CAAutomationHub.PilotFlows/WorkStart/WorkStartFlowStatus.cs
?? src/CAAutomationHub.PilotFlows/WorkStart/WorkStartStep.cs
?? src/CAAutomationHub.PilotFlows/WorkStart/WorkStartStepExtensions.cs
?? tests/CAAutomationHub.PilotFlows.Tests/WorkStart/WorkStartFlowResultPolicyTests.cs
```

## 15. 다음 후보

- AH-PILOT-04 후보 A: WorkStart operation adapter / DB query boundary review.
- AH-PILOT-04 후보 B: read block helper, payload builder, result skeleton을 사용하는 minimal WorkStart flow skeleton. 단, XGT / DB implementation은 제외한다.
- AH-PILOT-04 후보 C: ACK / error writer target address를 adapter binding/options 계층으로 분리하는 boundary review.

## 16. Self-Check

판정: `ACCEPT`

근거:

- WorkStart step / error code / error write policy를 PilotFlows-local model과 helper로 추가했다.
- 기존 `WorkStartPilotService`를 source copy하지 않았다.
- Runtime project와 FlowDefinitions project를 수정하지 않았다.
- XgtDriverCore / FakePlc / XgtChannelRunner reference를 추가하지 않았다.
- DB query, Flow Executor, ACK writer, error writer, actual PLC read/write를 구현하지 않았다.
- `%DB11410` address를 result model에 넣지 않았다.
- RequestHex / ResponseHex / reconnect attempt count를 result skeleton에 넣지 않았다.
- Focused tests, Runtime tests, solution build, boundary scan, project reference scan을 실행했다.

주의:

- Codex Self-Check는 작업자 자기검증이며 최종 승인 여부는 사용자 검토 후 결정된다.
