# AH-PILOT-LIVE-13 JSON-configured PLC Card + FakePlc Polling + Trend Binding

## 1. Summary

JSON/local config의 PLC target 표시 정보를 Pilot polling display boundary로 연결했다.

이번 변경은 상단 Runtime Dashboard mock card를 대체하지 않고, WPF Pilot Polling section 안에서 실제 Pilot source를 보는 PLC card/detail/trend 표시를 강화했다. `config/pilot.sample.json`에는 localhost 전용 display field를 추가했고, `PilotLocalComposition`은 config의 target id, display name, line name, host:port를 `PilotPollingOptions`로 전달한다. `PilotPollingService`는 이 값을 `PilotPlcCardStatus`에 보존하고, WPF `PilotPollingViewModel`과 `PilotPollingView.xaml`은 같은 snapshot에서 card/detail/trend를 표시한다.

RuntimeSnapshot, ChannelPollingResult, FlowDefinitions, Runtime DashboardSnapshot contract는 수정하지 않았다.

## 2. 변경 파일 목록

- `config/pilot.sample.json`
- `src/CAAutomationHub.PilotComposition/Configuration/PilotPlcTargetConfiguration.cs`
- `src/CAAutomationHub.PilotComposition/Polling/PilotLocalComposition.cs`
- `src/CAAutomationHub.PilotApp/Polling/PilotPollingOptions.cs`
- `src/CAAutomationHub.PilotApp/Polling/PilotPlcCardStatus.cs`
- `src/CAAutomationHub.PilotApp/Polling/PilotPollingService.cs`
- `src/CAAutomationHub.Wpf/ViewModels/Pilot/PilotPollingViewModel.cs`
- `src/CAAutomationHub.Wpf/Views/Pilot/PilotPollingView.xaml`
- `tests/CAAutomationHub.PilotComposition.Tests/Configuration/PilotLocalConfigurationLoaderTests.cs`
- `tests/CAAutomationHub.PilotComposition.Tests/Polling/PilotLocalCompositionTests.cs`
- `tests/CAAutomationHub.PilotApp.Tests/Polling/PilotPollingServiceTests.cs`
- `tests/CAAutomationHub.Wpf.Tests/ViewModels/PilotPollingViewModelTests.cs`
- `tests/CAAutomationHub.Wpf.Tests/Views/PilotPollingViewBindingTests.cs`
- `docs/harness/AH-PILOT-LIVE-13.md`

## 3. JSON/config 기반 PLC card mapping

`plc.displayName`과 `plc.lineName` optional field를 추가했다.

Sample config는 다음 localhost-only target을 유지한다.

- targetId: `fakeplc-local`
- displayName: `Fake PLC Local`
- lineName: `Local Test`
- host: `localhost`
- port: `2004`

displayName 또는 lineName이 없으면 `targetId` fallback을 사용한다. host:port는 `localhost:2004` 형태로 Pilot display snapshot에 전달된다.

## 4. WPF PLC card display 결과

Pilot Polling section의 PLC card는 같은 `PilotPlcCardStatus`에서 다음 값을 표시한다.

- Pilot PLC: displayName + targetId
- Line: lineName
- Target: host:port
- Connection: connection status
- Last Read: read result status

기존 Runtime Dashboard mock 5개 card는 대체하지 않았다. 이번 card는 Pilot section 내부의 live observation card다.

## 5. 상세정보 display 결과

상세정보 영역도 같은 `PilotPlcCardStatus`와 `PilotPollingSnapshot`에서 값을 읽는다.

- Detail PLC: displayName
- Detail Target: host:port
- Polling: current polling status
- Last Response: last result/read status
- Last Message: polling/workflow message
- Start/Complete request, ACK, result, error, scenario line

## 6. Polling trend 연결 결과

기존 Pilot trend collection을 유지하면서 WorkStart result duration을 trend point의 `DurationMs`에 반영했다. PollOnce마다 trend point가 추가되고, WPF ItemsControl은 `TrendPoints`에 binding된다.

Trend는 Pilot-specific list/bars 형태를 유지했다. 외부 chart package는 추가하지 않았다.

## 7. FakePlc connection / polling 관찰 결과

자동 하네스 기준으로 확인했다.

- `CAAutomationHub.PilotApp.Tests`의 in-process FakePlc polling integration은 start request ON, ACK ON, request OFF, ACK OFF, complete ACK ON/OFF cycle을 검증한다.
- `CAAutomationHub.PilotFlows.Xgt.Tests`는 FakePlc read/write/error/ACK path를 검증한다.
- `CAAutomationHub.PilotComposition.Tests`는 FakePlcLocal profile이 loopback target만 허용하고, no-listener 상태에서 `ReadFailed` snapshot을 반환하는 경로를 검증한다.

WPF GUI를 실제로 띄운 수동 관찰은 이번 세션에서 수행하지 않았다. WPF 관찰은 ViewModel/XAML binding harness로 대체했다.

## 8. WorkStart scenario 관찰 결과

WPF ViewModel harness에서 WorkStart processed 상태와 scenario line이 노출됨을 확인했다.

- `WorkStartProcessed`
- LOT ID 표시
- Start ACK True 표시
- scenario line: WorkStart + status + ACK state
- trend point 추가

## 9. ACK ON/OFF 관찰 결과

WPF ViewModel harness에서 request OFF 후 ACK OFF 관찰 path를 확인했다.

- 1회 PollOnce: `WorkStartProcessed`, Start ACK True
- 2회 PollOnce: `WorkStartAckOffWritten`, Start ACK False
- trend point 2개 유지

Backend FakePlc integration도 start ACK ON/OFF 및 complete ACK ON/OFF write를 검증한다.

## 10. 아직 부족한 UI / backend 항목

- 상단 Runtime Dashboard card를 pilot source로 대체하지 않았다.
- WPF GUI live manual observation은 수행하지 않았다.
- Pilot trend는 chart package 없이 lightweight list display다.
- TX/RX, cumulative error count 같은 통신 세부 metric은 아직 별도 field로 없다.

## 11. 테스트 결과

순차 실행 결과:

- `dotnet test tests/CAAutomationHub.PilotComposition.Tests/CAAutomationHub.PilotComposition.Tests.csproj --no-restore /p:UseSharedCompilation=false`: PASS, 16/16
- `dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj --no-restore /p:UseSharedCompilation=false`: PASS, 18/18
- `dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj --no-restore /p:UseSharedCompilation=false`: PASS, 234/234
- `dotnet test tests/CAAutomationHub.PilotFlows.Tests/CAAutomationHub.PilotFlows.Tests.csproj --no-restore /p:UseSharedCompilation=false`: PASS, 45/45
- `dotnet test tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj --no-restore /p:UseSharedCompilation=false`: PASS, 41/41
- `dotnet test tests/CAAutomationHub.PilotFlows.SqlServer.Tests/CAAutomationHub.PilotFlows.SqlServer.Tests.csproj --no-restore /p:UseSharedCompilation=false`: PASS, 4 passed / 1 skipped
- `dotnet test tests/CAAutomationHub.PilotSmoke.Tests/CAAutomationHub.PilotSmoke.Tests.csproj --no-restore /p:UseSharedCompilation=false`: PASS, 5/5
- `dotnet test tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj --no-restore /p:UseSharedCompilation=false`: PASS, 142/142

초기 RED 확인 중 병렬 `dotnet test` 실행으로 shared obj lock이 1회 발생했다. 이후 동일 범위를 순차 실행하고 PASS를 확인했다.

## 12. 빌드 결과

- `dotnet build CAAutomationHub.sln /p:UseSharedCompilation=false`: PASS, warnings 0, errors 0

## 13. Secret scan 결과

광역 secret scan은 기존 closeout 문서에 기록된 scan command 문자열만 hit했다. 이번 변경 파일 대상 좁힌 scan에서는 secret/connection-string pattern hit가 없었다.

`config/pilot.local.json`은 변경/스테이지하지 않았다.

## 14. Boundary scan 결과

광역 boundary scan은 기존 docs/context, docs/harness, Runtime source의 historical/contract hit가 다수 존재한다.

이번 변경 파일 대상 좁힌 scan에서는 `RuntimeSnapshot`, `ChannelPollingResult`, `FlowExecutor`, `FLOW.JSON`, `XgtChannelRunner` 금지 항목 hit가 없었다.

WPF direct reference scan 결과:

- WPF source/project에는 `Microsoft.Data.SqlClient`, `AutomationHub.XgtDriverCore`, `XgtChannelRunner`, FakePlc direct reference 추가 없음
- WPF test source에는 기존 Dashboard test method name의 FakePlc 문자열 1건만 존재

## 15. 다음 후보

- WPF GUI manual live observation script 또는 checklist 추가
- Pilot trend visual density 개선
- TX/RX/error-count metric을 Pilot-specific display boundary로 별도 확장
- 상단 Runtime Dashboard와 Pilot card 간 관계는 별도 Boundary Review 후 결정

## 16. Self-Check

판정: ACCEPT_WITH_CORRECTION

이유:

- ACCEPT 조건인 config-driven Pilot card/detail/trend binding, FakePlc polling harness, WorkStart ACK ON/OFF harness, test/build evidence, boundary preservation은 충족했다.
- 단, WPF GUI를 실제로 띄운 수동 live observation은 수행하지 못했으므로 correction note를 남긴다.
