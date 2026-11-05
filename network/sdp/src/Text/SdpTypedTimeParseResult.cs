namespace sdp;

internal class SdpTypedTimeParseResult
{
    internal bool Success = false;
    internal bool Invalid = false;
    internal bool Overflow = false;
    internal SdpTypedTime Value = new SdpTypedTime();
    internal string DiagnosticCode = string.Empty;
}
