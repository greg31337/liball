using System;
namespace token;

/// <summary>
/// Contains the outcome of reading one JSON value.
/// </summary>
public class JsonValueResult
{
    /// <summary>
    /// Indicates whether a JSON value was read successfully.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains failure details, or <c>null</c> when no error is present.
    /// </summary>
    public TokenError? Error;

    /// <summary>
    /// Contains the decoded value, or <c>null</c> when reading failed.
    /// </summary>
    public JsonValue? Value;

    /// <summary>
    /// Initializes an unsuccessful JSON value result with no details.
    /// </summary>
    public JsonValueResult()
    {
        Success = false;
        Error = null;
        Value = null;
    }
}
