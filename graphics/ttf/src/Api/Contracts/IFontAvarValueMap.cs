namespace ttf;

/// <summary>Defines one <c>avar</c> segment-map control point.</summary>
public interface IFontAvarValueMap
{
    /// <summary>Gets the input normalized coordinate.</summary>
    /// <returns>The input coordinate.</returns>
    double GetFromCoordinate();

    /// <summary>Gets the output normalized coordinate.</summary>
    /// <returns>The output coordinate.</returns>
    double GetToCoordinate();
}
