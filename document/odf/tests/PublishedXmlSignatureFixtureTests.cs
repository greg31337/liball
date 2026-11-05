using System.Security.Cryptography;
using System.Text;

namespace odf.Tests;

public class PublishedXmlSignatureFixtureTests
{
    [Test]
    public void VerifiesPublishedOdfToolkitMultiReferenceSignature()
    {
        // Apache ODF Toolkit validator fixture:
        // validator/src/test/resources/good.odt
        byte[] fixture = ReadFixture("signed-good.odt.base64");
        IOdfPackageReader reader = new OdfPackageReader();
        OdfPackage package = reader.ReadPackageBytes(
            fixture,
            new OdfLoadOptions());
        IOdfSecurityService service = new OdfSecurityService();

        OdfSignatureContainer[] containers = service.VerifyPackageSignatures(
            package,
            XmlSignatureVerifierTests.CreateLimits());
        OdfSignature signature = containers[0].Signatures[0];

        Assert.Multiple(() =>
        {
            Assert.That(
                Convert.ToHexString(SHA256.HashData(fixture)),
                Is.EqualTo("3CFC66C882898E24E2AAC7C067BB93673A913D60D8229435AC08EFD63A3BE5EF"));
            Assert.That(package.IsValid, Is.True);
            Assert.That(containers, Has.Length.EqualTo(1));
            Assert.That(containers[0].PartPath,
                Is.EqualTo(OdfPartNames.DocumentSignatures));
            Assert.That(containers[0].Version, Is.Empty);
            Assert.That(containers[0].Diagnostics, Has.Length.EqualTo(1));
            Assert.That(containers[0].Signatures, Has.Length.EqualTo(1));
            Assert.That(signature.SignatureMethod,
                Is.EqualTo(OdfAlgorithmIdentifiers.RsaSha1));
            Assert.That(signature.CanonicalizationMethod,
                Is.EqualTo(OdfAlgorithmIdentifiers.CanonicalXml10));
            Assert.That(signature.References, Has.Length.EqualTo(11));
            Assert.That(CountPackageReferences(signature.References), Is.EqualTo(9));
            Assert.That(CountSameDocumentReferences(signature.References), Is.EqualTo(2));
            Assert.That(AllReferencesMatch(signature.References), Is.True);
            Assert.That(signature.CertificateMaterialIsValid, Is.True);
            Assert.That(signature.SigningCertificateSubject,
                Does.Contain("CN=TSCP Test example Alice"));
            Assert.That(signature.SignatureValueMatches, Is.True);
            Assert.That(signature.Status, Is.EqualTo(OdfSignatureStatus.Valid));
        });
    }

    [Test]
    public void AlteringPublishedSignedContentReportsDigestMismatch()
    {
        byte[] fixture = ReadFixture("signed-good.odt.base64");
        IOdfPackageReader reader = new OdfPackageReader();
        OdfPackage package = reader.ReadPackageBytes(
            fixture,
            new OdfLoadOptions());
        IOdfPackageWriter writer = new OdfPackageWriter();
        OdfEditResult edit = writer.ReplacePackageEntry(
            package,
            "content.xml",
            Encoding.UTF8.GetBytes("<altered>after signing</altered>"));
        IOdfSecurityService service = new OdfSecurityService();

        OdfSignatureContainer[] containers = service.VerifyPackageSignatures(
            package,
            XmlSignatureVerifierTests.CreateLimits());
        OdfSignature signature = containers[0].Signatures[0];

        Assert.Multiple(() =>
        {
            Assert.That(edit.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(edit.Changed, Is.True);
            Assert.That(signature.ReferencesAreValid, Is.False);
            Assert.That(signature.SignatureValueMatches, Is.False);
            Assert.That(signature.Status,
                Is.EqualTo(OdfSignatureStatus.DigestMismatch));
            Assert.That(FindReference(signature.References, "content.xml").Status,
                Is.EqualTo(OdfSignatureStatus.DigestMismatch));
        });
    }

    static int CountPackageReferences(OdfSignatureReference[] references)
    {
        int result = 0;
        int index = 0;
        while (index < references.Length)
        {
            if (references[index].IsPackageReference)
            {
                result++;
            }
            index++;
        }
        return result;
    }

    static int CountSameDocumentReferences(OdfSignatureReference[] references)
    {
        int result = 0;
        int index = 0;
        while (index < references.Length)
        {
            if (references[index].IsSameDocument)
            {
                result++;
            }
            index++;
        }
        return result;
    }

    static bool AllReferencesMatch(OdfSignatureReference[] references)
    {
        int index = 0;
        while (index < references.Length)
        {
            if (references[index].Status != OdfSignatureStatus.Valid
                || !references[index].DigestMatches)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    static OdfSignatureReference FindReference(
        OdfSignatureReference[] references,
        string uri)
    {
        int index = 0;
        while (index < references.Length)
        {
            if (references[index].Uri == uri)
            {
                return references[index];
            }
            index++;
        }
        Assert.Fail($"Signature reference '{uri}' was not found.");
        return new OdfSignatureReference();
    }

    static byte[] ReadFixture(string name)
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "Fixtures",
            name));
        return Convert.FromBase64String(File.ReadAllText(path));
    }
}
