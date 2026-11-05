using System.Buffers.Binary;
using System.Text;
using ttf;

namespace ttf.Tests;

internal class Woff2ParserTests
{
    static readonly string[] KnownTags = new string[]
    {
        "cmap", "head", "hhea", "hmtx", "maxp", "name", "OS/2", "post",
        "cvt ", "fpgm", "glyf", "loca", "prep", "CFF ", "VORG", "EBDT",
        "EBLC", "gasp", "hdmx", "kern", "LTSH", "PCLT", "VDMX", "vhea",
        "vmtx", "BASE", "GDEF", "GPOS", "GSUB", "EBSC", "JSTF", "MATH",
        "CBDT", "CBLC", "COLR", "CPAL", "SVG ", "sbix", "acnt", "avar",
        "bdat", "bloc", "bsln", "cvar", "fdsc", "feat", "fmtx", "fvar",
        "gvar", "hsty", "just", "lcar", "mort", "morx", "opbd", "prop",
        "trak", "Zapf", "Silf", "Glat", "Gloc", "Feat", "Sill"
    };

    [Test]
    public void ParsesKnownAndCustomNullTransformTablesInStreamOrder()
    {
        byte[] cmap = new byte[] { 1, 2, 3 };
        byte[] custom = new byte[] { 9, 8 };
        byte[] head = new byte[] { 4, 5, 6, 7 };
        byte[] woff = new Woff2Builder()
            .AddKnownTable(0, cmap)
            .AddCustomTable(TestSfntEncoding.EncodeTag("ZZZZ"), custom)
            .AddKnownTable(1, head)
            .Build();

        Woff2Font font = Parse(woff, FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(font.GetFlavor(), Is.EqualTo(0x00010000U));
            Assert.That(font.GetDeclaredLength(), Is.EqualTo(woff.Length));
            Assert.That(font.GetMajorVersion(), Is.EqualTo(1));
            Assert.That(font.GetMinorVersion(), Is.Zero);
            Assert.That(font.GetTableCount(), Is.EqualTo(3));
            Assert.That(font.GetTable(0).GetTag(), Is.EqualTo(FontTags.Cmap));
            Assert.That(font.GetTable(0).GetTagIndex(), Is.Zero);
            Assert.That(font.GetTable(0).GetDataOffset(), Is.Zero);
            Assert.That(font.GetTable(0).CopyData(), Is.EqualTo(cmap));
            Assert.That(font.GetTable(1).UsesCustomTag(), Is.True);
            Assert.That(font.GetTable(1).GetTagIndex(), Is.EqualTo(63));
            Assert.That(font.GetTable(1).GetTag(), Is.EqualTo((int)TestSfntEncoding.EncodeTag("ZZZZ")));
            Assert.That(font.GetTable(1).GetDataOffset(), Is.EqualTo(cmap.Length));
            Assert.That(font.GetTable(1).CopyData(), Is.EqualTo(custom));
            Assert.That(font.GetTable(2).CopyData(), Is.EqualTo(head));
            Assert.That(font.GetDecompressedDataLength(), Is.EqualTo(cmap.Length + custom.Length + head.Length));
            Assert.That(font.CopyDecompressedData(), Is.EqualTo(cmap.Concat(custom).Concat(head)));
            Assert.That(font.GetCollection().IsPresent(), Is.False);
        });
    }

    [Test]
    public void ProvidesTheCompleteKnownTagTable()
    {
        Assert.That(Woff2KnownTags.GetCount(), Is.EqualTo(KnownTags.Length));

        for (int index = 0; index < KnownTags.Length; index += 1)
        {
            Assert.That(
                Woff2KnownTags.GetTag(index),
                Is.EqualTo((int)TestSfntEncoding.EncodeTag(KnownTags[index])),
                $"known tag index {index}");
        }

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Woff2KnownTags.GetTag(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Woff2KnownTags.GetTag(63));
        });
    }

    [Test]
    public void ParsesEveryKnownTagEncoding()
    {
        Woff2Builder builder = new();

        for (int index = 0; index < KnownTags.Length; index += 1)
        {
            builder.AddKnownTable(index, Array.Empty<byte>());
        }

        Woff2Font font = Parse(builder.Build(), FontLoadOptions.CreateStrict());

        Assert.That(font.GetTableCount(), Is.EqualTo(KnownTags.Length));

        for (int index = 0; index < KnownTags.Length; index += 1)
        {
            Assert.Multiple(() =>
            {
                Assert.That(font.GetTable(index).GetTagIndex(), Is.EqualTo(index));
                Assert.That(font.GetTable(index).GetTag(), Is.EqualTo(Woff2KnownTags.GetTag(index)));
                Assert.That(font.GetTable(index).UsesCustomTag(), Is.False);
            });
        }
    }

    [Test]
    public void PreservesTransformedTablePayloadsForLaterReconstruction()
    {
        byte[] transformedGlyf = new byte[] { 1, 2, 3, 4 };
        byte[] transformedHmtx = new byte[] { 5, 6 };
        byte[] woff = new Woff2Builder()
            .AddTransformedKnownTable(10, 0, 100, transformedGlyf)
            .AddTransformedKnownTable(11, 0, 22, Array.Empty<byte>())
            .AddTransformedKnownTable(3, 1, 40, transformedHmtx)
            .AddKnownTable(0, new byte[] { 7 })
            .Build();

        Woff2Font font = Parse(woff, FontLoadOptions.CreateStrict());
        Woff2Table glyf = font.GetTable(0);
        Woff2Table loca = font.GetTable(1);
        Woff2Table hmtx = font.GetTable(2);

        Assert.Multiple(() =>
        {
            Assert.That(glyf.IsTransformed(), Is.True);
            Assert.That(glyf.GetTransformVersion(), Is.Zero);
            Assert.That(glyf.GetOriginalLength(), Is.EqualTo(100));
            Assert.That(glyf.GetTransformLength(), Is.EqualTo(transformedGlyf.Length));
            Assert.That(glyf.CopyData(), Is.EqualTo(transformedGlyf));
            Assert.That(loca.IsTransformed(), Is.True);
            Assert.That(loca.GetTransformLength(), Is.Zero);
            Assert.That(loca.GetDataLength(), Is.Zero);
            Assert.That(hmtx.GetTransformVersion(), Is.EqualTo(1));
            Assert.That(hmtx.GetTransformLength(), Is.EqualTo(transformedHmtx.Length));
            Assert.That(hmtx.CopyData(), Is.EqualTo(transformedHmtx));
            Assert.That(font.GetTable(3).HasTransformLength(), Is.False);
            Assert.Throws<InvalidOperationException>(() => font.GetTable(3).GetTransformLength());
        });
    }

    [Test]
    public void DoesNotAllocateFromTransformedOriginalLengthHints()
    {
        byte[] woff = new Woff2Builder()
            .AddTransformedKnownTable(10, 0, uint.MaxValue, new byte[] { 1 })
            .AddTransformedKnownTable(11, 0, 4, Array.Empty<byte>())
            .Build();

        Woff2Font font = Parse(
            woff,
            CreateOptions(CreateLimits(10, 100, 100, 1)));

        Assert.Multiple(() =>
        {
            Assert.That(font.GetTable(0).GetOriginalLength(), Is.EqualTo(uint.MaxValue));
            Assert.That(font.GetTable(0).GetDataLength(), Is.EqualTo(1));
            Assert.That(font.GetDecompressedDataLength(), Is.EqualTo(1));
        });
    }

    [Test]
    public void AcceptsNonzeroReservedAndInaccurateReferenceSfntSize()
    {
        byte[] woff = new Woff2Builder().AddKnownTable(0, new byte[] { 1 }).Build();
        WriteUInt16(woff, 14, 0x1234);
        WriteUInt32(woff, 16, uint.MaxValue);

        Woff2Font font = Parse(woff, FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(font.GetReservedValue(), Is.EqualTo(0x1234));
            Assert.That(font.GetTotalSfntSize(), Is.EqualTo(uint.MaxValue));
        });
    }

    [Test]
    public void DecompressesMetadataAndPreservesPrivateData()
    {
        byte[] metadata = Encoding.UTF8.GetBytes("<metadata version=\"1.0\"></metadata>");
        byte[] privateData = new byte[] { 9, 8, 7, 6, 5 };
        byte[] woff = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1, 2 })
            .AddMetadata(metadata)
            .AddPrivateData(privateData)
            .Build();

        Woff2Font font = Parse(woff, FontLoadOptions.CreateStrict());
        byte[] firstMetadata = font.GetMetadata().CopyData();
        byte[] firstPrivate = font.GetPrivateData().CopyData();
        firstMetadata[0] = 0;
        firstPrivate[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(font.GetMetadata().IsPresent(), Is.True);
            Assert.That(font.GetMetadata().GetCompressedOffset() % 4, Is.Zero);
            Assert.That(font.GetMetadata().GetOriginalLength(), Is.EqualTo(metadata.Length));
            Assert.That(font.GetMetadata().CopyData(), Is.EqualTo(metadata));
            Assert.That(font.GetPrivateData().IsPresent(), Is.True);
            Assert.That(font.GetPrivateData().GetOffset() % 4, Is.Zero);
            Assert.That(font.GetPrivateData().CopyData(), Is.EqualTo(privateData));
        });
    }

    [Test]
    public void RepresentsAbsentOptionalBlocks()
    {
        Woff2Font font = Parse(
            new Woff2Builder().AddKnownTable(0, new byte[] { 1 }).Build(),
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(font.GetMetadata().IsPresent(), Is.False);
            Assert.That(font.GetPrivateData().IsPresent(), Is.False);
            Assert.Throws<InvalidOperationException>(() => font.GetMetadata().CopyData());
            Assert.Throws<InvalidOperationException>(() => font.GetPrivateData().CopyData());
            Assert.Throws<InvalidOperationException>(() => font.GetCollection().GetVersion());
        });
    }

    [Test]
    public void ParsesCollectionFacesAndSharedTableReferences()
    {
        byte[] woff = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddKnownTable(10, new byte[] { 2 })
            .AddKnownTable(11, new byte[] { 3 })
            .AddKnownTable(5, new byte[] { 4 })
            .AddCollectionFace(0x00010000U, 0, 1, 2)
            .AddCollectionFace(0x74727565U, 0, 1, 2, 3)
            .SetCollectionVersion(0x00020000U)
            .Build();

        Woff2Font font = Parse(woff, FontLoadOptions.CreateStrict());
        Woff2Collection collection = font.GetCollection();

        Assert.Multiple(() =>
        {
            Assert.That(font.GetFlavor(), Is.EqualTo(0x74746366U));
            Assert.That(collection.IsPresent(), Is.True);
            Assert.That(collection.GetVersion(), Is.EqualTo(0x00020000U));
            Assert.That(collection.GetFaceCount(), Is.EqualTo(2));
            Assert.That(collection.GetFace(0).GetFaceIndex(), Is.Zero);
            Assert.That(collection.GetFace(0).GetFlavor(), Is.EqualTo(0x00010000U));
            Assert.That(collection.GetFace(0).GetTableCount(), Is.EqualTo(3));
            Assert.That(collection.GetFace(1).GetFlavor(), Is.EqualTo(0x74727565U));
            Assert.That(collection.GetFace(1).GetTableIndex(3), Is.EqualTo(3));
        });
    }

    [Test]
    public void RejectsInvalidHeaderFields()
    {
        byte[] signature = BuildOneTable();
        WriteUInt32(signature, 0, 0);
        byte[] length = BuildOneTable();
        WriteUInt32(length, 8, 1);
        byte[] zeroTables = BuildOneTable();
        WriteUInt16(zeroTables, 12, 0);
        byte[] metadataFields = BuildOneTable();
        WriteUInt32(metadataFields, 32, 1);
        byte[] privateFields = BuildOneTable();
        WriteUInt32(privateFields, 44, 1);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(signature, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(length, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(zeroTables, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(metadataFields, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(privateFields, FontLoadOptions.CreateStrict()));
        });
    }

    [Test]
    public void RejectsUnknownTransformVersions()
    {
        byte[] ordinary = BuildOneTable();
        ordinary[48] = 0x40;
        byte[] glyf = new Woff2Builder()
            .AddKnownTable(10, new byte[] { 1 })
            .AddKnownTable(11, new byte[] { 2 })
            .Build();
        glyf[48] = 0x4A;
        byte[] hmtx = new Woff2Builder().AddKnownTable(3, new byte[] { 1 }).Build();
        hmtx[48] = 0x83;

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(ordinary, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(glyf, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(hmtx, FontLoadOptions.CreateStrict()));
        });
    }

    [Test]
    public void RejectsInvalidScalarGlyfLocaRelationshipsAndDuplicates()
    {
        byte[] missingLoca = new Woff2Builder().AddKnownTable(10, new byte[] { 1 }).Build();
        byte[] wrongOrder = new Woff2Builder()
            .AddKnownTable(11, new byte[] { 1 })
            .AddKnownTable(10, new byte[] { 2 })
            .Build();
        byte[] mismatchedTransforms = new Woff2Builder()
            .AddTransformedKnownTable(10, 0, 10, new byte[] { 1 })
            .AddKnownTable(11, Array.Empty<byte>())
            .Build();
        byte[] duplicate = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddCustomTable(TestSfntEncoding.EncodeTag("cmap"), new byte[] { 2 })
            .Build();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(missingLoca, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(wrongOrder, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(mismatchedTransforms, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(duplicate, FontLoadOptions.CreateStrict()));
        });
    }

    [Test]
    public void RejectsNonzeroTransformedLocaLength()
    {
        byte[] woff = new Woff2Builder()
            .AddTransformedKnownTable(10, 0, 10, new byte[] { 1 })
            .AddTransformedKnownTable(11, 0, 4, new byte[] { 2 })
            .Build();

        Assert.Throws<FontFormatException>(() => Parse(woff, FontLoadOptions.CreateStrict()));
    }

    [Test]
    public void RejectsBrotliOutputWhoseSizeDoesNotMatchTheDirectory()
    {
        byte[] woff = new Woff2Builder().AddKnownTable(0, new byte[] { 1, 2, 3, 4 }).Build();
        woff[49] = 5;

        Assert.Throws<FontFormatException>(() => Parse(woff, FontLoadOptions.CreateStrict()));
    }

    [Test]
    public void RejectsInvalidBrotliData()
    {
        byte[] woff = BuildOneTable();
        Woff2Font parsed = Parse(woff, FontLoadOptions.CreateStrict());
        woff[parsed.GetCompressedDataOffset()] = 0xFF;

        Assert.Throws<InvalidOperationException>(() => Parse(woff, FontLoadOptions.CreateStrict()));
    }

    [Test]
    public void EnforcesBrotliRatioAndCumulativeDecompressionLimits()
    {
        byte[] compressed = new Woff2Builder()
            .AddKnownTable(0, new byte[4096])
            .Build();
        byte[] metadata = new Woff2Builder()
            .AddKnownTable(0, new byte[64])
            .AddMetadata(new byte[64])
            .Build();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => Parse(
                compressed,
                CreateOptions(CreateLimits(10, 1_000_000, 1, 10))));
            Assert.Throws<FontLimitException>(() => Parse(
                metadata,
                CreateOptions(CreateLimits(10, 120, 100, 10))));
        });
    }

    [Test]
    public void RejectsInvalidOptionalBlockPlacementPaddingAndTrailingData()
    {
        byte[] badMetadataOffset = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddMetadata(new byte[] { 2 })
            .Build();
        WriteUInt32(badMetadataOffset, 28, ReadUInt32(badMetadataOffset, 28) + 1);
        byte[] badPrivateOffset = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddPrivateData(new byte[] { 2 })
            .Build();
        WriteUInt32(badPrivateOffset, 40, ReadUInt32(badPrivateOffset, 40) + 1);
        byte[] padding = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddMetadata(new byte[] { 2 })
            .Build();
        int compressedEnd = GetCompressedEnd(padding);
        int metadataOffset = (int)ReadUInt32(padding, 28);

        if (compressedEnd < metadataOffset)
        {
            padding[compressedEnd] = 1;
        }

        byte[] valid = BuildOneTable();
        byte[] trailing = new byte[valid.Length + 1];
        valid.CopyTo(trailing, 0);
        WriteUInt32(trailing, 8, (uint)trailing.Length);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(badMetadataOffset, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(badPrivateOffset, FontLoadOptions.CreateStrict()));

            if (compressedEnd < metadataOffset)
            {
                Assert.Throws<FontFormatException>(() => Parse(padding, FontLoadOptions.CreateStrict()));
            }

            Assert.Throws<FontFormatException>(() => Parse(trailing, FontLoadOptions.CreateStrict()));
        });
    }

    [Test]
    public void RejectsMalformedCollectionDirectories()
    {
        byte[] badVersion = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddCollectionFace(0x00010000U, 0)
            .SetCollectionVersion(3)
            .Build();
        byte[] emptyFace = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddCollectionFace(0x00010000U)
            .Build();
        byte[] zeroFaces = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddCollectionFace(0x00010000U, 0)
            .Build();
        zeroFaces[54] = 0;
        byte[] badIndex = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddCollectionFace(0x00010000U, 1)
            .Build();
        byte[] duplicateReference = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddCollectionFace(0x00010000U, 0, 0)
            .Build();
        byte[] unreferenced = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddKnownTable(5, new byte[] { 2 })
            .AddCollectionFace(0x00010000U, 0)
            .Build();
        byte[] unpaired = new Woff2Builder()
            .AddKnownTable(10, new byte[] { 1 })
            .AddKnownTable(11, new byte[] { 2 })
            .AddCollectionFace(0x00010000U, 0)
            .Build();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Parse(badVersion, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(emptyFace, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(zeroFaces, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(badIndex, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(duplicateReference, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(unreferenced, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Parse(unpaired, FontLoadOptions.CreateStrict()));
        });
    }

    [Test]
    public void EnforcesConfiguredTableFaceAndInputLimits()
    {
        byte[] scalar = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddKnownTable(5, new byte[] { 2 })
            .Build();
        byte[] collection = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddCollectionFace(0x00010000U, 0)
            .AddCollectionFace(0x00010000U, 0)
            .Build();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => Parse(
                scalar,
                CreateOptions(CreateLimits(1, 1000, 100, 10))));
            Assert.Throws<FontLimitException>(() => Parse(
                collection,
                CreateOptions(CreateLimits(10, 1000, 100, 1))));
            Assert.Throws<FontLimitException>(() => Parse(
                scalar,
                CreateOptions(CreateLimits(10, 1000, 100, 10, scalar.Length - 1))));
        });
    }

    [Test]
    public void AllowsCollectionWideUniqueTablesBeyondThePerFaceLimit()
    {
        byte[] collection = new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddKnownTable(5, new byte[] { 2 })
            .AddCollectionFace(0x00010000U, 0)
            .AddCollectionFace(0x00010000U, 1)
            .Build();

        Woff2Font font = Parse(
            collection,
            CreateOptions(CreateLimits(1, 1000, 100, 2)));

        Assert.Multiple(() =>
        {
            Assert.That(font.GetTableCount(), Is.EqualTo(2));
            Assert.That(font.GetCollection().GetFaceCount(), Is.EqualTo(2));
            Assert.That(font.GetCollection().GetFace(0).GetTableCount(), Is.EqualTo(1));
            Assert.That(font.GetCollection().GetFace(1).GetTableCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ObservesCancellationAndValidatesArgumentsAndIndexes()
    {
        using CancellationTokenSource source = new();
        source.Cancel();
        byte[] woff = BuildOneTable();
        Woff2Font font = Parse(woff, FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() => Parse(
                woff,
                FontLoadOptions.CreateStrict(source.Token)));
            Assert.Throws<ArgumentNullException>(() => Woff2Parser.Parse(null!, FontLoadOptions.CreateStrict()));
            Assert.Throws<ArgumentNullException>(() => Woff2Parser.Parse(FontData.Create(woff), null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => font.GetTable(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => font.GetTable(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => font.GetCollection().GetFace(0));
            Assert.That(font.FindTable(FontTags.Name), Is.Null);
        });
    }

    [Test]
    public void ReturnedDataCopiesAreIndependent()
    {
        Woff2Font font = Parse(BuildOneTable(), FontLoadOptions.CreateStrict());
        byte[] firstTable = font.GetTable(0).CopyData();
        byte[] secondTable = font.GetTable(0).CopyData();
        byte[] firstBlock = font.CopyDecompressedData();
        byte[] secondBlock = font.CopyDecompressedData();
        firstTable[0] = 9;
        firstBlock[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(secondTable, Is.EqualTo(new byte[] { 1 }));
            Assert.That(secondBlock, Is.EqualTo(new byte[] { 1 }));
            Assert.That(firstTable, Is.Not.SameAs(secondTable));
            Assert.That(firstBlock, Is.Not.SameAs(secondBlock));
        });
    }

    static byte[] BuildOneTable()
    {
        return new Woff2Builder().AddKnownTable(0, new byte[] { 1 }).Build();
    }

    static Woff2Font Parse(byte[] data, FontLoadOptions options)
    {
        return Woff2Parser.Parse(FontData.Create(data), options);
    }

    static int GetCompressedEnd(byte[] data)
    {
        Woff2Font parsed = Parse(data, FontLoadOptions.CreateStrict());
        return parsed.GetCompressedDataOffset() + parsed.GetCompressedDataLength();
    }

    static FontLoadOptions CreateOptions(FontLimits limits)
    {
        return FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch,
            limits,
            CancellationToken.None);
    }

    static FontLimits CreateLimits(
        int maxTables,
        int maxDecompressedBytes,
        int maxRatio,
        int maxFaces,
        int maxInputBytes = 1_000_000)
    {
        return FontLimits.Create(
            maxInputBytes,
            1_000_000,
            maxFaces,
            maxTables,
            100,
            100,
            100,
            10,
            10,
            10,
            100,
            100,
            100,
            100,
            maxDecompressedBytes,
            maxRatio,
            100,
            10,
            100);
    }

    static uint ReadUInt32(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
    }

    static void WriteUInt16(byte[] data, int offset, ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(offset, 2), value);
    }

    static void WriteUInt32(byte[] data, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset, 4), value);
    }
}
