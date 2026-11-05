using System;

namespace sql;

/// <summary>Represents one relational logical-plan operation without an inheritance hierarchy.</summary>
public class SqlLogicalPlanNode
{
    /// <summary>The operation represented by this node.</summary>
    public SqlLogicalPlanNodeKind Kind;
    /// <summary>The input plan nodes in evaluation order.</summary>
    public SqlLogicalPlanNode[] Inputs;
    /// <summary>The AST node from which this plan node was derived.</summary>
    public object SourceNode;
    /// <summary>The resolved or requested relation name, when applicable.</summary>
    public SqlName? RelationName;
    /// <summary>The expressions evaluated by this operation.</summary>
    public SqlExpression[] Expressions;
    /// <summary>The source span represented by this operation.</summary>
    public SqlTextSpan Span;
    /// <summary>A stable human-readable operation detail.</summary>
    public string Detail;
    /// <summary>Indicates that the node's represented semantics are complete.</summary>
    public bool IsComplete;

    /// <summary>Initializes a relational logical-plan node.</summary>
    /// <param name="kind">The operation kind.</param>
    /// <param name="inputs">The input operations.</param>
    /// <param name="sourceNode">The source AST node.</param>
    /// <param name="relationName">The optional relation name.</param>
    /// <param name="expressions">The evaluated expressions.</param>
    /// <param name="span">The source span.</param>
    /// <param name="detail">The operation detail.</param>
    /// <param name="isComplete">Whether represented semantics are complete.</param>
    /// <exception cref="ArgumentNullException">inputs, sourceNode, expressions, span, detail, or an array item is <c>null</c>.</exception>
    public SqlLogicalPlanNode(SqlLogicalPlanNodeKind kind, SqlLogicalPlanNode[] inputs, object sourceNode, SqlName? relationName, SqlExpression[] expressions, SqlTextSpan span, string detail, bool isComplete)
    {
        ValidateInputs(inputs);
        ValidateExpressions(expressions);
        SourceNode = sourceNode ?? throw new ArgumentNullException("sourceNode");
        Span = span ?? throw new ArgumentNullException("span");
        Detail = detail ?? throw new ArgumentNullException("detail");
        Kind = kind;
        Inputs = inputs;
        RelationName = relationName;
        Expressions = expressions;
        IsComplete = isComplete;
    }

    internal static void ValidateInputs(SqlLogicalPlanNode[] inputs)
    {
        if (inputs == null)
        {
            throw new ArgumentNullException("inputs");
        }

        int index = 0;
        while (index < inputs.Length)
        {
            if (inputs[index] == null)
            {
                throw new ArgumentNullException("inputs");
            }

            index = index + 1;
        }
    }

    internal static void ValidateExpressions(SqlExpression[] expressions)
    {
        if (expressions == null)
        {
            throw new ArgumentNullException("expressions");
        }

        int index = 0;
        while (index < expressions.Length)
        {
            if (expressions[index] == null)
            {
                throw new ArgumentNullException("expressions");
            }

            index = index + 1;
        }
    }
}
