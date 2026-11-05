namespace ttf.Tests;

internal class SfntPostTableTests
{
    [Test]
    public void ParsesVersionOneStandardGlyphOrderAndHeaderMetrics()
    {
        byte[] post = Header(0x00010000U).CopyBytes();
        SfntPostTable? table = Parse(post, 258);

        Assert.Multiple(() =>
        {
            Assert.That(SfntPostStandardNames.GetCount(), Is.EqualTo(258));
            Assert.That(table!.GetVersionRaw(), Is.EqualTo(0x00010000U));
            Assert.That(table.GetVersion(), Is.EqualTo(1.0));
            Assert.That(table.GetFormat(), Is.EqualTo(SfntPostFormat.Version1));
            Assert.That(table.GetGlyphCount(), Is.EqualTo(258));
            Assert.That(table.GetGlyphName(0), Is.EqualTo(".notdef"));
            Assert.That(table.GetGlyphName(36), Is.EqualTo("A"));
            Assert.That(table.GetGlyphName(257), Is.EqualTo("dcroat"));
            Assert.That(table.GetGlyphNameRecord(36).GetGlyphId(), Is.EqualTo(36));
            Assert.That(table.GetGlyphNameRecord(36).GetNameIndex(), Is.EqualTo(36));
            Assert.That(table.GetGlyphNameRecord(36).IsStandardName(), Is.True);
            Assert.That(table.GetGlyphNameRecord(36).HasValidName(), Is.True);
            Assert.That(table.GetProvidedGlyphNameCount(), Is.EqualTo(258));
            Assert.That(table.GetCustomNameCount(), Is.Zero);
            Assert.That(table.GetInvalidNameIndexCount(), Is.Zero);
            Assert.That(table.GetItalicAngleRaw(), Is.EqualTo(-819200));
            Assert.That(table.GetItalicAngle(), Is.EqualTo(-12.5));
            Assert.That(table.GetUnderlinePosition(), Is.EqualTo(-75));
            Assert.That(table.GetUnderlineThickness(), Is.EqualTo(50));
            Assert.That(table.GetFixedPitchRaw(), Is.EqualTo(2U));
            Assert.That(table.IsFixedPitch(), Is.True);
            Assert.That(table.GetMinimumType42Memory(), Is.EqualTo(100U));
            Assert.That(table.GetMaximumType42Memory(), Is.EqualTo(200U));
            Assert.That(table.GetMinimumType1Memory(), Is.EqualTo(300U));
            Assert.That(table.GetMaximumType1Memory(), Is.EqualTo(400U));
        });
    }

    [Test]
    public void ParsesVersionTwoStandardAndCustomNames()
    {
        TestBinaryBuilder builder = Header(0x00020000U)
            .WriteUInt16(4)
            .WriteUInt16(0)
            .WriteUInt16(36)
            .WriteUInt16(258)
            .WriteUInt16(259);
        WritePascalString(builder, "custom_name");
        WritePascalString(builder, "A.alt");

        SfntPostTable? table = Parse(builder.CopyBytes(), 4);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetFormat(), Is.EqualTo(SfntPostFormat.Version2));
            Assert.That(table.GetGlyphName(0), Is.EqualTo(".notdef"));
            Assert.That(table.GetGlyphName(1), Is.EqualTo("A"));
            Assert.That(table.GetGlyphName(2), Is.EqualTo("custom_name"));
            Assert.That(table.GetGlyphName(3), Is.EqualTo("A.alt"));
            Assert.That(table.GetGlyphNameRecord(2).IsCustomName(), Is.True);
            Assert.That(table.GetGlyphNameRecord(2).IsStandardName(), Is.False);
            Assert.That(table.GetGlyphNameRecord(2).GetNameIndex(), Is.EqualTo(258));
            Assert.That(table.GetCustomNameCount(), Is.EqualTo(2));
            Assert.That(table.GetCustomName(0), Is.EqualTo("custom_name"));
            Assert.That(table.GetCustomName(1), Is.EqualTo("A.alt"));
            Assert.That(table.GetInvalidNameIndexCount(), Is.Zero);
            Assert.That(table.GetInvalidGlyphNameCount(), Is.Zero);
            Assert.That(table.GetProvidedGlyphNameCount(), Is.EqualTo(4));
        });
    }

    [Test]
    public void PreservesMissingCustomNameIndexesWithSyntheticFallbacks()
    {
        TestBinaryBuilder builder = Header(0x00020000U)
            .WriteUInt16(1)
            .WriteUInt16(260);
        WritePascalString(builder, "first");
        WritePascalString(builder, "second");

        SfntPostTable? table = Parse(builder.CopyBytes(), 1);
        SfntPostGlyphName glyphName = table!.GetGlyphNameRecord(0);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetCustomNameCount(), Is.EqualTo(2));
            Assert.That(table.GetInvalidNameIndexCount(), Is.EqualTo(1));
            Assert.That(table.GetInvalidGlyphNameCount(), Is.EqualTo(1));
            Assert.That(table.GetProvidedGlyphNameCount(), Is.Zero);
            Assert.That(glyphName.GetNameIndex(), Is.EqualTo(260));
            Assert.That(glyphName.GetName(), Is.EqualTo("gid0000"));
            Assert.That(glyphName.HasValidNameIndex(), Is.False);
            Assert.That(glyphName.HasValidName(), Is.False);
            Assert.That(glyphName.IsSynthetic(), Is.True);
        });
    }

    [TestCase("")]
    [TestCase("bad-name")]
    [TestCase("bad name")]
    public void PreservesInvalidCustomNamesWithSyntheticFallbacks(string customName)
    {
        TestBinaryBuilder builder = Header(0x00020000U)
            .WriteUInt16(1)
            .WriteUInt16(258);
        WritePascalString(builder, customName);

        SfntPostTable? table = Parse(builder.CopyBytes(), 1);
        SfntPostGlyphName glyphName = table!.GetGlyphNameRecord(0);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetCustomName(0), Is.EqualTo(customName));
            Assert.That(table.GetInvalidNameIndexCount(), Is.Zero);
            Assert.That(table.GetInvalidGlyphNameCount(), Is.EqualTo(1));
            Assert.That(glyphName.HasValidNameIndex(), Is.True);
            Assert.That(glyphName.HasValidName(), Is.False);
            Assert.That(glyphName.GetName(), Is.EqualTo("gid0000"));
        });
    }

    [Test]
    public void RejectsCustomNamesLongerThanSixtyThreeCharacters()
    {
        TestBinaryBuilder builder = Header(0x00020000U)
            .WriteUInt16(1)
            .WriteUInt16(258);
        WritePascalString(builder, new string('a', 64));

        SfntPostTable? table = Parse(builder.CopyBytes(), 1);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetCustomName(0).Length, Is.EqualTo(64));
            Assert.That(table.GetInvalidGlyphNameCount(), Is.EqualTo(1));
            Assert.That(table.GetGlyphName(0), Is.EqualTo("gid0000"));
        });
    }

    [Test]
    public void ParsesDeprecatedVersionTwoPointFiveOffsets()
    {
        byte[] post = Header(0x00025000U)
            .WriteUInt16(3)
            .WriteInt8(36)
            .WriteInt8(36)
            .WriteInt8(-3)
            .CopyBytes();
        SfntPostTable? table = Parse(post, 3);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetVersion(), Is.EqualTo(2.5));
            Assert.That(table.GetFormat(), Is.EqualTo(SfntPostFormat.Version25));
            Assert.That(table.GetGlyphName(0), Is.EqualTo("A"));
            Assert.That(table.GetGlyphName(1), Is.EqualTo("B"));
            Assert.That(table.GetGlyphName(2), Is.EqualTo("gid0002"));
            Assert.That(table.GetGlyphNameRecord(2).GetNameIndex(), Is.EqualTo(-1));
            Assert.That(table.GetGlyphNameRecord(2).HasValidNameIndex(), Is.False);
            Assert.That(table.GetInvalidNameIndexCount(), Is.EqualTo(1));
            Assert.That(table.GetInvalidGlyphNameCount(), Is.EqualTo(1));
            Assert.That(table.GetProvidedGlyphNameCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void VersionThreeProvidesSyntheticNamesWithoutInvalidIndexes()
    {
        SfntPostTable? table = Parse(Header(0x00030000U).CopyBytes(), 3);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetFormat(), Is.EqualTo(SfntPostFormat.Version3));
            Assert.That(table.ProvidesGlyphNames(), Is.False);
            Assert.That(table.GetProvidedGlyphNameCount(), Is.Zero);
            Assert.That(table.GetGlyphName(0), Is.EqualTo("gid0000"));
            Assert.That(table.GetGlyphName(2), Is.EqualTo("gid0002"));
            Assert.That(table.GetGlyphNameRecord(2).IsSynthetic(), Is.True);
            Assert.That(table.GetGlyphNameRecord(2).HasValidNameIndex(), Is.True);
            Assert.That(table.GetGlyphNameRecord(2).HasValidName(), Is.False);
            Assert.That(table.GetInvalidNameIndexCount(), Is.Zero);
            Assert.That(table.GetInvalidGlyphNameCount(), Is.Zero);
        });
    }

    [Test]
    public void ParsesAppleVersionFourCharacterCodes()
    {
        byte[] post = Header(0x00040000U)
            .WriteUInt16(0x0041)
            .WriteUInt16(0x4E2D)
            .WriteUInt16(0xFFFF)
            .CopyBytes();
        SfntPostTable? table = Parse(post, 3);

        Assert.Multiple(() =>
        {
            Assert.That(table!.GetFormat(), Is.EqualTo(SfntPostFormat.Version4));
            Assert.That(table.GetGlyphName(0), Is.EqualTo("a0041"));
            Assert.That(table.GetGlyphName(1), Is.EqualTo("a4E2D"));
            Assert.That(table.GetGlyphName(2), Is.EqualTo("gid0002"));
            Assert.That(table.GetGlyphNameRecord(0).GetNameIndex(), Is.EqualTo(0x0041));
            Assert.That(table.GetGlyphNameRecord(2).GetNameIndex(), Is.EqualTo(0xFFFF));
            Assert.That(table.GetGlyphNameRecord(2).HasValidNameIndex(), Is.True);
            Assert.That(table.GetGlyphNameRecord(2).IsSynthetic(), Is.True);
            Assert.That(table.GetProvidedGlyphNameCount(), Is.EqualTo(2));
            Assert.That(table.GetInvalidNameIndexCount(), Is.Zero);
        });
    }

    [Test]
    public void RejectsEveryTruncationOfTheCommonHeader()
    {
        byte[] post = Header(0x00030000U).CopyBytes();

        for (int length = 0; length < 32; length += 1)
        {
            byte[] truncated = post.AsSpan(0, length).ToArray();
            Assert.Throws<FontFormatException>(
                () => Parse(truncated, 1),
                "Length {0}",
                length);
        }
    }

    [Test]
    public void RejectsMismatchedGlyphCountsAndFixedLengthForms()
    {
        byte[] versionOneExtra = Append(
            Header(0x00010000U).CopyBytes(),
            new byte[] { 0 });
        byte[] versionThreeExtra = Append(
            Header(0x00030000U).CopyBytes(),
            new byte[] { 0 });
        byte[] versionTwo = Header(0x00020000U)
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .CopyBytes();
        byte[] versionTwoFive = Header(0x00025000U)
            .WriteUInt16(1)
            .WriteInt8(0)
            .WriteUInt8(0)
            .CopyBytes();
        byte[] versionFour = Header(0x00040000U)
            .WriteUInt16(0)
            .CopyBytes();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(versionOneExtra, 258));
            Assert.Throws<FontFormatException>(
                () => Parse(Header(0x00010000U).CopyBytes(), 257));
            Assert.Throws<FontFormatException>(() => Parse(versionThreeExtra, 1));
            Assert.Throws<FontFormatException>(() => Parse(versionTwo, 1));
            Assert.Throws<FontFormatException>(() => Parse(versionTwoFive, 1));
            Assert.Throws<FontFormatException>(() => Parse(versionFour, 2));
        });
    }

    [Test]
    public void RejectsTruncatedPascalStringsAndTrailingStringData()
    {
        byte[] truncated = Header(0x00020000U)
            .WriteUInt16(1)
            .WriteUInt16(258)
            .WriteUInt8(3)
            .WriteUInt8((byte)'A')
            .CopyBytes();
        byte[] trailing = Header(0x00020000U)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt8(0)
            .CopyBytes();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(truncated, 1));
            Assert.Throws<FontFormatException>(() => Parse(trailing, 1));
        });
    }

    [Test]
    public void AppliesTheConfiguredStringLimit()
    {
        TestBinaryBuilder builder = Header(0x00020000U)
            .WriteUInt16(1)
            .WriteUInt16(258);
        WritePascalString(builder, "long");
        SfntTableCache cache = CreateCache(
            builder.CopyBytes(),
            1,
            FontLoadMode.Compatible,
            CreateLimits(3));

        Assert.Throws<FontLimitException>(() => SfntPostTableParser.FindTable(cache));
    }

    [TestCase(0x00020100U)]
    [TestCase(0x00050000U)]
    public void KeepsUnknownVersionsRaw(uint version)
    {
        byte[] post = Header(version).CopyBytes();
        SfntTableCache compatible = CreateCache(
            post,
            1,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntTableCache strict = CreateCache(
            post,
            1,
            FontLoadMode.Strict,
            FontLimits.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(SfntPostTableParser.FindTable(compatible), Is.Null);
            Assert.That(SfntPostTableParser.FindView(compatible)!.HasParsedData(), Is.False);
            Assert.That(SfntPostTableParser.FindView(compatible)!.GetVersionSupport(),
                Is.EqualTo(SfntTableVersionSupport.RawOnly));
            Assert.That(SfntPostTableParser.FindTable(strict), Is.Null);
        });
    }

    [Test]
    public void RequiresMaxpAndChecksAccessorIndexes()
    {
        byte[] post = Header(0x00030000U).CopyBytes();
        SfntTableCache withoutMaxp = CreateCacheWithoutMaxp(post);
        SfntPostTable? table = Parse(post, 1);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => SfntPostTableParser.FindTable(withoutMaxp));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => table!.GetGlyphNameRecord(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetGlyphName(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetCustomName(0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SfntPostStandardNames.GetName(258));
        });
    }

    [Test]
    public void FaceCachesPostTableAndMissingTableReturnsNoView()
    {
        SfntTableCache cache = CreateCache(
            Header(0x00030000U).CopyBytes(),
            1,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        SfntValidationResult validation = new SfntValidationResult(
            FontOutlineKind.Cff2,
            Array.Empty<FontDiagnostic>());
        FontFace face = new FontFace(0, 0x4F54544FU, validation, cache);
        SfntPostTable? first = face.GetPostTable();
        SfntTableCache missing = CreateCacheWithoutPost(1);

        Assert.Multiple(() =>
        {
            Assert.That(face.GetPostTable(), Is.SameAs(first));
            Assert.That(face.GetPostTableView()!.GetParsedData(), Is.SameAs(first));
            Assert.That(SfntPostTableParser.FindTable(missing), Is.Null);
            Assert.That(SfntPostTableParser.FindView(missing), Is.Null);
            Assert.Throws<ArgumentNullException>(() => SfntPostTableParser.FindView(null!));
        });
    }

    static SfntPostTable? Parse(byte[] post, int glyphCount)
    {
        return SfntPostTableParser.FindTable(CreateCache(
            post,
            glyphCount,
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(
        byte[] post,
        int glyphCount,
        FontLoadMode mode,
        FontLimits limits)
    {
        return CreateCacheFromTables(
            new byte[][] { BuildMaxp(glyphCount), post },
            new int[] { FontTags.Maxp, FontTags.Post },
            mode,
            limits);
    }

    static SfntTableCache CreateCacheWithoutMaxp(byte[] post)
    {
        return CreateCacheFromTables(
            new byte[][] { post },
            new int[] { FontTags.Post },
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
    }

    static SfntTableCache CreateCacheWithoutPost(int glyphCount)
    {
        return CreateCacheFromTables(
            new byte[][] { BuildMaxp(glyphCount) },
            new int[] { FontTags.Maxp },
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
    }

    static SfntTableCache CreateCacheFromTables(
        byte[][] bytes,
        int[] tags,
        FontLoadMode mode,
        FontLimits limits)
    {
        SfntTableData[] tables = new SfntTableData[bytes.Length];

        for (int index = 0; index < bytes.Length; index += 1)
        {
            FontData data = FontData.Create(bytes[index]);
            tables[index] = new SfntTableData(
                tags[index],
                FontChecksum.CalculateTable(bytes[index]),
                data.GetSlice(0, data.GetLength()),
                SfntTableClassification.Known,
                true);
        }

        return SfntTableCache.Create(
            new SfntTableStore(tables, Array.Empty<FontDiagnostic>()),
            mode,
            limits);
    }

    static TestBinaryBuilder Header(uint version)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(version)
            .WriteInt32(-819200)
            .WriteInt16(-75)
            .WriteInt16(50)
            .WriteUInt32(2)
            .WriteUInt32(100)
            .WriteUInt32(200)
            .WriteUInt32(300)
            .WriteUInt32(400);
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static void WritePascalString(TestBinaryBuilder builder, string value)
    {
        builder.WriteUInt8((byte)value.Length);

        for (int index = 0; index < value.Length; index += 1)
        {
            builder.WriteUInt8((byte)value[index]);
        }
    }

    static byte[] Append(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }

    static FontLimits CreateLimits(int maxStringBytes)
    {
        return FontLimits.Create(
            1000000,
            1000000,
            10,
            100,
            65535,
            1000,
            1000,
            10,
            10,
            10,
            100,
            100000,
            100000,
            maxStringBytes,
            1000000,
            100,
            1000,
            100,
            100);
    }
}
