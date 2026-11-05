using System;
using System.Collections.Generic;

namespace token;

/// <summary>
/// Represents RFC 9052 protected and unprotected COSE header parameter buckets.
/// </summary>
public class CoseHeader
{
    /// <summary>Contains parameters placed in the protected bucket.</summary>
    public CborMapEntry[]? ProtectedParameters;

    /// <summary>Contains parameters placed in the unprotected bucket.</summary>
    public CborMapEntry[]? UnprotectedParameters;

    /// <summary>Contains the exact encoded protected map bytes after parsing or writing.</summary>
    public byte[] ProtectedBytes;

    /// <summary>Contains the integer algorithm identifier, or zero when absent or textual.</summary>
    public int Algorithm;

    /// <summary>Indicates whether <see cref="Algorithm"/> contains an integer identifier.</summary>
    public bool HasAlgorithm;

    /// <summary>Contains the text algorithm identifier, or <c>null</c>.</summary>
    public string? AlgorithmText;

    /// <summary>Contains the key identifier, or <c>null</c>.</summary>
    public byte[]? KeyId;

    /// <summary>Contains the text content type, or <c>null</c>.</summary>
    public string? ContentTypeText;

    /// <summary>Contains the numeric content type, or zero when absent or textual.</summary>
    public ulong ContentTypeNumber;

    /// <summary>Indicates whether <see cref="ContentTypeNumber"/> contains a value.</summary>
    public bool HasContentTypeNumber;

    /// <summary>Contains the critical header labels, or an empty array when absent.</summary>
    public CborItem[] Critical;

    /// <summary>Contains the full initialization vector, or <c>null</c>.</summary>
    public byte[]? InitializationVector;

    /// <summary>Contains the partial initialization vector, or <c>null</c>.</summary>
    public byte[]? PartialInitializationVector;

    /// <summary>Initializes empty protected and unprotected header buckets.</summary>
    public CoseHeader()
    {
        ProtectedParameters = Array.Empty<CborMapEntry>();
        UnprotectedParameters = Array.Empty<CborMapEntry>();
        ProtectedBytes = Array.Empty<byte>();
        Algorithm = 0;
        HasAlgorithm = false;
        AlgorithmText = null;
        KeyId = null;
        ContentTypeText = null;
        ContentTypeNumber = 0;
        HasContentTypeNumber = false;
        Critical = Array.Empty<CborItem>();
        InitializationVector = null;
        PartialInitializationVector = null;
    }

    /// <summary>Creates header buckets from existing parameter arrays.</summary>
    /// <param name="protectedParameters">The protected parameters to copy.</param>
    /// <param name="unprotectedParameters">The unprotected parameters to copy.</param>
    /// <returns>A header containing independent copies of both parameter arrays.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseHeader Create(
        CborMapEntry[] protectedParameters,
        CborMapEntry[] unprotectedParameters)
    {
        if (protectedParameters == null)
        {
            throw new ArgumentNullException("protectedParameters");
        }

        if (unprotectedParameters == null)
        {
            throw new ArgumentNullException("unprotectedParameters");
        }

        CoseHeader result = new CoseHeader();
        result.ProtectedParameters = protectedParameters.AsSpan().ToArray();
        result.UnprotectedParameters = unprotectedParameters.AsSpan().ToArray();
        result.RefreshCommonParameters();
        return result;
    }

    /// <summary>Reads a complete two-item COSE Headers array with default limits.</summary>
    /// <param name="cbor">The CBOR array containing a protected bstr and unprotected map.</param>
    /// <returns>A parsed header or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>cbor</c> is null.</exception>
    internal static CoseHeaderResult Read(byte[] cbor)
    {
        return Read(cbor, new TokenPolicy());
    }

    /// <summary>Reads a complete two-item COSE Headers array with explicit limits.</summary>
    /// <param name="cbor">The CBOR array containing a protected bstr and unprotected map.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>A parsed header or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseHeaderResult Read(byte[] cbor, TokenPolicy policy)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        CborReader reader = CborReader.Create(
            cbor,
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        CborReadResult decoded = reader.Read();
        if (!decoded.Success)
        {
            return Failure(decoded.Error);
        }

        if (reader.GetRemainingLength() != 0)
        {
            return Failure(
                "trailing_cose_header_data",
                "A COSE Headers value must contain exactly one CBOR item.");
        }

        if ((decoded.Item ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenCborValueKind.Array
            || decoded.Item.ArrayValue == null
            || decoded.Item.ArrayValue.Length != 2)
        {
            return Failure(
                "invalid_cose_header_structure",
                "A COSE Headers value must be a two-item CBOR array.");
        }

        return FromBuckets(
            decoded.Item.ArrayValue[0],
            decoded.Item.ArrayValue[1],
            policy);
    }

    /// <summary>Parses protected and unprotected bucket items from a containing COSE structure.</summary>
    /// <param name="protectedBucket">The protected header byte-string item.</param>
    /// <param name="unprotectedBucket">The unprotected header map item.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>A parsed header or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CoseHeaderResult FromBuckets(
        CborItem protectedBucket,
        CborItem unprotectedBucket,
        TokenPolicy policy)
    {
        if (protectedBucket == null)
        {
            throw new ArgumentNullException("protectedBucket");
        }

        if (unprotectedBucket == null)
        {
            throw new ArgumentNullException("unprotectedBucket");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return Failure(policyResult.Error);
        }

        if (protectedBucket.Kind != TokenCborValueKind.ByteString
            || protectedBucket.Bytes == null)
        {
            return Failure(
                "invalid_cose_protected_header",
                "The protected COSE header bucket must be a byte string.");
        }

        if (unprotectedBucket.Kind != TokenCborValueKind.Map
            || unprotectedBucket.MapValue == null)
        {
            return Failure(
                "invalid_cose_unprotected_header",
                "The unprotected COSE header bucket must be a map.");
        }

        CborMapEntry[] protectedParameters = Array.Empty<CborMapEntry>();
        if (protectedBucket.Bytes.Length > 0)
        {
            CborReader protectedReader = CborReader.Create(
                protectedBucket.Bytes,
                policy.MaximumTokenSizeInBytes,
                policy.MaximumCborDepth);
            CborReadResult protectedMap = protectedReader.Read();
            if (!protectedMap.Success)
            {
                return Failure(
                    "invalid_cose_protected_header",
                    "The protected COSE header byte string contains invalid CBOR.");
            }

            if (protectedReader.GetRemainingLength() != 0
                || (protectedMap.Item ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenCborValueKind.Map
                || protectedMap.Item.MapValue == null)
            {
                return Failure(
                    "invalid_cose_protected_header",
                    "The protected COSE header byte string must contain exactly one map.");
            }

            protectedParameters = protectedMap.Item.MapValue;
        }

        TokenResult validation = ValidateParameters(
            protectedParameters,
            unprotectedBucket.MapValue,
            policy.MaximumCborDepth);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        CoseHeader header = new CoseHeader();
        header.ProtectedParameters = protectedParameters.AsSpan().ToArray();
        header.UnprotectedParameters =
            unprotectedBucket.MapValue.AsSpan().ToArray();
        header.ProtectedBytes = protectedBucket.Bytes.AsSpan().ToArray();
        header.RefreshCommonParameters();

        CoseHeaderResult result = new CoseHeaderResult();
        result.Header = header;
        result.Success = true;
        return result;
    }

    /// <summary>Finds a parameter, preferring the protected bucket.</summary>
    /// <param name="label">The integer parameter label.</param>
    /// <returns>The matching value, or <c>null</c> when absent.</returns>
    internal CborItem? Get(int label)
    {
        CborItem? value = GetProtected(label);
        return value == null ? GetUnprotected(label) : value;
    }

    /// <summary>Finds a private text-labeled parameter, preferring the protected bucket.</summary>
    /// <param name="label">The case-sensitive text parameter label.</param>
    /// <returns>The matching value, or <c>null</c> when absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>label</c> is null.</exception>
    internal CborItem? GetByTextLabel(string label)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        CborItem? value = GetProtectedByTextLabel(label);
        return value == null ? GetUnprotectedByTextLabel(label) : value;
    }

    /// <summary>Finds an integer-labeled parameter in the protected bucket.</summary>
    /// <param name="label">The integer parameter label.</param>
    /// <returns>The matching value, or <c>null</c> when absent.</returns>
    internal CborItem? GetProtected(int label)
    {
        return Find(ProtectedParameters, IntegerLabel(label));
    }

    /// <summary>Finds a text-labeled parameter in the protected bucket.</summary>
    /// <param name="label">The case-sensitive text parameter label.</param>
    /// <returns>The matching value, or <c>null</c> when absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>label</c> is null.</exception>
    internal CborItem? GetProtectedByTextLabel(string label)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        return Find(ProtectedParameters, TextLabel(label));
    }

    /// <summary>Finds an integer-labeled parameter in the unprotected bucket.</summary>
    /// <param name="label">The integer parameter label.</param>
    /// <returns>The matching value, or <c>null</c> when absent.</returns>
    internal CborItem? GetUnprotected(int label)
    {
        return Find(UnprotectedParameters, IntegerLabel(label));
    }

    /// <summary>Finds a text-labeled parameter in the unprotected bucket.</summary>
    /// <param name="label">The case-sensitive text parameter label.</param>
    /// <returns>The matching value, or <c>null</c> when absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>label</c> is null.</exception>
    internal CborItem? GetUnprotectedByTextLabel(string label)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        return Find(UnprotectedParameters, TextLabel(label));
    }

    /// <summary>Adds or replaces an integer-labeled protected parameter.</summary>
    /// <param name="label">The integer parameter label.</param>
    /// <param name="value">The CBOR parameter value.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the label exists unprotected.</exception>
    internal void SetProtected(int label, CborItem value)
    {
        SetParameter(true, IntegerLabel(label), value);
    }

    /// <summary>Adds or replaces a text-labeled protected parameter.</summary>
    /// <param name="label">The case-sensitive text parameter label.</param>
    /// <param name="value">The CBOR parameter value.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the label exists unprotected.</exception>
    internal void SetProtectedByTextLabel(string label, CborItem value)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        SetParameter(true, TextLabel(label), value);
    }

    /// <summary>Adds or replaces an integer-labeled unprotected parameter.</summary>
    /// <param name="label">The integer parameter label.</param>
    /// <param name="value">The CBOR parameter value.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the label exists protected.</exception>
    internal void SetUnprotected(int label, CborItem value)
    {
        SetParameter(false, IntegerLabel(label), value);
    }

    /// <summary>Adds or replaces a text-labeled unprotected parameter.</summary>
    /// <param name="label">The case-sensitive text parameter label.</param>
    /// <param name="value">The CBOR parameter value.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the label exists protected.</exception>
    internal void SetUnprotectedByTextLabel(string label, CborItem value)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        SetParameter(false, TextLabel(label), value);
    }

    /// <summary>Removes an integer-labeled parameter from either bucket.</summary>
    /// <param name="label">The integer parameter label.</param>
    /// <returns><c>true</c> when a parameter was removed.</returns>
    internal bool Remove(int label)
    {
        return RemoveParameter(IntegerLabel(label));
    }

    /// <summary>Removes a text-labeled parameter from either bucket.</summary>
    /// <param name="label">The case-sensitive text parameter label.</param>
    /// <returns><c>true</c> when a parameter was removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>label</c> is null.</exception>
    internal bool RemoveByTextLabel(string label)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        return RemoveParameter(TextLabel(label));
    }

    /// <summary>Reads the optional RFC 9597 CWT Claims header parameter.</summary>
    /// <param name="policy">The CBOR size and depth limits.</param>
    /// <returns>The parsed claims and their protection placement.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is null.</exception>
    internal CoseCwtClaimsResult GetCwtClaims(TokenPolicy policy)
    {
        return CoseCwtClaims.Read(this, policy);
    }

    /// <summary>Adds or replaces protected RFC 9597 CWT claims.</summary>
    /// <param name="claims">The claims to store.</param>
    /// <param name="policy">The CBOR size and depth limits.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal TokenResult SetProtectedCwtClaims(
        CwtClaims claims,
        TokenPolicy policy)
    {
        return CoseCwtClaims.SetProtected(this, claims, policy);
    }

    /// <summary>Adds or replaces unprotected RFC 9597 CWT claims.</summary>
    /// <param name="claims">The claims to store.</param>
    /// <param name="policy">The CBOR size and depth limits.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal TokenResult SetUnprotectedCwtClaims(
        CwtClaims claims,
        TokenPolicy policy)
    {
        return CoseCwtClaims.SetUnprotected(this, claims, policy);
    }

    /// <summary>Removes the RFC 9597 CWT Claims parameter.</summary>
    /// <returns><c>true</c> when the parameter was removed.</returns>
    internal bool RemoveCwtClaims()
    {
        return CoseCwtClaims.Remove(this);
    }

    /// <summary>Writes the two header buckets as a deterministic CBOR array.</summary>
    /// <returns>Encoded COSE Headers bytes or stable failure details.</returns>
    internal TokenBytesResult Write()
    {
        return Write(this, new TokenPolicy());
    }

    /// <summary>Writes the two header buckets with explicit limits.</summary>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>Encoded COSE Headers bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is null.</exception>
    internal TokenBytesResult WriteWithPolicy(TokenPolicy policy)
    {
        return Write(this, policy);
    }

    /// <summary>Writes a header as a deterministic protected-bstr and unprotected-map array.</summary>
    /// <param name="header">The header to write.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>Encoded COSE Headers bytes or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult Write(CoseHeader header, TokenPolicy policy)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return BytesFailure(policyResult.Error);
        }

        TokenResult validation = ValidateParameters(
            header.ProtectedParameters,
            header.UnprotectedParameters,
            policy.MaximumCborDepth);
        if (!validation.Success)
        {
            return BytesFailure(validation.Error);
        }

        TokenBytesResult protectedBytes = WriteProtected(header, policy);
        if (!protectedBytes.Success)
        {
            return protectedBytes;
        }

        CborItem protectedItem = BytesItem(protectedBytes.Bytes);
        CborItem unprotectedItem = MapItem(header.UnprotectedParameters);
        CborItem buckets = ArrayItem(new CborItem[]
        {
            protectedItem,
            unprotectedItem
        });
        CborWriter writer = CborWriter.Create(
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        TokenResult written = writer.WriteDeterministicItem(buckets);
        if (!written.Success)
        {
            return BytesFailure(written.Error);
        }

        header.ProtectedBytes = protectedBytes.Bytes.AsSpan().ToArray();
        header.RefreshCommonParameters();
        TokenBytesResult result = new TokenBytesResult();
        result.Bytes = writer.ToArray();
        result.Success = true;
        return result;
    }

    /// <summary>Writes only the deterministic protected map bytes used by COSE cryptographic structures.</summary>
    /// <param name="header">The header to write.</param>
    /// <param name="policy">The token size and CBOR depth limits.</param>
    /// <returns>The encoded map, or an empty byte string for an empty protected bucket.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenBytesResult WriteProtected(
        CoseHeader header,
        TokenPolicy policy)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        TokenResult policyResult = ValidatePolicy(policy);
        if (!policyResult.Success)
        {
            return BytesFailure(policyResult.Error);
        }

        TokenResult validation = ValidateParameters(
            header.ProtectedParameters,
            header.UnprotectedParameters,
            policy.MaximumCborDepth);
        if (!validation.Success)
        {
            return BytesFailure(validation.Error);
        }

        if ((header.ProtectedParameters ?? throw new InvalidOperationException("A required value is null.")).Length == 0)
        {
            TokenBytesResult empty = new TokenBytesResult();
            empty.Bytes = Array.Empty<byte>();
            empty.Success = true;
            return empty;
        }

        CborWriter writer = CborWriter.Create(
            policy.MaximumTokenSizeInBytes,
            policy.MaximumCborDepth);
        TokenResult written = writer.WriteDeterministicMap(
            header.ProtectedParameters);
        if (!written.Success)
        {
            return BytesFailure(written.Error);
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Bytes = writer.ToArray();
        result.Success = true;
        return result;
    }

    void SetParameter(bool protect, CborItem label, CborItem value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        CborMapEntry[]? opposite = protect
            ? UnprotectedParameters
            : ProtectedParameters;
        if (Find(opposite, label) != null)
        {
            throw new InvalidOperationException(
                "A COSE header label cannot occur in both parameter buckets.");
        }

        CborMapEntry[]? current = protect
            ? ProtectedParameters
            : UnprotectedParameters;
        if (current == null)
        {
            current = Array.Empty<CborMapEntry>();
        }

        int index = 0;
        while (index < current.Length)
        {
            CborMapEntry entry = current[index];
            if (entry != null
                && entry.Key != null
                && CborMapKeyComparer.AreEqual(entry.Key, label))
            {
                entry.Value = value;
                if (protect)
                {
                    ProtectedBytes = Array.Empty<byte>();
                }

                RefreshCommonParameters();
                return;
            }

            index += 1;
        }

        CborMapEntry[] expanded = new CborMapEntry[current.Length + 1];
        current.AsSpan().CopyTo(expanded);
        expanded[expanded.Length - 1] = new CborMapEntry(label, value);
        if (protect)
        {
            ProtectedParameters = expanded;
            ProtectedBytes = Array.Empty<byte>();
        }
        else
        {
            UnprotectedParameters = expanded;
        }

        RefreshCommonParameters();
    }

    bool RemoveParameter(CborItem label)
    {
        CoseHeaderRemovalResult protectedRemoval = RemoveFrom(
            ProtectedParameters,
            label);
        CoseHeaderRemovalResult unprotectedRemoval = RemoveFrom(
            UnprotectedParameters,
            label);
        ProtectedParameters = protectedRemoval.Parameters;
        UnprotectedParameters = unprotectedRemoval.Parameters;
        if (protectedRemoval.Removed)
        {
            ProtectedBytes = Array.Empty<byte>();
        }

        RefreshCommonParameters();
        return protectedRemoval.Removed || unprotectedRemoval.Removed;
    }

    void RefreshCommonParameters()
    {
        Algorithm = 0;
        HasAlgorithm = false;
        AlgorithmText = null;
        KeyId = null;
        ContentTypeText = null;
        ContentTypeNumber = 0;
        HasContentTypeNumber = false;
        Critical = Array.Empty<CborItem>();
        InitializationVector = null;
        PartialInitializationVector = null;

        CborItem? algorithm = Get(CoseHeaderLabels.Algorithm);
        CoseHeaderIntegerValue integerValue = ReadInt32(algorithm);
        if (integerValue.Present)
        {
            Algorithm = integerValue.Value;
            HasAlgorithm = true;
        }
        else if (algorithm != null
            && algorithm.Kind == TokenCborValueKind.TextString)
        {
            AlgorithmText = algorithm.Text;
        }

        CborItem? keyId = Get(CoseHeaderLabels.KeyId);
        if (keyId != null && keyId.Bytes != null)
        {
            KeyId = keyId.Bytes.AsSpan().ToArray();
        }

        CborItem? contentType = Get(CoseHeaderLabels.ContentType);
        if (contentType != null
            && contentType.Kind == TokenCborValueKind.TextString)
        {
            ContentTypeText = contentType.Text;
        }
        else if (contentType != null
            && contentType.Kind == TokenCborValueKind.UnsignedInteger)
        {
            ContentTypeNumber = contentType.IntegerArgument;
            HasContentTypeNumber = true;
        }

        CborItem? critical = Get(CoseHeaderLabels.Critical);
        if (critical != null && critical.ArrayValue != null)
        {
            Critical = critical.ArrayValue.AsSpan().ToArray();
        }

        CborItem? initializationVector = Get(
            CoseHeaderLabels.InitializationVector);
        if (initializationVector != null
            && initializationVector.Bytes != null)
        {
            InitializationVector =
                initializationVector.Bytes.AsSpan().ToArray();
        }

        CborItem? partialInitializationVector = Get(
            CoseHeaderLabels.PartialInitializationVector);
        if (partialInitializationVector != null
            && partialInitializationVector.Bytes != null)
        {
            PartialInitializationVector =
                partialInitializationVector.Bytes.AsSpan().ToArray();
        }
    }

    internal static TokenResult ValidateParameters(
        CborMapEntry[]? protectedParameters,
        CborMapEntry[]? unprotectedParameters,
        int maximumDepth)
    {
        if (protectedParameters == null || unprotectedParameters == null)
        {
            return ValidationFailure(
                "invalid_cose_header",
                "A COSE header parameter array is missing.");
        }

        TokenResult protectedMap = CborMapKeyComparer.Validate(
            protectedParameters,
            maximumDepth);
        if (!protectedMap.Success)
        {
            return protectedMap;
        }

        TokenResult unprotectedMap = CborMapKeyComparer.Validate(
            unprotectedParameters,
            maximumDepth);
        if (!unprotectedMap.Success)
        {
            return unprotectedMap;
        }

        TokenResult protectedLabels = ValidateLabels(protectedParameters);
        if (!protectedLabels.Success)
        {
            return protectedLabels;
        }

        TokenResult unprotectedLabels = ValidateLabels(unprotectedParameters);
        if (!unprotectedLabels.Success)
        {
            return unprotectedLabels;
        }

        int protectedIndex = 0;
        while (protectedIndex < protectedParameters.Length)
        {
            int unprotectedIndex = 0;
            while (unprotectedIndex < unprotectedParameters.Length)
            {
                if (CborMapKeyComparer.AreEqual(
                    (protectedParameters[protectedIndex].Key ?? throw new InvalidOperationException("A required value is null.")),
                    (unprotectedParameters[unprotectedIndex].Key ?? throw new InvalidOperationException("A required value is null.")),
                    maximumDepth))
                {
                    return ValidationFailure(
                        "duplicate_cose_header_label",
                        "A COSE header label occurs in both parameter buckets.");
                }

                unprotectedIndex += 1;
            }

            protectedIndex += 1;
        }

        CborItem? algorithm = FindEither(
            protectedParameters,
            unprotectedParameters,
            IntegerLabel(CoseHeaderLabels.Algorithm));
        if (algorithm != null
            && (algorithm.Kind == TokenCborValueKind.TextString
                && algorithm.Text == null
                || algorithm.Kind != TokenCborValueKind.TextString
                    && algorithm.Kind != TokenCborValueKind.UnsignedInteger
                    && algorithm.Kind != TokenCborValueKind.NegativeInteger))
        {
            return InvalidType("alg", "an integer or text string");
        }

        CborItem? critical = FindEither(
            protectedParameters,
            unprotectedParameters,
            IntegerLabel(CoseHeaderLabels.Critical));
        if (critical != null && !ValidCriticalLabels(critical))
        {
            return InvalidType("crit", "a nonempty array of integer or text labels");
        }

        CborItem? contentType = FindEither(
            protectedParameters,
            unprotectedParameters,
            IntegerLabel(CoseHeaderLabels.ContentType));
        if (contentType != null
            && (contentType.Kind == TokenCborValueKind.TextString
                && contentType.Text == null
                || contentType.Kind != TokenCborValueKind.TextString
                    && contentType.Kind != TokenCborValueKind.UnsignedInteger))
        {
            return InvalidType("content type", "a nonnegative integer or text string");
        }

        CborItem? cwtClaims = FindEither(
            protectedParameters,
            unprotectedParameters,
            IntegerLabel(CoseHeaderLabels.CwtClaims));
        if (cwtClaims != null)
        {
            if (cwtClaims.Kind != TokenCborValueKind.Map
                || cwtClaims.MapValue == null)
            {
                return InvalidType("CWT Claims", "a map");
            }

            TokenPolicy claimsPolicy = new TokenPolicy();
            claimsPolicy.MaximumCborDepth = maximumDepth;
            CwtClaimsResult claims = CwtClaims.FromMap(
                cwtClaims.MapValue,
                claimsPolicy);
            if (!claims.Success)
            {
                return claims.Error == null
                    ? ValidationFailure(
                        "invalid_cwt_claims_header",
                        "The CWT Claims header parameter is invalid.")
                    : ValidationFailure(
                        claims.Error.Code,
                        claims.Error.Message);
            }
        }

        TokenResult bytes = ValidateByteStringParameter(
            protectedParameters,
            unprotectedParameters,
            CoseHeaderLabels.KeyId,
            "kid");
        if (!bytes.Success)
        {
            return bytes;
        }

        bytes = ValidateByteStringParameter(
            protectedParameters,
            unprotectedParameters,
            CoseHeaderLabels.InitializationVector,
            "IV");
        if (!bytes.Success)
        {
            return bytes;
        }

        bytes = ValidateByteStringParameter(
            protectedParameters,
            unprotectedParameters,
            CoseHeaderLabels.PartialInitializationVector,
            "Partial IV");
        if (!bytes.Success)
        {
            return bytes;
        }

        CborItem? fullIv = FindEither(
            protectedParameters,
            unprotectedParameters,
            IntegerLabel(CoseHeaderLabels.InitializationVector));
        CborItem? partialIv = FindEither(
            protectedParameters,
            unprotectedParameters,
            IntegerLabel(CoseHeaderLabels.PartialInitializationVector));
        if (fullIv != null && partialIv != null)
        {
            return ValidationFailure(
                "conflicting_cose_initialization_vectors",
                "A COSE header layer cannot contain both IV and Partial IV.");
        }

        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    internal static TokenResult ValidateLabels(CborMapEntry[] parameters)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            CborMapEntry entry = parameters[index];
            if (entry == null || entry.Key == null || entry.Value == null)
            {
                return ValidationFailure(
                    "invalid_cose_header",
                    "A COSE header parameter is incomplete.");
            }

            if (!IsLabel(entry.Key))
            {
                return ValidationFailure(
                    "invalid_cose_header_label",
                    "A COSE header label must be an integer or text string.");
            }

            index += 1;
        }

        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    internal static bool ValidCriticalLabels(CborItem critical)
    {
        if (critical.Kind != TokenCborValueKind.Array
            || critical.ArrayValue == null
            || critical.ArrayValue.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < critical.ArrayValue.Length)
        {
            if (!IsLabel(critical.ArrayValue[index]))
            {
                return false;
            }

            int previous = 0;
            while (previous < index)
            {
                if (CborMapKeyComparer.AreEqual(
                    critical.ArrayValue[previous],
                    critical.ArrayValue[index]))
                {
                    return false;
                }

                previous += 1;
            }

            index += 1;
        }

        return true;
    }

    internal static TokenResult ValidateByteStringParameter(
        CborMapEntry[] protectedParameters,
        CborMapEntry[] unprotectedParameters,
        int label,
        string name)
    {
        CborItem? value = FindEither(
            protectedParameters,
            unprotectedParameters,
            IntegerLabel(label));
        if (value != null
            && (value.Kind != TokenCborValueKind.ByteString
                || value.Bytes == null))
        {
            return InvalidType(name, "a byte string");
        }

        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    internal static TokenResult InvalidType(string name, string expected)
    {
        return ValidationFailure(
            "invalid_cose_header_parameter",
            "The COSE " + name + " header parameter must be " + expected + ".");
    }

    internal static bool IsLabel(CborItem item)
    {
        return item != null
            && (item.Kind == TokenCborValueKind.UnsignedInteger
                || item.Kind == TokenCborValueKind.NegativeInteger
                || item.Kind == TokenCborValueKind.TextString
                    && item.Text != null);
    }

    internal static CoseHeaderIntegerValue ReadInt32(CborItem? item)
    {
        CoseHeaderIntegerValue result = new CoseHeaderIntegerValue();
        if (item == null)
        {
            return result;
        }

        if (item.Kind == TokenCborValueKind.UnsignedInteger)
        {
            if (item.IntegerArgument > int.MaxValue)
            {
                return result;
            }

            result.Value = (int)item.IntegerArgument;
            result.Present = true;
            return result;
        }

        if (item.Kind != TokenCborValueKind.NegativeInteger
            || item.IntegerArgument > int.MaxValue)
        {
            return result;
        }

        long signed = -1L - (long)item.IntegerArgument;
        if (signed < int.MinValue)
        {
            return result;
        }

        result.Value = (int)signed;
        result.Present = true;
        return result;
    }

    internal static CborItem? FindEither(
        CborMapEntry[] protectedParameters,
        CborMapEntry[] unprotectedParameters,
        CborItem label)
    {
        CborItem? value = Find(protectedParameters, label);
        return value == null ? Find(unprotectedParameters, label) : value;
    }

    internal static CborItem? Find(CborMapEntry[]? parameters, CborItem label)
    {
        if (parameters == null)
        {
            return null;
        }

        int index = 0;
        while (index < parameters.Length)
        {
            CborMapEntry entry = parameters[index];
            if (entry != null
                && entry.Key != null
                && CborMapKeyComparer.AreEqual(entry.Key, label))
            {
                return entry.Value;
            }

            index += 1;
        }

        return null;
    }

    internal static CoseHeaderRemovalResult RemoveFrom(
        CborMapEntry[]? parameters,
        CborItem label)
    {
        CoseHeaderRemovalResult removal = new CoseHeaderRemovalResult();
        removal.Parameters = parameters;
        if (parameters == null)
        {
            return removal;
        }

        int removedIndex = -1;
        int index = 0;
        while (index < parameters.Length)
        {
            CborMapEntry entry = parameters[index];
            if (entry != null
                && entry.Key != null
                && CborMapKeyComparer.AreEqual(entry.Key, label))
            {
                removedIndex = index;
                break;
            }

            index += 1;
        }

        if (removedIndex < 0)
        {
            return removal;
        }

        CborMapEntry[] result = new CborMapEntry[parameters.Length - 1];
        index = 0;
        int destination = 0;
        while (index < parameters.Length)
        {
            if (index != removedIndex)
            {
                result[destination] = parameters[index];
                destination += 1;
            }

            index += 1;
        }

        removal.Parameters = result;
        removal.Removed = true;
        return removal;
    }

    internal static CborItem IntegerLabel(int value)
    {
        CborItem result = new CborItem();
        if (value >= 0)
        {
            result.Kind = TokenCborValueKind.UnsignedInteger;
            result.IntegerArgument = (ulong)value;
        }
        else
        {
            result.Kind = TokenCborValueKind.NegativeInteger;
            result.IntegerArgument = (ulong)(-(long)value - 1L);
        }

        return result;
    }

    internal static CborItem TextLabel(string value)
    {
        CborItem result = new CborItem();
        result.Kind = TokenCborValueKind.TextString;
        result.Text = value;
        return result;
    }

    internal static CborItem BytesItem(byte[]? value)
    {
        CborItem result = new CborItem();
        result.Kind = TokenCborValueKind.ByteString;
        result.Bytes = value;
        return result;
    }

    internal static CborItem MapItem(CborMapEntry[]? value)
    {
        CborItem result = new CborItem();
        result.Kind = TokenCborValueKind.Map;
        result.MapValue = value;
        return result;
    }

    internal static CborItem ArrayItem(CborItem[] value)
    {
        CborItem result = new CborItem();
        result.Kind = TokenCborValueKind.Array;
        result.ArrayValue = value;
        return result;
    }

    internal static TokenResult ValidatePolicy(TokenPolicy policy)
    {
        if (policy.MaximumTokenSizeInBytes <= 0
            || policy.MaximumCborDepth <= 0)
        {
            return ValidationFailure(
                "invalid_policy",
                "The COSE header policy contains an invalid size or depth limit.");
        }

        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    internal static TokenResult ValidationFailure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    internal static CoseHeaderResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    internal static CoseHeaderResult Failure(TokenError? error)
    {
        CoseHeaderResult result = new CoseHeaderResult();
        result.Error = error;
        return result;
    }

    internal static TokenBytesResult BytesFailure(string code, string message)
    {
        return BytesFailure(new TokenError(code, message, -1));
    }

    internal static TokenBytesResult BytesFailure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }
}
