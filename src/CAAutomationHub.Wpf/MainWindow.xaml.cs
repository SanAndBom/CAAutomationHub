using System.Windows;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.CreateDefaultPilotLocal();
    }
}
