namespace sql;

/// <summary>Specifies the form of an explain statement.</summary>
public enum SqlExplainKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents ordinary <c>EXPLAIN</c>.</summary>
    Explain = 1,
    /// <summary>Represents <c>EXPLAIN QUERY PLAN</c>.</summary>
    QueryPlan = 2
}
