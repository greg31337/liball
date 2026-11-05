using whois;

namespace whois.Tests;

public class ResponseParserTests
{
    [Test]
    public void ParsesOrderedLinesFieldsContinuationsDuplicatesAndRecords()
    {
        WhoisTextResponse text = CreateDecoded(
            "% registry comment\r\n" +
            "Domain Name: EXAMPLE.COM\r\n" +
            "Status: active\r\n" +
            "Status: locked\r\n" +
            "Remarks: primary  \r\n" +
            "  continuation value  \r\n" +
            "Referral: whois://whois.example:43/path\r\n" +
            "Empty:   \r\n" +
            "\r\n" +
            "plain disclaimer\r\n" +
            "\r\n" +
            "Domain Name: SECOND.EXAMPLE");
        IWhoisResponseParser parser = new WhoisResponseParser();

        WhoisParsedResponse response = parser.ParseResponse(text, new WhoisLimits());

        Assert.That(response.TextResponse, Is.SameAs(text));
        Assert.That(response.IsParsed, Is.True);
        Assert.That(response.Error, Is.Null);
        Assert.That(response.Lines, Has.Length.EqualTo(12));
        Assert.That(response.Fields, Has.Length.EqualTo(7));
        Assert.That(response.Records, Has.Length.EqualTo(2));
        Assert.That(response.Lines[0].Kind, Is.EqualTo(WhoisLineKind.Comment));
        Assert.That(response.Lines[0].RawContent, Is.EqualTo("% registry comment"));
        Assert.That(response.Lines[1].Kind, Is.EqualTo(WhoisLineKind.Field));
        Assert.That(response.Lines[1].FieldIndex, Is.EqualTo(0));
        Assert.That(response.Lines[5].Kind, Is.EqualTo(WhoisLineKind.Continuation));
        Assert.That(response.Lines[5].FieldIndex, Is.EqualTo(3));
        Assert.That(response.Lines[5].RawContent, Is.EqualTo("  continuation value  "));
        Assert.That(response.Lines[5].NormalizedContent, Is.EqualTo("continuation value"));
        Assert.That(response.Lines[8].Kind, Is.EqualTo(WhoisLineKind.Blank));
        Assert.That(response.Lines[9].Kind, Is.EqualTo(WhoisLineKind.Text));
        Assert.That(response.Fields[0].OriginalName, Is.EqualTo("Domain Name"));
        Assert.That(response.Fields[0].CanonicalName, Is.EqualTo("domain name"));
        Assert.That(response.Fields[0].RawValue, Is.EqualTo("EXAMPLE.COM"));
        Assert.That(response.Fields[1].OriginalName, Is.EqualTo("Status"));
        Assert.That(response.Fields[1].RawValue, Is.EqualTo("active"));
        Assert.That(response.Fields[2].OriginalName, Is.EqualTo("Status"));
        Assert.That(response.Fields[2].RawValue, Is.EqualTo("locked"));
        Assert.That(response.Fields[3].RawValue, Is.EqualTo("primary  "));
        Assert.That(response.Fields[3].Value, Is.EqualTo("primary  \ncontinuation value  "));
        Assert.That(response.Fields[4].RawValue, Is.EqualTo("whois://whois.example:43/path"));
        Assert.That(response.Fields[5].RawValue, Is.Empty);
        Assert.That(response.Fields[6].SourceLine, Is.EqualTo(12));
        Assert.That(response.Records[0].Index, Is.Zero);
        Assert.That(response.Records[0].Fields, Has.Length.EqualTo(6));
        Assert.That(response.Records[0].Lines, Has.Length.EqualTo(8));
        Assert.That(response.Records[1].Fields, Is.EqualTo(new[] { response.Fields[6] }));
        Assert.That(response.Records[1].Lines, Is.EqualTo(new[] { response.Lines[11] }));
        Assert.That(response.Fields[0].RecordIndex, Is.Zero);
        Assert.That(response.Fields[5].RecordIndex, Is.Zero);
        Assert.That(response.Fields[6].RecordIndex, Is.EqualTo(1));
        Assert.That(parser.FindFields(response, "status"), Is.EqualTo(new[] { response.Fields[1], response.Fields[2] }));
    }

    [Test]
    public void LineLimitReturnsStatusBearingParserFailureBeforeAllocation()
    {
        WhoisTextResponse text = CreateDecoded("First: one\nSecond: two");
        WhoisLimits limits = new WhoisLimits();
        limits.MaximumLines = 1;
        IWhoisResponseParser parser = new WhoisResponseParser();

        WhoisParsedResponse response = parser.ParseResponse(text, limits);

        Assert.That(response.TextResponse, Is.SameAs(text));
        Assert.That(response.IsParsed, Is.False);
        Assert.That(response.Lines, Is.Empty);
        Assert.That(response.Fields, Is.Empty);
        Assert.That(response.Records, Is.Empty);
        Assert.That(response.Error, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void FieldLimitReturnsStatusBearingFailureWithRetainedPrefix()
    {
        WhoisTextResponse text = CreateDecoded("First: one\nSecond: two");
        WhoisLimits limits = new WhoisLimits();
        limits.MaximumFields = 1;
        IWhoisResponseParser parser = new WhoisResponseParser();

        WhoisParsedResponse response = parser.ParseResponse(text, limits);

        Assert.That(response.IsParsed, Is.False);
        Assert.That(response.Lines, Has.Length.EqualTo(2));
        Assert.That(response.Lines[0].FieldIndex, Is.Zero);
        Assert.That(response.Lines[1].Kind, Is.EqualTo(WhoisLineKind.Field));
        Assert.That(response.Lines[1].FieldIndex, Is.EqualTo(-1));
        Assert.That(response.Fields, Is.EqualTo(new[] { response.Fields[0] }));
        Assert.That(response.Fields[0].OriginalName, Is.EqualTo("First"));
        Assert.That(response.Records, Is.Empty);
        Assert.That(response.Error, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void UndecodedResponseReturnsFailureWithOriginalError()
    {
        InvalidOperationException error = new InvalidOperationException("Decoding failed.");
        WhoisTextResponse text = new WhoisTextResponse();
        text.Text = "ignored: value";
        text.IsDecoded = false;
        text.Error = error;
        IWhoisResponseParser parser = new WhoisResponseParser();

        WhoisParsedResponse response = parser.ParseResponse(text, new WhoisLimits());

        Assert.That(response.TextResponse, Is.SameAs(text));
        Assert.That(response.IsParsed, Is.False);
        Assert.That(response.Lines, Is.Empty);
        Assert.That(response.Fields, Is.Empty);
        Assert.That(response.Records, Is.Empty);
        Assert.That(response.Error, Is.SameAs(error));
    }

    [Test]
    public void FindFieldsMatchesCaseInsensitivelyWithoutCollapsingDuplicates()
    {
        WhoisTextResponse text = CreateDecoded(
            "Status: active\r\n" +
            "STATUS: locked\r\n" +
            "Other: value\r\n" +
            "status: pending");
        IWhoisResponseParser parser = new WhoisResponseParser();
        WhoisParsedResponse parsed = parser.ParseResponse(text, new WhoisLimits());

        WhoisField[] matches = parser.FindFields(parsed, "sTaTuS");

        Assert.That(matches, Has.Length.EqualTo(3));
        Assert.That(matches, Is.EqualTo(new[] { parsed.Fields[0], parsed.Fields[1], parsed.Fields[3] }));
        Assert.That(matches[0].RawValue, Is.EqualTo("active"));
        Assert.That(matches[1].RawValue, Is.EqualTo("locked"));
        Assert.That(matches[2].RawValue, Is.EqualTo("pending"));
        Assert.That(parsed.Fields, Has.Length.EqualTo(4));
    }

    [Test]
    public void FindFieldsReturnsEmptyArrayForAnUnmatchedName()
    {
        IWhoisResponseParser parser = new WhoisResponseParser();
        WhoisParsedResponse parsed = parser.ParseResponse(CreateDecoded("Status: active"), new WhoisLimits());

        WhoisField[] matches = parser.FindFields(parsed, "registrar");

        Assert.That(matches, Is.Empty);
        Assert.That(parsed.Fields, Has.Length.EqualTo(1));
    }

    [Test]
    public void ClassifiesEveryLineKindAcrossMixedNewlinesAndFinalLine()
    {
        WhoisTextResponse text = CreateDecoded(
            "# hash comment\r\n" +
            "% percent comment\n" +
            "Name: value\r" +
            "  continued value\r\n" +
            "   \n" +
            "unstructured final line");
        IWhoisResponseParser parser = new WhoisResponseParser();

        WhoisParsedResponse response = parser.ParseResponse(text, new WhoisLimits());

        Assert.That(response.IsParsed, Is.True);
        Assert.That(response.Lines, Has.Length.EqualTo(6));
        Assert.That(response.Lines[0].Kind, Is.EqualTo(WhoisLineKind.Comment));
        Assert.That(response.Lines[1].Kind, Is.EqualTo(WhoisLineKind.Comment));
        Assert.That(response.Lines[2].Kind, Is.EqualTo(WhoisLineKind.Field));
        Assert.That(response.Lines[3].Kind, Is.EqualTo(WhoisLineKind.Continuation));
        Assert.That(response.Lines[4].Kind, Is.EqualTo(WhoisLineKind.Blank));
        Assert.That(response.Lines[5].Kind, Is.EqualTo(WhoisLineKind.Text));
        Assert.That(response.Lines[5].LineNumber, Is.EqualTo(6));
        Assert.That(response.Lines[5].RawContent, Is.EqualTo("unstructured final line"));
        Assert.That(response.Fields[0].Value, Is.EqualTo("value\ncontinued value"));
    }

    [Test]
    public void AppliesFieldWhitespaceEmptyValueAndFirstColonRules()
    {
        WhoisTextResponse text = CreateDecoded(
            "  Mixed Key \t: \t value:with:colons  \r\n" +
            "Empty Value:   \r\n" +
            "MiXeD CaSe: preserved");
        IWhoisResponseParser parser = new WhoisResponseParser();

        WhoisParsedResponse response = parser.ParseResponse(text, new WhoisLimits());

        Assert.That(response.IsParsed, Is.True);
        Assert.That(response.Fields, Has.Length.EqualTo(3));
        Assert.That(response.Fields[0].OriginalName, Is.EqualTo("Mixed Key"));
        Assert.That(response.Fields[0].CanonicalName, Is.EqualTo("mixed key"));
        Assert.That(response.Fields[0].RawValue, Is.EqualTo("value:with:colons  "));
        Assert.That(response.Fields[0].Value, Is.EqualTo("value:with:colons  "));
        Assert.That(response.Fields[1].OriginalName, Is.EqualTo("Empty Value"));
        Assert.That(response.Fields[1].RawValue, Is.Empty);
        Assert.That(response.Fields[2].OriginalName, Is.EqualTo("MiXeD CaSe"));
        Assert.That(response.Fields[2].CanonicalName, Is.EqualTo("mixed case"));
        Assert.That(parser.FindFields(response, "MIXED CASE"), Is.EqualTo(new[] { response.Fields[2] }));
    }

    [Test]
    public void ContinuationsAssociateOnlyUntilABlankBoundary()
    {
        WhoisTextResponse text = CreateDecoded(
            "Remarks: first  \n" +
            "  second  \n" +
            "\tthird\n" +
            "\n" +
            "  orphaned continuation");
        IWhoisResponseParser parser = new WhoisResponseParser();

        WhoisParsedResponse response = parser.ParseResponse(text, new WhoisLimits());

        Assert.That(response.Fields, Has.Length.EqualTo(1));
        Assert.That(response.Fields[0].RawValue, Is.EqualTo("first  "));
        Assert.That(response.Fields[0].Value, Is.EqualTo("first  \nsecond  \nthird"));
        Assert.That(response.Lines[1].Kind, Is.EqualTo(WhoisLineKind.Continuation));
        Assert.That(response.Lines[2].Kind, Is.EqualTo(WhoisLineKind.Continuation));
        Assert.That(response.Lines[4].Kind, Is.EqualTo(WhoisLineKind.Text));
        Assert.That(response.Lines[4].FieldIndex, Is.EqualTo(-1));
    }

    [Test]
    public void BlankLinesSeparateFieldBearingRecordsAndExcludeProseOnlySections()
    {
        WhoisTextResponse text = CreateDecoded(
            "First: one\r\n" +
            "\r\n" +
            "plain disclaimer text\r\n" +
            "more prose\r\n" +
            "\r\n" +
            "Second: two");
        IWhoisResponseParser parser = new WhoisResponseParser();

        WhoisParsedResponse response = parser.ParseResponse(text, new WhoisLimits());

        Assert.That(response.Records, Has.Length.EqualTo(2));
        Assert.That(response.Records[0].Fields, Is.EqualTo(new[] { response.Fields[0] }));
        Assert.That(response.Records[0].Lines, Is.EqualTo(new[] { response.Lines[0] }));
        Assert.That(response.Records[1].Fields, Is.EqualTo(new[] { response.Fields[1] }));
        Assert.That(response.Records[1].Lines, Is.EqualTo(new[] { response.Lines[5] }));
        Assert.That(response.Lines[2].Kind, Is.EqualTo(WhoisLineKind.Text));
        Assert.That(response.Lines[3].Kind, Is.EqualTo(WhoisLineKind.Text));
    }

    [Test]
    public void ParsesRegistryStyleFixturesAcrossTheirNativeNewlines()
    {
        string[] fixtures = new[]
        {
            WhoisResponseFixtures.Iana,
            WhoisResponseFixtures.Gtld,
            WhoisResponseFixtures.Arin,
            WhoisResponseFixtures.Ripe,
            WhoisResponseFixtures.Apnic
        };
        IWhoisResponseParser parser = new WhoisResponseParser();
        int index = 0;
        while (index < fixtures.Length)
        {
            WhoisParsedResponse response = parser.ParseResponse(CreateDecoded(fixtures[index]), new WhoisLimits());

            Assert.That(response.IsParsed, Is.True);
            Assert.That(response.Error, Is.Null);
            Assert.That(response.Lines, Is.Not.Empty);
            Assert.That(response.Fields, Is.Not.Empty);
            Assert.That(response.Records, Is.Not.Empty);
            index = index + 1;
        }
    }

    [Test]
    public void ProseFixtureDoesNotCreateFieldsOrRecords()
    {
        IWhoisResponseParser parser = new WhoisResponseParser();

        WhoisParsedResponse response = parser.ParseResponse(CreateDecoded(WhoisResponseFixtures.ProseAndDisclaimer), new WhoisLimits());

        Assert.That(response.IsParsed, Is.True);
        Assert.That(response.Fields, Is.Empty);
        Assert.That(response.Records, Is.Empty);
        Assert.That(response.Lines[0].Kind, Is.EqualTo(WhoisLineKind.Text));
        Assert.That(response.Lines[4].Kind, Is.EqualTo(WhoisLineKind.Comment));
    }

    [Test]
    public void RepeatedAndContinuationFixturesPreserveOrderedDerivedValues()
    {
        IWhoisResponseParser parser = new WhoisResponseParser();
        WhoisParsedResponse repeated = parser.ParseResponse(CreateDecoded(WhoisResponseFixtures.RepeatedFields), new WhoisLimits());
        WhoisParsedResponse continued = parser.ParseResponse(CreateDecoded(WhoisResponseFixtures.Continuations), new WhoisLimits());

        WhoisField[] statuses = parser.FindFields(repeated, "domain status");
        Assert.That(statuses, Has.Length.EqualTo(3));
        Assert.That(statuses[0].RawValue, Is.EqualTo("active"));
        Assert.That(statuses[1].RawValue, Is.EqualTo("clientTransferProhibited"));
        Assert.That(statuses[2].RawValue, Is.EqualTo("clientUpdateProhibited"));
        Assert.That(continued.Fields[0].Value, Is.EqualTo("first line with trailing space \nsecond line of the same value\nthird line with a tab"));
        Assert.That(continued.Lines[1].Kind, Is.EqualTo(WhoisLineKind.Continuation));
        Assert.That(continued.Lines[2].Kind, Is.EqualTo(WhoisLineKind.Continuation));
    }

    [Test]
    public void EmptyDecodedResponseProducesSuccessfulEmptyModel()
    {
        WhoisTextResponse text = CreateDecoded(string.Empty);
        IWhoisResponseParser parser = new WhoisResponseParser();

        WhoisParsedResponse response = parser.ParseResponse(text, new WhoisLimits());

        Assert.That(response.TextResponse, Is.SameAs(text));
        Assert.That(response.IsParsed, Is.True);
        Assert.That(response.Lines, Is.Empty);
        Assert.That(response.Fields, Is.Empty);
        Assert.That(response.Records, Is.Empty);
        Assert.That(response.Error, Is.Null);
        Assert.That(parser.FindFields(response, "anything"), Is.Empty);
    }

    [Test]
    public void ExactLineAndFieldLimitsCompleteSuccessfully()
    {
        WhoisTextResponse text = CreateDecoded("First: one\r\nSecond: two");
        WhoisLimits limits = new WhoisLimits();
        limits.MaximumLines = 2;
        limits.MaximumFields = 2;
        IWhoisResponseParser parser = new WhoisResponseParser();

        WhoisParsedResponse response = parser.ParseResponse(text, limits);

        Assert.That(response.IsParsed, Is.True);
        Assert.That(response.Lines, Has.Length.EqualTo(2));
        Assert.That(response.Fields, Has.Length.EqualTo(2));
        Assert.That(response.Records, Has.Length.EqualTo(1));
        Assert.That(response.Error, Is.Null);
    }

    [Test]
    public void ContinuationsAndCommentsDoNotConsumeTheFieldLimit()
    {
        WhoisTextResponse text = CreateDecoded("Field: first\n  continued\n# comment");
        WhoisLimits limits = new WhoisLimits();
        limits.MaximumFields = 1;
        IWhoisResponseParser parser = new WhoisResponseParser();

        WhoisParsedResponse response = parser.ParseResponse(text, limits);

        Assert.That(response.IsParsed, Is.True);
        Assert.That(response.Fields, Has.Length.EqualTo(1));
        Assert.That(response.Fields[0].Value, Is.EqualTo("first\ncontinued"));
        Assert.That(response.Lines, Has.Length.EqualTo(3));
        Assert.That(response.Error, Is.Null);
    }

    [Test]
    public void DuplicateLookupPreservesSourceOrderAndRecordMembershipIdentity()
    {
        WhoisTextResponse text = CreateDecoded(
            "Status: first\r\n" +
            "Name: one\r\n" +
            "\r\n" +
            "Status: second\r\n" +
            "Status: third");
        IWhoisResponseParser parser = new WhoisResponseParser();
        WhoisParsedResponse response = parser.ParseResponse(text, new WhoisLimits());

        WhoisField[] statuses = parser.FindFields(response, "STATUS");

        Assert.That(statuses, Is.EqualTo(new[] { response.Fields[0], response.Fields[2], response.Fields[3] }));
        Assert.That(statuses[0].RecordIndex, Is.Zero);
        Assert.That(statuses[1].RecordIndex, Is.EqualTo(1));
        Assert.That(statuses[2].RecordIndex, Is.EqualTo(1));
        Assert.That(response.Records[0].Fields, Is.EqualTo(new[] { response.Fields[0], response.Fields[1] }));
        Assert.That(response.Records[1].Fields, Is.EqualTo(new[] { response.Fields[2], response.Fields[3] }));
        Assert.That(response.Records[0].Lines[0], Is.SameAs(response.Lines[0]));
        Assert.That(response.Records[1].Lines[1], Is.SameAs(response.Lines[4]));
    }

    [Test]
    public void PreservesRawDecodedAndParsedSourceLinks()
    {
        byte[] body = System.Text.Encoding.UTF8.GetBytes(WhoisResponseFixtures.Gtld);
        WhoisRawResponse raw = new WhoisRawResponse();
        raw.Body = body;
        IWhoisResponseDecoder decoder = new WhoisResponseDecoder();
        IWhoisResponseParser parser = new WhoisResponseParser();

        WhoisTextResponse text = decoder.DecodeResponse(raw, new WhoisEncodingOptions());
        WhoisParsedResponse parsed = parser.ParseResponse(text, new WhoisLimits());

        Assert.That(text.RawResponse, Is.SameAs(raw));
        Assert.That(parsed.TextResponse, Is.SameAs(text));
        Assert.That(raw.Body, Is.SameAs(body));
        Assert.That(text.Text, Is.EqualTo(WhoisResponseFixtures.Gtld));
        Assert.That(parsed.Lines[0].RawContent, Is.EqualTo("Domain Name: EXAMPLE.COM"));
        Assert.That(parsed.Fields[0].SourceLine, Is.EqualTo(1));
        Assert.That(parsed.Fields[0].RawValue, Is.EqualTo("EXAMPLE.COM"));
        Assert.That(parsed.Records[0].Fields[0], Is.SameAs(parsed.Fields[0]));
        Assert.That(parsed.Records[0].Lines[0], Is.SameAs(parsed.Lines[0]));
        Assert.That(parsed.IsParsed, Is.True);
        Assert.That(parsed.Error, Is.Null);
    }

    private static WhoisTextResponse CreateDecoded(string content)
    {
        WhoisTextResponse text = new WhoisTextResponse();
        text.Text = content;
        text.IsDecoded = true;
        return text;
    }
}
