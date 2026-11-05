namespace xml;

internal enum XsltContextItemUse
{
    Required = 0,
    Optional = 1,
    Absent = 2
}

internal enum XsltSequenceItemKind
{
    Text = 0,
    LiteralResultElement = 1,
    Instruction = 2,
    Variable = 3,
    Extension = 4
}

internal class XsltTemplateMode
{
    internal string LexicalName;
    internal string Name;
    internal bool IsAll;

    internal XsltTemplateMode(string lexicalName, string name, bool isAll)
    {
        LexicalName = lexicalName;
        Name = name;
        IsAll = isAll;
    }
}

internal class XsltMatchPatternPlan
{
    internal string Source;
    internal SourceRange? Range;
    internal bool IsDeferred;
    internal bool IsPredicatePattern;
    internal bool IsMotionless;
    internal bool HasRootedPath;
    internal bool HasKeyCall;
    internal XPathAstNode? Expression;
    internal XPathPlan? EquivalentExpression;
    internal XsltPatternPredicatePlan[] Predicates;

    internal XsltMatchPatternPlan(string source, SourceRange? range)
    {
        Source = source;
        Range = range;
        IsDeferred = true;
        IsPredicatePattern = false;
        IsMotionless = false;
        HasRootedPath = false;
        HasKeyCall = false;
        Expression = null;
        EquivalentExpression = null;
        Predicates = [];
    }
}

internal class XsltPatternPredicatePlan
{
    internal XPathAstNode Predicate;
    internal XPathPlan Expression;

    internal XsltPatternPredicatePlan(XPathAstNode predicate, XPathPlan expression)
    {
        Predicate = predicate;
        Expression = expression;
    }
}

internal class XsltContextItemPlan
{
    internal XmlNode Declaration;
    internal XsltContextItemUse Use;
    internal string? TypeSource;
    internal XPathTypeSyntax? Type;

    internal XsltContextItemPlan(XmlNode declaration)
    {
        Declaration = declaration;
        Use = XsltContextItemUse.Optional;
        TypeSource = null;
        Type = null;
    }
}

internal class XsltSequenceItemPlan
{
    internal XmlNode Node;
    internal XsltSequenceItemKind Kind;
    internal string NamespaceUri;
    internal string LocalName;
    internal XsltVariablePlan? Variable;
    internal XsltValueTemplate? TextTemplate;
    internal XsltValueTemplate[] AttributeTemplates;
    internal XsltInstructionPlan? Instruction;
    internal XsltSequenceConstructorPlan? Content;
    internal XsltExtensionBinding? Extension;
    internal XmlExtensionPolicy? ExtensionPolicy;

    internal XsltSequenceItemPlan(XmlNode node, XsltSequenceItemKind kind, string namespaceUri, string localName)
    {
        Node = node;
        Kind = kind;
        NamespaceUri = namespaceUri;
        LocalName = localName;
        Variable = null;
        TextTemplate = null;
        AttributeTemplates = [];
        Instruction = null;
        Content = null;
        Extension = null;
        ExtensionPolicy = null;
    }
}

internal class XsltSequenceConstructorPlan
{
    internal XmlNode Owner;
    internal XsltSequenceItemPlan[] Items;
    internal SourceRange? Range;

    internal XsltSequenceConstructorPlan(XmlNode owner, XsltSequenceItemPlan[] items)
    {
        Owner = owner;
        Items = items;
        Range = owner.Range;
    }
}

internal class XsltTemplatePlan
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal XsltStaticContext StaticContext;
    internal bool IsNamed;
    internal bool IsTemplateRule;
    internal bool IsSynthetic;
    internal string? Name;
    internal XmlName? ResolvedName;
    internal string? Match;
    internal XsltMatchPatternPlan? MatchPlan;
    internal XsltTemplateMode[] Modes;
    internal bool HasExplicitPriority;
    internal string? PrioritySource;
    internal string? Priority;
    internal string? ReturnTypeSource;
    internal XPathTypeSyntax? ReturnType;
    internal XsltComponentVisibility Visibility;
    internal XsltComponentVisibility DeclaredVisibility;
    internal bool HasDeclaredVisibility;
    internal bool IsAbstract;
    internal XsltContextItemPlan? ContextItem;
    internal XsltVariablePlan[] Parameters;
    internal XsltSequenceConstructorPlan Body;
    internal XsltComponent? Component;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltTemplatePlan(XmlNode declaration, XsltStylesheetModule module, XsltStaticContext staticContext)
    {
        Declaration = declaration;
        Module = module;
        StaticContext = staticContext;
        IsNamed = false;
        IsTemplateRule = false;
        IsSynthetic = false;
        Name = null;
        ResolvedName = null;
        Match = null;
        MatchPlan = null;
        Modes = [];
        HasExplicitPriority = false;
        PrioritySource = null;
        Priority = null;
        ReturnTypeSource = null;
        ReturnType = null;
        Visibility = XsltComponentVisibility.Private;
        DeclaredVisibility = XsltComponentVisibility.Private;
        HasDeclaredVisibility = false;
        IsAbstract = false;
        ContextItem = null;
        Parameters = [];
        Body = new XsltSequenceConstructorPlan(declaration, []);
        Component = null;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
    }
}
