# AH-PILOT-10 Closeout - XGT Adapter Project Boundary Review

## 1. Summary

AH-PILOT-10은 실제 XGT Adapter 구현 전에 `XgtDriverCore`를 어디에서 참조하고, WorkStart `IWorkStartPlcOperations` 구현을 어떤 project / namespace / test harness 경계에 둘지 검토한 Boundary Review다.

검토 결과, `CAAutomationHub.PilotFlows`는 계속 business flow / helper / interface seam 계층으로 유지하고 `XgtDriverCore`를 직접 참조하지 않는 방향이 안전하다. XGT-specific code는 별도 adapter project로 격리하고, 그 project가 `CAAutomationHub.PilotFlows`의 `IWorkStartPlcOperations`를 구현하면서 `XgtDriverCore` response / exception / diagnostics를 AH-PILOT-09에서 정한 `Success` / `OperationFailed` / `ParseFailed` 3-state로 낮추는 구조가 적절하다.

권장 project 이름은 초기 범위를 WorkStart/PilotFlows에 맞춘 `CAAutomationHub.PilotFlows.Xgt`다. 장기적으로 WorkStart 외 flow까지 확장할 가능성이 커지면 `CAAutomationHub.Adapters.Xgt`로 넓히는 후보가 있다. `CAAutomationHub.Runtime`과 `CAAutomationHub.FlowDefinitions`는 `XgtDriverCore`, `FakePlc`, `XgtChannelRunner`를 직접 참조하지 않는다.

Sibling repo `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`는 현재 dirty 상태다. 따라서 AH-PILOT-10에서는 실제 ProjectReference / subtree / source copy / package 선택을 확정하지 않고, clean commit anchor 확보 전 실제 reference 추가를 보류하는 판단을 남긴다.

이번 작업은 read-only 조사와 closeout 문서 작성만 수행했다. production code, test code, solution, csproj, project reference, package reference, XGT adapter skeleton, FakePlc test project, actual PLC read/write, DB concrete, FLOW.JSON, Flow Executor, commit은 수행하지 않았다. ContextPublisher automatic publish도 재도입하지 않았다.

## 2. 현재 PilotFlows / Runtime / FlowDefinitions project boundary

현재 `CAAutomationHub.sln`에는 다음 관련 project가 포함되어 있다.

- `src\CAAutomationHub.PilotFlows\CAAutomationHub.PilotFlows.csproj`
- `src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj`
- `src\CAAutomationHub.FlowDefinitions\CAAutomationHub.FlowDefinitions.csproj`
- `tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- `tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`

현재 reference 상태:

- `CAAutomationHub.PilotFlows`는 project reference가 없다.
- `CAAutomationHub.Runtime`은 `CAAutomationHub.Contracts`만 참조한다.
- `CAAutomationHub.FlowDefinitions`는 `CAAutomationHub.Contracts`만 참조한다.
- `CAAutomationHub.PilotFlows.Tests`는 test packages와 `CAAutomationHub.PilotFlows`만 참조한다.
- `CAAutomationHub.Runtime.Tests`는 `CAAutomationHub.Runtime`과 `CAAutomationHub.FlowDefinitions`를 참조한다.

`tests\CAAutomationHub.Runtime.Tests\RuntimeProjectReferenceBoundaryTests.cs`는 Runtime project가 `Contracts`만 참조하는지 확인하고, `CAAutomationHub.Wpf`, `XgtDriverCore`, `XgtChannelRunner`, `FakePlc` 문자열 참조를 금지한다.

추가로 `PollingPublishCoordinatorTests`에도 `XgtDriverCore`, `XgtChannelRunner`, `FakePlc` 금지 참조 검증이 존재한다.

판단:

- Runtime vendor-neutral boundary는 이미 test로 잠겨 있다.
- FlowDefinitions도 `Contracts` 외 참조가 없으므로 FLOW.JSON / definition layer에 XGT-specific dependency를 넣지 않는 현재 방향과 맞다.
- PilotFlows는 현재 XGT-free 상태이며 WorkStart business flow model, helper, interface seam만 가진다.
- AH-PILOT-10 이후에도 이 상태를 유지하는 것이 가장 안전하다.

## 3. CAAutomationHub.PilotFlows 직접 XgtDriverCore reference 후보 검토

후보:

```text
CAAutomationHub.PilotFlows
    -> AutomationHub.XgtDriverCore
```

장점:

- 가장 빠르게 구현할 수 있다.
- `WorkStartFlowService`와 `IWorkStartPlcOperations` 구현을 같은 project 안에서 다룰 수 있다.
- 별도 project 생성과 solution wiring이 줄어든다.

위험:

- `PilotFlows`가 business flow / helper / interface seam이라는 경계가 흐려진다.
- `XgtResponseClassification`, `XgtRawResponseInfo`, `TransportFailureKind`, raw frame, XGT address 같은 driver detail이 PilotFlows public model로 유입될 압력이 생긴다.
- AH-PILOT-09에서 정한 "XGT enum / raw diagnostics를 PilotFlows public model에 넣지 않는다"는 mapping boundary가 약해질 수 있다.
- 다른 PLC vendor 또는 다른 adapter를 붙일 때 `PilotFlows`가 특정 vendor에 묶인다.
- test harness에서 FakePlc나 transport scenario까지 PilotFlows test project에 끌려올 수 있다.

판정:

- 비권장.
- `CAAutomationHub.PilotFlows`는 XGT를 모르는 상태로 유지한다.
- `IWorkStartPlcOperations`는 PilotFlows가 소유하되, 그 concrete implementation은 adapter project가 소유한다.

## 4. 별도 XGT Adapter project 후보 검토

### 후보 A: `CAAutomationHub.PilotFlows.Xgt`

의미:

- PilotFlows interface를 구현하는 XGT adapter project.
- 초기 대상이 WorkStart pilot flow임을 이름에 드러낸다.

예상 reference:

```text
CAAutomationHub.PilotFlows.Xgt
    -> CAAutomationHub.PilotFlows
    -> AutomationHub.XgtDriverCore
```

장점:

- 현재 AH-PILOT-02~09의 흐름과 가장 직접적으로 연결된다.
- `IWorkStartPlcOperations` 구현 위치가 명확하다.
- `WorkStartReadBlockOperationResult` 3-state mapping을 adapter 내부에 둘 수 있다.
- Runtime / FlowDefinitions를 건드리지 않는다.

위험:

- 이름상 WorkStart/PilotFlows 전용 adapter로 보일 수 있다.
- 향후 전체 XGT adapter로 확장하면 project rename 또는 새 project 분리가 필요할 수 있다.

판정:

- AH-PILOT-11의 구현 후보로 가장 현실적이다.
- 초기 범위를 read adapter skeleton으로 제한한다면 이 이름이 안전하다.

### 후보 B: `CAAutomationHub.Adapters.Xgt`

의미:

- WorkStart를 넘어 향후 전체 XGT adapter 계층을 담는 project.

예상 reference:

```text
CAAutomationHub.Adapters.Xgt
    -> CAAutomationHub.PilotFlows
    -> AutomationHub.XgtDriverCore
```

장점:

- 장기 vendor adapter project로 명확하다.
- WorkStart 외 다른 flow / operation을 수용하기 좋다.
- `Runtime` core 밖에 XGT-specific implementation을 격리하기 좋다.

위험:

- 현재 PilotFlows 전환 단계에는 이름과 범위가 넓다.
- Runtime polling adapter와 Pilot business flow adapter가 한 project에 섞일 수 있다.
- `Runtime`과 연결해야 할 것처럼 오해될 수 있다.

판정:

- 장기 후보로 좋다.
- 단, AH-PILOT-11에서 read skeleton만 구현한다면 범위가 넓을 수 있다.

### 후보 C: `CAAutomationHub.WorkStart.XgtAdapter`

판정:

- WorkStart 전용이라는 의미는 가장 명확하다.
- 하지만 project 이름이 product namespace와 다소 분리되고, PilotFlows와의 관계가 덜 자연스럽다.
- 장기적으로 WorkStart 외 flow가 생기면 확장성이 낮다.

### 후보 D: `CAAutomationHub.XgtAdapter` / `CAAutomationHub.Pilot.XgtAdapter`

판정:

- 짧지만 project 책임이 모호하다.
- Runtime polling adapter인지, Pilot business flow adapter인지, general XGT adapter인지 구분이 약하다.

권장:

- AH-PILOT-11에서 실제 구현으로 간다면 `CAAutomationHub.PilotFlows.Xgt`를 1차 후보로 둔다.
- 장기 전체 adapter boundary를 먼저 확정하려면 `CAAutomationHub.Adapters.Xgt`를 별도 review 후보로 둔다.

## 5. sibling repo reference 후보 검토

Sibling repo:

- Path: `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- Latest commit: `fa0ab4f Merge pull request #119 from SanAndBom/codex/add-uint32-and-int32-display-formats`
- Current status: dirty

현재 dirty files:

```text
 M tools/AutomationHub.XgtDriverCore.FakePlc/appsettings/fakeplc.map.json
?? context-events/pending/evt_20260507_234052_receiveframeasync-whenresponsearrivesinp.json
?? context-events/pending/evt_20260507_234052_tcptransport-basic-request-response.json
?? context-events/pending/evt_20260507_234052_tcptransport-timeout-handling.json
?? context-events/pending/evt_20260507_234052_xgtsession-basic-exchange.json
?? context-events/pending/evt_20260516_005654_iwritableruntimeplcchannel-runtime-state.json
```

확인한 project:

- `src\AutomationHub.XgtDriverCore\AutomationHub.XgtDriverCore.csproj`
  - `AssemblyName`: `AutomationHub.XgtDriverCore`
  - `RootNamespace`: `AutomationHub.XgtDriverCore`
  - target framework는 `Directory.Build.props`의 `net10.0`
- `tools\AutomationHub.XgtDriverCore.FakePlc\AutomationHub.XgtDriverCore.FakePlc.csproj`
  - `AutomationHub.XgtDriverCore` project reference
  - appsettings fakeplc map 포함
- `XgtChannelRunner\XgtChannelRunner.csproj`
  - `net10.0-windows`
  - WinForms
  - `AutomationHub.XgtDriverCore` project reference
  - `ScottPlot.WinForms`, `Microsoft.Data.SqlClient`

확인한 XgtDriverCore API:

- `IXgtSession`
  - `ConnectAsync`
  - `DisconnectAsync`
  - `ReadAsync`
  - `WriteAsync`
  - `ReadStatusAsync`
  - `ExchangeRawAsync`
- `XgtFrameBuilder`
- `XgtFrameParser`
- `XgtRawResponseClassifier`
- `XgtRawResponseInfo`
- `XgtResponseClassification`
- `TransportException`
- `TransportFailureKind`

판단:

- Adapter project가 실제 XGT 통신 구현을 시작할 때 참조해야 할 최소 production dependency는 `AutomationHub.XgtDriverCore`다.
- `XgtChannelRunner`는 WinForms / SQL / runner orchestration dependency가 있어 직접 참조하지 않는다.
- `FakePlc`는 production adapter project가 아니라 integration test / harness project에서만 참조한다.
- sibling repo dirty 상태가 있으므로 AH-PILOT-10에서는 실제 reference 추가를 확정하지 않는다.

## 6. subtree / source copy / package 후보 검토

### 후보 A: sibling ProjectReference

개념:

```text
src\CAAutomationHub.PilotFlows.Xgt
    -> ..\..\..\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\AutomationHub.XgtDriverCore.csproj
```

장점:

- 가장 빠르다.
- 기존 driver core를 그대로 사용한다.
- local pilot 통신 테스트로 가기 쉽다.

위험:

- local path coupling이 생긴다.
- `CAAutomationHub` 단일 clone만으로 빌드할 수 없다.
- CI / 다른 PC / Codex 환경 재현성이 약하다.
- dirty sibling repo 영향이 build evidence를 흐릴 수 있다.

판정:

- clean anchor 확보 후 단기 local pilot에는 제한적으로 가능하다.
- dirty 상태에서는 실제 reference 추가 금지.

### 후보 B: git subtree

장점:

- 단일 repo clone 재현성이 좋다.
- 사용자 선호인 "CAAutomationHub 안에 녹이기"와 맞을 수 있다.
- CI / Codex 접근성이 좋아진다.

위험:

- upstream sync 부담이 있다.
- repository 관리와 history 병합이 복잡해질 수 있다.
- XgtDriverCore가 독립적으로 계속 진화하면 관리 비용이 커진다.

판정:

- 장기적으로 유력한 후보.
- AH-PILOT-10에서는 수행하지 않는다.
- subtree를 선택하려면 별도 retrieval step에서 clean commit anchor와 sync 정책을 먼저 정해야 한다.

### 후보 C: source copy

장점:

- 빠르고 단일 repo가 된다.
- 일부 pure helper 재구성에는 유용할 수 있다.

위험:

- 검증된 driver core와 drift가 생긴다.
- source history 추적이 어렵다.
- XGT protocol / transport core를 잘못 복사하면 driver 신뢰성이 깨질 수 있다.
- PilotFlows 또는 Runtime core로 XGT detail이 유입될 위험이 크다.

판정:

- `XgtDriverCore` core source copy는 비권장.
- payload packing rule 같은 pure business helper는 이미 `CAAutomationHub.PilotFlows`에 재구성되어 있으므로 driver source copy 필요성은 낮다.

### 후보 D: NuGet / package

장점:

- version pinning이 명확하다.
- 장기 dependency 관리가 깔끔하다.
- CI 재현성이 좋다.

위험:

- package pipeline / publish / versioning이 필요하다.
- 지금 pilot 속도에는 무겁다.
- local debugging이 번거로울 수 있다.

판정:

- 장기 표준화 후보.
- AH-PILOT-11 직전 단계로는 과하다.

## 7. FakePlc integration test 위치 검토

확인한 FakePlc 관련 위치:

- `tools\AutomationHub.XgtDriverCore.FakePlc\AutomationHub.XgtDriverCore.FakePlc.csproj`
- `tests\AutomationHub.XgtChannelRunner.Tests\TestDoubles\FakePlcScenarioServer.cs`

`FakePlcScenarioServer`가 다루는 response mode:

- `Ack`
- `Nak`
- `Malformed`
- `ForcedClose`
- `Timeout`
- `NoResponse`
- `SplitResponse`

판단:

- FakePlc는 production dependency가 아니다.
- production adapter project는 FakePlc를 참조하지 않는다.
- FakePlc는 별도 integration test project에서만 참조한다.
- 후보 test project 이름은 `tests\CAAutomationHub.PilotFlows.Xgt.Tests`다.

주의:

- `FakePlcScenarioServer`는 현재 sibling test project 내부 test double이다. 바로 source copy하거나 참조하려면 별도 검토가 필요하다.
- `tools\AutomationHub.XgtDriverCore.FakePlc`는 실행형 FakePlc tool이며, integration harness에서 외부 process로 사용할지 project reference로 사용할지 후속 review가 필요하다.

권장:

- AH-PILOT-11에서 production adapter skeleton만 만든다면 FakePlc test project는 만들지 않는다.
- FakePlc integration은 AH-PILOT-12 또는 별도 `FakePlc Integration Harness Boundary Review` 후 진행한다.

## 8. WorkStart XGT Read Adapter Skeleton 범위 검토

AH-PILOT-11에서 구현한다면 최소 안전 범위는 read adapter skeleton이다.

권장 최소 범위:

- `CAAutomationHub.PilotFlows.Xgt` project 생성
- `CAAutomationHub.PilotFlows` reference
- `AutomationHub.XgtDriverCore` reference는 clean anchor / retrieval decision 후에만 추가
- `IWorkStartPlcOperations` 구현 class skeleton
- read block operation만 우선 구현 또는 구현 준비
- XGT read response를 `WorkStartReadBlockOperationResult`로 낮추는 mapping

보류 범위:

- `WriteProcessPayloadAsync`
- `WriteStartAckAsync`
- `WriteErrorCodeBestEffortAsync`
- full WorkStart flow actual PLC execution
- real PLC write / ACK / error write
- FakePlc integration test project
- DB concrete

판단:

- 빠른 실제 통신 가능성을 보려면 read adapter부터 붙이는 것이 좋다.
- full WorkStart flow를 위해 write/ACK/error까지 한 번에 구현하면 boundary와 validation 범위가 너무 넓어진다.
- AH-PILOT-09에서 이미 read mapping boundary가 닫혔으므로 read adapter skeleton은 자연스러운 다음 구현 후보가 될 수 있다.
- 다만 sibling reference가 dirty 상태이므로 AH-PILOT-11은 implementation보다 retrieval/reference plan이 먼저일 수도 있다.

## 9. DB Query concrete와 순서 관계

현재 `WorkStartFlowService`는 `IWorkStartDataQuery` fake를 통해 happy path / representative failure path를 검증할 수 있다.

판단:

- XGT read adapter는 DB concrete 없이도 검증할 수 있다.
- read block, start signal, LOT ID extraction / selection은 DB 없이 확인 가능하다.
- 실제 PLC read-only test 또는 FakePlc read scenario가 DB concrete보다 먼저 갈 수 있다.
- full WorkStart flow는 DB concrete 또는 fake DB query와 write/ACK/error adapter가 필요하다.

권장 순서:

1. XGT adapter project boundary 확정
2. WorkStart XGT read adapter skeleton 또는 reference retrieval plan
3. read-only FakePlc / real PLC harness
4. write / ACK / error operation
5. DB concrete
6. full WorkStart flow wiring

## 10. 권장안

권장 구조:

```text
CAAutomationHub.PilotFlows
    - WorkStartFlowService
    - IWorkStartPlcOperations
    - IWorkStartDataQuery
    - WorkStartReadBlockOperationResult
    - WorkStart business helpers
    - XGT-free

CAAutomationHub.PilotFlows.Xgt
    - CAAutomationHub.PilotFlows reference
    - AutomationHub.XgtDriverCore reference only after clean retrieval decision
    - IWorkStartPlcOperations implementation
    - XGT response / exception -> WorkStartReadBlockOperationResult mapping
    - raw XGT diagnostics kept inside adapter/logging boundary

tests/CAAutomationHub.PilotFlows.Xgt.Tests
    - adapter mapping tests
    - FakePlc integration tests only when harness boundary is decided
    - FakePlc dependency test-only

CAAutomationHub.Runtime
    - no XgtDriverCore reference
    - no FakePlc reference
    - no XgtChannelRunner reference

CAAutomationHub.FlowDefinitions
    - no XgtDriverCore reference
    - no FakePlc reference
    - no XgtChannelRunner reference
```

핵심 판정:

- `CAAutomationHub.PilotFlows` 직접 `XgtDriverCore` reference는 비권장.
- 별도 adapter project를 둔다.
- adapter project 1차 이름은 `CAAutomationHub.PilotFlows.Xgt`.
- 장기 general adapter가 필요하면 `CAAutomationHub.Adapters.Xgt`를 후속 후보로 재검토한다.
- `XgtChannelRunner`는 직접 reference하지 않는다.
- `WorkStartPilotService`는 source copy하지 않는다.
- FakePlc는 integration test / harness dependency로만 둔다.
- sibling repo dirty state가 있으므로 실제 reference 방식은 clean anchor 확보 후 확정한다.

## 11. AH-PILOT-11 후보

### 후보 1: AH-PILOT-11 XGT Retrieval / Reference Plan

권장 조건:

- sibling repo dirty state가 계속 남아 있을 때
- ProjectReference / subtree / package 중 실제 선택을 먼저 닫아야 할 때

범위:

- sibling repo clean anchor 확인
- local ProjectReference / subtree / package 중 1차 선택
- CI / 다른 PC 재현성 판단
- code 구현 없음

판정:

- 현재 sibling repo dirty 상태를 고려하면 가장 보수적인 다음 단계다.

### 후보 2: AH-PILOT-11 XGT Adapter Project Skeleton

권장 조건:

- reference 없이 project boundary만 먼저 세울 때

범위:

- project 생성
- namespace / reference 방향 고정
- class skeleton only
- actual XGT read 구현 없음

판정:

- project boundary를 코드 구조로 고정하는 단계로 가능하다.
- 다만 사용자가 이번 단계에서 아직 implementation을 원하지 않는다면 retrieval plan 이후로 미룬다.

### 후보 3: AH-PILOT-11 WorkStart XGT Read Adapter Skeleton

권장 조건:

- `XgtDriverCore` clean anchor 또는 acceptable local reference decision이 확보됐을 때

범위:

- `IWorkStartPlcOperations.ReadWorkStartBlockAsync` 중심
- XGT read ACK / NAK / malformed / transport failure mapping
- `Success` / `OperationFailed` / `ParseFailed` 검증
- write/ACK/error는 NotImplemented 또는 후속 단계로 보류

판정:

- 실제 통신 검증을 빠르게 앞당기는 가장 실용적인 구현 후보.
- 단, reference boundary 미확정이면 아직 이르다.

### 후보 4: AH-PILOT-11 FakePlc Integration Harness Boundary Review

권장 조건:

- adapter 구현 전 test harness 위치를 먼저 확정하고 싶을 때

범위:

- `tests\CAAutomationHub.PilotFlows.Xgt.Tests` 후보 검토
- `FakePlcScenarioServer` vs FakePlc tool 사용 방식 검토
- test-only dependency boundary 확정

판정:

- XGT read adapter skeleton 직전 또는 직후에 유용하다.

최종 추천:

- sibling repo dirty state를 먼저 닫지 않는다면 후보 1.
- clean anchor가 확보되면 후보 3.
- reference 없이 in-repo structure부터 고정하려면 후보 2.

## 12. 제외한 범위

이번 AH-PILOT-10에서 의도적으로 제외한 범위:

- production code 수정
- test code 수정
- project / solution / csproj 수정
- `ProjectReference` 추가
- `PackageReference` 추가
- `XgtDriverCore` reference 추가
- `FakePlc` reference 추가
- `XgtChannelRunner` reference 추가
- adapter skeleton 추가
- actual PLC read/write 구현
- WorkStart write / ACK / error concrete 구현
- DB concrete / SQL / `SqlClient` 구현
- FLOW.JSON 연결
- Flow Executor 구현
- Runtime project 수정
- FlowDefinitions project 수정
- PilotFlows project 수정
- `ChannelPollingTarget` / `ChannelPollingResult` 수정
- `RuntimeSnapshot` 참조
- `WorkStartPilotService` source copy
- source copy / subtree / submodule 작업
- commit
- ContextPublisher automatic publish 재도입

## 13. 실행한 명령

현재 repo:

- `git log --oneline -10`
- `git status --short`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content CAAutomationHub.sln`
- `Get-Content src\CAAutomationHub.PilotFlows\CAAutomationHub.PilotFlows.csproj`
- `Get-Content src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj`
- `Get-Content src\CAAutomationHub.FlowDefinitions\CAAutomationHub.FlowDefinitions.csproj`
- `Get-Content tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- `Get-Content tests\CAAutomationHub.Runtime.Tests\RuntimeProjectReferenceBoundaryTests.cs`
- `Get-Content docs\harness\AH-PILOT-09.md`
- `Get-Content docs\harness\AH-PILOT-08.md`
- `Get-Content docs\harness\AH-PILOT-07.md`
- `Get-Content docs\harness\AH-RUNTIME-42.md`
- `Get-Content docs\harness\AH-RUNTIME-43.md`
- `Get-Content docs\harness\AH-RUNTIME-44.md`
- `rg --files src\CAAutomationHub.PilotFlows\WorkStart tests\CAAutomationHub.PilotFlows.Tests\WorkStart`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\IWorkStartPlcOperations.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartReadBlockOperationResult.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartReadBlockOperationStatus.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartFlowService.cs`
- `rg -n "XgtDriverCore|XgtChannelRunner|FakePlc|RuntimeSnapshot|ChannelPollingTarget|ChannelPollingResult|Microsoft.Data.SqlClient|FLOW\.JSON|FlowExecutor" src\CAAutomationHub.PilotFlows tests\CAAutomationHub.PilotFlows.Tests`
- `rg -n "XgtDriverCore|XgtChannelRunner|FakePlc" src\CAAutomationHub.Runtime src\CAAutomationHub.FlowDefinitions tests\CAAutomationHub.Runtime.Tests -g "*.cs" -g "*.csproj"`
- `rg -n "ProjectReference|PackageReference" src tests -g "*.csproj"`
- `rg -n "XgtDriverCore|XgtChannelRunner|FakePlc|RuntimeSnapshot|ChannelPollingTarget|ChannelPollingResult|Microsoft.Data.SqlClient|FLOW\.JSON|Flow Executor" docs\harness\AH-PILOT-07.md docs\harness\AH-PILOT-08.md docs\harness\AH-PILOT-09.md`

Sibling repo:

- `git status --short`
- `git log --oneline -5`
- `rg --files -g "*.csproj"`
- `Get-Content Directory.Build.props`
- `Get-Content src\AutomationHub.XgtDriverCore\AutomationHub.XgtDriverCore.csproj`
- `Get-Content tools\AutomationHub.XgtDriverCore.FakePlc\AutomationHub.XgtDriverCore.FakePlc.csproj`
- `Get-Content XgtChannelRunner\XgtChannelRunner.csproj`
- `rg -n "interface IXgtSession|class XgtFrameBuilder|class XgtFrameParser|class XgtRawResponseClassifier|record XgtRawResponseInfo|class XgtRawResponseInfo|enum XgtResponseClassification|class TransportException|enum TransportFailureKind|class WorkStartPilotService|ProcessDataPayloadBuilder"`
- `rg --files src/AutomationHub.XgtDriverCore XgtChannelRunner tests/AutomationHub.XgtChannelRunner.Tests tools/AutomationHub.XgtDriverCore.FakePlc | rg "(IXgtSession|XgtFrameBuilder|XgtFrameParser|XgtRawResponseClassifier|XgtRawResponseInfo|XgtResponseClassification|TransportException|TransportFailureKind|WorkStartPilotService|ProcessDataPayloadBuilder|FakePlcScenarioServer)"`
- `Get-Content src\AutomationHub.XgtDriverCore\Client\IXgtSession.cs`
- `Get-Content src\AutomationHub.XgtDriverCore\Protocol\XgtFrameBuilder.cs`
- `Get-Content src\AutomationHub.XgtDriverCore\Protocol\XgtFrameParser.cs`
- `Get-Content src\AutomationHub.XgtDriverCore\Protocol\XgtRawResponseClassifier.cs`
- `Get-Content src\AutomationHub.XgtDriverCore\Protocol\XgtRawResponseInfo.cs`
- `Get-Content src\AutomationHub.XgtDriverCore\Protocol\XgtResponseClassification.cs`
- `Get-Content src\AutomationHub.XgtDriverCore\Validation\TransportException.cs`
- `Get-Content src\AutomationHub.XgtDriverCore\Validation\TransportFailureKind.cs`
- `Get-Content XgtChannelRunner\Services\WorkStartPilotService.cs`
- `Get-Content XgtChannelRunner\Services\ProcessDataPayloadBuilder.cs`
- `Get-Content tests\AutomationHub.XgtChannelRunner.Tests\Services\ProcessDataPayloadBuilderTests.cs`
- `Get-Content tests\AutomationHub.XgtChannelRunner.Tests\TestDoubles\FakePlcScenarioServer.cs`

Validation:

- `git diff -- docs/harness/AH-PILOT-10.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-10.md`

테스트 / 빌드:

- 문서 작성만 수행했으므로 실행하지 않았다.

## 14. git diff --check 결과

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

- `docs/harness/AH-PILOT-10.md`는 신규 untracked 파일이다.

## 15. git status --short 결과

실행:

```text
git status --short
```

결과:

```text
?? docs/harness/AH-PILOT-10.md
```

## 16. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-10 목표인 XGT Adapter project boundary를 closeout 문서로 남겼다.
- 현재 `PilotFlows` / `Runtime` / `FlowDefinitions` reference 상태를 확인했다.
- Runtime의 XGT / FakePlc / XgtChannelRunner 금지 reference test를 확인했다.
- `CAAutomationHub.PilotFlows` 직접 `XgtDriverCore` reference 후보를 검토하고 비권장으로 판정했다.
- 별도 adapter project 후보를 비교하고 `CAAutomationHub.PilotFlows.Xgt`를 AH-PILOT-11 1차 구현 후보로 정리했다.
- sibling repo current status와 dirty files를 확인했다.
- `AutomationHub.XgtDriverCore` / FakePlc / XgtChannelRunner project file을 확인했다.
- `IXgtSession`, frame builder/parser, response classifier, transport exception, WorkStart anchor, payload tests, FakePlc scenario server를 확인했다.
- sibling ProjectReference / subtree / source copy / package 후보를 비교했다.
- FakePlc는 production dependency가 아니라 integration test / harness dependency로 정리했다.
- AH-PILOT-11 후보를 retrieval plan, project skeleton, read adapter skeleton, FakePlc harness review로 나눠 제안했다.
- production code, test code, solution, csproj, reference, adapter skeleton, actual PLC read/write, DB concrete, FLOW.JSON, Flow Executor를 수정하거나 구현하지 않았다.
- ContextPublisher automatic publish를 재도입하지 않았다.
- requested validation commands를 실행했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
