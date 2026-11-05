namespace xml;

internal enum XsltVariableScope
{
    Global = 0,
    Local = 1,
    TemplateParameter = 2,
    FunctionParameter = 3,
    IterationParameter = 4
}

internal enum XsltVariableValueKind
{
    Select = 0,
    Content = 1,
    EmptyString = 2,
    EmptySequence = 3,
    Required = 4,
    StaticValue = 5
}

internal class XsltVariablePlan
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal XsltStaticContext StaticContext;
    internal XmlName Name;
    internal string ExpandedName;
    internal XsltVariableScope Scope;
    internal XsltVariableValueKind ValueKind;
    internal bool IsParameter;
    internal bool IsGlobal;
    internal bool IsRequired;
    internal bool IsExplicitlyRequired;
    internal bool IsTunnel;
    internal bool IsStatic;
    internal bool IsEffective;
    internal string? SequenceTypeSource;
    internal XPathTypeSyntax? SequenceType;
    internal string? Select;
    internal XPathPlan? SelectPlan;
    internal XmlNode[] Content;
    internal XPathVariableBinding Binding;
    internal XsltVariablePlan[] Dependencies;
    internal XsltComponent? Component;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltVariablePlan(XmlNode declaration, XsltStylesheetModule module, XsltStaticContext staticContext, XmlName name, string expandedName)
    {
        Declaration = declaration;
        Module = module;
        StaticContext = staticContext;
        Name = name;
        ExpandedName = expandedName;
        Scope = XsltVariableScope.Local;
        ValueKind = XsltVariableValueKind.EmptyString;
        IsParameter = false;
        IsGlobal = false;
        IsRequired = false;
        IsExplicitlyRequired = false;
        IsTunnel = false;
        IsStatic = false;
        IsEffective = true;
        SequenceTypeSource = null;
        SequenceType = null;
        Select = null;
        SelectPlan = null;
        Content = [];
        Binding = new XPathVariableBinding();
        Dependencies = [];
        Component = null;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
    }
}
