namespace uri.Tests;

public class PublicUriApiTests
{
    [Test]
    public void ParserImplementsThrowingReferencePath()
    {
        IUriParser parser = new UriParser();

        UriReference reference = parser.ParseReference(
            "HTTP://User@Example.COM:080/a?b#c",
            new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.Kind, Is.EqualTo(UriReferenceKind.Uri));
            Assert.That(reference.Scheme, Is.EqualTo("HTTP"));
            Assert.That(reference.Authority.UserInfo, Is.EqualTo("User"));
            Assert.That(reference.Authority.Host, Is.EqualTo("Example.COM"));
            Assert.That(reference.Authority.Port, Is.EqualTo("080"));
            Assert.That(reference.Path, Is.EqualTo("/a"));
            Assert.That(reference.Query, Is.EqualTo("b"));
            Assert.That(reference.Fragment, Is.EqualTo("c"));
            Assert.That(reference.OriginalText, Is.EqualTo("HTTP://User@Example.COM:080/a?b#c"));
        });
    }

    [Test]
    public void ParserThrowingReferencePathPreservesFailureDiagnostic()
    {
        IUriParser parser = new UriParser();

        UriSyntaxException exception = Assert.Throws<UriSyntaxException>(() =>
            parser.ParseReference("x:path%GG", new UriParseOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPercentEncoding));
            Assert.That(exception.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(exception.Diagnostic.Range.Offset, Is.EqualTo(7));
            Assert.That(exception.Message, Is.EqualTo(exception.Diagnostic.Message));
        });
    }

    [Test]
    public void ParserReferenceResultAndSpanPathsReturnEquivalentModels()
    {
        IUriParser parser = new UriParser();
        UriParseOptions options = new UriParseOptions();

        UriParseResult text = parser.TryParseReference("//host/a?#", options);
        UriParseResult span = parser.TryParseReferenceSpan("//host/a?#".AsSpan(), options);

        Assert.Multiple(() =>
        {
            Assert.That(text.Success, Is.True);
            Assert.That(span.Success, Is.True);
            Assert.That(text.Reference.OriginalText, Is.EqualTo(span.Reference.OriginalText));
            Assert.That(span.Reference.Kind, Is.EqualTo(UriReferenceKind.NetworkPathReference));
            Assert.That(span.Reference.HasQuery, Is.True);
            Assert.That(span.Reference.HasFragment, Is.True);
        });
    }

    [Test]
    public void ParserResultPathAppliesInputLimitBeforeSyntaxParsing()
    {
        IUriParser parser = new UriParser();
        UriParseOptions options = new UriParseOptions();
        options.Limits.MaxInputCharacters = 4;

        UriParseResult result = parser.TryParseReference("x:path%GG", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Reference.OriginalText, Is.Empty);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Input));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(4));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(5));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(LimitGuard.InputLimitMessage));
        });
    }

    [Test]
    public void ParserResultPathRejectsNullInputsAndOptionsWithoutThrowing()
    {
        IUriParser parser = new UriParser();
        UriParseOptions noLimits = new UriParseOptions();
        noLimits.Limits = null!;

        UriParseResult nullText = parser.TryParseReference(null!, new UriParseOptions());
        UriParseResult nullOptions = parser.TryParseReference("x:a", null!);
        UriParseResult nullLimits = parser.TryParseReference("x:a", noLimits);

        Assert.Multiple(() =>
        {
            AssertInvalidInput(nullText.Diagnostic);
            AssertInvalidInput(nullOptions.Diagnostic);
            AssertInvalidInput(nullLimits.Diagnostic);
        });
    }

    [Test]
    public void ParserAbsoluteMethodsRequireAbsoluteUriAndForbidFragments()
    {
        IUriParser parser = new UriParser();
        UriParseOptions options = new UriParseOptions();

        UriReference reference = parser.ParseAbsoluteUri("x:path?query", options);
        UriParseResult missingScheme = parser.TryParseAbsoluteUri("/path", options);
        UriParseResult fragment = parser.TryParseAbsoluteUri("x:path#fragment", options);

        Assert.Multiple(() =>
        {
            Assert.That(reference.Kind, Is.EqualTo(UriReferenceKind.AbsoluteUri));
            Assert.That(reference.Query, Is.EqualTo("query"));
            Assert.That(missingScheme.Success, Is.False);
            Assert.That(missingScheme.Diagnostic, Is.Not.Null);
            Assert.That(missingScheme.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidScheme));
            Assert.That(fragment.Success, Is.False);
            Assert.That(fragment.Diagnostic, Is.Not.Null);
            Assert.That(fragment.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidFragment));
        });

        Assert.Throws<UriSyntaxException>(() =>
            parser.ParseAbsoluteUri("x:path#fragment", options));
    }

    [Test]
    public void ParserRelativeMethodAcceptsRelativeFormsAndRejectsSchemes()
    {
        IUriParser parser = new UriParser();

        UriParseResult relative = parser.TryParseRelativeReference(
            "a/b:c?d#e",
            new UriParseOptions());
        UriParseResult uri = parser.TryParseRelativeReference(
            "x:path",
            new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(relative.Success, Is.True);
            Assert.That(relative.Reference.Kind, Is.EqualTo(UriReferenceKind.RelativePathReference));
            Assert.That(uri.Success, Is.False);
            Assert.That(uri.Diagnostic, Is.Not.Null);
            Assert.That(uri.Diagnostic!.Component, Is.EqualTo(UriComponent.Path));
        });
    }

    [Test]
    public void ValidatorReferencePathReturnsParsedValueOrSyntaxFailure()
    {
        IUriValidator validator = new UriValidator();

        UriValidationResult valid = validator.ValidateReference(
            "x://host/path",
            new UriValidationOptions());
        UriValidationResult invalid = validator.ValidateReference(
            "x://host/bad path",
            new UriValidationOptions());
        UriValidationResult nullText = validator.ValidateReference(
            null!,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Reference.OriginalText, Is.EqualTo("x://host/path"));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Diagnostic, Is.Not.Null);
            Assert.That(invalid.Diagnostic!.Component, Is.EqualTo(UriComponent.Path));
            AssertInvalidInput(nullText.Diagnostic);
        });
    }

    [Test]
    public void ValidatorComponentPathReturnsTheCallerModelAndAppliesRequirements()
    {
        IUriValidator validator = new UriValidator();
        UriReference reference = new UriParser().ParseReference(
            "//host/path",
            new UriParseOptions());
        UriValidationOptions validOptions = new UriValidationOptions();
        validOptions.RequireHost = true;
        UriValidationOptions invalidOptions = new UriValidationOptions();
        invalidOptions.RequireScheme = true;

        UriValidationResult valid = validator.ValidateComponents(reference, validOptions);
        UriValidationResult invalid = validator.ValidateComponents(reference, invalidOptions);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Reference, Is.SameAs(reference));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Reference, Is.SameAs(reference));
            Assert.That(invalid.Diagnostic, Is.Not.Null);
            Assert.That(invalid.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.MissingRequiredComponent));
            Assert.That(invalid.Diagnostic.Component, Is.EqualTo(UriComponent.Scheme));
        });
    }

    [Test]
    public void ValidatorAllDiagnosticsReturnsDeterministicComponentOrder()
    {
        IUriValidator validator = new UriValidator();
        UriReference reference = new UriReference();
        reference.Kind = UriReferenceKind.RelativePathReference;
        reference.Scheme = "unexpected";
        reference.Path = "bad path";
        reference.HasQuery = true;
        reference.Query = "%GG";

        UriDiagnostic[] diagnostics = validator.ValidateAllDiagnostics(
            reference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Has.Length.EqualTo(3));
            Assert.That(diagnostics[0].Component, Is.EqualTo(UriComponent.Scheme));
            Assert.That(diagnostics[1].Component, Is.EqualTo(UriComponent.Path));
            Assert.That(diagnostics[2].Component, Is.EqualTo(UriComponent.Query));
        });
    }

    [Test]
    public void ComposerImplementsThrowingCompositionAndExactMeasurement()
    {
        IUriParser parser = new UriParser();
        IUriComposer composer = new UriComposer();
        UriReference reference = parser.ParseReference(
            "x://user@[2001:0DB8::1]:080/a//b/?q=%23#f",
            new UriParseOptions());
        reference.OriginalText = "not used";

        string text = composer.ComposeReference(reference, new UriValidationOptions());
        int length = composer.MeasureReference(reference, new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("x://user@[2001:0DB8::1]:080/a//b/?q=%23#f"));
            Assert.That(length, Is.EqualTo(text.Length));
        });
    }

    [Test]
    public void ComposerResultPathReportsSuccessAndFailureWithoutPartialText()
    {
        IUriComposer composer = new UriComposer();
        UriReference valid = new UriParser().ParseReference(
            "x:path?#",
            new UriParseOptions());
        UriReference invalid = new UriParser().ParseReference(
            "x:path",
            new UriParseOptions());
        invalid.Path = "bad path";

        UriTextResult success = composer.TryComposeReference(
            valid,
            new UriValidationOptions());
        UriTextResult failure = composer.TryComposeReference(
            invalid,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(success.Success, Is.True);
            Assert.That(success.Text, Is.EqualTo("x:path?#"));
            Assert.That(success.Length, Is.EqualTo(8));
            Assert.That(success.Diagnostic, Is.Null);
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Text, Is.Empty);
            Assert.That(failure.Length, Is.Zero);
            Assert.That(failure.Diagnostic, Is.Not.Null);
            Assert.That(failure.Diagnostic!.Component, Is.EqualTo(UriComponent.Path));
        });
    }

    [Test]
    public void ComposerThrowingPathsPreserveValidationAndLimitDiagnostics()
    {
        IUriComposer composer = new UriComposer();
        UriReference invalid = new UriParser().ParseReference(
            "x:path",
            new UriParseOptions());
        invalid.Path = "bad path";
        UriReference limited = new UriParser().ParseReference(
            "x:abcdef",
            new UriParseOptions());
        UriValidationOptions options = new UriValidationOptions();
        options.Limits.MaxOutputCharacters = 4;

        UriValidationException composition = Assert.Throws<UriValidationException>(() =>
            composer.ComposeReference(invalid, new UriValidationOptions()))!;
        UriValidationException measurement = Assert.Throws<UriValidationException>(() =>
            composer.MeasureReference(limited, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(composition.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(measurement.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(measurement.Diagnostic.Component, Is.EqualTo(UriComponent.Output));
            Assert.That(measurement.Diagnostic.Range.Offset, Is.EqualTo(4));
            Assert.That(measurement.Diagnostic.Range.Length, Is.EqualTo(4));
        });
    }

    [Test]
    public void ComposerResultPathRejectsNullValuesWithoutThrowing()
    {
        IUriComposer composer = new UriComposer();
        UriReference reference = new UriReference();

        UriTextResult nullReference = composer.TryComposeReference(
            null!,
            new UriValidationOptions());
        UriTextResult nullOptions = composer.TryComposeReference(reference, null!);

        Assert.Multiple(() =>
        {
            Assert.That(nullReference.Success, Is.False);
            AssertInvalidInput(nullReference.Diagnostic);
            Assert.That(nullOptions.Success, Is.False);
            AssertInvalidInput(nullOptions.Diagnostic);
        });
    }

    [Test]
    public void UriTextResultRetainsAllOutcomeFields()
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);

        UriTextResult result = new UriTextResult(false, "partial", 7, diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.EqualTo("partial"));
            Assert.That(result.Length, Is.EqualTo(7));
            Assert.That(result.Diagnostic, Is.SameAs(diagnostic));
        });
    }

    private static void AssertInvalidInput(UriDiagnostic? diagnostic)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
        Assert.That(diagnostic.Component, Is.EqualTo(UriComponent.Input));
    }
}
