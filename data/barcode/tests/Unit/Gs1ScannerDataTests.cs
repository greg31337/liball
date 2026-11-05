namespace barcode.Tests;

public class Gs1ScannerDataTests
{
    [TestCase("]C0", BarcodeFormat.Code128, false, false, false, false, false)]
    [TestCase("]C1", BarcodeFormat.Gs1128, true, true, false, false, false)]
    [TestCase("]E0", BarcodeFormat.Automatic, true, false, true, true, false)]
    [TestCase("]E4", BarcodeFormat.Ean8, true, false, true, true, false)]
    [TestCase("]A0", BarcodeFormat.Code39, false, false, false, false, false)]
    [TestCase("]A1", BarcodeFormat.Code39, false, false, true, true, false)]
    [TestCase("]A3", BarcodeFormat.Code39, false, false, true, false, false)]
    [TestCase("]A4", BarcodeFormat.Code39, false, false, false, false, true)]
    [TestCase("]A5", BarcodeFormat.Code39, false, false, true, true, true)]
    [TestCase("]A7", BarcodeFormat.Code39, false, false, true, false, true)]
    [TestCase("]I0", BarcodeFormat.Itf, false, false, false, false, false)]
    [TestCase("]I1", BarcodeFormat.Itf14, true, false, true, true, false)]
    [TestCase("]I3", BarcodeFormat.Itf14, true, false, true, false, false)]
    [TestCase("]F0", BarcodeFormat.Codabar, false, false, false, false, false)]
    [TestCase("]d1", BarcodeFormat.DataMatrix, false, false, false, false, false)]
    [TestCase("]d2", BarcodeFormat.Gs1DataMatrix, true, true, false, false, false)]
    [TestCase("]L0", BarcodeFormat.Pdf417, false, false, false, false, false)]
    public void InterpretIdentifierDecodesSupportedModifiers(
        string identifier,
        BarcodeFormat format,
        bool isGs1,
        bool containsApplicationIdentifiers,
        bool checksumVerified,
        bool checksumTransmitted,
        bool fullAscii)
    {
        Gs1SymbologyIdentifierInfo info =
            Gs1ScannerData.InterpretSymbologyIdentifier(identifier);

        Assert.Multiple(() =>
        {
            Assert.That(info.Recognized, Is.True);
            Assert.That(info.Identifier, Is.EqualTo(identifier));
            Assert.That(info.Modifier,
                Is.EqualTo(identifier[2] - '0'));
            Assert.That(info.Format, Is.EqualTo(format));
            Assert.That(info.IsGs1, Is.EqualTo(isGs1));
            Assert.That(info.ContainsApplicationIdentifiers,
                Is.EqualTo(containsApplicationIdentifiers));
            Assert.That(info.ChecksumVerified,
                Is.EqualTo(checksumVerified));
            Assert.That(info.ChecksumTransmitted,
                Is.EqualTo(checksumTransmitted));
            Assert.That(info.FullAscii, Is.EqualTo(fullAscii));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("]")]
    [TestCase("]X")]
    [TestCase("]X0")]
    [TestCase("]C2")]
    [TestCase("]d3")]
    [TestCase("[C1")]
    public void InterpretIdentifierRejectsMalformedAndUnsupportedValues(
        string? identifier)
    {
        Gs1SymbologyIdentifierInfo info =
            Gs1ScannerData.InterpretSymbologyIdentifier(identifier);

        Assert.That(info.Recognized, Is.False);
    }

    [TestCase("]C1", BarcodeFormat.Gs1128)]
    [TestCase("]d2", BarcodeFormat.Gs1DataMatrix)]
    public void ParseAiCarrierUsesTransmittedGsSeparators(
        string identifier,
        BarcodeFormat format)
    {
        byte[] source = Bytes(
            identifier + "010950600013435210LOT17260101");

        Gs1ScannerParseResult result =
            Gs1ScannerData.ParseScannerData(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SymbologyIdentifier, Is.EqualTo(identifier));
            Assert.That(result.Format, Is.EqualTo(format));
            Assert.That(result.Elements, Has.Length.EqualTo(3));
            AssertElement(
                result.Elements[0],
                "01",
                "09506000134352",
                3,
                16);
            AssertElement(
                result.Elements[1],
                "10",
                "LOT",
                19,
                6);
            AssertElement(
                result.Elements[2],
                "17",
                "260101",
                25,
                8);
        });
    }

    [Test]
    public void ParseAiCarrierMapsSemanticIssuesAfterIdentifier()
    {
        Gs1ScannerParseResult result =
            Gs1ScannerData.ParseScannerData(
                Bytes("]C10109506000134353"));

        AssertIssue(
            result,
            "GS1_LINTER_INCORRECT_CHECK_DIGIT",
            18);
    }

    [Test]
    public void ParseScannerRejectsCaretBecauseItIsNotTransmittedFnc1()
    {
        Gs1ScannerParseResult result =
            Gs1ScannerData.ParseScannerData(
                Bytes("]C110LOT^17260101"));

        AssertIssue(result, "GS1_SCANNER_ILLEGAL_CARET", 8);
        Assert.That(result.Elements, Is.Empty);
    }

    [TestCase(null, "GS1_SCANNER_NULL", -1)]
    [TestCase("", "GS1_SCANNER_IDENTIFIER_MISSING", 0)]
    [TestCase("ABC", "GS1_SCANNER_IDENTIFIER_MISSING", 0)]
    [TestCase("]X0DATA", "GS1_SCANNER_IDENTIFIER_UNSUPPORTED", 0)]
    [TestCase("]C0DATA", "GS1_SCANNER_IDENTIFIER_NOT_GS1", 0)]
    [TestCase("]d1DATA", "GS1_SCANNER_IDENTIFIER_NOT_GS1", 0)]
    public void ParseScannerReportsIdentifierFailures(
        string? source,
        string code,
        int offset)
    {
        Gs1ScannerParseResult result =
            Gs1ScannerData.ParseScannerData(
                source == null ? null : Bytes(source));

        AssertIssue(result, code, offset);
        Assert.That(result.Elements, Is.Empty);
    }

    [Test]
    public void ParseAiCarrierReportsEmptyAndLeadingGsPayloads()
    {
        Gs1ScannerParseResult empty =
            Gs1ScannerData.ParseScannerData(Bytes("]C1"));
        Gs1ScannerParseResult leadingGs =
            Gs1ScannerData.ParseScannerData(
                Bytes("]C10109506000134352"));

        Assert.Multiple(() =>
        {
            AssertIssue(empty, "GS1_EMPTY_MESSAGE", 3);
            AssertIssue(
                leadingGs,
                "GS1_BARCODE_MESSAGE_LEADING_GS",
                3);
            Assert.That(leadingGs.Elements, Has.Length.EqualTo(1));
        });
    }

    [TestCase("]E04006381333931", BarcodeFormat.Ean13, "04006381333931")]
    [TestCase("]E455123457", BarcodeFormat.Ean8, "00000055123457")]
    [TestCase("]E0036000291452", BarcodeFormat.UpcA, "00036000291452")]
    [TestCase("]E001234558", BarcodeFormat.UpcE, "00012345000058")]
    [TestCase("]I109506000134352", BarcodeFormat.Itf14, "09506000134352")]
    [TestCase("]I30950600013435", BarcodeFormat.Itf14, "09506000134352")]
    public void ParsePlainCarrierNormalizesPayloadToAi01(
        string scannerData,
        BarcodeFormat format,
        string expectedGtin)
    {
        Gs1ScannerParseResult result =
            Gs1ScannerData.ParseScannerData(Bytes(scannerData));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Format, Is.EqualTo(format));
            Assert.That(result.Elements, Has.Length.EqualTo(1));
            Assert.That(result.Elements[0].ApplicationIdentifier,
                Is.EqualTo("01"));
            Assert.That(result.Elements[0].Value,
                Is.EqualTo(expectedGtin));
            Assert.That(result.Elements[0].SourceOffset, Is.EqualTo(3));
            Assert.That(result.Elements[0].SourceLength,
                Is.EqualTo(scannerData.Length - 3));
        });
    }

    [TestCase("]E040063813339", "GS1_SCANNER_PRIMARY_LENGTH_INVALID", 3)]
    [TestCase("]E04006381A33931", "GS1_SCANNER_PRIMARY_NON_NUMERIC", 10)]
    [TestCase("]E04006381333932", "GS1_SCANNER_PRIMARY_CHECK_DIGIT_INVALID", 15)]
    [TestCase("]E021234558", "GS1_SCANNER_UPCE_NUMBER_SYSTEM_INVALID", 3)]
    [TestCase("]I109506000134353", "GS1_SCANNER_PRIMARY_CHECK_DIGIT_INVALID", 16)]
    public void ParsePlainCarrierReportsPayloadFailures(
        string scannerData,
        string code,
        int offset)
    {
        Gs1ScannerParseResult result =
            Gs1ScannerData.ParseScannerData(Bytes(scannerData));

        AssertIssue(result, code, offset);
        Assert.That(result.Elements, Is.Empty);
    }

    [TestCase(BarcodeFormat.Gs1128, "]C1")]
    [TestCase(BarcodeFormat.Gs1DataMatrix, "]d2")]
    public void BuildAiCarrierRoundTripsElementsAndSeparators(
        BarcodeFormat format,
        string identifier)
    {
        Gs1Element[] elements = new Gs1Element[]
        {
            CreateElement("01", "09506000134352"),
            CreateElement("10", "LOT"),
            CreateElement("17", "260101")
        };

        Gs1ScannerBuildResult built =
            Gs1ScannerData.BuildScannerData(elements, format);
        Gs1ScannerParseResult parsed =
            Gs1ScannerData.ParseScannerData(built.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(built.SymbologyIdentifier,
                Is.EqualTo(identifier));
            Assert.That(built.Text.StartsWith(identifier), Is.True);
            Assert.That(built.Bytes,
                Does.Contain((byte)Gs1UnbracketedSyntax.GroupSeparator));
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

    [TestCase(BarcodeFormat.Ean13, "04006381333931", "]E04006381333931")]
    [TestCase(BarcodeFormat.Ean8, "00000055123457", "]E455123457")]
    [TestCase(BarcodeFormat.UpcA, "00036000291452", "]E0036000291452")]
    [TestCase(BarcodeFormat.UpcE, "00012345000058", "]E001234558")]
    [TestCase(BarcodeFormat.Itf14, "09506000134352", "]I109506000134352")]
    public void BuildPlainCarrierRoundTripsNormalizedGtin(
        BarcodeFormat format,
        string gtin,
        string expectedScannerData)
    {
        Gs1ScannerBuildResult built =
            Gs1ScannerData.BuildScannerData(
                new Gs1Element[] { CreateElement("01", gtin) },
                format);
        Gs1ScannerParseResult parsed =
            Gs1ScannerData.ParseScannerData(built.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(built.Text, Is.EqualTo(expectedScannerData));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Elements.Single().Value,
                Is.EqualTo(gtin));
        });
    }

    [Test]
    public void BuildItf14CanReportVerifiedCheckDigitAsStripped()
    {
        Gs1ScannerBuildResult built =
            Gs1ScannerData.BuildItf14ScannerDataWithoutCheckDigit(
                new Gs1Element[]
                {
                    CreateElement("01", "09506000134352")
                });
        Gs1ScannerParseResult parsed =
            Gs1ScannerData.ParseScannerData(built.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(built.Text, Is.EqualTo("]I30950600013435"));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Elements.Single().Value,
                Is.EqualTo("09506000134352"));
        });
    }

    [Test]
    public void BuildRejectsNonGs1FormatAndNonPrimaryPlainMessage()
    {
        Gs1ScannerBuildResult wrongFormat =
            Gs1ScannerData.BuildScannerData(
                new Gs1Element[]
                {
                    CreateElement("01", "09506000134352")
                },
                BarcodeFormat.Code128);
        Gs1ScannerBuildResult wrongElements =
            Gs1ScannerData.BuildScannerData(
                new Gs1Element[]
                {
                    CreateElement("10", "LOT")
                },
                BarcodeFormat.Ean13);

        Assert.Multiple(() =>
        {
            AssertBuildIssue(
                wrongFormat,
                "GS1_SCANNER_FORMAT_NOT_GS1_CARRIER");
            AssertBuildIssue(
                wrongElements,
                "GS1_SCANNER_PRIMARY_ELEMENT_REQUIRED");
        });
    }

    [Test]
    public void BuildRejectsCarrierMismatchAndNonCompressibleUpcE()
    {
        Gs1ScannerBuildResult mismatch =
            Gs1ScannerData.BuildScannerData(
                new Gs1Element[]
                {
                    CreateElement("01", "09506000134352")
                },
                BarcodeFormat.UpcA);
        Gs1ScannerBuildResult notCompressible =
            Gs1ScannerData.BuildScannerData(
                new Gs1Element[]
                {
                    CreateElement("01", "00012345678905")
                },
                BarcodeFormat.UpcE);

        Assert.Multiple(() =>
        {
            AssertBuildIssue(
                mismatch,
                "GS1_SCANNER_GTIN_CARRIER_MISMATCH");
            AssertBuildIssue(
                notCompressible,
                "GS1_SCANNER_GTIN_NOT_UPCE_COMPRESSIBLE");
        });
    }

    static byte[] Bytes(string source)
    {
        return global::System.Text.Encoding.ASCII.GetBytes(source);
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
        int length)
    {
        Assert.Multiple(() =>
        {
            Assert.That(element.ApplicationIdentifier, Is.EqualTo(identifier));
            Assert.That(element.Value, Is.EqualTo(value));
            Assert.That(element.SourceOffset, Is.EqualTo(offset));
            Assert.That(element.SourceLength, Is.EqualTo(length));
        });
    }

    static void AssertIssue(
        Gs1ScannerParseResult result,
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
            Assert.That(issue.Observed, Is.Not.Null);
        });
    }

    static void AssertBuildIssue(
        Gs1ScannerBuildResult result,
        string code)
    {
        BarcodeValidationIssue issue = result.Issues.Single(
            candidate => candidate.Code == code);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(issue.Severity,
                Is.EqualTo(BarcodeValidationSeverity.Error));
        });
    }
}
