namespace avif.Tests;

public class StringByteIoTests
{
    [Test]
    public void Utf8StringFormsRoundTrip()
    {
        byte[] buffer = new byte[64];
        ByteWriter writer = new ByteWriter(buffer.Length);

        writer.WriteUtf8(buffer, "Aé😀");
        writer.WriteNullTerminatedUtf8(buffer, "hé");
        writer.WriteLengthPrefixedUtf8(buffer, "cat", 1);
        writer.WriteLengthPrefixedUtf8(buffer, "λ", 2);
        writer.WriteLengthPrefixedUtf8(buffer, string.Empty, 4);

        Assert.That(buffer[11..23], Is.EqualTo(new byte[]
        {
            3, 99, 97, 116,
            0, 2, 206, 187,
            0, 0, 0, 0
        }));

        ByteReader reader = new ByteReader(writer.Position);
        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadUtf8(buffer, 7), Is.EqualTo("Aé😀"));
            Assert.That(reader.ReadNullTerminatedUtf8(buffer), Is.EqualTo("hé"));
            Assert.That(reader.ReadLengthPrefixedUtf8(buffer, 1), Is.EqualTo("cat"));
            Assert.That(reader.ReadLengthPrefixedUtf8(buffer, 2), Is.EqualTo("λ"));
            Assert.That(reader.ReadLengthPrefixedUtf8(buffer, 4), Is.Empty);
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.Remaining(), Is.Zero);
        });
    }

    [Test]
    public void InvalidUtf8SequencesAreRejectedWithoutAdvancing()
    {
        byte[][] invalidSequences =
        [
            [192, 128],
            [224, 128, 128],
            [237, 160, 128],
            [244, 144, 128, 128],
            [245, 128, 128, 128],
            [226, 130],
            [128]
        ];

        int index = 0;
        while (index < invalidSequences.Length)
        {
            byte[] encoded = invalidSequences[index];
            ByteReader reader = new ByteReader(encoded.Length);

            string value = reader.ReadUtf8(encoded, encoded.Length);

            Assert.That(value, Is.Empty);
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.InvalidData));
            Assert.That(reader.Position, Is.Zero);
            index++;
        }
    }

    [Test]
    public void MissingNullTerminatorDistinguishesInvalidFromTruncatedInput()
    {
        byte[] encoded = [97, 98, 99];
        ByteReader completeReader = new ByteReader(encoded.Length);
        ByteReader partialReader = new ByteReader(encoded.Length + 1);

        string completeValue = completeReader.ReadNullTerminatedUtf8(encoded);
        string partialValue = partialReader.ReadNullTerminatedUtf8(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(completeValue, Is.Empty);
            Assert.That(completeReader.Code, Is.EqualTo(AvifResultCode.InvalidData));
            Assert.That(completeReader.Position, Is.Zero);
            Assert.That(partialValue, Is.Empty);
            Assert.That(partialReader.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(partialReader.Position, Is.Zero);
        });
    }

    [Test]
    public void LengthPrefixedReadIsAtomicWhenPayloadIsTruncated()
    {
        byte[] encoded = [0, 5, 97, 98, 99];
        ByteReader reader = new ByteReader(encoded.Length);

        string value = reader.ReadLengthPrefixedUtf8(encoded, 2);

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.Empty);
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(reader.Position, Is.Zero);
        });
    }

    [Test]
    public void InvalidUtf16IsRejectedWithoutWritingReplacementBytes()
    {
        byte[] destination = [204, 204, 204, 204];
        ByteWriter writer = new ByteWriter(destination.Length);
        string invalid = "\uD800";

        AvifResultCode code = writer.WriteUtf8(destination, invalid);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(writer.Position, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 204, 204, 204, 204 }));
        });
    }

    [Test]
    public void LengthPrefixCapacityIsCheckedBeforeWriting()
    {
        byte[] destination = new byte[300];
        ByteWriter writer = new ByteWriter(destination.Length);
        string value = new string('x', 256);

        AvifResultCode code = writer.WriteLengthPrefixedUtf8(destination, value, 1);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(writer.Position, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[300]));
        });
    }
}
