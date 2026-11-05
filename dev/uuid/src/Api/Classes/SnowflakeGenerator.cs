using System;

namespace uuid;

/// <summary>Generates and decodes stateful 63-bit Snowflake identifiers.</summary>
/// <remarks>The generator is thread-safe for automatic sequence allocation. Each timestamp supports <c>2^SequenceBitCount</c> values per generator; exhausting that range throws instead of waiting for the clock.</remarks>
public class SnowflakeGenerator : ISnowflakeGenerator
{
    internal SnowflakeSettings Settings;
    internal bool HasLast;
    internal long LastUnixMilliseconds;
    internal long NextSequence;
    internal ISnowflakeClock Clock;

    /// <summary>Creates a generator from a validated copy of custom settings.</summary>
    /// <param name="settings">The epoch, machine ID, bit allocation, and rollback policy.</param>
    /// <exception cref="ArgumentNullException">The settings are <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The settings are invalid.</exception>
    public SnowflakeGenerator(SnowflakeSettings? settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        settings.ThrowIfInvalid();
        Settings = SnowflakeValue.CopySettings(settings);
        Clock = new SystemSnowflakeClock();
    }

    /// <summary>Creates a generator with Twitter-compatible settings and machine ID zero.</summary>
    /// <returns>A new generator using independent default settings.</returns>
    /// <summary>Generates the next Snowflake using the current UTC millisecond.</summary>
    /// <returns>A value containing the configured machine ID and next sequence.</returns>
    /// <exception cref="IdentifierException">The clock moved backwards, sequence capacity was exhausted, or the current time is outside the configured timestamp range.</exception>
    public SnowflakeValue Next()
    {
        lock (this)
        {
            long unixMilliseconds = Clock.UnixMilliseconds();
            ThrowIfInvalidPublicUnixMilliseconds(unixMilliseconds, Settings);
            long timestamp = ResolveRealTimeClockRollback(unixMilliseconds);
            ThrowIfInvalidPublicUnixMilliseconds(timestamp, Settings);
            long sequence = NextSequenceForTimestamp(timestamp);
            ulong packed = Pack(timestamp, Settings.MachineId, sequence, Settings);
            return SnowflakeValue.Create(packed, Settings);
        }
    }

    /// <summary>Generates the next Snowflake for a supplied instant.</summary>
    /// <param name="time">The instant whose Unix millisecond is packed.</param>
    /// <returns>A value with an automatically allocated sequence.</returns>
    /// <exception cref="IdentifierException">The timestamp moved backwards, sequence capacity was exhausted, or the timestamp is outside the configured range.</exception>
    public SnowflakeValue NextAt(DateTimeOffset time)
    {
        return NextAtUnixMilliseconds(UnixTimeTools.ToUnixMilliseconds(time));
    }

    /// <summary>Generates the next Snowflake for a supplied Unix millisecond.</summary>
    /// <param name="unixMilliseconds">Milliseconds since the Unix epoch.</param>
    /// <returns>A value with an automatically allocated sequence.</returns>
    /// <exception cref="IdentifierException">The timestamp moved backwards, sequence capacity was exhausted, or the timestamp is outside the configured range.</exception>
    public SnowflakeValue NextAtUnixMilliseconds(long unixMilliseconds)
    {
        lock (this)
        {
            ThrowIfInvalidPublicUnixMilliseconds(unixMilliseconds, Settings);
            long timestamp = ResolveClockRollback(unixMilliseconds);
            long sequence = NextSequenceForTimestamp(timestamp);
            ulong packed = Pack(timestamp, Settings.MachineId, sequence, Settings);
            return SnowflakeValue.Create(packed, Settings);
        }
    }

    /// <summary>Packs a Snowflake for an instant and explicit sequence without advancing generator state.</summary>
    /// <param name="time">The instant whose Unix millisecond is packed.</param>
    /// <param name="sequence">A sequence that fits the configured sequence bits.</param>
    /// <returns>The packed value.</returns>
    /// <exception cref="IdentifierException">The timestamp or sequence is outside its configured range.</exception>
    public SnowflakeValue NextAtWithSequence(DateTimeOffset time, long sequence)
    {
        return NextAtUnixMillisecondsWithSequence(UnixTimeTools.ToUnixMilliseconds(time), sequence);
    }

    /// <summary>Packs a Snowflake for a Unix millisecond and explicit sequence without advancing generator state.</summary>
    /// <param name="unixMilliseconds">Milliseconds since the Unix epoch.</param>
    /// <param name="sequence">A sequence that fits the configured sequence bits.</param>
    /// <returns>The packed value.</returns>
    /// <exception cref="IdentifierException">The timestamp or sequence is outside its configured range.</exception>
    public SnowflakeValue NextAtUnixMillisecondsWithSequence(long unixMilliseconds, long sequence)
    {
        ThrowIfInvalidPublicUnixMilliseconds(unixMilliseconds, Settings);
        if (sequence < 0 || sequence > Settings.MaxSequence())
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidValue, "sequence is outside configured bit range");
        }

        ulong packed = Pack(unixMilliseconds, Settings.MachineId, sequence, Settings);
        return SnowflakeValue.Create(packed, Settings);
    }

    /// <summary>Builds the lowest Snowflake possible for an instant under the configured layout.</summary>
    /// <param name="time">The target instant.</param>
    /// <returns>A value with machine ID and sequence both set to zero.</returns>
    /// <exception cref="IdentifierException">The timestamp is outside the configured range.</exception>
    public SnowflakeValue RangeStartAt(DateTimeOffset time)
    {
        return RangeStartAtUnixMilliseconds(UnixTimeTools.ToUnixMilliseconds(time));
    }

    /// <summary>Builds the lowest Snowflake possible for a Unix millisecond.</summary>
    /// <param name="unixMilliseconds">The target Unix millisecond.</param>
    /// <returns>A value with machine ID and sequence both set to zero.</returns>
    /// <exception cref="IdentifierException">The timestamp is outside the configured range.</exception>
    public SnowflakeValue RangeStartAtUnixMilliseconds(long unixMilliseconds)
    {
        ThrowIfInvalidPublicUnixMilliseconds(unixMilliseconds, Settings);
        ulong packed = RangeStartAtUnixMilliseconds(unixMilliseconds, Settings);
        return SnowflakeValue.Create(packed, Settings);
    }

    /// <summary>Builds the highest Snowflake possible for an instant under the configured layout.</summary>
    /// <param name="time">The target instant.</param>
    /// <returns>A value with machine ID and sequence set to their configured maxima.</returns>
    /// <exception cref="IdentifierException">The timestamp is outside the configured range.</exception>
    public SnowflakeValue RangeEndAt(DateTimeOffset time)
    {
        return RangeEndAtUnixMilliseconds(UnixTimeTools.ToUnixMilliseconds(time));
    }

    /// <summary>Builds the highest Snowflake possible for a Unix millisecond.</summary>
    /// <param name="unixMilliseconds">The target Unix millisecond.</param>
    /// <returns>A value with machine ID and sequence set to their configured maxima.</returns>
    /// <exception cref="IdentifierException">The timestamp is outside the configured range.</exception>
    public SnowflakeValue RangeEndAtUnixMilliseconds(long unixMilliseconds)
    {
        ThrowIfInvalidPublicUnixMilliseconds(unixMilliseconds, Settings);
        ulong packed = RangeEndAtUnixMilliseconds(unixMilliseconds, Settings);
        return SnowflakeValue.Create(packed, Settings);
    }

    /// <summary>Wraps a packed number with this generator's settings for field decoding.</summary>
    /// <param name="value">The unsigned 63-bit Snowflake number.</param>
    /// <returns>A decoded value that owns a settings copy.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The number uses the reserved high bit.</exception>
    public SnowflakeValue Decode(ulong value)
    {
        return SnowflakeValue.Create(value, Settings);
    }

    /// <summary>Parses unsigned decimal Snowflake text with this generator's settings.</summary>
    /// <param name="text">A non-empty sequence of decimal digits.</param>
    /// <returns>A decoded value and normalized decimal text, or a failure reason.</returns>
    public SnowflakeParseResult Parse(string? text)
    {
        return ParseDecimalString(text, Settings);
    }

    /// <summary>Parses unsigned decimal Snowflake text without throwing for invalid input.</summary>
    /// <param name="text">The decimal text to parse.</param>
    /// <returns>The same result contract as <see cref="Parse"/>.</returns>
    public SnowflakeParseResult TryParse(string? text)
    {
        return Parse(text);
    }

    /// <summary>Parses unsigned decimal Snowflake text and returns the decoded value.</summary>
    /// <param name="text">A decimal value in the non-negative 63-bit range.</param>
    /// <returns>The decoded Snowflake.</returns>
    /// <exception cref="IdentifierException">The text is null, empty, non-decimal, or outside the supported range.</exception>
    public SnowflakeValue ParseValue(string? text)
    {
        SnowflakeParseResult result = Parse(text);
        if (!result.Ok || result.Value == null)
        {
            throw new IdentifierException(result.Code, result.Error);
        }

        return result.Value;
    }

    /// <summary>Returns a copy of this generator's settings.</summary>
    /// <returns>Independent mutable settings; changing them does not reconfigure the generator.</returns>
    public SnowflakeSettings ToSettings()
    {
        return SnowflakeValue.CopySettings(Settings);
    }

    internal long ResolveClockRollback(long unixMilliseconds)
    {
        if (!HasLast || unixMilliseconds >= LastUnixMilliseconds)
        {
            return unixMilliseconds;
        }

        if (Settings.ClockRollbackBehavior == SnowflakeClockRollbackBehavior.FailFast)
        {
            throw new IdentifierException(IdentifierErrorCode.ClockRollback, "Snowflake clock moved backwards");
        }

        return LastUnixMilliseconds;
    }

    internal long ResolveRealTimeClockRollback(long unixMilliseconds)
    {
        if (!HasLast || unixMilliseconds >= LastUnixMilliseconds)
        {
            return unixMilliseconds;
        }

        if (Settings.ClockRollbackBehavior == SnowflakeClockRollbackBehavior.FailFast)
        {
            throw new IdentifierException(IdentifierErrorCode.ClockRollback, "Snowflake clock moved backwards");
        }

        while (unixMilliseconds < LastUnixMilliseconds)
        {
            Clock.WaitOneMillisecond();
            unixMilliseconds = Clock.UnixMilliseconds();
        }

        return unixMilliseconds;
    }

    internal long NextSequenceForTimestamp(long unixMilliseconds)
    {
        if (!HasLast || unixMilliseconds > LastUnixMilliseconds)
        {
            HasLast = true;
            LastUnixMilliseconds = unixMilliseconds;
            NextSequence = 1;
            return 0;
        }

        if (NextSequence > Settings.MaxSequence())
        {
            throw new IdentifierException(IdentifierErrorCode.SequenceExhaustion, "Snowflake sequence exhausted for millisecond");
        }

        long sequence = NextSequence;
        NextSequence = NextSequence + 1;
        return sequence;
    }

    internal static SnowflakeParseResult ParseDecimalString(string? text, SnowflakeSettings? settings)
    {
        if (settings == null)
        {
            return SnowflakeParseResult.Failure(IdentifierErrorCode.NullInput, "Snowflake settings is null");
        }

        IdentifierValidationResult settingsValidation = settings.Validate();
        if (!settingsValidation.Ok)
        {
            return SnowflakeParseResult.Failure(settingsValidation.Code, settingsValidation.Error);
        }

        if (text == null)
        {
            return SnowflakeParseResult.Failure(IdentifierErrorCode.NullInput, "Snowflake text is null");
        }

        if (text.Length == 0)
        {
            return SnowflakeParseResult.Failure(IdentifierErrorCode.InvalidLength, "Snowflake text is empty");
        }

        ulong value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                return SnowflakeParseResult.Failure(IdentifierErrorCode.InvalidCharacter, "invalid decimal character at index " + index.ToString());
            }

            ulong digit = (ulong)(character - '0');
            if (value > (SnowflakeLimits.MaxValue - digit) / 10)
            {
                return SnowflakeParseResult.Failure(IdentifierErrorCode.ValueOverflow, "Snowflake decimal value exceeds 63-bit range");
            }

            value = (value * 10) + digit;
            index = index + 1;
        }

        return SnowflakeParseResult.Success(SnowflakeValue.Create(value, settings));
    }

    internal static ulong Pack(long unixMilliseconds, long machineId, long sequence, SnowflakeSettings? settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        settings.ThrowIfInvalid();
        ThrowIfInvalidUnixMilliseconds(unixMilliseconds, settings);

        if (machineId < 0 || machineId > settings.MaxMachineId())
        {
            throw new ArgumentOutOfRangeException("machineId");
        }

        if (sequence < 0 || sequence > settings.MaxSequence())
        {
            throw new ArgumentOutOfRangeException("sequence");
        }

        long timestampOffset = unixMilliseconds - settings.EpochUnixMilliseconds;
        ulong timestampPart = (ulong)timestampOffset << (settings.MachineBitCount + settings.SequenceBitCount);
        ulong machinePart = (ulong)machineId << settings.SequenceBitCount;
        ulong sequencePart = (ulong)sequence;
        return timestampPart | machinePart | sequencePart;
    }

    internal static ulong RangeStartAtUnixMilliseconds(long unixMilliseconds, SnowflakeSettings? settings)
    {
        return Pack(unixMilliseconds, 0, 0, settings);
    }

    internal static ulong RangeEndAtUnixMilliseconds(long unixMilliseconds, SnowflakeSettings? settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        settings.ThrowIfInvalid();
        return Pack(unixMilliseconds, settings.MaxMachineId(), settings.MaxSequence(), settings);
    }

    internal static void ThrowIfInvalidUnixMilliseconds(long unixMilliseconds, SnowflakeSettings? settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        settings.ThrowIfInvalid();
        long epoch = settings.EpochUnixMilliseconds;
        long maxOffset = settings.MaxTimestampOffsetMilliseconds();
        if (unixMilliseconds < epoch)
        {
            throw new ArgumentOutOfRangeException("unixMilliseconds");
        }

        if (epoch <= long.MaxValue - maxOffset && unixMilliseconds > epoch + maxOffset)
        {
            throw new ArgumentOutOfRangeException("unixMilliseconds");
        }
    }

    internal static void ThrowIfInvalidPublicUnixMilliseconds(long unixMilliseconds, SnowflakeSettings settings)
    {
        long epoch = settings.EpochUnixMilliseconds;
        long maxOffset = settings.MaxTimestampOffsetMilliseconds();
        if (unixMilliseconds < epoch)
        {
            throw new IdentifierException(IdentifierErrorCode.TimestampOverflow, "Snowflake timestamp is before the configured epoch");
        }

        if (epoch <= long.MaxValue - maxOffset && unixMilliseconds > epoch + maxOffset)
        {
            throw new IdentifierException(IdentifierErrorCode.TimestampOverflow, "Snowflake timestamp exceeds the configured 41-bit range");
        }
    }
}
