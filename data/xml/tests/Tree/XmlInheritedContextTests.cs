using System;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlInheritedContextTests
{
    [Test]
    public void ResolvesBaseLanguageAndSpaceAcrossOverridesResetsAndAttributes()
    {
        XmlParserOptions options = Options();
        XmlDocument document = Parse("<root xml:base='parts/root.xml' xml:lang='en' xml:space='preserve'><child xml:base='../inner/' xml:lang=''><leaf xml:base='' xml:space='invalid'/></child><tail/></root>", options, "https://example.test/base/document.xml");
        XmlNode root = document.DocumentElement!;
        XmlNode child = root.Children![0];
        XmlNode leaf = child.Children![0];
        XmlNode tail = root.Children[1];
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        Assert.That(navigator.GetXmlNodeBaseUri(document.DocumentNode!), Is.EqualTo("https://example.test/base/document.xml"));
        Assert.That(navigator.GetXmlNodeBaseUri(root), Is.EqualTo("https://example.test/base/parts/root.xml"));
        Assert.That(navigator.GetXmlNodeBaseUri(child), Is.EqualTo("https://example.test/base/inner/"));
        Assert.That(navigator.GetXmlNodeBaseUri(leaf), Is.EqualTo("https://example.test/base/inner/"));
        Assert.That(navigator.GetXmlNodeBaseUri(leaf.Attributes![0]), Is.EqualTo("https://example.test/base/inner/"));
        Assert.That(navigator.GetXmlNodeBaseUri(tail), Is.EqualTo("https://example.test/base/parts/root.xml"));
        Assert.That(navigator.GetXmlNodeLanguage(root), Is.EqualTo("en"));
        Assert.That(navigator.GetXmlNodeLanguage(child), Is.Null);
        Assert.That(navigator.GetXmlNodeLanguage(leaf), Is.Null);
        Assert.That(navigator.GetXmlNodeLanguage(tail), Is.EqualTo("en"));
        Assert.That(navigator.GetXmlNodeSpaceMode(root), Is.EqualTo(XmlSpaceMode.Preserve));
        Assert.That(navigator.GetXmlNodeSpaceMode(child), Is.EqualTo(XmlSpaceMode.Preserve));
        Assert.That(navigator.GetXmlNodeSpaceMode(leaf), Is.EqualTo(XmlSpaceMode.Preserve));
        Assert.That(navigator.GetXmlNodeSpaceMode(tail), Is.EqualTo(XmlSpaceMode.Preserve));
    }

    [Test]
    public void ReusesACompleteContextCacheEntryUntilTheDocumentMutates()
    {
        XmlParserOptions options = Options();
        XmlDocument document = Parse("<root xml:base='old/' xml:lang='en' xml:space='preserve'><leaf/></root>", options, "https://example.test/document.xml");
        XmlNode root = document.DocumentElement!;
        XmlNode leaf = root.Children![0];
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        Assert.That(navigator.GetXmlNodeBaseUri(leaf), Is.EqualTo("https://example.test/old/"));
        Assert.That(navigator.GetXmlNodeLanguage(leaf), Is.EqualTo("en"));
        Assert.That(navigator.GetXmlNodeSpaceMode(leaf), Is.EqualTo(XmlSpaceMode.Preserve));
        Assert.That(navigator.Navigation.ContextCache.Count, Is.EqualTo(1));
        TreeContextCacheEntry original = navigator.Navigation.ContextCache[leaf];
        Assert.That(original.BaseUriComputed, Is.True);
        Assert.That(original.LanguageComputed, Is.True);
        Assert.That(original.SpaceModeComputed, Is.True);
        Assert.That(original.MutationVersion, Is.EqualTo(document.MutationVersion));

        Assert.That(navigator.GetXmlNodeLanguage(leaf), Is.EqualTo("en"));
        Assert.That(navigator.Navigation.ContextCache[leaf], Is.SameAs(original));

        XmlTreeEditor editor = new XmlTreeEditor();
        editor.SetXmlTreeText(FindAttribute(root, "base"), "new/");
        editor.SetXmlTreeText(FindAttribute(root, "lang"), "fr");
        editor.SetXmlTreeText(FindAttribute(root, "space"), "default");

        Assert.That(navigator.GetXmlNodeLanguage(leaf), Is.EqualTo("fr"));
        TreeContextCacheEntry refreshed = navigator.Navigation.ContextCache[leaf];
        Assert.That(refreshed, Is.Not.SameAs(original));
        Assert.That(refreshed.MutationVersion, Is.EqualTo(document.MutationVersion));
        Assert.That(refreshed.LanguageComputed, Is.True);
        Assert.That(refreshed.BaseUriComputed, Is.False);
        Assert.That(refreshed.SpaceModeComputed, Is.False);
        Assert.That(navigator.GetXmlNodeBaseUri(leaf), Is.EqualTo("https://example.test/new/"));
        Assert.That(navigator.GetXmlNodeSpaceMode(leaf), Is.EqualTo(XmlSpaceMode.Default));
    }

    [Test]
    public void InvalidatesCachedContextWhenANodeMovesBetweenInheritedScopes()
    {
        XmlParserOptions options = Options();
        XmlDocument document = Parse("<root><left xml:base='left/' xml:lang='en' xml:space='preserve'><item/></left><right xml:base='right/' xml:lang='fr' xml:space='default'/></root>", options, "https://example.test/document.xml");
        XmlNode root = document.DocumentElement!;
        XmlNode left = root.Children![0];
        XmlNode right = root.Children[1];
        XmlNode item = left.Children![0];
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        Assert.That(navigator.GetXmlNodeBaseUri(item), Is.EqualTo("https://example.test/left/"));
        Assert.That(navigator.GetXmlNodeLanguage(item), Is.EqualTo("en"));
        Assert.That(navigator.GetXmlNodeSpaceMode(item), Is.EqualTo(XmlSpaceMode.Preserve));
        TreeContextCacheEntry original = navigator.Navigation.ContextCache[item];

        new XmlTreeEditor().AppendXmlTreeChild(right, item);

        Assert.That(navigator.GetXmlNodeBaseUri(item), Is.EqualTo("https://example.test/right/"));
        Assert.That(navigator.GetXmlNodeLanguage(item), Is.EqualTo("fr"));
        Assert.That(navigator.GetXmlNodeSpaceMode(item), Is.EqualTo(XmlSpaceMode.Default));
        Assert.That(navigator.Navigation.ContextCache[item], Is.Not.SameAs(original));
    }

    [Test]
    public void RemovingOverridesRevealsAncestorContextAndInvalidatesTheCache()
    {
        XmlParserOptions options = Options();
        XmlDocument document = Parse("<root xml:base='root/' xml:lang='en' xml:space='preserve'><child xml:base='child/' xml:lang='fr' xml:space='default'><leaf/></child></root>", options, "https://example.test/document.xml");
        XmlNode root = document.DocumentElement!;
        XmlNode child = root.Children![0];
        XmlNode leaf = child.Children![0];
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        Assert.That(navigator.GetXmlNodeBaseUri(leaf), Is.EqualTo("https://example.test/root/child/"));
        Assert.That(navigator.GetXmlNodeLanguage(leaf), Is.EqualTo("fr"));
        Assert.That(navigator.GetXmlNodeSpaceMode(leaf), Is.EqualTo(XmlSpaceMode.Default));
        TreeContextCacheEntry original = navigator.Navigation.ContextCache[leaf];
        XmlTreeEditor editor = new XmlTreeEditor();

        editor.RemoveXmlTreeAttribute(child, XmlName("base"));
        editor.RemoveXmlTreeAttribute(child, XmlName("lang"));
        editor.RemoveXmlTreeAttribute(child, XmlName("space"));

        Assert.That(navigator.GetXmlNodeBaseUri(leaf), Is.EqualTo("https://example.test/root/"));
        Assert.That(navigator.GetXmlNodeLanguage(leaf), Is.EqualTo("en"));
        Assert.That(navigator.GetXmlNodeSpaceMode(leaf), Is.EqualTo(XmlSpaceMode.Preserve));
        Assert.That(navigator.Navigation.ContextCache[leaf], Is.Not.SameAs(original));
    }

    [Test]
    public void KeepsContextKindsIndependentWhenAnXmlBaseValueIsInvalid()
    {
        XmlParserOptions options = Options();
        XmlDocument document = Parse("<root xml:base='%invalid' xml:lang='en' xml:space='preserve'><leaf/></root>", options, "https://example.test/document.xml");
        XmlNode leaf = document.DocumentElement!.Children![0];
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        Assert.That(navigator.GetXmlNodeLanguage(leaf), Is.EqualTo("en"));
        Assert.That(navigator.GetXmlNodeSpaceMode(leaf), Is.EqualTo(XmlSpaceMode.Preserve));
        Assert.Throws<UriFormatException>(() => navigator.GetXmlNodeBaseUri(leaf));
        TreeContextCacheEntry entry = navigator.Navigation.ContextCache[leaf];
        Assert.That(entry.LanguageComputed, Is.True);
        Assert.That(entry.SpaceModeComputed, Is.True);
        Assert.That(entry.BaseUriComputed, Is.False);
    }

    [Test]
    public void RejectsParentCyclesForEveryInheritedContextLookup()
    {
        XmlDocument document = Parse("<root><child/></root>", Options(), "https://example.test/document.xml");
        XmlNode root = document.DocumentElement!;
        XmlNode child = root.Children![0];
        child.Parent = child;
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        Assert.Throws<ArgumentException>(() => navigator.GetXmlNodeBaseUri(child));
        Assert.Throws<ArgumentException>(() => navigator.GetXmlNodeLanguage(child));
        Assert.Throws<ArgumentException>(() => navigator.GetXmlNodeSpaceMode(child));
    }

    private static XmlNode FindAttribute(XmlNode element, string localName)
    {
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? name = attributes[index].Name;
            if (name != null && string.Equals(name.LocalName, localName, StringComparison.Ordinal) && string.Equals(name.NamespaceUri, XmlNamespaces.Xml, StringComparison.Ordinal))
            {
                return attributes[index];
            }

            index = index + 1;
        }

        throw new InvalidOperationException("The expected XML context attribute was not found.");
    }

    private static XmlName XmlName(string localName)
    {
        return new XmlName
        {
            Prefix = "xml",
            LocalName = localName,
            NamespaceUri = XmlNamespaces.Xml,
            QualifiedName = "xml:" + localName
        };
    }

    private static XmlDocument Parse(string text, XmlParserOptions options, string baseUri)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = text, BaseUri = baseUri };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }

    private static XmlParserOptions Options()
    {
        return new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreservePrefixes = true
        };
    }
}
