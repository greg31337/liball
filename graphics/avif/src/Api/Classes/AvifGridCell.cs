namespace avif;

/// <summary>
/// Describes one row-major cell of a derived grid image and its decoded state.
/// </summary>
public class AvifGridCell : IAvifGridCell
{
    int index;
    int row;
    int column;
    uint itemId;
    int x;
    int y;
    int visibleWidth;
    int visibleHeight;
    AvifImage? image;
    int quality;

    internal AvifGridCell(
        int index,
        int row,
        int column,
        uint itemId)
    {
        this.index = index;
        this.row = row;
        this.column = column;
        this.itemId = itemId;
        quality = -1;
    }

    /// <summary>Returns the zero-based row-major cell index.</summary>
    /// <returns>The cell index.</returns>
    public int GetIndex()
    {
        return index;
    }

    /// <summary>Returns the zero-based grid row.</summary>
    /// <returns>The grid row.</returns>
    public int GetRow()
    {
        return row;
    }

    /// <summary>Returns the zero-based grid column.</summary>
    /// <returns>The grid column.</returns>
    public int GetColumn()
    {
        return column;
    }

    /// <summary>Returns the derived-image dependency item identifier.</summary>
    /// <returns>The dependency item identifier, or zero when none was supplied.</returns>
    public uint GetItemId()
    {
        return itemId;
    }

    /// <summary>Returns the horizontal cell origin in the complete grid.</summary>
    /// <returns>The luma x coordinate, or zero before cell geometry is known.</returns>
    public int GetX()
    {
        return x;
    }

    /// <summary>Returns the vertical cell origin in the complete grid.</summary>
    /// <returns>The luma y coordinate, or zero before cell geometry is known.</returns>
    public int GetY()
    {
        return y;
    }

    /// <summary>Returns the visible cell width after right-border cropping.</summary>
    /// <returns>The visible luma width, or zero before cell geometry is known.</returns>
    public int GetVisibleWidth()
    {
        return visibleWidth;
    }

    /// <summary>Returns the visible cell height after bottom-border cropping.</summary>
    /// <returns>The visible luma height, or zero before cell geometry is known.</returns>
    public int GetVisibleHeight()
    {
        return visibleHeight;
    }

    /// <summary>Reports whether the dependency has a decoded image in the grid cache.</summary>
    /// <returns><c>true</c> when decoded samples are available.</returns>
    public bool IsDecoded()
    {
        return image != null && image.GetCode() == AvifResultCode.Success;
    }

    /// <summary>Returns the caller-owned decoded image associated with this cell.</summary>
    /// <returns>The decoded image, or null while the dependency is unavailable.</returns>
    public AvifImage? GetImage()
    {
        return image;
    }

    /// <summary>Sets this cell's independent encoding quality.</summary>
    /// <param name="quality">Zero through one hundred, or negative one to inherit.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetQuality(int quality)
    {
        if (quality < -1 || quality > 100)
        {
            return AvifResultCode.InvalidArgument;
        }
        this.quality = quality;
        return AvifResultCode.Success;
    }

    /// <summary>Returns this cell's quality override.</summary>
    /// <returns>Zero through one hundred, or negative one to inherit.</returns>
    public int GetQuality()
    {
        return quality;
    }

    internal void SetGeometry(
        int cellWidth,
        int cellHeight,
        int outputWidth,
        int outputHeight)
    {
        x = column * cellWidth;
        y = row * cellHeight;
        visibleWidth = outputWidth - x;
        if (visibleWidth > cellWidth)
        {
            visibleWidth = cellWidth;
        }
        visibleHeight = outputHeight - y;
        if (visibleHeight > cellHeight)
        {
            visibleHeight = cellHeight;
        }
    }

    internal void SetImage(AvifImage value)
    {
        image = value;
    }
}
