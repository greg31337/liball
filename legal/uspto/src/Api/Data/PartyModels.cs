namespace uspto;

/// <summary>
/// Holds the components and rendered form of a person's name.
/// </summary>
public class PatentPerson
{
    /// <summary>The complete name as supplied by the source.</summary>
    public string? FullName = null;

    /// <summary>The person's first or given name.</summary>
    public string? FirstName = null;

    /// <summary>The person's middle name or initial.</summary>
    public string? MiddleName = null;

    /// <summary>The person's last or family name.</summary>
    public string? LastName = null;

    /// <summary>The person's preferred display name.</summary>
    public string? PreferredName = null;

    /// <summary>A title or prefix appearing before the name.</summary>
    public string? NamePrefix = null;

    /// <summary>A suffix appearing after the name.</summary>
    public string? NameSuffix = null;
}

/// <summary>
/// Holds identifying information for an organization involved with a patent application.
/// </summary>
public class PatentOrganization
{
    /// <summary>The organization name as supplied by the source.</summary>
    public string? Name = null;

    /// <summary>A normalized or standardized organization name.</summary>
    public string? StandardName = null;

    /// <summary>The source identifier assigned to the organization.</summary>
    public string? OrganizationIdentifier = null;

    /// <summary>The organization or legal-entity category.</summary>
    public string? OrganizationCategory = null;
}

/// <summary>
/// Holds a postal or correspondence address without discarding source formatting.
/// </summary>
public class PatentAddress
{
    /// <summary>The first recipient or organization name line.</summary>
    public string? NameLineOne = null;

    /// <summary>The second recipient or organization name line.</summary>
    public string? NameLineTwo = null;

    /// <summary>The first street-address line.</summary>
    public string? AddressLineOne = null;

    /// <summary>The second street-address line.</summary>
    public string? AddressLineTwo = null;

    /// <summary>The third street-address line.</summary>
    public string? AddressLineThree = null;

    /// <summary>The fourth street-address line.</summary>
    public string? AddressLineFour = null;

    /// <summary>The city or locality name.</summary>
    public string? City = null;

    /// <summary>The state, province, or other geographic-region name.</summary>
    public string? GeographicRegionName = null;

    /// <summary>The state, province, or other geographic-region code.</summary>
    public string? GeographicRegionCode = null;

    /// <summary>The postal or ZIP code.</summary>
    public string? PostalCode = null;

    /// <summary>The country code as supplied by the source.</summary>
    public string? CountryCode = null;

    /// <summary>The country name as supplied by the source.</summary>
    public string? CountryName = null;

    /// <summary>The source category for the postal address.</summary>
    public string? PostalAddressCategory = null;

    /// <summary>An email address associated with this address record.</summary>
    public string? EmailAddress = null;
}

/// <summary>
/// Holds one telephone, facsimile, or other telecommunications address.
/// </summary>
public class PatentTelecommunication
{
    /// <summary>The telecommunications number or address.</summary>
    public string? Number = null;

    /// <summary>An extension associated with the number.</summary>
    public string? Extension = null;

    /// <summary>The source code identifying the telecommunications type.</summary>
    public string? TypeCode = null;
}

/// <summary>
/// Holds an applicant and the applicant's application-specific role data.
/// </summary>
public class PatentApplicant
{
    /// <summary>The applicant's person data, when the applicant is an individual.</summary>
    public PatentPerson? Person = null;

    /// <summary>The applicant's organization data, when the applicant is an organization.</summary>
    public PatentOrganization? Organization = null;

    /// <summary>The applicant's known addresses.</summary>
    public PatentAddress[] Addresses = new PatentAddress[0];

    /// <summary>The applicant's country of residence or establishment.</summary>
    public string? ResidenceCountryCode = null;

    /// <summary>The applicant category supplied by the source.</summary>
    public string? ApplicantCategory = null;

    /// <summary>The applicant authority or designation text.</summary>
    public string? Authority = null;

    /// <summary>The applicant's sequence within the application.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;

    /// <summary>Whether the applicant is also an inventor.</summary>
    public bool IsInventor = false;

    /// <summary>Whether <see cref="IsInventor"/> was supplied.</summary>
    public bool HasIsInventor = false;
}

/// <summary>
/// Holds an inventor and the inventor's application-specific role data.
/// </summary>
public class PatentInventor
{
    /// <summary>The inventor's name.</summary>
    public PatentPerson Person = new PatentPerson();

    /// <summary>The inventor's known addresses.</summary>
    public PatentAddress[] Addresses = new PatentAddress[0];

    /// <summary>The inventor's country of residence.</summary>
    public string? ResidenceCountryCode = null;

    /// <summary>The inventor's sequence within the application.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;

    /// <summary>Whether the source identifies the inventor as deceased.</summary>
    public bool IsDeceased = false;

    /// <summary>Whether <see cref="IsDeceased"/> was supplied.</summary>
    public bool HasIsDeceased = false;
}

/// <summary>
/// Holds identifying and organizational information for a patent examiner.
/// </summary>
public class PatentExaminer
{
    /// <summary>The examiner's name.</summary>
    public PatentPerson Person = new PatentPerson();

    /// <summary>The examiner's employee or source identifier.</summary>
    public string? ExaminerIdentifier = null;

    /// <summary>The examiner's group art unit.</summary>
    public string? ArtUnit = null;

    /// <summary>The examiner's technology center.</summary>
    public string? TechnologyCenter = null;

    /// <summary>Whether this is the primary examiner.</summary>
    public bool IsPrimary = false;

    /// <summary>Whether <see cref="IsPrimary"/> was supplied.</summary>
    public bool HasIsPrimary = false;
}

/// <summary>
/// Holds an assignee and the assignee's application-specific role data.
/// </summary>
public class PatentAssignee
{
    /// <summary>The assignee's person data, when the assignee is an individual.</summary>
    public PatentPerson? Person = null;

    /// <summary>The assignee's organization data, when the assignee is an organization.</summary>
    public PatentOrganization? Organization = null;

    /// <summary>The assignee's known addresses.</summary>
    public PatentAddress[] Addresses = new PatentAddress[0];

    /// <summary>The assignee's sequence within the source record.</summary>
    public int Sequence = 0;

    /// <summary>Whether <see cref="Sequence"/> was supplied.</summary>
    public bool HasSequence = false;

    /// <summary>The assignee or ownership category.</summary>
    public string? AssigneeCategory = null;

    /// <summary>The ownership or interest text supplied by the source.</summary>
    public string? OwnershipText = null;
}

/// <summary>
/// Holds a patent application's correspondence destination and contact channels.
/// </summary>
public class PatentCorrespondence
{
    /// <summary>The party to which correspondence is directed.</summary>
    public PatentParty Recipient = new PatentParty();

    /// <summary>The correspondence address.</summary>
    public PatentAddress Address = new PatentAddress();

    /// <summary>Telephone, facsimile, and other contact channels.</summary>
    public PatentTelecommunication[] Telecommunications = new PatentTelecommunication[0];

    /// <summary>The customer number associated with the correspondence address.</summary>
    public string? CustomerNumber = null;

    /// <summary>The application or representative docket number.</summary>
    public string? DocketNumber = null;

    /// <summary>Unmodeled correspondence fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds an attorney or agent and practitioner-registration data.
/// </summary>
public class PatentAttorney
{
    /// <summary>The practitioner's name.</summary>
    public PatentPerson Person = new PatentPerson();

    /// <summary>The practitioner's firm or organization.</summary>
    public PatentOrganization? Organization = null;

    /// <summary>The practitioner's known addresses.</summary>
    public PatentAddress[] Addresses = new PatentAddress[0];

    /// <summary>Telephone, facsimile, and other contact channels.</summary>
    public PatentTelecommunication[] Telecommunications = new PatentTelecommunication[0];

    /// <summary>The USPTO practitioner registration number.</summary>
    public string? RegistrationNumber = null;

    /// <summary>The source category identifying an attorney, agent, or other practitioner.</summary>
    public string? PractitionerCategory = null;

    /// <summary>The practitioner's registration-status text.</summary>
    public string? Status = null;

    /// <summary>The practitioner's country code as supplied by the source.</summary>
    public string? CountryCode = null;

    /// <summary>Whether the practitioner is active.</summary>
    public bool IsActive = false;

    /// <summary>Whether <see cref="IsActive"/> was supplied.</summary>
    public bool HasIsActive = false;

    /// <summary>Whether the practitioner appears in the power-of-attorney list.</summary>
    public bool IsPowerOfAttorney = false;

    /// <summary>Whether <see cref="IsPowerOfAttorney"/> was supplied.</summary>
    public bool HasIsPowerOfAttorney = false;

    /// <summary>Unmodeled practitioner fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Collects the people, organizations, and role records associated with a patent application.
/// </summary>
public class PatentParty
{
    /// <summary>The source identifier for this party collection.</summary>
    public string? PartyIdentifier = null;

    /// <summary>The primary person represented by this party, when applicable.</summary>
    public PatentPerson? Person = null;

    /// <summary>The primary organization represented by this party, when applicable.</summary>
    public PatentOrganization? Organization = null;

    /// <summary>The addresses directly associated with the party.</summary>
    public PatentAddress[] Addresses = new PatentAddress[0];

    /// <summary>The party's telecommunications addresses.</summary>
    public PatentTelecommunication[] Telecommunications = new PatentTelecommunication[0];

    /// <summary>The applicant roles associated with the application.</summary>
    public PatentApplicant[] Applicants = new PatentApplicant[0];

    /// <summary>The inventor roles associated with the application.</summary>
    public PatentInventor[] Inventors = new PatentInventor[0];

    /// <summary>The examiner roles associated with the application.</summary>
    public PatentExaminer[] Examiners = new PatentExaminer[0];

    /// <summary>The assignee roles associated with the application.</summary>
    public PatentAssignee[] Assignees = new PatentAssignee[0];

    /// <summary>The correspondence records associated with the application.</summary>
    public PatentCorrespondence[] Correspondence = new PatentCorrespondence[0];

    /// <summary>The attorney and agent roles associated with the application.</summary>
    public PatentAttorney[] Attorneys = new PatentAttorney[0];
}

/// <summary>
/// Holds the attorneys, agents, and correspondence data returned for one patent application.
/// </summary>
public class PatentAttorneyRecord
{
    /// <summary>The canonical requested patent application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The attorneys and agents associated with the application.</summary>
    public PatentAttorney[] Attorneys = new PatentAttorney[0];

    /// <summary>The correspondence destinations associated with the application.</summary>
    public PatentCorrespondence[] Correspondence = new PatentCorrespondence[0];

    /// <summary>The number of returned attorney and agent records.</summary>
    public int Count = 0;

    /// <summary>The service request identifier associated with the response.</summary>
    public string? RequestIdentifier = null;

    /// <summary>The origin and authority of the attorney record.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>Unmodeled response and wrapper fields retained as raw JSON text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}
