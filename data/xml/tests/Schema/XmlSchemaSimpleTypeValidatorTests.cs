using xml;

namespace xml.Tests;

[TestFixture]
public class XmlSchemaSimpleTypeValidatorTests
{
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void AppliesWhitespaceBeforeUnicodeLengthAndPatternFacets()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:simpleType name='Code'><xs:restriction base='xs:string'><xs:whiteSpace value='collapse'/><xs:length value='3'/><xs:pattern value='[A-Z]{3}'/></xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='UnicodePair'><xs:restriction base='xs:string'><xs:length value='2'/></xs:restriction></xs:simpleType>");
        CompiledXmlSchemaSimpleType code = Find(compiled, "Code");
        CompiledXmlSchemaSimpleType unicode = Find(compiled, "UnicodePair");

        XmlSchemaSimpleTypeValidationResult valid = Validate(code, "  ABC  ");
        XmlSchemaSimpleTypeValidationResult wrongLength = Validate(code, "ABCD");
        XmlSchemaSimpleTypeValidationResult pattern = Validate(code, "AbC");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.NormalizedValue, Is.EqualTo("ABC"));
            Assert.That(valid.CanonicalValue, Is.EqualTo("ABC"));
            Assert.That(wrongLength.Success, Is.False);
            Assert.That(wrongLength.FailedFacet!.Kind, Is.EqualTo(XmlSchemaFacetKind.Length));
            Assert.That(pattern.Success, Is.False);
            Assert.That(pattern.FailedFacet!.Kind, Is.EqualTo(XmlSchemaFacetKind.Pattern));
            Assert.That(Validate(unicode, "A😀").Success, Is.True);
            Assert.That(Validate(unicode, "A😀B").Success, Is.False);
        });
    }

    [Test]
    public void ValidatesListItemsLengthsAndCanonicalValues()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:simpleType name='IntegerList'><xs:list itemType='xs:int'/></xs:simpleType>" +
            "<xs:simpleType name='Pair'><xs:restriction base='t:IntegerList'><xs:length value='2'/></xs:restriction></xs:simpleType>");
        CompiledXmlSchemaSimpleType pair = Find(compiled, "Pair");

        XmlSchemaSimpleTypeValidationResult valid = Validate(pair, "  01\t2  ");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.NormalizedValue, Is.EqualTo("01 2"));
            Assert.That(valid.CanonicalValue, Is.EqualTo("1 2"));
            Assert.That(valid.AtomicValues, Has.Length.EqualTo(2));
            Assert.That(Validate(pair, "1").Success, Is.False);
            Assert.That(Validate(pair, "1 nope").Success, Is.False);
        });
    }

    [Test]
    public void UsesValueSpaceEqualityForUnionAndNumericEnumerations()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:simpleType name='NumberOrFlag'><xs:union memberTypes='xs:int xs:boolean'/></xs:simpleType>" +
            "<xs:simpleType name='Allowed'><xs:restriction base='t:NumberOrFlag'><xs:enumeration value='1'/><xs:enumeration value='true'/></xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='One'><xs:restriction base='xs:decimal'><xs:enumeration value='1.0'/></xs:restriction></xs:simpleType>");
        CompiledXmlSchemaSimpleType allowed = Find(compiled, "Allowed");
        CompiledXmlSchemaSimpleType one = Find(compiled, "One");

        Assert.Multiple(() =>
        {
            Assert.That(Validate(allowed, "01").Success, Is.True);
            Assert.That(Validate(allowed, "true").Success, Is.True);
            Assert.That(Validate(allowed, "false").Success, Is.False);
            Assert.That(Validate(one, "1.00").Success, Is.True);
            Assert.That(Validate(one, "2").Success, Is.False);
        });
    }

    [Test]
    public void EnforcesInclusiveAndExclusiveNumericBounds()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:simpleType name='Range'><xs:restriction base='xs:decimal'><xs:minExclusive value='1.5'/><xs:maxInclusive value='10'/></xs:restriction></xs:simpleType>");
        CompiledXmlSchemaSimpleType range = Find(compiled, "Range");

        Assert.Multiple(() =>
        {
            Assert.That(Validate(range, "1.50").Success, Is.False);
            Assert.That(Validate(range, "1.5001").Success, Is.True);
            Assert.That(Validate(range, "10.00").Success, Is.True);
            Assert.That(Validate(range, "10.01").Success, Is.False);
        });
    }

    [Test]
    public void EnforcesDigitFractionAndScaleFacets()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:simpleType name='Money'><xs:restriction base='xs:decimal'><xs:totalDigits value='5'/><xs:fractionDigits value='2'/><xs:maxScale value='2'/><xs:minScale value='1'/></xs:restriction></xs:simpleType>");
        CompiledXmlSchemaSimpleType money = Find(compiled, "Money");

        Assert.Multiple(() =>
        {
            Assert.That(Validate(money, "123.45").Success, Is.True);
            Assert.That(Validate(money, "1234.5").Success, Is.True);
            Assert.That(Validate(money, "12345.6").Success, Is.False);
            Assert.That(Validate(money, "1.234").Success, Is.False);
            Assert.That(Validate(money, "12").Success, Is.False);
        });
    }

    [Test]
    public void EnforcesTemporalBoundsAndExplicitTimezone()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:simpleType name='Window'><xs:restriction base='xs:dateTime'><xs:minInclusive value='2020-01-01T00:00:00Z'/><xs:maxExclusive value='2021-01-01T00:00:00Z'/><xs:explicitTimezone value='required'/></xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='LocalDate'><xs:restriction base='xs:date'><xs:explicitTimezone value='prohibited'/></xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='Delay'><xs:restriction base='xs:dayTimeDuration'><xs:minInclusive value='PT1S'/><xs:maxInclusive value='PT10S'/></xs:restriction></xs:simpleType>");
        CompiledXmlSchemaSimpleType window = Find(compiled, "Window");
        CompiledXmlSchemaSimpleType local = Find(compiled, "LocalDate");
        CompiledXmlSchemaSimpleType delay = Find(compiled, "Delay");

        Assert.Multiple(() =>
        {
            Assert.That(Validate(window, "2020-06-01T00:00:00Z").Success, Is.True);
            Assert.That(Validate(window, "2020-06-01T00:00:00").Success, Is.False);
            Assert.That(Validate(window, "2021-01-01T00:00:00Z").Success, Is.False);
            Assert.That(Validate(local, "2020-06-01").Success, Is.True);
            Assert.That(Validate(local, "2020-06-01Z").Success, Is.False);
            Assert.That(Validate(delay, "PT1S").Success, Is.True);
            Assert.That(Validate(delay, "PT11S").Success, Is.False);
        });
    }

    [Test]
    public void MeasuresBinaryValuesByOctets()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:simpleType name='Digest'><xs:restriction base='xs:hexBinary'><xs:length value='2'/></xs:restriction></xs:simpleType>");
        CompiledXmlSchemaSimpleType digest = Find(compiled, "Digest");

        Assert.Multiple(() =>
        {
            Assert.That(Validate(digest, "0aff").Success, Is.True);
            Assert.That(Validate(digest, "0A").Success, Is.False);
            Assert.That(Validate(digest, "0AFFFF").Success, Is.False);
        });
    }

    [Test]
    public void CombinesSiblingPatternsAsAlternativesAndInheritedPatternsAsConjunctions()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:simpleType name='LettersOrDigits'><xs:restriction base='xs:string'><xs:pattern value='[A-Z]+'/><xs:pattern value='[0-9]+'/></xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='Three'><xs:restriction base='t:LettersOrDigits'><xs:length value='3'/><xs:pattern value='...'/></xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='NcNamePattern'><xs:restriction base='xs:string'><xs:pattern value='[\\i-[:]][\\c-[:]]*'/></xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='LiteralAnchors'><xs:restriction base='xs:string'><xs:pattern value='A^B$C'/></xs:restriction></xs:simpleType>");
        CompiledXmlSchemaSimpleType three = Find(compiled, "Three");
        CompiledXmlSchemaSimpleType ncNamePattern = Find(compiled, "NcNamePattern");
        CompiledXmlSchemaSimpleType literalAnchors = Find(compiled, "LiteralAnchors");

        Assert.Multiple(() =>
        {
            Assert.That(Validate(three, "ABC").Success, Is.True);
            Assert.That(Validate(three, "123").Success, Is.True);
            Assert.That(Validate(three, "abc").Success, Is.False);
            Assert.That(Validate(three, "AB").Success, Is.False);
            Assert.That(Validate(ncNamePattern, "valid-name").Success, Is.True);
            Assert.That(Validate(ncNamePattern, ":").Success, Is.False);
            Assert.That(Validate(literalAnchors, "A^B$C").Success, Is.True);
            Assert.That(Validate(literalAnchors, "xA^B$Cy").Success, Is.False);
        });
    }

    [Test]
    public void RejectsMalformedPatternFacetsDuringSchemaCompilation()
    {
        XmlSchemaException failure = Assert.Throws<XmlSchemaException>(() => Compile(
            "<xs:simpleType name='Broken'><xs:restriction base='xs:string'><xs:pattern value='[a-'/></xs:restriction></xs:simpleType>"))!;

        Assert.That(failure.Message, Does.Contain("character class"));
    }

    [Test]
    public void EvaluatesBooleanNumericStringAndPatternAssertions()
    {
        CompiledXmlSchemaSet compiled = Compile(
            "<xs:simpleType name='Even'><xs:restriction base='xs:int'><xs:assertion test='$value ge 0 and $value mod 2 eq 0'/></xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='Label'><xs:restriction base='xs:string'><xs:assertion test=\"string-length($value) ge 3 and starts-with($value, 'A')\"/><xs:assertion test=\"matches($value, '[A-Za-z]+')\"/></xs:restriction></xs:simpleType>");
        CompiledXmlSchemaSimpleType even = Find(compiled, "Even");
        CompiledXmlSchemaSimpleType label = Find(compiled, "Label");

        Assert.Multiple(() =>
        {
            Assert.That(Validate(even, "4").Success, Is.True);
            Assert.That(Validate(even, "-2").Success, Is.False);
            Assert.That(Validate(even, "3").Success, Is.False);
            Assert.That(Validate(label, "Alpha").Success, Is.True);
            Assert.That(Validate(label, "Ax").Success, Is.False);
            Assert.That(Validate(label, "Beta").Success, Is.False);
        });
    }

    [Test]
    public void ComparesQNameEnumerationsByExpandedName()
    {
        string body =
            "<xs:simpleType name='Named'><xs:restriction base='xs:QName'><xs:enumeration value='q:item' xmlns:q='urn:names'/></xs:restriction></xs:simpleType>";
        CompiledXmlSchemaSimpleType named = Find(Compile(body), "Named");
        XmlNode context = Parse("<value xmlns:a='urn:names' xmlns:b='urn:other'/>").DocumentElement!;

        Assert.Multiple(() =>
        {
            Assert.That(new XmlSchemaSimpleTypeValidator().ValidateXmlSchemaSimpleType(named, "a:item", context).Success, Is.True);
            Assert.That(new XmlSchemaSimpleTypeValidator().ValidateXmlSchemaSimpleType(named, "b:item", context).Success, Is.False);
        });
    }

    [Test]
    public void AppliesFacetsDuringElementSimpleContentAndAttributeAssessment()
    {
        string body =
            "<xs:simpleType name='Code'><xs:restriction base='xs:string'><xs:length value='3'/><xs:pattern value='[A-Z]+'/></xs:restriction></xs:simpleType>" +
            "<xs:complexType name='Amount'><xs:simpleContent><xs:extension base='xs:positiveInteger'><xs:attribute name='code' type='t:Code' use='required'/></xs:extension></xs:simpleContent></xs:complexType>" +
            "<xs:element name='root' type='t:Amount'/>";
        CompiledXmlSchemaSet compiled = Compile(body);

        XmlSchemaAssessmentResult valid = Assess(compiled, "<root xmlns='urn:test' code='ABC'>2</root>");
        XmlSchemaAssessmentResult badElement = Assess(compiled, "<root xmlns='urn:test' code='ABC'>0</root>");
        XmlSchemaAssessmentResult badAttribute = Assess(compiled, "<root xmlns='urn:test' code='ab'>2</root>");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(valid.Assessments[0].SimpleTypeValidation!.AtomicValue!.Type, Is.EqualTo(XdmAtomicType.PositiveInteger));
            Assert.That(badElement.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(badAttribute.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
        });
    }

    private static XmlSchemaSimpleTypeValidationResult Validate(CompiledXmlSchemaSimpleType type, string lexical)
    {
        return new XmlSchemaSimpleTypeValidator().ValidateXmlSchemaSimpleType(type, lexical, null);
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

    private static CompiledXmlSchemaSimpleType Find(CompiledXmlSchemaSet compiled, string localName)
    {
        return compiled.SimpleTypes.Last(type => type.Name != null && type.Name.NamespaceUri == "urn:test" && type.Name.LocalName == localName);
    }

    private sealed class RejectingResolver : IXmlResourceResolver
    {
        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            throw new InvalidOperationException("No external resource was expected.");
        }
    }
}
