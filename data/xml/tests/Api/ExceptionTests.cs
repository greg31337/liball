using xml;

namespace xml.Tests;

[TestFixture]
public class ExceptionTests
{
    [Test]
    public void EveryExceptionPreservesMessageDiagnosticAndLocation()
    {
        SourceLocation location = new SourceLocation();
        location.SourceUri = "file:///source.xml";
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = "TEST0001";
        XmlException exception0 = new XmlException("XmlException", diagnostic, location);
        AssertExceptionContext(exception0, exception0.Diagnostic, exception0.Location, "XmlException", diagnostic, location);
        XmlWellFormednessException exception1 = new XmlWellFormednessException("XmlWellFormednessException", diagnostic, location);
        AssertExceptionContext(exception1, exception1.Diagnostic, exception1.Location, "XmlWellFormednessException", diagnostic, location);
        XmlNamespaceException exception2 = new XmlNamespaceException("XmlNamespaceException", diagnostic, location);
        AssertExceptionContext(exception2, exception2.Diagnostic, exception2.Location, "XmlNamespaceException", diagnostic, location);
        XmlValidationException exception3 = new XmlValidationException("XmlValidationException", diagnostic, location);
        AssertExceptionContext(exception3, exception3.Diagnostic, exception3.Location, "XmlValidationException", diagnostic, location);
        XmlResourceException exception4 = new XmlResourceException("XmlResourceException", diagnostic, location);
        AssertExceptionContext(exception4, exception4.Diagnostic, exception4.Location, "XmlResourceException", diagnostic, location);
        XmlLimitException exception5 = new XmlLimitException("XmlLimitException", diagnostic, location);
        AssertExceptionContext(exception5, exception5.Diagnostic, exception5.Location, "XmlLimitException", diagnostic, location);
        XmlSerializationException exception6 = new XmlSerializationException("XmlSerializationException", diagnostic, location);
        AssertExceptionContext(exception6, exception6.Diagnostic, exception6.Location, "XmlSerializationException", diagnostic, location);
        XmlCanonicalizationException exception7 = new XmlCanonicalizationException("XmlCanonicalizationException", diagnostic, location);
        AssertExceptionContext(exception7, exception7.Diagnostic, exception7.Location, "XmlCanonicalizationException", diagnostic, location);
        XmlSchemaException exception8 = new XmlSchemaException("XmlSchemaException", diagnostic, location);
        AssertExceptionContext(exception8, exception8.Diagnostic, exception8.Location, "XmlSchemaException", diagnostic, location);
        XPathException exception9 = new XPathException("XPathException", diagnostic, location);
        AssertExceptionContext(exception9, exception9.Diagnostic, exception9.Location, "XPathException", diagnostic, location);
        XPathStaticException exception10 = new XPathStaticException("XPathStaticException", diagnostic, location);
        AssertExceptionContext(exception10, exception10.Diagnostic, exception10.Location, "XPathStaticException", diagnostic, location);
        XPathTypeException exception11 = new XPathTypeException("XPathTypeException", diagnostic, location);
        AssertExceptionContext(exception11, exception11.Diagnostic, exception11.Location, "XPathTypeException", diagnostic, location);
        XPathDynamicException exception12 = new XPathDynamicException("XPathDynamicException", diagnostic, location);
        AssertExceptionContext(exception12, exception12.Diagnostic, exception12.Location, "XPathDynamicException", diagnostic, location);
        XsltException exception13 = new XsltException("XsltException", diagnostic, location);
        AssertExceptionContext(exception13, exception13.Diagnostic, exception13.Location, "XsltException", diagnostic, location);
        XsltStaticException exception14 = new XsltStaticException("XsltStaticException", diagnostic, location);
        AssertExceptionContext(exception14, exception14.Diagnostic, exception14.Location, "XsltStaticException", diagnostic, location);
        XsltTypeException exception15 = new XsltTypeException("XsltTypeException", diagnostic, location);
        AssertExceptionContext(exception15, exception15.Diagnostic, exception15.Location, "XsltTypeException", diagnostic, location);
        XsltDynamicException exception16 = new XsltDynamicException("XsltDynamicException", diagnostic, location);
        AssertExceptionContext(exception16, exception16.Diagnostic, exception16.Location, "XsltDynamicException", diagnostic, location);
        XsltTerminationException exception17 = new XsltTerminationException("XsltTerminationException", diagnostic, location);
        AssertExceptionContext(exception17, exception17.Diagnostic, exception17.Location, "XsltTerminationException", diagnostic, location);
    }

    [Test]
    public void ExceptionContextCanBeUnavailable()
    {
        XmlException exception = new XmlException("message", null, null);

        Assert.That(exception.Message, Is.EqualTo("message"));
        Assert.That(exception.Diagnostic, Is.Null);
        Assert.That(exception.Location, Is.Null);
    }

    private static void AssertExceptionContext(Exception exception, XmlDiagnostic? actualDiagnostic, SourceLocation? actualLocation, string message, XmlDiagnostic expectedDiagnostic, SourceLocation expectedLocation)
    {
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(actualDiagnostic, Is.SameAs(expectedDiagnostic));
        Assert.That(actualLocation, Is.SameAs(expectedLocation));
    }
}

