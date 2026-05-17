# AH-RUNTIME-43 Closeout

## 1. Summary

AH-RUNTIME-43은 기존 `XgtDriverCore` / `FakePlc` / `XgtChannelRunner` 자산을 현재 `CAAutomationHub`에서 재사용하기 위해 repo / project / reference를 어떤 방식으로 고정할지 검토한 Existing Project Retrieval Plan Boundary Review 단계다.

핵심 결론은 `CAAutomationHub.Runtime` core를 계속 vendor-neutral로 유지하고, 기존 XGT 자산을 Runtime core에 직접 연결하지 않는 것이다. `AH-RUNTIME-44`는 `CAAutomationHub` repo 내부 Adapter project boundary review로 진행하는 것이 가장 현실적이며, 실제 `XgtDriverCore` reference 추가는 sibling repo clean / commit anchor 확보 뒤로 미루는 것이 안전하다.

현재 sibling repo인 `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`는 dirty 상태다. 이 상태를 기준으로 reference / submodule / subtree / source copy를 확정하면 Codex / CI / 다른 PC에서 재현성이 깨질 수 있다. 따라서 dirty working tree를 retrieval strategy의 기준으로 삼지 않고, clean commit anchor 확보 이후 실제 연결 방식을 확정해야 한다.

사용자 선호인 "CAAutomationHub 안에 녹이기"는 Runtime core에 XGT-specific code를 섞는 의미가 아니라, 외부 repo 관리 부담을 줄이되 Adapter boundary를 `CAAutomationHub` repo 안에 두는 방향으로 해석하는 것이 안전하다.

이번 작업은 Boundary Review 결과를 문서화한 단계이며, production code, test code, solution, csproj, project reference, package reference, submodule, subtree, source copy, commit은 수행하지 않았다. `ContextPublisher` 자동 publish도 재도입하지 않았다.

## 2. Goal

AH-RUNTIME-43의 목표는 기존 `XgtDriverCore` / `FakePlc` / `XgtChannelRunner` 자산을 `CAAutomationHub`에서 재사용하려면 어떤 방식으로 가져오거나 고정하는 것이 가장 안전한지 판단하는 것이다.

핵심 질문은 다음이었다.

- 현재 sibling repo를 그대로 둘 것인가?
- sibling repo project reference를 사용할 것인가?
- git submodule로 고정할 것인가?
- git subtree로 흡수할 것인가?
- source copy를 할 것인가?
- NuGet / package reference를 준비할 것인가?
- `CAAutomationHub` repo 내부로 일부만 이식할 것인가?
- Codex / CI / 다른 PC에서 재현 가능한 구조는 무엇인가?
- dirty state인 sibling repo를 어떻게 다룰 것인가?
- `AH-RUNTIME-44` XgtAdapter Project Boundary Review에 어떤 retrieval 전제를 넘겨야 하는가?

이번 단계는 구현이 아니라 Boundary Review다. 따라서 기존 프로젝트를 가져오거나, project reference를 추가하거나, Adapter project를 생성하지 않았다.

## 3. Background

AH-RUNTIME-42에서는 현재 `CAAutomationHub` repo 내부에 실제 `XgtDriverCore` / `FakePlc` / `XgtChannelRunner` project/source가 없고, 기존 XGT 자산은 sibling repo인 `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`에 존재함을 확인했다.

AH-RUNTIME-42의 핵심 결론은 다음이었다.

- `CAAutomationHub.Runtime` core 직접 참조는 비권장이다.
- XGT-specific code는 Adapter / Source 계층에 격리해야 한다.
- `XgtDriverCore`는 Adapter / Source 계층 재사용 후보다.
- `FakePlc`는 integration harness / adapter test 후보다.
- `XgtChannelRunner`는 직접 참조보다 helper / business flow reference 후보다.
- `WorkStartPilotService.RunOnceAsync(...)`는 pilot flow reuse anchor다.
- 기존 프로젝트를 쓰려면 retrieval / reference 전략이 먼저 필요하다.

AH-RUNTIME-43은 이 결론을 바탕으로 실제 `XgtAdapter` boundary 검토 전에 retrieval / reference / repository strategy를 먼저 고정하기 위한 review다.

## 4. 현재 CAAutomationHub repo 상태

현재 `CAAutomationHub` repo 상태는 다음과 같다.

- Repo root: `C:\AutomationHub.Rebuild\CAAutomationHub`
- Branch: `codex/local`
- Remote: `https://github.com/SanAndBom/CAAutomationHub`
- Latest commit: `2ee025d docs: close out AH-RUNTIME-42 existing xgt reuse boundary review`
- Working tree: clean

Solution projects:

- `src\CAAutomationHub.Contracts\CAAutomationHub.Contracts.csproj`
- `src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj`
- `src\CAAutomationHub.Wpf\CAAutomationHub.Wpf.csproj`
- `tests\CAAutomationHub.Runtime.Tests\CAAutomationHub.Runtime.Tests.csproj`
- `tests\CAAutomationHub.Wpf.Tests\CAAutomationHub.Wpf.Tests.csproj`

현재 `CAAutomationHub.sln`에는 `XgtDriverCore`, `FakePlc`, `XgtChannelRunner` project가 포함되어 있지 않다.

## 5. sibling AutomationHub.XgtDriverCore repo 상태

Sibling repo 상태는 다음과 같다.

- Repo root: `C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore`
- Branch: `main`
- Remote: `https://github.com/SanAndBom/AutomationHub_Rebuild`
- Latest commit: `fa0ab4f Merge pull request #119 from SanAndBom/codex/add-uint32-and-int32-display-formats`
- Working tree: dirty

Solution projects include:

- `src\AutomationHub.XgtDriverCore\AutomationHub.XgtDriverCore.csproj`
- `tools\AutomationHub.XgtDriverCore.FakePlc\AutomationHub.XgtDriverCore.FakePlc.csproj`
- `XgtChannelRunner\XgtChannelRunner.csproj`
- tests
- samples
- WinForms tools
- `AutomationHub.ContextPublisher`

## 6. Dirty state 확인 결과

Sibling repo dirty files:

Modified:

- `tools/AutomationHub.XgtDriverCore.FakePlc/appsettings/fakeplc.map.json`

Untracked:

- `context-events/pending/evt_20260507_234052_receiveframeasync-whenresponsearrivesinp.json`
- `context-events/pending/evt_20260507_234052_tcptransport-basic-request-response.json`
- `context-events/pending/evt_20260507_234052_tcptransport-timeout-handling.json`
- `context-events/pending/evt_20260507_234052_xgtsession-basic-exchange.json`
- `context-events/pending/evt_20260516_005654_iwritableruntimeplcchannel-runtime-state.json`

판단:

- 이 dirty state를 기준으로 reference / submodule / subtree / source copy를 진행하면 재현성이 깨진다.
- `AH-RUNTIME-44`에서 실제 reference를 추가하기 전 sibling repo clean 또는 별도 commit anchor 확보가 필요하다.
- dirty working tree를 참조 전략의 기준으로 삼지 않는다.
- dirty change가 의도된 변경인지 현재 Boundary Review만으로는 단정하지 않는다.

## 7. 현재 project / reference 구조

`CAAutomationHub.Runtime.csproj`:

- `CAAutomationHub.Contracts`만 참조한다.

`RuntimeProjectReferenceBoundaryTests.cs`:

- Runtime project가 `Contracts`만 참조하는지 검증한다.
- `CAAutomationHub.Wpf`, `XgtDriverCore`, `XgtChannelRunner`, `FakePlc` 문자열 참조를 금지한다.

Sibling repo project 구조:

`AutomationHub.XgtDriverCore`:

- `net10.0`
- package / project dependency 없음

`FakePlc`:

- `net10.0`
- `XgtDriverCore` project reference

`XgtChannelRunner`:

- `net10.0-windows`
- WinForms
- `XgtDriverCore`
- `ScottPlot.WinForms`
- `Microsoft.Data.SqlClient`

판단:

- Adapter project에서 참조 가능한 최소 후보는 `AutomationHub.XgtDriverCore`다.
- `FakePlc`는 production dependency가 아니라 test / integration harness dependency로 우선 검토한다.
- `XgtChannelRunner`는 Windows / WinForms / SQL / chart dependency가 있으므로 direct reference 후보로 부적절하다.

## 8. 후보 A: sibling repo 유지 + local project reference

개념:

    CAAutomationHub.Runtime.XgtAdapter
        -> ..\AutomationHub.XgtDriverCore\src\AutomationHub.XgtDriverCore\AutomationHub.XgtDriverCore.csproj

판정:

- 단기 local pilot에는 가능하다.
- 장기 구조로는 부적절하다.
- dirty 정리 전에는 비권장이다.

장점:

- 가장 빠르다.
- 기존 `XgtDriverCore`를 그대로 사용할 수 있다.
- 코드 중복이 없다.
- 로컬 개발 속도가 빠르다.

위험:

- local path coupling이 크다.
- CI / Codex / 다른 PC에서 재현성이 낮다.
- commit pinning이 없다.
- sibling repo dirty state가 build 신뢰성에 영향을 준다.
- `CAAutomationHub` repo만 clone하면 빌드가 불가능할 수 있다.

결론:

- 빠른 local pilot에는 제한적으로 가능하지만, `AH-RUNTIME-44`의 기본 전제로 삼기에는 위험하다.
- clean commit anchor 확보 전에는 실제 reference 추가를 금지한다.

## 9. 후보 B: Git submodule

개념:

    CAAutomationHub\
        external\AutomationHub.XgtDriverCore\

판정:

- 재현성 기준으로는 강하다.
- 사용자 관리 부담은 크다.
- clean commit anchor 확보 후에만 검토 가능하다.

장점:

- version pinning이 가능하다.
- 기존 repo history를 보존한다.
- `XgtDriverCore` upstream 추적이 가능하다.
- `CAAutomationHub` repo 내부에서 외부 자산 위치가 고정된다.

위험:

- submodule 운영 복잡도가 있다.
- clone 시 `--recurse-submodules`가 필요하다.
- Codex / 개발자 작업 시 submodule sync 실수 가능성이 있다.
- 사용자에게 Git 관리 부담이 증가한다.
- 현재 sibling repo dirty state를 먼저 정리해야 한다.

결론:

- 장기적으로 고려할 수 있으나, 사용자 관리 부담을 감안하면 즉시 채택은 신중해야 한다.

## 10. 후보 C: Git subtree

개념:

    CAAutomationHub\
        external\AutomationHub.XgtDriverCore\

판정:

- 사용자 선호인 "CAAutomationHub 안에 녹이기"와 잘 맞는다.
- 단일 repo 재현성에 강하다.
- upstream sync 부담은 존재한다.

장점:

- 단일 clone으로 재현 가능하다.
- submodule보다 사용자가 덜 헷갈릴 수 있다.
- CI / Codex 접근성이 좋다.
- 특정 시점 source를 `CAAutomationHub` 안에 흡수할 수 있다.

위험:

- upstream sync 부담이 있다.
- repo size가 증가한다.
- history / merge 관리가 복잡해질 수 있다.
- `XgtDriverCore`가 독립 진화할 경우 관리가 어려울 수 있다.

결론:

- 장기적으로 `CAAutomationHub` 안에 녹이는 방향을 강하게 원한다면 유력 후보다.
- 단, `AH-RUNTIME-44`에서 바로 수행하지는 않는다.

## 11. 후보 D: Source copy

개념:

- 필요한 `XgtDriverCore` source 일부를 `CAAutomationHub` repo로 복사한다.

판정:

- driver / session 핵심 로직 copy는 비권장이다.
- helper / rule 재구성 수준은 제한적으로 가능하다.

장점:

- 가장 단순해 보인다.
- 단일 repo 재현성이 있다.
- 초기 실험이 빠르다.
- 외부 repo 의존이 없다.

위험:

- 코드 중복이 생긴다.
- 원본과 drift가 생긴다.
- 검증된 driver core를 일부만 복사하면서 신뢰성이 저하될 수 있다.
- update 추적이 어렵다.
- license / authorship / history 추적이 약해진다.
- XGT-specific source가 Runtime core에 섞일 위험이 있다.

결론:

- `XgtDriverCore` 핵심 로직 copy는 비권장이다.
- `ProcessDataPayloadBuilder` 규칙, address helper, mapping helper 같은 작은 helper / business rule 재구성은 후속 Adapter / Pilot Flow 단계에서 검토 가능하다.

## 12. 후보 E: NuGet / package reference

개념:

- `XgtDriverCore`를 package로 배포하고 `CAAutomationHub` adapter가 package reference로 참조한다.

판정:

- 장기적으로 가장 깔끔한 후보다.
- 현재 pilot 단계에는 무겁다.

장점:

- version pinning이 가능하다.
- restore / CI 재현성이 좋다.
- dependency 관리가 표준화된다.

위험:

- package pipeline이 필요하다.
- 지금 pilot 속도에는 무겁다.
- local debugging이 불편할 수 있다.
- package publish / versioning 운영 부담이 있다.

결론:

- 장기 표준화 목표로 남기고, 지금 당장 선택하기에는 이르다.

## 13. 후보 F: CAAutomationHub repo 내부 Adapter project 먼저 생성

개념:

- `CAAutomationHub` repo 안에 `src\CAAutomationHub.Runtime.XgtAdapter` 같은 adapter project boundary를 검토한다.
- 초기에는 `XgtDriverCore` reference 없이 boundary skeleton만 검토할 수 있다.
- 실제 reference는 sibling repo clean / retrieval 전략 확정 이후 연결한다.

판정:

- `AH-RUNTIME-44`의 가장 현실적인 다음 단계다.

장점:

- 사용자 선호인 "CAAutomationHub 안에 녹이기"와 맞는다.
- Runtime core vendor-neutral 원칙을 유지할 수 있다.
- project boundary를 먼저 세울 수 있다.
- `XgtDriverCore` retrieval 결정을 조금 늦출 수 있다.
- Adapter tests를 fake / stub로 먼저 설계할 수 있다.

위험:

- 실제 `XgtDriverCore` 연동은 아직 안 된다.
- adapter skeleton이 실제 API와 어긋날 수 있다.
- retrieval 결정을 계속 미루면 실사용 연결이 늦어질 수 있다.

결론:

- `AH-RUNTIME-44`에서는 이 방향을 우선 검토한다.
- 단, 실제 project 추가 / reference 추가는 `AH-RUNTIME-44`에서 하지 않는다.

## 14. 후보 G: Test-only reference 우선

개념:

    Integration test project
        -> sibling XgtDriverCore
        -> FakePlc / FakePlcScenarioServer

판정:

- `FakePlc` harness 검증에는 유용하다.
- Adapter boundary보다 먼저 갈 단계로는 약하다.

장점:

- Runtime production code는 안전하다.
- `FakePlc` scenario 검증을 먼저 가져올 수 있다.
- 실제 통신 자산을 실험적으로 연결할 수 있다.

위험:

- production adapter 구현은 여전히 남는다.
- test-only wiring이 실제 runtime architecture를 늦출 수 있다.
- local path coupling은 여전히 존재한다.

결론:

- `AH-RUNTIME-44` 이후 integration harness 단계에서 검토하는 편이 안전하다.

## 15. 사용자 선호 반영 판단

사용자 선호:

- 별도 외부 프로젝트 관리 범위를 키우고 싶지 않음
- 가능하면 `CAAutomationHub` 안에 녹이고 싶음

해석:

- 이 선호를 Runtime core에 XGT를 섞자는 뜻으로 해석하지 않는다.
- 외부 repo 관리 부담을 줄이고 싶다는 의미로 해석한다.
- `CAAutomationHub` repo 내부 adapter project 또는 `external` / subtree folder가 절충안일 수 있다.
- 장기적으로는 single repo clone으로 재현 가능한 구조가 선호될 가능성이 높다.
- 단, Runtime core vendor-neutral 원칙은 유지해야 한다.

판단:

- 단기적으로는 `CAAutomationHub` repo 내부 Adapter project boundary를 먼저 세우는 것이 사용자 선호와 Runtime boundary를 함께 만족한다.
- 장기적으로는 subtree 또는 package가 사용자 관리 부담과 재현성 사이에서 검토 가치가 있다.
- submodule은 version pinning에는 강하지만 사용자에게 Git 관리 부담을 늘릴 수 있다.

## 16. 권장안

권장 순서:

1. `AH-RUNTIME-44`는 `CAAutomationHub` 내부 Adapter project boundary review로 진행한다.
2. 실제 `XgtDriverCore` reference 추가는 금지 상태를 유지한다.
3. sibling repo dirty state 정리 및 clean commit anchor를 먼저 확보해야 한다.
4. retrieval 방식은 단기 local pilot이면 sibling local reference를 제한적으로 사용할 수 있다.
5. 재현성 / 운영 기준이면 git subtree를 우선 검토한다.
6. upstream history / 독립 repo 추적이 더 중요해지면 submodule을 검토한다.
7. NuGet은 장기 표준화 목표로 보류한다.

핵심 판정:

- `AH-RUNTIME-44`의 전제는 Adapter boundary first, retrieval after clean anchor다.

Boundary 영향:

- `CAAutomationHub.Runtime` core는 계속 vendor-neutral이다.
- Runtime core는 `XgtDriverCore`, `FakePlc`, `XgtChannelRunner`를 직접 참조하지 않는다.
- XGT-specific code는 Adapter / Source / integration test 계층으로 격리한다.
- `FakePlc`는 production dependency가 아니라 harness dependency로 유지한다.
- `XgtChannelRunner`는 direct reference가 아니라 helper / mapping idea / pilot business flow reference로 유지한다.

## 17. AH-RUNTIME-44 지시문 보정 포인트

사용자가 `AH-RUNTIME-44` 지시문을 파일로 보관하고 있으므로, AH-RUNTIME-43 결과를 반영해 다음 사항을 보정해야 한다.

- Adapter project는 `CAAutomationHub` repo 내부 project로 우선 검토한다.
- Runtime core 직접 `XgtDriverCore` / `FakePlc` / `XgtChannelRunner` reference는 계속 금지한다.
- 실제 reference 추가는 sibling repo clean 또는 commit anchor 확보 전 금지한다.
- `XgtDriverCore`는 Adapter / Source 계층 dependency 후보다.
- `FakePlc`는 test / integration harness dependency 후보다.
- `XgtChannelRunner`는 direct reference 금지, helper / business flow reference로만 사용한다.
- `WorkStartPilotService.RunOnceAsync(...)`는 Pilot Flow / business transaction boundary 검토 anchor로 유지한다.
- source copy는 driver core가 아니라 helper / rule 재구성 수준으로 제한한다.

## 18. 제외한 범위

이번 AH-RUNTIME-43에서는 다음을 하지 않았다.

- 코드 수정
- 테스트 수정
- solution / csproj 수정
- `ProjectReference` 추가
- `PackageReference` 추가
- submodule 추가
- subtree 작업
- source copy
- git clone
- commit
- Closeout 문서 생성 외 작업
- `ContextPublisher` 수정
- `ContextPublisher` 자동 publish 재도입

이번 단계는 기존 XGT 자산 retrieval / reference / repository strategy를 결정하기 위한 문서화 단계다.

## 19. 실행한 명령

AH-RUNTIME-43 Boundary Review 당시 실행한 명령:

현재 repo:

- `git status --short`
- `git branch --show-current`
- `git remote -v`
- `git log --oneline -5`
- `dotnet sln list`
- `rg "<ProjectReference|PackageReference" . -g "*.csproj"`
- `Get-Content src\CAAutomationHub.Runtime\CAAutomationHub.Runtime.csproj`
- `Get-Content tests\CAAutomationHub.Runtime.Tests\RuntimeProjectReferenceBoundaryTests.cs`

sibling repo:

- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore status --short`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore branch --show-current`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore remote -v`
- `git -C C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore log --oneline -5`
- `dotnet sln C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore\AutomationHub.XgtDriverCore.sln list`
- `rg "<ProjectReference|PackageReference" C:\AutomationHub.Rebuild\AutomationHub.XgtDriverCore -g "*.csproj"`

추가 확인:

- relevant csproj
- `Directory.Build.props`
- AH-RUNTIME-42 closeout
- cognitive context docs
- final git status
- `git diff --stat`

AH-RUNTIME-43 Closeout 문서 작성 후 실행한 검증:

- `git diff -- docs/harness/AH-RUNTIME-43.md`
- `git diff --check`
- `git status --short`
- `Get-Content docs\harness\AH-RUNTIME-43.md`

## 20. Self-Check

판정: ACCEPT

이유:

- AH-RUNTIME-43 Boundary Review 결과를 `docs/harness/AH-RUNTIME-43.md` closeout 문서로 기록했다.
- 현재 `CAAutomationHub` repo 상태, branch, remote, latest commit, clean working tree를 기록했다.
- sibling `AutomationHub.XgtDriverCore` repo 상태, branch, remote, latest commit, dirty working tree를 기록했다.
- dirty file 목록과 untracked file 목록을 기록했다.
- dirty state를 retrieval strategy 기준으로 삼지 말아야 함을 기록했다.
- 현재 project / reference 구조와 `RuntimeProjectReferenceBoundaryTests.cs`의 금지 참조 검증을 기록했다.
- 후보 A / B / C / D / E / F / G를 각각 검토하고 판정을 기록했다.
- 사용자 선호를 Runtime core에 XGT를 섞자는 뜻으로 해석하지 않고, 외부 관리 부담 축소로 해석해야 함을 기록했다.
- 권장안으로 `Adapter boundary first, retrieval after clean anchor`를 기록했다.
- `AH-RUNTIME-44` 지시문 보정 포인트를 기록했다.
- production code, test code, solution, csproj, project reference, package reference, submodule, subtree, source copy, git clone, commit, `ContextPublisher` 수정은 수행하지 않았다.

주의:

- Codex Self-Check는 작업자 자기검증이며, 최종 승인 여부는 사용자 검토 후 결정된다.
