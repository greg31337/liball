namespace ttf;

/// <summary>Contains OpenType gasp grid-fitting and smoothing behavior.</summary>
public class FontGridFittingBehavior : IFontGridFittingBehavior
{
    SfntGaspTable table;

    internal FontGridFittingBehavior(SfntGaspTable table)
    {
        this.table = table;
    }

    /// <summary>Gets the table version.</summary>
    /// <returns>The version.</returns>
    public int GetVersion() { return table.GetVersion(); }

    /// <summary>Gets the number of behavior ranges.</summary>
    /// <returns>The range count.</returns>
    public int GetRangeCount() { return table.GetRangeCount(); }

    /// <summary>Gets one range's inclusive maximum pixels per em.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The maximum pixels per em.</returns>
    public int GetMaximumPixelsPerEm(int index)
    {
        return GetRange(index).GetMaximumPpem();
    }

    /// <summary>Gets one range's raw behavior mask.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The behavior mask.</returns>
    public int GetBehavior(int index) { return GetRange(index).GetBehavior(); }

    /// <summary>Reports whether a range requests grid fitting.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>True when requested.</returns>
    public bool UsesGridFitting(int index)
    {
        return GetRange(index).UsesGridFitting();
    }

    /// <summary>Reports whether a range requests grayscale rendering.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>True when requested.</returns>
    public bool UsesGrayscaleRendering(int index)
    {
        return GetRange(index).UsesGrayscaleRendering();
    }

    /// <summary>Reports whether a range requests symmetric grid fitting.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>True when requested.</returns>
    public bool UsesSymmetricGridFitting(int index)
    {
        return GetRange(index).UsesSymmetricGridFitting();
    }

    /// <summary>Reports whether a range requests symmetric smoothing.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>True when requested.</returns>
    public bool UsesSymmetricSmoothing(int index)
    {
        return GetRange(index).UsesSymmetricSmoothing();
    }

    /// <summary>Finds the behavior range for a pixels-per-em size.</summary>
    /// <param name="pixelsPerEm">The size from zero through 65535.</param>
    /// <returns>The range index, or negative one when absent.</returns>
    public int FindRangeIndex(int pixelsPerEm)
    {
        if (pixelsPerEm < 0 || pixelsPerEm > 0xFFFF)
        {
            throw new System.ArgumentOutOfRangeException("pixelsPerEm");
        }

        int index = 0;

        while (index < table.GetRangeCount())
        {
            if (pixelsPerEm <= table.GetRange(index).GetMaximumPpem())
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    SfntGaspRange GetRange(int index)
    {
        if (index < 0 || index >= table.GetRangeCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return table.GetRange(index);
    }
}
