using System.Windows;
using System.Windows.Controls;
using GarminCoach.ViewModels;

namespace GarminCoach.Views;

public partial class SettingsDialog : Window
{
    private readonly SettingsViewModel _vm;

    public SettingsDialog(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        PasswordBoxControl.Password = vm.GarminPassword;
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
        {
            _vm.GarminPassword = pb.Password;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        DialogResult = _vm.Saved;
        Close();
    }
}
