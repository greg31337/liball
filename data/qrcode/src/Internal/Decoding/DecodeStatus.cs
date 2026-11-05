namespace qrcode;

internal class DecodeStatus
{
    internal QrFailureReason FailureReason;
    internal global::System.Exception? Error;

    internal bool Fail(QrFailureReason reason, string message)
    {
        FailureReason = reason;
        Error = new QrDecodeException(message, reason, -1, 0);
        return false;
    }

    internal bool CheckAllocation(long requested, int maximum)
    {
        if (requested <= maximum) { return true; }
        return FailLimit("MaximumAllocationBytes", requested > int.MaxValue ? int.MaxValue : (int)requested, maximum);
    }

    internal bool FailLimit(string name, int requested, int maximum)
    {
        FailureReason = QrFailureReason.ResourceLimit;
        Error = new QrResourceLimitException("The decoded payload exceeds the configured limit.", name, requested, maximum);
        return false;
    }
}
