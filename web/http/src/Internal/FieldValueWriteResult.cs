using System;

namespace http;

internal class FieldValueWriteResult
{
    internal HttpOperationStatus Status;
    internal int Written;
    internal int RequiredSize;
    internal int ErrorPosition;
    internal byte[] Bytes = Array.Empty<byte>();
    internal bool IsMalformed;
    internal bool IsLimitExceeded;
}
