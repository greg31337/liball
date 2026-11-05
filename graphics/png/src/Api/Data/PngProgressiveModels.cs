namespace png;

/// <summary>
/// Describes one reconstructed non-interlaced image row.
/// </summary>
public class PngProgressiveRow
{
    /// <summary>
    /// Initializes an empty progressive row update.
    /// </summary>
    public PngProgressiveRow()
    {
    }

    /// <summary>
    /// Stores the zero-based row index in the image.
    /// </summary>
    public int RowIndex;
    /// <summary>
    /// Stores the decoded row as a one-row pixel buffer in the requested output format.
    /// </summary>
    public PngPixelBuffer Pixels = new PngPixelBuffer();
}

/// <summary>
/// Describes completion of one Adam7 pass, including empty passes.
/// </summary>
public class PngAdam7PassUpdate
{
    /// <summary>
    /// Initializes an empty Adam7 pass update.
    /// </summary>
    public PngAdam7PassUpdate()
    {
    }

    /// <summary>
    /// Stores the one-based Adam7 pass index.
    /// </summary>
    public int PassIndex;
    /// <summary>
    /// Stores the number of pixels in each pass row.
    /// </summary>
    public int Width;
    /// <summary>
    /// Stores the number of rows in the pass.
    /// </summary>
    public int Height;
    /// <summary>
    /// Stores the number of reconstructed rows in the completed pass.
    /// </summary>
    public int RowsDecoded;
    /// <summary>
    /// Indicates that the pass contains no pixels for the image dimensions.
    /// </summary>
    public bool IsEmpty;
}

/// <summary>
/// Stores an optional cumulative image preview produced during progressive decoding.
/// </summary>
public class PngProgressivePreview
{
    /// <summary>
    /// Initializes an empty progressive preview.
    /// </summary>
    public PngProgressivePreview()
    {
    }

    /// <summary>
    /// Stores zero for a non-interlaced row preview or the one-based Adam7 pass index.
    /// </summary>
    public int PassIndex;
    /// <summary>
    /// Stores the one-based update number within the progressive decode.
    /// </summary>
    public int UpdateIndex;
    /// <summary>
    /// Stores the cumulative image pixels in the requested output format.
    /// </summary>
    public PngPixelBuffer Pixels = new PngPixelBuffer();
}
