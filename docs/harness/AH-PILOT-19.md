# AH-PILOT-19 Closeout - Deterministic Failure Injection Boundary Review

## 1. Summary

AH-PILOT-19에서는 AH-PILOT-20에서 구현할 failure integration test 후보를 정하기 위해 CAAutomationHub WorkStart harness와 sibling `AutomationHub.XgtDriverCore` FakePlc failure injection capability를 read-only로 검토했다.

결론:

- 현재 CAAutomationHub in-process FakePlc harness는 `tools/AutomationHub.XgtDriverCore.FakePlc`의 production FakePlc protocol handler를 사용한다.
- production FakePlc는 memory-backed read/write ACK 성공 경로와 read request validation failure에 대한 read NAK는 제공한다.
- production FakePlc는 명시적 fault script, address-specific write NAK, malformed ACK payload, no response, timeout, forced close injection API를 제공하지 않는다.
- sibling `AutomationHub.XgtChannelRunner.Tests/TestDoubles/FakePlcScenarioServer.cs`에는 read/write/status kind별 queued response mode가 있지만, test project-local internal test double이며 CAAutomationHub에서 reference 없이 바로 재사용할 수 없다.
- AH-PILOT-20의 최소 구현 후보는 production FakePlc에서 unsupported read address를 사용해 deterministic read NAK를 만들고 `ReadFailed(1101)` / no error write를 확인하는 integration test다.
- process payload write failure 2501, ACK write failure 2601, error write best-effort failure는 address-specific write fault와 subsequent error write success를 분리할 수 있어야 안정적이므로 현 production FakePlc만으로는 보류하는 것이 맞다.

이번 작업 영향:

- Runtime / FlowDefinitions / PilotFlows core / PilotFlows.Xgt production source를 수정하지 않았다.
- FakePlc map, adapter, csproj, solution, ProjectReference, PackageReference를 수정하지 않았다.
- 테스트를 추가하거나 수정하지 않았다.
- commit하지 않았다.

## 2. 현재 failure coverage

이미 FakePlc integration으로 확인된 범위:

- read success
- process payload write success
- ACK write success
- error write success
- DB not found / multiple rows / failed 기반 error write
- start signal inactive 기반 no error write

이미 unit 또는 fake-session test로 확인된 범위:

- `ReadWorkStartBlockAsync_MapsNakToOperationFailed`
- `ReadWorkStartBlockAsync_MapsReadExceptionToOperationFailed`
- `ReadWorkStartBlockAsync_MapsAckWithoutDataToParseFailed`
- `WriteProcessPayloadAsync_ReturnsFalse_WhenSessionWriteFails`
- `WriteStartAckAsync_ReturnsFalse_WhenSessionWriteFails`
- `WriteErrorCodeBestEffortAsync_DoesNotThrow_WhenSessionWriteFails`
- `RunAsync_ReturnsReadFailed_WhenReadThrows`
- `RunAsync_ReturnsReadFailed_WhenReadOperationFails`
- `RunAsync_ReturnsReadParseFailed_WhenReadOperationParseFails`
- `RunAsync_ReturnsBulkWriteFailed_WhenPayloadWriteFails`
- `RunAsync_ReturnsAckWriteFailed_WhenAckWriteFails`
- `RunAsync_KeepsFailureResult_WhenBestEffortErrorWriteThrows`

아직 FakePlc integration으로 확인되지 않은 범위:

- read NAK / malformed / timeout / no response / forced close
- read parse failure 1102
- process payload write failure 2501
- ACK write failure 2601
- error write best-effort failure
- transport-level forced close / no response

## 3. FakePlc failure injection capability

### production FakePlc

확인 파일:

- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcProtocolHandler.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcRuntime.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcRuleConfig.cs`

지원:

- memory image 기반 continuous read ACK
- memory image 기반 continuous write ACK
- read request validation failure를 read NAK로 낮추는 경로
- last bulk / ACK / error write 기록
- ACK write 시 start signal clear rule

미지원:

- explicit fault injection script
- 특정 address / operation / nth request 기반 NAK injection
- malformed response injection
- no response / timeout / forced close injection
- write NAK response generation
- write failure 후 connection을 유지하고 다음 error write를 성공시키는 deterministic script

중요 관찰:

- read path는 `InvalidOperationException`을 catch해 read NAK로 응답한다.
- write path는 `runtime.WriteContinuous(...)` failure를 write NAK로 낮추는 catch가 없다.
- 따라서 unsupported write address를 failure injection처럼 사용하면 handler fault / connection close / dispose task fault로 흐를 수 있어 안정적인 integration harness로 보기 어렵다.

### `FakePlcScenarioServer`

확인 파일:

- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\TestDoubles\FakePlcScenarioServer.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\Channels\PlcChannelFakePlcValidationTests.cs`

지원:

- `FakePlcRequestKind.Read`, `Write`, `Status`별 response queue
- `Ack`, `Nak`, `Malformed`, `ForcedClose`, `Timeout`, `NoResponse`, `SplitResponse`
- FP-01 ~ FP-09 validation matrix에서 read/protocol/transport fault 검증

제약:

- `AutomationHub.XgtChannelRunner.Tests` 내부 test double이다.
- CAAutomationHub test project에서 직접 reference하거나 source copy하면 현재 Boundary를 침범한다.
- 현재 implementation은 queued mode 자체는 read/write kind별로 분리하지만, ACK/NAK/Malformed response frame builder가 read response 중심이어서 `XgtSession.WriteAsync(...)`의 real write response path 검증용으로는 보강이 필요하다.
- memory-backed WorkStart payload / ACK / error write evidence와 결합되어 있지 않다.

## 4. read operation failure 검토

대상:

- NAK
- malformed response
- timeout
- no response
- forced close
- transport failure

판단:

- production FakePlc는 unsupported read address 또는 unsupported read request를 read NAK로 낮출 수 있다.
- CAAutomationHub WorkStart harness는 `WorkStartXgtReadOptions`를 통해 read variable을 바꿀 수 있으므로, FakePlc map 수정 없이 unsupported `%DB...` address로 deterministic read NAK를 만들 가능성이 있다.
- 이 경우 `WorkStartXgtPlcOperations.ReadWorkStartBlockAsync(...)`는 NAK를 `OperationFailed`로 낮추고, `WorkStartFlowService`는 `ReadFailed(1101)` / `WorkStartStep.GroupRead`로 종료한다.
- `ReadFailed(1101)`은 `WorkStartErrorWritePolicy`상 error write 대상이 아니므로 error write success까지 요구하지 않는다.
- production FakePlc는 malformed / timeout / no response / forced close injection을 제공하지 않는다.
- `FakePlcScenarioServer`는 malformed / timeout / no response / forced close를 제공하지만 CAAutomationHub boundary 안에서 바로 재사용할 수 있는 public harness가 아니다.

AH-PILOT-20 후보성:

- 높음: production FakePlc unsupported read address를 이용한 read NAK / OperationFailed / 1101 integration.
- 보류: malformed / timeout / no response / forced close는 FakePlcScenarioServer 보강 또는 CAAutomationHub test-local protocol server 검토가 선행되어야 한다.

## 5. read parse failure 검토

대상:

- ACK이지만 variable block 없음
- ACK이지만 data null
- data block length 부족
- expected payload extraction failure

판단:

- `WorkStartXgtPlcOperationsTests.ReadWorkStartBlockAsync_MapsAckWithoutDataToParseFailed`는 fake session으로 1102 adapter mapping을 이미 검증한다.
- production FakePlc는 supported read에 대해 requested length만큼 valid ACK payload를 만든다.
- production FakePlc에서 memory range가 부족하면 ACK malformed가 아니라 read NAK로 떨어진다.
- `XgtReadResponseParser`는 continuous read ACK에서 block count, data length, trailing bytes를 엄격히 검사하므로 malformed ACK는 protocol exception이 될 가능성이 높다.
- `XgtSession.ReadAsync(...)` parser exception은 `WorkStartXgtPlcOperations` catch에서 `OperationFailed`로 낮아진다. 즉 raw malformed protocol frame은 WorkStart 관점에서 1102가 아니라 1101로 보일 가능성이 높다.
- 1102는 현재 adapter가 정상 parse된 ACK response object 안에서 data block 부재를 관측할 때 구분된다.

AH-PILOT-20 후보성:

- 낮음. production FakePlc로 deterministic 1102 integration을 만들기는 어렵다.
- 1102는 현재 fake-session unit coverage를 유지하고, 실제 protocol malformed와 1102의 구분은 별도 parser/adapter boundary review 후 단계화한다.

## 6. process payload write failure 검토

대상:

- `%DB11000` write NAK
- write request malformed response
- write timeout
- transport failure during write

판단:

- production FakePlc는 write request에 대해 address-specific NAK를 반환하는 API가 없다.
- unsupported write address를 사용하면 write NAK가 아니라 handler exception / connection close로 이어질 수 있다.
- `WorkStartFlowService`가 process write failure 2501 후 error code 2501을 best-effort로 쓰려면 같은 transaction에서 process write failure와 error write success가 분리되어야 한다.
- 현재 production FakePlc는 "process write fail, error write success"를 deterministic하게 제어하지 못한다.
- `FakePlcScenarioServer`는 `FakePlcRequestKind.Write` queue를 갖지만, 현재는 WorkStart write response / memory evidence를 제공하는 shared harness가 아니다.

AH-PILOT-20 후보성:

- production FakePlc만으로는 보류.
- 구현하려면 FakePlc에 address-specific write NAK와 subsequent write ACK script를 추가하거나, CAAutomationHub test-local XGT session / protocol server를 별도 boundary로 도입해야 한다.

## 7. ACK write failure 검토

대상:

- `%DB11416` write NAK
- write timeout
- forced close during ACK write

판단:

- ACK write failure 2601은 unit/fake-session coverage로 이미 확인되어 있다.
- FakePlc integration으로 안정화하려면 `%DB11416`만 실패시키고 `%DB11410` error write는 성공시켜야 한다.
- production FakePlc는 ACK target만 실패시키는 rule이나 fault plan이 없다.
- unsupported ACK target을 쓰는 방식은 process write failure와 동일하게 handler fault / connection close 위험이 있으며, error write success를 보장하지 못한다.

AH-PILOT-20 후보성:

- production FakePlc만으로는 보류.
- address-specific write fault injection이 생긴 뒤 2601 integration을 도입하는 편이 안정적이다.

## 8. error write best-effort failure 검토

대상:

- `%DB11410` write NAK
- error write timeout
- forced close during error write

판단:

- `WriteErrorCodeBestEffortAsync_DoesNotThrow_WhenSessionWriteFails`와 `RunAsync_KeepsFailureResult_WhenBestEffortErrorWriteThrows`가 핵심 정책을 unit coverage로 검증한다.
- FakePlc integration으로 검증하려면 primary failure result 유지와 error write failure swallow를 관측해야 한다.
- production FakePlc는 error write target만 실패시키는 deterministic API가 없다.
- unsupported error write를 사용하면 handler task fault가 disposal 시 test failure로 전파될 수 있어 안정적이지 않다.

AH-PILOT-20 후보성:

- 낮음. unit coverage로 충분하고, FakePlc integration 우선순위는 read NAK 또는 write fault injection infrastructure 이후로 둔다.

## 9. 후보 A / B / C / D 검토

### 후보 A: FakePlc built-in fault injection 사용

판정: 부분 가능.

- read NAK는 production FakePlc의 read validation failure를 이용해 가능하다.
- malformed / timeout / no response / forced close / address-specific write NAK는 production FakePlc built-in capability로는 불가하다.
- 장점은 실제 protocol path와 현재 CAAutomationHub test reference를 유지한다는 점이다.
- 위험은 unsupported write를 fault로 오용할 경우 flaky 또는 handler task fault가 될 수 있다는 점이다.

### 후보 B: FakePlcScenarioServer 보강

판정: 장기적으로 유용하지만 AH-PILOT-20 immediate path로는 경계 조정 필요.

- 이미 FP-01 ~ FP-09 read/protocol/transport fault script 모델이 있다.
- 하지만 test project-local internal double이며, CAAutomationHub에서 source copy하면 boundary를 훼손한다.
- WorkStart write failure를 위해서는 command-specific write ACK/NAK frame, address-specific scripts, memory evidence가 필요하다.
- sibling repo에서 shared test utility 또는 production FakePlc fault script로 승격하는 별도 task가 더 적절하다.

### 후보 C: CAAutomationHub test-local fake XGT session 사용

판정: 이미 상당 부분 존재한다.

- 빠르고 deterministic하다.
- read NAK, read exception, parse failed, bulk write false, ACK write false, error write throw policy를 검증한다.
- 단점은 FakePlc integration이 아니며 실제 TCP/protocol path confidence는 낮다.

### 후보 D: Hybrid

판정: 권장.

- AH-PILOT-20에서는 production FakePlc로 가능한 최소 read NAK integration만 먼저 추가한다.
- write failure 2501/2601/error best-effort failure는 unit coverage를 유지한다.
- address-specific write NAK, malformed, timeout/no response/forced close는 FakePlc failure injection enhancement task로 분리한다.

## 10. 권장안

권장 판정:

- AH-PILOT-20은 모든 failure를 한 번에 다루지 않는다.
- AH-PILOT-20의 1순위는 FakePlc deterministic read NAK integration이다.
- 구현 방식은 FakePlc map 수정 없이 `WorkStartXgtReadOptions`로 unsupported read variable을 지정해 production FakePlc가 read NAK를 반환하는지 확인하는 방향이 가장 작다.
- 기대 검증은 `WorkStartStep.GroupRead`, `WorkStartErrorCode.ReadFailed(1101)`, `ErrorWriteExpected == false`, DB/query/payload/ACK/error write 미호출 또는 미변경이다.
- process payload write failure 2501과 ACK write failure 2601은 error write success까지 이어져야 하므로 address-specific write fault injection이 생기기 전까지 보류한다.
- malformed / timeout / no response / forced close는 현재 production FakePlc에는 없으므로 FakePlcScenarioServer 보강 또는 shared fault script API 검토를 선행한다.
- error write best-effort failure는 unit coverage로 충분하며 integration 우선순위는 낮다.

## 11. AH-PILOT-20 후보

후보 1: FakePlc deterministic read NAK integration test

- production FakePlc 사용
- unsupported read variable로 read NAK 유도
- `ReadFailed(1101)` / no error write 확인
- Boundary 유지 비용이 가장 낮음

후보 2: FakePlc failure injection enhancement boundary task

- production FakePlc에 fault script를 넣을지
- `FakePlcScenarioServer`를 shared test utility로 승격할지
- address-specific write NAK / malformed / timeout / no response를 어떤 API로 표현할지 검토

후보 3: Test-local XGT session failure harness 유지/보강

- deterministic하지만 FakePlc integration은 아님
- 1102 / 2501 / 2601 / best-effort failure policy의 빠른 regression으로 유지

후보 4: Full WorkStart transaction readiness audit

- AH-PILOT-14-B / 17 / 18-A / 18-B / 19 evidence를 통합해 현재 Pilot readiness matrix 정리

## 12. 제외한 범위

이번 작업에서 제외했다.

- Runtime project 수정
- FlowDefinitions project 수정
- PilotFlows core 수정
- PilotFlows.Xgt production source 수정
- FakePlc production map 수정
- CAAutomationHub test 수정
- adapter 수정
- csproj / solution 수정
- ProjectReference 추가
- PackageReference 추가
- XgtChannelRunner reference 추가
- Microsoft.Data.SqlClient 추가
- actual PLC read/write 테스트
- DB concrete 구현
- FLOW.JSON 연결
- JSON parser / schema 구현
- Flow Executor 구현
- RuntimeSnapshot / ChannelPollingResult 참조
- WorkStartPilotService source copy
- commit

## 13. 실행한 명령

Precheck / context:

- `git log --oneline -10`
- `git status --short`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content docs\harness\AH-PILOT-17.md`
- `Get-Content docs\harness\AH-PILOT-18-A.md`
- `Get-Content docs\harness\AH-PILOT-18-B.md`

CAAutomationHub read-only inspection:

- `Get-ChildItem -Recurse -Force tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart`
- `Get-ChildItem -Recurse -Force src\CAAutomationHub.PilotFlows.Xgt\WorkStart`
- `Get-ChildItem -Recurse -Force src\CAAutomationHub.PilotFlows\WorkStart`
- `rg -n "LastBulkWrite|LastAckValue|LastErrorCode|ReadContinuous|WriteAsync|ReadAsync|FakePlc" tests\CAAutomationHub.PilotFlows.Xgt.Tests src\CAAutomationHub.PilotFlows.Xgt`
- `rg -n "WriteProcessPayloadAsync|WriteStartAckAsync|WriteErrorCodeBestEffortAsync|OperationFailed|ParseFailed" tests\CAAutomationHub.PilotFlows.Tests tests\CAAutomationHub.PilotFlows.Xgt.Tests src\CAAutomationHub.PilotFlows`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtPlcOperations.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart\WorkStartXgtPlcOperationsTests.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart\WorkStartXgtFakePlcIntegrationTests.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowService.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Tests\WorkStart\WorkStartFlowServiceTests.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartErrorWritePolicy.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`

Sibling repo read-only inspection:

- `rg -n "Failure|Fault|FaultMode|Nak|NAK|Malformed|Timeout|NoResponse|ForcedClose|SplitResponse|FailureKind|TransportFailure|Inject|Scenario" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- `Get-ChildItem -Recurse -Force C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\TestDoubles\FakePlcScenarioServer.cs`
- `Get-ChildItem -Recurse -Force C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcProtocolHandler.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcRuntime.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcMemoryImage.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcRuleConfig.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcMapConfig.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcScenarioConfig.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcScenarioInitializer.cs`
- `rg -n "EnqueueResponseMode|FakePlcResponseMode|Fp04|Fp05|Fp09|NoResponse|Malformed|Nak|ForcedClose|Timeout|Write" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests -g "*.cs"`
- `rg -n "ReadResponseParser|WriteResponseParser|VariableBlock|blockCount|data block|malformed|Malformed|Nak" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtDriverCore.Tests -g "*.cs"`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\Channels\PlcChannelFakePlcValidationTests.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\Services\PollingWorkerFakePlcTests.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\Services\ProcessDataPayloadBuilderTests.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Client\XgtSession.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtReadResponseParser.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtWriteResponseParser.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtRawResponseClassifier.cs`

Validation:

- `git diff -- docs/harness/AH-PILOT-19.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-19.md`

## 14. git diff --check 결과

- exit code `0`
- 출력 없음
- whitespace error 없음

참고:

- `git diff -- docs/harness/AH-PILOT-19.md`는 출력 없음.
- 이유: `docs/harness/AH-PILOT-19.md`는 신규 untracked 파일이라 plain `git diff -- <path>`에는 표시되지 않는다.
- 문서 내용 확인은 `Get-Content docs\harness\AH-PILOT-19.md`로 수행했다.

## 15. git status --short 결과

```text
?? docs/harness/AH-PILOT-19.md
```

## 16. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-19 목표인 deterministic failure injection capability review를 read-only로 수행했다.
- 이미 unit test로 검증된 failure와 FakePlc integration으로 새로 검증 가능한 failure를 구분했다.
- production FakePlc와 `FakePlcScenarioServer`의 capability / boundary 차이를 구분했다.
- AH-PILOT-20의 최소 후보를 read NAK integration으로 좁혔다.
- write failure 계열은 address-specific write fault와 error write success 분리가 필요하다는 이유로 보류 판정했다.
- Runtime / FlowDefinitions / PilotFlows core / PilotFlows.Xgt production source / FakePlc map / adapter / project files를 수정하지 않았다.
- 테스트와 빌드는 문서 작성 작업이므로 실행하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
