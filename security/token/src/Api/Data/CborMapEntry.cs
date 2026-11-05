using System;
namespace token;

/// <summary>
/// Associates a CBOR map key with its value.
/// </summary>
public class CborMapEntry
{
    /// <summary>
    /// Contains the map key.
    /// </summary>
    public CborItem? Key;

    /// <summary>
    /// Contains the map value.
    /// </summary>
    public CborItem? Value;

    /// <summary>
    /// Initializes a CBOR map entry with a key and value.
    /// </summary>
    /// <param name="key">The map key.</param>
    /// <param name="value">The map value.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> or <c>value</c> is null.</exception>
    public CborMapEntry(CborItem key, CborItem value)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        Key = key;
        Value = value;
    }
}
