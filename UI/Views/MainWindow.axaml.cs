using Avalonia.Controls;
using Avalonia;
using DuelLedger.UI.ViewModels;
namespace DuelLedger.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        HistoryCanvas.AttachedToVisualTree += (_, __) => UpdateHistoryLayout();
        HistoryCanvas.SizeChanged += (_, __) => UpdateHistoryLayout();
    }

    private void UpdateHistoryLayout()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var width = HistoryCanvas.Bounds.Width;
            vm.HistoryVm.ScheduleLayout(width);
        }
    }
}