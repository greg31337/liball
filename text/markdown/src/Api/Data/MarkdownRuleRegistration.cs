using System;

namespace markdown;

/// <summary>
/// Associates a stable name with a caller-supplied extension object.
/// </summary>
public class MarkdownRuleRegistration
{
    /// <summary>
    /// Stores the name value used by this instance.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the rule value used by this instance.
    /// </summary>
    public object Rule;

    /// <summary>
    /// Initializes a new <see cref="MarkdownRuleRegistration"/> instance.
    /// </summary>
    public MarkdownRuleRegistration()
    {
        Name = "";
        Rule = new object();
    }

    /// <summary>
    /// Creates a named rule registration.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <param name="rule">The caller-supplied rule object.</param>
    internal static MarkdownRuleRegistration Create(string name, object rule)
    {
        ValidateName(name);

        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        MarkdownRuleRegistration registration = new MarkdownRuleRegistration();
        registration.Name = name;
        registration.Rule = rule;
        return registration;
    }

    /// <summary>
    /// Creates a detached copy of the value.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownRuleRegistration"/> value.</returns>
    internal MarkdownRuleRegistration Copy()
    {
        return Create(Name, Rule);
    }

    internal static void ValidateName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Rule registration name must not be empty.");
        }
    }
}
