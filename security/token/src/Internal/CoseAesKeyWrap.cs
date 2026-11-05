using System;
namespace token;

/// <summary>Wraps and unwraps COSE key material using RFC 3394 AES Key Wrap.</summary>
internal static class CoseAesKeyWrap
{
    /// <summary>Wraps key material using the COSE A128KW, A192KW, or A256KW algorithm.</summary>
    /// <param name="keyMaterial">The key material to wrap.</param>
    /// <param name="keyEncryptionKey">The AES key-encryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES Key Wrap algorithm label.</param>
    /// <returns>A result containing the wrapped key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenBytesResult Wrap(
        byte[] keyMaterial,
        TokenKey keyEncryptionKey,
        int algorithm)
    {
        if (keyMaterial == null)
        {
            throw new ArgumentNullException("keyMaterial");
        }

        return Wrap(
            keyMaterial.AsSpan(),
            keyEncryptionKey,
            algorithm);
    }

    /// <summary>Wraps a key-material span using a COSE AES Key Wrap algorithm.</summary>
    /// <param name="keyMaterial">The key material to wrap.</param>
    /// <param name="keyEncryptionKey">The AES key-encryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES Key Wrap algorithm label.</param>
    /// <returns>A result containing the wrapped key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>keyEncryptionKey</c> is null.</exception>
    internal static TokenBytesResult Wrap(
        ReadOnlySpan<byte> keyMaterial,
        TokenKey keyEncryptionKey,
        int algorithm)
    {
        if (keyEncryptionKey == null)
        {
            throw new ArgumentNullException("keyEncryptionKey");
        }

        TokenResult validation = ValidateAlgorithm(algorithm);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        return AesKeyWrap.Wrap(
            keyMaterial,
            keyEncryptionKey,
            GetJoseAlgorithm(algorithm));
    }

    /// <summary>Unwraps key material using the COSE A128KW, A192KW, or A256KW algorithm.</summary>
    /// <param name="wrappedKey">The wrapped key material.</param>
    /// <param name="keyEncryptionKey">The AES key-encryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES Key Wrap algorithm label.</param>
    /// <returns>A result containing the unwrapped key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a reference argument is null.</exception>
    internal static TokenBytesResult Unwrap(
        byte[] wrappedKey,
        TokenKey keyEncryptionKey,
        int algorithm)
    {
        if (wrappedKey == null)
        {
            throw new ArgumentNullException("wrappedKey");
        }

        return Unwrap(
            wrappedKey.AsSpan(),
            keyEncryptionKey,
            algorithm);
    }

    /// <summary>Unwraps a key-material span using a COSE AES Key Wrap algorithm.</summary>
    /// <param name="wrappedKey">The wrapped key material.</param>
    /// <param name="keyEncryptionKey">The AES key-encryption key and its metadata.</param>
    /// <param name="algorithm">The COSE AES Key Wrap algorithm label.</param>
    /// <returns>A result containing the unwrapped key or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>keyEncryptionKey</c> is null.</exception>
    internal static TokenBytesResult Unwrap(
        ReadOnlySpan<byte> wrappedKey,
        TokenKey keyEncryptionKey,
        int algorithm)
    {
        if (keyEncryptionKey == null)
        {
            throw new ArgumentNullException("keyEncryptionKey");
        }

        TokenResult validation = ValidateAlgorithm(algorithm);
        if (!validation.Success)
        {
            return Failure(validation.Error);
        }

        return AesKeyWrap.Unwrap(
            wrappedKey,
            keyEncryptionKey,
            GetJoseAlgorithm(algorithm));
    }

    static TokenResult ValidateAlgorithm(int algorithm)
    {
        TokenResult availability =
            CryptoAlgorithmAvailability.CheckCoseAlgorithm(algorithm);
        if (!availability.Success)
        {
            return availability;
        }

        if (algorithm != CoseAlgorithms.Aes128KeyWrap
            && algorithm != CoseAlgorithms.Aes192KeyWrap
            && algorithm != CoseAlgorithms.Aes256KeyWrap)
        {
            return ValidationFailure(
                "invalid_algorithm",
                "The COSE algorithm is not an AES Key Wrap algorithm.");
        }

        return ValidationSuccess();
    }

    static string GetJoseAlgorithm(int algorithm)
    {
        if (algorithm == CoseAlgorithms.Aes128KeyWrap)
        {
            return JoseAlgorithms.Aes128KeyWrap;
        }

        if (algorithm == CoseAlgorithms.Aes192KeyWrap)
        {
            return JoseAlgorithms.Aes192KeyWrap;
        }

        return JoseAlgorithms.Aes256KeyWrap;
    }

    static TokenBytesResult Failure(TokenError? error)
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

    static TokenResult ValidationFailure(
        string code,
        string message)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
