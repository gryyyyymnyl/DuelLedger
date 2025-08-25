using System;
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

        public ShadowverseDetectorSet()
        {
            // IDマッパーをCoreに登録
            MatchContracts.SetClassIdMapper(ShadowverseClassIdMapper.Map);
            MatchContracts.SetFormatIdMapper(ShadowverseFormatIdMapper.Map);
        }

        public List<IStateDetector> CreateDetectors()
        {
            var tplRoot = Path.Combine(AppContext.BaseDirectory, "Templates");
            var modelPath = Path.Combine(AppContext.BaseDirectory, "models");
            var iconClassifier = IconClassifier.Load(modelPath);
            return new List<IStateDetector>
        {
            new FormatDetector(new[]{
                Path.Combine(tplRoot, @"format__2pick__elem=MatchFormat.png"),
                Path.Combine(tplRoot, @"format__Rank__elem=MatchFormat.png"),
                Path.Combine(tplRoot, @"formatP__elem=MenuDock.png")
                }),
            new MatchStartDetector(new[]{
                Path.Combine(tplRoot, @"matchStart__1st__elem=FirstSecond.png"),
                Path.Combine(tplRoot, @"matchStart__2nd__elem=FirstSecond.png"),
                Path.Combine(tplRoot, @"matchStartP__elem=VS.png")
                }),
            new BattleDetector(iconClassifier),
            new ResultDetector(new[]{
                Path.Combine(tplRoot, @"result__win__elem=ResultBanner.png"),
                Path.Combine(tplRoot, @"result__lose__elem=ResultBanner.png"),
                Path.Combine(tplRoot, @"resultP__elem=NextMatch.png")
                })/**/
        };
        }
    }
}