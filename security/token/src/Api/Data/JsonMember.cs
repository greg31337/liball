using System;
using System.Collections.Generic;
using System.Text.Json;

namespace token;

/// <summary>
/// Associates a JSON object member name with its value.
/// </summary>
public class JsonMember
{
    /// <summary>
    /// Contains the decoded member name.
    /// </summary>
    public string? Name;

    /// <summary>
    /// Contains the member value.
    /// </summary>
    public JsonValue? Value;

    /// <summary>
    /// Initializes a JSON member with a name and value.
    /// </summary>
    /// <param name="name">The decoded member name.</param>
    /// <param name="value">The member value.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>name</c> or <c>value</c> is null.</exception>
    public JsonMember(string name, JsonValue value)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        Name = name;
        Value = value;
    }
}
