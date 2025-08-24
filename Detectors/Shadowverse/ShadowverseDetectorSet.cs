using System.Collections.Generic;
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

        public ShadowverseDetectorSet(string? templateRoot = null)
        {
            _tplRoot = templateRoot ?? Path.Combine(AppContext.BaseDirectory, "Templates");
            // IDマッパーをCoreに登録
            MatchContracts.SetClassIdMapper(ShadowverseClassIdMapper.Map);
            MatchContracts.SetFormatIdMapper(ShadowverseFormatIdMapper.Map);
        }

        public List<IStateDetector> CreateDetectors()
        {
            var tplRoot = _tplRoot;
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
            new BattleDetector(
                new[]{
                Path.Combine(tplRoot, @"battleClassOwn__Forest__elem=MyClass.jpg"),
                Path.Combine(tplRoot, @"battleClassOwn__Sword__elem=MyClass.jpg"),
                Path.Combine(tplRoot, @"battleClassOwn__Rune__elem=MyClass.jpg"),
                Path.Combine(tplRoot, @"battleClassOwn__Dragon__elem=MyClass.jpg"),
                Path.Combine(tplRoot, @"battleClassOwn__Haven__elem=MyClass.jpg"),
                Path.Combine(tplRoot, @"battleClassOwn__Abyss__elem=MyClass.jpg"),
                Path.Combine(tplRoot, @"battleClassOwn__Portal__elem=MyClass.jpg")
                //Path.Combine(tplRoot, @"battleClassOwnP__elem=.jpg"),
                },
                new[]{
                Path.Combine(tplRoot, @"battleClassEmy__Forest__elem=OppClass.jpg"),
                Path.Combine(tplRoot, @"battleClassEmy__Sword__elem=OppClass.jpg"),
                Path.Combine(tplRoot, @"battleClassEmy__Rune__elem=OppClass.jpg"),
                Path.Combine(tplRoot, @"battleClassEmy__Dragon__elem=OppClass.jpg"),
                Path.Combine(tplRoot, @"battleClassEmy__Haven__elem=OppClass.jpg"),
                Path.Combine(tplRoot, @"battleClassEmy__Abyss__elem=OppClass.jpg"),
                Path.Combine(tplRoot, @"battleClassEmy__Portal__elem=OppClass.jpg")
                //Path.Combine(tplRoot, @"battleClassEmyP__elem=.jpg"),
                }),
            new ResultDetector(new[]{
                Path.Combine(tplRoot, @"result__win__elem=ResultBanner.png"),
                Path.Combine(tplRoot, @"result__lose__elem=ResultBanner.png"),
                Path.Combine(tplRoot, @"resultP__elem=NextMatch.png")
                })/**/
        };
        }
    }
}