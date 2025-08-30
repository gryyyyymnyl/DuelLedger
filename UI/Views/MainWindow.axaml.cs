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

    private void OnFormatClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DuelLedger.UI.ViewModels.MainWindowViewModel vm) return;
        if (sender is not MenuItem mi) return;

        var val = mi.DataContext; // 子MenuItemのアイテムそのもの
        // null → クリア
        if (val is null) { vm.SelectedFormat = null; return; }

        // 列挙値そのもの
        if (val is DuelLedger.UI.Models.MatchFormat fmt) { vm.SelectedFormat = fmt; return; }

        // 文字列 → 列挙へ
        if (val is string s && Enum.TryParse<DuelLedger.UI.Models.MatchFormat>(s, out var parsed)) { vm.SelectedFormat = parsed; return; }

        // 数値 → 列挙へ
        if (val is IConvertible conv) { try { vm.SelectedFormat = (DuelLedger.UI.Models.MatchFormat)Convert.ToInt32(conv); return; } catch { } }

        // ここまでで決まらない場合は無視
    }
}
