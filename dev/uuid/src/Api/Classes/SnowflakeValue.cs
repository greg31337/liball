using System;

namespace uuid;

/// <summary>Represents a non-negative 63-bit Snowflake value together with the settings needed to decode it.</summary>
/// <remarks>Decimal serialization avoids precision loss at system boundaries such as JavaScript. Decoded fields are meaningful only when the same epoch and bit allocation used to generate the number are supplied.</remarks>
public class SnowflakeValue : IIdentifierValue, ISnowflakeValue, ITimestampedIdentifierValue
{
    internal ulong Value;
    internal SnowflakeSettings Settings;

    /// <summary>Creates a value decoded with Twitter-default settings.</summary>
    /// <param name="value">A number no larger than <see cref="SnowflakeLimits.MaxValue"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The number uses the reserved high bit.</exception>
    public SnowflakeValue(ulong value)
    {
        if (value > SnowflakeLimits.MaxValue)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        Value = value;
        Settings = SnowflakeSettings.TwitterDefault();
    }

    /// <summary>Creates a value decoded with custom settings.</summary>
    /// <param name="value">A number no larger than <see cref="SnowflakeLimits.MaxValue"/>.</param>
    /// <param name="settings">The epoch and bit allocation used to encode the number.</param>
    /// <returns>A new value that owns an independent copy of the settings.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The number uses the reserved high bit.</exception>
    /// <exception cref="ArgumentNullException">The settings are <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The settings are invalid.</exception>
    internal static SnowflakeValue Create(ulong value, SnowflakeSettings? settings)
    {
        SnowflakeValue snowflake = new SnowflakeValue(value);

        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        settings.ThrowIfInvalid();
        snowflake.Settings = CopySettings(settings);
        return snowflake;
    }

    /// <summary>Returns the Snowflake identifier kind.</summary>
    /// <returns><see cref="IdentifierKind.Snowflake"/>.</returns>
    public IdentifierKind Kind()
    {
        return IdentifierKind.Snowflake;
    }

    /// <summary>Returns the packed numeric identifier.</summary>
    /// <returns>The unsigned 63-bit value.</returns>
    public ulong ToUInt64()
    {
        return Value;
    }

    /// <summary>Formats the packed value as unsigned decimal text.</summary>
    /// <returns>A culture-independent sequence of decimal digits.</returns>
    public string ToDecimalString()
    {
        return Value.ToString();
    }

    /// <summary>Formats the value as canonical unsigned decimal text.</summary>
    /// <returns>The same representation as <see cref="ToDecimalString"/>.</returns>
    public string ToCanonicalString()
    {
        return ToDecimalString();
    }

    /// <summary>Extracts the 41-bit millisecond offset without adding the configured epoch.</summary>
    /// <returns>Milliseconds since the custom Snowflake epoch.</returns>
    public long TimestampMilliseconds()
    {
        return (long)(Value >> (Settings.MachineBitCount + Settings.SequenceBitCount));
    }

    /// <summary>Extracts the absolute timestamp as Unix milliseconds.</summary>
    /// <returns>The configured epoch plus the stored 41-bit offset.</returns>
    /// <exception cref="IdentifierException">The decoded timestamp is larger than a signed 64-bit value.</exception>
    public long UnixMilliseconds()
    {
        long timestampOffset = TimestampMilliseconds();
        if (Settings.EpochUnixMilliseconds > long.MaxValue - timestampOffset)
        {
            throw new IdentifierException(IdentifierErrorCode.TimestampOverflow, "decoded Snowflake timestamp exceeds signed 64-bit range");
        }

        return Settings.EpochUnixMilliseconds + timestampOffset;
    }

    /// <summary>Returns the decoded timestamp as a UTC date and time.</summary>
    /// <returns>The Snowflake creation millisecond.</returns>
    /// <exception cref="IdentifierException">The decoded Unix timestamp is outside the range supported by <see cref="DateTimeOffset"/>.</exception>
    public DateTimeOffset Time()
    {
        IdentifierTimestampResult result = Timestamp();
        if (!result.Ok)
        {
            throw new IdentifierException(result.Code, result.Error);
        }

        return result.Time;
    }

    /// <summary>Returns the timestamp carried by the Snowflake.</summary>
    /// <returns>A successful millisecond-precision timestamp result.</returns>
    public IdentifierTimestampResult Timestamp()
    {
        long timestampOffset = TimestampMilliseconds();
        if (Settings.EpochUnixMilliseconds > long.MaxValue - timestampOffset)
        {
            return IdentifierTimestampResult.Failure(IdentifierErrorCode.TimestampOverflow, "decoded Snowflake timestamp exceeds signed 64-bit range");
        }

        return Identifier.TimestampFromUnixMilliseconds(IdentifierKind.Snowflake, Settings.EpochUnixMilliseconds + timestampOffset);
    }

    /// <summary>Extracts the configured-width machine or worker field.</summary>
    /// <returns>The decoded machine ID.</returns>
    public long MachineId()
    {
        ulong mask = (ulong)Settings.MaxMachineId();
        return (long)((Value >> Settings.SequenceBitCount) & mask);
    }

    /// <summary>Extracts the configured-width per-millisecond sequence field.</summary>
    /// <returns>The decoded sequence number.</returns>
    public long Sequence()
    {
        ulong mask = (ulong)Settings.MaxSequence();
        return (long)(Value & mask);
    }

    /// <summary>Returns a copy of the settings used to decode this value.</summary>
    /// <returns>Independent mutable settings.</returns>
    public SnowflakeSettings ToSettings()
    {
        return CopySettings(Settings);
    }

    internal static SnowflakeSettings CopySettings(SnowflakeSettings? settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        return new SnowflakeSettings(
            settings.EpochUnixMilliseconds,
            settings.MachineId,
            settings.MachineBitCount,
            settings.SequenceBitCount,
            settings.ClockRollbackBehavior);
    }
}
