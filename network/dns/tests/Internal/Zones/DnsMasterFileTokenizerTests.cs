namespace dns.Tests.Internal.Zones;

public class DnsMasterFileTokenizerTests
{
    [Test]
    public void TokenizesLogicalRecordsAndRetainsSourceLocations()
    {
        string source = "$ORIGIN example.\r\n" +
            "www 300 IN A 192.0.2.1 ; address\r\n";

        DnsMasterFileTokenizeResult result = Tokenize(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Records, Has.Count.EqualTo(2));
            Assert.That(result.Records[0].Tokens[0].Text,
                Is.EqualTo("$ORIGIN"));
            Assert.That(result.Records[0].Tokens[0].Line, Is.EqualTo(1));
            Assert.That(result.Records[0].Tokens[0].Column, Is.EqualTo(1));
            Assert.That(result.Records[0].Tokens[1].Column, Is.EqualTo(9));
            Assert.That(result.Records[1].Tokens[4].Text,
                Is.EqualTo("192.0.2.1"));
            Assert.That(result.Records[1].Tokens[4].Line, Is.EqualTo(2));
            Assert.That(result.Records[1].Tokens[4].Column, Is.EqualTo(14));
            Assert.That(result.Comments, Has.Count.EqualTo(1));
            Assert.That(result.Comments[0].Text, Is.EqualTo(" address"));
            Assert.That(result.Comments[0].Line, Is.EqualTo(2));
            Assert.That(result.Comments[0].Column, Is.EqualTo(24));
            Assert.That(result.Comments[0].SourcePath,
                Is.EqualTo("example.zone"));
        });
    }

    [Test]
    public void QuotesRetainRawSpellingAndDecodeEscapedOctets()
    {
        string source = "name TXT \"hello world; still text\" " +
            "\"a\\\"b\\032c\\000\"\n";

        DnsMasterFileTokenizeResult result = Tokenize(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Records[0].Tokens, Has.Count.EqualTo(4));
            Assert.That(result.Records[0].Tokens[2].Kind,
                Is.EqualTo(DnsMasterFileTokenKind.QuotedString));
            Assert.That(result.Records[0].Tokens[2].Text,
                Is.EqualTo("hello world; still text"));
            Assert.That(result.Comments, Is.Empty);
            Assert.That(result.Records[0].Tokens[3].Text.ToCharArray(),
                Is.EqualTo(new char[] { 'a', '"', 'b', ' ', 'c', '\0' }));
            Assert.That(result.Records[0].Tokens[3].RawText,
                Is.EqualTo("\"a\\\"b\\032c\\000\""));
        });
    }

    [Test]
    public void ParenthesesJoinPhysicalLinesAndIgnoreInteriorComments()
    {
        string source = "example. 300 IN SOA (\n" +
            " ns.example.\n" +
            " hostmaster.example.\n" +
            " 7 ; serial\n" +
            " 3600 600 86400 300\n" +
            ")\n" +
            "next.example. 60 IN A 192.0.2.2\n";

        DnsMasterFileTokenizeResult result = Tokenize(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Records, Has.Count.EqualTo(2));
            Assert.That(result.Records[0].Tokens, Has.Count.EqualTo(11));
            Assert.That(result.Records[0].Tokens[4].Text,
                Is.EqualTo("ns.example."));
            Assert.That(result.Records[0].Tokens[4].Line, Is.EqualTo(2));
            Assert.That(result.Records[0].Tokens[4].Column, Is.EqualTo(2));
            Assert.That(result.Records[0].OwnerOmitted, Is.False);
            Assert.That(result.Comments[0].Text, Is.EqualTo(" serial"));
            Assert.That(result.Records[1].Tokens[0].Text,
                Is.EqualTo("next.example."));
        });
    }

    [Test]
    public void LeadingWhitespaceMarksOmittedOwnersPerLogicalRecord()
    {
        string source = "first.example. 60 IN A 192.0.2.1\n" +
            "  60 IN AAAA 2001:db8::1\n" +
            "\t60 IN TXT \"same owner\"\n" +
            "last.example. 60 IN A 192.0.2.2\n";

        DnsMasterFileTokenizeResult result = Tokenize(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Records[0].OwnerOmitted, Is.False);
            Assert.That(result.Records[1].OwnerOmitted, Is.True);
            Assert.That(result.Records[1].Column, Is.EqualTo(3));
            Assert.That(result.Records[2].OwnerOmitted, Is.True);
            Assert.That(result.Records[2].Column, Is.EqualTo(2));
            Assert.That(result.Records[3].OwnerOmitted, Is.False);
        });
    }

    [Test]
    public void EscapesProtectDelimitersAndContinueTokensAcrossLines()
    {
        string source = "escaped\\ value escaped\\;semicolon " +
            "joined\\\nname\n";

        DnsMasterFileTokenizeResult result = Tokenize(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Records, Has.Count.EqualTo(1));
            Assert.That(result.Records[0].Tokens, Has.Count.EqualTo(3));
            Assert.That(result.Records[0].Tokens[0].Text,
                Is.EqualTo("escaped value"));
            Assert.That(result.Records[0].Tokens[1].Text,
                Is.EqualTo("escaped;semicolon"));
            Assert.That(result.Records[0].Tokens[2].Text,
                Is.EqualTo("joinedname"));
            Assert.That(result.Records[0].Tokens[2].EndLine, Is.EqualTo(2));
            Assert.That(result.Comments, Is.Empty);
        });
    }

    [Test]
    public void BlankAndCommentOnlyLinesDoNotCreateRecords()
    {
        string source = "\n  ; first\n\t\n;second\n" +
            "example. 60 IN A 192.0.2.1\n";

        DnsMasterFileTokenizeResult result = Tokenize(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Records, Has.Count.EqualTo(1));
            Assert.That(result.Records[0].OwnerOmitted, Is.False);
            Assert.That(result.Records[0].Line, Is.EqualTo(5));
            Assert.That(result.Comments, Has.Count.EqualTo(2));
        });
    }

    [TestCase("name TXT \"unterminated", 1, 10)]
    [TestCase("name ( value\n", 2, 1)]
    [TestCase("name ) value\n", 1, 6)]
    [TestCase("name (( value ))\n", 1, 7)]
    [TestCase("name bad\\999\n", 1, 9)]
    [TestCase("name bad\\12x\n", 1, 9)]
    public void MalformedLexicalFormsReturnLocatedDiagnostics(
        string source,
        int line,
        int column)
    {
        DnsMasterFileTokenizeResult result = Tokenize(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidText));
            Assert.That(result.Diagnostics[0].SourcePath,
                Is.EqualTo("example.zone"));
            Assert.That(result.Diagnostics[0].Line, Is.EqualTo(line));
            Assert.That(result.Diagnostics[0].Column, Is.EqualTo(column));
        });
    }

    [Test]
    public void RecordLimitReturnsPartialRecordsAndResourceDiagnostic()
    {
        DnsZoneLoadOptions options = new DnsZoneLoadOptions();
        options.MaximumRecords = 1;
        DnsMasterFileTokenizeResult result = DnsMasterFileTokenizer.Tokenize(
            "one 60 IN A 192.0.2.1\ntwo 60 IN A 192.0.2.2\n",
            "limited.zone",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Records, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(result.Diagnostics[0].SourcePath,
                Is.EqualTo("limited.zone"));
        });
    }

    [Test]
    public void TokenLengthLimitRejectsUnboundedAtoms()
    {
        string source = new string(
            'a', DnsMasterFileTokenizer.MaximumTokenLength + 1);

        DnsMasterFileTokenizeResult result = Tokenize(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(result.Diagnostics[0].HasLine, Is.True);
            Assert.That(result.Diagnostics[0].HasColumn, Is.True);
        });
    }

    static DnsMasterFileTokenizeResult Tokenize(string source)
    {
        return DnsMasterFileTokenizer.Tokenize(
            source,
            "example.zone",
            new DnsZoneLoadOptions());
    }
}
