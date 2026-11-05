namespace uuid;

/// <summary>Defines Snowflake settings and generator factory operations.</summary>
public interface ISnowflake
{
    /// <summary>Creates independent Twitter-compatible settings.</summary>
    SnowflakeSettings DefaultSettings();
    /// <summary>Creates a generator with Twitter-compatible settings.</summary>
    SnowflakeGenerator DefaultGenerator();
    /// <summary>Creates a generator from custom settings.</summary>
    SnowflakeGenerator CreateGenerator(SnowflakeSettings? settings);
    /// <summary>Validates mutable settings without throwing.</summary>
    IdentifierValidationResult ValidateSettings(SnowflakeSettings? settings);
    /// <summary>Returns the largest machine ID for a bit count.</summary>
    long MaxMachineId(int machineBitCount);
    /// <summary>Returns the largest sequence for a bit count.</summary>
    long MaxSequence(int sequenceBitCount);
    /// <summary>Returns the largest 41-bit timestamp offset.</summary>
    long MaxTimestampOffsetMilliseconds();
}
