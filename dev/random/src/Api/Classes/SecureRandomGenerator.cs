using System;

using System.Numerics;
using System.Security.Cryptography;

namespace random;

/// <summary>
/// Provides security-sensitive random bytes and unbiased integers from an operating-system source or HMAC-DRBG.
/// </summary>
/// <remarks>
/// Instances are mutable, owned by one caller at a time, and are not implicitly thread-safe.
/// This type deliberately provides no deterministic state, clone, jump, seek, spawn, or weak scalar-seed API.
/// Output is outside the deterministic replay contract even when caller-supplied material is used to
/// instantiate HMAC-DRBG. The library and its HMAC-DRBG wrapper make no FIPS 140 validation claim;
/// callers remain responsible for entropy quality, secret handling, and application-specific limits.
/// </remarks>
public class SecureRandomGenerator : ISecureRandomGenerator
{
    RandomAlgorithm algorithm;
    SystemCryptoEngine? systemCrypto;
    HmacDrbgSha256Engine? hmacDrbg;
    long uuid7Timestamp;
    int uuid7Counter;
    bool uuid7Initialized;

    internal SecureRandomGenerator(SystemCryptoEngine? systemCrypto, HmacDrbgSha256Engine? hmacDrbg)
    {
        if ((systemCrypto == null) == (hmacDrbg == null))
        {
            throw RandomArgumentException.Create("systemCrypto", "Exactly one secure random engine must be supplied.");
        }

        algorithm = systemCrypto == null ? RandomAlgorithm.HmacDrbgSha256V1 : RandomAlgorithm.SystemCryptoV1;
        this.systemCrypto = systemCrypto;
        this.hmacDrbg = hmacDrbg;
    }

    /// <summary>
    /// Creates a generator that obtains every request directly from the operating-system cryptographic source.
    /// </summary>
    /// <returns>A non-replayable system cryptographic generator.</returns>
    /// <remarks>Operating-system failures propagate and never fall back to deterministic data.</remarks>
    internal static SecureRandomGenerator CreateSystem()
    {
        return new SecureRandomGenerator(new SystemCryptoEngine(), null);
    }

    /// <summary>
    /// Creates an HMAC-SHA-256 DRBG instantiated from fresh operating-system entropy and nonce bytes.
    /// </summary>
    /// <returns>A non-replayable HMAC-DRBG generator.</returns>
    /// <remarks>Operating-system failures propagate and never fall back to deterministic data.</remarks>
    internal static SecureRandomGenerator CreateHmacDrbg()
    {
        byte[] entropy = new byte[HmacDrbgSha256Engine.MinimumEntropyBytes];
        byte[] nonce = new byte[HmacDrbgSha256Engine.MinimumNonceBytes];
        SystemCryptoEngine source = new SystemCryptoEngine();
        source.FillBytes(entropy);
        source.FillBytes(nonce);
        HmacDrbgSha256Engine engine = new HmacDrbgSha256Engine(entropy, nonce, new byte[0]);
        CryptographicOperations.ZeroMemory(entropy);
        CryptographicOperations.ZeroMemory(nonce);
        return new SecureRandomGenerator(null, engine);
    }

    /// <summary>
    /// Creates an HMAC-SHA-256 DRBG from caller-supplied entropy, nonce, and personalization bytes.
    /// </summary>
    /// <param name="entropy">At least 32 bytes containing 256 bits of entropy.</param>
    /// <param name="nonce">At least 16 nonce bytes suitable for this instantiation.</param>
    /// <param name="personalization">Optional personalization bytes, represented by an empty array when absent.</param>
    /// <returns>An HMAC-DRBG generator instantiated from copies of the supplied values.</returns>
    /// <remarks>
    /// Length validation cannot establish that caller data is unpredictable. Callers are responsible for using
    /// an approved entropy source and for never treating fixed test vectors as security-grade instantiation data.
    /// </remarks>
    /// <exception cref="RandomArgumentException">An argument is null or outside the HMAC-DRBG size policy.</exception>
    internal static SecureRandomGenerator CreateHmacDrbg(byte[] entropy, byte[] nonce, byte[] personalization)
    {
        return new SecureRandomGenerator(null, new HmacDrbgSha256Engine(entropy, nonce, personalization));
    }

    /// <summary>
    /// Returns metadata for the selected cryptographic algorithm.
    /// </summary>
    /// <returns>Cryptographic algorithm metadata with no state-export capability.</returns>
    public RandomAlgorithmInfo Info()
    {
        if (algorithm == RandomAlgorithm.SystemCryptoV1)
        {
            return SystemCrypto().Info();
        }

        return HmacDrbg().Info();
    }

    /// <summary>
    /// Returns the HMAC-DRBG reseed counter.
    /// </summary>
    /// <returns>
    /// One immediately after instantiation or reseeding, increased once after each
    /// successful nonempty generate request.
    /// </returns>
    /// <remarks>The direct operating-system source has no request counter and rejects this operation.</remarks>
    /// <exception cref="RandomCapabilityException">The selected algorithm has no reseed counter.</exception>
    /// <exception cref="RandomSecurityException">The HMAC-DRBG has been uninstantiated.</exception>
    public ulong ReseedCounter()
    {
        HmacDrbgSha256Engine selectedDrbg = ActiveHmacDrbgForReseedStatus();
        return selectedDrbg.ReseedCounter();
    }

    /// <summary>
    /// Reports whether the HMAC-DRBG must be reseeded before its next ordinary nonempty request.
    /// </summary>
    /// <returns><c>true</c> when the reseed counter exceeds the supported request interval; otherwise <c>false</c>.</returns>
    /// <remarks>The direct operating-system source has no reseed interval and rejects this operation.</remarks>
    /// <exception cref="RandomCapabilityException">The selected algorithm has no reseed interval.</exception>
    /// <exception cref="RandomSecurityException">The HMAC-DRBG has been uninstantiated.</exception>
    public bool IsReseedRequired()
    {
        HmacDrbgSha256Engine selectedDrbg = ActiveHmacDrbgForReseedStatus();
        return selectedDrbg.ReseedCounter() > HmacDrbgSha256Engine.ReseedInterval;
    }

    /// <summary>
    /// Reseeds an HMAC-DRBG with fresh operating-system entropy.
    /// </summary>
    /// <remarks>The direct system source has no mutable state and rejects this operation.</remarks>
    /// <exception cref="RandomCapabilityException">The selected algorithm does not support reseeding.</exception>
    /// <exception cref="RandomSecurityException">The HMAC-DRBG has been uninstantiated.</exception>
    public void Reseed()
    {
        ReseedWithAdditionalInput(new byte[0]);
    }

    /// <summary>
    /// Reseeds an HMAC-DRBG with fresh operating-system entropy and caller-provided additional input.
    /// </summary>
    /// <param name="additionalInput">Additional input mixed into the reseed operation, or an empty array.</param>
    /// <remarks>The caller-owned additional input is not modified.</remarks>
    /// <exception cref="RandomArgumentException">The additional input is null or too large.</exception>
    /// <exception cref="RandomCapabilityException">The selected algorithm does not support reseeding.</exception>
    /// <exception cref="RandomSecurityException">The HMAC-DRBG has been uninstantiated.</exception>
    public void ReseedWithAdditionalInput(byte[] additionalInput)
    {
        Guard.NotNull(additionalInput, "additionalInput");
        HmacDrbgSha256Engine.ValidateOptionalInputLength(additionalInput.Length, "additionalInput");
        if (algorithm != RandomAlgorithm.HmacDrbgSha256V1)
        {
            throw RandomCapabilityException.Create(algorithm, RandomCapability.Reseed, "The selected secure algorithm does not support reseeding.");
        }

        HmacDrbgSha256Engine selectedDrbg = HmacDrbg();
        selectedDrbg.EnsureInstantiated();
        byte[] entropy = new byte[HmacDrbgSha256Engine.MinimumEntropyBytes];
        SystemCryptoEngine source = new SystemCryptoEngine();
        source.FillBytes(entropy);
        selectedDrbg.Reseed(entropy, additionalInput);
        CryptographicOperations.ZeroMemory(entropy);
    }

    /// <summary>
    /// Generates one cryptographic byte request.
    /// </summary>
    /// <param name="destination">The destination bytes.</param>
    /// <remarks>An HMAC-DRBG request is limited to 65,536 bytes. An empty request changes no state.</remarks>
    /// <exception cref="RandomArgumentException">The HMAC-DRBG request is too large.</exception>
    /// <exception cref="RandomSecurityException">The HMAC-DRBG has been uninstantiated.</exception>
    public void Generate(Span<byte> destination)
    {
        GenerateWithOptions(destination, new byte[0], false);
    }

    /// <summary>
    /// Generates one cryptographic byte request with optional HMAC-DRBG input and prediction resistance.
    /// </summary>
    /// <param name="destination">The destination bytes.</param>
    /// <param name="additionalInput">Additional input mixed into an HMAC-DRBG request, or an empty array.</param>
    /// <param name="predictionResistance">Whether an HMAC-DRBG must obtain fresh entropy before generating.</param>
    /// <remarks>
    /// Additional input is not entropy. The direct system source accepts only empty additional input and
    /// <c>false</c> prediction resistance because it has no DRBG state to update or reseed.
    /// </remarks>
    /// <exception cref="RandomArgumentException">Additional input is null or an HMAC-DRBG size limit is exceeded.</exception>
    /// <exception cref="RandomCapabilityException">The selected algorithm does not support a requested option.</exception>
    /// <exception cref="RandomSecurityException">The HMAC-DRBG has been uninstantiated.</exception>
    public void GenerateWithOptions(Span<byte> destination, byte[] additionalInput, bool predictionResistance)
    {
        Guard.NotNull(additionalInput, "additionalInput");
        if (algorithm == RandomAlgorithm.SystemCryptoV1)
        {
            if (additionalInput.Length != 0)
            {
                throw RandomCapabilityException.Create(algorithm, RandomCapability.AdditionalInput, "The direct system source does not accept additional input.");
            }

            if (predictionResistance)
            {
                throw RandomCapabilityException.Create(algorithm, RandomCapability.PredictionResistance, "The direct system source does not expose a prediction-resistance option.");
            }

            SystemCrypto().FillBytes(destination);
            return;
        }

        HmacDrbgSha256Engine.ValidateRequestLength(destination.Length);
        HmacDrbgSha256Engine.ValidateOptionalInputLength(additionalInput.Length, "additionalInput");
        HmacDrbgSha256Engine selectedDrbg = HmacDrbg();
        selectedDrbg.EnsureInstantiated();
        if (destination.Length == 0)
        {
            return;
        }

        if (!predictionResistance && selectedDrbg.ReseedCounter() > HmacDrbgSha256Engine.ReseedInterval)
        {
            Reseed();
        }

        selectedDrbg.GenerateWithOptions(destination, additionalInput, predictionResistance);
    }

    /// <summary>
    /// Creates an array filled with cryptographic random bytes.
    /// </summary>
    /// <param name="length">The nonnegative byte count.</param>
    /// <returns>A new byte array of the requested length.</returns>
    /// <exception cref="RandomArgumentException">The length is negative.</exception>
    /// <exception cref="RandomSecurityException">The HMAC-DRBG has been uninstantiated.</exception>
    public byte[] NextBytes(int length)
    {
        Guard.Count(length, "length");
        byte[] result = new byte[length];
        FillBytes(result.AsSpan());
        return result;
    }

    /// <summary>
    /// Fills any length destination with cryptographic random bytes.
    /// </summary>
    /// <param name="destination">The destination bytes.</param>
    /// <remarks>HMAC-DRBG output is split into compliant requests of at most 65,536 bytes.</remarks>
    /// <exception cref="RandomSecurityException">The HMAC-DRBG has been uninstantiated.</exception>
    public void FillBytes(Span<byte> destination)
    {
        if (algorithm == RandomAlgorithm.SystemCryptoV1)
        {
            SystemCrypto().FillBytes(destination);
            return;
        }

        if (destination.Length == 0)
        {
            Generate(destination);
            return;
        }

        int offset = 0;
        while (offset < destination.Length)
        {
            int count = destination.Length - offset;
            if (count > HmacDrbgSha256Engine.MaximumRequestBytes)
            {
                count = HmacDrbgSha256Engine.MaximumRequestBytes;
            }

            Generate(destination.Slice(offset, count));
            offset += count;
        }
    }

    /// <summary>
    /// Returns an unbiased cryptographic byte below an exclusive bound.
    /// </summary>
    /// <param name="bound">The positive exclusive upper bound.</param>
    /// <returns>A uniformly distributed byte in <c>[0, bound)</c>.</returns>
    /// <remarks>A bound of one returns zero without requesting random bytes.</remarks>
    /// <exception cref="RandomArgumentException">The bound is zero.</exception>
    /// <exception cref="RandomSecurityException">The HMAC-DRBG has been uninstantiated.</exception>
    public byte NextByteBounded(byte bound)
    {
        return SecureIntegerUniform.NextByteBounded(this, bound);
    }

    /// <summary>
    /// Returns an unbiased cryptographic byte in a half-open range.
    /// </summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The exclusive upper bound.</param>
    /// <returns>A uniformly distributed byte in <c>[minimum, maximum)</c>.</returns>
    /// <remarks>A range containing one value returns it without requesting random bytes.</remarks>
    /// <exception cref="RandomArgumentException">The maximum is not greater than the minimum.</exception>
    /// <exception cref="RandomSecurityException">The HMAC-DRBG has been uninstantiated.</exception>
    public byte NextByteInRange(byte minimum, byte maximum)
    {
        return SecureIntegerUniform.NextByteInRange(this, minimum, maximum);
    }

    /// <summary>
    /// Returns 32 cryptographic random bits.
    /// </summary>
    /// <returns>An unsigned 32-bit value decoded from little-endian random bytes.</returns>
    public uint NextUInt32()
    {
        byte[] bytes = new byte[4];
        FillBytes(bytes.AsSpan());
        uint result = BinaryCodec.ReadUInt32(bytes, 0);
        CryptographicOperations.ZeroMemory(bytes);
        return result;
    }

    /// <summary>
    /// Returns an unbiased unsigned 32-bit integer below an exclusive bound.
    /// </summary>
    /// <param name="bound">The positive exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[0, bound)</c>.</returns>
    /// <remarks>A bound of one returns zero without requesting random bytes.</remarks>
    /// <exception cref="RandomArgumentException">The bound is zero.</exception>
    public uint NextUInt32Bounded(uint bound)
    {
        return SecureIntegerUniform.NextUInt32(this, bound);
    }

    /// <summary>
    /// Returns an unbiased unsigned 32-bit integer in a half-open range.
    /// </summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[minimum, maximum)</c>.</returns>
    /// <remarks>A one-value range returns its value without requesting random bytes.</remarks>
    /// <exception cref="RandomArgumentException">The maximum is not greater than the minimum.</exception>
    public uint NextUInt32InRange(uint minimum, uint maximum)
    {
        return SecureIntegerUniform.NextUInt32(this, minimum, maximum);
    }

    /// <summary>
    /// Returns 64 cryptographic random bits.
    /// </summary>
    /// <returns>An unsigned 64-bit value decoded from little-endian random bytes.</returns>
    public ulong NextUInt64()
    {
        byte[] bytes = new byte[8];
        FillBytes(bytes.AsSpan());
        ulong result = BinaryCodec.ReadUInt64(bytes, 0);
        CryptographicOperations.ZeroMemory(bytes);
        return result;
    }

    /// <summary>
    /// Returns an unbiased unsigned 64-bit integer below an exclusive bound.
    /// </summary>
    /// <param name="bound">The positive exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[0, bound)</c>.</returns>
    /// <remarks>A bound of one returns zero without requesting random bytes.</remarks>
    /// <exception cref="RandomArgumentException">The bound is zero.</exception>
    public ulong NextUInt64Bounded(ulong bound)
    {
        return SecureIntegerUniform.NextUInt64(this, bound);
    }

    /// <summary>
    /// Returns an unbiased unsigned 64-bit integer in a half-open range.
    /// </summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[minimum, maximum)</c>.</returns>
    /// <remarks>A one-value range returns its value without requesting random bytes.</remarks>
    /// <exception cref="RandomArgumentException">The maximum is not greater than the minimum.</exception>
    public ulong NextUInt64InRange(ulong minimum, ulong maximum)
    {
        return SecureIntegerUniform.NextUInt64(this, minimum, maximum);
    }

    /// <summary>
    /// Returns a signed value containing 32 cryptographic random bits.
    /// </summary>
    /// <returns>The same bit pattern as <see cref="NextUInt32()"/>, interpreted as a signed integer.</returns>
    public int NextInt32()
    {
        uint value = NextUInt32();
        if (value <= int.MaxValue)
        {
            return (int)value;
        }

        return int.MinValue + (int)(value - 0x80000000U);
    }

    /// <summary>
    /// Returns an unbiased nonnegative signed 32-bit integer below an exclusive bound.
    /// </summary>
    /// <param name="bound">The positive exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[0, bound)</c>.</returns>
    /// <remarks>A bound of one returns zero without requesting random bytes.</remarks>
    /// <exception cref="RandomArgumentException">The bound is not positive.</exception>
    public int NextInt32Bounded(int bound)
    {
        return SecureIntegerUniform.NextInt32(this, bound);
    }

    /// <summary>
    /// Returns an unbiased signed 32-bit integer in a half-open range.
    /// </summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[minimum, maximum)</c>.</returns>
    /// <remarks>A one-value range returns its value without requesting random bytes.</remarks>
    /// <exception cref="RandomArgumentException">The maximum is not greater than the minimum.</exception>
    public int NextInt32InRange(int minimum, int maximum)
    {
        return SecureIntegerUniform.NextInt32(this, minimum, maximum);
    }

    /// <summary>
    /// Returns a signed value containing 64 cryptographic random bits.
    /// </summary>
    /// <returns>The same bit pattern as <see cref="NextUInt64()"/>, interpreted as a signed integer.</returns>
    public long NextInt64()
    {
        ulong value = NextUInt64();
        if (value <= long.MaxValue)
        {
            return (long)value;
        }

        return long.MinValue + (long)(value - 0x8000000000000000UL);
    }

    /// <summary>
    /// Returns an unbiased nonnegative signed 64-bit integer below an exclusive bound.
    /// </summary>
    /// <param name="bound">The positive exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[0, bound)</c>.</returns>
    /// <remarks>A bound of one returns zero without requesting random bytes.</remarks>
    /// <exception cref="RandomArgumentException">The bound is not positive.</exception>
    public long NextInt64Bounded(long bound)
    {
        return SecureIntegerUniform.NextInt64(this, bound);
    }

    /// <summary>
    /// Returns an unbiased signed 64-bit integer in a half-open range.
    /// </summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[minimum, maximum)</c>.</returns>
    /// <remarks>A one-value range returns its value without requesting random bytes.</remarks>
    /// <exception cref="RandomArgumentException">The maximum is not greater than the minimum.</exception>
    public long NextInt64InRange(long minimum, long maximum)
    {
        return SecureIntegerUniform.NextInt64(this, minimum, maximum);
    }

    /// <summary>
    /// Returns an unbiased nonnegative arbitrary-precision integer below an exclusive bound.
    /// </summary>
    /// <param name="bound">The positive exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[0, bound)</c>.</returns>
    /// <remarks>A bound of one returns zero without requesting random bytes.</remarks>
    /// <exception cref="RandomArgumentException">The bound is not positive.</exception>
    public BigInteger NextBigInteger(BigInteger bound)
    {
        return SecureIntegerUniform.NextBigInteger(this, bound);
    }

    /// <summary>
    /// Returns an unbiased arbitrary-precision integer in a half-open range.
    /// </summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[minimum, maximum)</c>.</returns>
    /// <remarks>A one-value range returns its value without requesting random bytes.</remarks>
    /// <exception cref="RandomArgumentException">The maximum is not greater than the minimum.</exception>
    public BigInteger NextBigIntegerInRange(BigInteger minimum, BigInteger maximum)
    {
        return SecureIntegerUniform.NextBigInteger(this, minimum, maximum);
    }

    /// <summary>
    /// Returns a uniformly random Boolean value.
    /// </summary>
    /// <returns><c>false</c> or <c>true</c> with equal probability.</returns>
    public bool NextBoolean()
    {
        return NextUInt32Bounded(2U) != 0U;
    }

    /// <summary>
    /// Returns an uppercase base16 token from cryptographic random bytes.
    /// </summary>
    /// <param name="byteCount">The nonnegative number of random source bytes.</param>
    /// <returns>A token containing exactly two hexadecimal characters per source byte.</returns>
    /// <remarks>The token carries exactly <c>8 * byteCount</c> bits of entropy.</remarks>
    /// <exception cref="RandomArgumentException">The byte count is negative or the encoded token is too large.</exception>
    public string TokenHex(int byteCount)
    {
        SecureToken.HexLength(byteCount);
        byte[] bytes = NextBytes(byteCount);
        string result = SecureToken.EncodeHex(bytes);
        CryptographicOperations.ZeroMemory(bytes);
        return result;
    }

    /// <summary>
    /// Returns an unpadded RFC 4648 base32 token from cryptographic random bytes.
    /// </summary>
    /// <param name="byteCount">The nonnegative number of random source bytes.</param>
    /// <returns>An uppercase base32 token without trailing padding characters.</returns>
    /// <remarks>The token carries exactly <c>8 * byteCount</c> bits of entropy.</remarks>
    /// <exception cref="RandomArgumentException">The byte count is negative or the encoded token is too large.</exception>
    public string TokenBase32(int byteCount)
    {
        SecureToken.Base32Length(byteCount);
        byte[] bytes = NextBytes(byteCount);
        string result = SecureToken.EncodeBase32(bytes);
        CryptographicOperations.ZeroMemory(bytes);
        return result;
    }

    /// <summary>
    /// Returns a padded RFC 4648 base64 token from cryptographic random bytes.
    /// </summary>
    /// <param name="byteCount">The nonnegative number of random source bytes.</param>
    /// <returns>A standard base64 token including required trailing padding characters.</returns>
    /// <remarks>The token carries exactly <c>8 * byteCount</c> bits of entropy; padding adds no entropy.</remarks>
    /// <exception cref="RandomArgumentException">The byte count is negative or the encoded token is too large.</exception>
    public string TokenBase64(int byteCount)
    {
        SecureToken.Base64Length(byteCount);
        byte[] bytes = NextBytes(byteCount);
        string result = SecureToken.EncodeBase64(bytes);
        CryptographicOperations.ZeroMemory(bytes);
        return result;
    }

    /// <summary>
    /// Returns an unpadded RFC 4648 base64url token from cryptographic random bytes.
    /// </summary>
    /// <param name="byteCount">The nonnegative number of random source bytes.</param>
    /// <returns>A URL-safe base64 token without trailing padding characters.</returns>
    /// <remarks>The token carries exactly <c>8 * byteCount</c> bits of entropy.</remarks>
    /// <exception cref="RandomArgumentException">The byte count is negative or the encoded token is too large.</exception>
    public string TokenBase64Url(int byteCount)
    {
        SecureToken.Base64UrlLength(byteCount);
        byte[] bytes = NextBytes(byteCount);
        string result = SecureToken.EncodeBase64Url(bytes);
        CryptographicOperations.ZeroMemory(bytes);
        return result;
    }

    /// <summary>
    /// Returns the entropy supplied to any byte-based token encoder.
    /// </summary>
    /// <param name="byteCount">The nonnegative random source byte count.</param>
    /// <returns>Exactly eight times the byte count, measured in bits.</returns>
    /// <remarks>Encoding characters and padding do not increase this value.</remarks>
    /// <exception cref="RandomArgumentException">The byte count is negative.</exception>
    internal static long TokenEntropyBits(int byteCount)
    {
        return SecureToken.ByteEntropyBits(byteCount);
    }

    /// <summary>
    /// Returns a uniformly selected string from a caller-provided alphabet.
    /// </summary>
    /// <param name="alphabet">At least two distinct, non-surrogate UTF-16 characters.</param>
    /// <param name="length">The nonnegative output character count.</param>
    /// <returns>A string containing independently and uniformly selected alphabet characters.</returns>
    /// <remarks>
    /// Selection uses rejection-based bounded integers and has no modulo bias. The result carries
    /// <c>length * log2(alphabet.Length)</c> bits of entropy when the generator remains secure.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The alphabet is null, insufficient, duplicated, or contains a surrogate, or length is negative.</exception>
    public string String(string alphabet, int length)
    {
        int alphabetLength = SecureToken.ValidateAlphabet(alphabet);
        Guard.Count(length, "length");
        if (length == 0)
        {
            FillBytes(Span<byte>.Empty);
            return string.Empty;
        }

        char[] result = new char[length];
        int index = 0;
        while (index < result.Length)
        {
            uint alphabetIndex = NextUInt32Bounded((uint)alphabetLength);
            result[index] = alphabet[(int)alphabetIndex];
            index++;
        }

        string value = new string(result);
        Array.Fill(result, '\0');
        return value;
    }

    /// <summary>
    /// Calculates the entropy of a uniformly generated custom-alphabet string.
    /// </summary>
    /// <param name="alphabet">At least two distinct, non-surrogate UTF-16 characters.</param>
    /// <param name="length">The nonnegative output character count.</param>
    /// <returns><c>length * log2(alphabet.Length)</c> bits.</returns>
    /// <exception cref="RandomArgumentException">The alphabet is null, insufficient, duplicated, or contains a surrogate, or length is negative.</exception>
    internal static double StringEntropyBits(string alphabet, int length)
    {
        return SecureToken.StringEntropyBits(alphabet, length);
    }

    /// <summary>
    /// Generates a password that satisfies a fully validated caller policy.
    /// </summary>
    /// <param name="policy">The mutable policy to validate and snapshot before requesting entropy.</param>
    /// <returns>A password satisfying the length, alphabet, required-count, repetition, and shuffle rules.</returns>
    /// <remarks>
    /// Every random selection and shuffle index is unbiased. The method does not log the policy, intermediate
    /// characters, or returned password, and clears its mutable character buffer before returning.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The policy is null.</exception>
    /// <exception cref="RandomSecurityException">The policy is invalid or cannot be satisfied.</exception>
    public string Password(PasswordPolicy policy)
    {
        return SecurePassword.Generate(this, policy);
    }

    /// <summary>
    /// Generates an RFC 9562 version 4 UUID from 122 effective cryptographic random bits.
    /// </summary>
    /// <returns>A UUID with version <c>4</c> and variant <c>10</c> bits.</returns>
    /// <remarks>Six random bits are replaced by the required version and variant fields.</remarks>
    public Guid Uuid4()
    {
        byte[] randomBytes = NextBytes(SecureUuid.ByteCount);
        Guid result = SecureUuid.CreateVersion4(randomBytes);
        CryptographicOperations.ZeroMemory(randomBytes);
        return result;
    }

    /// <summary>
    /// Generates a time-ordered RFC 9562 version 7 UUID using the current UTC Unix timestamp in milliseconds.
    /// </summary>
    /// <returns>A UUID with a 48-bit millisecond timestamp, a monotonic 12-bit counter, and 62 fresh random bits.</returns>
    /// <remarks>
    /// On a new timestamp, the counter's lower 11 bits are randomly seeded and its high bit is cleared as a
    /// rollover guard. Same-millisecond requests and clock rollback increment the counter. Counter overflow
    /// advances a logical millisecond and reseeds the counter, preserving strict per-instance bytewise order.
    /// </remarks>
    /// <exception cref="RandomExhaustedException">The timestamp and counter have both reached their maximum values.</exception>
    public Guid Uuid7()
    {
        long unixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return Uuid7AtUnixMilliseconds(unixMilliseconds);
    }

    internal Guid Uuid7AtUnixMilliseconds(long unixMilliseconds)
    {
        SecureUuid.ValidateUnixMilliseconds(unixMilliseconds);
        long selectedTimestamp = unixMilliseconds;
        int selectedCounter = 0;
        bool seedCounter = false;
        if (!uuid7Initialized || unixMilliseconds > uuid7Timestamp)
        {
            seedCounter = true;
        }
        else if (uuid7Counter < SecureUuid.MaximumCounter)
        {
            selectedTimestamp = uuid7Timestamp;
            selectedCounter = uuid7Counter + 1;
        }
        else
        {
            if (uuid7Timestamp >= SecureUuid.MaximumUnixMilliseconds)
            {
                throw RandomExhaustedException.Create("The UUIDv7 timestamp and monotonic counter are exhausted.");
            }

            selectedTimestamp = uuid7Timestamp + 1L;
            seedCounter = true;
        }

        byte[] randomBytes = NextBytes(SecureUuid.ByteCount);
        if (seedCounter)
        {
            selectedCounter = SecureUuid.SeedCounter(randomBytes);
        }

        Guid result = SecureUuid.CreateVersion7(randomBytes, selectedTimestamp, selectedCounter);
        uuid7Timestamp = selectedTimestamp;
        uuid7Counter = selectedCounter;
        uuid7Initialized = true;
        CryptographicOperations.ZeroMemory(randomBytes);
        return result;
    }

    /// <summary>
    /// Securely erases and permanently uninstantiates an HMAC-DRBG instance.
    /// </summary>
    /// <remarks>The direct system source has no retained state and rejects this operation.</remarks>
    /// <exception cref="RandomCapabilityException">The selected algorithm does not support uninstantiation.</exception>
    public void Uninstantiate()
    {
        if (algorithm != RandomAlgorithm.HmacDrbgSha256V1)
        {
            throw RandomCapabilityException.Create(algorithm, RandomCapability.Uninstantiate, "The selected secure algorithm has no retained state to uninstantiate.");
        }

        HmacDrbg().Uninstantiate();
    }

    SystemCryptoEngine SystemCrypto()
    {
        if (systemCrypto == null)
        {
            throw RandomStateException.Create("The system cryptographic engine is unavailable.");
        }

        return systemCrypto;
    }

    HmacDrbgSha256Engine HmacDrbg()
    {
        if (hmacDrbg == null)
        {
            throw RandomStateException.Create("The HMAC-DRBG engine is unavailable.");
        }

        return hmacDrbg;
    }

    HmacDrbgSha256Engine ActiveHmacDrbgForReseedStatus()
    {
        if (algorithm != RandomAlgorithm.HmacDrbgSha256V1)
        {
            throw RandomCapabilityException.Create(algorithm, RandomCapability.Reseed, "The selected secure algorithm has no reseed request status.");
        }

        HmacDrbgSha256Engine selectedDrbg = HmacDrbg();
        selectedDrbg.EnsureInstantiated();
        return selectedDrbg;
    }
}
