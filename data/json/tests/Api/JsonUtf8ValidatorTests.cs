namespace json.Tests;

public class JsonUtf8ValidatorTests
{
    [Test]
    public void ValidatorImplementsItsInterfaceAndReportsCompleteConsumption()
    {
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(
            "{\"text\":\"€\",\"array\":[null,true,-1.25e2]}");

        JsonOperationResult result = validator.ValidateUtf8(utf8, new JsonReaderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesConsumed, Is.EqualTo(utf8.Length));
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void InvalidJsonReturnsItsParserDiagnosticWithoutThrowing()
    {
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("{\"a\" 0}");

        JsonOperationResult result = validator.ValidateUtf8(utf8, new JsonReaderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.BytesConsumed, Is.InRange(0, utf8.Length));
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.MissingColon));
            Assert.That(result.Diagnostic?.Message, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public void ConfiguredLimitsReturnLimitDiagnostics()
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumNestingDepth = 1;
        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = limits;
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("[[]]");

        JsonOperationResult result = validator.ValidateUtf8(utf8, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.BytesConsumed, Is.InRange(0, utf8.Length));
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.NestingDepthLimitExceeded));
        });
    }

    [Test]
    public void RecoverableSequenceErrorsStillMakeValidationFail()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.JsonTextSequence;
        options.SequenceErrorPolicy = JsonSequenceErrorPolicy.Resynchronize;
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        byte[] utf8 = new byte[]
        {
            JsonSyntaxBytes.RecordSeparator,
            0x31,
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginObject,
            JsonSyntaxBytes.EndObject,
            JsonSyntaxBytes.LineFeed
        };

        JsonOperationResult result = validator.ValidateUtf8(utf8, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.IncompleteSequenceRecord));
            Assert.That(result.BytesConsumed, Is.EqualTo(3));
        });
    }

    [Test]
    public void ValidatorCanBeReusedAfterFailuresAndDoesNotMutateInput()
    {
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        byte[] invalid = System.Text.Encoding.ASCII.GetBytes("01");
        byte[] original = invalid.ToArray();

        JsonOperationResult invalidResult = validator.ValidateUtf8(invalid, new JsonReaderOptions());
        JsonOperationResult validResult = validator.ValidateUtf8(
            System.Text.Encoding.ASCII.GetBytes("null"),
            new JsonReaderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(invalidResult.Success, Is.False);
            Assert.That(invalidResult.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.InvalidNumber));
            Assert.That(invalid, Is.EqualTo(original));
            Assert.That(validResult.Success, Is.True);
            Assert.That(validResult.BytesConsumed, Is.EqualTo(4));
            Assert.That(validResult.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void InvalidOptionsReturnAnOperationDiagnostic()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.TrackUnicodeScalarColumn = true;
        IJsonUtf8Validator validator = new JsonUtf8Validator();

        JsonOperationResult result = validator.ValidateUtf8(
            System.Text.Encoding.ASCII.GetBytes("null"),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.BytesConsumed, Is.Zero);
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
        });
    }
}
