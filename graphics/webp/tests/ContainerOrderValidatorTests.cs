using webp;

namespace webp.Tests;

[TestFixture]
public class ContainerOrderValidatorTests
{
    [Test]
    public void ValidateChunks_AcceptsReconstructionChunksInOrder()
    {
        List<WebpChunkInfo> chunks = new List<WebpChunkInfo>();
        chunks.Add(NewChunk("EXIF", 12));
        chunks.Add(NewChunk("VP8X", 20));
        chunks.Add(NewChunk("zzzz", 30));
        chunks.Add(NewChunk("ICCP", 40));
        chunks.Add(NewChunk("XMP ", 50));
        chunks.Add(NewChunk("ALPH", 60));
        chunks.Add(NewChunk("VP8 ", 70));

        WebpValidationResult result = WebpContainerOrderValidator.ValidateChunks(chunks, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void ValidateChunks_RejectsOutOfOrderReconstructionChunksInStrictMode()
    {
        List<WebpChunkInfo> chunks = new List<WebpChunkInfo>();
        chunks.Add(NewChunk("VP8X", 12));
        chunks.Add(NewChunk("VP8 ", 20));
        chunks.Add(NewChunk("ALPH", 30));

        WebpValidationResult result = WebpContainerOrderValidator.ValidateChunks(chunks, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Error));
        Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(30));
        Assert.That(result.Diagnostics[0].FourCc, Is.EqualTo("ALPH"));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("VP8 "));
    }

    [Test]
    public void ValidateChunks_WarnsForOutOfOrderReconstructionChunksInPermissiveMode()
    {
        List<WebpChunkInfo> chunks = new List<WebpChunkInfo>();
        chunks.Add(NewChunk("ANMF", 12));
        chunks.Add(NewChunk("ANIM", 30));

        WebpValidationResult result = WebpContainerOrderValidator.ValidateChunks(chunks, WebpStrictness.Permissive);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Warning));
        Assert.That(result.Diagnostics[0].FourCc, Is.EqualTo("ANIM"));
    }

    [Test]
    public void ValidateChunks_IgnoresExifXmpAndUnknownChunkPositions()
    {
        List<WebpChunkInfo> chunks = new List<WebpChunkInfo>();
        chunks.Add(NewChunk("VP8 ", 12));
        chunks.Add(NewChunk("EXIF", 20));
        chunks.Add(NewChunk("XMP ", 30));
        chunks.Add(NewChunk("zzzz", 40));

        WebpValidationResult result = WebpContainerOrderValidator.ValidateChunks(chunks, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void Validate_ParsesRiffAndValidatesChunkOrder()
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        chunks.Add(new WebpRiffChunk("VP8 ", new byte[] { 1 }));
        chunks.Add(new WebpRiffChunk("ALPH", new byte[] { 2 }));
        byte[] bytes = WriteWithoutSorting("WEBP", chunks);

        WebpValidationResult result = WebpContainerOrderValidator.Validate(bytes, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].FourCc, Is.EqualTo("ALPH"));
    }

    [Test]
    public void Validate_ReturnsRiffParserErrorsBeforeOrderValidation()
    {
        byte[] bytes = new byte[] { 1, 2, 3 };

        WebpValidationResult result = WebpContainerOrderValidator.Validate(bytes, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("RIFF"));
    }

    static WebpChunkInfo NewChunk(string fourCc, long offset)
    {
        WebpChunkInfo chunk = new WebpChunkInfo();
        chunk.FourCc = fourCc;
        chunk.Offset = offset;
        return chunk;
    }

    static byte[] WriteWithoutSorting(string formatFourCc, List<WebpRiffChunk> chunks)
    {
        long riffSize = WebpRiffWriter.ComputeRiffSize(chunks);
        List<byte> data = new List<byte>();
        AppendFourCc(data, "RIFF");
        WebpEndianWriter.AppendUInt32(data, (uint)riffSize);
        AppendFourCc(data, formatFourCc);

        int index = 0;
        while (index < chunks.Count)
        {
            WebpRiffChunk chunk = chunks[index];
            AppendFourCc(data, chunk.FourCc);
            WebpEndianWriter.AppendUInt32(data, (uint)chunk.Payload.Length);
            WebpEndianWriter.AppendChunkPayload(data, chunk.Payload);
            index = index + 1;
        }

        return data.ToArray();
    }

    static void AppendFourCc(List<byte> data, string fourCc)
    {
        data.Add((byte)fourCc[0]);
        data.Add((byte)fourCc[1]);
        data.Add((byte)fourCc[2]);
        data.Add((byte)fourCc[3]);
    }
}
