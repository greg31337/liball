using System;

using System.Security.Cryptography;

namespace random;

internal class HmacDrbgSha256Engine
{
    internal const int AlgorithmVersion = 1;
    internal const int OutputByteCount = 32;
    internal const int SecurityStrengthBits = 256;
    internal const int MinimumEntropyBytes = 32;
    internal const int MinimumNonceBytes = 16;
    internal const int MaximumRequestBytes = 65536;
    internal const long MaximumInputBytes = 4294967296L;
    internal const ulong ReseedInterval = 281474976710656UL;
    byte[] key;
    byte[] value;
    ulong reseedCounter;
    bool instantiated;

    internal HmacDrbgSha256Engine(byte[] entropy, byte[] nonce, byte[] personalization)
    {
        Guard.NotNull(entropy, "entropy");
        Guard.NotNull(nonce, "nonce");
        Guard.NotNull(personalization, "personalization");
        ValidateEntropyLength(entropy.Length, "entropy");
        ValidateNonceLength(nonce.Length, "nonce");
        ValidateOptionalInputLength(personalization.Length, "personalization");
        ValidateCombinedLength((long)entropy.Length + nonce.Length + personalization.Length, "personalization");
        key = new byte[OutputByteCount];
        value = new byte[OutputByteCount];
        Array.Fill(value, (byte)0x01);
        instantiated = true;
        byte[] seedMaterial = Concatenate(entropy, nonce, personalization);
        Update(seedMaterial);
        CryptographicOperations.ZeroMemory(seedMaterial);
        reseedCounter = 1UL;
    }

    internal void Generate(Span<byte> destination)
    {
        GenerateWithAdditionalInput(destination, new byte[0]);
    }

    internal void GenerateWithAdditionalInput(Span<byte> destination, byte[] additionalInput)
    {
        EnsureInstantiated();
        Guard.NotNull(additionalInput, "additionalInput");
        ValidateRequestLength(destination.Length);
        ValidateOptionalInputLength(additionalInput.Length, "additionalInput");
        if (destination.Length == 0)
        {
            return;
        }

        EnsureReseedNotRequired();
        if (additionalInput.Length != 0)
        {
            Update(additionalInput);
        }

        int offset = 0;
        while (offset < destination.Length)
        {
            byte[] nextValue = HMACSHA256.HashData(key, value);
            CryptographicOperations.ZeroMemory(value);
            value = nextValue;
            int count = destination.Length - offset;
            if (count > OutputByteCount)
            {
                count = OutputByteCount;
            }

            value.AsSpan(0, count).CopyTo(destination.Slice(offset, count));
            offset += count;
        }

        Update(additionalInput);
        reseedCounter++;
    }

    internal void GenerateWithOptions(Span<byte> destination, byte[] additionalInput, bool predictionResistance)
    {
        EnsureInstantiated();
        Guard.NotNull(additionalInput, "additionalInput");
        ValidateRequestLength(destination.Length);
        ValidateOptionalInputLength(additionalInput.Length, "additionalInput");
        if (destination.Length == 0)
        {
            return;
        }

        if (!predictionResistance)
        {
            GenerateWithAdditionalInput(destination, additionalInput);
            return;
        }

        byte[] entropy = new byte[MinimumEntropyBytes];
        SystemCryptoEngine source = new SystemCryptoEngine();
        source.FillBytes(entropy);
        GeneratePredictionResistant(destination, additionalInput, entropy);
        CryptographicOperations.ZeroMemory(entropy);
    }

    internal void GeneratePredictionResistant(Span<byte> destination, byte[] additionalInput, byte[] entropy)
    {
        EnsureInstantiated();
        Guard.NotNull(additionalInput, "additionalInput");
        Guard.NotNull(entropy, "entropy");
        ValidateRequestLength(destination.Length);
        ValidateOptionalInputLength(additionalInput.Length, "additionalInput");
        ValidateEntropyLength(entropy.Length, "entropy");
        ValidateCombinedLength((long)entropy.Length + additionalInput.Length, "additionalInput");
        if (destination.Length == 0)
        {
            return;
        }

        Reseed(entropy, additionalInput);
        GenerateWithAdditionalInput(destination, new byte[0]);
    }

    internal void Reseed(byte[] entropy, byte[] additionalInput)
    {
        EnsureInstantiated();
        Guard.NotNull(entropy, "entropy");
        Guard.NotNull(additionalInput, "additionalInput");
        ValidateEntropyLength(entropy.Length, "entropy");
        ValidateOptionalInputLength(additionalInput.Length, "additionalInput");
        ValidateCombinedLength((long)entropy.Length + additionalInput.Length, "additionalInput");
        byte[] seedMaterial = Concatenate(entropy, additionalInput);
        Update(seedMaterial);
        CryptographicOperations.ZeroMemory(seedMaterial);
        reseedCounter = 1UL;
    }

    internal void Update(byte[] providedData)
    {
        EnsureInstantiated();
        Guard.NotNull(providedData, "providedData");
        UpdateRound(0x00, providedData);
        if (providedData.Length != 0)
        {
            UpdateRound(0x01, providedData);
        }
    }

    internal void Uninstantiate()
    {
        if (!instantiated)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(key);
        CryptographicOperations.ZeroMemory(value);
        key = new byte[0];
        value = new byte[0];
        reseedCounter = 0UL;
        instantiated = false;
    }

    internal byte[] KeyBytes()
    {
        return (byte[])key.Clone();
    }

    internal byte[] ValueBytes()
    {
        return (byte[])value.Clone();
    }

    internal ulong ReseedCounter()
    {
        return reseedCounter;
    }

    internal bool IsInstantiated()
    {
        return instantiated;
    }

    internal RandomAlgorithmInfo Info()
    {
        return new RandomAlgorithmInfo(
            RandomAlgorithm.HmacDrbgSha256V1,
            "HMAC-DRBG SHA-256",
            AlgorithmVersion,
            StateCodec.CurrentTransformVersion,
            8,
            0,
            true,
            "Reseed required after 2^48 successful nonempty generate requests",
            [RandomCapability.Reseed, RandomCapability.AdditionalInput, RandomCapability.PredictionResistance, RandomCapability.Uninstantiate]);
    }

    internal void EnsureInstantiated()
    {
        if (!instantiated)
        {
            throw RandomSecurityException.Create("The HMAC-DRBG instance has been uninstantiated.");
        }
    }

    internal void EnsureReseedNotRequired()
    {
        if (reseedCounter > ReseedInterval)
        {
            throw RandomReseedRequiredException.Create("The HMAC-DRBG reseed interval has been exhausted.");
        }
    }

    internal static void ValidateEntropyLength(long byteLength, string argumentName)
    {
        if (byteLength < MinimumEntropyBytes || byteLength > MaximumInputBytes)
        {
            throw RandomArgumentException.Create(argumentName, "Entropy must contain between 32 and 4294967296 bytes.");
        }
    }

    internal static void ValidateNonceLength(long byteLength, string argumentName)
    {
        if (byteLength < MinimumNonceBytes || byteLength > MaximumInputBytes)
        {
            throw RandomArgumentException.Create(argumentName, "The nonce must contain between 16 and 4294967296 bytes.");
        }
    }

    internal static void ValidateOptionalInputLength(long byteLength, string argumentName)
    {
        if (byteLength < 0L || byteLength > MaximumInputBytes)
        {
            throw RandomArgumentException.Create(argumentName, "The input must contain at most 4294967296 bytes.");
        }
    }

    internal static void ValidateRequestLength(int byteLength)
    {
        if (byteLength < 0 || byteLength > MaximumRequestBytes)
        {
            throw RandomArgumentException.Create("destination", "An HMAC-DRBG request must not exceed 65536 bytes.");
        }
    }

    internal static void ValidateCombinedLength(long byteLength, string argumentName)
    {
        if (byteLength > int.MaxValue)
        {
            throw RandomArgumentException.Create(argumentName, "The combined input is too large for one HMAC-DRBG operation.");
        }
    }

    internal void UpdateRound(byte separator, byte[] providedData)
    {
        byte[] input = new byte[value.Length + 1 + providedData.Length];
        Array.Copy(value, input, value.Length);
        input[value.Length] = separator;
        Array.Copy(providedData, 0, input, value.Length + 1, providedData.Length);
        byte[] nextKey = HMACSHA256.HashData(key, input);
        CryptographicOperations.ZeroMemory(input);
        CryptographicOperations.ZeroMemory(key);
        key = nextKey;
        byte[] nextValue = HMACSHA256.HashData(key, value);
        CryptographicOperations.ZeroMemory(value);
        value = nextValue;
    }

    internal static byte[] Concatenate(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, result, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }

    internal static byte[] Concatenate(byte[] first, byte[] second, byte[] third)
    {
        byte[] result = new byte[first.Length + second.Length + third.Length];
        Array.Copy(first, result, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        Array.Copy(third, 0, result, first.Length + second.Length, third.Length);
        return result;
    }
}
