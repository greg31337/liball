namespace uri.Tests;

public class IriDataTests
{
    [Test]
    public void IriReferenceDefaultsToAnEmptyLosslessModel()
    {
        IriReference reference = new IriReference();

        Assert.Multiple(() =>
        {
            Assert.That(reference.Kind, Is.EqualTo(UriReferenceKind.EmptyReference));
            Assert.That(reference.HasScheme, Is.False);
            Assert.That(reference.Scheme, Is.Empty);
            Assert.That(reference.HasAuthority, Is.False);
            Assert.That(reference.Authority, Is.Not.Null);
            Assert.That(reference.Authority.HostKind, Is.EqualTo(UriHostKind.Absent));
            Assert.That(reference.Path, Is.Empty);
            Assert.That(reference.HasQuery, Is.False);
            Assert.That(reference.Query, Is.Empty);
            Assert.That(reference.HasFragment, Is.False);
            Assert.That(reference.Fragment, Is.Empty);
            Assert.That(reference.OriginalText, Is.Empty);
        });
    }

    [Test]
    public void IriParseResultStoresReferenceAndDiagnosticOutcomes()
    {
        IriReference reference = new IriReference();
        reference.Path = "/résumé";
        UriDiagnostic diagnostic = new UriDiagnostic(
            DiagnosticDomain.Iri,
            UriDiagnosticCode.InvalidIri,
            DiagnosticSeverity.Error,
            UriComponent.Iri,
            new TextRange(2, 1),
            "invalid IRI");

        IriParseResult success = new IriParseResult(true, reference, null);
        IriParseResult failure = new IriParseResult(
            false,
            new IriReference(),
            diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(success.Success, Is.True);
            Assert.That(success.Reference, Is.SameAs(reference));
            Assert.That(success.Reference.Path, Is.EqualTo("/résumé"));
            Assert.That(success.Diagnostic, Is.Null);
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Diagnostic, Is.SameAs(diagnostic));
        });
    }

    [Test]
    public void IriOptionsUseIndependentLimitsAndExplicitConservativeModes()
    {
        IriParseOptions firstParse = new IriParseOptions();
        IriParseOptions secondParse = new IriParseOptions();
        IriConversionOptions firstConversion = new IriConversionOptions();
        IriConversionOptions secondConversion = new IriConversionOptions();

        firstParse.Limits.MaxInputCharacters = 1;
        firstConversion.NormalizationMode = UnicodeNormalizationMode.NormalizeNfc;
        firstConversion.IdnMode = IriIdnMode.Platform;

        Assert.Multiple(() =>
        {
            Assert.That(secondParse.Limits.MaxInputCharacters, Is.EqualTo(1048576));
            Assert.That(firstParse.Limits, Is.Not.SameAs(secondParse.Limits));
            Assert.That(firstConversion.Limits, Is.Not.SameAs(secondConversion.Limits));
            Assert.That(firstConversion.NormalizationMode, Is.EqualTo(UnicodeNormalizationMode.NormalizeNfc));
            Assert.That(firstConversion.IdnMode, Is.EqualTo(IriIdnMode.Platform));
            Assert.That(secondConversion.NormalizationMode, Is.EqualTo(UnicodeNormalizationMode.Preserve));
            Assert.That(secondConversion.IdnMode, Is.EqualTo(IriIdnMode.None));
        });
    }

    [Test]
    public void UnicodeNormalizationModesExposePreserveNormalizeAndDiagnoseChoices()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)UnicodeNormalizationMode.Preserve, Is.EqualTo(0));
            Assert.That((int)UnicodeNormalizationMode.NormalizeNfc, Is.EqualTo(1));
            Assert.That((int)UnicodeNormalizationMode.DiagnoseNfc, Is.EqualTo(2));
            Assert.That((int)IriIdnMode.None, Is.EqualTo(0));
            Assert.That((int)IriIdnMode.Platform, Is.EqualTo(1));
        });
    }
}
