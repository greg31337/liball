namespace json.Tests;

public class JsonResultDataTests
{
    [Test]
    public void ResultDefaultsContainNoPayloadOrDiagnostic()
    {
        JsonFeedResult feed = new JsonFeedResult();
        JsonTokenResult token = new JsonTokenResult();
        JsonValueResult value = new JsonValueResult();
        JsonDocumentResult document = new JsonDocumentResult();
        JsonOperationResult operation = new JsonOperationResult();
        JsonInt64Result signed = new JsonInt64Result();
        JsonUInt64Result unsigned = new JsonUInt64Result();
        JsonDecimalResult decimalResult = new JsonDecimalResult();
        JsonDoubleResult doubleResult = new JsonDoubleResult();
        JsonPointerResult pointer = new JsonPointerResult();

        Assert.Multiple(() =>
        {
            Assert.That(feed.BytesAccepted, Is.Zero);
            Assert.That(feed.BytesConsumed, Is.Zero);
            Assert.That(feed.BytesWritten, Is.Zero);
            Assert.That(feed.Diagnostic, Is.Null);
            Assert.That(token.Token, Is.Null);
            Assert.That(token.Diagnostic, Is.Null);
            Assert.That(value.Success, Is.False);
            Assert.That(value.Value, Is.Null);
            Assert.That(value.Diagnostic, Is.Null);
            Assert.That(document.Success, Is.False);
            Assert.That(document.Document, Is.Null);
            Assert.That(document.Diagnostic, Is.Null);
            Assert.That(operation.Success, Is.False);
            Assert.That(operation.BytesConsumed, Is.Zero);
            Assert.That(operation.BytesWritten, Is.Zero);
            Assert.That(operation.Diagnostic, Is.Null);
            Assert.That(signed.Success, Is.False);
            Assert.That(signed.Value, Is.Zero);
            Assert.That(signed.Diagnostic, Is.Null);
            Assert.That(unsigned.Success, Is.False);
            Assert.That(unsigned.Value, Is.Zero);
            Assert.That(unsigned.Diagnostic, Is.Null);
            Assert.That(decimalResult.Success, Is.False);
            Assert.That(decimalResult.Value, Is.Zero);
            Assert.That(decimalResult.Diagnostic, Is.Null);
            Assert.That(doubleResult.Success, Is.False);
            Assert.That(doubleResult.Value, Is.Zero);
            Assert.That(doubleResult.Diagnostic, Is.Null);
            Assert.That(pointer.Success, Is.False);
            Assert.That(pointer.Value, Is.Null);
            Assert.That(pointer.Values, Is.Null);
            Assert.That(pointer.FailedSegmentIndex, Is.EqualTo(-1));
            Assert.That(pointer.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void SourcePositionsAndRangesRetainLocationData()
    {
        JsonSourcePosition start = new JsonSourcePosition();
        start.ByteOffset = 10;
        start.Line = 2;
        start.ByteColumn = 3;
        start.ScalarColumn = 4;
        JsonSourcePosition end = new JsonSourcePosition();
        end.ByteOffset = 15;
        end.Line = 2;
        end.ByteColumn = 8;
        end.ScalarColumn = 9;
        JsonSourceRange range = new JsonSourceRange();
        range.Start = start;
        range.End = end;

        Assert.Multiple(() =>
        {
            Assert.That(range.Start, Is.SameAs(start));
            Assert.That(range.End, Is.SameAs(end));
            Assert.That(range.Start?.ByteOffset, Is.EqualTo(10));
            Assert.That(range.Start?.Line, Is.EqualTo(2));
            Assert.That(range.Start?.ByteColumn, Is.EqualTo(3));
            Assert.That(range.Start?.ScalarColumn, Is.EqualTo(4));
            Assert.That(range.End?.ByteOffset, Is.EqualTo(15));
        });
    }

    [Test]
    public void DiagnosticRetainsAllFailureContext()
    {
        JsonSourceRange range = CreateRange();
        byte[] excerpt = { 0x5B, 0x31, 0x2C };
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.TrailingComma;
        diagnostic.Message = "Trailing comma.";
        diagnostic.Range = range;
        diagnostic.Pointer = "/items/1";
        diagnostic.Depth = 2;
        diagnostic.ExpectedTokens = "value";
        diagnostic.ActualToken = "]";
        diagnostic.SourceExcerpt = excerpt;

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.TrailingComma));
            Assert.That(diagnostic.Message, Is.EqualTo("Trailing comma."));
            Assert.That(diagnostic.Range, Is.SameAs(range));
            Assert.That(diagnostic.Pointer, Is.EqualTo("/items/1"));
            Assert.That(diagnostic.Depth, Is.EqualTo(2));
            Assert.That(diagnostic.ExpectedTokens, Is.EqualTo("value"));
            Assert.That(diagnostic.ActualToken, Is.EqualTo("]"));
            Assert.That(diagnostic.SourceExcerpt, Is.SameAs(excerpt));
        });
    }

    [Test]
    public void FeedAndOperationResultsRetainCountsStatusesAndDiagnostics()
    {
        JsonDiagnostic diagnostic = CreateDiagnostic();
        JsonFeedResult feed = new JsonFeedResult();
        feed.BytesAccepted = 11;
        feed.BytesConsumed = 9;
        feed.BytesWritten = 7;
        feed.ReadStatus = JsonReadStatus.NeedMoreData;
        feed.WriteStatus = JsonWriteStatus.Ready;
        feed.Diagnostic = diagnostic;
        JsonOperationResult operation = new JsonOperationResult();
        operation.Success = true;
        operation.BytesConsumed = 4_000_000_000;
        operation.BytesWritten = 5_000_000_000;
        operation.Diagnostic = diagnostic;

        Assert.Multiple(() =>
        {
            Assert.That(feed.BytesAccepted, Is.EqualTo(11));
            Assert.That(feed.BytesConsumed, Is.EqualTo(9));
            Assert.That(feed.BytesWritten, Is.EqualTo(7));
            Assert.That(feed.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(feed.WriteStatus, Is.EqualTo(JsonWriteStatus.Ready));
            Assert.That(feed.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(operation.Success, Is.True);
            Assert.That(operation.BytesConsumed, Is.EqualTo(4_000_000_000));
            Assert.That(operation.BytesWritten, Is.EqualTo(5_000_000_000));
            Assert.That(operation.Diagnostic, Is.SameAs(diagnostic));
        });
    }

    [Test]
    public void TokenRetainsDecodedRawLocationAndPointerPayloads()
    {
        JsonNumber number = new JsonNumber();
        number.Kind = JsonNumberKind.Integer;
        byte[] raw = { 0x31 };
        JsonPointer pointer = CreatePointer();
        JsonSourceRange range = CreateRange();
        JsonToken token = new JsonToken();
        token.Kind = JsonTokenKind.Number;
        token.StringValue = "decoded";
        token.Number = number;
        token.BooleanValue = true;
        token.RawUtf8 = raw;
        token.Depth = 3;
        token.Pointer = pointer;
        token.Range = range;

        Assert.Multiple(() =>
        {
            Assert.That(token.Kind, Is.EqualTo(JsonTokenKind.Number));
            Assert.That(token.StringValue, Is.EqualTo("decoded"));
            Assert.That(token.Number, Is.SameAs(number));
            Assert.That(token.BooleanValue, Is.True);
            Assert.That(token.RawUtf8, Is.SameAs(raw));
            Assert.That(token.Depth, Is.EqualTo(3));
            Assert.That(token.Pointer, Is.SameAs(pointer));
            Assert.That(token.Range, Is.SameAs(range));
        });
    }

    [Test]
    public void DocumentAndReadResultsRetainOwnedValuesAndDiagnostics()
    {
        JsonSourceRange range = CreateRange();
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.String;
        value.Range = range;
        JsonDocument document = new JsonDocument();
        document.Root = value;
        document.ConformanceProfile = JsonConformanceProfile.Portable;
        document.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        document.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        document.SourceByteCount = 6_000_000_000;
        JsonDiagnostic diagnostic = CreateDiagnostic();
        JsonToken token = new JsonToken();
        JsonTokenResult tokenResult = new JsonTokenResult();
        tokenResult.Status = JsonReadStatus.TokenAvailable;
        tokenResult.Token = token;
        tokenResult.Diagnostic = diagnostic;
        JsonValueResult valueResult = new JsonValueResult();
        valueResult.Success = true;
        valueResult.Status = JsonReadStatus.DocumentComplete;
        valueResult.Value = value;
        valueResult.Diagnostic = diagnostic;
        JsonDocumentResult documentResult = new JsonDocumentResult();
        documentResult.Success = true;
        documentResult.Status = JsonReadStatus.StreamComplete;
        documentResult.Document = document;
        documentResult.Diagnostic = diagnostic;

        Assert.Multiple(() =>
        {
            Assert.That(value.Range, Is.SameAs(range));
            Assert.That(document.Root, Is.SameAs(value));
            Assert.That(document.ConformanceProfile, Is.EqualTo(JsonConformanceProfile.Portable));
            Assert.That(document.DuplicateKeyPolicy, Is.EqualTo(JsonDuplicateKeyPolicy.Reject));
            Assert.That(document.UnicodePolicy, Is.EqualTo(JsonUnicodePolicy.UnicodeScalar));
            Assert.That(document.SourceByteCount, Is.EqualTo(6_000_000_000));
            Assert.That(tokenResult.Status, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(tokenResult.Token, Is.SameAs(token));
            Assert.That(tokenResult.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(valueResult.Success, Is.True);
            Assert.That(valueResult.Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
            Assert.That(valueResult.Value, Is.SameAs(value));
            Assert.That(valueResult.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(documentResult.Success, Is.True);
            Assert.That(documentResult.Status, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(documentResult.Document, Is.SameAs(document));
            Assert.That(documentResult.Diagnostic, Is.SameAs(diagnostic));
        });
    }

    [Test]
    public void NumericConversionResultsRetainExactClrValues()
    {
        JsonDiagnostic diagnostic = CreateDiagnostic();
        JsonInt64Result signed = new JsonInt64Result();
        signed.Success = true;
        signed.Value = long.MinValue;
        signed.Diagnostic = diagnostic;
        JsonUInt64Result unsigned = new JsonUInt64Result();
        unsigned.Success = true;
        unsigned.Value = ulong.MaxValue;
        unsigned.Diagnostic = diagnostic;
        JsonDecimalResult decimalResult = new JsonDecimalResult();
        decimalResult.Success = true;
        decimalResult.Value = 79228162514264337593543950335m;
        decimalResult.Diagnostic = diagnostic;
        JsonDoubleResult doubleResult = new JsonDoubleResult();
        doubleResult.Success = true;
        doubleResult.Value = double.Epsilon;
        doubleResult.Diagnostic = diagnostic;

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(signed.Value, Is.EqualTo(long.MinValue));
            Assert.That(signed.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(unsigned.Success, Is.True);
            Assert.That(unsigned.Value, Is.EqualTo(ulong.MaxValue));
            Assert.That(unsigned.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(decimalResult.Success, Is.True);
            Assert.That(decimalResult.Value, Is.EqualTo(decimal.MaxValue));
            Assert.That(decimalResult.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(doubleResult.Success, Is.True);
            Assert.That(doubleResult.Value, Is.EqualTo(double.Epsilon));
            Assert.That(doubleResult.Diagnostic, Is.SameAs(diagnostic));
        });
    }

    [Test]
    public void PointerResultAndVisitRetainTraversalContext()
    {
        JsonPointer pointer = CreatePointer();
        JsonValue parent = new JsonValue();
        parent.Kind = JsonValueKind.Array;
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Null;
        List<JsonValue> values = new List<JsonValue> { value };
        JsonDiagnostic diagnostic = CreateDiagnostic();
        JsonPointerResult pointerResult = new JsonPointerResult();

        Assert.That(pointerResult.FailedSegmentIndex, Is.EqualTo(-1));

        pointerResult.Success = true;
        pointerResult.Value = value;
        pointerResult.Values = values;
        pointerResult.FailedSegmentIndex = 2;
        pointerResult.Diagnostic = diagnostic;
        JsonVisit visit = new JsonVisit();

        Assert.That(visit.ArrayIndex, Is.EqualTo(-1));

        visit.Value = value;
        visit.Parent = parent;
        visit.MemberName = "item";
        visit.ArrayIndex = 4;
        visit.Pointer = pointer;
        visit.Depth = 2;
        visit.Range = CreateRange();

        Assert.Multiple(() =>
        {
            Assert.That(pointer.Encoding, Is.EqualTo(JsonPointerEncoding.String));
            Assert.That(pointer.OriginalRepresentation, Is.EqualTo("/items/4"));
            Assert.That(pointer.ReferenceTokens, Is.EqualTo(new[] { "items", "4" }));
            Assert.That(pointerResult.Success, Is.True);
            Assert.That(pointerResult.Value, Is.SameAs(value));
            Assert.That(pointerResult.Values, Is.SameAs(values));
            Assert.That(pointerResult.FailedSegmentIndex, Is.EqualTo(2));
            Assert.That(pointerResult.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(visit.Value, Is.SameAs(value));
            Assert.That(visit.Parent, Is.SameAs(parent));
            Assert.That(visit.MemberName, Is.EqualTo("item"));
            Assert.That(visit.ArrayIndex, Is.EqualTo(4));
            Assert.That(visit.Pointer, Is.SameAs(pointer));
            Assert.That(visit.Depth, Is.EqualTo(2));
            Assert.That(visit.Range, Is.Not.Null);
        });
    }

    private static JsonSourceRange CreateRange()
    {
        JsonSourcePosition start = new JsonSourcePosition();
        start.ByteOffset = 1;
        JsonSourcePosition end = new JsonSourcePosition();
        end.ByteOffset = 2;
        JsonSourceRange range = new JsonSourceRange();
        range.Start = start;
        range.End = end;
        return range;
    }

    private static JsonDiagnostic CreateDiagnostic()
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InvalidNumber;
        return diagnostic;
    }

    private static JsonPointer CreatePointer()
    {
        JsonPointer pointer = new JsonPointer();
        pointer.Encoding = JsonPointerEncoding.String;
        pointer.OriginalRepresentation = "/items/4";
        pointer.ReferenceTokens = new List<string> { "items", "4" };
        return pointer;
    }
}
