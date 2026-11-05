namespace dns.Tests.Internal.Wire;

public class TypeBitmapCodecTests
{
    [Test]
    public void EveryWindowAndBoundaryBitRoundTripsInCanonicalOrder()
    {
        List<int> input = new List<int>();
        for (int window = 255; window >= 0; window--)
        {
            int first = window * 256;
            if (first != 0)
            {
                input.Add(first);
            }

            input.Add(first + 1);
            input.Add(first + 255);
            input.Add(first + 1);
        }

        byte[] destination = new byte[256 * 34];
        WireWriter writer = new WireWriter(
            destination, 0, destination.Length);
        WireWriteResult encoded = writer.WriteTypeBitmap(input);
        WireReader reader = new WireReader(
            destination, 0, encoded.BytesWritten);
        WireReadResult decoded = reader.ReadTypeBitmap(encoded.BytesWritten);
        int[] expected = input.Distinct().OrderBy(value => value).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.BytesWritten, Is.EqualTo(destination.Length));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.IntegerValues, Is.EqualTo(expected));
            Assert.That(decoded.NextOffset, Is.EqualTo(destination.Length));
            Assert.That(reader.RemainingLength(), Is.Zero);
        });

        for (int window = 0; window <= 255; window++)
        {
            int offset = window * 34;
            Assert.Multiple(() =>
            {
                Assert.That(destination[offset], Is.EqualTo(window));
                Assert.That(destination[offset + 1], Is.EqualTo(32));
                Assert.That(destination[offset + 2],
                    Is.EqualTo(window == 0 ? 0x40 : 0xc0));
                Assert.That(destination[offset + 33], Is.EqualTo(0x01));
            });
        }
    }

    [Test]
    public void DecoderRejectsDuplicateWindowsAndTypeZero()
    {
        WireReader duplicateReader = new WireReader(
            [0, 1, 0x40, 0, 1, 0x20], 0, 6);
        WireReader typeZeroReader = new WireReader([0, 1, 0x80], 0, 3);

        WireReadResult duplicate = duplicateReader.ReadTypeBitmap(6);
        WireReadResult typeZero = typeZeroReader.ReadTypeBitmap(3);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Success, Is.False);
            Assert.That(duplicate.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTypeBitmap));
            Assert.That(duplicate.Diagnostics[0].Offset, Is.EqualTo(3));
            Assert.That(duplicateReader.ReadOffset(), Is.Zero);
            Assert.That(typeZero.Success, Is.False);
            Assert.That(typeZero.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTypeBitmap));
            Assert.That(typeZero.Diagnostics[0].Offset, Is.EqualTo(2));
            Assert.That(typeZeroReader.ReadOffset(), Is.Zero);
        });
    }
}
