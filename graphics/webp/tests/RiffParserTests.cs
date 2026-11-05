using webp;

namespace webp.Tests;

[TestFixture]
public class RiffParserTests
{
    [Test]
    public void Parse_ValidRiffTracksChunkOffsetsAndPadding()
    {
        List<byte> data = NewRiffHeader("WEBP", 26);
        AppendChunk(data, "VP8 ", new byte[] { 1, 2, 3 }, 0);
        AppendChunk(data, "EXIF", new byte[] { 4, 5 }, -1);

        WebpRiffParseResult result = WebpRiffParser.Parse(data.ToArray());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.RiffFourCc, Is.EqualTo("RIFF"));
        Assert.That(result.FormatFourCc, Is.EqualTo("WEBP"));
        Assert.That(result.DeclaredSize, Is.EqualTo(26));
        Assert.That(result.DeclaredFileLength, Is.EqualTo(data.Count));
        Assert.That(result.Chunks, Has.Count.EqualTo(2));
        Assert.That(result.Diagnostics, Is.Empty);

        Assert.That(result.Chunks[0].FourCc, Is.EqualTo("VP8 "));
        Assert.That(result.Chunks[0].Offset, Is.EqualTo(12));
        Assert.That(result.Chunks[0].PayloadOffset, Is.EqualTo(20));
        Assert.That(result.Chunks[0].PayloadLength, Is.EqualTo(3));
        Assert.That(result.Chunks[0].HasPadding, Is.True);
        Assert.That(result.Chunks[0].PaddingOffset, Is.EqualTo(23));
        Assert.That(result.Chunks[0].PaddedEndOffset, Is.EqualTo(24));
        Assert.That(result.Chunks[0].IsPaddingValid, Is.True);
        Assert.That(result.Chunks[0].IsKnown, Is.True);

        Assert.That(result.Chunks[1].FourCc, Is.EqualTo("EXIF"));
        Assert.That(result.Chunks[1].Offset, Is.EqualTo(24));
        Assert.That(result.Chunks[1].PayloadOffset, Is.EqualTo(32));
        Assert.That(result.Chunks[1].PayloadLength, Is.EqualTo(2));
        Assert.That(result.Chunks[1].HasPadding, Is.False);
        Assert.That(result.Chunks[1].PaddedEndOffset, Is.EqualTo(34));
    }

    [Test]
    public void Parse_InvalidPaddingByteReportsError()
    {
        List<byte> data = NewRiffHeader("WEBP", 14);
        AppendChunk(data, "VP8 ", new byte[] { 1 }, 0x7F);

        WebpRiffParseResult result = WebpRiffParser.Parse(data.ToArray());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Chunks, Has.Count.EqualTo(1));
        Assert.That(result.Chunks[0].IsPaddingValid, Is.False);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Error));
        Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(21));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("padding"));
    }

    [Test]
    public void Parse_MissingPaddingByteReportsError()
    {
        List<byte> data = NewRiffHeader("WEBP", 13);
        AppendFourCc(data, "VP8 ");
        WebpEndianWriter.AppendUInt32(data, 1);
        data.Add(0xAA);

        WebpRiffParseResult result = WebpRiffParser.Parse(data.ToArray());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Chunks, Has.Count.EqualTo(1));
        Assert.That(result.Chunks[0].HasPadding, Is.True);
        Assert.That(result.Chunks[0].IsPaddingValid, Is.False);
        Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(21));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("padding"));
    }

    [Test]
    public void Parse_ChunkPayloadBeyondDeclaredRiffSizeReportsError()
    {
        List<byte> data = NewRiffHeader("WEBP", 14);
        AppendFourCc(data, "VP8 ");
        WebpEndianWriter.AppendUInt32(data, 4);
        WebpEndianWriter.AppendBytes(data, new byte[] { 1, 2 });

        WebpRiffParseResult result = WebpRiffParser.Parse(data.ToArray());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Chunks, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Error));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("payload"));
    }

    [Test]
    public void Parse_TruncatedChunkHeaderReportsError()
    {
        List<byte> data = NewRiffHeader("WEBP", 5);
        data.Add(0x55);

        WebpRiffParseResult result = WebpRiffParser.Parse(data.ToArray());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Chunks, Is.Empty);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("header"));
    }

    [Test]
    public void Parse_TrailingDataAfterDeclaredRiffPayloadReportsWarning()
    {
        List<byte> data = NewRiffHeader("WEBP", 4);
        data.Add(0x99);
        data.Add(0x88);

        WebpRiffParseResult result = WebpRiffParser.Parse(data.ToArray());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Chunks, Is.Empty);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Warning));
        Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(12));
    }

    [Test]
    public void Parse_TruncatedRiffHeaderReportsError()
    {
        WebpRiffParseResult result = WebpRiffParser.Parse(new byte[] { 1, 2, 3 });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Chunks, Is.Empty);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("RIFF header"));
    }

    [Test]
    public void Parse_DeclaredSizeSmallerThanFormTypeReportsError()
    {
        List<byte> data = NewRiffHeader("WEBP", 2);

        WebpRiffParseResult result = WebpRiffParser.Parse(data.ToArray());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Chunks, Is.Empty);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("form type"));
    }

    [Test]
    public void Parse_DeclaredSizeBeyondInputReportsError()
    {
        List<byte> data = NewRiffHeader("WEBP", 20);

        WebpRiffParseResult result = WebpRiffParser.Parse(data.ToArray());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("extends beyond"));
    }

    [Test]
    public void Parse_InvalidRiffSignatureReportsError()
    {
        List<byte> data = NewRiffHeader("WEBP", 4);
        data[0] = (byte)'R';
        data[1] = (byte)'U';
        data[2] = (byte)'F';
        data[3] = (byte)'F';

        WebpRiffParseResult result = WebpRiffParser.Parse(data.ToArray());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.RiffFourCc, Is.EqualTo("RUFF"));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("signature"));
    }

    static List<byte> NewRiffHeader(string formatFourCc, uint declaredSize)
    {
        List<byte> data = new List<byte>();
        AppendFourCc(data, "RIFF");
        WebpEndianWriter.AppendUInt32(data, declaredSize);
        AppendFourCc(data, formatFourCc);
        return data;
    }

    static void AppendChunk(List<byte> data, string fourCc, byte[] payload, int paddingByte)
    {
        AppendFourCc(data, fourCc);
        WebpEndianWriter.AppendUInt32(data, (uint)payload.Length);
        WebpEndianWriter.AppendBytes(data, payload);
        if ((payload.Length & 1) != 0)
        {
            if (paddingByte < 0)
            {
                data.Add(0);
            }
            else
            {
                data.Add((byte)paddingByte);
            }
        }
    }

    static void AppendFourCc(List<byte> data, string fourCc)
    {
        data.Add((byte)fourCc[0]);
        data.Add((byte)fourCc[1]);
        data.Add((byte)fourCc[2]);
        data.Add((byte)fourCc[3]);
    }
}
