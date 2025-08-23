namespace DuelLedger.Detectors.Shadowverse
{
    public static class ShadowverseClassIdMapper
    {
        public static int Map(string label) => label switch
        {
            "エルフ" => 1,
            "ロイヤル" => 2,
            "ウィッチ" => 3,
            "ドラゴン" => 4,
            "ナイトメア" => 5,
            "ビショップ" => 6,
            "ネメシス" => 7,
            _ => 0 // Unknown / 未知
        };
    }
    public static class ShadowverseFormatIdMapper
    {
        public static int Map(string label) => label switch
        {
            "Rotation"   => 1,
            "Unlimited"  => 2,
            "GrandPrix"  => 3,
            _ => 0 // Unknown
        };
    }
}
