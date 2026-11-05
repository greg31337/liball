namespace ttf;

/// <summary>Defines one resolved COLR glyph clip box.</summary>
public interface IColorGlyphClipBox
{
    /// <summary>Gets the source clip-box format.</summary>
    /// <returns>One for static coordinates or two for variable coordinates.</returns>
    int GetSourceFormat();
    /// <summary>Reports whether the clip-box format is supported.</summary>
    /// <returns>True when resolved coordinates are available; otherwise false.</returns>
    bool IsSupported();
    /// <summary>Reports whether the source clip box is variable.</summary>
    /// <returns>True for a variable source clip box; otherwise false.</returns>
    bool IsVariable();
    /// <summary>Gets the minimum x coordinate.</summary>
    /// <returns>The resolved design-unit coordinate.</returns>
    double GetXMinimum();
    /// <summary>Gets the minimum y coordinate.</summary>
    /// <returns>The resolved design-unit coordinate.</returns>
    double GetYMinimum();
    /// <summary>Gets the maximum x coordinate.</summary>
    /// <returns>The resolved design-unit coordinate.</returns>
    double GetXMaximum();
    /// <summary>Gets the maximum y coordinate.</summary>
    /// <returns>The resolved design-unit coordinate.</returns>
    double GetYMaximum();
}
