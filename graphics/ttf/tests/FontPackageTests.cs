using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

internal class FontPackageTests
{
    [Test]
    public void ExposesScalarPackageFaceTablesAndIndependentByteCopies()
    {
        byte[] source = BuildValidSfnt();
        FontPackage package = FontLoader.Load(source, CompatibleOptions());

        Assert.That(package.GetCreatedFaceCount(), Is.Zero);
        FontFace first = package.GetFace(0);
        FontFace second = package.GetFace(0);
        byte[] originalCopy = package.CopyOriginalBytes();
        byte[] sfntCopy = package.CopySfntBytes(0);
        byte[] tableCopy = first.CopyTable(FontTags.Name);
        originalCopy[0] = 0;
        sfntCopy[0] = 0;
        tableCopy[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(package.GetSourceKind(), Is.EqualTo(FontContainerKind.Sfnt));
            Assert.That(package.GetFaceCount(), Is.EqualTo(1));
            Assert.That(first, Is.SameAs(second));
            Assert.That(package.GetCreatedFaceCount(), Is.EqualTo(1));
            Assert.That(first.GetFaceIndex(), Is.Zero);
            Assert.That(first.GetScalerType(), Is.EqualTo(0x00010000U));
            Assert.That(first.GetOutlineKind(), Is.EqualTo(FontOutlineKind.TrueType));
            Assert.That(first.GetTableCount(), Is.EqualTo(10));
            Assert.That(first.HasTable(FontTags.Name), Is.True);
            Assert.That(first.HasTable(FontTags.Cff), Is.False);
            Assert.That(package.CopyOriginalBytes(), Is.EqualTo(source));
            Assert.That(package.CopySfntBytes(0), Is.EqualTo(source));
            Assert.That(first.CopyTable(FontTags.Name), Is.EqualTo(new byte[] { 7, 8, 9 }));
            Assert.Throws<ArgumentException>(() => first.CopyTable(FontTags.Cff));
        });
    }

    [Test]
    public void CreatesExactlyOneFaceInstanceUnderConcurrentAccess()
    {
        FontPackage package = FontLoader.Load(BuildValidSfnt(), CompatibleOptions());
        FontFace[] results = new FontFace[256];

        Parallel.For(0, results.Length, index =>
        {
            results[index] = package.GetFace(0);
        });

        FontFace expected = results[0];

        Assert.Multiple(() =>
        {
            Assert.That(results.All(face => ReferenceEquals(face, expected)), Is.True);
            Assert.That(package.GetCreatedFaceCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void AggregatesLoadAndFaceDiagnosticsInStableOrderAndCreatesFaces()
    {
        byte[] source = new TinySfntBuilder()
            .AddTable("cmap", new byte[4])
            .AddTable("head", new byte[12])
            .AddTable("hhea", new byte[4])
            .AddTable("hmtx", new byte[4])
            .AddTable("maxp", new byte[4])
            .AddTable("name", new byte[] { 1 })
            .AddTable("OS/2", new byte[4])
            .AddTable("post", new byte[4])
            .AddTable("glyf", new byte[4])
            .AddTable("loca", new byte[4])
            .AddTable("ZZZZ", new byte[] { 4, 5 })
            .Build();
        BinaryPrimitives.WriteUInt16BigEndian(source.AsSpan(6, 2), 0);
        FontPackage package = FontLoader.Load(source, IgnoreChecksumOptions());

        Assert.That(package.GetCreatedFaceCount(), Is.Zero);
        FontDiagnostic[] first = package.GetDiagnostics();
        FontDiagnostic[] second = package.GetDiagnostics();
        Array.Reverse(first);

        Assert.Multiple(() =>
        {
            Assert.That(package.GetCreatedFaceCount(), Is.EqualTo(1));
            Assert.That(second.Length, Is.EqualTo(3));
            Assert.That(second[0].GetCode(), Is.EqualTo(FontDiagnosticCode.InvalidSearchFields));
            Assert.That(second[1].GetCode(), Is.EqualTo(FontDiagnosticCode.RepairApplied));
            Assert.That(second[2].GetCode(), Is.EqualTo(FontDiagnosticCode.TablePreservedAsRaw));
            Assert.That(second[2].GetTableTag(), Is.EqualTo((int)TestSfntEncoding.EncodeTag("ZZZZ")));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(package.GetDiagnostics()[0].GetCode(), Is.EqualTo(FontDiagnosticCode.InvalidSearchFields));
        });
    }

    [Test]
    public void ReconstructsEachCollectionFaceAsIndependentValidSfnt()
    {
        FontPackage package = FontLoader.Load(
            BuildTwoFaceWoff2Collection(),
            CompatibleOptions());

        Assert.That(package.GetCreatedFaceCount(), Is.Zero);
        byte[] first = package.CopySfntBytes(0);
        byte[] second = package.CopySfntBytes(1);
        byte[] secondCopy = package.CopySfntBytes(1);
        second[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(package.GetSourceKind(), Is.EqualTo(FontContainerKind.Woff2));
            Assert.That(package.GetFaceCount(), Is.EqualTo(2));
            Assert.That(package.GetFace(0).GetFaceIndex(), Is.Zero);
            Assert.That(package.GetFace(1).GetFaceIndex(), Is.EqualTo(1));
            Assert.That(package.GetCreatedFaceCount(), Is.EqualTo(2));
            Assert.That(ReadUInt32(first, 0), Is.EqualTo(0x00010000U));
            Assert.That(ReadUInt32(secondCopy, 0), Is.EqualTo(0x00010000U));
            Assert.That(FontChecksum.VerifyFont(first), Is.True);
            Assert.That(FontChecksum.VerifyFont(secondCopy), Is.True);
            Assert.That(CopyTable(first, FontTags.Name), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(CopyTable(secondCopy, FontTags.Name), Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
            Assert.That(package.CopySfntBytes(1), Is.EqualTo(secondCopy));
        });
    }

    [Test]
    public void HonorsDisabledRawPreservationWithoutAffectingFacePresence()
    {
        FontPackage package = FontLoader.Load(
            BuildValidSfnt(),
            WithoutRawPreservationOptions());
        FontFace face = package.GetFace(0);

        Assert.Multiple(() =>
        {
            Assert.That(face.HasTable(FontTags.Name), Is.True);
            Assert.Throws<InvalidOperationException>(() => face.CopyTable(FontTags.Name));
            Assert.That(package.CopySfntBytes(0), Is.EqualTo(BuildValidSfnt()));
        });
    }

    [Test]
    public void RejectsPackageIndexesAndInvalidInspectOnlyReconstruction()
    {
        FontPackage valid = FontLoader.Load(BuildValidSfnt(), CompatibleOptions());
        FontPackage invalid = FontLoader.Load(
            new TinySfntBuilder().AddTable("name", new byte[] { 1 }).Build(),
            FontLoadOptions.CreateInspectOnly());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => valid.GetFace(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => valid.GetFace(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => valid.CopySfntBytes(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => valid.CopySfntBytes(1));
            Assert.Throws<InvalidOperationException>(() => invalid.CopySfntBytes(0));
        });
    }

    [Test]
    public void LazyFaceCreationObservesTheLoadCancellationToken()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        FontLoadOptions options = CompatibleOptions(cancellation.Token);
        FontPackage package = FontLoader.Load(BuildValidSfnt(), options);
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() => package.GetFace(0));
            Assert.That(package.GetCreatedFaceCount(), Is.Zero);
        });
    }

    static byte[] BuildValidSfnt()
    {
        return new TinySfntBuilder()
            .AddTable("cmap", new byte[4])
            .AddTable("head", new byte[12])
            .AddTable("hhea", new byte[4])
            .AddTable("hmtx", new byte[4])
            .AddTable("maxp", new byte[4])
            .AddTable("name", new byte[] { 7, 8, 9 })
            .AddTable("OS/2", new byte[4])
            .AddTable("post", new byte[4])
            .AddTable("glyf", new byte[4])
            .AddTable("loca", new byte[4])
            .Build();
    }

    static byte[] BuildTwoFaceWoff2Collection()
    {
        return new Woff2Builder()
            .AddKnownTable(0, new byte[4])
            .AddKnownTable(1, new byte[12])
            .AddKnownTable(2, new byte[4])
            .AddKnownTable(3, new byte[4])
            .AddKnownTable(4, new byte[4])
            .AddKnownTable(5, new byte[] { 1, 2, 3 })
            .AddKnownTable(6, new byte[4])
            .AddKnownTable(7, new byte[4])
            .AddKnownTable(10, new byte[4])
            .AddKnownTable(11, new byte[4])
            .AddKnownTable(5, new byte[] { 4, 5, 6, 7 })
            .AddCollectionFace(0x00010000U, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9)
            .AddCollectionFace(0x00010000U, 0, 1, 2, 3, 4, 10, 6, 7, 8, 9)
            .Build();
    }

    static byte[] CopyTable(byte[] sfnt, int tag)
    {
        FontData data = FontData.Create(sfnt);
        SfntDirectory directory = SfntDirectoryParser.Parse(data.GetSlice(0, data.GetLength()));

        for (int index = 0; index < directory.GetTableCount(); index += 1)
        {
            SfntTableRecord record = directory.GetTableRecord(index);

            if (record.GetTag() == tag)
            {
                return sfnt.AsSpan((int)record.GetOffset(), (int)record.GetLength()).ToArray();
            }
        }

        throw new InvalidOperationException("The table was not found.");
    }

    static uint ReadUInt32(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
    }

    static FontLoadOptions CompatibleOptions()
    {
        return CompatibleOptions(CancellationToken.None);
    }

    static FontLoadOptions CompatibleOptions(CancellationToken cancellationToken)
    {
        return FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch,
            FontLimits.CreateCompatible(),
            cancellationToken);
    }

    static FontLoadOptions IgnoreChecksumOptions()
    {
        return FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.Ignore,
            FontLimits.CreateCompatible(),
            CancellationToken.None);
    }

    static FontLoadOptions WithoutRawPreservationOptions()
    {
        return FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            false,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.ReportMismatch,
            FontLimits.CreateCompatible(),
            CancellationToken.None);
    }
}
