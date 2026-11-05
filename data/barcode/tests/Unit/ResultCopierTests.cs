namespace barcode.Tests;

public class ResultCopierTests
{
    [Test]
    public void EncodingContextReturnsDeepCopyOfMutableSymbolData()
    {
        BarcodePayload payload = CreatePayload();
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        quietZone.LeftModules = 2;
        BarcodeSize size = new BarcodeSize();
        size.WidthModules = 7;
        size.QuietZone = quietZone;
        BarcodeTextRun run = new BarcodeTextRun();
        run.Text = "123";
        run.X = 1.5;
        BarcodeTextLayout textLayout = new BarcodeTextLayout();
        textLayout.Runs = new BarcodeTextRun[] { run };
        textLayout.BarExtensions = new BarcodeRectangle[]
        {
            new BarcodeRectangle { X = 2, Y = 3, Width = 1, Height = 5 }
        };
        textLayout.Bounds = new BarcodeRectangle { X = 1, Y = 2, Width = 3, Height = 4 };
        textLayout.Size = size;
        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = BarcodeFormat.Code128;
        symbol.Dimension = BarcodeDimension.Linear;
        symbol.WidthModules = 3;
        symbol.HeightModules = 1;
        symbol.Modules = new byte[] { 1, 0, 1 };
        symbol.Payload = payload;
        symbol.Size = size;
        symbol.RequiredQuietZone = quietZone;
        symbol.TextLayout = textLayout;
        symbol.BearerBars = new BarcodeRectangle[]
        {
            new BarcodeRectangle { X = 0, Y = 0, Width = 7, Height = 1 }
        };
        symbol.Codewords = new int[] { 12, 34 };
        symbol.HasChecksum = true;
        symbol.ChecksumValue = 46;
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        request.Format = BarcodeFormat.Code128;
        request.Payload = payload;
        EncodingContext context = new EncodingContext(request);

        BarcodeSymbol copy = context.CopySymbolResult(symbol);
        symbol.Modules[0] = 0;
        symbol.Payload.RawBytes![0] = 99;
        symbol.Payload.EciSegments![0].AssignmentNumber = 999;
        symbol.Payload.StructuredAppend!.Index = 9;
        symbol.Size.QuietZone!.LeftModules = 8;
        symbol.RequiredQuietZone.LeftModules = 9;
        symbol.TextLayout.Runs![0].X = 9;
        symbol.TextLayout.BarExtensions![0].Height = 9;
        symbol.TextLayout.Bounds!.X = 9;
        symbol.BearerBars[0].Width = 99;
        symbol.Codewords[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.Not.SameAs(symbol));
            Assert.That(copy.Modules, Is.EqualTo(new byte[] { 1, 0, 1 }));
            Assert.That(copy.Modules, Is.Not.SameAs(symbol.Modules));
            Assert.That(copy.Payload!.RawBytes, Is.EqualTo(new byte[] { 10, 20 }));
            Assert.That(copy.Payload.EciSegments![0].AssignmentNumber, Is.EqualTo(26));
            Assert.That(copy.Payload.StructuredAppend!.Index, Is.EqualTo(1));
            Assert.That(copy.Size!.QuietZone!.LeftModules, Is.EqualTo(2));
            Assert.That(copy.RequiredQuietZone!.LeftModules, Is.EqualTo(2));
            Assert.That(copy.TextLayout!.Runs![0].X, Is.EqualTo(1.5));
            Assert.That(copy.TextLayout.BarExtensions![0].Height,
                Is.EqualTo(5));
            Assert.That(copy.TextLayout.Bounds!.X, Is.EqualTo(1));
            Assert.That(copy.BearerBars![0].Width, Is.EqualTo(7));
            Assert.That(copy.Codewords, Is.EqualTo(new int[] { 12, 34 }));
        });
    }

    [Test]
    public void DecodingContextReturnsDeepCopyOfAttemptResultsAndDiagnostics()
    {
        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.Codewords = new int[] { 4, 5 };
        metadata.Confidence = 0.75;
        metadata.StructuredAppend = new StructuredAppendInfo { Index = 2, Count = 3 };
        BarcodeQuadrilateral location = new BarcodeQuadrilateral();
        location.TopLeft = new BarcodePoint { X = 1, Y = 2 };
        location.Bounds = new BarcodeRectangle { X = 1, Y = 2, Width = 8, Height = 9 };
        BarcodeDiagnostic diagnostic = new BarcodeDiagnostic();
        diagnostic.Code = "EDGE";
        diagnostic.Location = new BarcodePoint { X = 3, Y = 4 };
        diagnostic.Region = new BarcodeRectangle { X = 5, Y = 6, Width = 7, Height = 8 };
        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Payload = CreatePayload();
        result.Format = BarcodeFormat.DataMatrix;
        result.Dimension = BarcodeDimension.Matrix;
        result.Location = location;
        result.Metadata = metadata;
        result.Diagnostics = new BarcodeDiagnostic[] { diagnostic };
        BarcodeDecodeAttempt attempt = new BarcodeDecodeAttempt();
        attempt.Success = true;
        attempt.Results = new BarcodeDecodeResult[] { result };
        attempt.Diagnostics = new BarcodeDiagnostic[] { diagnostic };
        BarcodeDecodeRequest request = new BarcodeDecodeRequest();
        DecodingContext context = new DecodingContext(request);

        BarcodeDecodeAttempt copy = context.CopyDecodeAttemptResult(attempt);
        result.Payload.RawBytes![0] = 99;
        result.Location!.TopLeft!.X = 99;
        result.Location.Bounds!.Width = 99;
        result.Metadata!.Codewords![0] = 99;
        result.Metadata.StructuredAppend!.Index = 99;
        diagnostic.Location!.X = 99;
        diagnostic.Region!.Width = 99;

        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.Not.SameAs(attempt));
            Assert.That(copy.Results, Is.Not.SameAs(attempt.Results));
            Assert.That(copy.Results![0], Is.Not.SameAs(result));
            Assert.That(copy.Results[0].Payload!.RawBytes, Is.EqualTo(new byte[] { 10, 20 }));
            Assert.That(copy.Results[0].Location!.TopLeft!.X, Is.EqualTo(1));
            Assert.That(copy.Results[0].Location!.Bounds!.Width, Is.EqualTo(8));
            Assert.That(copy.Results[0].Metadata!.Codewords, Is.EqualTo(new int[] { 4, 5 }));
            Assert.That(copy.Results[0].Metadata!.StructuredAppend!.Index, Is.EqualTo(2));
            Assert.That(copy.Diagnostics![0].Location!.X, Is.EqualTo(3));
            Assert.That(copy.Diagnostics[0].Region!.Width, Is.EqualTo(7));
            Assert.That(copy.Results[0].Diagnostics![0], Is.Not.SameAs(copy.Diagnostics[0]));
        });
    }

    [Test]
    public void ResultCopyingPreservesNullOptionalDataAndRejectsNullRoots()
    {
        BarcodeEncodeRequest encodeRequest = new BarcodeEncodeRequest();
        encodeRequest.Format = BarcodeFormat.Code39;
        EncodingContext encoding = new EncodingContext(encodeRequest);
        DecodingContext decoding = new DecodingContext(new BarcodeDecodeRequest());
        BarcodeSymbol symbol = new BarcodeSymbol();
        BarcodeDecodeResult result = new BarcodeDecodeResult();

        BarcodeSymbol symbolCopy = encoding.CopySymbolResult(symbol);
        BarcodeDecodeResult[] resultCopies = decoding.CopyDecodeResultsResult(new BarcodeDecodeResult[] { result });

        Assert.Multiple(() =>
        {
            Assert.That(symbolCopy.Payload, Is.Null);
            Assert.That(symbolCopy.Modules, Is.Null);
            Assert.That(resultCopies[0].Payload, Is.Null);
            Assert.That(resultCopies[0].Diagnostics, Is.Null);
            Assert.That(() => encoding.CopySymbolResult(null!), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => decoding.CopyDecodeResultsResult(null!), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => decoding.CopyDecodeAttemptResult(null!), Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    private static BarcodePayload CreatePayload()
    {
        EciSegment segment = new EciSegment();
        segment.AssignmentNumber = 26;
        segment.ByteOffset = 0;
        segment.ByteCount = 2;
        StructuredAppendInfo append = new StructuredAppendInfo();
        append.Format = BarcodeFormat.DataMatrix;
        append.Index = 1;
        append.Count = 2;
        BarcodePayload payload = new BarcodePayload();
        payload.Text = "payload";
        payload.RawBytes = new byte[] { 10, 20 };
        payload.CharacterEncoding = "UTF-8";
        payload.EciSegments = new EciSegment[] { segment };
        payload.IsGs1 = true;
        payload.StructuredAppend = append;
        return payload;
    }
}
