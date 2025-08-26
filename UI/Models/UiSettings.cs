using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DuelLedger.UI.Models;

public sealed class UiSettings : INotifyPropertyChanged
{
    private bool _isTransparent;
    public bool IsTransparent
    {
        get => _isTransparent;
        set { if (_isTransparent != value) { _isTransparent = value; OnPropertyChanged(); } }
    }

    private bool _isClickThrough;
    public bool IsClickThrough
    {
        get => _isClickThrough;
        set { if (_isClickThrough != value) { _isClickThrough = value; OnPropertyChanged(); } }
    }

    private double _windowOpacity = 1.0;
    public double WindowOpacity
    {
        get => _windowOpacity;
        set { if (_windowOpacity != value) { _windowOpacity = value; OnPropertyChanged(); } }
    }

    private bool _isTopmost;
    public bool IsTopmost
    {
        get => _isTopmost;
        set { if (_isTopmost != value) { _isTopmost = value; OnPropertyChanged(); } }
    }

    private string _transparencyMode = "None";
    public string TransparencyMode
    {
        get => _transparencyMode;
        set { if (_transparencyMode != value) { _transparencyMode = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
