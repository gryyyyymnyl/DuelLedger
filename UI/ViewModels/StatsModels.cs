using System;
using System.Collections.ObjectModel;
using DuelLedger.UI.Models;

namespace DuelLedger.UI.ViewModels;

public sealed class ClassVsRow
{
    public PlayerClass Opponent { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public double WinRate => (Wins + Losses) == 0 ? 0 : (double)Wins / (Wins + Losses);
    public string WinRatePct => (WinRate * 100).ToString("0.0") + "%";
    public string BarText { get; init; } = string.Empty;
}

public sealed class Totals
{
    public int Wins { get; init; }
    public int Losses { get; init; }
    public double WinRate => (Wins + Losses) == 0 ? 0 : (double)Wins / (Wins + Losses);
    public string WinRatePct => (WinRate * 100).ToString("0.0") + "%";
}