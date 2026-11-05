using System;

namespace uuid;

internal class UlidGeneratorState
{
    internal bool HasLast;
    internal long LastUnixMilliseconds;
    internal byte[] LastBytes = new byte[16];

    internal static UlidGeneratorState FromLastValue(UlidValue? lastValue)
    {
        if (lastValue == null)
        {
            throw new ArgumentNullException("lastValue");
        }

        UlidGeneratorState state = new UlidGeneratorState();
        state.HasLast = true;
        state.LastBytes = lastValue.ToBytes();
        state.LastUnixMilliseconds = BigEndianTools.ReadUInt48(state.LastBytes, 0);
        return state;
    }

    internal UlidValue Next()
    {
        return NextAt(DateTimeOffset.UtcNow);
    }

    internal UlidValue NextAt(DateTimeOffset time)
    {
        long unixMilliseconds = UnixTimeTools.ToUnixMilliseconds(time);
        return NextAtUnixMilliseconds(unixMilliseconds);
    }

    internal UlidValue NextAtUnixMilliseconds(long unixMilliseconds)
    {
        lock (this)
        {
            if (unixMilliseconds < 0 || unixMilliseconds > BigEndianTools.MaxUInt48)
            {
                throw new IdentifierException(IdentifierErrorCode.TimestampOverflow, "ULID timestamp is outside the 48-bit range");
            }

            if (HasLast && unixMilliseconds < LastUnixMilliseconds)
            {
                throw new IdentifierException(IdentifierErrorCode.ClockRollback, "ULID timestamp moved backwards");
            }

            byte[] bytes;
            if (HasLast && unixMilliseconds == LastUnixMilliseconds)
            {
                bytes = ByteArrayTools.Copy(LastBytes);
                if (!UlidGenerator.IncrementRandomness(bytes))
                {
                    throw new IdentifierException(IdentifierErrorCode.RandomFieldOverflow, "ULID randomness overflow");
                }
            }
            else
            {
                UlidValue generated = UlidGenerator.NewAtUnixMilliseconds(unixMilliseconds);
                bytes = generated.ToBytes();
            }

            HasLast = true;
            LastUnixMilliseconds = unixMilliseconds;
            LastBytes = ByteArrayTools.Copy(bytes);
            return new UlidValue(bytes);
        }
    }
}
