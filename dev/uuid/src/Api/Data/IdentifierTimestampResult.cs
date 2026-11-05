using System;

namespace uuid;

/// <summary>Reports a timestamp extracted from a supported time-bearing identifier.</summary>
/// <remarks>Check <see cref="Ok"/> before reading the timestamp fields. Failures provide <see cref="Code"/> and <see cref="Error"/>. UUID versions 1, 6, and 7, ULID, KSUID, Snowflake, and ObjectId can carry timestamps.</remarks>
public class IdentifierTimestampResult
{
    /// <summary>Indicates whether a timestamp was extracted.</summary>
    public bool Ok;

    /// <summary>Contains the identifier format that supplied the timestamp.</summary>
    public IdentifierKind Kind;

    /// <summary>Contains the timestamp as milliseconds since the Unix epoch.</summary>
    public long UnixMilliseconds;

    /// <summary>Contains the timestamp as whole seconds since the Unix epoch.</summary>
    /// <remarks>Subsecond precision is discarded. Negative millisecond values are rounded toward the preceding whole Unix second.</remarks>
    public long UnixSeconds;

    /// <summary>Contains the timestamp as a UTC <see cref="DateTimeOffset"/> value.</summary>
    public DateTimeOffset Time;

    /// <summary>Contains a plain-English failure reason, or an empty string when extraction succeeded.</summary>
    public string Error;

    /// <summary>Contains a stable failure code, or <see cref="IdentifierErrorCode.None"/> when extraction succeeded.</summary>
    public IdentifierErrorCode Code;

    internal IdentifierTimestampResult(bool ok, IdentifierKind kind, long unixMilliseconds, long unixSeconds, DateTimeOffset time, string error, IdentifierErrorCode code)
    {
        Ok = ok;
        Kind = kind;
        UnixMilliseconds = unixMilliseconds;
        UnixSeconds = unixSeconds;
        Time = time;
        Error = error;
        Code = code;
    }

    internal static IdentifierTimestampResult Success(IdentifierKind kind, long unixMilliseconds, long unixSeconds, DateTimeOffset time)
    {
        return new IdentifierTimestampResult(true, kind, unixMilliseconds, unixSeconds, time, "", IdentifierErrorCode.None);
    }

    internal static IdentifierTimestampResult Failure(IdentifierErrorCode code, string error)
    {
        return new IdentifierTimestampResult(false, IdentifierKind.Unknown, 0, 0, DateTimeOffset.UnixEpoch, error, code);
    }
}
