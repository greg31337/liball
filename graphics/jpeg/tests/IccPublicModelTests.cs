namespace jpeg.Tests;

internal class IccPublicModelTests
{
    [Test]
    public void Parser_ExposesIndependentTagPayloadsAndTypedRenderingIntent()
    {
        byte[] profile = IccMatrixTrcConverterTests.CreateRgbMatrixTrcProfile();
        global::jpeg.JpegByteWriter.WriteUInt32(profile, 64, 2);
        int firstTagOffset = IccMatrixTrcConverterTests.FindTagDataOffset(profile, "rXYZ");
        byte[] expectedPayload = new byte[20];
        System.Array.Copy(profile, firstTagOffset, expectedPayload, 0, expectedPayload.Length);

        global::jpeg.JpegIccProfile parsed = new global::jpeg.JpegIccProfileParser().Parse(profile);
        profile[firstTagOffset] = 0;

        Assert.That(parsed.RenderingIntent, Is.EqualTo(2));
        Assert.That(parsed.RenderingIntentKind, Is.EqualTo(global::jpeg.JpegIccRenderingIntent.Saturation));
        Assert.That(parsed.Tags[0].Signature, Is.EqualTo("rXYZ"));
        Assert.That(parsed.Tags[0].TypeSignature, Is.EqualTo("XYZ "));
        Assert.That(parsed.Tags[0].Payload, Is.EqualTo(expectedPayload));
    }

    [Test]
    public void Identifier_PopulatesSupportedRgbIccConversionDescription()
    {
        byte[] profile = IccMatrixTrcConverterTests.CreateRgbMatrixTrcProfile();
        byte[] jpeg = CreateIdentifySource(profile);

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(jpeg);

        Assert.That(info.Metadata.HasIccProfile, Is.True);
        Assert.That(info.Metadata.Icc.IsPresent, Is.True);
        Assert.That(info.Metadata.Icc.Tags.Count, Is.EqualTo(7));
        Assert.That(info.Metadata.Icc.Conversion.Status, Is.EqualTo(global::jpeg.JpegIccConversionStatus.Supported));
        Assert.That(info.Metadata.Icc.Conversion.SourceColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.RGB));
        Assert.That(info.Metadata.Icc.Conversion.DestinationColorSpace, Is.EqualTo(global::jpeg.JpegIccDestinationColorSpace.Xyz));
        Assert.That(info.Metadata.Icc.Conversion.RenderingIntent, Is.EqualTo(global::jpeg.JpegIccRenderingIntent.Perceptual));
        Assert.That(info.Metadata.Icc.Conversion.MatrixTrcProfile.IsRgb, Is.True);
        Assert.That(info.Metadata.Icc.Conversion.MatrixTrcProfile.SourceColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.RGB));
        Assert.That(info.Metadata.Icc.Conversion.MatrixTrcProfile.DestinationColorSpace, Is.EqualTo(global::jpeg.JpegIccDestinationColorSpace.Xyz));
        Assert.That(info.Metadata.Icc.Conversion.Diagnostics, Is.Empty);
    }

    [Test]
    public void Parser_DescribesSupportedGrayAndCmykConversions()
    {
        global::jpeg.JpegIccProfile gray = new global::jpeg.JpegIccProfileParser().Parse(IccMatrixTrcConverterTests.CreateGrayMatrixTrcProfile());
        global::jpeg.JpegIccProfile cmyk = new global::jpeg.JpegIccProfileParser().Parse(IccMatrixTrcConverterTests.CreateCmykMatrixTrcProfile());

        Assert.That(gray.Conversion.Status, Is.EqualTo(global::jpeg.JpegIccConversionStatus.Supported));
        Assert.That(gray.Conversion.SourceColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.Grayscale));
        Assert.That(gray.Conversion.DestinationColorSpace, Is.EqualTo(global::jpeg.JpegIccDestinationColorSpace.Xyz));
        Assert.That(gray.Conversion.MatrixTrcProfile.IsGray, Is.True);
        Assert.That(gray.Conversion.MatrixTrcProfile.GrayGamma, Is.EqualTo(1.0));
        Assert.That(cmyk.Conversion.Status, Is.EqualTo(global::jpeg.JpegIccConversionStatus.Supported));
        Assert.That(cmyk.Conversion.SourceColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.CMYK));
        Assert.That(cmyk.Conversion.DestinationColorSpace, Is.EqualTo(global::jpeg.JpegIccDestinationColorSpace.Xyz));
        Assert.That(cmyk.Conversion.MatrixTrcProfile.IsCmyk, Is.True);
        Assert.That(cmyk.Conversion.MatrixTrcProfile.BlackGamma, Is.EqualTo(1.0));
    }

    [Test]
    public void Parser_ReportsUnsupportedTagsAndInvalidRenderingIntent()
    {
        byte[] unsupportedProfile = IccProfileParserTests.CreateIccProfileWithOneTag();
        global::jpeg.JpegIccProfile unsupported = new global::jpeg.JpegIccProfileParser().Parse(unsupportedProfile);
        byte[] unsupportedCurveProfile = IccMatrixTrcConverterTests.CreateRgbMatrixTrcProfile();
        int redCurveOffset = IccMatrixTrcConverterTests.FindTagDataOffset(unsupportedCurveProfile, "rTRC");
        global::jpeg.JpegByteWriter.WriteUInt32(unsupportedCurveProfile, redCurveOffset + 8, 2);
        global::jpeg.JpegIccProfile unsupportedCurve = new global::jpeg.JpegIccProfileParser().Parse(unsupportedCurveProfile);
        byte[] unsupportedColorProfile = IccProfileParserTests.CreateIccProfileWithOneTag();
        IccProfileParserTests.WriteSignature(unsupportedColorProfile, 16, "Lab ");
        global::jpeg.JpegIccProfile unsupportedColor = new global::jpeg.JpegIccProfileParser().Parse(unsupportedColorProfile);
        byte[] invalidIntentProfile = IccMatrixTrcConverterTests.CreateRgbMatrixTrcProfile();
        global::jpeg.JpegByteWriter.WriteUInt32(invalidIntentProfile, 64, 9);
        global::jpeg.JpegIccProfile invalidIntent = new global::jpeg.JpegIccProfileParser().Parse(invalidIntentProfile);

        Assert.That(unsupported.Conversion.Status, Is.EqualTo(global::jpeg.JpegIccConversionStatus.Unsupported));
        Assert.That(unsupported.Conversion.DestinationColorSpace, Is.EqualTo(global::jpeg.JpegIccDestinationColorSpace.Xyz));
        Assert.That(unsupported.Conversion.Diagnostics[0].Code, Is.EqualTo(global::jpeg.JpegIccConversionDiagnosticCode.MissingRequiredTag));
        Assert.That(unsupported.Conversion.Diagnostics[0].TagSignature, Is.EqualTo("rXYZ"));
        Assert.That(unsupported.Conversion.Message, Is.Not.Empty);
        Assert.That(unsupportedCurve.Conversion.Status, Is.EqualTo(global::jpeg.JpegIccConversionStatus.Unsupported));
        Assert.That(unsupportedCurve.Conversion.Diagnostics[0].Code, Is.EqualTo(global::jpeg.JpegIccConversionDiagnosticCode.UnsupportedToneCurve));
        Assert.That(unsupportedCurve.Conversion.Diagnostics[0].TagSignature, Is.EqualTo("rTRC"));
        Assert.That(unsupportedColor.Conversion.SourceColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.Unknown));
        Assert.That(unsupportedColor.Conversion.DestinationColorSpace, Is.EqualTo(global::jpeg.JpegIccDestinationColorSpace.Unknown));
        Assert.That(unsupportedColor.Conversion.Diagnostics[0].Code, Is.EqualTo(global::jpeg.JpegIccConversionDiagnosticCode.UnsupportedColorSpace));
        Assert.That(invalidIntent.RenderingIntentKind, Is.EqualTo(global::jpeg.JpegIccRenderingIntent.Unknown));
        Assert.That(invalidIntent.Conversion.Status, Is.EqualTo(global::jpeg.JpegIccConversionStatus.Unsupported));
        Assert.That(invalidIntent.Conversion.DestinationColorSpace, Is.EqualTo(global::jpeg.JpegIccDestinationColorSpace.Xyz));
        Assert.That(invalidIntent.Conversion.MatrixTrcProfile.IsRgb, Is.True);
        Assert.That(invalidIntent.Conversion.Diagnostics[0].Code, Is.EqualTo(global::jpeg.JpegIccConversionDiagnosticCode.InvalidRenderingIntent));
    }

    public static byte[] CreateIdentifySource(byte[] profile)
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App2, IccProfileChunkParserTests.CreateIccPayload(1, 1, profile));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();
        return writer.ToArray();
    }
}
