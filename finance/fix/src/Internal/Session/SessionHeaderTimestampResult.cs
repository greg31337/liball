using System;

namespace fix;

internal class SessionHeaderTimestampResult
{
    internal bool Success;
    internal DateTime ValueUtc;
    internal TimestampParseResult Parsed = new TimestampParseResult();
}
