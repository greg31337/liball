using System;

namespace cbor;

/// <summary>
/// Maps between runtime objects and generic CBOR values.
/// </summary>
public interface ICborObjectMapper
{
    /// <summary>Maps a runtime object to a generic CBOR value.</summary>
    /// <param name="value">The runtime object to map.</param>
    /// <param name="runtimeType">The declared runtime type.</param>
    /// <param name="options">The mapping policies and limits.</param>
    /// <returns>The mapped CBOR value.</returns>
    CborValue MapToValue(object value, Type runtimeType, CborMappingOptions options);

    /// <summary>Maps a generic CBOR value to a runtime object.</summary>
    /// <param name="value">The CBOR value to map.</param>
    /// <param name="targetType">The requested runtime type.</param>
    /// <param name="options">The mapping policies and limits.</param>
    /// <returns>The mapped runtime object.</returns>
    object? MapFromValue(CborValue value, Type targetType, CborMappingOptions options);
}
