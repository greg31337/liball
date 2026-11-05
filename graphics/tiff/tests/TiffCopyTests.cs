using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffCopyTests
{
    public const int CustomMetadataTag = 65000;

    [Test]
    public void CopyMetadata_ReturnsMetadataValuesAndExcludesStructuralTags()
    {
        TiffImage source = OpenSourceImage(true);

        TiffMetadataCopyResult result = TiffCopy.CopyMetadata(source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ValueCount(), Is.EqualTo(3));
        Assert.That(FindValue(result.Values, TiffTagId.ImageDescription)!.Text, Is.EqualTo("source metadata"));
        Assert.That(FindValue(result.Values, TiffTagId.Software)!.Text, Is.EqualTo("unit-test"));
        Assert.That(FindValue(result.Values, CustomMetadataTag)!.UnsignedValues[0], Is.EqualTo(42));
        Assert.That(FindValue(result.Values, TiffTagId.ImageWidth), Is.Null);
        Assert.That(FindValue(result.Values, TiffTagId.StripOffsets), Is.Null);
    }

    [Test]
    public void CopyMetadata_AppliesValuesToImageDefinitionWithoutOverwritingGeometry()
    {
        TiffImage source = OpenSourceImage(true);
        TiffImageDefinition destination = TiffImageDefinition.Create(CreateStripTags(1, 1));

        TiffImageDefinition copied = TiffCopy.CopyMetadata(source, destination);
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(copied);
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 9 });
        writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        TiffImage image = file.GetImage(0);
        TiffMetadataCopyResult metadata = TiffCopy.CopyMetadata(image);

        Assert.That(file.Success, Is.True);
        Assert.That(image.GetInfo().Width, Is.EqualTo(1));
        Assert.That(image.GetInfo().Length, Is.EqualTo(1));
        Assert.That(FindValue(metadata.Values, TiffTagId.ImageDescription)!.Text, Is.EqualTo("source metadata"));
        Assert.That(FindValue(metadata.Values, TiffTagId.Software)!.Text, Is.EqualTo("unit-test"));
        Assert.That(FindValue(metadata.Values, CustomMetadataTag)!.UnsignedValues[0], Is.EqualTo(42));
    }

    [Test]
    public void CopyMetadata_WritesValuesToExistingWriterImage()
    {
        TiffImage source = OpenSourceImage(true);
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(2, 1)));

        TiffDiagnostics diagnostics = TiffCopy.CopyMetadata(source, writer, imageIndex);
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2 });
        writer.Flush();
        TiffMetadataCopyResult metadata = TiffCopy.CopyMetadata(TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0));

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(writer.Success, Is.True);
        Assert.That(FindValue(metadata.Values, TiffTagId.ImageDescription)!.Text, Is.EqualTo("source metadata"));
        Assert.That(FindValue(metadata.Values, CustomMetadataTag)!.UnsignedValues[0], Is.EqualTo(42));
    }

    [Test]
    public void CopyMetadata_RespectsPreserveUnknownTagsOption()
    {
        TiffImage source = OpenSourceImage(false);

        TiffMetadataCopyResult result = TiffCopy.CopyMetadata(source);

        Assert.That(result.Success, Is.True);
        Assert.That(FindValue(result.Values, TiffTagId.ImageDescription), Is.Not.Null);
        Assert.That(FindValue(result.Values, CustomMetadataTag), Is.Null);
    }

    [Test]
    public void CopyMetadata_ReportsNullSource()
    {
        TiffMetadataCopyResult result = TiffCopy.CopyMetadata(null!);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnknownError), Is.True);
    }

    [Test]
    public void CopyRawImageData_CopiesStripsIntoImageDefinition()
    {
        TiffImage source = OpenStripImage(new byte[] { 1, 2, 3, 4 });

        TiffRawImageDataCopyResult result = TiffCopy.CopyRawImageData(source);
        TiffWriter writer = TiffWriter.Create();
        writer.AddImage(result.Definition);
        writer.Flush();
        TiffImage copied = TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsTiled, Is.False);
        Assert.That(result.StripCount(), Is.EqualTo(1));
        Assert.That(FindValue(result.Definition.Tags, TiffTagId.ImageWidth)!.UnsignedValues[0], Is.EqualTo(2));
        Assert.That(copied.GetInfo().Width, Is.EqualTo(2));
        Assert.That(copied.ReadRawStrip(0).Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void CopyRawImageData_WritesStripsToExistingWriterImage()
    {
        TiffImage source = OpenStripImage(new byte[] { 5, 6, 7, 8 });
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(new TiffImageDefinition());

        TiffDiagnostics diagnostics = TiffCopy.CopyRawImageData(source, writer, imageIndex);
        writer.Flush();
        TiffImage copied = TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        Assert.That(writer.Success, Is.True);
        Assert.That(copied.GetInfo().Length, Is.EqualTo(2));
        Assert.That(copied.ReadRawStrip(0).Bytes, Is.EqualTo(new byte[] { 5, 6, 7, 8 }));
    }

    [Test]
    public void CopyRawImageData_CopiesTilesIntoImageDefinition()
    {
        TiffImage source = OpenTileImage();

        TiffRawImageDataCopyResult result = TiffCopy.CopyRawImageData(source);
        TiffWriter writer = TiffWriter.Create();
        writer.AddImage(result.Definition);
        writer.Flush();
        TiffImage copied = TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsTiled, Is.True);
        Assert.That(result.TileCount(), Is.EqualTo(4));
        Assert.That(copied.GetTileCount(), Is.EqualTo(4));
        Assert.That(copied.ReadRawTile(2).Bytes, Is.EqualTo(new byte[] { 12 }));
    }

    [Test]
    public void CopyRawImageData_ReportsIncompatibleDestinationLayout()
    {
        TiffImage source = OpenStripImage(new byte[] { 1, 2, 3, 4 });
        TiffImageDefinition destination = TiffImageDefinition.Create(CreateStripTags(3, 2));

        TiffRawImageDataCopyResult result = TiffCopy.CopyRawImageData(source, destination);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    [Test]
    public void CopyRawImageData_ReportsMetadataOnlySource()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(2, 2)));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4 });
        writer.Flush();
        TiffImage source = TiffFile.OpenMetadata(writer.GetFileBytes()).GetImage(0);

        TiffRawImageDataCopyResult result = TiffCopy.CopyRawImageData(source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnknownError), Is.True);
    }

    static TiffImage OpenSourceImage(bool preserveUnknownTags)
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateSourceTags()));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4 });
        writer.Flush();
        TiffReadOptions options = new TiffReadOptions();
        options.PreserveUnknownTags = preserveUnknownTags;
        return TiffFile.OpenRead(writer.GetFileBytes(), options).GetImage(0);
    }

    static TiffImage OpenStripImage(byte[] stripBytes)
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags(2, 2)));
        writer.WriteRawStrip(imageIndex, 0, stripBytes);
        writer.Flush();
        return TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0);
    }

    static TiffImage OpenTileImage()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTileTags()));
        writer.WriteRawTile(imageIndex, 0, new byte[] { 10 });
        writer.WriteRawTile(imageIndex, 1, new byte[] { 11 });
        writer.WriteRawTile(imageIndex, 2, new byte[] { 12 });
        writer.WriteRawTile(imageIndex, 3, new byte[] { 13 });
        writer.Flush();
        return TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0);
    }

    static TiffTagValue[] CreateSourceTags()
    {
        TiffTagValue[] tags = CreateStripTags(2, 2);
        tags = TiffImageDefinition.SetTagValue(tags, TiffTagValue.CreateAscii(TiffTagId.ImageDescription, "source metadata"));
        tags = TiffImageDefinition.SetTagValue(tags, TiffTagValue.CreateAscii(TiffTagId.Software, "unit-test"));
        tags = TiffImageDefinition.SetTagValue(tags, TiffTagValue.CreateUnsignedScalar(CustomMetadataTag, TiffDataType.Long, 42));
        return tags;
    }

    static TiffTagValue[] CreateStripTags(ulong width, ulong length)
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, length)
        };
    }

    static TiffTagValue[] CreateTileTags()
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileWidth, TiffDataType.Long, 1),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileLength, TiffDataType.Long, 1)
        };
    }

    static TiffTagValue? FindValue(TiffTagValue[] values, int tagId)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].TagId == tagId)
            {
                return values[index];
            }

            index = index + 1;
        }

        return null;
    }
}
