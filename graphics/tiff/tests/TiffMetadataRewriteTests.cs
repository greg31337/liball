using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffMetadataRewriteTests
{
    const int CustomMetadataTag = 65010;
    const int DateTimeOriginalTag = 36867;

    [Test]
    public void RewriteImageMetadata_PassesThroughUnsupportedEncodedDataAndAppliesChanges()
    {
        byte[] encodedStrip = new byte[] { 91, 17, 204, 3, 88 };
        TiffImage source = OpenSourceImage(encodedStrip, false, false);
        TiffWriter destination = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        TiffMetadataRewriteOptions options = new TiffMetadataRewriteOptions();
        options.Updates = new TiffTagValue[]
        {
            TiffApi.Data.CreateAsciiTag(TiffTagId.ImageDescription, "rewritten description"),
            TiffApi.Data.CreateAsciiTag(TiffTagId.Artist, "metadata editor")
        };
        options.RemovedTagIds = new int[] { TiffTagId.Software, TiffTagId.ExifIfd };

        TiffMetadataRewriteResult result = TiffApi.Copy.RewriteImageMetadata(source, destination, options);
        TiffDiagnostics flush = destination.Flush();
        TiffReadOptions readOptions = new TiffReadOptions();
        readOptions.PreserveUnknownTags = true;
        TiffImage rewritten = TiffFile.OpenRead(destination.GetFileBytes(), readOptions).GetImage(0);
        TiffMetadataCopyResult metadata = TiffApi.Copy.CopyMetadata(rewritten);

        Assert.That(result.Success, Is.True);
        Assert.That(result.MetadataCopied, Is.True);
        Assert.That(result.UnknownMetadataPreserved, Is.True);
        Assert.That(result.RawImageDataPassThroughRequested, Is.True);
        Assert.That(result.CanPassThroughRawImageData, Is.True);
        Assert.That(result.RawImageDataPassedThrough, Is.True);
        Assert.That(result.RawImageDataPassThroughReason, Is.Empty);
        Assert.That(flush.Count(), Is.EqualTo(0));
        Assert.That(rewritten.GetInfo().Compression, Is.EqualTo((TiffCompression)65000));
        Assert.That(rewritten.ReadRawStrip(0).Bytes, Is.EqualTo(encodedStrip));
        Assert.That(FindValue(metadata.Values, TiffTagId.ImageDescription)!.Text, Is.EqualTo("rewritten description"));
        Assert.That(FindValue(metadata.Values, TiffTagId.Artist)!.Text, Is.EqualTo("metadata editor"));
        Assert.That(FindValue(metadata.Values, TiffTagId.Software), Is.Null);
        Assert.That(FindValue(metadata.Values, CustomMetadataTag)!.UnsignedValues[0], Is.EqualTo(42));
        Assert.That(rewritten.ReadExif().Metadata.HasExif, Is.False);
    }

    [Test]
    public void CopyAndRewriteMetadata_ApplyExplicitUnknownMetadataPolicy()
    {
        TiffImage source = OpenSourceImage(new byte[] { 1, 2, 3 }, false, false);

        TiffMetadataCopyResult ordinaryCopy = TiffApi.Copy.CopyMetadata(source);
        TiffMetadataCopyResult preservingCopy = TiffApi.Copy.CopyMetadataPreservingUnknown(source);
        TiffWriter destination = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        TiffMetadataRewriteOptions options = new TiffMetadataRewriteOptions();
        options.PreserveUnknownMetadata = false;
        TiffMetadataRewriteResult rewrite = TiffApi.Copy.RewriteImageMetadata(source, destination, options);
        destination.Flush();
        TiffReadOptions readOptions = new TiffReadOptions();
        readOptions.PreserveUnknownTags = true;
        TiffMetadataCopyResult rewrittenMetadata = TiffApi.Copy.CopyMetadata(TiffFile.OpenRead(destination.GetFileBytes(), readOptions).GetImage(0));

        Assert.That(FindValue(ordinaryCopy.Values, CustomMetadataTag), Is.Null);
        Assert.That(FindValue(preservingCopy.Values, CustomMetadataTag)!.UnsignedValues[0], Is.EqualTo(42));
        Assert.That(rewrite.Success, Is.True);
        Assert.That(rewrite.UnknownMetadataPreserved, Is.False);
        Assert.That(FindValue(rewrittenMetadata.Values, CustomMetadataTag), Is.Null);
        Assert.That(FindValue(rewrittenMetadata.Values, TiffTagId.ImageDescription), Is.Not.Null);
    }

    [Test]
    public void RewriteImageMetadata_ReportsWhyRawDataCannotBePassedThrough()
    {
        TiffImage source = OpenSourceImage(new byte[] { 5, 6, 7 }, true, false);
        TiffWriteOptions bigEndianOptions = new TiffWriteOptions();
        bigEndianOptions.ByteOrder = TiffByteOrder.BigEndian;
        TiffWriter bigEndianDestination = TiffApi.Factory.CreateWriter(bigEndianOptions);

        TiffMetadataRewriteResult byteOrderResult = TiffApi.Copy.RewriteImageMetadata(source, bigEndianDestination, new TiffMetadataRewriteOptions());
        TiffWriter metadataDestination = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        TiffMetadataRewriteResult metadataOnlyResult = TiffApi.Copy.RewriteImageMetadata(OpenSourceImage(new byte[] { 5, 6, 7 }, true, true), metadataDestination, new TiffMetadataRewriteOptions());

        Assert.That(byteOrderResult.Success, Is.False);
        Assert.That(byteOrderResult.DestinationImageIndex, Is.EqualTo(-1));
        Assert.That(byteOrderResult.CanPassThroughRawImageData, Is.False);
        Assert.That(byteOrderResult.RawImageDataPassedThrough, Is.False);
        Assert.That(byteOrderResult.RawImageDataPassThroughReason, Does.Contain("byte orders differ"));
        Assert.That(metadataOnlyResult.Success, Is.False);
        Assert.That(metadataOnlyResult.CanPassThroughRawImageData, Is.False);
        Assert.That(metadataOnlyResult.RawImageDataPassThroughReason, Does.Contain("payload reads are disabled"));
    }

    [Test]
    public void WriterMetadataUpdatesAndRemovals_AreValidatedAtomically()
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddImage(TiffApi.Data.CreateImageDefinition(CreateSourceTags()));
        TiffDiagnostics update = writer.UpdateMetadata(imageIndex, new TiffTagValue[]
        {
            TiffApi.Data.CreateAsciiTag(TiffTagId.ImageDescription, "updated"),
            TiffApi.Data.CreateAsciiTag(TiffTagId.Artist, "artist")
        });
        TiffDiagnostics removal = writer.RemoveMetadata(imageIndex, new int[] { TiffTagId.Software, CustomMetadataTag });

        TiffTagValue? descriptionBeforeInvalidBatch = FindValue(writer.Images[imageIndex].Definition.Tags, TiffTagId.ImageDescription);
        TiffDiagnostics invalid = writer.UpdateMetadata(imageIndex, new TiffTagValue[]
        {
            TiffApi.Data.CreateAsciiTag(TiffTagId.ImageDescription, "must not be applied"),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageWidth, TiffDataType.Long, 99)
        });

        Assert.That(update.Count(), Is.EqualTo(0));
        Assert.That(removal.Count(), Is.EqualTo(0));
        Assert.That(descriptionBeforeInvalidBatch!.Text, Is.EqualTo("updated"));
        Assert.That(FindValue(writer.Images[imageIndex].Definition.Tags, TiffTagId.Software), Is.Null);
        Assert.That(FindValue(writer.Images[imageIndex].Definition.Tags, CustomMetadataTag), Is.Null);
        Assert.That(invalid.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
        Assert.That(FindValue(writer.Images[imageIndex].Definition.Tags, TiffTagId.ImageDescription)!.Text, Is.EqualTo("updated"));
        Assert.That(FindValue(writer.Images[imageIndex].Definition.Tags, TiffTagId.ImageWidth)!.UnsignedValues[0], Is.EqualTo(2));
    }

    static TiffImage OpenSourceImage(byte[] encodedStrip, bool preserveUnknownTags, bool metadataOnly)
    {
        TiffWriteOptions writeOptions = new TiffWriteOptions();
        writeOptions.UnsupportedFeaturePolicy = TiffUnsupportedFeaturePolicy.PreserveRaw;
        TiffWriter writer = TiffApi.Factory.CreateWriter(writeOptions);
        int imageIndex = writer.AddImage(TiffApi.Data.CreateImageDefinition(CreateSourceTags()));
        writer.WriteRawStrip(imageIndex, 0, encodedStrip);
        TiffExifMetadata exif = new TiffExifMetadata();
        exif.HasExif = true;
        exif.Values = new TiffTagValue[] { TiffApi.Data.CreateAsciiTag(DateTimeOriginalTag, "2026:08:26 10:00:00") };
        writer.WriteExif(imageIndex, exif);
        writer.Flush();
        TiffReadOptions readOptions = new TiffReadOptions();
        readOptions.PreserveUnknownTags = preserveUnknownTags;
        if (metadataOnly)
        {
            return TiffFile.OpenMetadata(writer.GetFileBytes(), readOptions).GetImage(0);
        }

        return TiffFile.OpenRead(writer.GetFileBytes(), readOptions).GetImage(0);
    }

    static TiffTagValue[] CreateSourceTags()
    {
        return new TiffTagValue[]
        {
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageWidth, TiffDataType.Long, 2),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageLength, TiffDataType.Long, 2),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.Compression, TiffDataType.Short, 65000),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.RowsPerStrip, TiffDataType.Long, 2),
            TiffApi.Data.CreateAsciiTag(TiffTagId.ImageDescription, "source description"),
            TiffApi.Data.CreateAsciiTag(TiffTagId.Software, "source software"),
            TiffApi.Data.CreateUnsignedScalarTag(CustomMetadataTag, TiffDataType.Long, 42)
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
