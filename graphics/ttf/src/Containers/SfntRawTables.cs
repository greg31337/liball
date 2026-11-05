namespace ttf;

internal enum SfntTableClassification
{
    Known = 0,
    Unsupported = 1,
    Unknown = 2
}

internal class SfntTableData
{
    int tag;
    uint checksum;
    FontDataSlice data;
    SfntTableClassification classification;
    bool rawDataAvailable;
    FontChecksumValidationState checksumValidationState;
    FontTableAlignmentState alignmentState;

    internal SfntTableData(
        int tag,
        uint checksum,
        FontDataSlice data,
        SfntTableClassification classification,
        bool rawDataAvailable)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        this.tag = tag;
        this.checksum = checksum;
        this.data = data;
        this.classification = classification;
        this.rawDataAvailable = rawDataAvailable;
        checksumValidationState = FontChecksumValidationState.NotChecked;
        alignmentState = FontTableAlignmentState.NotChecked;
    }

    internal int GetTag()
    {
        return tag;
    }

    internal uint GetChecksum()
    {
        return checksum;
    }

    internal long GetOffset()
    {
        return data.GetAbsoluteOffset();
    }

    internal int GetLength()
    {
        return data.GetLength();
    }

    internal SfntTableClassification GetClassification()
    {
        return classification;
    }

    internal bool HasRawData()
    {
        return rawDataAvailable;
    }

    internal byte[] CopyRawData()
    {
        if (!rawDataAvailable)
        {
            throw new System.InvalidOperationException("Raw table preservation is disabled.");
        }

        return data.CopyBytes();
    }

    internal FontDataSlice GetDataSlice()
    {
        return data;
    }

    internal FontChecksumValidationState GetChecksumValidationState()
    {
        return checksumValidationState;
    }

    internal FontTableAlignmentState GetAlignmentState()
    {
        return alignmentState;
    }

    internal void SetValidationStates(
        FontChecksumValidationState checksumState,
        FontTableAlignmentState tableAlignmentState)
    {
        checksumValidationState = checksumState;
        alignmentState = tableAlignmentState;
    }
}

internal class SfntTableStore
{
    SfntTableData[] tables;
    FontDiagnostic[] diagnostics;

    internal SfntTableStore(SfntTableData[] tables, FontDiagnostic[] diagnostics)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        if (diagnostics == null)
        {
            throw new System.ArgumentNullException("diagnostics");
        }

        this.tables = tables;
        this.diagnostics = diagnostics;
    }

    internal int GetTableCount()
    {
        return tables.Length;
    }

    internal SfntTableData GetTable(int index)
    {
        if (index < 0 || index >= tables.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return tables[index];
    }

    internal bool HasTable(int tag)
    {
        return FindTable(tag) != null;
    }

    internal SfntTableData? FindTable(int tag)
    {
        int index = 0;

        while (index < tables.Length)
        {
            if (tables[index].GetTag() == tag)
            {
                return tables[index];
            }

            index += 1;
        }

        return null;
    }

    internal int GetDiagnosticCount()
    {
        return diagnostics.Length;
    }

    internal FontDiagnostic GetDiagnostic(int index)
    {
        if (index < 0 || index >= diagnostics.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return diagnostics[index];
    }
}

internal static class SfntTableStoreBuilder
{
    internal static SfntTableStore Create(
        FontData fontData,
        SfntDirectory directory,
        FontLoadOptions options,
        int faceIndex)
    {
        if (fontData == null)
        {
            throw new System.ArgumentNullException("fontData");
        }

        if (directory == null)
        {
            throw new System.ArgumentNullException("directory");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (faceIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("faceIndex");
        }

        options.GetCancellationToken().ThrowIfCancellationRequested();

        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        System.Collections.Generic.List<FontDiagnostic> diagnostics =
            new System.Collections.Generic.List<FontDiagnostic>();
        int index = 0;

        while (index < directory.GetTableCount())
        {
            SfntTableRecord record = directory.GetTableRecord(index);

            if (IsBounded(fontData, record))
            {
                FontDataSlice tableData = fontData.GetSlice(
                    (int)record.GetOffset(),
                    (int)record.GetLength());
                SfntTableClassification classification =
                    SfntTableClassifier.Classify(record.GetTag());
                bool rawDataAvailable = options.ShouldPreserveRawTables();
                SfntTableData table = new SfntTableData(
                    record.GetTag(),
                    record.GetChecksum(),
                    tableData,
                    classification,
                    rawDataAvailable);
                table.SetValidationStates(
                    GetChecksumValidationState(table, options),
                    GetAlignmentState(fontData, record));
                tables.Add(table);

                if (rawDataAvailable && classification != SfntTableClassification.Known)
                {
                    AddPreservationDiagnostic(
                        diagnostics,
                        options,
                        record,
                        classification,
                        faceIndex);
                }
            }

            index += 1;
        }

        SfntTableData[] orderedTables = tables.ToArray();
        SortTables(orderedTables);
        return new SfntTableStore(orderedTables, diagnostics.ToArray());
    }

    static bool IsBounded(FontData fontData, SfntTableRecord record)
    {
        long offset = record.GetOffset();
        long length = record.GetLength();
        long fontLength = fontData.GetLength();
        return offset <= fontLength && length <= fontLength - offset;
    }

    static FontChecksumValidationState GetChecksumValidationState(
        SfntTableData table,
        FontLoadOptions options)
    {
        if (options.GetChecksumPolicy() == FontChecksumPolicy.Ignore)
        {
            return FontChecksumValidationState.NotChecked;
        }

        ReadOnlySpan<byte> bytes = table.GetDataSlice().GetMemory().Span;
        bool valid;

        if (table.GetTag() == FontTags.Head)
        {
            valid = bytes.Length >= 12 &&
                FontChecksum.VerifyHeadTable(bytes, table.GetChecksum());
        }
        else
        {
            valid = FontChecksum.VerifyTable(bytes, table.GetChecksum());
        }

        return valid ?
            FontChecksumValidationState.Valid :
            FontChecksumValidationState.Invalid;
    }

    static FontTableAlignmentState GetAlignmentState(
        FontData fontData,
        SfntTableRecord record)
    {
        if (record.GetOffset() % 4 != 0)
        {
            return FontTableAlignmentState.MisalignedOffset;
        }

        long paddingLength = (4 - record.GetLength() % 4) % 4;
        long paddingStart = record.GetOffset() + record.GetLength();
        long paddingEnd = paddingStart + paddingLength;

        if (paddingEnd > fontData.GetLength())
        {
            return FontTableAlignmentState.MissingPadding;
        }

        ReadOnlySpan<byte> bytes = fontData.GetMemory().Span;
        int index = (int)paddingStart;

        while (index < (int)paddingEnd)
        {
            if (bytes[index] != 0)
            {
                return FontTableAlignmentState.NonZeroPadding;
            }

            index += 1;
        }

        return FontTableAlignmentState.Valid;
    }

    static void AddPreservationDiagnostic(
        System.Collections.Generic.List<FontDiagnostic> diagnostics,
        FontLoadOptions options,
        SfntTableRecord record,
        SfntTableClassification classification,
        int faceIndex)
    {
        if (diagnostics.Count >= options.GetLimits().GetMaxDiagnostics())
        {
            throw FontLimitException.Create("The configured diagnostic count limit was exceeded.");
        }

        string message = "An unknown table was preserved as bounded raw data.";

        if (classification == SfntTableClassification.Unsupported)
        {
            message = "A recognized unsupported table was preserved as bounded raw data.";
        }

        diagnostics.Add(new FontDiagnostic(
            FontDiagnosticSeverity.Information,
            FontDiagnosticCode.TablePreservedAsRaw,
            message,
            faceIndex,
            record.GetTag(),
            record.GetOffset(),
            "sfnt.rawTables"));
    }

    static void SortTables(SfntTableData[] tables)
    {
        int index = 1;

        while (index < tables.Length)
        {
            SfntTableData current = tables[index];
            int insertionIndex = index;

            while (insertionIndex > 0 &&
                CheckedFontMath.ReinterpretInt32AsUInt32(
                    tables[insertionIndex - 1].GetTag()) >
                CheckedFontMath.ReinterpretInt32AsUInt32(current.GetTag()))
            {
                tables[insertionIndex] = tables[insertionIndex - 1];
                insertionIndex -= 1;
            }

            tables[insertionIndex] = current;
            index += 1;
        }
    }
}

internal static class SfntTableClassifier
{
    internal static SfntTableClassification Classify(int tag)
    {
        switch (tag)
        {
            case FontTags.Avar:
            case FontTags.Base:
            case FontTags.Cbdt:
            case FontTags.Cblc:
            case FontTags.Cff:
            case FontTags.Cff2:
            case FontTags.Cmap:
            case FontTags.Colr:
            case FontTags.Cpal:
            case FontTags.Cvar:
            case FontTags.Cvt:
            case FontTags.Dsig:
            case FontTags.Ebdt:
            case FontTags.Eblc:
            case FontTags.Ebsc:
            case FontTags.Fpgm:
            case FontTags.Fvar:
            case FontTags.Gasp:
            case FontTags.Gdef:
            case FontTags.Glyf:
            case FontTags.Gpos:
            case FontTags.Gsub:
            case FontTags.Gvar:
            case FontTags.Hdmx:
            case FontTags.Head:
            case FontTags.Hhea:
            case FontTags.Hmtx:
            case FontTags.Hvar:
            case FontTags.Jstf:
            case FontTags.Kern:
            case FontTags.Loca:
            case FontTags.Ltsh:
            case FontTags.Math:
            case FontTags.Maxp:
            case FontTags.Merg:
            case FontTags.Meta:
            case FontTags.Mvar:
            case FontTags.Name:
            case FontTags.Os2:
            case FontTags.Pclt:
            case FontTags.Post:
            case FontTags.Prep:
            case FontTags.Sbix:
            case FontTags.Stat:
            case FontTags.Svg:
            case FontTags.Vdmx:
            case FontTags.Vhea:
            case FontTags.Vmtx:
            case FontTags.Vorg:
            case FontTags.Vvar:
                return SfntTableClassification.Known;
            case 0x61636E74:
            case 0x616E6B72:
            case 0x62646174:
            case 0x62686564:
            case 0x626C6F63:
            case 0x62736C6E:
            case 0x66656174:
            case 0x66647363:
            case 0x666D7478:
            case 0x666F6E64:
            case 0x67636964:
            case 0x68737479:
            case 0x6876676C:
            case 0x6876706D:
            case 0x6A757374:
            case 0x6B657278:
            case 0x6C636172:
            case 0x6C746167:
            case 0x6D6F7274:
            case 0x6D6F7278:
            case 0x6F706264:
            case 0x70726F70:
            case 0x7472616B:
            case 0x78726566:
            case 0x5A617066:
            case 0x46656174:
            case 0x476C6174:
            case 0x476C6F63:
            case 0x53696C66:
            case 0x53696C6C:
                return SfntTableClassification.Unsupported;
            default:
                return SfntTableClassification.Unknown;
        }
    }
}
