using xml;

namespace xml.Tests;

[TestFixture]
public class XmlSchemaPublicCoverageTests
{
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";
    private const string Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    [Test]
    public void PublicCompilerAssemblesEverySchemaComponentFamilyAcrossIncludesAndImports()
    {
        MemoryResolver resolver = new MemoryResolver();
        resolver.Add(
            "https://example.test/schema/included.xsd",
            Schema(
                "<xs:simpleType name='IncludedCode'><xs:restriction base='xs:string'/></xs:simpleType>" +
                "<xs:group name='IncludedGroup'><xs:sequence><xs:element name='entry' type='xs:string'/></xs:sequence></xs:group>" +
                "<xs:attributeGroup name='IncludedAttributes'><xs:attribute name='code' type='IncludedCode'/></xs:attributeGroup>" +
                "<xs:attribute name='includedAttribute' type='IncludedCode'/>" +
                "<xs:notation name='textFormat' public='text/plain'/>",
                null,
                ""));
        resolver.Add(
            "https://example.test/schema/remote.xsd",
            Schema(
                "<xs:complexType name='RemoteType'><xs:attribute name='remoteCode' type='xs:int'/></xs:complexType>" +
                "<xs:element name='remote' type='r:RemoteType'/>",
                "urn:remote",
                " xmlns:r='urn:remote'"));
        string root = Schema(
            "<xs:include schemaLocation='included.xsd'/>" +
            "<xs:import namespace='urn:remote' schemaLocation='remote.xsd'/>" +
            "<xs:simpleType name='CodeList'><xs:list itemType='t:IncludedCode'/></xs:simpleType>" +
            "<xs:simpleType name='CodeChoice'><xs:union memberTypes='xs:int t:IncludedCode'/></xs:simpleType>" +
            "<xs:complexType name='Base'><xs:group ref='t:IncludedGroup'/><xs:attributeGroup ref='t:IncludedAttributes'/></xs:complexType>" +
            "<xs:complexType name='Extended'><xs:complexContent><xs:extension base='t:Base'><xs:sequence><xs:element ref='r:remote' minOccurs='0'/></xs:sequence></xs:extension></xs:complexContent></xs:complexType>" +
            "<xs:attribute name='rootAttribute' type='t:CodeChoice'/>" +
            "<xs:element name='catalog' type='t:Extended'><xs:unique name='uniqueEntry'><xs:selector xpath='t:entry'/><xs:field xpath='.'/></xs:unique></xs:element>",
            "urn:root",
            " xmlns:t='urn:root' xmlns:r='urn:remote' elementFormDefault='qualified'");

        XmlSchemaSet result = Compile(
            [Input(root, "https://example.test/schema/root.xsd")],
            resolver,
            AllowedOptions());
        CompiledXmlSchemaSet compiled = (CompiledXmlSchemaSet)result.CompiledSet!;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsImmutable, Is.True);
            Assert.That(result.SchemaDocuments, Has.Length.EqualTo(3));
            Assert.That(result.TargetNamespaces, Is.EqualTo(new[] { "urn:root", "urn:remote" }));
            Assert.That(compiled.Modules, Has.Length.EqualTo(3));
            Assert.That(compiled.SimpleTypes.Any(type => IsNamed(type.Name, "urn:root", "IncludedCode")), Is.True);
            Assert.That(compiled.SimpleTypes.Any(type => IsNamed(type.Name, "urn:root", "CodeList")), Is.True);
            Assert.That(compiled.SimpleTypes.Any(type => IsNamed(type.Name, "urn:root", "CodeChoice")), Is.True);
            Assert.That(compiled.ComplexTypes.Any(type => IsNamed(type.Name, "urn:root", "Base")), Is.True);
            Assert.That(compiled.ComplexTypes.Any(type => IsNamed(type.Name, "urn:remote", "RemoteType")), Is.True);
            Assert.That(compiled.Groups.Any(group => IsNamed(group.Name, "urn:root", "IncludedGroup")), Is.True);
            Assert.That(compiled.AttributeGroups.Any(group => IsNamed(group.Name, "urn:root", "IncludedAttributes")), Is.True);
            Assert.That(compiled.Elements.Any(element => element.IsGlobal && IsNamed(element.Name, "urn:root", "catalog")), Is.True);
            Assert.That(compiled.Attributes.Any(attribute => attribute.IsGlobal && IsNamed(attribute.Name, "urn:root", "rootAttribute")), Is.True);
            Assert.That(compiled.IdentityConstraints.Any(constraint => IsNamed(constraint.Name, "urn:root", "uniqueEntry")), Is.True);
            Assert.That(compiled.Notations.Any(notation => IsNamed(notation.Name, "urn:root", "textFormat")), Is.True);
            Assert.That(resolver.Requests, Is.EqualTo(new[]
            {
                "https://example.test/schema/included.xsd",
                "https://example.test/schema/remote.xsd"
            }));
        });
    }

    [Test]
    public void PublicCompilerAndValidatorCoverEveryFacetAndDerivationFamily()
    {
        string schema = Schema(
            "<xs:simpleType name='Text'><xs:restriction base='xs:string'>" +
            "<xs:length value='4'/><xs:minLength value='2'/><xs:maxLength value='6'/>" +
            "<xs:pattern value='[A-Z]+'/><xs:enumeration value='TEST'/><xs:whiteSpace value='collapse'/>" +
            "<xs:assertion test='string-length($value) = 4'/></xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='Amount'><xs:restriction base='xs:decimal'>" +
            "<xs:minInclusive value='0'/><xs:maxInclusive value='100'/>" +
            "<xs:totalDigits value='5'/><xs:fractionDigits value='2'/><xs:minScale value='0'/><xs:maxScale value='2'/>" +
            "</xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='ExclusiveAmount'><xs:restriction base='xs:decimal'><xs:minExclusive value='1'/><xs:maxExclusive value='99'/></xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='Zoned'><xs:restriction base='xs:dateTime'><xs:explicitTimezone value='required'/></xs:restriction></xs:simpleType>" +
            "<xs:simpleType name='Numbers'><xs:list itemType='xs:int'/></xs:simpleType>" +
            "<xs:simpleType name='Choice'><xs:union memberTypes='xs:int xs:boolean'/></xs:simpleType>" +
            "<xs:complexType name='Base'><xs:sequence><xs:element name='base'/></xs:sequence></xs:complexType>" +
            "<xs:complexType name='Extended'><xs:complexContent><xs:extension base='t:Base'><xs:sequence><xs:element name='extra'/></xs:sequence></xs:extension></xs:complexContent></xs:complexType>" +
            "<xs:complexType name='Restricted'><xs:complexContent><xs:restriction base='t:Extended'><xs:sequence><xs:element name='base'/></xs:sequence></xs:restriction></xs:complexContent></xs:complexType>" +
            "<xs:element name='text' type='t:Text'/><xs:element name='amount' type='t:Amount'/><xs:element name='exclusiveAmount' type='t:ExclusiveAmount'/>" +
            "<xs:element name='zoned' type='t:Zoned'/><xs:element name='numbers' type='t:Numbers'/>" +
            "<xs:element name='choice' type='t:Choice'/><xs:element name='extended' type='t:Extended'/>",
            "urn:test",
            " xmlns:t='urn:test' elementFormDefault='qualified'");
        XmlSchemaSet set = Compile(schema);
        CompiledXmlSchemaSet compiled = (CompiledXmlSchemaSet)set.CompiledSet!;
        XmlSchemaFacetKind[] declaredKinds = compiled.SimpleTypes
            .Where(type => type.Module != null && type.Module.EffectiveTargetNamespace == "urn:test")
            .SelectMany(type => type.DeclaredFacets)
            .Select(facet => facet.Kind)
            .Distinct()
            .OrderBy(kind => kind)
            .ToArray();
        CompiledXmlSchemaSimpleType numbers = FindSimpleType(compiled, "Numbers");
        CompiledXmlSchemaSimpleType choice = FindSimpleType(compiled, "Choice");
        CompiledXmlSchemaComplexType extended = FindComplexType(compiled, "Extended");
        CompiledXmlSchemaComplexType restricted = FindComplexType(compiled, "Restricted");

        Assert.Multiple(() =>
        {
            Assert.That(declaredKinds, Is.EqualTo(Enum.GetValues<XmlSchemaFacetKind>()));
            Assert.That(FindSimpleType(compiled, "Text").DerivationMethod, Is.EqualTo(XmlSchemaDerivationMethod.Restriction));
            Assert.That(numbers.Variety, Is.EqualTo(XmlSchemaSimpleVariety.List));
            Assert.That(numbers.DerivationMethod, Is.EqualTo(XmlSchemaDerivationMethod.List));
            Assert.That(choice.Variety, Is.EqualTo(XmlSchemaSimpleVariety.Union));
            Assert.That(choice.DerivationMethod, Is.EqualTo(XmlSchemaDerivationMethod.Union));
            Assert.That(extended.DerivationMethod, Is.EqualTo(XmlSchemaDerivationMethod.Extension));
            Assert.That(restricted.DerivationMethod, Is.EqualTo(XmlSchemaDerivationMethod.Restriction));
            Assert.That(Validate(set, "<text xmlns='urn:test'>  TEST  </text>").Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(Validate(set, "<text xmlns='urn:test'>FAILS</text>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(Validate(set, "<amount xmlns='urn:test'>5.00</amount>").Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(Validate(set, "<amount xmlns='urn:test'>101</amount>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(Validate(set, "<exclusiveAmount xmlns='urn:test'>99</exclusiveAmount>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(Validate(set, "<zoned xmlns='urn:test'>2026-09-12T12:00:00Z</zoned>").Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(Validate(set, "<zoned xmlns='urn:test'>2026-09-12T12:00:00</zoned>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(Validate(set, "<numbers xmlns='urn:test'>01 2</numbers>").Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(Validate(set, "<choice xmlns='urn:test'>true</choice>").Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(Validate(set, "<extended xmlns='urn:test'><base/><extra/></extended>").Validity, Is.EqualTo(XmlSchemaValidity.Valid));
        });
    }

    [Test]
    public void PublicValidatorAppliesStrictLaxAndSkipWildcards()
    {
        XmlSchemaSet set = Compile(Schema(
            "<xs:element name='strict'/><xs:element name='lax'/>" +
            "<xs:element name='root'><xs:complexType><xs:sequence>" +
            "<xs:any namespace='##targetNamespace' processContents='strict'/>" +
            "<xs:any namespace='##targetNamespace' processContents='lax'/>" +
            "<xs:any namespace='##targetNamespace' processContents='skip'/>" +
            "</xs:sequence><xs:anyAttribute namespace='##other' processContents='lax'/></xs:complexType></xs:element>",
            "urn:test",
            " elementFormDefault='qualified'"));
        XmlDocument document = Parse("<root xmlns='urn:test' xmlns:o='urn:other' o:unknown='x'><strict/><lax/><unknown/></root>");

        XmlSchemaValidationResult result = Validate(set, document, new XmlSchemaValidationOptions { AnnotateTree = true });
        XmlNode[] children = document.DocumentElement!.Children!.Where(node => node.Kind == XmlNodeKind.Element).ToArray();
        XmlNode otherAttribute = document.DocumentElement.Attributes!.Single(attribute => attribute.Name!.LocalName == "unknown");
        XmlSchemaTypeInfo strictInformation = children[0].TypeInfo!;
        XmlSchemaTypeInfo laxInformation = children[1].TypeInfo!;
        XmlSchemaTypeInfo skippedInformation = children[2].TypeInfo!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(strictInformation.Declaration, Is.Not.Null);
            Assert.That(strictInformation.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(laxInformation.Declaration, Is.Not.Null);
            Assert.That(laxInformation.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(skippedInformation.Declaration, Is.Null);
            Assert.That(skippedInformation.Validity, Is.EqualTo(XmlSchemaValidity.NotKnown));
            Assert.That(otherAttribute.TypeInfo!.Validity, Is.EqualTo(XmlSchemaValidity.NotKnown));
            Assert.That(Validate(set, "<root xmlns='urn:test'><missing/><lax/><unknown/></root>").Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
        });
    }

    [Test]
    public void PublicValidatorEnforcesKeyUniqueAndKeyReferenceConstraints()
    {
        XmlSchemaSet set = Compile(Schema(
            "<xs:complexType name='Row'><xs:attribute name='id' type='xs:int'/><xs:attribute name='code' type='xs:string'/></xs:complexType>" +
            "<xs:complexType name='Reference'><xs:attribute name='row' type='xs:int'/></xs:complexType>" +
            "<xs:element name='root'><xs:complexType><xs:choice minOccurs='0' maxOccurs='unbounded'>" +
            "<xs:element name='row' type='t:Row'/><xs:element name='reference' type='t:Reference'/>" +
            "</xs:choice></xs:complexType>" +
            "<xs:key name='rowKey'><xs:selector xpath='t:row'/><xs:field xpath='@id'/></xs:key>" +
            "<xs:unique name='rowCode'><xs:selector xpath='t:row'/><xs:field xpath='@code'/></xs:unique>" +
            "<xs:keyref name='rowReference' refer='t:rowKey'><xs:selector xpath='t:reference'/><xs:field xpath='@row'/></xs:keyref>" +
            "</xs:element>",
            "urn:test",
            " xmlns:t='urn:test' elementFormDefault='qualified'"));

        XmlSchemaValidationResult valid = Validate(set, "<root xmlns='urn:test'><reference row='02'/><row id='1' code='A'/><row id='2' code='B'/></root>");
        XmlSchemaValidationResult duplicateKey = Validate(set, "<root xmlns='urn:test'><row id='1' code='A'/><row id='01' code='B'/></root>");
        XmlSchemaValidationResult duplicateUnique = Validate(set, "<root xmlns='urn:test'><row id='1' code='A'/><row id='2' code='A'/></root>");
        XmlSchemaValidationResult missingReference = Validate(set, "<root xmlns='urn:test'><row id='1' code='A'/><reference row='2'/></root>");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(duplicateKey.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(duplicateUnique.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(missingReference.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(missingReference.Diagnostics!.Any(diagnostic => diagnostic.Message!.Contains("does not match", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void PublicValidatorProjectsEveryPsviFieldForXsiAndValueConstraints()
    {
        XmlSchemaSet set = Compile(Schema(
            "<xs:simpleType name='Choice'><xs:union memberTypes='xs:int xs:boolean'/></xs:simpleType>" +
            "<xs:complexType name='Container'><xs:sequence><xs:element name='value' type='t:Choice' default='01'/></xs:sequence>" +
            "<xs:attribute name='id' type='xs:ID' use='required'/>" +
            "<xs:attribute name='reference' type='xs:IDREF' default='target'/>" +
            "<xs:attribute name='fixed' type='xs:int' fixed='1'/>" +
            "</xs:complexType>" +
            "<xs:element name='root' type='t:Container'/>" +
            "<xs:element name='nil' type='xs:string' nillable='true'/>",
            "urn:test",
            " xmlns:t='urn:test' elementFormDefault='qualified'"));
        XmlSchemaValidationOptions options = new XmlSchemaValidationOptions { AnnotateTree = true, ApplyDefaultValues = true };
        XmlDocument document = Parse("<root xmlns='urn:test' id='target' fixed='01'><value/></root>");

        XmlSchemaValidationResult result = Validate(set, document, options);
        XmlNode root = document.DocumentElement!;
        XmlSchemaTypeInfo rootInfo = root.TypeInfo!;
        XmlSchemaTypeInfo valueInfo = root.Children!.Single(node => node.Kind == XmlNodeKind.Element).TypeInfo!;
        XmlSchemaTypeInfo idInfo = root.Attributes!.Single(attribute => attribute.Name!.LocalName == "id").TypeInfo!;
        XmlSchemaTypeInfo referenceInfo = root.Attributes!.Single(attribute => attribute.Name!.LocalName == "reference").TypeInfo!;
        XmlSchemaTypeInfo fixedInfo = root.Attributes!.Single(attribute => attribute.Name!.LocalName == "fixed").TypeInfo!;
        XmlDocument nilledDocument = Parse("<nil xmlns='urn:test' xmlns:xsi='" + Xsi + "' xsi:nil='true'/>");
        XmlSchemaValidationResult nilled = Validate(set, nilledDocument, options);
        XmlSchemaTypeInfo nilledInfo = nilledDocument.DocumentElement!.TypeInfo!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(result.TypeAnnotations, Has.Length.EqualTo(5));
            Assert.That(rootInfo.TypeName!.LocalName, Is.EqualTo("Container"));
            Assert.That(rootInfo.TypeDefinition, Is.Not.Null);
            Assert.That(rootInfo.Declaration, Is.Not.Null);
            Assert.That(rootInfo.MemberTypeName, Is.Null);
            Assert.That(rootInfo.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(rootInfo.IsNilled, Is.False);
            Assert.That(valueInfo.TypeName!.LocalName, Is.EqualTo("Choice"));
            Assert.That(valueInfo.MemberTypeName!.LocalName, Is.EqualTo("int"));
            Assert.That(valueInfo.AtomicType, Is.EqualTo(XdmAtomicType.Int));
            Assert.That(valueInfo.IsDefaulted, Is.True);
            Assert.That(valueInfo.NormalizedValue, Is.EqualTo("01"));
            Assert.That(valueInfo.ActualValue!.CanonicalValue, Is.EqualTo("1"));
            Assert.That(valueInfo.ActualValues, Has.Length.EqualTo(1));
            Assert.That(valueInfo.TypeDefinition, Is.Not.Null);
            Assert.That(valueInfo.Declaration, Is.Not.Null);
            Assert.That(idInfo.IsId, Is.True);
            Assert.That(idInfo.IsIdReference, Is.False);
            Assert.That(referenceInfo.IsId, Is.False);
            Assert.That(referenceInfo.IsIdReference, Is.True);
            Assert.That(referenceInfo.IsDefaulted, Is.True);
            Assert.That(fixedInfo.IsDefaulted, Is.False);
            Assert.That(fixedInfo.ActualValue!.CanonicalValue, Is.EqualTo("1"));
            Assert.That(nilled.Validity, Is.EqualTo(XmlSchemaValidity.Valid));
            Assert.That(nilledInfo.IsNilled, Is.True);
            Assert.That(nilledInfo.ActualValue, Is.Null);
            Assert.That(nilledInfo.ActualValues, Is.Null);
        });
    }

    [Test]
    public void PublicSchemaApisDistinguishFatalInvalidAndNotKnownOutcomes()
    {
        XmlSchemaException fatal = Assert.Throws<XmlSchemaException>(() => Compile("<not-schema/>"))!;
        XmlSchemaSet numberSet = Compile(Schema("<xs:element name='number' type='xs:int'/>", "urn:test", ""));
        XmlSchemaValidationResult invalid = Validate(numberSet, "<number xmlns='urn:test'>not-an-int</number>");
        XmlSchemaSet emptySet = Compile([], new RejectingResolver(), DeniedOptions());
        XmlSchemaValidationResult notKnown = Validate(
            emptySet,
            Parse("<unknown xmlns='urn:unknown'><child/></unknown>"),
            new XmlSchemaValidationOptions { ProcessContents = XmlSchemaProcessContents.Lax, AnnotateTree = true });

        Assert.Multiple(() =>
        {
            Assert.That(fatal.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SchemaFailure));
            Assert.That(fatal.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Schema));
            Assert.That(fatal.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
            Assert.That(invalid.Succeeded, Is.True);
            Assert.That(invalid.Validity, Is.EqualTo(XmlSchemaValidity.Invalid));
            Assert.That(invalid.Diagnostics, Is.Not.Empty);
            Assert.That(invalid.Diagnostics!.All(diagnostic => diagnostic.Category == XmlErrorCategory.Schema), Is.True);
            Assert.That(notKnown.Succeeded, Is.True);
            Assert.That(notKnown.Validity, Is.EqualTo(XmlSchemaValidity.NotKnown));
            Assert.That(notKnown.Diagnostics, Is.Empty);
            Assert.That(notKnown.TypeAnnotations, Has.Length.EqualTo(2));
            Assert.That(notKnown.TypeAnnotations!.All(information => information.Validity == XmlSchemaValidity.NotKnown), Is.True);
        });
    }

    private static XmlSchemaSet Compile(string schema)
    {
        return Compile([Input(schema, "https://example.test/schema.xsd")], new RejectingResolver(), DeniedOptions());
    }

    private static XmlSchemaSet Compile(XmlInput[] inputs, IXmlResourceResolver resolver, XmlSchemaValidationOptions options)
    {
        return new XmlSchemaCompiler().CompileXmlSchemas(inputs, options, resolver);
    }

    private static XmlSchemaValidationResult Validate(XmlSchemaSet set, string source)
    {
        return Validate(set, Parse(source), new XmlSchemaValidationOptions());
    }

    private static XmlSchemaValidationResult Validate(XmlSchemaSet set, XmlDocument document, XmlSchemaValidationOptions options)
    {
        return new XmlSchemaValidator().ValidateXmlSchema(document, set, options);
    }

    private static CompiledXmlSchemaSimpleType FindSimpleType(CompiledXmlSchemaSet set, string localName)
    {
        return set.SimpleTypes.Last(type => IsNamed(type.Name, "urn:test", localName));
    }

    private static CompiledXmlSchemaComplexType FindComplexType(CompiledXmlSchemaSet set, string localName)
    {
        return set.ComplexTypes.Last(type => IsNamed(type.Name, "urn:test", localName));
    }

    private static bool IsNamed(XmlName? name, string namespaceUri, string localName)
    {
        return name != null && name.NamespaceUri == namespaceUri && name.LocalName == localName;
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
        return new XmlParser().ParseXmlDocument(
            new XmlInput { Kind = XmlInputKind.String, Text = source, BaseUri = "https://example.test/instance.xml" },
            options,
            null,
            null,
            null);
    }

    private static XmlInput Input(string source, string baseUri)
    {
        return new XmlInput { Kind = XmlInputKind.String, Text = source, BaseUri = baseUri };
    }

    private static string Schema(string content, string? targetNamespace, string extraAttributes)
    {
        string target = targetNamespace == null ? "" : " targetNamespace='" + targetNamespace + "'";
        return "<xs:schema xmlns:xs='" + Xsd + "'" + target + extraAttributes + ">" + content + "</xs:schema>";
    }

    private static XmlSchemaValidationOptions DeniedOptions()
    {
        return new XmlSchemaValidationOptions
        {
            ResolverPolicy = new XmlResolverPolicy { Access = XmlResourceAccess.Denied, AllowedSchemes = [] }
        };
    }

    private static XmlSchemaValidationOptions AllowedOptions()
    {
        return new XmlSchemaValidationOptions
        {
            ResolverPolicy = new XmlResolverPolicy
            {
                Access = XmlResourceAccess.Allowed,
                AllowedSchemes = ["https"],
                MaximumDepth = 8,
                MaximumResourceBytes = 1048576,
                MaximumWorkUnits = 2097152
            }
        };
    }

    private sealed class RejectingResolver : IXmlResourceResolver
    {
        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            throw new InvalidOperationException("No external resource was expected.");
        }
    }

    private sealed class MemoryResolver : IXmlResourceResolver
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
