using xml;

namespace xml.Tests;

[TestFixture]
public class XPathStaticAnalyzerTests
{
    [Test]
    public void AllowsStaticallyEmptySubexpressionsInBasicXPathMode()
    {
        XPathAstNode result = Analyze("empty(avg(((), ())))", new XPathStaticContext());

        Assert.That(result.StaticType!.AtomicType, Is.EqualTo(XdmAtomicType.Boolean));
    }

    [Test]
    public void ReportsInvalidFunctionArityBeforeAPathOperandTypeError()
    {
        XPathStaticException failure = Assert.Throws<XPathStaticException>(() => new XPathCompiler().CompileXPath("(1 to 10)/count()", new XPathStaticContext()))!;

        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
    }

    [Test]
    public void InfersLiteralArithmeticSequenceAndComparisonTypes()
    {
        XPathAstNode addition = Analyze("1 + 2.0", new XPathStaticContext());
        XPathAstNode division = Analyze("1 div 2", new XPathStaticContext());
        XPathAstNode integerDivision = Analyze("1 idiv 2", new XPathStaticContext());
        XPathAstNode concatenation = Analyze("'a' || 2", new XPathStaticContext());
        XPathAstNode range = Analyze("1 to 3", new XPathStaticContext());
        XPathAstNode comparison = Analyze("1 eq 2", new XPathStaticContext());
        XPathAstNode sequence = Analyze("(1, 2)", new XPathStaticContext());

        Assert.Multiple(() =>
        {
            Assert.That(addition.StaticType!.ItemKind, Is.EqualTo(XPathStaticItemKind.Atomic));
            Assert.That(addition.StaticType.AtomicType, Is.EqualTo(XdmAtomicType.Decimal));
            Assert.That(addition.StaticType.Cardinality, Is.EqualTo(XPathStaticCardinality.ExactlyOne));
            Assert.That(division.StaticType!.AtomicType, Is.EqualTo(XdmAtomicType.Decimal));
            Assert.That(integerDivision.StaticType!.AtomicType, Is.EqualTo(XdmAtomicType.Integer));
            Assert.That(concatenation.StaticType!.AtomicType, Is.EqualTo(XdmAtomicType.String));
            Assert.That(range.StaticType!.AtomicType, Is.EqualTo(XdmAtomicType.Integer));
            Assert.That(range.StaticType.Cardinality, Is.EqualTo(XPathStaticCardinality.ZeroOrMore));
            Assert.That(comparison.StaticType!.AtomicType, Is.EqualTo(XdmAtomicType.Boolean));
            Assert.That(sequence.StaticType!.ItemKind, Is.EqualTo(XPathStaticItemKind.Atomic));
            Assert.That(sequence.StaticType.Cardinality, Is.EqualTo(XPathStaticCardinality.OneOrMore));
        });
    }

    [Test]
    public void InfersRegisteredVariableAndFunctionSignatures()
    {
        XPathVariableBinding variable = new XPathVariableBinding
        {
            Name = Name(null, "values"),
            DeclaredType = XdmAtomicType.Integer,
            DeclaredOccurrence = XdmOccurrence.ZeroOrMore
        };
        XPathFunctionBinding function = Function("urn:functions", "render", 1, XdmAtomicType.String, XdmOccurrence.ZeroOrOne);
        function.ParameterTypes = [XdmAtomicType.Decimal];
        function.ParameterOccurrences = [XdmOccurrence.ExactlyOne];
        XPathStaticContext context = new XPathStaticContext
        {
            DefaultFunctionNamespace = "urn:functions",
            Variables = [variable],
            Functions = [function]
        };

        XPathAstNode result = Analyze("($values, render(1))", context);
        XPathAstNode[] expressions = result.Operand!.Children;

        Assert.Multiple(() =>
        {
            Assert.That(expressions[0].StaticType!.AtomicType, Is.EqualTo(XdmAtomicType.Integer));
            Assert.That(expressions[0].StaticType!.Cardinality, Is.EqualTo(XPathStaticCardinality.ZeroOrMore));
            Assert.That(expressions[1].StaticType!.AtomicType, Is.EqualTo(XdmAtomicType.String));
            Assert.That(expressions[1].StaticType!.Cardinality, Is.EqualTo(XPathStaticCardinality.ZeroOrOne));
        });
    }

    [Test]
    public void InfersAxisAndPathNodeKinds()
    {
        XPathAstNode result = Analyze("self::element()/@id/parent::node()/text()", new XPathStaticContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.StaticType!.ItemKind, Is.EqualTo(XPathStaticItemKind.Node));
            Assert.That(result.StaticType.Cardinality, Is.EqualTo(XPathStaticCardinality.ZeroOrMore));
            Assert.That(result.Children[0].StaticType!.NodeKind, Is.EqualTo(XdmNodeKind.Element));
            Assert.That(result.Children[0].StaticType!.Cardinality, Is.EqualTo(XPathStaticCardinality.ZeroOrOne));
            Assert.That(result.Children[1].StaticType!.NodeKind, Is.EqualTo(XdmNodeKind.Attribute));
            Assert.That(result.Children[2].StaticType!.AnyNode, Is.True);
            Assert.That(result.Children[3].StaticType!.NodeKind, Is.EqualTo(XdmNodeKind.Text));
        });
    }

    [Test]
    public void InfersHigherOrderFunctionsMapsAndArrays()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };
        XPathAstNode result = Analyze("(map {'a': 1}, [1], fn:concat(?, 'x'), fn:concat#2, function($x as xs:string) as xs:string {$x}, fn:concat#2('a', 'b'))", context);
        XPathAstNode[] expressions = result.Operand!.Children;

        Assert.Multiple(() =>
        {
            Assert.That(expressions[0].StaticType!.ItemKind, Is.EqualTo(XPathStaticItemKind.Map));
            Assert.That(expressions[1].StaticType!.ItemKind, Is.EqualTo(XPathStaticItemKind.Array));
            Assert.That(expressions[2].StaticType!.ItemKind, Is.EqualTo(XPathStaticItemKind.Function));
            Assert.That(expressions[3].StaticType!.ItemKind, Is.EqualTo(XPathStaticItemKind.Function));
            Assert.That(expressions[4].StaticType!.ItemKind, Is.EqualTo(XPathStaticItemKind.Function));
            Assert.That(expressions[4].Body!.StaticType!.AtomicType, Is.EqualTo(XdmAtomicType.String));
            Assert.That(expressions[5].StaticType!.ItemKind, Is.EqualTo(XPathStaticItemKind.Unknown));
        });
    }

    [Test]
    public void AppliesFunctionConversionAndNumericPromotionChecks()
    {
        XPathFunctionBinding acceptsDecimal = Function("urn:functions", "decimal", 1, XdmAtomicType.Boolean, XdmOccurrence.ExactlyOne);
        acceptsDecimal.ParameterTypes = [XdmAtomicType.Decimal];
        acceptsDecimal.ParameterOccurrences = [XdmOccurrence.ExactlyOne];
        XPathFunctionBinding acceptsQName = Function("urn:functions", "qname", 1, XdmAtomicType.Boolean, XdmOccurrence.ExactlyOne);
        acceptsQName.ParameterTypes = [XdmAtomicType.QName];
        acceptsQName.ParameterOccurrences = [XdmOccurrence.ExactlyOne];
        XPathStaticContext numericContext = new XPathStaticContext
        {
            DefaultFunctionNamespace = "urn:functions",
            Functions = [acceptsDecimal]
        };
        XPathStaticContext qnameContext = new XPathStaticContext
        {
            DefaultFunctionNamespace = "urn:functions",
            Functions = [acceptsQName],
            Variables =
            [
                new XPathVariableBinding
                {
                    Name = Name(null, "untyped"),
                    DeclaredType = XdmAtomicType.UntypedAtomic,
                    DeclaredOccurrence = XdmOccurrence.ExactlyOne
                }
            ]
        };

        XPathAstNode promoted = Analyze("decimal(1)", numericContext);
        XPathTypeException wrongType = Assert.Throws<XPathTypeException>(() => Analyze("decimal('one')", numericContext))!;
        XPathTypeException namespaceSensitive = Assert.Throws<XPathTypeException>(() => Analyze("qname($untyped)", qnameContext))!;

        Assert.Multiple(() =>
        {
            Assert.That(promoted.StaticType!.AtomicType, Is.EqualTo(XdmAtomicType.Boolean));
            Assert.That(wrongType.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(wrongType.Diagnostic.Range!.Start!.CharacterOffset, Is.EqualTo(8));
            Assert.That(namespaceSensitive.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0117));
        });
    }

    [TestCase("$unknown", "XPST0008")]
    [TestCase("no-such-function()", "XPST0017")]
    [TestCase("fn:concat('only-one')", "XPST0017")]
    [TestCase("1 instance of xs:notAType", "XPST0051")]
    [TestCase("1 instance of item", "XPST0051")]
    [TestCase("1 cast as xs:anyAtomicType", "XPST0080")]
    [TestCase("1 cast as xs:NOTATION", "XPST0080")]
    public void ReportsStaticNameFunctionAndTypeErrors(string source, string expectedCode)
    {
        XPathStaticException exception = Assert.Throws<XPathStaticException>(() => Analyze(source, new XPathStaticContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(expectedCode));
            Assert.That(exception.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.XPathStatic));
            Assert.That(exception.Diagnostic.Range, Is.Not.Null);
            Assert.That(exception.Location, Is.SameAs(exception.Diagnostic.Range!.Start));
        });
    }

    [TestCase("insert node a into b")]
    [TestCase("delete node a")]
    [TestCase("replace node a with b")]
    [TestCase("declare namespace p = 'urn:p'; p:a")]
    public void RejectsUpdatingAndDeclarationSyntax(string source)
    {
        XPathStaticException exception = Assert.Throws<XPathStaticException>(() => Analyze(source, new XPathStaticContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0003));
            Assert.That(exception.Diagnostic.Range, Is.Not.Null);
            Assert.That(exception.Location, Is.SameAs(exception.Diagnostic.Range!.Start));
        });
    }

    [TestCase("1 is 2", "XPTY0004")]
    [TestCase("1/a", "XPTY0019")]
    [TestCase("1 union 2", "XPTY0004")]
    [TestCase("1?key", "XPTY0004")]
    [TestCase("map {(): 1}", "XPTY0004")]
    public void ReportsStaticallyCertainTypeErrors(string source, string expectedCode)
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };

        XPathTypeException exception = Assert.Throws<XPathTypeException>(() => Analyze(source, context))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(expectedCode));
            Assert.That(exception.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.XPathType));
            Assert.That(exception.Diagnostic.Range, Is.Not.Null);
            Assert.That(exception.Location, Is.SameAs(exception.Diagnostic.Range!.Start));
        });
    }

    [Test]
    public void ReportsTheDefinedErrorForAStaticallyKnownInvalidEffectiveBooleanValue()
    {
        XPathStaticException exception = Assert.Throws<XPathStaticException>(() => Analyze("map {} and true()", new XPathStaticContext { AllowHigherOrderFunctions = true }))!;

        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0006));
    }

    [TestCase("fn:concat#2")]
    [TestCase("fn:concat(?, 'x')")]
    [TestCase("function($x) {$x}")]
    [TestCase("map {}")]
    [TestCase("[1]")]
    public void EnforcesTheHigherOrderFunctionFeatureFlag(string source)
    {
        XPathStaticException exception = Assert.Throws<XPathStaticException>(() => Analyze(source, new XPathStaticContext()))!;

        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0017));
    }

    [Test]
    public void HonorsHigherOrderEnablementThroughTheFeatureRegistry()
    {
        XPathStaticContext context = new XPathStaticContext { EnabledFeatures = ["higher-order-functions"] };

        XPathAstNode result = Analyze("fn:concat#2", context);

        Assert.That(result.StaticType!.ItemKind, Is.EqualTo(XPathStaticItemKind.Function));
    }

    [Test]
    public void RejectsDuplicateInlineParametersAndUnavailableSchemaTests()
    {
        XPathStaticContext higherOrder = new XPathStaticContext { AllowHigherOrderFunctions = true };

        XPathStaticException duplicate = Assert.Throws<XPathStaticException>(() => Analyze("function($x, $x) {$x}", higherOrder))!;
        XPathStaticException schema = Assert.Throws<XPathStaticException>(() => Analyze("schema-element(missing)", new XPathStaticContext()))!;

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XQST0039));
            Assert.That(schema.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0008));
        });
    }

    [Test]
    public void ValidatesDeclaredInlineFunctionResultTypes()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };

        XPathTypeException exception = Assert.Throws<XPathTypeException>(() => Analyze("function() as xs:string {1}", context))!;

        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
    }

    [Test]
    public void DefersBroadAtomicFunctionResultsToDynamicTypeChecking()
    {
        XPathStaticContext context = new XPathStaticContext { AllowHigherOrderFunctions = true };

        XPathAstNode result = Analyze("function() as xs:integer {sum((1, 2))}", context);

        Assert.That(result.StaticType!.ItemKind, Is.EqualTo(XPathStaticItemKind.Function));
    }

    private static XPathAstNode Analyze(string source, XPathStaticContext context)
    {
        return new XPathStaticAnalyzer(context).AnalyzeXPathSource(source, "urn:static-test");
    }

    private static XmlName Name(string? namespaceUri, string localName)
    {
        return new XmlName { NamespaceUri = namespaceUri, LocalName = localName, QualifiedName = localName };
    }

    private static XPathFunctionBinding Function(string namespaceUri, string localName, int arity, XdmAtomicType resultType, XdmOccurrence resultOccurrence)
    {
        return new XPathFunctionBinding
        {
            Name = Name(namespaceUri, localName),
            Arity = arity,
            ResultType = resultType,
            ResultOccurrence = resultOccurrence,
            BindingKey = localName
        };
    }
}
