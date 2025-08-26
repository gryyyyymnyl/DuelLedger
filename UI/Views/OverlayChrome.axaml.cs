using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace DuelLedger.UI.Views;

public partial class OverlayChrome : UserControl
{
    public OverlayChrome()
    {
        InitializeComponent();
    }

    private void OnDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            (this.GetVisualRoot() as Window)?.BeginMoveDrag(e);
        }
    }
}
