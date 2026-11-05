namespace jpeg.Tests;

internal class BitReaderTests
{
    [Test]
    public void Reader_ReadsBitsMostSignificantBitFirstAcrossBytes()
    {
        global::jpeg.JpegBitReader reader = new global::jpeg.JpegBitReader(new byte[] { 0xB2, 0x60 });

        int first = reader.ReadBits(3);
        int second = reader.ReadBits(5);
        int third = reader.ReadBits(4);

        Assert.That(first, Is.EqualTo(5));
        Assert.That(second, Is.EqualTo(18));
        Assert.That(third, Is.EqualTo(6));
        Assert.That(reader.BitsAvailable, Is.EqualTo(4));
    }

    [Test]
    public void Reader_PeeksAndDropsBitsWithoutChangingBytePosition()
    {
        global::jpeg.JpegBitReader reader = new global::jpeg.JpegBitReader(new byte[] { 0xCA, 0x80 });

        Assert.That(reader.PeekBits(4), Is.EqualTo(12));
        Assert.That(reader.PeekBits(4), Is.EqualTo(12));
        reader.DropBits(4);
        Assert.That(reader.ReadBits(4), Is.EqualTo(10));
        Assert.That(reader.ReadBit(), Is.EqualTo(1));
    }

    [Test]
    public void Reader_UnstuffsFfZeroAsEntropyDataByte()
    {
        global::jpeg.JpegBitReader reader = new global::jpeg.JpegBitReader(new byte[] { 0xFF, 0x00, 0x80 });

        int stuffed = reader.ReadBits(8);
        int next = reader.ReadBit();

        Assert.That(stuffed, Is.EqualTo(0xFF));
        Assert.That(next, Is.EqualTo(1));
        Assert.That(reader.StuffedByteCount, Is.EqualTo(1));
        Assert.That(reader.HasMarker, Is.False);
    }

    [Test]
    public void Reader_ReportsMarkerLookaheadAtUnstuffedMarker()
    {
        global::jpeg.JpegBitReader reader = new global::jpeg.JpegBitReader(new byte[] { 0xAA, 0xFF, 0xD0, 0xCC });

        Assert.That(reader.ReadBits(8), Is.EqualTo(0xAA));
        Assert.That(reader.EnsureBits(1), Is.False);

        global::jpeg.JpegScannedMarker marker = reader.GetMarkerLookahead();
        Assert.That(reader.HasMarker, Is.True);
        Assert.That(marker.Marker, Is.EqualTo(global::jpeg.JpegMarker.Restart0));
        Assert.That(marker.Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerRestart0));
        Assert.That(marker.Offset, Is.EqualTo(1));
        Assert.That(marker.EntropyBytesSkipped, Is.EqualTo(1));

        global::jpeg.JpegScannedMarker consumed = reader.ConsumeMarkerLookahead();
        Assert.That(consumed.Value, Is.EqualTo(global::jpeg.JpegConstants.MarkerRestart0));
        Assert.That(reader.HasMarker, Is.False);
        Assert.That(reader.ReadBits(8), Is.EqualTo(0xCC));
    }

    [Test]
    public void Reader_ByteAlignDropsPaddingBeforeMarkerLookahead()
    {
        global::jpeg.JpegBitReader reader = new global::jpeg.JpegBitReader(new byte[] { 0xAF, 0xFF, 0xD9 });

        Assert.That(reader.ReadBits(3), Is.EqualTo(5));
        reader.ByteAlign();
        Assert.That(reader.EnsureBits(1), Is.False);

        global::jpeg.JpegScannedMarker marker = reader.GetMarkerLookahead();
        Assert.That(marker.Marker, Is.EqualTo(global::jpeg.JpegMarker.EndOfImage));
        Assert.That(marker.Offset, Is.EqualTo(1));
    }

    [Test]
    public void Reader_SkipsFillBytesBeforeMarkerCode()
    {
        global::jpeg.JpegBitReader reader = new global::jpeg.JpegBitReader(new byte[] { 0xFF, 0xFF, 0xD1 });

        Assert.That(reader.EnsureBits(1), Is.False);

        global::jpeg.JpegScannedMarker marker = reader.GetMarkerLookahead();
        Assert.That(marker.Marker, Is.EqualTo(global::jpeg.JpegMarker.Restart1));
        Assert.That(marker.Offset, Is.EqualTo(0));
        Assert.That(marker.EntropyBytesSkipped, Is.EqualTo(0));
    }

    [Test]
    public void Reader_AdvanceToMarkerDiscardsEntropyBytes()
    {
        global::jpeg.JpegBitReader reader = new global::jpeg.JpegBitReader(new byte[] { 0x11, 0xFF, 0x00, 0x22, 0xFF, 0xD2 });

        bool found = reader.AdvanceToMarker();

        global::jpeg.JpegScannedMarker marker = reader.GetMarkerLookahead();
        Assert.That(found, Is.True);
        Assert.That(marker.Marker, Is.EqualTo(global::jpeg.JpegMarker.Restart2));
        Assert.That(marker.Offset, Is.EqualTo(4));
        Assert.That(marker.EntropyBytesSkipped, Is.EqualTo(4));
    }

    [Test]
    public void StreamReader_ReadsStuffedBytesAndMarkerLookahead()
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream(new byte[] { 0xFF, 0x00, 0xFF, 0xD9 });
        global::jpeg.JpegBitReader reader = global::jpeg.JpegBitReader.FromStream(stream);

        Assert.That(reader.ReadBits(8), Is.EqualTo(0xFF));
        Assert.That(reader.EnsureBits(1), Is.False);

        global::jpeg.JpegScannedMarker marker = reader.GetMarkerLookahead();
        Assert.That(marker.Marker, Is.EqualTo(global::jpeg.JpegMarker.EndOfImage));
        Assert.That(marker.Offset, Is.EqualTo(2));
    }

    [Test]
    public void Reader_RejectsInvalidCountsAndMissingMarkerLookahead()
    {
        global::jpeg.JpegBitReader reader = new global::jpeg.JpegBitReader(new byte[] { 0x00 });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                reader.ReadBits(25);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                reader.DropBits(1);
            });

        Assert.Throws<System.InvalidOperationException>(
            delegate
            {
                reader.GetMarkerLookahead();
            });
    }

    [Test]
    public void Reader_ThrowsWhenInputEndsBeforeRequestedBits()
    {
        global::jpeg.JpegBitReader reader = new global::jpeg.JpegBitReader(new byte[0]);

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                reader.ReadBit();
            });
    }
}
