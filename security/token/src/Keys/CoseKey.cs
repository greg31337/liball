using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

internal class CoseIntegerValue
{
    internal bool Present;
    internal int Value;
    internal TokenError? Error;
}

internal class CoseBytesValue
{
    internal bool Present;
    internal byte[]? Bytes;
    internal TokenError? Error;
}

internal class CoseOperationsValue
{
    internal int[]? Operations;
    internal TokenError? Error;
}
