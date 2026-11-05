using System.Collections.Generic;

namespace avif;

/// <summary>
/// Describes file-level brands and image-item metadata.
/// </summary>
public class AvifFileInfo : IAvifFileInfo
{
    string majorBrand;
    uint minorVersion;
    List<string> compatibleBrands;
    string handlerType;
    string handlerName;
    bool primaryItem;
    uint primaryItemId;

    internal AvifFileInfo(
        string majorBrand,
        uint minorVersion,
        List<string> compatibleBrands,
        string handlerType,
        string handlerName,
        bool primaryItem,
        uint primaryItemId)
    {
        this.majorBrand = majorBrand;
        this.minorVersion = minorVersion;
        this.compatibleBrands = new List<string>(compatibleBrands);
        this.handlerType = handlerType;
        this.handlerName = handlerName;
        this.primaryItem = primaryItem;
        this.primaryItemId = primaryItemId;
    }

    /// <summary>Returns the major brand.</summary>
    /// <returns>The four-character major brand, or an empty string.</returns>
    public string GetMajorBrand()
    {
        return majorBrand;
    }

    /// <summary>Returns the file type minor version.</summary>
    /// <returns>The unsigned minor version.</returns>
    public uint GetMinorVersion()
    {
        return minorVersion;
    }

    /// <summary>Returns the number of compatible brands.</summary>
    /// <returns>The compatible brand count.</returns>
    public int GetCompatibleBrandCount()
    {
        return compatibleBrands.Count;
    }

    /// <summary>Returns one compatible brand.</summary>
    /// <param name="index">The zero-based brand index.</param>
    /// <returns>The brand, or an empty string for an invalid index.</returns>
    public string GetCompatibleBrand(int index)
    {
        if (index < 0 || index >= compatibleBrands.Count)
        {
            return string.Empty;
        }

        return compatibleBrands[index];
    }

    /// <summary>Reports whether a brand is the major or a compatible brand.</summary>
    /// <param name="brand">The four-character brand to locate.</param>
    /// <returns><c>true</c> when the brand is declared.</returns>
    public bool HasBrand(string brand)
    {
        if (majorBrand == brand)
        {
            return true;
        }

        int index = 0;
        while (index < compatibleBrands.Count)
        {
            if (compatibleBrands[index] == brand)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    /// <summary>Returns the MetaBox handler type.</summary>
    /// <returns>The handler type, or an empty string.</returns>
    public string GetHandlerType()
    {
        return handlerType;
    }

    /// <summary>Returns the MetaBox handler name.</summary>
    /// <returns>The handler name.</returns>
    public string GetHandlerName()
    {
        return handlerName;
    }

    /// <summary>Reports whether the file declares a primary item.</summary>
    /// <returns><c>true</c> when a primary item identifier is available.</returns>
    public bool HasPrimaryItem()
    {
        return primaryItem;
    }

    /// <summary>Returns the primary item identifier.</summary>
    /// <returns>The identifier, or zero when no primary item exists.</returns>
    public uint GetPrimaryItemId()
    {
        return primaryItemId;
    }
}
