using System;

namespace uuid;

internal class KsuidGenerator
{
    internal static KsuidValue New()
    {
        return NewAt(DateTimeOffset.UtcNow);
    }

    internal static KsuidValue NewAt(DateTimeOffset time)
    {
        long unixSeconds = UnixTimeTools.ToUnixSeconds(time);
        return NewAtUnixSeconds(unixSeconds);
    }

    internal static KsuidValue NewAtUnixSeconds(long unixSeconds)
    {
        return NewAtUnixSeconds(unixSeconds, RandomTools.SecureSource);
    }

    internal static KsuidValue NewAtUnixSeconds(long unixSeconds, IRandomSource source)
    {
        long adjustedSeconds = KsuidTimestamp.ToAdjustedSeconds(unixSeconds);
        byte[] bytes = new byte[20];
        BigEndianTools.WriteUInt32(bytes, 0, (uint)adjustedSeconds);
        byte[] payload = RandomTools.NewBytes(16, source);
        int index = 0;
        while (index < payload.Length)
        {
            bytes[index + 4] = payload[index];
            index = index + 1;
        }

        return new KsuidValue(bytes);
    }
}
