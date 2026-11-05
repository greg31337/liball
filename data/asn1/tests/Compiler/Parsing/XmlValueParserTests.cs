using asn1;

namespace asn1.Tests;

public class XmlValueParserTests
{
    [TestCase("BOOLEAN", "<true/>", AsnValueKind.Boolean, "TRUE")]
    [TestCase("BOOLEAN", "<BOOLEAN>false</BOOLEAN>", AsnValueKind.Boolean, "FALSE")]
    [TestCase("INTEGER", "<INTEGER>-12345678901234567890</INTEGER>", AsnValueKind.Integer, "")]
    [TestCase("ENUMERATED { red, blue }", "<blue/>", AsnValueKind.Enumerated, "blue")]
    [TestCase("NULL", "<NULL/>", AsnValueKind.Null, "")]
    [TestCase("REAL", "<REAL>INF</REAL>", AsnValueKind.Real, "")]
    public void XmlScalarValuesMapToTheirBasicValueKinds(
        string typeSource,
        string valueSource,
        AsnValueKind expectedKind,
        string expectedText)
    {
        ValueFixture fixture = ParseExpectedValue(typeSource, valueSource);

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Value.Kind, Is.EqualTo(expectedKind));
        Assert.That(syntax.Value.TextValue, Is.EqualTo(expectedText));
        Assert.That(syntax.XmlElement, Is.Not.Null);
        AssertComplete(fixture);
    }

    [Test]
    public void XmlBinaryValuesProduceExactBitAndOctetPayloads()
    {
        ValueFixture bitsFixture = ParseExpectedValue("BIT STRING", "<bits>10110</bits>");
        ValueFixture namedFixture = ParseExpectedValue(
            "BIT STRING { red(0), blue(1) }",
            "<bits><red/><blue/></bits>");
        ValueFixture octetsFixture = ParseExpectedValue(
            "OCTET STRING",
            "<octets>AF 09</octets>");

        ParsedValueSyntax bits = RequireSyntax(bitsFixture.Result);
        ParsedValueSyntax named = RequireSyntax(namedFixture.Result);
        ParsedValueSyntax octets = RequireSyntax(octetsFixture.Result);
        Assert.That(bits.BitStringValue?.BitLength, Is.EqualTo(5));
        Assert.That(bits.BitStringValue?.Bytes, Is.EqualTo(new byte[] { 0xb0 }));
        Assert.That(named.Value.NamedValues.Select(item => item.Name),
            Is.EqualTo(new[] { "red", "blue" }));
        Assert.That(octets.Octets, Is.EqualTo(new byte[] { 0xaf, 0x09 }));
        AssertComplete(bitsFixture);
        AssertComplete(namedFixture);
        AssertComplete(octetsFixture);
    }

    [Test]
    public void XmlCharacterStringsDecodeEntitiesAndResolveNamespaces()
    {
        ValueFixture fixture = ParseExpectedValue(
            "UTF8String",
            "<x:UTF8String xmlns:x='urn:asn1' xmlns:m='urn:meta' m:id='A&amp;B' xml:lang='en'>"
            + "A&amp;&#x42;&#67;"
            + "</x:UTF8String>");

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        XmlElementSyntax element = syntax.XmlElement ?? throw new InvalidOperationException();
        Assert.That(syntax.Value.TextValue, Is.EqualTo("A&BC"));
        Assert.That(element.Prefix, Is.EqualTo("x"));
        Assert.That(element.LocalName, Is.EqualTo("UTF8String"));
        Assert.That(element.NamespaceUri, Is.EqualTo("urn:asn1"));
        XmlAttributeSyntax metadata = element.Attributes.Single(item => item.Name == "m:id");
        Assert.That(metadata.NamespaceUri, Is.EqualTo("urn:meta"));
        Assert.That(metadata.Value, Is.EqualTo("A&B"));
        Assert.That(
            element.Attributes.Single(item => item.Name == "xml:lang").NamespaceUri,
            Is.EqualTo("http://www.w3.org/XML/1998/namespace"));
        AssertComplete(fixture);
    }

    [Test]
    public void DefaultNamespacesApplyToElementsAndEmptyStringForms()
    {
        ValueFixture fixture = ParseExpectedValue(
            "UTF8String",
            "<UTF8String xmlns='urn:default'/>");

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Value.TextValue, Is.Empty);
        Assert.That(syntax.XmlElement?.NamespaceUri, Is.EqualTo("urn:default"));
        Assert.That(syntax.XmlElement?.Empty, Is.True);
        AssertComplete(fixture);
    }

    [Test]
    public void XmlOidAndIriValuesRetainNormalizedTextAndArcs()
    {
        ValueFixture oidFixture = ParseExpectedValue(
            "OBJECT IDENTIFIER",
            "<OBJECT-IDENTIFIER>1.2.840.113549</OBJECT-IDENTIFIER>");
        ValueFixture iriFixture = ParseExpectedValue(
            "OID-IRI",
            "<OID-IRI>/iso/1/member-body/2</OID-IRI>");

        ParsedValueSyntax oid = RequireSyntax(oidFixture.Result);
        ParsedValueSyntax iri = RequireSyntax(iriFixture.Result);
        Assert.That(oid.ObjectIdentifierValue?.Arcs.Select(item => item.ToString()),
            Is.EqualTo(new[] { "1", "2", "840", "113549" }));
        Assert.That(iri.Value.TextValue, Is.EqualTo("/iso/1/member-body/2"));
        AssertComplete(oidFixture);
        AssertComplete(iriFixture);
    }

    [Test]
    public void XmlStructuredValuesUseChildNamesAndComponentTypes()
    {
        ValueFixture fixture = ParseExpectedValue(
            "SEQUENCE { id INTEGER, enabled BOOLEAN, profile SET { name UTF8String } }",
            "<Person><id>7</id><enabled>true</enabled>"
            + "<profile><name>Alice</name></profile></Person>");

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Value.Kind, Is.EqualTo(AsnValueKind.Sequence));
        Assert.That(syntax.Value.NamedValues.Select(item => item.Name),
            Is.EqualTo(new[] { "id", "enabled", "profile" }));
        Assert.That(syntax.Items[0].Value.IntegerValue.ToString(), Is.EqualTo("7"));
        Assert.That(syntax.Items[1].Value.TextValue, Is.EqualTo("TRUE"));
        Assert.That(syntax.Items[2].Items[0].Value.TextValue, Is.EqualTo("Alice"));
        AssertComplete(fixture);
    }

    [Test]
    public void XmlCollectionsSupportItemsAndEmptyForms()
    {
        ValueFixture itemsFixture = ParseExpectedValue(
            "SEQUENCE OF INTEGER",
            "<Values><item>1</item><item>-2</item><item>3</item></Values>");
        ValueFixture emptyFixture = ParseExpectedValue("SET OF UTF8String", "<Values/>");

        ParsedValueSyntax items = RequireSyntax(itemsFixture.Result);
        ParsedValueSyntax empty = RequireSyntax(emptyFixture.Result);
        Assert.That(items.Items.Select(item => item.Value.IntegerValue.ToString()),
            Is.EqualTo(new[] { "1", "-2", "3" }));
        Assert.That(empty.Items, Is.Empty);
        AssertComplete(itemsFixture);
        AssertComplete(emptyFixture);
    }

    [Test]
    public void XmlChoiceAndOpenValuesRetainSelectedAndGovernedChildren()
    {
        ValueFixture choiceFixture = ParseExpectedValue(
            "CHOICE { number INTEGER, text UTF8String }",
            "<number>-4</number>");
        ValueFixture openFixture = ParseValueWithType(
            new AsnType { Kind = AsnTypeKind.OpenType },
            "<Open><INTEGER>42</INTEGER></Open>");

        ParsedValueSyntax choice = RequireSyntax(choiceFixture.Result);
        ParsedValueSyntax open = RequireSyntax(openFixture.Result);
        Assert.That(choice.Value.NamedValues.Single().Name, Is.EqualTo("number"));
        Assert.That(choice.InnerValue?.Value.IntegerValue.ToString(), Is.EqualTo("-4"));
        Assert.That(open.GoverningTypeSyntax?.Type.Kind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(open.InnerValue?.Value.IntegerValue.ToString(), Is.EqualTo("42"));
        AssertComplete(choiceFixture);
        AssertComplete(openFixture);
    }

    [TestCase("EXTERNAL", AsnValueKind.External)]
    [TestCase("EMBEDDED PDV", AsnValueKind.EmbeddedPdv)]
    [TestCase("INSTANCE OF TYPE-IDENTIFIER", AsnValueKind.InstanceOf)]
    public void XmlSpecialValuesUseStructuredChildNotation(
        string typeSource,
        AsnValueKind expectedKind)
    {
        ValueFixture fixture = ParseExpectedValue(
            typeSource,
            "<Special><identification>syntaxRef</identification><data-value>payload</data-value></Special>");

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Value.Kind, Is.EqualTo(expectedKind));
        Assert.That(syntax.Value.NamedValues.Select(item => item.Name),
            Is.EqualTo(new[] { "identification", "data-value" }));
        AssertComplete(fixture);
    }

    [TestCase("GeneralizedTime", "<GeneralizedTime>20260828153045.25Z</GeneralizedTime>", AsnTimeKind.GeneralizedTime)]
    [TestCase("DATE", "<DATE>2026-08-28</DATE>", AsnTimeKind.Date)]
    [TestCase("DURATION", "<DURATION>P2DT3H</DURATION>", AsnTimeKind.Duration)]
    public void XmlTimeValuesRetainTheirSpecificTimeKinds(
        string typeSource,
        string valueSource,
        AsnTimeKind expectedKind)
    {
        ValueFixture fixture = ParseExpectedValue(typeSource, valueSource);

        AsnTimeValue time = RequireSyntax(fixture.Result).Value.TimeValue
            ?? throw new InvalidOperationException();
        Assert.That(time.Kind, Is.EqualTo(expectedKind));
        Assert.That(time.Text, Is.Not.Empty);
        AssertComplete(fixture);
    }

    [Test]
    public void XmlValuesMapThroughTaggedTypes()
    {
        ValueFixture fixture = ParseExpectedValue("[APPLICATION 3] INTEGER", "<INTEGER>9</INTEGER>");

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Value.Kind, Is.EqualTo(AsnValueKind.Integer));
        Assert.That(syntax.Value.IntegerValue.ToString(), Is.EqualTo("9"));
        AssertComplete(fixture);
    }

    [Test]
    public void ParserConsumesOneXmlTokenAndLeavesFollowingAsnSourceAvailable()
    {
        IncrementalLexSession session = CreateSession(
            "xml-recovery.asn",
            "<INTEGER>7</INTEGER> nextValue");
        AsnToken[] tokens = LexTokens(session);
        ValueParser parser = new ValueParser(tokens, session.Diagnostics, true);

        ValueParseResult result = parser.ParseValueSyntax(new AsnType { Kind = AsnTypeKind.Integer });

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConsumedTokenCount, Is.EqualTo(1));
        Assert.That(parser.Position, Is.EqualTo(1));
        Assert.That(tokens[1].Text, Is.EqualTo("nextValue"));
        Assert.That(session.GetDiagnostics(), Is.Empty);
    }

    [TestCase("INTEGER", "<A></B>", "ASNP0093")]
    [TestCase("UTF8String", "<x:Text/>", "ASNP0090")]
    [TestCase("OCTET STRING", "<octets>ABC</octets>", "ASNP0105")]
    [TestCase("NULL", "<NULL>content</NULL>", "ASNP0106")]
    [TestCase("CHOICE { number INTEGER }", "<choice/>", "ASNP0107")]
    public void MalformedXmlValuesProduceSpecificDiagnostics(
        string typeSource,
        string valueSource,
        string expectedCode)
    {
        ValueFixture fixture = ParseExpectedValue(typeSource, valueSource);

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain(expectedCode));
    }

    private static ValueFixture ParseExpectedValue(string typeSource, string valueSource)
    {
        TypeFixture type = ParseType(typeSource);
        Assert.That(type.Result.Success, Is.True);
        Assert.That(type.Diagnostics.GetDiagnostics(), Is.Empty);
        return ParseValueWithType(RequireTypeSyntax(type.Result).Type, valueSource);
    }

    private static ValueFixture ParseValueWithType(AsnType type, string source)
    {
        IncrementalLexSession session = CreateSession("xml-value.asn", source);
        AsnToken[] tokens = LexTokens(session);
        return new ValueFixture
        {
            Tokens = tokens,
            Diagnostics = session.Diagnostics,
            Result = new ValueParser(tokens, session.Diagnostics, true).ParseValueSyntax(type)
        };
    }

    private static TypeFixture ParseType(string source)
    {
        IncrementalLexSession session = CreateSession("xml-value-type.asn", source);
        AsnToken[] tokens = LexTokens(session);
        return new TypeFixture
        {
            Diagnostics = session.Diagnostics,
            Result = new TypeParser(tokens, session.Diagnostics, true).ParseTypeSyntax()
        };
    }

    private static AsnToken[] LexTokens(IncrementalLexSession session)
    {
        List<AsnToken> tokens = new List<AsnToken>();
        while (true)
        {
            IncrementalLexResult lexical = session.ScanNext();
            if (lexical.Status == IncrementalLexStatus.EndOfInput)
            {
                return tokens.ToArray();
            }

            Assert.That(lexical.Status, Is.EqualTo(IncrementalLexStatus.Token));
            AsnToken token = lexical.Token ?? throw new InvalidOperationException();
            if (token.Kind != AsnTokenKind.Whitespace
                && token.Kind != AsnTokenKind.Comment
                && token.Kind != AsnTokenKind.EndOfInput)
            {
                tokens.Add(token);
            }
        }
    }

    private static IncrementalLexSession CreateSession(string sourceName, string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            sourceName,
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        return session;
    }

    private static void AssertComplete(ValueFixture fixture)
    {
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    private static ParsedValueSyntax RequireSyntax(ValueParseResult result)
    {
        Assert.That(result.Syntax, Is.Not.Null);
        return result.Syntax ?? throw new InvalidOperationException();
    }

    private static ParsedTypeSyntax RequireTypeSyntax(TypeParseResult result)
    {
        Assert.That(result.Syntax, Is.Not.Null);
        return result.Syntax ?? throw new InvalidOperationException();
    }

    private class ValueFixture
    {
        internal AsnToken[] Tokens = [];
        internal DiagnosticBag Diagnostics = null!;
        internal ValueParseResult Result = null!;
    }

    private class TypeFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal TypeParseResult Result = null!;
    }
}
