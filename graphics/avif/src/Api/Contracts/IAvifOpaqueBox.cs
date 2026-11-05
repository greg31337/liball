using System;
using System.Buffers.Binary;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifOpaqueBox"/>.</summary>
public interface IAvifOpaqueBox
{
    /// <summary>Returns the construction and validation result.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the opaque four-character box type.</summary>
    /// <returns>The type, or an empty string after failure.</returns>
    string? GetTypeName();

    /// <summary>Reports whether the property was marked essential.</summary>
    /// <returns><c>true</c> when the property is essential.</returns>
    bool IsEssential();

    /// <summary>Returns an independent copy of the complete property box.</summary>
    /// <returns>The raw box bytes, or an empty array after failure.</returns>
    byte[] GetRawData();
}
