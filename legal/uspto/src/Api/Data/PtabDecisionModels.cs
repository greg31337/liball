namespace uspto;

/// <summary>
/// Holds one decision outcome returned for a PTAB record.
/// </summary>
public class PtabDecision
{
    /// <summary>The decision issue date.</summary>
    public global::System.DateTimeOffset IssueDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="IssueDate"/> was supplied.</summary>
    public bool HasIssueDate = false;

    /// <summary>The decision type category.</summary>
    public string? TypeCategory = null;

    /// <summary>The trial outcome category.</summary>
    public string? TrialOutcomeCategory = null;

    /// <summary>The appeal outcome category, when the decision is shared with an appeal record.</summary>
    public string? AppealOutcomeCategory = null;

    /// <summary>The interference outcome category, when the decision is shared with an interference record.</summary>
    public string? InterferenceOutcomeCategory = null;

    /// <summary>The statutes and rules applied by the decision.</summary>
    public string[] StatutesAndRules = new string[0];

    /// <summary>The issue types considered by the decision.</summary>
    public string[] IssueTypes = new string[0];

    /// <summary>Unmodeled decision fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one PTAB trial decision record and its trial context.
/// </summary>
public class PtabTrialDecision
{
    /// <summary>The trial-decision record category.</summary>
    public string? RecordCategory = null;

    /// <summary>The PTAB trial number.</summary>
    public string? TrialNumber = null;

    /// <summary>The trial type code.</summary>
    public string? TrialTypeCode = null;

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

    /// <summary>The decision document data.</summary>
    public PtabDocument Document = new PtabDocument();

    /// <summary>The decision outcome and authority data.</summary>
    public PtabDecision Decision = new PtabDecision();

    /// <summary>The origin and authority of this trial decision record.</summary>
    public PtabDataProvenance Provenance = new PtabDataProvenance();

    /// <summary>Unmodeled trial decision fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one name and count returned for a PTAB facet.
/// </summary>
public class PtabFacetValue
{
    /// <summary>The facet value name.</summary>
    public string? Name = null;

    /// <summary>The number of matching PTAB records.</summary>
    public long Count = 0L;
}

/// <summary>
/// Holds all returned values for one PTAB facet field.
/// </summary>
public class PtabFacet
{
    /// <summary>The PTAB field that was aggregated.</summary>
    public string? Field = null;

    /// <summary>The returned facet values and counts.</summary>
    public PtabFacetValue[] Values = new PtabFacetValue[0];
}

/// <summary>
/// Holds one page or bounded aggregate of PTAB trial decisions.
/// </summary>
public class PtabTrialDecisionResponse
{
    /// <summary>The search request associated with the response.</summary>
    public SearchRequest Request = new SearchRequest();

    /// <summary>The returned trial decisions.</summary>
    public PtabTrialDecision[] Decisions = new PtabTrialDecision[0];

    /// <summary>The returned facet data.</summary>
    public PtabFacet[] Facets = new PtabFacet[0];

    /// <summary>The number of decisions held in this response.</summary>
    public int Count = 0;

    /// <summary>The service-reported total number of matching decisions.</summary>
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
