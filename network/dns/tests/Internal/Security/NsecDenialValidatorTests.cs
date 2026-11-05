using System.Text;

namespace dns.Tests.Internal.Security;

public class NsecDenialValidatorTests
{
    [Test]
    public void ProvesNameErrorWithNextCloserAndWildcardIntervals()
    {
        DnsNsecProofRequest request = Request(
            Name("x", "b", "example"),
            1);
        request.NameError = true;
        DnsResourceRecord wildcardProof = Nsec(
            Name("example"),
            Name("a", "example"),
            2, 46, 47);
        DnsResourceRecord nextCloserProof = Nsec(
            Name("a", "example"),
            Name("c", "example"),
            2, 46, 47);
        request.Records.Add(wildcardProof);
        request.Records.Add(nextCloserProof);

        DnsSecurityResult result =
            NsecDenialValidator.ValidateDenial(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.ProofRecords, Has.Count.EqualTo(2));
            Assert.That(result.ProofRecords, Does.Contain(wildcardProof));
            Assert.That(result.ProofRecords, Does.Contain(nextCloserProof));
        });
    }

    [Test]
    public void HandlesTheCanonicalOrderingWraparoundInterval()
    {
        DnsNsecProofRequest request = Request(
            Name("x", "zz", "example"),
            1);
        request.NameError = true;
        DnsResourceRecord wrapping = Nsec(
            Name("z", "example"),
            Name("a", "example"),
            2, 46, 47);
        request.Records.Add(wrapping);

        DnsSecurityResult result =
            NsecDenialValidator.ValidateDenial(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.ProofRecords, Has.Count.EqualTo(1));
            Assert.That(result.ProofRecords[0], Is.SameAs(wrapping));
        });
    }

    [Test]
    public void RejectsNameErrorWithoutWildcardDenial()
    {
        DnsNsecProofRequest request = Request(
            Name("x", "b", "example"),
            1);
        request.NameError = true;
        request.Records.Add(Nsec(
            Name("a", "example"),
            Name("c", "example"),
            2, 46, 47));

        DnsSecurityResult result =
            NsecDenialValidator.ValidateDenial(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Bogus));
            Assert.That(result.Diagnostics[0].Field,
                Is.EqualTo("nsec.nameError"));
        });
    }

    [Test]
    public void ProvesExactNameNoDataFromTheTypeBitmap()
    {
        DnsNsecProofRequest request = Request(
            Name("host", "example"),
            28);
        DnsResourceRecord proof = Nsec(
            Name("host", "example"),
            Name("z", "example"),
            1, 46, 47);
        request.Records.Add(proof);

        DnsSecurityResult result =
            NsecDenialValidator.ValidateDenial(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.ProofRecords[0], Is.SameAs(proof));
        });
    }

    [TestCase(1)]
    [TestCase(5)]
    public void RejectsNoDataWhenTheQueryTypeOrCnameExists(int presentType)
    {
        DnsNsecProofRequest request = Request(
            Name("host", "example"),
            presentType == 1 ? 1 : 28);
        request.Records.Add(Nsec(
            Name("host", "example"),
            Name("z", "example"),
            presentType, 46, 47));

        DnsSecurityResult result =
            NsecDenialValidator.ValidateDenial(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Bogus));
            Assert.That(result.Diagnostics[0].Field,
                Is.EqualTo("nsec.typeBitmap"));
        });
    }

    [Test]
    public void ProvesWildcardNoDataUsingTheClosestEncloser()
    {
        DnsNsecProofRequest request = Request(
            Name("x", "sub", "example"),
            28);
        request.WildcardAnswer = true;
        request.ExistingNames.Add(Name("sub", "example"));
        DnsResourceRecord nextCloser = Nsec(
            Name("a", "sub", "example"),
            Name("z", "sub", "example"),
            2, 46, 47);
        DnsResourceRecord wildcard = Nsec(
            Name("*", "sub", "example"),
            Name("a", "sub", "example"),
            1, 46, 47);
        request.Records.Add(nextCloser);
        request.Records.Add(wildcard);

        DnsSecurityResult result =
            NsecDenialValidator.ValidateDenial(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.ProofRecords, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void RejectsMalformedNsecBitmapSemantics()
    {
        DnsNsecProofRequest request = Request(
            Name("host", "example"),
            28);
        request.Records.Add(Nsec(
            Name("host", "example"),
            Name("z", "example"),
            47, 46));

        DnsSecurityResult result =
            NsecDenialValidator.ValidateDenial(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status,
                Is.EqualTo(DnsSecurityStatus.Indeterminate));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTypeBitmap));
        });
    }

    [Test]
    public void RequiresAuthenticatedProofsAndEnforcesTheProofLimit()
    {
        DnsNsecProofRequest unauthenticated = Request(
            Name("host", "example"),
            28);
        unauthenticated.ProofStatus = DnsSecurityStatus.Bogus;
        DnsSecurityResult propagated =
            NsecDenialValidator.ValidateDenial(unauthenticated);
        DnsNsecProofRequest limited = Request(
            Name("host", "example"),
            28);
        limited.Policy.MaximumProofRecords = 1;
        limited.Records.Add(Nsec(
            Name("host", "example"),
            Name("x", "example"),
            1, 46, 47));
        limited.Records.Add(Nsec(
            Name("x", "example"),
            Name("z", "example"),
            1, 46, 47));
        DnsSecurityResult exceeded =
            NsecDenialValidator.ValidateDenial(limited);

        Assert.Multiple(() =>
        {
            Assert.That(propagated.Success, Is.True);
            Assert.That(propagated.Status,
                Is.EqualTo(DnsSecurityStatus.Bogus));
            Assert.That(exceeded.Success, Is.False);
            Assert.That(exceeded.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
        });
    }

    static DnsNsecProofRequest Request(DnsName query, int queryType)
    {
        DnsNsecProofRequest request = new DnsNsecProofRequest();
        request.Zone = Name("example");
        request.QueryName = query;
        request.QueryTypeValue = queryType;
        request.ClassValue = 1;
        request.Policy.MaximumProofRecords = 8;
        request.Policy.ValidateDenial = true;
        return request;
    }

    static DnsResourceRecord Nsec(
        DnsName owner,
        DnsName next,
        params int[] types)
    {
        DnsNsecData nsec = new DnsNsecData();
        nsec.NextDomainName = next;
        int index = 0;
        while (index < types.Length)
        {
            nsec.RecordTypeValues.Add(types[index]);
            index = index + 1;
        }

        DnsRecordData data = new DnsRecordData();
        data.Nsec = nsec;
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = owner;
        record.RecordType = DnsRecordType.Nsec;
        record.RecordTypeValue = 47;
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
}
