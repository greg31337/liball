namespace log;

/// <summary>
/// Reports successful, dropped, diagnostic, and failed logging outcomes without using exceptions for normal flow.
/// </summary>
/// <remarks>
/// A result can report drops or diagnostics while still allowing processing to continue. ShouldContinue is false only for failures.
/// </remarks>
public class LogResult
{
    /// <summary>
    /// Whether the operation completed without a failure.
    /// </summary>
    public bool IsSuccess;
    /// <summary>
    /// Whether the operation encountered a failure that should stop processing.
    /// </summary>
    public bool IsFailure;
    /// <summary>
    /// Whether one or more events were intentionally not written.
    /// </summary>
    public bool IsDropped;
    /// <summary>
    /// Whether the operation produced noteworthy self-diagnostic information.
    /// </summary>
    public bool IsDiagnostic;
    /// <summary>
    /// The stable machine-readable outcome or diagnostic code.
    /// </summary>
    public string Code;
    /// <summary>
    /// The human-readable message.
    /// </summary>
    public string Message;
    /// <summary>
    /// The cumulative number of dropped events.
    /// </summary>
    public int DroppedCount;
    /// <summary>
    /// The number of failed operations observed by this object.
    /// </summary>
    public int FailureCount;
    /// <summary>
    /// The cumulative number of diagnostic outcomes.
    /// </summary>
    public int DiagnosticCount;

    /// <summary>
    /// Creates a result from explicit flags, details, and counters.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="isFailure">Whether the operation failed.</param>
    /// <param name="isDropped">Whether events were dropped.</param>
    /// <param name="isDiagnostic">Whether diagnostics were produced.</param>
    /// <param name="code">The stable result code.</param>
    /// <param name="message">The result message.</param>
    /// <param name="droppedCount">The dropped-event count.</param>
    /// <param name="failureCount">The failure count.</param>
    /// <param name="diagnosticCount">The diagnostic count.</param>
    public LogResult(bool isSuccess, bool isFailure, bool isDropped, bool isDiagnostic, string? code, string? message, int droppedCount, int failureCount, int diagnosticCount)
    {
        IsSuccess = isSuccess;
        IsFailure = isFailure;
        IsDropped = isDropped;
        IsDiagnostic = isDiagnostic;
        Code = LogResult.NormalizeText(code);
        Message = LogResult.NormalizeText(message);
        DroppedCount = LogResult.NormalizeCount(droppedCount);
        FailureCount = LogResult.NormalizeCount(failureCount);
        DiagnosticCount = LogResult.NormalizeCount(diagnosticCount);
    }

    /// <summary>
    /// Creates a result representing success.
    /// </summary>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Success()
    {
        return new LogResult(true, false, false, false, "", "", 0, 0, 0);
    }

    /// <summary>
    /// Creates a result representing success.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult SuccessMessage(string message)
    {
        return new LogResult(true, false, false, false, "", message, 0, 0, 0);
    }

    /// <summary>
    /// Creates a result representing failure.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Failure(string message)
    {
        return new LogResult(false, true, false, false, "", message, 0, 1, 0);
    }

    /// <summary>
    /// Creates a result representing failure.
    /// </summary>
    /// <param name="code">The stable machine-readable code.</param>
    /// <param name="message">The human-readable message.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult FailureCode(string code, string message)
    {
        return new LogResult(false, true, false, false, code, message, 0, 1, 0);
    }

    /// <summary>
    /// Creates a result representing dropped.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Dropped(string message)
    {
        return new LogResult(false, false, true, false, "", message, 1, 0, 0);
    }

    /// <summary>
    /// Creates a result representing dropped.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <param name="droppedCount">The number of dropped events represented by the result.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult DroppedCountResult(string message, int droppedCount)
    {
        return new LogResult(false, false, true, false, "", message, droppedCount, 0, 0);
    }

    /// <summary>
    /// Creates a result representing diagnostic.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Diagnostic(string message)
    {
        return new LogResult(true, false, false, true, "", message, 0, 0, 1);
    }

    /// <summary>
    /// Creates a result representing diagnostic.
    /// </summary>
    /// <param name="code">The stable machine-readable code.</param>
    /// <param name="message">The human-readable message.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult DiagnosticCode(string code, string message)
    {
        return new LogResult(true, false, false, true, code, message, 0, 0, 1);
    }

    /// <summary>
    /// Combines result flags, counters, codes, and messages from multiple operations.
    /// </summary>
    /// <param name="results">The results value.</param>
    /// <returns>A result containing success, drop, diagnostic, and failure details.</returns>
    internal static LogResult Combine(LogResult[] results)
    {
        if (results == null)
        {
            return LogResult.Success();
        }

        bool hasFailure = false;
        bool hasDropped = false;
        bool hasDiagnostic = false;
        int droppedCount = 0;
        int failureCount = 0;
        int diagnosticCount = 0;
        string code = "";
        string message = "";

        foreach (LogResult result in results)
        {
            if (result == null)
            {
                continue;
            }

            if (result.IsFailure)
            {
                hasFailure = true;
            }

            if (result.IsDropped)
            {
                hasDropped = true;
            }

            if (result.IsDiagnostic)
            {
                hasDiagnostic = true;
            }

            droppedCount = LogResult.AddCounts(droppedCount, result.DroppedCount);
            failureCount = LogResult.AddCounts(failureCount, result.FailureCount);
            diagnosticCount = LogResult.AddCounts(diagnosticCount, result.DiagnosticCount);

            if (code.Length == 0 && result.Code.Length > 0)
            {
                code = result.Code;
            }

            if (message.Length == 0 && result.Message.Length > 0)
            {
                message = result.Message;
            }
        }

        return new LogResult(!hasFailure && !hasDropped, hasFailure, hasDropped, hasDiagnostic, code, message, droppedCount, failureCount, diagnosticCount);
    }

    /// <summary>
    /// Reports whether processing may continue after this result.
    /// </summary>
    /// <returns>true when the stated condition is met; otherwise, false.</returns>
    internal bool ShouldContinue()
    {
        return !IsFailure && !IsDropped;
    }

    internal static string NormalizeText(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }

    internal static int NormalizeCount(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        return value;
    }

    internal static int IncrementCount(int value)
    {
        return LogResult.AddCounts(value, 1);
    }

    internal static int AddCounts(int left, int right)
    {
        int normalizedLeft = LogResult.NormalizeCount(left);
        int normalizedRight = LogResult.NormalizeCount(right);
        if (normalizedLeft > int.MaxValue - normalizedRight)
        {
            return int.MaxValue;
        }

        return normalizedLeft + normalizedRight;
    }
}
