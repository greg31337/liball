using System;
namespace token;

internal class DerLengthResult
{
    internal bool Success;
    internal int Length;
    internal int NextPosition;
    internal TokenError? Error;
}

internal class DerIntegerResult
{
    internal bool Success;
    internal int ValuePosition;
    internal int ValueLength;
    internal int NextPosition;
    internal TokenError? Error;
}
