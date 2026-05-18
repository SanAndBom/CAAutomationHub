# AH-PILOT-LIVE-08 Closeout - FakePlc Operational Rehearsal

## 1. Summary

AH-PILOT-LIVE-08은 실제 PLC를 사용하지 않고 `FakePlcLocal` 기반으로 WPF PilotPollingView 운영 리허설을 수행한 기록이다.

이번 관찰의 핵심 결과:

- 최신 live anchor는 `AH-PILOT-DB-LIVE-02`, commit `c921203`이다.
- working tree는 시작 시 clean이었다.
- `config/pilot.local.json`은 ignored local file이며 실제 connection string을 포함하지 않았다.
- FakePlc를 `::1:2004`에 실행했고 `Test-NetConnection localhost -Port 2004`가 성공했다.
- 현재 셸에는 `CAAH_WORKSTART_DB_CONNECTION_STRING`이 없어서 실제 SqlServer 성공 흐름은 재현하지 못했다.
- WPF는 DB env var 미설정 시 안전 fallback으로 fake profile을 로드했다.
- WPF fallback 상태에서 Poll Once 3회 관찰은 crash 없이 완료됐다.
- 비밀값이 아닌 임시 invalid env var를 WPF process에만 주입해 `FakePlcLocal + SqlServer` profile load와 DB failure 표시를 관찰했다.
- SqlServer profile poll은 FakePlc read, LOT ID 표시, DB exception 표시, FakePlc error-code write까지 수행했다.
- 실제 PLC read/write는 수행하지 않았다.
- 실제 DB connection string은 파일, 문서, git에 기록하지 않았다.

판정은 `ACCEPT_WITH_CORRECTION`이다. FakePlc 기반 WPF 운영 리허설의 주요 표시/실패 경로는 확인했지만, 실제 SqlServer env var가 없어 payload write / Start ACK ON 성공 흐름은 이번 세션에서 재확인하지 못했다. 또한 SqlServer failure 후 WPF `Poll Once` command가 disabled 상태로 남는 관찰이 있어 후속 보정 후보로 남긴다.

## 2. Preconditions

실행:

```text
git log --oneline -8
git status --short
Test-NetConnection localhost -Port 2004
[bool]$env:CAAH_WORKSTART_DB_CONNECTION_STRING
git check-ignore config/pilot.local.json
```

결과:

```text
latest commit: c921203 docs: update AH-PILOT-DB-LIVE-02 sqlserver wpf observation
git status --short: empty
localhost:2004 initial TcpTestSucceeded: False
CAAH_WORKSTART_DB_CONNECTION_STRING present: False
git check-ignore config/pilot.local.json -> config/pilot.local.json
```

주의:

- env var 값은 출력하지 않았다.
- 실제 DB secret은 closeout에 쓰지 않았다.

## 3. Local Config

`config/pilot.local.json` 관찰 요약:

```text
Exists: True
Profile: FakePlcLocal
PLC: localhost:2004
DB mode: SqlServer
DB env var: CAAH_WORKSTART_DB_CONNECTION_STRING
Has literal connection string: False
git status --short --ignored config/pilot.local.json -> !! config/pilot.local.json
```

의미:

- local config는 ignored 상태다.
- 실제 connection string 값은 local config에 없다.
- commit 대상이 아니다.

## 4. FakePlc 실행 상태

FakePlc 실행:

```text
dotnet run --project C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\AutomationHub.XgtDriverCore.FakePlc.csproj -- ::1 2004 AH-PILOT-LIVE-08 [map path]
```

결과:

```text
Listening on ::1:2004
Loaded lotId1 at D5000 = S0007652610B
D5083 = 0x0001
D5084 = 0x0000
Test-NetConnection localhost -Port 2004 -> TcpTestSucceeded: True
```

관찰:

- `localhost`가 IPv6 loopback `::1`로 확인되어 FakePlc도 `::1`에 bind했다.
- FakePlc map file은 수정하지 않았다.
- 실제 PLC host/IP는 사용하지 않았다.

## 5. WPF 실행 관찰

실행:

```text
dotnet run --project src\CAAutomationHub.Wpf\CAAutomationHub.Wpf.csproj
```

DB env var 미설정 상태의 WPF header:

```text
Pilot local config failed safely; fake profile loaded. Pilot SQL Server DB connection string environment variable ... is not set.
```

관찰:

- MainWindow 표시 성공
- PilotPollingView 표시 성공
- WPF crash 없음
- DB env var가 없을 때 실제 SqlServer profile 대신 fake fallback으로 안전하게 내려감

## 6. WorkStart 반복 Polling 결과

DB env var 미설정 fallback 상태에서 UI Automation으로 Poll Once 3회를 실행했다.

1회차:

```text
Request: WorkStart
Status: WorkStartProcessed
LOT ID: PILOT-FAKE-LOT
Start Req: True
Complete Req: False
Start ACK: True
Complete ACK: -
Result: Succeeded
Error: None
Log: Fake WorkStart processed for fake-profile.
```

2회차:

```text
Request: WorkStart
Status: Idle
LOT ID: PILOT-FAKE-LOT
Start Req: True
Start ACK: True
Result: WaitingRequestOff
Error: -
Log: ACK is already ON; waiting for request OFF.
```

3회차:

```text
Request: WorkStart
Status: Idle
LOT ID: PILOT-FAKE-LOT
Start Req: True
Start ACK: True
Result: WaitingRequestOff
Error: -
Log: ACK is already ON; waiting for request OFF.
```

의미:

- WPF 반복 Poll Once는 fallback path에서 crash 없이 동작했다.
- Start ACK ON 이후 request OFF 대기 표시가 유지됐다.
- 이 관찰은 FakePlcLocal + SqlServer 성공 path가 아니라 WPF fallback path다.

## 7. FakePlcLocal + SqlServer Profile 관찰

실제 DB secret이 없으므로 비밀값이 아닌 임시 invalid env var를 WPF process에만 주입했다. 값은 문서에 기록하지 않는다.

WPF header:

```text
FakePlcLocal pilot polling profile loaded for localhost:2004 with SqlServer DB mode.
```

Poll Once 관찰:

```text
Request: WorkStart
Status: Failed
LOT ID: S0007652610B
Start Req: True
Complete Req: False
Start ACK: -
Complete ACK: -
Result: Failed
Error: DbException
Log: SQL Server WorkStart query exception.
```

FakePlc log:

```text
read %DB10000 length=180
read %DB10000 length=180
read %DB10000 length=180
write %DB11410 length=2
```

의미:

- WPF가 `FakePlcLocal + SqlServer` profile을 로드했다.
- FakePlc read path가 동작했다.
- LOT ID `S0007652610B`가 WPF에 표시됐다.
- DB query 실패가 WPF에 `Failed / DbException`으로 표시됐다.
- error-code write가 FakePlc `%DB11410`에 수행됐다.
- 실제 DB secret 없이 성공 payload write / Start ACK ON은 재현하지 못했다.

## 8. ACK ON/OFF 관찰 결과

관찰 결과:

- fallback path: Start ACK ON 표시 확인
- fallback path: Start request가 계속 ON이므로 2회차/3회차는 `WaitingRequestOff`로 유지됨
- SqlServer profile invalid DB path: Start ACK ON은 수행되지 않음
- FakePlc `%DB11410` error write는 수행됨

미확인:

- Start request OFF 시 Start ACK OFF
- Complete request ON 시 Complete ACK ON
- Complete request OFF 시 Complete ACK OFF

이유:

- 현재 외부 FakePlc process map을 수정하지 않는 제약 때문에 요청 신호를 실시간 전환하지 않았다.
- WPF에서 request OFF / complete ON/OFF를 조작하는 UI는 없다.
- backend harness에는 해당 cycle coverage가 존재한다.

관련 backend evidence:

```text
PilotPollingServiceFakePlcIntegrationTests.PollOnceAsync_WithFakePlc_ProcessesStartAndCompleteOnOffCycle
```

이 테스트는 WorkStart ON, Start ACK OFF, WorkComplete ACK ON, WorkComplete ACK OFF cycle을 in-process FakePlc로 검증한다.

## 9. WorkComplete 관찰 결과

이번 WPF 운영 관찰에서는 WorkComplete ON/OFF를 직접 확인하지 못했다.

판정:

```text
backend harness exists, WPF display/control is insufficient for external FakePlc request toggling
```

이번 단계에서는 UI를 크게 수정하지 않았다.

## 10. FakePlc 중지 Failure 관찰

FakePlc process를 중지한 뒤:

```text
Test-NetConnection localhost -Port 2004 -> TcpTestSucceeded: False
```

추가 Poll Once를 시도했으나, 직전 SqlServer failure 이후 WPF `Poll Once` button이 disabled 상태로 남아 있었다.

관찰:

```text
WPF process Responding: True
Poll Once Enabled: False
Status remained: Failed
Error remained: DbException
```

의미:

- WPF process 자체는 crash하지 않았다.
- 다만 실패 후 command 재활성화가 되지 않는 운영 리스크가 관찰됐다.
- 구조 보정 없이 즉시 해결하기 어려운 UI 상태 문제로 판단해 이번 closeout에는 risk로 기록한다.

후속 후보:

- `PilotPollingViewModel` async command completion / UI thread continuation / `CanExecuteChanged` 재활성화 동작을 별도 AH-PILOT-LIVE correction으로 검증한다.

## 11. 실제 PLC 미사용 확인

- 실제 PLC 접속 없음
- 실제 PLC read 없음
- 실제 PLC write 없음
- 실제 PLC host/IP를 config 또는 문서에 기록하지 않음
- FakePlc localhost/loopback만 사용

## 12. 실제 DB Secret 미기록 확인

- 실제 DB connection string 값 출력 없음
- 실제 DB connection string 파일 기록 없음
- 실제 DB connection string closeout 기록 없음
- `config/pilot.local.json`은 env var name만 사용
- 임시 invalid env var는 WPF process 환경에만 주입했고 값은 문서에 기록하지 않음

## 13. 테스트 결과

```text
dotnet test tests\CAAutomationHub.PilotComposition.Tests\CAAutomationHub.PilotComposition.Tests.csproj
PASS: failed 0, passed 15, skipped 0, total 15

dotnet test tests\CAAutomationHub.PilotApp.Tests\CAAutomationHub.PilotApp.Tests.csproj
PASS: failed 0, passed 16, skipped 0, total 16

dotnet test tests\CAAutomationHub.Wpf.Tests\CAAutomationHub.Wpf.Tests.csproj
PASS: failed 0, passed 228, skipped 0, total 228

dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj
PASS: failed 0, passed 45, skipped 0, total 45

dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj
PASS: failed 0, passed 41, skipped 0, total 41

dotnet test tests\CAAutomationHub.PilotFlows.SqlServer.Tests\CAAutomationHub.PilotFlows.SqlServer.Tests.csproj
PASS: failed 0, passed 4, skipped 1, total 5

dotnet test tests\CAAutomationHub.PilotSmoke.Tests\CAAutomationHub.PilotSmoke.Tests.csproj
PASS: failed 0, passed 5, skipped 0, total 5

dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj
PASS: failed 0, passed 142, skipped 0, total 142
```

SqlServer smoke skip:

- `CAAH_WORKSTART_DB_CONNECTION_STRING`이 없어서 env-backed live smoke는 skip됐다.

## 14. 빌드 결과

```text
dotnet build CAAutomationHub.sln
PASS: warning 0, error 0
```

## 15. Secret Scan 결과

명령:

```text
rg -n "Password=|Pwd=|User ID=|Data Source=|Initial Catalog=|Server=|TrustServerCertificate|Encrypt=|ca_erp|PlcFaDatabase" src tests docs config tools -g "!**/bin/**" -g "!**/obj/**"
```

결과:

- actual connection string match 없음
- 실제 DB host/user/password match 없음
- 기존 closeout 문서 안의 scan command 문자열만 match됨

추가 확인:

```text
rg -n "Password=|Pwd=|User ID=|Data Source=|Initial Catalog=|Server=|TrustServerCertificate|Encrypt=|ca_erp|PlcFaDatabase" src tests config tools -g "!**/bin/**" -g "!**/obj/**"
```

결과:

- no matches

## 16. Boundary Scan 결과

명령:

```text
rg -n "XgtChannelRunner|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON" src tests docs tools
```

결과:

- 기존 historical docs와 기존 Runtime/WPF source/test reference가 다수 match됨
- 이번 작업에서 `RuntimeSnapshot` 수정 없음
- 이번 작업에서 `ChannelPollingResult` 수정 없음
- 이번 작업에서 `FLOW.JSON` / executor 연결 없음
- 이번 작업에서 `XgtChannelRunner` reference 추가 없음

추가 source/test/tools 확인:

```text
rg -n "XgtChannelRunner|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON" src tests tools
```

해석:

- `RuntimeSnapshot` / `ChannelPollingResult` match는 기존 Runtime/WPF boundary source 및 tests다.
- `XgtChannelRunner` match는 forbidden reference를 검증하는 기존 test assertion이다.
- 신규 production boundary contamination은 없다.

## 17. 변경 파일 목록

Tracked change:

- `docs/harness/AH-PILOT-LIVE-08.md`

Local-only ignored:

- `config/pilot.local.json`

수정하지 않은 것:

- production code
- test code
- FakePlc map file
- RuntimeSnapshot
- ChannelPollingResult
- FLOW.JSON / executor
- project reference

## 18. 다음 후보

1. `AH-PILOT-LIVE-08-CORRECTION`: SqlServer failure 후 WPF `Poll Once` command 재활성화 문제 검증 및 보정
2. 실제 DB env var가 있는 세션에서 FakePlcLocal + SqlServer success path 재관찰
3. WPF에서 request OFF / complete ON/OFF를 외부 FakePlc process에 대해 관찰할 수 있는 non-production harness 방법 검토
4. 실제 PLC는 별도 승인 전까지 계속 미사용

## 19. Self-Check

```text
Historical record: PASS
Actual PLC unused: PASS
Actual PLC write absent: PASS
FakePlc localhost only: PASS
DB secret not recorded: PASS
config/pilot.local.json ignored: PASS
FakePlc map untouched: PASS
RuntimeSnapshot untouched: PASS
ChannelPollingResult untouched: PASS
FLOW.JSON / executor untouched: PASS
XgtChannelRunner reference not added: PASS
Tests/build: PASS
Secret scan: PASS
Boundary scan: PASS
SqlServer success rehearsal: BLOCKED by missing env var
WPF command recovery after SqlServer failure: RISK OBSERVED
ACCEPT judgment: ACCEPT_WITH_CORRECTION
```
