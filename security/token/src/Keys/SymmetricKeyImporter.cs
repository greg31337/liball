using System;
using System.Security.Cryptography;

namespace token;

internal class SymmetricKeyRequirement
{
    internal int SizeInBytes;
    internal bool ExactSize;
    internal TokenError? Error;

    internal SymmetricKeyRequirement()
    {
        SizeInBytes = 0;
        ExactSize = false;
        Error = null;
    }
}
