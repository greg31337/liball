namespace xml;

internal enum XsltInputTypeAnnotations
{
    Unspecified = 0,
    Preserve = 1,
    Strip = 2
}

internal class XsltPackageMetadataPlan
{
    internal XmlNode Root;
    internal XsltStylesheetModule Module;
    internal string? Id;
    internal string? Name;
    internal string PackageVersion;
    internal bool IsExplicit;
    internal bool DeclaredModes;
    internal XsltInputTypeAnnotations InputTypeAnnotations;
    internal XsltInputTypeAnnotations EffectiveInputTypeAnnotations;
    internal string DefaultMode;
    internal XsltDefaultValidation DefaultValidation;
    internal string DefaultCollation;

    internal XsltPackageMetadataPlan(XmlNode root, XsltStylesheetModule module)
    {
        Root = root;
        Module = module;
        Id = null;
        Name = null;
        PackageVersion = "1";
        IsExplicit = false;
        DeclaredModes = false;
        InputTypeAnnotations = XsltInputTypeAnnotations.Unspecified;
        EffectiveInputTypeAnnotations = XsltInputTypeAnnotations.Unspecified;
        DefaultMode = "#unnamed";
        DefaultValidation = XsltDefaultValidation.Strip;
        DefaultCollation = XPathNamespaces.CodepointCollation;
    }
}

internal class XsltGlobalContextItemDeclarationPlan
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal XsltContextItemUse Use;
    internal string TypeSource;
    internal string TypeIdentity;
    internal XPathTypeSyntax Type;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltGlobalContextItemDeclarationPlan(XmlNode declaration, XsltStylesheetModule module, XPathTypeSyntax type)
    {
        Declaration = declaration;
        Module = module;
        Use = XsltContextItemUse.Optional;
        TypeSource = "item()";
        TypeIdentity = "item";
        Type = type;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
    }
}

internal class XsltGlobalContextItemPlan
{
    internal XsltGlobalContextItemDeclarationPlan[] Declarations;
    internal XsltContextItemUse Use;
    internal string TypeSource;
    internal string TypeIdentity;
    internal XPathTypeSyntax? Type;
    internal bool IsDeclared;

    internal XsltGlobalContextItemPlan()
    {
        Declarations = [];
        Use = XsltContextItemUse.Optional;
        TypeSource = "item()";
        TypeIdentity = "item";
        Type = null;
        IsDeclared = false;
    }
}

internal class XsltSchemaImportPlan
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal bool HasNamespace;
    internal string NamespaceUri;
    internal string? SchemaLocation;
    internal XmlNode? InlineSchema;
    internal XmlSchemaSet? CompiledSchema;
    internal bool IsEffective;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltSchemaImportPlan(XmlNode declaration, XsltStylesheetModule module)
    {
        Declaration = declaration;
        Module = module;
        HasNamespace = false;
        NamespaceUri = "";
        SchemaLocation = null;
        InlineSchema = null;
        CompiledSchema = null;
        IsEffective = true;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
    }
}
