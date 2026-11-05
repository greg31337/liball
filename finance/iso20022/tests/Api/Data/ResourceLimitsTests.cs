using iso20022;

namespace iso20022.Tests;

public class ResourceLimitsTests
{
    [Test]
    public void NewLimitsUseThePublishedConservativeDefaults()
    {
        ResourceLimits limits = new ResourceLimits();

        Assert.Multiple(() =>
        {
            Assert.That(limits.MaxDepth, Is.EqualTo(Iso20022Limits.MaxDepth));
            Assert.That(limits.MaxAttributesPerElement, Is.EqualTo(Iso20022Limits.MaxAttributesPerElement));
            Assert.That(limits.MaxTextLength, Is.EqualTo(Iso20022Limits.MaxTextLength));
            Assert.That(limits.MaxElementCount, Is.EqualTo(Iso20022Limits.MaxElementCount));
            Assert.That(limits.MaxCollectionCount, Is.EqualTo(Iso20022Limits.MaxCollectionCount));
            Assert.That(limits.MaxTotalBytes, Is.EqualTo(Iso20022Limits.MaxTotalBytes));
            Assert.That(limits.MaxSchemaCount, Is.EqualTo(Iso20022Limits.MaxSchemaCount));
            Assert.That(limits.MaxDiagnosticCount, Is.EqualTo(Iso20022Limits.MaxDiagnosticCount));
        });
    }

    [Test]
    public void NamespaceAndMediaConstantsMatchSupportedArtifactIdentifiers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Iso20022Namespaces.SchemaUrnPrefix, Is.EqualTo("urn:iso:std:iso:20022:tech:xsd:"));
            Assert.That(Iso20022Namespaces.Ecore2013, Is.EqualTo("urn:iso:std:iso:20022:2013:ecore"));
            Assert.That(Iso20022Namespaces.Xmi, Is.EqualTo("http://www.omg.org/XMI"));
            Assert.That(Iso20022Namespaces.XmlSchema, Is.EqualTo("http://www.w3.org/2001/XMLSchema"));
            Assert.That(Iso20022Namespaces.XmlSchemaInstance, Is.EqualTo("http://www.w3.org/2001/XMLSchema-instance"));
            Assert.That(Iso20022MediaTypes.Xml, Is.EqualTo("application/xml"));
            Assert.That(Iso20022MediaTypes.Json, Is.EqualTo("application/json"));
            Assert.That(Iso20022MediaTypes.Zip, Is.EqualTo("application/zip"));
        });
    }
}
