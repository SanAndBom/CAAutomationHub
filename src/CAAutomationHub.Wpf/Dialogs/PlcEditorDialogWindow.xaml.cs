using System.Windows;
using CAAutomationHub.Wpf.Models.Dashboard;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf.Dialogs;

public partial class PlcEditorDialogWindow : Window
{
    public PlcEditorDialogWindow()
        : this(null, isEditMode: false)
    {
    }

    public PlcEditorDialogWindow(PlcDashboardConfiguration? configuration, bool isEditMode)
    {
        InitializeComponent();
        DataContext = configuration is null
            ? new PlcEditorDialogViewModel()
            : new PlcEditorDialogViewModel(configuration, isEditMode);
    }

    public PlcDashboardConfiguration? ResultConfiguration { get; private set; }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is PlcEditorDialogViewModel viewModel)
        {
            ResultConfiguration = viewModel.ToConfiguration();
        }

        DialogResult = true;
    }
}
