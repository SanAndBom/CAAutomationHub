# AH-RUNTIME-59 Closeout - Validator Interface Skeleton

## 1. Summary

AH-RUNTIME-59는 AH-RUNTIME-58에서 추가한 neutral `FlowDefinitionCandidate` model을 입력으로 받는 validator interface skeleton만 추가한 작업이다.

interface 이름은 `IFlowDefinitionValidator`로 정했다. signature는 extension provider나 external registry check 가능성을 열어두기 위해 async 형태인 `ValueTask<ValidationResult> ValidateAsync(FlowDefinitionCandidate candidate, CancellationToken cancellationToken = default)`로 정했다.

이번 단계에서는 실제 validator rule implementation, parser, JSON schema validation, extension provider, XGT / DB / payload validation을 추가하지 않았다.

Self-Check 판정은 `ACCEPT`다.

## 2. 변경 파일 목록

- `src/CAAutomationHub.FlowDefinitions/CAAutomationHub.FlowDefinitions.csproj`
- `src/CAAutomationHub.FlowDefinitions/IFlowDefinitionValidator.cs`
- `tests/CAAutomationHub.Runtime.Tests/FlowDefinitions/FlowDefinitionValidatorInterfaceTests.cs`
- `docs/harness/AH-RUNTIME-59.md`

## 3. Interface 위치

위치:

- `src/CAAutomationHub.FlowDefinitions/IFlowDefinitionValidator.cs`

namespace:

- `CAAutomationHub.FlowDefinitions`

선택 이유:

- interface 입력 타입인 `FlowDefinitionCandidate`와 같은 Runtime-neutral definition boundary에 둔다.
- Runtime project가 validator contract owner가 되지 않는다.
- Contracts는 `ValidationResult` result shape만 유지하고, flow definition input/interface drift를 흡수하지 않는다.

project reference:

- `CAAutomationHub.FlowDefinitions`에서 `CAAutomationHub.Contracts`를 참조한다.
- 이유는 `IFlowDefinitionValidator`가 `ValidationResult`를 반환하기 때문이다.
- Runtime project reference는 변경하지 않았다.

## 4. Signature

선택한 signature:

```csharp
ValueTask<ValidationResult> ValidateAsync(
    FlowDefinitionCandidate candidate,
    CancellationToken cancellationToken = default);
```

sync/async 판단:

- 초기 skeleton은 sync가 단순하지만, validator는 향후 external registry check, extension provider result merge, async preflight source 확인이 필요할 수 있다.
- interface는 한 번 노출되면 변경 비용이 크므로 async 형태가 안전하다.
- `Task`보다 allocation 여지를 줄이기 위해 `ValueTask<ValidationResult>`를 선택했다.
- 실제 rule implementation은 추가하지 않았다.

## 5. Tests

추가 테스트:

- interface가 `ValidateAsync` public instance method를 노출하는지 확인
- return type이 `ValueTask<ValidationResult>`인지 확인
- 첫 번째 parameter가 `FlowDefinitionCandidate`인지 확인
- 두 번째 parameter가 optional `CancellationToken`인지 확인
- fake validator가 `ValidationResult.Success`를 반환할 수 있는지 확인
- fake validator가 issue 포함 `ValidationResult`를 반환할 수 있는지 확인

TDD RED evidence:

- interface 추가 전 `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj` 실행
- 결과: 실패
- 주요 실패:
  - `IFlowDefinitionValidator` type 없음

TDD GREEN evidence:

- interface와 Contracts project reference 추가 후 같은 test command 통과
- 실패 0, 통과 142, 건너뜀 0

## 6. Raw Input 금지 유지 여부

유지했다.

interface는 다음을 받지 않는다.

- raw JSON string
- file path
- object
- dynamic
- dictionary
- XGT / DB / payload type

interface는 다음만 받는다.

- `FlowDefinitionCandidate`
- optional `CancellationToken`

interface는 다음을 반환한다.

- `ValidationResult`

## 7. 금지 범위 유지 여부

이번 작업에서 다음을 하지 않았다.

- 실제 rule implementation
- StructuralValidator 구현
- BindingValidator 구현
- PolicyValidator 구현
- raw JSON parser 구현
- JSON schema validation 구현
- extension provider 구현
- XGT validation 구현
- DB validation 구현
- payload validation 구현
- executor 구현
- WPF wiring
- Runtime core owner화

## 8. Validation

실행한 명령과 결과:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
  - RED: `IFlowDefinitionValidator` 부재로 compile 실패 확인
  - GREEN: 실패 0, 통과 142, 건너뜀 0
  - 최종 실행: 실패 0, 통과 142, 건너뜀 0

- `dotnet build CAAutomationHub.sln`
  - 경고 0, 오류 0

- `git diff --check`
  - exit code 0
  - whitespace error 없음
  - line-ending 정규화 후 최종 출력 없음

- `rg -n "Json|JSON|Xgt|FakePlc|Db|DB|Sql|SQL|Executor|Wpf|RuntimeSnapshot|ChannelPollingResult" src\CAAutomationHub.FlowDefinitions\IFlowDefinitionValidator.cs tests\CAAutomationHub.Runtime.Tests\FlowDefinitions\FlowDefinitionValidatorInterfaceTests.cs`
  - match 없음

- `git status --short`
  - ` M CAAutomationHub.sln`
  - ` M tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj`
  - `?? docs/harness/AH-RUNTIME-57.md`
  - `?? docs/harness/AH-RUNTIME-58.md`
  - `?? src/CAAutomationHub.FlowDefinitions/`
  - `?? tests/CAAutomationHub.Runtime.Tests/FlowDefinitions/`

## 9. Boundary

유지한 boundary:

- validator interface는 raw JSON을 받지 않는다.
- validator interface는 neutral `FlowDefinitionCandidate`를 받는다.
- validator interface는 `ValidationResult`를 반환한다.
- `CAAutomationHub.FlowDefinitions`는 Contracts만 참조한다.
- Runtime project reference는 변경하지 않았다.
- Runtime core는 validator implementation을 소유하지 않는다.
- parser / executor / extension provider는 추가하지 않았다.

## 10. STOP CHECK

STOP CHECK 결과:

- validator interface가 string json 또는 file path를 받게 됨: 발생하지 않음
- interface가 object / dynamic / dictionary를 받게 됨: 발생하지 않음
- interface가 XGT / DB / payload type을 참조함: 발생하지 않음
- rule implementation을 추가하려 함: 발생하지 않음
- parser / executor를 추가하려 함: 발생하지 않음
- FlowDefinitionCandidate가 불안정해 interface를 고정하기 어려움: 발생하지 않음

따라서 AH-RUNTIME-60 audit로 진행 가능하다.

## 11. Self-Check

판정: ACCEPT

이유:

- `IFlowDefinitionValidator` interface skeleton만 추가했다.
- input은 neutral `FlowDefinitionCandidate`로 제한했다.
- output은 기존 `ValidationResult`로 제한했다.
- async signature 판단을 문서화했다.
- fake validator compile / behavior tests를 추가했다.
- RED / GREEN evidence를 확보했다.
- test와 solution build가 통과했다.
- 금지 키워드 scan에서 interface/test 추가 경로 match가 없었다.
- parser, validator rule, executor, XGT / DB / payload 구현을 추가하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
