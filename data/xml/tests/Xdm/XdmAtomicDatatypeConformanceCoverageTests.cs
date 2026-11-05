using xml;

namespace xml.Tests;

[TestFixture]
public class XdmAtomicDatatypeConformanceCoverageTests
{
    private const string InvalidUtf16Marker = "<invalid UTF-16>";

    private static readonly object[] ValidLexicalCases =
    [
        new object[] { XdmAtomicType.UntypedAtomic, "raw value", "raw value" },
        new object[] { XdmAtomicType.String, " A\tB ", " A\tB " },
        new object[] { XdmAtomicType.NormalizedString, " A\tB ", " A B " },
        new object[] { XdmAtomicType.Token, " A  B ", "A B" },
        new object[] { XdmAtomicType.Language, " en-US ", "en-US" },
        new object[] { XdmAtomicType.Name, " p:item ", "p:item" },
        new object[] { XdmAtomicType.NcName, " item ", "item" },
        new object[] { XdmAtomicType.Nmtoken, " 1:item ", "1:item" },
        new object[] { XdmAtomicType.Id, " item ", "item" },
        new object[] { XdmAtomicType.Idref, " item ", "item" },
        new object[] { XdmAtomicType.Entity, " item ", "item" },
        new object[] { XdmAtomicType.Boolean, " 1 ", "true" },
        new object[] { XdmAtomicType.Decimal, "+001.2300", "1.23" },
        new object[] { XdmAtomicType.Integer, "+001", "1" },
        new object[] { XdmAtomicType.NonPositiveInteger, "-1", "-1" },
        new object[] { XdmAtomicType.NegativeInteger, "-1", "-1" },
        new object[] { XdmAtomicType.Long, "-9223372036854775808", "-9223372036854775808" },
        new object[] { XdmAtomicType.Int, "2147483647", "2147483647" },
        new object[] { XdmAtomicType.Short, "-32768", "-32768" },
        new object[] { XdmAtomicType.Byte, "127", "127" },
        new object[] { XdmAtomicType.NonNegativeInteger, "0", "0" },
        new object[] { XdmAtomicType.UnsignedLong, "18446744073709551615", "18446744073709551615" },
        new object[] { XdmAtomicType.UnsignedInt, "4294967295", "4294967295" },
        new object[] { XdmAtomicType.UnsignedShort, "65535", "65535" },
        new object[] { XdmAtomicType.UnsignedByte, "255", "255" },
        new object[] { XdmAtomicType.PositiveInteger, "1", "1" },
        new object[] { XdmAtomicType.Float, "1.5", "1.5E0" },
        new object[] { XdmAtomicType.Double, "1.5", "1.5E0" },
        new object[] { XdmAtomicType.Duration, "P1Y2M3DT4H5M6.0S", "P1Y2M3DT4H5M6S" },
        new object[] { XdmAtomicType.YearMonthDuration, "P13M", "P1Y1M" },
        new object[] { XdmAtomicType.DayTimeDuration, "PT60S", "PT1M" },
        new object[] { XdmAtomicType.DateTime, "2000-01-01T00:00:00+00:00", "2000-01-01T00:00:00Z" },
        new object[] { XdmAtomicType.DateTimeStamp, "2000-01-01T00:00:00+00:00", "2000-01-01T00:00:00Z" },
        new object[] { XdmAtomicType.Date, "2000-01-01+00:00", "2000-01-01Z" },
        new object[] { XdmAtomicType.Time, "24:00:00", "00:00:00" },
        new object[] { XdmAtomicType.GYearMonth, "2000-01Z", "2000-01Z" },
        new object[] { XdmAtomicType.GYear, "2000Z", "2000Z" },
        new object[] { XdmAtomicType.GMonthDay, "--02-29", "--02-29" },
        new object[] { XdmAtomicType.GMonth, "--05", "--05" },
        new object[] { XdmAtomicType.GDay, "---31", "---31" },
        new object[] { XdmAtomicType.HexBinary, "0a", "0A" },
        new object[] { XdmAtomicType.Base64Binary, "YQ==", "YQ==" },
        new object[] { XdmAtomicType.AnyUri, " relative  path ", "relative path" },
        new object[] { XdmAtomicType.QName, " p:item ", "p:item" },
        new object[] { XdmAtomicType.Notation, " p:item ", "p:item" }
    ];

    private static readonly object[] InvalidLexicalCases =
    [
        new object[] { XdmAtomicType.UntypedAtomic, InvalidUtf16Marker, XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.String, InvalidUtf16Marker, XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.NormalizedString, InvalidUtf16Marker, XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Token, InvalidUtf16Marker, XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Language, "bad_tag", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Name, "1name", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.NcName, "p:name", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Nmtoken, "", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Id, "p:name", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Idref, "p:name", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Entity, "p:name", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Boolean, "yes", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Decimal, "1e2", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Integer, "1.0", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.NonPositiveInteger, "1", XPathErrorCodes.FOCA0002 },
        new object[] { XdmAtomicType.NegativeInteger, "0", XPathErrorCodes.FOCA0002 },
        new object[] { XdmAtomicType.Long, "9223372036854775808", XPathErrorCodes.FOCA0002 },
        new object[] { XdmAtomicType.Int, "2147483648", XPathErrorCodes.FOCA0002 },
        new object[] { XdmAtomicType.Short, "32768", XPathErrorCodes.FOCA0002 },
        new object[] { XdmAtomicType.Byte, "128", XPathErrorCodes.FOCA0002 },
        new object[] { XdmAtomicType.NonNegativeInteger, "-1", XPathErrorCodes.FOCA0002 },
        new object[] { XdmAtomicType.UnsignedLong, "-1", XPathErrorCodes.FOCA0002 },
        new object[] { XdmAtomicType.UnsignedInt, "-1", XPathErrorCodes.FOCA0002 },
        new object[] { XdmAtomicType.UnsignedShort, "-1", XPathErrorCodes.FOCA0002 },
        new object[] { XdmAtomicType.UnsignedByte, "-1", XPathErrorCodes.FOCA0002 },
        new object[] { XdmAtomicType.PositiveInteger, "0", XPathErrorCodes.FOCA0002 },
        new object[] { XdmAtomicType.Float, "nan", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Double, "Infinity", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Duration, "P", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.YearMonthDuration, "P1D", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.DayTimeDuration, "P1M", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.DateTime, "2000-01-01 00:00:00", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.DateTimeStamp, "2000-01-01T00:00:00", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Date, "2000-02-30", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Time, "24:00:01", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.GYearMonth, "2000-13", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.GYear, "20", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.GMonthDay, "--02-30", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.GMonth, "--00", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.GDay, "---32", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.HexBinary, "0", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Base64Binary, "A", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.AnyUri, InvalidUtf16Marker, XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.QName, "bad:name:again", XPathErrorCodes.FORG0001 },
        new object[] { XdmAtomicType.Notation, "p:other", XPathErrorCodes.FORG0001 }
    ];

    [TestCaseSource(nameof(ValidLexicalCases))]
    public void EveryConcreteBuiltInTypeAcceptsAValidLexicalAndProducesItsCanonicalForm(XdmAtomicType type, string lexical, string canonical)
    {
        XdmAtomicValue value = ParseAtomic(type, lexical);
        string stringValue = type == XdmAtomicType.Float || type == XdmAtomicType.Double ? new XdmFloatingDatatype().FormatXdmFloatingStringValue(value.FloatingValue, type) : canonical;

        Assert.Multiple(() =>
        {
            Assert.That(value.Type, Is.EqualTo(type));
            Assert.That(value.LexicalValue, Is.EqualTo(lexical));
            Assert.That(value.StringValue, Is.EqualTo(stringValue));
            Assert.That(value.CanonicalValue, Is.EqualTo(canonical));
            Assert.That(value.IsImmutable, Is.True);
            Assert.DoesNotThrow(() => new XdmFactory().ValidateXdmAtomicValue(value));
        });
    }

    [TestCaseSource(nameof(InvalidLexicalCases))]
    public void EveryConcreteBuiltInTypeRejectsAnInvalidLexicalWithTheRequiredError(XdmAtomicType type, string lexical, string errorCode)
    {
        string invalidLexical = lexical == InvalidUtf16Marker ? new string('\uD800', 1) : lexical;
        XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => ParseAtomic(type, invalidLexical))!;

        Assert.That(exception.Diagnostic, Is.Not.Null);
        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(errorCode));
    }

    [TestCaseSource(nameof(ValidLexicalCases))]
    public void EveryLegalBuiltInCastTargetAcceptsItsValidStringLexical(XdmAtomicType type, string lexical, string canonical)
    {
        if (type == XdmAtomicType.Notation)
        {
            XPathStaticException exception = Assert.Throws<XPathStaticException>(() => CastFromString(lexical, type))!;
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0080));
            return;
        }

        XdmAtomicValue value = CastFromString(lexical, type);
        Assert.Multiple(() =>
        {
            Assert.That(value.Type, Is.EqualTo(type));
            Assert.That(value.CanonicalValue, Is.EqualTo(canonical));
        });
    }

    [Test]
    public void AbstractAnyAtomicTypeIsNeitherConstructibleNorACastTarget()
    {
        XdmFactory factory = new XdmFactory();
        XdmAtomicValue text = new XdmStringDatatype().ParseXdmStringAtomic("value", XdmAtomicType.String);
        XdmTypeConverter converter = new XdmTypeConverter();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => factory.CreateXdmAtomicValue(XdmAtomicType.AnyAtomicType));
            Assert.That(Assert.Throws<XPathStaticException>(() => converter.CastXdmAtomic(text, XdmAtomicType.AnyAtomicType, new XPathStaticContext()))!.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0080));
        });
    }

    private static XdmAtomicValue CastFromString(string lexical, XdmAtomicType targetType)
    {
        XPathStaticContext context = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:test" }]
        };
        XdmAtomicValue source = new XdmStringDatatype().ParseXdmStringAtomic(lexical, XdmAtomicType.String);
        return new XdmTypeConverter().CastXdmAtomic(source, targetType, context);
    }

    private static XdmAtomicValue ParseAtomic(XdmAtomicType type, string lexical)
    {
        if (type == XdmAtomicType.UntypedAtomic)
        {
            return new XdmTypeConverter().CreateXdmUntypedAtomicValue(lexical);
        }

        XdmTypeSystem types = new XdmTypeSystem();
        if (types.IsXdmStringDerivedAtomicType(type))
        {
            return new XdmStringDatatype().ParseXdmStringAtomic(lexical, type);
        }

        if (type == XdmAtomicType.Boolean)
        {
            return new XdmNumericDatatype().ParseXdmBoolean(lexical);
        }

        if (type == XdmAtomicType.Decimal)
        {
            return new XdmNumericDatatype().ParseXdmDecimal(lexical);
        }

        if (types.IsXdmAtomicSubtypeOf(type, XdmAtomicType.Integer))
        {
            return new XdmNumericDatatype().ParseXdmInteger(lexical, type);
        }

        if (type == XdmAtomicType.Float || type == XdmAtomicType.Double)
        {
            return new XdmFloatingDatatype().ParseXdmFloating(lexical, type);
        }

        if (types.IsXdmDurationDerivedAtomicType(type))
        {
            return new XdmDurationDatatype().ParseXdmDuration(lexical, type);
        }

        if (type >= XdmAtomicType.DateTime && type <= XdmAtomicType.GDay)
        {
            return new XdmDateTimeDatatype().ParseXdmDateTime(lexical, type);
        }

        if (type == XdmAtomicType.HexBinary || type == XdmAtomicType.Base64Binary)
        {
            return new XdmBinaryDatatype().ParseXdmBinary(lexical, type);
        }

        if (type == XdmAtomicType.AnyUri)
        {
            return new XdmAnyUriDatatype().ParseXdmAnyUri(lexical);
        }

        XmlNamespaceBinding[] namespaces = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:test" }];
        if (type == XdmAtomicType.QName)
        {
            return new XdmQNameDatatype().ParseXdmQNameWithNamespaces(lexical, type, namespaces, null);
        }

        if (type == XdmAtomicType.Notation)
        {
            XmlName[] permitted = [new XmlName { Prefix = "p", LocalName = "item", NamespaceUri = "urn:test", QualifiedName = "p:item" }];
            return new XdmQNameDatatype().ResolveXdmDeclaredNotation(lexical, namespaces, null, permitted);
        }

        throw new ArgumentOutOfRangeException(nameof(type));
    }
}
