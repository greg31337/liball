namespace ttf;

/// <summary>Defines one OpenType layout feature.</summary>
public interface IFontLayoutFeature
{
    /// <summary>Gets the feature-list index.</summary>
    /// <returns>The zero-based feature index.</returns>
    int GetIndex();

    /// <summary>Gets the feature tag.</summary>
    /// <returns>The big-endian feature tag.</returns>
    int GetTag();

    /// <summary>Reports whether feature parameters are present.</summary>
    /// <returns>True when present; otherwise false.</returns>
    bool HasParameters();

    /// <summary>Gets the absolute parameter offset within the layout table.</summary>
    /// <returns>The table-relative offset, or zero when absent.</returns>
    int GetParameterOffset();

    /// <summary>Copies a bounded prefix of the feature-parameter data.</summary>
    /// <param name="length">The requested nonnegative byte count.</param>
    /// <returns>A new byte array.</returns>
    byte[] CopyParameterBytes(int length);

    /// <summary>Gets the referenced lookup count.</summary>
    /// <returns>The lookup-index count.</returns>
    int GetLookupCount();

    /// <summary>Gets one referenced lookup index.</summary>
    /// <param name="index">The zero-based reference index.</param>
    /// <returns>The lookup-list index.</returns>
    int GetLookupIndex(int index);
}
