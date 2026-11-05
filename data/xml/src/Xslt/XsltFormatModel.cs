namespace xml;

internal class XsltDecimalFormatPropertyPlan
{
    internal string Name;
    internal string Value;
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltDecimalFormatPropertyPlan(string name, string value, XmlNode declaration, XsltStylesheetModule module, int importPrecedence, int declarationOrder)
    {
        Name = name;
        Value = value;
        Declaration = declaration;
        Module = module;
        ImportPrecedence = importPrecedence;
        DeclarationOrder = declarationOrder;
    }
}

internal class XsltDecimalFormatDeclarationPlan
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal XmlName? ResolvedName;
    internal string Name;
    internal XsltDecimalFormatPropertyPlan[] Properties;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltDecimalFormatDeclarationPlan(XmlNode declaration, XsltStylesheetModule module, XmlName? resolvedName, string name)
    {
        Declaration = declaration;
        Module = module;
        ResolvedName = resolvedName;
        Name = name;
        Properties = [];
        ImportPrecedence = -1;
        DeclarationOrder = -1;
    }
}

internal class XsltDecimalFormatPlan
{
    internal XmlName? ResolvedName;
    internal string Name;
    internal XsltDecimalFormatDeclarationPlan[] Declarations;
    internal XsltDecimalFormatPropertyPlan[] Properties;
    internal string DecimalSeparator;
    internal string GroupingSeparator;
    internal string Infinity;
    internal string MinusSign;
    internal string ExponentSeparator;
    internal string NaN;
    internal string Percent;
    internal string PerMille;
    internal string ZeroDigit;
    internal string Digit;
    internal string PatternSeparator;

    internal XsltDecimalFormatPlan(XmlName? resolvedName, string name)
    {
        ResolvedName = resolvedName;
        Name = name;
        Declarations = [];
        Properties = [];
        DecimalSeparator = ".";
        GroupingSeparator = ",";
        Infinity = "Infinity";
        MinusSign = "-";
        ExponentSeparator = "e";
        NaN = "NaN";
        Percent = "%";
        PerMille = "‰";
        ZeroDigit = "0";
        Digit = "#";
        PatternSeparator = ";";
    }
}

internal class XsltCharacterMapEntryPlan
{
    internal XmlNode Declaration;
    internal uint CodePoint;
    internal string Replacement;
    internal int DeclarationOrder;

    internal XsltCharacterMapEntryPlan(XmlNode declaration, uint codePoint, string replacement, int declarationOrder)
    {
        Declaration = declaration;
        CodePoint = codePoint;
        Replacement = replacement;
        DeclarationOrder = declarationOrder;
    }
}

internal class XsltCharacterMapDeclarationPlan
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal XmlName ResolvedName;
    internal string Name;
    internal string[] UsedMapNames;
    internal XsltCharacterMapEntryPlan[] Entries;
    internal bool IsEffective;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltCharacterMapDeclarationPlan(XmlNode declaration, XsltStylesheetModule module, XmlName resolvedName, string name)
    {
        Declaration = declaration;
        Module = module;
        ResolvedName = resolvedName;
        Name = name;
        UsedMapNames = [];
        Entries = [];
        IsEffective = true;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
    }
}

internal class XsltCharacterMapPlan
{
    internal XmlName ResolvedName;
    internal string Name;
    internal XsltCharacterMapDeclarationPlan[] Declarations;
    internal XsltCharacterMapDeclarationPlan EffectiveDeclaration;
    internal XsltCharacterMapPlan[] UsedMaps;
    internal XmlCharacterMapEntry[] EffectiveEntries;

    internal XsltCharacterMapPlan(XmlName resolvedName, string name, XsltCharacterMapDeclarationPlan declaration)
    {
        ResolvedName = resolvedName;
        Name = name;
        Declarations = [];
        EffectiveDeclaration = declaration;
        UsedMaps = [];
        EffectiveEntries = [];
    }
}

internal class XsltOutputAttributePlan
{
    internal string Name;
    internal string LexicalValue;
    internal string NormalizedValue;
    internal XmlName[] Names;
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltOutputAttributePlan(string name, string lexicalValue, string normalizedValue, XmlName[] names, XmlNode declaration, XsltStylesheetModule module, int importPrecedence, int declarationOrder)
    {
        Name = name;
        LexicalValue = lexicalValue;
        NormalizedValue = normalizedValue;
        Names = names;
        Declaration = declaration;
        Module = module;
        ImportPrecedence = importPrecedence;
        DeclarationOrder = declarationOrder;
    }
}

internal class XsltOutputDeclarationPlan
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal XmlName? ResolvedName;
    internal string Name;
    internal XsltOutputAttributePlan[] Attributes;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltOutputDeclarationPlan(XmlNode declaration, XsltStylesheetModule module, XmlName? resolvedName, string name)
    {
        Declaration = declaration;
        Module = module;
        ResolvedName = resolvedName;
        Name = name;
        Attributes = [];
        ImportPrecedence = -1;
        DeclarationOrder = -1;
    }
}

internal class XsltOutputFormatPlan
{
    internal XmlName? ResolvedName;
    internal string Name;
    internal XsltOutputDeclarationPlan[] Declarations;
    internal XsltOutputAttributePlan[] Attributes;
    internal XmlSerializationOptions Options;
    internal bool HasMethod;
    internal bool BuildTree;
    internal string? ParameterDocument;
    internal string[] CharacterMapNames;
    internal XsltCharacterMapPlan[] CharacterMaps;

    internal XsltOutputFormatPlan(XmlName? resolvedName, string name)
    {
        ResolvedName = resolvedName;
        Name = name;
        Declarations = [];
        Attributes = [];
        Options = new XmlSerializationOptions();
        Options.Encoding = "utf-8";
        Options.EscapeUriAttributes = true;
        Options.HtmlVersion = "5";
        Options.IncludeContentType = true;
        Options.JsonNodeOutputMethod = XmlSerializationMethod.Xml;
        Options.NormalizationForm = "none";
        Options.Standalone = XmlStandalone.Unspecified;
        Options.Version = "1.0";
        HasMethod = false;
        BuildTree = true;
        ParameterDocument = null;
        CharacterMapNames = [];
        CharacterMaps = [];
    }
}

internal enum XsltWhitespaceNameTestKind
{
    Any = 0,
    Namespace = 1,
    LocalName = 2,
    Name = 3
}

internal class XsltWhitespaceRulePlan
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal string Source;
    internal string Name;
    internal string NamespaceUri;
    internal string LocalName;
    internal XsltWhitespaceNameTestKind Kind;
    internal bool Preserve;
    internal int Priority;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltWhitespaceRulePlan(XmlNode declaration, XsltStylesheetModule module, string source)
    {
        Declaration = declaration;
        Module = module;
        Source = source;
        Name = "";
        NamespaceUri = "";
        LocalName = "";
        Kind = XsltWhitespaceNameTestKind.Any;
        Preserve = true;
        Priority = 0;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
    }
}
