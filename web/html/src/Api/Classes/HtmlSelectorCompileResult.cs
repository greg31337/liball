using System;
using System.Threading;

namespace html;
/// <summary>Reports selector compilation success and provides the compiled selector or diagnostics.</summary>
public class HtmlSelectorCompileResult : IHtmlSelectorCompileResult
{
    internal HtmlSelector? selector;
    internal HtmlDiagnostic[] diagnostics;
    internal HtmlSelectorCompileError[] errors;
    internal HtmlSelectorCompileResult(HtmlSelector? selectorValue, HtmlDiagnostic[] diagnosticsValue)
    {
        this.selector = selectorValue;
        this.diagnostics = CopyDiagnostics(diagnosticsValue);
        this.errors = CreateErrors(this.diagnostics);
    }

    /// <summary>Reports whether the operation completed successfully.</summary>
    /// <returns><c>true</c> when the operation succeeded; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return this.selector != null;
    }

    /// <summary>Gets the reusable compiled selector when compilation succeeded.</summary>
    /// <returns>The compiled selector. Returns null when compilation failed.</returns>
    public HtmlSelector? GetSelector()
    {
        return this.selector;
    }

    /// <summary>Gets all bounded diagnostics produced while compiling the selector.</summary>
    /// <returns>A snapshot of the diagnostics; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Gets the stable code explaining failed selector compilation.</summary>
    /// <returns>The primary diagnostic code, or <see cref = "HtmlDiagnosticCode.None"/> when compilation succeeded.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.IsSuccess())
        {
            return HtmlDiagnosticCode.None;
        }

        return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
            HtmlDiagnosticCode.OperationFailed);
    }

    /// <summary>Gets how many selector diagnostics were retained.</summary>
    /// <returns>The length of the diagnostics snapshot.</returns>
    public int GetRetainedDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets the total selector diagnostic count.</summary>
    /// <returns>The number of diagnostics produced by compilation.</returns>
    public long GetDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets how many selector diagnostics were dropped.</summary>
    /// <returns><c>0</c>; selector compilation retains every diagnostic it produces.</returns>
    public long GetDroppedDiagnosticCount()
    {
        return 0;
    }

    /// <summary>Reports whether selector diagnostic retention was truncated.</summary>
    /// <returns><c>false</c>; selector compilation retains every diagnostic it produces.</returns>
    public bool WereDiagnosticsTruncated()
    {
        return false;
    }

    /// <summary>Reports whether compilation returned a partial selector.</summary>
    /// <returns><c>true</c> when forgiving compilation discarded one or more invalid branches but retained a usable selector.</returns>
    public bool IsPartial()
    {
        return this.selector != null && this.errors.Length != 0;
    }

    /// <summary>Reports whether cancellation stopped compilation.</summary>
    /// <returns><c>false</c>; selector compilation currently has no cancellable returned-result path.</returns>
    public bool WasCancelled()
    {
        return false;
    }

    /// <summary>Gets selector-specific error views derived from the compilation diagnostics.</summary>
    /// <returns>A snapshot of the errors; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlSelectorCompileError[] GetErrors()
    {
        HtmlSelectorCompileError[] result = new HtmlSelectorCompileError[this.errors.Length];
        Array.Copy(this.errors, result, this.errors.Length);
        return result;
    }

    internal static HtmlSelectorCompileError[] CreateErrors(HtmlDiagnostic[] diagnostics)
    {
        HtmlSelectorCompileError[] result = new HtmlSelectorCompileError[diagnostics.Length];
        int index = 0;
        while (index < diagnostics.Length)
        {
            result[index] = new HtmlSelectorCompileError(diagnostics[index]);
            index = index + 1;
        }

        return result;
    }

    internal static HtmlDiagnostic[] CopyDiagnostics(HtmlDiagnostic[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("diagnosticsValue");
        }

        HtmlDiagnostic[] result = new HtmlDiagnostic[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("A selector diagnostic cannot be null.", "diagnosticsValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}
