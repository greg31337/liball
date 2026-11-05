namespace ttf;

/// <summary>Defines one caller-selected feature value over a UTF-16 range.</summary>
public interface IFontLayoutFeatureSetting
{
    /// <summary>Gets the feature tag.</summary>
    /// <returns>The big-endian feature tag.</returns>
    int GetTag();

    /// <summary>Gets the nonnegative feature value.</summary>
    /// <returns>The feature value.</returns>
    int GetValue();

    /// <summary>Gets the inclusive UTF-16 start offset.</summary>
    /// <returns>The start offset.</returns>
    int GetStart();

    /// <summary>Gets the exclusive UTF-16 end offset.</summary>
    /// <returns>The end offset.</returns>
    int GetEnd();
}
