# AH-PILOT-DB-LIVE-02 Closeout - SqlServer env smoke and WPF observation

## 1. Summary

AH-PILOT-DB-LIVE-02는 `AH-PILOT-DB-LIVE` commit `41179d4` 이후 실제 SQL Server DB와 FakePlcLocal WPF PilotPollingView를 함께 관찰한 실행 검증이다.

이번 세션에서는 SQL Server connection string과 test LOT ID를 PowerShell process env var로만 주입했다. `config/pilot.local.json`은 ignored local file로 유지했고, 실제 connection string 값은 code/config/sample/closeout/commit에 남기지 않았다.

검증 결과:

- SqlServer WorkStartDataQuery smoke test가 skip 없이 실행되었다.
- FakePlc는 `localhost:2004` 경로에서 reachable 상태가 되었다.
- WPF는 `FakePlcLocal + SqlServer` profile을 load했다.
- WPF PilotPollingView에서 `Poll Once` 실행 후 실제 DB 조회 기반 WorkStart 흐름이 `Succeeded`로 관찰되었다.
- write는 실제 PLC가 아니라 FakePlc 대상으로만 수행되었다.

판정은 `ACCEPT`다.

## 2. Env Var / Secret Handling

사용 env var name:

- `CAAH_WORKSTART_DB_CONNECTION_STRING`
- `CAAH_WORKSTART_TEST_LOT_ID`

확인:

```text
CAAH_WORKSTART_DB_CONNECTION_STRING present: True
CAAH_WORKSTART_TEST_LOT_ID present: True
```

값은 모두 `[REDACTED]`로 취급했다.

## 3. DB Smoke 결과

실행:

```text
dotnet test tests\CAAutomationHub.PilotFlows.SqlServer.Tests\CAAutomationHub.PilotFlows.SqlServer.Tests.csproj
```

결과:

```text
PASS: failed 0, passed 5, skipped 0, total 5
```

관찰:

- `SqlServerWorkStartDataQuerySmokeTests.QueryAsync_WithEnvironmentConfiguration_ReturnsTerminalStatus`가 skip 없이 실행되었다.
- WPF PilotPolling observation 결과 기준 DB query는 `Succeeded` 흐름으로 이어졌다.
- mapped LOT ID는 test LOT ID와 일치했다.
- 오류 메시지에 connection string 또는 DB secret이 노출되지 않았다.

## 4. Local Config 상태

`config/pilot.local.json`은 local-only ignored file이다.

확인:

```text
git check-ignore config/pilot.local.json -> config/pilot.local.json
git check-ignore .local/ -> .local/
git check-ignore appsettings.local.json -> appsettings.local.json
git check-ignore config/foo.local.json -> config/foo.local.json
git status --short -> empty before closeout edit
```

local file 내용은 실제 connection string 없이 env var name만 사용한다.

요약:

```text
profile=FakePlcLocal
plc=localhost:2004
dbMode=SqlServer
dbEnv=CAAH_WORKSTART_DB_CONNECTION_STRING
```

## 5. FakePlc localhost 실행

실행 전 확인:

```text
Test-NetConnection localhost -Port 2004
TcpTestSucceeded: False
```

FakePlc 실행 후:

```text
Test-NetConnection localhost -Port 2004
TcpTestSucceeded: True
```

관찰 후 FakePlc process는 종료했다.

## 6. WPF FakePlcLocal + SqlServer Observation

WPF 실행:

```text
dotnet run --project src\CAAutomationHub.Wpf\CAAutomationHub.Wpf.csproj
```

WPF process에는 다음 env var만 주입했다.

- `CAAH_WORKSTART_DB_CONNECTION_STRING=[REDACTED]`
- `CAAH_WORKSTART_TEST_LOT_ID=[REDACTED]`
- `CAAH_PILOT_CONFIG=config\pilot.local.json`

UI Automation 관찰:

```text
WindowFound=True
PollOnceInvoked=True
Header status: FakePlcLocal pilot polling profile loaded for localhost:2004 with SqlServer DB mode.
Request: WorkStart
Status: WorkStartProcessed
LOT ID: S0007652610B
Start Req: True
Complete Req: False
Start ACK: True
Complete ACK: -
Result: Succeeded
Error: None
Log: WorkStart processed.
```

의미:

- WPF 앱 실행 성공
- MainWindow 하단 PilotPollingView 표시 확인
- local config load 성공
- profile = `FakePlcLocal`
- db.mode = `SqlServer`
- `Poll Once` 실행 성공
- FakePlc read success
- LOT ID 표시
- SQL Server query result 반영
- WorkStart 처리 결과 표시
- Start ACK write가 FakePlc에 반영됨
- crash 없음

## 7. 실제 PLC / Write / Secret Commit 확인

- 실제 PLC host를 사용하지 않았다.
- 실제 PLC read/write를 수행하지 않았다.
- FakePlc 대상 write만 허용 범위 안에서 수행했다.
- connection string을 code/config/sample/closeout/commit에 기록하지 않았다.
- DB host/user/password를 closeout에 기록하지 않았다.
- `config/pilot.local.json`은 ignored local file이며 commit 대상이 아니다.
- 관찰 후 WPF/FakePlc process가 남아 있지 않음을 확인했다.

## 8. Tests 결과

Fresh validation:

```text
dotnet test tests\CAAutomationHub.PilotFlows.SqlServer.Tests\CAAutomationHub.PilotFlows.SqlServer.Tests.csproj
PASS: failed 0, passed 5, skipped 0, total 5

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

dotnet test tests\CAAutomationHub.PilotSmoke.Tests\CAAutomationHub.PilotSmoke.Tests.csproj
PASS: failed 0, passed 5, skipped 0, total 5

dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj
PASS: failed 0, passed 142, skipped 0, total 142
```

## 9. Build 결과

```text
dotnet build CAAutomationHub.sln
PASS: warning 0, error 0
```

## 10. Secret Scan 결과

명령:

```text
rg -n "Password=|Pwd=|User ID=|User Id=|Data Source=|Initial Catalog=|Server=|TrustServerCertificate|Encrypt=" src tests docs config tools -g "!**/bin/**" -g "!**/obj/**"
```

결과:

- actual connection string match 없음
- 실제 DB host/user/password match 없음
- historical docs 안의 scan command 문자열 외 secret value match 없음

추가 확인:

```text
actual DB token scan with private patterns: no matches
```

결과:

- actual connection string 없음
- closeout 문서에 실제 DB user/db name/host/password 평문 없음

## 11. Boundary Scan 결과

Boundary scan:

```text
rg -n "XgtChannelRunner|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON" src tests docs tools
```

결과:

- 기존 historical docs와 기존 Runtime/WPF test/source reference가 다수 match되었다.
- 이번 작업에서 RuntimeSnapshot / ChannelPollingResult / FlowExecutor / FLOW.JSON 관련 source 변경은 없다.

Project reference scan:

```text
rg -n "<ProjectReference|PackageReference" src tests tools -g "*.csproj"
```

관찰:

- `Microsoft.Data.SqlClient` package reference는 `src/CAAutomationHub.PilotFlows.SqlServer`에만 있다.
- WPF project는 `CAAutomationHub.PilotComposition`을 참조하지만 `Microsoft.Data.SqlClient` package를 직접 참조하지 않는다.
- WPF project는 `XgtDriverCore` 또는 FakePlc project를 직접 참조하지 않는다.
- Runtime / FlowDefinitions project에 SqlClient, XgtDriverCore, FakePlc reference를 추가하지 않았다.

## 12. 변경 파일 목록

Tracked:

- `docs/harness/AH-PILOT-DB-LIVE-02.md`
- `docs/harness/AH-PILOT-DB-LIVE.md`

Local-only ignored:

- `config/pilot.local.json`

## 13. 다음 후보

- NotFound / MultipleRows LOT ID를 별도로 준비해 WPF 표시 정책을 관찰한다.
- WorkComplete request map 시나리오에서 SqlServer profile + FakePlc write 흐름을 관찰한다.
- 실제 PLC는 별도 승인 전까지 계속 미사용 상태로 둔다.

## 14. Self-Check

판정: `ACCEPT`

근거:

- SQL Server env smoke가 skip 없이 통과했다.
- FakePlc `localhost:2004` listener와 WPF `FakePlcLocal + SqlServer` profile을 함께 관찰했다.
- WPF PilotPollingView에서 실제 DB 조회 기반 WorkStart `Succeeded` 흐름을 확인했다.
- write는 실제 PLC가 아니라 FakePlc에만 수행했다.
- secret value는 code/config/sample/closeout/commit에 남기지 않았다.
- Runtime / FlowDefinitions / WPF direct SqlClient boundary를 오염시키지 않았다.
