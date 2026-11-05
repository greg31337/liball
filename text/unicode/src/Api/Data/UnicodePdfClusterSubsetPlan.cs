namespace unicode;

/// <summary>
/// Describes the font coverage decision for one complete grapheme cluster.
/// </summary>
public class UnicodePdfClusterSubsetPlan
{
    /// <summary>The inclusive UTF-16 start index.</summary>
    public int StartUtf16Index;
    /// <summary>The exclusive UTF-16 end index.</summary>
    public int EndUtf16Index;
    /// <summary>The UTF-16 length of the cluster.</summary>
    public int Utf16Length;
    /// <summary>The resolved script used for font planning.</summary>
    public UnicodeScript Script;
    /// <summary>The distinct scalars required by the cluster.</summary>
    public int[] Scalars;
    /// <summary>Whether one font covers every scalar in the cluster.</summary>
    public bool IsCovered;

    /// <summary>
    /// Initializes a cluster coverage plan.
    /// </summary>
    /// <param name="startUtf16Index">The inclusive UTF-16 start index.</param>
    /// <param name="endUtf16Index">The exclusive UTF-16 end index.</param>
    /// <param name="script">The resolved layout script.</param>
    /// <param name="scalars">The distinct cluster scalars.</param>
    /// <param name="isCovered">Whether every cluster scalar is covered.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the start index is negative or a scalar is invalid.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the end precedes the start.</exception>
    public UnicodePdfClusterSubsetPlan(
        int startUtf16Index,
        int endUtf16Index,
        UnicodeScript script,
        int[] scalars,
        bool isCovered)
    {
        if (startUtf16Index < 0)
        {
            throw new System.ArgumentOutOfRangeException("startUtf16Index");
        }

        if (endUtf16Index < startUtf16Index)
        {
            throw new System.ArgumentException("Cluster end must be greater than or equal to cluster start.");
        }

        UnicodePdfFontSubset.ValidateScalars(scalars);
        StartUtf16Index = startUtf16Index;
        EndUtf16Index = endUtf16Index;
        Utf16Length = endUtf16Index - startUtf16Index;
        Script = script;
        Scalars = UnicodePdfFontSubset.CopyScalars(scalars);
        IsCovered = isCovered;
    }
}
