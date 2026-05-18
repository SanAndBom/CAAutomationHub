# AH-PILOT-LIVE-01~04 Closeout

## 1. Summary

AH-PILOT-LIVE fast track은 WPF에서 Pilot polling 상태를 볼 수 있도록 local config boundary, config-based composition, WPF shell wiring, smoke/audit evidence를 한 묶음으로 진행했다.

변경 핵심:

- `PilotComposition.Configuration`에 local JSON config model / loader / validation을 추가했다.
- `config/pilot.sample.json`을 추가하고, 실제 local config 경로는 `.gitignore`에 등록했다.
- `PilotComposition.Polling`에 `Fake` / `FakePlcLocal` profile composition을 추가했다.
- `PilotFlows.Xgt.Polling`에 XGT session 생성 책임을 둔 operation factory를 추가해 `PilotComposition`이 XGT transport type을 직접 만지지 않게 했다.
- `MainWindow` 하단에 `PilotPollingView`를 연결하고, WPF는 `PilotComposition`을 통해 `IPilotPollingService`만 받도록 했다.
- FakePlc 미실행 또는 연결 실패 시 `PollOnceAsync`가 crash하지 않고 failed snapshot을 publish하도록 보강했다.

## 2. 구현한 config 구조

추가 구조:

- `PilotLocalConfiguration`
- `PilotPlcTargetConfiguration`
- `PilotDatabaseConfiguration`
- `PilotProfileKind`
- `PilotDatabaseMode`
- `PilotLocalConfigurationLoader`

Sample:

- `config/pilot.sample.json`

Sample은 `FakePlcLocal`, `localhost:2004`, `%DB10000`, read word count 90, 착공/완공 signal index 83/84, LOT ID offset/length, fake DB mode, DB env var name만 포함한다.

## 3. local / secret handling

Gitignored local config:

- `config/pilot.local.json`
- `.local/`
- `appsettings.local.json`

실제 DB 값은 config model에 저장하지 않는다. DB 설정은 env var name만 보존한다.

WPF startup lookup 순서:

1. `CAAH_PILOT_CONFIG` env var path
2. `config/pilot.local.json`
3. `.local/pilot.local.json`
4. `appsettings.local.json`
5. 없거나 실패하면 Fake profile fallback

실제 DB 접속은 구현하지 않았다.

## 4. composition 구조

`PilotLocalComposition.Create(...)`:

- `Fake`: in-memory observable polling service를 만든다.
- `FakePlcLocal`: loopback host만 허용하고 XGT adapter + fake DB query + existing Pilot polling/ACK services를 조립한다.
- `RealReadOnly`: fast track에서는 `NotSupportedException`으로 차단한다.
- `RealPilot`: fast track에서는 `NotSupportedException`으로 차단한다.

XGT session / transport 생성은 `CAAutomationHub.PilotFlows.Xgt.Polling.XgtPilotPollingOperationsFactory`로 이동했다.

## 5. WPF wiring 범위

`MainWindow.xaml`:

- 기존 Dashboard는 유지했다.
- 하단에 `PilotPollingView`를 추가했다.
- `PilotPollingView.DataContext`는 `MainWindowViewModel.PilotPolling`에 바인딩한다.

`MainWindowViewModel.CreateDefaultPilotLocal(...)`:

- local config를 읽어 `PilotLocalComposition`을 생성한다.
- 실패하면 fake profile로 안전하게 fallback한다.
- status message를 header 우측에 표시한다.

WPF는 `XgtDriverCore`, FakePlc, SqlClient를 직접 참조하지 않는다.

## 6. 실행 관찰 결과

관찰 가능 항목:

- config load: `PilotLocalConfigurationLoaderTests`
- service composition: `PilotLocalCompositionTests`
- PollOnce fake profile: `CreatePollingService_WithFakeProfile_ReturnsObservablePollingService`
- FakePlc 미실행 안전 실패: `PollOnce_WithFakePlcLocalProfileAndNoListener_ReturnsFailedSnapshot`
- WPF shell binding: `MainWindow_ContainsPilotPollingViewBoundToPilotPollingViewModel`
- ViewModel display binding: `PilotPollingViewModelTests`

Manual WPF app run은 실행하지 않았다. 이번 closeout의 실행 관찰은 WPF ViewModel/test host 기반 smoke로 대체했다.

## 7. FakePlc localhost 실행 여부

`Test-NetConnection -ComputerName localhost -Port 2004` 결과:

- `TcpTestSucceeded`: `False`

즉, 이 세션에서 localhost:2004 FakePlc는 실행 중이 아니었다.

## 8. 테스트 결과

실행 결과:

- `dotnet test tests/CAAutomationHub.PilotComposition.Tests/CAAutomationHub.PilotComposition.Tests.csproj`: PASS, 13 passed
- `dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj`: PASS, 16 passed
- `dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj`: PASS, 228 passed
- `dotnet test tests/CAAutomationHub.PilotFlows.Tests/CAAutomationHub.PilotFlows.Tests.csproj`: PASS, 45 passed
- `dotnet test tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj`: PASS, 41 passed
- `dotnet test tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj`: PASS, 142 passed
- `dotnet test tests/CAAutomationHub.PilotSmoke.Tests/CAAutomationHub.PilotSmoke.Tests.csproj`: PASS, 5 passed

## 9. 빌드 결과

`dotnet build CAAutomationHub.sln`: PASS

- warning 0
- error 0

`git diff --check`: PASS

## 10. boundary scan 결과

Required boundary scan은 repository 전체에 대해 실행했다. 기존 docs/context/docs/harness historical records에 `RuntimeSnapshot`, `ChannelPollingResult`, `FLOW.JSON`, `XgtChannelRunner` 언급이 다수 존재한다.

변경 파일 기준 추가 확인:

- `RuntimeSnapshot`: new/changed production file match 없음
- `ChannelPollingResult`: new/changed production file match 없음
- `XgtChannelRunner`: new/changed file match 없음
- `FlowExecutor`: new/changed file match 없음
- `FLOW.JSON`: new/changed file match 없음

RuntimeSnapshot / ChannelPollingResult는 수정하지 않았다.

## 11. secret scan 결과

Required secret-pattern scan은 repository 전체에 대해 실행했다. 기존 historical docs와 기존 test assertion의 일반 용어 match가 있었다.

변경 파일 기준 추가 확인:

- actual DB value 없음
- actual DB host 없음
- actual user/password 없음
- tracked sample에는 env var name만 있음
- local config path는 gitignored

## 12. 남은 것

- 실제 WPF 창을 띄운 manual observation
- localhost:2004 FakePlc 실행 상태에서 sample config 기반 `PollOnce`
- RealReadOnly profile 정책 확정
- 실제 PLC read-only 승인 절차
- 실제 DB query concrete 구현 여부와 위치 결정

## 13. 실제 PLC / DB 다음 후보

다음 후보:

- `RealReadOnlyProfile` 전용 config validation
- read-only PLC 접속 승인 token 또는 explicit command gate
- DB env var resolution boundary
- DB query concrete는 `PilotComposition` 아래 별도 adapter 또는 후속 infrastructure project 후보로 검토

금지 유지:

- 실제 PLC write
- 실제 DB secret commit
- WPF direct XGT/FakePlc/SqlClient reference
- RuntimeSnapshot / ChannelPollingResult pollution

## 14. Self-Check

판정: ACCEPT_WITH_CORRECTION

근거:

- Config / composition / WPF ViewModel observation path / safety failure path / tests / build는 통과했다.
- FakePlc localhost:2004는 실행 중이 아니었다.
- Manual WPF app run은 수행하지 않았고, WPF ViewModel/test host smoke로 대체했다.
- 실제 PLC / DB write/read는 수행하지 않았다.
