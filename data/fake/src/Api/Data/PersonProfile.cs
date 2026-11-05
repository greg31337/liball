namespace fake;

/// <summary>
/// Represents a correlated synthetic person profile.
/// </summary>
/// <remarks>Email, phone, and address values are fictional fixtures and make no claim of deliverability or allocation.</remarks>
public class PersonProfile
{
    /// <summary>The given name used by the profile.</summary>
    public string FirstName = string.Empty;

    /// <summary>The optional middle name used by the selected full-name template, or an empty string when omitted.</summary>
    public string MiddleName = string.Empty;

    /// <summary>The family name used by the profile.</summary>
    public string LastName = string.Empty;

    /// <summary>The optional maiden family-name component, or an empty string when omitted.</summary>
    public string MaidenName = string.Empty;

    /// <summary>The optional patronymic component, or an empty string when omitted.</summary>
    public string Patronymic = string.Empty;

    /// <summary>The optional matronymic component, or an empty string when omitted.</summary>
    public string Matronymic = string.Empty;

    /// <summary>The optional prefix used by the selected full-name template, or an empty string when omitted.</summary>
    public string Prefix = string.Empty;

    /// <summary>The optional suffix used by the selected full-name template, or an empty string when omitted.</summary>
    public string Suffix = string.Empty;

    /// <summary>The initials of the non-affix components present in <c>FullName</c>.</summary>
    public string Initials = string.Empty;

    /// <summary>The locale-ordered name containing the selected name components.</summary>
    public string FullName = string.Empty;

    /// <summary>The full name in the locale's native script and component order.</summary>
    public string NativeName = string.Empty;

    /// <summary>The full name transliterated to an ASCII Latin form where supported.</summary>
    public string TransliteratedName = string.Empty;

    /// <summary>A locale-provided correlated pronoun set.</summary>
    public PronounSet Pronouns = new PronounSet("", "", "", "", "", "");

    /// <summary>The synthetic date of birth.</summary>
    public System.DateOnly BirthDate;

    /// <summary>The completed age at the captured reference date.</summary>
    public int Age;

    /// <summary>The ASCII username derived from the selected name.</summary>
    public string Username = string.Empty;

    /// <summary>The reserved-domain email address derived from the username.</summary>
    public string Email = string.Empty;

    /// <summary>Correlated synthetic profile, username, and reserved-email identifiers.</summary>
    public PersonIdentifierRecord Identifiers = new PersonIdentifierRecord();

    /// <summary>A self-contained synthetic SVG avatar data URI.</summary>
    public string AvatarDataUri = string.Empty;

    /// <summary>A reviewed named favorite-color fixture.</summary>
    public string FavoriteColor = string.Empty;

    /// <summary>The synthetic phone number.</summary>
    public string Phone = string.Empty;

    /// <summary>The fictional address.</summary>
    public string Address = string.Empty;

    /// <summary>The fictional employer name.</summary>
    public string Employer = string.Empty;

    /// <summary>The canonical requested locale.</summary>
    public string Locale = string.Empty;

    /// <summary>The primary language subtag of <c>Locale</c>.</summary>
    public string Language = string.Empty;

    /// <summary>The IANA time-zone identifier associated with the selected locality.</summary>
    public string TimeZoneId = string.Empty;

    internal PersonProfile()
    {
    }
}
