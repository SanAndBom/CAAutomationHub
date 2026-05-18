# AH-PILOT-LIVE-14 Pilot Monitor Window and PLC Card Persistence

## 1. Summary

Pilot Polling UI를 `MainWindow` 하단에서 제거하고 별도 `PilotMonitorWindow`로 분리했다. `MainWindow`는 기존 Dashboard 중심 레이아웃을 회복하고, 헤더의 `Pilot Monitor 열기` 버튼으로 운영/관찰 창을 연다.

PLC Card edit persistence는 기존 `FakeDashboardRuntimeAdapter`가 in-memory `List<PlcDashboardConfiguration>`만 사용해 재시작 시 `CreateDefaultConfigurations()`로 초기화되는 것이 원인이었다. 이번 작업에서는 WPF UI card config 전용 JSON store skeleton을 추가하고, `MainWindowViewModel`의 Dashboard 구성은 `config/plc-cards.local.json` 경계에 저장 가능한 adapter를 사용하도록 연결했다.

RuntimeSnapshot, ChannelPollingResult, FlowDefinitions, FLOW.JSON, XgtChannelRunner, FakePlc map, 실제 PLC/DB 접속 경계는 변경하지 않았다.

## 2. PilotPollingView Window 분리 결과

- 추가: `src/CAAutomationHub.Wpf/Views/Pilot/PilotMonitorWindow.xaml`
- 추가: `src/CAAutomationHub.Wpf/Views/Pilot/PilotMonitorWindow.xaml.cs`
- `PilotMonitorWindow`는 기존 `PilotPollingView`를 그대로 host한다.
- `PilotMonitorWindow(PilotPollingViewModel viewModel)` 생성자는 기존 `MainWindowViewModel.PilotPolling` instance를 DataContext로 받는다.
- `MainWindow.xaml.cs`는 중복 window 생성을 방지하고, 이미 열린 window가 있으면 `Activate()`한다.

## 3. MainWindow 레이아웃 복원 결과

- `MainWindow.xaml` 하단 `PilotPollingView` row를 제거했다.
- `MainWindow` 본문은 `DashboardView` 단일 영역으로 복원했다.
- `DashboardView`는 `DataContext="{Binding Dashboard}"`로 `MainWindowViewModel.Dashboard`를 사용한다.
- 헤더에 `Pilot Monitor 열기` 버튼을 추가했다.

## 4. PilotMonitorWindow 동작 확인

- XAML binding test로 `PilotMonitorWindow`가 `PilotPollingView`를 host하고 별도 DataContext override 없이 window DataContext를 공유함을 확인했다.
- 기존 `PilotPollingView` command/detail/trend binding tests는 유지했고 통과했다.
- 실제 FakePlc 프로세스 실행 또는 WPF live screenshot/manual observation은 수행하지 않았다. 이번 검증은 WPF build와 ViewModel/XAML binding tests로 대체했다.

## 5. PLC Card Persistence 분석 결과

기존 상태:

- `FakeDashboardRuntimeAdapter`가 `_configurations` in-memory list를 생성한다.
- add/edit/delete command는 해당 list만 변경한다.
- 재시작 시 adapter가 새로 생성되며 default fake card list로 돌아간다.
- JSON load/save path가 없었다.

보정 결과:

- `IPlcDashboardConfigurationStore` 추가
- `JsonPlcDashboardConfigurationStore` 추가
- `DefaultPlcDashboardConfigurations`로 default fake card seed 분리
- `FakeDashboardRuntimeAdapter(IPlcDashboardConfigurationStore)` overload 추가
- add/edit/delete 후 store가 있으면 `Save(...)` 호출
- missing local file이면 default fake card list를 load fallback으로 사용하고, 파일은 edit/add/delete 시점에만 생성

## 6. JSON Persistence 구현 여부

구현함.

- sample: `config/plc-cards.sample.json`
- local target: `config/plc-cards.local.json`
- `.gitignore`에는 이미 `config/*.local.json`이 있어 local config는 commit 대상이 아니다.
- sample은 `localhost` fake endpoint만 포함하고 실제 현장 IP/DB secret을 포함하지 않는다.
- JSON schema는 별도 formal schema로 만들지 않았고, WPF card config skeleton DTO만 추가했다.

## 7. 변경 파일 목록

- `config/plc-cards.sample.json`
- `src/CAAutomationHub.Wpf/MainWindow.xaml`
- `src/CAAutomationHub.Wpf/MainWindow.xaml.cs`
- `src/CAAutomationHub.Wpf/Adapters/FakeDashboardRuntimeAdapter.cs`
- `src/CAAutomationHub.Wpf/ViewModels/MainWindowViewModel.cs`
- `src/CAAutomationHub.Wpf/Services/IPlcDashboardConfigurationStore.cs`
- `src/CAAutomationHub.Wpf/Services/DefaultPlcDashboardConfigurations.cs`
- `src/CAAutomationHub.Wpf/Services/JsonPlcDashboardConfigurationStore.cs`
- `src/CAAutomationHub.Wpf/Views/Pilot/PilotMonitorWindow.xaml`
- `src/CAAutomationHub.Wpf/Views/Pilot/PilotMonitorWindow.xaml.cs`
- `tests/CAAutomationHub.Wpf.Tests/Views/PilotPollingViewBindingTests.cs`
- `tests/CAAutomationHub.Wpf.Tests/Services/JsonPlcDashboardConfigurationStoreTests.cs`
- `docs/harness/AH-PILOT-LIVE-14.md`

## 8. 테스트 결과

- `dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj`
  - PASS: 240 passed, 0 failed
- `dotnet test tests/CAAutomationHub.PilotComposition.Tests/CAAutomationHub.PilotComposition.Tests.csproj`
  - PASS: 16 passed, 0 failed
- `dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj`
  - PASS: 18 passed, 0 failed
- `dotnet test tests/CAAutomationHub.PilotFlows.Tests/CAAutomationHub.PilotFlows.Tests.csproj`
  - PASS: 45 passed, 0 failed
- `dotnet test tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
  - PASS: 41 passed, 0 failed
- `dotnet test tests/CAAutomationHub.PilotFlows.SqlServer.Tests/CAAutomationHub.PilotFlows.SqlServer.Tests.csproj`
  - PASS: 4 passed, 1 skipped, 0 failed
- `dotnet test tests/CAAutomationHub.PilotSmoke.Tests/CAAutomationHub.PilotSmoke.Tests.csproj`
  - PASS: 5 passed, 0 failed
- `dotnet test tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj`
  - PASS: 142 passed, 0 failed

## 9. 빌드 결과

- `dotnet build CAAutomationHub.sln`
  - PASS: 0 warnings, 0 errors

## 10. Secret Scan 결과

명령:

```powershell
rg -n "Password=|Pwd=|User ID=|Data Source=|Initial Catalog=|Server=|TrustServerCertificate|Encrypt=|ca_erp|PlcFaDatabase" src tests docs config tools -g "!**/bin/**" -g "!**/obj/**"
```

결과:

- 기존 docs의 scan command 문자열 match
- 새 테스트의 `Assert.DoesNotContain("Password="...)`, `Assert.DoesNotContain("Pwd="...)` match
- 실제 connection string 또는 DB secret 값은 발견되지 않음

## 11. Boundary Scan 결과

명령:

```powershell
rg -n "XgtChannelRunner|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON" src tests docs tools
```

결과:

- historical docs와 기존 Runtime/Pilot boundary 문서에서 다수 match
- 이번 변경 source에는 RuntimeSnapshot / ChannelPollingResult / FlowExecutor / FLOW.JSON / XgtChannelRunner 추가 없음

Focused changed-file scan:

- changed source/config files에서는 `XgtChannelRunner`, `RuntimeSnapshot`, `ChannelPollingResult`, `FlowExecutor`, `FLOW.JSON`, `SqlConnection`, `Microsoft.Data.SqlClient`, `FakePlc`, `XgtDriverCore`, connection-string token 추가 없음
- 새 test file의 sample secret 부재 assertion 문자열만 match

## 12. Project Reference Scan 결과

명령:

```powershell
rg -n "<ProjectReference|PackageReference" src tests tools -g "*.csproj"
```

결과:

- WPF project reference는 기존과 동일하게 `Contracts`, `PilotApp`, `PilotComposition`, `Runtime`
- WPF project에 direct `XgtDriverCore`, `FakePlc`, `SqlClient` reference 추가 없음
- csproj 변경 없음

## 13. Manual Observation 여부

미수행.

이번 작업은 실제 PLC 접속/실제 DB 접속을 금지했고, WPF live window screenshot은 수행하지 않았다. 대신 XAML binding tests, ViewModel tests, WPF project tests, solution build로 PilotMonitorWindow host path와 Dashboard layout compile path를 검증했다.

## 14. 아직 남은 것

- 실제 WPF 실행 화면에서 `Pilot Monitor 열기` 버튼 위치와 창 크기 UX 관찰
- save failure user-facing message 정리
- 장기적으로 fake dashboard adapter와 persisted UI config service의 책임 분리 검토
- JSON formal schema 또는 migration policy는 아직 없음

## 15. 다음 후보

- AH-PILOT-LIVE-15: PilotMonitorWindow live UI observation and window UX polish
- AH-PILOT-LIVE-16: PLC card config save failure notification / local config diagnostics
- AH-PILOT-LIVE-17: Dashboard fake adapter와 persisted UI config service boundary 분리

## 16. Self-Check

- Harness: WPF/Pilot/Runtime tests와 Closeout 기록 완료
- Boundary: RuntimeSnapshot / ChannelPollingResult / FLOW.JSON / XgtChannelRunner / FakePlc map 미변경
- Validation: 필수 tests/build/scan 수행
- Local config: `config/plc-cards.local.json`는 `.gitignore` 대상이며 staging 대상 아님
- 판정: ACCEPT
