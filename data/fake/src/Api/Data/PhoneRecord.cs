namespace fake;

/// <summary>
/// Represents a generated phone fixture together with its parsing and safety metadata.
/// </summary>
/// <remarks>
/// Component and category fields preserve generation context only. They do not identify
/// a carrier, subscriber, owner, allocation, location, or reachable service.
/// </remarks>
public class PhoneRecord
{
    /// <summary>The complete formatted phone fixture, including an extension when requested.</summary>
    public string Number = string.Empty;

    /// <summary>The compact area or service-code component.</summary>
    public string AreaCode = string.Empty;

    /// <summary>The compact subscriber-number component.</summary>
    public string SubscriberNumber = string.Empty;

    /// <summary>The compact national significant number without calling or trunk prefixes.</summary>
    public string NationalSignificantNumber = string.Empty;

    /// <summary>The extension digits, or an empty string when no extension was requested.</summary>
    public string Extension = string.Empty;

    /// <summary>Whether the formatted number contains an extension suffix.</summary>
    public bool HasExtension;

    /// <summary>The locale, presentation, category, and masks needed to retain parsing context.</summary>
    public PhoneRecordParsingMetadata ParsingMetadata =
        new PhoneRecordParsingMetadata();

    /// <summary>The safety policy applied while generating the phone fixture.</summary>
    public PhoneSafetyMetadata SafetyMetadata = new PhoneSafetyMetadata();

    /// <summary>The generalized generation and validation metadata for this correlated fixture.</summary>
    public GenerationMetadata Metadata = new GenerationMetadata();

    internal PhoneRecord()
    {
    }
}
