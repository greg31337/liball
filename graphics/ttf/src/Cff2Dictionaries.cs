namespace ttf;

internal class Cff2TopDictionary
{
    CffDictionaryReader reader;

    internal Cff2TopDictionary(CffDictionary dictionary)
    {
        reader = new CffDictionaryReader(dictionary, null);
        Validate();
    }

    internal static Cff2TopDictionary Parse(FontDataSlice data)
    {
        return new Cff2TopDictionary(CffDictParser.ParseCff2(data));
    }

    internal CffDictionary GetRawDictionary()
    {
        return reader.GetDictionary();
    }

    internal int GetCharStringsOffset()
    {
        return reader.ReadRequiredInteger(
            CffDictOperator.CharStrings,
            "CharStrings");
    }

    internal bool HasVariationStore()
    {
        return reader.Has(CffDictOperator.VariationStore);
    }

    internal int GetVariationStoreOffset()
    {
        return reader.ReadRequiredInteger(
            CffDictOperator.VariationStore,
            "vstore");
    }

    internal int GetFontDictionaryArrayOffset()
    {
        return reader.ReadRequiredInteger(
            CffDictOperator.FontDictionaryArray,
            "FDArray");
    }

    internal bool HasFontDictionarySelect()
    {
        return reader.Has(CffDictOperator.FontDictionarySelect);
    }

    internal int GetFontDictionarySelectOffset()
    {
        return reader.ReadRequiredInteger(
            CffDictOperator.FontDictionarySelect,
            "FDSelect");
    }

    internal double[] CopyFontMatrix()
    {
        return reader.CopyNumbers(
            CffDictOperator.FontMatrix,
            new double[] { 0.001, 0, 0, 0.001, 0, 0 },
            6,
            6);
    }

    void Validate()
    {
        ValidateNonnegative(GetCharStringsOffset());
        ValidateNonnegative(GetFontDictionaryArrayOffset());

        if (HasVariationStore())
        {
            ValidateNonnegative(GetVariationStoreOffset());
        }

        if (HasFontDictionarySelect())
        {
            ValidateNonnegative(GetFontDictionarySelectOffset());
        }

        CffDictEntry? matrix = reader.Find(CffDictOperator.FontMatrix);

        if (matrix != null)
        {
            reader.ValidateOperandCount(matrix, 6, 6);
            int index = 0;

            while (index < 6)
            {
                if (matrix.GetOperand(index).IsInteger())
                {
                    throw FontFormatException.Create(
                        "A CFF2 FontMatrix operand must use real-number encoding.");
                }

                index += 1;
            }

            double[] values = matrix.CopyNumbers();

            if (values[0] <= 0 || values[3] != values[0] ||
                values[1] != 0 || values[2] != 0 ||
                values[4] != 0 || values[5] != 0)
            {
                throw FontFormatException.Create(
                    "A CFF2 FontMatrix is not a supported uniform scale matrix.");
            }
        }
    }

    void ValidateNonnegative(int value)
    {
        if (value < 0)
        {
            throw FontFormatException.Create(
                "A CFF2 dictionary offset must not be negative.");
        }
    }
}

internal class Cff2FontDictionary
{
    int privateSize;
    int privateOffset;

    internal Cff2FontDictionary(CffDictionary dictionary)
    {
        CffDictionaryReader reader = new CffDictionaryReader(dictionary, null);
        CffDictEntry entry = reader.Require(
            CffDictOperator.Private,
            "Private");
        reader.ValidateOperandCount(entry, 2, 2);
        privateSize = entry.GetOperand(0).GetInteger();
        privateOffset = entry.GetOperand(1).GetInteger();

        if (privateSize < 0 || privateOffset < 0 ||
            (privateSize == 0 && privateOffset != 0))
        {
            throw FontFormatException.Create(
                "A CFF2 Private DICT size and offset pair is invalid.");
        }
    }

    internal static Cff2FontDictionary Parse(FontDataSlice data)
    {
        return new Cff2FontDictionary(CffDictParser.ParseCff2(data));
    }

    internal int GetPrivateDictionarySize()
    {
        return privateSize;
    }

    internal int GetPrivateDictionaryOffset()
    {
        return privateOffset;
    }
}

internal class Cff2PrivateDictionary
{
    FontDataSlice? data;
    CffDictionaryReader? reader;
    bool sourceHadBlend;
    bool evaluated;

    Cff2PrivateDictionary(
        FontDataSlice? data,
        CffDictionary dictionary,
        bool sourceHadBlend,
        bool evaluated)
    {
        Initialize(data, dictionary, sourceHadBlend, evaluated);
    }

    internal static Cff2PrivateDictionary Create(
        FontDataSlice data,
        CffDictionary dictionary)
    {
        return new Cff2PrivateDictionary(data, dictionary, false, false);
    }

    static Cff2PrivateDictionary Create(
        FontDataSlice? data,
        CffDictionary dictionary,
        bool sourceHadBlend,
        bool evaluated)
    {
        return new Cff2PrivateDictionary(
            data,
            dictionary,
            sourceHadBlend,
            evaluated);
    }

    void Initialize(
        FontDataSlice? data,
        CffDictionary dictionary,
        bool sourceHadBlend,
        bool evaluated)
    {
        this.data = data;
        reader = new CffDictionaryReader(dictionary, null);
        this.sourceHadBlend = sourceHadBlend ||
            dictionary.GetEntryCount(CffDictOperator.Blend) != 0;
        this.evaluated = evaluated;
        Validate();
    }

    internal static Cff2PrivateDictionary Parse(FontDataSlice data)
    {
        return Cff2PrivateDictionary.Create(data, CffDictParser.ParseCff2(data));
    }

    internal FontDataSlice? GetData()
    {
        return data;
    }

    internal bool HasLocalSubroutines()
    {
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.Has(CffDictOperator.LocalSubroutines);
    }

    internal int GetLocalSubroutinesOffset()
    {
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.ReadRequiredInteger(
            CffDictOperator.LocalSubroutines,
            "Subrs");
    }

    internal bool HasVariationStoreDataIndex()
    {
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.Has(CffDictOperator.VariationStoreDataIndex);
    }

    internal int GetVariationStoreDataIndex()
    {
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.ReadInteger(
            CffDictOperator.VariationStoreDataIndex,
            0);
    }

    internal bool HasBlend()
    {
        return sourceHadBlend;
    }

    internal bool IsEvaluated()
    {
        return evaluated;
    }

    internal Cff2PrivateDictionary Evaluate(
        Cff2VariationStore? store,
        double[] normalizedCoordinates)
    {
        CffDictionary dictionary = Cff2PrivateDictionaryEvaluator.Evaluate(
            data,
            store,
            normalizedCoordinates);
        return Cff2PrivateDictionary.Create(
            data,
            dictionary,
            HasBlend(),
            true);
    }

    internal double[] CopyBlueValues()
    {
        RequireEvaluatedValues();
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.CopyDeltaValues(CffDictOperator.BlueValues, 0, 14);
    }

    internal double[] CopyOtherBlues()
    {
        RequireEvaluatedValues();
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.CopyDeltaValues(CffDictOperator.OtherBlues, 0, 10);
    }

    internal double[] CopyFamilyBlues()
    {
        RequireEvaluatedValues();
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.CopyDeltaValues(CffDictOperator.FamilyBlues, 0, 14);
    }

    internal double[] CopyFamilyOtherBlues()
    {
        RequireEvaluatedValues();
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.CopyDeltaValues(CffDictOperator.FamilyOtherBlues, 0, 10);
    }

    internal double GetBlueScale()
    {
        RequireEvaluatedValues();
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.ReadNumber(CffDictOperator.BlueScale, 0.039625);
    }

    internal double GetBlueShift()
    {
        RequireEvaluatedValues();
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.ReadNumber(CffDictOperator.BlueShift, 7);
    }

    internal double GetBlueFuzz()
    {
        RequireEvaluatedValues();
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.ReadNumber(CffDictOperator.BlueFuzz, 1);
    }

    internal bool HasStandardHorizontalWidth()
    {
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.Has(CffDictOperator.StandardHorizontalWidth);
    }

    internal double GetStandardHorizontalWidth()
    {
        RequireEvaluatedValues();
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.ReadNumber(CffDictOperator.StandardHorizontalWidth, 0);
    }

    internal bool HasStandardVerticalWidth()
    {
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.Has(CffDictOperator.StandardVerticalWidth);
    }

    internal double GetStandardVerticalWidth()
    {
        RequireEvaluatedValues();
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.ReadNumber(CffDictOperator.StandardVerticalWidth, 0);
    }

    internal double[] CopyStemSnapHorizontal()
    {
        RequireEvaluatedValues();
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.CopyDeltaValues(CffDictOperator.StemSnapHorizontal, 1, 12);
    }

    internal double[] CopyStemSnapVertical()
    {
        RequireEvaluatedValues();
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.CopyDeltaValues(CffDictOperator.StemSnapVertical, 1, 12);
    }

    internal int GetLanguageGroup()
    {
        RequireEvaluatedValues();
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.ReadInteger(CffDictOperator.LanguageGroup, 0);
    }

    internal double GetExpansionFactor()
    {
        RequireEvaluatedValues();
        System.ArgumentNullException.ThrowIfNull(reader);
        return reader.ReadNumber(CffDictOperator.ExpansionFactor, 0.06);
    }

    void Validate()
    {
        if (HasLocalSubroutines() && GetLocalSubroutinesOffset() < 0)
        {
            throw FontFormatException.Create(
                "A CFF2 local subroutine offset must not be negative.");
        }

        if (GetVariationStoreDataIndex() < 0)
        {
            throw FontFormatException.Create(
                "A CFF2 vsindex value must not be negative.");
        }

        if (!HasBlend() || evaluated)
        {
            ValidateEvenDelta(CffDictOperator.BlueValues, 14);
            ValidateEvenDelta(CffDictOperator.OtherBlues, 10);
            ValidateEvenDelta(CffDictOperator.FamilyBlues, 14);
            ValidateEvenDelta(CffDictOperator.FamilyOtherBlues, 10);
            GetBlueScale();
            GetBlueShift();
            GetBlueFuzz();

            if (HasStandardHorizontalWidth())
            {
                GetStandardHorizontalWidth();
            }

            if (HasStandardVerticalWidth())
            {
                GetStandardVerticalWidth();
            }

            CopyStemSnapHorizontal();
            CopyStemSnapVertical();
            GetLanguageGroup();
            GetExpansionFactor();
        }
    }

    void ValidateEvenDelta(int operatorCode, int maximumCount)
    {
        System.ArgumentNullException.ThrowIfNull(reader);
        CffDictEntry? entry = reader.Find(operatorCode);

        if (entry == null)
        {
            return;
        }

        reader.ValidateOperandCount(entry, 2, maximumCount);

        if (entry.GetOperandCount() % 2 != 0)
        {
            throw FontFormatException.Create(
                "A CFF2 blue-zone delta array must contain coordinate pairs.");
        }
    }

    void RequireEvaluatedValues()
    {
        if (HasBlend() && !evaluated)
        {
            throw new System.InvalidOperationException(
                "CFF2 blended Private DICT values require instance evaluation.");
        }
    }
}

internal static class Cff2PrivateDictionaryEvaluator
{
    const int MaximumOperandCount = 513;

    internal static CffDictionary Evaluate(
        FontDataSlice? data,
        Cff2VariationStore? store,
        double[] normalizedCoordinates)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        Cff2VariationLocation? location = null;

        if (store != null)
        {
            location = store.CreateLocation(normalizedCoordinates);
        }
        else if (normalizedCoordinates == null ||
            normalizedCoordinates.Length != 0)
        {
            throw new System.ArgumentException(
                "A CFF2 font without a VariationStore requires no coordinates.",
                "normalizedCoordinates");
        }

        BigEndianReader reader = new BigEndianReader(data);
        System.Collections.Generic.List<CffDictEntry> entries =
            new System.Collections.Generic.List<CffDictEntry>();
        System.Collections.Generic.List<CffDictOperand> operands =
            new System.Collections.Generic.List<CffDictOperand>();
        int variationDataIndex = 0;
        bool variationDataIndexSeen = false;
        bool blendSeen = false;

        while (reader.GetRemainingLength() != 0)
        {
            int firstByte = reader.ReadUInt8();

            if (firstByte <= 27)
            {
                int operatorCode = firstByte;

                if (firstByte == 12)
                {
                    operatorCode = CffDictOperator.CreateEscaped(
                        reader.ReadUInt8());
                }

                if (operatorCode == CffDictOperator.Blend)
                {
                    if (store == null)
                    {
                        throw FontFormatException.Create(
                            "A CFF2 blend operator requires a VariationStore.");
                    }

                    System.ArgumentNullException.ThrowIfNull(location);
                    Blend(
                        operands,
                        location.CopyRegionScalars(variationDataIndex));
                    blendSeen = true;
                    continue;
                }

                if (operatorCode == CffDictOperator.VariationStoreDataIndex)
                {
                    if (variationDataIndexSeen || blendSeen ||
                        operands.Count != 1)
                    {
                        throw FontFormatException.Create(
                            "A CFF2 Private DICT has an invalid vsindex operator.");
                    }

                    variationDataIndex = operands[0].GetInteger();

                    if (store == null)
                    {
                        throw FontFormatException.Create(
                            "A CFF2 vsindex operator requires a VariationStore.");
                    }

                    store.GetVariationData(variationDataIndex);
                    variationDataIndexSeen = true;
                }

                entries.Add(new CffDictEntry(
                    operatorCode,
                    operands.ToArray()));
                operands.Clear();
                continue;
            }

            AddNumber(operands, DecodeOperand(firstByte, reader));
        }

        if (operands.Count != 0)
        {
            throw FontFormatException.Create(
                "A CFF2 Private DICT ends with unused operands.");
        }

        return new CffDictionary(entries.ToArray());
    }

    static void Blend(
        System.Collections.Generic.List<CffDictOperand> operands,
        double[] scalars)
    {
        if (operands.Count == 0)
        {
            throw FontFormatException.Create(
                "A CFF2 blend operator has no operand count.");
        }

        int blendCount = operands[operands.Count - 1].GetInteger();

        if (blendCount <= 0)
        {
            throw FontFormatException.Create(
                "A CFF2 blend operator has an invalid operand count.");
        }

        long required = (long)blendCount * (scalars.Length + 1) + 1;

        if (required > operands.Count)
        {
            throw FontFormatException.Create(
                "A CFF2 blend operator does not have enough operands.");
        }

        int start = operands.Count - (int)required;
        CffDictOperand[] results = new CffDictOperand[blendCount];
        int valueIndex = 0;

        while (valueIndex < blendCount)
        {
            double value = operands[start + valueIndex].GetNumber();
            int scalarIndex = 0;

            while (scalarIndex < scalars.Length)
            {
                int deltaIndex = start + blendCount +
                    valueIndex * scalars.Length + scalarIndex;
                value += operands[deltaIndex].GetNumber() *
                    scalars[scalarIndex];
                scalarIndex += 1;
            }

            if (!double.IsFinite(value))
            {
                throw FontFormatException.Create(
                    "A CFF2 blended value is outside the supported range.");
            }

            results[valueIndex] = CffDictOperand.CreateReal(value);
            valueIndex += 1;
        }

        operands.RemoveRange(start, (int)required);
        operands.AddRange(results);
    }

    static CffDictOperand DecodeOperand(
        int firstByte,
        BigEndianReader reader)
    {
        if (firstByte == 28)
        {
            return CffDictOperand.CreateInteger(reader.ReadInt16());
        }

        if (firstByte == 29)
        {
            return CffDictOperand.CreateInteger(reader.ReadInt32());
        }

        if (firstByte == 30)
        {
            CffRealDecoder decoder = new CffRealDecoder();
            return CffDictOperand.CreateReal(decoder.Read(reader));
        }

        if (firstByte >= 32 && firstByte <= 246)
        {
            return CffDictOperand.CreateInteger(firstByte - 139);
        }

        if (firstByte >= 247 && firstByte <= 250)
        {
            return CffDictOperand.CreateInteger(
                (firstByte - 247) * 256 + reader.ReadUInt8() + 108);
        }

        if (firstByte >= 251 && firstByte <= 254)
        {
            return CffDictOperand.CreateInteger(
                -((firstByte - 251) * 256) - reader.ReadUInt8() - 108);
        }

        throw FontFormatException.Create(
            "A CFF2 Private DICT contains a reserved byte.");
    }

    static void AddNumber(
        System.Collections.Generic.List<CffDictOperand> operands,
        CffDictOperand operand)
    {
        if (operands.Count >= MaximumOperandCount)
        {
            throw FontLimitException.Create(
                "A CFF2 Private DICT operand stack exceeds 513 values.");
        }

        operands.Add(operand);
    }
}

internal class Cff2FontDictionaryRecord
{
    Cff2FontDictionary fontDictionary;
    Cff2PrivateDictionary privateDictionary;

    internal Cff2FontDictionaryRecord(
        Cff2FontDictionary fontDictionary,
        Cff2PrivateDictionary privateDictionary)
    {
        this.fontDictionary = fontDictionary;
        this.privateDictionary = privateDictionary;
    }

    internal Cff2FontDictionary GetFontDictionary()
    {
        return fontDictionary;
    }

    internal Cff2PrivateDictionary GetPrivateDictionary()
    {
        return privateDictionary;
    }
}

internal class Cff2FontDictionaryArray
{
    Cff2Index index;
    Cff2FontDictionaryRecord[] records;

    internal Cff2FontDictionaryArray(
        Cff2Index index,
        Cff2FontDictionaryRecord[] records)
    {
        this.index = index;
        this.records = records;
    }

    internal Cff2Index GetIndex() { return index; }
    internal int GetCount() { return records.Length; }

    internal Cff2FontDictionaryRecord GetRecord(int indexValue)
    {
        if (indexValue < 0 || indexValue >= records.Length)
        {
            throw new System.ArgumentOutOfRangeException("indexValue");
        }

        return records[indexValue];
    }
}

internal static class Cff2FontDictionaryArrayParser
{
    internal static Cff2FontDictionaryArray Parse(
        FontDataSlice data,
        int offset,
        int maximumCount)
    {
        Cff2Index index = Cff2IndexParser.Parse(data, offset, maximumCount);

        if (index.GetCount() == 0)
        {
            throw FontFormatException.Create(
                "A CFF2 FDArray INDEX must not be empty.");
        }

        Cff2FontDictionaryRecord[] records =
            new Cff2FontDictionaryRecord[index.GetCount()];
        int recordIndex = 0;

        while (recordIndex < records.Length)
        {
            Cff2FontDictionary fontDictionary =
                Cff2FontDictionary.Parse(index.GetObject(recordIndex));
            int size = fontDictionary.GetPrivateDictionarySize();
            int privateOffset = fontDictionary.GetPrivateDictionaryOffset();
            int end = CheckedFontMath.AddOffset(privateOffset, size);

            if (end > data.GetLength())
            {
                throw FontFormatException.Create(
                    "A CFF2 Private DICT extends outside the bounded table.");
            }

            Cff2PrivateDictionary privateDictionary =
                Cff2PrivateDictionary.Parse(data.GetSlice(privateOffset, size));
            records[recordIndex] = new Cff2FontDictionaryRecord(
                fontDictionary,
                privateDictionary);
            recordIndex += 1;
        }

        return new Cff2FontDictionaryArray(index, records);
    }
}

internal class Cff2FontDictionarySelect
{
    int format;
    int[] fontDictionaryIds;
    int encodedLength;

    internal Cff2FontDictionarySelect(
        int format,
        int[] fontDictionaryIds,
        int encodedLength)
    {
        this.format = format;
        this.fontDictionaryIds = fontDictionaryIds;
        this.encodedLength = encodedLength;
    }

    internal int GetFormat() { return format; }
    internal int GetGlyphCount() { return fontDictionaryIds.Length; }
    internal int GetEncodedLength() { return encodedLength; }

    internal int GetFontDictionaryIndex(int glyphIndex)
    {
        if (glyphIndex < 0 || glyphIndex >= fontDictionaryIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("glyphIndex");
        }

        return fontDictionaryIds[glyphIndex];
    }
}

internal static class Cff2FontDictionarySelectParser
{
    internal static Cff2FontDictionarySelect Parse(
        FontDataSlice data,
        int offset,
        int glyphCount,
        int fontDictionaryCount)
    {
        if (glyphCount < 1 || fontDictionaryCount < 1)
        {
            throw new System.ArgumentOutOfRangeException("glyphCount");
        }

        if (offset < 0 || offset > data.GetLength())
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        BigEndianReader reader = new BigEndianReader(data.GetSlice(
            offset,
            data.GetLength() - offset));
        int format = reader.ReadUInt8();
        int[] ids = new int[glyphCount];

        if (format == 0)
        {
            int glyph = 0;

            while (glyph < glyphCount)
            {
                ids[glyph] = reader.ReadUInt8();
                ValidateId(ids[glyph], fontDictionaryCount);
                glyph += 1;
            }

            return new Cff2FontDictionarySelect(
                format,
                ids,
                CheckedFontMath.AddOffset(1, glyphCount));
        }

        if (format != 3 && format != 4)
        {
            throw FontFormatException.Create(
                "A CFF2 FDSelect has an unsupported format.");
        }

        int rangeCount = format == 3 ? reader.ReadUInt16() : ReadInt32(reader);

        if (rangeCount < 1 || rangeCount > glyphCount)
        {
            throw FontFormatException.Create(
                "A CFF2 FDSelect has an invalid range count.");
        }

        int[] firstGlyphs = new int[rangeCount];
        int[] rangeIds = new int[rangeCount];
        int rangeIndex = 0;

        while (rangeIndex < rangeCount)
        {
            int first = format == 3 ? reader.ReadUInt16() : ReadInt32(reader);
            int id = format == 3 ? reader.ReadUInt8() : reader.ReadUInt16();
            ValidateId(id, fontDictionaryCount);

            if ((rangeIndex == 0 && first != 0) ||
                (rangeIndex != 0 && first <= firstGlyphs[rangeIndex - 1]))
            {
                throw FontFormatException.Create(
                    "CFF2 FDSelect ranges are not ordered.");
            }

            firstGlyphs[rangeIndex] = first;
            rangeIds[rangeIndex] = id;
            rangeIndex += 1;
        }

        int sentinel = format == 3 ? reader.ReadUInt16() : ReadInt32(reader);

        if (sentinel != glyphCount ||
            sentinel <= firstGlyphs[rangeCount - 1])
        {
            throw FontFormatException.Create(
                "A CFF2 FDSelect sentinel does not match the glyph count.");
        }

        rangeIndex = 0;

        while (rangeIndex < rangeCount)
        {
            int endGlyph = rangeIndex + 1 < rangeCount ?
                firstGlyphs[rangeIndex + 1] : sentinel;
            int glyph = firstGlyphs[rangeIndex];

            while (glyph < endGlyph)
            {
                ids[glyph] = rangeIds[rangeIndex];
                glyph += 1;
            }

            rangeIndex += 1;
        }

        int rangeSize = format == 3 ? 3 : 6;
        int countSize = format == 3 ? 2 : 4;
        int sentinelSize = format == 3 ? 2 : 4;
        int encodedLength = CheckedFontMath.AddOffset(
            CheckedFontMath.AddOffset(1, countSize),
            CheckedFontMath.AddOffset(
                CheckedFontMath.MultiplyCount(rangeCount, rangeSize),
                sentinelSize));
        return new Cff2FontDictionarySelect(format, ids, encodedLength);
    }

    static int ReadInt32(BigEndianReader reader)
    {
        uint value = reader.ReadUInt32();

        if (value > int.MaxValue)
        {
            throw FontFormatException.Create(
                "A CFF2 FDSelect value exceeds the supported range.");
        }

        return (int)value;
    }

    static void ValidateId(int id, int fontDictionaryCount)
    {
        if (id < 0 || id >= fontDictionaryCount)
        {
            throw FontFormatException.Create(
                "A CFF2 FDSelect references a missing Font DICT.");
        }
    }
}
