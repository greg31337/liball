using xml;

namespace xml.Tests;

[TestFixture]
public class XdmQNameDatatypeTests
{
    [Test]
    public void ResolvesPrefixedDefaultAndXmlNamesFromStaticContext()
    {
        XdmQNameDatatype datatype = new XdmQNameDatatype();
        XPathStaticContext context = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:products" }],
            DefaultElementNamespace = "urn:default"
        };

        XdmAtomicValue prefixed = datatype.ResolveXdmQNameStatic("  p:item  ", context);
        XdmAtomicValue unprefixed = datatype.ResolveXdmQNameStatic("item", context);
        XdmAtomicValue xml = datatype.ResolveXdmQNameStatic("xml:lang", new XPathStaticContext());

        Assert.Multiple(() =>
        {
            AssertQName(prefixed, XdmAtomicType.QName, "p", "item", "urn:products", "p:item");
            Assert.That(prefixed.LexicalValue, Is.EqualTo("  p:item  "));
            AssertQName(unprefixed, XdmAtomicType.QName, null, "item", "urn:default", "item");
            AssertQName(xml, XdmAtomicType.QName, "xml", "lang", XmlNamespaces.Xml, "xml:lang");
        });
    }

    [Test]
    public void ResolvesDynamicNamesUsingInheritedShadowedAndUndeclaredBindings()
    {
        XdmQNameDatatype datatype = new XdmQNameDatatype();
        XmlNode parent = new XmlNode
        {
            Kind = XmlNodeKind.Element,
            NamespaceBindings =
            [
                new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:outer" },
                new XmlNamespaceBinding { Prefix = "", NamespaceUri = "urn:default" }
            ]
        };
        XmlNode child = new XmlNode
        {
            Kind = XmlNodeKind.Element,
            Parent = parent,
            NamespaceBindings = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:inner" }]
        };
        XmlNode grandchild = new XmlNode
        {
            Kind = XmlNodeKind.Element,
            Parent = child,
            NamespaceBindings = [new XmlNamespaceBinding { Prefix = "", NamespaceUri = "", IsUndeclaration = true }]
        };

        XdmAtomicValue shadowed = datatype.ResolveXdmQNameDynamic("p:item", child);
        XdmAtomicValue inheritedDefault = datatype.ResolveXdmQNameDynamic("item", child);
        XdmAtomicValue undeclaredDefault = datatype.ResolveXdmQNameDynamic("item", grandchild);

        Assert.Multiple(() =>
        {
            Assert.That(shadowed.NameValue!.NamespaceUri, Is.EqualTo("urn:inner"));
            Assert.That(inheritedDefault.NameValue!.NamespaceUri, Is.EqualTo("urn:default"));
            Assert.That(undeclaredDefault.NameValue!.NamespaceUri, Is.Null);
            Assert.Throws<ArgumentException>(() => datatype.ResolveXdmQNameDynamic("item", new XmlNode { Kind = XmlNodeKind.Attribute }));
        });
    }

    [TestCase("")]
    [TestCase("p:")]
    [TestCase(":item")]
    [TestCase("p:item:extra")]
    [TestCase("two names")]
    [TestCase("1item")]
    [TestCase("p:1item")]
    [TestCase("p :item")]
    public void RejectsInvalidQNameDatatypeLexicalFormsWithForg0001(string lexical)
    {
        XdmQNameDatatype datatype = new XdmQNameDatatype();
        XmlNamespaceBinding[] namespaces = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:p" }];
        XdmNumericParseResult result = datatype.TryParseXdmQNameWithNamespaces(lexical, XdmAtomicType.QName, namespaces, null);
        XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => datatype.ParseXdmQNameWithNamespaces(lexical, XdmAtomicType.QName, namespaces, null))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
        });
    }

    [Test]
    public void DynamicQNameResolutionUsesFoca0002ForMalformedLexicalNames()
    {
        XdmQNameDatatype datatype = new XdmQNameDatatype();
        XmlNode element = new XmlNode { Kind = XmlNodeKind.Element };
        XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => datatype.ResolveXdmQNameDynamic("bad:name:again", element))!;

        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOCA0002));
    }

    [Test]
    public void ReportsUnboundPrefixesWithoutTreatingTheXmlPrefixAsUnbound()
    {
        XdmQNameDatatype datatype = new XdmQNameDatatype();
        XPathStaticContext context = new XPathStaticContext();
        XdmNumericParseResult result = datatype.TryParseXdmQNameWithNamespaces("missing:item", XdmAtomicType.QName, [], null);
        XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => datatype.ResolveXdmQNameStatic("missing:item", context))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCode, Is.EqualTo(XPathErrorCodes.FONS0004));
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FONS0004));
            Assert.That(datatype.ResolveXdmQNameStatic("xml:space", context).NameValue!.NamespaceUri, Is.EqualTo(XmlNamespaces.Xml));
        });
    }

    [Test]
    public void CreatesExpandedQNamesDirectlyAndRetainsTheSuppliedPrefix()
    {
        XdmQNameDatatype datatype = new XdmQNameDatatype();
        XdmAtomicValue prefixed = datatype.CreateXdmQNameValue("urn:people", " p:person ");
        XdmAtomicValue unprefixed = datatype.CreateXdmQNameValue("urn:people", "person");
        XdmAtomicValue absent = datatype.CreateXdmQNameValue("", "person");

        Assert.Multiple(() =>
        {
            AssertQName(prefixed, XdmAtomicType.QName, "p", "person", "urn:people", "p:person");
            AssertQName(unprefixed, XdmAtomicType.QName, null, "person", "urn:people", "person");
            Assert.That(absent.NameValue!.NamespaceUri, Is.Null);
            Assert.Throws<XPathDynamicException>(() => datatype.CreateXdmQNameValue(null, "p:person"));
            Assert.Throws<XPathDynamicException>(() => datatype.CreateXdmQNameValue("urn:people", "bad:name:again"));
            Assert.Throws<XPathDynamicException>(() => datatype.CreateXdmQNameValue(" leading", "person"));
        });
    }

    [Test]
    public void ComparesQNamesByExpandedNameAndIgnoresRetainedPrefixes()
    {
        XdmQNameDatatype datatype = new XdmQNameDatatype();
        XdmAtomicValue first = datatype.CreateXdmQNameValue("urn:items", "p:item");
        XdmAtomicValue alias = datatype.CreateXdmQNameValue("urn:items", "q:item");
        XdmAtomicValue otherNamespace = datatype.CreateXdmQNameValue("urn:other", "p:item");
        XdmAtomicValue otherLocal = datatype.CreateXdmQNameValue("urn:items", "p:other");

        Assert.Multiple(() =>
        {
            Assert.That(datatype.EqualXdmQName(first, alias), Is.True);
            Assert.That(datatype.EqualXdmQName(first, otherNamespace), Is.False);
            Assert.That(datatype.EqualXdmQName(first, otherLocal), Is.False);
            Assert.That(first.StringValue, Is.EqualTo("p:item"));
            Assert.That(alias.StringValue, Is.EqualTo("q:item"));
        });
    }

    [Test]
    public void CreatesOnlyExplicitlyPermittedNotationValuesAndComparesExpandedNames()
    {
        XdmQNameDatatype datatype = new XdmQNameDatatype();
        XmlNamespaceBinding[] namespaces =
        [
            new XmlNamespaceBinding { Prefix = "n", NamespaceUri = "urn:notations" },
            new XmlNamespaceBinding { Prefix = "alias", NamespaceUri = "urn:notations" }
        ];
        XmlName[] permitted = [new XmlName { NamespaceUri = "urn:notations", LocalName = "jpeg" }];
        XdmAtomicValue first = datatype.ResolveXdmDeclaredNotation("n:jpeg", namespaces, null, permitted);
        XdmAtomicValue alias = datatype.ResolveXdmDeclaredNotation("alias:jpeg", namespaces, null, permitted);
        XdmAtomicValue direct = datatype.CreateXdmNotationValue("urn:notations", "img:jpeg", permitted);
        XPathDynamicException undeclared = Assert.Throws<XPathDynamicException>(() => datatype.ResolveXdmDeclaredNotation("n:png", namespaces, null, permitted))!;

        Assert.Multiple(() =>
        {
            AssertQName(first, XdmAtomicType.Notation, "n", "jpeg", "urn:notations", "n:jpeg");
            Assert.That(datatype.EqualXdmNotation(first, alias), Is.True);
            Assert.That(datatype.EqualXdmNotation(first, direct), Is.True);
            Assert.That(undeclared.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.Throws<XPathDynamicException>(() => datatype.CreateXdmNotationValue("urn:notations", "img:png", permitted));
        });
    }

    [Test]
    public void EnforcesNamespaceSensitiveCastRules()
    {
        XdmQNameDatatype datatype = new XdmQNameDatatype();
        XPathStaticContext context = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:p" }]
        };
        XdmAtomicValue text = new XdmAtomicValue { Type = XdmAtomicType.String, StringValue = "p:item" };
        XdmAtomicValue token = new XdmAtomicValue { Type = XdmAtomicType.Token, StringValue = "p:item" };
        XdmAtomicValue untyped = new XdmAtomicValue { Type = XdmAtomicType.UntypedAtomic, StringValue = "p:item" };
        XdmAtomicValue fromText = datatype.CastXdmAtomicToQName(text, context);
        XdmAtomicValue fromToken = datatype.CastXdmAtomicToQName(token, context);
        XdmAtomicValue fromUntyped = datatype.CastXdmAtomicToQName(untyped, context);
        XdmAtomicValue copied = datatype.CastXdmAtomicToQName(fromText, new XPathStaticContext());
        XmlName[] permitted = [new XmlName { NamespaceUri = "urn:p", LocalName = "item" }];
        XdmAtomicValue notationValue = datatype.CreateXdmNotationValue("urn:p", "p:item", permitted);
        XdmAtomicValue fromNotation = datatype.CastXdmAtomicToQName(notationValue, new XPathStaticContext());
        XPathTypeException unsupported = Assert.Throws<XPathTypeException>(() => datatype.CastXdmAtomicToQName(new XdmAtomicValue { Type = XdmAtomicType.Int, IntegerValue = 1 }, context))!;
        XPathStaticException notation = Assert.Throws<XPathStaticException>(() => datatype.EnsureXdmNamespaceSensitiveCastTarget(XdmAtomicType.Notation))!;

        Assert.Multiple(() =>
        {
            Assert.That(datatype.EqualXdmQName(fromText, fromUntyped), Is.True);
            Assert.That(datatype.EqualXdmQName(fromText, fromToken), Is.True);
            Assert.That(datatype.EqualXdmQName(fromText, fromNotation), Is.True);
            Assert.That(datatype.EqualXdmQName(fromText, copied), Is.True);
            Assert.That(copied.NameValue, Is.Not.SameAs(fromText.NameValue));
            Assert.That(unsupported.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(notation.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPST0080));
            Assert.DoesNotThrow(() => datatype.EnsureXdmNamespaceSensitiveCastTarget(XdmAtomicType.QName));
            Assert.Throws<ArgumentException>(() => datatype.EnsureXdmNamespaceSensitiveCastTarget(XdmAtomicType.String));
        });
    }

    [Test]
    public void FactoryCreatesSerializesAndValidatesQNameStorage()
    {
        XdmFactory factory = new XdmFactory();
        XPathStaticContext context = new XPathStaticContext
        {
            Namespaces = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:p" }]
        };
        XdmAtomicValue value = factory.CreateXdmQNameAtomicValue("p:item", context);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => factory.ValidateXdmAtomicValue(value));
            Assert.That(new SequenceNormalizer().CastXdmAtomicToSerializationString(value), Is.EqualTo("p:item"));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.QName, NameValue = new XmlName { Prefix = "p", LocalName = "item", QualifiedName = "p:item" } }));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.QName, NameValue = new XmlName { LocalName = "1item", QualifiedName = "1item" } }));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.QName, NameValue = new XmlName { Prefix = "p", LocalName = "item", NamespaceUri = "urn:p", QualifiedName = "q:item" } }));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.QName, NameValue = new XmlName { Prefix = "p", LocalName = "item", NamespaceUri = "urn:p", QualifiedName = "p:item" }, LexicalValue = "q:item" }));
            Assert.Throws<ArgumentException>(() => factory.ValidateXdmAtomicValue(new XdmAtomicValue { Type = XdmAtomicType.QName, NameValue = new XmlName { Prefix = "p", LocalName = "item", NamespaceUri = "urn:p", QualifiedName = "p:item" }, CanonicalValue = "q:item" }));
        });
    }

    private static void AssertQName(XdmAtomicValue value, XdmAtomicType type, string? prefix, string localName, string? namespaceUri, string lexical)
    {
        Assert.Multiple(() =>
        {
            Assert.That(value.Type, Is.EqualTo(type));
            Assert.That(value.NameValue, Is.Not.Null);
            Assert.That(value.NameValue!.Prefix, Is.EqualTo(prefix));
            Assert.That(value.NameValue.LocalName, Is.EqualTo(localName));
            Assert.That(value.NameValue.NamespaceUri, Is.EqualTo(namespaceUri));
            Assert.That(value.NameValue.QualifiedName, Is.EqualTo(lexical));
            Assert.That(value.StringValue, Is.EqualTo(lexical));
            Assert.That(value.CanonicalValue, Is.EqualTo(lexical));
            Assert.That(value.IsImmutable, Is.True);
        });
    }
}
