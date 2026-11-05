namespace barcode.Tests;

public class Gs1UnbracketedSyntaxTests
{
    [Test]
    public void ParseUnbracketedUsesLeadingAndInternalCaretPlaceholders()
    {
        Gs1UnbracketedParseResult result =
            Gs1UnbracketedSyntax.ParseUnbracketedElementString(
                "^010950600013435210LOT^17260101");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.RequiresLeadingFunctionOne, Is.True);
            Assert.That(result.Elements, Has.Length.EqualTo(3));
            AssertElement(
                result.Elements[0],
                "01",
                "09506000134352",
                1,
                16,
                false);
            AssertElement(
                result.Elements[1],
                "10",
                "LOT",
                17,
                6,
                true);
            AssertElement(
                result.Elements[2],
                "17",
                "260101",
                23,
                8,
                false);
        });
    }

    [Test]
    public void ParseUnbracketedDoesNotRequireCaretBetweenPredefinedElements()
    {
        Gs1UnbracketedParseResult result =
            Gs1UnbracketedSyntax.ParseUnbracketedElementString(
                "^010950600013435217260101");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Elements, Has.Length.EqualTo(2));
            Assert.That(result.Elements[1].ApplicationIdentifier,
                Is.EqualTo("17"));
            Assert.That(result.Elements[1].SourceOffset, Is.EqualTo(17));
        });
    }

    [TestCase(null, "GS1_UNBRACKETED_NULL", -1)]
    [TestCase("", "GS1_UNBRACKETED_FNC1_FIRST_REQUIRED", 0)]
    [TestCase("0109506000134352", "GS1_UNBRACKETED_FNC1_FIRST_REQUIRED", 0)]
    public void ParseUnbracketedRequiresFirstPositionFnc1Placeholder(
        string? source,
        string code,
        int offset)
    {
        Gs1UnbracketedParseResult result =
            Gs1UnbracketedSyntax.ParseUnbracketedElementString(source);

        AssertIssue(result, code, offset);
        Assert.That(result.Elements, Is.Empty);
    }

    [Test]
    public void ParseUnbracketedRejectsAsciiGsInsteadOfTreatingItAsCaret()
    {
        Gs1UnbracketedParseResult result =
            Gs1UnbracketedSyntax.ParseUnbracketedElementString(
                "^10LOT17260101");

        AssertIssue(
            result,
            "GS1_UNBRACKETED_ASCII_GS_FORBIDDEN",
            6);
        Assert.That(result.Elements, Is.Empty);
    }

    [Test]
    public void ParseUnbracketedReportsEmptyAndRedundantPlaceholders()
    {
        Gs1UnbracketedParseResult empty =
            Gs1UnbracketedSyntax.ParseUnbracketedElementString("^");
        Gs1UnbracketedParseResult redundant =
            Gs1UnbracketedSyntax.ParseUnbracketedElementString(
                "^0109506000134352^17260101");

        Assert.Multiple(() =>
        {
            AssertIssue(empty, "GS1_EMPTY_MESSAGE", 1);
            AssertIssue(redundant, "GS1_UNEXPECTED_SEPARATOR", 17);
            Assert.That(redundant.Elements, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void ParseUnbracketedPreservesSemanticIssueOffsets()
    {
        Gs1UnbracketedParseResult result =
            Gs1UnbracketedSyntax.ParseUnbracketedElementString(
                "^0109506000134353");

        AssertIssue(
            result,
            "GS1_LINTER_INCORRECT_CHECK_DIGIT",
            16);
    }

    [Test]
    public void BuildUnbracketedProducesCanonicalCaretSyntaxAndRoundTrips()
    {
        Gs1Element[] elements = new Gs1Element[]
        {
            CreateElement("01", "09506000134352"),
            CreateElement("10", "LOT"),
            CreateElement("17", "260101")
        };

        Gs1UnbracketedBuildResult built =
            Gs1UnbracketedSyntax.BuildUnbracketedElementString(elements);
        Gs1UnbracketedParseResult parsed =
            Gs1UnbracketedSyntax.ParseUnbracketedElementString(built.Text);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(built.Text,
                Is.EqualTo("^010950600013435210LOT^17260101"));
            Assert.That(built.Bytes,
                Is.EqualTo(global::System.Text.Encoding.ASCII.GetBytes(
                    built.Text)));
            Assert.That(built.RequiresLeadingFunctionOne, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Elements.Select(element => element.Value),
                Is.EqualTo(new string[]
                {
                    "09506000134352",
                    "LOT",
                    "260101"
                }));
        });
    }

    [Test]
    public void BuildUnbracketedUsesDictionaryFnc1RuleAndOmitsTerminalSeparator()
    {
        Gs1UnbracketedBuildResult nonTerminal =
            Gs1UnbracketedSyntax.BuildUnbracketedElementString(
                new Gs1Element[]
                {
                    CreateElement("3940", "1234"),
                    CreateElement("17", "260101")
                });
        Gs1UnbracketedBuildResult terminal =
            Gs1UnbracketedSyntax.BuildUnbracketedElementString(
                new Gs1Element[]
                {
                    CreateElement("10", "LOT")
                });

        Assert.Multiple(() =>
        {
            Assert.That(nonTerminal.Success, Is.True);
            Assert.That(nonTerminal.Text,
                Is.EqualTo("^39401234^17260101"));
            Assert.That(terminal.Success, Is.True);
            Assert.That(terminal.Text, Is.EqualTo("^10LOT"));
        });
    }

    [Test]
    public void BuildUnbracketedRejectsReservedCaretAndAsciiGsInData()
    {
        Gs1UnbracketedBuildResult caret =
            Gs1UnbracketedSyntax.BuildUnbracketedElementString(
                new Gs1Element[]
                {
                    CreateElement("10", "LOT^A")
                });
        Gs1UnbracketedBuildResult groupSeparator =
            Gs1UnbracketedSyntax.BuildUnbracketedElementString(
                new Gs1Element[]
                {
                    CreateElement("10", "LOTA")
                });

        Assert.Multiple(() =>
        {
            AssertBuildIssue(
                caret,
                "GS1_UNBRACKETED_RESERVED_CARET",
                6);
            AssertBuildIssue(
                groupSeparator,
                "GS1_UNBRACKETED_ASCII_GS_FORBIDDEN",
                6);
            Assert.That(caret.Bytes, Is.Empty);
            Assert.That(groupSeparator.Bytes, Is.Empty);
        });
    }

    [Test]
    public void BuildUnbracketedMapsSemanticIssuesAfterLeadingCaret()
    {
        Gs1UnbracketedBuildResult result =
            Gs1UnbracketedSyntax.BuildUnbracketedElementString(
                new Gs1Element[]
                {
                    CreateElement("01", "09506000134353")
                });

        AssertBuildIssue(
            result,
            "GS1_LINTER_INCORRECT_CHECK_DIGIT",
            16);
    }

    [Test]
    public void ParseBarcodeMessageUsesAsciiGsForInternalSeparators()
    {
        byte[] source = global::System.Text.Encoding.ASCII.GetBytes(
            "010950600013435210LOT17260101");

        Gs1UnbracketedParseResult result =
            Gs1UnbracketedSyntax.ParseBarcodeMessageBytes(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.RequiresLeadingFunctionOne, Is.True);
            Assert.That(result.Elements, Has.Length.EqualTo(3));
            AssertElement(
                result.Elements[1],
                "10",
                "LOT",
                16,
                6,
                true);
            AssertElement(
                result.Elements[2],
                "17",
                "260101",
                22,
                8,
                false);
        });
    }

    [Test]
    public void ParseBarcodeMessageReportsNullAndEmptyInput()
    {
        Gs1UnbracketedParseResult nullResult =
            Gs1UnbracketedSyntax.ParseBarcodeMessageBytes(null);
        Gs1UnbracketedParseResult emptyResult =
            Gs1UnbracketedSyntax.ParseBarcodeMessageBytes(
                new byte[0]);

        Assert.Multiple(() =>
        {
            AssertIssue(nullResult, "GS1_BARCODE_MESSAGE_NULL", -1);
            AssertIssue(emptyResult, "GS1_EMPTY_MESSAGE", 0);
        });
    }

    [Test]
    public void ParseBarcodeMessageTreatsCaretAsLiteralDataNotFnc1()
    {
        byte[] source = global::System.Text.Encoding.ASCII.GetBytes(
            "10LOT^X");

        Gs1UnbracketedParseResult result =
            Gs1UnbracketedSyntax.ParseBarcodeMessageBytes(source);

        Assert.Multiple(() =>
        {
            AssertIssue(result, "GS1_INVALID_CSET82_CHARACTER", 5);
            Assert.That(result.Elements, Has.Length.EqualTo(1));
            Assert.That(result.Elements[0].Value, Is.EqualTo("LOT^X"));
        });
    }

    [Test]
    public void ParseBarcodeMessageRejectsLeadingGsButStillReportsElements()
    {
        byte[] source = global::System.Text.Encoding.ASCII.GetBytes(
            "0109506000134352");

        Gs1UnbracketedParseResult result =
            Gs1UnbracketedSyntax.ParseBarcodeMessageBytes(source);

        Assert.Multiple(() =>
        {
            AssertIssue(result, "GS1_BARCODE_MESSAGE_LEADING_GS", 0);
            Assert.That(result.Elements, Has.Length.EqualTo(1));
            Assert.That(result.Elements[0].SourceOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void ParseBarcodeMessageRejectsGsAfterPredefinedData()
    {
        byte[] source = global::System.Text.Encoding.ASCII.GetBytes(
            "010950600013435217260101");

        Gs1UnbracketedParseResult result =
            Gs1UnbracketedSyntax.ParseBarcodeMessageBytes(source);

        AssertIssue(result, "GS1_UNEXPECTED_SEPARATOR", 16);
        Assert.That(result.Elements, Has.Length.EqualTo(2));
    }

    [Test]
    public void BuildBarcodeMessageProducesRawBytesAndImplicitLeadingFnc1()
    {
        Gs1Element[] elements = new Gs1Element[]
        {
            CreateElement("01", "09506000134352"),
            CreateElement("10", "LOT"),
            CreateElement("17", "260101")
        };

        Gs1UnbracketedBuildResult built =
            Gs1UnbracketedSyntax.BuildBarcodeMessage(elements);
        Gs1UnbracketedParseResult parsed =
            Gs1UnbracketedSyntax.ParseBarcodeMessageBytes(built.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(built.Text,
                Is.EqualTo("010950600013435210LOT17260101"));
            Assert.That(built.Bytes[21],
                Is.EqualTo((byte)Gs1UnbracketedSyntax.GroupSeparator));
            Assert.That(built.Bytes[0], Is.EqualTo((byte)'0'));
            Assert.That(built.RequiresLeadingFunctionOne, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Elements, Has.Length.EqualTo(3));
        });
    }

    [Test]
    public void BuildBarcodeMessageOmitsSeparatorsBetweenPredefinedElements()
    {
        Gs1UnbracketedBuildResult result =
            Gs1UnbracketedSyntax.BuildBarcodeMessage(
                new Gs1Element[]
                {
                    CreateElement("01", "09506000134352"),
                    CreateElement("17", "260101")
                });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes,
                Does.Not.Contain(
                    (byte)Gs1UnbracketedSyntax.GroupSeparator));
            Assert.That(result.Text,
                Is.EqualTo("010950600013435217260101"));
        });
    }

    [Test]
    public void BuildBarcodeMessageRejectsAsciiGsInsideElementData()
    {
        Gs1UnbracketedBuildResult result =
            Gs1UnbracketedSyntax.BuildBarcodeMessage(
                new Gs1Element[]
                {
                    CreateElement("10", "LOTA")
                });

        AssertBuildIssue(
            result,
            "GS1_BARCODE_MESSAGE_RESERVED_GS",
            5);
        Assert.That(result.Bytes, Is.Empty);
    }

    [Test]
    public void BuildBarcodeMessageTreatsCaretAsInvalidLiteralData()
    {
        Gs1UnbracketedBuildResult result =
            Gs1UnbracketedSyntax.BuildBarcodeMessage(
                new Gs1Element[]
                {
                    CreateElement("10", "LOT^X")
                });

        Assert.Multiple(() =>
        {
            AssertBuildIssue(
                result,
                "GS1_INVALID_CSET82_CHARACTER",
                5);
            Assert.That(result.Text, Is.EqualTo("10LOT^X"));
            Assert.That(result.Bytes, Is.Empty);
        });
    }

    [Test]
    public void BuildReportsMissingNullAndUnknownElements()
    {
        Gs1UnbracketedBuildResult missing =
            Gs1UnbracketedSyntax.BuildBarcodeMessage(null);
        Gs1UnbracketedBuildResult invalid =
            Gs1UnbracketedSyntax.BuildBarcodeMessage(
                new Gs1Element[]
                {
                    null!,
                    CreateElement("04", "X")
                });

        Assert.Multiple(() =>
        {
            AssertBuildIssue(
                missing,
                "GS1_BARCODE_MESSAGE_ELEMENTS_MISSING",
                -1);
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Issues.Select(issue => issue.Code),
                Is.EqualTo(new string[]
                {
                    "GS1_BARCODE_MESSAGE_ELEMENT_NULL",
                    "GS1_BARCODE_MESSAGE_BUILD_AI_INVALID"
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
        Gs1UnbracketedParseResult result,
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

    static void AssertBuildIssue(
        Gs1UnbracketedBuildResult result,
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
            Assert.That(issue.Expected, Is.Not.Empty);
            Assert.That(issue.Observed, Is.Not.Empty);
        });
    }
}
