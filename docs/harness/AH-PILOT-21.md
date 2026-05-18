# AH-PILOT-21 Closeout - FakePlc Failure Injection Enhancement Boundary Review

## 1. Summary

AH-PILOT-21에서는 AH-PILOT-20 이후 남은 WorkStart failure integration gap을 기준으로 FakePlc failure injection enhancement가 필요한지, 필요하다면 어느 위치와 범위가 안전한지 read-only로 검토했다.

결론:

- 현재 CAAutomationHub in-process harness는 sibling production FakePlc의 memory-backed read/write path를 사용하며, read success, write success, full WorkStart happy path, DB failure error write, start signal inactive no-error-write, unsupported read address NAK를 검증했다.
- production FakePlc는 unsupported read address를 deterministic read NAK로 낮출 수 있지만, supported address에 대한 intentional read NAK, address-specific write NAK, one-shot write failure, malformed response, timeout/no response, forced close fault script를 제공하지 않는다.
- process payload write failure 2501, ACK write failure 2601, error write best-effort failure를 FakePlc integration으로 검증하려면 특정 write만 실패시키고 후속 write는 성공시키는 deterministic script가 필요하다.
- `FakePlcScenarioServer`는 test-only로 `Ack`, `Nak`, `Malformed`, `ForcedClose`, `Timeout`, `NoResponse`, `SplitResponse` response mode를 제공하지만, 현재 CAAutomationHub에서 직접 참조할 수 있는 shared harness가 아니며 WorkStart memory evidence와 address-specific write target matching은 없다.
- 권장안은 CAAutomationHub repo를 수정하지 않고 sibling repo에서 test-only `FakePlcScenarioServer` 또는 그에 준하는 shared test harness에 address-specific one-shot write NAK를 먼저 보강하는 것이다.

이번 작업 영향:

- Runtime / FlowDefinitions / PilotFlows core / PilotFlows.Xgt production source를 수정하지 않았다.
- CAAutomationHub test, FakePlc map, adapter, csproj, solution, ProjectReference, PackageReference를 수정하지 않았다.
- sibling repo도 read-only로만 확인했다.
- 테스트/빌드는 문서 작성 작업이므로 실행하지 않았다.
- commit하지 않았다.

## 2. 현재 failure coverage

FakePlc integration으로 검증된 범위:

- read success
- read unsupported address NAK -> `ReadFailed(1101)`
- process payload write success
- ACK write success
- error code write success
- full WorkStart happy transaction
- DB not found / multiple rows / failed transaction -> error code write
- start signal inactive -> no error write

unit 또는 fake-session coverage로 확인된 범위:

- read NAK -> operation failed
- read exception -> operation failed
- ACK read response without data -> parse failed
- process payload write false -> `BulkWriteFailed(2501)` + error write expected
- ACK write false -> `AckWriteFailed(2601)` + error write expected
- error write best-effort exception swallow

아직 FakePlc integration으로 검증되지 않은 범위:

- read parse failure 1102 FakePlc integration
- malformed response integration
- timeout / no response integration
- forced close integration
- process payload write failure 2501 integration
- ACK write failure 2601 integration
- error write best-effort failure integration
- payload build failure 2400 FakePlc transaction
- bulk write NAK with error write success
- ACK write NAK with error write success
- address-specific one-shot failure
- request OFF / ACK OFF handshake
- WorkComplete flow

## 3. FakePlc failure injection capability 확인 결과

### production FakePlc

확인 파일:

- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcProtocolHandler.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcRuntime.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcMemoryImage.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcRuleConfig.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcScenarioConfig.cs`

지원:

- memory image 기반 continuous read ACK
- memory image 기반 continuous write ACK
- unsupported read address / invalid continuous read request -> read NAK
- `LastBulkWrite`, `LastAckValue`, `LastErrorCode` write evidence
- ACK write 시 start signal clear rule

미지원:

- explicit fault script
- supported address에 대한 intentional read NAK
- address-specific write NAK
- one-shot failure
- malformed response injection
- timeout / no response / forced close injection
- process write fail 후 error write success 같은 transaction script

중요 관찰:

- read path는 `InvalidOperationException`을 catch해 `BuildReadNakResponseBody(...)`로 낮춘다.
- write path는 `runtime.WriteContinuous(...)` failure를 catch해 write NAK로 낮추지 않는다.
- 따라서 unsupported write address를 failure injection처럼 사용하면 deterministic write NAK가 아니라 handler exception, connection close, disposal fault로 흐를 수 있다.

### FakePlcScenarioServer

확인 파일:

- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\TestDoubles\FakePlcScenarioServer.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\Channels\PlcChannelFakePlcValidationTests.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\docs\context\04_fakeplc_validation_matrix.md`

지원:

- request kind별 response queue: `Read`, `Write`, `Status`
- response mode: `Ack`, `Nak`, `Malformed`, `ForcedClose`, `Timeout`, `NoResponse`, `SplitResponse`
- FP-01 ~ FP-09 transport/protocol/health probe validation axis

제약:

- `AutomationHub.XgtChannelRunner.Tests` 내부 `internal` test double이다.
- CAAutomationHub test에서 직접 reference하면 repo/test boundary가 흐려진다.
- 현재 response frame builder는 read-shaped ACK/NAK/Malformed 중심이다.
- write request kind queue는 있지만 WorkStart write target variable matching, memory-backed write evidence, write response frame specificity가 없다.

## 4. address-specific read NAK 검토

이미 가능한 범위:

- AH-PILOT-20에서 unsupported read address `%DB99990`을 사용해 production FakePlc read NAK -> `OperationFailed` -> `ReadFailed(1101)`을 검증했다.

현재 불가능하거나 보강 필요한 범위:

- supported address `%DB10000`에 대한 intentional read NAK
- 특정 address / nth read / one-shot read failure
- read NAK 이후 같은 connection에서 후속 operation을 이어가는 scripted transaction

판단:

- 현재 Pilot에서 read NAK 1101은 이미 충분한 FakePlc integration evidence가 있다.
- supported address read NAK와 one-shot read fault는 있으면 유용하지만, write failure 2501/2601보다 우선순위는 낮다.
- read parse failure 1102는 FakePlc production path로 만들기 어렵다. Raw malformed frame은 `XgtSession.ReadAsync(...)` parser exception으로 올라와 adapter에서 `OperationFailed`로 낮아질 가능성이 크며, 1102는 정상 parse된 ACK response object 안에 data block이 없을 때 구분된다.

## 5. address-specific write NAK 검토

필요성:

- process payload write failure test에서는 `%DB11000` write만 fail하고 `%DB11410` error write는 success해야 한다.
- ACK write failure test에서는 `%DB11416` write만 fail하고 `%DB11410` error write는 success해야 한다.
- error write best-effort failure test에서는 `%DB11410` write만 fail해야 하며 primary failure result를 보존해야 한다.

현재 capability:

- production FakePlc는 모든 supported write를 memory-backed ACK로 처리한다.
- write target별 reject rule이 없다.
- write operation count 또는 one-shot rule이 없다.
- write NAK response generator가 production FakePlc protocol handler에 없다.

unsupported write address 사용 검토:

- FakePlc memory range 밖 write는 `FakePlcMemoryImage.WriteContinuous(...)`에서 `InvalidOperationException`을 던질 수 있다.
- production `FakePlcProtocolHandler.BuildResponseFrame(...)`는 write path exception을 write NAK로 낮추지 않는다.
- 이 방식은 connection close / task fault / timing-dependent failure가 될 수 있어 AH-PILOT integration harness에 적합하지 않다.
- 무엇보다 process/ACK write failure 뒤에 error write success를 보장하지 못한다.

필요한 최소 기능:

- target variable match: `%DB11000`, `%DB11416`, `%DB11410`
- failure mode: write NAK
- cardinality: one-shot 또는 fail next write matching target
- connection 유지
- matching되지 않은 write는 normal memory-backed ACK
- failure event 기록: target, request order, consumed 여부

## 6. malformed / timeout / forced close 검토

### malformed response

- production FakePlc는 malformed response injection을 제공하지 않는다.
- `FakePlcScenarioServer`는 protocol-level malformed read frame을 제공한다.
- 현재 Pilot WorkStart 관점에서는 malformed protocol frame이 1102가 아니라 `ReadFailed(1101)` 계열로 보일 가능성이 있다.
- malformed read/write는 AH-PILOT 통합 harness보다 XgtDriverCore protocol validation matrix에 더 가깝다.

판정:

- AH-PILOT-22의 1순위로 두지 않는다.
- WorkStart에서 malformed를 별도 의미로 다룰 필요가 생기면 parser/adapter boundary review 후 진행한다.

### timeout / no response

- production FakePlc는 request별 no response / timeout mode를 제공하지 않는다.
- `FakePlcScenarioServer`는 `Timeout`과 `NoResponse`를 구분한다.
- XgtDriverCore FP-01/FP-09에서 timeout/no response 의미가 이미 분리되어 있다.
- CAAutomationHub WorkStart integration에서 timeout을 직접 다루려면 receive timeout tuning 때문에 test runtime 증가와 flake 위험이 있다.

판정:

- 현재 Pilot에서는 unit/fake-session 또는 XgtDriverCore FP matrix를 우선 신뢰한다.
- CAAutomationHub integration으로 가져오려면 timeout 상한, reconnect expectation, WorkStart mapping을 별도 Boundary Review로 분리한다.

### forced close

- production FakePlc는 operation-specific forced close를 제공하지 않는다.
- `FakePlcScenarioServer`는 forced close를 제공하지만 memory-backed WorkStart write evidence와 결합되어 있지 않다.
- WorkStart adapter에서는 transport/protocol exception이 `OperationFailed` 또는 write false로 낮아질 수 있으나, forced close 뒤 후속 error write success를 보장하는 transaction script는 현재 없다.

판정:

- process/ACK write NAK보다 우선순위 낮음.
- forced close는 XgtDriverCore recovery primitive 검증 축으로 남기고, WorkStart transaction 검증은 address-specific write NAK부터 진행하는 것이 안전하다.

### split / delayed response

- XgtDriverCore FP-07에서 split response 검증 축이 존재한다.
- 현재 AH-PILOT WorkStart failure list의 핵심 blocker는 split/delay가 아니라 address-specific write failure다.

판정:

- AH-PILOT-21/22 범위에서는 제외한다.

## 7. FakePlc enhancement 위치 후보

### 후보 A: sibling FakePlc production tool 보강

장점:

- CAAutomationHub의 기존 test project reference 흐름과 맞다.
- real `FakePlcRuntime` memory evidence와 결합하기 쉽다.
- 향후 다른 integration에도 사용할 수 있다.

위험:

- production tool에 test-specific fault script가 섞일 수 있다.
- operational FakePlc 사용자가 의도치 않게 fault mode를 켜는 구성 위험이 생긴다.
- sibling repo 변경이 먼저 필요하다.

판정:

- address-specific write NAK가 장기 공용 기능이 되어야 한다면 가능하지만, AH-PILOT-22의 첫 보강 위치로는 production tool 오염 리스크가 있다.

### 후보 B: sibling FakePlcScenarioServer test double 보강

장점:

- failure injection의 test-only 성격이 명확하다.
- request kind queue와 fault mode axis가 이미 있다.
- address-specific / one-shot failure를 넣기 좋다.
- production FakePlc tool 오염을 피할 수 있다.

위험:

- 현재 CAAutomationHub test에서 직접 접근할 수 없다.
- WorkStart memory evidence가 없으므로 process payload / ACK / error write read-back을 검증하려면 memory-backed 기능을 추가하거나 production FakePlc runtime과 결합해야 한다.
- 현재 builder가 read response 중심이라 write ACK/NAK frame builder 보강이 필요하다.

판정:

- 권장. 단, AH-PILOT-22는 sibling repo 내부 보강으로 제한하고 CAAutomationHub integration 소비는 후속 단계에서 boundary를 다시 정해야 한다.

### 후보 C: CAAutomationHub.PilotFlows.Xgt.Tests 내부 test-local handler 보강

장점:

- 빠르다.
- CAAutomationHub 안에서 deterministic WorkStart transaction test를 만들 수 있다.
- target-specific NAK와 error write success를 곧바로 구성할 수 있다.

위험:

- XGT protocol handler 중복 구현 위험이 크다.
- production FakePlc와 다른 동작을 만들 수 있다.
- long-term harness가 CAAutomationHub test-local protocol copy로 갈라질 수 있다.

판정:

- 비권장. 이번 Boundary Review의 핵심은 divergence를 만들지 않는 것이다.

### 후보 D: adapter-level fake IXgtSession 유지

장점:

- 가장 deterministic하다.
- 이미 1102, 2501, 2601, best-effort failure unit coverage가 존재한다.
- CAAutomationHub repo 안에서 boundary가 단순하다.

위험:

- FakePlc integration이 아니다.
- 실제 TCP/protocol path confidence를 추가하지 못한다.

판정:

- regression safety net으로 유지한다.
- FakePlc integration gap을 메우는 해법으로는 부족하다.

## 8. 후보 A / B / C / D 검토

| 후보 | deterministic | address-specific | one-shot | production 오염 | protocol path | 구현 비용 | 판정 |
|---|---|---|---|---|---|---|---|
| A production FakePlc 보강 | 높음 | 가능 | 가능 | 있음 | 높음 | 중간 | 장기 후보 |
| B FakePlcScenarioServer 보강 | 높음 | 가능 | 가능 | 낮음 | 중간~높음 | 중간 | 권장 |
| C CAAutomationHub test-local handler | 높음 | 가능 | 가능 | 낮음 | 중복 위험 | 낮음~중간 | 비권장 |
| D fake IXgtSession 확장 | 높음 | 가능 | 가능 | 낮음 | 낮음 | 낮음 | 보조 유지 |

판단 기준별 요약:

- deterministic / flake-free: B, C, D가 유리하고 A도 설계에 따라 가능하다.
- address-specific / one-shot: 현재는 모두 미지원이며 B 또는 A 보강이 필요하다.
- production dependency 오염 없음: B, C, D가 유리하다.
- FakePlc map 수정 불필요: B 또는 A의 fault script 방식이면 가능하다.
- CAAutomationHub / sibling repo 경계: B가 가장 명확하다. C는 CAA repo 안에서 빠르지만 장기 경계를 흐린다.
- 실제 protocol path coverage: A가 가장 높고 B도 write frame builder를 보강하면 충분히 의미 있다. D는 낮다.
- 구현 비용: D < C < B <= A 순서로 보인다. 하지만 장기 유지비는 C가 가장 위험하다.

## 9. 권장안

권장안:

- AH-PILOT-22는 sibling repo에서 test-only `FakePlcScenarioServer` address-specific write NAK enhancement를 검토/구현하는 방향이 가장 안전하다.
- 최소 기능은 "fail next write to target variable"이다.
- target은 `%DB11000`, `%DB11416`, `%DB11410` 같은 WorkStart write variable을 직접 match할 수 있어야 한다.
- failure는 write NAK로 반환해야 하며 connection을 유지해야 한다.
- matching failure가 consumed된 뒤 후속 write는 ACK되어야 한다.
- normal ACK write는 memory evidence를 남길 수 있어야 한다. 최소한 target/payload history를 기록하고, 가능하면 production `FakePlcRuntime` memory image와 결합한다.

CAAutomationHub repo 판단:

- 이번 단계와 AH-PILOT-22 첫 단계에서는 CAAutomationHub repo의 FakePlc 수정, test 수정, adapter 수정이 필요하지 않다.
- CAAutomationHub에서 integration으로 소비하기 전에 sibling 쪽에서 API와 behavior를 먼저 안정화해야 한다.
- 이후 CAAutomationHub가 직접 사용할 방식은 별도 Boundary Review가 필요하다. 선택지는 shared test-support project, production FakePlc opt-in fault config, 또는 기존 production FakePlc에 최소 fault plan을 추가하는 방식이다.

비권장:

- CAAutomationHub test-local XGT protocol handler를 새로 만들지 않는다.
- unsupported write address를 failure injection으로 오용하지 않는다.
- timeout/no response를 WorkStart integration에 먼저 넣지 않는다.

## 10. AH-PILOT-22 후보

### 후보 1: FakePlcScenarioServer address-specific write NAK enhancement

권장.

최소 scope:

- parse write request target variable
- build write ACK frame and write NAK frame with correct `WriteResponse`
- support `FailNextWriteTo("%DB11000")`
- support `FailNextWriteTo("%DB11416")`
- support `FailNextWriteTo("%DB11410")`
- keep connection open after NAK
- record write history and consumed fault history

후속 AH-PILOT-23:

- process payload write NAK -> error write success -> 2501 integration
- ACK write NAK -> error write success -> 2601 integration
- error write NAK best-effort -> primary failure result preserved

### 후보 2: CAAutomationHub test-local handler address-specific write NAK

빠르지만 비권장.

이 후보는 XGT protocol handler copy 위험이 크고 FakePlc integration의 장기 신뢰도를 낮출 수 있다.

### 후보 3: adapter-level fake IXgtSession failure expansion

보조 후보.

이미 핵심 정책 coverage가 있으므로 부족한 unit edge를 보강하는 정도는 가능하지만, FakePlc integration 목표를 대체하지 않는다.

### 후보 4: Pilot readiness audit

가능.

현재까지 AH-PILOT-14-B / 17 / 18-A / 18-B / 20 coverage를 matrix로 정리하고 real PLC preparation으로 넘어갈지 판단할 수 있다.

### 후보 5: ACK OFF / request OFF flow boundary review

가능.

WorkStart success 이후 handshake 종료 흐름을 다루는 별도 Boundary Review로 적합하다. 다만 write failure integration blocker를 해결하려면 후보 1이 먼저 더 직접적이다.

## 11. 아직 구현 안 된 failure 리스트

- read parse failure 1102 FakePlc integration
- malformed response integration
- timeout / no response integration
- forced close integration
- process payload write failure 2501 integration
- ACK write failure 2601 integration
- error write best-effort failure integration
- payload build failure 2400 FakePlc transaction
- bulk write NAK with error write success
- ACK write NAK with error write success
- address-specific one-shot failure
- request OFF / ACK OFF handshake
- WorkComplete flow

## 12. 제외한 범위

이번 작업에서 제외했다.

- Runtime project 수정
- FlowDefinitions project 수정
- PilotFlows core 수정
- PilotFlows.Xgt production 수정
- CAAutomationHub test 수정
- FakePlc production tool 수정
- FakePlc map 수정
- adapter 수정
- csproj / solution 수정
- ProjectReference 추가
- PackageReference 추가
- actual PLC test
- DB concrete 구현
- FLOW.JSON 연결
- JSON parser / schema 구현
- Flow Executor 구현
- RuntimeSnapshot / ChannelPollingResult 참조
- XgtChannelRunner reference 추가
- WorkStartPilotService source copy
- sibling repo 구현
- commit

## 13. 실행한 명령

Context / precheck:

- `git log --oneline -10`
- `git status --short`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content docs\harness\AH-PILOT-17.md`
- `Get-Content docs\harness\AH-PILOT-18-A.md`
- `Get-Content docs\harness\AH-PILOT-18-B.md`
- `Get-Content docs\harness\AH-PILOT-19.md`
- `Get-Content docs\harness\AH-PILOT-20.md`

CAAutomationHub read-only inspection:

- `Get-ChildItem -Recurse -Force tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart`
- `Get-ChildItem -Recurse -Force src\CAAutomationHub.PilotFlows.Xgt\WorkStart`
- `Get-ChildItem -Recurse -Force src\CAAutomationHub.PilotFlows\WorkStart`
- `rg -n "WriteProcessPayloadAsync|WriteStartAckAsync|WriteErrorCodeBestEffortAsync|ReadFailed|ReadParseFailed|BulkWriteFailed|AckWriteFailed|OperationFailed|ParseFailed" src\CAAutomationHub.PilotFlows src\CAAutomationHub.PilotFlows.Xgt tests\CAAutomationHub.PilotFlows.Tests tests\CAAutomationHub.PilotFlows.Xgt.Tests`
- `rg -n "LastBulkWrite|LastAckValue|LastErrorCode|ReadContinuous|WriteAsync|ReadAsync|FakePlc|unsupported|NAK|Nak" tests\CAAutomationHub.PilotFlows.Xgt.Tests src\CAAutomationHub.PilotFlows.Xgt`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtPlcOperations.cs`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtWriteOptions.cs`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtReadResultMapper.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowService.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartErrorWritePolicy.cs`

Sibling repo read-only inspection:

- `rg -n "Fault|FaultMode|Failure|FailureMode|FailureKind|Inject|Scenario|Nak|NAK|Malformed|Timeout|NoResponse|ForcedClose|Drop|Delay|Reject|RejectWrite|RejectRead|OnRead|OnWrite" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- `rg -n "LastBulkWrite|LastAckValue|LastErrorCode|ReadContinuous|WriteContinuous|WriteAsync|ReadAsync|FakePlcRuntime|FakePlcProtocolHandler" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- `Get-ChildItem -Recurse -Force C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc`
- `Get-ChildItem -Recurse -Force C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcProtocolHandler.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcRuntime.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcMemoryImage.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcRuleConfig.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcScenarioConfig.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\TestDoubles\FakePlcScenarioServer.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\Channels\PlcChannelFakePlcValidationTests.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\docs\context\04_fakeplc_validation_matrix.md`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\Services\ProcessDataPayloadBuilderTests.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtWriteResponseParser.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Client\XgtSession.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtDriverCore.IntegrationTests\Client\XgtSessionIntegrationTests.cs`
- `rg -n "BuildWriteNakResponse|WriteNak|WriteResponse|Write.*Nak|FakePlcRequestKind.Write|ResponseMode\(FakePlcRequestKind.Write" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src`

Validation:

- `git diff -- docs/harness/AH-PILOT-21.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-21.md`

## 14. git diff --check 결과

- exit code `0`
- 출력 없음
- whitespace error 없음

참고:

- `git diff -- docs/harness/AH-PILOT-21.md`는 출력 없음.
- 이유: `docs/harness/AH-PILOT-21.md`는 신규 untracked 파일이라 plain `git diff -- <path>`에는 표시되지 않는다.
- 문서 내용 확인은 `Get-Content docs\harness\AH-PILOT-21.md`로 수행했다.

## 15. git status --short 결과

```text
?? docs/harness/AH-PILOT-21.md
```

## 16. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-21 목표인 FakePlc failure injection enhancement Boundary Review를 수행했다.
- 현재 production FakePlc와 sibling `FakePlcScenarioServer`의 capability 차이를 구분했다.
- unsupported read address NAK 외에 현재 address-specific write NAK는 지원되지 않음을 확인했다.
- process write failure / ACK write failure / error write best-effort failure가 deterministic integration이 되려면 target-specific one-shot write NAK와 후속 write success가 필요함을 확인했다.
- malformed / timeout / no response / forced close는 현재 CAAutomationHub WorkStart integration 우선순위보다 XgtDriverCore fault matrix 또는 별도 shared harness 보강 쪽이 안전하다고 판단했다.
- CAAutomationHub repo 수정 없이 sibling test-only FakePlcScenarioServer 보강을 AH-PILOT-22 권장 후보로 정리했다.
- Runtime / FlowDefinitions / PilotFlows core / PilotFlows.Xgt production / tests / FakePlc map / adapter / project files를 수정하지 않았다.
- 테스트와 빌드는 문서 작성 작업이므로 실행하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
