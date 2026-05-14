using System.Xml.Linq;

namespace CAAutomationHub.Runtime.Tests;

public sealed class RuntimeProjectReferenceBoundaryTests
{
    [Fact]
    public void RuntimeProject_ReferencesOnlyContractsProject()
    {
        var projectPath = FindRepositoryRoot()
            .Combine("src", "CAAutomationHub.Runtime", "CAAutomationHub.Runtime.csproj");

        var project = XDocument.Load(projectPath.FullName);
        var references = project
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .ToArray();

        var reference = Assert.Single(references);
        Assert.Equal(@"..\CAAutomationHub.Contracts\CAAutomationHub.Contracts.csproj", reference);
    }

    [Theory]
    [InlineData("CAAutomationHub.Wpf")]
    [InlineData("XgtDriverCore")]
    [InlineData("XgtChannelRunner")]
    [InlineData("FakePlc")]
    public void RuntimeProject_DoesNotReferenceForbiddenProjects(string forbiddenReference)
    {
        var projectPath = FindRepositoryRoot()
            .Combine("src", "CAAutomationHub.Runtime", "CAAutomationHub.Runtime.csproj");

        var projectText = File.ReadAllText(projectPath.FullName);

        Assert.DoesNotContain(forbiddenReference, projectText, StringComparison.OrdinalIgnoreCase);
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
