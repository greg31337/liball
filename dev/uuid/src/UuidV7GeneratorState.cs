using System;

namespace uuid;

internal class UuidV7GeneratorState
{
    internal bool HasLast;
    internal long LastUnixMilliseconds;
    internal byte[] LastBytes = new byte[16];

    internal static UuidV7GeneratorState FromLastValue(UuidValue? lastValue)
    {
        if (lastValue == null)
        {
            throw new ArgumentNullException("lastValue");
        }

        if (lastValue.Version() != UuidVersion.Version7 || !lastValue.HasRfc9562Variant())
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidUuidVersion, "last value must be a UUIDv7 value with RFC 9562 variant bits");
        }

        UuidV7GeneratorState state = new UuidV7GeneratorState();
        state.HasLast = true;
        state.LastBytes = lastValue.ToBytes();
        state.LastUnixMilliseconds = BigEndianTools.ReadUInt48(state.LastBytes, 0);
        return state;
    }

    internal UuidValue Next()
    {
        return NextAt(DateTimeOffset.UtcNow);
    }

    internal UuidValue NextAt(DateTimeOffset time)
    {
        long unixMilliseconds = UnixTimeTools.ToUnixMilliseconds(time);
        return NextAtUnixMilliseconds(unixMilliseconds);
    }

    internal UuidValue NextAtUnixMilliseconds(long unixMilliseconds)
    {
        lock (this)
        {
            if (unixMilliseconds < 0 || unixMilliseconds > BigEndianTools.MaxUInt48)
            {
                throw new IdentifierException(IdentifierErrorCode.TimestampOverflow, "UUIDv7 timestamp is outside the 48-bit range");
            }

            if (HasLast && unixMilliseconds < LastUnixMilliseconds)
            {
                throw new IdentifierException(IdentifierErrorCode.ClockRollback, "UUIDv7 timestamp moved backwards");
            }

            byte[] bytes;
            if (HasLast && unixMilliseconds == LastUnixMilliseconds)
            {
                bytes = ByteArrayTools.Copy(LastBytes);
                if (!UuidGenerator.IncrementV7RandomTail(bytes))
                {
                    throw new IdentifierException(IdentifierErrorCode.RandomFieldOverflow, "UUIDv7 random tail overflow");
                }
            }
            else
            {
                UuidValue generated = UuidGenerator.NewV7AtUnixMilliseconds(unixMilliseconds);
                bytes = generated.ToBytes();
            }

            HasLast = true;
            LastUnixMilliseconds = unixMilliseconds;
            LastBytes = ByteArrayTools.Copy(bytes);
            return new UuidValue(bytes);
        }
    }
}
