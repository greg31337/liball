using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

public class SfntCollectionFaceCatalogTests
{
    [Test]
    public void EnumeratesFacesInHeaderOrderWithStableMetadata()
    {
        SfntCollectionFaceCatalog catalog = CreateCatalog(
            BuildCollection(true, FontTags.Name),
            FontLoadOptions.CreateCompatible());
        SfntCollectionFaceInfo first = catalog.GetFaceInfo(0);
        SfntCollectionFaceInfo second = catalog.GetFaceInfo(1);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.GetFaceCount(), Is.EqualTo(2));
            Assert.That(first.GetFaceIndex(), Is.EqualTo(0));
            Assert.That(first.GetDirectoryOffset(), Is.EqualTo(20));
            Assert.That(first.GetScalerType(), Is.EqualTo(0x00010000U));
            Assert.That(first.GetTableCount(), Is.EqualTo(1));
            Assert.That(second.GetFaceIndex(), Is.EqualTo(1));
            Assert.That(second.GetDirectoryOffset(), Is.EqualTo(48));
            Assert.That(second.GetScalerType(), Is.EqualTo(0x4F54544FU));
            Assert.That(second.GetTableCount(), Is.EqualTo(1));
            Assert.That(catalog.GetFaceInfo(0), Is.SameAs(first));
        });
    }

    [Test]
    public void SelectsEachFaceByZeroBasedIndex()
    {
        SfntCollectionFaceCatalog catalog = CreateCatalog(
            BuildCollection(false, FontTags.Name),
            FontLoadOptions.CreateCompatible());

        SfntSelectedCollectionFace first = catalog.SelectFace(0);
        SfntSelectedCollectionFace second = catalog.SelectFace(1);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetFaceIndex(), Is.EqualTo(0));
            Assert.That(first.GetDirectoryOffset(), Is.EqualTo(20));
            Assert.That(first.GetScalerType(), Is.EqualTo(0x00010000U));
            Assert.That(first.GetDeclaredTableCount(), Is.EqualTo(1));
            Assert.That(second.GetFaceIndex(), Is.EqualTo(1));
            Assert.That(second.GetDirectoryOffset(), Is.EqualTo(48));
            Assert.That(second.GetScalerType(), Is.EqualTo(0x4F54544FU));
            Assert.That(second.GetDeclaredTableCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void FaceIndicesFollowHeaderOrderRatherThanDirectoryOffsetOrder()
    {
        byte[] bytes = BuildCollection(false, FontTags.Name);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(12, 4), 48);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16, 4), 20);
        SfntCollectionFaceCatalog catalog = CreateCatalog(
            bytes,
            FontLoadOptions.CreateCompatible());

        SfntSelectedCollectionFace first = catalog.SelectFace(0);
        SfntSelectedCollectionFace second = catalog.SelectFace(1);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetDirectoryOffset(), Is.EqualTo(48));
            Assert.That(first.GetScalerType(), Is.EqualTo(0x4F54544FU));
            Assert.That(first.GetTableStore().GetTable(0).GetOffset(), Is.EqualTo(80));
            Assert.That(second.GetDirectoryOffset(), Is.EqualTo(20));
            Assert.That(second.GetScalerType(), Is.EqualTo(0x00010000U));
            Assert.That(second.GetTableStore().GetTable(0).GetOffset(), Is.EqualTo(76));
        });
    }

    [Test]
    public void SelectedTablesKeepOffsetsRelativeToCollectionRoot()
    {
        SfntCollectionFaceCatalog catalog = CreateCatalog(
            BuildCollection(false, FontTags.Name),
            FontLoadOptions.CreateCompatible());

        SfntSelectedCollectionFace first = catalog.SelectFace(0);
        SfntSelectedCollectionFace second = catalog.SelectFace(1);
        SfntTableData? firstTable = first.GetTableStore().FindTable(FontTags.Name);
        SfntTableData? secondTable = second.GetTableStore().FindTable(FontTags.Name);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetDirectory().GetTableRecord(0).GetOffset(), Is.EqualTo(76));
            Assert.That(firstTable!.GetOffset(), Is.EqualTo(76));
            Assert.That(first.GetTableRegion(0).GetOffset(), Is.EqualTo(76));
            Assert.That(firstTable.CopyRawData(), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(second.GetDirectory().GetTableRecord(0).GetOffset(), Is.EqualTo(80));
            Assert.That(secondTable!.GetOffset(), Is.EqualTo(80));
            Assert.That(second.GetTableRegion(0).GetOffset(), Is.EqualTo(80));
            Assert.That(secondTable.CopyRawData(), Is.EqualTo(new byte[] { 5, 6, 7, 8 }));
        });
    }

    [Test]
    public void SelectedFacesRetainCanonicalSharedRegionIdentity()
    {
        SfntCollectionFaceCatalog catalog = CreateCatalog(
            BuildCollection(true, FontTags.Name),
            FontLoadOptions.CreateCompatible());

        SfntSelectedCollectionFace first = catalog.SelectFace(0);
        SfntSelectedCollectionFace second = catalog.SelectFace(1);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetTableRegion(0), Is.SameAs(second.GetTableRegion(0)));
            Assert.That(first.GetTableRegion(0).GetFaceReferenceCount(), Is.EqualTo(2));
            Assert.That(first.GetTableRegion(0).GetOffset(), Is.EqualTo(76));
        });
    }

    [Test]
    public void SelectionProvidesFaceScopedValidationDiagnostics()
    {
        SfntCollectionFaceCatalog catalog = CreateCatalog(
            BuildCollection(true, FontTags.Name),
            FontLoadOptions.CreateCompatible());

        SfntSelectedCollectionFace selected = catalog.SelectFace(1);
        SfntValidationResult validation = selected.GetValidationResult();
        FontDiagnostic diagnostic = FindDiagnostic(
            validation,
            FontDiagnosticCode.MissingRequiredTable);

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid(), Is.False);
            Assert.That(diagnostic.GetFaceIndex(), Is.EqualTo(1));
            Assert.That(diagnostic.GetAbsoluteOffset(), Is.EqualTo(-1));
        });
    }

    [Test]
    public void SelectionProvidesFaceScopedRawPreservationDiagnostics()
    {
        int unknownTag = (int)TestSfntEncoding.EncodeTag("ZZZZ");
        SfntCollectionFaceCatalog catalog = CreateCatalog(
            BuildCollection(true, unknownTag),
            FontLoadOptions.CreateCompatible());

        SfntSelectedCollectionFace selected = catalog.SelectFace(1);
        SfntTableStore tables = selected.GetTableStore();

        Assert.Multiple(() =>
        {
            Assert.That(tables.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(tables.GetDiagnostic(0).GetCode(), Is.EqualTo(FontDiagnosticCode.TablePreservedAsRaw));
            Assert.That(tables.GetDiagnostic(0).GetFaceIndex(), Is.EqualTo(1));
            Assert.That(tables.GetDiagnostic(0).GetTableTag(), Is.EqualTo(unknownTag));
            Assert.That(tables.GetDiagnostic(0).GetAbsoluteOffset(), Is.EqualTo(76));
        });
    }

    [Test]
    public void SelectionHonorsDisabledRawPreservation()
    {
        SfntCollectionFaceCatalog catalog = CreateCatalog(
            BuildCollection(true, FontTags.Name),
            CreateWithoutRawPreservation());

        SfntTableData table = catalog.SelectFace(0).GetTableStore().GetTable(0);

        Assert.Multiple(() =>
        {
            Assert.That(table.HasRawData(), Is.False);
            Assert.That(table.GetOffset(), Is.EqualTo(76));
            Assert.Throws<InvalidOperationException>(() => table.CopyRawData());
        });
    }

    [Test]
    public void RepeatedSelectionIsDeterministicWithoutPrematureFaceCaching()
    {
        SfntCollectionFaceCatalog catalog = CreateCatalog(
            BuildCollection(true, FontTags.Name),
            FontLoadOptions.CreateCompatible());

        SfntSelectedCollectionFace first = catalog.SelectFace(0);
        SfntSelectedCollectionFace second = catalog.SelectFace(0);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.GetDirectory(), Is.SameAs(second.GetDirectory()));
            Assert.That(first.GetTableRegion(0), Is.SameAs(second.GetTableRegion(0)));
            Assert.That(
                first.GetTableStore().GetTable(0).CopyRawData(),
                Is.EqualTo(second.GetTableStore().GetTable(0).CopyRawData()));
        });
    }

    [Test]
    public void EnumerationAndSelectionRejectIndicesOutsideCatalog()
    {
        SfntCollectionFaceCatalog catalog = CreateCatalog(
            BuildCollection(true, FontTags.Name),
            FontLoadOptions.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.GetFaceInfo(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.GetFaceInfo(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.SelectFace(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.SelectFace(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.SelectFace(0).GetTableRegion(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.SelectFace(0).GetTableRegion(1));
        });
    }

    [Test]
    public void CatalogConstructorRejectsNullArguments()
    {
        byte[] bytes = BuildCollection(true, FontTags.Name);
        FontLoadOptions options = FontLoadOptions.CreateCompatible();
        FontData data = FontData.Create(bytes);
        SfntCollection collection = SfntCollectionParser.Parse(data, options);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => new SfntCollectionFaceCatalog(
                null!,
                collection,
                options));
            Assert.Throws<ArgumentNullException>(() => new SfntCollectionFaceCatalog(
                data,
                null!,
                options));
            Assert.Throws<ArgumentNullException>(() => new SfntCollectionFaceCatalog(
                data,
                collection,
                null!));
        });
    }

    [Test]
    public void SelectionObservesCancellationAfterEnumeration()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        FontLoadOptions options = FontLoadOptions.CreateCompatible(source.Token);
        byte[] bytes = BuildCollection(true, FontTags.Name);
        FontData data = FontData.Create(bytes);
        SfntCollection collection = SfntCollectionParser.Parse(data, options);
        SfntCollectionFaceCatalog catalog = new SfntCollectionFaceCatalog(
            data,
            collection,
            options);
        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(catalog.GetFaceCount(), Is.EqualTo(2));
            Assert.Throws<OperationCanceledException>(() => catalog.SelectFace(0));
        });
    }

    private static SfntCollectionFaceCatalog CreateCatalog(
        byte[] bytes,
        FontLoadOptions options)
    {
        FontData data = FontData.Create(bytes);
        SfntCollection collection = SfntCollectionParser.Parse(data, options);
        return new SfntCollectionFaceCatalog(data, collection, options);
    }

    private static FontDiagnostic FindDiagnostic(
        SfntValidationResult result,
        FontDiagnosticCode code)
    {
        for (int index = 0; index < result.GetDiagnosticCount(); index += 1)
        {
            FontDiagnostic diagnostic = result.GetDiagnostic(index);

            if (diagnostic.GetCode() == code)
            {
                return diagnostic;
            }
        }

        throw new AssertionException($"Diagnostic {code} was not found.");
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

    private static byte[] BuildCollection(bool sharedTable, int tag)
    {
        int firstFaceOffset = 20;
        int secondFaceOffset = 48;
        int firstTableOffset = 76;
        int secondTableOffset = sharedTable ? firstTableOffset : 80;
        int length = sharedTable ? 80 : 84;
        byte[] firstData = new byte[] { 1, 2, 3, 4 };
        byte[] secondData = sharedTable ? firstData : new byte[] { 5, 6, 7, 8 };
        byte[] bytes = new byte[length];
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 0x74746366U);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4, 4), 0x00010000U);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8, 4), 2);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(12, 4), (uint)firstFaceOffset);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16, 4), (uint)secondFaceOffset);
        WriteFaceDirectory(
            bytes,
            firstFaceOffset,
            0x00010000U,
            tag,
            firstTableOffset,
            firstData);
        WriteFaceDirectory(
            bytes,
            secondFaceOffset,
            0x4F54544FU,
            tag,
            secondTableOffset,
            secondData);
        firstData.CopyTo(bytes, firstTableOffset);

        if (!sharedTable)
        {
            secondData.CopyTo(bytes, secondTableOffset);
        }

        return bytes;
    }

    private static void WriteFaceDirectory(
        byte[] bytes,
        int directoryOffset,
        uint scalerType,
        int tag,
        int tableOffset,
        byte[] tableData)
    {
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(directoryOffset, 4), scalerType);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(directoryOffset + 4, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(directoryOffset + 6, 2), 16);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(directoryOffset + 12, 4), (uint)tag);
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(directoryOffset + 16, 4),
            FontChecksum.CalculateTable(tableData));
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(directoryOffset + 20, 4),
            (uint)tableOffset);
        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(directoryOffset + 24, 4),
            (uint)tableData.Length);
    }
}
