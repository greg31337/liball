using System;

namespace html;
/// <summary>Reports article-extraction success, completeness, output, and diagnostics.</summary>
public class HtmlArticleResult : IHtmlArticleResult
{
    internal bool success;
    internal bool complete;
    internal bool truncated;
    internal HtmlArticle? article;
    internal HtmlDiagnostic[] diagnostics;
    internal long diagnosticCount;
    internal long droppedDiagnosticCount;
    internal HtmlArticleResult(bool successValue, bool completeValue, bool truncatedValue, HtmlArticle? articleValue, HtmlDiagnostic[] diagnosticsValue, long diagnosticCountValue, long droppedDiagnosticCountValue)
    {
        if (successValue != (articleValue != null))
        {
            throw new ArgumentException("A successful article result must contain an article, and " + "an unsuccessful result cannot contain one.", "articleValue");
        }

        if (diagnosticsValue == null)
        {
            throw new ArgumentNullException("diagnosticsValue");
        }

        if (diagnosticCountValue < 0 || droppedDiagnosticCountValue < 0 || droppedDiagnosticCountValue > diagnosticCountValue || diagnosticsValue.Length != diagnosticCountValue - droppedDiagnosticCountValue)
        {
            throw new ArgumentOutOfRangeException("diagnosticCountValue");
        }

        if (completeValue && truncatedValue)
        {
            throw new ArgumentException("A complete article result cannot be truncated.", "completeValue");
        }

        this.success = successValue;
        this.complete = completeValue;
        this.truncated = truncatedValue;
        this.article = articleValue;
        this.diagnostics = CopyDiagnostics(diagnosticsValue);
        this.diagnosticCount = diagnosticCountValue;
        this.droppedDiagnosticCount = droppedDiagnosticCountValue;
    }

    /// <summary>Reports whether the operation completed successfully.</summary>
    /// <returns><c>true</c> when the operation succeeded; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return this.success;
    }

    /// <summary>Reports whether the operation produced a complete result.</summary>
    /// <returns><c>true</c> when processing completed without truncation; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return this.complete;
    }

    /// <summary>Reports whether the result was truncated by a configured limit.</summary>
    /// <returns><c>true</c> when a configured limit truncated the result; otherwise, <c>false</c>.</returns>
    public bool WasTruncated()
    {
        return this.truncated;
    }

    /// <summary>Reports whether incomplete extraction retained an article.</summary>
    /// <returns><c>true</c> when an incomplete result contains an article; otherwise, <c>false</c>.</returns>
    public bool IsPartial()
    {
        return !this.complete && this.article != null;
    }

    /// <summary>Reports whether cancellation stopped extraction.</summary>
    /// <returns><c>true</c> when the result contains the cancellation diagnostic; otherwise, <c>false</c>.</returns>
    public bool WasCancelled()
    {
        return HtmlResultDiagnostics.ContainsCode(this.diagnostics,
            HtmlDiagnosticCode.OperationCancelled);
    }

    /// <summary>Gets the stable code explaining unsuccessful extraction.</summary>
    /// <returns>The primary failure code, or <see cref = "HtmlDiagnosticCode.None"/> when extraction succeeded completely.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.success && this.complete)
        {
            return HtmlDiagnosticCode.None;
        }

        if (this.WasCancelled())
        {
            return HtmlDiagnosticCode.OperationCancelled;
        }

        if (this.truncated)
        {
            return HtmlDiagnosticCode.ResourceLimitExceeded;
        }

        if (!this.success)
        {
            return HtmlDiagnosticCode.ArticleExtractionFailed;
        }

        return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
            HtmlDiagnosticCode.OperationIncomplete);
    }

    /// <summary>Gets the extraction-stage article.</summary>
    /// <remarks>The returned content is not sanitized. Use the combined
    /// processing pipeline when renderable article content is required.
    /// </remarks>
    /// <returns>The unsanitized article. Returns null when extraction was
    /// unsuccessful.</returns>
    public HtmlArticle? GetArticle()
    {
        return this.article;
    }

    internal HtmlArticle? GetUnsanitizedArticle()
    {
        return this.article;
    }

    /// <summary>Gets the bounded diagnostics retained by this result.</summary>
    /// <returns>A snapshot of the diagnostics; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Gets how many extraction diagnostics were retained.</summary>
    /// <returns>The length of the diagnostics snapshot.</returns>
    public int GetRetainedDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets how many diagnostics extraction produced before retention limits were applied.</summary>
    /// <returns>The total diagnostic count, including retained and dropped entries.</returns>
    public long GetDiagnosticCount()
    {
        return this.diagnosticCount;
    }

    /// <summary>Gets how many extraction diagnostics could not be retained.</summary>
    /// <returns>The number omitted from <see cref = "GetDiagnostics"/> because the diagnostic limit was exhausted.</returns>
    public long GetDroppedDiagnosticCount()
    {
        return this.droppedDiagnosticCount;
    }

    /// <summary>Reports whether the diagnostic retention limit discarded entries.</summary>
    /// <returns><c>true</c> when one or more diagnostics were not retained; otherwise, <c>false</c>.</returns>
    public bool WereDiagnosticsTruncated()
    {
        return this.droppedDiagnosticCount > 0;
    }

    internal static HtmlDiagnostic[] CopyDiagnostics(HtmlDiagnostic[] values)
    {
        HtmlDiagnostic[] result = new HtmlDiagnostic[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Article diagnostics cannot contain null.", "diagnosticsValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}
