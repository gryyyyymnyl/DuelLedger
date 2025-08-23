using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using DuelLedger.Vision;

namespace DuelLedger.Vision.Windows
{
    /// <summary>
    /// Windows-only screen source using Win32 GDI APIs.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class WinScreenSource : IScreenSource
    {
        private readonly string _processName;
        public WinScreenSource(string processName) => _processName = processName;

        public bool TryCapture(out Mat frame)
        {
            frame = null!;
            using var bmp = CaptureWindowByProcessName(_processName);
            if (bmp is null)
                return false;
            frame = BitmapConverter.ToMat(bmp);
            return true;
        }

        // --- Win32 capture implementation (ported from legacy ScreenCapturer) ---
        private static bool _dpiInit = false;

        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] private static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref RECT lpPoints, int cPoints);
        [DllImport("user32.dll")] private static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest,
                                                                       int nWidth, int nHeight,
                                                                       IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);
        [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
        [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);
        [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);
        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        private const int SRCCOPY = 0x00CC0020;
        private const uint PW_RENDERFULLCONTENT = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        private static void EnsureDpiAware()
        {
            if (_dpiInit) return;
            try
            {
                SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
                _dpiInit = true;
            }
            catch
            {
                // ignore
            }
        }

        private static bool TryGetWindowRectPx(IntPtr hWnd, out RECT rect)
        {
            if (DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf<RECT>()) == 0)
                return true;
            return GetWindowRect(hWnd, out rect);
        }

        private static Bitmap? CaptureWindowByProcessName(string processName)
        {
            EnsureDpiAware();

            var procs = Process.GetProcessesByName(processName);
            if (procs.Length == 0)
                return null;

            IntPtr hWnd = procs[0].MainWindowHandle;
            if (hWnd == IntPtr.Zero)
                return null;

            if (!TryGetWindowRectPx(hWnd, out var wr))
                return null;

            int width = Math.Max(1, wr.Right - wr.Left);
            int height = Math.Max(1, wr.Bottom - wr.Top);

            var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                IntPtr hdcDest = g.GetHdc();
                bool ok = false;
                try
                {
                    ok = PrintWindow(hWnd, hdcDest, PW_RENDERFULLCONTENT);
                    if (!ok)
                    {
                        IntPtr hdcSrc = GetWindowDC(hWnd);
                        if (hdcSrc != IntPtr.Zero)
                        {
                            ok = BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);
                            ReleaseDC(hWnd, hdcSrc);
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
}
