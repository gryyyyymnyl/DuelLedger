using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
namespace DuelLedger.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnToggleTheme(object? sender, RoutedEventArgs e)
    {
        var app = Application.Current;
        if (app is null)
            return;

        var current = app.ActualThemeVariant;
        app.RequestedThemeVariant = current == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    }
}