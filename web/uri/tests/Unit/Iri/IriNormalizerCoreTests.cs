namespace uri.Tests;

public class IriNormalizerCoreTests
{
    [Test]
    public void NormalizeModeAppliesNfcToEveryInternationalizedComponent()
    {
        string input = "x://use\u0301r@e\u0301xample.test:443/" +
            "cafe\u0301/re\u0301sume\u0301?q=e\u0301#f=e\u0301";
        string expected = "x://usér@éxample.test:443/café/résumé?q=é#f=é";

        UriTextResult result = IriNormalizerCore.NormalizeIri(
            input.AsSpan(),
            UnicodeNormalizationMode.NormalizeNfc,
            new OperationLimits());

        AssertSuccess(result, expected);
    }

    [Test]
    public void NormalizationDoesNotDecodePercentTripletsOrCrossTheirBoundaries()
    {
        string input = "x:/e%CC%81/e\u0301?e%CC%81#e%CC%81";

        UriTextResult result = IriNormalizerCore.NormalizeIri(
            input.AsSpan(),
            UnicodeNormalizationMode.NormalizeNfc,
            new OperationLimits());

        AssertSuccess(result, "x:/e%CC%81/é?e%CC%81#e%CC%81");
    }

    [Test]
    public void NormalizeModePerformsCanonicalCompositionAndOrdering()
    {
        UriTextResult result = IriNormalizerCore.NormalizeIri(
            "x:a\u0315\u0300".AsSpan(),
            UnicodeNormalizationMode.NormalizeNfc,
            new OperationLimits());

        AssertSuccess(result, "x:à\u0315");
    }

    [Test]
    public void NormalizeModeRejectsCanonicalOutputThatIsNotLegalIriText()
    {
        UriTextResult result = IriNormalizerCore.NormalizeIri(
            "x:/\u1FEF".AsSpan(),
            UnicodeNormalizationMode.NormalizeNfc,
            new OperationLimits());

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidPath,
            UriComponent.Path,
            3,
            1);
    }

    [Test]
    public void SupplementaryAndPrivateScalarsRemainUnchanged()
    {
        string input = "x:/𐀀?\uE000𐀀#𐀀";

        UriTextResult result = IriNormalizerCore.NormalizeIri(
            input.AsSpan(),
            UnicodeNormalizationMode.NormalizeNfc,
            new OperationLimits());

        AssertSuccess(result, input);
    }

    [Test]
    public void PreserveModeReturnsDecomposedTextExactlyAsSupplied()
    {
        string input = "x://e\u0301.test/cafe\u0301?q=e\u0301#e\u0301";

        UriTextResult result = IriNormalizerCore.NormalizeIri(
            input.AsSpan(),
            UnicodeNormalizationMode.Preserve,
            new OperationLimits());

        AssertSuccess(result, input);
    }

    [TestCase("x://user@host/path?query#fragment")]
    [TestCase("x://usér@éxample.test/café?résumé#é")]
    [TestCase("?#")]
    [TestCase("")]
    public void DiagnoseModeReturnsNormalizedInputWithoutChangingIt(string input)
    {
        UriTextResult result = IriNormalizerCore.NormalizeIri(
            input.AsSpan(),
            UnicodeNormalizationMode.DiagnoseNfc,
            new OperationLimits());

        AssertSuccess(result, input);
    }

    [TestCase("x://e\u0301@host", UriComponent.UserInfo, 4, 2)]
    [TestCase("x://e\u0301.test", UriComponent.Host, 4, 7)]
    [TestCase("x:/e\u0301", UriComponent.Path, 2, 3)]
    [TestCase("x:?e\u0301", UriComponent.Query, 3, 2)]
    [TestCase("x:#e\u0301", UriComponent.Fragment, 3, 2)]
    public void DiagnoseModeReportsTheFirstNonNfcComponentAndSourceRange(
        string input,
        UriComponent component,
        int offset,
        int length)
    {
        UriTextResult result = IriNormalizerCore.NormalizeIri(
            input.AsSpan(),
            UnicodeNormalizationMode.DiagnoseNfc,
            new OperationLimits());

        AssertFailure(
            result,
            UriDiagnosticCode.NormalizationRequired,
            component,
            offset,
            length);
    }

    [Test]
    public void DiagnoseModeStopsAtTheFirstComponentNeedingNormalization()
    {
        UriTextResult result = IriNormalizerCore.NormalizeIri(
            "x://e\u0301@e\u0301.test/e\u0301?e\u0301#e\u0301".AsSpan(),
            UnicodeNormalizationMode.DiagnoseNfc,
            new OperationLimits());

        AssertFailure(
            result,
            UriDiagnosticCode.NormalizationRequired,
            UriComponent.UserInfo,
            4,
            2);
    }

    [Test]
    public void ParsingNeverSilentlyAppliesRequestedProducerNormalization()
    {
        string input = "x:/cafe\u0301";
        IriParseResult before = IriParserCore.ParseReference(input.AsSpan());
        UriTextResult normalized = IriNormalizerCore.NormalizeIri(
            input.AsSpan(),
            UnicodeNormalizationMode.NormalizeNfc,
            new OperationLimits());
        IriParseResult after = IriParserCore.ParseReference(input.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(before.Success, Is.True);
            Assert.That(before.Reference.Path, Is.EqualTo("/cafe\u0301"));
            AssertSuccess(normalized, "x:/café");
            Assert.That(after.Success, Is.True);
            Assert.That(after.Reference.Path, Is.EqualTo("/cafe\u0301"));
            Assert.That(after.Reference.OriginalText, Is.EqualTo(input));
        });
    }

    [Test]
    public void NormalizeModeIsIdempotentAndProducesAValidIriReference()
    {
        OperationLimits limits = new OperationLimits();
        UriTextResult first = IriNormalizerCore.NormalizeIri(
            "x://e\u0301.test/cafe\u0301?q=e\u0301#e\u0301".AsSpan(),
            UnicodeNormalizationMode.NormalizeNfc,
            limits);
        UriTextResult second = IriNormalizerCore.NormalizeIri(
            first.Text.AsSpan(),
            UnicodeNormalizationMode.NormalizeNfc,
            limits);
        IriParseResult parsed = IriParserCore.ParseReference(second.Text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            AssertSuccess(second, first.Text);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Reference.OriginalText, Is.EqualTo(first.Text));
        });
    }

    [Test]
    public void SpanNormalizationConsumesOnlyTheProvidedSlice()
    {
        string input = "x:/cafe\u0301";
        string padded = "before" + input + "after";

        UriTextResult result = IriNormalizerCore.NormalizeIri(
            padded.AsSpan(6, input.Length),
            UnicodeNormalizationMode.NormalizeNfc,
            new OperationLimits());

        AssertSuccess(result, "x:/café");
    }

    [Test]
    public void InputAndNormalizedOutputLimitsAreEnforced()
    {
        OperationLimits inputLimits = new OperationLimits();
        inputLimits.MaxInputCharacters = 3;
        OperationLimits exactOutput = new OperationLimits();
        exactOutput.MaxOutputCharacters = 3;
        OperationLimits shortOutput = new OperationLimits();
        shortOutput.MaxOutputCharacters = 2;

        UriTextResult input = IriNormalizerCore.NormalizeIri(
            "x:e\u0301".AsSpan(),
            UnicodeNormalizationMode.NormalizeNfc,
            inputLimits);
        UriTextResult exact = IriNormalizerCore.NormalizeIri(
            "x:e\u0301".AsSpan(),
            UnicodeNormalizationMode.NormalizeNfc,
            exactOutput);
        UriTextResult output = IriNormalizerCore.NormalizeIri(
            "x:e\u0301".AsSpan(),
            UnicodeNormalizationMode.NormalizeNfc,
            shortOutput);

        Assert.Multiple(() =>
        {
            AssertFailure(input, UriDiagnosticCode.LimitExceeded, UriComponent.Input, 3, 1);
            AssertSuccess(exact, "x:é");
            AssertFailure(output, UriDiagnosticCode.LimitExceeded, UriComponent.Output, 2, 1);
        });
    }

    [Test]
    public void InvalidInputModeAndLimitsReturnIriDiagnostics()
    {
        string malformed = "x:" + '\uD800';
        OperationLimits negativeOutput = new OperationLimits();
        negativeOutput.MaxOutputCharacters = -1;

        UriTextResult invalidIri = IriNormalizerCore.NormalizeIri(
            malformed.AsSpan(),
            UnicodeNormalizationMode.NormalizeNfc,
            new OperationLimits());
        UriTextResult invalidMode = IriNormalizerCore.NormalizeIri(
            "x:é".AsSpan(),
            (UnicodeNormalizationMode)999,
            new OperationLimits());
        UriTextResult missingLimits = IriNormalizerCore.NormalizeIri(
            "x:é".AsSpan(),
            UnicodeNormalizationMode.NormalizeNfc,
            null);
        UriTextResult invalidOutput = IriNormalizerCore.NormalizeIri(
            "".AsSpan(),
            UnicodeNormalizationMode.Preserve,
            negativeOutput);

        Assert.Multiple(() =>
        {
            AssertFailure(invalidIri, UriDiagnosticCode.InvalidUnicode, UriComponent.Path, 2, 1);
            AssertFailure(invalidMode, UriDiagnosticCode.InvalidInput, UriComponent.Input, 0, 0);
            AssertFailure(missingLimits, UriDiagnosticCode.InvalidInput, UriComponent.Input, 0, 0);
            AssertFailure(invalidOutput, UriDiagnosticCode.LimitExceeded, UriComponent.Output, 0, 0);
        });
    }

    private static void AssertSuccess(UriTextResult result, string expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Length, Is.EqualTo(expected.Length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertFailure(
        UriTextResult result,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(length));
        });
    }
}
