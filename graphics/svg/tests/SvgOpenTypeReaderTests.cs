namespace svg.Tests;

public class SvgOpenTypeReaderTests
{
    [Test]
    public void ReadsTrueTypeDirectoryCoreMetricsNamesAndCharacterMapDescriptors()
    {
        byte[] bytes = BuildFont(false, true);

        SvgOpenTypeReadResult result = new SvgOpenTypeReader().ReadWithBytes(bytes);
        SvgFont font = result.Font;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgOpenTypeError.None));
            Assert.That(font.Success, Is.True);
            Assert.That(font.Bytes, Is.SameAs(bytes));
            Assert.That(font.OwnsBytes, Is.False);
            Assert.That(font.SfntVersion, Is.EqualTo(0x00010000u));
            Assert.That(font.OutlineKind, Is.EqualTo(SvgOpenTypeOutlineKind.TrueType));
            Assert.That(font.Tables.Select(table => table.Tag), Is.Ordered.Using<string>(StringComparer.Ordinal));
            Assert.That(font.Tables, Has.Count.EqualTo(10));
            Assert.That(font.Tables, Has.All.Matches<SvgOpenTypeTable>(table => table.ChecksumValid));
            Assert.That(font.FindTable("head"), Is.Not.Null);
            Assert.That(font.FindTable("missing"), Is.Null);
            Assert.That(font.Head.UnitsPerEm, Is.EqualTo(1000));
            Assert.That(font.Head.XMin, Is.Zero);
            Assert.That(font.Head.YMin, Is.EqualTo(-200));
            Assert.That(font.Head.XMax, Is.EqualTo(1000));
            Assert.That(font.Head.YMax, Is.EqualTo(800));
            Assert.That(font.Head.IndexToLocationFormat, Is.Zero);
            Assert.That(font.MaximumProfile.Version, Is.EqualTo(0x00010000u));
            Assert.That(font.MaximumProfile.GlyphCount, Is.EqualTo(3));
            Assert.That(font.MaximumProfile.MaximumPoints, Is.EqualTo(4));
            Assert.That(font.MaximumProfile.MaximumCompositePoints, Is.EqualTo(8));
            Assert.That(font.HorizontalHeader.Ascender, Is.EqualTo(800));
            Assert.That(font.HorizontalHeader.Descender, Is.EqualTo(-200));
            Assert.That(font.HorizontalHeader.NumberOfHorizontalMetrics, Is.EqualTo(2));
            Assert.That(font.HorizontalMetrics.Select(metric => metric.AdvanceWidth), Is.EqualTo(new[] { 500, 600, 600 }));
            Assert.That(font.HorizontalMetrics.Select(metric => metric.LeftSideBearing), Is.EqualTo(new[] { 0, 10, -20 }));
            Assert.That(font.Os2.Version, Is.EqualTo(4));
            Assert.That(font.Os2.WeightClass, Is.EqualTo(700));
            Assert.That(font.Os2.WidthClass, Is.EqualTo(5));
            Assert.That(font.Os2.VendorIdentifier, Is.EqualTo("TEST"));
            Assert.That(font.Os2.TypographicAscender, Is.EqualTo(750));
            Assert.That(font.Os2.XHeight, Is.EqualTo(500));
            Assert.That(font.Os2.CapitalHeight, Is.EqualTo(700));
            Assert.That(font.FamilyName, Is.EqualTo("Fixture"));
            Assert.That(font.SubfamilyName, Is.EqualTo("Book"));
            Assert.That(font.FullName, Is.EqualTo("Fixture Regular"));
            Assert.That(font.PostScriptName, Is.EqualTo("Fixture-Regular"));
            Assert.That(font.VersionName, Is.EqualTo("Version 1.000"));
            Assert.That(font.FindName(1), Is.EqualTo("Legacy Family"));
            Assert.That(font.Names.Where(record => record.PlatformIdentifier == 3), Has.All.Matches<SvgOpenTypeNameRecord>(record => record.LanguageTag == "en-US"));
            Assert.That(font.CharacterMaps.Select(map => map.Format), Is.EqualTo(new[] { 12, 4 }));
            Assert.That(font.CharacterMaps.Select(map => map.PlatformIdentifier), Is.EqualTo(new[] { 0, 3 }));
            Assert.That(font.CharacterMaps, Has.All.Matches<SvgOpenTypeCmapRecord>(map => map.Length > 0));
            Assert.That(font.Post.Version, Is.EqualTo(0x00030000u));
            Assert.That(font.Post.ItalicAngle, Is.EqualTo(-12.5));
            Assert.That(font.Post.UnderlinePosition, Is.EqualTo(-100));
            Assert.That(font.Post.FixedPitch, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ReadsOttoCffMetadataWithTheCompactMaximumProfile()
    {
        byte[] bytes = BuildFont(true, true);

        SvgOpenTypeReadResult result = new SvgOpenTypeReader().ReadWithBytes(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Font.SfntVersion, Is.EqualTo(0x4F54544Fu));
            Assert.That(result.Font.OutlineKind, Is.EqualTo(SvgOpenTypeOutlineKind.Cff));
            Assert.That(result.Font.FindTable("CFF "), Is.Not.Null);
            Assert.That(result.Font.FindTable("glyf"), Is.Null);
            Assert.That(result.Font.MaximumProfile.Version, Is.EqualTo(0x00005000u));
            Assert.That(result.Font.MaximumProfile.GlyphCount, Is.EqualTo(3));
            Assert.That(result.Font.FamilyName, Is.EqualTo("Fixture"));
        });
    }

    [Test]
    public void CopyOwnershipAndChecksumValidationAreExplicit()
    {
        byte[] copiedInput = BuildFont(false, true);
        byte original = copiedInput[0];
        SvgOpenTypeReadOptions copyOptions = new SvgOpenTypeReadOptions();
        copyOptions.CopyInputData = true;

        SvgOpenTypeReadResult copied = new SvgOpenTypeReader().ReadWithBytesAndOptions(copiedInput, copyOptions);
        copiedInput[0] = 0;

        byte[] corrupted = BuildFont(false, true);
        int postOffset = FindTableOffset(corrupted, "post");
        corrupted[postOffset + 10] = 1;
        SvgOpenTypeReadResult checkedResult = new SvgOpenTypeReader().ReadWithBytes(corrupted);
        SvgOpenTypeReadOptions uncheckedOptions = new SvgOpenTypeReadOptions();
        uncheckedOptions.ValidateChecksums = false;
        SvgOpenTypeReadResult uncheckedResult = new SvgOpenTypeReader().ReadWithBytesAndOptions(corrupted, uncheckedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(copied.Success, Is.True);
            Assert.That(copied.Font.OwnsBytes, Is.True);
            Assert.That(copied.Font.Bytes, Is.Not.SameAs(copiedInput));
            Assert.That(copied.Font.Bytes[0], Is.EqualTo(original));
            Assert.That(checkedResult.Success, Is.False);
            Assert.That(checkedResult.Error, Is.EqualTo(SvgOpenTypeError.InvalidChecksum));
            Assert.That(checkedResult.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidFont));
            Assert.That(uncheckedResult.Success, Is.True);
            Assert.That(uncheckedResult.Font.FindTable("post").ChecksumValid, Is.False);
            Assert.That(uncheckedResult.Font.Post.UnderlineThickness, Is.EqualTo(306));
        });
    }

    [Test]
    public void RejectsMalformedDirectoriesChecksumsAndMissingRequiredTablesWithLocatedDiagnostics()
    {
        SvgSourceUri sourceUri = new SvgSourceUri("memory:font.ttf");
        SvgSourceSpan source = SvgSourceSpan.Unknown(sourceUri);
        SvgOpenTypeReadOptions options = new SvgOpenTypeReadOptions();
        options.Source = source;

        SvgOpenTypeReadResult truncated = new SvgOpenTypeReader().ReadWithBytesAndOptions(new byte[7], options);

        byte[] unsortedBytes = BuildFont(false, true);
        SwapDirectoryRecords(unsortedBytes, 0, 1);
        SvgOpenTypeReadResult unsorted = new SvgOpenTypeReader().ReadWithBytesAndOptions(unsortedBytes, options);

        byte[] overlapBytes = BuildFont(false, true);
        WriteUInt32(overlapBytes, 12 + 16 + 8, ReadUInt32(overlapBytes, 12 + 8));
        SvgOpenTypeReadResult overlap = new SvgOpenTypeReader().ReadWithBytesAndOptions(overlapBytes, options);

        SvgOpenTypeReadResult missing = new SvgOpenTypeReader().ReadWithBytesAndOptions(BuildFont(false, false), options);

        byte[] checksumBytes = BuildFont(false, true);
        checksumBytes[FindTableOffset(checksumBytes, "name") + 1] = 2;
        SvgOpenTypeReadResult checksum = new SvgOpenTypeReader().ReadWithBytesAndOptions(checksumBytes, options);

        Assert.Multiple(() =>
        {
            AssertFailure(truncated, SvgOpenTypeError.InvalidDirectory, source);
            AssertFailure(unsorted, SvgOpenTypeError.InvalidDirectory, source);
            AssertFailure(overlap, SvgOpenTypeError.InvalidDirectory, source);
            AssertFailure(missing, SvgOpenTypeError.MissingRequiredTable, source);
            AssertFailure(checksum, SvgOpenTypeError.InvalidChecksum, source);
        });
    }

    [Test]
    public void EnforcesByteTableGlyphPointAndCancellationBoundsBeforeUnboundedWork()
    {
        byte[] bytes = BuildFont(false, true);
        SvgOpenTypeReadOptions byteOptions = new SvgOpenTypeReadOptions();
        byteOptions.Limits.MaxBytesPerFont = bytes.Length - 1;
        SvgOpenTypeReadOptions tableOptions = new SvgOpenTypeReadOptions();
        tableOptions.Limits.MaxTablesPerFont = 9;
        SvgOpenTypeReadOptions glyphOptions = new SvgOpenTypeReadOptions();
        glyphOptions.Limits.MaxGlyphsPerFont = 2;
        SvgOpenTypeReadOptions pointOptions = new SvgOpenTypeReadOptions();
        pointOptions.Limits.MaxPointsPerGlyph = 7;
        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        cancellation.Cancel();
        SvgOpenTypeReadOptions cancelledOptions = new SvgOpenTypeReadOptions();
        cancelledOptions.CancellationToken = cancellation.Token;

        SvgOpenTypeReadResult byteLimit = new SvgOpenTypeReader().ReadWithBytesAndOptions(bytes, byteOptions);
        SvgOpenTypeReadResult tableLimit = new SvgOpenTypeReader().ReadWithBytesAndOptions(bytes, tableOptions);
        SvgOpenTypeReadResult glyphLimit = new SvgOpenTypeReader().ReadWithBytesAndOptions(bytes, glyphOptions);
        SvgOpenTypeReadResult pointLimit = new SvgOpenTypeReader().ReadWithBytesAndOptions(bytes, pointOptions);
        SvgOpenTypeReadResult cancelled = new SvgOpenTypeReader().ReadWithBytesAndOptions(bytes, cancelledOptions);

        Assert.Multiple(() =>
        {
            Assert.That(byteLimit.Error, Is.EqualTo(SvgOpenTypeError.LimitExceeded));
            Assert.That(byteLimit.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(tableLimit.Error, Is.EqualTo(SvgOpenTypeError.LimitExceeded));
            Assert.That(glyphLimit.Error, Is.EqualTo(SvgOpenTypeError.LimitExceeded));
            Assert.That(pointLimit.Error, Is.EqualTo(SvgOpenTypeError.LimitExceeded));
            Assert.That(cancelled.Error, Is.EqualTo(SvgOpenTypeError.Cancelled));
            Assert.That(cancelled.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.Cancelled));
        });
    }

    [Test]
    public void RejectsMalformedRequiredMetadataMetricNameAndCmapTables()
    {
        SvgOpenTypeReadOptions uncheckedOptions = new SvgOpenTypeReadOptions();
        uncheckedOptions.ValidateChecksums = false;

        byte[] head = BuildFont(false, true);
        WriteUInt32(head, FindTableOffset(head, "head") + 12, 0);
        byte[] hhea = BuildFont(false, true);
        WriteUInt16(hhea, FindTableOffset(hhea, "hhea") + 34, 4);
        byte[] os2 = BuildFont(false, true);
        WriteUInt16(os2, FindTableOffset(os2, "OS/2") + 4, 0);
        byte[] name = BuildFont(false, true);
        WriteUInt16(name, FindTableOffset(name, "name") + 4, 4);
        byte[] cmap = BuildFont(false, true);
        int cmapOffset = FindTableOffset(cmap, "cmap");
        WriteUInt32(cmap, cmapOffset + 20 + 4, 0x7FFFFFFFu);
        byte[] post = BuildFont(false, true);
        WriteUInt32(post, FindTableOffset(post, "post"), 0x00050000u);

        SvgOpenTypeReadResult badHead = new SvgOpenTypeReader().ReadWithBytesAndOptions(head, uncheckedOptions);
        SvgOpenTypeReadResult badHhea = new SvgOpenTypeReader().ReadWithBytesAndOptions(hhea, uncheckedOptions);
        SvgOpenTypeReadResult badOs2 = new SvgOpenTypeReader().ReadWithBytesAndOptions(os2, uncheckedOptions);
        SvgOpenTypeReadResult badName = new SvgOpenTypeReader().ReadWithBytesAndOptions(name, uncheckedOptions);
        SvgOpenTypeReadResult badCmap = new SvgOpenTypeReader().ReadWithBytesAndOptions(cmap, uncheckedOptions);
        SvgOpenTypeReadResult badPost = new SvgOpenTypeReader().ReadWithBytesAndOptions(post, uncheckedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(badHead.Error, Is.EqualTo(SvgOpenTypeError.InvalidTable));
            Assert.That(badHhea.Error, Is.EqualTo(SvgOpenTypeError.InvalidTable));
            Assert.That(badOs2.Error, Is.EqualTo(SvgOpenTypeError.InvalidTable));
            Assert.That(badName.Error, Is.EqualTo(SvgOpenTypeError.InvalidTable));
            Assert.That(badCmap.Error, Is.EqualTo(SvgOpenTypeError.InvalidTable));
            Assert.That(badPost.Error, Is.EqualTo(SvgOpenTypeError.UnsupportedVersion));
            Assert.That(new[] { badHead, badHhea, badOs2, badName, badCmap, badPost }, Has.All.Matches<SvgOpenTypeReadResult>(value => value.Diagnostics.Single().Code == SvgDiagnosticCode.InvalidFont));
        });
    }

    [Test]
    public void ReadsOptionalVerticalMetricsAndRejectsAnIncompletePair()
    {
        SvgOpenTypeReadResult vertical = new SvgOpenTypeReader().ReadWithBytes(BuildFont(false, true, true));
        byte[] incompleteBytes = BuildFont(false, true, true);
        int vmtxRecord = FindTableRecord(incompleteBytes, "vmtx");
        WriteTag(incompleteBytes, vmtxRecord, "zzzz");
        SvgOpenTypeReadOptions uncheckedOptions = new SvgOpenTypeReadOptions();
        uncheckedOptions.ValidateChecksums = false;

        SvgOpenTypeReadResult incomplete = new SvgOpenTypeReader().ReadWithBytesAndOptions(incompleteBytes, uncheckedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(vertical.Success, Is.True);
            Assert.That(vertical.Font.VerticalHeader.Version, Is.EqualTo(0x00011000u));
            Assert.That(vertical.Font.VerticalHeader.NumberOfVerticalMetrics, Is.EqualTo(2));
            Assert.That(vertical.Font.VerticalMetrics.Select(metric => metric.AdvanceHeight), Is.EqualTo(new[] { 1000, 1100, 1100 }));
            Assert.That(vertical.Font.VerticalMetrics.Select(metric => metric.TopSideBearing), Is.EqualTo(new[] { 50, 40, -10 }));
            Assert.That(incomplete.Error, Is.EqualTo(SvgOpenTypeError.InvalidTable));
        });
    }

    static void AssertFailure(SvgOpenTypeReadResult result, SvgOpenTypeError error, SvgSourceSpan source)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidFont));
        Assert.That(result.Diagnostics[0].Source, Is.SameAs(source));
    }

    internal static byte[] BuildFont(bool cff, bool includePost)
    {
        return BuildFont(cff, includePost, false);
    }

    static byte[] BuildFont(bool cff, bool includePost, bool vertical)
    {
        List<FontTable> tables = new List<FontTable>();
        tables.Add(new FontTable("OS/2", BuildOs2()));
        if (cff) tables.Add(new FontTable("CFF ", new byte[] { 1, 0, 4, 1 }));
        tables.Add(new FontTable("cmap", BuildCmap()));
        if (!cff) tables.Add(new FontTable("glyf", Array.Empty<byte>()));
        tables.Add(new FontTable("head", BuildHead()));
        tables.Add(new FontTable("hhea", BuildHhea()));
        tables.Add(new FontTable("hmtx", BuildHmtx()));
        if (!cff) tables.Add(new FontTable("loca", new byte[8]));
        tables.Add(new FontTable("maxp", BuildMaxp(cff)));
        tables.Add(new FontTable("name", BuildName()));
        if (includePost) tables.Add(new FontTable("post", BuildPost()));
        if (vertical)
        {
            tables.Add(new FontTable("vhea", BuildVhea()));
            tables.Add(new FontTable("vmtx", BuildVmtx()));
        }
        tables.Sort((first, second) => string.CompareOrdinal(first.Tag, second.Tag));

        int tableCount = tables.Count;
        int directoryLength = 12 + tableCount * 16;
        int offset = Align4(directoryLength);
        int index = 0;
        while (index < tables.Count)
        {
            tables[index].Offset = offset;
            offset = offset + Align4(tables[index].Bytes.Length);
            index = index + 1;
        }
        byte[] font = new byte[offset];
        WriteUInt32(font, 0, cff ? 0x4F54544Fu : 0x00010000u);
        WriteUInt16(font, 4, tableCount);
        int selector = 0;
        int power = 1;
        while (power <= tableCount / 2)
        {
            power = power * 2;
            selector = selector + 1;
        }
        WriteUInt16(font, 6, power * 16);
        WriteUInt16(font, 8, selector);
        WriteUInt16(font, 10, tableCount * 16 - power * 16);
        index = 0;
        while (index < tables.Count)
        {
            FontTable table = tables[index];
            Array.Copy(table.Bytes, 0, font, table.Offset, table.Bytes.Length);
            int record = 12 + index * 16;
            WriteTag(font, record, table.Tag);
            uint checksum = Checksum(table.Bytes);
            WriteUInt32(font, record + 4, checksum);
            WriteUInt32(font, record + 8, (uint)table.Offset);
            WriteUInt32(font, record + 12, (uint)table.Bytes.Length);
            index = index + 1;
        }
        int headOffset = FindTableOffset(font, "head");
        uint adjustment = 0xB1B0AFBAu - Checksum(font);
        WriteUInt32(font, headOffset + 8, adjustment);
        return font;
    }

    static byte[] BuildHead()
    {
        byte[] table = new byte[54];
        WriteUInt16(table, 0, 1);
        WriteUInt16(table, 2, 0);
        WriteUInt32(table, 4, 0x00010000u);
        WriteUInt32(table, 8, 0);
        WriteUInt32(table, 12, 0x5F0F3CF5u);
        WriteUInt16(table, 16, 3);
        WriteUInt16(table, 18, 1000);
        WriteInt16(table, 36, 0);
        WriteInt16(table, 38, -200);
        WriteInt16(table, 40, 1000);
        WriteInt16(table, 42, 800);
        WriteUInt16(table, 44, 1);
        WriteUInt16(table, 46, 8);
        WriteInt16(table, 48, 2);
        WriteInt16(table, 50, 0);
        WriteInt16(table, 52, 0);
        return table;
    }

    static byte[] BuildHhea()
    {
        byte[] table = new byte[36];
        WriteUInt16(table, 0, 1);
        WriteUInt16(table, 2, 0);
        WriteInt16(table, 4, 800);
        WriteInt16(table, 6, -200);
        WriteInt16(table, 8, 200);
        WriteUInt16(table, 10, 600);
        WriteInt16(table, 12, -20);
        WriteInt16(table, 14, 0);
        WriteInt16(table, 16, 610);
        WriteInt16(table, 18, 1);
        WriteInt16(table, 20, 0);
        WriteInt16(table, 22, 0);
        WriteInt16(table, 32, 0);
        WriteUInt16(table, 34, 2);
        return table;
    }

    static byte[] BuildHmtx()
    {
        byte[] table = new byte[10];
        WriteUInt16(table, 0, 500);
        WriteInt16(table, 2, 0);
        WriteUInt16(table, 4, 600);
        WriteInt16(table, 6, 10);
        WriteInt16(table, 8, -20);
        return table;
    }

    static byte[] BuildVhea()
    {
        byte[] table = new byte[36];
        WriteUInt32(table, 0, 0x00011000u);
        WriteInt16(table, 4, 500);
        WriteInt16(table, 6, -500);
        WriteInt16(table, 8, 0);
        WriteUInt16(table, 10, 1100);
        WriteInt16(table, 12, -10);
        WriteInt16(table, 14, 0);
        WriteInt16(table, 16, 1000);
        WriteInt16(table, 18, 0);
        WriteInt16(table, 20, 1);
        WriteInt16(table, 22, 0);
        WriteInt16(table, 32, 0);
        WriteUInt16(table, 34, 2);
        return table;
    }

    static byte[] BuildVmtx()
    {
        byte[] table = new byte[10];
        WriteUInt16(table, 0, 1000);
        WriteInt16(table, 2, 50);
        WriteUInt16(table, 4, 1100);
        WriteInt16(table, 6, 40);
        WriteInt16(table, 8, -10);
        return table;
    }

    static byte[] BuildMaxp(bool cff)
    {
        if (cff)
        {
            byte[] compact = new byte[6];
            WriteUInt32(compact, 0, 0x00005000u);
            WriteUInt16(compact, 4, 3);
            return compact;
        }
        byte[] table = new byte[32];
        WriteUInt32(table, 0, 0x00010000u);
        WriteUInt16(table, 4, 3);
        WriteUInt16(table, 6, 4);
        WriteUInt16(table, 8, 1);
        WriteUInt16(table, 10, 8);
        WriteUInt16(table, 12, 2);
        WriteUInt16(table, 14, 2);
        WriteUInt16(table, 28, 2);
        WriteUInt16(table, 30, 1);
        return table;
    }

    static byte[] BuildOs2()
    {
        byte[] table = new byte[96];
        WriteUInt16(table, 0, 4);
        WriteInt16(table, 2, 500);
        WriteUInt16(table, 4, 700);
        WriteUInt16(table, 6, 5);
        WriteUInt16(table, 8, 0);
        WriteInt16(table, 30, 0);
        WriteTag(table, 58, "TEST");
        WriteUInt16(table, 62, 32);
        WriteUInt16(table, 64, 65);
        WriteUInt16(table, 66, 90);
        WriteInt16(table, 68, 750);
        WriteInt16(table, 70, -250);
        WriteInt16(table, 72, 100);
        WriteUInt16(table, 74, 900);
        WriteUInt16(table, 76, 300);
        WriteInt16(table, 86, 500);
        WriteInt16(table, 88, 700);
        WriteUInt16(table, 90, 0);
        WriteUInt16(table, 92, 32);
        WriteUInt16(table, 94, 2);
        return table;
    }

    static byte[] BuildName()
    {
        List<NameValue> values = new List<NameValue>();
        values.Add(new NameValue(0, 4, 0, 1, "Fallback"));
        values.Add(new NameValue(3, 1, 0x8000, 1, "Legacy Family"));
        values.Add(new NameValue(3, 1, 0x8000, 2, "Regular"));
        values.Add(new NameValue(3, 1, 0x8000, 4, "Fixture Regular"));
        values.Add(new NameValue(3, 1, 0x8000, 5, "Version 1.000"));
        values.Add(new NameValue(3, 1, 0x8000, 6, "Fixture-Regular"));
        values.Add(new NameValue(3, 1, 0x8000, 16, "Fixture"));
        values.Add(new NameValue(3, 1, 0x8000, 17, "Book"));
        byte[] language = Utf16BigEndian("en-US");
        int storageOffset = 6 + values.Count * 12 + 2 + 4;
        int storageLength = language.Length;
        int index = 0;
        while (index < values.Count)
        {
            values[index].Bytes = Utf16BigEndian(values[index].Text);
            values[index].Offset = storageLength;
            storageLength = storageLength + values[index].Bytes.Length;
            index = index + 1;
        }
        byte[] table = new byte[storageOffset + storageLength];
        WriteUInt16(table, 0, 1);
        WriteUInt16(table, 2, values.Count);
        WriteUInt16(table, 4, storageOffset);
        index = 0;
        while (index < values.Count)
        {
            NameValue value = values[index];
            int record = 6 + index * 12;
            WriteUInt16(table, record, value.Platform);
            WriteUInt16(table, record + 2, value.Encoding);
            WriteUInt16(table, record + 4, value.Language);
            WriteUInt16(table, record + 6, value.Name);
            WriteUInt16(table, record + 8, value.Bytes.Length);
            WriteUInt16(table, record + 10, value.Offset);
            Array.Copy(value.Bytes, 0, table, storageOffset + value.Offset, value.Bytes.Length);
            index = index + 1;
        }
        int languageRecord = 6 + values.Count * 12;
        WriteUInt16(table, languageRecord, 1);
        WriteUInt16(table, languageRecord + 2, language.Length);
        WriteUInt16(table, languageRecord + 4, 0);
        Array.Copy(language, 0, table, storageOffset, language.Length);
        return table;
    }

    static byte[] BuildCmap()
    {
        byte[] format12 = new byte[28];
        WriteUInt16(format12, 0, 12);
        WriteUInt16(format12, 2, 0);
        WriteUInt32(format12, 4, 28);
        WriteUInt32(format12, 8, 0);
        WriteUInt32(format12, 12, 1);
        WriteUInt32(format12, 16, 65);
        WriteUInt32(format12, 20, 65);
        WriteUInt32(format12, 24, 1);

        byte[] format4 = new byte[32];
        WriteUInt16(format4, 0, 4);
        WriteUInt16(format4, 2, 32);
        WriteUInt16(format4, 4, 0);
        WriteUInt16(format4, 6, 4);
        WriteUInt16(format4, 8, 4);
        WriteUInt16(format4, 10, 1);
        WriteUInt16(format4, 12, 0);
        WriteUInt16(format4, 14, 65);
        WriteUInt16(format4, 16, 0xFFFF);
        WriteUInt16(format4, 18, 0);
        WriteUInt16(format4, 20, 65);
        WriteUInt16(format4, 22, 0xFFFF);
        WriteInt16(format4, 24, -64);
        WriteInt16(format4, 26, 1);
        WriteUInt16(format4, 28, 0);
        WriteUInt16(format4, 30, 0);

        byte[] table = new byte[20 + format12.Length + format4.Length];
        WriteUInt16(table, 0, 0);
        WriteUInt16(table, 2, 2);
        WriteUInt16(table, 4, 0);
        WriteUInt16(table, 6, 4);
        WriteUInt32(table, 8, 20);
        WriteUInt16(table, 12, 3);
        WriteUInt16(table, 14, 1);
        WriteUInt32(table, 16, (uint)(20 + format12.Length));
        Array.Copy(format12, 0, table, 20, format12.Length);
        Array.Copy(format4, 0, table, 20 + format12.Length, format4.Length);
        return table;
    }

    static byte[] BuildPost()
    {
        byte[] table = new byte[32];
        WriteUInt32(table, 0, 0x00030000u);
        WriteUInt32(table, 4, 0xFFF38000u);
        WriteInt16(table, 8, -100);
        WriteInt16(table, 10, 50);
        WriteUInt32(table, 12, 1);
        return table;
    }

    static byte[] Utf16BigEndian(string value)
    {
        byte[] bytes = new byte[value.Length * 2];
        int index = 0;
        while (index < value.Length)
        {
            bytes[index * 2] = (byte)(value[index] >> 8);
            bytes[index * 2 + 1] = (byte)value[index];
            index = index + 1;
        }
        return bytes;
    }

    static int FindTableOffset(byte[] font, string tag)
    {
        int count = ReadUInt16(font, 4);
        int index = 0;
        while (index < count)
        {
            int record = 12 + index * 16;
            if (ReadTag(font, record) == tag) return (int)ReadUInt32(font, record + 8);
            index = index + 1;
        }
        return -1;
    }

    static int FindTableRecord(byte[] font, string tag)
    {
        int count = ReadUInt16(font, 4);
        int index = 0;
        while (index < count)
        {
            int record = 12 + index * 16;
            if (ReadTag(font, record) == tag) return record;
            index = index + 1;
        }
        return -1;
    }

    static void SwapDirectoryRecords(byte[] font, int first, int second)
    {
        int firstOffset = 12 + first * 16;
        int secondOffset = 12 + second * 16;
        int index = 0;
        while (index < 16)
        {
            byte temporary = font[firstOffset + index];
            font[firstOffset + index] = font[secondOffset + index];
            font[secondOffset + index] = temporary;
            index = index + 1;
        }
    }

    static int Align4(int value)
    {
        return (value + 3) & -4;
    }

    static uint Checksum(byte[] bytes)
    {
        uint sum = 0;
        int index = 0;
        while (index < Align4(bytes.Length))
        {
            uint word = 0;
            int part = 0;
            while (part < 4)
            {
                int offset = index + part;
                byte value = offset < bytes.Length ? bytes[offset] : (byte)0;
                word = word | (uint)value << (24 - part * 8);
                part = part + 1;
            }
            sum = sum + word;
            index = index + 4;
        }
        return sum;
    }

    static string ReadTag(byte[] bytes, int offset)
    {
        return new string(new[] { (char)bytes[offset], (char)bytes[offset + 1], (char)bytes[offset + 2], (char)bytes[offset + 3] });
    }

    static int ReadUInt16(byte[] bytes, int offset)
    {
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    static uint ReadUInt32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }

    static void WriteTag(byte[] bytes, int offset, string value)
    {
        bytes[offset] = (byte)value[0];
        bytes[offset + 1] = (byte)value[1];
        bytes[offset + 2] = (byte)value[2];
        bytes[offset + 3] = (byte)value[3];
    }

    static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void WriteInt16(byte[] bytes, int offset, int value)
    {
        WriteUInt16(bytes, offset, value & 0xFFFF);
    }

    static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    class FontTable
    {
        public string Tag;
        public byte[] Bytes;
        public int Offset;

        public FontTable(string tag, byte[] bytes)
        {
            Tag = tag;
            Bytes = bytes;
            Offset = 0;
        }
    }

    class NameValue
    {
        public int Platform;
        public int Encoding;
        public int Language;
        public int Name;
        public string Text;
        public byte[] Bytes;
        public int Offset;

        public NameValue(int platform, int encoding, int language, int name, string text)
        {
            Platform = platform;
            Encoding = encoding;
            Language = language;
            Name = name;
            Text = text;
            Bytes = Array.Empty<byte>();
            Offset = 0;
        }
    }
}
