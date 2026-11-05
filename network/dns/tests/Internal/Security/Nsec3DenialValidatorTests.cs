using System.Text;

namespace dns.Tests.Internal.Security;

public class Nsec3DenialValidatorTests
{
    [Test]
    public void CalculatesTheRfc5155ExampleZoneHash()
    {
        DnsNsec3HashResult result = Nsec3DenialValidator.HashName(
            Name("example"),
            1,
            12,
            Convert.FromHexString("AABBCCDD"),
            12);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(DnsTextEncoding.FormatBase32Hex(result.Hash),
                Is.EqualTo("0P9MHAVEQVM6T7VBL5LOP2U3T2RP3TOM"));
        });
    }

    [Test]
    public void ProvesNameErrorWithClosestNextCloserAndWildcardHashes()
    {
        DnsNsec3ProofRequest request = Request(
            Name("missing", "example"),
            1);
        request.NameError = true;
        DnsResourceRecord apex = ExactRecord(
            Name("example"),
            0,
            2, 6);
        request.Records.Add(apex);

        DnsSecurityResult result =
            Nsec3DenialValidator.ValidateDenial(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.ProofRecords, Has.Count.EqualTo(1));
            Assert.That(result.ProofRecords[0], Is.SameAs(apex));
        });
    }

    [Test]
    public void ProvesExactNameNoDataAndEnforcesBitmapSemantics()
    {
        DnsName host = Name("host", "example");
        DnsNsec3ProofRequest valid = Request(host, 28);
        valid.Records.Add(ExactRecord(host, 0, 1, 15));
        DnsNsec3ProofRequest hasType = Request(host, 28);
        hasType.Records.Add(ExactRecord(host, 0, 1, 28));
        DnsNsec3ProofRequest hasCname = Request(host, 28);
        hasCname.Records.Add(ExactRecord(host, 0, 5, 15));

        DnsSecurityResult accepted =
            Nsec3DenialValidator.ValidateDenial(valid);
        DnsSecurityResult rejectedType =
            Nsec3DenialValidator.ValidateDenial(hasType);
        DnsSecurityResult rejectedCname =
            Nsec3DenialValidator.ValidateDenial(hasCname);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status,
                Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(rejectedType.Status,
                Is.EqualTo(DnsSecurityStatus.Bogus));
            Assert.That(rejectedCname.Status,
                Is.EqualTo(DnsSecurityStatus.Bogus));
        });
    }

    [Test]
    public void ProvesWildcardNoDataFromMatchingWildcardHash()
    {
        DnsNsec3ProofRequest request = Request(
            Name("www", "example"),
            28);
        request.WildcardAnswer = true;
        request.Records.Add(ExactRecord(
            Name("example"),
            0,
            2, 6));
        DnsResourceRecord wildcard = ExactRecord(
            Name("*", "example"),
            0,
            1, 15);
        request.Records.Add(wildcard);

        DnsSecurityResult result =
            Nsec3DenialValidator.ValidateDenial(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.ProofRecords, Does.Contain(wildcard));
        });
    }

    [Test]
    public void TreatsAnAllowedOptOutDsProofAsInsecure()
    {
        DnsNsec3ProofRequest allowed = Request(
            Name("delegation", "example"),
            43);
        allowed.OptOutAllowed = true;
        allowed.Records.Add(ExactRecord(
            Name("example"),
            1,
            2, 6));
        DnsNsec3ProofRequest forbidden = Request(
            Name("delegation", "example"),
            43);
        forbidden.Records.Add(ExactRecord(
            Name("example"),
            1,
            2, 6));

        DnsSecurityResult insecure =
            Nsec3DenialValidator.ValidateDenial(allowed);
        DnsSecurityResult bogus =
            Nsec3DenialValidator.ValidateDenial(forbidden);

        Assert.Multiple(() =>
        {
            Assert.That(insecure.Success, Is.True);
            Assert.That(insecure.Status,
                Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(bogus.Status, Is.EqualTo(DnsSecurityStatus.Bogus));
        });
    }

    [Test]
    public void RejectsExcessiveIterationsAndUnsupportedAlgorithms()
    {
        DnsNsec3ProofRequest excessive = Request(
            Name("host", "example"),
            28);
        excessive.Policy.MaximumNsec3Iterations = 1;
        excessive.Records.Add(Record(
            Hash(Name("host", "example")),
            Hash(Name("host", "example")),
            1,
            2,
            Salt(),
            0,
            1));
        DnsNsec3ProofRequest unknown = Request(
            Name("host", "example"),
            28);
        unknown.Records.Add(Record(
            Hash(Name("host", "example")),
            Hash(Name("host", "example")),
            250,
            2,
            Salt(),
            0,
            1));

        DnsSecurityResult bounded =
            Nsec3DenialValidator.ValidateDenial(excessive);
        DnsSecurityResult unsupported =
            Nsec3DenialValidator.ValidateDenial(unknown);
        unknown.Policy.TreatUnsupportedAsBogus = true;
        DnsSecurityResult promoted =
            Nsec3DenialValidator.ValidateDenial(unknown);

        Assert.Multiple(() =>
        {
            Assert.That(bounded.Success, Is.True);
            Assert.That(bounded.Status,
                Is.EqualTo(DnsSecurityStatus.UnsupportedAlgorithm));
            Assert.That(bounded.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(unsupported.Status,
                Is.EqualTo(DnsSecurityStatus.UnsupportedAlgorithm));
            Assert.That(promoted.Status,
                Is.EqualTo(DnsSecurityStatus.Bogus));
        });
    }

    [Test]
    public void RejectsSaltBeyondTheProtocolLengthLimit()
    {
        DnsName host = Name("host", "example");
        byte[] hostHash = Hash(host);
        DnsNsec3ProofRequest request = Request(host, 28);
        request.Records.Add(Record(
            hostHash,
            hostHash,
            1,
            2,
            new byte[256],
            0,
            1));

        DnsSecurityResult result =
            Nsec3DenialValidator.ValidateDenial(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status,
                Is.EqualTo(DnsSecurityStatus.Indeterminate));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidRecordData));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("nsec3"));
        });
    }

    [Test]
    public void RejectsMixedParametersAndMalformedBitmaps()
    {
        byte[] hostHash = Hash(Name("host", "example"));
        DnsNsec3ProofRequest mixed = Request(
            Name("host", "example"),
            28);
        mixed.Records.Add(Record(
            hostHash,
            hostHash,
            1,
            2,
            Salt(),
            0,
            1));
        mixed.Records.Add(Record(
            Hash(Name("other", "example")),
            hostHash,
            1,
            3,
            Salt(),
            0,
            1));
        DnsNsec3ProofRequest bitmap = Request(
            Name("host", "example"),
            28);
        bitmap.Records.Add(ExactRecord(
            Name("host", "example"),
            0,
            28, 1));

        DnsSecurityResult mixedResult =
            Nsec3DenialValidator.ValidateDenial(mixed);
        DnsSecurityResult bitmapResult =
            Nsec3DenialValidator.ValidateDenial(bitmap);

        Assert.Multiple(() =>
        {
            Assert.That(mixedResult.Success, Is.False);
            Assert.That(mixedResult.Diagnostics[0].Field,
                Is.EqualTo("nsec3.parameters"));
            Assert.That(bitmapResult.Success, Is.False);
            Assert.That(bitmapResult.Diagnostics[0].Field,
                Is.EqualTo("nsec3"));
        });
    }

    [Test]
    public void RequiresAuthenticatedProofsAndEnforcesRecordLimits()
    {
        DnsNsec3ProofRequest unauthenticated = Request(
            Name("host", "example"),
            28);
        unauthenticated.ProofStatus = DnsSecurityStatus.Bogus;
        DnsSecurityResult propagated =
            Nsec3DenialValidator.ValidateDenial(unauthenticated);
        DnsNsec3ProofRequest limited = Request(
            Name("host", "example"),
            28);
        limited.Policy.MaximumProofRecords = 1;
        limited.Records.Add(ExactRecord(
            Name("host", "example"),
            0,
            1));
        limited.Records.Add(ExactRecord(
            Name("other", "example"),
            0,
            1));
        DnsSecurityResult exceeded =
            Nsec3DenialValidator.ValidateDenial(limited);

        Assert.Multiple(() =>
        {
            Assert.That(propagated.Status,
                Is.EqualTo(DnsSecurityStatus.Bogus));
            Assert.That(exceeded.Success, Is.False);
            Assert.That(exceeded.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
        });
    }

    static DnsNsec3ProofRequest Request(DnsName query, int typeValue)
    {
        DnsNsec3ProofRequest request = new DnsNsec3ProofRequest();
        request.Zone = Name("example");
        request.QueryName = query;
        request.QueryTypeValue = typeValue;
        request.ClassValue = 1;
        request.Policy.ValidateDenial = true;
        request.Policy.MaximumProofRecords = 8;
        request.Policy.MaximumNsec3Iterations = 10;
        return request;
    }

    static DnsResourceRecord ExactRecord(
        DnsName original,
        int flags,
        params int[] types)
    {
        byte[] hash = Hash(original);
        return Record(hash, hash, 1, 2, Salt(), flags, types);
    }

    static DnsResourceRecord Record(
        byte[] ownerHash,
        byte[] nextHash,
        int algorithm,
        int iterations,
        byte[] salt,
        int flags,
        params int[] types)
    {
        DnsNsec3Data nsec3 = new DnsNsec3Data();
        nsec3.HashAlgorithm = (DnsNsec3HashAlgorithm)algorithm;
        nsec3.HashAlgorithmValue = algorithm;
        nsec3.Flags = flags;
        nsec3.Iterations = iterations;
        nsec3.Salt = salt;
        nsec3.NextHashedOwnerName = nextHash;
        int index = 0;
        while (index < types.Length)
        {
            nsec3.RecordTypeValues.Add(types[index]);
            index = index + 1;
        }

        DnsRecordData data = new DnsRecordData();
        data.Nsec3 = nsec3;
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = Name(
            DnsTextEncoding.FormatBase32Hex(ownerHash),
            "example");
        record.RecordType = DnsRecordType.Nsec3;
        record.RecordTypeValue = 50;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 300;
        record.Data = data;
        return record;
    }

    static byte[] Hash(DnsName name)
    {
        DnsNsec3HashResult result = Nsec3DenialValidator.HashName(
            name,
            1,
            2,
            Salt(),
            10);
        return result.Hash;
    }

    static byte[] Salt()
    {
        return [170, 187, 204, 221];
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
