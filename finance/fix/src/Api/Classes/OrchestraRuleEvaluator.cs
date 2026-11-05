namespace fix;

/// <summary>Evaluates FIX Orchestra Score rules against dictionary-defined messages and external state.</summary>
public class OrchestraRuleEvaluator : IRuleEvaluator
{
    internal FixDictionary Dictionary;

    /// <summary>Creates a rule evaluator for one canonical FIX dictionary.</summary>
    /// <param name="dictionary">The dictionary used to resolve field, group, and code-set names.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when the dictionary is null.</exception>
    public OrchestraRuleEvaluator(FixDictionary dictionary)
    {
        if (dictionary == null)
        {
            throw new System.ArgumentNullException("dictionary");
        }
        Dictionary = dictionary;
    }

    /// <summary>Evaluates one member rule against a message and external state.</summary>
    /// <param name="member">The member carrying the rule expression.</param>
    /// <param name="message">The message being validated.</param>
    /// <param name="context">The session and external validation state.</param>
    /// <returns>The explicit evaluation outcome.</returns>
    public RuleEvaluationResult EvaluateRule(FixMemberDefinition member, FixMessage message, ValidationContext context)
    {
        if (member == null)
        {
            throw new System.ArgumentNullException("member");
        }
        if (message == null)
        {
            throw new System.ArgumentNullException("message");
        }
        if (context == null)
        {
            throw new System.ArgumentNullException("context");
        }
        return ScoreEvaluator.EvaluateScoreRule(member.PresenceRule, member.Presence, Dictionary, message, context);
    }
}
