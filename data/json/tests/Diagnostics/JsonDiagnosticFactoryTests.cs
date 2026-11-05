namespace json.Tests;

public class JsonDiagnosticFactoryTests
{
    [Test]
    public void FactoryCreatesAnOwnedDiagnosticSnapshotWithAllContext()
    {
        byte[] source = CreateSequentialBytes(20);
        JsonSourceRange range = CreateRange(110, 113);
        JsonSourcePosition originalStart = range.Start
            ?? throw new InvalidOperationException("Expected start position.");
        JsonDiagnosticFactory factory = new JsonDiagnosticFactory(6);

        JsonDiagnostic diagnostic = factory.CreateDiagnostic(
            JsonDiagnosticCode.UnexpectedToken,
            "A value was expected.",
            range,
            "/a~1b/0",
            2,
            "string, number, object, or array",
            "]",
            source,
            100);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.UnexpectedToken));
            Assert.That(diagnostic.Message, Is.EqualTo("A value was expected."));
            Assert.That(diagnostic.Pointer, Is.EqualTo("/a~1b/0"));
            Assert.That(diagnostic.Depth, Is.EqualTo(2));
            Assert.That(diagnostic.ExpectedTokens, Is.EqualTo("string, number, object, or array"));
            Assert.That(diagnostic.ActualToken, Is.EqualTo("]"));
            Assert.That(diagnostic.SourceExcerpt, Is.EqualTo(new byte[] { 7, 8, 9, 10, 11, 12 }));
            Assert.That(diagnostic.Range, Is.Not.SameAs(range));
            Assert.That(diagnostic.Range?.Start, Is.Not.SameAs(range.Start));
            Assert.That(diagnostic.Range?.Start?.ByteOffset, Is.EqualTo(110));
            Assert.That(diagnostic.Range?.Start?.Line, Is.EqualTo(3));
            Assert.That(diagnostic.Range?.End?.ByteOffset, Is.EqualTo(113));
        });

        source[7] = 200;
        originalStart.ByteOffset = 999;

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.SourceExcerpt?[0], Is.EqualTo(7));
            Assert.That(diagnostic.Range?.Start?.ByteOffset, Is.EqualTo(110));
        });
    }

    [Test]
    public void ExcerptWindowClampsToAvailableSourceBoundaries()
    {
        byte[] source = CreateSequentialBytes(10);
        JsonDiagnosticFactory factory = new JsonDiagnosticFactory(5);

        JsonDiagnostic atStart = CreateWithRange(factory, source, 50, CreateRange(50, 51));
        JsonDiagnostic beforeWindow = CreateWithRange(factory, source, 50, CreateRange(40, 41));
        JsonDiagnostic atEnd = CreateWithRange(factory, source, 50, CreateRange(60, 60));
        JsonDiagnostic afterWindow = CreateWithRange(factory, source, 50, CreateRange(500, 501));
        JsonDiagnostic withoutRange = CreateWithRange(factory, source, 50, null);

        Assert.Multiple(() =>
        {
            Assert.That(atStart.SourceExcerpt, Is.EqualTo(new byte[] { 0, 1, 2, 3, 4 }));
            Assert.That(beforeWindow.SourceExcerpt, Is.EqualTo(new byte[] { 0, 1, 2, 3, 4 }));
            Assert.That(atEnd.SourceExcerpt, Is.EqualTo(new byte[] { 5, 6, 7, 8, 9 }));
            Assert.That(afterWindow.SourceExcerpt, Is.EqualTo(new byte[] { 5, 6, 7, 8, 9 }));
            Assert.That(withoutRange.SourceExcerpt, Is.EqualTo(new byte[] { 0, 1, 2, 3, 4 }));
        });
    }

    [Test]
    public void ExcerptsUseTheConfiguredLimitAndOwnSmallInputs()
    {
        JsonDiagnosticFactory defaultFactory = new JsonDiagnosticFactory(0);
        byte[] largeSource = CreateSequentialBytes(300);
        JsonDiagnostic bounded = CreateWithRange(
            defaultFactory,
            largeSource,
            0,
            CreateRange(150, 151));

        JsonDiagnosticFactory largeFactory = new JsonDiagnosticFactory(10);
        byte[] smallSource = new byte[] { 4, 5, 6 };
        JsonDiagnostic small = CreateWithRange(largeFactory, smallSource, 0, CreateRange(1, 2));
        JsonDiagnostic absent = CreateWithRange(largeFactory, global::System.Array.Empty<byte>(), 0, null);

        Assert.Multiple(() =>
        {
            Assert.That(defaultFactory.GetMaximumExcerptBytes(), Is.EqualTo(JsonDefaults.MaximumDiagnosticExcerptBytes));
            Assert.That(bounded.SourceExcerpt, Has.Length.EqualTo(JsonDefaults.MaximumDiagnosticExcerptBytes));
            Assert.That(bounded.SourceExcerpt?[0], Is.EqualTo(22));
            Assert.That(bounded.SourceExcerpt?[255], Is.EqualTo(21));
            Assert.That(small.SourceExcerpt, Is.EqualTo(new byte[] { 4, 5, 6 }));
            Assert.That(small.SourceExcerpt, Is.Not.SameAs(smallSource));
            Assert.That(absent.SourceExcerpt, Is.Null);
        });

        smallSource[0] = 99;
        Assert.That(small.SourceExcerpt?[0], Is.EqualTo(4));
    }

    [Test]
    public void ExpectedAndActualContextAreBoundedWithoutSplittingSurrogates()
    {
        JsonDiagnosticFactory factory = new JsonDiagnosticFactory(8);
        string expected = new string('e', 300);
        string actual = new string('a', 255) + "😀tail";

        JsonDiagnostic diagnostic = factory.CreateDiagnostic(
            JsonDiagnosticCode.UnexpectedToken,
            "Unexpected token.",
            null,
            null,
            0,
            expected,
            actual,
            global::System.Array.Empty<byte>(),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.ExpectedTokens, Has.Length.EqualTo(256));
            Assert.That(diagnostic.ExpectedTokens, Is.EqualTo(new string('e', 256)));
            Assert.That(diagnostic.ActualToken, Has.Length.EqualTo(255));
            Assert.That(diagnostic.ActualToken, Is.EqualTo(new string('a', 255)));
        });
    }

    [Test]
    public void FactoryPreservesEveryDefinedDiagnosticCode()
    {
        JsonDiagnosticFactory factory = new JsonDiagnosticFactory(4);

        for (int value = 0; value <= (int)JsonDiagnosticCode.IoFailure; value++)
        {
            JsonDiagnosticCode code = (JsonDiagnosticCode)value;
            JsonDiagnostic diagnostic = factory.CreateDiagnostic(
                code,
                "Diagnostic.",
                null,
                null,
                0,
                null,
                null,
                global::System.Array.Empty<byte>(),
                0);
            Assert.That(diagnostic.Code, Is.EqualTo(code), $"code {value}");
        }
    }

    [Test]
    public void FactoryRejectsInvalidLimitsAndArguments()
    {
        JsonLimitException invalidLimit = Assert.Throws<JsonLimitException>(
            () => new JsonDiagnosticFactory(-1))
            ?? throw new InvalidOperationException("Expected JsonLimitException.");
        Assert.That(invalidLimit.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));

        JsonDiagnosticFactory factory = new JsonDiagnosticFactory(4);
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateDiagnostic(
            (JsonDiagnosticCode)99,
            "Invalid.",
            null,
            null,
            0,
            null,
            null,
            global::System.Array.Empty<byte>(),
            0));
        Assert.Throws<ArgumentNullException>(() => factory.CreateDiagnostic(
            JsonDiagnosticCode.InvalidNumber,
            null!,
            null,
            null,
            0,
            null,
            null,
            global::System.Array.Empty<byte>(),
            0));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateDiagnostic(
            JsonDiagnosticCode.InvalidNumber,
            "Invalid.",
            null,
            null,
            -1,
            null,
            null,
            global::System.Array.Empty<byte>(),
            0));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateDiagnostic(
            JsonDiagnosticCode.InvalidNumber,
            "Invalid.",
            null,
            null,
            0,
            null,
            null,
            global::System.Array.Empty<byte>(),
            -1));
    }

    static JsonDiagnostic CreateWithRange(
        JsonDiagnosticFactory factory,
        byte[] source,
        long sourceByteOffset,
        JsonSourceRange? range)
    {
        return factory.CreateDiagnostic(
            JsonDiagnosticCode.UnexpectedCharacter,
            "Unexpected byte.",
            range,
            "",
            0,
            "JSON value",
            "byte",
            source,
            sourceByteOffset);
    }

    static JsonSourceRange CreateRange(long startOffset, long endOffset)
    {
        JsonSourcePosition start = new JsonSourcePosition();
        start.ByteOffset = startOffset;
        start.Line = 3;
        start.ByteColumn = 4;
        start.ScalarColumn = 4;

        JsonSourcePosition end = new JsonSourcePosition();
        end.ByteOffset = endOffset;
        end.Line = 3;
        end.ByteColumn = 4 + (int)(endOffset - startOffset);
        end.ScalarColumn = end.ByteColumn;

        JsonSourceRange range = new JsonSourceRange();
        range.Start = start;
        range.End = end;
        return range;
    }

    static byte[] CreateSequentialBytes(int length)
    {
        byte[] bytes = new byte[length];
        for (int index = 0; index < length; index++)
        {
            bytes[index] = (byte)index;
        }

        return bytes;
    }
}
