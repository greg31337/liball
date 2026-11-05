namespace sdp.Tests.Unit.Parsing;

public class SdpConnectionLineParserTests
{
    [Test]
    public void SessionIpv4Unicast_ParsesTypedAndRawValues()
    {
        ISdpParser parser = new SdpFragmentParser();
        string connectionText = "IN IP4 192.0.2.10";

        SdpParseResult result = parser.ParseText("v=0\r\nc=" + connectionText, new SdpParseOptions());
        SdpConnectionData connection = result.Description?.Connection ?? new SdpConnectionData();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(connection.NetworkType, Is.EqualTo("IN"));
            Assert.That(connection.AddressType, Is.EqualTo("IP4"));
            Assert.That(connection.Address, Is.EqualTo("192.0.2.10"));
            Assert.That(connection.AddressKind, Is.EqualTo(SdpAddressKind.Ipv4));
            Assert.That(connection.HasTimeToLive, Is.False);
            Assert.That(connection.TimeToLive, Is.Zero);
            Assert.That(connection.HasAddressCount, Is.False);
            Assert.That(connection.AddressCount, Is.EqualTo(1));
            Assert.That(connection.RawAddress, Is.EqualTo("192.0.2.10"));
            Assert.That(connection.RawText, Is.EqualTo(connectionText));
            Assert.That(connection.Location.CharacterOffset, Is.EqualTo("v=0\r\n".Length));
            Assert.That(connection.Location.ByteOffset, Is.EqualTo(-1));
            Assert.That(connection.Location.LineNumber, Is.EqualTo(2));
            Assert.That(connection.Location.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void Ipv4Multicast_ParsesRequiredTtlAndOptionalAddressCount()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult one = parser.ParseText("v=0\r\nc=IN IP4 224.0.0.1/0", new SdpParseOptions());
        SdpParseResult layered = parser.ParseText("v=0\r\nc=IN IP4 239.255.255.255/255/3", new SdpParseOptions());
        SdpConnectionData oneConnection = one.Description?.Connection ?? new SdpConnectionData();
        SdpConnectionData layeredConnection = layered.Description?.Connection ?? new SdpConnectionData();

        Assert.Multiple(() =>
        {
            Assert.That(one.Success, Is.True);
            Assert.That(oneConnection.Address, Is.EqualTo("224.0.0.1"));
            Assert.That(oneConnection.AddressKind, Is.EqualTo(SdpAddressKind.Ipv4Multicast));
            Assert.That(oneConnection.HasTimeToLive, Is.True);
            Assert.That(oneConnection.TimeToLive, Is.Zero);
            Assert.That(oneConnection.HasAddressCount, Is.False);
            Assert.That(oneConnection.AddressCount, Is.EqualTo(1));
            Assert.That(oneConnection.RawAddress, Is.EqualTo("224.0.0.1/0"));

            Assert.That(layered.Success, Is.True);
            Assert.That(layeredConnection.Address, Is.EqualTo("239.255.255.255"));
            Assert.That(layeredConnection.AddressKind, Is.EqualTo(SdpAddressKind.Ipv4Multicast));
            Assert.That(layeredConnection.HasTimeToLive, Is.True);
            Assert.That(layeredConnection.TimeToLive, Is.EqualTo(255));
            Assert.That(layeredConnection.HasAddressCount, Is.True);
            Assert.That(layeredConnection.AddressCount, Is.EqualTo(3));
            Assert.That(layeredConnection.RawAddress, Is.EqualTo("239.255.255.255/255/3"));
        });
    }

    [TestCase("2001:db8::1")]
    [TestCase("0:0:0:0:0:0:0:1")]
    [TestCase("::ffff:192.0.2.1")]
    public void Ipv6Unicast_AcceptsCompressedFullAndEmbeddedIpv4Forms(string address)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nc=IN IP6 " + address, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Connection?.Address, Is.EqualTo(address));
            Assert.That(result.Description?.Connection?.AddressKind, Is.EqualTo(SdpAddressKind.Ipv6));
            Assert.That(result.Description?.Connection?.HasAddressCount, Is.False);
        });
    }

    [Test]
    public void Ipv6Multicast_ParsesOptionalAddressCountWithoutTtl()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult one = parser.ParseText("v=0\r\nc=IN IP6 ff15::101", new SdpParseOptions());
        SdpParseResult layered = parser.ParseText("v=0\r\nc=IN IP6 FF15::101/4", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(one.Success, Is.True);
            Assert.That(one.Description?.Connection?.AddressKind, Is.EqualTo(SdpAddressKind.Ipv6Multicast));
            Assert.That(one.Description?.Connection?.HasTimeToLive, Is.False);
            Assert.That(one.Description?.Connection?.HasAddressCount, Is.False);
            Assert.That(one.Description?.Connection?.AddressCount, Is.EqualTo(1));
            Assert.That(layered.Success, Is.True);
            Assert.That(layered.Description?.Connection?.Address, Is.EqualTo("FF15::101"));
            Assert.That(layered.Description?.Connection?.AddressKind, Is.EqualTo(SdpAddressKind.Ipv6Multicast));
            Assert.That(layered.Description?.Connection?.HasTimeToLive, Is.False);
            Assert.That(layered.Description?.Connection?.HasAddressCount, Is.True);
            Assert.That(layered.Description?.Connection?.AddressCount, Is.EqualTo(4));
            Assert.That(layered.Description?.Connection?.RawAddress, Is.EqualTo("FF15::101/4"));
        });
    }

    [TestCase("IP4")]
    [TestCase("IP6")]
    public void IpAddressTypes_AcceptFqdnConnectionAddresses(string addressType)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nc=IN " + addressType + " relay.example.test", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Description?.Connection?.Address, Is.EqualTo("relay.example.test"));
            Assert.That(result.Description?.Connection?.AddressKind, Is.EqualTo(SdpAddressKind.HostName));
        });
    }

    [Test]
    public void ExtensionConnectionTypes_PreserveOpaqueAddressAndSuffixes()
    {
        ISdpParser parser = new SdpFragmentParser();
        string text = "X-NET X-ADDR opaque:value/part/extra";

        SdpParseResult result = parser.ParseText("v=0\r\nc=" + text, new SdpParseOptions());
        SdpConnectionData connection = result.Description?.Connection ?? new SdpConnectionData();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(connection.NetworkType, Is.EqualTo("X-NET"));
            Assert.That(connection.AddressType, Is.EqualTo("X-ADDR"));
            Assert.That(connection.Address, Is.EqualTo("opaque:value/part/extra"));
            Assert.That(connection.AddressKind, Is.EqualTo(SdpAddressKind.Unknown));
            Assert.That(connection.RawAddress, Is.EqualTo("opaque:value/part/extra"));
            Assert.That(connection.RawText, Is.EqualTo(text));
        });
    }

    [Test]
    public void MediaConnections_AreRepeatedAndIndependentFromSessionConnection()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input =
            "v=0\r\n" +
            "c=IN IP4 session.example\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "c=IN IP4 192.0.2.1\r\n" +
            "c=IN IP6 2001:db8::1\r\n" +
            "m=video 9 RTP/AVP 96\r\n" +
            "c=X-NET X-ADDR opaque";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(description.Connection?.Address, Is.EqualTo("session.example"));
            Assert.That(description.MediaDescriptions, Has.Count.EqualTo(2));
            Assert.That(description.MediaDescriptions[0].Connections, Has.Count.EqualTo(2));
            Assert.That(description.MediaDescriptions[0].Connections[0].AddressKind, Is.EqualTo(SdpAddressKind.Ipv4));
            Assert.That(description.MediaDescriptions[0].Connections[1].AddressKind, Is.EqualTo(SdpAddressKind.Ipv6));
            Assert.That(description.MediaDescriptions[1].Connections, Has.Count.EqualTo(1));
            Assert.That(description.MediaDescriptions[1].Connections[0].AddressKind, Is.EqualTo(SdpAddressKind.Unknown));
        });
    }

    [Test]
    public void MediaConnectionOccurrencePaths_IncludeMalformedSourceOccurrences()
    {
        ISdpParser parser = new SdpFragmentParser();
        string input = "v=0\r\nm=audio 9 RTP/AVP 0\r\nc=IN IP4\r\nc=IN IP4 bad";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.MediaDescriptions[0].Connections, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Diagnostics[0].MediaIndex, Is.Zero);
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.Zero);
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationConnection));
            Assert.That(result.Diagnostics[1].Path.OccurrenceIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void DuplicateSessionConnection_PreservesTheFirstOccurrence()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nc=IN IP4 first.example\r\nc=IN IP6 2001:db8::1", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Connection?.Address, Is.EqualTo("first.example"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseDuplicateField));
            Assert.That(result.Diagnostics[0].Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(result.Diagnostics[0].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(3));
        });
    }

    [TestCase("c=")]
    [TestCase("c=IN IP4")]
    [TestCase("c=IN IP4 host extra")]
    [TestCase("c= IN IP4 host")]
    [TestCase("c=IN  IP4 host")]
    [TestCase("c=IN IP4 host ")]
    public void Connection_RequiresExactlyThreeSingleSpaceSeparatedTokens(string connectionLine)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\n" + connectionLine, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Connection, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('c'));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(2));
        });
    }

    [Test]
    public void Connection_ValidatesNetworkAndAddressTokenGrammar()
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nc=I/N IP@ opaque", new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Connection?.RawText, Is.EqualTo("I/N IP@ opaque"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(4));
            Assert.That(result.Diagnostics[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[1].Location.ColumnNumber, Is.EqualTo(9));
        });
    }

    [TestCase("999.0.0.1")]
    [TestCase("192.168.01.1")]
    [TestCase("192.0.2")]
    [TestCase("240.0.0.1")]
    public void InvalidIpv4Address_IsRetainedAsRawFallback(string address)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nc=IN IP4 " + address, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Connection?.Address, Is.EqualTo(address));
            Assert.That(result.Description?.Connection?.RawAddress, Is.EqualTo(address));
            Assert.That(result.Description?.Connection?.AddressKind, Is.EqualTo(SdpAddressKind.Unknown));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationConnection));
        });
    }

    [TestCase("224.0.0.1", SdpDiagnosticCodes.ValidationConnection)]
    [TestCase("224.0.0.1/", SdpDiagnosticCodes.ParseInvalidNumber)]
    [TestCase("224.0.0.1/x", SdpDiagnosticCodes.ParseInvalidNumber)]
    [TestCase("224.0.0.1/01", SdpDiagnosticCodes.ValidationConnection)]
    [TestCase("224.0.0.1/256", SdpDiagnosticCodes.ValidationConnection)]
    [TestCase("192.0.2.1/1", SdpDiagnosticCodes.ValidationConnection)]
    [TestCase("224.0.0.1/1/2/3", SdpDiagnosticCodes.ParseInvalidToken)]
    public void Ipv4MulticastSuffixRules_AreValidated(string address, string diagnosticCode)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nc=IN IP4 " + address, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Connection?.RawAddress, Is.EqualTo(address));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(diagnosticCode));
        });
    }

    [TestCase("224.0.0.1/1/0", SdpDiagnosticCodes.ValidationConnection)]
    [TestCase("224.0.0.1/1/02", SdpDiagnosticCodes.ValidationConnection)]
    [TestCase("224.0.0.1/1/x", SdpDiagnosticCodes.ParseInvalidNumber)]
    [TestCase("224.0.0.1/1/2147483648", SdpDiagnosticCodes.ParseNumberOverflow)]
    [TestCase("ff15::1/0", SdpDiagnosticCodes.ValidationConnection)]
    public void MulticastAddressCount_RequiresPositiveRepresentableCanonicalDecimal(string address, string diagnosticCode)
    {
        ISdpParser parser = new SdpFragmentParser();
        string addressType = address.IndexOf(':') >= 0 ? "IP6" : "IP4";

        SdpParseResult result = parser.ParseText("v=0\r\nc=IN " + addressType + " " + address, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Connection?.HasAddressCount, Is.True);
            Assert.That(result.Description?.Connection?.AddressCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(diagnosticCode));
        });
    }

    [TestCase("2001:db8::1/2")]
    [TestCase("ff15::1/2/3")]
    [TestCase("2001::db8::1")]
    [TestCase("1:2:3:4:5:6:7:8:9")]
    [TestCase("gggg::1")]
    public void InvalidIpv6AddressOrSuffix_IsRetainedAndDiagnosed(string address)
    {
        ISdpParser parser = new SdpFragmentParser();

        SdpParseResult result = parser.ParseText("v=0\r\nc=IN IP6 " + address, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Connection?.RawAddress, Is.EqualTo(address));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('c'));
        });
    }

    [Test]
    public void ConnectionTokenLimit_StopsBeforeFollowingLines()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumTokenLength = 4 };

        SdpParseResult result = parser.ParseText("v=0\r\nc=LONGN IP4 host\r\ns=unread", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Connection, Is.Null);
            Assert.That(result.LinesConsumed, Is.EqualTo(2));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(3));
        });
    }

    [Test]
    public void Utf8ConnectionTokenLimit_IsMeasuredInInputBytes()
    {
        ISdpParser parser = new SdpFragmentParser();
        SdpParseOptions options = new SdpParseOptions { MaximumTokenLength = 4 };

        SdpParseResult result = parser.ParseUtf8Bytes(System.Text.Encoding.UTF8.GetBytes("v=0\r\nc=X Y ééé"), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Connection, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseTokenLengthLimitExceeded));
            Assert.That(result.Diagnostics[0].Location.CharacterOffset, Is.EqualTo(11));
            Assert.That(result.Diagnostics[0].Location.ByteOffset, Is.EqualTo(11));
            Assert.That(result.Diagnostics[0].Location.ColumnNumber, Is.EqualTo(7));
        });
    }

    [Test]
    public void VeryLongIpv4Segment_IsDiagnosedWithoutCheckedArithmeticFailure()
    {
        ISdpParser parser = new SdpFragmentParser();
        string address = "9999999999999999999999999999999999999999.0.0.1";

        SdpParseResult result = parser.ParseText("v=0\r\nc=IN IP4 " + address, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description?.Connection?.RawAddress, Is.EqualTo(address));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationConnection));
        });
    }
}
