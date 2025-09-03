using System;
using System.Collections.Generic;
using System.IO;
using DuelLedger.Vision;
using DuelLedger.Core;
using DuelLedger.Core.Abstractions;

namespace DuelLedger.Detectors.Shadowverse
{
    public class ShadowverseDetectorSet : IGameStateDetectorSet
    {
        public string GameName => "Shadowverse";
        public string ProcessName => "ShadowverseWB";

        private readonly string _tplRoot;
        private readonly Dictionary<string, string> _keys;

        public ShadowverseDetectorSet(string templateRoot, Dictionary<string, string> keys)
        {
            _tplRoot = templateRoot;
            _keys = keys;
            MatchContracts.SetClassIdMapper(ShadowverseClassIdMapper.Map);
            MatchContracts.SetFormatIdMapper(ShadowverseFormatIdMapper.Map);
        }

        private string[] GetPaths(string key)
        {
            if (!_keys.TryGetValue(key, out var pattern))
                return Array.Empty<string>();
            return Directory.GetFiles(_tplRoot, pattern);
        }

        public List<IStateDetector> CreateDetectors()
        {
            return new List<IStateDetector>
            {
                new FormatDetector(GetPaths("Format")),
                new MatchStartDetector(GetPaths("MatchStart")),
                new BattleDetector(GetPaths("BattleOwn"), GetPaths("BattleEnemy")),
                new ResultDetector(GetPaths("Result"))
            };
        }
    }
}
