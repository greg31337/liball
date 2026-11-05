namespace avif.Tests;

public class Leb128Tests
{
    [Test]
    public void CanonicalKnownValuesRoundTripWithExpectedBytes()
    {
        ulong[] values = [0, 127, 128, 624485, Leb128.MaxValue];
        byte[][] expected =
        [
            [0],
            [127],
            [128, 1],
            [229, 142, 38],
            [255, 255, 255, 255, 255, 255, 255, 127]
        ];

        int index = 0;
        while (index < values.Length)
        {
            byte[] buffer = new byte[8];
            ByteWriter writer = new ByteWriter(buffer.Length);
            AvifResultCode writeCode = Leb128.WriteUnsigned(buffer, writer, values[index]);

            Assert.That(writeCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(buffer[0..writer.Position], Is.EqualTo(expected[index]));

            ByteReader reader = new ByteReader(writer.Position);
            ulong decoded = Leb128.ReadUnsigned(buffer, reader);
            Assert.That(decoded, Is.EqualTo(values[index]));
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.Position, Is.EqualTo(writer.Position));
            index++;
        }
    }

    [Test]
    public void CanonicalLengthChangesAtEverySevenBitBoundary()
    {
        ulong[] values =
        [
            0,
            127,
            128,
            16383,
            16384,
            2097151,
            2097152,
            268435455,
            268435456,
            34359738367,
            34359738368,
            4398046511103,
            4398046511104,
            562949953421311,
            562949953421312,
            Leb128.MaxValue
        ];

        int index = 0;
        while (index < values.Length)
        {
            int expectedLength = index / 2 + 1;
            byte[] destination = new byte[Leb128.MaxBytes];
            ByteWriter writer = new ByteWriter(destination.Length);
            Leb128.WriteUnsigned(destination, writer, values[index]);
            ByteReader reader = new ByteReader(writer.Position);
            ulong decoded = Leb128.ReadUnsigned(destination, reader);

            Assert.Multiple(() =>
            {
                Assert.That(Leb128.GetEncodedLength(values[index]), Is.EqualTo(expectedLength));
                Assert.That(writer.Position, Is.EqualTo(expectedLength));
                Assert.That(reader.Position, Is.EqualTo(expectedLength));
                Assert.That(decoded, Is.EqualTo(values[index]));
            });

            index++;
        }
    }

    [Test]
    public void NonCanonicalEncodingsAreRejectedWithoutAdvancing()
    {
        byte[][] nonCanonical =
        [
            [128, 0],
            [129, 0],
            [255, 0],
            [128, 128, 128, 128, 128, 128, 128, 0]
        ];

        int index = 0;
        while (index < nonCanonical.Length)
        {
            byte[] encoded = nonCanonical[index];
            ByteReader reader = new ByteReader(encoded.Length);

            ulong value = Leb128.ReadUnsigned(encoded, reader);

            Assert.That(value, Is.Zero);
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.InvalidData));
            Assert.That(reader.Position, Is.Zero);
            index++;
        }
    }

    [Test]
    public void UnterminatedAndTruncatedEncodingsHaveDistinctFailures()
    {
        byte[] unterminated = [128, 128, 128, 128, 128, 128, 128, 128];
        byte[] truncated = [128];
        ByteReader unterminatedReader = new ByteReader(unterminated.Length);
        ByteReader truncatedReader = new ByteReader(truncated.Length);

        ulong unterminatedValue = Leb128.ReadUnsigned(unterminated, unterminatedReader);
        ulong truncatedValue = Leb128.ReadUnsigned(truncated, truncatedReader);

        Assert.Multiple(() =>
        {
            Assert.That(unterminatedValue, Is.Zero);
            Assert.That(unterminatedReader.Code, Is.EqualTo(AvifResultCode.InvalidData));
            Assert.That(unterminatedReader.Position, Is.Zero);
            Assert.That(truncatedValue, Is.Zero);
            Assert.That(truncatedReader.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(truncatedReader.Position, Is.Zero);
        });
    }

    [Test]
    public void WriterRejectsOutOfRangeAndInsufficientDestinationAtomically()
    {
        byte[] outOfRangeDestination = [204];
        ByteWriter outOfRangeWriter = new ByteWriter(outOfRangeDestination.Length);
        AvifResultCode outOfRangeCode = Leb128.WriteUnsigned(
            outOfRangeDestination,
            outOfRangeWriter,
            Leb128.MaxValue + 1);

        byte[] shortDestination = [204];
        ByteWriter shortWriter = new ByteWriter(shortDestination.Length);
        AvifResultCode shortCode = Leb128.WriteUnsigned(shortDestination, shortWriter, 128);

        Assert.Multiple(() =>
        {
            Assert.That(outOfRangeCode, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(outOfRangeWriter.Position, Is.Zero);
            Assert.That(outOfRangeDestination, Is.EqualTo(new byte[] { 204 }));
            Assert.That(shortCode, Is.EqualTo(AvifResultCode.BufferTooSmall));
            Assert.That(shortWriter.Position, Is.Zero);
            Assert.That(shortDestination, Is.EqualTo(new byte[] { 204 }));
        });
    }
}
