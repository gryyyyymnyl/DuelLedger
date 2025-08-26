using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using DuelLedger.UI.ViewModels;

namespace DuelLedger.UI.Views;

public partial class MainWindow : Window
{
    private SolidColorBrush? _appSurfaceBrush;
    private Color _opaqueColor = Color.FromRgb(0x1E, 0x1E, 0x1E);

    public MainWindow()
    {
        InitializeComponent();

        TransparencyLevelHint = new[]
        {
            WindowTransparencyLevel.Transparent,
            WindowTransparencyLevel.AcrylicBlur,
            WindowTransparencyLevel.Mica
        };

        this.GetObservable(DataContextProperty).Subscribe(_ =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                SetTransparentMode(vm.IsBackgroundTransparent);
                vm.PropertyChanged -= VmOnPropertyChanged;
                vm.PropertyChanged += VmOnPropertyChanged;
            }
        });
    }

    private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MainWindowViewModel vm &&
            e.PropertyName == nameof(MainWindowViewModel.IsBackgroundTransparent))
        {
            SetTransparentMode(vm.IsBackgroundTransparent);
        }
    }

    private void EnsureAppSurfaceBrush()
    {
        if (_appSurfaceBrush is not null)
            return;

        if (Application.Current?.TryFindResource("AppSurfaceBrush", out var obj) == true &&
            obj is SolidColorBrush b)
        {
            _appSurfaceBrush = b;
            _opaqueColor = Color.FromArgb(0xFF, b.Color.R, b.Color.G, b.Color.B);
        }
        else
        {
            _appSurfaceBrush = new SolidColorBrush(_opaqueColor);
            Application.Current!.Resources["AppSurfaceBrush"] = _appSurfaceBrush;
        }
    }

    private void SetTransparentMode(bool on)
    {
        EnsureAppSurfaceBrush();

        if (on)
        {
            _appSurfaceBrush!.Color = Color.FromArgb(0x01, _opaqueColor.R, _opaqueColor.G, _opaqueColor.B);
            Background = Brushes.Transparent;
        }
        else
        {
            _appSurfaceBrush!.Color = _opaqueColor;
            Background = Brushes.Transparent;
        }
    }

    private void MinimizeButton_Click(object? sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
        => Close();

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var src = e.Source as Control;
        if (src is Button || src is MenuItem) return;
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }
}

