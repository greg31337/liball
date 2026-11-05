namespace xml;

internal enum XsltAccumulatorPhase
{
    Start = 0,
    End = 1
}

internal enum XsltAccumulatorValueKind
{
    Expression = 0,
    Content = 1
}

internal class XsltAccumulatorReferencePlan
{
    internal string Name;
    internal XsltAccumulatorPhase ValuePhase;
    internal XsltAccumulatorPlan? Target;

    internal XsltAccumulatorReferencePlan(string name, XsltAccumulatorPhase valuePhase)
    {
        Name = name;
        ValuePhase = valuePhase;
        Target = null;
    }
}

internal class XsltAccumulatorRulePlan
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal XsltStaticContext StaticContext;
    internal string MatchSource;
    internal XsltMatchPatternPlan Match;
    internal XsltAccumulatorPhase Phase;
    internal XsltAccumulatorValueKind ValueKind;
    internal string? SelectSource;
    internal XPathPlan? Select;
    internal XPathStaticType? SelectType;
    internal XsltSequenceConstructorPlan? Content;
    internal XsltVariablePlan[] Dependencies;
    internal XsltAccumulatorReferencePlan[] AccumulatorDependencies;
    internal bool ValueIsGrounded;
    internal bool ValueIsMotionless;
    internal int RuleOrder;

    internal XsltAccumulatorRulePlan(XmlNode declaration, XsltStylesheetModule module, XsltStaticContext staticContext, string matchSource)
    {
        Declaration = declaration;
        Module = module;
        StaticContext = staticContext;
        MatchSource = matchSource;
        Match = new XsltMatchPatternPlan(matchSource, declaration.Range);
        Phase = XsltAccumulatorPhase.Start;
        ValueKind = XsltAccumulatorValueKind.Content;
        SelectSource = null;
        Select = null;
        SelectType = null;
        Content = null;
        Dependencies = [];
        AccumulatorDependencies = [];
        ValueIsGrounded = false;
        ValueIsMotionless = false;
        RuleOrder = -1;
    }
}

internal class XsltAccumulatorDeclarationPlan
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal XsltStaticContext StaticContext;
    internal XmlName ResolvedName;
    internal string Name;
    internal string InitialValueSource;
    internal XPathPlan InitialValue;
    internal XPathStaticType InitialValueType;
    internal string? SequenceTypeSource;
    internal XPathTypeSyntax SequenceType;
    internal bool Streamable;
    internal XsltAccumulatorRulePlan[] Rules;
    internal XsltVariablePlan[] Dependencies;
    internal XsltAccumulatorReferencePlan[] AccumulatorDependencies;
    internal bool InitialValueIsGrounded;
    internal bool InitialValueIsMotionless;
    internal bool IsGuaranteedStreamableCandidate;
    internal bool IsEffective;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltAccumulatorDeclarationPlan(XmlNode declaration, XsltStylesheetModule module, XsltStaticContext staticContext, XmlName resolvedName, string name, string initialValueSource, XPathPlan initialValue, XPathTypeSyntax sequenceType)
    {
        Declaration = declaration;
        Module = module;
        StaticContext = staticContext;
        ResolvedName = resolvedName;
        Name = name;
        InitialValueSource = initialValueSource;
        InitialValue = initialValue;
        InitialValueType = initialValue.Root.StaticType;
        SequenceTypeSource = null;
        SequenceType = sequenceType;
        Streamable = false;
        Rules = [];
        Dependencies = [];
        AccumulatorDependencies = [];
        InitialValueIsGrounded = false;
        InitialValueIsMotionless = false;
        IsGuaranteedStreamableCandidate = false;
        IsEffective = true;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
    }
}

internal class XsltAccumulatorPlan
{
    internal XmlName ResolvedName;
    internal string Name;
    internal XsltAccumulatorDeclarationPlan[] Declarations;
    internal XsltAccumulatorDeclarationPlan EffectiveDeclaration;
    internal XsltAccumulatorPlan[] Dependencies;

    internal XsltAccumulatorPlan(XmlName resolvedName, string name, XsltAccumulatorDeclarationPlan declaration)
    {
        ResolvedName = resolvedName;
        Name = name;
        Declarations = [];
        EffectiveDeclaration = declaration;
        Dependencies = [];
    }
}
