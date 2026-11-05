namespace barcode.Tests;

public class StructuredAppendAssemblerTests
{
    [Test]
    public void PublicAssemblerDispatchesDataMatrixAndReturnsDefensiveCopies()
    {
        BarcodeDecodeResult first = CreateSegment(
            BarcodeFormat.DataMatrix,
            0,
            2,
            "LEFT",
            "1");
        BarcodeDecodeResult last = CreateSegment(
            BarcodeFormat.DataMatrix,
            1,
            2,
            "RIGHT",
            "001001");
        StructuredAppendRequest request = CreateRequest(
            BarcodeFormat.DataMatrix,
            new BarcodeDecodeResult[] { last, first });
        IStructuredAppendAssembler assembler =
            new StructuredAppendAssembler();

        StructuredAppendResult result =
            assembler.AssembleStructuredAppend(request);
        first.Payload!.RawBytes![0] = 0;
        first.Payload.Text = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(result.Complete, Is.True);
            Assert.That(result.Payload!.Text, Is.EqualTo("LEFTRIGHT"));
            Assert.That(result.Payload.RawBytes, Is.EqualTo(Bytes("LEFTRIGHT")));
            Assert.That(result.FileIdentifier, Is.EqualTo("001001"));
            Assert.That(result.Segments, Has.Length.EqualTo(2));
            Assert.That(result.Segments, Is.Not.SameAs(request.Segments));
            Assert.That(result.Segments![0], Is.Not.SameAs(first));
            Assert.That(result.Segments[0].Payload, Is.Not.SameAs(first.Payload));
            Assert.That(result.MissingIndices, Is.Empty);
            Assert.That(result.Conflicts, Is.Empty);
        });
    }

    [Test]
    public void PublicAssemblerDispatchesMacroPdf417()
    {
        BarcodeDecodeResult first = CreateSegment(
            BarcodeFormat.MacroPdf417,
            0,
            2,
            "MACRO-",
            "100200");
        BarcodeDecodeResult last = CreateSegment(
            BarcodeFormat.MacroPdf417,
            1,
            2,
            "PDF417",
            "100200");
        StructuredAppendRequest request = CreateRequest(
            BarcodeFormat.MacroPdf417,
            new BarcodeDecodeResult[] { last, first });
        IStructuredAppendAssembler assembler =
            new StructuredAppendAssembler();

        StructuredAppendResult result =
            assembler.AssembleStructuredAppend(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Complete, Is.True);
            Assert.That(result.Payload!.Text, Is.EqualTo("MACRO-PDF417"));
            Assert.That(result.Payload.RawBytes, Is.EqualTo(Bytes("MACRO-PDF417")));
            Assert.That(result.FileIdentifier, Is.EqualTo("100200"));
            Assert.That(result.Segments, Has.Length.EqualTo(2));
            Assert.That(result.Segments![0].Format,
                Is.EqualTo(BarcodeFormat.MacroPdf417));
            Assert.That(result.Segments[0], Is.Not.SameAs(first));
        });
    }

    [Test]
    public void PublicAssemblerReportsMissingMacroSegmentsAndHonorsCompletionPolicy()
    {
        BarcodeDecodeResult first = CreateSegment(
            BarcodeFormat.MacroPdf417,
            0,
            3,
            "FIRST",
            "300400");
        BarcodeDecodeResult last = CreateSegment(
            BarcodeFormat.MacroPdf417,
            2,
            3,
            "LAST",
            "300400");
        StructuredAppendRequest request = CreateRequest(
            BarcodeFormat.MacroPdf417,
            new BarcodeDecodeResult[] { last, first });
        IStructuredAppendAssembler assembler =
            new StructuredAppendAssembler();

        StructuredAppendResult partial =
            assembler.AssembleStructuredAppend(request);
        request.RequireComplete = true;

        Assert.Multiple(() =>
        {
            Assert.That(partial.Complete, Is.False);
            Assert.That(partial.Payload?.Text, Is.EqualTo("FIRSTLAST"));
            Assert.That(partial.MissingIndices, Is.EqualTo(new int[] { 1 }));
            Assert.That(partial.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code
                        == "MACRO_PDF417_SEGMENTS_MISSING"));
            Assert.That(
                () => assembler.AssembleStructuredAppend(request),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void PublicAssemblerRejectsSegmentsFromAnotherFormat()
    {
        BarcodeDecodeResult macro = CreateSegment(
            BarcodeFormat.MacroPdf417,
            0,
            2,
            "DATA",
            "100200");
        StructuredAppendRequest request = CreateRequest(
            BarcodeFormat.DataMatrix,
            new BarcodeDecodeResult[] { macro });
        IStructuredAppendAssembler assembler =
            new StructuredAppendAssembler();

        Assert.That(
            () => assembler.AssembleStructuredAppend(request),
            Throws.TypeOf<BarcodeValidationException>());
    }

    static StructuredAppendRequest CreateRequest(
        BarcodeFormat format,
        BarcodeDecodeResult[] segments)
    {
        StructuredAppendRequest request = new StructuredAppendRequest();
        request.Format = format;
        request.Segments = segments;
        request.RequireComplete = false;
        request.AllowIdenticalDuplicates = false;
        return request;
    }

    static BarcodeDecodeResult CreateSegment(
        BarcodeFormat format,
        int index,
        int count,
        string content,
        string fileIdentifier)
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
        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Format = format;
        result.Dimension = format == BarcodeFormat.MacroPdf417
            ? BarcodeDimension.StackedLinear
            : BarcodeDimension.Matrix;
        result.Payload = payload;
        result.Metadata = new BarcodeDecodeMetadata();
        result.Metadata.StructuredAppend = ResultCopier.CopyStructuredAppendInfo(
            information);
        return result;
    }

    static byte[] Bytes(string value)
    {
        return global::System.Text.Encoding.ASCII.GetBytes(value);
    }
}
