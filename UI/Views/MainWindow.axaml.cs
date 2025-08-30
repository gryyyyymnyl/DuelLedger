using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DuelLedger.UI.ViewModels;

namespace DuelLedger.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        if (e.Source is Control src)
        {
            bool inMenu = src.GetSelfAndVisualAncestors().OfType<Menu>().Any()
                        || src.GetSelfAndVisualAncestors().OfType<MenuItem>().Any();
            if (inMenu) return;
        }
        BeginMoveDrag(e);
    }

    private void OnMinimize(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
