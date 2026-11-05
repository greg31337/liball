namespace xml;

internal enum XsltOnNoMatchBehavior
{
    DeepCopy = 0,
    ShallowCopy = 1,
    DeepSkip = 2,
    ShallowSkip = 3,
    TextOnlyCopy = 4,
    Fail = 5
}

internal enum XsltOnMultipleMatchBehavior
{
    UseLast = 0,
    Fail = 1
}

internal class XsltModeReferencePlan
{
    internal string Name;
    internal XmlNode Node;
    internal XsltStylesheetModule Module;

    internal XsltModeReferencePlan(string name, XmlNode node, XsltStylesheetModule module)
    {
        Name = name;
        Node = node;
        Module = module;
    }
}

internal class XsltModeDeclarationPlan
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal XsltStaticContext StaticContext;
    internal string Name;
    internal int ImportPrecedence;
    internal int DeclarationOrder;
    internal bool HasStreamable;
    internal bool Streamable;
    internal bool HasUseAccumulators;
    internal bool UseAllAccumulators;
    internal string[] AccumulatorNames;
    internal bool HasOnNoMatch;
    internal XsltOnNoMatchBehavior OnNoMatch;
    internal bool HasOnMultipleMatch;
    internal XsltOnMultipleMatchBehavior OnMultipleMatch;
    internal bool HasWarningOnNoMatch;
    internal bool WarningOnNoMatch;
    internal bool HasWarningOnMultipleMatch;
    internal bool WarningOnMultipleMatch;
    internal bool HasTyped;
    internal XsltTypedMode Typed;
    internal bool HasVisibility;
    internal XsltComponentVisibility Visibility;

    internal XsltModeDeclarationPlan(XmlNode declaration, XsltStylesheetModule module, XsltStaticContext staticContext, string name)
    {
        Declaration = declaration;
        Module = module;
        StaticContext = staticContext;
        Name = name;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
        HasStreamable = false;
        Streamable = false;
        HasUseAccumulators = false;
        UseAllAccumulators = false;
        AccumulatorNames = [];
        HasOnNoMatch = false;
        OnNoMatch = XsltOnNoMatchBehavior.TextOnlyCopy;
        HasOnMultipleMatch = false;
        OnMultipleMatch = XsltOnMultipleMatchBehavior.UseLast;
        HasWarningOnNoMatch = false;
        WarningOnNoMatch = false;
        HasWarningOnMultipleMatch = false;
        WarningOnMultipleMatch = false;
        HasTyped = false;
        Typed = XsltTypedMode.Unspecified;
        HasVisibility = false;
        Visibility = XsltComponentVisibility.Private;
    }
}

internal class XsltModePlan
{
    internal string Name;
    internal bool IsUnnamed;
    internal bool IsImplicit;
    internal XmlNode Origin;
    internal XsltStylesheetModule Module;
    internal XsltModeDeclarationPlan[] Declarations;
    internal bool Streamable;
    internal bool UseAllAccumulators;
    internal string[] AccumulatorNames;
    internal XsltAccumulatorPlan[] Accumulators;
    internal XsltOnNoMatchBehavior OnNoMatch;
    internal XsltOnMultipleMatchBehavior OnMultipleMatch;
    internal bool WarningOnNoMatch;
    internal bool WarningOnMultipleMatch;
    internal XsltTypedMode Typed;
    internal bool HasDeclaredVisibility;
    internal XsltComponentVisibility DeclaredVisibility;
    internal int ImportPrecedence;
    internal int DeclarationOrder;
    internal XsltComponent? Component;

    internal XsltModePlan(string name, XmlNode origin, XsltStylesheetModule module)
    {
        Name = name;
        IsUnnamed = name == "#unnamed";
        IsImplicit = false;
        Origin = origin;
        Module = module;
        Declarations = [];
        Streamable = false;
        UseAllAccumulators = false;
        AccumulatorNames = [];
        Accumulators = [];
        OnNoMatch = XsltOnNoMatchBehavior.TextOnlyCopy;
        OnMultipleMatch = XsltOnMultipleMatchBehavior.UseLast;
        WarningOnNoMatch = false;
        WarningOnMultipleMatch = false;
        Typed = XsltTypedMode.Unspecified;
        HasDeclaredVisibility = false;
        DeclaredVisibility = XsltComponentVisibility.Private;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
        Component = null;
    }
}
