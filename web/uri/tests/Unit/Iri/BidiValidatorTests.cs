namespace uri.Tests;

public class BidiValidatorTests
{
    [TestCase("x://user@example.test/path?query#fragment")]
    [TestCase("x://שלום@שלום/שלום?שלום#שלום")]
    [TestCase("x:/שלום//שלום/")]
    [TestCase("x:/𞸀?𞸀#𞸀")]
    [TestCase("")]
    public void Rfc3987ComponentsWithoutDirectionConflictsHaveNoDiagnostic(
        string iri)
    {
        UriDiagnostic? diagnostic = BidiValidator.ValidateIri(iri.AsSpan());

        Assert.That(diagnostic, Is.Null);
    }

    [TestCase("x://שa@host", UriComponent.UserInfo, 5)]
    [TestCase("x://user@שa", UriComponent.Host, 10)]
    [TestCase("x:/שa", UriComponent.Path, 4)]
    [TestCase("x:?שa", UriComponent.Query, 4)]
    [TestCase("x:#שa", UriComponent.Fragment, 4)]
    public void MixedStrongDirectionsReportTheFirstLtrCharacter(
        string iri,
        UriComponent component,
        int offset)
    {
        UriDiagnostic? diagnostic = BidiValidator.ValidateIri(iri.AsSpan());

        AssertBidiDiagnostic(
            diagnostic,
            DiagnosticSeverity.Warning,
            component,
            offset,
            1);
    }

    [TestCase("x:/1שלום", 3)]
    [TestCase("x:/-שלום", 3)]
    [TestCase("x:/שלום1", 7)]
    [TestCase("x:/שלום-", 7)]
    public void RtlComponentsMustStartAndEndWithRtlCharacters(
        string iri,
        int offset)
    {
        UriDiagnostic? diagnostic = BidiValidator.ValidateIri(iri.AsSpan());

        AssertBidiDiagnostic(
            diagnostic,
            DiagnosticSeverity.Warning,
            UriComponent.Path,
            offset,
            1);
    }

    [Test]
    public void PathSegmentsAreValidatedIndependently()
    {
        UriDiagnostic? diagnostic = BidiValidator.ValidateIri(
            "x:/שלום/שa/שלום".AsSpan());

        AssertBidiDiagnostic(
            diagnostic,
            DiagnosticSeverity.Warning,
            UriComponent.Path,
            9,
            1);
    }

    [Test]
    public void PercentEncodedDataDoesNotCreateAUnicodeDirection()
    {
        UriDiagnostic? encodedOnly = BidiValidator.ValidateIri(
            "x:/%D7%A9%D7%9C%D7%95%D7%9D".AsSpan());
        UriDiagnostic? trailingEncoding = BidiValidator.ValidateIri(
            "x:/שלום%31".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(encodedOnly, Is.Null);
            AssertBidiDiagnostic(
                trailingEncoding,
                DiagnosticSeverity.Warning,
                UriComponent.Path,
                7,
                3);
        });
    }

    [Test]
    public void InvalidIriSyntaxIsReturnedBeforeBidiAnalysis()
    {
        UriDiagnostic? diagnostic = BidiValidator.ValidateIri(
            "x:/bad space".AsSpan());

        Assert.That(diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPath));
            Assert.That(diagnostic.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(diagnostic.Range.Offset, Is.EqualTo(6));
        });
    }

    [TestCase('\u200E')]
    [TestCase('\u200F')]
    [TestCase('\u202A')]
    [TestCase('\u202B')]
    [TestCase('\u202C')]
    [TestCase('\u202D')]
    [TestCase('\u202E')]
    public void Rfc3987FormattingCharactersAreHardErrors(char formattingCharacter)
    {
        string iri = "x:/a" + formattingCharacter + "b";

        UriDiagnostic? diagnostic = BidiValidator.ValidateIri(iri.AsSpan());

        AssertBidiDiagnostic(
            diagnostic,
            DiagnosticSeverity.Error,
            UriComponent.Path,
            4,
            1);
    }

    [TestCase("שלום")]
    [TestCase("שלום1")]
    [TestCase("שלום\u0301")]
    [TestCase("مثال")]
    public void RtlIdnLabelsAcceptValidStartsAndPermittedEnds(string label)
    {
        UriDiagnostic? diagnostic = BidiValidator.ValidateIdnLabelText(
            label.AsSpan(),
            20);

        Assert.That(diagnostic, Is.Null);
    }

    [TestCase("aשלום", 20)]
    [TestCase("1שלום", 20)]
    [TestCase("שלוםa", 24)]
    [TestCase("שלום-", 24)]
    [TestCase("שלום1٢", 25)]
    public void InvalidRtlIdnLabelsReturnHardErrors(string label, int offset)
    {
        UriDiagnostic? diagnostic = BidiValidator.ValidateIdnLabelText(
            label.AsSpan(),
            20);

        AssertBidiDiagnostic(
            diagnostic,
            DiagnosticSeverity.Error,
            UriComponent.Host,
            offset,
            1);
    }

    [Test]
    public void IdnHostValidationUsesIndependentLabelsAndSourceOffsets()
    {
        UriDiagnostic? diagnostic = BidiValidator.ValidateIdnHost(
            "example.1שלום.test".AsSpan(),
            IriIdnMode.Platform);

        AssertBidiDiagnostic(
            diagnostic,
            DiagnosticSeverity.Error,
            UriComponent.Host,
            8,
            1);
    }

    [Test]
    public void IdnValidationRunsOnlyForTheSelectedMappingMode()
    {
        UriDiagnostic? none = BidiValidator.ValidateIdnHost(
            "1שלום".AsSpan(),
            IriIdnMode.None);
        UriDiagnostic? unknown = BidiValidator.ValidateIdnHost(
            "שלום".AsSpan(),
            (IriIdnMode)999);

        Assert.Multiple(() =>
        {
            Assert.That(none, Is.Null);
            Assert.That(unknown, Is.Not.Null);
            Assert.That(unknown!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(unknown.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
            Assert.That(unknown.Component, Is.EqualTo(UriComponent.Host));
        });
    }

    [TestCase("שלום.example", "xn--9dbne9b.example")]
    [TestCase("مثال.example", "xn--mgbh0fb.example")]
    public void IdnConversionAcceptsPrevalidatedRtlLabels(
        string unicode,
        string ascii)
    {
        OperationLimits limits = new OperationLimits();
        UriTextResult encoded = IdnHostCore.ConvertHostToAscii(
            unicode.AsSpan(),
            IriIdnMode.Platform,
            limits);
        UriTextResult decoded = IdnHostCore.ConvertHostToUnicode(
            ascii.AsSpan(),
            IriIdnMode.Platform,
            limits);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Text, Is.EqualTo(ascii));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Text, Is.EqualTo(unicode));
        });
    }

    [TestCase("aשלום.example", 0)]
    [TestCase("1שלום.example", 0)]
    [TestCase("שלוםa.example", 4)]
    [TestCase("שלום1٢.example", 5)]
    public void IdnConversionRejectsBidiFailuresBeforePlatformMapping(
        string host,
        int offset)
    {
        UriTextResult result = IdnHostCore.ConvertHostToAscii(
            host.AsSpan(),
            IriIdnMode.Platform,
            new OperationLimits());

        Assert.That(result.Success, Is.False);
        AssertBidiDiagnostic(
            result.Diagnostic,
            DiagnosticSeverity.Error,
            UriComponent.Host,
            offset,
            1);
    }

    private static void AssertBidiDiagnostic(
        UriDiagnostic? diagnostic,
        DiagnosticSeverity severity,
        UriComponent component,
        int offset,
        int length)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidBidiText));
            Assert.That(diagnostic.Severity, Is.EqualTo(severity));
            Assert.That(diagnostic.Component, Is.EqualTo(component));
            Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
            Assert.That(diagnostic.Range.Length, Is.EqualTo(length));
        });
    }
}
