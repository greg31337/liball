using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifFileInfo"/>.</summary>
public interface IAvifFileInfo
{
    /// <summary>Returns the major brand.</summary>
    /// <returns>The four-character major brand, or an empty string.</returns>
    string GetMajorBrand();

    /// <summary>Returns the file type minor version.</summary>
    /// <returns>The unsigned minor version.</returns>
    uint GetMinorVersion();

    /// <summary>Returns the number of compatible brands.</summary>
    /// <returns>The compatible brand count.</returns>
    int GetCompatibleBrandCount();

    /// <summary>Returns one compatible brand.</summary>
    /// <param name="index">The zero-based brand index.</param>
    /// <returns>The brand, or an empty string for an invalid index.</returns>
    string GetCompatibleBrand(int index);

    /// <summary>Reports whether a brand is the major or a compatible brand.</summary>
    /// <param name="brand">The four-character brand to locate.</param>
    /// <returns><c>true</c> when the brand is declared.</returns>
    bool HasBrand(string brand);

    /// <summary>Returns the MetaBox handler type.</summary>
    /// <returns>The handler type, or an empty string.</returns>
    string GetHandlerType();

    /// <summary>Returns the MetaBox handler name.</summary>
    /// <returns>The handler name.</returns>
    string GetHandlerName();

    /// <summary>Reports whether the file declares a primary item.</summary>
    /// <returns><c>true</c> when a primary item identifier is available.</returns>
    bool HasPrimaryItem();

    /// <summary>Returns the primary item identifier.</summary>
    /// <returns>The identifier, or zero when no primary item exists.</returns>
    uint GetPrimaryItemId();
}
