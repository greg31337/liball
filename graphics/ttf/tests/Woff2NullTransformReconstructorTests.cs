using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

internal class Woff2NullTransformReconstructorTests
{
    [Test]
    public void ReconstructsCanonicalScalarSfntWithChecksumsAndPhysicalOrder()
    {
        byte[] name = new byte[] { 9, 8, 7 };
        byte[] head = new byte[12];
        WriteUInt32(head, 8, 0x12345678U);
        byte[] cmap = new byte[] { 1, 2, 3, 4, 5 };
        Woff2Font font = Parse(new Woff2Builder()
            .AddKnownTable(5, name)
            .AddKnownTable(1, head)
            .AddKnownTable(0, cmap)
            .Build());

        Woff2Reconstruction reconstruction = Reconstruct(font, FontLoadOptions.CreateStrict());
        byte[] output = reconstruction.CopyData();
        SfntDirectory directory = SfntDirectoryParser.Parse(
            FontData.Create(output).GetSlice(0, output.Length));
        SfntTableRecord cmapRecord = directory.GetTableRecord(0);
        SfntTableRecord headRecord = directory.GetTableRecord(1);
        SfntTableRecord nameRecord = directory.GetTableRecord(2);

        Assert.Multiple(() =>
        {
            Assert.That(reconstruction.IsCollection(), Is.False);
            Assert.That(reconstruction.GetFaceCount(), Is.EqualTo(1));
            Assert.That(reconstruction.GetFaceOffset(0), Is.Zero);
            Assert.That(reconstruction.GetLength(), Is.EqualTo(output.Length));
            Assert.That(directory.GetScalerType(), Is.EqualTo(0x00010000U));
            Assert.That(directory.GetTableCount(), Is.EqualTo(3));
            Assert.That(directory.GetStoredSearchRange(), Is.EqualTo(32));
            Assert.That(directory.GetStoredEntrySelector(), Is.EqualTo(1));
            Assert.That(directory.GetStoredRangeShift(), Is.EqualTo(16));
            Assert.That(cmapRecord.GetTag(), Is.EqualTo(FontTags.Cmap));
            Assert.That(headRecord.GetTag(), Is.EqualTo(FontTags.Head));
            Assert.That(nameRecord.GetTag(), Is.EqualTo(FontTags.Name));
            Assert.That(nameRecord.GetOffset(), Is.LessThan(headRecord.GetOffset()));
            Assert.That(headRecord.GetOffset(), Is.LessThan(cmapRecord.GetOffset()));
            Assert.That(CopyTable(output, nameRecord), Is.EqualTo(name));
            Assert.That(CopyTable(output, cmapRecord), Is.EqualTo(cmap));
            Assert.That(headRecord.GetChecksum(), Is.EqualTo(FontChecksum.CalculateHeadTable(
                CopyTable(output, headRecord))));
            Assert.That(nameRecord.GetChecksum(), Is.EqualTo(FontChecksum.CalculateTable(name)));
            Assert.That(cmapRecord.GetChecksum(), Is.EqualTo(FontChecksum.CalculateTable(cmap)));
            Assert.That(FontChecksum.VerifyFont(output), Is.True);
            Assert.That(ReadUInt32(output, (int)headRecord.GetOffset() + 8), Is.Not.Zero);
            Assert.That(ReadUInt32(font.GetTable(1).CopyData(), 8), Is.EqualTo(0x12345678U));
        });
    }

    [Test]
    public void SortsDirectoryTagsUsingUnsignedValues()
    {
        Woff2Font font = Parse(new Woff2Builder()
            .AddCustomTable(uint.MaxValue, new byte[] { 1 })
            .AddKnownTable(0, new byte[] { 2 })
            .Build());

        Woff2Reconstruction reconstruction = Reconstruct(font, FontLoadOptions.CreateStrict());
        SfntDirectory directory = SfntDirectoryParser.Parse(
            reconstruction.GetData().GetSlice(0, reconstruction.GetLength()));

        Assert.Multiple(() =>
        {
            Assert.That(directory.GetTableRecord(0).GetTag(), Is.EqualTo(FontTags.Cmap));
            Assert.That((uint)directory.GetTableRecord(1).GetTag(), Is.EqualTo(uint.MaxValue));
        });
    }

    [Test]
    public void PreservesNullTransformGlyfAndLocaBytes()
    {
        byte[] glyf = new byte[] { 1, 2, 3, 4, 5 };
        byte[] loca = new byte[] { 0, 0, 0, 0 };
        Woff2Font font = Parse(new Woff2Builder()
            .AddKnownTable(10, glyf)
            .AddKnownTable(11, loca)
            .Build());

        Woff2Reconstruction reconstruction = Reconstruct(font, FontLoadOptions.CreateStrict());
        byte[] output = reconstruction.CopyData();
        SfntDirectory directory = SfntDirectoryParser.Parse(
            FontData.Create(output).GetSlice(0, output.Length));

        Assert.Multiple(() =>
        {
            Assert.That(font.GetTable(0).GetTransformVersion(), Is.EqualTo(3));
            Assert.That(font.GetTable(1).GetTransformVersion(), Is.EqualTo(3));
            Assert.That(CopyTable(output, directory.GetTableRecord(0)), Is.EqualTo(glyf));
            Assert.That(CopyTable(output, directory.GetTableRecord(1)), Is.EqualTo(loca));
        });
    }

    [Test]
    public void ReconstructsVersionTwoCollectionWithSharedTablesAndNullDsig()
    {
        byte[] firstName = new byte[] { 1, 2, 3 };
        byte[] head = new byte[12];
        WriteUInt32(head, 8, uint.MaxValue);
        byte[] secondName = new byte[] { 4, 5, 6, 7 };
        Woff2Font font = Parse(new Woff2Builder()
            .AddKnownTable(5, firstName)
            .AddKnownTable(1, head)
            .AddKnownTable(5, secondName)
            .AddCollectionFace(0x00010000U, 0, 1)
            .AddCollectionFace(0x4F54544FU, 2, 1)
            .SetCollectionVersion(0x00020000U)
            .Build());

        Woff2Reconstruction reconstruction = Reconstruct(font, FontLoadOptions.CreateStrict());
        byte[] output = reconstruction.CopyData();
        SfntCollection collection = SfntCollectionParser.Parse(
            FontData.Create(output),
            FontLoadOptions.CreateStrict());
        SfntDirectory firstDirectory = collection.GetFace(0).GetDirectory();
        SfntDirectory secondDirectory = collection.GetFace(1).GetDirectory();
        SfntTableRecord firstHead = firstDirectory.GetTableRecord(0);
        SfntTableRecord firstNameRecord = firstDirectory.GetTableRecord(1);
        SfntTableRecord secondHead = secondDirectory.GetTableRecord(0);
        SfntTableRecord secondNameRecord = secondDirectory.GetTableRecord(1);

        Assert.Multiple(() =>
        {
            Assert.That(reconstruction.IsCollection(), Is.True);
            Assert.That(reconstruction.GetFaceCount(), Is.EqualTo(2));
            Assert.That(reconstruction.GetFaceOffset(0), Is.EqualTo(32));
            Assert.That(reconstruction.GetFaceOffset(1), Is.EqualTo(76));
            Assert.That(collection.GetVersion(), Is.EqualTo(0x00020000U));
            Assert.That(collection.GetFaceCount(), Is.EqualTo(2));
            Assert.That(collection.GetDsig().IsPresent(), Is.False);
            Assert.That(firstDirectory.GetScalerType(), Is.EqualTo(0x00010000U));
            Assert.That(secondDirectory.GetScalerType(), Is.EqualTo(0x4F54544FU));
            Assert.That(firstHead.GetTag(), Is.EqualTo(FontTags.Head));
            Assert.That(firstNameRecord.GetTag(), Is.EqualTo(FontTags.Name));
            Assert.That(secondHead.GetTag(), Is.EqualTo(FontTags.Head));
            Assert.That(secondNameRecord.GetTag(), Is.EqualTo(FontTags.Name));
            Assert.That(firstHead.GetOffset(), Is.EqualTo(secondHead.GetOffset()));
            Assert.That(collection.GetSharedTableRegionCount(), Is.EqualTo(1));
            Assert.That(CopyTable(output, firstNameRecord), Is.EqualTo(firstName));
            Assert.That(CopyTable(output, secondNameRecord), Is.EqualTo(secondName));
            Assert.That(ReadUInt32(output, (int)firstHead.GetOffset() + 8), Is.Zero);
            Assert.That(firstHead.GetChecksum(), Is.EqualTo(FontChecksum.CalculateTable(
                CopyTable(output, firstHead))));
            Assert.That(firstNameRecord.GetChecksum(), Is.EqualTo(FontChecksum.CalculateTable(firstName)));
            Assert.That(secondNameRecord.GetChecksum(), Is.EqualTo(FontChecksum.CalculateTable(secondName)));
        });
    }

    [Test]
    public void ReconstructsVersionOneCollectionHeaderWithoutDsigFields()
    {
        Woff2Font font = Parse(new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .AddCollectionFace(0x00010000U, 0)
            .SetCollectionVersion(0x00010000U)
            .Build());

        Woff2Reconstruction reconstruction = Reconstruct(font, FontLoadOptions.CreateStrict());
        SfntCollection collection = SfntCollectionParser.Parse(
            reconstruction.GetData(),
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(reconstruction.GetFaceOffset(0), Is.EqualTo(16));
            Assert.That(collection.GetVersion(), Is.EqualTo(0x00010000U));
            Assert.That(collection.GetDsig().IsPresent(), Is.False);
        });
    }

    [Test]
    public void RejectsTransformedTables()
    {
        Woff2Font font = Parse(new Woff2Builder()
            .AddTransformedKnownTable(10, 0, 20, new byte[] { 1, 2 })
            .AddTransformedKnownTable(11, 0, 6, Array.Empty<byte>())
            .Build());

        Assert.Throws<InvalidOperationException>(() =>
            Reconstruct(font, FontLoadOptions.CreateStrict()));
    }

    [Test]
    public void RejectsTruncatedHeadTableDuringChecksumGeneration()
    {
        Woff2Font scalar = Parse(new Woff2Builder()
            .AddKnownTable(1, new byte[11])
            .Build());
        Woff2Font collection = Parse(new Woff2Builder()
            .AddKnownTable(1, new byte[11])
            .AddCollectionFace(0x00010000U, 0)
            .Build());

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                Reconstruct(scalar, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() =>
                Reconstruct(collection, FontLoadOptions.CreateStrict()));
        });
    }

    [Test]
    public void EnforcesOutputLimitBeforeAllocatingResult()
    {
        Woff2Font font = Parse(new Woff2Builder()
            .AddKnownTable(0, new byte[64])
            .Build());

        Assert.Throws<FontLimitException>(() =>
            Reconstruct(font, CreateOptions(64, CancellationToken.None)));
    }

    [Test]
    public void RejectsSfntFaceCountThatCannotEncodeSearchFields()
    {
        Woff2Builder builder = new();

        for (uint tag = 0; tag < 4096; tag += 1)
        {
            builder.AddCustomTable(tag, Array.Empty<byte>());
        }

        Woff2Font font = Woff2Parser.Parse(
            FontData.Create(builder.Build()),
            FontLoadOptions.CreateInspectOnly());

        Assert.Throws<FontFormatException>(() =>
            Reconstruct(font, FontLoadOptions.CreateInspectOnly()));
    }

    [Test]
    public void ObservesCancellationAndValidatesArgumentsAndIndexes()
    {
        Woff2Font font = Parse(new Woff2Builder()
            .AddKnownTable(0, new byte[] { 1 })
            .Build());
        using CancellationTokenSource source = new();
        source.Cancel();
        Woff2Reconstruction reconstruction = Reconstruct(
            font,
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                Reconstruct(font, FontLoadOptions.CreateStrict(source.Token)));
            Assert.Throws<ArgumentNullException>(() =>
                Woff2NullTransformReconstructor.Reconstruct(null!, FontLoadOptions.CreateStrict()));
            Assert.Throws<ArgumentNullException>(() =>
                Woff2NullTransformReconstructor.Reconstruct(font, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => reconstruction.GetFaceOffset(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => reconstruction.GetFaceOffset(1));
        });
    }

    [Test]
    public void ReturnedOutputCopiesAreIndependent()
    {
        Woff2Reconstruction reconstruction = Reconstruct(
            Parse(new Woff2Builder().AddKnownTable(0, new byte[] { 1 }).Build()),
            FontLoadOptions.CreateStrict());
        byte[] first = reconstruction.CopyData();
        byte[] second = reconstruction.CopyData();
        first[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(second[0], Is.Zero);
        });
    }

    static Woff2Font Parse(byte[] woff)
    {
        return Woff2Parser.Parse(
            FontData.Create(woff),
            FontLoadOptions.CreateStrict());
    }

    static Woff2Reconstruction Reconstruct(Woff2Font font, FontLoadOptions options)
    {
        return Woff2NullTransformReconstructor.Reconstruct(font, options);
    }

    static byte[] CopyTable(byte[] font, SfntTableRecord record)
    {
        return font.AsSpan((int)record.GetOffset(), (int)record.GetLength()).ToArray();
    }

    static FontLoadOptions CreateOptions(
        int maxOutputBytes,
        CancellationToken cancellationToken)
    {
        FontLimits limits = FontLimits.Create(
            1_000_000,
            maxOutputBytes,
            10,
            100,
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
            1_000_000,
            100,
            100,
            10,
            100);
        return FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch,
            limits,
            cancellationToken);
    }

    static uint ReadUInt32(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
    }

    static void WriteUInt32(byte[] data, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset, 4), value);
    }
}
