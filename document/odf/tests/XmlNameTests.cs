namespace odf.Tests;

public class XmlNameTests
{
    [TestCase("name")]
    [TestCase("_private")]
    [TestCase(":qualified")]
    [TestCase("a:b:c")]
    [TestCase("name-1.2")]
    [TestCase("éclair")]
    [TestCase("a\u0301")]
    [TestCase("a·b")]
    [TestCase("\U00010400name")]
    [TestCase("😀")]
    public void XmlNameAcceptsValidXml10Names(string value)
    {
        Assert.That(OdfNameRules.IsName(value), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("1name")]
    [TestCase("-name")]
    [TestCase(".name")]
    [TestCase("\u0301name")]
    [TestCase("has space")]
    [TestCase("name/")]
    [TestCase("\uFDD0")]
    [TestCase("\U000F0000")]
    public void XmlNameRejectsInvalidXml10Names(string? value)
    {
        Assert.That(OdfNameRules.IsName(value), Is.False);
    }

    [Test]
    public void XmlNameRejectsMalformedSurrogatePairs()
    {
        string highSurrogate = new string([(char)0xD800]);
        string lowSurrogate = new string([(char)0xDC00]);
        string reversedPair = new string([(char)0xDC00, (char)0xD800]);

        Assert.Multiple(() =>
        {
            Assert.That(OdfNameRules.IsName(highSurrogate), Is.False);
            Assert.That(OdfNameRules.IsName(lowSurrogate), Is.False);
            Assert.That(OdfNameRules.IsName(reversedPair), Is.False);
        });
    }

    [TestCase("name")]
    [TestCase("_private")]
    [TestCase("éclair")]
    [TestCase("a\u0301")]
    [TestCase("\U00010400name")]
    public void NcNameAndXmlIdAcceptNamesWithoutColons(string value)
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdfNameRules.IsNcName(value), Is.True);
            Assert.That(OdfNameRules.IsXmlId(value), Is.True);
        });
    }

    [TestCase(":name")]
    [TestCase("prefix:name")]
    [TestCase("a:b:c")]
    [TestCase("1name")]
    [TestCase("has space")]
    public void NcNameAndXmlIdRejectColonsAndInvalidNames(string value)
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdfNameRules.IsNcName(value), Is.False);
            Assert.That(OdfNameRules.IsXmlId(value), Is.False);
        });
    }

    [TestCase("name")]
    [TestCase("text:p")]
    [TestCase("δοκιμή:όνομα")]
    [TestCase("𐐀:𐐁")]
    public void QNameAcceptsOneOptionalPrefix(string value)
    {
        Assert.That(OdfNameRules.IsQName(value), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(":name")]
    [TestCase("prefix:")]
    [TestCase("a:b:c")]
    [TestCase("bad prefix:name")]
    [TestCase("prefix:1name")]
    public void QNameRejectsMalformedLexicalNames(string? value)
    {
        Assert.That(OdfNameRules.IsQName(value), Is.False);
    }

    [Test]
    public void NamespaceDeclarationsEnforceReservedBindings()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdfNameRules.IsNamespaceDeclarationValue(Declaration("", "")), Is.True);
            Assert.That(OdfNameRules.IsNamespaceDeclarationValue(Declaration("", OdfNamespaces.Office)), Is.True);
            Assert.That(OdfNameRules.IsNamespaceDeclarationValue(Declaration("text", OdfNamespaces.Text)), Is.True);
            Assert.That(OdfNameRules.IsNamespaceDeclarationValue(Declaration("xml", OdfNamespaces.Xml)), Is.True);
            Assert.That(OdfNameRules.IsNamespaceDeclarationValue(Declaration("text", "")), Is.False);
            Assert.That(OdfNameRules.IsNamespaceDeclarationValue(Declaration("bad:prefix", OdfNamespaces.Text)), Is.False);
            Assert.That(OdfNameRules.IsNamespaceDeclarationValue(Declaration("xmlns", OdfNamespaces.Text)), Is.False);
            Assert.That(OdfNameRules.IsNamespaceDeclarationValue(Declaration("p", "http://www.w3.org/2000/xmlns/")), Is.False);
            Assert.That(OdfNameRules.IsNamespaceDeclarationValue(Declaration("xml", OdfNamespaces.Text)), Is.False);
            Assert.That(OdfNameRules.IsNamespaceDeclarationValue(Declaration("other", OdfNamespaces.Xml)), Is.False);
            Assert.That(OdfNameRules.IsNamespaceDeclarationValue(Declaration("XMLdata", "urn:example")), Is.False);
            Assert.That(OdfNameRules.IsNamespaceDeclarationValue(null), Is.False);
        });
    }

    [Test]
    public void QualifiedNamesEnforceLocalPrefixAndNamespaceRules()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdfNameRules.IsQualifiedNameValue(Qualified("", "plain", "")), Is.True);
            Assert.That(OdfNameRules.IsQualifiedNameValue(Qualified(OdfNamespaces.Text, "p", "text")), Is.True);
            Assert.That(OdfNameRules.IsQualifiedNameValue(Qualified(OdfNamespaces.Xml, "id", "xml")), Is.True);
            Assert.That(OdfNameRules.IsQualifiedNameValue(Qualified("", "1bad", "")), Is.False);
            Assert.That(OdfNameRules.IsQualifiedNameValue(Qualified("", "name", "p")), Is.False);
            Assert.That(OdfNameRules.IsQualifiedNameValue(Qualified(OdfNamespaces.Text, "name", "bad:prefix")), Is.False);
            Assert.That(OdfNameRules.IsQualifiedNameValue(Qualified(OdfNamespaces.Text, "name", "xmlns")), Is.False);
            Assert.That(OdfNameRules.IsQualifiedNameValue(Qualified(OdfNamespaces.Text, "lang", "xml")), Is.False);
            Assert.That(OdfNameRules.IsQualifiedNameValue(Qualified(OdfNamespaces.Xml, "lang", "other")), Is.False);
            Assert.That(OdfNameRules.IsQualifiedNameValue(null), Is.False);
        });
    }

    [Test]
    public void XmlNameDataDefaultsAreSafeAndComposeIntoNodes()
    {
        OdfQualifiedName name = new OdfQualifiedName();
        OdfNamespaceDeclaration declaration = new OdfNamespaceDeclaration();
        OdfAttribute attribute = new OdfAttribute();
        OdfNode node = new OdfNode
        {
            Name = name,
            NamespaceDeclarations = [declaration],
            Attributes = [attribute]
        };

        Assert.Multiple(() =>
        {
            Assert.That(name.NamespaceName, Is.Empty);
            Assert.That(name.LocalName, Is.Empty);
            Assert.That(name.Prefix, Is.Empty);
            Assert.That(declaration.Prefix, Is.Empty);
            Assert.That(declaration.NamespaceName, Is.Empty);
            Assert.That(declaration.Location, Is.Not.Null);
            Assert.That(attribute.Name, Is.Not.Null);
            Assert.That(attribute.Value, Is.Empty);
            Assert.That(attribute.Location, Is.Not.Null);
            Assert.That(node.Name, Is.SameAs(name));
            Assert.That(node.NamespaceDeclarations, Is.EqualTo(new[] { declaration }));
            Assert.That(node.Attributes, Is.EqualTo(new[] { attribute }));
            Assert.That(new OdfNode().NamespaceDeclarations, Is.Empty);
            Assert.That(new OdfNode().Attributes, Is.Empty);
        });
    }

    private static OdfQualifiedName Qualified(string namespaceName, string localName, string prefix)
    {
        return new OdfQualifiedName
        {
            NamespaceName = namespaceName,
            LocalName = localName,
            Prefix = prefix
        };
    }

    private static OdfNamespaceDeclaration Declaration(string prefix, string namespaceName)
    {
        return new OdfNamespaceDeclaration
        {
            Prefix = prefix,
            NamespaceName = namespaceName
        };
    }
}
