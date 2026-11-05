using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace odf;

internal class XmlSignatureParseContext
{
    internal string PartPath = "";
    internal List<OdfDiagnostic> Diagnostics = new List<OdfDiagnostic>();
}

internal static class XmlSignatureReader
{
    const string ExclusiveCanonicalizationNamespace = "http://www.w3.org/2001/10/xml-exc-c14n#";
    const string XmlDigitalSignature11Namespace = "http://www.w3.org/2009/xmldsig11#";

    internal static OdfSignatureContainer ParseBytes(
        byte[] data,
        string partPath,
        OdfSecurityLimits limits)
    {
        ValidateLimits(limits);
        if (data.LongLength > limits.MaximumSingleEntryBytes)
        {
            throw new OdfSecurityException("The XML signature container exceeds the configured size limit.");
        }

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.IgnoreComments = false;
        settings.IgnoreWhitespace = false;
        settings.MaxCharactersFromEntities = 0;
        settings.MaxCharactersInDocument = limits.MaximumSingleEntryBytes;
        XmlDocument document = new XmlDocument();
        document.XmlResolver = null;
        document.PreserveWhitespace = true;
        using (MemoryStream stream = new MemoryStream(data, false))
        using (XmlReader reader = XmlReader.Create(stream, settings))
        {
            document.Load(reader);
        }
        ValidateTree(document, 0, limits);

        OdfSignatureContainer result = new OdfSignatureContainer();
        result.PartPath = partPath;
        result.OriginalBytes = CopyBytes(data);
        XmlSignatureParseContext context = new XmlSignatureParseContext();
        context.PartPath = partPath;
        bool valid = true;
        if (!IsSignaturePartPath(partPath))
        {
            AddContainerDiagnostic(
                context,
                "The XML signature container is not stored at a standard ODF signature path.");
            valid = false;
        }

        XmlElement? root = document.DocumentElement;
        if (root == null
            || root.NamespaceURI != OdfNamespaces.DigitalSignature
            || root.LocalName != "document-signatures")
        {
            AddContainerDiagnostic(
                context,
                "The XML signature container has no dsig:document-signatures root.");
            result.Diagnostics = context.Diagnostics.ToArray();
            return result;
        }
        result.OriginalXml = root.OuterXml;
        result.Version = root.GetAttribute(
            "version",
            OdfNamespaces.DigitalSignature);
        if (result.Version != "1.2"
            && result.Version != "1.3"
            && result.Version != "1.4")
        {
            AddContainerDiagnostic(
                context,
                "The ODF signature container version is missing or unsupported.");
            valid = false;
        }

        List<OdfSignature> signatures = new List<OdfSignature>();
        foreach (XmlNode child in root.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement element = (XmlElement)child;
            if (IsElement(element, OdfNamespaces.XmlDigitalSignature, "Signature"))
            {
                OdfSignature signature = ParseSignature(element, context);
                signatures.Add(signature);
                if (!signature.IsValid)
                {
                    valid = false;
                }
            }
            else
            {
                AddContainerDiagnostic(
                    context,
                    "The ODF signature container has an unexpected child element.");
                valid = false;
            }
        }
        if (signatures.Count == 0)
        {
            AddContainerDiagnostic(
                context,
                "The ODF signature container has no XML Signature elements.");
            valid = false;
        }
        result.Signatures = signatures.ToArray();
        result.Diagnostics = context.Diagnostics.ToArray();
        result.IsValid = valid;
        return result;
    }

    static OdfSignature ParseSignature(
        XmlElement element,
        XmlSignatureParseContext context)
    {
        OdfSignature result = new OdfSignature();
        result.Status = OdfSignatureStatus.NotChecked;
        result.Id = element.GetAttribute("Id");
        result.OriginalXml = element.OuterXml;
        result.SignatureElement = element;
        bool valid = true;

        XmlElement? signedInfo = FindDirectChild(
            element,
            OdfNamespaces.XmlDigitalSignature,
            "SignedInfo");
        if (signedInfo == null
            || CountDirectChildren(
                element,
                OdfNamespaces.XmlDigitalSignature,
                "SignedInfo") != 1)
        {
            AddSignatureDiagnostic(
                context,
                result,
                "An XML Signature must contain exactly one SignedInfo element.");
            valid = false;
        }
        else
        {
            result.SignedInfoElement = signedInfo;
            result.SignedInfoXml = signedInfo.OuterXml;
            if (!ParseSignedInfo(signedInfo, result, context))
            {
                valid = false;
            }
        }

        XmlElement? signatureValue = FindDirectChild(
            element,
            OdfNamespaces.XmlDigitalSignature,
            "SignatureValue");
        if (signatureValue == null
            || CountDirectChildren(
                element,
                OdfNamespaces.XmlDigitalSignature,
                "SignatureValue") != 1)
        {
            AddSignatureDiagnostic(
                context,
                result,
                "An XML Signature must contain exactly one SignatureValue element.");
            valid = false;
        }
        else
        {
            result.SignatureValueId = signatureValue.GetAttribute("Id");
            result.SignatureValueLexical = signatureValue.InnerText;
            OdfLexicalValue parsed = OdfLexicalParser.ParseBase64(
                result.SignatureValueLexical);
            result.SignatureValue = parsed.BinaryValue;
            result.SignatureValueIsValid = parsed.IsValid
                && parsed.BinaryValue.Length > 0;
            if (!result.SignatureValueIsValid)
            {
                AddSignatureDiagnostic(
                    context,
                    result,
                    "The XML Signature value is not valid non-empty base64.");
                valid = false;
            }
        }

        XmlElement? keyInfo = FindDirectChild(
            element,
            OdfNamespaces.XmlDigitalSignature,
            "KeyInfo");
        if (CountDirectChildren(
            element,
            OdfNamespaces.XmlDigitalSignature,
            "KeyInfo") > 1)
        {
            AddSignatureDiagnostic(
                context,
                result,
                "An XML Signature contains more than one KeyInfo element.");
            valid = false;
        }
        if (keyInfo != null)
        {
            result.KeyInfoXml = keyInfo.OuterXml;
            if (!ParseKeyInfo(keyInfo, result, context))
            {
                valid = false;
            }
        }

        List<string> objects = new List<string>();
        List<OdfSignatureReference> manifestReferences = new List<OdfSignatureReference>();
        List<OdfXadesProperties> xadesProperties = new List<OdfXadesProperties>();
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                XmlElement objectElement = (XmlElement)child;
                if (IsElement(
                    objectElement,
                    OdfNamespaces.XmlDigitalSignature,
                    "Object"))
                {
                    objects.Add(objectElement.OuterXml);
                    if (!CollectManifestReferences(
                        objectElement,
                        manifestReferences,
                        result,
                        context))
                    {
                        valid = false;
                    }
                    CollectXadesProperties(
                        objectElement,
                        xadesProperties,
                        result,
                        context);
                }
            }
        }
        result.ObjectXml = objects.ToArray();
        result.ManifestReferences = manifestReferences.ToArray();
        result.XadesProperties = xadesProperties.ToArray();
        foreach (OdfSignatureReference reference in result.ManifestReferences)
        {
            if (!reference.IsValid)
            {
                valid = false;
            }
        }
        foreach (OdfXadesProperties properties in result.XadesProperties)
        {
            if (!properties.IsValid)
            {
                valid = false;
            }
        }
        result.IsValid = valid;
        return result;
    }

    static bool ParseSignedInfo(
        XmlElement signedInfo,
        OdfSignature signature,
        XmlSignatureParseContext context)
    {
        bool valid = true;
        XmlElement? canonicalization = FindDirectChild(
            signedInfo,
            OdfNamespaces.XmlDigitalSignature,
            "CanonicalizationMethod");
        if (canonicalization == null
            || CountDirectChildren(
                signedInfo,
                OdfNamespaces.XmlDigitalSignature,
                "CanonicalizationMethod") != 1)
        {
            AddSignatureDiagnostic(
                context,
                signature,
                "SignedInfo must contain exactly one CanonicalizationMethod element.");
            valid = false;
        }
        else
        {
            signature.CanonicalizationMethod = canonicalization.GetAttribute("Algorithm");
            signature.CanonicalizationInclusivePrefixes = GetInclusivePrefixList(
                canonicalization);
            if (signature.CanonicalizationMethod.Length == 0)
            {
                AddSignatureDiagnostic(
                    context,
                    signature,
                    "CanonicalizationMethod has no Algorithm value.");
                valid = false;
            }
        }

        XmlElement? signatureMethod = FindDirectChild(
            signedInfo,
            OdfNamespaces.XmlDigitalSignature,
            "SignatureMethod");
        if (signatureMethod == null
            || CountDirectChildren(
                signedInfo,
                OdfNamespaces.XmlDigitalSignature,
                "SignatureMethod") != 1)
        {
            AddSignatureDiagnostic(
                context,
                signature,
                "SignedInfo must contain exactly one SignatureMethod element.");
            valid = false;
        }
        else
        {
            signature.SignatureMethod = signatureMethod.GetAttribute("Algorithm");
            if (signature.SignatureMethod.Length == 0)
            {
                AddSignatureDiagnostic(
                    context,
                    signature,
                    "SignatureMethod has no Algorithm value.");
                valid = false;
            }
        }

        List<OdfSignatureReference> references = new List<OdfSignatureReference>();
        foreach (XmlNode child in signedInfo.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                XmlElement referenceElement = (XmlElement)child;
                if (IsElement(
                    referenceElement,
                    OdfNamespaces.XmlDigitalSignature,
                    "Reference"))
                {
                    references.Add(ParseReference(
                        referenceElement,
                        signature,
                        context));
                }
            }
        }
        if (references.Count == 0)
        {
            AddSignatureDiagnostic(
                context,
                signature,
                "SignedInfo must contain at least one Reference element.");
            valid = false;
        }
        foreach (OdfSignatureReference reference in references)
        {
            if (!reference.IsValid)
            {
                valid = false;
            }
        }
        signature.References = references.ToArray();
        return valid;
    }

    static OdfSignatureReference ParseReference(
        XmlElement element,
        OdfSignature signature,
        XmlSignatureParseContext context)
    {
        OdfSignatureReference result = new OdfSignatureReference();
        result.Id = element.GetAttribute("Id");
        result.HasUri = element.HasAttribute("URI");
        result.Uri = element.GetAttribute("URI");
        result.Type = element.GetAttribute("Type");
        result.IsSameDocument = !result.HasUri
            || result.Uri.Length == 0
            || result.Uri.StartsWith("#", StringComparison.Ordinal);
        result.IsPackageReference = !result.IsSameDocument
            && OdfIriResolver.IsPackageRelativeReference(result.Uri);
        result.OriginalXml = element.OuterXml;
        result.ReferenceElement = element;
        bool valid = true;

        List<OdfSignatureTransform> transforms = new List<OdfSignatureTransform>();
        XmlElement? transformsElement = FindDirectChild(
            element,
            OdfNamespaces.XmlDigitalSignature,
            "Transforms");
        if (CountDirectChildren(
            element,
            OdfNamespaces.XmlDigitalSignature,
            "Transforms") > 1)
        {
            AddSignatureDiagnostic(
                context,
                signature,
                "A Reference contains more than one Transforms element.");
            valid = false;
        }
        if (transformsElement != null)
        {
            foreach (XmlNode child in transformsElement.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    XmlElement transformElement = (XmlElement)child;
                    if (IsElement(
                        transformElement,
                        OdfNamespaces.XmlDigitalSignature,
                        "Transform"))
                    {
                        OdfSignatureTransform transform = ParseTransform(
                            transformElement);
                        transforms.Add(transform);
                        if (!transform.IsValid)
                        {
                            AddSignatureDiagnostic(
                                context,
                                signature,
                                "A Reference Transform has no Algorithm value.");
                            valid = false;
                        }
                    }
                }
            }
            if (transforms.Count == 0)
            {
                AddSignatureDiagnostic(
                    context,
                    signature,
                    "A Transforms element must contain at least one Transform.");
                valid = false;
            }
        }
        result.Transforms = transforms.ToArray();

        XmlElement? digestMethod = FindDirectChild(
            element,
            OdfNamespaces.XmlDigitalSignature,
            "DigestMethod");
        if (digestMethod == null
            || CountDirectChildren(
                element,
                OdfNamespaces.XmlDigitalSignature,
                "DigestMethod") != 1)
        {
            AddSignatureDiagnostic(
                context,
                signature,
                "A Reference must contain exactly one DigestMethod element.");
            valid = false;
        }
        else
        {
            result.DigestMethod = digestMethod.GetAttribute("Algorithm");
            if (result.DigestMethod.Length == 0)
            {
                AddSignatureDiagnostic(
                    context,
                    signature,
                    "A Reference DigestMethod has no Algorithm value.");
                valid = false;
            }
        }

        XmlElement? digestValue = FindDirectChild(
            element,
            OdfNamespaces.XmlDigitalSignature,
            "DigestValue");
        if (digestValue == null
            || CountDirectChildren(
                element,
                OdfNamespaces.XmlDigitalSignature,
                "DigestValue") != 1)
        {
            AddSignatureDiagnostic(
                context,
                signature,
                "A Reference must contain exactly one DigestValue element.");
            valid = false;
        }
        else
        {
            result.DigestValueLexical = digestValue.InnerText;
            OdfLexicalValue parsed = OdfLexicalParser.ParseBase64(
                result.DigestValueLexical);
            result.DigestValue = parsed.BinaryValue;
            result.DigestValueIsValid = parsed.IsValid
                && parsed.BinaryValue.Length > 0;
            if (!result.DigestValueIsValid)
            {
                AddSignatureDiagnostic(
                    context,
                    signature,
                    "A Reference DigestValue is not valid non-empty base64.");
                valid = false;
            }
        }
        result.IsValid = valid;
        return result;
    }

    static OdfSignatureTransform ParseTransform(XmlElement element)
    {
        OdfSignatureTransform result = new OdfSignatureTransform();
        result.Algorithm = element.GetAttribute("Algorithm");
        result.InclusiveNamespacePrefixes = GetInclusivePrefixList(element);
        result.InclusivePrefixes = SplitXmlWhitespace(
            result.InclusiveNamespacePrefixes);
        result.OriginalXml = element.OuterXml;
        result.TransformElement = element;
        result.IsValid = result.Algorithm.Length > 0;
        return result;
    }

    static bool ParseKeyInfo(
        XmlElement element,
        OdfSignature signature,
        XmlSignatureParseContext context)
    {
        bool valid = true;
        List<string> keyNames = new List<string>();
        List<OdfX509Data> x509Groups = new List<OdfX509Data>();
        List<byte[]> derValues = new List<byte[]>();
        List<string> derLexical = new List<string>();
        List<bool> derValid = new List<bool>();
        List<string> keyInfoReferences = new List<string>();
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement keyElement = (XmlElement)child;
            if (IsElement(
                keyElement,
                OdfNamespaces.XmlDigitalSignature,
                "KeyName"))
            {
                keyNames.Add(keyElement.InnerText);
            }
            else if (IsElement(
                keyElement,
                OdfNamespaces.XmlDigitalSignature,
                "X509Data"))
            {
                OdfX509Data data = ParseX509Data(keyElement);
                x509Groups.Add(data);
                if (!data.IsValid)
                {
                    AddSignatureDiagnostic(
                        context,
                        signature,
                        "An X509Data element contains missing or invalid values.");
                    valid = false;
                }
            }
            else if (IsElement(
                keyElement,
                XmlDigitalSignature11Namespace,
                "DEREncodedKeyValue"))
            {
                AddBinaryElement(
                    keyElement,
                    derValues,
                    derLexical,
                    derValid);
                if (!derValid[derValid.Count - 1])
                {
                    AddSignatureDiagnostic(
                        context,
                        signature,
                        "A DEREncodedKeyValue is not valid non-empty base64.");
                    valid = false;
                }
            }
            else if (IsElement(
                keyElement,
                XmlDigitalSignature11Namespace,
                "KeyInfoReference"))
            {
                string uri = keyElement.GetAttribute("URI");
                keyInfoReferences.Add(uri);
                if (uri.Length == 0)
                {
                    AddSignatureDiagnostic(
                        context,
                        signature,
                        "A KeyInfoReference has no URI value.");
                    valid = false;
                }
            }
        }
        signature.KeyNames = keyNames.ToArray();
        signature.X509Data = x509Groups.ToArray();
        signature.DerEncodedKeyValues = derValues.ToArray();
        signature.DerEncodedKeyValueLexicalValues = derLexical.ToArray();
        signature.DerEncodedKeyValueIsValid = derValid.ToArray();
        signature.KeyInfoReferenceUris = keyInfoReferences.ToArray();
        return valid;
    }

    static OdfX509Data ParseX509Data(XmlElement element)
    {
        OdfX509Data result = new OdfX509Data();
        result.OriginalXml = element.OuterXml;
        List<string> issuerNames = new List<string>();
        List<string> serialNumbers = new List<string>();
        List<string> subjectNames = new List<string>();
        List<byte[]> subjectKeyIds = new List<byte[]>();
        List<string> subjectKeyIdLexical = new List<string>();
        List<bool> subjectKeyIdValid = new List<bool>();
        List<byte[]> certificates = new List<byte[]>();
        List<string> certificateLexical = new List<string>();
        List<bool> certificateValid = new List<bool>();
        List<byte[]> revocationLists = new List<byte[]>();
        List<string> revocationListLexical = new List<string>();
        List<bool> revocationListValid = new List<bool>();
        List<string> digestAlgorithms = new List<string>();
        List<byte[]> digests = new List<byte[]>();
        List<string> digestLexical = new List<string>();
        List<bool> digestValid = new List<bool>();
        bool valid = true;
        bool hasContent = false;

        foreach (XmlNode child in element.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            XmlElement valueElement = (XmlElement)child;
            hasContent = true;
            if (IsElement(
                valueElement,
                OdfNamespaces.XmlDigitalSignature,
                "X509IssuerSerial"))
            {
                XmlElement? issuer = FindDirectChild(
                    valueElement,
                    OdfNamespaces.XmlDigitalSignature,
                    "X509IssuerName");
                XmlElement? serial = FindDirectChild(
                    valueElement,
                    OdfNamespaces.XmlDigitalSignature,
                    "X509SerialNumber");
                string issuerValue = issuer == null ? "" : issuer.InnerText;
                string serialValue = serial == null ? "" : serial.InnerText;
                issuerNames.Add(issuerValue);
                serialNumbers.Add(serialValue);
                if (issuerValue.Length == 0 || serialValue.Length == 0)
                {
                    valid = false;
                }
            }
            else if (IsElement(
                valueElement,
                OdfNamespaces.XmlDigitalSignature,
                "X509SubjectName"))
            {
                subjectNames.Add(valueElement.InnerText);
                if (valueElement.InnerText.Length == 0)
                {
                    valid = false;
                }
            }
            else if (IsElement(
                valueElement,
                OdfNamespaces.XmlDigitalSignature,
                "X509SKI"))
            {
                AddBinaryElement(
                    valueElement,
                    subjectKeyIds,
                    subjectKeyIdLexical,
                    subjectKeyIdValid);
                if (!subjectKeyIdValid[subjectKeyIdValid.Count - 1])
                {
                    valid = false;
                }
            }
            else if (IsElement(
                valueElement,
                OdfNamespaces.XmlDigitalSignature,
                "X509Certificate"))
            {
                AddBinaryElement(
                    valueElement,
                    certificates,
                    certificateLexical,
                    certificateValid);
                if (!certificateValid[certificateValid.Count - 1])
                {
                    valid = false;
                }
            }
            else if (IsElement(
                valueElement,
                OdfNamespaces.XmlDigitalSignature,
                "X509CRL"))
            {
                AddBinaryElement(
                    valueElement,
                    revocationLists,
                    revocationListLexical,
                    revocationListValid);
                if (!revocationListValid[revocationListValid.Count - 1])
                {
                    valid = false;
                }
            }
            else if (IsElement(
                valueElement,
                XmlDigitalSignature11Namespace,
                "X509Digest"))
            {
                digestAlgorithms.Add(valueElement.GetAttribute("Algorithm"));
                AddBinaryElement(
                    valueElement,
                    digests,
                    digestLexical,
                    digestValid);
                if (digestAlgorithms[digestAlgorithms.Count - 1].Length == 0
                    || !digestValid[digestValid.Count - 1])
                {
                    valid = false;
                }
            }
        }
        result.IssuerNames = issuerNames.ToArray();
        result.SerialNumbers = serialNumbers.ToArray();
        result.SubjectNames = subjectNames.ToArray();
        result.SubjectKeyIdentifiers = subjectKeyIds.ToArray();
        result.SubjectKeyIdentifierLexicalValues = subjectKeyIdLexical.ToArray();
        result.SubjectKeyIdentifierIsValid = subjectKeyIdValid.ToArray();
        result.Certificates = certificates.ToArray();
        result.CertificateLexicalValues = certificateLexical.ToArray();
        result.CertificateIsValid = certificateValid.ToArray();
        result.CertificateRevocationLists = revocationLists.ToArray();
        result.CertificateRevocationListLexicalValues = revocationListLexical.ToArray();
        result.CertificateRevocationListIsValid = revocationListValid.ToArray();
        result.DigestAlgorithms = digestAlgorithms.ToArray();
        result.Digests = digests.ToArray();
        result.DigestLexicalValues = digestLexical.ToArray();
        result.DigestIsValid = digestValid.ToArray();
        result.IsValid = hasContent && valid;
        return result;
    }

    static void AddBinaryElement(
        XmlElement element,
        List<byte[]> values,
        List<string> lexicalValues,
        List<bool> validity)
    {
        string lexical = element.InnerText;
        OdfLexicalValue parsed = OdfLexicalParser.ParseBase64(lexical);
        values.Add(parsed.BinaryValue);
        lexicalValues.Add(lexical);
        validity.Add(parsed.IsValid && parsed.BinaryValue.Length > 0);
    }

    static bool CollectManifestReferences(
        XmlElement element,
        List<OdfSignatureReference> references,
        OdfSignature signature,
        XmlSignatureParseContext context)
    {
        bool valid = true;
        if (IsElement(element, OdfNamespaces.XmlDigitalSignature, "Manifest"))
        {
            int initialCount = references.Count;
            foreach (XmlNode child in element.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    XmlElement referenceElement = (XmlElement)child;
                    if (IsElement(
                        referenceElement,
                        OdfNamespaces.XmlDigitalSignature,
                        "Reference"))
                    {
                        references.Add(ParseReference(
                            referenceElement,
                            signature,
                            context));
                    }
                }
            }
            if (references.Count == initialCount)
            {
                AddSignatureDiagnostic(
                    context,
                    signature,
                    "An XML Signature Manifest must contain at least one Reference.");
                valid = false;
            }
        }
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                if (!CollectManifestReferences(
                    (XmlElement)child,
                    references,
                    signature,
                    context))
                {
                    valid = false;
                }
            }
        }
        return valid;
    }

    static void CollectXadesProperties(
        XmlElement element,
        List<OdfXadesProperties> properties,
        OdfSignature signature,
        XmlSignatureParseContext context)
    {
        if (element.LocalName == "QualifyingProperties"
            && IsXadesNamespace(element.NamespaceURI))
        {
            OdfXadesProperties parsed = ParseXadesProperties(element);
            properties.Add(parsed);
            if (!parsed.IsValid)
            {
                AddSignatureDiagnostic(
                    context,
                    signature,
                    "An XAdES QualifyingProperties element is structurally invalid.");
            }
            return;
        }
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                CollectXadesProperties(
                    (XmlElement)child,
                    properties,
                    signature,
                    context);
            }
        }
    }

    static OdfXadesProperties ParseXadesProperties(XmlElement element)
    {
        OdfXadesProperties result = new OdfXadesProperties();
        result.NamespaceName = element.NamespaceURI;
        result.Target = element.GetAttribute("Target");
        result.Id = element.GetAttribute("Id");
        result.OriginalXml = element.OuterXml;
        XmlElement? signedProperties = FindDirectChild(
            element,
            element.NamespaceURI,
            "SignedProperties");
        XmlElement? unsignedProperties = FindDirectChild(
            element,
            element.NamespaceURI,
            "UnsignedProperties");
        result.HasSignedProperties = signedProperties != null;
        result.HasUnsignedProperties = unsignedProperties != null;
        if (signedProperties != null)
        {
            result.SignedPropertiesId = signedProperties.GetAttribute("Id");
            result.SignedPropertiesXml = signedProperties.OuterXml;
            XmlElement? signatureProperties = FindDirectChild(
                signedProperties,
                element.NamespaceURI,
                "SignedSignatureProperties");
            XmlElement? dataProperties = FindDirectChild(
                signedProperties,
                element.NamespaceURI,
                "SignedDataObjectProperties");
            if (signatureProperties != null)
            {
                result.SignedSignaturePropertiesXml = signatureProperties.OuterXml;
                XmlElement? signingTime = FindFirstDescendant(
                    signatureProperties,
                    element.NamespaceURI,
                    "SigningTime");
                result.SigningTime = signingTime == null
                    ? ""
                    : signingTime.InnerText;
                result.HasSigningCertificate = FindFirstDescendant(
                    signatureProperties,
                    element.NamespaceURI,
                    "SigningCertificate") != null;
                result.HasSigningCertificateV2 = FindFirstDescendant(
                    signatureProperties,
                    element.NamespaceURI,
                    "SigningCertificateV2") != null;
            }
            if (dataProperties != null)
            {
                result.SignedDataObjectPropertiesXml = dataProperties.OuterXml;
            }
        }
        if (unsignedProperties != null)
        {
            result.UnsignedPropertiesXml = unsignedProperties.OuterXml;
            XmlElement? signatureProperties = FindDirectChild(
                unsignedProperties,
                element.NamespaceURI,
                "UnsignedSignatureProperties");
            XmlElement? dataProperties = FindDirectChild(
                unsignedProperties,
                element.NamespaceURI,
                "UnsignedDataObjectProperties");
            if (signatureProperties != null)
            {
                result.UnsignedSignaturePropertiesXml = signatureProperties.OuterXml;
            }
            if (dataProperties != null)
            {
                result.UnsignedDataObjectPropertiesXml = dataProperties.OuterXml;
            }
        }
        result.IsValid = result.Target.Length > 0
            && (result.HasSignedProperties || result.HasUnsignedProperties);
        return result;
    }

    static string GetInclusivePrefixList(XmlElement element)
    {
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                XmlElement prefixElement = (XmlElement)child;
                if (IsElement(
                    prefixElement,
                    ExclusiveCanonicalizationNamespace,
                    "InclusiveNamespaces"))
                {
                    return prefixElement.GetAttribute("PrefixList");
                }
            }
        }
        return "";
    }

    static string[] SplitXmlWhitespace(string value)
    {
        return value.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries);
    }

    static XmlElement? FindDirectChild(
        XmlElement parent,
        string namespaceName,
        string localName)
    {
        foreach (XmlNode child in parent.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)child;
                if (IsElement(element, namespaceName, localName))
                {
                    return element;
                }
            }
        }
        return null;
    }

    static XmlElement? FindFirstDescendant(
        XmlElement parent,
        string namespaceName,
        string localName)
    {
        foreach (XmlNode child in parent.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                XmlElement element = (XmlElement)child;
                if (IsElement(element, namespaceName, localName))
                {
                    return element;
                }
                XmlElement? descendant = FindFirstDescendant(
                    element,
                    namespaceName,
                    localName);
                if (descendant != null)
                {
                    return descendant;
                }
            }
        }
        return null;
    }

    static int CountDirectChildren(
        XmlElement parent,
        string namespaceName,
        string localName)
    {
        int result = 0;
        foreach (XmlNode child in parent.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element
                && IsElement((XmlElement)child, namespaceName, localName))
            {
                result = result + 1;
            }
        }
        return result;
    }

    static bool IsElement(
        XmlElement element,
        string namespaceName,
        string localName)
    {
        return element.NamespaceURI == namespaceName
            && element.LocalName == localName;
    }

    static bool IsXadesNamespace(string namespaceName)
    {
        return namespaceName == "http://uri.etsi.org/01903#"
            || namespaceName.StartsWith(
                "http://uri.etsi.org/01903/",
                StringComparison.Ordinal);
    }

    static bool IsSignaturePartPath(string partPath)
    {
        return partPath == OdfPartNames.DocumentSignatures
            || partPath == OdfPartNames.MacroSignatures
            || partPath == OdfPartNames.PackageSignatures;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    static void ValidateLimits(OdfSecurityLimits limits)
    {
        if (limits == null
            || limits.MaximumSingleEntryBytes <= 0
            || limits.MaximumXmlDepth <= 0
            || limits.MaximumXmlAttributesPerElement < 0
            || limits.MaximumXmlTextLength < 0)
        {
            throw new OdfSecurityException("XML signature limits must be non-negative and bounded.");
        }
    }

    static void ValidateTree(
        XmlNode node,
        int depth,
        OdfSecurityLimits limits)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            if (depth > limits.MaximumXmlDepth)
            {
                throw new OdfSecurityException("The XML signature exceeds the configured element-depth limit.");
            }
            XmlElement element = (XmlElement)node;
            if (element.Attributes.Count > limits.MaximumXmlAttributesPerElement)
            {
                throw new OdfSecurityException("The XML signature exceeds the configured per-element attribute limit.");
            }
            foreach (XmlAttribute attribute in element.Attributes)
            {
                if (attribute.Value.Length > limits.MaximumXmlTextLength)
                {
                    throw new OdfSecurityException("The XML signature exceeds the configured text-value limit.");
                }
            }
        }
        string value = node.Value ?? "";
        if (value.Length > limits.MaximumXmlTextLength)
        {
            throw new OdfSecurityException("The XML signature exceeds the configured text-value limit.");
        }
        foreach (XmlNode child in node.ChildNodes)
        {
            int childDepth = depth;
            if (child.NodeType == XmlNodeType.Element)
            {
                childDepth = depth + 1;
            }
            ValidateTree(child, childDepth, limits);
        }
    }

    static void AddContainerDiagnostic(
        XmlSignatureParseContext context,
        string message)
    {
        OdfDiagnostic diagnostic = CreateDiagnostic(context.PartPath, message);
        context.Diagnostics.Add(diagnostic);
    }

    static void AddSignatureDiagnostic(
        XmlSignatureParseContext context,
        OdfSignature signature,
        string message)
    {
        OdfDiagnostic diagnostic = CreateDiagnostic(context.PartPath, message);
        context.Diagnostics.Add(diagnostic);
        OdfDiagnostic[] updated = new OdfDiagnostic[signature.Diagnostics.Length + 1];
        Array.Copy(signature.Diagnostics, updated, signature.Diagnostics.Length);
        updated[updated.Length - 1] = diagnostic;
        signature.Diagnostics = updated;
    }

    static OdfDiagnostic CreateDiagnostic(string partPath, string message)
    {
        OdfLocation location = new OdfLocation();
        location.PartPath = partPath;
        return OdfDiagnosticFactory.Create(
            OdfDiagnosticCodes.SignatureInvalid,
            message,
            OdfValidationSeverity.Error,
            location,
            null,
            OdfVersion.Unknown);
    }
}
