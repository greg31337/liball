namespace dns.Tests.Internal.Names;

public class IdnaDnsNameTests
{
    [Test]
    public void UnicodeAbsoluteNameConvertsToAsciiCompatibleLabels()
    {
        DnsTextResult result = IdnaDnsName.ConvertIdnaToName(
            "b\u00fccher.example.",
            null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Name, Is.Not.Null);
            Assert.That(result.Name!.IsAbsolute, Is.True);
            Assert.That(result.Name.Labels, Has.Count.EqualTo(2));
            Assert.That(result.Text, Is.EqualTo("xn--bcher-kva.example."));
            Assert.That(PresentationDnsName.FormatName(result.Name).Text,
                Is.EqualTo("xn--bcher-kva.example."));
        });
    }

    [Test]
    public void UnicodeAndAsciiCompatibleNamesRoundTrip()
    {
        DnsTextResult encoded = IdnaDnsName.ConvertIdnaToName(
            "WwW.b\u00fccher.example.",
            null);
        DnsTextResult decoded = IdnaDnsName.ConvertNameToUnicode(encoded.Name!);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Name, Is.Not.SameAs(encoded.Name));
            Assert.That(decoded.Text, Is.EqualTo("www.b\u00fccher.example."));
            Assert.That(decoded.Name!.IsAbsolute, Is.True);
        });
    }

    [Test]
    public void MultilingualLabelsUseBuiltInIdnaMapping()
    {
        DnsTextResult encoded = IdnaDnsName.ConvertIdnaToName(
            "\u4f8b\u3048.\u30c6\u30b9\u30c8.",
            null);
        DnsTextResult decoded = IdnaDnsName.ConvertNameToUnicode(encoded.Name!);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Text,
                Is.EqualTo("xn--r8jz45g.xn--zckzah."));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Text,
                Is.EqualTo("\u4f8b\u3048.\u30c6\u30b9\u30c8."));
        });
    }

    [Test]
    public void RelativeInputRemainsRelativeWithoutAnOrigin()
    {
        DnsTextResult encoded = IdnaDnsName.ConvertIdnaToName(
            "b\u00fccher",
            null);
        DnsTextResult decoded = IdnaDnsName.ConvertNameToUnicode(encoded.Name!);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Name!.IsAbsolute, Is.False);
            Assert.That(encoded.Text, Is.EqualTo("xn--bcher-kva"));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Text, Is.EqualTo("b\u00fccher"));
            Assert.That(decoded.Name!.IsAbsolute, Is.False);
        });
    }

    [Test]
    public void RelativeInputJoinsAnAbsoluteRawOriginWithoutSharingLabels()
    {
        DnsName origin = Parse("Example.COM.");
        DnsTextResult result = IdnaDnsName.ConvertIdnaToName(
            "b\u00fccher",
            origin);
        origin.Labels[0][0] = 0x78;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Name!.IsAbsolute, Is.True);
            Assert.That(PresentationDnsName.FormatName(result.Name).Text,
                Is.EqualTo("xn--bcher-kva.Example.COM."));
            Assert.That(result.Name.Labels[1], Is.Not.SameAs(origin.Labels[0]));
            Assert.That(result.Name.Labels[1][0], Is.EqualTo(0x45));
        });
    }

    [Test]
    public void RootAndEmptyRelativeNameKeepTheirTerminalState()
    {
        DnsTextResult root = IdnaDnsName.ConvertIdnaToName(".", null);
        DnsTextResult empty = IdnaDnsName.ConvertIdnaToName(string.Empty, null);
        DnsTextResult rootText = IdnaDnsName.ConvertNameToUnicode(root.Name!);
        DnsTextResult emptyText = IdnaDnsName.ConvertNameToUnicode(empty.Name!);

        Assert.Multiple(() =>
        {
            Assert.That(root.Success, Is.True);
            Assert.That(root.Name!.IsAbsolute, Is.True);
            Assert.That(root.Name.Labels, Is.Empty);
            Assert.That(rootText.Text, Is.EqualTo("."));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Name!.IsAbsolute, Is.False);
            Assert.That(empty.Name.Labels, Is.Empty);
            Assert.That(emptyText.Text, Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void UnicodeLabelSeparatorsAreHandledByIdnaMapping()
    {
        DnsTextResult result = IdnaDnsName.ConvertIdnaToName(
            "b\u00fccher\u3002example\uff0e",
            null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo("xn--bcher-kva.example."));
            Assert.That(result.Name!.IsAbsolute, Is.True);
        });
    }

    [Test]
    public void OriginValidationAndJoinedLengthFailuresReturnDiagnostics()
    {
        DnsName relativeOrigin = Parse("example");
        DnsTextResult invalidOrigin = IdnaDnsName.ConvertIdnaToName(
            "host",
            relativeOrigin);
        DnsName maximumOrigin = RawDnsName.CreateAbsoluteName(new List<byte[]>
        {
            new byte[63],
            new byte[63],
            new byte[63],
            new byte[61]
        }).Name!;
        DnsTextResult excessive = IdnaDnsName.ConvertIdnaToName(
            "a",
            maximumOrigin);

        Assert.Multiple(() =>
        {
            Assert.That(invalidOrigin.Success, Is.False);
            Assert.That(invalidOrigin.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidText));
            Assert.That(invalidOrigin.Diagnostics[0].Field, Is.EqualTo("origin"));
            Assert.That(excessive.Success, Is.False);
            Assert.That(excessive.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.NameTooLong));
        });
    }

    [Test]
    public void RawBinaryLabelsAreNotLossilyConvertedToUnicode()
    {
        DnsName binary = new DnsName();
        binary.IsAbsolute = true;
        binary.Labels.Add([0x78, 0xff]);
        DnsTextResult highOctet = IdnaDnsName.ConvertNameToUnicode(binary);
        DnsTextResult nonIdnaAscii = IdnaDnsName.ConvertNameToUnicode(
            Parse("_sip.example."));

        Assert.Multiple(() =>
        {
            Assert.That(highOctet.Success, Is.False);
            Assert.That(highOctet.Text, Is.EqualTo(string.Empty));
            Assert.That(highOctet.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
            Assert.That(binary.Labels[0], Is.EqualTo(new byte[] { 0x78, 0xff }));
            Assert.That(nonIdnaAscii.Success, Is.False);
            Assert.That(nonIdnaAscii.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
        });
    }

    [Test]
    public void BuiltInIdnaValidationRejectsInvalidUnicodeAndAceLabels()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => IdnaDnsName.ConvertIdnaToName("_sip.example", null),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => IdnaDnsName.ConvertNameToUnicode(Parse("xn--invalid-.")),
                Throws.TypeOf<ArgumentException>());
        });
    }

    private static DnsName Parse(string text)
    {
        return PresentationDnsName.ParseName(text, null).Name!;
    }
}
