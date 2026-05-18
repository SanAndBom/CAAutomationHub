# AH-PILOT-16 Closeout - WorkStart XGT Write Operations

## 1. Summary

AH-PILOT-16은 WorkStart process payload write, start ACK write, error code best-effort write를 `CAAutomationHub.PilotFlows.Xgt` adapter boundary 안에 구현했다.

XGT write target과 ACK value는 `WorkStartXgtWriteOptions`에 격리했다. `CAAutomationHub.PilotFlows` core의 `IWorkStartPlcOperations` signature는 유지했고, PilotFlows core / Runtime / FlowDefinitions에는 XGT address를 추가하지 않았다.

이번 변경은 XGT continuous write request 구성, ACK/NAK bool 매핑, `ushort` 1 word little-endian encoding을 adapter-local unit test로 고정한다. FakePlc write integration과 actual PLC write는 수행하지 않았으며 AH-PILOT-17 후보로 남긴다.

## 2. 변경 파일 목록

- `src/CAAutomationHub.PilotFlows.Xgt/WorkStart/WorkStartXgtWriteOptions.cs`
- `src/CAAutomationHub.PilotFlows.Xgt/WorkStart/WorkStartXgtPlcOperations.cs`
- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/WorkStart/WorkStartXgtPlcOperationsTests.cs`
- `docs/harness/AH-PILOT-16.md`

## 3. WorkStartXgtWriteOptions 추가 내용

`WorkStartXgtWriteOptions`를 `CAAutomationHub.PilotFlows.Xgt.WorkStart` namespace에 추가했다.

Default baseline:

- `ProcessPayloadWriteVariable = "%DB11000"`
- `StartAckWriteVariable = "%DB11416"`
- `StartAckValue = 1`
- `ErrorCodeWriteVariable = "%DB11410"`

Validation:

- process payload write variable null / empty / whitespace 금지
- start ACK write variable null / empty / whitespace 금지
- error code write variable null / empty / whitespace 금지
- `StartAckValue`는 `ushort`라 별도 range validation 없음

## 4. WorkStartXgtPlcOperations 구현 내용

기존 read constructor 경로를 유지하면서 write options를 받을 수 있는 constructor를 추가했다.

- `WorkStartXgtPlcOperations(IXgtSession session)` 유지
- `WorkStartXgtPlcOperations(IXgtSession session, WorkStartXgtReadOptions options)` 유지
- `WorkStartXgtPlcOperations(IXgtSession session, XgtReadRequest readRequest)` 유지
- `WorkStartXgtPlcOperations(IXgtSession session, WorkStartXgtReadOptions readOptions, WorkStartXgtWriteOptions writeOptions)` 추가
- `WorkStartXgtPlcOperations(IXgtSession session, XgtReadRequest readRequest, WorkStartXgtWriteOptions writeOptions)` 추가

Write implementation:

- `WriteProcessPayloadAsync(byte[] payload)`:
  - null payload는 `ArgumentNullException`
  - `%DB11000` default 또는 options target으로 `XgtWriteRequest`
  - `XgtDataType.Continuous`
  - `IXgtSession.WriteAsync`
  - ACK면 `true`, NAK / exception이면 `false`
- `WriteStartAckAsync()`:
  - `StartAckValue`를 2 bytes little-endian으로 변환
  - ACK target으로 continuous write
  - ACK면 `true`, NAK / exception이면 `false`
- `WriteErrorCodeBestEffortAsync(WorkStartErrorCode errorCode)`:
  - error code numeric value를 `ushort`로 변환
  - 2 bytes little-endian으로 error target에 continuous write
  - NAK / exception은 primary failure result를 보존하기 위해 삼킴

## 5. ushort Little-Endian Encoding

ACK value와 error code는 `System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian`으로 1 word payload를 만든다.

검증된 예:

- ACK `0x1234` -> `{ 0x34, 0x12 }`
- `WorkStartErrorCode.BulkWriteFailed = 2501` -> `{ 0xC5, 0x09 }`

## 6. Best-Effort Error Writer 정책

Error writer는 `IWorkStartPlcOperations` contract의 best-effort writer다.

정책:

- 일반 write failure, NAK, exception은 삼킨다.
- primary WorkStart failure result를 바꾸지 않는다.
- cancellation은 삼키지 않고 `OperationCanceledException`을 다시 던진다.

판단 이유:

- best-effort error code write가 원래 실패 원인을 덮으면 안 된다.
- 반면 cancellation은 호출자의 실행 중단 의도를 표현하므로 무시하지 않는 것이 .NET 관례와 더 잘 맞는다.

## 7. 이식하지 않은 범위

이번 작업에서 제외했다.

- FakePlc write integration test 생성
- actual PLC write test
- Runtime project 수정
- FlowDefinitions project 수정
- PilotFlows core에 XGT address 추가
- XgtChannelRunner reference 추가
- FakePlc production reference 추가
- Microsoft.Data.SqlClient 추가
- DB concrete 구현
- FLOW.JSON 연결
- JSON parser / schema 구현
- Flow Executor 구현
- RuntimeSnapshot / ChannelPollingResult 참조
- WorkStartPilotService source copy
- XGT read options default 수정
- WorkStartReadBlockLayout default 수정

## 8. 테스트 결과

RED 확인:

- `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
- 결과: 실패
- 원인: `WorkStartXgtWriteOptions` type 없음
- 판단: AH-PILOT-16 feature missing에 의한 expected RED

GREEN / validation:

- `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
  - 결과: pass
  - `29` passed, `0` failed, `0` skipped
- `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
  - 결과: pass
  - `40` passed, `0` failed, `0` skipped
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
  - 결과: pass
  - `142` passed, `0` failed, `0` skipped

## 9. 빌드 결과

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

## 10. Boundary Scan 결과

실행:

```text
rg -n "FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.PilotFlows.Xgt tests\CAAutomationHub.PilotFlows.Xgt.Tests
```

결과:

- `src\CAAutomationHub.PilotFlows.Xgt` production source에는 신규 금지 boundary hit 없음
- scan hit는 기존 `tests\CAAutomationHub.PilotFlows.Xgt.Tests`의 FakePlc read integration harness와 기존 test project reference에 한정됨
- 이번 AH-PILOT-16에서 FakePlc write integration은 추가하지 않음
- `XgtChannelRunner`, `SqlConnection`, `Microsoft.Data.SqlClient`, `RuntimeSnapshot`, `ChannelPollingResult`, `FlowExecutor`, `FLOW.JSON`, `Json`, `JSON` 신규 hit 없음

Project reference scan:

- `CAAutomationHub.PilotFlows.Xgt` production reference는 `CAAutomationHub.PilotFlows`와 sibling `AutomationHub.XgtDriverCore`만 유지
- FakePlc reference는 기존 `CAAutomationHub.PilotFlows.Xgt.Tests`에만 있음
- XgtChannelRunner reference 없음
- SqlClient package/reference 없음
- Runtime / FlowDefinitions / PilotFlows core project 파일 변경 없음

`git diff --check`:

- exit code `0`
- whitespace error 없음
- Git line-ending warning만 출력됨

## 11. 다음 후보

- AH-PILOT-17: FakePlc write verification harness
  - process payload write 후 `%DB11000` memory / `LastBulkWrite`
  - ACK write 후 `%DB11416` / `LastAckValue`
  - error code write 후 `%DB11410` / `LastErrorCode`
  - ACK clear behavior는 FakePlc-specific integration 단계에서 별도 명시

## 12. Self-Check

판정: `ACCEPT`

근거:

- XGT write / ACK / error target을 adapter-local `WorkStartXgtWriteOptions`에 격리했다.
- `IWorkStartPlcOperations` signature를 변경하지 않았다.
- process payload, ACK, error code write를 `IXgtSession.WriteAsync(XgtWriteRequest, ...)`로 구현했다.
- ACK / error code는 `ushort` little-endian 1 word encoding으로 테스트 고정했다.
- write NAK / exception은 service contract에 맞게 bool false 또는 best-effort swallow로 낮췄다.
- cancellation은 best-effort writer에서도 호출자 취소 의미를 보존하도록 재던진다.
- Runtime / FlowDefinitions / PilotFlows core 변경 없음.
- FakePlc write integration / actual PLC write / FLOW.JSON / DB / Flow Executor 범위로 확장하지 않았다.
- 테스트, 빌드, boundary scan evidence를 남겼다.
