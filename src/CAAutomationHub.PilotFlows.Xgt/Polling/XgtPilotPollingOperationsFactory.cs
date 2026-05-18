using AutomationHub.XgtDriverCore.Client;
using AutomationHub.XgtDriverCore.Transport;
using CAAutomationHub.PilotFlows.Xgt.WorkComplete;
using CAAutomationHub.PilotFlows.Xgt.WorkStart;

namespace CAAutomationHub.PilotFlows.Xgt.Polling;

public static class XgtPilotPollingOperationsFactory
{
    public static XgtPilotPollingOperations Create(
        XgtPilotConnectionOptions connectionOptions,
        WorkStartXgtReadOptions workStartReadOptions,
        WorkCompleteXgtReadOptions workCompleteReadOptions)
    {
        ArgumentNullException.ThrowIfNull(connectionOptions);
        ArgumentNullException.ThrowIfNull(workStartReadOptions);
        ArgumentNullException.ThrowIfNull(workCompleteReadOptions);

        var transport = new TcpTransport(new XgtTransportOptions
        {
            Host = connectionOptions.Host,
            Port = connectionOptions.Port,
            ConnectTimeout = connectionOptions.ConnectTimeout,
            SendTimeout = connectionOptions.SendTimeout,
            ReceiveTimeout = connectionOptions.ReceiveTimeout
        });
        var session = new XgtSession(transport);

        var startAckOnOperations = new WorkStartXgtPlcOperations(
            session,
            workStartReadOptions,
            WorkStartXgtWriteOptions.Default);
        var startAckOffOperations = new WorkStartXgtPlcOperations(
            session,
            workStartReadOptions,
            new WorkStartXgtWriteOptions(
                WorkStartXgtWriteOptions.DefaultProcessPayloadWriteVariable,
                WorkStartXgtWriteOptions.DefaultStartAckWriteVariable,
                startAckValue: 0,
                WorkStartXgtWriteOptions.DefaultErrorCodeWriteVariable));
        var completeOperations = new WorkCompleteXgtPlcOperations(
            session,
            workCompleteReadOptions,
            WorkCompleteXgtWriteOptions.Default);

        return new XgtPilotPollingOperations(
            startAckOnOperations,
            startAckOffOperations,
            completeOperations);
    }
}
