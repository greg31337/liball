namespace ttf;

internal class ScriptRunMap
{
    string[] scripts;
    int[] clusterStarts;
    int[] clusterLengths;
    int[] runeStarts;
    int[] runeLengths;
    int[] utf16Starts;
    int[] utf16Lengths;
    int sourceLength;

    internal ScriptRunMap(
        string[] scripts,
        int[] clusterStarts,
        int[] clusterLengths,
        int[] runeStarts,
        int[] runeLengths,
        int[] utf16Starts,
        int[] utf16Lengths,
        int sourceLength)
    {
        this.scripts = scripts;
        this.clusterStarts = clusterStarts;
        this.clusterLengths = clusterLengths;
        this.runeStarts = runeStarts;
        this.runeLengths = runeLengths;
        this.utf16Starts = utf16Starts;
        this.utf16Lengths = utf16Lengths;
        this.sourceLength = sourceLength;
    }

    internal int GetRunCount()
    {
        return scripts.Length;
    }

    internal string GetScript(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return scripts[runIndex];
    }

    internal int GetClusterStart(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return clusterStarts[runIndex];
    }

    internal int GetClusterLength(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return clusterLengths[runIndex];
    }

    internal int GetClusterEnd(int runIndex)
    {
        return GetClusterStart(runIndex) + GetClusterLength(runIndex);
    }

    internal int GetRuneStart(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return runeStarts[runIndex];
    }

    internal int GetRuneLength(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return runeLengths[runIndex];
    }

    internal int GetRuneEnd(int runIndex)
    {
        return GetRuneStart(runIndex) + GetRuneLength(runIndex);
    }

    internal int GetUtf16Start(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return utf16Starts[runIndex];
    }

    internal int GetUtf16Length(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return utf16Lengths[runIndex];
    }

    internal int GetUtf16End(int runIndex)
    {
        return GetUtf16Start(runIndex) + GetUtf16Length(runIndex);
    }

    internal int GetSourceLength()
    {
        return sourceLength;
    }

    void ValidateRunIndex(int runIndex)
    {
        if (runIndex < 0 || runIndex >= scripts.Length)
        {
            throw new System.ArgumentOutOfRangeException("runIndex");
        }
    }
}

internal static class ScriptItemizer
{
    const string CommonScript = "Zyyy";
    const string InheritedScript = "Zinh";
    const string UnknownScript = "Zzzz";
    const string RuneLimitMessage =
        "Script itemization exceeds the configured rune limit.";
    const string RunLimitMessage =
        "Script itemization exceeds the configured run limit.";

    internal static ScriptRunMap Itemize(
        RuneBuffer input,
        GraphemeClusterMap clusters,
        UnicodeData unicodeData,
        int maximumRuneCount,
        int maximumRunCount)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }

        if (clusters == null)
        {
            throw new System.ArgumentNullException("clusters");
        }

        if (unicodeData == null)
        {
            throw new System.ArgumentNullException("unicodeData");
        }

        if (maximumRuneCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumRuneCount");
        }

        if (maximumRunCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumRunCount");
        }

        if (input.GetCount() > maximumRuneCount)
        {
            throw FontLimitException.Create(RuneLimitMessage);
        }

        ValidateClusters(input, clusters);
        int clusterCount = clusters.GetClusterCount();
        string[] candidates = new string[clusterCount];
        string[] resolvedScripts = new string[clusterCount];
        LoadCandidates(input, clusters, unicodeData, candidates);
        ResolveScripts(candidates, resolvedScripts);
        ResolvePairedPunctuation(
            input,
            clusters,
            unicodeData,
            resolvedScripts);
        int runCount = CountRuns(resolvedScripts, maximumRunCount);
        string[] scripts = new string[runCount];
        int[] clusterStarts = new int[runCount];
        int[] clusterLengths = new int[runCount];
        int[] runeStarts = new int[runCount];
        int[] runeLengths = new int[runCount];
        int[] utf16Starts = new int[runCount];
        int[] utf16Lengths = new int[runCount];
        FillRuns(
            clusters,
            resolvedScripts,
            scripts,
            clusterStarts,
            clusterLengths,
            runeStarts,
            runeLengths,
            utf16Starts,
            utf16Lengths);
        return new ScriptRunMap(
            scripts,
            clusterStarts,
            clusterLengths,
            runeStarts,
            runeLengths,
            utf16Starts,
            utf16Lengths,
            input.GetSourceLength());
    }

    static void ValidateClusters(
        RuneBuffer input,
        GraphemeClusterMap clusters)
    {
        if (input.GetSourceLength() != clusters.GetSourceLength())
        {
            throw new System.ArgumentException(
                "The grapheme clusters do not describe the rune buffer.",
                "clusters");
        }

        int expectedRuneStart = 0;
        int clusterIndex = 0;

        while (clusterIndex < clusters.GetClusterCount())
        {
            if (clusters.GetRuneStart(clusterIndex) != expectedRuneStart ||
                clusters.GetRuneLength(clusterIndex) <= 0)
            {
                throw new System.ArgumentException(
                    "The grapheme clusters do not describe the rune buffer.",
                    "clusters");
            }

            expectedRuneStart = clusters.GetRuneEnd(clusterIndex);
            clusterIndex++;
        }

        if (expectedRuneStart != input.GetCount())
        {
            throw new System.ArgumentException(
                "The grapheme clusters do not describe the rune buffer.",
                "clusters");
        }
    }

    static void LoadCandidates(
        RuneBuffer input,
        GraphemeClusterMap clusters,
        UnicodeData unicodeData,
        string[] candidates)
    {
        int clusterIndex = 0;

        while (clusterIndex < clusters.GetClusterCount())
        {
            int runeIndex = clusters.GetRuneStart(clusterIndex);
            int runeEnd = clusters.GetRuneEnd(clusterIndex);
            string clusterCandidates = "";

            while (runeIndex < runeEnd && clusterCandidates.Length == 0)
            {
                int codePoint = input.GetScalarValue(runeIndex);
                string extensions = unicodeData.GetPropertyName(
                    UnicodeDataTags.ScriptExtensions,
                    codePoint);

                if (extensions.Length != 0)
                {
                    clusterCandidates = RemoveSpecialScripts(extensions);
                }
                else
                {
                    string script = unicodeData.GetPropertyName(
                        UnicodeDataTags.Script,
                        codePoint);

                    if (IsSpecificScript(script))
                    {
                        clusterCandidates = script;
                    }
                }

                runeIndex++;
            }

            candidates[clusterIndex] = clusterCandidates;
            clusterIndex++;
        }
    }

    static void ResolveScripts(
        string[] candidates,
        string[] resolvedScripts)
    {
        string runCandidates = "";
        int runStart = 0;
        int clusterIndex = 0;

        while (clusterIndex < candidates.Length)
        {
            string candidateSet = candidates[clusterIndex];

            if (candidateSet.Length != 0)
            {
                if (runCandidates.Length == 0)
                {
                    runCandidates = candidateSet;
                }
                else
                {
                    string intersection = IntersectScripts(
                        runCandidates,
                        candidateSet);

                    if (intersection.Length == 0)
                    {
                        FillResolvedScripts(
                            resolvedScripts,
                            runStart,
                            clusterIndex,
                            GetFirstScript(runCandidates));
                        runStart = clusterIndex;
                        runCandidates = candidateSet;
                    }
                    else
                    {
                        runCandidates = intersection;
                    }
                }
            }

            clusterIndex++;
        }

        string finalScript = runCandidates.Length == 0
            ? CommonScript
            : GetFirstScript(runCandidates);
        FillResolvedScripts(
            resolvedScripts,
            runStart,
            resolvedScripts.Length,
            finalScript);
    }

    static void FillResolvedScripts(
        string[] resolvedScripts,
        int start,
        int end,
        string script)
    {
        int index = start;

        while (index < end)
        {
            resolvedScripts[index] = script;
            index++;
        }
    }

    static string IntersectScripts(string first, string second)
    {
        System.Text.StringBuilder intersection =
            new System.Text.StringBuilder();
        int tokenStart = 0;

        while (tokenStart < first.Length)
        {
            int tokenEnd = FindTokenEnd(first, tokenStart);
            string script = first.Substring(
                tokenStart,
                tokenEnd - tokenStart);

            if (ContainsScript(second, script))
            {
                if (intersection.Length != 0)
                {
                    intersection.Append(' ');
                }

                intersection.Append(script);
            }

            tokenStart = tokenEnd + 1;
        }

        return intersection.ToString();
    }

    static void ResolvePairedPunctuation(
        RuneBuffer input,
        GraphemeClusterMap clusters,
        UnicodeData unicodeData,
        string[] resolvedScripts)
    {
        int clusterCount = clusters.GetClusterCount();
        int[] openingClusters = new int[clusterCount];
        int[] expectedClosings = new int[clusterCount];
        int[] pairOpenings = new int[clusterCount];
        int[] pairClosings = new int[clusterCount];
        int stackCount = 0;
        int pairCount = 0;
        int clusterIndex = 0;

        while (clusterIndex < clusterCount)
        {
            int bracketCodePoint = GetClusterBracket(
                input,
                clusters,
                unicodeData,
                clusterIndex);

            if (bracketCodePoint >= 0)
            {
                int bracketType = unicodeData.GetMappingAuxiliary(
                    UnicodeDataTags.Bracket,
                    bracketCodePoint);

                if (bracketType == 1)
                {
                    openingClusters[stackCount] = clusterIndex;
                    expectedClosings[stackCount] = unicodeData.GetMapping(
                        UnicodeDataTags.Bracket,
                        bracketCodePoint);
                    stackCount++;
                }
                else if (bracketType == 2)
                {
                    int matchIndex = stackCount - 1;

                    while (matchIndex >= 0 &&
                        expectedClosings[matchIndex] != bracketCodePoint)
                    {
                        matchIndex--;
                    }

                    if (matchIndex >= 0)
                    {
                        pairOpenings[pairCount] =
                            openingClusters[matchIndex];
                        pairClosings[pairCount] = clusterIndex;
                        pairCount++;
                        stackCount = matchIndex;
                    }
                }
            }

            clusterIndex++;
        }

        int pairIndex = 0;

        while (pairIndex < pairCount)
        {
            int opening = pairOpenings[pairIndex];
            int closing = pairClosings[pairIndex];
            string script = SelectPairScript(
                resolvedScripts,
                opening,
                closing);
            resolvedScripts[opening] = script;
            resolvedScripts[closing] = script;
            pairIndex++;
        }
    }

    static int GetClusterBracket(
        RuneBuffer input,
        GraphemeClusterMap clusters,
        UnicodeData unicodeData,
        int clusterIndex)
    {
        int runeIndex = clusters.GetRuneStart(clusterIndex);
        int runeEnd = clusters.GetRuneEnd(clusterIndex);

        while (runeIndex < runeEnd)
        {
            int codePoint = input.GetScalarValue(runeIndex);

            if (unicodeData.GetMapping(UnicodeDataTags.Bracket, codePoint) >= 0)
            {
                return codePoint;
            }

            runeIndex++;
        }

        return -1;
    }

    static string SelectPairScript(
        string[] resolvedScripts,
        int opening,
        int closing)
    {
        if (opening > 0 &&
            resolvedScripts[opening - 1] != CommonScript)
        {
            return resolvedScripts[opening - 1];
        }

        if (closing + 1 < resolvedScripts.Length &&
            resolvedScripts[closing + 1] != CommonScript)
        {
            return resolvedScripts[closing + 1];
        }

        int index = opening + 1;

        while (index < closing)
        {
            if (resolvedScripts[index] != CommonScript)
            {
                return resolvedScripts[index];
            }

            index++;
        }

        return resolvedScripts[opening];
    }

    static int CountRuns(string[] resolvedScripts, int maximumRunCount)
    {
        if (resolvedScripts.Length == 0)
        {
            return 0;
        }

        if (maximumRunCount == 0)
        {
            throw FontLimitException.Create(RunLimitMessage);
        }

        int runCount = 1;
        int index = 1;

        while (index < resolvedScripts.Length)
        {
            if (resolvedScripts[index] != resolvedScripts[index - 1])
            {
                if (runCount == maximumRunCount)
                {
                    throw FontLimitException.Create(RunLimitMessage);
                }

                runCount++;
            }

            index++;
        }

        return runCount;
    }

    static void FillRuns(
        GraphemeClusterMap clusters,
        string[] resolvedScripts,
        string[] scripts,
        int[] clusterStarts,
        int[] clusterLengths,
        int[] runeStarts,
        int[] runeLengths,
        int[] utf16Starts,
        int[] utf16Lengths)
    {
        if (resolvedScripts.Length == 0)
        {
            return;
        }

        int runIndex = 0;
        int clusterStart = 0;
        int clusterIndex = 1;

        while (clusterIndex <= resolvedScripts.Length)
        {
            if (clusterIndex == resolvedScripts.Length ||
                resolvedScripts[clusterIndex] !=
                    resolvedScripts[clusterStart])
            {
                FillRun(
                    clusters,
                    resolvedScripts[clusterStart],
                    clusterStart,
                    clusterIndex,
                    scripts,
                    clusterStarts,
                    clusterLengths,
                    runeStarts,
                    runeLengths,
                    utf16Starts,
                    utf16Lengths,
                    runIndex);
                runIndex++;
                clusterStart = clusterIndex;
            }

            clusterIndex++;
        }
    }

    static void FillRun(
        GraphemeClusterMap clusters,
        string script,
        int clusterStart,
        int clusterEnd,
        string[] scripts,
        int[] clusterStarts,
        int[] clusterLengths,
        int[] runeStarts,
        int[] runeLengths,
        int[] utf16Starts,
        int[] utf16Lengths,
        int runIndex)
    {
        int utf16Start = clusters.GetUtf16Start(clusterStart);
        int utf16End = clusters.GetUtf16End(clusterStart);
        int clusterIndex = clusterStart + 1;

        while (clusterIndex < clusterEnd)
        {
            int candidateStart = clusters.GetUtf16Start(clusterIndex);
            int candidateEnd = clusters.GetUtf16End(clusterIndex);

            if (candidateStart < utf16Start)
            {
                utf16Start = candidateStart;
            }

            if (candidateEnd > utf16End)
            {
                utf16End = candidateEnd;
            }

            clusterIndex++;
        }

        int runeStart = clusters.GetRuneStart(clusterStart);
        int lastCluster = clusterEnd - 1;
        int runeEnd = clusters.GetRuneEnd(lastCluster);
        scripts[runIndex] = script;
        clusterStarts[runIndex] = clusterStart;
        clusterLengths[runIndex] = clusterEnd - clusterStart;
        runeStarts[runIndex] = runeStart;
        runeLengths[runIndex] = runeEnd - runeStart;
        utf16Starts[runIndex] = utf16Start;
        utf16Lengths[runIndex] = utf16End - utf16Start;
    }

    static string RemoveSpecialScripts(string scripts)
    {
        if (scripts == CommonScript ||
            scripts == InheritedScript ||
            scripts == UnknownScript)
        {
            return "";
        }

        return scripts;
    }

    static bool IsSpecificScript(string script)
    {
        return script != CommonScript &&
            script != InheritedScript &&
            script != UnknownScript;
    }

    static string GetFirstScript(string scripts)
    {
        int tokenEnd = FindTokenEnd(scripts, 0);
        return scripts.Substring(0, tokenEnd);
    }

    static bool ContainsScript(string scripts, string script)
    {
        int tokenStart = 0;

        while (tokenStart < scripts.Length)
        {
            int tokenEnd = FindTokenEnd(scripts, tokenStart);
            int tokenLength = tokenEnd - tokenStart;

            if (tokenLength == script.Length &&
                System.String.CompareOrdinal(
                    scripts,
                    tokenStart,
                    script,
                    0,
                    tokenLength) == 0)
            {
                return true;
            }

            tokenStart = tokenEnd + 1;
        }

        return false;
    }

    static int FindTokenEnd(string scripts, int tokenStart)
    {
        int index = tokenStart;

        while (index < scripts.Length && scripts[index] != ' ')
        {
            index++;
        }

        return index;
    }
}
