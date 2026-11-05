namespace ttf;

internal static class SfntDirectoryValidator
{
    internal static SfntValidationResult Validate(
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

        int tableCount = directory.GetTableCount();

        if (tableCount > options.GetLimits().GetMaxTablesPerFace())
        {
            throw FontLimitException.Create("The SFNT table count exceeds the configured per-face limit.");
        }

        SfntDiagnosticBuffer diagnostics = new SfntDiagnosticBuffer(
            options.GetLimits().GetMaxDiagnostics());

        if (tableCount == 0)
        {
            diagnostics.Add(
                FontDiagnosticSeverity.Fatal,
                FontDiagnosticCode.InvalidSfntHeader,
                "The SFNT directory does not contain any table records.",
                faceIndex,
                0,
                directory.GetDirectoryOffset() + 4,
                "sfnt.tableDirectory.numTables");
            return diagnostics.CreateResult(FontOutlineKind.Unknown);
        }

        ValidateSearchFields(directory, options, diagnostics, faceIndex);
        ValidateOrderingAndDuplicates(directory, diagnostics, faceIndex);

        bool[] boundedRecords = ValidateRanges(
            fontData,
            directory,
            options,
            diagnostics,
            faceIndex);

        ValidateOverlaps(directory, boundedRecords, diagnostics, faceIndex);
        FontChecksumValidationState checksumValidationState = ValidateChecksums(
            fontData,
            directory,
            boundedRecords,
            options,
            diagnostics,
            faceIndex);

        FontOutlineKind outlineKind = ValidateRequiredTables(
            directory,
            diagnostics,
            faceIndex);

        SfntValidationResult result = diagnostics.CreateResult(outlineKind);
        result.SetChecksumValidationState(checksumValidationState);
        return result;
    }

    static void ValidateSearchFields(
        SfntDirectory directory,
        FontLoadOptions options,
        SfntDiagnosticBuffer diagnostics,
        int faceIndex)
    {
        if (directory.GetStoredSearchRange() != directory.GetSearchRange() ||
            directory.GetStoredEntrySelector() != directory.GetEntrySelector() ||
            directory.GetStoredRangeShift() != directory.GetRangeShift())
        {
            long offset = directory.GetDirectoryOffset() + 6;
            string path = "sfnt.tableDirectory.searchFields";
            diagnostics.Add(
                GetRecoverableSeverity(options),
                FontDiagnosticCode.InvalidSearchFields,
                "The stored SFNT binary-search fields do not match the table count.",
                faceIndex,
                0,
                offset,
                path);
            AddCompatibleRepair(
                diagnostics,
                options,
                "Computed canonical SFNT binary-search fields were used for semantic processing.",
                faceIndex,
                0,
                offset,
                path);
        }
    }

    static void ValidateOrderingAndDuplicates(
        SfntDirectory directory,
        SfntDiagnosticBuffer diagnostics,
        int faceIndex)
    {
        int tableCount = directory.GetTableCount();
        int index = 0;

        while (index < tableCount)
        {
            SfntTableRecord current = directory.GetTableRecord(index);

            if (index > 0)
            {
                SfntTableRecord previous = directory.GetTableRecord(index - 1);

                if (CheckedFontMath.ReinterpretInt32AsUInt32(current.GetTag()) <
                    CheckedFontMath.ReinterpretInt32AsUInt32(previous.GetTag()))
                {
                    diagnostics.Add(
                        FontDiagnosticSeverity.Fatal,
                        FontDiagnosticCode.UnsortedTableDirectory,
                        "The SFNT table records are not ordered by tag.",
                        faceIndex,
                        current.GetTag(),
                        GetRecordFieldOffset(directory, index, 0),
                        $"sfnt.tableDirectory.records[{index}].tag");
                }
            }

            int previousIndex = 0;

            while (previousIndex < index)
            {
                if (directory.GetTableRecord(previousIndex).GetTag() == current.GetTag())
                {
                    diagnostics.Add(
                        FontDiagnosticSeverity.Fatal,
                        FontDiagnosticCode.DuplicateTable,
                        "The SFNT table directory contains a duplicate tag.",
                        faceIndex,
                        current.GetTag(),
                        GetRecordFieldOffset(directory, index, 0),
                        $"sfnt.tableDirectory.records[{index}].tag");
                    previousIndex = index;
                }
                else
                {
                    previousIndex += 1;
                }
            }

            index += 1;
        }
    }

    static bool[] ValidateRanges(
        FontData fontData,
        SfntDirectory directory,
        FontLoadOptions options,
        SfntDiagnosticBuffer diagnostics,
        int faceIndex)
    {
        int tableCount = directory.GetTableCount();
        bool[] boundedRecords = new bool[tableCount];
        long fontLength = fontData.GetLength();
        long directoryStart = directory.GetDirectoryOffset();
        long directoryEnd = directoryStart + 12L + tableCount * 16L;
        int index = 0;

        while (index < tableCount)
        {
            SfntTableRecord record = directory.GetTableRecord(index);
            long offset = record.GetOffset();
            long length = record.GetLength();

            if (offset > fontLength || length > fontLength - offset)
            {
                diagnostics.Add(
                    FontDiagnosticSeverity.Fatal,
                    FontDiagnosticCode.TableOutOfBounds,
                    "The declared table range extends outside the font data.",
                    faceIndex,
                    record.GetTag(),
                    GetRecordFieldOffset(directory, index, 8),
                    $"sfnt.tableDirectory.records[{index}].range");
            }
            else
            {
                boundedRecords[index] = true;

                if (length > 0 && offset < directoryEnd && directoryStart < offset + length)
                {
                    diagnostics.Add(
                        FontDiagnosticSeverity.Fatal,
                        FontDiagnosticCode.TableOutOfBounds,
                        "The declared table range overlaps the SFNT directory.",
                        faceIndex,
                        record.GetTag(),
                        offset,
                        $"sfnt.tableDirectory.records[{index}].range");
                }

                ValidatePadding(
                    fontData,
                    directory,
                    record,
                    index,
                    options,
                    diagnostics,
                    faceIndex);
            }

            if (offset % 4 != 0)
            {
                diagnostics.Add(
                    FontDiagnosticSeverity.Fatal,
                    FontDiagnosticCode.MisalignedTable,
                    "The table offset is not aligned to a four-byte boundary.",
                    faceIndex,
                    record.GetTag(),
                    GetRecordFieldOffset(directory, index, 8),
                    $"sfnt.tableDirectory.records[{index}].offset");
            }

            index += 1;
        }

        return boundedRecords;
    }

    static void ValidatePadding(
        FontData fontData,
        SfntDirectory directory,
        SfntTableRecord record,
        int index,
        FontLoadOptions options,
        SfntDiagnosticBuffer diagnostics,
        int faceIndex)
    {
        long offset = record.GetOffset();
        long length = record.GetLength();
        long paddingLength = (4 - length % 4) % 4;
        long paddingStart = offset + length;
        long paddingEnd = paddingStart + paddingLength;

        if (paddingEnd > fontData.GetLength())
        {
            diagnostics.Add(
                FontDiagnosticSeverity.Fatal,
                FontDiagnosticCode.TableOutOfBounds,
                "The table alignment padding extends outside the font data.",
                faceIndex,
                record.GetTag(),
                GetRecordFieldOffset(directory, index, 12),
                $"sfnt.tableDirectory.records[{index}].padding");
            return;
        }

        ReadOnlySpan<byte> bytes = fontData.GetMemory().Span;
        int paddingIndex = (int)paddingStart;

        while (paddingIndex < (int)paddingEnd)
        {
            if (bytes[paddingIndex] != 0)
            {
                string path = $"sfnt.tableDirectory.records[{index}].padding";
                diagnostics.Add(
                    GetRecoverableSeverity(options),
                    FontDiagnosticCode.NonZeroPadding,
                    "The table alignment padding contains a nonzero byte.",
                    faceIndex,
                    record.GetTag(),
                    paddingIndex,
                    path);
                AddCompatibleRepair(
                    diagnostics,
                    options,
                    "Nonzero table alignment padding was ignored during semantic processing.",
                    faceIndex,
                    record.GetTag(),
                    paddingIndex,
                    path);
                return;
            }

            paddingIndex += 1;
        }
    }

    static void ValidateOverlaps(
        SfntDirectory directory,
        bool[] boundedRecords,
        SfntDiagnosticBuffer diagnostics,
        int faceIndex)
    {
        int firstIndex = 0;

        while (firstIndex < directory.GetTableCount())
        {
            SfntTableRecord first = directory.GetTableRecord(firstIndex);
            int secondIndex = firstIndex + 1;

            while (secondIndex < directory.GetTableCount())
            {
                SfntTableRecord second = directory.GetTableRecord(secondIndex);

                if (boundedRecords[firstIndex] &&
                    boundedRecords[secondIndex] &&
                    first.GetLength() > 0 &&
                    second.GetLength() > 0 &&
                    first.GetOffset() < second.GetOffset() + second.GetLength() &&
                    second.GetOffset() < first.GetOffset() + first.GetLength())
                {
                    diagnostics.Add(
                        FontDiagnosticSeverity.Fatal,
                        FontDiagnosticCode.OverlappingTables,
                        "Two declared SFNT table ranges overlap.",
                        faceIndex,
                        second.GetTag(),
                        second.GetOffset(),
                        $"sfnt.tableDirectory.records[{secondIndex}].range");
                }

                secondIndex += 1;
            }

            firstIndex += 1;
        }
    }

    static FontChecksumValidationState ValidateChecksums(
        FontData fontData,
        SfntDirectory directory,
        bool[] boundedRecords,
        FontLoadOptions options,
        SfntDiagnosticBuffer diagnostics,
        int faceIndex)
    {
        if (options.GetChecksumPolicy() == FontChecksumPolicy.Ignore)
        {
            return FontChecksumValidationState.NotChecked;
        }

        ReadOnlySpan<byte> fontBytes = fontData.GetMemory().Span;
        int index = 0;
        bool hasBoundedHead = false;

        while (index < directory.GetTableCount())
        {
            options.GetCancellationToken().ThrowIfCancellationRequested();
            SfntTableRecord record = directory.GetTableRecord(index);

            if (boundedRecords[index])
            {
                int offset = (int)record.GetOffset();
                int length = (int)record.GetLength();
                ReadOnlySpan<byte> tableBytes = fontBytes.Slice(offset, length);
                bool matches = false;

                if (record.GetTag() == FontTags.Head)
                {
                    if (length >= 12)
                    {
                        matches = FontChecksum.VerifyHeadTable(
                            tableBytes,
                            record.GetChecksum());
                        hasBoundedHead = true;
                    }
                }
                else
                {
                    matches = FontChecksum.VerifyTable(tableBytes, record.GetChecksum());
                }

                if (!matches)
                {
                    diagnostics.Add(
                        GetChecksumSeverity(options),
                        FontDiagnosticCode.TableChecksumMismatch,
                        "The table checksum does not match its directory record.",
                        faceIndex,
                        record.GetTag(),
                        GetRecordFieldOffset(directory, index, 4),
                        $"sfnt.tableDirectory.records[{index}].checksum");
                }
            }

            index += 1;
        }

        if (directory.GetDirectoryOffset() != 0 || !hasBoundedHead)
        {
            return FontChecksumValidationState.NotApplicable;
        }

        if (!FontChecksum.VerifyFont(fontBytes))
        {
            diagnostics.Add(
                GetChecksumSeverity(options),
                FontDiagnosticCode.FontChecksumMismatch,
                "The whole-font checksum adjustment is invalid.",
                faceIndex,
                FontTags.Head,
                FindTableOffset(directory, FontTags.Head) + 8,
                "sfnt.head.checksumAdjustment");
            return FontChecksumValidationState.Invalid;
        }

        return FontChecksumValidationState.Valid;
    }

    static FontOutlineKind ValidateRequiredTables(
        SfntDirectory directory,
        SfntDiagnosticBuffer diagnostics,
        int faceIndex)
    {
        int[] requiredTags = new int[]
        {
            FontTags.Cmap,
            FontTags.Head,
            FontTags.Hhea,
            FontTags.Hmtx,
            FontTags.Maxp,
            FontTags.Name,
            FontTags.Os2,
            FontTags.Post
        };
        int requiredIndex = 0;

        while (requiredIndex < requiredTags.Length)
        {
            RequireTable(directory, requiredTags[requiredIndex], diagnostics, faceIndex);
            requiredIndex += 1;
        }

        bool hasGlyf = HasTable(directory, FontTags.Glyf);
        bool hasLoca = HasTable(directory, FontTags.Loca);
        bool hasCff = HasTable(directory, FontTags.Cff);
        bool hasCff2 = HasTable(directory, FontTags.Cff2);
        bool hasCbdt = HasTable(directory, FontTags.Cbdt);
        bool hasCblc = HasTable(directory, FontTags.Cblc);
        bool hasEbdt = HasTable(directory, FontTags.Ebdt);
        bool hasEblc = HasTable(directory, FontTags.Eblc);
        bool hasSbix = HasTable(directory, FontTags.Sbix);

        ValidateBitmapPair(
            hasCbdt,
            hasCblc,
            FontTags.Cbdt,
            FontTags.Cblc,
            diagnostics,
            faceIndex);
        ValidateBitmapPair(
            hasEbdt,
            hasEblc,
            FontTags.Ebdt,
            FontTags.Eblc,
            diagnostics,
            faceIndex);

        int vectorKindCount = 0;

        if (hasGlyf)
        {
            vectorKindCount += 1;
        }

        if (hasCff)
        {
            vectorKindCount += 1;
        }

        if (hasCff2)
        {
            vectorKindCount += 1;
        }

        if (vectorKindCount > 1)
        {
            AddInvalidCombination(
                diagnostics,
                faceIndex,
                "The font declares more than one vector outline technology.");
            return FontOutlineKind.Unknown;
        }

        if (hasGlyf)
        {
            if (!hasLoca)
            {
                RequireTable(directory, FontTags.Loca, diagnostics, faceIndex);
            }

            if (directory.GetScalerType() != 0x00010000U &&
                directory.GetScalerType() != 0x74727565U)
            {
                AddInvalidCombination(
                    diagnostics,
                    faceIndex,
                    "The SFNT scaler type is inconsistent with TrueType outlines.");
            }

            return FontOutlineKind.TrueType;
        }

        if (hasLoca)
        {
            AddInvalidCombination(
                diagnostics,
                faceIndex,
                "The loca table is present without a glyf table.");
        }

        if (hasCff)
        {
            if (directory.GetScalerType() != 0x4F54544FU)
            {
                AddInvalidCombination(
                    diagnostics,
                    faceIndex,
                    "The SFNT scaler type is inconsistent with CFF outlines.");
            }

            return FontOutlineKind.Cff1;
        }

        if (hasCff2)
        {
            if (directory.GetScalerType() != 0x4F54544FU)
            {
                AddInvalidCombination(
                    diagnostics,
                    faceIndex,
                    "The SFNT scaler type is inconsistent with CFF2 outlines.");
            }

            return FontOutlineKind.Cff2;
        }

        if ((hasCbdt && hasCblc) || (hasEbdt && hasEblc) || hasSbix)
        {
            if (directory.GetScalerType() != 0x00010000U &&
                directory.GetScalerType() != 0x74727565U)
            {
                AddInvalidCombination(
                    diagnostics,
                    faceIndex,
                    "The SFNT scaler type is inconsistent with bitmap-only glyph data.");
            }

            return FontOutlineKind.BitmapOnly;
        }

        AddInvalidCombination(
            diagnostics,
            faceIndex,
            "The font does not contain a supported outline or bitmap table combination.");
        return FontOutlineKind.Unknown;
    }

    static void ValidateBitmapPair(
        bool hasData,
        bool hasLocation,
        int dataTag,
        int locationTag,
        SfntDiagnosticBuffer diagnostics,
        int faceIndex)
    {
        if (hasData && !hasLocation)
        {
            diagnostics.Add(
                FontDiagnosticSeverity.Error,
                FontDiagnosticCode.MissingRequiredTable,
                "A bitmap data table is missing its required location table.",
                faceIndex,
                locationTag,
                -1,
                "sfnt.requiredTables");
        }

        if (!hasData && hasLocation)
        {
            diagnostics.Add(
                FontDiagnosticSeverity.Error,
                FontDiagnosticCode.MissingRequiredTable,
                "A bitmap location table is missing its required data table.",
                faceIndex,
                dataTag,
                -1,
                "sfnt.requiredTables");
        }
    }

    static void RequireTable(
        SfntDirectory directory,
        int tag,
        SfntDiagnosticBuffer diagnostics,
        int faceIndex)
    {
        if (!HasTable(directory, tag))
        {
            diagnostics.Add(
                FontDiagnosticSeverity.Error,
                FontDiagnosticCode.MissingRequiredTable,
                "A table required by the font outline kind is missing.",
                faceIndex,
                tag,
                -1,
                "sfnt.requiredTables");
        }
    }

    static bool HasTable(SfntDirectory directory, int tag)
    {
        int index = 0;

        while (index < directory.GetTableCount())
        {
            if (directory.GetTableRecord(index).GetTag() == tag)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static long FindTableOffset(SfntDirectory directory, int tag)
    {
        int index = 0;

        while (index < directory.GetTableCount())
        {
            SfntTableRecord record = directory.GetTableRecord(index);

            if (record.GetTag() == tag)
            {
                return record.GetOffset();
            }

            index += 1;
        }

        return -1;
    }

    static void AddInvalidCombination(
        SfntDiagnosticBuffer diagnostics,
        int faceIndex,
        string message)
    {
        diagnostics.Add(
            FontDiagnosticSeverity.Error,
            FontDiagnosticCode.InvalidTableCombination,
            message,
            faceIndex,
            0,
            -1,
            "sfnt.outlineKind");
    }

    static FontDiagnosticSeverity GetRecoverableSeverity(FontLoadOptions options)
    {
        if (options.GetMode() == FontLoadMode.Strict ||
            (options.GetMode() == FontLoadMode.Compatible &&
            options.GetRepairPolicy() == FontRepairPolicy.None))
        {
            return FontDiagnosticSeverity.Error;
        }

        return FontDiagnosticSeverity.Warning;
    }

    static void AddCompatibleRepair(
        SfntDiagnosticBuffer diagnostics,
        FontLoadOptions options,
        string message,
        int faceIndex,
        int tableTag,
        long absoluteOffset,
        string logicalPath)
    {
        if (options.GetMode() != FontLoadMode.Compatible ||
            options.GetRepairPolicy() != FontRepairPolicy.Safe)
        {
            return;
        }

        diagnostics.Add(
            FontDiagnosticSeverity.Information,
            FontDiagnosticCode.RepairApplied,
            message,
            faceIndex,
            tableTag,
            absoluteOffset,
            logicalPath);
    }

    static FontDiagnosticSeverity GetChecksumSeverity(FontLoadOptions options)
    {
        if (options.GetChecksumPolicy() == FontChecksumPolicy.RequireValid)
        {
            return FontDiagnosticSeverity.Error;
        }

        return FontDiagnosticSeverity.Warning;
    }

    static long GetRecordFieldOffset(SfntDirectory directory, int index, int fieldOffset)
    {
        return directory.GetDirectoryOffset() + 12L + index * 16L + fieldOffset;
    }
}

internal class SfntDiagnosticBuffer
{
    System.Collections.Generic.List<FontDiagnostic> diagnostics;
    int maximumCount;

    internal SfntDiagnosticBuffer(int maximumCount)
    {
        diagnostics = new System.Collections.Generic.List<FontDiagnostic>();
        this.maximumCount = maximumCount;
    }

    internal void Add(
        FontDiagnosticSeverity severity,
        FontDiagnosticCode code,
        string message,
        int faceIndex,
        int tableTag,
        long absoluteOffset,
        string logicalPath)
    {
        if (diagnostics.Count >= maximumCount)
        {
            throw FontLimitException.Create("The configured diagnostic count limit was exceeded.");
        }

        diagnostics.Add(new FontDiagnostic(
            severity,
            code,
            message,
            faceIndex,
            tableTag,
            absoluteOffset,
            logicalPath));
    }

    internal SfntValidationResult CreateResult(FontOutlineKind outlineKind)
    {
        return new SfntValidationResult(outlineKind, diagnostics.ToArray());
    }
}
