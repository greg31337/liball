namespace unicode;

/// <summary>
/// Describes one grapheme-safe PDF layout cluster.
/// </summary>
public class UnicodePdfLayoutCluster
{
    /// <summary>The inclusive UTF-16 start index.</summary>
    public int StartUtf16Index;
    /// <summary>The exclusive UTF-16 end index.</summary>
    public int EndUtf16Index;
    /// <summary>The UTF-16 length.</summary>
    public int Utf16Length;
    /// <summary>The resolved script.</summary>
    public UnicodeScript Script;
    /// <summary>The resolved simple bidi direction.</summary>
    public UnicodePdfBidiDirection Direction;
    /// <summary>The estimated display-column width.</summary>
    public int DisplayWidth;

    /// <summary>
    /// Initializes a PDF layout cluster.
    /// </summary>
    /// <param name="startUtf16Index">The inclusive UTF-16 start index.</param>
    /// <param name="endUtf16Index">The exclusive UTF-16 end index.</param>
    /// <param name="script">The resolved script.</param>
    /// <param name="direction">The resolved direction.</param>
    /// <param name="displayWidth">The estimated display width.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an index or width is negative.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the end precedes the start.</exception>
    public UnicodePdfLayoutCluster(
        int startUtf16Index,
        int endUtf16Index,
        UnicodeScript script,
        UnicodePdfBidiDirection direction,
        int displayWidth)
    {
        if (startUtf16Index < 0)
        {
            throw new System.ArgumentOutOfRangeException("startUtf16Index");
        }

        if (endUtf16Index < startUtf16Index)
        {
            throw new System.ArgumentException("Cluster end must be greater than or equal to cluster start.");
        }

        if (displayWidth < 0)
        {
            throw new System.ArgumentOutOfRangeException("displayWidth");
        }

        StartUtf16Index = startUtf16Index;
        EndUtf16Index = endUtf16Index;
        Utf16Length = endUtf16Index - startUtf16Index;
        Script = script;
        Direction = direction;
        DisplayWidth = displayWidth;
    }
}
