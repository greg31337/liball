namespace xml;

internal enum XmlSchemaDirectiveKind
{
    Root = 0,
    Include = 1,
    Import = 2,
    Redefine = 3,
    Override = 4
}

internal class CompiledXmlSchemaSet
{
    internal XmlSchemaModule[] Modules;
    internal CompiledXmlSchemaSimpleType[] SimpleTypes;
    internal CompiledXmlSchemaComplexType[] ComplexTypes;
    internal CompiledXmlSchemaGroup[] Groups;
    internal CompiledXmlSchemaAttributeGroup[] AttributeGroups;
    internal CompiledXmlSchemaElementDeclaration[] Elements;
    internal CompiledXmlSchemaAttributeDeclaration[] Attributes;
    internal CompiledXmlSchemaIdentityConstraint[] IdentityConstraints;
    internal CompiledXmlSchemaNotation[] Notations;
    internal bool IsImmutable;

    internal CompiledXmlSchemaSet(XmlSchemaModule[] modules, CompiledXmlSchemaSimpleType[] simpleTypes, CompiledXmlSchemaComplexType[] complexTypes, CompiledXmlSchemaGroup[] groups, CompiledXmlSchemaAttributeGroup[] attributeGroups, CompiledXmlSchemaElementDeclaration[] elements, CompiledXmlSchemaAttributeDeclaration[] attributes, CompiledXmlSchemaIdentityConstraint[] identityConstraints, CompiledXmlSchemaNotation[] notations)
    {
        Modules = modules;
        SimpleTypes = simpleTypes;
        ComplexTypes = complexTypes;
        Groups = groups;
        AttributeGroups = attributeGroups;
        Elements = elements;
        Attributes = attributes;
        IdentityConstraints = identityConstraints;
        Notations = notations;
        IsImmutable = true;
    }
}

internal class XmlSchemaModule
{
    internal XmlDocument Document;
    internal string? Location;
    internal string? DeclaredTargetNamespace;
    internal string? EffectiveTargetNamespace;
    internal XmlSchemaDirectiveKind Relation;
    internal XmlSchemaDirective[] Directives;
    internal bool IsChameleon;
    internal bool IsComplete;

    internal XmlSchemaModule(XmlDocument document, string? location, string? declaredTargetNamespace, string? effectiveTargetNamespace, XmlSchemaDirectiveKind relation, bool isChameleon)
    {
        Document = document;
        Location = location;
        DeclaredTargetNamespace = declaredTargetNamespace;
        EffectiveTargetNamespace = effectiveTargetNamespace;
        Relation = relation;
        Directives = [];
        IsChameleon = isChameleon;
        IsComplete = false;
    }
}

internal class XmlSchemaDirective
{
    internal XmlSchemaDirectiveKind Kind;
    internal XmlNode Node;
    internal string? SchemaLocation;
    internal string? NamespaceUri;
    internal XmlSchemaModule? TargetModule;

    internal XmlSchemaDirective(XmlSchemaDirectiveKind kind, XmlNode node, string? schemaLocation, string? namespaceUri)
    {
        Kind = kind;
        Node = node;
        SchemaLocation = schemaLocation;
        NamespaceUri = namespaceUri;
        TargetModule = null;
    }
}
