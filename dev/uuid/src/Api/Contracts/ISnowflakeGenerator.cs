using System;

namespace uuid;

/// <summary>Defines stateful Snowflake generation and decoding operations.</summary>
public interface ISnowflakeGenerator
{
    /// <summary>Generates the next Snowflake.</summary>
    SnowflakeValue Next();
    /// <summary>Generates the next Snowflake for an instant.</summary>
    SnowflakeValue NextAt(DateTimeOffset time);
    /// <summary>Generates the next Snowflake for a Unix millisecond.</summary>
    SnowflakeValue NextAtUnixMilliseconds(long unixMilliseconds);
    /// <summary>Packs a Snowflake for an instant and sequence.</summary>
    SnowflakeValue NextAtWithSequence(DateTimeOffset time, long sequence);
    /// <summary>Packs a Snowflake for a Unix millisecond and sequence.</summary>
    SnowflakeValue NextAtUnixMillisecondsWithSequence(long unixMilliseconds, long sequence);
    /// <summary>Returns the range start for an instant.</summary>
    SnowflakeValue RangeStartAt(DateTimeOffset time);
    /// <summary>Returns the range start for a Unix millisecond.</summary>
    SnowflakeValue RangeStartAtUnixMilliseconds(long unixMilliseconds);
    /// <summary>Returns the range end for an instant.</summary>
    SnowflakeValue RangeEndAt(DateTimeOffset time);
    /// <summary>Returns the range end for a Unix millisecond.</summary>
    SnowflakeValue RangeEndAtUnixMilliseconds(long unixMilliseconds);
    /// <summary>Decodes a packed number.</summary>
    SnowflakeValue Decode(ulong value);
    /// <summary>Parses decimal Snowflake text.</summary>
    SnowflakeParseResult Parse(string? text);
    /// <summary>Tries to parse decimal Snowflake text.</summary>
    SnowflakeParseResult TryParse(string? text);
    /// <summary>Parses decimal Snowflake text or throws.</summary>
    SnowflakeValue ParseValue(string? text);
    /// <summary>Returns an independent settings copy.</summary>
    SnowflakeSettings ToSettings();
}
