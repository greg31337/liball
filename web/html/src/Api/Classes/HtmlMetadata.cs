using System;
using System.Collections.Generic;

namespace html;
/// <summary>Contains resolved document metadata and the bounded candidates used to select each value.</summary>
public class HtmlMetadata : IHtmlMetadata
{
    internal HtmlMetadataCandidate[] candidates = Array.Empty<HtmlMetadataCandidate>();
    internal long candidateCount;
    internal long droppedCandidateCount;
    internal long duplicateCandidateCount;
    internal bool rawValuesTruncated;
    internal HtmlDiagnostic[] diagnostics = Array.Empty<HtmlDiagnostic>();
    internal long diagnosticCount;
    internal long droppedDiagnosticCount;
    internal HtmlMetadata(HtmlMetadataCandidate[] candidatesValue, long candidateCountValue, long droppedCandidateCountValue, long duplicateCandidateCountValue, bool rawValuesTruncatedValue, HtmlDiagnostic[] diagnosticsValue, long diagnosticCountValue, long droppedDiagnosticCountValue)
    {
        this.Initialize(candidatesValue, candidateCountValue, droppedCandidateCountValue, duplicateCandidateCountValue, rawValuesTruncatedValue, diagnosticsValue, diagnosticCountValue, droppedDiagnosticCountValue);
    }

    internal void Initialize(HtmlMetadataCandidate[] candidatesValue, long candidateCountValue, long droppedCandidateCountValue, long duplicateCandidateCountValue, bool rawValuesTruncatedValue, HtmlDiagnostic[] diagnosticsValue, long diagnosticCountValue, long droppedDiagnosticCountValue)
    {
        if (candidatesValue == null)
        {
            throw new ArgumentNullException("candidatesValue");
        }

        if (diagnosticsValue == null)
        {
            throw new ArgumentNullException("diagnosticsValue");
        }

        ValidateCandidateCounts(candidatesValue.Length, candidateCountValue, droppedCandidateCountValue, duplicateCandidateCountValue);
        ValidateCounts(diagnosticsValue.Length, diagnosticCountValue, droppedDiagnosticCountValue, "diagnosticCountValue");
        this.candidates = CopyCandidates(candidatesValue);
        Array.Sort(this.candidates, HtmlMetadataCandidateComparer.Compare);
        this.candidateCount = candidateCountValue;
        this.droppedCandidateCount = droppedCandidateCountValue;
        this.duplicateCandidateCount = duplicateCandidateCountValue;
        this.rawValuesTruncated = rawValuesTruncatedValue;
        this.diagnostics = CopyDiagnostics(diagnosticsValue);
        this.diagnosticCount = diagnosticCountValue;
        this.droppedDiagnosticCount = droppedDiagnosticCountValue;
    }

    /// <summary>Gets the selected document title.</summary>
    /// <returns>The title. Returns null when no title candidate exists.
    /// </returns>
    public string? GetTitle()
    {
        return this.GetValue(HtmlMetadataField.Title);
    }

    /// <summary>Gets the selected document base URL.</summary>
    /// <returns>The base URL. Returns null when it is unavailable.</returns>
    public string? GetBaseUrl()
    {
        return this.GetValue(HtmlMetadataField.BaseUrl);
    }

    /// <summary>Gets the selected canonical URL.</summary>
    /// <returns>The canonical URL. Returns null when it is unavailable.
    /// </returns>
    public string? GetCanonicalUrl()
    {
        return this.GetValue(HtmlMetadataField.CanonicalUrl);
    }

    /// <summary>Gets the alternate URLs.</summary>
    /// <returns>A snapshot of the alternate URLs; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetAlternateUrls()
    {
        HtmlMetadataCandidate[] values = this.GetCandidatesForField(HtmlMetadataField.AlternateUrl);
        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index].GetValue();
            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets the selected document description.</summary>
    /// <returns>The description. Returns null when it is unavailable.
    /// </returns>
    public string? GetDescription()
    {
        return this.GetValue(HtmlMetadataField.Description);
    }

    /// <summary>Gets the selected site name.</summary>
    /// <returns>The site name. Returns null when it is unavailable.</returns>
    public string? GetSiteName()
    {
        return this.GetValue(HtmlMetadataField.SiteName);
    }

    /// <summary>Gets the selected language tag.</summary>
    /// <returns>The language. Returns null when it is unavailable.</returns>
    public string? GetLanguage()
    {
        return this.GetValue(HtmlMetadataField.Language);
    }

    /// <summary>Gets the selected text direction.</summary>
    /// <returns>The direction. Returns null when it is unavailable.</returns>
    public string? GetDirection()
    {
        return this.GetValue(HtmlMetadataField.Direction);
    }

    /// <summary>Gets the selected favicon URL.</summary>
    /// <returns>The favicon URL. Returns null when it is unavailable.
    /// </returns>
    public string? GetFaviconUrl()
    {
        return this.GetValue(HtmlMetadataField.FaviconUrl);
    }

    /// <summary>Gets the selected author or byline.</summary>
    /// <returns>The author. Returns null when it is unavailable.</returns>
    public string? GetAuthor()
    {
        return this.GetValue(HtmlMetadataField.Author);
    }

    /// <summary>Gets the selected publication time text.</summary>
    /// <returns>The publication time text. Returns null when it is
    /// unavailable.</returns>
    public string? GetPublishedTime()
    {
        return this.GetValue(HtmlMetadataField.PublishedTime);
    }

    /// <summary>Gets the selected modification time text.</summary>
    /// <returns>The modification time text. Returns null when it is
    /// unavailable.</returns>
    public string? GetModifiedTime()
    {
        return this.GetValue(HtmlMetadataField.ModifiedTime);
    }

    /// <summary>Gets the selected representative image URL.</summary>
    /// <returns>The image URL. Returns null when it is unavailable.</returns>
    public string? GetImageUrl()
    {
        return this.GetValue(HtmlMetadataField.ImageUrl);
    }

    /// <summary>
    /// Gets candidates ordered by field, precedence, document position,
    /// source, value, and stable retention order.
    /// </summary>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlMetadataCandidate[] GetCandidates()
    {
        return CopyCandidates(this.candidates);
    }

    /// <summary>
    /// Gets candidates for a field from highest to lowest precedence.
    /// </summary>
    /// <param name = "field">The metadata field to inspect.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlMetadataCandidate[] GetCandidatesForField(HtmlMetadataField field)
    {
        HtmlMetadataValidation.ValidateField(field, "field");
        int count = 0;
        int index = 0;
        while (index < this.candidates.Length)
        {
            if (this.candidates[index].GetField() == field)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        HtmlMetadataCandidate[] result = new HtmlMetadataCandidate[count];
        index = 0;
        int outputIndex = 0;
        while (index < this.candidates.Length)
        {
            if (this.candidates[index].GetField() == field)
            {
                result[outputIndex] = this.candidates[index];
                outputIndex = outputIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets the highest-precedence candidate for a field.</summary>
    /// <returns>The candidate. Returns null when the field has no
    /// candidate.</returns>
    /// <param name = "field">The metadata field to inspect.</param>
    public HtmlMetadataCandidate? GetCandidate(HtmlMetadataField field)
    {
        HtmlMetadataValidation.ValidateField(field, "field");
        int index = 0;
        while (index < this.candidates.Length)
        {
            if (this.candidates[index].GetField() == field)
            {
                return this.candidates[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Gets how many normalized metadata candidates were considered before retention limits were applied.</summary>
    /// <returns>The total candidate count, including retained and dropped candidates.</returns>
    public long GetCandidateCount()
    {
        return this.candidateCount;
    }

    /// <summary>Gets how many metadata candidates could not be retained.</summary>
    /// <returns>The number omitted from candidate snapshots because the retention limit was exhausted.</returns>
    public long GetDroppedCandidateCount()
    {
        return this.droppedCandidateCount;
    }

    /// <summary>Gets how many otherwise equivalent metadata candidates were coalesced.</summary>
    /// <returns>The number of duplicates excluded from the retained candidate snapshot.</returns>
    public long GetDuplicateCandidateCount()
    {
        return this.duplicateCandidateCount;
    }

    /// <summary>Gets how many metadata candidates are available for inspection.</summary>
    /// <returns>The length of the array returned by <see cref = "GetCandidates()"/>.</returns>
    public int GetRetainedCandidateCount()
    {
        return this.candidates.Length;
    }

    /// <summary>Reports whether the candidate retention limit discarded entries.</summary>
    /// <returns><c>true</c> when one or more candidates were not retained; otherwise, <c>false</c>.</returns>
    public bool WereCandidatesTruncated()
    {
        return this.droppedCandidateCount > 0;
    }

    /// <summary>Reports whether one or more retained candidate values were shortened.</summary>
    /// <returns><c>true</c> when one or more candidate values were shortened; otherwise, <c>false</c>.</returns>
    public bool WereRawValuesTruncated()
    {
        return this.rawValuesTruncated;
    }

    /// <summary>Reports whether extraction retained incomplete bounded metadata.</summary>
    /// <returns><c>true</c> when extraction stopped early or retained truncated candidates, values, or diagnostics; otherwise, <c>false</c>.</returns>
    public bool IsPartial()
    {
        return this.WereCandidatesTruncated()
            || this.rawValuesTruncated
            || this.WereDiagnosticsTruncated()
            || HtmlResultDiagnostics.ContainsCode(this.diagnostics,
                HtmlDiagnosticCode.ResourceLimitExceeded)
            || HtmlResultDiagnostics.ContainsCode(this.diagnostics,
                HtmlDiagnosticCode.OperationCancelled)
            || HtmlResultDiagnostics.ContainsCode(this.diagnostics,
                HtmlDiagnosticCode.OperationIncomplete);
    }

    /// <summary>Reports whether cancellation stopped metadata extraction.</summary>
    /// <returns><c>true</c> when the diagnostics contain the cancellation code; otherwise, <c>false</c>.</returns>
    public bool WasCancelled()
    {
        return HtmlResultDiagnostics.ContainsCode(this.diagnostics,
            HtmlDiagnosticCode.OperationCancelled);
    }

    /// <summary>Gets the stable code explaining incomplete metadata extraction.</summary>
    /// <returns>The primary failure code, or <see cref = "HtmlDiagnosticCode.None"/> when extraction was not stopped.</returns>
    public HtmlDiagnosticCode GetFailureCode()
    {
        if (this.WasCancelled())
        {
            return HtmlDiagnosticCode.OperationCancelled;
        }

        if (this.IsPartial())
        {
            return HtmlResultDiagnostics.GetPrimaryCode(this.diagnostics,
                HtmlDiagnosticCode.ResourceLimitExceeded);
        }

        return HtmlDiagnosticCode.None;
    }

    /// <summary>Gets bounded diagnostics from metadata parsing and normalization.</summary>
    /// <returns>A snapshot of the diagnostics; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Gets how many metadata diagnostics were retained.</summary>
    /// <returns>The length of the diagnostics snapshot.</returns>
    public int GetRetainedDiagnosticCount()
    {
        return this.diagnostics.Length;
    }

    /// <summary>Gets how many metadata diagnostics were produced before retention limits were applied.</summary>
    /// <returns>The total diagnostic count, including retained and dropped entries.</returns>
    public long GetDiagnosticCount()
    {
        return this.diagnosticCount;
    }

    /// <summary>Gets how many metadata diagnostics could not be retained.</summary>
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

    internal string? GetValue(HtmlMetadataField field)
    {
        HtmlMetadataCandidate? candidate = this.GetCandidate(field);
        return candidate == null ? null : candidate.GetValue();
    }

    internal static void ValidateCounts(int retainedCount, long totalCount, long droppedCount, string parameterName)
    {
        if (totalCount < 0 || droppedCount < 0 || droppedCount > totalCount || retainedCount != totalCount - droppedCount)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    internal static void ValidateCandidateCounts(int retainedCount, long totalCount, long droppedCount, long duplicateCount)
    {
        if (totalCount < 0 || droppedCount < 0 || duplicateCount < 0 || droppedCount > totalCount || duplicateCount > totalCount - droppedCount || retainedCount != totalCount - droppedCount - duplicateCount)
        {
            throw new ArgumentOutOfRangeException("candidateCountValue");
        }
    }

    internal static HtmlMetadataCandidate[] CopyCandidates(HtmlMetadataCandidate[] values)
    {
        HtmlMetadataCandidate[] result = new HtmlMetadataCandidate[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Metadata candidates cannot contain null.", "candidatesValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }

    internal static HtmlDiagnostic[] CopyDiagnostics(HtmlDiagnostic[] values)
    {
        HtmlDiagnostic[] result = new HtmlDiagnostic[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Metadata diagnostics cannot contain null.", "diagnosticsValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}
