using System.Text;
using System.Xml;

namespace odf.Tests;

public class SecureXmlReaderTests
{
    [Test]
    public void PreservesWhitespaceCommentsInstructionsAndStandardCharacterReferences()
    {
        const string xml = "<?document mode='test'?><root> before &lt;&amp;&#65;<!--keep--><child/> after </root>";
        OdfSecurityLimits limits = Limits();
        List<XmlNodeType> nodeTypes = new List<XmlNodeType>();
        List<string> values = new List<string>();

        using (MemoryStream input = Input(xml))
        using (XmlReader reader = SecureXmlReader.Create(input, limits, true))
        {
            while (SecureXmlReader.Read(reader, limits))
            {
                nodeTypes.Add(reader.NodeType);
                values.Add(reader.Value);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(nodeTypes, Does.Contain(XmlNodeType.ProcessingInstruction));
            Assert.That(nodeTypes, Does.Contain(XmlNodeType.Comment));
            Assert.That(values, Does.Contain(" before <&A"));
            Assert.That(values, Does.Contain("keep"));
            Assert.That(values, Does.Contain(" after "));
        });
    }

    [TestCase("<!DOCTYPE root SYSTEM 'file:///does-not-exist.dtd'><root/>")]
    [TestCase("<!DOCTYPE root [<!ENTITY value 'expanded'>]><root>&value;</root>")]
    public void RejectsExternalAndInternalDocumentTypeEntities(string xml)
    {
        OdfSecurityLimits limits = Limits();

        Assert.That(
            () => ReadAll(xml, limits),
            Throws.TypeOf<XmlException>());
    }

    [Test]
    public void EnforcesTotalDocumentCharacterLimit()
    {
        OdfSecurityLimits limits = Limits();
        limits.MaximumSingleEntryBytes = 16;

        Assert.That(
            () => ReadAll("<root>1234567890</root>", limits),
            Throws.TypeOf<XmlException>());
    }

    [Test]
    public void EnforcesElementDepthLimit()
    {
        OdfSecurityLimits limits = Limits();
        limits.MaximumXmlDepth = 1;

        Assert.That(
            () => ReadAll("<root><child/></root>", limits),
            Throws.TypeOf<OdfXmlException>()
                .With.Message.Contains("element-depth"));
    }

    [Test]
    public void EnforcesPerElementAttributeLimit()
    {
        OdfSecurityLimits limits = Limits();
        limits.MaximumXmlAttributesPerElement = 1;

        Assert.That(
            () => ReadAll("<root first='1' second='2'/>", limits),
            Throws.TypeOf<OdfXmlException>()
                .With.Message.Contains("attribute"));
    }

    [TestCase("<root>four</root>")]
    [TestCase("<root value='four'/>")]
    [TestCase("<root><!--four--></root>")]
    [TestCase("<root><?target four?></root>")]
    public void EnforcesCharacterDataAndAttributeValueLength(string xml)
    {
        OdfSecurityLimits limits = Limits();
        limits.MaximumXmlTextLength = 3;

        Assert.That(
            () => ReadAll(xml, limits),
            Throws.TypeOf<OdfXmlException>()
                .With.Message.Contains("text-value"));
    }

    [Test]
    public void AppliesConfiguredInputOwnership()
    {
        OdfSecurityLimits limits = Limits();
        MemoryStream retained = Input("<root/>");
        using (XmlReader reader = SecureXmlReader.Create(retained, limits, true))
        {
            while (SecureXmlReader.Read(reader, limits))
            {
            }
        }
        MemoryStream closed = Input("<root/>");
        using (XmlReader reader = SecureXmlReader.Create(closed, limits, false))
        {
            while (SecureXmlReader.Read(reader, limits))
            {
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(retained.CanRead, Is.True);
            Assert.That(closed.CanRead, Is.False);
        });
        retained.Dispose();
    }

    private static void ReadAll(string xml, OdfSecurityLimits limits)
    {
        using MemoryStream input = Input(xml);
        using XmlReader reader = SecureXmlReader.Create(input, limits, true);
        while (SecureXmlReader.Read(reader, limits))
        {
        }
    }

    private static MemoryStream Input(string xml)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(xml), false);
    }

    private static OdfSecurityLimits Limits()
    {
        return new OdfSecurityLimits
        {
            MaximumSingleEntryBytes = 1048576,
            MaximumXmlDepth = 32,
            MaximumXmlAttributesPerElement = 32,
            MaximumXmlTextLength = 1024
        };
    }
}
