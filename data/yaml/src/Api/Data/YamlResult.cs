using System;
using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Collects success state and structured diagnostics for an operation.
/// </summary>
public class YamlResult
{
    /// <summary>
    /// Whether the operation completed without errors.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The diagnostics in the order they were reported.
    /// </summary>
    public List<YamlDiagnostic> Diagnostics;

    /// <summary>
    /// Initializes a new YAML result with successful status and no diagnostics.
    /// </summary>
    public YamlResult()
    {
        Success = true;
        Diagnostics = new List<YamlDiagnostic>();
    }

    /// <summary>
    /// Resets this instance to the library's default state.
    /// </summary>
    internal void Reset()
    {
        Success = true;
        Diagnostics.Clear();
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to append.</param>
    internal void AddDiagnostic(YamlDiagnostic? diagnostic)
    {
        if (diagnostic == null)
        {
            return;
        }

        Diagnostics.Add(diagnostic);
        if (diagnostic.IsError())
        {
            Success = false;
        }
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The plain-English diagnostic or validation message.</param>
    /// <param name="span">The source range associated with the value.</param>
    internal void AddError(string? code, string? message, YamlSourceSpan? span)
    {
        AddDiagnostic(YamlDiagnostic.Error(code, message, span));
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The plain-English diagnostic or validation message.</param>
    /// <param name="span">The source range associated with the value.</param>
    internal void AddWarning(string? code, string? message, YamlSourceSpan? span)
    {
        AddDiagnostic(YamlDiagnostic.Warning(code, message, span));
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The plain-English diagnostic or validation message.</param>
    /// <param name="span">The source range associated with the value.</param>
    internal void AddInfo(string? code, string? message, YamlSourceSpan? span)
    {
        AddDiagnostic(YamlDiagnostic.Info(code, message, span));
    }

    /// <summary>
    /// Determines whether any diagnostics have been recorded.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool HasDiagnostics()
    {
        return Diagnostics.Count > 0;
    }

    /// <summary>
    /// Determines whether at least one recorded diagnostic has error severity.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool HasErrors()
    {
        int index = 0;
        while (index < Diagnostics.Count)
        {
            if (Diagnostics[index].IsError())
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Returns the number of error entries.
    /// </summary>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int ErrorCount()
    {
        int count = 0;
        int index = 0;
        while (index < Diagnostics.Count)
        {
            if (Diagnostics[index].IsError())
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>
    /// Returns the number of warning entries.
    /// </summary>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int WarningCount()
    {
        int count = 0;
        int index = 0;
        while (index < Diagnostics.Count)
        {
            if (Diagnostics[index].IsWarning())
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>
    /// Formats all diagnostics as newline-separated readable lines.
    /// </summary>
    /// <returns>The emitted or formatted text.</returns>
    internal string FormatDiagnostics()
    {
        string text = "";
        int index = 0;
        while (index < Diagnostics.Count)
        {
            if (index > 0)
            {
                text = text + "\n";
            }

            text = text + Diagnostics[index].Format();
            index = index + 1;
        }

        return text;
    }
}

/// <summary>
/// Receives a parsed, loaded, or dumped value and diagnostics from a non-throwing facade method.
/// </summary>
public class YamlParseResult
{
    internal Exception? Failure;
    /// <summary>
    /// Whether the operation completed without errors.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The operation result: a document, stream, loaded value, or emitted string depending on the facade method.
    /// </summary>
    public object? Value;
    /// <summary>
    /// The diagnostics in the order they were reported.
    /// </summary>
    public List<YamlDiagnostic> Diagnostics;

    /// <summary>
    /// Initializes a new YAML parse result with no value or diagnostics.
    /// </summary>
    public YamlParseResult()
    {
        Failure = null;
        Success = true;
        Value = null;
        Diagnostics = new List<YamlDiagnostic>();
    }

    /// <summary>
    /// Resets this instance to the library's default state.
    /// </summary>
    internal void Reset()
    {
        Failure = null;
        Success = true;
        Value = null;
        Diagnostics.Clear();
    }

    /// <summary>
    /// Replaces this instance's stored values with the supplied values.
    /// </summary>
    /// <param name="value">The value to process.</param>
    internal void SetValue(object? value)
    {
        Value = value;
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to append.</param>
    internal void AddDiagnostic(YamlDiagnostic? diagnostic)
    {
        if (diagnostic == null)
        {
            return;
        }

        Diagnostics.Add(diagnostic);
        if (diagnostic.IsError())
        {
            Success = false;
        }
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The plain-English diagnostic or validation message.</param>
    /// <param name="span">The source range associated with the value.</param>
    internal void AddError(string? code, string? message, YamlSourceSpan? span)
    {
        AddDiagnostic(YamlDiagnostic.Error(code, message, span));
    }

    /// <summary>
    /// Records an operation exception as an error while retaining it for a throwing facade.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="failure">The exception that caused the operation to fail.</param>
    internal void AddFailure(string code, Exception? failure)
    {
        Failure = failure;
        string message = failure == null || failure.Message == ""
            ? "YAML processing failed."
            : failure.Message;
        AddError(code, message, YamlSourceSpan.Empty());
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The plain-English diagnostic or validation message.</param>
    /// <param name="span">The source range associated with the value.</param>
    internal void AddWarning(string? code, string? message, YamlSourceSpan? span)
    {
        AddDiagnostic(YamlDiagnostic.Warning(code, message, span));
    }

    /// <summary>
    /// Determines whether any diagnostics have been recorded.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool HasDiagnostics()
    {
        return Diagnostics.Count > 0;
    }

    /// <summary>
    /// Determines whether at least one recorded diagnostic has error severity.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool HasErrors()
    {
        int index = 0;
        while (index < Diagnostics.Count)
        {
            if (Diagnostics[index].IsError())
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Returns the number of error entries.
    /// </summary>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int ErrorCount()
    {
        int count = 0;
        int index = 0;
        while (index < Diagnostics.Count)
        {
            if (Diagnostics[index].IsError())
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>
    /// Formats all diagnostics as newline-separated readable lines.
    /// </summary>
    /// <returns>The emitted or formatted text.</returns>
    internal string FormatDiagnostics()
    {
        string text = "";
        int index = 0;
        while (index < Diagnostics.Count)
        {
            if (index > 0)
            {
                text = text + "\n";
            }

            text = text + Diagnostics[index].Format();
            index = index + 1;
        }

        return text;
    }
}
