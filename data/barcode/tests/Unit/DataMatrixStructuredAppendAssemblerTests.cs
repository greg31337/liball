namespace barcode.Tests;

public class DataMatrixStructuredAppendAssemblerTests
{
    [Test]
    public void OutOfOrderSegmentsAreNormalizedAndPayloadMetadataIsRebased()
    {
        BarcodeDecodeResult first = CreateSegment(
            BarcodeFormat.DataMatrix,
            0,
            3,
            "AA",
            "1");
        BarcodeDecodeResult second = CreateSegment(
            BarcodeFormat.DataMatrix,
            1,
            3,
            "BB",
            "001001");
        BarcodeDecodeResult third = CreateSegment(
            BarcodeFormat.DataMatrix,
            2,
            3,
            "CC",
            "1");
        EciSegment eci = new EciSegment();
        eci.AssignmentNumber = 26;
        eci.ByteOffset = 0;
        eci.ByteCount = 2;
        second.Payload!.EciSegments = new EciSegment[] { eci };
        StructuredAppendRequest request = CreateRequest(
            BarcodeFormat.DataMatrix,
            new BarcodeDecodeResult[] { third, first, second },
            false,
            false);

        StructuredAppendResult result =
            DataMatrixStructuredAppendAssembler.
                AssembleDataMatrixSegments(request);
        first.Payload!.RawBytes![0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Complete, Is.True);
            Assert.That(result.Payload!.Text, Is.EqualTo("AABBCC"));
            Assert.That(result.Payload.RawBytes, Is.EqualTo(Bytes("AABBCC")));
            Assert.That(result.Payload.RawBytes![0], Is.EqualTo((byte)'A'));
            Assert.That(result.Payload.CharacterEncoding, Is.EqualTo("ISO-8859-1"));
            Assert.That(result.Payload.IsGs1, Is.False);
            Assert.That(result.Payload.StructuredAppend, Is.Null);
            Assert.That(result.Payload.EciSegments, Has.Length.EqualTo(1));
            Assert.That(result.Payload.EciSegments![0].ByteOffset, Is.EqualTo(2));
            Assert.That(result.Segments, Has.Length.EqualTo(3));
            Assert.That(result.Segments![0].Payload!.StructuredAppend!.Index, Is.Zero);
            Assert.That(result.Segments[1].Payload!.StructuredAppend!.Index, Is.EqualTo(1));
            Assert.That(result.Segments[2].Payload!.StructuredAppend!.Index, Is.EqualTo(2));
            Assert.That(result.MissingIndices, Is.Empty);
            Assert.That(result.Conflicts, Is.Empty);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.FileIdentifier, Is.EqualTo("001001"));
        });
    }

    [Test]
    public void MissingSegmentsProducePartialOutputOrFailCompletePolicy()
    {
        BarcodeDecodeResult first = CreateSegment(
            BarcodeFormat.DataMatrix,
            0,
            3,
            "AA",
            "200");
        BarcodeDecodeResult third = CreateSegment(
            BarcodeFormat.DataMatrix,
            2,
            3,
            "CC",
            "200");
        StructuredAppendRequest request = CreateRequest(
            BarcodeFormat.DataMatrix,
            new BarcodeDecodeResult[] { third, first },
            false,
            false);

        StructuredAppendResult partial =
            DataMatrixStructuredAppendAssembler.
                AssembleDataMatrixSegments(request);
        request.RequireComplete = true;

        Assert.Multiple(() =>
        {
            Assert.That(partial.Complete, Is.False);
            Assert.That(partial.Payload!.Text, Is.EqualTo("AACC"));
            Assert.That(partial.MissingIndices, Is.EqualTo(new int[] { 1 }));
            Assert.That(partial.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code ==
                        "DATAMATRIX_STRUCTURED_APPEND_SEGMENTS_MISSING"));
            Assert.That(
                () => DataMatrixStructuredAppendAssembler.
                    AssembleDataMatrixSegments(request),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void IdenticalDuplicatesFollowTheRequestedPolicy()
    {
        BarcodeDecodeResult first = CreateSegment(
            BarcodeFormat.DataMatrix,
            0,
            2,
            "LEFT",
            "300");
        BarcodeDecodeResult duplicate = CreateSegment(
            BarcodeFormat.DataMatrix,
            0,
            2,
            "LEFT",
            "300");
        BarcodeDecodeResult last = CreateSegment(
            BarcodeFormat.DataMatrix,
            1,
            2,
            "RIGHT",
            "300");
        BarcodeDecodeResult[] segments = new BarcodeDecodeResult[]
        {
            last,
            duplicate,
            first
        };

        StructuredAppendResult allowed =
            DataMatrixStructuredAppendAssembler.AssembleDataMatrixSegments(
                CreateRequest(
                    BarcodeFormat.DataMatrix,
                    segments,
                    false,
                    true));
        StructuredAppendResult rejected =
            DataMatrixStructuredAppendAssembler.AssembleDataMatrixSegments(
                CreateRequest(
                    BarcodeFormat.DataMatrix,
                    segments,
                    false,
                    false));

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Complete, Is.True);
            Assert.That(allowed.Payload!.Text, Is.EqualTo("LEFTRIGHT"));
            Assert.That(allowed.Conflicts, Has.Length.EqualTo(1));
            Assert.That(allowed.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code ==
                        "DATAMATRIX_STRUCTURED_APPEND_IDENTICAL_DUPLICATE" &&
                        issue.Severity == BarcodeValidationSeverity.Warning));
            Assert.That(rejected.Complete, Is.False);
            Assert.That(rejected.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code ==
                        "DATAMATRIX_STRUCTURED_APPEND_DUPLICATE_NOT_ALLOWED"));
        });
    }

    [Test]
    public void ConflictingCountsIdentifiersAndPayloadsPreventCompletion()
    {
        BarcodeDecodeResult first = CreateSegment(
            BarcodeFormat.DataMatrix,
            0,
            2,
            "ONE",
            "400");
        BarcodeDecodeResult duplicate = CreateSegment(
            BarcodeFormat.DataMatrix,
            0,
            2,
            "OTHER",
            "400");
        BarcodeDecodeResult second = CreateSegment(
            BarcodeFormat.DataMatrix,
            1,
            3,
            "TWO",
            "401");
        StructuredAppendRequest request = CreateRequest(
            BarcodeFormat.DataMatrix,
            new BarcodeDecodeResult[] { first, duplicate, second },
            false,
            true);

        StructuredAppendResult result =
            DataMatrixStructuredAppendAssembler.
                AssembleDataMatrixSegments(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Complete, Is.False);
            Assert.That(result.Issues,
                Has.Some.Matches<BarcodeValidationIssue>(
                    issue => issue.Code ==
                        "DATAMATRIX_STRUCTURED_APPEND_DUPLICATE_CONFLICT"));
            Assert.That(result.Issues,
                Has.Some.Matches<BarcodeValidationIssue>(
                    issue => issue.Code ==
                        "DATAMATRIX_STRUCTURED_APPEND_FILE_IDENTIFIER_CONFLICT"));
            Assert.That(result.Issues,
                Has.Some.Matches<BarcodeValidationIssue>(
                    issue => issue.Code ==
                        "DATAMATRIX_STRUCTURED_APPEND_COUNT_CONFLICT"));
            Assert.That(result.Conflicts!.Length, Is.GreaterThanOrEqualTo(3));
        });
    }

    [Test]
    public void Gs1DataMatrixStateIsPreservedAndInvalidSegmentsAreRejected()
    {
        BarcodeDecodeResult first = CreateSegment(
            BarcodeFormat.Gs1DataMatrix,
            0,
            2,
            "01",
            null);
        BarcodeDecodeResult last = CreateSegment(
            BarcodeFormat.Gs1DataMatrix,
            1,
            2,
            "09506000134352",
            null);
        StructuredAppendRequest request = CreateRequest(
            BarcodeFormat.Gs1DataMatrix,
            new BarcodeDecodeResult[] { last, first },
            false,
            false);
        StructuredAppendResult result =
            DataMatrixStructuredAppendAssembler.
                AssembleDataMatrixSegments(request);
        last.Payload!.IsGs1 = false;

        Assert.Multiple(() =>
        {
            Assert.That(result.Complete, Is.True);
            Assert.That(result.Payload!.IsGs1, Is.True);
            Assert.That(result.Payload.Text, Is.EqualTo("0109506000134352"));
            Assert.That(result.FileIdentifier, Is.EqualTo("001001"));
            Assert.That(
                () => DataMatrixStructuredAppendAssembler.
                    AssembleDataMatrixSegments(request),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static StructuredAppendRequest CreateRequest(
        BarcodeFormat format,
        BarcodeDecodeResult[] segments,
        bool requireComplete,
        bool allowIdenticalDuplicates)
    {
        StructuredAppendRequest request = new StructuredAppendRequest();
        request.Format = format;
        request.Segments = segments;
        request.RequireComplete = requireComplete;
        request.AllowIdenticalDuplicates = allowIdenticalDuplicates;
        return request;
    }

    static BarcodeDecodeResult CreateSegment(
        BarcodeFormat format,
        int index,
        int count,
        string content,
        string? fileIdentifier)
    {
        StructuredAppendInfo information = new StructuredAppendInfo();
        information.Format = format;
        information.Index = index;
        information.Count = count;
        information.FileIdentifier = fileIdentifier;
        information.IsLastSegment = index == count - 1;
        BarcodePayload payload = new BarcodePayload();
        payload.Text = content;
        payload.RawBytes = Bytes(content);
        payload.CharacterEncoding = "ISO-8859-1";
        payload.IsGs1 = format == BarcodeFormat.Gs1DataMatrix;
        payload.StructuredAppend = information;
        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.StructuredAppend = ResultCopier.CopyStructuredAppendInfo(
            information);
        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Format = format;
        result.Dimension = BarcodeDimension.Matrix;
        result.Payload = payload;
        result.Metadata = metadata;
        return result;
    }

    static byte[] Bytes(string value)
    {
        return global::System.Text.Encoding.ASCII.GetBytes(value);
    }
}
