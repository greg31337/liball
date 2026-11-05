using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailOpenPgpAlgorithmsConstants
{
    /// <summary>
    /// The public key RSA value exposed by this API.
    /// </summary>
    public const int PublicKeyRsa = 1;
    /// <summary>
    /// The public key RSA encrypt only value exposed by this API.
    /// </summary>
    public const int PublicKeyRsaEncryptOnly = 2;
    /// <summary>
    /// The public key RSA sign only value exposed by this API.
    /// </summary>
    public const int PublicKeyRsaSignOnly = 3;
    /// <summary>
    /// The public key ECDSA value exposed by this API.
    /// </summary>
    public const int PublicKeyEcdsa = 19;
    /// <summary>
    /// The public key x 25519 value exposed by this API.
    /// </summary>
    public const int PublicKeyX25519 = 25;
    /// <summary>
    /// The public key ed 25519 value exposed by this API.
    /// </summary>
    public const int PublicKeyEd25519 = 27;
    /// <summary>
    /// The symmetric aes 128 value exposed by this API.
    /// </summary>
    public const int SymmetricAes128 = 7;
    /// <summary>
    /// The symmetric aes 192 value exposed by this API.
    /// </summary>
    public const int SymmetricAes192 = 8;
    /// <summary>
    /// The symmetric aes 256 value exposed by this API.
    /// </summary>
    public const int SymmetricAes256 = 9;
    /// <summary>
    /// The compression uncompressed value exposed by this API.
    /// </summary>
    public const int CompressionUncompressed = 0;
    /// <summary>
    /// The compression zip value exposed by this API.
    /// </summary>
    public const int CompressionZip = 1;
    /// <summary>
    /// The compression zlib value exposed by this API.
    /// </summary>
    public const int CompressionZlib = 2;
    /// <summary>
    /// The hash sha 1 value exposed by this API.
    /// </summary>
    public const int HashSha1 = 2;
    /// <summary>
    /// The hash sha 256 value exposed by this API.
    /// </summary>
    public const int HashSha256 = 8;
    /// <summary>
    /// The hash sha 384 value exposed by this API.
    /// </summary>
    public const int HashSha384 = 9;
    /// <summary>
    /// The hash sha 512 value exposed by this API.
    /// </summary>
    public const int HashSha512 = 10;
    /// <summary>
    /// The hash sha 3_256 value exposed by this API.
    /// </summary>
    public const int HashSha3_256 = 12;
    /// <summary>
    /// The hash sha 3_512 value exposed by this API.
    /// </summary>
    public const int HashSha3_512 = 14;
    /// <summary>
    /// The aead GCM value exposed by this API.
    /// </summary>
    public const int AeadGcm = 3;
}
