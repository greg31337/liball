using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

/// <summary>Parses COSE_Key structures into normalized key containers.</summary>
internal static class CoseKey
{
    const ulong CoseKeyTag = 101;

    /// <summary>Reads one complete COSE_Key from CBOR bytes.</summary>
    /// <param name="cbor">The encoded COSE_Key bytes.</param>
    /// <returns>A result containing the normalized key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>cbor</c> is null.</exception>
    internal static CoseKeyResult Read(byte[] cbor)
    {
        return Read(cbor, false);
    }

    /// <summary>Reads one complete COSE_Key with explicit compatibility control.</summary>
    /// <param name="cbor">The encoded COSE_Key bytes.</param>
    /// <param name="allowCompatibility">Whether deprecated polymorphic ECDSA labels may be used.</param>
    /// <returns>A result containing the normalized key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>cbor</c> is null.</exception>
    internal static CoseKeyResult Read(
        byte[] cbor,
        bool allowCompatibility)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        CborReader reader = CborReader.Create(cbor);
        CborReadResult decoded = reader.Read();
        if (!decoded.Success)
        {
            return Failure(decoded.Error);
        }

        if (reader.GetRemainingLength() != 0)
        {
            return Failure(
                "trailing_cose_key_data",
                "A COSE_Key must contain exactly one CBOR item.");
        }

        CborItem? item = decoded.Item;
        if ((item ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenCborValueKind.Tag)
        {
            if (item.TagValue != CoseKeyTag)
            {
                return Failure(
                    "invalid_cose_key_tag",
                    "The CBOR tag is not the registered COSE_Key tag.");
            }

            item = item.TaggedValue;
        }

        if ((item ?? throw new InvalidOperationException("A required value is null.")).Kind != TokenCborValueKind.Map)
        {
            return Failure(
                "cose_key_not_map",
                "A COSE_Key must be encoded as a CBOR map.");
        }

        return ReadMap(item.MapValue, allowCompatibility);
    }

    /// <summary>Writes a deterministic untagged COSE_Key map.</summary>
    /// <remarks>
    /// Private parameters are included only when the key container declares private material.
    /// Caller-owned native keys and secret arrays are not modified or released.
    /// </remarks>
    /// <param name="key">The normalized key to serialize.</param>
    /// <returns>A result containing deterministic CBOR or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenBytesResult Write(TokenKey key)
    {
        return CoseKeyWriter.Write(key);
    }

    static CoseKeyResult ReadMap(
        CborMapEntry[]? entries,
        bool allowCompatibility)
    {
        TokenError? labelError = ValidateLabels(entries);
        if (labelError != null)
        {
            return Failure(labelError);
        }

        CoseIntegerValue keyType = ReadInteger(entries, CoseKeyLabels.KeyType, true);
        if (keyType.Error != null)
        {
            return Failure(keyType.Error);
        }

        CoseIntegerValue algorithm = ReadInteger(entries, CoseKeyLabels.Algorithm, false);
        if (algorithm.Error != null)
        {
            ClearSecretParameters((entries ?? throw new InvalidOperationException("A required value is null.")), keyType.Value);
            return Failure(algorithm.Error);
        }

        CoseBytesValue keyId = ReadBytes((entries ?? throw new InvalidOperationException("A required value is null.")), CoseKeyLabels.KeyId, false);
        if (keyId.Error != null)
        {
            ClearSecretParameters(entries, keyType.Value);
            return Failure(keyId.Error);
        }

        CoseBytesValue baseInitializationVector = ReadBytes(
            entries,
            CoseKeyLabels.BaseInitializationVector,
            false);
        if (baseInitializationVector.Error != null)
        {
            ClearSecretParameters(entries, keyType.Value);
            return Failure(baseInitializationVector.Error);
        }

        CoseOperationsValue operations = ReadOperations(entries);
        if (operations.Error != null)
        {
            ClearSecretParameters(entries, keyType.Value);
            return Failure(operations.Error);
        }

        CoseKeyResult result;
        if (keyType.Value == CoseKeyLabels.KeyTypeSymmetric)
        {
            result = ReadSymmetric(entries);
        }
        else if (keyType.Value == CoseKeyLabels.KeyTypeRsa)
        {
            result = ReadRsa(entries);
        }
        else if (keyType.Value == CoseKeyLabels.KeyTypeEc2)
        {
            result = ReadEc2(entries);
        }
        else if (keyType.Value == CoseKeyLabels.KeyTypeOkp)
        {
            result = ReadOkp(entries);
        }
        else if (keyType.Value == CoseKeyLabels.KeyTypeAlgorithmKeyPair)
        {
            result = ReadAlgorithmKeyPair(entries, algorithm);
        }
        else
        {
            return Failure(
                "unsupported_key_type",
                "The COSE key type is not supported.");
        }

        ClearSecretParameters(entries, keyType.Value);
        if (!result.Success)
        {
            return result;
        }

        (result.Key ?? throw new InvalidOperationException("A required value is null.")).KeyIdBytes = keyId.Bytes;
        result.Key.CoseKeyOperations = operations.Operations;
        result.Key.BaseInitializationVector =
            baseInitializationVector.Bytes;
        result.Key.CoseKeyType = keyType.Value;
        result.Key.CoseAlgorithm = algorithm.Value;
        result.Key.HasCoseAlgorithm = algorithm.Present;

        TokenResult validation = ValidateKey(
            result.Key,
            allowCompatibility);
        if (!validation.Success)
        {
            result.Key.ReleaseOwnedMaterial();
            return Failure(validation.Error);
        }

        return result;
    }

    static CoseKeyResult ReadSymmetric(CborMapEntry[] entries)
    {
        CoseBytesValue material = ReadBytes(entries, CoseKeyLabels.SymmetricKey, true);
        if (material.Error != null)
        {
            return Failure(material.Error);
        }

        if ((material.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length == 0)
        {
            return Failure(
                "invalid_key_material",
                "The symmetric COSE key material is empty.");
        }

        TokenKeyResult imported = SymmetricKeyImporterOperations.Import(material.Bytes);
        CryptographicOperations.ZeroMemory(material.Bytes);
        return FromImport(imported);
    }

    static CoseKeyResult ReadRsa(CborMapEntry[] entries)
    {
        if (Find(entries, CoseKeyLabels.RsaOtherPrimes) != null
            || Find(entries, CoseKeyLabels.RsaAdditionalPrimeFactor) != null
            || Find(entries, CoseKeyLabels.RsaAdditionalFactorCrtExponent) != null
            || Find(entries, CoseKeyLabels.RsaAdditionalFactorCrtCoefficient) != null)
        {
            return Failure(
                "unsupported_rsa_multi_prime",
                "Multi-prime RSA COSE keys are not supported.");
        }

        TokenError? parameterError = ValidateByteParameters(
            entries,
            new int[]
            {
                CoseKeyLabels.RsaModulus,
                CoseKeyLabels.RsaPublicExponent,
                CoseKeyLabels.RsaPrivateExponent,
                CoseKeyLabels.RsaFirstPrimeFactor,
                CoseKeyLabels.RsaSecondPrimeFactor,
                CoseKeyLabels.RsaFirstFactorCrtExponent,
                CoseKeyLabels.RsaSecondFactorCrtExponent,
                CoseKeyLabels.RsaCrtCoefficient
            });
        if (parameterError != null)
        {
            return Failure(parameterError);
        }

        CoseBytesValue modulus = ReadBytes(entries, CoseKeyLabels.RsaModulus, true);
        if (modulus.Error != null)
        {
            return Failure(modulus.Error);
        }

        CoseBytesValue exponent = ReadBytes(
            entries,
            CoseKeyLabels.RsaPublicExponent,
            true);
        if (exponent.Error != null)
        {
            return Failure(exponent.Error);
        }

        StringBuilder json = BeginJwk(TokenKeyTypes.Rsa);
        AppendUnsigned(json, "n", modulus.Bytes);
        AppendUnsigned(json, "e", exponent.Bytes);
        AppendOptionalUnsigned(json, entries, CoseKeyLabels.RsaPrivateExponent, "d");
        AppendOptionalUnsigned(json, entries, CoseKeyLabels.RsaFirstPrimeFactor, "p");
        AppendOptionalUnsigned(json, entries, CoseKeyLabels.RsaSecondPrimeFactor, "q");
        AppendOptionalUnsigned(json, entries, CoseKeyLabels.RsaFirstFactorCrtExponent, "dp");
        AppendOptionalUnsigned(json, entries, CoseKeyLabels.RsaSecondFactorCrtExponent, "dq");
        AppendOptionalUnsigned(json, entries, CoseKeyLabels.RsaCrtCoefficient, "qi");
        json.Append('}');
        return FromJwk(JsonWebKey.Read(json.ToString()));
    }

    static CoseKeyResult ReadEc2(CborMapEntry[] entries)
    {
        TokenError? parameterError = ValidateByteParameters(
            entries,
            new int[]
            {
                CoseKeyLabels.X,
                CoseKeyLabels.PrivateKey
            });
        if (parameterError != null)
        {
            return Failure(parameterError);
        }

        CoseIntegerValue curve = ReadInteger(entries, CoseKeyLabels.Curve, true);
        if (curve.Error != null)
        {
            return Failure(curve.Error);
        }

        string? curveName = CoseKeyLabelsOperations.GetCurveName(curve.Value);
        if (curveName == null)
        {
            return Failure("unsupported_curve", "The COSE EC2 curve is not supported.");
        }

        CoseBytesValue x = ReadBytes(entries, CoseKeyLabels.X, true);
        if (x.Error != null)
        {
            return Failure(x.Error);
        }

        CborItem? yItem = Find(entries, CoseKeyLabels.Y);
        if (yItem == null)
        {
            return Failure(
                "missing_cose_key_parameter",
                "A required COSE key parameter is missing.");
        }

        byte[]? yBytes;
        if (yItem.Kind == TokenCborValueKind.ByteString)
        {
            yBytes = yItem.Bytes.AsSpan().ToArray();
        }
        else if (yItem.Kind == TokenCborValueKind.Boolean)
        {
            JwkBytesValue decompressed = JsonWebKey.DecompressEcY(
                curveName,
                x.Bytes,
                yItem.BooleanValue);
            if (decompressed.Error != null)
            {
                return Failure(decompressed.Error);
            }

            yBytes = decompressed.Bytes;
        }
        else
        {
            return Failure(
                "invalid_cose_key_parameter",
                "The COSE EC2 y-coordinate must be a byte string or boolean.");
        }

        StringBuilder json = BeginJwk(TokenKeyTypes.EllipticCurve);
        AppendText(json, "crv", curveName);
        AppendBytes(json, "x", x.Bytes);
        AppendBytes(json, "y", yBytes);
        AppendOptionalBytes(json, entries, CoseKeyLabels.PrivateKey, "d");
        json.Append('}');
        return FromJwk(JsonWebKey.Read(json.ToString()));
    }

    static CoseKeyResult ReadOkp(CborMapEntry[] entries)
    {
        TokenError? parameterError = ValidateByteParameters(
            entries,
            new int[] { CoseKeyLabels.X, CoseKeyLabels.PrivateKey });
        if (parameterError != null)
        {
            return Failure(parameterError);
        }

        CoseIntegerValue curve = ReadInteger(entries, CoseKeyLabels.Curve, true);
        if (curve.Error != null)
        {
            return Failure(curve.Error);
        }

        string? curveName = CoseKeyLabelsOperations.GetCurveName(curve.Value);
        if (!IsOkpCurve(curve.Value))
        {
            return Failure("unsupported_curve", "The COSE OKP curve is not supported.");
        }

        CoseBytesValue x = ReadBytes(entries, CoseKeyLabels.X, true);
        if (x.Error != null)
        {
            return Failure(x.Error);
        }

        StringBuilder json = BeginJwk(TokenKeyTypes.OctetKeyPair);
        AppendText(json, "crv", curveName);
        AppendBytes(json, "x", x.Bytes);
        AppendOptionalBytes(json, entries, CoseKeyLabels.PrivateKey, "d");
        json.Append('}');
        return FromJwk(JsonWebKey.Read(json.ToString()));
    }

    static CoseKeyResult ReadAlgorithmKeyPair(
        CborMapEntry[] entries,
        CoseIntegerValue algorithm)
    {
        if (!algorithm.Present)
        {
            return Failure(
                "missing_cose_key_parameter",
                "The AKP COSE key algorithm parameter is required.");
        }

        AlgorithmMappingResult mapping = JoseCoseAlgorithmMap.FromCose(
            algorithm.Value);
        if (!mapping.Mapped || mapping.JoseName == null)
        {
            return Failure(
                "unsupported_algorithm",
                "The AKP COSE algorithm is not supported.");
        }

        CoseBytesValue publicKey = ReadBytes(
            entries,
            CoseKeyLabels.AlgorithmKeyPairPublicKey,
            true);
        if (publicKey.Error != null)
        {
            return Failure(publicKey.Error);
        }

        CoseBytesValue privateKey = ReadBytes(
            entries,
            CoseKeyLabels.AlgorithmKeyPairPrivateKey,
            false);
        if (privateKey.Error != null)
        {
            return Failure(privateKey.Error);
        }

        TokenKeyResult imported = MlDsaKeyImporter.Read(
            (publicKey.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            privateKey.Bytes,
            mapping.JoseName);
        if (privateKey.Bytes != null)
        {
            CryptographicOperations.ZeroMemory(privateKey.Bytes);
        }

        if (!imported.Success)
        {
            return Failure(imported.Error);
        }

        return FromImport(imported);
    }

    static TokenResult ValidateKey(
        TokenKey key,
        bool allowCompatibility)
    {
        if (key.CoseKeyType == CoseKeyLabels.KeyTypeSymmetric)
        {
            return SymmetricKeyImporterOperations.Validate(key);
        }

        if (key.CoseKeyType == CoseKeyLabels.KeyTypeRsa)
        {
            return RsaKeyImporterOperations.Validate(key, false, false);
        }

        if (key.CoseKeyType == CoseKeyLabels.KeyTypeEc2)
        {
            return ValidateEc2(key, allowCompatibility);
        }

        if (key.CoseKeyType == CoseKeyLabels.KeyTypeAlgorithmKeyPair)
        {
            return ValidateAlgorithmKeyPair(key);
        }

        return ValidateOkp(key);
    }

    static TokenResult ValidateAlgorithmKeyPair(TokenKey key)
    {
        if (!key.HasCoseAlgorithm)
        {
            return ValidationFailure(
                "missing_cose_key_parameter",
                "The AKP COSE key algorithm parameter is required.");
        }

        AlgorithmMappingResult mapping = JoseCoseAlgorithmMap.FromCose(
            key.CoseAlgorithm);
        if (!mapping.Mapped || mapping.JoseName == null)
        {
            return ValidationFailure(
                "unsupported_algorithm",
                "The AKP COSE algorithm is not supported.");
        }

        MLDsaAlgorithm? selected = MlDsaKeyImporter.GetAlgorithm(mapping.JoseName);
        if (selected == null
            || key.AlgorithmPublicKey == null
            || key.AlgorithmPublicKey.Length != selected.PublicKeySizeInBytes
            || key.AlgorithmPrivateKey != null
                && key.AlgorithmPrivateKey.Length != selected.PrivateSeedSizeInBytes)
        {
            return ValidationFailure(
                "invalid_mldsa_key",
                "The ML-DSA key material has an invalid length.");
        }

        TokenResult result = SuccessResult();
        return result;
    }

    static TokenResult ValidateEc2(
        TokenKey key,
        bool allowCompatibility)
    {
        if (!key.HasCoseAlgorithm)
        {
            return EcdsaKeyImporter.Validate(
                key,
                false,
                allowCompatibility);
        }

        if (IsEcdhAlgorithm(key.CoseAlgorithm))
        {
            return SuccessResult();
        }

        return EcdsaKeyImporter.Validate(
            key,
            false,
            allowCompatibility);
    }

    static TokenResult ValidateOkp(TokenKey key)
    {
        if (!key.HasCoseAlgorithm)
        {
            return SuccessResult();
        }

        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetCose(key.CoseAlgorithm);
        if (!metadata.Known)
        {
            return ValidationFailure("unknown_algorithm", "The COSE algorithm is not registered.");
        }

        bool valid;
        if (key.CoseCurve == CoseKeyLabels.CurveEd25519)
        {
            valid = key.CoseAlgorithm == CoseAlgorithms.EdDsa
                || key.CoseAlgorithm == CoseAlgorithms.Ed25519;
        }
        else if (key.CoseCurve == CoseKeyLabels.CurveEd448)
        {
            valid = key.CoseAlgorithm == CoseAlgorithms.EdDsa
                || key.CoseAlgorithm == CoseAlgorithms.Ed448;
        }
        else
        {
            valid = key.CoseAlgorithm == CoseAlgorithms.EcdhEsHkdfSha256
                || key.CoseAlgorithm == CoseAlgorithms.EcdhEsHkdfSha512
                || key.CoseAlgorithm == CoseAlgorithms.EcdhSsHkdfSha256
                || key.CoseAlgorithm == CoseAlgorithms.EcdhSsHkdfSha512
                || key.CoseAlgorithm == CoseAlgorithms.EcdhEsAes128KeyWrap
                || key.CoseAlgorithm == CoseAlgorithms.EcdhEsAes192KeyWrap
                || key.CoseAlgorithm == CoseAlgorithms.EcdhEsAes256KeyWrap
                || key.CoseAlgorithm == CoseAlgorithms.EcdhSsAes128KeyWrap
                || key.CoseAlgorithm == CoseAlgorithms.EcdhSsAes192KeyWrap
                || key.CoseAlgorithm == CoseAlgorithms.EcdhSsAes256KeyWrap;
        }

        if (!valid)
        {
            return ValidationFailure(
                "invalid_key_algorithm",
                "The COSE algorithm does not use the selected OKP curve.");
        }

        return SuccessResult();
    }

    static TokenError? ValidateLabels(CborMapEntry[]? entries)
    {
        int index = 0;
        while (index < (entries ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            CborItem? key = entries[index].Key;
            if ((key ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenCborValueKind.TextString)
            {
                index++;
                continue;
            }

            CoseIntegerValue label = ToInteger(key, "invalid_cose_key_label");
            if (label.Error != null)
            {
                return label.Error;
            }

            index++;
        }

        return null;
    }

    static TokenError? ValidateByteParameters(CborMapEntry[] entries, int[] labels)
    {
        int index = 0;
        while (index < labels.Length)
        {
            CborItem? item = Find(entries, labels[index]);
            if (item != null && item.Kind != TokenCborValueKind.ByteString)
            {
                return Error(
                    "invalid_cose_key_parameter",
                    "The COSE key parameter must be a byte string.");
            }

            index++;
        }

        return null;
    }

    static CoseIntegerValue ReadInteger(
        CborMapEntry[]? entries,
        int label,
        bool required)
    {
        CborItem? item = Find((entries ?? throw new InvalidOperationException("A required value is null.")), label);
        if (item == null)
        {
            if (required)
            {
                return IntegerFailure(
                    "missing_cose_key_parameter",
                    "A required COSE key parameter is missing.");
            }

            return new CoseIntegerValue();
        }

        CoseIntegerValue result = ToInteger(item, "invalid_cose_key_parameter");
        result.Present = result.Error == null;
        return result;
    }

    static CoseBytesValue ReadBytes(
        CborMapEntry[] entries,
        int label,
        bool required)
    {
        CborItem? item = Find(entries, label);
        if (item == null)
        {
            if (required)
            {
                return BytesFailure(
                    "missing_cose_key_parameter",
                    "A required COSE key parameter is missing.");
            }

            return new CoseBytesValue();
        }

        if (item.Kind != TokenCborValueKind.ByteString)
        {
            return BytesFailure(
                "invalid_cose_key_parameter",
                "The COSE key parameter must be a byte string.");
        }

        CoseBytesValue result = new CoseBytesValue();
        result.Present = true;
        result.Bytes = item.Bytes.AsSpan().ToArray();
        return result;
    }

    static CoseOperationsValue ReadOperations(CborMapEntry[] entries)
    {
        CborItem? item = Find(entries, CoseKeyLabels.KeyOperations);
        CoseOperationsValue result = new CoseOperationsValue();
        if (item == null)
        {
            result.Operations = System.Array.Empty<int>();
            return result;
        }

        if (item.Kind != TokenCborValueKind.Array)
        {
            result.Error = Error(
                "invalid_cose_key_operations",
                "The COSE key operations parameter must be an array.");
            return result;
        }

        int[] operations = new int[(item.ArrayValue ?? throw new InvalidOperationException("A required value is null.")).Length];
        int index = 0;
        while (index < operations.Length)
        {
            CoseIntegerValue operation = ToInteger(
                item.ArrayValue[index],
                "invalid_cose_key_operation");
            if (operation.Error != null)
            {
                result.Error = operation.Error;
                return result;
            }

            if (!CoseKeyLabelsOperations.IsRegisteredOperation(operation.Value))
            {
                result.Error = Error(
                    "unknown_key_operation",
                    "The COSE key contains an unsupported key operation.");
                return result;
            }

            int previous = 0;
            while (previous < index)
            {
                if (operations[previous] == operation.Value)
                {
                    result.Error = Error(
                        "duplicate_key_operation",
                        "The COSE key contains a duplicate key operation.");
                    return result;
                }

                previous++;
            }

            operations[index] = operation.Value;
            index++;
        }

        result.Operations = operations;
        return result;
    }

    static CoseIntegerValue ToInteger(CborItem? item, string code)
    {
        CoseIntegerValue result = new CoseIntegerValue();
        if ((item ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenCborValueKind.UnsignedInteger
            && item.IntegerArgument <= int.MaxValue)
        {
            result.Present = true;
            result.Value = (int)item.IntegerArgument;
            return result;
        }

        if (item.Kind == TokenCborValueKind.NegativeInteger
            && item.IntegerArgument <= int.MaxValue)
        {
            result.Present = true;
            result.Value = -1 - (int)item.IntegerArgument;
            return result;
        }

        result.Error = Error(code, "The COSE key integer is invalid or out of range.");
        return result;
    }

    static CborItem? Find(CborMapEntry[] entries, int label)
    {
        int index = 0;
        while (index < entries.Length)
        {
            CoseIntegerValue current = ToInteger(entries[index].Key, "invalid_cose_key_label");
            if (current.Error == null && current.Value == label)
            {
                return entries[index].Value;
            }

            index++;
        }

        return null;
    }

    static StringBuilder BeginJwk(string keyType)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("{\"kty\":\"");
        builder.Append(keyType);
        builder.Append('"');
        return builder;
    }

    static void AppendText(StringBuilder builder, string name, string? value)
    {
        builder.Append(",\"");
        builder.Append(name);
        builder.Append("\":\"");
        builder.Append(value);
        builder.Append('"');
    }

    static void AppendBytes(StringBuilder builder, string name, byte[]? bytes)
    {
        TokenTextResult encoded = Base64Url.Encode((bytes ?? throw new InvalidOperationException("A required value is null.")));
        AppendText(builder, name, encoded.Text);
    }

    static void AppendUnsigned(StringBuilder builder, string name, byte[]? bytes)
    {
        int index = 0;
        while (index < (bytes ?? throw new InvalidOperationException("A required value is null.")).Length - 1 && bytes[index] == 0)
        {
            index++;
        }

        byte[] minimal = bytes.AsSpan(index).ToArray();
        AppendBytes(builder, name, minimal);
        CryptographicOperations.ZeroMemory(minimal);
    }

    static void AppendOptionalBytes(
        StringBuilder builder,
        CborMapEntry[] entries,
        int label,
        string name)
    {
        CoseBytesValue value = ReadBytes(entries, label, false);
        if (value.Error == null && value.Present)
        {
            AppendBytes(builder, name, value.Bytes);
            CryptographicOperations.ZeroMemory(value.Bytes);
        }
    }

    static void AppendOptionalUnsigned(
        StringBuilder builder,
        CborMapEntry[] entries,
        int label,
        string name)
    {
        CoseBytesValue value = ReadBytes(entries, label, false);
        if (value.Error == null && value.Present)
        {
            AppendUnsigned(builder, name, value.Bytes);
            CryptographicOperations.ZeroMemory(value.Bytes);
        }
    }

    static void ClearSecretParameters(CborMapEntry[] entries, int keyType)
    {
        int index = 0;
        while (index < entries.Length)
        {
            CoseIntegerValue label = ToInteger(entries[index].Key, "invalid_cose_key_label");
            bool clear = keyType == CoseKeyLabels.KeyTypeSymmetric
                && label.Error == null
                && label.Value == CoseKeyLabels.SymmetricKey;
            if (keyType == CoseKeyLabels.KeyTypeRsa
                && label.Error == null
                && label.Value <= CoseKeyLabels.RsaPrivateExponent)
            {
                clear = true;
            }

            if ((keyType == CoseKeyLabels.KeyTypeEc2
                    || keyType == CoseKeyLabels.KeyTypeOkp)
                && label.Error == null
                && label.Value == CoseKeyLabels.PrivateKey)
            {
                clear = true;
            }

            if (keyType == CoseKeyLabels.KeyTypeAlgorithmKeyPair
                && label.Error == null
                && label.Value == CoseKeyLabels.AlgorithmKeyPairPrivateKey)
            {
                clear = true;
            }

            if (clear && (entries[index].Value ?? throw new InvalidOperationException("A required value is null.")).Kind == TokenCborValueKind.ByteString)
            {
                CryptographicOperations.ZeroMemory((entries[index].Value ?? throw new InvalidOperationException("A required value is null.")).Bytes);
            }

            index++;
        }
    }

    static bool IsOkpCurve(int curve)
    {
        return curve == CoseKeyLabels.CurveX25519
            || curve == CoseKeyLabels.CurveX448
            || curve == CoseKeyLabels.CurveEd25519
            || curve == CoseKeyLabels.CurveEd448;
    }

    static bool IsEcdhAlgorithm(int algorithm)
    {
        return algorithm == CoseAlgorithms.EcdhEsHkdfSha256
            || algorithm == CoseAlgorithms.EcdhEsHkdfSha512
            || algorithm == CoseAlgorithms.EcdhSsHkdfSha256
            || algorithm == CoseAlgorithms.EcdhSsHkdfSha512
            || algorithm == CoseAlgorithms.EcdhEsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhEsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhEsAes256KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes128KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes192KeyWrap
            || algorithm == CoseAlgorithms.EcdhSsAes256KeyWrap;
    }

    static CoseKeyResult FromJwk(JwkResult source)
    {
        if (!source.Success)
        {
            return Failure(source.Error);
        }

        CoseKeyResult result = new CoseKeyResult();
        result.Success = true;
        result.Key = source.Key;
        return result;
    }

    static CoseKeyResult FromImport(TokenKeyResult source)
    {
        if (!source.Success)
        {
            return Failure(source.Error);
        }

        CoseKeyResult result = new CoseKeyResult();
        result.Success = true;
        result.Key = source.Key;
        return result;
    }

    static CoseIntegerValue IntegerFailure(string code, string message)
    {
        CoseIntegerValue result = new CoseIntegerValue();
        result.Error = Error(code, message);
        return result;
    }

    static CoseBytesValue BytesFailure(string code, string message)
    {
        CoseBytesValue result = new CoseBytesValue();
        result.Error = Error(code, message);
        return result;
    }

    static TokenResult SuccessResult()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ValidationFailure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = Error(code, message);
        return result;
    }

    static TokenError Error(string code, string message)
    {
        return new TokenError(code, message, -1);
    }

    static CoseKeyResult Failure(string code, string message)
    {
        return Failure(Error(code, message));
    }

    static CoseKeyResult Failure(TokenError? error)
    {
        CoseKeyResult result = new CoseKeyResult();
        result.Error = error;
        return result;
    }
}
