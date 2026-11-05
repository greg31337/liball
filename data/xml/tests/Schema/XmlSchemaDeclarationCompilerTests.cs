using xml;

namespace xml.Tests;

[TestFixture]
public class XmlSchemaDeclarationCompilerTests
{
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void CompilesGlobalAndLocalElementAndAttributeDeclarations()
    {
        string source = Schema(
            "<xs:attribute name='globalCode' type='xs:token' fixed='G' inheritable='true'/>" +
            "<xs:element name='head' type='xs:string' abstract='true' nillable='true' default='fallback' block='substitution' final='restriction'/>" +
            "<xs:complexType name='Container'><xs:sequence>" +
            "<xs:element name='local' type='xs:int' form='unqualified' default='4' nillable='true'/>" +
            "<xs:element name='nested'><xs:complexType><xs:attribute name='inside' type='xs:boolean' form='qualified'/></xs:complexType></xs:element>" +
            "<xs:element ref='t:head'/>" +
            "</xs:sequence><xs:attribute ref='t:globalCode' use='required'/><xs:attribute name='plain' default='x'/></xs:complexType>",
            "urn:test",
            " xmlns:t='urn:test' elementFormDefault='qualified' attributeFormDefault='unqualified'");

        CompiledXmlSchemaSet compiled = Compile(source);
        CompiledXmlSchemaElementDeclaration head = FindElement(compiled, "urn:test", "head", true);
        CompiledXmlSchemaAttributeDeclaration globalCode = FindAttribute(compiled, "urn:test", "globalCode", true);
        CompiledXmlSchemaComplexType container = FindType(compiled, "urn:test", "Container");
        CompiledXmlSchemaParticle localParticle = container.Particle!.Children[0];
        CompiledXmlSchemaParticle nestedParticle = container.Particle.Children[1];
        CompiledXmlSchemaParticle referenceParticle = container.Particle.Children[2];
        CompiledXmlSchemaElementDeclaration local = localParticle.ElementDeclaration!;
        CompiledXmlSchemaElementDeclaration nested = nestedParticle.ElementDeclaration!;
        CompiledXmlSchemaAttributeDeclaration inside = nested.ComplexType!.Attributes[0].Declaration!;
        CompiledXmlSchemaAttributeDeclaration plain = container.Attributes[1].Declaration!;

        Assert.Multiple(() =>
        {
            Assert.That(head.IsGlobal, Is.True);
            Assert.That(head.Name.NamespaceUri, Is.EqualTo("urn:test"));
            Assert.That(head.SimpleType!.AtomicType, Is.EqualTo(XdmAtomicType.String));
            Assert.That(head.IsAbstract, Is.True);
            Assert.That(head.IsNillable, Is.True);
            Assert.That(head.DefaultValue, Is.EqualTo("fallback"));
            Assert.That(head.Block, Is.EqualTo(XmlSchemaDerivationMethod.Substitution));
            Assert.That(head.Final, Is.EqualTo(XmlSchemaDerivationMethod.Restriction));
            Assert.That(globalCode.Type!.AtomicType, Is.EqualTo(XdmAtomicType.Token));
            Assert.That(globalCode.FixedValue, Is.EqualTo("G"));
            Assert.That(globalCode.Inheritable, Is.True);
            Assert.That(local.IsGlobal, Is.False);
            Assert.That(local.Name.NamespaceUri, Is.Null);
            Assert.That(local.IsQualified, Is.False);
            Assert.That(local.SimpleType!.AtomicType, Is.EqualTo(XdmAtomicType.Int));
            Assert.That(local.DefaultValue, Is.EqualTo("4"));
            Assert.That(local.IsNillable, Is.True);
            Assert.That(nested.Name.NamespaceUri, Is.EqualTo("urn:test"));
            Assert.That(nested.ComplexType!.IsAnonymous, Is.True);
            Assert.That(inside.Name.NamespaceUri, Is.EqualTo("urn:test"));
            Assert.That(inside.Type!.AtomicType, Is.EqualTo(XdmAtomicType.Boolean));
            Assert.That(referenceParticle.ElementDeclaration, Is.SameAs(head));
            Assert.That(container.Attributes[0].Declaration, Is.SameAs(globalCode));
            Assert.That(container.Attributes[0].Type, Is.SameAs(globalCode.Type));
            Assert.That(container.Attributes[0].FixedValue, Is.EqualTo("G"));
            Assert.That(plain.Name.NamespaceUri, Is.Null);
            Assert.That(plain.Type!.AtomicType, Is.EqualTo(XdmAtomicType.String));
            Assert.That(compiled.Elements.Count(element => !element.IsGlobal), Is.EqualTo(2));
            Assert.That(compiled.Attributes.Count(attribute => !attribute.IsGlobal), Is.EqualTo(2));
        });
    }

    [Test]
    public void CompilesSubstitutionGroupsAndTypeAlternatives()
    {
        string source = Schema(
            "<xs:complexType name='Base'><xs:sequence><xs:element name='value' type='xs:string'/></xs:sequence></xs:complexType>" +
            "<xs:complexType name='Derived'><xs:complexContent><xs:extension base='t:Base'><xs:attribute name='code'/></xs:extension></xs:complexContent></xs:complexType>" +
            "<xs:element name='head' type='t:Base'/>" +
            "<xs:element name='member' type='t:Derived' substitutionGroup='t:head'>" +
            "<xs:alternative test='@kind = 1' type='xs:int'/>" +
            "<xs:alternative><xs:simpleType><xs:restriction base='xs:string'><xs:maxLength value='8'/></xs:restriction></xs:simpleType></xs:alternative>" +
            "</xs:element>",
            "urn:test",
            " xmlns:t='urn:test'");

        CompiledXmlSchemaSet compiled = Compile(source);
        CompiledXmlSchemaElementDeclaration head = FindElement(compiled, "urn:test", "head", true);
        CompiledXmlSchemaElementDeclaration member = FindElement(compiled, "urn:test", "member", true);

        Assert.Multiple(() =>
        {
            Assert.That(member.SubstitutionGroupHead, Is.SameAs(head));
            Assert.That(head.SubstitutionGroupMembers, Is.EqualTo(new[] { member }));
            Assert.That(member.ComplexType!.Name!.LocalName, Is.EqualTo("Derived"));
            Assert.That(member.TypeAlternatives, Has.Length.EqualTo(2));
            Assert.That(member.TypeAlternatives[0].Test, Is.EqualTo("@kind = 1"));
            Assert.That(member.TypeAlternatives[0].SimpleType!.AtomicType, Is.EqualTo(XdmAtomicType.Int));
            Assert.That(member.TypeAlternatives[1].Test, Is.Null);
            Assert.That(member.TypeAlternatives[1].SimpleType!.IsAnonymous, Is.True);
            Assert.That(member.TypeAlternatives[1].SimpleType!.EffectiveFacets.Last(facet => facet.Kind == XmlSchemaFacetKind.MaxLength).Value, Is.EqualTo("8"));
        });
    }

    [Test]
    public void CompilesIdentityConstraintsAndNotations()
    {
        string source = Schema(
            "<xs:notation name='image' public='  image-format  ' system='https://example.test/image'/>" +
            "<xs:element name='catalog'>" +
            "<xs:key name='itemKey' xpathDefaultNamespace='##targetNamespace'><xs:selector xpath=' t:item '/><xs:field xpath='@id'/></xs:key>" +
            "<xs:keyref name='itemReference' refer='t:itemKey'><xs:selector xpath='t:reference'/><xs:field xpath='@item'/></xs:keyref>" +
            "<xs:unique name='pair'><xs:selector xpath='t:item'/><xs:field xpath='@a'/><xs:field xpath='@b'/></xs:unique>" +
            "</xs:element>",
            "urn:test",
            " xmlns:t='urn:test'");

        CompiledXmlSchemaSet compiled = Compile(source);
        CompiledXmlSchemaElementDeclaration catalog = FindElement(compiled, "urn:test", "catalog", true);
        CompiledXmlSchemaIdentityConstraint key = catalog.IdentityConstraints[0];
        CompiledXmlSchemaIdentityConstraint reference = catalog.IdentityConstraints[1];
        CompiledXmlSchemaIdentityConstraint unique = catalog.IdentityConstraints[2];
        CompiledXmlSchemaNotation notation = compiled.Notations.Single();

        Assert.Multiple(() =>
        {
            Assert.That(compiled.IdentityConstraints, Has.Length.EqualTo(3));
            Assert.That(key.Kind, Is.EqualTo(XmlSchemaIdentityConstraintKind.Key));
            Assert.That(key.Selector, Is.EqualTo("t:item"));
            Assert.That(key.Fields, Is.EqualTo(new[] { "@id" }));
            Assert.That(key.XPathDefaultNamespace, Is.EqualTo("##targetNamespace"));
            Assert.That(key.OwnerElement, Is.SameAs(catalog));
            Assert.That(reference.Kind, Is.EqualTo(XmlSchemaIdentityConstraintKind.KeyReference));
            Assert.That(reference.ReferencedConstraint, Is.SameAs(key));
            Assert.That(unique.Kind, Is.EqualTo(XmlSchemaIdentityConstraintKind.Unique));
            Assert.That(unique.Fields, Is.EqualTo(new[] { "@a", "@b" }));
            Assert.That(notation.Name.NamespaceUri, Is.EqualTo("urn:test"));
            Assert.That(notation.PublicIdentifier, Is.EqualTo("image-format"));
            Assert.That(notation.SystemIdentifier, Is.EqualTo("https://example.test/image"));
        });
    }

    [Test]
    public void AppliesLocalTargetNamespaceOverrides()
    {
        string source = Schema(
            "<xs:complexType name='Container'><xs:sequence><xs:element name='foreign' targetNamespace='urn:local'/></xs:sequence>" +
            "<xs:attribute name='foreignAttribute' targetNamespace='urn:attributes'/><xs:attribute name='foreignAttribute' targetNamespace='urn:other-attributes'/></xs:complexType>",
            "urn:test",
            "");

        CompiledXmlSchemaComplexType type = FindType(Compile(source), "urn:test", "Container");

        Assert.Multiple(() =>
        {
            Assert.That(type.Particle!.Children[0].ElementDeclaration!.Name.NamespaceUri, Is.EqualTo("urn:local"));
            Assert.That(type.Particle.Children[0].ElementDeclaration!.IsQualified, Is.True);
            Assert.That(type.Attributes[0].Declaration!.Name.NamespaceUri, Is.EqualTo("urn:attributes"));
            Assert.That(type.Attributes[0].Declaration!.IsQualified, Is.True);
            Assert.That(type.Attributes[1].Declaration!.Name.NamespaceUri, Is.EqualTo("urn:other-attributes"));
        });
    }

    [Test]
    public void AppliesOverriddenDeclarationsToReferences()
    {
        MemoryResolver resolver = new MemoryResolver();
        resolver.Add(
            "https://example.test/base.xsd",
            Schema("<xs:element name='item' type='xs:string'/><xs:attribute name='code' type='xs:string'/><xs:notation name='format' system='old'/>", "urn:test", ""));
        string source = Schema(
            "<xs:override schemaLocation='base.xsd'>" +
            "<xs:element name='item' type='xs:int' fixed='7'/>" +
            "<xs:attribute name='code' type='xs:token' default='new'/>" +
            "<xs:notation name='format' public='new-format'/>" +
            "</xs:override>" +
            "<xs:complexType name='Container'><xs:sequence><xs:element ref='t:item'/></xs:sequence><xs:attribute ref='t:code'/></xs:complexType>",
            "urn:test",
            " xmlns:t='urn:test'");

        CompiledXmlSchemaSet compiled = CompileWithResolver(source, resolver);
        CompiledXmlSchemaElementDeclaration element = FindElement(compiled, "urn:test", "item", true);
        CompiledXmlSchemaAttributeDeclaration attribute = FindAttribute(compiled, "urn:test", "code", true);
        CompiledXmlSchemaNotation notation = compiled.Notations.Last(value => value.Name.LocalName == "format");
        CompiledXmlSchemaComplexType container = FindType(compiled, "urn:test", "Container");

        Assert.Multiple(() =>
        {
            Assert.That(element.PreviousDefinition, Is.Not.Null);
            Assert.That(element.SimpleType!.AtomicType, Is.EqualTo(XdmAtomicType.Int));
            Assert.That(element.FixedValue, Is.EqualTo("7"));
            Assert.That(attribute.PreviousDefinition, Is.Not.Null);
            Assert.That(attribute.Type!.AtomicType, Is.EqualTo(XdmAtomicType.Token));
            Assert.That(attribute.DefaultValue, Is.EqualTo("new"));
            Assert.That(notation.PreviousDefinition, Is.Not.Null);
            Assert.That(notation.PublicIdentifier, Is.EqualTo("new-format"));
            Assert.That(container.Particle!.Children[0].ElementDeclaration, Is.SameAs(element));
            Assert.That(container.Attributes[0].Declaration, Is.SameAs(attribute));
        });
    }

    [Test]
    [TestCase("<xs:element name='a' default='x' fixed='x'/>")]
    [TestCase("<xs:attribute name='a' default='x' fixed='x'/>")]
    [TestCase("<xs:element name='a' type='xs:string'><xs:simpleType><xs:restriction base='xs:string'/></xs:simpleType></xs:element>")]
    [TestCase("<xs:complexType name='C'><xs:sequence><xs:element ref='t:missing'/></xs:sequence></xs:complexType>")]
    [TestCase("<xs:element name='a'><xs:alternative/><xs:alternative test='true()' type='xs:string'/></xs:element>")]
    [TestCase("<xs:element name='a'><xs:key name='k'><xs:field xpath='@id'/></xs:key></xs:element>")]
    [TestCase("<xs:element name='a'><xs:key name='k'><xs:selector xpath='item'/></xs:key></xs:element>")]
    [TestCase("<xs:element name='a'><xs:keyref name='r' refer='t:missing'><xs:selector xpath='item'/><xs:field xpath='@id'/></xs:keyref></xs:element>")]
    [TestCase("<xs:notation name='n'/>")]
    [TestCase("<xs:element name='a' substitutionGroup='t:missing'/>")]
    [TestCase("<xs:element name='a' substitutionGroup='t:b'/><xs:element name='b' substitutionGroup='t:a'/>")]
    [TestCase("<xs:element name='head' type='xs:int'/><xs:element name='member' type='xs:string' substitutionGroup='t:head'/>")]
    [TestCase("<xs:attribute name='a' fixed='x'/><xs:complexType name='C'><xs:attribute ref='t:a' fixed='y'/></xs:complexType>")]
    [TestCase("<xs:complexType name='C'><xs:sequence><xs:element name='child'/></xs:sequence></xs:complexType><xs:element name='a' type='t:C' default='x'/>")]
    [TestCase("<xs:element name='a'/><xs:complexType name='C'><xs:sequence><xs:element ref='t:a'><xs:key name='k'><xs:selector xpath='.'/><xs:field xpath='@id'/></xs:key></xs:element></xs:sequence></xs:complexType>")]
    public void RejectsInvalidDeclarationStructuresAndReferences(string content)
    {
        Assert.Throws<XmlSchemaException>(() => Compile(Schema(content, "urn:test", " xmlns:t='urn:test'")));
    }

    [Test]
    [TestCase("elementFormDefault='invalid'")]
    [TestCase("attributeFormDefault='invalid'")]
    public void RejectsInvalidSchemaFormDefaults(string extraAttribute)
    {
        Assert.Throws<XmlSchemaException>(() => Compile(Schema("", "urn:test", " " + extraAttribute)));
    }

    [Test]
    [TestCase("<xs:complexType name='C'><xs:sequence><xs:element name='a' form='qualified' targetNamespace='urn:other'/></xs:sequence></xs:complexType>")]
    [TestCase("<xs:complexType name='C'><xs:attribute name='a' form='qualified' targetNamespace='urn:other'/></xs:complexType>")]
    [TestCase("<xs:element name='a' minOccurs='0'/>")]
    [TestCase("<xs:attribute name='a' use='required'/>")]
    [TestCase("<xs:attribute name='a'/><xs:complexType name='C'><xs:attribute ref='t:a' form='qualified'/></xs:complexType>")]
    public void RejectsInvalidGlobalAndLocalFormUsage(string content)
    {
        Assert.Throws<XmlSchemaException>(() => Compile(Schema(content, "urn:test", "")));
    }

    private static CompiledXmlSchemaSet Compile(string source)
    {
        XmlResolverPolicy policy = new XmlResolverPolicy
        {
            Access = XmlResourceAccess.Denied,
            AllowedSchemes = [],
            MaximumDepth = 8,
            MaximumResourceBytes = 1048576,
            MaximumWorkUnits = 2097152
        };
        XmlSchemaSet result = new XmlSchemaCompiler().CompileXmlSchemas(
            [new XmlInput { Kind = XmlInputKind.String, Text = source, BaseUri = "https://example.test/root.xsd" }],
            new XmlSchemaValidationOptions { ResolverPolicy = policy },
            new RejectingResolver());
        return (CompiledXmlSchemaSet)result.CompiledSet!;
    }

    private static CompiledXmlSchemaSet CompileWithResolver(string source, IXmlResourceResolver resolver)
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

    private static CompiledXmlSchemaElementDeclaration FindElement(CompiledXmlSchemaSet compiled, string namespaceUri, string localName, bool global)
    {
        return compiled.Elements.Last(element => element.IsGlobal == global && element.Name.NamespaceUri == namespaceUri && element.Name.LocalName == localName);
    }

    private static CompiledXmlSchemaAttributeDeclaration FindAttribute(CompiledXmlSchemaSet compiled, string namespaceUri, string localName, bool global)
    {
        return compiled.Attributes.Last(attribute => attribute.IsGlobal == global && attribute.Name.NamespaceUri == namespaceUri && attribute.Name.LocalName == localName);
    }

    private static CompiledXmlSchemaComplexType FindType(CompiledXmlSchemaSet compiled, string namespaceUri, string localName)
    {
        return compiled.ComplexTypes.Last(type => type.Name != null && type.Name.NamespaceUri == namespaceUri && type.Name.LocalName == localName);
    }

    private static string Schema(string content, string? targetNamespace, string extraAttributes)
    {
        string target = targetNamespace == null ? "" : " targetNamespace='" + targetNamespace + "'";
        return "<xs:schema xmlns:xs='" + Xsd + "'" + target + extraAttributes + ">" + content + "</xs:schema>";
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

        public void Add(string absoluteUri, string source)
        {
            resources.Add(absoluteUri, source);
        }

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            string absolute = new Uri(new Uri(baseUri!), systemIdentifier!).AbsoluteUri;
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
