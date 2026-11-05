using System.Text;

namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontCoordinates"/>.</summary>
public interface IFontCoordinates
{
    /// <summary>Gets the number of explicitly supplied axis coordinates.</summary>
    /// <returns>The coordinate count.</returns>
    int GetCount();

    /// <summary>Reports whether a coordinate was supplied for an axis.</summary>
    /// <param name="axisTag">The big-endian OpenType variation-axis tag.</param>
    /// <returns>True when the axis has an explicit value; otherwise false.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>axisTag</c> is not a valid variation-axis tag.
    /// </exception>
    bool Contains(int axisTag);

    /// <summary>Sets or replaces one user-scale axis coordinate.</summary>
    /// <param name="axisTag">The big-endian OpenType variation-axis tag.</param>
    /// <param name="value">The finite user-scale coordinate.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>axisTag</c> is invalid or <c>value</c> is not finite.
    /// </exception>
    void Set(int axisTag, double value);

    /// <summary>Gets one explicitly supplied user-scale coordinate.</summary>
    /// <param name="axisTag">The big-endian OpenType variation-axis tag.</param>
    /// <returns>The supplied coordinate.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>axisTag</c> is not a valid variation-axis tag.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// No coordinate was supplied for <c>axisTag</c>.
    /// </exception>
    double Get(int axisTag);

    /// <summary>Resolves these coordinates for one font face.</summary>
    /// <param name="face">The variable or static font face.</param>
    /// <returns>An immutable normalized coordinate set.</returns>
    /// <exception cref="System.ArgumentNullException"><c>face</c> is null.</exception>
    /// <exception cref="System.ArgumentException">
    /// A supplied axis is not defined by the face.
    /// </exception>
    /// <exception cref="FontFormatException">
    /// Required variation metadata is unsupported or malformed.
    /// </exception>
    NormalizedFontCoordinates Normalize(FontFace face);
}
