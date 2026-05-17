# AH-RUNTIME-40 Closeout

## 1. Summary

AH-RUNTIME-40은 Runtime 내부 Polling Target Model의 최소 skeleton을 추가한 단계다.

이번 변경은 실제 polling 실행, Scheduler, PollingSource, Driver Adapter, XgtDriverCore, FakePlc, WPF 연결이 아니다. Runtime이 무엇을 polling 대상으로 볼 것인지를 PLC-level / vendor-neutral 내부 모델로 표현하고, target provider boundary를 세우는 데 범위를 제한했다.

추가된 구조는 다음이다.

- `ChannelPollingTarget`: Runtime 내부 PLC-level polling target
- `IPollingTargetProvider`: target 목록을 비동기로 반환하는 Runtime 내부 provider boundary
- `RuntimeChannelPollingTargetProvider`: `RuntimeChannelRegistry.GetChannels()`를 읽어 등록 channel의 `PlcId`만 `ChannelPollingTarget`으로 투영하는 bootstrap provider

`RuntimeChannelRegistry` 자체는 수정하지 않았고, lookup / collection / state read 책임을 확장하지 않았다. `PollingCycleCoordinator`, `PollingResultStateOrchestrator`, `PollingPublishCoordinator`, Contracts DTO, WPF, XGT, FakePlc도 수정하지 않았다.

## 2. 변경 파일

- `src/CAAutomationHub.Runtime/Polling/ChannelPollingTarget.cs`
- `src/CAAutomationHub.Runtime/Polling/IPollingTargetProvider.cs`
- `src/CAAutomationHub.Runtime/Polling/RuntimeChannelPollingTargetProvider.cs`
- `tests/CAAutomationHub.Runtime.Tests/Polling/PollingTargetProviderTests.cs`
- `docs/harness/AH-RUNTIME-40.md`

## 3. 각 파일 변경 이유

`ChannelPollingTarget.cs`

- Runtime 내부 polling target identity를 PLC-level로 표현하기 위해 추가했다.
- 초기 skeleton은 `PlcId`만 가진다.
- XGT address, datatype, count, command kind, FakePlc scenario, raw frame은 포함하지 않았다.

`IPollingTargetProvider.cs`

- target discovery/source boundary를 Runtime 내부 Polling namespace에 세우기 위해 추가했다.
- signature는 `ValueTask<IReadOnlyCollection<ChannelPollingTarget>> GetTargetsAsync(CancellationToken)`이다.
- 향후 config source / DB / file source로 확장할 수 있지만, 현재 단계에서는 polling 실행 책임을 갖지 않는다.

`RuntimeChannelPollingTargetProvider.cs`

- bootstrap provider로 `RuntimeChannelRegistry`를 생성자에서 받고, `GetTargetsAsync` 호출 시 `GetChannels()`를 읽는다.
- 각 channel의 `PlcId`만 `ChannelPollingTarget`으로 투영한다.
- registry를 target configuration source나 scheduler policy source로 확장하지 않는다.

`PollingTargetProviderTests.cs`

- provider가 등록 channel을 PLC-level target으로 투영하는지 검증한다.
- 빈 registry에서 empty collection을 반환하는지 검증한다.
- null registry와 invalid `PlcId` validation을 검증한다.
- provider 호출이 registry channel count를 바꾸거나 channel state를 읽지 않는지 검증한다.
- `ChannelPollingTarget` public property가 `PlcId` 하나뿐인지 reflection으로 검증한다.

## 4. 추가한 타입 요약

`ChannelPollingTarget`

- Runtime 내부 Polling namespace 타입
- `PlcId` 단일 property
- null / empty / whitespace `PlcId`는 `ArgumentException`

`IPollingTargetProvider`

- Runtime 내부 Polling namespace interface
- target 목록 조회 boundary
- `ChannelPollingResult`를 반환하지 않음

`RuntimeChannelPollingTargetProvider`

- registry-backed bootstrap provider
- `RuntimeChannelRegistry.GetChannels()` 결과를 read-only target collection으로 projection
- polling 실행, result 생성, coordinator 호출 책임 없음

## 5. Boundary / Harness 영향

유지한 경계:

- target model은 Runtime 내부 타입이다.
- target model은 Contracts DTO가 아니다.
- target model은 WPF DTO가 아니다.
- target model은 PLC-level이다.
- target model은 vendor-neutral이다.
- `RuntimeChannelRegistry`는 수정하지 않았다.
- `PollingCycleCoordinator`는 target model을 알지 않는다.
- `PollingResultStateOrchestrator`와 `PollingPublishCoordinator`는 target provider를 알지 않는다.
- XgtDriverCore / FakePlc / Real PLC / WPF 연결은 추가하지 않았다.
- ContextPublisher 자동 publish는 재도입하지 않았다.

추가된 harness:

- `PollingTargetProviderTests`

## 6. 실행한 명령과 결과

RED 확인:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter PollingTargetProviderTests`
- 결과: 실패, 예상대로 `RuntimeChannelPollingTargetProvider` / `ChannelPollingTarget` missing type compile error

GREEN 및 validation:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj --filter PollingTargetProviderTests`
- 결과: 통과, 실패 0 / 통과 7 / 전체 7

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
- 결과: 통과, 실패 0 / 통과 127 / 전체 127

- `dotnet build CAAutomationHub.sln`
- 결과: 성공, 경고 0 / 오류 0

Boundary scan:

- `rg "XgtDriverCore|FakePlc|PollingCycleCoordinator|PollingResultStateOrchestrator|PollingPublishCoordinator|ChannelPollingResult" ...new target files...`
- 결과: match 없음

- `rg "ChannelPollingTarget|IPollingTargetProvider|RuntimeChannelPollingTargetProvider" src\CAAutomationHub.Contracts src\CAAutomationHub.Wpf tests\CAAutomationHub.Wpf.Tests`
- 결과: match 없음

## 7. 제외한 범위

이번 작업에서 하지 않은 것:

- address-level target 도입
- XGT address / datatype / count 추가
- PollingSource interface 추가
- Driver Adapter 추가
- polling operation 실행
- timer loop 구현
- PeriodicTimer 사용
- HostedService 구현
- XgtDriverCore 연결
- FakePlc 연결
- Real PLC 연결
- WPF 수정
- Contracts DTO 수정
- PollingCycleCoordinator 수정
- PollingResultStateOrchestrator 수정
- PollingPublishCoordinator 수정
- RuntimePlcChannelStateMapper 수정
- ContextPublisher 자동 publish 재도입
- commit

## 8. Self-Check

판정: ACCEPT

이유:

- PLC-level / vendor-neutral Runtime 내부 polling target model을 최소 skeleton으로 추가했다.
- `ChannelPollingTarget`은 `PlcId`만 포함한다.
- target provider boundary를 Runtime 내부 Polling namespace에 추가했다.
- bootstrap provider는 registry를 수정하지 않고 등록 channel identity만 read-only projection한다.
- RuntimeChannelRegistry 책임을 확장하지 않았다.
- publish path와 coordinator/orchestrator/publisher 경계를 수정하지 않았다.
- XGT / FakePlc / WPF / Contracts로 범위를 확장하지 않았다.
- RED / GREEN / full Runtime tests / solution build evidence를 확보했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
