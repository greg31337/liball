using System;

namespace csv;

/// <summary>
/// Wraps field validation and diagnostic callbacks used by a pipeline.
/// </summary>
public class CsvFieldValidator : ICsvFieldValidator
{
    internal string Code;
    internal string Message;
    internal Func<CsvRow, int, string?, bool> IsValidCallback;
    internal Func<CsvRow, int, string?, CsvDiagnostic> CreateDiagnosticCallback;
    internal bool HasIsValidCallback;
    internal bool HasCreateDiagnosticCallback;

    /// <summary>
    /// Initializes a field validator with validation and diagnostic callbacks.
    /// </summary>
    /// <param name="code">The fallback diagnostic code.</param>
    /// <param name="message">The fallback diagnostic message.</param>
    /// <param name="isValidCallback">The field validation predicate.</param>
    /// <param name="createDiagnosticCallback">The invalid-field diagnostic callback.</param>
    public CsvFieldValidator(string code, string message, Func<CsvRow, int, string?, bool> isValidCallback, Func<CsvRow, int, string?, CsvDiagnostic> createDiagnosticCallback)
    {
        Code = code;
        Message = message;
        IsValidCallback = isValidCallback;
        CreateDiagnosticCallback = createDiagnosticCallback;
        HasIsValidCallback = true;
        HasCreateDiagnosticCallback = true;
    }

    internal static CsvFieldValidator CreateDefault()
    {
        CsvFieldValidator validator = new CsvFieldValidator("FieldValidationFailed", "Field failed validation.", IsValidAlways, CreateFallbackDiagnostic);
        validator.HasIsValidCallback = false;
        validator.HasCreateDiagnosticCallback = false;
        return validator;
    }

    /// <summary>Returns the validation result, or true when validation is disabled.</summary>
    /// <param name="row">The row containing the field.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="value">The field value, which may be null.</param>
    /// <returns>The configured predicate result, or <c>true</c> when validation is disabled.</returns>
    public bool IsValid(CsvRow row, int fieldIndex, string? value)
    {
        if (HasIsValidCallback)
        {
            bool valid = IsValidCallback(row, fieldIndex, value);
            return valid;
        }

        return true;
    }

    /// <summary>Creates a custom or fallback diagnostic for an invalid field.</summary>
    /// <param name="row">The row containing the invalid field.</param>
    /// <param name="fieldIndex">The zero-based field position.</param>
    /// <param name="value">The invalid field value, which may be null.</param>
    /// <returns>The field validation diagnostic.</returns>
    public CsvDiagnostic CreateDiagnostic(CsvRow row, int fieldIndex, string? value)
    {
        if (HasCreateDiagnosticCallback)
        {
            CsvDiagnostic customDiagnostic = CreateDiagnosticCallback(row, fieldIndex, value);
            return customDiagnostic;
        }

        CsvDiagnostic diagnostic = CreateDefaultDiagnostic(row, fieldIndex);
        return diagnostic;
    }

    internal CsvDiagnostic CreateDefaultDiagnostic(CsvRow row, int fieldIndex)
    {
        CsvDiagnostic diagnostic = CsvDiagnostic.CreateProcessingError(Code, Message, row.RowNumber, fieldIndex + 1, row.Line, row.CharacterOffset, row.RawText);
        diagnostic.ByteOffset = row.ByteOffset;
        return diagnostic;
    }

    internal static bool IsValidAlways(CsvRow row, int fieldIndex, string? value)
    {
        return true;
    }

    internal static CsvDiagnostic CreateFallbackDiagnostic(CsvRow row, int fieldIndex, string? value)
    {
        CsvDiagnostic diagnostic = CsvDiagnostic.CreateProcessingError("FieldValidationFailed", "Field failed validation.", row.RowNumber, fieldIndex + 1, row.Line, row.CharacterOffset, row.RawText);
        diagnostic.ByteOffset = row.ByteOffset;
        return diagnostic;
    }
}
