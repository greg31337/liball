using System;

namespace cbor;

/// <summary>
/// Converts between a supported runtime type and generic CBOR values.
/// </summary>
public interface ICborTypeAdapter
{
    /// <summary>Determines whether the adapter supports a runtime type.</summary>
    /// <param name="runtimeType">The runtime type to inspect.</param>
    /// <returns>True when the adapter supports the type; otherwise, false.</returns>
    bool SupportsType(Type runtimeType);

    /// <summary>Converts a native object to a generic CBOR value.</summary>
    /// <param name="value">The native object to convert.</param>
    /// <param name="context">The current mapping state.</param>
    /// <returns>The converted CBOR value.</returns>
    CborValue EncodeObject(object value, CborMappingContext context);

    /// <summary>Converts a generic CBOR value to a native object.</summary>
    /// <param name="value">The CBOR value to convert.</param>
    /// <param name="targetType">The requested runtime type.</param>
    /// <param name="context">The current mapping state.</param>
    /// <returns>The converted native object.</returns>
    object? DecodeObject(CborValue value, Type targetType, CborMappingContext context);
}
