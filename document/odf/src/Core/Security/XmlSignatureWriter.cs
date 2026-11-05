using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace odf;

internal class XmlSignatureSigningAlgorithm
{
    internal string KeyKind = "";
    internal HashAlgorithmName HashAlgorithm;
    internal bool IsSupported;
}

internal static class XmlSignatureWriter
{
    const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    internal static OdfSignatureContainer CreateContainer(
        ZipEntryIndex package,
        string partPath,
        string version,
        string[] entryPaths,
        AsymmetricAlgorithm privateKey,
        X509Certificate2 certificate,
        string signatureMethod,
        OdfSecurityLimits limits)
    {
        ValidateCreationInputs(
            package,
            partPath,
            version,
            entryPaths,
            privateKey,
            certificate,
            limits);
        XmlSignatureSigningAlgorithm algorithm = ResolveSigningAlgorithm(
            signatureMethod);
        if (!algorithm.IsSupported)
        {
            throw new OdfSecurityException(
                "The requested XML Signature method is not supported for signing.");
        }
        EditablePackageEntry[] entries = ResolveSignedEntries(
            package,
            partPath,
            entryPaths,
            limits);

        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        XmlElement root = CreateContainerRoot(document, version);
        document.AppendChild(root);
        XmlElement signature = CreateSignatureElement(document);
        root.AppendChild(signature);
        XmlElement signedInfo = CreateSignedInfo(
            document,
            entries,
            signatureMethod);
        signature.AppendChild(signedInfo);

        byte[] canonicalSignedInfo = XmlCanonicalizer.CanonicalizeElement(
            signedInfo,
            OdfAlgorithmIdentifiers.CanonicalXml10,
            "",
            limits);
        byte[] signatureValue = SignCanonicalData(
            canonicalSignedInfo,
            privateKey,
            algorithm);
        SecurityBuffer.ClearBytes(canonicalSignedInfo);
        XmlElement signatureValueElement = CreateDsElement(
            document,
            "SignatureValue");
        signatureValueElement.InnerText = Convert.ToBase64String(signatureValue);
        SecurityBuffer.ClearBytes(signatureValue);
        signature.AppendChild(signatureValueElement);
        signature.AppendChild(CreateKeyInfo(document, certificate));

        byte[] output = WriteDocument(document);
        if (output.LongLength > limits.MaximumSingleEntryBytes)
        {
            SecurityBuffer.ClearBytes(output);
            throw new OdfSecurityException(
                "The generated XML signature container exceeds the configured size limit.");
        }
        OdfSignatureContainer result = XmlSignatureReader.ParseBytes(
            output,
            partPath,
            limits);
        SecurityBuffer.ClearBytes(output);
        XmlSignatureVerifier.VerifyContainer(result, package, limits);
        if (result.Signatures.Length != 1
            || result.Signatures[0].Status != OdfSignatureStatus.Valid)
        {
            throw new OdfSecurityException(
                "The caller-provided signing key does not match the certificate or generated signature.");
        }
        return result;
    }

    static void ValidateCreationInputs(
        ZipEntryIndex package,
        string partPath,
        string version,
        string[] entryPaths,
        AsymmetricAlgorithm privateKey,
        X509Certificate2 certificate,
        OdfSecurityLimits limits)
    {
        if (package == null
            || entryPaths == null
            || privateKey == null
            || certificate == null)
        {
            throw new OdfSecurityException(
                "A package, entry selection, private key, and certificate are required.");
        }
        if (partPath != OdfPartNames.DocumentSignatures
            && partPath != OdfPartNames.MacroSignatures
            && partPath != OdfPartNames.PackageSignatures)
        {
            throw new OdfSecurityException(
                "The XML signature output path is not a standard ODF signature-container path.");
        }
        if (version != "1.2" && version != "1.3" && version != "1.4")
        {
            throw new OdfSecurityException(
                "XML signature creation supports ODF versions 1.2 through 1.4.");
        }
        if (entryPaths.Length == 0)
        {
            throw new OdfSecurityException(
                "At least one package entry must be selected for signing.");
        }
        if (limits == null
            || limits.MaximumSingleEntryBytes <= 0
            || limits.MaximumXmlDepth <= 0
            || limits.MaximumXmlAttributesPerElement < 0
            || limits.MaximumXmlTextLength < 0)
        {
            throw new OdfSecurityException(
                "XML signature creation limits must be non-negative and bounded.");
        }
    }

    static EditablePackageEntry[] ResolveSignedEntries(
        ZipEntryIndex package,
        string partPath,
        string[] entryPaths,
        OdfSecurityLimits limits)
    {
        EditablePackageEntry[] packageEntries = package.GetEditableEntries();
        EditablePackageEntry[] result = new EditablePackageEntry[entryPaths.Length];
        HashSet<string> selectedPaths = new HashSet<string>(StringComparer.Ordinal);
        int pathIndex = 0;
        while (pathIndex < entryPaths.Length)
        {
            PackagePathValue path = PackagePathRules.CanonicalizeEntryPath(
                entryPaths[pathIndex]);
            if (!path.IsValid || path.IsDirectory)
            {
                throw new OdfSecurityException(
                    "A selected signature entry path is invalid.");
            }
            if (path.CanonicalPath == partPath)
            {
                throw new OdfSecurityException(
                    "A signature container cannot include a circular reference to itself.");
            }
            if (!selectedPaths.Add(path.CanonicalPath))
            {
                throw new OdfSecurityException(
                    "A package entry cannot be selected for signing more than once.");
            }
            EditablePackageEntry? found = FindPackageEntry(
                packageEntries,
                path.CanonicalPath);
            if (found == null || !found.IsContentAvailable)
            {
                throw new OdfSecurityException(
                    "A selected package entry is missing or unavailable.");
            }
            if (found.Data.LongLength > limits.MaximumSingleEntryBytes)
            {
                throw new OdfSecurityException(
                    "A selected package entry exceeds the configured size limit.");
            }
            result[pathIndex] = found;
            pathIndex = pathIndex + 1;
        }
        return result;
    }

    static EditablePackageEntry? FindPackageEntry(
        EditablePackageEntry[] entries,
        string path)
    {
        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index].Entry.CanonicalPath == path
                || entries[index].Entry.Path == path)
            {
                return entries[index];
            }
            index = index + 1;
        }
        return null;
    }

    static XmlElement CreateContainerRoot(
        XmlDocument document,
        string version)
    {
        XmlElement root = document.CreateElement(
            "dsig",
            "document-signatures",
            OdfNamespaces.DigitalSignature);
        AddNamespaceDeclaration(
            document,
            root,
            "dsig",
            OdfNamespaces.DigitalSignature);
        AddNamespaceDeclaration(
            document,
            root,
            "ds",
            OdfNamespaces.XmlDigitalSignature);
        AddNamespaceDeclaration(
            document,
            root,
            "ds11",
            "http://www.w3.org/2009/xmldsig11#");
        XmlAttribute versionAttribute = document.CreateAttribute(
            "dsig",
            "version",
            OdfNamespaces.DigitalSignature);
        versionAttribute.Value = version;
        root.Attributes.Append(versionAttribute);
        return root;
    }

    static void AddNamespaceDeclaration(
        XmlDocument document,
        XmlElement element,
        string prefix,
        string namespaceName)
    {
        XmlAttribute declaration = document.CreateAttribute(
            "xmlns",
            prefix,
            XmlnsNamespace);
        declaration.Value = namespaceName;
        element.Attributes.Append(declaration);
    }

    static XmlElement CreateSignatureElement(XmlDocument document)
    {
        XmlElement result = CreateDsElement(document, "Signature");
        result.SetAttribute("Id", "signature-1");
        return result;
    }

    static XmlElement CreateSignedInfo(
        XmlDocument document,
        EditablePackageEntry[] entries,
        string signatureMethod)
    {
        XmlElement result = CreateDsElement(document, "SignedInfo");
        XmlElement canonicalization = CreateDsElement(
            document,
            "CanonicalizationMethod");
        canonicalization.SetAttribute(
            "Algorithm",
            OdfAlgorithmIdentifiers.CanonicalXml10);
        result.AppendChild(canonicalization);
        XmlElement signatureMethodElement = CreateDsElement(
            document,
            "SignatureMethod");
        signatureMethodElement.SetAttribute("Algorithm", signatureMethod);
        result.AppendChild(signatureMethodElement);
        int index = 0;
        while (index < entries.Length)
        {
            result.AppendChild(CreateReference(document, entries[index]));
            index = index + 1;
        }
        return result;
    }

    static XmlElement CreateReference(
        XmlDocument document,
        EditablePackageEntry entry)
    {
        XmlElement result = CreateDsElement(document, "Reference");
        string path = entry.Entry.CanonicalPath.Length == 0
            ? entry.Entry.Path
            : entry.Entry.CanonicalPath;
        result.SetAttribute("URI", path);
        XmlElement digestMethod = CreateDsElement(document, "DigestMethod");
        digestMethod.SetAttribute(
            "Algorithm",
            OdfAlgorithmIdentifiers.Sha256);
        result.AppendChild(digestMethod);
        byte[] digest = SHA256.HashData(entry.Data);
        XmlElement digestValue = CreateDsElement(document, "DigestValue");
        digestValue.InnerText = Convert.ToBase64String(digest);
        SecurityBuffer.ClearBytes(digest);
        result.AppendChild(digestValue);
        return result;
    }

    static XmlElement CreateKeyInfo(
        XmlDocument document,
        X509Certificate2 certificate)
    {
        XmlElement keyInfo = CreateDsElement(document, "KeyInfo");
        XmlElement x509Data = CreateDsElement(document, "X509Data");
        XmlElement issuerSerial = CreateDsElement(
            document,
            "X509IssuerSerial");
        XmlElement issuerName = CreateDsElement(document, "X509IssuerName");
        issuerName.InnerText = certificate.Issuer;
        issuerSerial.AppendChild(issuerName);
        XmlElement serialNumber = CreateDsElement(
            document,
            "X509SerialNumber");
        serialNumber.InnerText = GetCertificateSerial(certificate);
        issuerSerial.AppendChild(serialNumber);
        x509Data.AppendChild(issuerSerial);
        XmlElement subjectName = CreateDsElement(document, "X509SubjectName");
        subjectName.InnerText = certificate.Subject;
        x509Data.AppendChild(subjectName);
        byte[] subjectKeyIdentifier = GetCertificateSubjectKeyIdentifier(
            certificate);
        if (subjectKeyIdentifier.Length > 0)
        {
            XmlElement ski = CreateDsElement(document, "X509SKI");
            ski.InnerText = Convert.ToBase64String(subjectKeyIdentifier);
            x509Data.AppendChild(ski);
            SecurityBuffer.ClearBytes(subjectKeyIdentifier);
        }
        byte[] certificateBytes = certificate.RawData;
        byte[] certificateDigest = SHA256.HashData(certificateBytes);
        XmlElement digest = document.CreateElement(
            "ds11",
            "X509Digest",
            "http://www.w3.org/2009/xmldsig11#");
        digest.SetAttribute(
            "Algorithm",
            OdfAlgorithmIdentifiers.Sha256);
        digest.InnerText = Convert.ToBase64String(certificateDigest);
        SecurityBuffer.ClearBytes(certificateDigest);
        x509Data.AppendChild(digest);
        XmlElement certificateElement = CreateDsElement(
            document,
            "X509Certificate");
        certificateElement.InnerText = Convert.ToBase64String(certificateBytes);
        x509Data.AppendChild(certificateElement);
        keyInfo.AppendChild(x509Data);
        return keyInfo;
    }

    static XmlElement CreateDsElement(
        XmlDocument document,
        string localName)
    {
        return document.CreateElement(
            "ds",
            localName,
            OdfNamespaces.XmlDigitalSignature);
    }

    static byte[] SignCanonicalData(
        byte[] data,
        AsymmetricAlgorithm privateKey,
        XmlSignatureSigningAlgorithm algorithm)
    {
        if (algorithm.KeyKind == "RSA")
        {
            RSA? rsa = privateKey as RSA;
            if (rsa == null)
            {
                throw new OdfSecurityException(
                    "The requested signature method requires an RSA private key.");
            }
            return rsa.SignData(
                data,
                algorithm.HashAlgorithm,
                RSASignaturePadding.Pkcs1);
        }
        if (algorithm.KeyKind == "ECDSA")
        {
            ECDsa? ecdsa = privateKey as ECDsa;
            if (ecdsa == null)
            {
                throw new OdfSecurityException(
                    "The requested signature method requires an ECDSA private key.");
            }
            return ecdsa.SignData(
                data,
                algorithm.HashAlgorithm,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        if (algorithm.KeyKind == "DSA")
        {
            DSA? dsa = privateKey as DSA;
            if (dsa == null)
            {
                throw new OdfSecurityException(
                    "The requested signature method requires a DSA private key.");
            }
            return dsa.SignData(
                data,
                algorithm.HashAlgorithm,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        throw new OdfSecurityException(
            "The requested XML Signature method is not supported for signing.");
    }

    static XmlSignatureSigningAlgorithm ResolveSigningAlgorithm(
        string signatureMethod)
    {
        XmlSignatureSigningAlgorithm result = new XmlSignatureSigningAlgorithm();
        if (signatureMethod == OdfAlgorithmIdentifiers.RsaSha1)
        {
            result.KeyKind = "RSA";
            result.HashAlgorithm = HashAlgorithmName.SHA1;
            result.IsSupported = true;
        }
        else if (signatureMethod == OdfAlgorithmIdentifiers.RsaSha256)
        {
            result.KeyKind = "RSA";
            result.HashAlgorithm = HashAlgorithmName.SHA256;
            result.IsSupported = true;
        }
        else if (signatureMethod == OdfAlgorithmIdentifiers.RsaSha384)
        {
            result.KeyKind = "RSA";
            result.HashAlgorithm = HashAlgorithmName.SHA384;
            result.IsSupported = true;
        }
        else if (signatureMethod == OdfAlgorithmIdentifiers.RsaSha512)
        {
            result.KeyKind = "RSA";
            result.HashAlgorithm = HashAlgorithmName.SHA512;
            result.IsSupported = true;
        }
        else if (signatureMethod == OdfAlgorithmIdentifiers.EcdsaSha256)
        {
            result.KeyKind = "ECDSA";
            result.HashAlgorithm = HashAlgorithmName.SHA256;
            result.IsSupported = true;
        }
        else if (signatureMethod == OdfAlgorithmIdentifiers.DsaSha1)
        {
            result.KeyKind = "DSA";
            result.HashAlgorithm = HashAlgorithmName.SHA1;
            result.IsSupported = true;
        }
        return result;
    }

    static string GetCertificateSerial(X509Certificate2 certificate)
    {
        byte[] serial = certificate.GetSerialNumber();
        BigInteger value = new BigInteger(serial, true, false);
        return value.ToString(CultureInfo.InvariantCulture);
    }

    static byte[] GetCertificateSubjectKeyIdentifier(
        X509Certificate2 certificate)
    {
        foreach (X509Extension extension in certificate.Extensions)
        {
            if (extension.Oid != null
                && extension.Oid.Value == "2.5.29.14")
            {
                return DecodeDerOctetString(extension.RawData);
            }
        }
        return [];
    }

    static byte[] DecodeDerOctetString(byte[] value)
    {
        if (value.Length < 2 || value[0] != 4)
        {
            return [];
        }
        int offset = 2;
        int length = value[1];
        if ((length & 128) != 0)
        {
            int width = length & 127;
            if (width <= 0 || width > 4 || value.Length < 2 + width)
            {
                return [];
            }
            length = 0;
            int index = 0;
            while (index < width)
            {
                length = length * 256 + value[2 + index];
                index = index + 1;
            }
            offset = 2 + width;
        }
        if (length < 0 || offset + length != value.Length)
        {
            return [];
        }
        byte[] result = new byte[length];
        Array.Copy(value, offset, result, 0, length);
        return result;
    }

    static byte[] WriteDocument(XmlDocument document)
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Encoding = new UTF8Encoding(false);
        settings.Indent = false;
        settings.OmitXmlDeclaration = false;
        settings.CloseOutput = false;
        using (MemoryStream output = new MemoryStream())
        {
            using (XmlWriter writer = XmlWriter.Create(output, settings))
            {
                document.Save(writer);
            }
            return output.ToArray();
        }
    }
}
