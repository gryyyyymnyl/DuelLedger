using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DuelLedger.UI.ViewModels;

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
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        if (e.Source is Control s && s.GetSelfAndVisualAncestors().OfType<Control>().Any(c => c.Classes.Contains("NoDrag"))) return;

        if (e.Source is Control c)
        {
            if (c is Menu || c is MenuItem || c is Button || c is Avalonia.Controls.Primitives.ToggleButton ||
                c is ComboBox || c is TextBox || c is Slider || c is ListBox ||
                c is DataGrid || c is CheckBox || c is RadioButton)
                return;

            if (c.GetSelfAndVisualAncestors().OfType<Menu>().Any() ||
                c.GetSelfAndVisualAncestors().OfType<MenuItem>().Any() ||
                c.GetSelfAndVisualAncestors().OfType<ComboBox>().Any())
                return;
        }
        BeginMoveDrag(e);
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
