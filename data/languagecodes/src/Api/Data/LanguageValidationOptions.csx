namespace languagecodes;

/// <summary>Controls assignment, entity, and canonicality policy during language validation.</summary>
public class LanguageValidationOptions
{
    /// <summary>The required ISO 639 set, or <see cref="Iso639Set.None"/> when no set is required.</summary>
    public Iso639Set RequiredSet;

    /// <summary>The required code form, or <see cref="LanguageCodeForm.Unknown"/> when no form is required.</summary>
    public LanguageCodeForm RequiredForm;

    /// <summary>The accepted lifecycle statuses, empty when status is unrestricted.</summary>
    public LanguageCodeStatus[] AllowedStatuses = new LanguageCodeStatus[0];

    /// <summary>The accepted entity scopes, empty when scope is unrestricted.</summary>
    public LanguageScope[] AllowedScopes = new LanguageScope[0];

    /// <summary>The accepted language types, empty when language type is unrestricted.</summary>
    public LanguageType[] AllowedTypes = new LanguageType[0];

    /// <summary>Whether special-purpose language identifiers are accepted.</summary>
    public bool AllowSpecialCodes;

    /// <summary>Whether language collections, families, and groups are accepted.</summary>
    public bool AllowGroupCodes;

    /// <summary>Whether macrolanguage identifiers are accepted.</summary>
    public bool AllowMacrolanguages;

    /// <summary>Whether reserved private-use identifiers are accepted.</summary>
    public bool AllowPrivateUseCodes;

    /// <summary>Whether valid aliases and replaceable retired codes must be rejected as noncanonical.</summary>
    public bool RequireCanonical;
}
