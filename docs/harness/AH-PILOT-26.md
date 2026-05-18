# AH-PILOT-26 Closeout - WorkComplete ACK ON/OFF FakePlc Harness

## 1. Summary

AH-PILOT-26은 완공요청 흐름의 최소 ACK ON/OFF skeleton을 FakePlc 하네스로 검증했다.

이번 단계는 DB 조회, payload build, WorkStart flow 연결, Runtime state 반영 없이 다음 handshake만 다뤘다.

```text
Complete request ON
  -> Complete ACK ON write
Complete request OFF
  -> Complete ACK OFF write
```

변경은 `CAAutomationHub.PilotFlows.WorkComplete` core helper와 `CAAutomationHub.PilotFlows.Xgt.WorkComplete` XGT adapter에 한정했다. `PilotApp`, WPF, Runtime, FlowDefinitions, 실제 DB, 실제 PLC는 수정하거나 연결하지 않았다.

판정: `ACCEPT`

## 2. 근거 확인

확인한 기준:

- FakePlc complete request signal: D5084
- read block 내 complete signal word index: `84`
- Complete ACK target: `%DB11418`
- ACK ON value: `1`
- ACK OFF value: `0`
- FakePlc map: `%DB11418` base block 존재

근거 파일:

- sibling `FakePlcScenarioInitializer`: `CompleteSignalWordDAddress = 5084`
- sibling `FakePlcWordSignalTests`: D5084가 DB10000 offset 168에 반영됨
- sibling `PilotScenarioConfig`: `CompleteAckWriteVariable = "%DB11418"`, `CompleteSignalWordIndexInReadBlock = 84`
- sibling `fakeplc.map.json`: `%DB11418` surface 존재

## 3. 변경 파일

- `src/CAAutomationHub.PilotFlows/WorkComplete/IWorkCompletePlcOperations.cs`
  - 완공 ACK 최소 PLC operation boundary 추가.
- `src/CAAutomationHub.PilotFlows/WorkComplete/WorkCompleteReadBlockLayout.cs`
  - read word count와 complete signal word index default 추가.
- `src/CAAutomationHub.PilotFlows/WorkComplete/WorkCompleteReadBlockInterpreter.cs`
  - complete request active/inactive 판단 helper 추가.
- `src/CAAutomationHub.PilotFlows/WorkComplete/WorkCompleteReadBlockOperationResult.cs`
- `src/CAAutomationHub.PilotFlows/WorkComplete/WorkCompleteReadBlockOperationStatus.cs`
  - XGT-free read result shape 추가.
- `src/CAAutomationHub.PilotFlows/WorkComplete/WorkCompleteAckService.cs`
  - ACK ON/OFF write orchestration 추가.
- `src/CAAutomationHub.PilotFlows/WorkComplete/WorkCompleteAckOptions.cs`
- `src/CAAutomationHub.PilotFlows/WorkComplete/WorkCompleteAckResult.cs`
- `src/CAAutomationHub.PilotFlows/WorkComplete/WorkCompleteAckStatus.cs`
  - ACK value, result, status 계약 추가.
- `src/CAAutomationHub.PilotFlows.Xgt/WorkComplete/WorkCompleteXgtPlcOperations.cs`
  - `%DB10000` read와 `%DB11418` write adapter 추가.
- `src/CAAutomationHub.PilotFlows.Xgt/WorkComplete/WorkCompleteXgtReadOptions.cs`
- `src/CAAutomationHub.PilotFlows.Xgt/WorkComplete/WorkCompleteXgtWriteOptions.cs`
  - XGT read/write default options 추가.
- `tests/CAAutomationHub.PilotFlows.Tests/WorkComplete/WorkCompleteAckServiceTests.cs`
  - ACK ON, ACK OFF, request still ON wait tests 추가.
- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/WorkComplete/WorkCompleteXgtFakePlcIntegrationTests.cs`
  - FakePlc `%DB11418` ACK ON/OFF read-back integration test 추가.

## 4. TDD Evidence

RED:

```text
dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj --no-restore
dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj --no-restore
```

결과:

```text
CS0234 / CS0246: CAAutomationHub.PilotFlows.WorkComplete 및 CAAutomationHub.PilotFlows.Xgt.WorkComplete 타입 없음
```

GREEN:

```text
dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj --no-restore
```

결과:

```text
passed 45, failed 0, skipped 0
```

```text
dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj --no-restore
```

결과:

```text
passed 41, failed 0, skipped 0
```

## 5. Validation

실행:

```text
dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj --no-restore
dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj --no-restore
dotnet test tests\CAAutomationHub.PilotApp.Tests\CAAutomationHub.PilotApp.Tests.csproj --no-restore
dotnet test tests\CAAutomationHub.Wpf.Tests\CAAutomationHub.Wpf.Tests.csproj --no-restore
dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --no-restore
dotnet build CAAutomationHub.sln --no-restore
git diff --check
rg -n "XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src tests
git status --short
```

결과:

```text
PilotFlows.Xgt tests: passed 41, failed 0, skipped 0
PilotFlows tests: passed 45, failed 0, skipped 0
PilotApp tests: passed 8, failed 0, skipped 0
WPF tests: passed 223, failed 0, skipped 0
Runtime tests: passed 142, failed 0, skipped 0
solution build: warnings 0, errors 0
git diff --check: exit code 0
```

Boundary scan:

- 기존 Runtime / WPF / test contract hit는 유지된다.
- 이번 변경에서 `XgtChannelRunner`, `SqlConnection`, `Microsoft.Data.SqlClient`, `FLOW.JSON`, FlowExecutor 연결은 추가하지 않았다.
- RuntimeSnapshot / ChannelPollingResult는 수정하지 않았다.

## 6. Boundary / Harness 영향

유지한 boundary:

- PilotFlows core는 XGT-free다.
- PilotFlows.Xgt만 XgtDriverCore를 참조한다.
- FakePlc는 test-only project reference로만 사용한다.
- WorkComplete 최소 ACK flow는 Runtime polling state path에 섞지 않았다.
- WPF / PilotApp / composition / App.xaml DI wiring은 수정하지 않았다.
- 실제 DB concrete와 connection string은 추가하지 않았다.

Harness 의미:

- FakePlc D5084 complete request signal과 `%DB11418` ACK target을 통해 최소 완공 ACK ON/OFF handshake를 검증했다.
- 아직 완공 DB 조회, payload build, failure policy, timeout/retry, real PLC readiness는 다루지 않았다.

## 7. 남은 리스크 / 후속

- Complete ACK 실패 시 error code / retry / recovery policy는 아직 `MissingPolicy`에 가깝다.
- Complete flow는 DB나 payload 없는 ACK skeleton이다.
- Complete request OFF는 test에서 FakePlc memory를 직접 D5084 OFF로 전환한다. 운영 loop/polling integration은 후속이다.
- AH-PILOT-28 이후 DB Concrete 진행 시 connection string은 user-secrets / environment variable / local settings 후보로 분리해야 한다.

DB Concrete 후보:

- AH-PILOT-DB-01 DB Query Concrete Boundary Review
- AH-PILOT-DB-02 SQL Server WorkStartDataQuery implementation
- AH-PILOT-DB-03 DB integration smoke test
- AH-PILOT-DB-04 PilotApp Real DB / FakePlc Transaction Harness

## 8. Self-Check

판정: `ACCEPT`

근거:

- Complete ACK address/value/request signal 근거를 sibling FakePlc와 scenario config에서 확인했다.
- FakePlc read-back harness로 `%DB11418` ACK ON/OFF를 검증했다.
- 관련 tests와 solution build를 실행했다.
- Runtime / WPF / Driver / FakePlc / Harness 경계를 유지했다.
- 실제 PLC / real DB / connection string / FLOW.JSON / RuntimeSnapshot / ChannelPollingResult 변경 없이 완료했다.
