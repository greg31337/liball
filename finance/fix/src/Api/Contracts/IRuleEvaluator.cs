namespace fix;

/// <summary>Evaluates dictionary conditional-presence expressions.</summary>
public interface IRuleEvaluator
{
    /// <summary>Evaluates one member rule against a message and external state.</summary>
    /// <param name="member">The member carrying the rule expression.</param>
    /// <param name="message">The message being validated.</param>
    /// <param name="context">The session and external validation state.</param>
    /// <returns>The explicit evaluation outcome.</returns>
    RuleEvaluationResult EvaluateRule(FixMemberDefinition member, FixMessage message, ValidationContext context);
}
