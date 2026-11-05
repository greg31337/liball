namespace qrcode;

internal class RecoveryBudget
{
    internal int MaximumAttempts;
    internal int AttemptCount;

    internal RecoveryBudget(int maximumAttempts)
    {
        if (maximumAttempts <= 0) { throw new QrInputException("The recovery attempt limit must be positive.", "MaximumRecoveryAttempts"); }
        MaximumAttempts = maximumAttempts;
    }

    internal bool IsExhausted()
    {
        return AttemptCount >= MaximumAttempts;
    }

    // One object follows an image through every sampled matrix, BCH choice,
    // and uncertain-value trial. Check before incrementing, including int.MaxValue.
    internal bool TryBeginAttempt()
    {
        if (IsExhausted()) { return false; }
        AttemptCount++;
        return true;
    }

    internal bool TryBeginMatrixAttempt(MatrixRecoveryResult result)
    {
        if (!TryBeginAttempt())
        {
            result.FailureReason = QrFailureReason.ResourceLimit;
            return false;
        }
        result.AttemptCount++;
        return true;
    }
}
