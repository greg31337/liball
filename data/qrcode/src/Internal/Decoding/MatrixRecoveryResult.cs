namespace qrcode;

internal class MatrixRecoveryResult
{
    internal bool Success;
    internal QrFailureReason FailureReason = QrFailureReason.NotFound;
    internal MatrixCodewordResult? Codewords;
    internal QrMatrix? Matrix;
    // Transform applied to the supplied matrix to obtain canonical geometry.
    internal int QuarterTurnsClockwise;
    internal bool IsMirrored;
    internal bool IsInverted;
    internal bool UsedAlternateInformation;
    internal int AttemptCount;
    internal int[] FlippedModuleIndices = new int[0];
    internal bool HasAmbiguousRepair;
}
