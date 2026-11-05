namespace sdp;

internal class SdpRecoveryResult
{
    internal bool Recovered = false;
    internal bool Retained = false;
    internal bool EndOfInput = false;
    internal bool LineLengthExceeded = false;
    internal bool LineCountExceeded = false;
    internal bool NextLineIsFieldBoundary = false;
    internal bool NextLineIsMediaBoundary = false;
    internal bool NextLineIsSessionBoundary = false;
    internal int NextOffset = 0;
    internal int NextCharacterOffset = 0;
    internal int NextByteOffset = -1;
    internal int NextLineNumber = 1;
    internal SdpRawLine? Line = null;
    internal string DiagnosticCode = string.Empty;
}
