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

        public ShadowverseDetectorSet()
        {
            // IDマッパーをCoreに登録
            MatchContracts.SetClassIdMapper(ShadowverseClassIdMapper.Map);
            MatchContracts.SetFormatIdMapper(ShadowverseFormatIdMapper.Map);
        }

        public List<IStateDetector> CreateDetectors()
        {
            return new List<IStateDetector>
        {
            new FormatDetector(new[]{
                @"Games/Shadowverse/Templates/format__2pick__elem=MatchFormat.png",
                @"Games/Shadowverse/Templates/format__Rank__elem=MatchFormat.png",
                @"Games/Shadowverse/Templates/formatP__elem=MenuDock.png"
                }),
            new MatchStartDetector(new[]{
                @"Games/Shadowverse/Templates/matchStart__1st__elem=FirstSecond.png",
                @"Games/Shadowverse/Templates/matchStart__2nd__elem=FirstSecond.png",
                @"Games/Shadowverse/Templates/matchStartP__elem=VS.png"
                }),
            new BattleDetector(
                new[]{
                @"Games/Shadowverse/Templates/battleClassOwn__Forest__elem=MyClass.jpg",
                @"Games/Shadowverse/Templates/battleClassOwn__Sword__elem=MyClass.jpg",
                @"Games/Shadowverse/Templates/battleClassOwn__Rune__elem=MyClass.jpg",
                @"Games/Shadowverse/Templates/battleClassOwn__Dragon__elem=MyClass.jpg",
                @"Games/Shadowverse/Templates/battleClassOwn__Haven__elem=MyClass.jpg",
                @"Games/Shadowverse/Templates/battleClassOwn__Abyss__elem=MyClass.jpg",
                @"Games/Shadowverse/Templates/battleClassOwn__Portal__elem=MyClass.jpg"
                //@"Games/Shadowverse/Templates/battleClassOwnP__elem=.jpg",
                },
                new[]{
                @"Games/Shadowverse/Templates/battleClassEmy__Forest__elem=OppClass.jpg",
                @"Games/Shadowverse/Templates/battleClassEmy__Sword__elem=OppClass.jpg",
                @"Games/Shadowverse/Templates/battleClassEmy__Rune__elem=OppClass.jpg",
                @"Games/Shadowverse/Templates/battleClassEmy__Dragon__elem=OppClass.jpg",
                @"Games/Shadowverse/Templates/battleClassEmy__Haven__elem=OppClass.jpg",
                @"Games/Shadowverse/Templates/battleClassEmy__Abyss__elem=OppClass.jpg",
                @"Games/Shadowverse/Templates/battleClassEmy__Portal__elem=OppClass.jpg"
                //@"Games/Shadowverse/Templates/battleClassEmyP__elem=.jpg",
                }),
            new ResultDetector(new[]{
                @"Games/Shadowverse/Templates/result__win__elem=ResultBanner.png",
                @"Games/Shadowverse/Templates/result__lose__elem=ResultBanner.png",
                @"Games/Shadowverse/Templates/resultP__elem=NextMatch.png"
                })/**/
        };
        }
    }
}