using System.Text;
using System.Xml;

namespace odf.Tests;

public class XmlCanonicalizerTests
{
    [Test]
    public void CanonicalXmlSortsNamespacesAndAttributesAndNormalizesMarkup()
    {
        string xml = "<?xml version='1.0'?><?before keep?>"
            + "<doc xmlns:b='urn:b' xmlns:a='urn:a'>"
            + "<e b:z='2' plain='&quot;&#x9;&#xA;&#xD;&amp;&lt;' a:z='1'>"
            + "x<![CDATA[>&<]]><!--inside--></e><empty/></doc><!--after-->";

        string canonical = Canonicalize(xml, OdfAlgorithmIdentifiers.CanonicalXml10, "");
        string commented = Canonicalize(
            xml,
            OdfAlgorithmIdentifiers.CanonicalXml10WithComments,
            "");

        Assert.Multiple(() =>
        {
            Assert.That(
                canonical,
                Is.EqualTo("<?before keep?>\n"
                    + "<doc xmlns:a=\"urn:a\" xmlns:b=\"urn:b\">"
                    + "<e plain=\"&quot;&#x9;&#xA;&#xD;&amp;&lt;\" a:z=\"1\" b:z=\"2\">"
                    + "x&gt;&amp;&lt;</e><empty></empty></doc>"));
            Assert.That(
                commented,
                Is.EqualTo("<?before keep?>\n"
                    + "<doc xmlns:a=\"urn:a\" xmlns:b=\"urn:b\">"
                    + "<e plain=\"&quot;&#x9;&#xA;&#xD;&amp;&lt;\" a:z=\"1\" b:z=\"2\">"
                    + "x&gt;&amp;&lt;<!--inside--></e><empty></empty></doc>\n<!--after-->"));
        });
    }

    [Test]
    public void InclusiveCanonicalXmlEliminatesOnlySuperfluousDeclarations()
    {
        string xml = "<doc>\n"
            + "<e5 a:attr='out' b:attr='sorted' attr2='all' attr=\"I'm\""
            + " xmlns:b='http://www.ietf.org' xmlns:a='http://www.w3.org'"
            + " xmlns='http://example.org'/>\n"
            + "<e6 xmlns='' xmlns:a='http://www.w3.org'>"
            + "<e7 xmlns='http://www.ietf.org'>"
            + "<e8 xmlns='' xmlns:a='http://www.w3.org'>"
            + "<e9 xmlns='' xmlns:a='http://www.ietf.org'/>"
            + "</e8></e7></e6>\n</doc>";

        string canonical = Canonicalize(xml, OdfAlgorithmIdentifiers.CanonicalXml10, "");

        Assert.That(
            canonical,
            Is.EqualTo("<doc>\n"
                + "<e5 xmlns=\"http://example.org\" xmlns:a=\"http://www.w3.org\""
                + " xmlns:b=\"http://www.ietf.org\" attr=\"I'm\" attr2=\"all\""
                + " b:attr=\"sorted\" a:attr=\"out\"></e5>\n"
                + "<e6 xmlns:a=\"http://www.w3.org\">"
                + "<e7 xmlns=\"http://www.ietf.org\">"
                + "<e8 xmlns=\"\">"
                + "<e9 xmlns:a=\"http://www.ietf.org\"></e9>"
                + "</e8></e7></e6>\n</doc>"));
    }

    [Test]
    public void ExclusiveCanonicalXmlUsesOnlyVisibleAndExplicitlyIncludedPrefixes()
    {
        XmlDocument document = LoadDocument(
            "<n2:pdu xmlns:n1='http://example.com' xmlns:n2='http://foo.example'"
            + " xml:lang='fr' xml:space='preserve'>"
            + "<n1:elem2 xmlns:n1='http://example.net' xml:lang='en'>"
            + "<n3:stuff xmlns:n3='ftp://example.org'/></n1:elem2>"
            + "</n2:pdu>");
        XmlElement element = (XmlElement)document.DocumentElement!.FirstChild!;

        string exclusive = CanonicalizeElement(
            element,
            OdfAlgorithmIdentifiers.ExclusiveCanonicalXml10,
            "");
        string withIncludedPrefix = CanonicalizeElement(
            element,
            OdfAlgorithmIdentifiers.ExclusiveCanonicalXml10,
            "n2");
        string inclusive = CanonicalizeElement(
            element,
            OdfAlgorithmIdentifiers.CanonicalXml10,
            "");

        Assert.Multiple(() =>
        {
            Assert.That(
                exclusive,
                Is.EqualTo("<n1:elem2 xmlns:n1=\"http://example.net\" xml:lang=\"en\">"
                    + "<n3:stuff xmlns:n3=\"ftp://example.org\"></n3:stuff>"
                    + "</n1:elem2>"));
            Assert.That(
                withIncludedPrefix,
                Is.EqualTo("<n1:elem2 xmlns:n1=\"http://example.net\""
                    + " xmlns:n2=\"http://foo.example\" xml:lang=\"en\">"
                    + "<n3:stuff xmlns:n3=\"ftp://example.org\"></n3:stuff>"
                    + "</n1:elem2>"));
            Assert.That(
                inclusive,
                Is.EqualTo("<n1:elem2 xmlns:n1=\"http://example.net\""
                    + " xmlns:n2=\"http://foo.example\" xml:lang=\"en\""
                    + " xml:space=\"preserve\">"
                    + "<n3:stuff xmlns:n3=\"ftp://example.org\"></n3:stuff>"
                    + "</n1:elem2>"));
        });
    }

    [Test]
    public void CanonicalXmlElevenFixesBaseAndDoesNotInheritXmlId()
    {
        XmlDocument document = LoadDocument(
            "<root xmlns:u='urn:u' xml:base='something/else' xml:id='outer'"
            + " xml:space='preserve'><middle xml:base='bar/'>"
            + "<leaf xml:base='foo'/></middle></root>");
        XmlElement middle = (XmlElement)document.DocumentElement!.FirstChild!;
        XmlElement leaf = (XmlElement)middle.FirstChild!;

        string version10 = CanonicalizeElement(
            leaf,
            OdfAlgorithmIdentifiers.CanonicalXml10,
            "");
        string version11 = CanonicalizeElement(
            leaf,
            OdfAlgorithmIdentifiers.CanonicalXml11,
            "");

        Assert.Multiple(() =>
        {
            Assert.That(
                version10,
                Is.EqualTo("<leaf xmlns:u=\"urn:u\" xml:base=\"foo\""
                    + " xml:id=\"outer\" xml:space=\"preserve\"></leaf>"));
            Assert.That(
                version11,
                Is.EqualTo("<leaf xmlns:u=\"urn:u\" xml:base=\"something/bar/foo\""
                    + " xml:space=\"preserve\"></leaf>"));
        });
    }

    [Test]
    public void CanonicalXmlElevenPreservesLeadingParentSegmentsDuringBaseFixup()
    {
        XmlDocument document = LoadDocument(
            "<outer xml:base='..'><middle xml:base='..'><leaf/></middle></outer>");
        XmlElement middle = (XmlElement)document.DocumentElement!.FirstChild!;
        XmlElement leaf = (XmlElement)middle.FirstChild!;

        string canonical = CanonicalizeElement(
            leaf,
            OdfAlgorithmIdentifiers.CanonicalXml11,
            "");

        Assert.That(canonical, Is.EqualTo("<leaf xml:base=\"../../\"></leaf>"));
    }

    [TestCase(OdfAlgorithmIdentifiers.CanonicalXml10, false)]
    [TestCase(OdfAlgorithmIdentifiers.CanonicalXml10WithComments, true)]
    [TestCase(OdfAlgorithmIdentifiers.CanonicalXml11, false)]
    [TestCase(OdfAlgorithmIdentifiers.CanonicalXml11WithComments, true)]
    [TestCase(OdfAlgorithmIdentifiers.ExclusiveCanonicalXml10, false)]
    [TestCase(OdfAlgorithmIdentifiers.ExclusiveCanonicalXml10WithComments, true)]
    public void SupportsEveryDeclaredSignatureCanonicalizationMode(
        string algorithm,
        bool includesComments)
    {
        string canonical = Canonicalize("<r><!--kept--><e/></r>", algorithm, "");

        Assert.That(canonical.Contains("<!--kept-->", StringComparison.Ordinal), Is.EqualTo(includesComments));
        Assert.That(canonical, Does.Contain("<e></e>"));
    }

    [Test]
    public void EnforcesInputTreeAndOutputBounds()
    {
        OdfSecurityLimits inputLimits = CreateLimits();
        inputLimits.MaximumSingleEntryBytes = 3;
        OdfSecurityLimits depthLimits = CreateLimits();
        depthLimits.MaximumXmlDepth = 1;
        OdfSecurityLimits attributeLimits = CreateLimits();
        attributeLimits.MaximumXmlAttributesPerElement = 0;
        OdfSecurityLimits textLimits = CreateLimits();
        textLimits.MaximumXmlTextLength = 3;
        OdfSecurityLimits outputLimits = CreateLimits();
        outputLimits.MaximumSingleEntryBytes = 5;
        XmlDocument subtreeDocument = LoadDocument("<r><parent><child/></parent></r>");
        XmlElement subtreeParent = (XmlElement)subtreeDocument.DocumentElement!.FirstChild!;
        XmlElement subtreeChild = (XmlElement)subtreeParent.FirstChild!;

        Assert.Multiple(() =>
        {
            Assert.Throws<OdfSecurityException>(() => CanonicalizeWithLimits("<r/>", inputLimits));
            Assert.Throws<OdfSecurityException>(() => CanonicalizeWithLimits("<r><e/></r>", depthLimits));
            Assert.Throws<OdfSecurityException>(() => CanonicalizeWithLimits("<r a='1'/>", attributeLimits));
            Assert.Throws<OdfSecurityException>(() => CanonicalizeWithLimits("<r>text</r>", textLimits));
            Assert.Throws<OdfSecurityException>(() => CanonicalizeWithLimits("<r/>", outputLimits));
            Assert.Throws<OdfSecurityException>(() => XmlCanonicalizer.CanonicalizeElement(
                subtreeChild,
                OdfAlgorithmIdentifiers.CanonicalXml10,
                "",
                depthLimits));
        });
    }

    [Test]
    public void RejectsDtdsRelativeNamespacesAndUnknownAlgorithms()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<XmlException>(() => Canonicalize(
                "<!DOCTYPE r [<!ENTITY x 'value'>]><r>&x;</r>",
                OdfAlgorithmIdentifiers.CanonicalXml10,
                ""));
            Assert.Throws<OdfSecurityException>(() => Canonicalize(
                "<r xmlns:p='relative'><p:e/></r>",
                OdfAlgorithmIdentifiers.CanonicalXml10,
                ""));
            Assert.Throws<OdfSecurityException>(() => Canonicalize(
                "<r/>",
                "urn:example:unsupported-canonicalization",
                ""));
            Assert.Throws<OdfSecurityException>(() => Canonicalize(
                "<r/>",
                OdfAlgorithmIdentifiers.ExclusiveCanonicalXml10,
                "not:a:prefix"));
        });
    }

    static string Canonicalize(string xml, string algorithm, string inclusivePrefixes)
    {
        return Encoding.UTF8.GetString(XmlCanonicalizer.CanonicalizeBytes(
            Encoding.UTF8.GetBytes(xml),
            algorithm,
            inclusivePrefixes,
            CreateLimits()));
    }

    static string CanonicalizeElement(
        XmlElement element,
        string algorithm,
        string inclusivePrefixes)
    {
        return Encoding.UTF8.GetString(XmlCanonicalizer.CanonicalizeElement(
            element,
            algorithm,
            inclusivePrefixes,
            CreateLimits()));
    }

    static string CanonicalizeWithLimits(string xml, OdfSecurityLimits limits)
    {
        return Encoding.UTF8.GetString(XmlCanonicalizer.CanonicalizeBytes(
            Encoding.UTF8.GetBytes(xml),
            OdfAlgorithmIdentifiers.CanonicalXml10,
            "",
            limits));
    }

    static XmlDocument LoadDocument(string xml)
    {
        XmlDocument result = new XmlDocument();
        result.PreserveWhitespace = true;
        result.LoadXml(xml);
        return result;
    }

    static OdfSecurityLimits CreateLimits()
    {
        OdfSecurityLimits result = new OdfSecurityLimits();
        result.MaximumSingleEntryBytes = 1048576;
        result.MaximumXmlDepth = 32;
        result.MaximumXmlAttributesPerElement = 32;
        result.MaximumXmlTextLength = 1048576;
        return result;
    }
}
