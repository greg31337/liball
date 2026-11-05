namespace uuid;

/// <summary>Creates Snowflake settings and generators and validates mutable settings.</summary>
public class Snowflake : ISnowflake
{
    /// <summary>Creates independent Twitter-compatible settings.</summary>
    /// <returns>Settings using the Twitter epoch, 10 machine bits, and 12 sequence bits.</returns>
    public SnowflakeSettings DefaultSettings()
    {
        return SnowflakeSettings.TwitterDefault();
    }

    /// <summary>Creates a generator with Twitter-compatible settings and machine ID zero.</summary>
    /// <returns>A new stateful generator.</returns>
    public SnowflakeGenerator DefaultGenerator()
    {
        return new SnowflakeGenerator(DefaultSettings());
    }

    /// <summary>Creates a generator from custom settings.</summary>
    /// <param name="settings">The settings to copy and validate.</param>
    /// <returns>A new stateful generator.</returns>
    public SnowflakeGenerator CreateGenerator(SnowflakeSettings? settings)
    {
        return new SnowflakeGenerator(settings);
    }

    /// <summary>Validates mutable Snowflake settings without throwing.</summary>
    /// <param name="settings">The settings to inspect.</param>
    /// <returns>A success result or a specific failure reason.</returns>
    public IdentifierValidationResult ValidateSettings(SnowflakeSettings? settings)
    {
        if (settings == null)
        {
            return IdentifierValidationResult.Failure(IdentifierErrorCode.NullInput, "Snowflake settings is null");
        }

        return settings.Validate();
    }

    /// <summary>Returns the largest machine ID supported by a bit count.</summary>
    /// <param name="machineBitCount">A bit count from zero through 22.</param>
    /// <returns>The maximum representable value.</returns>
    public long MaxMachineId(int machineBitCount)
    {
        return SnowflakeSettings.MaxValueForBitCount(machineBitCount);
    }

    /// <summary>Returns the largest sequence supported by a bit count.</summary>
    /// <param name="sequenceBitCount">A bit count from zero through 22.</param>
    /// <returns>The maximum representable value.</returns>
    public long MaxSequence(int sequenceBitCount)
    {
        return SnowflakeSettings.MaxValueForBitCount(sequenceBitCount);
    }

    /// <summary>Returns the largest offset supported by the 41-bit timestamp field.</summary>
    /// <returns><c>2^41 - 1</c> milliseconds.</returns>
    public long MaxTimestampOffsetMilliseconds()
    {
        return (1L << SnowflakeSettings.TimestampBitCount) - 1;
    }
}
