using System.Security.Cryptography;
using System.Text;

namespace dns.Tests.Internal.Resolver;

public class IterativeDnssecOperationsTests
{
    [Test]
    public void ValidatesSignedPositiveAnswersWithSecureCachedKeys()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        DnsKeyData key = Key(rsa.ExportParameters(false));
        DnsName zone = Name("example");
        DnsResourceRecordSet answer = AddressSet(
            Name("www", "example"));
        DnsSignatureData signature = SignSet(
            answer,
            zone,
            key,
            rsa);
        DnsMessage response = new DnsMessage();
        response.Answers.Add(answer.Records[0]);
        response.Answers.Add(SignatureRecord(
            answer.Name,
            signature));
        DnsRecursiveResolver resolver = ResolverWithKey(zone, key);
        DnsIterativeResolverState state = State(
            resolver,
            zone,
            answer.Name,
            1);

        DnsSecurityResult result =
            IterativeDnssecOperations.ValidatePositiveAnswer(
                state,
                response,
                [answer],
                resolver);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.ValidatedRecordSets, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RejectsUnsignedAnswersFromASecureDelegation()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        DnsKeyData key = Key(rsa.ExportParameters(false));
        DnsName zone = Name("example");
        DnsResourceRecordSet answer = AddressSet(
            Name("www", "example"));
        DnsRecursiveResolver resolver = ResolverWithKey(zone, key);
        DnsIterativeResolverState state = State(
            resolver,
            zone,
            answer.Name,
            1);

        DnsSecurityResult result =
            IterativeDnssecOperations.ValidatePositiveAnswer(
                state,
                new DnsMessage(),
                [answer],
                resolver);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Bogus));
        });
    }

    [Test]
    public void RejectsAnUnsignedAliasInAnOtherwiseSignedAnswerChain()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        DnsKeyData key = Key(rsa.ExportParameters(false));
        DnsName zone = Name("example");
        DnsName alias = Name("alias", "example");
        DnsName target = Name("target", "example");
        DnsResourceRecordSet aliasSet = CnameSet(alias, target);
        DnsResourceRecordSet answerSet = AddressSet(target);
        DnsMessage response = new DnsMessage();
        response.Answers.Add(aliasSet.Records[0]);
        response.Answers.Add(answerSet.Records[0]);
        response.Answers.Add(SignatureRecord(
            target,
            SignSet(answerSet, zone, key, rsa)));
        DnsRecursiveResolver resolver = ResolverWithKey(zone, key);
        DnsIterativeResolverState state = State(
            resolver,
            zone,
            alias,
            1);

        DnsSecurityResult result =
            IterativeDnssecOperations.ValidatePositiveAnswer(
                state,
                response,
                [aliasSet, answerSet],
                resolver);

        Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Bogus));
    }

    [Test]
    public void AuthenticatesDsAndEstablishesASecureChildDelegation()
    {
        using RSA parentRsa = RSA.Create();
        using RSA childRsa = RSA.Create();
        parentRsa.KeySize = 1024;
        childRsa.KeySize = 1024;
        DnsName parent = Name("example");
        DnsName child = Name("child", "example");
        DnsKeyData parentKey = Key(parentRsa.ExportParameters(false));
        DnsKeyData childKey = Key(childRsa.ExportParameters(false));
        DnsRecursiveResolver resolver = ResolverWithKey(
            parent,
            parentKey);
        StoreKey(resolver, child, childKey);
        DnsDelegationDigestResult digest =
            DnssecAlgorithms.CreateDelegationDigest(
                child,
                childKey,
                DnsDigestAlgorithm.Sha256,
                2);
        DnsRecordData dsData = new DnsRecordData();
        dsData.Ds = digest.DelegationSigner;
        DnsResourceRecordSet dsSet = Set(
            child,
            DnsRecordType.Ds,
            43,
            dsData);
        DnsMessage response = new DnsMessage();
        response.Authorities.Add(dsSet.Records[0]);
        response.Authorities.Add(SignatureRecord(
            child,
            SignSet(dsSet, parent, parentKey, parentRsa)));
        DnsIterativeResolverState state = State(
            resolver,
            parent,
            Name("www", "child", "example"),
            1);
        DnsDelegation delegation = new DnsDelegation();
        delegation.Zone = child;

        DnsSecurityResult result =
            IterativeDnssecOperations.ValidateReferral(
                state,
                response,
                delegation,
                resolver);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(delegation.AuthorityRecordSets,
                Has.Count.EqualTo(1));
            Assert.That(delegation.AuthorityRecordSets[0].RecordType,
                Is.EqualTo(DnsRecordType.Ds));
        });
    }

    [Test]
    public void ValidatesFetchedChildKeysAfterAnAuthenticatedReferral()
    {
        using RSA parentRsa = RSA.Create();
        using RSA childRsa = RSA.Create();
        parentRsa.KeySize = 1024;
        childRsa.KeySize = 1024;
        DnsName parent = Name("example");
        DnsName child = Name("child", "example");
        DnsKeyData parentKey = Key(parentRsa.ExportParameters(false));
        DnsKeyData childKey = Key(childRsa.ExportParameters(false));
        DnsRecursiveResolver resolver = ResolverWithKey(
            parent,
            parentKey);
        DnsDelegationDigestResult digest =
            DnssecAlgorithms.CreateDelegationDigest(
                child,
                childKey,
                DnsDigestAlgorithm.Sha256,
                2);
        DnsRecordData dsData = new DnsRecordData();
        dsData.Ds = digest.DelegationSigner;
        DnsResourceRecordSet dsSet = Set(
            child,
            DnsRecordType.Ds,
            43,
            dsData);
        DnsMessage referralResponse = new DnsMessage();
        referralResponse.Authorities.Add(dsSet.Records[0]);
        referralResponse.Authorities.Add(SignatureRecord(
            child,
            SignSet(dsSet, parent, parentKey, parentRsa)));
        DnsIterativeResolverState state = State(
            resolver,
            parent,
            Name("www", "child", "example"),
            1);
        DnsDelegation delegation = new DnsDelegation();
        delegation.Zone = child;
        DnsSecurityResult referral =
            IterativeDnssecOperations.ValidateReferral(
                state,
                referralResponse,
                delegation,
                resolver);
        state.CurrentDelegation = delegation;
        state.CurrentDelegationSecurityStatus = referral.Status;
        DnsResourceRecordSet keySet = DnskeySet(child, childKey);
        DnsMessage keyResponse = new DnsMessage();
        keyResponse.Header.Flags = DnsHeaderFlag.AuthoritativeAnswer;
        keyResponse.Header.FlagsValue =
            (int)DnsHeaderFlag.AuthoritativeAnswer;
        keyResponse.Answers.Add(keySet.Records[0]);
        keyResponse.Answers.Add(SignatureRecord(
            child,
            SignSet(keySet, child, childKey, childRsa)));

        bool needed =
            IterativeDnssecOperations.NeedsDelegationKeyValidation(
                state,
                resolver);
        DnsSecurityResult validated =
            IterativeDnssecOperations.ValidateDelegationKeys(
                state,
                keyResponse,
                resolver);
        delegation.Secure = validated.Status == DnsSecurityStatus.Secure;
        ReferralGlueOperations.StoreAuthority(
            delegation,
            resolver.Cache,
            resolver.Clock.ReadClock());
        DnsCacheLookup cached = resolver.Cache.Lookup(
            ResolveRequest(child, 48),
            resolver.Clock.ReadClock());
        DnsCacheLookup cachedDs = resolver.Cache.Lookup(
            ResolveRequest(child, 43),
            resolver.Clock.ReadClock());

        Assert.Multiple(() =>
        {
            Assert.That(referral.Status,
                Is.EqualTo(DnsSecurityStatus.Indeterminate));
            Assert.That(needed, Is.True);
            Assert.That(validated.Status,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(cached.Found, Is.True);
            Assert.That(cached.Entry!.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(cachedDs.Found, Is.True);
            Assert.That(cachedDs.Entry!.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Secure));
        });
    }

    [Test]
    public void ValidatesAndAggressivelyReusesAuthenticatedNsecNoData()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        DnsKeyData key = Key(rsa.ExportParameters(false));
        DnsName zone = Name("example");
        DnsName host = Name("host", "example");
        DnsResourceRecordSet nsecSet = NsecSet(host);
        DnsSignatureData signature = SignSet(
            nsecSet,
            zone,
            key,
            rsa);
        DnsMessage response = new DnsMessage();
        response.Authorities.Add(nsecSet.Records[0]);
        response.Authorities.Add(SignatureRecord(host, signature));
        DnsRecursiveResolver resolver = ResolverWithKey(zone, key);
        DnsIterativeResolverState state = State(
            resolver,
            zone,
            host,
            28);

        DnsSecurityResult validated =
            IterativeDnssecOperations.ValidateNegativeAnswer(
                state,
                response,
                false,
                resolver);
        IterativeDnssecOperations.StoreAggressiveDenial(
            state,
            validated.ProofRecords,
            300,
            resolver);
        DnsResolveRequest lookup = ResolveRequest(host, 28);
        DnsResolveResult? cached =
            IterativeDnssecOperations.ReadAggressiveDenial(
                lookup,
                state.Options,
                resolver);

        Assert.Multiple(() =>
        {
            Assert.That(validated.Success, Is.True);
            Assert.That(validated.Status,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(validated.ProofRecords, Has.Count.EqualTo(1));
            Assert.That(cached, Is.Not.Null);
            Assert.That(cached!.Status, Is.EqualTo(DnsResolveStatus.NoData));
            Assert.That(cached.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(cached.FromCache, Is.True);
            Assert.That(cached.Diagnostics[0].Field,
                Is.EqualTo("cache.aggressiveDenial"));
        });
    }

    [Test]
    public void RejectsNegativeAnswersWithAnUnsignedSoa()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        DnsKeyData key = Key(rsa.ExportParameters(false));
        DnsName zone = Name("example");
        DnsName host = Name("host", "example");
        DnsResourceRecordSet nsecSet = NsecSet(host);
        DnsResourceRecordSet soaSet = SoaSet(zone);
        DnsMessage response = new DnsMessage();
        response.Authorities.Add(nsecSet.Records[0]);
        response.Authorities.Add(SignatureRecord(
            host,
            SignSet(nsecSet, zone, key, rsa)));
        response.Authorities.Add(soaSet.Records[0]);
        DnsRecursiveResolver resolver = ResolverWithKey(zone, key);
        DnsIterativeResolverState state = State(
            resolver,
            zone,
            host,
            28);

        DnsSecurityResult result =
            IterativeDnssecOperations.ValidateNegativeAnswer(
                state,
                response,
                false,
                resolver);

        Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Bogus));
    }

    [Test]
    public void ValidatesAndAggressivelyReusesAuthenticatedNsec3NoData()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        DnsKeyData key = Key(rsa.ExportParameters(false));
        DnsName zone = Name("example");
        DnsName host = Name("host", "example");
        DnsResourceRecordSet nsec3Set = Nsec3Set(host);
        DnsMessage response = new DnsMessage();
        response.Authorities.Add(nsec3Set.Records[0]);
        response.Authorities.Add(SignatureRecord(
            nsec3Set.Name,
            SignSet(nsec3Set, zone, key, rsa)));
        DnsRecursiveResolver resolver = ResolverWithKey(zone, key);
        DnsIterativeResolverState state = State(
            resolver,
            zone,
            host,
            28);

        DnsSecurityResult validated =
            IterativeDnssecOperations.ValidateNegativeAnswer(
                state,
                response,
                false,
                resolver);
        IterativeDnssecOperations.StoreAggressiveDenial(
            state,
            validated.ProofRecords,
            300,
            resolver);
        DnsResolveResult? cached =
            IterativeDnssecOperations.ReadAggressiveDenial(
                ResolveRequest(host, 28),
                state.Options,
                resolver);

        Assert.Multiple(() =>
        {
            Assert.That(validated.Status,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(cached, Is.Not.Null);
            Assert.That(cached!.Status, Is.EqualTo(DnsResolveStatus.NoData));
            Assert.That(cached.SecurityStatus,
                Is.EqualTo(DnsSecurityStatus.Secure));
        });
    }

    [Test]
    public void DoesNotReuseExpiredAggressiveDenialProofs()
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        FixedClock clock = new FixedClock();
        clock.Value.UnixTimeMilliseconds = 1000;
        clock.Value.MonotonicMilliseconds = 1000;
        resolver.Clock = clock;
        DnsIterativeResolverState state = new DnsIterativeResolverState();
        state.ResolverClock = clock;
        state.CurrentDelegation.Zone = Name("example");
        state.Request = ResolveRequest(Name("host", "example"), 28);
        DnsResourceRecordSet set = NsecSet(
            Name("host", "example"));
        IterativeDnssecOperations.StoreAggressiveDenial(
            state,
            set.Records,
            1,
            resolver);
        clock.Value.UnixTimeMilliseconds = 2001;
        clock.Value.MonotonicMilliseconds = 2001;
        DnsRecursiveOptions options = Options();

        DnsResolveResult? result =
            IterativeDnssecOperations.ReadAggressiveDenial(
                state.Request,
                options,
                resolver);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(resolver.DenialEntries, Is.Empty);
        });
    }

    [Test]
    public void RecursiveOptionsOwnAnIndependentDnssecPolicy()
    {
        DnsRecursiveOptions first = new DnsRecursiveOptions();
        DnsRecursiveOptions second = new DnsRecursiveOptions();
        first.Security.MaximumProofRecords = 3;

        Assert.Multiple(() =>
        {
            Assert.That(first.Security, Is.Not.SameAs(second.Security));
            Assert.That(first.Security.MaximumProofRecords, Is.EqualTo(3));
            Assert.That(second.Security.MaximumProofRecords, Is.EqualTo(64));
        });
    }

    static DnsRecursiveResolver ResolverWithKey(
        DnsName zone,
        DnsKeyData key)
    {
        DnsRecursiveResolver resolver = new DnsRecursiveResolver();
        MemoryDnsCache cache = new MemoryDnsCache();
        resolver.Cache = cache;
        FixedClock clock = new FixedClock();
        clock.Value.UnixTimeMilliseconds = 100000;
        clock.Value.MonotonicMilliseconds = 100000;
        resolver.Clock = clock;
        StoreKey(resolver, zone, key);
        return resolver;
    }

    static void StoreKey(
        DnsRecursiveResolver resolver,
        DnsName zone,
        DnsKeyData key)
    {
        DnsResourceRecordSet keySet = DnskeySet(zone, key);
        DnsCacheEntry entry = new DnsCacheEntry();
        entry.Kind = DnsCacheEntryKind.Positive;
        entry.Name = zone;
        entry.RecordType = DnsRecordType.Dnskey;
        entry.RecordTypeValue = 48;
        entry.Class = DnsClass.Internet;
        entry.ClassValue = 1;
        entry.RecordSets.Add(keySet);
        entry.SecurityStatus = DnsSecurityStatus.Secure;
        entry.StoredAt = resolver.Clock.ReadClock();
        entry.ExpiresAt = new DnsClockValue();
        entry.ExpiresAt.UnixTimeMilliseconds = 3700000;
        entry.ExpiresAt.MonotonicMilliseconds = 3700000;
        entry.OriginalTimeToLive = 3600;
        entry.Authoritative = true;
        entry.Credibility = 5;
        resolver.Cache.Store(entry);
    }

    static DnsIterativeResolverState State(
        DnsRecursiveResolver resolver,
        DnsName zone,
        DnsName queryName,
        int typeValue)
    {
        DnsIterativeResolverState state = new DnsIterativeResolverState();
        state.Request = ResolveRequest(queryName, typeValue);
        state.CurrentName = queryName;
        state.CurrentDelegation.Zone = zone;
        state.CurrentDelegation.Secure = true;
        state.CurrentDelegationSecurityStatus = DnsSecurityStatus.Secure;
        state.Options = Options();
        state.ResolverClock = resolver.Clock;
        return state;
    }

    static DnsRecursiveOptions Options()
    {
        DnsRecursiveOptions options = new DnsRecursiveOptions();
        options.ValidateDnssec = true;
        options.Security.DefaultAlgorithmPolicy =
            DnsAlgorithmPolicy.Unsupported;
        options.Security.AlgorithmValues.Add(8);
        options.Security.AlgorithmPolicies.Add(
            DnsAlgorithmPolicy.Allowed);
        options.Security.DefaultDigestAlgorithmPolicy =
            DnsAlgorithmPolicy.Unsupported;
        options.Security.DigestAlgorithmValues.Add(2);
        options.Security.DigestAlgorithmPolicies.Add(
            DnsAlgorithmPolicy.Allowed);
        options.Security.HasValidationTime = true;
        options.Security.ValidationTime = 1000;
        options.Security.ClockSkewSeconds = 0;
        options.Security.ValidateDenial = true;
        return options;
    }

    static DnsResolveRequest ResolveRequest(DnsName name, int typeValue)
    {
        DnsResolveRequest request = new DnsResolveRequest();
        request.Name = name;
        request.RecordType = (DnsRecordType)typeValue;
        request.RecordTypeValue = typeValue;
        request.Class = DnsClass.Internet;
        request.ClassValue = 1;
        return request;
    }

    static DnsKeyData Key(RSAParameters parameters)
    {
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
        DnsKeyData key = new DnsKeyData();
        key.Flags = 257;
        key.Protocol = 3;
        key.Algorithm = DnsDnssecAlgorithm.RsaSha256;
        key.AlgorithmValue = 8;
        key.PublicKey = publicKey;
        return key;
    }

    static DnsResourceRecordSet DnskeySet(
        DnsName zone,
        DnsKeyData key)
    {
        DnsRecordData data = new DnsRecordData();
        data.Key = key;
        return Set(zone, DnsRecordType.Dnskey, 48, data);
    }

    static DnsResourceRecordSet AddressSet(DnsName owner)
    {
        DnsAddressData address = new DnsAddressData();
        address.Address = [192, 0, 2, 44];
        DnsRecordData data = new DnsRecordData();
        data.Address = address;
        return Set(owner, DnsRecordType.A, 1, data);
    }

    static DnsResourceRecordSet CnameSet(
        DnsName owner,
        DnsName target)
    {
        DnsNameData name = new DnsNameData();
        name.Name = target;
        DnsRecordData data = new DnsRecordData();
        data.Name = name;
        return Set(owner, DnsRecordType.Cname, 5, data);
    }

    static DnsResourceRecordSet SoaSet(DnsName zone)
    {
        DnsSoaData soa = new DnsSoaData();
        soa.PrimaryNameServer = Name("ns", "example");
        soa.ResponsibleMailbox = Name("hostmaster", "example");
        soa.Serial = 1;
        soa.Refresh = 3600;
        soa.Retry = 600;
        soa.Expire = 86400;
        soa.Minimum = 300;
        DnsRecordData data = new DnsRecordData();
        data.Soa = soa;
        return Set(zone, DnsRecordType.Soa, 6, data);
    }

    static DnsResourceRecordSet NsecSet(DnsName owner)
    {
        DnsNsecData nsec = new DnsNsecData();
        nsec.NextDomainName = Name("z", "example");
        nsec.RecordTypeValues.Add(1);
        nsec.RecordTypeValues.Add(46);
        nsec.RecordTypeValues.Add(47);
        DnsRecordData data = new DnsRecordData();
        data.Nsec = nsec;
        return Set(owner, DnsRecordType.Nsec, 47, data);
    }

    static DnsResourceRecordSet Nsec3Set(DnsName original)
    {
        byte[] salt = [170, 187, 204, 221];
        DnsNsec3HashResult hash = Nsec3DenialValidator.HashName(
            original,
            1,
            2,
            salt,
            10);
        DnsNsec3Data nsec3 = new DnsNsec3Data();
        nsec3.HashAlgorithm = DnsNsec3HashAlgorithm.Sha1;
        nsec3.HashAlgorithmValue = 1;
        nsec3.Iterations = 2;
        nsec3.Salt = salt;
        nsec3.NextHashedOwnerName = hash.Hash;
        nsec3.RecordTypeValues.Add(1);
        nsec3.RecordTypeValues.Add(15);
        DnsRecordData data = new DnsRecordData();
        data.Nsec3 = nsec3;
        return Set(
            Name(
                DnsTextEncoding.FormatBase32Hex(hash.Hash),
                "example"),
            DnsRecordType.Nsec3,
            50,
            data);
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

    static DnsSignatureData SignSet(
        DnsResourceRecordSet set,
        DnsName signer,
        DnsKeyData key,
        RSA rsa)
    {
        DnsDnssecIntegerResult tag =
            DnssecAlgorithms.CalculateKeyTag(key);
        DnsSignatureData signature = new DnsSignatureData();
        signature.TypeCovered = set.RecordType;
        signature.TypeCoveredValue = set.RecordTypeValue;
        signature.Algorithm = DnsDnssecAlgorithm.RsaSha256;
        signature.AlgorithmValue = 8;
        signature.Labels = set.Name.Labels.Count;
        signature.OriginalTimeToLive = set.TimeToLive;
        signature.SignatureInception = 900;
        signature.SignatureExpiration = 1100;
        signature.KeyTag = tag.Value;
        signature.SignerName = signer;
        DnsEncodeResult signed =
            CanonicalDnsData.BuildRrsigSignedData(set, signature);
        signature.Signature = rsa.SignData(
            signed.Bytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return signature;
    }

    static DnsResourceRecord SignatureRecord(
        DnsName owner,
        DnsSignatureData signature)
    {
        DnsRecordData data = new DnsRecordData();
        data.Signature = signature;
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = owner;
        record.RecordType = DnsRecordType.Rrsig;
        record.RecordTypeValue = 46;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 300;
        record.Data = data;
        return record;
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

    class FixedClock : IDnsIterativeClock
    {
        public DnsClockValue Value = new DnsClockValue();

        public DnsClockValue ReadClock()
        {
            DnsClockValue result = new DnsClockValue();
            result.UnixTimeMilliseconds = this.Value.UnixTimeMilliseconds;
            result.MonotonicMilliseconds =
                this.Value.MonotonicMilliseconds;
            return result;
        }
    }
}
