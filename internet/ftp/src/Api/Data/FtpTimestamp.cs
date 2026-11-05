using System;

namespace ftp;

/// <summary>
/// Represents an available or unavailable RFC 3659 UTC timestamp.
/// </summary>
public class FtpTimestamp
{
    /// <summary>
    /// Indicates whether <see cref="ValueUtc"/> contains a parsed timestamp.
    /// </summary>
    public bool HasValue;

    /// <summary>
    /// The parsed UTC timestamp when <see cref="HasValue"/> is true.
    /// </summary>
    public DateTime ValueUtc;

    /// <summary>
    /// The exact fractional-second digits without the decimal separator.
    /// </summary>
    public string FractionalSeconds;

    /// <summary>
    /// The original timestamp text.
    /// </summary>
    public string RawValue;

    /// <summary>
    /// Indicates that the wire value used second 60; <see cref="ValueUtc"/> retains second 59 because <see cref="DateTime"/> cannot represent leap seconds.
    /// </summary>
    public bool LeapSecond;

    /// <summary>
    /// Creates an unavailable timestamp whose placeholder value has UTC kind.
    /// </summary>
    public FtpTimestamp()
    {
        HasValue = false;
        ValueUtc = new DateTime(0, DateTimeKind.Utc);
        FractionalSeconds = "";
        RawValue = "";
        LeapSecond = false;
    }
}
