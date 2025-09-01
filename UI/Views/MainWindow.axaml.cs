using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;
using DuelLedger.UI.ViewModels;
using System;
using System.Linq;

namespace DuelLedger.UI.Views;

public partial class MainWindow : Window
{
    public static readonly StyledProperty<bool> IsTransparencyEnabledProperty =
        AvaloniaProperty.Register<MainWindow, bool>(nameof(IsTransparencyEnabled));

    public bool IsTransparencyEnabled
    {
        get => GetValue(IsTransparencyEnabledProperty);
        set => SetValue(IsTransparencyEnabledProperty, value);
    }

    public MainWindow()
    {
        InitializeComponent();
        this.GetObservable(IsTransparencyEnabledProperty)
            .Subscribe(enabled => ApplyTransparency(enabled));
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

    private void OnEnableTransparency(object? sender, RoutedEventArgs e)
        => IsTransparencyEnabled = true;

    private void OnDisableTransparency(object? sender, RoutedEventArgs e)
        => IsTransparencyEnabled = false;

    private IBrush? _originalBackground;

    private void ApplyTransparency(bool enabled)
    {
        _originalBackground ??= Background;

        if (enabled)
        {
            if (_originalBackground is SolidColorBrush solid)
            {
                Background = new SolidColorBrush(solid.Color) { Opacity = 0.5 };
            }
            else
            {
                Background = new SolidColorBrush(Colors.White) { Opacity = 0.5 };
            }

            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        }
        else
        {
            Background = _originalBackground;
            TransparencyLevelHint = Array.Empty<WindowTransparencyLevel>();
        }
    }
}
