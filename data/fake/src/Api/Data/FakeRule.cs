using System;

namespace fake;

/// <summary>Describes one ordered rule for a public field or property member path.</summary>
/// <remarks>Generated and derived callbacks are invoked only when the rule is evaluated by composition APIs.</remarks>
public class FakeRule
{
    /// <summary>The dot-separated public field or property path targeted by the rule.</summary>
    public string MemberPath = string.Empty;

    /// <summary>The operation performed by the rule.</summary>
    public FakeRuleKind Kind;

    /// <summary>The zero-based insertion order within the containing rule set.</summary>
    public int Order;

    /// <summary>The value supplied by a <see cref="FakeRuleKind.Constant"/> rule.</summary>
    public object? ConstantValue;

    /// <summary>The callback used by a <see cref="FakeRuleKind.Generated"/> rule.</summary>
    public Func<Fake, object?>? Generator;

    /// <summary>The callback used by a <see cref="FakeRuleKind.Derived"/> rule.</summary>
    public Func<object, object?>? Deriver;

    internal FakeRule(
        string memberPath,
        FakeRuleKind kind,
        int order,
        object? constantValue,
        Func<Fake, object?>? generator,
        Func<object, object?>? deriver)
    {
        MemberPath = memberPath;
        Kind = kind;
        Order = order;
        ConstantValue = constantValue;
        Generator = generator;
        Deriver = deriver;
    }

    internal object? Evaluate(Fake value, object target)
    {
        switch (Kind)
        {
            case FakeRuleKind.Constant:
                return ConstantValue;
            case FakeRuleKind.Generated:
                if (value == null)
                    throw new ArgumentNullException("value");
                if (Generator == null)
                    throw FakeException.Create("A generated rule has no callback.");
                return Generator(value);
            case FakeRuleKind.Derived:
                if (target == null)
                    throw new ArgumentNullException("target");
                if (Deriver == null)
                    throw FakeException.Create("A derived rule has no callback.");
                return Deriver(target);
            case FakeRuleKind.Skipped:
                throw new InvalidOperationException(
                    "A skipped rule does not produce a value.");
            default:
                throw FakeException.Create("The rule kind is not supported.");
        }
    }
}
