namespace tftp;

internal class ZeroTerminatedAsciiDecodeResult
{
    internal bool Success = false;
    internal string Value = "";
    internal int NextOffset = 0;
    internal string FailureMessage = "";
    internal bool HasFailureOffset = false;
    internal int FailureOffset = 0;
}
