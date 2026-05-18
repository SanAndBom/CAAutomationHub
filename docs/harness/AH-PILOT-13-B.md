# AH-PILOT-13-B Closeout - WorkStart XGT Read Options

## 1. Summary

AH-PILOT-13-B에서는 WorkStart XGT read request 설정을 `CAAutomationHub.PilotFlows.Xgt` adapter boundary 안의 명시적 options로 분리했다.

변경 내용:

- `WorkStartXgtReadOptions`를 추가했다.
- pilot baseline default로 `%DB10000`, `90` words를 제공했다.
- `WorkStartXgtPlcOperations`에 options 기반 constructor를 추가했다.
- options의 word count를 XGT continuous read byte length로 변환해 `XgtReadRequest`를 생성한다.
- 기존 `XgtReadRequest` 직접 주입 constructor는 low-level/test seam으로 유지했다.
- options default / validation / request construction tests를 추가했다.

영향:

- XGT read start variable과 word count는 `CAAutomationHub.PilotFlows.Xgt` 안에 머문다.
- `CAAutomationHub.PilotFlows` core에는 XGT address를 추가하지 않았다.
- `WorkStartReadBlockLayout`은 `byte[]` 해석 layout 책임만 유지한다.
- Runtime / FlowDefinitions project는 수정하지 않았다.
- FakePlc, XgtChannelRunner, SqlClient, FLOW.JSON, Flow Executor, actual PLC read는 도입하지 않았다.

## 2. 변경 파일 목록

- `src/CAAutomationHub.PilotFlows.Xgt/WorkStart/WorkStartXgtReadOptions.cs`
  - WorkStart XGT read request options를 추가했다.
- `src/CAAutomationHub.PilotFlows.Xgt/WorkStart/WorkStartXgtPlcOperations.cs`
  - options 기반 constructor와 `XgtReadRequest` 생성 경로를 추가했다.
- `tests/CAAutomationHub.PilotFlows.Xgt.Tests/WorkStart/WorkStartXgtPlcOperationsTests.cs`
  - options default / validation / request construction tests를 추가했다.
- `docs/harness/AH-PILOT-13-B.md`
  - 이번 구현의 historical record를 추가했다.

## 3. WorkStartXgtReadOptions 추가 내용

추가 타입:

- namespace: `CAAutomationHub.PilotFlows.Xgt.WorkStart`
- type: `WorkStartXgtReadOptions`
- properties:
  - `ReadStartVariable`
  - `ReadWordCount`
- default constants:
  - `DefaultReadStartVariable`
  - `DefaultReadWordCount`
- default instance:
  - `WorkStartXgtReadOptions.Default`

Validation:

- `ReadStartVariable`은 null / empty / whitespace를 허용하지 않는다.
- `ReadWordCount`는 0 이하를 허용하지 않는다.
- XGT address syntax의 상세 검증은 이번 단계에서 추가하지 않았다.

## 4. Default 값과 의미

Default 값:

- `ReadStartVariable = "%DB10000"`
- `ReadWordCount = 90`

의미:

- 기존 pilot 기준의 WorkStart read baseline이다.
- `ReadWordCount = 90`은 XGT continuous read request 생성 시 `180` bytes로 변환된다.
- 이 값은 Runtime core default가 아니다.
- 이 값은 현장별 binding / FLOW.JSON 경로가 생기면 adapter-local options로 대체될 수 있는 pilot baseline이다.

## 5. WorkStartXgtPlcOperations 변경 내용

추가 constructor:

- `WorkStartXgtPlcOperations(IXgtSession session)`
- `WorkStartXgtPlcOperations(IXgtSession session, WorkStartXgtReadOptions options)`

유지한 constructor:

- `WorkStartXgtPlcOperations(IXgtSession session, XgtReadRequest readRequest)`

선택 이유:

- options 기반 constructor는 production baseline path를 명시한다.
- 기존 `XgtReadRequest` 직접 주입 constructor는 low-level request seam과 기존 tests 안정성을 위해 유지했다.
- adapter 내부에서 `WorkStartXgtReadOptions.ReadWordCount * 2`를 checked conversion으로 `ushort` byte length로 변환한 뒤 `XgtReadRequest`를 만든다.
- `XgtReadRequest` public API가 continuous byte variable과 byte length를 검증하므로, 이번 단계에서는 별도 address syntax validation을 과하게 추가하지 않았다.

## 6. WorkStartReadBlockLayout과의 책임 분리

`WorkStartReadBlockLayout` 책임:

- WorkStart read block 내부의 byte/word 해석 layout.
- start signal word index, LOT ID offsets, LOT ID length.
- `CAAutomationHub.PilotFlows` core에 남는 vendor-neutral layout contract.

`WorkStartXgtReadOptions` 책임:

- XGT read request가 어느 variable에서 몇 word를 읽을지 표현.
- `%DB10000` 같은 XGT-specific address를 adapter boundary 안에 보관.
- word count를 XGT continuous byte length로 변환하는 request creation의 입력.

이번 작업에서 `WorkStartReadBlockLayout`은 수정하지 않았다.

## 7. 테스트 결과

TDD RED:

- command: `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
- result: failed as expected
- reason: `WorkStartXgtReadOptions` type / options constructor did not exist.

TDD GREEN 및 최종 validation:

- command: `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
- result: passed
- count: failed 0, passed 14, skipped 0, total 14

- command: `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
- result: passed
- count: failed 0, passed 40, skipped 0, total 40

- command: `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
- result: passed
- count: failed 0, passed 142, skipped 0, total 142

## 8. 빌드 결과

- command: `dotnet build CAAutomationHub.sln`
- result: passed
- warnings: 0
- errors: 0

## 9. Boundary scan 결과

Command:

```text
rg -n "FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.PilotFlows.Xgt tests\CAAutomationHub.PilotFlows.Xgt.Tests
```

Result:

- no matches
- `rg` exit code 1 because no forbidden patterns were found.

Project reference scan:

```text
rg -n "<ProjectReference|PackageReference" src tests -g "*.csproj"
```

확인 결과:

- `src\CAAutomationHub.PilotFlows.Xgt\CAAutomationHub.PilotFlows.Xgt.csproj`만 sibling `AutomationHub.XgtDriverCore`를 참조한다.
- `src\CAAutomationHub.PilotFlows\CAAutomationHub.PilotFlows.csproj`에는 XgtDriverCore reference가 없다.
- `src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj`에는 XgtDriverCore / FakePlc / XgtChannelRunner reference가 없다.
- `src\CAAutomationHub.FlowDefinitions\CAAutomationHub.FlowDefinitions.csproj`에는 XgtDriverCore / FakePlc / XgtChannelRunner reference가 없다.
- FakePlc / XgtChannelRunner / `Microsoft.Data.SqlClient` PackageReference는 추가하지 않았다.

## 10. git diff --check 결과

Command:

```text
git diff --check
```

Result:

- exit code 0
- whitespace error 없음
- Git line-ending warning only:
  - `LF will be replaced by CRLF the next time Git touches it`

## 11. git status --short 결과

Closeout 작성 후 final status:

```text
 M src/CAAutomationHub.PilotFlows.Xgt/WorkStart/WorkStartXgtPlcOperations.cs
 M tests/CAAutomationHub.PilotFlows.Xgt.Tests/WorkStart/WorkStartXgtPlcOperationsTests.cs
?? docs/harness/AH-PILOT-13-B.md
?? src/CAAutomationHub.PilotFlows.Xgt/WorkStart/WorkStartXgtReadOptions.cs
```

## 12. 제외한 범위

이번 AH-PILOT-13-B에서 제외한 범위:

- FakePlc integration test
- actual PLC read test
- XGT write adapter 구현
- ACK adapter 구현
- error writer adapter 구현
- DB concrete 구현
- FLOW.JSON binding 구현
- JSON schema / parser 생성
- Flow Executor 구현
- Runtime 연결
- `WorkStartPilotService` source copy
- PilotFlows core에 XGT address 추가
- `WorkStartReadBlockLayout`에 XGT address 추가
- FakePlc reference 추가
- XgtChannelRunner reference 추가
- Microsoft.Data.SqlClient reference 추가
- SQL connection string / SQL text 추가
- RuntimeSnapshot / ChannelPollingResult 참조

## 13. 다음 후보

- AH-PILOT-14 후보: FakePlc integration 전 alignment review.
- FakePlc initializer와 `WorkStartReadBlockLayout.DefaultStartSignalWordIndex` alignment 확인.
- 현장별 binding / FLOW.JSON이 생기기 전까지 adapter-local options를 pilot baseline으로 유지.
- 필요 시 `ReadWordCount`와 layout offsets가 서로 충분한지 검증하는 adapter-local validation 후보 검토.

## 14. Self-Check

판정: `ACCEPT`

근거:

- XGT read request 설정을 `CAAutomationHub.PilotFlows.Xgt` adapter boundary 안에 명시했다.
- `%DB10000`, `90` words default는 pilot baseline으로 제공했다.
- Runtime / FlowDefinitions / PilotFlows core는 수정하지 않았다.
- PilotFlows core에 XGT address를 추가하지 않았다.
- `WorkStartReadBlockLayout`은 byte block 해석 layout 책임만 유지했다.
- FakePlc / XgtChannelRunner / SqlClient / FLOW.JSON / Flow Executor / actual PLC read로 범위를 확장하지 않았다.
- RED -> GREEN 테스트 evidence를 남겼다.
- 지정 테스트, build, boundary scan, project reference scan, diff check를 수행했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
