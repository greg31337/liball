using System;

namespace http;

internal class OctetOperationResult
{
    internal HttpOperationStatus Status;
    internal int Consumed;
    internal int Written;
    internal int RequiredSize;
    internal byte Value;
    internal byte[] Bytes = Array.Empty<byte>();
    internal bool IsMalformed;
    internal bool IsLimitExceeded;
}
