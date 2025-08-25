using System.Text.Json;
using OpenCvSharp;
using DuelLedger.Vision;
using DuelLedger.Core;

namespace DuelLedger.Detectors.Shadowverse;

public class BattleDetector : IStateDetector
{
    public GameState State => GameState.InBattle;
    private readonly IIconClassifier _classifier;
    private readonly double[] _emaOwn = new double[8];
    private readonly double[] _emaEnemy = new double[8];
    private const double Alpha = 0.6;
    public string Message { get; private set; } = "{}";
    public IReadOnlyList<string> OwnBestLabelsInGroups => Array.Empty<string>();
    public IReadOnlyList<string> EnemyBestLabelsInGroups => Array.Empty<string>();

    public BattleDetector(IIconClassifier classifier)
    {
        _classifier = classifier;
    }

    public bool IsMatch(Mat screen, out double score, out Point location)
    {
        score = 0; location = default;
        if (screen.Empty()) return false;
        var ownRect = VsUiMap.GetRect(VsElem.MyClass, screen.Width, screen.Height);
        var enemyRect = VsUiMap.GetRect(VsElem.OppClass, screen.Width, screen.Height);
        using var ownRoi = new Mat(screen, ownRect);
        using var enemyRoi = new Mat(screen, enemyRect);
        var ownPred = _classifier.Predict(ownRoi);
        var enemyPred = _classifier.Predict(enemyRoi);
        UpdateEma(_emaOwn, ownPred.probs);
        UpdateEma(_emaEnemy, enemyPred.probs);
        int ownId = ArgMax(_emaOwn);
        int enemyId = ArgMax(_emaEnemy);
        score = Math.Max(ownPred.maxP, enemyPred.maxP);
        var dict = new Dictionary<string,int>{{"own_class",ownId},{"enemy_class",enemyId}};
        Message = JsonSerializer.Serialize(dict);
        return ownId>0 || enemyId>0;
    }

    private static void UpdateEma(double[] ema, double[] probs)
    {
        for(int i=0;i<ema.Length && i<probs.Length;i++)
            ema[i] = Alpha*probs[i] + (1-Alpha)*ema[i];
    }
    private static int ArgMax(double[] arr)
    {
        int best=0; double bestVal=arr[0];
        for(int i=1;i<arr.Length;i++) if(arr[i]>bestVal){best=i; bestVal=arr[i];}
        return bestVal>=0.55?best:0;
    }
}
