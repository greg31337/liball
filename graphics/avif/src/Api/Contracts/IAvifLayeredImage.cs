using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifLayeredImage"/>.</summary>
public interface IAvifLayeredImage
{
    /// <summary>Returns the construction validation result.</summary>
    /// <returns>A stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Adds one caller-owned image as the next progressive layer.</summary>
    /// <param name="image">The image to retain until writer insertion.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode AddLayer(AvifImage image);

    /// <summary>Adds one validated layer descriptor.</summary>
    /// <param name="layer">The layer to retain until writer insertion.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode AddLayerDescriptor(AvifLayer layer);

    /// <summary>Returns the number of inserted layers.</summary>
    /// <returns>Zero through four.</returns>
    int GetLayerCount();

    /// <summary>Returns one inserted layer descriptor.</summary>
    /// <param name="index">The zero-based layer index.</param>
    /// <returns>The layer, or null for an invalid index.</returns>
    AvifLayer? GetLayer(int index);

    /// <summary>Returns the selected AV1 operating-point index.</summary>
    /// <returns>Zero through 31.</returns>
    int GetOperatingPointIndex();

    /// <summary>Reports whether intermediate layers may be presented progressively.</summary>
    /// <returns><c>true</c> for progressive presentation.</returns>
    bool IsProgressive();
}
