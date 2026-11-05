namespace jpeg.Tests;

internal class InspectorMarkerKindCoverageTests
{
    [Test]
    public void Inspector_ReportsEveryMarkerKindWithExactOffsets()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        System.Collections.Generic.List<int> values = new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> offsets = new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> lengths = new System.Collections.Generic.List<int>();
        writer.WriteStartOfImage();
        values.Add(global::jpeg.JpegConstants.MarkerStartOfImage);
        offsets.Add(0);
        lengths.Add(2);
        int offset = 2;

        foreach (global::jpeg.JpegMarker marker in System.Enum.GetValues<global::jpeg.JpegMarker>())
        {
            int value = (int)marker;
            if (marker == global::jpeg.JpegMarker.Unknown
                || marker == global::jpeg.JpegMarker.StartOfImage
                || marker == global::jpeg.JpegMarker.EndOfImage
                || marker == global::jpeg.JpegMarker.StartOfScan)
            {
                continue;
            }

            values.Add(value);
            offsets.Add(offset);
            if (marker == global::jpeg.JpegMarker.Temporary
                || (marker >= global::jpeg.JpegMarker.Restart0 && marker <= global::jpeg.JpegMarker.Restart7))
            {
                writer.WriteStandaloneMarker(marker);
                lengths.Add(2);
                offset = offset + 2;
            }
            else
            {
                writer.WriteMarkerSegment(marker, new byte[0]);
                lengths.Add(4);
                offset = offset + 4;
            }
        }

        values.Add(0xFFF0);
        offsets.Add(offset);
        lengths.Add(4);
        writer.WriteMarkerSegmentValue(0xFFF0, new byte[0]);
        offset = offset + 4;
        values.Add(global::jpeg.JpegConstants.MarkerStartOfScan);
        offsets.Add(offset);
        lengths.Add(4);
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, new byte[0]);
        offset = offset + 4;
        values.Add(global::jpeg.JpegConstants.MarkerEndOfImage);
        offsets.Add(offset);
        lengths.Add(2);
        writer.WriteEndOfImage();

        global::jpeg.JpegMarkerSegment[] markers = global::jpeg.JpegInspector.ReadMarkers(writer.ToArray());

        Assert.That(markers.Length, Is.EqualTo(values.Count));
        int index = 0;
        while (index < markers.Length)
        {
            global::jpeg.JpegMarkerSegment segment = markers[index];
            Assert.That(segment.Order, Is.EqualTo(index));
            Assert.That(segment.Value, Is.EqualTo(values[index]));
            Assert.That(segment.MarkerOffset, Is.EqualTo(offsets[index]));
            Assert.That(segment.EndOffset, Is.EqualTo(offsets[index] + lengths[index]));
            if (lengths[index] == 4)
            {
                Assert.That(segment.HasLength, Is.True);
                Assert.That(segment.LengthOffset, Is.EqualTo(offsets[index] + 2));
                Assert.That(segment.PayloadOffset, Is.EqualTo(offsets[index] + 4));
                Assert.That(segment.SegmentLength, Is.EqualTo(2));
                Assert.That(segment.Payload, Is.Empty);
            }
            else
            {
                Assert.That(segment.HasLength, Is.False);
                Assert.That(segment.LengthOffset, Is.EqualTo(-1));
                Assert.That(segment.PayloadOffset, Is.EqualTo(-1));
            }

            index = index + 1;
        }

        Assert.That(markers.Any(item => item.Value == 0xFFF0 && item.Marker == global::jpeg.JpegMarker.Unknown), Is.True);
        Assert.That(markers.Select(item => item.Marker).Distinct(), Does.Contain(global::jpeg.JpegMarker.JpegLs));
    }
}
