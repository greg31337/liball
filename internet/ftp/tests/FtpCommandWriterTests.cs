using System.Text;

namespace ftp.Tests;

public class FtpCommandWriterTests
{
    [Test]
    public void Writer_uses_canonical_kind_verb_and_preserves_argument_text()
    {
        FtpCommand command = new FtpCommand();
        command.Kind = FtpCommandKind.Cwd;
        command.Verb = "wrong";
        command.Argument = " leading and trailing ";

        string formatted = FtpCommandWriter.FormatDecodedCommand(command);

        Assert.That(formatted, Is.EqualTo("CWD  leading and trailing \r\n"));
    }

    [Test]
    public void Writer_distinguishes_absent_empty_and_unknown_arguments()
    {
        FtpCommand absent = new FtpCommand();
        absent.Kind = FtpCommandKind.Noop;
        FtpCommand empty = new FtpCommand();
        empty.Kind = FtpCommandKind.Noop;
        empty.Argument = "";
        FtpCommand unknown = new FtpCommand();
        unknown.Verb = "x-vendor";
        unknown.Argument = "value";

        Assert.Multiple(() =>
        {
            Assert.That(FtpCommandWriter.FormatDecodedCommand(absent), Is.EqualTo("NOOP\r\n"));
            Assert.That(FtpCommandWriter.FormatDecodedCommand(empty), Is.EqualTo("NOOP \r\n"));
            Assert.That(FtpCommandWriter.FormatDecodedCommand(unknown), Is.EqualTo("X-VENDOR value\r\n"));
        });
    }

    [Test]
    public void Writer_rejects_argument_injection_invalid_verbs_and_numeric_fields()
    {
        FtpCommand injected = new FtpCommand();
        injected.Kind = FtpCommandKind.Cwd;
        injected.Argument = "safe\r\nDELE victim";
        FtpCommand invalidVerb = new FtpCommand();
        invalidVerb.Verb = "1BAD";
        FtpCommand invalidRest = new FtpCommand();
        invalidRest.Kind = FtpCommandKind.Rest;
        invalidRest.Argument = "-1";

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => FtpCommandWriter.FormatDecodedCommand(injected));
            Assert.Throws<FtpProtocolException>(() => FtpCommandWriter.FormatDecodedCommand(invalidVerb));
            Assert.Throws<FtpProtocolException>(() => FtpCommandWriter.FormatDecodedCommand(invalidRest));
        });
    }

    [Test]
    public void Encoded_writer_emits_strict_utf8_or_ascii_with_canonical_crlf()
    {
        FtpCommand unicode = new FtpCommand();
        unicode.Kind = FtpCommandKind.Cwd;
        unicode.Argument = "café/資料";
        byte[] utf8 = FtpCommandWriter.FormatEncodedCommand(unicode, Encoding.UTF8);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.UTF8.GetString(utf8), Is.EqualTo("CWD café/資料\r\n"));
            Assert.Throws<FtpProtocolException>(() => FtpCommandWriter.FormatEncodedCommand(unicode, Encoding.ASCII));
            Assert.Throws<FtpConfigurationException>(() => FtpCommandWriter.FormatEncodedCommand(unicode, Encoding.Unicode));
        });
    }

    [Test]
    public void Writer_accepts_exact_maximum_encoded_line_and_rejects_one_more_byte()
    {
        FtpCommand exact = new FtpCommand();
        exact.Kind = FtpCommandKind.Cwd;
        exact.Argument = new string('a', FtpDefaults.MaximumControlLineBytes - 6);
        FtpCommand oversized = new FtpCommand();
        oversized.Kind = FtpCommandKind.Cwd;
        oversized.Argument = new string('a', FtpDefaults.MaximumControlLineBytes - 5);

        string formatted = FtpCommandWriter.FormatDecodedCommand(exact);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.UTF8.GetByteCount(formatted), Is.EqualTo(FtpDefaults.MaximumControlLineBytes));
            Assert.Throws<FtpProtocolException>(() => FtpCommandWriter.FormatDecodedCommand(oversized));
            Assert.Throws<FtpProtocolException>(() => FtpCommandWriter.FormatEncodedCommand(oversized, Encoding.UTF8));
        });
    }

    [Test]
    public void Parser_and_writer_classify_secret_command_payloads_for_redaction()
    {
        FtpCommand parsedPassword = FtpCommandParser.ParseDecodedCommand("PASS secret");
        FtpCommand parsedSecurityData = FtpCommandParser.ParseDecodedCommand("ADAT token");
        FtpCommand parsedPath = FtpCommandParser.ParseDecodedCommand("CWD secret");
        FtpCommand outbound = new FtpCommand();
        outbound.Kind = FtpCommandKind.Conf;
        outbound.Argument = "protected-token";

        FtpCommandWriter.FormatDecodedCommand(outbound);

        Assert.Multiple(() =>
        {
            Assert.That(parsedPassword.Sensitive, Is.True);
            Assert.That(parsedSecurityData.Sensitive, Is.True);
            Assert.That(parsedPath.Sensitive, Is.False);
            Assert.That(outbound.Sensitive, Is.True);
        });
    }
}
