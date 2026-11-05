using System;
using System.Collections.Generic;

namespace fake;
/// <summary>Stores composition rules in deterministic insertion order.</summary>
/// <remarks>Duplicate member paths are retained and evaluated in the order in which they were added.</remarks>
public class FakeRuleSet : IFakeRuleSet
{
    List<FakeRule> RuleValues;
    bool StrictValue;
    /// <summary>Initializes an empty ordered rule set.</summary>
    public FakeRuleSet()
    {
        RuleValues = new List<FakeRule>();
        StrictValue = false;
    }

    /// <summary>Adds a constant-value rule.</summary>
    /// <param name="memberPath">The dot-separated public field or property path.</param>
    /// <param name="value">The value to assign, including null when the destination accepts it.</param>
    /// <returns>The added rule.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>memberPath</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>memberPath</c> is empty, contains whitespace, or has an empty segment.</exception>
    public FakeRule Constant(string memberPath, object? value)
    {
        return Add(ValidateMemberPath(memberPath, "memberPath"), FakeRuleKind.Constant, value, null, null);
    }

    /// <summary>Adds a generated-value rule using the active <see cref="Fake"/> instance.</summary>
    /// <param name="memberPath">The dot-separated public field or property path.</param>
    /// <param name="generate">The callback invoked when the rule is evaluated.</param>
    /// <returns>The added rule.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>memberPath</c> is empty, contains whitespace, or has an empty segment.</exception>
    public FakeRule Generate(string memberPath, Func<Fake, object?> generate)
    {
        string path = ValidateMemberPath(memberPath, "memberPath");
        if (generate == null)
            throw new ArgumentNullException("generate");
        return Add(path, FakeRuleKind.Generated, null, generate, null);
    }

    /// <summary>Adds a generated-value rule using a parameterless callback.</summary>
    /// <param name="memberPath">The dot-separated public field or property path.</param>
    /// <param name="generate">The callback invoked when the rule is evaluated.</param>
    /// <returns>The added rule.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>memberPath</c> is empty, contains whitespace, or has an empty segment.</exception>
    public FakeRule GenerateFromMemberPathAndGenerate(string memberPath, Func<object?> generate)
    {
        string path = ValidateMemberPath(memberPath, "memberPath");
        if (generate == null)
            throw new ArgumentNullException("generate");
        Func<Fake, object?> adapter = value => generate();
        return Add(path, FakeRuleKind.Generated, null, adapter, null);
    }

    /// <summary>Adds a rule that leaves a member unchanged.</summary>
    /// <param name="memberPath">The dot-separated public field or property path.</param>
    /// <returns>The added rule.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>memberPath</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>memberPath</c> is empty, contains whitespace, or has an empty segment.</exception>
    public FakeRule Skip(string memberPath)
    {
        return Add(ValidateMemberPath(memberPath, "memberPath"), FakeRuleKind.Skipped, null, null, null);
    }

    /// <summary>Adds a value rule derived from the partially populated target object.</summary>
    /// <param name="memberPath">The dot-separated public field or property path.</param>
    /// <param name="derive">The callback invoked with the target object when the rule is evaluated.</param>
    /// <returns>The added rule.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>memberPath</c> is empty, contains whitespace, or has an empty segment.</exception>
    public FakeRule Derived(string memberPath, Func<object, object?> derive)
    {
        string path = ValidateMemberPath(memberPath, "memberPath");
        if (derive == null)
            throw new ArgumentNullException("derive");
        return Add(path, FakeRuleKind.Derived, null, null, derive);
    }

    /// <summary>Adds a snapshot of another rule set beneath a member-path prefix.</summary>
    /// <param name="memberPath">The dot-separated path prepended to every nested rule.</param>
    /// <param name="rules">The nested rules to copy in their existing order.</param>
    /// <returns>The copied rules with combined paths and new insertion-order values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>memberPath</c> is empty, contains whitespace, or has an empty segment.</exception>
    public FakeRule[] Nested(string memberPath, FakeRuleSet rules)
    {
        string prefix = ValidateMemberPath(memberPath, "memberPath");
        if (rules == null)
            throw new ArgumentNullException("rules");
        FakeRule[] nested = rules.Rules();
        FakeRule[] result = new FakeRule[nested.Length];
        int index = 0;
        while (index < nested.Length)
        {
            FakeRule source = nested[index];
            result[index] = Add(prefix + "." + source.MemberPath, source.Kind, source.ConstantValue, source.Generator, source.Deriver);
            index = index + 1;
        }

        return result;
    }

    /// <summary>Returns the number of configured rules.</summary>
    /// <returns>The rule count.</returns>
    public int Count()
    {
        return RuleValues.Count;
    }

    /// <summary>Returns the configured rules in deterministic insertion order.</summary>
    /// <returns>A new array containing the ordered rules.</returns>
    public FakeRule[] Rules()
    {
        return RuleValues.ToArray();
    }

    /// <summary>Enables strict diagnostics for missing and read-only member paths.</summary>
    /// <returns>This rule set.</returns>
    public FakeRuleSet Strict()
    {
        StrictValue = true;
        return this;
    }

    /// <summary>Sets whether missing and read-only member paths produce diagnostics.</summary>
    /// <param name="enabled">True to enable strict diagnostics; false to skip inaccessible paths.</param>
    /// <returns>This rule set.</returns>
    public FakeRuleSet StrictFromEnabled(bool enabled)
    {
        StrictValue = enabled;
        return this;
    }

    /// <summary>Returns whether strict missing-member diagnostics are enabled.</summary>
    /// <returns>True when strict diagnostics are enabled.</returns>
    public bool IsStrict()
    {
        return StrictValue;
    }

    FakeRule Add(string memberPath, FakeRuleKind kind, object? constantValue, Func<Fake, object?>? generator, Func<object, object?>? deriver)
    {
        FakeRule result = new FakeRule(memberPath, kind, RuleValues.Count, constantValue, generator, deriver);
        RuleValues.Add(result);
        return result;
    }

    internal static string ValidateMemberPath(string memberPath, string parameterName)
    {
        if (memberPath == null)
            throw new ArgumentNullException(parameterName);
        if (String.IsNullOrWhiteSpace(memberPath))
            throw new ArgumentException("memberPath must contain at least one member name.", parameterName);
        int segmentLength = 0;
        int index = 0;
        while (index < memberPath.Length)
        {
            char character = memberPath[index];
            if (character == '.')
            {
                if (segmentLength == 0)
                    throw new ArgumentException("memberPath cannot contain an empty segment.", parameterName);
                segmentLength = 0;
            }
            else
            {
                if (Char.IsWhiteSpace(character) || Char.IsControl(character))
                    throw new ArgumentException("memberPath cannot contain whitespace or control characters.", parameterName);
                segmentLength = segmentLength + 1;
            }

            index = index + 1;
        }

        if (segmentLength == 0)
            throw new ArgumentException("memberPath cannot contain an empty segment.", parameterName);
        return memberPath;
    }
}
