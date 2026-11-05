using System;

namespace uuid;

/// <summary>Defines operations specific to a Snowflake value.</summary>
public interface ISnowflakeValue
{
    /// <summary>Returns the packed unsigned number.</summary>
    ulong ToUInt64();
    /// <summary>Formats decimal text.</summary>
    string ToDecimalString();
    /// <summary>Returns milliseconds since the configured epoch.</summary>
    long TimestampMilliseconds();
    /// <summary>Returns Unix milliseconds.</summary>
    long UnixMilliseconds();
    /// <summary>Returns the timestamp as date and time.</summary>
    DateTimeOffset Time();
    /// <summary>Returns the machine identifier.</summary>
    long MachineId();
    /// <summary>Returns the sequence.</summary>
    long Sequence();
    /// <summary>Returns an independent settings copy.</summary>
    SnowflakeSettings ToSettings();
}
