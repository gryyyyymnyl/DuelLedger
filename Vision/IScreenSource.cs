using OpenCvSharp;

namespace DuelLedger.Vision
{
    /// <summary>
    /// Provides screen frames as OpenCvSharp Mat.
    /// </summary>
    public interface IScreenSource
    {
        /// <summary>Attempts to capture a screen frame.</summary>
        /// <param name="frame">Captured frame on success.</param>
        /// <returns>true if capture succeeded.</returns>
        bool TryCapture(out Mat frame);
    }
}
