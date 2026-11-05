using System.Text;

namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="NormalizedFontCoordinates"/>.</summary>
public interface INormalizedFontCoordinates
{
    /// <summary>Gets the number of axes in the face's canonical axis order.</summary>
    /// <returns>The axis count.</returns>
    int GetAxisCount();

    /// <summary>Gets one big-endian variation-axis tag.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The axis tag.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    int GetAxisTag(int index);

    /// <summary>Gets one canonical, clamped user-scale coordinate.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The 16.16-canonical user coordinate.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    double GetUserValue(int index);

    /// <summary>Gets one final normalized coordinate after <c>avar</c> mapping.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The exact F2Dot14 normalized value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    double GetNormalizedValue(int index);

    /// <summary>Gets the clamping and normalization report for one axis.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The immutable per-axis mapping report.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    FontCoordinateMapping GetMapping(int index);

    /// <summary>Gets the number of explicitly requested coordinates that were clamped.</summary>
    /// <returns>The clamped-axis count.</returns>
    int GetClampedAxisCount();

    /// <summary>Gets the deterministic key for the final normalized location.</summary>
    /// <returns>The invariant axis-order coordinate key.</returns>
    string GetKey();

    /// <summary>Reports whether this location exactly selects a named instance.</summary>
    /// <returns>True when a named instance was selected; otherwise false.</returns>
    bool HasNamedInstance();

    /// <summary>Gets the selected <c>fvar</c> named-instance index.</summary>
    /// <returns>The first matching named-instance index, or -1.</returns>
    int GetNamedInstanceIndex();
}
