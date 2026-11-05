using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace email;
internal class EmailMboxEscapedMessage
{
    byte[] bytes;
    int escapedLineCount;

    internal EmailMboxEscapedMessage(byte[] bytes, int escapedLineCount)
    {
        this.bytes = bytes;
        this.escapedLineCount = escapedLineCount;
    }

    internal byte[] GetBytes() { return bytes; }
    internal int GetEscapedLineCount() { return escapedLineCount; }
}
