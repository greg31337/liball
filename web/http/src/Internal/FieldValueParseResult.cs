using System;
using System.Collections.Generic;

namespace http;

internal class FieldValueParseResult
{
    internal HttpOperationStatus Status;
    internal int Consumed;
    internal int NextPosition;
    internal int ErrorPosition;
    internal int EmptyElements;
    internal byte[] Value = Array.Empty<byte>();
    internal List<byte[]> Values = new List<byte[]>();
    internal List<HttpParameter> Parameters = new List<HttpParameter>();
    internal bool IsMalformed;
    internal bool IsLimitExceeded;
}
