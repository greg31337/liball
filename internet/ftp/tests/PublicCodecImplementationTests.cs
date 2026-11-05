using System.Text;

namespace ftp.Tests;

public class PublicCodecImplementationTests
{
    [Test]
    public void Command_codec_parses_known_unknown_and_unicode_commands_through_its_contract()
    {
        IFtpCommandCodec codec = new FtpCommandCodec();

        FtpCommand known = codec.ParseCommand("cwd café/資料");
        FtpCommand unknown = codec.ParseCommand("x-vendor value");
        string formatted = codec.FormatCommand(known);

        Assert.Multiple(() =>
        {
            Assert.That(known.Kind, Is.EqualTo(FtpCommandKind.Cwd));
            Assert.That(known.Verb, Is.EqualTo(FtpCommandNames.Cwd));
            Assert.That(known.Argument, Is.EqualTo("café/資料"));
            Assert.That(known.RawText, Is.EqualTo("cwd café/資料"));
            Assert.That(unknown.Kind, Is.EqualTo(FtpCommandKind.Unknown));
            Assert.That(unknown.Verb, Is.EqualTo("X-VENDOR"));
            Assert.That(formatted, Is.EqualTo("CWD café/資料\r\n"));
        });
    }

    [Test]
    public void Command_codec_rejects_null_injection_invalid_unicode_and_oversized_lines()
    {
        IFtpCommandCodec codec = new FtpCommandCodec();
        string unpairedSurrogate = "CWD " + '\ud800';
        string oversized = "CWD " + new string('a', FtpDefaults.MaximumControlLineBytes);
        FtpCommand injected = new FtpCommand();
        injected.Kind = FtpCommandKind.Cwd;
        injected.Argument = "safe\r\nDELE victim";

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => codec.ParseCommand(null!));
            Assert.Throws<FtpProtocolException>(() => codec.ParseCommand("CWD bad\0path"));
            Assert.Throws<FtpProtocolException>(() => codec.ParseCommand(unpairedSurrogate));
            Assert.Throws<FtpProtocolException>(() => codec.ParseCommand(oversized));
            Assert.Throws<FtpProtocolException>(() => codec.FormatCommand(injected));
            Assert.Throws<ArgumentNullException>(() => codec.FormatCommand(null!));
        });
    }

    [Test]
    public void Reply_codec_reads_exactly_one_reply_and_leaves_the_next_reply_buffered()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        byte[] bytes = Encoding.UTF8.GetBytes("220 Ready\r\n200 Next\r\n");
        using MemoryStream source = new MemoryStream(bytes);

        FtpReply first = codec.ReadReply(source);
        long positionAfterFirst = source.Position;
        FtpReply second = codec.ReadReply(source);

        Assert.Multiple(() =>
        {
            Assert.That(first.Code, Is.EqualTo(FtpReplyCodes.ServiceReady));
            Assert.That(first.Text, Is.EqualTo("Ready"));
            Assert.That(first.RawText, Is.EqualTo("220 Ready\r\n"));
            Assert.That(positionAfterFirst, Is.EqualTo(11));
            Assert.That(second.Code, Is.EqualTo(FtpReplyCodes.CommandOkay));
            Assert.That(second.Text, Is.EqualTo("Next"));
            Assert.That(source.Position, Is.EqualTo(bytes.Length));
        });
    }

    [Test]
    public void Reply_codec_reads_strict_multiline_utf8_replies()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        string wire = "211-功能\r\n UTF8\r\n café\r\n211 完了\r\n";
        using MemoryStream source = new MemoryStream(Encoding.UTF8.GetBytes(wire));

        FtpReply reply = codec.ReadReply(source);

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(FtpReplyCodes.SystemStatus));
            Assert.That(reply.Multiline, Is.True);
            Assert.That(reply.Lines, Has.Length.EqualTo(4));
            Assert.That(reply.Lines[1].Text, Is.EqualTo(" UTF8"));
            Assert.That(reply.Text, Is.EqualTo("完了"));
            Assert.That(reply.RawText, Is.EqualTo(wire));
        });
    }

    [Test]
    public void Reply_codec_rejects_truncated_invalid_utf8_and_overlong_stream_input()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        using MemoryStream truncated = new MemoryStream(
            Encoding.UTF8.GetBytes("211-Features\r\n UTF8\r\n"));
        using MemoryStream bareLineFeed = new MemoryStream(
            Encoding.UTF8.GetBytes("220 Ready\n"));
        using MemoryStream invalidUtf8 = new MemoryStream(
            new byte[] { 50, 50, 48, 32, 192, 175, 13, 10 });
        string longLine = "220 " + new string('a', FtpDefaults.MaximumControlLineBytes);
        using MemoryStream oversized = new MemoryStream(Encoding.UTF8.GetBytes(longLine + "\r\n"));

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => codec.ReadReply(truncated));
            Assert.Throws<FtpProtocolException>(() => codec.ReadReply(bareLineFeed));
            Assert.Throws<FtpProtocolException>(() => codec.ReadReply(invalidUtf8));
            Assert.Throws<FtpProtocolException>(() => codec.ReadReply(oversized));
            Assert.Throws<ArgumentNullException>(() => codec.ReadReply(null!));
        });
    }

    [Test]
    public void Reply_codec_writes_single_and_multiline_utf8_without_owning_the_stream()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        using MemoryStream destination = new MemoryStream();
        FtpReply ready = new FtpReply();
        ready.Code = FtpReplyCodes.ServiceReady;
        ready.Text = "Prêt";
        codec.WriteReply(destination, ready);

        FtpReply multiline = CreateMultilineReply();
        codec.WriteReply(destination, multiline);

        string wire = Encoding.UTF8.GetString(destination.ToArray());
        Assert.Multiple(() =>
        {
            Assert.That(wire, Is.EqualTo(
                "220 Prêt\r\n211-Features\r\n UTF8\r\n211 End\r\n"));
            Assert.That(destination.CanWrite, Is.True);
        });
    }

    [Test]
    public void Reply_codec_validates_write_arguments_and_reply_framing()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        using MemoryStream destination = new MemoryStream();
        FtpReply invalid = new FtpReply();
        invalid.Code = 699;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => codec.WriteReply(null!, invalid));
            Assert.Throws<ArgumentNullException>(() => codec.WriteReply(destination, null!));
            Assert.Throws<FtpProtocolException>(() => codec.WriteReply(destination, invalid));
        });
    }

    [Test]
    public void Reply_codec_classification_is_exceptionless_for_unknown_numeric_codes()
    {
        IFtpReplyCodec codec = new FtpReplyCodec();
        FtpReply success = new FtpReply();
        success.Code = FtpReplyCodes.CommandOkay;
        FtpReply unknown = new FtpReply();
        unknown.Code = 699;

        FtpReplyClassification successful = codec.ClassifyReply(success);
        FtpReplyClassification malformed = codec.ClassifyReply(unknown);

        Assert.Multiple(() =>
        {
            Assert.That(successful.Succeeded, Is.True);
            Assert.That(success.Classification, Is.SameAs(successful));
            Assert.That(malformed.Malformed, Is.True);
            Assert.That(malformed.Category, Is.EqualTo(FtpReplyCategory.Unknown));
            Assert.That(unknown.Classification, Is.SameAs(malformed));
            Assert.Throws<ArgumentNullException>(() => codec.ClassifyReply(null!));
        });
    }

    private static FtpReply CreateMultilineReply()
    {
        FtpReply reply = new FtpReply();
        reply.Code = FtpReplyCodes.SystemStatus;
        reply.Text = "End";
        reply.Multiline = true;
        FtpReplyLine opening = new FtpReplyLine();
        opening.HasCode = true;
        opening.Code = FtpReplyCodes.SystemStatus;
        opening.Text = "Features";
        FtpReplyLine feature = new FtpReplyLine();
        feature.Text = " UTF8";
        FtpReplyLine terminal = new FtpReplyLine();
        terminal.HasCode = true;
        terminal.Code = FtpReplyCodes.SystemStatus;
        terminal.Text = "End";
        reply.Lines = new FtpReplyLine[] { opening, feature, terminal };
        return reply;
    }
}
