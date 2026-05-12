using System.Windows;

namespace CAAutomationHub.Wpf.Dialogs;

public partial class PlcEditorDialogWindow : Window
{
    public PlcEditorDialogWindow()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
