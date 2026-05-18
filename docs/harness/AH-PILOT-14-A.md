# AH-PILOT-14-A Closeout - FakePlc / WorkStart Layout Alignment Review

## 1. Summary

AH-PILOT-14-A는 FakePlc integration 전에 `WorkStartXgtReadOptions`, `WorkStartReadBlockLayout`, sibling FakePlc memory map / initializer가 같은 read layout 기준을 사용하는지 확인한 Boundary Review다.

확인 결과, `CAAutomationHub`의 WorkStart defaults와 sibling `WorkStartPilotService` / `PilotScenarioConfig` defaults는 서로 일치한다.

- `WorkStartXgtReadOptions.Default`: `%DB10000`, `90` words
- `WorkStartReadBlockLayout` default: start signal index `80`, LOT ID 1 offset `0`, LOT ID 2 offset `10`, LOT ID length `6`
- `PilotScenarioConfig`: 동일하게 `%DB10000`, `90`, start index `80`, LOT ID offsets `0` / `10`, length `6`

다만 sibling FakePlc initializer는 start signal을 `D5083`에 쓴다. current FakePlc implementation 기준으로 `%DB10000`은 byte offset `10000`이고, D address 변환은 `D * 2` byte offset이다. 따라서 `%DB10000` read block의 word index `80`은 `D5080`이며, `D5083`은 word index `83`이다.

즉 LOT ID 영역은 정렬되어 있지만, start signal 위치는 `CAAutomationHub` / `WorkStartPilotService` default와 FakePlc initializer가 다르다. 판정은 `B. Partially aligned`다.

이번 작업은 read-only 조사와 closeout 문서 작성만 수행했다. production code, test code, adapter, FakePlc map, csproj / solution, reference, actual PLC read/write, FakePlc integration test, commit은 수행하지 않았다.

## 2. 확인한 WorkStartXgtReadOptions default

파일:

- `src/CAAutomationHub.PilotFlows.Xgt/WorkStart/WorkStartXgtReadOptions.cs`

확인값:

- `DefaultReadStartVariable = "%DB10000"`
- `DefaultReadWordCount = 90`
- `WorkStartXgtReadOptions.Default = new("%DB10000", 90)`

`WorkStartXgtPlcOperations`는 options constructor에서 `ReadWordCount * 2`를 `continuousByteLength`로 변환해 `XgtReadRequest`를 만든다.

의미:

- `%DB10000`에서 `90` words, 즉 `180` bytes를 continuous read한다.
- 이 값은 AH-PILOT-13-B에서 adapter-local pilot baseline으로 추가되었다.
- Runtime / FlowDefinitions / PilotFlows core로 XGT address를 올리지 않는 boundary는 유지되어 있다.

## 3. 확인한 WorkStartReadBlockLayout default

파일:

- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartReadBlockLayout.cs`

확인값:

- `DefaultReadWordCount = 90`
- `DefaultStartSignalWordIndex = 80`
- `DefaultLotId1WordOffset = 0`
- `DefaultLotId2WordOffset = 10`
- `DefaultLotIdWordLength = 6`

관련 확인:

- `WorkStartFlowOptions`는 위 layout default를 기본값으로 사용한다.
- `WorkStartReadBlockInterpreterTests`는 start signal을 `DefaultStartSignalWordIndex * 2` byte offset에서 읽고, LOT ID를 word offset 기준으로 읽는다.
- `LotIdWordLength = 6`은 `12` bytes ASCII 영역이다.

## 4. FakePlc map / initializer 확인 결과

확인 파일:

- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\appsettings\fakeplc.map.json`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcScenarioInitializer.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcMemoryImage.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcRuntime.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtDriverCore.Tests\FakePlcWordSignalTests.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtDriverCore.Tests\FakePlcMemoryImageAddressResolutionTests.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\PilotScenarioConfig.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\WorkStartPilotService.cs`

FakePlc map:

- current `fakeplc.map.json` has `%DB10000` base block with `360` hex chars = `180` bytes = `90` words.
- scenario `lotId1 = "S0007652610B"`, `lotId2 = ""`, `startSignal = true`, `heartbeatEnabled = true`, `heartbeatInitialValue = true`.
- sibling repo status shows this map file is currently modified:
  - `%DB12000` block shortened
  - `readDelayMs` changed from `80` to `50`
  - `%DB10000` length and WorkStart scenario values are unchanged by that diff.

FakePlc initializer:

- LOT ID 1 is written at `D5000`, length `12` bytes.
- LOT ID 2 is written at `D5010`, length `12` bytes.
- heartbeat is written at `D5080`.
- start signal is written at `D5083`.
- complete signal is written at `D5084`.

FakePlc runtime rule:

- ACK write to `%DB11416` clears `D5083`, not `D5080`.

FakePlc tests:

- `FakePlcWordSignalTests` asserts `D5080` at byte offset `160` and `D5083` at byte offset `166` inside `%DB10000`.
- `FakePlcMemoryImageAddressResolutionTests` asserts `%DB10000[10000..10179]` corresponds to `D5000..D5089`.

FakePlcScenarioServer test double:

- `tests\AutomationHub.XgtChannelRunner.Tests\TestDoubles\FakePlcScenarioServer.cs` is a protocol response double.
- It returns fixed ACK payload bytes (`AA BB`) for read ACK and does not model the WorkStart `%DB10000` memory map.
- Therefore it is not evidence for WorkStart LOT ID / start signal address alignment.

## 5. Address / index 계산

사용자 지시의 후보 공식:

```text
ReadStartVariable = "%DB10000"
read block word index = N
해당 D word 주소 = D10000 + N
```

현재 sibling FakePlc implementation 기준 판정:

- 위 후보 공식은 current FakePlc에는 맞지 않는다.
- `FakePlcMemoryImage.ParseDbByteOffset("%DB10000")`는 numeric part `10000`을 byte offset으로 해석한다.
- `FakePlcMemoryImage.DAddressToByteOffset(dAddress)`는 `dAddress * 2`다.
- 따라서 `%DB10000`의 시작 byte offset `10000`은 `D5000`에 해당한다.

current FakePlc 공식:

```text
%DB10000 read block word index = N
absolute byte offset = 10000 + (N * 2)
D word address = (10000 + (N * 2)) / 2
               = D(5000 + N)
```

따라서:

- index `0` -> byte offset `10000` -> `D5000`
- index `10` -> byte offset `10020` -> `D5010`
- index `80` -> byte offset `10160` -> `D5080`
- index `83` -> byte offset `10166` -> `D5083`
- index `84` -> byte offset `10168` -> `D5084`

WorkStart read options / layout와 FakePlc initializer 비교:

| 항목 | CAAutomationHub / PilotScenarioConfig default | current FakePlc initializer | 판정 |
| --- | --- | --- | --- |
| read start | `%DB10000` | `%DB10000` base block | aligned |
| read length | `90` words / `180` bytes | `%DB10000` block `180` bytes | aligned |
| LOT ID 1 | index `0` -> `D5000`, 6 words / 12 bytes | `D5000`, length 12 bytes | aligned |
| LOT ID 2 | index `10` -> `D5010`, 6 words / 12 bytes | `D5010`, length 12 bytes | aligned |
| start signal | index `80` -> `D5080` | `D5083` | misaligned |
| complete signal | not in CAAutomationHub WorkStart read skeleton default, sibling config index `84` | `D5084` | aligned with sibling config |
| heartbeat | not WorkStart start signal | `D5080` | conflicts with start index `80` if heartbeat is enabled |

핵심 결론:

- `D5083`은 `%DB10000` read block 안에 포함된다.
- 그러나 `D5083`은 word index `80`이 아니라 word index `83`이다.
- current FakePlc에서 word index `80`은 `D5080`이고, 이 주소는 heartbeat로 초기화된다.

## 6. Alignment 판정

판정: `B. Partially aligned`

근거:

- `CAAutomationHub` default와 sibling `WorkStartPilotService` / `PilotScenarioConfig` default는 일치한다.
- `%DB10000` / `90` words, LOT ID offsets `0` / `10`, LOT ID length `6`은 current FakePlc map / initializer와도 일치한다.
- start signal만 다르다.
  - CAAutomationHub / PilotScenarioConfig: start signal index `80` -> current FakePlc 기준 `D5080`
  - FakePlc initializer / clear-on-ACK rule: start signal `D5083` -> read block index `83`
- FakePlc의 `D5080`은 heartbeat로 사용되고 있어, 현재 map의 `heartbeatInitialValue = true` 상태에서는 `StartSignalWordIndex = 80`이 의미상 start signal이 아니라 heartbeat를 active start처럼 읽을 위험이 있다.

`C. Misaligned`로 보지 않은 이유:

- CAAutomationHub default 자체는 기존 `WorkStartPilotService` / `PilotScenarioConfig`와 다르지 않다.
- mismatch는 current FakePlc initializer의 start signal address와 pilot config의 start signal index 사이에 있다.

`D. Unknown`으로 보지 않은 이유:

- FakePlc address resolution tests가 `%DB10000` -> `D5000..D5089` mapping을 명시적으로 고정하고 있다.
- FakePlc initializer와 runtime ACK clear rule이 `D5083`을 start signal로 직접 사용한다.
- `PilotScenarioConfig`가 start signal index `80`을 직접 default로 둔다.

남은 확인 필요:

- 실제 현장 pilot baseline에서 start signal의 의미상 주소가 `D5080`인지 `D5083`인지 최종 확인이 필요하다.
- 현장 baseline이 `D5083`이라면 `PilotScenarioConfig`와 CAAutomationHub layout default가 함께 재검토 대상이다.
- 현재까지 repo evidence만 기준으로는 `WorkStartPilotService` / `PilotScenarioConfig`가 CAAutomationHub defaults의 business anchor다.

## 7. 보정 방향 후보

### 후보 A: CAAutomationHub defaults 유지, FakePlc map / initializer를 후속 integration에서 맞춘다

내용:

- `WorkStartXgtReadOptions.Default = "%DB10000" / 90` 유지.
- `WorkStartReadBlockLayout.DefaultStartSignalWordIndex = 80` 유지.
- FakePlc에서 WorkStart start signal을 `D5080`으로 맞추거나, heartbeat와 start signal 책임을 분리한다.

장점:

- CAAutomationHub와 기존 `WorkStartPilotService` / `PilotScenarioConfig` anchor를 유지한다.
- Default를 FakePlc 편의에 맞춰 흔들지 않는다.

위험:

- sibling FakePlc behavior 변경이 필요하다.
- `D5080` heartbeat의 기존 의미가 있다면 그 의미도 함께 재정렬해야 한다.

### 후보 B: FakePlc 기준에 맞춰 CAAutomationHub layout default를 변경한다

내용:

- `DefaultStartSignalWordIndex`를 `83`으로 변경하는 방향이다.

장점:

- current FakePlc initializer / ACK clear rule과 즉시 정렬된다.

위험:

- 기존 `WorkStartPilotService` / `PilotScenarioConfig` default와 CAAutomationHub default가 달라진다.
- 현장 pilot baseline 확인 없이 default를 바꾸면 business anchor를 FakePlc 편의에 맞춰 바꾸는 위험이 있다.

### 후보 C: Default를 고정 변경하지 않고 FakePlc integration test에서 test-specific layout/options를 명시한다

내용:

- FakePlc integration test에서 `StartSignalWordIndex = 83`을 명시하거나, FakePlc-specific fixture map을 사용한다.
- default alignment test와 FakePlc compatibility test를 분리한다.

장점:

- CAAutomationHub default를 유지하면서 current FakePlc와의 integration path를 검증할 수 있다.
- AH-PILOT-14-B에서 test-only reference boundary를 유지하기 쉽다.

위험:

- default path와 harness path가 diverge할 수 있다.
- "Runtime shared execution path / Harness divergence" 관점에서 명시적인 문서화가 필요하다.

### 후보 D: 현장 pilot baseline과 FakePlc baseline을 분리한다

내용:

- 현장 pilot baseline은 `PilotScenarioConfig` / CAAutomationHub defaults로 둔다.
- FakePlc baseline은 scenario profile로 분리해 `D5083` start signal을 명시한다.

장점:

- FakePlc가 historical / diagnostic scenario를 유지할 수 있다.
- 현장 baseline과 harness baseline을 이름으로 구분할 수 있다.

위험:

- Integration test가 어떤 baseline을 검증하는지 흐려질 수 있다.
- Default flow 검증과 FakePlc scenario 검증을 분리하는 harness policy가 필요하다.

## 8. 권장안

권장: 후보 A 또는 후보 C를 우선 검토한다.

현재 repo evidence 기준으로는 `WorkStartPilotService` / `PilotScenarioConfig`가 CAAutomationHub defaults의 business anchor다. 따라서 AH-PILOT-14-A 범위에서는 CAAutomationHub defaults를 FakePlc convenience에 맞춰 바로 바꾸지 않는 것이 안전하다.

구체 권장:

1. `WorkStartXgtReadOptions.Default = "%DB10000" / 90`은 유지한다.
2. `WorkStartReadBlockLayout.DefaultStartSignalWordIndex = 80`은 현장 baseline 확인 전까지 유지한다.
3. AH-PILOT-14-B에서 current FakePlc를 그대로 사용할 경우, `StartSignalWordIndex = 83` override를 test-specific로 명시하거나 FakePlc map / initializer alignment patch를 먼저 수행한다.
4. Default path alignment를 검증하려면 FakePlc가 `D5080`을 WorkStart start signal로 제공해야 한다. 이 경우 heartbeat의 주소/의미도 함께 정리해야 한다.
5. 현장 evidence가 `D5083`을 실제 start signal로 확인하면, 그때는 `PilotScenarioConfig`와 CAAutomationHub `WorkStartReadBlockLayout` default를 함께 보정하는 AH-PILOT-14-C 후보로 다룬다.

AH-PILOT-14-B로 바로 FakePlc read integration을 진행해도 되는지:

- current FakePlc map / initializer를 default layout 그대로 검증하는 integration으로는 진행하지 않는 것이 안전하다.
- `StartSignalWordIndex = 83` test-specific override 또는 FakePlc map alignment patch를 전제로 하면 진행 가능하다.
- LOT ID read integration만 분리 검증하는 제한된 harness라면 현재 map으로도 진행 가능하지만, start signal semantic verification은 통과 판정하면 안 된다.

## 9. AH-PILOT-14-B 후보

### 후보 1: FakePlc / XGT Read Integration Harness

전제:

- `StartSignalWordIndex = 83` test-specific override를 명시하거나,
- FakePlc initializer를 CAAutomationHub default index `80`에 맞추는 patch가 먼저 이루어진다.

주의:

- current FakePlc 그대로 + default layout 그대로는 start signal semantic mismatch가 있다.
- FakePlc reference는 test-only project boundary에만 둔다.

### 후보 2: FakePlc Memory Map Alignment Patch

전제:

- FakePlc map / initializer가 WorkStart pilot baseline과 맞지 않는다고 판단한다.
- sibling repo에서 `D5080` heartbeat / start signal 책임을 재정렬한다.

추천도:

- default alignment를 우선 검증하려면 가장 직접적인 후보다.

### 후보 3: WorkStart Read Options / Layout Correction

전제:

- 현장 pilot baseline 또는 사용자 추가 evidence가 start signal을 `D5083`으로 확정한다.
- 이 경우 `PilotScenarioConfig.StartSignalWordIndexInReadBlock = 80`도 같이 보정 대상이 된다.

주의:

- CAAutomationHub만 단독 변경하면 기존 business anchor와 diverge한다.

### 후보 4: Alignment Evidence Collection

전제:

- 실제 현장 map, Wireshark capture, PLC monitor evidence, 운영 map 중 하나가 필요하다.

추천 상황:

- `D5080` heartbeat와 `D5083` start signal의 의미가 모두 현장 기준에서 중요할 가능성이 있으면 먼저 선택한다.

## 10. 제외한 범위

이번 AH-PILOT-14-A에서 제외한 범위:

- `CAAutomationHub.Runtime` project 수정
- `CAAutomationHub.FlowDefinitions` project 수정
- `CAAutomationHub.PilotFlows` project 수정
- `CAAutomationHub.PilotFlows.Xgt` project 수정
- FakePlc map / initializer 수정
- test code 수정
- adapter 수정
- FakePlc integration test 생성
- actual PLC read/write test
- `csproj` / solution 수정
- ProjectReference 추가
- PackageReference 추가
- FakePlc reference 추가
- XgtChannelRunner reference 추가
- Microsoft.Data.SqlClient reference 추가
- actual DB Query 구현
- FLOW.JSON 파일 생성
- JSON schema / parser 생성
- Flow Executor 구현
- RuntimeSnapshot / ChannelPollingResult 참조
- WorkStartPilotService source copy
- ContextPublisher automatic publish
- commit

## 11. 실행한 명령

Current repo:

- `git log --oneline -8`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-12.md`
- `Get-Content docs\harness\AH-PILOT-13-A.md`
- `Get-Content docs\harness\AH-PILOT-13-B.md`
- `Get-Content docs\context\META_IPRO_CODEX_COGNITIVE_INTERFACE.md`
- `Get-Content docs\context\COGNITIVE_SYNC_CHECK.md`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartReadBlockLayout.cs`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtReadOptions.cs`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtPlcOperations.cs`
- `rg --files tests\CAAutomationHub.PilotFlows.Tests\WorkStart`
- `rg --files tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart`
- `rg -n "DefaultStartSignalWordIndex|LotId1|LotId2|WordOffset|WordLength|ReadWordCount|ReadStartVariable|StartSignalWordIndex|DefaultReadWordCount" C:\AutomationHub.Rebuild\CAAutomationHub`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart\WorkStartXgtPlcOperationsTests.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Tests\WorkStart\WorkStartReadBlockInterpreterTests.cs`

Sibling repo:

- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\appsettings\fakeplc.map.json`
- `Get-ChildItem -Recurse C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc | Select-Object FullName`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\TestDoubles\FakePlcScenarioServer.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\PilotScenarioConfig.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\WorkStartPilotService.cs`
- `rg -n "D5083|5083|DB10000|%DB10000|StartSignal|StartSignalWordIndex|LotId1|LotId2|D5000|D5020|D5080|D5083" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcScenarioInitializer.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcMemoryImage.cs`
- `Get-ChildItem C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration | Select-Object Name,FullName`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcRuntime.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtDriverCore.Tests\FakePlcWordSignalTests.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtDriverCore.Tests\FakePlcMemoryImageAddressResolutionTests.cs`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcMapConfig.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcScenarioConfig.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcRuleConfig.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Configuration\FakePlcMapConfigLoader.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\README.md`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Program.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\docs\context\02_implementation.md`
- `rg -n "LotReadStartVariable|LotReadWordCount|StartSignalWordIndexInReadBlock|LotId1WordOffset|LotId2WordOffset|LotIdWordLength|D5083|D5080|D5000|%DB10000" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools`
- `rg --files C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\Runtime\FakePlcProtocolHandler.cs`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore diff -- tools/AutomationHub.XgtDriverCore.FakePlc/appsettings/fakeplc.map.json`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore log --oneline -5`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore branch --show-current`
- PowerShell JSON check for `%DB10000` hex chars / bytes / words

File path correction commands:

- `Get-Content ...\Configuration\FakePlcScenarioOptions.cs` was attempted and failed because the file does not exist. Actual file is `FakePlcScenarioConfig.cs`.
- `Get-Content ...\Configuration\FakePlcMapOptions.cs` was attempted and failed because the file does not exist. Actual file is `FakePlcMapConfig.cs`.
- `Get-Content ...\Runtime\FakePlcProtocolServer.cs` was attempted and failed because the file does not exist. Actual file is `FakePlcProtocolHandler.cs`.

Validation:

- `git diff -- docs/harness/AH-PILOT-14-A.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-PILOT-14-A.md`

테스트 / 빌드:

- 문서 작성과 read-only Boundary Review만 수행했으므로 실행하지 않았다.

## 12. git diff --check 결과

실행:

```text
git diff --check
```

결과:

```text
exit code 0
output 없음
```

의미:

- whitespace error 없음.
- 주의: `docs/harness/AH-PILOT-14-A.md`는 신규 untracked 파일이므로 `git diff --check`의 tracked diff 검사 대상에는 포함되지 않는다.

## 13. git status --short 결과

실행:

```text
git status --short
```

결과:

```text
?? docs/harness/AH-PILOT-14-A.md
```

## 14. Self-Check

판정: `ACCEPT_WITH_CORRECTION`

근거:

- AH-PILOT-14-A 목표인 FakePlc / WorkStart read layout alignment review를 closeout 문서로 남겼다.
- `WorkStartXgtReadOptions.Default`와 `WorkStartReadBlockLayout` default를 확인했다.
- sibling `PilotScenarioConfig` / `WorkStartPilotService`와 CAAutomationHub defaults가 일치함을 확인했다.
- FakePlc map `%DB10000` length가 `180` bytes / `90` words임을 확인했다.
- current FakePlc address mapping에서 `%DB10000` word index `80`이 `D5080`, `D5083`이 index `83`임을 확인했다.
- LOT ID offsets `0` / `10` / length `6`은 FakePlc `D5000` / `D5010` / 12 bytes initialization과 일치함을 확인했다.
- start signal은 CAAutomationHub / `PilotScenarioConfig` default index `80`과 FakePlc initializer `D5083` 사이에 mismatch가 있음을 확인했다.
- FakePlc current map의 heartbeat `D5080`이 default start index `80`과 충돌할 수 있음을 확인했다.
- production code, test code, adapter, FakePlc map, csproj / solution, references, integration test, actual PLC read/write, commit을 수행하지 않았다.

Correction 필요:

- AH-PILOT-14-B에서 current FakePlc를 그대로 사용할 경우 `StartSignalWordIndex = 83` test-specific override가 필요하다.
- default path 검증을 목표로 하면 FakePlc map / initializer를 `StartSignalWordIndex = 80` 기준으로 정렬해야 한다.
- 현장 pilot baseline이 `D5083`으로 확인되면 `PilotScenarioConfig`와 CAAutomationHub layout default를 함께 재검토해야 한다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
