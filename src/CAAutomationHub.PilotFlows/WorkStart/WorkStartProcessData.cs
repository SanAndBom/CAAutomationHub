namespace CAAutomationHub.PilotFlows.WorkStart;

public sealed record WorkStartProcessData
{
    public string? LotId { get; init; }

    public string? Profile { get; init; }

    public string? Tblr { get; init; }

    public string? WinType { get; init; }

    public int CutSize { get; init; }

    public string? Lr { get; init; }

    public string? RollerYn { get; init; }

    public int RollerHolePos { get; init; }

    public int RollerHoleWidth { get; init; }

    public int RollerHoleLength { get; init; }

    public string? RollerType { get; init; }

    public int CutDegree { get; init; }
}
