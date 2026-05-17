# AH-PILOT-08 Closeout - WorkStart Read Operation Result Model

## 1. Summary

AH-PILOT-08은 WorkStart read operation seam을 `byte[]` 단독 반환에서 최소 result model 반환으로 보강한 구현 작업이다.

`IWorkStartPlcOperations.ReadWorkStartBlockAsync(...)`는 이제 `WorkStartReadBlockOperationResult`를 반환한다. Service는 read operation failure를 `GroupRead / ReadFailed = 1101`로, read response parse 또는 payload extraction failure를 `GroupReadParse / ReadParseFailed = 1102`로 구분한다. Success일 때만 data block을 기존 `WorkStartReadBlockInterpreter`에 넘겨 start signal, LOT ID, DB query, payload write, ACK write 흐름을 계속 진행한다.

이번 변경은 PilotFlows-local read seam에 한정했다. Runtime, FlowDefinitions, XGT adapter, DB concrete, FLOW.JSON, Flow Executor, ChannelPollingResult, RuntimeSnapshot 경계는 수정하지 않았다.

## 2. 변경 파일 목록

- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartReadBlockOperationStatus.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartReadBlockOperationResult.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/IWorkStartPlcOperations.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartFlowService.cs`
- `tests/CAAutomationHub.PilotFlows.Tests/WorkStart/WorkStartFlowServiceTests.cs`
- `tests/CAAutomationHub.PilotFlows.Tests/WorkStart/WorkStartReadBlockOperationResultTests.cs`
- `docs/harness/AH-PILOT-08.md`

## 3. Read Operation Result Model

추가한 model:

- `WorkStartReadBlockOperationStatus`
  - `Success`
  - `OperationFailed`
  - `ParseFailed`
- `WorkStartReadBlockOperationResult`
  - `Status`
  - `Data`
  - `Message`
  - `Success(byte[] data)`
  - `OperationFailed(string? message = null)`
  - `ParseFailed(string? message = null)`

`Success(...)`는 전달받은 data를 내부 복사한다. `Data` property도 복사본을 반환해 외부 byte array 변경이 result 내부 상태를 바꾸지 않도록 했다.

Failure result는 data를 갖지 않는다. `OperationFailed`와 `ParseFailed`는 message만 선택적으로 보존한다.

XGT-specific classification, raw request/response hex, exception object, adapter detail, XGT address는 model에 넣지 않았다.

## 4. IWorkStartPlcOperations 변경

기존:

```csharp
ValueTask<byte[]> ReadWorkStartBlockAsync(CancellationToken cancellationToken = default);
```

변경:

```csharp
ValueTask<WorkStartReadBlockOperationResult> ReadWorkStartBlockAsync(
    CancellationToken cancellationToken = default);
```

다음 write seam은 유지했다.

- `WriteProcessPayloadAsync(...) -> bool`
- `WriteStartAckAsync(...) -> bool`
- `WriteErrorCodeBestEffortAsync(...)`

## 5. WorkStartFlowService 변경

`RunAsync(...)` read 구간을 다음 흐름으로 정렬했다.

1. `ReadWorkStartBlockAsync(...)` 호출
2. 호출 자체가 exception을 던지면 기존 AH-PILOT-06 동작대로 `GroupRead / 1101`
3. `Status.OperationFailed`이면 `GroupRead / 1101`
4. `Status.ParseFailed`이면 `GroupReadParse / 1102`
5. `Status.Success`이면 `Data`를 기존 interpreter에 전달
6. Success인데 `Data`가 null이면 방어적으로 `GroupReadParse / 1102`

기존 start signal inactive `1200`, LOT ID empty `2201`, DB failure, payload build, bulk write, ACK write 흐름은 유지했다.

## 6. 1101 / 1102 매핑

- `OperationFailed`
  - step: `WorkStartStep.GroupRead`
  - code: `WorkStartErrorCode.ReadFailed = 1101`
  - error write: No
- `ParseFailed`
  - step: `WorkStartStep.GroupReadParse`
  - code: `WorkStartErrorCode.ReadParseFailed = 1102`
  - error write: No
- read exception
  - step: `WorkStartStep.GroupRead`
  - code: `WorkStartErrorCode.ReadFailed = 1101`
  - error write: No
- read success
  - data block을 기존 WorkStart interpreter로 전달

## 7. 유지한 Write Bool Seam

AH-PILOT-07 권장대로 write 계열 result model은 도입하지 않았다.

- process payload write 실패는 기존 `BulkWrite / 2501`
- ACK write 실패는 기존 `AckWrite / 2601`
- error write best-effort 정책은 기존 유지

## 8. 이식하지 않은 범위

이번 작업에서 의도적으로 제외한 범위:

- XGT adapter concrete 구현
- DB concrete 구현
- `Microsoft.Data.SqlClient` 추가
- actual PLC read/write 구현
- ACK writer concrete 구현
- error writer concrete 구현
- FLOW.JSON 연결 또는 생성
- JSON schema / parser 구현
- Flow Executor 구현
- RuntimeSnapshot 연결
- ChannelPollingResult 연결
- ChannelPollingTarget 수정
- WorkStartPilotService source copy
- SQL connection string / SQL text 추가
- XGT address를 service / result model에 직접 추가
- XGT response classification enum 이식
- WorkStart write operation result model 도입

## 9. 테스트 결과

실행:

```text
dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj
```

결과:

```text
통과! - 실패: 0, 통과: 40, 건너뜀: 0, 전체: 40
```

실행:

```text
dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj
```

결과:

```text
통과! - 실패: 0, 통과: 142, 건너뜀: 0, 전체: 142
```

TDD RED 확인:

```text
dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj
```

초기 결과:

```text
error CS0246: 'WorkStartReadBlockOperationResult' 형식 또는 네임스페이스 이름을 찾을 수 없습니다.
error CS0738: FakeWorkStartPlcOperations가 IWorkStartPlcOperations.ReadWorkStartBlockAsync(...) 반환 형식을 구현하지 않습니다.
```

## 10. 빌드 결과

실행:

```text
dotnet build CAAutomationHub.sln
```

결과:

```text
빌드했습니다.
경고 0개
오류 0개
```

## 11. Boundary Scan 결과

실행:

```text
rg -n "Xgt|FakePlc|XgtChannelRunner|PlcChannel|XgtFrame|TcpTransport|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.PilotFlows tests\CAAutomationHub.PilotFlows.Tests
```

결과:

```text
출력 없음
match 없음
```

Project reference scan:

```text
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
```

확인:

- `src/CAAutomationHub.PilotFlows/CAAutomationHub.PilotFlows.csproj`에는 새 reference를 추가하지 않았다.
- `tests/CAAutomationHub.PilotFlows.Tests/CAAutomationHub.PilotFlows.Tests.csproj`는 기존 test packages와 `CAAutomationHub.PilotFlows` project reference만 유지한다.
- Runtime / FlowDefinitions project file은 수정하지 않았다.

## 12. git diff --check 결과

실행:

```text
git diff --check
```

결과:

```text
exit 0
whitespace error 없음
```

주의:

- Git이 일부 기존 tracked 파일에 대해 `LF will be replaced by CRLF the next time Git touches it` line-ending warning을 출력했다.
- whitespace error는 보고되지 않았다.

## 13. 다음 후보

- AH-PILOT-09 후보: 실제 adapter가 생기기 전 read result model과 XGT adapter mapping 후보를 Boundary Review로 고정
- AH-PILOT-09 또는 별도 작업 후보: `EnsureConnectedAsync(...)` failure를 `2999`로 유지할지, PLC operation setup failure로 보아 `1101` 계열로 매핑할지 정책 검토
- 실제 XGT adapter 구현 전 후보: XGT-specific classification을 PilotFlows-local status로 낮추는 mapping test 설계

## 14. Self-Check

판정: `ACCEPT`

근거:

- read operation failure와 read parse / payload extraction failure를 PilotFlows seam에서 구분했다.
- `1101`과 `1102` 매핑을 service test로 고정했다.
- read exception의 기존 `GroupRead / 1101` behavior를 유지했다.
- happy path와 기존 대표 failure path가 유지됐다.
- write bool seam은 유지했다.
- Runtime / FlowDefinitions / WPF / XGT / FakePlc / XgtChannelRunner / SqlClient reference를 추가하지 않았다.
- actual DB query, actual PLC read/write, adapter, FLOW.JSON, Flow Executor를 구현하지 않았다.
- Runtime shared execution path와 Runtime polling state path를 수정하지 않았다.
- Verification evidence를 생성했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
