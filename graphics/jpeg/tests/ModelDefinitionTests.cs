using System.Reflection;

namespace jpeg.Tests;

internal class ModelDefinitionTests
{
    [Test]
    public void DataClasses_UseFieldsOnly()
    {
        Type[] types = new Type[]
        {
            typeof(global::jpeg.JpegImage),
            typeof(global::jpeg.JpegInfo),
            typeof(global::jpeg.JpegCoefficientImage),
            typeof(global::jpeg.JpegCoefficientComponent),
            typeof(global::jpeg.JpegFrameHeader),
            typeof(global::jpeg.JpegFrameComponent),
            typeof(global::jpeg.JpegScanHeader),
            typeof(global::jpeg.JpegScanComponent),
            typeof(global::jpeg.JpegTableStore),
            typeof(global::jpeg.JpegJfifMetadata),
            typeof(global::jpeg.JpegJfxxMetadata),
            typeof(global::jpeg.JpegAdobeMetadata),
            typeof(global::jpeg.JpegExifMetadata),
            typeof(global::jpeg.JpegExifTiffHeader),
            typeof(global::jpeg.JpegXmpMetadata),
            typeof(global::jpeg.JpegExtendedXmpChunk),
            typeof(global::jpeg.JpegMpfMetadata),
            typeof(global::jpeg.JpegPhotoshopMetadata),
            typeof(global::jpeg.JpegPhotoshopResourceBlock),
            typeof(global::jpeg.JpegExifIfdEntry),
            typeof(global::jpeg.JpegIccProfileChunk),
            typeof(global::jpeg.JpegIccProfile),
            typeof(global::jpeg.JpegIccTag),
            typeof(global::jpeg.JpegIccMatrixTrcProfile),
            typeof(global::jpeg.JpegIccPolicyResult),
            typeof(global::jpeg.JpegXyzColor),
            typeof(global::jpeg.JpegDiagnostic),
            typeof(global::jpeg.JpegDiagnosticCollection),
            typeof(global::jpeg.JpegQuantizationTable),
            typeof(global::jpeg.JpegHuffmanTable),
            typeof(global::jpeg.JpegArithmeticConditioningTable),
            typeof(global::jpeg.JpegMcuLayout),
            typeof(global::jpeg.JpegMcuComponentLayout),
            typeof(global::jpeg.JpegPlanarComponentBuffer),
            typeof(global::jpeg.JpegPlanarImageBuffer),
            typeof(global::jpeg.JpegInterleavedPixelBuffer),
            typeof(global::jpeg.JpegMetadata),
            typeof(global::jpeg.JpegMetadataSegment),
            typeof(global::jpeg.JpegRawMarker),
            typeof(global::jpeg.JpegRawMarkerCollection),
            typeof(global::jpeg.JpegReadOptions),
            typeof(global::jpeg.JpegWriteOptions)
        };

        foreach (Type type in types)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Assert.That(type.GetProperties(flags), Is.Empty, type.FullName);
            Assert.That(type.GetFields(flags), Is.Not.Empty, type.FullName);
        }
    }

    [Test]
    public void ImageInfoAndMetadata_DefaultsHaveUsableContainers()
    {
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        global::jpeg.JpegInfo info = new global::jpeg.JpegInfo();
        global::jpeg.JpegCoefficientImage coefficientImage = new global::jpeg.JpegCoefficientImage();
        global::jpeg.JpegCoefficientComponent coefficientComponent = new global::jpeg.JpegCoefficientComponent();
        global::jpeg.JpegMetadata metadata = new global::jpeg.JpegMetadata();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        global::jpeg.JpegMetadataSegment segment = new global::jpeg.JpegMetadataSegment();
        global::jpeg.JpegJfxxMetadata jfxx = new global::jpeg.JpegJfxxMetadata();
        global::jpeg.JpegDiagnostic diagnostic = new global::jpeg.JpegDiagnostic();

        Assert.That(image.Pixels, Is.Not.Null);
        Assert.That(image.Metadata, Is.Not.Null);
        Assert.That(image.Diagnostics, Is.Not.Null);
        Assert.That(image.DiagnosticCollection, Is.Not.Null);
        Assert.That(info.Metadata, Is.Not.Null);
        Assert.That(info.Frame, Is.Not.Null);
        Assert.That(info.Components, Is.Not.Null);
        Assert.That(info.LastScanHeader, Is.Not.Null);
        Assert.That(info.ScanHeaders, Is.Not.Null);
        Assert.That(info.Diagnostics, Is.Not.Null);
        Assert.That(info.DiagnosticCollection, Is.Not.Null);
        Assert.That(info.QuantizationTables, Is.Not.Null);
        Assert.That(info.HuffmanTables, Is.Not.Null);
        Assert.That(info.ArithmeticConditioningTables, Is.Not.Null);
        Assert.That(info.HasHierarchicalProgression, Is.False);
        Assert.That(info.IsJpegLs, Is.False);
        Assert.That(info.HasSmartScale, Is.False);
        Assert.That(coefficientImage.Frame, Is.Not.Null);
        Assert.That(coefficientImage.ScanHeaders, Is.Not.Null);
        Assert.That(coefficientImage.QuantizationTables, Is.Not.Null);
        Assert.That(coefficientImage.HuffmanTables, Is.Not.Null);
        Assert.That(coefficientImage.Metadata, Is.Not.Null);
        Assert.That(coefficientImage.Diagnostics, Is.Not.Null);
        Assert.That(coefficientImage.DiagnosticCollection, Is.Not.Null);
        Assert.That(coefficientImage.Components, Is.Not.Null);
        Assert.That(coefficientComponent.Coefficients, Is.Not.Null);
        Assert.That(options.TableStore, Is.Not.Null);
        Assert.That(options.TableStore.QuantizationTables, Is.Not.Null);
        Assert.That(options.TableStore.HuffmanTables, Is.Not.Null);
        Assert.That(options.TableStore.ArithmeticConditioningTables, Is.Not.Null);
        Assert.That(metadata.Jfif, Is.Not.Null);
        Assert.That(metadata.Jfxx, Is.Not.Null);
        Assert.That(metadata.Adobe, Is.Not.Null);
        Assert.That(metadata.ExifMetadata, Is.Not.Null);
        Assert.That(metadata.XmpMetadata, Is.Not.Null);
        Assert.That(metadata.MpfMetadata, Is.Not.Null);
        Assert.That(metadata.Photoshop, Is.Not.Null);
        Assert.That(metadata.Icc, Is.Not.Null);
        Assert.That(metadata.IccProfileChunks, Is.Not.Null);
        Assert.That(metadata.ExtendedXmpChunks, Is.Not.Null);
        Assert.That(metadata.Jfif.ThumbnailData, Is.Not.Null);
        Assert.That(metadata.Jfif.RgbThumbnailPixels, Is.Not.Null);
        Assert.That(jfxx.JpegThumbnailData, Is.Not.Null);
        Assert.That(jfxx.RgbThumbnailPixels, Is.Not.Null);
        Assert.That(jfxx.IndexedThumbnailPalette, Is.Not.Null);
        Assert.That(jfxx.IndexedThumbnailPixels, Is.Not.Null);
        Assert.That(jfxx.ExtensionData, Is.Not.Null);
        Assert.That(metadata.Exif, Is.Not.Null);
        Assert.That(metadata.ExifMetadata.RawPayload, Is.Not.Null);
        Assert.That(metadata.ExifMetadata.TiffData, Is.Not.Null);
        Assert.That(metadata.ExifMetadata.TiffHeader, Is.Not.Null);
        Assert.That(metadata.ExifMetadata.DateTime, Is.Not.Null);
        Assert.That(metadata.ExifMetadata.DateTimeOriginal, Is.Not.Null);
        Assert.That(metadata.ExifMetadata.Make, Is.Not.Null);
        Assert.That(metadata.ExifMetadata.Model, Is.Not.Null);
        Assert.That(metadata.ExifMetadata.Software, Is.Not.Null);
        Assert.That(metadata.ExifMetadata.LensModel, Is.Not.Null);
        Assert.That(metadata.ExifMetadata.GpsLatitudeRef, Is.Not.Null);
        Assert.That(metadata.ExifMetadata.GpsLongitudeRef, Is.Not.Null);
        Assert.That(metadata.ExifMetadata.JpegThumbnailData, Is.Not.Null);
        Assert.That(metadata.ExifMetadata.RgbThumbnailPixels, Is.Not.Null);
        Assert.That(metadata.ExifMetadata.YCbCrThumbnailPixels, Is.Not.Null);
        Assert.That(metadata.Xmp, Is.Not.Null);
        Assert.That(metadata.XmpMetadata.RawPayload, Is.Not.Null);
        Assert.That(metadata.XmpMetadata.PacketData, Is.Not.Null);
        Assert.That(metadata.XmpMetadata.Packet, Is.Not.Null);
        Assert.That(metadata.ExtendedXmp, Is.Not.Null);
        Assert.That(new global::jpeg.JpegExtendedXmpChunk().Guid, Is.Not.Null);
        Assert.That(new global::jpeg.JpegExtendedXmpChunk().Data, Is.Not.Null);
        Assert.That(metadata.Mpf, Is.Not.Null);
        Assert.That(metadata.MpfMetadata.RawPayload, Is.Not.Null);
        Assert.That(metadata.MpfMetadata.TiffData, Is.Not.Null);
        Assert.That(metadata.Iptc, Is.Not.Null);
        Assert.That(metadata.Photoshop.RawPayload, Is.Not.Null);
        Assert.That(metadata.Photoshop.ResourceBlocks, Is.Not.Null);
        Assert.That(metadata.Photoshop.IptcData, Is.Not.Null);
        Assert.That(new global::jpeg.JpegPhotoshopResourceBlock().Signature, Is.Not.Null);
        Assert.That(new global::jpeg.JpegPhotoshopResourceBlock().Name, Is.Not.Null);
        Assert.That(new global::jpeg.JpegPhotoshopResourceBlock().Data, Is.Not.Null);
        Assert.That(metadata.IccProfile, Is.Not.Null);
        Assert.That(metadata.Icc.CmmType, Is.Not.Null);
        Assert.That(metadata.Icc.ProfileClass, Is.Not.Null);
        Assert.That(metadata.Icc.Tags, Is.Not.Null);
        Assert.That(new global::jpeg.JpegIccTag().Signature, Is.Not.Null);
        Assert.That(new global::jpeg.JpegIccMatrixTrcProfile(), Is.Not.Null);
        Assert.That(new global::jpeg.JpegIccPolicyResult().PreservedProfile, Is.Not.Null);
        Assert.That(new global::jpeg.JpegIccPolicyResult().Icc, Is.Not.Null);
        Assert.That(new global::jpeg.JpegIccPolicyResult().MatrixTrcProfile, Is.Not.Null);
        Assert.That(new global::jpeg.JpegIccProfileChunk().Data, Is.Not.Null);
        Assert.That(metadata.Comments, Is.Not.Null);
        Assert.That(metadata.Segments, Is.Not.Null);
        Assert.That(metadata.RawMarkers, Is.Not.Null);
        Assert.That(segment.Data, Is.Not.Null);
        Assert.That(diagnostic.Message, Is.Not.Null);
    }

    [Test]
    public void DataClasses_CarryExpectedJpegFields()
    {
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = 16;
        image.Height = 8;
        image.Precision = 8;
        image.ComponentCount = 3;
        image.PixelFormat = global::jpeg.JpegPixelFormat.Rgb24;
        image.ColorSpace = global::jpeg.JpegColorSpace.YCbCr;
        image.Subsampling = global::jpeg.JpegSubsampling.Ratio420;
        image.Pixels = new byte[] { 1, 2, 3 };

        global::jpeg.JpegInfo info = new global::jpeg.JpegInfo();
        info.Width = image.Width;
        info.Height = image.Height;
        info.DefinedNumberOfLines = image.Height;
        info.HasDefinedNumberOfLines = true;
        info.Process = global::jpeg.JpegProcess.BaselineDct;
        info.Progressive = false;
        info.ArithmeticCoding = false;
        info.IsJpegLs = true;
        info.HasSmartScale = true;
        info.IsAbbreviatedTableStream = true;
        info.IsAbbreviatedImageStream = true;
        info.UsedSuppliedTables = true;
        info.MetadataBytes = 12;
        info.HasHierarchicalProgression = true;
        info.HierarchicalProgressionMarkerCount = 1;
        info.ExpandReferenceComponentMarkerCount = 1;
        info.JpegLsMarkerCount = 1;
        info.SmartScaleMarkerCount = 1;
        info.RestartInterval = 4;
        info.RestartMarkerCount = 2;
        info.LastRestartMarkerIndex = 1;
        info.ExpectedRestartMarkerIndex = 2;

        global::jpeg.JpegDiagnostic diagnostic = new global::jpeg.JpegDiagnostic();
        diagnostic.Severity = global::jpeg.JpegDiagnosticSeverity.Warning;
        diagnostic.Code = global::jpeg.JpegDiagnosticCode.CorruptMetadata;
        diagnostic.Marker = global::jpeg.JpegMarker.App2;
        diagnostic.Offset = 32;
        diagnostic.Message = "ignored corrupt ICC chunk";
        info.Diagnostics.Add(diagnostic);

        Assert.That(image.Width, Is.EqualTo(16));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.Pixels.Length, Is.EqualTo(3));
        Assert.That(info.HasDefinedNumberOfLines, Is.True);
        Assert.That(info.DefinedNumberOfLines, Is.EqualTo(8));
        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.BaselineDct));
        Assert.That(info.IsAbbreviatedTableStream, Is.True);
        Assert.That(info.IsAbbreviatedImageStream, Is.True);
        Assert.That(info.UsedSuppliedTables, Is.True);
        Assert.That(info.IsJpegLs, Is.True);
        Assert.That(info.HasSmartScale, Is.True);
        Assert.That(info.HasHierarchicalProgression, Is.True);
        Assert.That(info.HierarchicalProgressionMarkerCount, Is.EqualTo(1));
        Assert.That(info.ExpandReferenceComponentMarkerCount, Is.EqualTo(1));
        Assert.That(info.JpegLsMarkerCount, Is.EqualTo(1));
        Assert.That(info.SmartScaleMarkerCount, Is.EqualTo(1));
        Assert.That(info.RestartInterval, Is.EqualTo(4));
        Assert.That(info.LastRestartMarkerIndex, Is.EqualTo(1));
        Assert.That(info.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(info.Diagnostics[0].Code, Is.EqualTo(global::jpeg.JpegDiagnosticCode.CorruptMetadata));
    }

    [Test]
    public void OptionClasses_UseExplicitPolicyDefaults()
    {
        global::jpeg.JpegReadOptions readOptions = new global::jpeg.JpegReadOptions();
        global::jpeg.JpegWriteOptions writeOptions = new global::jpeg.JpegWriteOptions();

        Assert.That(readOptions.MetadataPolicy, Is.EqualTo(global::jpeg.JpegMetadataPolicy.PreserveAll));
        Assert.That(readOptions.ColorPolicy, Is.EqualTo(global::jpeg.JpegColorPolicy.PreserveSource));
        Assert.That(readOptions.IccBehaviorPolicy, Is.EqualTo(global::jpeg.JpegIccBehaviorPolicy.Preserve));
        Assert.That(readOptions.ErrorPolicy, Is.EqualTo(global::jpeg.JpegErrorPolicy.Strict));
        Assert.That(readOptions.OutputPixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Unknown));
        Assert.That(readOptions.InverseDctMethod, Is.EqualTo(global::jpeg.JpegInverseDctMethod.Accurate));
        Assert.That(readOptions.IdctScaleNumerator, Is.EqualTo(1));
        Assert.That(readOptions.IdctScaleDenominator, Is.EqualTo(1));
        Assert.That(readOptions.AutoOrient, Is.False);

        Assert.That(writeOptions.MetadataPolicy, Is.EqualTo(global::jpeg.JpegMetadataPolicy.PreserveAll));
        Assert.That(writeOptions.ColorPolicy, Is.EqualTo(global::jpeg.JpegColorPolicy.PreserveSource));
        Assert.That(writeOptions.IccBehaviorPolicy, Is.EqualTo(global::jpeg.JpegIccBehaviorPolicy.Preserve));
        Assert.That(writeOptions.ErrorPolicy, Is.EqualTo(global::jpeg.JpegErrorPolicy.Strict));
        Assert.That(writeOptions.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Unknown));
        Assert.That(writeOptions.ChromaDownsamplingFilter, Is.EqualTo(global::jpeg.JpegChromaDownsamplingFilter.Box));
    }
}
