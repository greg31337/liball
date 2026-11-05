using System;
using System.Collections.Generic;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlMetadata"/>.</summary>
public interface IHtmlMetadata
{
    /// <summary>Executes the GetTitle operation.</summary>
    public string? GetTitle();
    /// <summary>Executes the GetBaseUrl operation.</summary>
    public string? GetBaseUrl();
    /// <summary>Executes the GetCanonicalUrl operation.</summary>
    public string? GetCanonicalUrl();
    /// <summary>Executes the GetAlternateUrls operation.</summary>
    public string[] GetAlternateUrls();
    /// <summary>Executes the GetDescription operation.</summary>
    public string? GetDescription();
    /// <summary>Executes the GetSiteName operation.</summary>
    public string? GetSiteName();
    /// <summary>Executes the GetLanguage operation.</summary>
    public string? GetLanguage();
    /// <summary>Executes the GetDirection operation.</summary>
    public string? GetDirection();
    /// <summary>Executes the GetFaviconUrl operation.</summary>
    public string? GetFaviconUrl();
    /// <summary>Executes the GetAuthor operation.</summary>
    public string? GetAuthor();
    /// <summary>Executes the GetPublishedTime operation.</summary>
    public string? GetPublishedTime();
    /// <summary>Executes the GetModifiedTime operation.</summary>
    public string? GetModifiedTime();
    /// <summary>Executes the GetImageUrl operation.</summary>
    public string? GetImageUrl();
    /// <summary>Executes the GetCandidates operation.</summary>
    public HtmlMetadataCandidate[] GetCandidates();
    /// <summary>Executes the GetCandidatesForField operation.</summary>
    public HtmlMetadataCandidate[] GetCandidatesForField(HtmlMetadataField field);
    /// <summary>Executes the GetCandidate operation.</summary>
    public HtmlMetadataCandidate? GetCandidate(HtmlMetadataField field);
    /// <summary>Executes the GetCandidateCount operation.</summary>
    public long GetCandidateCount();
    /// <summary>Executes the GetDroppedCandidateCount operation.</summary>
    public long GetDroppedCandidateCount();
    /// <summary>Executes the GetDuplicateCandidateCount operation.</summary>
    public long GetDuplicateCandidateCount();
    /// <summary>Executes the GetRetainedCandidateCount operation.</summary>
    public int GetRetainedCandidateCount();
    /// <summary>Executes the WereCandidatesTruncated operation.</summary>
    public bool WereCandidatesTruncated();
    /// <summary>Executes the WereRawValuesTruncated operation.</summary>
    public bool WereRawValuesTruncated();
    /// <summary>Reports whether extraction retained incomplete bounded metadata.</summary>
    public bool IsPartial();
    /// <summary>Reports whether cancellation stopped metadata extraction.</summary>
    public bool WasCancelled();
    /// <summary>Gets the stable code explaining incomplete metadata extraction.</summary>
    public HtmlDiagnosticCode GetFailureCode();
    /// <summary>Executes the GetDiagnostics operation.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Gets how many metadata diagnostics were retained.</summary>
    public int GetRetainedDiagnosticCount();
    /// <summary>Executes the GetDiagnosticCount operation.</summary>
    public long GetDiagnosticCount();
    /// <summary>Executes the GetDroppedDiagnosticCount operation.</summary>
    public long GetDroppedDiagnosticCount();
    /// <summary>Executes the WereDiagnosticsTruncated operation.</summary>
    public bool WereDiagnosticsTruncated();
}
