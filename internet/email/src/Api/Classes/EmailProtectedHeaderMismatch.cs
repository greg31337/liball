using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents a protected header mismatch.
/// </summary>
public class EmailProtectedHeaderMismatch : IEmailProtectedHeaderMismatchContract
{
    EmailProtectedHeaderMismatchKind kind;
    string fieldName;
    string? expectedValue;
    string? actualValue;
    /// <summary>
    /// Creates a new protected header mismatch.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <param name = "expectedValue">The expected value used by the operation.</param>
    /// <param name = "actualValue">The actual value used by the operation.</param>
    public EmailProtectedHeaderMismatch(EmailProtectedHeaderMismatchKind kind, string fieldName, string? expectedValue, string? actualValue)
    {
        this.kind = kind;
        this.fieldName = fieldName;
        this.expectedValue = expectedValue;
        this.actualValue = actualValue;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailProtectedHeaderMismatchKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the field name.
    /// </summary>
    /// <returns>The current field name.</returns>
    public string GetFieldName()
    {
        return fieldName;
    }

    /// <summary>
    /// Determines whether the expected value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the expected value is available; otherwise, <c>false</c>.</returns>
    public bool HasExpectedValue()
    {
        return expectedValue != null;
    }

    /// <summary>
    /// Gets the expected value.
    /// </summary>
    /// <returns>The current expected value.</returns>
    public string? GetExpectedValue()
    {
        return expectedValue;
    }

    /// <summary>
    /// Determines whether the actual value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the actual value is available; otherwise, <c>false</c>.</returns>
    public bool HasActualValue()
    {
        return actualValue != null;
    }

    /// <summary>
    /// Gets the actual value.
    /// </summary>
    /// <returns>The current actual value.</returns>
    public string? GetActualValue()
    {
        return actualValue;
    }
}
