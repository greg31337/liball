using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Coordinates form state, validation callbacks, issues, and submission eligibility.</summary>
public interface IUiValidationEngine
{
    /// <summary>Registers one copied form definition.</summary>
    /// <param name="form">The form definition.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiValidationResult RegisterValidationForm(UiForm? form);

    /// <summary>Unregisters one form and its validation state.</summary>
    /// <param name="formKey">The stable form key.</param>
    /// <returns>The exceptionless unregistration result.</returns>
    UiValidationResult UnregisterValidationForm(string? formKey);

    /// <summary>Reads one copied form and its current validation state.</summary>
    /// <param name="formKey">The stable form key.</param>
    /// <returns>The exceptionless form result.</returns>
    UiValidationResult ReadValidationForm(string? formKey);

    /// <summary>Updates one copied field value and cancels callbacks made stale by the edit.</summary>
    /// <param name="formKey">The stable form key.</param>
    /// <param name="fieldKey">The stable field key.</param>
    /// <param name="value">The new portable value.</param>
    /// <returns>The exceptionless field update result.</returns>
    UiValidationResult UpdateValidationFieldValue(string? formKey, string? fieldKey, UiValue? value);

    /// <summary>Runs matching field validation rules and their registered callbacks.</summary>
    /// <param name="formKey">The stable form key.</param>
    /// <param name="fieldKey">The stable field key.</param>
    /// <param name="trigger">The requested validation trigger.</param>
    /// <returns>A task containing the completed exceptionless validation result.</returns>
    Task<UiValidationResult> ValidateUiField(string? formKey, string? fieldKey, UiValidationTrigger trigger);

    /// <summary>Runs matching rules for every field and the form.</summary>
    /// <param name="formKey">The stable form key.</param>
    /// <param name="trigger">The requested validation trigger.</param>
    /// <returns>A task containing the completed exceptionless validation result.</returns>
    Task<UiValidationResult> ValidateUiForm(string? formKey, UiValidationTrigger trigger);

    /// <summary>Registers one stable custom-validation callback handler.</summary>
    /// <param name="handlerId">The stable handler identifier referenced by rules.</param>
    /// <param name="handler">The validation callback handler.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiResult RegisterUiValidationHandler(string? handlerId, IUiValidationHandler? handler);

    /// <summary>Unregisters one custom-validation callback handler.</summary>
    /// <param name="handlerId">The stable handler identifier.</param>
    /// <returns>The exceptionless unregistration result.</returns>
    UiResult UnregisterUiValidationHandler(string? handlerId);

    /// <summary>Cancels one pending custom-validation callback.</summary>
    /// <param name="formKey">The stable form key.</param>
    /// <param name="fieldKey">The optional stable field key.</param>
    /// <param name="ruleKey">The stable rule key.</param>
    /// <returns>The exceptionless cancellation result.</returns>
    UiValidationResult CancelUiValidationCallback(string? formKey, string? fieldKey, string? ruleKey);

    /// <summary>Merges copied issues into current form validation state.</summary>
    /// <param name="formKey">The stable form key.</param>
    /// <param name="fieldKey">The optional field scope.</param>
    /// <param name="issues">The issues to merge.</param>
    /// <returns>The exceptionless merge result.</returns>
    UiValidationResult MergeUiValidationIssues(string? formKey, string? fieldKey, UiValidationIssue[]? issues);

    /// <summary>Clears issues from one source in a field or entire form scope.</summary>
    /// <param name="formKey">The stable form key.</param>
    /// <param name="fieldKey">The optional field scope.</param>
    /// <param name="source">The issue source token.</param>
    /// <returns>The exceptionless clear result.</returns>
    UiValidationResult ClearUiValidationIssues(string? formKey, string? fieldKey, string? source);

    /// <summary>Returns current form validity and submission eligibility.</summary>
    /// <param name="formKey">The stable form key.</param>
    /// <returns>The exceptionless eligibility result.</returns>
    UiValidationResult QueryUiSubmitEligibility(string? formKey);

    /// <summary>Begins submission and captures the submitted value version.</summary>
    /// <param name="formKey">The stable form key.</param>
    /// <returns>The exceptionless submission result.</returns>
    UiValidationResult BeginUiFormSubmission(string? formKey);

    /// <summary>Completes submission and discards its captured value snapshot.</summary>
    /// <param name="formKey">The stable form key.</param>
    /// <param name="submittedVersion">The captured submitted version.</param>
    /// <param name="success">Whether submission was accepted.</param>
    /// <returns>The exceptionless submission result.</returns>
    UiValidationResult CompleteUiFormSubmission(string? formKey, int submittedVersion, bool success);

    /// <summary>Ingests host or server issues for one submitted value version.</summary>
    /// <param name="formKey">The stable form key.</param>
    /// <param name="fieldKey">The optional field scope.</param>
    /// <param name="submittedVersion">The captured submitted version.</param>
    /// <param name="source">The host or server source token.</param>
    /// <param name="issues">The versioned validation issues.</param>
    /// <returns>The exceptionless ingestion result.</returns>
    UiValidationResult IngestUiValidationIssues(string? formKey, string? fieldKey, int submittedVersion, string? source, UiValidationIssue[]? issues);
}
