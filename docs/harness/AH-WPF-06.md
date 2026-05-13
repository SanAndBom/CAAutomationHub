# AH-WPF-06 Runtime Event to Realtime Log Mapper Skeleton

## 1. Scenario Overview

```text
Scenario ID: AH-WPF-06
Name: Runtime Event to Realtime Log Mapper Skeleton
Type: WPF UI / Runtime Event Boundary Mapper Skeleton
Final Status: ACCEPT
```

AH-WPF-06의 목표는 실제 Runtime 이벤트를 `RealtimeEventLogWindow`에 연결하는 것이 아니다.

이번 작업의 목적은 Adapter 경계 이벤트인 `RuntimeDashboardEvent`를 UI 로그 표시 모델인 `RuntimeEventLogItem`으로 변환하는 Mapper 계약과 Skeleton을 준비하는 것이다.

이 작업은 Runtime 이벤트 언어와 UI 로그 표시 언어를 분리하기 위한 중간 번역 계층이다.

현재 고정된 개념 흐름:

```text
RuntimeDashboardEvent
-> RuntimeEventLogItemMapper
-> RuntimeEventLogItem
-> RealtimeEventLogViewModel
-> RealtimeEventLogWindow
```

## 2. Final Status

최종 판정은 `ACCEPT`이다.

검증 결과:

```text
Build 성공
경고 0개
오류 0개
RuntimeDashboardEvent optional 필드 보강 확인
기존 3개 인자 생성 방식 유지 확인
IRuntimeEventLogItemMapper 추가 확인
RuntimeEventLogItemMapper 추가 확인
RuntimeDashboardEvent -> RuntimeEventLogItem 변환 경계 생성 확인
Severity 문자열 -> EventSeverity enum 변환 구현 확인
기본값 처리 구현 확인
FakePlc / XgtDriverCore / XgtChannelRunner 직접 참조 없음 확인
실제 Runtime 연결 없음 확인
```

판정 사유:

```text
AH-WPF-06의 범위인 Mapper 계약과 Skeleton 추가를 충족했다.
RuntimeDashboardEvent와 RuntimeEventLogItem을 통합하지 않고 변환 경계를 분리했다.
실제 Runtime / PLC / Driver 연결 없이 UI 로그 표시 모델로 변환하는 중간 계층만 준비했다.
```

## 3. Completed Scope

완료된 범위:

```text
1. RuntimeDashboardEvent optional 필드 보강
2. IRuntimeEventLogItemMapper 추가
3. RuntimeEventLogItemMapper 추가
4. RuntimeDashboardEvent -> RuntimeEventLogItem 변환 구현
5. Severity 문자열 매핑 구현
6. PlcName / Source fallback 처리
7. Category 기본값 처리
8. Message 기본값 처리
9. Status 기본값 처리
10. RuntimeDashboardEvent와 RuntimeEventLogItem 미통합 유지
11. Runtime / XgtChannelRunner / XgtDriverCore / FakePlc 미참조
12. Build 성공
```

이번 작업에서 고정된 구조:

```text
src/CAAutomationHub.Wpf/Models/Dashboard/RuntimeDashboardEvent.cs
src/CAAutomationHub.Wpf/Mappers/IRuntimeEventLogItemMapper.cs
src/CAAutomationHub.Wpf/Mappers/RuntimeEventLogItemMapper.cs
```

수정하지 않은 구조:

```text
src/CAAutomationHub.Wpf/Models/Dashboard/RuntimeEventLogItem.cs
src/CAAutomationHub.Wpf/Models/Dashboard/EventSeverity.cs
src/CAAutomationHub.Wpf/ViewModels/RealtimeEventLogViewModel.cs
src/CAAutomationHub.Wpf/Dialogs/RealtimeEventLogWindow.xaml
src/CAAutomationHub.Wpf/Services/FakeEventStreamService.cs
```

## 4. Mapper Contract

Mapper 계약:

```text
IRuntimeEventLogItemMapper
└── RuntimeEventLogItem Map(RuntimeDashboardEvent dashboardEvent)
```

계약의 의미:

```text
RuntimeDashboardEvent는 Adapter 경계 이벤트이다.
RuntimeEventLogItem은 UI 로그 표시 모델이다.
Runtime은 RuntimeEventLogItem을 직접 만들지 않는다.
UI는 RuntimeDashboardEvent를 직접 알 필요가 없다.
Mapper가 두 모델 사이의 변환을 담당한다.
```

`RuntimeDashboardEvent`는 Mapper에 필요한 최소 UI 경계용 문자열/기본 타입만 갖는다.

현재 구조:

```text
OccurredAt
Severity
Message
Source
Category
PlcId
PlcName
Status
```

`Source`, `Category`, `PlcId`, `PlcName`, `Status`는 optional 필드이다. 기존 3개 인자 생성 방식은 유지된다.

## 5. Mapping Policy

Severity 매핑:

```text
Critical / Error / Fatal -> Critical
Warning / Warn -> Warning
Info / Information -> Info
null / empty / unknown -> Info
```

Severity 문자열 비교는 대소문자를 구분하지 않는다.

필드 매핑:

```text
OccurredAt: source.OccurredAt
Severity: 문자열 severity를 EventSeverity로 변환
PlcName: source.PlcName ?? source.Source ?? "Runtime"
Category: source.Category ?? "Runtime"
Message: 빈 값이면 "Runtime event received."
Status:
  source.Status가 있으면 사용
  없으면 Severity 기준 기본값 사용
    Critical -> "Open"
    Warning -> "Watch"
    Info -> "Live"
```

기본값 처리 기준:

```text
PlcName이 없고 Source도 없으면 Runtime
Category가 없으면 Runtime
Message가 없으면 Runtime event received.
Status가 없으면 Severity에 맞는 UI 표시 상태를 사용
알 수 없는 Severity는 Info로 낮춰 표시 안정성을 우선
```

## 6. Verification Evidence

Build 확인:

```powershell
dotnet build CAAutomationHub.sln
```

결과:

```text
Build 성공
경고 0개
오류 0개
```

Git 확인:

```powershell
git status --short
```

결과:

```text
커밋 전:
 M src/CAAutomationHub.Wpf/Models/Dashboard/RuntimeDashboardEvent.cs
?? src/CAAutomationHub.Wpf/Mappers/

커밋 후:
출력 없음
```

커밋:

```text
f573606 feat: add runtime event to realtime log mapper skeleton
```

범위 확인:

```text
RealtimeEventLogWindow UI 변경 없음
FakeEventStreamService 대규모 변경 없음
RuntimeEventLogItem 변경 없음
EventSeverity 변경 없음
실제 Runtime 이벤트 스트림 연결 없음
FakePlc / XgtDriverCore / XgtChannelRunner 참조 없음
```

## 7. Boundary Rules Maintained

유지 중인 경계 규칙:

```text
RuntimeDashboardEvent는 Adapter 경계 이벤트이다.
RuntimeEventLogItem은 UI 로그 표시 모델이다.
Runtime은 RuntimeEventLogItem을 직접 만들지 않는다.
UI는 RuntimeDashboardEvent를 직접 알 필요가 없다.
Mapper가 두 모델 사이의 변환을 담당한다.
Mapper는 WPF Control을 참조하지 않는다.
Mapper는 Runtime 내부 구현체를 참조하지 않는다.
Mapper는 FakePlc, XgtDriverCore, XgtChannelRunner를 참조하지 않는다.
실제 Runtime 이벤트 스트림 연결은 아직 구현하지 않는다.
```

이번 작업에서 구현하지 않은 항목:

```text
실제 Runtime 연결
XgtChannelRunner 참조
XgtDriverCore 호출
FakePlc 참조
실제 PLC 연결
Polling Loop
PLC Read / Write
Reconnect / Timeout 실제 처리
DB 연동
파일 로그 저장
Export 실제 구현
사용자별 Ack
RealtimeEventLogWindow 대규모 UI 변경
AH-WPF-07 진행
```

## 8. Remaining Risks / Follow-up Items

AH-WPF-06 완료를 막는 요소는 아니지만 후속 작업 후보로 남는 항목:

```text
Mapper 단위 테스트 인프라 없음
PlcId는 RuntimeDashboardEvent에는 있지만 RuntimeEventLogItem에는 아직 출력되지 않음
실제 Runtime 이벤트 연결 전 Mapper 테스트 필요
RuntimeDashboardEvent 필드가 실제 Runtime 이벤트에 충분한지 후속 검토 필요
RuntimeEventLogItem에 PlcId 추가 여부 후속 검토
Runtime Event Stream과 FakeEventStreamService 통합 방식 후속 검토
```

주의할 점:

```text
PlcId는 현재 Mapper 입력 경계에만 존재한다.
RuntimeEventLogItem의 UI 표시 구조에는 PlcId 필드가 아직 없다.
따라서 AH-WPF-06에서는 PlcId를 출력 모델로 전달하지 않는다.
```

## 9. Next Scenario Candidates

권장 다음 순서:

```text
1. AH-WPF-07: Mapper Unit Test Harness
2. AH-WPF-08: Runtime Snapshot Provider Contract
3. AH-WPF-09: Fake Trend Chart Binding Prototype
4. AH-WPF-10: Runtime Event Stream Adapter Skeleton
5. AH-WPF-11: PlcEditorDialog Add/Edit Model Preparation
```
