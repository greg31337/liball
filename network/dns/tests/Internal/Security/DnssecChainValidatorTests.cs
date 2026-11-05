using System.Security.Cryptography;
using System.Text;

namespace dns.Tests.Internal.Security;

public class DnssecChainValidatorTests
{
    [Test]
    public void ValidatesAZoneCutAndReusesItsSecureKeyCacheEntry()
    {
        MemoryDnsCache cache = new MemoryDnsCache();
        DnssecZoneCutRequest request = SignedZoneCut();
        request.Cache = cache;

        DnsSecurityResult first =
            DnssecChainValidator.ValidateZoneCut(request);
        DnssecZoneCutRequest cachedRequest = new DnssecZoneCutRequest();
        cachedRequest.ParentStatus = DnsSecurityStatus.Secure;
        cachedRequest.Zone = request.Zone;
        cachedRequest.ClassValue = 1;
        cachedRequest.DelegationSigners.Add(
            request.DelegationSigners[0]);
        cachedRequest.Policy = request.Policy;
        cachedRequest.Cache = cache;
        cachedRequest.Now = request.Now;
        DnsSecurityResult cached =
            DnssecChainValidator.ValidateZoneCut(cachedRequest);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(first.HasKeyTag, Is.True);
            Assert.That(cached.Success, Is.True);
            Assert.That(cached.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(cached.HasKeyTag, Is.True);
        });
    }

    [Test]
    public void RejectsADelegationSignerThatDoesNotMatchAnyDnskey()
    {
        DnssecZoneCutRequest request = SignedZoneCut();
        request.DelegationSigners[0].Digest[0] =
            (byte)(request.DelegationSigners[0].Digest[0] ^ 1);

        DnsSecurityResult result =
            DnssecChainValidator.ValidateZoneCut(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Bogus));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("dnskey"));
        });
    }

    [Test]
    public void AcceptsAMatchingDelegationSignerDuringRollover()
    {
        DnssecZoneCutRequest request = SignedZoneCut();
        DnsDsData current = request.DelegationSigners[0];
        DnsDsData stale = new DnsDsData();
        stale.KeyTag = current.KeyTag;
        stale.Algorithm = current.Algorithm;
        stale.AlgorithmValue = current.AlgorithmValue;
        stale.DigestAlgorithm = current.DigestAlgorithm;
        stale.DigestAlgorithmValue = current.DigestAlgorithmValue;
        stale.Digest = current.Digest.ToArray();
        stale.Digest[0] = (byte)(stale.Digest[0] ^ 1);
        request.DelegationSigners.Insert(0, stale);

        DnsSecurityResult result =
            DnssecChainValidator.ValidateZoneCut(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.HasKeyTag, Is.True);
            Assert.That(result.KeyTag, Is.EqualTo(current.KeyTag));
        });
    }

    [Test]
    public void AppliesSecureAndInsecureZoneCutTransitions()
    {
        DnssecZoneCutRequest proven = SignedZoneCut();
        proven.DelegationSigners.Clear();
        proven.DelegationSignerAbsenceProven = true;
        DnssecZoneCutRequest unknown = SignedZoneCut();
        unknown.DelegationSigners.Clear();
        DnssecZoneCutRequest insecureParent = SignedZoneCut();
        insecureParent.ParentStatus = DnsSecurityStatus.Insecure;

        DnsSecurityResult insecure =
            DnssecChainValidator.ValidateZoneCut(proven);
        DnsSecurityResult indeterminate =
            DnssecChainValidator.ValidateZoneCut(unknown);
        DnsSecurityResult inherited =
            DnssecChainValidator.ValidateZoneCut(insecureParent);

        Assert.Multiple(() =>
        {
            Assert.That(insecure.Status,
                Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(indeterminate.Status,
                Is.EqualTo(DnsSecurityStatus.Indeterminate));
            Assert.That(inherited.Status,
                Is.EqualTo(DnsSecurityStatus.Insecure));
        });
    }

    [Test]
    public void MatchesAnEnabledDnskeyTrustAnchor()
    {
        DnssecZoneCutRequest request = SignedZoneCut();
        DnsTrustAnchor anchor = new DnsTrustAnchor();
        anchor.Name = request.Zone;
        anchor.Key = request.DnskeyRecordSet!.Records[0].Data!.Key;
        anchor.Enabled = true;
        request.Policy.TrustAnchors.Add(anchor);

        DnsSecurityResult result =
            DnssecChainValidator.ValidateTrustAnchors(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.TrustAnchors, Has.Count.EqualTo(1));
            Assert.That(result.TrustAnchors[0], Is.SameAs(anchor));
        });
    }

    [Test]
    public void MatchesAnEnabledDelegationSignerTrustAnchor()
    {
        DnssecZoneCutRequest request = SignedZoneCut();
        DnsTrustAnchor anchor = new DnsTrustAnchor();
        anchor.Name = request.Zone;
        anchor.DelegationSigner = request.DelegationSigners[0];
        request.Policy.TrustAnchors.Add(anchor);

        DnsSecurityResult result =
            DnssecChainValidator.ValidateTrustAnchors(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.TrustAnchors, Has.Count.EqualTo(1));
            Assert.That(result.TrustAnchors[0], Is.SameAs(anchor));
        });
    }

    [Test]
    public void IgnoresDisabledAndOutOfWindowTrustAnchors()
    {
        DnssecZoneCutRequest request = SignedZoneCut();
        DnsTrustAnchor disabled = new DnsTrustAnchor();
        disabled.Name = request.Zone;
        disabled.Key = request.DnskeyRecordSet!.Records[0].Data!.Key;
        disabled.Enabled = false;
        DnsTrustAnchor future = new DnsTrustAnchor();
        future.Name = request.Zone;
        future.Key = disabled.Key;
        future.HasValidFrom = true;
        future.ValidFrom = 2000;
        request.Policy.TrustAnchors.Add(disabled);
        request.Policy.TrustAnchors.Add(future);

        DnsSecurityResult result =
            DnssecChainValidator.ValidateTrustAnchors(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status,
                Is.EqualTo(DnsSecurityStatus.Indeterminate));
            Assert.That(result.Diagnostics[0].Field,
                Is.EqualTo("trustAnchor"));
        });
    }

    [Test]
    public void MapsUnsupportedDigestPolicyAndBogusPromotion()
    {
        DnssecZoneCutRequest request = SignedZoneCut();
        request.Policy.DigestAlgorithmPolicies[0] =
            DnsAlgorithmPolicy.Unsupported;
        DnsSecurityResult unsupported =
            DnssecChainValidator.ValidateZoneCut(request);
        request.Policy.TreatUnsupportedAsBogus = true;
        DnsSecurityResult bogus =
            DnssecChainValidator.ValidateZoneCut(request);

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Status,
                Is.EqualTo(DnsSecurityStatus.UnsupportedAlgorithm));
            Assert.That(bogus.Status, Is.EqualTo(DnsSecurityStatus.Bogus));
        });
    }

    [Test]
    public void HonorsCheckingDisabledAndSetsDoCdAndAdFlags()
    {
        DnsMessage request = new DnsMessage();
        DnsMessage prepared =
            DnssecChainValidator.PrepareValidationRequest(
                request,
                1232,
                true);
        DnsResourceRecord? opt = MessageOperations.FindOptRecord(prepared);
        int requestFlags = MessageOperations.EffectiveFlags(prepared.Header);
        int ednsFlags = EdnsRecordDataCodec.ReadEffectiveFlags(
            opt!.Data!.Edns!);
        DnsMessage secure = DnssecChainValidator.ApplyAuthenticData(
            new DnsMessage(),
            DnsSecurityStatus.Secure,
            false);
        DnsMessage disabled = DnssecChainValidator.ApplyAuthenticData(
            secure,
            DnsSecurityStatus.Secure,
            true);
        DnssecZoneCutRequest zoneCut = SignedZoneCut();
        zoneCut.CheckingDisabled = true;
        DnsSecurityResult bypassed =
            DnssecChainValidator.ValidateZoneCut(zoneCut);

        Assert.Multiple(() =>
        {
            Assert.That(requestFlags & (int)DnsHeaderFlag.CheckingDisabled,
                Is.Not.Zero);
            Assert.That(ednsFlags & (int)DnsEdnsFlag.DnssecAnswerOk,
                Is.Not.Zero);
            Assert.That(
                MessageOperations.EffectiveFlags(secure.Header) &
                    (int)DnsHeaderFlag.AuthenticData,
                Is.Not.Zero);
            Assert.That(
                MessageOperations.EffectiveFlags(disabled.Header) &
                    (int)DnsHeaderFlag.AuthenticData,
                Is.Zero);
            Assert.That(bypassed.Status,
                Is.EqualTo(DnsSecurityStatus.Indeterminate));
        });
    }

    static DnssecZoneCutRequest SignedZoneCut()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        RSAParameters parameters = rsa.ExportParameters(false);
        DnsKeyData key = new DnsKeyData();
        key.Flags = 257;
        key.Protocol = 3;
        key.Algorithm = DnsDnssecAlgorithm.RsaSha256;
        key.AlgorithmValue = 8;
        key.PublicKey = RsaDnskey(parameters);
        DnsName zone = Name("child", "example");
        DnsResourceRecordSet set = DnskeySet(zone, key);
        DnsDnssecIntegerResult tag =
            DnssecAlgorithms.CalculateKeyTag(key);
        DnsSignatureData signature = new DnsSignatureData();
        signature.TypeCovered = DnsRecordType.Dnskey;
        signature.TypeCoveredValue = 48;
        signature.Algorithm = DnsDnssecAlgorithm.RsaSha256;
        signature.AlgorithmValue = 8;
        signature.Labels = 2;
        signature.OriginalTimeToLive = 3600;
        signature.SignatureInception = 900;
        signature.SignatureExpiration = 1100;
        signature.KeyTag = tag.Value;
        signature.SignerName = zone;
        DnsEncodeResult signed =
            CanonicalDnsData.BuildRrsigSignedData(set, signature);
        signature.Signature = rsa.SignData(
            signed.Bytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        DnsDelegationDigestResult digest =
            DnssecAlgorithms.CreateDelegationDigest(
                zone,
                key,
                DnsDigestAlgorithm.Sha256,
                2);
        DnssecZoneCutRequest request = new DnssecZoneCutRequest();
        request.ParentStatus = DnsSecurityStatus.Secure;
        request.Zone = zone;
        request.ClassValue = 1;
        request.DelegationSigners.Add(digest.DelegationSigner!);
        request.DnskeyRecordSet = set;
        request.Signatures.Add(signature);
        request.Policy = Policy();
        request.Now.UnixTimeMilliseconds = 100000;
        request.Now.MonotonicMilliseconds = 100000;
        return request;
    }

    static DnsSecurityPolicy Policy()
    {
        DnsSecurityPolicy policy = new DnsSecurityPolicy();
        policy.DefaultAlgorithmPolicy = DnsAlgorithmPolicy.Unsupported;
        policy.DefaultDigestAlgorithmPolicy =
            DnsAlgorithmPolicy.Unsupported;
        policy.AlgorithmValues.Add(8);
        policy.AlgorithmPolicies.Add(DnsAlgorithmPolicy.Allowed);
        policy.DigestAlgorithmValues.Add(2);
        policy.DigestAlgorithmPolicies.Add(DnsAlgorithmPolicy.Allowed);
        policy.HasValidationTime = true;
        policy.ValidationTime = 1000;
        policy.ClockSkewSeconds = 0;
        return policy;
    }

    static DnsResourceRecordSet DnskeySet(
        DnsName zone,
        DnsKeyData key)
    {
        DnsRecordData data = new DnsRecordData();
        data.Key = key;
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = zone;
        record.RecordType = DnsRecordType.Dnskey;
        record.RecordTypeValue = 48;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 3600;
        record.Data = data;
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = zone;
        set.RecordType = DnsRecordType.Dnskey;
        set.RecordTypeValue = 48;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = 3600;
        set.Records.Add(record);
        return set;
    }

    static byte[] RsaDnskey(RSAParameters parameters)
    {
        byte[] exponent = parameters.Exponent ?? [];
        byte[] modulus = parameters.Modulus ?? [];
        byte[] result = new byte[1 + exponent.Length + modulus.Length];
        result[0] = (byte)exponent.Length;
        Array.Copy(exponent, 0, result, 1, exponent.Length);
        Array.Copy(
            modulus,
            0,
            result,
            1 + exponent.Length,
            modulus.Length);
        return result;
    }

    static DnsName Name(params string[] labels)
    {
        DnsName name = new DnsName();
        name.IsAbsolute = true;
        int index = 0;
        while (index < labels.Length)
        {
            name.Labels.Add(Encoding.ASCII.GetBytes(labels[index]));
            index = index + 1;
        }

        return name;
    }
}
