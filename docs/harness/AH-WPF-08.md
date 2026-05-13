# AH-WPF-08 Closeout

## 1. Status

```text
ACCEPT
```

## 2. Scenario Goal

AH-WPF-08의 목표는 Runtime 연결 전 Pilot Dashboard의 주요 조작감을 개선하는 것이다.

목표 범위:

```text
Dashboard 카드 선택 표시를 자연스럽게 개선
카드 리스트 탐색 UX를 개선
Runtime 연결 전 Pilot Dashboard의 조작감을 향상
```

## 3. Implemented Scope

완료된 범위:

```text
1. Button 기본 외부 hover/focus/pressed 시각 효과 제거
2. PLC Card 자체 Border 기반 선택 하이라이트 적용
3. Hover와 Selected 시각 상태 분리
4. PlcStatusCardViewModel.IsSelected 추가
5. DashboardViewModel에서 SelectedPlc 변경 시 카드별 IsSelected 동기화
6. Fake Live Update 중 선택 상태 유지
7. 카드 리스트 ScrollViewer에 Drag Scroll interaction 추가
8. Shift + MouseWheel 가로 스크롤 추가
9. 선택 상태 유지 테스트 추가
```

이번 작업은 Dashboard interaction polish에 한정했으며, Communication Trend 구현은 후속 시나리오로 분리했다.

## 4. Changed Files

변경 파일:

```text
src/CAAutomationHub.Wpf/Views/DashboardView.xaml
src/CAAutomationHub.Wpf/Views/DashboardView.xaml.cs
src/CAAutomationHub.Wpf/ViewModels/DashboardViewModel.cs
src/CAAutomationHub.Wpf/Controls/PlcStatusCard.xaml
src/CAAutomationHub.Wpf/ViewModels/PlcStatusCardViewModel.cs
tests/CAAutomationHub.Wpf.Tests/ViewModels/DashboardViewModelSelectionTests.cs
```

각 파일의 역할:

```text
DashboardView.xaml
- PLC 카드 Button 전용 스타일 추가
- 기본 Button chrome 제거
- 카드 리스트 ScrollViewer에 drag/wheel interaction 이벤트 연결

DashboardView.xaml.cs
- 카드 리스트 마우스 drag scroll 처리
- click과 drag 구분
- drag 중 카드 선택 방지
- Shift + MouseWheel 가로 스크롤 처리

DashboardViewModel.cs
- SelectedPlc 변경 시 카드별 IsSelected 상태 동기화
- snapshot refresh 후에도 같은 PlcId의 선택 상태 유지

PlcStatusCard.xaml
- 카드 자체 Border 기반 Hover / Selected 스타일 적용
- Selected 상태에서 푸른색 Border와 약한 glow 표시

PlcStatusCardViewModel.cs
- IsSelected 속성 추가

DashboardViewModelSelectionTests.cs
- 카드 선택 시 단일 선택 상태 검증
- snapshot refresh 후 선택 카드 하이라이트 유지 검증
```

## 5. UI Behavior

최종 UI 동작:

```text
짧은 클릭
- PLC 카드 선택
- Detail Pane 열림 또는 선택 PLC로 갱신

선택된 카드
- 푸른색 Border 하이라이트 표시
- 약한 glow로 현재 보고 있는 카드임을 표현

Hover 카드
- 흰색/회색 계열 Border 표시
- Selected 상태와 구분됨

Drag Scroll
- 카드 리스트 영역을 잡고 좌우로 이동하면 가로 스크롤
- drag 거리 임계값을 넘기 전에는 click으로 유지
- drag 중 카드 선택 방지
- drag 완료 후 의도치 않은 카드 선택 방지

Shift + MouseWheel
- 카드 리스트에서 가로 스크롤 수행

ScrollBar
- 기존처럼 보조 수단으로 유지
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
40 passed
0 failed
```

수동 확인:

```text
카드 외부 하늘색 선택 블록 제거 확인
Hover / Selected 구분 확인
클릭 선택 정상 확인
Drag Scroll 정상 확인
Shift + MouseWheel 정상 확인
Fake Live Update 중 선택 유지 확인
Communication Trend 영역은 기존 Placeholder 상태 유지 확인
```

## 7. Boundary Rules

유지된 경계 규칙:

```text
CommunicationTrendChart 구현 없음
TrendPoint / TrendData / Trend ViewModel 추가 없음
DashboardSnapshot trend 계약 변경 없음
FakeDashboardRuntimeAdapter trend 데이터 추가 없음
실제 Runtime 연결 없음
FakePlc 연결 없음
XgtDriverCore 참조 없음
XgtChannelRunner 참조 없음
외부 UI 프레임워크 추가 없음
Dashboard 대규모 재설계 없음
```

이번 작업의 code-behind는 View 전용 interaction만 포함하며, Runtime/Adapter/비즈니스 로직을 포함하지 않는다.

## 8. Risks / Notes

남은 주의 사항:

```text
Drag Scroll은 View 전용 interaction으로 DashboardView.xaml.cs에 구현됨
비즈니스 로직, Runtime 로직, Adapter 로직은 code-behind에 포함하지 않음
향후 재사용 범위가 커지면 Attached Behavior 분리 가능
Communication Trend는 AH-WPF-09로 분리하는 것이 적절함
```

AH-WPF-08은 Dashboard interaction polish 단계이므로, 시계열 데이터 계약과 chart rendering은 후속 시나리오에서 다룬다.

## 9. Next Scenario

추천:

```text
AH-WPF-09: Communication Trend Prototype
```

목표:

```text
Fake Trend 데이터 계약 설계
전체 PLC Overview Trend 표시
선택 PLC Trend 전환
응답시간 추이 MVP 표시
```
