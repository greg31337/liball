using System;

namespace ftp;

internal static class FtpTimeout
{
    internal static FtpDeadline StartDeadline(int timeoutMilliseconds)
    {
        return new FtpDeadline(Environment.TickCount64, timeoutMilliseconds);
    }

    internal static int GetRemainingWaitMilliseconds(FtpDeadline deadline)
    {
        return deadline.GetRemainingMilliseconds(Environment.TickCount64);
    }

    internal static bool HasDeadlineExpired(FtpDeadline deadline)
    {
        return deadline.HasExpired(Environment.TickCount64);
    }

    internal static void ThrowIfDeadlineExpired(FtpDeadline deadline, string operation)
    {
        if (HasDeadlineExpired(deadline))
        {
            throw new TimeoutException($"The FTP {operation} timed out.");
        }
    }
}
