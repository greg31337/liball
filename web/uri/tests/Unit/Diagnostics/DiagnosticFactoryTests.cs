namespace uri.Tests;

public class DiagnosticFactoryTests
{
    [Test]
    public void CreateErrorPreservesSuppliedContext()
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateError(
            DiagnosticDomain.Urn,
            UriDiagnosticCode.InvalidUrn,
            UriComponent.Urn,
            5,
            8,
            "The URN is invalid.");

        AssertDiagnostic(
            diagnostic,
            DiagnosticDomain.Urn,
            UriDiagnosticCode.InvalidUrn,
            DiagnosticSeverity.Error,
            UriComponent.Urn,
            5,
            8,
            "The URN is invalid.");
    }

    [Test]
    public void CreateWarningPreservesSuppliedContext()
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateWarning(
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.DuplicateParameter,
            UriComponent.LinkParameter,
            17,
            6,
            "The repeated parameter was ignored.");

        AssertDiagnostic(
            diagnostic,
            DiagnosticDomain.WebLink,
            UriDiagnosticCode.DuplicateParameter,
            DiagnosticSeverity.Warning,
            UriComponent.LinkParameter,
            17,
            6,
            "The repeated parameter was ignored.");
    }

    [Test]
    public void CreateInvalidCharacterUsesOneCodeUnitRangeAndStableMessage()
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateInvalidCharacter(
            DiagnosticDomain.Uri,
            UriComponent.Path,
            9);

        AssertDiagnostic(
            diagnostic,
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidCharacter,
            DiagnosticSeverity.Error,
            UriComponent.Path,
            9,
            1,
            DiagnosticFactory.InvalidCharacterMessage);
    }

    [Test]
    public void CreateUnexpectedEndUsesEmptyRangeAtEndOffset()
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateUnexpectedEnd(
            DiagnosticDomain.UriTemplate,
            UriComponent.TemplateExpression,
            22);

        AssertDiagnostic(
            diagnostic,
            DiagnosticDomain.UriTemplate,
            UriDiagnosticCode.UnexpectedEnd,
            DiagnosticSeverity.Error,
            UriComponent.TemplateExpression,
            22,
            0,
            DiagnosticFactory.UnexpectedEndMessage);
    }

    [Test]
    public void CreateInvalidPercentEncodingUsesPercentDomain()
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateInvalidPercentEncoding(
            UriComponent.Query,
            3,
            2);

        AssertDiagnostic(
            diagnostic,
            DiagnosticDomain.PercentEncoding,
            UriDiagnosticCode.InvalidPercentEncoding,
            DiagnosticSeverity.Error,
            UriComponent.Query,
            3,
            2,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    [Test]
    public void CreateInvalidUnicodePreservesCallingDomain()
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateInvalidUnicode(
            DiagnosticDomain.Iri,
            UriComponent.Iri,
            14,
            1);

        AssertDiagnostic(
            diagnostic,
            DiagnosticDomain.Iri,
            UriDiagnosticCode.InvalidUnicode,
            DiagnosticSeverity.Error,
            UriComponent.Iri,
            14,
            1,
            DiagnosticFactory.InvalidUnicodeMessage);
    }

    [Test]
    public void CreateInvalidUtf8UsesPercentDomain()
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateInvalidUtf8(
            UriComponent.Fragment,
            30,
            9);

        AssertDiagnostic(
            diagnostic,
            DiagnosticDomain.PercentEncoding,
            UriDiagnosticCode.InvalidUtf8,
            DiagnosticSeverity.Error,
            UriComponent.Fragment,
            30,
            9,
            DiagnosticFactory.InvalidUtf8Message);
    }

    [Test]
    public void CreateLimitExceededPreservesOperationAndRange()
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateLimitExceeded(
            DiagnosticDomain.UriTemplate,
            UriComponent.Output,
            1024,
            12);

        AssertDiagnostic(
            diagnostic,
            DiagnosticDomain.UriTemplate,
            UriDiagnosticCode.LimitExceeded,
            DiagnosticSeverity.Error,
            UriComponent.Output,
            1024,
            12,
            DiagnosticFactory.LimitExceededMessage);
    }

    [Test]
    public void FactoryResultsOwnIndependentRangeObjects()
    {
        UriDiagnostic first = DiagnosticFactory.CreateInvalidCharacter(
            DiagnosticDomain.Uri,
            UriComponent.Path,
            2);
        UriDiagnostic second = DiagnosticFactory.CreateInvalidCharacter(
            DiagnosticDomain.Uri,
            UriComponent.Path,
            7);

        first.Range.Offset = 99;

        Assert.Multiple(() =>
        {
            Assert.That(first.Range, Is.Not.SameAs(second.Range));
            Assert.That(second.Range.Offset, Is.EqualTo(7));
            Assert.That(second.Range.Length, Is.EqualTo(1));
        });
    }

    private static void AssertDiagnostic(
        UriDiagnostic diagnostic,
        DiagnosticDomain domain,
        UriDiagnosticCode code,
        DiagnosticSeverity severity,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Domain, Is.EqualTo(domain));
            Assert.That(diagnostic.Code, Is.EqualTo(code));
            Assert.That(diagnostic.Severity, Is.EqualTo(severity));
            Assert.That(diagnostic.Component, Is.EqualTo(component));
            Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
            Assert.That(diagnostic.Range.Length, Is.EqualTo(length));
            Assert.That(diagnostic.Message, Is.EqualTo(message));
        });
    }
}
