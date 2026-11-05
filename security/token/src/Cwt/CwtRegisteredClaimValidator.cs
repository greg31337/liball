using System;
namespace token;

internal class CwtNumericDateResult
{
    internal bool Success;
    internal decimal Seconds;

    internal CwtNumericDateResult()
    {
        Success = false;
        Seconds = 0m;
    }
}
