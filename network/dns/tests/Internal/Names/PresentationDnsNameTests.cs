namespace dns.Tests.Internal.Names;

public class PresentationDnsNameTests
{
    [Test]
    public void RootParsesAndFormatsAsSingleDot()
    {
        DnsTextResult parsed = PresentationDnsName.ParseName(".", null);
        DnsTextResult formatted = PresentationDnsName.FormatName(parsed.Name!);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Name, Is.Not.Null);
            Assert.That(parsed.Name!.IsAbsolute, Is.True);
            Assert.That(parsed.Name.Labels, Is.Empty);
            Assert.That(parsed.Text, Is.EqualTo("."));
            Assert.That(formatted.Success, Is.True);
            Assert.That(formatted.Text, Is.EqualTo("."));
        });
    }

    [Test]
    public void AbsoluteAndRelativeNamesPreserveAsciiCase()
    {
        DnsTextResult absolute = PresentationDnsName.ParseName("WwW.ExAmPlE.", null);
        DnsTextResult relative = PresentationDnsName.ParseName("WwW.ExAmPlE", null);

        Assert.Multiple(() =>
        {
            Assert.That(absolute.Success, Is.True);
            Assert.That(absolute.Name!.IsAbsolute, Is.True);
            Assert.That(absolute.Name.Labels[0],
                Is.EqualTo(new byte[] { 0x57, 0x77, 0x57 }));
            Assert.That(absolute.Name.Labels[1],
                Is.EqualTo(new byte[] { 0x45, 0x78, 0x41, 0x6d, 0x50, 0x6c, 0x45 }));
            Assert.That(relative.Success, Is.True);
            Assert.That(relative.Name!.IsAbsolute, Is.False);
            Assert.That(relative.Name.Labels[0],
                Is.EqualTo(new byte[] { 0x57, 0x77, 0x57 }));
            Assert.That(PresentationDnsName.FormatName(absolute.Name).Text,
                Is.EqualTo("WwW.ExAmPlE."));
            Assert.That(PresentationDnsName.FormatName(relative.Name).Text,
                Is.EqualTo("WwW.ExAmPlE"));
        });
    }

    [Test]
    public void RelativeNamesJoinAnAbsoluteOriginWithoutSharingLabels()
    {
        DnsName origin = new DnsName();
        origin.IsAbsolute = true;
        origin.Labels.Add([0x45, 0x78, 0x41, 0x6d, 0x50, 0x6c, 0x45]);
        origin.Labels.Add([0x43, 0x4f, 0x4d]);

        DnsTextResult joined = PresentationDnsName.ParseName("WwW", origin);
        DnsTextResult empty = PresentationDnsName.ParseName(string.Empty, origin);
        origin.Labels[0][0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(joined.Success, Is.True);
            Assert.That(joined.Name!.IsAbsolute, Is.True);
            Assert.That(joined.Name.Labels, Has.Count.EqualTo(3));
            Assert.That(joined.Name.Labels[0],
                Is.EqualTo(new byte[] { 0x57, 0x77, 0x57 }));
            Assert.That(joined.Name.Labels[1],
                Is.EqualTo(new byte[] { 0x45, 0x78, 0x41, 0x6d, 0x50, 0x6c, 0x45 }));
            Assert.That(joined.Name.Labels[1], Is.Not.SameAs(origin.Labels[0]));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Name!.IsAbsolute, Is.True);
            Assert.That(empty.Name.Labels, Has.Count.EqualTo(2));
            Assert.That(empty.Name.Labels[0][0], Is.EqualTo(0x45));
        });
    }

    [Test]
    public void AbsoluteInputDoesNotDependOnTheSuppliedOrigin()
    {
        DnsName relativeOrigin = new DnsName();
        relativeOrigin.Labels.Add([1]);

        DnsTextResult result = PresentationDnsName.ParseName("host.example.", relativeOrigin);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Name!.IsAbsolute, Is.True);
            Assert.That(result.Name.Labels, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void RelativeInputRejectsARelativeOrigin()
    {
        DnsName origin = new DnsName();
        origin.Labels.Add([0x78]);

        DnsTextResult result = PresentationDnsName.ParseName("host", origin);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Name, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidText));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("origin"));
        });
    }

    [Test]
    public void SimpleEscapesPreserveDnsAndZoneMetacharacters()
    {
        string text = "a\\.b.c\\\\d.\\@\\$\\(\\)\\;\\\".";

        DnsTextResult parsed = PresentationDnsName.ParseName(text, null);
        DnsTextResult formatted = PresentationDnsName.FormatName(parsed.Name!);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Name!.Labels, Has.Count.EqualTo(3));
            Assert.That(parsed.Name.Labels[0],
                Is.EqualTo(new byte[] { 0x61, 0x2e, 0x62 }));
            Assert.That(parsed.Name.Labels[1],
                Is.EqualTo(new byte[] { 0x63, 0x5c, 0x64 }));
            Assert.That(parsed.Name.Labels[2],
                Is.EqualTo(new byte[] { 0x40, 0x24, 0x28, 0x29, 0x3b, 0x22 }));
            Assert.That(formatted.Success, Is.True);
            Assert.That(formatted.Text, Is.EqualTo(text));
        });
    }

    [Test]
    public void DecimalEscapesDecodeOneExactOctet()
    {
        DnsTextResult parsed = PresentationDnsName.ParseName(@"\000\255\046.", null);
        DnsTextResult formatted = PresentationDnsName.FormatName(parsed.Name!);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Name!.Labels[0],
                Is.EqualTo(new byte[] { 0, 255, 46 }));
            Assert.That(formatted.Text, Is.EqualTo(@"\000\255\." + "."));
        });
    }

    [Test]
    public void DecimalEscapesRequireThreeDigitsWithinByteRange()
    {
        DnsTextResult shortEscape = PresentationDnsName.ParseName(@"a\1", null);
        DnsTextResult nonDigit = PresentationDnsName.ParseName(@"a\12x", null);
        DnsTextResult excessive = PresentationDnsName.ParseName(@"a\256", null);

        Assert.Multiple(() =>
        {
            Assert.That(shortEscape.Success, Is.False);
            Assert.That(shortEscape.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidText));
            Assert.That(shortEscape.Diagnostics[0].Offset, Is.EqualTo(1));
            Assert.That(nonDigit.Success, Is.False);
            Assert.That(nonDigit.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidText));
            Assert.That(nonDigit.Diagnostics[0].Offset, Is.EqualTo(1));
            Assert.That(excessive.Success, Is.False);
            Assert.That(excessive.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(excessive.Diagnostics[0].Offset, Is.EqualTo(1));
        });
    }

    [Test]
    public void TrailingEscapeReportsItsExactOffset()
    {
        DnsTextResult result = PresentationDnsName.ParseName("abc\\", null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidText));
            Assert.That(result.Diagnostics[0].HasOffset, Is.True);
            Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(3));
        });
    }

    [Test]
    public void UnescapedWhitespaceUnicodeAndMetacharactersAreRejected()
    {
        DnsTextResult whitespace = PresentationDnsName.ParseName("a b", null);
        DnsTextResult unicode = PresentationDnsName.ParseName("café", null);
        DnsTextResult atSign = PresentationDnsName.ParseName("a@b", null);
        DnsTextResult semicolon = PresentationDnsName.ParseName("a;b", null);

        Assert.Multiple(() =>
        {
            Assert.That(whitespace.Success, Is.False);
            Assert.That(whitespace.Diagnostics[0].Offset, Is.EqualTo(1));
            Assert.That(unicode.Success, Is.False);
            Assert.That(unicode.Diagnostics[0].Offset, Is.EqualTo(3));
            Assert.That(atSign.Success, Is.False);
            Assert.That(atSign.Diagnostics[0].Offset, Is.EqualTo(1));
            Assert.That(semicolon.Success, Is.False);
            Assert.That(semicolon.Diagnostics[0].Offset, Is.EqualTo(1));
        });
    }

    [Test]
    public void LeadingAndConsecutiveDotsAreRejectedAsEmptyLabels()
    {
        DnsTextResult leading = PresentationDnsName.ParseName(".example", null);
        DnsTextResult consecutive = PresentationDnsName.ParseName("a..example", null);

        Assert.Multiple(() =>
        {
            Assert.That(leading.Success, Is.False);
            Assert.That(leading.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
            Assert.That(leading.Diagnostics[0].Offset, Is.Zero);
            Assert.That(consecutive.Success, Is.False);
            Assert.That(consecutive.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
            Assert.That(consecutive.Diagnostics[0].Offset, Is.EqualTo(2));
        });
    }

    [Test]
    public void DecodedLabelLengthIsEnforcedForLiteralAndEscapedText()
    {
        string literalMaximum = new string('a', 63);
        string literalOverlong = new string('a', 64);
        string escapedMaximum = string.Empty;
        int index = 0;
        while (index < 63)
        {
            escapedMaximum = escapedMaximum + @"\097";
            index = index + 1;
        }

        DnsTextResult acceptedLiteral = PresentationDnsName.ParseName(
            literalMaximum,
            null);
        DnsTextResult rejectedLiteral = PresentationDnsName.ParseName(
            literalOverlong,
            null);
        DnsTextResult acceptedEscaped = PresentationDnsName.ParseName(
            escapedMaximum,
            null);
        DnsTextResult rejectedEscaped = PresentationDnsName.ParseName(
            escapedMaximum + @"\097",
            null);

        Assert.Multiple(() =>
        {
            Assert.That(acceptedLiteral.Success, Is.True);
            Assert.That(acceptedLiteral.Name!.Labels[0], Has.Length.EqualTo(63));
            Assert.That(rejectedLiteral.Success, Is.False);
            Assert.That(rejectedLiteral.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
            Assert.That(rejectedLiteral.Diagnostics[0].Offset, Is.EqualTo(63));
            Assert.That(acceptedEscaped.Success, Is.True);
            Assert.That(acceptedEscaped.Name!.Labels[0], Has.Length.EqualTo(63));
            Assert.That(rejectedEscaped.Success, Is.False);
            Assert.That(rejectedEscaped.Diagnostics[0].Offset, Is.EqualTo(252));
        });
    }

    [Test]
    public void OriginJoiningStillEnforcesExpandedNameLimit()
    {
        DnsName origin = new DnsName();
        origin.IsAbsolute = true;
        origin.Labels.Add(new byte[63]);
        origin.Labels.Add(new byte[63]);
        origin.Labels.Add(new byte[63]);
        origin.Labels.Add(new byte[61]);

        DnsTextResult result = PresentationDnsName.ParseName("a", origin);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Name, Is.Null);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.NameTooLong));
        });
    }

    [Test]
    public void FormattingAndParsingRoundTripEveryPossibleLabelOctet()
    {
        int value = 0;
        while (value <= 255)
        {
            DnsName name = new DnsName();
            name.IsAbsolute = true;
            name.Labels.Add([(byte)value]);

            DnsTextResult formatted = PresentationDnsName.FormatName(name);
            DnsTextResult parsed = PresentationDnsName.ParseName(formatted.Text, null);

            Assert.That(formatted.Success, Is.True, $"format {value}");
            Assert.That(parsed.Success, Is.True, $"parse {value}");
            Assert.That(parsed.Name!.Labels[0],
                Is.EqualTo(new byte[] { (byte)value }),
                $"octet {value}");
            value = value + 1;
        }
    }

    [Test]
    public void FormattingRejectsInvalidMutableNamesWithDiagnostics()
    {
        DnsName name = new DnsName();
        name.IsAbsolute = true;
        name.Labels.Add(new byte[64]);

        DnsTextResult result = PresentationDnsName.FormatName(name);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLabel));
        });
    }
}
