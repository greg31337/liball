namespace ttf;

internal class FontSubsetNameData
{
    byte[] tableData;
    string prefix;
    string familyName;
    string fullName;
    string postScriptName;
    string sourcePostScriptName;

    internal FontSubsetNameData(
        byte[] tableData,
        string prefix,
        string familyName,
        string fullName,
        string postScriptName,
        string sourcePostScriptName)
    {
        this.tableData = CopyBytes(tableData);
        this.prefix = prefix;
        this.familyName = familyName;
        this.fullName = fullName;
        this.postScriptName = postScriptName;
        this.sourcePostScriptName = sourcePostScriptName;
    }

    internal byte[] CopyTableData()
    {
        return CopyBytes(tableData);
    }

    internal string GetPrefix()
    {
        return prefix;
    }

    internal string GetFamilyName()
    {
        return familyName;
    }

    internal string GetFullName()
    {
        return fullName;
    }

    internal string GetPostScriptName()
    {
        return postScriptName;
    }

    internal string GetSourcePostScriptName()
    {
        return sourcePostScriptName;
    }

    static byte[] CopyBytes(byte[] values)
    {
        byte[] copy = new byte[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}

internal class FontSubsetNameRecordData
{
    int platformId;
    int encodingId;
    int languageId;
    int nameId;
    byte[] value;

    internal FontSubsetNameRecordData(
        int platformId,
        int encodingId,
        int languageId,
        int nameId,
        byte[] value)
    {
        this.platformId = platformId;
        this.encodingId = encodingId;
        this.languageId = languageId;
        this.nameId = nameId;
        this.value = value;
    }

    internal int GetPlatformId()
    {
        return platformId;
    }

    internal int GetEncodingId()
    {
        return encodingId;
    }

    internal int GetLanguageId()
    {
        return languageId;
    }

    internal int GetNameId()
    {
        return nameId;
    }

    internal byte[] GetValue()
    {
        return value;
    }
}

internal static class FontSubsetNameBuilder
{
    const int EnglishUnitedStates = 0x0409;

    internal static FontSubsetNameData Create(
        FontInstance instance,
        int[] oldToNewGlyphIds,
        FontSubsetEmbeddingMode embeddingMode,
        FontSubsetMetadataPolicy metadataPolicy)
    {
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetEmbeddingMode(embeddingMode);
        options.SetMetadataPolicy(metadataPolicy);
        return Create(instance, oldToNewGlyphIds, options);
    }

    internal static FontSubsetNameData Create(
        FontInstance instance,
        int[] oldToNewGlyphIds,
        FontSubsetOptions options)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        if (oldToNewGlyphIds == null)
        {
            throw new System.ArgumentNullException("oldToNewGlyphIds");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (oldToNewGlyphIds.Length != instance.GetGlyphCount())
        {
            throw new System.ArgumentException(
                "The glyph map does not match the font instance.",
                "oldToNewGlyphIds");
        }

        FontSubsetEmbeddingMode embeddingMode = options.GetEmbeddingMode();
        FontSubsetMetadataPolicy metadataPolicy = options.GetMetadataPolicy();
        ValidateModes(embeddingMode, metadataPolicy);
        FontFace face = instance.GetFace();
        SfntNameTable? source = face.GetNameTable();
        string? sourceFamily = SelectName(source, 16);

        if (sourceFamily == null)
        {
            sourceFamily = SelectName(source, 1);
        }

        if (sourceFamily == null)
        {
            sourceFamily = "Unnamed";
        }

        string? style = SelectName(source, 17);

        if (style == null)
        {
            style = SelectName(source, 2);
        }

        if (style == null)
        {
            style = "Regular";
        }

        string sourcePostScript = GetPostScriptName(
            source,
            sourceFamily,
            style);
        string prefix = embeddingMode == FontSubsetEmbeddingMode.Subset ?
            FontSubsetPrefix.Create(
                instance,
                options,
                oldToNewGlyphIds,
                sourcePostScript) : "";
        string family = prefix.Length == 0 ? sourceFamily :
            prefix + "+" + sourceFamily;
        string full = family + " " + style;
        string postScript = prefix.Length == 0 ? sourcePostScript :
            SfntPostScriptNameNormalizer.Normalize(
                prefix + "+" + sourcePostScript)
                .GetNormalizedName();
        string unique = postScript;
        System.Collections.Generic.List<byte[]> languageTags =
            new System.Collections.Generic.List<byte[]>();
        System.Collections.Generic.List<FontSubsetNameRecordData> records =
            new System.Collections.Generic.List<FontSubsetNameRecordData>();
        AddPreservedRecords(
            source,
            metadataPolicy,
            records,
            languageTags);
        AddGeneratedRecord(records, 1, family);
        AddGeneratedRecord(records, 2, style);
        AddGeneratedRecord(records, 3, unique);
        AddGeneratedRecord(records, 4, full);
        AddGeneratedRecord(records, 6, postScript);
        AddGeneratedRecord(records, 16, family);
        AddGeneratedRecord(records, 17, style);
        SortAndRemoveDuplicateRecords(records);
        FontLimits limits = face.GetTableCache().GetLimits();
        byte[] tableData = WriteTable(records, languageTags, limits);
        return new FontSubsetNameData(
            tableData,
            prefix,
            family,
            full,
            postScript,
            sourcePostScript);
    }

    static void AddPreservedRecords(
        SfntNameTable? source,
        FontSubsetMetadataPolicy metadataPolicy,
        System.Collections.Generic.List<FontSubsetNameRecordData> records,
        System.Collections.Generic.List<byte[]> languageTags)
    {
        if (source == null)
        {
            return;
        }

        int[] languageTagMap = new int[source.GetLanguageTagCount()];
        System.Array.Fill(languageTagMap, -1);
        int index = 0;

        while (index < source.GetRecordCount())
        {
            SfntNameRecord record = source.GetRecord(index);

            if (ShouldPreserveNameId(
                record.GetNameId(),
                metadataPolicy) &&
                record.HasValidLanguage() &&
                record.HasValidString())
            {
                int languageId = record.GetLanguageId();

                if (record.UsesLanguageTag())
                {
                    int oldTagIndex = record.GetLanguageTagIndex();
                    int newTagIndex = languageTagMap[oldTagIndex];

                    if (newTagIndex < 0)
                    {
                        newTagIndex = languageTags.Count;
                        languageTagMap[oldTagIndex] = newTagIndex;
                        languageTags.Add(source
                            .GetLanguageTagRecord(oldTagIndex)
                            .CopyStringBytes());
                    }

                    languageId = 0x8000 + newTagIndex;
                }

                records.Add(new FontSubsetNameRecordData(
                    record.GetPlatformId(),
                    record.GetEncodingId(),
                    languageId,
                    record.GetNameId(),
                    record.CopyStringBytes()));
            }

            index++;
        }
    }

    static bool ShouldPreserveNameId(
        int nameId,
        FontSubsetMetadataPolicy metadataPolicy)
    {
        if (nameId == 13 || nameId == 14)
        {
            return true;
        }

        if (metadataPolicy != FontSubsetMetadataPolicy.RetainSafe)
        {
            return false;
        }

        return nameId != 1 && nameId != 2 && nameId != 3 &&
            nameId != 4 && nameId != 6 && nameId != 16 &&
            nameId != 17 && nameId != 21 && nameId != 22 &&
            nameId != 25;
    }

    static void AddGeneratedRecord(
        System.Collections.Generic.List<FontSubsetNameRecordData> records,
        int nameId,
        string value)
    {
        records.Add(new FontSubsetNameRecordData(
            3,
            10,
            EnglishUnitedStates,
            nameId,
            System.Text.Encoding.BigEndianUnicode.GetBytes(value)));
    }

    static void SortAndRemoveDuplicateRecords(
        System.Collections.Generic.List<FontSubsetNameRecordData> records)
    {
        records.Sort((left, right) => CompareRecords(left, right));
        int readIndex = 0;
        int writeIndex = 0;

        while (readIndex < records.Count)
        {
            if (writeIndex == 0 || CompareRecords(
                records[writeIndex - 1],
                records[readIndex]) != 0)
            {
                records[writeIndex] = records[readIndex];
                writeIndex++;
            }

            readIndex++;
        }

        if (writeIndex < records.Count)
        {
            records.RemoveRange(writeIndex, records.Count - writeIndex);
        }
    }

    static int CompareRecords(
        FontSubsetNameRecordData left,
        FontSubsetNameRecordData right)
    {
        int comparison = left.GetPlatformId().CompareTo(
            right.GetPlatformId());

        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.GetEncodingId().CompareTo(right.GetEncodingId());

        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.GetLanguageId().CompareTo(right.GetLanguageId());
        return comparison != 0 ? comparison :
            left.GetNameId().CompareTo(right.GetNameId());
    }

    static byte[] WriteTable(
        System.Collections.Generic.List<FontSubsetNameRecordData> records,
        System.Collections.Generic.List<byte[]> languageTags,
        FontLimits limits)
    {
        if (records.Count > ushort.MaxValue)
        {
            throw FontLimitException.Create(
                "A subset name table has too many records.");
        }

        int version = languageTags.Count == 0 ? 0 : 1;
        int headerLength = 6 + records.Count * 12 +
            (version == 0 ? 0 : 2 + languageTags.Count * 4);

        if (headerLength > ushort.MaxValue)
        {
            throw FontLimitException.Create(
                "A subset name table header exceeds the UInt16 range.");
        }

        long storageLength = CalculateStorageLength(
            records,
            languageTags,
            limits);

        if (headerLength + storageLength > limits.GetMaxOutputBytes())
        {
            throw FontLimitException.Create(
                "A subset name table exceeds the configured output limit.");
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            headerLength + (int)storageLength,
            limits.GetMaxOutputBytes());
        writer.WriteUInt16((ushort)version);
        writer.WriteUInt16((ushort)records.Count);
        writer.WriteUInt16((ushort)headerLength);
        int offset = 0;
        int index = 0;

        while (index < records.Count)
        {
            FontSubsetNameRecordData record = records[index];
            ValidateStringOffsetAndLength(
                offset,
                record.GetValue().Length,
                limits);
            writer.WriteUInt16((ushort)record.GetPlatformId());
            writer.WriteUInt16((ushort)record.GetEncodingId());
            writer.WriteUInt16((ushort)record.GetLanguageId());
            writer.WriteUInt16((ushort)record.GetNameId());
            writer.WriteUInt16((ushort)record.GetValue().Length);
            writer.WriteUInt16((ushort)offset);
            offset += record.GetValue().Length;
            index++;
        }

        if (version == 1)
        {
            writer.WriteUInt16((ushort)languageTags.Count);
            index = 0;

            while (index < languageTags.Count)
            {
                byte[] value = languageTags[index];
                ValidateStringOffsetAndLength(offset, value.Length, limits);
                writer.WriteUInt16((ushort)value.Length);
                writer.WriteUInt16((ushort)offset);
                offset += value.Length;
                index++;
            }
        }

        index = 0;

        while (index < records.Count)
        {
            writer.WriteBytes(records[index].GetValue());
            index++;
        }

        index = 0;

        while (index < languageTags.Count)
        {
            writer.WriteBytes(languageTags[index]);
            index++;
        }

        return writer.CopyBytes();
    }

    static long CalculateStorageLength(
        System.Collections.Generic.List<FontSubsetNameRecordData> records,
        System.Collections.Generic.List<byte[]> languageTags,
        FontLimits limits)
    {
        long length = 0;
        int index = 0;

        while (index < records.Count)
        {
            int valueLength = records[index].GetValue().Length;

            if (valueLength > limits.GetMaxStringBytes())
            {
                throw FontLimitException.Create(
                    "A subset name string exceeds the configured limit.");
            }

            length += valueLength;
            index++;
        }

        index = 0;

        while (index < languageTags.Count)
        {
            int valueLength = languageTags[index].Length;

            if (valueLength > limits.GetMaxStringBytes())
            {
                throw FontLimitException.Create(
                    "A subset language tag exceeds the configured limit.");
            }

            length += valueLength;
            index++;
        }

        return length;
    }

    static void ValidateStringOffsetAndLength(
        int offset,
        int length,
        FontLimits limits)
    {
        if (offset > ushort.MaxValue || length > ushort.MaxValue ||
            length > limits.GetMaxStringBytes())
        {
            throw FontLimitException.Create(
                "A subset name string cannot be represented.");
        }
    }

    static string GetPostScriptName(
        SfntNameTable? table,
        string family,
        string style)
    {
        if (table != null)
        {
            SfntPostScriptName? postScript = table.GetPostScriptName();

            if (postScript != null)
            {
                return postScript.GetNormalizedName();
            }
        }

        return SfntPostScriptNameNormalizer.Normalize(
            family + "-" + style)
            .GetNormalizedName();
    }

    static string? SelectName(SfntNameTable? table, int nameId)
    {
        if (table == null)
        {
            return null;
        }

        SfntLocalizedName? name = table.SelectDefaultName(nameId);
        return name == null ? null : name.GetValue();
    }

    static void ValidateModes(
        FontSubsetEmbeddingMode embeddingMode,
        FontSubsetMetadataPolicy metadataPolicy)
    {
        if (embeddingMode < FontSubsetEmbeddingMode.Subset ||
            embeddingMode > FontSubsetEmbeddingMode.FullFont)
        {
            throw new System.ArgumentOutOfRangeException("embeddingMode");
        }

        if (metadataPolicy < FontSubsetMetadataPolicy.Minimal ||
            metadataPolicy > FontSubsetMetadataPolicy.RetainSafe)
        {
            throw new System.ArgumentOutOfRangeException("metadataPolicy");
        }
    }
}
