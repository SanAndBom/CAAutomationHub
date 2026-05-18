using CAAutomationHub.PilotApp.Polling;
using CAAutomationHub.PilotApp.WorkStart;
using CAAutomationHub.PilotComposition.Configuration;
using CAAutomationHub.PilotFlows.WorkComplete;
using CAAutomationHub.PilotFlows.WorkStart;
using CAAutomationHub.PilotFlows.Xgt.Polling;
using CAAutomationHub.PilotFlows.Xgt.WorkComplete;
using CAAutomationHub.PilotFlows.Xgt.WorkStart;

namespace CAAutomationHub.PilotComposition.Polling;

public static class PilotLocalComposition
{
    public static PilotPollingComposition Create(PilotLocalConfiguration configuration)
    {
        PilotLocalConfigurationLoader.Validate(configuration);

        return configuration.Profile switch
        {
            PilotProfileKind.Fake => CreateFake(configuration),
            PilotProfileKind.FakePlcLocal => CreateFakePlcLocal(configuration),
            PilotProfileKind.RealReadOnly => throw new NotSupportedException(
                "RealReadOnly pilot profile is not enabled in AH-PILOT-LIVE fast track."),
            PilotProfileKind.RealPilot => throw new NotSupportedException(
                "RealPilot pilot profile is not enabled in AH-PILOT-LIVE fast track."),
            _ => throw new NotSupportedException($"Pilot profile '{configuration.Profile}' is not supported.")
        };
    }

    private static PilotPollingComposition CreateFake(PilotLocalConfiguration configuration)
    {
        var service = new PilotPollingService(
            new FakePilotPollingFlowPort(configuration.Plc.TargetId),
            new PilotPollingOptions { TargetId = configuration.Plc.TargetId });

        return new PilotPollingComposition(
            service,
            configuration,
            "Fake pilot polling profile loaded.");
    }

    private static PilotPollingComposition CreateFakePlcLocal(PilotLocalConfiguration configuration)
    {
        if (!IsLoopbackHost(configuration.Plc.Host))
        {
            throw new InvalidOperationException("FakePlcLocal profile only allows localhost or loopback PLC targets.");
        }

        var operations = XgtPilotPollingOperationsFactory.Create(
            new XgtPilotConnectionOptions
            {
                Host = configuration.Plc.Host,
                Port = configuration.Plc.Port
            },
            new WorkStartXgtReadOptions(
                configuration.Plc.ReadStartVariable,
                configuration.Plc.ReadWordCount),
            new WorkCompleteXgtReadOptions(
                configuration.Plc.ReadStartVariable,
                configuration.Plc.ReadWordCount));

        var workStartOptions = new WorkStartFlowOptions
        {
            StartSignalWordIndex = configuration.Plc.StartSignalWordIndex,
            LotId1WordOffset = configuration.Plc.LotId1WordOffset,
            LotId2WordOffset = configuration.Plc.LotId2WordOffset,
            LotIdWordLength = configuration.Plc.LotIdWordLength
        };
        var workCompleteOptions = new WorkCompleteAckOptions
        {
            CompleteSignalWordIndex = configuration.Plc.CompleteSignalWordIndex
        };

        var workStartFlow = new WorkStartFlowService(
            operations.StartAckOnOperations,
            new FakeWorkStartDataQuery(),
            workStartOptions);
        var workStartExecutionService = new WorkStartExecutionService(
            new WorkStartFlowServiceRunner(workStartFlow));
        var reader = new PilotPollingRequestStateReader(
            operations.StartAckOnOperations,
            operations.CompleteOperations,
            workStartOptions,
            workCompleteOptions);
        var port = new PilotPollingFlowPort(
            reader,
            workStartExecutionService,
            new WorkStartAckOffService(
                operations.StartAckOffOperations,
                new WorkStartAckOffOptions
                {
                    StartSignalWordIndex = configuration.Plc.StartSignalWordIndex
                }),
            new WorkCompleteAckService(operations.CompleteOperations, workCompleteOptions));

        var service = new PilotPollingService(
            port,
            new PilotPollingOptions { TargetId = configuration.Plc.TargetId });

        return new PilotPollingComposition(
            service,
            configuration,
            $"FakePlcLocal pilot polling profile loaded for {configuration.Plc.Host}:{configuration.Plc.Port}.");
    }

    private static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("::1", StringComparison.OrdinalIgnoreCase);

    private sealed class FakePilotPollingFlowPort : IPilotPollingFlowPort
    {
        private readonly string _targetId;

        public FakePilotPollingFlowPort(string targetId)
        {
            _targetId = targetId;
        }

        public ValueTask<PilotPollingRequestState> ReadRequestStateAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(new PilotPollingRequestState
            {
                StartRequestActive = true,
                CompleteRequestActive = false,
                StartLotId = "PILOT-FAKE-LOT"
            });
        }

        public ValueTask<WorkStartExecutionResult> ExecuteWorkStartAsync(
            WorkStartExecutionRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var startedAt = DateTimeOffset.Parse("2026-05-18T00:00:00+00:00");

            return ValueTask.FromResult(new WorkStartExecutionResult
            {
                Succeeded = true,
                Status = "Succeeded",
                Step = "completed",
                ErrorCode = 0,
                ErrorCodeName = "None",
                Message = $"Fake WorkStart processed for {_targetId}.",
                SelectedLotId = "PILOT-FAKE-LOT",
                ErrorWriteExpected = false,
                StartedAt = startedAt,
                CompletedAt = startedAt.AddSeconds(1),
                Duration = TimeSpan.FromSeconds(1)
            });
        }

        public ValueTask<WorkStartAckOffResult> ClearWorkStartAckAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(WorkStartAckOffResult.AckOffWritten());
        }

        public ValueTask<WorkCompleteAckResult> WriteWorkCompleteAckOnAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(WorkCompleteAckResult.AckOnWritten());
        }

        public ValueTask<WorkCompleteAckResult> ClearWorkCompleteAckAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(WorkCompleteAckResult.AckOffWritten());
        }
    }

    private sealed class FakeWorkStartDataQuery : IWorkStartDataQuery
    {
        public ValueTask<WorkStartDataQueryResult> QueryAsync(
            string lotId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(WorkStartDataQueryResult.Success(new WorkStartProcessData
            {
                LotId = lotId,
                Profile = "FAKE-PROFILE",
                Tblr = "TB01",
                WinType = "W1",
                CutSize = 1200,
                Lr = "L",
                RollerYn = "Y",
                RollerHolePos = 10,
                RollerHoleWidth = 20,
                RollerHoleLength = 30,
                RollerType = "R1",
                CutDegree = 45
            }));
        }
    }
}
