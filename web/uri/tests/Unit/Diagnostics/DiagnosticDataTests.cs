namespace uri.Tests;

public class DiagnosticDataTests
{
    [Test]
    public void TextRangeConstructorPreservesCoordinates()
    {
        TextRange range = new TextRange(7, 3);

        Assert.Multiple(() =>
        {
            Assert.That(range.Offset, Is.EqualTo(7));
            Assert.That(range.Length, Is.EqualTo(3));
        });
    }

    [Test]
    public void UriDiagnosticConstructorPreservesAllData()
    {
        TextRange range = new TextRange(12, 4);
        UriDiagnostic diagnostic = new UriDiagnostic(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidHost,
            DiagnosticSeverity.Warning,
            UriComponent.Host,
            range,
            "A stable message.");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidHost));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Warning));
            Assert.That(diagnostic.Component, Is.EqualTo(UriComponent.Host));
            Assert.That(diagnostic.Range, Is.SameAs(range));
            Assert.That(diagnostic.Message, Is.EqualTo("A stable message."));
        });
    }

    [Test]
    public void DiagnosticEnumValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)DiagnosticDomain.General, Is.EqualTo(0));
            Assert.That((int)DiagnosticDomain.WebLink, Is.EqualTo(6));
            Assert.That((int)DiagnosticSeverity.Error, Is.EqualTo(0));
            Assert.That((int)DiagnosticSeverity.Warning, Is.EqualTo(1));
            Assert.That((int)UriDiagnosticCode.None, Is.EqualTo(0));
            Assert.That((int)UriDiagnosticCode.LimitExceeded, Is.EqualTo(7));
            Assert.That((int)UriDiagnosticCode.IncompatibleOptions, Is.EqualTo(46));
            Assert.That((int)UriComponent.None, Is.EqualTo(0));
            Assert.That((int)UriComponent.LanguageTag, Is.EqualTo(29));
        });
    }
}
