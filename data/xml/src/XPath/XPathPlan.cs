namespace xml;

internal enum XPathPlanOpcode
{
    Sequence = 0,
    For = 1,
    Let = 2,
    Quantified = 3,
    If = 4,
    Or = 5,
    And = 6,
    GeneralComparison = 7,
    ValueComparison = 8,
    NodeComparison = 9,
    StringConcatenation = 10,
    Range = 11,
    Addition = 12,
    Subtraction = 13,
    Multiplication = 14,
    Division = 15,
    IntegerDivision = 16,
    Modulo = 17,
    Union = 18,
    Intersect = 19,
    Except = 20,
    InstanceOf = 21,
    TreatAs = 22,
    CastableAs = 23,
    CastAs = 24,
    Arrow = 25,
    UnaryPlus = 26,
    UnaryMinus = 27,
    SimpleMap = 28,
    Path = 29,
    IntegerLiteral = 30,
    DecimalLiteral = 31,
    DoubleLiteral = 32,
    StringLiteral = 33,
    Name = 34,
    Variable = 35,
    ContextItem = 36,
    ParentStep = 37,
    AttributeStep = 38,
    Parenthesized = 39,
    StaticFunctionCall = 40,
    NamedFunctionReference = 41,
    InlineFunction = 42,
    DynamicFunctionCall = 43,
    Predicate = 44,
    Lookup = 45,
    MapConstructor = 46,
    SquareArrayConstructor = 47,
    CurlyArrayConstructor = 48,
    ArgumentPlaceholder = 49,
    UnaryLookup = 50,
    Wildcard = 51,
    AxisStep = 52,
    Constant = 53
}

internal enum XPathPlanLiteralKind
{
    Integer = 0,
    Decimal = 1,
    Double = 2,
    String = 3,
    Boolean = 4
}

internal class XPathPlanLiteral
{
    internal XPathPlanLiteralKind Kind;
    internal string Value;

    internal XPathPlanLiteral(XPathPlanLiteralKind kind, string value)
    {
        Kind = kind;
        Value = value;
    }
}

internal class XPathPlanBinding
{
    internal string Name;
    internal string LocalName;
    internal string? NamespaceUri;
    internal XPathPlanInstruction Expression;
    internal SourceRange Range;

    internal XPathPlanBinding(string name, string localName, string? namespaceUri, XPathPlanInstruction expression, SourceRange range)
    {
        Name = name;
        LocalName = localName;
        NamespaceUri = namespaceUri;
        Expression = expression;
        Range = range;
    }
}

internal class XPathPlanParameter
{
    internal string Name;
    internal string LocalName;
    internal string? NamespaceUri;
    internal XPathTypeSyntax? Type;
    internal SourceRange Range;

    internal XPathPlanParameter(string name, string localName, string? namespaceUri, XPathTypeSyntax? type, SourceRange range)
    {
        Name = name;
        LocalName = localName;
        NamespaceUri = namespaceUri;
        Type = type;
        Range = range;
    }
}

internal class XPathPlanMapEntry
{
    internal XPathPlanInstruction Key;
    internal XPathPlanInstruction Value;
    internal SourceRange Range;

    internal XPathPlanMapEntry(XPathPlanInstruction key, XPathPlanInstruction value, SourceRange range)
    {
        Key = key;
        Value = value;
        Range = range;
    }
}

internal class XPathPlanInstruction
{
    internal XPathPlanOpcode Opcode;
    internal SourceRange Range;
    internal XPathStaticType StaticType;
    internal XPathPlanLiteral[] Constants;
    internal string? Operator;
    internal string? Value;
    internal string? Prefix;
    internal string? LocalName;
    internal string? NamespaceUri;
    internal XPathPlanInstruction? Left;
    internal XPathPlanInstruction? Right;
    internal XPathPlanInstruction? Operand;
    internal XPathPlanInstruction? Condition;
    internal XPathPlanInstruction? ThenInstruction;
    internal XPathPlanInstruction? ElseInstruction;
    internal XPathPlanInstruction? Body;
    internal XPathPlanInstruction[] Children;
    internal XPathPlanBinding[] Bindings;
    internal XPathPlanParameter[] Parameters;
    internal XPathPlanMapEntry[] MapEntries;
    internal XPathTypeSyntax? Type;
    internal string[] PathOperators;
    internal bool Absolute;
    internal bool LeadingDescendant;
    internal int Arity;
    internal XPathAxis Axis;
    internal XPathNodeTest? NodeTest;
    internal bool ReverseAxis;
    internal XmlName? ResolvedName;
    internal XPathVariableBinding? VariableBinding;
    internal XPathFunctionBinding? FunctionBinding;
    internal bool TailCall;

    internal XPathPlanInstruction(XPathPlanOpcode opcode, SourceRange range, XPathStaticType staticType)
    {
        Opcode = opcode;
        Range = range;
        StaticType = staticType;
        Constants = [];
        Operator = null;
        Value = null;
        Prefix = null;
        LocalName = null;
        NamespaceUri = null;
        Left = null;
        Right = null;
        Operand = null;
        Condition = null;
        ThenInstruction = null;
        ElseInstruction = null;
        Body = null;
        Children = [];
        Bindings = [];
        Parameters = [];
        MapEntries = [];
        Type = null;
        PathOperators = [];
        Absolute = false;
        LeadingDescendant = false;
        Arity = 0;
        Axis = XPathAxis.Child;
        NodeTest = null;
        ReverseAxis = false;
        ResolvedName = null;
        VariableBinding = null;
        FunctionBinding = null;
        TailCall = false;
    }
}

internal class XPathPlan
{
    internal string? Source;
    internal SourceRange Range;
    internal XPathPlanInstruction Root;
    internal XPathStaticEnvironment Environment;

    internal XPathPlan(string? source, SourceRange range, XPathPlanInstruction root, XPathStaticEnvironment environment)
    {
        Source = source;
        Range = range;
        Root = root;
        Environment = environment;
    }
}
