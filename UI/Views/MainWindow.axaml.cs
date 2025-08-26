using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using DuelLedger.UI.ViewModels;

namespace DuelLedger.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 透過は起動時に許可セットを固定
        TransparencyLevelHint = new[]
        {
            WindowTransparencyLevel.Transparent,
            WindowTransparencyLevel.AcrylicBlur,
            WindowTransparencyLevel.Mica
        };

        // VM変更監視
        this.GetObservable(DataContextProperty).Subscribe(_ =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                ApplyTransparency(vm.IsBackgroundTransparent);
                vm.PropertyChanged -= VmOnPropertyChanged;
                vm.PropertyChanged += VmOnPropertyChanged;
            }
        });
    }

    private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MainWindowViewModel vm && e.PropertyName == nameof(MainWindowViewModel.IsBackgroundTransparent))
        {
            ApplyTransparency(vm.IsBackgroundTransparent);
        }
    }

    private void ApplyTransparency(bool on)
    {
        var brush = on ? Brushes.Transparent : ResolveSolidWindowBackground();
        Background = brush;

        foreach (var ctl in this.GetVisualDescendants().OfType<Control>())
        {
            if (!ctl.Classes.Contains("backdrop"))
                continue;

            switch (ctl)
            {
                case Panel p:
                    p.Background = brush;
                    break;
                case Border b:
                    b.Background = brush;
                    break;
            }
        }
    }

    private static IBrush ResolveSolidWindowBackground()
    {
        if (Application.Current?.TryFindResource("SolidWindowBackgroundBrush", out var obj) == true && obj is IBrush brush)
            return brush;
        return Brushes.White;
    }

    private void MinimizeButton_Click(object? sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
        => Close();

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Control c && c is not Button && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }
}