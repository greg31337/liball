namespace json.Tests;

public class JsonEnumTests
{
    [Test]
    public void EnumDefinitionsHaveStableNamesAndValues()
    {
        AssertEnum<JsonValueKind>(
            ("Uninitialized", 0), ("Null", 1), ("Boolean", 2), ("Number", 3),
            ("String", 4), ("Array", 5), ("Object", 6));
        AssertEnum<JsonTokenKind>(
            ("None", 0), ("StartObject", 1), ("EndObject", 2), ("StartArray", 3),
            ("EndArray", 4), ("PropertyName", 5), ("Null", 6), ("Boolean", 7),
            ("Number", 8), ("String", 9), ("DocumentBoundary", 10));
        AssertEnum<JsonReadStatus>(
            ("TokenAvailable", 0), ("NeedMoreData", 1), ("DocumentComplete", 2),
            ("StreamComplete", 3), ("Paused", 4), ("Stopped", 5), ("Error", 6));
        AssertEnum<JsonWriteStatus>(("Ready", 0), ("Complete", 1), ("Error", 2));
        AssertEnum<JsonConformanceProfile>(
            ("SyntaxCompatible", 0), ("Portable", 1), ("IJson", 2), ("Jcs", 3));
        AssertEnum<JsonDuplicateKeyPolicy>(
            ("Preserve", 0), ("Reject", 1), ("KeepFirst", 2), ("KeepLast", 3));
        AssertEnum<JsonInputMode>(
            ("SingleValue", 0), ("WhitespaceSeparated", 1), ("JsonTextSequence", 2));
        AssertEnum<JsonSequenceErrorPolicy>(("Stop", 0), ("Resynchronize", 1));
        AssertEnum<JsonUnicodePolicy>(("SyntaxCompatible", 0), ("UnicodeScalar", 1));
        AssertEnum<JsonBomPolicy>(("Reject", 0), ("AllowInitial", 1));
        AssertEnum<JsonNumberKind>(
            ("Integer", 0), ("Fraction", 1), ("Exponent", 2), ("FractionWithExponent", 3));
        AssertEnum<JsonFormatting>(("Compact", 0), ("Pretty", 1));
        AssertEnum<JsonPropertyOrder>(
            ("Preserve", 0), ("OrdinalUtf16", 1), ("OrdinalUnicodeScalar", 2), ("Jcs", 3));
        AssertEnum<JsonStringEscapeMode>(("Minimal", 0), ("AsciiOnly", 1), ("HtmlSafe", 2));
        AssertEnum<JsonComparisonMode>(("ExactModel", 0), ("JsonSemantic", 1));
        AssertEnum<JsonHandlerAction>(("Continue", 0), ("Pause", 1), ("Stop", 2));
        AssertEnum<JsonVisitAction>(("Continue", 0), ("SkipChildren", 1), ("Stop", 2));
        AssertEnum<JsonPointerEncoding>(("String", 0), ("UriFragment", 1));
        AssertEnum<JsonDiagnosticCode>(
            ("None", 0),
            ("UnexpectedCharacter", 1),
            ("InvalidWhitespace", 2),
            ("InvalidLiteral", 3),
            ("InvalidNumber", 4),
            ("InvalidStringCharacter", 5),
            ("InvalidEscape", 6),
            ("InvalidUnicodeEscape", 7),
            ("InvalidSurrogatePair", 8),
            ("InvalidUtf8", 9),
            ("UnexpectedEndOfInput", 10),
            ("UnexpectedToken", 11),
            ("MissingPropertyName", 12),
            ("MissingColon", 13),
            ("MissingCommaOrEnd", 14),
            ("TrailingComma", 15),
            ("TrailingContent", 16),
            ("DuplicatePropertyName", 17),
            ("BomNotAllowed", 18),
            ("UnexpectedBom", 19),
            ("MissingRecordSeparator", 20),
            ("EmptySequenceRecord", 21),
            ("IncompleteSequenceRecord", 22),
            ("DocumentByteLimitExceeded", 23),
            ("InputBufferLimitExceeded", 24),
            ("TokenByteLimitExceeded", 25),
            ("DecodedStringByteLimitExceeded", 26),
            ("DecodedStringCodeUnitLimitExceeded", 27),
            ("NumberCharacterLimitExceeded", 28),
            ("NestingDepthLimitExceeded", 29),
            ("ArrayItemLimitExceeded", 30),
            ("ObjectMemberLimitExceeded", 31),
            ("DomNodeLimitExceeded", 32),
            ("OutputByteLimitExceeded", 33),
            ("InvalidPointerSyntax", 34),
            ("InvalidPointerEscape", 35),
            ("InvalidPointerPercentEncoding", 36),
            ("InvalidPointerIndex", 37),
            ("PointerIndexOutOfRange", 38),
            ("PointerTargetNotFound", 39),
            ("PointerTypeMismatch", 40),
            ("InvalidModel", 41),
            ("ModelCycle", 42),
            ("InvalidWriterState", 43),
            ("InvalidRawValue", 44),
            ("NonFiniteNumber", 45),
            ("NumericConversionFailed", 46),
            ("InvalidOptions", 47),
            ("IoFailure", 48));
    }

    [Test]
    public void EnumBackedDataSurfacesStoreSelections()
    {
        JsonReaderOptions reader = new JsonReaderOptions();
        reader.ConformanceProfile = JsonConformanceProfile.IJson;
        reader.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.KeepLast;
        reader.InputMode = JsonInputMode.JsonTextSequence;
        reader.SequenceErrorPolicy = JsonSequenceErrorPolicy.Resynchronize;
        reader.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        reader.BomPolicy = JsonBomPolicy.AllowInitial;

        JsonWriterOptions writer = new JsonWriterOptions();
        writer.Formatting = JsonFormatting.Pretty;
        writer.StringEscapeMode = JsonStringEscapeMode.HtmlSafe;
        writer.PropertyOrder = JsonPropertyOrder.Jcs;
        writer.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        writer.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        writer.OutputMode = JsonInputMode.WhitespaceSeparated;

        JsonNumber number = new JsonNumber();
        number.Kind = JsonNumberKind.FractionWithExponent;
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Number;
        value.Number = number;
        JsonToken token = new JsonToken();
        token.Kind = JsonTokenKind.Number;
        token.Number = number;
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InvalidNumber;
        JsonFeedResult feed = new JsonFeedResult();
        feed.ReadStatus = JsonReadStatus.Error;
        feed.WriteStatus = JsonWriteStatus.Error;
        feed.Diagnostic = diagnostic;
        JsonPointer pointer = new JsonPointer();
        pointer.Encoding = JsonPointerEncoding.UriFragment;
        JsonVisit visit = new JsonVisit();
        visit.Value = value;
        visit.Pointer = pointer;

        Assert.Multiple(() =>
        {
            Assert.That(reader.ConformanceProfile, Is.EqualTo(JsonConformanceProfile.IJson));
            Assert.That(reader.DuplicateKeyPolicy, Is.EqualTo(JsonDuplicateKeyPolicy.KeepLast));
            Assert.That(reader.InputMode, Is.EqualTo(JsonInputMode.JsonTextSequence));
            Assert.That(reader.SequenceErrorPolicy, Is.EqualTo(JsonSequenceErrorPolicy.Resynchronize));
            Assert.That(reader.UnicodePolicy, Is.EqualTo(JsonUnicodePolicy.UnicodeScalar));
            Assert.That(reader.BomPolicy, Is.EqualTo(JsonBomPolicy.AllowInitial));
            Assert.That(writer.Formatting, Is.EqualTo(JsonFormatting.Pretty));
            Assert.That(writer.StringEscapeMode, Is.EqualTo(JsonStringEscapeMode.HtmlSafe));
            Assert.That(writer.PropertyOrder, Is.EqualTo(JsonPropertyOrder.Jcs));
            Assert.That(writer.DuplicateKeyPolicy, Is.EqualTo(JsonDuplicateKeyPolicy.Reject));
            Assert.That(writer.UnicodePolicy, Is.EqualTo(JsonUnicodePolicy.UnicodeScalar));
            Assert.That(writer.OutputMode, Is.EqualTo(JsonInputMode.WhitespaceSeparated));
            Assert.That(value.Kind, Is.EqualTo(JsonValueKind.Number));
            Assert.That(value.Number, Is.SameAs(number));
            Assert.That(token.Kind, Is.EqualTo(JsonTokenKind.Number));
            Assert.That(token.Number, Is.SameAs(number));
            Assert.That(feed.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(feed.WriteStatus, Is.EqualTo(JsonWriteStatus.Error));
            Assert.That(feed.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(visit.Value, Is.SameAs(value));
            Assert.That(visit.Pointer, Is.SameAs(pointer));
            Assert.That(pointer.Encoding, Is.EqualTo(JsonPointerEncoding.UriFragment));
        });
    }

    private static void AssertEnum<TEnum>(params (string Name, int Value)[] expected)
        where TEnum : struct, Enum
    {
        (string Name, int Value)[] actual = Enum.GetValues<TEnum>()
            .Select(value => (value.ToString(), Convert.ToInt32(value)))
            .ToArray();

        Assert.That(actual, Is.EqualTo(expected));
    }
}
