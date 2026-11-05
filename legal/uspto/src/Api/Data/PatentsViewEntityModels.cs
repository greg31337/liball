namespace uspto;

/// <summary>
/// Holds an entity's patent activity for one calendar year.
/// </summary>
public class PatentsViewEntityActivityYear
{
    /// <summary>The calendar year.</summary>
    public int Year = 0;

    /// <summary>Whether <see cref="Year"/> was supplied.</summary>
    public bool HasYear = false;

    /// <summary>The number of patents recorded in the year.</summary>
    public int PatentCount = 0;

    /// <summary>Whether <see cref="PatentCount"/> was supplied.</summary>
    public bool HasPatentCount = false;
}

/// <summary>
/// Maps a source-specific entity identifier to a persistent disambiguated identifier.
/// </summary>
public class PatentsViewEntityCrosswalk
{
    /// <summary>The inventor, assignee, applicant, attorney, or other entity category.</summary>
    public string? EntityType = null;

    /// <summary>The identifier assigned by the source table or schema generation.</summary>
    public string? SourceEntityIdentifier = null;

    /// <summary>The persistent PatentsView entity identifier.</summary>
    public string? PersistentEntityIdentifier = null;

    /// <summary>The disambiguation or crosswalk version.</summary>
    public string? DisambiguationVersion = null;

    /// <summary>The beginning of the mapping's validity window.</summary>
    public global::System.DateTimeOffset ValidFrom = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ValidFrom"/> was supplied.</summary>
    public bool HasValidFrom = false;

    /// <summary>The end of the mapping's validity window.</summary>
    public global::System.DateTimeOffset ValidTo = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ValidTo"/> was supplied.</summary>
    public bool HasValidTo = false;

    /// <summary>Whether this is the current mapping for the source identifier.</summary>
    public bool IsCurrent = false;

    /// <summary>Whether <see cref="IsCurrent"/> was supplied.</summary>
    public bool HasIsCurrent = false;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled crosswalk fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Links a granted patent, application, and pre-grant publication without coercing their identifiers.
/// </summary>
public class PatentsViewGrantPublicationCrosswalk
{
    /// <summary>The granted patent identifier.</summary>
    public string? PatentIdentifier = null;

    /// <summary>The application identifier.</summary>
    public string? ApplicationIdentifier = null;

    /// <summary>The pre-grant publication document number.</summary>
    public string? DocumentNumber = null;

    /// <summary>Whether the document number is current for the application.</summary>
    public bool IsCurrentDocumentNumber = false;

    /// <summary>Whether <see cref="IsCurrentDocumentNumber"/> was supplied.</summary>
    public bool HasIsCurrentDocumentNumber = false;

    /// <summary>Whether the patent identifier is current for the publication.</summary>
    public bool IsCurrentPatentIdentifier = false;

    /// <summary>Whether <see cref="IsCurrentPatentIdentifier"/> was supplied.</summary>
    public bool HasIsCurrentPatentIdentifier = false;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled crosswalk fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one applicant or applicant-inventor association reported by PatentsView.
/// </summary>
public class PatentsViewApplicant
{
    /// <summary>The persistent or source applicant identifier, when supplied.</summary>
    public string? ApplicantIdentifier = null;

    /// <summary>The related patent identifier.</summary>
    public string? PatentIdentifier = null;

    /// <summary>The related pre-grant publication document number.</summary>
    public string? DocumentNumber = null;

    /// <summary>The related application identifier.</summary>
    public string? ApplicationIdentifier = null;

    /// <summary>The applicant's given name.</summary>
    public string? FirstName = null;

    /// <summary>The applicant's family name.</summary>
    public string? LastName = null;

    /// <summary>The applicant organization name.</summary>
    public string? Organization = null;

    /// <summary>The applicant or applicant-inventor category.</summary>
    public string? ApplicantType = null;

    /// <summary>The source applicant designation.</summary>
    public string? Designation = null;

    /// <summary>The applicant authority reported for a publication party.</summary>
    public string? Authority = null;

    /// <summary>The associated PatentsView location identifier.</summary>
    public string? LocationIdentifier = null;

    /// <summary>The applicant's order on the application.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled applicant fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one disambiguated PatentsView geographic location.
/// </summary>
public class PatentsViewLocation
{
    /// <summary>The persistent PatentsView location identifier.</summary>
    public string? LocationIdentifier = null;

    /// <summary>The city or place name.</summary>
    public string? Name = null;

    /// <summary>The county name.</summary>
    public string? County = null;

    /// <summary>The county FIPS code, retained as identifier text.</summary>
    public string? CountyFipsCode = null;

    /// <summary>The state or region name.</summary>
    public string? State = null;

    /// <summary>The state FIPS code, retained as identifier text.</summary>
    public string? StateFipsCode = null;

    /// <summary>The country name or code.</summary>
    public string? Country = null;

    /// <summary>The source place category.</summary>
    public string? PlaceType = null;

    /// <summary>The geographic latitude.</summary>
    public double Latitude = 0.0;

    /// <summary>Whether <see cref="Latitude"/> was supplied.</summary>
    public bool HasLatitude = false;

    /// <summary>The geographic longitude.</summary>
    public double Longitude = 0.0;

    /// <summary>Whether <see cref="Longitude"/> was supplied.</summary>
    public bool HasLongitude = false;

    /// <summary>The number of associated assignees.</summary>
    public int AssigneeCount = 0;

    /// <summary>Whether <see cref="AssigneeCount"/> was supplied.</summary>
    public bool HasAssigneeCount = false;

    /// <summary>The number of associated patents.</summary>
    public int PatentCount = 0;

    /// <summary>Whether <see cref="PatentCount"/> was supplied.</summary>
    public bool HasPatentCount = false;

    /// <summary>The number of associated inventors.</summary>
    public int InventorCount = 0;

    /// <summary>Whether <see cref="InventorCount"/> was supplied.</summary>
    public bool HasInventorCount = false;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled location fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one disambiguated PatentsView inventor and research activity summary.
/// </summary>
public class PatentsViewInventor
{
    /// <summary>The persistent PatentsView inventor identifier.</summary>
    public string? InventorIdentifier = null;

    /// <summary>The API resource reference supplied for the inventor.</summary>
    public string? ResourceReference = null;

    /// <summary>The inventor's given name.</summary>
    public string? FirstName = null;

    /// <summary>The inventor's family name.</summary>
    public string? LastName = null;

    /// <summary>The source gender code.</summary>
    public string? GenderCode = null;

    /// <summary>The inventor's last-known location.</summary>
    public PatentsViewLocation? LastKnownLocation = null;

    /// <summary>The first patent date on which the inventor was recorded.</summary>
    public global::System.DateTimeOffset FirstSeenDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FirstSeenDate"/> was supplied.</summary>
    public bool HasFirstSeenDate = false;

    /// <summary>The most recent patent date on which the inventor was recorded.</summary>
    public global::System.DateTimeOffset LastSeenDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastSeenDate"/> was supplied.</summary>
    public bool HasLastSeenDate = false;

    /// <summary>The number of associated assignees.</summary>
    public int AssigneeCount = 0;

    /// <summary>Whether <see cref="AssigneeCount"/> was supplied.</summary>
    public bool HasAssigneeCount = false;

    /// <summary>The number of associated patents.</summary>
    public int PatentCount = 0;

    /// <summary>Whether <see cref="PatentCount"/> was supplied.</summary>
    public bool HasPatentCount = false;

    /// <summary>The number of active calendar years.</summary>
    public int YearsActive = 0;

    /// <summary>Whether <see cref="YearsActive"/> was supplied.</summary>
    public bool HasYearsActive = false;

    /// <summary>The inventor's patent activity by calendar year.</summary>
    public PatentsViewEntityActivityYear[] ActivityYears = new PatentsViewEntityActivityYear[0];

    /// <summary>The source-to-persistent identifier mappings for this inventor.</summary>
    public PatentsViewEntityCrosswalk[] Crosswalks = new PatentsViewEntityCrosswalk[0];

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled inventor fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one disambiguated PatentsView assignee and research activity summary.
/// </summary>
public class PatentsViewAssignee
{
    /// <summary>The persistent PatentsView assignee identifier.</summary>
    public string? AssigneeIdentifier = null;

    /// <summary>The API resource reference supplied for the assignee.</summary>
    public string? ResourceReference = null;

    /// <summary>The assignee's given name when the assignee is an individual.</summary>
    public string? FirstName = null;

    /// <summary>The assignee's family name when the assignee is an individual.</summary>
    public string? LastName = null;

    /// <summary>The assignee organization name.</summary>
    public string? Organization = null;

    /// <summary>The source assignee category.</summary>
    public string? AssigneeType = null;

    /// <summary>The assignee's last-known location.</summary>
    public PatentsViewLocation? LastKnownLocation = null;

    /// <summary>The first patent date on which the assignee was recorded.</summary>
    public global::System.DateTimeOffset FirstSeenDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FirstSeenDate"/> was supplied.</summary>
    public bool HasFirstSeenDate = false;

    /// <summary>The most recent patent date on which the assignee was recorded.</summary>
    public global::System.DateTimeOffset LastSeenDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastSeenDate"/> was supplied.</summary>
    public bool HasLastSeenDate = false;

    /// <summary>The number of associated inventors.</summary>
    public int InventorCount = 0;

    /// <summary>Whether <see cref="InventorCount"/> was supplied.</summary>
    public bool HasInventorCount = false;

    /// <summary>The number of associated patents.</summary>
    public int PatentCount = 0;

    /// <summary>Whether <see cref="PatentCount"/> was supplied.</summary>
    public bool HasPatentCount = false;

    /// <summary>The number of active calendar years.</summary>
    public int YearsActive = 0;

    /// <summary>Whether <see cref="YearsActive"/> was supplied.</summary>
    public bool HasYearsActive = false;

    /// <summary>The assignee's patent activity by calendar year.</summary>
    public PatentsViewEntityActivityYear[] ActivityYears = new PatentsViewEntityActivityYear[0];

    /// <summary>The source-to-persistent identifier mappings for this assignee.</summary>
    public PatentsViewEntityCrosswalk[] Crosswalks = new PatentsViewEntityCrosswalk[0];

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled assignee fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one disambiguated PatentsView patent attorney and research activity summary.
/// </summary>
public class PatentsViewAttorney
{
    /// <summary>The persistent PatentsView attorney identifier.</summary>
    public string? AttorneyIdentifier = null;

    /// <summary>The attorney's given name.</summary>
    public string? FirstName = null;

    /// <summary>The attorney's family name.</summary>
    public string? LastName = null;

    /// <summary>The attorney organization name.</summary>
    public string? Organization = null;

    /// <summary>The first patent date on which the attorney was recorded.</summary>
    public global::System.DateTimeOffset FirstSeenDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FirstSeenDate"/> was supplied.</summary>
    public bool HasFirstSeenDate = false;

    /// <summary>The most recent patent date on which the attorney was recorded.</summary>
    public global::System.DateTimeOffset LastSeenDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="LastSeenDate"/> was supplied.</summary>
    public bool HasLastSeenDate = false;

    /// <summary>The number of associated inventors.</summary>
    public int InventorCount = 0;

    /// <summary>Whether <see cref="InventorCount"/> was supplied.</summary>
    public bool HasInventorCount = false;

    /// <summary>The number of associated patents.</summary>
    public int PatentCount = 0;

    /// <summary>Whether <see cref="PatentCount"/> was supplied.</summary>
    public bool HasPatentCount = false;

    /// <summary>The number of active calendar years.</summary>
    public int YearsActive = 0;

    /// <summary>Whether <see cref="YearsActive"/> was supplied.</summary>
    public bool HasYearsActive = false;

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled attorney fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one federal organization associated with a government-interest statement.
/// </summary>
public class PatentsViewGovernmentOrganization
{
    /// <summary>The focal federal agency name.</summary>
    public string? FederalAgencyName = null;

    /// <summary>The top-level department or agency name.</summary>
    public string? LevelOne = null;

    /// <summary>The second-level agency or bureau name.</summary>
    public string? LevelTwo = null;

    /// <summary>The third-level agency or office name.</summary>
    public string? LevelThree = null;
}

/// <summary>
/// Holds a PatentsView government-interest statement, awards, and federal organizations.
/// </summary>
public class PatentsViewGovernmentInterest
{
    /// <summary>The related patent identifier.</summary>
    public string? PatentIdentifier = null;

    /// <summary>The related pre-grant publication document number.</summary>
    public string? DocumentNumber = null;

    /// <summary>The complete government-interest statement.</summary>
    public string? Statement = null;

    /// <summary>The federal contract, grant, or award numbers.</summary>
    public string[] AwardNumbers = new string[0];

    /// <summary>The federal organizations extracted from the statement.</summary>
    public PatentsViewGovernmentOrganization[] Organizations = new PatentsViewGovernmentOrganization[0];

    /// <summary>The research source and retrieval context.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance
    {
        Source = DataProvenance.PatentsView,
        IsAuthoritative = false
    };

    /// <summary>Unmodeled government-interest fields retained as raw scalar text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> AdditionalFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView inventors.
/// </summary>
public class PatentsViewInventorResponse
{
    /// <summary>The returned inventor records in stable query order.</summary>
    public PatentsViewInventor[] Inventors = new PatentsViewInventor[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView assignees.
/// </summary>
public class PatentsViewAssigneeResponse
{
    /// <summary>The returned assignee records in stable query order.</summary>
    public PatentsViewAssignee[] Assignees = new PatentsViewAssignee[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView locations.
/// </summary>
public class PatentsViewLocationResponse
{
    /// <summary>The returned location records in stable query order.</summary>
    public PatentsViewLocation[] Locations = new PatentsViewLocation[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}

/// <summary>
/// Holds one page or bounded aggregate of PatentsView patent attorneys.
/// </summary>
public class PatentsViewAttorneyResponse
{
    /// <summary>The returned attorney records in stable query order.</summary>
    public PatentsViewAttorney[] Attorneys = new PatentsViewAttorney[0];

    /// <summary>The query, counts, cursor, raw response, and research provenance.</summary>
    public PatentsViewResponseMetadata Metadata = new PatentsViewResponseMetadata();
}
