namespace uri.Tests;

public class UriOptionsAndResultsTests
{
    [Test]
    public void ParseOptionsStartWithIndependentDefaultLimits()
    {
        UriParseOptions first = new UriParseOptions();
        UriParseOptions second = new UriParseOptions();
        first.Limits.MaxInputCharacters = 12;

        Assert.Multiple(() =>
        {
            Assert.That(first.Limits.MaxInputCharacters, Is.EqualTo(12));
            Assert.That(second.Limits.MaxInputCharacters, Is.EqualTo(1048576));
            Assert.That(second.Limits, Is.Not.SameAs(first.Limits));
        });
    }

    [Test]
    public void ValidationOptionsDefaultToNoRequirementsAndRemainConfigurable()
    {
        UriValidationOptions options = new UriValidationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Limits, Is.Not.Null);
            Assert.That(options.RequireScheme, Is.False);
            Assert.That(options.RequireAuthority, Is.False);
            Assert.That(options.RequireHost, Is.False);
            Assert.That(options.RequirePath, Is.False);
            Assert.That(options.RequireQuery, Is.False);
            Assert.That(options.RequireFragment, Is.False);
            Assert.That(options.RequireAbsoluteUri, Is.False);
        });

        options.RequireScheme = true;
        options.RequireAuthority = true;
        options.RequireHost = true;
        options.RequirePath = true;
        options.RequireQuery = true;
        options.RequireFragment = true;
        options.RequireAbsoluteUri = true;

        Assert.Multiple(() =>
        {
            Assert.That(options.RequireScheme, Is.True);
            Assert.That(options.RequireAuthority, Is.True);
            Assert.That(options.RequireHost, Is.True);
            Assert.That(options.RequirePath, Is.True);
            Assert.That(options.RequireQuery, Is.True);
            Assert.That(options.RequireFragment, Is.True);
            Assert.That(options.RequireAbsoluteUri, Is.True);
        });
    }

    [Test]
    public void ParseResultRetainsSuccessValueOrFailureDiagnostic()
    {
        UriReference reference = new UriReference();
        reference.Kind = UriReferenceKind.AbsolutePathReference;
        reference.Path = "/a";
        UriDiagnostic diagnostic = CreateDiagnostic(UriDiagnosticCode.InvalidPath, 2);

        UriParseResult success = new UriParseResult(true, reference, null);
        UriParseResult failure = new UriParseResult(false, new UriReference(), diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(success.Success, Is.True);
            Assert.That(success.Reference, Is.SameAs(reference));
            Assert.That(success.Diagnostic, Is.Null);
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Reference.Kind, Is.EqualTo(UriReferenceKind.EmptyReference));
            Assert.That(failure.Diagnostic, Is.SameAs(diagnostic));
        });
    }

    [Test]
    public void ValidationResultPreservesFirstAndAllDiagnosticsInOrder()
    {
        UriReference reference = new UriReference();
        UriDiagnostic first = CreateDiagnostic(UriDiagnosticCode.MissingRequiredComponent, 0);
        UriDiagnostic second = CreateDiagnostic(UriDiagnosticCode.InvalidHost, 4);
        UriDiagnostic[] diagnostics = new UriDiagnostic[] { first, second };

        UriValidationResult result = new UriValidationResult(
            false,
            reference,
            first,
            diagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Reference, Is.SameAs(reference));
            Assert.That(result.Diagnostic, Is.SameAs(first));
            Assert.That(result.Diagnostics, Is.SameAs(diagnostics));
            Assert.That(result.Diagnostics, Is.EqualTo(new UriDiagnostic[] { first, second }));
        });
    }

    [Test]
    public void SuccessfulValidationCanCarryTheValidatedReferenceAndNoDiagnostics()
    {
        UriReference reference = new UriReference();
        reference.Kind = UriReferenceKind.RelativePathReference;
        reference.Path = "a/b";

        UriValidationResult result = new UriValidationResult(
            true,
            reference,
            null,
            new UriDiagnostic[0]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Reference, Is.SameAs(reference));
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    private static UriDiagnostic CreateDiagnostic(UriDiagnosticCode code, int offset)
    {
        return new UriDiagnostic(
            DiagnosticDomain.Uri,
            code,
            DiagnosticSeverity.Error,
            UriComponent.Input,
            new TextRange(offset, 1),
            "URI diagnostic");
    }
}
