namespace ttf;

internal class FontSubsetCffData
{
    byte[] data;

    internal FontSubsetCffData(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        this.data = data.AsSpan().ToArray();
    }

    internal byte[] CopyData()
    {
        return data.AsSpan().ToArray();
    }
}

internal class FontSubsetCffPrivateBlock
{
    byte[] dictionary;
    byte[] subroutines;

    internal FontSubsetCffPrivateBlock(
        byte[] dictionary,
        byte[] subroutines)
    {
        this.dictionary = dictionary;
        this.subroutines = subroutines;
    }

    internal int GetLength()
    {
        return dictionary.Length + subroutines.Length;
    }

    internal byte[] CopyDictionary()
    {
        return dictionary.AsSpan().ToArray();
    }

    internal byte[] CopySubroutines()
    {
        return subroutines.AsSpan().ToArray();
    }
}

internal class FontSubsetCffStringPlan
{
    System.Collections.Generic.HashSet<int> retained;
    System.Collections.Generic.List<byte[]> generated;
    int[] oldSids;
    byte[][] strings;
    int standardCount;

    internal FontSubsetCffStringPlan()
    {
        retained = new System.Collections.Generic.HashSet<int>();
        generated = new System.Collections.Generic.List<byte[]>();
        oldSids = new int[0];
        strings = new byte[0][];
        standardCount = CffStandardStrings.GetCount();
    }

    internal void RetainSid(int sid)
    {
        if (sid >= standardCount)
        {
            retained.Add(sid);
        }
    }

    internal int AddGenerated(string value)
    {
        generated.Add(System.Text.Encoding.ASCII.GetBytes(value));
        return generated.Count - 1;
    }

    internal void Build(CffIndex sourceStrings)
    {
        oldSids = new int[retained.Count];
        retained.CopyTo(oldSids);
        System.Array.Sort(oldSids);
        strings = new byte[oldSids.Length + generated.Count][];
        int index = 0;

        while (index < oldSids.Length)
        {
            int sourceIndex = oldSids[index] - standardCount;
            strings[index] = sourceStrings.CopyObject(sourceIndex);
            index += 1;
        }

        while (index < strings.Length)
        {
            strings[index] = generated[index - oldSids.Length]
                .AsSpan().ToArray();
            index += 1;
        }
    }

    internal int MapSid(int sid)
    {
        if (sid < standardCount)
        {
            return sid;
        }

        int index = System.Array.BinarySearch(oldSids, sid);

        if (index < 0)
        {
            throw FontFormatException.Create(
                "A retained CFF dictionary references an unavailable SID.");
        }

        return standardCount + index;
    }

    internal int GetGeneratedSid(int index)
    {
        if (index < 0 || index >= generated.Count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return standardCount + oldSids.Length + index;
    }

    internal byte[][] CopyStrings()
    {
        byte[][] copy = new byte[strings.Length][];
        int index = 0;

        while (index < copy.Length)
        {
            copy[index] = strings[index].AsSpan().ToArray();
            index += 1;
        }

        return copy;
    }
}

internal static class FontSubsetCffBuilder
{
    const int TopDictionaryKind = 0;
    const int FontDictionaryKind = 1;
    const int PrivateDictionaryKind = 2;

    internal static FontSubsetCffData Create(
        FontInstance instance,
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds,
        FontSubsetCffClosure closure,
        string outputName)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        if (oldToNewGlyphIds == null)
        {
            throw new System.ArgumentNullException("oldToNewGlyphIds");
        }

        if (newToOldGlyphIds == null)
        {
            throw new System.ArgumentNullException("newToOldGlyphIds");
        }

        if (closure == null)
        {
            throw new System.ArgumentNullException("closure");
        }

        if (outputName == null)
        {
            throw new System.ArgumentNullException("outputName");
        }

        FontFace face = instance.GetFace();
        FontOutlineKind outlineKind = face.GetOutlineKind();
        FontLimits limits = face.GetTableCache().GetLimits();
        SfntCffTable? source;
        FontSubsetCffClosure effectiveClosure = closure;

        if (outlineKind == FontOutlineKind.Cff2)
        {
            source = ParseStaticCffProgram(
                SfntStaticCff2Instancer.CreateCffProgram(instance),
                limits);
            effectiveClosure = new FontSubsetCffClosure(
                true,
                new int[0],
                new int[0],
                new FontSubsetCffLocalSubroutineClosure[0],
                new FontDiagnostic[0]);
        }
        else if (outlineKind == FontOutlineKind.Cff1)
        {
            source = face.GetCffTable();
        }
        else
        {
            throw FontFormatException.Create(
                "CFF subsetting requires CFF 1 or CFF 2 outlines.");
        }

        return Create(
            source,
            limits,
            oldToNewGlyphIds,
            newToOldGlyphIds,
            effectiveClosure,
            outputName,
            null,
            false);
    }

    internal static FontSubsetCffData CreatePdfCidFont(
        FontInstance instance,
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds,
        FontSubsetCffClosure closure,
        string outputName,
        int[] charsetIdentifiers)
    {
        if (instance == null || oldToNewGlyphIds == null ||
            newToOldGlyphIds == null || closure == null ||
            outputName == null || charsetIdentifiers == null)
        {
            throw new System.ArgumentNullException("values");
        }

        FontFace face = instance.GetFace();

        if (face.GetOutlineKind() != FontOutlineKind.Cff1)
        {
            throw FontFormatException.Create(
                "A CIDFontType0 program requires CFF 1 outlines.");
        }

        SfntCffTable? source = face.GetCffTable();

        System.ArgumentNullException.ThrowIfNull(source);
        if (!source.GetTopDictionary().IsCidKeyed())
        {
            throw FontFormatException.Create(
                "A CIDFontType0 program requires a CID-keyed CFF source.");
        }

        return Create(
            source,
            face.GetTableCache().GetLimits(),
            oldToNewGlyphIds,
            newToOldGlyphIds,
            closure,
            outputName,
            charsetIdentifiers,
            true);
    }

    static FontSubsetCffData Create(
        SfntCffTable? source,
        FontLimits limits,
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds,
        FontSubsetCffClosure closure,
        string outputName,
        int[]? pdfCharsetIdentifiers,
        bool identityRos)
    {
        System.ArgumentNullException.ThrowIfNull(source);
        CffIndex sourceCharStrings = source.GetCharStringsIndex();

        if (oldToNewGlyphIds.Length != sourceCharStrings.GetCount() ||
            newToOldGlyphIds.Length == 0 ||
            newToOldGlyphIds[0] != 0)
        {
            throw FontFormatException.Create(
                "CFF subset glyph maps do not match the source CharStrings INDEX.");
        }

        CffTopDictionary top = source.GetTopDictionary();
        bool cidKeyed = top.IsCidKeyed();
        int[] retainedFontDictionaries = cidKeyed ?
            closure.CopyFontDictionaryIndices() : new int[] { 0 };
        int[] oldToNewFontDictionaries = CreateFontDictionaryMap(
            source,
            retainedFontDictionaries,
            cidKeyed);
        FontSubsetCffStringPlan stringPlan = CreateStringPlan(
            source,
            top,
            retainedFontDictionaries,
            newToOldGlyphIds,
            cidKeyed);
        int registryStringIndex = identityRos ?
            stringPlan.AddGenerated("Adobe") : -1;
        int orderingStringIndex = identityRos ?
            stringPlan.AddGenerated("Identity") : -1;
        int[] charsetIdentifiers = CreateCharsetIdentifiers(
            source,
            newToOldGlyphIds,
            cidKeyed);
        stringPlan.Build(source.GetStringIndex());
        RemapCharsetIdentifiers(
            charsetIdentifiers,
            newToOldGlyphIds,
            stringPlan,
            cidKeyed);

        if (pdfCharsetIdentifiers != null)
        {
            charsetIdentifiers = ValidatePdfCharsetIdentifiers(
                pdfCharsetIdentifiers,
                newToOldGlyphIds.Length);
        }

        int registrySid = identityRos ?
            stringPlan.GetGeneratedSid(registryStringIndex) : -1;
        int orderingSid = identityRos ?
            stringPlan.GetGeneratedSid(orderingStringIndex) : -1;

        byte[][] charStrings = CopyGlyphPrograms(
            sourceCharStrings,
            newToOldGlyphIds);
        byte[][] globalPrograms = CreateSparseSubroutines(
            source.GetGlobalSubroutineIndex(),
            closure.CopyGlobalSubroutineIndices());
        byte[] nameIndex = CreateIndex(
            new byte[][]
            {
                System.Text.Encoding.ASCII.GetBytes(outputName)
            },
            limits);
        byte[] stringIndex = CreateIndex(stringPlan.CopyStrings(), limits);
        byte[] globalIndex = CreateIndex(globalPrograms, limits);
        int predefinedEncoding = cidKeyed ? -1 :
            GetPredefinedEncoding(source);
        byte[] charset = CreateCharset(charsetIdentifiers, limits);
        byte[] encoding = cidKeyed || predefinedEncoding >= 0 ?
            new byte[0] : CreateEncoding(
                source,
                oldToNewGlyphIds,
                newToOldGlyphIds,
                charsetIdentifiers,
                limits);
        byte[] charStringIndex = CreateIndex(charStrings, limits);
        FontSubsetCffPrivateBlock[] privateBlocks = CreatePrivateBlocks(
            source,
            retainedFontDictionaries,
            closure,
            cidKeyed,
            limits);
        int[] outputFontDictionaries = cidKeyed ?
            CreateOutputFontDictionaries(
                source,
                newToOldGlyphIds,
                oldToNewFontDictionaries) : new int[0];
        byte[] fdSelect = cidKeyed ?
            CreateFontDictionarySelect(outputFontDictionaries, limits) :
            new byte[0];

        return new FontSubsetCffData(CreateTable(
            source,
            top,
            stringPlan,
            nameIndex,
            stringIndex,
            globalIndex,
            charset,
            encoding,
            predefinedEncoding,
            charStringIndex,
            retainedFontDictionaries,
            privateBlocks,
            fdSelect,
            charsetIdentifiers,
            cidKeyed,
            identityRos,
            registrySid,
            orderingSid,
            limits));
    }

    static SfntCffTable ParseStaticCffProgram(
        byte[] bytes,
        FontLimits limits)
    {
        FontData data = FontData.Create(bytes);
        SfntTableData table = new SfntTableData(
            FontTags.Cff,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                new SfntTableData[] { table },
                new FontDiagnostic[0]),
            FontLoadMode.Strict,
            limits);
        SfntCffTable? parsed = SfntCffTableParser.FindTable(cache);

        if (parsed == null)
        {
            throw FontFormatException.Create(
                "The materialized CFF2 instance is not valid CFF 1 data.");
        }

        return parsed;
    }

    static byte[] CreateTable(
        SfntCffTable source,
        CffTopDictionary top,
        FontSubsetCffStringPlan stringPlan,
        byte[] nameIndex,
        byte[] stringIndex,
        byte[] globalIndex,
        byte[] charset,
        byte[] encoding,
        int predefinedEncoding,
        byte[] charStringIndex,
        int[] retainedFontDictionaries,
        FontSubsetCffPrivateBlock[] privateBlocks,
        byte[] fdSelect,
        int[] charsetIdentifiers,
        bool cidKeyed,
        bool identityRos,
        int registrySid,
        int orderingSid,
        FontLimits limits)
    {
        byte[] provisionalTop = CreateTopDictionary(
            top,
            stringPlan,
            0,
            predefinedEncoding,
            0,
            0,
            0,
            0,
            cidKeyed ? GetCidCount(charsetIdentifiers) : 0,
            cidKeyed,
            identityRos,
            registrySid,
            orderingSid,
            limits);
        byte[] provisionalTopIndex = CreateIndex(
            new byte[][] { provisionalTop },
            limits);
        int fixedLength = CheckedLength(
            4L + nameIndex.Length + provisionalTopIndex.Length +
            stringIndex.Length + globalIndex.Length,
            limits);
        int charsetOffset = fixedLength;
        int encodingOffset = cidKeyed || predefinedEncoding >= 0 ?
            predefinedEncoding : CheckedLength(
                (long)charsetOffset + charset.Length,
                limits);
        int charStringsOffset = CheckedLength(
            (long)charsetOffset + charset.Length + encoding.Length,
            limits);
        int nextOffset = CheckedLength(
            (long)charStringsOffset + charStringIndex.Length,
            limits);
        int fdSelectOffset = cidKeyed ? nextOffset : 0;

        if (cidKeyed)
        {
            nextOffset = CheckedLength(
                (long)nextOffset + fdSelect.Length,
                limits);
        }

        int fdArrayOffset = cidKeyed ? nextOffset : 0;
        byte[] provisionalFdArray = cidKeyed ? CreateFontDictionaryArray(
            source,
            retainedFontDictionaries,
            privateBlocks,
            new int[privateBlocks.Length],
            stringPlan,
            limits) : new byte[0];

        if (cidKeyed)
        {
            nextOffset = CheckedLength(
                (long)nextOffset + provisionalFdArray.Length,
                limits);
        }

        int[] privateOffsets = new int[privateBlocks.Length];
        int blockIndex = 0;

        while (blockIndex < privateBlocks.Length)
        {
            privateOffsets[blockIndex] = nextOffset;
            nextOffset = CheckedLength(
                (long)nextOffset + privateBlocks[blockIndex].GetLength(),
                limits);
            blockIndex += 1;
        }

        byte[] fdArray = cidKeyed ? CreateFontDictionaryArray(
            source,
            retainedFontDictionaries,
            privateBlocks,
            privateOffsets,
            stringPlan,
            limits) : new byte[0];

        if (fdArray.Length != provisionalFdArray.Length)
        {
            throw FontFormatException.Create(
                "The subset CFF FDArray layout did not stabilize.");
        }

        byte[] topDictionary = CreateTopDictionary(
            top,
            stringPlan,
            charsetOffset,
            encodingOffset,
            charStringsOffset,
            cidKeyed ? 0 : privateBlocks[0].CopyDictionary().Length,
            cidKeyed ? 0 : privateOffsets[0],
            fdArrayOffset,
            cidKeyed ? GetCidCount(charsetIdentifiers) : 0,
            cidKeyed,
            identityRos,
            registrySid,
            orderingSid,
            limits,
            fdSelectOffset);
        byte[] topIndex = CreateIndex(
            new byte[][] { topDictionary },
            limits);

        if (topIndex.Length != provisionalTopIndex.Length)
        {
            throw FontFormatException.Create(
                "The subset CFF Top DICT layout did not stabilize.");
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        writer.WriteUInt8(1);
        writer.WriteUInt8(0);
        writer.WriteUInt8(4);
        writer.WriteUInt8(4);
        writer.WriteBytes(nameIndex);
        writer.WriteBytes(topIndex);
        writer.WriteBytes(stringIndex);
        writer.WriteBytes(globalIndex);
        writer.WriteBytes(charset);
        writer.WriteBytes(encoding);
        writer.WriteBytes(charStringIndex);

        if (cidKeyed)
        {
            writer.WriteBytes(fdSelect);
            writer.WriteBytes(fdArray);
        }

        blockIndex = 0;
        while (blockIndex < privateBlocks.Length)
        {
            writer.WriteBytes(privateBlocks[blockIndex].CopyDictionary());
            writer.WriteBytes(privateBlocks[blockIndex].CopySubroutines());
            blockIndex += 1;
        }

        return writer.CopyBytes();
    }

    static int[] CreateFontDictionaryMap(
        SfntCffTable source,
        int[] retainedFontDictionaries,
        bool cidKeyed)
    {
        int sourceCount = 1;
        if (cidKeyed)
        {
            CffFontDictionaryArray? array = source.GetFontDictionaryArray();
            System.ArgumentNullException.ThrowIfNull(array);
            sourceCount = array.GetCount();
        }
        int[] map = new int[sourceCount];
        System.Array.Fill(map, -1);
        int index = 0;

        while (index < retainedFontDictionaries.Length)
        {
            int oldIndex = retainedFontDictionaries[index];

            if (oldIndex < 0 || oldIndex >= sourceCount ||
                map[oldIndex] >= 0)
            {
                throw FontFormatException.Create(
                    "The CFF subset contains an invalid retained Font DICT.");
            }

            map[oldIndex] = index;
            index += 1;
        }

        if (retainedFontDictionaries.Length == 0)
        {
            throw FontFormatException.Create(
                "A CFF subset must retain at least one Font DICT context.");
        }

        return map;
    }

    static FontSubsetCffStringPlan CreateStringPlan(
        SfntCffTable source,
        CffTopDictionary top,
        int[] retainedFontDictionaries,
        int[] newToOldGlyphIds,
        bool cidKeyed)
    {
        FontSubsetCffStringPlan plan = new FontSubsetCffStringPlan();
        RetainDictionarySids(
            top.GetRawDictionary(),
            TopDictionaryKind,
            plan);
        CffCharset charset = source.GetCharset();
        int glyphIndex = 1;

        while (glyphIndex < newToOldGlyphIds.Length)
        {
            int oldGlyphId = newToOldGlyphIds[glyphIndex];

            if (!cidKeyed && oldGlyphId >= 0)
            {
                plan.RetainSid(charset.GetIdentifier(oldGlyphId));
            }
            else if (!cidKeyed)
            {
                plan.AddGenerated(".empty" + glyphIndex.ToString(
                    "D5",
                    System.Globalization.CultureInfo.InvariantCulture));
            }

            glyphIndex += 1;
        }

        if (cidKeyed)
        {
            CffFontDictionaryArray? array = source.GetFontDictionaryArray();
            int index = 0;

            while (index < retainedFontDictionaries.Length)
            {
                System.ArgumentNullException.ThrowIfNull(array);
                RetainDictionarySids(
                    array.GetRecord(retainedFontDictionaries[index])
                        .GetDictionary().GetRawDictionary(),
                    FontDictionaryKind,
                    plan);
                index += 1;
            }
        }

        return plan;
    }

    static void RetainDictionarySids(
        CffDictionary dictionary,
        int kind,
        FontSubsetCffStringPlan plan)
    {
        int entryIndex = 0;

        while (entryIndex < dictionary.GetCount())
        {
            CffDictEntry entry = dictionary.GetEntry(entryIndex);
            int operatorCode = entry.GetOperator();

            if (kind == TopDictionaryKind &&
                IsTopSingleSidOperator(operatorCode))
            {
                plan.RetainSid(entry.GetOperand(0).GetInteger());
            }
            else if (kind == TopDictionaryKind &&
                operatorCode ==
                    CffDictOperator.RegistryOrderingSupplement)
            {
                plan.RetainSid(entry.GetOperand(0).GetInteger());
                plan.RetainSid(entry.GetOperand(1).GetInteger());
            }
            else if (kind == FontDictionaryKind &&
                operatorCode == CffDictOperator.FontName)
            {
                plan.RetainSid(entry.GetOperand(0).GetInteger());
            }

            entryIndex += 1;
        }
    }

    static bool IsTopSingleSidOperator(int operatorCode)
    {
        return operatorCode == CffDictOperator.Version ||
            operatorCode == CffDictOperator.Notice ||
            operatorCode == CffDictOperator.Copyright ||
            operatorCode == CffDictOperator.FullName ||
            operatorCode == CffDictOperator.FamilyName ||
            operatorCode == CffDictOperator.Weight ||
            operatorCode == CffDictOperator.PostScript ||
            operatorCode == CffDictOperator.BaseFontName;
    }

    static int[] CreateCharsetIdentifiers(
        SfntCffTable source,
        int[] newToOldGlyphIds,
        bool cidKeyed)
    {
        CffCharset sourceCharset = source.GetCharset();
        int[] identifiers = new int[newToOldGlyphIds.Length];
        System.Collections.Generic.HashSet<int> used =
            new System.Collections.Generic.HashSet<int>();
        used.Add(0);
        int generatedIndex = 0;
        int glyphIndex = 1;

        while (glyphIndex < identifiers.Length)
        {
            int oldGlyphId = newToOldGlyphIds[glyphIndex];

            if (oldGlyphId >= 0)
            {
                identifiers[glyphIndex] =
                    sourceCharset.GetIdentifier(oldGlyphId);
                used.Add(identifiers[glyphIndex]);
            }
            else if (!cidKeyed)
            {
                identifiers[glyphIndex] = -generatedIndex - 1;
                generatedIndex += 1;
            }

            glyphIndex += 1;
        }

        if (cidKeyed)
        {
            int candidate = 1;
            glyphIndex = 1;

            while (glyphIndex < identifiers.Length)
            {
                if (newToOldGlyphIds[glyphIndex] < 0)
                {
                    while (candidate <= ushort.MaxValue &&
                        used.Contains(candidate))
                    {
                        candidate += 1;
                    }

                    if (candidate > ushort.MaxValue)
                    {
                        throw FontFormatException.Create(
                            "A preserve-GID CFF subset has no available gap CID.");
                    }

                    identifiers[glyphIndex] = candidate;
                    used.Add(candidate);
                    candidate += 1;
                }

                glyphIndex += 1;
            }
        }

        return identifiers;
    }

    static void RemapCharsetIdentifiers(
        int[] identifiers,
        int[] newToOldGlyphIds,
        FontSubsetCffStringPlan stringPlan,
        bool cidKeyed)
    {
        if (cidKeyed)
        {
            return;
        }

        int generatedIndex = 0;
        int glyphIndex = 1;

        while (glyphIndex < identifiers.Length)
        {
            if (newToOldGlyphIds[glyphIndex] < 0)
            {
                identifiers[glyphIndex] =
                    stringPlan.GetGeneratedSid(generatedIndex);
                generatedIndex += 1;
            }
            else
            {
                identifiers[glyphIndex] =
                    stringPlan.MapSid(identifiers[glyphIndex]);
            }

            glyphIndex += 1;
        }
    }

    static byte[][] CopyGlyphPrograms(
        CffIndex source,
        int[] newToOldGlyphIds)
    {
        byte[][] programs = new byte[newToOldGlyphIds.Length][];
        int index = 0;

        while (index < programs.Length)
        {
            int oldGlyphId = newToOldGlyphIds[index];
            programs[index] = oldGlyphId < 0 ?
                new byte[] { 14 } : source.CopyObject(oldGlyphId);
            index += 1;
        }

        return programs;
    }

    static byte[][] CreateSparseSubroutines(
        CffIndex? source,
        int[] retainedIndices)
    {
        if (source == null || retainedIndices.Length == 0)
        {
            return new byte[0][];
        }

        int outputCount = retainedIndices[retainedIndices.Length - 1] + 1;
        int sourceBias = Type2CharStringExecutor.GetSubroutineBias(
            source.GetCount());

        if (sourceBias == 1131 && outputCount < 1240)
        {
            outputCount = 1240;
        }
        else if (sourceBias == 32768 && outputCount < 33900)
        {
            outputCount = 33900;
        }

        byte[][] programs = new byte[outputCount][];
        int index = 0;

        while (index < programs.Length)
        {
            programs[index] = new byte[] { 11 };
            index += 1;
        }

        index = 0;
        while (index < retainedIndices.Length)
        {
            int retainedIndex = retainedIndices[index];

            if (retainedIndex < 0 || retainedIndex >= source.GetCount())
            {
                throw FontFormatException.Create(
                    "A retained CFF subroutine index is outside its source INDEX.");
            }

            programs[retainedIndex] = source.CopyObject(retainedIndex);
            index += 1;
        }

        return programs;
    }

    static int GetPredefinedEncoding(SfntCffTable source)
    {
        CffEncoding? encoding = source.GetEncoding();
        System.ArgumentNullException.ThrowIfNull(encoding);
        return encoding.IsPredefined() ? encoding.GetPredefinedValue() : -1;
    }

    static byte[] CreateEncoding(
        SfntCffTable source,
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds,
        int[] charsetIdentifiers,
        FontLimits limits)
    {
        CffEncoding? sourceEncoding = source.GetEncoding();
        bool[] mappedCodes = new bool[256];
        int[] baseCodes = new int[System.Math.Min(
            newToOldGlyphIds.Length - 1,
            255)];
        int baseCount = 0;
        int newGlyphId = 1;

        while (newGlyphId < newToOldGlyphIds.Length &&
            baseCount < baseCodes.Length)
        {
            int oldGlyphId = newToOldGlyphIds[newGlyphId];
            int code = FindEncodingCode(
                sourceEncoding,
                oldGlyphId,
                mappedCodes);

            if (code < 0)
            {
                break;
            }

            baseCodes[baseCount] = code;
            mappedCodes[code] = true;
            baseCount += 1;
            newGlyphId += 1;
        }

        System.Collections.Generic.List<int> supplementCodes =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> supplementSids =
            new System.Collections.Generic.List<int>();
        int sourceCode = 0;

        while (sourceCode < 256)
        {
            System.ArgumentNullException.ThrowIfNull(sourceEncoding);
            if (sourceEncoding.HasCode(sourceCode) &&
                !mappedCodes[sourceCode])
            {
                int oldGlyphId = sourceEncoding.GetGlyphIndex(sourceCode);
                int mappedGlyphId = oldGlyphId >= 0 &&
                    oldGlyphId < oldToNewGlyphIds.Length ?
                    oldToNewGlyphIds[oldGlyphId] : -1;

                if (mappedGlyphId > 0)
                {
                    supplementCodes.Add(sourceCode);
                    supplementSids.Add(charsetIdentifiers[mappedGlyphId]);
                }
            }

            sourceCode += 1;
        }

        if (supplementCodes.Count > 255)
        {
            throw FontFormatException.Create(
                "A subset CFF Encoding requires more than 255 supplements.");
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        writer.WriteUInt8((byte)(supplementCodes.Count == 0 ? 0 : 0x80));
        writer.WriteUInt8((byte)baseCount);
        int index = 0;

        while (index < baseCount)
        {
            writer.WriteUInt8((byte)baseCodes[index]);
            index += 1;
        }

        if (supplementCodes.Count != 0)
        {
            writer.WriteUInt8((byte)supplementCodes.Count);
            index = 0;

            while (index < supplementCodes.Count)
            {
                writer.WriteUInt8((byte)supplementCodes[index]);
                writer.WriteUInt16((ushort)supplementSids[index]);
                index += 1;
            }
        }

        return writer.CopyBytes();
    }

    static int FindEncodingCode(
        CffEncoding? encoding,
        int glyphId,
        bool[] unavailable)
    {
        if (glyphId < 0)
        {
            return -1;
        }

        int code = 0;
        System.ArgumentNullException.ThrowIfNull(encoding);

        while (code < 256)
        {
            if (!unavailable[code] && encoding.HasCode(code) &&
                encoding.GetGlyphIndex(code) == glyphId)
            {
                return code;
            }

            code += 1;
        }

        return -1;
    }

    static byte[] CreateCharset(int[] identifiers, FontLimits limits)
    {
        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        writer.WriteUInt8(0);
        int index = 1;

        while (index < identifiers.Length)
        {
            if (identifiers[index] < 0 ||
                identifiers[index] > ushort.MaxValue)
            {
                throw FontFormatException.Create(
                    "A subset CFF charset identifier exceeds UInt16.");
            }

            writer.WriteUInt16((ushort)identifiers[index]);
            index += 1;
        }

        return writer.CopyBytes();
    }

    static int[] CreateOutputFontDictionaries(
        SfntCffTable source,
        int[] newToOldGlyphIds,
        int[] oldToNewFontDictionaries)
    {
        int[] output = new int[newToOldGlyphIds.Length];
        int fallback = oldToNewFontDictionaries[
            source.GetFontDictionaryIndexForGlyph(0)];
        int glyphIndex = 0;

        while (glyphIndex < output.Length)
        {
            int oldGlyphId = newToOldGlyphIds[glyphIndex];
            output[glyphIndex] = oldGlyphId < 0 ? fallback :
                oldToNewFontDictionaries[
                    source.GetFontDictionaryIndexForGlyph(oldGlyphId)];

            if (output[glyphIndex] < 0 || output[glyphIndex] > 255)
            {
                throw FontFormatException.Create(
                    "A subset CFF glyph references an omitted Font DICT.");
            }

            glyphIndex += 1;
        }

        return output;
    }

    static byte[] CreateFontDictionarySelect(
        int[] fontDictionaries,
        FontLimits limits)
    {
        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        writer.WriteUInt8(0);
        int index = 0;

        while (index < fontDictionaries.Length)
        {
            writer.WriteUInt8((byte)fontDictionaries[index]);
            index += 1;
        }

        return writer.CopyBytes();
    }

    static FontSubsetCffPrivateBlock[] CreatePrivateBlocks(
        SfntCffTable source,
        int[] retainedFontDictionaries,
        FontSubsetCffClosure closure,
        bool cidKeyed,
        FontLimits limits)
    {
        FontSubsetCffPrivateBlock[] blocks =
            new FontSubsetCffPrivateBlock[
                retainedFontDictionaries.Length];
        FontSubsetCffLocalSubroutineClosure[] localClosures =
            closure.CopyLocalSubroutines();
        int index = 0;

        while (index < blocks.Length)
        {
            int oldFontDictionaryIndex = retainedFontDictionaries[index];
            CffPrivateDictionary? privateDictionary;
            if (cidKeyed)
            {
                CffFontDictionaryArray? array = source.GetFontDictionaryArray();
                System.ArgumentNullException.ThrowIfNull(array);
                privateDictionary = array.GetRecord(oldFontDictionaryIndex)
                    .GetPrivateDictionary();
            }
            else
            {
                privateDictionary = source.GetPrivateDictionary();
            }
            CffIndex? sourceSubroutines =
                source.GetLocalSubroutineIndexForSubsetting(
                    oldFontDictionaryIndex);
            int[] retainedSubroutines = FindLocalSubroutines(
                localClosures,
                oldFontDictionaryIndex);
            byte[][] programs = CreateSparseSubroutines(
                sourceSubroutines,
                retainedSubroutines);
            byte[] subroutineIndex = CreateIndex(programs, limits);
            bool hasSubroutines = programs.Length != 0;
            System.ArgumentNullException.ThrowIfNull(privateDictionary);
            byte[] provisional = CreatePrivateDictionary(
                privateDictionary.GetRawDictionary(),
                hasSubroutines,
                0,
                limits);
            byte[] dictionary = CreatePrivateDictionary(
                privateDictionary.GetRawDictionary(),
                hasSubroutines,
                provisional.Length,
                limits);

            if (dictionary.Length != provisional.Length)
            {
                throw FontFormatException.Create(
                    "The subset CFF Private DICT layout did not stabilize.");
            }

            blocks[index] = new FontSubsetCffPrivateBlock(
                dictionary,
                hasSubroutines ? subroutineIndex : new byte[0]);
            index += 1;
        }

        return blocks;
    }

    static int[] FindLocalSubroutines(
        FontSubsetCffLocalSubroutineClosure[] closures,
        int fontDictionaryIndex)
    {
        int index = 0;

        while (index < closures.Length)
        {
            if (closures[index].GetFontDictionaryIndex() ==
                fontDictionaryIndex)
            {
                return closures[index].CopySubroutineIndices();
            }

            index += 1;
        }

        return new int[0];
    }

    static int GetCidCount(int[] identifiers)
    {
        int maximum = 0;
        int index = 0;

        while (index < identifiers.Length)
        {
            maximum = System.Math.Max(maximum, identifiers[index]);
            index += 1;
        }

        if (maximum == ushort.MaxValue)
        {
            return 65536;
        }

        return maximum + 1;
    }

    static int[] ValidatePdfCharsetIdentifiers(
        int[] values,
        int glyphCount)
    {
        if (values.Length != glyphCount || values.Length == 0 ||
            values[0] != 0)
        {
            throw new System.ArgumentException(
                "A PDF CFF charset must contain one CID per glyph and reserve CID zero.",
                "charsetIdentifiers");
        }

        int[] copy = new int[values.Length];
        System.Collections.Generic.HashSet<int> used =
            new System.Collections.Generic.HashSet<int>();
        int index = 0;

        while (index < values.Length)
        {
            int cid = values[index];

            if (cid < 0 || cid > ushort.MaxValue || !used.Add(cid))
            {
                throw new System.ArgumentException(
                    "A PDF CFF charset must contain unique 16-bit CIDs.",
                    "charsetIdentifiers");
            }

            copy[index] = cid;
            index += 1;
        }

        return copy;
    }

    static byte[] CreateTopDictionary(
        CffTopDictionary source,
        FontSubsetCffStringPlan stringPlan,
        int charsetOffset,
        int encodingOffset,
        int charStringsOffset,
        int privateSize,
        int privateOffset,
        int fdArrayOffset,
        int cidCount,
        bool cidKeyed,
        bool identityRos,
        int registrySid,
        int orderingSid,
        FontLimits limits)
    {
        return CreateTopDictionary(
            source,
            stringPlan,
            charsetOffset,
            encodingOffset,
            charStringsOffset,
            privateSize,
            privateOffset,
            fdArrayOffset,
            cidCount,
            cidKeyed,
            identityRos,
            registrySid,
            orderingSid,
            limits,
            0);
    }

    static byte[] CreateTopDictionary(
        CffTopDictionary source,
        FontSubsetCffStringPlan stringPlan,
        int charsetOffset,
        int encodingOffset,
        int charStringsOffset,
        int privateSize,
        int privateOffset,
        int fdArrayOffset,
        int cidCount,
        bool cidKeyed,
        bool identityRos,
        int registrySid,
        int orderingSid,
        FontLimits limits,
        int fdSelectOffset)
    {
        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        CffDictionary dictionary = source.GetRawDictionary();
        int index = 0;

        while (index < dictionary.GetCount())
        {
            CffDictEntry entry = dictionary.GetEntry(index);
            int operatorCode = entry.GetOperator();

            if (!IsReplacedTopOperator(
                operatorCode,
                cidKeyed,
                identityRos))
            {
                WriteDictionaryEntry(
                    writer,
                    entry,
                    TopDictionaryKind,
                    stringPlan);
            }

            index += 1;
        }

        if (identityRos)
        {
            WriteDictionaryInteger(writer, registrySid);
            WriteDictionaryInteger(writer, orderingSid);
            WriteDictionaryInteger(writer, 0);
            WriteDictionaryOperator(
                writer,
                CffDictOperator.RegistryOrderingSupplement);
        }

        WriteDictionaryInteger(writer, charsetOffset);
        WriteDictionaryOperator(writer, CffDictOperator.Charset);

        if (!cidKeyed)
        {
            WriteDictionaryInteger(writer, encodingOffset);
            WriteDictionaryOperator(writer, CffDictOperator.Encoding);
        }

        WriteDictionaryInteger(writer, charStringsOffset);
        WriteDictionaryOperator(writer, CffDictOperator.CharStrings);

        if (cidKeyed)
        {
            WriteDictionaryInteger(writer, cidCount);
            WriteDictionaryOperator(writer, CffDictOperator.CidCount);
            WriteDictionaryInteger(writer, fdArrayOffset);
            WriteDictionaryOperator(
                writer,
                CffDictOperator.FontDictionaryArray);
            WriteDictionaryInteger(writer, fdSelectOffset);
            WriteDictionaryOperator(
                writer,
                CffDictOperator.FontDictionarySelect);
        }
        else
        {
            WriteDictionaryInteger(writer, privateSize);
            WriteDictionaryInteger(writer, privateOffset);
            WriteDictionaryOperator(writer, CffDictOperator.Private);
        }

        return writer.CopyBytes();
    }

    static bool IsReplacedTopOperator(
        int operatorCode,
        bool cidKeyed,
        bool identityRos)
    {
        return operatorCode == CffDictOperator.Charset ||
            operatorCode == CffDictOperator.Encoding ||
            operatorCode == CffDictOperator.CharStrings ||
            operatorCode == CffDictOperator.Private ||
            operatorCode == CffDictOperator.FontDictionaryArray ||
            operatorCode == CffDictOperator.FontDictionarySelect ||
            (identityRos && operatorCode ==
                CffDictOperator.RegistryOrderingSupplement) ||
            (cidKeyed && operatorCode == CffDictOperator.CidCount);
    }

    static byte[] CreateFontDictionaryArray(
        SfntCffTable source,
        int[] retainedFontDictionaries,
        FontSubsetCffPrivateBlock[] privateBlocks,
        int[] privateOffsets,
        FontSubsetCffStringPlan stringPlan,
        FontLimits limits)
    {
        byte[][] dictionaries =
            new byte[retainedFontDictionaries.Length][];
        CffFontDictionaryArray? sourceArray =
            source.GetFontDictionaryArray();
        int index = 0;

        while (index < dictionaries.Length)
        {
            System.ArgumentNullException.ThrowIfNull(sourceArray);
            CffDictionary sourceDictionary = sourceArray
                .GetRecord(retainedFontDictionaries[index])
                .GetDictionary().GetRawDictionary();
            BigEndianWriter writer = BigEndianWriter.Create(
                limits.GetMaxOutputBytes());
            int entryIndex = 0;

            while (entryIndex < sourceDictionary.GetCount())
            {
                CffDictEntry entry =
                    sourceDictionary.GetEntry(entryIndex);

                if (entry.GetOperator() != CffDictOperator.Private)
                {
                    WriteDictionaryEntry(
                        writer,
                        entry,
                        FontDictionaryKind,
                        stringPlan);
                }

                entryIndex += 1;
            }

            WriteDictionaryInteger(
                writer,
                privateBlocks[index].CopyDictionary().Length);
            WriteDictionaryInteger(writer, privateOffsets[index]);
            WriteDictionaryOperator(writer, CffDictOperator.Private);
            dictionaries[index] = writer.CopyBytes();
            index += 1;
        }

        return CreateIndex(dictionaries, limits);
    }

    static byte[] CreatePrivateDictionary(
        CffDictionary source,
        bool hasSubroutines,
        int subroutineOffset,
        FontLimits limits)
    {
        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        int index = 0;

        while (index < source.GetCount())
        {
            CffDictEntry entry = source.GetEntry(index);

            if (entry.GetOperator() != CffDictOperator.LocalSubroutines)
            {
                WriteDictionaryEntry(
                    writer,
                    entry,
                    PrivateDictionaryKind,
                    null);
            }

            index += 1;
        }

        if (hasSubroutines)
        {
            WriteDictionaryInteger(writer, subroutineOffset);
            WriteDictionaryOperator(
                writer,
                CffDictOperator.LocalSubroutines);
        }

        return writer.CopyBytes();
    }

    static void WriteDictionaryEntry(
        BigEndianWriter writer,
        CffDictEntry entry,
        int kind,
        FontSubsetCffStringPlan? stringPlan)
    {
        int operandIndex = 0;

        while (operandIndex < entry.GetOperandCount())
        {
            CffDictOperand operand = entry.GetOperand(operandIndex);

            if (IsSidOperand(
                entry.GetOperator(),
                operandIndex,
                kind))
            {
                System.ArgumentNullException.ThrowIfNull(stringPlan);
                WriteDictionaryInteger(
                    writer,
                    stringPlan.MapSid(operand.GetInteger()));
            }
            else if (operand.IsInteger())
            {
                WriteDictionaryInteger(writer, operand.GetInteger());
            }
            else
            {
                WriteDictionaryReal(writer, operand.GetNumber());
            }

            operandIndex += 1;
        }

        WriteDictionaryOperator(writer, entry.GetOperator());
    }

    static bool IsSidOperand(
        int operatorCode,
        int operandIndex,
        int kind)
    {
        if (kind == FontDictionaryKind &&
            operatorCode == CffDictOperator.FontName)
        {
            return true;
        }

        if (kind != TopDictionaryKind)
        {
            return false;
        }

        if (IsTopSingleSidOperator(operatorCode))
        {
            return true;
        }

        return operatorCode ==
            CffDictOperator.RegistryOrderingSupplement &&
            operandIndex < 2;
    }

    static byte[] CreateIndex(byte[][] objects, FontLimits limits)
    {
        if (objects.Length > ushort.MaxValue)
        {
            throw FontFormatException.Create(
                "A subset CFF INDEX contains too many objects.");
        }

        long maximumOffset = 1;
        int index = 0;

        while (index < objects.Length)
        {
            if (objects[index] == null)
            {
                throw FontFormatException.Create(
                    "A subset CFF INDEX object is unavailable.");
            }

            maximumOffset += objects[index].Length;
            index += 1;
        }

        int boundedMaximumOffset = CheckedLength(maximumOffset, limits);
        int offsetSize = GetOffsetSize(boundedMaximumOffset);
        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        writer.WriteUInt16((ushort)objects.Length);

        if (objects.Length == 0)
        {
            return writer.CopyBytes();
        }

        writer.WriteUInt8((byte)offsetSize);
        int offset = 1;
        WriteOffset(writer, offset, offsetSize);
        index = 0;

        while (index < objects.Length)
        {
            offset += objects[index].Length;
            WriteOffset(writer, offset, offsetSize);
            index += 1;
        }

        index = 0;
        while (index < objects.Length)
        {
            writer.WriteBytes(objects[index]);
            index += 1;
        }

        return writer.CopyBytes();
    }

    static int GetOffsetSize(int value)
    {
        if (value <= byte.MaxValue) { return 1; }
        if (value <= ushort.MaxValue) { return 2; }
        if (value <= 0x00FFFFFF) { return 3; }
        return 4;
    }

    static void WriteOffset(
        BigEndianWriter writer,
        int offset,
        int offsetSize)
    {
        if (offsetSize == 1) { writer.WriteUInt8((byte)offset); return; }
        if (offsetSize == 2) { writer.WriteUInt16((ushort)offset); return; }
        if (offsetSize == 3) { writer.WriteUInt24((uint)offset); return; }
        writer.WriteUInt32((uint)offset);
    }

    static void WriteDictionaryInteger(BigEndianWriter writer, int value)
    {
        writer.WriteUInt8(29);
        writer.WriteInt32(value);
    }

    static void WriteDictionaryOperator(
        BigEndianWriter writer,
        int operatorCode)
    {
        if (operatorCode >= 0x0C00)
        {
            writer.WriteUInt8(12);
            writer.WriteUInt8((byte)(operatorCode & 255));
        }
        else
        {
            writer.WriteUInt8((byte)operatorCode);
        }
    }

    static void WriteDictionaryReal(
        BigEndianWriter writer,
        double value)
    {
        if (!double.IsFinite(value))
        {
            throw FontFormatException.Create(
                "A subset CFF DICT real operand is not finite.");
        }

        string text = value.ToString(
            "R",
            System.Globalization.CultureInfo.InvariantCulture);
        writer.WriteUInt8(30);
        int highNibble = -1;
        int index = 0;

        while (index < text.Length)
        {
            char character = text[index];
            int nibble;

            if ((character == 'E' || character == 'e') &&
                index + 1 < text.Length && text[index + 1] == '-')
            {
                nibble = 12;
                index += 1;
            }
            else if ((character == 'E' || character == 'e') &&
                index + 1 < text.Length && text[index + 1] == '+')
            {
                nibble = 11;
                index += 1;
            }
            else
            {
                nibble = GetRealNibble(character);
            }

            if (highNibble < 0)
            {
                highNibble = nibble;
            }
            else
            {
                writer.WriteUInt8((byte)(highNibble * 16 + nibble));
                highNibble = -1;
            }

            index += 1;
        }

        if (highNibble < 0)
        {
            writer.WriteUInt8(0xFF);
        }
        else
        {
            writer.WriteUInt8((byte)(highNibble * 16 + 15));
        }
    }

    static int GetRealNibble(char value)
    {
        if (value >= '0' && value <= '9') { return value - '0'; }
        if (value == '.') { return 10; }
        if (value == 'E' || value == 'e') { return 11; }
        if (value == '-') { return 14; }
        throw FontFormatException.Create(
            "A subset CFF DICT real operand has unsupported syntax.");
    }

    static int CheckedLength(long length, FontLimits limits)
    {
        if (length < 0 || length > limits.GetMaxOutputBytes() ||
            length > int.MaxValue)
        {
            throw FontLimitException.Create(
                "The subset CFF table exceeds the configured output limit.");
        }

        return (int)length;
    }
}
