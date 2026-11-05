namespace qrcode;

internal class ImageRecoveryResult
{
    internal bool Success;
    internal QrFailureReason FailureReason = QrFailureReason.NotFound;
    internal MatrixRecoveryResult? Recovery;
    internal QrDetection? Detection;
    internal int AttemptCount;
    internal int SampleCount;
    internal int ThresholdCandidateIndex;
    internal double SamplingOffsetX;
    internal double SamplingOffsetY;
    internal int AlignmentCandidateIndex = -1;
    internal bool UsedRefinement;
}
