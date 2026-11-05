using System.IO;
using System.Linq;
using xml;

namespace xml.Tests;

[TestFixture]
public class ExclusiveXml10Tests
{
    [Test]
    public void RendersOnlyVisiblyUtilizedNamespacesAtTheirPointOfUse()
    {
        XmlDocument document = Parse("<root xmlns:p=\"urn:p\" xmlns:q=\"urn:q\" xmlns:unused=\"urn:unused\" z=\"2\" p:a=\"1\"><q:child/></root>");

        string result = Canonicalize(document, null, false);

        Assert.That(result, Is.EqualTo("<root xmlns:p=\"urn:p\" z=\"2\" p:a=\"1\"><q:child xmlns:q=\"urn:q\"></q:child></root>"));
    }

    [Test]
    public void ProducesThePublishedOrphanSubtreeFormWithoutImportingXmlAttributes()
    {
        XmlDocument document = Parse("<n2:pdu xmlns:n1=\"http://example.com\" xmlns:n2=\"http://foo.example\" xml:lang=\"fr\" xml:space=\"retain\"><n1:elem2 xmlns:n1=\"http://example.net\" xml:lang=\"en\"><n3:stuff xmlns:n3=\"ftp://example.org\"/></n1:elem2></n2:pdu>");
        XmlNode pdu = document.DocumentElement!;
        XmlNode elem2 = pdu.Children!.Single();
        XmlNode stuff = elem2.Children!.Single();
        XmlNode language = elem2.Attributes!.Single(node => node.Name!.LocalName == "lang");
        XmlNodeSelection selection = Selection(document,
        [
            elem2,
            Namespace(document, elem2, "n1", "http://example.net"),
            Namespace(document, elem2, "n2", "http://foo.example"),
            language,
            stuff,
            Namespace(document, stuff, "n1", "http://example.net"),
            Namespace(document, stuff, "n2", "http://foo.example"),
            Namespace(document, stuff, "n3", "ftp://example.org")
        ]);

        string result = Canonicalize(document, selection, false);

        Assert.That(result, Is.EqualTo("<n1:elem2 xmlns:n1=\"http://example.net\" xml:lang=\"en\"><n3:stuff xmlns:n3=\"ftp://example.org\"></n3:stuff></n1:elem2>"));
    }

    [Test]
    public void RendersInclusivePrefixesEvenWhenTheyAreNotVisiblyUtilized()
    {
        XmlDocument document = Parse("<n2:pdu xmlns:n1=\"http://example.com\" xmlns:n2=\"http://foo.example\"><n1:elem xmlns:n1=\"http://example.net\"/></n2:pdu>");
        XmlNode elem = document.DocumentElement!.Children!.Single();
        XmlNodeSelection selection = Selection(document,
        [
            elem,
            Namespace(document, elem, "n1", "http://example.net"),
            Namespace(document, elem, "n2", "http://foo.example")
        ]);

        string result = Canonicalize(document, selection, false, ["n2"]);

        Assert.That(result, Is.EqualTo("<n1:elem xmlns:n1=\"http://example.net\" xmlns:n2=\"http://foo.example\"></n1:elem>"));
    }

    [Test]
    public void RecognizesTheDefaultNamespacePrefixListToken()
    {
        XmlDocument document = Parse("<outer xmlns=\"urn:outer\"><p:item xmlns:p=\"urn:item\"/></outer>");
        XmlNode item = document.DocumentElement!.Children!.Single();
        XmlNodeSelection selection = Selection(document,
        [
            item,
            Namespace(document, item, "", "urn:outer"),
            Namespace(document, item, "p", "urn:item")
        ]);

        string result = Canonicalize(document, selection, false, ["#default"]);

        Assert.That(result, Is.EqualTo("<p:item xmlns=\"urn:outer\" xmlns:p=\"urn:item\"></p:item>"));
    }

    [Test]
    public void UndeclaresAnOutputAncestorDefaultNamespaceForAnOrphan()
    {
        XmlDocument document = Parse("<A xmlns=\"urn:a\"><skip><B xmlns=\"\"/></skip></A>");
        XmlNode a = document.DocumentElement!;
        XmlNode skip = a.Children!.Single();
        XmlNode b = skip.Children!.Single();
        XmlNodeSelection selection = Selection(document,
        [
            a,
            Namespace(document, a, "", "urn:a"),
            b
        ]);

        string result = Canonicalize(document, selection, false);

        Assert.That(result, Is.EqualTo("<A xmlns=\"urn:a\"><B xmlns=\"\"></B></A>"));
    }

    [Test]
    public void AppliesTheCommentSwitchToExclusiveCanonicalization()
    {
        XmlDocument document = Parse("<!--before--><root><!--inside--></root><!--after-->");

        string withoutComments = Canonicalize(document, null, false);
        string withComments = Canonicalize(document, null, true);

        Assert.Multiple(() =>
        {
            Assert.That(withoutComments, Is.EqualTo("<root></root>"));
            Assert.That(withComments, Is.EqualTo("<!--before-->\n<root><!--inside--></root>\n<!--after-->"));
        });
    }

    [Test]
    public void RejectsInvalidInclusivePrefixTokens()
    {
        XmlDocument document = Parse("<root/>");
        XmlCanonicalizationException exception = Assert.Throws<XmlCanonicalizationException>(() => Canonicalize(document, null, false, ["bad:prefix"]))!;

        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.CanonicalizationFailure));
    }

    private static string Canonicalize(XmlDocument document, XmlNodeSelection? selection, bool includeComments)
    {
        return Canonicalize(document, selection, includeComments, null);
    }

    private static string Canonicalize(XmlDocument document, XmlNodeSelection? selection, bool includeComments, string[]? inclusivePrefixes)
    {
        StringWriter writer = new StringWriter();
        XmlCanonicalizationOptions options = new XmlCanonicalizationOptions
        {
            Method = XmlCanonicalizationMethod.ExclusiveXml10,
            IncludeComments = includeComments,
            Selection = selection,
            InclusivePrefixes = inclusivePrefixes
        };
        new XmlCanonicalizer().CanonicalizeXml(document, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }, options);
        return writer.ToString();
    }

    private static XmlDocument Parse(string source)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreservePrefixes = true,
            RetainNamespaceDeclarations = true
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Kind = XmlInputKind.String, Text = source }, options, null, null, null);
    }

    private static XmlNodeSelection Selection(XmlDocument document, XmlNode[] nodes)
    {
        return new XmlNodeSelection
        {
            Document = document,
            Nodes = nodes,
            MutationVersion = document.MutationVersion
        };
    }

    private static XmlNode Namespace(XmlDocument document, XmlNode owner, string prefix, string namespaceUri)
    {
        return new XmlNode
        {
            Kind = XmlNodeKind.Namespace,
            Name = new XmlName { LocalName = prefix, QualifiedName = prefix },
            Value = namespaceUri,
            Parent = owner,
            OwnerDocument = document,
            MutationVersion = document.MutationVersion
        };
    }
}
