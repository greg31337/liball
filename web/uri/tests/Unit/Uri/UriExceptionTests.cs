namespace uri.Tests;

public class UriExceptionTests
{
    [Test]
    public void SyntaxExceptionRetainsDiagnosticContextAndMessage()
    {
        UriDiagnostic diagnostic = CreateDiagnostic(
            UriDiagnosticCode.InvalidScheme,
            UriComponent.Scheme,
            "The scheme is invalid.");

        UriSyntaxException exception = new UriSyntaxException(diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(exception.Message, Is.EqualTo(diagnostic.Message));
        });
    }

    [Test]
    public void ValidationExceptionRetainsDiagnosticContextAndMessage()
    {
        UriDiagnostic diagnostic = CreateDiagnostic(
            UriDiagnosticCode.MissingRequiredComponent,
            UriComponent.Host,
            "A required host is absent.");

        UriValidationException exception = new UriValidationException(diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(exception.Message, Is.EqualTo(diagnostic.Message));
        });
    }

    private static UriDiagnostic CreateDiagnostic(
        UriDiagnosticCode code,
        UriComponent component,
        string message)
    {
        return new UriDiagnostic(
            DiagnosticDomain.Uri,
            code,
            DiagnosticSeverity.Error,
            component,
            new TextRange(3, 2),
            message);
    }
}
