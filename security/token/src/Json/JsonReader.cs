using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace token;

internal class JsonValidationState
{
    internal string Text;
    internal int Position;
    internal int MaximumDepth;
    internal TokenError? Error;

    internal JsonValidationState(string text, int maximumDepth)
    {
        Text = text;
        Position = 0;
        MaximumDepth = maximumDepth;
        Error = null;
    }
}

internal class JsonStringResult
{
    internal bool Success;
    internal string? Value;
    internal int Start;
}
