using iso20022;

namespace iso20022.Tests;

public class QualifiedNameLexicalTests
{
    [TestCase("Document")]
    [TestCase("pacs:Document")]
    [TestCase("名:要素")]
    [TestCase("_prefix:local-name")]
    public void QualifiedNameValidatorAcceptsOneOrTwoNcNames(string text)
    {
        Assert.That(QualifiedNameLexical.IsQualifiedName(text.AsSpan()), Is.True);
    }

    [TestCase("")]
    [TestCase(":Document")]
    [TestCase("pacs:")]
    [TestCase("a:b:c")]
    [TestCase("1bad:Document")]
    [TestCase("pacs:1bad")]
    [TestCase("pacs :Document")]
    public void QualifiedNameValidatorRejectsMalformedForms(string text)
    {
        Assert.That(QualifiedNameLexical.IsQualifiedName(text.AsSpan()), Is.False);
    }

    [Test]
    public void QualifiedNameParserSeparatesPrefixLocalNameAndNamespace()
    {
        string namespaceUri = "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10";
        QualifiedNameData? result = QualifiedNameLexical.ParseQualifiedName("pacs:Document".AsSpan(), namespaceUri.AsSpan());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result?.Prefix, Is.EqualTo("pacs"));
            Assert.That(result?.LocalName, Is.EqualTo("Document"));
            Assert.That(result?.NamespaceUri, Is.EqualTo(namespaceUri));
        });
    }

    [Test]
    public void QualifiedNameParserSupportsUnprefixedAndUnicodeNames()
    {
        QualifiedNameData? unprefixed = QualifiedNameLexical.ParseQualifiedName("Document".AsSpan(), ReadOnlySpan<char>.Empty);
        QualifiedNameData? unicode = QualifiedNameLexical.ParseQualifiedName("名:要素".AsSpan(), "relative/namespace".AsSpan());

        Assert.That(unprefixed, Is.Not.Null);
        Assert.That(unicode, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(unprefixed?.Prefix, Is.Empty);
            Assert.That(unprefixed?.NamespaceUri, Is.Empty);
            Assert.That(unicode?.Prefix, Is.EqualTo("名"));
            Assert.That(unicode?.LocalName, Is.EqualTo("要素"));
            Assert.That(unicode?.NamespaceUri, Is.EqualTo("relative/namespace"));
        });
    }

    [Test]
    public void QualifiedNameParserEnforcesReservedNamespaceBindings()
    {
        QualifiedNameData? xml = QualifiedNameLexical.ParseQualifiedName("xml:lang".AsSpan(), Iso20022Namespaces.Xml.AsSpan());

        Assert.That(xml, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(xml?.Prefix, Is.EqualTo("xml"));
            Assert.That(QualifiedNameLexical.ParseQualifiedName("xml:lang".AsSpan(), "urn:wrong".AsSpan()), Is.Null);
            Assert.That(QualifiedNameLexical.ParseQualifiedName("other:lang".AsSpan(), Iso20022Namespaces.Xml.AsSpan()), Is.Null);
            Assert.That(QualifiedNameLexical.ParseQualifiedName("xmlns:item".AsSpan(), "urn:example".AsSpan()), Is.Null);
            Assert.That(QualifiedNameLexical.ParseQualifiedName("other:item".AsSpan(), Iso20022Namespaces.Xmlns.AsSpan()), Is.Null);
        });
    }

    [Test]
    public void QualifiedNameParserRequiresNamespacesForPrefixesAndValidatesUris()
    {
        Assert.Multiple(() =>
        {
            Assert.That(QualifiedNameLexical.ParseQualifiedName("pacs:Document".AsSpan(), ReadOnlySpan<char>.Empty), Is.Null);
            Assert.That(QualifiedNameLexical.ParseQualifiedName("Document".AsSpan(), "namespace with spaces".AsSpan()), Is.Null);
            Assert.That(QualifiedNameLexical.ParseQualifiedName("a:b:c".AsSpan(), "urn:example:value".AsSpan()), Is.Null);
        });
    }
}
