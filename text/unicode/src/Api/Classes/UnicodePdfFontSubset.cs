using System.Collections.Generic;

namespace unicode;

/// <summary>
/// Collects the Unicode scalars and scripts needed when planning a PDF font subset.
/// </summary>
public class UnicodePdfFontSubset : IUnicodePdfFontSubset
{
    internal static readonly UnicodePdfFontSubset Instance = new UnicodePdfFontSubset();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodePdfFontSubset()
    {
    }
    /// <summary>
    /// Returns each distinct valid Unicode scalar used by the text, in first-seen order.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public int[] CollectScalars(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] records = UnicodeScalarReader.Instance.ReadAll(text);
        int[] scalars = new int[] { };
        int index = 0;
        while (index < records.Length)
        {
            scalars = AddScalar(scalars, records[index].CodePoint);
            index = index + 1;
        }

        return scalars;
    }

    /// <summary>
    /// Returns the significant scripts used by the text, in first-seen order.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodeScript[] CollectScripts(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] records = UnicodeScalarReader.Instance.ReadAll(text);
        UnicodeScript[] layoutScripts = UnicodePdfText.ResolveLayoutScripts(records);
        UnicodeScript[] scripts = new UnicodeScript[] { };
        int index = 0;
        while (index < layoutScripts.Length)
        {
            scripts = AddScript(scripts, layoutScripts[index]);
            index = index + 1;
        }

        return scripts;
    }

    /// <summary>
    /// Groups distinct input scalars by their resolved script for font-subset planning.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodePdfScriptScalarSet[] CollectScriptScalarSets(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] records = UnicodeScalarReader.Instance.ReadAll(text);
        UnicodeScript[] layoutScripts = UnicodePdfText.ResolveLayoutScripts(records);
        List<UnicodePdfScriptScalarSet> sets = new List<UnicodePdfScriptScalarSet>();
        int index = 0;
        while (index < records.Length)
        {
            int setIndex = FindScriptScalarSetIndex(sets, layoutScripts[index]);
            if (setIndex < 0)
            {
                sets.Add(new UnicodePdfScriptScalarSet(
                    layoutScripts[index],
                    new int[] { records[index].CodePoint }));
            }
            else
            {
                sets[setIndex].Scalars = AddScalar(sets[setIndex].Scalars, records[index].CodePoint);
            }

            index = index + 1;
        }

        return sets.ToArray();
    }

    /// <summary>
    /// Returns each distinct scalar assigned to the requested script, in first-seen order.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="script">The Unicode script to select.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public int[] CollectScalarsForScript(string text, UnicodeScript script)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodePdfScriptScalarSet[] sets = CollectScriptScalarSets(text);
        int index = 0;
        while (index < sets.Length)
        {
            if (sets[index].Script == script)
            {
                return sets[index].GetScalars();
            }

            index = index + 1;
        }

        return new int[] { };
    }

    /// <summary>
    /// Creates a grapheme-safe PDF font-subset plan using caller-provided font coverage.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="coverage">The font coverage callback.</param>
    /// <returns>The cluster-safe subset plan.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    public UnicodePdfFontSubsetPlan CreateSubsetPlan(string text, IUnicodePdfFontCoverage coverage)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(coverage);

        UnicodeScalarRecord[] records = UnicodeScalarReader.Instance.ReadAll(text);
        UnicodeScript[] scripts = UnicodePdfText.ResolveLayoutScripts(records);
        UnicodeSegmentRecord[] segments = UnicodeSegments.Instance.GetGraphemeSegments(text);
        List<UnicodePdfClusterSubsetPlan> clusters = new List<UnicodePdfClusterSubsetPlan>();
        List<UnicodePdfScriptScalarSet> requiredSets = new List<UnicodePdfScriptScalarSet>();
        List<UnicodePdfScriptScalarSet> coveredSets = new List<UnicodePdfScriptScalarSet>();
        List<UnicodePdfScriptScalarSet> missingSets = new List<UnicodePdfScriptScalarSet>();
        int[] requiredScalars = new int[] { };
        int[] coveredScalars = new int[] { };
        int[] missingScalars = new int[] { };
        int recordIndex = 0;
        int segmentIndex = 0;

        while (segmentIndex < segments.Length)
        {
            int firstRecordIndex = recordIndex;
            int[] clusterScalars = new int[] { };
            bool clusterCovered = true;
            while (recordIndex < records.Length)
            {
                if (records[recordIndex].Utf16Index >= segments[segmentIndex].EndUtf16Index)
                {
                    break;
                }

                int codePoint = records[recordIndex].CodePoint;
                clusterScalars = AddScalar(clusterScalars, codePoint);
                requiredScalars = AddScalar(requiredScalars, codePoint);
                AddScalarToScriptSets(requiredSets, scripts[recordIndex], codePoint);
                if (!coverage.CoversScalar(codePoint))
                {
                    clusterCovered = false;
                }

                recordIndex = recordIndex + 1;
            }

            int clusterRecordIndex = firstRecordIndex;
            while (clusterRecordIndex < recordIndex)
            {
                int codePoint = records[clusterRecordIndex].CodePoint;
                if (clusterCovered)
                {
                    coveredScalars = AddScalar(coveredScalars, codePoint);
                    AddScalarToScriptSets(coveredSets, scripts[clusterRecordIndex], codePoint);
                }
                else
                {
                    missingScalars = AddScalar(missingScalars, codePoint);
                    AddScalarToScriptSets(missingSets, scripts[clusterRecordIndex], codePoint);
                }

                clusterRecordIndex = clusterRecordIndex + 1;
            }

            UnicodeScript clusterScript = UnicodeScript.Common;
            if (firstRecordIndex < scripts.Length)
            {
                clusterScript = scripts[firstRecordIndex];
            }

            clusters.Add(new UnicodePdfClusterSubsetPlan(
                segments[segmentIndex].StartUtf16Index,
                segments[segmentIndex].EndUtf16Index,
                clusterScript,
                clusterScalars,
                clusterCovered));
            segmentIndex = segmentIndex + 1;
        }

        UnicodePdfScriptSubsetPlan[] scriptPlans = CreateScriptSubsetPlans(
            requiredSets,
            coveredSets,
            missingSets);
        return new UnicodePdfFontSubsetPlan(
            requiredScalars,
            coveredScalars,
            missingScalars,
            scriptPlans,
            clusters.ToArray());
    }

    internal static void AddScalarToScriptSets(
        List<UnicodePdfScriptScalarSet> sets,
        UnicodeScript script,
        int codePoint)
    {
        System.ArgumentNullException.ThrowIfNull(sets);

        int setIndex = FindScriptScalarSetIndex(sets, script);
        if (setIndex < 0)
        {
            sets.Add(new UnicodePdfScriptScalarSet(script, new int[] { codePoint }));
            return;
        }

        sets[setIndex].Scalars = AddScalar(sets[setIndex].Scalars, codePoint);
    }

    internal static UnicodePdfScriptSubsetPlan[] CreateScriptSubsetPlans(
        List<UnicodePdfScriptScalarSet> requiredSets,
        List<UnicodePdfScriptScalarSet> coveredSets,
        List<UnicodePdfScriptScalarSet> missingSets)
    {
        System.ArgumentNullException.ThrowIfNull(requiredSets);
        System.ArgumentNullException.ThrowIfNull(coveredSets);
        System.ArgumentNullException.ThrowIfNull(missingSets);

        UnicodePdfScriptSubsetPlan[] plans = new UnicodePdfScriptSubsetPlan[requiredSets.Count];
        int index = 0;
        while (index < requiredSets.Count)
        {
            if (requiredSets[index] == null)
            {
                throw new System.ArgumentNullException("requiredSets");
            }

            int[] covered = GetScriptSetScalars(coveredSets, requiredSets[index].Script);
            int[] missing = GetScriptSetScalars(missingSets, requiredSets[index].Script);
            plans[index] = new UnicodePdfScriptSubsetPlan(
                requiredSets[index].Script,
                requiredSets[index].Scalars,
                covered,
                missing);
            index = index + 1;
        }

        return plans;
    }

    internal static int[] GetScriptSetScalars(
        List<UnicodePdfScriptScalarSet> sets,
        UnicodeScript script)
    {
        System.ArgumentNullException.ThrowIfNull(sets);

        int index = FindScriptScalarSetIndex(sets, script);
        if (index < 0)
        {
            return new int[] { };
        }

        return sets[index].GetScalars();
    }

    internal static int[] AddScalar(int[] scalars, int codePoint)
    {
        ValidateScalars(scalars);

        if (!UnicodeScalar.Instance.IsValid(codePoint))
        {
            throw new System.ArgumentOutOfRangeException("codePoint");
        }

        if (ContainsScalar(scalars, codePoint))
        {
            return CopyScalars(scalars);
        }

        int[] result = new int[scalars.Length + 1];
        bool inserted = false;
        int sourceIndex = 0;
        int targetIndex = 0;
        while (sourceIndex < scalars.Length)
        {
            if (!inserted)
            {
                if (codePoint < scalars[sourceIndex])
                {
                    result[targetIndex] = codePoint;
                    targetIndex = targetIndex + 1;
                    inserted = true;
                }
            }

            result[targetIndex] = scalars[sourceIndex];
            targetIndex = targetIndex + 1;
            sourceIndex = sourceIndex + 1;
        }

        if (!inserted)
        {
            result[targetIndex] = codePoint;
        }

        return result;
    }

    internal static UnicodeScript[] AddScript(UnicodeScript[] scripts, UnicodeScript script)
    {
        System.ArgumentNullException.ThrowIfNull(scripts);

        if (ContainsScript(scripts, script))
        {
            return CopyScripts(scripts);
        }

        UnicodeScript[] result = new UnicodeScript[scripts.Length + 1];
        int index = 0;
        while (index < scripts.Length)
        {
            result[index] = scripts[index];
            index = index + 1;
        }

        result[scripts.Length] = script;
        return result;
    }

    internal static bool ContainsScalar(int[] scalars, int codePoint)
    {
        ValidateScalars(scalars);

        if (!UnicodeScalar.Instance.IsValid(codePoint))
        {
            return false;
        }

        int index = 0;
        while (index < scalars.Length)
        {
            if (scalars[index] == codePoint)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsScript(UnicodeScript[] scripts, UnicodeScript script)
    {
        System.ArgumentNullException.ThrowIfNull(scripts);

        int index = 0;
        while (index < scripts.Length)
        {
            if (scripts[index] == script)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int FindScriptScalarSetIndex(List<UnicodePdfScriptScalarSet> sets, UnicodeScript script)
    {
        System.ArgumentNullException.ThrowIfNull(sets);

        int index = 0;
        while (index < sets.Count)
        {
            if (sets[index] == null)
            {
                throw new System.ArgumentNullException("sets");
            }

            if (sets[index].Script == script)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static void ValidateScalars(int[] scalars)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        int index = 0;
        while (index < scalars.Length)
        {
            if (!UnicodeScalar.Instance.IsValid(scalars[index]))
            {
                throw new System.ArgumentOutOfRangeException("scalars");
            }

            index = index + 1;
        }
    }

    internal static int[] CopyScalars(int[] scalars)
    {
        ValidateScalars(scalars);

        int[] copy = new int[scalars.Length];
        int index = 0;
        while (index < scalars.Length)
        {
            copy[index] = scalars[index];
            index = index + 1;
        }

        return copy;
    }

    internal static UnicodeScript[] CopyScripts(UnicodeScript[] scripts)
    {
        System.ArgumentNullException.ThrowIfNull(scripts);

        UnicodeScript[] copy = new UnicodeScript[scripts.Length];
        int index = 0;
        while (index < scripts.Length)
        {
            copy[index] = scripts[index];
            index = index + 1;
        }

        return copy;
    }
}
