using System.Buffers.Binary;
using System.Reflection;
using ttf;

namespace ttf.Tests;

internal class SfntRawTableTests
{
    [Test]
    public void PreservesKnownUnsupportedAndUnknownTablesAsExactBoundedData()
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("name", new byte[] { 3, 4 })
            .AddTable("morx", new byte[] { 1, 2, 3 })
            .AddTable("ZZZZ", new byte[] { 9, 8 })
            .Build();

        SfntTableStore store = CreateStore(font, FontLoadOptions.CreateCompatible());
        SfntTableData? name = store.FindTable(FontTags.Name);
        SfntTableData? morx = store.FindTable((int)TestSfntEncoding.EncodeTag("morx"));
        SfntTableData? unknown = store.FindTable((int)TestSfntEncoding.EncodeTag("ZZZZ"));

        Assert.Multiple(() =>
        {
            Assert.That(name!.GetClassification(), Is.EqualTo(SfntTableClassification.Known));
            Assert.That(name.CopyRawData(), Is.EqualTo(new byte[] { 3, 4 }));
            Assert.That(morx!.GetClassification(), Is.EqualTo(SfntTableClassification.Unsupported));
            Assert.That(morx.CopyRawData(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(unknown!.GetClassification(), Is.EqualTo(SfntTableClassification.Unknown));
            Assert.That(unknown.CopyRawData(), Is.EqualTo(new byte[] { 9, 8 }));
            Assert.That(unknown.GetChecksum(), Is.EqualTo(FontChecksum.CalculateTable(new byte[] { 9, 8 })));
            Assert.That(morx.GetDataSlice().GetAbsoluteOffset(), Is.EqualTo(morx.GetOffset()));
            Assert.That(unknown.GetDataSlice().GetLength(), Is.EqualTo(2));
        });
    }

    [Test]
    public void EnumerationIsSortedByUnsignedTagRegardlessOfDirectoryOrder()
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("morx", new byte[] { 1 })
            .AddTable("head", new byte[12])
            .AddTable("ZZZZ", new byte[] { 2 })
            .Build();
        SwapRecords(font, 0, 2);

        SfntTableStore store = CreateStore(font, FontLoadOptions.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(store.GetTableCount(), Is.EqualTo(3));
            Assert.That(store.GetTable(0).GetTag(), Is.EqualTo(TestSfntEncoding.EncodeTag("ZZZZ")));
            Assert.That(store.GetTable(1).GetTag(), Is.EqualTo(FontTags.Head));
            Assert.That(store.GetTable(2).GetTag(), Is.EqualTo(TestSfntEncoding.EncodeTag("morx")));
        });
    }

    [Test]
    public void EqualTagsRetainDirectoryOrderDuringDeterministicSort()
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("AAAA", new byte[] { 1 })
            .AddTable("BBBB", new byte[] { 2 })
            .Build();
        int secondRecord = 12 + 16;
        BinaryPrimitives.WriteUInt32BigEndian(
            font.AsSpan(secondRecord, 4),
            (uint)TestSfntEncoding.EncodeTag("AAAA"));

        SfntTableStore store = CreateStore(font, FontLoadOptions.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(store.GetTable(0).GetTag(), Is.EqualTo(TestSfntEncoding.EncodeTag("AAAA")));
            Assert.That(store.GetTable(1).GetTag(), Is.EqualTo(TestSfntEncoding.EncodeTag("AAAA")));
            Assert.That(store.GetTable(0).CopyRawData(), Is.EqualTo(new byte[] { 1 }));
            Assert.That(store.GetTable(1).CopyRawData(), Is.EqualTo(new byte[] { 2 }));
        });
    }

    [Test]
    public void CopyRawDataReturnsIndependentArrays()
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("ZZZZ", new byte[] { 4, 5, 6 })
            .Build();
        SfntTableData table = CreateStore(
            font,
            FontLoadOptions.CreateCompatible()).GetTable(0);

        byte[] first = table.CopyRawData();
        byte[] second = table.CopyRawData();
        first[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(new byte[] { 4, 5, 6 }));
            Assert.That(first, Is.Not.SameAs(second));
        });
    }

    [Test]
    public void OwnedFontDataInsulatesRawTablesFromCallerMutation()
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("ZZZZ", new byte[] { 7, 8, 9 })
            .Build();
        FontData data = FontData.Create(font);
        SfntDirectory directory = ParseDirectory(data);
        SfntTableStore store = SfntTableStoreBuilder.Create(
            data,
            directory,
            FontLoadOptions.CreateCompatible(),
            0);
        int tableOffset = (int)store.GetTable(0).GetOffset();

        font[tableOffset] = 0;

        Assert.That(store.GetTable(0).CopyRawData(), Is.EqualTo(new byte[] { 7, 8, 9 }));
    }

    [Test]
    public void RawPreservationOptionDisablesCopiesButKeepsMetadataEnumeration()
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("head", new byte[12])
            .AddTable("ZZZZ", new byte[] { 1, 2 })
            .Build();

        SfntTableStore store = CreateStore(font, CreateWithoutRawPreservation());

        Assert.Multiple(() =>
        {
            Assert.That(store.GetTableCount(), Is.EqualTo(2));
            Assert.That(store.GetTable(0).HasRawData(), Is.False);
            Assert.That(store.GetTable(1).HasRawData(), Is.False);
            Assert.Throws<InvalidOperationException>(() => store.GetTable(0).CopyRawData());
            Assert.That(store.GetDiagnosticCount(), Is.EqualTo(0));
        });
    }

    [Test]
    public void UnsafeDeclaredRangesAreNotExposedAsTables()
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("head", new byte[12])
            .AddTable("ZZZZ", new byte[] { 1, 2 })
            .Build();
        int unknownRecord = FindRecordOffset(font, (int)TestSfntEncoding.EncodeTag("ZZZZ"));
        BinaryPrimitives.WriteUInt32BigEndian(
            font.AsSpan(unknownRecord + 8, 4),
            uint.MaxValue);

        SfntTableStore store = CreateStore(font, FontLoadOptions.CreateInspectOnly());

        Assert.Multiple(() =>
        {
            Assert.That(store.GetTableCount(), Is.EqualTo(1));
            Assert.That(store.HasTable(FontTags.Head), Is.True);
            Assert.That(store.HasTable((int)TestSfntEncoding.EncodeTag("ZZZZ")), Is.False);
        });
    }

    [Test]
    public void ZeroLengthTableAtEndOfFontRemainsBoundedAndCopyable()
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("ZZZZ", Array.Empty<byte>())
            .Build();

        SfntTableData table = CreateStore(
            font,
            FontLoadOptions.CreateCompatible()).GetTable(0);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetOffset(), Is.EqualTo(font.Length));
            Assert.That(table.GetLength(), Is.EqualTo(0));
            Assert.That(table.CopyRawData(), Is.Empty);
        });
    }

    [Test]
    public void LookupReportsPresentAndMissingTagsDeterministically()
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("head", new byte[12])
            .Build();
        SfntTableStore store = CreateStore(font, FontLoadOptions.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(store.HasTable(FontTags.Head), Is.True);
            Assert.That(store.FindTable(FontTags.Head), Is.SameAs(store.GetTable(0)));
            Assert.That(store.HasTable(FontTags.Name), Is.False);
            Assert.That(store.FindTable(FontTags.Name), Is.Null);
        });
    }

    [Test]
    public void UnknownAndUnsupportedPreservationProducesStructuredDiagnostics()
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("morx", new byte[] { 1 })
            .AddTable("ZZZZ", new byte[] { 2 })
            .Build();

        SfntTableStore store = CreateStore(
            font,
            FontLoadOptions.CreateCompatible(),
            4);

        Assert.Multiple(() =>
        {
            Assert.That(store.GetDiagnosticCount(), Is.EqualTo(2));
            Assert.That(store.GetDiagnostic(0).GetCode(), Is.EqualTo(FontDiagnosticCode.TablePreservedAsRaw));
            Assert.That(store.GetDiagnostic(0).GetFaceIndex(), Is.EqualTo(4));
            Assert.That(store.GetDiagnostic(0).GetSeverity(), Is.EqualTo(FontDiagnosticSeverity.Information));
            Assert.That(store.GetDiagnostic(1).GetCode(), Is.EqualTo(FontDiagnosticCode.TablePreservedAsRaw));
        });
    }

    [Test]
    public void EveryPublishedFontTagIsClassifiedAsKnown()
    {
        FieldInfo[] fields = typeof(FontTags).GetFields(BindingFlags.Public | BindingFlags.Static);

        Assert.Multiple(() =>
        {
            foreach (FieldInfo field in fields)
            {
                int tag = (int)field.GetRawConstantValue()!;
                Assert.That(
                    SfntTableClassifier.Classify(tag),
                    Is.EqualTo(SfntTableClassification.Known),
                    field.Name);
            }
        });
    }

    [TestCase("ankr")]
    [TestCase("acnt")]
    [TestCase("bdat")]
    [TestCase("morx")]
    [TestCase("kerx")]
    [TestCase("xref")]
    [TestCase("Zapf")]
    [TestCase("Feat")]
    [TestCase("Glat")]
    [TestCase("Gloc")]
    [TestCase("Silf")]
    [TestCase("Sill")]
    public void RecognizesUnsupportedAatAndGraphiteTables(string tag)
    {
        Assert.That(
            SfntTableClassifier.Classify((int)TestSfntEncoding.EncodeTag(tag)),
            Is.EqualTo(SfntTableClassification.Unsupported));
    }

    [Test]
    public void ArbitraryPrivateTagRemainsUnknown()
    {
        Assert.That(
            SfntTableClassifier.Classify((int)TestSfntEncoding.EncodeTag("ZZZZ")),
            Is.EqualTo(SfntTableClassification.Unknown));
    }

    [Test]
    public void IndexAccessRejectsValuesOutsideStore()
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("head", new byte[12])
            .Build();
        SfntTableStore store = CreateStore(font, FontLoadOptions.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => store.GetTable(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.GetTable(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.GetDiagnostic(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.GetDiagnostic(0));
        });
    }

    [Test]
    public void BuilderRejectsNullArgumentsAndNegativeFaceIndex()
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("head", new byte[12])
            .Build();
        FontData data = FontData.Create(font);
        SfntDirectory directory = ParseDirectory(data);
        FontLoadOptions options = FontLoadOptions.CreateCompatible();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => SfntTableStoreBuilder.Create(
                null!,
                directory,
                options,
                0));
            Assert.Throws<ArgumentNullException>(() => SfntTableStoreBuilder.Create(
                data,
                null!,
                options,
                0));
            Assert.Throws<ArgumentNullException>(() => SfntTableStoreBuilder.Create(
                data,
                directory,
                null!,
                0));
            Assert.Throws<ArgumentOutOfRangeException>(() => SfntTableStoreBuilder.Create(
                data,
                directory,
                options,
                -1));
        });
    }

    [Test]
    public void BuilderObservesCancellationBeforeCreatingViews()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        FontLoadOptions options = FontLoadOptions.CreateCompatible(source.Token);
        byte[] font = new TinySfntBuilder()
            .AddTable("head", new byte[12])
            .Build();

        Assert.Throws<OperationCanceledException>(() => CreateStore(font, options));
    }

    private static SfntTableStore CreateStore(byte[] font, FontLoadOptions options)
    {
        return CreateStore(font, options, 0);
    }

    private static SfntTableStore CreateStore(
        byte[] font,
        FontLoadOptions options,
        int faceIndex)
    {
        FontData data = FontData.Create(font);
        SfntDirectory directory = ParseDirectory(data);
        return SfntTableStoreBuilder.Create(data, directory, options, faceIndex);
    }

    private static SfntDirectory ParseDirectory(FontData data)
    {
        return SfntDirectoryParser.Parse(data.GetSlice(0, data.GetLength()));
    }

    private static FontLoadOptions CreateWithoutRawPreservation()
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

    private static int FindRecordOffset(byte[] font, int tag)
    {
        int count = BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(4, 2));

        for (int index = 0; index < count; index += 1)
        {
            int recordOffset = 12 + index * 16;
            int actualTag = BinaryPrimitives.ReadInt32BigEndian(font.AsSpan(recordOffset, 4));

            if (actualTag == tag)
            {
                return recordOffset;
            }
        }

        throw new InvalidOperationException("The requested test table was not found.");
    }

    private static void SwapRecords(byte[] font, int firstIndex, int secondIndex)
    {
        int firstOffset = 12 + firstIndex * 16;
        int secondOffset = 12 + secondIndex * 16;
        byte[] temporary = font.AsSpan(firstOffset, 16).ToArray();
        font.AsSpan(secondOffset, 16).CopyTo(font.AsSpan(firstOffset, 16));
        temporary.CopyTo(font, secondOffset);
    }
}
