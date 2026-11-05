using webp;

namespace webp.Tests;

[TestFixture]
public class SignatureTests
{
    [Test]
    public void HasRiffWebpSignature_ReturnsTrueForMinimalWebpRiff()
    {
        byte[] bytes = WebpRiffWriter.Write("WEBP", new List<WebpRiffChunk>());

        Assert.That(WebpSignature.HasRiffWebpSignature(bytes), Is.True);
    }

    [Test]
    public void HasRiffWebpSignature_ReturnsFalseForShortOrWrongSignatures()
    {
        byte[] shortData = new byte[] { (byte)'R', (byte)'I', (byte)'F' };
        byte[] wrongRiff = WebpRiffWriter.Write("WEBP", new List<WebpRiffChunk>());
        byte[] wrongForm = WebpRiffWriter.Write("AVI ", new List<WebpRiffChunk>());
        wrongRiff[0] = (byte)'R';
        wrongRiff[1] = (byte)'I';
        wrongRiff[2] = (byte)'F';
        wrongRiff[3] = (byte)'X';

        Assert.That(WebpSignature.HasRiffWebpSignature(shortData), Is.False);
        Assert.That(WebpSignature.HasRiffWebpSignature(wrongRiff), Is.False);
        Assert.That(WebpSignature.HasRiffWebpSignature(wrongForm), Is.False);
    }

    [Test]
    public void ValidateRiffWebp_AcceptsValidWebpRiff()
    {
        byte[] bytes = WebpRiffWriter.Write("WEBP", new List<WebpRiffChunk>());

        WebpValidationResult result = WebpSignature.ValidateRiffWebp(bytes);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void ValidateRiffWebp_RejectsNonWebpRiffFormType()
    {
        byte[] bytes = WebpRiffWriter.Write("AVI ", new List<WebpRiffChunk>());

        WebpValidationResult result = WebpSignature.ValidateRiffWebp(bytes);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Error));
        Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(8));
        Assert.That(result.Diagnostics[0].FourCc, Is.EqualTo("AVI "));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("WEBP"));
    }

    [Test]
    public void ValidateRiffWebp_RejectsTruncatedHeader()
    {
        WebpValidationResult result = WebpSignature.ValidateRiffWebp(new byte[] { 1, 2, 3 });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("truncated"));
    }

    [Test]
    public void ValidateRiffWebp_CarriesRiffParserErrors()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", new byte[] { 1 }));
        byte[] bytes = WebpRiffWriter.Write("WEBP", chunks);
        bytes[bytes.Length - 1] = 0x7F;

        WebpValidationResult result = WebpSignature.ValidateRiffWebp(bytes);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("padding"));
    }

    [Test]
    public void ValidateRiffWebp_AllowsRiffParserWarningsWithoutInvalidating()
    {
        byte[] bytes = WebpRiffWriter.Write("WEBP", new List<WebpRiffChunk>());
        byte[] withTrailing = new byte[bytes.Length + 1];
        Array.Copy(bytes, withTrailing, bytes.Length);
        withTrailing[withTrailing.Length - 1] = 0x99;

        WebpValidationResult result = WebpSignature.ValidateRiffWebp(withTrailing);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Warning));
    }
}
