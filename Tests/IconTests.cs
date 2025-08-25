using DuelLedger.Vision;
using DuelLedger.Net;
using Svg.Skia;
using SkiaSharp;
using OpenCvSharp;
using System.Text.Json;

namespace DuelLedger.Tests;

public class IconTests
{
    [Fact]
    public void HogFeaturizer_ReturnsFixedLength()
    {
        using var mat = new Mat(128,128,MatType.CV_8UC1, Scalar.All(0));
        var feat = HogFeaturizer.Extract(mat);
        Assert.Equal(8100, feat.Length);
    }

    private class MockSvgProvider : ISvgProvider
    {
        public Task<string?> GetSvgAsync(int classId)
        {
            string svg = classId switch
            {
                1 => "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><rect x='0' y='0' width='100' height='100' fill='black'/></svg>",
                2 => "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><circle cx='50' cy='50' r='40' fill='black'/></svg>",
                3 => "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><polygon points='50,10 90,90 10,90' fill='black'/></svg>",
                4 => "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><rect x='0' y='0' width='100' height='50' fill='black'/></svg>",
                5 => "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><rect x='0' y='0' width='50' height='100' fill='black'/></svg>",
                6 => "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><ellipse cx='50' cy='50' rx='40' ry='20' fill='black'/></svg>",
                7 => "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><line x1='0' y1='0' x2='100' y2='100' stroke='black' stroke-width='10'/></svg>",
                _ => null
            };
            return Task.FromResult<string?>(svg);
        }
    }

    private static Mat SvgToMat(string svg)
    {
        var svgDom = new SKSvg();
        svgDom.FromSvg(svg);
        var bmp = new SKBitmap(128,128, true);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);
        var scale = Math.Min(128 / svgDom.Picture.CullRect.Width, 128 / svgDom.Picture.CullRect.Height);
        canvas.Scale(scale, scale);
        canvas.DrawPicture(svgDom.Picture);
        var mat = Mat.FromPixelData(128,128,MatType.CV_8UC4,bmp.GetPixels());
        Cv2.CvtColor(mat, mat, ColorConversionCodes.BGRA2GRAY);
        return mat;
    }

    [Fact]
    public async Task IconTrainer_SyntheticTrain_E2E()
    {
        var provider = new MockSvgProvider();
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        await Trainer.TrainAsync(provider, dir, 2);
        var classifier = IconClassifier.Load(dir);
        int correct = 0;
        for(int id=1; id<=7; id++)
        {
            var svg = await provider.GetSvgAsync(id);
            if (svg==null) continue;
            using var mat = SvgToMat(svg);
            var pred = classifier.Predict(mat);
            if (pred.classId==id) correct++;
        }
        Assert.True(correct/7.0 >= 0.98);
    }

    [Fact]
    public void IconClassifier_Predict_Thresholding()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        // simple model with one centroid
        var centroids = new Dictionary<int,float[]>{{1,new float[8100]}};
        File.WriteAllText(Path.Combine(dir,"class_icon_svm.json"),JsonSerializer.Serialize(centroids));
        File.WriteAllText(Path.Combine(dir,"class_icon_platt.json"),"{}");
        var clf = IconClassifier.Load(dir);
        using var roi = new Mat(128,128,MatType.CV_8UC1,Scalar.All(255));
        var res = clf.Predict(roi);
        Assert.Equal(0,res.classId);
    }

    private class FakeIconClassifier : IIconClassifier
    {
        public (int classId, double maxP, double[] probs) Predict(Mat roiGray)
        {
            var mean = Cv2.Mean(roiGray)[0];
            if (mean < 128)
            {
                var probs = new double[8];
                probs[1] = 0.9;
                return (1,0.9,probs);
            }
            else
            {
                return (0,0.1,new double[8]);
            }
        }
    }

    [Fact]
    public void BattleDetector_UsesClassifier_Path()
    {
        var clf = new FakeIconClassifier();
        var detector = new DuelLedger.Detectors.Shadowverse.BattleDetector(clf);
        using var screen = new Mat(DuelLedger.Detectors.Shadowverse.VsUiMap.RefH, DuelLedger.Detectors.Shadowverse.VsUiMap.RefW, MatType.CV_8UC1, Scalar.All(255));
        var ownRect = DuelLedger.Detectors.Shadowverse.VsUiMap.GetRect(DuelLedger.Detectors.Shadowverse.VsElem.MyClass, screen.Width, screen.Height);
        screen.Rectangle(ownRect, Scalar.All(0), -1);
        var ok = detector.IsMatch(screen, out var score, out var loc);
        Assert.True(ok);
        var msg = JsonSerializer.Deserialize<Dictionary<string,int>>(detector.Message)!;
        Assert.Equal(1, msg["own_class"]);
        Assert.Equal(0, msg["enemy_class"]);
    }
}
