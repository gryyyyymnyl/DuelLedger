using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DuelLedger.UI.ViewModels;
using System;
using System.Linq;

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
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (e.Source is Control s && s.GetSelfAndVisualAncestors()
                .OfType<Control>().Any(c => c is Menu || c is MenuItem || c is Avalonia.Controls.Primitives.Popup || c is Avalonia.Controls.Primitives.PopupRoot))
                return;
            BeginMoveDrag(e);
        }
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
