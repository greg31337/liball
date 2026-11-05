namespace xml;

internal enum XsltInstructionKind
{
    Unknown = 0,
    AnalyzeString = 1,
    ApplyImports = 2,
    ApplyTemplates = 3,
    Array = 4,
    ArrayMember = 5,
    Assert = 6,
    Attribute = 7,
    Break = 8,
    CallTemplate = 9,
    Catch = 10,
    Choose = 11,
    Comment = 12,
    ContextItem = 13,
    Copy = 14,
    CopyOf = 15,
    Document = 16,
    Element = 17,
    Evaluate = 18,
    Fallback = 19,
    ForEach = 20,
    ForEachGroup = 21,
    Fork = 22,
    If = 23,
    Iterate = 24,
    Map = 25,
    MapEntry = 26,
    MatchingSubstring = 27,
    Merge = 28,
    MergeAction = 29,
    MergeKey = 30,
    MergeSource = 31,
    Message = 32,
    Namespace = 33,
    NextIteration = 34,
    NextMatch = 35,
    NonMatchingSubstring = 36,
    Number = 37,
    OnCompletion = 38,
    OnEmpty = 39,
    OnNonEmpty = 40,
    Otherwise = 41,
    Parameter = 42,
    PerformSort = 43,
    ProcessingInstruction = 44,
    ResultDocument = 45,
    Sequence = 46,
    Sort = 47,
    SourceDocument = 48,
    Text = 49,
    Try = 50,
    ValueOf = 51,
    Variable = 52,
    When = 53,
    WherePopulated = 54,
    WithParam = 55
}

internal class XsltInstructionAttributePlan
{
    internal XmlNode Attribute;
    internal string Name;
    internal string Source;
    internal XPathPlan? Expression;
    internal XPathTypeSyntax? SequenceType;
    internal XsltMatchPatternPlan? Pattern;
    internal XmlName? ResolvedName;

    internal XsltInstructionAttributePlan(XmlNode attribute, string name, string source)
    {
        Attribute = attribute;
        Name = name;
        Source = source;
        Expression = null;
        SequenceType = null;
        Pattern = null;
        ResolvedName = null;
    }
}

internal class XsltInstructionPlan
{
    internal XmlNode Instruction;
    internal XsltStylesheetModule Module;
    internal XsltStaticContext StaticContext;
    internal XsltInstructionKind Kind;
    internal string LocalName;
    internal bool IsHelper;
    internal bool IsForwardCompatible;
    internal XsltInstructionAttributePlan[] Attributes;
    internal XmlNode[] ContentNodes;

    internal XsltInstructionPlan(XmlNode instruction, XsltStylesheetModule module, XsltStaticContext staticContext, XsltInstructionKind kind, string localName)
    {
        Instruction = instruction;
        Module = module;
        StaticContext = staticContext;
        Kind = kind;
        LocalName = localName;
        IsHelper = false;
        IsForwardCompatible = false;
        Attributes = [];
        ContentNodes = [];
    }
}
