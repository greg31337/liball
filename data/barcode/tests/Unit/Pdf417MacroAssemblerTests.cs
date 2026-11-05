namespace barcode.Tests;

public class Pdf417MacroAssemblerTests
{
    [Test]
    public void OutOfOrderSegmentsAreAssembledAndChecksumIsVerified()
    {
        int checksum = Pdf417MacroAssembler.CalculateMacroChecksum(
            Bytes("123456789"));
        BarcodeDecodeResult third = CreateSegment(
            2,
            3,
            true,
            "789",
            "100200",
            checksum);
        BarcodeDecodeResult first = CreateSegment(
            0,
            3,
            false,
            "123",
            "100200",
            -1);
        BarcodeDecodeResult second = CreateSegment(
            1,
            3,
            false,
            "456",
            "100200",
            -1);
        EciSegment eci = new EciSegment();
        eci.AssignmentNumber = 26;
        eci.ByteOffset = 0;
        eci.ByteCount = 3;
        second.Payload!.EciSegments = new EciSegment[] { eci };
        StructuredAppendRequest request = CreateRequest(
            new BarcodeDecodeResult[] { third, first, second },
            false,
            false);

        StructuredAppendResult assembled =
            Pdf417MacroAssembler.AssembleMacroSegments(request);

        Assert.Multiple(() =>
        {
            Assert.That(checksum, Is.EqualTo(0x31C3));
            Assert.That(assembled.Complete, Is.True);
            Assert.That(assembled.Payload?.RawBytes,
                Is.EqualTo(Bytes("123456789")));
            Assert.That(assembled.Payload?.Text,
                Is.EqualTo("123456789"));
            Assert.That(assembled.Payload?.EciSegments,
                Has.Length.EqualTo(1));
            Assert.That(assembled.Payload?.EciSegments?[0].AssignmentNumber,
                Is.EqualTo(26));
            Assert.That(assembled.Payload?.EciSegments?[0].ByteOffset,
                Is.EqualTo(3));
            Assert.That(assembled.Segments, Has.Length.EqualTo(3));
            Assert.That(
                assembled.Segments?[0].Payload?.StructuredAppend?.Index,
                Is.Zero);
            Assert.That(
                assembled.Segments?[1].Payload?.StructuredAppend?.Index,
                Is.EqualTo(1));
            Assert.That(
                assembled.Segments?[2].Payload?.StructuredAppend?.Index,
                Is.EqualTo(2));
            Assert.That(assembled.MissingIndices, Is.Empty);
            Assert.That(assembled.Conflicts, Is.Empty);
            Assert.That(assembled.FileIdentifier, Is.EqualTo("100200"));
            Assert.That(assembled.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "MACRO_PDF417_CHECKSUM_VALID"
                        && issue.Severity
                            == BarcodeValidationSeverity.Information
                        && issue.Standard == BarcodeStandards.Pdf417
                        && issue.Field == "ChecksumValue"
                        && issue.Offset == -1
                        && issue.Expected == checksum.ToString(
                            global::System.Globalization.CultureInfo.InvariantCulture)
                        && issue.Observed == checksum.ToString(
                            global::System.Globalization.CultureInfo.InvariantCulture)));
        });

        first.Payload!.RawBytes![0] = 0;
        Assert.That(assembled.Payload?.RawBytes?[0], Is.EqualTo((byte)'1'));
    }

    [Test]
    public void MissingSegmentsProducePartialOutputOrFailCompletePolicy()
    {
        BarcodeDecodeResult first = CreateSegment(
            0,
            3,
            false,
            "AAA",
            "300400",
            -1);
        BarcodeDecodeResult third = CreateSegment(
            2,
            3,
            true,
            "CCC",
            "300400",
            -1);
        StructuredAppendRequest request = CreateRequest(
            new BarcodeDecodeResult[] { third, first },
            false,
            false);

        StructuredAppendResult partial =
            Pdf417MacroAssembler.AssembleMacroSegments(request);
        request.RequireComplete = true;

        Assert.Multiple(() =>
        {
            Assert.That(partial.Complete, Is.False);
            Assert.That(partial.Payload?.Text, Is.EqualTo("AAACCC"));
            Assert.That(partial.MissingIndices, Is.EqualTo(new int[] { 1 }));
            Assert.That(partial.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "MACRO_PDF417_SEGMENTS_MISSING"));
            Assert.That(
                () => Pdf417MacroAssembler.AssembleMacroSegments(request),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void IdenticalDuplicatesFollowTheRequestedPolicy()
    {
        BarcodeDecodeResult first = CreateSegment(
            0,
            2,
            false,
            "LEFT",
            "500600",
            -1);
        BarcodeDecodeResult duplicate = CreateSegment(
            0,
            2,
            false,
            "LEFT",
            "500600",
            -1);
        BarcodeDecodeResult last = CreateSegment(
            1,
            2,
            true,
            "RIGHT",
            "500600",
            -1);
        BarcodeDecodeResult[] segments = new BarcodeDecodeResult[]
        {
            last,
            duplicate,
            first
        };
        StructuredAppendRequest allowedRequest = CreateRequest(
            segments,
            false,
            true);
        StructuredAppendRequest rejectedRequest = CreateRequest(
            segments,
            false,
            false);

        StructuredAppendResult allowed =
            Pdf417MacroAssembler.AssembleMacroSegments(allowedRequest);
        StructuredAppendResult rejected =
            Pdf417MacroAssembler.AssembleMacroSegments(rejectedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Complete, Is.True);
            Assert.That(allowed.Segments, Has.Length.EqualTo(2));
            Assert.That(allowed.Payload?.Text, Is.EqualTo("LEFTRIGHT"));
            Assert.That(allowed.Conflicts, Has.Length.EqualTo(1));
            Assert.That(allowed.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code
                        == "MACRO_PDF417_IDENTICAL_DUPLICATE"
                        && issue.Severity
                            == BarcodeValidationSeverity.Warning));
            Assert.That(rejected.Complete, Is.False);
            Assert.That(rejected.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code
                        == "MACRO_PDF417_DUPLICATE_NOT_ALLOWED"));
        });
    }

    [Test]
    public void ConflictingPayloadAndGlobalMetadataAreReported()
    {
        BarcodeDecodeResult first = CreateSegment(
            0,
            2,
            false,
            "ONE",
            "700800",
            123);
        BarcodeDecodeResult conflictingDuplicate = CreateSegment(
            0,
            2,
            false,
            "OTHER",
            "700800",
            456);
        BarcodeDecodeResult last = CreateSegment(
            1,
            3,
            true,
            "TWO",
            "DIFFERENT",
            -1);
        StructuredAppendRequest request = CreateRequest(
            new BarcodeDecodeResult[]
            {
                first,
                conflictingDuplicate,
                last
            },
            false,
            true);

        StructuredAppendResult assembled =
            Pdf417MacroAssembler.AssembleMacroSegments(request);

        Assert.Multiple(() =>
        {
            Assert.That(assembled.Complete, Is.False);
            Assert.That(assembled.Issues,
                Has.Some.Matches<BarcodeValidationIssue>(
                    issue => issue.Code
                        == "MACRO_PDF417_DUPLICATE_CONFLICT"));
            Assert.That(assembled.Issues,
                Has.Some.Matches<BarcodeValidationIssue>(
                    issue => issue.Code
                        == "MACRO_PDF417_FILE_IDENTIFIER_CONFLICT"));
            Assert.That(assembled.Issues,
                Has.Some.Matches<BarcodeValidationIssue>(
                    issue => issue.Code
                        == "MACRO_PDF417_SEGMENT_COUNT_CONFLICT"));
            Assert.That(assembled.Issues,
                Has.Some.Matches<BarcodeValidationIssue>(
                    issue => issue.Code
                        == "MACRO_PDF417_CHECKSUM_CONFLICT"));
            Assert.That(assembled.Conflicts?.Length,
                Is.GreaterThanOrEqualTo(4));
        });
    }

    [Test]
    public void ChecksumMismatchPreventsSuccessfulCompletion()
    {
        BarcodeDecodeResult first = CreateSegment(
            0,
            2,
            false,
            "CHECK",
            "111222",
            1);
        BarcodeDecodeResult last = CreateSegment(
            1,
            2,
            true,
            "SUM",
            "111222",
            -1);
        StructuredAppendRequest request = CreateRequest(
            new BarcodeDecodeResult[] { last, first },
            false,
            false);

        StructuredAppendResult assembled =
            Pdf417MacroAssembler.AssembleMacroSegments(request);

        Assert.Multiple(() =>
        {
            Assert.That(assembled.Complete, Is.False);
            Assert.That(assembled.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code
                        == "MACRO_PDF417_CHECKSUM_MISMATCH"
                        && issue.Expected == "1"
                        && issue.Observed != "1"));
            Assert.That(assembled.Conflicts, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void IncompleteRawPayloadLeavesChecksumUnverified()
    {
        BarcodeDecodeResult first = CreateSegment(
            0,
            3,
            false,
            "LEFT",
            "222333",
            12345);
        BarcodeDecodeResult last = CreateSegment(
            2,
            3,
            true,
            "RIGHT",
            "222333",
            -1);
        StructuredAppendRequest request = CreateRequest(
            new BarcodeDecodeResult[] { last, first },
            false,
            false);

        StructuredAppendResult assembled =
            Pdf417MacroAssembler.AssembleMacroSegments(request);

        Assert.Multiple(() =>
        {
            Assert.That(assembled.Complete, Is.False);
            Assert.That(assembled.MissingIndices, Is.EqualTo(new int[] { 1 }));
            Assert.That(assembled.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code
                        == "MACRO_PDF417_CHECKSUM_NOT_VERIFIED"
                        && issue.Expected == "12345"
                        && issue.Observed == "not verified"));
            Assert.That(assembled.Issues,
                Has.None.Matches<BarcodeValidationIssue>(
                    issue => issue.Code
                        == "MACRO_PDF417_CHECKSUM_MISMATCH"));
        });
    }

    [Test]
    public void TerminatorCountConflictPreventsCompletion()
    {
        BarcodeDecodeResult first = CreateSegment(
            0,
            3,
            false,
            "FIRST",
            "444555",
            -1);
        BarcodeDecodeResult prematureLast = CreateSegment(
            1,
            3,
            true,
            "LAST",
            "444555",
            -1);
        StructuredAppendRequest request = CreateRequest(
            new BarcodeDecodeResult[] { prematureLast, first },
            false,
            false);

        StructuredAppendResult assembled =
            Pdf417MacroAssembler.AssembleMacroSegments(request);

        Assert.Multiple(() =>
        {
            Assert.That(assembled.Complete, Is.False);
            Assert.That(assembled.MissingIndices, Is.EqualTo(new int[] { 2 }));
            Assert.That(assembled.Conflicts, Has.Length.EqualTo(1));
            Assert.That(assembled.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code
                        == "MACRO_PDF417_TERMINATOR_COUNT_CONFLICT"
                        && issue.Expected == "3"
                        && issue.Observed == "2"));
        });
    }

    [Test]
    public void CompletionIsUnknownWithoutCountOrTerminator()
    {
        BarcodeDecodeResult segment = CreateSegment(
            0,
            0,
            false,
            "PART",
            "333444",
            -1);
        StructuredAppendRequest request = CreateRequest(
            new BarcodeDecodeResult[] { segment },
            false,
            false);

        StructuredAppendResult assembled =
            Pdf417MacroAssembler.AssembleMacroSegments(request);

        Assert.Multiple(() =>
        {
            Assert.That(assembled.Complete, Is.False);
            Assert.That(assembled.MissingIndices, Is.Empty);
            Assert.That(assembled.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code
                        == "MACRO_PDF417_COMPLETION_UNKNOWN"));
        });
    }

    [Test]
    public void InvalidRequestsAndSegmentsAreRejected()
    {
        StructuredAppendRequest empty = new StructuredAppendRequest();
        empty.Format = BarcodeFormat.MacroPdf417;
        BarcodeDecodeResult wrongFormat = CreateSegment(
            0,
            1,
            true,
            "DATA",
            "555666",
            -1);
        wrongFormat.Format = BarcodeFormat.Pdf417;
        StructuredAppendRequest wrongRequest = CreateRequest(
            new BarcodeDecodeResult[] { wrongFormat },
            false,
            false);
        BarcodeDecodeResult missingMetadata = new BarcodeDecodeResult();
        missingMetadata.Format = BarcodeFormat.MacroPdf417;
        missingMetadata.Payload = new BarcodePayload();
        missingMetadata.Payload.Text = "DATA";
        StructuredAppendRequest missingRequest = CreateRequest(
            new BarcodeDecodeResult[] { missingMetadata },
            false,
            false);
        BarcodeDecodeResult invalidChecksum = CreateSegment(
            0,
            1,
            true,
            "DATA",
            "777888",
            -1);
        invalidChecksum.Metadata!.HasChecksum = true;
        invalidChecksum.Metadata.ChecksumValue = 65536;
        StructuredAppendRequest invalidChecksumRequest = CreateRequest(
            new BarcodeDecodeResult[] { invalidChecksum },
            false,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Pdf417MacroAssembler.AssembleMacroSegments(null!),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417MacroAssembler.AssembleMacroSegments(empty),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417MacroAssembler.AssembleMacroSegments(
                    wrongRequest),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417MacroAssembler.AssembleMacroSegments(
                    missingRequest),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417MacroAssembler.CalculateMacroChecksum(null!),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Pdf417MacroAssembler.AssembleMacroSegments(
                    invalidChecksumRequest),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static StructuredAppendRequest CreateRequest(
        BarcodeDecodeResult[] segments,
        bool requireComplete,
        bool allowIdenticalDuplicates)
    {
        StructuredAppendRequest request = new StructuredAppendRequest();
        request.Format = BarcodeFormat.MacroPdf417;
        request.Segments = segments;
        request.RequireComplete = requireComplete;
        request.AllowIdenticalDuplicates = allowIdenticalDuplicates;
        return request;
    }

    static BarcodeDecodeResult CreateSegment(
        int index,
        int count,
        bool isLast,
        string content,
        string fileIdentifier,
        int checksum)
    {
        StructuredAppendInfo information = new StructuredAppendInfo();
        information.Format = BarcodeFormat.MacroPdf417;
        information.Index = index;
        information.Count = count;
        information.FileIdentifier = fileIdentifier;
        information.IsLastSegment = isLast;
        BarcodePayload payload = new BarcodePayload();
        payload.Text = content;
        payload.RawBytes = Bytes(content);
        payload.CharacterEncoding = "ISO-8859-1";
        payload.StructuredAppend = information;
        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        if (checksum >= 0)
        {
            metadata.HasChecksum = true;
            metadata.ChecksumValue = checksum;
        }

        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Format = BarcodeFormat.MacroPdf417;
        result.Dimension = BarcodeDimension.StackedLinear;
        result.Payload = payload;
        result.Metadata = metadata;
        return result;
    }

    static byte[] Bytes(string value)
    {
        return global::System.Text.Encoding.ASCII.GetBytes(value);
    }
}
