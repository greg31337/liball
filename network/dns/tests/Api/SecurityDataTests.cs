namespace dns.Tests.Api;

public class SecurityDataTests
{
    [Test]
    public void SecurityPolicyStartsBoundedAndOwnsPolicyCollections()
    {
        DnsSecurityPolicy first = new DnsSecurityPolicy();
        DnsSecurityPolicy second = new DnsSecurityPolicy();
        first.AlgorithmValues.Add(253);
        first.AlgorithmPolicies.Add(DnsAlgorithmPolicy.ValidateOnly);
        first.DigestAlgorithmValues.Add(250);
        first.DigestAlgorithmPolicies.Add(DnsAlgorithmPolicy.Forbidden);
        first.TrustAnchors.Add(new DnsTrustAnchor());

        Assert.Multiple(() =>
        {
            Assert.That(first.DefaultAlgorithmPolicy, Is.EqualTo(DnsAlgorithmPolicy.Unsupported));
            Assert.That(first.DefaultDigestAlgorithmPolicy,
                Is.EqualTo(DnsAlgorithmPolicy.Unsupported));
            Assert.That(first.ClockSkewSeconds, Is.EqualTo(300));
            Assert.That(first.MaximumSignatureValidations, Is.EqualTo(64));
            Assert.That(first.MaximumNsec3Iterations, Is.EqualTo(100));
            Assert.That(first.MaximumProofRecords, Is.EqualTo(64));
            Assert.That(first.ValidateSignatures, Is.True);
            Assert.That(first.ValidateDenial, Is.True);
            Assert.That(first.AlgorithmValues, Is.EqualTo(new[] { 253 }));
            Assert.That(first.AlgorithmPolicies,
                Is.EqualTo(new[] { DnsAlgorithmPolicy.ValidateOnly }));
            Assert.That(first.DigestAlgorithmValues, Is.EqualTo(new[] { 250 }));
            Assert.That(first.DigestAlgorithmPolicies,
                Is.EqualTo(new[] { DnsAlgorithmPolicy.Forbidden }));
            Assert.That(second.AlgorithmValues, Is.Empty);
            Assert.That(second.AlgorithmPolicies, Is.Empty);
            Assert.That(second.DigestAlgorithmValues, Is.Empty);
            Assert.That(second.DigestAlgorithmPolicies, Is.Empty);
            Assert.That(second.TrustAnchors, Is.Empty);
        });
    }

    [Test]
    public void TrustAnchorsRetainKeyFormsAndSerialValidityBounds()
    {
        DnsTrustAnchor first = new DnsTrustAnchor();
        DnsTrustAnchor second = new DnsTrustAnchor();
        DnsKeyData key = new DnsKeyData();
        DnsDsData delegationSigner = new DnsDsData();
        first.Key = key;
        first.DelegationSigner = delegationSigner;
        first.Source = "iana-root-anchors.xml";
        first.Revoked = true;
        first.HasValidFrom = true;
        first.ValidFrom = 0;
        first.HasValidUntil = true;
        first.ValidUntil = uint.MaxValue;
        first.LoadedAt.UnixTimeMilliseconds = long.MaxValue;

        Assert.Multiple(() =>
        {
            Assert.That(first.Enabled, Is.True);
            Assert.That(first.Key, Is.SameAs(key));
            Assert.That(first.DelegationSigner, Is.SameAs(delegationSigner));
            Assert.That(first.Source, Is.EqualTo("iana-root-anchors.xml"));
            Assert.That(first.Revoked, Is.True);
            Assert.That(first.HasValidFrom, Is.True);
            Assert.That(first.ValidFrom, Is.Zero);
            Assert.That(first.HasValidUntil, Is.True);
            Assert.That(first.ValidUntil, Is.EqualTo(uint.MaxValue));
            Assert.That(first.LoadedAt.UnixTimeMilliseconds, Is.EqualTo(long.MaxValue));
            Assert.That(second.Name, Is.Not.SameAs(first.Name));
            Assert.That(second.LoadedAt, Is.Not.SameAs(first.LoadedAt));
            Assert.That(second.Key, Is.Null);
            Assert.That(second.DelegationSigner, Is.Null);
        });
    }

    [Test]
    public void KeyMaterialPreservesPrivateBytesUnknownClassAndSigningTimes()
    {
        DnsKeyMaterial first = new DnsKeyMaterial();
        DnsKeyMaterial second = new DnsKeyMaterial();
        first.Class = (DnsClass)65000;
        first.ClassValue = 65000;
        first.Key.Algorithm = (DnsDnssecAlgorithm)250;
        first.Key.AlgorithmValue = 250;
        first.KeyTag = 65535;
        first.HasPrivateKey = true;
        first.PrivateKey = [0, 1, 255];
        first.PrivateKeyFormat = "PKCS8";
        first.KeySigningKey = true;
        first.ZoneSigningKey = true;
        first.HasInception = true;
        first.Inception = 0;
        first.HasExpiration = true;
        first.Expiration = uint.MaxValue;

        Assert.Multiple(() =>
        {
            Assert.That((int)first.Class, Is.EqualTo(65000));
            Assert.That(first.ClassValue, Is.EqualTo(65000));
            Assert.That((int)first.Key.Algorithm, Is.EqualTo(250));
            Assert.That(first.Key.AlgorithmValue, Is.EqualTo(250));
            Assert.That(first.KeyTag, Is.EqualTo(65535));
            Assert.That(first.HasPrivateKey, Is.True);
            Assert.That(first.PrivateKey, Is.EqualTo(new byte[] { 0, 1, 255 }));
            Assert.That(first.PrivateKeyFormat, Is.EqualTo("PKCS8"));
            Assert.That(first.KeySigningKey, Is.True);
            Assert.That(first.ZoneSigningKey, Is.True);
            Assert.That(first.HasInception, Is.True);
            Assert.That(first.Inception, Is.Zero);
            Assert.That(first.HasExpiration, Is.True);
            Assert.That(first.Expiration, Is.EqualTo(uint.MaxValue));
            Assert.That(second.Class, Is.EqualTo(DnsClass.Internet));
            Assert.That(second.ClassValue, Is.EqualTo(1));
            Assert.That(second.Name, Is.Not.SameAs(first.Name));
            Assert.That(second.Key, Is.Not.SameAs(first.Key));
        });
    }

    [Test]
    public void SignatureRequestsOwnInputsAndRetainDenialGenerationOptions()
    {
        DnsSignatureRequest first = new DnsSignatureRequest();
        DnsSignatureRequest second = new DnsSignatureRequest();
        DnsZone zone = new DnsZone();
        DnsNsec3ParamData parameters = new DnsNsec3ParamData();
        first.Zone = zone;
        first.RecordSets.Add(new DnsResourceRecordSet());
        first.Keys.Add(new DnsKeyMaterial());
        first.HasOriginalTimeToLive = true;
        first.OriginalTimeToLive = uint.MaxValue;
        first.HasInception = true;
        first.Inception = 1;
        first.HasExpiration = true;
        first.Expiration = uint.MaxValue - 1;
        first.GenerateDenialRecords = true;
        first.UseNsec3 = true;
        first.Nsec3Parameters = parameters;

        Assert.Multiple(() =>
        {
            Assert.That(first.Zone, Is.SameAs(zone));
            Assert.That(first.RecordSets, Has.Count.EqualTo(1));
            Assert.That(first.Keys, Has.Count.EqualTo(1));
            Assert.That(first.HasOriginalTimeToLive, Is.True);
            Assert.That(first.OriginalTimeToLive, Is.EqualTo(uint.MaxValue));
            Assert.That(first.Inception, Is.EqualTo(1));
            Assert.That(first.Expiration, Is.EqualTo(uint.MaxValue - 1));
            Assert.That(first.GenerateDenialRecords, Is.True);
            Assert.That(first.UseNsec3, Is.True);
            Assert.That(first.Nsec3Parameters, Is.SameAs(parameters));
            Assert.That(second.RecordSets, Is.Empty);
            Assert.That(second.Keys, Is.Empty);
            Assert.That(second.SignerName, Is.Not.SameAs(first.SignerName));
            Assert.That(second.Policy, Is.Not.SameAs(first.Policy));
        });
    }

    [Test]
    public void SecurityResultsOwnProofsOutputsAnchorsAndDiagnostics()
    {
        DnsSecurityResult first = new DnsSecurityResult();
        DnsSecurityResult second = new DnsSecurityResult();
        DnsMessage message = new DnsMessage();
        first.Success = true;
        first.Status = DnsSecurityStatus.UnsupportedAlgorithm;
        first.Message = message;
        first.Algorithm = (DnsDnssecAlgorithm)251;
        first.AlgorithmValue = 251;
        first.HasKeyTag = true;
        first.KeyTag = 65535;
        first.HasExtendedError = true;
        first.ExtendedError = (DnsExtendedErrorCode)65000;
        first.ExtendedErrorValue = 65000;
        first.ValidatedRecordSets.Add(new DnsResourceRecordSet());
        first.ProofRecords.Add(new DnsResourceRecord());
        first.GeneratedRecords.Add(new DnsResourceRecord());
        first.TrustAnchors.Add(new DnsTrustAnchor());
        first.Diagnostics.Add(new DnsDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Status, Is.EqualTo(DnsSecurityStatus.UnsupportedAlgorithm));
            Assert.That(first.Message, Is.SameAs(message));
            Assert.That((int)first.Algorithm, Is.EqualTo(251));
            Assert.That(first.AlgorithmValue, Is.EqualTo(251));
            Assert.That(first.HasKeyTag, Is.True);
            Assert.That(first.KeyTag, Is.EqualTo(65535));
            Assert.That(first.HasExtendedError, Is.True);
            Assert.That((int)first.ExtendedError, Is.EqualTo(65000));
            Assert.That(first.ExtendedErrorValue, Is.EqualTo(65000));
            Assert.That(first.ValidatedRecordSets, Has.Count.EqualTo(1));
            Assert.That(first.ProofRecords, Has.Count.EqualTo(1));
            Assert.That(first.GeneratedRecords, Has.Count.EqualTo(1));
            Assert.That(first.TrustAnchors, Has.Count.EqualTo(1));
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(second.ValidatedRecordSets, Is.Empty);
            Assert.That(second.ProofRecords, Is.Empty);
            Assert.That(second.GeneratedRecords, Is.Empty);
            Assert.That(second.TrustAnchors, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void TsigKeysRetainSecretsAndUnsignedFortyEightBitValidityTimes()
    {
        DnsTsigKey first = new DnsTsigKey();
        DnsTsigKey second = new DnsTsigKey();
        first.Secret = [0xde, 0xad, 0xbe, 0xef];
        first.MacLength = 16;
        first.Fudge = 60;
        first.HasValidFrom = true;
        first.ValidFrom = 0;
        first.HasValidUntil = true;
        first.ValidUntil = 0xffffffffffffUL;

        Assert.Multiple(() =>
        {
            Assert.That(first.Secret, Is.EqualTo(new byte[] { 0xde, 0xad, 0xbe, 0xef }));
            Assert.That(first.MacLength, Is.EqualTo(16));
            Assert.That(first.Fudge, Is.EqualTo(60));
            Assert.That(first.HasValidFrom, Is.True);
            Assert.That(first.ValidFrom, Is.Zero);
            Assert.That(first.HasValidUntil, Is.True);
            Assert.That(first.ValidUntil, Is.EqualTo(0xffffffffffffUL));
            Assert.That(first.Enabled, Is.True);
            Assert.That(second.Fudge, Is.EqualTo(300));
            Assert.That(second.Name, Is.Not.SameAs(first.Name));
            Assert.That(second.AlgorithmName, Is.Not.SameAs(first.AlgorithmName));
        });
    }

    [Test]
    public void TsigResultsPreserveErrorsMacChainsAndPeerTimes()
    {
        DnsTsigResult first = new DnsTsigResult();
        DnsTsigResult second = new DnsTsigResult();
        DnsMessage message = new DnsMessage();
        DnsTsigData tsig = new DnsTsigData();
        first.Success = true;
        first.Authenticated = true;
        first.Error = (DnsTsigError)65000;
        first.ErrorValue = 65000;
        first.Message = message;
        first.Tsig = tsig;
        first.KeyName = new DnsName();
        first.Mac = [1, 2];
        first.PriorMac = [3, 4];
        first.HasTimeSigned = true;
        first.TimeSigned = 0xffffffffffffUL;
        first.HasServerTime = true;
        first.ServerTime = 0xfffffffffffeUL;
        first.Diagnostics.Add(new DnsDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Authenticated, Is.True);
            Assert.That((int)first.Error, Is.EqualTo(65000));
            Assert.That(first.ErrorValue, Is.EqualTo(65000));
            Assert.That(first.Message, Is.SameAs(message));
            Assert.That(first.Tsig, Is.SameAs(tsig));
            Assert.That(first.KeyName, Is.Not.Null);
            Assert.That(first.Mac, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(first.PriorMac, Is.EqualTo(new byte[] { 3, 4 }));
            Assert.That(first.TimeSigned, Is.EqualTo(0xffffffffffffUL));
            Assert.That(first.ServerTime, Is.EqualTo(0xfffffffffffeUL));
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(second.Mac, Is.Empty);
            Assert.That(second.PriorMac, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ZoneDigestResultsRetainRawAlgorithmsCountsAndExactDigest()
    {
        DnsZoneDigestResult first = new DnsZoneDigestResult();
        DnsZoneDigestResult second = new DnsZoneDigestResult();
        DnsZonemdData data = new DnsZonemdData();
        first.Success = true;
        first.Verified = true;
        first.Serial = uint.MaxValue;
        first.Scheme = 250;
        first.HashAlgorithm = 251;
        first.Digest = [0, 255, 1];
        first.RecordData = data;
        first.IncludedRecordCount = int.MaxValue;
        first.ExcludedRecordCount = 2;
        first.Diagnostics.Add(new DnsDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Verified, Is.True);
            Assert.That(first.Serial, Is.EqualTo(uint.MaxValue));
            Assert.That(first.Scheme, Is.EqualTo(250));
            Assert.That(first.HashAlgorithm, Is.EqualTo(251));
            Assert.That(first.Digest, Is.EqualTo(new byte[] { 0, 255, 1 }));
            Assert.That(first.RecordData, Is.SameAs(data));
            Assert.That(first.IncludedRecordCount, Is.EqualTo(int.MaxValue));
            Assert.That(first.ExcludedRecordCount, Is.EqualTo(2));
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(second.Digest, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
        });
    }
}
