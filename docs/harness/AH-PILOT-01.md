# AH-PILOT-01 Closeout - WorkStart Pilot Fast Port Boundary Review

## 1. Summary

AH-PILOT-01은 `XgtChannelRunner`의 검증된 `WorkStartPilotService.RunOnceAsync(...)` 흐름을 `CAAutomationHub` 안으로 빠르게 옮기기 전에, 이식 범위와 금지 범위, 첫 구현 단위를 확정한 Boundary Review다.

핵심 결론은 `RunOnceAsync(...)`를 직접 source copy하거나 Runtime core에 넣지 않는 것이다. 기존 흐름은 중요한 business sequence anchor지만, 현재 구현은 `PlcChannel`, XGT raw frame builder/parser, SQL connection string, hard-coded address, WinForms runner diagnostic에 묶여 있다. 따라서 AH-PILOT 라인은 Runtime polling state path와 분리된 Business Flow / PilotFlows 계층에서 선택 이식해야 한다.

AH-PILOT-02의 1순위 구현 후보는 `ProcessDataPayloadBuilder` 선택 이식과 tests다. 단, `PilotScenarioConfig` 전체나 address hard-code를 그대로 가져오지 않고, payload layout / process data / output payload shape를 Runtime-neutral 또는 PilotFlow-local helper로 재구성해야 한다.

이번 작업은 read-only 조사와 closeout 문서 생성만 수행했다. production code, test code, project / solution / csproj, source copy, reference 추가, `FLOW.JSON`, JSON schema, parser, executor, DB query, Payload Builder 이식, commit은 수행하지 않았다.

## 2. 확인한 현재 anchor

현재 최신 commit anchor:

- `45fcf53 AH-RUNTIME-57-60 add flow definition candidate and validator boundary`

확인 결과:

- `CAAutomationHub.Runtime`은 vendor-neutral Runtime state / polling target / polling result / snapshot publish path를 유지한다.
- `CAAutomationHub.FlowDefinitions`는 neutral `FlowDefinitionCandidate`, `FlowStepCandidate`, `FlowReference`, `FlowPolicyReference`, `IFlowDefinitionValidator`만 가진다.
- `FlowDefinitions`는 parser / executor / JSON schema / XGT / DB / payload implementation이 아니다.
- `RuntimeProjectReferenceBoundaryTests`는 Runtime project가 `XgtDriverCore`, `XgtChannelRunner`, `FakePlc`, `Wpf`를 참조하지 않도록 잠근다.
- `COGNITIVE_SYNC_CHECK.md`의 Current Anchors는 AH-RUNTIME-51 기준으로 남아 있으나, 이번 review는 사용자 지시와 실제 git log 기준인 AH-RUNTIME-57~60 / `45fcf53`를 최신 anchor로 보정해 읽었다.

## 3. 확인한 기존 WorkStartPilotService 근거

확인한 sibling repo 파일:

- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\WorkStartPilotService.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\PilotScenarioConfig.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\WorkStartPilotResult.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\ProcessDataPayloadBuilder.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\LotDataQueryService.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\Services\ProcessDataPayloadBuilderTests.cs`
- 필요 범위에서 `MainForm.cs`의 `btnRunPilotOnce_Click`

확인한 핵심 sequence:

1. `PlcChannel.EnsureConnectedAsync`
2. `%DB10000`, 90 word continuous read
3. start signal word 확인, 기본 index `80`
4. LOT ID 1 / LOT ID 2 추출, word offset `0`, `10`, length `6`
5. LOT ID 1 우선 선택, 없으면 LOT ID 2 선택
6. SQL DB query
7. process data를 `%DB11000` bulk write payload로 packing
8. bulk write 실행
9. `%DB11416`에 ACK value `1` write
10. 실패 시 일부 단계에서 `%DB11410`에 error code best-effort write

Error write 수행:

- `2201`
- `2300`
- `2301`
- `2302`
- `2303`
- `2400`
- `2501`
- `2601`

Error write 미수행:

- `1101`
- `1102`
- `1200`
- `2999`

기존 tests 근거:

- payload packing tests: LOT ID / PROFILE ASCII, single char word, two-word numeric little-endian int32
- scenario tests: happy path, DB not found, bulk write failure, transport / unexpected failure

## 4. 직접 이식 금지 대상

아래 항목은 AH-PILOT 라인으로 그대로 가져오지 않는다.

| 금지 대상 | 금지 이유 |
| --- | --- |
| `btnRunPilotOnce_Click` 전체 | WinForms UI entry point이며 channel 선택, polling stop, button state, textbox, label, messagebox 표시 책임을 포함한다. |
| `MainForm` / WinForms control 접근 코드 | CAAutomationHub의 business flow나 Runtime core가 UI control state를 알면 WPF / Runtime boundary가 깨진다. |
| `MessageBox` / `TextBox` / `Label` / `Button` state | UI projection 책임이며 flow execution result와 분리해야 한다. |
| `PlcChannel` concrete | connection / reconnect / raw exchange / recovery state를 포함한 XgtChannelRunner concrete다. Pilot business flow는 operation adapter seam을 통해야 한다. |
| `PollingCoordinator` stop/start 제어 | Runtime polling lifecycle / Supervisor 책임과 충돌할 수 있다. Pilot command flow가 polling loop를 직접 제어하면 shared execution path가 흔들린다. |
| `XgtFrameBuilder` / `XgtFrameParser` 직접 호출 코드 | raw protocol primitive는 XGT operation adapter 내부 책임이다. Business flow helper가 raw frame을 만들면 vendor-neutral 경계가 깨진다. |
| raw request / response hex 중심 UI 표시 코드 | diagnostics projection이며 core business result가 아니다. 필요하면 adapter diagnostics policy로 분리한다. |
| `LotDataQueryService`의 SQL connection string / SQL text 직접 사용 | SQL policy와 connection detail은 DB query abstraction 또는 external config 책임이다. CAAutomationHub core에 hard-code하지 않는다. |
| `PilotScenarioConfig` hard-coded default 전체 | XGT address, SQL text, timeout, polling interval, complete ACK 후보가 섞여 있어 그대로 가져오면 config가 business/helper/runtime 경계를 오염시킨다. |
| `XgtChannelRunner` project reference | WinForms, SQL, channel manager, polling coordinator, XGT dependency가 연쇄 유입된다. 사용자 지시와 Runtime boundary 모두에 어긋난다. |
| `XgtDriverCore` reference | 이번 단계의 명시적 금지 대상이며, 실제 adapter seam 확정 후 별도 단계에서만 검토한다. |
| `FakePlc` reference | production dependency가 아니라 harness dependency다. 이번 단계에서는 reference를 추가하지 않는다. |

## 5. 빠르게 이식 가능한 후보 분류

| 후보 | 분류 | 판단 |
| --- | --- | --- |
| LOT ID extraction rule | 바로 이식 가능 | byte block에서 word offset / word length 기준 ASCII trim을 수행하는 pure helper에 가깝다. |
| LOT ID selection rule | 바로 이식 가능 | LOT ID 1 우선, 없으면 LOT ID 2는 business decision rule로 분리 가능하다. |
| start signal interpretation | 재구성 후 이식 가능 | word index와 nonzero rule은 단순하지만, request detection과 flow trigger policy로 이름을 보정해야 한다. |
| error code mapping table | 재구성 후 이식 가능 | 현재 WorkStart verified flow 기준으로는 명확하나, ACK OFF / Complete flow와 공통 정책은 아직 미결이다. |
| error write policy | adapter seam 필요 | 어떤 error가 write되는지는 business policy지만 실제 write는 PLC operation adapter가 맡아야 한다. |
| ACK write policy | adapter seam 필요 | ACK ON/OFF action은 business policy지만 실제 address/value write는 adapter 책임이다. |
| `ProcessDataPayloadBuilder` packing rule | 재구성 후 이식 가능 | pure helper에 가깝고 tests가 있으나 `PilotScenarioConfig.WriteStartVariable`, `WriteWordCount`, hard-coded layout 의존을 분리해야 한다. |
| `WorkStartPilotResult` 개념 | 재구성 후 이식 가능 | result shape는 유용하지만 raw hex / reconnect attempts 같은 diagnostics는 adapter diagnostics로 분리해야 한다. |
| happy path scenario test | 재구성 후 이식 가능 | 기존 sequence 검증 anchor로 유용하나 `PlcChannel` / `IXgtSession` stub 의존은 새 operation interface test로 바꿔야 한다. |
| DB not found scenario | DB abstraction 필요 | `2301` mapping과 error write expected는 보존하되 DB query는 interface로 대체해야 한다. |
| bulk write failure scenario | adapter seam 필요 | `2501` mapping과 error write expected는 보존하되 PLC write failure는 operation adapter result로 표현해야 한다. |
| transport / unexpected failure scenario | adapter seam 필요 | `2999`는 현재 outer exception policy anchor이나 실제 transport classification은 adapter boundary에서 분리해야 한다. |
| payload packing tests | 바로 이식 가능에 가까움 | AH-PILOT-02의 가장 좋은 test anchor다. 단 model/config 이름은 CAAutomationHub 쪽으로 재구성한다. |

## 6. XGT operation adapter seam

`WorkStartPilotService`에서 직접 수행하던 아래 작업은 business flow service가 직접 호출하지 않고 adapter seam으로 분리해야 한다.

- ensure connected
- read words
- write process payload words
- write ACK value
- write error code best-effort
- operation failure / timeout / malformed / NAK classification

후보 interface:

- `IPilotPlcOperations`
- `IWorkStartPlcOperations`
- `IPlcWordBlockClient`
- `IPilotPlcWriter`

권장 방향:

- AH-PILOT-02에서 바로 구현하지 않는다.
- AH-PILOT-03 또는 후속에서 PilotFlowService skeleton을 만들 때 interface shape를 최소화한다.
- XGT address, raw frame, parser, classifier, `TransportException`은 이 adapter 구현 내부에만 둔다.

## 7. DB query abstraction

`LotDataQueryService`는 직접 이식하지 않는다.

필요 abstraction 후보:

- `IWorkStartDataQuery`
- `ILotProcessDataQuery`
- `IProcessDataQueryService`

필요 result:

- success with process data
- not found
- multiple rows
- failed with message
- exception path

분리 대상:

- SQL connection string
- SQL text
- query timeout
- `@LotId` parameter binding
- DB row to process data mapping

권장 방향:

- AH-PILOT-02에서는 DB query abstraction을 구현하지 않는다.
- payload builder tests에는 in-memory process data model만 사용한다.
- WorkStart flow skeleton 단계에서 query interface를 도입한다.

## 8. Payload Builder 분리

`ProcessDataPayloadBuilder`는 AH-PILOT-02의 1순위 후보지만 그대로 copy하지 않는다.

보존할 packing rule:

- payload length는 write word count 기준 byte length로 산출
- LOTID_1: byte offset `0`, 12 bytes, 6 words
- LOTID_2: byte offset `20`, 12 bytes, 6 words, 현재 empty write
- PROFILE: byte offset `80`, 18 bytes, 9 words
- TBLR: byte offset `100`, single ASCII word
- WIN_TYPE: byte offset `104`, single ASCII word
- CUT_SIZE: byte offset `108`, little-endian int32, 2 words
- LR: byte offset `112`, single ASCII word
- RollerYN: byte offset `116`, single ASCII word
- ROLLER_HOLE_POS: byte offset `120`, little-endian int32, 2 words
- ROLLER_HOLE_WIDTH: byte offset `124`, little-endian int32, 2 words
- ROLLER_HOLE_LENGTH: byte offset `128`, little-endian int32, 2 words
- ROLLER_TYPE: byte offset `132`, single ASCII word
- CUT_DEGREE: byte offset `136`, little-endian int32, 2 words

분리할 것:

- `PilotScenarioConfig` 전체 의존
- `%DB11000` address
- SQL result DTO naming 그대로 사용
- layout hard-code의 장기 고정

AH-PILOT-02 권장 shape:

- PilotFlow-local process data model
- PilotFlow-local payload output model
- payload layout constants 또는 options를 최소 범위로 둔다.
- existing tests를 CAAutomationHub 기준으로 재구성한다.

## 9. 첫 구현 단위 후보 A / B / C / D / E 검토

### 후보 A: ProcessDataPayloadBuilder 선택 이식 + tests

판정: 1순위 권장.

장점:

- 비교적 pure helper에 가깝다.
- 기존 payload packing tests가 있다.
- XGT / DB / Runtime dependency 없이 CAAutomationHub 안에서 빠르게 검증 가능하다.
- 이후 PilotFlowService skeleton의 payload boundary로 자연스럽게 연결된다.

위험:

- layout이 hard-coded이면 후속 `FLOW.JSON` / payload layout ref 단계에서 재작업 가능성이 있다.
- `PilotScenarioConfig` 의존을 제거해야 한다.
- SQL DTO 이름과 field 이름을 무비판적으로 고정하면 business model이 과도하게 빨리 굳을 수 있다.

권장 보정:

- source copy가 아니라 선택 재구성으로 진행한다.
- tests는 payload length, ASCII fixed field, single ASCII word, int32 little-endian, truncation/blank behavior 중심으로 시작한다.

### 후보 B: LOT ID extraction / selection helper + tests

판정: 2순위.

장점:

- 가장 작고 안전하다.
- XGT / DB / payload 의존이 없다.
- WorkStart flow의 핵심 decision rule이다.

위험:

- 너무 작아서 AH-PILOT 구현 가속의 체감 진전이 약하다.
- payload builder와 별도 파일로 만들 경우 helper 분산이 빨라질 수 있다.

권장 보정:

- AH-PILOT-02에서 후보 A와 같은 project/module 안에 포함할 수 있다.
- 별도 first task로 떼기보다 payload builder 다음에 이어 붙이는 편이 좋다.

### 후보 C: WorkStartPilotResult / error code model skeleton

판정: 3순위.

장점:

- business flow result shape를 먼저 잡으면 이후 service test가 쉬워진다.
- error code mapping table과 error write policy를 명시할 수 있다.

위험:

- operation interface 없이 result model만 만들면 추상적인 DTO가 먼저 굳는다.
- raw hex / reconnect attempts 같은 diagnostic 필드를 그대로 가져올 위험이 있다.

권장 보정:

- AH-PILOT-02의 주 작업으로는 과하다.
- PilotFlowService skeleton 직전에 최소 result shape로 도입한다.

### 후보 D: PilotFlowService skeleton with interfaces

판정: 4순위.

장점:

- 실제 flow 구조가 빠르게 보인다.
- `IPilotPlcOperations`, `IWorkStartDataQuery`, `IWorkStartPayloadBuilder` seam을 한 번에 둘 수 있다.

위험:

- 한 번에 범위가 커진다.
- interface가 실제 구현 없이 과설계될 수 있다.
- error policy, ACK OFF, Complete flow와의 관계가 아직 남아 있다.

권장 보정:

- payload builder와 LOT helper test가 생긴 뒤, 최소 happy path와 2~3개 failure path를 기준으로 도입한다.

### 후보 E: XGT operation adapter seam

판정: 보류.

장점:

- 기존 XgtDriverCore 연결로 가는 관문이다.
- 실제 PLC 통신에 가깝다.

위험:

- 이번 단계에서는 `XgtDriverCore` reference 추가가 명시적으로 금지되어 있다.
- sibling repo dirty state가 남아 있다.
- Runtime core boundary가 흔들릴 수 있다.

권장 보정:

- interface seam review는 가능하지만 구현과 reference 추가는 후속 clean anchor / adapter boundary 단계로 미룬다.

## 10. 권장 첫 구현 선택

AH-PILOT-02 권장 1순위:

- `ProcessDataPayloadBuilder` 선택 이식 + tests

권장 이유:

- 빠른 체감 진전이 있다.
- 기존 XgtChannelRunner payload tests를 가장 많이 활용할 수 있다.
- XGT / DB dependency 없이 검증 가능하다.
- Runtime core 오염 없이 별도 PilotFlows / BusinessFlows 계층에 둘 수 있다.
- 후속 PilotFlowService skeleton이 사용할 실제 helper boundary가 먼저 생긴다.
- 과설계 위험이 후보 D/E보다 낮다.

AH-PILOT-02 최소 범위:

- 새 business flow 계층 위치 확정
- payload builder helper
- process data input model
- payload result model
- payload packing tests
- LOT ID extraction / selection helper는 가능하면 같은 단계의 작은 companion helper로 포함

AH-PILOT-02에서 계속 제외:

- `WorkStartPilotService` 전체 copy
- `PlcChannel` / `XgtDriverCore` 연결
- DB query implementation
- PilotFlowService full sequence
- FLOW.JSON / parser / executor

## 11. 배치 위치 후보 검토

| 위치 후보 | 판정 | 이유 |
| --- | --- | --- |
| `src/CAAutomationHub.PilotFlows` | 1순위 권장 | WorkStart / WorkComplete business transaction을 Runtime core 밖에 둘 수 있다. Pilot 구현 가속 라인이라는 이름과도 맞다. |
| `src/CAAutomationHub.BusinessFlows` | 2순위 | 장기적으로 WorkStart 외 업무 flow를 포괄하기 좋지만, 현재 pilot 단계에서는 이름이 넓어질 수 있다. |
| `src/CAAutomationHub.WorkStart` | 조건부 | 좁고 빠르지만 WorkComplete / ACK OFF / 공통 policy로 확장할 때 이름이 좁을 수 있다. |
| `src/CAAutomationHub.FlowDefinitions` 안 | 비권장 | FlowDefinitions는 candidate model / validator interface boundary다. payload builder 같은 execution helper를 넣으면 definition과 implementation이 섞인다. |
| `src/CAAutomationHub.Runtime` 안 | 비권장 | Runtime core는 vendor-neutral state / polling / snapshot owner다. XGT address, LOTID, DB, payload, ACK policy가 들어오면 boundary가 깨진다. |

권장:

- AH-PILOT-02에서 새 project를 만든다면 `src/CAAutomationHub.PilotFlows`가 가장 안전하다.
- project 수 증가가 부담이면 먼저 문서에서 위치를 확정한 뒤, 구현 단계에서 최소 project와 tests만 추가한다.
- Runtime project에는 reference를 추가하지 않는다.

## 12. AH-PILOT 라인과 AH-RUNTIME 라인 관계

AH-RUNTIME:

- vendor-neutral Runtime state
- channel registry
- polling target/result
- polling result state orchestration
- snapshot publish path
- neutral flow definition candidate model
- validation result / validator interface boundary

AH-PILOT:

- WorkStart / WorkComplete business transaction
- LOTID extraction / selection
- DB query abstraction
- process payload build
- ACK / error write policy
- XGT operation adapter-adjacent seam

서로 섞지 않아야 할 것:

- AH-PILOT result가 `ChannelPollingResult`로 들어가면 안 된다.
- AH-RUNTIME polling target에 XGT address, datatype, count를 넣으면 안 된다.
- AH-PILOT helper가 `RuntimeSnapshot`을 몰라야 한다.
- `CAAutomationHub.Runtime`이 WorkStart payload layout을 소유하면 안 된다.
- `CAAutomationHub.FlowDefinitions`가 payload builder implementation을 소유하면 안 된다.

관계 정리:

- AH-RUNTIME은 canonical state와 neutral validation boundary를 보존한다.
- AH-PILOT은 adapter-adjacent business flow를 구현하되 Runtime core를 오염시키지 않는다.
- 장기적으로 PilotFlows는 FlowDefinitions candidate 또는 future FLOW.JSON output을 소비할 수 있지만, 현재 AH-PILOT-02에서는 parser/executor 없이 pure helper부터 시작한다.

## 13. 제외한 범위

이번 AH-PILOT-01에서는 다음을 하지 않았다.

- 코드 수정
- 테스트 수정
- project / solution / csproj 수정
- source copy
- `XgtDriverCore` reference 추가
- `FakePlc` reference 추가
- `XgtChannelRunner` reference 추가
- `FLOW.JSON` 파일 생성
- JSON schema 생성
- parser 구현
- Flow Executor 구현
- DB Query 구현
- Payload Builder 이식
- `WorkStartPilotService` copy
- `btnRunPilotOnce_Click` copy
- commit
- `ContextPublisher` 자동 publish 재도입

## 14. Validation

문서 작성 후 실행 대상:

- `git diff -- docs/harness/AH-PILOT-01.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-01.md`

테스트 / 빌드:

- 문서 작성만 수행했으므로 실행하지 않는다.

## 15. 실행한 명령

현재 repo:

- `git log --oneline -10`
- `git status --short`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content docs\harness\AH-RUNTIME-42.md`
- `Get-Content docs\harness\AH-RUNTIME-43.md`
- `Get-Content docs\harness\AH-RUNTIME-44.md`
- `Get-Content docs\harness\AH-RUNTIME-45.md`
- `Get-Content docs\harness\AH-RUNTIME-47.md`
- `Get-Content docs\harness\AH-RUNTIME-50.md`
- `Get-Content docs\harness\AH-RUNTIME-57.md`
- `Get-Content docs\harness\AH-RUNTIME-58.md`
- `Get-Content docs\harness\AH-RUNTIME-59.md`
- `Get-Content docs\harness\AH-RUNTIME-60.md`
- `rg --files src\CAAutomationHub.FlowDefinitions src\CAAutomationHub.Runtime tests\CAAutomationHub.Runtime.Tests`
- `Get-Content` for selected FlowDefinitions / Runtime polling / Runtime boundary test files
- `rg -n "Xgt|FakePlc|XgtChannelRunner|FLOW.JSON|FlowDefinition|FlowExecutor|Payload|LotId|ACK|ErrorCode|DB|Sql" src\CAAutomationHub.Runtime src\CAAutomationHub.FlowDefinitions tests\CAAutomationHub.Runtime.Tests`
- `rg -n "<ProjectReference|<PackageReference" src tests -g "*.csproj"`
- `Test-Path docs\harness\AH-PILOT-01.md`

Sibling repo:

- `Get-Content` for `WorkStartPilotService.cs`
- `Get-Content` for `PilotScenarioConfig.cs`
- `Get-Content` for `WorkStartPilotResult.cs`
- `Get-Content` for `ProcessDataPayloadBuilder.cs`
- `Get-Content` for `LotDataQueryService.cs`
- `Get-Content` for `ProcessDataPayloadBuilderTests.cs`
- `rg -n -C 35 "btnRunPilotOnce_Click|btnRunPilotOnce" ...\MainForm.cs`
- `Get-Content` for payload-related model files
- `rg -n "TryWriteErrorCodeAsync|Fail\(|ErrorCodeWriteVariable|AckWriteVariable|WriteStartVariable|LotReadStartVariable|StartSignalWordIndex|LotId" ...`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short`

## 16. git diff --check 결과

실행:

- `git diff --check`

결과:

- exit code 0
- whitespace error 없음
- 출력 없음

## 17. git status --short 결과

실행:

- `git status --short`

결과:

```text
?? docs/harness/AH-PILOT-01.md
```

주의:

- `git diff -- docs/harness/AH-PILOT-01.md`는 파일이 untracked 상태라 출력이 없었다.
- `Get-Content docs\harness\AH-PILOT-01.md`로 문서 내용 확인을 완료했다.

## 18. Harness / Boundary / Validation 영향

Harness 영향:

- AH-PILOT-02에서 payload packing tests를 CAAutomationHub 기준으로 재구성할 수 있는 근거를 남겼다.
- 기존 XgtChannelRunner tests는 직접 복사 대상이 아니라 scenario / assertion anchor로 사용한다.

Boundary 영향:

- Runtime core는 vendor-neutral 유지.
- FlowDefinitions는 candidate / validator interface boundary 유지.
- Pilot business flow는 Runtime polling state path와 분리.
- XGT operation, DB query, payload builder, ACK/error writer 책임을 분리.

Validation 영향:

- 이번 단계는 문서 review이므로 tests/build는 생략 가능하다.
- verification evidence는 requested diff/status/content check로 제한한다.

## 19. Self-Check

판정: `ACCEPT`

이유:

- AH-PILOT-01 Boundary Review 결과를 `docs/harness/AH-PILOT-01.md`에 기록했다.
- 직접 이식 금지 대상과 이유를 기록했다.
- 빠르게 이식 가능한 후보를 분류했다.
- XGT operation adapter seam, DB query abstraction, Payload Builder 분리를 기록했다.
- 후보 A/B/C/D/E를 검토하고 AH-PILOT-02 권장 1순위를 제안했다.
- 배치 위치 후보와 AH-PILOT / AH-RUNTIME 관계를 정리했다.
- production code, test code, project reference, source copy, FLOW.JSON, parser, executor, DB query, Payload Builder 이식, commit을 수행하지 않았다.
- requested validation commands를 실행했다.
- 문서 작성만 수행했으므로 tests/build는 실행하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
