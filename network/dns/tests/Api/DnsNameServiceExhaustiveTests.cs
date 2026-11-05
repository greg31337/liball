namespace dns.Tests.Api;

public class DnsNameServiceExhaustiveTests
{
    [Test]
    public void RootRelativeAndAbsoluteNamesRetainTheirTerminalSemantics()
    {
        IDnsNameService service = new DnsNameService();
        DnsName root = service.ParseName(".", null);
        DnsName emptyRelative = service.ParseName(string.Empty, null);
        DnsName relative = service.ParseName("host", null);
        DnsName origin = service.ParseName("Example.", null);
        DnsName joined = service.ParseName("host", origin);
        DnsName absolute = service.ParseName("host.example.", origin);

        Assert.Multiple(() =>
        {
            Assert.That(root.IsAbsolute, Is.True);
            Assert.That(root.Labels, Is.Empty);
            Assert.That(service.FormatName(root, new DnsTextOptions()),
                Is.EqualTo("."));
            Assert.That(emptyRelative.IsAbsolute, Is.False);
            Assert.That(emptyRelative.Labels, Is.Empty);
            Assert.That(service.FormatName(
                emptyRelative, new DnsTextOptions()), Is.Empty);
            Assert.That(relative.IsAbsolute, Is.False);
            Assert.That(service.FormatName(relative, new DnsTextOptions()),
                Is.EqualTo("host"));
            Assert.That(joined.IsAbsolute, Is.True);
            Assert.That(service.FormatName(joined, new DnsTextOptions()),
                Is.EqualTo("host.Example."));
            Assert.That(service.NamesEqual(joined, absolute), Is.True);
        });
    }

    [Test]
    public void PublicPresentationRoundTripsEveryPossibleLabelOctet()
    {
        IDnsNameService service = new DnsNameService();
        int value = 0;
        while (value <= byte.MaxValue)
        {
            DnsName source = Name(true, [(byte)value]);

            string text = service.FormatName(source, new DnsTextOptions());
            DnsName parsed = service.ParseName(text, null);

            Assert.That(parsed.IsAbsolute, Is.True, $"absolute {value}");
            Assert.That(parsed.Labels, Has.Count.EqualTo(1), $"labels {value}");
            Assert.That(parsed.Labels[0],
                Is.EqualTo(new byte[] { (byte)value }),
                $"octet {value}");
            value = value + 1;
        }
    }

    [Test]
    public void CanonicalComparisonAndEqualityCoverEveryOctetPair()
    {
        IDnsNameService service = new DnsNameService();
        DnsName[] names = new DnsName[256];
        int value = 0;
        while (value < names.Length)
        {
            names[value] = Name(true, [(byte)value]);
            value = value + 1;
        }

        int left = 0;
        while (left < names.Length)
        {
            int right = 0;
            while (right < names.Length)
            {
                int expected = FoldAscii(left).CompareTo(FoldAscii(right));
                int actual = service.CompareNames(names[left], names[right]);
                bool equal = service.NamesEqual(names[left], names[right]);
                if (Math.Sign(actual) != Math.Sign(expected) ||
                    equal != (expected == 0))
                {
                    Assert.Fail(
                        $"Comparison mismatch for octets {left} and {right}: " +
                        $"expected {Math.Sign(expected)}, actual {Math.Sign(actual)}, " +
                        $"equal {equal}.");
                }

                right = right + 1;
            }

            left = left + 1;
        }
    }

    [Test]
    public void SuffixOperationsCoverEveryDepthAndTerminalMismatch()
    {
        IDnsNameService service = new DnsNameService();
        DnsName name = service.ParseName("a.B.c.", null);
        DnsName root = service.ParseName(".", null);
        DnsName c = service.ParseName("C.", null);
        DnsName bc = service.ParseName("b.C.", null);
        DnsName whole = service.ParseName("A.b.c.", null);
        DnsName unrelated = service.ParseName("a.c.", null);
        DnsName relative = service.ParseName("b.c", null);

        Assert.Multiple(() =>
        {
            Assert.That(service.HasNameSuffix(name, root), Is.True);
            Assert.That(service.HasNameSuffix(name, c), Is.True);
            Assert.That(service.HasNameSuffix(name, bc), Is.True);
            Assert.That(service.HasNameSuffix(name, whole), Is.True);
            Assert.That(service.HasNameSuffix(name, unrelated), Is.False);
            Assert.That(service.HasNameSuffix(name, relative), Is.False);
            Assert.That(service.IsAncestorName(root, name), Is.True);
            Assert.That(service.IsAncestorName(c, name), Is.True);
            Assert.That(service.IsAncestorName(bc, name), Is.True);
            Assert.That(service.IsAncestorName(whole, name), Is.False);
            Assert.That(service.IsAncestorName(relative, name), Is.False);
            Assert.That(service.CountNameLabels(name), Is.EqualTo(3));
        });
    }

    [Test]
    public void ExactLabelAndExpandedNameLimitsAreAcceptedBeforeOverflow()
    {
        IDnsNameService service = new DnsNameService();
        DnsName maximum = Name(
            true,
            new byte[63],
            new byte[63],
            new byte[63],
            new byte[61]);
        DnsName expandedOverflow = Name(
            true,
            new byte[63],
            new byte[63],
            new byte[63],
            new byte[62]);
        DnsName labelMaximum = Name(false, new byte[63]);
        DnsName labelOverflow = Name(false, new byte[64]);

        DnsName validatedMaximum = service.ValidateName(maximum);
        DnsName validatedLabel = service.ValidateName(labelMaximum);
        DnsEncodeResult encoded = service.EncodeName(
            maximum, new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(validatedMaximum, Is.Not.SameAs(maximum));
            Assert.That(validatedMaximum.Labels, Has.Count.EqualTo(4));
            Assert.That(validatedLabel.Labels[0], Has.Length.EqualTo(63));
            Assert.That(encoded.Bytes, Has.Length.EqualTo(255));
            Assert.That(
                () => service.ValidateName(expandedOverflow),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(
                () => service.ValidateName(labelOverflow),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(
                () => service.ParseName(new string('a', 64), null),
                Throws.TypeOf<DnsFormatException>());
        });
    }

    [Test]
    public void IdnaAndReverseMapsRoundTripBoundaryInputsThroughPublicApi()
    {
        IDnsNameService service = new DnsNameService();
        DnsName idna = service.ConvertIdnaToName("bücher.例え.", null);
        byte[] ipv4Address = [0, 9, 10, 255];
        byte[] ipv6Address =
        [
            0x01, 0x23, 0x45, 0x67,
            0x89, 0xab, 0xcd, 0xef,
            0xfe, 0xdc, 0xba, 0x98,
            0x76, 0x54, 0x32, 0x10
        ];
        DnsName ipv4 = service.BuildIpv4ReverseName(ipv4Address);
        DnsName ipv6 = service.BuildIpv6ReverseName(ipv6Address);

        Assert.Multiple(() =>
        {
            Assert.That(service.FormatName(idna, new DnsTextOptions()),
                Is.EqualTo("xn--bcher-kva.xn--r8jz45g."));
            Assert.That(service.ConvertNameToUnicode(idna),
                Is.EqualTo("bücher.例え."));
            Assert.That(service.FormatName(ipv4, new DnsTextOptions()),
                Is.EqualTo("255.10.9.0.in-addr.arpa."));
            Assert.That(service.ParseIpv4ReverseAddress(ipv4),
                Is.EqualTo(ipv4Address));
            Assert.That(service.ParseIpv6ReverseAddress(ipv6),
                Is.EqualTo(ipv6Address));
        });
    }

    [Test]
    public void InvalidMutableLabelsFailThroughEveryPublicOperationFamily()
    {
        IDnsNameService service = new DnsNameService();
        DnsName invalid = Name(true, new byte[64]);
        DnsName valid = service.ParseName("example.", null);
        DnsTextOptions textOptions = new DnsTextOptions();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.ValidateName(invalid),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(() => service.FormatName(invalid, textOptions),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(() => service.NamesEqual(invalid, valid),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(() => service.CompareNames(valid, invalid),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(() => service.HasNameSuffix(invalid, valid),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(() => service.IsAncestorName(valid, invalid),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(() => service.CountNameLabels(invalid),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(() => service.GetParentName(invalid),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(() => service.ConcatenateNames(invalid, valid),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(() => service.RelativizeName(invalid, valid),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(() => service.FindCommonAncestor(valid, invalid),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(() => service.CreateCanonicalName(invalid),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(() => service.ConvertNameToUnicode(invalid),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(() => service.ParseIpv4ReverseAddress(invalid),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(() => service.ParseIpv6ReverseAddress(invalid),
                Throws.TypeOf<DnsFormatException>());
        });
    }

    static int FoldAscii(int value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return value + ('a' - 'A');
        }

        return value;
    }

    static DnsName Name(bool absolute, params byte[][] labels)
    {
        DnsName name = new DnsName();
        name.IsAbsolute = absolute;
        int index = 0;
        while (index < labels.Length)
        {
            name.Labels.Add(labels[index]);
            index = index + 1;
        }

        return name;
    }
}
