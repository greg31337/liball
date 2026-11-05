namespace dns.Tests.Internal.Names;

public class ReverseDnsNameTests
{
    [Test]
    public void Ipv4BuilderEmitsReversedCanonicalDecimalLabels()
    {
        byte[] address = [192, 0, 2, 255];
        ReverseDnsNameResult result = ReverseDnsName.BuildIpv4ReverseName(address);
        address[0] = 1;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Name, Is.Not.Null);
            Assert.That(result.Name!.IsAbsolute, Is.True);
            Assert.That(result.Name.Labels, Has.Count.EqualTo(6));
            Assert.That(Format(result.Name),
                Is.EqualTo("255.2.0.192.in-addr.arpa."));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Ipv4AddressesRoundTripAtDecimalBoundaries()
    {
        byte[] address = [0, 9, 10, 255];
        ReverseDnsNameResult built = ReverseDnsName.BuildIpv4ReverseName(address);
        ReverseDnsNameResult parsed = ReverseDnsName.ParseIpv4ReverseAddress(
            built.Name!);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(Format(built.Name!),
                Is.EqualTo("255.10.9.0.in-addr.arpa."));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Address, Is.EqualTo(address));
            Assert.That(parsed.Address, Is.Not.SameAs(address));
        });
    }

    [Test]
    public void Ipv4ParserAcceptsCaseInsensitiveReverseSuffixes()
    {
        DnsName name = Parse("4.3.2.1.IN-ADDR.ARPA.");
        ReverseDnsNameResult result = ReverseDnsName.ParseIpv4ReverseAddress(name);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Address, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(Format(name), Is.EqualTo("4.3.2.1.IN-ADDR.ARPA."));
        });
    }

    [Test]
    public void Ipv4ParserRejectsPartialRelativeAndWrongSuffixNames()
    {
        ReverseDnsNameResult partial = ReverseDnsName.ParseIpv4ReverseAddress(
            Parse("3.2.1.in-addr.arpa."));
        ReverseDnsNameResult relative = ReverseDnsName.ParseIpv4ReverseAddress(
            Parse("4.3.2.1.in-addr.arpa"));
        ReverseDnsNameResult wrongSuffix = ReverseDnsName.ParseIpv4ReverseAddress(
            Parse("4.3.2.1.example.arpa."));

        Assert.Multiple(() =>
        {
            AssertInvalidName(partial);
            AssertInvalidName(relative);
            AssertInvalidName(wrongSuffix);
        });
    }

    [Test]
    public void Ipv4ParserRejectsNonCanonicalDecimalOctets()
    {
        ReverseDnsNameResult excessive = ReverseDnsName.ParseIpv4ReverseAddress(
            Parse("256.3.2.1.in-addr.arpa."));
        ReverseDnsNameResult leadingZero = ReverseDnsName.ParseIpv4ReverseAddress(
            Parse("04.3.2.1.in-addr.arpa."));
        ReverseDnsNameResult nonDecimal = ReverseDnsName.ParseIpv4ReverseAddress(
            Parse("x.3.2.1.in-addr.arpa."));

        Assert.Multiple(() =>
        {
            Assert.That(excessive.Success, Is.False);
            Assert.That(excessive.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(excessive.Diagnostics[0].Field, Is.EqualTo("labels[0]"));
            AssertInvalidLabel(leadingZero, "labels[0]");
            AssertInvalidLabel(nonDecimal, "labels[0]");
        });
    }

    [Test]
    public void Ipv6BuilderEmitsThirtyTwoReversedLowercaseNibbles()
    {
        byte[] address =
        [
            0x20, 0x01, 0x0d, 0xb8,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 1
        ];
        ReverseDnsNameResult result = ReverseDnsName.BuildIpv6ReverseName(address);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Name!.IsAbsolute, Is.True);
            Assert.That(result.Name.Labels, Has.Count.EqualTo(34));
            Assert.That(Format(result.Name), Is.EqualTo(
                "1.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0.0." +
                "8.b.d.0.1.0.0.2.ip6.arpa."));
        });
    }

    [Test]
    public void Ipv6AddressesRoundTripEveryNibbleValue()
    {
        byte[] address =
        [
            0x01, 0x23, 0x45, 0x67,
            0x89, 0xab, 0xcd, 0xef,
            0xfe, 0xdc, 0xba, 0x98,
            0x76, 0x54, 0x32, 0x10
        ];
        ReverseDnsNameResult built = ReverseDnsName.BuildIpv6ReverseName(address);
        ReverseDnsNameResult parsed = ReverseDnsName.ParseIpv6ReverseAddress(
            built.Name!);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Address, Is.EqualTo(address));
            Assert.That(parsed.Address, Is.Not.SameAs(address));
        });
    }

    [Test]
    public void Ipv6ParserAcceptsUppercaseNibblesAndSuffixes()
    {
        ReverseDnsNameResult built = ReverseDnsName.BuildIpv6ReverseName(
            new byte[16]);
        DnsName name = built.Name!;
        name.Labels[0][0] = 0x46;
        name.Labels[32] = [0x49, 0x50, 0x36];
        name.Labels[33] = [0x41, 0x52, 0x50, 0x41];

        ReverseDnsNameResult parsed = ReverseDnsName.ParseIpv6ReverseAddress(name);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Address[15], Is.EqualTo(0x0f));
            Assert.That(parsed.Address[0], Is.Zero);
        });
    }

    [Test]
    public void Ipv6ParserRejectsIncompleteRelativeAndWrongSuffixNames()
    {
        ReverseDnsNameResult built = ReverseDnsName.BuildIpv6ReverseName(
            new byte[16]);
        DnsName incomplete = built.Name!;
        incomplete.Labels.RemoveAt(0);
        DnsName relative = ReverseDnsName.BuildIpv6ReverseName(new byte[16]).Name!;
        relative.IsAbsolute = false;
        DnsName wrongSuffix = ReverseDnsName.BuildIpv6ReverseName(new byte[16]).Name!;
        wrongSuffix.Labels[32] = [0x69, 0x70, 0x34];

        Assert.Multiple(() =>
        {
            AssertInvalidName(ReverseDnsName.ParseIpv6ReverseAddress(incomplete));
            AssertInvalidName(ReverseDnsName.ParseIpv6ReverseAddress(relative));
            AssertInvalidName(ReverseDnsName.ParseIpv6ReverseAddress(wrongSuffix));
        });
    }

    [Test]
    public void Ipv6ParserRejectsNonHexadecimalAndMultiOctetNibbles()
    {
        DnsName nonHexadecimal = ReverseDnsName.BuildIpv6ReverseName(
            new byte[16]).Name!;
        nonHexadecimal.Labels[0][0] = 0x67;
        DnsName multipleOctets = ReverseDnsName.BuildIpv6ReverseName(
            new byte[16]).Name!;
        multipleOctets.Labels[0] = [0x30, 0x30];

        ReverseDnsNameResult nonHexadecimalResult =
            ReverseDnsName.ParseIpv6ReverseAddress(nonHexadecimal);
        ReverseDnsNameResult multipleOctetsResult =
            ReverseDnsName.ParseIpv6ReverseAddress(multipleOctets);

        Assert.Multiple(() =>
        {
            AssertInvalidLabel(nonHexadecimalResult, "labels[0]");
            AssertInvalidLabel(multipleOctetsResult, "labels[0]");
        });
    }

    [Test]
    public void BuildersRejectNullAndIncorrectAddressLengths()
    {
        ReverseDnsNameResult nullIpv4 = ReverseDnsName.BuildIpv4ReverseName(null!);
        ReverseDnsNameResult shortIpv4 = ReverseDnsName.BuildIpv4ReverseName(
            new byte[3]);
        ReverseDnsNameResult nullIpv6 = ReverseDnsName.BuildIpv6ReverseName(null!);
        ReverseDnsNameResult shortIpv6 = ReverseDnsName.BuildIpv6ReverseName(
            new byte[15]);

        Assert.Multiple(() =>
        {
            AssertAddressFailure(nullIpv4);
            AssertAddressFailure(shortIpv4);
            AssertAddressFailure(nullIpv6);
            AssertAddressFailure(shortIpv6);
        });
    }

    [Test]
    public void ParsersPropagateRawNameValidationDiagnostics()
    {
        DnsName invalid = new DnsName();
        invalid.Labels.Add(new byte[64]);

        ReverseDnsNameResult ipv4 = ReverseDnsName.ParseIpv4ReverseAddress(invalid);
        ReverseDnsNameResult ipv6 = ReverseDnsName.ParseIpv6ReverseAddress(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(ipv4.Success, Is.False);
            Assert.That(ipv4.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
            Assert.That(ipv6.Success, Is.False);
            Assert.That(ipv6.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
        });
    }

    private static DnsName Parse(string text)
    {
        return PresentationDnsName.ParseName(text, null).Name!;
    }

    private static string Format(DnsName name)
    {
        return PresentationDnsName.FormatName(name).Text!;
    }

    private static void AssertInvalidName(ReverseDnsNameResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Address, Is.Empty);
        Assert.That(result.Diagnostics[0].Code,
            Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
        Assert.That(result.Diagnostics[0].Field, Is.EqualTo("name"));
    }

    private static void AssertInvalidLabel(
        ReverseDnsNameResult result,
        string field)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Address, Is.Empty);
        Assert.That(result.Diagnostics[0].Code,
            Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
        Assert.That(result.Diagnostics[0].Field, Is.EqualTo(field));
    }

    private static void AssertAddressFailure(ReverseDnsNameResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Name, Is.Null);
        Assert.That(result.Diagnostics[0].Code,
            Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
        Assert.That(result.Diagnostics[0].Field, Is.EqualTo("address"));
    }
}
