using System.Security.Cryptography;
using System.Text;

namespace dns.Tests.Internal.Authoritative;

public class AuthoritativeDnssecOperationsTests
{
    [Test]
    public void DoControlsAutomaticSignaturesAndAuthoritativeCdIsCleared()
    {
        DnsZone zone = SignedZone(
            "www 300 A 192.0.2.1\n",
            false);
        DnsAuthoritativeService service = Service(zone);
        DnsAuthoritativeRequest plain = Request(
            "www.example",
            DnsRecordType.A);
        DnsAuthoritativeRequest dnssec = Request(
            "www.example",
            DnsRecordType.A);
        RequestDnssec(dnssec);
        dnssec.Message.Header.Flags = dnssec.Message.Header.Flags |
            DnsHeaderFlag.CheckingDisabled;
        dnssec.Message.Header.FlagsValue = dnssec.Message.Header.FlagsValue |
            (int)DnsHeaderFlag.CheckingDisabled;

        DnsAuthoritativeResult plainResult =
            service.AnswerAuthoritatively(plain);
        DnsAuthoritativeResult dnssecResult =
            service.AnswerAuthoritatively(dnssec);

        Assert.Multiple(() =>
        {
            Assert.That(Types(plainResult.AnswerRecordSets),
                Is.EqualTo(new[] { 1 }));
            Assert.That(Types(dnssecResult.AnswerRecordSets),
                Is.EqualTo(new[] { 1, 46 }));
            Assert.That(dnssecResult.AnswerRecordSets[1].Records.All(
                record => record.Data!.Signature!.TypeCoveredValue == 1),
                Is.True);
            Assert.That(dnssecResult.Response!.Header.FlagsValue &
                (int)DnsHeaderFlag.CheckingDisabled, Is.Zero);
            Assert.That(dnssecResult.Response.Header.FlagsValue &
                (int)DnsHeaderFlag.AuthenticData, Is.Zero);
        });
    }

    [Test]
    public void ExplicitAuthenticDataPolicyRequiresClientSignal()
    {
        DnsZone zone = SignedZone(
            "www 300 A 192.0.2.2\n",
            false);
        DnsAuthoritativeService service = Service(zone);
        DnsAuthoritativeRequest signaled = Request(
            "www.example",
            DnsRecordType.A);
        signaled.Options.SetAuthenticData = true;
        signaled.Message.Header.Flags = DnsHeaderFlag.AuthenticData;
        signaled.Message.Header.FlagsValue =
            (int)DnsHeaderFlag.AuthenticData;
        DnsAuthoritativeRequest unsignaled = Request(
            "www.example",
            DnsRecordType.A);
        unsignaled.Options.SetAuthenticData = true;

        DnsAuthoritativeResult signaledResult =
            service.AnswerAuthoritatively(signaled);
        DnsAuthoritativeResult unsignaledResult =
            service.AnswerAuthoritatively(unsignaled);

        Assert.Multiple(() =>
        {
            Assert.That(signaledResult.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(signaledResult.Response!.Header.FlagsValue &
                (int)DnsHeaderFlag.AuthenticData, Is.Not.Zero);
            Assert.That(Types(signaledResult.AnswerRecordSets),
                Is.EqualTo(new[] { 1 }));
            Assert.That(unsignaledResult.Response!.Header.FlagsValue &
                (int)DnsHeaderFlag.AuthenticData, Is.Zero);
            Assert.That(unsignaledResult.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Indeterminate));
        });
    }

    [Test]
    public void ExplicitDnssecQueriesDoNotRequireDoOrAutomaticServing()
    {
        DnsZone zone = SignedZone(
            "www 300 A 192.0.2.8\n",
            false);
        DnsAuthoritativeService service = Service(zone);
        DnsAuthoritativeRequest keyRequest = Request(
            "example",
            DnsRecordType.Dnskey);
        keyRequest.Options.ServeDnssec = false;
        DnsAuthoritativeRequest addressRequest = Request(
            "www.example",
            DnsRecordType.A);
        addressRequest.Options.ServeDnssec = false;
        RequestDnssec(addressRequest);

        DnsAuthoritativeResult keys =
            service.AnswerAuthoritatively(keyRequest);
        DnsAuthoritativeResult address =
            service.AnswerAuthoritatively(addressRequest);

        Assert.Multiple(() =>
        {
            Assert.That(Types(keys.AnswerRecordSets),
                Is.EqualTo(new[] { 48 }));
            Assert.That(Types(address.AnswerRecordSets),
                Is.EqualTo(new[] { 1 }));
            Assert.That(address.AuthorityRecordSets, Is.Empty);
        });
    }

    [Test]
    public void ServesSignedNsecProofForNameError()
    {
        DnsZone zone = SignedZone(
            "www 300 A 192.0.2.3\n",
            false);
        DnsAuthoritativeRequest request = Request(
            "missing.example",
            DnsRecordType.A);
        RequestDnssec(request);

        DnsAuthoritativeResult result =
            Service(zone).AnswerAuthoritatively(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.NameError));
            Assert.That(result.ResponseCode,
                Is.EqualTo(DnsResponseCode.NameError));
            Assert.That(Types(result.AuthorityRecordSets),
                Does.Contain(6));
            Assert.That(Types(result.AuthorityRecordSets),
                Does.Contain(47));
            Assert.That(Types(result.AuthorityRecordSets),
                Does.Contain(46));
            Assert.That(result.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Secure));
        });
    }

    [Test]
    public void ServesWildcardExpansionDenialWithTheWildcardSignature()
    {
        DnsZone zone = SignedZone(
            "* 300 A 192.0.2.4\n",
            false);
        DnsAuthoritativeRequest request = Request(
            "host.example",
            DnsRecordType.A);
        RequestDnssec(request);

        DnsAuthoritativeResult result =
            Service(zone).AnswerAuthoritatively(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(Types(result.AnswerRecordSets),
                Is.EqualTo(new[] { 1, 46 }));
            Assert.That(Text(result.AnswerRecordSets[1].Name),
                Is.EqualTo("host.example."));
            Assert.That(Types(result.AuthorityRecordSets),
                Does.Contain(47));
            Assert.That(Types(result.AuthorityRecordSets),
                Does.Contain(46));
        });
    }

    [Test]
    public void ServesNsec3ProofsWithoutFallingBackToNsec()
    {
        DnsZone zone = SignedZone(
            "www 300 A 192.0.2.5\n",
            true);
        DnsAuthoritativeRequest request = Request(
            "missing.example",
            DnsRecordType.Aaaa);
        RequestDnssec(request);

        DnsAuthoritativeResult result =
            Service(zone).AnswerAuthoritatively(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(Types(result.AuthorityRecordSets),
                Does.Contain(50));
            Assert.That(Types(result.AuthorityRecordSets),
                Does.Not.Contain(47));
            Assert.That(Types(result.AuthorityRecordSets),
                Does.Contain(46));
            Assert.That(result.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Secure));
        });
    }

    [Test]
    public void AddsDelegationSignerOrAuthenticatedDsDenialToReferral()
    {
        DnsZone unsignedDelegation = SignedZone(
            "child 300 NS ns.child.example.\n" +
            "ns.child 300 A 192.0.2.6\n",
            false);
        DnsZone signedDelegation = SignedZone(
            "child 300 NS ns.child.example.\n" +
            "child 300 DS 12345 8 2 " +
            "0123456789abcdef0123456789abcdef" +
            "0123456789abcdef0123456789abcdef\n" +
            "ns.child 300 A 192.0.2.7\n",
            false);
        DnsAuthoritativeRequest deniedRequest = Request(
            "host.child.example",
            DnsRecordType.A);
        DnsAuthoritativeRequest signerRequest = Request(
            "host.child.example",
            DnsRecordType.A);
        RequestDnssec(deniedRequest);
        RequestDnssec(signerRequest);

        DnsAuthoritativeResult denied = Service(
            unsignedDelegation).AnswerAuthoritatively(deniedRequest);
        DnsAuthoritativeResult signer = Service(
            signedDelegation).AnswerAuthoritatively(signerRequest);

        Assert.Multiple(() =>
        {
            Assert.That(denied.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Referral));
            Assert.That(Types(denied.AuthorityRecordSets),
                Does.Contain(47));
            Assert.That(Types(signer.AuthorityRecordSets),
                Does.Contain(43));
            Assert.That(signer.AuthorityRecordSets.Any(set =>
                    set.RecordTypeValue == 46 && set.Records.Any(record =>
                        record.Data!.Signature!.TypeCoveredValue == 43)),
                Is.True);
        });
    }

    static DnsZone SignedZone(string records, bool useNsec3)
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        DnsZone zone = LoadZone(records);
        DnsKeyMaterial key = Key(zone.Apex, rsa);
        AddRecordSet(zone, DnskeySet(zone.Apex, key.Key));
        DnsSignatureRequest request = new DnsSignatureRequest();
        request.Zone = zone;
        request.Keys.Add(key);
        request.GenerateDenialRecords = true;
        request.UseNsec3 = useNsec3;
        request.HasInception = true;
        request.Inception = 1000;
        request.HasExpiration = true;
        request.Expiration = 2000;
        request.Policy.AlgorithmValues.Add(8);
        request.Policy.AlgorithmPolicies.Add(
            DnsAlgorithmPolicy.Allowed);
        request.Policy.MaximumSignatureValidations = 1000;
        request.Policy.MaximumProofRecords = 1000;
        request.Policy.HasValidationTime = true;
        request.Policy.ValidationTime = 1500;
        request.Policy.ClockSkewSeconds = 0;
        if (useNsec3)
        {
            request.Nsec3Parameters = new DnsNsec3ParamData();
            request.Nsec3Parameters.HashAlgorithm =
                DnsNsec3HashAlgorithm.Sha1;
            request.Nsec3Parameters.HashAlgorithmValue = 1;
            request.Nsec3Parameters.Iterations = 0;
            request.Nsec3Parameters.Salt = [];
        }

        DnsSecurityResult signed = DnssecSigningOperations.SignDnssec(
            request);
        Assert.That(signed.Success, Is.True,
            string.Join("; ", signed.Diagnostics.Select(
                item => item.Reason)));
        foreach (DnsResourceRecord record in signed.GeneratedRecords)
        {
            AddRecord(zone, record);
        }

        return zone;
    }

    static DnsKeyMaterial Key(DnsName owner, RSA rsa)
    {
        RSAParameters parameters = rsa.ExportParameters(false);
        byte[] exponent = parameters.Exponent ?? [];
        byte[] modulus = parameters.Modulus ?? [];
        byte[] publicKey = new byte[
            1 + exponent.Length + modulus.Length];
        publicKey[0] = (byte)exponent.Length;
        Array.Copy(exponent, 0, publicKey, 1, exponent.Length);
        Array.Copy(
            modulus,
            0,
            publicKey,
            1 + exponent.Length,
            modulus.Length);
        DnsKeyMaterial result = new DnsKeyMaterial();
        result.Name = owner;
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

    static DnsResourceRecordSet DnskeySet(
        DnsName owner,
        DnsKeyData key)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = owner;
        record.RecordType = DnsRecordType.Dnskey;
        record.RecordTypeValue = 48;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 300;
        record.Data = new DnsRecordData();
        record.Data.Key = key;
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = owner;
        result.RecordType = DnsRecordType.Dnskey;
        result.RecordTypeValue = 48;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = 300;
        result.Records.Add(record);
        return result;
    }

    static void AddRecord(DnsZone zone, DnsResourceRecord record)
    {
        DnsZoneNode? node = zone.Nodes.FirstOrDefault(candidate =>
            RecordSetOperations.NamesEqual(candidate.Name, record.Name));
        if (node == null)
        {
            node = new DnsZoneNode();
            node.Name = record.Name;
            zone.Nodes.Add(node);
        }

        int typeValue = RecordSetOperations.EffectiveValue(
            record.RecordTypeValue,
            (int)record.RecordType);
        DnsResourceRecordSet? set = node.RecordSets.FirstOrDefault(
            candidate => RecordSetOperations.EffectiveValue(
                candidate.RecordTypeValue,
                (int)candidate.RecordType) == typeValue);
        if (set == null)
        {
            set = new DnsResourceRecordSet();
            set.Name = record.Name;
            set.RecordType = record.RecordType;
            set.RecordTypeValue = record.RecordTypeValue;
            set.Class = record.Class;
            set.ClassValue = record.ClassValue;
            set.TimeToLive = record.TimeToLive;
            node.RecordSets.Add(set);
        }

        set.Records.Add(record);
    }

    static void AddRecordSet(DnsZone zone, DnsResourceRecordSet set)
    {
        DnsZoneNode node = zone.Nodes.Single(candidate =>
            RecordSetOperations.NamesEqual(candidate.Name, set.Name));
        node.RecordSets.Add(set);
    }

    static DnsZone LoadZone(string records)
    {
        string source =
            "$ORIGIN example.\n" +
            "@ 300 SOA ns.outside. hostmaster.outside. " +
            "1 3600 600 86400 300\n" +
            "@ 300 NS ns.outside.\n" +
            records;
        DnsZoneLoadOptions options = new DnsZoneLoadOptions();
        options.ValidateZone = true;
        DnsZoneLoadResult loaded = new DnsZoneCodec().ParseZone(
            source,
            options);
        Assert.That(loaded.Success, Is.True,
            string.Join("; ", loaded.Diagnostics.Select(
                item => item.Reason)));
        return loaded.Zone!;
    }

    static DnsAuthoritativeService Service(DnsZone zone)
    {
        MemoryDnsZoneStore store = new MemoryDnsZoneStore();
        DnsZoneValidationResult added = store.AddZone(zone);
        Assert.That(added.Valid, Is.True,
            string.Join("; ", added.Diagnostics.Select(
                item => item.Reason)));
        return new DnsAuthoritativeService(store);
    }

    static DnsAuthoritativeRequest Request(
        string name,
        DnsRecordType recordType)
    {
        DnsAuthoritativeRequest result = new DnsAuthoritativeRequest();
        result.Message.Header.Opcode = DnsOpcode.Query;
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse(name);
        question.RecordType = recordType;
        question.RecordTypeValue = (int)recordType;
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        result.Message.Questions.Add(question);
        result.Message.Header.QuestionCount = 1;
        result.Options.Security.MaximumProofRecords = 1000;
        return result;
    }

    static void RequestDnssec(DnsAuthoritativeRequest request)
    {
        request.Message = EdnsOperations.PrepareRequest(
            request.Message,
            1232,
            true);
        request.Message.Header.AdditionalCount =
            request.Message.Additionals.Count;
    }

    static int[] Types(List<DnsResourceRecordSet> sets)
    {
        return sets.Select(set => set.RecordTypeValue).ToArray();
    }

    static DnsName Parse(string text)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        foreach (string label in text.Split('.'))
        {
            if (label.Length > 0)
            {
                result.Labels.Add(Encoding.ASCII.GetBytes(label));
            }
        }

        return result;
    }

    static string Text(DnsName name)
    {
        return string.Join(
            ".",
            name.Labels.Select(Encoding.ASCII.GetString)) + ".";
    }
}
