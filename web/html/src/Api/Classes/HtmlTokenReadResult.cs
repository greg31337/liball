using System;

namespace html;
/// <summary>Reports whether an incremental read needs input, produced a token, completed, or stopped at a limit, cancellation request, or input failure.</summary>
public class HtmlTokenReadResult : IHtmlTokenReadResult
{
    internal HtmlTokenReadStatus status;
    internal HtmlToken? token;
    internal HtmlDiagnostic[] diagnostics;
    internal bool partial;
    internal bool diagnosticsTruncated;
    internal long droppedDiagnosticCount;
    internal HtmlTokenReadResult(HtmlTokenReadStatus status, HtmlToken? token, HtmlDiagnostic[] diagnostics, bool partial, bool diagnosticsTruncated, long droppedDiagnosticCount)
    {
        this.status = status;
        this.token = token;
        this.diagnostics = CopyDiagnostics(diagnostics);
        this.partial = partial;
        this.diagnosticsTruncated = diagnosticsTruncated;
        this.droppedDiagnosticCount = droppedDiagnosticCount;
    }

    internal static HtmlTokenReadResult CreateToken(HtmlToken token)
    {
        return CreateToken(token, new HtmlDiagnostic[0]);
    }

    internal static HtmlTokenReadResult CreateToken(HtmlToken token, HtmlDiagnostic[] diagnostics)
    {
        return CreateToken(token, diagnostics, false, false, 0);
    }

    internal static HtmlTokenReadResult CreateToken(HtmlToken token, HtmlDiagnostic[] diagnostics, bool partial, bool diagnosticsTruncated, long droppedDiagnosticCount)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        ValidateDiagnostics(diagnostics);
        ValidateDiagnosticTruncation(diagnosticsTruncated, droppedDiagnosticCount);
        return new HtmlTokenReadResult(HtmlTokenReadStatus.TokenAvailable, token, diagnostics, partial, diagnosticsTruncated, droppedDiagnosticCount);
    }

    internal static HtmlTokenReadResult CreateStatus(HtmlTokenReadStatus status)
    {
        return CreateStatus(status, new HtmlDiagnostic[0]);
    }

    internal static HtmlTokenReadResult CreateStatus(HtmlTokenReadStatus status, HtmlDiagnostic[] diagnostics)
    {
        return CreateStatus(status, diagnostics, false, false, 0);
    }

    internal static HtmlTokenReadResult CreateStatus(HtmlTokenReadStatus status, HtmlDiagnostic[] diagnostics, bool partial, bool diagnosticsTruncated, long droppedDiagnosticCount)
    {
        if (status == HtmlTokenReadStatus.TokenAvailable)
        {
            throw new ArgumentException("TokenAvailable requires a token.", "status");
        }

        if (status != HtmlTokenReadStatus.NeedMoreInput
            && status != HtmlTokenReadStatus.Complete
            && status != HtmlTokenReadStatus.LimitExceeded
            && status != HtmlTokenReadStatus.Cancelled
            && status != HtmlTokenReadStatus.Failed)
        {
            throw new ArgumentOutOfRangeException("status", status, "Unknown token read status.");
        }

        if (partial && status != HtmlTokenReadStatus.LimitExceeded
            && status != HtmlTokenReadStatus.Cancelled
            && status != HtmlTokenReadStatus.Failed)
        {
            throw new ArgumentException("Only a terminal stopped status can describe partial output.", "partial");
        }

        ValidateDiagnostics(diagnostics);
        ValidateDiagnosticTruncation(diagnosticsTruncated, droppedDiagnosticCount);
        return new HtmlTokenReadResult(status, null, diagnostics, partial, diagnosticsTruncated, droppedDiagnosticCount);
    }

    /// <summary>Gets whether the incremental read produced a token, needs input, completed, or stopped.</summary>
    /// <returns>The tokenizer read status.</returns>
    public HtmlTokenReadStatus GetStatus()
    {
        return this.status;
    }

    /// <summary>Reports whether this incremental read emitted a token that can be retrieved.</summary>
    /// <returns><c>true</c> when the requested token is present; otherwise, <c>false</c>.</returns>
    public bool HasToken()
    {
        return this.token != null;
    }

    /// <summary>Gets the token produced by this incremental read.</summary>
    /// <returns>The token. Returns null when the status did not produce one.</returns>
    public HtmlToken? GetToken()
    {
        return this.token;
    }

    /// <summary>Gets the bounded diagnostics retained by this result.</summary>
    /// <returns>A snapshot of the diagnostics; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Gets the stable code that explains a terminal limit, cancellation, or failure status.</summary>
    /// <returns>The primary diagnostic code, or <see cref = "HtmlDiagnosticCode.None"/> when the read did not fail.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.status == HtmlTokenReadStatus.Cancelled)
        {
            return HtmlDiagnosticCode.OperationCancelled;
        }

        if (this.status == HtmlTokenReadStatus.Failed)
        {
            return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
                HtmlDiagnosticCode.OperationFailed);
        }

        if (this.status == HtmlTokenReadStatus.LimitExceeded)
        {
            return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
                HtmlDiagnosticCode.ResourceLimitExceeded);
        }

        return HtmlDiagnosticCode.None;
    }

    /// <summary>Gets how many diagnostics are retained by this result.</summary>
    /// <returns>The length of the diagnostics snapshot.</returns>
    public int GetRetainedDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets the total diagnostic count before retention limits were applied.</summary>
    /// <returns>The retained plus dropped diagnostic count.</returns>
    public long GetDiagnosticCount()
    {
        return HtmlResultDiagnostics.GetTotalCount(this.diagnostics.Length,
            this.droppedDiagnosticCount);
    }

    /// <summary>Reports whether a terminal stopped status follows output already produced by this tokenizer.</summary>
    /// <returns><c>true</c> when output was produced before a limit, cancellation request, or failure stopped tokenization; otherwise, <c>false</c>.</returns>
    public bool IsPartial()
    {
        return this.partial;
    }

    /// <summary>Reports whether the diagnostic retention limit discarded entries.</summary>
    /// <returns><c>true</c> when one or more diagnostics were not retained; otherwise, <c>false</c>.</returns>
    public bool WereDiagnosticsTruncated()
    {
        return this.diagnosticsTruncated;
    }

    /// <summary>Gets how many tokenizer diagnostics could not be retained.</summary>
    /// <returns>The number omitted from <see cref = "GetDiagnostics"/> because the diagnostic limit was exhausted.</returns>
    public long GetDroppedDiagnosticCount()
    {
        return this.droppedDiagnosticCount;
    }

    internal static void ValidateDiagnostics(HtmlDiagnostic[] diagnostics)
    {
        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index] == null)
            {
                throw new ArgumentException("A diagnostic cannot be null.", "diagnostics");
            }

            index++;
        }
    }

    internal static void ValidateDiagnosticTruncation(bool diagnosticsTruncated, long droppedDiagnosticCount)
    {
        if (droppedDiagnosticCount < 0)
        {
            throw new ArgumentOutOfRangeException("droppedDiagnosticCount", droppedDiagnosticCount, "A dropped diagnostic count cannot be negative.");
        }

        if (diagnosticsTruncated != (droppedDiagnosticCount > 0))
        {
            throw new ArgumentException("Diagnostic truncation must agree with the dropped diagnostic count.", "diagnosticsTruncated");
        }
    }

    internal static HtmlDiagnostic[] CopyDiagnostics(HtmlDiagnostic[] values)
    {
        HtmlDiagnostic[] result = new HtmlDiagnostic[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }
}
