# AH-PILOT-LIVE-09-CORRECTION Closeout - Polling Command Reenable After Failure

## 1. Summary

AH-PILOT-LIVE-09-CORRECTION은 AH-PILOT-LIVE-08에서 관찰된 WPF `Poll Once` 버튼 disabled 유지 리스크를 보정한 기록이다.

변경 내용:

- `PilotPollingViewModel`의 async command continuation이 WPF 호출 context로 복귀하도록 보정했다.
- polling failure snapshot 이후 `IsCommandRunning`이 `false`로 돌아오고 `PollOnceCommand.CanExecute`가 다시 `true`가 되는 회귀 테스트를 추가했다.
- snapshot event가 background thread에서 발생해도 ViewModel 반영은 캡처된 synchronization context로 마샬링되도록 했다.

목적:

- SqlServer-like failure 또는 failed polling snapshot 이후 사용자가 WPF에서 즉시 재시도할 수 있게 한다.
- 실패 snapshot은 화면에 남기되 command 상태가 잠기지 않게 한다.

영향:

- 변경 범위는 WPF ViewModel command/snapshot dispatch와 WPF test에 한정된다.
- `RuntimeSnapshot`, `ChannelPollingResult`, `FLOW.JSON`, FakePlc map, production DI는 수정하지 않았다.
- WPF가 XGT/FakePlc/SqlClient를 직접 참조하지 않는 기존 boundary를 유지했다.

## 2. Changed Files

- `src/CAAutomationHub.Wpf/ViewModels/Pilot/PilotPollingViewModel.cs`
  - command await에서 WPF synchronization context를 보존한다.
  - service snapshot event를 캡처된 context로 마샬링한다.
- `tests/CAAutomationHub.Wpf.Tests/ViewModels/PilotPollingViewModelTests.cs`
  - failed snapshot 이후 command 재활성화와 재시도 가능 여부를 검증한다.
  - 재활성화 `CanExecuteChanged`가 캡처된 context에서 발생하는지 검증한다.

## 3. Validation

실행:

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj --filter "FullyQualifiedName~PilotPollingViewModelTests.PollOnceCommand_ReenablesOnCapturedContextAfterPollingFailure"
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj
dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj
dotnet build CAAutomationHub.sln
git diff --check
git status --short
```

결과:

```text
RED: targeted WPF test failed before implementation.
GREEN targeted: failed 0, passed 1, skipped 0, total 1
WPF tests: failed 0, passed 229, skipped 0, total 229
PilotApp tests: failed 0, passed 16, skipped 0, total 16
Build: warning 0, error 0
git diff --check: exit 0, line-ending warnings only
git status --short: WPF ViewModel/test and this closeout modified before commit
```

## 4. Harness / Boundary

- Harness: WPF ViewModel test로 failure 이후 command recovery를 고정했다.
- Boundary: Pilot WPF display/control layer 안의 command 상태 보정이다.
- Runtime shared execution path: 변경 없음.
- RuntimeSnapshot 오염: 없음.
- ChannelPollingResult 오염: 없음.
- FakePlc map 수정: 없음.
- Real PLC 접속/write: 없음.
- DB connection string 기록: 없음.

## 5. STOP Check

다음 STOP 조건은 발생하지 않았다.

- command helper 전체 대규모 변경 없음.
- WPF DI 수정 없음.
- RuntimeSnapshot 수정 없음.
- PilotPollingService production logic 대규모 변경 없음.

## 6. Self-Check

```text
Historical record: PASS
Failure command reenable: PASS
Retry possible after failed snapshot: PASS
WPF context command reenable: PASS
Runtime boundary preserved: PASS
Driver/FakePlc direct WPF reference absent: PASS
Validation evidence: PASS
ACCEPT judgment: ACCEPT
```
