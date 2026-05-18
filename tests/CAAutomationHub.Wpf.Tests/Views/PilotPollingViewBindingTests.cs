using System.Xml.Linq;

namespace CAAutomationHub.Wpf.Tests.Views;

public sealed class PilotPollingViewBindingTests
{
    [Fact]
    public void PollingButtons_BindToPollingCommands()
    {
        var document = XDocument.Load(FindRepositoryFile(
            "src",
            "CAAutomationHub.Wpf",
            "Views",
            "Pilot",
            "PilotPollingView.xaml"));

        XNamespace wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var buttons = document
            .Descendants(wpf + "Button")
            .ToDictionary(
                element => (string?)element.Attribute("Content") ?? string.Empty,
                element => (string?)element.Attribute("Command"));

        Assert.Equal("{Binding StartPollingCommand}", buttons["Polling 시작"]);
        Assert.Equal("{Binding StopPollingCommand}", buttons["Polling 중지"]);
        Assert.Equal("{Binding PollOnceCommand}", buttons["Poll Once"]);
    }

    [Fact]
    public void MainWindow_ContainsPilotPollingViewBoundToPilotPollingViewModel()
    {
        var document = XDocument.Load(FindRepositoryFile(
            "src",
            "CAAutomationHub.Wpf",
            "MainWindow.xaml"));

        XNamespace pilot = "clr-namespace:CAAutomationHub.Wpf.Views.Pilot";
        var pilotView = document
            .Descendants(pilot + "PilotPollingView")
            .Single();

        Assert.Equal("{Binding PilotPolling}", (string?)pilotView.Attribute("DataContext"));
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
