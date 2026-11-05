namespace sdp.Tests.Unit.Operations;

public class SdpDtlsServiceTests
{
    [Test]
    public void ResolveMediaDtls_AppliesIndependentSessionDefaultsAndMediaOverrides()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(FingerprintAttribute("sha-256", 0x11, 32));
        description.Attributes.Add(FingerprintAttribute("sha-1", 0x22, 20));
        description.Attributes.Add(ValueAttribute("setup", "actpass"));
        description.Attributes.Add(ValueAttribute("connection", "new"));
        description.Attributes.Add(ValueAttribute("crypto", "1 AES_CM_128_HMAC_SHA1_80 inline:session-key"));
        SdpMediaDescription media = new SdpMediaDescription();
        media.Attributes.Add(FingerprintAttribute("sha-384", 0x33, 48));
        media.Attributes.Add(ValueAttribute("fingerprint", "malformed"));
        media.Attributes.Add(ValueAttribute("setup", "active"));
        media.Attributes.Add(ValueAttribute("tls-id", Id('m')));
        media.Attributes.Add(ValueAttribute("crypto", "2 AES_CM_128_HMAC_SHA1_32 inline:media-key"));
        description.MediaDescriptions.Add(media);
        ISdpDtlsService service = new SdpDtlsService();

        SdpDtlsDescription resolved = service.ResolveMediaDtls(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Fingerprints, Has.Count.EqualTo(1));
            Assert.That(resolved.Fingerprints[0].Algorithm, Is.EqualTo("sha-384"));
            Assert.That(resolved.Fingerprints[0].Digest, Has.Count.EqualTo(48));
            Assert.That(resolved.SetupRole, Is.EqualTo(SdpDtlsSetupRole.Active));
            Assert.That(resolved.SetupRoleToken, Is.EqualTo("active"));
            Assert.That(resolved.ConnectionRole, Is.EqualTo("new"));
            Assert.That(resolved.TlsId, Is.EqualTo(Id('m')));
            Assert.That(resolved.CryptoAttributes, Has.Count.EqualTo(1));
            Assert.That(resolved.CryptoAttributes[0].Tag, Is.EqualTo(2));
        });
    }

    [Test]
    public void ResolveMediaDtls_InheritsMultipleFingerprintsAndSkipsMalformedValues()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(FingerprintAttribute("sha-256", 0x11, 32));
        description.Attributes.Add(ValueAttribute("fingerprint", "malformed"));
        description.Attributes.Add(FingerprintAttribute("sha-1", 0x22, 20));
        description.Attributes.Add(ValueAttribute("setup", "extension-role"));
        description.Attributes.Add(ValueAttribute("crypto", "broken"));
        description.Attributes.Add(ValueAttribute("crypto", "3 FUTURE_SUITE x_method:key"));
        description.MediaDescriptions.Add(new SdpMediaDescription());
        ISdpDtlsService service = new SdpDtlsService();

        SdpDtlsDescription first = service.ResolveMediaDtls(description, 0);
        SdpDtlsDescription second = service.ResolveMediaDtls(description, 0);
        first.Fingerprints[0].Algorithm = "changed";
        first.CryptoAttributes[0].Suite = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first.Fingerprints, Has.Count.EqualTo(2));
            Assert.That(second.Fingerprints.Select(x => x.Algorithm), Is.EqualTo(new[] { "sha-256", "sha-1" }));
            Assert.That(second.SetupRole, Is.EqualTo(SdpDtlsSetupRole.Unknown));
            Assert.That(second.SetupRoleToken, Is.EqualTo("extension-role"));
            Assert.That(second.ConnectionRole, Is.Empty);
            Assert.That(second.TlsId, Is.Empty);
            Assert.That(second.CryptoAttributes, Has.Count.EqualTo(1));
            Assert.That(second.CryptoAttributes[0].Tag, Is.EqualTo(3));
            Assert.That(second.CryptoAttributes[0].Suite, Is.EqualTo("FUTURE_SUITE"));
        });
    }

    [Test]
    public void ResolveMediaDtls_MalformedMediaOverrideDoesNotExposeSessionDefault()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(FingerprintAttribute("sha-256", 0x11, 32));
        description.Attributes.Add(ValueAttribute("setup", "actpass"));
        description.Attributes.Add(ValueAttribute("connection", "new"));
        SdpMediaDescription media = new SdpMediaDescription();
        media.Attributes.Add(ValueAttribute("fingerprint", "malformed"));
        media.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "setup" });
        media.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "connection" });
        description.MediaDescriptions.Add(media);
        ISdpDtlsService service = new SdpDtlsService();

        SdpDtlsDescription resolved = service.ResolveMediaDtls(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Fingerprints, Is.Empty);
            Assert.That(resolved.SetupRole, Is.EqualTo(SdpDtlsSetupRole.Unknown));
            Assert.That(resolved.SetupRoleToken, Is.Empty);
            Assert.That(resolved.ConnectionRole, Is.Empty);
        });
    }

    [Test]
    public void ResolveEffectiveDtls_UsesTheBundleTaggedMediaTransport()
    {
        SdpSessionDescription description = BundledDescription("BUNDLE audio video");
        SdpMediaDescription audio = description.MediaDescriptions[0];
        audio.Attributes.Add(ValueAttribute("setup", "actpass"));
        audio.Attributes.Add(FingerprintAttribute("sha-256", 0x11, 32));
        audio.Attributes.Add(ValueAttribute("tls-id", Id('a')));
        SdpMediaDescription video = description.MediaDescriptions[1];
        video.Attributes.Add(ValueAttribute("setup", "passive"));
        video.Attributes.Add(FingerprintAttribute("sha-384", 0x22, 48));
        video.Attributes.Add(ValueAttribute("tls-id", Id('v')));
        ISdpDtlsService service = new SdpDtlsService();

        SdpDtlsDescription direct = service.ResolveMediaDtls(description, 1);
        SdpDtlsDescription effective = service.ResolveEffectiveDtls(description, 1);
        int ownerIndex = service.ResolveDtlsTransportMediaIndex(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(direct.SetupRole, Is.EqualTo(SdpDtlsSetupRole.Passive));
            Assert.That(direct.Fingerprints[0].Algorithm, Is.EqualTo("sha-384"));
            Assert.That(direct.TlsId, Is.EqualTo(Id('v')));
            Assert.That(ownerIndex, Is.Zero);
            Assert.That(effective.SetupRole, Is.EqualTo(SdpDtlsSetupRole.ActPass));
            Assert.That(effective.Fingerprints[0].Algorithm, Is.EqualTo("sha-256"));
            Assert.That(effective.TlsId, Is.EqualTo(Id('a')));
        });
    }

    [Test]
    public void ResolveEffectiveDtls_FallsBackWhenBundleOwnershipCannotBeResolved()
    {
        SdpSessionDescription description = BundledDescription("BUNDLE missing video");
        SdpMediaDescription video = description.MediaDescriptions[1];
        video.Attributes.Add(ValueAttribute("setup", "active"));
        video.Attributes.Add(FingerprintAttribute("sha-256", 0x33, 32));
        ISdpDtlsService service = new SdpDtlsService();

        int ownerIndex = service.ResolveDtlsTransportMediaIndex(description, 1);
        SdpDtlsDescription effective = service.ResolveEffectiveDtls(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(ownerIndex, Is.EqualTo(1));
            Assert.That(effective.SetupRole, Is.EqualTo(SdpDtlsSetupRole.Active));
            Assert.That(effective.Fingerprints[0].Digest[0], Is.EqualTo(0x33));
        });
    }

    [Test]
    public void SelectPreferredFingerprint_UsesAlgorithmPreferenceThenSourceOrder()
    {
        SdpDtlsDescription description = new SdpDtlsDescription();
        SdpFingerprintAttribute firstSha256 = Fingerprint("sha-256", 0x11, 32);
        SdpFingerprintAttribute secondSha256 = Fingerprint("SHA-256", 0x22, 32);
        description.Fingerprints.Add(firstSha256);
        description.Fingerprints.Add(secondSha256);
        description.Fingerprints.Add(Fingerprint("sha-1", 0x33, 20));
        description.Fingerprints.Add(Fingerprint("md5", 0x44, 16));
        ISdpDtlsService service = new SdpDtlsService();

        SdpFingerprintAttribute? selected = service.SelectPreferredFingerprint(description, new List<string> { "SHA-512", "Sha-256", "sha-1" });
        SdpFingerprintAttribute? missing = service.SelectPreferredFingerprint(description, new List<string> { "sha-384" });
        SdpFingerprintAttribute? prohibited = service.SelectPreferredFingerprint(description, new List<string> { "md5" });

        Assert.Multiple(() =>
        {
            Assert.That(selected, Is.SameAs(firstSha256));
            Assert.That(missing, Is.Null);
            Assert.That(prohibited, Is.Null);
            Assert.That(() => service.SelectPreferredFingerprint(description, new List<string> { "sha-256", "" }), Throws.ArgumentException.With.Property("ParamName").EqualTo("preferredAlgorithms"));
        });
    }

    [TestCase(SdpDtlsSetupRole.ActPass, SdpDtlsSetupRole.Active, SdpDtlsSetupRole.Active)]
    [TestCase(SdpDtlsSetupRole.ActPass, SdpDtlsSetupRole.Passive, SdpDtlsSetupRole.Passive)]
    [TestCase(SdpDtlsSetupRole.Active, SdpDtlsSetupRole.Active, SdpDtlsSetupRole.Passive)]
    [TestCase(SdpDtlsSetupRole.Passive, SdpDtlsSetupRole.Passive, SdpDtlsSetupRole.Active)]
    public void SelectAnswerSetupRole_ReturnsCompatibleRole(SdpDtlsSetupRole offered, SdpDtlsSetupRole preferred, SdpDtlsSetupRole expected)
    {
        ISdpDtlsService service = new SdpDtlsService();

        SdpDtlsSetupRole selected = service.SelectAnswerSetupRole(offered, preferred);

        Assert.That(selected, Is.EqualTo(expected));
    }

    [Test]
    public void SelectAnswerSetupRole_RejectsUnsupportedAndUndefinedRoles()
    {
        ISdpDtlsService service = new SdpDtlsService();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.SelectAnswerSetupRole(SdpDtlsSetupRole.Unknown, SdpDtlsSetupRole.Active), Throws.ArgumentException.With.Property("ParamName").EqualTo("offeredRole"));
            Assert.That(() => service.SelectAnswerSetupRole(SdpDtlsSetupRole.HoldConnection, SdpDtlsSetupRole.Active), Throws.ArgumentException.With.Property("ParamName").EqualTo("offeredRole"));
            Assert.That(() => service.SelectAnswerSetupRole(SdpDtlsSetupRole.ActPass, SdpDtlsSetupRole.ActPass), Throws.ArgumentException.With.Property("ParamName").EqualTo("preferredAnswerRole"));
            Assert.That(() => service.SelectAnswerSetupRole((SdpDtlsSetupRole)99, SdpDtlsSetupRole.Active), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("offeredRole"));
            Assert.That(() => service.SelectAnswerSetupRole(SdpDtlsSetupRole.ActPass, (SdpDtlsSetupRole)99), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("preferredAnswerRole"));
        });
    }

    [Test]
    public void ReplaceFingerprints_ReplacesEveryOccurrenceInCallerOrderAndCanClearOverride()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        description.Attributes.Add(FingerprintAttribute("sha-1", 0x10, 20));
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(PropertyAttribute("x-before"));
        media.Attributes.Add(FingerprintAttribute("sha-1", 0x20, 20));
        media.Attributes.Add(PropertyAttribute("x-middle"));
        media.Attributes.Add(FingerprintAttribute("sha-224", 0x30, 28));
        media.Attributes.Add(PropertyAttribute("x-after"));
        ISdpDtlsService service = new SdpDtlsService();
        List<SdpFingerprintAttribute> replacements = new List<SdpFingerprintAttribute>();
        replacements.Add(Fingerprint("sha-512", 0x40, 64));
        replacements.Add(Fingerprint("sha-256", 0x50, 32));

        service.ReplaceFingerprints(description, 0, replacements);
        SdpDtlsDescription replaced = service.ResolveMediaDtls(description, 0);
        string[] namesAfterReplace = media.Attributes.Select(x => x.Name).ToArray();
        service.ReplaceFingerprints(description, 0, new List<SdpFingerprintAttribute>());
        SdpDtlsDescription inherited = service.ResolveMediaDtls(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(replaced.Fingerprints.Select(x => x.Algorithm), Is.EqualTo(new[] { "sha-512", "sha-256" }));
            Assert.That(namesAfterReplace, Is.EqualTo(new[] { "x-before", "fingerprint", "fingerprint", "x-middle", "x-after" }));
            Assert.That(inherited.Fingerprints, Has.Count.EqualTo(1));
            Assert.That(inherited.Fingerprints[0].Algorithm, Is.EqualTo("sha-1"));
            Assert.That(media.Attributes.Select(x => x.Name), Is.EqualTo(new[] { "x-before", "x-middle", "x-after" }));
        });
    }

    [Test]
    public void ReplaceFingerprints_ValidatesEntireReplacementBeforeMutation()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(FingerprintAttribute("sha-1", 0x20, 20));
        media.Attributes.Add(PropertyAttribute("x-after"));
        string[] original = media.Attributes.Select(x => x.RawText).ToArray();
        List<SdpFingerprintAttribute> replacements = new List<SdpFingerprintAttribute>();
        replacements.Add(Fingerprint("sha-256", 0x30, 32));
        replacements.Add(new SdpFingerprintAttribute { Algorithm = "sha-384" });
        ISdpDtlsService service = new SdpDtlsService();

        Assert.That(() => service.ReplaceFingerprints(description, 0, replacements), Throws.ArgumentException.With.Property("ParamName").EqualTo("fingerprints"));
        Assert.That(media.Attributes.Select(x => x.RawText), Is.EqualTo(original));
    }

    [Test]
    public void ReplaceTlsId_RemovesDuplicatesAndRejectsInvalidReplacementAtomically()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(PropertyAttribute("x-before"));
        media.Attributes.Add(ValueAttribute("tls-id", Id('a')));
        media.Attributes.Add(PropertyAttribute("x-middle"));
        media.Attributes.Add(ValueAttribute("TLS-ID", Id('b')));
        ISdpDtlsService service = new SdpDtlsService();

        service.ReplaceTlsId(description, 0, new SdpTlsIdAttribute { Id = Id('c') });
        string[] afterReplacement = media.Attributes.Select(x => x.RawText).ToArray();
        SdpDtlsDescription resolved = service.ResolveMediaDtls(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.TlsId, Is.EqualTo(Id('c')));
            Assert.That(media.Attributes.Count(x => x.Name.Equals("tls-id", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(1));
            Assert.That(afterReplacement, Is.EqualTo(new[] { "x-before", "tls-id:" + Id('c'), "x-middle" }));
            Assert.That(() => service.ReplaceTlsId(description, 0, new SdpTlsIdAttribute { Id = "short" }), Throws.ArgumentException.With.Property("ParamName").EqualTo("tlsId"));
            Assert.That(media.Attributes.Select(x => x.RawText), Is.EqualTo(afterReplacement));
        });
    }

    [Test]
    public void ServiceMethods_RejectNullArgumentsAndInvalidMediaIndexes()
    {
        ISdpDtlsService service = new SdpDtlsService();
        SdpSessionDescription empty = new SdpSessionDescription();
        SdpDtlsDescription dtls = new SdpDtlsDescription();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.ResolveMediaDtls(null!, 0), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.ResolveMediaDtls(empty, -1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveEffectiveDtls(empty, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveDtlsTransportMediaIndex(null!, 0), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.SelectPreferredFingerprint(null!, new List<string>()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.SelectPreferredFingerprint(dtls, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("preferredAlgorithms"));
            Assert.That(() => service.SelectPreferredFingerprint(dtls, new List<string> { "" }), Throws.ArgumentException.With.Property("ParamName").EqualTo("preferredAlgorithms"));
            Assert.That(() => service.ReplaceFingerprints(empty, 0, new List<SdpFingerprintAttribute>()), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ReplaceFingerprints(DescriptionWithMedia(), 0, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("fingerprints"));
            Assert.That(() => service.ReplaceTlsId(DescriptionWithMedia(), 0, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("tlsId"));
        });
    }

    static SdpSessionDescription BundledDescription(string groupValue)
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute("group", groupValue));
        SdpMediaDescription audio = new SdpMediaDescription();
        audio.Attributes.Add(ValueAttribute("mid", "audio"));
        SdpMediaDescription video = new SdpMediaDescription();
        video.Attributes.Add(ValueAttribute("mid", "video"));
        description.MediaDescriptions.Add(audio);
        description.MediaDescriptions.Add(video);
        return description;
    }

    static SdpSessionDescription DescriptionWithMedia()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(new SdpMediaDescription());
        return description;
    }

    static SdpAttribute FingerprintAttribute(string algorithm, byte value, int count)
    {
        SdpFingerprintAttribute fingerprint = Fingerprint(algorithm, value, count);
        SdpAttributeEncodeResult encoded = new SdpAttributeCodec().EncodeAttribute(fingerprint);
        return encoded.Attribute!;
    }

    static SdpFingerprintAttribute Fingerprint(string algorithm, byte value, int count)
    {
        SdpFingerprintAttribute fingerprint = new SdpFingerprintAttribute { Algorithm = algorithm };
        int index = 0;
        while (index < count)
        {
            fingerprint.Digest.Add(value);
            index++;
        }
        return fingerprint;
    }

    static string Id(char character)
    {
        return new string(character, 20);
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }

    static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Property, Name = name, RawText = name };
    }
}
