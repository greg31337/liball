namespace xml;

internal enum XPathAstKind
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
    AxisStep = 52
}

internal enum XPathAxis
{
    Child = 0,
    Descendant = 1,
    Attribute = 2,
    Self = 3,
    DescendantOrSelf = 4,
    FollowingSibling = 5,
    Following = 6,
    Namespace = 7,
    Parent = 8,
    Ancestor = 9,
    PrecedingSibling = 10,
    Preceding = 11,
    AncestorOrSelf = 12
}

internal enum XPathNodeTestKind
{
    Name = 0,
    Wildcard = 1,
    NamespaceWildcard = 2,
    LocalNameWildcard = 3,
    UriWildcard = 4,
    AnyKind = 5,
    Document = 6,
    Text = 7,
    Comment = 8,
    NamespaceNode = 9,
    ProcessingInstruction = 10,
    Attribute = 11,
    SchemaAttribute = 12,
    Element = 13,
    SchemaElement = 14
}

internal class XPathTypeSyntax
{
    internal string Name;
    internal string? Prefix;
    internal string? LocalName;
    internal string? NamespaceUri;
    internal string? Occurrence;
    internal bool EmptySequence;
    internal SourceRange Range;
    internal XmlName? ResolvedName;
    internal XdmAtomicTypeDefinition? AtomicType;
    internal bool SchemaType;
    internal bool ParenthesizedTest;
    internal bool WildcardTest;
    internal bool ParenthesizedItemType;
    internal XPathNodeTest? NodeTest;
    internal XPathTypeSyntax[] ParameterTypes;
    internal XPathTypeSyntax? ResultType;
    internal XPathTypeSyntax? KeyType;
    internal XPathTypeSyntax? ValueType;
    internal XPathTypeSyntax? MemberType;
    internal XPathTypeSyntax? InnerType;

    internal XPathTypeSyntax(string name, string? prefix, string? localName, string? namespaceUri, string? occurrence, bool emptySequence, SourceRange range)
    {
        Name = name;
        Prefix = prefix;
        LocalName = localName;
        NamespaceUri = namespaceUri;
        Occurrence = occurrence;
        EmptySequence = emptySequence;
        Range = range;
        ResolvedName = null;
        AtomicType = null;
        SchemaType = false;
        ParenthesizedTest = false;
        WildcardTest = false;
        ParenthesizedItemType = false;
        NodeTest = null;
        ParameterTypes = [];
        ResultType = null;
        KeyType = null;
        ValueType = null;
        MemberType = null;
        InnerType = null;
    }
}

internal class XPathBinding
{
    internal string Name;
    internal string? Prefix;
    internal string? LocalName;
    internal string? NamespaceUri;
    internal XPathAstNode Expression;
    internal SourceRange Range;
    internal bool SingletonValue;

    internal XPathBinding(string name, string? prefix, string? localName, string? namespaceUri, XPathAstNode expression, SourceRange range)
    {
        Name = name;
        Prefix = prefix;
        LocalName = localName;
        NamespaceUri = namespaceUri;
        Expression = expression;
        Range = range;
        SingletonValue = false;
    }
}

internal class XPathParameter
{
    internal string Name;
    internal string? Prefix;
    internal string? LocalName;
    internal string? NamespaceUri;
    internal XPathTypeSyntax? Type;
    internal SourceRange Range;

    internal XPathParameter(string name, string? prefix, string? localName, string? namespaceUri, XPathTypeSyntax? type, SourceRange range)
    {
        Name = name;
        Prefix = prefix;
        LocalName = localName;
        NamespaceUri = namespaceUri;
        Type = type;
        Range = range;
    }
}

internal class XPathMapEntry
{
    internal XPathAstNode Key;
    internal XPathAstNode Value;
    internal SourceRange Range;

    internal XPathMapEntry(XPathAstNode key, XPathAstNode value, SourceRange range)
    {
        Key = key;
        Value = value;
        Range = range;
    }
}

internal class XPathNodeTest
{
    internal XPathNodeTestKind Kind;
    internal SourceRange Range;
    internal XPathToken? Token;
    internal string? Value;
    internal string? Prefix;
    internal string? LocalName;
    internal string? NamespaceUri;
    internal XPathTypeSyntax? Type;
    internal XPathNodeTest? InnerTest;
    internal bool Nillable;
    internal XmlName? ResolvedName;
    internal XmlName? ResolvedTypeName;

    internal XPathNodeTest(XPathNodeTestKind kind, SourceRange range)
    {
        Kind = kind;
        Range = range;
        Token = null;
        Value = null;
        Prefix = null;
        LocalName = null;
        NamespaceUri = null;
        Type = null;
        InnerTest = null;
        Nillable = false;
        ResolvedName = null;
        ResolvedTypeName = null;
    }
}

internal class XPathAstNode
{
    internal XPathAstKind Kind;
    internal SourceRange Range;
    internal XPathToken? Token;
    internal string? Operator;
    internal string? Value;
    internal string? Prefix;
    internal string? LocalName;
    internal string? NamespaceUri;
    internal XPathAstNode? Left;
    internal XPathAstNode? Right;
    internal XPathAstNode? Operand;
    internal XPathAstNode? Condition;
    internal XPathAstNode? ThenExpression;
    internal XPathAstNode? ElseExpression;
    internal XPathAstNode? Body;
    internal XPathAstNode[] Children;
    internal XPathBinding[] Bindings;
    internal XPathParameter[] Parameters;
    internal XPathMapEntry[] MapEntries;
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
    internal XPathBinding? LocalBinding;
    internal XPathParameter? ParameterBinding;
    internal XPathStaticType? StaticType;

    internal XPathAstNode(XPathAstKind kind, SourceRange range)
    {
        Kind = kind;
        Range = range;
        Token = null;
        Operator = null;
        Value = null;
        Prefix = null;
        LocalName = null;
        NamespaceUri = null;
        Left = null;
        Right = null;
        Operand = null;
        Condition = null;
        ThenExpression = null;
        ElseExpression = null;
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
        LocalBinding = null;
        ParameterBinding = null;
        StaticType = null;
    }
}
