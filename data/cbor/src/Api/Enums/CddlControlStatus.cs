namespace cbor;

/// <summary>Identifies the outcome of evaluating or generating a CDDL control.</summary>
public enum CddlControlStatus
{
    /// <summary>No control outcome has been assigned.</summary>
    None = 0,
    /// <summary>The control completed successfully.</summary>
    Applied = 1,
    /// <summary>The controlled value did not match.</summary>
    Mismatch = 2,
    /// <summary>The handler does not apply to the supplied operands.</summary>
    NotApplicable = 3,
    /// <summary>The control is recognized but unsupported.</summary>
    Unsupported = 4,
    /// <summary>The control failed exceptionally.</summary>
    Error = 5
}
