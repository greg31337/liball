using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace token;

internal class X509CertificateKeyInfo
{
    internal string? KeyType;
    internal int CoseKeyType;
    internal string? Curve;
    internal int CoseCurve;
    internal TokenError? Error;

    internal X509CertificateKeyInfo()
    {
        KeyType = null;
        CoseKeyType = 0;
        Curve = null;
        CoseCurve = 0;
        Error = null;
    }
}
