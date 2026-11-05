namespace jpeg.Tests;

internal class BitWriterTests
{
    [Test]
    public void Writer_WritesBitsMostSignificantBitFirstAcrossBytes()
    {
        global::jpeg.JpegBitWriter writer = global::jpeg.JpegBitWriter.WithCapacity(1);

        writer.WriteBits(5, 3);
        writer.WriteBits(18, 5);
        writer.WriteBits(6, 4);
        writer.FlushToByteBoundary();

        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xB2, 0x6F }));
        Assert.That(writer.BitsInBuffer, Is.EqualTo(0));
        Assert.That(writer.BitsWritten, Is.EqualTo(16));
    }

    [Test]
    public void Writer_StuffsZeroAfterFfEntropyByte()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();

        writer.WriteBits(0xFF, 8);

        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xFF, 0x00 }));
        Assert.That(writer.StuffedByteCount, Is.EqualTo(1));
    }

    [Test]
    public void Writer_PadsPartialByteWithOnesByDefault()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();

        writer.WriteBits(5, 3);
        writer.FlushToByteBoundary();

        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xBF }));
        Assert.That(writer.BitsInBuffer, Is.EqualTo(0));
    }

    [Test]
    public void Writer_CanPadPartialByteWithExplicitBit()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();

        writer.WriteBits(5, 3);
        writer.FlushToByteBoundaryWithPadBit(0);

        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xA0 }));
    }

    [Test]
    public void Writer_OutputRoundTripsThroughBitReader()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();
        writer.WriteBits(0xFF, 8);
        writer.WriteBits(42, 6);
        writer.FlushToByteBoundary();

        global::jpeg.JpegBitReader reader = new global::jpeg.JpegBitReader(writer.ToArray());
        Assert.That(reader.ReadBits(8), Is.EqualTo(0xFF));
        Assert.That(reader.ReadBits(6), Is.EqualTo(42));
        Assert.That(reader.HasMarker, Is.False);
    }

    [Test]
    public void StreamWriter_WritesStuffedBytesToStream()
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        global::jpeg.JpegBitWriter writer = global::jpeg.JpegBitWriter.FromStream(stream);

        writer.WriteBits(0xFF, 8);
        writer.WriteBits(0, 1);
        writer.FlushToByteBoundaryWithPadBit(0);

        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0xFF, 0x00, 0x00 }));
        Assert.That(writer.StuffedByteCount, Is.EqualTo(1));
    }

    [Test]
    public void Writer_WritesStandaloneMarkerAfterByteAlign()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();

        writer.WriteBits(5, 3);
        writer.WriteMarkerAfterByteAlign(global::jpeg.JpegConstants.MarkerRestart3);

        Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0xBF, 0xFF, 0xD3 }));
        Assert.That(writer.BitsInBuffer, Is.EqualTo(0));
    }

    [Test]
    public void Writer_RejectsInvalidBitsCountsValuesAndMarkers()
    {
        global::jpeg.JpegBitWriter writer = new global::jpeg.JpegBitWriter();

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                writer.WriteBit(2);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                writer.WriteBits(1, 25);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                writer.WriteBits(4, 2);
            });

        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                writer.WriteBits(1, 0);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WriteMarkerAfterByteAlign(global::jpeg.JpegConstants.MarkerApp0);
            });
    }

    [Test]
    public void Writer_RejectsNullAndNonWritableStreams()
    {
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegBitWriter.FromWriter((global::jpeg.JpegByteWriter)null!);
            });

        System.IO.MemoryStream nonWritable = new System.IO.MemoryStream(new byte[] { 1 }, false);
        Assert.Throws<System.ArgumentException>(
            delegate
            {
                global::jpeg.JpegBitWriter.FromStream(nonWritable);
            });
    }
}
