# AH-PILOT-07 Closeout - PLC Operation Result Model Boundary Review

## 1. Summary

AH-PILOT-07은 `IWorkStartPlcOperations` seam이 기존 `WorkStartPilotService.RunOnceAsync(...)`의 PLC operation failure를 충분히 표현할 수 있는지 검토한 Boundary Review다.

검토 결과, 현재 `ReadWorkStartBlockAsync() -> byte[]` seam은 read exception을 `1101`로 매핑하는 데는 충분하지만, read NAK / malformed / unexpected response 같은 operation failure와 ACK response 안의 payload extraction / parse failure를 명확히 분리하기에는 부족하다. 특히 기존 `WorkStartPilotService`는 classified exchange 실패를 `group-read / 1101`로, classified exchange는 성공했지만 read response payload block이 비어 있거나 유효하지 않은 경우를 `group-read-parse / 1102`로 구분한다.

반면 `WriteProcessPayloadAsync(...) -> bool`과 `WriteStartAckAsync() -> bool`은 현재 service policy에서 `2501` / `2601`을 자연스럽게 만들 수 있으므로 AH-PILOT-08에서 즉시 result model로 확장할 필요는 낮다. 향후 diagnostics나 adapter failure classification이 필요해지면 write result model을 별도 검토할 수 있다.

권장안은 AH-PILOT-08에서 write bool은 유지하고 read seam만 최소 PilotFlows-local result로 보강하는 것이다. 이 model은 XGT-specific type이나 XGT enum을 노출하지 않고, adapter 내부에서 XGT classification을 vendor-neutral / pilot-local failure kind와 read parse status로 매핑해야 한다.

이번 작업은 read-only 조사와 closeout 문서 작성만 수행했다. production code, test code, interface, service, XGT adapter, DB concrete, FLOW.JSON, Flow Executor, csproj / solution, reference, commit은 수정하지 않았다.

## 2. 현재 IWorkStartPlcOperations seam 검토

현재 seam:

- `EnsureConnectedAsync(CancellationToken)`
- `ReadWorkStartBlockAsync(CancellationToken) -> byte[]`
- `WriteProcessPayloadAsync(byte[], CancellationToken) -> bool`
- `WriteStartAckAsync(CancellationToken) -> bool`
- `WriteErrorCodeBestEffortAsync(WorkStartErrorCode, CancellationToken)`

현재 `WorkStartFlowService` 동작:

- `ReadWorkStartBlockAsync(...)`가 exception을 던지면 `WorkStartStep.GroupRead` / `ReadFailed = 1101`로 실패한다.
- 반환된 `byte[]`를 해석하는 중 exception이 발생하면 `WorkStartStep.GroupReadParse` / `ReadParseFailed = 1102`로 실패한다.
- start signal range가 부족하면 `WorkStartReadBlockInterpreter.IsStartSignalActive(...)`가 false를 반환해 `StartSignalInactive = 1200`으로 간다.
- LOT ID range가 부족하면 `ExtractLotId(...)`가 empty result를 반환하고, 양쪽 LOT ID가 비어 있으면 `LotIdEmpty = 2201`로 간다.

이 구조의 한계:

- `byte[]`는 이미 "read response에서 data block 추출이 끝난 결과"에 가깝다.
- 따라서 XGT NAK / malformed / unexpected command 같은 classified failure는 `byte[]` 안에 표현될 수 없다.
- adapter가 classified failure를 exception으로 던지면 현재 service는 `1101`을 만들 수 있지만, failure kind나 operation failure reason은 사라진다.
- adapter가 read ACK이지만 payload block 없음 / null data를 exception으로 던지면 현재 service는 `1101`로 매핑될 가능성이 높아, 기존 `1102` 의미와 어긋난다.
- empty `byte[]`는 현재 `1102`가 아니라 start signal inactive `1200`으로 내려갈 수 있다.
- short `byte[]`는 위치에 따라 `1200` 또는 `2201`로 내려갈 수 있어, "PLC read response payload parse failure"라는 기존 의미를 보존하기 어렵다.

추가 확인:

- `EnsureConnectedAsync(...)` exception은 현재 read inner catch 밖에서 발생하므로 outer catch를 통해 `UnexpectedException = 2999`로 갈 수 있다.
- 이번 검토의 직접 범위는 read operation result이지만, AH-PILOT-08에서 connection ensure failure를 `1101` 또는 별도 PLC operation failure로 볼지 확인 필요하다.

## 3. read operation result 필요성

현재 `byte[]` 유지 후보는 가장 작지만 다음 경계를 표현하지 못한다.

- operation exchange 실패: timeout, connection, protocol NAK, malformed response, unexpected response
- classified read exchange 성공 후 payload extraction 실패: ACK이지만 variable block 없음, data null, expected block 없음
- service-level business interpretation: start signal inactive, both LOT IDs empty

기존 sibling `WorkStartPilotService` 근거:

- `ExchangeClassifiedWithRecoveryAsync(...)` result가 success가 아니면 `group-read / 1101`
- `XgtFrameParser.Parse(...)`와 `XgtReadResponseParser.Parse(...)` 이후 ACK이 아니거나 variable block이 없거나 data가 null이면 `group-read-parse / 1102`
- 이후 extracted data block에서 start signal을 확인하고 inactive이면 `start-signal / 1200`
- LOT ID extraction 후 둘 다 비어 있으면 `lotid / 2201`

따라서 `1101`과 `1102`를 자연스럽게 만들려면 최소한 다음 구분이 필요하다.

- read operation failed: service가 `1101`로 매핑
- read response parsed enough to know no payload block / invalid payload extraction: service가 `1102`로 매핑
- read data block available: service가 기존 interpreter로 `1200`, `2201`, 이후 flow를 판단

결론:

- AH-PILOT-08에서는 `ReadWorkStartBlockAsync()`가 raw `byte[]`만 반환하는 구조를 보강하는 것이 적절하다.
- 다만 공통 generic result까지 바로 도입하기보다, WorkStart read에 한정된 최소 result가 더 안전하다.
- result model은 XGT-specific classification 이름을 노출하지 않아야 한다.

## 4. write operation result 필요성

현재 write seam:

- `WriteProcessPayloadAsync(byte[]) -> bool`
- `WriteStartAckAsync() -> bool`

현재 service는 다음을 이미 자연스럽게 표현한다.

- process payload write false 또는 exception: `BulkWrite / 2501`
- ACK write false 또는 exception: `AckWrite / 2601`
- 두 failure 모두 `ErrorWriteExpected = true`
- error write best-effort 호출 후 primary failure result 유지

검토 결과:

- bulk write와 ACK write는 현재 error code 분기만 보면 bool로 충분하다.
- XGT NAK / malformed / timeout을 모두 final error code에서는 `2501` 또는 `2601`로 압축해도 기존 pilot policy와 크게 어긋나지 않는다.
- 향후 diagnostics, telemetry, retry policy, adapter-specific observability가 필요하면 write result model을 도입할 수 있다.
- 그러나 지금 공통 operation result를 read/write/ack에 한 번에 적용하면 AH-PILOT-08 범위가 커지고 service tests도 불필요하게 넓어진다.

결론:

- AH-PILOT-08에서는 write bool 유지가 안전하다.
- write result model은 후속 adapter diagnostics review 또는 real XGT adapter integration 전에 별도 검토한다.

## 5. operation failure kind 후보

XGT-specific enum을 직접 노출하지 않고 PilotFlows-local failure kind를 둘 수 있다.

후보:

- `None`
- `Failed`
- `Timeout`
- `Connection`
- `Protocol`
- `UnexpectedResponse`
- `MalformedResponse`
- `Cancelled`
- `Unknown`

주의:

- 이 enum은 `ChannelPollingFailureKind`와 같아야 하지 않는다.
- `ChannelPollingFailureKind`를 직접 참조하지 않는다.
- `XgtResponseClassification.Nak`, `Malformed`, `UnexpectedCommand`를 그대로 복사하지 않는다.
- XGT NAK은 pilot-local 관점에서 `Protocol` 또는 `Failed`로 낮춰 표현할 수 있다.
- XGT malformed는 `MalformedResponse`로 표현할 수 있지만, 이것도 XGT raw parser 세부가 아니라 "operation response가 유효한 업무 응답으로 사용할 수 없음" 수준이어야 한다.
- timeout과 connection은 transport/recovery primitive 의미를 직접 구현하지 말고 adapter가 pilot-local failure kind로 매핑한다.

최소 권장:

- AH-PILOT-08에서는 failure kind를 너무 세분화하지 않아도 된다.
- `Failed`, `Timeout`, `Connection`, `Protocol`, `UnexpectedResponse`, `MalformedResponse`, `Cancelled`, `Unknown` 정도는 adapter mapping에 충분하다.
- service의 error code 결정은 우선 status 중심으로 하고, failure kind는 message / diagnostics 확장 여지로 남기는 것이 안전하다.

## 6. parse failure 표현 방식

기존 `WorkStartReadBlockInterpreter`는 data block 내부의 business offsets를 해석하는 helper다.

현재 interpreter 정책:

- start signal word range가 없으면 inactive false
- LOT ID range가 없으면 empty extraction result
- null input은 exception
- empty block은 start signal inactive로 처리될 수 있음

이 정책은 business-level tolerance에는 유용하지만, 기존 `1102`의 의미와는 다르다.

`1102`의 기존 의미:

- XGT read response는 왔지만 ACK/payload block이 유효하지 않음
- variable block count가 0
- first variable block data가 null
- response body parse 또는 payload extraction 단계에서 WorkStart read data block을 얻지 못함

따라서 parse failure는 두 층으로 나누어 보는 것이 더 정확하다.

- adapter/read response extraction parse failure: `1102`
- WorkStart data block business interpretation: start signal inactive `1200`, LOT ID empty `2201`

결론:

- `ReadWorkStartBlockAsync()`가 성공 result로 data block을 반환했다면 service는 기존 interpreter로 판단한다.
- adapter가 data block을 얻지 못한 경우는 성공 `byte[]` 대신 read result의 parse failure status로 service에 전달하는 편이 맞다.
- empty/null/invalid payload를 무조건 service interpreter에서 `1102`로 만들려고 하면 현재 helper 정책과 기존 pilot 의미가 섞인다.

## 7. best-effort error write와 operation result 관계

기존 정책:

- error write 대상 code: `2201`, `2300`, `2301`, `2302`, `2303`, `2400`, `2501`, `2601`
- error write 미대상 code: `1101`, `1102`, `1200`, `2999`, `None`
- error write 실패는 final result를 바꾸지 않음

현재 `WriteErrorCodeBestEffortAsync(...)` seam은 이 정책에 충분하다.

검토 결과:

- error write 자체의 operation result는 final WorkStart result에 강하게 반영하지 않는다.
- error write 실패 diagnostics가 필요하면 후속 logging/telemetry 영역에서 다룬다.
- AH-PILOT-08에서 read operation result를 도입하더라도 error write result는 도입하지 않는 편이 경계가 작고 안전하다.

## 8. AH-PILOT-08 구현 후보

### 후보 A: read operation result model만 도입

- `IWorkStartPlcOperations.ReadWorkStartBlockAsync(...) -> WorkStartReadBlockOperationResult`
- service는 result status에 따라 `1101` 또는 `1102`를 만든다.
- data block이 있는 success만 기존 interpreter로 넘긴다.
- write bool은 유지한다.

판정:

- 가장 직접적인 후보.
- `1101` / `1102` 경계를 회복할 수 있다.
- model 이름과 status가 너무 adapter-like해지지 않도록 주의 필요.

### 후보 B: 공통 PLC operation result model 도입

- `WorkStartPlcOperationResult`
- `WorkStartPlcOperationResult<T>`
- read/write/ack 공통 사용

판정:

- 장기 확장성은 있지만 AH-PILOT-08에는 크다.
- write는 현재 bool로 충분하므로 불필요한 model 확장이 될 수 있다.
- service tests와 fake setup이 한 번에 커질 위험이 있다.

### 후보 C: write는 bool 유지, read만 result model 도입

- read만 최소 result 도입
- process payload write / ACK write는 bool 유지
- error write best-effort는 현재 유지

판정:

- 권장.
- AH-PILOT-06에서 남긴 read parse failure 공백을 가장 작은 범위로 닫을 수 있다.
- XGT adapter 구현 전에도 fake tests로 `1101` / `1102` 경계를 검증할 수 있다.

### 후보 D: 현재 seam 유지, 1102는 후속 XGT adapter에서만 처리

- `ReadWorkStartBlockAsync() -> byte[]` 유지
- adapter가 parse failure를 exception 또는 special byte block으로 표현

판정:

- 권장하지 않음.
- exception은 현재 `1101`로 매핑될 수 있고, special byte block은 `1200` / `2201`과 섞인다.
- 테스트 공백이 계속 남는다.

### 후보 E: read result / parse result를 나누어 표현

- `WorkStartReadOperationResult`
- `WorkStartReadBlockParseResult`

판정:

- 의미는 가장 명확하지만 model 수가 증가한다.
- AH-PILOT-08에서는 과할 수 있다.
- 실제 adapter 구현 시 response extraction과 business interpretation을 더 세밀하게 나눌 필요가 생기면 재검토한다.

## 9. 권장안

AH-PILOT-08 권장안은 후보 C다.

권장 방향:

- read seam만 최소 result model로 보강한다.
- result status는 최소한 `Succeeded`, `OperationFailed`, `ParseFailed`를 구분한다.
- success일 때만 `byte[]` data block을 가진다.
- operation failure는 service가 `GroupRead / 1101`로 매핑한다.
- parse failure는 service가 `GroupReadParse / 1102`로 매핑한다.
- success data block은 기존 `WorkStartReadBlockInterpreter`에 넘겨 `1200`, `2201`, 이후 flow를 판단한다.
- failure kind는 pilot-local / vendor-neutral로 둔다.
- XGT classification, raw frame, request/response hex, XGT status code, XGT address는 result model에 직접 넣지 않는다.
- write / ACK / error write는 이번에는 유지한다.

최소 model 성격:

- PilotFlows-local
- WorkStart read operation 전용
- adapter가 data block을 추출한 뒤 넘기는 boundary
- Runtime polling state path와 무관
- `ChannelPollingFailureKind`와 무관
- XgtDriverCore type과 무관

확인 필요:

- `EnsureConnectedAsync(...)` failure를 현재처럼 `2999`로 둘지, PLC operation setup failure로 보아 `1101` 계열로 매핑할지 AH-PILOT-08에서 별도 판단이 필요하다.
- 이 판단은 service behavior 변경이므로 AH-PILOT-08 구현 범위에 명시해야 한다.

## 10. 제외한 범위

이번 AH-PILOT-07에서 의도적으로 제외한 범위:

- production code 수정
- test code 수정
- interface 수정
- service 수정
- result model 추가
- XGT Adapter 구현
- DB concrete 구현
- actual PLC read/write 구현
- ACK writer / error writer concrete 구현
- FLOW.JSON 파일 생성 또는 연결
- JSON schema / parser 구현
- Flow Executor 구현
- csproj / solution 수정
- ProjectReference / PackageReference 추가
- commit
- `CAAutomationHub.Runtime` project 수정
- `CAAutomationHub.FlowDefinitions` project 수정
- `XgtDriverCore` reference 추가
- `FakePlc` reference 추가
- `XgtChannelRunner` reference 추가
- `Microsoft.Data.SqlClient` reference 추가
- `ChannelPollingTarget` / `ChannelPollingResult` 수정
- `RuntimeSnapshot` 참조
- `WorkStartPilotService` source copy
- SQL connection string / SQL text 추가
- XGT address를 service / result model에 직접 추가

## 11. 실행한 명령

현재 repo:

- `git log --oneline -8`
- `git status --short`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content docs\harness\AH-PILOT-04.md`
- `Get-Content docs\harness\AH-PILOT-05.md`
- `Get-Content docs\harness\AH-PILOT-06.md`
- `rg --files src\CAAutomationHub.PilotFlows\WorkStart`
- `rg --files tests\CAAutomationHub.PilotFlows.Tests\WorkStart`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\IWorkStartPlcOperations.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowService.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartReadBlockInterpreter.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartErrorCode.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartStep.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowResult.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartErrorWritePolicy.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Tests\WorkStart\WorkStartFlowServiceTests.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Tests\WorkStart\WorkStartReadBlockInterpreterTests.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Tests\WorkStart\WorkStartFlowResultPolicyTests.cs`
- `rg -n "ChannelPollingFailureKind|PollingFailure|Malformed|Nak|UnexpectedResponse|Timeout|Protocol|Connection" src tests`
- `rg -n "ReadParseFailed|ReadFailed|GroupRead|GroupReadParse|StartSignalInactive" src\CAAutomationHub.PilotFlows tests\CAAutomationHub.PilotFlows.Tests`
- `Get-Content src\CAAutomationHub.Runtime\Polling\ChannelPollingFailureKind.cs`
- `Get-Content src\CAAutomationHub.Runtime\Polling\ChannelPollingResult.cs`

Sibling repo:

- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\WorkStartPilotService.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\WorkStartPilotResult.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\PilotScenarioConfig.cs`
- `rg -n "ExchangeClassifiedWithRecoveryAsync|class .*Classified|record .*Classified|IsSuccess|ResponseBytes|Malformed|Nak|NAK|Timeout|Unexpected" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Channels\PlcChannel.cs`
- `rg -n "class PlcRawExchangeResult|record PlcRawExchangeResult|struct PlcRawExchangeResult|ResponseInfo|ResponseClassification|IsSuccess" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\PlcRawExchangeResult.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtRawResponseInfo.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtResponseClassification.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtRawResponseClassifier.cs`

Validation:

- `git diff -- docs/harness/AH-PILOT-07.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-07.md`

테스트 / 빌드:

- 문서 작성만 수행했으므로 실행하지 않았다.

## 12. git diff --check 결과

git diff 확인:

- 실행: `git diff -- docs/harness/AH-PILOT-07.md`
- 결과: 출력 없음
- 이유: `docs/harness/AH-PILOT-07.md`가 신규 untracked 파일이라 tracked diff에 포함되지 않음

실행:

- `git diff --check`

결과:

- pass
- whitespace error 없음

주의:

- `docs/harness/AH-PILOT-07.md`는 신규 untracked 파일로 생성되었다.

## 13. git status --short 결과

실행:

- `git status --short`

결과:

```text
?? docs/harness/AH-PILOT-07.md
```

## 14. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-07 목표인 PLC operation result model Boundary Review를 closeout 문서로 남겼다.
- 현재 `IWorkStartPlcOperations` seam의 표현 가능 범위와 한계를 확인했다.
- read operation failure `1101`과 read response parse / payload extraction failure `1102`를 구분해야 함을 정리했다.
- current `byte[]` seam으로 empty / short payload를 `1102`로 강제하면 `1200` / `2201`과 의미가 섞이는 위험을 확인했다.
- write / ACK write는 현재 bool로도 `2501` / `2601`을 만들 수 있어 AH-PILOT-08에서 유지 가능하다고 판단했다.
- operation failure kind는 XGT-specific enum이 아니라 PilotFlows-local / vendor-neutral 후보로 정리했다.
- best-effort error write는 final result에 강하게 반영하지 않는 기존 정책 유지가 적절하다고 정리했다.
- AH-PILOT-08 구현 후보 A-E를 비교하고 후보 C를 권장했다.
- Runtime / FlowDefinitions project를 수정하지 않았다.
- XgtDriverCore / FakePlc / XgtChannelRunner / SqlClient reference를 추가하지 않았다.
- actual DB query, actual PLC read/write, adapter, executor, FLOW.JSON을 구현하지 않았다.
- requested validation commands를 실행했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
