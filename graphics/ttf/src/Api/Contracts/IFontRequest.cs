namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontRequest"/>.</summary>
public interface IFontRequest
{
    /// <summary>Gets the requested family name.</summary>
    /// <returns>The caller-supplied family name.</returns>
    string GetFamilyName();

    /// <summary>Reports whether this request uses a configured generic family.</summary>
    /// <returns>True for a generic-family request.</returns>
    bool HasGenericFamily();

    /// <summary>Gets the requested generic family.</summary>
    /// <returns>The generic family identifier.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// This is a concrete family-name request.
    /// </exception>
    FontGenericFamily GetGenericFamily();

    /// <summary>Gets the requested style.</summary>
    /// <returns>The requested style.</returns>
    FontStyle GetStyle();

    /// <summary>Sets the requested style.</summary>
    /// <param name="style">The upright or slanted style.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>style</c> is undefined.</exception>
    void SetStyle(FontStyle style);

    /// <summary>Gets the requested OpenType/CSS weight.</summary>
    /// <returns>The value from 1 through 1000.</returns>
    int GetWeight();

    /// <summary>Sets the requested OpenType/CSS weight.</summary>
    /// <param name="weight">The value from 1 through 1000.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>weight</c> is outside 1 through 1000.
    /// </exception>
    void SetWeight(int weight);

    /// <summary>Gets the requested stretch percentage.</summary>
    /// <returns>The positive finite stretch percentage.</returns>
    double GetStretch();

    /// <summary>Sets the requested stretch percentage.</summary>
    /// <param name="stretch">A positive finite percentage where 100 is normal.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>stretch</c> is not positive and finite.
    /// </exception>
    void SetStretch(double stretch);

    /// <summary>Reports whether an optical point size was requested.</summary>
    /// <returns>True when an optical size is present.</returns>
    bool HasOpticalSize();

    /// <summary>Gets the requested optical point size.</summary>
    /// <returns>The nonnegative finite optical size.</returns>
    /// <exception cref="System.InvalidOperationException">No optical size was set.</exception>
    double GetOpticalSize();

    /// <summary>Sets the requested optical point size.</summary>
    /// <param name="opticalSize">The nonnegative finite point size.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>opticalSize</c> is negative or non-finite.
    /// </exception>
    void SetOpticalSize(double opticalSize);

    /// <summary>Clears the requested optical point size.</summary>
    void ClearOpticalSize();

    /// <summary>Sets or replaces one explicit variation coordinate.</summary>
    /// <param name="axisTag">The big-endian OpenType variation-axis tag.</param>
    /// <param name="value">The finite user-scale coordinate.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>axisTag</c> is invalid or <c>value</c> is not finite.
    /// </exception>
    void SetCoordinate(int axisTag, double value);

    /// <summary>Gets the number of explicit variation coordinates.</summary>
    /// <returns>The coordinate count.</returns>
    int GetCoordinateCount();

    /// <summary>Reports whether an explicit axis coordinate is present.</summary>
    /// <param name="axisTag">The big-endian OpenType variation-axis tag.</param>
    /// <returns>True when the coordinate is present.</returns>
    bool HasCoordinate(int axisTag);

    /// <summary>Gets one explicit variation coordinate.</summary>
    /// <param name="axisTag">The big-endian OpenType variation-axis tag.</param>
    /// <returns>The caller-supplied coordinate.</returns>
    /// <exception cref="System.ArgumentException">The coordinate is absent.</exception>
    double GetCoordinate(int axisTag);
}
