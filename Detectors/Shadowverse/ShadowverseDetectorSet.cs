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

        public ShadowverseDetectorSet(string templateRoot)
        {
            _tplRoot = templateRoot;
            // IDマッパーをCoreに登録
            MatchContracts.SetClassIdMapper(ShadowverseClassIdMapper.Map);
            MatchContracts.SetFormatIdMapper(ShadowverseFormatIdMapper.Map);
        }

        public List<IStateDetector> CreateDetectors()
        {
            return new List<IStateDetector>
        {
            new FormatDetector(new[]{
                Path.Combine(_tplRoot, @"format__2pick__elem=MatchFormat.png"),
                Path.Combine(_tplRoot, @"format__Rank__elem=MatchFormat.png"),
                Path.Combine(_tplRoot, @"formatP__elem=MenuDock.png")
                }),
            new MatchStartDetector(new[]{
                Path.Combine(_tplRoot, @"matchStart__1st__elem=FirstSecond.png"),
                Path.Combine(_tplRoot, @"matchStart__2nd__elem=FirstSecond.png"),
                Path.Combine(_tplRoot, @"matchStartP__elem=VS.png")
                }),
            new BattleDetector(
                new[]{
                Path.Combine(_tplRoot, @"battleClassOwn__Forest_Lovesign__elem=MyClass.png"),
                Path.Combine(_tplRoot, @"battleClassOwn__Forest_Titania__elem=MyClass.png"),
                Path.Combine(_tplRoot, @"battleClassOwn__Sword__elem=MyClass.jpg"),
                Path.Combine(_tplRoot, @"battleClassOwn__Rune__elem=MyClass.jpg"),
                Path.Combine(_tplRoot, @"battleClassOwn__Dragon__elem=MyClass.jpg"),
                Path.Combine(_tplRoot, @"battleClassOwn__Haven__elem=MyClass.jpg"),
                Path.Combine(_tplRoot, @"battleClassOwn__Abyss__elem=MyClass.jpg"),
                Path.Combine(_tplRoot, @"battleClassOwn__Portal__elem=MyClass.jpg")
                //Path.Combine(tplRoot, @"battleClassOwnP__elem=.jpg"),
                },
                new[]{
                Path.Combine(_tplRoot, @"battleClassEmy__Forest_Lovesign__elem=OppClass.png"),
                Path.Combine(_tplRoot, @"battleClassEmy__Sword__elem=OppClass.jpg"),
                Path.Combine(_tplRoot, @"battleClassEmy__Rune__elem=OppClass.jpg"),
                Path.Combine(_tplRoot, @"battleClassEmy__Dragon__elem=OppClass.jpg"),
                Path.Combine(_tplRoot, @"battleClassEmy__Haven__elem=OppClass.jpg"),
                Path.Combine(_tplRoot, @"battleClassEmy__Abyss__elem=OppClass.jpg"),
                Path.Combine(_tplRoot, @"battleClassEmy__Portal__elem=OppClass.jpg")
                //Path.Combine(tplRoot, @"battleClassEmyP__elem=.jpg"),
                }),
            new ResultDetector(new[]{
                Path.Combine(_tplRoot, @"result__win__elem=ResultBanner.png"),
                Path.Combine(_tplRoot, @"result__lose__elem=ResultBanner.png"),
                Path.Combine(_tplRoot, @"resultP__elem=NextMatch.png")
                })/**/
        };
        }
    }
}