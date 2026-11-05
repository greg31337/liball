using System.Text;

namespace ftp.Tests;

public class FtpCommandParserTests
{
    [Test]
    public void Parser_classifies_case_insensitively_and_preserves_raw_argument_text()
    {
        FtpCommand command = FtpCommandParser.ParseDecodedCommand("cWd  leading and trailing ");

        Assert.Multiple(() =>
        {
            Assert.That(command.Kind, Is.EqualTo(FtpCommandKind.Cwd));
            Assert.That(command.Verb, Is.EqualTo(FtpCommandNames.Cwd));
            Assert.That(command.Argument, Is.EqualTo(" leading and trailing "));
            Assert.That(command.RawText, Is.EqualTo("cWd  leading and trailing "));
        });
    }

    [Test]
    public void Parser_distinguishes_absent_empty_and_unknown_command_arguments()
    {
        FtpCommand absent = FtpCommandParser.ParseDecodedCommand("NOOP");
        FtpCommand empty = FtpCommandParser.ParseDecodedCommand("NOOP ");
        FtpCommand unknown = FtpCommandParser.ParseDecodedCommand("x-vendor value");

        Assert.Multiple(() =>
        {
            Assert.That(absent.Argument, Is.Null);
            Assert.That(empty.Argument, Is.EqualTo(""));
            Assert.That(unknown.Kind, Is.EqualTo(FtpCommandKind.Unknown));
            Assert.That(unknown.Verb, Is.EqualTo("X-VENDOR"));
            Assert.That(unknown.Argument, Is.EqualTo("value"));
            Assert.That(unknown.RawText, Is.EqualTo("x-vendor value"));
        });
    }

    [Test]
    public void Parser_rejects_invalid_verb_whitespace_and_embedded_controls()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand(""));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand(" NOOP"));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand("1CMD value"));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand("NOOP\tvalue"));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand("CWD bad\rpath"));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand("CWD bad\0path"));
        });
    }

    [Test]
    public void Encoded_parser_applies_negotiated_ascii_and_utf8_strictly()
    {
        FtpCommand unicode = FtpCommandParser.ParseEncodedCommand(
            Encoding.UTF8.GetBytes("CWD café/資料"), Encoding.UTF8);

        byte[] invalidUtf8 = new byte[] { 67, 87, 68, 32, 192, 175 };
        byte[] nonAscii = new byte[] { 67, 87, 68, 32, 233 };

        Assert.Multiple(() =>
        {
            Assert.That(unicode.Argument, Is.EqualTo("café/資料"));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseEncodedCommand(invalidUtf8, Encoding.UTF8));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseEncodedCommand(nonAscii, Encoding.ASCII));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpCommandParser.ParseEncodedCommand(Encoding.Unicode.GetBytes("NOOP"), Encoding.Unicode));
        });
    }

    [Test]
    public void Parser_accepts_bounded_numeric_command_grammars()
    {
        FtpCommand rest = FtpCommandParser.ParseDecodedCommand("REST 9223372036854775807");
        FtpCommand pbsz = FtpCommandParser.ParseDecodedCommand("PBSZ 0");
        FtpCommand allocation = FtpCommandParser.ParseDecodedCommand("ALLO 4096 R 512");
        FtpCommand epsvProtocol = FtpCommandParser.ParseDecodedCommand("EPSV 2");
        FtpCommand epsvAll = FtpCommandParser.ParseDecodedCommand("EPSV all");

        Assert.Multiple(() =>
        {
            Assert.That(rest.Kind, Is.EqualTo(FtpCommandKind.Rest));
            Assert.That(pbsz.Argument, Is.EqualTo("0"));
            Assert.That(allocation.Argument, Is.EqualTo("4096 R 512"));
            Assert.That(epsvProtocol.Argument, Is.EqualTo("2"));
            Assert.That(epsvAll.Argument, Is.EqualTo("all"));
        });
    }

    [Test]
    public void Parser_rejects_numeric_signs_overflow_ranges_and_spacing_errors()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand("REST +1"));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand("REST 9223372036854775808"));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand("PBSZ"));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand("ALLO 4  R 2"));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand("ALLO 4 R"));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand("EPSV 0"));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand("EPSV "));
        });
    }

    [Test]
    public void Numeric_helpers_accept_boundaries_without_overflow()
    {
        long maximum = FtpCommandParser.ParseNonNegativeInt64("9223372036854775807", "test");
        int protocol = FtpCommandParser.ParseInt32InRange("2", 1, 2, "test");

        Assert.Multiple(() =>
        {
            Assert.That(maximum, Is.EqualTo(long.MaxValue));
            Assert.That(protocol, Is.EqualTo(2));
            Assert.Throws<FtpProtocolException>(() =>
                FtpCommandParser.ParseInt32InRange("2147483648", 0, int.MaxValue, "test"));
        });
    }
}
