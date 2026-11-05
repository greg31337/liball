namespace tftp;

internal class NetasciiDecodeResult
{
    internal bool Success = false;
    internal int BytesConsumed = 0;
    internal int BytesWritten = 0;
    internal bool HasPendingCarriageReturn = false;
    internal bool NeedsMoreOutput = false;
    internal string FailureMessage = "";
    internal bool HasFailureOffset = false;
    internal int FailureOffset = 0;
}
