using System;
using System.Numerics;
using System.Security.Cryptography;

namespace token;

internal class JwkTextValue
{
    internal string? Text;
    internal TokenError? Error;
}

internal class JwkBytesValue
{
    internal byte[]? Bytes;
    internal TokenError? Error;
}

internal class JwkStringArrayValue
{
    internal string[] Values;
    internal TokenError? Error;

    internal JwkStringArrayValue()
    {
        Values = System.Array.Empty<string>();
        Error = null;
    }
}

internal class JwkRsaPrivateValues
{
    internal byte[]? D;
    internal byte[]? P;
    internal byte[]? Q;
    internal byte[]? DP;
    internal byte[]? DQ;
    internal byte[]? QI;
    internal TokenError? Error;
}

internal class JwkEcCurve
{
    internal string? Name;
    internal int Size;
    internal ECCurve NativeCurve;
    internal BigInteger Prime;
    internal BigInteger A;
    internal BigInteger B;
    internal BigInteger Order;
    internal BigInteger GeneratorX;
    internal BigInteger GeneratorY;
    internal TokenError? Error;
}

internal class JwkEcPoint
{
    internal BigInteger X;
    internal BigInteger Y;
    internal bool Infinity;
}

internal class JwkOkpCurve
{
    internal int Size;
    internal int CoseCurve;
    internal TokenError? Error;
}
