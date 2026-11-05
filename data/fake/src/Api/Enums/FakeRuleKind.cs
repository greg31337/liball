using System;

namespace fake;

/// <summary>Selects how a composition rule supplies or suppresses a member value.</summary>
public enum FakeRuleKind
{
    /// <summary>The rule supplies its stored constant value.</summary>
    Constant = 0,

    /// <summary>The rule invokes a callback with the active <see cref="Fake"/> instance.</summary>
    Generated = 1,

    /// <summary>The rule leaves the target member unchanged.</summary>
    Skipped = 2,

    /// <summary>The rule invokes a callback with the partially populated target object.</summary>
    Derived = 3
}
