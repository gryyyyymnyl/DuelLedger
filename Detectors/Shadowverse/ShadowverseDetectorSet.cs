using System.Collections.Generic;
using System.IO;
using DuelLedger.Vision;
using DuelLedger.Core;
using DuelLedger.Contracts;

namespace DuelLedger.Detectors.Shadowverse
{
    public class ShadowverseDetectorSet : IGameStateDetectorSet
    {
        public string GameName => "Shadowverse";
        public string ProcessName => "ShadowverseWB";
        private readonly string _tplRoot;
        private readonly IDictionary<string, string> _keys;

        public ShadowverseDetectorSet(string templateRoot, IDictionary<string, string> keys)
        {
            _tplRoot = templateRoot;
            _keys = keys;
            // IDマッパーをCoreに登録
            MatchContracts.SetClassIdMapper(ShadowverseClassIdMapper.Map);
            MatchContracts.SetFormatIdMapper(ShadowverseFormatIdMapper.Map);
        }

        private string[] GetFiles(string key, string fallback)
        {
            var pattern = _keys.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;
            return Directory.GetFiles(_tplRoot, pattern);
        }

        public List<IStateDetector> CreateDetectors()
        {
            var format = GetFiles("Format", "format__*.png");
            var matchStart = GetFiles("MatchStart", "matchStart__*.png");
            var battleOwn = GetFiles("BattleOwn", "battleClassOwn__*.jpg");
            var battleEnemy = GetFiles("BattleEnemy", "battleClassEmy__*.jpg");
            var result = GetFiles("Result", "result__*.png");

            return new List<IStateDetector>
            {
                new FormatDetector(format),
                new MatchStartDetector(matchStart),
                new BattleDetector(battleOwn, battleEnemy),
                new ResultDetector(result),
            };
        }
    }
}
