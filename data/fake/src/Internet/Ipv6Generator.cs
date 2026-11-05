using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace fake;

internal class Ipv6Block
{
    internal byte[] Network;
    internal int PrefixLength;

    internal Ipv6Block(byte[] network, int prefixLength)
    {
        Network = network;
        PrefixLength = prefixLength;
    }
}

internal static class Ipv6Generator
{
    static readonly Ipv6Block[] PrivateBlocks = new Ipv6Block[]
    {
        new Ipv6Block(new byte[]
        {
            0xfd, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0
        }, 8)
    };

    static readonly Ipv6Block[] LoopbackBlocks = new Ipv6Block[]
    {
        new Ipv6Block(new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 1
        }, 128)
    };

    static readonly Ipv6Block[] LinkLocalBlocks = new Ipv6Block[]
    {
        new Ipv6Block(new byte[]
        {
            0xfe, 0x80, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0
        }, 10)
    };

    static readonly Ipv6Block[] MulticastBlocks = new Ipv6Block[]
    {
        new Ipv6Block(new byte[]
        {
            0xff, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0
        }, 8)
    };

    static readonly Ipv6Block[] PublicBlocks = PublicBlockValues();

    internal static string Text(
        GenerationContext context,
        FakeRandom random,
        NetworkAddressKind kind)
    {
        byte[] bytes = AddressBytes(context, random, kind);
        string result = Format(bytes);
        Guard.OutputSize(
            result.Length,
            context.CapturedOptions.MaximumOutputSize,
            "ipv6");
        return result;
    }

    internal static IPAddress Address(
        GenerationContext context,
        FakeRandom random,
        NetworkAddressKind kind)
    {
        byte[] bytes = AddressBytes(context, random, kind);
        string text = Format(bytes);
        Guard.OutputSize(
            text.Length,
            context.CapturedOptions.MaximumOutputSize,
            "ipv6Address");
        return new IPAddress(bytes);
    }

    internal static string Canonical(
        string value,
        int maximumOutputSize)
    {
        Guard.NotNullOrWhiteSpace(value, "address");
        if (value.Contains('%') || value.Contains('[') || value.Contains(']'))
        {
            throw new ArgumentException(
                "address must be an IPv6 address without a scope identifier or endpoint brackets.",
                "address");
        }
        IPAddress address = IPAddress.Parse(value);
        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            throw new ArgumentException("address must be an IPv6 address.", "address");
        }
        string result;
        if (address.IsIPv4MappedToIPv6)
        {
            result = address.ToString().ToLowerInvariant();
        }
        else
        {
            result = Format(address.GetAddressBytes());
        }
        Guard.OutputSize(result.Length, maximumOutputSize, "address");
        return result;
    }

    internal static string Mask(
        int prefixLength,
        int maximumOutputSize)
    {
        ValidatePrefix(prefixLength, 0);
        string result = Format(MaskBytes(prefixLength));
        Guard.OutputSize(result.Length, maximumOutputSize, "prefixLength");
        return result;
    }

    internal static string Cidr(
        GenerationContext context,
        FakeRandom random,
        NetworkAddressKind kind,
        int prefixLength)
    {
        Ipv6Block[] blocks = Blocks(context, kind);
        Ipv6Block block = EligibleBlock(random, kind, blocks, prefixLength);
        byte[] candidate = InsideBlock(random, block);
        byte[] network = ApplyPrefix(candidate, prefixLength);
        string result = Format(network) + "/" +
            prefixLength.ToString(CultureInfo.InvariantCulture);
        Guard.OutputSize(
            result.Length,
            context.CapturedOptions.MaximumOutputSize,
            "ipv6Cidr");
        return result;
    }

    internal static Ipv6Range Range(
        GenerationContext context,
        FakeRandom random,
        NetworkAddressKind kind,
        int prefixLength)
    {
        Ipv6Block[] blocks = Blocks(context, kind);
        Ipv6Block block = EligibleBlock(random, kind, blocks, prefixLength);
        byte[] candidate = InsideBlock(random, block);
        byte[] network = ApplyPrefix(candidate, prefixLength);
        byte[] mask = MaskBytes(prefixLength);
        byte[] last = LastBytes(network, mask);
        string networkText = Format(network);
        string lastText = Format(last);
        string maskText = Format(mask);
        string cidr = networkText + "/" +
            prefixLength.ToString(CultureInfo.InvariantCulture);
        int maximumOutputSize = context.CapturedOptions.MaximumOutputSize;
        Guard.OutputSize(networkText.Length, maximumOutputSize, "ipv6Range");
        Guard.OutputSize(lastText.Length, maximumOutputSize, "ipv6Range");
        Guard.OutputSize(maskText.Length, maximumOutputSize, "ipv6Range");
        Guard.OutputSize(cidr.Length, maximumOutputSize, "ipv6Range");
        Ipv6Range result = new Ipv6Range();
        result.Kind = kind;
        result.NetworkAddress = new IPAddress(network);
        result.LastAddress = new IPAddress(last);
        result.Netmask = new IPAddress(mask);
        result.PrefixLength = prefixLength;
        result.AddressCount = System.Numerics.BigInteger.One <<
            (128 - prefixLength);
        result.Cidr = cidr;
        result.NetworkBytes = network;
        result.LastBytes = last;
        return result;
    }

    internal static IPEndPoint Endpoint(
        GenerationContext context,
        FakeRandom random,
        NetworkAddressKind kind,
        int port)
    {
        if (port < 0 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(
                "port",
                port,
                "port must be between 0 and 65535, inclusive.");
        }
        byte[] bytes = AddressBytes(context, random, kind);
        string addressText = Format(bytes);
        string portText = port.ToString(CultureInfo.InvariantCulture);
        Guard.OutputSize(
            addressText.Length + 3 + portText.Length,
            context.CapturedOptions.MaximumOutputSize,
            "ipv6Endpoint");
        return new IPEndPoint(new IPAddress(bytes), port);
    }

    internal static int DefaultPrefix(NetworkAddressKind kind)
    {
        switch (kind)
        {
            case NetworkAddressKind.Documentation:
                return 64;
            case NetworkAddressKind.Private:
                return 64;
            case NetworkAddressKind.Loopback:
                return 128;
            case NetworkAddressKind.LinkLocal:
                return 64;
            case NetworkAddressKind.Multicast:
                return 64;
            case NetworkAddressKind.Public:
                return 64;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported IPv6 address category.");
        }
    }

    internal static string Format(byte[] bytes)
    {
        if (bytes.Length != 16)
        {
            throw new ArgumentException("IPv6 formatting requires exactly 16 bytes.", "bytes");
        }
        int[] groups = new int[8];
        int index = 0;
        while (index < groups.Length)
        {
            groups[index] = bytes[index * 2] << 8 | bytes[index * 2 + 1];
            index = index + 1;
        }
        int bestStart = -1;
        int bestLength = 0;
        int currentStart = -1;
        int currentLength = 0;
        index = 0;
        while (index <= groups.Length)
        {
            if (index < groups.Length && groups[index] == 0)
            {
                if (currentLength == 0)
                {
                    currentStart = index;
                }
                currentLength = currentLength + 1;
            }
            else
            {
                if (currentLength > bestLength && currentLength >= 2)
                {
                    bestStart = currentStart;
                    bestLength = currentLength;
                }
                currentStart = -1;
                currentLength = 0;
            }
            index = index + 1;
        }
        StringBuilder builder = new StringBuilder();
        index = 0;
        while (index < groups.Length)
        {
            if (index == bestStart)
            {
                builder.Append("::");
                index = index + bestLength;
                continue;
            }
            if (builder.Length != 0 && builder[builder.Length - 1] != ':')
            {
                builder.Append(':');
            }
            builder.Append(groups[index].ToString("x", CultureInfo.InvariantCulture));
            index = index + 1;
        }
        return builder.ToString();
    }

    static byte[] AddressBytes(
        GenerationContext context,
        FakeRandom random,
        NetworkAddressKind kind)
    {
        Ipv6Block[] blocks = Blocks(context, kind);
        Ipv6Block block = blocks[random.IntFromMaximumExclusive(blocks.Length)];
        return InsideBlock(random, block);
    }

    static byte[] InsideBlock(FakeRandom random, Ipv6Block block)
    {
        byte[] result = RandomBytes(random);
        int fullBytes = block.PrefixLength / 8;
        int remainingBits = block.PrefixLength % 8;
        int index = 0;
        while (index < fullBytes)
        {
            result[index] = block.Network[index];
            index = index + 1;
        }
        if (remainingBits != 0)
        {
            byte mask = (byte)((255 << (8 - remainingBits)) & byte.MaxValue);
            result[fullBytes] = (byte)(
                block.Network[fullBytes] & mask |
                result[fullBytes] & ~mask);
        }
        return result;
    }

    static byte[] ApplyPrefix(byte[] address, int prefixLength)
    {
        byte[] result = new byte[16];
        Array.Copy(address, result, result.Length);
        int fullBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;
        int index = fullBytes;
        if (remainingBits != 0)
        {
            byte mask = (byte)((255 << (8 - remainingBits)) & byte.MaxValue);
            result[index] = (byte)(result[index] & mask);
            index = index + 1;
        }
        while (index < result.Length)
        {
            result[index] = 0;
            index = index + 1;
        }
        return result;
    }

    static byte[] MaskBytes(int prefixLength)
    {
        byte[] result = new byte[16];
        int fullBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;
        int index = 0;
        while (index < fullBytes)
        {
            result[index] = byte.MaxValue;
            index++;
        }
        if (remainingBits != 0)
        {
            result[index] = (byte)(
                (255 << (8 - remainingBits)) & byte.MaxValue);
        }
        return result;
    }

    static byte[] LastBytes(byte[] network, byte[] mask)
    {
        byte[] result = new byte[16];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = (byte)(
                network[index] | (mask[index] ^ byte.MaxValue));
            index++;
        }
        return result;
    }

    static byte[] RandomBytes(FakeRandom random)
    {
        byte[] result = new byte[16];
        ulong first = random.ULong();
        ulong second = random.ULong();
        int index = 0;
        while (index < 8)
        {
            result[index] = (byte)((first >> (index * 8)) & byte.MaxValue);
            result[index + 8] = (byte)((second >> (index * 8)) & byte.MaxValue);
            index = index + 1;
        }
        return result;
    }

    static Ipv6Block EligibleBlock(
        FakeRandom random,
        NetworkAddressKind kind,
        Ipv6Block[] blocks,
        int prefixLength)
    {
        ValidatePrefix(prefixLength, MinimumPrefix(kind));
        int eligibleCount = 0;
        int index = 0;
        while (index < blocks.Length)
        {
            if (blocks[index].PrefixLength <= prefixLength)
            {
                eligibleCount = eligibleCount + 1;
            }
            index = index + 1;
        }
        if (eligibleCount == 0)
        {
            throw new ArgumentOutOfRangeException(
                "prefixLength",
                prefixLength,
                "prefixLength does not fit any block in the requested category.");
        }
        int selected = random.IntFromMaximumExclusive(eligibleCount);
        index = 0;
        while (index < blocks.Length)
        {
            if (blocks[index].PrefixLength <= prefixLength)
            {
                if (selected == 0)
                {
                    return blocks[index];
                }
                selected = selected - 1;
            }
            index = index + 1;
        }
        throw FakeException.Create("An eligible IPv6 block could not be selected.");
    }

    static Ipv6Block[] Blocks(GenerationContext context, NetworkAddressKind kind)
    {
        switch (kind)
        {
            case NetworkAddressKind.Documentation:
                return DocumentationBlocks(context);
            case NetworkAddressKind.Private:
                return PrivateBlocks;
            case NetworkAddressKind.Loopback:
                return LoopbackBlocks;
            case NetworkAddressKind.LinkLocal:
                return LinkLocalBlocks;
            case NetworkAddressKind.Multicast:
                return MulticastBlocks;
            case NetworkAddressKind.Public:
                RequireNonReservedPublicMode(context);
                return PublicBlocks;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported IPv6 address category.");
        }
    }

    static void RequireNonReservedPublicMode(GenerationContext context)
    {
        if (context.SafetyModeValue == SafetyMode.Reserved)
        {
            throw FakeException.Create(
                "Public IPv6 generation requires SafetyMode.SyntheticPlausible or SafetyMode.UnrestrictedShape.");
        }
    }

    static int MinimumPrefix(NetworkAddressKind kind)
    {
        switch (kind)
        {
            case NetworkAddressKind.Documentation:
                return 32;
            case NetworkAddressKind.Private:
                return 8;
            case NetworkAddressKind.Loopback:
                return 128;
            case NetworkAddressKind.LinkLocal:
                return 10;
            case NetworkAddressKind.Multicast:
                return 8;
            case NetworkAddressKind.Public:
                return 12;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported IPv6 address category.");
        }
    }

    static void ValidatePrefix(int prefixLength, int minimum)
    {
        if (prefixLength < minimum || prefixLength > 128)
        {
            throw new ArgumentOutOfRangeException(
                "prefixLength",
                prefixLength,
                $"prefixLength must be between {minimum} and 128, inclusive.");
        }
    }

    static Ipv6Block[] DocumentationBlocks(GenerationContext context)
    {
        object source = LocaleResolver.Resolve(
            context,
            context.CapturedOptions.Locale,
            "internet.ipv6.documentation-prefix",
            context.LocaleValues());
        string json = JsonSerializer.Serialize(source);
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.String)
            {
                throw FakeException.Create("Resolved IPv6 documentation data must be a CIDR string.");
            }
            string value = JsonText.Required(root, "ipv6DocumentationPrefix");
            string[] pieces = value.Split(new char[] { '/' });
            if (pieces.Length != 2 || pieces[1] != "32")
            {
                throw FakeException.Create("Resolved IPv6 documentation data must use /32 CIDR notation.");
            }
            if (!string.Equals(
                pieces[0],
                "2001:db8::",
                StringComparison.OrdinalIgnoreCase))
            {
                throw FakeException.Create("Resolved IPv6 documentation data must identify the RFC 3849 prefix 2001:db8::/32.");
            }
            byte[] network = new byte[]
            {
                0x20, 0x01, 0x0d, 0xb8, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0
            };
            return new Ipv6Block[] { new Ipv6Block(network, 32) };
        }
    }

    static Ipv6Block[] PublicBlockValues()
    {
        byte[] firstBytes = new byte[] { 0x24, 0x26, 0x28, 0x2a, 0x2c };
        Ipv6Block[] result = new Ipv6Block[firstBytes.Length];
        int index = 0;
        while (index < result.Length)
        {
            byte[] network = new byte[16];
            network[0] = firstBytes[index];
            result[index] = new Ipv6Block(network, 12);
            index = index + 1;
        }
        return result;
    }
}
