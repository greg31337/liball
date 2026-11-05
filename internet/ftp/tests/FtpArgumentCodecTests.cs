namespace ftp.Tests;

public class FtpArgumentCodecTests
{
    [Test]
    public void Host_codec_accepts_domains_and_address_literals_without_changing_spelling()
    {
        string domain = FtpHostCodec.ParseHostArgument("Files.Example-Host.test");
        string ipv4 = FtpHostCodec.ParseHostArgument("192.0.2.10");
        string ipv6 = FtpHostCodec.ParseHostArgument("[2001:db8::10]");

        Assert.Multiple(() =>
        {
            Assert.That(domain, Is.EqualTo("Files.Example-Host.test"));
            Assert.That(ipv4, Is.EqualTo("192.0.2.10"));
            Assert.That(ipv6, Is.EqualTo("[2001:db8::10]"));
            Assert.That(FtpHostCodec.FormatHostArgument(domain), Is.EqualTo(domain));
        });
    }

    [Test]
    public void Host_codec_rejects_ports_unbracketed_ipv6_and_invalid_dns_labels()
    {
        string longLabel = new string('a', 64) + ".test";

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => FtpHostCodec.ParseHostArgument(null));
            Assert.Throws<FtpProtocolException>(() => FtpHostCodec.ParseHostArgument("example.test:21"));
            Assert.Throws<FtpProtocolException>(() => FtpHostCodec.ParseHostArgument("2001:db8::10"));
            Assert.Throws<FtpProtocolException>(() => FtpHostCodec.ParseHostArgument("[fe80::1%3]"));
            Assert.Throws<FtpProtocolException>(() => FtpHostCodec.ParseHostArgument("-bad.example"));
            Assert.Throws<FtpProtocolException>(() => FtpHostCodec.ParseHostArgument("bad_.example"));
            Assert.Throws<FtpProtocolException>(() => FtpHostCodec.ParseHostArgument("example.test."));
            Assert.Throws<FtpProtocolException>(() => FtpHostCodec.ParseHostArgument(longLabel));
        });
    }

    [Test]
    public void Language_tag_codec_normalizes_case_and_enforces_rfc_subtags()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FtpArgumentCodec.ParseLanguageTag("EN-us"), Is.EqualTo("en-us"));
            Assert.That(FtpArgumentCodec.FormatLanguageTag("ZH-hant-TW"), Is.EqualTo("zh-hant-tw"));
            Assert.Throws<FtpProtocolException>(() => FtpArgumentCodec.ParseLanguageTag(""));
            Assert.Throws<FtpProtocolException>(() => FtpArgumentCodec.ParseLanguageTag("en--us"));
            Assert.Throws<FtpProtocolException>(() => FtpArgumentCodec.ParseLanguageTag("en-US1"));
            Assert.Throws<FtpProtocolException>(() => FtpArgumentCodec.ParseLanguageTag("abcdefghi"));
        });
    }

    [Test]
    public void Quoted_pathname_codec_round_trips_embedded_quotes_and_accepts_reply_text()
    {
        string encoded = FtpPathCodec.FormatQuotedPathname("/reports/\"final\"");
        string decoded = FtpPathCodec.ParseQuotedPathname(encoded + " directory created");

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo("\"/reports/\"\"final\"\"\""));
            Assert.That(decoded, Is.EqualTo("/reports/\"final\""));
            Assert.That(FtpPathCodec.ParseQuotedPathname("\"\""), Is.Empty);
        });
    }

    [Test]
    public void Quoted_pathname_codec_rejects_ambiguous_suffixes_controls_and_missing_quotes()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => FtpPathCodec.ParseQuotedPathname("plain"));
            Assert.Throws<FtpProtocolException>(() => FtpPathCodec.ParseQuotedPathname("\"open"));
            Assert.Throws<FtpProtocolException>(() => FtpPathCodec.ParseQuotedPathname("\"path\"suffix"));
            Assert.Throws<FtpProtocolException>(() => FtpPathCodec.ParseQuotedPathname("\"path\" bad\rreply"));
            Assert.Throws<FtpProtocolException>(() => FtpPathCodec.FormatQuotedPathname("bad\npath"));
        });
    }

    [Test]
    public void Allocation_codec_round_trips_plain_and_record_allocations()
    {
        FtpAllocationArgument plain = FtpArgumentCodec.ParseAllocateArgument("9223372036854775807");
        FtpAllocationArgument records = FtpArgumentCodec.ParseAllocateArgument("4096 r 512");

        Assert.Multiple(() =>
        {
            Assert.That(plain.ByteCount, Is.EqualTo(long.MaxValue));
            Assert.That(plain.HasRecordSize, Is.False);
            Assert.That(FtpArgumentCodec.FormatAllocateArgument(plain),
                Is.EqualTo("9223372036854775807"));
            Assert.That(records.ByteCount, Is.EqualTo(4096));
            Assert.That(records.HasRecordSize, Is.True);
            Assert.That(records.RecordSize, Is.EqualTo(512));
            Assert.That(FtpArgumentCodec.FormatAllocateArgument(records), Is.EqualTo("4096 R 512"));
        });
    }

    [Test]
    public void Allocation_codec_rejects_malformed_overflow_and_inconsistent_values()
    {
        FtpAllocationArgument inconsistent = new FtpAllocationArgument();
        inconsistent.ByteCount = 1;
        inconsistent.RecordSize = 2;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => FtpArgumentCodec.ParseAllocateArgument("4  R 2"));
            Assert.Throws<FtpProtocolException>(() => FtpArgumentCodec.ParseAllocateArgument("4 R"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpArgumentCodec.ParseAllocateArgument("9223372036854775808"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpArgumentCodec.FormatAllocateArgument(inconsistent));
        });
    }

    [Test]
    public void Restart_and_type_local_codecs_enforce_numeric_bounds_and_canonical_output()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FtpArgumentCodec.ParseRestartMarker("9223372036854775807"),
                Is.EqualTo(long.MaxValue));
            Assert.That(FtpArgumentCodec.FormatRestartMarker(0), Is.EqualTo("0"));
            Assert.That(FtpArgumentCodec.ParseTypeLocalArgument("l 1"), Is.EqualTo(1));
            Assert.That(FtpArgumentCodec.ParseTypeLocalArgument("L 255"), Is.EqualTo(255));
            Assert.That(FtpArgumentCodec.FormatTypeLocalArgument(8), Is.EqualTo("L 8"));
            Assert.Throws<FtpProtocolException>(() => FtpArgumentCodec.FormatRestartMarker(-1));
            Assert.Throws<FtpProtocolException>(() => FtpArgumentCodec.ParseRestartMarker("1.0"));
            Assert.Throws<FtpProtocolException>(() => FtpArgumentCodec.ParseTypeLocalArgument("L 0"));
            Assert.Throws<FtpProtocolException>(() => FtpArgumentCodec.ParseTypeLocalArgument("L 256"));
            Assert.Throws<FtpProtocolException>(() => FtpArgumentCodec.ParseTypeLocalArgument("L  8"));
        });
    }

    [Test]
    public void Command_parser_applies_the_new_argument_grammars()
    {
        FtpCommand host = FtpCommandParser.ParseDecodedCommand("HOST ftp.example.test");
        FtpCommand defaultLanguage = FtpCommandParser.ParseDecodedCommand("LANG");
        FtpCommand language = FtpCommandParser.ParseDecodedCommand("LANG en-US");
        FtpCommand localByte = FtpCommandParser.ParseDecodedCommand("TYPE L 8");

        Assert.Multiple(() =>
        {
            Assert.That(host.Kind, Is.EqualTo(FtpCommandKind.Host));
            Assert.That(defaultLanguage.Argument, Is.Null);
            Assert.That(language.Kind, Is.EqualTo(FtpCommandKind.Lang));
            Assert.That(localByte.Kind, Is.EqualTo(FtpCommandKind.Type));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand("HOST ftp.example:21"));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand("LANG en_US"));
            Assert.Throws<FtpProtocolException>(() => FtpCommandParser.ParseDecodedCommand("TYPE L 0"));
        });
    }
}
