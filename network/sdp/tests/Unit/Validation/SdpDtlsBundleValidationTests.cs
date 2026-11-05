namespace sdp.Tests.Unit.Validation;

public class SdpDtlsBundleValidationTests
{
    [Test]
    public void BundledTransport_AllowsSharedTlsIdAndEquivalentRepeatedAttributes()
    {
        SdpSessionDescription description = BundledTlsDescription();
        SdpMediaDescription owner = description.MediaDescriptions[0];
        SdpMediaDescription member = description.MediaDescriptions[1];
        owner.Attributes.Add(Fingerprint("sha-1", 0x22, 20));
        member.Attributes.Insert(1, Fingerprint("SHA-1", 0x22, 20));

        SdpValidationResult result = Validate(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void BundledMember_InheritsTaggedTransportAttributesWhenOmitted()
    {
        SdpSessionDescription description = BundledTlsDescription();
        SdpMediaDescription member = description.MediaDescriptions[1];
        int index = member.Attributes.Count - 1;
        while (index >= 0)
        {
            if (IsDtlsTransportAttribute(member.Attributes[index].Name))
            {
                member.Attributes.RemoveAt(index);
            }
            index--;
        }

        SdpValidationResult result = Validate(description);
        SdpDtlsDescription resolved = new SdpDtlsService().ResolveEffectiveDtls(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(resolved.SetupRole, Is.EqualTo(SdpDtlsSetupRole.Active));
            Assert.That(resolved.ConnectionRole, Is.EqualTo("new"));
            Assert.That(resolved.TlsId, Is.EqualTo(Id('x')));
            Assert.That(resolved.Fingerprints, Has.Count.EqualTo(1));
            Assert.That(resolved.CryptoAttributes, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void BundledDtlsMember_UsesTaggedSetupAndFingerprintForValidation()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute("group", "BUNDLE audio video"));
        SdpMediaDescription owner = DtlsMedia("audio");
        owner.Attributes.Add(Fingerprint("sha-256", 0x11, 32));
        owner.Attributes.Add(ValueAttribute("setup", "actpass"));
        owner.Attributes.Add(ValueAttribute("tls-id", Id('d')));
        description.MediaDescriptions.Add(owner);
        description.MediaDescriptions.Add(DtlsMedia("video"));

        SdpValidationResult result = Validate(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [TestCase("fingerprint")]
    [TestCase("setup")]
    [TestCase("connection")]
    [TestCase("tls-id")]
    [TestCase("crypto")]
    public void BundledMember_ReportsConflictingTransportCategoryAttribute(string attributeName)
    {
        SdpSessionDescription description = BundledTlsDescription();
        SdpMediaDescription member = description.MediaDescriptions[1];
        SdpAttribute attribute = member.Attributes.First(x => x.Name == attributeName);
        if (attributeName == "fingerprint")
        {
            SdpAttribute replacement = Fingerprint("sha-256", 0x99, 32);
            attribute.Value = replacement.Value;
            attribute.RawText = replacement.RawText;
        }
        else if (attributeName == "setup")
        {
            attribute.Value = "passive";
        }
        else if (attributeName == "connection")
        {
            attribute.Value = "existing";
        }
        else if (attributeName == "tls-id")
        {
            attribute.Value = Id('y');
        }
        else
        {
            attribute.Value = "1 AES_CM_128_HMAC_SHA1_80 inline:member-secret";
        }

        SdpValidationResult result = Validate(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationBundle));
            Assert.That(result.Issues[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Issues[0].MediaIndex, Is.EqualTo(1));
            Assert.That(result.Issues[0].AttributeName, Is.EqualTo(attributeName));
            Assert.That(result.Issues[0].Detail, Does.Contain("tagged media transport"));
            Assert.That(result.Issues[0].Detail, Does.Not.Contain("member-secret"));
            Assert.That(result.Issues[0].Detail, Does.Not.Contain("99:99"));
        });
    }

    [Test]
    public void SeparateBundleTransports_StillRequireUniqueTlsIds()
    {
        SdpSessionDescription description = BundledTlsDescription();
        description.Attributes[0].Value = "BUNDLE audio";
        description.Attributes[0].RawText = "group:BUNDLE audio";

        SdpValidationResult result = Validate(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationDtls));
            Assert.That(result.Issues[0].AttributeName, Is.EqualTo("tls-id"));
            Assert.That(result.Issues[0].Detail, Does.Contain("unique"));
        });
    }

    static SdpSessionDescription BundledTlsDescription()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute("group", "BUNDLE audio video"));
        description.MediaDescriptions.Add(TlsMedia("audio"));
        description.MediaDescriptions.Add(TlsMedia("video"));
        return description;
    }

    static SdpMediaDescription TlsMedia(string mediaId)
    {
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = "audio",
            MediaKind = SdpMediaKind.Audio,
            Port = 5000,
            Protocol = "TCP/TLS"
        };
        media.Formats.Add("0");
        media.Attributes.Add(ValueAttribute("mid", mediaId));
        media.Attributes.Add(Fingerprint("sha-256", 0x11, 32));
        media.Attributes.Add(ValueAttribute("setup", "active"));
        media.Attributes.Add(ValueAttribute("connection", "new"));
        media.Attributes.Add(ValueAttribute("tls-id", Id('x')));
        media.Attributes.Add(ValueAttribute("crypto", "1 AES_CM_128_HMAC_SHA1_80 inline:shared-key"));
        return media;
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
        media.Attributes.Add(ValueAttribute("mid", mediaId));
        return media;
    }

    static bool IsDtlsTransportAttribute(string name)
    {
        return name == "fingerprint" || name == "setup" || name == "connection" || name == "tls-id" || name == "crypto";
    }

    static SdpValidationResult Validate(SdpSessionDescription description)
    {
        SdpValidationOptions options = new SdpValidationOptions { Profile = SdpValidationProfile.TlsDtls };
        return new SdpValidator().ValidateDescription(description, options);
    }

    static SdpAttribute Fingerprint(string algorithm, byte value, int count)
    {
        string fingerprint = string.Join(":", Enumerable.Repeat(value.ToString("X2"), count));
        return ValueAttribute("fingerprint", algorithm + " " + fingerprint);
    }

    static string Id(char character)
    {
        return new string(character, 20);
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }
}
