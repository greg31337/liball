using System;

namespace uuid;

internal interface ISnowflakeClock
{
    long UnixMilliseconds();
    void WaitOneMillisecond();
}

internal class SystemSnowflakeClock : ISnowflakeClock
{
    public long UnixMilliseconds()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public void WaitOneMillisecond()
    {
        System.Threading.Thread.Sleep(1);
    }
}
