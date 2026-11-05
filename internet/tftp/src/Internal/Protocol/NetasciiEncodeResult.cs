namespace tftp;

internal class NetasciiEncodeResult
{
    internal int BytesConsumed = 0;
    internal int BytesWritten = 0;
    internal bool HasPendingOutput = false;
    internal bool NeedsMoreOutput = false;
}
