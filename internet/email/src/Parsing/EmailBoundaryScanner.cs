using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailBoundaryValue
{
    byte[] bytes;
    bool recovered;

    internal EmailBoundaryValue(byte[] bytes, bool recovered)
    {
        this.bytes = bytes;
        this.recovered = recovered;
    }

    internal byte[] GetBytes()
    {
        return bytes;
    }

    internal bool WasRecovered()
    {
        return recovered;
    }
}
internal class EmailBoundaryMatch
{
    bool match;
    bool closing;
    int transportPaddingByteCount;
    bool recovered;

    EmailBoundaryMatch(bool match, bool closing, int transportPaddingByteCount, bool recovered)
    {
        this.match = match;
        this.closing = closing;
        this.transportPaddingByteCount = transportPaddingByteCount;
        this.recovered = recovered;
    }

    internal static EmailBoundaryMatch CreateNoMatch()
    {
        return new EmailBoundaryMatch(false, false, 0, false);
    }

    internal static EmailBoundaryMatch CreateMatch(bool closing, int transportPaddingByteCount, bool recovered)
    {
        return new EmailBoundaryMatch(true, closing, transportPaddingByteCount, recovered);
    }

    internal bool IsMatch()
    {
        return match;
    }

    internal bool IsClosing()
    {
        return closing;
    }

    internal int GetTransportPaddingByteCount()
    {
        return transportPaddingByteCount;
    }

    internal bool WasRecovered()
    {
        return recovered;
    }
}
