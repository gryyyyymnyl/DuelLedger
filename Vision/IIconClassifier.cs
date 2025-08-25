using OpenCvSharp;

namespace DuelLedger.Vision;

public interface IIconClassifier
{
    (int classId, double maxP, double[] probs) Predict(Mat roiGray);
}
