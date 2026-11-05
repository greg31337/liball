using System.Text;
using email;

namespace email.Tests;

public class EmailBase64EncoderTests
{
    [TestCase("", "")]
    [TestCase("M", "TQ==")]
    [TestCase("Ma", "TWE=")]
    [TestCase("Man", "TWFu")]
    [TestCase("Some longer text", "U29tZSBsb25nZXIgdGV4dA==")]
    public void OneShotEncoderProducesCanonicalVectors(string decoded, string encoded)
    {
        byte[] result = new EmailBase64().EncodeWithInput(Encoding.ASCII.GetBytes(decoded));

        Assert.That(Encoding.ASCII.GetString(result), Is.EqualTo(encoded));
    }

    [TestCase(4, "TWFu\r\nTWFu\r\nTWFu")]
    [TestCase(8, "TWFuTWFu\r\nTWFu")]
    [TestCase(12, "TWFuTWFuTWFu")]
    public void SelectableLineLengthWrapsBetweenQuantumsWithCrlf(int lineLength, string expected)
    {
        byte[] result = new EmailBase64().EncodeWithInputAndLineLength(Encoding.ASCII.GetBytes("ManManMan"), lineLength);

        Assert.That(Encoding.ASCII.GetString(result), Is.EqualTo(expected));
        Assert.That(result.Length == 0 || result[result.Length - 1] != 10, Is.True);
    }

    [Test]
    public void DefaultLineLengthWrapsOnlyAfterSeventySixCharacters()
    {
        byte[] exactLine = CreateInput(57);
        byte[] wrapped = CreateInput(58);

        string exactResult = Encoding.ASCII.GetString(new EmailBase64().EncodeWithInput(exactLine));
        string wrappedResult = Encoding.ASCII.GetString(new EmailBase64().EncodeWithInput(wrapped));

        Assert.That(exactResult.Length, Is.EqualTo(76));
        Assert.That(exactResult, Does.Not.Contain("\r\n"));
        Assert.That(wrappedResult.IndexOf("\r\n"), Is.EqualTo(76));
        Assert.That(wrappedResult.Length, Is.EqualTo(82));
        Assert.That(wrappedResult.EndsWith("\r\n"), Is.False);
    }

    [Test]
    public void EncoderMatchesBclAlphabetAndPaddingForEveryByteValue()
    {
        byte[] input = new byte[256];
        int index = 0;
        while (index < input.Length)
        {
            input[index] = (byte)index;
            index++;
        }

        string encoded = Encoding.ASCII.GetString(new EmailBase64().EncodeWithInput(input));
        string unwrapped = encoded.Replace("\r\n", "");

        Assert.That(unwrapped, Is.EqualTo(Convert.ToBase64String(input)));
        Assert.That(unwrapped, Does.Not.Contain("\r"));
        Assert.That(unwrapped, Does.Not.Contain("\n"));
    }

    [Test]
    public void IncrementalEncodingIsIndependentOfEveryTwoChunkSplit()
    {
        byte[] input = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog.");
        byte[] expected = new EmailBase64().EncodeWithInputAndLineLength(input, 16);
        int split = 0;
        while (split <= input.Length)
        {
            EmailBase64Encoder encoder = new EmailBase64Encoder(16);
            List<byte> encoded = new List<byte>();
            encoded.AddRange(encoder.Feed(new ReadOnlySpan<byte>(input, 0, split)));
            encoded.AddRange(encoder.Feed(new ReadOnlySpan<byte>(input, split, input.Length - split)));
            encoded.AddRange(encoder.Complete());

            Assert.That(encoded.ToArray(), Is.EqualTo(expected), "split " + split);
            Assert.That(encoder.GetInputByteCount(), Is.EqualTo(input.Length));
            Assert.That(encoder.GetOutputByteCount(), Is.EqualTo(expected.Length));
            split++;
        }
    }

    [Test]
    public void EncoderBuffersPartialQuantumUntilEnoughInputArrives()
    {
        EmailBase64Encoder encoder = new EmailBase64Encoder(4);

        Assert.That(encoder.Feed(new byte[] { (byte)'M' }), Is.Empty);
        Assert.That(encoder.Feed(new byte[] { (byte)'a' }), Is.Empty);
        Assert.That(Encoding.ASCII.GetString(encoder.Feed(new byte[] { (byte)'n' })), Is.EqualTo("TWFu"));
        Assert.That(encoder.Feed(new byte[] { (byte)'!' }), Is.Empty);
        Assert.That(Encoding.ASCII.GetString(encoder.Complete()), Is.EqualTo("\r\nIQ=="));
        Assert.That(encoder.GetLineColumn(), Is.EqualTo(4));
        Assert.That(encoder.GetInputByteCount(), Is.EqualTo(4));
        Assert.That(encoder.GetOutputByteCount(), Is.EqualTo(10));
    }

    [Test]
    public void EncoderLifecycleAndConfigurationAreExplicit()
    {
        EmailBase64Encoder encoder = new EmailBase64Encoder(20);
        encoder.Feed(Encoding.ASCII.GetBytes("M"));
        encoder.Complete();

        Assert.That(encoder.GetLineLength(), Is.EqualTo(20));
        Assert.That(encoder.IsComplete(), Is.True);
        Assert.Throws<InvalidOperationException>(() => encoder.Feed(Array.Empty<byte>()));
        Assert.Throws<InvalidOperationException>(() => encoder.Complete());
    }

    [TestCase(0)]
    [TestCase(3)]
    [TestCase(5)]
    [TestCase(77)]
    [TestCase(80)]
    public void EncoderRejectsUnsupportedLineLengths(int lineLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBase64Encoder(lineLength));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailBase64().EncodeWithInputAndLineLength(Array.Empty<byte>(), lineLength));
    }

    [Test]
    public void EncodedOutputRoundTripsThroughStrictDecoderForManyLengthsAndWidths()
    {
        int[] lineLengths = new int[] { 4, 8, 20, 76 };
        int length = 0;
        while (length <= 128)
        {
            byte[] original = CreateInput(length);
            int widthIndex = 0;
            while (widthIndex < lineLengths.Length)
            {
                byte[] encoded = new EmailBase64().EncodeWithInputAndLineLength(original, lineLengths[widthIndex]);
                EmailBase64DecodeResult decoded = new EmailBase64().Decode(
                    encoded,
                    EmailBase64DecodeMode.Strict,
                    original.Length);

                Assert.That(decoded.GetDecodedBytes(), Is.EqualTo(original), "length " + length + ", width " + lineLengths[widthIndex]);
                Assert.That(decoded.GetIssues().GetCount(), Is.Zero);
                widthIndex++;
            }

            length++;
        }
    }

    static byte[] CreateInput(int length)
    {
        byte[] input = new byte[length];
        int index = 0;
        while (index < input.Length)
        {
            input[index] = (byte)(index * 37 + length);
            index++;
        }

        return input;
    }
}
