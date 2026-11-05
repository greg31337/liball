using System.Numerics;

namespace xml;

internal enum XmlSchemaContentKind
{
    Empty = 0,
    Simple = 1,
    ElementOnly = 2,
    Mixed = 3
}

internal enum XmlSchemaParticleKind
{
    Empty = 0,
    Element = 1,
    Any = 2,
    Sequence = 3,
    Choice = 4,
    All = 5,
    GroupReference = 6
}

internal enum XmlSchemaAttributeUseKind
{
    Optional = 0,
    Required = 1,
    Prohibited = 2
}

internal enum XmlSchemaWildcardKind
{
    Any = 0,
    Other = 1,
    Enumeration = 2,
    Exclusion = 3
}

internal enum XmlSchemaWildcardCombination
{
    None = 0,
    Union = 1,
    Intersection = 2
}

internal enum XmlSchemaOpenContentMode
{
    None = 0,
    Interleave = 1,
    Suffix = 2
}

internal class CompiledXmlSchemaParticle
{
    internal XmlSchemaParticleKind Kind;
    internal BigInteger MinOccurs;
    internal BigInteger MaxOccurs;
    internal bool MaxOccursUnbounded;
    internal CompiledXmlSchemaParticle[] Children;
    internal XmlName? Name;
    internal bool IsReference;
    internal CompiledXmlSchemaElementDeclaration? ElementDeclaration;
    internal CompiledXmlSchemaGroup? GroupDefinition;
    internal CompiledXmlSchemaWildcard? Wildcard;
    internal XmlNode? Node;

    internal CompiledXmlSchemaParticle(XmlSchemaParticleKind kind, XmlNode? node)
    {
        Kind = kind;
        MinOccurs = BigInteger.One;
        MaxOccurs = BigInteger.One;
        MaxOccursUnbounded = false;
        Children = [];
        Name = null;
        IsReference = false;
        ElementDeclaration = null;
        GroupDefinition = null;
        Wildcard = null;
        Node = node;
    }
}

internal class CompiledXmlSchemaWildcard
{
    internal XmlSchemaWildcardKind Kind;
    internal string[] Namespaces;
    internal string[] ExcludedNamespaces;
    internal XmlName[] ExcludedNames;
    internal CompiledXmlSchemaWildcard[] Operands;
    internal XmlSchemaWildcardCombination Combination;
    internal bool ExcludesDefinedNames;
    internal bool ExcludesDefinedSiblingNames;
    internal XmlSchemaProcessContents ProcessContents;
    internal XmlNode? Node;

    internal CompiledXmlSchemaWildcard(XmlNode? node)
    {
        Kind = XmlSchemaWildcardKind.Any;
        Namespaces = [];
        ExcludedNamespaces = [];
        ExcludedNames = [];
        Operands = [];
        Combination = XmlSchemaWildcardCombination.None;
        ExcludesDefinedNames = false;
        ExcludesDefinedSiblingNames = false;
        ProcessContents = XmlSchemaProcessContents.Strict;
        Node = node;
    }
}

internal class CompiledXmlSchemaAttributeUse
{
    internal XmlName Name;
    internal bool IsReference;
    internal CompiledXmlSchemaAttributeDeclaration? Declaration;
    internal CompiledXmlSchemaSimpleType? Type;
    internal XmlSchemaAttributeUseKind Use;
    internal string? DefaultValue;
    internal string? FixedValue;
    internal XmlNode Node;

    internal CompiledXmlSchemaAttributeUse(XmlName name, XmlNode node)
    {
        Name = name;
        IsReference = false;
        Declaration = null;
        Type = null;
        Use = XmlSchemaAttributeUseKind.Optional;
        DefaultValue = null;
        FixedValue = null;
        Node = node;
    }
}

internal class CompiledXmlSchemaAttributeSet
{
    internal CompiledXmlSchemaAttributeUse[] Attributes;
    internal CompiledXmlSchemaWildcard? Wildcard;

    internal CompiledXmlSchemaAttributeSet()
    {
        Attributes = [];
        Wildcard = null;
    }
}

internal class CompiledXmlSchemaOpenContent
{
    internal XmlSchemaOpenContentMode Mode;
    internal CompiledXmlSchemaWildcard? Wildcard;
    internal bool IsDefault;
    internal XmlNode? Node;

    internal CompiledXmlSchemaOpenContent(XmlNode? node)
    {
        Mode = XmlSchemaOpenContentMode.Interleave;
        Wildcard = null;
        IsDefault = false;
        Node = node;
    }
}

internal class CompiledXmlSchemaGroup
{
    internal XmlName Name;
    internal CompiledXmlSchemaParticle? Particle;
    internal XmlNode Node;
    internal XmlSchemaModule Module;
    internal CompiledXmlSchemaGroup? PreviousDefinition;
    internal bool IsCompiled;
    internal bool IsCompiling;

    internal CompiledXmlSchemaGroup(XmlName name, XmlNode node, XmlSchemaModule module)
    {
        Name = name;
        Particle = null;
        Node = node;
        Module = module;
        PreviousDefinition = null;
        IsCompiled = false;
        IsCompiling = false;
    }
}

internal class CompiledXmlSchemaAttributeGroup
{
    internal XmlName Name;
    internal CompiledXmlSchemaAttributeUse[] Attributes;
    internal CompiledXmlSchemaWildcard? Wildcard;
    internal XmlNode Node;
    internal XmlSchemaModule Module;
    internal CompiledXmlSchemaAttributeGroup? PreviousDefinition;
    internal bool IsCompiled;
    internal bool IsCompiling;

    internal CompiledXmlSchemaAttributeGroup(XmlName name, XmlNode node, XmlSchemaModule module)
    {
        Name = name;
        Attributes = [];
        Wildcard = null;
        Node = node;
        Module = module;
        PreviousDefinition = null;
        IsCompiled = false;
        IsCompiling = false;
    }
}

internal class CompiledXmlSchemaComplexType
{
    internal XmlName? Name;
    internal CompiledXmlSchemaComplexType? BaseType;
    internal CompiledXmlSchemaSimpleType? BaseSimpleType;
    internal CompiledXmlSchemaSimpleType? SimpleContentType;
    internal XmlSchemaContentKind ContentKind;
    internal CompiledXmlSchemaParticle? Particle;
    internal CompiledXmlSchemaAttributeUse[] Attributes;
    internal CompiledXmlSchemaWildcard? AttributeWildcard;
    internal CompiledXmlSchemaOpenContent? OpenContent;
    internal string[] Assertions;
    internal XmlSchemaDerivationMethod DerivationMethod;
    internal XmlSchemaDerivationMethod Final;
    internal XmlSchemaDerivationMethod Block;
    internal bool Mixed;
    internal bool Abstract;
    internal bool IsBuiltIn;
    internal bool IsAnonymous;
    internal bool IsCompiled;
    internal bool IsCompiling;
    internal XmlNode? Node;
    internal XmlSchemaModule? Module;
    internal CompiledXmlSchemaComplexType? PreviousDefinition;

    internal CompiledXmlSchemaComplexType(XmlName? name, bool builtIn, bool anonymous, XmlNode? node, XmlSchemaModule? module)
    {
        Name = name;
        BaseType = null;
        BaseSimpleType = null;
        SimpleContentType = null;
        ContentKind = XmlSchemaContentKind.Empty;
        Particle = null;
        Attributes = [];
        AttributeWildcard = null;
        OpenContent = null;
        Assertions = [];
        DerivationMethod = XmlSchemaDerivationMethod.None;
        Final = XmlSchemaDerivationMethod.None;
        Block = XmlSchemaDerivationMethod.None;
        Mixed = false;
        Abstract = false;
        IsBuiltIn = builtIn;
        IsAnonymous = anonymous;
        IsCompiled = builtIn;
        IsCompiling = false;
        Node = node;
        Module = module;
        PreviousDefinition = null;
    }
}
