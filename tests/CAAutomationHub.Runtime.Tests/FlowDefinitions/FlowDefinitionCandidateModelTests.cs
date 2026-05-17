using CAAutomationHub.FlowDefinitions;

namespace CAAutomationHub.Runtime.Tests.FlowDefinitions;

public sealed class FlowDefinitionCandidateModelTests
{
    [Fact]
    public void DefinitionCandidate_PreservesIdentityAndSteps()
    {
        var step = new FlowStepCandidate(
            stepId: "read-ready-signal",
            actionKind: "ReadSignal",
            onSuccess: "build-message",
            onFailure: "mark-failed");

        var candidate = new FlowDefinitionCandidate(
            flowId: "work-start",
            flowKind: "Pilot",
            initialState: "read-ready-signal",
            steps: [step]);

        Assert.Equal("work-start", candidate.FlowId);
        Assert.Equal("Pilot", candidate.FlowKind);
        Assert.Equal("read-ready-signal", candidate.InitialState);
        Assert.Same(step, Assert.Single(candidate.Steps));
    }

    [Fact]
    public void DefinitionCandidate_CopiesStepsAtCreation()
    {
        var steps = new List<FlowStepCandidate>
        {
            new(
                stepId: "read-ready-signal",
                actionKind: "ReadSignal",
                onSuccess: "build-message",
                onFailure: "mark-failed")
        };

        var candidate = new FlowDefinitionCandidate(
            flowId: "work-start",
            flowKind: "Pilot",
            initialState: "read-ready-signal",
            steps: steps);

        steps.Add(new FlowStepCandidate(
            stepId: "unexpected-late-step",
            actionKind: "LateMutation",
            onSuccess: null,
            onFailure: null));

        Assert.Single(candidate.Steps);
    }

    [Fact]
    public void StepCandidate_PreservesTransitionAndRequiredReferenceKeys()
    {
        var step = new FlowStepCandidate(
            stepId: "build-message",
            actionKind: "BuildPayload",
            onSuccess: "write-result",
            onFailure: "mark-failed",
            requiredBindingRefs: ["readySignal", "messageLayout"],
            requiredPolicyRefs: ["failurePolicy"]);

        Assert.Equal("build-message", step.StepId);
        Assert.Equal("BuildPayload", step.ActionKind);
        Assert.Equal("write-result", step.OnSuccess);
        Assert.Equal("mark-failed", step.OnFailure);
        Assert.Equal(["readySignal", "messageLayout"], step.RequiredBindingRefs);
        Assert.Equal(["failurePolicy"], step.RequiredPolicyRefs);
    }

    [Fact]
    public void DefinitionCandidate_PreservesBindingAndPolicyReferences()
    {
        var binding = new FlowReference(key: "readySignal", kind: "Signal");
        var policy = new FlowPolicyReference(key: "failurePolicy", kind: "FailureHandling", status: "Draft");

        var candidate = new FlowDefinitionCandidate(
            flowId: "work-start",
            flowKind: "Pilot",
            initialState: "read-ready-signal",
            steps: [],
            bindings: new Dictionary<string, FlowReference>
            {
                [binding.Key] = binding
            },
            policies: new Dictionary<string, FlowPolicyReference>
            {
                [policy.Key] = policy
            });

        Assert.Equal("Signal", candidate.Bindings?["readySignal"].Kind);
        Assert.Equal("FailureHandling", candidate.Policies?["failurePolicy"].Kind);
        Assert.Equal("Draft", candidate.Policies?["failurePolicy"].Status);
    }

    [Fact]
    public void CandidateAssembly_DoesNotDependOnRuntimeProject()
    {
        var referencedAssemblyNames = typeof(FlowDefinitionCandidate)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("CAAutomationHub.Runtime", referencedAssemblyNames);
    }
}
