using System.Text;

namespace ftp.Tests;

public class FtpReplyParserTests
{
    [Test]
    public void Parser_preserves_single_line_unknown_codes_and_raw_text()
    {
        FtpReplyParser parser = CreateStrictParser();

        FtpReply reply = parser.ParseReplyText("699 Vendor-specific status\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(699));
            Assert.That(reply.Text, Is.EqualTo("Vendor-specific status"));
            Assert.That(reply.Multiline, Is.False);
            Assert.That(reply.RawText, Is.EqualTo("699 Vendor-specific status\r\n"));
            Assert.That(reply.Lines, Has.Length.EqualTo(1));
            Assert.That(reply.Lines[0].HasCode, Is.True);
            Assert.That(reply.Lines[0].Code, Is.EqualTo(699));
            Assert.That(reply.Lines[0].RawText, Is.EqualTo("699 Vendor-specific status"));
        });
    }

    [Test]
    public void Parser_accepts_arbitrary_multiline_content_until_matching_terminator()
    {
        FtpReplyParser parser = CreateStrictParser();
        string text = "211-Features\r\n UTF8\r\n234 unrelated\r\n\r\n211 End\r\n";

        FtpReply reply = parser.ParseReplyText(text);

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(211));
            Assert.That(reply.Text, Is.EqualTo("End"));
            Assert.That(reply.Multiline, Is.True);
            Assert.That(reply.Lines, Has.Length.EqualTo(5));
            Assert.That(reply.Lines[1].HasCode, Is.False);
            Assert.That(reply.Lines[2].HasCode, Is.True);
            Assert.That(reply.Lines[2].TerminatesMultiline, Is.False);
            Assert.That(reply.Lines[3].Text, Is.Empty);
            Assert.That(reply.Lines[4].TerminatesMultiline, Is.True);
            Assert.That(reply.RawText, Is.EqualTo(text));
        });
    }

    [Test]
    public void Parser_requires_a_matching_multiline_terminating_code()
    {
        FtpReplyParser parser = CreateStrictParser();

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => parser.ParseReplyText("211-Features\r\n212 Wrong code\r\n"));
            Assert.Throws<FtpProtocolException>(() => parser.ParseReplyText("211-Features\r\n211-Still open\r\n"));
            Assert.Throws<FtpProtocolException>(() => parser.ParseReplyText("211-Features\r\n211 End\r\n220 trailing\r\n"));
        });
    }

    [Test]
    public void Strict_parser_requires_crlf_and_rfc_code_separators()
    {
        FtpReplyParser parser = CreateStrictParser();

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => parser.ParseReplyText("220 Ready\n"));
            Assert.Throws<FtpProtocolException>(() => parser.ParseReplyText("220 Ready\r"));
            Assert.Throws<FtpProtocolException>(() => parser.ParseReplyText("220Ready\r\n"));
            Assert.Throws<FtpProtocolException>(() => parser.ParseReplyText("220 Ready"));
            Assert.Throws<FtpProtocolException>(() => parser.ParseReplyText("Ready\r\n"));
        });
    }

    [Test]
    public void Lenient_parser_accepts_mixed_line_endings_and_compact_replies()
    {
        FtpReplyParser parser = new FtpReplyParser(FtpDefaults.MaximumReplyBytes, FtpDefaults.MaximumReplyLines, false);

        FtpReply compact = parser.ParseReplyText("220ready\n");
        FtpReply multiline = parser.ParseReplyText("211-Features\r UTF8\n211 End\r");

        Assert.Multiple(() =>
        {
            Assert.That(compact.Code, Is.EqualTo(220));
            Assert.That(compact.Text, Is.EqualTo("ready"));
            Assert.That(multiline.Multiline, Is.True);
            Assert.That(multiline.Lines, Has.Length.EqualTo(3));
            Assert.That(multiline.Text, Is.EqualTo("End"));
        });
    }

    [Test]
    public void Parser_preserves_blank_reply_text()
    {
        FtpReplyParser parser = CreateStrictParser();

        FtpReply single = parser.ParseReplyText("220 \r\n");
        FtpReply multiline = parser.ParseReplyText("211-\r\n211 \r\n");

        Assert.Multiple(() =>
        {
            Assert.That(single.Text, Is.Empty);
            Assert.That(multiline.Lines[0].Text, Is.Empty);
            Assert.That(multiline.Text, Is.Empty);
        });
    }

    [Test]
    public void Parser_enforces_aggregate_byte_and_physical_line_limits()
    {
        FtpReplyParser byteLimited = new FtpReplyParser(10, 4, true);
        FtpReplyParser lineLimited = new FtpReplyParser(100, 2, true);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => byteLimited.ParseReplyText("220 ok\r\n"));
            Assert.Throws<FtpProtocolException>(() => byteLimited.ParseReplyText("220 ready\r\n"));
            Assert.Throws<FtpProtocolException>(() =>
                lineLimited.ParseReplyText("211-start\r\none\r\ntwo\r\n211 end\r\n"));
        });
    }

    [Test]
    public void Encoded_parser_rejects_invalid_negotiated_text()
    {
        FtpReplyParser parser = CreateStrictParser();
        byte[] invalidUtf8 = new byte[] { 50, 50, 48, 32, 192, 175, 13, 10 };
        byte[] nonAscii = new byte[] { 50, 50, 48, 32, 233, 13, 10 };
        byte[] valid = Encoding.UTF8.GetBytes("220 café\r\n");

        FtpReply reply = parser.ParseEncodedReply(valid, Encoding.UTF8);

        Assert.Multiple(() =>
        {
            Assert.That(reply.Text, Is.EqualTo("café"));
            Assert.Throws<FtpProtocolException>(() => parser.ParseEncodedReply(invalidUtf8, Encoding.UTF8));
            Assert.Throws<FtpProtocolException>(() => parser.ParseEncodedReply(nonAscii, Encoding.ASCII));
            Assert.Throws<FtpConfigurationException>(() => parser.ParseEncodedReply(valid, Encoding.Unicode));
        });
    }

    private static FtpReplyParser CreateStrictParser()
    {
        return new FtpReplyParser(FtpDefaults.MaximumReplyBytes, FtpDefaults.MaximumReplyLines, true);
    }
}
