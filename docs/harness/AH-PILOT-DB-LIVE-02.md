# AH-PILOT-DB-LIVE-02 Closeout - SqlServer env smoke and WPF observation gate

## 1. Summary

AH-PILOT-DB-LIVE-02는 `AH-PILOT-DB-LIVE` commit `41179d4` 이후 실제 SQL Server DB와 FakePlcLocal WPF PilotPollingView를 함께 관찰하기 위한 실행 시도다.

이번 세션에서는 secret policy와 local-only config guard를 먼저 확인했고, `config/pilot.local.json`은 ignored local file로 `FakePlcLocal + SqlServer` profile 형태에 맞췄다.

단, Codex가 실행하는 PowerShell 환경에 `CAAH_WORKSTART_DB_CONNECTION_STRING`과 `CAAH_WORKSTART_TEST_LOT_ID`가 모두 없었기 때문에 실제 SQL Server smoke는 skip되었고, WPF FakePlcLocal + SqlServer live observation은 진행하지 않았다. 실제 DB connection string, DB host, DB user, password는 파일, 문서, commit, 출력에 남기지 않았다.

판정은 `PARTIAL`이다. 구현 결함이 아니라 live observation precondition인 env var 부재로 실제 DB query와 WPF 화면 관찰을 완료하지 못했다.

## 2. Env Var / Secret Handling

사용 env var name:

- `CAAH_WORKSTART_DB_CONNECTION_STRING`
- `CAAH_WORKSTART_TEST_LOT_ID`

현재 세션 확인:

```text
CAAH_WORKSTART_DB_CONNECTION_STRING present: False
CAAH_WORKSTART_TEST_LOT_ID present: False
```

값은 모두 `[REDACTED]`로 취급한다.

## 3. DB Smoke 결과

실행:

```text
dotnet test tests\CAAutomationHub.PilotFlows.SqlServer.Tests\CAAutomationHub.PilotFlows.SqlServer.Tests.csproj
```

결과:

```text
PASS: failed 0, passed 4, skipped 1, total 5
```

관찰:

- `SqlServerWorkStartDataQuerySmokeTests.QueryAsync_WithEnvironmentConfiguration_ReturnsTerminalStatus`는 skip되었다.
- skip 사유는 `CAAH_WORKSTART_DB_CONNECTION_STRING` 또는 `CAAH_WORKSTART_TEST_LOT_ID` env var 부재다.
- 실제 DB query result status, mapped fields sanity, SQL exception path는 이번 세션에서 관찰하지 못했다.

## 4. Local Config 상태

`config/pilot.local.json`은 local-only ignored file이다.

확인:

```text
git check-ignore config/pilot.local.json -> config/pilot.local.json
git check-ignore .local/ -> .local/
git check-ignore appsettings.local.json -> appsettings.local.json
git check-ignore config/foo.local.json -> config/foo.local.json
git status --short -> empty
```

local file 내용은 실제 connection string 없이 env var name만 사용한다.

요약:

```text
profile=FakePlcLocal
plc=localhost:2004
dbMode=SqlServer
dbEnv=CAAH_WORKSTART_DB_CONNECTION_STRING
```

## 5. FakePlc localhost 상태

실행 전 확인:

```text
Test-NetConnection localhost -Port 2004
TcpTestSucceeded: False
```

실제 DB env var가 없어 WPF SqlServer observation으로 이어질 수 없으므로 FakePlc process는 실행하지 않았다.

종료 확인:

```text
FakePlc process: none
WPF process: none
```

## 6. WPF FakePlcLocal + SqlServer Observation

이번 세션에서는 수행하지 않았다.

사유:

- 실제 DB connection string env var가 없으면 WPF `FakePlcLocal + SqlServer` profile은 실제 DB 조회 기반 PilotPolling 상태를 만들 수 없다.
- 이 상태에서 WPF를 실행하면 목적한 live observation이 아니라 safe fallback 또는 config failure 관찰이 되어 AH-PILOT-DB-LIVE-02의 증거로 적합하지 않다.

미관찰 항목:

- WPF 화면의 실제 DB 조회 기반 LOT ID / status / WorkStart 처리 결과
- SQL Server query result 반영
- FakePlc 대상 ACK / payload write 결과

## 7. 실제 PLC / Write / Secret Commit 확인

- 실제 PLC host를 사용하지 않았다.
- 실제 PLC read/write를 수행하지 않았다.
- FakePlc write도 이번 세션에서는 수행하지 않았다.
- connection string을 code/config/sample/closeout/commit에 기록하지 않았다.
- `config/pilot.local.json`은 ignored local file이며 commit 대상이 아니다.

## 8. Tests 결과

Fresh validation:

```text
dotnet test tests\CAAutomationHub.PilotFlows.SqlServer.Tests\CAAutomationHub.PilotFlows.SqlServer.Tests.csproj
PASS: failed 0, passed 4, skipped 1, total 5

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
rg -n "Password=|Pwd=|User ID=|Data Source=|Initial Catalog=|Server=|TrustServerCertificate|Encrypt=|ca_erp|PlcFaDatabase" src tests docs config tools -g "!**/bin/**" -g "!**/obj/**"
```

결과:

- actual connection string match 없음
- 실제 DB host/user/password match 없음
- match는 historical docs 안의 scan command 문자열뿐이다.

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

Local-only ignored:

- `config/pilot.local.json`

## 13. 다음 후보

- 같은 PowerShell 프로세스 안에서 `CAAH_WORKSTART_DB_CONNECTION_STRING`과 `CAAH_WORKSTART_TEST_LOT_ID`를 설정하고 SQL Server smoke를 다시 실행한다.
- smoke가 skip 없이 실행되면 FakePlc를 `localhost:2004` 경로에서 실행하고 WPF `FakePlcLocal + SqlServer` profile을 관찰한다.
- WPF 화면에서 LOT ID, DB query status, WorkStart result, FakePlc write result를 기록한다.
- 실제 PLC는 별도 승인 전까지 계속 미사용 상태로 둔다.

## 14. Self-Check

판정: `PARTIAL`

근거:

- secret hygiene, ignored local config guard, tests/build, boundary/reference scan은 확인했다.
- 실제 SQL Server env var가 없어 DB smoke는 skip되었다.
- 실제 DB 조회 기반 WPF PilotPollingView 관찰은 수행하지 않았다.
- 실제 PLC write 가능성은 발생하지 않았다.

`ACCEPT`로 올리려면 env var가 설정된 세션에서 SQL Server smoke가 skip 없이 실행되고, FakePlcLocal + SqlServer WPF 화면 관찰까지 완료되어야 한다.
