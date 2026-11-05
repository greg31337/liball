using System.Collections.Generic;

namespace sdp;

internal class SdpMediaTokenizationResult
{
    internal bool Success = false;
    internal bool TokenLengthExceeded = false;
    internal int InvalidOffset = 0;
    internal List<SdpTokenReadResult> Tokens = new List<SdpTokenReadResult>();
}
