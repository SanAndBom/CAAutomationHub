# AH-PILOT-25 Closeout - WorkStart ACK OFF FakePlc Harness

## 1. Summary

AH-PILOT-25는 WorkStart happy path의 ACK ON 이후, 착공요청 OFF 감지 시 동일 ACK target에 `0`을 write하는 최소 ACK OFF 흐름을 FakePlc 하네스로 검증했다.

변경은 `WorkStartFlowService`에 ACK OFF 책임을 억지로 합치지 않고, `WorkStartAckOffService`를 별도 core helper로 추가했다. 이 service는 기존 `IWorkStartPlcOperations.ReadWorkStartBlockAsync`와 `WorkStartReadBlockInterpreter.IsStartSignalActive`를 재사용해 request OFF를 판단하고, ACK OFF writer는 기존 `WriteStartAckAsync` capability를 ACK value `0` 옵션으로 구성해 사용한다.

영향:

- WorkStart ACK OFF 흐름이 FakePlc read/write read-back evidence를 갖게 되었다.
- RuntimeSnapshot / ChannelPollingResult / FlowDefinitions / WPF App.xaml DI wiring은 수정하지 않았다.
- FakePlc reference는 test project에만 유지했다.
- 실제 PLC / 실제 DB / connection string은 사용하지 않았다.

판정: `ACCEPT`

## 2. 근거 확인

확인한 기준:

- Start ACK ON target: `%DB11416`
- Start ACK ON value: `1`
- ACK OFF target 후보: ACK ON과 동일 target `%DB11416`
- ACK OFF value: `0`
- Request OFF 판단: 기존 read block layout과 `WorkStartReadBlockInterpreter.IsStartSignalActive(...)`의 word signal inactive 판단 재사용
- FakePlc test-specific start signal 위치: D5083에 해당하는 word index `83`

Sibling repo 확인:

- `AutomationHub.XgtDriverCore` FakePlc map은 `%DB11416` surface를 제공한다.
- FakePlc는 `%DB11416` write를 `LastAckValue`로 기록한다.
- 기존 FakePlc rule은 ACK ON 후 start signal clear를 지원한다.

## 3. 변경 파일

- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartAckOffService.cs`
  - request OFF 확인 후 ACK OFF write를 수행하는 최소 service 추가.
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartAckOffOptions.cs`
  - start signal word index 옵션 추가.
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartAckOffResult.cs`
  - ACK OFF 결과 shape 추가.
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartAckOffStatus.cs`
  - `AckOffWritten`, `WaitingRequestOff`, `ReadFailed`, `AckOffWriteFailed` 상태 추가.
- `tests/CAAutomationHub.PilotFlows.Tests/WorkStart/WorkStartAckOffServiceTests.cs`
  - request OFF이면 ACK OFF writer를 호출하고, request ON이면 writer를 호출하지 않는 core tests 추가.
- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/WorkStart/WorkStartXgtFakePlcIntegrationTests.cs`
  - FakePlc에서 `%DB11416`을 ACK ON 상태로 준비한 뒤 request OFF 상태에서 ACK OFF service가 `0x0000`을 write하는 read-back test 추가.

## 4. TDD Evidence

RED:

```text
dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj --no-restore
dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj --no-restore
```

결과:

```text
CS0246 / CS0103: WorkStartAckOffService, WorkStartAckOffOptions, WorkStartAckOffStatus 없음
```

GREEN:

```text
dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj --no-restore
```

결과:

```text
passed 42, failed 0, skipped 0
```

```text
dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj --no-restore
```

결과:

```text
passed 40, failed 0, skipped 0
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
PilotFlows.Xgt tests: passed 40, failed 0, skipped 0
PilotFlows tests: passed 42, failed 0, skipped 0
PilotApp tests: passed 8, failed 0, skipped 0
WPF tests: passed 223, failed 0, skipped 0
Runtime tests: passed 142, failed 0, skipped 0
solution build: warnings 0, errors 0
git diff --check: exit code 0
```

참고:

- `git diff --check` 중 `WorkStartXgtFakePlcIntegrationTests.cs` line ending warning이 표시되었지만 whitespace error는 없었다.
- boundary scan hit는 기존 Runtime / WPF / test contract references이며, 이번 변경에서 금지된 production reference는 추가하지 않았다.

## 6. Boundary / Harness 영향

유지한 boundary:

- PilotFlows core는 XGT / FakePlc를 참조하지 않는다.
- PilotFlows.Xgt는 기존 XGT operation adapter를 유지한다.
- FakePlc는 test-only integration harness로만 사용한다.
- Runtime polling state path와 pilot business transaction path를 섞지 않았다.
- WPF ViewModel / command shell / App.xaml DI wiring은 수정하지 않았다.
- RuntimeSnapshot / ChannelPollingResult / FlowDefinitions는 수정하지 않았다.

Harness 의미:

- FakePlc가 ACK target `%DB11416`에 대한 실제 XGT write와 read-back evidence를 제공한다.
- ACK OFF는 WorkStart happy transaction 안에 강제로 포함하지 않고 request OFF 관측 이후 별도 service로 유지했다.

## 7. 남은 리스크 / 후속

- ACK OFF 실패 policy와 timeout / retry는 아직 정의하지 않았다.
- ACK OFF service는 ACK value `0` writer를 options로 구성한 `IWorkStartPlcOperations`를 받는 방식이다. 후속에서 ACK ON/OFF writer interface를 분리할 수 있다.
- AH-PILOT-26에서 WorkComplete ACK ON/OFF address 근거가 부족하면 구현하지 않고 Boundary Review로 닫는다.

## 8. Self-Check

판정: `ACCEPT`

근거:

- WorkStart ACK OFF target/value/request OFF 기준이 기존 FakePlc와 PilotFlows 계약에서 확인되었다.
- FakePlc 기반 ACK OFF read-back test가 추가되었다.
- 관련 tests와 solution build를 실행했다.
- Runtime / WPF / Driver / FakePlc / Harness 경계를 유지했다.
- 실제 PLC / real DB / connection string / FLOW.JSON / RuntimeSnapshot / ChannelPollingResult 변경 없이 완료했다.
