using System.Text;
using email;

namespace email.Tests;

public class EmailQuotedPrintableEncoderTests
{
    [TestCase("", "")]
    [TestCase("Hello World!", "Hello World!")]
    [TestCase("a=b", "a=3Db")]
    [TestCase("tab\there", "tab\there")]
    public void OneShotEncoderProducesCanonicalBodyVectors(string decoded, string encoded)
    {
        byte[] result = new EmailQuotedPrintable().EncodeWithInput(Encoding.ASCII.GetBytes(decoded));

        Assert.That(Encoding.ASCII.GetString(result), Is.EqualTo(encoded));
    }

    [Test]
    public void UnsafeBytesUseUppercaseHexadecimalEscapes()
    {
        byte[] input = new byte[] { 0, 10, 61, 127, 128, 255 };

        byte[] encoded = new EmailQuotedPrintable().EncodeWithInputAndNewlinePolicyAndProtectFromLines(
            input,
            EmailQuotedPrintableNewlinePolicy.Encode,
            false);

        Assert.That(Encoding.ASCII.GetString(encoded), Is.EqualTo("=00=0A=3D=7F=80=FF"));
    }

    [Test]
    public void SpacesAndTabsAtLineEndAreAlwaysEncoded()
    {
        byte[] input = Encoding.ASCII.GetBytes("one \t\r\ntwo \t\nthree ");

        byte[] encoded = new EmailQuotedPrintable().EncodeWithInput(input);

        Assert.That(
            Encoding.ASCII.GetString(encoded),
            Is.EqualTo("one=20=09\r\ntwo=20=09\r\nthree=20"));
    }

    [Test]
    public void WrappingUsesCrlfAndNeverSplitsAnEncodedToken()
    {
        byte[] literals = new EmailQuotedPrintable().EncodeWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(
            Encoding.ASCII.GetBytes("ABCDEFGHIJK"),
            8,
            EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf,
            false);
        byte[] escaped = new EmailQuotedPrintable().EncodeWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(
            new byte[] { 65, 65, 65, 65, 65, 65, 255 },
            8,
            EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf,
            false);

        Assert.That(Encoding.ASCII.GetString(literals), Is.EqualTo("ABCDEFG=\r\nHIJK"));
        Assert.That(Encoding.ASCII.GetString(escaped), Is.EqualTo("AAAAAA=\r\n=FF"));
    }

    [Test]
    public void EveryGeneratedPhysicalLineStaysWithinTheSelectedLength()
    {
        byte[] input = new byte[256];
        int index = 0;
        while (index < input.Length)
        {
            input[index] = (byte)index;
            index++;
        }

        string encoded = Encoding.ASCII.GetString(new EmailQuotedPrintable().EncodeWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(
            input,
            20,
            EmailQuotedPrintableNewlinePolicy.Encode,
            false));
        string[] lines = encoded.Split("\r\n");

        index = 0;
        while (index < lines.Length)
        {
            Assert.That(lines[index].Length, Is.LessThanOrEqualTo(20), "line " + index);
            if (index + 1 < lines.Length)
            {
                Assert.That(lines[index].EndsWith("="), Is.True, "line " + index);
            }

            index++;
        }
    }

    [Test]
    public void NewlinePoliciesPreserveNormalizeOrEncodeInputForms()
    {
        byte[] input = Encoding.ASCII.GetBytes("a\r\nb\nc\rd");

        string preserved = Encoding.ASCII.GetString(new EmailQuotedPrintable().EncodeWithInputAndNewlinePolicyAndProtectFromLines(
            input,
            EmailQuotedPrintableNewlinePolicy.Preserve,
            false));
        string normalized = Encoding.ASCII.GetString(new EmailQuotedPrintable().EncodeWithInputAndNewlinePolicyAndProtectFromLines(
            input,
            EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf,
            false));
        string encoded = Encoding.ASCII.GetString(new EmailQuotedPrintable().EncodeWithInputAndNewlinePolicyAndProtectFromLines(
            input,
            EmailQuotedPrintableNewlinePolicy.Encode,
            false));

        Assert.That(preserved, Is.EqualTo("a\r\nb\nc\rd"));
        Assert.That(normalized, Is.EqualTo("a\r\nb\r\nc\r\nd"));
        Assert.That(encoded, Is.EqualTo("a=0D=0Ab=0Ac=0Dd"));
    }

    [Test]
    public void FromLineProtectionOnlyChangesDangerousHardLinePrefixes()
    {
        byte[] input = Encoding.ASCII.GetBytes("From one\r\nnot From two\r\nFrom three");

        string unprotected = Encoding.ASCII.GetString(new EmailQuotedPrintable().EncodeWithInputAndNewlinePolicyAndProtectFromLines(
            input,
            EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf,
            false));
        string protectedValue = Encoding.ASCII.GetString(new EmailQuotedPrintable().EncodeWithInputAndNewlinePolicyAndProtectFromLines(
            input,
            EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf,
            true));

        Assert.That(unprotected, Is.EqualTo("From one\r\nnot From two\r\nFrom three"));
        Assert.That(protectedValue, Is.EqualTo("=46rom one\r\nnot From two\r\n=46rom three"));
    }

    [Test]
    public void FromLineProtectionAlsoCoversGeneratedSoftLineStarts()
    {
        string encoded = Encoding.ASCII.GetString(new EmailQuotedPrintable().EncodeWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(
            Encoding.ASCII.GetBytes("AAAAAAAFrom x"),
            8,
            EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf,
            true));
        string[] lines = encoded.Split("\r\n");

        Assert.That(encoded, Does.Contain("\r\n=46"));
        int index = 0;
        while (index < lines.Length)
        {
            Assert.That(lines[index].StartsWith("From "), Is.False, "line " + index);
            index++;
        }
    }

    [Test]
    public void IncrementalEncodingIsIndependentOfEveryTwoChunkSplit()
    {
        byte[] input = Encoding.ASCII.GetBytes("From one \t\r\ntwo=three\nfour ");
        byte[] expected = new EmailQuotedPrintable().EncodeWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(
            input,
            12,
            EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf,
            true);
        int split = 0;
        while (split <= input.Length)
        {
            EmailQuotedPrintableEncoder encoder = new EmailQuotedPrintableEncoder(
                12,
                EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf,
                true);
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
    public void EncodedBytesRoundTripForManyLengthsAndWidths()
    {
        int[] lineLengths = new int[] { 4, 8, 20, 76 };
        int length = 0;
        while (length <= 128)
        {
            byte[] original = CreateInput(length);
            int widthIndex = 0;
            while (widthIndex < lineLengths.Length)
            {
                byte[] encoded = new EmailQuotedPrintable().EncodeWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(
                    original,
                    lineLengths[widthIndex],
                    EmailQuotedPrintableNewlinePolicy.Encode,
                    true);
                EmailQuotedPrintableDecodeResult decoded = new EmailQuotedPrintable().Decode(
                    encoded,
                    EmailQuotedPrintableDecodeMode.Strict,
                    original.Length);

                Assert.That(decoded.GetDecodedBytes(), Is.EqualTo(original), "length " + length + ", width " + lineLengths[widthIndex]);
                Assert.That(decoded.GetIssues().GetCount(), Is.Zero);
                widthIndex++;
            }

            length++;
        }
    }

    [Test]
    public void EncoderLifecycleAndConfigurationAreExplicit()
    {
        EmailQuotedPrintableEncoder encoder = new EmailQuotedPrintableEncoder(
            20,
            EmailQuotedPrintableNewlinePolicy.Preserve,
            true);
        Assert.That(encoder.Feed(Encoding.ASCII.GetBytes("From")), Is.Empty);
        Assert.That(Encoding.ASCII.GetString(encoder.Feed(Encoding.ASCII.GetBytes(" "))), Is.EqualTo("=46rom"));
        Assert.That(Encoding.ASCII.GetString(encoder.Complete()), Is.EqualTo("=20"));

        Assert.That(encoder.GetLineLength(), Is.EqualTo(20));
        Assert.That(encoder.GetNewlinePolicy(), Is.EqualTo(EmailQuotedPrintableNewlinePolicy.Preserve));
        Assert.That(encoder.GetProtectFromLines(), Is.True);
        Assert.That(encoder.GetLineColumn(), Is.EqualTo(9));
        Assert.That(encoder.GetInputByteCount(), Is.EqualTo(5));
        Assert.That(encoder.GetOutputByteCount(), Is.EqualTo(9));
        Assert.That(encoder.IsComplete(), Is.True);
        Assert.Throws<InvalidOperationException>(() => encoder.Feed(Array.Empty<byte>()));
        Assert.Throws<InvalidOperationException>(() => encoder.Complete());
    }

    [TestCase(0)]
    [TestCase(3)]
    [TestCase(77)]
    public void EncoderRejectsUnsupportedLineLengths(int lineLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailQuotedPrintableEncoder(
            lineLength,
            EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf,
            false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailQuotedPrintable().EncodeWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(
            Array.Empty<byte>(),
            lineLength,
            EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf,
            false));
    }

    [Test]
    public void EncoderRejectsUnknownNewlinePolicy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailQuotedPrintableEncoder(
            76,
            (EmailQuotedPrintableNewlinePolicy)10,
            false));
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
