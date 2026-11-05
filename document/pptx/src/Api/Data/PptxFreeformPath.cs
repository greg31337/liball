using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Stores a DrawingML freeform path and its local coordinate dimensions.
/// </summary>
public class PptxFreeformPath
{
    /// <summary>
    /// Stores the local path width.
    /// </summary>
    public long Width;
    /// <summary>
    /// Stores the local path height.
    /// </summary>
    public long Height;
    /// <summary>
    /// Contains path commands in drawing order.
    /// </summary>
    public List<PptxFreeformCommand> Commands;

    /// <summary>
    /// Initializes an empty path using the supplied local coordinate dimensions.
    /// </summary>
    /// <param name="width">The local path width.</param>
    /// <param name="height">The local path height.</param>
    public PptxFreeformPath(long width, long height)
    {
        if (width <= 0L)
        {
            throw new System.ArgumentOutOfRangeException("width", "Path dimensions must be positive.");
        }

        if (height <= 0L)
        {
            throw new System.ArgumentOutOfRangeException("height", "Path dimensions must be positive.");
        }

        Width = width;
        Height = height;
        Commands = new List<PptxFreeformCommand>();
    }
}
