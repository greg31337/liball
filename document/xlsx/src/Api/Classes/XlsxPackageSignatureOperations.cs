using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Enumerates, validates, removes, and authors supported OPC package digital signatures.
/// </summary>
/// <remarks>
/// Authoring supports detached package-part references using RSA with SHA-256, SHA-384, or SHA-512. Relationship transforms and external references are reported as unsupported.
/// </remarks>
public class XlsxPackageSignatureOperations : IXlsxPackageSignatureOperations
{
    internal const string XmlSignatureNamespace = "http://www.w3.org/2000/09/xmldsig#";
    internal const string PackageSignatureNamespace = "http://schemas.openxmlformats.org/package/2006/digital-signature";
    internal const string Canonicalization = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
    internal const string ExclusiveCanonicalization = "http://www.w3.org/2001/10/xml-exc-c14n#";
    internal const string Sha256Digest = "http://www.w3.org/2001/04/xmlenc#sha256";
    internal const string Sha384Digest = "http://www.w3.org/2001/04/xmldsig-more#sha384";
    internal const string Sha512Digest = "http://www.w3.org/2001/04/xmlenc#sha512";
    internal const string RsaSha256Signature = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
    internal const string RsaSha384Signature = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha384";
    internal const string RsaSha512Signature = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512";

    /// <summary>
    /// Initializes a new package signature operations service.
    /// </summary>
    public XlsxPackageSignatureOperations()
    {
    }

    /// <summary>
    /// Enumerates typed signatures and signed package part references.
    /// </summary>
    /// <param name="workbook">The workbook package model to inspect.</param>
    /// <returns>The signatures and enumeration diagnostics.</returns>
    public XlsxSignatureEnumerationResult EnumerateSignatures(XlsxWorkbook workbook)
    {
        XlsxSignatureEnumerationResult result = new XlsxSignatureEnumerationResult();
        if (workbook == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.enumerate.workbookNull", "Workbook must not be null."));
            return result;
        }

        if (workbook.DigitalSignatures == null)
        {
            return result;
        }

        foreach (XlsxDigitalSignaturePart part in workbook.DigitalSignatures)
        {
            if (part == null || !part.IsSignatureXmlPart)
            {
                continue;
            }

            XlsxPackageSignature signature = ParseSignature(part);
            result.Signatures.Add(signature);
            CopyEnumerationDiagnostics(result, signature.Validation);
        }

        result.IsValid = result.Validation.IsValid;
        return result;
    }

    /// <summary>
    /// Validates signature values, part references, certificates, and optionally certificate trust.
    /// </summary>
    /// <param name="workbook">The workbook package model to validate.</param>
    /// <param name="evaluateTrust">Whether to build certificate chains against the current trust store.</param>
    /// <returns>The independent validation outcomes.</returns>
    public XlsxSignatureValidationResult ValidateSignatures(XlsxWorkbook workbook, bool evaluateTrust)
    {
        XlsxSignatureValidationResult result = new XlsxSignatureValidationResult();
        if (workbook == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.validate.workbookNull", "Workbook must not be null."));
            return result;
        }

        XlsxSignatureEnumerationResult enumeration = EnumerateSignatures(workbook);
        CopyValidationDiagnostics(result, enumeration.Validation);
        result.Signatures = enumeration.Signatures;
        result.HasSignatures = result.Signatures.Count > 0;
        result.TrustEvaluated = evaluateTrust;
        if (!result.HasSignatures)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.validate.none", "Workbook does not contain a package signature."));
            return result;
        }

        result.AllCryptographicSignaturesValid = true;
        result.AllReferencesValid = true;
        result.AllCertificatesValid = true;
        result.AllCertificatesTrusted = evaluateTrust;
        foreach (XlsxPackageSignature signature in result.Signatures)
        {
            ValidateSignature(workbook, signature, evaluateTrust);
            CopyValidationDiagnostics(result, signature.Validation);
            if (!signature.CryptographicSignatureValid)
            {
                result.AllCryptographicSignaturesValid = false;
            }

            if (!signature.ReferencesValid)
            {
                result.AllReferencesValid = false;
            }

            if (!signature.CertificateValid)
            {
                result.AllCertificatesValid = false;
            }

            if (evaluateTrust && !signature.CertificateTrusted)
            {
                result.AllCertificatesTrusted = false;
            }
        }

        result.IsValid = result.Validation.IsValid;
        return result;
    }

    /// <summary>
    /// Removes every package signature and its OPC signature infrastructure.
    /// </summary>
    /// <param name="workbook">The workbook package model to change.</param>
    /// <returns>Diagnostics produced by the removal operation.</returns>
    public XlsxValidationResult RemoveSignatures(XlsxWorkbook workbook)
    {
        XlsxValidationResult result = new XlsxValidationResult();
        if (workbook == null)
        {
            result.AddError("signature.remove.workbookNull", "Workbook must not be null.");
            return result;
        }

        if (workbook.PackageRelationships != null && workbook.PackageRelationships.Relationships != null)
        {
            int relationshipIndex = 0;
            while (relationshipIndex < workbook.PackageRelationships.Relationships.Count)
            {
                XlsxRelationship relationship = workbook.PackageRelationships.Relationships[relationshipIndex];
                if (relationship != null && relationship.Type == XlsxRelationshipTypes.DigitalSignatureOrigin)
                {
                    workbook.PackageRelationships.Relationships.RemoveAt(relationshipIndex);
                    continue;
                }

                relationshipIndex = relationshipIndex + 1;
            }
        }

        workbook.RemoveUnknownPartsByPrefix(XlsxPackagePaths.DigitalSignaturesDirectory);
        if (workbook.DigitalSignatures != null)
        {
            workbook.DigitalSignatures.Clear();
        }

        workbook.HasDigitalSignatures = false;
        RemoveEnumeratedSignatureParts(workbook);
        RemoveEnumeratedSignatureRelationships(workbook);
        return result;
    }

    /// <summary>
    /// Authors a supported RSA and SHA-2 OPC signature over selected package parts.
    /// </summary>
    /// <param name="workbook">The workbook package model to sign.</param>
    /// <param name="options">The certificate, algorithm, identifiers, and parts to sign.</param>
    /// <returns>The authored signature and diagnostics.</returns>
    public XlsxSignatureAuthoringResult AuthorSignature(XlsxWorkbook workbook, XlsxSignatureAuthoringOptions options)
    {
        XlsxSignatureAuthoringResult result = new XlsxSignatureAuthoringResult();
        if (!ValidateAuthoringInput(workbook, options, result))
        {
            return result;
        }

        XlsxWorkbookSaveResult packageResult = new XlsxWorkbookSaveResult();
        List<XlsxPackageEntry> entries = workbook.BuildPackageEntries(packageResult);
        CopyAuthoringDiagnostics(result, packageResult.Validation);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxContentTypesPart contentTypes = ReadEntryContentTypes(entries, result);
        List<XlsxPackageEntry> selectedEntries = SelectEntries(entries, options, result);
        if (!result.IsValid)
        {
            return result;
        }

        if (selectedEntries.Count == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.partsEmpty", "At least one supported package part must be selected for signing."));
            return result;
        }

        selectedEntries.Sort(CompareEntriesByPath);
        List<XlsxSignatureReference> references = CreateReferences(selectedEntries, contentTypes, options.Algorithm);
        string objectXml = BuildPackageObjectXml(references, options.SignatureId);
        byte[] objectDigest = HashData(CanonicalizeXml(objectXml, Canonicalization), options.Algorithm);
        string signedInfoXml = BuildSignedInfoXml(objectDigest, options.Algorithm);
        byte[] signedInfoBytes = CanonicalizeXml(signedInfoXml, Canonicalization);
        X509Certificate2? signingCertificate = options.Certificate;
        if (signingCertificate == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.certificateMissing", "A signing certificate with an RSA private key is required."));
            return result;
        }

        byte[] signatureValue;
        if (options.PrivateKey != null)
        {
            signatureValue = SignData(signedInfoBytes, options.PrivateKey, options.Algorithm);
        }
        else
        {
            using (RSA? privateKey = signingCertificate.GetRSAPrivateKey())
            {
                if (privateKey == null)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.privateKeyMissing", "An RSA private key is required."));
                    return result;
                }

                signatureValue = SignData(signedInfoBytes, privateKey, options.Algorithm);
            }
        }

        using (RSA? publicKey = signingCertificate.GetRSAPublicKey())
        {
            if (publicKey == null || !publicKey.VerifyData(signedInfoBytes, signatureValue, GetHashAlgorithm(options.Algorithm), RSASignaturePadding.Pkcs1))
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.privateKeyMismatch", "The RSA private key does not match the signing certificate."));
                return result;
            }
        }

        string signatureXml = BuildSignatureXml(options, signingCertificate, signedInfoXml, signatureValue, objectXml);
        AddSignatureInfrastructure(workbook, options, signatureXml, entries, contentTypes);

        XlsxDigitalSignaturePart signaturePart = new XlsxDigitalSignaturePartReader().ReadString(options.SignaturePartPath, XlsxContentTypes.DigitalSignatureXml, signatureXml);
        XlsxPackageSignature signature = ParseSignature(signaturePart);
        result.Signature = signature;
        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal static bool ValidateAuthoringInput(XlsxWorkbook workbook, XlsxSignatureAuthoringOptions options, XlsxSignatureAuthoringResult result)
    {
        if (workbook == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.workbookNull", "Workbook must not be null."));
            return false;
        }

        if (options == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.optionsNull", "Signature authoring options must not be null."));
            return false;
        }

        if (workbook.HasDigitalSignatures || (workbook.DigitalSignatures != null && workbook.DigitalSignatures.Count > 0))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.alreadySigned", "Existing package signatures must be removed before authoring a new signature."));
        }

        if (options.Certificate == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.certificateMissing", "A signing certificate with an RSA private key is required."));
        }
        else
        {
            if (!IsCertificateCurrentlyValid(options.Certificate) || !AllowsDigitalSignature(options.Certificate))
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.certificateInvalid", "The signing certificate must be currently valid and allow digital signatures."));
            }

            using (RSA? publicKey = options.Certificate.GetRSAPublicKey())
            {
                if (publicKey == null)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.certificateKeyUnsupported", "The signing certificate must contain an RSA public key."));
                }
            }

            if (options.PrivateKey == null && !options.Certificate.HasPrivateKey)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.privateKeyMissing", "The signing certificate must contain an RSA private key."));
            }
        }

        if (GetSignatureMethod(options.Algorithm).Length == 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.algorithmUnsupported", "The requested signature algorithm is not supported."));
        }

        if (!IsValidXmlId(options.SignatureId))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.idInvalid", "SignatureId must be a non-empty XML identifier beginning with a letter or underscore."));
        }

        XlsxPackagePathResult signaturePath = XlsxPackagePath.NormalizePartPath(options.SignaturePartPath);
        if (!signaturePath.Validation.IsValid || !signaturePath.Path.StartsWith(XlsxPackagePaths.DigitalSignaturesDirectory, System.StringComparison.Ordinal) || !signaturePath.Path.EndsWith(".xml", System.StringComparison.Ordinal))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.partPathInvalid", "SignaturePartPath must identify an XML part under /_xmlsignatures/."));
        }
        else
        {
            options.SignaturePartPath = signaturePath.Path;
        }

        result.IsValid = result.Validation.IsValid;
        return result.IsValid;
    }

    internal static XlsxContentTypesPart ReadEntryContentTypes(List<XlsxPackageEntry> entries, XlsxSignatureAuthoringResult result)
    {
        foreach (XlsxPackageEntry entry in entries)
        {
            if (entry.Path == XlsxPackagePaths.ContentTypes)
            {
                XlsxContentTypesPart contentTypes = new XlsxContentTypesReader().ReadBytes(entry.Data);
                CopyAuthoringDiagnostics(result, contentTypes.Validation);
                return contentTypes;
            }
        }

        result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.contentTypesMissing", "The package content types part could not be composed."));
        return new XlsxContentTypesPart();
    }

    internal static List<XlsxPackageEntry> SelectEntries(List<XlsxPackageEntry> entries, XlsxSignatureAuthoringOptions options, XlsxSignatureAuthoringResult result)
    {
        List<XlsxPackageEntry> selected = new List<XlsxPackageEntry>();
        if (options.PartPaths == null || options.PartPaths.Count == 0)
        {
            foreach (XlsxPackageEntry entry in entries)
            {
                if (IsDefaultSignablePart(entry.Path))
                {
                    selected.Add(entry);
                }
            }

            return selected;
        }

        Dictionary<string, bool> seen = new Dictionary<string, bool>();
        foreach (string requestedPath in options.PartPaths)
        {
            XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(requestedPath);
            if (!normalized.Validation.IsValid)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.partPathInvalid", "A selected package part path is invalid."));
                continue;
            }

            if (XlsxWorkbook.IsRelationshipPartPath(normalized.Path))
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.relationshipTransformUnsupported", "Authoring signatures over relationship parts is not supported."));
                continue;
            }

            if (seen.ContainsKey(normalized.Path))
            {
                continue;
            }

            XlsxPackageEntry? entry = FindEntry(entries, normalized.Path);
            if (entry == null)
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.author.partMissing", "A selected package part does not exist."));
                continue;
            }

            seen.Add(normalized.Path, true);
            selected.Add(entry);
        }

        return selected;
    }

    internal static bool IsDefaultSignablePart(string path)
    {
        if (path == XlsxPackagePaths.ContentTypes || XlsxWorkbook.IsRelationshipPartPath(path))
        {
            return false;
        }

        return !path.StartsWith(XlsxPackagePaths.DigitalSignaturesDirectory, System.StringComparison.Ordinal);
    }

    internal static List<XlsxSignatureReference> CreateReferences(List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes, XlsxSignatureAlgorithm algorithm)
    {
        List<XlsxSignatureReference> references = new List<XlsxSignatureReference>();
        foreach (XlsxPackageEntry entry in entries)
        {
            XlsxSignatureReference reference = new XlsxSignatureReference();
            reference.PartPath = entry.Path;
            reference.ContentType = contentTypes.GetContentType(entry.Path);
            reference.Uri = entry.Path + "?ContentType=" + Uri.EscapeDataString(reference.ContentType == null ? "" : reference.ContentType);
            reference.DigestMethod = GetDigestMethod(algorithm);
            reference.DigestValue = HashData(entry.Data, algorithm);
            reference.PartExists = true;
            reference.IsSupported = true;
            reference.DigestValid = true;
            references.Add(reference);
        }

        return references;
    }

    internal static string BuildPackageObjectXml(List<XlsxSignatureReference> references, string signatureId)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("<ds:Object xmlns:ds=\"");
        builder.Append(XmlSignatureNamespace);
        builder.Append("\" xmlns:pds=\"");
        builder.Append(PackageSignatureNamespace);
        builder.Append("\" Id=\"idPackageObject\"><ds:Manifest>");
        foreach (XlsxSignatureReference reference in references)
        {
            builder.Append("<ds:Reference URI=\"");
            builder.Append(EscapeXmlAttribute(reference.Uri));
            builder.Append("\"><ds:DigestMethod Algorithm=\"");
            builder.Append(reference.DigestMethod);
            builder.Append("\"></ds:DigestMethod><ds:DigestValue>");
            builder.Append(Convert.ToBase64String(reference.DigestValue));
            builder.Append("</ds:DigestValue></ds:Reference>");
        }

        builder.Append("</ds:Manifest><ds:SignatureProperties><ds:SignatureProperty Id=\"idSignatureTime\" Target=\"#");
        builder.Append(EscapeXmlAttribute(signatureId));
        builder.Append("\"><pds:SignatureTime><pds:Format>YYYY-MM-DDThh:mm:ssTZD</pds:Format><pds:Value>");
        builder.Append(DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
        builder.Append("</pds:Value></pds:SignatureTime></ds:SignatureProperty></ds:SignatureProperties></ds:Object>");
        return builder.ToString();
    }

    internal static string BuildSignedInfoXml(byte[] objectDigest, XlsxSignatureAlgorithm algorithm)
    {
        return "<ds:SignedInfo xmlns:ds=\"" + XmlSignatureNamespace + "\"><ds:CanonicalizationMethod Algorithm=\"" + Canonicalization + "\"></ds:CanonicalizationMethod><ds:SignatureMethod Algorithm=\"" + GetSignatureMethod(algorithm) + "\"></ds:SignatureMethod><ds:Reference URI=\"#idPackageObject\" Type=\"" + XmlSignatureNamespace + "Object\"><ds:Transforms><ds:Transform Algorithm=\"" + Canonicalization + "\"></ds:Transform></ds:Transforms><ds:DigestMethod Algorithm=\"" + GetDigestMethod(algorithm) + "\"></ds:DigestMethod><ds:DigestValue>" + Convert.ToBase64String(objectDigest) + "</ds:DigestValue></ds:Reference></ds:SignedInfo>";
    }

    internal static string BuildSignatureXml(XlsxSignatureAuthoringOptions options, X509Certificate2 certificate, string signedInfoXml, byte[] signatureValue, string objectXml)
    {
        return "<ds:Signature xmlns:ds=\"" + XmlSignatureNamespace + "\" Id=\"" + EscapeXmlAttribute(options.SignatureId) + "\">" + signedInfoXml + "<ds:SignatureValue>" + Convert.ToBase64String(signatureValue) + "</ds:SignatureValue><ds:KeyInfo><ds:X509Data><ds:X509Certificate>" + Convert.ToBase64String(certificate.RawData) + "</ds:X509Certificate></ds:X509Data></ds:KeyInfo>" + objectXml + "</ds:Signature>";
    }

    internal static void AddSignatureInfrastructure(XlsxWorkbook workbook, XlsxSignatureAuthoringOptions options, string signatureXml, List<XlsxPackageEntry> entries, XlsxContentTypesPart contentTypes)
    {
        byte[] signatureBytes = Encoding.UTF8.GetBytes(signatureXml);
        XlsxRelationshipsPart originRelationships = new XlsxRelationshipsPart();
        originRelationships.SourcePartPath = XlsxPackagePaths.DigitalSignatureOrigin;
        originRelationships.PartPath = XlsxPackagePath.GetRelationshipPartPath(XlsxPackagePaths.DigitalSignatureOrigin);
        XlsxRelationship signatureRelationship = new XlsxRelationship();
        signatureRelationship.Id = "rIdSignature1";
        signatureRelationship.Type = XlsxRelationshipTypes.DigitalSignature;
        int slashIndex = options.SignaturePartPath.LastIndexOf('/');
        signatureRelationship.Target = options.SignaturePartPath.Substring(slashIndex + 1);
        originRelationships.Relationships.Add(signatureRelationship);
        XlsxRelationshipsWriteResult relationshipWrite = new XlsxPartRelationshipsWriter().WriteBytes(originRelationships);

        workbook.UnknownParts.AddPart(XlsxPackagePaths.DigitalSignatureOrigin, XlsxContentTypes.DigitalSignatureOrigin, new byte[0]);
        workbook.UnknownParts.AddPart(originRelationships.PartPath, XlsxContentTypes.Relationships, relationshipWrite.Bytes);
        workbook.UnknownParts.AddPart(options.SignaturePartPath, XlsxContentTypes.DigitalSignatureXml, signatureBytes);
        XlsxWorkbook.AddPackageRelationshipIfMissing(workbook.PackageRelationships, "rIdSignatureOrigin", XlsxRelationshipTypes.DigitalSignatureOrigin, "_xmlsignatures/origin.sigs");

        workbook.DigitalSignatures.Clear();
        workbook.DigitalSignatures.Add(new XlsxDigitalSignaturePartReader().ReadBytes(XlsxPackagePaths.DigitalSignatureOrigin, XlsxContentTypes.DigitalSignatureOrigin, new byte[0]));
        workbook.DigitalSignatures.Add(new XlsxDigitalSignaturePartReader().ReadBytes(options.SignaturePartPath, XlsxContentTypes.DigitalSignatureXml, signatureBytes));
        workbook.HasDigitalSignatures = true;
        workbook.ContentTypes = contentTypes;
        workbook.PackageParts = XlsxWorkbook.EnumeratePackageParts(CreatePackageReadResult(entries), contentTypes);
    }

    internal static XlsxZipPackageReadResult CreatePackageReadResult(List<XlsxPackageEntry> entries)
    {
        XlsxZipPackageReadResult package = new XlsxZipPackageReadResult();
        foreach (XlsxPackageEntry entry in entries)
        {
            package.Entries.Add(entry);
        }

        return package;
    }

    internal static XlsxPackageSignature ParseSignature(XlsxDigitalSignaturePart part)
    {
        XlsxPackageSignature signature = new XlsxPackageSignature();
        signature.PartPath = part.PartPath;
        if (part.Data == null || part.Data.Length == 0)
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.xml.empty", "Signature XML must not be empty."));
            return signature;
        }

        XmlDocument document = LoadXmlDocument(part.Data);
        XmlElement? root = document.DocumentElement;
        if (root == null || root.LocalName != "Signature" || root.NamespaceURI != XmlSignatureNamespace)
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.xml.rootInvalid", "Signature XML must contain a Signature root in the XML signature namespace."));
            return signature;
        }

        signature.Id = root.GetAttribute("Id");
        XmlElement? signedInfo = FindDirectChild(root, "SignedInfo", XmlSignatureNamespace);
        XmlElement? signatureValue = FindDirectChild(root, "SignatureValue", XmlSignatureNamespace);
        XmlElement? certificate = FindDescendant(root, "X509Certificate", XmlSignatureNamespace);
        XmlElement? packageObject = FindPackageObject(root);
        if (signedInfo == null)
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.signedInfoMissing", "Signature XML is missing SignedInfo."));
        }
        else
        {
            ReadSignedInfo(signature, signedInfo);
        }

        signature.SignatureValue = ReadBase64Element(signatureValue, signature, "signature.valueInvalid", "SignatureValue is missing or is not valid base64.");
        signature.CertificateData = ReadBase64Element(certificate, signature, "signature.certificateInvalid", "An embedded X.509 certificate is missing or is not valid base64.");
        if (packageObject == null)
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.packageObjectMissing", "Signature XML is missing the OPC package Object."));
        }
        else
        {
            ReadManifest(signature, packageObject);
        }

        signature.IsValid = signature.Validation.IsValid;
        return signature;
    }

    internal static void ReadSignedInfo(XlsxPackageSignature signature, XmlElement signedInfo)
    {
        XmlElement? canonicalization = FindDirectChild(signedInfo, "CanonicalizationMethod", XmlSignatureNamespace);
        XmlElement? signatureMethod = FindDirectChild(signedInfo, "SignatureMethod", XmlSignatureNamespace);
        if (canonicalization != null)
        {
            signature.CanonicalizationMethod = canonicalization.GetAttribute("Algorithm");
        }

        if (signatureMethod != null)
        {
            signature.SignatureMethod = signatureMethod.GetAttribute("Algorithm");
            signature.Algorithm = GetAlgorithm(signature.SignatureMethod);
        }

        if (!IsSupportedCanonicalization(signature.CanonicalizationMethod))
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.canonicalizationUnsupported", "The SignedInfo canonicalization method is not supported."));
        }

        if (signature.Algorithm == XlsxSignatureAlgorithm.Unknown)
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.algorithmUnsupported", "The signature method is not supported."));
        }
    }

    internal static void ReadManifest(XlsxPackageSignature signature, XmlElement packageObject)
    {
        XmlElement? manifest = FindDirectChild(packageObject, "Manifest", XmlSignatureNamespace);
        if (manifest == null)
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.manifestMissing", "The OPC package Object is missing its Manifest."));
            return;
        }

        foreach (XmlNode node in manifest.ChildNodes)
        {
            XmlElement? element = node as XmlElement;
            if (element == null || element.LocalName != "Reference" || element.NamespaceURI != XmlSignatureNamespace)
            {
                continue;
            }

            XlsxSignatureReference reference = new XlsxSignatureReference();
            reference.Uri = element.GetAttribute("URI");
            bool uriSupported = ParseReferenceUri(reference);
            XmlElement? digestMethod = FindDirectChild(element, "DigestMethod", XmlSignatureNamespace);
            XmlElement? digestValue = FindDirectChild(element, "DigestValue", XmlSignatureNamespace);
            XmlElement? transform = FindDescendant(element, "Transform", XmlSignatureNamespace);
            if (digestMethod != null)
            {
                reference.DigestMethod = digestMethod.GetAttribute("Algorithm");
            }

            if (transform != null)
            {
                reference.TransformMethod = transform.GetAttribute("Algorithm");
            }

            reference.DigestValue = ReadBase64Element(digestValue, signature, "signature.reference.digestInvalid", "A signed part digest is missing or is not valid base64.");
            reference.IsSupported = uriSupported && IsSupportedDigest(reference.DigestMethod) && IsSupportedPartTransform(reference.TransformMethod);
            if (!uriSupported)
            {
                signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.reference.uriUnsupported", "A signed part reference URI is invalid or external."));
            }
            else if (!reference.IsSupported)
            {
                signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.reference.transformUnsupported", "A signed part reference uses an unsupported digest method or transform."));
            }

            signature.SignedParts.Add(reference);
        }

        if (signature.SignedParts.Count == 0)
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.manifest.partsMissing", "The signature Manifest does not reference any package parts."));
        }
    }

    internal static void ValidateSignature(XlsxWorkbook workbook, XlsxPackageSignature signature, bool evaluateTrust)
    {
        XlsxDigitalSignaturePart? part = FindDigitalSignaturePart(workbook, signature.PartPath);
        if (part == null)
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.partMissing", "The signature XML part is missing."));
            return;
        }

        XmlDocument document = LoadXmlDocument(part.Data);
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.xml.rootInvalid", "Signature XML must contain a Signature root."));
            return;
        }

        XmlElement? signedInfo = FindDirectChild(root, "SignedInfo", XmlSignatureNamespace);
        XmlElement? packageObject = FindPackageObject(root);
        ValidatePartReferences(workbook, signature);
        bool objectReferenceValid = ValidatePackageObjectReference(signature, signedInfo, packageObject);
        signature.ReferencesValid = objectReferenceValid && ArePartReferencesValid(signature.SignedParts);
        ValidateCertificateAndSignature(signature, signedInfo, evaluateTrust);
        signature.IsValid = signature.Validation.IsValid;
    }

    internal static void ValidatePartReferences(XlsxWorkbook workbook, XlsxPackageSignature signature)
    {
        foreach (XlsxSignatureReference reference in signature.SignedParts)
        {
            XlsxPackagePart? part = FindPackagePart(workbook.PackageParts, reference.PartPath);
            reference.PartExists = part != null;
            if (part == null)
            {
                signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.reference.partMissing", "A signed package part is missing."));
                continue;
            }

            if (!reference.IsSupported)
            {
                continue;
            }

            byte[] data = ApplyPartTransform(part.Data, reference.TransformMethod);
            byte[] digest = HashData(data, GetAlgorithmForDigest(reference.DigestMethod));
            reference.DigestValid = CryptographicOperations.FixedTimeEquals(digest, reference.DigestValue);
            if (!reference.DigestValid)
            {
                signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.reference.digestMismatch", "A signed package part digest does not match."));
            }
        }
    }

    internal static bool ValidatePackageObjectReference(XlsxPackageSignature signature, XmlElement? signedInfo, XmlElement? packageObject)
    {
        if (signedInfo == null || packageObject == null)
        {
            return false;
        }

        XmlElement? reference = FindReferenceByUri(signedInfo, "#" + packageObject.GetAttribute("Id"));
        if (reference == null)
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.packageObjectReferenceMissing", "SignedInfo does not reference the OPC package Object."));
            return false;
        }

        XmlElement? digestMethodElement = FindDirectChild(reference, "DigestMethod", XmlSignatureNamespace);
        XmlElement? digestValueElement = FindDirectChild(reference, "DigestValue", XmlSignatureNamespace);
        XmlElement? transformElement = FindDescendant(reference, "Transform", XmlSignatureNamespace);
        string digestMethod = digestMethodElement == null ? "" : digestMethodElement.GetAttribute("Algorithm");
        string transform = transformElement == null ? "" : transformElement.GetAttribute("Algorithm");
        if (!IsSupportedDigest(digestMethod) || !IsSupportedCanonicalization(transform))
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.packageObjectReferenceUnsupported", "The OPC package Object reference uses an unsupported digest or transform."));
            return false;
        }

        byte[] expected = ReadBase64Element(digestValueElement, signature, "signature.packageObjectDigestInvalid", "The OPC package Object digest is invalid.");
        byte[] canonical = CanonicalizeElement(packageObject, transform);
        byte[] actual = HashData(canonical, GetAlgorithmForDigest(digestMethod));
        bool valid = CryptographicOperations.FixedTimeEquals(actual, expected);
        if (!valid)
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.packageObjectDigestMismatch", "The OPC package Object digest does not match."));
        }

        return valid;
    }

    internal static void ValidateCertificateAndSignature(XlsxPackageSignature signature, XmlElement? signedInfo, bool evaluateTrust)
    {
        if (signature.CertificateData.Length == 0 || signedInfo == null)
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.certificateMissing", "A valid embedded signing certificate is required."));
            return;
        }

        using (X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(signature.CertificateData))
        {
            signature.CertificateSubject = certificate.Subject;
            signature.CertificateIssuer = certificate.Issuer;
            signature.CertificateThumbprint = certificate.Thumbprint;
            signature.CertificateValid = IsCertificateCurrentlyValid(certificate) && AllowsDigitalSignature(certificate);
            if (!signature.CertificateValid)
            {
                signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.certificateInvalid", "The signing certificate is expired, not yet valid, or disallows digital signatures."));
            }

            byte[] canonicalSignedInfo = CanonicalizeElement(signedInfo, signature.CanonicalizationMethod);
            using (RSA? rsa = certificate.GetRSAPublicKey())
            {
                if (rsa == null)
                {
                    signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.certificateKeyUnsupported", "The embedded certificate does not contain an RSA public key."));
                }
                else
                {
                    signature.CryptographicSignatureValid = rsa.VerifyData(canonicalSignedInfo, signature.SignatureValue, GetHashAlgorithm(signature.Algorithm), RSASignaturePadding.Pkcs1);
                    if (!signature.CryptographicSignatureValid)
                    {
                        signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "signature.valueMismatch", "The cryptographic signature value does not match SignedInfo."));
                    }
                }
            }

            signature.TrustEvaluated = evaluateTrust;
            if (evaluateTrust)
            {
                using (X509Chain chain = new X509Chain())
                {
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    signature.CertificateTrusted = chain.Build(certificate);
                }

                if (!signature.CertificateTrusted)
                {
                    signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Warning, "signature.certificateUntrusted", "The signing certificate chain does not terminate in a trusted root."));
                }
            }
        }
    }

    internal static bool IsCertificateCurrentlyValid(X509Certificate2 certificate)
    {
        DateTime now = DateTime.UtcNow;
        return now >= certificate.NotBefore.ToUniversalTime() && now <= certificate.NotAfter.ToUniversalTime();
    }

    internal static bool AllowsDigitalSignature(X509Certificate2 certificate)
    {
        foreach (X509Extension extension in certificate.Extensions)
        {
            if (extension.Oid != null && extension.Oid.Value == "2.5.29.15")
            {
                X509KeyUsageExtension usage = new X509KeyUsageExtension(extension, extension.Critical);
                return (usage.KeyUsages & X509KeyUsageFlags.DigitalSignature) != 0;
            }
        }

        return true;
    }

    internal static byte[] ApplyPartTransform(byte[] data, string transform)
    {
        if (transform == null || transform.Length == 0)
        {
            return data;
        }

        return CanonicalizeXml(Encoding.UTF8.GetString(data), transform);
    }

    internal static byte[] CanonicalizeXml(string xml, string algorithm)
    {
        XmlDocument document = LoadXmlDocument(Encoding.UTF8.GetBytes(xml));
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return new byte[0];
        }

        return CanonicalizeElement(root, algorithm);
    }

    internal static byte[] CanonicalizeElement(XmlElement element, string algorithm)
    {
        Transform transform;
        if (algorithm == ExclusiveCanonicalization)
        {
            transform = new XmlDsigExcC14NTransform();
        }
        else
        {
            transform = new XmlDsigC14NTransform();
        }

        XmlNodeList? nodes = element.SelectNodes(". | .//@* | .//node()");
        if (nodes == null)
        {
            return new byte[0];
        }

        transform.LoadInput(nodes);
        Stream? output = transform.GetOutput() as Stream;
        if (output == null)
        {
            return new byte[0];
        }

        using (output)
        {
            using (MemoryStream buffer = new MemoryStream())
            {
                output.CopyTo(buffer);
                return buffer.ToArray();
            }
        }
    }

    internal static XmlDocument LoadXmlDocument(byte[] bytes)
    {
        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.XmlResolver = null;
        using (MemoryStream stream = new MemoryStream(bytes))
        {
            using (XmlReader reader = XmlReader.Create(stream, XlsxXmlReaderHelpers.CreateSafeReaderSettings()))
            {
                document.Load(reader);
            }
        }

        return document;
    }

    internal static byte[] SignData(byte[] data, RSA privateKey, XlsxSignatureAlgorithm algorithm)
    {
        return privateKey.SignData(data, GetHashAlgorithm(algorithm), RSASignaturePadding.Pkcs1);
    }

    internal static byte[] HashData(byte[] data, XlsxSignatureAlgorithm algorithm)
    {
        if (algorithm == XlsxSignatureAlgorithm.RsaSha384)
        {
            return SHA384.HashData(data);
        }

        if (algorithm == XlsxSignatureAlgorithm.RsaSha512)
        {
            return SHA512.HashData(data);
        }

        return SHA256.HashData(data);
    }

    internal static HashAlgorithmName GetHashAlgorithm(XlsxSignatureAlgorithm algorithm)
    {
        if (algorithm == XlsxSignatureAlgorithm.RsaSha384)
        {
            return HashAlgorithmName.SHA384;
        }

        if (algorithm == XlsxSignatureAlgorithm.RsaSha512)
        {
            return HashAlgorithmName.SHA512;
        }

        return HashAlgorithmName.SHA256;
    }

    internal static string GetSignatureMethod(XlsxSignatureAlgorithm algorithm)
    {
        if (algorithm == XlsxSignatureAlgorithm.RsaSha256)
        {
            return RsaSha256Signature;
        }

        if (algorithm == XlsxSignatureAlgorithm.RsaSha384)
        {
            return RsaSha384Signature;
        }

        if (algorithm == XlsxSignatureAlgorithm.RsaSha512)
        {
            return RsaSha512Signature;
        }

        return "";
    }

    internal static string GetDigestMethod(XlsxSignatureAlgorithm algorithm)
    {
        if (algorithm == XlsxSignatureAlgorithm.RsaSha384)
        {
            return Sha384Digest;
        }

        if (algorithm == XlsxSignatureAlgorithm.RsaSha512)
        {
            return Sha512Digest;
        }

        if (algorithm == XlsxSignatureAlgorithm.RsaSha256)
        {
            return Sha256Digest;
        }

        return "";
    }

    internal static XlsxSignatureAlgorithm GetAlgorithm(string signatureMethod)
    {
        if (signatureMethod == RsaSha256Signature)
        {
            return XlsxSignatureAlgorithm.RsaSha256;
        }

        if (signatureMethod == RsaSha384Signature)
        {
            return XlsxSignatureAlgorithm.RsaSha384;
        }

        if (signatureMethod == RsaSha512Signature)
        {
            return XlsxSignatureAlgorithm.RsaSha512;
        }

        return XlsxSignatureAlgorithm.Unknown;
    }

    internal static XlsxSignatureAlgorithm GetAlgorithmForDigest(string digestMethod)
    {
        if (digestMethod == Sha384Digest)
        {
            return XlsxSignatureAlgorithm.RsaSha384;
        }

        if (digestMethod == Sha512Digest)
        {
            return XlsxSignatureAlgorithm.RsaSha512;
        }

        if (digestMethod == Sha256Digest)
        {
            return XlsxSignatureAlgorithm.RsaSha256;
        }

        return XlsxSignatureAlgorithm.Unknown;
    }

    internal static bool IsSupportedDigest(string digestMethod)
    {
        return GetAlgorithmForDigest(digestMethod) != XlsxSignatureAlgorithm.Unknown;
    }

    internal static bool IsSupportedCanonicalization(string algorithm)
    {
        return algorithm == Canonicalization || algorithm == ExclusiveCanonicalization;
    }

    internal static bool IsSupportedPartTransform(string transform)
    {
        return transform == null || transform.Length == 0 || IsSupportedCanonicalization(transform);
    }

    internal static bool IsValidXmlId(string value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        char first = value[0];
        if (!char.IsLetter(first) && first != '_')
        {
            return false;
        }

        int index = 1;
        while (index < value.Length)
        {
            char character = value[index];
            if (!char.IsLetterOrDigit(character) && character != '_' && character != '-' && character != '.')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsValidBase64(string text)
    {
        if (text == null || text.Length == 0 || text.Length % 4 != 0)
        {
            return false;
        }

        int paddingStart = text.Length;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            bool base64Character = character >= 'A' && character <= 'Z';
            base64Character = base64Character || character >= 'a' && character <= 'z';
            base64Character = base64Character || character >= '0' && character <= '9';
            base64Character = base64Character || character == '+' || character == '/';
            if (character == '=')
            {
                if (paddingStart == text.Length)
                {
                    paddingStart = index;
                }
            }
            else if (!base64Character || paddingStart != text.Length)
            {
                return false;
            }

            index = index + 1;
        }

        return text.Length - paddingStart <= 2;
    }

    internal static byte[] ReadBase64Element(XmlElement? element, XlsxPackageSignature signature, string code, string message)
    {
        if (element == null)
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message));
            return new byte[0];
        }

        string text = RemoveXmlWhitespace(element.InnerText);
        if (!IsValidBase64(text))
        {
            signature.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message));
            return new byte[0];
        }

        return Convert.FromBase64String(text);
    }

    internal static string RemoveXmlWhitespace(string text)
    {
        StringBuilder builder = new StringBuilder();
        foreach (char character in text)
        {
            if (!char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    internal static bool ParseReferenceUri(XlsxSignatureReference reference)
    {
        int queryIndex = reference.Uri.IndexOf('?');
        string path = queryIndex < 0 ? reference.Uri : reference.Uri.Substring(0, queryIndex);
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(path);
        if (!normalized.Validation.IsValid)
        {
            return false;
        }

        reference.PartPath = normalized.Path;

        if (queryIndex >= 0)
        {
            string query = reference.Uri.Substring(queryIndex + 1);
            const string prefix = "ContentType=";
            if (query.StartsWith(prefix, System.StringComparison.Ordinal))
            {
                reference.ContentType = Uri.UnescapeDataString(query.Substring(prefix.Length));
            }
        }

        return true;
    }

    internal static XmlElement? FindDirectChild(XmlElement parent, string localName, string namespaceUri)
    {
        foreach (XmlNode node in parent.ChildNodes)
        {
            XmlElement? element = node as XmlElement;
            if (element != null && element.LocalName == localName && element.NamespaceURI == namespaceUri)
            {
                return element;
            }
        }

        return null;
    }

    internal static XmlElement? FindDescendant(XmlElement parent, string localName, string namespaceUri)
    {
        XmlNodeList nodes = parent.GetElementsByTagName(localName, namespaceUri);
        if (nodes.Count == 0)
        {
            return null;
        }

        return nodes[0] as XmlElement;
    }

    internal static XmlElement? FindPackageObject(XmlElement root)
    {
        foreach (XmlNode node in root.ChildNodes)
        {
            XmlElement? element = node as XmlElement;
            if (element != null && element.LocalName == "Object" && element.NamespaceURI == XmlSignatureNamespace && FindDirectChild(element, "Manifest", XmlSignatureNamespace) != null)
            {
                return element;
            }
        }

        return null;
    }

    internal static XmlElement? FindReferenceByUri(XmlElement signedInfo, string uri)
    {
        foreach (XmlNode node in signedInfo.ChildNodes)
        {
            XmlElement? element = node as XmlElement;
            if (element != null && element.LocalName == "Reference" && element.NamespaceURI == XmlSignatureNamespace && element.GetAttribute("URI") == uri)
            {
                return element;
            }
        }

        return null;
    }

    internal static bool ArePartReferencesValid(List<XlsxSignatureReference> references)
    {
        if (references == null || references.Count == 0)
        {
            return false;
        }

        foreach (XlsxSignatureReference reference in references)
        {
            if (!reference.PartExists || !reference.IsSupported || !reference.DigestValid)
            {
                return false;
            }
        }

        return true;
    }

    internal static XlsxPackageEntry? FindEntry(List<XlsxPackageEntry> entries, string path)
    {
        foreach (XlsxPackageEntry entry in entries)
        {
            if (entry.Path == path)
            {
                return entry;
            }
        }

        return null;
    }

    internal static XlsxPackagePart? FindPackagePart(List<XlsxPackagePart> parts, string path)
    {
        if (parts == null)
        {
            return null;
        }

        foreach (XlsxPackagePart part in parts)
        {
            if (part.Path == path)
            {
                return part;
            }
        }

        return null;
    }

    internal static XlsxDigitalSignaturePart? FindDigitalSignaturePart(XlsxWorkbook workbook, string path)
    {
        foreach (XlsxDigitalSignaturePart part in workbook.DigitalSignatures)
        {
            if (part.PartPath == path)
            {
                return part;
            }
        }

        return null;
    }

    internal static int CompareEntriesByPath(XlsxPackageEntry left, XlsxPackageEntry right)
    {
        return string.Compare(left.Path, right.Path, System.StringComparison.Ordinal);
    }

    internal static string EscapeXmlAttribute(string value)
    {
        if (value == null)
        {
            return "";
        }

        return value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    internal static void RemoveEnumeratedSignatureParts(XlsxWorkbook workbook)
    {
        if (workbook.PackageParts == null)
        {
            return;
        }

        int index = 0;
        while (index < workbook.PackageParts.Count)
        {
            if (workbook.PackageParts[index].Path.StartsWith(XlsxPackagePaths.DigitalSignaturesDirectory, System.StringComparison.Ordinal))
            {
                workbook.PackageParts.RemoveAt(index);
                continue;
            }

            index = index + 1;
        }
    }

    internal static void RemoveEnumeratedSignatureRelationships(XlsxWorkbook workbook)
    {
        if (workbook.RelationshipParts == null)
        {
            return;
        }

        string signatureRelationshipPath = XlsxPackagePath.GetRelationshipPartPath(XlsxPackagePaths.DigitalSignatureOrigin);
        int index = 0;
        while (index < workbook.RelationshipParts.Count)
        {
            if (workbook.RelationshipParts[index].PartPath == signatureRelationshipPath)
            {
                workbook.RelationshipParts.RemoveAt(index);
                continue;
            }

            index = index + 1;
        }
    }

    internal static void CopyEnumerationDiagnostics(XlsxSignatureEnumerationResult target, XlsxValidationResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }

    internal static void CopyValidationDiagnostics(XlsxSignatureValidationResult target, XlsxValidationResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }

    internal static void CopyAuthoringDiagnostics(XlsxSignatureAuthoringResult target, XlsxValidationResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
    }
}
