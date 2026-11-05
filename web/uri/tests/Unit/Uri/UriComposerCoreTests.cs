namespace uri.Tests;

public class UriComposerCoreTests
{
    [TestCase("")]
    [TestCase("?")]
    [TestCase("#")]
    [TestCase("?#")]
    [TestCase("HTTP://User%40Name@Example.COM:080/a//b/?x=%23#f%3F")]
    [TestCase("x:///path")]
    [TestCase("x://@:/?")]
    [TestCase("http://192.0.2.1/")]
    [TestCase("x://[2001:0DB8:0000:0000:0000:0000:0000:0001]/")]
    [TestCase("x://[Vf.A:B]:/")]
    [TestCase("urn:example:a%2Fb")]
    [TestCase("//example.com/a")]
    [TestCase("/a//b/")]
    [TestCase("a/b:c?d#e")]
    public void ParsedReferencesRecomposeCharacterForCharacter(string text)
    {
        UriParseResult parsed = UriParserCore.ParseReference(text.AsSpan());

        UriCompositionResult result = UriComposerCore.RecomposeParsedReference(
            parsed.Reference,
            new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, text);
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Text, Is.EqualTo(text), text);
            Assert.That(result.Length, Is.EqualTo(text.Length), text);
            Assert.That(result.Diagnostic, Is.Null, text);
        });
    }

    [Test]
    public void CallerCreatedReferenceComposesAllDelimitersAndIpLiteralBrackets()
    {
        UriReference reference = new UriReference();
        reference.Kind = UriReferenceKind.Uri;
        reference.HasScheme = true;
        reference.Scheme = "x";
        reference.HasAuthority = true;
        reference.Authority.HasUserInfo = true;
        reference.Authority.UserInfo = "u%40ser";
        reference.Authority.HostKind = UriHostKind.IpvFuture;
        reference.Authority.Host = "v1.a:b";
        reference.Authority.HasPort = true;
        reference.Authority.Port = "";
        reference.Path = "/a//b/";
        reference.HasQuery = true;
        reference.Query = "x=%23";
        reference.HasFragment = true;
        reference.Fragment = "";
        reference.OriginalText = "unrelated source text";

        UriCompositionResult result = UriComposerCore.ComposeValidatedReference(
            reference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo("x://u%40ser@[v1.a:b]:/a//b/?x=%23#"));
            Assert.That(result.Length, Is.EqualTo(result.Text.Length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void PresentEmptyAuthorityPartsQueryAndFragmentRemainPresent()
    {
        UriReference reference = new UriReference();
        reference.Kind = UriReferenceKind.Uri;
        reference.HasScheme = true;
        reference.Scheme = "x";
        reference.HasAuthority = true;
        reference.Authority.HasUserInfo = true;
        reference.Authority.HostKind = UriHostKind.RegisteredName;
        reference.Authority.HasPort = true;
        reference.HasQuery = true;
        reference.HasFragment = true;

        UriCompositionResult result = UriComposerCore.ComposeValidatedReference(
            reference,
            new UriValidationOptions());

        Assert.That(result.Text, Is.EqualTo("x://@:?#"));
    }

    [Test]
    public void CompositionUsesCurrentComponentsInsteadOfOriginalText()
    {
        UriReference reference = UriParserCore.ParseReference(
            "x://example/old?before".AsSpan()).Reference;
        reference.Path = "/new%2Fpath";
        reference.Query = "after";

        UriCompositionResult result = UriComposerCore.RecomposeParsedReference(
            reference,
            new OperationLimits());

        Assert.That(result.Text, Is.EqualTo("x://example/new%2Fpath?after"));
    }

    [Test]
    public void CallerCreatedInvalidComponentsFailBeforeWritingOutput()
    {
        UriReference reference = UriParserCore.ParseReference("x:/valid".AsSpan()).Reference;
        reference.Path = "/bad path";

        UriCompositionResult result = UriComposerCore.ComposeValidatedReference(
            reference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidPath));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
        });
    }

    [Test]
    public void CompositionAppliesValidationRequirementsToCallerCreatedData()
    {
        UriReference reference = UriParserCore.ParseReference("relative".AsSpan()).Reference;
        UriValidationOptions options = new UriValidationOptions();
        options.RequireScheme = true;

        UriCompositionResult result = UriComposerCore.ComposeValidatedReference(
            reference,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.MissingRequiredComponent));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Scheme));
        });
    }

    [TestCase("", 0)]
    [TestCase("?#", 2)]
    [TestCase("x://user@[2001:db8::1]:80/a?b#c", 31)]
    [TestCase("//:0", 4)]
    public void MeasurementIncludesEverySerializedDelimiter(string text, int expectedLength)
    {
        UriReference reference = UriParserCore.ParseReference(text.AsSpan()).Reference;

        UriMeasureResult result = UriComposerCore.MeasureValidatedReference(
            reference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Length, Is.EqualTo(expectedLength), text);
            Assert.That(result.Length, Is.EqualTo(text.Length), text);
            Assert.That(result.Diagnostic, Is.Null, text);
        });
    }

    [Test]
    public void ExactOutputLimitSucceedsAndShorterLimitFailsWithoutPartialText()
    {
        const string text = "x://[2001:db8::1]:80/a?b#c";
        UriReference reference = UriParserCore.ParseReference(text.AsSpan()).Reference;
        UriValidationOptions exactOptions = new UriValidationOptions();
        exactOptions.Limits.MaxOutputCharacters = text.Length;
        UriValidationOptions shortOptions = new UriValidationOptions();
        shortOptions.Limits.MaxOutputCharacters = text.Length - 1;

        UriCompositionResult exact = UriComposerCore.ComposeValidatedReference(
            reference,
            exactOptions);
        UriCompositionResult shortResult = UriComposerCore.ComposeValidatedReference(
            reference,
            shortOptions);
        UriMeasureResult shortMeasurement = UriComposerCore.MeasureValidatedReference(
            reference,
            shortOptions);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Text, Is.EqualTo(text));
            Assert.That(shortResult.Success, Is.False);
            Assert.That(shortResult.Text, Is.Empty);
            Assert.That(shortResult.Length, Is.Zero);
            AssertOutputLimit(shortResult.Diagnostic, text.Length - 1, 1);
            Assert.That(shortMeasurement.Success, Is.False);
            Assert.That(shortMeasurement.Length, Is.Zero);
            AssertOutputLimit(shortMeasurement.Diagnostic, text.Length - 1, 1);
        });
    }

    [Test]
    public void NegativeOutputLimitFailsDeterministically()
    {
        UriReference reference = UriParserCore.ParseReference("x:a".AsSpan()).Reference;
        UriValidationOptions options = new UriValidationOptions();
        options.Limits.MaxOutputCharacters = -1;

        UriCompositionResult result = UriComposerCore.ComposeValidatedReference(
            reference,
            options);

        AssertOutputLimit(result.Diagnostic, 0, 0);
    }

    [Test]
    public void LengthAdditionRejectsOverflowAndInvalidInputs()
    {
        UriMeasureResult maximum = UriComposerCore.AddMeasuredLength(
            int.MaxValue - 1,
            1);
        UriMeasureResult overflow = UriComposerCore.AddMeasuredLength(int.MaxValue, 1);
        UriMeasureResult negative = UriComposerCore.AddMeasuredLength(0, -1);

        Assert.Multiple(() =>
        {
            Assert.That(maximum.Success, Is.True);
            Assert.That(maximum.Length, Is.EqualTo(int.MaxValue));
            Assert.That(overflow.Success, Is.False);
            AssertOutputLimit(overflow.Diagnostic, int.MaxValue, 0);
            Assert.That(negative.Success, Is.False);
            Assert.That(negative.Diagnostic, Is.Not.Null);
            Assert.That(negative.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
        });
    }

    [Test]
    public void NullInputsReturnInvalidInputDiagnostics()
    {
        UriReference reference = new UriReference();

        UriCompositionResult nullReference = UriComposerCore.ComposeValidatedReference(
            null,
            new UriValidationOptions());
        UriCompositionResult nullOptions = UriComposerCore.ComposeValidatedReference(
            reference,
            null);
        UriCompositionResult nullLimits = UriComposerCore.RecomposeParsedReference(
            reference,
            null);
        UriMeasureResult nullMeasurement = UriComposerCore.MeasureValidatedReference(
            null,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            AssertInvalidInput(nullReference.Diagnostic);
            AssertInvalidInput(nullOptions.Diagnostic);
            AssertInvalidInput(nullLimits.Diagnostic);
            AssertInvalidInput(nullMeasurement.Diagnostic);
        });
    }

    private static void AssertOutputLimit(
        UriDiagnostic? diagnostic,
        int offset,
        int length)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
        Assert.That(diagnostic.Component, Is.EqualTo(UriComponent.Output));
        Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(diagnostic.Message, Is.EqualTo(LimitGuard.OutputLimitMessage));
    }

    private static void AssertInvalidInput(UriDiagnostic? diagnostic)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
        Assert.That(diagnostic.Component, Is.EqualTo(UriComponent.Input));
    }
}
