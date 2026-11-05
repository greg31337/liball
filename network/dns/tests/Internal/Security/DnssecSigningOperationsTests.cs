using System.Security.Cryptography;
using System.Text;

namespace dns.Tests.Internal.Security;

public class DnssecSigningOperationsTests
{
    [Test]
    public void SignsAndValidatesRrsetsWithCallerOwnedPkcs8Keys()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        DnsName zone = Name("example");
        DnsKeyMaterial key = RsaKey(zone, rsa);
        byte[] privateKeyBefore = key.PrivateKey.ToArray();
        DnsResourceRecordSet dnskeySet = DnskeySet(zone, key.Key);
        DnsResourceRecordSet addressSet = AddressSet(
            Name("www", "example"));
        DnsSignatureRequest request = Request(zone, key);
        request.RecordSets.Add(dnskeySet);
        request.RecordSets.Add(addressSet);

        DnsSecurityResult signed =
            new DnsDnssecService().SignDnssec(request);
        DnsMessage message = new DnsMessage();
        message.Answers.Add(dnskeySet.Records[0]);
        message.Answers.Add(addressSet.Records[0]);
        int index = 0;
        while (index < signed.GeneratedRecords.Count)
        {
            message.Answers.Add(signed.GeneratedRecords[index]);
            index = index + 1;
        }

        DnsTrustAnchor anchor = new DnsTrustAnchor();
        anchor.Name = zone;
        anchor.Key = key.Key;
        request.Policy.TrustAnchors.Add(anchor);
        DnsSecurityResult validated =
            new DnsDnssecService().ValidateDnssec(
                message,
                request.Policy);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(signed.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(signed.GeneratedRecords, Has.Count.EqualTo(2));
            Assert.That(signed.GeneratedRecords.All(record =>
                record.Data!.Signature!.Signature.Length == 128), Is.True);
            Assert.That(validated.Success, Is.True);
            Assert.That(validated.Status,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(validated.ValidatedRecordSets,
                Has.Count.EqualTo(2));
            Assert.That(key.PrivateKey, Is.EqualTo(privateKeyBefore));
            Assert.That(signed.GeneratedRecords.Any(record =>
                object.ReferenceEquals(record.Data!.Signature!.Signature,
                    key.PrivateKey)), Is.False);
        });
    }

    [Test]
    public void SignsWithEcdsaAndProducesDnsWireSignatureShape()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        DnsName zone = Name("example");
        DnsKeyMaterial key = EcdsaKey(zone, ecdsa);
        DnsResourceRecordSet set = AddressSet(
            Name("www", "example"));
        DnsSignatureRequest request = Request(zone, key);
        request.RecordSets.Add(set);

        DnsSecurityResult result =
            DnssecSigningOperations.SignDnssec(request);
        DnsSignatureData signature =
            result.GeneratedRecords[0].Data!.Signature!;
        DnsSecurityResult validated =
            DnssecRecordSetValidator.ValidateRecordSet(
                set,
                [key],
                [signature],
                request.Policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(signature.Algorithm,
                Is.EqualTo(DnsDnssecAlgorithm.EcdsaP256Sha256));
            Assert.That(signature.Signature, Has.Length.EqualTo(64));
            Assert.That(validated.Status,
                Is.EqualTo(DnsSecurityStatus.Secure));
        });
    }

    [Test]
    public void GeneratesCanonicalNsecChainAndSignsEveryDenialRecord()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        DnsZone zone = Zone();
        DnsKeyMaterial key = RsaKey(zone.Apex, rsa);
        DnsSignatureRequest request = ZoneRequest(zone, key);
        request.GenerateDenialRecords = true;

        DnsSecurityResult result =
            DnssecSigningOperations.SignDnssec(request);
        List<DnsResourceRecord> nsec = result.GeneratedRecords.Where(
            record => record.Data != null &&
                record.Data.Nsec != null).ToList();
        List<DnsResourceRecord> signatures = result.GeneratedRecords.Where(
            record => record.Data != null &&
                record.Data.Signature != null &&
                record.Data.Signature.TypeCovered ==
                    DnsRecordType.Nsec).ToList();
        DnsResourceRecord host = nsec.Single(record =>
            Text(record.Name) == "host.example.");
        DnsNsecProofRequest proof = new DnsNsecProofRequest();
        proof.Zone = zone.Apex;
        proof.QueryName = host.Name;
        proof.QueryTypeValue = 28;
        proof.ClassValue = 1;
        proof.Records = nsec;
        proof.Policy.ValidateDenial = true;

        DnsSecurityResult denial =
            NsecDenialValidator.ValidateDenial(proof);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(nsec, Has.Count.EqualTo(3));
            Assert.That(signatures, Has.Count.EqualTo(3));
            Assert.That(Text(nsec[0].Name), Is.EqualTo("example."));
            Assert.That(Text(nsec[0].Data!.Nsec!.NextDomainName),
                Is.EqualTo(Text(nsec[1].Name)));
            Assert.That(Text(nsec[1].Data!.Nsec!.NextDomainName),
                Is.EqualTo(Text(nsec[2].Name)));
            Assert.That(Text(nsec[2].Data!.Nsec!.NextDomainName),
                Is.EqualTo(Text(nsec[0].Name)));
            Assert.That(host.Data!.Nsec!.RecordTypeValues,
                Is.EqualTo(new[] { 1, 46, 47 }));
            Assert.That(denial.Status,
                Is.EqualTo(DnsSecurityStatus.Secure));
        });
    }

    [Test]
    public void GeneratesCanonicalNsec3ChainParameterAndProofs()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        DnsZone zone = Zone();
        DnsKeyMaterial key = RsaKey(zone.Apex, rsa);
        DnsSignatureRequest request = ZoneRequest(zone, key);
        request.GenerateDenialRecords = true;
        request.UseNsec3 = true;
        request.Nsec3Parameters = new DnsNsec3ParamData();
        request.Nsec3Parameters.HashAlgorithm =
            DnsNsec3HashAlgorithm.Sha1;
        request.Nsec3Parameters.HashAlgorithmValue = 1;
        request.Nsec3Parameters.Iterations = 2;
        request.Nsec3Parameters.Salt = [170, 187, 204, 221];

        DnsSecurityResult result =
            DnssecSigningOperations.SignDnssec(request);
        List<DnsResourceRecord> nsec3 = result.GeneratedRecords.Where(
            record => record.Data != null &&
                record.Data.Nsec3 != null).ToList();
        DnsResourceRecord parameter = result.GeneratedRecords.Single(
            record => record.Data != null &&
                record.Data.Nsec3Param != null);
        DnsNsec3HashResult hostHash = Nsec3DenialValidator.HashName(
            Name("host", "example"),
            1,
            2,
            [170, 187, 204, 221],
            10);
        string hostOwner = DnsTextEncoding.FormatBase32Hex(
            hostHash.Hash).ToLowerInvariant() + ".example.";
        DnsResourceRecord host = nsec3.Single(record =>
            Text(record.Name) == hostOwner);
        DnsNsec3ProofRequest proof = new DnsNsec3ProofRequest();
        proof.Zone = zone.Apex;
        proof.QueryName = Name("host", "example");
        proof.QueryTypeValue = 28;
        proof.ClassValue = 1;
        proof.Records = nsec3;
        proof.Policy.MaximumNsec3Iterations = 10;

        DnsSecurityResult denial =
            Nsec3DenialValidator.ValidateDenial(proof);
        string[] expectedOwners =
        {
            "3sievvggn53864uiqcfm3vakf9pj20fs.example.",
            "75uajtfu9802kao121j70m3bqje8d8c9.example.",
            "jna1f7dijaf4u9n74a2v5cgfpq63ajuc.example."
        };
        string[] expectedSuccessors =
        {
            "75uajtfu9802kao121j70m3bqje8d8c9",
            "jna1f7dijaf4u9n74a2v5cgfpq63ajuc",
            "3sievvggn53864uiqcfm3vakf9pj20fs"
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(parameter.Name.Labels,
                Has.Count.EqualTo(zone.Apex.Labels.Count));
            Assert.That(nsec3, Has.Count.EqualTo(3));
            Assert.That(nsec3.Select(record => Text(record.Name)),
                Is.EqualTo(expectedOwners));
            Assert.That(host.Data!.Nsec3!.RecordTypeValues,
                Is.EqualTo(new[] { 1 }));
            Assert.That(nsec3.Select(record =>
                    DnsTextEncoding.FormatBase32Hex(
                        record.Data!.Nsec3!.NextHashedOwnerName)
                        .ToLowerInvariant()),
                Is.EqualTo(expectedSuccessors));
            Assert.That(denial.Status,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.GeneratedRecords.Count(record =>
                    record.Data != null &&
                    record.Data.Signature != null &&
                    record.Data.Signature.TypeCovered ==
                        DnsRecordType.Nsec3),
                Is.EqualTo(3));
        });
    }

    [Test]
    public void EnforcesSigningRolesAndWorkLimits()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        DnsName zone = Name("example");
        DnsKeyMaterial key = RsaKey(zone, rsa);
        key.KeySigningKey = false;
        DnsSignatureRequest roleRequest = Request(zone, key);
        roleRequest.RecordSets.Add(DnskeySet(zone, key.Key));
        DnsSecurityResult roleFailure =
            DnssecSigningOperations.SignDnssec(roleRequest);
        key.KeySigningKey = true;
        DnsSignatureRequest limitRequest = Request(zone, key);
        limitRequest.Policy.MaximumSignatureValidations = 1;
        limitRequest.RecordSets.Add(DnskeySet(zone, key.Key));
        limitRequest.RecordSets.Add(AddressSet(Name("www", "example")));
        DnsSecurityResult limitFailure =
            DnssecSigningOperations.SignDnssec(limitRequest);

        Assert.Multiple(() =>
        {
            Assert.That(roleFailure.Success, Is.False);
            Assert.That(roleFailure.Diagnostics[0].Field,
                Is.EqualTo("keys.keySigningKey"));
            Assert.That(limitFailure.Success, Is.False);
            Assert.That(limitFailure.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
        });
    }

    static DnsSignatureRequest Request(
        DnsName zone,
        DnsKeyMaterial key)
    {
        DnsSignatureRequest request = new DnsSignatureRequest();
        request.SignerName = zone;
        request.Keys.Add(key);
        request.HasInception = true;
        request.Inception = 1000;
        request.HasExpiration = true;
        request.Expiration = 2000;
        request.Policy.DefaultAlgorithmPolicy =
            DnsAlgorithmPolicy.Unsupported;
        request.Policy.AlgorithmValues.Add(
            EffectiveAlgorithm(key.Key));
        request.Policy.AlgorithmPolicies.Add(
            DnsAlgorithmPolicy.Allowed);
        request.Policy.HasValidationTime = true;
        request.Policy.ValidationTime = 1500;
        request.Policy.ClockSkewSeconds = 0;
        return request;
    }

    static DnsSignatureRequest ZoneRequest(
        DnsZone zone,
        DnsKeyMaterial key)
    {
        DnsSignatureRequest request = Request(zone.Apex, key);
        request.Zone = zone;
        request.SignerName = new DnsName();
        return request;
    }

    static DnsKeyMaterial RsaKey(DnsName zone, RSA rsa)
    {
        RSAParameters parameters = rsa.ExportParameters(false);
        byte[] exponent = parameters.Exponent ?? [];
        byte[] modulus = parameters.Modulus ?? [];
        byte[] publicKey = new byte[1 + exponent.Length + modulus.Length];
        publicKey[0] = (byte)exponent.Length;
        Array.Copy(exponent, 0, publicKey, 1, exponent.Length);
        Array.Copy(
            modulus,
            0,
            publicKey,
            1 + exponent.Length,
            modulus.Length);
        DnsKeyMaterial result = new DnsKeyMaterial();
        result.Name = zone;
        result.Key.Flags = 257;
        result.Key.Protocol = 3;
        result.Key.Algorithm = DnsDnssecAlgorithm.RsaSha256;
        result.Key.AlgorithmValue = 8;
        result.Key.PublicKey = publicKey;
        result.HasPrivateKey = true;
        result.PrivateKey = rsa.ExportPkcs8PrivateKey();
        result.PrivateKeyFormat = "PKCS8";
        result.KeySigningKey = true;
        result.ZoneSigningKey = true;
        return result;
    }

    static DnsKeyMaterial EcdsaKey(DnsName zone, ECDsa ecdsa)
    {
        ECParameters parameters = ecdsa.ExportParameters(false);
        byte[] x = parameters.Q.X ?? [];
        byte[] y = parameters.Q.Y ?? [];
        byte[] publicKey = new byte[x.Length + y.Length];
        Array.Copy(x, 0, publicKey, 0, x.Length);
        Array.Copy(y, 0, publicKey, x.Length, y.Length);
        DnsKeyMaterial result = new DnsKeyMaterial();
        result.Name = zone;
        result.Key.Flags = 257;
        result.Key.Protocol = 3;
        result.Key.Algorithm = DnsDnssecAlgorithm.EcdsaP256Sha256;
        result.Key.AlgorithmValue = 13;
        result.Key.PublicKey = publicKey;
        result.HasPrivateKey = true;
        result.PrivateKey = ecdsa.ExportPkcs8PrivateKey();
        result.PrivateKeyFormat = "PKCS8";
        result.KeySigningKey = true;
        result.ZoneSigningKey = true;
        return result;
    }

    static DnsZone Zone()
    {
        DnsZoneLoadOptions options = new DnsZoneLoadOptions();
        options.Origin = Name("example");
        options.Class = DnsClass.Internet;
        options.ClassValue = 1;
        options.ValidateZone = true;
        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            "@ 300 SOA ns hostmaster 1 3600 600 86400 300\n" +
            "@ 300 NS ns\n" +
            "ns 300 A 192.0.2.53\n" +
            "host 300 A 192.0.2.1\n",
            options);
        Assert.That(result.Success, Is.True);
        return result.Zone!;
    }

    static DnsResourceRecordSet DnskeySet(
        DnsName owner,
        DnsKeyData key)
    {
        DnsRecordData data = new DnsRecordData();
        data.Key = key;
        return Set(owner, DnsRecordType.Dnskey, 48, data);
    }

    static DnsResourceRecordSet AddressSet(DnsName owner)
    {
        DnsAddressData address = new DnsAddressData();
        address.Address = [192, 0, 2, 44];
        DnsRecordData data = new DnsRecordData();
        data.Address = address;
        return Set(owner, DnsRecordType.A, 1, data);
    }

    static DnsResourceRecordSet Set(
        DnsName owner,
        DnsRecordType type,
        int typeValue,
        DnsRecordData data)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = owner;
        record.RecordType = type;
        record.RecordTypeValue = typeValue;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 300;
        record.Data = data;
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = owner;
        set.RecordType = type;
        set.RecordTypeValue = typeValue;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = 300;
        set.Records.Add(record);
        return set;
    }

    static int EffectiveAlgorithm(DnsKeyData key)
    {
        if (key.AlgorithmValue != 0 || key.Algorithm == 0)
        {
            return key.AlgorithmValue;
        }

        return (int)key.Algorithm;
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

    static string Text(DnsName name)
    {
        if (name.Labels.Count == 0)
        {
            return ".";
        }

        return string.Join(
            ".",
            name.Labels.Select(label =>
                Encoding.ASCII.GetString(label))) + ".";
    }
}
