using System;
using System.Collections.Generic;
using System.Text.Json;

namespace token;

/// <summary>
/// Identifies the kind of value stored in a JSON object model.
/// </summary>
public enum TokenJsonValueKind
{
    /// <summary>
    /// A JSON null value.
    /// </summary>
    Null = 0,

    /// <summary>
    /// A JSON boolean value.
    /// </summary>
    Boolean = 1,

    /// <summary>
    /// A JSON number represented by its exact source text.
    /// </summary>
    Number = 2,

    /// <summary>
    /// A JSON string value.
    /// </summary>
    String = 3,

    /// <summary>
    /// A JSON array value.
    /// </summary>
    Array = 4,

    /// <summary>
    /// A JSON object value.
    /// </summary>
    Object = 5
}
