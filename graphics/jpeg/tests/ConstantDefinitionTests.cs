using System.Reflection;

namespace jpeg.Tests;

internal class ConstantDefinitionTests
{
    [Test]
    public void MarkerConstants_MatchPublicMarkerEnums()
    {
        Assert.That(global::jpeg.JpegConstants.MarkerStartOfImage, Is.EqualTo((int)global::jpeg.JpegMarker.StartOfImage));
        Assert.That(global::jpeg.JpegConstants.MarkerEndOfImage, Is.EqualTo((int)global::jpeg.JpegMarker.EndOfImage));
        Assert.That(global::jpeg.JpegConstants.MarkerStartOfScan, Is.EqualTo((int)global::jpeg.JpegMarker.StartOfScan));
        Assert.That(global::jpeg.JpegConstants.MarkerDefineQuantizationTable, Is.EqualTo((int)global::jpeg.JpegMarker.DefineQuantizationTable));
        Assert.That(global::jpeg.JpegConstants.MarkerDefineHuffmanTable, Is.EqualTo((int)global::jpeg.JpegMarker.DefineHuffmanTable));
        Assert.That(global::jpeg.JpegConstants.MarkerDefineRestartInterval, Is.EqualTo((int)global::jpeg.JpegMarker.DefineRestartInterval));
        Assert.That(global::jpeg.JpegConstants.MarkerApp0, Is.EqualTo((int)global::jpeg.JpegMarker.App0));
        Assert.That(global::jpeg.JpegConstants.MarkerApp15, Is.EqualTo((int)global::jpeg.JpegMarker.App15));
        Assert.That(global::jpeg.JpegConstants.MarkerJpegLs, Is.EqualTo((int)global::jpeg.JpegMarker.JpegLs));
        Assert.That(global::jpeg.JpegConstants.MarkerComment, Is.EqualTo((int)global::jpeg.JpegMarker.Comment));
    }

    [Test]
    public void NumericConstants_AreCompileTimeConstantsWherePossible()
    {
        BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        FieldInfo? markerField = typeof(global::jpeg.JpegConstants).GetField("MarkerStartOfImage", flags);
        FieldInfo? payloadField = typeof(global::jpeg.JpegConstants).GetField("MaximumMarkerPayloadLength", flags);
        FieldInfo? qualityField = typeof(global::jpeg.JpegConstants).GetField("DefaultQuality", flags);
        FieldInfo? zigZagField = typeof(global::jpeg.JpegConstants).GetField("ZigZagOrder", flags);

        Assert.That(markerField, Is.Not.Null);
        Assert.That(markerField?.IsLiteral, Is.True);
        Assert.That(payloadField, Is.Not.Null);
        Assert.That(payloadField?.IsLiteral, Is.True);
        Assert.That(qualityField, Is.Not.Null);
        Assert.That(qualityField?.IsLiteral, Is.True);
        Assert.That(zigZagField, Is.Not.Null);
        Assert.That(zigZagField?.IsStatic, Is.True);
        Assert.That(zigZagField?.IsLiteral, Is.False);
    }

    [Test]
    public void MarkerPayloadLimit_AccountsForLengthBytes()
    {
        Assert.That(global::jpeg.JpegConstants.MaximumMarkerSegmentLength, Is.EqualTo(65535));
        Assert.That(global::jpeg.JpegConstants.MarkerSegmentLengthBytes, Is.EqualTo(2));
        Assert.That(global::jpeg.JpegConstants.ExifIdentifierLength, Is.EqualTo(6));
        Assert.That(global::jpeg.JpegConstants.XmpIdentifierLength, Is.EqualTo(29));
        Assert.That(global::jpeg.JpegConstants.ExtendedXmpIdentifierLength, Is.EqualTo(35));
        Assert.That(global::jpeg.JpegConstants.ExtendedXmpChunkHeaderLength, Is.EqualTo(75));
        Assert.That(global::jpeg.JpegConstants.MpfIdentifierLength, Is.EqualTo(4));
        Assert.That(global::jpeg.JpegConstants.PhotoshopIdentifierLength, Is.EqualTo(14));
        Assert.That(global::jpeg.JpegConstants.PhotoshopIptcResourceId, Is.EqualTo(0x0404));
        Assert.That(
            global::jpeg.JpegConstants.MaximumMarkerPayloadLength,
            Is.EqualTo(global::jpeg.JpegConstants.MaximumMarkerSegmentLength - global::jpeg.JpegConstants.MarkerSegmentLengthBytes));
    }

    [Test]
    public void ZigZagOrder_ContainsStandardEightByEightOrder()
    {
        int[] expected = new int[]
        {
            0, 1, 8, 16, 9, 2, 3, 10,
            17, 24, 32, 25, 18, 11, 4, 5,
            12, 19, 26, 33, 40, 48, 41, 34,
            27, 20, 13, 6, 7, 14, 21, 28,
            35, 42, 49, 56, 57, 50, 43, 36,
            29, 22, 15, 23, 30, 37, 44, 51,
            58, 59, 52, 45, 38, 31, 39, 46,
            53, 60, 61, 54, 47, 55, 62, 63
        };

        Assert.That(global::jpeg.JpegConstants.BlockWidth, Is.EqualTo(8));
        Assert.That(global::jpeg.JpegConstants.BlockHeight, Is.EqualTo(8));
        Assert.That(global::jpeg.JpegConstants.BlockSampleCount, Is.EqualTo(64));
        Assert.That(global::jpeg.JpegConstants.ZigZagOrder, Is.EqualTo(expected));
        Assert.That(global::jpeg.JpegConstants.ZigZagOrder.Distinct().Count(), Is.EqualTo(64));
    }

    [Test]
    public void DefaultLimitsAndQuality_AreAvailableToOptions()
    {
        global::jpeg.JpegReadOptions readOptions = new global::jpeg.JpegReadOptions();
        global::jpeg.JpegWriteOptions writeOptions = new global::jpeg.JpegWriteOptions();

        Assert.That(global::jpeg.JpegConstants.DefaultQuality, Is.EqualTo(75));
        Assert.That(readOptions.MaxWidth, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxWidth));
        Assert.That(readOptions.MaxHeight, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxHeight));
        Assert.That(readOptions.MaxComponents, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxComponents));
        Assert.That(readOptions.MaxScans, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxScans));
        Assert.That(readOptions.MaxMetadataBytes, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxMetadataBytes));
        Assert.That(readOptions.MaxEntropyBytes, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxEntropyBytes));
        Assert.That(readOptions.MaxOutputPixels, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxOutputPixels));
        Assert.That(readOptions.MaxOutputBytes, Is.EqualTo(global::jpeg.JpegConstants.DefaultMaxOutputBytes));
        Assert.That(writeOptions.Quality, Is.EqualTo(global::jpeg.JpegConstants.DefaultQuality));
    }
}
