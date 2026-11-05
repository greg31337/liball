using System;
namespace token;

/// <summary>Reads, validates, and writes proof-of-possession confirmation methods in CWT claims.</summary>
/// <remarks>
/// Unknown confirmation methods are preserved and ignored. A directly embedded symmetric key
/// is accepted because RFC 8747 permits it when the containing CWT is encrypted; callers are
/// responsible for applying that confidentiality requirement to the complete token.
/// </remarks>
internal static class CwtConfirmation
{
    /// <summary>Validates the optional CWT confirmation claim and its registered methods.</summary>
    /// <param name="claims">The CWT claims to validate.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>claims</c> is null.</exception>
    internal static TokenResult Validate(CwtClaims claims)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        CborItem? claim = claims.Get(CwtClaimLabels.Confirmation);
        if (claim == null)
        {
            return Success();
        }

        CwtConfirmationMapResult map = ReadMap(claim);
        if (map.Error != null)
        {
            return Failure(map.Error);
        }

        return ValidateMethods(map.Entries);
    }

    /// <summary>Extracts a directly embedded proof-of-possession COSE_Key.</summary>
    /// <remarks>The returned key owns parsed secret or native material and should be released by its consumer.</remarks>
    /// <param name="claims">The CWT claims containing the confirmation claim.</param>
    /// <returns>The parsed key, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>claims</c> is null.</exception>
    internal static TokenKeyResult GetProofOfPossessionKey(CwtClaims claims)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        CwtConfirmationMapResult map = GetRequiredMap(claims);
        if (map.Error != null)
        {
            return KeyFailure(map.Error);
        }

        TokenResult validation = ValidateMethods(map.Entries);
        if (!validation.Success)
        {
            return KeyFailure(validation.Error);
        }

        CborItem? item = Find(map.Entries, CwtConfirmationMethodLabels.CoseKey);
        if (item == null)
        {
            return KeyFailure(
                "missing_confirmation_key",
                "The CWT confirmation claim does not contain a directly embedded COSE_Key.");
        }

        TokenBytesResult encoded = Encode(item);
        if (!encoded.Success)
        {
            return KeyFailure(encoded.Error);
        }

        CoseKeyResult parsed = CoseKey.Read((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")));
        if (!parsed.Success)
        {
            return KeyFailure(parsed.Error);
        }

        TokenKeyResult result = new TokenKeyResult();
        result.Success = true;
        result.Key = parsed.Key;
        return result;
    }

    /// <summary>Adds or replaces the directly embedded proof-of-possession COSE_Key.</summary>
    /// <remarks>
    /// An asymmetric key must contain public material only. A symmetric key may be embedded,
    /// but the caller must ensure that the complete CWT is encrypted.
    /// </remarks>
    /// <param name="claims">The CWT claims to update.</param>
    /// <param name="key">The proof-of-possession key.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult SetProofOfPossessionKey(
        CwtClaims claims,
        TokenKey key)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (!IsSymmetric(key) && key.ContainsPrivateMaterial())
        {
            return Failure(
                "confirmation_key_not_public",
                "A directly embedded asymmetric confirmation key must contain public material only.");
        }

        TokenBytesResult encoded = CoseKey.Write(key);
        if (!encoded.Success)
        {
            return Failure(encoded.Error);
        }

        CborReadResult decoded = CborReader.Create((encoded.Bytes ?? throw new InvalidOperationException("A required value is null."))).Read();
        if (!decoded.Success)
        {
            return Failure(decoded.Error);
        }

        return SetExclusiveKeyMethod(
            claims,
            CwtConfirmationMethodLabels.CoseKey,
            decoded.Item);
    }

    /// <summary>Gets the binary key identifier from a CWT confirmation claim.</summary>
    /// <param name="claims">The CWT claims containing the confirmation claim.</param>
    /// <returns>An independent copy of the identifier, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>claims</c> is null.</exception>
    internal static TokenBytesResult GetKeyId(CwtClaims claims)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        CwtConfirmationMapResult map = GetRequiredMap(claims);
        if (map.Error != null)
        {
            return BytesFailure(map.Error);
        }

        TokenResult validation = ValidateMethods(map.Entries);
        if (!validation.Success)
        {
            return BytesFailure(validation.Error);
        }

        CborItem? item = Find(map.Entries, CwtConfirmationMethodLabels.KeyId);
        if (item == null)
        {
            return BytesFailure(
                "missing_confirmation_key_id",
                "The CWT confirmation claim does not contain a key identifier.");
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = item.Bytes.AsSpan().ToArray();
        return result;
    }

    /// <summary>Adds or replaces the binary key identifier in a CWT confirmation claim.</summary>
    /// <param name="claims">The CWT claims to update.</param>
    /// <param name="keyId">The application-specific key identifier.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult SetKeyId(CwtClaims claims, byte[] keyId)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        if (keyId == null)
        {
            throw new ArgumentNullException("keyId");
        }

        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = keyId.AsSpan().ToArray();
        return SetMethod(claims, CwtConfirmationMethodLabels.KeyId, item);
    }

    /// <summary>Gets a validated COSE_Encrypt or COSE_Encrypt0 confirmation value.</summary>
    /// <param name="claims">The CWT claims containing the confirmation claim.</param>
    /// <returns>Deterministic encoded COSE bytes, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>claims</c> is null.</exception>
    internal static TokenBytesResult GetEncryptedKey(CwtClaims claims)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        CwtConfirmationMapResult map = GetRequiredMap(claims);
        if (map.Error != null)
        {
            return BytesFailure(map.Error);
        }

        TokenResult validation = ValidateMethods(map.Entries);
        if (!validation.Success)
        {
            return BytesFailure(validation.Error);
        }

        CborItem? item = Find(
            map.Entries,
            CwtConfirmationMethodLabels.EncryptedCoseKey);
        if (item == null)
        {
            return BytesFailure(
                "missing_encrypted_confirmation_key",
                "The CWT confirmation claim does not contain an encrypted COSE_Key.");
        }

        return Encode(item);
    }

    /// <summary>Adds or replaces an encrypted proof-of-possession COSE_Key.</summary>
    /// <param name="claims">The CWT claims to update.</param>
    /// <param name="encryptedKey">An encoded COSE_Encrypt or COSE_Encrypt0 value.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static TokenResult SetEncryptedKey(
        CwtClaims claims,
        byte[] encryptedKey)
    {
        if (claims == null)
        {
            throw new ArgumentNullException("claims");
        }

        if (encryptedKey == null)
        {
            throw new ArgumentNullException("encryptedKey");
        }

        CborReader reader = CborReader.Create(encryptedKey);
        CborReadResult decoded = reader.Read();
        if (!decoded.Success)
        {
            return Failure(decoded.Error);
        }

        if (reader.GetRemainingLength() != 0)
        {
            return Failure(
                "invalid_encrypted_confirmation_key",
                "An encrypted confirmation key must contain exactly one COSE message.");
        }

        TokenResult validation = ValidateEncryptedKey(decoded.Item);
        if (!validation.Success)
        {
            return validation;
        }

        return SetExclusiveKeyMethod(
            claims,
            CwtConfirmationMethodLabels.EncryptedCoseKey,
            decoded.Item);
    }

    static TokenResult SetExclusiveKeyMethod(
        CwtClaims claims,
        int label,
        CborItem? value)
    {
        CwtConfirmationMapResult map = GetOptionalMap(claims);
        if (map.Error != null)
        {
            return Failure(map.Error);
        }

        TokenResult validation = ValidateMethods(map.Entries);
        if (!validation.Success)
        {
            return validation;
        }

        CborMapEntry[] retained = RemoveKeyMethods(map.Entries);
        CborMapEntry[] updated = Append(retained, label, value);
        claims.Set(CwtClaimLabels.Confirmation, MapItem(updated));
        return Success();
    }

    static TokenResult SetMethod(
        CwtClaims claims,
        int label,
        CborItem value)
    {
        CwtConfirmationMapResult map = GetOptionalMap(claims);
        if (map.Error != null)
        {
            return Failure(map.Error);
        }

        TokenResult validation = ValidateMethods(map.Entries);
        if (!validation.Success)
        {
            return validation;
        }

        CborMapEntry[] retained = Remove(map.Entries, label);
        CborMapEntry[] updated = Append(retained, label, value);
        claims.Set(CwtClaimLabels.Confirmation, MapItem(updated));
        return Success();
    }

    static CwtConfirmationMapResult GetRequiredMap(CwtClaims claims)
    {
        CborItem? claim = claims.Get(CwtClaimLabels.Confirmation);
        if (claim == null)
        {
            CwtConfirmationMapResult missing = new CwtConfirmationMapResult();
            missing.Error = new TokenError(
                "missing_confirmation_claim",
                "The CWT Claims Set does not contain a confirmation claim.",
                -1);
            return missing;
        }

        return ReadMap(claim);
    }

    static CwtConfirmationMapResult GetOptionalMap(CwtClaims claims)
    {
        CborItem? claim = claims.Get(CwtClaimLabels.Confirmation);
        if (claim == null)
        {
            CwtConfirmationMapResult empty = new CwtConfirmationMapResult();
            empty.Entries = Array.Empty<CborMapEntry>();
            return empty;
        }

        return ReadMap(claim);
    }

    static CwtConfirmationMapResult ReadMap(CborItem claim)
    {
        CwtConfirmationMapResult result = new CwtConfirmationMapResult();
        if (claim.Kind != TokenCborValueKind.Map || claim.MapValue == null)
        {
            result.Error = new TokenError(
                "confirmation_not_map",
                "The CWT confirmation claim must be a CBOR map.",
                -1);
            return result;
        }

        TokenResult map = CborMapKeyComparer.Validate(claim.MapValue, 64);
        if (!map.Success)
        {
            result.Error = map.Error;
            return result;
        }

        result.Entries = claim.MapValue;
        return result;
    }

    static TokenResult ValidateMethods(CborMapEntry[]? entries)
    {
        bool direct = false;
        bool encrypted = false;
        int index = 0;
        while (index < (entries ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            CborItem? key = entries[index].Key;
            if (!IsConfirmationLabel(key))
            {
                return Failure(
                    "invalid_confirmation_method_key",
                    "A CWT confirmation method key must be an integer or text string.");
            }

            CborItem? value = entries[index].Value;
            if (IsLabel(key, CwtConfirmationMethodLabels.CoseKey))
            {
                direct = true;
                TokenResult keyResult = ValidateDirectKey(value);
                if (!keyResult.Success)
                {
                    return keyResult;
                }
            }
            else if (IsLabel(key, CwtConfirmationMethodLabels.EncryptedCoseKey))
            {
                encrypted = true;
                TokenResult encryptedResult = ValidateEncryptedKey(value);
                if (!encryptedResult.Success)
                {
                    return encryptedResult;
                }
            }
            else if (IsLabel(key, CwtConfirmationMethodLabels.KeyId))
            {
                if ((value ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenCborValueKind.ByteString
                    || value.Bytes == null)
                {
                    return Failure(
                        "invalid_confirmation_key_id",
                        "The CWT confirmation key identifier must be a byte string.");
                }
            }
            else if (IsLabel(key, CwtConfirmationMethodLabels.OscoreInputMaterial))
            {
                if ((value ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenCborValueKind.Map
                    || value.MapValue == null)
                {
                    return Failure(
                        "invalid_confirmation_oscore_input_material",
                        "OSCORE confirmation input material must be a CBOR map.");
                }
            }
            else if (IsLabel(key, CwtConfirmationMethodLabels.CoseKeyThumbprint)
                || IsLabel(key, CwtConfirmationMethodLabels.JwkThumbprint))
            {
                if ((value ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenCborValueKind.ByteString
                    || value.Bytes == null)
                {
                    return Failure(
                        "invalid_confirmation_thumbprint",
                        "A CWT confirmation key thumbprint must be a byte string.");
                }
            }

            index += 1;
        }

        if (direct && encrypted)
        {
            return Failure(
                "multiple_confirmation_keys",
                "A CWT confirmation claim cannot contain both COSE_Key and Encrypted_COSE_Key methods.");
        }

        return Success();
    }

    static TokenResult ValidateDirectKey(CborItem? item)
    {
        TokenBytesResult encoded = Encode(item);
        if (!encoded.Success)
        {
            return Failure(encoded.Error);
        }

        CoseKeyResult parsed = CoseKey.Read((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")));
        if (!parsed.Success)
        {
            return Failure(
                "invalid_confirmation_key",
                "The CWT confirmation COSE_Key is invalid.");
        }

        if (!IsSymmetric(parsed.Key) && (parsed.Key ?? throw new InvalidOperationException("A required value is null.")).ContainsPrivateMaterial())
        {
            parsed.Key.ReleaseOwnedMaterial();
            return Failure(
                "confirmation_key_not_public",
                "A directly embedded asymmetric confirmation key must contain public material only.");
        }

        (parsed.Key ?? throw new InvalidOperationException("A required value is null.")).ReleaseOwnedMaterial();
        return Success();
    }

    static TokenResult ValidateEncryptedKey(CborItem? item)
    {
        CborItem? value = item;
        ulong tag = 0;
        if (value != null && value.Kind == TokenCborValueKind.Tag)
        {
            tag = value.TagValue;
            value = value.TaggedValue;
        }

        if (value == null
            || value.Kind != TokenCborValueKind.Array
            || value.ArrayValue == null)
        {
            return Failure(
                "invalid_encrypted_confirmation_key",
                "An encrypted confirmation key must be a COSE_Encrypt or COSE_Encrypt0 structure.");
        }

        TokenBytesResult encoded = Encode(item);
        if (!encoded.Success)
        {
            return Failure(encoded.Error);
        }

        if ((tag == 0 || tag == 16) && value.ArrayValue.Length == 3)
        {
            CoseEncrypt0Result parsed0 = CoseEncrypt0.Read((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")));
            if (parsed0.Success)
            {
                return Success();
            }
        }

        if ((tag == 0 || tag == 96) && value.ArrayValue.Length == 4)
        {
            CoseEncryptResult parsed = CoseEncrypt.Read((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")));
            if (parsed.Success)
            {
                return Success();
            }
        }

        return Failure(
            "invalid_encrypted_confirmation_key",
            "The encrypted confirmation key is not a valid COSE_Encrypt or COSE_Encrypt0 message.");
    }

    static TokenBytesResult Encode(CborItem? item)
    {
        CborWriter writer = CborWriter.Create();
        TokenResult written = writer.WriteDeterministicItem((item ?? throw new InvalidOperationException("A required value is null.")));
        if (!written.Success)
        {
            return BytesFailure(written.Error);
        }

        TokenBytesResult result = new TokenBytesResult();
        result.Success = true;
        result.Bytes = writer.ToArray();
        return result;
    }

    static CborItem? Find(CborMapEntry[]? entries, int label)
    {
        int index = 0;
        while (index < (entries ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            if (IsLabel(entries[index].Key, label))
            {
                return entries[index].Value;
            }

            index += 1;
        }

        return null;
    }

    static CborMapEntry[] RemoveKeyMethods(CborMapEntry[]? entries)
    {
        CborMapEntry[] first = Remove(
            entries,
            CwtConfirmationMethodLabels.CoseKey);
        return Remove(first, CwtConfirmationMethodLabels.EncryptedCoseKey);
    }

    static CborMapEntry[] Remove(CborMapEntry[]? entries, int label)
    {
        int count = 0;
        int index = 0;
        while (index < (entries ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            if (!IsLabel(entries[index].Key, label))
            {
                count += 1;
            }

            index += 1;
        }

        CborMapEntry[] result = new CborMapEntry[count];
        index = 0;
        int destination = 0;
        while (index < entries.Length)
        {
            if (!IsLabel(entries[index].Key, label))
            {
                result[destination] = entries[index];
                destination += 1;
            }

            index += 1;
        }

        return result;
    }

    static CborMapEntry[] Append(
        CborMapEntry[] entries,
        int label,
        CborItem? value)
    {
        CborMapEntry[] result = new CborMapEntry[entries.Length + 1];
        entries.AsSpan().CopyTo(result);
        result[result.Length - 1] = new CborMapEntry(Integer(label), (value ?? throw new InvalidOperationException("A required value is null.")));
        return result;
    }

    static bool IsConfirmationLabel(CborItem? item)
    {
        return (item ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenCborValueKind.UnsignedInteger
            || item.Kind == TokenCborValueKind.NegativeInteger
            || item.Kind == TokenCborValueKind.TextString;
    }

    static bool IsLabel(CborItem? item, int label)
    {
        if (label >= 0)
        {
            return (item ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenCborValueKind.UnsignedInteger
                && item.IntegerArgument == (ulong)label;
        }

        return (item ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenCborValueKind.NegativeInteger
            && item.IntegerArgument == (ulong)(-(long)label - 1L);
    }

    static bool IsSymmetric(TokenKey? key)
    {
        return (key ?? throw new InvalidOperationException("A required value is null.")).SymmetricKey != null
            || key.CoseKeyType == CoseKeyLabels.KeyTypeSymmetric
            || string.Equals(key.KeyType, TokenKeyTypes.Symmetric, StringComparison.Ordinal);
    }

    static CborItem Integer(int value)
    {
        CborItem item = new CborItem();
        if (value >= 0)
        {
            item.Kind = TokenCborValueKind.UnsignedInteger;
            item.IntegerArgument = (ulong)value;
        }
        else
        {
            item.Kind = TokenCborValueKind.NegativeInteger;
            item.IntegerArgument = (ulong)(-(long)value - 1L);
        }

        return item;
    }

    static CborItem MapItem(CborMapEntry[] entries)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = entries;
        return item;
    }

    static TokenResult Success()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static TokenResult Failure(TokenError? error)
    {
        TokenResult result = new TokenResult();
        result.Error = error;
        return result;
    }

    static TokenKeyResult KeyFailure(string code, string message)
    {
        return KeyFailure(new TokenError(code, message, -1));
    }

    static TokenKeyResult KeyFailure(TokenError? error)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Error = error;
        return result;
    }

    static TokenBytesResult BytesFailure(string code, string message)
    {
        return BytesFailure(new TokenError(code, message, -1));
    }

    static TokenBytesResult BytesFailure(TokenError? error)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = error;
        return result;
    }
}
