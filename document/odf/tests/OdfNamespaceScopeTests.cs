namespace odf.Tests;

public class OdfNamespaceScopeTests
{
    [Test]
    public void ChildScopesInheritAndShadowBindingsWithoutChangingParents()
    {
        OdfNamespaceScope parent = new OdfNamespaceScope(null, true);
        parent.AddDeclaration(Declaration("", OdfNamespaces.Text));
        parent.AddDeclaration(Declaration("o", OdfNamespaces.Office));
        OdfNamespaceScope child = new OdfNamespaceScope(parent, true);
        child.AddDeclaration(Declaration("o", OdfNamespaces.Table));
        child.AddDeclaration(Declaration("ext", "urn:example:extension"));
        OdfNamespaceDeclaration[] declarations = child.GetDeclarations();

        Assert.Multiple(() =>
        {
            Assert.That(parent.LookupNamespace("o"), Is.EqualTo(OdfNamespaces.Office));
            Assert.That(child.LookupNamespace("o"), Is.EqualTo(OdfNamespaces.Table));
            Assert.That(child.LookupNamespace(""), Is.EqualTo(OdfNamespaces.Text));
            Assert.That(child.LookupNamespace("ext"), Is.EqualTo("urn:example:extension"));
            Assert.That(child.LookupNamespace("xml"), Is.EqualTo(OdfNamespaces.Xml));
            Assert.That(parent.LookupNamespace("ext"), Is.Null);
            Assert.That(child.Parent, Is.SameAs(parent));
            Assert.That(declarations.Select(item => item.Prefix), Is.EqualTo(new[] { "o", "ext" }));
        });
    }

    [Test]
    public void DefaultNamespaceCanBeUndeclaredInAChildScope()
    {
        OdfNamespaceScope parent = new OdfNamespaceScope(null, true);
        parent.AddDeclaration(Declaration("", OdfNamespaces.Text));
        OdfNamespaceScope child = new OdfNamespaceScope(parent, true);
        child.AddDeclaration(Declaration("", ""));

        Assert.Multiple(() =>
        {
            Assert.That(parent.LookupNamespace(""), Is.EqualTo(OdfNamespaces.Text));
            Assert.That(child.LookupNamespace(""), Is.Empty);
            Assert.That(child.ResolveQualifiedName("p", true).NamespaceName, Is.Empty);
        });
    }

    [TestCase("xmlns", "urn:example")]
    [TestCase("xml", "urn:example")]
    [TestCase("other", "http://www.w3.org/XML/1998/namespace")]
    [TestCase("xmlData", "urn:example")]
    [TestCase("bad:prefix", "urn:example")]
    [TestCase("p", "")]
    [TestCase("p", "http://www.w3.org/2000/xmlns/")]
    public void RejectsInvalidAndReservedNamespaceDeclarations(
        string prefix,
        string namespaceName)
    {
        OdfNamespaceScope scope = new OdfNamespaceScope(null, true);

        Assert.That(
            () => scope.AddDeclaration(Declaration(prefix, namespaceName)),
            Throws.TypeOf<OdfXmlException>());
    }

    [Test]
    public void RejectsDuplicateDeclarationsWithinOneScope()
    {
        OdfNamespaceScope scope = new OdfNamespaceScope(null, true);
        scope.AddDeclaration(Declaration("p", "urn:first"));

        Assert.That(
            () => scope.AddDeclaration(Declaration("p", "urn:second")),
            Throws.TypeOf<OdfXmlException>()
                .With.Message.Contains("more than once"));
    }

    [Test]
    public void ResolvesQualifiedNamesWithElementAndAttributeNamespaceRules()
    {
        OdfNamespaceScope scope = new OdfNamespaceScope(null, true);
        scope.AddDeclaration(Declaration("", OdfNamespaces.Text));
        scope.AddDeclaration(Declaration("table", OdfNamespaces.Table));

        OdfQualifiedName elementName = scope.ResolveQualifiedName("p", true);
        OdfQualifiedName attributeName = scope.ResolveQualifiedName("style-name", false);
        OdfQualifiedName tableName = scope.ResolveQualifiedName("table:table", true);
        OdfQualifiedName languageName = scope.ResolveQualifiedName("xml:lang", false);

        Assert.Multiple(() =>
        {
            AssertName(elementName, OdfNamespaces.Text, "p", "");
            AssertName(attributeName, "", "style-name", "");
            AssertName(tableName, OdfNamespaces.Table, "table", "table");
            AssertName(languageName, OdfNamespaces.Xml, "lang", "xml");
            Assert.That(
                () => scope.ResolveQualifiedName("missing:name", true),
                Throws.TypeOf<OdfXmlException>()
                    .With.Message.Contains("unbound"));
            Assert.That(
                () => scope.ResolveQualifiedName("bad:name:again", true),
                Throws.TypeOf<OdfXmlException>()
                    .With.Message.Contains("invalid"));
        });
    }

    [Test]
    public void SelectsPreservedOrStandardPrefixesAccordingToMode()
    {
        OdfNamespaceScope preserving = new OdfNamespaceScope(null, true);
        OdfNamespaceScope standardizing = new OdfNamespaceScope(null, false);

        string preserved = preserving.SelectPrefix(OdfNamespaces.Text, "legacy", false);
        string standard = standardizing.SelectPrefix(OdfNamespaces.Text, "legacy", false);
        preserving.AddDeclaration(Declaration(preserved, OdfNamespaces.Text));

        Assert.Multiple(() =>
        {
            Assert.That(preserved, Is.EqualTo("legacy"));
            Assert.That(standard, Is.EqualTo(OdfNamespaces.TextPrefix));
            Assert.That(preserving.LookupPrefix(OdfNamespaces.Text, false), Is.EqualTo("legacy"));
            Assert.That(standardizing.SelectPrefix(OdfNamespaces.Office, "", false), Is.EqualTo(OdfNamespaces.OfficePrefix));
            Assert.That(standardizing.SelectPrefix(OdfNamespaces.Xml, "anything", false), Is.EqualTo(OdfNamespaces.XmlPrefix));
        });
    }

    [Test]
    public void AvoidsCollisionsAndGeneratesDeterministicPrefixes()
    {
        OdfNamespaceScope scope = new OdfNamespaceScope(null, false);
        scope.AddDeclaration(Declaration("text", "urn:not-text"));
        scope.AddDeclaration(Declaration("text1", "urn:also-not-text"));
        scope.AddDeclaration(Declaration("ns", "urn:first"));
        scope.AddDeclaration(Declaration("ns1", "urn:second"));

        string knownPrefix = scope.SelectPrefix(OdfNamespaces.Text, "ignored", false);
        string generatedPrefix = scope.SelectPrefix("urn:third", "bad:prefix", false);

        Assert.Multiple(() =>
        {
            Assert.That(knownPrefix, Is.EqualTo("text2"));
            Assert.That(generatedPrefix, Is.EqualTo("ns2"));
        });
    }

    [Test]
    public void PreservesAnAvailableDefaultPrefixForElementsOnly()
    {
        OdfNamespaceScope scope = new OdfNamespaceScope(null, true);

        Assert.Multiple(() =>
        {
            Assert.That(scope.SelectPrefix("urn:example", "", true), Is.Empty);
            Assert.That(scope.SelectPrefix("urn:example", "", false), Is.EqualTo("ns"));
        });
    }

    private static OdfNamespaceDeclaration Declaration(
        string prefix,
        string namespaceName)
    {
        return new OdfNamespaceDeclaration
        {
            Prefix = prefix,
            NamespaceName = namespaceName
        };
    }

    private static void AssertName(
        OdfQualifiedName name,
        string namespaceName,
        string localName,
        string prefix)
    {
        Assert.That(name.NamespaceName, Is.EqualTo(namespaceName));
        Assert.That(name.LocalName, Is.EqualTo(localName));
        Assert.That(name.Prefix, Is.EqualTo(prefix));
    }
}
