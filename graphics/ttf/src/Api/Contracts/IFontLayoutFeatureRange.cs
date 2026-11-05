namespace ttf;

/// <summary>Defines one effective feature-value range.</summary>
public interface IFontLayoutFeatureRange
{
    /// <summary>Gets the inclusive UTF-16 start offset.</summary>
    /// <returns>The start offset.</returns>
    int GetStart();

    /// <summary>Gets the exclusive UTF-16 end offset.</summary>
    /// <returns>The end offset.</returns>
    int GetEnd();

    /// <summary>Gets the selected feature value.</summary>
    /// <returns>The nonzero feature value.</returns>
    int GetValue();
}
