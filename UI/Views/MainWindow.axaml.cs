using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DuelLedger.UI.Converters;
using DuelLedger.UI.Models;
using DuelLedger.UI.Services;
using DuelLedger.UI.ViewModels;
using System;
using System.Linq;

namespace DuelLedger.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    public MainWindow(MainWindowViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Application.Current?.Resources["UiMap"] is UiMapProvider map)
            {
                var size = new Size(24, 24);
                var theme = ActualThemeVariant ?? ThemeVariant.Light;
                var items = Enum.GetValues<PlayerClass>().Where(c => c != PlayerClass.Unknown)
                    .Select(c =>
                    {
                        var uri = new Uri(map.Get($"Class.{c}").icon);
                        var key = ClassIconConverter.BuildKey(c, size, theme);
                        return (key, uri, size);
                    });
                SvgIconCache.Instance.Warmup(items);
            }
        });
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
