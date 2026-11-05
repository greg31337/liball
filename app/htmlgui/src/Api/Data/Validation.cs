using System;

namespace htmlgui;

/// <summary>Describes one copied custom-validation callback invocation.</summary>
public class UiValidationContext
{
    /// <summary>The stable form key.</summary>
    public string FormKey = string.Empty;
    /// <summary>The optional stable field key.</summary>
    public string? FieldKey;
    /// <summary>The stable validation rule key.</summary>
    public string RuleKey = string.Empty;
    /// <summary>The custom validation kind token.</summary>
    public string Kind = string.Empty;
    /// <summary>The callback generation.</summary>
    public int Generation;
    /// <summary>The submitted value version.</summary>
    public int ValueVersion;
    /// <summary>Whether the rule is configured for deferred task completion.</summary>
    public bool ReturnsTask;
    /// <summary>The optional copied field value.</summary>
    public UiValue? Value;
    /// <summary>The copied form values.</summary>
    public UiValueEntry[] Values = Array.Empty<UiValueEntry>();
    /// <summary>The copied validation parameters.</summary>
    public UiValueEntry[] Parameters = Array.Empty<UiValueEntry>();
}

/// <summary>Describes an exceptionless custom-validation callback outcome.</summary>
public class UiValidationHandlerResult
{
    /// <summary>Whether the callback completed successfully.</summary>
    public bool Success = true;
    /// <summary>The validation issues produced by the callback.</summary>
    public UiValidationIssue[] Issues = Array.Empty<UiValidationIssue>();
    /// <summary>The ordered callback diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = Array.Empty<UiDiagnostic>();
}

/// <summary>Contains one exceptionless validation or submission operation result.</summary>
public class UiValidationResult
{
    /// <summary>Whether the operation completed successfully.</summary>
    public bool Success;
    /// <summary>Whether the operation changed validation or submission state.</summary>
    public bool Applied;
    /// <summary>Whether completed validation accepts the form.</summary>
    public bool IsValid;
    /// <summary>Whether validation callbacks remain pending.</summary>
    public bool IsPending;
    /// <summary>Whether form submission is currently permitted.</summary>
    public bool CanSubmit;
    /// <summary>The captured submission value version, or zero when no submission was captured.</summary>
    public int SubmittedVersion;
    /// <summary>The resulting copied form.</summary>
    public UiForm? Form;
    /// <summary>The resulting copied aggregate validation report.</summary>
    public UiValidationReport? Report;
    /// <summary>The ordered operation and callback diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = Array.Empty<UiDiagnostic>();
}
