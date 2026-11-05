using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace token;

internal class JoseCompressionOperationResult
{
    internal bool Success;
    internal bool LimitExceeded;
    internal byte[]? Bytes;

    internal JoseCompressionOperationResult()
    {
        Success = false;
        LimitExceeded = false;
        Bytes = null;
    }
}
