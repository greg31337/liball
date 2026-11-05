namespace json.Tests;

public class JsonSequenceFramerTests
{
    [Test]
    public void SingleValueModeAllowsSurroundingWhitespaceAndRejectsTrailingContent()
    {
        JsonSequenceFramer framer = new JsonSequenceFramer(
            JsonInputMode.SingleValue,
            JsonSequenceErrorPolicy.Stop);

        Assert.That(framer.ProcessByte(JsonSyntaxBytes.Space), Is.EqualTo(JsonFramingAction.SkipByte));
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.BeginObject), Is.EqualTo(JsonFramingAction.StartDocument));
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.EndObject), Is.EqualTo(JsonFramingAction.ContinueDocument));
        Assert.That(framer.CompleteDocument(true), Is.True);
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.LineFeed), Is.EqualTo(JsonFramingAction.SkipByte));

        Assert.Multiple(() =>
        {
            Assert.That(framer.GetCompletedDocumentCount(), Is.EqualTo(1));
            Assert.That(framer.CompleteInput(), Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(framer.CompleteInput(), Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.Throws<InvalidOperationException>(() => framer.ProcessByte(JsonSyntaxBytes.Space));
        });

        JsonSequenceFramer trailing = new JsonSequenceFramer(
            JsonInputMode.SingleValue,
            JsonSequenceErrorPolicy.Stop);
        trailing.ProcessByte(0x6E);
        trailing.CompleteDocument(false);
        AssertFailure(() => trailing.ProcessByte(0x66), JsonDiagnosticCode.TrailingContent);
    }

    [Test]
    public void SingleValueModeRequiresOneCompleteDocument()
    {
        JsonSequenceFramer empty = new JsonSequenceFramer(
            JsonInputMode.SingleValue,
            JsonSequenceErrorPolicy.Stop);
        empty.ProcessByte(JsonSyntaxBytes.Space);
        AssertFailure(() => empty.CompleteInput(), JsonDiagnosticCode.UnexpectedEndOfInput);

        JsonSequenceFramer incomplete = new JsonSequenceFramer(
            JsonInputMode.SingleValue,
            JsonSequenceErrorPolicy.Stop);
        incomplete.ProcessByte(JsonSyntaxBytes.BeginArray);
        AssertFailure(() => incomplete.CompleteInput(), JsonDiagnosticCode.UnexpectedEndOfInput);
    }

    [Test]
    public void WhitespaceSeparatedModeRequiresWhitespaceBetweenValues()
    {
        JsonSequenceFramer framer = new JsonSequenceFramer(
            JsonInputMode.WhitespaceSeparated,
            JsonSequenceErrorPolicy.Stop);

        Assert.That(framer.ProcessByte(JsonSyntaxBytes.HorizontalTab), Is.EqualTo(JsonFramingAction.SkipByte));
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.BeginArray), Is.EqualTo(JsonFramingAction.StartDocument));
        Assert.That(framer.CompleteDocument(true), Is.True);
        AssertFailure(() => framer.ProcessByte(JsonSyntaxBytes.BeginObject), JsonDiagnosticCode.UnexpectedToken);

        Assert.That(framer.ProcessByte(JsonSyntaxBytes.Space), Is.EqualTo(JsonFramingAction.SkipByte));
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.BeginObject), Is.EqualTo(JsonFramingAction.StartDocument));
        Assert.That(framer.CompleteDocument(true), Is.True);
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.CarriageReturn), Is.EqualTo(JsonFramingAction.SkipByte));

        Assert.Multiple(() =>
        {
            Assert.That(framer.GetCompletedDocumentCount(), Is.EqualTo(2));
            Assert.That(framer.CompleteInput(), Is.EqualTo(JsonReadStatus.StreamComplete));
        });

        JsonSequenceFramer empty = new JsonSequenceFramer(
            JsonInputMode.WhitespaceSeparated,
            JsonSequenceErrorPolicy.Stop);
        empty.ProcessByte(JsonSyntaxBytes.LineFeed);
        Assert.That(empty.CompleteInput(), Is.EqualTo(JsonReadStatus.StreamComplete));
    }

    [Test]
    public void JsonTextSequenceFramesSelfDelimitedAndScalarValues()
    {
        JsonSequenceFramer framer = new JsonSequenceFramer(
            JsonInputMode.JsonTextSequence,
            JsonSequenceErrorPolicy.Stop);

        Assert.That(framer.ProcessByte(JsonSyntaxBytes.RecordSeparator), Is.EqualTo(JsonFramingAction.SkipByte));
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.RecordSeparator), Is.EqualTo(JsonFramingAction.SkipByte));
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.BeginObject), Is.EqualTo(JsonFramingAction.StartDocument));
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.EndObject), Is.EqualTo(JsonFramingAction.ContinueDocument));
        Assert.That(framer.CompleteDocument(true), Is.True);

        Assert.That(framer.ProcessByte(JsonSyntaxBytes.RecordSeparator), Is.EqualTo(JsonFramingAction.SkipByte));
        Assert.That(framer.ProcessByte(0x31), Is.EqualTo(JsonFramingAction.StartDocument));
        Assert.That(framer.CompleteDocument(false), Is.False);
        Assert.That(framer.GetFramingState(), Is.EqualTo(JsonFramingState.AwaitSequenceTerminator));
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.LineFeed), Is.EqualTo(JsonFramingAction.CompleteDocument));

        Assert.That(framer.ProcessByte(JsonSyntaxBytes.Space), Is.EqualTo(JsonFramingAction.SkipByte));
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.RecordSeparator), Is.EqualTo(JsonFramingAction.SkipByte));
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.StringDelimiter), Is.EqualTo(JsonFramingAction.StartDocument));
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.StringDelimiter), Is.EqualTo(JsonFramingAction.ContinueDocument));
        Assert.That(framer.CompleteDocument(true), Is.True);
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.LineFeed), Is.EqualTo(JsonFramingAction.SkipByte));

        Assert.Multiple(() =>
        {
            Assert.That(framer.GetCompletedDocumentCount(), Is.EqualTo(3));
            Assert.That(framer.CompleteInput(), Is.EqualTo(JsonReadStatus.StreamComplete));
        });
    }

    [Test]
    public void JsonTextSequenceRequiresRecordSeparatorsAndDiagnosesEmptyRecords()
    {
        JsonSequenceFramer missing = new JsonSequenceFramer(
            JsonInputMode.JsonTextSequence,
            JsonSequenceErrorPolicy.Stop);
        AssertFailure(() => missing.ProcessByte(JsonSyntaxBytes.Space), JsonDiagnosticCode.MissingRecordSeparator);

        JsonSequenceFramer consecutive = new JsonSequenceFramer(
            JsonInputMode.JsonTextSequence,
            JsonSequenceErrorPolicy.Stop);
        consecutive.ProcessByte(JsonSyntaxBytes.RecordSeparator);
        consecutive.ProcessByte(JsonSyntaxBytes.RecordSeparator);
        Assert.That(consecutive.CompleteInput(), Is.EqualTo(JsonReadStatus.StreamComplete));

        JsonSequenceFramer whitespaceOnly = new JsonSequenceFramer(
            JsonInputMode.JsonTextSequence,
            JsonSequenceErrorPolicy.Stop);
        whitespaceOnly.ProcessByte(JsonSyntaxBytes.RecordSeparator);
        whitespaceOnly.ProcessByte(JsonSyntaxBytes.Space);
        AssertFailure(
            () => whitespaceOnly.ProcessByte(JsonSyntaxBytes.RecordSeparator),
            JsonDiagnosticCode.EmptySequenceRecord);

        JsonSequenceFramer finalWhitespaceOnly = new JsonSequenceFramer(
            JsonInputMode.JsonTextSequence,
            JsonSequenceErrorPolicy.Stop);
        finalWhitespaceOnly.ProcessByte(JsonSyntaxBytes.RecordSeparator);
        finalWhitespaceOnly.ProcessByte(JsonSyntaxBytes.LineFeed);
        AssertFailure(() => finalWhitespaceOnly.CompleteInput(), JsonDiagnosticCode.EmptySequenceRecord);
    }

    [Test]
    public void JsonTextSequenceDetectsIncompleteRecordsAndScalarCanaries()
    {
        JsonSequenceFramer interruptedContainer = new JsonSequenceFramer(
            JsonInputMode.JsonTextSequence,
            JsonSequenceErrorPolicy.Stop);
        interruptedContainer.ProcessByte(JsonSyntaxBytes.RecordSeparator);
        interruptedContainer.ProcessByte(JsonSyntaxBytes.BeginArray);
        AssertFailure(
            () => interruptedContainer.ProcessByte(JsonSyntaxBytes.RecordSeparator),
            JsonDiagnosticCode.IncompleteSequenceRecord);

        JsonSequenceFramer unterminatedScalar = new JsonSequenceFramer(
            JsonInputMode.JsonTextSequence,
            JsonSequenceErrorPolicy.Stop);
        unterminatedScalar.ProcessByte(JsonSyntaxBytes.RecordSeparator);
        unterminatedScalar.ProcessByte(0x31);
        unterminatedScalar.CompleteDocument(false);
        AssertFailure(
            () => unterminatedScalar.ProcessByte(JsonSyntaxBytes.RecordSeparator),
            JsonDiagnosticCode.IncompleteSequenceRecord);

        JsonSequenceFramer finalScalar = new JsonSequenceFramer(
            JsonInputMode.JsonTextSequence,
            JsonSequenceErrorPolicy.Stop);
        finalScalar.ProcessByte(JsonSyntaxBytes.RecordSeparator);
        finalScalar.ProcessByte(0x74);
        finalScalar.CompleteDocument(false);
        AssertFailure(() => finalScalar.CompleteInput(), JsonDiagnosticCode.IncompleteSequenceRecord);
    }

    [Test]
    public void ResynchronizeReportsFramingErrorsAndUsesTheSameRecordSeparatorForRecovery()
    {
        JsonSequenceFramer framer = new JsonSequenceFramer(
            JsonInputMode.JsonTextSequence,
            JsonSequenceErrorPolicy.Resynchronize);

        framer.ProcessByte(JsonSyntaxBytes.RecordSeparator);
        framer.ProcessByte(0x31);
        framer.CompleteDocument(false);
        Assert.That(
            framer.ProcessByte(JsonSyntaxBytes.RecordSeparator),
            Is.EqualTo(JsonFramingAction.RecoverableError));
        Assert.That(
            framer.GetCurrentDiagnostic()?.Code,
            Is.EqualTo(JsonDiagnosticCode.IncompleteSequenceRecord));

        Assert.That(framer.ProcessByte(JsonSyntaxBytes.BeginObject), Is.EqualTo(JsonFramingAction.StartDocument));
        framer.ProcessByte(JsonSyntaxBytes.EndObject);
        framer.CompleteDocument(true);
        Assert.That(framer.ProcessByte(0x78), Is.EqualTo(JsonFramingAction.RecoverableError));
        Assert.That(framer.GetCurrentDiagnostic()?.Code, Is.EqualTo(JsonDiagnosticCode.UnexpectedToken));
        Assert.That(framer.ProcessByte(0x79), Is.EqualTo(JsonFramingAction.SkipByte));
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.RecordSeparator), Is.EqualTo(JsonFramingAction.SkipByte));
        Assert.That(framer.ProcessByte(JsonSyntaxBytes.BeginArray), Is.EqualTo(JsonFramingAction.StartDocument));
        framer.ProcessByte(JsonSyntaxBytes.EndArray);
        framer.CompleteDocument(true);

        Assert.Multiple(() =>
        {
            Assert.That(framer.GetCompletedDocumentCount(), Is.EqualTo(2));
            Assert.That(framer.CompleteInput(), Is.EqualTo(JsonReadStatus.StreamComplete));
        });
    }

    [Test]
    public void ResynchronizeCanContinueAfterParserAndFinalRecordErrors()
    {
        JsonSequenceFramer parserRecovery = new JsonSequenceFramer(
            JsonInputMode.JsonTextSequence,
            JsonSequenceErrorPolicy.Resynchronize);
        parserRecovery.ProcessByte(JsonSyntaxBytes.RecordSeparator);
        parserRecovery.ProcessByte(JsonSyntaxBytes.BeginObject);
        JsonDiagnostic parserDiagnostic = new JsonDiagnostic();
        parserDiagnostic.Code = JsonDiagnosticCode.UnexpectedToken;
        parserDiagnostic.Message = "Malformed object.";

        Assert.That(parserRecovery.HandleDocumentError(parserDiagnostic), Is.True);
        Assert.That(parserRecovery.GetCurrentDiagnostic(), Is.SameAs(parserDiagnostic));
        Assert.That(parserRecovery.ProcessByte(0x78), Is.EqualTo(JsonFramingAction.SkipByte));
        Assert.That(parserRecovery.ProcessByte(JsonSyntaxBytes.RecordSeparator), Is.EqualTo(JsonFramingAction.SkipByte));
        Assert.That(parserRecovery.ProcessByte(JsonSyntaxBytes.BeginArray), Is.EqualTo(JsonFramingAction.StartDocument));

        JsonSequenceFramer finalRecovery = new JsonSequenceFramer(
            JsonInputMode.JsonTextSequence,
            JsonSequenceErrorPolicy.Resynchronize);
        finalRecovery.ProcessByte(JsonSyntaxBytes.RecordSeparator);
        finalRecovery.ProcessByte(0x31);
        finalRecovery.CompleteDocument(false);

        Assert.Multiple(() =>
        {
            Assert.That(finalRecovery.CompleteInput(), Is.EqualTo(JsonReadStatus.Error));
            Assert.That(
                finalRecovery.GetCurrentDiagnostic()?.Code,
                Is.EqualTo(JsonDiagnosticCode.IncompleteSequenceRecord));
            Assert.That(finalRecovery.CompleteInput(), Is.EqualTo(JsonReadStatus.StreamComplete));
        });
    }

    [Test]
    public void ResetAndOptionValidationPreserveConfigurationBoundaries()
    {
        JsonSequenceFramer framer = new JsonSequenceFramer(
            JsonInputMode.JsonTextSequence,
            JsonSequenceErrorPolicy.Resynchronize);
        framer.ProcessByte(JsonSyntaxBytes.RecordSeparator);
        framer.ProcessByte(JsonSyntaxBytes.BeginObject);
        framer.CompleteDocument(true);

        framer.ResetFramer();

        Assert.Multiple(() =>
        {
            Assert.That(framer.GetInputMode(), Is.EqualTo(JsonInputMode.JsonTextSequence));
            Assert.That(framer.GetErrorPolicy(), Is.EqualTo(JsonSequenceErrorPolicy.Resynchronize));
            Assert.That(framer.GetFramingState(), Is.EqualTo(JsonFramingState.AwaitRecordSeparator));
            Assert.That(framer.GetCompletedDocumentCount(), Is.Zero);
            Assert.That(framer.GetCurrentDiagnostic(), Is.Null);
            Assert.That(framer.IsDocumentActive(), Is.False);
        });

        AssertFailure(
            () => new JsonSequenceFramer(JsonInputMode.SingleValue, JsonSequenceErrorPolicy.Resynchronize),
            JsonDiagnosticCode.InvalidOptions);
        AssertFailure(
            () => new JsonSequenceFramer((JsonInputMode)99, JsonSequenceErrorPolicy.Stop),
            JsonDiagnosticCode.InvalidOptions);
        AssertFailure(
            () => new JsonSequenceFramer(JsonInputMode.JsonTextSequence, (JsonSequenceErrorPolicy)99),
            JsonDiagnosticCode.InvalidOptions);
    }

    static void AssertFailure(TestDelegate action, JsonDiagnosticCode expectedCode)
    {
        JsonParseException exception = Assert.Throws<JsonParseException>(action)
            ?? throw new InvalidOperationException("Expected JsonParseException.");
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(expectedCode));
    }
}
