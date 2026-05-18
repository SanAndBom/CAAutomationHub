# AH-PILOT-14-B Closeout - FakePlc WorkStart Read Integration Harness

## 1. Summary

AH-PILOT-14-B에서는 `CAAutomationHub.PilotFlows.Xgt.Tests`에 FakePlc 기반 WorkStart read integration harness를 추가했다.

변경 내용:

- `CAAutomationHub.PilotFlows.Xgt.Tests`에 sibling FakePlc tool project를 test-only `ProjectReference`로 추가했다.
- in-process FakePlc server test helper를 추가했다.
- `WorkStartXgtPlcOperations`가 `WorkStartXgtReadOptions.Default`로 `%DB10000` / `90` words를 실제 XGT session read path로 읽는지 검증했다.
- read result `Success(data)`를 확인했다.
- test-specific start signal index `83`을 사용해 FakePlc `D5083` start signal을 해석했다.
- LOT ID 1 offset `0`, LOT ID 2 offset `10`, LOT ID length `6`을 사용해 LOT ID selection을 검증했다.

영향:

- production project에는 FakePlc reference를 추가하지 않았다.
- Runtime / FlowDefinitions / PilotFlows core는 수정하지 않았다.
- `WorkStartReadBlockLayout.DefaultStartSignalWordIndex = 80`은 유지했다.
- `WorkStartXgtReadOptions.Default = "%DB10000" / 90`은 유지했다.
- FakePlc map은 수정하지 않았다.
- XgtChannelRunner reference, DB / SqlClient, actual PLC read/write, write / ACK / error adapter, FLOW.JSON / parser / executor는 추가하지 않았다.

## 2. AH-PILOT-14-A Alignment 판정 요약

AH-PILOT-14-A 판정:

- Alignment: `B. Partially aligned`
- `WorkStartXgtReadOptions.Default = "%DB10000" / 90`
- `WorkStartReadBlockLayout.DefaultStartSignalWordIndex = 80`
- current FakePlc start signal = `D5083`
- current FakePlc `%DB10000` 기준:
  - index `0` -> `D5000`
  - index `10` -> `D5010`
  - index `80` -> `D5080`
  - index `83` -> `D5083`
- LOT ID offsets are aligned.
- start signal location is not aligned with the CAAutomationHub / `PilotScenarioConfig` default.

AH-PILOT-14-B 결정:

- CAAutomationHub default는 수정하지 않는다.
- FakePlc integration harness에서만 test-specific `StartSignalWordIndex = 83` override를 사용한다.

## 3. Test-Specific Override 사용 여부

사용했다.

Test-specific layout:

- `StartSignalWordIndex = 83`
- `LotId1WordOffset = 0`
- `LotId2WordOffset = 10`
- `LotIdWordLength = 6`

의미:

- `StartSignalWordIndex = 83`은 current FakePlc initializer의 `D5083` start signal을 해석하기 위한 test-specific override다.
- default `StartSignalWordIndex = 80`은 변경하지 않았다.
- LOT ID offsets와 length는 default와 동일하게 사용했다.

## 4. 변경 파일 목록

- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
  - sibling FakePlc tool project를 test-only `ProjectReference`로 추가했다.
- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/WorkStart/WorkStartXgtFakePlcIntegrationTests.cs`
  - in-process FakePlc server를 사용한 WorkStart XGT read integration test를 추가했다.
- `docs/harness/AH-PILOT-14-B.md`
  - AH-PILOT-14-B historical record를 추가했다.

## 5. FakePlc / XGT Read Integration 방식

Test flow:

1. `FakePlcMapConfig`를 test 내부에서 구성한다.
2. `%DB10000` base block을 `180` bytes / `90` words로 만든다.
3. `FakePlcScenarioInitializer.CreateMemoryImage(...)`를 사용해 LOT ID / start signal을 초기화한다.
4. `FakePlcRuntime`과 `FakePlcProtocolHandler`를 in-process `TcpListener`에 연결한다.
5. `TcpTransport` + `XgtSession`으로 loopback FakePlc에 연결한다.
6. `WorkStartXgtPlcOperations(session, WorkStartXgtReadOptions.Default)`를 만든다.
7. `EnsureConnectedAsync()` 후 `ReadWorkStartBlockAsync()`를 호출한다.
8. `WorkStartReadBlockInterpreter`로 returned data를 해석한다.

선택 이유:

- FakePlcScenarioServer는 protocol response double이며 WorkStart `%DB10000` memory map을 모델링하지 않는다.
- 이번 목표는 FakePlc memory initializer와 WorkStart read adapter의 실제 read path 정렬을 검증하는 것이다.
- 따라서 actual FakePlc runtime / protocol handler를 test-only dependency로 사용했다.

## 6. 검증한 Read Result

검증 내용:

- `ReadWorkStartBlockAsync()` result status is `Success`.
- result data is not null.
- result data length is `180` bytes.
- read path uses `WorkStartXgtReadOptions.Default`, therefore `%DB10000` / `90` words.

TDD RED:

- command: `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
- result: failed as expected
- reason: FakePlc namespace / types were unavailable because the test project did not yet have the test-only FakePlc reference.

TDD GREEN:

- command: `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
- result: passed
- count: failed 0, passed 15, skipped 0, total 15

## 7. 검증한 Start Signal / LOT ID

Start signal:

- test-specific index `83` is active.
- This corresponds to current FakePlc `D5083`.

LOT ID:

- LOT ID 1 offset `0`, length `6` words -> `"S0007652610B"`.
- LOT ID 2 offset `10`, length `6` words -> empty string.
- selected LOT ID is `"S0007652610B"`.
- selection source is `LotId1`.

## 8. 수정하지 않은 Default 목록

수정하지 않은 defaults:

- `WorkStartXgtReadOptions.DefaultReadStartVariable = "%DB10000"`
- `WorkStartXgtReadOptions.DefaultReadWordCount = 90`
- `WorkStartReadBlockLayout.DefaultReadWordCount = 90`
- `WorkStartReadBlockLayout.DefaultStartSignalWordIndex = 80`
- `WorkStartReadBlockLayout.DefaultLotId1WordOffset = 0`
- `WorkStartReadBlockLayout.DefaultLotId2WordOffset = 10`
- `WorkStartReadBlockLayout.DefaultLotIdWordLength = 6`

## 9. 제외한 범위

이번 AH-PILOT-14-B에서 제외한 범위:

- production project FakePlc reference
- Runtime project 수정
- FlowDefinitions project 수정
- PilotFlows core default 수정
- PilotFlows.Xgt default 수정
- FakePlc map 수정
- XgtChannelRunner reference 추가
- DB concrete 구현
- Microsoft.Data.SqlClient reference 추가
- actual PLC read/write
- write / ACK / error adapter 구현
- FLOW.JSON / parser / Flow Executor 연결
- RuntimeSnapshot / ChannelPollingResult 참조
- WorkStartPilotService source copy
- ContextPublisher automatic publish

## 10. 테스트 결과

실행:

```text
dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj
dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj
dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj
```

결과:

```text
CAAutomationHub.PilotFlows.Xgt.Tests:
  failed 0, passed 15, skipped 0, total 15

CAAutomationHub.PilotFlows.Tests:
  failed 0, passed 40, skipped 0, total 40

CAAutomationHub.Runtime.Tests:
  failed 0, passed 142, skipped 0, total 142
```

주의:

- 첫 `CAAutomationHub.PilotFlows.Xgt.Tests` run은 TDD RED로 실패했다.
- 실패 이유는 FakePlc namespace / type을 찾을 수 없는 compile failure였고, test-only FakePlc reference 추가 전 기대한 failure였다.

## 11. 빌드 결과

실행:

```text
dotnet build CAAutomationHub.sln
```

결과:

```text
first run:
  failed
  CS2012: CAAutomationHub.Runtime.dll obj output was locked by another process.
  message indicated Microsoft Defender Antivirus Service may have locked the file.

retry after tests completed:
  passed
  warnings 0
  errors 0
```

판단:

- 첫 build failure는 코드 변경 문제가 아니라 validation commands를 병렬 실행하면서 발생한 transient file lock으로 판단했다.
- 같은 command를 sequential retry 했고, `dotnet build CAAutomationHub.sln`은 통과했다.

## 12. Boundary Scan 결과

Boundary scan:

```text
rg -n "XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src tests
```

결과:

```text
command exited with matches from existing Runtime / WPF contract files and tests.
No forbidden match was introduced in src/CAAutomationHub.PilotFlows.Xgt or tests/CAAutomationHub.PilotFlows.Xgt.Tests.
```

Project reference scan:

```text
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
```

결과:

```text
tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj:
  PackageReference coverlet.collector
  PackageReference Microsoft.NET.Test.Sdk
  PackageReference xunit
  PackageReference xunit.runner.visualstudio
  ProjectReference ..\..\src\CAAutomationHub.PilotFlows.Xgt\CAAutomationHub.PilotFlows.Xgt.csproj
  ProjectReference ..\..\..\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\AutomationHub.XgtDriverCore.FakePlc.csproj

src\CAAutomationHub.PilotFlows.Xgt\CAAutomationHub.PilotFlows.Xgt.csproj:
  ProjectReference ..\CAAutomationHub.PilotFlows\CAAutomationHub.PilotFlows.csproj
  ProjectReference ..\..\..\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\AutomationHub.XgtDriverCore.csproj

Runtime / FlowDefinitions / PilotFlows core:
  no FakePlc reference
  no XgtChannelRunner reference
  no Microsoft.Data.SqlClient reference
```

Additional targeted scans:

```text
rg -n "XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" tests\CAAutomationHub.PilotFlows.Xgt.Tests src\CAAutomationHub.PilotFlows.Xgt
  no matches, rg exit code 1

rg -n "FakePlc" src tests -g "*.csproj"
  only tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj

rg -n "XgtChannelRunner" src tests -g "*.csproj"
  no matches, rg exit code 1

rg -n "Microsoft.Data.SqlClient|SqlConnection" src tests -g "*.csproj" -g "*.cs"
  no matches, rg exit code 1
```

Boundary judgment:

- FakePlc reference는 `tests/CAAutomationHub.PilotFlows.Xgt.Tests`에만 존재해야 한다.
- Runtime / FlowDefinitions / PilotFlows core에는 FakePlc / XGT 추가 reference가 없어야 한다.
- XgtChannelRunner reference는 없어야 한다.
- DB / SqlClient reference는 없어야 한다.

판정:

- 위 boundary 조건을 만족한다.

## 13. 다음 후보

- AH-PILOT-15 후보: WorkStart XGT write / ACK / error writer boundary review
- AH-PILOT-15 후보: FakePlc start signal baseline reconciliation review
- AH-PILOT-15 후보: WorkStart read options/layout consistency validation
- AH-PILOT-15 후보: 현장 pilot baseline evidence collection for `D5080` vs `D5083`

## 14. 실행한 명령

Phase 1:

- `git status --short`
- `git diff --check`
- `Get-Content docs\harness\AH-PILOT-14-A.md`
- `git add docs/harness/AH-PILOT-14-A.md`
- `git commit -m "docs: close out AH-PILOT-14-A fakeplc workstart alignment review"`
- `git status --short`

Phase 2 precheck:

- `git status --short`
- `git log --oneline -8`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short`
- `Get-Content docs\harness\AH-PILOT-14-A.md`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\WorkStart\WorkStartXgtReadOptions.cs`
- `Get-Content src\CAAutomationHub.PilotFlows\WorkStart\WorkStartReadBlockLayout.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
- `Get-Content src\CAAutomationHub.PilotFlows.Xgt\CAAutomationHub.PilotFlows.Xgt.csproj`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\AutomationHub.XgtDriverCore.FakePlc.csproj`
- `rg -n "class XgtSession|sealed class XgtSession|record XgtConnection|class XgtConnection|XgtSession\(" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests`
- `rg --files C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests | Select-Object -First 30`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Client\XgtSession.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Client\XgtSessionOptions.cs`
- `rg --files C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore | Select-String -Pattern 'Transport|Tcp'`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\Channels\PlcChannelFakePlcValidationTests.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tests\AutomationHub.XgtChannelRunner.Tests\Operational\FakePlcOperationalHarnessTests.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Transport\XgtTransportOptions.cs`
- `Get-Content C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\Transport\TcpTransport.cs`

Implementation / TDD:

- `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
- `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
- `git status --short`
- `git diff -- tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj tests/CAAutomationHub.PilotFlows.Xgt.Tests/WorkStart/WorkStartXgtFakePlcIntegrationTests.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\WorkStart\WorkStartXgtFakePlcIntegrationTests.cs`
- `Get-Content tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`

Final validation:

- `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
- `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
- `dotnet build CAAutomationHub.sln`
- `git diff --check`
- `git status --short`
- `rg -n "XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src tests`
- `rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"`

## 15. Self-Check

판정: `ACCEPT`

근거:

- AH-PILOT-14-A closeout를 별도 commit으로 먼저 고정했다.
- AH-PILOT-14-B는 working tree clean 상태에서 시작했다.
- FakePlc dependency는 test project에만 추가했다.
- WorkStart XGT read path를 실제 `XgtSession` / `TcpTransport` / FakePlc protocol handler로 검증했다.
- test-specific `StartSignalWordIndex = 83` override를 사용했다.
- CAAutomationHub defaults를 수정하지 않았다.
- FakePlc map을 수정하지 않았다.
- XgtChannelRunner reference, DB / SqlClient, Runtime / FlowDefinitions / PilotFlows core 변경을 추가하지 않았다.
- validation commands를 실행했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
