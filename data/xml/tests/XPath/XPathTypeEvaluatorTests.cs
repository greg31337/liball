using xml;

namespace xml.Tests;

[TestFixture]
public class XPathTypeEvaluatorTests
{
    [Test]
    public void EvaluatesInstanceOfForSubtypesCardinalitiesAndCallableKinds()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        string source = "(1 instance of xs:decimal, (1, 2) instance of xs:integer+, " +
            "() instance of empty-sequence(), map {'x': 1} instance of map(*), " +
            "[1] instance of array(*), function(){1} instance of function(*))";

        XdmSequence result = Evaluate(source, context, new XPathDynamicContext());

        Assert.That(Booleans(result), Is.EqualTo(new[] { true, true, true, true, true, true }));
    }

    [Test]
    public void EvaluatesTypedFunctionTestsUsingArityAndVariance()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        string source = "(function($value as item()) as item() { $value } instance of function(xs:string) as item(), " +
            "function($value as item()) as item() { $value } instance of function() as item(), " +
            "function($value as item()) as xs:integer { $value } instance of function(item(), item()) as item(), " +
            "function($value as xs:string) as item() { $value } instance of function(item()) as item())";

        XdmSequence result = Evaluate(source, context, new XPathDynamicContext());

        Assert.That(Booleans(result), Is.EqualTo(new[] { true, false, false, false }));
    }

    [Test]
    public void EvaluatesParameterizedArrayAndMapTypesRecursively()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        string source = "([('A', 'B'), 'C'] instance of array(xs:string), " +
            "[('A', 'B'), 'C'] instance of array(xs:string+), " +
            "[map{1: map{2: map{}}}] instance of array(map(xs:integer, map(xs:integer, map(*)))), " +
            "[map{1: map{true(): false()}}] instance of array(map(xs:integer, map(xs:integer, map(*)))))";

        XdmSequence result = Evaluate(source, context, new XPathDynamicContext());

        Assert.That(Booleans(result), Is.EqualTo(new[] { false, true, true, false }));
    }

    [Test]
    public void AppliesStructuralFunctionMapAndEmptySequenceSubtyping()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        string source = "(map{'key':()} instance of map(xs:string, empty-sequence()), " +
            "function($value as map(xs:integer, xs:string)) as xs:integer {map:size($value)} instance of function(map(*)) as xs:integer, " +
            "function($value as function(*)) as xs:integer {function-arity($value)} instance of function(map(*)) as xs:integer, " +
            "map{1:'A'} instance of function(xs:integer) as xs:string?, " +
            "map{} instance of function(xs:integer) as empty-sequence(), " +
            "name#1 instance of function(element()) as xs:string, " +
            "filter#2 instance of function(item()*, function(item()*) as xs:boolean) as item()*)";

        XdmSequence result = Evaluate(source, context, new XPathDynamicContext());

        Assert.That(Booleans(result), Is.EqualTo(new[] { true, false, true, true, true, true, true }));
    }

    [Test]
    public void TreatAsReturnsMatchingValuesAndReportsTypedFailures()
    {
        XPathStaticContext context = new XPathStaticContext();

        XdmSequence result = Evaluate("(1, 2) treat as xs:integer+", context, new XPathDynamicContext());
        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate("1 treat as xs:string", context, new XPathDynamicContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(Integers(result), Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0050));
            Assert.That(error.Diagnostic.Range, Is.Not.Null);
        });
    }

    [Test]
    public void CastAndCastableAtomizeAndApplyOptionalCardinality()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        string source = "('42' cast as xs:int, 'bad' castable as xs:int, " +
            "() castable as xs:int?, () castable as xs:int, ['7'] cast as xs:int)";

        XdmSequence result = Evaluate(source, context, new XPathDynamicContext());
        XdmAtomicValue[] values = result.AtomicValues!;

        Assert.Multiple(() =>
        {
            Assert.That(values.Length, Is.EqualTo(5));
            Assert.That(values[0].Type, Is.EqualTo(XdmAtomicType.Int));
            Assert.That(values[0].IntegerValue, Is.EqualTo(42));
            Assert.That(values[1].BooleanValue, Is.False);
            Assert.That(values[2].BooleanValue, Is.True);
            Assert.That(values[3].BooleanValue, Is.False);
            Assert.That(values[4].IntegerValue, Is.EqualTo(7));
        });
    }

    [Test]
    public void NumericUnionConstructorAndCastPreserveMemberTypes()
    {
        XPathStaticContext context = new XPathStaticContext();

        XdmSequence result = Evaluate("(xs:numeric('12.5'), 17 cast as xs:numeric, xs:float(3) cast as xs:numeric, true() cast as xs:numeric, '12.5' castable as xs:numeric, 'bad' castable as xs:numeric)", context, new XPathDynamicContext());
        XdmAtomicValue[] values = result.AtomicValues!;

        Assert.Multiple(() =>
        {
            Assert.That(values.Select(value => value.Type), Is.EqualTo(new[] { XdmAtomicType.Double, XdmAtomicType.Integer, XdmAtomicType.Float, XdmAtomicType.Double, XdmAtomicType.Boolean, XdmAtomicType.Boolean }));
            Assert.That(values[4].BooleanValue, Is.True);
            Assert.That(values[5].BooleanValue, Is.False);
        });
    }

    [Test]
    public void CastReportsLexicalAndCardinalityErrors()
    {
        XPathStaticContext context = new XPathStaticContext();

        XPathDynamicException lexical = Assert.Throws<XPathDynamicException>(() => Evaluate("'bad' cast as xs:int", context, new XPathDynamicContext()))!;
        XPathDynamicException cardinality = Assert.Throws<XPathDynamicException>(() => Evaluate("(1, 2) cast as xs:int", context, new XPathDynamicContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(lexical.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.That(lexical.Diagnostic.Range, Is.Not.Null);
            Assert.That(cardinality.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(cardinality.Diagnostic.Range, Is.Not.Null);
        });
    }

    [Test]
    public void ConstructorFunctionsAndCastsResolveNamespaceSensitiveValues()
    {
        XPathStaticContext context = new XPathStaticContext
        {
            AllowHigherOrderFunctions = true,
            Namespaces = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:values" }]
        };
        string source = "(xs:int('8'), (xs:int#1)('9'), xs:int(()), " +
            "xs:QName('p:first'), 'p:second' cast as xs:QName, 'q:bad' castable as xs:QName)";

        XdmSequence result = Evaluate(source, context, new XPathDynamicContext());
        XdmAtomicValue[] values = result.AtomicValues!;
        XmlName first = values[2].NameValue!;
        XmlName second = values[3].NameValue!;

        Assert.Multiple(() =>
        {
            Assert.That(values.Length, Is.EqualTo(5));
            Assert.That(values[0].IntegerValue, Is.EqualTo(8));
            Assert.That(values[1].IntegerValue, Is.EqualTo(9));
            Assert.That(first.NamespaceUri, Is.EqualTo("urn:values"));
            Assert.That(first.LocalName, Is.EqualTo("first"));
            Assert.That(second.NamespaceUri, Is.EqualTo("urn:values"));
            Assert.That(second.LocalName, Is.EqualTo("second"));
            Assert.That(values[4].BooleanValue, Is.False);
        });
    }

    [Test]
    public void SchemaAwareCastsConstructorsInstanceAndTreatUseTypeIdentityAndFacets()
    {
        XmlSchemaSet schemas = CompileSchema();
        XPathStaticContext context = SchemaContext(schemas);
        string source = "('8' cast as t:Code instance of t:Code, " +
            "t:Code('9') instance of t:Code, (t:Code#1)('10') instance of t:Code, " +
            "'0' castable as t:Code, 'true' cast as t:Choice instance of t:Choice, " +
            "'not-a-member' castable as t:Choice)";

        XdmSequence result = Evaluate(source, context, new XPathDynamicContext());
        XPathDynamicException facet = Assert.Throws<XPathDynamicException>(() => Evaluate("'0' cast as t:Code", context, new XPathDynamicContext()))!;
        XPathStaticException complex = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath("1 instance of t:Complex", context))!;

        Assert.Multiple(() =>
        {
            Assert.That(Booleans(result), Is.EqualTo(new[] { true, true, true, false, true, false }));
            Assert.That(facet.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.That(complex.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0051));
        });
    }

    [Test]
    public void SchemaAwareTreatRecognizesOnlyAnnotatedDerivedValues()
    {
        XmlSchemaSet schemas = CompileSchema();
        XPathStaticContext context = SchemaContext(schemas);
        XmlDocument document = Validate(schemas, "<value xmlns='urn:test'>7</value>");
        XdmSequence typed = new XdmTypeConverter().DeriveXdmNodeTypedValue(document.DocumentElement!);
        XPathDynamicContext dynamicContext = new XPathDynamicContext
        {
            Variables = [Variable("value", typed)]
        };

        XdmSequence result = Evaluate("($value instance of t:Code, $value treat as t:Code)", context, dynamicContext);
        XPathDynamicContext plainContext = new XPathDynamicContext
        {
            Variables = [Variable("value", new XdmSequenceOperations().CreateXdmAtomicSequence(new XdmFactory().CreateXdmIntegerAtomicValue("7", XdmAtomicType.Int)))]
        };
        XPathDynamicException mismatch = Assert.Throws<XPathDynamicException>(() => Evaluate("$value treat as t:Code", context, plainContext))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.AtomicValues![0].BooleanValue, Is.True);
            Assert.That(result.AtomicValues[1].IntegerValue, Is.EqualTo(7));
            Assert.That(mismatch.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0050));
        });
    }

    private static XdmSequence Evaluate(string source, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;
    }

    private static bool[] Booleans(XdmSequence value)
    {
        return value.AtomicValues!.Select(item => item.BooleanValue).ToArray();
    }

    private static long[] Integers(XdmSequence value)
    {
        return value.AtomicValues!.Select(item => item.IntegerValue).ToArray();
    }

    private static XPathStaticContext SchemaContext(XmlSchemaSet schemas)
    {
        XPathVariableBinding declaration = Variable("value", null);
        declaration.DeclaredType = XdmAtomicType.AnyAtomicType;
        declaration.DeclaredOccurrence = XdmOccurrence.ZeroOrMore;
        return new XPathStaticContext
        {
            SchemaAware = true,
            AllowHigherOrderFunctions = true,
            SchemaSet = schemas,
            Namespaces = [new XmlNamespaceBinding { Prefix = "t", NamespaceUri = "urn:test" }],
            Variables = [declaration]
        };
    }

    private static XPathVariableBinding Variable(string localName, XdmSequence? value)
    {
        return new XPathVariableBinding
        {
            Name = new XmlName { LocalName = localName },
            Value = value,
            DeclaredType = XdmAtomicType.AnyAtomicType,
            DeclaredOccurrence = XdmOccurrence.ZeroOrMore
        };
    }

    private static XmlSchemaSet CompileSchema()
    {
        string schema =
            "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:t='urn:test' targetNamespace='urn:test' elementFormDefault='qualified'>" +
            "<xs:simpleType name='Code'><xs:restriction base='xs:int'><xs:minInclusive value='1'/><xs:maxInclusive value='99'/></xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='Choice'><xs:union memberTypes='xs:int xs:boolean'/></xs:simpleType>" +
            "<xs:complexType name='Complex'><xs:sequence/></xs:complexType>" +
            "<xs:element name='value' type='t:Code'/>" +
            "</xs:schema>";
        return new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Kind = XmlInputKind.String, Text = schema, BaseUri = "https://example.test/types.xsd" }],
            new XmlSchemaValidationOptions { ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied, AllowedSchemes = [] } },
            new XmlDefaultResolver());
    }

    private static XmlDocument Validate(XmlSchemaSet schemas, string text)
    {
        XmlDocument document = new XmlParser().ParseXmlDocument(
            new XmlInput { Kind = XmlInputKind.String, Text = text, BaseUri = "https://example.test/value.xml" },
            new XmlParserOptions { NamespaceHandling = XmlNamespaceHandling.Namespaces10, DtdProcessing = XmlDtdProcessing.Prohibit },
            null,
            null,
            null);
        XmlSchemaValidationResult result = new XmlSchemaValidator().ValidateXmlSchema(document, schemas, new XmlSchemaValidationOptions { AnnotateTree = true });
        Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
        return document;
    }
}
