namespace ttf;

/// <summary>Reports which SFNT tables are present or omitted in subset output.</summary>
public class FontSubsetTableReport : IFontSubsetTableReport
{
    int[] retainedTableTags;
    int[] droppedTableTags;
    int[] rewrittenTableTags;
    int[] preservedTableTags;

    internal FontSubsetTableReport(
        int[] retainedTableTags,
        int[] droppedTableTags,
        int[] rewrittenTableTags,
        int[] preservedTableTags)
    {
        this.retainedTableTags = CopyIntegers(retainedTableTags);
        this.droppedTableTags = CopyIntegers(droppedTableTags);
        this.rewrittenTableTags = CopyIntegers(rewrittenTableTags);
        this.preservedTableTags = CopyIntegers(preservedTableTags);
    }

    /// <summary>Copies the sorted tags present in the emitted font.</summary>
    /// <returns>A new array of big-endian table tags.</returns>
    public int[] GetRetainedTableTags()
    {
        return CopyIntegers(retainedTableTags);
    }

    /// <summary>Copies the sorted source tags omitted from the emitted font.</summary>
    /// <returns>A new array of big-endian table tags.</returns>
    public int[] GetDroppedTableTags()
    {
        return CopyIntegers(droppedTableTags);
    }

    /// <summary>Copies emitted tags whose data was generated or changed.</summary>
    /// <returns>A new sorted array of big-endian table tags.</returns>
    public int[] GetRewrittenTableTags()
    {
        return CopyIntegers(rewrittenTableTags);
    }

    /// <summary>Copies emitted tags whose source table bytes were unchanged.</summary>
    /// <returns>A new sorted array of big-endian table tags.</returns>
    public int[] GetPreservedTableTags()
    {
        return CopyIntegers(preservedTableTags);
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
