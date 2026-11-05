using System;

namespace sql;

/// <summary>Stores the typed components of a postfix collation expression.</summary>
public class SqlCollateExpressionData
{
    /// <summary>The expression receiving the collation.</summary>
    public SqlExpression Operand;
    /// <summary>The applied collation name.</summary>
    public SqlName Collation;

    /// <summary>Initializes typed collation expression data.</summary>
    /// <param name="operand">The expression receiving the collation.</param>
    /// <param name="collation">The applied collation name.</param>
    public SqlCollateExpressionData(SqlExpression operand, SqlName collation)
    {
        Operand = operand ?? throw new ArgumentNullException("operand");
        Collation = collation ?? throw new ArgumentNullException("collation");
    }
}

/// <summary>Stores the typed components of a pattern predicate and optional escape clause.</summary>
public class SqlPatternExpressionData
{
    /// <summary>The value tested by the predicate.</summary>
    public SqlExpression Value;
    /// <summary>The pattern expression.</summary>
    public SqlExpression Pattern;
    /// <summary>The optional escape expression.</summary>
    public SqlExpression? Escape;
    /// <summary>The pattern operator spelling.</summary>
    public string OperatorText;
    /// <summary>Indicates whether the predicate is negated.</summary>
    public bool IsNegated;

    /// <summary>Initializes typed pattern expression data.</summary>
    /// <param name="value">The value tested by the predicate.</param>
    /// <param name="pattern">The pattern expression.</param>
    /// <param name="escape">The optional escape expression.</param>
    /// <param name="operatorText">The pattern operator spelling.</param>
    /// <param name="isNegated">Whether the predicate is negated.</param>
    public SqlPatternExpressionData(SqlExpression value, SqlExpression pattern, SqlExpression? escape, string operatorText, bool isNegated)
    {
        Value = value ?? throw new ArgumentNullException("value");
        Pattern = pattern ?? throw new ArgumentNullException("pattern");
        OperatorText = operatorText ?? throw new ArgumentNullException("operatorText");
        Escape = escape;
        IsNegated = isNegated;
    }
}

/// <summary>Stores the typed components of a distinctness predicate.</summary>
public class SqlDistinctnessExpressionData
{
    /// <summary>The left operand.</summary>
    public SqlExpression Left;
    /// <summary>The right operand.</summary>
    public SqlExpression Right;
    /// <summary>The distinctness predicate kind.</summary>
    public SqlDistinctnessKind Kind;

    /// <summary>Initializes typed distinctness predicate data.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="kind">The predicate kind.</param>
    public SqlDistinctnessExpressionData(SqlExpression left, SqlExpression right, SqlDistinctnessKind kind)
    {
        Left = left ?? throw new ArgumentNullException("left");
        Right = right ?? throw new ArgumentNullException("right");
        Kind = kind;
    }
}

/// <summary>Stores every supported right-hand form of an <c>IN</c> predicate.</summary>
public class SqlInExpressionData
{
    /// <summary>The tested expression.</summary>
    public SqlExpression Operand;
    /// <summary>Indicates whether the predicate is negated.</summary>
    public bool IsNegated;
    /// <summary>The right-hand target form.</summary>
    public SqlInTargetKind TargetKind;
    /// <summary>The expression-list values.</summary>
    public SqlExpression[] Values;
    /// <summary>The subquery target.</summary>
    public SqlQuery? Query;
    /// <summary>The table or function name.</summary>
    public SqlName? TargetName;
    /// <summary>The table-function arguments.</summary>
    public SqlExpression[] FunctionArguments;

    /// <summary>Initializes typed <c>IN</c> predicate data.</summary>
    /// <param name="operand">The tested expression.</param>
    /// <param name="isNegated">Whether the predicate is negated.</param>
    /// <param name="targetKind">The right-hand target form.</param>
    /// <param name="values">The expression-list values.</param>
    /// <param name="query">The subquery target.</param>
    /// <param name="targetName">The table or function name.</param>
    /// <param name="functionArguments">The table-function arguments.</param>
    public SqlInExpressionData(SqlExpression operand, bool isNegated, SqlInTargetKind targetKind, SqlExpression[] values, SqlQuery? query, SqlName? targetName, SqlExpression[] functionArguments)
    {
        Operand = operand ?? throw new ArgumentNullException("operand");
        Values = values ?? throw new ArgumentNullException("values");
        FunctionArguments = functionArguments ?? throw new ArgumentNullException("functionArguments");
        IsNegated = isNegated;
        TargetKind = targetKind;
        Query = query;
        TargetName = targetName;
    }
}

/// <summary>Stores the typed components of an extract-like expression.</summary>
public class SqlExtractExpressionData
{
    /// <summary>The requested source field spelling.</summary>
    public string Field;
    /// <summary>The expression from which the field is extracted.</summary>
    public SqlExpression Source;

    /// <summary>Initializes typed extract expression data.</summary>
    /// <param name="field">The requested source field spelling.</param>
    /// <param name="source">The source expression.</param>
    public SqlExtractExpressionData(string field, SqlExpression source)
    {
        Field = field ?? throw new ArgumentNullException("field");
        Source = source ?? throw new ArgumentNullException("source");
    }
}

/// <summary>Stores the typed components and spelling of a cast expression.</summary>
public class SqlCastExpressionData
{
    /// <summary>The expression being cast.</summary>
    public SqlExpression Operand;
    /// <summary>The original target type spelling.</summary>
    public string TypeName;
    /// <summary>The cast syntax form.</summary>
    public SqlCastSyntaxKind SyntaxKind;

    /// <summary>Initializes typed cast expression data.</summary>
    /// <param name="operand">The expression being cast.</param>
    /// <param name="typeName">The original target type spelling.</param>
    /// <param name="syntaxKind">The cast syntax form.</param>
    public SqlCastExpressionData(SqlExpression operand, string typeName, SqlCastSyntaxKind syntaxKind)
    {
        Operand = operand ?? throw new ArgumentNullException("operand");
        TypeName = typeName ?? throw new ArgumentNullException("typeName");
        SyntaxKind = syntaxKind;
    }
}

/// <summary>Stores array-constructor elements or a subquery source.</summary>
public class SqlArrayExpressionData
{
    /// <summary>The explicitly listed array elements.</summary>
    public SqlExpression[] Elements;
    /// <summary>The optional query supplying array elements.</summary>
    public SqlQuery? Query;

    /// <summary>Initializes typed array constructor data.</summary>
    /// <param name="elements">The explicitly listed elements.</param>
    /// <param name="query">The optional query source.</param>
    public SqlArrayExpressionData(SqlExpression[] elements, SqlQuery? query)
    {
        Elements = elements ?? throw new ArgumentNullException("elements");
        Query = query;
    }
}

/// <summary>Stores an index or slice applied to an expression.</summary>
public class SqlSubscriptExpressionData
{
    /// <summary>The expression being subscripted.</summary>
    public SqlExpression Target;
    /// <summary>The index or lower slice bound.</summary>
    public SqlExpression? LowerBound;
    /// <summary>The optional upper slice bound.</summary>
    public SqlExpression? UpperBound;
    /// <summary>Indicates whether colon slice syntax was used.</summary>
    public bool IsSlice;

    /// <summary>Initializes typed subscript expression data.</summary>
    /// <param name="target">The expression being subscripted.</param>
    /// <param name="lowerBound">The index or lower slice bound.</param>
    /// <param name="upperBound">The optional upper slice bound.</param>
    /// <param name="isSlice">Whether colon slice syntax was used.</param>
    public SqlSubscriptExpressionData(SqlExpression target, SqlExpression? lowerBound, SqlExpression? upperBound, bool isSlice)
    {
        Target = target ?? throw new ArgumentNullException("target");
        LowerBound = lowerBound;
        UpperBound = upperBound;
        IsSlice = isSlice;
    }
}

/// <summary>Stores operands and exact spelling for JSON or caller-defined operators.</summary>
public class SqlOperatorExpressionData
{
    /// <summary>The left operand.</summary>
    public SqlExpression Left;
    /// <summary>The right operand.</summary>
    public SqlExpression Right;
    /// <summary>The exact operator spelling.</summary>
    public string OperatorText;
    /// <summary>The operator precedence used while parsing.</summary>
    public int Precedence;
    /// <summary>The non-standard operator category.</summary>
    public SqlOperatorExpressionKind Kind;

    /// <summary>Initializes typed operator expression data.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="operatorText">The exact operator spelling.</param>
    /// <param name="precedence">The parsing precedence.</param>
    /// <param name="kind">The non-standard operator category.</param>
    public SqlOperatorExpressionData(SqlExpression left, SqlExpression right, string operatorText, int precedence, SqlOperatorExpressionKind kind)
    {
        Left = left ?? throw new ArgumentNullException("left");
        Right = right ?? throw new ArgumentNullException("right");
        OperatorText = operatorText ?? throw new ArgumentNullException("operatorText");
        if (precedence < 1)
        {
            throw new ArgumentOutOfRangeException("precedence");
        }

        Precedence = precedence;
        Kind = kind;
    }
}

/// <summary>Stores the optional qualifier of a wildcard expression.</summary>
public class SqlWildcardExpressionData
{
    /// <summary>The wildcard qualifier, or <c>null</c> for an unqualified wildcard.</summary>
    public SqlName? Qualifier;

    /// <summary>Initializes typed wildcard expression data.</summary>
    /// <param name="qualifier">The optional qualifier.</param>
    public SqlWildcardExpressionData(SqlName? qualifier)
    {
        Qualifier = qualifier;
    }
}

/// <summary>Stores function, aggregate, filter, and window invocation components.</summary>
public class SqlFunctionInvocationData
{
    /// <summary>The invoked function name.</summary>
    public SqlName Name;
    /// <summary>The function arguments.</summary>
    public SqlExpression[] Arguments;
    /// <summary>Indicates whether <c>DISTINCT</c> modifies the arguments.</summary>
    public bool IsDistinct;
    /// <summary>The aggregate-local ordering terms.</summary>
    public SqlOrderingTerm[] OrderBy;
    /// <summary>The aggregate filter predicate.</summary>
    public SqlExpression? Filter;
    /// <summary>The referenced named window.</summary>
    public SqlName? WindowName;
    /// <summary>The inline window definition.</summary>
    public SqlWindowDefinition? WindowDefinition;

    /// <summary>Initializes typed function invocation data.</summary>
    /// <param name="name">The function name.</param>
    /// <param name="arguments">The function arguments.</param>
    /// <param name="isDistinct">Whether <c>DISTINCT</c> is present.</param>
    /// <param name="orderBy">The aggregate-local ordering terms.</param>
    /// <param name="filter">The aggregate filter predicate.</param>
    /// <param name="windowName">The referenced named window.</param>
    /// <param name="windowDefinition">The inline window definition.</param>
    public SqlFunctionInvocationData(SqlName name, SqlExpression[] arguments, bool isDistinct, SqlOrderingTerm[] orderBy, SqlExpression? filter, SqlName? windowName, SqlWindowDefinition? windowDefinition)
    {
        Name = name ?? throw new ArgumentNullException("name");
        Arguments = arguments ?? throw new ArgumentNullException("arguments");
        OrderBy = orderBy ?? throw new ArgumentNullException("orderBy");
        IsDistinct = isDistinct;
        Filter = filter;
        WindowName = windowName;
        WindowDefinition = windowDefinition;
    }
}
