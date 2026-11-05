namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontSubsetTableReport"/>.</summary>
public interface IFontSubsetTableReport
{
    /// <summary>Copies the sorted tags present in the emitted font.</summary>
    /// <returns>A new array of big-endian table tags.</returns>
    int[] GetRetainedTableTags();

    /// <summary>Copies the sorted source tags omitted from the emitted font.</summary>
    /// <returns>A new array of big-endian table tags.</returns>
    int[] GetDroppedTableTags();

    /// <summary>Copies emitted tags whose data was generated or changed.</summary>
    /// <returns>A new sorted array of big-endian table tags.</returns>
    int[] GetRewrittenTableTags();

    /// <summary>Copies emitted tags whose source table bytes were unchanged.</summary>
    /// <returns>A new sorted array of big-endian table tags.</returns>
    int[] GetPreservedTableTags();
}
