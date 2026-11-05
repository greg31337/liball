using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace token;

/// <summary>
/// Represents one decoded RFC 9901 Disclosure.
/// </summary>
public class SelectiveDisclosure
{
    /// <summary>Contains the original base64url-encoded Disclosure.</summary>
    public string? Encoded;

    /// <summary>Contains the Disclosure salt.</summary>
    public string? Salt;

    /// <summary>Contains the object-property claim name, or <c>null</c> for an array element.</summary>
    public string? ClaimName;

    /// <summary>Contains the disclosed JSON value.</summary>
    public JsonValue? Value;

    /// <summary>Indicates whether this Disclosure represents an array element.</summary>
    public bool ArrayElement;

    /// <summary>Initializes an empty Disclosure.</summary>
    public SelectiveDisclosure()
    {
        Encoded = null;
        Salt = null;
        ClaimName = null;
        Value = null;
        ArrayElement = false;
    }
}
