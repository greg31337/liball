using System.Threading;

namespace ftp;

internal class FtpDeadline
{
    internal long StartMilliseconds;
    internal int TimeoutMilliseconds;
    internal bool Infinite;

    internal FtpDeadline(long startMilliseconds, int timeoutMilliseconds)
    {
        if (timeoutMilliseconds < Timeout.Infinite)
        {
            throw new FtpConfigurationException("A timeout must be non-negative or Timeout.Infinite.", null);
        }

        StartMilliseconds = startMilliseconds;
        TimeoutMilliseconds = timeoutMilliseconds;
        Infinite = timeoutMilliseconds == Timeout.Infinite;
    }

    internal int GetRemainingMilliseconds(long currentMilliseconds)
    {
        if (Infinite)
        {
            return Timeout.Infinite;
        }

        if (currentMilliseconds <= StartMilliseconds)
        {
            return TimeoutMilliseconds;
        }

        long elapsedMilliseconds = currentMilliseconds - StartMilliseconds;
        if (elapsedMilliseconds >= TimeoutMilliseconds)
        {
            return 0;
        }

        return TimeoutMilliseconds - (int)elapsedMilliseconds;
    }

    internal bool HasExpired(long currentMilliseconds)
    {
        return GetRemainingMilliseconds(currentMilliseconds) == 0;
    }
}
