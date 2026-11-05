namespace sdp.Tests.Conformance.Rfc8122;

public class SdpTlsDtlsRfcConformanceTests
{
    const string Sha256Example = "12:DF:3E:5D:49:6B:19:E5:7C:AB:4A:AD:B9:B1:3F:82:18:3B:54:02:12:DF:3E:5D:49:6B:19:E5:7C:AB:4A:AD";
    const string Sha1Example = "4A:AD:B9:B1:3F:82:18:3B:54:02:12:DF:3E:5D:49:6B:19:E5:7C:AB";
    const string RfcTlsId = "abc3de65cddef001be82";

    [TestCase(false)]
    [TestCase(true)]
    public void Rfc8122AndRfc8842TlsExamples_ParseValidateAndRoundTripSemantically(bool includeTlsId)
    {
        string tlsIdLine = includeTlsId ? "a=tls-id:" + RfcTlsId + "\r\n" : string.Empty;
        string text = "v=0\r\n" +
            "o=- 0 0 IN IP4 192.0.2.1\r\n" +
            "s=-\r\n" +
            "t=0 0\r\n" +
            "m=image 54111 TCP/TLS t38\r\n" +
            "c=IN IP4 192.0.2.2\r\n" +
            tlsIdLine +
            "a=setup:passive\r\n" +
            "a=connection:new\r\n" +
            "a=fingerprint:SHA-256 " + Sha256Example + "\r\n" +
            "a=fingerprint:SHA-1 " + Sha1Example + "\r\n";
        SdpFixtureRoundTripAssert.AssertCanonical(text, new SdpParseOptions());
        ISdpParser parser = new SdpParser();
        ISdpDtlsService service = new SdpDtlsService();

        SdpParseResult parsed = parser.ParseText(text, new SdpParseOptions());
        SdpValidationResult validated = Validate(parsed.Description!, SdpNegotiationRole.Unknown, null);
        SdpDtlsDescription resolved = service.ResolveMediaDtls(parsed.Description!, 0);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());
        SdpParseResult reparsed = parser.ParseText(written.Text, new SdpParseOptions());
        SdpComparisonResult compared = CompareSemantically(parsed.Description!, reparsed.Description!);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Diagnostics, Is.Empty);
            Assert.That(validated.Success, Is.True);
            Assert.That(validated.Issues, Is.Empty);
            Assert.That(resolved.SetupRole, Is.EqualTo(SdpDtlsSetupRole.Passive));
            Assert.That(resolved.ConnectionRole, Is.EqualTo("new"));
            Assert.That(resolved.TlsId, Is.EqualTo(includeTlsId ? RfcTlsId : string.Empty));
            Assert.That(resolved.Fingerprints.Select(x => x.Algorithm), Is.EqualTo(new[] { "SHA-256", "SHA-1" }));
            Assert.That(resolved.Fingerprints.Select(x => x.Digest.Count), Is.EqualTo(new[] { 32, 20 }));
            Assert.That(written.Success, Is.True);
            Assert.That(written.Text, Is.EqualTo(text));
            Assert.That(reparsed.Success, Is.True);
            Assert.That(reparsed.Diagnostics, Is.Empty);
            Assert.That(compared.Equal, Is.True, DifferenceText(compared));
        });
    }

    [Test]
    public void Rfc8122SessionLevelTlsAttributes_RoundTripWithEffectiveMediaSemantics()
    {
        string text = "v=0\r\n" +
            "o=- 0 0 IN IP4 192.0.2.1\r\n" +
            "s=-\r\n" +
            "t=0 0\r\n" +
            "a=setup:passive\r\n" +
            "a=connection:new\r\n" +
            "a=fingerprint:SHA-256 " + Sha256Example + "\r\n" +
            "m=image 54111 TCP/TLS t38\r\n" +
            "c=IN IP4 192.0.2.2\r\n";
        SdpFixtureRoundTripAssert.AssertCanonical(text, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(text, new SdpParseOptions());
        SdpValidationResult validated = Validate(parsed.Description!, SdpNegotiationRole.Unknown, null);
        SdpDtlsDescription resolved = new SdpDtlsService().ResolveMediaDtls(parsed.Description!, 0);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());
        SdpParseResult reparsed = new SdpParser().ParseText(written.Text, new SdpParseOptions());
        SdpComparisonResult compared = CompareSemantically(parsed.Description!, reparsed.Description!);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(validated.Success, Is.True);
            Assert.That(validated.Issues, Is.Empty);
            Assert.That(resolved.SetupRole, Is.EqualTo(SdpDtlsSetupRole.Passive));
            Assert.That(resolved.ConnectionRole, Is.EqualTo("new"));
            Assert.That(resolved.Fingerprints, Has.Count.EqualTo(1));
            Assert.That(resolved.Fingerprints[0].Digest, Has.Count.EqualTo(32));
            Assert.That(written.Success, Is.True);
            Assert.That(written.Text, Is.EqualTo(text));
            Assert.That(reparsed.Success, Is.True);
            Assert.That(compared.Equal, Is.True, DifferenceText(compared));
        });
    }

    [TestCase("TCP/TLS", "a=setup:x-role\r\na=connection:new\r\na=fingerprint:SHA-256 " + Sha256Example + "\r\n", "setup", "registered")]
    [TestCase("TCP/TLS", "a=setup:passive\r\na=connection:x-reuse\r\na=fingerprint:SHA-256 " + Sha256Example + "\r\n", "connection", "registered")]
    [TestCase("TCP/TLS", "a=setup:passive\r\na=connection:new\r\na=fingerprint:MD2 00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF\r\n", "fingerprint", "prohibits")]
    [TestCase("TCP/TLS", "a=setup:passive\r\na=connection:new\r\na=fingerprint:MD5 00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF\r\n", "fingerprint", "prohibits")]
    [TestCase("TCP/TLS", "a=setup:passive\r\na=connection:new\r\na=fingerprint:SHA-256 00\r\n", "fingerprint", "length")]
    [TestCase("TCP/TLS", "a=setup:passive\r\na=connection:new\r\n", "fingerprint", "does not have")]
    [TestCase("TCP", "a=setup:passive\r\na=connection:new\r\na=fingerprint:SHA-256 " + Sha256Example + "\r\n", "fingerprint", "requires")]
    [TestCase("UDP/TLS/RTP/SAVPF", "a=setup:actpass\r\na=connection:existing\r\na=fingerprint:SHA-256 " + Sha256Example + "\r\n", "connection", "reuse")]
    public void InvalidTlsSemanticFixture_RoundTripsAndRetainsProfileFailure(string protocol, string attributeLines, string expectedAttribute, string detailFragment)
    {
        string text = "v=0\r\n" +
            "o=- 0 0 IN IP4 192.0.2.1\r\n" +
            "s=-\r\n" +
            "t=0 0\r\n" +
            "m=image 54111 " + protocol + " t38\r\n" +
            "c=IN IP4 192.0.2.2\r\n" +
            attributeLines;
        SdpFixtureRoundTripAssert.AssertCanonical(text, new SdpParseOptions());
        SdpParseResult parsed = new SdpParser().ParseText(text, new SdpParseOptions());
        SdpValidationResult initialValidation = Validate(parsed.Description!, SdpNegotiationRole.Unknown, null);
        SdpWriteResult written = new SdpWriter().WriteText(parsed.Description!, new SdpWriteOptions());
        SdpParseResult reparsed = new SdpParser().ParseText(written.Text, new SdpParseOptions());
        SdpValidationResult roundTripValidation = Validate(reparsed.Description!, SdpNegotiationRole.Unknown, null);
        SdpComparisonResult compared = CompareSemantically(parsed.Description!, reparsed.Description!);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Diagnostics, Is.Empty);
            Assert.That(initialValidation.ErrorCount, Is.EqualTo(1), ValidationText(initialValidation));
            Assert.That(initialValidation.Issues[0].AttributeName, Is.EqualTo(expectedAttribute));
            Assert.That(initialValidation.Issues[0].Detail, Does.Contain(detailFragment));
            Assert.That(written.Success, Is.True);
            Assert.That(written.Text, Is.EqualTo(text));
            Assert.That(reparsed.Success, Is.True);
            Assert.That(roundTripValidation.ErrorCount, Is.EqualTo(1), ValidationText(roundTripValidation));
            Assert.That(roundTripValidation.Issues[0].AttributeName, Is.EqualTo(expectedAttribute));
            Assert.That(roundTripValidation.Issues[0].Detail, Does.Contain(detailFragment));
            Assert.That(compared.Equal, Is.True, DifferenceText(compared));
        });
    }

    [TestCase("active")]
    [TestCase("passive")]
    public void Rfc5763DtlsSrtpOfferAnswerExchange_UsesNegotiatedRoles(string answerRole)
    {
        SdpSessionDescription offer = DtlsDescription("actpass", Id('o'));
        SdpSessionDescription answer = DtlsDescription(answerRole, Id('a'));

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

    [TestCase("sha-256 00", true)]
    [TestCase("sha-256 00:FF", true)]
    [TestCase("sha-256 0", false)]
    [TestCase("sha-256 000", false)]
    [TestCase("sha-256 :00", false)]
    [TestCase("sha-256 00:", false)]
    [TestCase("sha-256 00::FF", false)]
    [TestCase("sha-256 00-FF", false)]
    [TestCase("sha-256 0G", false)]
    [TestCase("sha-256 ff", false)]
    public void Rfc8122FingerprintOctetGrammar_CoversEveryDelimiterAndHexBoundary(string value, bool expectedSuccess)
    {
        SdpAttribute source = ValueAttribute("fingerprint", value);

        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(source);

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
    }

    [TestCase("sha-1", 20)]
    [TestCase("sha-224", 28)]
    [TestCase("sha-256", 32)]
    [TestCase("sha-384", 48)]
    [TestCase("sha-512", 64)]
    public void Rfc8122RegisteredAlgorithms_EnforceExactDigestLengths(string algorithm, int expectedLength)
    {
        SdpSessionDescription exact = TlsDescription(Fingerprint(algorithm, 0x11, expectedLength));
        SdpSessionDescription shortDigest = TlsDescription(Fingerprint(algorithm, 0x11, expectedLength - 1));
        SdpSessionDescription longDigest = TlsDescription(Fingerprint(algorithm, 0x11, expectedLength + 1));

        SdpValidationResult exactResult = Validate(exact, SdpNegotiationRole.Unknown, null);
        SdpValidationResult shortResult = Validate(shortDigest, SdpNegotiationRole.Unknown, null);
        SdpValidationResult longResult = Validate(longDigest, SdpNegotiationRole.Unknown, null);

        Assert.Multiple(() =>
        {
            Assert.That(exactResult.Success, Is.True);
            Assert.That(shortResult.ErrorCount, Is.EqualTo(1));
            Assert.That(longResult.ErrorCount, Is.EqualTo(1));
            Assert.That(shortResult.Issues[0].AttributeName, Is.EqualTo("fingerprint"));
            Assert.That(longResult.Issues[0].AttributeName, Is.EqualTo("fingerprint"));
        });
    }

    [TestCase("md2")]
    [TestCase("MD5")]
    public void Rfc8122ProhibitedAlgorithms_AreDecodedButNeverAcceptedOrSelected(string algorithm)
    {
        SdpSessionDescription description = TlsDescription(Fingerprint(algorithm, 0x22, 16));
        SdpDtlsDescription resolved = new SdpDtlsService().ResolveMediaDtls(description, 0);

        SdpValidationResult validated = Validate(description, SdpNegotiationRole.Unknown, null);
        SdpFingerprintAttribute? selected = new SdpDtlsService().SelectPreferredFingerprint(resolved, new List<string> { algorithm });

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Fingerprints, Has.Count.EqualTo(1));
            Assert.That(validated.ErrorCount, Is.EqualTo(1));
            Assert.That(validated.Issues[0].Detail, Does.Contain("prohibits"));
            Assert.That(selected, Is.Null);
        });
    }

    [TestCase(19, false)]
    [TestCase(20, true)]
    [TestCase(255, true)]
    [TestCase(256, false)]
    public void Rfc8842TlsIdLengthBoundaries_AreExact(int length, bool expectedSuccess)
    {
        string id = new string('A', length);
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(ValueAttribute("tls-id", id));
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(new SdpTlsIdAttribute { Id = id });

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.EqualTo(expectedSuccess));
            Assert.That(encoded.Success, Is.EqualTo(expectedSuccess));
        });
    }

    [TestCase("AZaz09+/-_AZaz09+/-_", true)]
    [TestCase("abc3de65cddef001be8=", false)]
    [TestCase("abc3de65cddef001be8.", false)]
    [TestCase("abc3de65cddef001be8 ", false)]
    [TestCase("abc3de65cddef001be8é", false)]
    public void Rfc8842TlsIdAlphabet_IsExact(string id, bool expectedSuccess)
    {
        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(ValueAttribute("tls-id", id));

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
    }

    [TestCase("actpass", "active", true)]
    [TestCase("actpass", "passive", true)]
    [TestCase("actpass", "actpass", false)]
    [TestCase("actpass", "holdconn", false)]
    [TestCase("active", "active", false)]
    [TestCase("active", "passive", true)]
    [TestCase("active", "actpass", false)]
    [TestCase("active", "holdconn", false)]
    [TestCase("passive", "active", true)]
    [TestCase("passive", "passive", false)]
    [TestCase("passive", "actpass", false)]
    [TestCase("passive", "holdconn", false)]
    [TestCase("holdconn", "active", false)]
    [TestCase("holdconn", "passive", false)]
    [TestCase("holdconn", "actpass", false)]
    [TestCase("holdconn", "holdconn", false)]
    public void Rfc5763AndRfc8842SetupRolePairMatrix_IsComplete(string offeredRole, string answeredRole, bool expectedSuccess)
    {
        SdpSessionDescription offer = DtlsDescription(offeredRole, Id('o'));
        SdpSessionDescription answer = DtlsDescription(answeredRole, Id('a'));

        SdpValidationResult result = Validate(answer, SdpNegotiationRole.Answerer, offer);

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
    }

    [Test]
    public void Rfc8842RoleMarkedOffer_RequiresTlsIdWhileLegacyInputCanOmitIt()
    {
        SdpSessionDescription description = DtlsDescription("actpass", string.Empty);

        SdpValidationResult localOffer = Validate(description, SdpNegotiationRole.Offerer, null);
        SdpValidationResult legacyPeerInput = Validate(description, SdpNegotiationRole.Unknown, null);

        Assert.Multiple(() =>
        {
            Assert.That(localOffer.ErrorCount, Is.EqualTo(1));
            Assert.That(localOffer.Issues[0].AttributeName, Is.EqualTo("tls-id"));
            Assert.That(localOffer.Issues[0].Detail, Does.Contain("offer"));
            Assert.That(legacyPeerInput.Success, Is.True);
        });
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void SessionAndMediaInheritance_CoversEverySetupAndFingerprintCombination(bool mediaSetup, bool mediaFingerprint)
    {
        SdpSessionDescription description = EmptyDtlsDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        description.Attributes.Add(ValueAttribute("setup", "actpass"));
        description.Attributes.Add(Fingerprint("sha-256", 0x11, 32));
        media.Attributes.Add(ValueAttribute("tls-id", Id('i')));
        if (mediaSetup)
        {
            media.Attributes.Add(ValueAttribute("setup", "actpass"));
        }
        if (mediaFingerprint)
        {
            media.Attributes.Add(Fingerprint("sha-384", 0x22, 48));
        }

        SdpValidationResult validated = Validate(description, SdpNegotiationRole.Offerer, null);
        SdpDtlsDescription resolved = new SdpDtlsService().ResolveMediaDtls(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(validated.Success, Is.True);
            Assert.That(resolved.SetupRole, Is.EqualTo(SdpDtlsSetupRole.ActPass));
            Assert.That(resolved.Fingerprints, Has.Count.EqualTo(1));
            Assert.That(resolved.Fingerprints[0].Algorithm, Is.EqualTo(mediaFingerprint ? "sha-384" : "sha-256"));
        });
    }

    [Test]
    public void MultipleFingerprints_RetainAllCertificatesAndSelectPreferredHashSet()
    {
        SdpSessionDescription description = EmptyDtlsDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("setup", "actpass"));
        media.Attributes.Add(ValueAttribute("tls-id", Id('f')));
        media.Attributes.Add(Fingerprint("sha-1", 0x10, 20));
        media.Attributes.Add(Fingerprint("sha-384", 0x20, 48));
        media.Attributes.Add(Fingerprint("sha-384", 0x30, 48));
        media.Attributes.Add(Fingerprint("sha-256", 0x40, 32));
        ISdpDtlsService service = new SdpDtlsService();

        SdpDtlsDescription resolved = service.ResolveMediaDtls(description, 0);
        SdpFingerprintAttribute? selected = service.SelectPreferredFingerprint(resolved, new List<string> { "sha-512", "SHA-384", "sha-256" });
        SdpValidationResult validated = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(validated.Success, Is.True);
            Assert.That(resolved.Fingerprints, Has.Count.EqualTo(4));
            Assert.That(selected, Is.SameAs(resolved.Fingerprints[1]));
            Assert.That(selected!.Digest[0], Is.EqualTo(0x20));
        });
    }

    [Test]
    public void Rfc8842TlsConnectionReuse_RequiresConnectionAlongsideTlsId()
    {
        SdpSessionDescription description = TlsDescription(Fingerprint("sha-256", 0x11, 32));
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.RemoveAll(x => x.Name == "connection");
        media.Attributes.Add(ValueAttribute("tls-id", RfcTlsId));

        SdpValidationResult missing = Validate(description, SdpNegotiationRole.Unknown, null);
        media.Attributes.Add(ValueAttribute("connection", "existing"));
        SdpValidationResult existing = Validate(description, SdpNegotiationRole.Unknown, null);
        media.Attributes[^1].Value = "new";
        SdpValidationResult fresh = Validate(description, SdpNegotiationRole.Unknown, null);

        Assert.Multiple(() =>
        {
            Assert.That(missing.ErrorCount, Is.EqualTo(1));
            Assert.That(missing.Issues[0].AttributeName, Is.EqualTo("connection"));
            Assert.That(existing.Success, Is.True);
            Assert.That(fresh.Success, Is.True);
        });
    }

    [Test]
    public void BundleTag_SuppliesDtlsStateAndConflictsRemainRedacted()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute("group", "BUNDLE audio video"));
        SdpMediaDescription owner = DtlsMedia("audio");
        owner.Attributes.Add(ValueAttribute("setup", "actpass"));
        owner.Attributes.Add(ValueAttribute("tls-id", Id('b')));
        owner.Attributes.Add(Fingerprint("sha-256", 0x11, 32));
        SdpMediaDescription member = DtlsMedia("video");
        description.MediaDescriptions.Add(owner);
        description.MediaDescriptions.Add(member);
        ISdpDtlsService service = new SdpDtlsService();

        SdpValidationResult inherited = Validate(description, SdpNegotiationRole.Offerer, null);
        SdpDtlsDescription effective = service.ResolveEffectiveDtls(description, 1);
        member.Attributes.Add(Fingerprint("sha-256", 0x99, 32));
        SdpValidationResult conflicting = Validate(description, SdpNegotiationRole.Offerer, null);

        Assert.Multiple(() =>
        {
            Assert.That(inherited.Success, Is.True);
            Assert.That(effective.TlsId, Is.EqualTo(Id('b')));
            Assert.That(effective.Fingerprints[0].Digest[0], Is.EqualTo(0x11));
            Assert.That(conflicting.ErrorCount, Is.EqualTo(1));
            Assert.That(conflicting.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationBundle));
            Assert.That(conflicting.Issues[0].Detail, Does.Not.Contain("99:99"));
        });
    }

    [Test]
    public void MalformedAndMutationFailures_DoNotExposeFingerprintOrKeyContents()
    {
        string fingerprintSecret = "DE:AD:BE:EF:PRIVATE-FINGERPRINT";
        SdpAttribute fingerprint = ValueAttribute("fingerprint", "sha-256 " + fingerprintSecret);
        SdpAttributeDecodeResult fingerprintResult = new SdpAttributeCodec().DecodeAttribute(fingerprint);
        string keySecret = "inline:private-key-material;";
        SdpAttribute crypto = ValueAttribute("crypto", "1 AES_CM_128_HMAC_SHA1_80 " + keySecret);
        SdpAttributeDecodeResult cryptoResult = new SdpAttributeCodec().DecodeAttribute(crypto);
        SdpSessionDescription description = EmptyDtlsDescription();
        SdpFingerprintAttribute invalidReplacement = new SdpFingerprintAttribute
        {
            Algorithm = "bad/algorithm",
            Fingerprint = fingerprintSecret
        };
        invalidReplacement.Digest.Add(0xDE);
        ISdpDtlsService service = new SdpDtlsService();

        ArgumentException replacementError = Assert.Throws<ArgumentException>(() => service.ReplaceFingerprints(description, 0, new List<SdpFingerprintAttribute> { invalidReplacement }))!;

        Assert.Multiple(() =>
        {
            Assert.That(fingerprintResult.Success, Is.False);
            Assert.That(fingerprintResult.Diagnostics[0].Detail, Does.Not.Contain(fingerprintSecret));
            Assert.That(cryptoResult.Success, Is.False);
            Assert.That(cryptoResult.Diagnostics[0].Detail, Does.Not.Contain(keySecret));
            Assert.That(replacementError.Message, Does.Not.Contain(fingerprintSecret));
            Assert.That(description.MediaDescriptions[0].Attributes, Is.Empty);
        });
    }

    static SdpSessionDescription TlsDescription(SdpAttribute fingerprint)
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = "image",
            MediaKind = SdpMediaKind.Unknown,
            Port = 54111,
            Protocol = "TCP/TLS"
        };
        media.Formats.Add("t38");
        media.Attributes.Add(ValueAttribute("setup", "passive"));
        media.Attributes.Add(ValueAttribute("connection", "new"));
        media.Attributes.Add(fingerprint);
        description.MediaDescriptions.Add(media);
        return description;
    }

    static SdpSessionDescription DtlsDescription(string setupRole, string tlsId)
    {
        SdpSessionDescription description = EmptyDtlsDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("setup", setupRole));
        media.Attributes.Add(Fingerprint("sha-256", 0x11, 32));
        if (tlsId.Length > 0)
        {
            media.Attributes.Add(ValueAttribute("tls-id", tlsId));
        }
        return description;
    }

    static SdpSessionDescription EmptyDtlsDescription()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(DtlsMedia(string.Empty));
        return description;
    }

    static SdpMediaDescription DtlsMedia(string mediaId)
    {
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = "audio",
            MediaKind = SdpMediaKind.Audio,
            Port = 5000,
            Protocol = "UDP/TLS/RTP/SAVPF"
        };
        media.Formats.Add("111");
        if (mediaId.Length > 0)
        {
            media.Attributes.Add(ValueAttribute("mid", mediaId));
        }
        return media;
    }

    static SdpAttribute Fingerprint(string algorithm, byte value, int count)
    {
        string digest = string.Join(":", Enumerable.Repeat(value.ToString("X2"), count));
        return ValueAttribute("fingerprint", algorithm + " " + digest);
    }

    static string Id(char character)
    {
        return new string(character, 20);
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }

    static SdpValidationResult Validate(SdpSessionDescription description, SdpNegotiationRole role, SdpSessionDescription? offer)
    {
        SdpValidationOptions options = new SdpValidationOptions
        {
            Profile = SdpValidationProfile.TlsDtls,
            NegotiationRole = role,
            OfferDescription = offer
        };
        return new SdpValidator().ValidateDescription(description, options);
    }

    static SdpComparisonResult CompareSemantically(SdpSessionDescription first, SdpSessionDescription second)
    {
        return new SdpComparer().CompareDescriptions(first, second, new SdpComparisonOptions
        {
            CompareSemanticValues = true,
            IgnoreInsignificantOrdering = true
        });
    }

    static string DifferenceText(SdpComparisonResult result)
    {
        return string.Join("|", result.Differences.Select(difference => difference.Path.AttributeName + ":" + difference.Detail));
    }

    static string ValidationText(SdpValidationResult result)
    {
        return string.Join("|", result.Issues.Select(issue => issue.AttributeName + ":" + issue.Detail));
    }
}
