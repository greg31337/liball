namespace uspto;

/// <summary>
/// Describes one published Office Action dataset and its discovery endpoint.
/// </summary>
public class OfficeActionDatasetMetadata
{
    /// <summary>The stable dataset name reported by the service.</summary>
    public string? DatasetName = null;

    /// <summary>The published dataset API version.</summary>
    public string? Version = null;

    /// <summary>The publication or availability status reported by the service.</summary>
    public string? Status = null;

    /// <summary>The credential-free dataset field-discovery URI.</summary>
    public global::System.Uri? ServiceUri = null;

    /// <summary>The credential-free schema or API documentation URI.</summary>
    public global::System.Uri? SchemaUri = null;

    /// <summary>The source-provided last-update value preserved without normalization.</summary>
    public string? LastUpdatedText = null;

    /// <summary>The parsed dataset last-update time, when the source value is recognized.</summary>
    public global::System.DateTimeOffset LastUpdatedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastUpdatedAt"/> was parsed successfully.</summary>
    public bool HasLastUpdatedAt = false;
}

/// <summary>
/// Holds searchable field metadata returned by an Office Action dataset.
/// </summary>
public class OfficeActionFieldResponse
{
    /// <summary>The dataset identity, version, status, schema, and update metadata.</summary>
    public OfficeActionDatasetMetadata Metadata = new OfficeActionDatasetMetadata();

    /// <summary>The searchable field names in service order.</summary>
    public string[] Fields = new string[0];

    /// <summary>The number of searchable fields held in this response.</summary>
    public int Count = 0;

    /// <summary>The service request identifier, when supplied.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and retrieval context of the field metadata.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>A bounded retained raw response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Whether the complete field metadata response was processed.</summary>
    public bool IsComplete = false;

    /// <summary>Unmodeled response fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds structured text extracted from the sections of one Office Action.
/// </summary>
public class OfficeActionTextSection
{
    /// <summary>The Office Action identifiers reported by the section data.</summary>
    public string[] OfficeActionIdentifiers = new string[0];

    /// <summary>The patent application numbers reported by the section data.</summary>
    public string[] ApplicationNumbers = new string[0];

    /// <summary>The Office Action summary passages.</summary>
    public string[] SummaryText = new string[0];

    /// <summary>The 35 U.S.C. 101 rejection passages.</summary>
    public string[] Section101RejectionText = new string[0];

    /// <summary>The form paragraphs associated with 35 U.S.C. 101 rejections.</summary>
    public string[] Section101RejectionFormParagraphText = new string[0];

    /// <summary>The 35 U.S.C. 102 rejection passages.</summary>
    public string[] Section102RejectionText = new string[0];

    /// <summary>The form paragraphs associated with 35 U.S.C. 102 rejections.</summary>
    public string[] Section102RejectionFormParagraphText = new string[0];

    /// <summary>The 35 U.S.C. 103 rejection passages.</summary>
    public string[] Section103RejectionText = new string[0];

    /// <summary>The form paragraphs associated with 35 U.S.C. 103 rejections.</summary>
    public string[] Section103RejectionFormParagraphText = new string[0];

    /// <summary>The combined 35 U.S.C. 102 or 103 rejection passages.</summary>
    public string[] Section102And103RejectionText = new string[0];

    /// <summary>The form paragraphs associated with combined 35 U.S.C. 102 or 103 rejections.</summary>
    public string[] Section102And103RejectionFormParagraphText = new string[0];

    /// <summary>The 35 U.S.C. 112 rejection passages.</summary>
    public string[] Section112RejectionText = new string[0];

    /// <summary>The form paragraphs associated with 35 U.S.C. 112 rejections.</summary>
    public string[] Section112RejectionFormParagraphText = new string[0];

    /// <summary>The passages describing withdrawn rejections.</summary>
    public string[] WithdrawalRejectionText = new string[0];

    /// <summary>The terminal-disclaimer status passages.</summary>
    public string[] TerminalDisclaimerStatusText = new string[0];

    /// <summary>The examiner employee numbers reported by the section data.</summary>
    public string[] ExaminerEmployeeNumbers = new string[0];

    /// <summary>The group art unit numbers reported by the section data.</summary>
    public string[] GroupArtUnitNumbers = new string[0];

    /// <summary>The technology center numbers reported by the section data.</summary>
    public string[] TechnologyCenterNumbers = new string[0];

    /// <summary>The work group numbers reported by the section data.</summary>
    public string[] WorkGroupNumbers = new string[0];

    /// <summary>The national class values reported by the section data.</summary>
    public string[] NationalClasses = new string[0];

    /// <summary>The national subclass values reported by the section data.</summary>
    public string[] NationalSubclasses = new string[0];

    /// <summary>The application filing date reported by the section data.</summary>
    public global::System.DateTimeOffset FilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FilingDate"/> was supplied.</summary>
    public bool HasFilingDate = false;

    /// <summary>The patent grant date reported by the section data.</summary>
    public global::System.DateTimeOffset GrantDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="GrantDate"/> was supplied.</summary>
    public bool HasGrantDate = false;

    /// <summary>The Office Action submission date reported by the section data.</summary>
    public global::System.DateTimeOffset SubmissionDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="SubmissionDate"/> was supplied.</summary>
    public bool HasSubmissionDate = false;

    /// <summary>Unmodeled section fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one Office Action text record and its application and examination context.
/// </summary>
public class OfficeActionTextRecord
{
    /// <summary>The source record identifier.</summary>
    public string? Identifier = null;

    /// <summary>The patent application numbers associated with the Office Action.</summary>
    public string[] ApplicationNumbers = new string[0];

    /// <summary>The issued patent numbers associated with the application.</summary>
    public string[] PatentNumbers = new string[0];

    /// <summary>The full Office Action body passages.</summary>
    public string[] BodyText = new string[0];

    /// <summary>The invention titles reported for the application.</summary>
    public string[] InventionTitles = new string[0];

    /// <summary>The application type categories.</summary>
    public string[] ApplicationTypeCategories = new string[0];

    /// <summary>The source application status number.</summary>
    public int ApplicationStatusNumber = 0;

    /// <summary>Whether <see cref="ApplicationStatusNumber"/> was supplied.</summary>
    public bool HasApplicationStatusNumber = false;

    /// <summary>The application confirmation number.</summary>
    public int ApplicationConfirmationNumber = 0;

    /// <summary>Whether <see cref="ApplicationConfirmationNumber"/> was supplied.</summary>
    public bool HasApplicationConfirmationNumber = false;

    /// <summary>The customer number associated with the application.</summary>
    public int CustomerNumber = 0;

    /// <summary>Whether <see cref="CustomerNumber"/> was supplied.</summary>
    public bool HasCustomerNumber = false;

    /// <summary>The application group art unit number.</summary>
    public int GroupArtUnitNumber = 0;

    /// <summary>Whether <see cref="GroupArtUnitNumber"/> was supplied.</summary>
    public bool HasGroupArtUnitNumber = false;

    /// <summary>The examiner employee numbers associated with the Office Action.</summary>
    public string[] ExaminerEmployeeNumbers = new string[0];

    /// <summary>The work group codes associated with the application.</summary>
    public string[] WorkGroups = new string[0];

    /// <summary>The technology center codes associated with the application.</summary>
    public string[] TechnologyCenters = new string[0];

    /// <summary>The national class values associated with the application.</summary>
    public string[] NationalClasses = new string[0];

    /// <summary>The national subclass values associated with the application.</summary>
    public string[] NationalSubclasses = new string[0];

    /// <summary>The legacy document code identifiers.</summary>
    public string[] LegacyDocumentCodeIdentifiers = new string[0];

    /// <summary>The obsolete source document identifiers.</summary>
    public string[] ObsoleteDocumentIdentifiers = new string[0];

    /// <summary>The application filing date.</summary>
    public global::System.DateTimeOffset FilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FilingDate"/> was supplied.</summary>
    public bool HasFilingDate = false;

    /// <summary>The application's effective filing date.</summary>
    public global::System.DateTimeOffset EffectiveFilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="EffectiveFilingDate"/> was supplied.</summary>
    public bool HasEffectiveFilingDate = false;

    /// <summary>The date on which the application was deemed withdrawn.</summary>
    public global::System.DateTimeOffset ApplicationDeemedWithdrawnDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ApplicationDeemedWithdrawnDate"/> was supplied.</summary>
    public bool HasApplicationDeemedWithdrawnDate = false;

    /// <summary>The patent grant date.</summary>
    public global::System.DateTimeOffset GrantDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="GrantDate"/> was supplied.</summary>
    public bool HasGrantDate = false;

    /// <summary>The Office Action submission date.</summary>
    public global::System.DateTimeOffset SubmissionDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="SubmissionDate"/> was supplied.</summary>
    public bool HasSubmissionDate = false;

    /// <summary>The source record's last-modified time.</summary>
    public global::System.DateTimeOffset LastModifiedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedAt"/> was supplied.</summary>
    public bool HasLastModifiedAt = false;

    /// <summary>The structured Office Action section fields.</summary>
    public OfficeActionTextSection Sections = new OfficeActionTextSection();

    /// <summary>Whether any structured section field was supplied.</summary>
    public bool HasSections = false;

    /// <summary>The origin and authority of the Office Action record.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Unmodeled record fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one page or bounded aggregate of Office Action text records.
/// </summary>
public class OfficeActionTextResponse
{
    /// <summary>The search request associated with the response.</summary>
    public SearchRequest Request = new SearchRequest();

    /// <summary>The returned Office Action text records.</summary>
    public OfficeActionTextRecord[] Records = new OfficeActionTextRecord[0];

    /// <summary>The number of records held in this response.</summary>
    public int Count = 0;

    /// <summary>The service-reported number of matching Office Actions.</summary>
    public long Total = 0L;

    /// <summary>Whether <see cref="Total"/> was supplied.</summary>
    public bool HasTotal = false;

    /// <summary>The zero-based service offset of the returned page.</summary>
    public long Start = 0L;

    /// <summary>The requested page coordinates.</summary>
    public OffsetPage Page = new OffsetPage();

    /// <summary>The service request identifier, when supplied.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and retrieval context of the response.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>A bounded retained raw response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Whether the complete matching result set was processed.</summary>
    public bool IsComplete = false;

    /// <summary>Unmodeled response fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds the safe classification and canonical form of a cited reference identifier.
/// </summary>
public class CitedReferenceIdentifier
{
    /// <summary>The complete citation identifier or reference text exactly as supplied by the source.</summary>
    public string? VerbatimText = null;

    /// <summary>The canonical patent identifier when the reference can be normalized without ambiguity.</summary>
    public string? CanonicalPatentIdentifier = null;

    /// <summary>The normalized patent identifier kind, when known.</summary>
    public IdentifierKind PatentIdentifierKind = IdentifierKind.Unknown;

    /// <summary>Whether the source or unambiguous identifier form establishes that this is a patent document.</summary>
    public bool IsPatentDocument = false;

    /// <summary>Whether the source or an explicit citation marker establishes that this is non-patent literature.</summary>
    public bool IsNonPatentLiterature = false;

    /// <summary>Whether the reference was safely classified as a patent document or non-patent literature.</summary>
    public bool IsClassified = false;
}

/// <summary>
/// Holds one reference cited in an Office Action and its prosecution context.
/// </summary>
public class OfficeActionCitation
{
    /// <summary>The source citation record identifier.</summary>
    public string? Identifier = null;

    /// <summary>The patent application number associated with the citation.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The Office Action category in which the reference was cited.</summary>
    public string? ActionTypeCategory = null;

    /// <summary>The statutory legal section associated with the citation.</summary>
    public string? LegalSectionCode = null;

    /// <summary>The source citation text preserved without normalization.</summary>
    public string? ReferenceIdentifier = null;

    /// <summary>The source-parsed patent reference identifier, when supplied.</summary>
    public string? ParsedReferenceIdentifier = null;

    /// <summary>The preserved and safely normalized cited-reference identity.</summary>
    public CitedReferenceIdentifier CitedIdentifier = new CitedReferenceIdentifier();

    /// <summary>The paragraph number associated with the citation.</summary>
    public string? ParagraphNumber = null;

    /// <summary>The group art unit associated with the citation.</summary>
    public string? GroupArtUnitNumber = null;

    /// <summary>The examiner work group associated with the citation.</summary>
    public string? WorkGroup = null;

    /// <summary>The technology center associated with the citation.</summary>
    public string? TechnologyCenter = null;

    /// <summary>Whether the applicant supplied the citation on an examiner-reference form.</summary>
    public bool ApplicantCitedExaminerReferenceIndicator = false;

    /// <summary>Whether <see cref="ApplicantCitedExaminerReferenceIndicator"/> was supplied.</summary>
    public bool HasApplicantCitedExaminerReferenceIndicator = false;

    /// <summary>Whether the examiner cited the reference.</summary>
    public bool ExaminerCitedReferenceIndicator = false;

    /// <summary>Whether <see cref="ExaminerCitedReferenceIndicator"/> was supplied.</summary>
    public bool HasExaminerCitedReferenceIndicator = false;

    /// <summary>Whether the reference was extracted as an Office Action citation.</summary>
    public bool OfficeActionCitationReferenceIndicator = false;

    /// <summary>Whether <see cref="OfficeActionCitationReferenceIndicator"/> was supplied.</summary>
    public bool HasOfficeActionCitationReferenceIndicator = false;

    /// <summary>The source user or process that created the citation record.</summary>
    public string? CreateUserIdentifier = null;

    /// <summary>The time at which the citation record was created.</summary>
    public global::System.DateTimeOffset CreatedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="CreatedAt"/> was supplied.</summary>
    public bool HasCreatedAt = false;

    /// <summary>The legacy document identifier linking the citation to its Office Action.</summary>
    public string? ObsoleteDocumentIdentifier = null;

    /// <summary>The origin and authority of the citation record.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Unmodeled citation fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one page or bounded aggregate of Office Action citations.
/// </summary>
public class OfficeActionCitationResponse
{
    /// <summary>The search request associated with the response.</summary>
    public SearchRequest Request = new SearchRequest();

    /// <summary>The returned Office Action citations.</summary>
    public OfficeActionCitation[] Citations = new OfficeActionCitation[0];

    /// <summary>The number of citations held in this response.</summary>
    public int Count = 0;

    /// <summary>The service-reported number of matching citations.</summary>
    public long Total = 0L;

    /// <summary>Whether <see cref="Total"/> was supplied.</summary>
    public bool HasTotal = false;

    /// <summary>The zero-based service offset of the returned page.</summary>
    public long Start = 0L;

    /// <summary>The requested page coordinates.</summary>
    public OffsetPage Page = new OffsetPage();

    /// <summary>The service request identifier, when supplied.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and retrieval context of the response.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>A bounded retained raw response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Whether the complete matching result set was processed.</summary>
    public bool IsComplete = false;

    /// <summary>Unmodeled response fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one Office Action rejection record, including affected claims and source-derived indicators.
/// </summary>
public class OfficeActionRejection
{
    /// <summary>The source rejection record identifier.</summary>
    public string? Identifier = null;

    /// <summary>The patent application number associated with the rejection.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The legacy document code for the Office Action.</summary>
    public string? LegacyDocumentCodeIdentifier = null;

    /// <summary>The Office Action category in which the rejection appears.</summary>
    public string? ActionTypeCategory = null;

    /// <summary>The statutory legal section associated with the rejection.</summary>
    public string? LegalSectionCode = null;

    /// <summary>The examiner group art unit associated with the rejection.</summary>
    public string? GroupArtUnitNumber = null;

    /// <summary>The national classification assigned to the application.</summary>
    public string? NationalClass = null;

    /// <summary>The national subclass assigned to the application.</summary>
    public string? NationalSubclass = null;

    /// <summary>The paragraph number associated with the rejection.</summary>
    public string? ParagraphNumber = null;

    /// <summary>The legacy document identifier linking the rejection to its Office Action.</summary>
    public string? ObsoleteDocumentIdentifier = null;

    /// <summary>The source user or process that created the rejection record.</summary>
    public string? CreateUserIdentifier = null;

    /// <summary>The individual claim numbers referenced by the rejection record.</summary>
    public string[] ClaimNumbers = new string[0];

    /// <summary>The Office Action submission date.</summary>
    public global::System.DateTimeOffset SubmissionDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="SubmissionDate"/> was supplied.</summary>
    public bool HasSubmissionDate = false;

    /// <summary>The time at which the rejection record was created.</summary>
    public global::System.DateTimeOffset CreatedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="CreatedAt"/> was supplied.</summary>
    public bool HasCreatedAt = false;

    /// <summary>Whether a 35 U.S.C. 101 rejection was identified.</summary>
    public bool HasSection101Rejection = false;

    /// <summary>Whether <see cref="HasSection101Rejection"/> was supplied by the source.</summary>
    public bool HasSection101RejectionState = false;

    /// <summary>Whether a 35 U.S.C. 102 rejection was identified.</summary>
    public bool HasSection102Rejection = false;

    /// <summary>Whether <see cref="HasSection102Rejection"/> was supplied by the source.</summary>
    public bool HasSection102RejectionState = false;

    /// <summary>Whether a 35 U.S.C. 103 rejection was identified.</summary>
    public bool HasSection103Rejection = false;

    /// <summary>Whether <see cref="HasSection103Rejection"/> was supplied by the source.</summary>
    public bool HasSection103RejectionState = false;

    /// <summary>Whether a 35 U.S.C. 112 rejection was identified.</summary>
    public bool HasSection112Rejection = false;

    /// <summary>Whether <see cref="HasSection112Rejection"/> was supplied by the source.</summary>
    public bool HasSection112RejectionState = false;

    /// <summary>Whether a non-statutory double-patenting rejection was identified.</summary>
    public bool HasDoublePatentingRejection = false;

    /// <summary>Whether <see cref="HasDoublePatentingRejection"/> was supplied by the source.</summary>
    public bool HasDoublePatentingRejectionState = false;

    /// <summary>The largest number of references used together in a 35 U.S.C. 103 rejection.</summary>
    public int MaximumSection103CitationCount = 0;

    /// <summary>Whether <see cref="MaximumSection103CitationCount"/> was supplied.</summary>
    public bool HasMaximumSection103CitationCount = false;

    /// <summary>The source indicator that exactly one reference supports a 35 U.S.C. 103 rejection.</summary>
    public int SingleCitationSection103Indicator = 0;

    /// <summary>Whether <see cref="SingleCitationSection103Indicator"/> was supplied.</summary>
    public bool HasSingleCitationSection103Indicator = false;

    /// <summary>The source indicator that more than three references support a 35 U.S.C. 103 rejection.</summary>
    public int MoreThanThreeCitationsSection103Indicator = 0;

    /// <summary>Whether <see cref="MoreThanThreeCitationsSection103Indicator"/> was supplied.</summary>
    public bool HasMoreThanThreeCitationsSection103Indicator = false;

    /// <summary>The source quality indicator for a missing closing paragraph.</summary>
    public int ClosingMissingIndicator = 0;

    /// <summary>Whether <see cref="ClosingMissingIndicator"/> was supplied.</summary>
    public bool HasClosingMissingIndicator = false;

    /// <summary>The source quality indicator for a mismatch between form content and document code.</summary>
    public int RejectionFormMismatchIndicator = 0;

    /// <summary>Whether <see cref="RejectionFormMismatchIndicator"/> was supplied.</summary>
    public bool HasRejectionFormMismatchIndicator = false;

    /// <summary>The source quality indicator for a missing required form paragraph.</summary>
    public int FormParagraphMissingIndicator = 0;

    /// <summary>Whether <see cref="FormParagraphMissingIndicator"/> was supplied.</summary>
    public bool HasFormParagraphMissingIndicator = false;

    /// <summary>The source quality indicator for a missing standard metadata header.</summary>
    public int HeaderMissingIndicator = 0;

    /// <summary>Whether <see cref="HeaderMissingIndicator"/> was supplied.</summary>
    public bool HasHeaderMissingIndicator = false;

    /// <summary>Whether the rejection record references <c>Bilski v. Kappos</c>.</summary>
    public bool BilskiIndicator = false;

    /// <summary>Whether <see cref="BilskiIndicator"/> was supplied.</summary>
    public bool HasBilskiIndicator = false;

    /// <summary>Whether the rejection record references <c>Mayo v. Prometheus</c>.</summary>
    public bool MayoIndicator = false;

    /// <summary>Whether <see cref="MayoIndicator"/> was supplied.</summary>
    public bool HasMayoIndicator = false;

    /// <summary>Whether the Alice and Mayo eligibility framework was identified.</summary>
    public bool AliceIndicator = false;

    /// <summary>Whether <see cref="AliceIndicator"/> was supplied.</summary>
    public bool HasAliceIndicator = false;

    /// <summary>Whether the rejection record references <c>Association for Molecular Pathology v. Myriad Genetics</c>.</summary>
    public bool MyriadIndicator = false;

    /// <summary>Whether <see cref="MyriadIndicator"/> was supplied.</summary>
    public bool HasMyriadIndicator = false;

    /// <summary>Whether the Office Action identifies at least one allowed claim.</summary>
    public bool AllowedClaimIndicator = false;

    /// <summary>Whether <see cref="AllowedClaimIndicator"/> was supplied.</summary>
    public bool HasAllowedClaimIndicator = false;

    /// <summary>The origin and authority of the rejection record.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Unmodeled rejection fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one page or bounded aggregate of Office Action rejection records.
/// </summary>
public class OfficeActionRejectionResponse
{
    /// <summary>The search request associated with the response.</summary>
    public SearchRequest Request = new SearchRequest();

    /// <summary>The returned Office Action rejection records.</summary>
    public OfficeActionRejection[] Rejections = new OfficeActionRejection[0];

    /// <summary>The number of rejection records held in this response.</summary>
    public int Count = 0;

    /// <summary>The service-reported number of matching rejection records.</summary>
    public long Total = 0L;

    /// <summary>Whether <see cref="Total"/> was supplied.</summary>
    public bool HasTotal = false;

    /// <summary>The zero-based service offset of the returned page.</summary>
    public long Start = 0L;

    /// <summary>The requested page coordinates.</summary>
    public OffsetPage Page = new OffsetPage();

    /// <summary>The service request identifier, when supplied.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and retrieval context of the response.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>A bounded retained raw response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Whether the complete matching result set was processed.</summary>
    public bool IsComplete = false;

    /// <summary>Unmodeled response fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one enriched Office Action citation and its normalized patent, claim, and review metadata.
/// </summary>
public class OfficeActionEnrichedCitation
{
    /// <summary>The source enriched-citation record identifier.</summary>
    public string? Identifier = null;

    /// <summary>The patent application number associated with the Office Action.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The cited document identifier supplied by the enrichment source.</summary>
    public string? CitedDocumentIdentifier = null;

    /// <summary>The preserved and safely normalized cited-reference identity.</summary>
    public CitedReferenceIdentifier CitedIdentifier = new CitedReferenceIdentifier();

    /// <summary>The normalized publication number for a cited patent document.</summary>
    public string? PublicationNumber = null;

    /// <summary>The normalized kind code for a cited patent document.</summary>
    public string? KindCode = null;

    /// <summary>The normalized country code for a cited patent document.</summary>
    public string? CountryCode = null;

    /// <summary>The inventor or owner name extracted for the cited document.</summary>
    public string? InventorNameText = null;

    /// <summary>The date of the Office Action containing the citation.</summary>
    public global::System.DateTimeOffset OfficeActionDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="OfficeActionDate"/> was supplied.</summary>
    public bool HasOfficeActionDate = false;

    /// <summary>The category of the Office Action containing the citation.</summary>
    public string? OfficeActionCategory = null;

    /// <summary>The source citation-relevance category code.</summary>
    public string? CitationCategoryCode = null;

    /// <summary>The source text identifying claims related to the citation.</summary>
    public string? RelatedClaimNumberText = null;

    /// <summary>The individual related claim values derived from <see cref="RelatedClaimNumberText"/>.</summary>
    public string[] RelatedClaimNumbers = new string[0];

    /// <summary>Whether the examiner cited the reference.</summary>
    public bool ExaminerCitedReferenceIndicator = false;

    /// <summary>Whether <see cref="ExaminerCitedReferenceIndicator"/> was supplied.</summary>
    public bool HasExaminerCitedReferenceIndicator = false;

    /// <summary>Whether the applicant supplied the citation on an examiner-reference form.</summary>
    public bool ApplicantCitedExaminerReferenceIndicator = false;

    /// <summary>Whether <see cref="ApplicantCitedExaminerReferenceIndicator"/> was supplied.</summary>
    public bool HasApplicantCitedExaminerReferenceIndicator = false;

    /// <summary>Whether the cited reference is non-patent literature.</summary>
    public bool NonPatentLiteratureIndicator = false;

    /// <summary>Whether <see cref="NonPatentLiteratureIndicator"/> was supplied.</summary>
    public bool HasNonPatentLiteratureIndicator = false;

    /// <summary>The examiner work group associated with the Office Action.</summary>
    public string? WorkGroupNumber = null;

    /// <summary>The examiner group art unit associated with the Office Action.</summary>
    public string? GroupArtUnitNumber = null;

    /// <summary>The technology center associated with the Office Action.</summary>
    public string? TechnologyCenter = null;

    /// <summary>The source-provided quality or review summary for the enriched citation.</summary>
    public string? QualitySummaryText = null;

    /// <summary>The source-provided locations of passages related to the citation.</summary>
    public string[] PassageLocations = new string[0];

    /// <summary>The legacy document identifier linking the citation to its Office Action.</summary>
    public string? ObsoleteDocumentIdentifier = null;

    /// <summary>The source user or process that created the enriched-citation record.</summary>
    public string? CreateUserIdentifier = null;

    /// <summary>The time at which the enriched-citation record was created.</summary>
    public global::System.DateTimeOffset CreatedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="CreatedAt"/> was supplied.</summary>
    public bool HasCreatedAt = false;

    /// <summary>The origin and authority of the enriched-citation record.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Unmodeled enriched-citation fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one page or bounded aggregate of enriched Office Action citations.
/// </summary>
public class OfficeActionEnrichedCitationResponse
{
    /// <summary>The search request associated with the response.</summary>
    public SearchRequest Request = new SearchRequest();

    /// <summary>The returned enriched Office Action citations.</summary>
    public OfficeActionEnrichedCitation[] Citations = new OfficeActionEnrichedCitation[0];

    /// <summary>The number of enriched citations held in this response.</summary>
    public int Count = 0;

    /// <summary>The service-reported number of matching enriched citations.</summary>
    public long Total = 0L;

    /// <summary>Whether <see cref="Total"/> was supplied.</summary>
    public bool HasTotal = false;

    /// <summary>The zero-based service offset of the returned page.</summary>
    public long Start = 0L;

    /// <summary>The requested page coordinates.</summary>
    public OffsetPage Page = new OffsetPage();

    /// <summary>The service request identifier, when supplied.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and retrieval context of the response.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>A bounded retained raw response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Whether the complete matching result set was processed.</summary>
    public bool IsComplete = false;

    /// <summary>Unmodeled response fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one chronologically ordered prosecution event and any client-derived source matches.
/// </summary>
public class ProsecutionTimelineEvent
{
    /// <summary>The stable source category: <c>officeAction</c>, <c>transaction</c>, or <c>document</c>.</summary>
    public string? SourceCategory = null;

    /// <summary>The best available source identifier for deterministic ordering and display.</summary>
    public string? EventIdentifier = null;

    /// <summary>The best available official date for the combined event.</summary>
    public global::System.DateTimeOffset EventDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="EventDate"/> was supplied by one of the source records.</summary>
    public bool HasEventDate = false;

    /// <summary>The Office Action record represented by this event, when present.</summary>
    public OfficeActionTextRecord? OfficeAction = null;

    /// <summary>The file-wrapper transaction represented by or matched to this event.</summary>
    public PatentTransaction? Transaction = null;

    /// <summary>The file-wrapper document represented by or matched to this event.</summary>
    public PatentDocument? Document = null;

    /// <summary>The confidence of the client-derived Office Action to transaction match.</summary>
    public ProsecutionMatchConfidence TransactionMatchConfidence = ProsecutionMatchConfidence.None;

    /// <summary>The fields that supported the Office Action to transaction match.</summary>
    public string? TransactionMatchBasis = null;

    /// <summary>The confidence of the client-derived Office Action to document match.</summary>
    public ProsecutionMatchConfidence DocumentMatchConfidence = ProsecutionMatchConfidence.None;

    /// <summary>The fields that supported the Office Action to document match.</summary>
    public string? DocumentMatchBasis = null;

    /// <summary>Whether this event combines records using a client-derived match.</summary>
    public bool IsDerivedMatch = false;

    /// <summary>The provenance of the event's primary official source record.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>The non-authoritative client provenance of a derived match.</summary>
    public PatentDataProvenance DerivationProvenance = new PatentDataProvenance();
}

/// <summary>
/// Holds a combined prosecution timeline for one patent application.
/// </summary>
public class ProsecutionTimeline
{
    /// <summary>The canonical patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The combined prosecution events in deterministic chronological order.</summary>
    public ProsecutionTimelineEvent[] Events = new ProsecutionTimelineEvent[0];

    /// <summary>The number of combined prosecution events.</summary>
    public int Count = 0;

    /// <summary>The number of Office Actions matched to at least one file-wrapper record.</summary>
    public int MatchedOfficeActionCount = 0;

    /// <summary>The number of Office Actions with no derived transaction or document match.</summary>
    public int UnmatchedOfficeActionCount = 0;

    /// <summary>The number of transactions not matched to an Office Action.</summary>
    public int UnmatchedTransactionCount = 0;

    /// <summary>The number of documents not matched to an Office Action.</summary>
    public int UnmatchedDocumentCount = 0;

    /// <summary>The number of source sections loaded successfully.</summary>
    public int LoadedSectionCount = 0;

    /// <summary>The number of source sections that could not be loaded.</summary>
    public int FailedSectionCount = 0;

    /// <summary>The failures encountered while loading independent timeline sources.</summary>
    public SectionError[] Errors = new SectionError[0];

    /// <summary>Whether every source section and the complete Office Action result set were processed.</summary>
    public bool IsComplete = false;

    /// <summary>Whether the result contains useful events alongside one or more source failures.</summary>
    public bool HasPartialSuccess = false;

    /// <summary>The authoritative Office Action dataset provenance.</summary>
    public PatentDataProvenance OfficeActionProvenance = new PatentDataProvenance();

    /// <summary>The authoritative file-wrapper transaction provenance.</summary>
    public PatentDataProvenance TransactionProvenance = new PatentDataProvenance();

    /// <summary>The authoritative file-wrapper document provenance.</summary>
    public PatentDataProvenance DocumentProvenance = new PatentDataProvenance();

    /// <summary>The non-authoritative client provenance of the timeline composition.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();
}
