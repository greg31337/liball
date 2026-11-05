using System.Text;
using System.Xml;
using uspto;

namespace uspto.Tests;

public class SecureXmlReaderTests
{
    [Test]
    public void NamespacedMixedContentRetainsLocationsAttributesAndIndexedPaths()
    {
        string xml = "<p:root xmlns:p=\"urn:patent\"><p:item p:id=\"1\">A &amp; <![CDATA[B]]></p:item><p:item p:id=\"2\"/></p:root>";
        byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(xml)).ToArray();
        using MemoryStream input = new MemoryStream(bytes);
        SecureXmlReader reader = new SecureXmlReader(input, "namespaced.xml", CreateLimits(10, 100), false);

        Assert.That(reader.Read(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(reader.IsElement("root", null), Is.True);
            Assert.That(reader.IsElement("root", "urn:patent"), Is.True);
            Assert.That(reader.IsElement("root", "urn:other"), Is.False);
            Assert.That(reader.Current.Path, Is.EqualTo("/p:root"));
            Assert.That(reader.Current.HasLineInfo, Is.True);
            Assert.That(reader.Current.LineNumber, Is.EqualTo(1));
            Assert.That(reader.Current.LinePosition, Is.GreaterThan(0));
        });

        Assert.That(reader.Read(), Is.True);
        SecureXmlAttribute firstIdentifier = reader.ReadAttribute("id", null);
        Assert.Multiple(() =>
        {
            Assert.That(reader.Current.Path, Is.EqualTo("/p:root/p:item"));
            Assert.That(firstIdentifier.IsPresent, Is.True);
            Assert.That(firstIdentifier.Value, Is.EqualTo("1"));
            Assert.That(firstIdentifier.NamespaceUri, Is.EqualTo("urn:patent"));
            Assert.That(firstIdentifier.Path, Is.EqualTo("/p:root/p:item/@p:id"));
            Assert.That(reader.ReadAttribute("id", "urn:other").IsPresent, Is.False);
        });

        Assert.That(reader.Read(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(reader.Current.NodeType, Is.EqualTo(XmlNodeType.Text));
            Assert.That(reader.Current.Value, Is.EqualTo("A & "));
            Assert.That(reader.Current.Path, Is.EqualTo("/p:root/p:item"));
        });

        Assert.That(reader.Read(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(reader.Current.NodeType, Is.EqualTo(XmlNodeType.CDATA));
            Assert.That(reader.Current.Value, Is.EqualTo("B"));
        });

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.Current.NodeType, Is.EqualTo(XmlNodeType.EndElement));
        Assert.That(reader.Read(), Is.True);
        SecureXmlAttribute secondIdentifier = reader.ReadAttribute("id", "urn:patent");
        Assert.Multiple(() =>
        {
            Assert.That(reader.Current.Path, Is.EqualTo("/p:root/p:item[2]"));
            Assert.That(reader.Current.IsEmptyElement, Is.True);
            Assert.That(secondIdentifier.Value, Is.EqualTo("2"));
        });

        reader.Close();
        Assert.That(input.CanRead, Is.True);
    }

    [Test]
    public void StreamInputHonorsUtf16BomAndXmlEncodingDeclaration()
    {
        string xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?><root>Résumé 漢字</root>";
        byte[] bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(xml)).ToArray();
        using MemoryStream input = new MemoryStream(bytes);
        SecureXmlReader reader = new SecureXmlReader(input, "utf16.xml", CreateLimits(10, 100), false);

        while (reader.Read() && reader.Current.NodeType != XmlNodeType.Text)
        {
        }

        Assert.Multiple(() =>
        {
            Assert.That(reader.Current.NodeType, Is.EqualTo(XmlNodeType.Text));
            Assert.That(reader.Current.Value, Is.EqualTo("Résumé 漢字"));
            Assert.That(reader.Current.Path, Is.EqualTo("/root"));
        });
    }

    [Test]
    public void DtdAndExternalEntityDeclarationsAreRejectedWithoutClosingInput()
    {
        string xml = "<!DOCTYPE root [<!ENTITY secret SYSTEM \"http://127.0.0.1:1/secret\">]><root>&secret;</root>";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        SecureXmlReader reader = new SecureXmlReader(input, "external.xml", CreateLimits(10, 100), false);

        XmlException exception = Assert.Throws<XmlException>(() =>
        {
            while (reader.Read())
            {
            }
        });

        reader.Close();
        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("DTD").IgnoreCase);
            Assert.That(input.CanRead, Is.True);
        });
    }

    [Test]
    public void NestingDepthLimitReportsSourcePathAndLine()
    {
        string xml = "<root>\n<child><grandchild/></child>\n</root>";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        SecureXmlReader reader = new SecureXmlReader(input, "deep.xml", CreateLimits(2, 100), false);

        ParseException exception = Assert.Throws<ParseException>(() =>
        {
            while (reader.Read())
            {
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(exception.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(exception.SourceName, Is.EqualTo("deep.xml"));
            Assert.That(exception.DataPath, Is.EqualTo("/root/child/grandchild"));
            Assert.That(exception.HasLineNumber, Is.True);
            Assert.That(exception.LineNumber, Is.EqualTo(2));
            Assert.That(exception.HasOffset, Is.False);
        });
    }

    [Test]
    public void TextLimitAppliesAcrossAdjacentTextAndCdataNodes()
    {
        string xml = "<root>ab<![CDATA[cd]]>e</root>";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        SecureXmlReader reader = new SecureXmlReader(input, "text.xml", CreateLimits(10, 4), false);

        ParseException exception = Assert.Throws<ParseException>(() =>
        {
            while (reader.Read())
            {
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(exception.DataPath, Is.EqualTo("/root"));
            Assert.That(exception.SourceName, Is.EqualTo("text.xml"));
            Assert.That(exception.HasLineNumber, Is.True);
        });
    }

    [Test]
    public void AttributeValuesAreBoundedBeforeTheyAreExposed()
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes("<root value=\"abcde\"/>"));
        SecureXmlReader reader = new SecureXmlReader(input, "attribute.xml", CreateLimits(10, 4), false);

        ParseException exception = Assert.Throws<ParseException>(() => reader.Read());

        Assert.Multiple(() =>
        {
            Assert.That(exception.DataPath, Is.EqualTo("/root/@value"));
            Assert.That(exception.LineNumber, Is.EqualTo(1));
            Assert.That(exception.HasLineNumber, Is.True);
        });
    }

    [Test]
    public void MalformedXmlRetainsParserLineAndPositionInformation()
    {
        string xml = "<root>\n<child></root>";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        SecureXmlReader reader = new SecureXmlReader(input, "malformed.xml", CreateLimits(10, 100), false);

        XmlException exception = Assert.Throws<XmlException>(() =>
        {
            while (reader.Read())
            {
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(exception.LineNumber, Is.EqualTo(2));
            Assert.That(exception.LinePosition, Is.GreaterThan(0));
        });
    }

    private static UsptoLimits CreateLimits(int maximumDepth, int maximumTextCharacters)
    {
        UsptoLimits limits = new UsptoLimits();
        limits.MaximumXmlDepth = maximumDepth;
        limits.MaximumStringCharacters = maximumTextCharacters;
        return limits;
    }
}
