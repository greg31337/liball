using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace token;

/// <summary>
/// Contains the outcome of reading a JSON value.
/// </summary>
public class JsonReadResult
{
    /// <summary>
    /// Indicates whether the JSON value was read successfully.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains failure details, or <c>null</c> when no error is present.
    /// </summary>
    public TokenError? Error;

    /// <summary>
    /// Contains the parsed JSON document, or <c>null</c> when reading failed.
    /// </summary>
    /// <remarks>The caller is responsible for disposing a successful document.</remarks>
    public JsonDocument? Document;

    /// <summary>
    /// Initializes an unsuccessful JSON read result with no details.
    /// </summary>
    public JsonReadResult()
    {
        Success = false;
        Error = null;
        Document = null;
    }
}
