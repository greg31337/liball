using System;

namespace htmlgui;

/// <summary>Describes a form, its fields, validation, and edit state.</summary>
public class UiForm
{
    /// <summary>The stable form key.</summary>
    public string Key = string.Empty;
    /// <summary>The optional submission URL.</summary>
    public string? Action;
    /// <summary>The submission method token.</summary>
    public string Method = "post";
    /// <summary>The registered fields.</summary>
    public UiField[] Fields = Array.Empty<UiField>();
    /// <summary>The form-level validation rules.</summary>
    public UiValidationRule[] Rules = Array.Empty<UiValidationRule>();
    /// <summary>The current form state.</summary>
    public UiFormState State = new UiFormState();
    /// <summary>The aggregate validation report.</summary>
    public UiValidationReport Validation = new UiValidationReport();
    /// <summary>The optional stable submit-command identifier.</summary>
    public string? SubmitCommandId;
    /// <summary>The optional stable reset-command identifier.</summary>
    public string? ResetCommandId;
    /// <summary>The optional stable cancel-command identifier.</summary>
    public string? CancelCommandId;
    /// <summary>Whether browser-native validation UI is disabled.</summary>
    public bool DisableNativeValidation;
}

/// <summary>Describes one registered form field and its current interaction state.</summary>
public class UiField
{
    /// <summary>The stable field key.</summary>
    public string Key = string.Empty;
    /// <summary>The submitted field name.</summary>
    public string Name = string.Empty;
    /// <summary>The visible label.</summary>
    public string Label = string.Empty;
    /// <summary>The current portable value.</summary>
    public UiValue Value = new UiValue();
    /// <summary>The optional input description.</summary>
    public UiInput? Input;
    /// <summary>The selectable choices.</summary>
    public UiChoice[] Choices = Array.Empty<UiChoice>();
    /// <summary>The validation rules.</summary>
    public UiValidationRule[] Rules = Array.Empty<UiValidationRule>();
    /// <summary>The current validation issues.</summary>
    public UiValidationIssue[] Issues = Array.Empty<UiValidationIssue>();
    /// <summary>Whether the field is dirty.</summary>
    public bool IsDirty;
    /// <summary>Whether the field has been touched.</summary>
    public bool IsTouched;
    /// <summary>Whether the field has been visited.</summary>
    public bool IsVisited;
    /// <summary>Whether validation is pending.</summary>
    public bool IsPending;
    /// <summary>Whether completed validation currently accepts the field.</summary>
    public bool IsValid = true;
}

/// <summary>Describes one built-in or handler-based validation rule.</summary>
public class UiValidationRule
{
    /// <summary>The stable rule key.</summary>
    public string Key = string.Empty;
    /// <summary>The rule kind token.</summary>
    public string Kind = string.Empty;
    /// <summary>The validation trigger.</summary>
    public UiValidationTrigger Trigger;
    /// <summary>The severity produced by a failed rule.</summary>
    public UiValidationSeverity Severity = UiValidationSeverity.Error;
    /// <summary>The failure message.</summary>
    public string Message = string.Empty;
    /// <summary>The optional custom handler identifier.</summary>
    public string? HandlerId;
    /// <summary>The rule parameters.</summary>
    public UiValueEntry[] Parameters = Array.Empty<UiValueEntry>();
}

/// <summary>Describes one validation issue associated with a form or field.</summary>
public class UiValidationIssue
{
    /// <summary>The stable issue identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The optional stable field key.</summary>
    public string? FieldKey;
    /// <summary>The stable diagnostic code.</summary>
    public string Code = string.Empty;
    /// <summary>The user-facing message.</summary>
    public string Message = string.Empty;
    /// <summary>The issue severity.</summary>
    public UiValidationSeverity Severity = UiValidationSeverity.Error;
    /// <summary>The source token, such as local, custom, remote, or host.</summary>
    public string Source = string.Empty;
    /// <summary>The value version to which the issue applies.</summary>
    public int ValueVersion;
}

/// <summary>Describes aggregate form validation state and ordered issues.</summary>
public class UiValidationReport
{
    /// <summary>The ordered validation issues.</summary>
    public UiValidationIssue[] Issues = Array.Empty<UiValidationIssue>();
    /// <summary>Whether all completed validation succeeded.</summary>
    public bool IsValid = true;
    /// <summary>Whether validation is pending.</summary>
    public bool IsPending;
    /// <summary>The optional key of the first invalid field.</summary>
    public string? FirstInvalidFieldKey;
    /// <summary>The validation generation.</summary>
    public int Version;
}

/// <summary>Describes initial/current form values and interaction lifecycle state.</summary>
public class UiFormState
{
    /// <summary>The initial field values.</summary>
    public UiValueEntry[] InitialValues = Array.Empty<UiValueEntry>();
    /// <summary>The current field values.</summary>
    public UiValueEntry[] CurrentValues = Array.Empty<UiValueEntry>();
    /// <summary>Whether any field is dirty.</summary>
    public bool IsDirty;
    /// <summary>Whether any validation is pending.</summary>
    public bool IsPending;
    /// <summary>Whether the form is currently submitting.</summary>
    public bool IsSubmitting;
    /// <summary>Whether submission is currently permitted.</summary>
    public bool CanSubmit = true;
    /// <summary>The monotonically increasing edit version.</summary>
    public int Version;
}
