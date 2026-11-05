namespace jpeg.Tests;

internal class ColorHintPrecedenceTests
{
    [Test]
    public void IccParser_ExposesValidAndMalformedCicpTags()
    {
        byte[] validProfile = CreateCicpProfile(9, 16, 0, 1);
        byte[] malformedProfile = CreateCicpProfile(1, 13, 1, 2);
        global::jpeg.JpegIccProfileParser parser = new global::jpeg.JpegIccProfileParser();

        global::jpeg.JpegIccProfile valid = parser.Parse(validProfile);
        global::jpeg.JpegIccProfile malformed = parser.Parse(malformedProfile);
        validProfile[152] = 1;

        Assert.That(valid.HasCicp, Is.True);
        Assert.That(valid.Cicp.IsPresent, Is.True);
        Assert.That(valid.Cicp.IsValid, Is.True);
        Assert.That(valid.Cicp.ColorPrimaries, Is.EqualTo(9));
        Assert.That(valid.Cicp.TransferCharacteristics, Is.EqualTo(16));
        Assert.That(valid.Cicp.MatrixCoefficients, Is.EqualTo(0));
        Assert.That(valid.Cicp.VideoFullRangeFlag, Is.EqualTo(1));
        Assert.That(valid.Cicp.FullRange, Is.True);
        Assert.That(valid.Cicp.RawPayload.Length, Is.EqualTo(12));
        Assert.That(valid.Cicp.RawPayload[8], Is.EqualTo(9));
        Assert.That(malformed.HasCicp, Is.True);
        Assert.That(malformed.Cicp.IsPresent, Is.True);
        Assert.That(malformed.Cicp.IsValid, Is.False);
        Assert.That(malformed.Cicp.FullRange, Is.False);
    }

    [Test]
    public void Identifier_AppliesAdobeAndIccAheadOfOtherColorHints()
    {
        byte[] profile = CreateCicpProfile(9, 16, 0, 1);
        byte[] sof = CreateRgbSofPayload();
        byte[] sos = CreateRgbSosPayload();
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, JfifMetadataParserTests.CreateJfifPayload(1, 2, 0, 1, 1, 0, 0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App1, ExifMetadataParserTests.CreateExifPayload(ExifTagReaderTests.CreateCommonExifTiff()));
        new global::jpeg.JpegIccProfileChunkWriter().WriteMarkerSegments(writer, profile);
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App14, AdobeMetadataParserTests.CreateAdobePayload(100, 0, 0, global::jpeg.JpegConstants.AdobeTransformYCbCr));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, sof);
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, sos);
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasCicp, Is.True);
        Assert.That(info.Metadata.Cicp.IsValid, Is.True);
        Assert.That(info.ColorHints.HasAdobeColorSpace, Is.True);
        Assert.That(info.ColorHints.AdobeColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCbCr));
        Assert.That(info.ColorHints.HasCicpColorSpace, Is.True);
        Assert.That(info.ColorHints.CicpColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.RGB));
        Assert.That(info.ColorHints.HasJfifColorSpace, Is.True);
        Assert.That(info.ColorHints.HasExifColorSpace, Is.True);
        Assert.That(info.ColorHints.HasComponentIdentifierColorSpace, Is.True);
        Assert.That(info.ColorHints.HasIccColorSpace, Is.True);
        Assert.That(info.ColorHints.ComponentIdentifiers, Is.EqualTo(new int[] { 82, 71, 66 }));
        Assert.That(info.ColorHints.HasConflict, Is.True);
        Assert.That(info.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCbCr));
        Assert.That(info.ColorSpaceSource, Is.EqualTo(global::jpeg.JpegColorHintSource.Adobe));
        Assert.That(info.ColorHints.ColorCharacteristicsSource, Is.EqualTo(global::jpeg.JpegColorHintSource.IccProfile));
    }

    [Test]
    public void Resolver_AppliesCicpJfifAndComponentIdentifierFallbacks()
    {
        global::jpeg.JpegInfo info = CreateRgbInfo();
        info.Metadata.HasJfif = true;
        info.Metadata.HasExif = true;
        info.Metadata.ExifMetadata.HasColorSpace = true;
        info.Metadata.ExifMetadata.ColorSpace = 1;
        info.Metadata.HasCicp = true;
        info.Metadata.Cicp.IsPresent = true;
        info.Metadata.Cicp.IsValid = true;
        info.Metadata.Cicp.ColorPrimaries = 9;
        info.Metadata.Cicp.TransferCharacteristics = 16;
        info.Metadata.Cicp.MatrixCoefficients = 1;
        global::jpeg.JpegColorHintResolver resolver = new global::jpeg.JpegColorHintResolver();

        global::jpeg.JpegColorHintMetadata cicp = resolver.Resolve(info);
        info.Metadata.HasCicp = false;
        global::jpeg.JpegColorHintMetadata jfif = resolver.Resolve(info);
        info.Metadata.HasJfif = false;
        info.Metadata.HasExif = false;
        global::jpeg.JpegColorHintMetadata components = resolver.Resolve(info);

        Assert.That(cicp.EncodedColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCbCr));
        Assert.That(cicp.EncodedColorSpaceSource, Is.EqualTo(global::jpeg.JpegColorHintSource.Cicp));
        Assert.That(cicp.ColorCharacteristicsSource, Is.EqualTo(global::jpeg.JpegColorHintSource.Cicp));
        Assert.That(cicp.HasConflict, Is.True);
        Assert.That(jfif.EncodedColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCbCr));
        Assert.That(jfif.EncodedColorSpaceSource, Is.EqualTo(global::jpeg.JpegColorHintSource.Jfif));
        Assert.That(jfif.ColorCharacteristicsSource, Is.EqualTo(global::jpeg.JpegColorHintSource.Exif));
        Assert.That(components.EncodedColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.RGB));
        Assert.That(components.EncodedColorSpaceSource, Is.EqualTo(global::jpeg.JpegColorHintSource.ComponentIdentifiers));
        Assert.That(components.ColorCharacteristicsSource, Is.EqualTo(global::jpeg.JpegColorHintSource.Unknown));
    }

    public static byte[] CreateCicpProfile(int colorPrimaries, int transferCharacteristics, int matrixCoefficients, int videoFullRangeFlag)
    {
        byte[] profile = IccProfileParserTests.CreateIccProfileWithOneTag();
        IccProfileParserTests.WriteSignature(profile, 132, "cicp");
        global::jpeg.JpegByteWriter.WriteUInt32(profile, 140, 12);
        IccProfileParserTests.WriteSignature(profile, 144, "cicp");
        profile[152] = (byte)colorPrimaries;
        profile[153] = (byte)transferCharacteristics;
        profile[154] = (byte)matrixCoefficients;
        profile[155] = (byte)videoFullRangeFlag;
        return profile;
    }

    public static byte[] CreateRgbSofPayload()
    {
        byte[] payload = IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3);
        payload[6] = 82;
        payload[9] = 71;
        payload[12] = 66;
        return payload;
    }

    public static byte[] CreateRgbSosPayload()
    {
        byte[] payload = IdentifierTests.CreateSosPayload(3);
        payload[1] = 82;
        payload[3] = 71;
        payload[5] = 66;
        return payload;
    }

    public static global::jpeg.JpegInfo CreateRgbInfo()
    {
        global::jpeg.JpegInfo info = new global::jpeg.JpegInfo();
        info.Frame.ComponentCount = 3;
        global::jpeg.JpegFrameComponent red = new global::jpeg.JpegFrameComponent();
        red.Identifier = 82;
        global::jpeg.JpegFrameComponent green = new global::jpeg.JpegFrameComponent();
        green.Identifier = 71;
        global::jpeg.JpegFrameComponent blue = new global::jpeg.JpegFrameComponent();
        blue.Identifier = 66;
        info.Frame.Components.Add(red);
        info.Frame.Components.Add(green);
        info.Frame.Components.Add(blue);
        return info;
    }
}
