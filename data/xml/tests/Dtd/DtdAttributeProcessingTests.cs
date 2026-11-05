using xml;

namespace xml.Tests;

[TestFixture]
public class DtdAttributeProcessingTests
{
    [Test]
    public void NormalizesCdataTokenizedAndEntityReplacementValues()
    {
        string subset = "<!ELEMENT root ANY>" +
            "<!ENTITY words '  alpha   beta  '>" +
            "<!ATTLIST root c CDATA #IMPLIED t NMTOKENS #IMPLIED e NMTOKENS #IMPLIED>";
        string text = "<!DOCTYPE root [" + subset + "]><root c=' a\t b &#xA; c ' t='  one\t \n two  ' e='&words;'/>";

        XmlDocument document = Parse(text, XmlValidationMode.Dtd, XmlNamespaceHandling.Disabled, null);

        XmlNode[] attributes = document.DocumentElement!.Attributes!;
        Assert.That(attributes[0].Value, Is.EqualTo(" a  b \n c "));
        Assert.That(attributes[1].Value, Is.EqualTo("one two"));
        Assert.That(attributes[2].Value, Is.EqualTo("alpha beta"));
    }

    [Test]
    public void NormalizesAndInsertsDefaultAttributeValues()
    {
        string subset = "<!ELEMENT root ANY>" +
            "<!ENTITY later 'a  b'>" +
            "<!ATTLIST root c CDATA 'before&#xA;after' tokens NMTOKENS '  one   two  ' entityValue CDATA '&later;'>";

        XmlDocument document = Parse("<!DOCTYPE root [" + subset + "]><root/>", XmlValidationMode.Dtd, XmlNamespaceHandling.Disabled, null);

        XmlNode[] attributes = document.DocumentElement!.Attributes!;
        Assert.That(attributes.Select(item => item.Name!.QualifiedName), Is.EqualTo(new string[] { "c", "tokens", "entityValue" }));
        Assert.That(attributes.Select(item => item.Value), Is.EqualTo(new string[] { "before\nafter", "one two", "a  b" }));
        Assert.That(document.DocumentType!.AttributeDeclarations![0].DefaultValue, Is.EqualTo("before\nafter"));
        Assert.That(document.DocumentType.AttributeDeclarations[1].DefaultValue, Is.EqualTo("one two"));
        Assert.That(document.DocumentType.AttributeDeclarations[2].DefaultValue, Is.EqualTo("a  b"));
    }

    [Test]
    public void MarksSpecifiedDefaultedAndFixedAttributeData()
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);
        parser.LoadDtdSubsets("<!ATTLIST root supplied NMTOKEN #FIXED 'value' fallback CDATA #FIXED 'default'>", null, null, null);
        XmlAttributeValueNormalizer normalizer = new XmlAttributeValueNormalizer(XmlVersion.Xml10);
        normalizer.ConfigureXmlAttributeEntities(parser, null, null, null);
        DtdAttributeProcessor processor = new DtdAttributeProcessor(parser, normalizer, new DtdValidationContext(null, null, false), true, false);
        XmlAttributeData supplied = Attribute("supplied", "  value  ");

        XmlAttributeData[] attributes = processor.ProcessDtdElementAttributes("root", [supplied], 0);

        Assert.That(attributes, Has.Length.EqualTo(2));
        Assert.That(attributes[0].Value, Is.EqualTo("value"));
        Assert.That(attributes[0].IsSpecified, Is.True);
        Assert.That(attributes[0].IsDefaulted, Is.False);
        Assert.That(attributes[0].IsFixed, Is.True);
        Assert.That(attributes[1].Value, Is.EqualTo("default"));
        Assert.That(attributes[1].IsSpecified, Is.False);
        Assert.That(attributes[1].IsDefaulted, Is.True);
        Assert.That(attributes[1].IsFixed, Is.True);
        Assert.That(attributes[1].Range, Is.Null);
    }

    [TestCase("<!ATTLIST root required CDATA #REQUIRED>", "<root/>")]
    [TestCase("<!ATTLIST root fixed CDATA #FIXED 'expected'>", "<root fixed='different'/>")]
    [TestCase("<!ATTLIST root mode (one|two) #IMPLIED>", "<root mode='three'/>")]
    [TestCase("<!ATTLIST root format NOTATION (png|svg) #IMPLIED>", "<root format='gif'/>")]
    [TestCase("<!ATTLIST unused mode (one|two) 'three'>", "<root/>")]
    public void EnforcesRequiredFixedAndEnumeratedAttributesDuringDtdValidation(string declaration, string root)
    {
        string text = "<!DOCTYPE root [<!ELEMENT root ANY>" + declaration + "]>" + root;

        Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, XmlNamespaceHandling.Disabled, null));
    }

    [Test]
    public void ComparesFixedAndEnumeratedValuesAfterTokenizedNormalization()
    {
        string subset = "<!ELEMENT root ANY><!ATTLIST root fixed NMTOKENS #FIXED 'one two' mode (ready|waiting) #REQUIRED>";

        XmlDocument document = Parse("<!DOCTYPE root [" + subset + "]><root fixed='  one   two  ' mode=' ready '/>", XmlValidationMode.Dtd, XmlNamespaceHandling.Disabled, null);

        Assert.That(document.DocumentElement!.Attributes![0].Value, Is.EqualTo("one two"));
        Assert.That(document.DocumentElement.Attributes[1].Value, Is.EqualTo("ready"));
    }

    [Test]
    public void AppliesDefaultsWithoutValidityChecksWhenDtdValidationIsDisabled()
    {
        string subset = "<!ELEMENT root ANY><!ATTLIST root required CDATA #REQUIRED fixed CDATA #FIXED 'expected' mode (one|two) #IMPLIED added NMTOKEN ' value '>";

        XmlDocument document = Parse("<!DOCTYPE root [" + subset + "]><root fixed='different' mode='three'/>", XmlValidationMode.None, XmlNamespaceHandling.Disabled, null);

        Assert.That(document.DocumentElement!.Attributes, Has.Length.EqualTo(3));
        Assert.That(document.DocumentElement.Attributes![0].Value, Is.EqualTo("different"));
        Assert.That(document.DocumentElement.Attributes[1].Value, Is.EqualTo("three"));
        Assert.That(document.DocumentElement.Attributes[2].Value, Is.EqualTo("value"));
    }

    [Test]
    public void EnforcesStandaloneAttributeConstraintsFromExternalDeclarations()
    {
        AttributeSubsetResolver resolver = new AttributeSubsetResolver();
        resolver.Text = "<!ELEMENT root ANY><!ATTLIST root added CDATA 'external'>";
        string emptyRoot = "<?xml version='1.0' standalone='yes'?><!DOCTYPE root SYSTEM 'root.dtd'><root/>";

        Assert.Throws<XmlValidationException>(() => Parse(emptyRoot, XmlValidationMode.Dtd, XmlNamespaceHandling.Disabled, resolver));

        resolver.Text = "<!ELEMENT root ANY><!ATTLIST root tokens NMTOKENS #IMPLIED>";
        string changed = "<?xml version='1.0' standalone='yes'?><!DOCTYPE root SYSTEM 'root.dtd'><root tokens=' one  two '/>";
        string unchanged = "<?xml version='1.0' standalone='yes'?><!DOCTYPE root SYSTEM 'root.dtd'><root tokens='one two'/>";
        Assert.Throws<XmlValidationException>(() => Parse(changed, XmlValidationMode.Dtd, XmlNamespaceHandling.Disabled, resolver));
        Assert.That(Parse(unchanged, XmlValidationMode.Dtd, XmlNamespaceHandling.Disabled, resolver).DocumentElement, Is.Not.Null);
    }

    [Test]
    public void GivesInternalAttributeDeclarationsPrecedenceForStandaloneDocuments()
    {
        AttributeSubsetResolver resolver = new AttributeSubsetResolver();
        resolver.Text = "<!ELEMENT root ANY><!ATTLIST root added CDATA 'external'>";
        string text = "<?xml version='1.0' standalone='yes'?><!DOCTYPE root SYSTEM 'root.dtd' [<!ATTLIST root added CDATA 'internal'>]><root/>";

        XmlDocument document = Parse(text, XmlValidationMode.Dtd, XmlNamespaceHandling.Disabled, resolver);

        Assert.That(document.DocumentElement!.Attributes![0].Value, Is.EqualTo("internal"));
    }

    [Test]
    public void TreatsDeclarationsInsideInternalParameterEntitiesAsExternalForStandalone()
    {
        string text = "<?xml version='1.0' standalone='yes'?><!DOCTYPE root [<!ELEMENT root ANY><!ENTITY % attributes \"<!ATTLIST root added CDATA 'value'>\">%attributes;]><root/>";

        Assert.Throws<XmlValidationException>(() => Parse(text, XmlValidationMode.Dtd, XmlNamespaceHandling.Disabled, null));
    }

    [Test]
    public void AppliesDefaultedNamespaceDeclarationsBeforeNameResolution()
    {
        string subset = "<!ELEMENT p:root ANY><!ATTLIST p:root xmlns:p CDATA 'urn:p' p:value NMTOKEN ' item '>";

        XmlDocument document = Parse("<!DOCTYPE p:root [" + subset + "]><p:root/>", XmlValidationMode.Dtd, XmlNamespaceHandling.Namespaces10, null);

        Assert.That(document.DocumentElement!.Name!.NamespaceUri, Is.EqualTo("urn:p"));
        Assert.That(document.DocumentElement.Attributes, Has.Length.EqualTo(1));
        Assert.That(document.DocumentElement.Attributes![0].Name!.NamespaceUri, Is.EqualTo("urn:p"));
        Assert.That(document.DocumentElement.Attributes[0].Value, Is.EqualTo("item"));
    }

    [Test]
    public void RejectsMarkupAndExternalEntitiesInAttributeReplacementText()
    {
        string markup = "<!DOCTYPE root [<!ELEMENT root ANY><!ENTITY bad '<child/>'>]><root value='&bad;'/>";
        Assert.Throws<XmlWellFormednessException>(() => Parse(markup, XmlValidationMode.None, XmlNamespaceHandling.Disabled, null));

        AttributeSubsetResolver resolver = new AttributeSubsetResolver();
        resolver.Text = "external";
        string external = "<!DOCTYPE root [<!ELEMENT root ANY><!ENTITY bad SYSTEM 'bad.ent'>]><root value='&bad;'/>";
        Assert.Throws<XmlWellFormednessException>(() => Parse(external, XmlValidationMode.None, XmlNamespaceHandling.Disabled, resolver));
    }

    private static XmlAttributeData Attribute(string name, string rawValue)
    {
        XmlName attributeName = new XmlName();
        attributeName.LocalName = name;
        attributeName.QualifiedName = name;
        XmlAttributeData attribute = new XmlAttributeData();
        attribute.Name = attributeName;
        attribute.Value = rawValue;
        attribute.RawValue = rawValue;
        attribute.Quote = '\'';
        attribute.IsSpecified = true;
        return attribute;
    }

    private static XmlDocument Parse(string text, XmlValidationMode validationMode, XmlNamespaceHandling namespaceHandling, IXmlResourceResolver? resolver)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = text;
        input.BaseUri = "urn:document";
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = namespaceHandling;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = validationMode;
        options.ResolverPolicy = AllowedPolicy();
        return new XmlParser().ParseXmlDocument(input, options, resolver, null, null);
    }

    private static XmlResolverPolicy AllowedPolicy()
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Allowed;
        return policy;
    }

    private sealed class AttributeSubsetResolver : IXmlResourceResolver
    {
        public string Text = "";

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            XmlInput input = new XmlInput();
            input.Kind = XmlInputKind.String;
            input.Text = Text;
            XmlResolvedResource resource = new XmlResolvedResource();
            resource.Kind = kind;
            resource.Access = XmlResourceAccess.Allowed;
            resource.Input = input;
            resource.AbsoluteUri = kind == XmlResourceKind.ExternalSubset ? "urn:root.dtd" : "urn:bad.ent";
            return resource;
        }
    }
}
