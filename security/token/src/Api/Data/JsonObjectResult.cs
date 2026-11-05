using System;
using System.Collections.Generic;
using System.Text.Json;

namespace token;

/// <summary>
/// Contains the outcome of reading a JSON object model.
/// </summary>
public class JsonObjectResult
{
    /// <summary>
    /// Indicates whether the JSON object was read successfully.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains failure details, or <c>null</c> when no error is present.
    /// </summary>
    public TokenError? Error;

    /// <summary>
    /// Contains the parsed object, or <c>null</c> when reading failed.
    /// </summary>
    public JsonObject? Object;

    /// <summary>
    /// Initializes an unsuccessful JSON object result with no details.
    /// </summary>
    public JsonObjectResult()
    {
        Success = false;
        Error = null;
        Object = null;
    }
}
