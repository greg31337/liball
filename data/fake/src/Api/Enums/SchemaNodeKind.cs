using System;

namespace fake;

/// <summary>Selects the operation represented by a schema node.</summary>
public enum SchemaNodeKind
{
    /// <summary>The node invokes a scalar value callback.</summary>
    Scalar = 0,

    /// <summary>The node evaluates ordered named child fields.</summary>
    Object = 1,

    /// <summary>The node repeats one item node a bounded number of times.</summary>
    Array = 2,

    /// <summary>The node returns a stored constant value.</summary>
    Constant = 3,

    /// <summary>The node resolves a previously evaluated field path.</summary>
    Reference = 4,

    /// <summary>The node invokes a callback with the current correlation context.</summary>
    Derived = 5,

    /// <summary>The node chooses between two children using a predicate.</summary>
    Condition = 6,

    /// <summary>The node selects one weighted child.</summary>
    Variant = 7,

    /// <summary>The node returns the next value from a named integer sequence.</summary>
    Sequence = 8,

    /// <summary>The node retries a child until its value is unique in a named scope.</summary>
    Unique = 9
}
