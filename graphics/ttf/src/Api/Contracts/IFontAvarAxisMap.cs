namespace ttf;

/// <summary>Defines one <c>avar</c> axis segment map.</summary>
public interface IFontAvarAxisMap
{
    /// <summary>Gets the mapped axis tag.</summary>
    /// <returns>The big-endian axis tag.</returns>
    int GetAxisTag();

    /// <summary>Gets the number of mapping control points.</summary>
    /// <returns>The control-point count.</returns>
    int GetValueMapCount();

    /// <summary>Gets one mapping control point.</summary>
    /// <param name="index">The zero-based point index.</param>
    /// <returns>The control point.</returns>
    FontAvarValueMap GetValueMap(int index);

    /// <summary>Reports whether the required identity anchors are present.</summary>
    /// <returns>True when the segment map is active; otherwise false.</returns>
    bool HasRequiredMappings();

    /// <summary>Maps one normalized coordinate through the segment map.</summary>
    /// <param name="value">A finite normalized coordinate from -1 through 1.</param>
    /// <returns>The mapped normalized coordinate.</returns>
    double Map(double value);
}
