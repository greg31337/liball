namespace json.Tests;

public class JsonExceptionTests
{
    [Test]
    public void ParseExceptionPreservesDiagnosticWhenThrown()
    {
        JsonDiagnostic diagnostic = CreateDiagnostic(JsonDiagnosticCode.InvalidNumber, "Invalid JSON number.");
        JsonParseException exception = new JsonParseException(diagnostic);

        JsonParseException caught = Assert.Throws<JsonParseException>(() => throw exception)
            ?? throw new InvalidOperationException("Expected JsonParseException.");

        Assert.Multiple(() =>
        {
            Assert.That(caught, Is.SameAs(exception));
            Assert.That(caught.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(caught.Message, Is.EqualTo(diagnostic.Message));
        });
    }

    [Test]
    public void WriteExceptionPreservesDiagnosticWhenThrown()
    {
        JsonDiagnostic diagnostic = CreateDiagnostic(JsonDiagnosticCode.InvalidWriterState, "Invalid writer state.");
        JsonWriteException exception = new JsonWriteException(diagnostic);

        JsonWriteException caught = Assert.Throws<JsonWriteException>(() => throw exception)
            ?? throw new InvalidOperationException("Expected JsonWriteException.");

        Assert.Multiple(() =>
        {
            Assert.That(caught, Is.SameAs(exception));
            Assert.That(caught.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(caught.Message, Is.EqualTo(diagnostic.Message));
        });
    }

    [Test]
    public void PointerExceptionPreservesDiagnosticWhenThrown()
    {
        JsonDiagnostic diagnostic = CreateDiagnostic(JsonDiagnosticCode.PointerTargetNotFound, "Pointer target not found.");
        JsonPointerException exception = new JsonPointerException(diagnostic);

        JsonPointerException caught = Assert.Throws<JsonPointerException>(() => throw exception)
            ?? throw new InvalidOperationException("Expected JsonPointerException.");

        Assert.Multiple(() =>
        {
            Assert.That(caught, Is.SameAs(exception));
            Assert.That(caught.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(caught.Message, Is.EqualTo(diagnostic.Message));
        });
    }

    [Test]
    public void LimitExceptionPreservesDiagnosticWhenThrown()
    {
        JsonDiagnostic diagnostic = CreateDiagnostic(JsonDiagnosticCode.NestingDepthLimitExceeded, "Maximum depth exceeded.");
        JsonLimitException exception = new JsonLimitException(diagnostic);

        JsonLimitException caught = Assert.Throws<JsonLimitException>(() => throw exception)
            ?? throw new InvalidOperationException("Expected JsonLimitException.");

        Assert.Multiple(() =>
        {
            Assert.That(caught, Is.SameAs(exception));
            Assert.That(caught.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(caught.Message, Is.EqualTo(diagnostic.Message));
        });
    }

    private static JsonDiagnostic CreateDiagnostic(JsonDiagnosticCode code, string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Pointer = "/value";
        return diagnostic;
    }
}
