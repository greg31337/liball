namespace ttf;

/// <summary>Defines one deterministic font-family and face request.</summary>
/// <remarks>
/// Weight uses the OpenType/CSS range from 1 through 1000. Stretch uses the
/// percentage scale of the OpenType <c>wdth</c> axis, where 100 is normal.
/// Explicit variation coordinates override automatically derived standard-axis
/// coordinates and require a face that defines every supplied axis.
/// </remarks>
public class FontRequest : IFontRequest
{
    string familyName;
    string normalizedFamilyName;
    FontStyle style;
    int weight;
    double stretch;
    bool hasOpticalSize;
    double opticalSize;
    FontCoordinates? coordinates;
    bool genericFamilyRequest;
    FontGenericFamily genericFamily;

    /// <summary>Creates a normal, weight-400, 100-percent-stretch family request.</summary>
    /// <param name="familyName">A localized typographic family name.</param>
    /// <exception cref="System.ArgumentNullException"><c>familyName</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>familyName</c> is empty or whitespace.</exception>
    public FontRequest(string familyName)
    {
        if (familyName == null)
        {
            throw new System.ArgumentNullException("familyName");
        }

        string normalized = FontFamilyNameResolver.NormalizeFamilyName(familyName);

        if (normalized.Length == 0)
        {
            throw new System.ArgumentException(
                "A font family name cannot be empty or whitespace.",
                "familyName");
        }

        this.familyName = familyName;
        normalizedFamilyName = normalized;
        InitializeDefaults();
    }

    /// <summary>Creates a request for one caller-configured generic family.</summary>
    /// <param name="genericFamily">The generic family identifier.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>genericFamily</c> is undefined.
    /// </exception>
    /// <returns>A new generic-family request.</returns>
    internal static FontRequest CreateGeneric(FontGenericFamily genericFamily)
    {
        FontFallbackConfiguration.ValidateGenericFamily(genericFamily);
        FontRequest request = new FontRequest(
            FontFallbackConfiguration.GetGenericFamilyName(genericFamily));
        request.genericFamilyRequest = true;
        request.genericFamily = genericFamily;
        return request;
    }

    void InitializeDefaults()
    {
        style = FontStyle.Normal;
        weight = 400;
        stretch = 100;
        hasOpticalSize = false;
        opticalSize = 0;
        coordinates = new FontCoordinates();
        genericFamilyRequest = false;
        genericFamily = FontGenericFamily.Serif;
    }

    /// <summary>Gets the requested family name.</summary>
    /// <returns>The caller-supplied family name.</returns>
    public string GetFamilyName()
    {
        return familyName;
    }

    /// <summary>Reports whether this request uses a configured generic family.</summary>
    /// <returns>True for a generic-family request.</returns>
    public bool HasGenericFamily()
    {
        return genericFamilyRequest;
    }

    /// <summary>Gets the requested generic family.</summary>
    /// <returns>The generic family identifier.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// This is a concrete family-name request.
    /// </exception>
    public FontGenericFamily GetGenericFamily()
    {
        if (!genericFamilyRequest)
        {
            throw new System.InvalidOperationException(
                "This request does not use a generic family.");
        }

        return genericFamily;
    }

    /// <summary>Gets the requested style.</summary>
    /// <returns>The requested style.</returns>
    public FontStyle GetStyle()
    {
        return style;
    }

    /// <summary>Sets the requested style.</summary>
    /// <param name="style">The upright or slanted style.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>style</c> is undefined.</exception>
    public void SetStyle(FontStyle style)
    {
        if (style != FontStyle.Normal &&
            style != FontStyle.Italic &&
            style != FontStyle.Oblique)
        {
            throw new System.ArgumentOutOfRangeException("style");
        }

        this.style = style;
    }

    /// <summary>Gets the requested OpenType/CSS weight.</summary>
    /// <returns>The value from 1 through 1000.</returns>
    public int GetWeight()
    {
        return weight;
    }

    /// <summary>Sets the requested OpenType/CSS weight.</summary>
    /// <param name="weight">The value from 1 through 1000.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>weight</c> is outside 1 through 1000.
    /// </exception>
    public void SetWeight(int weight)
    {
        if (weight < 1 || weight > 1000)
        {
            throw new System.ArgumentOutOfRangeException("weight");
        }

        this.weight = weight;
    }

    /// <summary>Gets the requested stretch percentage.</summary>
    /// <returns>The positive finite stretch percentage.</returns>
    public double GetStretch()
    {
        return stretch;
    }

    /// <summary>Sets the requested stretch percentage.</summary>
    /// <param name="stretch">A positive finite percentage where 100 is normal.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>stretch</c> is not positive and finite.
    /// </exception>
    public void SetStretch(double stretch)
    {
        if (!double.IsFinite(stretch) || stretch <= 0)
        {
            throw new System.ArgumentOutOfRangeException("stretch");
        }

        this.stretch = stretch;
    }

    /// <summary>Reports whether an optical point size was requested.</summary>
    /// <returns>True when an optical size is present.</returns>
    public bool HasOpticalSize()
    {
        return hasOpticalSize;
    }

    /// <summary>Gets the requested optical point size.</summary>
    /// <returns>The nonnegative finite optical size.</returns>
    /// <exception cref="System.InvalidOperationException">No optical size was set.</exception>
    public double GetOpticalSize()
    {
        if (!hasOpticalSize)
        {
            throw new System.InvalidOperationException("No optical size was requested.");
        }

        return opticalSize;
    }

    /// <summary>Sets the requested optical point size.</summary>
    /// <param name="opticalSize">The nonnegative finite point size.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>opticalSize</c> is negative or non-finite.
    /// </exception>
    public void SetOpticalSize(double opticalSize)
    {
        if (!double.IsFinite(opticalSize) || opticalSize < 0)
        {
            throw new System.ArgumentOutOfRangeException("opticalSize");
        }

        this.opticalSize = opticalSize;
        hasOpticalSize = true;
    }

    /// <summary>Clears the requested optical point size.</summary>
    public void ClearOpticalSize()
    {
        opticalSize = 0;
        hasOpticalSize = false;
    }

    /// <summary>Sets or replaces one explicit variation coordinate.</summary>
    /// <param name="axisTag">The big-endian OpenType variation-axis tag.</param>
    /// <param name="value">The finite user-scale coordinate.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>axisTag</c> is invalid or <c>value</c> is not finite.
    /// </exception>
    public void SetCoordinate(int axisTag, double value)
    {
        System.ArgumentNullException.ThrowIfNull(coordinates);
        coordinates.Set(axisTag, value);
    }

    /// <summary>Gets the number of explicit variation coordinates.</summary>
    /// <returns>The coordinate count.</returns>
    public int GetCoordinateCount()
    {
        System.ArgumentNullException.ThrowIfNull(coordinates);
        return coordinates.GetCount();
    }

    /// <summary>Reports whether an explicit axis coordinate is present.</summary>
    /// <param name="axisTag">The big-endian OpenType variation-axis tag.</param>
    /// <returns>True when the coordinate is present.</returns>
    public bool HasCoordinate(int axisTag)
    {
        System.ArgumentNullException.ThrowIfNull(coordinates);
        return coordinates.Contains(axisTag);
    }

    /// <summary>Gets one explicit variation coordinate.</summary>
    /// <param name="axisTag">The big-endian OpenType variation-axis tag.</param>
    /// <returns>The caller-supplied coordinate.</returns>
    /// <exception cref="System.ArgumentException">The coordinate is absent.</exception>
    public double GetCoordinate(int axisTag)
    {
        System.ArgumentNullException.ThrowIfNull(coordinates);
        return coordinates.Get(axisTag);
    }

    internal string GetNormalizedFamilyName()
    {
        return normalizedFamilyName;
    }

    internal FontCoordinates? GetCoordinates()
    {
        return coordinates;
    }

    internal FontRequest Copy()
    {
        FontRequest copy = new FontRequest(familyName);
        copy.normalizedFamilyName = normalizedFamilyName;
        copy.style = style;
        copy.weight = weight;
        copy.stretch = stretch;
        copy.hasOpticalSize = hasOpticalSize;
        copy.opticalSize = opticalSize;
        copy.genericFamilyRequest = genericFamilyRequest;
        copy.genericFamily = genericFamily;
        int index = 0;

        System.ArgumentNullException.ThrowIfNull(coordinates);
        while (index < coordinates.GetCount())
        {
            System.ArgumentNullException.ThrowIfNull(copy.coordinates);
            copy.coordinates.Set(
                coordinates.GetAxisTag(index),
                coordinates.GetValue(index));
            index += 1;
        }

        return copy;
    }

    internal FontRequest CopyForFamily(string concreteFamilyName)
    {
        FontRequest copy = new FontRequest(concreteFamilyName);
        copy.style = style;
        copy.weight = weight;
        copy.stretch = stretch;
        copy.hasOpticalSize = hasOpticalSize;
        copy.opticalSize = opticalSize;
        int index = 0;

        System.ArgumentNullException.ThrowIfNull(coordinates);
        while (index < coordinates.GetCount())
        {
            System.ArgumentNullException.ThrowIfNull(copy.coordinates);
            copy.coordinates.Set(
                coordinates.GetAxisTag(index),
                coordinates.GetValue(index));
            index += 1;
        }

        return copy;
    }
}
