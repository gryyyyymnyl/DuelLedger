using System;
using OpenCvSharp;
using Padding = DuelLedger.Vision.UiPadding;

public readonly struct RelativeRegion
{
    // 基準解像度に対する相対矩形（0..1）
    public double X { get; }   // left / refW
    public double Y { get; }   // top  / refH
    public double W { get; }   // width / refW
    public double H { get; }   // height / refH

    public int RefW { get; }   // 基準解像度の幅
    public int RefH { get; }   // 基準解像度の高
    public Padding DefaultMargin { get; }

    [Flags]
    public enum Anchor2D
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Top = 1 << 2,
        Bottom = 1 << 3,
        CenterX = 1 << 4,
        CenterY = 1 << 5,
    }

    public enum ScaleMode
    {
        UniformByHeight, // 等倍（縦基準）。横基準にしたい場合は下の UniformByWidth
        UniformByWidth,
        Stretch,         // 両辺アンカー時のみストレッチ（Unityのアンカーに近い）
        None,            // サイズ固定（ピクセルで指定したい時用）
    }

    public RelativeRegion(double x, double y, double w, double h, int refW, int refH, Padding? defaultMargin = null)
    { X = x; Y = y; W = w; H = h; RefW = refW; RefH = refH; DefaultMargin = defaultMargin ?? Padding.Empty; }

    public static RelativeRegion FromPixels(int left, int top, int width, int height, int refW, int refH, Padding? defaultMargin = null)
        => new((double)left / refW, (double)top / refH, (double)width / refW, (double)height / refH, refW, refH, defaultMargin);

    /// <summary>
    /// 任意のターゲットサイズにおけるピクセル矩形（テンプレート側の相対領域→絶対矩形に使用）
    /// </summary>
    public Rect ToRect(Size targetSize)
    {
        return ComputeRect(
            screenW: targetSize.Width,
            screenH: targetSize.Height,
            anchor: Anchor2D.Left | Anchor2D.Top,
            scaleMode: ScaleMode.Stretch
        );
    }

    /// <summary>
    /// 任意スクリーンサイズにおけるピクセル矩形を返す
    /// </summary>
    public Rect ComputeRect(int screenW, int screenH, Anchor2D anchor, ScaleMode scaleMode)
    {
        // 基準座標（px）
        double L0 = X * RefW;
        double T0 = Y * RefH;
        double W0 = W * RefW;
        double H0 = H * RefH;

        // 基準から見た余白（px）
        double ml = L0;
        double mt = T0;
        double mr = RefW - (L0 + W0);
        double mb = RefH - (T0 + H0);

        // スケール係数
        double sx = (double)screenW / RefW;
        double sy = (double)screenH / RefH;

        // サイズ計算
        double w, h;

        if (scaleMode == ScaleMode.UniformByHeight)
        {
            double s = sy;
            w = W0 * s; h = H0 * s;
        }
        else if (scaleMode == ScaleMode.UniformByWidth)
        {
            double s = sx;
            w = W0 * s; h = H0 * s;
        }
        else if (scaleMode == ScaleMode.None)
        {
            // ピクセル固定（基準ピクセルをそのまま使う）
            w = W0; h = H0;
        }
        else // Stretch
        {
            // 両辺アンカーのみをストレッチ対象にする（片側 or 中央寄せは等倍）
            bool stretchX = anchor.HasFlag(Anchor2D.Left) && anchor.HasFlag(Anchor2D.Right);
            bool stretchY = anchor.HasFlag(Anchor2D.Top) && anchor.HasFlag(Anchor2D.Bottom);

            if (stretchX) w = screenW - (ml * sx) - (mr * sx);
            else w = W0 * ((anchor.HasFlag(Anchor2D.CenterX)) ? sx : sx); // 実質 sx

            if (stretchY) h = screenH - (mt * sy) - (mb * sy);
            else h = H0 * ((anchor.HasFlag(Anchor2D.CenterY)) ? sy : sy); // 実質 sy
        }

        // 位置計算（X）
        double x;
        if (anchor.HasFlag(Anchor2D.Left) && anchor.HasFlag(Anchor2D.Right))
        {
            // 両辺アンカー：左は左余白スケール
            x = ml * sx;
        }
        else if (anchor.HasFlag(Anchor2D.Left))
        {
            x = ml * sx;
        }
        else if (anchor.HasFlag(Anchor2D.Right))
        {
            x = screenW - (mr * sx) - w;
        }
        else // CenterX
        {
            double cx0 = (L0 + W0 / 2.0);
            double dx = cx0 - RefW / 2.0;
            double cx = screenW / 2.0 + dx * sx;
            x = cx - w / 2.0;
        }

        // 位置計算（Y）
        double y;
        if (anchor.HasFlag(Anchor2D.Top) && anchor.HasFlag(Anchor2D.Bottom))
        {
            y = mt * sy;
        }
        else if (anchor.HasFlag(Anchor2D.Top))
        {
            y = mt * sy;
        }
        else if (anchor.HasFlag(Anchor2D.Bottom))
        {
            y = screenH - (mb * sy) - h;
        }
        else // CenterY
        {
            double cy0 = (T0 + H0 / 2.0);
            double dy = cy0 - RefH / 2.0;
            double cy = screenH / 2.0 + dy * sy;
            y = cy - h / 2.0;
        }

        /*var r = new Rect(
            (int)Math.Round(x),
            (int)Math.Round(y),
            Math.Max(0, (int)Math.Round(w)),
            Math.Max(0, (int)Math.Round(h))
        );
        if (DefaultMargin.Left != 0 || DefaultMargin.Top != 0 ||
        DefaultMargin.Right != 0 || DefaultMargin.Bottom != 0)
        {
            r.X -= DefaultMargin.Left;
            r.Y -= DefaultMargin.Top;
            r.Width  += DefaultMargin.Horizontal;
            r.Height += DefaultMargin.Vertical;
        }
        return r;*/
        return new Rect(
            (int)Math.Round(x),
            (int)Math.Round(y),
            Math.Max(0, (int)Math.Round(w)),
            Math.Max(0, (int)Math.Round(h))
        );
    }
}
