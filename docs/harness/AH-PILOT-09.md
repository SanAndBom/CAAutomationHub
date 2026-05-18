# AH-PILOT-09 Closeout - XGT Classified Result to WorkStart Read Result Mapping Boundary Review

## 1. Summary

AH-PILOT-09는 실제 XGT adapter 구현 전에, sibling `AutomationHub.XgtDriverCore` / `XgtChannelRunner`의 classified read result, transport failure, malformed response를 `CAAutomationHub.PilotFlows`의 `WorkStartReadBlockOperationResult`로 낮추는 mapping boundary를 검토한 Boundary Review다.

검토 결과, 현재 AH-PILOT-08에서 도입된 `WorkStartReadBlockOperationResult`의 3-state seam은 초기 XGT read adapter에 충분하다. XGT 쪽 ACK + 유효한 read payload는 `Success(data)`로, XGT NAK / malformed / unexpected command / transport failure / timeout / forced close / read operation exception은 `OperationFailed(message)`로, ACK이지만 WorkStart read data block을 추출할 수 없는 응답은 `ParseFailed(message)`로 낮추는 것이 적절하다.

중요한 경계는 XGT-specific enum, raw request/response hex, transport exception detail을 PilotFlows public model로 노출하지 않는 것이다. PilotFlows는 `1101` / `1102`를 결정할 수 있는 최소 상태만 받고, XGT classification / raw hex / transport detail은 향후 adapter diagnostics output으로 분리한다.

이번 작업은 read-only 조사와 closeout 문서 작성만 수행했다. production code, test code, interface, service, XGT adapter, DB concrete, FLOW.JSON, Flow Executor, csproj / solution, reference, commit은 수정하지 않았다.

## 2. 현재 WorkStart read result seam

현재 `IWorkStartPlcOperations.ReadWorkStartBlockAsync(...)`는 `WorkStartReadBlockOperationResult`를 반환한다.

현재 status:

- `Success`
- `OperationFailed`
- `ParseFailed`

현재 service mapping:

- `OperationFailed` -> `WorkStartStep.GroupRead` / `WorkStartErrorCode.ReadFailed = 1101`
- `ParseFailed` -> `WorkStartStep.GroupReadParse` / `WorkStartErrorCode.ReadParseFailed = 1102`
- `Success(data)` -> 기존 `WorkStartReadBlockInterpreter`로 start signal, LOT ID extraction, LOT ID selection 진행

현재 seam의 의미:

- PilotFlows는 XGT ACK / NAK / Malformed / UnexpectedCommand enum을 알지 않는다.
- PilotFlows는 raw request/response hex를 갖지 않는다.
- PilotFlows는 transport failure kind를 알지 않는다.
- PilotFlows는 read operation 결과를 business flow error code로 낮추는 책임만 가진다.

Sibling `WorkStartPilotService.RunOnceAsync(...)`의 기존 기준:

- `ExchangeClassifiedWithRecoveryAsync(...)` 결과가 success가 아니면 `group-read / 1101`
- classified exchange는 success지만 read response payload block이 비어 있거나 null이면 `group-read-parse / 1102`
- data block이 있으면 start signal / LOT ID / DB / payload / write flow로 진행

따라서 AH-PILOT-08 seam은 기존 pilot anchor의 `1101` / `1102` 분리를 PilotFlows-local model로 회복한 상태다.

## 3. XGT success mapping 후보

Success 후보:

- XGT response classification이 ACK
- 요청이 read command이고 응답이 expected read response
- `XgtFrameParser.Parse(...)`가 성공
- `XgtReadResponseParser.Parse(...)`가 성공
- read response status가 ACK
- variable block이 존재
- 첫 variable block의 `Data`가 null이 아님
- WorkStart read block으로 사용할 byte array 추출 성공

PilotFlows mapping:

- `WorkStartReadBlockOperationResult.Success(data)`

주의:

- PilotFlows result에는 `XgtResponseClassification.Ack`를 노출하지 않는다.
- adapter 내부에서만 XGT response를 해석한다.
- success data는 XGT frame 전체가 아니라 WorkStart read payload bytes다.
- success 이후 data block 내부 business interpretation은 현재처럼 `WorkStartReadBlockInterpreter`가 담당한다.

## 4. OperationFailed / 1101 mapping 후보

OperationFailed 후보:

- XGT NAK
- malformed response
- unexpected command / response classification mismatch
- timeout / no response
- forced close / remote close
- send / receive / connect failure
- frame extraction failure
- connection failure
- protocol exchange failure
- read operation exception

근거:

- `XgtRawResponseClassifier`는 ACK이 아닌 protocol-level 결과를 `Nak`, `Malformed`, `UnexpectedCommand`로 분류한다.
- `PlcRawExchangeResult.IsSuccess`는 `ResponseInfo.IsAck` 기준이다.
- sibling `WorkStartPilotService`는 `readResult.IsSuccess == false`를 `group-read / 1101`로 낮춘다.
- `PlcChannel`은 reconnect 후보 transport failure 후에도 current exchange가 실패하면 exception을 전파할 수 있다.
- FakePlc validation matrix의 FP-04 / FP-05는 NAK / malformed가 reconnect 없이 protocol classification failure로 관측된다고 정리한다.
- FP-09는 no response가 reconnect 이후에도 `TransportException(ReceiveTimeout)`으로 종료될 수 있음을 보존한다.

PilotFlows mapping:

- `WorkStartReadBlockOperationResult.OperationFailed(message)`

WorkStartFlowService mapping:

- step: `WorkStartStep.GroupRead`
- code: `WorkStartErrorCode.ReadFailed = 1101`
- error write expected: `false`

판정:

- XGT NAK는 `ParseFailed`가 아니라 `OperationFailed`다.
- malformed response는 `ParseFailed`가 아니라 `OperationFailed`다.
- unexpected command / wrong response type도 `OperationFailed`다.
- timeout / no response / forced close / transport failure는 `OperationFailed`다.

이유:

- `ParseFailed`는 ACK response 이후 WorkStart read payload extraction 단계에서만 사용한다.
- NAK / malformed / unexpected / transport failure는 read operation exchange 자체가 업무 payload를 제공하지 못한 상태다.
- malformed를 parse failure로 낮추면 protocol frame validity 실패와 ACK payload extraction 실패가 섞인다.

## 5. ParseFailed / 1102 mapping 후보

ParseFailed 후보:

- response classification은 ACK으로 보임
- expected read response로 파싱됨
- 하지만 read variable block이 없음
- first variable block이 없음
- data block이 null
- data length가 WorkStart read expected length보다 부족하다고 adapter가 판단
- adapter가 ACK read response에서 WorkStart word bytes를 추출하지 못함
- ACK response structure가 WorkStart read payload로 해석 불가능함

PilotFlows mapping:

- `WorkStartReadBlockOperationResult.ParseFailed(message)`

WorkStartFlowService mapping:

- step: `WorkStartStep.GroupReadParse`
- code: `WorkStartErrorCode.ReadParseFailed = 1102`
- error write expected: `false`

판정:

- ACK이지만 variable block 없음 / data null / payload extraction 실패는 `ParseFailed`다.
- data length 부족은 adapter가 "read operation ACK은 받았지만 WorkStart read block으로 사용할 수 없음"으로 판정하는 경우 `ParseFailed`다.
- 단, success로 넘긴 data block 내부에서 start signal inactive 또는 LOT ID empty가 발생하면 기존 service policy대로 각각 `1200` / `2201`이다.

주의:

- `ParseFailed`는 XGT raw frame malformed와 다르다.
- `ParseFailed`는 WorkStart read payload extraction boundary의 실패다.
- service 내부 business interpreter가 처리할 수 있는 empty / inactive / LOT ID empty 의미와 섞지 않는다.

## 6. Cancellation 처리 후보

현재 확인:

- `WorkStartFlowService.RunAsync(...)`는 일반 `Exception` catch를 사용한다.
- read operation 내부에서 `OperationCanceledException`이 발생하면 현재 구조상 `GroupRead / 1101`로 낮아질 수 있다.
- `EnsureConnectedAsync(...)`나 outer flow에서 cancellation이 발생하면 `Exception / 2999`로 낮아질 수 있다.
- sibling XGT transport는 사용자 cancellation과 timeout cancellation을 구분한다. timeout은 `TransportException(...Timeout)`으로 변환하지만, 사용자 cancellation은 `OperationCanceledException`으로 전파될 수 있다.

권장 후보:

- 사용자 요청 / shutdown cancellation은 `OperationFailed`로 낮추지 않는 방향을 우선 후보로 둔다.
- adapter는 `OperationCanceledException`을 catch해서 `OperationFailed`로 변환하지 말고 전파하는 후보가 안전하다.
- service의 cancellation policy는 별도 AH에서 결정한다.

이유:

- shutdown / user cancellation을 `1101` PLC read failure로 기록하면 운영 취소가 PLC 장애처럼 보일 위험이 있다.
- timeout / no response는 transport failure로 `OperationFailed / 1101`에 들어갈 수 있지만, cancellation은 제어 흐름에 더 가깝다.

AH-PILOT-09 판정:

- cancellation은 이번 단계에서 구현하지 않는다.
- cancellation은 `OperationFailed`로 확정하지 않고, "flow cancellation 또는 예외 전파 후보"로 남긴다.
- AH-PILOT-10 이후 실제 adapter skeleton 전에 cancellation policy review가 필요할 수 있다.

## 7. diagnostics / message 처리 후보

권장 message 원칙:

- PilotFlows result message에는 safe summary만 담는다.
- raw request/response hex는 `WorkStartReadBlockOperationResult`에 넣지 않는다.
- XGT response classification enum은 PilotFlows public model로 노출하지 않는다.
- `TransportFailureKind` enum은 PilotFlows public model로 노출하지 않는다.
- transport exception message를 그대로 외부 UI / business result에 흘리는 것은 피하고 adapter에서 축약한다.

OperationFailed message 후보:

- `PLC read operation failed.`
- `PLC read response was rejected.`
- `PLC read transport failed.`
- `PLC read response was malformed.`

ParseFailed message 후보:

- `PLC read payload extraction failed.`
- `PLC read response payload is empty.`
- `PLC read response payload is shorter than expected.`

Adapter diagnostics 후보:

- request hex
- response hex
- XGT classification name
- status code / error code
- transport failure kind
- reconnect performed 여부
- channel id
- elapsed time

판정:

- PilotFlows result message는 운영 판정용 safe summary다.
- 상세 분석용 raw hex / classification / transport details는 adapter diagnostics output 또는 logging / telemetry로 분리한다.
- AH-PILOT-09에서는 diagnostics model을 구현하지 않는다.

## 8. 후보 A / B / C / D 검토

### 후보 A: XGT adapter가 바로 WorkStartReadBlockOperationResult를 반환

판정:

- 단기 구현 후보로 가능하다.
- WorkStart 전용 adapter라면 속도가 빠르고 AH-PILOT-08 seam을 바로 사용할 수 있다.

장점:

- `WorkStartFlowService`가 바로 사용할 수 있다.
- `1101` / `1102` mapping이 adapter 내부에서 명확하다.
- PilotFlows service 변경을 최소화한다.

위험:

- adapter project가 PilotFlows model을 직접 참조하게 된다.
- adapter가 WorkStart 전용으로 굳어질 수 있다.
- 향후 다른 flow에서 XGT read adapter 재사용성이 낮아질 수 있다.

### 후보 B: adapter-local result를 만들고 PilotFlows result로 mapping

판정:

- 장기적으로 가장 깔끔한 후보지만 초기 구현량이 늘어난다.

장점:

- XGT-specific layer와 PilotFlows boundary를 분리할 수 있다.
- adapter diagnostics를 보존하기 쉽다.
- 향후 다른 pilot flow에 재사용 가능한 XGT read result를 만들 수 있다.

위험:

- 모델이 늘어난다.
- AH-PILOT-10 skeleton 범위가 커질 수 있다.
- 초기 pilot 전환 속도가 느려질 수 있다.

### 후보 C: PilotFlows에 operation failure kind를 확장

판정:

- 지금은 비권장이다.

장점:

- service나 tests에서 timeout / protocol / malformed 차이를 직접 볼 수 있다.

위험:

- PilotFlows가 driver-like failure model을 소유하게 된다.
- XGT / transport 의미가 PilotFlows public model로 새어 들어올 수 있다.
- `ChannelPollingFailureKind` 또는 `TransportFailureKind`와 의미가 섞일 수 있다.
- Runtime polling state path와 pilot business flow boundary가 흐려질 수 있다.

### 후보 D: 현재 WorkStartReadBlockOperationResult 유지 + adapter에서 3-state로 낮춤

판정:

- AH-PILOT-09 권장안이다.

장점:

- 현재 최소 모델 유지
- WorkStartFlowService 변경 불필요
- `1101` / `1102` 구분 가능
- XGT detail 유입 방지
- Runtime / FlowDefinitions / RuntimeSnapshot / ChannelPollingResult 경계를 건드리지 않음

위험:

- diagnostics는 별도 경로가 필요하다.
- timeout / protocol / malformed 세부 차이는 PilotFlows result에서 사라진다.
- AH-PILOT-10에서 adapter diagnostics 위치를 별도로 정해야 한다.

## 9. 권장안

AH-PILOT-09 권장안은 후보 D다.

권장 mapping:

- XGT ACK + expected read response + payload extraction success -> `Success(data)`
- XGT NAK -> `OperationFailed`
- malformed response -> `OperationFailed`
- unexpected command / response mismatch -> `OperationFailed`
- timeout / no response / forced close / transport failure -> `OperationFailed`
- connection failure / read operation exception -> `OperationFailed`
- ACK이지만 variable block 없음 / data null / expected payload extraction 실패 -> `ParseFailed`
- user cancellation / shutdown cancellation -> `OperationFailed`로 낮추지 말고 예외 전파 또는 별도 cancellation path 후보 유지

권장 boundary:

- `WorkStartReadBlockOperationResult`는 3-state로 유지한다.
- PilotFlows public model에 XGT classification enum을 가져오지 않는다.
- PilotFlows public model에 `TransportFailureKind`를 가져오지 않는다.
- raw hex를 `WorkStartReadBlockOperationResult`에 넣지 않는다.
- XGT adapter가 내부 classification을 3-state로 낮춘다.
- detailed diagnostics는 adapter diagnostics output 후보로 분리한다.

AH-PILOT-10에서 실제 XGT read adapter skeleton을 구현해도 되는지:

- project boundary가 확정되기 전에는 바로 구현하지 않는 것이 안전하다.
- 어디에 XgtDriverCore reference를 둘지, adapter project / namespace / reference 방향을 먼저 확정해야 한다.
- 따라서 AH-PILOT-10은 "XGT Adapter Project Boundary Review"가 우선 후보다.

## 10. AH-PILOT-10 후보

### 후보 1: AH-PILOT-10 XGT Adapter Project Boundary Review

권장 우선순위: 1

검토 대상:

- 어디에 XgtDriverCore reference를 둘지
- adapter project name / namespace
- PilotFlows와 adapter reference 방향
- sibling repo clean anchor
- diagnostics output 위치
- cancellation policy
- test-only FakePlc 연결 경계

판정:

- 가장 안전한 다음 단계다.
- AH-PILOT-09에서 mapping boundary는 닫았지만 reference boundary는 아직 닫지 않았다.

### 후보 2: AH-PILOT-10 WorkStart XGT Read Adapter Skeleton

권장 우선순위: 2

조건:

- project boundary가 이미 명확하다고 사용자 판단이 있을 때만 가능하다.
- XgtDriverCore reference 위치가 확정되어야 한다.
- diagnostics와 cancellation의 최소 정책이 정리되어야 한다.

판정:

- 지금 바로 진행하기에는 reference boundary가 아직 열려 있다.

### 후보 3: AH-PILOT-10 DB Query Boundary Review

권장 우선순위: 3

판정:

- XGT보다 DB를 먼저 붙일지 검토하는 단계로 가능하다.
- 단, 이번 AH-PILOT-09의 직접 후속성은 XGT adapter reference boundary 쪽이 더 높다.

### 후보 4: AH-PILOT-10 Fake Integration Harness Review

권장 우선순위: 4

판정:

- FakePlc를 test-only harness로 어떻게 붙일지 검토하는 단계로 유용하다.
- XGT adapter project boundary 이후에 더 구체화하는 편이 안전하다.

## 11. 제외한 범위

이번 AH-PILOT-09에서 의도적으로 제외한 범위:

- `CAAutomationHub.Runtime` project 수정
- `CAAutomationHub.FlowDefinitions` project 수정
- `CAAutomationHub.PilotFlows` production code 수정
- PilotFlows test code 수정
- interface 수정
- service 수정
- XGT Adapter 구현
- DB concrete 구현
- actual PLC read/write 구현
- ACK writer / error writer concrete 구현
- FLOW.JSON 파일 생성 또는 연결
- JSON schema / parser 구현
- Flow Executor 구현
- csproj / solution 수정
- ProjectReference / PackageReference 추가
- XgtDriverCore reference 추가
- FakePlc reference 추가
- XgtChannelRunner reference 추가
- Microsoft.Data.SqlClient reference 추가
- ChannelPollingTarget / ChannelPollingResult 수정
- RuntimeSnapshot 참조
- WorkStartPilotService source copy
- SQL connection string / SQL text 추가
- XGT classification enum을 PilotFlows public model로 이식
- raw hex를 `WorkStartReadBlockOperationResult`에 추가
- commit

## 12. 실행한 명령

현재 repo:

- `git log --oneline -8`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-07.md`
- `Get-Content docs\harness\AH-PILOT-08.md`
- `rg --files src\CAAutomationHub.PilotFlows\WorkStart`
- `rg --files tests\CAAutomationHub.PilotFlows.Tests\WorkStart`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartReadBlockOperationResult.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartReadBlockOperationStatus.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\IWorkStartPlcOperations.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowService.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartErrorCode.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartStep.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Tests\WorkStart\WorkStartFlowServiceTests.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Tests\WorkStart\WorkStartReadBlockOperationResultTests.cs`
- `rg -n "Cancellation|OperationCanceled|TaskCanceled|cancellationToken" src\CAAutomationHub.PilotFlows\WorkStart tests\CAAutomationHub.PilotFlows.Tests\WorkStart`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `rg -n "XgtDriverCore|XgtChannelRunner|FakePlc|Microsoft.Data.SqlClient|ChannelPollingResult|RuntimeSnapshot|FLOW.JSON|FlowExecutor|WorkStartPilotService" src\CAAutomationHub.PilotFlows tests\CAAutomationHub.PilotFlows.Tests`
- `rg -n "ProjectReference|PackageReference" src\CAAutomationHub.PilotFlows\CAAutomationHub.PilotFlows.csproj tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`

Sibling repo:

- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\WorkStartPilotService.cs`
- `rg -n "ExchangeClassifiedWithRecoveryAsync|XgtRawResponseClassifier|XgtRawResponseInfo|XgtResponseClassification|XgtFrameParser|TransportFailureKind|TransportException|ForcedClose|Timeout|Malformed|Nak|NAK|VariableBlock|Data" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- `rg --files C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore`
- `rg --files C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtResponseClassification.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtRawResponseInfo.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtRawResponseClassifier.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtFrameParser.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtReadResponseParser.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Validation\TransportFailureKind.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Validation\TransportException.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Channels\PlcChannel.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\PlcRawExchangeResult.cs`
- `rg -n "Cancellation|OperationCanceled|TaskCanceled|cancellationToken" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\WorkStartPilotService.cs C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Channels\PlcChannel.cs C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\docs\context\04_fakeplc_validation_matrix.md`

Validation:

- `git diff -- docs/harness/AH-PILOT-09.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-09.md`

테스트 / 빌드:

- 문서 작성만 수행했으므로 실행하지 않았다.

## 13. git diff --check 결과

실행:

- `git diff --check`

결과:

- pass
- whitespace error 없음

주의:

- `docs/harness/AH-PILOT-09.md`는 신규 untracked 파일로 생성되었다.

## 14. git status --short 결과

실행:

- `git status --short`

결과:

```text
?? docs/harness/AH-PILOT-09.md
```

## 15. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-09 목표인 XGT classified result to WorkStart read result mapping boundary를 closeout 문서로 남겼다.
- 현재 WorkStart read result seam이 `Success` / `OperationFailed` / `ParseFailed` 3-state임을 확인했다.
- XGT ACK + payload extraction success는 `Success(data)`로 낮추는 후보를 정리했다.
- XGT NAK / malformed / unexpected command / transport failure는 `OperationFailed / 1101`로 낮추는 후보를 정리했다.
- ACK이지만 variable block 없음 / data null / payload extraction failure는 `ParseFailed / 1102`로 낮추는 후보를 정리했다.
- cancellation은 PLC 장애로 확정하지 않고 flow cancellation 또는 예외 전파 후보로 남겼다.
- PilotFlows result message에는 safe summary만 담고 raw hex / classification / transport detail은 adapter diagnostics로 분리하는 방향을 정리했다.
- 후보 A / B / C / D를 비교하고 후보 D를 권장했다.
- AH-PILOT-10은 XGT Adapter Project Boundary Review를 우선 후보로 제안했다.
- Runtime / FlowDefinitions / PilotFlows production code를 수정하지 않았다.
- test code, interface, service, XGT adapter, DB concrete, FLOW.JSON, Flow Executor를 수정하거나 구현하지 않았다.
- XgtDriverCore / FakePlc / XgtChannelRunner / SqlClient reference를 추가하지 않았다.
- XGT classification enum을 PilotFlows public model로 가져오지 않았다.
- requested validation commands를 실행했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
