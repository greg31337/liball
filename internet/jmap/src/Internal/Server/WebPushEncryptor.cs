using System;
using System.Security.Cryptography;
using System.Text;

namespace jmap;

internal class WebPushEncryptor
{
    internal const int SaltLength = 16;
    internal const int PublicKeyLength = 65;
    internal const int AuthenticationTagLength = 16;
    internal const int RecordSize = 4096;
    internal const int MaximumPlaintextLength = 3993;
    internal static readonly byte[] KeyInfoPrefix = Encoding.ASCII.GetBytes("WebPush: info\0");
    internal static readonly byte[] ContentKeyInfo = Encoding.ASCII.GetBytes("Content-Encoding: aes128gcm\0");
    internal static readonly byte[] NonceInfo = Encoding.ASCII.GetBytes("Content-Encoding: nonce\0");

    internal byte[] Encrypt(byte[] plaintext, byte[] receiverPublicKey, byte[] authenticationSecret)
    {
        if (plaintext == null || receiverPublicKey == null || authenticationSecret == null)
        {
            throw new ArgumentNullException("plaintext");
        }

        byte[] salt = RandomNumberGenerator.GetBytes(SaltLength);
        using ECDiffieHellman sender = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        ECParameters senderParameters = sender.ExportParameters(true);
        byte[] senderPublicKey = ExportPublicKey(senderParameters);
        byte[] senderPrivateKey = senderParameters.D == null ? new byte[0] : senderParameters.D;
        byte[] result = EncryptWithMaterial(plaintext, receiverPublicKey, authenticationSecret, salt, senderPrivateKey, senderPublicKey);
        CryptographicOperations.ZeroMemory(senderPrivateKey);
        CryptographicOperations.ZeroMemory(salt);
        return result;
    }

    internal byte[] EncryptWithMaterial(
        byte[] plaintext,
        byte[] receiverPublicKey,
        byte[] authenticationSecret,
        byte[] salt,
        byte[] senderPrivateKey,
        byte[] senderPublicKey)
    {
        ValidateInputs(plaintext, receiverPublicKey, authenticationSecret, salt, senderPrivateKey, senderPublicKey);
        using ECDiffieHellman receiver = CreateReceiver(receiverPublicKey);
        using ECDiffieHellman sender = CreateSender(senderPrivateKey, senderPublicKey);
        byte[] sharedSecret = sender.DeriveRawSecretAgreement(receiver.PublicKey);
        byte[] keyInfo = JoinThree(KeyInfoPrefix, receiverPublicKey, senderPublicKey);
        byte[] keyPrk = Hmac(authenticationSecret, sharedSecret);
        byte[] inputKeyMaterial = Expand(keyPrk, keyInfo, 32);
        byte[] prk = Hmac(salt, inputKeyMaterial);
        byte[] contentEncryptionKey = Expand(prk, ContentKeyInfo, 16);
        byte[] nonce = Expand(prk, NonceInfo, 12);
        byte[] record = new byte[plaintext.Length + 1];
        Buffer.BlockCopy(plaintext, 0, record, 0, plaintext.Length);
        record[record.Length - 1] = 2;
        byte[] ciphertext = new byte[record.Length];
        byte[] tag = new byte[AuthenticationTagLength];
        using AesGcm aes = new AesGcm(contentEncryptionKey, AuthenticationTagLength);
        aes.Encrypt(nonce, record, ciphertext, tag);
        byte[] body = BuildBody(salt, senderPublicKey, ciphertext, tag);
        ClearSecrets(sharedSecret, keyPrk, inputKeyMaterial, prk, contentEncryptionKey, nonce, record);
        return body;
    }

    internal void ValidateInputs(
        byte[] plaintext,
        byte[] receiverPublicKey,
        byte[] authenticationSecret,
        byte[] salt,
        byte[] senderPrivateKey,
        byte[] senderPublicKey)
    {
        if (plaintext == null || plaintext.Length > MaximumPlaintextLength)
        {
            throw new ArgumentException("A Web Push plaintext must not exceed 3993 octets.", "plaintext");
        }

        PushValidator validator = new PushValidator();
        string receiver = EncodeBase64Url(receiverPublicKey);
        string sender = EncodeBase64Url(senderPublicKey);
        if (!validator.IsValidP256dh(receiver) || !validator.IsValidP256dh(sender))
        {
            throw new ArgumentException("Web Push public keys must be valid uncompressed P-256 points.", "receiverPublicKey");
        }

        if (authenticationSecret == null || authenticationSecret.Length != 16)
        {
            throw new ArgumentException("The Web Push authentication secret must contain 16 octets.", "authenticationSecret");
        }

        if (salt == null || salt.Length != SaltLength)
        {
            throw new ArgumentException("The Web Push salt must contain 16 octets.", "salt");
        }

        if (senderPrivateKey == null || senderPrivateKey.Length != 32)
        {
            throw new ArgumentException("The Web Push sender private key must contain 32 octets.", "senderPrivateKey");
        }
    }

    internal ECDiffieHellman CreateReceiver(byte[] publicKey)
    {
        ECParameters parameters = new ECParameters();
        parameters.Curve = ECCurve.NamedCurves.nistP256;
        ECPoint point = new ECPoint();
        point.X = CopyRange(publicKey, 1, 32);
        point.Y = CopyRange(publicKey, 33, 32);
        parameters.Q = point;
        return ECDiffieHellman.Create(parameters);
    }

    internal ECDiffieHellman CreateSender(byte[] privateKey, byte[] publicKey)
    {
        ECParameters parameters = new ECParameters();
        parameters.Curve = ECCurve.NamedCurves.nistP256;
        ECPoint point = new ECPoint();
        point.X = CopyRange(publicKey, 1, 32);
        point.Y = CopyRange(publicKey, 33, 32);
        parameters.Q = point;
        parameters.D = CopyRange(privateKey, 0, privateKey.Length);
        return ECDiffieHellman.Create(parameters);
    }

    internal byte[] ExportPublicKey(ECParameters parameters)
    {
        if (parameters.Q.X == null || parameters.Q.Y == null || parameters.Q.X.Length != 32 || parameters.Q.Y.Length != 32)
        {
            throw new CryptographicException("The generated Web Push key is not an uncompressed P-256 key.");
        }

        byte[] result = new byte[PublicKeyLength];
        result[0] = 4;
        Buffer.BlockCopy(parameters.Q.X, 0, result, 1, 32);
        Buffer.BlockCopy(parameters.Q.Y, 0, result, 33, 32);
        return result;
    }

    internal byte[] Hmac(byte[] key, byte[] data)
    {
        using HMACSHA256 hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    internal byte[] Expand(byte[] pseudorandomKey, byte[] info, int length)
    {
        byte[] input = new byte[info.Length + 1];
        Buffer.BlockCopy(info, 0, input, 0, info.Length);
        input[input.Length - 1] = 1;
        byte[] block = Hmac(pseudorandomKey, input);
        byte[] result = CopyRange(block, 0, length);
        CryptographicOperations.ZeroMemory(block);
        return result;
    }

    internal byte[] BuildBody(byte[] salt, byte[] senderPublicKey, byte[] ciphertext, byte[] tag)
    {
        byte[] body = new byte[SaltLength + 4 + 1 + PublicKeyLength + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(salt, 0, body, 0, SaltLength);
        body[16] = 0;
        body[17] = 0;
        body[18] = 16;
        body[19] = 0;
        body[20] = PublicKeyLength;
        Buffer.BlockCopy(senderPublicKey, 0, body, 21, PublicKeyLength);
        Buffer.BlockCopy(ciphertext, 0, body, 21 + PublicKeyLength, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, body, 21 + PublicKeyLength + ciphertext.Length, tag.Length);
        return body;
    }

    internal byte[] JoinThree(byte[] first, byte[] second, byte[] third)
    {
        byte[] result = new byte[first.Length + second.Length + third.Length];
        Buffer.BlockCopy(first, 0, result, 0, first.Length);
        Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
        Buffer.BlockCopy(third, 0, result, first.Length + second.Length, third.Length);
        return result;
    }

    internal byte[] CopyRange(byte[] source, int offset, int length)
    {
        byte[] result = new byte[length];
        Buffer.BlockCopy(source, offset, result, 0, length);
        return result;
    }

    internal string EncodeBase64Url(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    internal void ClearSecrets(
        byte[] sharedSecret,
        byte[] keyPrk,
        byte[] inputKeyMaterial,
        byte[] prk,
        byte[] contentEncryptionKey,
        byte[] nonce,
        byte[] record)
    {
        CryptographicOperations.ZeroMemory(sharedSecret);
        CryptographicOperations.ZeroMemory(keyPrk);
        CryptographicOperations.ZeroMemory(inputKeyMaterial);
        CryptographicOperations.ZeroMemory(prk);
        CryptographicOperations.ZeroMemory(contentEncryptionKey);
        CryptographicOperations.ZeroMemory(nonce);
        CryptographicOperations.ZeroMemory(record);
    }
}
