using Avalonia.Controls;
using DuelLedger.UI.Platform.Windows;
using DuelLedger.UI.ViewModels;
using DuelLedger.UI.Models;

namespace DuelLedger.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Opened += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.Settings.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(UiSettings.IsClickThrough))
                        Win32WindowEx.EnableClickThrough(this, vm.Settings.IsClickThrough);
                };
                Win32WindowEx.EnableClickThrough(this, vm.Settings.IsClickThrough);
            }
        };
    }
}