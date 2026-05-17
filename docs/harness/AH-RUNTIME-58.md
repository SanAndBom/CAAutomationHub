# AH-RUNTIME-58 Closeout - Minimal Candidate Model Skeleton

## 1. Summary

AH-RUNTIME-58은 AH-RUNTIME-57에서 확정한 placement에 따라 최소 neutral candidate model skeleton을 추가한 작업이다.

새 Runtime-neutral definition project `CAAutomationHub.FlowDefinitions`를 만들고, model-only 타입 네 개를 추가했다. 이번 변경은 parser, validator rule, validator interface, executor, FLOW.JSON, JSON schema 구현이 아니다.

TDD 순서로 먼저 `tests/CAAutomationHub.Runtime.Tests`에 model behavior tests와 project reference를 추가했고, production project가 없어 실패하는 RED를 확인했다. 이후 `CAAutomationHub.FlowDefinitions` project와 최소 model을 추가해 GREEN을 확인했다.

Self-Check 판정은 `ACCEPT`다.

## 2. 변경 파일 목록

- `CAAutomationHub.sln`
- `src/CAAutomationHub.FlowDefinitions/CAAutomationHub.FlowDefinitions.csproj`
- `src/CAAutomationHub.FlowDefinitions/FlowDefinitionCandidate.cs`
- `src/CAAutomationHub.FlowDefinitions/FlowStepCandidate.cs`
- `src/CAAutomationHub.FlowDefinitions/FlowReference.cs`
- `src/CAAutomationHub.FlowDefinitions/FlowPolicyReference.cs`
- `tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj`
- `tests/CAAutomationHub.Runtime.Tests/FlowDefinitions/FlowDefinitionCandidateModelTests.cs`
- `docs/harness/AH-RUNTIME-58.md`

## 3. Model Placement

추가 project:

- `src/CAAutomationHub.FlowDefinitions/CAAutomationHub.FlowDefinitions.csproj`

namespace:

- `CAAutomationHub.FlowDefinitions`

선택 이유:

- Runtime core가 flow definition owner가 되지 않는다.
- Contracts가 input schema model 저장소로 비대해지지 않는다.
- Runtime, WPF, parser, adapter extension이 장기적으로 같은 neutral candidate를 공유할 수 있다.
- `ValidationResult` 계열은 Contracts에 유지하고, flow definition input candidate는 별도 Runtime-neutral definition boundary에 둔다.

## 4. 추가 타입 요약

추가 타입:

- `FlowDefinitionCandidate`
- `FlowStepCandidate`
- `FlowReference`
- `FlowPolicyReference`

`FlowDefinitionCandidate` 필드:

- `string FlowId`
- `string FlowKind`
- `string InitialState`
- `IReadOnlyList<FlowStepCandidate> Steps`
- `IReadOnlyDictionary<string, FlowReference>? Bindings`
- `IReadOnlyDictionary<string, FlowPolicyReference>? Policies`

`FlowStepCandidate` 필드:

- `string StepId`
- `string ActionKind`
- `string? OnSuccess`
- `string? OnFailure`
- `IReadOnlyList<string>? RequiredBindingRefs`
- `IReadOnlyList<string>? RequiredPolicyRefs`

`FlowReference` 필드:

- `string Key`
- `string Kind`

`FlowPolicyReference` 필드:

- `string Key`
- `string Kind`
- `string? Status`

Model behavior:

- 필수 string 값은 empty / whitespace를 허용하지 않는다.
- `Steps`, `RequiredBindingRefs`, `RequiredPolicyRefs`는 생성 시 배열로 복사한다.
- `Bindings`, `Policies`는 생성 시 `ReadOnlyDictionary`로 복사한다.

## 5. Tests

추가 테스트:

- `FlowDefinitionCandidate`가 `FlowId`, `FlowKind`, `InitialState`, `Steps`를 보존하는지 확인
- `Steps`가 생성 시 복사되어 외부 list 변경에 영향받지 않는지 확인
- `FlowStepCandidate`가 `StepId`, `ActionKind`, transition refs, required reference keys를 보존하는지 확인
- `Bindings`, `Policies` reference를 표현할 수 있는지 확인
- candidate assembly가 Runtime project에 의존하지 않는지 확인

TDD RED evidence:

- production project 추가 전 `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj` 실행
- 결과: 실패
- 주요 실패:
  - `src\CAAutomationHub.FlowDefinitions\CAAutomationHub.FlowDefinitions.csproj` project 없음
  - `CAAutomationHub.FlowDefinitions` namespace 없음

TDD GREEN evidence:

- `CAAutomationHub.FlowDefinitions` project와 model 추가 후 같은 test command 통과
- 실패 0, 통과 139, 건너뜀 0

## 6. 금지 필드 유지 여부

유지했다.

candidate model에는 다음을 넣지 않았다.

- XGT address
- datatype
- count
- SQL text
- connection string
- payload byte offset
- raw JSON path
- raw JSON text
- RuntimeSnapshot
- ChannelPollingResult
- XgtDriverCore type
- FakePlc scenario id
- WPF view model
- executor handler type

이번 작업에서 다음도 하지 않았다.

- parser 구현
- validator rule 구현
- validator interface 추가
- executor 구현
- FLOW.JSON 생성
- JSON schema 생성
- XGT Adapter 구현
- DB Query 구현
- Payload Builder 이식
- Runtime core에 flow definition owner 책임 추가

## 7. Validation

실행한 명령과 결과:

- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
  - RED: project / namespace 부재로 실패 확인
  - GREEN: 실패 0, 통과 139, 건너뜀 0
  - 최종 실행: 실패 0, 통과 139, 건너뜀 0

- `dotnet build CAAutomationHub.sln`
  - 경고 0, 오류 0

- `git diff --check`
  - exit code 0
  - whitespace error 없음
  - line-ending 정규화 후 최종 출력 없음

- `rg -n "Xgt|FakePlc|Db|DB|Sql|SQL|Json|JSON|Executor|Wpf|RuntimeSnapshot|ChannelPollingResult" src\CAAutomationHub.FlowDefinitions tests\CAAutomationHub.Runtime.Tests\FlowDefinitions`
  - match 없음

- `git status --short`
  - ` M CAAutomationHub.sln`
  - ` M tests/CAAutomationHub.Runtime.Tests/CAAutomationHub.Runtime.Tests.csproj`
  - `?? docs/harness/AH-RUNTIME-57.md`
  - `?? src/CAAutomationHub.FlowDefinitions/`
  - `?? tests/CAAutomationHub.Runtime.Tests/FlowDefinitions/`

## 8. Boundary

유지한 boundary:

- Runtime project에는 새 ProjectReference를 추가하지 않았다.
- Runtime core는 `FlowDefinitionCandidate` owner가 아니다.
- `CAAutomationHub.FlowDefinitions`는 Runtime, WPF, XGT, DB, FakePlc를 참조하지 않는다.
- model은 neutral reference key / kind / status 표현까지만 포함한다.
- validation result model은 기존처럼 Contracts에 남아 있다.
- Flow Executor, parser, adapter-specific validation으로 확장하지 않았다.

주의:

- `tests/CAAutomationHub.Runtime.Tests`는 model behavior와 compile boundary 검증을 위해 `CAAutomationHub.FlowDefinitions`를 참조한다.
- 이는 production Runtime project reference boundary 변경이 아니다.

## 9. STOP CHECK

STOP CHECK 결과:

- model에 XGT address가 필요해짐: 발생하지 않음
- model에 SQL text가 필요해짐: 발생하지 않음
- model에 payload offset이 필요해짐: 발생하지 않음
- raw JSON path/text가 필요해짐: 발생하지 않음
- RuntimeSnapshot 또는 ChannelPollingResult 참조 필요: 발생하지 않음
- parser나 validator rule 필요: 발생하지 않음
- csproj reference 방향이 Runtime core boundary를 깨뜨림: 발생하지 않음
- model 범위가 FLOW.JSON schema 전체로 커짐: 발생하지 않음

따라서 AH-RUNTIME-59로 진행 가능하다.

## 10. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-57 placement 결론에 따라 별도 Runtime-neutral definition project를 추가했다.
- model-only skeleton으로 범위를 제한했다.
- 최소 타입 네 개만 추가했다.
- TDD RED / GREEN evidence를 확보했다.
- test와 solution build가 통과했다.
- 금지 키워드 scan에서 source/test 추가 경로 match가 없었다.
- Runtime project reference boundary를 변경하지 않았다.
- parser, validator rule, validator interface, executor, XGT / DB / payload 구현을 추가하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
