# AH-WPF-05 RuntimeDashboardAdapter Skeleton

## 1. Scenario Overview

```text
Scenario ID: AH-WPF-05
Name: RuntimeDashboardAdapter Skeleton
Type: WPF UI / Runtime Boundary Adapter Skeleton
Final Status: ACCEPT
```

AH-WPF-05의 목표는 실제 Runtime 연결을 구현하는 것이 아니라, 추후 Real Runtime 구성을 연결할 수 있도록 `RuntimeDashboardAdapter`의 Skeleton을 준비하는 것이다.

이 Adapter는 "연결자"가 아니라 "번역자" 역할을 수행할 예정이다. 즉, Runtime 내부 상태를 WPF UI에 그대로 노출하지 않고, UI가 소비할 수 있는 `DashboardSnapshot`으로 변환하는 경계를 담당한다.

현재 단계에서는 실제 Runtime 상태를 읽지 않는다. 대신 UI가 안전하게 소비할 수 있는 null 없는 빈 `DashboardSnapshot`만 반환한다.

## 2. Final Status

최종 판정은 `ACCEPT`이다.

검증 결과:

```text
Build 성공
경고 0개
오류 0개
RuntimeDashboardAdapter Skeleton 추가 확인
IRuntimeDashboardAdapter 구현 확인
GetSnapshot()에서 null 없는 빈 DashboardSnapshot 반환 확인
RuntimeHealthSnapshot 0값 구성 확인
PlcCards 빈 배열 구성 확인
SnapshotTime에 DateTimeOffset.UtcNow 사용 확인
기존 앱 동작은 계속 FakeDashboardRuntimeAdapter 사용 확인
Fake Live Update 흐름 변경 없음 확인
Runtime / XgtChannelRunner / XgtDriverCore / FakePlc 참조 없음 확인
RuntimeDashboardEvent와 RuntimeEventLogItem 결합 없음 확인
```

판정 사유:

```text
AH-WPF-05의 범위인 Real Adapter Skeleton 추가를 충족했다.
실제 Runtime / PLC / Driver 연결 없이 Adapter 계약과 빈 Snapshot 반환 경계를 준비했다.
기존 Fake Dashboard 흐름을 변경하지 않아 AH-WPF-01~04 UI 안정화 범위를 유지했다.
```

## 3. Completed Scope

완료된 범위:

```text
1. RuntimeDashboardAdapter.cs 추가
2. IRuntimeDashboardAdapter 구현
3. GetSnapshot() 구현
4. null 없는 빈 DashboardSnapshot 반환
5. RuntimeHealthSnapshot 0값 구성
6. PlcCards 빈 배열 구성
7. SnapshotTime에 DateTimeOffset.UtcNow 설정
8. Runtime 연결 TODO 성격의 Skeleton 주석 추가
9. Runtime / XgtChannelRunner / XgtDriverCore / FakePlc 미참조
10. 기존 FakeDashboardRuntimeAdapter 사용 흐름 유지
11. RuntimeDashboardEvent / RuntimeEventLogItem 미결합 유지
12. Build 성공
```

이번 작업에서 고정된 구조:

```text
src/CAAutomationHub.Wpf/Adapters/RuntimeDashboardAdapter.cs
```

수정하지 않은 구조:

```text
src/CAAutomationHub.Wpf/Adapters/IRuntimeDashboardAdapter.cs
src/CAAutomationHub.Wpf/Adapters/FakeDashboardRuntimeAdapter.cs
src/CAAutomationHub.Wpf/ViewModels/DashboardViewModel.cs
src/CAAutomationHub.Wpf/Models/Dashboard/RuntimeDashboardEvent.cs
src/CAAutomationHub.Wpf/Models/Dashboard/RuntimeEventLogItem.cs
```

## 4. Adapter Layer Meaning

현재 Dashboard Adapter 흐름:

```text
WPF Dashboard
-> IRuntimeDashboardAdapter
-> FakeDashboardRuntimeAdapter
-> Fake Snapshot / Fake Live Update
```

추후 Real Runtime Adapter 흐름:

```text
WPF Dashboard
-> IRuntimeDashboardAdapter
-> RuntimeDashboardAdapter
-> Runtime / XgtChannelRunner
-> XgtDriverCore
-> PLC
```

중요한 의미:

```text
WPF Dashboard는 Adapter 구현체가 Fake인지 Real인지 몰라야 한다.
WPF Dashboard는 DashboardSnapshot만 소비한다.
IRuntimeDashboardAdapter는 WPF와 Runtime 사이의 표시용 Snapshot 경계이다.
RuntimeDashboardAdapter는 Runtime 내부 타입을 WPF에 노출하지 않는 번역 계층이다.
```

## 5. Implementation Summary

현재 구현 요약:

```text
RuntimeDashboardAdapter
└── IRuntimeDashboardAdapter
    └── GetSnapshot()
        ├── RuntimeHealthSnapshot(0, 0, 0, 0, 0, DateTimeOffset.UtcNow)
        └── DashboardSnapshot(health, Array.Empty<PlcCardSnapshot>())
```

Skeleton 주석의 의도:

```text
AH-WPF-05는 실제 Runtime 연결이 아니다.
Runtime, XgtChannelRunner, XgtDriverCore, FakePlc를 아직 참조하지 않는다.
향후 Runtime 상태를 DashboardSnapshot으로 변환할 자리이다.
```

현재 앱 동작:

```text
DashboardViewModel 기본 생성자는 계속 FakeDashboardRuntimeAdapter를 사용한다.
FakeDashboardRuntimeAdapter의 Fake Live Update 흐름은 변경하지 않았다.
RuntimeDashboardAdapter는 생성만 되었고 실제 앱 흐름에 연결하지 않았다.
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
커밋 전: ?? src/CAAutomationHub.Wpf/Adapters/RuntimeDashboardAdapter.cs
커밋 후: 출력 없음
```

커밋:

```text
b3b6f17 feat: add runtime dashboard adapter skeleton
```

범위 확인:

```text
RuntimeDashboardAdapter.cs 외 C# / XAML / 프로젝트 파일 변경 없음
IRuntimeDashboardAdapter 변경 없음
DashboardViewModel 변경 없음
FakeDashboardRuntimeAdapter 변경 없음
RuntimeDashboardEvent 변경 없음
RuntimeEventLogItem 변경 없음
```

## 7. Boundary Rules Maintained

유지 중인 경계 규칙:

```text
WPF UI는 FakePlc를 직접 참조하지 않는다.
WPF UI는 XgtDriverCore를 직접 참조하지 않는다.
WPF UI는 XgtChannelRunner를 직접 참조하지 않는다.
WPF UI는 Runtime 내부 구현체를 직접 참조하지 않는다.
RuntimeDashboardAdapter는 아직 Runtime을 참조하지 않는다.
RuntimeDashboardAdapter는 아직 XgtChannelRunner를 참조하지 않는다.
RuntimeDashboardAdapter는 아직 XgtDriverCore를 호출하지 않는다.
Adapter DTO는 Runtime 내부 타입을 포함하지 않는다.
RuntimeDashboardEvent와 RuntimeEventLogItem은 아직 결합하지 않는다.
실제 PLC 통신은 아직 연결하지 않는다.
```

이번 작업에서 구현하지 않은 항목:

```text
실제 Runtime 연결
XgtChannelRunner 참조
XgtDriverCore 호출
FakePlc 참조
실제 PLC 연결
Polling Loop
Read / Write
Reconnect
Timeout 처리
DB 연동
Flow Runtime 연동
실제 Event Stream 구현
로그 저장소 구현
사용자 관리 기능
Dashboard UI 변경
```

## 8. Remaining Risks / Follow-up Items

AH-WPF-05 완료를 막는 요소는 아니지만 후속 작업 후보로 남는 항목:

```text
RuntimeDashboardAdapter가 실제 Runtime 상태를 언제 참조할지 결정 필요
XgtChannelRunner 상태 Snapshot Provider 계약 필요
RuntimeDashboardEvent -> RuntimeEventLogItem 변환 계층 필요
DashboardSnapshot / PlcCardSnapshot이 실제 Runtime 데이터에 충분한지 재검토 필요
실제 Runtime 연결 전 테스트용 Contract / Mapper 테스트 필요
Fake Adapter와 Real Adapter 교체 방식 정리 필요
DI 구성 또는 Adapter 선택 방식 정리 필요
XgtDriverCore는 WPF가 아니라 Runtime / Infrastructure 계층에서 참조해야 함
실제 PLC 연결은 별도 시나리오로 분리 필요
```

특이 사항:

```text
요구사항의 CapturedAt에 해당하는 현재 DTO 필드는 RuntimeHealthSnapshot.SnapshotTime이다.
따라서 AH-WPF-05 구현에서는 SnapshotTime에 DateTimeOffset.UtcNow를 사용했다.
```

## 9. Next Scenario Candidates

권장 다음 순서:

```text
1. AH-WPF-06: Runtime Event to Realtime Log Mapper Skeleton
2. AH-WPF-07: Runtime Snapshot Provider Contract
3. AH-WPF-08: Fake Trend Chart Binding Prototype
4. AH-WPF-09: PlcEditorDialog Add/Edit Model Preparation
5. AH-WPF-10: RuntimeDashboardAdapter Real Source Planning
```
