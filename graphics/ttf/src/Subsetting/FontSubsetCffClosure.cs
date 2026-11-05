namespace ttf;

internal class FontSubsetCffLocalSubroutineClosure
{
    int fontDictionaryIndex;
    int[] subroutineIndices;

    internal FontSubsetCffLocalSubroutineClosure(
        int fontDictionaryIndex,
        int[] subroutineIndices)
    {
        this.fontDictionaryIndex = fontDictionaryIndex;
        this.subroutineIndices = CopyIntegers(subroutineIndices);
    }

    internal int GetFontDictionaryIndex()
    {
        return fontDictionaryIndex;
    }

    internal int[] CopySubroutineIndices()
    {
        return CopyIntegers(subroutineIndices);
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}

internal class FontSubsetCffClosure
{
    bool applicable;
    int[] fontDictionaryIndices;
    int[] globalSubroutineIndices;
    FontSubsetCffLocalSubroutineClosure[] localSubroutines;
    FontDiagnostic[] diagnostics;

    internal FontSubsetCffClosure(
        bool applicable,
        int[] fontDictionaryIndices,
        int[] globalSubroutineIndices,
        FontSubsetCffLocalSubroutineClosure[] localSubroutines,
        FontDiagnostic[] diagnostics)
    {
        this.applicable = applicable;
        this.fontDictionaryIndices = CopyIntegers(fontDictionaryIndices);
        this.globalSubroutineIndices = CopyIntegers(globalSubroutineIndices);
        this.localSubroutines = CopyLocalSubroutines(localSubroutines);
        this.diagnostics = CopyDiagnostics(diagnostics);
    }

    internal bool IsApplicable()
    {
        return applicable;
    }

    internal int[] CopyFontDictionaryIndices()
    {
        return CopyIntegers(fontDictionaryIndices);
    }

    internal int[] CopyGlobalSubroutineIndices()
    {
        return CopyIntegers(globalSubroutineIndices);
    }

    internal FontSubsetCffLocalSubroutineClosure[] CopyLocalSubroutines()
    {
        return CopyLocalSubroutines(localSubroutines);
    }

    internal FontDiagnostic[] CopyDiagnostics()
    {
        return CopyDiagnostics(diagnostics);
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static FontSubsetCffLocalSubroutineClosure[] CopyLocalSubroutines(
        FontSubsetCffLocalSubroutineClosure[] values)
    {
        FontSubsetCffLocalSubroutineClosure[] copy =
            new FontSubsetCffLocalSubroutineClosure[values.Length];
        int index = 0;

        while (index < copy.Length)
        {
            FontSubsetCffLocalSubroutineClosure value = values[index];
            copy[index] = new FontSubsetCffLocalSubroutineClosure(
                value.GetFontDictionaryIndex(),
                value.CopySubroutineIndices());
            index++;
        }

        return copy;
    }

    static FontDiagnostic[] CopyDiagnostics(FontDiagnostic[] values)
    {
        FontDiagnostic[] copy = new FontDiagnostic[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}

internal class FontSubsetCffNumber
{
    bool known;
    int value;

    internal FontSubsetCffNumber(bool known, int value)
    {
        this.known = known;
        this.value = value;
    }

    internal bool IsKnown()
    {
        return known;
    }

    internal int GetValue()
    {
        return value;
    }
}

internal class FontSubsetCffClosureBuilder
{
    byte[][] glyphPrograms;
    int[] glyphFontDictionaries;
    byte[][] globalPrograms;
    byte[][][] localPrograms;
    bool retainFontDictionaries;
    int maximumEntries;
    int maximumDepth;
    int faceIndex;
    int tableTag;
    System.Collections.Generic.HashSet<int> fontDictionaries;
    System.Collections.Generic.HashSet<int> globalSubroutines;
    System.Collections.Generic.HashSet<int>[] localSubroutines;
    int[][] globalStates;
    int[][] localStates;
    bool[] globalFallbacks;
    bool[] localFallbacks;
    System.Collections.Generic.List<FontDiagnostic> diagnostics;
    int retainedEntryCount;

    FontSubsetCffClosureBuilder(
        byte[][] glyphPrograms,
        int[] glyphFontDictionaries,
        byte[][] globalPrograms,
        byte[][][] localPrograms,
        bool retainFontDictionaries,
        int maximumEntries,
        int maximumDepth,
        int faceIndex,
        int tableTag)
    {
        this.glyphPrograms = glyphPrograms;
        this.glyphFontDictionaries = glyphFontDictionaries;
        this.globalPrograms = globalPrograms;
        this.localPrograms = localPrograms;
        this.retainFontDictionaries = retainFontDictionaries;
        this.maximumEntries = maximumEntries;
        this.maximumDepth = maximumDepth;
        this.faceIndex = faceIndex;
        this.tableTag = tableTag;
        fontDictionaries = new System.Collections.Generic.HashSet<int>();
        globalSubroutines = new System.Collections.Generic.HashSet<int>();
        localSubroutines =
            new System.Collections.Generic.HashSet<int>[localPrograms.Length];
        globalStates = new int[localPrograms.Length][];
        localStates = new int[localPrograms.Length][];
        globalFallbacks = new bool[localPrograms.Length];
        localFallbacks = new bool[localPrograms.Length];
        diagnostics = new System.Collections.Generic.List<FontDiagnostic>();

        int index = 0;
        while (index < localPrograms.Length)
        {
            localSubroutines[index] =
                new System.Collections.Generic.HashSet<int>();
            globalStates[index] = new int[globalPrograms.Length];
            localStates[index] = new int[localPrograms[index].Length];
            index++;
        }
    }

    internal static FontSubsetCffClosure Create(
        FontInstance instance,
        int[] glyphIds,
        int maximumEntries)
    {
        FontFace face = instance.GetFace();
        FontOutlineKind kind = face.GetOutlineKind();

        if (kind != FontOutlineKind.Cff1 && kind != FontOutlineKind.Cff2)
        {
            return Empty(false);
        }

        int maximumDepth = face.GetTableCache()
            .GetLimits()
            .GetMaxCffSubroutineDepth();

        if (kind == FontOutlineKind.Cff1)
        {
            return CreateCff1(
                face.GetCffTable(),
                glyphIds,
                maximumEntries,
                maximumDepth,
                face.GetFaceIndex());
        }

        return CreateCff2(
            face.GetCff2Table(),
            glyphIds,
            maximumEntries,
            maximumDepth,
            face.GetFaceIndex());
    }

    internal static FontSubsetCffClosure CreateForPrograms(
        byte[][] glyphPrograms,
        int[] glyphFontDictionaries,
        byte[][] globalPrograms,
        byte[][][] localPrograms,
        bool retainFontDictionaries,
        int[] glyphIds,
        int maximumEntries,
        int maximumDepth)
    {
        ValidateProgramInputs(
            glyphPrograms,
            glyphFontDictionaries,
            globalPrograms,
            localPrograms,
            glyphIds);
        FontSubsetCffClosureBuilder builder =
            new FontSubsetCffClosureBuilder(
                glyphPrograms,
                glyphFontDictionaries,
                globalPrograms,
                localPrograms,
                retainFontDictionaries,
                maximumEntries,
                maximumDepth,
                0,
                FontTags.Cff);
        return builder.Build(glyphIds);
    }

    static FontSubsetCffClosure CreateCff1(
        SfntCffTable? table,
        int[] glyphIds,
        int maximumEntries,
        int maximumDepth,
        int faceIndex)
    {
        System.ArgumentNullException.ThrowIfNull(table);
        CffIndex charStrings = table.GetCharStringsIndex();
        int fontDictionaryCount =
            table.GetFontDictionaryCountForSubsetting();
        byte[][] glyphPrograms = CopyPrograms(charStrings);
        byte[][] globalPrograms = CopyPrograms(
            table.GetGlobalSubroutineIndex());
        byte[][][] localPrograms = new byte[fontDictionaryCount][][];
        int[] glyphFontDictionaries =
            new int[charStrings.GetCount()];
        int index = 0;

        while (index < fontDictionaryCount)
        {
            localPrograms[index] = CopyPrograms(
                table.GetLocalSubroutineIndexForSubsetting(index));
            index++;
        }

        index = 0;
        while (index < glyphFontDictionaries.Length)
        {
            glyphFontDictionaries[index] =
                table.GetFontDictionaryIndexForGlyph(index);
            index++;
        }

        FontSubsetCffClosureBuilder builder =
            new FontSubsetCffClosureBuilder(
                glyphPrograms,
                glyphFontDictionaries,
                globalPrograms,
                localPrograms,
                table.HasFontDictionariesForSubsetting(),
                maximumEntries,
                maximumDepth,
                faceIndex,
                FontTags.Cff);
        return builder.Build(glyphIds);
    }

    static FontSubsetCffClosure CreateCff2(
        SfntCff2Table? table,
        int[] glyphIds,
        int maximumEntries,
        int maximumDepth,
        int faceIndex)
    {
        System.ArgumentNullException.ThrowIfNull(table);
        Cff2Index charStrings = table.GetCharStringsIndex();
        int fontDictionaryCount = table.GetFontDictionaryArray().GetCount();
        byte[][] glyphPrograms = CopyPrograms(charStrings);
        byte[][] globalPrograms = CopyPrograms(
            table.GetGlobalSubroutineIndex());
        byte[][][] localPrograms = new byte[fontDictionaryCount][][];
        int[] glyphFontDictionaries =
            new int[charStrings.GetCount()];
        int index = 0;

        while (index < fontDictionaryCount)
        {
            localPrograms[index] = CopyPrograms(
                table.GetLocalSubroutineIndex(index));
            index++;
        }

        index = 0;
        while (index < glyphFontDictionaries.Length)
        {
            glyphFontDictionaries[index] =
                table.GetFontDictionaryIndexForGlyph(index);
            index++;
        }

        FontSubsetCffClosureBuilder builder =
            new FontSubsetCffClosureBuilder(
                glyphPrograms,
                glyphFontDictionaries,
                globalPrograms,
                localPrograms,
                true,
                maximumEntries,
                maximumDepth,
                faceIndex,
                FontTags.Cff2);
        return builder.Build(glyphIds);
    }

    FontSubsetCffClosure Build(int[] glyphIds)
    {
        int index = 0;

        while (index < glyphIds.Length)
        {
            int glyphId = glyphIds[index];
            int fontDictionaryIndex = glyphFontDictionaries[glyphId];
            AddFontDictionary(fontDictionaryIndex);
            ScanProgram(
                glyphPrograms[glyphId],
                fontDictionaryIndex,
                0,
                "glyph[" + glyphId + "]");
            index++;
        }

        int[] sortedFontDictionaries = ToSortedArray(fontDictionaries);
        int[] sortedGlobals = ToSortedArray(globalSubroutines);
        FontSubsetCffLocalSubroutineClosure[] localClosures =
            new FontSubsetCffLocalSubroutineClosure[
                sortedFontDictionaries.Length];
        index = 0;

        while (index < sortedFontDictionaries.Length)
        {
            int fontDictionaryIndex = sortedFontDictionaries[index];
            localClosures[index] =
                new FontSubsetCffLocalSubroutineClosure(
                    fontDictionaryIndex,
                    ToSortedArray(localSubroutines[fontDictionaryIndex]));
            index++;
        }

        int[] retainedFontDictionaries = retainFontDictionaries ?
            sortedFontDictionaries : new int[0];
        return new FontSubsetCffClosure(
            true,
            retainedFontDictionaries,
            sortedGlobals,
            localClosures,
            diagnostics.ToArray());
    }

    void ScanProgram(
        byte[] program,
        int fontDictionaryIndex,
        int depth,
        string logicalPath)
    {
        System.Collections.Generic.List<FontSubsetCffNumber> stack =
            new System.Collections.Generic.List<FontSubsetCffNumber>();
        int stemCount = 0;
        bool stemCountKnown = depth == 0;
        int offset = 0;

        while (offset < program.Length)
        {
            int operation = program[offset];
            offset++;

            if (operation >= 32 || operation == 28 || operation == 255)
            {
                FontSubsetCffNumber number = ReadNumber(program, offset - 1);
                stack.Add(number);
                offset += GetNumberLength(operation) - 1;
                continue;
            }

            if (operation == 10 || operation == 29)
            {
                FontSubsetCffNumber target = PopCallTarget(stack);

                if (operation == 10)
                {
                    ProcessLocalCall(
                        target,
                        fontDictionaryIndex,
                        depth,
                        logicalPath);
                }
                else
                {
                    ProcessGlobalCall(
                        target,
                        fontDictionaryIndex,
                        depth,
                        logicalPath);
                }

                stack.Clear();
                stack.Add(new FontSubsetCffNumber(false, 0));
                stemCountKnown = false;
                continue;
            }

            if (operation == 11 || operation == 14)
            {
                return;
            }

            if (operation == 1 || operation == 3 ||
                operation == 18 || operation == 23)
            {
                stemCount += stack.Count / 2;
                stack.Clear();
                continue;
            }

            if (operation == 19 || operation == 20)
            {
                if (!stemCountKnown)
                {
                    RetainForUncertainProgram(
                        fontDictionaryIndex,
                        depth,
                        logicalPath);
                    return;
                }

                stemCount += stack.Count / 2;
                stack.Clear();
                int maskLength = (stemCount + 7) / 8;

                if (maskLength > program.Length - offset)
                {
                    throw FontFormatException.Create(
                        "A Type 2 hint mask extends outside its charstring.");
                }

                offset += maskLength;
                continue;
            }

            if (operation == 12)
            {
                if (offset >= program.Length)
                {
                    throw FontFormatException.Create(
                        "A Type 2 escaped operator is truncated.");
                }

                offset++;
                stack.Clear();
                stack.Add(new FontSubsetCffNumber(false, 0));
                continue;
            }

            stack.Clear();
        }
    }

    void RetainForUncertainProgram(
        int fontDictionaryIndex,
        int depth,
        string logicalPath)
    {
        if (localPrograms[fontDictionaryIndex].Length != 0)
        {
            RetainAllLocal(
                fontDictionaryIndex,
                depth,
                logicalPath);
        }

        if (globalPrograms.Length != 0)
        {
            RetainAllGlobal(
                fontDictionaryIndex,
                depth,
                logicalPath);
        }
    }

    void ProcessLocalCall(
        FontSubsetCffNumber target,
        int fontDictionaryIndex,
        int depth,
        string logicalPath)
    {
        byte[][] programs = localPrograms[fontDictionaryIndex];

        if (!target.IsKnown())
        {
            RetainAllLocal(
                fontDictionaryIndex,
                depth,
                logicalPath);
            return;
        }

        int resolvedIndex = ResolveSubroutineIndex(
            target.GetValue(),
            programs.Length);
        VisitLocal(
            fontDictionaryIndex,
            resolvedIndex,
            depth,
            logicalPath);
    }

    void ProcessGlobalCall(
        FontSubsetCffNumber target,
        int fontDictionaryIndex,
        int depth,
        string logicalPath)
    {
        if (!target.IsKnown())
        {
            RetainAllGlobal(
                fontDictionaryIndex,
                depth,
                logicalPath);
            return;
        }

        int resolvedIndex = ResolveSubroutineIndex(
            target.GetValue(),
            globalPrograms.Length);
        VisitGlobal(
            fontDictionaryIndex,
            resolvedIndex,
            depth,
            logicalPath);
    }

    void VisitLocal(
        int fontDictionaryIndex,
        int subroutineIndex,
        int depth,
        string logicalPath)
    {
        AddLocal(fontDictionaryIndex, subroutineIndex);

        if (localStates[fontDictionaryIndex][subroutineIndex] != 0)
        {
            return;
        }

        EnsureDepth(depth);
        localStates[fontDictionaryIndex][subroutineIndex] = 1;
        ScanProgram(
            localPrograms[fontDictionaryIndex][subroutineIndex],
            fontDictionaryIndex,
            depth + 1,
            logicalPath + ".localSubr[" + subroutineIndex + "]");
        localStates[fontDictionaryIndex][subroutineIndex] = 2;
    }

    void VisitGlobal(
        int fontDictionaryIndex,
        int subroutineIndex,
        int depth,
        string logicalPath)
    {
        AddGlobal(subroutineIndex);

        if (globalStates[fontDictionaryIndex][subroutineIndex] != 0)
        {
            return;
        }

        EnsureDepth(depth);
        globalStates[fontDictionaryIndex][subroutineIndex] = 1;
        ScanProgram(
            globalPrograms[subroutineIndex],
            fontDictionaryIndex,
            depth + 1,
            logicalPath + ".globalSubr[" + subroutineIndex + "]");
        globalStates[fontDictionaryIndex][subroutineIndex] = 2;
    }

    void RetainAllLocal(
        int fontDictionaryIndex,
        int depth,
        string logicalPath)
    {
        byte[][] programs = localPrograms[fontDictionaryIndex];

        if (programs.Length == 0)
        {
            throw FontFormatException.Create(
                "A Type 2 charstring calls a missing local subroutine INDEX.");
        }

        if (!localFallbacks[fontDictionaryIndex])
        {
            localFallbacks[fontDictionaryIndex] = true;
            AddFallbackDiagnostic(
                "local",
                fontDictionaryIndex,
                logicalPath);
        }

        int index = 0;
        while (index < programs.Length)
        {
            VisitLocal(fontDictionaryIndex, index, depth, logicalPath);
            index++;
        }
    }

    void RetainAllGlobal(
        int fontDictionaryIndex,
        int depth,
        string logicalPath)
    {
        if (globalPrograms.Length == 0)
        {
            throw FontFormatException.Create(
                "A Type 2 charstring calls a missing global subroutine INDEX.");
        }

        if (!globalFallbacks[fontDictionaryIndex])
        {
            globalFallbacks[fontDictionaryIndex] = true;
            AddFallbackDiagnostic(
                "global",
                fontDictionaryIndex,
                logicalPath);
        }

        int index = 0;
        while (index < globalPrograms.Length)
        {
            VisitGlobal(fontDictionaryIndex, index, depth, logicalPath);
            index++;
        }
    }

    void AddFontDictionary(int index)
    {
        if (index < 0 || index >= localPrograms.Length)
        {
            throw FontFormatException.Create(
                "A CFF glyph selects a Font DICT outside the FDArray.");
        }

        if (!fontDictionaries.Contains(index))
        {
            if (retainFontDictionaries)
            {
                EnsureEntryCapacity();
            }

            fontDictionaries.Add(index);
        }
    }

    void AddLocal(int fontDictionaryIndex, int subroutineIndex)
    {
        System.Collections.Generic.HashSet<int> retained =
            localSubroutines[fontDictionaryIndex];

        if (!retained.Contains(subroutineIndex))
        {
            EnsureEntryCapacity();
            retained.Add(subroutineIndex);
        }
    }

    void AddGlobal(int subroutineIndex)
    {
        if (!globalSubroutines.Contains(subroutineIndex))
        {
            EnsureEntryCapacity();
            globalSubroutines.Add(subroutineIndex);
        }
    }

    void EnsureEntryCapacity()
    {
        if (retainedEntryCount >= maximumEntries)
        {
            throw FontLimitException.Create(
                "CFF subset closure exceeds the configured entry limit.");
        }

        retainedEntryCount++;
    }

    void EnsureDepth(int depth)
    {
        if (depth >= maximumDepth)
        {
            throw FontLimitException.Create(
                "CFF subset closure exceeds the configured subroutine depth.");
        }
    }

    void AddFallbackDiagnostic(
        string kind,
        int fontDictionaryIndex,
        string logicalPath)
    {
        diagnostics.Add(new FontDiagnostic(
            FontDiagnosticSeverity.Warning,
            FontDiagnosticCode.DynamicSubroutineFallback,
            "A dynamic CFF " + kind +
                " subroutine call retained every candidate subroutine.",
            faceIndex,
            tableTag,
            -1,
            "subset." + (tableTag == FontTags.Cff2 ? "CFF2" : "CFF") +
                ".fd[" + fontDictionaryIndex + "]." +
                kind + "Subrs." + logicalPath));
    }

    static FontSubsetCffNumber PopCallTarget(
        System.Collections.Generic.List<FontSubsetCffNumber> stack)
    {
        if (stack.Count == 0)
        {
            return new FontSubsetCffNumber(false, 0);
        }

        int index = stack.Count - 1;
        FontSubsetCffNumber value = stack[index];
        stack.RemoveAt(index);
        return value;
    }

    static FontSubsetCffNumber ReadNumber(byte[] program, int offset)
    {
        int first = program[offset];

        if (first >= 32 && first <= 246)
        {
            return new FontSubsetCffNumber(true, first - 139);
        }

        int length = GetNumberLength(first);

        if (length > program.Length - offset)
        {
            throw FontFormatException.Create(
                "A Type 2 number is truncated.");
        }

        if (first >= 247 && first <= 250)
        {
            int value = (first - 247) * 256 + program[offset + 1] + 108;
            return new FontSubsetCffNumber(true, value);
        }

        if (first >= 251 && first <= 254)
        {
            int value = -(first - 251) * 256 - program[offset + 1] - 108;
            return new FontSubsetCffNumber(true, value);
        }

        if (first == 28)
        {
            int encodedInt16 = program[offset + 1] * 256 +
                program[offset + 2];
            int value = encodedInt16 < 32768
                ? encodedInt16
                : encodedInt16 - 65536;
            return new FontSubsetCffNumber(true, value);
        }

        int encoded = program[offset + 1] * 16777216 +
            program[offset + 2] * 65536 +
            program[offset + 3] * 256 +
            program[offset + 4];
        return (encoded & 65535) == 0 ?
            new FontSubsetCffNumber(true, encoded / 65536) :
            new FontSubsetCffNumber(false, 0);
    }

    static int GetNumberLength(int first)
    {
        if (first >= 32 && first <= 246)
        {
            return 1;
        }

        if (first >= 247 && first <= 254)
        {
            return 2;
        }

        if (first == 28)
        {
            return 3;
        }

        return 5;
    }

    static int ResolveSubroutineIndex(int operand, int count)
    {
        long resolved = (long)operand +
            Type2CharStringExecutor.GetSubroutineBias(count);

        if (resolved < 0 || resolved >= count)
        {
            throw FontFormatException.Create(
                "A Type 2 charstring calls a subroutine outside its INDEX.");
        }

        return (int)resolved;
    }

    static byte[][] CopyPrograms(CffIndex? index)
    {
        if (index == null)
        {
            return new byte[0][];
        }

        byte[][] programs = new byte[index.GetCount()][];
        int objectIndex = 0;

        while (objectIndex < programs.Length)
        {
            programs[objectIndex] = index.CopyObject(objectIndex);
            objectIndex++;
        }

        return programs;
    }

    static byte[][] CopyPrograms(Cff2Index? index)
    {
        if (index == null)
        {
            return new byte[0][];
        }

        byte[][] programs = new byte[index.GetCount()][];
        int objectIndex = 0;

        while (objectIndex < programs.Length)
        {
            programs[objectIndex] = index.CopyObject(objectIndex);
            objectIndex++;
        }

        return programs;
    }

    static int[] ToSortedArray(
        System.Collections.Generic.HashSet<int> values)
    {
        int[] result = new int[values.Count];
        values.CopyTo(result);
        System.Array.Sort(result);
        return result;
    }

    static void ValidateProgramInputs(
        byte[][] glyphPrograms,
        int[] glyphFontDictionaries,
        byte[][] globalPrograms,
        byte[][][] localPrograms,
        int[] glyphIds)
    {
        if (glyphPrograms == null ||
            glyphFontDictionaries == null ||
            globalPrograms == null ||
            localPrograms == null ||
            glyphIds == null)
        {
            throw new System.ArgumentNullException("programs");
        }

        if (glyphPrograms.Length != glyphFontDictionaries.Length ||
            localPrograms.Length == 0)
        {
            throw new System.ArgumentException(
                "CFF subset program arrays are inconsistent.");
        }
    }

    static FontSubsetCffClosure Empty(bool applicable)
    {
        return new FontSubsetCffClosure(
            applicable,
            new int[0],
            new int[0],
            new FontSubsetCffLocalSubroutineClosure[0],
            new FontDiagnostic[0]);
    }
}
