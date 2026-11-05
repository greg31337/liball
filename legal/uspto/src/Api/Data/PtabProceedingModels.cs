namespace uspto;

/// <summary>
/// Identifies the source and retrieval context of PTAB data.
/// </summary>
public class PtabDataProvenance
{
    /// <summary>The origin and authority category.</summary>
    public DataProvenance Source = DataProvenance.Unknown;

    /// <summary>The PTAB collection from which the record was obtained.</summary>
    public PtabCollection Collection = PtabCollection.TrialProceedings;

    /// <summary>Whether <see cref="Collection"/> was supplied.</summary>
    public bool HasCollection = false;

    /// <summary>The credential-free source URI, when available.</summary>
    public global::System.Uri? SourceUri = null;

    /// <summary>The source dataset or product identifier.</summary>
    public string? DatasetIdentifier = null;

    /// <summary>The source dataset version.</summary>
    public string? DatasetVersion = null;

    /// <summary>The service request identifier associated with the data.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The time at which the data was retrieved.</summary>
    public global::System.DateTimeOffset RetrievedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="RetrievedAt"/> was supplied.</summary>
    public bool HasRetrievedAt = false;

    /// <summary>The source-reported last modification time.</summary>
    public global::System.DateTimeOffset LastModifiedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedAt"/> was supplied.</summary>
    public bool HasLastModifiedAt = false;

    /// <summary>Whether the record came from a response cache.</summary>
    public bool FromCache = false;

    /// <summary>Whether the source is authoritative PTAB data.</summary>
    public bool IsAuthoritative = false;

    /// <summary>The origin and delivery labels for individual fields in composite or cached results.</summary>
    public FieldDataProvenance[] FieldSources = new FieldDataProvenance[0];
}

/// <summary>
/// Holds counsel information reported for a PTAB party.
/// </summary>
public class PtabCounsel
{
    /// <summary>The counsel name as reported by PTAB.</summary>
    public string? Name = null;

    /// <summary>Unmodeled counsel fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds patent and representative information for a PTAB patent owner.
/// </summary>
public class PtabPatentOwner
{
    /// <summary>The associated patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The associated patent number.</summary>
    public string? PatentNumber = null;

    /// <summary>The patent owner name.</summary>
    public string? Name = null;

    /// <summary>The real party in interest.</summary>
    public string? RealPartyInInterestName = null;

    /// <summary>The named inventor.</summary>
    public string? InventorName = null;

    /// <summary>The group art unit number.</summary>
    public string? ArtUnit = null;

    /// <summary>The technology center number.</summary>
    public string? TechnologyCenter = null;

    /// <summary>The patent grant date.</summary>
    public global::System.DateTimeOffset GrantDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="GrantDate"/> was supplied.</summary>
    public bool HasGrantDate = false;

    /// <summary>The counsel reported for the patent owner.</summary>
    public PtabCounsel Counsel = new PtabCounsel();

    /// <summary>Unmodeled patent-owner fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds patent and representative information for a regular or derivation petitioner.
/// </summary>
public class PtabPetitioner
{
    /// <summary>The regular or derivation petitioner category.</summary>
    public string? Category = null;

    /// <summary>The associated patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The associated patent number.</summary>
    public string? PatentNumber = null;

    /// <summary>The patent owner name reported for the challenged patent.</summary>
    public string? PatentOwnerName = null;

    /// <summary>The real party in interest.</summary>
    public string? RealPartyInInterestName = null;

    /// <summary>The named inventor.</summary>
    public string? InventorName = null;

    /// <summary>The group art unit number.</summary>
    public string? ArtUnit = null;

    /// <summary>The technology center number.</summary>
    public string? TechnologyCenter = null;

    /// <summary>The patent grant date.</summary>
    public global::System.DateTimeOffset GrantDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="GrantDate"/> was supplied.</summary>
    public bool HasGrantDate = false;

    /// <summary>The counsel reported for the petitioner.</summary>
    public PtabCounsel Counsel = new PtabCounsel();

    /// <summary>Unmodeled petitioner fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds patent and representative information for a PTAB respondent.
/// </summary>
public class PtabRespondent
{
    /// <summary>The associated patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The associated patent number.</summary>
    public string? PatentNumber = null;

    /// <summary>The patent owner name.</summary>
    public string? PatentOwnerName = null;

    /// <summary>The real party in interest.</summary>
    public string? RealPartyInInterestName = null;

    /// <summary>The named inventor.</summary>
    public string? InventorName = null;

    /// <summary>The group art unit number.</summary>
    public string? ArtUnit = null;

    /// <summary>The technology center number.</summary>
    public string? TechnologyCenter = null;

    /// <summary>The patent grant date.</summary>
    public global::System.DateTimeOffset GrantDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="GrantDate"/> was supplied.</summary>
    public bool HasGrantDate = false;

    /// <summary>The counsel reported for the respondent.</summary>
    public PtabCounsel Counsel = new PtabCounsel();

    /// <summary>Unmodeled respondent fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds lifecycle metadata for one PTAB trial.
/// </summary>
public class PtabTrialMetadata
{
    /// <summary>The trial type code.</summary>
    public string? TrialTypeCode = null;

    /// <summary>The current trial status category.</summary>
    public string? StatusCategory = null;

    /// <summary>The petition filing date.</summary>
    public global::System.DateTimeOffset PetitionFilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="PetitionFilingDate"/> was supplied.</summary>
    public bool HasPetitionFilingDate = false;

    /// <summary>The accorded filing date.</summary>
    public global::System.DateTimeOffset AccordedFilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="AccordedFilingDate"/> was supplied.</summary>
    public bool HasAccordedFilingDate = false;

    /// <summary>The institution decision date.</summary>
    public global::System.DateTimeOffset InstitutionDecisionDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="InstitutionDecisionDate"/> was supplied.</summary>
    public bool HasInstitutionDecisionDate = false;

    /// <summary>The latest decision date.</summary>
    public global::System.DateTimeOffset LatestDecisionDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LatestDecisionDate"/> was supplied.</summary>
    public bool HasLatestDecisionDate = false;

    /// <summary>The trial termination date.</summary>
    public global::System.DateTimeOffset TerminationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="TerminationDate"/> was supplied.</summary>
    public bool HasTerminationDate = false;

    /// <summary>The source-reported trial modification time.</summary>
    public global::System.DateTimeOffset LastModifiedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedAt"/> was supplied.</summary>
    public bool HasLastModifiedAt = false;

    /// <summary>The separately reported trial modification date.</summary>
    public global::System.DateTimeOffset LastModifiedDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedDate"/> was supplied.</summary>
    public bool HasLastModifiedDate = false;

    /// <summary>Unmodeled trial metadata fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one PTAB trial proceeding and its participating parties.
/// </summary>
public class PtabTrialProceeding
{
    /// <summary>The PTAB trial number.</summary>
    public string? TrialNumber = null;

    /// <summary>The source-reported record modification time.</summary>
    public global::System.DateTimeOffset LastModifiedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastModifiedAt"/> was supplied.</summary>
    public bool HasLastModifiedAt = false;

    /// <summary>The trial lifecycle metadata.</summary>
    public PtabTrialMetadata Metadata = new PtabTrialMetadata();

    /// <summary>The challenged patent owner data.</summary>
    public PtabPatentOwner PatentOwner = new PtabPatentOwner();

    /// <summary>The regular petitioner data.</summary>
    public PtabPetitioner RegularPetitioner = new PtabPetitioner();

    /// <summary>The respondent data.</summary>
    public PtabRespondent Respondent = new PtabRespondent();

    /// <summary>The derivation petitioner data.</summary>
    public PtabPetitioner DerivationPetitioner = new PtabPetitioner();

    /// <summary>The origin and authority of this proceeding.</summary>
    public PtabDataProvenance Provenance = new PtabDataProvenance();

    /// <summary>Unmodeled proceeding fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one page or bounded aggregate of PTAB trial proceedings.
/// </summary>
public class PtabTrialProceedingResponse
{
    /// <summary>The search request associated with the response.</summary>
    public SearchRequest Request = new SearchRequest();

    /// <summary>The returned trial proceedings.</summary>
    public PtabTrialProceeding[] Proceedings = new PtabTrialProceeding[0];

    /// <summary>The returned facet data.</summary>
    public PtabFacet[] Facets = new PtabFacet[0];

    /// <summary>The number of proceedings held in this response.</summary>
    public int Count = 0;

    /// <summary>The service-reported total number of matching proceedings.</summary>
    public long Total = 0L;

    /// <summary>Whether <see cref="Total"/> was supplied.</summary>
    public bool HasTotal = false;

    /// <summary>The offset-page coordinates associated with the response.</summary>
    public OffsetPage Page = new OffsetPage();

    /// <summary>The service request identifier.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and authority of the response.</summary>
    public PtabDataProvenance Provenance = new PtabDataProvenance();

    /// <summary>A bounded retained raw response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Whether all requested pages were processed.</summary>
    public bool IsComplete = false;
}
