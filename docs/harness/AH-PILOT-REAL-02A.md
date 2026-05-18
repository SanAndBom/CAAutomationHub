# AH-PILOT-REAL-02A Closeout - FakePlc Localhost Read-Only Smoke Execution

## 1. Summary

AH-PILOT-REAL-02A는 실제 PLC 대신 로컬 FakePlc `localhost:2004`에 연결해 `tools/CAAutomationHub.PilotSmoke` read-only harness가 TCP/XGT 경로로 WorkStart read block을 읽고 해석할 수 있는지 확인한 실행 검증이다.

검증 경로:

```text
CAAutomationHub.PilotSmoke
  -> WorkStartXgtPlcOperations
  -> XgtDriverCore
  -> TCP localhost:2004
  -> FakePlc
  -> %DB10000 / 90 words read
  -> WorkStartReadBlockInterpreter
  -> start signal / LOT ID 출력
```

판정은 `ACCEPT`다.

핵심 결론:

- 실제 PLC에는 연결하지 않았다.
- write 테스트는 수행하지 않았다.
- DB query는 수행하지 않았다.
- ConnectionString은 사용하지 않았다.
- appsettings 기본 파일과 FakePlc map은 수정하지 않았다.
- `StartSignalWordIndex` default `80`은 수정하지 않았고, 이번 실행 명령에서만 FakePlc alignment용 `83` override를 사용했다.
- `PilotSmoke`는 FakePlc와 TCP/XGT 경로로 연결했고 `%DB10000` 90 words read 결과를 정상 해석했다.

## 2. FakePlc localhost:2004 상태

사전 확인:

```text
Get-NetTCPConnection -LocalPort 2004 -ErrorAction SilentlyContinue
```

결과:

```text
초기 상태: LISTEN 프로세스 없음
```

다른 프로세스가 `2004` port를 점유하지 않았기 때문에 로컬 FakePlc를 실행했다.

실행한 FakePlc:

```text
dotnet run --project C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\AutomationHub.XgtDriverCore.FakePlc.csproj -- 127.0.0.1 2004 AH-PILOT-REAL-02A C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\appsettings\fakeplc.map.json
```

FakePlc 확인 결과:

```text
Listening on 127.0.0.1:2004
Map file: C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\tools\AutomationHub.XgtDriverCore.FakePlc\appsettings\fakeplc.map.json
%DB10000: bytes=180, D-range=D5000..D5089
Loaded lotId1 at D5000 = S0007652610B
D5080 = 0x0001
D5083 = 0x0001
D5084 = 0x0000
```

의미:

- `%DB10000` base block은 `180` bytes, 즉 `90` words다.
- LOT ID 1은 FakePlc map 기준 `S0007652610B`다.
- current FakePlc start signal은 `D5083`, 즉 `%DB10000` read block의 word index `83`에서 active다.

## 3. PilotSmoke 실행 명령

먼저 help 후보를 확인했다.

```text
dotnet run --project tools/CAAutomationHub.PilotSmoke -- --help
```

결과:

```text
CAAutomationHub PilotSmoke WorkStart read-only harness
Target: (not configured):0
SKIPPED: Actual PLC read skipped. Pass --execute-read-only after confirming the target is read-only safe.
```

현재 tool은 별도 help text를 출력하지 않고, `--execute-read-only`가 없으면 안전 skip 경로로 종료한다.

구현 확인 결과 read start option 이름은 `--read-start`다. 사용자 지시의 예상 형태인 `--read-start-variable`은 현재 `PilotSmokeConfigurationLoader` 구현에는 없다.

실행 명령:

```text
dotnet run --project tools/CAAutomationHub.PilotSmoke -- --execute-read-only --host localhost --port 2004 --read-start "%DB10000" --read-word-count 90 --start-signal-word-index 83 --lot-id1-word-offset 0 --lot-id2-word-offset 10 --lot-id-word-length 6
```

## 4. Read-Only Safety 확인

이번 실행에서 호출된 `PilotSmoke` 경로는 다음 read-only operation만 사용한다.

```text
EnsureConnectedAsync
ReadWorkStartBlockAsync
WorkStartReadBlockInterpreter.IsStartSignalActive
WorkStartReadBlockInterpreter.ExtractLotId
WorkStartReadBlockInterpreter.SelectLotId
```

수행하지 않은 operation:

```text
WriteProcessPayloadAsync
WriteStartAckAsync
WriteErrorCodeBestEffortAsync
```

수행하지 않은 범위:

- ACK write
- error write
- payload write
- actual PLC connection
- DB query
- ConnectionString 사용
- appsettings 기본 파일 수정
- FakePlc map 수정
- RuntimeSnapshot 수정
- ChannelPollingResult 수정
- WPF 수정
- DI wiring 수정

FakePlc 로그에서도 이번 smoke connection은 read만 수행했다.

```text
[Client 127.0.0.1:60485] connected
[Client 127.0.0.1:60485] read %DB10000 length=180
[Client 127.0.0.1:60485] disconnected
```

## 5. Read Result

PilotSmoke 출력:

```text
CAAutomationHub PilotSmoke WorkStart read-only harness
Target: l***:2004
Connection success: True
Read success: True
Start signal active: True
LOT ID 1: S0007652610B
LOT ID 2:
Selected LOT ID: S0007652610B
Raw length: 180
```

기록 항목:

```text
connection attempt result: success
read result status: success
raw data length: 180 bytes
start signal active: True
LOT ID 1: S0007652610B
LOT ID 2: empty
selected LOT ID: S0007652610B
error / exception: none
write executed: no
```

## 6. 테스트 / 빌드 결과

실행:

```text
dotnet test tests/CAAutomationHub.PilotSmoke.Tests/CAAutomationHub.PilotSmoke.Tests.csproj
```

결과:

```text
passed 5, failed 0, skipped 0
```

실행:

```text
dotnet test tests/CAAutomationHub.PilotFlows.Xgt.Tests/CAAutomationHub.PilotFlows.Xgt.Tests.csproj
```

결과:

```text
passed 41, failed 0, skipped 0
```

실행:

```text
dotnet build CAAutomationHub.sln
```

결과:

```text
build succeeded
warnings 0
errors 0
```

## 7. git diff --check / git status

문서 작성 전 실행:

```text
git diff --check
git status --short
```

결과:

```text
git diff --check: output 없음
git status --short: output 없음
```

이 closeout 문서 추가 후 최종 `git diff --check`와 `git status --short`는 commit 전 확인 대상으로 남긴다.

## 8. Boundary / Harness 영향

유지된 boundary:

- Runtime shared execution path를 수정하지 않았다.
- Runtime core는 `XgtDriverCore` / `FakePlc` / `XgtChannelRunner`를 직접 참조하지 않았다.
- `RuntimeSnapshot` / `ChannelPollingResult`를 수정하지 않았다.
- WPF와 DI wiring을 수정하지 않았다.
- DB query / ConnectionString / SqlClient 경로를 사용하지 않았다.
- FakePlc는 production dependency가 아니라 로컬 통신 하네스로만 사용했다.

Harness 의미:

- 이번 검증은 실제 PLC acceptance가 아니다.
- 이번 검증은 write transaction 검증이 아니다.
- 이번 검증은 DB 연동 검증이 아니다.
- 이번 검증은 `PilotSmoke` read-only harness가 current FakePlc map과 TCP/XGT 경로로 연결되어 WorkStart read block을 해석할 수 있음을 확인한 것이다.

## 9. 다음 후보

1. `AH-PILOT-REAL-02B`: 실제 현장 read-only 실행 전 host/port/read layout safety review
2. `AH-PILOT-REAL-03`: 실제 PLC read-only smoke 실행, 별도 승인 후 진행
3. `AH-PILOT-DB-02`: DB query concrete 검증은 별도 DB safety instruction으로 분리
4. `AH-PILOT-WRITE-*`: ACK / error / payload write는 별도 write safety review와 승인 후 분리 진행

## 10. Self-Check

판정: `ACCEPT`

근거:

- FakePlc `localhost:2004` TCP/XGT 연결을 확인했다.
- `%DB10000` / `90` words read가 성공했고 raw data length `180` bytes를 확인했다.
- FakePlc alignment용 `StartSignalWordIndex = 83` override로 start signal active를 확인했다.
- LOT ID 1 `S0007652610B`, LOT ID 2 empty, selected LOT ID `S0007652610B`를 확인했다.
- write / ACK write / error write / DB query / actual PLC connection / ConnectionString 사용은 수행하지 않았다.
- code, default config, FakePlc map, RuntimeSnapshot, ChannelPollingResult, WPF, DI wiring은 수정하지 않았다.
- 요청된 tests와 solution build가 통과했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
