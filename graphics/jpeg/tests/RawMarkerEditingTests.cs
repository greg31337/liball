namespace jpeg.Tests;

internal class RawMarkerEditingTests
{
    [Test]
    public void Identifier_ExposesPreservedMarkersInOriginalStreamOrder()
    {
        byte[] data = MetadataPolicyTests.CreateMetadataHeavyJpeg();

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);

        Assert.That(
            info.Metadata.RawMarkers.Select(item => item.Value),
            Is.EqualTo(new int[]
            {
                global::jpeg.JpegConstants.MarkerApp0,
                global::jpeg.JpegConstants.MarkerApp1,
                global::jpeg.JpegConstants.MarkerApp1,
                global::jpeg.JpegConstants.MarkerComment,
                global::jpeg.JpegConstants.MarkerApp2,
                global::jpeg.JpegConstants.MarkerApp13,
                0xFFF0,
                global::jpeg.JpegConstants.MarkerApp2,
                global::jpeg.JpegConstants.MarkerApp14
            }));
        Assert.That(info.Metadata.RawMarkers.Select(item => item.Order), Is.Ordered.Ascending);
        Assert.That(info.Metadata.RawMarkers[6].Payload, Is.EqualTo(new byte[] { 7, 7 }));
    }

    [Test]
    public void ExplicitRawMarkers_CanBeRetainedReplacedInsertedRemovedAndReordered()
    {
        global::jpeg.JpegImage original = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        original.Metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App1, global::jpeg.JpegConstants.MarkerApp1, new byte[] { 1 }));
        original.Metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.Comment, global::jpeg.JpegConstants.MarkerComment, System.Text.Encoding.Latin1.GetBytes("remove me")));
        original.Metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App3, global::jpeg.JpegConstants.MarkerApp3, new byte[] { 3 }));
        byte[] initialData = new global::jpeg.JpegWriter().Write(original);
        global::jpeg.JpegImage edited = global::jpeg.Jpeg.Read(initialData);
        System.Collections.Generic.List<global::jpeg.JpegRawMarker> markers = edited.Metadata.RawMarkers;

        markers[0].Payload = new byte[] { 9, 9 };
        markers[0].PayloadLength = 2;
        markers.Insert(1, CreateRawMarker(global::jpeg.JpegMarker.App4, global::jpeg.JpegConstants.MarkerApp4, new byte[] { 4 }));
        markers.RemoveAt(2);
        global::jpeg.JpegRawMarker moved = markers[2];
        markers.RemoveAt(2);
        markers.Insert(0, moved);
        edited.Metadata.RawMarkersAreExplicit = true;
        edited.Metadata.Comments.Add("structured fallback must not be written");
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.MetadataPolicy = global::jpeg.JpegMetadataPolicy.ReplaceSelected;

        byte[] result = new global::jpeg.JpegWriter().WriteWithOptions(edited, options);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(result);

        Assert.That(
            info.Metadata.RawMarkers.Select(item => item.Value),
            Is.EqualTo(new int[]
            {
                global::jpeg.JpegConstants.MarkerApp3,
                global::jpeg.JpegConstants.MarkerApp1,
                global::jpeg.JpegConstants.MarkerApp4
            }));
        Assert.That(info.Metadata.RawMarkers[0].Payload, Is.EqualTo(new byte[] { 3 }));
        Assert.That(info.Metadata.RawMarkers[1].Payload, Is.EqualTo(new byte[] { 9, 9 }));
        Assert.That(info.Metadata.RawMarkers[2].Payload, Is.EqualTo(new byte[] { 4 }));
        Assert.That(info.CommentCount, Is.EqualTo(0));
    }

    [Test]
    public void ExplicitRawMarkers_AreFilteredWithoutChangingRelativeOrder()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        byte[] adobe = AdobeMetadataParserTests.CreateAdobePayload(100, 1, 2, global::jpeg.JpegConstants.AdobeTransformUnknown);
        byte[] jfif = JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 96, 0, 0);
        byte[] exif = ExifMetadataParserTests.CreateExifPayload(new byte[] { 0x4D, 0x4D, 0, 0x2A, 0, 0, 0, 8 });
        source.Metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.Comment, global::jpeg.JpegConstants.MarkerComment, new byte[] { 1 }));
        source.Metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.Unknown, 0xFFF0, new byte[] { 2 }));
        source.Metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App14, global::jpeg.JpegConstants.MarkerApp14, adobe));
        source.Metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App0, global::jpeg.JpegConstants.MarkerApp0, jfif));
        source.Metadata.RawMarkers.Add(CreateRawMarker(global::jpeg.JpegMarker.App1, global::jpeg.JpegConstants.MarkerApp1, exif));
        source.Metadata.RawMarkersAreExplicit = true;
        global::jpeg.JpegWriteOptions knownOptions = new global::jpeg.JpegWriteOptions();
        knownOptions.MetadataPolicy = global::jpeg.JpegMetadataPolicy.PreserveKnown;
        global::jpeg.JpegWriteOptions safeOptions = new global::jpeg.JpegWriteOptions();
        safeOptions.MetadataPolicy = global::jpeg.JpegMetadataPolicy.StripUnsafe;

        global::jpeg.JpegInfo known = global::jpeg.Jpeg.Identify(new global::jpeg.JpegWriter().WriteWithOptions(source, knownOptions));
        global::jpeg.JpegInfo safe = global::jpeg.Jpeg.Identify(new global::jpeg.JpegWriter().WriteWithOptions(source, safeOptions));

        Assert.That(
            known.Metadata.RawMarkers.Select(item => item.Value),
            Is.EqualTo(new int[]
            {
                global::jpeg.JpegConstants.MarkerComment,
                global::jpeg.JpegConstants.MarkerApp14,
                global::jpeg.JpegConstants.MarkerApp0,
                global::jpeg.JpegConstants.MarkerApp1
            }));
        Assert.That(
            safe.Metadata.RawMarkers.Select(item => item.Value),
            Is.EqualTo(new int[]
            {
                global::jpeg.JpegConstants.MarkerApp14,
                global::jpeg.JpegConstants.MarkerApp0
            }));
    }

    [Test]
    public void ExplicitEmptyRawMarkerSequence_RemovesStructuredMetadataUnderReplaceSelected()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        source.Metadata.Comments.Add("remove everything");
        source.Metadata.RawMarkersAreExplicit = true;
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.MetadataPolicy = global::jpeg.JpegMetadataPolicy.ReplaceSelected;

        byte[] result = new global::jpeg.JpegWriter().WriteWithOptions(source, options);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(result);

        Assert.That(info.Metadata.RawMarkers, Is.Empty);
        Assert.That(info.CommentCount, Is.EqualTo(0));
    }

    public static global::jpeg.JpegRawMarker CreateRawMarker(global::jpeg.JpegMarker marker, int value, byte[] payload)
    {
        global::jpeg.JpegRawMarker result = new global::jpeg.JpegRawMarker();
        result.Marker = marker;
        result.Value = value;
        result.Code = value & 255;
        result.Payload = payload;
        result.PayloadLength = payload.Length;
        result.IsApplicationMarker = value >= global::jpeg.JpegConstants.MarkerApp0 && value <= global::jpeg.JpegConstants.MarkerApp15;
        result.IsComment = value == global::jpeg.JpegConstants.MarkerComment;
        result.IsUnknownMarker = value == 0xFFF0;
        return result;
    }
}
