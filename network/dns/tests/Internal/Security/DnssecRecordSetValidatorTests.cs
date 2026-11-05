using System.Text;

namespace dns.Tests.Internal.Security;

public class DnssecRecordSetValidatorTests
{
    [Test]
    public void AcceptsAValidSignatureAfterOtherCandidatesFail()
    {
        DnsSignatureData invalid = RfcSignature();
        invalid.Signature[0] = (byte)(invalid.Signature[0] ^ 1);
        DnsSignatureData valid = RfcSignature();
        DnsKeyMaterial wrongKey = RfcKeyMaterial();
        wrongKey.Name = Name("other", "example", "net");

        DnsSecurityResult result =
            DnssecRecordSetValidator.ValidateRecordSet(
                RfcRecordSet(),
                [wrongKey, RfcKeyMaterial()],
                [invalid, valid],
                Policy(DnsAlgorithmPolicy.ValidateOnly));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Secure));
            Assert.That(result.ValidatedRecordSets, Has.Count.EqualTo(1));
            Assert.That(result.AlgorithmValue, Is.EqualTo(8));
            Assert.That(result.HasKeyTag, Is.True);
            Assert.That(result.KeyTag, Is.EqualTo(9033));
        });
    }

    [Test]
    public void ReportsBogusForACryptographicallyInvalidSignature()
    {
        DnsSignatureData signature = RfcSignature();
        signature.Signature[0] = (byte)(signature.Signature[0] ^ 1);

        DnsSecurityResult result =
            DnssecRecordSetValidator.ValidateRecordSet(
                RfcRecordSet(),
                [RfcKeyMaterial()],
                [signature],
                Policy(DnsAlgorithmPolicy.Allowed));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Bogus));
            Assert.That(result.Diagnostics[0].Field,
                Is.EqualTo("rrsig.signature"));
        });
    }

    [Test]
    public void ReportsBogusForAnExpiredSignature()
    {
        DnsSecurityPolicy policy = Policy(DnsAlgorithmPolicy.Allowed);
        policy.ValidationTime = 1893456001;

        DnsSecurityResult result =
            DnssecRecordSetValidator.ValidateRecordSet(
                RfcRecordSet(),
                [RfcKeyMaterial()],
                [RfcSignature()],
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Bogus));
            Assert.That(result.Diagnostics[0].Field,
                Is.EqualTo("rrsig.validity"));
        });
    }

    [Test]
    public void ReportsBogusForANotYetValidSignature()
    {
        DnsSecurityPolicy policy = Policy(DnsAlgorithmPolicy.Allowed);
        policy.ValidationTime = 946684799;

        DnsSecurityResult result =
            DnssecRecordSetValidator.ValidateRecordSet(
                RfcRecordSet(),
                [RfcKeyMaterial()],
                [RfcSignature()],
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Bogus));
            Assert.That(result.Diagnostics[0].Field,
                Is.EqualTo("rrsig.validity"));
            Assert.That(result.Diagnostics[0].Reason,
                Is.EqualTo(
                    "The RRSIG is outside its accepted validity interval."));
        });
    }

    [Test]
    public void DistinguishesUnsignedAndMissingKeyOutcomes()
    {
        DnsSecurityPolicy policy = Policy(DnsAlgorithmPolicy.Allowed);
        DnsSecurityResult unsigned =
            DnssecRecordSetValidator.ValidateRecordSet(
                RfcRecordSet(),
                [],
                [],
                policy);
        DnsSecurityResult missingKey =
            DnssecRecordSetValidator.ValidateRecordSet(
                RfcRecordSet(),
                [],
                [RfcSignature()],
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(unsigned.Success, Is.True);
            Assert.That(unsigned.Status,
                Is.EqualTo(DnsSecurityStatus.Insecure));
            Assert.That(missingKey.Success, Is.True);
            Assert.That(missingKey.Status,
                Is.EqualTo(DnsSecurityStatus.Indeterminate));
            Assert.That(missingKey.Diagnostics[0].Field,
                Is.EqualTo("dnskey"));
        });
    }

    [Test]
    public void ReportsForbiddenAlgorithmsAsBogus()
    {
        DnsSecurityResult result =
            DnssecRecordSetValidator.ValidateRecordSet(
                RfcRecordSet(),
                [RfcKeyMaterial()],
                [RfcSignature()],
                Policy(DnsAlgorithmPolicy.Forbidden));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(DnsSecurityStatus.Bogus));
            Assert.That(result.Diagnostics[0].Reason,
                Does.Contain("forbidden"));
        });
    }

    [Test]
    public void ReportsUnsupportedAlgorithmsAccordingToPolicy()
    {
        DnsSecurityPolicy policy = Policy(DnsAlgorithmPolicy.Unsupported);
        DnsSecurityResult unsupported =
            DnssecRecordSetValidator.ValidateRecordSet(
                RfcRecordSet(),
                [],
                [RfcSignature()],
                policy);
        policy.TreatUnsupportedAsBogus = true;
        DnsSecurityResult promoted =
            DnssecRecordSetValidator.ValidateRecordSet(
                RfcRecordSet(),
                [],
                [RfcSignature()],
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Success, Is.True);
            Assert.That(unsupported.Status,
                Is.EqualTo(DnsSecurityStatus.UnsupportedAlgorithm));
            Assert.That(promoted.Success, Is.True);
            Assert.That(promoted.Status,
                Is.EqualTo(DnsSecurityStatus.Bogus));
        });
    }

    [Test]
    public void ReportsAllowedEd25519AsUnavailableFromTheRuntime()
    {
        DnsKeyData key = new DnsKeyData();
        key.Flags = 256;
        key.Protocol = 3;
        key.Algorithm = DnsDnssecAlgorithm.Ed25519;
        key.AlgorithmValue = 15;
        key.PublicKey = Convert.FromBase64String(
            "l02Woi0iS8Aa25FQkUd9RMzZHJpBoRQwAQEX1SxZJA4=");
        DnsDnssecIntegerResult tag =
            DnssecAlgorithms.CalculateKeyTag(key);
        DnsKeyMaterial material = new DnsKeyMaterial();
        material.Name = Name("example", "net");
        material.Class = DnsClass.Internet;
        material.ClassValue = 1;
        material.Key = key;
        DnsSignatureData signature = RfcSignature();
        signature.Algorithm = DnsDnssecAlgorithm.Ed25519;
        signature.AlgorithmValue = 15;
        signature.KeyTag = tag.Value;
        signature.Signature = new byte[64];
        DnsSecurityPolicy policy = Policy(DnsAlgorithmPolicy.Unsupported);
        policy.AlgorithmValues[0] = 15;
        policy.AlgorithmPolicies[0] = DnsAlgorithmPolicy.Allowed;

        DnsSecurityResult result =
            DnssecRecordSetValidator.ValidateRecordSet(
                RfcRecordSet(),
                [material],
                [signature],
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(tag.Success, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status,
                Is.EqualTo(DnsSecurityStatus.UnsupportedAlgorithm));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnsupportedAlgorithm));
        });
    }

    [Test]
    public void EnforcesTheSignatureValidationWorkLimit()
    {
        DnsSignatureData invalid = RfcSignature();
        invalid.Signature[0] = (byte)(invalid.Signature[0] ^ 1);
        DnsSecurityPolicy policy = Policy(DnsAlgorithmPolicy.Allowed);
        policy.MaximumSignatureValidations = 1;

        DnsSecurityResult result =
            DnssecRecordSetValidator.ValidateRecordSet(
                RfcRecordSet(),
                [RfcKeyMaterial(), RfcKeyMaterial()],
                [invalid],
                policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status,
                Is.EqualTo(DnsSecurityStatus.Indeterminate));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void IgnoresSignaturesForOtherRecordTypes()
    {
        DnsSignatureData signature = RfcSignature();
        signature.TypeCovered = DnsRecordType.Aaaa;
        signature.TypeCoveredValue = 28;

        DnsSecurityResult result =
            DnssecRecordSetValidator.ValidateRecordSet(
                RfcRecordSet(),
                [RfcKeyMaterial()],
                [signature],
                Policy(DnsAlgorithmPolicy.Allowed));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status,
                Is.EqualTo(DnsSecurityStatus.Insecure));
        });
    }

    static DnsSecurityPolicy Policy(DnsAlgorithmPolicy algorithmPolicy)
    {
        DnsSecurityPolicy policy = new DnsSecurityPolicy();
        policy.DefaultAlgorithmPolicy = DnsAlgorithmPolicy.Unsupported;
        policy.AlgorithmValues.Add(8);
        policy.AlgorithmPolicies.Add(algorithmPolicy);
        policy.HasValidationTime = true;
        policy.ValidationTime = 1000000000;
        policy.ClockSkewSeconds = 0;
        return policy;
    }

    static DnsKeyMaterial RfcKeyMaterial()
    {
        DnsKeyData key = new DnsKeyData();
        key.Flags = 256;
        key.Protocol = 3;
        key.Algorithm = DnsDnssecAlgorithm.RsaSha256;
        key.AlgorithmValue = 8;
        key.PublicKey = Convert.FromBase64String(
            "AwEAAcFcGsaxxdgiuuGmCkVImy4h99CqT7jwY3pexPGcnUFt" +
            "R2Fh36BponcwtkZ4cAgtvd4Qs8PkxUdp6p/DlUmObdk=");
        DnsKeyMaterial material = new DnsKeyMaterial();
        material.Name = Name("example", "net");
        material.Class = DnsClass.Internet;
        material.ClassValue = 1;
        material.Key = key;
        material.KeyTag = 9033;
        return material;
    }

    static DnsSignatureData RfcSignature()
    {
        DnsSignatureData signature = new DnsSignatureData();
        signature.TypeCovered = DnsRecordType.A;
        signature.TypeCoveredValue = 1;
        signature.Algorithm = DnsDnssecAlgorithm.RsaSha256;
        signature.AlgorithmValue = 8;
        signature.Labels = 3;
        signature.OriginalTimeToLive = 3600;
        signature.SignatureExpiration = 1893456000;
        signature.SignatureInception = 946684800;
        signature.KeyTag = 9033;
        signature.SignerName = Name("example", "net");
        signature.Signature = Convert.FromBase64String(
            "kRCOH6u7l0QGy9qpC9l1sLncJcOKFLJ7GhiUOibu4teYp5VE" +
            "9RncriShZNz85mwlMgNEacFYK/lPtPiVYP4bwg==");
        return signature;
    }

    static DnsResourceRecordSet RfcRecordSet()
    {
        DnsName owner = Name("www", "example", "net");
        DnsAddressData address = new DnsAddressData();
        address.Address = [192, 0, 2, 91];
        DnsRecordData data = new DnsRecordData();
        data.Address = address;
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = owner;
        record.RecordType = DnsRecordType.A;
        record.RecordTypeValue = 1;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 3600;
        record.Data = data;
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = owner;
        set.RecordType = DnsRecordType.A;
        set.RecordTypeValue = 1;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = 3600;
        set.Records.Add(record);
        return set;
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
