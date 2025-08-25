using System.Text.Json;
using OpenCvSharp;

namespace DuelLedger.Vision;

public class IconClassifier : IIconClassifier
{
    private Dictionary<int, float[]> _centroids = new();
    private Dictionary<int, (double A,double B)> _platt = new();

    public static IconClassifier Load(string modelsPath)
    {
        var clf = new IconClassifier();
        var svmPath = Path.Combine(modelsPath, "class_icon_svm.json");
        if (File.Exists(svmPath))
        {
            var json = File.ReadAllText(svmPath);
            var dict = JsonSerializer.Deserialize<Dictionary<int,float[]>>(json);
            if (dict != null)
                clf._centroids = dict;
        }
        var plattPath = Path.Combine(modelsPath, "class_icon_platt.json");
        if (File.Exists(plattPath))
        {
            var json = File.ReadAllText(plattPath);
            var dict = JsonSerializer.Deserialize<Dictionary<int,(double A,double B)>>(json);
            if (dict != null)
                clf._platt = dict;
        }
        return clf;
    }

    public (int classId, double maxP, double[] probs) Predict(Mat roiGray)
    {
        var feat = HogFeaturizer.Extract(roiGray);
        var scores = new Dictionary<int,double>();
        foreach (var kv in _centroids)
        {
            var dist = Euclidean(feat, kv.Value);
            scores[kv.Key] = -dist; // higher better
        }
        if (scores.Count==0)
            return (0,0, Enumerable.Repeat(0.0,8).ToArray());
        // softmax
        var exp = scores.ToDictionary(kv=>kv.Key, kv=>Math.Exp(kv.Value));
        double sum = exp.Values.Sum();
        var probs = new double[8];
        foreach(var kv in exp)
            probs[kv.Key] = kv.Value/sum;
        int bestId = probs.Select((p,i)=> (p,i)).OrderByDescending(x=>x.p).First().i;
        double maxP = probs[bestId];
        if (maxP < 0.55) bestId = 0;
        return (bestId, maxP, probs);
    }

    private static double Euclidean(float[] a, float[] b)
    {
        double s=0;
        int n=Math.Min(a.Length,b.Length);
        for(int i=0;i<n;i++){double d=a[i]-b[i]; s+=d*d;}
        return Math.Sqrt(s);
    }
}
