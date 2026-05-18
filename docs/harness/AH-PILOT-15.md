# AH-PILOT-15 Closeout - WorkStart Write / ACK / Error Writer Boundary Review

## 1. Summary

AH-PILOT-15는 WorkStart write / ACK / error writer를 실제 XGT adapter로 구현하기 전 필요한 boundary를 정리한 read-only Boundary Review다.

검토 결과, process payload write target `%DB11000`, ACK write target `%DB11416`, ACK value `1`, error code write target `%DB11410`은 `CAAutomationHub.PilotFlows.Xgt.WorkStart` 안의 adapter-local write options로 격리하는 것이 적절하다. 권장 이름은 `WorkStartXgtWriteOptions`다.

`IWorkStartPlcOperations`의 기존 write methods는 현재 signature를 유지할 수 있다. service는 payload bytes, ACK 요청, error code 의미만 전달하고, XGT address / ACK value / encoding은 adapter options와 adapter implementation 책임으로 둔다.

기존 sibling `WorkStartPilotService` 기준으로 ACK와 error code write는 `ushort` 1 word little-endian payload다. process payload는 이미 `WorkStartProcessDataPayloadBuilder`가 만든 `byte[]`를 XGT continuous write payload로 그대로 전달하면 된다.

이번 작업은 문서 작성만 수행했다. production code, test code, adapter, FakePlc map, csproj / solution, ProjectReference / PackageReference, actual PLC write, commit은 수행하지 않았다.

## 2. 현재 WorkStart write / ACK / error seam

현재 `CAAutomationHub.PilotFlows` seam:

- `WriteProcessPayloadAsync(byte[] payload, CancellationToken) -> bool`
- `WriteStartAckAsync(CancellationToken) -> bool`
- `WriteErrorCodeBestEffortAsync(WorkStartErrorCode errorCode, CancellationToken)`

현재 `WorkStartFlowService` 정책:

- payload write가 false 또는 exception이면 `BulkWrite / 2501`
- ACK write가 false 또는 exception이면 `AckWrite / 2601`
- error write failure는 primary failure result를 바꾸지 않음
- `ErrorWriteExpected`가 true인 code에 대해서만 error write 수행

현재 error write 대상:

- `2201`
- `2300`
- `2301`
- `2302`
- `2303`
- `2400`
- `2501`
- `2601`

현재 error write 미대상:

- `1101`
- `1102`
- `1200`
- `2999`
- `None`

현재 `WorkStartXgtPlcOperations` 상태:

- connect/read path는 `IXgtSession.ReadAsync(XgtReadRequest, ...)`로 구현되어 있다.
- write / ACK / error writer는 AH-PILOT-12 read skeleton 범위 밖으로 남아 `NotSupportedException`을 던진다.
- `WorkStartXgtReadOptions`는 read-only options이며 `%DB10000`, `90` words를 소유한다.

## 3. write / ACK / error options 위치 후보

### 후보 A: `WorkStartXgtWriteOptions`

위치:

- `src/CAAutomationHub.PilotFlows.Xgt/WorkStart`
- namespace: `CAAutomationHub.PilotFlows.Xgt.WorkStart`

필드 후보:

- `ProcessPayloadWriteVariable`
- `StartAckWriteVariable`
- `StartAckValue`
- `ErrorCodeWriteVariable`

default 후보:

- `ProcessPayloadWriteVariable = "%DB11000"`
- `StartAckWriteVariable = "%DB11416"`
- `StartAckValue = 1`
- `ErrorCodeWriteVariable = "%DB11410"`

판정: 권장.

이유:

- XGT-specific write target을 `CAAutomationHub.PilotFlows.Xgt`에 격리할 수 있다.
- `CAAutomationHub.PilotFlows` core가 XGT address를 알 필요가 없다.
- read options와 write options의 변경 주기를 분리할 수 있다.
- AH-PILOT-16에서 write-only validation과 request construction test를 작게 만들 수 있다.

### 후보 B: `WorkStartXgtOptions` 하나로 read/write 통합

필드 후보:

- read start variable / read word count
- process payload write variable
- ACK variable / ACK value
- error variable

판정: 보류.

장점:

- adapter config entry point가 한 곳으로 모인다.

위험:

- read config와 write config의 변경 주기가 다를 수 있다.
- options가 빠르게 커질 수 있다.
- AH-PILOT-13-B에서 이미 read options가 closeout과 test로 고정되어 있어, 지금 통합하면 불필요한 refactor가 된다.

### 후보 C: PilotFlows core options에 추가

판정: 비권장.

이유:

- `%DB11000`, `%DB11416`, `%DB11410`은 XGT-specific address다.
- PilotFlows core에 넣으면 business flow seam이 vendor request config를 알게 된다.
- Runtime / FlowDefinitions vendor-neutral boundary에도 나쁜 압력이 생긴다.

### 후보 D: adapter constructor parameters로 직접 전달

판정: 단기 가능하지만 비권장.

이유:

- target/value 의미가 constructor parameter 목록에 흩어진다.
- default / validation / future binding handoff를 한 타입에 묶기 어렵다.

## 4. IWorkStartPlcOperations signature 충분성

판정: 현재 signature 유지 가능.

근거:

- process payload write target `%DB11000`은 adapter-local concern이다. service가 target address를 알 필요가 없다.
- ACK target `%DB11416`과 ACK value `1`도 adapter-local write options 책임이다.
- error code write target `%DB11410` 역시 adapter-local write options 책임이다.
- `WriteProcessPayloadAsync(...) -> bool`은 현재 service 정책에서 `BulkWriteFailed / 2501` 매핑에 충분하다.
- `WriteStartAckAsync(...) -> bool`은 현재 service 정책에서 `AckWriteFailed / 2601` 매핑에 충분하다.
- `WriteErrorCodeBestEffortAsync(...)`는 best-effort 정책을 유지하기에 충분하다.

주의:

- XGT NAK / malformed / timeout detail은 현재 public service result에 직접 필요하지 않다.
- 후속 diagnostics, telemetry, retry policy가 필요하면 write result model 또는 adapter diagnostics를 별도 review로 분리한다.
- AH-PILOT-16에서 interface를 수정해야 한다는 압력은 현재 evidence 기준으로 없다.

## 5. XGT write request 구성 방식

Sibling `XgtDriverCore` 기준:

- `IXgtSession.WriteAsync(XgtWriteRequest request, CancellationToken cancellationToken)`가 public write seam이다.
- `XgtWriteRequest`는 `XgtDataType`과 `IReadOnlyList<XgtVariableBlock>`을 받는다.
- write block은 `XgtVariableBlock(variableName, data)` 형태이며, write request는 모든 block에 data가 있어야 한다.
- `XgtWriteRequestBuilder`는 variable name length, variable name ASCII, data length, data bytes를 body에 쓴다.
- continuous write는 `XgtDataType.Continuous`와 `%DB...` byte variable을 사용할 수 있다.

기존 `WorkStartPilotService` 기준:

- process payload write:
  - `BuildContinuousWriteRequest(payload.StartVariable, payload.PayloadBytes)`
  - `payload.StartVariable`은 `%DB11000`
  - `payload.PayloadBytes`는 70 words / 140 bytes
- ACK write:
  - `BitConverter.GetBytes(config.AckValue)`
  - `%DB11416`에 2 bytes write
- error code write:
  - `BitConverter.GetBytes(errorCode)`
  - `%DB11410`에 2 bytes write

AH-PILOT-16 구현 후보:

- `new XgtWriteRequest(XgtDataType.Continuous, [new XgtVariableBlock(variable, payload)])`
- `await _session.WriteAsync(request, cancellationToken)`
- `response.Status.IsAck`이면 true
- NAK 또는 write exception은 process payload / ACK write에서는 false 또는 service-mapped exception policy 중 하나로 낮춘다.

권장:

- process payload는 `byte[] payload`를 그대로 `XgtVariableBlock` data로 전달한다.
- AH-PILOT-16에서 payload length가 `WorkStartPayloadBuildOptions.DefaultWordCount * 2`와 맞는지 adapter-local validation 후보를 검토한다.
- word model로 재해석하지 않는다. payload packing 책임은 이미 `WorkStartProcessDataPayloadBuilder`가 가진다.

## 6. Error code write encoding 확인 결과

확인 결과: 기존 source 기준 명확하다.

기존 `WorkStartPilotService.TryWriteErrorCodeAsync(...)`:

- `ushort errorCode`
- `var payload = BitConverter.GetBytes(errorCode)`
- `BuildContinuousWriteRequest(config.ErrorCodeWriteVariable, payload)`

기존 ACK write:

- `AckValue` type은 `ushort`
- `var ackPayload = BitConverter.GetBytes(config.AckValue)`
- `%DB11416`에 continuous write

FakePlc 기준:

- `%DB11410` write payload를 `BinaryPrimitives.ReadUInt16LittleEndian(payload)`로 `LastErrorCode`에 저장한다.
- `%DB11416` write payload를 `BinaryPrimitives.ReadUInt16LittleEndian(payload)`로 `LastAckValue`에 저장한다.

판정:

- error code write는 1 word `ushort` little-endian이다.
- ACK value도 1 word `ushort` little-endian이다.
- 2 word int32가 아니다.
- AH-PILOT-16 전에 별도 Error Write Encoding Review는 필요하지 않다.

주의:

- `WorkStartErrorCode` enum numeric value는 현재 error code 범위가 `ushort` 안에 있다.
- AH-PILOT-16에서 `checked((ushort)errorCode)` 또는 equivalent guard를 두는 후보가 있다.

## 7. FakePlc memory verification 후보

현재 FakePlc 지원:

- `FakePlcRuntime.WriteContinuous(variableName, payload)`가 memory image에 payload를 쓴다.
- `FakePlcMemoryImage.SnapshotBlocks()`로 block snapshot을 볼 수 있다.
- `FakePlcMemoryImage.ReadContinuous(variableName, requestedLength)`로 read-back 할 수 있다.
- `FakePlcRuntime.LastBulkWrite`가 `%DB11000` write payload를 저장한다. 단 `Rules.StoreLastWrites`가 true일 때다.
- `FakePlcRuntime.LastErrorCode`가 `%DB11410` write value를 저장한다.
- `FakePlcRuntime.LastAckValue`가 `%DB11416` write value를 저장한다.
- ACK write 시 `Rules.ClearStartSignalOnAck`가 true이면 `D5083` start signal을 clear한다.

검증 후보:

- process payload write 후 `runtime.LastBulkWrite` 또는 `MemoryImage.ReadContinuous("%DB11000", 140)` 확인
- ACK write 후 `runtime.LastAckValue == 1` 또는 `MemoryImage.ReadContinuous("%DB11416", 2)` 확인
- error write 후 `runtime.LastErrorCode == expected` 또는 `MemoryImage.ReadContinuous("%DB11410", 2)` 확인
- ACK write 후 current FakePlc 기준 `D5083` clear 여부 확인

주의:

- FakePlc read-back 검증은 가능하지만 AH-PILOT-16에서 바로 integration harness까지 확장하면 범위가 커질 수 있다.
- AH-PILOT-16은 adapter-local fake `IXgtSession`으로 request construction / ACK/NAK mapping을 먼저 닫고, FakePlc write integration은 AH-PILOT-17로 분리하는 것이 더 안전하다.
- AH-PILOT-14-B의 `StartSignalWordIndex = 83` test-specific override와 ACK clear target `D5083`의 관계는 FakePlc integration 단계에서 계속 명시해야 한다.

## 8. AH-PILOT-16 구현 후보

### 후보 A: `WorkStartXgtWriteOptions` 추가

권장.

내용:

- process payload write variable
- ACK write variable / value
- error code write variable
- default values: `%DB11000`, `%DB11416`, `1`, `%DB11410`
- null / whitespace address validation

### 후보 B: `WorkStartXgtPlcOperations` write / ACK / error implementation

권장.

내용:

- `WriteProcessPayloadAsync`
- `WriteStartAckAsync`
- `WriteErrorCodeBestEffortAsync`
- `IXgtSession.WriteAsync(XgtWriteRequest, ...)` 사용
- `XgtDataType.Continuous` 사용
- ACK response면 true 또는 completed
- error writer는 exception / NAK를 swallow하거나 내부에서 best-effort로 낮춘다.

### 후보 C: FakePlc write / ACK / error integration harness

후순위 권장.

내용:

- write 후 FakePlc `LastBulkWrite` / `LastAckValue` / `LastErrorCode` 또는 read-back 검증

권장 timing:

- AH-PILOT-17로 분리.
- AH-PILOT-16이 options + adapter write implementation만으로도 충분히 의미 있는 step이 된다.

### 후보 D: Error write encoding review

현재는 불필요.

근거:

- 기존 runner와 FakePlc 모두 `ushort` little-endian 1 word로 확인됐다.

## 9. 권장안

권장 순서:

1. AH-PILOT-16에서 `WorkStartXgtWriteOptions`를 먼저 도입한다.
2. `IWorkStartPlcOperations` signature는 유지한다.
3. XGT address / ACK value / error target은 PilotFlows.Xgt adapter options로 둔다.
4. `WorkStartXgtPlcOperations`는 read options와 write options를 각각 받아 request를 구성한다.
5. process payload는 `byte[]` 그대로 XGT continuous write payload로 보낸다.
6. ACK / error code는 `ushort` little-endian 1 word로 쓴다.
7. process payload write와 ACK write는 `bool`로 service policy에 맞춘다.
8. error writer는 best-effort를 유지하며 primary failure result를 바꾸지 않는다.
9. FakePlc memory verification은 AH-PILOT-17에서 integration harness로 분리한다.

권장 constructor 후보:

- `WorkStartXgtPlcOperations(IXgtSession session)`
- `WorkStartXgtPlcOperations(IXgtSession session, WorkStartXgtReadOptions readOptions, WorkStartXgtWriteOptions writeOptions)`
- 기존 `XgtReadRequest` direct constructor는 low-level/test seam으로 유지하되 write options 없이 write methods를 사용할 때의 정책을 AH-PILOT-16에서 명확히 한다.

Boundary 판단:

- PilotFlows core에 XGT address를 추가하지 않는다.
- Runtime / FlowDefinitions project는 수정하지 않는다.
- XgtDriverCore reference는 `CAAutomationHub.PilotFlows.Xgt`에만 유지한다.
- FakePlc reference는 production project에 추가하지 않는다.

## 10. 제외한 범위

이번 AH-PILOT-15에서 제외한 범위:

- production code 수정
- test code 수정
- adapter 수정
- csproj / solution 수정
- ProjectReference 추가
- PackageReference 추가
- FakePlc map 수정
- actual PLC write test
- FakePlc integration test 작성
- `WorkStartXgtWriteOptions` 구현
- `WorkStartXgtPlcOperations` write / ACK / error 구현
- `IWorkStartPlcOperations` interface 수정
- Runtime project 수정
- FlowDefinitions project 수정
- PilotFlows core에 XGT address 추가
- XgtChannelRunner reference 추가
- Microsoft.Data.SqlClient reference 추가
- actual DB Query 구현
- FLOW.JSON 파일 생성
- JSON schema / parser 생성
- Flow Executor 구현
- RuntimeSnapshot / ChannelPollingResult 참조
- WorkStartPilotService source copy
- commit
- ContextPublisher automatic publish

## 11. 실행한 명령

Current repo:

- `git log --oneline -10`
- `git status --short`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content docs\harness\AH-PILOT-12.md`
- `Get-Content docs\harness\AH-PILOT-13-A.md`
- `Get-Content docs\harness\AH-PILOT-13-B.md`
- `Get-Content docs\harness\AH-PILOT-14-A.md`
- `Get-Content docs\harness\AH-PILOT-14-B.md`
- `rg --files src\CAAutomationHub.PilotFlows\WorkStart`
- `rg --files src\CAAutomationHub.PilotFlows.Xgt\WorkStart`
- `rg --files tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart`
- `rg -n "WriteProcessPayloadAsync|WriteStartAckAsync|WriteErrorCodeBestEffortAsync|WorkStartXgtReadOptions|XgtWriteRequest|WriteAsync" C:\AutomationHub.Rebuild\CAAutomationHub`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\IWorkStartPlcOperations.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowService.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartErrorWritePolicy.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartErrorCode.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartProcessDataPayloadBuilder.cs`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtPlcOperations.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart\WorkStartXgtPlcOperationsTests.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart\WorkStartXgtFakePlcIntegrationTests.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Tests\WorkStart\WorkStartFlowServiceTests.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartPayloadBuildOptions.cs`
- `rg -n "write|ACK|error writer|XgtWriteRequest|IWorkStartPlcOperations|WriteProcessPayloadAsync|WriteStartAckAsync|WriteErrorCodeBestEffortAsync|WorkStartXgt" docs\harness\AH-PILOT-07.md docs\harness\AH-PILOT-08.md docs\harness\AH-PILOT-10.md docs\harness\AH-PILOT-12.md`
- `rg -n "WriteProcessPayloadAsync|WriteStartAckAsync|WriteErrorCodeBestEffortAsync|ErrorWriteExpected|ShouldWriteErrorCode" src\CAAutomationHub.PilotFlows tests\CAAutomationHub.PilotFlows.Tests`

Sibling repo:

- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\PilotScenarioConfig.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\WorkStartPilotService.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\ProcessDataPayloadBuilder.cs`
- `rg -n "DB11000|%DB11000|DB11416|%DB11416|DB11410|%DB11410|AckValue|ErrorCodeWrite|WriteStartVariable|AckWriteVariable|ErrorCodeWriteVariable" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- `rg -n "XgtWriteRequest|WriteAsync|record XgtWriteRequest|class XgtWriteRequest|struct XgtWriteRequest|IXgtSession" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtWriteRequest.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtWriteRequestBuilder.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtVariableBlock.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Client\IXgtSession.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Client\XgtSession.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtWriteResponse.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtWriteResponseParser.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcMemoryImage.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcRuntime.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcProtocolHandler.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtDriverCore.Tests\FakePlcWordSignalTests.cs`
- `rg -n "Write|LastAckValue|LastCompleteAckValue|LastError|ReadUInt16|WriteBlock|Db11410|Db11416|DB11000|DB11410|DB11416" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\Services\ProcessDataPayloadBuilderTests.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\FakePlc\FakePlcConcurrentMemoryTests.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtDriverCore.Tests\Protocol\XgtInstructionBodyTests.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcRuleConfig.cs`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short`

Validation:

- `git diff -- docs/harness/AH-PILOT-15.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-15.md`

테스트 / 빌드:

- 문서 작성과 read-only Boundary Review만 수행했으므로 실행하지 않았다.

## 12. git diff --check 결과

실행:

```text
git diff --check
```

결과:

```text
pass
whitespace error 없음
```

주의:

- `docs/harness/AH-PILOT-15.md`는 신규 untracked 파일이므로 `git diff --check`의 tracked diff 검사 대상에는 포함되지 않는다.

## 13. git status --short 결과

실행:

```text
git status --short
```

결과:

```text
?? docs/harness/AH-PILOT-15.md
```

## 14. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-15 목표인 WorkStart write / ACK / error writer Boundary Review를 closeout 문서로 남겼다.
- process payload write target `%DB11000`, ACK write target `%DB11416`, ACK value `1`, error write target `%DB11410`의 위치를 adapter-local write options로 정리했다.
- `IWorkStartPlcOperations` signature는 현재 유지 가능하다고 판단했다.
- XGT write request는 `XgtWriteRequest(XgtDataType.Continuous, XgtVariableBlock(variable, payload))`로 구성 가능함을 확인했다.
- process payload는 `byte[]` 그대로 넘기는 방향이 적절하다고 정리했다.
- ACK / error code encoding은 기존 source와 FakePlc 기준 `ushort` little-endian 1 word임을 확인했다.
- FakePlc memory verification 후보를 `LastBulkWrite`, `LastAckValue`, `LastErrorCode`, `ReadContinuous`, `SnapshotBlocks` 기준으로 정리했다.
- AH-PILOT-16은 options + adapter write methods 구현으로 제한하고, FakePlc write integration은 AH-PILOT-17 후보로 분리하는 권장안을 남겼다.
- production code, test code, adapter, FakePlc map, csproj / solution, references, actual PLC write, commit을 수행하지 않았다.

주의:

- sibling repo는 기존 dirty 상태다.
  - `tools/AutomationHub.XgtDriverCore.FakePlc/appsettings/fakeplc.map.json`
  - `context-events/pending/*.json`
- 이번 AH-PILOT-15는 sibling source를 read-only로만 확인했다.
- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
