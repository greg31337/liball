namespace ttf;

internal class FontSubsetUnknownTables
{
    FontSubsetAuxiliaryTable[] tables;
    int[] droppedTags;
    FontDiagnostic[] diagnostics;

    internal FontSubsetUnknownTables(
        FontSubsetAuxiliaryTable[] tables,
        int[] droppedTags,
        FontDiagnostic[] diagnostics)
    {
        this.tables = CopyTables(tables);
        this.droppedTags = CopyIntegers(droppedTags);
        this.diagnostics = CopyDiagnostics(diagnostics);
    }

    internal int GetTableCount() { return tables.Length; }

    internal FontSubsetAuxiliaryTable GetTable(int index)
    {
        if (index < 0 || index >= tables.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return new FontSubsetAuxiliaryTable(
            tables[index].GetTag(), tables[index].CopyData());
    }

    internal byte[]? CopyTableData(int tag)
    {
        int index = 0;
        while (index < tables.Length)
        {
            if (tables[index].GetTag() == tag)
            {
                return tables[index].CopyData();
            }
            index += 1;
        }
        return null;
    }

    internal int[] CopyDroppedTags()
    {
        return CopyIntegers(droppedTags);
    }

    internal int GetDiagnosticCount() { return diagnostics.Length; }

    internal FontDiagnostic GetDiagnostic(int index)
    {
        if (index < 0 || index >= diagnostics.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return CopyDiagnostic(diagnostics[index]);
    }

    static FontSubsetAuxiliaryTable[] CopyTables(
        FontSubsetAuxiliaryTable[] values)
    {
        FontSubsetAuxiliaryTable[] output =
            new FontSubsetAuxiliaryTable[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            output[index] = new FontSubsetAuxiliaryTable(
                values[index].GetTag(), values[index].CopyData());
            index += 1;
        }
        return output;
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] output = new int[values.Length];
        System.Array.Copy(values, output, values.Length);
        return output;
    }

    static FontDiagnostic[] CopyDiagnostics(FontDiagnostic[] values)
    {
        FontDiagnostic[] output = new FontDiagnostic[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            output[index] = CopyDiagnostic(values[index]);
            index += 1;
        }
        return output;
    }

    static FontDiagnostic CopyDiagnostic(FontDiagnostic value)
    {
        return new FontDiagnostic(
            value.GetSeverity(),
            value.GetCode(),
            value.GetMessage(),
            value.GetFaceIndex(),
            value.GetTableTag(),
            value.GetAbsoluteOffset(),
            value.GetLogicalPath());
    }
}

internal static class FontSubsetUnknownTablesBuilder
{
    static readonly int[] safeTags = new int[]
    {
        0x46656174,
        0x53696C6C,
        0x66647363,
        0x66656174,
        0x6C746167,
        0x7472616B
    };

    internal static FontSubsetUnknownTables Create(
        FontFace face,
        FontSubsetOptions options)
    {
        if (face == null || options == null)
        {
            throw new System.ArgumentNullException("values");
        }
        System.Collections.Generic.List<FontSubsetAuxiliaryTable> tables =
            new System.Collections.Generic.List<FontSubsetAuxiliaryTable>();
        System.Collections.Generic.List<int> dropped =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<FontDiagnostic> diagnostics =
            new System.Collections.Generic.List<FontDiagnostic>();
        bool retain = options.GetUnknownTablePolicy() ==
            FontSubsetUnknownTablePolicy.RetainSafe &&
            options.GetGlyphRetentionMode() ==
                FontSubsetGlyphRetentionMode.Preserve;
        SfntTableStore store = face.GetTableStore();
        System.Collections.Generic.List<SfntTableData> candidates =
            new System.Collections.Generic.List<SfntTableData>();
        int index = 0;
        while (index < store.GetTableCount())
        {
            SfntTableData table = store.GetTable(index);
            if (table.GetClassification() != SfntTableClassification.Known)
            {
                candidates.Add(table);
            }
            index += 1;
        }
        SfntTableData[] orderedCandidates = candidates.ToArray();
        SortSourceTables(orderedCandidates);
        index = 0;
        while (index < orderedCandidates.Length)
        {
            ProcessTable(
                face,
                orderedCandidates[index],
                retain,
                options.GetUnknownTablePolicy(),
                tables,
                dropped,
                diagnostics);
            index += 1;
        }
        FontSubsetAuxiliaryTable[] retainedTables = tables.ToArray();
        SortTables(retainedTables);
        int[] droppedTags = dropped.ToArray();
        SortTags(droppedTags);
        return new FontSubsetUnknownTables(
            retainedTables, droppedTags, diagnostics.ToArray());
    }

    static void ProcessTable(
        FontFace face,
        SfntTableData table,
        bool retain,
        FontSubsetUnknownTablePolicy policy,
        System.Collections.Generic.List<FontSubsetAuxiliaryTable> tables,
        System.Collections.Generic.List<int> dropped,
        System.Collections.Generic.List<FontDiagnostic> diagnostics)
    {
        bool safe = IsExplicitlySafe(table);
        if (retain && safe && table.HasRawData())
        {
            tables.Add(new FontSubsetAuxiliaryTable(
                table.GetTag(), table.CopyRawData()));
            AddDiagnostic(
                face,
                table,
                FontDiagnosticSeverity.Information,
                FontDiagnosticCode.TablePreservedAsRaw,
                "An explicitly safe unsupported table was copied unchanged.",
                diagnostics);
            return;
        }
        dropped.Add(table.GetTag());
        if (!safe)
        {
            AddDiagnostic(
                face,
                table,
                FontDiagnosticSeverity.Warning,
                FontDiagnosticCode.TableDropped,
                "The table was omitted because its unknown semantics may contain unrewritten glyph references.",
                diagnostics);
        }
        else if (policy == FontSubsetUnknownTablePolicy.RetainSafe)
        {
            AddDiagnostic(
                face,
                table,
                FontDiagnosticSeverity.Warning,
                FontDiagnosticCode.TableDropped,
                "The safe raw table could not be retained without preserved glyph identifiers and raw bytes.",
                diagnostics);
        }
        else
        {
            AddDiagnostic(
                face,
                table,
                FontDiagnosticSeverity.Information,
                FontDiagnosticCode.TableDropped,
                "The safe unsupported table was omitted by policy.",
                diagnostics);
        }
    }

    static bool IsExplicitlySafe(SfntTableData table)
    {
        if (table.GetClassification() !=
            SfntTableClassification.Unsupported)
        {
            return false;
        }
        int tag = table.GetTag();
        int index = 0;
        while (index < safeTags.Length)
        {
            if (safeTags[index] == tag) { return true; }
            index += 1;
        }
        return false;
    }

    static void AddDiagnostic(
        FontFace face,
        SfntTableData table,
        FontDiagnosticSeverity severity,
        FontDiagnosticCode code,
        string message,
        System.Collections.Generic.List<FontDiagnostic> diagnostics)
    {
        if (diagnostics.Count >= face.GetTableCache().GetLimits()
            .GetMaxDiagnostics())
        {
            throw FontLimitException.Create(
                "The configured diagnostic count limit was exceeded.");
        }
        diagnostics.Add(new FontDiagnostic(
            severity,
            code,
            message,
            face.GetFaceIndex(),
            table.GetTag(),
            table.GetOffset(),
            "subset.unknownTables"));
    }

    static void SortTables(FontSubsetAuxiliaryTable[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            FontSubsetAuxiliaryTable current = values[index];
            int position = index;
            while (position > 0 &&
                CheckedFontMath.ReinterpretInt32AsUInt32(
                    values[position - 1].GetTag()) >
                CheckedFontMath.ReinterpretInt32AsUInt32(current.GetTag()))
            {
                values[position] = values[position - 1];
                position -= 1;
            }
            values[position] = current;
            index += 1;
        }
    }

    static void SortSourceTables(SfntTableData[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            SfntTableData current = values[index];
            int position = index;
            while (position > 0 &&
                CheckedFontMath.ReinterpretInt32AsUInt32(
                    values[position - 1].GetTag()) >
                CheckedFontMath.ReinterpretInt32AsUInt32(current.GetTag()))
            {
                values[position] = values[position - 1];
                position -= 1;
            }
            values[position] = current;
            index += 1;
        }
    }

    static void SortTags(int[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            int current = values[index];
            int position = index;
            while (position > 0 &&
                CheckedFontMath.ReinterpretInt32AsUInt32(
                    values[position - 1]) >
                CheckedFontMath.ReinterpretInt32AsUInt32(current))
            {
                values[position] = values[position - 1];
                position -= 1;
            }
            values[position] = current;
            index += 1;
        }
    }
}
