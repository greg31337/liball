using System;
using System.Collections.Generic;
using System.IO;

namespace email;
internal class EmailParserStreamInput
{
    byte[] headerBytes;
    byte[]? preservedBytes;
    long sourceByteCount;

    internal EmailParserStreamInput(byte[] headerBytes, byte[]? preservedBytes, long sourceByteCount)
    {
        this.headerBytes = headerBytes;
        this.preservedBytes = preservedBytes;
        this.sourceByteCount = sourceByteCount;
    }

    internal byte[] GetHeaderBytes()
    {
        return headerBytes;
    }

    internal byte[]? GetPreservedBytes()
    {
        return preservedBytes;
    }

    internal long GetSourceByteCount()
    {
        return sourceByteCount;
    }
}
