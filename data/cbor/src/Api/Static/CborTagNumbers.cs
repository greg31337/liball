namespace cbor;

/// <summary>
/// Provides tag numbers used by built-in CBOR semantic processing.
/// </summary>
public static class CborTagNumbers
{
    /// <summary>The standard date and time string tag.</summary>
    public const ulong DateTimeString = 0;
    /// <summary>The epoch-based date and time tag.</summary>
    public const ulong EpochDateTime = 1;
    /// <summary>The unsigned bignum tag.</summary>
    public const ulong PositiveBignum = 2;
    /// <summary>The negative bignum tag.</summary>
    public const ulong NegativeBignum = 3;
    /// <summary>The decimal fraction tag.</summary>
    public const ulong DecimalFraction = 4;
    /// <summary>The bigfloat tag.</summary>
    public const ulong Bigfloat = 5;
    /// <summary>The expected base64url conversion tag.</summary>
    public const ulong ExpectedBase64Url = 21;
    /// <summary>The expected base64 conversion tag.</summary>
    public const ulong ExpectedBase64 = 22;
    /// <summary>The expected base16 conversion tag.</summary>
    public const ulong ExpectedBase16 = 23;
    /// <summary>The encoded CBOR data item tag.</summary>
    public const ulong EncodedCbor = 24;
    /// <summary>The string reference tag.</summary>
    public const ulong StringReference = 25;
    /// <summary>The potentially shared value tag.</summary>
    public const ulong ShareableValue = 28;
    /// <summary>The shared value reference tag.</summary>
    public const ulong SharedValueReference = 29;
    /// <summary>The URI tag.</summary>
    public const ulong Uri = 32;
    /// <summary>The base64url text tag.</summary>
    public const ulong Base64Url = 33;
    /// <summary>The base64 text tag.</summary>
    public const ulong Base64 = 34;
    /// <summary>The regular expression tag.</summary>
    public const ulong RegularExpression = 35;
    /// <summary>The MIME message tag.</summary>
    public const ulong MimeMessage = 36;
    /// <summary>The universally unique identifier tag.</summary>
    public const ulong Uuid = 37;
    /// <summary>The string reference namespace tag.</summary>
    public const ulong StringReferenceNamespace = 256;
    /// <summary>The self-described CBOR tag.</summary>
    public const ulong SelfDescribedCbor = 55799;
}
