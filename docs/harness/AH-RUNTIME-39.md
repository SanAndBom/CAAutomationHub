# AH-RUNTIME-39 Closeout

## 1. Summary

AH-RUNTIME-39는 실제 Driver Adapter / `XgtDriverCore` / `FakePlc`를 연결하기 전에 Runtime이 무엇을 polling 대상으로 볼 것인지 검토한 Polling Target Model Boundary Review 단계다.

검토 결과, 현재 Runtime에는 `PollingTarget` / `ChannelPollingTarget` 계열 타입이 없다. 현재 Runtime publish path는 `ChannelPollingResult` batch 이후부터 닫혀 있으며, Polling Target Model은 그 앞단인 target discovery / source / driver adapter boundary에 속한다.

핵심 결론은 다음이다.

- 초기 target model은 PLC-level로 유지하는 것이 안전하다.
- target model은 vendor-neutral Runtime 내부 타입이어야 한다.
- target model은 Contracts DTO가 아니다.
- XGT address / datatype / count는 아직 Runtime target model에 넣지 않는다.
- address-level target은 Driver Adapter Boundary Review 이후에 다루는 것이 안전하다.
- `RuntimeChannelRegistry`를 target source로 직접 사용하지 않는 것이 좋다.
- 필요하다면 provider 구현체 내부에서 `RuntimeChannelRegistry.GetChannels()`를 읽어 `PlcId`만 target으로 투영하는 정도는 제한적으로 가능해 보인다.
- `PollingCycleCoordinator`는 target discovery를 알지 않고, 계속 `ChannelPollingResult` batch를 single-writer로 publish하는 boundary에 머무른다.

이번 작업은 Boundary Review 결과를 문서화한 단계이며 production code, test code, Contracts DTO, WPF, XGT, FakePlc, scheduler, source, driver adapter는 수정하지 않았다. `ContextPublisher` 자동 publish도 재도입하지 않았다.

## 2. Goal

AH-RUNTIME-39의 목표는 Polling Target Model의 책임과 경계를 구현 전에 검토하는 것이다.

핵심 질문은 다음이었다.

- `PollingCycleCoordinator`가 `ChannelPollingResult` batch를 받기 전에 어떤 대상들을 polling해야 하는가?
- 그 대상 목록은 어디서 오는가?
- Runtime 내부에서는 어떤 모델로 표현해야 하는가?
- target model과 `ChannelPollingResult`, `RuntimePlcChannelState`, `RuntimeChannelRegistry`, `PollingCycleCoordinator`의 관계는 무엇인가?

이번 단계는 구현이 아니라 Boundary Review다. 따라서 `PollingTarget` skeleton, `PollingSource` interface, Driver Adapter, timer loop, XGT / FakePlc 연결은 추가하지 않았다.

## 3. Background

AH-RUNTIME-38까지 Runtime publish path 앞단에는 manual cycle boundary가 추가되었다.

현재 흐름은 다음이다.

    외부 ChannelPollingResult batch
            ↓
    PollingCycleCoordinator
            ↓
    single-writer / overlap guard
            ↓
    IPollingResultBatchPublisher.PublishBatchAsync(...)
            ↓
    PollingResultStateOrchestrator
            ↓
    PollingPublishCoordinator
            ↓
    RuntimeSnapshot / SnapshotChanged

하지만 아직 아래 영역은 정해지지 않았다.

    PollingTarget list
            ↓
    PollingSource / Driver Adapter
            ↓
    ChannelPollingResult batch

AH-RUNTIME-39는 이 앞단의 target model을 어디에 둘지 검토했다. 실제 timer scheduler, source execution, XGT adapter, FakePlc integration은 다음 단계로 미뤘다.

## 4. 확인한 기존 PollingTarget / Registry / PLC 관련 타입

확인한 Runtime / Contracts 타입은 다음이다.

`RuntimeChannelRegistry`

- `Add`
- `TryGetChannel`
- `GetChannels`
- `GetStates`
- channel collection 관리, lookup, state read 책임을 가진다.

`IRuntimePlcChannel`

- `PlcId`
- `GetState(capturedAt)`
- read-only publish state provider boundary다.

`IWritableRuntimePlcChannel`

- `GetRuntimeState`
- `ReplaceState`
- optional writable boundary다.
- `ReplaceState`는 snapshot publish를 수행하지 않는다.

`RuntimePlcChannelState`

- Runtime-local state다.
- `IsEnabled`, `ConfiguredPollingIntervalMs`, `EffectivePollingIntervalMs` 같은 config-like field가 있다.
- 하지만 현재 의미는 target configuration source가 아니라 observable runtime state / snapshot 계약의 원천으로 보는 것이 안전하다.

`ChannelRuntimeState`

- `CAAutomationHub.Contracts`의 snapshot DTO다.
- RuntimeSnapshot publish 경계에서 사용된다.

`ChannelPollingResult`

- PLC 단위 vendor-neutral polling event result다.
- `PlcId` 기준으로 Runtime state update와 연결된다.
- XGT / FakePlc / raw frame / driver exception을 직접 노출하지 않는다.

현재 확인 결과는 다음이다.

- Runtime에는 `PollingTarget` / `ChannelPollingTarget` 계열 타입이 없다.
- Runtime에는 `PollingSource` / `PollingOperation` 계열 타입이 없다.
- `XgtDriverCore` / `FakePlc` / `XgtChannelRunner`는 아직 Runtime core에 직접 연결되지 않았다.

## 5. Registry와 target source 관계

`RuntimeChannelRegistry.GetChannels()`는 등록된 channel 목록을 snapshot copy로 반환할 수 있다.

하지만 `RuntimeChannelRegistry`의 현재 책임은 다음이다.

- channel collection 관리
- lookup
- state read

Registry가 다음 책임까지 갖게 되면 lookup-only 원칙이 흐려질 수 있다.

- polling target discovery
- enabled / disabled 정책
- interval 정책
- priority / group 정책
- polling configuration source 역할

따라서 `RuntimeChannelRegistry`를 직접 target source로 사용하는 것은 비권장이다.

단, 초기 PLC-level bootstrap 단계에서 provider 구현체 내부가 `RuntimeChannelRegistry.GetChannels()`를 읽어 `PlcId`만 `ChannelPollingTarget`으로 투영하는 것은 제한적으로 가능해 보인다. 이 경우에도 Registry 자체가 configuration source가 되는 것이 아니라, provider가 등록된 channel identity를 read-only로 투영하는 구조로 제한해야 한다.

## 6. PLC-level target vs address-level target 판단

현재 `ChannelPollingResult`와 batch invariant는 PLC 단위다.

현재 구조상 이미 다음 정책이 강하게 잡혀 있다.

- `ChannelPollingResult`는 `PlcId` 중심이다.
- `PollingResultStateOrchestrator`는 duplicate `PlcId`를 skip한다.
- `PollingCycleCoordinator`는 batch를 cycle 단위로 publish한다.
- Scheduler / Cycle boundary는 같은 PLC에 대해 cycle당 result 1개를 보장해야 한다.

따라서 초기 target model은 PLC-level이 맞다.

address-level target을 지금 도입하면 다음 문제가 생긴다.

- XGT address / datatype / count가 Runtime core에 들어올 수 있다.
- vendor-specific coupling 위험이 생긴다.
- 같은 PLC에 여러 address target이 생긴다.
- "cycle당 PLC result 1개" invariant를 다시 설계해야 한다.
- address-level 결과를 PLC-level `ChannelPollingResult`로 aggregation해야 한다.
- result aggregation / partial read failure 정책이 필요해진다.

결론:

- AH-RUNTIME-39에서는 PLC-level target을 권장한다.
- address-level target은 AH-RUNTIME-40 이후 Driver Adapter Boundary Review 또는 별도 단계로 미룬다.

## 7. vendor-neutral target 필요성

AH-RUNTIME-31 이후 `ChannelPollingResult`는 vendor-neutral Runtime 내부 polling event result로 고정되었다.

따라서 `PollingTarget`도 같은 원칙을 따라야 한다.

`PollingTarget`에 넣지 말아야 할 것은 다음이다.

- XGT raw address model
- XGT datatype
- XGT count
- XGT command kind
- FakePlc scenario id
- raw frame
- driver-specific exception
- WPF display formatting

초기 target은 최소한 다음 수준이 안전하다.

    ChannelPollingTarget
        PlcId

필요 시 이후 단계에서 다음을 신중히 검토할 수 있다.

- `Enabled`
- `Interval`
- `Timeout`
- `Priority`
- `Group`
- `Metadata`

단, interval / timeout / priority / group은 Scheduler policy와 섞일 수 있으므로 지금 바로 넣을지는 별도 판단이 필요하다.

## 8. target model과 ChannelPollingResult 관계

`PollingTarget`은 "무엇을 polling할지"다.

`ChannelPollingResult`는 "polling event 결과"다.

최소 관계는 다음이다.

    target.PlcId
            ↓
    result.PlcId

초기 PLC-level target에서는 target identity가 result에 직접 포함될 필요는 없다.

`PlcId` 매칭으로 충분하다.

## 9. target model과 RuntimePlcChannelState 관계

`RuntimePlcChannelState`는 현재 관측 가능한 Runtime state다.

`PollingTarget`은 polling 대상 configuration / source 쪽 모델이다.

따라서 target model은 `RuntimePlcChannelState`를 읽어 만드는 것이 아니라 configuration / source에서 오는 것이 안전하다.

`RuntimePlcChannelState` 안에 config-like field가 있더라도, 현재는 publish state / snapshot 계약의 관측값으로 유지한다.

권장 분리는 다음이다.

- target model: 무엇을 polling할지
- `ChannelPollingResult`: polling 결과
- `RuntimePlcChannelState`: 결과 반영 후 관측 가능한 runtime state
- `ChannelRuntimeState`: snapshot publish DTO

## 10. PollingCycleCoordinator와 target model 관계

`PollingCycleCoordinator`는 target model을 알지 않는 것이 안전하다.

이유는 다음이다.

- AH-RUNTIME-38에서 `PollingCycleCoordinator`는 manual cycle boundary로 정의되었다.
- 역할은 `ChannelPollingResult` batch를 single-writer로 publish하는 것이다.
- target discovery / source execution을 넣으면 cycle boundary 책임이 흐려진다.

따라서 `PollingCycleCoordinator`는 계속 다음 입력만 받는 것이 안전하다.

    IReadOnlyCollection<ChannelPollingResult>

target discovery와 polling execution은 `PollingSource` / Driver Adapter 또는 Scheduler 상위 레이어에서 다루는 것이 좋다.

## 11. 후보 A: RuntimeChannelRegistry를 target source로 사용 검토

판정:

- 직접 사용은 비권장
- provider 내부에서 `PlcId`만 read-only로 투영하는 것은 제한적으로 가능

장점:

- 현재 등록된 Runtime channel과 polling 대상이 일치할 수 있다.
- 별도 target source가 없어도 시작 가능하다.
- PLC-level bootstrap에는 단순하고 실용적이다.

위험:

- `RuntimeChannelRegistry`의 lookup-only 원칙이 흐려진다.
- registry에 target discovery / scheduling 책임이 섞일 수 있다.
- channel 등록 여부와 polling enabled 여부가 항상 같지 않을 수 있다.
- interval / priority / group 같은 설정을 registry가 알게 될 위험이 있다.
- address-level target으로 확장하기 어렵다.

결론:

- `RuntimeChannelRegistry`를 target source로 직접 쓰지 않는다.
- 다만 초기 provider 구현체가 `GetChannels()`를 읽어 `PlcId` target으로 투영하는 것은 검토 가능 후보로 남긴다.

## 12. 후보 B: 별도 PollingTargetSource / Provider abstraction 검토

판정:

- 권장 방향

후보 이름:

- `IPollingTargetProvider`
- `IPollingTargetSource`
- `IChannelPollingTargetProvider`
- `IRuntimePollingTargetProvider`

후보 역할:

    GetTargetsAsync(CancellationToken)
        → IReadOnlyCollection<ChannelPollingTarget>

장점:

- Registry lookup-only 원칙을 유지할 수 있다.
- Scheduler / Source가 target 목록을 명시적으로 받아 사용할 수 있다.
- enabled / interval / priority / group 같은 설정 확장이 가능하다.
- Runtime channel registry와 polling configuration source를 분리할 수 있다.

위험:

- 아직 target model이 단순한 상태에서 abstraction이 늘어난다.
- target source 구현체가 필요하다.
- 현재 단계에서는 과설계일 수 있다.

결론:

- AH-RUNTIME-40의 skeleton 후보로 적절하다.
- 단, 최소 PLC-level provider로 시작하는 것이 안전하다.

## 13. 후보 C: PLC-level PollingTarget 먼저 정의 검토

판정:

- 가장 안전한 초기 model

후보 타입:

    ChannelPollingTarget
        PlcId

장점:

- vendor-neutral이다.
- `ChannelPollingResult.PlcId`와 자연스럽게 연결된다.
- address-level 상세를 아직 끌어오지 않는다.
- Runtime publish path와 잘 맞는다.
- cycle당 PLC result 1개 invariant와 맞는다.

위험:

- 실제 polling operation을 수행하려면 address / command detail이 필요할 수 있다.
- 너무 단순하면 다음 Driver Adapter 단계에서 model이 추가로 필요할 수 있다.
- interval / timeout을 지금 넣으면 scheduler policy와 섞일 수 있다.

결론:

- AH-RUNTIME-40 전 단계에서 최소 target model로 적절하다.
- 초기 skeleton은 `PlcId`만 가진 `ChannelPollingTarget`이 안전하다.

## 14. 후보 D: Address-level PollingTarget 도입 검토

판정:

- 지금은 이르다.

후보 개념:

    PollingTarget
        PlcId
        Address
        DataType
        Count
        OperationKind
        OutputKey?

장점:

- 실제 PLC polling operation과 가깝다.
- Driver Adapter가 바로 사용할 수 있다.
- address-level 성공 / 실패 / latency를 기록할 수 있다.

위험:

- XGT address model / datatype이 Runtime core에 들어올 수 있다.
- vendor-specific coupling 위험이 있다.
- PLC-level `ChannelPollingResult`와 mismatch 가능성이 있다.
- cycle당 PLC result 1개 invariant를 다시 설계해야 한다.
- address-level detail을 `ChannelPollingResult`에 어떻게 합칠지 정해야 한다.

결론:

- AH-RUNTIME-39 / AH-RUNTIME-40 초기 skeleton 범위에서는 제외한다.
- Driver Adapter Boundary Review에서 다루는 것이 더 안전하다.

## 15. 후보 E: target model 없이 외부 ChannelPollingResult batch 유지 검토

판정:

- 현재 상태 유지는 가능하지만 AH-RUNTIME-40 이후로 가려면 부족하다.

장점:

- Runtime core를 가장 단순하게 유지한다.
- target / source / driver abstraction을 미룰 수 있다.
- fake event 기반 테스트와 잘 맞는다.

위험:

- 실제 Scheduler / Driver Adapter 단계로 갈 때 target boundary가 다시 필요하다.
- `PollingCycleCoordinator` 앞단이 계속 불명확하다.
- "무엇을 polling할 것인가" 질문이 해결되지 않는다.

결론:

- AH-RUNTIME-39에서 target invariant를 정리하는 것이 필요하다.
- AH-RUNTIME-40에서는 최소 target model skeleton을 검토하는 것이 좋다.

## 16. 권장안

AH-RUNTIME-39 권장안은 다음이다.

- 별도 target provider boundary를 둔다.
- 초기 target model은 PLC-level로 둔다.
- target model은 vendor-neutral Runtime 내부 타입으로 둔다.
- target model은 Contracts DTO가 아니다.
- target model은 WPF DTO가 아니다.
- XGT address / datatype / count는 넣지 않는다.
- address-level target은 Driver Adapter Boundary Review 이후로 미룬다.
- `PollingCycleCoordinator`는 target을 알지 않는다.
- `PollingSource` / Driver Adapter가 target을 받아 `ChannelPollingResult`를 생성하는 방향으로 이어간다.

초기 skeleton 후보:

    ChannelPollingTarget
        PlcId

    IPollingTargetProvider
        GetTargetsAsync(CancellationToken)
            → IReadOnlyCollection<ChannelPollingTarget>

선택 후보:

    RuntimeChannelPollingTargetProvider
        RuntimeChannelRegistry.GetChannels()
            → ChannelPollingTarget(PlcId)

단, `RuntimeChannelPollingTargetProvider`는 Registry를 configuration source로 만들지 않고, 등록된 channel의 `PlcId`를 read-only로 투영하는 bootstrap provider로만 취급한다.

## 17. 시간 / 책임 구분

시간 구분:

- target configured time
- cycle start time
- operation start time
- operation occurred time
- publish start time
- `RuntimeSnapshot.CapturedAt`

책임 구분:

- target model: 무엇을 polling할지
- target provider: polling target 목록을 어디서 가져올지
- source / driver adapter: target을 어떻게 polling할지
- `ChannelPollingResult`: polling event 결과
- `PollingCycleCoordinator`: result batch를 single-writer로 publish
- `PollingResultStateOrchestrator`: result를 Runtime state update로 변환
- `PollingPublishCoordinator`: state update batch를 publish
- `RuntimeSnapshot`: 현재 관측 가능한 snapshot

## 18. AH-RUNTIME-40 후보

권장 후보:

- AH-RUNTIME-40 Polling Target Model Skeleton

후보 파일:

- `src/CAAutomationHub.Runtime/Polling/ChannelPollingTarget.cs`
- `src/CAAutomationHub.Runtime/Polling/IPollingTargetProvider.cs`
- `src/CAAutomationHub.Runtime/Polling/RuntimeChannelPollingTargetProvider.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingTargetProviderTests.cs`

최소 skeleton 범위:

- PLC-level `PlcId` target
- provider가 target 목록을 read-only로 반환
- `RuntimeChannelRegistry`를 직접 target source로 쓰지 않고 provider 내부에서 `PlcId`만 투영
- no scheduler
- no timer
- no polling source
- no driver adapter
- no XGT / FakePlc / real PLC
- no WPF
- no Contracts DTO

제외 범위:

- address map
- XGT datatype / count
- FakePlc scenario
- real PLC
- WPF
- Contracts DTO
- reconnect policy
- timer loop
- driver source execution

리스크:

- abstraction 선도입 과설계 가능성
- `RuntimePlcChannelState`의 config-like field와 target config 의미 혼동 가능성
- target provider가 registry 책임을 침범할 가능성

## 19. 제외한 범위

이번 AH-RUNTIME-39에서는 다음을 하지 않았다.

- production code 수정
- test code 수정
- 문서 생성 외 작업
- `PollingTarget` skeleton class 추가
- `PollingSource` interface 추가
- Driver Adapter 추가
- timer loop 구현
- interface 확장
- enum 추가
- Contracts DTO 수정
- WPF 수정
- `XgtDriverCore` 연결
- `FakePlc` 연결
- `XgtChannelRunner` 연결
- reconnect 정책 구현
- `ContextPublisher` 자동 publish 재도입
- commit

## 20. 실행한 명령

AH-RUNTIME-39 Boundary Review 당시 실행한 명령:

- `git status --short`
- `rg "PollingTarget|ChannelPollingTarget|TargetProvider|TargetSource|PollingSource|PollingOperation" src tests`
- `rg "RuntimeChannelRegistry" src tests`
- `rg "GetStates|TryGetChannel|Add" src/CAAutomationHub.Runtime tests/CAAutomationHub.Runtime.Tests`
- `rg "PlcId|Address|DataType|Interval|Timeout|Priority|Group|Enabled" src/CAAutomationHub.Runtime src/CAAutomationHub.Contracts tests/CAAutomationHub.Runtime.Tests`
- `rg "AddressMap|Plc|PLC|Xgt|Device|DataType|Count|OutputKey" src tests docs/harness docs/context`
- `rg "PollingCycleCoordinator|PublishCycleAsync|ChannelPollingResult" src tests`
- `rg "RuntimePlcChannelState|ChannelRuntimeState" src tests`
- `rg "XgtDriverCore|FakePlc|XgtChannelRunner" src/CAAutomationHub.Runtime docs/harness docs/context`
- 관련 source / test / docs `Get-Content`
- `git log --oneline -5`
- project / file 목록 확인

테스트 / 빌드는 이번 지시가 조사 전용이라 실행하지 않았다.

AH-RUNTIME-39 Closeout 문서 작성 후 실행한 검증:

- `git diff -- docs/harness/AH-RUNTIME-39.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-39.md`

## 21. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-39 Boundary Review 결과를 closeout 문서로 기록했다.
- 현재 Runtime에 `PollingTarget` / `ChannelPollingTarget` / `PollingSource` / `PollingOperation` 계열 타입이 없음을 기록했다.
- 현재 Runtime publish path가 `ChannelPollingResult` batch 이후부터 닫혀 있음을 기록했다.
- Polling Target Model이 `ChannelPollingResult` 이전 단계의 boundary임을 기록했다.
- 초기 target model을 PLC-level, vendor-neutral Runtime 내부 타입으로 두는 결론을 기록했다.
- XGT address / datatype / count와 address-level target을 초기 Runtime target model에서 제외하는 결론을 기록했다.
- `RuntimeChannelRegistry`를 직접 target source로 쓰지 않고, 필요 시 provider 내부에서 `PlcId`만 read-only로 투영하는 제한 후보를 기록했다.
- `PollingCycleCoordinator`가 target discovery를 알지 않고 `ChannelPollingResult` batch single-writer publish boundary로 유지되어야 함을 기록했다.
- 후보 A / B / C / D / E 검토와 권장안을 기록했다.
- AH-RUNTIME-40 Polling Target Model Skeleton 후보와 제외 범위, 리스크를 기록했다.
- 시간 구분과 책임 구분을 기록했다.
- `ContextPublisher` 자동 publish 미사용 정책을 유지했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
