using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

public class FontSubsetFullFontWriterTests
{
    [Test]
    public void FullFontModePreservesEveryUnchangedRawTable()
    {
        byte[] head = new byte[]
        {
            1, 2, 3, 4,
            5, 6, 7, 8,
            0xAA, 0xBB, 0xCC, 0xDD,
            9, 10, 11, 12
        };
        byte[] name = new byte[] { 21, 22, 23, 24, 25 };
        byte[] privateData = new byte[] { 31, 32, 33 };
        int privateTag = Tag("ZZZZ");
        FontFace face = CreateFace(head, name, privateTag, privateData);
        FontSubset subset = CreateFullFontSubset(
            face.CreateDefaultInstance());

        byte[] first = subset.CreateFullFontData();
        byte[] second = subset.CreateFullFontData();
        byte[] outputHead = CopyTable(first, FontTags.Head);
        SfntDirectory directory = ParseDirectory(first);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(FontChecksum.VerifyFont(first), Is.True);
            Assert.That(directory.GetTableCount(), Is.EqualTo(4));
            Assert.That(CopyTable(first, FontTags.Name), Is.EqualTo(name));
            Assert.That(CopyTable(first, privateTag),
                Is.EqualTo(privateData));
            Assert.That(CopyTable(first, FontTags.Maxp),
                Is.EqualTo(BuildMaxp()));
            Assert.That(outputHead.AsSpan(0, 8).ToArray(),
                Is.EqualTo(head.AsSpan(0, 8).ToArray()));
            Assert.That(outputHead.AsSpan(12, 4).ToArray(),
                Is.EqualTo(head.AsSpan(12, 4).ToArray()));
            Assert.That(ReadUInt32(outputHead, 8),
                Is.Not.EqualTo(0xAABBCCDDU));
            Assert.That(ReadUInt32(head, 8), Is.EqualTo(0xAABBCCDDU));
            Assert.Throws<InvalidOperationException>(() =>
                face.CopyTable(privateTag));
        });

        int index = 1;
        while (index < directory.GetTableCount())
        {
            Assert.That(
                (uint)directory.GetTableRecord(index - 1).GetTag(),
                Is.LessThan((uint)directory.GetTableRecord(index).GetTag()));
            index += 1;
        }
    }

    [Test]
    public void FullFontModeMaterializesVariableTrueTypeInstance()
    {
        FontFace face = FontInstanceTests.CreateVariableFace(true);
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set(Tag("wght"), 0.5);
        FontSubset subset = CreateFullFontSubset(
            face.CreateInstance(coordinates));

        byte[] output = subset.CreateFullFontData();
        SfntDirectory directory = ParseDirectory(output);

        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.VerifyFont(output), Is.True);
            Assert.That(HasTable(directory, FontTags.Head), Is.True);
            Assert.That(HasTable(directory, FontTags.Glyf), Is.True);
            Assert.That(HasTable(directory, FontTags.Fvar), Is.False);
            Assert.That(HasTable(directory, FontTags.Gvar), Is.False);
            Assert.That(HasTable(directory, FontTags.Cvar), Is.False);
            Assert.That(HasTable(directory, FontTags.Hvar), Is.False);
            Assert.That(HasTable(directory, FontTags.Vvar), Is.False);
            Assert.That(HasTable(directory, FontTags.Mvar), Is.False);
        });
    }

    [Test]
    public void SubsetModeRejectsFullFontSerialization()
    {
        FontSubset subset = new FontSubsetBuilder(
            FontInstanceTests.CreateFace().CreateDefaultInstance(),
            FontSubsetOptions.CreateStrict()).Build();

        Assert.Throws<InvalidOperationException>(() =>
            subset.CreateFullFontData());
    }

    static FontSubset CreateFullFontSubset(FontInstance instance)
    {
        FontSubsetOptions options = FontSubsetOptions.CreateCompatible();
        options.SetEmbeddingMode(FontSubsetEmbeddingMode.FullFont);
        return new FontSubsetBuilder(instance, options).Build();
    }

    static FontFace CreateFace(
        byte[] head,
        byte[] name,
        int privateTag,
        byte[] privateData)
    {
        SfntTableData[] tables = new SfntTableData[]
        {
            CreateTable(
                FontTags.Name,
                name,
                SfntTableClassification.Known),
            CreateTable(
                privateTag,
                privateData,
                SfntTableClassification.Unknown),
            CreateTable(
                FontTags.Head,
                head,
                SfntTableClassification.Known),
            CreateTable(
                FontTags.Maxp,
                BuildMaxp(),
                SfntTableClassification.Known)
        };
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables, Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.BitmapOnly,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static SfntTableData CreateTable(
        int tag,
        byte[] bytes,
        SfntTableClassification classification)
    {
        FontData data = FontData.Create(bytes);
        return new SfntTableData(
            tag,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            classification,
            false);
    }

    static byte[] BuildMaxp()
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16(1)
            .CopyBytes();
    }

    static SfntDirectory ParseDirectory(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        return SfntDirectoryParser.Parse(
            data.GetSlice(0, data.GetLength()));
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

    static byte[] CopyTable(byte[] sfnt, int tag)
    {
        SfntDirectory directory = ParseDirectory(sfnt);
        int index = 0;
        while (index < directory.GetTableCount())
        {
            SfntTableRecord record = directory.GetTableRecord(index);
            if (record.GetTag() == tag)
            {
                return sfnt.AsSpan(
                    (int)record.GetOffset(),
                    (int)record.GetLength()).ToArray();
            }
            index += 1;
        }
        throw new InvalidOperationException("The table was not found.");
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }

    static uint ReadUInt32(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(
            data.AsSpan(offset, 4));
    }
}
