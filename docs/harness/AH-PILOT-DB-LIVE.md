# AH-PILOT-DB-LIVE Closeout - SQL Server backed Pilot local composition

## 1. Summary

AH-PILOT-DB-LIVE는 AH-PILOT-DB-02, AH-PILOT-DB-03, AH-PILOT-LIVE-06, AH-PILOT-LIVE-07을 한 묶음으로 진행한 Fast Track closeout이다.

변경 핵심:

- local secret 파일 보호를 위해 `.gitignore`에 `config/*.local.json`, `*.user.local.json` ignore rule을 추가했다.
- `CAAutomationHub.PilotFlows.SqlServer` project를 추가하고 `IWorkStartDataQuery` SQL Server concrete를 구현했다.
- `Microsoft.Data.SqlClient` package reference는 SQL Server concrete project에만 추가했다.
- `PilotComposition`에서 `db.mode = SqlServer`일 때 env var 기반 connection string을 해석해 SQL Server query concrete를 조립하도록 했다.
- `config/pilot.sample.json`은 actual value 없이 env var name만 담는 SqlServer profile sample로 보강했다.
- WPF는 계속 `PilotLocalComposition` / `IPilotPollingService` boundary만 사용하며 SqlClient를 직접 참조하지 않는다.

실제 DB connection string은 코드, 테스트, sample, closeout, commit 대상에 기록하지 않았다.

## 2. 변경 파일 목록

Production / configuration:

- `.gitignore`
- `CAAutomationHub.sln`
- `config/pilot.sample.json`
- `src/CAAutomationHub.PilotFlows.SqlServer/CAAutomationHub.PilotFlows.SqlServer.csproj`
- `src/CAAutomationHub.PilotFlows.SqlServer/Properties/AssemblyInfo.cs`
- `src/CAAutomationHub.PilotFlows.SqlServer/WorkStart/SqlServerWorkStartDataQuery.cs`
- `src/CAAutomationHub.PilotFlows.SqlServer/WorkStart/SqlServerWorkStartDataQueryMapper.cs`
- `src/CAAutomationHub.PilotFlows.SqlServer/WorkStart/SqlServerWorkStartDataQueryOptions.cs`
- `src/CAAutomationHub.PilotFlows.SqlServer/WorkStart/WorkStartSqlQueryText.cs`
- `src/CAAutomationHub.PilotComposition/CAAutomationHub.PilotComposition.csproj`
- `src/CAAutomationHub.PilotComposition/Configuration/PilotDatabaseConfiguration.cs`
- `src/CAAutomationHub.PilotComposition/Configuration/PilotDatabaseMode.cs`
- `src/CAAutomationHub.PilotComposition/Configuration/PilotLocalConfigurationLoader.cs`
- `src/CAAutomationHub.PilotComposition/Polling/PilotLocalComposition.cs`
- `src/CAAutomationHub.Wpf/ViewModels/MainWindowViewModel.cs`

Tests:

- `tests/CAAutomationHub.PilotFlows.SqlServer.Tests/CAAutomationHub.PilotFlows.SqlServer.Tests.csproj`
- `tests/CAAutomationHub.PilotFlows.SqlServer.Tests/WorkStart/SqlServerWorkStartDataQueryTests.cs`
- `tests/CAAutomationHub.PilotFlows.SqlServer.Tests/WorkStart/SqlServerWorkStartDataQuerySmokeTests.cs`
- `tests/CAAutomationHub.PilotComposition.Tests/Configuration/PilotLocalConfigurationLoaderTests.cs`
- `tests/CAAutomationHub.PilotComposition.Tests/Polling/PilotLocalCompositionTests.cs`

## 3. Gitignore / Local Config Guard

확인한 ignore 대상:

- `config/pilot.local.json`
- `config/*.local.json`
- `.local/`
- `appsettings.local.json`
- `*.user.local.json`

검증:

```text
git check-ignore config/pilot.local.json -> ignored
git check-ignore .local/pilot.local.json -> ignored
git check-ignore appsettings.local.json -> ignored
git check-ignore config/foo.local.json -> ignored
git check-ignore foo.user.local.json -> ignored
git ls-files local secret candidates -> no tracked files
```

## 4. SQL Server Concrete

추가 project:

```text
src/CAAutomationHub.PilotFlows.SqlServer
```

참조:

- `CAAutomationHub.PilotFlows`
- `Microsoft.Data.SqlClient`

구현:

- `SqlServerWorkStartDataQuery : IWorkStartDataQuery`
- `SqlServerWorkStartDataQueryOptions`
- `WorkStartSqlQueryText.Default`
- internal `SqlServerWorkStartDataQueryMapper`

동작:

- `@LotId` parameter만 사용한다.
- 0 row는 `WorkStartDataQueryResult.NotFound`로 매핑한다.
- 1 row는 `WorkStartDataQueryResult.Success(WorkStartProcessData)`로 매핑한다.
- 2 rows 이상은 `WorkStartDataQueryResult.MultipleRows`로 매핑한다.
- `SqlException`은 sanitized `DbException` result로 매핑한다.
- mapping/general failure는 sanitized `Failed` result로 매핑한다.

## 5. Query / Mapping 요약

Query는 지시문에 제공된 WorkDataList / profile spec / roller join 형태를 사용하며 `WHERE WDL.LotId = @LotId`로 LOT ID를 parameterized 처리한다.

매핑 컬럼:

- `PROFILE`
- `TBLR`
- `WIN_TYPE`
- `CUT_SIZE`
- `LR`
- `RollerYN`
- `ROLLER_HOLE_POS`
- `ROLLER_HOLE_WIDTH`
- `ROLLER_HOLE_LENGTH`
- `ROLLER_TYPE`
- `CUT_DEGREE`

정책:

- null string은 empty string으로 매핑한다.
- null numeric은 0으로 매핑한다.
- missing column은 mapping failure로 처리한다.
- 실제 connection string 또는 DB host/user/password는 error message에 넣지 않는다.

## 6. Env Var / Secret Handling

사용 env var name:

- `CAAH_WORKSTART_DB_CONNECTION_STRING`
- `CAAH_WORKSTART_TEST_LOT_ID`

현재 세션 상태:

```text
CAAH_WORKSTART_DB_CONNECTION_STRING present: False
CAAH_WORKSTART_TEST_LOT_ID present: False
```

따라서 실제 DB integration smoke와 WPF SqlServer observation은 skipped 처리했다. 값은 `[REDACTED]`로만 취급한다.

## 7. Integration Smoke 결과

테스트:

```text
SqlServerWorkStartDataQuerySmokeTests.QueryAsync_WithEnvironmentConfiguration_ReturnsTerminalStatus
```

결과:

```text
skipped 1
```

skip 사유:

- `CAAH_WORKSTART_DB_CONNECTION_STRING` 없음
- `CAAH_WORKSTART_TEST_LOT_ID` 없음

env var가 설정되면 test는 실제 SQL query를 수행하고 `Succeeded`, `NotFound`, `MultipleRows` 중 하나를 terminal status로 허용한다.

## 8. PilotComposition SqlServer Profile

`PilotDatabaseMode.SqlServer`를 추가했다.

`PilotLocalComposition.Create(configuration, getEnvironmentVariable)` overload를 추가해 tests에서는 env resolver를 주입하고, production 기본 경로에서는 `Environment.GetEnvironmentVariable`을 사용한다.

`FakePlcLocal + SqlServer` composition은 DB connection을 즉시 열지 않는다. connection string env var가 있으면 `SqlServerWorkStartDataQuery`를 조립하고, 실제 DB access는 `PollOnce`에서 WorkStart flow가 query를 실행할 때 발생한다.

env var가 없으면 configuration error로 safe failure가 발생하며 WPF 기본 생성 경로는 기존처럼 fake profile fallback을 사용한다.

## 9. WPF Observation 결과

WPF production code는 `MainWindowViewModel`의 fallback config property 이름만 새 config model에 맞게 조정했다.

현재 세션에서는 DB env var가 없어 FakePlcLocal + SqlServer + WPF manual observation은 수행하지 않았다.

WPF boundary 유지:

- WPF direct `Microsoft.Data.SqlClient` reference 없음
- WPF direct `XgtDriverCore` / FakePlc reference 없음
- WPF는 계속 `PilotComposition` / `PilotApp` boundary만 사용

판정은 `ACCEPT_WITH_CORRECTION`이다. Correction은 구현 결함이 아니라 실제 DB env var와 test LOT ID가 없는 세션 조건 때문에 live observation이 skip된 것이다.

## 10. Tests 결과

Fresh validation:

```text
dotnet test tests/CAAutomationHub.PilotFlows.SqlServer.Tests/CAAutomationHub.PilotFlows.SqlServer.Tests.csproj
PASS: failed 0, passed 4, skipped 1, total 5

dotnet test tests/CAAutomationHub.PilotComposition.Tests/CAAutomationHub.PilotComposition.Tests.csproj
PASS: failed 0, passed 15, skipped 0, total 15

dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj --no-build
PASS: failed 0, passed 16, skipped 0, total 16

dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj --no-build
PASS: failed 0, passed 228, skipped 0, total 228

dotnet test tests/CAAutomationHub.PilotFlows.Tests/CAAutomationHub.PilotFlows.Tests.csproj --no-build
PASS: failed 0, passed 45, skipped 0, total 45

dotnet test tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj --no-build
PASS: failed 0, passed 41, skipped 0, total 41

dotnet test tests/CAAutomationHub.PilotSmoke.Tests/CAAutomationHub.PilotSmoke.Tests.csproj --no-build
PASS: failed 0, passed 5, skipped 0, total 5

dotnet test tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj --no-build
PASS: failed 0, passed 142, skipped 0, total 142
```

## 11. Build 결과

```text
dotnet build CAAutomationHub.sln
PASS: warning 0, error 0
```

## 12. Secret Scan 결과

명령:

```text
rg -n "Password=|Pwd=|User ID=|User Id=|Data Source=|Initial Catalog=|Server=|TrustServerCertificate|Encrypt=" src tests docs config tools -g "!**/bin/**" -g "!**/obj/**"
```

결과:

- 새 production/test/config 파일에 actual connection string 없음
- sample json에는 env var name만 있음
- scan match는 historical doc에 기록된 scan command 문자열 2건뿐임

## 13. Boundary Scan 결과

Project reference scan:

- `Microsoft.Data.SqlClient` package reference는 `src/CAAutomationHub.PilotFlows.SqlServer`에만 존재한다.
- `PilotComposition`은 `CAAutomationHub.PilotFlows.SqlServer` project를 참조한다.
- WPF는 SqlServer project 또는 SqlClient package를 직접 참조하지 않는다.
- Runtime / FlowDefinitions / PilotApp / PilotFlows core에는 SqlClient direct reference가 없다.

Boundary 유지:

- `RuntimeSnapshot` 수정 없음
- `ChannelPollingResult` 수정 없음
- `FlowDefinitions` 수정 없음
- `FLOW.JSON` / parser / executor 구현 없음
- `XgtChannelRunner` reference 추가 없음
- `WorkStartPilotService` source copy 없음
- 실제 PLC write 없음
- ACK/error write를 실제 PLC 대상으로 실행하지 않음

## 14. 다음 후보

- env var가 설정된 세션에서 AH-PILOT-DB-03 실제 SQL smoke 실행
- FakePlc `localhost:2004` 실행 후 WPF `FakePlcLocal + SqlServer` manual observation
- Success / NotFound / MultipleRows별 WPF 표시 및 FakePlc write 결과 관찰
- 실제 PLC는 별도 승인 전까지 read/write 모두 보류

## 15. Self-Check

판정: `ACCEPT_WITH_CORRECTION`

근거:

- SQL Server concrete와 config-driven composition은 구현 및 tests/build로 검증했다.
- local secret guard와 secret scan을 통과했다.
- WPF / Runtime / FlowDefinitions / ChannelPollingResult boundary를 오염시키지 않았다.
- 실제 DB env var와 test LOT ID가 없어 integration smoke와 WPF SqlServer live observation은 skip되었다.

Correction:

- `CAAH_WORKSTART_DB_CONNECTION_STRING`과 `CAAH_WORKSTART_TEST_LOT_ID`가 설정된 별도 세션에서 DB smoke 및 WPF live observation을 수행해야 `ACCEPT`로 올릴 수 있다.
