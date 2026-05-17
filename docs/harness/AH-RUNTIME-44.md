# AH-RUNTIME-44 Closeout

## 1. Summary

AH-RUNTIME-44는 `XgtChannelRunner`의 검증된 pilot business flow를 `CAAutomationHub` 안에 어떻게 녹일지 검토한 XgtAdapter / Pilot Business Flow Placement Boundary Review다.

핵심 결론은 `WorkStartPilotService.RunOnceAsync(...)`가 재사용 가치가 높은 pilot business sequence anchor라는 점이다. 다만 현재 구현은 `PlcChannel`, XGT raw frame, response parser / classifier, SQL, hard-coded address / config, hex logging에 결합되어 있다.

따라서 이 흐름을 그대로 `CAAutomationHub.Runtime` core에 넣거나 `XgtChannelRunner` project reference로 붙이면 Runtime vendor-neutral boundary가 깨진다. 가장 안전한 방향은 `CAAutomationHub` repo 안에 녹이되 Runtime core 밖에서 책임 분리하여 재구성하는 것이다.

이번 작업은 Boundary Review 결과를 문서화하는 단계이며, production code, test code, solution, csproj, project reference, package reference, source copy, adapter skeleton, commit, `ContextPublisher` 자동 publish는 수행하지 않았다.

## 2. Goal

AH-RUNTIME-44의 목표는 다음 질문에 답하는 것이었다.

- `XgtChannelRunner`의 검증된 `WorkStartPilotService.RunOnceAsync(...)` 흐름을 `CAAutomationHub` 안에 녹인다면 어떤 계층 / namespace / module / project boundary가 가장 안전한가?
- 사용자 선호인 "project reference가 아니라 CAAutomationHub 안에 직접 녹이기"를 Runtime core vendor-neutral 원칙과 어떻게 양립시킬 수 있는가?
- 어떤 로직은 business flow로 재구성하고, 어떤 로직은 XGT adapter/source 계층으로 격리해야 하는가?
- 어떤 부분은 UI / WinForms event handler이므로 가져오면 안 되는가?
- 향후 `XgtDriverCore` 연결 시 어떤 adapter seam을 남겨야 하는가?

이번 단계는 구현이 아니라 Boundary Review다. 따라서 코드 추가나 project 구조 변경 없이 read-only 조사와 판단만 수행했다.

## 3. Background

AH-RUNTIME-43의 핵심 결론은 다음이었다.

    Adapter boundary first,
    retrieval after clean anchor

즉, 기존 `XgtDriverCore` / `FakePlc` / `XgtChannelRunner` 자산을 바로 reference로 연결하거나 가져오기 전에, `CAAutomationHub` 내부에서 XGT / Pilot business flow를 어떤 경계로 녹일지 먼저 결정해야 한다.

현재 `CAAutomationHub.Runtime`에는 vendor-neutral polling pipeline이 있다.

    ChannelPollingTarget
        -> polling source / business operation / adapter
        -> ChannelPollingResult
        -> PollingCycleCoordinator
        -> PollingResultStateOrchestrator
        -> PollingPublishCoordinator
        -> Runtime snapshot publish path

현재 비어 있는 구간은 `ChannelPollingTarget`에서 실제 polling source / business operation / adapter를 거쳐 `ChannelPollingResult`를 만드는 구간이다.

별도 reuse anchor로 확인된 pilot business flow는 다음이다.

    XgtChannelRunner
        btnRunPilotOnce_Click
            -> WorkStartPilotService.RunOnceAsync(...)

`btnRunPilotOnce_Click`는 UI entry point이며, 실제 재사용 가치는 `WorkStartPilotService.RunOnceAsync(...)`의 business sequence에 있다.

## 4. 사용자 선호 반영 판단

사용자 선호는 다음과 같이 해석한다.

- `XgtChannelRunner`를 별도 project reference로 연결하는 방식은 선호하지 않는다.
- 프로젝트 관리 범위가 커지는 것을 원하지 않는다.
- 가능하면 `XgtChannelRunner`의 핵심 비즈니스 로직을 기준으로 `CAAutomationHub` 소스 안에 직접 녹이는 방식을 선호한다.
- 단, 무분별한 source copy가 아니라 검증된 business flow를 책임 분리하여 `CAAutomationHub` 구조에 맞게 재구성하는 방향을 원한다.

이 선호는 `CAAutomationHub.Runtime` core에 XGT-specific code를 섞자는 뜻이 아니다. 또한 `XgtChannelRunner` project를 그대로 참조하거나 WinForms event handler를 복사하자는 뜻도 아니다.

권장 해석은 다음이다.

- Runtime core는 계속 vendor-neutral을 유지한다.
- XGT read / write / status / response classification은 adapter/source boundary로 격리한다.
- Pilot business transaction은 Runtime polling state path와 분리한다.
- payload packing, LOT ID extraction, start signal interpretation, error code policy는 선택 이식 후보로 다룬다.
- WinForms handler, `MessageBox`, chart, form state, hard-coded SQL/config는 제외한다.

따라서 사용자 선호는 "외부 project reference가 아니라 `CAAutomationHub` repo 내부에 책임 분리된 module / project로 녹인다"는 방향으로 반영하는 것이 안전하다.

## 5. 확인한 WorkStartPilotService / btnRunPilotOnce_Click 구조

확인한 주요 파일은 다음이다.

- `XgtChannelRunner\MainForm.cs`
- `XgtChannelRunner\MainForm.Designer.cs`
- `XgtChannelRunner\Services\WorkStartPilotService.cs`
- `XgtChannelRunner\Models\PilotScenarioConfig.cs`
- `XgtChannelRunner\Services\ProcessDataPayloadBuilder.cs`
- `XgtChannelRunner\Services\LotDataQueryService.cs`
- `tests\AutomationHub.XgtChannelRunner.Tests\Services\ProcessDataPayloadBuilderTests.cs`

`btnRunPilotOnce_Click`의 책임은 다음이다.

- selected channel 확인
- connected 상태 확인
- polling 중이면 stop
- button enable / disable
- `PilotScenarioConfig` 생성
- `WorkStartPilotService` 생성
- 결과를 textbox / label / messagebox에 표시

판단:

- `btnRunPilotOnce_Click`는 UI / control-plane entry point다.
- `CAAutomationHub.Runtime` 또는 Pilot business flow로 가져오면 안 되는 부분이다.
- 핵심 재사용 후보는 `WorkStartPilotService.RunOnceAsync(...)`의 sequence다.

## 6. 핵심 business flow 요약

실제 코드 기준 sequence는 다음이다.

1. `PlcChannel.EnsureConnectedAsync`
2. `%DB10000`, 90 word continuous read request build
3. raw exchange + response classification
4. read NAK / malformed이면 error `1101`, step `group-read`
5. read response parse 실패 / empty이면 error `1102`, step `group-read-parse`
6. start signal 확인
   - 기본 word index: `80`
7. inactive이면 error `1200`, step `start-signal`
8. LOT ID 1 offset `0`, LOT ID 2 offset `10`, length `6` word
9. LOT ID 1 우선, 없으면 LOT ID 2
10. 둘 다 empty이면 error code `2201` best-effort write
11. SQL query
12. DB exception `2300`
13. DB not found `2301`
14. DB multiple rows `2302`
15. 기타 query failure `2303`
16. payload build 실패 `2400`
17. `%DB11000` bulk write payload build / write
18. bulk write 실패 `2501`
19. `%DB11416` ACK value `1` write
20. ACK write 실패 `2601`
21. unexpected exception `2999`

중요 보정:

- 실제 코드에서는 모든 실패가 error code write를 수행하지 않는다.
- `2201`, `2300~2303`, `2400`, `2501`, `2601` 경로는 best-effort write가 있다.
- `1101`, `1102`, `1200`, `2999`는 현재 `TryWriteErrorCodeAsync`를 호출하지 않는다.

## 7. 위험 의존성

확인된 위험 의존성은 다음이다.

- `WorkStartPilotService`가 `PlcChannel` concrete를 직접 받는다.
- `AutomationHub.XgtDriverCore.Protocol` frame builder / parser를 직접 사용한다.
- `PollingSnapshotJsonStore.FormatHex`로 persistence helper에 의존한다.
- `LotDataQueryService`가 `Microsoft.Data.SqlClient`와 connection string / SQL text를 직접 사용한다.
- `PilotScenarioConfig`에 XGT address, SQL text, connection string, timeout이 hard-coded 되어 있다.
- result에 request / response hex, reconnect attempt count 등 adapter / diagnostic 정보가 혼재되어 있다.
- tests도 source link 방식으로 Runner source를 직접 compile한다.

판단:

- 그대로 Runtime core에 넣으면 vendor-neutral boundary가 깨진다.
- 그대로 source copy하면 XGT / SQL / hard-coded config / diagnostic detail이 함께 유입된다.
- sequence는 재사용 anchor지만 구현체는 책임 분리 후 재구성해야 한다.

## 8. 후보 A: XgtChannelRunner / WorkStartPilotService project reference

판정: 비권장

개념:

    CAAutomationHub
        -> XgtChannelRunner project reference
        -> WorkStartPilotService 직접 사용

이유:

- `XgtChannelRunner`는 WinForms, SQL, chart / UI, channel manager, polling coordinator 성격이 같이 들어 있다.
- `CAAutomationHub.Runtime` vendor-neutral boundary가 깨질 수 있다.
- `RuntimeProjectReferenceBoundaryTests`는 Runtime이 `XgtDriverCore`, `XgtChannelRunner`, `FakePlc`, `Wpf`를 참조하지 않도록 잠그고 있다.
- 사용자 선호인 "프로젝트 참조가 아니라 CAAutomationHub 안에 녹이기"와도 맞지 않는다.

결론:

- 특별한 이유가 없다면 채택하지 않는다.
- `WorkStartPilotService.RunOnceAsync(...)`는 project reference 대상이 아니라 business sequence reference로만 사용한다.

## 9. 후보 B: WorkStartPilotService 그대로 source copy

판정: 비권장

이유:

- sequence 참고 가치는 높다.
- 하지만 그대로 복사하면 XGT raw frame, SQL, hard-coded config, persistence hex formatting, `PlcChannel` concrete가 함께 들어온다.
- 원본과 drift가 생길 수 있다.
- 책임 분리 없이 복사하면 `CAAutomationHub` 구조가 오염된다.

결론:

- 그대로 copy가 아니라 business rule과 pure helper를 재구성해야 한다.
- 선택 이식 후보는 `ProcessDataPayloadBuilder`의 packing rule, LOT ID extraction rule, start signal interpretation, error code mapping 같은 작은 규칙으로 제한하는 것이 안전하다.

## 10. 후보 C: CAAutomationHub 내부 PilotFlow module로 재구성

판정: 조건부 가능

판단:

- `src/CAAutomationHub.Runtime/Pilot`처럼 Runtime project 내부에 넣는 것은 위험하다.
- Pilot flow는 Runtime canonical state 업데이트보다 business transaction 성격이 강하다.
- Runtime core에 두면 DB / address / XGT policy가 흘러들 가능성이 크다.
- 가능한 해석은 Runtime 내부가 아니라 `CAAutomationHub` repo 내부의 별도 business-flow boundary다.

결론:

- `CAAutomationHub.Runtime` project 내부 module로 두는 방식은 신중해야 한다.
- 별도 project 또는 Runtime 밖 namespace/module로 분리하는 방향을 우선 검토한다.

## 11. 후보 D: CAAutomationHub repo 내부 XgtAdapter / Pilot project 추가

판정: 가장 안전한 절충안

후보:

- `src/CAAutomationHub.Runtime.XgtAdapter`
- `src/CAAutomationHub.PilotFlows`
- 또는 초기에는 하나의 in-repo integration project에서 adapter와 pilot flow를 분리 namespace로 시작

장점:

- `CAAutomationHub` repo 안에 녹일 수 있다.
- Runtime core vendor-neutral을 보존할 수 있다.
- XGT-specific / business-specific 로직을 격리할 수 있다.
- project reference 관리 범위를 `CAAutomationHub` repo 내부로 제한할 수 있다.
- 사용자 선호와 아키텍처 경계 사이의 균형점이다.

위험:

- project 수가 늘어난다.
- adapter / pilot flow / runtime 관계를 잘못 잡으면 복잡도가 증가한다.
- `XgtDriverCore` retrieval 문제는 여전히 남는다.

결론:

- AH-RUNTIME-45 이후 project split을 더 좁게 결정하는 것이 좋다.
- 실제 `XgtDriverCore` reference 추가는 clean anchor와 adapter boundary 확정 이후로 미룬다.

## 12. 후보 E: Runtime 안 XGT-specific folder

판정: 비권장

개념:

    src/CAAutomationHub.Runtime/Polling/Xgt
        Xgt-specific polling source 또는 mapping code 배치

이유:

- project 수를 늘리지 않는 장점은 있다.
- 하지만 장기적으로 Runtime이 `XgtDriverCore`를 참조하게 될 압력이 크다.
- `RuntimeProjectReferenceBoundaryTests`와 충돌 가능성이 있다.
- vendor-neutral core가 흐려진다.
- XGT-specific namespace가 Runtime core에 들어온다.

결론:

- 사용자 선호를 고려해도 비권장 가능성이 크다.
- Runtime core에는 vendor-neutral polling target / result / state publish path만 남기는 것이 안전하다.

## 13. 후보 F: Business flow 문서 / 테스트 시나리오 흡수

판정: 강력 추천되는 첫 단계

장점:

- 코드 오염 없이 LOTID -> DB -> payload -> bulk write -> ACK / error sequence를 `CAAutomationHub` harness 기준으로 정식화할 수 있다.
- 검증된 business sequence를 보존할 수 있다.
- 후속 구현 시 기준이 된다.
- project reference가 필요 없다.
- AH-RUNTIME-44가 구현 금지 단계였으므로 가장 잘 맞는 흡수 방식이다.

위험:

- 실제 구현은 아직 아니다.
- 코드 재사용 시간 단축 효과는 제한적이다.
- 다음 단계에서 다시 구현이 필요하다.

결론:

- AH-RUNTIME-45의 최우선 후보로 적절하다.
- 먼저 scenario / harness 문서로 business contract를 고정한 뒤 helper extraction과 adapter boundary를 진행한다.

## 14. 후보 G: 순수 helper 선택 이식

판정: 추천, 단 후속 단계에서만 수행

선택 이식 후보:

- `ProcessDataPayloadBuilder`의 packing rule
- LOT ID extraction rule
- start signal interpretation
- error code mapping table
- `WorkStartPilotResult` 개념
- `LotProcessData` shape

주의:

- `ProcessDataPayloadBuilder`는 비교적 순수하지만 `PilotScenarioConfig.WriteStartVariable`, word count, address policy에 기대고 있다.
- helper만 가져와도 options / config 분리가 먼저 필요하다.
- 테스트도 함께 재구성해야 의미가 있다.

결론:

- source copy가 아니라 selective extraction / reconstruction으로 다룬다.
- Runtime core가 아니라 PilotFlow 또는 Adapter-adjacent 계층에 둔다.

## 15. WorkStartPilotService 재구성 시 책임 분리 후보

### 15.1 Pilot business flow

책임:

- start signal 판단
- LOT ID 선택
- DB 조회 요청
- payload 생성 요청
- write / ACK / error 요청
- 단계별 result 생성

주의:

- 직접 `IXgtSession`을 호출하지 않는 것이 이상적이다.
- 직접 `PlcChannel`을 알지 않는 것이 이상적이다.
- 직접 SQL connection을 열지 않는 것이 이상적이다.

### 15.2 XGT operation adapter

책임:

- read words
- write words
- write ACK
- write error code
- XGT response / exception mapping

이 계층은 향후 `XgtDriverCore` 연결 시 `IXgtSession`, `ReadAsync`, `WriteAsync`, `ReadStatusAsync`, `TransportException`, `TransportFailureKind`, `XgtRawResponseClassifier`, `XgtResponseClassification`, `XgtStatusInterpreter`와 만나는 seam이 된다.

### 15.3 Data query abstraction

책임:

- LOT ID로 process data 조회
- not found / multiple / DB exception 구분

이 계층은 `Microsoft.Data.SqlClient`, connection string, SQL text를 Runtime core 밖으로 격리해야 한다.

### 15.4 Payload builder

책임:

- process data를 PLC write payload로 packing
- 순수 함수에 가깝게 유지 가능

`ProcessDataPayloadBuilder`의 rule은 reuse 가치가 있으나 address / word count / field layout policy는 options로 분리해야 한다.

### 15.5 Scenario / options

책임:

- address
- word offset
- length
- ACK value
- error address
- read / write size
- DB config
- pilot flow settings

주의:

- hard-coded 값을 코드에 박지 않고 option / config로 분리하는 방향을 검토한다.
- `ChannelPollingTarget`에는 XGT address / datatype / count를 넣지 않는다.
- `ChannelPollingResult`에는 raw frame / XGT exception / FakePlc scenario detail을 넣지 않는다.

## 16. 권장안

권장 순서:

1. 후보 F: Business flow 문서 / 테스트 시나리오 흡수
2. 후보 G: Pure helper extraction review
3. 후보 D: `CAAutomationHub` repo 내부 XgtAdapter / Pilot project boundary 결정

판단 이유:

- 지금은 "무엇을 가져올지"보다 "검증된 sequence를 어떤 계약으로 보존할지"가 먼저다.
- sibling repo dirty state도 여전히 남아 있으므로 실제 reference 연결 전 clean anchor가 필요하다.
- Runtime core는 계속 vendor-neutral로 유지해야 한다.
- Pilot business transaction은 Runtime polling state path와 분리하는 것이 안전하다.

Runtime core에 남겨야 할 것:

- vendor-neutral polling target / result / state publish path
- PLC id 중심 canonical state update
- driver / vendor를 모르는 failure kind mapping 결과

Runtime core에 들어가면 안 되는 것:

- XGT address / datatype / count
- raw frame / parser / classifier
- SQL query / connection string
- Pilot-specific LOTID / payload / ACK sequence
- FakePlc scenario detail
- WinForms UI state

## 17. AH-RUNTIME-45 후보 및 우선순위

추천 우선순위:

1. Pilot Flow Documentation / Scenario Harness
   - LOTID -> DB -> payload -> bulk write -> ACK / error sequence를 `CAAutomationHub` docs / harness 기준으로 정식화
2. Pure Helper Extraction Review
   - `ProcessDataPayloadBuilder` / LOT ID extraction / error code mapping 등 선택 이식 후보 검토
3. XgtAdapter In-Repo Project Boundary Review
   - `CAAutomationHub` repo 내부 project로 adapter boundary를 세울지 결정
4. WorkStartPilot Flow Boundary Review
   - business flow를 Runtime / Adapter / PilotFlow 계층으로 어떻게 재구성할지 상세 검토
5. Existing Project Clean Anchor Plan
   - sibling repo dirty state 정리 / commit anchor 확보

판단:

- AH-RUNTIME-45는 먼저 Pilot Flow Documentation / Scenario Harness로 가는 것이 가장 안전하다.
- 그 다음 pure helper extraction을 검토하고, 이후 in-repo adapter / pilot project split을 결정하는 편이 좋다.

## 18. 제외한 범위

이번 AH-RUNTIME-44에서는 다음을 하지 않았다.

- production code 수정
- test code 수정
- project / solution / csproj 수정
- `ProjectReference` / `PackageReference` 추가
- source copy
- `XgtDriverCore` 연결
- `FakePlc` 연결
- `XgtChannelRunner` 연결
- adapter skeleton 추가
- Closeout 생성 외 작업
- commit
- `ContextPublisher` 자동 publish 재도입

이번 단계는 Boundary Review 및 closeout 문서화 단계다.

## 19. 실행한 명령

AH-RUNTIME-44 Boundary Review 당시 실행한 명령은 다음과 같다.

현재 repo:

- `git status --short`
- `git log --oneline -5`
- `git branch --show-current`
- `dotnet sln list`
- `rg "Pilot|WorkStart|Payload|Lot|LotId|ProcessData|XgtAdapter|PollingSource" src tests docs/harness docs/context`
- `rg "ProjectReference|PackageReference" . -g "*.csproj"`
- Runtime polling / reference 관련 `rg --files`, `rg -n`, `Get-Content`
- cognitive context docs 및 AH-RUNTIME-43 closeout `Get-Content`

sibling repo:

- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore log --oneline -5`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore branch --show-current`
- requested pilot flow `rg -n`
- relevant `Get-Content` for `MainForm`, Designer snippet, service / model / test / readme files
- `XgtDriverCore` API seam files `Get-Content`

AH-RUNTIME-44 Closeout 작성 후 실행한 검증 명령:

- `git diff -- docs/harness/AH-RUNTIME-44.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-44.md`

## 20. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-44 Boundary Review 결과를 `docs/harness/AH-RUNTIME-44.md` closeout 문서로 기록했다.
- 사용자 선호인 "`XgtChannelRunner` project reference가 아니라 `CAAutomationHub` 안에 녹이기"를 Runtime core vendor-neutral 원칙과 분리해 해석했다.
- `btnRunPilotOnce_Click`는 UI / control-plane entry point이며 가져오면 안 되는 부분으로 기록했다.
- `WorkStartPilotService.RunOnceAsync(...)`는 재사용 가치가 높은 pilot business sequence anchor로 기록했다.
- 실제 코드 기준 business flow와 error code / step / best-effort error write 보정 사항을 기록했다.
- 위험 의존성으로 `PlcChannel`, XGT raw frame, parser / classifier, SQL, hard-coded config, diagnostic result 혼재를 기록했다.
- 후보 A / B / C / D / E / F / G를 검토하고 판정을 기록했다.
- 권장안으로 후보 F -> 후보 G -> 후보 D 순서를 기록했다.
- AH-RUNTIME-45 후보와 우선순위를 기록했다.
- production code, test code, solution, csproj, project reference, package reference, source copy, adapter skeleton, commit, `ContextPublisher` 자동 publish는 수행하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
