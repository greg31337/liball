namespace sdp.Tests.Unit.Validation;

public class SdpBfcpValidationTests
{
    [TestCase("TCP/BFCP", false)]
    [TestCase("TCP/DTLS/BFCP", true)]
    [TestCase("TCP/TLS/BFCP", false)]
    [TestCase("UDP/BFCP", false)]
    [TestCase("UDP/TLS/BFCP", true)]
    public void CurrentBfcpTransports_AcceptACompleteClientOffer(string protocol, bool requiresDtlsSetup)
    {
        SdpSessionDescription description = ClientDescription(protocol, "c-only");
        if (requiresDtlsSetup)
        {
            description.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.Setup, "actpass"));
        }

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void ServerOffer_WithBoundedIdentifiersAndResolvableLabelsIsValid()
    {
        SdpSessionDescription description = ClientDescription(SdpTransportProtocols.TcpTlsBfcp, "c-only s-only");
        AddServerData(description, description.MediaDescriptions[0], "4294967295", "65535", "65535", "audio-label", false);

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void BfcpAttributes_AreMediaScopedAndSingletonsRetainOccurrenceIndexes()
    {
        SdpSessionDescription description = ClientDescription(SdpTransportProtocols.TcpBfcp, "c-only");
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.FloorControl, "c-only"));
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.FloorControl, "c-only"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.BfcpVersion, "2"));

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeScope && issue.Scope == SdpScope.Session), Is.True);
            Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.EqualTo(2));
            Assert.That(result.Issues.Where(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality).All(issue => issue.Path.OccurrenceIndex == 1), Is.True);
        });
    }

    [Test]
    public void BfcpTransport_RequiresApplicationMediaAndAsteriskFormat()
    {
        SdpSessionDescription description = ClientDescription(SdpTransportProtocols.UdpBfcp, "c-only");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.MediaType = SdpMediaTypes.Audio;
        media.MediaKind = SdpMediaKind.Audio;
        media.Formats.Clear();
        media.Formats.Add("0");
        media.Formats.Add("*");

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Count(issue => issue.FieldName == 'm' && issue.Code == SdpDiagnosticCodes.ValidationBfcp), Is.EqualTo(2));
            Assert.That(result.Issues.Any(issue => issue.Detail.Contains("application", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Detail.Contains("asterisk", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void UnsupportedTransportAndInapplicableAttributes_AreReported()
    {
        SdpSessionDescription description = ClientDescription("SCTP/BFCP", "c-only");

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue => issue.FieldName == 'm' && issue.Detail.Contains("current TCP or UDP", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Count(issue => issue.AttributeName == SdpAttributeNames.FloorControl || issue.AttributeName == SdpAttributeNames.BfcpVersion), Is.EqualTo(2));
            Assert.That(result.Issues.Where(issue => issue.FieldName == 'a').All(issue => issue.Detail.Contains("not applicable", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void IdentifierVersionAndRoleConstraints_AreReported()
    {
        SdpSessionDescription description = ClientDescription(SdpTransportProtocols.TcpBfcp, "c-only s-only c-s");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes[1].Value = "1 01";
        AddServerData(description, media, "4294967296", "65536", "65536", "audio-label", false);

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.FloorControl && issue.Detail.Contains("BFCP offer", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.BfcpVersion && issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.ConferenceId && issue.Detail.Contains("32-bit", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.UserId && issue.Detail.Contains("16-bit", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.FloorId && issue.Detail.Contains("16-bit", StringComparison.Ordinal)), Is.True);
        });
    }

    [TestCase("future", "floorctrl")]
    [TestCase("c-only c-only", "repeated")]
    public void InvalidFloorControlValues_AreReported(string value, string detailFragment)
    {
        SdpSessionDescription description = ClientDescription(SdpTransportProtocols.TcpBfcp, value);

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.FloorControl && issue.Detail.Contains(detailFragment, StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [TestCase("0")]
    [TestCase("3")]
    [TestCase("version-one")]
    public void UndefinedBfcpVersions_AreReported(string value)
    {
        SdpSessionDescription description = ClientDescription(SdpTransportProtocols.TcpBfcp, "c-only");
        description.MediaDescriptions[0].Attributes[1].Value = value;

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.BfcpVersion && issue.Code == SdpDiagnosticCodes.ValidationBfcp), Is.True);
    }

    [Test]
    public void VersionSpellings_AreComparedByDecimalValue()
    {
        SdpSessionDescription offer = ClientDescription(SdpTransportProtocols.TcpBfcp, "c-only");
        offer.MediaDescriptions[0].Attributes[1].Value = "01 02";
        SdpSessionDescription answer = ClientDescription(SdpTransportProtocols.TcpBfcp, "s-only");
        answer.MediaDescriptions[0].Attributes[1].Value = "02";
        AddServerData(answer, answer.MediaDescriptions[0], "10", "20", "1", "audio-label", false);

        SdpValidationResult result = Validate(answer, SdpNegotiationRole.Answerer, offer);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ServerRole_RequiresConferenceUserAndFloorIdentifiers()
    {
        SdpSessionDescription description = ClientDescription(SdpTransportProtocols.TcpBfcp, "s-only");

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Select(issue => issue.AttributeName), Does.Contain(SdpAttributeNames.ConferenceId));
            Assert.That(result.Issues.Select(issue => issue.AttributeName), Does.Contain(SdpAttributeNames.UserId));
            Assert.That(result.Issues.Select(issue => issue.AttributeName), Does.Contain(SdpAttributeNames.FloorId));
            Assert.That(result.Issues.Count(issue => issue.Path.OccurrenceIndex == -1), Is.EqualTo(3));
        });
    }

    [Test]
    public void FloorPointers_MustResolveToExactlyOneMediaLabel()
    {
        SdpSessionDescription description = ClientDescription(SdpTransportProtocols.TcpBfcp, "s-only");
        SdpMediaDescription bfcp = description.MediaDescriptions[0];
        AddServerData(description, bfcp, "10", "20", "1", "shared", false);
        bfcp.Attributes.Add(ValueAttribute(SdpAttributeNames.FloorId, "2 m-stream:missing"));
        SdpMediaDescription duplicate = ControlledMedia("video", "shared");
        description.MediaDescriptions.Add(duplicate);

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.FloorId && issue.Path.OccurrenceIndex == 0 && issue.Detail.Contains("exactly one", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.FloorId && issue.Path.OccurrenceIndex == 1 && issue.Detail.Contains("existing", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void DuplicateFloorIdentifiers_AreComparedNumerically()
    {
        SdpSessionDescription description = ClientDescription(SdpTransportProtocols.TcpBfcp, "s-only");
        SdpMediaDescription bfcp = description.MediaDescriptions[0];
        AddServerData(description, bfcp, "10", "20", "01", "audio-label", false);
        bfcp.Attributes.Add(ValueAttribute(SdpAttributeNames.FloorId, "1 mstrm:audio-label"));

        SdpValidationResult result = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality && issue.AttributeName == SdpAttributeNames.FloorId && issue.Path.OccurrenceIndex == 1), Is.True);
    }

    [TestCase("c-only", "s-only", true)]
    [TestCase("s-only", "c-only", true)]
    [TestCase("c-only s-only", "c-only", true)]
    [TestCase("c-s", "s-only", true)]
    [TestCase("c-only", "c-only", false)]
    [TestCase("s-only", "s-only", false)]
    public void AnswerFloorRole_MustBeOppositeAndOffered(string offerRole, string answerRole, bool valid)
    {
        SdpSessionDescription offer = ClientDescription(SdpTransportProtocols.TcpBfcp, offerRole);
        SdpSessionDescription answer = ClientDescription(SdpTransportProtocols.TcpBfcp, answerRole);
        AddServerDataWhenNeeded(offer, offer.MediaDescriptions[0], offerRole);
        AddServerDataWhenNeeded(answer, answer.MediaDescriptions[0], answerRole);

        SdpValidationResult result = Validate(answer, SdpNegotiationRole.Answerer, offer);

        Assert.That(result.Success, Is.EqualTo(valid));
    }

    [Test]
    public void LegacyAnswerWithoutFloorControl_DefaultsToServerRequirements()
    {
        SdpSessionDescription offer = BareDescription(SdpTransportProtocols.TcpBfcp);
        offer.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.BfcpVersion, "1"));
        SdpSessionDescription incompleteAnswer = BareDescription(SdpTransportProtocols.TcpBfcp);
        incompleteAnswer.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.BfcpVersion, "1"));
        SdpSessionDescription completeAnswer = BareDescription(SdpTransportProtocols.TcpBfcp);
        completeAnswer.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.BfcpVersion, "1"));
        AddServerData(completeAnswer, completeAnswer.MediaDescriptions[0], "10", "20", "1", "audio-label", true);

        SdpValidationResult incomplete = Validate(incompleteAnswer, SdpNegotiationRole.Answerer, offer);
        SdpValidationResult complete = Validate(completeAnswer, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.Issues.Count(issue => issue.Path.OccurrenceIndex == -1), Is.EqualTo(3));
            Assert.That(complete.Success, Is.True);
        });
    }

    [Test]
    public void AnswerVersionsAndTransport_MustBeOfferedAndUnchanged()
    {
        SdpSessionDescription offer = ClientDescription(SdpTransportProtocols.TcpBfcp, "c-only");
        offer.MediaDescriptions[0].Attributes[1].Value = "1";
        SdpSessionDescription answer = ClientDescription(SdpTransportProtocols.TcpTlsBfcp, "s-only");
        answer.MediaDescriptions[0].Attributes[1].Value = "2";
        AddServerData(answer, answer.MediaDescriptions[0], "10", "20", "1", "audio-label", false);

        SdpValidationResult result = Validate(answer, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.Any(issue => issue.FieldName == 'm' && issue.Detail.Contains("retain", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.BfcpVersion && issue.Detail.Contains("offered", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void AnswerSetupAndConnectionReuse_MustMatchOfferChoices()
    {
        SdpSessionDescription offer = ClientDescription(SdpTransportProtocols.TcpBfcp, "c-only");
        offer.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.Setup, "passive"));
        offer.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.Connection, "new"));
        SdpSessionDescription answer = ClientDescription(SdpTransportProtocols.TcpBfcp, "s-only");
        AddServerData(answer, answer.MediaDescriptions[0], "10", "20", "1", "audio-label", false);
        answer.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.Setup, "passive"));
        answer.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.Connection, "existing"));

        SdpValidationResult result = Validate(answer, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.Setup && issue.Detail.Contains("compatible", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.Connection && issue.Detail.Contains("connection:new", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void RejectedAnswer_DoesNotRequireActiveBfcpAttributes()
    {
        SdpSessionDescription offer = ClientDescription(SdpTransportProtocols.UdpBfcp, "c-only");
        SdpSessionDescription answer = BareDescription(SdpTransportProtocols.UdpBfcp);
        answer.MediaDescriptions[0].Port = 0;

        SdpValidationResult result = Validate(answer, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [TestCase(SdpValidationProfile.Bfcp, true)]
    [TestCase(SdpValidationProfile.CombinedStrict, true)]
    [TestCase(SdpValidationProfile.Rfc8866, false)]
    public void BfcpRules_AreRoutedByProfile(SdpValidationProfile profile, bool expectedIssue)
    {
        SdpSessionDescription description = BareDescription(SdpTransportProtocols.TcpBfcp);
        SdpValidationOptions options = new SdpValidationOptions { Profile = profile, NegotiationRole = SdpNegotiationRole.Offerer };

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, options);

        Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationBfcp), Is.EqualTo(expectedIssue));
    }

    static SdpSessionDescription ClientDescription(string protocol, string role)
    {
        SdpSessionDescription description = BareDescription(protocol);
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.FloorControl, role));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.BfcpVersion, "1 2"));
        return description;
    }

    static SdpSessionDescription BareDescription(string protocol)
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Application,
            MediaKind = SdpMediaKind.Application,
            Port = 50000,
            Protocol = protocol
        };
        media.Formats.Add("*");
        description.MediaDescriptions.Add(media);
        return description;
    }

    static void AddServerData(SdpSessionDescription description, SdpMediaDescription bfcp, string conferenceId, string userId, string floorId, string label, bool legacyPointer)
    {
        bfcp.Attributes.Add(ValueAttribute(SdpAttributeNames.ConferenceId, conferenceId));
        bfcp.Attributes.Add(ValueAttribute(SdpAttributeNames.UserId, userId));
        bfcp.Attributes.Add(ValueAttribute(SdpAttributeNames.FloorId, floorId + (legacyPointer ? " m-stream:" : " mstrm:") + label));
        description.MediaDescriptions.Add(ControlledMedia("audio", label));
    }

    static void AddServerDataWhenNeeded(SdpSessionDescription description, SdpMediaDescription bfcp, string role)
    {
        if (role.Contains("s-only", StringComparison.Ordinal) || role == "c-s")
        {
            AddServerData(description, bfcp, "10", "20", "1", "media-label", false);
        }
    }

    static SdpMediaDescription ControlledMedia(string mediaType, string label)
    {
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = mediaType,
            MediaKind = mediaType == "audio" ? SdpMediaKind.Audio : SdpMediaKind.Video,
            Port = 50002,
            Protocol = SdpTransportProtocols.RtpAvp
        };
        media.Formats.Add("0");
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.Label, label));
        return media;
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }

    static SdpValidationResult Validate(SdpSessionDescription description, SdpNegotiationRole role, SdpSessionDescription? offer)
    {
        SdpValidationOptions options = new SdpValidationOptions
        {
            Profile = SdpValidationProfile.Bfcp,
            NegotiationRole = role,
            OfferDescription = offer
        };
        return new SdpValidator().ValidateDescription(description, options);
    }
}
