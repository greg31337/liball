using System.Collections.Generic;

namespace xml;

internal enum XsltComponentKind
{
    Template = 0,
    Function = 1,
    AttributeSet = 2,
    Variable = 3,
    Mode = 4
}

internal class XsltPackageCompilationContext
{
    internal List<string> ActivePackageIdentities;
    internal Dictionary<string, XsltExecutable> Executables;

    internal XsltPackageCompilationContext()
    {
        ActivePackageIdentities = new List<string>();
        Executables = new Dictionary<string, XsltExecutable>();
    }
}

internal class XsltPackagePlan
{
    internal string? Name;
    internal string Version;
    internal bool IsExplicit;
    internal bool IsLibrary;
    internal bool IsExecutable;
    internal bool IsReusable;
    internal string? Id;
    internal bool DeclaredModes;
    internal XsltInputTypeAnnotations InputTypeAnnotations;
    internal XsltInputTypeAnnotations EffectiveInputTypeAnnotations;
    internal string DefaultMode;
    internal XsltDefaultValidation DefaultValidation;
    internal string DefaultCollation;
    internal XsltComponent[] LocalComponents;
    internal XsltComponent[] Components;
    internal XsltPackageUse[] Uses;

    internal XsltPackagePlan()
    {
        Name = null;
        Version = "1";
        IsExplicit = false;
        IsLibrary = false;
        IsExecutable = true;
        IsReusable = false;
        Id = null;
        DeclaredModes = false;
        InputTypeAnnotations = XsltInputTypeAnnotations.Unspecified;
        EffectiveInputTypeAnnotations = XsltInputTypeAnnotations.Unspecified;
        DefaultMode = "#unnamed";
        DefaultValidation = XsltDefaultValidation.Strip;
        DefaultCollation = XPathNamespaces.CodepointCollation;
        LocalComponents = [];
        Components = [];
        Uses = [];
    }
}

internal class XsltPackageUse
{
    internal XmlNode Node;
    internal string Name;
    internal string VersionRange;
    internal XsltPackagePlan Target;
    internal XsltComponent[] AcceptedComponents;
    internal XsltExecutable? SeparateExecutable;

    internal XsltPackageUse(XmlNode node, string name, string versionRange, XsltPackagePlan target)
    {
        Node = node;
        Name = name;
        VersionRange = versionRange;
        Target = target;
        AcceptedComponents = [];
        SeparateExecutable = null;
    }
}

internal class XsltComponent
{
    internal XsltComponentKind Kind;
    internal string Name;
    internal int Arity;
    internal string SymbolicIdentifier;
    internal XmlNode Declaration;
    internal XsltPackagePlan DeclaringPackage;
    internal XsltPackagePlan ContainingPackage;
    internal XsltComponentVisibility Visibility;
    internal XsltComponentVisibility DeclaredVisibility;
    internal int ImportPrecedence;
    internal int DeclarationOrder;
    internal bool HasDeclaredVisibility;
    internal bool IsParameter;
    internal bool IsStatic;
    internal bool IsOverride;
    internal XmlNode? UseDeclaration;
    internal XsltComponent? CorrespondingComponent;
    internal XsltComponent? OriginalComponent;
    internal XsltFunctionPlan? Function;
    internal XsltModePlan? Mode;
    internal XsltAttributeSetPlan? AttributeSet;
    internal XsltTemplatePlan? Template;
    internal XsltVariablePlan? Variable;
    internal XsltSymbolicReference[] References;

    internal XsltComponent(XsltComponentKind kind, string name, int arity, string symbolicIdentifier, XmlNode declaration, XsltPackagePlan declaringPackage, XsltPackagePlan containingPackage)
    {
        Kind = kind;
        Name = name;
        Arity = arity;
        SymbolicIdentifier = symbolicIdentifier;
        Declaration = declaration;
        DeclaringPackage = declaringPackage;
        ContainingPackage = containingPackage;
        Visibility = XsltComponentVisibility.Private;
        DeclaredVisibility = XsltComponentVisibility.Private;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
        HasDeclaredVisibility = false;
        IsParameter = false;
        IsStatic = false;
        IsOverride = false;
        UseDeclaration = null;
        CorrespondingComponent = null;
        OriginalComponent = null;
        Function = null;
        Mode = null;
        AttributeSet = null;
        Template = null;
        Variable = null;
        References = [];
    }
}

internal class XsltSymbolicReference
{
    internal XsltComponentKind Kind;
    internal string Name;
    internal int Arity;
    internal XmlNode Node;
    internal XsltComponent? Target;

    internal XsltSymbolicReference(XsltComponentKind kind, string name, int arity, XmlNode node)
    {
        Kind = kind;
        Name = name;
        Arity = arity;
        Node = node;
        Target = null;
    }
}

internal class XsltVisibilityRule
{
    internal XmlNode Node;
    internal XsltComponentKind Kind;
    internal bool AllKinds;
    internal string Selector;
    internal XsltComponentVisibility Visibility;
    internal int Rank;
    internal bool IsWildcard;
    internal bool Matched;

    internal XsltVisibilityRule(XmlNode node)
    {
        Node = node;
        Kind = XsltComponentKind.Template;
        AllKinds = false;
        Selector = "";
        Visibility = XsltComponentVisibility.Private;
        Rank = 0;
        IsWildcard = false;
        Matched = false;
    }
}
