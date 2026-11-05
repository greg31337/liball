using System.Collections.Generic;

namespace fix;

/// <summary>Contains all retained issues from a layered validation pass.</summary>
public class ValidationResult
{
    /// <summary>Whether the message passed every selected validation layer.</summary>
    public bool IsValid;
    /// <summary>Whether issue collection stopped at the configured limit.</summary>
    public bool IssueLimitReached;
    /// <summary>The issues in deterministic validation order.</summary>
    public List<ValidationIssue> Issues = [];
}
