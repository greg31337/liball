namespace barcode.Tests;

public class LogicalSymbolValidationTests
{
    [Test]
    public void EverySupportedLogicalSymbolPassesValidation()
    {
        BarcodeSymbol[] symbols = CreateSupportedSymbols();
        IBarcodeValidator validator = new BarcodeValidator();

        foreach (BarcodeSymbol symbol in symbols)
        {
            BarcodeValidationReport report = validator.ValidateBarcodeSymbol(
                CreateRequest(symbol));

            Assert.That(report.IsConformant, Is.True,
                $"{symbol.Format}: {JoinIssueCodes(report.Issues)}");
            Assert.That(report.Issues, Is.Empty, symbol.Format.ToString());
            Assert.That(report.Standard, Is.Not.Empty, symbol.Format.ToString());
        }
    }

    [Test]
    public void StructuralValidationAccumulatesDimensionQuietZoneAndIdentifierIssues()
    {
        BarcodeSymbol symbol = CreateCode128Symbol();
        symbol.Dimension = BarcodeDimension.Matrix;
        symbol.RequiredQuietZone!.LeftModules = 1;
        symbol.SymbologyIdentifier = "]X9";

        BarcodeValidationReport report = new BarcodeValidator().ValidateBarcodeSymbol(
            CreateRequest(symbol));
        string[] codes = report.Issues!.Select(issue => issue.Code!).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(report.IsConformant, Is.False);
            Assert.That(codes, Does.Contain("SYMBOL_DIMENSION_CLASS_INVALID"));
            Assert.That(codes, Does.Contain("SYMBOL_QUIET_ZONE_INSUFFICIENT"));
            Assert.That(codes, Does.Contain("SYMBOL_IDENTIFIER_INVALID"));
        });
    }

    [Test]
    public void Code128ValidationReportsChecksumAndPatternDamage()
    {
        BarcodeSymbol symbol = CreateCode128Symbol();
        symbol.Codewords![symbol.Codewords.Length - 2] = 2;
        symbol.ChecksumValue = 2;
        symbol.Modules![0] = 0;

        BarcodeValidationReport report = new BarcodeValidator().ValidateBarcodeSymbol(
            CreateRequest(symbol));
        string[] codes = report.Issues!.Select(issue => issue.Code!).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(codes, Does.Contain("CODE128_CHECKSUM_INVALID"));
            Assert.That(codes, Does.Contain("CODE128_START_PATTERN_INVALID"));
            Assert.That(codes, Does.Contain("CODE128_PATTERN_INVALID"));
        });
    }

    [Test]
    public void DataMatrixValidationSeparatesEccAndFinderDamage()
    {
        BarcodeSymbol ecc = CreateDataMatrixSymbol();
        ecc.Codewords![0] ^= 1;
        BarcodeSymbol finder = CreateDataMatrixSymbol();
        finder.Modules![0] = finder.Modules[0] == 0 ? (byte)1 : (byte)0;
        IBarcodeValidator validator = new BarcodeValidator();

        BarcodeValidationReport eccReport = validator.ValidateBarcodeSymbol(
            CreateRequest(ecc));
        BarcodeValidationReport finderReport = validator.ValidateBarcodeSymbol(
            CreateRequest(finder));

        Assert.Multiple(() =>
        {
            Assert.That(eccReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "DATAMATRIX_ECC_INVALID"));
            Assert.That(finderReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "DATAMATRIX_FINDER_PATTERN_INVALID"));
        });
    }

    [Test]
    public void Pdf417ValidationSeparatesEccIndicatorsAndDimensionFailures()
    {
        BarcodeSymbol ecc = CreatePdf417Symbol();
        ecc.Codewords![1] ^= 1;
        BarcodeSymbol indicator = CreatePdf417Symbol();
        indicator.Modules![Pdf417CodewordTable.StartPatternModuleCount] =
            indicator.Modules[Pdf417CodewordTable.StartPatternModuleCount] == 0
                ? (byte)1
                : (byte)0;
        BarcodeSymbol dimensions = CreatePdf417Symbol();
        dimensions.WidthModules++;
        dimensions.Modules = new byte[
            dimensions.WidthModules * dimensions.HeightModules];
        IBarcodeValidator validator = new BarcodeValidator();

        BarcodeValidationReport eccReport = validator.ValidateBarcodeSymbol(
            CreateRequest(ecc));
        BarcodeValidationReport indicatorReport = validator.ValidateBarcodeSymbol(
            CreateRequest(indicator));
        BarcodeValidationReport dimensionReport = validator.ValidateBarcodeSymbol(
            CreateRequest(dimensions));

        Assert.Multiple(() =>
        {
            Assert.That(eccReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "PDF417_ECC_INVALID"));
            Assert.That(indicatorReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "PDF417_ROW_INDICATOR_INVALID"));
            Assert.That(dimensionReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "PDF417_ROW_WIDTH_INVALID"));
        });
    }

    [Test]
    public void Itf14ValidationRequiresBearerBarsAndReportsDefensiveCopies()
    {
        BarcodeSymbol symbol = CreateItf14Symbol();
        symbol.BearerBars = null;
        BarcodeValidationRequest request = CreateRequest(symbol);
        IBarcodeValidator validator = new BarcodeValidator();

        BarcodeValidationReport first = validator.ValidateBarcodeSymbol(request);
        first.Issues![0].Code = "CHANGED";
        BarcodeValidationReport second = validator.ValidateBarcodeSymbol(request);

        Assert.Multiple(() =>
        {
            Assert.That(second.IsConformant, Is.False);
            Assert.That(second.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "ITF14_BEARER_BARS_MISSING"));
            Assert.That(second.Metrics, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void MalformedItf14CodewordsProduceIssuesWithoutThrowing()
    {
        BarcodeSymbol symbol = CreateItf14Symbol();
        symbol.Codewords![0] = 99;

        BarcodeValidationReport report = new BarcodeValidator().ValidateBarcodeSymbol(
            CreateRequest(symbol));

        Assert.Multiple(() =>
        {
            Assert.That(report.IsConformant, Is.False);
            Assert.That(report.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "ITF_CODEWORDS_INVALID"));
        });
    }

    [Test]
    public void GenericItfChecksumMetadataAndIdentifierAreValidatedTogether()
    {
        ItfOptions options = new ItfOptions();
        options.ChecksumMode = BarcodeChecksumMode.Calculate;
        BarcodeSymbol symbol = ItfLayout.CreateLayout(
            "12345",
            CreatePayload("12345"),
            options,
            null).Symbol;

        BarcodeValidationReport report = new BarcodeValidator().ValidateBarcodeSymbol(
            CreateRequest(symbol));

        Assert.Multiple(() =>
        {
            Assert.That(symbol.SymbologyIdentifier, Is.EqualTo("]I1"));
            Assert.That(symbol.HasChecksum, Is.True);
            Assert.That(report.IsConformant, Is.True,
                JoinIssueCodes(report.Issues));
        });
    }

    static BarcodeSymbol[] CreateSupportedSymbols()
    {
        return new BarcodeSymbol[]
        {
            CreateCode128Symbol(),
            CreateGs1128Symbol(),
            CreateEanSymbol(BarcodeFormat.Ean8, "55123457"),
            CreateEanSymbol(BarcodeFormat.Ean13, "4006381333931"),
            CreateEanSymbol(BarcodeFormat.UpcA, "036000291452"),
            CreateUpcESymbol(),
            CreateAddOnSymbol("12"),
            CreateAddOnSymbol("51234"),
            Code39Layout.CreateLayout(
                "ABC-39",
                CreatePayload("ABC-39"),
                null,
                null).Symbol,
            ItfLayout.CreateLayout(
                "123456",
                CreatePayload("123456"),
                null,
                null).Symbol,
            CreateItf14Symbol(),
            CodabarLayout.CreateLayout(
                "1234",
                CreatePayload("1234"),
                null,
                null).Symbol,
            CreateDataMatrixSymbol(),
            CreateGs1DataMatrixSymbol(),
            CreatePdf417Symbol(),
            CreateCompactPdf417Symbol(),
            CreateMacroPdf417Symbol()
        };
    }

    static BarcodeSymbol CreateCode128Symbol()
    {
        byte[] bytes = Bytes("ABC123");
        return Code128Layout.CreateSymbol(
            Code128Encoder.EncodeBytes(bytes, null),
            CreatePayload("ABC123"),
            null);
    }

    static BarcodeSymbol CreateGs1128Symbol()
    {
        return Gs1128Codec.EncodeElements(
            CreateGs1Elements(),
            null,
            null).Symbol;
    }

    static BarcodeSymbol CreateEanSymbol(
        BarcodeFormat format,
        string text)
    {
        EanUpcAddonAssociation association = new EanUpcAddonAssociation();
        association.Main = EanUpcEncoder.Encode(
            text,
            format,
            BarcodeChecksumMode.Require);
        return EanUpcLayout.CreateSymbol(
            association,
            CreatePayload(text),
            null,
            null);
    }

    static BarcodeSymbol CreateUpcESymbol()
    {
        string text = "01234558";
        EanUpcAddonAssociation association = new EanUpcAddonAssociation();
        association.Main = EanUpcEncoder.EncodeUpcE(
            text,
            0,
            BarcodeChecksumMode.Require);
        return EanUpcLayout.CreateSymbol(
            association,
            CreatePayload(text),
            null,
            null);
    }

    static BarcodeSymbol CreateAddOnSymbol(string text)
    {
        EanUpcEncodedData encoded = EanUpcAddon.Encode(text);
        BarcodeQuietZone quiet = new BarcodeQuietZone();
        quiet.LeftModules = 7;
        quiet.RightModules = EanUpcLayout.AddOnRightQuietZoneModules;
        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = encoded.Format;
        symbol.Dimension = BarcodeDimension.Linear;
        symbol.WidthModules = encoded.Modules.Length;
        symbol.HeightModules = 1;
        symbol.Modules = encoded.Modules.ToArray();
        symbol.Payload = CreatePayload(text);
        symbol.RequiredQuietZone = quiet;
        symbol.SymbologyIdentifier = BarcodeSymbologyIdentifiers.EanUpc;
        symbol.Codewords = encoded.Digits.ToArray();
        symbol.HasChecksum = true;
        symbol.ChecksumValue = encoded.ChecksumValue;
        return symbol;
    }

    static BarcodeSymbol CreateItf14Symbol()
    {
        string text = "09501101530010";
        return Itf14Profile.CreateLayout(
            text,
            CreatePayload(text),
            null,
            null).Symbol;
    }

    static BarcodeSymbol CreateDataMatrixSymbol()
    {
        byte[] bytes = Bytes("DATA");
        return DataMatrixEncoder.Encode(
            bytes,
            CreatePayload("DATA"),
            null,
            null,
            false).Symbol;
    }

    static BarcodeSymbol CreateGs1DataMatrixSymbol()
    {
        return Gs1DataMatrixCodec.EncodeElements(
            CreateGs1Elements(),
            null,
            null).Symbol;
    }

    static BarcodeSymbol CreatePdf417Symbol()
    {
        byte[] bytes = Bytes("PDF417");
        return Pdf417Encoder.Encode(
            bytes,
            CreatePayload("PDF417"),
            null,
            null).Symbol;
    }

    static BarcodeSymbol CreateCompactPdf417Symbol()
    {
        byte[] bytes = Bytes("COMPACT");
        Pdf417Options options = new Pdf417Options();
        options.Variant = Pdf417Variant.Compact;
        options.ErrorCorrectionLevel = -1;
        return Pdf417Encoder.Encode(
            bytes,
            CreatePayload("COMPACT"),
            options,
            null).Symbol;
    }

    static BarcodeSymbol CreateMacroPdf417Symbol()
    {
        byte[] bytes = Bytes("MACRO");
        StructuredAppendInfo macro = new StructuredAppendInfo();
        macro.Format = BarcodeFormat.MacroPdf417;
        macro.Index = 0;
        macro.Count = 1;
        macro.FileIdentifier = "017053";
        macro.IsLastSegment = true;
        Pdf417Options options = new Pdf417Options();
        options.ErrorCorrectionLevel = -1;
        options.Macro = macro;
        return Pdf417Encoder.Encode(
            bytes,
            CreatePayload("MACRO"),
            options,
            null).Symbol;
    }

    static Gs1Element[] CreateGs1Elements()
    {
        Gs1Element element = new Gs1Element();
        element.ApplicationIdentifier = "01";
        element.Value = "09506000134352";
        return new Gs1Element[] { element };
    }

    static BarcodePayload CreatePayload(string text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = Bytes(text);
        return payload;
    }

    static BarcodeValidationRequest CreateRequest(BarcodeSymbol symbol)
    {
        BarcodeValidationRequest request = new BarcodeValidationRequest();
        request.Format = symbol.Format;
        request.Symbol = symbol;
        request.Mode = BarcodeValidationMode.Strict;
        return request;
    }

    static byte[] Bytes(string text)
    {
        return global::System.Text.Encoding.ASCII.GetBytes(text);
    }

    static string JoinIssueCodes(BarcodeValidationIssue[]? issues)
    {
        if (issues == null)
        {
            return "null";
        }

        return string.Join(",", issues.Select(issue => issue.Code));
    }
}
