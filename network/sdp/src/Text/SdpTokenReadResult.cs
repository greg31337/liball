namespace sdp;

internal class SdpTokenReadResult
{
    internal bool Success = false;
    internal bool TokenLengthExceeded = false;
    internal int StartOffset = 0;
    internal int Length = 0;
    internal int NextOffset = 0;
}
