namespace asn1;

/// <summary>
/// Defines the universal tag numbers assigned by X.680.
/// </summary>
public static class AsnUniversalTags
{
    /// <summary>
    /// Identifies the tag reserved for use by encoding rules.
    /// </summary>
    public const int ReservedForEncodingRules = 0;

    /// <summary>
    /// Identifies the BOOLEAN tag.
    /// </summary>
    public const int Boolean = 1;

    /// <summary>
    /// Identifies the INTEGER tag.
    /// </summary>
    public const int Integer = 2;

    /// <summary>
    /// Identifies the BIT STRING tag.
    /// </summary>
    public const int BitString = 3;

    /// <summary>
    /// Identifies the OCTET STRING tag.
    /// </summary>
    public const int OctetString = 4;

    /// <summary>
    /// Identifies the NULL tag.
    /// </summary>
    public const int Null = 5;

    /// <summary>
    /// Identifies the OBJECT IDENTIFIER tag.
    /// </summary>
    public const int ObjectIdentifier = 6;

    /// <summary>
    /// Identifies the ObjectDescriptor tag.
    /// </summary>
    public const int ObjectDescriptor = 7;

    /// <summary>
    /// Identifies the EXTERNAL tag.
    /// </summary>
    public const int External = 8;

    /// <summary>
    /// Identifies the INSTANCE OF tag.
    /// </summary>
    public const int InstanceOf = 8;

    /// <summary>
    /// Identifies the REAL tag.
    /// </summary>
    public const int Real = 9;

    /// <summary>
    /// Identifies the ENUMERATED tag.
    /// </summary>
    public const int Enumerated = 10;

    /// <summary>
    /// Identifies the EMBEDDED PDV tag.
    /// </summary>
    public const int EmbeddedPdv = 11;

    /// <summary>
    /// Identifies the UTF8String tag.
    /// </summary>
    public const int Utf8String = 12;

    /// <summary>
    /// Identifies the RELATIVE-OID tag.
    /// </summary>
    public const int RelativeObjectIdentifier = 13;

    /// <summary>
    /// Identifies the TIME tag.
    /// </summary>
    public const int Time = 14;

    /// <summary>
    /// Identifies the reserved universal tag number 15.
    /// </summary>
    public const int Reserved = 15;

    /// <summary>
    /// Identifies the SEQUENCE tag.
    /// </summary>
    public const int Sequence = 16;

    /// <summary>
    /// Identifies the SEQUENCE OF tag.
    /// </summary>
    public const int SequenceOf = 16;

    /// <summary>
    /// Identifies the SET tag.
    /// </summary>
    public const int Set = 17;

    /// <summary>
    /// Identifies the SET OF tag.
    /// </summary>
    public const int SetOf = 17;

    /// <summary>
    /// Identifies the NumericString tag.
    /// </summary>
    public const int NumericString = 18;

    /// <summary>
    /// Identifies the PrintableString tag.
    /// </summary>
    public const int PrintableString = 19;

    /// <summary>
    /// Identifies the TeletexString tag.
    /// </summary>
    public const int TeletexString = 20;

    /// <summary>
    /// Identifies the T61String tag.
    /// </summary>
    public const int T61String = 20;

    /// <summary>
    /// Identifies the VideotexString tag.
    /// </summary>
    public const int VideotexString = 21;

    /// <summary>
    /// Identifies the IA5String tag.
    /// </summary>
    public const int Ia5String = 22;

    /// <summary>
    /// Identifies the UTCTime tag.
    /// </summary>
    public const int UtcTime = 23;

    /// <summary>
    /// Identifies the GeneralizedTime tag.
    /// </summary>
    public const int GeneralizedTime = 24;

    /// <summary>
    /// Identifies the GraphicString tag.
    /// </summary>
    public const int GraphicString = 25;

    /// <summary>
    /// Identifies the VisibleString tag.
    /// </summary>
    public const int VisibleString = 26;

    /// <summary>
    /// Identifies the ISO646String tag.
    /// </summary>
    public const int Iso646String = 26;

    /// <summary>
    /// Identifies the GeneralString tag.
    /// </summary>
    public const int GeneralString = 27;

    /// <summary>
    /// Identifies the UniversalString tag.
    /// </summary>
    public const int UniversalString = 28;

    /// <summary>
    /// Identifies the unrestricted CHARACTER STRING tag.
    /// </summary>
    public const int CharacterString = 29;

    /// <summary>
    /// Identifies the BMPString tag.
    /// </summary>
    public const int BmpString = 30;

    /// <summary>
    /// Identifies the DATE tag.
    /// </summary>
    public const int Date = 31;

    /// <summary>
    /// Identifies the TIME-OF-DAY tag.
    /// </summary>
    public const int TimeOfDay = 32;

    /// <summary>
    /// Identifies the DATE-TIME tag.
    /// </summary>
    public const int DateTime = 33;

    /// <summary>
    /// Identifies the DURATION tag.
    /// </summary>
    public const int Duration = 34;

    /// <summary>
    /// Identifies the OID-IRI tag.
    /// </summary>
    public const int ObjectIdentifierIri = 35;

    /// <summary>
    /// Identifies the RELATIVE-OID-IRI tag.
    /// </summary>
    public const int RelativeObjectIdentifierIri = 36;
}
