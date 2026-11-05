using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Stores one logical SMARTS expression node, its primitive value, children, and optional recursive query.
/// </summary>
public class QueryExpression
{
    /// <summary>
    /// The logical or comparison operation represented by this node.
    /// </summary>
    public QueryOperation Operation;
    /// <summary>
    /// The atom or bond primitive represented by a leaf node.
    /// </summary>
    public QueryPrimitive Primitive;
    /// <summary>
    /// The integer value used by a primitive leaf.
    /// </summary>
    public int Value;
    /// <summary>
    /// Child expressions in source evaluation order.
    /// </summary>
    public List<QueryExpression> Children;
    /// <summary>
    /// The anchored query used by a recursive SMARTS expression.
    /// </summary>
    public QueryMolecule RecursiveQuery;
    /// <summary>
    /// The source text represented by this node.
    /// </summary>
    public string RawText;

    /// <summary>
    /// Initializes an empty expression node.
    /// </summary>
    public QueryExpression()
    {
        Operation = QueryOperation.None;
        Primitive = QueryPrimitive.None;
        Value = 0;
        Children = new List<QueryExpression>();
        RecursiveQuery = new QueryMolecule();
        RawText = "";
    }
}
