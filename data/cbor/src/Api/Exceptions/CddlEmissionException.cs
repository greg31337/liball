namespace cbor;

/// <summary>Reports an exceptional failure while emitting a compiled CDDL schema.</summary>
public class CddlEmissionException : System.Exception
{
    /// <summary>The stable schema-emission failure code.</summary>
    public string ErrorCode;
    /// <summary>The schema source range associated with the failed callback.</summary>
    public CddlSourceSpan? Span;
    /// <summary>The containing rule name, or null outside a rule callback.</summary>
    public string? RuleName;
    /// <summary>The stable rule index, or negative one outside a rule callback.</summary>
    public int RuleIndex;
    /// <summary>The stable node index, or negative one outside a node callback.</summary>
    public int NodeIndex;

    /// <summary>Creates a schema-emission exception with callback context.</summary>
    /// <param name="errorCode">The stable emission failure code.</param>
    /// <param name="message">The explanatory failure message.</param>
    /// <param name="span">The schema source range, or null when unavailable.</param>
    /// <param name="ruleName">The containing rule name, or null when unavailable.</param>
    /// <param name="ruleIndex">The stable rule index, or negative one.</param>
    /// <param name="nodeIndex">The stable node index, or negative one.</param>
    public CddlEmissionException(
        string errorCode,
        string message,
        CddlSourceSpan? span,
        string? ruleName,
        int ruleIndex,
        int nodeIndex) : base(message)
    {
        ErrorCode = errorCode;
        Span = span;
        RuleName = ruleName;
        RuleIndex = ruleIndex;
        NodeIndex = nodeIndex;
    }
}
