using System;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace odf;

internal class XmlSignatureReferenceInput
{
    internal byte[] Bytes = [];
    internal XmlElement? Element;
    internal bool HasBytes;
    internal OdfSignatureStatus Status;
}

internal class XmlSignatureCertificateSelection
{
    internal byte[] Bytes = [];
    internal OdfX509Data? Data;
}

internal class XmlSignatureAlgorithm
{
    internal string KeyKind = "";
    internal HashAlgorithmName HashAlgorithm;
    internal bool IsSupported;
}

internal class XmlSignatureElementMatch
{
    internal XmlElement? Element;
    internal int Count;
}

internal static class XmlSignatureVerifier
{
    const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    internal static void VerifyContainer(
        OdfSignatureContainer container,
        ZipEntryIndex package,
        OdfSecurityLimits limits)
    {
        int index = 0;
        while (index < container.Signatures.Length)
        {
            VerifyOneSignature(
                container,
                container.Signatures[index],
                package,
                limits);
            index = index + 1;
        }
    }

    static void VerifyOneSignature(
        OdfSignatureContainer container,
        OdfSignature signature,
        ZipEntryIndex package,
        OdfSecurityLimits limits)
    {
        ResetSignatureResults(signature);
        if (!signature.IsValid || signature.SignedInfoElement == null)
        {
            SetSignatureFailure(
                container,
                signature,
                OdfSignatureStatus.Invalid,
                "The XML Signature structure is invalid and cannot be verified.");
            return;
        }
        if (!IsCanonicalizationAlgorithm(signature.CanonicalizationMethod))
        {
            SetSignatureFailure(
                container,
                signature,
                OdfSignatureStatus.UnsupportedAlgorithm,
                "The SignedInfo canonicalization algorithm is not supported.");
            return;
        }
        XmlSignatureAlgorithm signatureAlgorithm = ResolveSignatureAlgorithm(
            signature.SignatureMethod);
        if (!signatureAlgorithm.IsSupported)
        {
            SetSignatureFailure(
                container,
                signature,
                OdfSignatureStatus.UnsupportedAlgorithm,
                "The XML Signature method is not supported.");
            return;
        }

        OdfSignatureStatus referenceStatus = VerifyAllReferences(
            signature,
            container.PartPath,
            package,
            limits);
        signature.ReferencesAreValid = referenceStatus == OdfSignatureStatus.Valid;
        if (!signature.ReferencesAreValid)
        {
            SetSignatureFailure(
                container,
                signature,
                referenceStatus,
                ReferenceFailureMessage(referenceStatus));
            return;
        }

        XmlSignatureCertificateSelection selection = SelectCertificate(signature);
        if (selection.Data == null || selection.Bytes.Length == 0)
        {
            SetSignatureFailure(
                container,
                signature,
                OdfSignatureStatus.CertificateInvalid,
                "No usable embedded X.509 signing certificate is available.");
            return;
        }

        using (X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(
            selection.Bytes))
        {
            OdfSignatureStatus certificateStatus = ValidateCertificateMaterial(
                selection.Data,
                certificate);
            if (certificateStatus != OdfSignatureStatus.Valid)
            {
                SetSignatureFailure(
                    container,
                    signature,
                    certificateStatus,
                    certificateStatus == OdfSignatureStatus.UnsupportedAlgorithm
                        ? "An X.509 certificate identifier uses an unsupported digest algorithm."
                        : "The embedded X.509 certificate does not match its declared identifiers.");
                return;
            }
            signature.CertificateMaterialIsValid = true;
            signature.SigningCertificateSubject = certificate.Subject;
            signature.SigningCertificateThumbprint = certificate.Thumbprint;

            byte[] signedInfo = XmlCanonicalizer.CanonicalizeElement(
                signature.SignedInfoElement,
                signature.CanonicalizationMethod,
                signature.CanonicalizationInclusivePrefixes,
                limits);
            OdfSignatureStatus signatureStatus = VerifySignatureValue(
                signature,
                certificate,
                signatureAlgorithm,
                signedInfo);
            SecurityBuffer.ClearBytes(signedInfo);
            if (signatureStatus != OdfSignatureStatus.Valid)
            {
                SetSignatureFailure(
                    container,
                    signature,
                    signatureStatus,
                    signatureStatus == OdfSignatureStatus.CertificateInvalid
                        ? "The signing certificate does not contain the required public-key type."
                        : "The XML Signature value does not match canonicalized SignedInfo.");
                return;
            }
        }

        signature.SignatureValueMatches = true;
        signature.Status = OdfSignatureStatus.Valid;
    }

    static void ResetSignatureResults(OdfSignature signature)
    {
        signature.Status = OdfSignatureStatus.NotChecked;
        signature.ReferencesAreValid = false;
        signature.CertificateMaterialIsValid = false;
        signature.SigningCertificateSubject = "";
        signature.SigningCertificateThumbprint = "";
        signature.SignatureValueMatches = false;
        ResetReferenceResults(signature.References);
        ResetReferenceResults(signature.ManifestReferences);
    }

    static void ResetReferenceResults(OdfSignatureReference[] references)
    {
        int index = 0;
        while (index < references.Length)
        {
            references[index].ResolvedPackagePath = "";
            references[index].UsesEncryptedPackageBytes = false;
            references[index].IsVerified = false;
            references[index].DigestMatches = false;
            references[index].Status = OdfSignatureStatus.NotChecked;
            index = index + 1;
        }
    }

    static OdfSignatureStatus VerifyAllReferences(
        OdfSignature signature,
        string signaturePartPath,
        ZipEntryIndex package,
        OdfSecurityLimits limits)
    {
        OdfSignatureStatus result = OdfSignatureStatus.Valid;
        int index = 0;
        while (index < signature.References.Length)
        {
            OdfSignatureStatus status = VerifyReference(
                signature.References[index],
                signature,
                signaturePartPath,
                package,
                limits);
            if (result == OdfSignatureStatus.Valid
                && status != OdfSignatureStatus.Valid)
            {
                result = status;
            }
            index = index + 1;
        }
        index = 0;
        while (index < signature.ManifestReferences.Length)
        {
            OdfSignatureStatus status = VerifyReference(
                signature.ManifestReferences[index],
                signature,
                signaturePartPath,
                package,
                limits);
            if (result == OdfSignatureStatus.Valid
                && status != OdfSignatureStatus.Valid)
            {
                result = status;
            }
            index = index + 1;
        }
        return result;
    }

    static OdfSignatureStatus VerifyReference(
        OdfSignatureReference reference,
        OdfSignature signature,
        string signaturePartPath,
        ZipEntryIndex package,
        OdfSecurityLimits limits)
    {
        if (!reference.IsValid || !reference.DigestValueIsValid)
        {
            reference.Status = OdfSignatureStatus.Invalid;
            return reference.Status;
        }
        if (!IsDigestAlgorithmSupported(reference.DigestMethod))
        {
            reference.Status = OdfSignatureStatus.UnsupportedAlgorithm;
            return reference.Status;
        }
        XmlSignatureReferenceInput input = ResolveReferenceInput(
            reference,
            signature,
            signaturePartPath,
            package,
            limits);
        if (input.Status != OdfSignatureStatus.Valid)
        {
            reference.Status = input.Status;
            return reference.Status;
        }
        OdfSignatureStatus transformStatus = ApplyReferenceTransforms(
            input,
            reference,
            signature,
            limits);
        if (transformStatus != OdfSignatureStatus.Valid)
        {
            reference.Status = transformStatus;
            return reference.Status;
        }
        byte[] digestInput = input.Bytes;
        if (!input.HasBytes && input.Element != null)
        {
            digestInput = XmlCanonicalizer.CanonicalizeElement(
                input.Element,
                OdfAlgorithmIdentifiers.CanonicalXml10,
                "",
                limits);
        }
        byte[] actualDigest = CreateDigest(digestInput, reference.DigestMethod);
        reference.IsVerified = true;
        reference.DigestMatches = ByteArraysMatch(
            actualDigest,
            reference.DigestValue);
        SecurityBuffer.ClearBytes(actualDigest);
        reference.Status = reference.DigestMatches
            ? OdfSignatureStatus.Valid
            : OdfSignatureStatus.DigestMismatch;
        return reference.Status;
    }

    static XmlSignatureReferenceInput ResolveReferenceInput(
        OdfSignatureReference reference,
        OdfSignature signature,
        string signaturePartPath,
        ZipEntryIndex package,
        OdfSecurityLimits limits)
    {
        if (reference.IsSameDocument)
        {
            return ResolveSameDocumentInput(reference, signature);
        }
        if (reference.IsPackageReference)
        {
            return ResolvePackageInput(
                reference,
                signaturePartPath,
                package,
                limits);
        }
        XmlSignatureReferenceInput unsupported = new XmlSignatureReferenceInput();
        unsupported.Status = OdfSignatureStatus.UnsupportedAlgorithm;
        return unsupported;
    }

    static XmlSignatureReferenceInput ResolvePackageInput(
        OdfSignatureReference reference,
        string signaturePartPath,
        ZipEntryIndex package,
        OdfSecurityLimits limits)
    {
        XmlSignatureReferenceInput result = new XmlSignatureReferenceInput();
        string path = PackagePathRules.CanonicalizeEntryPath(reference.Uri).CanonicalPath;
        if (path.Length == 0)
        {
            path = PackagePathRules.ResolveRelativeReference(
                signaturePartPath,
                reference.Uri);
        }
        int suffix = path.IndexOfAny(['?', '#']);
        if (suffix >= 0)
        {
            result.Status = OdfSignatureStatus.UnsupportedAlgorithm;
            return result;
        }
        EditablePackageEntry[] entries = package.GetEditableEntries();
        int index = 0;
        while (index < entries.Length)
        {
            EditablePackageEntry entry = entries[index];
            if ((entry.Entry.CanonicalPath == path || entry.Entry.Path == path)
                && entry.IsContentAvailable)
            {
                if (entry.Data.LongLength > limits.MaximumSingleEntryBytes)
                {
                    throw new OdfSecurityException(
                        "A signature reference exceeds the configured entry-size limit.");
                }
                result.Bytes = CopyBytes(entry.Data);
                result.HasBytes = true;
                result.Status = OdfSignatureStatus.Valid;
                reference.ResolvedPackagePath = path;
                reference.UsesEncryptedPackageBytes = IsEncryptedManifestEntry(
                    package.Manifest,
                    path);
                return result;
            }
            index = index + 1;
        }
        result.Status = OdfSignatureStatus.MissingReference;
        return result;
    }

    static XmlSignatureReferenceInput ResolveSameDocumentInput(
        OdfSignatureReference reference,
        OdfSignature signature)
    {
        XmlSignatureReferenceInput result = new XmlSignatureReferenceInput();
        if (signature.SignatureElement == null
            || signature.SignatureElement.OwnerDocument == null)
        {
            result.Status = OdfSignatureStatus.MissingReference;
            return result;
        }
        XmlElement? element;
        if (reference.Uri.Length == 0)
        {
            element = signature.SignatureElement.OwnerDocument.DocumentElement;
        }
        else
        {
            if (!reference.Uri.StartsWith("#", StringComparison.Ordinal)
                || reference.Uri.StartsWith("#xpointer(", StringComparison.Ordinal))
            {
                result.Status = OdfSignatureStatus.UnsupportedAlgorithm;
                return result;
            }
            string identifier = reference.Uri.Substring(1);
            XmlSignatureElementMatch match = FindElementById(
                signature.SignatureElement.OwnerDocument.DocumentElement,
                identifier);
            if (match.Count != 1)
            {
                result.Status = match.Count == 0
                    ? OdfSignatureStatus.MissingReference
                    : OdfSignatureStatus.Invalid;
                return result;
            }
            element = match.Element;
        }
        if (element == null)
        {
            result.Status = OdfSignatureStatus.MissingReference;
            return result;
        }
        result.Element = element;
        result.Status = OdfSignatureStatus.Valid;
        return result;
    }

    static XmlSignatureElementMatch FindElementById(
        XmlElement? element,
        string identifier)
    {
        XmlSignatureElementMatch result = new XmlSignatureElementMatch();
        CollectElementsById(element, identifier, result);
        return result;
    }

    static void CollectElementsById(
        XmlElement? element,
        string identifier,
        XmlSignatureElementMatch result)
    {
        if (element == null)
        {
            return;
        }
        if (element.GetAttribute("Id") == identifier
            || element.GetAttribute("id", XmlNamespace) == identifier)
        {
            result.Count = result.Count + 1;
            if (result.Element == null)
            {
                result.Element = element;
            }
        }
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                CollectElementsById(
                    (XmlElement)child,
                    identifier,
                    result);
            }
        }
    }

    static OdfSignatureStatus ApplyReferenceTransforms(
        XmlSignatureReferenceInput input,
        OdfSignatureReference reference,
        OdfSignature signature,
        OdfSecurityLimits limits)
    {
        int index = 0;
        while (index < reference.Transforms.Length)
        {
            OdfSignatureStatus status = ApplyOneTransform(
                input,
                reference.Transforms[index],
                signature,
                limits);
            if (status != OdfSignatureStatus.Valid)
            {
                return status;
            }
            index = index + 1;
        }
        return OdfSignatureStatus.Valid;
    }

    static OdfSignatureStatus ApplyOneTransform(
        XmlSignatureReferenceInput input,
        OdfSignatureTransform transform,
        OdfSignature signature,
        OdfSecurityLimits limits)
    {
        if (IsCanonicalizationAlgorithm(transform.Algorithm))
        {
            if (input.Element != null)
            {
                input.Bytes = XmlCanonicalizer.CanonicalizeElement(
                    input.Element,
                    transform.Algorithm,
                    transform.InclusiveNamespacePrefixes,
                    limits);
            }
            else
            {
                input.Bytes = XmlCanonicalizer.CanonicalizeBytes(
                    input.Bytes,
                    transform.Algorithm,
                    transform.InclusiveNamespacePrefixes,
                    limits);
            }
            input.Element = null;
            input.HasBytes = true;
            return OdfSignatureStatus.Valid;
        }
        if (transform.Algorithm == OdfAlgorithmIdentifiers.Base64Transform)
        {
            string lexical = input.Element == null
                ? Encoding.UTF8.GetString(input.Bytes)
                : input.Element.InnerText;
            OdfLexicalValue parsed = OdfLexicalParser.ParseBase64(lexical);
            if (!parsed.IsValid)
            {
                return OdfSignatureStatus.Invalid;
            }
            input.Bytes = parsed.BinaryValue;
            input.Element = null;
            input.HasBytes = true;
            return OdfSignatureStatus.Valid;
        }
        if (transform.Algorithm == OdfAlgorithmIdentifiers.EnvelopedSignatureTransform)
        {
            return ApplyEnvelopedTransform(input, signature);
        }
        return OdfSignatureStatus.UnsupportedAlgorithm;
    }

    static OdfSignatureStatus ApplyEnvelopedTransform(
        XmlSignatureReferenceInput input,
        OdfSignature signature)
    {
        if (input.Element == null)
        {
            return OdfSignatureStatus.UnsupportedAlgorithm;
        }
        XmlElement clone = (XmlElement)input.Element.CloneNode(true);
        if (!RemoveSignatureFromClone(clone, signature.Id))
        {
            return OdfSignatureStatus.Invalid;
        }
        input.Element = clone;
        input.HasBytes = false;
        return OdfSignatureStatus.Valid;
    }

    static bool RemoveSignatureFromClone(XmlElement element, string signatureId)
    {
        int index = 0;
        while (index < element.ChildNodes.Count)
        {
            XmlNode? child = element.ChildNodes[index];
            if (child != null && child.NodeType == XmlNodeType.Element)
            {
                XmlElement childElement = (XmlElement)child;
                if (childElement.NamespaceURI == OdfNamespaces.XmlDigitalSignature
                    && childElement.LocalName == "Signature"
                    && (signatureId.Length == 0
                        || childElement.GetAttribute("Id") == signatureId))
                {
                    element.RemoveChild(childElement);
                    return true;
                }
                if (RemoveSignatureFromClone(childElement, signatureId))
                {
                    return true;
                }
            }
            index = index + 1;
        }
        return false;
    }

    static byte[] CreateDigest(byte[] data, string algorithm)
    {
        if (algorithm == OdfAlgorithmIdentifiers.Sha1)
        {
            return SHA1.HashData(data);
        }
        if (algorithm == OdfAlgorithmIdentifiers.Sha256
            || algorithm == OdfAlgorithmIdentifiers.Sha256Odf)
        {
            return SHA256.HashData(data);
        }
        if (algorithm == OdfAlgorithmIdentifiers.Sha384)
        {
            return SHA384.HashData(data);
        }
        if (algorithm == OdfAlgorithmIdentifiers.Sha512)
        {
            return SHA512.HashData(data);
        }
        throw new OdfSecurityException("The XML Signature digest algorithm is not supported.");
    }

    static bool IsDigestAlgorithmSupported(string algorithm)
    {
        return algorithm == OdfAlgorithmIdentifiers.Sha1
            || algorithm == OdfAlgorithmIdentifiers.Sha256
            || algorithm == OdfAlgorithmIdentifiers.Sha256Odf
            || algorithm == OdfAlgorithmIdentifiers.Sha384
            || algorithm == OdfAlgorithmIdentifiers.Sha512;
    }

    static bool IsCanonicalizationAlgorithm(string algorithm)
    {
        return algorithm == OdfAlgorithmIdentifiers.CanonicalXml10
            || algorithm == OdfAlgorithmIdentifiers.CanonicalXml10WithComments
            || algorithm == OdfAlgorithmIdentifiers.CanonicalXml11
            || algorithm == OdfAlgorithmIdentifiers.CanonicalXml11WithComments
            || algorithm == OdfAlgorithmIdentifiers.ExclusiveCanonicalXml10
            || algorithm == OdfAlgorithmIdentifiers.ExclusiveCanonicalXml10WithComments;
    }

    static XmlSignatureAlgorithm ResolveSignatureAlgorithm(string algorithm)
    {
        XmlSignatureAlgorithm result = new XmlSignatureAlgorithm();
        if (algorithm == OdfAlgorithmIdentifiers.RsaSha1)
        {
            result.KeyKind = "RSA";
            result.HashAlgorithm = HashAlgorithmName.SHA1;
            result.IsSupported = true;
        }
        else if (algorithm == OdfAlgorithmIdentifiers.RsaSha256)
        {
            result.KeyKind = "RSA";
            result.HashAlgorithm = HashAlgorithmName.SHA256;
            result.IsSupported = true;
        }
        else if (algorithm == OdfAlgorithmIdentifiers.RsaSha384)
        {
            result.KeyKind = "RSA";
            result.HashAlgorithm = HashAlgorithmName.SHA384;
            result.IsSupported = true;
        }
        else if (algorithm == OdfAlgorithmIdentifiers.RsaSha512)
        {
            result.KeyKind = "RSA";
            result.HashAlgorithm = HashAlgorithmName.SHA512;
            result.IsSupported = true;
        }
        else if (algorithm == OdfAlgorithmIdentifiers.EcdsaSha256)
        {
            result.KeyKind = "ECDSA";
            result.HashAlgorithm = HashAlgorithmName.SHA256;
            result.IsSupported = true;
        }
        else if (algorithm == OdfAlgorithmIdentifiers.DsaSha1)
        {
            result.KeyKind = "DSA";
            result.HashAlgorithm = HashAlgorithmName.SHA1;
            result.IsSupported = true;
        }
        return result;
    }

    static XmlSignatureCertificateSelection SelectCertificate(
        OdfSignature signature)
    {
        XmlSignatureCertificateSelection result = new XmlSignatureCertificateSelection();
        int groupIndex = 0;
        while (groupIndex < signature.X509Data.Length)
        {
            OdfX509Data group = signature.X509Data[groupIndex];
            int certificateIndex = 0;
            while (certificateIndex < group.Certificates.Length)
            {
                bool lexicalValid = certificateIndex < group.CertificateIsValid.Length
                    && group.CertificateIsValid[certificateIndex];
                byte[] bytes = group.Certificates[certificateIndex];
                if (lexicalValid
                    && bytes.Length > 0
                    && LooksLikeDerCertificate(bytes)
                    && X509Certificate2.GetCertContentType(bytes) == X509ContentType.Cert)
                {
                    result.Bytes = bytes;
                    result.Data = group;
                    return result;
                }
                certificateIndex = certificateIndex + 1;
            }
            groupIndex = groupIndex + 1;
        }
        return result;
    }

    static bool LooksLikeDerCertificate(byte[] value)
    {
        if (value.Length < 8 || value[0] != 48)
        {
            return false;
        }
        int offset = 2;
        int length = value[1];
        if ((length & 128) != 0)
        {
            int width = length & 127;
            if (width <= 0 || width > 4 || value.Length < 2 + width)
            {
                return false;
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
        return length > 0
            && offset + length == value.Length
            && value[offset] == 48;
    }

    static OdfSignatureStatus ValidateCertificateMaterial(
        OdfX509Data data,
        X509Certificate2 certificate)
    {
        if (!CertificateNamesMatch(data, certificate)
            || !CertificateIssuerSerialMatches(data, certificate)
            || !CertificateSubjectKeyIdentifierMatches(data, certificate))
        {
            return OdfSignatureStatus.CertificateInvalid;
        }
        int index = 0;
        bool hasDigestMatch = data.Digests.Length == 0;
        while (index < data.Digests.Length)
        {
            string algorithm = index < data.DigestAlgorithms.Length
                ? data.DigestAlgorithms[index]
                : "";
            if (!IsDigestAlgorithmSupported(algorithm))
            {
                return OdfSignatureStatus.UnsupportedAlgorithm;
            }
            byte[] actual = CreateDigest(certificate.RawData, algorithm);
            if (ByteArraysMatch(actual, data.Digests[index]))
            {
                hasDigestMatch = true;
            }
            SecurityBuffer.ClearBytes(actual);
            index = index + 1;
        }
        return hasDigestMatch
            ? OdfSignatureStatus.Valid
            : OdfSignatureStatus.CertificateInvalid;
    }

    static bool CertificateNamesMatch(
        OdfX509Data data,
        X509Certificate2 certificate)
    {
        if (data.SubjectNames.Length == 0)
        {
            return true;
        }
        int index = 0;
        while (index < data.SubjectNames.Length)
        {
            if (DistinguishedNamesMatch(
                data.SubjectNames[index],
                certificate.Subject))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool CertificateIssuerSerialMatches(
        OdfX509Data data,
        X509Certificate2 certificate)
    {
        if (data.IssuerNames.Length == 0)
        {
            return true;
        }
        string serial = GetCertificateSerial(certificate);
        int index = 0;
        while (index < data.IssuerNames.Length
            && index < data.SerialNumbers.Length)
        {
            if (DistinguishedNamesMatch(
                    data.IssuerNames[index],
                    certificate.Issuer)
                && data.SerialNumbers[index] == serial)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool DistinguishedNamesMatch(
        string declaredName,
        string certificateName)
    {
        if (declaredName == certificateName)
        {
            return true;
        }
        string normalizedDeclaredName = NormalizeDistinguishedName(
            declaredName);
        string normalizedCertificateName = NormalizeDistinguishedName(
            certificateName);
        return normalizedDeclaredName == normalizedCertificateName;
    }

    static string NormalizeDistinguishedName(string value)
    {
        StringBuilder result = new StringBuilder(value.Length);
        int index = 0;
        bool componentStart = true;
        bool escaped = false;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == ',' && !escaped)
            {
                result.Append(current);
                componentStart = true;
                escaped = false;
                index = index + 1;
                while (index < value.Length && value[index] == ' ')
                {
                    index = index + 1;
                }
            }
            else if (componentStart
                && current == 'S'
                && index + 1 < value.Length
                && value[index + 1] == '=')
            {
                result.Append("ST=");
                componentStart = false;
                index = index + 2;
            }
            else
            {
                result.Append(current);
                if (current != ' ')
                {
                    componentStart = false;
                }
                if (current == '\\')
                {
                    escaped = !escaped;
                }
                else
                {
                    escaped = false;
                }
                index = index + 1;
            }
        }
        return result.ToString();
    }

    static bool CertificateSubjectKeyIdentifierMatches(
        OdfX509Data data,
        X509Certificate2 certificate)
    {
        if (data.SubjectKeyIdentifiers.Length == 0)
        {
            return true;
        }
        byte[] certificateIdentifier = GetCertificateSubjectKeyIdentifier(certificate);
        int index = 0;
        while (index < data.SubjectKeyIdentifiers.Length)
        {
            if (ByteArraysMatch(
                certificateIdentifier,
                data.SubjectKeyIdentifiers[index]))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
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

    static OdfSignatureStatus VerifySignatureValue(
        OdfSignature signature,
        X509Certificate2 certificate,
        XmlSignatureAlgorithm algorithm,
        byte[] signedInfo)
    {
        if (!signature.SignatureValueIsValid)
        {
            return OdfSignatureStatus.Invalid;
        }
        if (algorithm.KeyKind == "RSA")
        {
            using (RSA? rsa = certificate.GetRSAPublicKey())
            {
                if (rsa == null)
                {
                    return OdfSignatureStatus.CertificateInvalid;
                }
                return rsa.VerifyData(
                    signedInfo,
                    signature.SignatureValue,
                    algorithm.HashAlgorithm,
                    RSASignaturePadding.Pkcs1)
                    ? OdfSignatureStatus.Valid
                    : OdfSignatureStatus.Invalid;
            }
        }
        if (algorithm.KeyKind == "ECDSA")
        {
            using (ECDsa? ecdsa = certificate.GetECDsaPublicKey())
            {
                if (ecdsa == null)
                {
                    return OdfSignatureStatus.CertificateInvalid;
                }
                return ecdsa.VerifyData(
                    signedInfo,
                    signature.SignatureValue,
                    algorithm.HashAlgorithm,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
                    ? OdfSignatureStatus.Valid
                    : OdfSignatureStatus.Invalid;
            }
        }
        if (algorithm.KeyKind == "DSA")
        {
            using (DSA? dsa = certificate.GetDSAPublicKey())
            {
                if (dsa == null)
                {
                    return OdfSignatureStatus.CertificateInvalid;
                }
                return dsa.VerifyData(
                    signedInfo,
                    signature.SignatureValue,
                    algorithm.HashAlgorithm,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
                    ? OdfSignatureStatus.Valid
                    : OdfSignatureStatus.Invalid;
            }
        }
        return OdfSignatureStatus.UnsupportedAlgorithm;
    }

    static bool IsEncryptedManifestEntry(OdfManifest? manifest, string path)
    {
        if (manifest == null)
        {
            return false;
        }
        int index = 0;
        while (index < manifest.Entries.Length)
        {
            OdfManifestEntry entry = manifest.Entries[index];
            if (entry.FullPath == path && entry.Encryption != null)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ByteArraysMatch(byte[] first, byte[] second)
    {
        return first.Length == second.Length
            && CryptographicOperations.FixedTimeEquals(first, second);
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    static string ReferenceFailureMessage(OdfSignatureStatus status)
    {
        if (status == OdfSignatureStatus.UnsupportedAlgorithm)
        {
            return "A signature reference uses an unsupported digest, transform, or resource form.";
        }
        if (status == OdfSignatureStatus.MissingReference)
        {
            return "A signature reference does not resolve to available package or signature content.";
        }
        if (status == OdfSignatureStatus.DigestMismatch)
        {
            return "A signature reference digest does not match its resolved content.";
        }
        return "A signature reference is invalid.";
    }

    static void SetSignatureFailure(
        OdfSignatureContainer container,
        OdfSignature signature,
        OdfSignatureStatus status,
        string message)
    {
        signature.Status = status;
        AddVerificationDiagnostic(container, signature, status, message);
    }

    static void AddVerificationDiagnostic(
        OdfSignatureContainer container,
        OdfSignature signature,
        OdfSignatureStatus status,
        string message)
    {
        string code = status == OdfSignatureStatus.UnsupportedAlgorithm
            ? OdfDiagnosticCodes.SignatureAlgorithmUnsupported
            : OdfDiagnosticCodes.SignatureInvalid;
        OdfLocation location = new OdfLocation();
        location.PartPath = container.PartPath;
        OdfDiagnostic diagnostic = OdfDiagnosticFactory.Create(
            code,
            message,
            OdfValidationSeverity.Error,
            location,
            null,
            OdfVersion.Unknown);
        OdfDiagnostic[] signatureDiagnostics = new OdfDiagnostic[
            signature.Diagnostics.Length + 1];
        Array.Copy(
            signature.Diagnostics,
            signatureDiagnostics,
            signature.Diagnostics.Length);
        signatureDiagnostics[signatureDiagnostics.Length - 1] = diagnostic;
        signature.Diagnostics = signatureDiagnostics;
        OdfDiagnostic[] containerDiagnostics = new OdfDiagnostic[
            container.Diagnostics.Length + 1];
        Array.Copy(
            container.Diagnostics,
            containerDiagnostics,
            container.Diagnostics.Length);
        containerDiagnostics[containerDiagnostics.Length - 1] = diagnostic;
        container.Diagnostics = containerDiagnostics;
    }
}
