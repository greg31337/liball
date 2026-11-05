using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace xlsx;

internal class XlsxParsedEncryption
{
    internal XlsxEncryptionInfo Info;
    internal byte[] Salt;
    internal byte[] VerifierInput;
    internal byte[] VerifierHash;
    internal byte[] EncryptedKey;
    internal byte[] KeyDataSalt;
    internal byte[] EncryptedHmacKey;
    internal byte[] EncryptedHmacValue;
    internal int HashSize;

    internal XlsxParsedEncryption()
    {
        Info = new XlsxEncryptionInfo();
        Salt = new byte[0];
        VerifierInput = new byte[0];
        VerifierHash = new byte[0];
        EncryptedKey = new byte[0];
        KeyDataSalt = new byte[0];
        EncryptedHmacKey = new byte[0];
        EncryptedHmacValue = new byte[0];
        HashSize = 0;
    }
}

internal static class XlsxEncryptedPackageCryptography
{
    internal static readonly byte[] VerifierInputBlock = new byte[] { 0xFE, 0xA7, 0xD2, 0x76, 0x3B, 0x4B, 0x9E, 0x79 };
    internal static readonly byte[] VerifierHashBlock = new byte[] { 0xD7, 0xAA, 0x0F, 0x6D, 0x30, 0x61, 0x34, 0x4E };
    internal static readonly byte[] EncryptedKeyBlock = new byte[] { 0x14, 0x6E, 0x0B, 0xE7, 0xAB, 0xAC, 0xD0, 0xD6 };
    internal static readonly byte[] HmacKeyBlock = new byte[] { 0x5F, 0xB2, 0xAD, 0x01, 0x0C, 0xB9, 0xE1, 0xF6 };
    internal static readonly byte[] HmacValueBlock = new byte[] { 0xA0, 0x67, 0x7F, 0x02, 0xB2, 0x2C, 0x84, 0x33 };

    internal static XlsxEncryptedPackageDetectionResult Detect(byte[] compoundFileBytes)
    {
        XlsxEncryptedPackageDetectionResult result = new XlsxEncryptedPackageDetectionResult();
        XlsxCompoundFileResult compound = XlsxCompoundFile.Read(compoundFileBytes);
        CopyDetectionDiagnostics(result, compound.Validation);
        if (!compound.IsValid)
        {
            return result;
        }
        if (!HasEncryptionDataSpaces(compound))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "cfb.encryptionDataSpacesMissing", "Compound file does not contain the required Office encryption DataSpace streams."));
        }
        byte[] encryptionInfo = XlsxCompoundFile.GetStream(compound, "EncryptionInfo");
        byte[] encryptedPackage = XlsxCompoundFile.GetStream(compound, "EncryptedPackage");
        if (encryptionInfo.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptionInfo.streamMissing", "Compound file does not contain an EncryptionInfo stream."));
            return result;
        }
        if (encryptedPackage.Length < 8)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptedPackage.streamMissing", "Compound file does not contain a valid EncryptedPackage stream."));
            return result;
        }
        XlsxParsedEncryption parsed = ParseEncryptionInfo(encryptionInfo);
        result.EncryptionInfo = parsed.Info;
        CopyDetectionDiagnostics(result, parsed.Info.Validation);
        result.IsValid = result.Validation.IsValid && parsed.Info.IsSupported;
        return result;
    }

    internal static XlsxEncryptedPackageResult Decrypt(byte[] compoundFileBytes, char[] password)
    {
        XlsxEncryptedPackageResult result = new XlsxEncryptedPackageResult();
        if (password == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.passwordNull", "Password characters must not be null."));
            return result;
        }
        if (PasswordCodePointCount(password) > 255)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.passwordTooLong", "OOXML encryption passwords must not exceed 255 Unicode characters."));
            return result;
        }
        XlsxCompoundFileResult compound = XlsxCompoundFile.Read(compoundFileBytes);
        CopyOperationDiagnostics(result, compound.Validation);
        if (!compound.IsValid)
        {
            return result;
        }
        if (!HasEncryptionDataSpaces(compound))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "cfb.encryptionDataSpacesMissing", "Compound file does not contain the required Office encryption DataSpace streams."));
            return result;
        }
        byte[] encryptionInfo = XlsxCompoundFile.GetStream(compound, "EncryptionInfo");
        byte[] encryptedPackage = XlsxCompoundFile.GetStream(compound, "EncryptedPackage");
        if (encryptionInfo.Length == 0 || encryptedPackage.Length < 8)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptedPackage.requiredStreamsMissing", "Compound file must contain EncryptionInfo and EncryptedPackage streams."));
            return result;
        }
        XlsxParsedEncryption parsed = ParseEncryptionInfo(encryptionInfo);
        result.EncryptionInfo = parsed.Info;
        CopyOperationDiagnostics(result, parsed.Info.Validation);
        if (!parsed.Info.IsSupported || !result.IsValid)
        {
            return result;
        }
        if (parsed.Info.Kind == XlsxPackageEncryptionKind.Standard)
        {
            return DecryptStandard(encryptedPackage, parsed, password, result);
        }
        if (parsed.Info.Kind == XlsxPackageEncryptionKind.Agile)
        {
            return DecryptAgile(encryptedPackage, parsed, password, result);
        }
        result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.algorithmUnsupported", "Encryption format is not supported."));
        return result;
    }

    internal static XlsxEncryptedPackageResult Encrypt(byte[] packageBytes, XlsxPackageEncryptionOptions options)
    {
        XlsxEncryptedPackageResult result = new XlsxEncryptedPackageResult();
        if (packageBytes == null || packageBytes.Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.packageEmpty", "OOXML package bytes must not be empty."));
            return result;
        }
        if (packageBytes.Length < 4 || packageBytes[0] != 0x50 || packageBytes[1] != 0x4B)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.packageNotZip", "OOXML package bytes must begin with a ZIP signature."));
            return result;
        }
        if (options == null || options.Password == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.optionsInvalid", "Encryption options and password characters must not be null."));
            return result;
        }
        if (PasswordCodePointCount(options.Password) > 255)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.passwordTooLong", "OOXML encryption passwords must not exceed 255 Unicode characters."));
            return result;
        }
        int keyBits = KeyBits(options.Algorithm);
        if (keyBits == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.algorithmUnsupported", "Only AES-128, AES-192, and AES-256 are supported."));
            return result;
        }
        if (options.Kind == XlsxPackageEncryptionKind.Standard)
        {
            return EncryptStandard(packageBytes, options.Password, options.Algorithm, result);
        }
        if (options.Kind == XlsxPackageEncryptionKind.Agile)
        {
            if (options.SpinCount < 1 || options.SpinCount > 10000000)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.spinCountInvalid", "Agile spin count must be between one and ten million."));
                return result;
            }
            return EncryptAgile(packageBytes, options.Password, options.Algorithm, options.SpinCount, result);
        }
        result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.kindUnsupported", "Only Standard and Agile encryption can be written."));
        return result;
    }

    internal static bool HasEncryptionDataSpaces(XlsxCompoundFileResult compound)
    {
        return compound.Streams.ContainsKey("Version") &&
            compound.Streams.ContainsKey("DataSpaceMap") &&
            compound.Streams.ContainsKey("StrongEncryptionDataSpace") &&
            compound.Streams.ContainsKey("\x06Primary");
    }

    internal static XlsxParsedEncryption ParseEncryptionInfo(byte[] bytes)
    {
        XlsxParsedEncryption parsed = new XlsxParsedEncryption();
        parsed.Info = XlsxEncryptionDetector.ReadEncryptionInfo(bytes);
        if (!parsed.Info.IsValid || bytes.Length < 8)
        {
            return parsed;
        }
        if (parsed.Info.IsStandard)
        {
            ParseStandardInfo(bytes, parsed);
        }
        else if (parsed.Info.IsAgile)
        {
            ParseAgileInfo(bytes, parsed);
        }
        else
        {
            parsed.Info.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.kindUnsupported", "EncryptionInfo version is not supported."));
        }
        return parsed;
    }

    internal static void ParseStandardInfo(byte[] bytes, XlsxParsedEncryption parsed)
    {
        if (bytes.Length < 12)
        {
            parsed.Info.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptionInfo.standardTruncated", "Standard EncryptionInfo header is truncated."));
            return;
        }
        int headerSize = XlsxCompoundFile.ReadInt32(bytes, 8);
        int verifierOffset = 12 + headerSize;
        if (headerSize < 32 || verifierOffset < 44 || verifierOffset + 40 > bytes.Length)
        {
            parsed.Info.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptionInfo.standardTruncated", "Standard EncryptionInfo fields are truncated."));
            return;
        }
        int flags = XlsxCompoundFile.ReadInt32(bytes, 12);
        int algorithmId = XlsxCompoundFile.ReadInt32(bytes, 20);
        int hashId = XlsxCompoundFile.ReadInt32(bytes, 24);
        int keyBits = XlsxCompoundFile.ReadInt32(bytes, 28);
        int providerType = XlsxCompoundFile.ReadInt32(bytes, 32);
        parsed.Info.Kind = XlsxPackageEncryptionKind.Standard;
        parsed.Info.KeyBits = keyBits;
        parsed.Info.SpinCount = 50000;
        parsed.Info.Algorithm = AlgorithmFromBits(keyBits);
        if ((flags & 0x24) != 0x24 || hashId != 0x8004 || providerType != 0x18 || !MatchesStandardAlgorithm(algorithmId, keyBits))
        {
            parsed.Info.IsSupported = false;
            parsed.Info.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.algorithmUnsupported", "Standard encryption requires AES-128, AES-192, or AES-256 with SHA-1 and an AES provider."));
            return;
        }
        int saltSize = XlsxCompoundFile.ReadInt32(bytes, verifierOffset);
        int hashSizeOffset = verifierOffset + 4 + saltSize + 16;
        if (saltSize != 16 || hashSizeOffset + 4 > bytes.Length)
        {
            parsed.Info.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptionInfo.standardVerifierInvalid", "Standard encryption verifier fields are invalid."));
            return;
        }
        int hashSize = XlsxCompoundFile.ReadInt32(bytes, hashSizeOffset);
        int encryptedHashLength = RoundBlock(hashSize, 16);
        if (hashSize != 20 || hashSizeOffset + 4 + encryptedHashLength > bytes.Length)
        {
            parsed.Info.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptionInfo.standardVerifierInvalid", "Standard encryption verifier hash is invalid."));
            return;
        }
        parsed.Salt = Slice(bytes, verifierOffset + 4, saltSize);
        parsed.VerifierInput = Slice(bytes, verifierOffset + 4 + saltSize, 16);
        parsed.VerifierHash = Slice(bytes, hashSizeOffset + 4, encryptedHashLength);
        parsed.HashSize = hashSize;
        parsed.Info.IsSupported = true;
        parsed.Info.IsValid = parsed.Info.Validation.IsValid;
    }

    internal static void ParseAgileInfo(byte[] bytes, XlsxParsedEncryption parsed)
    {
        string xml = Encoding.UTF8.GetString(bytes, 8, bytes.Length - 8);
        string keyData = FindTag(xml, "keyData");
        string encryptedKey = FindTag(xml, "encryptedKey");
        string dataIntegrity = FindTag(xml, "dataIntegrity");
        if (keyData.Length == 0 || encryptedKey.Length == 0 || dataIntegrity.Length == 0)
        {
            parsed.Info.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptionInfo.agileXmlInvalid", "Agile EncryptionInfo XML is missing required elements."));
            return;
        }
        string cipher = ReadAttribute(keyData, "cipherAlgorithm");
        string chaining = ReadAttribute(keyData, "cipherChaining");
        string hash = ReadAttribute(keyData, "hashAlgorithm");
        int keyBits = ReadIntegerAttribute(keyData, "keyBits");
        int blockSize = ReadIntegerAttribute(keyData, "blockSize");
        int hashSize = ReadIntegerAttribute(keyData, "hashSize");
        int spinCount = ReadIntegerAttribute(encryptedKey, "spinCount");
        int encryptedKeyBits = ReadIntegerAttribute(encryptedKey, "keyBits");
        int encryptedBlockSize = ReadIntegerAttribute(encryptedKey, "blockSize");
        int encryptedHashSize = ReadIntegerAttribute(encryptedKey, "hashSize");
        string encryptedCipher = ReadAttribute(encryptedKey, "cipherAlgorithm");
        string encryptedChaining = ReadAttribute(encryptedKey, "cipherChaining");
        string encryptedHash = ReadAttribute(encryptedKey, "hashAlgorithm");
        parsed.Info.Kind = XlsxPackageEncryptionKind.Agile;
        parsed.Info.KeyBits = keyBits;
        parsed.Info.SpinCount = spinCount;
        parsed.Info.Algorithm = AlgorithmFromBits(keyBits);
        if (cipher != "AES" || encryptedCipher != "AES" || chaining != "ChainingModeCBC" || encryptedChaining != "ChainingModeCBC" || hash != "SHA512" || encryptedHash != "SHA512" || blockSize != 16 || encryptedBlockSize != 16 || hashSize != 64 || encryptedHashSize != 64 || keyBits != encryptedKeyBits || parsed.Info.Algorithm == XlsxPackageEncryptionAlgorithm.Unknown || spinCount < 1 || spinCount > 10000000)
        {
            parsed.Info.IsSupported = false;
            parsed.Info.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.algorithmUnsupported", "Agile encryption requires AES CBC with SHA-512, a supported AES key size, and a bounded spin count."));
            return;
        }
        parsed.KeyDataSalt = ReadBase64Attribute(keyData, "saltValue");
        parsed.Salt = ReadBase64Attribute(encryptedKey, "saltValue");
        parsed.VerifierInput = ReadBase64Attribute(encryptedKey, "encryptedVerifierHashInput");
        parsed.VerifierHash = ReadBase64Attribute(encryptedKey, "encryptedVerifierHashValue");
        parsed.EncryptedKey = ReadBase64Attribute(encryptedKey, "encryptedKeyValue");
        parsed.EncryptedHmacKey = ReadBase64Attribute(dataIntegrity, "encryptedHmacKey");
        parsed.EncryptedHmacValue = ReadBase64Attribute(dataIntegrity, "encryptedHmacValue");
        parsed.HashSize = hashSize;
        int encryptedKeyLength = RoundBlock(keyBits / 8, 16);
        if (parsed.KeyDataSalt.Length != 16 || parsed.Salt.Length != 16 || parsed.VerifierInput.Length != 16 || parsed.VerifierHash.Length != 64 || parsed.EncryptedKey.Length != encryptedKeyLength || parsed.EncryptedHmacKey.Length != 64 || parsed.EncryptedHmacValue.Length != 64)
        {
            parsed.Info.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptionInfo.agileValuesInvalid", "Agile EncryptionInfo contains invalid salt or encrypted values."));
            return;
        }
        parsed.Info.IsSupported = true;
        parsed.Info.IsValid = parsed.Info.Validation.IsValid;
    }

    internal static XlsxEncryptedPackageResult EncryptStandard(byte[] packageBytes, char[] password, XlsxPackageEncryptionAlgorithm algorithm, XlsxEncryptedPackageResult result)
    {
        int keyBits = KeyBits(algorithm);
        byte[] salt = RandomBytes(16);
        byte[] verifier = RandomBytes(16);
        byte[] key = DeriveStandardKey(password, salt, keyBits);
        byte[] verifierHash = SHA1.HashData(verifier);
        byte[] encryptedVerifier = TransformAes(verifier, key, new byte[16], CipherMode.ECB, true);
        byte[] encryptedVerifierHash = TransformAes(PadBlock(verifierHash, 16), key, new byte[16], CipherMode.ECB, true);
        byte[] encryptedData = TransformAes(PadBlock(packageBytes, 16), key, new byte[16], CipherMode.ECB, true);
        byte[] encryptedPackage = PrefixLength(encryptedData, packageBytes.Length);
        byte[] encryptionInfo = BuildStandardInfo(salt, encryptedVerifier, encryptedVerifierHash, keyBits);
        Dictionary<string, byte[]> streams = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        streams["EncryptedPackage"] = encryptedPackage;
        streams["EncryptionInfo"] = encryptionInfo;
        result.Bytes = XlsxCompoundFile.Write(streams);
        result.EncryptionInfo = ParseEncryptionInfo(encryptionInfo).Info;
        result.PasswordVerified = true;
        result.IntegrityVerified = false;
        result.IsValid = result.Validation.IsValid;
        ClearBytes(key);
        ClearBytes(verifierHash);
        return result;
    }

    internal static XlsxEncryptedPackageResult DecryptStandard(byte[] encryptedPackage, XlsxParsedEncryption parsed, char[] password, XlsxEncryptedPackageResult result)
    {
        byte[] key = DeriveStandardKey(password, parsed.Salt, parsed.Info.KeyBits);
        byte[] verifier = TransformAes(parsed.VerifierInput, key, new byte[16], CipherMode.ECB, false);
        byte[] expectedHash = SHA1.HashData(verifier);
        byte[] actualHashPadded = TransformAes(parsed.VerifierHash, key, new byte[16], CipherMode.ECB, false);
        byte[] actualHash = Slice(actualHashPadded, 0, expectedHash.Length);
        bool verified = CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        result.PasswordVerified = verified;
        if (!verified)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.passwordInvalid", "Password verification failed."));
            ClearBytes(key);
            ClearBytes(verifier);
            ClearBytes(expectedHash);
            ClearBytes(actualHashPadded);
            ClearBytes(actualHash);
            return result;
        }
        long size = XlsxCompoundFile.ReadInt64(encryptedPackage, 0);
        int ciphertextLength = encryptedPackage.Length - 8;
        if (size < 0 || size > ciphertextLength || ciphertextLength % 16 != 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptedPackage.sizeInvalid", "EncryptedPackage plaintext size is invalid."));
            ClearBytes(key);
            return result;
        }
        byte[] ciphertext = Slice(encryptedPackage, 8, encryptedPackage.Length - 8);
        byte[] plaintextPadded = TransformAes(ciphertext, key, new byte[16], CipherMode.ECB, false);
        if (size > plaintextPadded.Length)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptedPackage.sizeInvalid", "EncryptedPackage plaintext size exceeds decrypted bytes."));
        }
        else
        {
            result.Bytes = Slice(plaintextPadded, 0, (int)size);
        }
        result.IsValid = result.Validation.IsValid;
        ClearBytes(key);
        ClearBytes(verifier);
        ClearBytes(expectedHash);
        ClearBytes(actualHashPadded);
        ClearBytes(actualHash);
        ClearBytes(plaintextPadded);
        return result;
    }

    internal static XlsxEncryptedPackageResult EncryptAgile(byte[] packageBytes, char[] password, XlsxPackageEncryptionAlgorithm algorithm, int spinCount, XlsxEncryptedPackageResult result)
    {
        int keyBits = KeyBits(algorithm);
        byte[] passwordSalt = RandomBytes(16);
        byte[] keyDataSalt = RandomBytes(16);
        byte[] verifier = RandomBytes(16);
        byte[] contentKey = RandomBytes(keyBits / 8);
        byte[] passwordHash = DeriveAgilePasswordHash(password, passwordSalt, spinCount);
        byte[] inputKey = DeriveAgileBlockKey(passwordHash, VerifierInputBlock, keyBits);
        byte[] hashKey = DeriveAgileBlockKey(passwordHash, VerifierHashBlock, keyBits);
        byte[] encryptedKeyKey = DeriveAgileBlockKey(passwordHash, EncryptedKeyBlock, keyBits);
        byte[] verifierIv = Slice(passwordSalt, 0, 16);
        byte[] verifierHashIv = Slice(passwordSalt, 0, 16);
        byte[] encryptedKeyIv = Slice(passwordSalt, 0, 16);
        byte[] encryptedVerifier = TransformAes(PadBlock(verifier, 16), inputKey, verifierIv, CipherMode.CBC, true);
        byte[] verifierHash = SHA512.HashData(verifier);
        byte[] encryptedVerifierHash = TransformAes(PadBlock(verifierHash, 16), hashKey, verifierHashIv, CipherMode.CBC, true);
        byte[] encryptedKeyValue = TransformAes(PadBlock(contentKey, 16), encryptedKeyKey, encryptedKeyIv, CipherMode.CBC, true);
        byte[] encryptedPackage = EncryptAgilePackage(packageBytes, contentKey, keyDataSalt);
        byte[] hmacKey = RandomBytes(64);
        byte[] hmacValue;
        using (HMACSHA512 hmac = new HMACSHA512(hmacKey))
        {
            hmacValue = hmac.ComputeHash(encryptedPackage);
        }
        byte[] hmacKeyIv = DeriveAgileIv(keyDataSalt, HmacKeyBlock);
        byte[] hmacValueIv = DeriveAgileIv(keyDataSalt, HmacValueBlock);
        byte[] encryptedHmacKey = TransformAes(PadBlock(hmacKey, 16), contentKey, hmacKeyIv, CipherMode.CBC, true);
        byte[] encryptedHmacValue = TransformAes(PadBlock(hmacValue, 16), contentKey, hmacValueIv, CipherMode.CBC, true);
        byte[] encryptionInfo = BuildAgileInfo(passwordSalt, keyDataSalt, encryptedVerifier, encryptedVerifierHash, encryptedKeyValue, encryptedHmacKey, encryptedHmacValue, keyBits, spinCount);
        Dictionary<string, byte[]> streams = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        streams["EncryptedPackage"] = encryptedPackage;
        streams["EncryptionInfo"] = encryptionInfo;
        result.Bytes = XlsxCompoundFile.Write(streams);
        result.EncryptionInfo = ParseEncryptionInfo(encryptionInfo).Info;
        result.PasswordVerified = true;
        result.IntegrityVerified = true;
        result.IsValid = result.Validation.IsValid;
        ClearBytes(passwordHash);
        ClearBytes(inputKey);
        ClearBytes(hashKey);
        ClearBytes(encryptedKeyKey);
        ClearBytes(contentKey);
        ClearBytes(hmacKey);
        ClearBytes(hmacValue);
        return result;
    }

    internal static XlsxEncryptedPackageResult DecryptAgile(byte[] encryptedPackage, XlsxParsedEncryption parsed, char[] password, XlsxEncryptedPackageResult result)
    {
        int keyBits = parsed.Info.KeyBits;
        byte[] passwordHash = DeriveAgilePasswordHash(password, parsed.Salt, parsed.Info.SpinCount);
        byte[] inputKey = DeriveAgileBlockKey(passwordHash, VerifierInputBlock, keyBits);
        byte[] hashKey = DeriveAgileBlockKey(passwordHash, VerifierHashBlock, keyBits);
        byte[] encryptedKeyKey = DeriveAgileBlockKey(passwordHash, EncryptedKeyBlock, keyBits);
        byte[] verifier = TransformAes(parsed.VerifierInput, inputKey, parsed.Salt, CipherMode.CBC, false);
        byte[] verifierHashPadded = TransformAes(parsed.VerifierHash, hashKey, parsed.Salt, CipherMode.CBC, false);
        byte[] expectedHash = SHA512.HashData(Slice(verifier, 0, 16));
        byte[] actualHash = Slice(verifierHashPadded, 0, 64);
        bool verified = CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        result.PasswordVerified = verified;
        if (!verified)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.passwordInvalid", "Password verification failed."));
            ClearBytes(passwordHash);
            ClearBytes(inputKey);
            ClearBytes(hashKey);
            ClearBytes(encryptedKeyKey);
            return result;
        }
        byte[] contentKeyPadded = TransformAes(parsed.EncryptedKey, encryptedKeyKey, parsed.Salt, CipherMode.CBC, false);
        byte[] contentKey = Slice(contentKeyPadded, 0, keyBits / 8);
        byte[] hmacKeyPadded = TransformAes(parsed.EncryptedHmacKey, contentKey, DeriveAgileIv(parsed.KeyDataSalt, HmacKeyBlock), CipherMode.CBC, false);
        byte[] hmacValuePadded = TransformAes(parsed.EncryptedHmacValue, contentKey, DeriveAgileIv(parsed.KeyDataSalt, HmacValueBlock), CipherMode.CBC, false);
        byte[] hmacKey = Slice(hmacKeyPadded, 0, 64);
        byte[] expectedHmac = Slice(hmacValuePadded, 0, 64);
        byte[] actualHmac;
        using (HMACSHA512 hmac = new HMACSHA512(hmacKey))
        {
            actualHmac = hmac.ComputeHash(encryptedPackage);
        }
        result.IntegrityVerified = CryptographicOperations.FixedTimeEquals(expectedHmac, actualHmac);
        if (!result.IntegrityVerified)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryption.integrityInvalid", "Agile encrypted-package integrity verification failed."));
        }
        else
        {
            result.Bytes = DecryptAgilePackage(encryptedPackage, contentKey, parsed.KeyDataSalt, result);
        }
        result.IsValid = result.Validation.IsValid;
        ClearBytes(passwordHash);
        ClearBytes(inputKey);
        ClearBytes(hashKey);
        ClearBytes(encryptedKeyKey);
        ClearBytes(contentKeyPadded);
        ClearBytes(contentKey);
        ClearBytes(hmacKeyPadded);
        ClearBytes(hmacValuePadded);
        ClearBytes(hmacKey);
        ClearBytes(expectedHmac);
        ClearBytes(actualHmac);
        return result;
    }

    internal static byte[] EncryptAgilePackage(byte[] packageBytes, byte[] key, byte[] salt)
    {
        List<byte> output = new List<byte>();
        AppendInt64(output, packageBytes.Length);
        int offset = 0;
        int segment = 0;
        while (offset < packageBytes.Length)
        {
            int count = packageBytes.Length - offset;
            if (count > 4096)
            {
                count = 4096;
            }
            byte[] block = Slice(packageBytes, offset, count);
            byte[] encrypted = TransformAes(PadBlock(block, 16), key, DeriveSegmentIv(salt, segment), CipherMode.CBC, true);
            AppendBytes(output, encrypted);
            offset = offset + count;
            segment = segment + 1;
        }
        return output.ToArray();
    }

    internal static byte[] DecryptAgilePackage(byte[] encryptedPackage, byte[] key, byte[] salt, XlsxEncryptedPackageResult result)
    {
        if (encryptedPackage.Length < 8)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptedPackage.sizeInvalid", "EncryptedPackage stream is too short."));
            return new byte[0];
        }
        long size = XlsxCompoundFile.ReadInt64(encryptedPackage, 0);
        if (size < 0 || size > int.MaxValue)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptedPackage.sizeInvalid", "EncryptedPackage plaintext size is invalid."));
            return new byte[0];
        }
        byte[] output = new byte[(int)size];
        int encryptedOffset = 8;
        int outputOffset = 0;
        int segment = 0;
        while (outputOffset < output.Length)
        {
            int plainCount = output.Length - outputOffset;
            if (plainCount > 4096)
            {
                plainCount = 4096;
            }
            int encryptedCount = RoundBlock(plainCount, 16);
            if (encryptedOffset + encryptedCount > encryptedPackage.Length)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "encryptedPackage.dataTruncated", "Agile encrypted package data is truncated."));
                return new byte[0];
            }
            byte[] encrypted = Slice(encryptedPackage, encryptedOffset, encryptedCount);
            byte[] plain = TransformAes(encrypted, key, DeriveSegmentIv(salt, segment), CipherMode.CBC, false);
            Array.Copy(plain, 0, output, outputOffset, plainCount);
            ClearBytes(plain);
            outputOffset = outputOffset + plainCount;
            encryptedOffset = encryptedOffset + encryptedCount;
            segment = segment + 1;
        }
        return output;
    }

    internal static byte[] BuildStandardInfo(byte[] salt, byte[] verifier, byte[] verifierHash, int keyBits)
    {
        string provider = "Microsoft Enhanced RSA and AES Cryptographic Provider\0";
        byte[] providerBytes = Encoding.Unicode.GetBytes(provider);
        int headerSize = 32 + providerBytes.Length;
        byte[] result = new byte[12 + headerSize + 4 + 16 + 16 + 4 + verifierHash.Length];
        result[0] = 0x04;
        result[2] = 0x02;
        XlsxCompoundFile.WriteInt32(result, 4, 0x24);
        XlsxCompoundFile.WriteInt32(result, 8, headerSize);
        XlsxCompoundFile.WriteInt32(result, 12, 0x24);
        XlsxCompoundFile.WriteInt32(result, 16, 0);
        XlsxCompoundFile.WriteInt32(result, 20, AlgorithmId(keyBits));
        XlsxCompoundFile.WriteInt32(result, 24, 0x8004);
        XlsxCompoundFile.WriteInt32(result, 28, keyBits);
        XlsxCompoundFile.WriteInt32(result, 32, 0x18);
        XlsxCompoundFile.WriteInt32(result, 36, 0);
        XlsxCompoundFile.WriteInt32(result, 40, 0);
        Array.Copy(providerBytes, 0, result, 44, providerBytes.Length);
        int offset = 12 + headerSize;
        XlsxCompoundFile.WriteInt32(result, offset, 16);
        Array.Copy(salt, 0, result, offset + 4, salt.Length);
        Array.Copy(verifier, 0, result, offset + 20, verifier.Length);
        XlsxCompoundFile.WriteInt32(result, offset + 36, 20);
        Array.Copy(verifierHash, 0, result, offset + 40, verifierHash.Length);
        return result;
    }

    internal static byte[] BuildAgileInfo(byte[] passwordSalt, byte[] keyDataSalt, byte[] verifier, byte[] verifierHash, byte[] encryptedKey, byte[] encryptedHmacKey, byte[] encryptedHmacValue, int keyBits, int spinCount)
    {
        StringBuilder xml = new StringBuilder();
        xml.Append("<encryption xmlns=\"http://schemas.microsoft.com/office/2006/encryption\" xmlns:p=\"http://schemas.microsoft.com/office/2006/keyEncryptor/password\">");
        xml.Append("<keyData saltSize=\"16\" blockSize=\"16\" keyBits=\"");
        xml.Append(keyBits);
        xml.Append("\" hashSize=\"64\" cipherAlgorithm=\"AES\" cipherChaining=\"ChainingModeCBC\" hashAlgorithm=\"SHA512\" saltValue=\"");
        xml.Append(Convert.ToBase64String(keyDataSalt));
        xml.Append("\"/>");
        xml.Append("<dataIntegrity encryptedHmacKey=\"");
        xml.Append(Convert.ToBase64String(encryptedHmacKey));
        xml.Append("\" encryptedHmacValue=\"");
        xml.Append(Convert.ToBase64String(encryptedHmacValue));
        xml.Append("\"/>");
        xml.Append("<keyEncryptors><keyEncryptor uri=\"http://schemas.microsoft.com/office/2006/keyEncryptor/password\"><p:encryptedKey spinCount=\"");
        xml.Append(spinCount);
        xml.Append("\" saltSize=\"16\" blockSize=\"16\" keyBits=\"");
        xml.Append(keyBits);
        xml.Append("\" hashSize=\"64\" cipherAlgorithm=\"AES\" cipherChaining=\"ChainingModeCBC\" hashAlgorithm=\"SHA512\" saltValue=\"");
        xml.Append(Convert.ToBase64String(passwordSalt));
        xml.Append("\" encryptedVerifierHashInput=\"");
        xml.Append(Convert.ToBase64String(verifier));
        xml.Append("\" encryptedVerifierHashValue=\"");
        xml.Append(Convert.ToBase64String(verifierHash));
        xml.Append("\" encryptedKeyValue=\"");
        xml.Append(Convert.ToBase64String(encryptedKey));
        xml.Append("\"/></keyEncryptor></keyEncryptors></encryption>");
        byte[] xmlBytes = Encoding.UTF8.GetBytes(xml.ToString());
        byte[] result = new byte[8 + xmlBytes.Length];
        result[0] = 0x04;
        result[2] = 0x04;
        result[4] = 0x40;
        Array.Copy(xmlBytes, 0, result, 8, xmlBytes.Length);
        return result;
    }

    internal static byte[] DeriveStandardKey(char[] password, byte[] salt, int keyBits)
    {
        byte[] passwordBytes = Encoding.Unicode.GetBytes(password);
        byte[] initial = Combine(salt, passwordBytes);
        byte[] hash = SHA1.HashData(initial);
        int iteration = 0;
        while (iteration < 50000)
        {
            byte[] counter = CounterBytes(iteration);
            byte[] iterationInput = Combine(counter, hash);
            byte[] next = SHA1.HashData(iterationInput);
            ClearBytes(hash);
            ClearBytes(iterationInput);
            hash = next;
            iteration = iteration + 1;
        }
        byte[] finalInput = Combine(hash, CounterBytes(0));
        byte[] finalHash = SHA1.HashData(finalInput);
        byte[] pad1 = new byte[64];
        byte[] pad2 = new byte[64];
        int index = 0;
        while (index < 64)
        {
            byte source = index < finalHash.Length ? finalHash[index] : (byte)0;
            pad1[index] = (byte)(source ^ 0x36);
            pad2[index] = (byte)(source ^ 0x5C);
            index = index + 1;
        }
        byte[] first = SHA1.HashData(pad1);
        byte[] second = SHA1.HashData(pad2);
        byte[] keyMaterial = Combine(first, second);
        byte[] key = Slice(keyMaterial, 0, keyBits / 8);
        ClearBytes(passwordBytes);
        ClearBytes(initial);
        ClearBytes(hash);
        ClearBytes(finalInput);
        ClearBytes(finalHash);
        ClearBytes(pad1);
        ClearBytes(pad2);
        ClearBytes(first);
        ClearBytes(second);
        ClearBytes(keyMaterial);
        return key;
    }

    internal static byte[] DeriveAgilePasswordHash(char[] password, byte[] salt, int spinCount)
    {
        byte[] passwordBytes = Encoding.Unicode.GetBytes(password);
        byte[] initial = Combine(salt, passwordBytes);
        byte[] hash = SHA512.HashData(initial);
        int iteration = 0;
        while (iteration < spinCount)
        {
            byte[] input = Combine(CounterBytes(iteration), hash);
            byte[] next = SHA512.HashData(input);
            ClearBytes(hash);
            ClearBytes(input);
            hash = next;
            iteration = iteration + 1;
        }
        ClearBytes(passwordBytes);
        ClearBytes(initial);
        return hash;
    }

    internal static byte[] DeriveAgileBlockKey(byte[] passwordHash, byte[] blockKey, int keyBits)
    {
        byte[] input = Combine(passwordHash, blockKey);
        byte[] hash = SHA512.HashData(input);
        byte[] key = new byte[keyBits / 8];
        int index = 0;
        while (index < key.Length)
        {
            key[index] = index < hash.Length ? hash[index] : (byte)0x36;
            index = index + 1;
        }
        ClearBytes(input);
        ClearBytes(hash);
        return key;
    }

    internal static byte[] DeriveAgileIv(byte[] salt, byte[] blockKey)
    {
        byte[] input = Combine(salt, blockKey);
        byte[] hash = SHA512.HashData(input);
        byte[] iv = Slice(hash, 0, 16);
        ClearBytes(input);
        ClearBytes(hash);
        return iv;
    }

    internal static byte[] DeriveSegmentIv(byte[] salt, int segment)
    {
        return DeriveAgileIv(salt, CounterBytes(segment));
    }

    internal static byte[] TransformAes(byte[] input, byte[] key, byte[] iv, CipherMode mode, bool encrypt)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = mode;
            aes.Padding = PaddingMode.None;
            using (ICryptoTransform transform = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor())
            {
                return transform.TransformFinalBlock(input, 0, input.Length);
            }
        }
    }

    internal static byte[] PrefixLength(byte[] bytes, int length)
    {
        byte[] result = new byte[8 + bytes.Length];
        XlsxCompoundFile.WriteInt64(result, 0, length);
        Array.Copy(bytes, 0, result, 8, bytes.Length);
        return result;
    }

    internal static byte[] PadBlock(byte[] bytes, int blockSize)
    {
        int length = RoundBlock(bytes.Length, blockSize);
        byte[] result = new byte[length];
        Array.Copy(bytes, result, bytes.Length);
        return result;
    }

    internal static int RoundBlock(int length, int blockSize)
    {
        if (length == 0)
        {
            return 0;
        }
        return ((length + blockSize - 1) / blockSize) * blockSize;
    }

    internal static byte[] Slice(byte[] bytes, int offset, int count)
    {
        byte[] result = new byte[count];
        Array.Copy(bytes, offset, result, 0, count);
        return result;
    }

    internal static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, result, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }

    internal static byte[] CounterBytes(int value)
    {
        byte[] result = new byte[4];
        XlsxCompoundFile.WriteInt32(result, 0, value);
        return result;
    }

    internal static byte[] RandomBytes(int count)
    {
        byte[] result = new byte[count];
        RandomNumberGenerator.Fill(result);
        return result;
    }

    internal static int KeyBits(XlsxPackageEncryptionAlgorithm algorithm)
    {
        if (algorithm == XlsxPackageEncryptionAlgorithm.Aes128)
        {
            return 128;
        }
        if (algorithm == XlsxPackageEncryptionAlgorithm.Aes192)
        {
            return 192;
        }
        if (algorithm == XlsxPackageEncryptionAlgorithm.Aes256)
        {
            return 256;
        }
        return 0;
    }

    internal static int PasswordCodePointCount(char[] password)
    {
        int count = 0;
        int index = 0;
        while (index < password.Length)
        {
            if (char.IsHighSurrogate(password[index]) && index + 1 < password.Length && char.IsLowSurrogate(password[index + 1]))
            {
                index = index + 2;
            }
            else
            {
                index = index + 1;
            }
            count = count + 1;
        }
        return count;
    }

    internal static XlsxPackageEncryptionAlgorithm AlgorithmFromBits(int keyBits)
    {
        if (keyBits == 128)
        {
            return XlsxPackageEncryptionAlgorithm.Aes128;
        }
        if (keyBits == 192)
        {
            return XlsxPackageEncryptionAlgorithm.Aes192;
        }
        if (keyBits == 256)
        {
            return XlsxPackageEncryptionAlgorithm.Aes256;
        }
        return XlsxPackageEncryptionAlgorithm.Unknown;
    }

    internal static int AlgorithmId(int keyBits)
    {
        if (keyBits == 128)
        {
            return 0x660E;
        }
        if (keyBits == 192)
        {
            return 0x660F;
        }
        return 0x6610;
    }

    internal static bool MatchesStandardAlgorithm(int algorithmId, int keyBits)
    {
        return (algorithmId == 0x660E && keyBits == 128) || (algorithmId == 0x660F && keyBits == 192) || (algorithmId == 0x6610 && keyBits == 256);
    }

    internal static string FindTag(string xml, string localName)
    {
        string first = "<" + localName;
        int offset = xml.IndexOf(first, StringComparison.Ordinal);
        if (offset < 0)
        {
            string prefixed = ":" + localName;
            offset = xml.IndexOf(prefixed, StringComparison.Ordinal);
            if (offset < 0)
            {
                return "";
            }
            offset = xml.LastIndexOf('<', offset);
        }
        int end = xml.IndexOf('>', offset);
        if (end < 0)
        {
            return "";
        }
        return xml.Substring(offset, end - offset + 1);
    }

    internal static string ReadAttribute(string tag, string name)
    {
        string marker = name + "=\"";
        int start = tag.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return "";
        }
        start = start + marker.Length;
        int end = tag.IndexOf('"', start);
        if (end < 0)
        {
            return "";
        }
        return tag.Substring(start, end - start);
    }

    internal static int ReadIntegerAttribute(string tag, string name)
    {
        int value;
        if (int.TryParse(ReadAttribute(tag, name), out value))
        {
            return value;
        }
        return 0;
    }

    internal static byte[] ReadBase64Attribute(string tag, string name)
    {
        string value = ReadAttribute(tag, name);
        if (!IsBase64(value))
        {
            return new byte[0];
        }
        return Convert.FromBase64String(value);
    }

    internal static bool IsBase64(string value)
    {
        if (value.Length == 0 || value.Length % 4 != 0)
        {
            return false;
        }
        int index = 0;
        bool padding = false;
        while (index < value.Length)
        {
            char character = value[index];
            bool letter = (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
            bool digit = character >= '0' && character <= '9';
            if (character == '=')
            {
                padding = true;
                if (index < value.Length - 2)
                {
                    return false;
                }
            }
            else if (padding || (!letter && !digit && character != '+' && character != '/'))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal static void AppendInt64(List<byte> output, long value)
    {
        byte[] bytes = new byte[8];
        XlsxCompoundFile.WriteInt64(bytes, 0, value);
        AppendBytes(output, bytes);
    }

    internal static void AppendBytes(List<byte> output, byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            output.Add(bytes[index]);
            index = index + 1;
        }
    }

    internal static void ClearBytes(byte[] bytes)
    {
        CryptographicOperations.ZeroMemory(bytes);
    }

    internal static void CopyDetectionDiagnostics(XlsxEncryptedPackageDetectionResult target, XlsxValidationResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }

    internal static void CopyOperationDiagnostics(XlsxEncryptedPackageResult target, XlsxValidationResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }
}
