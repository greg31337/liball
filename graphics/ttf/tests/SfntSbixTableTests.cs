using ttf;

namespace ttf.Tests;

internal class SfntSbixTableTests
{
    [Test]
    public void ParsesStrikesPayloadsOriginsAndResolutionMetadata()
    {
        byte[] pngPayload = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        byte[] jpegPayload = new byte[] { 0xFF, 0xD8, 0xFF };
        byte[] tiffPayload = new byte[] { 0x49, 0x49, 0x2A, 0x00 };
        byte[] firstStrike = BuildStrike(
            16,
            96,
            new byte[][] {
                null!,
                BuildGlyph(-2, 3, "png ", pngPayload),
                BuildGlyph(5, -6, "dupe", new byte[] { 0, 1 }),
                BuildGlyph(7, 8, "jpg ", jpegPayload)
            });
        byte[] secondStrike = BuildStrike(
            32,
            192,
            new byte[][] {
                BuildGlyph(1, 2, "tiff", tiffPayload),
                null!,
                null!,
                null!
            });
        SfntSbixTable? table = Parse(
            BuildSbix(3, new byte[][] { firstStrike, secondStrike }),
            4);
        SfntSbixStrike strike = table!.GetStrike(0);
        SfntSbixGlyphData png = strike.GetGlyphData(1);
        SfntSbixGlyphData duplicate = strike.GetGlyphData(2);
        SfntSbixGlyphImage? resolved = strike.ResolveGlyphImage(2);
        byte[] copy = resolved!.CopyData();
        copy[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(table.GetVersion(), Is.EqualTo(1));
            Assert.That(table.GetFlags(), Is.EqualTo(3));
            Assert.That(table.DrawOutlines(), Is.True);
            Assert.That(table.GetStrikeCount(), Is.EqualTo(2));
            Assert.That(strike.GetPixelsPerEm(), Is.EqualTo(16));
            Assert.That(strike.GetPixelsPerInch(), Is.EqualTo(96));
            Assert.That(strike.GetGlyphCount(), Is.EqualTo(4));
            Assert.That(strike.HasGlyphData(0), Is.False);
            Assert.That(strike.ResolveGlyphImage(0), Is.Null);
            Assert.That(png.GetOriginOffsetX(), Is.EqualTo(-2));
            Assert.That(png.GetOriginOffsetY(), Is.EqualTo(3));
            Assert.That(png.GetGraphicKind(), Is.EqualTo(SfntSbixGraphicKind.Png));
            Assert.That(png.CopyData(), Is.EqualTo(pngPayload));
            Assert.That(duplicate.IsDuplicate(), Is.True);
            Assert.That(duplicate.GetDuplicateGlyphId(), Is.EqualTo(1));
            Assert.That(resolved.GetGlyphId(), Is.EqualTo(2));
            Assert.That(resolved.GetSourceGlyphId(), Is.EqualTo(1));
            Assert.That(resolved.GetOriginOffsetX(), Is.EqualTo(5));
            Assert.That(resolved.GetOriginOffsetY(), Is.EqualTo(-6));
            Assert.That(resolved.GetGraphicKind(), Is.EqualTo(SfntSbixGraphicKind.Png));
            Assert.That(resolved.CopyData(), Is.EqualTo(pngPayload));
            Assert.That(strike.ResolveGlyphImage(3)!.GetGraphicKind(),
                Is.EqualTo(SfntSbixGraphicKind.Jpeg));
            Assert.That(table.GetStrike(1).GetPixelsPerEm(), Is.EqualTo(32));
            Assert.That(table.GetStrike(1).GetPixelsPerInch(), Is.EqualTo(192));
            Assert.That(table.GetStrike(1).ResolveGlyphImage(0)!.GetGraphicKind(),
                Is.EqualTo(SfntSbixGraphicKind.Tiff));
        });
    }

    [Test]
    public void PreservesUnsupportedGraphicTagsAndRawPayloads()
    {
        byte[] payload = new byte[] { 1, 2, 3, 4 };
        byte[] strike = BuildStrike(
            20,
            72,
            new byte[][] {
                BuildGlyph(0, 0, "pdf ", payload),
                null!
            });
        SfntSbixGlyphImage? image = Parse(
            BuildSbix(1, new byte[][] { strike }),
            2)!.GetStrike(0).ResolveGlyphImage(0);

        Assert.Multiple(() =>
        {
            Assert.That(image!.GetGraphicKind(),
                Is.EqualTo(SfntSbixGraphicKind.Unsupported));
            Assert.That(image.GetGraphicTypeTag(),
                Is.EqualTo((int)TestSfntEncoding.EncodeTag("pdf ")));
            Assert.That(image.GetDataLength(), Is.EqualTo(4));
            Assert.That(image.CopyData(), Is.EqualTo(payload));
        });
    }

    [Test]
    public void ExposesPayloadKindsAndSelectsTheClosestPublicStrike()
    {
        byte[] first = BuildStrike(
            16,
            96,
            new byte[][] {
                BuildGlyph(-2, 3, "png ", new byte[] { 1 }), null!
            });
        byte[] second = BuildStrike(
            32,
            192,
            new byte[][] {
                BuildGlyph(4, -5, "jpg ", new byte[] { 2 }), null!
            });
        byte[] third = BuildStrike(
            64,
            288,
            new byte[][] {
                BuildGlyph(6, 7, "tiff", new byte[] { 3 }), null!
            });
        FontFace face = CreateFace(
            BuildSbix(3, new byte[][] { first, second, third }),
            2);
        ColorGlyphOptions options = new ColorGlyphOptions();
        options.SetPreferredRepresentation(ColorGlyphRepresentation.Sbix);
        options.SetBitmapTargetSize(28, 28);

        ColorGlyph glyph = new ColorGlyphResolver().ResolveGlyphWithOptions(
            face.CreateDefaultInstance(),
            0,
            options);
        ColorBitmapStrike[] strikes = glyph.GetBitmapStrikes();
        ColorBitmapStrike selected = glyph.GetSelectedBitmapStrike()!;

        Assert.Multiple(() =>
        {
            Assert.That(strikes, Has.Length.EqualTo(3));
            Assert.That(strikes[0].GetPayloadKind(),
                Is.EqualTo(ColorBitmapPayloadKind.Png));
            Assert.That(strikes[1].GetPayloadKind(),
                Is.EqualTo(ColorBitmapPayloadKind.Jpeg));
            Assert.That(strikes[2].GetPayloadKind(),
                Is.EqualTo(ColorBitmapPayloadKind.Tiff));
            Assert.That(strikes[0].IsCompressed(), Is.True);
            Assert.That(selected.GetPixelsPerEmX(), Is.EqualTo(32));
            Assert.That(selected.GetPixelsPerInch(), Is.EqualTo(192));
            Assert.That(selected.GetOriginOffsetX(), Is.EqualTo(4));
            Assert.That(selected.GetOriginOffsetY(), Is.EqualTo(-5));
            Assert.That(selected.GetGlyphMetrics(), Is.Null);
            Assert.That(selected.GetHorizontalLineMetrics(), Is.Null);
            Assert.That(glyph.GetMonochromeFallbackGuidance(), Is.EqualTo(
                ColorMonochromeFallbackGuidance.RequiredButUnavailable));
            Assert.That(options.HasBitmapTargetSize(), Is.True);
            Assert.That(options.GetTargetPixelsPerEmX(), Is.EqualTo(28));
            Assert.That(options.GetTargetPixelsPerEmY(), Is.EqualTo(28));
        });

        options.ClearBitmapTargetSize();
        ColorGlyph firstSelected = new ColorGlyphResolver().ResolveGlyphWithOptions(
            face.CreateDefaultInstance(),
            0,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(options.HasBitmapTargetSize(), Is.False);
            Assert.That(firstSelected.GetSelectedBitmapStrike()!.GetPixelsPerEmX(),
                Is.EqualTo(16));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetBitmapTargetSize(0, 16));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetBitmapTargetSize(16, 65536));
        });
    }

    [Test]
    public void KeepsUnsupportedSbixPayloadsInspectableButNotSelectable()
    {
        byte[] payload = new byte[] { 1, 2, 3, 4 };
        byte[] strike = BuildStrike(
            20,
            72,
            new byte[][] {
                BuildGlyph(0, 0, "pdf ", payload), null!
            });
        FontFace face = CreateFace(
            BuildSbix(1, new byte[][] { strike }),
            2);

        ColorGlyph glyph = new ColorGlyphResolver().ResolveGlyph(
            face.CreateDefaultInstance(),
            0);
        ColorBitmapStrike exposed = glyph.GetBitmapStrikes()[0];

        Assert.Multiple(() =>
        {
            Assert.That(glyph.GetSelectedRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.None));
            Assert.That(glyph.GetSelectedBitmapStrike(), Is.Null);
            Assert.That(exposed.GetPayloadKind(),
                Is.EqualTo(ColorBitmapPayloadKind.Unsupported));
            Assert.That(exposed.IsSupported(), Is.False);
            Assert.That(exposed.GetImageFormat(),
                Is.EqualTo((int)TestSfntEncoding.EncodeTag("pdf ")));
            Assert.That(exposed.CopyData(), Is.EqualTo(payload));
        });
    }

    [Test]
    public void ResolvesMultiHopDuplicateChains()
    {
        byte[] payload = new byte[] { 9, 8, 7 };
        byte[] strike = BuildStrike(
            24,
            144,
            new byte[][] {
                BuildGlyph(1, 2, "png ", payload),
                BuildGlyph(3, 4, "dupe", new byte[] { 0, 0 }),
                BuildGlyph(5, 6, "dupe", new byte[] { 0, 1 }),
                BuildGlyph(7, 8, "dupe", new byte[] { 0, 2 })
            });
        SfntSbixGlyphImage? image = Parse(
            BuildSbix(1, new byte[][] { strike }),
            4)!.GetStrike(0).ResolveGlyphImage(3);

        Assert.Multiple(() =>
        {
            Assert.That(image!.GetGlyphId(), Is.EqualTo(3));
            Assert.That(image.GetSourceGlyphId(), Is.Zero);
            Assert.That(image.GetOriginOffsetX(), Is.EqualTo(7));
            Assert.That(image.GetOriginOffsetY(), Is.EqualTo(8));
            Assert.That(image.CopyData(), Is.EqualTo(payload));
        });
    }

    [TestCaseSource(nameof(InvalidSbixTables))]
    public void RejectsMalformedHeadersOffsetsGlyphRecordsAndDuplicates(byte[] bytes)
    {
        Assert.Throws<FontFormatException>(() => Parse(bytes, 4));
    }

    [Test]
    public void RejectsDuplicateCycles()
    {
        byte[] strike = BuildStrike(
            16,
            72,
            new byte[][] {
                null!,
                BuildGlyph(0, 0, "dupe", new byte[] { 0, 2 }),
                BuildGlyph(0, 0, "dupe", new byte[] { 0, 1 }),
                null!
            });

        FontFormatException? exception = Assert.Throws<FontFormatException>(() =>
            Parse(BuildSbix(1, new byte[][] { strike }), 4));

        Assert.That(exception!.Message, Does.Contain("cycle"));
    }

    [Test]
    public void EnforcesCoverageAndDuplicateDepthLimits()
    {
        byte[] simpleStrike = BuildStrike(
            16,
            72,
            new byte[][] {
                BuildGlyph(0, 0, "png ", new byte[] { 1 }),
                null!,
                null!,
                null!
            });
        byte[] chainedStrike = BuildStrike(
            16,
            72,
            new byte[][] {
                BuildGlyph(0, 0, "png ", new byte[] { 1 }),
                BuildGlyph(0, 0, "dupe", new byte[] { 0, 0 }),
                BuildGlyph(0, 0, "dupe", new byte[] { 0, 1 }),
                BuildGlyph(0, 0, "dupe", new byte[] { 0, 2 })
            });

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => Parse(
                BuildSbix(1, new byte[][] { simpleStrike }),
                4,
                CreateLimits(5, 10)));
            Assert.Throws<FontLimitException>(() => Parse(
                BuildSbix(1, new byte[][] { chainedStrike }),
                4,
                CreateLimits(100, 2)));
        });
    }

    [Test]
    public void ValidatesAccessorIndexesAndDuplicateOnlyMethods()
    {
        byte[] strikeBytes = BuildStrike(
            16,
            72,
            new byte[][] {
                BuildGlyph(0, 0, "png ", new byte[] { 1 }),
                null!
            });
        SfntSbixTable? table = Parse(
            BuildSbix(1, new byte[][] { strikeBytes }),
            2);
        SfntSbixStrike strike = table!.GetStrike(0);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetStrike(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => strike.GetGlyphData(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => strike.ResolveGlyphImage(-1));
            Assert.Throws<InvalidOperationException>(() =>
                strike.GetGlyphData(0).GetDuplicateGlyphId());
        });
    }

    [Test]
    public void KeepsUnknownVersionsAsRawTableData()
    {
        byte[] bytes = BuildSbix(1, new byte[0][]);
        bytes[1] = 2;
        SfntParsedTableView? view = SfntSbixTableParser.FindView(
            CreateCache(bytes, 2, FontLimits.CreateCompatible()));

        Assert.Multiple(() =>
        {
            Assert.That(view!.HasParsedData(), Is.False);
            Assert.That(view.GetVersionSupport(), Is.EqualTo(SfntTableVersionSupport.RawOnly));
        });
    }

    static IEnumerable<TestCaseData> InvalidSbixTables()
    {
        byte[] direct = BuildGlyph(0, 0, "png ", new byte[] { 1, 2 });

        byte[] truncatedHeader = new byte[7];
        truncatedHeader[1] = 1;
        yield return InvalidCase(truncatedHeader, "TruncatedHeader");
        yield return InvalidCase(BuildSbix(0, new byte[0][]), "MissingRequiredFlag");
        yield return InvalidCase(BuildSbix(5, new byte[0][]), "ReservedFlag");

        byte[] headerStrikeOffset = BuildSbix(
            1,
            new byte[][] { BuildStrike(16, 72, new byte[][] { direct, null!, null!, null! }) });
        PatchUInt32(headerStrikeOffset, 8, 8);
        yield return InvalidCase(headerStrikeOffset, "HeaderStrikeOffset");

        byte[] outsideStrikeOffset = BuildSbix(
            1,
            new byte[][] { BuildStrike(16, 72, new byte[][] { direct, null!, null!, null! }) });
        PatchUInt32(outsideStrikeOffset, 8, uint.MaxValue);
        yield return InvalidCase(outsideStrikeOffset, "OutsideStrikeOffset");

        byte[] descendingGlyphOffsets = BuildSbix(
            1,
            new byte[][] { BuildStrike(16, 72, new byte[][] { direct, null!, null!, null! }) });
        PatchUInt32(descendingGlyphOffsets, 20, 23);
        yield return InvalidCase(descendingGlyphOffsets, "DescendingGlyphOffsets");

        byte[] shortRecord = BuildSbix(
            1,
            new byte[][] {
                BuildStrike(16, 72, new byte[][] { new byte[7], null!, null!, null! })
            });
        yield return InvalidCase(shortRecord, "ShortGlyphRecord");

        byte[] emptyPayload = BuildSbix(
            1,
            new byte[][] {
                BuildStrike(16, 72, new byte[][] {
                    BuildGlyph(0, 0, "png ", new byte[0]), null!, null!, null!
                })
            });
        yield return InvalidCase(emptyPayload, "EmptyImagePayload");

        byte[] shortDuplicate = BuildSbix(
            1,
            new byte[][] {
                BuildStrike(16, 72, new byte[][] {
                    BuildGlyph(0, 0, "dupe", new byte[] { 0 }), null!, null!, null!
                })
            });
        yield return InvalidCase(shortDuplicate, "ShortDuplicate");

        byte[] missingDuplicateGlyph = BuildSbix(
            1,
            new byte[][] {
                BuildStrike(16, 72, new byte[][] {
                    BuildGlyph(0, 0, "dupe", new byte[] { 0, 4 }), null!, null!, null!
                })
            });
        yield return InvalidCase(missingDuplicateGlyph, "MissingDuplicateGlyph");

        byte[] emptyDuplicateTarget = BuildSbix(
            1,
            new byte[][] {
                BuildStrike(16, 72, new byte[][] {
                    BuildGlyph(0, 0, "dupe", new byte[] { 0, 1 }), null!, null!, null!
                })
            });
        yield return InvalidCase(emptyDuplicateTarget, "EmptyDuplicateTarget");
    }

    static TestCaseData InvalidCase(byte[] bytes, string name)
    {
        return new TestCaseData(bytes).SetName("Rejects" + name);
    }

    static byte[] BuildGlyph(
        int originX,
        int originY,
        string graphicType,
        byte[] data)
    {
        return new TestBinaryBuilder()
            .WriteInt16((short)originX)
            .WriteInt16((short)originY)
            .WriteTag(graphicType)
            .WriteBytes(data)
            .CopyBytes();
    }

    static byte[] BuildStrike(
        int pixelsPerEm,
        int pixelsPerInch,
        byte[][]? glyphRecords)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)pixelsPerEm)
            .WriteUInt16((ushort)pixelsPerInch);
        int offsetPosition = builder.GetLength();
        builder.WriteZeros((glyphRecords!.Length + 1) * 4);
        int glyphId = 0;

        while (glyphId < glyphRecords.Length)
        {
            builder.PatchUInt32(
                offsetPosition + glyphId * 4,
                (uint)builder.GetLength());

            if (glyphRecords[glyphId] != null)
            {
                builder.WriteBytes(glyphRecords[glyphId]);
            }

            glyphId += 1;
        }

        builder.PatchUInt32(
            offsetPosition + glyphRecords.Length * 4,
            (uint)builder.GetLength());
        return builder.CopyBytes();
    }

    static byte[] BuildSbix(int flags, byte[][] strikes)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(1)
            .WriteUInt16((ushort)flags)
            .WriteUInt32((uint)strikes.Length);
        int offsetsPosition = builder.GetLength();
        builder.WriteZeros(strikes.Length * 4);
        int index = 0;

        while (index < strikes.Length)
        {
            builder.PatchUInt32(
                offsetsPosition + index * 4,
                (uint)builder.GetLength());
            builder.WriteBytes(strikes[index]);
            index += 1;
        }

        return builder.CopyBytes();
    }

    static SfntSbixTable? Parse(byte[] bytes, int glyphCount)
    {
        return Parse(bytes, glyphCount, FontLimits.CreateCompatible());
    }

    static SfntSbixTable? Parse(
        byte[] bytes,
        int glyphCount,
        FontLimits limits)
    {
        return SfntSbixTableParser.FindTable(CreateCache(bytes, glyphCount, limits));
    }

    static SfntTableCache CreateCache(
        byte[] sbix,
        int glyphCount,
        FontLimits limits)
    {
        return SfntTableCache.Create(
            new SfntTableStore(
                new SfntTableData[] {
                    CreateTable(FontTags.Sbix, sbix),
                    CreateTable(FontTags.Maxp, BuildMaxp(glyphCount))
                },
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            limits);
    }

    static FontFace CreateFace(byte[] sbix, int glyphCount)
    {
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.BitmapOnly,
                Array.Empty<FontDiagnostic>()),
            CreateCache(sbix, glyphCount, FontLimits.CreateCompatible()));
    }

    static SfntTableData CreateTable(int tag, byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static void PatchUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    static FontLimits CreateLimits(
        int maximumCoverageEntries,
        int maximumComponentDepth)
    {
        return FontLimits.Create(
            10000,
            10000,
            10,
            100,
            100,
            100,
            100,
            maximumComponentDepth,
            10,
            10,
            100,
            1000,
            maximumCoverageEntries,
            1000,
            1000,
            10,
            100,
            20,
            100);
    }
}
