namespace cbor;

/// <summary>
/// Provides standardized and reserved CBOR simple values.
/// </summary>
public static class CborSimpleValues
{
    /// <summary>The simple value for false.</summary>
    public const byte False = 20;
    /// <summary>The simple value for true.</summary>
    public const byte True = 21;
    /// <summary>The simple value for null.</summary>
    public const byte Null = 22;
    /// <summary>The simple value for undefined.</summary>
    public const byte Undefined = 23;
    /// <summary>The first simple value reserved by RFC 8949.</summary>
    public const byte FirstReserved = 24;
    /// <summary>The last simple value reserved by RFC 8949.</summary>
    public const byte LastReserved = 31;
}
