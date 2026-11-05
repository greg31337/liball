using System;
using System.Security.Cryptography;

namespace token;

internal class EcCurveInfo
{
    internal string? Name;
    internal int CoseLabel;
    internal TokenError? Error;

    internal EcCurveInfo()
    {
        Name = null;
        CoseLabel = 0;
        Error = null;
    }
}

internal class EcAlgorithmRequirement
{
    internal int CurveLabel;
    internal TokenError? Error;

    internal EcAlgorithmRequirement()
    {
        CurveLabel = 0;
        Error = null;
    }
}
