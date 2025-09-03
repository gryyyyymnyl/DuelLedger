namespace DuelLedger.Games.Shadowverse
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
        public static int Map(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return 0;
            var s = label.Trim();
            var sNorm = s.Replace(" ", string.Empty)
                         .Replace("　", string.Empty)
                         .ToLowerInvariant();

            int id;
            if (s == "ランクマッチ" || sNorm.StartsWith("rank") || sNorm.StartsWith("rotation") || sNorm.StartsWith("unlimited"))
            {
                id = 1;
            }
            else if (string.Equals(s, "2Pick", StringComparison.OrdinalIgnoreCase) || sNorm.Contains("2pick"))
            {
                id = 2;
            }
            else if (string.Equals(s, "GrandPrix", StringComparison.OrdinalIgnoreCase) || s == "グランプリ")
            {
                id = 3;
            }
            else
            {
                id = 0;
            }

            Console.WriteLine($"[FormatIdMapper] '{label}' -> {id}");
            return id;
        }
    }
}
