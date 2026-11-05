using System;

namespace csv;

/// <summary>
/// Wraps row validation and diagnostic callbacks used by a pipeline.
/// </summary>
public class CsvRowValidator : ICsvRowValidator
{
    /// <summary>
    /// A stable machine-readable diagnostic code.
    /// </summary>
    internal string Code;
    /// <summary>
    /// A plain-language diagnostic explanation.
    /// </summary>
    internal string Message;
    /// <summary>
    /// Predicate that determines whether a row is valid.
    /// </summary>
    internal Func<CsvRow, bool> IsValidCallback;
    /// <summary>
    /// Callback that describes an invalid row.
    /// </summary>
    internal Func<CsvRow, CsvDiagnostic> CreateDiagnosticCallback;
    /// <summary>
    /// Whether validation invokes its predicate.
    /// </summary>
    internal bool HasIsValidCallback;
    /// <summary>
    /// Whether custom validation diagnostics are active.
    /// </summary>
    internal bool HasCreateDiagnosticCallback;

    /// <summary>
    /// Initializes a row validator with validation and diagnostic callbacks.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="isValidCallback">The validation predicate.</param>
    /// <param name="createDiagnosticCallback">The invalid-row diagnostic callback.</param>
    public CsvRowValidator(string code, string message, Func<CsvRow, bool> isValidCallback, Func<CsvRow, CsvDiagnostic> createDiagnosticCallback)
    {
        Code = code;
        Message = message;
        IsValidCallback = isValidCallback;
        CreateDiagnosticCallback = createDiagnosticCallback;
        HasIsValidCallback = true;
        HasCreateDiagnosticCallback = true;
    }

    /// <summary>
    /// Creates an instance populated with library defaults.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvRowValidator CreateDefault()
    {
        CsvRowValidator validator = new CsvRowValidator("ValidationFailed", "Row failed validation.", IsValidAlways, CreateFallbackDiagnostic);
        validator.HasIsValidCallback = false;
        validator.HasCreateDiagnosticCallback = false;
        return validator;
    }

    /// <summary>
    /// Returns the validation result, or true when validation is disabled.
    /// </summary>
    /// <param name="row">The row to process.</param>
    /// <returns>The configured predicate result, or <c>true</c> when validation is disabled.</returns>
    public bool IsValid(CsvRow row)
    {
        if (HasIsValidCallback)
        {
            bool valid = IsValidCallback(row);
            return valid;
        }

        return true;
    }

    internal static bool IsValidAlways(CsvRow row)
    {
        return true;
    }

    /// <summary>
    /// Creates a custom or default diagnostic for an invalid row.
    /// </summary>
    /// <param name="row">The row to process.</param>
    /// <returns>The initialized result.</returns>
    public CsvDiagnostic CreateDiagnostic(CsvRow row)
    {
        if (HasCreateDiagnosticCallback)
        {
            CsvDiagnostic customDiagnostic = CreateDiagnosticCallback(row);
            return customDiagnostic;
        }

        CsvDiagnostic diagnostic = CreateDefaultDiagnostic(row);
        return diagnostic;
    }

    internal CsvDiagnostic CreateDefaultDiagnostic(CsvRow row)
    {
        CsvDiagnostic diagnostic = CsvDiagnostic.CreateProcessingError(Code, Message, row.RowNumber, 0, row.Line, row.CharacterOffset, row.RawText);
        diagnostic.ByteOffset = row.ByteOffset;
        return diagnostic;
    }

    internal static CsvDiagnostic CreateFallbackDiagnostic(CsvRow row)
    {
        CsvDiagnostic diagnostic = CsvDiagnostic.CreateProcessingError("ValidationFailed", "Row failed validation.", row.RowNumber, 0, row.Line, row.CharacterOffset, row.RawText);
        diagnostic.ByteOffset = row.ByteOffset;
        return diagnostic;
    }
}
