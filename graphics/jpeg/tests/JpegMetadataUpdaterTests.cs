using System.Collections.Generic;

namespace jpeg.Tests;

internal class JpegMetadataUpdaterTests
{
    [Test]
    public void SetComments_ReplacesCommentsAndPreservesUnknownMarkers()
    {
        byte[] source = CreateMetadataUpdateSource();
        List<string> comments = new List<string>();
        comments.Add("new");
        comments.Add("second");

        byte[] output = global::jpeg.Jpeg.SetComments(source, comments);

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(output);
        global::jpeg.JpegCoefficientImage coefficients = global::jpeg.JpegInspector.ReadCoefficients(output);
        Assert.That(info.Metadata.Comments.Count, Is.EqualTo(2));
        Assert.That(info.Metadata.Comments[0], Is.EqualTo("new"));
        Assert.That(info.Metadata.Comments[1], Is.EqualTo("second"));
        Assert.That(ContainsRawMarker(info.Metadata, 0xFFF0, new byte[] { 0x77 }), Is.True);
        Assert.That(GetNaturalCoefficient(coefficients.Components[0], 0, 0), Is.EqualTo(1));
    }

    [Test]
    public void SetIccProfile_ReplacesIccAndPreservesOtherMetadata()
    {
        byte[] source = CreateMetadataUpdateSource();
        byte[] profile = new byte[] { 9, 8, 7, 6 };

        byte[] output = global::jpeg.Jpeg.SetIccProfile(source, profile);

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(output);
        Assert.That(info.Metadata.HasIccProfile, Is.True);
        Assert.That(info.Metadata.IccProfile, Is.EqualTo(profile));
        Assert.That(info.Metadata.HasExif, Is.True);
        Assert.That(info.Metadata.HasXmp, Is.True);
        Assert.That(info.Metadata.Comments[0], Is.EqualTo("old"));
        Assert.That(ContainsRawMarker(info.Metadata, 0xFFF0, new byte[] { 0x77 }), Is.True);
    }

    [Test]
    public void SetExifAndSetXmp_ReplaceSelectedApp1Payloads()
    {
        byte[] source = CreateMetadataUpdateSource();
        byte[] exifPayload = ExifMetadataParserTests.CreateExifPayload(new byte[] { 0x4D, 0x4D, 0, 0x2A, 0, 0, 0, 8, 0, 0, 0, 0, 0, 0 });
        byte[] xmpPayload = XmpMetadataParserTests.CreateXmpPayload(System.Text.Encoding.UTF8.GetBytes("<updated />"));

        byte[] withExif = global::jpeg.Jpeg.SetExif(source, exifPayload);
        byte[] withXmp = global::jpeg.Jpeg.SetXmp(withExif, xmpPayload);

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(withXmp);
        Assert.That(info.Metadata.HasExif, Is.True);
        Assert.That(info.Metadata.Exif, Is.EqualTo(exifPayload));
        Assert.That(info.Metadata.HasXmp, Is.True);
        Assert.That(info.Metadata.Xmp, Is.EqualTo(xmpPayload));
        Assert.That(info.Metadata.HasIccProfile, Is.True);
        Assert.That(ContainsRawMarker(info.Metadata, 0xFFF0, new byte[] { 0x77 }), Is.True);
    }

    [Test]
    public void MetadataUpdater_RejectsInvalidInputs()
    {
        byte[] source = CreateMetadataUpdateSource();
        List<string> comments = new List<string>();
        comments.Add("ok");

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.SetComments(null!, comments);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.SetComments(source, null!);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.SetIccProfile(source, null!);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.Jpeg.SetIccProfile(source, new byte[0]);
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.Jpeg.SetExif(source, new byte[] { 1 });
            });
        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                global::jpeg.Jpeg.SetXmp(source, new byte[] { 1 });
            });
    }

    public static byte[] CreateMetadataUpdateSource()
    {
        global::jpeg.JpegCoefficientImage image = JpegLosslessTransformerTests.CreateFourBlockCoefficientImage();
        image.Metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.Comment, global::jpeg.JpegConstants.MarkerComment, System.Text.Encoding.Latin1.GetBytes("old")));
        image.Metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App1, global::jpeg.JpegConstants.MarkerApp1, ExifMetadataParserTests.CreateExifPayload(new byte[] { 0x4D, 0x4D, 0, 0x2A, 0, 0, 0, 8, 0, 0, 0, 0, 0, 0 })));
        image.Metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App1, global::jpeg.JpegConstants.MarkerApp1, XmpMetadataParserTests.CreateXmpPayload(System.Text.Encoding.UTF8.GetBytes("<old />"))));
        image.Metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App2, global::jpeg.JpegConstants.MarkerApp2, CreateIccPayload(new byte[] { 1, 2, 3 })));
        image.Metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.Unknown, 0xFFF0, new byte[] { 0x77 }));
        return global::jpeg.JpegInspector.WriteCoefficients(image);
    }

    public static global::jpeg.JpegRawMarker CreateRawMarker(global::jpeg.JpegMarker marker, int value, byte[] payload)
    {
        global::jpeg.JpegRawMarker raw = new global::jpeg.JpegRawMarker();
        raw.Marker = marker;
        raw.Value = value;
        raw.Code = value & 255;
        raw.Payload = payload;
        raw.PayloadLength = payload.Length;
        raw.IsApplicationMarker = value >= global::jpeg.JpegConstants.MarkerApp0 && value <= global::jpeg.JpegConstants.MarkerApp15;
        raw.IsComment = value == global::jpeg.JpegConstants.MarkerComment;
        raw.IsUnknownMarker = value == 0xFFF0;
        return raw;
    }

    public static byte[] CreateIccPayload(byte[] profile)
    {
        global::jpeg.JpegIccProfileChunkWriter writer = new global::jpeg.JpegIccProfileChunkWriter();
        List<global::jpeg.JpegIccProfileChunk> chunks = writer.CreateChunks(profile);
        return writer.WritePayload(chunks[0]);
    }

    public static bool ContainsRawMarker(global::jpeg.JpegMetadata metadata, int value, byte[] payload)
    {
        foreach (global::jpeg.JpegRawMarker marker in metadata.RawMarkers)
        {
            if (marker.Value == value && EqualBytes(marker.Payload, payload))
            {
                return true;
            }
        }

        return false;
    }

    public static bool EqualBytes(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    public static int GetNaturalCoefficient(global::jpeg.JpegCoefficientComponent component, int blockIndex, int naturalIndex)
    {
        return JpegLosslessTransformerTests.GetNaturalCoefficient(component, blockIndex, naturalIndex);
    }
}
