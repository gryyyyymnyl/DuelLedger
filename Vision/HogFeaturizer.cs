using OpenCvSharp;

namespace DuelLedger.Vision;

public static class HogFeaturizer
{
    private static readonly HOGDescriptor hog = new(
        new Size(128,128),
        new Size(16,16),
        new Size(8,8),
        new Size(8,8),
        9);

    public static float[] Extract(Mat srcGray)
    {
        using var resized = new Mat();
        Cv2.Resize(srcGray, resized, new Size(128,128));
        Cv2.EqualizeHist(resized, resized);
        Cv2.GaussianBlur(resized, resized, new Size(3,3), 0);
        return hog.Compute(resized).ToArray();
    }
}
