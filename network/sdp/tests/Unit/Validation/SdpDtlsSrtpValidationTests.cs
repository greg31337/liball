namespace sdp.Tests.Unit.Validation;

public class SdpDtlsSrtpValidationTests
{
    [TestCase("active")]
    [TestCase("passive")]
    public void InitialOfferAndAnswer_UseComplementarySetupRolesAndTlsIds(string answerRole)
    {
        SdpSessionDescription offer = CreateDtlsDescription("actpass", Id('o'), "UDP/TLS/RTP/SAVPF");
        SdpSessionDescription answer = CreateDtlsDescription(answerRole, Id('a'), "UDP/TLS/RTP/SAVPF");
        SdpValidationOptions offerOptions = DtlsOptions(SdpNegotiationRole.Offerer, null);
        SdpValidationOptions answerOptions = DtlsOptions(SdpNegotiationRole.Answerer, offer);

        SdpValidationResult offerResult = new SdpValidator().ValidateDescription(offer, offerOptions);
        SdpValidationResult answerResult = new SdpValidator().ValidateDescription(answer, answerOptions);

        Assert.Multiple(() =>
        {
            Assert.That(offerResult.Success, Is.True);
            Assert.That(offerResult.Issues, Is.Empty);
            Assert.That(answerResult.Success, Is.True);
            Assert.That(answerResult.Issues, Is.Empty);
        });
    }

    [TestCase(SdpNegotiationRole.Offerer, "active")]
    [TestCase(SdpNegotiationRole.Offerer, "passive")]
    [TestCase(SdpNegotiationRole.Answerer, "actpass")]
    public void NegotiationRole_RejectsInvalidEffectiveSetupRole(SdpNegotiationRole role, string setup)
    {
        SdpSessionDescription description = CreateDtlsDescription(setup, Id('r'), "UDP/TLS/RTP/SAVPF");

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, DtlsOptions(role, null));

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationDtls));
            Assert.That(result.Issues[0].AttributeName, Is.EqualTo("setup"));
            Assert.That(result.Issues[0].Detail, Does.Contain(role == SdpNegotiationRole.Offerer ? "offer" : "answer"));
        });
    }

    [TestCase("actpass", "active", true)]
    [TestCase("actpass", "passive", true)]
    [TestCase("active", "passive", true)]
    [TestCase("passive", "active", true)]
    [TestCase("active", "active", false)]
    [TestCase("passive", "passive", false)]
    public void AnswerSetupRole_MustBeCompatibleWithOfferedRole(string offeredRole, string answeredRole, bool expectedSuccess)
    {
        SdpSessionDescription offer = CreateDtlsDescription(offeredRole, Id('o'), "UDP/TLS/RTP/SAVPF");
        SdpSessionDescription answer = CreateDtlsDescription(answeredRole, Id('a'), "UDP/TLS/RTP/SAVPF");

        SdpValidationResult result = new SdpValidator().ValidateDescription(answer, DtlsOptions(SdpNegotiationRole.Answerer, offer));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.EqualTo(expectedSuccess));
            Assert.That(result.ErrorCount, Is.EqualTo(expectedSuccess ? 0 : 1));
            if (!expectedSuccess)
            {
                Assert.That(result.Issues[0].Detail, Does.Contain("corresponding offered role"));
            }
        });
    }

    [Test]
    public void SessionSetupAndMultipleFingerprints_AreInheritedByDtlsMedia()
    {
        SdpSessionDescription description = CreateDtlsMediaOnly("UDP/TLS/RTP/SAVPF");
        description.Attributes.Add(ValueAttribute("setup", "actpass"));
        description.Attributes.Add(Fingerprint("sha-256", 32));
        description.Attributes.Add(Fingerprint("sha-1", 20));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("tls-id", Id('i')));

        SdpValidationResult inherited = new SdpValidator().ValidateDescription(description, DtlsOptions(SdpNegotiationRole.Offerer, null));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("setup", "active"));
        SdpValidationResult overridden = new SdpValidator().ValidateDescription(description, DtlsOptions(SdpNegotiationRole.Offerer, null));

        Assert.Multiple(() =>
        {
            Assert.That(inherited.Success, Is.True);
            Assert.That(inherited.Issues, Is.Empty);
            Assert.That(overridden.ErrorCount, Is.EqualTo(1));
            Assert.That(overridden.Issues[0].Detail, Does.Contain("offer"));
        });
    }

    [Test]
    public void DtlsMedia_RequiresSetupAndForbidsHoldConnection()
    {
        SdpSessionDescription description = CreateDtlsMediaOnly("UDP/TLS/RTP/SAVPF");
        description.MediaDescriptions[0].Attributes.Add(Fingerprint("sha-256", 32));

        SdpValidationResult missing = ValidateDtls(description);
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("setup", "holdconn"));
        SdpValidationResult held = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(missing.ErrorCount, Is.EqualTo(1));
            Assert.That(missing.Issues[0].Detail, Does.Contain("effective setup"));
            Assert.That(held.ErrorCount, Is.EqualTo(1));
            Assert.That(held.Issues[0].Detail, Does.Contain("holdconn"));
        });
    }

    [Test]
    public void TlsId_IsMediaOnlyAndSingleton()
    {
        SdpSessionDescription description = CreateDtlsDescription("actpass", Id('m'), "UDP/TLS/RTP/SAVPF");
        description.Attributes.Add(ValueAttribute("tls-id", Id('s')));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("TLS-ID", Id('d')));

        SdpValidationResult result = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.Count(x => x.Code == SdpDiagnosticCodes.ValidationAttributeScope), Is.EqualTo(1));
            Assert.That(result.Issues.Count(x => x.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.EqualTo(1));
            Assert.That(result.Issues.Any(x => x.Scope == SdpScope.Session && x.AttributeName == "tls-id"), Is.True);
            Assert.That(result.Issues.Any(x => x.Scope == SdpScope.Media && x.Path.OccurrenceIndex == 1), Is.True);
        });
    }

    [Test]
    public void MalformedTlsId_IsReportedThroughDtlsValidation()
    {
        SdpSessionDescription description = CreateDtlsDescription("actpass", "short", "UDP/TLS/RTP/SAVPF");

        SdpValidationResult result = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationDtls));
            Assert.That(result.Issues[0].AttributeName, Is.EqualTo("tls-id"));
            Assert.That(result.Issues[0].Detail, Does.Contain("20 through 255"));
        });
    }

    [Test]
    public void TlsId_MustBeUniqueAcrossIndependentMediaAssociations()
    {
        string sharedId = Id('x');
        SdpSessionDescription description = CreateDtlsMediaOnly("UDP/TLS/RTP/SAVPF");
        description.Attributes.Add(ValueAttribute("setup", "actpass"));
        description.Attributes.Add(Fingerprint("sha-256", 32));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("tls-id", sharedId));
        SdpMediaDescription second = DtlsMedia("UDP/TLS/RTP/SAVPF");
        SdpAttribute secondId = ValueAttribute("tls-id", sharedId);
        second.Attributes.Add(secondId);
        description.MediaDescriptions.Add(second);

        SdpValidationResult duplicate = ValidateDtls(description);
        secondId.Value = Id('y');
        SdpValidationResult unique = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.ErrorCount, Is.EqualTo(1));
            Assert.That(duplicate.Issues[0].MediaIndex, Is.EqualTo(1));
            Assert.That(duplicate.Issues[0].AttributeName, Is.EqualTo("tls-id"));
            Assert.That(duplicate.Issues[0].Detail, Does.Contain("unique"));
            Assert.That(unique.Success, Is.True);
            Assert.That(unique.Issues, Is.Empty);
        });
    }

    [Test]
    public void AnswerTlsIdPresence_MustFollowOfferSupport()
    {
        SdpSessionDescription offerWithId = CreateDtlsDescription("actpass", Id('o'), "UDP/TLS/RTP/SAVPF");
        SdpSessionDescription answerWithoutId = CreateDtlsDescription("active", string.Empty, "UDP/TLS/RTP/SAVPF");
        SdpSessionDescription offerWithoutId = CreateDtlsDescription("actpass", string.Empty, "UDP/TLS/RTP/SAVPF");
        SdpSessionDescription answerWithId = CreateDtlsDescription("passive", Id('a'), "UDP/TLS/RTP/SAVPF");
        SdpSessionDescription matchingAnswer = CreateDtlsDescription("active", Id('b'), "UDP/TLS/RTP/SAVPF");

        SdpValidationResult missing = new SdpValidator().ValidateDescription(answerWithoutId, DtlsOptions(SdpNegotiationRole.Answerer, offerWithId));
        SdpValidationResult introduced = new SdpValidator().ValidateDescription(answerWithId, DtlsOptions(SdpNegotiationRole.Answerer, offerWithoutId));
        SdpValidationResult matching = new SdpValidator().ValidateDescription(matchingAnswer, DtlsOptions(SdpNegotiationRole.Answerer, offerWithId));

        Assert.Multiple(() =>
        {
            Assert.That(missing.ErrorCount, Is.EqualTo(1));
            Assert.That(missing.Issues[0].Detail, Does.Contain("must include"));
            Assert.That(introduced.ErrorCount, Is.EqualTo(1));
            Assert.That(introduced.Issues[0].Detail, Does.Contain("must not introduce"));
            Assert.That(matching.Success, Is.True);
            Assert.That(matching.Issues, Is.Empty);
        });
    }

    [TestCase("UDP/TLS/RTP/SAVP", true)]
    [TestCase("UDP/TLS/RTP/SAVPF", true)]
    [TestCase("UDP/TLS/RTP/AVP", false)]
    [TestCase("UDP/DTLS/RTP/SAVP", false)]
    public void DtlsSrtp_RequiresARegisteredSecureTransportProfile(string protocol, bool expectedSuccess)
    {
        SdpSessionDescription description = CreateDtlsDescription("actpass", Id('p'), protocol);

        SdpValidationResult result = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.EqualTo(expectedSuccess));
            Assert.That(result.ErrorCount, Is.EqualTo(expectedSuccess ? 0 : 1));
            if (!expectedSuccess)
            {
                Assert.That(result.Issues[0].Detail, Does.Contain("DTLS-SRTP"));
            }
        });
    }

    [Test]
    public void TlsIdOnTlsConnection_RequiresExplicitConnectionSemantics()
    {
        SdpSessionDescription description = CreateDtlsDescription("passive", Id('t'), "TCP/TLS");

        SdpValidationResult missing = ValidateDtls(description);
        description.Attributes.Add(ValueAttribute("connection", "new"));
        SdpValidationResult explicitConnection = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(missing.ErrorCount, Is.EqualTo(1));
            Assert.That(missing.Issues[0].AttributeName, Is.EqualTo("connection"));
            Assert.That(missing.Issues[0].Detail, Does.Contain("tls-id"));
            Assert.That(explicitConnection.Success, Is.True);
            Assert.That(explicitConnection.Issues, Is.Empty);
        });
    }

    [Test]
    public void TlsIdOnNonSecureMedia_IsRejectedByTransportApplicability()
    {
        SdpSessionDescription description = CreateDtlsMediaOnly("RTP/AVP");
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("tls-id", Id('n')));

        SdpValidationResult result = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttributeScope));
            Assert.That(result.Issues[0].AttributeName, Is.EqualTo("tls-id"));
        });
    }

    static SdpSessionDescription CreateDtlsDescription(string setup, string tlsId, string protocol)
    {
        SdpSessionDescription description = CreateDtlsMediaOnly(protocol);
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("setup", setup));
        media.Attributes.Add(Fingerprint("sha-256", 32));
        if (tlsId.Length > 0)
        {
            media.Attributes.Add(ValueAttribute("tls-id", tlsId));
        }
        return description;
    }

    static SdpSessionDescription CreateDtlsMediaOnly(string protocol)
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(DtlsMedia(protocol));
        return description;
    }

    static SdpMediaDescription DtlsMedia(string protocol)
    {
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = "audio",
            MediaKind = SdpMediaKind.Audio,
            Port = 5000,
            Protocol = protocol
        };
        media.Formats.Add("111");
        return media;
    }

    static SdpAttribute Fingerprint(string algorithm, int byteCount)
    {
        return ValueAttribute("fingerprint", algorithm + " " + string.Join(":", Enumerable.Repeat("AA", byteCount)));
    }

    static string Id(char character)
    {
        return new string(character, 20);
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }

    static SdpValidationOptions DtlsOptions(SdpNegotiationRole role, SdpSessionDescription? offer)
    {
        return new SdpValidationOptions
        {
            Profile = SdpValidationProfile.TlsDtls,
            NegotiationRole = role,
            OfferDescription = offer
        };
    }

    static SdpValidationResult ValidateDtls(SdpSessionDescription description)
    {
        return new SdpValidator().ValidateDescription(description, DtlsOptions(SdpNegotiationRole.Unknown, null));
    }
}
