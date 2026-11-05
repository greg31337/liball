using System;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace token;

/// <summary>
/// Contains the outcome of converting a JWT NumericDate value.
/// </summary>
public class JwtNumericDateResult
{
    /// <summary>Indicates whether conversion succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> when conversion succeeded.</summary>
    public TokenError? Error;

    /// <summary>Contains the converted instant normalized to UTC.</summary>
    public DateTimeOffset DateTime;

    /// <summary>Contains the converted seconds since the Unix epoch.</summary>
    public decimal Seconds;

    /// <summary>Contains the JSON number text associated with the conversion.</summary>
    public string? NumberText;

    /// <summary>Initializes an unsuccessful conversion result with no details.</summary>
    public JwtNumericDateResult()
    {
        Success = false;
        Error = null;
        DateTime = DateTimeOffset.UnixEpoch;
        Seconds = 0m;
        NumberText = null;
    }
}
