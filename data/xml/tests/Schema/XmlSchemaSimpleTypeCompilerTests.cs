using xml;

namespace xml.Tests;

[TestFixture]
public class XmlSchemaSimpleTypeCompilerTests
{
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void RegistersBuiltInAtomicAndListTypesWithFacetsAndFundamentalProperties()
    {
        CompiledXmlSchemaSet compiled = Compile(Schema("", null));
        CompiledXmlSchemaSimpleType anySimple = Find(compiled, Xsd, "anySimpleType");
        CompiledXmlSchemaSimpleType anyAtomic = Find(compiled, Xsd, "anyAtomicType");
        CompiledXmlSchemaSimpleType integer = Find(compiled, Xsd, "integer");
        CompiledXmlSchemaSimpleType intType = Find(compiled, Xsd, "int");
        CompiledXmlSchemaSimpleType floatType = Find(compiled, Xsd, "float");
        CompiledXmlSchemaSimpleType token = Find(compiled, Xsd, "token");
        CompiledXmlSchemaSimpleType stamp = Find(compiled, Xsd, "dateTimeStamp");
        CompiledXmlSchemaSimpleType idrefs = Find(compiled, Xsd, "IDREFS");

        Assert.Multiple(() =>
        {
            Assert.That(compiled.SimpleTypes, Has.Length.EqualTo(53));
            Assert.That(anySimple.Variety, Is.EqualTo(XmlSchemaSimpleVariety.Universal));
            Assert.That(anyAtomic.BaseType, Is.SameAs(anySimple));
            Assert.That(integer.BaseType!.AtomicType, Is.EqualTo(XdmAtomicType.Decimal));
            Assert.That(Facet(integer, XmlSchemaFacetKind.FractionDigits)!.Value, Is.EqualTo("0"));
            Assert.That(Facet(integer, XmlSchemaFacetKind.FractionDigits)!.Fixed, Is.True);
            Assert.That(Facet(integer, XmlSchemaFacetKind.Pattern)!.Value, Is.EqualTo("[-+]?[0-9]+"));
            Assert.That(intType.FundamentalFacets.Ordered, Is.EqualTo(XmlSchemaOrderKind.Total));
            Assert.That(intType.FundamentalFacets.Bounded, Is.True);
            Assert.That(intType.FundamentalFacets.CardinalityFinite, Is.True);
            Assert.That(intType.FundamentalFacets.Numeric, Is.True);
            Assert.That(floatType.FundamentalFacets.Ordered, Is.EqualTo(XmlSchemaOrderKind.Partial));
            Assert.That(floatType.FundamentalFacets.CardinalityFinite, Is.True);
            Assert.That(Facet(token, XmlSchemaFacetKind.WhiteSpace)!.Value, Is.EqualTo("collapse"));
            Assert.That(Facet(token, XmlSchemaFacetKind.WhiteSpace)!.Fixed, Is.True);
            Assert.That(Facet(stamp, XmlSchemaFacetKind.ExplicitTimezone)!.Value, Is.EqualTo("required"));
            Assert.That(idrefs.Variety, Is.EqualTo(XmlSchemaSimpleVariety.List));
            Assert.That(idrefs.BaseType!.IsAnonymous, Is.True);
            Assert.That(idrefs.DerivationMethod, Is.EqualTo(XmlSchemaDerivationMethod.Restriction));
            Assert.That(idrefs.ItemType!.AtomicType, Is.EqualTo(XdmAtomicType.Idref));
            Assert.That(Facet(idrefs, XmlSchemaFacetKind.WhiteSpace)!.Value, Is.EqualTo("collapse"));
            Assert.That(Facet(idrefs, XmlSchemaFacetKind.MinLength)!.Value, Is.EqualTo("1"));
        });
    }

    [Test]
    public void CompilesForwardRestrictionsAndInheritedFacets()
    {
        string source = Schema(
            "<xs:simpleType name='Small'><xs:restriction base='t:Range'>" +
            "<xs:maxInclusive value='99'/><xs:totalDigits value='2'/><xs:assertion test='$value ge 0'/>" +
            "</xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='Range'><xs:restriction base='xs:int'>" +
            "<xs:minInclusive value='0'/><xs:maxInclusive value='100'/>" +
            "</xs:restriction></xs:simpleType>",
            "urn:test",
            " xmlns:t='urn:test'");

        CompiledXmlSchemaSet compiled = Compile(source);
        CompiledXmlSchemaSimpleType range = Find(compiled, "urn:test", "Range");
        CompiledXmlSchemaSimpleType small = Find(compiled, "urn:test", "Small");

        Assert.Multiple(() =>
        {
            Assert.That(small.BaseType, Is.SameAs(range));
            Assert.That(small.Variety, Is.EqualTo(XmlSchemaSimpleVariety.Atomic));
            Assert.That(small.AtomicType, Is.EqualTo(XdmAtomicType.Int));
            Assert.That(Facet(small, XmlSchemaFacetKind.MinInclusive)!.Value, Is.EqualTo("0"));
            Assert.That(Facet(small, XmlSchemaFacetKind.MaxInclusive)!.Value, Is.EqualTo("99"));
            Assert.That(Facet(small, XmlSchemaFacetKind.TotalDigits)!.Value, Is.EqualTo("2"));
            Assert.That(Facet(small, XmlSchemaFacetKind.FractionDigits)!.Value, Is.EqualTo("0"));
            Assert.That(Facets(small, XmlSchemaFacetKind.Assertion), Has.Length.EqualTo(1));
            Assert.That(Facet(small, XmlSchemaFacetKind.Assertion)!.Value, Is.EqualTo("$value ge 0"));
            Assert.That(small.FundamentalFacets.Ordered, Is.EqualTo(XmlSchemaOrderKind.Total));
            Assert.That(small.FundamentalFacets.Bounded, Is.True);
            Assert.That(small.FundamentalFacets.CardinalityFinite, Is.True);
            Assert.That(small.FundamentalFacets.Numeric, Is.True);
        });
    }

    [Test]
    public void CompilesListUnionAndAnonymousMemberTypes()
    {
        string source = Schema(
            "<xs:simpleType name='Codes'><xs:list itemType='xs:NCName'/></xs:simpleType>" +
            "<xs:simpleType name='Choice'><xs:union memberTypes='xs:int t:Codes'>" +
            "<xs:simpleType><xs:restriction base='xs:boolean'/></xs:simpleType>" +
            "</xs:union></xs:simpleType>" +
            "<xs:simpleType name='ShortCodes'><xs:restriction base='t:Codes'><xs:maxLength value='3'/></xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='Flags'><xs:list itemType='xs:boolean'/></xs:simpleType>" +
            "<xs:simpleType name='TwoFlags'><xs:restriction base='t:Flags'><xs:length value='2'/></xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='Numbers'><xs:union memberTypes='xs:int xs:decimal'/></xs:simpleType>",
            "urn:test",
            " xmlns:t='urn:test'");

        CompiledXmlSchemaSet compiled = Compile(source);
        CompiledXmlSchemaSimpleType codes = Find(compiled, "urn:test", "Codes");
        CompiledXmlSchemaSimpleType choice = Find(compiled, "urn:test", "Choice");
        CompiledXmlSchemaSimpleType shortCodes = Find(compiled, "urn:test", "ShortCodes");
        CompiledXmlSchemaSimpleType twoFlags = Find(compiled, "urn:test", "TwoFlags");
        CompiledXmlSchemaSimpleType numbers = Find(compiled, "urn:test", "Numbers");

        Assert.Multiple(() =>
        {
            Assert.That(codes.Variety, Is.EqualTo(XmlSchemaSimpleVariety.List));
            Assert.That(codes.ItemType!.AtomicType, Is.EqualTo(XdmAtomicType.NcName));
            Assert.That(codes.DerivationMethod, Is.EqualTo(XmlSchemaDerivationMethod.List));
            Assert.That(choice.Variety, Is.EqualTo(XmlSchemaSimpleVariety.Union));
            Assert.That(choice.MemberTypes, Has.Length.EqualTo(3));
            Assert.That(choice.MemberTypes[0].AtomicType, Is.EqualTo(XdmAtomicType.Int));
            Assert.That(choice.MemberTypes[1], Is.SameAs(codes));
            Assert.That(choice.MemberTypes[2].IsAnonymous, Is.True);
            Assert.That(choice.MemberTypes[2].AtomicType, Is.EqualTo(XdmAtomicType.Boolean));
            Assert.That(shortCodes.Variety, Is.EqualTo(XmlSchemaSimpleVariety.List));
            Assert.That(shortCodes.ItemType, Is.SameAs(codes.ItemType));
            Assert.That(Facet(shortCodes, XmlSchemaFacetKind.MaxLength)!.Value, Is.EqualTo("3"));
            Assert.That(shortCodes.FundamentalFacets.Bounded, Is.False);
            Assert.That(shortCodes.FundamentalFacets.CardinalityFinite, Is.False);
            Assert.That(twoFlags.FundamentalFacets.Bounded, Is.False);
            Assert.That(twoFlags.FundamentalFacets.CardinalityFinite, Is.True);
            Assert.That(numbers.FundamentalFacets.Ordered, Is.EqualTo(XmlSchemaOrderKind.Total));
            Assert.That(numbers.FundamentalFacets.Bounded, Is.False);
            Assert.That(numbers.FundamentalFacets.Numeric, Is.True);
        });
    }

    [Test]
    public void CompilesAllFacetFamiliesAndCanonicalizesTypedFacetValues()
    {
        string source = Schema(
            "<xs:simpleType name='Text'><xs:restriction base='xs:string'>" +
            "<xs:length value=' 4 '/><xs:pattern value='[A-Z]+'/><xs:enumeration value='TEST'/>" +
            "<xs:whiteSpace value='collapse'/><xs:assertion test='string-length($value) = 4'/>" +
            "</xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='Amount'><xs:restriction base='xs:decimal'>" +
            "<xs:minInclusive value='+000.10'/><xs:maxExclusive value='100.00'/>" +
            "<xs:totalDigits value='5'/><xs:fractionDigits value='2'/><xs:minScale value='0'/><xs:maxScale value='2'/>" +
            "</xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='Zoned'><xs:restriction base='xs:dateTime'><xs:explicitTimezone value='required'/></xs:restriction></xs:simpleType>",
            "urn:test",
            "");

        CompiledXmlSchemaSet compiled = Compile(source);
        CompiledXmlSchemaSimpleType text = Find(compiled, "urn:test", "Text");
        CompiledXmlSchemaSimpleType amount = Find(compiled, "urn:test", "Amount");
        CompiledXmlSchemaSimpleType zoned = Find(compiled, "urn:test", "Zoned");

        Assert.Multiple(() =>
        {
            Assert.That(Facet(text, XmlSchemaFacetKind.Length)!.Value, Is.EqualTo("4"));
            Assert.That(Facet(text, XmlSchemaFacetKind.Pattern)!.Value, Is.EqualTo("[A-Z]+"));
            Assert.That(Facet(text, XmlSchemaFacetKind.Enumeration)!.Value, Is.EqualTo("TEST"));
            Assert.That(Facet(text, XmlSchemaFacetKind.WhiteSpace)!.Value, Is.EqualTo("collapse"));
            Assert.That(text.FundamentalFacets.Bounded, Is.False);
            Assert.That(text.FundamentalFacets.CardinalityFinite, Is.True);
            Assert.That(Facet(amount, XmlSchemaFacetKind.MinInclusive)!.Value, Is.EqualTo("0.1"));
            Assert.That(Facet(amount, XmlSchemaFacetKind.MaxExclusive)!.Value, Is.EqualTo("100"));
            Assert.That(Facet(amount, XmlSchemaFacetKind.TotalDigits)!.Value, Is.EqualTo("5"));
            Assert.That(Facet(amount, XmlSchemaFacetKind.FractionDigits)!.Value, Is.EqualTo("2"));
            Assert.That(Facet(amount, XmlSchemaFacetKind.MinScale)!.Value, Is.EqualTo("0"));
            Assert.That(Facet(amount, XmlSchemaFacetKind.MaxScale)!.Value, Is.EqualTo("2"));
            Assert.That(amount.FundamentalFacets.Bounded, Is.True);
            Assert.That(amount.FundamentalFacets.CardinalityFinite, Is.True);
            Assert.That(Facet(zoned, XmlSchemaFacetKind.ExplicitTimezone)!.Value, Is.EqualTo("required"));
        });
    }

    [Test]
    public void ResolvesImportedAndChameleonTypeQNamesAcrossModules()
    {
        TypeResolver resolver = new TypeResolver();
        resolver.Add(
            "https://example.test/remote.xsd",
            Schema("<xs:simpleType name='Remote'><xs:restriction base='xs:token'/></xs:simpleType>", "urn:remote", ""));
        resolver.Add(
            "https://example.test/common.xsd",
            Schema(
                "<xs:simpleType name='Base'><xs:restriction base='xs:string'/></xs:simpleType>" +
                "<xs:simpleType name='Derived'><xs:restriction base='Base'/></xs:simpleType>",
                null,
                ""));
        string root = Schema(
            "<xs:import namespace='urn:remote' schemaLocation='remote.xsd'/>" +
            "<xs:include schemaLocation='common.xsd'/>" +
            "<xs:simpleType name='Local'><xs:restriction base='r:Remote'/></xs:simpleType>",
            "urn:root",
            " xmlns:r='urn:remote'");

        CompiledXmlSchemaSet compiled = Compile(root, resolver);
        CompiledXmlSchemaSimpleType remote = Find(compiled, "urn:remote", "Remote");
        CompiledXmlSchemaSimpleType local = Find(compiled, "urn:root", "Local");
        CompiledXmlSchemaSimpleType chameleonBase = Find(compiled, "urn:root", "Base");
        CompiledXmlSchemaSimpleType chameleonDerived = Find(compiled, "urn:root", "Derived");

        Assert.Multiple(() =>
        {
            Assert.That(local.BaseType, Is.SameAs(remote));
            Assert.That(chameleonDerived.BaseType, Is.SameAs(chameleonBase));
            Assert.That(chameleonDerived.Module!.IsChameleon, Is.True);
            Assert.That(resolver.Requests, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void AppliesRedefinitionsAgainstThePreviousTypeDefinition()
    {
        TypeResolver resolver = new TypeResolver();
        resolver.Add(
            "https://example.test/base.xsd",
            Schema("<xs:simpleType name='Code'><xs:restriction base='xs:string'><xs:maxLength value='10'/></xs:restriction></xs:simpleType>", "urn:test", ""));
        string root = Schema(
            "<xs:redefine schemaLocation='base.xsd'>" +
            "<xs:simpleType name='Code'><xs:restriction base='t:Code'><xs:maxLength value='5'/></xs:restriction></xs:simpleType>" +
            "</xs:redefine>",
            "urn:test",
            " xmlns:t='urn:test'");

        CompiledXmlSchemaSet compiled = Compile(root, resolver);
        CompiledXmlSchemaSimpleType code = Find(compiled, "urn:test", "Code");

        Assert.Multiple(() =>
        {
            Assert.That(code.PreviousDefinition, Is.Not.Null);
            Assert.That(code.BaseType, Is.SameAs(code.PreviousDefinition));
            Assert.That(Facet(code.PreviousDefinition!, XmlSchemaFacetKind.MaxLength)!.Value, Is.EqualTo("10"));
            Assert.That(Facet(code, XmlSchemaFacetKind.MaxLength)!.Value, Is.EqualTo("5"));
        });
    }

    [Test]
    public void AppliesOverrideDefinitionsWithoutRequiringRestrictionOfThePreviousType()
    {
        TypeResolver resolver = new TypeResolver();
        resolver.Add(
            "https://example.test/base.xsd",
            Schema("<xs:simpleType name='Code'><xs:restriction base='xs:string'><xs:maxLength value='10'/></xs:restriction></xs:simpleType>", "urn:test", ""));
        string root = Schema(
            "<xs:override schemaLocation='base.xsd'>" +
            "<xs:simpleType name='Code'><xs:restriction base='xs:token'><xs:length value='3'/></xs:restriction></xs:simpleType>" +
            "</xs:override>",
            "urn:test",
            "");

        CompiledXmlSchemaSimpleType code = Find(Compile(root, resolver), "urn:test", "Code");

        Assert.Multiple(() =>
        {
            Assert.That(code.PreviousDefinition, Is.Not.Null);
            Assert.That(code.BaseType!.AtomicType, Is.EqualTo(XdmAtomicType.Token));
            Assert.That(code.BaseType, Is.Not.SameAs(code.PreviousDefinition));
            Assert.That(Facet(code, XmlSchemaFacetKind.Length)!.Value, Is.EqualTo("3"));
        });
    }

    [Test]
    public void HonorsSchemaFinalDefaultAndAcceptsTheExtensionFinalToken()
    {
        string blocked = Schema(
            "<xs:simpleType name='Base'><xs:restriction base='xs:string'/></xs:simpleType>" +
            "<xs:simpleType name='Derived'><xs:restriction base='t:Base'/></xs:simpleType>",
            "urn:test",
            " xmlns:t='urn:test' finalDefault='restriction'");
        string allowed = Schema(
            "<xs:simpleType name='Base' final='extension'><xs:restriction base='xs:string'/></xs:simpleType>",
            "urn:test",
            "");

        XmlSchemaException exception = Assert.Throws<XmlSchemaException>(() => Compile(blocked))!;
        CompiledXmlSchemaSimpleType baseType = Find(Compile(allowed), "urn:test", "Base");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SchemaFailure));
            Assert.That(baseType.Final, Is.EqualTo(XmlSchemaDerivationMethod.Extension));
        });
    }

    [TestCase("<xs:simpleType name='Bad'/>")]
    [TestCase("<xs:simpleType name='1Bad'><xs:restriction base='xs:string'/></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Bad'><xs:restriction base='xs:string'><xs:simpleType><xs:restriction base='xs:string'/></xs:simpleType></xs:restriction></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Bad'><xs:restriction base='t:Missing'/></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Bad'><xs:list/></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Bad'><xs:union/></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Bad'><xs:restriction base='xs:int'><xs:length value='1'/></xs:restriction></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Bad'><xs:restriction base='xs:anySimpleType'/></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Bad'><xs:restriction base='xs:anyAtomicType'/></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Choice'><xs:union memberTypes='xs:string xs:int'/></xs:simpleType><xs:simpleType name='Bad'><xs:restriction base='t:Choice'><xs:whiteSpace value='collapse'/></xs:restriction></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Bad'><xs:restriction base='xs:string'><xs:length value='-1'/></xs:restriction></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Bad'><xs:restriction base='xs:string'><xs:length value='1'/><xs:length value='2'/></xs:restriction></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Bad'><xs:restriction base='xs:string'><xs:minLength value='3'/><xs:maxLength value='2'/></xs:restriction></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Bad'><xs:restriction base='xs:decimal'><xs:totalDigits value='2'/><xs:fractionDigits value='3'/></xs:restriction></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Bad'><xs:restriction base='xs:int'><xs:minInclusive value='5'/><xs:maxInclusive value='4'/></xs:restriction></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Bad'><xs:restriction base='xs:token'><xs:whiteSpace value='replace'/></xs:restriction></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Bad'><xs:restriction base='xs:dateTimeStamp'><xs:explicitTimezone value='optional'/></xs:restriction></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Base' final='restriction'><xs:restriction base='xs:string'/></xs:simpleType><xs:simpleType name='Bad'><xs:restriction base='t:Base'/></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Base' final='list'><xs:restriction base='xs:string'/></xs:simpleType><xs:simpleType name='Bad'><xs:list itemType='t:Base'/></xs:simpleType>")]
    [TestCase("<xs:simpleType name='Base' final='union'><xs:restriction base='xs:string'/></xs:simpleType><xs:simpleType name='Bad'><xs:union memberTypes='t:Base'/></xs:simpleType>")]
    public void RejectsInvalidSimpleTypeStructuresFacetsAndDerivations(string content)
    {
        string source = Schema(content, "urn:test", " xmlns:t='urn:test'");

        XmlSchemaException exception = Assert.Throws<XmlSchemaException>(() => Compile(source))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SchemaFailure));
            Assert.That(exception.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Schema));
            Assert.That(exception.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
        });
    }

    [Test]
    public void RejectsCircularDerivationAndListsOfLists()
    {
        string circular = Schema(
            "<xs:simpleType name='A'><xs:restriction base='t:B'/></xs:simpleType>" +
            "<xs:simpleType name='B'><xs:restriction base='t:A'/></xs:simpleType>",
            "urn:test",
            " xmlns:t='urn:test'");
        string listOfList = Schema(
            "<xs:simpleType name='Items'><xs:list itemType='xs:string'/></xs:simpleType>" +
            "<xs:simpleType name='Bad'><xs:list itemType='t:Items'/></xs:simpleType>",
            "urn:test",
            " xmlns:t='urn:test'");

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<XmlSchemaException>(() => Compile(circular))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SchemaFailure));
            Assert.That(Assert.Throws<XmlSchemaException>(() => Compile(listOfList))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SchemaFailure));
        });
    }

    private static CompiledXmlSchemaSet Compile(string source)
    {
        return Compile(source, new TypeResolver());
    }

    private static CompiledXmlSchemaSet Compile(string source, TypeResolver resolver)
    {
        XmlResolverPolicy policy = new XmlResolverPolicy
        {
            Access = XmlResourceAccess.Allowed,
            AllowedSchemes = ["https"],
            MaximumDepth = 8,
            MaximumResourceBytes = 1048576,
            MaximumWorkUnits = 2097152
        };
        XmlSchemaSet result = new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Kind = XmlInputKind.String, Text = source, BaseUri = "https://example.test/root.xsd" }],
            new XmlSchemaValidationOptions { ResolverPolicy = policy },
            resolver);
        return (CompiledXmlSchemaSet)result.CompiledSet!;
    }

    private static CompiledXmlSchemaSimpleType Find(CompiledXmlSchemaSet compiled, string namespaceUri, string localName)
    {
        for (int index = compiled.SimpleTypes.Length - 1; index >= 0; index--)
        {
            XmlName? name = compiled.SimpleTypes[index].Name;
            if (name != null && name.NamespaceUri == namespaceUri && name.LocalName == localName)
            {
                return compiled.SimpleTypes[index];
            }
        }

        throw new AssertionException("The compiled simple type was not found: {" + namespaceUri + "}" + localName);
    }

    private static CompiledXmlSchemaFacet? Facet(CompiledXmlSchemaSimpleType type, XmlSchemaFacetKind kind)
    {
        return Facets(type, kind).LastOrDefault();
    }

    private static CompiledXmlSchemaFacet[] Facets(CompiledXmlSchemaSimpleType type, XmlSchemaFacetKind kind)
    {
        return type.EffectiveFacets.Where(facet => facet.Kind == kind).ToArray();
    }

    private static string Schema(string content, string? targetNamespace)
    {
        return Schema(content, targetNamespace, "");
    }

    private static string Schema(string content, string? targetNamespace, string extraAttributes)
    {
        string target = targetNamespace == null ? "" : " targetNamespace='" + targetNamespace + "'";
        return "<xs:schema xmlns:xs='" + Xsd + "'" + target + extraAttributes + ">" + content + "</xs:schema>";
    }

    private sealed class TypeResolver : IXmlResourceResolver
    {
        private readonly Dictionary<string, string> resources = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly List<string> Requests = new List<string>();

        public void Add(string absoluteUri, string source)
        {
            resources.Add(absoluteUri, source);
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            string absolute = new Uri(new Uri(baseUri!), systemIdentifier!).AbsoluteUri;
            Requests.Add(absolute);
            if (!resources.ContainsKey(absolute))
            {
                throw new InvalidOperationException("No schema was registered for " + absolute + ".");
            }

            return new XmlResolvedResource
            {
                Kind = kind,
                Access = XmlResourceAccess.Allowed,
                AbsoluteUri = absolute,
                Input = new XmlInput { Kind = XmlInputKind.String, Text = resources[absolute], BaseUri = absolute }
            };
        }
    }
}
