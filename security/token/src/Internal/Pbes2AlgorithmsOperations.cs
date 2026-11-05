using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

/// <summary>
/// Applies the JWE PBES2-HMAC-SHA2 algorithms with RFC 3394 AES Key Wrap.
/// </summary>
internal static class Pbes2AlgorithmsOperations
{
    /// <summary>The minimum accepted JWE PBES2 salt-input size in bytes.</summary>
    internal const int MinimumSaltInputSize = Pbes2Algorithms.MinimumSaltInputSize;

    /// <summary>The generated JWE PBES2 salt-input size in bytes.</summary>
    internal const int GeneratedSaltInputSize = Pbes2Algorithms.GeneratedSaltInputSize;

    /// <summary>The largest accepted JWE PBES2 salt-input size in bytes.</summary>
    internal const int MaximumSaltInputSize = Pbes2Algorithms.MaximumSaltInputSize;

    /// <summary>The minimum accepted PBKDF2 iteration count.</summary>
    internal const int MinimumIterationCount = Pbes2Algorithms.MinimumIterationCount;

    /// <summary>The default PBKDF2 iteration count used by wrapping operations.</summary>
    internal const int DefaultIterationCount = Pbes2Algorithms.DefaultIterationCount;

    /// <summary>The maximum accepted PBKDF2 iteration count.</summary>
    internal const int MaximumIterationCount = Pbes2Algorithms.MaximumIterationCount;

    /// <summary>Wraps key material with a generated salt input and the default iteration count.</summary>
    /// <param name="keyMaterial">The key material to wrap.</param>
    /// <param name="password">The password octets, using UTF-8 when the password originates as text.</param>
    /// <param name="algorithm">The case-sensitive JWE PBES2 algorithm name.</param>
    /// <returns>A result containing the encrypted key and generated <c>p2s</c> and <c>p2c</c> values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static Pbes2KeyWrapResult Wrap(
        byte[] keyMaterial,
        byte[] password,
        string algorithm)
    {
        return Wrap(
            keyMaterial,
            password,
            algorithm,
            DefaultIterationCount);
    }

    /// <summary>Wraps key material with a generated salt input and caller-selected iteration count.</summary>
    /// <param name="keyMaterial">The key material to wrap.</param>
    /// <param name="password">The password octets, using UTF-8 when the password originates as text.</param>
    /// <param name="algorithm">The case-sensitive JWE PBES2 algorithm name.</param>
    /// <param name="iterationCount">The PBKDF2 iteration count to place in the <c>p2c</c> header.</param>
    /// <returns>A result containing the encrypted key and generated <c>p2s</c> and <c>p2c</c> values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static Pbes2KeyWrapResult Wrap(
        byte[] keyMaterial,
        byte[] password,
        string algorithm,
        int iterationCount)
    {
        if (keyMaterial == null)
        {
            throw new ArgumentNullException("keyMaterial");
        }

        if (password == null)
        {
            throw new ArgumentNullException("password");
        }

        return Wrap(
            keyMaterial.AsSpan(),
            password.AsSpan(),
            algorithm,
            iterationCount);
    }

    /// <summary>Wraps key-material and password spans with a generated salt input.</summary>
    /// <param name="keyMaterial">The key material to wrap.</param>
    /// <param name="password">The password octets, using UTF-8 when the password originates as text.</param>
    /// <param name="algorithm">The case-sensitive JWE PBES2 algorithm name.</param>
    /// <param name="iterationCount">The PBKDF2 iteration count to place in the <c>p2c</c> header.</param>
    /// <returns>A result containing the encrypted key and generated <c>p2s</c> and <c>p2c</c> values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>algorithm</c> is null.</exception>
    internal static Pbes2KeyWrapResult Wrap(
        ReadOnlySpan<byte> keyMaterial,
        ReadOnlySpan<byte> password,
        string algorithm,
        int iterationCount)
    {
        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        TokenResult algorithmValidation = ValidateAlgorithm(algorithm);
        if (!algorithmValidation.Success)
        {
            return Failure(algorithmValidation.Error);
        }

        if (password.Length == 0)
        {
            return Failure(
                "invalid_password",
                "The PBES2 password must not be empty.");
        }

        if (keyMaterial.Length < 16 || keyMaterial.Length % 8 != 0)
        {
            return Failure(
                "invalid_key_size",
                "PBES2 AES Key Wrap input must contain at least two 64-bit blocks.");
        }

        if (!ValidIterationCount(iterationCount))
        {
            return Failure(
                "invalid_iteration_count",
                "The PBES2 iteration count is outside the permitted range.");
        }

        byte[] saltInput = RandomBytes.Generate(GeneratedSaltInputSize);
        TokenBytesResult wrapped = Transform(
            keyMaterial,
            password,
            algorithm,
            saltInput,
            iterationCount,
            true);
        if (!wrapped.Success)
        {
            CryptographicOperations.ZeroMemory(saltInput);
            return Failure(wrapped.Error);
        }

        Pbes2KeyWrapResult result = new Pbes2KeyWrapResult();
        result.Success = true;
        result.EncryptedKey = wrapped.Bytes;
        result.SaltInput = saltInput;
        result.IterationCount = iterationCount;
        return result;
    }

    /// <summary>Unwraps key material using decoded JWE PBES2 header values.</summary>
    /// <param name="wrappedKey">The JWE encrypted-key value.</param>
    /// <param name="password">The password octets, using UTF-8 when the password originates as text.</param>
    /// <param name="algorithm">The case-sensitive JWE PBES2 algorithm name.</param>
    /// <param name="saltInput">The decoded JWE <c>p2s</c> header value.</param>
    /// <param name="iterationCount">The JWE <c>p2c</c> header value.</param>
    /// <returns>A result containing the authenticated unwrapped key material.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenBytesResult Unwrap(
        byte[] wrappedKey,
        byte[] password,
        string algorithm,
        byte[] saltInput,
        int iterationCount)
    {
        if (wrappedKey == null)
        {
            throw new ArgumentNullException("wrappedKey");
        }

        if (password == null)
        {
            throw new ArgumentNullException("password");
        }

        if (saltInput == null)
        {
            throw new ArgumentNullException("saltInput");
        }

        return Unwrap(
            wrappedKey.AsSpan(),
            password.AsSpan(),
            algorithm,
            saltInput.AsSpan(),
            iterationCount);
    }

    /// <summary>Unwraps key material using JWE encrypted-key, password, and salt-input spans.</summary>
    /// <param name="wrappedKey">The JWE encrypted-key value.</param>
    /// <param name="password">The password octets, using UTF-8 when the password originates as text.</param>
    /// <param name="algorithm">The case-sensitive JWE PBES2 algorithm name.</param>
    /// <param name="saltInput">The decoded JWE <c>p2s</c> header value.</param>
    /// <param name="iterationCount">The JWE <c>p2c</c> header value.</param>
    /// <returns>A result containing the authenticated unwrapped key material.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>algorithm</c> is null.</exception>
    internal static TokenBytesResult Unwrap(
        ReadOnlySpan<byte> wrappedKey,
        ReadOnlySpan<byte> password,
        string algorithm,
        ReadOnlySpan<byte> saltInput,
        int iterationCount)
    {
        if (algorithm == null)
        {
            throw new ArgumentNullException("algorithm");
        }

        TokenResult algorithmValidation = ValidateAlgorithm(algorithm);
        if (!algorithmValidation.Success)
        {
            return BytesFailure(algorithmValidation.Error);
        }

        if (password.Length == 0
            || wrappedKey.Length < 24
            || wrappedKey.Length % 8 != 0
            || !ValidSaltInput(saltInput.Length)
            || !ValidIterationCount(iterationCount))
        {
            return UnwrapFailure();
        }

        return Transform(
            wrappedKey,
            password,
            algorithm,
            saltInput,
            iterationCount,
            false);
    }

    static TokenBytesResult Transform(
        ReadOnlySpan<byte> keyMaterial,
        ReadOnlySpan<byte> password,
        string algorithm,
        ReadOnlySpan<byte> saltInput,
        int iterationCount,
        bool wrapping)
    {
        byte[] algorithmBytes = Encoding.UTF8.GetBytes(algorithm);
        byte[] salt = new byte[algorithmBytes.Length + 1 + saltInput.Length];
        algorithmBytes.CopyTo(salt, 0);
        saltInput.CopyTo(salt.AsSpan(algorithmBytes.Length + 1));
        byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterationCount,
            GetHashAlgorithm(algorithm),
            GetKeySize(algorithm));
        CryptographicOperations.ZeroMemory(algorithmBytes);
        CryptographicOperations.ZeroMemory(salt);

        TokenKey key = new TokenKey();
        key.KeyType = TokenKeyTypes.Symmetric;
        key.SymmetricKey = derivedKey;
        string keyWrapAlgorithm = GetKeyWrapAlgorithm(algorithm);
        TokenBytesResult result = wrapping
            ? AesKeyWrap.Wrap(keyMaterial, key, keyWrapAlgorithm)
            : AesKeyWrap.Unwrap(keyMaterial, key, keyWrapAlgorithm);
        key.ReleaseOwnedMaterial();
        return result;
    }

    static TokenResult ValidateAlgorithm(string algorithm)
    {
        if (string.Equals(
                algorithm,
                JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap,
                StringComparison.Ordinal)
            || string.Equals(
                algorithm,
                JoseAlgorithms.Pbes2HmacSha512Aes256KeyWrap,
                StringComparison.Ordinal))
        {
            return ValidationSuccess();
        }

        TokenResult availability = CryptoAlgorithmAvailability.CheckJoseAlgorithm(
            algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        return ValidationFailure(
            "invalid_algorithm",
            "The JOSE algorithm is not a PBES2 key-wrap algorithm.");
    }

    static bool ValidSaltInput(int length)
    {
        return length >= MinimumSaltInputSize
            && length <= MaximumSaltInputSize;
    }

    static bool ValidIterationCount(int iterationCount)
    {
        return iterationCount >= MinimumIterationCount
            && iterationCount <= MaximumIterationCount;
    }

    static HashAlgorithmName GetHashAlgorithm(string algorithm)
    {
        if (string.Equals(
            algorithm,
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            StringComparison.Ordinal))
        {
            return HashAlgorithmName.SHA256;
        }

        if (string.Equals(
            algorithm,
            JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap,
            StringComparison.Ordinal))
        {
            return HashAlgorithmName.SHA384;
        }

        return HashAlgorithmName.SHA512;
    }

    static int GetKeySize(string algorithm)
    {
        if (string.Equals(
            algorithm,
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            StringComparison.Ordinal))
        {
            return 16;
        }

        if (string.Equals(
            algorithm,
            JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap,
            StringComparison.Ordinal))
        {
            return 24;
        }

        return 32;
    }

    static string GetKeyWrapAlgorithm(string algorithm)
    {
        if (string.Equals(
            algorithm,
            JoseAlgorithms.Pbes2HmacSha256Aes128KeyWrap,
            StringComparison.Ordinal))
        {
            return JoseAlgorithms.Aes128KeyWrap;
        }

        if (string.Equals(
            algorithm,
            JoseAlgorithms.Pbes2HmacSha384Aes192KeyWrap,
            StringComparison.Ordinal))
        {
            return JoseAlgorithms.Aes192KeyWrap;
        }

        return JoseAlgorithms.Aes256KeyWrap;
    }

    static TokenBytesResult UnwrapFailure()
    {
        return BytesFailure(
            "unwrap_failed",
            "The PBES2 encrypted key could not be derived and unwrapped.");
    }

    static Pbes2KeyWrapResult Failure(string code, string message)
    {
        return Failure(new TokenError(code, message, -1));
    }

    static Pbes2KeyWrapResult Failure(TokenError? error)
    {
        Pbes2KeyWrapResult result = new Pbes2KeyWrapResult();
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

    static TokenResult ValidationSuccess()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult ValidationFailure(string code, string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
