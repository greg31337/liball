namespace xml;

internal enum XmlSchemaIdentityConstraintKind
{
    Key = 0,
    Unique = 1,
    KeyReference = 2
}

internal class CompiledXmlSchemaTypeAlternative
{
    internal string? Test;
    internal CompiledXmlSchemaSimpleType? SimpleType;
    internal CompiledXmlSchemaComplexType? ComplexType;
    internal XmlNode Node;

    internal CompiledXmlSchemaTypeAlternative(XmlNode node)
    {
        Test = null;
        SimpleType = null;
        ComplexType = null;
        Node = node;
    }
}

internal class CompiledXmlSchemaIdentityConstraint
{
    internal XmlName Name;
    internal XmlSchemaIdentityConstraintKind Kind;
    internal string Selector;
    internal string[] Fields;
    internal string? XPathDefaultNamespace;
    internal XmlName? ReferName;
    internal CompiledXmlSchemaIdentityConstraint? ReferencedConstraint;
    internal XmlSchemaIdentityXPathExpression? CompiledSelector;
    internal XmlSchemaIdentityXPathExpression[] CompiledFields;
    internal CompiledXmlSchemaElementDeclaration? OwnerElement;
    internal XmlNode Node;
    internal XmlSchemaModule Module;

    internal CompiledXmlSchemaIdentityConstraint(XmlName name, XmlSchemaIdentityConstraintKind kind, XmlNode node, XmlSchemaModule module)
    {
        Name = name;
        Kind = kind;
        Selector = "";
        Fields = [];
        XPathDefaultNamespace = null;
        ReferName = null;
        ReferencedConstraint = null;
        CompiledSelector = null;
        CompiledFields = [];
        OwnerElement = null;
        Node = node;
        Module = module;
    }
}

internal class CompiledXmlSchemaElementDeclaration
{
    internal XmlName Name;
    internal CompiledXmlSchemaSimpleType? SimpleType;
    internal CompiledXmlSchemaComplexType? ComplexType;
    internal bool IsGlobal;
    internal bool IsAbstract;
    internal bool IsNillable;
    internal bool IsQualified;
    internal string? DefaultValue;
    internal string? FixedValue;
    internal XmlSchemaDerivationMethod Block;
    internal XmlSchemaDerivationMethod Final;
    internal XmlName? SubstitutionGroupName;
    internal CompiledXmlSchemaElementDeclaration? SubstitutionGroupHead;
    internal CompiledXmlSchemaElementDeclaration[] SubstitutionGroupMembers;
    internal CompiledXmlSchemaTypeAlternative[] TypeAlternatives;
    internal CompiledXmlSchemaIdentityConstraint[] IdentityConstraints;
    internal XmlNode Node;
    internal XmlSchemaModule Module;
    internal CompiledXmlSchemaElementDeclaration? PreviousDefinition;
    internal bool IsCompiled;
    internal bool IsCompiling;

    internal CompiledXmlSchemaElementDeclaration(XmlName name, bool global, XmlNode node, XmlSchemaModule module)
    {
        Name = name;
        SimpleType = null;
        ComplexType = null;
        IsGlobal = global;
        IsAbstract = false;
        IsNillable = false;
        IsQualified = global;
        DefaultValue = null;
        FixedValue = null;
        Block = XmlSchemaDerivationMethod.None;
        Final = XmlSchemaDerivationMethod.None;
        SubstitutionGroupName = null;
        SubstitutionGroupHead = null;
        SubstitutionGroupMembers = [];
        TypeAlternatives = [];
        IdentityConstraints = [];
        Node = node;
        Module = module;
        PreviousDefinition = null;
        IsCompiled = false;
        IsCompiling = false;
    }
}

internal class CompiledXmlSchemaAttributeDeclaration
{
    internal XmlName Name;
    internal CompiledXmlSchemaSimpleType? Type;
    internal bool IsGlobal;
    internal bool IsQualified;
    internal bool Inheritable;
    internal string? DefaultValue;
    internal string? FixedValue;
    internal XmlNode Node;
    internal XmlSchemaModule Module;
    internal CompiledXmlSchemaAttributeDeclaration? PreviousDefinition;
    internal bool IsCompiled;
    internal bool IsCompiling;

    internal CompiledXmlSchemaAttributeDeclaration(XmlName name, bool global, XmlNode node, XmlSchemaModule module)
    {
        Name = name;
        Type = null;
        IsGlobal = global;
        IsQualified = global;
        Inheritable = false;
        DefaultValue = null;
        FixedValue = null;
        Node = node;
        Module = module;
        PreviousDefinition = null;
        IsCompiled = false;
        IsCompiling = false;
    }
}

internal class CompiledXmlSchemaNotation
{
    internal XmlName Name;
    internal string? PublicIdentifier;
    internal string? SystemIdentifier;
    internal XmlNode Node;
    internal XmlSchemaModule Module;
    internal CompiledXmlSchemaNotation? PreviousDefinition;

    internal CompiledXmlSchemaNotation(XmlName name, XmlNode node, XmlSchemaModule module)
    {
        Name = name;
        PublicIdentifier = null;
        SystemIdentifier = null;
        Node = node;
        Module = module;
        PreviousDefinition = null;
    }
}
