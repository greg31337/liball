namespace xml;

internal enum XsltKeyValueKind
{
    Expression = 0,
    Content = 1
}

internal class XsltKeyDeclarationPlan
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal XsltStaticContext StaticContext;
    internal XmlName ResolvedName;
    internal string Name;
    internal string MatchSource;
    internal XsltMatchPatternPlan Match;
    internal XsltKeyValueKind ValueKind;
    internal string? UseSource;
    internal XPathPlan? Use;
    internal XPathStaticType? UseType;
    internal XsltSequenceConstructorPlan? Content;
    internal XsltVariablePlan[] Dependencies;
    internal bool Composite;
    internal string Collation;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltKeyDeclarationPlan(XmlNode declaration, XsltStylesheetModule module, XsltStaticContext staticContext, XmlName resolvedName, string name, string matchSource)
    {
        Declaration = declaration;
        Module = module;
        StaticContext = staticContext;
        ResolvedName = resolvedName;
        Name = name;
        MatchSource = matchSource;
        Match = new XsltMatchPatternPlan(matchSource, declaration.Range);
        ValueKind = XsltKeyValueKind.Expression;
        UseSource = null;
        Use = null;
        UseType = null;
        Content = null;
        Dependencies = [];
        Composite = false;
        Collation = staticContext.DefaultCollation;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
    }
}

internal class XsltKeyPlan
{
    internal XmlName ResolvedName;
    internal string Name;
    internal XsltKeyDeclarationPlan[] Declarations;
    internal bool Composite;
    internal string Collation;

    internal XsltKeyPlan(XmlName resolvedName, string name)
    {
        ResolvedName = resolvedName;
        Name = name;
        Declarations = [];
        Composite = false;
        Collation = XPathNamespaces.CodepointCollation;
    }
}
