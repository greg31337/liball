namespace uspto;

/// <summary>
/// Holds a postal address associated with a trademark party or correspondent.
/// </summary>
public class TrademarkAddress
{
    /// <summary>The first address line.</summary>
    public string? Line1 = null;

    /// <summary>The second address line.</summary>
    public string? Line2 = null;

    /// <summary>The city or locality.</summary>
    public string? City = null;

    /// <summary>The state, province, or other first-level region.</summary>
    public string? StateOrProvince = null;

    /// <summary>The postal code.</summary>
    public string? PostalCode = null;

    /// <summary>The source country code.</summary>
    public string? CountryCode = null;

    /// <summary>The source country name.</summary>
    public string? CountryName = null;

    /// <summary>Unmodeled address fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds a current or prior owner reported for a trademark case.
/// </summary>
public class TrademarkOwner
{
    /// <summary>The source sequence number.</summary>
    public int SequenceNumber = 0;

    /// <summary>Whether <see cref="SequenceNumber"/> was supplied.</summary>
    public bool HasSequenceNumber = false;

    /// <summary>The numeric party type code.</summary>
    public int PartyTypeCode = 0;

    /// <summary>Whether <see cref="PartyTypeCode"/> was supplied.</summary>
    public bool HasPartyTypeCode = false;

    /// <summary>The party type description.</summary>
    public string? PartyTypeDescription = null;

    /// <summary>The owner name.</summary>
    public string? Name = null;

    /// <summary>The source entity type code.</summary>
    public string? EntityTypeCode = null;

    /// <summary>The source entity type description.</summary>
    public string? EntityTypeDescription = null;

    /// <summary>Doing-business-as, also-known-as, or former-name text.</summary>
    public string? AlternateNameText = null;

    /// <summary>Text describing the entities that compose the owner.</summary>
    public string? CompositionText = null;

    /// <summary>The assignment reel and frame associated with the owner.</summary>
    public string? AssignmentReelFrame = null;

    /// <summary>The owner's citizenship country code.</summary>
    public string? CitizenshipCountryCode = null;

    /// <summary>The owner's citizenship country name.</summary>
    public string? CitizenshipCountryName = null;

    /// <summary>The owner's postal address.</summary>
    public TrademarkAddress Address = new TrademarkAddress();

    /// <summary>The owner's email addresses.</summary>
    public string[] EmailAddresses = new string[0];

    /// <summary>Whether the source identifies this as a current owner.</summary>
    public bool IsCurrent = false;

    /// <summary>Whether <see cref="IsCurrent"/> was supplied.</summary>
    public bool HasIsCurrent = false;

    /// <summary>Unmodeled owner fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds attorney or representative information for a trademark case.
/// </summary>
public class TrademarkAttorney
{
    /// <summary>The source employee or attorney identifier.</summary>
    public string? EmployeeNumber = null;

    /// <summary>The attorney's first name.</summary>
    public string? FirstName = null;

    /// <summary>The attorney's middle name or initial.</summary>
    public string? MiddleName = null;

    /// <summary>The attorney's last name.</summary>
    public string? LastName = null;

    /// <summary>The attorney's name suffix.</summary>
    public string? NameSuffix = null;

    /// <summary>The complete attorney name as supplied.</summary>
    public string? Name = null;

    /// <summary>The attorney's organization.</summary>
    public string? Organization = null;

    /// <summary>The attorney docket number.</summary>
    public string? DocketNumber = null;

    /// <summary>The attorney's phone numbers.</summary>
    public string[] PhoneNumbers = new string[0];

    /// <summary>The attorney's email addresses.</summary>
    public string[] EmailAddresses = new string[0];

    /// <summary>The attorney's postal address.</summary>
    public TrademarkAddress Address = new TrademarkAddress();

    /// <summary>Unmodeled attorney fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds the correspondence contact and address for a trademark case.
/// </summary>
public class TrademarkCorrespondence
{
    /// <summary>The correspondence contact name.</summary>
    public string? Name = null;

    /// <summary>The correspondence organization.</summary>
    public string? Organization = null;

    /// <summary>The correspondence email addresses.</summary>
    public string[] EmailAddresses = new string[0];

    /// <summary>The correspondence phone numbers.</summary>
    public string[] PhoneNumbers = new string[0];

    /// <summary>The correspondence postal address.</summary>
    public TrademarkAddress Address = new TrademarkAddress();

    /// <summary>Unmodeled correspondence fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one design-search classification assigned to a trademark mark.
/// </summary>
public class TrademarkDesignSearchClassification
{
    /// <summary>The design-search code.</summary>
    public string? Code = null;

    /// <summary>The descriptions associated with the design-search code.</summary>
    public string[] Descriptions = new string[0];

    /// <summary>Unmodeled design-search fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds verbal, drawing, color, and design metadata for a trademark mark.
/// </summary>
public class TrademarkMark
{
    /// <summary>The verbal element or word-mark text.</summary>
    public string? VerbalElementText = null;

    /// <summary>The source mark type.</summary>
    public string? Type = null;

    /// <summary>The mark drawing code.</summary>
    public string? DrawingCode = null;

    /// <summary>The mark drawing description.</summary>
    public string? DrawingDescription = null;

    /// <summary>The source mark feature code.</summary>
    public string? FeatureCode = null;

    /// <summary>The source mark feature description.</summary>
    public string? FeatureDescription = null;

    /// <summary>Whether the mark uses standard characters.</summary>
    public bool IsStandardCharacter = false;

    /// <summary>Whether <see cref="IsStandardCharacter"/> was supplied.</summary>
    public bool HasIsStandardCharacter = false;

    /// <summary>Whether the current drawing claims color.</summary>
    public bool IsColorDrawing = false;

    /// <summary>Whether <see cref="IsColorDrawing"/> was supplied.</summary>
    public bool HasIsColorDrawing = false;

    /// <summary>The colors claimed as a feature of the mark.</summary>
    public string? ColorClaimText = null;

    /// <summary>The locations in which claimed colors appear.</summary>
    public string? ColorLocationText = null;

    /// <summary>The narrative mark description.</summary>
    public string? Description = null;

    /// <summary>The disclaimer text.</summary>
    public string? DisclaimerText = null;

    /// <summary>The pseudo-mark text used for searching.</summary>
    public string? PseudoMarkText = null;

    /// <summary>The translation of wording in the mark.</summary>
    public string? TranslationText = null;

    /// <summary>The transliteration of non-Latin wording in the mark.</summary>
    public string? TransliterationText = null;

    /// <summary>The consent statement for a name or portrait in the mark.</summary>
    public string? NamePortraitConsentText = null;

    /// <summary>The lining or stippling statement.</summary>
    public string? LiningStipplingText = null;

    /// <summary>The design-search classifications assigned to the mark.</summary>
    public TrademarkDesignSearchClassification[] DesignSearchClassifications = new TrademarkDesignSearchClassification[0];

    /// <summary>The source sound-file location.</summary>
    public string? SoundFile = null;

    /// <summary>The source multimedia-file locations.</summary>
    public string[] MultimediaFiles = new string[0];

    /// <summary>Unmodeled mark fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds filing-basis indicators associated with a trademark case or class.
/// </summary>
public class TrademarkFilingBasis
{
    /// <summary>Whether use in commerce under Section 1(a) applies.</summary>
    public bool Section1A = false;

    /// <summary>Whether <see cref="Section1A"/> was supplied.</summary>
    public bool HasSection1A = false;

    /// <summary>Whether intent to use under Section 1(b) applies.</summary>
    public bool Section1B = false;

    /// <summary>Whether <see cref="Section1B"/> was supplied.</summary>
    public bool HasSection1B = false;

    /// <summary>Whether foreign priority under Section 44(d) applies.</summary>
    public bool Section44D = false;

    /// <summary>Whether <see cref="Section44D"/> was supplied.</summary>
    public bool HasSection44D = false;

    /// <summary>Whether foreign registration under Section 44(e) applies.</summary>
    public bool Section44E = false;

    /// <summary>Whether <see cref="Section44E"/> was supplied.</summary>
    public bool HasSection44E = false;

    /// <summary>Whether a Madrid Protocol request under Section 66(a) applies.</summary>
    public bool Section66A = false;

    /// <summary>Whether <see cref="Section66A"/> was supplied.</summary>
    public bool HasSection66A = false;

    /// <summary>Unmodeled filing-basis fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds the current status, lifecycle dates, register, and case-location data.
/// </summary>
public class TrademarkStatus
{
    /// <summary>The numeric current status code.</summary>
    public int Code = 0;

    /// <summary>Whether <see cref="Code"/> was supplied.</summary>
    public bool HasCode = false;

    /// <summary>The current status code preserved as text.</summary>
    public string? CodeText = null;

    /// <summary>The public-facing status description.</summary>
    public string? ExternalDescription = null;

    /// <summary>The internal source status description.</summary>
    public string? InternalDescription = null;

    /// <summary>The TM5 common status code.</summary>
    public int Tm5Code = 0;

    /// <summary>Whether <see cref="Tm5Code"/> was supplied.</summary>
    public bool HasTm5Code = false;

    /// <summary>The TM5 status description.</summary>
    public string? Tm5Description = null;

    /// <summary>The TM5 status definition.</summary>
    public string? Tm5Definition = null;

    /// <summary>The parsed current status date.</summary>
    public global::System.DateTimeOffset StatusDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="StatusDate"/> was parsed successfully.</summary>
    public bool HasStatusDate = false;

    /// <summary>The current status date exactly as supplied.</summary>
    public string? StatusDateText = null;

    /// <summary>The parsed application filing date.</summary>
    public global::System.DateTimeOffset FilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FilingDate"/> was parsed successfully.</summary>
    public bool HasFilingDate = false;

    /// <summary>The application filing date exactly as supplied.</summary>
    public string? FilingDateText = null;

    /// <summary>The parsed United States registration date.</summary>
    public global::System.DateTimeOffset RegistrationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="RegistrationDate"/> was parsed successfully.</summary>
    public bool HasRegistrationDate = false;

    /// <summary>The registration date exactly as supplied.</summary>
    public string? RegistrationDateText = null;

    /// <summary>The parsed abandonment date.</summary>
    public global::System.DateTimeOffset AbandonmentDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="AbandonmentDate"/> was parsed successfully.</summary>
    public bool HasAbandonmentDate = false;

    /// <summary>The abandonment date exactly as supplied.</summary>
    public string? AbandonmentDateText = null;

    /// <summary>The parsed cancellation date.</summary>
    public global::System.DateTimeOffset CancellationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="CancellationDate"/> was parsed successfully.</summary>
    public bool HasCancellationDate = false;

    /// <summary>The cancellation date exactly as supplied.</summary>
    public string? CancellationDateText = null;

    /// <summary>The parsed renewal date.</summary>
    public global::System.DateTimeOffset RenewalDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="RenewalDate"/> was parsed successfully.</summary>
    public bool HasRenewalDate = false;

    /// <summary>The renewal date exactly as supplied.</summary>
    public string? RenewalDateText = null;

    /// <summary>The source register category.</summary>
    public string? RegisterCategory = null;

    /// <summary>The current law office assignment code.</summary>
    public string? LawOfficeCode = null;

    /// <summary>The current case location code.</summary>
    public string? CurrentLocationCode = null;

    /// <summary>The current case location description.</summary>
    public string? CurrentLocationDescription = null;

    /// <summary>The number of active classes.</summary>
    public int ActiveClassCount = 0;

    /// <summary>Whether <see cref="ActiveClassCount"/> was supplied.</summary>
    public bool HasActiveClassCount = false;

    /// <summary>The total number of classes.</summary>
    public int TotalClassCount = 0;

    /// <summary>Whether <see cref="TotalClassCount"/> was supplied.</summary>
    public bool HasTotalClassCount = false;

    /// <summary>Unmodeled status fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one prosecution-history event for a trademark case.
/// </summary>
public class TrademarkProsecutionEvent
{
    /// <summary>The source entry or action code.</summary>
    public string? Code = null;

    /// <summary>The source entry type.</summary>
    public string? Type = null;

    /// <summary>The human-readable event description.</summary>
    public string? Description = null;

    /// <summary>The source entry number.</summary>
    public int EntryNumber = 0;

    /// <summary>Whether <see cref="EntryNumber"/> was supplied.</summary>
    public bool HasEntryNumber = false;

    /// <summary>The source sequence number.</summary>
    public int SequenceNumber = 0;

    /// <summary>Whether <see cref="SequenceNumber"/> was supplied.</summary>
    public bool HasSequenceNumber = false;

    /// <summary>The associated proceeding number.</summary>
    public string? ProceedingNumber = null;

    /// <summary>The associated source document identifier.</summary>
    public string? DocumentIdentifier = null;

    /// <summary>The credential-free associated document URI.</summary>
    public global::System.Uri? DocumentUri = null;

    /// <summary>Whether the source marks the event as confidential.</summary>
    public bool IsConfidential = false;

    /// <summary>Whether <see cref="IsConfidential"/> was supplied.</summary>
    public bool HasIsConfidential = false;

    /// <summary>The parsed event date.</summary>
    public global::System.DateTimeOffset EventDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="EventDate"/> was parsed successfully.</summary>
    public bool HasEventDate = false;

    /// <summary>The event date exactly as supplied.</summary>
    public string? EventDateText = null;

    /// <summary>The parsed response due date.</summary>
    public global::System.DateTimeOffset DueDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="DueDate"/> was parsed successfully.</summary>
    public bool HasDueDate = false;

    /// <summary>The response due date exactly as supplied.</summary>
    public string? DueDateText = null;

    /// <summary>The parsed source recordal time.</summary>
    public global::System.DateTimeOffset RecordedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="RecordedAt"/> was parsed successfully.</summary>
    public bool HasRecordedAt = false;

    /// <summary>The recordal time exactly as supplied.</summary>
    public string? RecordedAtText = null;

    /// <summary>Unmodeled prosecution-event fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one United States or international trademark classification.
/// </summary>
public class TrademarkClass
{
    /// <summary>The classification category, such as <c>international</c> or <c>us</c>.</summary>
    public string? Category = null;

    /// <summary>The class code.</summary>
    public string? Code = null;

    /// <summary>The class description.</summary>
    public string? Description = null;

    /// <summary>Whether the source identifies this as the prime international class.</summary>
    public bool IsPrime = false;

    /// <summary>Whether <see cref="IsPrime"/> was supplied.</summary>
    public bool HasIsPrime = false;

    /// <summary>Unmodeled classification fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one goods-and-services entry and its classes, basis, status, and use dates.
/// </summary>
public class TrademarkGoodsAndService
{
    /// <summary>The trademark serial number associated with the entry.</summary>
    public string? SerialNumber = null;

    /// <summary>The prime class code.</summary>
    public string? PrimeClassCode = null;

    /// <summary>The goods or services description.</summary>
    public string? Description = null;

    /// <summary>The source class status code.</summary>
    public string? StatusCode = null;

    /// <summary>The source class status description.</summary>
    public string? StatusDescription = null;

    /// <summary>The United States and international classifications.</summary>
    public TrademarkClass[] Classes = new TrademarkClass[0];

    /// <summary>The source pseudo-class codes.</summary>
    public string[] PseudoClassCodes = new string[0];

    /// <summary>The filing basis associated with the class.</summary>
    public TrademarkFilingBasis FilingBasis = new TrademarkFilingBasis();

    /// <summary>The parsed class status date.</summary>
    public global::System.DateTimeOffset StatusDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="StatusDate"/> was parsed successfully.</summary>
    public bool HasStatusDate = false;

    /// <summary>The class status date exactly as supplied.</summary>
    public string? StatusDateText = null;

    /// <summary>The parsed first-use-anywhere date.</summary>
    public global::System.DateTimeOffset FirstUseDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FirstUseDate"/> was parsed successfully.</summary>
    public bool HasFirstUseDate = false;

    /// <summary>The first-use-anywhere date exactly as supplied.</summary>
    public string? FirstUseDateText = null;

    /// <summary>The source description of an imprecise first-use-anywhere date.</summary>
    public string? FirstUseDateDescription = null;

    /// <summary>The parsed first-use-in-commerce date.</summary>
    public global::System.DateTimeOffset FirstUseInCommerceDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FirstUseInCommerceDate"/> was parsed successfully.</summary>
    public bool HasFirstUseInCommerceDate = false;

    /// <summary>The first-use-in-commerce date exactly as supplied.</summary>
    public string? FirstUseInCommerceDateText = null;

    /// <summary>The source description of an imprecise first-use-in-commerce date.</summary>
    public string? FirstUseInCommerceDateDescription = null;

    /// <summary>Unmodeled goods-and-services fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one foreign trademark application or registration and its priority data.
/// </summary>
public class TrademarkForeignRegistration
{
    /// <summary>The foreign application number.</summary>
    public string? ApplicationNumber = null;

    /// <summary>The foreign registration number.</summary>
    public string? RegistrationNumber = null;

    /// <summary>The foreign country code.</summary>
    public string? CountryCode = null;

    /// <summary>The foreign country name.</summary>
    public string? CountryName = null;

    /// <summary>Whether priority is claimed from this foreign record.</summary>
    public bool IsPriorityClaimed = false;

    /// <summary>Whether <see cref="IsPriorityClaimed"/> was supplied.</summary>
    public bool HasIsPriorityClaimed = false;

    /// <summary>The parsed foreign application filing date.</summary>
    public global::System.DateTimeOffset FilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="FilingDate"/> was parsed successfully.</summary>
    public bool HasFilingDate = false;

    /// <summary>The foreign application filing date exactly as supplied.</summary>
    public string? FilingDateText = null;

    /// <summary>The parsed foreign registration date.</summary>
    public global::System.DateTimeOffset RegistrationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="RegistrationDate"/> was parsed successfully.</summary>
    public bool HasRegistrationDate = false;

    /// <summary>The foreign registration date exactly as supplied.</summary>
    public string? RegistrationDateText = null;

    /// <summary>The parsed foreign registration expiration date.</summary>
    public global::System.DateTimeOffset ExpirationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ExpirationDate"/> was parsed successfully.</summary>
    public bool HasExpirationDate = false;

    /// <summary>The foreign registration expiration date exactly as supplied.</summary>
    public string? ExpirationDateText = null;

    /// <summary>Unmodeled foreign-registration fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one event reported in Madrid Protocol history.
/// </summary>
public class TrademarkMadridEvent
{
    /// <summary>The Madrid event serial number.</summary>
    public string? SerialNumber = null;

    /// <summary>The Madrid event action code.</summary>
    public string? ActionCode = null;

    /// <summary>The Madrid event description.</summary>
    public string? Description = null;

    /// <summary>The related document identifier.</summary>
    public string? DocumentIdentifier = null;

    /// <summary>The parsed event time.</summary>
    public global::System.DateTimeOffset EventDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="EventDate"/> was parsed successfully.</summary>
    public bool HasEventDate = false;

    /// <summary>The event time exactly as supplied.</summary>
    public string? EventDateText = null;

    /// <summary>The parsed recordal time.</summary>
    public global::System.DateTimeOffset RecordedAt = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="RecordedAt"/> was parsed successfully.</summary>
    public bool HasRecordedAt = false;

    /// <summary>The recordal time exactly as supplied.</summary>
    public string? RecordedAtText = null;

    /// <summary>Unmodeled Madrid-event fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds one international registration associated with a United States trademark case.
/// </summary>
public class TrademarkInternationalRegistration
{
    /// <summary>The international registration number.</summary>
    public string? RegistrationNumber = null;

    /// <summary>The related United States serial number.</summary>
    public string? SerialNumber = null;

    /// <summary>The source international-registration status code.</summary>
    public int StatusCode = 0;

    /// <summary>Whether <see cref="StatusCode"/> was supplied.</summary>
    public bool HasStatusCode = false;

    /// <summary>The international-registration status description.</summary>
    public string? StatusDescription = null;

    /// <summary>Whether international priority is claimed.</summary>
    public bool IsPriorityClaimed = false;

    /// <summary>Whether <see cref="IsPriorityClaimed"/> was supplied.</summary>
    public bool HasIsPriorityClaimed = false;

    /// <summary>Whether a first refusal was recorded.</summary>
    public bool FirstRefusal = false;

    /// <summary>Whether <see cref="FirstRefusal"/> was supplied.</summary>
    public bool HasFirstRefusal = false;

    /// <summary>The parsed international registration date.</summary>
    public global::System.DateTimeOffset RegistrationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="RegistrationDate"/> was parsed successfully.</summary>
    public bool HasRegistrationDate = false;

    /// <summary>The international registration date exactly as supplied.</summary>
    public string? RegistrationDateText = null;

    /// <summary>The parsed international status date.</summary>
    public global::System.DateTimeOffset StatusDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="StatusDate"/> was parsed successfully.</summary>
    public bool HasStatusDate = false;

    /// <summary>The international status date exactly as supplied.</summary>
    public string? StatusDateText = null;

    /// <summary>The parsed notification-of-designation date.</summary>
    public global::System.DateTimeOffset DesignationNotificationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="DesignationNotificationDate"/> was parsed successfully.</summary>
    public bool HasDesignationNotificationDate = false;

    /// <summary>The notification-of-designation date exactly as supplied.</summary>
    public string? DesignationNotificationDateText = null;

    /// <summary>The parsed automatic-protection date.</summary>
    public global::System.DateTimeOffset AutomaticProtectionDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="AutomaticProtectionDate"/> was parsed successfully.</summary>
    public bool HasAutomaticProtectionDate = false;

    /// <summary>The automatic-protection date exactly as supplied.</summary>
    public string? AutomaticProtectionDateText = null;

    /// <summary>The parsed cancellation date.</summary>
    public global::System.DateTimeOffset CancellationDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="CancellationDate"/> was parsed successfully.</summary>
    public bool HasCancellationDate = false;

    /// <summary>The cancellation date exactly as supplied.</summary>
    public string? CancellationDateText = null;

    /// <summary>The parsed renewal date.</summary>
    public global::System.DateTimeOffset RenewalDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="RenewalDate"/> was parsed successfully.</summary>
    public bool HasRenewalDate = false;

    /// <summary>The renewal date exactly as supplied.</summary>
    public string? RenewalDateText = null;

    /// <summary>Unmodeled international-registration fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds identifiers that relate a United States case to a Madrid international registration.
/// </summary>
public class TrademarkRelationship
{
    /// <summary>The United States serial number.</summary>
    public string? SerialNumber = null;

    /// <summary>The United States registration number.</summary>
    public string? RegistrationNumber = null;

    /// <summary>The USPTO Madrid reference number.</summary>
    public string? ReferenceNumber = null;

    /// <summary>The international registration number.</summary>
    public string? InternationalRegistrationNumber = null;

    /// <summary>Unmodeled relationship fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds Madrid Protocol identifiers, registrations, relationships, events, and lifecycle dates.
/// </summary>
public class TrademarkMadridData
{
    /// <summary>The primary USPTO Madrid reference number.</summary>
    public string? ReferenceNumber = null;

    /// <summary>The primary international registration number.</summary>
    public string? InternationalRegistrationNumber = null;

    /// <summary>The Madrid status code.</summary>
    public int StatusCode = 0;

    /// <summary>Whether <see cref="StatusCode"/> was supplied.</summary>
    public bool HasStatusCode = false;

    /// <summary>The Madrid status code preserved as text.</summary>
    public string? StatusCodeText = null;

    /// <summary>The Madrid status description.</summary>
    public string? StatusDescription = null;

    /// <summary>The associated international registrations.</summary>
    public TrademarkInternationalRegistration[] Registrations = new TrademarkInternationalRegistration[0];

    /// <summary>The Madrid history events.</summary>
    public TrademarkMadridEvent[] Events = new TrademarkMadridEvent[0];

    /// <summary>The related United States serial numbers.</summary>
    public string[] RelatedSerialNumbers = new string[0];

    /// <summary>The related international registration numbers.</summary>
    public string[] RelatedInternationalRegistrationNumbers = new string[0];

    /// <summary>The explicit relationships reported by the source.</summary>
    public TrademarkRelationship[] Relationships = new TrademarkRelationship[0];

    /// <summary>The parsed original filing date.</summary>
    public global::System.DateTimeOffset OriginalFilingDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="OriginalFilingDate"/> was parsed successfully.</summary>
    public bool HasOriginalFilingDate = false;

    /// <summary>The original filing date exactly as supplied.</summary>
    public string? OriginalFilingDateText = null;

    /// <summary>The parsed Madrid status date.</summary>
    public global::System.DateTimeOffset StatusDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="StatusDate"/> was parsed successfully.</summary>
    public bool HasStatusDate = false;

    /// <summary>The Madrid status date exactly as supplied.</summary>
    public string? StatusDateText = null;

    /// <summary>The parsed reply-by date.</summary>
    public global::System.DateTimeOffset ReplyByDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="ReplyByDate"/> was parsed successfully.</summary>
    public bool HasReplyByDate = false;

    /// <summary>The reply-by date exactly as supplied.</summary>
    public string? ReplyByDateText = null;

    /// <summary>The parsed international renewal date.</summary>
    public global::System.DateTimeOffset RenewalDate = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="RenewalDate"/> was parsed successfully.</summary>
    public bool HasRenewalDate = false;

    /// <summary>The international renewal date exactly as supplied.</summary>
    public string? RenewalDateText = null;

    /// <summary>Unmodeled Madrid fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Holds a parsed TSDR trademark case-status response and its retained raw representation.
/// </summary>
public class TrademarkCaseStatus
{
    /// <summary>The identifier supplied by the caller.</summary>
    public string? RequestedIdentifier = null;

    /// <summary>The canonical identifier used for the TSDR request.</summary>
    public string? CanonicalIdentifier = null;

    /// <summary>The kind of canonical trademark identifier.</summary>
    public TrademarkIdentifierKind IdentifierKind = TrademarkIdentifierKind.Unknown;

    /// <summary>The United States serial number.</summary>
    public string? SerialNumber = null;

    /// <summary>The United States registration number.</summary>
    public string? RegistrationNumber = null;

    /// <summary>The USPTO Madrid reference number.</summary>
    public string? ReferenceNumber = null;

    /// <summary>The Madrid international registration number.</summary>
    public string? InternationalRegistrationNumber = null;

    /// <summary>The mark representation and design metadata.</summary>
    public TrademarkMark Mark = new TrademarkMark();

    /// <summary>The current lifecycle status and location data.</summary>
    public TrademarkStatus Status = new TrademarkStatus();

    /// <summary>The application-level filing basis.</summary>
    public TrademarkFilingBasis FilingBasis = new TrademarkFilingBasis();

    /// <summary>The owners currently associated with the case.</summary>
    public TrademarkOwner[] CurrentOwners = new TrademarkOwner[0];

    /// <summary>The owners formerly associated with the case.</summary>
    public TrademarkOwner[] PriorOwners = new TrademarkOwner[0];

    /// <summary>The attorneys and representatives associated with the case.</summary>
    public TrademarkAttorney[] Attorneys = new TrademarkAttorney[0];

    /// <summary>The current correspondence contact and address.</summary>
    public TrademarkCorrespondence Correspondence = new TrademarkCorrespondence();

    /// <summary>The prosecution-history events in source order.</summary>
    public TrademarkProsecutionEvent[] ProsecutionEvents = new TrademarkProsecutionEvent[0];

    /// <summary>The goods-and-services entries and their classifications.</summary>
    public TrademarkGoodsAndService[] GoodsAndServices = new TrademarkGoodsAndService[0];

    /// <summary>The foreign applications and registrations associated with the case.</summary>
    public TrademarkForeignRegistration[] ForeignRegistrations = new TrademarkForeignRegistration[0];

    /// <summary>The Madrid Protocol data associated with the case.</summary>
    public TrademarkMadridData Madrid = new TrademarkMadridData();

    /// <summary>Whether the source identifies the case as containing international data.</summary>
    public bool IsInternationalData = false;

    /// <summary>Whether <see cref="IsInternationalData"/> was supplied.</summary>
    public bool HasIsInternationalData = false;

    /// <summary>The service search or transaction identifier.</summary>
    public string? SearchIdentifier = null;

    /// <summary>The origin and retrieval context of the case status.</summary>
    public PatentDataProvenance Provenance = new PatentDataProvenance();

    /// <summary>The returned representation of <see cref="RawResponse"/>.</summary>
    public DocumentFormat RawFormat = DocumentFormat.Unknown;

    /// <summary>A bounded retained raw JSON or XML response, when requested.</summary>
    public string? RawResponse = null;

    /// <summary>Whether the complete case-status response was processed.</summary>
    public bool IsComplete = false;

    /// <summary>Unmodeled response fields retained as raw JSON or XML text.</summary>
    public global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

/// <summary>
/// Describes availability and supported alternatives for Trademark Decisions and Proceedings data.
/// </summary>
public class TrademarkDecisionCapabilityResult
{
    /// <summary>The human-readable capability name.</summary>
    public string? CapabilityName = null;

    /// <summary>Whether a stable published API is available for direct access.</summary>
    public bool IsPublishedApiAvailable = false;

    /// <summary>The stable reason code when direct API access is unavailable.</summary>
    public string? UnavailabilityCode = null;

    /// <summary>A credential-free explanation of the direct API availability state.</summary>
    public string? UnavailabilityReason = null;

    /// <summary>Whether automated access to the browser portal is supported.</summary>
    public bool IsPortalAutomationSupported = false;

    /// <summary>Whether related products can be discovered through the generic bulk catalog.</summary>
    public bool IsBulkDiscoveryAvailable = false;

    /// <summary>The bulk dataset families that can provide related records.</summary>
    public DatasetFamily[] BulkDatasetFamilies = new DatasetFamily[0];

    /// <summary>A ready-to-use generic bulk catalog search for related products.</summary>
    public SearchRequest BulkProductSearch = new SearchRequest();

    /// <summary>The origin of this capability determination.</summary>
    public DataProvenance Provenance = DataProvenance.Unknown;
}
