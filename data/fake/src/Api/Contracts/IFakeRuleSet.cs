using System;
using System.Collections.Generic;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="FakeRuleSet"/>.</summary>
public interface IFakeRuleSet
{
    /// <summary>Adds a constant-value rule.</summary>
    /// <param name="memberPath">The dot-separated public field or property path.</param>
    /// <param name="value">The value to assign, including null when the destination accepts it.</param>
    /// <returns>The added rule.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>memberPath</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>memberPath</c> is empty, contains whitespace, or has an empty segment.</exception>
    FakeRule Constant(string memberPath, object? value);
    /// <summary>Adds a generated-value rule using the active <see cref="Fake"/> instance.</summary>
    /// <param name="memberPath">The dot-separated public field or property path.</param>
    /// <param name="generate">The callback invoked when the rule is evaluated.</param>
    /// <returns>The added rule.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>memberPath</c> is empty, contains whitespace, or has an empty segment.</exception>
    FakeRule Generate(string memberPath, Func<Fake, object?> generate);
    /// <summary>Adds a generated-value rule using a parameterless callback.</summary>
    /// <param name="memberPath">The dot-separated public field or property path.</param>
    /// <param name="generate">The callback invoked when the rule is evaluated.</param>
    /// <returns>The added rule.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>memberPath</c> is empty, contains whitespace, or has an empty segment.</exception>
    FakeRule GenerateFromMemberPathAndGenerate(string memberPath, Func<object?> generate);
    /// <summary>Adds a rule that leaves a member unchanged.</summary>
    /// <param name="memberPath">The dot-separated public field or property path.</param>
    /// <returns>The added rule.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>memberPath</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>memberPath</c> is empty, contains whitespace, or has an empty segment.</exception>
    FakeRule Skip(string memberPath);
    /// <summary>Adds a value rule derived from the partially populated target object.</summary>
    /// <param name="memberPath">The dot-separated public field or property path.</param>
    /// <param name="derive">The callback invoked with the target object when the rule is evaluated.</param>
    /// <returns>The added rule.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>memberPath</c> is empty, contains whitespace, or has an empty segment.</exception>
    FakeRule Derived(string memberPath, Func<object, object?> derive);
    /// <summary>Adds a snapshot of another rule set beneath a member-path prefix.</summary>
    /// <param name="memberPath">The dot-separated path prepended to every nested rule.</param>
    /// <param name="rules">The nested rules to copy in their existing order.</param>
    /// <returns>The copied rules with combined paths and new insertion-order values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>memberPath</c> is empty, contains whitespace, or has an empty segment.</exception>
    FakeRule[] Nested(string memberPath, FakeRuleSet rules);
    /// <summary>Returns the number of configured rules.</summary>
    /// <returns>The rule count.</returns>
    int Count();
    /// <summary>Returns the configured rules in deterministic insertion order.</summary>
    /// <returns>A new array containing the ordered rules.</returns>
    FakeRule[] Rules();
    /// <summary>Enables strict diagnostics for missing and read-only member paths.</summary>
    /// <returns>This rule set.</returns>
    FakeRuleSet Strict();
    /// <summary>Sets whether missing and read-only member paths produce diagnostics.</summary>
    /// <param name="enabled">True to enable strict diagnostics; false to skip inaccessible paths.</param>
    /// <returns>This rule set.</returns>
    FakeRuleSet StrictFromEnabled(bool enabled);
    /// <summary>Returns whether strict missing-member diagnostics are enabled.</summary>
    /// <returns>True when strict diagnostics are enabled.</returns>
    bool IsStrict();
}
