using asn1;

namespace asn1.Tests;

public class ValueParserTests
{
    [TestCase("BOOLEAN", "TRUE", AsnValueKind.Boolean, "TRUE")]
    [TestCase("BOOLEAN", "FALSE", AsnValueKind.Boolean, "FALSE")]
    [TestCase("ENUMERATED { red, green }", "green", AsnValueKind.Enumerated, "green")]
    [TestCase("NULL", "NULL", AsnValueKind.Null, "")]
    public void ParserRecognizesBooleanEnumeratedAndNullValues(
        string typeSource,
        string valueSource,
        AsnValueKind expectedKind,
        string expectedText)
    {
        ValueFixture fixture = ParseExpectedValue(typeSource, valueSource);

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Value.Kind, Is.EqualTo(expectedKind));
        Assert.That(syntax.Value.TextValue, Is.EqualTo(expectedText));
        AssertComplete(fixture);
    }

    [Test]
    public void IntegerValuesRetainArbitrarySizeAndSign()
    {
        ValueFixture fixture = ParseExpectedValue(
            "INTEGER",
            "-123456789012345678901234567890");

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Value.Kind, Is.EqualTo(AsnValueKind.Integer));
        Assert.That(syntax.Value.HasIntegerValue, Is.True);
        Assert.That(
            syntax.Value.IntegerValue.ToString(),
            Is.EqualTo("-123456789012345678901234567890"));
        AssertComplete(fixture);
    }

    [TestCase("1.25E-3", AsnRealKind.FiniteDecimal)]
    [TestCase("-0", AsnRealKind.MinusZero)]
    [TestCase("PLUS-INFINITY", AsnRealKind.PositiveInfinity)]
    [TestCase("MINUS-INFINITY", AsnRealKind.NegativeInfinity)]
    [TestCase("NOT-A-NUMBER", AsnRealKind.NotANumber)]
    public void ParserRecognizesDecimalAndSpecialRealValues(
        string source,
        AsnRealKind expectedKind)
    {
        ValueFixture fixture = ParseExpectedValue("REAL", source);

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Value.Kind, Is.EqualTo(AsnValueKind.Real));
        Assert.That(syntax.Value.RealValue?.Kind, Is.EqualTo(expectedKind));
        AssertComplete(fixture);
    }

    [Test]
    public void BinaryRealRetainsArbitraryMantissaRadixAndExponent()
    {
        ValueFixture fixture = ParseExpectedValue(
            "REAL",
            "{ mantissa -12345678901234567890, base 16, exponent -7 }");

        AsnRealValue real = RequireSyntax(fixture.Result).Value.RealValue
            ?? throw new InvalidOperationException();
        Assert.That(real.Kind, Is.EqualTo(AsnRealKind.FiniteBinary));
        Assert.That(real.Mantissa.ToString(), Is.EqualTo("-12345678901234567890"));
        Assert.That(real.Radix, Is.EqualTo(16));
        Assert.That(real.Exponent.ToString(), Is.EqualTo("-7"));
        AssertComplete(fixture);
    }

    [Test]
    public void BitAndOctetStringLiteralsProduceExactBinaryPayloads()
    {
        ValueFixture bitsFixture = ParseExpectedValue("BIT STRING", "'A'H");
        ValueFixture octetsFixture = ParseExpectedValue("OCTET STRING", "'AF09'H");

        ParsedValueSyntax bits = RequireSyntax(bitsFixture.Result);
        ParsedValueSyntax octets = RequireSyntax(octetsFixture.Result);
        Assert.That(bits.BitStringValue?.BitLength, Is.EqualTo(4));
        Assert.That(bits.BitStringValue?.Bytes, Is.EqualTo(new byte[] { 0xa0 }));
        Assert.That(octets.Octets, Is.EqualTo(new byte[] { 0xaf, 0x09 }));
        AssertComplete(bitsFixture);
        AssertComplete(octetsFixture);
    }

    [Test]
    public void NamedBitValuesRetainEverySelectedIdentifier()
    {
        ValueFixture fixture = ParseExpectedValue(
            "BIT STRING { red(0), green(1), blue(2) }",
            "{ red, blue }");

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(
            syntax.Value.NamedValues.Select(item => item.Name),
            Is.EqualTo(new[] { "red", "blue" }));
        AssertComplete(fixture);
    }

    [TestCase("UTF8String", "\"a\"\"b\"", "a\"b")]
    [TestCase("UniversalString", "{0,0,0,65}", "{0,0,0,65}")]
    public void CharacterStringValuesRetainDecodedAndTupleForms(
        string typeSource,
        string valueSource,
        string expectedText)
    {
        ValueFixture fixture = ParseExpectedValue(typeSource, valueSource);

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Value.Kind, Is.EqualTo(AsnValueKind.CharacterString));
        Assert.That(syntax.Value.TextValue, Is.EqualTo(expectedText));
        AssertComplete(fixture);
    }

    [Test]
    public void ObjectIdentifierValuesRetainResolvedAndUnresolvedComponents()
    {
        ValueFixture fixture = ParseExpectedValue(
            "OBJECT IDENTIFIER",
            "{ iso(1) member-body(2) 840 example(1) laterArc }");

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Value.Kind, Is.EqualTo(AsnValueKind.ObjectIdentifier));
        Assert.That(syntax.ObjectIdentifierComponents, Has.Length.EqualTo(5));
        Assert.That(
            syntax.ObjectIdentifierValue?.Arcs.Select(item => item.ToString()),
            Is.EqualTo(new[] { "1", "2", "840", "1" }));
        Assert.That(syntax.ObjectIdentifierComponents[4].DefinedValue, Is.EqualTo("laterArc"));
        AssertComplete(fixture);
    }

    [TestCase("OID-IRI", "\"/iso/1/member-body/2\"", AsnValueKind.ObjectIdentifierIri)]
    [TestCase("RELATIVE-OID-IRI", "\"member/2\"", AsnValueKind.RelativeObjectIdentifierIri)]
    public void IriValuesRetainQuotedContent(
        string typeSource,
        string valueSource,
        AsnValueKind expectedKind)
    {
        ValueFixture fixture = ParseExpectedValue(typeSource, valueSource);

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Value.Kind, Is.EqualTo(expectedKind));
        Assert.That(syntax.Value.TextValue, Does.Not.StartWith("\""));
        AssertComplete(fixture);
    }

    [Test]
    public void StructuredValuesUseComponentTypesForNestedValues()
    {
        ValueFixture fixture = ParseExpectedValue(
            "SEQUENCE { id INTEGER, enabled BOOLEAN, nested SET { text UTF8String } }",
            "{ id 7, enabled TRUE, nested { text \"ok\" } }");

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Value.Kind, Is.EqualTo(AsnValueKind.Sequence));
        Assert.That(syntax.Value.NamedValues.Select(item => item.Name),
            Is.EqualTo(new[] { "id", "enabled", "nested" }));
        Assert.That(syntax.Items[0].Value.IntegerValue.ToString(), Is.EqualTo("7"));
        Assert.That(syntax.Items[1].Value.Kind, Is.EqualTo(AsnValueKind.Boolean));
        Assert.That(syntax.Items[2].Value.Kind, Is.EqualTo(AsnValueKind.Set));
        AssertComplete(fixture);
    }

    [Test]
    public void CollectionsSupportPlainAndNamedValueLists()
    {
        ValueFixture plainFixture = ParseExpectedValue("SEQUENCE OF INTEGER", "{ 1, -2, 3 }");
        ValueFixture namedFixture = ParseExpectedValue(
            "SEQUENCE OF item INTEGER",
            "{ first 1, second 2 }");

        ParsedValueSyntax plain = RequireSyntax(plainFixture.Result);
        ParsedValueSyntax named = RequireSyntax(namedFixture.Result);
        Assert.That(plain.Items.Select(item => item.Value.IntegerValue.ToString()),
            Is.EqualTo(new[] { "1", "-2", "3" }));
        Assert.That(named.Value.NamedValues.Select(item => item.Name),
            Is.EqualTo(new[] { "first", "second" }));
        AssertComplete(plainFixture);
        AssertComplete(namedFixture);
    }

    [Test]
    public void CollectionItemsCanBeChoiceValuesWithoutBeingMisclassifiedAsNames()
    {
        ValueFixture fixture = ParseExpectedValue(
            "SEQUENCE OF CHOICE { number INTEGER, text UTF8String }",
            "{ number: 1, text: \"ok\" }");

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Items.Select(item => item.Value.Kind),
            Is.EqualTo(new[] { AsnValueKind.Choice, AsnValueKind.Choice }));
        Assert.That(syntax.Items[0].InnerValue?.Value.IntegerValue.ToString(), Is.EqualTo("1"));
        Assert.That(syntax.Items[1].InnerValue?.Value.TextValue, Is.EqualTo("ok"));
        AssertComplete(fixture);
    }

    [Test]
    public void ChoiceAndOpenValuesRetainTheirSelectedAndGovernedValues()
    {
        ValueFixture choiceFixture = ParseExpectedValue(
            "CHOICE { number INTEGER, text UTF8String }",
            "number: -4");
        ValueFixture openFixture = ParseValueWithType(
            new AsnType { Kind = AsnTypeKind.OpenType },
            "INTEGER : 42");

        ParsedValueSyntax choice = RequireSyntax(choiceFixture.Result);
        ParsedValueSyntax open = RequireSyntax(openFixture.Result);
        Assert.That(choice.Value.Kind, Is.EqualTo(AsnValueKind.Choice));
        Assert.That(choice.Value.NamedValues.Single().Name, Is.EqualTo("number"));
        Assert.That(choice.InnerValue?.Value.IntegerValue.ToString(), Is.EqualTo("-4"));
        Assert.That(open.Value.Kind, Is.EqualTo(AsnValueKind.OpenType));
        Assert.That(open.GoverningTypeSyntax?.Type.Kind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(open.InnerValue?.Value.IntegerValue.ToString(), Is.EqualTo("42"));
        AssertComplete(choiceFixture);
        AssertComplete(openFixture);
    }

    [TestCase("EXTERNAL", "{ identification syntaxRef, data-value payload }", AsnValueKind.External)]
    [TestCase("EMBEDDED PDV", "{ identification syntaxRef, data-value payload }", AsnValueKind.EmbeddedPdv)]
    [TestCase("INSTANCE OF TYPE-IDENTIFIER", "{ type-id oidRef, value INTEGER: 5 }", AsnValueKind.InstanceOf)]
    public void SpecialStructuredValuesRetainTheirNamedFields(
        string typeSource,
        string valueSource,
        AsnValueKind expectedKind)
    {
        ValueFixture fixture = ParseExpectedValue(typeSource, valueSource);

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Value.Kind, Is.EqualTo(expectedKind));
        Assert.That(syntax.Value.NamedValues, Has.Length.EqualTo(2));
        AssertComplete(fixture);
    }

    [TestCase("GeneralizedTime", "\"20260828153045.123-0700\"", AsnTimeKind.GeneralizedTime)]
    [TestCase("DATE", "\"2026-08-28\"", AsnTimeKind.Date)]
    [TestCase("DURATION", "\"P2DT3H\"", AsnTimeKind.Duration)]
    public void UsefulAndTimeValuesRetainKindTextAndMetadata(
        string typeSource,
        string valueSource,
        AsnTimeKind expectedKind)
    {
        ValueFixture fixture = ParseExpectedValue(typeSource, valueSource);

        AsnTimeValue time = RequireSyntax(fixture.Result).Value.TimeValue
            ?? throw new InvalidOperationException();
        Assert.That(time.Kind, Is.EqualTo(expectedKind));
        Assert.That(time.Text, Is.Not.Empty);
        if (expectedKind == AsnTimeKind.GeneralizedTime)
        {
            Assert.That(time.FractionalDigits, Is.EqualTo(3));
            Assert.That(time.UtcOffsetMinutes, Is.EqualTo(-420));
        }

        AssertComplete(fixture);
    }

    [TestCase("INTEGER", "limit", "", "limit")]
    [TestCase("BOOLEAN", "Constants.enabled", "Constants", "enabled")]
    public void DefinedValuesRetainLocalAndExternalReferences(
        string typeSource,
        string valueSource,
        string expectedModule,
        string expectedName)
    {
        ValueFixture fixture = ParseExpectedValue(typeSource, valueSource);

        ParsedValueSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Value.Kind, Is.EqualTo(AsnValueKind.Defined));
        Assert.That(syntax.ReferenceModule, Is.EqualTo(expectedModule));
        Assert.That(syntax.ReferenceName, Is.EqualTo(expectedName));
        AssertComplete(fixture);
    }

    [Test]
    public void ComponentDefaultsUseTheGeneralValueParser()
    {
        TypeFixture fixture = ParseType(
            "SEQUENCE { enabled BOOLEAN DEFAULT TRUE, count INTEGER DEFAULT -3 }");

        ParsedTypeSyntax syntax = RequireTypeSyntax(fixture.Result);
        Assert.That(syntax.Type.Components[0].DefaultValue?.Kind, Is.EqualTo(AsnValueKind.Boolean));
        Assert.That(syntax.Type.Components[1].DefaultValue?.Kind, Is.EqualTo(AsnValueKind.Integer));
        Assert.That(
            syntax.ComponentSyntax[1].DefaultValueSyntax?.Value.IntegerValue.ToString(),
            Is.EqualTo("-3"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase("OCTET STRING", "'A'H", "ASNP0075")]
    [TestCase("OBJECT IDENTIFIER", "{}", "ASNP0079")]
    [TestCase("OBJECT IDENTIFIER", "{ iso(1), member(2) }", "ASNP0077")]
    [TestCase("REAL", "{ mantissa 1, exponent 2 }", "ASNP0071")]
    [TestCase("SEQUENCE { id INTEGER }", "{ id 1 extra 2 }", "ASNP0081")]
    [TestCase("SEQUENCE OF INTEGER", "{ 1, }", "ASNP0083")]
    public void MalformedBasicValuesProduceSpecificDiagnostics(
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
        IncrementalLexSession session = CreateSession("value.asn", source);
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
        IncrementalLexSession session = CreateSession("value-type.asn", source);
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
