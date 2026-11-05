namespace pptx;

/// <summary>
/// Represents image crop data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxImageCrop
{
    /// <summary>
    /// Stores the left crop in thousandths of one percent, from zero through 100,000.
    /// </summary>
    public int Left;
    /// <summary>
    /// Stores the top crop in thousandths of one percent, from zero through 100,000.
    /// </summary>
    public int Top;
    /// <summary>
    /// Stores the right crop in thousandths of one percent, from zero through 100,000.
    /// </summary>
    public int Right;
    /// <summary>
    /// Stores the bottom crop in thousandths of one percent, from zero through 100,000.
    /// </summary>
    public int Bottom;

    /// <summary>
    /// Initializes a new image crop with safe defaults or the supplied values.
    /// </summary>
    public PptxImageCrop()
    {
        Left = 0;
        Top = 0;
        Right = 0;
        Bottom = 0;
    }
    internal bool IsValid()
    {
        if (Left < 0 || Left > 100000 || Top < 0 || Top > 100000 || Right < 0 || Right > 100000 || Bottom < 0 || Bottom > 100000)
        {
            return false;
        }

        if (Left + Right > 100000 || Top + Bottom > 100000)
        {
            return false;
        }

        return true;
    }
    internal PptxBounds VisibleBounds(PptxBounds imageBounds)
    {
        if (imageBounds == null)
        {
            throw new System.ArgumentNullException("imageBounds");
        }

        if (!IsValid())
        {
            throw new System.ArgumentException("Image crop values must describe a valid rectangle.");
        }

        long x = imageBounds.X + PptxUnitsCore.RoundToLong((double)imageBounds.Width * ((double)Left / 100000.0));
        long y = imageBounds.Y + PptxUnitsCore.RoundToLong((double)imageBounds.Height * ((double)Top / 100000.0));
        long width = PptxUnitsCore.RoundToLong((double)imageBounds.Width * ((double)(100000 - Left - Right) / 100000.0));
        long height = PptxUnitsCore.RoundToLong((double)imageBounds.Height * ((double)(100000 - Top - Bottom) / 100000.0));
        return new PptxBounds(x, y, width, height);
    }
}
