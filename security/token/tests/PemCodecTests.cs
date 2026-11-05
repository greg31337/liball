using token;

namespace token.Tests;

public class PemCodecTests
{
    [Test]
    public void WriteProducesCanonicalPemWithSixtyFourCharacterLines()
    {
        byte[] bytes = Enumerable.Range(0, 100).Select(value => (byte)value).ToArray();

        TokenTextResult result = PemCodec.Write("CERTIFICATE", bytes);
        string[] lines = result.Text.Split('\n');

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(lines[0], Is.EqualTo("-----BEGIN CERTIFICATE-----"));
            Assert.That(lines[1], Has.Length.EqualTo(64));
            Assert.That(lines[2], Has.Length.EqualTo(64));
            Assert.That(lines[3], Has.Length.EqualTo(8));
            Assert.That(lines[4], Is.EqualTo("-----END CERTIFICATE-----"));
            Assert.That(lines[5], Is.Empty);
            Assert.That(string.Concat(lines[1], lines[2], lines[3]), Is.EqualTo(Convert.ToBase64String(bytes)));
        });
    }

    [Test]
    public void ReadRoundTripsWrittenPem()
    {
        byte[] expected = Enumerable.Range(0, 100).Select(value => (byte)(255 - value)).ToArray();
        string pem = PemCodec.Write("PUBLIC KEY", expected).Text;

        PemReadResult result = PemCodec.Read(pem);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Block.Label, Is.EqualTo("PUBLIC KEY"));
            Assert.That(result.Block.Bytes, Is.EqualTo(expected));
        });
    }

    [Test]
    public void ReadAcceptsCrLfAndAsciiWhitespaceInBodyAndAroundBlock()
    {
        string pem = " \t\r\n-----BEGIN CERTIFICATE-----\r\n Zm9v\t\r\nYmFy \r\n-----END CERTIFICATE-----\r\n\t ";

        PemReadResult result = PemCodec.Read(pem);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Block.Label, Is.EqualTo("CERTIFICATE"));
            Assert.That(System.Text.Encoding.ASCII.GetString(result.Block.Bytes), Is.EqualTo("foobar"));
        });
    }

    [Test]
    public void EmptyPemBlockRoundTrips()
    {
        TokenTextResult written = PemCodec.Write("DATA", Array.Empty<byte>());
        PemReadResult read = PemCodec.Read(written.Text);

        Assert.Multiple(() =>
        {
            Assert.That(written.Text, Is.EqualTo("-----BEGIN DATA-----\n-----END DATA-----\n"));
            Assert.That(read.Success, Is.True);
            Assert.That(read.Block.Bytes, Is.Empty);
        });
    }

    [TestCase("")]
    [TestCase(" CERTIFICATE")]
    [TestCase("CERTIFICATE ")]
    [TestCase("PRIVATE  KEY")]
    [TestCase("PRIVATE--KEY")]
    [TestCase("CERTIFICATE\nKEY")]
    public void WriteRejectsInvalidLabels(string label)
    {
        TokenTextResult result = PemCodec.Write(label, Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_pem_label"));
        });
    }

    [Test]
    public void ReadRejectsMismatchedBoundaryLabels()
    {
        string pem = "-----BEGIN CERTIFICATE-----\nZg==\n-----END PUBLIC KEY-----\n";

        PemReadResult result = PemCodec.Read(pem);

        AssertInvalidPem(result, 33);
    }

    [TestCase("Z===", 22)]
    [TestCase("Zh==", 22)]
    [TestCase("Zm-v", 23)]
    public void ReadReportsBase64ErrorsAtOriginalPemPositions(string body, int position)
    {
        string pem = "-----BEGIN DATA-----\n" + body + "\n-----END DATA-----\n";

        PemReadResult result = PemCodec.Read(pem);

        AssertInvalidPem(result, position);
    }

    [Test]
    public void ReadRejectsMissingBoundariesAndTrailingData()
    {
        PemReadResult missingBegin = PemCodec.Read("Zg==");
        PemReadResult missingEnd = PemCodec.Read("-----BEGIN DATA-----\nZg==\n");
        PemReadResult trailing = PemCodec.Read("-----BEGIN DATA-----\nZg==\n-----END DATA-----\nother");

        Assert.Multiple(() =>
        {
            Assert.That(missingBegin.Error.Code, Is.EqualTo("invalid_pem"));
            Assert.That(missingEnd.Error.Code, Is.EqualTo("invalid_pem"));
            Assert.That(trailing.Error.Code, Is.EqualTo("invalid_pem"));
            Assert.That(trailing.Error.Position, Is.EqualTo(45));
        });
    }

    [Test]
    public void WriteSupportsByteSpanSlices()
    {
        byte[] bytes = new byte[] { 0xFF, 102, 111, 111, 0xFF };

        TokenTextResult result = PemCodec.Write("DATA", bytes.AsSpan(1, 3));

        Assert.That(result.Text, Does.Contain("\nZm9v\n"));
    }

    [Test]
    public void StringAndArrayOverloadsRejectNull()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => PemCodec.Read(null), Throws.ArgumentNullException);
            Assert.That(() => PemCodec.Write(null, Array.Empty<byte>()), Throws.ArgumentNullException);
            Assert.That(() => PemCodec.Write("DATA", (byte[])null), Throws.ArgumentNullException);
        });
    }

    static void AssertInvalidPem(PemReadResult result, int position)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Block, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("invalid_pem"));
            Assert.That(result.Error.Position, Is.EqualTo(position));
        });
    }
}
