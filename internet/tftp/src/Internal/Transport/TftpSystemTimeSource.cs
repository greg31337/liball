using System;

namespace tftp;

internal class TftpSystemTimeSource : ITftpTimeSource
{
    public long GetTimestampMilliseconds()
    {
        return Environment.TickCount64;
    }
}
