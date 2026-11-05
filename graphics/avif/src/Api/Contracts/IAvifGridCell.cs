namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifGridCell"/>.</summary>
public interface IAvifGridCell
{
    /// <summary>Returns the zero-based row-major cell index.</summary>
    /// <returns>The cell index.</returns>
    int GetIndex();

    /// <summary>Returns the zero-based grid row.</summary>
    /// <returns>The grid row.</returns>
    int GetRow();

    /// <summary>Returns the zero-based grid column.</summary>
    /// <returns>The grid column.</returns>
    int GetColumn();

    /// <summary>Returns the derived-image dependency item identifier.</summary>
    /// <returns>The dependency item identifier, or zero when none was supplied.</returns>
    uint GetItemId();

    /// <summary>Returns the horizontal cell origin in the complete grid.</summary>
    /// <returns>The luma x coordinate, or zero before cell geometry is known.</returns>
    int GetX();

    /// <summary>Returns the vertical cell origin in the complete grid.</summary>
    /// <returns>The luma y coordinate, or zero before cell geometry is known.</returns>
    int GetY();

    /// <summary>Returns the visible cell width after right-border cropping.</summary>
    /// <returns>The visible luma width, or zero before cell geometry is known.</returns>
    int GetVisibleWidth();

    /// <summary>Returns the visible cell height after bottom-border cropping.</summary>
    /// <returns>The visible luma height, or zero before cell geometry is known.</returns>
    int GetVisibleHeight();

    /// <summary>Reports whether the dependency has a decoded image in the grid cache.</summary>
    /// <returns><c>true</c> when decoded samples are available.</returns>
    bool IsDecoded();

    /// <summary>Returns the caller-owned decoded image associated with this cell.</summary>
    /// <returns>The decoded image, or null while the dependency is unavailable.</returns>
    AvifImage? GetImage();

    /// <summary>Sets this cell's independent encoding quality.</summary>
    /// <param name="quality">Zero through one hundred, or negative one to inherit.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetQuality(int quality);

    /// <summary>Returns this cell's quality override.</summary>
    /// <returns>Zero through one hundred, or negative one to inherit.</returns>
    int GetQuality();
}
