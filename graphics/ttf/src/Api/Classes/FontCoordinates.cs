using System.Text;

namespace ttf;

/// <summary>Collects user-scale coordinates for a variable font instance.</summary>
public class FontCoordinates : IFontCoordinates
{
    int[] axisTags;
    double[] values;

    /// <summary>Creates an empty coordinate collection.</summary>
    public FontCoordinates()
    {
        axisTags = new int[0];
        values = new double[0];
    }

    /// <summary>Gets the number of explicitly supplied axis coordinates.</summary>
    /// <returns>The coordinate count.</returns>
    public int GetCount()
    {
        return axisTags.Length;
    }

    /// <summary>Reports whether a coordinate was supplied for an axis.</summary>
    /// <param name="axisTag">The big-endian OpenType variation-axis tag.</param>
    /// <returns>True when the axis has an explicit value; otherwise false.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>axisTag</c> is not a valid variation-axis tag.
    /// </exception>
    public bool Contains(int axisTag)
    {
        SfntCoordinateResolver.ValidateUserAxisTag(axisTag);
        return FindIndex(axisTag) >= 0;
    }

    /// <summary>Sets or replaces one user-scale axis coordinate.</summary>
    /// <param name="axisTag">The big-endian OpenType variation-axis tag.</param>
    /// <param name="value">The finite user-scale coordinate.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>axisTag</c> is invalid or <c>value</c> is not finite.
    /// </exception>
    public void Set(int axisTag, double value)
    {
        SfntCoordinateResolver.ValidateUserAxisTag(axisTag);

        if (!double.IsFinite(value))
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        int index = FindIndex(axisTag);

        if (index >= 0)
        {
            values[index] = value;
            return;
        }

        int[] expandedTags = new int[axisTags.Length + 1];
        double[] expandedValues = new double[values.Length + 1];
        axisTags.CopyTo(expandedTags, 0);
        values.CopyTo(expandedValues, 0);
        expandedTags[axisTags.Length] = axisTag;
        expandedValues[values.Length] = value;
        axisTags = expandedTags;
        values = expandedValues;
    }

    /// <summary>Gets one explicitly supplied user-scale coordinate.</summary>
    /// <param name="axisTag">The big-endian OpenType variation-axis tag.</param>
    /// <returns>The supplied coordinate.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>axisTag</c> is not a valid variation-axis tag.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// No coordinate was supplied for <c>axisTag</c>.
    /// </exception>
    public double Get(int axisTag)
    {
        SfntCoordinateResolver.ValidateUserAxisTag(axisTag);
        int index = FindIndex(axisTag);

        if (index < 0)
        {
            throw new System.ArgumentException(
                "The requested variation-axis coordinate is not present.",
                "axisTag");
        }

        return values[index];
    }

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
    public NormalizedFontCoordinates Normalize(FontFace face)
    {
        return SfntCoordinateResolver.Resolve(face, this);
    }

    internal int GetAxisTag(int index)
    {
        return axisTags[index];
    }

    internal double GetValue(int index)
    {
        return values[index];
    }

    int FindIndex(int axisTag)
    {
        int index = 0;

        while (index < axisTags.Length)
        {
            if (axisTags[index] == axisTag)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }
}
