# AH-PILOT-DAY1 WPF start-complete polling pilot closeout

## 1. Summary

WPF에서 착공/완공 Polling 흐름을 확인할 수 있도록 PilotApp-facing polling 상태 모델, polling service, isolated WPF shell, FakePlc smoke harness를 추가했다.

이번 변경은 Runtime shared execution path를 수정하지 않고 Pilot business flow boundary 안에서 수행했다. WPF는 새 `PilotPollingViewModel`을 통해 `IPilotPollingService`만 바라보며, XGT/FakePlc/SqlClient concrete를 직접 참조하지 않는다.

### AH-PILOT-25~27 이후 보정 반영

작업 시작 시점의 최신 Pilot anchor는 다음으로 확인했다.

- AH-PILOT-25 / `38545b9`: WorkStart ACK OFF FakePlc harness
- AH-PILOT-26 / `4dcd1bb`: WorkComplete ACK ON/OFF FakePlc harness
- AH-PILOT-27 / `9b79fd8`: Pilot FakePlc full cycle audit

따라서 DAY1에서는 위 자산을 재구현하지 않았다. 새로 추가한 범위는 PilotApp/WPF-facing polling 상태 layer, PollOnce orchestration adapter, isolated WPF 표시/명령 shell, 그리고 기존 harness 자산을 실제 polling service 경유로 연결하는 smoke test다.

## 2. 구현한 stage 목록

- Stage 1: `PilotPollingSnapshot`, `PilotPollingStatus`, `WorkRequestKind`, `PilotPollingLogEntry` 추가
- Stage 2: AH-PILOT-26의 기존 `WorkCompleteAckService` / XGT complete ACK adapter를 polling service에서 재사용
- Stage 3: AH-PILOT-25의 기존 `WorkStartAckOffService` / XGT test-specific ACK OFF option을 polling service에서 재사용
- Stage 4: `IPilotPollingService`, `PilotPollingService`, `PilotPollingFlowPort`, `PilotPollingRequestStateReader` 추가
- Stage 5: `PilotPollingViewModel` 추가
- Stage 6: isolated `PilotPollingView` shell 추가
- Stage 7: AH-PILOT-27 audit 결과를 중복 작성하지 않고, 기존 FakePlc 자산을 `PilotPollingService` 경유로 연결하는 PollOnce smoke harness 추가

## 2.1 새로 추가한 것과 재사용한 것

새로 추가한 것:

- PilotApp DTO/contract:
  - `PilotPollingSnapshot`
  - `PilotPollingStatus`
  - `WorkRequestKind`
  - `PilotPollingLogEntry`
  - `PilotPollingRequestState`
  - `IPilotPollingService`
  - `IPilotPollingFlowPort`
  - `IPilotPollingRequestStateReader`
- PilotApp orchestration:
  - `PilotPollingService`
  - `PilotPollingFlowPort`
  - `PilotPollingRequestStateReader`
- WPF-facing layer:
  - `PilotPollingViewModel`
  - `PilotPollingView`
- 신규 검증:
  - `PilotPollingServiceTests`
  - `PilotPollingViewModelTests`
  - `PilotPollingViewBindingTests`
  - `PilotPollingServiceFakePlcIntegrationTests`

재사용한 것:

- AH-PILOT-25:
  - `WorkStartAckOffService`
  - `WorkStartAckOffResult`
  - `WorkStartAckOffOptions`
  - WorkStart ACK OFF FakePlc 검증 근거
- AH-PILOT-26:
  - `WorkCompleteAckService`
  - `WorkCompleteAckResult`
  - `WorkCompleteAckOptions`
  - `WorkCompleteXgtPlcOperations`
  - WorkComplete ACK ON/OFF FakePlc 검증 근거
- AH-PILOT-27:
  - Pilot FakePlc full cycle audit 결과
  - FakePlc cycle 의미와 주소/시나리오 검토 결과
- 기존 WorkStart path:
  - `WorkStartExecutionService`
  - `WorkStartFlowService`
  - `WorkStartXgtPlcOperations`
  - fake DB용 `IWorkStartDataQuery` test adapter

## 3. 착공 Polling 구현 범위

- PollOnce에서 착공요청 ON을 감지한다.
- 착공 LOT ID를 WorkStart read block interpreter로 추출한다.
- WorkStart execution service를 통해 기존 WorkStart flow를 실행한다.
- 착공 payload write와 ACK ON은 기존 WorkStart flow / XGT adapter 경로를 사용한다.
- 착공요청 OFF 감지 후 AH-PILOT-25의 `WorkStartAckOffService`를 통해 ACK OFF를 수행한다.
- ACK OFF는 현장 default로 확정하지 않고 test-specific XGT write option에서 `startAckValue: 0`을 사용했다.

## 4. 완공 Polling 구현 범위

- PollOnce에서 완공요청 ON을 감지한다.
- 완공 DB 조회나 payload write는 포함하지 않았다.
- 완공요청 ON이면 AH-PILOT-26의 `WorkCompleteAckService.AckOnAsync`로 ACK ON을 수행한다.
- 완공요청 OFF 감지 후 AH-PILOT-26의 `WorkCompleteAckService.AckOffAsync`로 ACK OFF를 수행한다.
- 완공 주소 근거는 현장 default 확정이 아니라 기존 FakePlc/XGT test-specific baseline 검증으로 남겼다.

## 5. ACK ON/OFF 구현 범위

- 착공 ACK ON: 기존 `WorkStartFlowService`의 `WriteStartAckAsync` 경로 유지
- 착공 ACK OFF: 기존 `WorkStartAckOffService` + XGT write option value 0 사용
- 완공 ACK ON/OFF: 기존 `WorkCompleteAckService` + `WriteCompleteAckAsync(ushort value)` 사용
- 실제 PLC write는 수행하지 않았다. 모든 write 검증은 FakePlc in-process harness에서만 수행했다.

## 6. WPF 표시 구현 범위

- `PilotPollingViewModel` 표시 필드:
  - `IsPolling`
  - `LastRequestKind`
  - `LastSelectedLotId`
  - `LastStartRequestActive`
  - `LastCompleteRequestActive`
  - `LastStartAckState`
  - `LastCompleteAckState`
  - `LastStatus`
  - `LastResultStatus`
  - `LastErrorCode`
  - `LastMessage`
  - `LastUpdatedAt`
  - `LogEntries`
- 명령:
  - `StartPollingCommand`
  - `StopPollingCommand`
  - `PollOnceCommand`
- Dashboard 배치와 App.xaml DI wiring은 수행하지 않았다.

## 7. FakePlc 검증 결과

AH-PILOT-27의 cycle audit을 중복 작성하지 않고, `PilotPollingServiceFakePlcIntegrationTests.PollOnceAsync_WithFakePlc_ProcessesStartAndCompleteOnOffCycle`에서 기존 WorkStart/WorkComplete/FakePlc 자산이 새 PollOnce service 경유로 연결되는지 검증했다.

검증한 흐름:

1. 착공요청 ON 감지
2. WorkStart flow 실행
3. 착공 ACK ON read-back: `%DB11416 == 01 00`
4. 착공요청 OFF 반영
5. 착공 ACK OFF read-back: `%DB11416 == 00 00`
6. 완공요청 ON 반영
7. 완공 ACK ON read-back: `%DB11418 == 01 00`
8. 완공요청 OFF 반영
9. 완공 ACK OFF read-back: `%DB11418 == 00 00`

## 8. 아직 보류한 것

- 실제 background timer loop
- Dashboard 실제 배치
- App.xaml DI wiring
- real PLC 연결
- real DB concrete / connection string
- 완공 DB 조회 / payload write
- 현장 주소 default 확정
- FLOW.JSON / Flow Executor 구현

## 9. 테스트 결과

- `dotnet test tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj --no-restore`
  - PASS: 15 passed, 0 failed
- `dotnet test tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj --no-restore`
  - PASS: 226 passed, 0 failed
- `dotnet test tests/CAAutomationHub.PilotFlows.Tests/CAAutomationHub.PilotFlows.Tests.csproj --no-restore`
  - PASS: 45 passed, 0 failed
- `dotnet test tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj --no-restore`
  - PASS: 41 passed, 0 failed
- `dotnet test tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj --no-restore`
  - PASS: 142 passed, 0 failed

## 10. 빌드 결과

- `dotnet build CAAutomationHub.sln --no-restore`
  - PASS: warning 0, error 0

## 11. boundary scan 결과

- `git diff --check`
  - exit 0
  - 참고: `tests/CAAutomationHub.Wpf.Tests/Views/WorkStartPilotViewBindingTests.cs` line ending warning만 표시됨.
- broad scan:
  - `rg -n "XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src tests`
  - 기존 Runtime/WPF contract 및 기존 boundary test 참조가 조회됨.
- changed-scope scan:
  - PilotApp polling / WPF pilot polling / 신규 polling tests 범위에서는 hard-stop term match 없음.
- project reference scan:
  - production WPF는 `CAAutomationHub.PilotApp`만 참조하는 기존 경계를 유지한다.
  - production PilotApp은 `CAAutomationHub.PilotFlows`만 참조한다.
  - XGT/FakePlc reference는 기존과 동일하게 XGT project 또는 test project에만 존재한다.

## 12. 실제 PLC 테스트까지 남은 것

- 현장 착공/완공 request/ACK 주소 확인
- ACK OFF value/write variable 현장 확정
- 안전한 dry-run 또는 write-disabled mode 결정
- 실제 PLC write 승인 후 제한된 현장 smoke 실행
- 실패 시 ACK/error rollback 정책 확인

## 13. DB Concrete 다음 단계

- WorkStart fake DB를 대체할 `IWorkStartDataQuery` concrete adapter 후보를 별도 flag/adapter로 추가한다.
- connection string은 코드/문서에 기록하지 않는다.
- 완공 DB 조회가 필요한지 업무 정책을 먼저 확정한다.

## 14. Self-Check

- RuntimeSnapshot / ChannelPollingResult 수정 없음
- Runtime core 수정 없음
- WPF가 XgtDriverCore / FakePlc / SqlClient 직접 참조하지 않음
- FakePlc reference는 test-only
- FLOW.JSON / Flow Executor 구현 없음
- 실제 PLC write 없음
- 실제 DB connection string 기록 없음

판정: ACCEPT
