namespace odf.Tests;

public class OdfExceptionTests
{
    [Test]
    public void PackageExceptionPreservesItsMessage()
    {
        OdfPackageException exception = new OdfPackageException("Package entry is corrupt.");

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.InstanceOf<System.Exception>());
            Assert.That(exception.Message, Is.EqualTo("Package entry is corrupt."));
        });
    }

    [Test]
    public void XmlExceptionPreservesItsMessage()
    {
        OdfXmlException exception = new OdfXmlException("XML content is malformed.");

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.InstanceOf<System.Exception>());
            Assert.That(exception.Message, Is.EqualTo("XML content is malformed."));
        });
    }

    [Test]
    public void DocumentExceptionPreservesItsMessage()
    {
        OdfDocumentException exception = new OdfDocumentException("Document loading failed.");

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.InstanceOf<System.Exception>());
            Assert.That(exception.Message, Is.EqualTo("Document loading failed."));
        });
    }

    [Test]
    public void ValidationExceptionPreservesItsMessage()
    {
        OdfValidationException exception = new OdfValidationException("Validation could not complete.");

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.InstanceOf<System.Exception>());
            Assert.That(exception.Message, Is.EqualTo("Validation could not complete."));
        });
    }

    [Test]
    public void FormulaExceptionPreservesItsMessage()
    {
        OdfFormulaException exception = new OdfFormulaException("Formula parsing failed.");

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.InstanceOf<System.Exception>());
            Assert.That(exception.Message, Is.EqualTo("Formula parsing failed."));
        });
    }

    [Test]
    public void EditExceptionPreservesItsMessage()
    {
        OdfEditException exception = new OdfEditException("The edit could not be applied.");

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.InstanceOf<System.Exception>());
            Assert.That(exception.Message, Is.EqualTo("The edit could not be applied."));
        });
    }

    [Test]
    public void SecurityExceptionPreservesItsMessage()
    {
        OdfSecurityException exception = new OdfSecurityException("Signature verification failed.");

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.InstanceOf<System.Exception>());
            Assert.That(exception.Message, Is.EqualTo("Signature verification failed."));
        });
    }
}
