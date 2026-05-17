# AH-RUNTIME-54 Closeout - Validation Model Skeleton

## 1. Summary

AH-RUNTIME-54는 AH-RUNTIME-52 Validation Rule Matrix와 AH-RUNTIME-53 Validator Skeleton Boundary Review의 결론을 바탕으로, 이후 validator가 공통으로 사용할 runtime-neutral Validation model skeleton을 최소 추가한 작업이다.

이번 변경은 validator 구현이 아니라 결과 모델 추가에 한정했다. `RuleId`는 enum이 아닌 string code로 유지했고, `BlocksExecution`은 `ValidationSeverity`에서 자동 유도하지 않는 명시 필드로 분리했다.

모델은 `CAAutomationHub.Contracts`의 `Runtime/Validation` namespace 아래에 배치했다. Runtime core, WPF, future authoring/parser layer, adapter extension rule provider가 같은 result shape를 공유할 수 있도록 하기 위한 선택이며, XGT / DB / JSON parser / executor / WPF / FakePlc 연결은 추가하지 않았다.

## 2. Changed Files

- `src/CAAutomationHub.Contracts/Runtime/Validation/ValidationSeverity.cs`
- `src/CAAutomationHub.Contracts/Runtime/Validation/ValidationCategory.cs`
- `src/CAAutomationHub.Contracts/Runtime/Validation/ValidationIssue.cs`
- `src/CAAutomationHub.Contracts/Runtime/Validation/ValidationResult.cs`
- `tests/CAAutomationHub.Runtime.Tests/Validation/ValidationResultModelTests.cs`
- `docs/harness/AH-RUNTIME-54.md`

## 3. Model Placement

모델은 `CAAutomationHub.Contracts.Runtime.Validation` namespace에 추가했다.

선택 이유:

- Validation result model은 Runtime core 내부 구현체가 아니라 boundary contract에 가깝다.
- AH-RUNTIME-53에서 검토한 것처럼 WPF authoring, Runtime preflight, adapter extension, external validation tool이 같은 issue/result shape를 공유할 수 있어야 한다.
- Contracts 배치는 기존 Runtime project가 Contracts만 참조하는 boundary와 충돌하지 않는다.
- Contracts에는 raw JSON AST, FLOW.JSON parser model, XGT address, DB query, payload layout implementation detail을 넣지 않았다.

## 4. Implemented Model

추가한 최소 모델:

- `ValidationSeverity`
  - `Error`
  - `Warning`
  - `Info`
  - `ReviewRequired`

- `ValidationCategory`
  - `Structural`
  - `Binding`
  - `Policy`
  - `Extension`

- `ValidationIssue`
  - `string RuleId`
  - `ValidationCategory Category`
  - `ValidationSeverity Severity`
  - `bool BlocksExecution`
  - `string Message`
  - `string? TargetPath`
  - `string? Evidence`
  - `IReadOnlyDictionary<string, string>? Metadata`

- `ValidationResult`
  - `IReadOnlyList<ValidationIssue> Issues`
  - `bool IsValid`
  - `bool BlocksExecution`
  - `Empty`
  - `Success`

`ValidationResult.IsValid`은 issue가 없을 때만 `true`다. `ValidationResult.BlocksExecution`은 `BlocksExecution=true` issue가 하나라도 있을 때 `true`다.

외부 변경 가능성을 줄이기 위해 `ValidationResult`는 issue list를 생성 시 배열로 복사하고, `ValidationIssue`는 metadata를 `ReadOnlyDictionary`로 복사한다. 속성은 생성 후 재지정하지 않도록 get-only로 두었다.

## 5. Tests

추가 테스트는 model behavior만 검증한다.

검증한 항목:

- 빈 `ValidationResult`는 `IsValid=true`.
- issue가 하나라도 있으면 `IsValid=false`.
- `Error` severity라도 issue의 `BlocksExecution=false`이면 result의 `BlocksExecution=false`가 가능하다.
- `BlocksExecution=true` issue가 하나라도 있으면 result의 `BlocksExecution=true`.
- `RuleId`는 string code로 유지된다.
- `ReviewRequired` severity를 표현할 수 있다.
- `Metadata`, `TargetPath`, `Evidence`를 선택적으로 표현할 수 있다.

TDD RED evidence:

- production model 추가 전 `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj` 실행 결과, `CAAutomationHub.Contracts.Runtime.Validation` namespace가 없어 컴파일 실패했다.

TDD GREEN evidence:

- model 추가 후 같은 테스트 명령이 통과했다.

## 6. Boundary

Runtime vendor-neutral boundary는 유지했다.

유지한 경계:

- Runtime core에 XGT-specific validation rule을 넣지 않았다.
- Runtime core에 DB-specific validation rule을 넣지 않았다.
- Runtime core에 FLOW.JSON parser 또는 JSON schema validator를 넣지 않았다.
- Runtime core와 executor를 연결하지 않았다.
- WPF UI와 연결하지 않았다.
- FakePlc, XgtDriverCore, XgtChannelRunner를 참조하지 않았다.
- `RuleId`를 enum으로 고정하지 않고 extension rule provider가 string code를 사용할 수 있게 유지했다.
- `BlocksExecution`을 `Severity` 자동 계산으로 묶지 않았다.

## 7. Excluded Scope

이번 작업에서 제외한 범위:

- FLOW.JSON parser 구현
- JSON schema 구현
- validator rule 실행 구현
- StructuralValidator / BindingValidator / PolicyValidator 구현
- XGT Adapter 구현
- DB Query 구현
- Payload Builder 구현
- Executor 연결
- WPF 연결
- FakePlc 연결
- XgtDriverCore 참조
- ProjectReference / PackageReference 추가
- Runtime core vendor-specific dependency 추가

## 8. Validation

실행한 명령과 결과:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
  - RED: Validation namespace 부재로 컴파일 실패 확인.
  - GREEN: 실패 0, 통과 134, 건너뜀 0.
  - 최종 실행: 실패 0, 통과 134, 건너뜀 0.
- `dotnet build CAAutomationHub.sln`
  - 경고 0, 오류 0.
- `dotnet build CAAutomationHub.sln`
  - 최종 실행: 경고 0, 오류 0.
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
  - 최종 실행: 실패 0, 통과 134, 건너뜀 0.
- `git diff --check`
  - PASS. whitespace error 없음.
- `git status --short`
  - `?? docs/harness/AH-RUNTIME-54.md`
  - `?? src/CAAutomationHub.Contracts/Runtime/Validation/`
  - `?? tests/CAAutomationHub.Runtime.Tests/Validation/`
- 금지 범위 검색
  - `rg -n "Xgt|FakePlc|Db|DB|Sql|SQL|Json|JSON|Executor|Wpf|ProjectReference|PackageReference" src\CAAutomationHub.Contracts\Runtime\Validation tests\CAAutomationHub.Runtime.Tests\Validation`
  - source/test 추가 경로에서 금지 키워드 match 없음.

## 9. Next Candidate

다음 후보는 validator 구현으로 바로 뛰기보다 다음 중 하나가 적절하다.

1. Neutral validation input model boundary review
   - validator가 받을 parsed neutral draft/executable candidate의 최소 shape를 검토한다.
   - raw JSON parser나 schema implementation은 여전히 제외한다.

2. Validator interface skeleton
   - `ValidationResult`를 반환하는 runtime-neutral interface만 검토한다.
   - rule implementation, parser, XGT / DB extension rule provider는 제외한다.

3. Extension rule provider boundary review
   - XGT / DB-specific rule이 Runtime core 밖에서 어떻게 result model만 공유할지 검토한다.

## 10. Self-Check

판정: ACCEPT

이유:

- Validation model skeleton과 최소 테스트만 추가했다.
- `ValidationSeverity`, `ValidationCategory`, `ValidationIssue`, `ValidationResult`가 존재한다.
- `RuleId`는 string code다.
- `BlocksExecution`은 `Severity`와 분리된 명시 필드다.
- `ValidationResult`는 `Issues` 기반으로 `IsValid`와 `BlocksExecution`을 계산한다.
- Runtime-neutral boundary를 유지했다.
- parser, schema, validator, executor, XGT, DB, WPF 연결을 구현하지 않았다.
- ProjectReference / PackageReference를 추가하지 않았다.
- AH-RUNTIME-54 Closeout 문서를 생성했다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
