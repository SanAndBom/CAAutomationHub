# AH-PILOT-13-A Closeout - XGT Read Request Configuration Policy

## 1. Summary

AH-PILOT-13-A는 WorkStart XGT read adapter가 사용할 read request 설정값의 위치와 책임을 정리한 Boundary Review다.

검토 결과, `WorkStartReadBlockLayout`은 `byte[]` read block 내부의 해석 layout으로 유지하고, XGT read start variable / read word count는 `CAAutomationHub.PilotFlows.Xgt` 쪽 adapter-local options로 분리하는 방향이 가장 안전하다.

권장 이름은 `WorkStartXgtReadOptions`다. 이 타입은 `ReadStartVariable`과 `ReadWordCount`를 소유하고, AH-PILOT-13-B에서 `XgtReadRequest` 생성 또는 adapter constructor wiring으로 연결하는 후보가 된다. 기본값은 기존 pilot 기준인 `%DB10000`, `90` words를 제공할 수 있지만, 이는 현장별 binding으로 교체될 pilot default이며 Runtime core default가 아니다.

이번 작업은 read-only 조사와 closeout 문서 작성만 수행했다. production code, test code, interface, service, adapter, csproj, solution, project/package reference, FakePlc integration, actual PLC read/write, FLOW.JSON, Flow Executor, commit은 수행하지 않았다.

## 2. 현재 WorkStart read config 상태

현재 `CAAutomationHub.PilotFlows` 상태:

- `WorkStartReadBlockLayout`
  - `DefaultReadWordCount = 90`
  - `DefaultStartSignalWordIndex = 80`
  - `DefaultLotId1WordOffset = 0`
  - `DefaultLotId2WordOffset = 10`
  - `DefaultLotIdWordLength = 6`
- `WorkStartReadBlockInterpreter`
  - 전달받은 `byte[]` read block에서 start signal과 LOT ID를 해석한다.
  - XGT address 또는 `XgtReadRequest`를 알지 않는다.
- `WorkStartFlowOptions`
  - start signal index, LOT ID offset, LOT ID length를 flow option으로 받는다.
  - `ReadWordCount`는 직접 사용하지 않는다.

현재 `CAAutomationHub.PilotFlows.Xgt` 상태:

- `WorkStartXgtPlcOperations`
  - `IXgtSession`과 `XgtReadRequest`를 constructor로 받는다.
  - `ReadWorkStartBlockAsync`에서 `_session.ReadAsync(_readRequest, cancellationToken)`을 호출한다.
  - XGT response를 `WorkStartReadBlockOperationResult` 3-state로 낮춘다.
- 현재 adapter에는 `%DB10000` 또는 `90` words WorkStart default를 만드는 adapter-local config 타입이 없다.
- 현재 test는 `%MB100`, `continuousByteLength: 16` dummy request를 사용해 request forwarding과 mapping만 검증한다.

Sibling `XgtChannelRunner` 기준:

- `PilotScenarioConfig.LotReadStartVariable = "%DB10000"`
- `PilotScenarioConfig.LotReadWordCount = 90`
- `PilotScenarioConfig.StartSignalWordIndexInReadBlock = 80`
- `PilotScenarioConfig.LotId1WordOffset = 0`
- `PilotScenarioConfig.LotId2WordOffset = 10`
- `PilotScenarioConfig.LotIdWordLength = 6`
- `WorkStartPilotService.BuildContinuousReadRequest(...)`는 word count를 byte length로 변환해 continuous read frame을 만든다.

Sibling `XgtDriverCore` 기준:

- `IXgtSession.ReadAsync(XgtReadRequest request, CancellationToken cancellationToken)`가 public read seam이다.
- `XgtReadRequest` continuous mode는 byte variable address를 요구한다.
- `%DB10000` 같은 XGT variable name과 continuous byte length는 `XgtReadRequest` 생성 시점의 책임이다.

## 3. WorkStartReadBlockLayout과 XGT read request config 분리 판단

판정: 분리한다.

`WorkStartReadBlockLayout`의 책임:

- WorkStart read block 내부의 의미 위치를 표현한다.
- start signal word index, LOT ID word offset, LOT ID word length를 소유한다.
- `byte[]` 안에서 어디를 읽을지 결정한다.
- PilotFlows core에 남길 수 있다.

XGT read request config의 책임:

- 어떤 PLC variable에서 몇 word를 read할지 표현한다.
- `%DB10000` 같은 XGT-specific address를 소유한다.
- XGT continuous read의 byte length 계산 근거가 된다.
- `CAAutomationHub.PilotFlows.Xgt` adapter boundary 안에 둔다.

둘을 하나로 합치면 생기는 문제:

- `CAAutomationHub.PilotFlows` core에 `%DB10000` 같은 XGT address가 들어갈 압력이 생긴다.
- PilotFlows가 business flow / layout seam이 아니라 vendor request config를 소유하게 된다.
- 향후 다른 PLC vendor 또는 FLOW.JSON binding으로 갈 때 core model이 XGT에 묶인다.

둘을 분리했을 때의 장점:

- config 객체가 하나 늘어나지만 boundary가 선명하다.
- PilotFlows core는 XGT-free 상태를 유지한다.
- `CAAutomationHub.PilotFlows.Xgt`가 XGT request construction을 책임질 수 있다.
- 향후 FLOW.JSON / binding 값이 adapter-local options로 내려오는 구조를 만들기 쉽다.

관계:

- `WorkStartReadBlockLayout`은 read block을 해석하는 layout contract다.
- `WorkStartXgtReadOptions` 후보는 read block을 가져오기 위한 XGT request contract다.
- 두 값은 일관되어야 한다. 예를 들어 `ReadWordCount`는 start signal index와 LOT ID range를 포함할 만큼 충분해야 한다.
- 이 일관성 검증은 AH-PILOT-13-B 또는 AH-PILOT-14에서 options validation / harness validation 후보로 남긴다.

## 4. XGT read request config 위치 후보

### 후보 A: `WorkStartReadBlockLayout`에 XGT address / word count 추가

판정: 비권장.

이유:

- `%DB10000`은 XGT-specific request address다.
- `CAAutomationHub.PilotFlows` core가 XGT address를 알게 된다.
- WorkStart layout과 vendor request policy가 한 타입에 섞인다.

보정:

- 현재 `WorkStartReadBlockLayout.DefaultReadWordCount = 90`은 read block 크기 기준으로 이미 존재한다.
- 그러나 `%DB10000`을 같은 타입에 추가하지 않는다.
- AH-PILOT-13-B에서 `ReadWordCount`를 XGT options에도 둘 경우, 두 default의 관계를 문서화하거나 validation한다.

### 후보 B: `CAAutomationHub.PilotFlows.Xgt.WorkStart.WorkStartXgtReadOptions` 추가

판정: 권장.

예상 필드:

- `string ReadStartVariable`
- `int ReadWordCount`

예상 default:

- `ReadStartVariable = "%DB10000"`
- `ReadWordCount = 90`

이유:

- XGT-specific config가 adapter project 안에 머문다.
- `XgtDriverCore` reference가 이미 있는 `CAAutomationHub.PilotFlows.Xgt`에서 `XgtReadRequest`로 변환할 수 있다.
- PilotFlows core, Runtime, FlowDefinitions는 XGT address를 몰라도 된다.
- FLOW.JSON / binding이 생기면 binding value를 이 options로 내릴 수 있다.

### 후보 C: adapter constructor에 `string startVariable`, `int wordCount` 직접 전달

판정: 단기 가능하지만 비권장.

장점:

- 구현이 가장 간단하다.
- test에서 값 전달이 쉽다.

위험:

- 두 값의 의미가 흩어진다.
- default / validation / naming을 한곳에 묶기 어렵다.
- 향후 write options, connection options, binding options와 같이 확장될 때 constructor parameter가 흐려질 수 있다.

보정:

- AH-PILOT-13-B에서 overload로는 가능하지만, primary contract는 options 타입이 더 명확하다.

### 후보 D: 향후 FLOW.JSON binding 전까지 hard-code 유지

판정: 비권장.

이유:

- `%DB10000`, `90` words는 FakePlc / real PLC read 준비에 직접 영향을 주는 값이다.
- 지금 명명하지 않으면 AH-PILOT-14 integration harness에서 request 근거가 흐려진다.
- FLOW.JSON parser / binding을 아직 만들지 않는 상태에서도 adapter-local pilot default는 필요하다.

## 5. Default 값 처리 판단

판정: pilot default는 제공할 수 있다. 단, 현장별 binding으로 교체될 값임을 문서화한다.

권장 default:

- `WorkStartXgtReadOptions.Default.ReadStartVariable = "%DB10000"`
- `WorkStartXgtReadOptions.Default.ReadWordCount = 90`
- `WorkStartReadBlockLayout.DefaultStartSignalWordIndex = 80`
- `WorkStartReadBlockLayout.DefaultLotId1WordOffset = 0`
- `WorkStartReadBlockLayout.DefaultLotId2WordOffset = 10`
- `WorkStartReadBlockLayout.DefaultLotIdWordLength = 6`

주의:

- default는 Runtime core로 올리지 않는다.
- default는 FlowDefinitions로 올리지 않는다.
- default는 실제 현장/PLC별 binding이 생기면 교체 가능한 pilot baseline이다.
- `ReadWordCount = 90`은 XGT request byte length `180`으로 변환되어야 한다.
- `XgtReadRequest` continuous mode는 `%DB` byte variable과 `continuousByteLength`를 요구하므로, options 또는 factory가 word count를 byte length로 변환해야 한다.

## 6. FakePlc / XGT read integration 준비성

판정: AH-PILOT-13-A policy 기준으로 AH-PILOT-14 FakePlc / XGT read integration으로 넘어갈 수 있다.

근거:

- `CAAutomationHub.PilotFlows.Xgt` adapter project가 이미 존재한다.
- `WorkStartXgtPlcOperations`는 `IXgtSession.ReadAsync(XgtReadRequest, CancellationToken)`를 사용한다.
- `WorkStartReadBlockOperationResult` 3-state mapping은 AH-PILOT-12에서 test로 고정되어 있다.
- FakePlc memory map에는 `%DB10000` base block이 있고, 90 words / 180 bytes read 기준과 맞는 후보가 있다.
- FakePlc scenario initializer는 LOT ID 1을 D5000, LOT ID 2를 D5010, start signal을 D5083에 쓴다. `%DB10000` 기준으로는 각각 read block word offset 0, 10, 83에 해당한다.

주의:

- 현재 WorkStart layout default는 `DefaultStartSignalWordIndex = 80`이다.
- Sibling FakePlc initializer는 start signal D5083을 사용한다.
- 이 차이는 AH-PILOT-14 전에 반드시 확인해야 할 integration alignment risk다.
- 사용자 지시 기준의 기존 WorkStartPilotService 값은 start signal index 80이므로, FakePlc map 또는 initializer가 pilot 기준과 다르면 FakePlc harness 쪽 정렬 검토가 필요하다.
- Sibling repo는 현재 dirty 상태이며, dirty 범위에 FakePlc map이 포함된다. AH-PILOT-14에서 FakePlc integration으로 들어가기 전 clean/anchor 확인이 필요하다.
- FakePlc는 test-only dependency로 유지한다.

## 7. AH-PILOT-13-B 구현 후보

후보 1: `WorkStartXgtReadOptions` model 추가

- location: `src/CAAutomationHub.PilotFlows.Xgt/WorkStart`
- namespace: `CAAutomationHub.PilotFlows.Xgt.WorkStart`
- fields:
  - `string ReadStartVariable`
  - `int ReadWordCount`
- default:
  - `%DB10000`
  - `90`
- validation 후보:
  - address null/whitespace 금지
  - `ReadWordCount > 0`
  - continuous byte length overflow 방지
  - `ReadWordCount * 2 <= XgtReadRequest.MaxContinuousByteLength`

후보 2: `WorkStartXgtPlcOperations` constructor에 options 도입

- primary constructor 후보:
  - `WorkStartXgtPlcOperations(IXgtSession session, WorkStartXgtReadOptions options)`
- 내부에서 `XgtReadRequest`를 생성한다.
- 기존 `XgtReadRequest` direct constructor는 low-level seam 또는 test seam으로 유지할지 AH-PILOT-13-B에서 결정한다.

후보 3: request factory 분리

- internal helper 후보:
  - `WorkStartXgtReadRequestFactory.Create(WorkStartXgtReadOptions options)`
- `ReadWordCount`를 continuous byte length로 변환한다.
- options validation과 request construction을 adapter class에서 분리한다.

후보 4: `WorkStartReadBlockLayout` default 관계 문서화 / validation

- XGT options default `ReadWordCount = 90`과 layout default range의 관계를 test로 고정할 수 있다.
- 단, `WorkStartReadBlockLayout`에 XGT address를 추가하지 않는다.

후보 5: AH-PILOT-14 FakePlc integration으로 이동

- AH-PILOT-13-B에서 options boundary가 구현된 뒤 진행하는 것이 안전하다.
- 특히 FakePlc start signal offset alignment를 먼저 확인해야 한다.

## 8. 권장안

권장 policy:

1. `WorkStartReadBlockLayout`은 PilotFlows core에 유지하고 read block 해석 책임만 가진다.
2. `%DB10000`, `90` words는 `CAAutomationHub.PilotFlows.Xgt`의 `WorkStartXgtReadOptions` 후보가 소유한다.
3. `WorkStartXgtReadOptions.Default`는 기존 pilot 기준 `%DB10000`, `90` words를 제공할 수 있다.
4. default는 pilot baseline이며 현장별 FLOW.JSON / binding으로 교체될 수 있음을 문서화한다.
5. PilotFlows core, Runtime, FlowDefinitions는 XGT address를 소유하지 않는다.
6. AH-PILOT-13-B에서 options model + request factory 또는 adapter constructor 반영을 구현한다.
7. AH-PILOT-14에서 FakePlc / XGT read integration harness로 넘어가되, FakePlc start signal offset alignment와 sibling dirty state를 먼저 확인한다.

## 9. 제외한 범위

이번 AH-PILOT-13-A에서 제외한 범위:

- production code 수정
- test code 수정
- interface 수정
- service 수정
- adapter 수정
- csproj / solution 수정
- ProjectReference 추가
- PackageReference 추가
- FakePlc integration test 생성
- actual PLC read/write 테스트
- FLOW.JSON 파일 생성
- JSON schema / parser 생성
- Flow Executor 구현
- Runtime project 수정
- FlowDefinitions project 수정
- PilotFlows core에 XGT address 추가
- XgtChannelRunner reference 추가
- FakePlc reference 추가
- Microsoft.Data.SqlClient reference 추가
- actual DB query 구현
- RuntimeSnapshot / ChannelPollingResult 참조
- WorkStartPilotService source copy
- SQL connection string / SQL text 추가
- commit
- ContextPublisher automatic publish 재도입

## 10. 실행한 명령

현재 repo:

- `git log --oneline -8`
- `git status --short`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content docs\harness\AH-PILOT-12.md`
- `Get-Content docs\harness\AH-PILOT-11.md`
- `Get-Content docs\harness\AH-PILOT-10.md`
- `Get-Content docs\harness\AH-PILOT-09.md`
- `rg --files src\CAAutomationHub.PilotFlows\WorkStart src\CAAutomationHub.PilotFlows.Xgt tests\CAAutomationHub.PilotFlows.Tests\WorkStart tests\CAAutomationHub.PilotFlows.Xgt.Tests`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartReadBlockLayout.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartReadBlockInterpreter.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowOptions.cs`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtPlcOperations.cs`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtReadResultMapper.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart\WorkStartXgtPlcOperationsTests.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Tests\WorkStart\WorkStartReadBlockInterpreterTests.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Tests\WorkStart\WorkStartFlowServiceTests.cs`
- `Test-Path docs\harness\AH-PILOT-13-A.md`
- `rg -n "DefaultReadWordCount|WorkStartReadBlockLayout|XgtReadRequest|ReadWordCount|ReadStartVariable|%DB10000|DefaultStartSignal" src\CAAutomationHub.PilotFlows src\CAAutomationHub.PilotFlows.Xgt tests\CAAutomationHub.PilotFlows.Tests tests\CAAutomationHub.PilotFlows.Xgt.Tests`
- `rg -n "ProjectReference|PackageReference|XgtDriverCore|XgtChannelRunner|FakePlc|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FLOW\.JSON|FlowExecutor|Flow Executor" src tests -g "*.cs" -g "*.csproj"`

Sibling repo:

- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\PilotScenarioConfig.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\WorkStartPilotService.cs`
- `rg -n "record XgtReadRequest|class XgtReadRequest|struct XgtReadRequest|IXgtSession|ReadAsync\(" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore`
- `rg -n "ReadStart|Read.*Variable|Read.*Count|%DB10000|90|StartSignal|LotId|LOT" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtReadRequest.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtReadRequestBuilder.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Client\IXgtSession.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Client\XgtSession.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtVariableBlock.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtDataType.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcMemoryImage.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcScenarioInitializer.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\appsettings\fakeplc.map.json`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short`

Validation:

- `git diff -- docs/harness/AH-PILOT-13-A.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-13-A.md`

테스트 / 빌드:

- 문서 작성과 read-only Boundary Review만 수행했으므로 실행하지 않았다.

## 11. git diff --check 결과

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

- `docs/harness/AH-PILOT-13-A.md`는 신규 untracked 파일이므로 `git diff -- docs/harness/AH-PILOT-13-A.md`는 diff body를 출력하지 않는다.

## 12. git status --short 결과

실행:

```text
git status --short
```

결과:

```text
?? docs/harness/AH-PILOT-13-A.md
```

## 13. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-13-A 목표인 XGT read request configuration policy를 closeout 문서로 남겼다.
- `WorkStartReadBlockLayout`과 XGT read request config의 책임을 분리했다.
- `WorkStartReadBlockLayout`은 PilotFlows core의 byte[] 해석 layout으로 유지하는 판단을 남겼다.
- `%DB10000`, `90` words는 `CAAutomationHub.PilotFlows.Xgt`의 `WorkStartXgtReadOptions` 후보가 소유하는 권장안을 남겼다.
- default는 pilot baseline이며 현장별 binding으로 교체될 수 있음을 기록했다.
- AH-PILOT-13-B 구현 후보와 AH-PILOT-14 FakePlc / XGT read integration 준비성을 정리했다.
- FakePlc start signal offset alignment risk를 확인 필요로 남겼다.
- Runtime / FlowDefinitions / PilotFlows core에 XGT address를 넣지 않았다.
- production code, test code, interface, service, adapter, csproj, solution, reference, FakePlc integration, actual PLC read/write, FLOW.JSON, Flow Executor, commit을 수행하지 않았다.
- requested validation commands를 실행했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
