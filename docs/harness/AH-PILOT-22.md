# AH-PILOT-22 Closeout - PilotApp FakePlc WorkStart Transaction Harness

## 1. Summary

AH-PILOT-22는 WPF가 호출하게 될 `IWorkStartExecutionService.ExecuteOnceAsync` application service 경로가 FakePlc 기반 full WorkStart transaction까지 관통하는지 검증한 stage다.

새 production code는 추가하지 않았다. `tests/CAAutomationHub.PilotApp.Tests`에 integration-style FakePlc harness를 추가하고, test project에만 `CAAutomationHub.PilotFlows.Xgt`와 `AutomationHub.XgtDriverCore.FakePlc` reference를 추가했다.

검증된 경로:

```text
WorkStartExecutionService
  -> WorkStartFlowServiceRunner
  -> WorkStartFlowService
  -> WorkStartXgtPlcOperations
  -> in-process FakePlc
  -> test-local fake DB query
  -> WorkStartExecutionResult
```

판정은 `ACCEPT`다. RuntimeSnapshot, ChannelPollingResult, DashboardSnapshot, WPF, Runtime, FlowDefinitions, production PilotApp code는 수정하지 않았다.

## 2. 변경 파일 목록

- `tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj`
  - PilotApp test project에만 `CAAutomationHub.PilotFlows.Xgt`와 `AutomationHub.XgtDriverCore.FakePlc` project reference를 추가했다.
- `tests/CAAutomationHub.PilotApp.Tests/WorkStart/WorkStartExecutionServiceFakePlcIntegrationTests.cs`
  - PilotApp-level full WorkStart FakePlc transaction harness를 추가했다.
- `docs/harness/AH-PILOT-22.md`
  - 본 closeout 문서다.

## 3. PilotApp-level FakePlc Transaction 방식

테스트는 in-process `TcpListener`를 열고 `FakePlcProtocolHandler.HandleClientAsync`를 사용해 FakePlc TCP endpoint를 구성한다.

`WorkStartXgtPlcOperations`는 실제 `XgtSession` / `TcpTransport`를 통해 FakePlc에 연결한다. DB는 production concrete가 아니라 test-local `FakeWorkStartDataQuery`를 사용한다.

## 4. WorkStartExecutionService 경로 검증

추가 테스트:

- `ExecuteOnceAsync_ReturnsSuccess_WhenFakePlcWorkStartTransactionSucceeds`
- `ExecuteOnceAsync_ReturnsFailureDisplayResult_WhenFakeDbNotFound`

검증 내용:

- `IWorkStartExecutionService.ExecuteOnceAsync` 호출
- FakePlc read block에서 LOT ID `S0007652610B` 추출
- fake DB query 호출 및 queried LOT ID 기록
- process payload write
- ACK write
- DB not found failure에서 error code write
- `WorkStartExecutionResult`의 WPF-friendly display fields 매핑 확인

## 5. WorkStartExecutionResult 검증

Happy path 결과:

- `Succeeded == true`
- `Status == "Succeeded"`
- `Step == "completed"`
- `ErrorCode == 0`
- `ErrorCodeName == "None"`
- `SelectedLotId == "S0007652610B"`
- `ErrorWriteExpected == false`
- `Duration >= TimeSpan.Zero`

DB not found 결과:

- `Succeeded == false`
- `Status == "Failed"`
- `Step == "db-query"`
- `ErrorCode == 2301`
- `ErrorCodeName == "DbNotFound"`
- `SelectedLotId == "S0007652610B"`
- `ErrorWriteExpected == true`

## 6. FakePlc Memory / Records 검증

Happy path:

- `runtime.LastBulkWrite == expectedPayload`
- `Db11000` memory에 process payload 반영
- `runtime.LastAckValue == 1`
- `Db11416 == 01 00`
- `runtime.LastErrorCode == null`
- `Db11410 == 00 00`

DB not found path:

- `runtime.LastErrorCode == 2301`
- `Db11410 == FD 08`
- bulk payload write 없음
- ACK write 없음

## 7. Runtime / WPF 비오염 확인

이번 stage에서 수정한 production project는 없다.

비오염 확인:

- Runtime project 수정 없음
- RuntimeSnapshot 수정 없음
- ChannelPollingResult 수정 없음
- DashboardSnapshot 수정 없음
- WPF project 수정 없음
- PilotApp production code 수정 없음
- FlowDefinitions project 수정 없음
- FakePlc reference는 test project에만 추가됨
- Microsoft.Data.SqlClient package/reference 추가 없음
- actual DB query 구현 없음
- actual PLC read/write 테스트 없음
- DI wiring / XAML / App.xaml 수정 없음

## 8. 테스트 결과

실행:

```text
dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj
```

결과:

```text
pass, failed 0, passed 8, skipped 0
```

실행:

```text
dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj
dotnet test tests/CAAutomationHub.PilotFlows.Tests/CAAutomationHub.PilotFlows.Tests.csproj
dotnet test tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj
dotnet test tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj
```

결과:

```text
WPF tests: pass, failed 0, passed 219, skipped 0
PilotFlows tests: pass, failed 0, passed 40, skipped 0
PilotFlows.Xgt tests: pass, failed 0, passed 39, skipped 0
Runtime tests: pass, failed 0, passed 142, skipped 0
```

## 9. 빌드 결과

실행:

```text
dotnet build CAAutomationHub.sln
```

결과:

```text
build passed
warnings 0
errors 0
```

## 10. Boundary Scan 결과

실행:

```text
git diff --check
```

결과:

```text
exit code 0
whitespace error 없음
```

참고:

```text
warning: in the working copy of 'tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj', LF will be replaced by CRLF the next time Git touches it
```

이는 Git line ending warning이며 whitespace error는 아니다.

실행:

```text
rg -n "XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src tests -g '!bin/**' -g '!obj/**'
```

결과:

- 기존 Runtime / WPF RuntimeSnapshot, ChannelPollingResult, dashboard settings JSON hit 존재
- AH-PILOT-22 신규 test file에서 RuntimeSnapshot / ChannelPollingResult / FlowExecutor / FLOW.JSON / Json / JSON hit 없음
- SqlConnection / Microsoft.Data.SqlClient / XgtChannelRunner 신규 hit 없음

실행:

```text
rg -n "FakePlc|XgtDriverCore|PilotFlows.Xgt|Microsoft.Data.SqlClient|SqlConnection|XgtChannelRunner" src tests -g "*.csproj"
```

결과:

- `tests/CAAutomationHub.PilotApp.Tests`와 기존 `tests/CAAutomationHub.PilotFlows.Xgt.Tests`에만 FakePlc test reference 존재
- `src/CAAutomationHub.PilotFlows.Xgt`만 `AutomationHub.XgtDriverCore`를 참조
- WPF project에는 PilotFlows.Xgt / XgtDriverCore / FakePlc / SqlClient reference 없음

Project reference scan:

```text
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
```

결과:

- PilotApp production project는 기존처럼 `CAAutomationHub.PilotFlows`만 참조
- PilotApp test project에만 `CAAutomationHub.PilotFlows.Xgt` / `AutomationHub.XgtDriverCore.FakePlc` reference 추가
- Runtime project reference 변경 없음
- FlowDefinitions project reference 변경 없음
- Microsoft.Data.SqlClient package 추가 없음

## 11. 다음 후보

다음 stage는 AH-PILOT-WPF-07이다.

후보:

- `WorkStartPilotViewModel`에 최소 command boundary 추가
- XAML binding 없이 fake service 기반 command behavior 테스트
- busy 중 duplicate command 방지 정책 고정

후속 별도 후보:

- PilotApp-level FakePlc failure scenario 추가 확장
- command boundary 이후 실제 UI wiring boundary review
- DI composition boundary review

## 12. Self-Check

판정: `ACCEPT`

근거:

- PilotApp-level full WorkStart FakePlc transaction evidence를 추가했다.
- DB not found failure display result와 error write evidence까지 포함했다.
- production PilotApp code 변경 없이 application service 경로를 검증했다.
- FakePlc / XGT concrete reference는 test project에만 추가했다.
- RuntimeSnapshot / ChannelPollingResult / DashboardSnapshot / Runtime / WPF / FlowDefinitions를 수정하지 않았다.
- 지시된 tests, solution build, diff check, boundary scan, project reference scan을 실행했다.
- ContextPublisher 자동 publish는 재도입하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
