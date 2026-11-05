using System;
using System.Collections.Generic;

namespace http;

internal class HttpExpectationValue
{
    internal string Name = "";
    internal bool HasValue;
    internal byte[] Value = Array.Empty<byte>();
    internal bool IsQuoted;
    internal List<HttpParameter> Parameters = new List<HttpParameter>();
}
