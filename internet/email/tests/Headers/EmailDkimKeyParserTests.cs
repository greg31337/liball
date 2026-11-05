using System.Security.Cryptography;
using System.Text;

namespace email.Tests;

public class EmailDkimKeyParserTests
{
    [Test]
    public void ParsesRsaDnsRecordsWithMetadataAndUnknownTags()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        byte[] spki = rsa.ExportSubjectPublicKeyInfo();
        byte[] pkcs1 = rsa.ExportRSAPublicKey();
        string encoded = Convert.ToBase64String(spki);
        string folded = encoded.Substring(0, 40) + "\r\n\t" + encoded.Substring(40);
        string source = "v=DKIM1; h=sha256:future-hash; k=rsa; n=ops=20key; p=" + folded + "; s=email:*; t=y:s; x_vendor=opaque";

        EmailDkimKeyParseResult result = new EmailDkimKeyParser().ParseWithInputAndMode(Bytes(source), EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True, Describe(result.GetIssues()));
        EmailDkimKey key = result.GetValue()!;
        Assert.Multiple(() =>
        {
            Assert.That(key!.GetAlgorithmKind(), Is.EqualTo(EmailDkimKeyAlgorithmKind.Rsa));
            Assert.That(key.GetKeyType(), Is.EqualTo("rsa"));
            Assert.That(key.GetVersion(), Is.EqualTo("DKIM1"));
            Assert.That(key.GetNotes(), Is.EqualTo("ops key"));
            Assert.That(key.GetKeyBitCount(), Is.EqualTo(1024));
            Assert.That(key.GetPublicKeyData(), Is.EqualTo(spki));
            Assert.That(key.GetSubjectPublicKeyInfo(), Is.EqualTo(spki));
            Assert.That(key.GetRsaPublicKey(), Is.EqualTo(pkcs1));
            Assert.That(key.IsTesting(), Is.True);
            Assert.That(key.RequiresStrictIdentity(), Is.True);
            Assert.That(key.AllowsHashAlgorithm("SHA256"), Is.True);
            Assert.That(key.AllowsHashAlgorithm("sha1"), Is.False);
            Assert.That(key.AllowsService("email"), Is.True);
            Assert.That(key.AllowsService("future-service"), Is.True);
            Assert.That(key.GetUnknownTags().Single().GetName(), Is.EqualTo("x_vendor"));
            Assert.That(key.IsUsable(), Is.True);
            Assert.That(Ascii(key.GetRawBytes()), Is.EqualTo(source));
        });
        byte[] returned = key!.GetRsaPublicKey()!;
        returned![0] ^= 1;
        Assert.That(key.GetRsaPublicKey(), Is.EqualTo(pkcs1));
    }

    [Test]
    public void AppliesRecordDefaultsAndAcceptsPkcs1RsaData()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        byte[] pkcs1 = rsa.ExportRSAPublicKey();

        EmailDkimKeyParseResult result = new EmailDkimKeyParser().ParseWithInputAndMode(Bytes("p=" + Convert.ToBase64String(pkcs1)), EmailStructuredValueParseMode.Strict);

        Assert.That(result.IsSuccess(), Is.True, Describe(result.GetIssues()));
        EmailDkimKey key = result.GetValue()!;
        Assert.Multiple(() =>
        {
            Assert.That(key!.GetVersion(), Is.EqualTo("DKIM1"));
            Assert.That(key.GetKeyType(), Is.EqualTo("rsa"));
            Assert.That(key.GetServiceTypes(), Is.EqualTo(new[] { "*" }));
            Assert.That(key.GetHashAlgorithms(), Is.Empty);
            Assert.That(key.AllowsHashAlgorithm("sha256"), Is.True);
            Assert.That(key.HasSubjectPublicKeyInfo(), Is.False);
            Assert.That(key.GetRsaPublicKey(), Is.EqualTo(pkcs1));
            Assert.That(key.IsUsable(), Is.True);
        });
    }

    [Test]
    public void ParsesEd25519RevokedAndExtensionRecords()
    {
        byte[] publicKey = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        EmailDkimKeyParseResult ed = new EmailDkimKeyParser().ParseWithInputAndMode(Bytes("v=DKIM1; k=ed25519; p=" + Convert.ToBase64String(publicKey)), EmailStructuredValueParseMode.Strict);
        EmailDkimKeyParseResult revoked = new EmailDkimKeyParser().ParseWithInputAndMode(Bytes("v=DKIM1; p="), EmailStructuredValueParseMode.Strict);
        EmailDkimKeyParseResult extension = new EmailDkimKeyParser().ParseWithInputAndMode(Bytes("v=DKIM1; k=future-key; p=AQID; x_mode=one"), EmailStructuredValueParseMode.Strict);

        Assert.That(ed.IsSuccess(), Is.True, Describe(ed.GetIssues()));
        Assert.Multiple(() =>
        {
            Assert.That(ed.GetValue()!.GetAlgorithmKind(), Is.EqualTo(EmailDkimKeyAlgorithmKind.Ed25519));
            Assert.That(ed.GetValue()!.GetEd25519PublicKey(), Is.EqualTo(publicKey));
            Assert.That(ed.GetValue()!.GetKeyBitCount(), Is.EqualTo(256));
            Assert.That(ed.GetValue()!.IsUsable(), Is.True);
            Assert.That(revoked.IsSuccess(), Is.True);
            Assert.That(revoked.GetValue()!.IsRevoked(), Is.True);
            Assert.That(revoked.GetValue()!.IsUsable(), Is.False);
            Assert.That(revoked.GetIssues().ContainsCode(EmailIssueCode.DkimKeyRevoked), Is.True);
            Assert.That(extension.IsSuccess(), Is.True);
            Assert.That(extension.GetValue()!.GetAlgorithmKind(), Is.EqualTo(EmailDkimKeyAlgorithmKind.Extension));
            Assert.That(extension.GetValue()!.GetPublicKeyData(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(extension.GetValue()!.IsSupported(), Is.False);
            Assert.That(extension.GetValue()!.GetUnknownTags().Single().GetName(), Is.EqualTo("x_mode"));
            Assert.That(extension.GetIssues().ContainsCode(EmailIssueCode.DkimKeyAlgorithmUnsupported), Is.True);
        });
    }

    [Test]
    public void ImportsSpkiPkcs1AndPemPublicKeys()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        byte[] spki = rsa.ExportSubjectPublicKeyInfo();
        byte[] pkcs1 = rsa.ExportRSAPublicKey();
        string publicPem = Pem("PUBLIC KEY", spki);
        string rsaPem = Pem("RSA PUBLIC KEY", pkcs1);

        EmailDkimKeyParseResult importedSpki = new EmailDkimKeyParser().ImportSubjectPublicKeyInfoWithInput(spki);
        EmailDkimKeyParseResult importedPkcs1 = new EmailDkimKeyParser().ImportRsaPublicKeyWithInput(pkcs1);
        EmailDkimKeyParseResult importedPem = new EmailDkimKeyParser().ImportPemWithInput(publicPem);
        EmailDkimKeyParseResult importedRsaPem = new EmailDkimKeyParser().ImportPemWithInput(rsaPem);
        byte[] ed = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        byte[] edSpki = Concat(Hex("302a300506032b6570032100"), ed);
        EmailDkimKeyParseResult importedEd = new EmailDkimKeyParser().ImportSubjectPublicKeyInfoWithInput(edSpki);

        Assert.That(new[] { importedSpki, importedPkcs1, importedPem, importedRsaPem, importedEd }.All(value => value.IsSuccess()), Is.True, Describe(importedPem.GetIssues()));
        Assert.Multiple(() =>
        {
            Assert.That(importedSpki.GetValue()!.GetSourceKind(), Is.EqualTo(EmailDkimKeySourceKind.SubjectPublicKeyInfo));
            Assert.That(importedSpki.GetValue()!.GetRsaPublicKey(), Is.EqualTo(pkcs1));
            Assert.That(importedPkcs1.GetValue()!.GetSourceKind(), Is.EqualTo(EmailDkimKeySourceKind.RsaPublicKey));
            Assert.That(importedPkcs1.GetValue()!.GetRsaPublicKey(), Is.EqualTo(pkcs1));
            Assert.That(importedPem.GetValue()!.GetSourceKind(), Is.EqualTo(EmailDkimKeySourceKind.PemSubjectPublicKeyInfo));
            Assert.That(Ascii(importedPem.GetValue()!.GetRawBytes()), Is.EqualTo(publicPem));
            Assert.That(importedRsaPem.GetValue()!.GetSourceKind(), Is.EqualTo(EmailDkimKeySourceKind.PemRsaPublicKey));
            Assert.That(importedEd.GetValue()!.GetAlgorithmKind(), Is.EqualTo(EmailDkimKeyAlgorithmKind.Ed25519));
            Assert.That(importedEd.GetValue()!.GetEd25519PublicKey(), Is.EqualTo(ed));
            Assert.That(importedEd.GetValue()!.GetSubjectPublicKeyInfo(), Is.EqualTo(edSpki));
        });
    }

    [Test]
    public void RejectsMalformedRecordsAndKeyMaterial()
    {
        EmailDkimKeyParseResult version = new EmailDkimKeyParser().ParseWithInputAndMode(Bytes("p=; v=DKIM1.0"), EmailStructuredValueParseMode.Strict);
        EmailDkimKeyParseResult duplicate = new EmailDkimKeyParser().ParseWithInputAndMode(Bytes("v=DKIM1; p=; P="), EmailStructuredValueParseMode.Strict);
        EmailDkimKeyParseResult base64 = new EmailDkimKeyParser().ParseWithInputAndMode(Bytes("v=DKIM1; p=not_base64!"), EmailStructuredValueParseMode.Strict);
        EmailDkimKeyParseResult der = new EmailDkimKeyParser().ParseWithInputAndMode(Bytes("v=DKIM1; p=" + Convert.ToBase64String(new byte[] { 0x30, 0x00 })), EmailStructuredValueParseMode.Strict);
        EmailDkimKeyParseResult ed = new EmailDkimKeyParser().ParseWithInputAndMode(Bytes("v=DKIM1; k=ed25519; p=AQID"), EmailStructuredValueParseMode.Strict);
        EmailDkimKeyParseResult pem = new EmailDkimKeyParser().ImportPemWithInput("-----BEGIN PRIVATE KEY-----\nAQID\n-----END PRIVATE KEY-----");
        EmailDkimKeyParseResult joinedPem = new EmailDkimKeyParser().ImportPemWithInput("-----BEGIN PUBLIC KEY-----AQID-----END PUBLIC KEY-----");
        EmailDkimKeyParseResult notes = new EmailDkimKeyParser().ParseWithInputAndMode(Bytes("v=DKIM1; n=bad=2fvalue; p="), EmailStructuredValueParseMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(version.IsSuccess(), Is.False);
            Assert.That(version.GetValue()!.IsStructurallyValid(), Is.False);
            Assert.That(version.GetIssues().ContainsCode(EmailIssueCode.DkimKeyRecordMalformed), Is.True);
            Assert.That(duplicate.IsSuccess(), Is.False);
            Assert.That(duplicate.GetValue()!.GetTags().GetAllNamed("p").Count, Is.EqualTo(2));
            Assert.That(base64.IsSuccess(), Is.False);
            Assert.That(base64.GetIssues().ContainsCode(EmailIssueCode.DkimKeyMaterialMalformed), Is.True);
            Assert.That(der.IsSuccess(), Is.False);
            Assert.That(der.GetIssues().ContainsCode(EmailIssueCode.DkimKeyMaterialMalformed), Is.True);
            Assert.That(ed.IsSuccess(), Is.False);
            Assert.That(ed.GetValue()!.GetPublicKeyData(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(pem.HasValue(), Is.False);
            Assert.That(pem.GetIssues().ContainsCode(EmailIssueCode.DkimKeyPemMalformed), Is.True);
            Assert.That(joinedPem.HasValue(), Is.False);
            Assert.That(joinedPem.GetIssues().ContainsCode(EmailIssueCode.DkimKeyPemMalformed), Is.True);
            Assert.That(notes.IsSuccess(), Is.False);
            Assert.That(notes.GetIssues().ContainsCode(EmailIssueCode.DkimKeyRecordMalformed), Is.True);
        });
    }

    [Test]
    public void EnforcesRsaPolicyInputAndDecodedKeyLimits()
    {
        using RSA rsa = RSA.Create();
        rsa.KeySize = 1024;
        byte[] spki = rsa.ExportSubjectPublicKeyInfo();
        string record = "v=DKIM1; p=" + Convert.ToBase64String(spki);
        EmailDkimKeyParserOptions policy = new EmailDkimKeyParserOptions();
        policy.SetMode(EmailStructuredValueParseMode.Strict);
        policy.SetMinimumRsaKeyBits(2048);
        EmailDkimKeyParseResult policyResult = new EmailDkimKeyParser().ParseWithInputAndOptions(Bytes(record), policy);
        EmailDkimKeyParserOptions inputLimit = new EmailDkimKeyParserOptions();
        inputLimit.SetMaximumInputBytes(3);
        EmailDkimKeyParseResult inputResult = new EmailDkimKeyParser().ParseWithInputAndOptions(Bytes(record), inputLimit);
        EmailDkimKeyParserOptions decodedLimit = new EmailDkimKeyParserOptions();
        decodedLimit.SetMode(EmailStructuredValueParseMode.Strict);
        decodedLimit.SetMaximumDecodedKeyBytes(10);
        EmailDkimKeyParseResult decodedResult = new EmailDkimKeyParser().ParseWithInputAndOptions(Bytes(record), decodedLimit);
        EmailDkimKeyParserOptions clone = policy.Clone();
        policy.SetMinimumRsaKeyBits(3072);

        Assert.Multiple(() =>
        {
            Assert.That(policyResult.IsSuccess(), Is.False);
            Assert.That(policyResult.GetValue()!.GetKeyBitCount(), Is.EqualTo(1024));
            Assert.That(policyResult.GetValue()!.IsSizePolicyCompliant(), Is.False);
            Assert.That(policyResult.GetIssues().ContainsCode(EmailIssueCode.DkimKeySizePolicyViolation), Is.True);
            Assert.That(inputResult.HasValue(), Is.False);
            Assert.That(inputResult.HasFatalError(), Is.True);
            Assert.That(inputResult.GetConsumedByteCount(), Is.Zero);
            Assert.That(inputResult.GetIssues().ContainsCode(EmailIssueCode.DkimKeyInputLimitExceeded), Is.True);
            Assert.That(decodedResult.HasFatalError(), Is.True);
            Assert.That(decodedResult.GetValue()!.IsUsable(), Is.False);
            Assert.That(decodedResult.GetIssues().ContainsCode(EmailIssueCode.DkimKeyInputLimitExceeded), Is.True);
            Assert.That(clone.GetMinimumRsaKeyBits(), Is.EqualTo(2048));
            Assert.Throws<ArgumentOutOfRangeException>(() => clone.SetMinimumRsaKeyBits(512));
            Assert.Throws<ArgumentOutOfRangeException>(() => clone.SetMaximumRsaKeyBits(2048));
        });
    }

    [Test]
    public void TolerantParsingRetainsUnusableRecordsAndSourcePositions()
    {
        EmailDkimKeyParserOptions options = new EmailDkimKeyParserOptions();
        options.SetMode(EmailStructuredValueParseMode.Tolerant);

        EmailDkimKeyParseResult result = new EmailDkimKeyParser().ParseWithInputAndOptionsAndInitialPosition(Bytes("p=; v=wrong"), options, new EmailSourcePosition(100, 5, 7));

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetValue()!.IsStructurallyValid(), Is.False);
        Assert.That(result.GetValue()!.IsUsable(), Is.False);
        Assert.That(result.GetValue()!.GetSourcePosition().GetByteOffset(), Is.EqualTo(100));
        Assert.That(result.GetValue()!.GetTags().GetFirst("v")!.GetSourcePosition().GetByteOffset(), Is.EqualTo(104));
        Assert.That(result.GetIssues().GetAll().Any(value => value.GetCode() == EmailIssueCode.DkimKeyRecordMalformed && value.GetSeverity() == EmailIssueSeverity.Warning), Is.True);
    }

    static string Pem(string label, byte[] value)
    {
        return "-----BEGIN " + label + "-----\n" + Convert.ToBase64String(value, Base64FormattingOptions.InsertLineBreaks) + "\n-----END " + label + "-----";
    }

    static byte[] Hex(string value)
    {
        return Convert.FromHexString(value);
    }

    static byte[] Concat(byte[] first, byte[] second)
    {
        return first.Concat(second).ToArray();
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Ascii(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }

    static string Describe(EmailIssueList issues)
    {
        return string.Join(" | ", issues.GetAll().Select(value => value.GetCode() + ": " + value.GetDescription()));
    }
}
