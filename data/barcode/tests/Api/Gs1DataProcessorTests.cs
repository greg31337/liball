namespace barcode.Tests;

public class Gs1DataProcessorTests
{
    const string Gtin = "09506000134352";
    const string Bracketed =
        "(01)09506000134352(10)LOT(17)260101";

    [Test]
    public void LookupReturnsExactPublicMetadataAndDefensiveArrays()
    {
        IGs1DataProcessor processor = new Gs1DataProcessor();

        Gs1ApplicationIdentifierInfo? gtin =
            processor.LookupGs1ApplicationIdentifier("01");
        Gs1ApplicationIdentifierInfo? measure =
            processor.LookupGs1ApplicationIdentifier("3103");

        Assert.Multiple(() =>
        {
            Assert.That(gtin, Is.Not.Null);
            Assert.That(gtin!.Identifier, Is.EqualTo("01"));
            Assert.That(gtin.Title, Is.EqualTo("GTIN"));
            Assert.That(gtin.MinimumLength, Is.EqualTo(14));
            Assert.That(gtin.MaximumLength, Is.EqualTo(14));
            Assert.That(gtin.CharacterSet, Is.EqualTo("N"));
            Assert.That(gtin.FixedLength, Is.True);
            Assert.That(gtin.HasCheckCharacter, Is.True);
            Assert.That(gtin.IsPrimaryKey, Is.True);
            Assert.That(gtin.ExcludedIdentifiers,
                Does.Contain("255"));
            Assert.That(measure, Is.Not.Null);
            Assert.That(measure!.Identifier, Is.EqualTo("3103"));
            Assert.That(measure.MinimumLength, Is.EqualTo(6));
            Assert.That(measure.MaximumLength, Is.EqualTo(6));
            Assert.That(measure.FixedLength, Is.True);
            Assert.That(
                processor.LookupGs1ApplicationIdentifier("88"),
                Is.Null);
            Assert.That(
                processor.LookupGs1ApplicationIdentifier("ABC"),
                Is.Null);
        });

        gtin!.ExcludedIdentifiers![0] = "changed";
        Gs1ApplicationIdentifierInfo refreshed =
            processor.LookupGs1ApplicationIdentifier("01")!;
        Assert.That(refreshed.ExcludedIdentifiers,
            Does.Contain("255"));
    }

    [Test]
    public void BracketedSourceConvertsToEverySupportedTarget()
    {
        IGs1DataProcessor processor = new Gs1DataProcessor();

        Gs1ProcessResult bracketed = Process(
            processor,
            Gs1Syntax.Bracketed,
            Gs1Syntax.Bracketed,
            Bracketed,
            null);
        Gs1ProcessResult elementString = Process(
            processor,
            Gs1Syntax.Bracketed,
            Gs1Syntax.ElementString,
            Bracketed,
            null);
        Gs1ProcessResult barcodeMessage = Process(
            processor,
            Gs1Syntax.Bracketed,
            Gs1Syntax.BarcodeMessage,
            Bracketed,
            null);
        Gs1ProcessResult scannerData = Process(
            processor,
            Gs1Syntax.Bracketed,
            Gs1Syntax.ScannerData,
            Bracketed,
            null);
        Gs1ProcessResult hri = Process(
            processor,
            Gs1Syntax.Bracketed,
            Gs1Syntax.HumanReadableInterpretation,
            Bracketed,
            null);
        Gs1ProcessResult digitalLink = Process(
            processor,
            Gs1Syntax.Bracketed,
            Gs1Syntax.DigitalLinkUri,
            Bracketed,
            null);

        string barcodeText =
            "01" + Gtin + "10LOT" + (char)29 + "17260101";
        Assert.Multiple(() =>
        {
            AssertSuccess(bracketed, Gs1Syntax.Bracketed, 3);
            Assert.That(bracketed.Text, Is.EqualTo(Bracketed));
            AssertSuccess(elementString, Gs1Syntax.Bracketed, 3);
            Assert.That(elementString.Text,
                Is.EqualTo("^01" + Gtin + "10LOT^17260101"));
            AssertSuccess(barcodeMessage, Gs1Syntax.Bracketed, 3);
            Assert.That(barcodeMessage.Text, Is.EqualTo(barcodeText));
            Assert.That(barcodeMessage.Bytes,
                Is.EqualTo(ToBytes(barcodeText)));
            AssertSuccess(scannerData, Gs1Syntax.Bracketed, 3);
            Assert.That(scannerData.Text,
                Is.EqualTo("]C1" + barcodeText));
            Assert.That(scannerData.Bytes,
                Is.EqualTo(ToBytes("]C1" + barcodeText)));
            AssertSuccess(hri, Gs1Syntax.Bracketed, 3);
            Assert.That(hri.Text, Is.EqualTo(Bracketed));
            AssertSuccess(digitalLink, Gs1Syntax.Bracketed, 3);
            Assert.That(digitalLink.Text, Is.EqualTo(
                "https://id.gs1.org/01/" + Gtin
                + "/10/LOT?17=260101"));
        });
    }

    [Test]
    public void EverySupportedSourceConvertsBackToBracketedText()
    {
        IGs1DataProcessor processor = new Gs1DataProcessor();
        string barcodeText =
            "01" + Gtin + "10LOT" + (char)29 + "17260101";

        Gs1ProcessResult elementString = Process(
            processor,
            Gs1Syntax.ElementString,
            Gs1Syntax.Bracketed,
            "^01" + Gtin + "10LOT^17260101",
            null);
        Gs1ProcessResult barcodeMessage = Process(
            processor,
            Gs1Syntax.BarcodeMessage,
            Gs1Syntax.Bracketed,
            null,
            ToBytes(barcodeText));
        Gs1ProcessResult scannerData = Process(
            processor,
            Gs1Syntax.ScannerData,
            Gs1Syntax.Bracketed,
            null,
            ToBytes("]C1" + barcodeText));
        Gs1ProcessResult hri = Process(
            processor,
            Gs1Syntax.HumanReadableInterpretation,
            Gs1Syntax.Bracketed,
            "GTIN (01) " + Gtin
                + "\nBATCH/LOT (10) LOT\nUSE BY (17) 260101",
            null);
        Gs1ProcessResult digitalLink = Process(
            processor,
            Gs1Syntax.DigitalLinkUri,
            Gs1Syntax.Bracketed,
            "https://example.test/id/01/" + Gtin
                + "/10/LOT?17=260101",
            null);

        Assert.Multiple(() =>
        {
            Assert.That(elementString.Success, Is.True);
            Assert.That(elementString.Text, Is.EqualTo(Bracketed));
            Assert.That(barcodeMessage.Success, Is.True);
            Assert.That(barcodeMessage.Text, Is.EqualTo(Bracketed));
            Assert.That(scannerData.Success, Is.True);
            Assert.That(scannerData.Text, Is.EqualTo(Bracketed));
            Assert.That(scannerData.Message!.SymbologyIdentifier,
                Is.EqualTo("]C1"));
            Assert.That(hri.Success, Is.True);
            Assert.That(hri.Text, Is.EqualTo(Bracketed));
            Assert.That(digitalLink.Success, Is.True);
            Assert.That(digitalLink.Text, Is.EqualTo(Bracketed));
        });
    }

    [Test]
    public void EverySourceTargetPairRoundTripsThroughThePublicApi()
    {
        IGs1DataProcessor processor = new Gs1DataProcessor();
        Gs1Syntax[] syntaxes = new Gs1Syntax[]
        {
            Gs1Syntax.Bracketed,
            Gs1Syntax.ElementString,
            Gs1Syntax.BarcodeMessage,
            Gs1Syntax.ScannerData,
            Gs1Syntax.HumanReadableInterpretation,
            Gs1Syntax.DigitalLinkUri
        };

        int sourceIndex = 0;
        while (sourceIndex < syntaxes.Length)
        {
            Gs1Syntax sourceSyntax = syntaxes[sourceIndex];
            string? sourceText = SourceTextForSyntax(sourceSyntax);
            byte[]? sourceBytes = SourceBytesForSyntax(sourceSyntax);
            int targetIndex = 0;
            while (targetIndex < syntaxes.Length)
            {
                Gs1Syntax targetSyntax = syntaxes[targetIndex];
                Gs1ProcessResult converted = Process(
                    processor,
                    sourceSyntax,
                    targetSyntax,
                    sourceText,
                    sourceBytes);
                Gs1ProcessResult roundTrip = Process(
                    processor,
                    targetSyntax,
                    Gs1Syntax.Bracketed,
                    IsBinarySyntax(targetSyntax) ? null : converted.Text,
                    IsBinarySyntax(targetSyntax) ? converted.Bytes : null);
                string description = sourceSyntax + " -> " + targetSyntax;

                Assert.Multiple(() =>
                {
                    Assert.That(converted.Success, Is.True, description);
                    Assert.That(converted.Issues, Is.Empty, description);
                    Assert.That(roundTrip.Success, Is.True, description);
                    Assert.That(roundTrip.Text,
                        Is.EqualTo(Bracketed),
                        description);
                    Assert.That(roundTrip.Message?.Elements,
                        Has.Length.EqualTo(3),
                        description);
                });
                targetIndex++;
            }

            sourceIndex++;
        }
    }

    [Test]
    public void AssociationValidationFollowsTheRequestPolicy()
    {
        IGs1DataProcessor processor = new Gs1DataProcessor();
        Gs1ProcessRequest uncheckedRequest = new Gs1ProcessRequest();
        uncheckedRequest.SourceSyntax = Gs1Syntax.Bracketed;
        uncheckedRequest.TargetSyntax = Gs1Syntax.Bracketed;
        uncheckedRequest.SourceText =
            "(01)" + Gtin + "(250)SECONDARY";
        uncheckedRequest.ValidateAssociations = false;

        Gs1ProcessRequest checkedRequest = new Gs1ProcessRequest();
        checkedRequest.SourceSyntax = Gs1Syntax.Bracketed;
        checkedRequest.TargetSyntax = Gs1Syntax.Bracketed;
        checkedRequest.SourceText = uncheckedRequest.SourceText;
        checkedRequest.ValidateAssociations = true;

        Gs1ProcessResult uncheckedResult =
            processor.ProcessGs1Data(uncheckedRequest);
        Gs1ProcessResult checkedResult =
            processor.ProcessGs1Data(checkedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(uncheckedResult.Success, Is.True);
            Assert.That(checkedResult.Success, Is.False);
            Assert.That(checkedResult.Issues!.Select(issue => issue.Code),
                Does.Contain("GS1_REQUIRED_ASSOCIATION_MISSING"));
            Assert.That(checkedResult.Text, Is.Null);
        });
    }

    [Test]
    public void UnknownAiPolicyAcceptsOnlyExplicitlyDelimitedSourceData()
    {
        IGs1DataProcessor processor = new Gs1DataProcessor();
        Gs1ProcessRequest rejectedRequest = new Gs1ProcessRequest();
        rejectedRequest.SourceSyntax = Gs1Syntax.Bracketed;
        rejectedRequest.TargetSyntax = Gs1Syntax.Bracketed;
        rejectedRequest.SourceText = "(88)FUTURE";

        Gs1ProcessRequest acceptedRequest = new Gs1ProcessRequest();
        acceptedRequest.SourceSyntax = Gs1Syntax.Bracketed;
        acceptedRequest.TargetSyntax = Gs1Syntax.ElementString;
        acceptedRequest.SourceText =
            "(88)FUTURE(01)" + Gtin;
        acceptedRequest.AllowUnknownApplicationIdentifiers = true;

        Gs1ProcessRequest ambiguousRequest = new Gs1ProcessRequest();
        ambiguousRequest.SourceSyntax = Gs1Syntax.ElementString;
        ambiguousRequest.TargetSyntax = Gs1Syntax.Bracketed;
        ambiguousRequest.SourceText = "^88FUTURE";
        ambiguousRequest.AllowUnknownApplicationIdentifiers = true;

        Gs1ProcessRequest hriRequest = new Gs1ProcessRequest();
        hriRequest.SourceSyntax =
            Gs1Syntax.HumanReadableInterpretation;
        hriRequest.TargetSyntax = Gs1Syntax.ScannerData;
        hriRequest.SourceText =
            "FUTURE (88) FUTURE\nGTIN (01) " + Gtin;
        hriRequest.AllowUnknownApplicationIdentifiers = true;

        Gs1ProcessRequest digitalLinkRequest = new Gs1ProcessRequest();
        digitalLinkRequest.SourceSyntax = Gs1Syntax.Bracketed;
        digitalLinkRequest.TargetSyntax = Gs1Syntax.DigitalLinkUri;
        digitalLinkRequest.SourceText =
            "(01)" + Gtin + "(88)FUTURE";
        digitalLinkRequest.AllowUnknownApplicationIdentifiers = true;

        Gs1ProcessResult rejected =
            processor.ProcessGs1Data(rejectedRequest);
        Gs1ProcessResult accepted =
            processor.ProcessGs1Data(acceptedRequest);
        Gs1ProcessResult ambiguous =
            processor.ProcessGs1Data(ambiguousRequest);
        Gs1ProcessResult hri =
            processor.ProcessGs1Data(hriRequest);
        Gs1ProcessResult digitalLink =
            processor.ProcessGs1Data(digitalLinkRequest);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Issues!.Select(issue => issue.Code),
                Does.Contain("GS1_UNKNOWN_APPLICATION_IDENTIFIER"));
            Assert.That(accepted.Success, Is.True);
            Assert.That(accepted.Text,
                Is.EqualTo("^88FUTURE^01" + Gtin));
            Assert.That(accepted.Message!.Elements![0].Definition,
                Is.Null);
            Assert.That(accepted.Message.Elements[0].VariableLength,
                Is.True);
            Assert.That(ambiguous.Success, Is.False);
            Assert.That(ambiguous.Issues!.Select(issue => issue.Code),
                Does.Contain("GS1_UNKNOWN_APPLICATION_IDENTIFIER"));
            Assert.That(hri.Success, Is.True);
            Assert.That(hri.Text,
                Is.EqualTo("]C188FUTURE" + (char)29 + "01" + Gtin));
            Assert.That(hri.Bytes, Is.EqualTo(ToBytes(hri.Text!)));
            Assert.That(digitalLink.Success, Is.False);
            Assert.That(digitalLink.Issues!.Select(issue => issue.Code),
                Does.Contain("GS1_DIGITAL_LINK_BUILD_AI_INVALID"));
        });
    }

    [Test]
    public void InvalidSyntaxAndMalformedInputReturnIssuesWithoutThrowing()
    {
        IGs1DataProcessor processor = new Gs1DataProcessor();
        Gs1ProcessRequest invalidSyntax = new Gs1ProcessRequest();
        invalidSyntax.SourceSyntax = (Gs1Syntax)99;
        invalidSyntax.TargetSyntax = Gs1Syntax.Bracketed;
        invalidSyntax.SourceText = Bracketed;

        Gs1ProcessRequest malformed = new Gs1ProcessRequest();
        malformed.SourceSyntax = Gs1Syntax.Bracketed;
        malformed.TargetSyntax = Gs1Syntax.DigitalLinkUri;
        malformed.SourceText = "(01)123";

        Gs1ProcessRequest invalidTarget = new Gs1ProcessRequest();
        invalidTarget.SourceSyntax = Gs1Syntax.Bracketed;
        invalidTarget.TargetSyntax = (Gs1Syntax)99;
        invalidTarget.SourceText = Bracketed;

        Gs1ProcessResult invalidResult =
            processor.ProcessGs1Data(invalidSyntax);
        Gs1ProcessResult malformedResult =
            processor.ProcessGs1Data(malformed);
        Gs1ProcessResult invalidTargetResult =
            processor.ProcessGs1Data(invalidTarget);
        Gs1ProcessResult nullResult =
            processor.ProcessGs1Data(null!);

        Assert.Multiple(() =>
        {
            Assert.That(invalidResult.Success, Is.False);
            Assert.That(invalidResult.Issues![0].Code,
                Is.EqualTo("GS1_PROCESS_SOURCE_SYNTAX_INVALID"));
            Assert.That(malformedResult.Success, Is.False);
            Assert.That(malformedResult.Text, Is.Null);
            Assert.That(malformedResult.Issues, Is.Not.Empty);
            Assert.That(invalidTargetResult.Success, Is.False);
            Assert.That(invalidTargetResult.Issues![0].Code,
                Is.EqualTo("GS1_PROCESS_TARGET_SYNTAX_INVALID"));
            Assert.That(nullResult.Success, Is.False);
            Assert.That(nullResult.Issues![0].Code,
                Is.EqualTo("GS1_PROCESS_REQUEST_NULL"));
        });
    }

    [TestCase(Gs1Syntax.Bracketed, "GS1_BRACKETED_NULL")]
    [TestCase(Gs1Syntax.ElementString, "GS1_UNBRACKETED_NULL")]
    [TestCase(Gs1Syntax.BarcodeMessage, "GS1_BARCODE_MESSAGE_NULL")]
    [TestCase(Gs1Syntax.ScannerData, "GS1_SCANNER_NULL")]
    [TestCase(Gs1Syntax.HumanReadableInterpretation, "GS1_EXPLICIT_NULL")]
    [TestCase(Gs1Syntax.DigitalLinkUri, "GS1_DIGITAL_LINK_NULL")]
    public void NullInputUsesTheSourceSyntaxDiagnostic(
        Gs1Syntax sourceSyntax,
        string expectedCode)
    {
        IGs1DataProcessor processor = new Gs1DataProcessor();

        Gs1ProcessResult result = Process(
            processor,
            sourceSyntax,
            Gs1Syntax.Bracketed,
            null,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Null);
            Assert.That(result.Bytes, Is.Null);
            Assert.That(result.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == expectedCode));
        });
    }

    static Gs1ProcessResult Process(
        IGs1DataProcessor processor,
        Gs1Syntax sourceSyntax,
        Gs1Syntax targetSyntax,
        string? sourceText,
        byte[]? sourceBytes)
    {
        Gs1ProcessRequest request = new Gs1ProcessRequest();
        request.SourceSyntax = sourceSyntax;
        request.TargetSyntax = targetSyntax;
        request.SourceText = sourceText;
        request.SourceBytes = sourceBytes;
        request.DigitalLinkBaseUri = "https://id.gs1.org";
        return processor.ProcessGs1Data(request);
    }

    static void AssertSuccess(
        Gs1ProcessResult result,
        Gs1Syntax sourceSyntax,
        int elementCount)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Issues, Is.Empty);
        Assert.That(result.Message, Is.Not.Null);
        Assert.That(result.Message!.Syntax, Is.EqualTo(sourceSyntax));
        Assert.That(result.Message.Elements,
            Has.Length.EqualTo(elementCount));
    }

    static byte[] ToBytes(string source)
    {
        byte[] result = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = (byte)source[index];
            index = index + 1;
        }

        return result;
    }

    static string? SourceTextForSyntax(Gs1Syntax syntax)
    {
        switch (syntax)
        {
            case Gs1Syntax.Bracketed:
                return Bracketed;
            case Gs1Syntax.ElementString:
                return "^01" + Gtin + "10LOT^17260101";
            case Gs1Syntax.HumanReadableInterpretation:
                return "GTIN (01) " + Gtin
                    + "\nBATCH/LOT (10) LOT\nUSE BY (17) 260101";
            case Gs1Syntax.DigitalLinkUri:
                return "https://example.test/id/01/" + Gtin
                    + "/10/LOT?17=260101";
            default:
                return null;
        }
    }

    static byte[]? SourceBytesForSyntax(Gs1Syntax syntax)
    {
        string barcodeText =
            "01" + Gtin + "10LOT" + (char)29 + "17260101";
        if (syntax == Gs1Syntax.BarcodeMessage)
        {
            return ToBytes(barcodeText);
        }

        if (syntax == Gs1Syntax.ScannerData)
        {
            return ToBytes("]d2" + barcodeText);
        }

        return null;
    }

    static bool IsBinarySyntax(Gs1Syntax syntax)
    {
        return syntax == Gs1Syntax.BarcodeMessage
            || syntax == Gs1Syntax.ScannerData;
    }
}
