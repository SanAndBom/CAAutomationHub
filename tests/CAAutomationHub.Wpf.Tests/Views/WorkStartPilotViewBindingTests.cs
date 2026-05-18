using System.Xml.Linq;

namespace CAAutomationHub.Wpf.Tests.Views;

public sealed class WorkStartPilotViewBindingTests
{
    [Fact]
    public void ExecuteButton_BindsToExecuteOnceCommand()
    {
        var document = XDocument.Load(FindRepositoryFile(
            "src",
            "CAAutomationHub.Wpf",
            "Views",
            "Pilot",
            "WorkStartPilotView.xaml"));

        XNamespace wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var executeButton = document
            .Descendants(wpf + "Button")
            .Single(element => (string?)element.Attribute("Content") == "착공 실행");

        Assert.Equal("{Binding ExecuteOnceCommand}", (string?)executeButton.Attribute("Command"));
        Assert.Null(executeButton.Attribute("IsEnabled"));
    }

    private static string FindRepositoryFile(params string[] relativePathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativePathParts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find repository file.", Path.Combine(relativePathParts));
    }
}
