# AH-WPF-02 Fake Live Update Prototype

## 1. Scenario Overview

```text
Scenario ID: AH-WPF-02
Name: Fake Live Update Prototype
Type: WPF UI / Runtime Boundary Harness
Final Status: ACCEPT
```

AH-WPF-02의 목표는 실제 PLC 통신이 아니라, `FakeDashboardRuntimeAdapter`가 호출마다 변화하는 `DashboardSnapshot`을 반환하고, `DashboardViewModel`이 주기적으로 Snapshot을 받아 기존 `PlcStatusCardViewModel`을 갱신하는 구조를 검증하는 것이다.

이 시나리오는 AH-WPF-01에서 만든 Dashboard Shell + PLC Card Static Prototype을 유지하면서, 정적 Snapshot 기반 Dashboard를 주기적으로 갱신되는 Fake Snapshot 기반 Dashboard로 확장한다.

## 2. Final Status

최종 판정은 `ACCEPT`이다.

검증 결과:

```text
Build 성공
Run 성공
1초 주기 Fake Live Update 동작 확인
PLC Card 값 갱신 확인
TX/RX 값 증가 확인
Summary Header Count 갱신 확인
Detail Pane을 열어둔 상태에서 선택 PLC 값 갱신 확인
선택 상태 유지 확인
Detail Pane 열림 상태 유지 확인
PlcCards.Clear() 미사용 확인
FakePlc / XgtDriverCore / Runtime 직접 참조 없음 확인
```

## 3. Completed Scope

완료된 범위:

```text
1. 기존 DashboardView / PlcStatusCard / PlcDetailPane 구조 유지
2. DispatcherTimer 기반 1초 주기 Fake Live Update 추가
3. IRuntimeDashboardAdapter.GetSnapshot() 반복 호출 구조 추가
4. FakeDashboardRuntimeAdapter가 호출마다 변화하는 Fake Snapshot 반환
5. PlcId 기준 기존 PlcStatusCardViewModel 갱신
6. PlcCards.Clear() 미사용
7. PlcStatusCardViewModel.UpdateSnapshot() 추가
8. 표시용 속성 기반 PropertyChanged 알림 정리
9. PlcStatusCard.xaml 표시용 속성 바인딩
10. PlcDetailPane.xaml 표시용 속성 바인딩
11. Detail Pane 열린 상태에서 선택 PLC 값 갱신
12. Summary Header Count가 카드 상태 변화에 따라 갱신
13. Build 성공
14. 로컬 실행 확인
```

## 4. Implementation Summary

현재 고정된 구현 구조:

```text
DashboardView
└── DashboardViewModel
    ├── DispatcherTimer
    ├── IRuntimeDashboardAdapter.GetSnapshot()
    ├── ApplySnapshot(DashboardSnapshot)
    └── ObservableCollection<PlcStatusCardViewModel>

IRuntimeDashboardAdapter
└── FakeDashboardRuntimeAdapter
    ├── _snapshotIndex
    ├── Fake Snapshot 변화 생성
    └── Summary Count 재집계
```

핵심 동작:

```text
1. DashboardViewModel 생성 시 Snapshot을 1회 로드한다.
2. DispatcherTimer가 1초마다 Snapshot을 다시 요청한다.
3. FakeDashboardRuntimeAdapter는 _snapshotIndex 기반으로 호출마다 다른 Fake Snapshot을 반환한다.
4. DashboardViewModel은 PlcId 기준으로 기존 PlcStatusCardViewModel을 찾는다.
5. 기존 Card ViewModel이 있으면 UpdateSnapshot()으로 값만 갱신한다.
6. 새 PlcId가 들어오면 Card ViewModel을 추가한다.
7. PlcCards.Clear()는 사용하지 않는다.
8. 선택된 PLC 객체와 Detail Pane 바인딩은 유지된다.
9. Summary Count는 Snapshot Health 값을 통해 갱신된다.
```

`PlcStatusCardViewModel`은 UI 표시용 속성을 노출한다.

```text
PlcId
PlcName
LineName
IpAddress
Port
EndpointText
PollingIntervalMs
PollingIntervalText
LastResponseMs
LastResponseText
TxPerMinute
RxPerMinute
TxRxText
ErrorCount
ConnectionState
ConnectionText
StatusText
```

`UpdateSnapshot()`은 Snapshot 교체 후 UI 표시 속성 전체에 대해 `PropertyChanged`를 발생시킨다. `PlcStatusCard.xaml`과 `PlcDetailPane.xaml`은 `Snapshot.LastResponseMs` 같은 중첩 바인딩에 의존하지 않고, ViewModel의 표시용 속성에 바인딩한다.

## 5. Repair History

### AH-WPF-02 Initial Implementation

적용 내용:

```text
DispatcherTimer 추가
FakeDashboardRuntimeAdapter가 호출마다 변화하는 Snapshot 반환
PlcId 기준 Card ViewModel 갱신 구조 추가
Detail Pane 바인딩 추가
Build 성공
```

판정:

```text
ACCEPT_WITH_RISK
```

사유:

```text
Build와 경계 준수는 확인되었지만 실제 실행 화면에서 라이브 갱신을 아직 육안 확인하지 않았다.
```

### AH-WPF-02 Repair-01

적용 내용:

```text
PlcStatusCardViewModel에 표시용 속성 추가
UpdateSnapshot()에서 UI 표시 속성 전체에 PropertyChanged 발생
PlcStatusCard.xaml / PlcDetailPane.xaml의 Snapshot.* 중첩 바인딩을 표시용 속성 바인딩으로 교체
Build 성공
로컬 실행 확인
```

판정:

```text
ACCEPT
```

## 6. Verification Evidence

실행 검증 결과:

```text
1. Build 성공
2. 앱 실행 성공
3. 약 1초마다 카드 값이 갱신됨
4. TX/RX 값이 증가함
5. Summary Header Count가 갱신됨
6. Detail Pane을 열어둔 상태에서 선택 PLC 값도 갱신됨
7. 선택 상태와 Detail Pane 상태가 유지됨
8. FakePlc / XgtDriverCore / Runtime 직접 참조 없음
```

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

경계 확인:

```text
PlcCards.Clear() 미사용
FakePlc 직접 참조 없음
XgtDriverCore 직접 참조 없음
Runtime 내부 구현체 직접 참조 없음
```

## 7. Boundary Rules Maintained

현재 유지 중인 경계 규칙:

```text
WPF UI는 FakePlc를 직접 참조하지 않는다.
WPF UI는 XgtDriverCore를 직접 참조하지 않는다.
WPF UI는 Runtime 내부 구현체를 직접 참조하지 않는다.
UI는 IRuntimeDashboardAdapter를 통해 DashboardSnapshot을 받는다.
FakeDashboardRuntimeAdapter는 Fake Snapshot만 반환한다.
실제 PLC 통신은 아직 연결하지 않는다.
실제 Polling Loop / Read / Write / Reconnect / Timeout 처리는 구현하지 않는다.
```

명시적 제외 범위:

```text
실제 PLC 연결
FakePlc 직접 연결
XgtDriverCore 직접 호출
Runtime 연결
실제 Polling Loop 구현
PLC Read / Write 구현
Reconnect 실제 구현
Timeout 실제 처리
DB 연동
Flow Runtime 연동
PLC 추가 Popup 구현
이벤트 로그 Popup 구현
사용자 관리 기능 추가
Dashboard 레이아웃 대규모 변경
```

## 8. Remaining Risks / Follow-up Items

AH-WPF-02 완료를 막는 요소는 아니지만 후속 작업 후보로 남긴다.

```text
Trend Chart Placeholder를 실제 Fake Trend 데이터와 연결
카드 선택 하이라이트 강화
Detail Pane 데이터 밀도 및 섹션 구조 개선
Timer 생명주기 정책 보강
창 전환/Unloaded 시 Dispose 정책 재검토
Fake Scenario 상태 변화 패턴 고도화
카드 미니 트렌드 실제 값 반영
장시간 실행 시 메모리/Timer 안정성 확인
```

## 9. Next Scenario Candidates

권장 다음 순서:

```text
1. AH-WPF-03: PlcEditorDialog Static Prototype
2. AH-WPF-04: RealtimeEventLogPopup Static Prototype
3. AH-WPF-05: RuntimeDashboardAdapter Skeleton
4. AH-WPF-06: Fake Trend Chart Binding Prototype
```
