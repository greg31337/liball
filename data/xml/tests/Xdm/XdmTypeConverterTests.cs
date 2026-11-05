using xml;

namespace xml.Tests;

[TestFixture]
public class XdmTypeConverterTests
{
    [Test]
    public void AtomizationRetainsAtomicsDerivesNodeValuesAndFlattensNestedArrays()
    {
        XdmAtomicValue direct = new XdmStringDatatype().ParseXdmStringAtomic("direct", XdmAtomicType.String);
        XmlNode text = new XmlNode { Kind = XmlNodeKind.Text, Value = "node" };
        XdmSequence member = Sequence(Atomic("7", XdmAtomicType.Int), text);
        XdmArrayValue nested = new XdmArrayValue
        {
            Count = 1,
            Members = [new XdmSequenceOperations().CreateXdmAtomicSequence(Atomic("tail", XdmAtomicType.String))],
            IsImmutable = true
        };
        XdmArrayValue array = new XdmArrayValue
        {
            Count = 2,
            Members = [member, new XdmSequenceOperations().CreateXdmArraySequence(nested)],
            IsImmutable = true
        };

        XdmSequence result = new XdmTypeConverter().AtomizeXdmSequence(Sequence(direct, array));

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(4));
            Assert.That(result.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Atomic, XdmItemKind.Atomic, XdmItemKind.Atomic, XdmItemKind.Atomic }));
            Assert.That(result.AtomicValues![0], Is.SameAs(direct));
            Assert.That(result.AtomicValues[1].Type, Is.EqualTo(XdmAtomicType.Int));
            Assert.That(result.AtomicValues[1].CanonicalValue, Is.EqualTo("7"));
            Assert.That(result.AtomicValues[2].Type, Is.EqualTo(XdmAtomicType.UntypedAtomic));
            Assert.That(result.AtomicValues[2].StringValue, Is.EqualTo("node"));
            Assert.That(result.AtomicValues[3].StringValue, Is.EqualTo("tail"));
            Assert.That(result.IsImmutable, Is.True);
        });
    }

    [Test]
    public void AtomizationRejectsFunctionsMapsAndMalformedArrays()
    {
        XdmTypeConverter converter = new XdmTypeConverter();
        XdmSequenceOperations sequences = new XdmSequenceOperations();

        XPathDynamicException functionError = Assert.Throws<XPathDynamicException>(() => converter.AtomizeXdmSequence(sequences.CreateXdmFunctionSequence(new XdmFunctionItem())))!;
        XPathDynamicException mapError = Assert.Throws<XPathDynamicException>(() => converter.AtomizeXdmSequence(sequences.CreateXdmMapSequence(new XdmMapValue())))!;
        XdmArrayValue malformed = new XdmArrayValue { Count = 1, Members = [] };

        Assert.Multiple(() =>
        {
            Assert.That(functionError.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOTY0013));
            Assert.That(mapError.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOTY0013));
            Assert.Throws<ArgumentException>(() => converter.AtomizeXdmSequence(sequences.CreateXdmArraySequence(malformed)));
        });
    }

    [Test]
    public void TypedValueDerivationParsesSchemaAnnotationsAndAppliesNodeDefaults()
    {
        XmlSchemaTypeInfo annotation = new XmlSchemaTypeInfo
        {
            AtomicType = XdmAtomicType.Int,
            NormalizedValue = "0042"
        };
        XmlNode element = new XmlNode { Kind = XmlNodeKind.Element, TypeInfo = annotation };
        XmlNode comment = new XmlNode { Kind = XmlNodeKind.Comment, Value = "comment" };
        XdmNodeAccessor accessor = new XdmNodeAccessor();

        XdmAtomicValue typed = accessor.GetXdmNodeTypedValue(accessor.WrapXmlNode(element)).AtomicValues![0];
        XdmAtomicValue untyped = accessor.GetXdmNodeTypedValue(accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.Attribute, Value = "raw" })).AtomicValues![0];
        XdmAtomicValue stringValue = accessor.GetXdmNodeTypedValue(accessor.WrapXmlNode(comment)).AtomicValues![0];

        Assert.Multiple(() =>
        {
            Assert.That(typed.Type, Is.EqualTo(XdmAtomicType.Int));
            Assert.That(typed.CanonicalValue, Is.EqualTo("42"));
            Assert.That(typed.TypeInfo, Is.SameAs(annotation));
            Assert.That(untyped.Type, Is.EqualTo(XdmAtomicType.UntypedAtomic));
            Assert.That(untyped.StringValue, Is.EqualTo("raw"));
            Assert.That(stringValue.Type, Is.EqualTo(XdmAtomicType.String));
            Assert.That(stringValue.StringValue, Is.EqualTo("comment"));
        });

        annotation.IsNilled = true;
        Assert.That(accessor.GetXdmNodeTypedValue(accessor.WrapXmlNode(element)).Count, Is.Zero);
    }

    [Test]
    public void TypedValueDerivationUsesActualValuesAndReportsUnavailableTypedValues()
    {
        XdmAtomicValue actual = Atomic("12", XdmAtomicType.Integer);
        XmlSchemaTypeInfo assessed = new XmlSchemaTypeInfo { AtomicType = XdmAtomicType.Integer, ActualValue = actual };
        XmlNode assessedNode = new XmlNode { Kind = XmlNodeKind.Attribute, TypeInfo = assessed };
        XmlSchemaTypeInfo complex = new XmlSchemaTypeInfo
        {
            AtomicType = XdmAtomicType.AnyAtomicType,
            TypeName = Name("urn:types", "complex")
        };
        XmlNode complexNode = new XmlNode { Kind = XmlNodeKind.Element, TypeInfo = complex };
        XdmTypeConverter converter = new XdmTypeConverter();

        Assert.That(converter.DeriveXdmNodeTypedValue(assessedNode).AtomicValues![0], Is.SameAs(actual));
        XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => converter.DeriveXdmNodeTypedValue(complexNode))!;
        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOTY0012));
    }

    [Test]
    public void EffectiveBooleanValueHandlesNodesStringsBooleansAndNumerics()
    {
        XdmTypeConverter converter = new XdmTypeConverter();
        XdmSequenceOperations sequences = new XdmSequenceOperations();

        Assert.Multiple(() =>
        {
            Assert.That(converter.EffectiveBooleanValueXdmSequence(sequences.CreateEmptyXdmSequence()), Is.False);
            Assert.That(converter.EffectiveBooleanValueXdmSequence(Sequence(new XmlNode { Kind = XmlNodeKind.Element }, new XmlNode { Kind = XmlNodeKind.Text })), Is.True);
            Assert.That(converter.EffectiveBooleanValueXdmSequence(Sequence(new XmlNode { Kind = XmlNodeKind.Text }, Atomic("x", XdmAtomicType.String))), Is.True);
            Assert.That(converter.EffectiveBooleanValueXdmSequence(sequences.CreateXdmAtomicSequence(Atomic("true", XdmAtomicType.Boolean))), Is.True);
            Assert.That(converter.EffectiveBooleanValueXdmSequence(sequences.CreateXdmAtomicSequence(Atomic("", XdmAtomicType.String))), Is.False);
            Assert.That(converter.EffectiveBooleanValueXdmSequence(sequences.CreateXdmAtomicSequence(converter.CreateXdmUntypedAtomicValue("x"))), Is.True);
            Assert.That(converter.EffectiveBooleanValueXdmSequence(sequences.CreateXdmAtomicSequence(Atomic("0", XdmAtomicType.Decimal))), Is.False);
            Assert.That(converter.EffectiveBooleanValueXdmSequence(sequences.CreateXdmAtomicSequence(Atomic("NaN", XdmAtomicType.Double))), Is.False);
            Assert.That(converter.EffectiveBooleanValueXdmSequence(sequences.CreateXdmAtomicSequence(Atomic("-3", XdmAtomicType.Int))), Is.True);
        });

        XPathDynamicException multipleAtomics = Assert.Throws<XPathDynamicException>(() => converter.EffectiveBooleanValueXdmSequence(Sequence(Atomic("a", XdmAtomicType.String), Atomic("b", XdmAtomicType.String))))!;
        XPathDynamicException date = Assert.Throws<XPathDynamicException>(() => converter.EffectiveBooleanValueXdmSequence(sequences.CreateXdmAtomicSequence(Atomic("2024-01-01", XdmAtomicType.Date))))!;

        Assert.Multiple(() =>
        {
            Assert.That(multipleAtomics.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0006));
            Assert.That(date.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0006));
        });
    }

    [Test]
    public void SequenceTypeMatchingCombinesItemKindsSubtypesAndOccurrences()
    {
        XdmTypeConverter converter = new XdmTypeConverter();
        XdmSequenceOperations sequences = new XdmSequenceOperations();
        XdmSequence integer = sequences.CreateXdmAtomicSequence(Atomic("7", XdmAtomicType.Int));
        XdmSequence element = sequences.CreateXdmNodeSequence(new XmlNode { Kind = XmlNodeKind.Element });
        XdmSequence functions = Sequence(new XdmFunctionItem(), new XdmMapValue(), new XdmArrayValue { Count = 0, Members = [] });

        Assert.Multiple(() =>
        {
            Assert.That(converter.MatchesXdmSequenceType(integer, AtomicSequenceType(XdmAtomicType.Integer, XdmOccurrence.ExactlyOne)), Is.True);
            Assert.That(converter.MatchesXdmSequenceType(integer, AtomicSequenceType(XdmAtomicType.Byte, XdmOccurrence.ExactlyOne)), Is.False);
            Assert.That(converter.MatchesXdmSequenceType(sequences.CreateEmptyXdmSequence(), AtomicSequenceType(XdmAtomicType.String, XdmOccurrence.ZeroOrOne)), Is.True);
            Assert.That(converter.MatchesXdmSequenceType(Sequence(Atomic("a", XdmAtomicType.String), Atomic("b", XdmAtomicType.String)), AtomicSequenceType(XdmAtomicType.String, XdmOccurrence.OneOrMore)), Is.True);
            Assert.That(converter.MatchesXdmSequenceType(element, NodeSequenceType(XdmNodeKind.Element)), Is.True);
            Assert.That(converter.MatchesXdmSequenceType(element, NodeSequenceType(XdmNodeKind.Comment)), Is.False);
            Assert.That(converter.MatchesXdmSequenceType(functions, AnyFunctionSequenceType()), Is.True);
            Assert.That(converter.MatchesXdmSequenceType(Sequence(Atomic("x", XdmAtomicType.String), new XmlNode { Kind = XmlNodeKind.Text }), AnyItemSequenceType()), Is.True);
        });
    }

    [Test]
    public void FunctionConversionAtomizesCastsUntypedAndAppliesPromotions()
    {
        XdmTypeConverter converter = new XdmTypeConverter();
        XPathStaticContext context = new XPathStaticContext();
        XmlNode node = new XmlNode { Kind = XmlNodeKind.Text, Value = "42" };
        XdmSequence converted = converter.ApplyXdmFunctionConversion(Sequence(node), AtomicSequenceType(XdmAtomicType.Int, XdmOccurrence.ExactlyOne), context);
        XdmSequence promoted = converter.ApplyXdmFunctionConversion(Sequence(Atomic("0.5", XdmAtomicType.Decimal)), AtomicSequenceType(XdmAtomicType.Double, XdmOccurrence.ExactlyOne), context);
        XdmSequence uriPromoted = converter.ApplyXdmFunctionConversion(Sequence(Atomic("https://example.test/a b", XdmAtomicType.AnyUri)), AtomicSequenceType(XdmAtomicType.String, XdmOccurrence.ExactlyOne), context);

        Assert.Multiple(() =>
        {
            Assert.That(converted.AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.Int));
            Assert.That(converted.AtomicValues[0].CanonicalValue, Is.EqualTo("42"));
            Assert.That(promoted.AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(promoted.AtomicValues[0].FloatingValue, Is.EqualTo(0.5d));
            Assert.That(uriPromoted.AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.String));
            Assert.That(uriPromoted.AtomicValues[0].StringValue, Is.EqualTo("https://example.test/a b"));
        });

        XPathTypeException namespaceSensitive = Assert.Throws<XPathTypeException>(() => converter.ApplyXdmFunctionConversion(Sequence(converter.CreateXdmUntypedAtomicValue("p:item")), AtomicSequenceType(XdmAtomicType.QName, XdmOccurrence.ExactlyOne), context))!;
        XPathTypeException cardinality = Assert.Throws<XPathTypeException>(() => converter.ApplyXdmFunctionConversion(Sequence(Atomic("1", XdmAtomicType.Int), Atomic("2", XdmAtomicType.Int)), AtomicSequenceType(XdmAtomicType.Int, XdmOccurrence.ExactlyOne), context))!;
        Assert.Multiple(() =>
        {
            Assert.That(namespaceSensitive.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0117));
            Assert.That(cardinality.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
        });
    }

    [Test]
    public void CastsLexicalValuesWithStaticNamespacesAndReportsLexicalFailures()
    {
        XdmTypeConverter converter = new XdmTypeConverter();
        XPathStaticContext context = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:people" }]
        };
        XdmAtomicValue lexical = Atomic(" p:item ", XdmAtomicType.String);

        XdmAtomicValue qname = converter.CastXdmAtomic(lexical, XdmAtomicType.QName, context);
        XdmAtomicValue integer = converter.CastXdmAtomic(Atomic(" +0042 ", XdmAtomicType.String), XdmAtomicType.Int, context);
        XdmAtomicValue boolean = converter.CastXdmAtomic(Atomic("1", XdmAtomicType.String), XdmAtomicType.Boolean, context);
        XdmAtomicValue token = converter.CastXdmAtomic(Atomic("42", XdmAtomicType.Int), XdmAtomicType.Token, context);

        Assert.Multiple(() =>
        {
            Assert.That(qname.NameValue!.NamespaceUri, Is.EqualTo("urn:people"));
            Assert.That(qname.NameValue.LocalName, Is.EqualTo("item"));
            Assert.That(integer.CanonicalValue, Is.EqualTo("42"));
            Assert.That(boolean.BooleanValue, Is.True);
            Assert.That(token.Type, Is.EqualTo(XdmAtomicType.Token));
            Assert.That(token.StringValue, Is.EqualTo("42"));
            Assert.That(converter.CastableXdmAtomic(Atomic("128", XdmAtomicType.String), XdmAtomicType.Byte, context), Is.False);
            Assert.That(converter.CastableXdmAtomic(Atomic("not-a-date", XdmAtomicType.String), XdmAtomicType.Date, context), Is.False);
        });

        XPathDynamicException failure = Assert.Throws<XPathDynamicException>(() => converter.CastXdmAtomic(Atomic("truthy", XdmAtomicType.String), XdmAtomicType.Boolean, context))!;
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
    }

    [Test]
    public void CastsBetweenBooleanNumericBinaryDurationAndTemporalTypes()
    {
        XdmTypeConverter converter = new XdmTypeConverter();
        XPathStaticContext context = new XPathStaticContext();

        XdmAtomicValue integer = converter.CastXdmAtomic(Atomic("-2.9", XdmAtomicType.Decimal), XdmAtomicType.Int, context);
        XdmAtomicValue truth = converter.CastXdmAtomic(Atomic("NaN", XdmAtomicType.Double), XdmAtomicType.Boolean, context);
        XdmAtomicValue one = converter.CastXdmAtomic(Atomic("true", XdmAtomicType.Boolean), XdmAtomicType.Decimal, context);
        XdmAtomicValue binary = converter.CastXdmAtomic(Atomic("0AFF", XdmAtomicType.HexBinary), XdmAtomicType.Base64Binary, context);
        XdmAtomicValue months = converter.CastXdmAtomic(Atomic("P1Y2M3DT4H", XdmAtomicType.Duration), XdmAtomicType.YearMonthDuration, context);
        XdmAtomicValue daytime = converter.CastXdmAtomic(Atomic("P1Y2M3DT4H", XdmAtomicType.Duration), XdmAtomicType.DayTimeDuration, context);
        XdmAtomicValue dateTime = Atomic("2024-03-04T05:06:07Z", XdmAtomicType.DateTime);
        XdmAtomicValue date = converter.CastXdmAtomic(dateTime, XdmAtomicType.Date, context);
        XdmAtomicValue time = converter.CastXdmAtomic(dateTime, XdmAtomicType.Time, context);
        XdmAtomicValue yearMonth = converter.CastXdmAtomic(dateTime, XdmAtomicType.GYearMonth, context);
        XdmAtomicValue monthDay = converter.CastXdmAtomic(dateTime, XdmAtomicType.GMonthDay, context);
        XdmAtomicValue month = converter.CastXdmAtomic(Atomic("2024-03-04Z", XdmAtomicType.Date), XdmAtomicType.GMonth, context);

        Assert.Multiple(() =>
        {
            Assert.That(integer.CanonicalValue, Is.EqualTo("-2"));
            Assert.That(truth.BooleanValue, Is.False);
            Assert.That(one.CanonicalValue, Is.EqualTo("1"));
            Assert.That(binary.BinaryValue, Is.EqualTo(new byte[] { 10, 255 }));
            Assert.That(months.CanonicalValue, Is.EqualTo("P1Y2M"));
            Assert.That(daytime.CanonicalValue, Is.EqualTo("P3DT4H"));
            Assert.That(date.CanonicalValue, Is.EqualTo("2024-03-04Z"));
            Assert.That(new XdmDateTimeDatatype().EqualXdmDateTime(date, Atomic("2024-03-04Z", XdmAtomicType.Date), 0), Is.True);
            Assert.That(time.CanonicalValue, Is.EqualTo("05:06:07Z"));
            Assert.That(yearMonth.CanonicalValue, Is.EqualTo("2024-03Z"));
            Assert.That(monthDay.CanonicalValue, Is.EqualTo("--03-04Z"));
            Assert.That(month.CanonicalValue, Is.EqualTo("--03Z"));
        });
    }

    [Test]
    public void PromotionCastAndCastableEnforceAllowedConversionsAndTargets()
    {
        XdmTypeConverter converter = new XdmTypeConverter();
        XPathStaticContext context = new XPathStaticContext();

        Assert.Multiple(() =>
        {
            Assert.That(converter.PromoteXdmAtomic(Atomic("4", XdmAtomicType.Int), XdmAtomicType.Decimal).Type, Is.EqualTo(XdmAtomicType.Int));
            Assert.That(converter.PromoteXdmAtomic(Atomic("4", XdmAtomicType.Int), XdmAtomicType.Float).Type, Is.EqualTo(XdmAtomicType.Float));
            Assert.That(converter.PromoteXdmAtomic(Atomic("4", XdmAtomicType.Float), XdmAtomicType.Double).Type, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(converter.CastableXdmAtomic(Atomic("2024-01-01Z", XdmAtomicType.Date), XdmAtomicType.DateTimeStamp, context), Is.True);
            Assert.That(converter.CastableXdmAtomic(Atomic("2024-01-01", XdmAtomicType.Date), XdmAtomicType.DateTimeStamp, context), Is.False);
            Assert.Throws<XPathTypeException>(() => converter.PromoteXdmAtomic(Atomic("4", XdmAtomicType.Double), XdmAtomicType.Float));
            Assert.Throws<XPathTypeException>(() => converter.CastXdmAtomic(Atomic("2024-01-01", XdmAtomicType.Date), XdmAtomicType.Int, context));
        });

        XPathStaticException abstractTarget = Assert.Throws<XPathStaticException>(() => converter.CastableXdmAtomic(Atomic("x", XdmAtomicType.String), XdmAtomicType.AnyAtomicType, context))!;
        XPathStaticException notationTarget = Assert.Throws<XPathStaticException>(() => converter.CastXdmAtomic(Atomic("x", XdmAtomicType.String), XdmAtomicType.Notation, context))!;
        Assert.Multiple(() =>
        {
            Assert.That(abstractTarget.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0080));
            Assert.That(notationTarget.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0080));
        });
    }

    [Test]
    public void InstanceOfAndTreatUseTheSameSequenceTypeRules()
    {
        XdmTypeConverter converter = new XdmTypeConverter();
        XdmSequence value = Sequence(Atomic("3", XdmAtomicType.Int));
        XdmSequenceType integer = AtomicSequenceType(XdmAtomicType.Integer, XdmOccurrence.ExactlyOne);
        XdmSequenceType stringType = AtomicSequenceType(XdmAtomicType.String, XdmOccurrence.ExactlyOne);

        Assert.Multiple(() =>
        {
            Assert.That(converter.InstanceOfXdmSequence(value, integer), Is.True);
            Assert.That(converter.InstanceOfXdmSequence(value, stringType), Is.False);
            Assert.That(converter.TreatXdmSequence(value, integer), Is.SameAs(value));
        });

        XPathDynamicException failure = Assert.Throws<XPathDynamicException>(() => converter.TreatXdmSequence(value, stringType))!;
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPDY0050));
    }

    private static XdmAtomicValue Atomic(string lexical, XdmAtomicType type)
    {
        XdmTypeConverter converter = new XdmTypeConverter();
        return converter.CastXdmAtomic(new XdmStringDatatype().ParseXdmStringAtomic(lexical, XdmAtomicType.String), type, new XPathStaticContext());
    }

    private static XdmSequence Sequence(params object[] items)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        foreach (object item in items)
        {
            if (item is XmlNode node)
            {
                builder.AddXdmNode(node);
            }
            else if (item is XdmAtomicValue atomic)
            {
                builder.AddXdmAtomicValue(atomic);
            }
            else if (item is XdmFunctionItem function)
            {
                builder.AddXdmFunction(function);
            }
            else if (item is XdmMapValue map)
            {
                builder.AddXdmMap(map);
            }
            else if (item is XdmArrayValue array)
            {
                builder.AddXdmArray(array);
            }
        }

        return builder.BuildImmutableXdmSequence();
    }

    private static XdmSequenceType AtomicSequenceType(XdmAtomicType type, XdmOccurrence occurrence)
    {
        return new XdmSequenceType
        {
            ItemKind = XdmItemKind.Atomic,
            AtomicType = type,
            Occurrence = occurrence
        };
    }

    private static XdmSequenceType NodeSequenceType(XdmNodeKind kind)
    {
        return new XdmSequenceType
        {
            ItemKind = XdmItemKind.Node,
            NodeKind = kind,
            Occurrence = XdmOccurrence.ExactlyOne
        };
    }

    private static XdmSequenceType AnyFunctionSequenceType()
    {
        return new XdmSequenceType
        {
            AnyFunction = true,
            Occurrence = XdmOccurrence.OneOrMore
        };
    }

    private static XdmSequenceType AnyItemSequenceType()
    {
        return new XdmSequenceType
        {
            AnyItem = true,
            Occurrence = XdmOccurrence.OneOrMore
        };
    }

    private static XmlName Name(string namespaceUri, string localName)
    {
        return new XmlName
        {
            Prefix = "t",
            LocalName = localName,
            NamespaceUri = namespaceUri,
            QualifiedName = "t:" + localName
        };
    }
}
