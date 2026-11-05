using xml;

namespace xml.Tests;

[TestFixture]
public class XmlSchemaIdentityConstraintValidatorTests
{
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void EnforcesKeysUsingTypedValueEquality()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:complexType name='Item'><xs:attribute name='id' type='xs:decimal'/></xs:complexType>" +
            "<xs:element name='root'><xs:complexType><xs:sequence><xs:element name='item' type='t:Item' minOccurs='0' maxOccurs='unbounded'/></xs:sequence></xs:complexType>" +
            "<xs:key name='itemKey'><xs:selector xpath='t:item'/><xs:field xpath='@id'/></xs:key></xs:element>");

        XmlSchemaAssessmentResult valid = Assess(compiled, "<root xmlns='urn:test'><item id='1.0'/><item id='2'/></root>");
        XmlSchemaAssessmentResult duplicate = Assess(compiled, "<root xmlns='urn:test'><item id='1.0'/><item id='01.00'/></root>");
        XmlSchemaAssessmentResult missing = Assess(compiled, "<root xmlns='urn:test'><item/></root>");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(duplicate.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(duplicate.Diagnostics.Any(item => item.Message!.Contains("duplicate", StringComparison.Ordinal)), Is.True);
            Assert.That(missing.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(missing.Diagnostics.Any(item => item.Message!.Contains("missing", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void AllowsIncompleteUniqueTuplesButRejectsDuplicateCompleteTuples()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:complexType name='Item'><xs:attribute name='a' type='xs:string'/><xs:attribute name='b' type='xs:int'/></xs:complexType>" +
            "<xs:element name='root'><xs:complexType><xs:sequence><xs:element name='item' type='t:Item' minOccurs='0' maxOccurs='unbounded'/></xs:sequence></xs:complexType>" +
            "<xs:unique name='pair'><xs:selector xpath='t:item'/><xs:field xpath='@a'/><xs:field xpath='@b'/></xs:unique></xs:element>");

        XmlSchemaAssessmentResult incomplete = Assess(compiled, "<root xmlns='urn:test'><item a='x'/><item a='x'/></root>");
        XmlSchemaAssessmentResult duplicate = Assess(compiled, "<root xmlns='urn:test'><item a='x' b='1'/><item a='x' b='01'/></root>");

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(duplicate.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
        });
    }

    [Test]
    public void ResolvesForwardKeyReferencesAfterAllValuesAreCollected()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:complexType name='Item'><xs:attribute name='id' type='xs:int'/></xs:complexType>" +
            "<xs:complexType name='Reference'><xs:attribute name='item' type='xs:int'/></xs:complexType>" +
            "<xs:element name='root'><xs:complexType><xs:choice minOccurs='0' maxOccurs='unbounded'><xs:element name='item' type='t:Item'/><xs:element name='reference' type='t:Reference'/></xs:choice></xs:complexType>" +
            "<xs:keyref name='itemReference' refer='t:itemKey'><xs:selector xpath='t:reference'/><xs:field xpath='@item'/></xs:keyref>" +
            "<xs:key name='itemKey'><xs:selector xpath='t:item'/><xs:field xpath='@id'/></xs:key></xs:element>");

        XmlSchemaAssessmentResult valid = Assess(compiled, "<root xmlns='urn:test'><reference item='02'/><item id='2'/></root>");
        XmlSchemaAssessmentResult missing = Assess(compiled, "<root xmlns='urn:test'><reference item='3'/><item id='2'/></root>");
        XmlSchemaAssessmentResult incomplete = Assess(compiled, "<root xmlns='urn:test'><reference/><item id='2'/></root>");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(missing.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(missing.Diagnostics.Any(item => item.Message!.Contains("does not match", StringComparison.Ordinal)), Is.True);
            Assert.That(incomplete.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
        });
    }

    [Test]
    public void PropagatesNestedKeyTablesAndRemovesConflictingEntries()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:complexType name='Pointer'><xs:attribute name='item' type='xs:int'/></xs:complexType>" +
            "<xs:element name='root'><xs:complexType><xs:sequence><xs:element ref='t:group' minOccurs='0' maxOccurs='unbounded'/><xs:element name='pointer' type='t:Pointer' minOccurs='0' maxOccurs='unbounded'/></xs:sequence></xs:complexType>" +
            "<xs:keyref name='nestedReference' refer='t:nestedKey'><xs:selector xpath='t:pointer'/><xs:field xpath='@item'/></xs:keyref></xs:element>" +
            "<xs:element name='group'><xs:complexType><xs:sequence><xs:element name='item' minOccurs='0' maxOccurs='unbounded'><xs:complexType><xs:attribute name='id' type='xs:int'/></xs:complexType></xs:element></xs:sequence></xs:complexType>" +
            "<xs:key name='nestedKey'><xs:selector xpath='t:item'/><xs:field xpath='@id'/></xs:key></xs:element>");

        XmlSchemaAssessmentResult valid = Assess(compiled, "<root xmlns='urn:test'><group><item id='1'/></group><group><item id='2'/></group><pointer item='2'/></root>");
        XmlSchemaAssessmentResult conflict = Assess(compiled, "<root xmlns='urn:test'><group><item id='1'/></group><group><item id='01'/></group><pointer item='1'/></root>");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(conflict.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(conflict.Diagnostics.Any(item => item.Message!.Contains("does not match", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void SupportsDescendantUnionWildcardAndDefaultNamespacePaths()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:element name='root'><xs:complexType><xs:sequence><xs:element name='box' minOccurs='0' maxOccurs='unbounded'><xs:complexType><xs:sequence><xs:element name='item' minOccurs='0' maxOccurs='unbounded'><xs:complexType><xs:sequence><xs:element name='value' type='xs:int'/></xs:sequence><xs:attribute name='id' type='xs:int'/></xs:complexType></xs:element></xs:sequence><xs:attribute name='id' type='xs:int'/></xs:complexType></xs:element></xs:sequence></xs:complexType>" +
            "<xs:unique name='deepValue'><xs:selector xpath='.// item | t:missing' xpathDefaultNamespace='##targetNamespace'/><xs:field xpath='value' xpathDefaultNamespace='##targetNamespace'/></xs:unique>" +
            "<xs:unique name='allIds'><xs:selector xpath='.//t:*'/><xs:field xpath='@id'/></xs:unique></xs:element>");

        XmlSchemaAssessmentResult valid = Assess(compiled, "<root xmlns='urn:test'><box id='1'><item id='2'><value>1</value></item><item id='3'><value>2</value></item></box></root>");
        XmlSchemaAssessmentResult duplicate = Assess(compiled, "<root xmlns='urn:test'><box id='1'><item id='2'><value>1</value></item><item id='3'><value>01</value></item></box></root>");
        XmlSchemaAssessmentResult wildcardDuplicate = Assess(compiled, "<root xmlns='urn:test'><box id='1'><item id='01'><value>1</value></item></box></root>");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(duplicate.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(wildcardDuplicate.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(compiled.IdentityConstraints[0].CompiledSelector!.Paths, Has.Length.EqualTo(2));
            Assert.That(compiled.IdentityConstraints[0].CompiledSelector!.Paths[0].DescendantStart, Is.True);
        });
    }

    [Test]
    public void RejectsFieldsThatSelectMoreThanOneNode()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:element name='root'><xs:complexType><xs:sequence><xs:element name='row'><xs:complexType><xs:sequence><xs:element name='value' type='xs:int' maxOccurs='unbounded'/></xs:sequence></xs:complexType></xs:element></xs:sequence></xs:complexType>" +
            "<xs:unique name='oneValue'><xs:selector xpath='t:row'/><xs:field xpath='t:value'/></xs:unique></xs:element>");

        XmlSchemaAssessmentResult result = Assess(compiled, "<root xmlns='urn:test'><row><value>1</value><value>2</value></row></root>");

        Assert.Multiple(() =>
        {
            Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(result.Diagnostics.Any(item => item.Message!.Contains("more than one", StringComparison.Ordinal)), Is.True);
        });
    }

    [TestCase("t:item//@id", "@id")]
    [TestCase("@id", "@id")]
    [TestCase("t:item", "@id/t:value")]
    [TestCase("u:item", "@id")]
    [TestCase("t: item", "@id")]
    [TestCase(". / / t:item", "@id")]
    public void RejectsExpressionsOutsideTheIdentityXPathSubset(string selector, string field)
    {
        string body =
            "<xs:element name='root'><xs:key name='bad'><xs:selector xpath='" + selector + "'/><xs:field xpath='" + field + "'/></xs:key></xs:element>";

        Assert.Throws<XmlSchemaException>(() => Compile(body));
    }

    private static XmlSchemaAssessmentResult Assess(CompiledXmlSchemaSet compiled, string instance)
    {
        XmlDocument document = Parse(instance);
        return new XmlSchemaAssessmentEngine(compiled, new XmlSchemaValidationOptions(), new RejectingResolver()).AssessXmlSchemaDocument(document);
    }

    private static XmlDocument Parse(string source)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Kind = XmlInputKind.String, Text = source, BaseUri = "https://example.test/value.xml" }, options, null, null, null);
    }

    private static CompiledXmlSchemaSet Compile(string body)
    {
        string schema = "<xs:schema xmlns:xs='" + Xsd + "' xmlns:t='urn:test' targetNamespace='urn:test' elementFormDefault='qualified'>" + body + "</xs:schema>";
        XmlSchemaSet result = new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Kind = XmlInputKind.String, Text = schema, BaseUri = "https://example.test/schema.xsd" }],
            new XmlSchemaValidationOptions { ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied, AllowedSchemes = [] } },
            new RejectingResolver());
        return (CompiledXmlSchemaSet)result.CompiledSet!;
    }

    private sealed class RejectingResolver : IXmlResourceResolver
    {
        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            throw new InvalidOperationException("No external resource was expected.");
        }
    }
}
