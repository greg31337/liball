namespace barcode.Tests;

public class Gs1BracketedSyntaxTests
{
    [Test]
    public void ParseRecognizesElementsAndPreservesLiteralParentheses()
    {
        Gs1BracketedParseResult result =
            Gs1BracketedSyntax.ParseBracketed(
                "(01)09506000134352(10)LOT(A)42");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Elements, Has.Length.EqualTo(2));
            AssertElement(
                result.Elements[0],
                "01",
                "09506000134352",
                1,
                17,
                false);
            AssertElement(
                result.Elements[1],
                "10",
                "LOT(A)42",
                19,
                11,
                true);
            Assert.That(result.Elements[0].Components,
                Is.EqualTo(new string[] { "09506000134352" }));
            Assert.That(result.Elements[0].Definition!.Title,
                Is.EqualTo("GTIN"));
        });
    }

    [Test]
    public void LiteralOpeningParenthesisAtEndRemainsElementData()
    {
        Gs1BracketedParseResult result =
            Gs1BracketedSyntax.ParseBracketed("(10)LOT(");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Elements, Has.Length.EqualTo(1));
            Assert.That(result.Elements[0].Value, Is.EqualTo("LOT("));
        });
    }

    [Test]
    public void ExplicitBracketsPreserveFollowingElementAfterShortFixedData()
    {
        Gs1BracketedParseResult result =
            Gs1BracketedSyntax.ParseBracketed("(01)123(17)260101");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Elements, Has.Length.EqualTo(2));
            Assert.That(result.Elements[0].Value, Is.EqualTo("123"));
            AssertElement(
                result.Elements[1],
                "17",
                "260101",
                8,
                9,
                false);
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain("GS1_DATA_TOO_SHORT"));
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain("GS1_COMPONENT_TOO_SHORT"));
        });
    }

    [TestCase(null, "GS1_BRACKETED_NULL", -1)]
    [TestCase("", "GS1_BRACKETED_EMPTY", 0)]
    [TestCase("TEXT", "GS1_BRACKETED_AI_MISSING", 0)]
    [TestCase("(0)X", "GS1_BRACKETED_AI_LENGTH_INVALID", 1)]
    [TestCase("(AB)X", "GS1_BRACKETED_AI_NON_NUMERIC", 1)]
    [TestCase("(01X", "GS1_BRACKETED_AI_UNTERMINATED", 0)]
    [TestCase("(04)X", "GS1_UNKNOWN_APPLICATION_IDENTIFIER", 1)]
    public void ParseReportsMalformedLeadingApplicationIdentifier(
        string? source,
        string code,
        int offset)
    {
        Gs1BracketedParseResult result =
            Gs1BracketedSyntax.ParseBracketed(source);

        AssertIssue(result, code, offset);
        Assert.That(result.Elements, Is.Empty);
    }

    [TestCase("(10)LOT(12345)X", "GS1_BRACKETED_AI_LENGTH_INVALID", 8)]
    [TestCase("(10)LOT(12A)X", "GS1_BRACKETED_AI_NON_NUMERIC", 10)]
    [TestCase("(10)LOT(1", "GS1_BRACKETED_AI_UNTERMINATED", 7)]
    [TestCase("(10)LOT(04)X", "GS1_UNKNOWN_APPLICATION_IDENTIFIER", 8)]
    public void ParseDistinguishesMalformedNumericCandidatesFromLiteralData(
        string source,
        string code,
        int offset)
    {
        Gs1BracketedParseResult result =
            Gs1BracketedSyntax.ParseBracketed(source);

        Assert.Multiple(() =>
        {
            AssertIssue(result, code, offset);
            Assert.That(result.Elements, Has.Length.EqualTo(1));
            Assert.That(result.Elements[0].ApplicationIdentifier,
                Is.EqualTo("10"));
            Assert.That(result.Elements[0].Value, Is.EqualTo("LOT"));
        });
    }

    [Test]
    public void ParseMapsSemanticIssueToOriginalBracketedOffset()
    {
        Gs1BracketedParseResult result =
            Gs1BracketedSyntax.ParseBracketed("(01)09506000134353");

        AssertIssue(
            result,
            "GS1_LINTER_INCORRECT_CHECK_DIGIT",
            17);
    }

    [Test]
    public void BuildRoundTripsParsedElements()
    {
        string source = "(01)09506000134352(17)260101";
        Gs1BracketedParseResult parsed =
            Gs1BracketedSyntax.ParseBracketed(source);

        Gs1BracketedBuildResult built =
            Gs1BracketedSyntax.BuildBracketed(parsed.Elements);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(built.Success, Is.True);
            Assert.That(built.Issues, Is.Empty);
            Assert.That(built.Text, Is.EqualTo(source));
        });
    }

    [Test]
    public void BuildAllowsLiteralParenthesesBeginningWithNonDigit()
    {
        Gs1Element element = CreateElement("10", "LOT(A)42");

        Gs1BracketedBuildResult result =
            Gs1BracketedSyntax.BuildBracketed(
                new Gs1Element[] { element });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo("(10)LOT(A)42"));
        });
    }

    [Test]
    public void BuildRejectsNumericParenthesisDataThatCannotRoundTrip()
    {
        Gs1Element element = CreateElement("10", "LOT(01)X");

        Gs1BracketedBuildResult result =
            Gs1BracketedSyntax.BuildBracketed(
                new Gs1Element[] { element });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.EqualTo("(10)LOT(01)X"));
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Code,
                Is.EqualTo("GS1_BRACKETED_AMBIGUOUS_DATA"));
            Assert.That(result.Issues[0].Offset, Is.EqualTo(7));
        });
    }

    [Test]
    public void BuildValidatesElementSemanticsAtOutputOffsets()
    {
        Gs1Element element = CreateElement("01", "09506000134353");

        Gs1BracketedBuildResult result =
            Gs1BracketedSyntax.BuildBracketed(
                new Gs1Element[] { element });

        BarcodeValidationIssue issue = result.Issues.Single(
            candidate => candidate.Code
                == "GS1_LINTER_INCORRECT_CHECK_DIGIT");
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(issue.Offset, Is.EqualTo(17));
        });
    }

    [Test]
    public void BuildReportsMissingAndInvalidElements()
    {
        Gs1BracketedBuildResult missing =
            Gs1BracketedSyntax.BuildBracketed(null);
        Gs1BracketedBuildResult invalid =
            Gs1BracketedSyntax.BuildBracketed(
                new Gs1Element[]
                {
                    null!,
                    CreateElement("04", "X")
                });

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Issues.Single().Code,
                Is.EqualTo("GS1_BRACKETED_ELEMENTS_MISSING"));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Issues.Select(issue => issue.Code),
                Is.EqualTo(new string[]
                {
                    "GS1_BRACKETED_ELEMENT_NULL",
                    "GS1_BRACKETED_BUILD_AI_INVALID"
                }));
        });
    }

    static Gs1Element CreateElement(string identifier, string value)
    {
        Gs1Element element = new Gs1Element();
        element.ApplicationIdentifier = identifier;
        element.Value = value;
        return element;
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
        Gs1BracketedParseResult result,
        string code,
        int offset)
    {
        BarcodeValidationIssue issue = result.Issues.Single(
            candidate => candidate.Code == code);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(issue.Severity,
                Is.EqualTo(BarcodeValidationSeverity.Error));
            Assert.That(issue.Offset, Is.EqualTo(offset));
            Assert.That(issue.Standard,
                Is.EqualTo(Gs1ElementStringParser.StandardName));
            Assert.That(issue.Expected, Is.Not.Empty);
            Assert.That(issue.Observed, Is.Not.Empty);
        });
    }
}
