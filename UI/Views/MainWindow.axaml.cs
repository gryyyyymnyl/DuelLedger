using System;
using Avalonia;
using Avalonia.Controls;
using DuelLedger.UI.ViewModels;

namespace DuelLedger.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var canvas = this.FindControl<ItemsControl>("HistoryCanvas");
        canvas?.GetObservable(BoundsProperty).Subscribe(b =>
        {
            if (DataContext is MainWindowViewModel vm)
                vm.CanvasWidth = b.Width;
        });
    }
}