using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PackageSignatureOperationsTests
{
    [TestCase(XlsxSignatureAlgorithm.RsaSha256)]
    [TestCase(XlsxSignatureAlgorithm.RsaSha384)]
    [TestCase(XlsxSignatureAlgorithm.RsaSha512)]
    public void AuthorSaveOpenEnumerateValidate_CreatesValidSignatureWithSeparateTrustResult(XlsxSignatureAlgorithm algorithm)
    {
        using RSA privateKey = CreateSigningKey();
        using X509Certificate2 certificate = CreateSigningCertificate();
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        IXlsxPackageSignatureOperations operations = new XlsxPackageSignatureOperations();
        XlsxSignatureAuthoringOptions options = new XlsxSignatureAuthoringOptions();
        options.Certificate = certificate;
        options.PrivateKey = privateKey;
        options.Algorithm = algorithm;

        XlsxSignatureAuthoringResult authored = operations.AuthorSignature(workbook, options);
        XlsxWorkbookSaveResult save = workbook.SaveBytes();
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(save.Bytes);
        XlsxSignatureEnumerationResult enumeration = operations.EnumerateSignatures(open.Workbook!);
        XlsxSignatureValidationResult validation = operations.ValidateSignatures(open.Workbook!, true);

        Assert.That(authored.IsValid, Is.True);
        Assert.That(authored.Signature, Is.Not.Null);
        Assert.That(authored.Signature!.SignedParts.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(save.IsValid, Is.True);
        Assert.That(open.IsValid, Is.True);
        Assert.That(enumeration.IsValid, Is.True);
        Assert.That(enumeration.Signatures.Count, Is.EqualTo(1));
        Assert.That(enumeration.Signatures[0].Algorithm, Is.EqualTo(algorithm));
        Assert.That(FindReference(enumeration.Signatures[0], XlsxPackagePaths.Workbook), Is.Not.Null);
        Assert.That(validation.IsValid, Is.True);
        Assert.That(validation.HasSignatures, Is.True);
        Assert.That(validation.AllCryptographicSignaturesValid, Is.True);
        Assert.That(validation.AllReferencesValid, Is.True);
        Assert.That(validation.AllCertificatesValid, Is.True);
        Assert.That(validation.TrustEvaluated, Is.True);
        Assert.That(validation.AllCertificatesTrusted, Is.False);
        Assert.That(validation.Signatures[0].CertificateSubject, Does.Contain("xlsx test signer"));
        Assert.That(HasDiagnostic(validation.Validation, "signature.certificateUntrusted"), Is.True);
    }

    [Test]
    public void ValidateSignatures_TamperedSignedPartReportsReferenceFailureWithoutChangingSignatureValueResult()
    {
        using RSA privateKey = CreateSigningKey();
        using X509Certificate2 certificate = CreateSigningCertificate();
        XlsxWorkbookOpenResult open = CreateSignedOpenWorkbook(certificate, privateKey);
        XlsxPackagePart workbookPart = FindPackagePart(open.Workbook!, XlsxPackagePaths.Workbook)!;
        workbookPart.Data = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(workbookPart.Data) + " ");
        workbookPart.UncompressedBytes = workbookPart.Data.Length;

        XlsxSignatureValidationResult validation = new XlsxPackageSignatureOperations().ValidateSignatures(open.Workbook!, false);

        Assert.That(validation.IsValid, Is.False);
        Assert.That(validation.AllCryptographicSignaturesValid, Is.True);
        Assert.That(validation.AllReferencesValid, Is.False);
        Assert.That(validation.AllCertificatesValid, Is.True);
        Assert.That(validation.TrustEvaluated, Is.False);
        Assert.That(HasDiagnostic(validation.Validation, "signature.reference.digestMismatch"), Is.True);
    }

    [Test]
    public void ValidateSignatures_TamperedSignatureValueReportsCryptographicFailure()
    {
        using RSA privateKey = CreateSigningKey();
        using X509Certificate2 certificate = CreateSigningCertificate();
        XlsxWorkbookOpenResult open = CreateSignedOpenWorkbook(certificate, privateKey);
        XlsxDigitalSignaturePart signaturePart = FindSignaturePart(open.Workbook!)!;
        string xml = Encoding.UTF8.GetString(signaturePart.Data);
        int valueStart = xml.IndexOf("<ds:SignatureValue>", System.StringComparison.Ordinal) + "<ds:SignatureValue>".Length;
        char replacement = xml[valueStart] == 'A' ? 'B' : 'A';
        xml = xml.Substring(0, valueStart) + replacement + xml.Substring(valueStart + 1);
        signaturePart.Data = Encoding.UTF8.GetBytes(xml);

        XlsxSignatureValidationResult validation = new XlsxPackageSignatureOperations().ValidateSignatures(open.Workbook!, false);

        Assert.That(validation.IsValid, Is.False);
        Assert.That(validation.AllCryptographicSignaturesValid, Is.False);
        Assert.That(validation.AllReferencesValid, Is.True);
        Assert.That(HasDiagnostic(validation.Validation, "signature.valueMismatch"), Is.True);
    }

    [Test]
    public void RemoveSignatures_SaveReopenRemovesSignaturePartsRelationshipsAndContentTypes()
    {
        using RSA privateKey = CreateSigningKey();
        using X509Certificate2 certificate = CreateSigningCertificate();
        XlsxWorkbookOpenResult open = CreateSignedOpenWorkbook(certificate, privateKey);
        IXlsxPackageSignatureOperations operations = new XlsxPackageSignatureOperations();

        XlsxValidationResult removal = operations.RemoveSignatures(open.Workbook!);
        XlsxWorkbookSaveResult save = open.Workbook!.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());
        XlsxWorkbookOpenResult reopened = XlsxWorkbook.OpenBytes(save.Bytes);

        Assert.That(removal.IsValid, Is.True);
        Assert.That(save.IsValid, Is.True);
        Assert.That(package.GetEntry(XlsxPackagePaths.DigitalSignatureOrigin), Is.Null);
        Assert.That(package.GetEntry("/_xmlsignatures/sig1.xml"), Is.Null);
        Assert.That(package.GetEntry(XlsxPackagePath.GetRelationshipPartPath(XlsxPackagePaths.DigitalSignatureOrigin)), Is.Null);
        Assert.That(package.GetEntry(XlsxPackagePaths.PackageRelationships)!.GetText(), Does.Not.Contain(XlsxRelationshipTypes.DigitalSignatureOrigin));
        Assert.That(package.GetEntry(XlsxPackagePaths.ContentTypes)!.GetText(), Does.Not.Contain(XlsxContentTypes.DigitalSignatureXml));
        Assert.That(reopened.Workbook!.HasDigitalSignatures, Is.False);
        Assert.That(operations.EnumerateSignatures(reopened.Workbook).Signatures, Is.Empty);
    }

    [Test]
    public void AuthorSignature_ReportsMissingCertificateUnsupportedAlgorithmAndRelationshipTransform()
    {
        IXlsxPackageSignatureOperations operations = new XlsxPackageSignatureOperations();
        XlsxSignatureAuthoringOptions missingCertificate = new XlsxSignatureAuthoringOptions();
        missingCertificate.Algorithm = XlsxSignatureAlgorithm.Unknown;
        XlsxSignatureAuthoringResult invalidOptions = operations.AuthorSignature(XlsxWorkbook.Create(), missingCertificate);

        using X509Certificate2 certificate = CreateSigningCertificate();
        using RSA privateKey = CreateSigningKey();
        XlsxSignatureAuthoringOptions relationshipOptions = new XlsxSignatureAuthoringOptions();
        relationshipOptions.Certificate = certificate;
        relationshipOptions.PrivateKey = privateKey;
        relationshipOptions.PartPaths.Add(XlsxPackagePaths.WorkbookRelationships);
        XlsxSignatureAuthoringResult unsupportedRelationship = operations.AuthorSignature(XlsxWorkbook.Create(), relationshipOptions);

        using RSA wrongPrivateKey = RSA.Create(2048);
        XlsxSignatureAuthoringOptions mismatchedOptions = new XlsxSignatureAuthoringOptions();
        mismatchedOptions.Certificate = certificate;
        mismatchedOptions.PrivateKey = wrongPrivateKey;
        XlsxSignatureAuthoringResult mismatchedKey = operations.AuthorSignature(XlsxWorkbook.Create(), mismatchedOptions);

        Assert.That(invalidOptions.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidOptions.Validation, "signature.author.certificateMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidOptions.Validation, "signature.author.algorithmUnsupported"), Is.True);
        Assert.That(unsupportedRelationship.IsValid, Is.False);
        Assert.That(HasDiagnostic(unsupportedRelationship.Validation, "signature.author.relationshipTransformUnsupported"), Is.True);
        Assert.That(mismatchedKey.IsValid, Is.False);
        Assert.That(HasDiagnostic(mismatchedKey.Validation, "signature.author.privateKeyMismatch"), Is.True);
    }

    [Test]
    public void AuthorSignature_RejectsCertificateThatDisallowsDigitalSigning()
    {
        using RSA privateKey = RSA.Create(2048);
        using X509Certificate2 certificate = CreateCertificate(privateKey, X509KeyUsageFlags.KeyEncipherment);
        XlsxSignatureAuthoringOptions options = new XlsxSignatureAuthoringOptions();
        options.Certificate = certificate;
        options.PrivateKey = privateKey;

        XlsxSignatureAuthoringResult authored = new XlsxPackageSignatureOperations().AuthorSignature(XlsxWorkbook.Create(), options);

        Assert.That(authored.IsValid, Is.False);
        Assert.That(authored.Signature, Is.Null);
        Assert.That(HasDiagnostic(authored.Validation, "signature.author.certificateInvalid"), Is.True);
    }

    [Test]
    public void EnumerateSignatures_ReportsExternalSignedPartReferenceAsUnsupported()
    {
        using RSA privateKey = CreateSigningKey();
        using X509Certificate2 certificate = CreateSigningCertificate();
        XlsxWorkbookOpenResult open = CreateSignedOpenWorkbook(certificate, privateKey);
        XlsxDigitalSignaturePart part = FindSignaturePart(open.Workbook!)!;
        string xml = Encoding.UTF8.GetString(part.Data);
        const string marker = "<ds:Manifest><ds:Reference URI=\"";
        int uriStart = xml.IndexOf(marker, System.StringComparison.Ordinal) + marker.Length;
        int uriEnd = xml.IndexOf('"', uriStart);
        xml = xml.Substring(0, uriStart) + "https://example.invalid/workbook.xml" + xml.Substring(uriEnd);
        part.Data = Encoding.UTF8.GetBytes(xml);

        XlsxSignatureEnumerationResult enumeration = new XlsxPackageSignatureOperations().EnumerateSignatures(open.Workbook!);

        Assert.That(enumeration.IsValid, Is.False);
        Assert.That(enumeration.Signatures, Has.Count.EqualTo(1));
        Assert.That(enumeration.Signatures[0].SignedParts[0].IsSupported, Is.False);
        Assert.That(HasDiagnostic(enumeration.Validation, "signature.reference.uriUnsupported"), Is.True);
    }

    static XlsxWorkbookOpenResult CreateSignedOpenWorkbook(X509Certificate2 certificate, RSA privateKey)
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxSignatureAuthoringOptions options = new XlsxSignatureAuthoringOptions();
        options.Certificate = certificate;
        options.PrivateKey = privateKey;
        XlsxSignatureAuthoringResult authored = new XlsxPackageSignatureOperations().AuthorSignature(workbook, options);
        Assert.That(authored.IsValid, Is.True);
        XlsxWorkbookSaveResult save = workbook.SaveBytes();
        Assert.That(save.IsValid, Is.True);
        XlsxWorkbookOpenResult open = XlsxWorkbook.OpenBytes(save.Bytes);
        Assert.That(open.IsValid, Is.True);
        return open;
    }

    static X509Certificate2 CreateSigningCertificate()
    {
        return X509CertificateLoader.LoadCertificate(Convert.FromBase64String(SigningCertificateDer));
    }

    static X509Certificate2 CreateCertificate(RSA privateKey, X509KeyUsageFlags usage)
    {
        CertificateRequest request = new CertificateRequest("CN=xlsx generated signer", privateKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(usage, true));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    }

    static RSA CreateSigningKey()
    {
        RSA privateKey = RSA.Create();
        privateKey.ImportPkcs8PrivateKey(Convert.FromBase64String(SigningPrivateKeyDer), out int bytesRead);
        Assert.That(bytesRead, Is.GreaterThan(0));
        return privateKey;
    }

    const string SigningCertificateDer = "MIIDFzCCAf+gAwIBAgIUNgm3oriKuReMeX3Zh1FrcT3eX/AwDQYJKoZIhvcNAQELBQAwGzEZMBcGA1UEAwwQeGxzeCB0ZXN0IHNpZ25lcjAeFw0yNjA4MjcwODI4NTBaFw0zNjA4MjQwODI4NTBaMBsxGTAXBgNVBAMMEHhsc3ggdGVzdCBzaWduZXIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDEsJvDOQ4/jjc4RXvj/1HnuaVQHmI+AAtd4fKl5VYRhjhGz62+r7XcUD8jqtbMxlIvRe1Fxx2X7x6TBBQxCbK7jmcXtzkZUaOCfoClqRlojBh5HxdClIxeuoukfOojDHMQBAvZO/NId2Qa6PaeGG5UngiZeZU1n40WBP+DadBSWdExqhD7b881OvtW9WnRYDeglEit4Moyj0/1GxhmnFVZbmHcu2tK7TglGeb2n8Ggf9VH/N443pq/bhrs2qKXcMeK+FEpwS0nW4NoLxndjs4nNcOM28HEIS+zrp+UI2d6PjzZ7rLuLwOVmj6Uq6llPkWaL9nxmW01fPruIbT4iEBDAgMBAAGjUzBRMB0GA1UdDgQWBBRvUravCzpr7aosKaV8anw0mUbX9jAfBgNVHSMEGDAWgBRvUravCzpr7aosKaV8anw0mUbX9jAPBgNVHRMBAf8EBTADAQH/MA0GCSqGSIb3DQEBCwUAA4IBAQCxHXVUE/ZE/IzCM7zyraKeLJ54BmP89P+6xkYriuDVTVaVgvwS3+sAYh7/NAUMEagrAlNuUSFFKWBCRtndcNkcjM6xZLcTXbwmuxwCIgmKM4DYSuHzDXUp4ZVOuTLPV+cBEjxYqs0W6Lc5cesTpb+odY54KCou7v31GaZTxPG9Yv62pMSo1HjoiHkH4tdRBLfV/TY7otwyXbVSVVBD5PNvehuvictVhTg3HfN7ixFBk5zWiwBjLohEDFIygi2FB9AKo83TRS/tQFBPHnMEXBjn7EsXv1AB/AN+O2x+0721on1aYJYIypOGacMICEk7hL434HfJuxXHAdZPamFf6sAU";
    const string SigningPrivateKeyDer = "MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQDEsJvDOQ4/jjc4RXvj/1HnuaVQHmI+AAtd4fKl5VYRhjhGz62+r7XcUD8jqtbMxlIvRe1Fxx2X7x6TBBQxCbK7jmcXtzkZUaOCfoClqRlojBh5HxdClIxeuoukfOojDHMQBAvZO/NId2Qa6PaeGG5UngiZeZU1n40WBP+DadBSWdExqhD7b881OvtW9WnRYDeglEit4Moyj0/1GxhmnFVZbmHcu2tK7TglGeb2n8Ggf9VH/N443pq/bhrs2qKXcMeK+FEpwS0nW4NoLxndjs4nNcOM28HEIS+zrp+UI2d6PjzZ7rLuLwOVmj6Uq6llPkWaL9nxmW01fPruIbT4iEBDAgMBAAECggEADnBUjulCVYIQJa5cIOmeTp9/Njl0xs5kBzlOAcWrItAyk2gtfgESHaE6cVjaOeAZKmU8vNGYJ44jNXGoNc1bdI9InCIUK/qlQTAeK4sVVWprZYDVyUp0X4PcOm/9FCOW0scpQ4I2Isy46YzOhd6PQuDzCqk1PeF/ju1BAJmk9FaIgCC7QHIBXCp4KR794zeRzHQqi2beXkQS0iy188k+V0I9GAkOqny4jAgrqYStOB7vRDadoqqKo4vseMdq2Mha6xAmp4RRpPLFwlgKgalPSUS9LtBImDTzQGPHp1jvm0posoPbtxS+1w5rpc5U+OfO5EYDkaQkqb9jkZ4ATZ3j6QKBgQD2oBkF+o1h9tMmNkMPMZ8W7H47QKNFpIHTu76Inf2TUKIyLLhrr11BtliR48Hqe/FJlHmnaw6L6kCCXNTnmHfH9VK5wzV9pr1tM2cX1YhbhZaDP6tbmLxuYOmj64sLodVQqIPfYdK5QdcL2yEZrLYl3Al2KYCWGSsA6gqLFgygtwKBgQDMKpcWwPbDiyU/aaQzBg4673jQGNM2sxB14PL7xr9RCjQW4F9xSpt0WAyJSma2zRGw8PpcyCmz8WdGr6LG9Tlma3/uypWgpGgXvjynv1kkHR/WzqCnRi7hf4TBLCfRsk9Gfy7KqktEUynOMkdroYC9CAMsFnCENoz4odT5DpW41QKBgQCtIoduow5OD7D7ymwZZoSA21Vi02XLpkleExcXYZJ6sMis4Kg3MM0PLKvQ/0tLCefB9/HPjmCP6MuQuQrd6zNrtNV2g4wxwbSJzOQ63zTyyz12tmPd0cO22WIsKVdbuyi9jw/E6i+8znPhZtw94fmdanNZ+JjExrZh1evA6KHE0QKBgCfqh8GWFmiisrSUJdsn6u5Q1XDx+ZFvLt+tDDR3Zf4lHYv/cAmJSZ1UfZb16eEBdN0AMCKlq6mXn9ZtDkkRtpg0mj2Ic/qFSiiiM9+kBHyQCSKJIVWy3gWOVdkileJklyU0RmSvKKoCDGFY6Se82puajmw3MuZWd/dARJAw7srFAoGBAMdJ2i4EQ91aS/DyuMorsrp6geAvLYgykVrcdIjL3bi0pLs/RqTwCeccWZv1F5WzKzLh3gmsnAmVl3T3CgtR6RqUi9R1I1MmEuVq0kWRsH02ZC3ARp8vjmtTyqmTnzEF2dFYBqV+MgFuU9ZUN1Utruv/lWPzDKsMn7cmMDnve19+";

    const string SigningCertificatePfx = "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";

    static XlsxSignatureReference? FindReference(XlsxPackageSignature signature, string partPath)
    {
        foreach (XlsxSignatureReference reference in signature.SignedParts)
        {
            if (reference.PartPath == partPath)
            {
                return reference;
            }
        }

        return null;
    }

    static XlsxPackagePart? FindPackagePart(XlsxWorkbook workbook, string partPath)
    {
        foreach (XlsxPackagePart part in workbook.PackageParts)
        {
            if (part.Path == partPath)
            {
                return part;
            }
        }

        return null;
    }

    static XlsxDigitalSignaturePart? FindSignaturePart(XlsxWorkbook workbook)
    {
        foreach (XlsxDigitalSignaturePart part in workbook.DigitalSignatures)
        {
            if (part.IsSignatureXmlPart)
            {
                return part;
            }
        }

        return null;
    }

    static bool HasDiagnostic(XlsxValidationResult validation, string code)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
