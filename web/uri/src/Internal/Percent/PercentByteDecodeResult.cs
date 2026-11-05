namespace uri;

internal class PercentByteDecodeResult
{
    internal bool Success;
    internal byte[] Bytes;
    internal UriDiagnostic? Diagnostic;

    internal PercentByteDecodeResult(bool success, byte[] bytes, UriDiagnostic? diagnostic)
    {
        Success = success;
        Bytes = bytes;
        Diagnostic = diagnostic;
    }
}
