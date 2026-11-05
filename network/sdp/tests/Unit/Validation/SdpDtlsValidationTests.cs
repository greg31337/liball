namespace sdp.Tests.Unit.Validation;

public class SdpDtlsValidationTests
{
    [Test]
    public void Rfc8122TlsDescription_WithMultipleFingerprintsIsValid()
    {
        SdpSessionDescription description = CreateSecureDescription("TCP/TLS");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("setup", "passive"));
        media.Attributes.Add(ValueAttribute("connection", "new"));
        media.Attributes.Add(Fingerprint("SHA-256", 32));
        media.Attributes.Add(Fingerprint("SHA-1", 20));

        SdpValidationResult result = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.ErrorCount, Is.Zero);
        });
    }

    [TestCase("sha-1", 20)]
    [TestCase("sha-224", 28)]
    [TestCase("sha-256", 32)]
    [TestCase("sha-384", 48)]
    [TestCase("sha-512", 64)]
    public void RegisteredFingerprintAlgorithms_RequireTheirDigestLength(string algorithm, int length)
    {
        SdpSessionDescription validDescription = CreateSecureDescription("TCP/TLS");
        validDescription.MediaDescriptions[0].Attributes.Add(Fingerprint(algorithm, length));
        SdpSessionDescription invalidDescription = CreateSecureDescription("TCP/TLS");
        invalidDescription.MediaDescriptions[0].Attributes.Add(Fingerprint(algorithm, length - 1));

        SdpValidationResult valid = ValidateDtls(validDescription);
        SdpValidationResult invalid = ValidateDtls(invalidDescription);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Issues, Is.Empty);
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.ErrorCount, Is.EqualTo(1));
            Assert.That(invalid.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationDtls));
            Assert.That(invalid.Issues[0].Detail, Does.Contain("length"));
        });
    }

    [Test]
    public void FingerprintSyntaxAndProhibitedAlgorithms_AreReported()
    {
        SdpSessionDescription description = CreateSecureDescription("TCP/TLS");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("fingerprint", "sha-256 aa:BB"));
        media.Attributes.Add(Fingerprint("md5", 16));
        media.Attributes.Add(Fingerprint("md2", 16));

        SdpValidationResult result = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCount, Is.EqualTo(3));
            Assert.That(result.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationDtls), Is.True);
            Assert.That(result.Issues.Count(x => x.Detail.Contains("prohibits", StringComparison.Ordinal)), Is.EqualTo(2));
            Assert.That(result.Issues.Any(x => x.Detail.Contains("uppercase hexadecimal", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void FutureFingerprintAlgorithm_IsRetainedWithoutAssumingDigestLength()
    {
        SdpSessionDescription description = CreateSecureDescription("TCP/TLS");
        description.MediaDescriptions[0].Attributes.Add(Fingerprint("x-future-hash", 3));

        SdpValidationResult result = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void SetupAndConnection_AreSingletonsAtSessionAndMediaScope()
    {
        SdpSessionDescription description = CreateSecureDescription("TCP/TLS");
        description.Attributes.Add(ValueAttribute("setup", "actpass"));
        description.Attributes.Add(ValueAttribute("SETUP", "active"));
        description.Attributes.Add(ValueAttribute("connection", "new"));
        description.Attributes.Add(ValueAttribute("CONNECTION", "existing"));
        description.Attributes.Add(Fingerprint("sha-256", 32));
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("setup", "passive"));
        media.Attributes.Add(ValueAttribute("Setup", "active"));
        media.Attributes.Add(ValueAttribute("connection", "existing"));
        media.Attributes.Add(ValueAttribute("Connection", "new"));

        SdpValidationResult result = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCount, Is.EqualTo(4));
            Assert.That(result.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.True);
            Assert.That(result.Issues.Select(x => x.Path.OccurrenceIndex), Is.EqualTo(new[] { 1, 1, 1, 1 }));
            Assert.That(result.Issues.Count(x => x.Scope == SdpScope.Session), Is.EqualTo(2));
            Assert.That(result.Issues.Count(x => x.Scope == SdpScope.Media && x.MediaIndex == 0), Is.EqualTo(2));
        });
    }

    [Test]
    public void SessionLevelAttributes_AreInheritedByApplicableTlsMedia()
    {
        SdpSessionDescription description = CreateSecureDescription("TCP/TLS");
        description.Attributes.Add(ValueAttribute("setup", "passive"));
        description.Attributes.Add(ValueAttribute("connection", "existing"));
        description.Attributes.Add(Fingerprint("sha-256", 32));

        SdpValidationResult result = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void AttributesWithoutAnApplicableTransport_ReportTheirScope()
    {
        SdpSessionDescription description = CreateSecureDescription("RTP/AVP");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("setup", "active"));
        media.Attributes.Add(ValueAttribute("connection", "existing"));
        media.Attributes.Add(Fingerprint("sha-256", 32));

        SdpValidationResult result = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCount, Is.EqualTo(3));
            Assert.That(result.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationAttributeScope), Is.True);
            Assert.That(result.Issues.All(x => x.Scope == SdpScope.Media && x.MediaIndex == 0), Is.True);
            Assert.That(result.Issues.Select(x => x.AttributeName), Is.EqualTo(new[] { "setup", "connection", "fingerprint" }));
        });
    }

    [Test]
    public void SessionAttributes_RequireAtLeastOneApplicableTransport()
    {
        SdpSessionDescription description = CreateSecureDescription("RTP/AVP");
        description.Attributes.Add(ValueAttribute("setup", "active"));
        description.Attributes.Add(ValueAttribute("connection", "new"));
        description.Attributes.Add(Fingerprint("sha-256", 32));

        SdpValidationResult result = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(3));
            Assert.That(result.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationAttributeScope), Is.True);
            Assert.That(result.Issues.All(x => x.Scope == SdpScope.Session && x.MediaIndex == -1), Is.True);
        });
    }

    [Test]
    public void RegisteredSetupAndConnectionValues_AreRequiredByTheProfile()
    {
        SdpSessionDescription description = CreateSecureDescription("TCP/TLS");
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("setup", "x-role"));
        media.Attributes.Add(ValueAttribute("connection", "x-reuse"));
        media.Attributes.Add(Fingerprint("sha-256", 32));

        SdpValidationResult result = ValidateDtls(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationDtls), Is.True);
            Assert.That(result.Issues.Select(x => x.AttributeName), Is.EqualTo(new[] { "setup", "connection" }));
        });
    }

    [Test]
    public void TlsMedia_RequiresAnEffectiveFingerprintUnlessRejected()
    {
        SdpSessionDescription active = CreateSecureDescription("TCP/TLS");
        SdpSessionDescription rejected = CreateSecureDescription("TCP/TLS");
        rejected.MediaDescriptions[0].Port = 0;

        SdpValidationResult activeResult = ValidateDtls(active);
        SdpValidationResult rejectedResult = ValidateDtls(rejected);

        Assert.Multiple(() =>
        {
            Assert.That(activeResult.ErrorCount, Is.EqualTo(1));
            Assert.That(activeResult.Issues[0].AttributeName, Is.EqualTo("fingerprint"));
            Assert.That(activeResult.Issues[0].Path.OccurrenceIndex, Is.EqualTo(-1));
            Assert.That(rejectedResult.Success, Is.True);
            Assert.That(rejectedResult.Issues, Is.Empty);
        });
    }

    [Test]
    public void ConnectionReuse_IsValidForTcpTlsButNotUdpDtls()
    {
        SdpSessionDescription tcp = CreateSecureDescription("TCP/TLS");
        tcp.MediaDescriptions[0].Attributes.Add(ValueAttribute("connection", "existing"));
        tcp.MediaDescriptions[0].Attributes.Add(Fingerprint("sha-256", 32));
        SdpSessionDescription udp = CreateSecureDescription("UDP/TLS/RTP/SAVPF");
        udp.MediaDescriptions[0].Attributes.Add(ValueAttribute("setup", "actpass"));
        udp.MediaDescriptions[0].Attributes.Add(ValueAttribute("connection", "existing"));
        udp.MediaDescriptions[0].Attributes.Add(Fingerprint("sha-256", 32));

        SdpValidationResult tcpResult = ValidateDtls(tcp);
        SdpValidationResult udpResult = ValidateDtls(udp);

        Assert.Multiple(() =>
        {
            Assert.That(tcpResult.Success, Is.True);
            Assert.That(tcpResult.Issues, Is.Empty);
            Assert.That(udpResult.ErrorCount, Is.EqualTo(1));
            Assert.That(udpResult.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttributeScope));
            Assert.That(udpResult.Issues[0].AttributeName, Is.EqualTo("connection"));
            Assert.That(udpResult.Issues[0].Detail, Does.Contain("reuse"));
        });
    }

    [TestCase(SdpValidationProfile.TlsDtls)]
    [TestCase(SdpValidationProfile.WebRtc)]
    [TestCase(SdpValidationProfile.CombinedStrict)]
    public void DtlsProfiles_ApplyValidationRules(SdpValidationProfile profile)
    {
        SdpSessionDescription description = CreateSecureDescription("TCP/TLS");
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("setup", "invalid-role"));
        description.MediaDescriptions[0].Attributes.Add(Fingerprint("sha-256", 32));

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = profile });

        Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationDtls), Is.EqualTo(1));
    }

    [Test]
    public void NonDtlsProfiles_DoNotApplyTlsRules()
    {
        SdpSessionDescription description = CreateSecureDescription("TCP/TLS");
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("fingerprint", "malformed"));

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.Rfc8866 });

        Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationDtls), Is.False);
    }

    static SdpSessionDescription CreateSecureDescription(string protocol)
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = "image",
            Port = 54111,
            Protocol = protocol
        };
        media.Formats.Add("t38");
        description.MediaDescriptions.Add(media);
        return description;
    }

    static SdpAttribute Fingerprint(string algorithm, int byteCount)
    {
        string digest = string.Join(":", Enumerable.Repeat("AA", byteCount));
        return ValueAttribute("fingerprint", algorithm + " " + digest);
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }

    static SdpValidationResult ValidateDtls(SdpSessionDescription description)
    {
        SdpValidationOptions options = new SdpValidationOptions { Profile = SdpValidationProfile.TlsDtls };
        return new SdpValidator().ValidateDescription(description, options);
    }
}
