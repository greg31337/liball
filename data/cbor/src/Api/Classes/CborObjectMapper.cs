using System;

namespace cbor;

/// <summary>
/// Maps between runtime objects and generic CBOR values.
/// </summary>
public class CborObjectMapper : ICborObjectMapper
{
    /// <summary>Creates a CBOR object mapper.</summary>
    public CborObjectMapper()
    {
    }

    /// <summary>Maps a runtime object to a generic CBOR value.</summary>
    /// <param name="value">The runtime object to map.</param>
    /// <param name="runtimeType">The declared runtime type.</param>
    /// <param name="options">The mapping policies and limits.</param>
    /// <returns>The mapped CBOR value.</returns>
    public CborValue MapToValue(object value, Type runtimeType, CborMappingOptions options)
    {
        CborBuiltInObjectMapping mapping = new CborBuiltInObjectMapping(options);
        return mapping.MapNative(value, runtimeType);
    }

    /// <summary>Maps a generic CBOR value to a runtime object.</summary>
    /// <param name="value">The CBOR value to map.</param>
    /// <param name="targetType">The requested runtime type.</param>
    /// <param name="options">The mapping policies and limits.</param>
    /// <returns>The mapped runtime object.</returns>
    public object? MapFromValue(CborValue value, Type targetType, CborMappingOptions options)
    {
        CborBuiltInObjectMapping mapping = new CborBuiltInObjectMapping(options);
        return mapping.MapCbor(value, targetType);
    }
}
