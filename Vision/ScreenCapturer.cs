// --- ScreenCapturer.cs (DPI対応 + PrintWindow/BitBlt フォールバック) ---
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

public static class ScreenCapturer
{
    // DPI Awareness
    private static bool _dpiInit = false;

    // Win32
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref RECT lpPoints, int cPoints);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest,
                                                               int nWidth, int nHeight,
                                                               IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    // DWM (拡張フレーム境界)
    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    // DPI Awareness API（Windows 10+）
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);
    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    private const int SRCCOPY = 0x00CC0020;
    private const uint PW_RENDERFULLCONTENT = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    /// <summary>最初の呼び出しで DPI Awareness を設定</summary>
    private static void EnsureDpiAware()
    {
        if (_dpiInit) return;
        try
        {
            SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            _dpiInit = true;
#if DEBUG
            Console.WriteLine("[Debug] DPI awareness set: Per-Monitor v2");
#endif
        }
        catch
        {
            // 失敗しても続行
        }
    }

    /// <summary>拡張フレーム境界でウィンドウ矩形を取得。失敗時は GetWindowRect。</summary>
    private static bool TryGetWindowRectPx(IntPtr hWnd, out RECT rect)
    {
        if (DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf<RECT>()) == 0)
            return true;
        return GetWindowRect(hWnd, out rect);
    }

    /// <summary>プロセス名からメインウィンドウをキャプチャして Bitmap を返す</summary>
    public static Bitmap? CaptureWindowByProcessName(string processName)
    {
        EnsureDpiAware();

        var procs = Process.GetProcessesByName(processName);
        if (procs.Length == 0)
        {
#if DEBUG
            Console.WriteLine($"[Debug] プロセスが見つかりません: {processName}");
#endif
            return null;
        }

        IntPtr hWnd = procs[0].MainWindowHandle;
        if (hWnd == IntPtr.Zero)
        {
#if DEBUG
            Console.WriteLine("[Debug] MainWindowHandle が 0 です（最小化/非表示かも）");
#endif
            return null;
        }

        if (!TryGetWindowRectPx(hWnd, out var wr))
        {
#if DEBUG
            Console.WriteLine("[Debug] ウィンドウ矩形の取得に失敗");
#endif
            return null;
        }

        int width = Math.Max(1, wr.Right - wr.Left);
        int height = Math.Max(1, wr.Bottom - wr.Top);

#if DEBUG
        Console.WriteLine($"[Debug] WindowRect(px): x={wr.Left}, y={wr.Top}, w={width}, h={height}");
#endif

        var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            IntPtr hdcDest = g.GetHdc();
            bool ok = false;

            try
            {
                // 優先: PrintWindow（フルコンテンツ）
                ok = PrintWindow(hWnd, hdcDest, PW_RENDERFULLCONTENT);
#if DEBUG
                Console.WriteLine($"[Debug] PrintWindow => {ok}");
#endif

                if (!ok)
                {
                    // フォールバック: BitBlt
                    IntPtr hdcSrc = GetWindowDC(hWnd);
                    if (hdcSrc != IntPtr.Zero)
                    {
                        ok = BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);
                        ReleaseDC(hWnd, hdcSrc);
#if DEBUG
                        Console.WriteLine($"[Debug] BitBlt => {ok}");
#endif
                    }
                }
            }
            finally
            {
                g.ReleaseHdc(hdcDest);
            }
        }

        return bmp;
    }
}
