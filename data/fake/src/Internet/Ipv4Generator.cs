using System;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace fake;

internal class Ipv4Block
{
    internal uint Network;
    internal int PrefixLength;

    internal Ipv4Block(uint network, int prefixLength)
    {
        Network = network;
        PrefixLength = prefixLength;
    }
}

internal static class Ipv4Generator
{
    static readonly Ipv4Block[] PrivateBlocks = new Ipv4Block[]
    {
        new Ipv4Block(0x0a000000U, 8),
        new Ipv4Block(0xac100000U, 12),
        new Ipv4Block(0xc0a80000U, 16)
    };

    static readonly Ipv4Block[] LoopbackBlocks = new Ipv4Block[]
    {
        new Ipv4Block(0x7f000000U, 8)
    };

    static readonly Ipv4Block[] LinkLocalBlocks = new Ipv4Block[]
    {
        new Ipv4Block(0xa9fe0000U, 16)
    };

    static readonly Ipv4Block[] MulticastBlocks = new Ipv4Block[]
    {
        new Ipv4Block(0xe0000000U, 4)
    };

    static readonly Ipv4Block[] PublicBlocks = PublicBlockValues();

    internal static string Text(
        GenerationContext context,
        FakeRandom random,
        NetworkAddressKind kind)
    {
        uint number = AddressNumber(context, random, kind);
        string result = Format(number);
        Guard.OutputSize(
            result.Length,
            context.CapturedOptions.MaximumOutputSize,
            "ipv4");
        return result;
    }

    internal static IPAddress Address(
        GenerationContext context,
        FakeRandom random,
        NetworkAddressKind kind)
    {
        uint number = AddressNumber(context, random, kind);
        string text = Format(number);
        Guard.OutputSize(
            text.Length,
            context.CapturedOptions.MaximumOutputSize,
            "ipv4Address");
        return Address(number);
    }

    internal static string Mask(int prefixLength, int maximumOutputSize)
    {
        ValidatePrefix(prefixLength, 0);
        string result = Format(MaskNumber(prefixLength));
        Guard.OutputSize(result.Length, maximumOutputSize, "prefixLength");
        return result;
    }

    internal static string Cidr(
        GenerationContext context,
        FakeRandom random,
        NetworkAddressKind kind,
        int prefixLength)
    {
        Ipv4Block[] blocks = Blocks(context, kind);
        Ipv4Block block = EligibleBlock(random, kind, blocks, prefixLength);
        uint candidate = block.Network |
            (random.UInt() & ~MaskNumber(block.PrefixLength));
        uint network = candidate & MaskNumber(prefixLength);
        string result = Format(network) + "/" +
            prefixLength.ToString(CultureInfo.InvariantCulture);
        Guard.OutputSize(
            result.Length,
            context.CapturedOptions.MaximumOutputSize,
            "ipv4Cidr");
        return result;
    }

    internal static Ipv4Range Range(
        GenerationContext context,
        FakeRandom random,
        NetworkAddressKind kind,
        int prefixLength)
    {
        Ipv4Block[] blocks = Blocks(context, kind);
        Ipv4Block block = EligibleBlock(random, kind, blocks, prefixLength);
        uint mask = MaskNumber(prefixLength);
        uint candidate = block.Network | (random.UInt() & ~MaskNumber(block.PrefixLength));
        uint network = candidate & mask;
        uint last = network | ~mask;
        string networkText = Format(network);
        string lastText = Format(last);
        string maskText = Format(mask);
        string cidr = networkText + "/" + prefixLength.ToString(CultureInfo.InvariantCulture);
        int maximumOutputSize = context.CapturedOptions.MaximumOutputSize;
        Guard.OutputSize(networkText.Length, maximumOutputSize, "ipv4Range");
        Guard.OutputSize(lastText.Length, maximumOutputSize, "ipv4Range");
        Guard.OutputSize(maskText.Length, maximumOutputSize, "ipv4Range");
        Guard.OutputSize(cidr.Length, maximumOutputSize, "ipv4Range");
        Ipv4Range result = new Ipv4Range();
        result.Kind = kind;
        result.NetworkAddress = Address(network);
        result.LastAddress = Address(last);
        result.Netmask = Address(mask);
        result.PrefixLength = prefixLength;
        result.AddressCount = 1UL << (32 - prefixLength);
        result.Cidr = cidr;
        result.NetworkNumber = network;
        result.LastNumber = last;
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
        uint number = AddressNumber(context, random, kind);
        string addressText = Format(number);
        string portText = port.ToString(CultureInfo.InvariantCulture);
        Guard.OutputSize(
            addressText.Length + 1 + portText.Length,
            context.CapturedOptions.MaximumOutputSize,
            "ipv4Endpoint");
        return new IPEndPoint(Address(number), port);
    }

    internal static uint Number(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        return ((uint)bytes[0] << 24) |
            ((uint)bytes[1] << 16) |
            ((uint)bytes[2] << 8) |
            bytes[3];
    }

    static uint AddressNumber(
        GenerationContext context,
        FakeRandom random,
        NetworkAddressKind kind)
    {
        Ipv4Block[] blocks = Blocks(context, kind);
        Ipv4Block block = blocks[random.IntFromMaximumExclusive(blocks.Length)];
        return block.Network | (random.UInt() & ~MaskNumber(block.PrefixLength));
    }

    static Ipv4Block EligibleBlock(
        FakeRandom random,
        NetworkAddressKind kind,
        Ipv4Block[] blocks,
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
        throw FakeException.Create("An eligible IPv4 block could not be selected.");
    }

    static Ipv4Block[] Blocks(GenerationContext context, NetworkAddressKind kind)
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
                    "kind is not a supported IPv4 address category.");
        }
    }

    static void RequireNonReservedPublicMode(GenerationContext context)
    {
        if (context.SafetyModeValue == SafetyMode.Reserved)
        {
            throw FakeException.Create(
                "Public IPv4 generation requires SafetyMode.SyntheticPlausible or SafetyMode.UnrestrictedShape.");
        }
    }

    static int MinimumPrefix(NetworkAddressKind kind)
    {
        switch (kind)
        {
            case NetworkAddressKind.Documentation:
                return 24;
            case NetworkAddressKind.Private:
                return 8;
            case NetworkAddressKind.Loopback:
                return 8;
            case NetworkAddressKind.LinkLocal:
                return 16;
            case NetworkAddressKind.Multicast:
                return 4;
            case NetworkAddressKind.Public:
                return 8;
            default:
                throw new ArgumentOutOfRangeException(
                    "kind",
                    kind,
                    "kind is not a supported IPv4 address category.");
        }
    }

    static void ValidatePrefix(int prefixLength, int minimum)
    {
        if (prefixLength < minimum || prefixLength > 32)
        {
            throw new ArgumentOutOfRangeException(
                "prefixLength",
                prefixLength,
                $"prefixLength must be between {minimum} and 32, inclusive.");
        }
    }

    static uint MaskNumber(int prefixLength)
    {
        if (prefixLength == 0)
        {
            return 0U;
        }
        return uint.MaxValue << (32 - prefixLength);
    }

    static string Format(uint number)
    {
        return ((number >> 24) & 255U).ToString(CultureInfo.InvariantCulture) + "." +
            ((number >> 16) & 255U).ToString(CultureInfo.InvariantCulture) + "." +
            ((number >> 8) & 255U).ToString(CultureInfo.InvariantCulture) + "." +
            (number & 255U).ToString(CultureInfo.InvariantCulture);
    }

    static IPAddress Address(uint number)
    {
        byte[] bytes = new byte[4];
        bytes[0] = (byte)((number >> 24) & byte.MaxValue);
        bytes[1] = (byte)((number >> 16) & byte.MaxValue);
        bytes[2] = (byte)((number >> 8) & byte.MaxValue);
        bytes[3] = (byte)(number & byte.MaxValue);
        return new IPAddress(bytes);
    }

    static Ipv4Block[] DocumentationBlocks(GenerationContext context)
    {
        object source = LocaleResolver.Resolve(
            context,
            context.CapturedOptions.Locale,
            "internet.ipv4.documentation-prefixes",
            context.LocaleValues());
        string json = JsonSerializer.Serialize(source);
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            {
                throw FakeException.Create("Resolved IPv4 documentation data must be a nonempty array.");
            }
            Guard.Count(
                root.GetArrayLength(),
                context.CapturedOptions.MaximumCount,
                "ipv4DocumentationPrefixes");
            Ipv4Block[] result = new Ipv4Block[root.GetArrayLength()];
            int index = 0;
            while (index < result.Length)
            {
                if (root[index].ValueKind != JsonValueKind.String)
                {
                    throw FakeException.Create("Resolved IPv4 documentation data must contain only CIDR strings.");
                }
                result[index] = ParseDocumentationBlock(
                    JsonText.Required(root[index], "ipv4DocumentationPrefixes"));
                index = index + 1;
            }
            return result;
        }
    }

    static Ipv4Block ParseDocumentationBlock(string value)
    {
        string[] pieces = value.Split(new char[] { '/' });
        if (pieces.Length != 2 || pieces[1] != "24")
        {
            throw FakeException.Create("Resolved IPv4 documentation prefixes must use /24 CIDR notation.");
        }
        uint number = ParseAddress(pieces[0]);
        if ((number & MaskNumber(24)) != number)
        {
            throw FakeException.Create("Resolved IPv4 documentation prefixes must use aligned network addresses.");
        }
        if (number != 0xc0000200U &&
            number != 0xc6336400U &&
            number != 0xcb007100U)
        {
            throw FakeException.Create(
                "Resolved IPv4 documentation prefixes must identify an RFC 5737 TEST-NET block.");
        }
        return new Ipv4Block(number, 24);
    }

    static uint ParseAddress(string value)
    {
        string[] pieces = value.Split(new char[] { '.' });
        if (pieces.Length != 4)
        {
            throw FakeException.Create("Resolved IPv4 documentation prefixes must contain dotted-decimal addresses.");
        }
        uint result = 0U;
        int index = 0;
        while (index < pieces.Length)
        {
            if (pieces[index].Length == 0 || pieces[index].Length > 3)
            {
                throw FakeException.Create("Resolved IPv4 documentation prefixes contain an invalid octet.");
            }
            int digitIndex = 0;
            uint octet = 0U;
            while (digitIndex < pieces[index].Length)
            {
                char character = pieces[index][digitIndex];
                if (character < '0' || character > '9')
                {
                    throw FakeException.Create("Resolved IPv4 documentation prefixes contain an invalid octet.");
                }
                octet = octet * 10U + (uint)(character - '0');
                digitIndex = digitIndex + 1;
            }
            if (octet > 255U)
            {
                throw FakeException.Create("Resolved IPv4 documentation prefixes contain an invalid octet.");
            }
            result = (result << 8) | octet;
            index = index + 1;
        }
        return result;
    }

    static Ipv4Block[] PublicBlockValues()
    {
        byte[] firstOctets = new byte[]
        {
            1, 8, 9, 11, 23, 31, 45, 46, 50, 52, 54, 64,
            66, 67, 68, 69, 70, 72, 74, 75, 76, 77, 78, 80,
            81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92,
            93, 94, 95
        };
        Ipv4Block[] result = new Ipv4Block[firstOctets.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new Ipv4Block((uint)firstOctets[index] << 24, 8);
            index = index + 1;
        }
        return result;
    }
}
