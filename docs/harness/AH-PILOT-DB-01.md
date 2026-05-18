# AH-PILOT-DB-01 Closeout - SQL Server WorkStartDataQuery Boundary Review

## 1. Summary

AH-PILOT-DB-01은 `IWorkStartDataQuery`의 SQL Server concrete를 구현하기 전 project placement, secret handling, query policy, mapping boundary를 검토한 Boundary Review stage다.

이번 stage에서는 production DB concrete skeleton을 추가하지 않았다. sibling repo의 기존 `LotDataQueryService`와 query shape는 확인했지만, 실제 DB schema와 운영 query policy를 현재 repo에 확정 구현하기에는 아직 확인 필요 항목이 남아 있다.

판정은 `ACCEPT_WITH_CORRECTION`이다.

Correction 의미:

- SQL Server concrete의 project boundary는 확정 가능하다.
- connection string secret handling 원칙도 확정 가능하다.
- 단, `.gitignore`가 `appsettings.local.json`을 아직 ignore하지 않으므로 local appsettings 방식은 AH-PILOT-DB-02 전 보정이 필요하다.
- sibling repo에는 실제 connection string default가 존재하므로, 해당 값은 이 repo 코드/문서/commit으로 복사하지 않는다.

## 2. 확인한 source

현재 repo:

- `.gitignore`
- `src/CAAutomationHub.PilotFlows/WorkStart/IWorkStartDataQuery.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartDataQueryResult.cs`
- `src/CAAutomationHub.PilotFlows/WorkStart/WorkStartProcessData.cs`
- project reference graph

Sibling repo read-only:

- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\LotDataQueryService.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Models\PilotScenarioConfig.cs`
- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\Services\WorkStartPilotService.cs`

## 3. Boundary 결정

### 3.1 WorkStartDataQuery concrete 위치

권장 project:

```text
src/CAAutomationHub.PilotFlows.SqlServer
```

참조:

```text
CAAutomationHub.PilotFlows
Microsoft.Data.SqlClient
```

구현 후보:

```text
SqlServerWorkStartDataQuery : IWorkStartDataQuery
SqlServerWorkStartDataQueryOptions
```

이 결정의 이유:

- `CAAutomationHub.PilotFlows` core는 `IWorkStartDataQuery` contract만 유지해야 한다.
- `Microsoft.Data.SqlClient`는 vendor concrete project에만 둔다.
- `CAAutomationHub.PilotApp`, `CAAutomationHub.Wpf`, `CAAutomationHub.Runtime`은 SqlClient를 직접 참조하지 않는다.
- DB concrete는 Pilot business flow adapter이며 Runtime canonical state path에 들어가지 않는다.

### 3.2 금지 project reference

금지:

- `CAAutomationHub.Wpf -> Microsoft.Data.SqlClient`
- `CAAutomationHub.Runtime -> Microsoft.Data.SqlClient`
- `CAAutomationHub.PilotApp -> Microsoft.Data.SqlClient`
- `CAAutomationHub.PilotFlows -> Microsoft.Data.SqlClient`
- `CAAutomationHub.PilotFlows.SqlServer -> XgtDriverCore`
- `CAAutomationHub.PilotFlows.SqlServer -> FakePlc`
- `CAAutomationHub.PilotFlows.SqlServer -> XgtChannelRunner`

허용:

- `CAAutomationHub.PilotFlows.SqlServer -> CAAutomationHub.PilotFlows`
- `CAAutomationHub.PilotFlows.SqlServer -> Microsoft.Data.SqlClient`
- test project에서 mapping/guard 검증을 위한 SqlServer project reference

## 4. Secret Handling 결정

우선순위:

1. 환경변수
   - 후보 이름: `CAAH_WORKSTART_DB_CONNECTION_STRING`
   - AH-PILOT-DB-02에서 가장 빠르고 안전한 default
2. user-secrets
   - 개발자 로컬 환경에 적합
   - secrets project 기준은 SqlServer concrete project 또는 smoke/integration test project 중 하나로 명확히 고정 필요
3. `appsettings.local.json`
   - 현재 `.gitignore`에서 ignore되지 않음
   - 사용하려면 먼저 `.gitignore`에 local appsettings ignore rule을 추가해야 함

금지:

- 실제 connection string을 코드에 default로 넣기
- 실제 connection string을 기본 `appsettings.json`에 넣기
- 실제 connection string을 closeout 문서에 평문 기록하기
- sibling repo의 `PilotScenarioConfig.ConnectionString` default를 복사하기

이번 stage에서는 실제 connection string을 사용하지 않았고, 문서에도 기록하지 않았다.

## 5. Query / Mapping Review

현재 `IWorkStartDataQuery` contract:

```text
ValueTask<WorkStartDataQueryResult> QueryAsync(string lotId, CancellationToken cancellationToken = default)
```

현재 `WorkStartDataQueryResult` 상태:

- `Succeeded`
- `NotFound`
- `MultipleRows`
- `Failed`
- `Exception`

현재 `WorkStartProcessData` mapping target:

- `LotId`
- `Profile`
- `Tblr`
- `WinType`
- `CutSize`
- `Lr`
- `RollerYn`
- `RollerHolePos`
- `RollerHoleWidth`
- `RollerHoleLength`
- `RollerType`
- `CutDegree`

Sibling `LotDataQueryService`에서 확인한 query shape:

- parameter: `@LotId`
- row count 0: not found
- row count 2 이상: multiple rows
- data columns: `PROFILE`, `TBLR`, `WIN_TYPE`, `CUT_SIZE`, `LR`, `RollerYN`, `ROLLER_HOLE_POS`, `ROLLER_HOLE_WIDTH`, `ROLLER_HOLE_LENGTH`, `ROLLER_TYPE`, `CUT_DEGREE`
- null numeric value는 0으로 변환

AH-PILOT-DB-02에서 구현할 최소 mapping 후보:

- parameterized command만 사용한다.
- `SqlConnection`, `SqlCommand`, `SqlDataReader`는 SqlServer project 내부에만 둔다.
- `SqlServerWorkStartDataQueryOptions`는 `ConnectionString`, `SqlText`, `CommandTimeoutSeconds`를 받는다.
- `ConnectionString`과 `SqlText`는 required로 두고 코드 default를 두지 않는다.
- 0 row는 `WorkStartDataQueryResult.NotFound(lotId)`로 매핑한다.
- 2 rows 이상은 `WorkStartDataQueryResult.MultipleRows(lotId)`로 매핑한다.
- SqlClient exception은 `WorkStartDataQueryResult.DbException(lotId, sanitizedMessage)`로 매핑한다.
- 기타 mapping 실패는 `WorkStartDataQueryResult.Failed(lotId, sanitizedMessage)`로 매핑한다.

## 6. 왜 Skeleton을 보류했는가

이번 stage에서 skeleton을 만들지 않은 이유:

- query text를 어디에 둘지 아직 최종 확정하지 않았다.
- local appsettings 방식은 `.gitignore` 보정 전까지 사용하면 안 된다.
- sibling repo의 query는 참고 가능하지만 운영 DB schema 계약으로 확정하려면 사용자 확인이 필요하다.
- DB integration smoke는 환경변수 skip 방식으로 분리하는 것이 더 안전하다.

따라서 AH-PILOT-DB-01은 Boundary Review closeout으로 닫고, AH-PILOT-DB-02에서 최소 concrete 구현으로 넘어가는 것을 권장한다.

## 7. AH-PILOT-DB-02 최소 구현 후보

생성 후보:

```text
src/CAAutomationHub.PilotFlows.SqlServer/CAAutomationHub.PilotFlows.SqlServer.csproj
src/CAAutomationHub.PilotFlows.SqlServer/WorkStart/SqlServerWorkStartDataQuery.cs
src/CAAutomationHub.PilotFlows.SqlServer/WorkStart/SqlServerWorkStartDataQueryOptions.cs
tests/CAAutomationHub.PilotFlows.SqlServer.Tests/WorkStart/SqlServerWorkStartDataQueryTests.cs
```

테스트 후보:

- options가 null 또는 blank connection string이면 guard failure
- blank sql text면 guard failure
- blank LOT ID면 `Failed` 또는 argument policy 중 하나로 명확히 고정
- fake reader 없이 mapping helper를 분리할 경우 column mapping test
- 실제 DB integration test는 `CAAH_WORKSTART_DB_CONNECTION_STRING`이 없으면 skip

추가 보정 후보:

- `.gitignore`에 `appsettings.local.json` 또는 `appsettings.*.local.json` ignore rule 추가
- user-secrets project 기준 결정
- query text storage 위치 결정

## 8. Tests / Build 결과

이 stage는 docs-only Boundary Review이며 production code/test를 변경하지 않았다.

Stage 1에서 같은 working session에 실행한 fresh validation:

```text
PilotApp tests: passed 15, failed 0, skipped 0
WPF tests: passed 226, failed 0, skipped 0
PilotFlows tests: passed 45, failed 0, skipped 0
PilotFlows.Xgt tests: passed 41, failed 0, skipped 0
PilotComposition tests: passed 3, failed 0, skipped 0
Runtime tests: passed 142, failed 0, skipped 0
solution build: warnings 0, errors 0
```

DB-01 자체 추가 검증:

```text
rg -n "interface IWorkStartDataQuery|record WorkStartDataQueryResult|class WorkStartDataQueryResult|WorkStartDataQueryResult" src tests -g "*.cs"
rg -n "LotDataQueryService|WorkStartPilotService|IWorkStartDataQuery|ConnectionString|SqlConnection|SELECT|StoredProcedure" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore -g "*.cs" -g "!**/bin/**" -g "!**/obj/**"
rg -n "<ProjectReference|<PackageReference" src tests -g "*.csproj"
```

결과:

- `IWorkStartDataQuery`와 `WorkStartDataQueryResult`는 `CAAutomationHub.PilotFlows`에 있다.
- sibling repo의 `LotDataQueryService`는 `Microsoft.Data.SqlClient`를 직접 사용한다.
- sibling repo의 config에는 실제 connection string default가 있으므로 복사 금지 대상으로 확인했다.
- 현재 repo의 production WPF / PilotApp / Runtime / PilotFlows core는 SqlClient를 참조하지 않는다.

## 9. Boundary / Harness 영향

유지된 경계:

- Runtime shared execution path를 수정하지 않았다.
- RuntimeSnapshot / ChannelPollingResult를 수정하지 않았다.
- FlowDefinitions / FLOW.JSON / parser / executor를 수정하지 않았다.
- WPF가 DB concrete 또는 SqlClient를 직접 참조하지 않는다.
- PilotApp이 DB concrete 또는 SqlClient를 직접 참조하지 않는다.
- PilotFlows core가 DB concrete 또는 SqlClient를 직접 참조하지 않는다.
- XgtChannelRunner reference를 추가하지 않았다.
- WorkStartPilotService source copy를 하지 않았다.
- 실제 DB 접속을 수행하지 않았다.
- 실제 connection string을 코드/문서에 기록하지 않았다.

Harness 의미:

- AH-PILOT-DB-02의 DB integration smoke는 환경변수 또는 user-secrets가 있을 때만 실행하고, 없으면 skip되어야 한다.
- FakePlc / PLC harness와 DB query harness는 분리되어야 한다.
- DB concrete 구현은 Pilot business flow boundary 안에 머무르며 Runtime canonical state에 들어가지 않는다.

## 10. Self-Check

판정: `ACCEPT_WITH_CORRECTION`

근거:

- SQL Server concrete 위치와 forbidden references를 명확히 했다.
- secret handling 우선순위를 환경변수 / user-secrets / ignored local config로 정리했다.
- `.gitignore`가 local appsettings를 아직 보호하지 않는다는 보정 포인트를 확인했다.
- sibling repo의 실제 connection string default를 복사하지 않았다.
- 실제 DB connection string, DB host, credential을 기록하지 않았다.
- Runtime / WPF / Driver / FakePlc / Harness 경계를 변경하지 않았다.

남은 correction:

- AH-PILOT-DB-02 전 `appsettings.local.json` 사용 여부를 결정하고, 사용한다면 `.gitignore` 보정이 필요하다.
- 운영 query text source와 schema contract를 사용자 확인 후 concrete 구현해야 한다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
