using System.Security.Cryptography;
using System.Text;

namespace fake;

internal static class InternetCompletionGenerator
{
    const string ShortCodeAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    internal static System.Net.IPAddress Address(
        GenerationContext context,
        FakeRandom random,
        NetworkAddressKind kind)
    {
        ValidateKind(kind);
        if (random.Bool())
        {
            return Ipv4Generator.Address(context, random, kind);
        }
        return Ipv6Generator.Address(context, random, kind);
    }

    internal static System.Net.IPEndPoint Endpoint(
        GenerationContext context,
        FakeRandom random,
        NetworkAddressKind kind,
        int port)
    {
        ValidateKind(kind);
        ValidatePort(port);
        if (random.Bool())
        {
            return Ipv4Generator.Endpoint(context, random, kind, port);
        }
        return Ipv6Generator.Endpoint(context, random, kind, port);
    }

    internal static System.Net.IPEndPoint EndpointWithDynamicPort(
        GenerationContext context,
        FakeRandom random,
        NetworkAddressKind kind)
    {
        ValidateKind(kind);
        int port = random.IntFromMinimumAndMaximum(49152, 65536);
        return Endpoint(context, random, kind, port);
    }

    internal static string GravatarHash(
        string email,
        int maximumOutputSize)
    {
        email = Guard.NotNullOrWhiteSpace(email, "email");
        UnicodeText.Validate(email, "email");
        Guard.OutputSize(32, maximumOutputSize, "gravatarHash");
        string normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length == 0)
        {
            throw new System.ArgumentException(
                "email cannot be empty or whitespace.",
                "email");
        }
        byte[] input = Encoding.UTF8.GetBytes(normalized);
        byte[] hash = MD5.HashData(input);
        char[] result = new char[32];
        HexIdentifierCodec.Format(
            new System.ReadOnlySpan<byte>(hash),
            new System.Span<char>(result));
        return new string(result);
    }

    internal static string ShortCode(
        FakeRandom random,
        int length,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Positive(length, "length");
        Guard.Count(length, maximumCount, "length");
        return CharacterStringGenerator.Generate(
            random,
            ShortCodeAlphabet,
            length,
            true,
            maximumOutputSize,
            maximumCount,
            true);
    }

    static void ValidateKind(NetworkAddressKind kind)
    {
        switch (kind)
        {
            case NetworkAddressKind.Documentation:
            case NetworkAddressKind.Private:
            case NetworkAddressKind.Loopback:
            case NetworkAddressKind.LinkLocal:
            case NetworkAddressKind.Multicast:
            case NetworkAddressKind.Public:
                return;
            default:
                throw new System.ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported IP address category.");
        }
    }

    static void ValidatePort(int port)
    {
        if (port < 0 || port > 65535)
        {
            throw new System.ArgumentOutOfRangeException(
                "port",
                port,
                "port must be between 0 and 65535, inclusive.");
        }
    }
}
