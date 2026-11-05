using System;
using System.Numerics;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Parses and writes JSON Web Keys using normalized key containers.
/// </summary>
internal static class JsonWebKey
{
    /// <summary>Reads a strict JSON Web Key object.</summary>
    /// <param name="json">The JSON object text.</param>
    /// <returns>A result containing the parsed key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>json</c> is null.</exception>
    internal static JwkResult Read(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException("json");
        }

        JsonObjectResult parsed = JsonObject.Read(json);
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        return ReadObject(parsed.Object);
    }

    /// <summary>Reads strict UTF-8 JSON Web Key bytes.</summary>
    /// <param name="utf8">The UTF-8 JSON object bytes.</param>
    /// <returns>A result containing the parsed key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>utf8</c> is null.</exception>
    internal static JwkResult Read(byte[] utf8)
    {
        if (utf8 == null)
        {
            throw new ArgumentNullException("utf8");
        }

        JsonObjectResult parsed = JsonObject.Read(utf8);
        if (!parsed.Success)
        {
            return Failure(parsed.Error);
        }

        return ReadObject(parsed.Object);
    }

    /// <summary>Writes a compact deterministic JSON Web Key.</summary>
    /// <remarks>
    /// Symmetric keys are necessarily private. RSA and EC private members are written only when
    /// the container declares native private material. Certificate-backed keys are written with
    /// public parameters and their supplied certificate chain.
    /// </remarks>
    /// <param name="key">The key to serialize.</param>
    /// <returns>A result containing compact JSON or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenTextResult Write(TokenKey key)
    {
        return JwkWriter.Write(key, JwkWriteProjection.Complete);
    }

    /// <summary>Writes the public projection of an asymmetric JSON Web Key.</summary>
    /// <remarks>All private and symmetric key material is excluded from the result.</remarks>
    /// <param name="key">The key to serialize.</param>
    /// <returns>A result containing a public JWK or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenTextResult WritePublic(TokenKey key)
    {
        return JwkWriter.Write(key, JwkWriteProjection.Public);
    }

    /// <summary>Writes the private projection of a JSON Web Key.</summary>
    /// <remarks>The operation fails when the supplied key has no private or symmetric material.</remarks>
    /// <param name="key">The key to serialize.</param>
    /// <returns>A result containing a private JWK or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenTextResult WritePrivate(TokenKey key)
    {
        return JwkWriter.Write(key, JwkWriteProjection.Private);
    }

    /// <summary>Writes a minimal JSON Web Key containing only required key parameters.</summary>
    /// <remarks>
    /// Optional key identifiers, algorithms, use, operations, certificate chains, and certificate
    /// thumbprints are omitted. Available private material is retained.
    /// </remarks>
    /// <param name="key">The key to serialize.</param>
    /// <returns>A result containing a minimal JWK or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenTextResult WriteMinimal(TokenKey key)
    {
        return JwkWriter.Write(key, JwkWriteProjection.Minimal);
    }

    /// <summary>Calculates the RFC 7638 SHA-256 thumbprint of a JSON Web Key.</summary>
    /// <remarks>
    /// Only required key members participate. Asymmetric private material and all optional
    /// metadata are excluded, so public and private representations of one key have the same value.
    /// </remarks>
    /// <param name="key">The key whose thumbprint is calculated.</param>
    /// <returns>A result containing the 32-byte SHA-256 thumbprint or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenBytesResult Thumbprint(TokenKey key)
    {
        return JwkThumbprint.Calculate(key);
    }

    /// <summary>Calculates the RFC 9278 SHA-256 JWK thumbprint URI.</summary>
    /// <param name="key">The key identified by the URI.</param>
    /// <returns>A result containing the JWK thumbprint URI or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenTextResult ThumbprintUri(TokenKey key)
    {
        return JwkThumbprint.CalculateUri(key);
    }

    /// <summary>Validates JWK public key use and key operation metadata.</summary>
    /// <param name="key">The normalized key and metadata to validate.</param>
    /// <returns>A successful result or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>key</c> is null.</exception>
    internal static TokenResult ValidateUseAndOperations(TokenKey key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        string? algorithm = key.Algorithm;
        if (algorithm == null && key.HasCoseAlgorithm)
        {
            AlgorithmMappingResult mapping = JoseCoseAlgorithmMap.FromCose(
                key.CoseAlgorithm);
            if (!mapping.Mapped)
            {
                TokenResult failure = new TokenResult();
                failure.Error = new TokenError(
                    "unmapped_algorithm",
                    "The COSE algorithm has no equivalent JOSE algorithm name.",
                    -1);
                return failure;
            }
            else
            {
                algorithm = mapping.JoseName;
            }
        }

        return JwkKeyPolicy.Validate(key, algorithm, false);
    }

    internal static JwkResult ReadObject(JsonObject? value)
    {
        JwkTextValue keyType = ReadString(value, JwkNames.KeyType, true);
        if (keyType.Error != null)
        {
            return Failure(keyType.Error);
        }

        JwkTextValue algorithm = ReadString(value, JwkNames.Algorithm, false);
        if (algorithm.Error != null)
        {
            return Failure(algorithm.Error);
        }

        JwkResult result;
        if (string.Equals(keyType.Text, TokenKeyTypes.Symmetric, StringComparison.Ordinal))
        {
            result = ReadSymmetric(value, algorithm.Text);
        }
        else if (string.Equals(keyType.Text, TokenKeyTypes.Rsa, StringComparison.Ordinal))
        {
            result = ReadRsa(value, algorithm.Text);
        }
        else if (string.Equals(
            keyType.Text,
            TokenKeyTypes.EllipticCurve,
            StringComparison.Ordinal))
        {
            result = ReadEc(value, algorithm.Text);
        }
        else if (string.Equals(
            keyType.Text,
            TokenKeyTypes.OctetKeyPair,
            StringComparison.Ordinal))
        {
            result = ReadOkp(value, algorithm.Text);
        }
        else if (string.Equals(
            keyType.Text,
            TokenKeyTypes.AlgorithmKeyPair,
            StringComparison.Ordinal))
        {
            result = ReadAlgorithmKeyPair(value, algorithm.Text);
        }
        else
        {
            return Failure(
                "unsupported_key_type",
                "The JWK key type is not supported.");
        }

        if (!result.Success)
        {
            return result;
        }

        TokenError? commonError = ApplyCommon((value ?? throw new InvalidOperationException("A required value is null.")), result.Key, algorithm.Text);
        if (commonError != null)
        {
            (result.Key ?? throw new InvalidOperationException("A required value is null.")).ReleaseOwnedMaterial();
            return Failure(commonError);
        }

        TokenResult policy = ValidateUseAndOperations((result.Key ?? throw new InvalidOperationException("A required value is null.")));
        if (!policy.Success)
        {
            result.Key.ReleaseOwnedMaterial();
            return Failure(policy.Error);
        }

        return result;
    }

    static JwkResult ReadSymmetric(JsonObject? value, string? algorithm)
    {
        JwkBytesValue material = ReadBytes((value ?? throw new InvalidOperationException("A required value is null.")), JwkNames.SymmetricKey, true);
        if (material.Error != null)
        {
            return Failure(material.Error);
        }

        if ((material.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length == 0)
        {
            return Failure("invalid_key_material", "The symmetric JWK key is empty.");
        }

        TokenKeyResult imported = SymmetricKeyImporterOperations.Import(material.Bytes);
        if (imported.Success && algorithm != null)
        {
            TokenError? algorithmError = ValidateSymmetricAlgorithm(
                algorithm,
                material.Bytes.Length);
            if (algorithmError != null)
            {
                SymmetricKeyImporterOperations.Clear((imported.Key ?? throw new InvalidOperationException("A required value is null.")));
                CryptographicOperations.ZeroMemory(material.Bytes);
                return Failure(algorithmError);
            }

            (imported.Key ?? throw new InvalidOperationException("A required value is null.")).Algorithm = algorithm;
        }

        CryptographicOperations.ZeroMemory(material.Bytes);
        return FromImport(imported);
    }

    static JwkResult ReadRsa(JsonObject? value, string? algorithm)
    {
        JwkBytesValue modulus = ReadUnsignedInteger((value ?? throw new InvalidOperationException("A required value is null.")), JwkNames.RsaModulus, true);
        if (modulus.Error != null)
        {
            return Failure(modulus.Error);
        }

        JwkBytesValue exponent = ReadUnsignedInteger(value, JwkNames.RsaExponent, true);
        if (exponent.Error != null)
        {
            return Failure(exponent.Error);
        }

        TokenError? publicError = ValidateRsaPublic(modulus.Bytes, exponent.Bytes);
        if (publicError != null)
        {
            return Failure(publicError);
        }

        JwkBytesValue privateExponent = ReadUnsignedInteger(
            value,
            JwkNames.PrivateKey,
            false);
        if (privateExponent.Error != null)
        {
            return Failure(privateExponent.Error);
        }

        if (value.Contains(JwkNames.RsaOtherPrimes))
        {
            return Failure(
                "unsupported_rsa_multi_prime",
                "Multi-prime RSA JWKs are not supported.");
        }

        RSAParameters parameters = new RSAParameters();
        parameters.Modulus = modulus.Bytes;
        parameters.Exponent = exponent.Bytes;
        bool hasPrivate = privateExponent.Bytes != null;

        if (hasPrivate)
        {
            JwkRsaPrivateValues privateValues = ReadRsaPrivate(value, privateExponent.Bytes);
            if (privateValues.Error != null)
            {
                return Failure(privateValues.Error);
            }

            TokenError? privateError = ValidateRsaPrivate(
                modulus.Bytes,
                exponent.Bytes,
                privateValues);
            if (privateError != null)
            {
                return Failure(privateError);
            }

            int factorWidth = ((modulus.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length + 1) / 2;
            parameters.D = Fixed(privateValues.D, modulus.Bytes.Length);
            parameters.P = Fixed(privateValues.P, factorWidth);
            parameters.Q = Fixed(privateValues.Q, factorWidth);
            parameters.DP = Fixed(privateValues.DP, factorWidth);
            parameters.DQ = Fixed(privateValues.DQ, factorWidth);
            parameters.InverseQ = Fixed(privateValues.QI, factorWidth);
        }
        else if (HasRsaPrivateMember(value))
        {
            return Failure(
                "incomplete_private_key",
                "RSA private CRT members require the private exponent.");
        }

        RSA rsa = RSA.Create();
        rsa.ImportParameters(parameters);
        TokenKeyResult imported = RsaKeyImporterOperations.Import(rsa, hasPrivate);
        if (imported.Success && algorithm != null)
        {
            TokenError? algorithmError = ValidateRsaAlgorithm(algorithm);
            if (algorithmError != null)
            {
                rsa.Dispose();
                return Failure(algorithmError);
            }

            (imported.Key ?? throw new InvalidOperationException("A required value is null.")).Algorithm = algorithm;
        }

        if (!imported.Success)
        {
            rsa.Dispose();
            return Failure(imported.Error);
        }

        (imported.Key ?? throw new InvalidOperationException("A required value is null.")).OwnsNativeKey = true;
        return FromImport(imported);
    }

    static JwkResult ReadEc(JsonObject? value, string? algorithm)
    {
        JwkTextValue curveName = ReadString(value, JwkNames.Curve, true);
        if (curveName.Error != null)
        {
            return Failure(curveName.Error);
        }

        JwkEcCurve curve = GetEcCurve(curveName.Text);
        if (curve.Error != null)
        {
            return Failure(curve.Error);
        }

        JwkBytesValue x = ReadBytes((value ?? throw new InvalidOperationException("A required value is null.")), JwkNames.X, true);
        if (x.Error != null)
        {
            return Failure(x.Error);
        }

        JwkBytesValue y = ReadBytes(value, JwkNames.Y, true);
        if (y.Error != null)
        {
            return Failure(y.Error);
        }

        if ((x.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length != curve.Size || (y.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length != curve.Size)
        {
            return Failure(
                "invalid_ec_coordinate",
                "The EC coordinates do not have the required curve width.");
        }

        if (!ValidateEcPoint(curve, x.Bytes, y.Bytes))
        {
            return Failure(
                "invalid_ec_point",
                "The EC public point is not on the named curve.");
        }

        JwkBytesValue privateKey = ReadBytes(value, JwkNames.PrivateKey, false);
        if (privateKey.Error != null)
        {
            return Failure(privateKey.Error);
        }

        bool hasPrivate = privateKey.Bytes != null;
        if (hasPrivate)
        {
            if ((privateKey.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length != curve.Size)
            {
                return Failure(
                    "invalid_private_key",
                    "The EC private key does not have the required curve width.");
            }

            BigInteger scalar = ToInteger(privateKey.Bytes);
            if (scalar <= BigInteger.Zero || scalar >= curve.Order)
            {
                return Failure(
                    "invalid_private_key",
                    "The EC private scalar is outside the named curve order.");
            }

            if (!ValidateEcPrivate(curve, scalar, x.Bytes, y.Bytes))
            {
                return Failure(
                    "conflicting_key_material",
                    "The EC private scalar does not match the public point.");
            }
        }

        ECParameters parameters = new ECParameters();
        parameters.Curve = curve.NativeCurve;
        ECPoint point = new ECPoint();
        point.X = x.Bytes;
        point.Y = y.Bytes;
        parameters.Q = point;
        parameters.D = privateKey.Bytes;

        ECDsa ecdsa = ECDsa.Create(parameters);
        TokenKeyResult imported;
        if (algorithm == null)
        {
            imported = EcdsaKeyImporter.Import(ecdsa, hasPrivate);
        }
        else
        {
            imported = EcdsaKeyImporter.ImportJose(ecdsa, algorithm, hasPrivate);
        }

        if (!imported.Success)
        {
            ecdsa.Dispose();
            return Failure(imported.Error);
        }

        (imported.Key ?? throw new InvalidOperationException("A required value is null.")).OwnsNativeKey = true;
        return FromImport(imported);
    }

    static JwkResult ReadOkp(JsonObject? value, string? algorithm)
    {
        JwkTextValue curveName = ReadString(value, JwkNames.Curve, true);
        if (curveName.Error != null)
        {
            return Failure(curveName.Error);
        }

        JwkOkpCurve curve = GetOkpCurve(curveName.Text);
        if (curve.Error != null)
        {
            return Failure(curve.Error);
        }

        JwkBytesValue publicKey = ReadBytes((value ?? throw new InvalidOperationException("A required value is null.")), JwkNames.X, true);
        if (publicKey.Error != null)
        {
            return Failure(publicKey.Error);
        }

        JwkBytesValue privateKey = ReadBytes(value, JwkNames.PrivateKey, false);
        if (privateKey.Error != null)
        {
            return Failure(privateKey.Error);
        }

        if ((publicKey.Bytes ?? throw new InvalidOperationException("A required value is null.")).Length != curve.Size)
        {
            return Failure(
                "invalid_okp_key",
                "The OKP public key does not have the required curve length.");
        }

        if (privateKey.Bytes != null && privateKey.Bytes.Length != curve.Size)
        {
            return Failure(
                "invalid_okp_key",
                "The OKP private key does not have the required curve length.");
        }

        TokenError? algorithmError = ValidateOkpAlgorithm(curveName.Text, algorithm);
        if (algorithmError != null)
        {
            return Failure(algorithmError);
        }

        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.OctetKeyPair;
        key.CoseKeyType = CoseKeyLabels.KeyTypeOkp;
        key.Algorithm = algorithm;
        key.Curve = curveName.Text;
        key.CoseCurve = curve.CoseCurve;
        key.OkpPublicKey = publicKey.Bytes;
        key.OkpPrivateKey = privateKey.Bytes;
        key.NativePublicMaterial = true;
        key.NativePrivateMaterial = privateKey.Bytes != null;
        return Success(key);
    }

    static JwkResult ReadAlgorithmKeyPair(JsonObject? value, string? algorithm)
    {
        if (algorithm == null)
        {
            return Failure(
                "missing_jwk_member",
                "The AKP JWK algorithm member is required.");
        }

        JwkBytesValue publicKey = ReadBytes(
            (value ?? throw new InvalidOperationException("A required value is null.")),
            JwkNames.AlgorithmPublicKey,
            true);
        if (publicKey.Error != null)
        {
            return Failure(publicKey.Error);
        }

        JwkBytesValue privateKey = ReadBytes(
            value,
            JwkNames.AlgorithmPrivateKey,
            false);
        if (privateKey.Error != null)
        {
            return Failure(privateKey.Error);
        }

        TokenKeyResult imported = MlDsaKeyImporter.Read(
            (publicKey.Bytes ?? throw new InvalidOperationException("A required value is null.")),
            privateKey.Bytes,
            algorithm);
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

    static TokenError? ApplyCommon(JsonObject value, TokenKey? key, string? algorithm)
    {
        JwkTextValue keyId = ReadString(value, JwkNames.KeyId, false);
        if (keyId.Error != null)
        {
            return keyId.Error;
        }

        JwkTextValue use = ReadString(value, JwkNames.Use, false);
        if (use.Error != null)
        {
            return use.Error;
        }

        JwkStringArrayValue operations = ReadStringArray(
            value,
            JwkNames.KeyOperations,
            false);
        if (operations.Error != null)
        {
            return operations.Error;
        }

        JwkBytesValue sha1 = ReadBytes(value, JwkNames.X509Sha1Thumbprint, false);
        if (sha1.Error != null)
        {
            return sha1.Error;
        }

        JwkBytesValue sha256 = ReadBytes(
            value,
            JwkNames.X509Sha256Thumbprint,
            false);
        if (sha256.Error != null)
        {
            return sha256.Error;
        }

        if (sha1.Bytes != null && sha1.Bytes.Length != 20)
        {
            return Error(
                "invalid_certificate_thumbprint",
                "The x5t member must contain a 20-byte SHA-1 thumbprint.");
        }

        if (sha256.Bytes != null && sha256.Bytes.Length != 32)
        {
            return Error(
                "invalid_certificate_thumbprint",
                "The x5t#S256 member must contain a 32-byte SHA-256 thumbprint.");
        }

        (key ?? throw new InvalidOperationException("A required value is null.")).KeyId = keyId.Text;
        key.Algorithm = algorithm;
        key.Use = use.Text;
        key.KeyOperations = operations.Values;
        key.CertificateSha1Thumbprint = sha1.Bytes;
        key.CertificateSha256Thumbprint = sha256.Bytes;
        return null;
    }

    static JwkRsaPrivateValues ReadRsaPrivate(JsonObject value, byte[]? d)
    {
        JwkRsaPrivateValues result = new JwkRsaPrivateValues();
        result.D = d;
        JwkBytesValue p = ReadUnsignedInteger(value, JwkNames.RsaFirstPrime, true);
        if (p.Error != null)
        {
            result.Error = p.Error;
            return result;
        }

        JwkBytesValue q = ReadUnsignedInteger(value, JwkNames.RsaSecondPrime, true);
        if (q.Error != null)
        {
            result.Error = q.Error;
            return result;
        }

        JwkBytesValue dp = ReadUnsignedInteger(value, JwkNames.RsaFirstExponent, true);
        if (dp.Error != null)
        {
            result.Error = dp.Error;
            return result;
        }

        JwkBytesValue dq = ReadUnsignedInteger(value, JwkNames.RsaSecondExponent, true);
        if (dq.Error != null)
        {
            result.Error = dq.Error;
            return result;
        }

        JwkBytesValue qi = ReadUnsignedInteger(value, JwkNames.RsaCoefficient, true);
        if (qi.Error != null)
        {
            result.Error = qi.Error;
            return result;
        }

        result.P = p.Bytes;
        result.Q = q.Bytes;
        result.DP = dp.Bytes;
        result.DQ = dq.Bytes;
        result.QI = qi.Bytes;
        return result;
    }

    static TokenError? ValidateRsaPublic(byte[]? modulus, byte[]? exponent)
    {
        int bits = UnsignedBitLength(modulus);
        if (bits < RsaKeyImporter.MinimumKeySizeInBits)
        {
            return Error(
                "weak_key",
                "The RSA modulus is shorter than the required minimum size.");
        }

        BigInteger e = ToInteger(exponent);
        BigInteger n = ToInteger(modulus);
        if (e < new BigInteger(3) || e >= n || e.IsEven)
        {
            return Error(
                "invalid_rsa_exponent",
                "The RSA public exponent must be an odd integer from 3 through n minus 1.");
        }

        return null;
    }

    static TokenError? ValidateRsaPrivate(
        byte[]? modulus,
        byte[]? exponent,
        JwkRsaPrivateValues values)
    {
        BigInteger n = ToInteger(modulus);
        BigInteger e = ToInteger(exponent);
        BigInteger d = ToInteger(values.D);
        BigInteger p = ToInteger(values.P);
        BigInteger q = ToInteger(values.Q);
        BigInteger dp = ToInteger(values.DP);
        BigInteger dq = ToInteger(values.DQ);
        BigInteger qi = ToInteger(values.QI);

        int factorWidth = ((modulus ?? throw new InvalidOperationException("A required value is null.")).Length + 1) / 2;
        if ((values.D ?? throw new InvalidOperationException("A required value is null.")).Length > modulus.Length
            || (values.P ?? throw new InvalidOperationException("A required value is null.")).Length > factorWidth
            || (values.Q ?? throw new InvalidOperationException("A required value is null.")).Length > factorWidth
            || (values.DP ?? throw new InvalidOperationException("A required value is null.")).Length > factorWidth
            || (values.DQ ?? throw new InvalidOperationException("A required value is null.")).Length > factorWidth
            || (values.QI ?? throw new InvalidOperationException("A required value is null.")).Length > factorWidth)
        {
            return Error(
                "invalid_private_key",
                "An RSA private component exceeds the supported modulus width.");
        }

        if (p <= new BigInteger(2) || q <= new BigInteger(2) || p.IsEven || q.IsEven)
        {
            return Error("invalid_private_key", "The RSA prime factors are invalid.");
        }

        if (p * q != n)
        {
            return Error(
                "conflicting_key_material",
                "The RSA prime factors do not produce the public modulus.");
        }

        BigInteger pMinusOne = p - BigInteger.One;
        BigInteger qMinusOne = q - BigInteger.One;
        BigInteger gcd = BigInteger.GreatestCommonDivisor(pMinusOne, qMinusOne);
        BigInteger lcm = (pMinusOne / gcd) * qMinusOne;
        if (d <= BigInteger.Zero || d >= n || (e * d) % lcm != BigInteger.One)
        {
            return Error("invalid_private_key", "The RSA private exponent is invalid.");
        }

        if (dp != d % pMinusOne || dq != d % qMinusOne)
        {
            return Error(
                "conflicting_key_material",
                "The RSA CRT exponents do not match the private exponent.");
        }

        if ((qi * q) % p != BigInteger.One)
        {
            return Error(
                "conflicting_key_material",
                "The RSA CRT coefficient is invalid.");
        }

        return null;
    }

    static TokenError? ValidateSymmetricAlgorithm(string algorithm, int length)
    {
        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetJose(algorithm);
        if (!metadata.Known)
        {
            return Error("unknown_algorithm", "The JOSE algorithm is not registered.");
        }

        if (metadata.Prohibited)
        {
            return Error(
                "prohibited_algorithm",
                "The JOSE algorithm is prohibited for symmetric JWK import.");
        }

        int required = 0;
        bool exact = false;
        if (string.Equals(algorithm, JoseAlgorithms.HmacSha1, StringComparison.Ordinal))
        {
            required = 20;
        }
        else if (string.Equals(algorithm, JoseAlgorithms.HmacSha256, StringComparison.Ordinal))
        {
            required = 32;
        }
        else if (string.Equals(algorithm, JoseAlgorithms.HmacSha384, StringComparison.Ordinal))
        {
            required = 48;
        }
        else if (string.Equals(algorithm, JoseAlgorithms.HmacSha512, StringComparison.Ordinal))
        {
            required = 64;
        }
        else if (string.Equals(algorithm, JoseAlgorithms.Aes128KeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes128GcmKeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes128Gcm, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes128Cbc, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes128Ctr, StringComparison.Ordinal))
        {
            required = 16;
            exact = true;
        }
        else if (string.Equals(algorithm, JoseAlgorithms.Aes192KeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes192GcmKeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes192Gcm, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes192Cbc, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes192Ctr, StringComparison.Ordinal))
        {
            required = 24;
            exact = true;
        }
        else if (string.Equals(algorithm, JoseAlgorithms.Aes256KeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes256GcmKeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes256Gcm, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes256Cbc, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.Aes256Ctr, StringComparison.Ordinal))
        {
            required = 32;
            exact = true;
        }
        else if (string.Equals(
            algorithm,
            JoseAlgorithms.Aes128CbcHmacSha256,
            StringComparison.Ordinal))
        {
            required = 32;
            exact = true;
        }
        else if (string.Equals(
            algorithm,
            JoseAlgorithms.Aes192CbcHmacSha384,
            StringComparison.Ordinal))
        {
            required = 48;
            exact = true;
        }
        else if (string.Equals(
            algorithm,
            JoseAlgorithms.Aes256CbcHmacSha512,
            StringComparison.Ordinal))
        {
            required = 64;
            exact = true;
        }
        else if (string.Equals(algorithm, JoseAlgorithms.Direct, StringComparison.Ordinal))
        {
            required = SymmetricKeyImporter.MinimumKeySizeInBytes;
        }
        else
        {
            return Error(
                "invalid_key_algorithm",
                "The JOSE algorithm does not directly use a symmetric key.");
        }

        if (exact && length != required)
        {
            return Error(
                "invalid_key_size",
                "The symmetric JWK key does not have the exact algorithm size.");
        }

        if (!exact && length < required)
        {
            return Error(
                "weak_key",
                "The symmetric JWK key is shorter than the algorithm minimum.");
        }

        return null;
    }

    static TokenError? ValidateRsaAlgorithm(string algorithm)
    {
        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetJose(algorithm);
        if (!metadata.Known)
        {
            return Error("unknown_algorithm", "The JOSE algorithm is not registered.");
        }

        if (metadata.Prohibited)
        {
            return Error(
                "prohibited_algorithm",
                "The JOSE algorithm is prohibited for RSA JWK import.");
        }

        if (metadata.CompatibilityOnly)
        {
            return Error(
                "compatibility_required",
                "The JOSE algorithm requires explicit compatibility policy.");
        }

        bool valid = string.Equals(algorithm, JoseAlgorithms.RsaSha1, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaSha512, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPssSha512, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaPkcs1, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaep, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaepSha256, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaepSha384, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.RsaOaepSha512, StringComparison.Ordinal);
        if (!valid)
        {
            return Error(
                "invalid_key_algorithm",
                "The JOSE algorithm does not use an RSA key.");
        }

        return null;
    }

    static bool HasRsaPrivateMember(JsonObject value)
    {
        return value.Contains(JwkNames.RsaFirstPrime)
            || value.Contains(JwkNames.RsaSecondPrime)
            || value.Contains(JwkNames.RsaFirstExponent)
            || value.Contains(JwkNames.RsaSecondExponent)
            || value.Contains(JwkNames.RsaCoefficient);
    }

    static byte[]? Fixed(byte[]? value, int width)
    {
        TokenBytesResult fixedValue = BigEndianInteger.ToFixedWidth((value ?? throw new InvalidOperationException("A required value is null.")), width);
        return fixedValue.Bytes;
    }

    static int UnsignedBitLength(byte[]? value)
    {
        if ((value ?? throw new InvalidOperationException("A required value is null.")).Length == 0)
        {
            return 0;
        }

        int leading = 0;
        int mask = 128;
        while (mask > 0 && (value[0] & mask) == 0)
        {
            leading++;
            mask >>= 1;
        }

        return value.Length * 8 - leading;
    }

    static bool ValidateEcPoint(JwkEcCurve curve, byte[] xBytes, byte[] yBytes)
    {
        BigInteger x = ToInteger(xBytes);
        BigInteger y = ToInteger(yBytes);
        if (x < BigInteger.Zero || x >= curve.Prime
            || y < BigInteger.Zero || y >= curve.Prime)
        {
            return false;
        }

        BigInteger left = Mod(y * y, curve.Prime);
        BigInteger right = Mod(x * x * x + curve.A * x + curve.B, curve.Prime);
        return left == right;
    }

    static bool ValidateEcPrivate(
        JwkEcCurve curve,
        BigInteger scalar,
        byte[] xBytes,
        byte[] yBytes)
    {
        JwkEcPoint point = Multiply(curve, scalar);
        return !point.Infinity
            && point.X == ToInteger(xBytes)
            && point.Y == ToInteger(yBytes);
    }

    static JwkEcPoint Multiply(JwkEcCurve curve, BigInteger scalar)
    {
        JwkEcPoint result = new JwkEcPoint();
        result.Infinity = true;
        JwkEcPoint addend = new JwkEcPoint();
        addend.X = curve.GeneratorX;
        addend.Y = curve.GeneratorY;

        while (scalar > BigInteger.Zero)
        {
            if (!scalar.IsEven)
            {
                result = Add(curve, result, addend);
            }

            addend = Add(curve, addend, addend);
            scalar >>= 1;
        }

        return result;
    }

    static JwkEcPoint Add(JwkEcCurve curve, JwkEcPoint left, JwkEcPoint right)
    {
        if (left.Infinity)
        {
            return CopyPoint(right);
        }

        if (right.Infinity)
        {
            return CopyPoint(left);
        }

        if (left.X == right.X && Mod(left.Y + right.Y, curve.Prime) == BigInteger.Zero)
        {
            JwkEcPoint infinity = new JwkEcPoint();
            infinity.Infinity = true;
            return infinity;
        }

        BigInteger numerator;
        BigInteger denominator;
        if (left.X == right.X && left.Y == right.Y)
        {
            numerator = new BigInteger(3) * left.X * left.X + curve.A;
            denominator = new BigInteger(2) * left.Y;
        }
        else
        {
            numerator = right.Y - left.Y;
            denominator = right.X - left.X;
        }

        BigInteger inverse = BigInteger.ModPow(
            Mod(denominator, curve.Prime),
            curve.Prime - new BigInteger(2),
            curve.Prime);
        BigInteger slope = Mod(numerator * inverse, curve.Prime);
        JwkEcPoint result = new JwkEcPoint();
        result.X = Mod(slope * slope - left.X - right.X, curve.Prime);
        result.Y = Mod(slope * (left.X - result.X) - left.Y, curve.Prime);
        return result;
    }

    static JwkEcPoint CopyPoint(JwkEcPoint value)
    {
        JwkEcPoint copy = new JwkEcPoint();
        copy.X = value.X;
        copy.Y = value.Y;
        copy.Infinity = value.Infinity;
        return copy;
    }

    static BigInteger Mod(BigInteger value, BigInteger modulus)
    {
        BigInteger result = value % modulus;
        if (result < BigInteger.Zero)
        {
            result += modulus;
        }

        return result;
    }

    internal static JwkEcCurve GetEcCurve(string? name)
    {
        if (string.Equals(name, "P-256", StringComparison.Ordinal))
        {
            return EcCurve(
                name,
                32,
                ECCurve.NamedCurves.nistP256,
                "FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFF",
                "FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFC",
                "5AC635D8AA3A93E7B3EBBD55769886BC651D06B0CC53B0F63BCE3C3E27D2604B",
                "FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551",
                "6B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296",
                "4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5");
        }

        if (string.Equals(name, "P-384", StringComparison.Ordinal))
        {
            return EcCurve(
                name,
                48,
                ECCurve.NamedCurves.nistP384,
                "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFFFF0000000000000000FFFFFFFF",
                "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFFFF0000000000000000FFFFFFFC",
                "B3312FA7E23EE7E4988E056BE3F82D19181D9C6EFE8141120314088F5013875AC656398D8A2ED19D2A85C8EDD3EC2AEF",
                "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFC7634D81F4372DDF581A0DB248B0A77AECEC196ACCC52973",
                "AA87CA22BE8B05378EB1C71EF320AD746E1D3B628BA79B9859F741E082542A385502F25DBF55296C3A545E3872760AB7",
                "3617DE4A96262C6F5D9E98BF9292DC29F8F41DBD289A147CE9DA3113B5F0B8C00A60B1CE1D7E819D7A431D7C90EA0E5F");
        }

        if (string.Equals(name, "P-521", StringComparison.Ordinal))
        {
            return EcCurve(
                name,
                66,
                ECCurve.NamedCurves.nistP521,
                "01FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF",
                "01FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFC",
                "0051953EB9618E1C9A1F929A21A0B68540EEA2DA725B99B315F3B8B489918EF109E156193951EC7E937B1652C0BD3BB1BF073573DF883D2C34F1EF451FD46B503F00",
                "01FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFA51868783BF2F966B7FCC0148F709A5D03BB5C9B8899C47AEBB6FB71E91386409",
                "00C6858E06B70404E9CD9E3ECB662395B4429C648139053FB521F828AF606B4D3DBAA14B5E77EFE75928FE1DC127A2FFA8DE3348B3C1856A429BF97E7E31C2E5BD66",
                "011839296A789A3BC0045C8A5FB42C7D1BD998F54449579B446817AFBD17273E662C97EE72995EF42640C550B9013FAD0761353C7086A272C24088BE94769FD16650");
        }

        if (string.Equals(name, "secp256k1", StringComparison.Ordinal))
        {
            return EcCurve(
                name,
                32,
                ECCurve.CreateFromValue("1.3.132.0.10"),
                "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F",
                "00",
                "07",
                "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364141",
                "79BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798",
                "483ADA7726A3C4655DA4FBFC0E1108A8FD17B448A68554199C47D08FFB10D4B8");
        }

        JwkEcCurve result = new JwkEcCurve();
        result.Error = Error(
            "unsupported_curve",
            "The EC JWK curve is not supported.");
        return result;
    }

    internal static JwkBytesValue DecompressEcY(
        string curveName,
        byte[]? xBytes,
        bool odd)
    {
        JwkEcCurve curve = GetEcCurve(curveName);
        if (curve.Error != null)
        {
            JwkBytesValue unsupported = new JwkBytesValue();
            unsupported.Error = curve.Error;
            return unsupported;
        }

        if (xBytes == null || xBytes.Length != curve.Size)
        {
            JwkBytesValue invalidX = new JwkBytesValue();
            invalidX.Error = Error(
                "invalid_ec_coordinate",
                "The EC x-coordinate does not have the required curve width.");
            return invalidX;
        }

        BigInteger x = ToInteger(xBytes);
        BigInteger squared = Mod(x * x * x + curve.A * x + curve.B, curve.Prime);
        BigInteger y = BigInteger.ModPow(
            squared,
            (curve.Prime + BigInteger.One) / new BigInteger(4),
            curve.Prime);
        if (Mod(y * y, curve.Prime) != squared)
        {
            JwkBytesValue invalidPoint = new JwkBytesValue();
            invalidPoint.Error = Error(
                "invalid_ec_point",
                "The compressed EC point is not on the named curve.");
            return invalidPoint;
        }

        if ((!y.IsEven) != odd)
        {
            y = curve.Prime - y;
        }

        byte[] unsigned = y.ToByteArray(true, true);
        TokenBytesResult fixedValue = BigEndianInteger.ToFixedWidth(unsigned, curve.Size);
        JwkBytesValue result = new JwkBytesValue();
        result.Bytes = fixedValue.Bytes;
        return result;
    }

    static JwkEcCurve EcCurve(
        string? name,
        int size,
        ECCurve nativeCurve,
        string prime,
        string a,
        string b,
        string order,
        string generatorX,
        string generatorY)
    {
        JwkEcCurve curve = new JwkEcCurve();
        curve.Name = name;
        curve.Size = size;
        curve.NativeCurve = nativeCurve;
        curve.Prime = HexInteger(prime);
        curve.A = HexInteger(a);
        curve.B = HexInteger(b);
        curve.Order = HexInteger(order);
        curve.GeneratorX = HexInteger(generatorX);
        curve.GeneratorY = HexInteger(generatorY);
        return curve;
    }

    internal static JwkOkpCurve GetOkpCurve(string? name)
    {
        if (string.Equals(name, "X25519", StringComparison.Ordinal))
        {
            return OkpCurve(32, CoseKeyLabels.CurveX25519);
        }

        if (string.Equals(name, "X448", StringComparison.Ordinal))
        {
            return OkpCurve(56, CoseKeyLabels.CurveX448);
        }

        if (string.Equals(name, "Ed25519", StringComparison.Ordinal))
        {
            return OkpCurve(32, CoseKeyLabels.CurveEd25519);
        }

        if (string.Equals(name, "Ed448", StringComparison.Ordinal))
        {
            return OkpCurve(57, CoseKeyLabels.CurveEd448);
        }

        JwkOkpCurve result = new JwkOkpCurve();
        result.Error = Error(
            "unsupported_curve",
            "The OKP JWK curve is not supported.");
        return result;
    }

    static JwkOkpCurve OkpCurve(int size, int coseCurve)
    {
        JwkOkpCurve result = new JwkOkpCurve();
        result.Size = size;
        result.CoseCurve = coseCurve;
        return result;
    }

    internal static TokenError? ValidateOkpAlgorithm(string? curve, string? algorithm)
    {
        if (algorithm == null)
        {
            return null;
        }

        AlgorithmMetadata metadata = AlgorithmMetadataRegistry.GetJose(algorithm);
        if (!metadata.Known)
        {
            return Error("unknown_algorithm", "The JOSE algorithm is not registered.");
        }


        if (metadata.Prohibited)
        {
            return Error(
                "prohibited_algorithm",
                "The JOSE algorithm is prohibited for OKP JWK import.");
        }

        if (metadata.CompatibilityOnly)
        {
            return Error(
                "compatibility_required",
                "The JOSE algorithm requires explicit compatibility policy.");
        }

        bool edwards = string.Equals(curve, "Ed25519", StringComparison.Ordinal)
            || string.Equals(curve, "Ed448", StringComparison.Ordinal);
        if (edwards)
        {
            bool valid = string.Equals(algorithm, JoseAlgorithms.EdDsa, StringComparison.Ordinal)
                || string.Equals(algorithm, curve, StringComparison.Ordinal);
            if (!valid)
            {
                return Error(
                    "invalid_key_algorithm",
                    "The JOSE algorithm does not use the selected Edwards curve.");
            }

            return null;
        }

        bool ecdh = string.Equals(algorithm, JoseAlgorithms.EcdhEs, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.EcdhEsAes128KeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.EcdhEsAes192KeyWrap, StringComparison.Ordinal)
            || string.Equals(algorithm, JoseAlgorithms.EcdhEsAes256KeyWrap, StringComparison.Ordinal);
        if (!ecdh)
        {
            return Error(
                "invalid_key_algorithm",
                "The JOSE algorithm does not use a Montgomery OKP key.");
        }

        return null;
    }

    static JwkTextValue ReadString(JsonObject? value, string name, bool required)
    {
        JwkTextValue result = new JwkTextValue();
        JsonValue? member = (value ?? throw new InvalidOperationException("A required value is null.")).Get(name);
        if (member == null)
        {
            if (required)
            {
                result.Error = Error(
                    "missing_jwk_member",
                    "A required JWK member is missing: " + name + ".");
            }

            return result;
        }

        if (member.Kind != TokenJsonValueKind.String)
        {
            result.Error = Error(
                "invalid_jwk_member",
                "The JWK member must be a string: " + name + ".");
            return result;
        }

        result.Text = member.StringValue;
        return result;
    }

    static JwkBytesValue ReadBytes(JsonObject value, string name, bool required)
    {
        JwkTextValue text = ReadString(value, name, required);
        JwkBytesValue result = new JwkBytesValue();
        if (text.Error != null)
        {
            result.Error = text.Error;
            return result;
        }

        if (text.Text == null)
        {
            return result;
        }

        TokenBytesResult decoded = Base64Url.Decode(text.Text);
        if (!decoded.Success)
        {
            result.Error = decoded.Error;
            return result;
        }

        result.Bytes = decoded.Bytes;
        return result;
    }

    static JwkBytesValue ReadUnsignedInteger(JsonObject value, string name, bool required)
    {
        JwkBytesValue result = ReadBytes(value, name, required);
        if (result.Error != null || result.Bytes == null)
        {
            return result;
        }

        if (result.Bytes.Length == 0 || result.Bytes[0] == 0)
        {
            result.Bytes = null;
            result.Error = Error(
                "invalid_unsigned_integer",
                "A JWK base64urlUInt must use a non-empty minimal encoding: " + name + ".");
        }

        return result;
    }

    static JwkStringArrayValue ReadStringArray(
        JsonObject value,
        string name,
        bool required)
    {
        JwkStringArrayValue result = new JwkStringArrayValue();
        JsonValue? member = value.Get(name);
        if (member == null)
        {
            if (required)
            {
                result.Error = Error(
                    "missing_jwk_member",
                    "A required JWK member is missing: " + name + ".");
            }

            return result;
        }

        if (member.Kind != TokenJsonValueKind.Array)
        {
            result.Error = Error(
                "invalid_jwk_member",
                "The JWK member must be an array: " + name + ".");
            return result;
        }

        string[] values = new string[(member.ArrayValue ?? throw new InvalidOperationException("A required value is null.")).Length];
        int index = 0;
        while (index < member.ArrayValue.Length)
        {
            if (member.ArrayValue[index].Kind != TokenJsonValueKind.String)
            {
                result.Error = Error(
                    "invalid_jwk_member",
                    "Every key_ops item must be a string.");
                return result;
            }

            values[index] = (member.ArrayValue[index].StringValue ?? throw new InvalidOperationException("A required value is null."));
            index++;
        }

        result.Values = values;
        return result;
    }

    static BigInteger ToInteger(byte[]? value)
    {
        return new BigInteger(value, true, true);
    }

    static BigInteger HexInteger(string value)
    {
        return ToInteger(Convert.FromHexString(value));
    }

    static JwkResult FromImport(TokenKeyResult imported)
    {
        if (!imported.Success)
        {
            return Failure(imported.Error);
        }

        return Success(imported.Key);
    }

    static JwkResult Success(TokenKey? key)
    {
        JwkResult result = new JwkResult();
        result.Success = true;
        result.Key = key;
        return result;
    }

    static JwkResult Failure(string code, string message)
    {
        return Failure(Error(code, message));
    }

    static JwkResult Failure(TokenError? error)
    {
        JwkResult result = new JwkResult();
        result.Error = error;
        return result;
    }

    static TokenError Error(string code, string message)
    {
        return new TokenError(code, message, -1);
    }
}
