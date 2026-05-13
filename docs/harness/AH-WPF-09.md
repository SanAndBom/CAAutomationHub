# AH-WPF-09 Closeout

## 1. Status

```text
ACCEPT
```

## 2. Scenario Goal

AH-WPF-09의 목표는 Dashboard 하단 `CommunicationTrendChart` Placeholder를 실제 관제용 Trend Prototype으로 전환하는 것이다.

목표 범위:

```text
하단 Communication Trend Placeholder를 실제 관제용 Trend Prototype으로 전환
Runtime 연결 전 Fake 기반으로 Trend 데이터 계약, 렌더링, Overview/Selected 전환 정책 검증
외부 Chart 라이브러리 없이 WPF OnRender 기반 경량 Trend 구현
```

## 3. Final Implemented Scope

완료된 범위:

```text
1. Trend 데이터 계약 추가
2. DashboardSnapshot에 CommunicationTrendSetSnapshot 포함
3. RuntimeDashboardAdapter Skeleton에서 null 없는 Empty Trend 반환
4. FakeDashboardRuntimeAdapter에서 Fake Trend 생성
5. Overview Trend를 PLC별 RTT series overlap chart로 구현
6. Selected PLC Trend를 선택 PLC 단일 RTT trend로 구현
7. Overview / Selected PLC 전환 구현
8. 같은 PLC Card 재클릭 시 선택 해제 및 Overview 복귀 구현
9. 선택 해제 시 Detail Pane 닫힘
10. X축 최근 30분 고정
11. Y축 고정 스케일 적용
12. Warning / Congested / Error threshold 적용
13. RTT 구간별 segment color 정책 적용
14. Overview와 Selected가 동일한 차트 해석 규칙 공유
15. Compact Summary Cluster 적용
16. Axis label / threshold label / legend 표시
17. OnRender 기반 TrendRenderControl 구현
```

추가된 Trend 계약:

```text
CommunicationTrendSnapshot
- Trend 대상 단위 Snapshot
- TargetId / TargetName / IsOverview
- WarningThresholdMs / CongestedThresholdMs / ErrorThresholdMs
- Points
- WorstPlcId / WorstPlcName / WorstResponseMs
- Series
- Empty / CreateEmpty 지원

CommunicationTrendSetSnapshot
- Overview Trend
- PLC별 Trend 목록
- Empty 지원

CommunicationTrendSeries
- Overview 전용 PLC별 RTT series
- TargetId / TargetName / State / IsWorst / Points

TrendPoint
- OccurredAt
- ResponseMs
- HasError
- MarkerKind
- MarkerText

TrendMarkerKind
- None
- Warning
- Error
```

최종 Overview Trend:

```text
대상: 전체 PLC 통신 품질
표현: PLC별 RTT series overlap chart
의미: 동일 시간축 PLC별 RTT 비교
Summary: 정상 / 주의·정체 / 오류 PLC / 비활성
Worst PLC: 우측 상단 텍스트 힌트로 표시
```

최종 Selected PLC Trend:

```text
대상: 선택된 PLC
표현: 선택 PLC 단일 RTT trend
Summary: 평균 RTT / 최대 RTT / 오류 샘플 / 샘플 수
Warn / Congested / Error threshold line 표시
Marker는 제거
```

최종 threshold:

```text
WarningThresholdMs = 250
CongestedThresholdMs = 500
ErrorThresholdMs = 750
```

최종 RTT 구간별 색상 정책:

```text
정상: RTT < 250
주의: 250 <= RTT < 500
정체: 500 <= RTT < 750
오류: RTT >= 750 또는 실패
```

최종 표현 정책:

```text
Overview와 Selected PLC는 동일한 X/Y축, 동일 threshold, 동일 RTT 구간별 색상 정책을 공유한다.
차이는 표시 대상뿐이다.

Overview:
- 여러 PLC RTT series overlap

Selected PLC:
- 선택 PLC 하나의 RTT series

비활성 / Worst PLC는 범례에서 제외한다.
Worst PLC는 상태가 아니라 원인 후보 텍스트 힌트로만 유지한다.
Card 색상은 현재 PLC 상태를 표현한다.
Trend 색상은 RTT 구간 상태를 표현한다.
```

## 4. Changed Files

변경 파일:

```text
src/CAAutomationHub.Wpf/Models/Dashboard/DashboardSnapshot.cs
src/CAAutomationHub.Wpf/Models/Dashboard/CommunicationTrendSnapshot.cs
src/CAAutomationHub.Wpf/Models/Dashboard/CommunicationTrendSetSnapshot.cs
src/CAAutomationHub.Wpf/Models/Dashboard/CommunicationTrendSeries.cs
src/CAAutomationHub.Wpf/Models/Dashboard/TrendPoint.cs
src/CAAutomationHub.Wpf/Models/Dashboard/TrendMarkerKind.cs
src/CAAutomationHub.Wpf/Adapters/FakeDashboardRuntimeAdapter.cs
src/CAAutomationHub.Wpf/Adapters/RuntimeDashboardAdapter.cs
src/CAAutomationHub.Wpf/ViewModels/DashboardViewModel.cs
src/CAAutomationHub.Wpf/Controls/CommunicationTrendChart.xaml
src/CAAutomationHub.Wpf/Controls/CommunicationTrendChart.xaml.cs
src/CAAutomationHub.Wpf/Controls/TrendRenderControl.cs
src/CAAutomationHub.Wpf/Views/DashboardView.xaml
tests/CAAutomationHub.Wpf.Tests/ViewModels/DashboardViewModelTrendTests.cs
tests/CAAutomationHub.Wpf.Tests/Controls/TrendRenderControlPolicyTests.cs
docs/harness/AH-WPF-09.md
```

각 파일의 역할:

```text
DashboardSnapshot.cs
- CommunicationTrendSetSnapshot 포함
- 기존 2인자 생성자 호환 경로 유지

CommunicationTrendSnapshot.cs
- 단일 Trend 대상 Snapshot 계약
- threshold / points / overview series / worst PLC 힌트 포함
- Empty 경로 제공

CommunicationTrendSetSnapshot.cs
- Overview Trend와 PLC별 Trend 목록을 묶는 Snapshot 계약

CommunicationTrendSeries.cs
- Overview overlap chart용 PLC별 RTT series 계약

TrendPoint.cs
- Trend point 데이터 계약

TrendMarkerKind.cs
- Trend point marker 종류 계약
- 최종 UI에서는 marker 렌더링을 제거했지만 데이터 계약은 향후 이벤트성 표시 확장을 위해 유지

FakeDashboardRuntimeAdapter.cs
- Fake PLC Card Snapshot 생성
- Fake Overview / PLC별 Trend 생성
- threshold와 Worst PLC 힌트 생성
- PLC별 RTT series 생성

RuntimeDashboardAdapter.cs
- 실제 Runtime 연결 없이 Empty Trend를 포함한 null 없는 Skeleton Snapshot 반환

DashboardViewModel.cs
- CurrentCommunicationTrend 추가
- Overview / Selected PLC Trend 전환
- 같은 PLC 재클릭 선택 해제
- 선택 해제 시 Detail Pane 닫힘
- Trend Summary 계산

CommunicationTrendChart.xaml
- Placeholder 제거
- Trend title / criteria text / target / worst hint / compact summary / legend / render control 배치

CommunicationTrendChart.xaml.cs
- Trend 관련 DependencyProperty 추가
- Overview / Selected 모드별 Summary text 구성
- RTT 기준 설명 구성

TrendRenderControl.cs
- FrameworkElement.OnRender 기반 경량 chart rendering
- Overview series overlap rendering
- Selected PLC single series rendering
- 고정 X/Y scale
- threshold / axis label / segment color rendering
- RTT segment policy와 render priority policy 제공

DashboardView.xaml
- CommunicationTrendChart에 CurrentCommunicationTrend와 Summary Metrics binding 연결

DashboardViewModelTrendTests.cs
- Overview / Selected 전환 정책
- 선택 해제 / Overview 복귀 정책
- Summary Metrics
- Fake Trend 계약
- threshold / series / worst PLC 정책 테스트

TrendRenderControlPolicyTests.cs
- RTT segment color policy
- 위험도 기반 render priority policy 테스트
```

## 5. Repair History

### Initial AH-WPF-09

```text
Placeholder 제거
Fake Trend 생성
Overview/Selected 전환
Summary Metrics 표시
Warning/Error Marker 표시
OnRender 기반 TrendRenderControl 구현
```

### Repair-01

```text
같은 PLC Card 재클릭 시 선택 해제
선택 해제 시 Detail Pane 닫힘
선택 해제 시 Overview 복귀
Overview Peak 표현 개선
Marker 노이즈 감소
```

### Repair-02

```text
Overview threshold 표시 정책 조정
Worst PLC / Peak 힌트 추가
Overview Summary Label 개선
```

### Repair-03

```text
Overview를 단일 Peak line에서 PLC별 RTT overlap chart로 전환
CommunicationTrendSeries 추가
Worst PLC line 강조
상태 분포 Summary 적용
```

### Repair-04

```text
X/Y축 의미 레이블 추가
Overview threshold line 복원
Selected PLC marker 제거
Compact legend 추가
```

### Repair-05

```text
X축 최근 30분 고정
Y축 고정 스케일
Summary Metrics 좌측 compact cluster
Threshold 값을 Snapshot에서 전달받는 구조 유지
Mini Trend 후속 검토로 분리
```

### Repair-06

```text
상태별 line style 차등 적용
위험도 기반 렌더링 순서 정렬
Worst PLC overlay 개선
Legend sample 정리
```

### Repair-07

```text
RTT 상태 기준 설명 추가
CongestedThresholdMs = 500 추가
"색상=현재 PLC 상태" 혼란 해소
RTT 용어 정리
```

### Repair-08

```text
Overview와 Selected 공통 차트 해석 규칙 적용
RTT 구간별 segment color 정책 공통화
Selected PLC도 segment color 적용
비활성 / Worst PLC 범례 제거
Worst는 텍스트 힌트로만 유지
```

## 6. Final Trend Semantics

최종 의미 체계:

```text
X축: 최근 30분 시간 흐름
Y축: RTT(ms)
선 높이: RTT
선 색상: RTT 구간 상태
Card 색상: 현재 PLC 상태
Overview: 여러 PLC RTT series overlap
Selected PLC: 선택 PLC 단일 RTT series
Worst PLC: 상태가 아니라 원인 후보 텍스트 힌트
비활성: Card/Summary에서 표현, Trend 범례에서는 제외
```

RTT 구간:

```text
정상: RTT < 250ms
주의: 250ms <= RTT < 500ms
정체: 500ms <= RTT < 750ms
오류: RTT >= 750ms 또는 실패
```

Overview / Selected 공통 규칙:

```text
동일한 X축 의미
동일한 Y축 고정 스케일
동일한 Warn / Congested / Error threshold
동일한 RTT 구간별 색상 정책
동일한 범례
```

## 7. Validation

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
63 passed
0 failed
```

Runtime smoke:

```text
앱 3초 이상 실행 유지
XAML parse crash 없음
```

수동 확인:

```text
Overview가 PLC별 RTT overlap chart로 보임
Selected PLC가 단일 RTT trend로 보임
Overview와 Selected가 동일한 RTT 색상 규칙을 공유함
Error threshold 이상 구간이 빨간색으로 보임
같은 PLC 재클릭 시 Overview 복귀 확인
카드 선택/Drag Scroll/Shift Wheel 유지 확인
```

## 8. Boundary Rules

유지된 경계 규칙:

```text
외부 Chart 라이브러리 도입 없음
Runtime 연결 없음
FakePlc 연결 없음
XgtDriverCore 참조 없음
XgtChannelRunner 참조 없음
Tooltip 없음
확대/축소 없음
클릭 가능한 Marker 없음
Event Log 연동 없음
대규모 Dashboard 재설계 없음
OnRender 기반 경량 렌더링 유지
```

## 9. Known Limitations / Notes

남은 제약 및 메모:

```text
현재 Fake 10 PLC 기준에서는 카드와 Trend series의 1:1 대응을 사용자가 완전히 장악하기 어렵다.
카드 수정/삭제 기능이 생기면 사용자가 PLC 수를 줄이거나 조정하면서 Trend 일치성을 더 쉽게 검증할 수 있다.
Card 내부 Mini Trend는 아직 하단 Trend와 역할이 중복될 수 있다.
Mini Trend는 후속 시나리오에서 최근 3~5분 Sparkline으로 축소하거나 제거 여부를 검토한다.
Trend 코드는 Repair가 길게 누적되었으므로, 기능 Closeout 이후 Trend Refactor Review가 필요하다.
실제 Runtime 연결 시 threshold, scale, Worst PLC 산식은 설정/정책으로 재확정해야 한다.
```

Mini Trend 후속 검토 방향:

```text
Card Mini Trend는 최근 3~5분 Sparkline으로 의미 축소
하단 Trend는 최근 30분 상세 Trend로 역할 분리
Mini Trend 제거 가능성 검토
Mini Trend가 단순 장식이 아니라 카드 수준의 빠른 흔들림 신호인지 결정 필요
```

## 10. Next Scenario Candidates

추천 순서:

```text
1. AH-WPF-10: PLC Card Edit/Delete Interaction Prototype
   - 카드 수정
   - 카드 삭제
   - 카드 구성 조작
   - Trend series와 카드 대응 확인성 개선

2. AH-WPF-11: Trend Consistency Review
   - 카드 구성과 Trend series 일치성 확인
   - Overview/Selected Trend 최종 표현 재검토
   - Mini Trend 역할 재정의

3. AH-WPF-12: Trend Refactor Review / Refactor
   - TrendRenderControl 구조 점검
   - Fake Trend 생성 구조 점검
   - 모델/계약 정리
   - 불필요한 Repair 잔재 제거

4. AH-WPF-13: Dashboard Source Refactor Review
   - Dashboard 전체 View/ViewModel/Control 책임 분리 점검
```
