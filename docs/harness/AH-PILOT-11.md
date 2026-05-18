# AH-PILOT-11 Closeout - XGT Retrieval / Reference Plan Boundary Review

## 1. Summary

AH-PILOT-11은 실제 XGT Adapter 구현 전에 `XgtDriverCore`를 `CAAutomationHub`에서 어떻게 가져오고 참조할지 결정하기 위한 Retrieval / Reference Plan Boundary Review다.

검토 결과, `CAAutomationHub.PilotFlows` / `CAAutomationHub.Runtime` / `CAAutomationHub.FlowDefinitions`는 계속 `XgtDriverCore` / `FakePlc` / `XgtChannelRunner`를 직접 참조하지 않는 상태로 유지해야 한다. XGT-specific code는 별도 adapter project인 `CAAutomationHub.PilotFlows.Xgt` 후보에 격리하고, 그 project만 `CAAutomationHub.PilotFlows`의 `IWorkStartPlcOperations` seam을 구현하면서 `AutomationHub.XgtDriverCore`를 참조하는 방향이 가장 안전하다.

Sibling repo `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`는 여전히 dirty 상태다. 다만 현재 dirty 범위는 `tools/AutomationHub.XgtDriverCore.FakePlc/appsettings/fakeplc.map.json` 수정과 `context-events/pending` untracked files이며, reference 대상인 `src/AutomationHub.XgtDriverCore` source 및 `AutomationHub.XgtDriverCore.csproj`는 clean으로 확인됐다.

따라서 단기 local pilot에서는 clean commit anchor `fa0ab4f`를 명시하고, dirty 범위가 production driver core가 아니라는 조건을 문서화한 뒤 `CAAutomationHub.PilotFlows.Xgt -> sibling AutomationHub.XgtDriverCore` local `ProjectReference`를 제한적으로 허용할 수 있다. 그러나 repo 전체가 dirty이므로 장기/CI/다른 PC 재현성 기준에서는 clean anchor 확보 또는 subtree/package 전략이 여전히 필요하다.

이번 작업은 read-only 조사와 closeout 문서 작성만 수행했다. production code, test code, solution, csproj, project reference, package reference, subtree, source copy, adapter project, FakePlc test project, actual PLC read/write, commit은 수행하지 않았다. ContextPublisher automatic publish도 재도입하지 않았다.

## 2. 현재 CAAutomationHub project/reference 상태

현재 anchor:

- Latest commit: `dddc4b0 docs: close out AH-PILOT-10 xgt adapter boundary review`
- Working tree before closeout creation: clean

`CAAutomationHub.sln`에 포함된 project:

- `src\CAAutomationHub.Contracts\CAAutomationHub.Contracts.csproj`
- `src\CAAutomationHub.FlowDefinitions\CAAutomationHub.FlowDefinitions.csproj`
- `src\CAAutomationHub.PilotFlows\CAAutomationHub.PilotFlows.csproj`
- `src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj`
- `src\CAAutomationHub.Wpf\CAAutomationHub.Wpf.csproj`
- `tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- `tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
- `tests\CAAutomationHub.Wpf.Tests\CAAutomationHub.Wpf.Tests.csproj`

현재 reference 상태:

- `CAAutomationHub.PilotFlows`는 project/package reference가 없다.
- `CAAutomationHub.Runtime`은 `CAAutomationHub.Contracts`만 참조한다.
- `CAAutomationHub.FlowDefinitions`는 `CAAutomationHub.Contracts`만 참조한다.
- `CAAutomationHub.PilotFlows.Tests`는 test packages와 `CAAutomationHub.PilotFlows`만 참조한다.
- `tests\CAAutomationHub.Runtime.Tests\RuntimeProjectReferenceBoundaryTests.cs`는 Runtime project가 `Contracts`만 참조하는지 검증하고, `XgtDriverCore`, `XgtChannelRunner`, `FakePlc`, `CAAutomationHub.Wpf` 문자열 참조를 금지한다.

판단:

- 현재 CAAutomationHub 기준에서 `Runtime`, `FlowDefinitions`, `PilotFlows`는 모두 XGT-free 상태다.
- AH-PILOT-10의 결론인 "`CAAutomationHub.PilotFlows`는 XGT-free로 유지하고 XGT-specific code는 별도 adapter project에 둔다"는 상태가 유지되고 있다.
- 아직 `CAAutomationHub.PilotFlows.Xgt` project는 생성되지 않았다.

## 3. sibling XgtDriverCore repo 상태

Sibling repo:

- Path: `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- Branch: `main`
- Remote: `https://github.com/SanAndBom/AutomationHub_Rebuild`
- Latest commit: `fa0ab4f Merge pull request #119 from SanAndBom/codex/add-uint32-and-int32-display-formats`
- Working tree: dirty

최근 commit:

- `fa0ab4f Merge pull request #119 from SanAndBom/codex/add-uint32-and-int32-display-formats`
- `cda4bc0 Add UInt32/Int32 display decoding for device monitor grid`
- `2268250 Merge pull request #118 from SanAndBom/codex/refactor-baseblocks-dynamic-length-validation`
- `f5be118 Make FakePlc baseBlocks validation dynamic`
- `be84739 Merge pull request #117 from SanAndBom/codex/add-console-output-for-loaded-baseblocks`
- `8a5f2f0 Improve FakePLC startup baseBlock diagnostics`
- `44f0592 Merge pull request #116 from SanAndBom/codex/improve-memory-address-parsing-in-fakeplc`
- `6d742c1 Refactor FakePLC DB address resolution to use baseBlocks map`
- `d1db2d4 Merge pull request #115 from SanAndBom/codex/update-markdown-documents-to-reflect-current-state`
- `79f404d docs: refresh markdowns to current fakeplc-runner-monitor status`

Dirty files:

```text
 M tools/AutomationHub.XgtDriverCore.FakePlc/appsettings/fakeplc.map.json
?? context-events/pending/evt_20260507_234052_receiveframeasync-whenresponsearrivesinp.json
?? context-events/pending/evt_20260507_234052_tcptransport-basic-request-response.json
?? context-events/pending/evt_20260507_234052_tcptransport-timeout-handling.json
?? context-events/pending/evt_20260507_234052_xgtsession-basic-exchange.json
?? context-events/pending/evt_20260516_005654_iwritableruntimeplcchannel-runtime-state.json
```

Reference 대상 상태:

- `src/AutomationHub.XgtDriverCore` source tree: clean
- `src/AutomationHub.XgtDriverCore/AutomationHub.XgtDriverCore.csproj`: clean
- dirty file은 FakePlc map과 context event pending files에 한정된다.

Sibling solution projects include:

- `src\AutomationHub.XgtDriverCore\AutomationHub.XgtDriverCore.csproj`
- `tools\AutomationHub.XgtDriverCore.FakePlc\AutomationHub.XgtDriverCore.FakePlc.csproj`
- `XgtChannelRunner\XgtChannelRunner.csproj`
- `tests\AutomationHub.XgtDriverCore.Tests`
- `tests\AutomationHub.XgtDriverCore.IntegrationTests`
- `tests\AutomationHub.XgtChannelRunner.Tests`
- samples / tools / monitor projects
- `AutomationHub.ContextPublisher`

확인한 project 특징:

- `AutomationHub.XgtDriverCore`는 `Directory.Build.props`의 `net10.0` / nullable / implicit usings 설정을 사용하며, 별도 project/package dependency가 없다.
- `AutomationHub.XgtDriverCore.FakePlc`는 executable tool이고 `AutomationHub.XgtDriverCore`를 project reference로 참조하며 `appsettings/fakeplc.map.json`을 output에 복사한다.
- `XgtChannelRunner`는 `net10.0-windows`, WinForms, `AutomationHub.XgtDriverCore`, `ScottPlot.WinForms`, `Microsoft.Data.SqlClient`에 의존한다.

확인한 production API 후보:

- `IXgtSession`
- `XgtFrameBuilder`
- `XgtFrameParser`
- `XgtRawResponseClassifier`
- `XgtRawResponseInfo`
- `XgtResponseClassification`
- `TransportException`
- `TransportFailureKind`

판단:

- Adapter 구현에 필요한 최소 production dependency는 `AutomationHub.XgtDriverCore`다.
- `XgtChannelRunner`는 WinForms / SQL / runner orchestration dependency 때문에 direct reference 대상이 아니다.
- FakePlc는 production adapter dependency가 아니라 integration harness dependency로 유지한다.

## 4. local sibling ProjectReference 후보 검토

후보 구조:

```text
CAAutomationHub.PilotFlows.Xgt
    -> CAAutomationHub.PilotFlows
    -> ..\..\..\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\AutomationHub.XgtDriverCore.csproj
```

장점:

- 가장 빠르게 WorkStart XGT read adapter 구현으로 넘어갈 수 있다.
- 기존 `XgtDriverCore`의 `IXgtSession`, read/write request/response, parser/classifier, transport exception을 그대로 사용할 수 있다.
- driver core source copy 없이 검증된 production driver boundary를 재사용한다.
- 현재 dirty 범위가 `src/AutomationHub.XgtDriverCore`가 아니므로 단기 pilot risk를 제한적으로 관리할 수 있다.

위험:

- local path coupling이 강하다.
- `CAAutomationHub` repo만 clone하면 build가 깨질 수 있다.
- CI / 다른 PC / Codex 재현성이 낮다.
- sibling repo 전체 working tree가 dirty이면 evidence가 흐려진다.
- dirty FakePlc map은 production driver core에는 직접 영향이 없지만, integration harness를 함께 다룰 때 재현성 혼란을 만들 수 있다.

판정:

- 단기 local pilot 전용으로는 조건부 허용 가능하다.
- 전제는 `AutomationHub.XgtDriverCore` latest commit `fa0ab4f`를 local anchor로 명시하고, `src/AutomationHub.XgtDriverCore`와 csproj가 clean임을 다시 확인하는 것이다.
- repo 전체 clean anchor가 확보되기 전에는 장기/CI 기준 구조로 확정하지 않는다.
- ProjectReference는 `CAAutomationHub.PilotFlows.Xgt` project에만 허용하고, `Runtime`, `FlowDefinitions`, `PilotFlows`에는 금지한다.

## 5. git subtree 후보 검토

후보 구조:

```text
CAAutomationHub/external/AutomationHub.XgtDriverCore
```

장점:

- 단일 repo clone 재현성이 좋다.
- 사용자 선호인 "CAAutomationHub 안에 녹이기"와 가장 잘 맞을 수 있다.
- Codex / CI 접근성이 좋아진다.
- local sibling path coupling을 제거할 수 있다.

위험:

- repo size가 증가한다.
- upstream sync 부담이 생긴다.
- history / merge 관리 복잡도가 높아진다.
- 지금 즉시 수행하기에는 무겁고, dirty sibling repo를 먼저 clean anchor로 고정해야 한다.

판정:

- 장기 권장 후보로 유지한다.
- AH-PILOT-11에서는 실제 subtree 작업을 수행하지 않는다.
- subtree를 선택하려면 별도 task에서 clean commit anchor, external path, sync policy, subtree pull/push 운영 규칙을 먼저 닫는다.

## 6. source copy 후보 검토

장점:

- 빠르다.
- 단일 repo 내부에 들어온다.
- 아주 작은 pure helper rule은 local 재구성이 쉬울 수 있다.

위험:

- driver core drift가 발생한다.
- 검증된 `XgtDriverCore` source history와 validation 의미가 약해진다.
- XGT protocol / session / transport source가 잘못된 위치로 섞일 가능성이 크다.
- `PilotFlows`, `Runtime`, `FlowDefinitions`에 XGT-specific detail이 유입될 수 있다.

판정:

- `XgtDriverCore` 전체 source copy는 비권장이다.
- XGT protocol / session / transport / parser / classifier는 copy하지 않는다.
- WorkStart business helper rule처럼 이미 `PilotFlows` seam에 맞게 재구성 가능한 작은 규칙만 후속 review에서 선택 재구성할 수 있다.
- `WorkStartPilotService`는 source copy하지 않는다.

## 7. NuGet/package 후보 검토

장점:

- 장기적으로 dependency 관리가 가장 깔끔하다.
- version pinning이 가능하다.
- CI restore가 표준화된다.
- `CAAutomationHub` repo가 sibling path에 묶이지 않는다.

위험:

- package pipeline / publish / versioning 정책이 필요하다.
- 지금 pilot 속도에는 무겁다.
- local debugging과 driver 동시 수정이 불편할 수 있다.
- private feed 또는 local package source 운영 정책이 필요하다.

판정:

- 장기 후보로 남긴다.
- AH-PILOT-11에서는 보류한다.
- pilot read adapter를 빠르게 검증한 뒤, driver core가 더 안정된 release cadence를 가지면 재검토한다.

## 8. adapter project skeleton only 후보 검토

후보:

```text
CAAutomationHub.PilotFlows.Xgt
    -> CAAutomationHub.PilotFlows
    -> no XgtDriverCore reference yet
```

장점:

- boundary를 먼저 코드 구조로 고정할 수 있다.
- sibling repo dirty 상태와 무관하게 project / namespace / public API shape를 정할 수 있다.
- `Runtime`, `FlowDefinitions`, `PilotFlows` XGT-free 경계를 유지하기 쉽다.

위험:

- 실제 read adapter 구현은 여전히 미뤄진다.
- skeleton이 실제 `XgtDriverCore` API와 어긋날 수 있다.
- 너무 오래 skeleton만 유지하면 real PLC / FakePlc 검증이 늦어진다.

판정:

- sibling dirty state를 더 엄격하게 다루기로 한다면 AH-PILOT-12 후보로 적절하다.
- 다만 현재 `src/AutomationHub.XgtDriverCore` source/csproj가 clean으로 확인됐으므로, 단기 pilot 속도를 우선하면 skeleton only보다 read adapter skeleton으로 바로 가는 것이 더 실용적이다.

## 9. sibling dirty state 판단

확인 결과:

- Sibling branch: `main`
- Latest commit: `fa0ab4f`
- Dirty file: `tools/AutomationHub.XgtDriverCore.FakePlc/appsettings/fakeplc.map.json`
- Untracked files: `context-events/pending/*.json`
- `XgtDriverCore` source tree dirty 여부: clean
- `XgtDriverCore` csproj dirty 여부: clean
- reference 대상 project dirty 영향권: production driver core 기준 직접 영향 없음

판단:

- 원칙적으로 sibling repo 전체가 dirty이면 clean anchor 전 장기 reference 확정은 보류하는 것이 맞다.
- 그러나 dirty가 FakePlc map과 context event pending files로 한정되고, `src/AutomationHub.XgtDriverCore`와 csproj가 clean이면 단기 local pilot `ProjectReference`는 제한적으로 허용 가능하다.
- 이 경우에도 reference 문서에는 `fa0ab4f` commit anchor, sibling path dependency, dirty 범위, CI 재현성 한계를 명시해야 한다.
- FakePlc integration test로 넘어갈 때는 dirty FakePlc map이 직접 영향권에 들어오므로 반드시 clean/commit anchor를 먼저 확보해야 한다.

## 10. AH-PILOT-12 진행 가능성 판단

### 후보 1: WorkStart XGT Read Adapter Skeleton

판정: 조건부 추천

전제:

- `CAAutomationHub.PilotFlows.Xgt` project를 생성한다.
- 이 project만 `CAAutomationHub.PilotFlows`와 sibling `AutomationHub.XgtDriverCore`를 참조한다.
- `Runtime`, `FlowDefinitions`, `PilotFlows`는 XGT-free를 유지한다.
- read adapter만 구현한다.
- write / ACK / error write는 후속으로 보류한다.
- reference 문서 또는 closeout에 sibling path와 `fa0ab4f` anchor를 명시한다.

이유:

- `src/AutomationHub.XgtDriverCore`와 csproj가 clean으로 확인됐기 때문에 read adapter skeleton으로 넘어갈 수 있는 최소 근거는 있다.
- 실제 PLC read path를 너무 늦추지 않는 장점이 있다.
- AH-PILOT-09/10에서 이미 read result mapping과 adapter boundary를 닫았으므로 다음 구현 후보로 자연스럽다.

### 후보 2: XGT Adapter Project Skeleton

판정: 보수적 대안

전제:

- reference 전략을 더 엄격하게 보류한다.
- `CAAutomationHub.PilotFlows.Xgt` project / namespace / adapter class skeleton만 생성한다.
- `XgtDriverCore` reference와 actual read 구현은 넣지 않는다.

이유:

- sibling repo 전체 dirty state를 이유로 실제 reference를 보류하고 싶다면 안전하다.
- 그러나 실제 read verification은 한 단계 더 늦어진다.

### 후보 3: Sibling Repo Clean Anchor Task

판정: 장기 재현성 우선 시 추천

전제:

- dirty state가 reference 진행의 blocking risk로 판단된다.
- 먼저 `AutomationHub.XgtDriverCore` repo의 FakePlc map / context event pending files를 정리하거나 commit anchor를 확보한다.

이유:

- CI / 다른 PC / Codex 재현성을 가장 깔끔하게 만든다.
- FakePlc integration harness로 넘어가기 전에는 특히 필요하다.

### 후보 4: FakePlc Integration Harness Boundary Review

판정: read adapter 이후 또는 clean anchor 이후 추천

전제:

- FakePlc를 test-only dependency로 유지할 integration test project 위치와 실행 방식을 먼저 검토한다.

이유:

- 현재 dirty file이 FakePlc map이므로 FakePlc harness를 바로 붙이기 전에는 clean anchor가 필요하다.
- production read adapter보다 harness boundary를 먼저 닫고 싶을 때는 유용하다.

최종 판단:

- AH-PILOT-12는 `WorkStart XGT Read Adapter Skeleton`으로 진행 가능하다. 단, "local pilot only / sibling `fa0ab4f` anchor / `src/AutomationHub.XgtDriverCore` clean 확인 / adapter project only reference" 조건을 붙인다.
- 사용자가 clean reproducibility를 더 우선하면 AH-PILOT-12를 `Sibling Repo Clean Anchor Task`로 전환한다.

## 11. 권장안

권장 구조:

```text
CAAutomationHub.PilotFlows
    - IWorkStartPlcOperations seam
    - WorkStart business helpers
    - XGT-free

CAAutomationHub.PilotFlows.Xgt
    - CAAutomationHub.PilotFlows reference
    - AutomationHub.XgtDriverCore reference
    - IWorkStartPlcOperations read implementation
    - XGT response / exception -> WorkStartReadBlockOperationResult mapping
    - raw XGT diagnostics kept inside adapter/logging boundary

tests/CAAutomationHub.PilotFlows.Xgt.Tests
    - later integration harness
    - FakePlc test-only dependency

CAAutomationHub.Runtime
    - no XgtDriverCore / FakePlc / XgtChannelRunner reference

CAAutomationHub.FlowDefinitions
    - no XgtDriverCore / FakePlc / XgtChannelRunner reference
```

단기 추천:

1. AH-PILOT-12는 `WorkStart XGT Read Adapter Skeleton`으로 진행한다.
2. `CAAutomationHub.PilotFlows.Xgt` project만 생성한다.
3. 이 project만 sibling `AutomationHub.XgtDriverCore`를 local `ProjectReference`로 참조한다.
4. reference에는 `fa0ab4f` anchor와 local path dependency를 closeout에 기록한다.
5. read adapter만 구현하고 write / ACK / error write는 후속으로 남긴다.
6. FakePlc integration test project는 아직 만들지 않는다.

보정 조건:

- AH-PILOT-12 시작 직전에 sibling repo status를 다시 확인한다.
- `src/AutomationHub.XgtDriverCore` 또는 `AutomationHub.XgtDriverCore.csproj`가 dirty이면 read adapter skeleton으로 진행하지 않고 `Sibling Repo Clean Anchor Task`로 전환한다.
- FakePlc integration으로 넘어가기 전에는 FakePlc map dirty state를 clean/commit anchor로 정리한다.

장기 추천:

- subtree 또는 package 방식을 재검토한다.
- 사용자 선호인 "CAAutomationHub 안에 녹이기"와 CI 재현성을 우선하면 subtree가 유력하다.
- dependency versioning / release cadence가 중요해지면 NuGet/package가 유력하다.

## 12. 제외한 범위

이번 AH-PILOT-11에서 제외한 범위:

- production code 수정
- test code 수정
- solution / csproj 수정
- `ProjectReference` 추가
- `PackageReference` 추가
- subtree 작업
- source copy
- adapter project 생성
- XGT Adapter skeleton 생성
- FakePlc test project 생성
- actual PLC read/write 구현
- `Runtime` project 수정
- `PilotFlows` project 수정
- `FlowDefinitions` project 수정
- `XgtChannelRunner` project reference
- `WorkStartPilotService` source copy
- `RuntimeSnapshot` / `ChannelPollingResult` business detail 추가
- FLOW.JSON / parser / Flow Executor 연결
- ContextPublisher automatic publish 재도입
- commit

## 13. 실행한 명령

현재 repo:

- `git log --oneline -10`
- `git status --short`
- `dotnet sln C:\AutomationHub.Rebuild\CAAutomationHub\CAAutomationHub.sln list`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content src\CAAutomationHub.PilotFlows\CAAutomationHub.PilotFlows.csproj`
- `Get-Content src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj`
- `Get-Content src\CAAutomationHub.FlowDefinitions\CAAutomationHub.FlowDefinitions.csproj`
- `Get-Content tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- `Get-Content tests\CAAutomationHub.Runtime.Tests\RuntimeProjectReferenceBoundaryTests.cs`
- `Get-Content docs\harness\AH-PILOT-10.md`
- `Get-Content docs\harness\AH-RUNTIME-42.md`
- `Get-Content docs\harness\AH-RUNTIME-43.md`
- `Get-Content docs\harness\AH-RUNTIME-44.md`
- `rg -n "ProjectReference|PackageReference|XgtDriverCore|XgtChannelRunner|FakePlc" C:\AutomationHub.Rebuild\CAAutomationHub\src C:\AutomationHub.Rebuild\CAAutomationHub\tests -g "*.csproj" -g "*.cs"`
- `Get-Content CAAutomationHub.sln`
- `Test-Path docs\harness\AH-PILOT-11.md`

Sibling repo:

- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore log --oneline -10`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore branch --show-current`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore remote -v`
- `dotnet sln C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\AutomationHub.XgtDriverCore.sln list`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\AutomationHub.XgtDriverCore.csproj`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\AutomationHub.XgtDriverCore.FakePlc.csproj`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\XgtChannelRunner.csproj`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short -- src/AutomationHub.XgtDriverCore src/AutomationHub.XgtDriverCore/AutomationHub.XgtDriverCore.csproj`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short -- tools/AutomationHub.XgtDriverCore.FakePlc XgtChannelRunner context-events/pending`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\Directory.Build.props`
- `rg -n "interface IXgtSession|class XgtFrameBuilder|class XgtFrameParser|class XgtRawResponseClassifier|record XgtRawResponseInfo|class XgtRawResponseInfo|enum XgtResponseClassification|class TransportException|enum TransportFailureKind|class FakePlcScenarioServer|class WorkStartPilotService" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- `rg --files C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner | rg "(IXgtSession|XgtFrameBuilder|XgtFrameParser|XgtRawResponseClassifier|XgtRawResponseInfo|XgtResponseClassification|TransportException|TransportFailureKind|FakePlcScenarioServer|WorkStartPilotService)"`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Client\IXgtSession.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtRawResponseClassifier.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtRawResponseInfo.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Protocol\XgtResponseClassification.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Validation\TransportFailureKind.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Validation\TransportException.cs`

Validation:

- `git diff -- docs/harness/AH-PILOT-11.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-11.md`

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

추가 확인:

```text
git diff -- docs/harness/AH-PILOT-11.md
```

결과:

```text
no output
```

주의:

- `docs/harness/AH-PILOT-11.md`는 신규 untracked 파일이므로 plain `git diff -- docs/harness/AH-PILOT-11.md`에는 diff body가 표시되지 않았다.

## 15. git status --short 결과

실행:

```text
git status --short
```

결과:

```text
?? docs/harness/AH-PILOT-11.md
```

## 16. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-11 목표인 `XgtDriverCore` retrieval / reference strategy를 closeout 문서로 정리했다.
- 현재 `CAAutomationHub` solution / project / reference 상태를 확인했고, `Runtime`, `FlowDefinitions`, `PilotFlows`가 XGT-free인 상태를 기록했다.
- sibling repo branch, latest commit, remote, dirty files, untracked files를 기록했다.
- reference 대상인 `src/AutomationHub.XgtDriverCore` source tree와 `AutomationHub.XgtDriverCore.csproj`가 clean임을 확인했다.
- dirty 범위가 FakePlc map과 context-events pending files에 한정됨을 기록했다.
- local sibling `ProjectReference`, git subtree, source copy, NuGet/package, adapter skeleton only 후보를 각각 검토했다.
- AH-PILOT-12 후보를 비교하고, `WorkStart XGT Read Adapter Skeleton`을 조건부 추천하되 clean reproducibility 우선 시 `Sibling Repo Clean Anchor Task`로 전환하는 판단을 남겼다.
- production code, test code, solution, csproj, project reference, package reference, subtree, source copy, adapter project, FakePlc test project, actual PLC read/write, commit을 수행하지 않았다.
- ContextPublisher automatic publish를 재도입하지 않았다.
- requested validation commands를 실행했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
