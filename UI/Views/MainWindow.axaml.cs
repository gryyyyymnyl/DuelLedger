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
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.PropertyChanged += VmOnPropertyChanged;
            ApplyTransparency(vm.IsBackgroundTransparent);
        }
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
        TransparencyLevelHint = on
            ? new[] { WindowTransparencyLevel.Transparent, WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Mica }
            : new[] { WindowTransparencyLevel.None };
        Background = on ? Brushes.Transparent : (IBrush?)this.FindResource("SolidWindowBackgroundBrush") ?? Brushes.White;
    }

    private void MinimizeButton_Click(object? sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
        => Close();

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Control c && c is not Button && c is not MenuItem && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}