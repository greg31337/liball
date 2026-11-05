namespace svg;

/// <summary>Assigns a language to one UTF-16 source interval.</summary>
public class SvgLanguageSpan
{
    /// <summary>The zero-based UTF-16 start index.</summary>
    public int Start;
    /// <summary>The nonnegative UTF-16 code-unit length.</summary>
    public int Length;
    /// <summary>The nonempty caller-supplied language tag.</summary>
    public string Language;

    /// <summary>Creates one language interval.</summary>
    /// <param name="start">The zero-based UTF-16 start index.</param>
    /// <param name="length">The nonnegative UTF-16 code-unit length.</param>
    /// <param name="language">The nonempty language tag.</param>
    public SvgLanguageSpan(int start, int length, string language)
    {
        if (start < 0) throw new System.ArgumentOutOfRangeException("start");
        if (length < 0) throw new System.ArgumentOutOfRangeException("length");
        if (language == null || language.Trim().Length == 0) throw new System.ArgumentOutOfRangeException("language");
        Start = start;
        Length = length;
        Language = language.Trim();
    }
}

/// <summary>Configures bounded Unicode scalar, grapheme, script, and language analysis.</summary>
public class SvgUnicodeTextOptions
{
    /// <summary>The language assigned outside explicit spans.</summary>
    public string DefaultLanguage;
    /// <summary>Ordered language spans; later containing spans take precedence.</summary>
    public System.Collections.Generic.List<SvgLanguageSpan> LanguageSpans;
    /// <summary>The maximum decoded scalar count.</summary>
    public int MaxScalars;
    /// <summary>The maximum extended grapheme cluster count.</summary>
    public int MaxClusters;
    /// <summary>The maximum script, language, or combined item-run count.</summary>
    public int MaxRuns;
    /// <summary>The cancellation token observed during bounded loops.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates secure Unicode text analysis defaults.</summary>
    public SvgUnicodeTextOptions()
    {
        DefaultLanguage = "und";
        LanguageSpans = new System.Collections.Generic.List<SvgLanguageSpan>();
        MaxScalars = 1000000;
        MaxClusters = 1000000;
        MaxRuns = 1000000;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains one decoded Unicode scalar and exact UTF-16 source mapping.</summary>
public class SvgUnicodeScalar
{
    /// <summary>The stable zero-based scalar index.</summary>
    public int Index;
    /// <summary>The Unicode scalar value.</summary>
    public int Value;
    /// <summary>The zero-based UTF-16 source index.</summary>
    public int Utf16Start;
    /// <summary>The UTF-16 code-unit length, one or two.</summary>
    public int Utf16Length;
    /// <summary>Whether an unpaired surrogate was replaced by U+FFFD.</summary>
    public bool IsReplacement;
    /// <summary>Whether this scalar is a standardized variation selector.</summary>
    public bool IsVariationSelector;
    /// <summary>Whether this scalar is a non-variation combining mark.</summary>
    public bool IsCombiningMark;
    /// <summary>Whether this scalar is U+200C or U+200D.</summary>
    public bool IsJoinControl;
    /// <summary>The Unicode general category.</summary>
    public System.Globalization.UnicodeCategory Category;
    /// <summary>The Unicode Script property name or <c>Unknown</c>.</summary>
    public string Script;
    /// <summary>The containing extended grapheme cluster index.</summary>
    public int ClusterIndex;
    /// <summary>The preceding non-selector scalar index for a variation selector, or minus one.</summary>
    public int VariationBaseIndex;

    internal SvgUnicodeScalar()
    {
        Index = -1;
        Value = 0;
        Utf16Start = 0;
        Utf16Length = 1;
        IsReplacement = false;
        IsVariationSelector = false;
        IsCombiningMark = false;
        IsJoinControl = false;
        Category = System.Globalization.UnicodeCategory.OtherNotAssigned;
        Script = "Unknown";
        ClusterIndex = -1;
        VariationBaseIndex = -1;
    }
}

/// <summary>Contains one Unicode extended grapheme cluster and its scalar mapping.</summary>
public class SvgGraphemeCluster
{
    /// <summary>The stable zero-based cluster index.</summary>
    public int Index;
    /// <summary>The zero-based UTF-16 source index.</summary>
    public int Utf16Start;
    /// <summary>The UTF-16 code-unit length.</summary>
    public int Utf16Length;
    /// <summary>The first contained scalar index.</summary>
    public int ScalarStart;
    /// <summary>The contained scalar count.</summary>
    public int ScalarCount;
    /// <summary>The first base scalar index, or minus one for a mark-only cluster.</summary>
    public int BaseScalarIndex;
    /// <summary>The contained variation-selector scalar indices.</summary>
    public System.Collections.Generic.List<int> VariationSelectorIndices;
    /// <summary>Whether the cluster contains a non-variation combining mark.</summary>
    public bool HasCombiningMarks;
    /// <summary>Whether the cluster contains an emoji or emoji-presentation sequence.</summary>
    public bool IsEmojiSequence;
    /// <summary>The resolved Unicode Script property name.</summary>
    public string Script;
    /// <summary>The resolved caller language tag.</summary>
    public string Language;
    /// <summary>The normalized cluster text with invalid scalars replaced.</summary>
    public string Text;

    internal SvgGraphemeCluster()
    {
        Index = -1;
        Utf16Start = 0;
        Utf16Length = 0;
        ScalarStart = 0;
        ScalarCount = 0;
        BaseScalarIndex = -1;
        VariationSelectorIndices = new System.Collections.Generic.List<int>();
        HasCombiningMarks = false;
        IsEmojiSequence = false;
        Script = "Common";
        Language = "und";
        Text = "";
    }
}

/// <summary>Contains one contiguous script, language, or combined text item run.</summary>
public class SvgTextItemRun
{
    /// <summary>The stable zero-based run index.</summary>
    public int Index;
    /// <summary>The zero-based UTF-16 source index.</summary>
    public int Utf16Start;
    /// <summary>The UTF-16 code-unit length.</summary>
    public int Utf16Length;
    /// <summary>The first contained scalar index.</summary>
    public int ScalarStart;
    /// <summary>The contained scalar count.</summary>
    public int ScalarCount;
    /// <summary>The first contained grapheme cluster index.</summary>
    public int ClusterStart;
    /// <summary>The contained grapheme cluster count.</summary>
    public int ClusterCount;
    /// <summary>The resolved Unicode Script property name.</summary>
    public string Script;
    /// <summary>The resolved caller language tag.</summary>
    public string Language;

    internal SvgTextItemRun()
    {
        Index = -1;
        Utf16Start = 0;
        Utf16Length = 0;
        ScalarStart = 0;
        ScalarCount = 0;
        ClusterStart = 0;
        ClusterCount = 0;
        Script = "Common";
        Language = "und";
    }
}

/// <summary>Iterates a stable Unicode scalar snapshot without implementing collection interfaces.</summary>
public class SvgUnicodeScalarIterator
{
    /// <summary>The current scalar, or <c>null</c> before and after iteration.</summary>
    public SvgUnicodeScalar? Current;
    /// <summary>The current zero-based scalar index, or minus one.</summary>
    public int Index;
    System.Collections.Generic.List<SvgUnicodeScalar> scalars;

    internal SvgUnicodeScalarIterator(System.Collections.Generic.List<SvgUnicodeScalar> values)
    {
        scalars = values;
        Current = null;
        Index = -1;
    }

    /// <summary>Advances to the next scalar.</summary>
    /// <returns><c>true</c> when <see cref="Current"/> contains a scalar.</returns>
    internal bool MoveNext()
    {
        int next = Index + 1;
        if (next >= scalars.Count)
        {
            Index = scalars.Count;
            Current = null;
            return false;
        }
        Index = next;
        Current = scalars[Index];
        return true;
    }

    /// <summary>Resets iteration to before the first scalar.</summary>
    internal void Reset()
    {
        Index = -1;
        Current = null;
    }
}

/// <summary>Iterates a stable extended grapheme cluster snapshot without implementing collection interfaces.</summary>
public class SvgGraphemeIterator
{
    /// <summary>The current cluster, or <c>null</c> before and after iteration.</summary>
    public SvgGraphemeCluster? Current;
    /// <summary>The current zero-based cluster index, or minus one.</summary>
    public int Index;
    System.Collections.Generic.List<SvgGraphemeCluster> clusters;

    internal SvgGraphemeIterator(System.Collections.Generic.List<SvgGraphemeCluster> values)
    {
        clusters = values;
        Current = null;
        Index = -1;
    }

    /// <summary>Advances to the next extended grapheme cluster.</summary>
    /// <returns><c>true</c> when <see cref="Current"/> contains a cluster.</returns>
    internal bool MoveNext()
    {
        int next = Index + 1;
        if (next >= clusters.Count)
        {
            Index = clusters.Count;
            Current = null;
            return false;
        }
        Index = next;
        Current = clusters[Index];
        return true;
    }

    /// <summary>Resets iteration to before the first cluster.</summary>
    internal void Reset()
    {
        Index = -1;
        Current = null;
    }
}

/// <summary>Contains normalized Unicode text plus scalar, grapheme, script, language, and item mappings.</summary>
public class SvgUnicodeText
{
    /// <summary>The original caller text, including invalid UTF-16 code units.</summary>
    public string SourceText;
    /// <summary>The offset-stable text with every unpaired surrogate replaced by U+FFFD.</summary>
    public string Text;
    /// <summary>The decoded scalars in logical order.</summary>
    public System.Collections.Generic.List<SvgUnicodeScalar> Scalars;
    /// <summary>The extended grapheme clusters in logical order.</summary>
    public System.Collections.Generic.List<SvgGraphemeCluster> Clusters;
    /// <summary>The contiguous resolved-script runs.</summary>
    public System.Collections.Generic.List<SvgTextItemRun> ScriptRuns;
    /// <summary>The contiguous caller-language runs.</summary>
    public System.Collections.Generic.List<SvgTextItemRun> LanguageRuns;
    /// <summary>The contiguous combined script-and-language runs.</summary>
    public System.Collections.Generic.List<SvgTextItemRun> ItemRuns;
    /// <summary>The number of invalid UTF-16 code units replaced by U+FFFD.</summary>
    public int ReplacementCount;

    internal SvgUnicodeText()
    {
        SourceText = "";
        Text = "";
        Scalars = new System.Collections.Generic.List<SvgUnicodeScalar>();
        Clusters = new System.Collections.Generic.List<SvgGraphemeCluster>();
        ScriptRuns = new System.Collections.Generic.List<SvgTextItemRun>();
        LanguageRuns = new System.Collections.Generic.List<SvgTextItemRun>();
        ItemRuns = new System.Collections.Generic.List<SvgTextItemRun>();
        ReplacementCount = 0;
    }

    /// <summary>Creates an iterator over the scalar snapshot.</summary>
    /// <returns>A scalar iterator positioned before the first item.</returns>
    internal SvgUnicodeScalarIterator CreateScalarIterator()
    {
        return new SvgUnicodeScalarIterator(Scalars);
    }

    /// <summary>Creates an iterator over the extended grapheme cluster snapshot.</summary>
    /// <returns>A cluster iterator positioned before the first item.</returns>
    internal SvgGraphemeIterator CreateGraphemeIterator()
    {
        return new SvgGraphemeIterator(Clusters);
    }

    /// <summary>Finds the scalar containing one UTF-16 source index.</summary>
    /// <param name="utf16Index">The zero-based UTF-16 index.</param>
    /// <returns>The containing scalar, or <c>null</c>.</returns>
    internal SvgUnicodeScalar? FindScalar(int utf16Index)
    {
        if (utf16Index < 0 || utf16Index >= SourceText.Length) return null;
        int index = 0;
        while (index < Scalars.Count)
        {
            SvgUnicodeScalar scalar = Scalars[index];
            if (utf16Index >= scalar.Utf16Start && utf16Index < scalar.Utf16Start + scalar.Utf16Length) return scalar;
            index = index + 1;
        }
        return null;
    }

    /// <summary>Finds the extended grapheme cluster containing one UTF-16 source index.</summary>
    /// <param name="utf16Index">The zero-based UTF-16 index.</param>
    /// <returns>The containing cluster, or <c>null</c>.</returns>
    internal SvgGraphemeCluster? FindCluster(int utf16Index)
    {
        if (utf16Index < 0 || utf16Index >= SourceText.Length) return null;
        int index = 0;
        while (index < Clusters.Count)
        {
            SvgGraphemeCluster cluster = Clusters[index];
            if (utf16Index >= cluster.Utf16Start && utf16Index < cluster.Utf16Start + cluster.Utf16Length) return cluster;
            index = index + 1;
        }
        return null;
    }
}

/// <summary>Contains a completed or failed Unicode text analysis.</summary>
public class SvgUnicodeTextResult
{
    /// <summary>Whether all analysis completed within configured bounds.</summary>
    public bool Success;
    /// <summary>The stable analysis outcome.</summary>
    public SvgUnicodeTextError Error;
    /// <summary>The complete or partially populated text model.</summary>
    public SvgUnicodeText UnicodeText;
    /// <summary>A deterministic failure explanation, or empty string.</summary>
    public string Message;

    internal SvgUnicodeTextResult()
    {
        Success = false;
        Error = SvgUnicodeTextError.None;
        UnicodeText = new SvgUnicodeText();
        Message = "";
    }
}
