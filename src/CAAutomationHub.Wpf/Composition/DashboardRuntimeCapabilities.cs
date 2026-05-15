namespace CAAutomationHub.Wpf.Composition;

public sealed class DashboardRuntimeCapabilities
{
    public static DashboardRuntimeCapabilities Editable { get; } =
        new(canAddPlc: true, canEditPlc: true, canDeletePlc: true);

    public static DashboardRuntimeCapabilities ReadOnly { get; } =
        new(canAddPlc: false, canEditPlc: false, canDeletePlc: false);

    public DashboardRuntimeCapabilities(
        bool canAddPlc,
        bool canEditPlc,
        bool canDeletePlc)
    {
        CanAddPlc = canAddPlc;
        CanEditPlc = canEditPlc;
        CanDeletePlc = canDeletePlc;
    }

    public bool CanAddPlc { get; }

    public bool CanEditPlc { get; }

    public bool CanDeletePlc { get; }

    public bool CanEditConfiguration => CanAddPlc || CanEditPlc || CanDeletePlc;
}
