namespace fix;

/// <summary>Identifies the severity of a validation issue.</summary>
public enum ValidationSeverity
{
    /// <summary>The issue is informational.</summary>
    Information = 0,
    /// <summary>The issue is a warning.</summary>
    Warning = 1,
    /// <summary>The issue is an error.</summary>
    Error = 2,
    /// <summary>The issue prevents further meaningful validation.</summary>
    Fatal = 3
}
