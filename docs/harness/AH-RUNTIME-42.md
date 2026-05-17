# AH-RUNTIME-42 Closeout

## 1. Summary

AH-RUNTIME-42는 기존 `XgtDriverCore` / `FakePlc` / `XgtChannelRunner` 자산을 현재 CAAutomationHub Runtime 전환 작업에서 어떻게 재사용할 수 있는지 검토한 Boundary Review 단계다.

AH-RUNTIME-41까지 Runtime에는 다음 구간이 아직 비어 있다.

    IPollingTargetProvider
            ↓
    ChannelPollingTarget 목록
            ↓
    target별 polling operation
            ↓
    ChannelPollingResult batch
            ↓
    PollingCycleCoordinator

검토 결과, 현재 `CAAutomationHub` repo 내부에는 실제 `XgtDriverCore` / `FakePlc` / `XgtChannelRunner` project/source가 없다. 기존 XGT 자산은 sibling repo인 `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`에 존재한다.

`CAAutomationHub.Runtime` core가 기존 XGT 자산을 직접 참조하는 것은 vendor-neutral boundary를 깨므로 비권장이다. `XgtDriverCore`는 별도 Adapter / Source 계층에서 재사용하는 것이 안전하고, `FakePlc`는 production dependency가 아니라 integration test / harness dependency로 보는 것이 안전하다. `XgtChannelRunner`는 lifecycle / reconnect / polling loop 책임이 Runtime 구조와 겹칠 수 있으므로 직접 붙이지 않는다.

단, `XgtChannelRunner` 안의 `btnRunPilotOnce_Click` / `WorkStartPilotService.RunOnceAsync(...)`에는 검증된 pilot business flow 일부가 있으므로 후속 `XgtAdapter` / `PollingResultProducer` / Pilot Flow 설계의 중요한 reuse anchor로 기록한다.

이번 작업은 Boundary Review 결과를 문서화한 단계이며 production code, test code, project reference, solution, package, XGT/FakePlc/XgtChannelRunner 연결은 수정하지 않았다. `ContextPublisher` 자동 publish도 재도입하지 않았다.

## 2. Goal

AH-RUNTIME-42의 목표는 기존에 만들어 둔 XGT / FakePlc / XgtChannelRunner 관련 자산을 현재 Runtime 전환 작업에 어떻게 재사용할 수 있는지 조사하고, 참조 위치 / repo 위치 / Adapter 경계 / test 활용 전략을 정리하는 것이다.

핵심 질문은 다음이었다.

- 기존 `XgtDriverCore` / `FakePlc` / `XgtChannelRunner`는 어디에 있는가?
- 현재 repo / branch / Git history / sibling repo / package 중 어디에서 찾을 수 있는가?
- 현재 CAAutomationHub 작업 폴더에서 직접 참조 가능한가?
- 직접 참조가 어렵다면 어떤 방식으로 가져올 수 있는가?
- Runtime core가 직접 참조해도 되는가?
- Adapter / Source 계층을 별도로 두어야 하는가?
- 어떤 코드는 project reference로 쓰고, 어떤 코드는 복사 / 이식이 나은가?
- AH-RUNTIME-43 Polling Source / Result Producer Boundary Review에 어떤 정보를 넘겨야 하는가?

이번 단계는 구현이 아니라 Boundary Review다. 따라서 existing project retrieval, project reference 추가, source copy, Adapter skeleton, Driver Adapter, timer loop, Runtime 연결은 수행하지 않았다.

## 3. Background

AH-RUNTIME-40까지 Runtime에는 PLC-level / vendor-neutral `ChannelPollingTarget`과 `IPollingTargetProvider` boundary가 추가되었다.

AH-RUNTIME-41에서는 `ChannelPollingTarget` 목록에서 `ChannelPollingResult` batch가 만들어지는 production boundary를 검토했고, `PollingCycleCoordinator`를 target discovery / driver polling execution으로 확장하지 않는 것이 안전하다는 결론을 냈다.

현재 Runtime 기준 구조는 다음과 같다.

    ChannelPollingTarget
        = 무엇을 polling할지
        = PLC-level / PlcId only / vendor-neutral

    IPollingTargetProvider
        = target 목록 제공

    ChannelPollingResult
        = polling event 결과
        = vendor-neutral / PLC-level

    PollingCycleCoordinator
        = ChannelPollingResult batch를 single-writer로 publish

    PollingResultStateOrchestrator
        = ChannelPollingResult를 Runtime state update로 변환

    PollingPublishCoordinator
        = Runtime state update batch를 snapshot으로 publish

아직 비어 있는 구간은 다음이다.

    ChannelPollingTarget
            ↓
    실제 polling operation
            ↓
    ChannelPollingResult

AH-RUNTIME-42는 이 구간을 채우기 전에 기존 XGT 프로젝트 재사용 가능성을 조사했다.

## 4. 현재 repo / branch / remote 상태

현재 CAAutomationHub repo 상태:

- Repo root: `C:\AutomationHub.Rebuild\CAAutomationHub`
- Branch: `codex/local`
- Remote: `origin https://github.com/SanAndBom/CAAutomationHub`
- Current anchor: `7964be8 docs: close out AH-RUNTIME-41 polling result production boundary review`
- `git status --short`: clean

확인한 최근 commit anchor:

- `7964be8 docs: close out AH-RUNTIME-41 polling result production boundary review`
- `e01401a AH-RUNTIME-40 add polling target model skeleton`
- `15cb533 docs: close out AH-RUNTIME-39 polling target boundary review`
- `7a4ebb3 AH-RUNTIME-38 add polling cycle coordinator skeleton`

## 5. 현재 solution / project 목록

`CAAutomationHub.sln` 포함 프로젝트:

- `src\CAAutomationHub.Contracts\CAAutomationHub.Contracts.csproj`
- `src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj`
- `src\CAAutomationHub.Wpf\CAAutomationHub.Wpf.csproj`
- `tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
- `tests\CAAutomationHub.Wpf.Tests\CAAutomationHub.Wpf.Tests.csproj`

확인 결과:

- `XgtDriverCore` project는 현재 solution에 포함되어 있지 않다.
- `FakePlc` project는 현재 solution에 포함되어 있지 않다.
- `XgtChannelRunner` project는 현재 solution에 포함되어 있지 않다.

현재 project reference 상태:

- `CAAutomationHub.Runtime`은 `CAAutomationHub.Contracts`만 참조한다.
- `CAAutomationHub.Wpf`는 `CAAutomationHub.Contracts`와 `CAAutomationHub.Runtime`을 참조한다.
- Runtime test project에는 Runtime project reference가 있다.
- WPF test project에는 Contracts / WPF project reference가 있다.
- XGT / FakePlc / XgtChannelRunner project reference는 없다.

## 6. 현재 CAAutomationHub repo 안의 XGT 자산 존재 여부

현재 CAAutomationHub tracked files에는 실제 XGT project/source가 없다.

확인 결과:

- `git ls-files '*Xgt*'`: 없음
- `git ls-files '*FakePlc*'`: 없음
- `git ls-files '*ChannelRunner*'`: 없음
- `rg` / `git grep` 결과는 대부분 docs / tests / boundary 문구다.
- Runtime test에는 boundary 검증이 존재한다.
  - `tests\CAAutomationHub.Runtime.Tests\RuntimeProjectReferenceBoundaryTests.cs`

의미:

- 현재 repo만으로는 기존 XGT 프로젝트를 직접 빌드 참조할 수 없다.
- 기존 프로젝트를 사용하려면 별도 가져오기 / 참조 전략이 필요하다.
- Codex / GPT가 현재 repo에 없는 프로젝트를 자동으로 직접 참조할 수는 없다.
- Git에 존재하거나 sibling repo로 존재한다면 path / branch / repo / project file / 참조 가능성을 먼저 고정해야 한다.

## 7. XgtDriverCore 재사용 가능성

위치:

- `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`

Repo 정보:

- Branch: `main`
- Remote: `https://github.com/SanAndBom/AutomationHub_Rebuild`

Core project:

- `src\AutomationHub.XgtDriverCore\AutomationHub.XgtDriverCore.csproj`
- target framework: `net10.0` via `Directory.Build.props`

확인한 주요 API:

`IXgtSession`

- `ConnectAsync`
- `DisconnectAsync`
- `ReadAsync`
- `WriteAsync`
- `ReadStatusAsync`
- `ExchangeRawAsync`

Transport / protocol 관련:

- `TcpTransport`
- `ITransport`
- `XgtTransportOptions`
- `XgtFrameBuilder`
- `XgtFrameParser`

response / status / failure 관련:

- `XgtRawResponseClassifier`
- `XgtRawResponseInfo`
- `XgtResponseClassification`
- `XgtStatusInterpreter`
- `XgtStatusSnapshot`
- `TransportException`
- `TransportFailureKind`

재사용 판정:

- Adapter project에서 참조하기 좋다.
- Runtime core 직접 참조는 비권장이다.
- `ChannelPollingResult`를 생성하는 XGT-specific source / adapter 계층에서 사용하는 것이 안전하다.
- `TransportFailureKind`와 `XgtResponseClassification`은 `ChannelPollingFailureKind`로 매핑 가능한 후보 정보를 제공한다.
- XGT raw frame, protocol DTO, transport exception은 Runtime core에 유입되면 안 된다.

## 8. FakePlc 재사용 가능성

위치:

- `tools\AutomationHub.XgtDriverCore.FakePlc`
- `tests\AutomationHub.XgtChannelRunner.Tests\TestDoubles\FakePlcScenarioServer.cs`

확인한 scenario mode:

- `Ack`
- `Nak`
- `Malformed`
- `ForcedClose`
- `Timeout`
- `NoResponse`
- `SplitResponse`

재사용 판정:

- production Runtime dependency가 아니다.
- adapter / integration harness dependency로 적절하다.
- `ChannelPollingResult` failure 생성 테스트에 활용 가능성이 있다.
- FakePlc는 Runtime core에 직접 들어오면 안 된다.
- timeout / no response / malformed / forced close / split response 같은 failure scenario는 XGT Adapter integration harness에서 재사용 가치가 높다.

## 9. XgtChannelRunner 재사용 가능성

위치:

- `XgtChannelRunner\XgtChannelRunner.csproj`

특징:

- target framework: `net10.0-windows`
- references: `AutomationHub.XgtDriverCore`
- packages:
  - `ScottPlot.WinForms`
  - `Microsoft.Data.SqlClient`

주요 타입:

`PlcChannel`

- connect / disconnect / reconnect / status probe / recovery 상태를 소유한다.
- `IXgtSession`을 통해 raw exchange와 recovery를 수행한다.
- `TransportFailureKind` 기반 reconnect 후보 판단과 health probe를 포함한다.

`PollingWorker`

- 자체 timer loop와 polling event 발생 책임을 가진다.
- `PollingCycleCompleted` event를 발생시킨다.

`MultiChannelPollingCoordinator`

- channel별 worker orchestration을 수행한다.
- 여러 `PollingWorker`를 시작 / 중지한다.

`PollingCycleResult`

- XGT-specific result fields를 포함한다.
- `XgtResponseClassification`, `XgtCommand`, raw request / response hex, invoke id 등을 포함한다.

재사용 판정:

- 그대로 Runtime에 붙이면 lifecycle / reconnect / polling loop 책임이 Runtime / Supervisor / `PollingCycleCoordinator`와 겹친다.
- 직접 참조는 비권장이다.
- 일부 helper / mapping idea / operational insight만 Adapter 쪽에서 참고하는 것이 안전하다.
- 단, `btnRunPilotOnce_Click` / `WorkStartPilotService.RunOnceAsync(...)` pilot flow는 별도 reuse anchor로 기록한다.

## 10. XgtChannelRunner pilot flow reuse anchor

### 위치

확인된 위치:

- Project: `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\XgtChannelRunner\XgtChannelRunner.csproj`
- Class: `MainForm : Form`
- Handler: `MainForm.cs`의 `btnRunPilotOnce_Click`
- Designer binding: `MainForm.Designer.cs`
- Core service: `Services\WorkStartPilotService.cs`의 `WorkStartPilotService.RunOnceAsync(...)`

### 핵심 판단

`btnRunPilotOnce_Click`는 검증된 pilot business flow의 UI entry point다.

하지만 실제 재사용 가치가 있는 핵심 흐름은 버튼 핸들러 자체보다 `WorkStartPilotService.RunOnceAsync(...)`에 있다.

버튼 핸들러는 다음 UI / runner orchestration을 수행한다.

- 선택 채널 확인
- 연결 상태 확인
- polling 중이면 `_pollingCoordinator.StopAsync()` 호출
- `btnRunPilotOnce.Enabled = false`
- `WorkStartPilotService.RunOnceAsync(...)` 실행
- 결과를 `txtRequestResponse`, `lblLastErrorValue`, `MessageBox`에 표시
- finally에서 버튼 enable 복원 및 `UpdateChannelRuntimeUi()` 호출

### 확인한 pilot sequence

`WorkStartPilotService.RunOnceAsync(...)`의 핵심 pilot sequence는 다음이다.

1. `PlcChannel.EnsureConnectedAsync`
2. `%DB10000`부터 90 word continuous read
3. start signal word 확인
   - 기본 index: 80
4. LOT ID 1 / LOT ID 2 추출
   - word offset: 0, 10
   - length: 6
5. LOT ID 선택
   - LOT ID 1 우선
   - 없으면 LOT ID 2
6. SQL DB query
7. process data를 `%DB11000` bulk write payload로 packing
8. bulk write 실행
9. `%DB11416`에 ACK value `1` write
10. 실패 시 단계별 error code를 `%DB11410`에 best-effort write

### 확인된 테스트 / 검증 흔적

`ProcessDataPayloadBuilderTests.cs`에서 다음 경로가 테스트된 것으로 확인되었다.

- happy path
- DB not found
- bulk write NAK
- transport exception

추가로 payload packing 검증도 포함되어 있다.

- LOT ID / PROFILE ASCII packing
- single char word packing
- two-word numeric little-endian int32 packing

### 재사용 후보

Adapter / Source 계층 이식 후보:

- LOT read -> start signal 판단 -> LOT ID 선택 순서
- XGT continuous read / write request 구성
- ACK / error code write sequence
- `WorkStartPilotResult` 형태의 operation result 개념
- XGT response success / failure를 neutral result로 매핑하는 정책
- `ProcessDataPayloadBuilder`의 payload packing 규칙

테스트 시나리오 / 문서화 후보:

- LOTID read -> DB 조회 -> bulk write -> ACK
- start signal inactive -> error `1200`
- LOT ID empty -> error `2201`
- DB exception / not found / multiple rows -> `2300` / `2301` / `2302` / `2303`
- payload build fail -> `2400`
- bulk write fail -> `2501`
- ACK write fail -> `2601`
- unexpected exception -> `2999`

### 직접 Runtime 반영 금지 사유

현재 구현은 아래 의존성에 묶여 있다.

WinForms UI:

- `MainForm`
- `ToolStripButton`
- `MessageBox`
- UI log / label / button state

`XgtChannelRunner` 내부 상태:

- `_channelManager`
- `_pollingCoordinator`
- selected config

XGT-specific implementation:

- `PlcChannel`
- `XgtReadRequest`
- `XgtWriteRequest`
- `XgtFrameBuilder`
- raw request / response hex

DB dependency:

- `Microsoft.Data.SqlClient`
- default connection string

hard-coded XGT addresses:

- `%DB10000`
- `%DB11000`
- `%DB11410`
- `%DB11416`

polling interaction:

- pilot 실행 전 `_pollingCoordinator` stop 처리

따라서 이 로직을 Runtime core에 직접 복사하거나 project reference로 연결하면 안 된다.

### Closeout 기록용 결론

`XgtChannelRunner`의 `btnRunPilotOnce_Click`는 검증된 pilot business flow의 UI entry point이며, 실제 핵심 흐름은 `WorkStartPilotService.RunOnceAsync(...)`에 분리되어 있다. 이 흐름은 LOTID read -> DB query -> process payload bulk write -> ACK / error write 순서를 포함하므로 AH-RUNTIME-43 이후 `XgtAdapter`, `PollingResultProducer`, 또는 별도 Pilot Flow 설계에서 중요한 reuse candidate다. 단, 현재 구현은 WinForms event handler, `XgtChannelRunner` channel state, XGT raw frame, SQL DB, hard-coded address / config에 묶여 있으므로 Runtime core에 직접 복사하거나 project reference로 연결하지 않는다. AH-RUNTIME-42에서는 read-only anchor로만 기록하고, 후속 단계에서 Adapter 계층, integration test scenario, business flow document로 책임을 분리해 재구성한다.

## 11. WinForms Test Client / sample 재사용 가능성

위치:

- `samples\AutomationHub.XgtDriverCore.TestClient.WinForms`

재사용 후보:

- `XgtReadHelper`
- `XgtWriteHelper`
- `XgtDecodeHelper`
- `RequestHistoryService`
- `XgtTestClientService`

판정:

- production reference보다 test helper / adapter helper 이식 후보다.
- XGT-specific이므로 Runtime core에는 넣지 않는다.
- polling loop가 포함된 service는 production Runtime에 직접 부적절하다.
- request history / replay / decode helper는 integration test나 adapter-level diagnostic 도구로 참고할 수 있다.

## 12. 직접 참조 vs Adapter vs test-only 판단

Runtime core direct reference:

- 비권장
- vendor-neutral boundary 붕괴 위험
- XGT / protocol / transport exception이 Runtime core에 유입될 수 있음
- 다른 PLC vendor 확장성이 떨어짐
- `FakePlc` / `XgtChannelRunner`까지 참조가 연쇄 유입될 수 있음

별도 Adapter / Source project:

- 권장
- 예시 이름:
  - `CAAutomationHub.Runtime.Polling.Xgt`
  - `CAAutomationHub.Runtime.XgtAdapter`
  - `CAAutomationHub.Polling.Xgt`
  - `CAAutomationHub.XgtAdapter`

Adapter 책임:

- `XgtDriverCore` 참조
- `ChannelPollingTarget` 또는 adapter 전용 target model을 XGT read / status operation으로 변환
- XGT response / exception을 `ChannelPollingResult`로 변환
- `ChannelPollingFailureKind`로 vendor-neutral failure mapping 수행
- XGT raw frame, protocol DTO, transport exception을 Runtime core에 노출하지 않음

FakePlc reference:

- tests / integration harness 전용 권장
- production Runtime dependency로 보지 않음

XgtChannelRunner direct reference:

- 비권장
- responsibility overlap이 크다.
- `PollingWorker` / `MultiChannelPollingCoordinator`는 Runtime의 `PollingCycleCoordinator`, 후속 Scheduler, Supervisor 책임과 겹친다.
- 단, `WorkStartPilotService.RunOnceAsync(...)` business flow는 후속 Pilot Flow 설계의 reuse anchor로 남긴다.

## 13. Copy / 이식 후보

복사 / 이식 가능 후보:

- failure mapping helper
- status interpretation wrapper
- test fixture builder
- fake result builder
- address helper
- protocol-independent utility
- `WorkStartPilotService.RunOnceAsync(...)`의 업무 순서
- `ProcessDataPayloadBuilder` payload packing 규칙
- ACK / error write 시나리오

복사 / 이식 비권장 후보:

- `PlcChannel` 전체
- `PollingWorker` 전체
- `MultiChannelPollingCoordinator` 전체
- `XgtTestClientService`의 polling loop 전체
- `btnRunPilotOnce_Click` event handler 전체
- WinForms UI control 접근 코드
- `MessageBox` / UI log 처리

주의:

- XGT address / datatype / count는 `ChannelPollingTarget`에 넣지 않는다.
- 이런 정보는 Adapter 전용 model / config 또는 Pilot Flow 전용 configuration으로 두는 것이 안전하다.
- copy는 reference보다 빠를 수 있지만 원본 변경과 분리되어 drift가 생길 수 있다.
- XGT-specific 코드가 Runtime core에 섞이면 AH-RUNTIME-39 / 40 / 41에서 유지한 vendor-neutral target/result boundary가 깨진다.

## 14. Repo 가져오기 전략 후보

1. Sibling repo 유지

- 현재 이미 존재한다.
- 빠르다.
- 하지만 CI / Codex 재현성이 약하다.
- local path coupling이 발생한다.

2. Project reference to sibling

- 가장 빠르게 연결 가능하다.
- 하지만 local path 의존이 크다.
- 현재 sibling repo dirty 상태면 재현성 리스크가 있다.

3. Git submodule

- version pinning이 좋다.
- repo 운영 복잡도가 증가한다.
- Codex / CI 환경에서 submodule initialization 정책이 필요하다.

4. Git subtree / source copy

- 단일 repo 재현성이 좋다.
- upstream 추적 부담이 있다.
- source copy는 장기 drift 위험이 있다.

5. NuGet / package reference

- 장기적으로 가장 깔끔하다.
- package pipeline이 필요하다.
- versioning / publishing / restore 전략이 필요하다.

6. Source copy

- 초기 실험은 빠르다.
- 중복 / drift 위험이 크다.
- Runtime core에 XGT-specific 코드가 섞일 위험이 있다.

추가 관찰:

- sibling repo `AutomationHub.XgtDriverCore`는 현재 dirty 상태다.
- `tools/.../fakeplc.map.json` 수정 및 `context-events/pending` untracked 파일들이 있다.
- 이 상태를 기준으로 바로 참조 전략을 확정하면 재현성 리스크가 있다.

## 15. 권장안

권장 구조:

    CAAutomationHub.Runtime
        - ChannelPollingTarget
        - ChannelPollingResult
        - PollingCycleCoordinator
        - vendor-neutral only

    CAAutomationHub.Runtime.Polling.Xgt 또는 CAAutomationHub.Runtime.XgtAdapter
        - XgtDriverCore reference
        - XGT request / status / read execution
        - XGT response / exception -> ChannelPollingResult mapping
        - XgtChannelRunner WorkStartPilotService flow를 직접 복사하지 않고 책임 분리 후 재구성

    Integration Tests / Harness
        - FakePlc / FakePlcScenarioServer reference
        - timeout / malformed / forced close / split response 검증
        - WorkStartPilotService 기반 happy path / failure path 시나리오 재구성 가능

핵심 원칙:

- `CAAutomationHub.Runtime` core는 계속 vendor-neutral이어야 한다.
- Runtime core는 `ChannelPollingTarget`과 `ChannelPollingResult`만 알도록 유지한다.
- XGT / FakePlc / raw frame / driver exception은 Adapter / Source 계층에서 `ChannelPollingResult`로 변환한다.
- `PollingCycleCoordinator`는 target discovery / driver polling execution을 직접 맡지 않는다.
- `FakePlc`는 production dependency가 아니라 harness dependency다.
- `XgtChannelRunner`는 그대로 붙이기보다 Adapter boundary와 Pilot Flow boundary로 분해해 참고한다.

## 16. AH-RUNTIME-43 후보 및 우선순위

1. Existing Project Retrieval Plan

- sibling repo를 어떻게 고정할지 먼저 결정한다.
- dirty state / branch / remote / version pinning을 선결한다.
- CI / Codex environment 재현성을 검토한다.

2. XgtAdapter Project Boundary Review

- project name 결정
- reference direction 결정
- namespace 결정
- Adapter model 위치 결정
- Runtime core와 Adapter project의 dependency boundary 확정

3. Polling Source / Result Producer Boundary Review

- `IChannelPollingSource` / `PollingResultProducer` shape 검토
- target fetch / source execution / result batch assembly 책임 분리
- cancellation / duplicate / partial failure 정책 검토

4. Pilot Flow / Business Transaction Boundary Review

- `WorkStartPilotService.RunOnceAsync(...)`의 LOTID -> DB -> payload write -> ACK / error write 흐름을 어떻게 Runtime / Adapter / business flow 계층으로 분리할지 검토한다.
- UI event handler가 아니라 business transaction flow를 재사용 대상으로 본다.
- hard-coded address / DB / XGT raw frame dependency를 분리한다.

5. FakePlc Integration Test Boundary Review

- FP-01 ~ FP-09 scenario를 Runtime adapter harness로 어떻게 가져올지 결정한다.
- `FakePlcScenarioServer`와 existing FakePlc tool의 역할을 구분한다.

권장 우선순위는 Existing Project Retrieval Plan을 먼저 수행한 뒤, XgtAdapter Project Boundary Review와 Polling Source / Result Producer Boundary Review로 이어가는 것이다. Pilot Flow / Business Transaction Boundary Review는 `btnRunPilotOnce_Click` / `WorkStartPilotService.RunOnceAsync(...)` reuse anchor를 바탕으로 별도 단계로 분리하는 것이 안전하다.

## 17. 제외한 범위

이번 AH-RUNTIME-42에서는 다음을 하지 않았다.

- production code 수정
- test code 수정
- project / solution / package reference 추가
- checkout / copy / clone / submodule
- adapter skeleton 추가
- interface 추가
- enum 추가
- `XgtDriverCore` 직접 연결
- `FakePlc` 직접 연결
- `XgtChannelRunner` 직접 연결
- timer loop 구현
- Contracts DTO 수정
- WPF 수정
- `ContextPublisher` 자동 publish 재도입
- commit

이번 단계는 조사와 문서화만 수행했다.

## 18. 실행한 명령

AH-RUNTIME-42 Boundary Review 당시 실행한 명령:

- `git status --short`
- `git branch -a`
- `git remote -v`
- `git log --oneline -10`
- `dotnet sln list`
- `git ls-files`
- `git ls-files '*Xgt*'`
- `git ls-files '*FakePlc*'`
- `git ls-files '*ChannelRunner*'`
- `git grep -n 'XgtDriverCore'`
- `git grep -n 'FakePlc'`
- `git grep -n 'XgtChannelRunner'`
- `rg 'XgtDriverCore|FakePlc|XgtChannelRunner|IXgtSession|PollingWorker|MultiChannelPollingCoordinator' .`
- `rg '<ProjectReference|PackageReference' . -g '*.csproj'`

추가 read-only 조사:

- parent folder / sibling repo listing
- sibling repo `git status` / `git branch` / `git remote` / `git log` / `dotnet sln list`
- Runtime and sibling API files `Get-Content`
- `rg -n "btnRunPilotOnce_Click" "C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore"`
- `rg -n "RunPilotOnce|RunPilot|PilotOnce|Pilot" "C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore"`
- `MainForm.cs`, `MainForm.Designer.cs`, `WorkStartPilotService.cs`, `PilotScenarioConfig.cs`, `WorkStartPilotResult.cs`, `ProcessDataPayloadBuilder.cs`, `LotDataQueryService.cs`, 관련 tests / readme `Get-Content`

AH-RUNTIME-42 Closeout 문서 작성 후 실행한 검증:

- `git diff -- docs/harness/AH-RUNTIME-42.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-42.md`

## 19. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-42 Boundary Review 결과를 closeout 문서로 기록했다.
- 현재 CAAutomationHub repo 내부에는 실제 `XgtDriverCore` / `FakePlc` / `XgtChannelRunner` project/source가 없음을 기록했다.
- 기존 XGT 자산이 sibling repo `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`에 있음을 기록했다.
- 현재 `CAAutomationHub.sln`에 sibling repo project가 포함되어 있지 않음을 기록했다.
- `CAAutomationHub.Runtime` core가 XGT 자산을 직접 참조하면 vendor-neutral boundary가 깨질 수 있음을 기록했다.
- `XgtDriverCore`는 Adapter / Source 계층에서 재사용하는 방향이 안전하다고 기록했다.
- `FakePlc`는 production dependency가 아니라 tests / integration harness dependency로 보는 것이 안전하다고 기록했다.
- `XgtChannelRunner`는 lifecycle / reconnect / polling loop 책임이 Runtime 구조와 겹치므로 직접 참조보다 helper / mapping idea / business flow reference로 활용하는 것이 안전하다고 기록했다.
- `btnRunPilotOnce_Click` / `WorkStartPilotService.RunOnceAsync(...)` pilot flow reuse anchor를 별도 섹션으로 기록했다.
- LOTID read -> DB query -> process payload bulk write -> ACK / error write 흐름과 관련 error code scenario를 기록했다.
- 직접 참조 vs Adapter vs test-only 판단을 기록했다.
- copy / 이식 후보와 비권장 후보를 분리했다.
- repo 가져오기 전략 후보와 dirty sibling repo 재현성 리스크를 기록했다.
- AH-RUNTIME-43 후보 및 우선순위를 기록했다.
- production code, test code, project reference, solution, package, ContextPublisher는 수정하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
