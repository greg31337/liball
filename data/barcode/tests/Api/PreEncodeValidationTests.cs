namespace barcode.Tests;

public class PreEncodeValidationTests
{
    [Test]
    public void EverySupportedFormatAcceptsAConformantPreEncodeRequest()
    {
        BarcodeEncodeRequest[] requests = new BarcodeEncodeRequest[]
        {
            CreateTextEncodeRequest(BarcodeFormat.Code128, "ABC123"),
            CreateGs1EncodeRequest(BarcodeFormat.Gs1128),
            CreateTextEncodeRequest(BarcodeFormat.Ean8, "55123457"),
            CreateTextEncodeRequest(BarcodeFormat.Ean13, "4006381333931"),
            CreateTextEncodeRequest(BarcodeFormat.UpcA, "036000291452"),
            CreateTextEncodeRequest(BarcodeFormat.UpcE, "01234558"),
            CreateTextEncodeRequest(BarcodeFormat.EanUpcAddon2, "12"),
            CreateTextEncodeRequest(BarcodeFormat.EanUpcAddon5, "51234"),
            CreateTextEncodeRequest(BarcodeFormat.Code39, "ABC-39"),
            CreateTextEncodeRequest(BarcodeFormat.Itf, "123456"),
            CreateTextEncodeRequest(BarcodeFormat.Itf14, "09501101530010"),
            CreateTextEncodeRequest(BarcodeFormat.Codabar, "1234"),
            CreateBinaryEncodeRequest(BarcodeFormat.DataMatrix, Bytes("DATA")),
            CreateGs1EncodeRequest(BarcodeFormat.Gs1DataMatrix),
            CreateBinaryEncodeRequest(BarcodeFormat.Pdf417, Bytes("PDF")),
            CreateCompactPdf417Request(),
            CreateMacroPdf417Request()
        };
        IBarcodeValidator validator = new BarcodeValidator();

        foreach (BarcodeEncodeRequest encode in requests)
        {
            BarcodeValidationReport report = validator.ValidateBarcodeData(
                CreateValidationRequest(encode));

            Assert.That(report.IsConformant, Is.True, encode.Format.ToString());
            Assert.That(report.Issues, Is.Empty, encode.Format.ToString());
            Assert.That(report.Standard, Is.Not.Empty, encode.Format.ToString());
        }
    }

    [Test]
    public void EanValidationReportsCheckDigitContextPrecisely()
    {
        BarcodeEncodeRequest encode = CreateTextEncodeRequest(
            BarcodeFormat.Ean13,
            "4006381333932");
        EanUpcOptions options = new EanUpcOptions();
        options.ChecksumMode = BarcodeChecksumMode.Require;
        encode.EanUpc = options;

        BarcodeValidationReport report = new BarcodeValidator().ValidateBarcodeData(
            CreateValidationRequest(encode));
        BarcodeValidationIssue issue = report.Issues!.Single(
            value => value.Code == "EAN_UPC_CHECK_DIGIT_INVALID");

        Assert.Multiple(() =>
        {
            Assert.That(report.IsConformant, Is.False);
            Assert.That(report.Standard, Is.EqualTo(BarcodeStandards.EanUpc));
            Assert.That(issue.Severity, Is.EqualTo(BarcodeValidationSeverity.Error));
            Assert.That(issue.Field, Is.EqualTo("Payload.Text"));
            Assert.That(issue.Offset, Is.EqualTo(12));
            Assert.That(issue.Expected, Is.EqualTo("1"));
            Assert.That(issue.Observed, Is.EqualTo("2"));
        });
    }

    [Test]
    public void Code128ValidationChecksForcedRepertoireAndCapacity()
    {
        BarcodeEncodeRequest forced = CreateTextEncodeRequest(
            BarcodeFormat.Code128,
            "12A");
        Code128Options forcedOptions = new Code128Options();
        forcedOptions.CodeSet = Code128CodeSet.C;
        forced.Code128 = forcedOptions;
        BarcodeEncodeRequest excessive = CreateTextEncodeRequest(
            BarcodeFormat.Code128,
            new string('A', Code128Encoder.MaximumPlannedCodewordCount + 1));
        IBarcodeValidator validator = new BarcodeValidator();

        BarcodeValidationReport forcedReport = validator.ValidateBarcodeData(
            CreateValidationRequest(forced));
        BarcodeValidationReport capacityReport = validator.ValidateBarcodeData(
            CreateValidationRequest(excessive));

        Assert.Multiple(() =>
        {
            Assert.That(forcedReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "CODE128_CODE_SET_C_DATA_INVALID"));
            Assert.That(capacityReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "CODE128_CAPACITY_EXCEEDED"));
        });
    }

    [Test]
    public void Code128ValidationRejectsBytesWithoutRequiredUpperShiftSupport()
    {
        BarcodeEncodeRequest rejected = CreateBinaryEncodeRequest(
            BarcodeFormat.Code128,
            new byte[] { 200 });
        BarcodeEncodeRequest accepted = CreateBinaryEncodeRequest(
            BarcodeFormat.Code128,
            new byte[] { 200 });
        Code128Options options = new Code128Options();
        options.AllowFnc4 = true;
        options.UseLatin1UpperShift = true;
        accepted.Code128 = options;
        IBarcodeValidator validator = new BarcodeValidator();

        BarcodeValidationReport rejectedReport = validator.ValidateBarcodeData(
            CreateValidationRequest(rejected));
        BarcodeValidationReport acceptedReport = validator.ValidateBarcodeData(
            CreateValidationRequest(accepted));

        Assert.Multiple(() =>
        {
            Assert.That(rejectedReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "CODE128_CODE_SET_CHARACTER_INVALID"));
            Assert.That(acceptedReport.IsConformant, Is.True);
        });
    }

    [Test]
    public void DataMatrixValidationAccumulatesModeCapacityAndAppendIssues()
    {
        byte[] bytes = new byte[DataMatrixModeCodec.MaximumBase256DataCount + 1];
        bytes[0] = (byte)'a';
        BarcodeEncodeRequest encode = CreateBinaryEncodeRequest(
            BarcodeFormat.DataMatrix,
            bytes);
        StructuredAppendInfo append = new StructuredAppendInfo();
        append.Format = BarcodeFormat.Pdf417;
        append.Index = 3;
        append.Count = 2;
        append.FileIdentifier = "999999";
        DataMatrixOptions options = new DataMatrixOptions();
        options.Shape = DataMatrixShape.Square;
        options.EncodingMode = DataMatrixEncodingMode.Base256;
        options.Rows = 11;
        options.Columns = 11;
        options.EciAssignmentNumber = 1000000;
        options.MacroType = 5;
        options.ReaderProgramming = true;
        options.StructuredAppend = append;
        encode.DataMatrix = options;

        BarcodeValidationReport report = new BarcodeValidator().ValidateBarcodeData(
            CreateValidationRequest(encode));
        string[] codes = report.Issues!.Select(issue => issue.Code!).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(report.IsConformant, Is.False);
            Assert.That(codes, Does.Contain("DATAMATRIX_DIMENSIONS_UNSUPPORTED"));
            Assert.That(codes, Does.Contain("DATAMATRIX_ECI_INVALID"));
            Assert.That(codes, Does.Contain("ENCODE_OPTION_COMBINATION_INVALID"));
            Assert.That(codes, Does.Contain("DATAMATRIX_APPEND_FORMAT_INVALID"));
            Assert.That(codes, Does.Contain("DATAMATRIX_APPEND_INDEX_INVALID"));
            Assert.That(codes, Does.Contain("DATAMATRIX_APPEND_FILE_ID_RANGE_INVALID"));
            Assert.That(codes, Does.Contain("DATAMATRIX_CAPACITY_EXCEEDED"));
        });
    }

    [Test]
    public void DataMatrixValidationChecksLastSegmentAgainstIndexAndCount()
    {
        BarcodeEncodeRequest encode = CreateBinaryEncodeRequest(
            BarcodeFormat.DataMatrix,
            Bytes("PART"));
        StructuredAppendInfo append = new StructuredAppendInfo();
        append.Format = BarcodeFormat.DataMatrix;
        append.Index = 1;
        append.Count = 2;
        append.FileIdentifier = "1";
        append.IsLastSegment = false;
        DataMatrixOptions options = new DataMatrixOptions();
        options.StructuredAppend = append;
        encode.DataMatrix = options;

        BarcodeValidationReport report = new BarcodeValidator().ValidateBarcodeData(
            CreateValidationRequest(encode));

        Assert.That(report.Issues,
            Has.One.Matches<BarcodeValidationIssue>(
                issue => issue.Code ==
                    "DATAMATRIX_APPEND_LAST_SEGMENT_CONFLICT"));
    }

    [Test]
    public void MacroPdf417ValidationChecksEveryStructuredAppendField()
    {
        BarcodeEncodeRequest encode = CreateBinaryEncodeRequest(
            BarcodeFormat.MacroPdf417,
            new byte[] { (byte)'A' });
        StructuredAppendInfo append = new StructuredAppendInfo();
        append.Format = BarcodeFormat.DataMatrix;
        append.Index = 4;
        append.Count = 2;
        append.FileIdentifier = "900";
        append.IsLastSegment = false;
        Pdf417Options options = new Pdf417Options();
        options.ErrorCorrectionLevel = 9;
        options.Macro = append;
        options.FileName = "";
        options.Sender = "\u0001";
        options.Timestamp = -1;
        options.FileSize = -1;
        options.FileChecksum = 65536;
        encode.Pdf417 = options;

        BarcodeValidationReport report = new BarcodeValidator().ValidateBarcodeData(
            CreateValidationRequest(encode));
        string[] codes = report.Issues!.Select(issue => issue.Code!).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(codes, Does.Contain("PDF417_ECC_LEVEL_INVALID"));
            Assert.That(codes, Does.Contain("MACRO_PDF417_APPEND_FORMAT_INVALID"));
            Assert.That(codes, Does.Contain("MACRO_PDF417_INDEX_COUNT_CONFLICT"));
            Assert.That(codes, Does.Contain("MACRO_PDF417_FILE_ID_GROUP_INVALID"));
            Assert.That(codes, Does.Contain("MACRO_PDF417_TEXT_FIELD_EMPTY"));
            Assert.That(codes,
                Does.Contain("MACRO_PDF417_TEXT_FIELD_CHARACTER_INVALID"));
            Assert.That(codes, Does.Contain("MACRO_PDF417_TIMESTAMP_INVALID"));
            Assert.That(codes, Does.Contain("MACRO_PDF417_FILE_SIZE_INVALID"));
            Assert.That(codes, Does.Contain("MACRO_PDF417_CHECKSUM_INVALID"));
        });
    }

    [Test]
    public void ValidationRejectsUnrelatedOptionsAndReturnsDefensiveReports()
    {
        BarcodeEncodeRequest encode = CreateTextEncodeRequest(
            BarcodeFormat.Code39,
            "ABC");
        encode.DataMatrix = new DataMatrixOptions();
        BarcodeValidationRequest request = CreateValidationRequest(encode);
        IBarcodeValidator validator = new BarcodeValidator();

        BarcodeValidationReport first = validator.ValidateBarcodeData(request);
        first.Issues![0].Code = "CHANGED";
        BarcodeValidationReport second = validator.ValidateBarcodeData(request);

        Assert.Multiple(() =>
        {
            Assert.That(second.IsConformant, Is.False);
            Assert.That(second.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "ENCODE_OPTION_NOT_APPLICABLE"));
            Assert.That(second.Metrics, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
        });
    }

    static BarcodeValidationRequest CreateValidationRequest(
        BarcodeEncodeRequest encode)
    {
        BarcodeValidationRequest request = new BarcodeValidationRequest();
        request.Format = encode.Format;
        request.Data = encode;
        request.Mode = BarcodeValidationMode.Strict;
        return request;
    }

    static BarcodeEncodeRequest CreateTextEncodeRequest(
        BarcodeFormat format,
        string text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        request.Format = format;
        request.Payload = payload;
        return request;
    }

    static BarcodeEncodeRequest CreateBinaryEncodeRequest(
        BarcodeFormat format,
        byte[] bytes)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.RawBytes = bytes;
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        request.Format = format;
        request.Payload = payload;
        return request;
    }

    static BarcodeEncodeRequest CreateGs1EncodeRequest(BarcodeFormat format)
    {
        BarcodeEncodeRequest request = CreateTextEncodeRequest(
            format,
            "0109506000134352");
        request.Payload!.IsGs1 = true;
        return request;
    }

    static BarcodeEncodeRequest CreateCompactPdf417Request()
    {
        BarcodeEncodeRequest request = CreateBinaryEncodeRequest(
            BarcodeFormat.CompactPdf417,
            Bytes("COMPACT"));
        Pdf417Options options = new Pdf417Options();
        options.Variant = Pdf417Variant.Compact;
        options.ErrorCorrectionLevel = -1;
        request.Pdf417 = options;
        return request;
    }

    static BarcodeEncodeRequest CreateMacroPdf417Request()
    {
        BarcodeEncodeRequest request = CreateBinaryEncodeRequest(
            BarcodeFormat.MacroPdf417,
            Bytes("MACRO"));
        StructuredAppendInfo append = new StructuredAppendInfo();
        append.Format = BarcodeFormat.MacroPdf417;
        append.Index = 0;
        append.Count = 1;
        append.FileIdentifier = "017053";
        append.IsLastSegment = true;
        Pdf417Options options = new Pdf417Options();
        options.ErrorCorrectionLevel = -1;
        options.Macro = append;
        request.Pdf417 = options;
        return request;
    }

    static byte[] Bytes(string text)
    {
        return global::System.Text.Encoding.ASCII.GetBytes(text);
    }
}
