using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace fake;

internal static class CryptocurrencyGenerator
{
    const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    const string HexAlphabet = "0123456789abcdef";
    const string Marker = "fake.synthetic-crypto.v1";

    internal static string BitcoinAddress(
        FakeRandom random,
        int maximumCount,
        int maximumOutputSize)
    {
        return Base58Address(
            random,
            0x00,
            maximumCount,
            maximumOutputSize);
    }

    internal static string LitecoinAddress(
        FakeRandom random,
        int maximumCount,
        int maximumOutputSize)
    {
        return Base58Address(
            random,
            0x30,
            maximumCount,
            maximumOutputSize);
    }

    internal static string EthereumAddress(
        FakeRandom random,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(20, maximumCount, "ethereumAddress");
        Guard.OutputSize(42, maximumOutputSize, "ethereumAddress");
        byte[] payload = new byte[20];
        random.Fill(payload);
        return EthereumChecksumAddress(LowerHex(payload));
    }

    internal static bool IsBitcoinAddressValid(
        string value,
        int maximumCount)
    {
        return IsBase58AddressValid(
            value,
            0x00,
            0x6f,
            maximumCount);
    }

    internal static bool IsLitecoinAddressValid(
        string value,
        int maximumCount)
    {
        return IsBase58AddressValid(
            value,
            0x30,
            0x6f,
            maximumCount);
    }

    internal static bool IsEthereumAddressValid(
        string value,
        int maximumCount)
    {
        Guard.NotNull(value, "value");
        Guard.Count(value.Length, maximumCount, "value");
        if (value.Length != 42 || value[0] != '0' || value[1] != 'x')
        {
            return false;
        }
        bool hasLower = false;
        bool hasUpper = false;
        int index = 2;
        while (index < value.Length)
        {
            char character = value[index];
            if (!HexCharacter(character))
            {
                return false;
            }
            if (character >= 'a' && character <= 'f')
            {
                hasLower = true;
            }
            else if (character >= 'A' && character <= 'F')
            {
                hasUpper = true;
            }
            index = index + 1;
        }
        if (!hasLower || !hasUpper)
        {
            return true;
        }
        string lower = value.Substring(2).ToLowerInvariant();
        return value == EthereumChecksumAddress(lower);
    }

    internal static string Address(
        FakeRandom random,
        Cryptocurrency cryptocurrency,
        int maximumCount,
        int maximumOutputSize)
    {
        switch (cryptocurrency)
        {
            case Cryptocurrency.Bitcoin:
                return BitcoinAddress(
                    random,
                    maximumCount,
                    maximumOutputSize);
            case Cryptocurrency.Ethereum:
                return EthereumAddress(
                    random,
                    maximumCount,
                    maximumOutputSize);
            case Cryptocurrency.Litecoin:
                return LitecoinAddress(
                    random,
                    maximumCount,
                    maximumOutputSize);
            default:
                throw new ArgumentOutOfRangeException(
                    "cryptocurrency",
                    cryptocurrency,
                    "cryptocurrency is not supported.");
        }
    }

    internal static bool IsAddressValid(
        Cryptocurrency cryptocurrency,
        string value,
        int maximumCount)
    {
        switch (cryptocurrency)
        {
            case Cryptocurrency.Bitcoin:
                return IsBitcoinAddressValid(value, maximumCount);
            case Cryptocurrency.Ethereum:
                return IsEthereumAddressValid(value, maximumCount);
            case Cryptocurrency.Litecoin:
                return IsLitecoinAddressValid(value, maximumCount);
            default:
                throw new ArgumentOutOfRangeException(
                    "cryptocurrency",
                    cryptocurrency,
                    "cryptocurrency is not supported.");
        }
    }

    internal static CryptoAddressRecord Record(
        FakeRandom random,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(20, maximumCount, "cryptoAddress");
        Guard.OutputSize(112, maximumOutputSize, "cryptoAddress");
        return Record(
            random,
            (Cryptocurrency)random.IntFromMaximumExclusive(3),
            maximumCount,
            maximumOutputSize);
    }

    internal static CryptoAddressRecord Record(
        FakeRandom random,
        Cryptocurrency cryptocurrency,
        int maximumCount,
        int maximumOutputSize)
    {
        Validate(cryptocurrency);
        Guard.Count(20, maximumCount, "cryptoAddress");
        int maximumSize = cryptocurrency == Cryptocurrency.Ethereum ? 112 : 104;
        Guard.OutputSize(maximumSize, maximumOutputSize, "cryptoAddress");
        CryptoAddressRecord result = new CryptoAddressRecord();
        result.Cryptocurrency = cryptocurrency;
        result.Symbol = Symbol(cryptocurrency);
        result.Address = Address(
            random,
            cryptocurrency,
            maximumCount,
            FakeLimits.DefaultMaximumOutputSize);
        if (cryptocurrency == Cryptocurrency.Ethereum)
        {
            result.Format = "EIP-55 hexadecimal";
            result.Network = "network-unspecified";
            result.ChecksumKind = "EIP-55";
        }
        else
        {
            result.Format = "P2PKH Base58Check";
            result.Network = "mainnet-shaped";
            result.ChecksumKind = "Base58Check";
        }
        result.IsChecksumValid = IsAddressValid(
            cryptocurrency,
            result.Address,
            FakeLimits.DefaultMaximumCount);
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.ContainsPrivateKey = false;
        result.IsSpendable = false;
        result.RepresentsActiveWallet = false;
        Guard.OutputSize(
            Size(result),
            maximumOutputSize,
            "cryptoAddress");
        return result;
    }

    internal static string Hash(
        FakeRandom random,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(32, maximumCount, "cryptoHash");
        Guard.OutputSize(64, maximumOutputSize, "cryptoHash");
        byte[] value = new byte[32];
        random.Fill(value);
        return LowerHex(value);
    }

    internal static string Hash(
        FakeRandom random,
        Cryptocurrency cryptocurrency,
        int maximumCount,
        int maximumOutputSize)
    {
        Validate(cryptocurrency);
        int length = cryptocurrency == Cryptocurrency.Ethereum ? 66 : 64;
        Guard.OutputSize(length, maximumOutputSize, "cryptoHash");
        string result = Hash(
            random,
            maximumCount,
            FakeLimits.DefaultMaximumOutputSize);
        return cryptocurrency == Cryptocurrency.Ethereum
            ? "0x" + result
            : result;
    }

    static string Base58Address(
        FakeRandom random,
        byte version,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(20, maximumCount, "cryptoAddress");
        Guard.OutputSize(35, maximumOutputSize, "cryptoAddress");
        byte[] versioned = new byte[21];
        versioned[0] = version;
        byte[] payload = new byte[20];
        random.Fill(payload);
        Array.Copy(payload, 0, versioned, 1, payload.Length);
        byte[] checksum = DoubleSha256(versioned);
        byte[] complete = new byte[25];
        Array.Copy(versioned, complete, versioned.Length);
        Array.Copy(checksum, 0, complete, versioned.Length, 4);
        string result = Base58(complete);
        Guard.OutputSize(result.Length, maximumOutputSize, "cryptoAddress");
        return result;
    }

    static bool IsBase58AddressValid(
        string value,
        byte mainVersion,
        byte testVersion,
        int maximumCount)
    {
        Guard.NotNull(value, "value");
        Guard.Count(value.Length, maximumCount, "value");
        if (value.Length < 26 || value.Length > 35)
        {
            return false;
        }
        byte[]? decoded = Base58Bytes(value);
        if (decoded == null || decoded.Length != 25 ||
            decoded[0] != mainVersion && decoded[0] != testVersion)
        {
            return false;
        }
        byte[] versioned = new byte[21];
        Array.Copy(decoded, versioned, versioned.Length);
        byte[] expected = DoubleSha256(versioned);
        int index = 0;
        while (index < 4)
        {
            if (decoded[21 + index] != expected[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static string Base58(byte[] value)
    {
        int leading = 0;
        while (leading < value.Length && value[leading] == 0)
        {
            leading = leading + 1;
        }
        BigInteger number = new BigInteger(value, true, true);
        StringBuilder reverse = new StringBuilder();
        while (number > BigInteger.Zero)
        {
            int remainder = (int)(number % 58);
            reverse.Append(Base58Alphabet[remainder]);
            number = number / 58;
        }
        StringBuilder result = new StringBuilder(leading + reverse.Length);
        int index = 0;
        while (index < leading)
        {
            result.Append('1');
            index = index + 1;
        }
        index = reverse.Length - 1;
        while (index >= 0)
        {
            result.Append(reverse[index]);
            index = index - 1;
        }
        return result.ToString();
    }

    static byte[]? Base58Bytes(string value)
    {
        BigInteger number = BigInteger.Zero;
        int index = 0;
        while (index < value.Length)
        {
            int digit = Base58Alphabet.IndexOf(value[index]);
            if (digit < 0)
            {
                return null;
            }
            number = number * 58 + digit;
            index = index + 1;
        }
        int leading = 0;
        while (leading < value.Length && value[leading] == '1')
        {
            leading = leading + 1;
        }
        byte[] encoded = number == BigInteger.Zero
            ? new byte[0]
            : number.ToByteArray(true, true);
        byte[] result = new byte[leading + encoded.Length];
        Array.Copy(encoded, 0, result, leading, encoded.Length);
        return result;
    }

    static byte[] DoubleSha256(byte[] value)
    {
        return SHA256.HashData(SHA256.HashData(value));
    }

    static string EthereumChecksumAddress(string lowerHex)
    {
        byte[] hash = Keccak256.Hash(Encoding.ASCII.GetBytes(lowerHex));
        char[] result = new char[42];
        result[0] = '0';
        result[1] = 'x';
        int index = 0;
        while (index < lowerHex.Length)
        {
            char character = lowerHex[index];
            int hashNibble = index % 2 == 0
                ? hash[index / 2] >> 4
                : hash[index / 2] & 15;
            result[index + 2] = character >= 'a' && character <= 'f' && hashNibble >= 8
                ? char.ToUpperInvariant(character)
                : character;
            index = index + 1;
        }
        return new string(result);
    }

    static string LowerHex(byte[] value)
    {
        char[] result = new char[value.Length * 2];
        int index = 0;
        while (index < value.Length)
        {
            result[index * 2] = HexAlphabet[value[index] >> 4];
            result[index * 2 + 1] = HexAlphabet[value[index] & 15];
            index = index + 1;
        }
        return new string(result);
    }

    static bool HexCharacter(char value)
    {
        return value >= '0' && value <= '9' ||
            value >= 'a' && value <= 'f' ||
            value >= 'A' && value <= 'F';
    }

    static string Symbol(Cryptocurrency cryptocurrency)
    {
        switch (cryptocurrency)
        {
            case Cryptocurrency.Bitcoin:
                return "BTC";
            case Cryptocurrency.Ethereum:
                return "ETH";
            case Cryptocurrency.Litecoin:
                return "LTC";
            default:
                throw new ArgumentOutOfRangeException(
                    "cryptocurrency",
                    cryptocurrency,
                    "cryptocurrency is not supported.");
        }
    }

    static void Validate(Cryptocurrency cryptocurrency)
    {
        switch (cryptocurrency)
        {
            case Cryptocurrency.Bitcoin:
            case Cryptocurrency.Ethereum:
            case Cryptocurrency.Litecoin:
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    "cryptocurrency",
                    cryptocurrency,
                    "cryptocurrency is not supported.");
        }
    }

    static int Size(CryptoAddressRecord value)
    {
        return value.Symbol.Length + value.Address.Length + value.Format.Length +
            value.Network.Length + value.ChecksumKind.Length + value.TestMarker.Length;
    }
}
