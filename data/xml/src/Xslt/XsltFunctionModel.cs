namespace xml;

internal enum XsltFunctionDeterminism
{
    Proactive = 0,
    Deterministic = 1,
    Elidable = 2
}

internal class XsltFunctionExpressionPlan
{
    internal XmlNode Node;
    internal string Name;
    internal string Source;
    internal XPathPlan Plan;

    internal XsltFunctionExpressionPlan(XmlNode node, string name, string source, XPathPlan plan)
    {
        Node = node;
        Name = name;
        Source = source;
        Plan = plan;
    }
}

internal class XsltFunctionReferencePlan
{
    internal XmlName Name;
    internal string ExpandedName;
    internal int Arity;
    internal XmlNode Node;
    internal SourceRange Range;
    internal XPathFunctionBinding Binding;
    internal XsltComponent? TargetComponent;
    internal XsltFunctionPlan? TargetFunction;
    internal bool IsRecursive;
    internal bool IsOriginal;

    internal XsltFunctionReferencePlan(XmlName name, string expandedName, int arity, XmlNode node, SourceRange range, XPathFunctionBinding binding)
    {
        Name = name;
        ExpandedName = expandedName;
        Arity = arity;
        Node = node;
        Range = range;
        Binding = binding;
        TargetComponent = null;
        TargetFunction = null;
        IsRecursive = false;
        IsOriginal = false;
    }
}

internal class XsltFunctionPlan
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal XsltStaticContext StaticContext;
    internal XmlName Name;
    internal string ExpandedName;
    internal int Arity;
    internal XsltVariablePlan[] Parameters;
    internal XPathTypeSyntax?[] ParameterTypes;
    internal string? ReturnTypeSource;
    internal XPathTypeSyntax? ReturnType;
    internal bool Cache;
    internal XsltFunctionDeterminism Determinism;
    internal bool OverrideExtensionFunction;
    internal bool HasOverrideExtensionFunction;
    internal bool IsOverriddenByExtension;
    internal string Streamability;
    internal XsltComponentVisibility Visibility;
    internal XsltComponentVisibility DeclaredVisibility;
    internal bool HasDeclaredVisibility;
    internal bool IsAbstract;
    internal bool IsOverride;
    internal bool IsEffective;
    internal XsltSequenceConstructorPlan Body;
    internal XsltFunctionExpressionPlan[] Expressions;
    internal XsltFunctionReferencePlan[] References;
    internal XPathFunctionBinding Binding;
    internal XsltComponent? Component;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltFunctionPlan(XmlNode declaration, XsltStylesheetModule module, XsltStaticContext staticContext, XmlName name, string expandedName)
    {
        Declaration = declaration;
        Module = module;
        StaticContext = staticContext;
        Name = name;
        ExpandedName = expandedName;
        Arity = 0;
        Parameters = [];
        ParameterTypes = [];
        ReturnTypeSource = null;
        ReturnType = null;
        Cache = false;
        Determinism = XsltFunctionDeterminism.Proactive;
        OverrideExtensionFunction = true;
        HasOverrideExtensionFunction = false;
        IsOverriddenByExtension = false;
        Streamability = "unclassified";
        Visibility = XsltComponentVisibility.Private;
        DeclaredVisibility = XsltComponentVisibility.Private;
        HasDeclaredVisibility = false;
        IsAbstract = false;
        IsOverride = false;
        IsEffective = true;
        Body = new XsltSequenceConstructorPlan(declaration, []);
        Expressions = [];
        References = [];
        Binding = new XPathFunctionBinding();
        Component = null;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
    }
}
