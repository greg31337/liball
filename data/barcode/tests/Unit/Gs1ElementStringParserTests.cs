namespace barcode.Tests;

public class Gs1ElementStringParserTests
{
    [Test]
    public void ParseRecognizesFixedAndVariableElementsWithExactOffsets()
    {
        Gs1ElementStringParseResult result =
            Gs1ElementStringParser.ParseElementString(
                "010950600013435210LOT123\u001d17260101");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Elements, Has.Length.EqualTo(3));
            AssertElement(result.Elements[0], "01", "09506000134352", 0, 16, false);
            AssertElement(result.Elements[1], "10", "LOT123", 16, 9, true);
            AssertElement(result.Elements[2], "17", "260101", 25, 8, false);
            Assert.That(result.Elements[0].Definition!.Title, Is.EqualTo("GTIN"));
            Assert.That(result.Elements[0].Definition!.FixedLength, Is.True);
            Assert.That(result.Elements[0].Definition!.HasCheckCharacter, Is.True);
            Assert.That(result.Elements[0].Definition!.IsPrimaryKey, Is.True);
            Assert.That(result.Elements[0].Definition!.ExcludedIdentifiers,
                Is.EqualTo(new string[] { "255", "37" }));
            Assert.That(result.Elements[1].Definition!.MinimumLength, Is.EqualTo(1));
            Assert.That(result.Elements[1].Definition!.MaximumLength, Is.EqualTo(20));
        });
    }

    [Test]
    public void MaximumLengthVariableElementStillNeedsSeparatorBeforeAnotherElement()
    {
        Gs1ElementStringParseResult terminated =
            Gs1ElementStringParser.ParseElementString(
                "10ABCDEFGHIJKLMNOPQRST\u001d17260101");
        Gs1ElementStringParseResult unterminated =
            Gs1ElementStringParser.ParseElementString(
                "10ABCDEFGHIJKLMNOPQRST17260101");

        Assert.Multiple(() =>
        {
            Assert.That(terminated.Success, Is.True);
            Assert.That(terminated.Elements, Has.Length.EqualTo(2));
            AssertElement(
                terminated.Elements[0],
                "10",
                "ABCDEFGHIJKLMNOPQRST",
                0,
                23,
                true);
            AssertElement(terminated.Elements[1], "17", "260101", 23, 8, false);
            AssertIssue(
                unterminated,
                "GS1_MISSING_SEPARATOR",
                22,
                "10");
            Assert.That(unterminated.Elements, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void FixedDataWithoutPredefinedLengthUsesGsTermination()
    {
        Gs1ElementStringParseResult terminated =
            Gs1ElementStringParser.ParseElementString(
                "39401234\u001d17260101");
        Gs1ElementStringParseResult unterminated =
            Gs1ElementStringParser.ParseElementString(
                "3940123417260101");

        Assert.Multiple(() =>
        {
            Assert.That(terminated.Success, Is.True);
            Assert.That(terminated.Elements, Has.Length.EqualTo(2));
            AssertElement(terminated.Elements[0], "3940", "1234", 0, 9, true);
            Assert.That(terminated.Elements[0].Definition!.FixedLength, Is.True);
            AssertElement(terminated.Elements[1], "17", "260101", 9, 8, false);
            AssertIssue(
                unterminated,
                "GS1_MISSING_SEPARATOR",
                8,
                "3940");
            Assert.That(unterminated.Elements, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void LeadingFnc1AndWildcardAiAreRecognizedFromBytes()
    {
        byte[] source = global::System.Text.Encoding.ASCII.GetBytes(
            "\u001d310200123491ABC");

        Gs1ElementStringParseResult result =
            Gs1ElementStringParser.ParseElementBytes(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Elements, Has.Length.EqualTo(2));
            AssertElement(result.Elements[0], "3102", "001234", 1, 10, false);
            Assert.That(result.Elements[0].Definition!.Identifier,
                Is.EqualTo("3102"));
            Assert.That(result.Elements[0].Definition!.RequiredIdentifiers,
                Is.EqualTo(new string[] { "01", "02" }));
            AssertElement(result.Elements[1], "91", "ABC", 11, 5, true);
        });
    }

    [Test]
    public void CompositeAndOptionalComponentsAreSlicedInDictionaryOrder()
    {
        Gs1ElementStringParseResult composite =
            Gs1ElementStringParser.ParseElementString(
                "2531234567890128ABC\u001d17260101");
        Gs1ElementStringParseResult repeatedOptional =
            Gs1ElementStringParser.ParseElementString("423840124");

        Assert.Multiple(() =>
        {
            Assert.That(composite.Success, Is.True);
            Assert.That(composite.Elements, Has.Length.EqualTo(2));
            Assert.That(composite.Elements[0].Components,
                Is.EqualTo(new string[] { "1234567890128", "ABC" }));
            Assert.That(composite.Elements[0].Definition!.CharacterSet,
                Is.EqualTo("N+X"));
            Assert.That(composite.Elements[0].Definition!.MinimumLength,
                Is.EqualTo(13));
            Assert.That(composite.Elements[0].Definition!.MaximumLength,
                Is.EqualTo(30));
            Assert.That(repeatedOptional.Success, Is.True);
            Assert.That(repeatedOptional.Elements[0].Components,
                Is.EqualTo(new string[] { "840", "124" }));
        });
    }

    [Test]
    public void CharacterSetFailuresReportTheOffendingSourceCharacter()
    {
        Gs1ElementStringParseResult numeric =
            Gs1ElementStringParser.ParseElementString("17A60101");
        Gs1ElementStringParseResult cset82 =
            Gs1ElementStringParser.ParseElementString("10A B");
        Gs1ElementStringParseResult cset39 =
            Gs1ElementStringParser.ParseElementString("8010Aa");
        Gs1ElementStringParseResult base64Url =
            Gs1ElementStringParser.ParseElementString("8030A+");

        Assert.Multiple(() =>
        {
            AssertIssue(numeric, "GS1_INVALID_NUMERIC_CHARACTER", 2, "17");
            AssertIssue(cset82, "GS1_INVALID_CSET82_CHARACTER", 3, "10");
            AssertIssue(cset39, "GS1_INVALID_CSET39_CHARACTER", 5, "8010");
            AssertIssue(base64Url, "GS1_INVALID_BASE64URL_CHARACTER", 5, "8030");
        });
    }

    [Test]
    public void Base64UrlPaddingMustBeTrailingAndStructurallyValid()
    {
        Gs1ElementStringParseResult valid =
            Gs1ElementStringParser.ParseElementString("8030AA=");
        Gs1ElementStringParseResult embedded =
            Gs1ElementStringParser.ParseElementString("8030A=A");
        Gs1ElementStringParseResult wrongLength =
            Gs1ElementStringParser.ParseElementString("8030A=");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            AssertIssue(embedded, "GS1_INVALID_BASE64URL_CHARACTER", 5, "8030");
            AssertIssue(wrongLength, "GS1_INVALID_BASE64URL_PADDING", 5, "8030");
        });
    }

    [Test]
    public void ShortFixedElementReportsLengthAndContinuesAfterSeparator()
    {
        Gs1ElementStringParseResult result =
            Gs1ElementStringParser.ParseElementString(
                "010950\u001d17260101");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Elements, Has.Length.EqualTo(2));
            AssertElement(result.Elements[0], "01", "0950", 0, 7, false);
            AssertElement(result.Elements[1], "17", "260101", 7, 8, false);
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain("GS1_DATA_TOO_SHORT"));
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain("GS1_COMPONENT_TOO_SHORT"));
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain("GS1_UNEXPECTED_SEPARATOR"));
            Assert.That(result.Issues.Single(
                issue => issue.Code == "GS1_UNEXPECTED_SEPARATOR").Offset,
                Is.EqualTo(6));
        });
    }

    [Test]
    public void EmptyAndUnknownElementsReturnStableDiagnostics()
    {
        Gs1ElementStringParseResult empty =
            Gs1ElementStringParser.ParseElementString("");
        Gs1ElementStringParseResult onlyFnc1 =
            Gs1ElementStringParser.ParseElementString("\u001d");
        Gs1ElementStringParseResult unknown =
            Gs1ElementStringParser.ParseElementString("04ABC");
        Gs1ElementStringParseResult extraSeparator =
            Gs1ElementStringParser.ParseElementString("\u001d\u001d10LOT");

        Assert.Multiple(() =>
        {
            AssertIssue(empty, "GS1_EMPTY_MESSAGE", 0, "Message");
            AssertIssue(onlyFnc1, "GS1_EMPTY_MESSAGE", 1, "Message");
            AssertIssue(unknown,
                "GS1_UNKNOWN_APPLICATION_IDENTIFIER",
                0,
                "ApplicationIdentifier");
            AssertIssue(extraSeparator, "GS1_EMPTY_ELEMENT", 1, "Message");
            Assert.That(extraSeparator.Elements, Has.Length.EqualTo(1));
            AssertElement(extraSeparator.Elements[0], "10", "LOT", 2, 5, true);
        });
    }

    static void AssertElement(
        Gs1Element element,
        string identifier,
        string value,
        int offset,
        int length,
        bool variableLength)
    {
        Assert.Multiple(() =>
        {
            Assert.That(element.ApplicationIdentifier, Is.EqualTo(identifier));
            Assert.That(element.Value, Is.EqualTo(value));
            Assert.That(element.SourceOffset, Is.EqualTo(offset));
            Assert.That(element.SourceLength, Is.EqualTo(length));
            Assert.That(element.VariableLength, Is.EqualTo(variableLength));
        });
    }

    static void AssertIssue(
        Gs1ElementStringParseResult result,
        string code,
        int offset,
        string field)
    {
        BarcodeValidationIssue issue = result.Issues.Single(
            candidate => candidate.Code == code);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(issue.Severity,
                Is.EqualTo(BarcodeValidationSeverity.Error));
            Assert.That(issue.Offset, Is.EqualTo(offset));
            Assert.That(issue.Field, Is.EqualTo(field));
            Assert.That(issue.Standard,
                Is.EqualTo(Gs1ElementStringParser.StandardName));
            Assert.That(issue.Expected, Is.Not.Empty);
            Assert.That(issue.Observed, Is.Not.Empty);
        });
    }
}
