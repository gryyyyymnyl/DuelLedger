namespace DuelLedger.Detectors.Shadowverse;

using System;
using System.Collections.Generic;
using OpenCvSharp;
using Padding = DuelLedger.Vision.UiPadding;

public enum VsElem
{
    MatchFormat,   // 試合形式
    FirstSecond,   // 先行/後攻
    MyClass,       // 自分クラス
    OppClass,      // 相手クラス
    ResultBanner,  // 勝敗
    MenuDock,       //画面下部メニュー
    NextMatch,      //バトル開始ボタン(試合継続)
    PlayerIcon,     //プレイヤーアイコン
    VS,             //VS表示
}

public static class VsUiMap
{
    // ★ここを「その矩形を採寸した画像の解像度」に置き換え
    public const int RefW = 2564;
    public const int RefH = 1494;

    // 基準ピクセル矩形（そのまま保存）
    private static readonly Dictionary<VsElem,(RelativeRegion rr, RelativeRegion.Anchor2D a, RelativeRegion.ScaleMode s)> map = new()
    {
        // 試合形式：左寄せ＋垂直は中央寄せ／縦基準の等倍スケール
        [VsElem.MatchFormat] = (
            RelativeRegion.FromPixels(left: 71, top: 273, width: 864, height: 481, refW: RefW, refH: RefH),
            RelativeRegion.Anchor2D.Left | RelativeRegion.Anchor2D.CenterY,
            RelativeRegion.ScaleMode.UniformByHeight
        ),

        // 先行/後攻：画面中央に現れる帯。中心固定／縦基準の等倍
        [VsElem.FirstSecond] = (
            RelativeRegion.FromPixels(left: 726, top: 890, width: 1126, height: 333, refW: RefW, refH: RefH),
            RelativeRegion.Anchor2D.CenterX | RelativeRegion.Anchor2D.CenterY,
            RelativeRegion.ScaleMode.Stretch
        ),

        // 自分クラス：左下寄せ／縦基準の等倍
        [VsElem.MyClass] = (
            RelativeRegion.FromPixels(left: 0, top: 0, width: 2564/2, height: 1494, refW: RefW, refH: RefH),
            //RelativeRegion.FromPixels(left: 14, top: 919, width: 730, height: 460, refW: RefW, refH: RefH,
            //new Padding(-40, 0, 0, -60)),
            //RelativeRegion.FromPixels( left: 0, top: 930, width: 560, height: 560, refW: RefW, refH: RefH , 
            // new Padding(-40, 0, -60, 0)),
            RelativeRegion.Anchor2D.Right | RelativeRegion.Anchor2D.CenterY,
            RelativeRegion.ScaleMode.UniformByHeight
        ),

        // 相手クラス：右下寄せ／縦基準の等倍
        [VsElem.OppClass] = (
            RelativeRegion.FromPixels(left: 2564/2, top: 0, width: 2564/2, height: 1494, refW: RefW, refH: RefH),
            //RelativeRegion.FromPixels(left: 1832, top: 934, width: 730, height: 460, refW: RefW, refH: RefH,
            //new Padding(0, 0, -40, -60)),
            //RelativeRegion.FromPixels(left: 2049, top: 930, width: 560, height: 560, refW: RefW, refH: RefH , 
            // new Padding(0, 0, -40, -60)),
            RelativeRegion.Anchor2D.Right | RelativeRegion.Anchor2D.CenterY,
            RelativeRegion.ScaleMode.Stretch
        ),

        // 勝敗：上辺寄せの中央寄せ（Xは中央、Yは上固定）／縦基準の等倍
        [VsElem.ResultBanner] = (
            RelativeRegion.FromPixels(left: 867, top: 55, width: 950, height: 303, refW: RefW, refH: RefH),
            RelativeRegion.Anchor2D.CenterX | RelativeRegion.Anchor2D.Top,
            RelativeRegion.ScaleMode.Stretch
        ),

        // 画面下部メニュー：下辺寄せの中央寄せ）／縦基準の等倍
        [VsElem.MenuDock] = (
            RelativeRegion.FromPixels(left: 428, top: 1220, width: 1738, height: 269, refW: RefW, refH: RefH),
            RelativeRegion.Anchor2D.CenterX | RelativeRegion.Anchor2D.Bottom,
            RelativeRegion.ScaleMode.Stretch
        ),

        // バトル開始ボタン(試合継続)：下辺寄せの右寄せ）／縦基準の等倍
        [VsElem.NextMatch] = (
            RelativeRegion.FromPixels(left: 1335, top: 837, width: 1112, height: 581, refW: RefW, refH: RefH),
            RelativeRegion.Anchor2D.Right | RelativeRegion.Anchor2D.Bottom,
            RelativeRegion.ScaleMode.Stretch
        ),

        // プレイヤーアイコン：下辺寄せの右寄せ）／縦基準の等倍
        [VsElem.PlayerIcon] = (
            RelativeRegion.FromPixels( left: 0, top: 1100, width: 2564, height: 310, refW: RefW, refH: RefH ),
            RelativeRegion.Anchor2D.CenterX | RelativeRegion.Anchor2D.Bottom,
            RelativeRegion.ScaleMode.Stretch
        ),

        // VS表示／縦基準の等倍
        [VsElem.VS] = (
            RelativeRegion.FromPixels( left: 730, top: 620, width: 1100, height: 600, refW: RefW, refH: RefH ),
            RelativeRegion.Anchor2D.CenterX | RelativeRegion.Anchor2D.CenterY,
            RelativeRegion.ScaleMode.Stretch
        ),

    };

    public static Rect GetRect(VsElem e, int screenW, int screenH)
    {
        var (rr, a, s) = map[e];
        return rr.ComputeRect(screenW, screenH, a, s);
    }

    public static Point GetCenter(VsElem e, int screenW, int screenH)
    {
        var r = GetRect(e, screenW, screenH);
        return new Point(r.Left + r.Width/2, r.Top + r.Height/2);
    }
}
