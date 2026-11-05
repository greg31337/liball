using System.Numerics;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlSchemaComplexTypeCompilerTests
{
    private const string Xsd = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public void RegistersTheBuiltInAnyTypeContentAndAttributeWildcards()
    {
        CompiledXmlSchemaSet compiled = Compile(Schema("", null, ""));
        CompiledXmlSchemaComplexType anyType = FindType(compiled, Xsd, "anyType");

        Assert.Multiple(() =>
        {
            Assert.That(compiled.ComplexTypes, Has.Length.EqualTo(1));
            Assert.That(anyType.IsBuiltIn, Is.True);
            Assert.That(anyType.BaseType, Is.SameAs(anyType));
            Assert.That(anyType.ContentKind, Is.EqualTo(XmlSchemaContentKind.Mixed));
            Assert.That(anyType.Particle!.Kind, Is.EqualTo(XmlSchemaParticleKind.Sequence));
            Assert.That(anyType.Particle.Children[0].Kind, Is.EqualTo(XmlSchemaParticleKind.Any));
            Assert.That(anyType.Particle.Children[0].MinOccurs, Is.EqualTo(BigInteger.Zero));
            Assert.That(anyType.Particle.Children[0].MaxOccursUnbounded, Is.True);
            Assert.That(anyType.Particle.Children[0].Wildcard!.ProcessContents, Is.EqualTo(XmlSchemaProcessContents.Lax));
            Assert.That(anyType.AttributeWildcard!.Kind, Is.EqualTo(XmlSchemaWildcardKind.Any));
            Assert.That(anyType.AttributeWildcard.ProcessContents, Is.EqualTo(XmlSchemaProcessContents.Lax));
        });
    }

    [Test]
    public void CompilesMixedParticlesAttributesWildcardsOpenContentAndAssertions()
    {
        string source = Schema(
            "<xs:element name='global'/>" +
            "<xs:complexType name='Catalog' mixed='true' abstract='true' final='extension' block='restriction'>" +
            "<xs:openContent mode='suffix'><xs:any namespace='##other' processContents='lax' notQName='t:blocked ##defined'/></xs:openContent>" +
            "<xs:sequence minOccurs='0' maxOccurs='2'>" +
            "<xs:element name='item' maxOccurs='unbounded'/>" +
            "<xs:choice><xs:element ref='t:global'/><xs:any namespace='##targetNamespace ##local' processContents='skip'/></xs:choice>" +
            "</xs:sequence>" +
            "<xs:attribute name='code' type='xs:token' use='required' fixed='X'/>" +
            "<xs:anyAttribute notNamespace='##targetNamespace ##local' processContents='skip'/>" +
            "<xs:assert test='count(item) ge 1'/>" +
            "</xs:complexType>",
            "urn:test",
            " xmlns:t='urn:test'");

        CompiledXmlSchemaComplexType type = FindType(Compile(source), "urn:test", "Catalog");
        CompiledXmlSchemaParticle sequence = type.Particle!;
        CompiledXmlSchemaParticle choice = sequence.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(type.ContentKind, Is.EqualTo(XmlSchemaContentKind.Mixed));
            Assert.That(type.Abstract, Is.True);
            Assert.That(type.Final, Is.EqualTo(XmlSchemaDerivationMethod.Extension));
            Assert.That(type.Block, Is.EqualTo(XmlSchemaDerivationMethod.Restriction));
            Assert.That(sequence.Kind, Is.EqualTo(XmlSchemaParticleKind.Sequence));
            Assert.That(sequence.MinOccurs, Is.EqualTo(BigInteger.Zero));
            Assert.That(sequence.MaxOccurs, Is.EqualTo(new BigInteger(2)));
            Assert.That(sequence.Children[0].Name!.LocalName, Is.EqualTo("item"));
            Assert.That(sequence.Children[0].MaxOccursUnbounded, Is.True);
            Assert.That(choice.Kind, Is.EqualTo(XmlSchemaParticleKind.Choice));
            Assert.That(choice.Children[0].IsReference, Is.True);
            Assert.That(choice.Children[0].Name!.NamespaceUri, Is.EqualTo("urn:test"));
            Assert.That(choice.Children[1].Wildcard!.Namespaces, Is.EqualTo(new[] { "urn:test", "" }));
            Assert.That(choice.Children[1].Wildcard!.ProcessContents, Is.EqualTo(XmlSchemaProcessContents.Skip));
            Assert.That(type.Attributes, Has.Length.EqualTo(1));
            Assert.That(type.Attributes[0].Use, Is.EqualTo(XmlSchemaAttributeUseKind.Required));
            Assert.That(type.Attributes[0].FixedValue, Is.EqualTo("X"));
            Assert.That(type.Attributes[0].Type!.AtomicType, Is.EqualTo(XdmAtomicType.Token));
            Assert.That(type.AttributeWildcard!.Kind, Is.EqualTo(XmlSchemaWildcardKind.Exclusion));
            Assert.That(type.AttributeWildcard.ExcludedNamespaces, Is.EqualTo(new[] { "urn:test", "" }));
            Assert.That(type.OpenContent!.Mode, Is.EqualTo(XmlSchemaOpenContentMode.Suffix));
            Assert.That(type.OpenContent.Wildcard!.Kind, Is.EqualTo(XmlSchemaWildcardKind.Other));
            Assert.That(type.OpenContent.Wildcard.ExcludedNames[0].LocalName, Is.EqualTo("blocked"));
            Assert.That(type.OpenContent.Wildcard.ExcludesDefinedNames, Is.True);
            Assert.That(type.Assertions, Is.EqualTo(new[] { "count(item) ge 1" }));
        });
    }

    [Test]
    public void ResolvesForwardModelAndAttributeGroupsAndInlineAttributeTypes()
    {
        string source = Schema(
            "<xs:group name='Outer'><xs:sequence><xs:group ref='t:Inner' minOccurs='0'/></xs:sequence></xs:group>" +
            "<xs:attributeGroup name='Extended'><xs:attributeGroup ref='t:Common'/><xs:attribute name='label'>" +
            "<xs:simpleType><xs:restriction base='xs:string'><xs:maxLength value='12'/></xs:restriction></xs:simpleType>" +
            "</xs:attribute></xs:attributeGroup>" +
            "<xs:group name='Inner'><xs:choice><xs:element name='left'/><xs:element name='right'/></xs:choice></xs:group>" +
            "<xs:attributeGroup name='Common'><xs:attribute name='id' type='xs:ID' use='required'/><xs:anyAttribute namespace='##other' processContents='lax'/></xs:attributeGroup>" +
            "<xs:complexType name='UsesGroups'><xs:group ref='t:Outer'/><xs:attributeGroup ref='t:Extended'/></xs:complexType>",
            "urn:test",
            " xmlns:t='urn:test'");

        CompiledXmlSchemaSet compiled = Compile(source);
        CompiledXmlSchemaComplexType type = FindType(compiled, "urn:test", "UsesGroups");
        CompiledXmlSchemaGroup outer = FindGroup(compiled, "urn:test", "Outer");
        CompiledXmlSchemaAttributeGroup extended = FindAttributeGroup(compiled, "urn:test", "Extended");

        Assert.Multiple(() =>
        {
            Assert.That(compiled.Groups, Has.Length.EqualTo(2));
            Assert.That(compiled.AttributeGroups, Has.Length.EqualTo(2));
            Assert.That(outer.Particle!.Children[0].Kind, Is.EqualTo(XmlSchemaParticleKind.GroupReference));
            Assert.That(outer.Particle.Children[0].GroupDefinition!.Name.LocalName, Is.EqualTo("Inner"));
            Assert.That(outer.Particle.Children[0].Children[0].Kind, Is.EqualTo(XmlSchemaParticleKind.Choice));
            Assert.That(type.Particle!.GroupDefinition, Is.SameAs(outer));
            Assert.That(type.Attributes.Select(attribute => attribute.Name.LocalName), Is.EqualTo(new[] { "id", "label" }));
            Assert.That(type.Attributes[0].Type!.AtomicType, Is.EqualTo(XdmAtomicType.Id));
            Assert.That(type.Attributes[1].Type!.IsAnonymous, Is.True);
            Assert.That(type.Attributes[1].Type!.EffectiveFacets.Last(facet => facet.Kind == XmlSchemaFacetKind.MaxLength).Value, Is.EqualTo("12"));
            Assert.That(type.AttributeWildcard!.Kind, Is.EqualTo(XmlSchemaWildcardKind.Other));
            Assert.That(extended.Attributes, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void CompilesComplexContentExtensionAndRestrictionDerivations()
    {
        string source = Schema(
            "<xs:complexType name='Base'><xs:sequence><xs:element name='base'/></xs:sequence>" +
            "<xs:attribute name='code' type='xs:string'/><xs:assert test='@code'/></xs:complexType>" +
            "<xs:complexType name='Extended'><xs:complexContent mixed='true'><xs:extension base='t:Base'>" +
            "<xs:choice><xs:element name='extra'/><xs:any namespace='##other'/></xs:choice>" +
            "<xs:attribute name='version' type='xs:int' use='required'/><xs:assert test='@version gt 0'/>" +
            "</xs:extension></xs:complexContent></xs:complexType>" +
            "<xs:complexType name='Restricted'><xs:complexContent><xs:restriction base='t:Extended'>" +
            "<xs:sequence><xs:element name='base'/></xs:sequence><xs:attribute name='code' type='xs:string' use='required'/>" +
            "</xs:restriction></xs:complexContent></xs:complexType>",
            "urn:test",
            " xmlns:t='urn:test'");

        CompiledXmlSchemaSet compiled = Compile(source);
        CompiledXmlSchemaComplexType baseType = FindType(compiled, "urn:test", "Base");
        CompiledXmlSchemaComplexType extended = FindType(compiled, "urn:test", "Extended");
        CompiledXmlSchemaComplexType restricted = FindType(compiled, "urn:test", "Restricted");

        Assert.Multiple(() =>
        {
            Assert.That(extended.BaseType, Is.SameAs(baseType));
            Assert.That(extended.DerivationMethod, Is.EqualTo(XmlSchemaDerivationMethod.Extension));
            Assert.That(extended.ContentKind, Is.EqualTo(XmlSchemaContentKind.Mixed));
            Assert.That(extended.Particle!.Kind, Is.EqualTo(XmlSchemaParticleKind.Sequence));
            Assert.That(extended.Particle.Children[0], Is.SameAs(baseType.Particle));
            Assert.That(extended.Particle.Children[1].Kind, Is.EqualTo(XmlSchemaParticleKind.Choice));
            Assert.That(extended.Attributes.Select(attribute => attribute.Name.LocalName), Is.EqualTo(new[] { "code", "version" }));
            Assert.That(extended.Assertions, Is.EqualTo(new[] { "@code", "@version gt 0" }));
            Assert.That(restricted.BaseType, Is.SameAs(extended));
            Assert.That(restricted.DerivationMethod, Is.EqualTo(XmlSchemaDerivationMethod.Restriction));
            Assert.That(restricted.ContentKind, Is.EqualTo(XmlSchemaContentKind.ElementOnly));
            Assert.That(restricted.Particle!.Children[0].Name!.LocalName, Is.EqualTo("base"));
            Assert.That(restricted.Attributes.Select(attribute => attribute.Name.LocalName), Is.EqualTo(new[] { "code", "version" }));
        });
    }

    [Test]
    public void CompilesSimpleContentExtensionAndFacetBasedRestriction()
    {
        string source = Schema(
            "<xs:simpleType name='Money'><xs:restriction base='xs:decimal'><xs:fractionDigits value='2'/></xs:restriction></xs:simpleType>" +
            "<xs:complexType name='Amount'><xs:simpleContent><xs:extension base='t:Money'>" +
            "<xs:attribute name='currency' type='xs:token' use='required'/></xs:extension></xs:simpleContent></xs:complexType>" +
            "<xs:complexType name='SmallAmount'><xs:simpleContent><xs:restriction base='t:Amount'>" +
            "<xs:simpleType><xs:restriction base='t:Money'><xs:totalDigits value='5'/></xs:restriction></xs:simpleType>" +
            "<xs:minInclusive value='0'/><xs:attribute name='currency' use='prohibited'/><xs:assert test='$value lt 1000'/>" +
            "</xs:restriction></xs:simpleContent></xs:complexType>",
            "urn:test",
            " xmlns:t='urn:test'");

        CompiledXmlSchemaSet compiled = Compile(source);
        CompiledXmlSchemaComplexType amount = FindType(compiled, "urn:test", "Amount");
        CompiledXmlSchemaComplexType small = FindType(compiled, "urn:test", "SmallAmount");

        Assert.Multiple(() =>
        {
            Assert.That(amount.ContentKind, Is.EqualTo(XmlSchemaContentKind.Simple));
            Assert.That(amount.BaseSimpleType!.Name!.LocalName, Is.EqualTo("Money"));
            Assert.That(amount.SimpleContentType!.Name!.LocalName, Is.EqualTo("Money"));
            Assert.That(amount.Attributes[0].Name.LocalName, Is.EqualTo("currency"));
            Assert.That(small.BaseType, Is.SameAs(amount));
            Assert.That(small.SimpleContentType!.IsAnonymous, Is.True);
            Assert.That(small.SimpleContentType.EffectiveFacets.Last(facet => facet.Kind == XmlSchemaFacetKind.TotalDigits).Value, Is.EqualTo("5"));
            Assert.That(small.SimpleContentType.EffectiveFacets.Last(facet => facet.Kind == XmlSchemaFacetKind.MinInclusive).Value, Is.EqualTo("0"));
            Assert.That(small.Attributes, Is.Empty);
            Assert.That(small.Assertions, Is.EqualTo(new[] { "$value lt 1000" }));
            Assert.That(compiled.SimpleTypes.Count(type => type.IsAnonymous), Is.GreaterThanOrEqualTo(2));
        });
    }

    [Test]
    public void AppliesDefaultOpenContentToEmptyAndNonEmptyTypes()
    {
        string source = Schema(
            "<xs:defaultOpenContent appliesToEmpty='true' mode='interleave'><xs:any namespace='##any' processContents='skip'/></xs:defaultOpenContent>" +
            "<xs:complexType name='Empty'/>" +
            "<xs:complexType name='Elements'><xs:sequence><xs:element name='item'/></xs:sequence></xs:complexType>",
            "urn:test",
            "");

        CompiledXmlSchemaSet compiled = Compile(source);
        CompiledXmlSchemaComplexType empty = FindType(compiled, "urn:test", "Empty");
        CompiledXmlSchemaComplexType elements = FindType(compiled, "urn:test", "Elements");

        Assert.Multiple(() =>
        {
            Assert.That(empty.OpenContent!.IsDefault, Is.True);
            Assert.That(empty.OpenContent.Mode, Is.EqualTo(XmlSchemaOpenContentMode.Interleave));
            Assert.That(empty.OpenContent.Wildcard!.ProcessContents, Is.EqualTo(XmlSchemaProcessContents.Skip));
            Assert.That(empty.ContentKind, Is.EqualTo(XmlSchemaContentKind.ElementOnly));
            Assert.That(empty.Particle!.Kind, Is.EqualTo(XmlSchemaParticleKind.Sequence));
            Assert.That(empty.Particle.Children, Is.Empty);
            Assert.That(elements.OpenContent!.IsDefault, Is.True);
        });
    }

    [Test]
    public void AppliesAndSuppressesSchemaDefaultAttributeGroups()
    {
        string source = Schema(
            "<xs:attributeGroup name='Defaults'><xs:attribute name='lang' type='xs:language'/><xs:anyAttribute namespace='##other'/></xs:attributeGroup>" +
            "<xs:complexType name='Applied'/>" +
            "<xs:complexType name='Suppressed' defaultAttributesApply='false'/>",
            "urn:test",
            " xmlns:t='urn:test' defaultAttributes='t:Defaults'");

        CompiledXmlSchemaSet compiled = Compile(source);
        CompiledXmlSchemaComplexType applied = FindType(compiled, "urn:test", "Applied");
        CompiledXmlSchemaComplexType suppressed = FindType(compiled, "urn:test", "Suppressed");

        Assert.Multiple(() =>
        {
            Assert.That(applied.Attributes.Select(attribute => attribute.Name.LocalName), Is.EqualTo(new[] { "lang" }));
            Assert.That(applied.AttributeWildcard!.Kind, Is.EqualTo(XmlSchemaWildcardKind.Other));
            Assert.That(suppressed.Attributes, Is.Empty);
            Assert.That(suppressed.AttributeWildcard, Is.Null);
        });
    }

    [Test]
    public void RestrictsEmptiableMixedContentToSimpleContent()
    {
        string source = Schema(
            "<xs:complexType name='Mixed' mixed='true'><xs:sequence minOccurs='0'><xs:element name='item'/></xs:sequence></xs:complexType>" +
            "<xs:complexType name='Text'><xs:simpleContent><xs:restriction base='t:Mixed'>" +
            "<xs:simpleType><xs:restriction base='xs:string'><xs:maxLength value='20'/></xs:restriction></xs:simpleType>" +
            "</xs:restriction></xs:simpleContent></xs:complexType>",
            "urn:test",
            " xmlns:t='urn:test'");

        CompiledXmlSchemaSet compiled = Compile(source);
        CompiledXmlSchemaComplexType mixed = FindType(compiled, "urn:test", "Mixed");
        CompiledXmlSchemaComplexType text = FindType(compiled, "urn:test", "Text");

        Assert.Multiple(() =>
        {
            Assert.That(text.BaseType, Is.SameAs(mixed));
            Assert.That(text.ContentKind, Is.EqualTo(XmlSchemaContentKind.Simple));
            Assert.That(text.SimpleContentType!.EffectiveFacets.Last(facet => facet.Kind == XmlSchemaFacetKind.MaxLength).Value, Is.EqualTo("20"));
        });
    }

    [Test]
    public void FiltersSchemaDerivationDefaultsForComplexTypes()
    {
        string source = Schema(
            "<xs:complexType name='Base'/>" +
            "<xs:complexType name='Restricted'><xs:complexContent><xs:restriction base='t:Base'/></xs:complexContent></xs:complexType>",
            "urn:test",
            " xmlns:t='urn:test' finalDefault='extension list' blockDefault='restriction substitution'");

        CompiledXmlSchemaSet compiled = Compile(source);
        CompiledXmlSchemaComplexType baseType = FindType(compiled, "urn:test", "Base");

        Assert.Multiple(() =>
        {
            Assert.That(baseType.Final, Is.EqualTo(XmlSchemaDerivationMethod.Extension));
            Assert.That(baseType.Block, Is.EqualTo(XmlSchemaDerivationMethod.Restriction));
            Assert.That(FindType(compiled, "urn:test", "Restricted").DerivationMethod, Is.EqualTo(XmlSchemaDerivationMethod.Restriction));
        });
    }

    [Test]
    public void NormalizesEmptyParticlesAndCombinesAllGroupExtensions()
    {
        string source = Schema(
            "<xs:complexType name='Empty'><xs:sequence/></xs:complexType>" +
            "<xs:complexType name='MixedEmpty' mixed='true'><xs:sequence/></xs:complexType>" +
            "<xs:complexType name='Base'><xs:all><xs:element name='a'/><xs:any minOccurs='0' maxOccurs='unbounded'/></xs:all></xs:complexType>" +
            "<xs:complexType name='Extended'><xs:complexContent><xs:extension base='t:Base'>" +
            "<xs:all minOccurs='0'><xs:element name='b'/></xs:all>" +
            "</xs:extension></xs:complexContent></xs:complexType>",
            "urn:test",
            " xmlns:t='urn:test'");

        CompiledXmlSchemaSet compiled = Compile(source);
        CompiledXmlSchemaComplexType empty = FindType(compiled, "urn:test", "Empty");
        CompiledXmlSchemaComplexType mixed = FindType(compiled, "urn:test", "MixedEmpty");
        CompiledXmlSchemaComplexType extended = FindType(compiled, "urn:test", "Extended");

        Assert.Multiple(() =>
        {
            Assert.That(empty.ContentKind, Is.EqualTo(XmlSchemaContentKind.Empty));
            Assert.That(empty.Particle, Is.Null);
            Assert.That(mixed.ContentKind, Is.EqualTo(XmlSchemaContentKind.Mixed));
            Assert.That(mixed.Particle!.Kind, Is.EqualTo(XmlSchemaParticleKind.Sequence));
            Assert.That(mixed.Particle.Children, Is.Empty);
            Assert.That(extended.Particle!.Kind, Is.EqualTo(XmlSchemaParticleKind.All));
            Assert.That(extended.Particle.MinOccurs, Is.EqualTo(BigInteger.Zero));
            Assert.That(extended.Particle.Children, Has.Length.EqualTo(3));
            Assert.That(extended.Particle.Children[0].Name!.LocalName, Is.EqualTo("a"));
            Assert.That(extended.Particle.Children[1].Kind, Is.EqualTo(XmlSchemaParticleKind.Any));
            Assert.That(extended.Particle.Children[1].MaxOccursUnbounded, Is.True);
            Assert.That(extended.Particle.Children[2].Name!.LocalName, Is.EqualTo("b"));
        });
    }

    [Test]
    public void ResolvesChameleonGroupAndComplexTypeQNames()
    {
        TypeResolver resolver = new TypeResolver();
        resolver.Add(
            "https://example.test/common.xsd",
            Schema(
                "<xs:group name='Items'><xs:sequence><xs:element name='item'/></xs:sequence></xs:group>" +
                "<xs:complexType name='Common'><xs:group ref='Items'/></xs:complexType>",
                null,
                ""));
        string source = Schema(
            "<xs:include schemaLocation='common.xsd'/>" +
            "<xs:complexType name='Root'><xs:complexContent><xs:extension base='t:Common'><xs:attribute name='id'/></xs:extension></xs:complexContent></xs:complexType>",
            "urn:test",
            " xmlns:t='urn:test'");

        CompiledXmlSchemaSet compiled = Compile(source, resolver);
        CompiledXmlSchemaComplexType common = FindType(compiled, "urn:test", "Common");
        CompiledXmlSchemaComplexType root = FindType(compiled, "urn:test", "Root");

        Assert.Multiple(() =>
        {
            Assert.That(common.Module!.IsChameleon, Is.True);
            Assert.That(common.Particle!.GroupDefinition!.Name.NamespaceUri, Is.EqualTo("urn:test"));
            Assert.That(root.BaseType, Is.SameAs(common));
            Assert.That(resolver.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void AppliesRedefinedGroupsAndComplexTypesAgainstPreviousDefinitions()
    {
        TypeResolver resolver = new TypeResolver();
        resolver.Add(
            "https://example.test/base.xsd",
            Schema(
                "<xs:group name='Items'><xs:sequence><xs:element name='old'/></xs:sequence></xs:group>" +
                "<xs:complexType name='Base'><xs:group ref='t:Items'/></xs:complexType>",
                "urn:test",
                " xmlns:t='urn:test'"));
        string source = Schema(
            "<xs:redefine schemaLocation='base.xsd'>" +
            "<xs:group name='Items'><xs:sequence><xs:group ref='t:Items'/><xs:element name='new'/></xs:sequence></xs:group>" +
            "<xs:complexType name='Base'><xs:complexContent><xs:extension base='t:Base'><xs:attribute name='version'/></xs:extension></xs:complexContent></xs:complexType>" +
            "</xs:redefine>",
            "urn:test",
            " xmlns:t='urn:test'");

        CompiledXmlSchemaSet compiled = Compile(source, resolver);
        CompiledXmlSchemaGroup items = FindGroup(compiled, "urn:test", "Items");
        CompiledXmlSchemaComplexType baseType = FindType(compiled, "urn:test", "Base");

        Assert.Multiple(() =>
        {
            Assert.That(items.PreviousDefinition, Is.Not.Null);
            Assert.That(items.Particle!.Children[0].GroupDefinition, Is.SameAs(items.PreviousDefinition));
            Assert.That(items.Particle.Children[1].Name!.LocalName, Is.EqualTo("new"));
            Assert.That(baseType.PreviousDefinition, Is.Not.Null);
            Assert.That(baseType.BaseType, Is.SameAs(baseType.PreviousDefinition));
            Assert.That(baseType.Attributes[0].Name.LocalName, Is.EqualTo("version"));
        });
    }

    [TestCase("<xs:complexType name='A'/><xs:complexType name='A'/>")]
    [TestCase("<xs:simpleType name='A'><xs:restriction base='xs:string'/></xs:simpleType><xs:complexType name='A'/>")]
    [TestCase("<xs:complexType name='A'><xs:simpleContent/></xs:complexType>")]
    [TestCase("<xs:complexType name='A' mixed='true'><xs:simpleContent><xs:extension base='xs:string'/></xs:simpleContent></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:simpleContent><xs:extension base='xs:string'/></xs:simpleContent><xs:sequence/></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:complexContent><xs:extension/></xs:complexContent></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:complexContent><xs:extension base='t:B'/></xs:complexContent></xs:complexType><xs:complexType name='B'><xs:complexContent><xs:extension base='t:A'/></xs:complexContent></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:complexContent><xs:extension base='xs:string'/></xs:complexContent></xs:complexType>")]
    [TestCase("<xs:complexType name='Base'><xs:sequence><xs:element name='x'/></xs:sequence></xs:complexType><xs:complexType name='A'><xs:simpleContent><xs:extension base='t:Base'/></xs:simpleContent></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:sequence minOccurs='2' maxOccurs='1'/></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:all><xs:element name='x' maxOccurs='2'/></xs:all></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:all><xs:sequence/></xs:all></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:sequence><xs:all/></xs:sequence></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:group ref='t:Missing'/></xs:complexType>")]
    [TestCase("<xs:group name='A' minOccurs='0'><xs:sequence/></xs:group>")]
    [TestCase("<xs:group name='A' ref='t:B'><xs:sequence/></xs:group>")]
    [TestCase("<xs:group name='A'><xs:sequence><xs:group ref='t:B'/></xs:sequence></xs:group><xs:group name='B'><xs:sequence><xs:group ref='t:A'/></xs:sequence></xs:group>")]
    [TestCase("<xs:complexType name='A'><xs:attribute name='x'/><xs:attribute name='x'/></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:attribute ref='t:x' type='xs:string'/></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:attribute name='x' default='a' fixed='b'/></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:attribute name='x' use='sometimes'/></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:attributeGroup ref='t:Missing'/></xs:complexType>")]
    [TestCase("<xs:attributeGroup name='A' ref='t:B'/>")]
    [TestCase("<xs:attributeGroup name='A'><xs:attributeGroup ref='t:B'/></xs:attributeGroup><xs:attributeGroup name='B'><xs:attributeGroup ref='t:A'/></xs:attributeGroup>")]
    [TestCase("<xs:complexType name='A'><xs:anyAttribute namespace='##any' notNamespace='##local'/></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:anyAttribute processContents='maybe'/></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:anyAttribute namespace='relative'/></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:anyAttribute notQName='##bad'/></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:anyAttribute notQName='##definedSibling'/></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:openContent mode='suffix'/></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:openContent><xs:any minOccurs='0'/></xs:openContent></xs:complexType>")]
    [TestCase("<xs:complexType name='A'><xs:openContent mode='none'><xs:any/></xs:openContent></xs:complexType>")]
    [TestCase("<xs:defaultOpenContent mode='none'><xs:any/></xs:defaultOpenContent><xs:complexType name='A'/>")]
    [TestCase("<xs:complexType name='A' mixed='true'><xs:complexContent mixed='false'><xs:restriction base='xs:anyType'/></xs:complexContent></xs:complexType>")]
    [TestCase("<xs:complexType name='A' block='substitution'/>")]
    [TestCase("<xs:simpleType name='Base' final='extension'><xs:restriction base='xs:string'/></xs:simpleType><xs:complexType name='A'><xs:simpleContent><xs:extension base='t:Base'/></xs:simpleContent></xs:complexType>")]
    [TestCase("<xs:complexType name='Base'><xs:simpleContent><xs:extension base='xs:decimal'/></xs:simpleContent></xs:complexType><xs:complexType name='A'><xs:simpleContent><xs:restriction base='t:Base'><xs:simpleType><xs:restriction base='xs:string'/></xs:simpleType></xs:restriction></xs:simpleContent></xs:complexType>")]
    [TestCase("<xs:complexType name='Base' final='extension'/><xs:complexType name='A'><xs:complexContent><xs:extension base='t:Base'/></xs:complexContent></xs:complexType>")]
    public void RejectsInvalidComplexTypeStructuresAndReferences(string content)
    {
        XmlSchemaException exception = Assert.Throws<XmlSchemaException>(() => Compile(Schema(content, "urn:test", " xmlns:t='urn:test'")))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SchemaFailure));
            Assert.That(exception.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Schema));
            Assert.That(exception.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
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

    private static CompiledXmlSchemaComplexType FindType(CompiledXmlSchemaSet compiled, string namespaceUri, string localName)
    {
        return compiled.ComplexTypes.Last(type => type.Name != null && type.Name.NamespaceUri == namespaceUri && type.Name.LocalName == localName);
    }

    private static CompiledXmlSchemaGroup FindGroup(CompiledXmlSchemaSet compiled, string namespaceUri, string localName)
    {
        return compiled.Groups.Last(group => group.Name.NamespaceUri == namespaceUri && group.Name.LocalName == localName);
    }

    private static CompiledXmlSchemaAttributeGroup FindAttributeGroup(CompiledXmlSchemaSet compiled, string namespaceUri, string localName)
    {
        return compiled.AttributeGroups.Last(group => group.Name.NamespaceUri == namespaceUri && group.Name.LocalName == localName);
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
