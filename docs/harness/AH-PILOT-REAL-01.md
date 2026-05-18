# AH-PILOT-REAL-01 Closeout - Real PLC Read-Only Smoke Harness Skeleton

## 1. Summary

AH-PILOT-REAL-01은 실제 PLC write 없이 WorkStart read-only smoke를 실행할 수 있는 console harness skeleton을 추가한 stage다.

추가한 harness는 `tools/CAAutomationHub.PilotSmoke`이며, 기본 실행은 실제 PLC에 접속하지 않고 skip된다. 실제 read는 `--execute-read-only` 플래그와 명시적 host/port/read layout 설정이 모두 있을 때만 실행된다.

판정은 `ACCEPT`다.

핵심 결론:

- real PLC write는 수행하지 않았다.
- ACK write / error write / payload write는 harness runner에서 호출하지 않는다.
- DB query는 수행하지 않는다.
- WorkStart full transaction은 수행하지 않는다.
- 실제 PLC host/port/address는 코드 default로 넣지 않았다.
- 실제 PLC 정보 또는 connection string을 문서/코드에 기록하지 않았다.
- actual real PLC read는 환경 정보와 사용자 명시 승인이 없어서 실행하지 않았다.

## 2. 구현한 smoke harness 형태

생성:

```text
tools/CAAutomationHub.PilotSmoke/CAAutomationHub.PilotSmoke.csproj
tools/CAAutomationHub.PilotSmoke/Program.cs
tools/CAAutomationHub.PilotSmoke/PilotSmokeConfiguration.cs
tools/CAAutomationHub.PilotSmoke/PilotSmokeConfigurationLoader.cs
tools/CAAutomationHub.PilotSmoke/WorkStartReadOnlySmoke.cs
tests/CAAutomationHub.PilotSmoke.Tests/CAAutomationHub.PilotSmoke.Tests.csproj
tests/CAAutomationHub.PilotSmoke.Tests/PilotSmokeConfigurationLoaderTests.cs
tests/CAAutomationHub.PilotSmoke.Tests/WorkStartReadOnlySmokeTests.cs
```

Solution 등록:

```text
CAAutomationHub.sln
```

구조:

```text
Program
  -> PilotSmokeConfigurationLoader
  -> XgtSession + TcpTransport
  -> WorkStartXgtPlcOperations
  -> WorkStartReadOnlySmoke
  -> WorkStartReadBlockInterpreter
```

## 3. Read-Only Safety Guard

실제 read 실행 조건:

- `--execute-read-only`를 명시적으로 전달하거나 `CAAH_PILOT_PLC_EXECUTE_READ_ONLY=true`를 설정해야 한다.
- host, port, read start variable, read word count, start signal index, LOT ID offsets, LOT ID word length가 모두 명시되어야 한다.
- 설정이 없거나 불완전하면 실제 접속 없이 skip한다.

기본 실행 검증:

```text
dotnet run --project tools/CAAutomationHub.PilotSmoke/CAAutomationHub.PilotSmoke.csproj
```

결과:

```text
CAAutomationHub PilotSmoke WorkStart read-only harness
Target: (not configured):0
SKIPPED: Actual PLC read skipped. Pass --execute-read-only after confirming the target is read-only safe.
```

## 4. Configuration 방식

Command-line:

```text
--execute-read-only
--host
--port
--read-start
--read-word-count
--start-signal-word-index
--lot-id1-word-offset
--lot-id2-word-offset
--lot-id-word-length
```

Environment variables:

```text
CAAH_PILOT_PLC_EXECUTE_READ_ONLY
CAAH_PILOT_PLC_HOST
CAAH_PILOT_PLC_PORT
CAAH_PILOT_PLC_READ_START_VARIABLE
CAAH_PILOT_PLC_READ_WORD_COUNT
CAAH_PILOT_PLC_START_SIGNAL_WORD_INDEX
CAAH_PILOT_PLC_LOT_ID1_WORD_OFFSET
CAAH_PILOT_PLC_LOT_ID2_WORD_OFFSET
CAAH_PILOT_PLC_LOT_ID_WORD_LENGTH
```

금지한 것:

- 실제 PLC IP/port code default
- 실제 PLC host/port closeout 평문 기록
- local config commit
- DB connection string

## 5. Output

실제 read가 실행되면 console에 다음만 출력한다.

- connection success/fail
- read success/fail
- start signal active
- LOT ID 1
- LOT ID 2
- selected LOT ID
- raw length
- failure message

host는 report용 `MaskedHost`로 마스킹한다.

## 6. 제외한 Write 범위

`WorkStartReadOnlySmoke.RunAsync`가 호출하는 operation:

```text
EnsureConnectedAsync
ReadWorkStartBlockAsync
WorkStartReadBlockInterpreter.IsStartSignalActive
WorkStartReadBlockInterpreter.ExtractLotId
WorkStartReadBlockInterpreter.SelectLotId
```

호출하지 않는 operation:

```text
WriteProcessPayloadAsync
WriteStartAckAsync
WriteErrorCodeBestEffortAsync
```

수행하지 않는 업무:

- ACK write
- error write
- payload write
- DB query
- WorkStartFlowService full transaction
- WorkComplete ACK
- WPF App.xaml production DI wiring

## 7. Tests / Build 결과

TDD RED:

```text
dotnet test tests/CAAutomationHub.PilotSmoke.Tests/CAAutomationHub.PilotSmoke.Tests.csproj
```

결과:

```text
FAILED: PilotSmoke project and types were missing
Missing: PilotSmokeConfigurationLoader, PilotSmokeReadLayout, WorkStartReadOnlySmoke
```

GREEN:

```text
dotnet test tests/CAAutomationHub.PilotSmoke.Tests/CAAutomationHub.PilotSmoke.Tests.csproj
dotnet build tools/CAAutomationHub.PilotSmoke/CAAutomationHub.PilotSmoke.csproj
```

결과:

```text
PilotSmoke tests: passed 5, failed 0, skipped 0
PilotSmoke tool build: warnings 0, errors 0
```

## 8. Actual Run 여부

Actual real PLC read-only run: `not executed / skipped`

이유:

- 실제 PLC host/port/read layout 정보가 제공되지 않았다.
- 사용자의 명시적 real PLC read 실행 승인이 없었다.
- 안전 조건이 충족되지 않으면 tool이 접속하지 않도록 구현했다.

## 9. Boundary / Harness 영향

유지된 경계:

- RuntimeSnapshot / ChannelPollingResult를 수정하지 않았다.
- Runtime shared execution path를 수정하지 않았다.
- FlowDefinitions / FLOW.JSON / parser / executor를 수정하지 않았다.
- WPF는 XgtDriverCore / FakePlc / SqlClient를 직접 참조하지 않는다.
- PilotApp은 real PLC / DB concrete를 직접 참조하지 않는다.
- XgtChannelRunner reference를 추가하지 않았다.
- WorkStartPilotService source copy를 하지 않았다.
- DB query 자동 실행을 추가하지 않았다.
- 실제 PLC write 가능한 path를 smoke runner에서 호출하지 않는다.

허용된 boundary:

- `tools/CAAutomationHub.PilotSmoke`는 field smoke harness로서 `CAAutomationHub.PilotFlows.Xgt`와 `AutomationHub.XgtDriverCore`를 참조한다.
- 이는 WPF / PilotApp / Runtime production boundary가 아니라 명시적 tools harness boundary다.

## 10. 다음 후보

1. 실제 현장 read-only 실행 전 host/port/read layout을 local env 또는 command-line으로 준비
2. 현장 safety 확인 후 `--execute-read-only`로 한 번만 read smoke 실행
3. 결과는 host 마스킹 후 closeout 보정
4. AH-PILOT-DB-02에서 SqlServer WorkStartDataQuery concrete 구현
5. real PLC write는 별도 승인과 ACK/error/payload safety review 이후 분리 진행

## 11. Self-Check

판정: `ACCEPT`

근거:

- read-only smoke harness skeleton과 테스트를 추가했다.
- 기본 실행은 실제 PLC 접속 없이 skip됨을 확인했다.
- runner 테스트에서 write operation call count가 0임을 검증했다.
- 실제 PLC write / ACK write / error write / payload write / DB query를 수행하지 않았다.
- 실제 PLC 정보와 DB connection string을 기록하지 않았다.
- Runtime / WPF / Driver / FakePlc / Harness 경계를 보존했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
