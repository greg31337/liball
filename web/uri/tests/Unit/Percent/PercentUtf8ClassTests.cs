using System.Globalization;
using System.Text;

namespace uri.Tests;

public class PercentUtf8ClassTests
{
    [TestCase("00", "\0")]
    [TestCase("7F", "\u007f")]
    [TestCase("C280", "\u0080")]
    [TestCase("DFBF", "\u07ff")]
    [TestCase("E0A080", "\u0800")]
    [TestCase("ED9FBF", "\ud7ff")]
    [TestCase("EE8080", "\ue000")]
    [TestCase("EFBFBF", "\uffff")]
    [TestCase("F0908080", "\ud800\udc00")]
    [TestCase("F48FBFBF", "\udbff\udfff")]
    public void PublicStrictDecoderAcceptsEveryLegalUtf8BoundaryClass(
        string hex,
        string expected)
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();
        string input = ToPercentText(Convert.FromHexString(hex));

        PercentDecodeResult result = codec.DecodeText(input, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, hex);
            Assert.That(result.Text, Is.EqualTo(expected), hex);
            Assert.That(result.UsedReplacement, Is.False, hex);
            Assert.That(result.Diagnostic, Is.Null, hex);
        });
    }

    [TestCase("80", 0, 3, "\uFFFD")]
    [TestCase("C080", 0, 3, "\uFFFD\uFFFD")]
    [TestCase("C180", 0, 3, "\uFFFD\uFFFD")]
    [TestCase("C2", 3, 0, "\uFFFD")]
    [TestCase("C220", 3, 3, "\uFFFD ")]
    [TestCase("E08080", 3, 3, "\uFFFD\uFFFD\uFFFD")]
    [TestCase("EDA080", 3, 3, "\uFFFD\uFFFD\uFFFD")]
    [TestCase("E228A1", 3, 3, "\uFFFD(\uFFFD")]
    [TestCase("E282", 6, 0, "\uFFFD")]
    [TestCase("E28241", 6, 3, "\uFFFDA")]
    [TestCase("F0808080", 3, 3, "\uFFFD\uFFFD\uFFFD\uFFFD")]
    [TestCase("F4908080", 3, 3, "\uFFFD\uFFFD\uFFFD\uFFFD")]
    [TestCase("F09080", 9, 0, "\uFFFD")]
    [TestCase("F0908041", 9, 3, "\uFFFDA")]
    [TestCase("F5808080", 0, 3, "\uFFFD\uFFFD\uFFFD\uFFFD")]
    [TestCase("FF", 0, 3, "\uFFFD")]
    public void PublicDecodersCoverEveryIllegalUtf8Class(
        string hex,
        int errorOffset,
        int errorLength,
        string replacementText)
    {
        IPercentCodec codec = new PercentCodec();
        string input = ToPercentText(Convert.FromHexString(hex));
        PercentCodecOptions strictOptions = new PercentCodecOptions();
        PercentCodecOptions replacementOptions = new PercentCodecOptions();
        replacementOptions.DecodeMode = PercentDecodeMode.ReplaceInvalidUtf8;

        PercentDecodeResult strict = codec.DecodeText(input, strictOptions);
        PercentDecodeResult replacement = codec.DecodeText(input, replacementOptions);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False, hex);
            Assert.That(strict.Text, Is.Empty, hex);
            Assert.That(strict.Diagnostic, Is.Not.Null, hex);
            Assert.That(strict.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidUtf8), hex);
            Assert.That(strict.Diagnostic.Range.Offset, Is.EqualTo(errorOffset), hex);
            Assert.That(strict.Diagnostic.Range.Length, Is.EqualTo(errorLength), hex);
            Assert.That(replacement.Success, Is.True, hex);
            Assert.That(replacement.Text, Is.EqualTo(replacementText), hex);
            Assert.That(replacement.UsedReplacement, Is.True, hex);
            Assert.That(replacement.Diagnostic, Is.Null, hex);
        });
    }

    private static string ToPercentText(byte[] bytes)
    {
        StringBuilder text = new StringBuilder();
        int offset = 0;
        while (offset < bytes.Length)
        {
            text.Append('%');
            text.Append(bytes[offset].ToString("X2", CultureInfo.InvariantCulture));
            offset = offset + 1;
        }

        return text.ToString();
    }
}
