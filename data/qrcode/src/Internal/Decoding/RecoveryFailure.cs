namespace qrcode;

internal class RecoveryFailure
{
    // Keep the furthest verified decoding stage, independent of trial order.
    // Exhaustion is terminal and outranks evidence from an incomplete search.
    internal static QrFailureReason Choose(QrFailureReason current, QrFailureReason candidate)
    {
        return Rank(candidate) > Rank(current) ? candidate : current;
    }

    static int Rank(QrFailureReason reason)
    {
        if (reason == QrFailureReason.ResourceLimit) { return 6; }
        if (reason == QrFailureReason.ChecksumFailure) { return 5; }
        if (reason == QrFailureReason.InvalidData) { return 4; }
        if (reason == QrFailureReason.InvalidVersion) { return 3; }
        if (reason == QrFailureReason.InvalidFormat) { return 2; }
        if (reason == QrFailureReason.InvalidGeometry) { return 1; }
        return 0;
    }
}
