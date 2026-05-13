# AH-WPF-12 Closeout

## 1. Status

```text
ACCEPT
```

## 2. Scenario Goal

AH-WPF-12의 목표는 `PlcEditorDialog` Add/Edit 입력 validation을 보강하는 것이다.

목표 범위:

```text
PlcEditorDialog Add/Edit 입력 validation 보강
잘못된 값이 Fake Dashboard configuration에 반영되지 않도록 방지
사용자가 저장 실패 이유를 Dialog 내부에서 확인할 수 있도록 개선
```

## 3. Implemented Scope

완료된 범위:

```text
1. PlcEditorDialogViewModel에 ValidationErrors 추가
2. HasValidationErrors 추가
3. TryCreateConfiguration(out PlcDashboardConfiguration?) 추가
4. 검증 없는 ToConfiguration 직접 저장 경로 제거
5. 숫자 입력을 string 기반 Text 속성으로 전환
6. PortText 추가
7. PollingIntervalMsText 추가
8. TimeoutMsText 추가
9. ReconnectIntervalSecText 추가
10. MaxRetryCountText 추가
11. 필수값 검증 추가
12. IPv4 형식 검증 추가
13. 숫자 parse 검증 추가
14. 숫자 범위 검증 추가
15. Dialog Error Summary 추가
16. Save 클릭 시 validation 성공일 때만 DialogResult = true 처리
17. validation 실패 시 Dialog 유지
18. validation 실패 시 ResultConfiguration 생성 안 함
```

구성 관리 경계:

```text
Dialog/ViewModel은 사용자 입력 validation을 담당한다.
Save 실패 시 Dialog는 Add/Update command 흐름으로 넘어가지 않는다.
Service/FakeAdapter의 기존 PlcId integrity 방어는 유지한다.
IP/Port 중복 검증과 PLC 이름 중복 검증은 이번 범위에서 제외한다.
```

## 4. Validation Rules

필수값 정책:

```text
PLC 이름 필수
IP 주소 필수
Port 필수
Polling Interval 필수
Timeout 필수
```

선택값 정책:

```text
LineName 선택
Description 선택
```

형식 정책:

```text
IPv4만 허용
IPv6는 실패
Port / Polling / Timeout / Reconnect / Retry는 정수만 허용
소수, 문자, 공백-only 입력은 실패
```

범위 정책:

```text
Port: 1 ~ 65535
Polling Interval: 50 ~ 60000 ms
Timeout: 50 ~ 60000 ms
Reconnect Interval: 1 ~ 3600 sec
Max Retry Count: 0 ~ 100
```

보류한 정책:

```text
Timeout <= PollingInterval 논리 검증 제외
IP/Port 중복 검증 제외
PLC 이름 중복 검증 제외
```

## 5. Changed Files

최종 변경 파일:

```text
src/CAAutomationHub.Wpf/ViewModels/PlcEditorDialogViewModel.cs
src/CAAutomationHub.Wpf/Dialogs/PlcEditorDialogWindow.xaml
src/CAAutomationHub.Wpf/Dialogs/PlcEditorDialogWindow.xaml.cs
tests/CAAutomationHub.Wpf.Tests/ViewModels/PlcEditorDialogViewModelTests.cs
docs/harness/AH-WPF-12.md
```

각 파일의 역할:

```text
PlcEditorDialogViewModel.cs
- ValidationErrors / HasValidationErrors 추가
- TryCreateConfiguration(out PlcDashboardConfiguration?) 추가
- 숫자 입력을 string 기반 Text 속성으로 전환
- 필수값 / IPv4 / 정수 parse / 범위 validation 구현
- validation 성공 시에만 PlcDashboardConfiguration 생성

PlcEditorDialogWindow.xaml
- 숫자 TextBox binding을 PortText 등 string 입력 속성으로 변경
- Dialog 하단에 Error Summary 영역 추가
- HasValidationErrors가 false이면 Error Summary 숨김
- ValidationErrors를 줄 단위 목록으로 표시

PlcEditorDialogWindow.xaml.cs
- Save 클릭 시 TryCreateConfiguration 호출
- validation 실패 시 ResultConfiguration을 null로 유지하고 Dialog 유지
- validation 성공 시 ResultConfiguration 설정 후 DialogResult = true 처리

PlcEditorDialogViewModelTests.cs
- validation 성공/실패 정책 테스트 추가
- 숫자 string 입력 정책 테스트 추가
- validation 실패 시 configuration 미생성 및 errors 표시 정책 고정
```

## 6. Final Behavior

### 6.1 Invalid Save

잘못된 입력 저장 시 최종 동작:

```text
1. Save 클릭
2. validation 실행
3. 오류가 있으면 Error Summary 표시
4. Dialog는 닫히지 않음
5. ResultConfiguration은 생성되지 않음
6. Add/Update command 흐름으로 넘어가지 않음
```

### 6.2 Valid Save

올바른 입력 저장 시 최종 동작:

```text
1. Save 클릭
2. validation 성공
3. ResultConfiguration 생성
4. DialogResult = true
5. 기존 Add/Edit 흐름으로 이어짐
```

### 6.3 Cancel

Cancel 최종 동작:

```text
validation과 무관하게 기존처럼 취소 가능
DialogResult = false
configuration 변경 없음
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
103 passed
```

수동 확인:

```text
필수값 비움 시 Error Summary 표시 확인
잘못된 입력 시 Dialog 유지 확인
정상 입력 시 Save 가능 확인
Error Summary 영역은 오류가 많을 때 커지고 스크롤이 생기지만 Prototype 단계에서 수용 가능
정상 Add 기본값 상태에서는 Error Summary 없이 깔끔하게 표시 확인
Add/Edit/Delete 기존 흐름 유지 확인
```

## 8. Tests Added / Updated

추가/갱신된 테스트 정책:

```text
빈 PLC 이름 실패
빈 IP 실패
잘못된 IPv4 실패
IPv6 실패
Port 빈 값/비숫자/범위 밖 실패
Polling Interval 빈 값/비숫자/범위 밖 실패
Timeout 빈 값/비숫자/범위 밖 실패
Reconnect Interval 비숫자/범위 밖 실패
Max Retry Count 비숫자/범위 밖 실패
Max Retry Count 0 성공
유효한 Add/Edit configuration 성공
validation 실패 시 output configuration null
validation 실패 시 errors 채워짐
validation 성공 시 errors 비어 있음
```

테스트 파일:

```text
tests/CAAutomationHub.Wpf.Tests/ViewModels/PlcEditorDialogViewModelTests.cs
```

## 9. Boundary Rules

유지된 경계 규칙:

```text
실제 Runtime 연결 없음
실제 PLC 연결 없음
FakePlc 연결 없음
XgtDriverCore 참조 없음
XgtChannelRunner 참조 없음
DB 저장 없음
JSON 저장 없음
영구 저장 없음
Runtime Channel 생성/삭제 없음
실제 통신 재연결 처리 없음
INotifyDataErrorInfo 도입 없음
Field-level validation UI 없음
네트워크 접속 테스트 없음
IP/Port 중복 고급 검증 없음
PLC 이름 중복 검증 없음
Timeout vs Polling 논리 검증 없음
Communication Trend 재설계 없음
Mini Trend 재설계 없음
```

구조 경계:

```text
WPF UI는 Adapter가 Fake인지 Real인지 알지 못한다.
Dialog는 configuration 결과만 반환하고 adapter/service를 직접 수정하지 않는다.
Validation 실패 시 service Add/Update 흐름으로 넘어가지 않는다.
Add/Edit/Delete는 기존 Fake Dashboard configuration 흐름을 유지한다.
```

## 10. Known Limitations / Notes

남은 제약 및 메모:

```text
Error Summary가 여러 줄일 때 영역이 커지고 스크롤이 생길 수 있다.
현재 Prototype 단계에서는 수용 가능하다.
Field-level validation UI는 후속에서 필요 시 검토한다.
IP/Port 중복 검증과 PLC 이름 중복 검증은 후속 후보이다.
IPAddress.TryParse + InterNetwork 기반이므로 더 엄격한 dotted-quad 검증은 후속에서 검토 가능하다.
```

## 11. Next Scenario Candidates

추천 순서:

```text
1. AH-WPF-13: Duplicate Validation / Configuration Integrity
   - 중복 PLC 이름 검증
   - 중복 IP/Port 검증
   - Edit 자기 자신 예외 처리

2. AH-WPF-14: Mini Trend Role Review
   - Card Mini Trend 유지/축소/제거 판단
   - 최근 3~5분 Sparkline으로 의미 축소 가능성 검토

3. AH-WPF-15: Trend Consistency Review
   - 카드 구성과 Trend series 일치성 최종 검토
   - Overview/Selected Trend 표현 재검토

4. AH-WPF-16: Trend Refactor Review
   - AH-WPF-09에서 누적된 Trend 코드 구조 점검
   - TrendRenderControl / Fake Trend 생성 / 모델 계약 정리
```
