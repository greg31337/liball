namespace sdp.Tests.Unit.Validation;

public class SdpBundleValidationTests
{
    [Test]
    public void InitialOfferAndAnswer_WithRequiredBundleSignalingAreValid()
    {
        SdpSessionDescription offer = InitialOffer();
        SdpSessionDescription answer = EstablishedDescription();

        SdpValidationResult offerResult = Validate(offer, SdpNegotiationRole.Offerer, null);
        SdpValidationResult answerResult = Validate(answer, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(offerResult.Success, Is.True);
            Assert.That(offerResult.Issues, Is.Empty);
            Assert.That(answerResult.Success, Is.True);
            Assert.That(answerResult.Issues, Is.Empty);
        });
    }

    [Test]
    public void EstablishedBundle_RequiresMatchingAddressAndTransportProtocols()
    {
        SdpSessionDescription previous = EstablishedDescription();
        SdpSessionDescription description = EstablishedDescription();
        description.MediaDescriptions[1].Port = 20002;
        description.MediaDescriptions[1].Connections.Add(Connection(SdpAddressTypes.Ip6, "2001:db8::20"));
        description.MediaDescriptions[1].Protocol = SdpTransportProtocols.TcpRtpAvp;

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, previous);
        List<SdpValidationIssue> bundleIssues = result.Issues.Where(issue => issue.Code == SdpDiagnosticCodes.ValidationBundle).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(bundleIssues.Any(issue => issue.FieldName == SdpFieldNames.Connection && issue.Detail.Contains("address type")), Is.True);
            Assert.That(bundleIssues.Any(issue => issue.FieldName == SdpFieldNames.Media && issue.Detail.Contains("tagged address and port")), Is.True);
            Assert.That(bundleIssues.Any(issue => issue.FieldName == SdpFieldNames.Media && issue.Detail.Contains("transport-layer protocol")), Is.True);
            Assert.That(bundleIssues.Any(issue => issue.FieldName == SdpFieldNames.Media && issue.Detail.Contains("RTP transport profile")), Is.True);
        });
    }

    [Test]
    public void BundlePortAndBundleOnlyRulesRejectInvalidPlacementAndZeroPorts()
    {
        SdpSessionDescription description = InitialOffer();
        description.MediaDescriptions[1].Port = 0;
        SdpMediaDescription outside = Media("outside", 0, SdpTransportProtocols.RtpAvp);
        outside.Attributes.Add(PropertyAttribute(SdpAttributeNames.BundleOnly));
        outside.Attributes.Add(ExtMap(1));
        description.MediaDescriptions.Add(outside);

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Unknown, null);
        List<SdpValidationIssue> bundleIssues = result.Issues.Where(issue => issue.Code == SdpDiagnosticCodes.ValidationBundle).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(bundleIssues.Any(issue => issue.MediaIndex == 1 && issue.FieldName == SdpFieldNames.Media && issue.Detail.Contains("zero-port")), Is.True);
            Assert.That(bundleIssues.Any(issue => issue.MediaIndex == 3 && issue.AttributeName == SdpAttributeNames.BundleOnly && issue.Detail.Contains("listed in a BUNDLE group")), Is.True);
        });
    }

    [Test]
    public void InitialOffer_AllowsDisabledZeroPortMemberWithoutBundleOnly()
    {
        SdpSessionDescription description = InitialOffer();
        description.MediaDescriptions[1].Port = 0;
        RemoveAttribute(description.MediaDescriptions[1].Attributes, SdpAttributeNames.RtcpMux);

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void MalformedBundleReferences_DoNotInventAResolvableTaggedTransport()
    {
        SdpSessionDescription description = EstablishedDescription();
        description.Attributes[0] = ValueAttribute(SdpAttributeNames.Group, "BUNDLE missing video data");

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Answerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues.Single().Code, Is.EqualTo(SdpDiagnosticCodes.ValidationGroup));
            Assert.That(result.Issues.Single().Detail, Does.Contain("existing media section"));
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationBundle), Is.False);
        });
    }

    [Test]
    public void DuplicateMediaIdentifiers_DoNotCreateAnAmbiguousBundleTransport()
    {
        SdpSessionDescription description = EstablishedDescription();
        description.MediaDescriptions[2].Attributes[0] = ValueAttribute(SdpAttributeNames.MediaId, "video");

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Answerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationGroup), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Detail.Contains("unique")), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Detail.Contains("existing media section")), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationBundle), Is.False);
        });
    }

    [Test]
    public void InitialOffer_RequiresRtcpMuxAndUniqueFallbackIceCredentials()
    {
        SdpSessionDescription description = InitialOffer();
        RemoveAttribute(description.MediaDescriptions[1].Attributes, SdpAttributeNames.RtcpMux);
        AddIceCredentials(description.MediaDescriptions[0], "shared", "abcdefghijklmnopqrstuv");
        AddIceCredentials(description.MediaDescriptions[1], "shared", "abcdefghijklmnopqrstuv");

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, null);
        List<SdpValidationIssue> bundleIssues = result.Issues.Where(issue => issue.Code == SdpDiagnosticCodes.ValidationBundle).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(bundleIssues.Any(issue => issue.MediaIndex == 1 && issue.AttributeName == SdpAttributeNames.RtcpMux && issue.Detail.Contains("initial RTP BUNDLE offer")), Is.True);
            Assert.That(bundleIssues.Any(issue => issue.MediaIndex == 1 && issue.AttributeName == SdpAttributeNames.IceUsernameFragment && issue.Detail.Contains("unique ICE credentials")), Is.True);
        });
    }

    [Test]
    public void Answer_RequiresTaggedIceAndRtcpStateAndForbidsSeparateRtcp()
    {
        SdpSessionDescription offer = InitialOffer();
        AddIceCredentials(offer.MediaDescriptions[0], "offer-audio", "abcdefghijklmnopqrstuv");
        AddIceCredentials(offer.MediaDescriptions[1], "offer-video", "zyxwvutsrqponmlkjihgfe");
        SdpSessionDescription answer = EstablishedDescription();
        answer.MediaDescriptions[0].Attributes.Add(PropertyAttribute(SdpAttributeNames.RtcpMuxOnly));
        answer.MediaDescriptions[1].Attributes.Add(ValueAttribute(SdpAttributeNames.Candidate, "one 1 UDP 100 203.0.113.20 20000 typ host"));
        answer.MediaDescriptions[1].Attributes.Add(ValueAttribute(SdpAttributeNames.Rtcp, "20001"));

        SdpValidationResult result = Validate(answer, SdpNegotiationRole.Answerer, offer);
        List<SdpValidationIssue> bundleIssues = result.Issues.Where(issue => issue.Code == SdpDiagnosticCodes.ValidationBundle).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(bundleIssues.Any(issue => issue.MediaIndex == 0 && issue.AttributeName == SdpAttributeNames.IceUsernameFragment && issue.Detail.Contains("complete ICE credentials")), Is.True);
            Assert.That(bundleIssues.Any(issue => issue.MediaIndex == 1 && issue.AttributeName == SdpAttributeNames.Candidate && issue.Detail.Contains("active tagged")), Is.True);
            Assert.That(bundleIssues.Any(issue => issue.MediaIndex == 1 && issue.AttributeName == SdpAttributeNames.Rtcp && issue.Detail.Contains("must not include")), Is.True);
        });
    }

    [Test]
    public void DtlsBundle_RequiresFingerprintAndSetupOnTheTaggedTransport()
    {
        SdpSessionDescription description = EstablishedDescription();
        int index = 0;
        while (index < description.MediaDescriptions.Count)
        {
            description.MediaDescriptions[index].Protocol = SdpTransportProtocols.UdpTlsRtpSavp;
            index++;
        }

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Answerer, null);
        List<SdpValidationIssue> bundleIssues = result.Issues.Where(issue => issue.Code == SdpDiagnosticCodes.ValidationBundle).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(bundleIssues.Any(issue => issue.AttributeName == SdpAttributeNames.Fingerprint && issue.Detail.Contains("certificate fingerprint")), Is.True);
            Assert.That(bundleIssues.Any(issue => issue.AttributeName == SdpAttributeNames.Setup && issue.Detail.Contains("setup role")), Is.True);
        });
    }

    [Test]
    public void EveryBundledRtpMediaRequiresOneConsistentMidExtensionMapping()
    {
        SdpSessionDescription description = EstablishedDescription();
        RemoveAttribute(description.MediaDescriptions[1].Attributes, SdpAttributeNames.ExtMap);
        RemoveAttribute(description.MediaDescriptions[2].Attributes, SdpAttributeNames.ExtMap);
        description.MediaDescriptions[2].Attributes.Add(ExtMap(2));

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Answerer, null);
        List<SdpValidationIssue> extMapIssues = result.Issues.Where(issue => issue.Code == SdpDiagnosticCodes.ValidationBundle && issue.AttributeName == SdpAttributeNames.ExtMap).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(extMapIssues.Any(issue => issue.MediaIndex == 1 && issue.Detail.Contains("must include")), Is.True);
            Assert.That(extMapIssues.Any(issue => issue.MediaIndex == 2 && issue.Detail.Contains("consistent mapping")), Is.True);
        });
    }

    [Test]
    public void Answer_PlacesIdenticalAndTransportAttributesOnlyOnTheTag()
    {
        SdpSessionDescription description = EstablishedDescription();
        description.MediaDescriptions[1].Attributes.Add(ValueAttribute(SdpAttributeNames.Fingerprint, "sha-256 AA:BB"));
        description.MediaDescriptions[1].Attributes.Add(PropertyAttribute(SdpAttributeNames.ExtMapAllowMixed));
        description.MediaDescriptions[1].Attributes.Add(ValueAttribute(SdpAttributeNames.RtpMap, "96 opus/48000/2"));
        description.MediaDescriptions[1].Attributes.Add(PropertyAttribute("x-normal"));

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Answerer, null);
        List<SdpValidationIssue> placementIssues = result.Issues.Where(issue => issue.Code == SdpDiagnosticCodes.ValidationBundle && issue.Detail.Contains("IDENTICAL and TRANSPORT")).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(placementIssues.Select(issue => issue.AttributeName), Is.EquivalentTo(new[] { SdpAttributeNames.Fingerprint, SdpAttributeNames.ExtMapAllowMixed }));
            Assert.That(placementIssues.All(issue => issue.MediaIndex == 1), Is.True);
            Assert.That(placementIssues.Any(issue => issue.AttributeName == SdpAttributeNames.RtpMap), Is.False);
            Assert.That(placementIssues.Any(issue => issue.AttributeName == "x-normal"), Is.False);
        });
    }

    [Test]
    public void CautionCategoryAttributesProduceRegistryDrivenBundleWarnings()
    {
        SdpSessionDescription description = EstablishedDescription();
        description.Attributes.Add(ValueAttribute("3GPP-Asset-Information", "session-data"));
        description.MediaDescriptions[1].Attributes.Add(ValueAttribute("DCCP-PORT", "5000"));
        description.MediaDescriptions[1].Attributes.Add(ValueAttribute(SdpAttributeNames.Content, "slides"));
        description.MediaDescriptions[1].Attributes.Add(PropertyAttribute("x-extension"));

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Answerer, null);
        List<SdpValidationIssue> warnings = result.Issues.Where(issue => issue.Code == SdpDiagnosticCodes.ValidationBundle && issue.Severity == SdpValidationSeverity.Warning).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(warnings, Has.Count.EqualTo(2));
            Assert.That(warnings.Any(issue => issue.Scope == SdpScope.Session && issue.AttributeName == "3GPP-Asset-Information"), Is.True);
            Assert.That(warnings.Any(issue => issue.Scope == SdpScope.Media && issue.MediaIndex == 1 && issue.AttributeName == "DCCP-PORT"), Is.True);
            Assert.That(warnings.All(issue => issue.Detail.Contains("CAUTION")), Is.True);
            Assert.That(warnings.Any(issue => issue.AttributeName == SdpAttributeNames.Content), Is.False);
            Assert.That(warnings.Any(issue => issue.AttributeName == "x-extension"), Is.False);
        });
    }

    static SdpValidationResult Validate(SdpSessionDescription description, SdpNegotiationRole role, SdpSessionDescription? relatedOffer)
    {
        SdpValidationOptions options = new SdpValidationOptions
        {
            Profile = SdpValidationProfile.Bundle,
            NegotiationRole = role,
            OfferDescription = relatedOffer
        };
        return new SdpValidator().ValidateDescription(description, options);
    }

    static SdpSessionDescription InitialOffer()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Connection = Connection(SdpAddressTypes.Ip4, "192.0.2.10");
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video data"));
        description.MediaDescriptions.Add(Media("audio", 10000, SdpTransportProtocols.RtpAvp));
        description.MediaDescriptions.Add(Media("video", 10002, SdpTransportProtocols.RtpAvp));
        description.MediaDescriptions.Add(Media("data", 0, SdpTransportProtocols.RtpAvp));
        description.MediaDescriptions[0].Attributes.Add(PropertyAttribute(SdpAttributeNames.RtcpMux));
        description.MediaDescriptions[1].Attributes.Add(PropertyAttribute(SdpAttributeNames.RtcpMux));
        description.MediaDescriptions[2].Attributes.Add(PropertyAttribute(SdpAttributeNames.BundleOnly));
        int index = 0;
        while (index < description.MediaDescriptions.Count)
        {
            description.MediaDescriptions[index].Attributes.Add(ExtMap(1));
            index++;
        }
        return description;
    }

    static SdpSessionDescription EstablishedDescription()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Connection = Connection(SdpAddressTypes.Ip4, "203.0.113.20");
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video data"));
        description.MediaDescriptions.Add(Media("audio", 20000, SdpTransportProtocols.RtpAvp));
        description.MediaDescriptions.Add(Media("video", 20000, SdpTransportProtocols.RtpAvp));
        description.MediaDescriptions.Add(Media("data", 20000, SdpTransportProtocols.RtpAvp));
        description.MediaDescriptions[0].Attributes.Add(PropertyAttribute(SdpAttributeNames.RtcpMux));
        int index = 0;
        while (index < description.MediaDescriptions.Count)
        {
            description.MediaDescriptions[index].Attributes.Add(ExtMap(1));
            index++;
        }
        return description;
    }

    static SdpMediaDescription Media(string mediaId, int port, string protocol)
    {
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Audio,
            MediaKind = SdpMediaKind.Audio,
            Port = port,
            Protocol = protocol,
            Location = new SdpSourceLocation { LineNumber = 6, ColumnNumber = 1 }
        };
        media.Formats.Add("0");
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, mediaId));
        return media;
    }

    static SdpConnectionData Connection(string addressType, string address)
    {
        return new SdpConnectionData
        {
            NetworkType = SdpNetworkTypes.Internet,
            AddressType = addressType,
            Address = address,
            RawAddress = address,
            Location = new SdpSourceLocation { LineNumber = 5, ColumnNumber = 1 }
        };
    }

    static void AddIceCredentials(SdpMediaDescription media, string username, string password)
    {
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, username));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, password));
    }

    static void RemoveAttribute(List<SdpAttribute> attributes, string name)
    {
        attributes.RemoveAll(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    static SdpAttribute ExtMap(int id)
    {
        return ValueAttribute(SdpAttributeNames.ExtMap, id + " " + SdpHeaderExtensionUris.MediaId);
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = name,
            Value = value,
            RawText = name + ":" + value,
            Location = new SdpSourceLocation { LineNumber = 7, ColumnNumber = 1 }
        };
    }

    static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Property,
            Name = name,
            RawText = name,
            Location = new SdpSourceLocation { LineNumber = 7, ColumnNumber = 1 }
        };
    }
}
