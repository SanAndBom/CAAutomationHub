# AH-WPF-07 Closeout

## 1. Status

```text
ACCEPT
```

## 2. Scenario Goal

AH-WPF-07의 목표는 `RuntimeDashboardEvent`에서 `RuntimeEventLogItem`으로 변환되는 Mapper 경계의 정책을 단위 테스트로 고정하는 것이다.

이번 작업은 실제 Runtime 연결 전에 `RuntimeEventLogItemMapper`의 severity/default/status/compatibility 정책을 회귀 테스트로 보호하기 위한 테스트 하네스 구축이다.

고정 대상 흐름:

```text
RuntimeDashboardEvent
-> RuntimeEventLogItemMapper
-> RuntimeEventLogItem
```

## 3. Implemented Scope

완료된 범위:

```text
1. xUnit 테스트 프로젝트 추가
2. RuntimeEventLogItemMapperTests 추가
3. Severity 매핑 정책 테스트 추가
4. PlcName / Category / Message 기본값 정책 테스트 추가
5. Status 기본값 정책 테스트 추가
6. RuntimeDashboardEvent 기존 생성 방식 호환성 테스트 추가
7. OccurredAt 보존 테스트 추가
8. Map(null!) 방어 동작 테스트 추가
9. 솔루션에 테스트 프로젝트 포함
10. 템플릿 UnitTest1.cs 잔재 제거
```

이번 작업은 Mapper 정책을 변경하지 않고, 현재 정책을 테스트로 고정하는 범위에 한정했다.

## 4. Changed Files

변경 파일:

```text
CAAutomationHub.sln
tests/CAAutomationHub.Wpf.Tests/CAAutomationHub.Wpf.Tests.csproj
tests/CAAutomationHub.Wpf.Tests/Mappers/RuntimeEventLogItemMapperTests.cs
```

수정하지 않은 파일:

```text
src/CAAutomationHub.Wpf/Mappers/RuntimeEventLogItemMapper.cs
src/CAAutomationHub.Wpf/Mappers/IRuntimeEventLogItemMapper.cs
src/CAAutomationHub.Wpf/Models/Dashboard/RuntimeDashboardEvent.cs
src/CAAutomationHub.Wpf/Models/Dashboard/RuntimeEventLogItem.cs
src/CAAutomationHub.Wpf/Models/Dashboard/EventSeverity.cs
src/CAAutomationHub.Wpf/ViewModels/*
src/CAAutomationHub.Wpf/Views/*
src/CAAutomationHub.Wpf/Dialogs/*
```

## 5. Fixed Mapper Policies

Severity 매핑 정책:

```text
Critical / Error / Fatal / ERROR -> EventSeverity.Critical
Warning / Warn / " warning " -> EventSeverity.Warning
Info / Information / Unknown / empty / whitespace -> EventSeverity.Info
```

PlcName 기본값 정책:

```text
PlcName 우선
PlcName이 없으면 Source 사용
PlcName과 Source가 모두 없으면 "Runtime" 사용
PlcName이 whitespace이고 Source가 있으면 Source 사용
```

Category 기본값 정책:

```text
Category가 있으면 Category 사용
Category가 null / empty / whitespace이면 "Runtime" 사용
```

Message 기본값 정책:

```text
Message가 있으면 Message 사용
Message가 null / empty / whitespace이면 "Runtime event received." 사용
```

Status 기본값 정책:

```text
Status 명시값 우선
Status 미지정 시 Severity 기준 기본값 사용
  Critical -> Open
  Warning -> Watch
  Info -> Live
```

호환성 및 방어 동작:

```text
기존 RuntimeDashboardEvent 생성 방식 유지
new RuntimeDashboardEvent(occurredAt, "Info", "message") 사용 가능
OccurredAt 값 보존
Map(null!) -> ArgumentNullException
```

## 6. Validation

Build 확인:

```powershell
dotnet build CAAutomationHub.sln
```

결과:

```text
성공
warning 0
error 0
```

Test 확인:

```powershell
dotnet test CAAutomationHub.sln
```

결과:

```text
성공
총 38개 테스트 통과
실패 0
건너뜀 0
```

Git 확인:

```powershell
git status --short
```

결과:

```text
 M CAAutomationHub.sln
?? tests/
```

## 7. Boundary Rules

유지된 경계 규칙:

```text
WPF UI는 Runtime을 직접 참조하지 않음
WPF UI는 FakePlc를 직접 참조하지 않음
WPF UI는 XgtDriverCore를 직접 참조하지 않음
WPF UI는 XgtChannelRunner를 직접 참조하지 않음
테스트 프로젝트는 CAAutomationHub.Wpf 프로젝트만 참조
실제 PLC/Runtime 연결 없음
UI 변경 없음
프로덕션 Mapper 정책 변경 없음
```

이번 작업에서 구현하지 않은 항목:

```text
실제 Runtime Event Stream 연결
FakePlc 연결
XgtDriverCore 참조
XgtChannelRunner 참조
PLC Read / Write
Runtime Snapshot Provider 구현
RealtimeEventLogWindow UI 변경
Mapper 정책 변경
```

## 8. Risks / Notes

남은 주의 사항:

```text
RuntimeDashboardEvent.Message는 non-null string이지만 null! 방어 테스트를 통해 현재 Mapper 방어 동작을 고정함
whitespace 처리도 정책으로 고정됨
이후 Runtime Event Stream 연결 시 이 Mapper 테스트가 회귀 방지 역할을 함
```

AH-WPF-07은 테스트 하네스 구축 단계이므로 Runtime 이벤트의 실제 발생원, 연결 방식, 스트림 수명 주기는 후속 시나리오에서 다룬다.

## 9. Next Scenario Candidates

후속 시나리오 후보:

```text
1. AH-WPF-08: Runtime Snapshot Provider Contract
2. AH-WPF-09: Fake Trend Chart Binding Prototype
3. AH-WPF-10: Runtime Event Stream Adapter Skeleton
4. AH-WPF-11: PlcEditorDialog Add/Edit Model Preparation
```
