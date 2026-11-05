using System.Text;

namespace ttf;

/// <summary>Represents immutable clamped and normalized variation coordinates.</summary>
public class NormalizedFontCoordinates : INormalizedFontCoordinates
{
    int[] axisTags;
    double[] userValues;
    double[] normalizedValues;
    int[] encodedNormalizedValues;
    FontCoordinateMapping[] mappings;
    string key;
    int namedInstanceIndex;
    bool defaultUserLocation;

    internal NormalizedFontCoordinates(
        int[] axisTags,
        double[] userValues,
        double[] normalizedValues,
        int[] encodedNormalizedValues,
        string key,
        int namedInstanceIndex,
        bool defaultUserLocation)
    {
        this.axisTags = axisTags;
        this.userValues = userValues;
        this.normalizedValues = normalizedValues;
        this.encodedNormalizedValues = encodedNormalizedValues;
        mappings = CreateDefaultMappings(
            axisTags,
            userValues,
            normalizedValues);
        this.key = key;
        this.namedInstanceIndex = namedInstanceIndex;
        this.defaultUserLocation = defaultUserLocation;
    }

    /// <summary>Gets the number of axes in the face's canonical axis order.</summary>
    /// <returns>The axis count.</returns>
    public int GetAxisCount()
    {
        return axisTags.Length;
    }

    /// <summary>Gets one big-endian variation-axis tag.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The axis tag.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public int GetAxisTag(int index)
    {
        ValidateIndex(index);
        return axisTags[index];
    }

    /// <summary>Gets one canonical, clamped user-scale coordinate.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The 16.16-canonical user coordinate.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public double GetUserValue(int index)
    {
        ValidateIndex(index);
        return userValues[index];
    }

    /// <summary>Gets one final normalized coordinate after <c>avar</c> mapping.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The exact F2Dot14 normalized value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public double GetNormalizedValue(int index)
    {
        ValidateIndex(index);
        return normalizedValues[index];
    }

    /// <summary>Gets the clamping and normalization report for one axis.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The immutable per-axis mapping report.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public FontCoordinateMapping GetMapping(int index)
    {
        ValidateIndex(index);
        return mappings[index];
    }

    /// <summary>Gets the number of explicitly requested coordinates that were clamped.</summary>
    /// <returns>The clamped-axis count.</returns>
    public int GetClampedAxisCount()
    {
        int count = 0;
        int index = 0;

        while (index < mappings.Length)
        {
            if (mappings[index].WasExplicitlySpecified() &&
                mappings[index].WasClamped())
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }

    /// <summary>Gets the deterministic key for the final normalized location.</summary>
    /// <returns>The invariant axis-order coordinate key.</returns>
    public string GetKey()
    {
        return key;
    }

    /// <summary>Reports whether this location exactly selects a named instance.</summary>
    /// <returns>True when a named instance was selected; otherwise false.</returns>
    public bool HasNamedInstance()
    {
        return namedInstanceIndex >= 0;
    }

    /// <summary>Gets the selected <c>fvar</c> named-instance index.</summary>
    /// <returns>The first matching named-instance index, or -1.</returns>
    public int GetNamedInstanceIndex()
    {
        return namedInstanceIndex;
    }

    internal double[] GetNormalizedValues()
    {
        return normalizedValues;
    }

    internal bool IsDefaultUserLocation()
    {
        return defaultUserLocation;
    }

    internal NormalizedFontCoordinates SelectNamedInstance(int index)
    {
        NormalizedFontCoordinates selected = new NormalizedFontCoordinates(
            axisTags,
            userValues,
            normalizedValues,
            encodedNormalizedValues,
            key,
            index,
            defaultUserLocation);
        selected.SetMappings(mappings);
        return selected;
    }

    internal void SetMappings(FontCoordinateMapping[] value)
    {
        mappings = value;
    }

    static FontCoordinateMapping[] CreateDefaultMappings(
        int[] tags,
        double[] users,
        double[] normalized)
    {
        FontCoordinateMapping[] result = new FontCoordinateMapping[tags.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new FontCoordinateMapping(
                tags[index],
                false,
                users[index],
                users[index],
                users[index],
                users[index],
                users[index],
                normalized[index],
                normalized[index]);
            index += 1;
        }
        return result;
    }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= axisTags.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}
