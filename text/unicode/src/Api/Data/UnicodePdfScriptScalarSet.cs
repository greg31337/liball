namespace unicode;

/// <summary>
/// Groups the distinct Unicode scalar values needed for one script in a PDF font subset.
/// </summary>
public class UnicodePdfScriptScalarSet
{
    /// <summary>
    /// The script assigned to this run or scalar set.
    /// </summary>
    public UnicodeScript Script;
    /// <summary>
    /// The distinct Unicode scalar values assigned to the script.
    /// </summary>
    public int[] Scalars;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="script">The Unicode script to select.</param>
    /// <param name="scalars">The Unicode scalars to include or inspect.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>scalars</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>scalars</c> contains a value that is not a Unicode scalar.</exception>
    public UnicodePdfScriptScalarSet(UnicodeScript script, int[] scalars)
    {
        UnicodePdfFontSubset.ValidateScalars(scalars);

        Script = script;
        Scalars = UnicodePdfFontSubset.CopyScalars(scalars);
    }

    /// <summary>
    /// Returns the number of code points, mappings, scalars, or UTF-16 positions represented by this value.
    /// </summary>
    /// <returns>The resulting integer value.</returns>
    internal int Count()
    {
        return Scalars.Length;
    }

    /// <summary>
    /// Reports whether the set contains the specified Unicode scalar.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool ContainsScalar(int codePoint)
    {
        return UnicodePdfFontSubset.ContainsScalar(Scalars, codePoint);
    }

    /// <summary>
    /// Returns a defensive copy of the scalars in this script set.
    /// </summary>
    /// <returns>A new array containing the resulting values.</returns>
    internal int[] GetScalars()
    {
        return UnicodePdfFontSubset.CopyScalars(Scalars);
    }
}
