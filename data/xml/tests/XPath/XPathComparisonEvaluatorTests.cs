using xml;

namespace xml.Tests;

[TestFixture]
public class XPathComparisonEvaluatorTests
{
    [Test]
    public void GeneralComparisonsUseExistentialPairSemantics()
    {
        XPathVariableBinding left = Binding("left", Sequence(Integer("1"), Integer("2")), XdmAtomicType.Integer, XdmOccurrence.OneOrMore);
        XPathVariableBinding right = Binding("right", Sequence(Integer("2"), Integer("3")), XdmAtomicType.Integer, XdmOccurrence.OneOrMore);

        Assert.Multiple(() =>
        {
            Assert.That(Boolean(Evaluate("$left = $right", [left, right])), Is.True);
            Assert.That(Boolean(Evaluate("$left != $right", [left, right])), Is.True);
            Assert.That(Boolean(Evaluate("$left > 5", [left])), Is.False);
            Assert.That(Boolean(Evaluate("$left = ()", [left])), Is.False);
        });
    }

    [Test]
    public void ComparisonsAtomizeNodesArraysAndConvertUntypedValues()
    {
        XmlDocument document = Parse("<root><value>02</value></root>");
        XPathDynamicContext context = Context(document.DocumentNode!);
        XdmArrayValue array = new XdmArrayOperations().CreateXdmArray([Sequence(String("miss")), Sequence(String("hit"))]);
        XPathVariableBinding values = Binding("values", Sequence(array), XdmAtomicType.String, XdmOccurrence.OneOrMore);

        Assert.Multiple(() =>
        {
            Assert.That(Boolean(Evaluate("/root/value = 2", [], context)), Is.True);
            Assert.That(Boolean(Evaluate("/root/value eq '02'", [], context)), Is.True);
            Assert.That(Boolean(Evaluate("$values = 'hit'", [values])), Is.True);
        });
    }

    [Test]
    public void ValueComparisonsPreserveEmptyAndRejectMultipleValues()
    {
        XPathVariableBinding empty = Binding("empty", Sequence(), XdmAtomicType.Integer, XdmOccurrence.ZeroOrOne);
        XPathVariableBinding many = Binding("many", Sequence(Integer("1"), Integer("2")), XdmAtomicType.Integer, XdmOccurrence.ZeroOrMore);

        XdmSequence emptyResult = Evaluate("$empty eq 1", [empty]);
        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate("$many eq 1", [many]))!;

        Assert.Multiple(() =>
        {
            Assert.That(emptyResult.Count, Is.EqualTo(0));
            Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(error.Diagnostic.Range, Is.Not.Null);
        });
    }

    [Test]
    public void NumericComparisonsPromoteTypesAndApplyNaNSemantics()
    {
        XPathVariableBinding integer = Binding("integer", Sequence(Integer("16777217")), XdmAtomicType.Integer, XdmOccurrence.ExactlyOne);
        XPathVariableBinding single = Binding("single", Sequence(Float("16777216")), XdmAtomicType.Float, XdmOccurrence.ExactlyOne);
        XPathVariableBinding nan = Binding("nan", Sequence(Double("NaN")), XdmAtomicType.Double, XdmOccurrence.ExactlyOne);

        Assert.Multiple(() =>
        {
            Assert.That(Boolean(Evaluate("$integer eq $single", [integer, single])), Is.True);
            Assert.That(Boolean(Evaluate("$nan eq $nan", [nan])), Is.False);
            Assert.That(Boolean(Evaluate("$nan ne $nan", [nan])), Is.True);
            Assert.That(Boolean(Evaluate("$nan lt 1", [nan])), Is.False);
        });
    }

    [Test]
    public void UsesUnicodeCodepointCollationForStringsAndAnyUris()
    {
        string bmp = "\uE000";
        string supplementary = "\U00010000";
        XPathVariableBinding uri = Binding("uri", Sequence(new XdmFactory().CreateXdmAnyUriAtomicValue("urn:item")), XdmAtomicType.AnyUri, XdmOccurrence.ExactlyOne);

        Assert.Multiple(() =>
        {
            Assert.That(new XPathCodepointCollation().CompareXPathCollation(bmp, supplementary), Is.LessThan(0));
            Assert.That(Boolean(Evaluate("'\uE000' lt '\U00010000'", [])), Is.True);
            Assert.That(Boolean(Evaluate("$uri eq 'urn:item'", [uri])), Is.True);
        });
    }

    [Test]
    public void ComparesBooleanTemporalDurationBinaryAndQNameValues()
    {
        XdmFactory factory = new XdmFactory();
        XPathStaticContext qnameContext = new XPathStaticContext { Namespaces = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:names" }] };
        XPathVariableBinding falseValue = Binding("falseValue", Sequence(factory.CreateXdmBooleanAtomicValue("false")), XdmAtomicType.Boolean, XdmOccurrence.ExactlyOne);
        XPathVariableBinding trueValue = Binding("trueValue", Sequence(factory.CreateXdmBooleanAtomicValue("true")), XdmAtomicType.Boolean, XdmOccurrence.ExactlyOne);
        XPathVariableBinding firstDate = Binding("firstDate", Sequence(factory.CreateXdmDateTimeAtomicValue("2026-01-01Z", XdmAtomicType.Date)), XdmAtomicType.Date, XdmOccurrence.ExactlyOne);
        XPathVariableBinding secondDate = Binding("secondDate", Sequence(factory.CreateXdmDateTimeAtomicValue("2026-01-02Z", XdmAtomicType.Date)), XdmAtomicType.Date, XdmOccurrence.ExactlyOne);
        XPathVariableBinding shortDuration = Binding("shortDuration", Sequence(factory.CreateXdmDurationAtomicValue("P1M", XdmAtomicType.YearMonthDuration)), XdmAtomicType.YearMonthDuration, XdmOccurrence.ExactlyOne);
        XPathVariableBinding longDuration = Binding("longDuration", Sequence(factory.CreateXdmDurationAtomicValue("P2M", XdmAtomicType.YearMonthDuration)), XdmAtomicType.YearMonthDuration, XdmOccurrence.ExactlyOne);
        XPathVariableBinding lowBinary = Binding("lowBinary", Sequence(factory.CreateXdmBinaryAtomicValue("01", XdmAtomicType.HexBinary)), XdmAtomicType.HexBinary, XdmOccurrence.ExactlyOne);
        XPathVariableBinding highBinary = Binding("highBinary", Sequence(factory.CreateXdmBinaryAtomicValue("02", XdmAtomicType.HexBinary)), XdmAtomicType.HexBinary, XdmOccurrence.ExactlyOne);
        XPathVariableBinding firstName = Binding("firstName", Sequence(factory.CreateXdmQNameAtomicValue("p:item", qnameContext)), XdmAtomicType.QName, XdmOccurrence.ExactlyOne);
        XPathVariableBinding secondName = Binding("secondName", Sequence(factory.CreateXdmQNameAtomicValue("p:item", qnameContext)), XdmAtomicType.QName, XdmOccurrence.ExactlyOne);

        Assert.Multiple(() =>
        {
            Assert.That(Boolean(Evaluate("$falseValue lt $trueValue", [falseValue, trueValue])), Is.True);
            Assert.That(Boolean(Evaluate("$firstDate lt $secondDate", [firstDate, secondDate])), Is.True);
            Assert.That(Boolean(Evaluate("$shortDuration lt $longDuration", [shortDuration, longDuration])), Is.True);
            Assert.That(Boolean(Evaluate("$lowBinary lt $highBinary", [lowBinary, highBinary])), Is.True);
            Assert.That(Boolean(Evaluate("$firstName eq $secondName", [firstName, secondName])), Is.True);
        });
    }

    [Test]
    public void ReportsIncompatibleAtomicTypesAndUnsupportedOrdering()
    {
        XPathVariableBinding text = Binding("text", Sequence(String("true")), XdmAtomicType.String, XdmOccurrence.ExactlyOne);
        XPathVariableBinding boolean = Binding("boolean", Sequence(new XdmFactory().CreateXdmBooleanAtomicValue("true")), XdmAtomicType.Boolean, XdmOccurrence.ExactlyOne);
        XPathStaticContext qnameContext = new XPathStaticContext { Namespaces = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:names" }] };
        XPathVariableBinding name = Binding("name", Sequence(new XdmFactory().CreateXdmQNameAtomicValue("p:item", qnameContext)), XdmAtomicType.QName, XdmOccurrence.ExactlyOne);

        XPathDynamicException incompatible = Assert.Throws<XPathDynamicException>(() => Evaluate("$text eq $boolean", [text, boolean]))!;
        XPathDynamicException ordering = Assert.Throws<XPathDynamicException>(() => Evaluate("$name lt $name", [name]))!;

        Assert.Multiple(() =>
        {
            Assert.That(incompatible.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(ordering.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    [Test]
    public void DistinguishesValueAndGeneralUntypedConversionFailures()
    {
        XmlDocument numericDocument = Parse("<root><value>2</value></root>");
        XmlDocument invalidDocument = Parse("<root><value>not-a-number</value></root>");

        XPathDynamicException valueType = Assert.Throws<XPathDynamicException>(() => Evaluate("/root/value eq 2", [], Context(numericDocument.DocumentNode!)))!;
        XPathDynamicException lexical = Assert.Throws<XPathDynamicException>(() => Evaluate("/root/value = 2", [], Context(invalidDocument.DocumentNode!)))!;

        Assert.Multiple(() =>
        {
            Assert.That(valueType.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(lexical.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
        });
    }

    [Test]
    public void RejectsADeclaredCollationWithoutAnImplementation()
    {
        const string collation = "urn:unsupported-collation";
        XPathStaticContext context = new XPathStaticContext
        {
            DefaultCollation = collation,
            Collations = [collation]
        };

        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => Evaluate("'a' lt 'b'", context, new XPathDynamicContext()))!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOCH0002));
    }

    [Test]
    public void NodeComparisonsHandleIdentityOrderEmptyAndCardinality()
    {
        XmlDocument document = Parse("<root><a/><b/></root>");
        XPathDynamicContext context = Context(document.DocumentNode!);

        XdmSequence empty = Evaluate("/root/missing is /root/a", [], context);
        XPathDynamicException cardinality = Assert.Throws<XPathDynamicException>(() => Evaluate("/root/* is /root/a", [], context))!;

        Assert.Multiple(() =>
        {
            Assert.That(Boolean(Evaluate("/root/a is /root/a", [], context)), Is.True);
            Assert.That(Boolean(Evaluate("/root/a is /root/b", [], context)), Is.False);
            Assert.That(Boolean(Evaluate("/root/a << /root/b", [], context)), Is.True);
            Assert.That(Boolean(Evaluate("/root/b >> /root/a", [], context)), Is.True);
            Assert.That(empty.Count, Is.EqualTo(0));
            Assert.That(cardinality.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    [Test]
    public void BackwardsCompatibleBooleanComparisonUsesEffectiveBooleanValue()
    {
        XPathVariableBinding text = Binding("text", Sequence(String("value")), XdmAtomicType.String, XdmOccurrence.ExactlyOne);
        XPathVariableBinding falseValue = Binding("falseValue", Sequence(new XdmFactory().CreateXdmBooleanAtomicValue("false")), XdmAtomicType.Boolean, XdmOccurrence.ExactlyOne);
        XPathStaticContext context = new XPathStaticContext
        {
            CompatibilityMode = XPathCompatibilityMode.BackwardsCompatible,
            Variables = [text, falseValue]
        };

        XdmSequence value = Evaluate("$falseValue = $text", context, new XPathDynamicContext());

        Assert.That(Boolean(value), Is.False);
    }

    [Test]
    public void RejectsDocumentOrderComparisonForStreamedNodes()
    {
        XmlDocument document = Parse("<root><a/><b/></root>");
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath("/root/a << /root/b", new XPathStaticContext());
        XPathRuntime runtime = new XPathRuntime((XPathPlan)expression.Plan!, Context(document.DocumentNode!));
        runtime.Paths.MarkXPathStreamedNode(document.DocumentElement!.Children![0]);

        XPathDynamicException error = Assert.Throws<XPathDynamicException>(() => runtime.EvaluateXPathPlan())!;

        Assert.That(error.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0130));
    }

    private static XdmSequence Evaluate(string source, XPathVariableBinding[] variables)
    {
        return Evaluate(source, new XPathStaticContext { Variables = variables }, new XPathDynamicContext());
    }

    private static XdmSequence Evaluate(string source, XPathVariableBinding[] variables, XPathDynamicContext dynamicContext)
    {
        return Evaluate(source, new XPathStaticContext { Variables = variables }, dynamicContext);
    }

    private static XdmSequence Evaluate(string source, XPathStaticContext staticContext, XPathDynamicContext dynamicContext)
    {
        XPathCompiledExpression expression = new XPathCompiler().CompileXPath(source, staticContext);
        return new XPathEvaluator().EvaluateXPath(expression, dynamicContext).Value!;
    }

    private static XPathVariableBinding Binding(string name, XdmSequence value, XdmAtomicType type, XdmOccurrence occurrence)
    {
        return new XPathVariableBinding
        {
            Name = new XmlName { LocalName = name, NamespaceUri = "", QualifiedName = name },
            Value = value,
            DeclaredType = type,
            DeclaredOccurrence = occurrence
        };
    }

    private static XdmSequence Sequence(params XdmAtomicValue[] values)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        int index = 0;
        while (index < values.Length)
        {
            builder.AddXdmAtomicValue(values[index]);
            index = index + 1;
        }

        return builder.BuildImmutableXdmSequence();
    }

    private static XdmSequence Sequence(XdmArrayValue value)
    {
        return new XdmSequenceOperations().CreateXdmArraySequence(value);
    }

    private static XdmAtomicValue Integer(string value)
    {
        return new XdmFactory().CreateXdmIntegerAtomicValue(value, XdmAtomicType.Integer);
    }

    private static XdmAtomicValue Float(string value)
    {
        return new XdmFactory().CreateXdmFloatingAtomicValue(value, XdmAtomicType.Float);
    }

    private static XdmAtomicValue Double(string value)
    {
        return new XdmFactory().CreateXdmFloatingAtomicValue(value, XdmAtomicType.Double);
    }

    private static XdmAtomicValue String(string value)
    {
        return new XdmFactory().CreateXdmStringAtomicValue(value, XdmAtomicType.String);
    }

    private static bool Boolean(XdmSequence value)
    {
        return value.AtomicValues![0].BooleanValue;
    }

    private static XPathDynamicContext Context(XmlNode node)
    {
        return new XPathDynamicContext { ContextItem = new XdmSequenceOperations().CreateXdmNodeSequence(node) };
    }

    private static XmlDocument Parse(string source)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Text = source }, options, null, null, null);
    }
}
