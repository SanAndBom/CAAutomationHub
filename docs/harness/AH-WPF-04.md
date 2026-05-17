# AH-WPF-04 RealtimeEventLogPopup Static Prototype

## 1. Scenario Overview

```text
Scenario ID: AH-WPF-04
Name: RealtimeEventLogPopup Static Prototype
Type: WPF UI / Realtime Event Log Static Prototype
Final Status: ACCEPT
```

AH-WPF-04의 목표는 실제 로그 저장 시스템이나 Runtime Event 연결을 구현하는 것이 아니라, `FakeEventStreamService` 기반으로 실시간 이벤트 로그를 UI에서 어떻게 표시할지 검증하는 것이다.

이 화면은 "로그 관리 시스템"이 아니라 "실시간 Runtime Event 관찰창 Prototype"이다. 따라서 페이징 기반 이벤트 관리 화면이 아니라 Rolling Buffer, 최신순 이벤트 리스트, Pause / Resume, 필터, 검색, Clear View 중심의 단순한 실시간 관찰창으로 범위를 고정한다.

## 2. Final Status

최종 판정은 `ACCEPT`이다.

검증 결과:

```text
Build 성공
Run 성공
최근 이벤트 로그 Window 표시 확인
Live Indicator 표시 확인
Auto Scroll 표시 확인
Pause / Resume 동작 확인
Close 동작 확인
Severity Filter 표시 확인
PLC Filter 표시 확인
Search 표시 확인
이벤트 리스트 표시 확인
Severity Badge / Status 표시 확인
Rolling Buffer 기반 최신순 이벤트 표시 확인
Clear View 동작 확인
Export Placeholder 표시 확인
Auto Scroll ON 상태에서 최신 이벤트 최상단 이동 확인
Auto Scroll OFF 상태에서 사용자 스크롤 위치 유지 확인
창 닫기 시 예외 없음 확인
```

판정 사유:

```text
AH-WPF-04에서 요구한 실시간 이벤트 로그 Static Prototype 범위를 충족했다.
실제 Runtime / PLC / 저장소 연결 없이 Fake 이벤트 스트림 기반 UI 동작을 검증했다.
Repair-01을 통해 Auto Scroll 표시 상태가 실제 ScrollToTop 동작으로 연결되었다.
```

## 3. Completed Scope

완료된 범위:

```text
1. EventSeverity enum 추가
2. RuntimeEventLogItem UI 로그 모델 추가
3. IEventStreamService 경계 추가
4. FakeEventStreamService 추가
5. RealtimeEventLogViewModel 추가
6. RealtimeEventLogWindow.xaml 추가
7. RealtimeEventLogWindow.xaml.cs 추가
8. PlcDetailPane의 "전체 보기" 버튼 연결
9. code-behind 기반 Window 오픈
10. 향후 IDialogService 또는 ViewModel Command로 이동 예정이라는 주석 추가
11. Rolling Buffer 200건 유지
12. 최신 이벤트 상단 Insert
13. Pause / Resume
14. Severity Filter
15. PLC Filter
16. Search Filter
17. Clear View
18. Export Placeholder
19. Auto Scroll UI 표시
20. Auto Scroll ON 상태의 ScrollToTop 동작
21. Auto Scroll OFF 상태의 사용자 스크롤 위치 유지
22. Build 성공
23. 로컬 실행 확인
```

이번 작업에서 고정된 구조:

```text
src/CAAutomationHub.Wpf/Models/Dashboard/EventSeverity.cs
src/CAAutomationHub.Wpf/Models/Dashboard/RuntimeEventLogItem.cs
src/CAAutomationHub.Wpf/Services/IEventStreamService.cs
src/CAAutomationHub.Wpf/Services/FakeEventStreamService.cs
src/CAAutomationHub.Wpf/ViewModels/RealtimeEventLogViewModel.cs
src/CAAutomationHub.Wpf/Dialogs/RealtimeEventLogWindow.xaml
src/CAAutomationHub.Wpf/Dialogs/RealtimeEventLogWindow.xaml.cs
src/CAAutomationHub.Wpf/Controls/PlcDetailPane.xaml
src/CAAutomationHub.Wpf/Controls/PlcDetailPane.xaml.cs
```

## 4. Implementation Summary

현재 구현 요약:

```text
PlcDetailPane
└── "전체 보기" Button
    └── PlcDetailPane.xaml.cs OnOpenEventLogClick()
        └── RealtimeEventLogWindow.ShowDialog()

RealtimeEventLogWindow
└── RealtimeEventLogViewModel
    ├── IEventStreamService
    │   └── FakeEventStreamService
    ├── ObservableCollection<RuntimeEventLogItem> Events
    ├── Rolling Buffer 200건
    ├── Pause / Resume
    ├── Auto Scroll 표시 및 ScrollToTop 제어
    ├── Severity Filter
    ├── PLC Filter
    ├── Search Filter
    ├── Clear View
    └── Export Placeholder
```

핵심 동작:

```text
1. Dashboard에서 PLC Card를 선택하면 PlcDetailPane이 표시된다.
2. PlcDetailPane의 "전체 보기" 버튼을 클릭하면 RealtimeEventLogWindow가 열린다.
3. FakeEventStreamService는 0.8~1.5초 간격으로 결정적 패턴의 RuntimeEventLogItem을 발생시킨다.
4. RealtimeEventLogViewModel은 새 이벤트를 화면 버퍼 상단에 추가한다.
5. 화면 버퍼는 최근 200건만 유지한다.
6. Pause 상태에서는 새 이벤트를 화면 컬렉션에 추가하지 않는다.
7. Severity / PLC / Search 조건에 따라 표시 이벤트를 필터링한다.
8. Clear View는 화면 버퍼만 비우며 실제 저장소 삭제 의미가 없다.
9. Export는 실제 구현하지 않고 Placeholder 상태로 둔다.
10. Auto Scroll ON 상태에서 새 이벤트가 index 0에 추가되면 리스트를 최상단으로 이동한다.
11. Auto Scroll OFF 상태에서는 사용자의 현재 스크롤 위치를 강제로 변경하지 않는다.
12. 창이 닫힐 때 이벤트 스트림과 CollectionChanged 구독을 정리한다.
```

`PlcDetailPane.xaml.cs`의 Window 오픈 코드는 AH-WPF-04 Prototype 범위에서 허용된 임시 연결이다. 향후 실제 Dialog 흐름으로 확장할 때 `IDialogService` 또는 ViewModel Command 기반 구조로 이동할 예정이다.

## 5. Repair History

### AH-WPF-04 Initial Implementation

```text
RealtimeEventLogWindow 추가
FakeEventStreamService 추가
RealtimeEventLogViewModel 추가
Rolling Buffer / Pause / Filter / Search / Clear View / Export Placeholder 구현
PlcDetailPane "전체 보기" 버튼 연결
Build 성공
로컬 실행 전 ACCEPT_WITH_RISK 판정
```

Initial Implementation에서 확인된 사항:

```text
RuntimeDashboardEvent.cs는 수정하지 않았다.
RealtimeEventLogItem은 EventSeverity enum을 사용하도록 별도 모델로 분리했다.
IEventStreamService / FakeEventStreamService 경계를 추가했다.
실제 Runtime / PLC / FakePlc / XgtDriverCore / 저장소 연결은 구현하지 않았다.
```

### AH-WPF-04 Repair-01

문제:

```text
Auto Scroll 체크 상태에서 새 이벤트가 들어와도 리스트가 최상단으로 이동하지 않았다.
```

수정:

```text
RealtimeEventLogWindow에서 Events.CollectionChanged 감지
새 항목이 index 0에 추가될 때 Auto Scroll ON이면 Dispatcher.BeginInvoke로 ScrollToTop() 호출
Auto Scroll OFF에서는 스크롤 위치를 강제로 변경하지 않음
Closed 시 CollectionChanged 구독 해제
RealtimeEventLogViewModel에서 새 이벤트 수신 시 전체 Clear / Add 재구성 대신 Insert(0) 중심으로 조정
Build 성공
로컬 실행 확인 후 ACCEPT 판정
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

로컬 실행 검증 결과:

```text
1. Dashboard에서 PLC 선택 후 "전체 보기" 클릭 시 RealtimeEventLogWindow 표시 확인
2. 이벤트가 0.8~1.5초 간격으로 최신순 추가 확인
3. Live Indicator 표시 확인
4. Auto Scroll 표시 확인
5. Pause / Resume 동작 확인
6. Severity Filter 동작 확인
7. PLC Filter 동작 확인
8. Search Filter 동작 확인
9. 이벤트 리스트 표시 확인
10. Severity Badge / Status 표시 확인
11. Rolling Buffer 기반 최신순 이벤트 표시 확인
12. Clear View 동작 확인
13. Export Placeholder 표시 확인
14. Auto Scroll ON 상태에서 최신 이벤트 최상단 이동 확인
15. Auto Scroll OFF 상태에서 사용자 스크롤 위치 유지 확인
16. Close 동작 확인
17. 창 닫기 시 예외 없음 확인
```

범위 확인:

```text
실제 Runtime Event 연결 없음
실제 PLC 통신 이벤트 연결 없음
FakePlc 직접 연결 없음
XgtDriverCore 직접 호출 없음
Runtime 내부 구현체 직접 참조 없음
파일 로그 저장 없음
DB 로그 저장 없음
Export 실제 구현 없음
사용자별 Ack 없음
복잡한 페이징 없음
사용자 관리 기능 없음
```

## 7. Boundary Rules Maintained

현재 유지 중인 경계 규칙:

```text
WPF UI는 FakePlc를 직접 참조하지 않는다.
WPF UI는 XgtDriverCore를 직접 참조하지 않는다.
WPF UI는 Runtime 내부 구현체를 직접 참조하지 않는다.
실제 Runtime Event 연결은 아직 구현하지 않는다.
실제 PLC 통신 이벤트 연결은 아직 구현하지 않는다.
FakeEventStreamService는 UI 검증용 Fake 이벤트만 생성한다.
파일 로그 저장은 구현하지 않는다.
DB 로그 저장은 구현하지 않는다.
Export는 실제 구현하지 않고 Placeholder 상태로 둔다.
사용자별 Ack 기능은 구현하지 않는다.
복잡한 페이징은 구현하지 않는다.
사용자 관리 기능은 구현하지 않는다.
```

명시적 제외 범위:

```text
실제 Runtime Event 연결
실제 PLC 통신 이벤트 연결
FakePlc 직접 연결
XgtDriverCore 직접 호출
Runtime 내부 구현체 직접 참조
파일 로그 저장
DB 로그 저장
Export 실제 구현
사용자별 Ack
이벤트 영구 삭제
복잡한 페이징
사용자 관리 기능
실제 장애 진단 로직
AH-WPF-05 진행
```

## 8. Remaining Risks / Follow-up Items

AH-WPF-04 완료를 막는 요소는 아니지만 후속 작업 후보로 남긴다.

```text
Event Row 높이 / 밀도 조정
Severity Color Bar와 Badge 시각 정리
PLC Filter를 ComboBox로 바꿀지 검토
Search 입력 시 필터 UX 개선
Pause 상태에서 Live Indicator 색상 변경
Clear View 후 표시 개수 UX 개선
Export 버튼 disabled 스타일 명확화
한국어 / 영어 혼합 메시지 정리
실제 Runtime Event 모델과 RuntimeEventLogItem 매핑 계약 설계
장시간 실행 시 이벤트 버퍼 / Timer 안정성 확인
실제 로그 저장소와 화면 Rolling Buffer 분리 정책 문서화
```

## 9. Next Scenario Candidates

권장 다음 순서:

```text
1. AH-WPF-05: RuntimeDashboardAdapter Skeleton
2. AH-WPF-06: Fake Trend Chart Binding Prototype
3. AH-WPF-07: PlcEditorDialog Add/Edit Model Preparation
4. AH-WPF-08: Plc Configuration Save Prototype
5. AH-WPF-09: Runtime Event to Realtime Log Adapter Contract
```
