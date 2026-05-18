using System.Xml.Linq;
using CAAutomationHub.PilotApp.WorkStart;
using CAAutomationHub.PilotComposition.WorkStart;

namespace CAAutomationHub.PilotComposition.Tests.WorkStart;

public sealed class WorkStartDemoCompositionTests
{
    [Fact]
    public void Create_ReturnsWorkStartExecutionService()
    {
        IWorkStartExecutionServiceFactory factory = new WorkStartDemoComposition();

        IWorkStartExecutionService service = factory.Create();

        Assert.NotNull(service);
    }

    [Fact]
    public async Task CreatedService_ReturnsDemoSuccessResult()
    {
        var composition = new WorkStartDemoComposition(
            new WorkStartDemoOptions(SimulatedLotId: "DEMO-LOT-0001"));

        var service = composition.Create();
        var result = await service.ExecuteOnceAsync(new WorkStartExecutionRequest(TargetId: "PLC-DEMO"));

        Assert.True(result.Succeeded);
        Assert.Equal("Succeeded", result.Status);
        Assert.Equal("completed", result.Step);
        Assert.Equal("DEMO-LOT-0001", result.SelectedLotId);
        Assert.Equal(TimeSpan.FromSeconds(1), result.Duration);
    }

    [Fact]
    public void PilotCompositionProject_DoesNotReferenceXgtFakePlcOrSqlClient()
    {
        var projectPath = FindRepositoryRoot()
            .Combine("src", "CAAutomationHub.PilotComposition", "CAAutomationHub.PilotComposition.csproj");

        var project = XDocument.Load(projectPath.FullName);
        var references = project
            .Descendants()
            .Where(static element => element.Name.LocalName is "ProjectReference" or "PackageReference")
            .Select(static element => element.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(references, static reference => reference.Contains("XgtDriverCore", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, static reference => reference.Contains("FakePlc", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, static reference => reference.Contains("XgtChannelRunner", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, static reference => reference.Contains("Microsoft.Data.SqlClient", StringComparison.OrdinalIgnoreCase));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null && !File.Exists(Path.Combine(current.FullName, "CAAutomationHub.sln")))
        {
            current = current.Parent;
        }

        return current ?? throw new InvalidOperationException("Repository root could not be found.");
    }
}

internal static class DirectoryInfoExtensions
{
    public static FileInfo Combine(this DirectoryInfo directory, params string[] paths)
    {
        return new FileInfo(Path.Combine([directory.FullName, .. paths]));
    }
}
