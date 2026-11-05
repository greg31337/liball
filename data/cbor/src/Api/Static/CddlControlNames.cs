namespace cbor;

/// <summary>Provides standardized CDDL control-operator names.</summary>
public static class CddlControlNames
{
    /// <summary>The RFC 8610 size control.</summary>
    public const string Size = ".size";
    /// <summary>The RFC 8610 bit-selection control.</summary>
    public const string Bits = ".bits";
    /// <summary>The RFC 8610 XML Schema regular-expression control.</summary>
    public const string RegularExpression = ".regexp";
    /// <summary>The RFC 8610 embedded-CBOR control.</summary>
    public const string Cbor = ".cbor";
    /// <summary>The RFC 8610 embedded-CBOR-sequence control.</summary>
    public const string CborSequence = ".cborseq";
    /// <summary>The RFC 8610 containment control.</summary>
    public const string Within = ".within";
    /// <summary>The RFC 8610 intersection control.</summary>
    public const string And = ".and";
    /// <summary>The RFC 8610 less-than control.</summary>
    public const string LessThan = ".lt";
    /// <summary>The RFC 8610 less-than-or-equal control.</summary>
    public const string LessThanOrEqual = ".le";
    /// <summary>The RFC 8610 greater-than control.</summary>
    public const string GreaterThan = ".gt";
    /// <summary>The RFC 8610 greater-than-or-equal control.</summary>
    public const string GreaterThanOrEqual = ".ge";
    /// <summary>The RFC 8610 equality control.</summary>
    public const string Equal = ".eq";
    /// <summary>The RFC 8610 inequality control.</summary>
    public const string NotEqual = ".ne";
    /// <summary>The RFC 8610 default-value control.</summary>
    public const string Default = ".default";
    /// <summary>The RFC 9165 numeric-addition control.</summary>
    public const string Plus = ".plus";
    /// <summary>The RFC 9165 concatenation control.</summary>
    public const string Concatenate = ".cat";
    /// <summary>The RFC 9165 dedented-concatenation control.</summary>
    public const string DedentedConcatenate = ".det";
    /// <summary>The RFC 9165 text ABNF control.</summary>
    public const string Abnf = ".abnf";
    /// <summary>The RFC 9165 byte-string ABNF control.</summary>
    public const string AbnfBytes = ".abnfb";
    /// <summary>The RFC 9165 feature-selection control.</summary>
    public const string Feature = ".feature";
    /// <summary>The RFC 9741 strict base64url text control.</summary>
    public const string Base64Url = ".b64u";
    /// <summary>The RFC 9741 sloppy base64url text control.</summary>
    public const string Base64UrlSloppy = ".b64u-sloppy";
    /// <summary>The RFC 9741 strict classic-base64 text control.</summary>
    public const string Base64Classic = ".b64c";
    /// <summary>The RFC 9741 sloppy classic-base64 text control.</summary>
    public const string Base64ClassicSloppy = ".b64c-sloppy";
    /// <summary>The RFC 9741 base45 text control.</summary>
    public const string Base45 = ".b45";
    /// <summary>The RFC 9741 base32 text control.</summary>
    public const string Base32 = ".b32";
    /// <summary>The RFC 9741 base32hex text control.</summary>
    public const string Base32Hex = ".h32";
    /// <summary>The RFC 9741 case-insensitive hexadecimal text control.</summary>
    public const string Hex = ".hex";
    /// <summary>The RFC 9741 lowercase hexadecimal text control.</summary>
    public const string HexLower = ".hexlc";
    /// <summary>The RFC 9741 uppercase hexadecimal text control.</summary>
    public const string HexUpper = ".hexuc";
    /// <summary>The RFC 9741 base-10 integer text control.</summary>
    public const string Base10 = ".base10";
    /// <summary>The RFC 9741 formatted-text control.</summary>
    public const string Printf = ".printf";
    /// <summary>The RFC 9741 JSON text control.</summary>
    public const string Json = ".json";
    /// <summary>The RFC 9741 group-join text control.</summary>
    public const string Join = ".join";

    /// <summary>All standardized control names in RFC and registry order.</summary>
    public static readonly string[] RegisteredNames =
    [
        Size, Bits, RegularExpression, Cbor, CborSequence, Within, And, LessThan,
        LessThanOrEqual, GreaterThan, GreaterThanOrEqual, Equal, NotEqual, Default,
        Plus, Concatenate, DedentedConcatenate, Abnf, AbnfBytes, Feature, Base64Url,
        Base64UrlSloppy, Base64Classic, Base64ClassicSloppy, Base45, Base32,
        Base32Hex, Hex, HexLower, HexUpper, Base10, Printf, Json, Join
    ];
}
