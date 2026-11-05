using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents a DKIM key.
/// </summary>
public class EmailDkimKey : IEmailDkimKeyContract
{
    EmailSignatureTagList tags;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    EmailDkimKeySourceKind sourceKind;
    EmailDkimKeyAlgorithmKind algorithmKind;
    string keyType;
    byte[] publicKeyData;
    byte[]? subjectPublicKeyInfo;
    byte[]? rsaPublicKey;
    byte[]? ed25519PublicKey;
    int keyBitCount;
    bool revoked;
    bool structurallyValid;
    bool sizePolicyCompliant;
    /// <summary>
    /// Creates a new DKIM key.
    /// </summary>
    /// <param name = "tags">The tags used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <param name = "sourceKind">The source kind used by the operation.</param>
    /// <param name = "algorithmKind">The algorithm kind used by the operation.</param>
    /// <param name = "keyType">The key type used by the operation.</param>
    /// <param name = "publicKeyData">The public key data used by the operation.</param>
    /// <param name = "subjectPublicKeyInfo">The subject public key info used by the operation.</param>
    /// <param name = "rsaPublicKey">The rsa public key used by the operation.</param>
    /// <param name = "ed25519PublicKey">The ed 25519 public key used by the operation.</param>
    /// <param name = "keyBitCount">The number of items or bytes to process.</param>
    /// <param name = "revoked">Whether to enable the revoked behavior.</param>
    /// <param name = "structurallyValid">Whether to enable the structurally valid behavior.</param>
    /// <param name = "sizePolicyCompliant">Whether to enable the size policy compliant behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDkimKey(EmailSignatureTagList tags, byte[] rawBytes, EmailSourcePosition sourcePosition, EmailDkimKeySourceKind sourceKind, EmailDkimKeyAlgorithmKind algorithmKind, string keyType, byte[] publicKeyData, byte[]? subjectPublicKeyInfo, byte[]? rsaPublicKey, byte[]? ed25519PublicKey, int keyBitCount, bool revoked, bool structurallyValid, bool sizePolicyCompliant)
    {
        if (tags == null || rawBytes == null || sourcePosition == null || keyType == null || publicKeyData == null)
            throw new ArgumentNullException("tags");
        this.tags = tags;
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
        this.sourceKind = sourceKind;
        this.algorithmKind = algorithmKind;
        this.keyType = keyType;
        this.publicKeyData = Copy(publicKeyData);
        this.subjectPublicKeyInfo = CopyOptional(subjectPublicKeyInfo);
        this.rsaPublicKey = CopyOptional(rsaPublicKey);
        this.ed25519PublicKey = CopyOptional(ed25519PublicKey);
        this.keyBitCount = keyBitCount;
        this.revoked = revoked;
        this.structurallyValid = structurallyValid;
        this.sizePolicyCompliant = sizePolicyCompliant;
    }

    /// <summary>
    /// Gets the tags.
    /// </summary>
    /// <returns>The current tags.</returns>
    public EmailSignatureTagList GetTags()
    {
        return tags;
    }

    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    public byte[] GetRawBytes()
    {
        return Copy(rawBytes);
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    /// <summary>
    /// Gets the source kind.
    /// </summary>
    /// <returns>The current source kind.</returns>
    public EmailDkimKeySourceKind GetSourceKind()
    {
        return sourceKind;
    }

    /// <summary>
    /// Gets the algorithm kind.
    /// </summary>
    /// <returns>The current algorithm kind.</returns>
    public EmailDkimKeyAlgorithmKind GetAlgorithmKind()
    {
        return algorithmKind;
    }

    /// <summary>
    /// Gets the key type.
    /// </summary>
    /// <returns>The current key type.</returns>
    public string GetKeyType()
    {
        return keyType;
    }

    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    public string GetVersion()
    {
        return ValueOrDefault("v", "DKIM1");
    }

    /// <summary>
    /// Gets the notes.
    /// </summary>
    /// <returns>The current notes.</returns>
    public string GetNotes()
    {
        return DecodeNotes();
    }

    /// <summary>
    /// Gets the hash algorithms.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current hash algorithms.</returns>
    public List<string> GetHashAlgorithms()
    {
        return SplitTag("h");
    }

    /// <summary>
    /// Gets the service types.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current service types.</returns>
    public List<string> GetServiceTypes()
    {
        return SplitTagOrDefault("s", "*");
    }

    /// <summary>
    /// Gets the flags.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current flags.</returns>
    public List<string> GetFlags()
    {
        return SplitTag("t");
    }

    /// <summary>
    /// Gets the unknown tags.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current unknown tags.</returns>
    public List<EmailSignatureTag> GetUnknownTags()
    {
        return FindUnknownTags();
    }

    /// <summary>
    /// Determines whether the current value or state is revoked.
    /// </summary>
    /// <returns><c>true</c> when the value or state is revoked; otherwise, <c>false</c>.</returns>
    public bool IsRevoked()
    {
        return revoked;
    }

    /// <summary>
    /// Determines whether the current value or state is structurally valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is structurally valid; otherwise, <c>false</c>.</returns>
    public bool IsStructurallyValid()
    {
        return structurallyValid;
    }

    /// <summary>
    /// Determines whether the current value or state is size policy compliant.
    /// </summary>
    /// <returns><c>true</c> when the value or state is size policy compliant; otherwise, <c>false</c>.</returns>
    public bool IsSizePolicyCompliant()
    {
        return sizePolicyCompliant;
    }

    /// <summary>
    /// Determines whether the current value or state is supported.
    /// </summary>
    /// <returns><c>true</c> when the value or state is supported; otherwise, <c>false</c>.</returns>
    public bool IsSupported()
    {
        return algorithmKind == EmailDkimKeyAlgorithmKind.Rsa || algorithmKind == EmailDkimKeyAlgorithmKind.Ed25519;
    }

    /// <summary>
    /// Determines whether the current value or state is usable.
    /// </summary>
    /// <returns><c>true</c> when the value or state is usable; otherwise, <c>false</c>.</returns>
    public bool IsUsable()
    {
        return !revoked && structurallyValid && sizePolicyCompliant && IsSupported() && HasPublicKey();
    }

    /// <summary>
    /// Determines whether the public key is present or available.
    /// </summary>
    /// <returns><c>true</c> when the public key is available; otherwise, <c>false</c>.</returns>
    public bool HasPublicKey()
    {
        return publicKeyData.Length > 0;
    }

    /// <summary>
    /// Gets the public key data.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current public key data.</returns>
    public byte[] GetPublicKeyData()
    {
        return Copy(publicKeyData);
    }

    /// <summary>
    /// Determines whether the subject public key info is present or available.
    /// </summary>
    /// <returns><c>true</c> when the subject public key info is available; otherwise, <c>false</c>.</returns>
    public bool HasSubjectPublicKeyInfo()
    {
        return subjectPublicKeyInfo != null;
    }

    /// <summary>
    /// Gets the subject public key info.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current subject public key info.</returns>
    public byte[]? GetSubjectPublicKeyInfo()
    {
        return CopyOptional(subjectPublicKeyInfo);
    }

    /// <summary>
    /// Determines whether the RSA public key is present or available.
    /// </summary>
    /// <returns><c>true</c> when the RSA public key is available; otherwise, <c>false</c>.</returns>
    public bool HasRsaPublicKey()
    {
        return rsaPublicKey != null;
    }

    /// <summary>
    /// Gets the RSA public key.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current RSA public key.</returns>
    public byte[]? GetRsaPublicKey()
    {
        return CopyOptional(rsaPublicKey);
    }

    /// <summary>
    /// Determines whether the ed 25519 public key is present or available.
    /// </summary>
    /// <returns><c>true</c> when the ed 25519 public key is available; otherwise, <c>false</c>.</returns>
    public bool HasEd25519PublicKey()
    {
        return ed25519PublicKey != null;
    }

    /// <summary>
    /// Gets the ed 25519 public key.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current ed 25519 public key.</returns>
    public byte[]? GetEd25519PublicKey()
    {
        return CopyOptional(ed25519PublicKey);
    }

    /// <summary>
    /// Gets the key bit count.
    /// </summary>
    /// <returns>The current key bit count.</returns>
    public int GetKeyBitCount()
    {
        return keyBitCount;
    }

    /// <summary>
    /// Determines whether the current value or state is testing.
    /// </summary>
    /// <returns><c>true</c> when the value or state is testing; otherwise, <c>false</c>.</returns>
    public bool IsTesting()
    {
        return ContainsToken(GetFlags(), "y");
    }

    /// <summary>
    /// Determines whether the current policy requires strict identity.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool RequiresStrictIdentity()
    {
        return ContainsToken(GetFlags(), "s");
    }

    /// <summary>
    /// Determines whether the current policy allows hash algorithm.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the policy allows hash algorithm; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public bool AllowsHashAlgorithm(string value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        List<string> values = GetHashAlgorithms();
        return values.Count == 0 || ContainsToken(values, value);
    }

    /// <summary>
    /// Determines whether the current policy allows service.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the policy allows service; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public bool AllowsService(string value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        List<string> values = GetServiceTypes();
        return ContainsToken(values, "*") || ContainsToken(values, value);
    }

    /// <summary>
    /// Formats this key as a deterministic canonical DKIM DNS record.
    /// </summary>
    /// <returns>The format result.</returns>
    public EmailValueFormatResult Format()
    {
        return new EmailDkimKeyFormatter().FormatWithValue(this);
    }

    /// <summary>
    /// Formats this key as a deterministic canonical DKIM DNS record with options.
    /// </summary>
    /// <param name = "options">The formatting limits and initial column.</param>
    /// <returns>The format result.</returns>
    public EmailValueFormatResult FormatWithOptions(EmailValueFormatOptions options)
    {
        return new EmailDkimKeyFormatter().FormatWithValueAndOptions(this, options);
    }

    /// <summary>
    /// Preserves this key's original safe DNS record when available.
    /// </summary>
    /// <returns>The format result.</returns>
    public EmailValueFormatResult FormatPreserving()
    {
        return new EmailDkimKeyFormatter().FormatPreservingWithValue(this);
    }

    /// <summary>
    /// Preserves this key's original safe DNS record when available, subject to formatting limits.
    /// </summary>
    /// <param name = "options">The formatting limits and initial column.</param>
    /// <returns>The format result.</returns>
    public EmailValueFormatResult FormatPreservingWithOptions(EmailValueFormatOptions options)
    {
        return new EmailDkimKeyFormatter().FormatPreservingWithValueAndOptions(this, options);
    }

    string ValueOrDefault(string name, string defaultValue)
    {
        EmailSignatureTag? tag = tags.GetFirst(name);
        return tag == null ? defaultValue : tag.GetCompactValue();
    }

    string DecodeNotes()
    {
        EmailSignatureTag? tag = tags.GetFirst("n");
        if (tag == null)
            return "";
        string value = tag.GetValue();
        List<byte> bytes = new List<byte>();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '=' && index + 2 < value.Length && IsUpperHex(value[index + 1]) && IsUpperHex(value[index + 2]))
            {
                bytes.Add((byte)(HexValue(value[index + 1]) * 16 + HexValue(value[index + 2])));
                index += 3;
            }
            else
            {
                bytes.Add((byte)value[index]);
                index++;
            }
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    List<string> SplitTag(string name)
    {
        EmailSignatureTag? tag = tags.GetFirst(name);
        if (tag == null || tag.GetCompactValue().Length == 0)
            return new List<string>();
        return new List<string>(tag.GetCompactValue().Split(':'));
    }

    List<string> SplitTagOrDefault(string name, string defaultValue)
    {
        List<string> values = SplitTag(name);
        if (values.Count == 0)
            values.Add(defaultValue);
        return values;
    }

    List<EmailSignatureTag> FindUnknownTags()
    {
        List<EmailSignatureTag> result = new List<EmailSignatureTag>();
        List<EmailSignatureTag> all = tags.GetAll();
        int index = 0;
        while (index < all.Count)
        {
            string name = all[index].GetNormalizedName();
            if (name != "v" && name != "h" && name != "k" && name != "n" && name != "p" && name != "s" && name != "t")
                result.Add(all[index]);
            index++;
        }

        return result;
    }

    static bool ContainsToken(List<string> values, string expected)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(values[index], expected, StringComparison.OrdinalIgnoreCase))
                return true;
            index++;
        }

        return false;
    }

    static int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
            return value - '0';
        return value - 'A' + 10;
    }

    static bool IsUpperHex(char value)
    {
        return value >= '0' && value <= '9' || value >= 'A' && value <= 'F';
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }

    static byte[]? CopyOptional(byte[]? value)
    {
        return value == null ? null : Copy(value);
    }
}
