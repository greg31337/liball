namespace sdp.Tests.Conformance.Rfc8839;

public class SdpIceRfc8839ConformanceTests
{
    const string SessionUsername = "sess";
    const string SessionPassword = "abcdefghijklmnopqrstuv";
    const string MediaUsername = "medi";
    const string MediaPassword = "zyxwvutsrqponmlkjihgfe";

    [TestCase("ice-ufrag", 3, false)]
    [TestCase("ice-ufrag", 4, true)]
    [TestCase("ice-ufrag", 256, true)]
    [TestCase("ice-ufrag", 257, false)]
    [TestCase("ice-pwd", 21, false)]
    [TestCase("ice-pwd", 22, true)]
    [TestCase("ice-pwd", 256, true)]
    [TestCase("ice-pwd", 257, false)]
    public void CredentialDecoder_CoversEveryReceiveLengthBoundary(string name, int length, bool expectedSuccess)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = ValueAttribute(name, new string('A', length));

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
        Assert.That(result.SourceAttribute, Is.SameAs(source));
    }

    [TestCase("ice-ufrag", 3, false)]
    [TestCase("ice-ufrag", 4, true)]
    [TestCase("ice-ufrag", 32, true)]
    [TestCase("ice-ufrag", 33, false)]
    [TestCase("ice-pwd", 21, false)]
    [TestCase("ice-pwd", 22, true)]
    [TestCase("ice-pwd", 256, true)]
    [TestCase("ice-pwd", 257, false)]
    public void CredentialEncoder_CoversEverySendLengthBoundary(string name, int length, bool expectedSuccess)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttributeEncodeResult result;
        if (name == "ice-ufrag")
        {
            result = codec.EncodeAttribute(new SdpIceUsernameFragmentAttribute { UsernameFragment = new string('A', length) });
        }
        else
        {
            result = codec.EncodeAttribute(new SdpIcePasswordAttribute { Password = new string('A', length) });
        }

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
        Assert.That(result.Attribute == null, Is.EqualTo(!expectedSuccess));
    }

    [TestCase("AZaz09+/", true)]
    [TestCase("abc-", false)]
    [TestCase("abc_", false)]
    [TestCase("abc=", false)]
    [TestCase("abc ", false)]
    [TestCase("abcé", false)]
    public void CredentialAlphabet_IsExactlyTheIceCharacterSet(string value, bool expectedSuccess)
    {
        SdpAttribute source = ValueAttribute("ice-ufrag", value);

        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(source);

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
    }

    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(32, true)]
    [TestCase(33, false)]
    public void CandidateFoundation_CoversEveryLengthBoundary(int length, bool expectedSuccess)
    {
        string foundation = new string('F', length);
        SdpAttribute source = CandidateAttribute(foundation + " 1 UDP 1 192.0.2.1 5000 typ host");

        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(source);

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
    }

    [TestCase("AZaz09+/", true)]
    [TestCase("bad-", false)]
    [TestCase("bad_", false)]
    public void CandidateFoundation_UsesTheIceCharacterAlphabet(string foundation, bool expectedSuccess)
    {
        SdpAttribute source = CandidateAttribute(foundation + " 1 UDP 1 192.0.2.1 5000 typ host");

        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(source);

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
    }

    [TestCase("0", true, false)]
    [TestCase("1", true, true)]
    [TestCase("256", true, true)]
    [TestCase("257", true, false)]
    [TestCase("999", true, false)]
    [TestCase("1000", false, false)]
    [TestCase("x", false, false)]
    public void CandidateComponent_CoversGrammarAndSemanticBoundaries(string component, bool expectedDecode, bool expectedSemanticSuccess)
    {
        string value = "F " + component + " UDP 1 192.0.2.1 5000 typ host";

        AssertCandidateBoundary(value, expectedDecode, expectedSemanticSuccess);
    }

    [TestCase("0", true, false)]
    [TestCase("1", true, true)]
    [TestCase("2147483647", true, true)]
    [TestCase("2147483648", true, false)]
    [TestCase("4294967295", true, false)]
    [TestCase("4294967296", false, false)]
    [TestCase("99999999999", false, false)]
    [TestCase("x", false, false)]
    public void CandidatePriority_CoversGrammarAndSemanticBoundaries(string priority, bool expectedDecode, bool expectedSemanticSuccess)
    {
        string value = "F 1 UDP " + priority + " 192.0.2.1 5000 typ host";

        AssertCandidateBoundary(value, expectedDecode, expectedSemanticSuccess);
    }

    [TestCase("0", true, false)]
    [TestCase("1", true, true)]
    [TestCase("65535", true, true)]
    [TestCase("65536", false, false)]
    [TestCase("x", false, false)]
    public void CandidatePort_CoversGrammarAndSemanticBoundaries(string port, bool expectedDecode, bool expectedSemanticSuccess)
    {
        string value = "F 1 UDP 1 192.0.2.1 " + port + " typ host";

        AssertCandidateBoundary(value, expectedDecode, expectedSemanticSuccess);
    }

    [TestCase("UDP", "192.0.2.1", "host", SdpIceTransportProtocol.Udp, SdpAddressKind.Ipv4, SdpIceCandidateType.Host)]
    [TestCase("udp", "2001:db8::1", "srflx", SdpIceTransportProtocol.Udp, SdpAddressKind.Ipv6, SdpIceCandidateType.ServerReflexive)]
    [TestCase("X-QUIC", "peer.example", "x-future", SdpIceTransportProtocol.Unknown, SdpAddressKind.HostName, SdpIceCandidateType.Unknown)]
    public void CandidateTokensAndAddressForms_RetainRegisteredAndExtensionValues(string transport, string address, string candidateType, SdpIceTransportProtocol expectedTransport, SdpAddressKind expectedAddress, SdpIceCandidateType expectedType)
    {
        string related = candidateType == "host" || candidateType == "x-future" ? string.Empty : " raddr 2001:db8::2 rport 6000";
        SdpAttribute source = CandidateAttribute("F 1 " + transport + " 1 " + address + " 5000 typ " + candidateType + related);

        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.IceCandidate!.TransportProtocol, Is.EqualTo(expectedTransport));
            Assert.That(result.IceCandidate.TransportToken, Is.EqualTo(transport));
            Assert.That(result.IceCandidate.AddressKind, Is.EqualTo(expectedAddress));
            Assert.That(result.IceCandidate.CandidateType, Is.EqualTo(expectedType));
            Assert.That(result.IceCandidate.CandidateTypeToken, Is.EqualTo(candidateType));
        });
    }

    [Test]
    public void CandidateExtensions_PreserveOrderDuplicatesAndVisibleCharacterBoundaries()
    {
        string value = "F 1 UDP 1 192.0.2.1 5000 typ host x-visible !#$%&'*+-.^_`|~ x-visible second x-empty ";
        SdpAttribute source = CandidateAttribute(value);
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.IceCandidate!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.IceCandidate!.Extensions.Select(x => x.Name), Is.EqualTo(new[] { "x-visible", "x-visible", "x-empty" }));
            Assert.That(decoded.IceCandidate.Extensions.Select(x => x.Value), Is.EqualTo(new[] { "!#$%&'*+-.^_`|~", "second", string.Empty }));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Value, Does.EndWith("x-visible !#$%&'*+-.^_`|~ x-visible second x-empty "));
        });
    }

    [TestCase("TCP", "active", 9, true)]
    [TestCase("tcp", "ACTIVE", 5000, false)]
    [TestCase("TCP", "passive", 5000, true)]
    [TestCase("TCP", "so", 5000, true)]
    [TestCase("TCP", "future", 5000, false)]
    [TestCase("UDP", "passive", 5000, false)]
    public void TcpCandidates_EnforceTransportTypeAndActiveDiscardPort(string transport, string tcpType, int port, bool expectedSuccess)
    {
        string value = "F 1 " + transport + " 1 192.0.2.1 " + port + " typ host tcptype " + tcpType;
        SdpSessionDescription description = CreatePartialIceDescription();
        description.MediaDescriptions[0].Attributes.Add(CandidateAttribute(value));

        SdpValidationResult result = ValidateIce(description);

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
    }

    [Test]
    public void TcpCandidate_RequiresAConnectionTypeExtension()
    {
        SdpSessionDescription description = CreatePartialIceDescription();
        description.MediaDescriptions[0].Attributes.Add(CandidateAttribute("F 1 TCP 1 192.0.2.1 5000 typ host"));

        SdpValidationResult result = ValidateIce(description);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Issues.Single().Detail, Does.Contain("requires"));
    }

    [TestCase(SessionUsername, SessionPassword, null, null, SessionUsername, SessionPassword)]
    [TestCase(SessionUsername, SessionPassword, MediaUsername, null, MediaUsername, SessionPassword)]
    [TestCase(SessionUsername, SessionPassword, null, MediaPassword, SessionUsername, MediaPassword)]
    [TestCase(null, null, MediaUsername, MediaPassword, MediaUsername, MediaPassword)]
    [TestCase(SessionUsername, SessionPassword, MediaUsername, MediaPassword, MediaUsername, MediaPassword)]
    public void Credentials_InheritAndOverrideIndependentlyAcrossEveryScopeCombination(string? sessionUsername, string? sessionPassword, string? mediaUsername, string? mediaPassword, string expectedUsername, string expectedPassword)
    {
        SdpSessionDescription description = CreatePartialIceDescription();
        RemoveCredentials(description);
        AddOptionalCredential(description.Attributes, "ice-ufrag", sessionUsername);
        AddOptionalCredential(description.Attributes, "ice-pwd", sessionPassword);
        AddOptionalCredential(description.MediaDescriptions[0].Attributes, "ice-ufrag", mediaUsername);
        AddOptionalCredential(description.MediaDescriptions[0].Attributes, "ice-pwd", mediaPassword);
        ISdpIceService service = new SdpIceService();

        SdpValidationResult validation = ValidateIce(description);
        SdpIceDescription resolved = service.ResolveMediaIce(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.True);
            Assert.That(resolved.Credentials, Is.Not.Null);
            Assert.That(resolved.Credentials!.UsernameFragment, Is.EqualTo(expectedUsername));
            Assert.That(resolved.Credentials.Password, Is.EqualTo(expectedPassword));
        });
    }

    [Test]
    public void FullAndLiteAgents_ApplyDistinctGatheringAndCandidateRules()
    {
        SdpSessionDescription full = CreatePartialIceDescription();
        SdpSessionDescription liteWithoutCandidate = CreatePartialIceDescription();
        liteWithoutCandidate.Attributes.Add(PropertyAttribute("ice-lite"));
        SdpSessionDescription liteWithHost = CreatePartialIceDescription();
        liteWithHost.Attributes.Add(PropertyAttribute("ice-lite"));
        liteWithHost.MediaDescriptions[0].Attributes.Add(CandidateAttribute("host 1 UDP 1 192.0.2.1 5000 typ host"));
        SdpSessionDescription liteWithReflexive = CreatePartialIceDescription();
        liteWithReflexive.Attributes.Add(PropertyAttribute("ice-lite"));
        liteWithReflexive.MediaDescriptions[0].Attributes.Add(CandidateAttribute("srflx 1 UDP 1 192.0.2.1 5000 typ srflx raddr 10.0.0.1 rport 4000"));
        ISdpIceService service = new SdpIceService();

        Assert.Multiple(() =>
        {
            Assert.That(ValidateIce(full).Success, Is.True);
            Assert.That(service.ResolveMediaIce(full, 0).IceLite, Is.False);
            Assert.That(ValidateIce(liteWithoutCandidate).Success, Is.False);
            Assert.That(ValidateIce(liteWithHost).Success, Is.True);
            Assert.That(service.ResolveMediaIce(liteWithHost, 0).IceLite, Is.True);
            Assert.That(ValidateIce(liteWithReflexive).Issues.Any(x => x.Detail.Contains("only host", StringComparison.Ordinal)), Is.True);
        });
    }

    [TestCase("192.0.2.1", 5000, "192.0.2.1", 5000, true)]
    [TestCase("2001:db8::1", 5000, "2001:db8::1", 5000, true)]
    [TestCase("192.0.2.1", 5000, "192.0.2.2", 5000, false)]
    [TestCase("192.0.2.1", 5000, "192.0.2.1", 5001, false)]
    [TestCase("peer.example", 5000, "192.0.2.2", 5000, true)]
    public void DefaultDestination_ValidatesAddressAndPortWithFqdnException(string defaultAddress, int defaultPort, string candidateAddress, int candidatePort, bool expectedSuccess)
    {
        SdpSessionDescription description = CreateCompletedIceDescription(defaultAddress, defaultPort);
        description.MediaDescriptions[0].Attributes.Add(CandidateAttribute("default 1 UDP 1 " + candidateAddress + " " + candidatePort + " typ host"));

        SdpValidationResult result = ValidateIce(description);

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
    }

    [TestCase("0.0.0.0")]
    [TestCase("::")]
    public void PlaceholderDefaultDestination_AllowsAnEmptyCandidateSet(string address)
    {
        SdpSessionDescription description = CreateCompletedIceDescription(address, 9);

        SdpValidationResult result = ValidateIce(description);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ImplicitAndExplicitRtcpDefaults_RequireMatchingComponentTwoCandidates()
    {
        SdpSessionDescription implicitDescription = CreateCompletedRtpDescription();
        implicitDescription.MediaDescriptions[0].Attributes.Add(CandidateAttribute("rtp 1 UDP 2 192.0.2.1 5000 typ host"));
        implicitDescription.MediaDescriptions[0].Attributes.Add(CandidateAttribute("rtcp 2 UDP 1 192.0.2.1 5001 typ host"));
        SdpSessionDescription explicitDescription = CreateCompletedRtpDescription();
        explicitDescription.MediaDescriptions[0].Attributes.Add(ValueAttribute("rtcp", "6000 IN IP4 192.0.2.2"));
        explicitDescription.MediaDescriptions[0].Attributes.Add(CandidateAttribute("rtp 1 UDP 2 192.0.2.1 5000 typ host"));
        explicitDescription.MediaDescriptions[0].Attributes.Add(CandidateAttribute("rtcp 2 UDP 1 192.0.2.2 6000 typ host"));

        Assert.Multiple(() =>
        {
            Assert.That(ValidateIce(implicitDescription).Success, Is.True);
            Assert.That(ValidateIce(explicitDescription).Success, Is.True);
        });
    }

    [Test]
    public void IceMismatch_AllowsNoCandidatesAndRejectsAnyCandidate()
    {
        SdpSessionDescription description = CreateCompletedIceDescription("192.0.2.1", 5000);
        description.MediaDescriptions[0].Attributes.Add(PropertyAttribute("ice-mismatch"));

        SdpValidationResult withoutCandidates = ValidateIce(description);
        description.MediaDescriptions[0].Attributes.Add(CandidateAttribute("late 1 UDP 1 192.0.2.1 5000 typ host"));
        SdpValidationResult withCandidate = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(withoutCandidates.Success, Is.True);
            Assert.That(withCandidate.Success, Is.False);
            Assert.That(withCandidate.Issues.Any(x => x.AttributeName == "ice-mismatch"), Is.True);
        });
    }

    [Test]
    public void TrickleCoverage_AcceptsSessionScopeAndRequiresEveryMediaAtMediaScope()
    {
        SdpSessionDescription sessionTrickle = CreatePartialIceDescription();
        sessionTrickle.MediaDescriptions.Add(new SdpMediaDescription { MediaType = "application", Port = 5002, Protocol = "UDP" });
        SdpSessionDescription mediaTrickle = CreatePartialIceDescription();
        MoveTrickleToFirstMedia(mediaTrickle);
        mediaTrickle.MediaDescriptions.Add(new SdpMediaDescription { MediaType = "application", Port = 5002, Protocol = "UDP" });

        SdpValidationResult sessionResult = ValidateIce(sessionTrickle);
        SdpValidationResult mediaResult = ValidateIce(mediaTrickle);

        Assert.Multiple(() =>
        {
            Assert.That(sessionResult.Success, Is.True);
            Assert.That(mediaResult.Success, Is.False);
            Assert.That(mediaResult.Issues.Any(x => x.AttributeName == "ice-options" && x.MediaIndex == 1), Is.True);
        });
    }

    [Test]
    public void TrickleFragment_RoundTripsMidCandidatesAndCompletion()
    {
        string text =
            "a=ice-ufrag:sess\r\n" +
            "a=ice-pwd:abcdefghijklmnopqrstuv\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "a=mid:data\r\n" +
            "a=candidate:F 1 UDP 1 192.0.2.1 5000 typ host x-empty \r\n" +
            "a=end-of-candidates\r\n";
        ISdpIceService service = new SdpIceService();

        SdpParseResult parsed = service.ParseTrickleIceFragment(text, new SdpParseOptions());
        SdpWriteResult written = service.WriteTrickleIceFragment(parsed.Description!, new SdpWriteOptions());
        SdpParseResult reparsed = service.ParseTrickleIceFragment(written.Text, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(written.Success, Is.True);
            Assert.That(reparsed.Success, Is.True);
            Assert.That(service.ResolveMediaIce(reparsed.Description!, 0).Candidates, Has.Count.EqualTo(1));
            Assert.That(service.ResolveMediaIce(reparsed.Description!, 0).EndOfCandidates, Is.True);
        });
    }

    [Test]
    public void RestartAndBundle_UseEffectiveCredentialsAndTransportOwner()
    {
        SdpSessionDescription previous = CreateBundledDescription(SessionUsername, SessionPassword);
        SdpSessionDescription relocated = CreateBundledDescription(SessionUsername, SessionPassword);
        relocated.Attributes.Clear();
        relocated.MediaDescriptions[0].Attributes.Add(ValueAttribute("ice-ufrag", SessionUsername));
        relocated.MediaDescriptions[0].Attributes.Add(ValueAttribute("ice-pwd", SessionPassword));
        ISdpIceService service = new SdpIceService();

        bool scopeOnlyChange = service.IsIceRestart(previous, 1, relocated, 1);
        service.RestartIce(previous, 1, new SdpIceCredentials { UsernameFragment = MediaUsername, Password = MediaPassword });
        SdpIceDescription owner = service.ResolveMediaIce(previous, 0);
        SdpIceDescription bundled = service.ResolveEffectiveIce(previous, 1);

        Assert.Multiple(() =>
        {
            Assert.That(scopeOnlyChange, Is.False);
            Assert.That(service.ResolveIceTransportMediaIndex(previous, 1), Is.Zero);
            Assert.That(owner.Credentials!.UsernameFragment, Is.EqualTo(MediaUsername));
            Assert.That(bundled.Credentials!.Password, Is.EqualTo(MediaPassword));
            Assert.That(previous.MediaDescriptions[1].Attributes.All(x => x.Name != "ice-ufrag"), Is.True);
        });
    }

    [Test]
    public void MalformedIceInput_UsesParseDecodeValidationAndFragmentResultPaths()
    {
        string secret = "abcdefghijklmnopqrstuv";
        string document =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 192.0.2.1\r\n" +
            "s=-\r\n" +
            "c=IN IP4 0.0.0.0\r\n" +
            "t=0 0\r\n" +
            "a=ice-ufrag:sess\r\n" +
            "a=ice-pwd:" + secret + "\r\n" +
            "m=application 9 UDP data\r\n" +
            "a=candidate:malformed\r\n";
        ISdpParser parser = new SdpParser();

        SdpParseResult parsed = parser.ParseText(document, new SdpParseOptions());
        SdpAttribute candidate = parsed.Description!.MediaDescriptions[0].Attributes.Single(x => x.Name == "candidate");
        SdpAttributeDecodeResult decoded = new SdpAttributeCodec().DecodeAttribute(candidate);
        SdpValidationResult validated = ValidateIce(parsed.Description);
        string fragment = "a=ice-ufrag:sess\r\na=ice-pwd:" + secret + "\r\nm=audio 9 RTP/AVP 0\r\na=mid:data\r\na=candidate:malformed\r\n";
        SdpParseResult fragmentResult = new SdpIceService().ParseTrickleIceFragment(fragment, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(decoded.Success, Is.False);
            Assert.That(decoded.SourceAttribute, Is.SameAs(candidate));
            Assert.That(validated.Success, Is.False);
            Assert.That(fragmentResult.Success, Is.False);
            Assert.That(validated.Issues.All(x => !x.Detail.Contains(secret, StringComparison.Ordinal)), Is.True);
            Assert.That(fragmentResult.Diagnostics.All(x => !x.Detail.Contains(secret, StringComparison.Ordinal)), Is.True);
        });
    }

    static void AssertCandidateBoundary(string value, bool expectedDecode, bool expectedSemanticSuccess)
    {
        SdpAttribute attribute = CandidateAttribute(value);
        SdpAttributeDecodeResult decoded = new SdpAttributeCodec().DecodeAttribute(attribute);
        SdpSessionDescription description = CreatePartialIceDescription();
        description.MediaDescriptions[0].Attributes.Add(attribute);
        SdpValidationResult validated = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.EqualTo(expectedDecode));
            Assert.That(validated.Success, Is.EqualTo(expectedSemanticSuccess));
        });
    }

    static SdpSessionDescription CreatePartialIceDescription()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute("ice-options", "trickle ice2"));
        description.Attributes.Add(ValueAttribute("ice-ufrag", SessionUsername));
        description.Attributes.Add(ValueAttribute("ice-pwd", SessionPassword));
        description.MediaDescriptions.Add(new SdpMediaDescription { MediaType = "application", Port = 5000, Protocol = "UDP" });
        return description;
    }

    static SdpSessionDescription CreateCompletedIceDescription(string address, int port)
    {
        SdpAddressKind kind = SdpAddressKind.Ipv4;
        string addressType = "IP4";
        if (address.Contains(':', StringComparison.Ordinal))
        {
            kind = SdpAddressKind.Ipv6;
            addressType = "IP6";
        }
        else if (address.Contains("peer", StringComparison.Ordinal))
        {
            kind = SdpAddressKind.HostName;
        }
        SdpSessionDescription description = new SdpSessionDescription();
        description.Connection = new SdpConnectionData { NetworkType = "IN", AddressType = addressType, Address = address, AddressKind = kind };
        description.Attributes.Add(ValueAttribute("ice-ufrag", SessionUsername));
        description.Attributes.Add(ValueAttribute("ice-pwd", SessionPassword));
        description.MediaDescriptions.Add(new SdpMediaDescription { MediaType = "application", Port = port, Protocol = "UDP" });
        return description;
    }

    static SdpSessionDescription CreateCompletedRtpDescription()
    {
        SdpSessionDescription description = CreateCompletedIceDescription("192.0.2.1", 5000);
        description.MediaDescriptions[0].MediaType = "audio";
        description.MediaDescriptions[0].MediaKind = SdpMediaKind.Audio;
        description.MediaDescriptions[0].Protocol = "RTP/AVP";
        return description;
    }

    static SdpSessionDescription CreateBundledDescription(string username, string password)
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute("group", "BUNDLE audio video"));
        description.Attributes.Add(ValueAttribute("ice-ufrag", username));
        description.Attributes.Add(ValueAttribute("ice-pwd", password));
        SdpMediaDescription audio = new SdpMediaDescription();
        audio.Attributes.Add(ValueAttribute("mid", "audio"));
        SdpMediaDescription video = new SdpMediaDescription();
        video.Attributes.Add(ValueAttribute("mid", "video"));
        description.MediaDescriptions.Add(audio);
        description.MediaDescriptions.Add(video);
        return description;
    }

    static void RemoveCredentials(SdpSessionDescription description)
    {
        int index = description.Attributes.Count - 1;
        while (index >= 0)
        {
            if (description.Attributes[index].Name == "ice-ufrag" || description.Attributes[index].Name == "ice-pwd")
            {
                description.Attributes.RemoveAt(index);
            }
            index = index - 1;
        }
    }

    static void AddOptionalCredential(List<SdpAttribute> attributes, string name, string? value)
    {
        if (value != null)
        {
            attributes.Add(ValueAttribute(name, value));
        }
    }

    static void MoveTrickleToFirstMedia(SdpSessionDescription description)
    {
        int index = 0;
        while (index < description.Attributes.Count)
        {
            if (description.Attributes[index].Name == "ice-options")
            {
                SdpAttribute attribute = description.Attributes[index];
                description.Attributes.RemoveAt(index);
                description.MediaDescriptions[0].Attributes.Add(attribute);
                return;
            }
            index = index + 1;
        }
    }

    static SdpAttribute CandidateAttribute(string value)
    {
        return ValueAttribute("candidate", value);
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }

    static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Property, Name = name, RawText = name };
    }

    static SdpValidationResult ValidateIce(SdpSessionDescription description)
    {
        SdpValidationOptions options = new SdpValidationOptions { Profile = SdpValidationProfile.Ice };
        return new SdpValidator().ValidateDescription(description, options);
    }
}
