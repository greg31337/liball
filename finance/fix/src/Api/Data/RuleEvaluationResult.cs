using System.Collections.Generic;

namespace fix;

/// <summary>Contains the explicit outcome of evaluating one conditional or assignment rule.</summary>
public class RuleEvaluationResult
{
    /// <summary>Whether all inputs required by the rule were available.</summary>
    public bool WasEvaluated;
    /// <summary>Whether the evaluated condition matched.</summary>
    public bool ConditionMatched;
    /// <summary>The presence selected by a presence rule.</summary>
    public FixPresence Presence;
    /// <summary>The lexical value produced by an assignment rule.</summary>
    public string AssignedValue = "";
    /// <summary>The stable reason evaluation could not complete.</summary>
    public string ErrorCode = "";
    /// <summary>A human-readable evaluation description.</summary>
    public string ErrorMessage = "";
    /// <summary>Validation issues produced while evaluating the rule.</summary>
    public List<ValidationIssue> Issues = [];
}
