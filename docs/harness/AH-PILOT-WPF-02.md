# AH-PILOT-WPF-02 Closeout - Pilot Application Service Boundary Skeleton

## 1. Summary

AH-PILOT-WPF-02는 WPF ViewModel이 `WorkStartFlowService`, XGT concrete, DB concrete를 직접 조립하지 않도록 WorkStart execution application service boundary skeleton을 추가한 작업이다.

새 `CAAutomationHub.PilotApp` project를 만들고 `IWorkStartExecutionService`, `WorkStartExecutionRequest`, `WorkStartExecutionResult`, `IWorkStartFlowRunner`, `WorkStartExecutionService`, `WorkStartFlowServiceRunner`를 추가했다. `WorkStartExecutionService`는 fake runner 또는 adapter runner를 통해 `WorkStartFlowResult`를 받아 WPF-friendly result DTO로 낮추며, timestamp / duration / busy duplicate guard / cancellation token 전달을 담당한다.

이번 작업은 application service skeleton과 fake runner 기반 unit tests에 한정했다. `RuntimeSnapshot`, `ChannelPollingResult`, Runtime project, FlowDefinitions project, WPF ViewModel, WPF View/XAML, DI wiring, XGT concrete, DB concrete, FLOW.JSON, JSON parser, Flow Executor는 수정하지 않았다.

## 2. 변경 파일 목록

- `CAAutomationHub.sln`
- `src/CAAutomationHub.PilotApp/CAAutomationHub.PilotApp.csproj`
- `src/CAAutomationHub.PilotApp/WorkStart/IWorkStartExecutionClock.cs`
- `src/CAAutomationHub.PilotApp/WorkStart/IWorkStartExecutionService.cs`
- `src/CAAutomationHub.PilotApp/WorkStart/IWorkStartFlowRunner.cs`
- `src/CAAutomationHub.PilotApp/WorkStart/SystemWorkStartExecutionClock.cs`
- `src/CAAutomationHub.PilotApp/WorkStart/WorkStartExecutionRequest.cs`
- `src/CAAutomationHub.PilotApp/WorkStart/WorkStartExecutionResult.cs`
- `src/CAAutomationHub.PilotApp/WorkStart/WorkStartExecutionService.cs`
- `src/CAAutomationHub.PilotApp/WorkStart/WorkStartFlowServiceRunner.cs`
- `tests/CAAutomationHub.PilotApp.Tests/CAAutomationHub.PilotApp.Tests.csproj`
- `tests/CAAutomationHub.PilotApp.Tests/WorkStart/WorkStartExecutionServiceTests.cs`
- `docs/harness/AH-PILOT-WPF-02.md`

## 3. 선택한 project / namespace 배치 이유

선택: 후보 B, 새 project `CAAutomationHub.PilotApp`.

이유:

- WPF가 나중에 참조할 application boundary를 `PilotFlows` domain/service helper와 분리할 수 있다.
- `PilotFlows`는 WorkStart business flow와 primitive interfaces를 유지하고, `PilotApp`은 WPF/CLI/service host가 호출 가능한 execution boundary를 소유한다.
- `PilotApp`은 `CAAutomationHub.PilotFlows`만 참조한다.
- `PilotApp`은 `PilotFlows.Xgt`, XGT driver, FakePlc, Runtime, FlowDefinitions, SqlClient를 참조하지 않는다.

`PilotFlows` 내부에 application service를 넣는 후보 A는 project 수는 줄지만 business flow와 UI-facing orchestration 책임이 섞일 위험이 있어 보류했다. WPF 내부에 넣는 후보 C는 WPF가 pilot orchestration을 소유하는 모양이 되므로 선택하지 않았다.

## 4. 추가한 request / result / service 타입

- `IWorkStartExecutionService`: WPF ViewModel이 후속 단계에서 호출할 application service interface.
- `WorkStartExecutionRequest`: `TargetId`, `RequestedBy`, `CorrelationId`만 가진 최소 request DTO. XGT address, SQL text, RuntimeSnapshot, LOT ID를 담지 않는다.
- `WorkStartExecutionResult`: WPF-friendly result DTO. `Succeeded`, `Status`, `Step`, `ErrorCode`, `ErrorCodeName`, `Message`, `SelectedLotId`, `ErrorWriteExpected`, `StartedAt`, `CompletedAt`, `Duration`만 제공한다.
- `IWorkStartFlowRunner`: `WorkStartFlowService` 또는 fake runner를 감싸기 위한 application seam.
- `WorkStartFlowServiceRunner`: existing `WorkStartFlowService.RunAsync`를 `IWorkStartFlowRunner`로 낮추는 adapter.
- `WorkStartExecutionService`: runner 호출, duplicate busy guard, cancellation token 전달, timestamp / duration 부여, result mapping을 담당한다.
- `IWorkStartExecutionClock` / `SystemWorkStartExecutionClock`: duration test를 deterministic하게 만들기 위한 clock seam.

## 5. WorkStartFlowResult -> WPF-friendly result mapping

Mapping:

- `WorkStartFlowResult.Succeeded` -> `WorkStartExecutionResult.Succeeded`
- `WorkStartFlowResult.Status.ToString()` -> `Status`
- `WorkStartFlowResult.Step.ToCode()` -> `Step`
- `(int)WorkStartFlowResult.ErrorCode` -> `ErrorCode`
- `WorkStartFlowResult.ErrorCode.ToString()` -> `ErrorCodeName`
- `WorkStartFlowResult.Message` -> `Message`
- `WorkStartFlowResult.SelectedLotId` -> `SelectedLotId`
- `WorkStartFlowResult.ErrorWriteExpected` -> `ErrorWriteExpected`
- application clock before/after runner -> `StartedAt`, `CompletedAt`, `Duration`

Duplicate execution policy skeleton:

- `WorkStartExecutionService` uses a single `SemaphoreSlim` gate.
- 두 번째 동시 호출은 runner를 다시 호출하지 않고 `Status = "Busy"`, `Step = "busy"`, `Succeeded = false` result를 반환한다.
- Busy result는 PLC error write 대상이 아니므로 `ErrorWriteExpected = false`다.

Cancellation policy skeleton:

- `ExecuteOnceAsync`의 `CancellationToken`은 gate wait와 runner `RunAsync`로 전달한다.
- 이번 skeleton은 cancellation-specific DTO status를 만들지 않는다.

## 6. RuntimeSnapshot 비오염 확인

`RuntimeSnapshot`과 `ChannelPollingResult`는 수정하지 않았다.

`WorkStartExecutionResult`는 payload bytes, request/response hex, RuntimeSnapshot, ChannelPollingResult, DB result object를 노출하지 않는다. `ExecutionResult_DoesNotExposePayloadOrChannelState` reflection test로 DTO property/type boundary를 검증했다.

Runtime project와 FlowDefinitions project도 수정하지 않았다.

## 7. WPF ViewModel / DI 미구현 범위

이번 작업에서 구현하지 않은 범위:

- WPF ViewModel command
- WPF View / XAML
- App.xaml.cs 또는 composition root DI wiring
- XGT adapter composition
- DB concrete composition
- Runtime command dispatcher
- operation log/event stream/history
- FLOW.JSON parser/schema/executor

WPF project는 수정하지 않았다. 후속 단계에서 WPF는 `CAAutomationHub.PilotApp`만 참조하는 방향을 우선 검토한다.

## 8. 테스트 결과

RED evidence:

- `dotnet test tests\CAAutomationHub.PilotApp.Tests\CAAutomationHub.PilotApp.Tests.csproj`
- result: expected compile failure because `CAAutomationHub.PilotApp.WorkStart`, `IWorkStartFlowRunner`, `IWorkStartExecutionClock` did not exist yet.

GREEN / validation:

- `dotnet test tests\CAAutomationHub.PilotApp.Tests\CAAutomationHub.PilotApp.Tests.csproj`
  - result: pass, failed 0, passed 6, skipped 0
- `dotnet test tests\CAAutomationHub.PilotFlows.Tests\CAAutomationHub.PilotFlows.Tests.csproj`
  - result: pass, failed 0, passed 40, skipped 0
- `dotnet test tests\CAAutomationHub.PilotFlows.Xgt.Tests\CAAutomationHub.PilotFlows.Xgt.Tests.csproj`
  - result: pass, failed 0, passed 39, skipped 0
- `dotnet test tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
  - result: pass, failed 0, passed 142, skipped 0

## 9. 빌드 결과

- `dotnet build CAAutomationHub.sln`
  - result: pass
  - warnings: 0
  - errors: 0

## 10. Boundary scan 결과

Full scan:

- `rg -n "XgtDriverCore|FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src tests`
- result: exit code 0 with existing hits in `CAAutomationHub.PilotFlows.Xgt`, `CAAutomationHub.PilotFlows.Xgt.Tests`, Runtime, WPF, and existing tests.
- 판단: 기존 XGT/FakePlc/Runtime/WPF 책임 위치의 hit이며, 새 PilotApp boundary 오염은 아니다.

New boundary scan:

- `rg -n "XgtDriverCore|FakePlc|XgtChannelRunner|SqlConnection|Microsoft.Data.SqlClient|RuntimeSnapshot|ChannelPollingResult|FlowExecutor|FLOW.JSON|Json|JSON" src\CAAutomationHub.PilotApp tests\CAAutomationHub.PilotApp.Tests`
- result: exit code 1, output 없음.
- 판단: 새 PilotApp source/tests에 금지 boundary keyword hit 없음.

Project reference scan:

- `rg -n "<ProjectReference|<PackageReference" src tests -g "*.csproj"`
- result: 새 reference는 `src/CAAutomationHub.PilotApp -> src/CAAutomationHub.PilotFlows`와 `tests/CAAutomationHub.PilotApp.Tests -> src/CAAutomationHub.PilotApp`만 추가됨.
- `CAAutomationHub.Wpf.csproj`는 수정되지 않았다.
- `CAAutomationHub.Runtime.csproj`는 수정되지 않았다.
- `CAAutomationHub.PilotApp.csproj`는 XGT driver, FakePlc, XgtChannelRunner, SqlClient, Runtime, FlowDefinitions를 참조하지 않는다.

Whitespace / status:

- `git diff --check`
  - result: exit code 0, output 없음.
- `git status --short`
  - closeout 작성 전 result: `M CAAutomationHub.sln`, new `src/CAAutomationHub.PilotApp/`, new `tests/CAAutomationHub.PilotApp.Tests/`.

## 11. 다음 후보

- AH-PILOT-WPF-03: WPF ViewModel boundary test with fake `IWorkStartExecutionService`.
- PilotApp composition review: actual `WorkStartFlowServiceRunner`를 어떤 composition root에서 만들지 검토하되 WPF가 XGT/DB concrete를 직접 조립하지 않도록 유지.
- Pilot operation log/event boundary review: Runtime event path와 섞지 않는 별도 operation stream 후보 검토.

## 12. Self-Check

판정: `ACCEPT`

근거:

- WPF-friendly application service boundary skeleton을 새 `CAAutomationHub.PilotApp` project에 추가했다.
- `WorkStartFlowResult`를 RuntimeSnapshot / ChannelPollingResult에 넣지 않았다.
- `ChannelPollingResult`, `RuntimeSnapshot`, Runtime project, FlowDefinitions project, WPF ViewModel, WPF View/XAML, DI wiring을 수정하지 않았다.
- PilotApp은 `PilotFlows`만 참조하며 XGT/FakePlc/DB concrete를 참조하지 않는다.
- fake runner 기반 unit tests로 success, failure, cancellation token 전달, duplicate busy guard, DTO 비오염, assembly reference boundary를 검증했다.
- 요청된 PilotFlows, PilotFlows.Xgt, Runtime 테스트와 새 PilotApp 테스트를 실행했고 모두 통과했다.
- solution build와 `git diff --check`를 통과했다.
- ContextPublisher 자동 publish는 재도입하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
