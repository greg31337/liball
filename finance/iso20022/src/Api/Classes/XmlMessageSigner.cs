using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace iso20022;

/// <summary>Signs contained XML message paths with deterministic XML Signature markup.</summary>
public class XmlMessageSigner : IXmlMessageSigner
{
    /// <summary>Initializes an XML message signer.</summary>
    public XmlMessageSigner()
    {
    }

    /// <summary>Signs selected contained message paths and inserts one XML signature deterministically.</summary>
    /// <param name="request">The source XML, key material, algorithms, references, and insertion path.</param>
    /// <returns>The stable signing result and signed XML.</returns>
    public MessageSigningResult SignMessage(MessageSigningRequest? request)
    {
        MessageSigningResult? requestFailure = ValidateRequest(request);
        if (requestFailure != null)
        {
            return requestFailure;
        }
        MessageSigningRequest policy = request ?? new MessageSigningRequest();
        RSA? key = policy.SigningKey as RSA;
        if (key == null)
        {
            return CreateFailure(
                MessageSigningStatus.KeyUnavailable,
                "signing.key_unavailable",
                "A supported RSA signing key is required.");
        }
        if (key.KeySize < 2048)
        {
            return CreateFailure(
                MessageSigningStatus.UnsafeAlgorithm,
                "signing.key_unsafe",
                "RSA signing requires a key of at least 2048 bits.");
        }

        string canonicalizationAlgorithm = policy.CanonicalizationAlgorithm;
        if (canonicalizationAlgorithm.Length == 0)
        {
            canonicalizationAlgorithm =
                SignatureAlgorithmUris.ExclusiveCanonicalization;
        }
        string digestAlgorithm = policy.DigestAlgorithm;
        if (digestAlgorithm.Length == 0)
        {
            digestAlgorithm = SignatureAlgorithmUris.Sha256;
        }
        string signatureAlgorithm = policy.SignatureAlgorithm;
        if (signatureAlgorithm.Length == 0)
        {
            signatureAlgorithm = SignatureAlgorithmUris.RsaSha256;
        }
        MessageSigningResult? algorithmFailure = ValidateAlgorithms(
            canonicalizationAlgorithm,
            digestAlgorithm,
            signatureAlgorithm);
        if (algorithmFailure != null)
        {
            return algorithmFailure;
        }

        MessageReadOptions readOptions = new MessageReadOptions();
        readOptions.NamespaceMode = NamespaceMode.Strict;
        readOptions.UnknownContentMode = UnknownContentMode.Preserve;
        readOptions.Limits = policy.Limits;
        readOptions.PreserveComments = true;
        readOptions.PreserveProcessingInstructions = true;
        readOptions.PreserveWhitespace = true;
        readOptions.PreserveLexicalValues = true;
        DynamicXmlNode tree = new GenericXmlNodeReader(readOptions)
            .ReadDocumentFromBytes(policy.Xml, null);
        SigningPathSearchResult insertion = ResolvePath(
            tree,
            policy.InsertionPath);
        if (insertion.Count != 1 || insertion.Node == null)
        {
            return CreateLocationFailure(
                insertion.Count,
                policy.InsertionPath,
                "signing.insertion_location_invalid",
                "The signature insertion path must select exactly one contained element.");
        }

        List<SigningReferenceData> references = BuildReferences(
            tree,
            policy.ReferencePaths,
            digestAlgorithm,
            policy.Limits);
        if (references.Count == 0)
        {
            return CreateFailure(
                MessageSigningStatus.InvalidRequest,
                "signing.reference_invalid",
                "Every explicit signing reference must resolve uniquely and have a unique XML identifier.");
        }

        DynamicXmlNode signature = BuildSignatureNode(
            references,
            key,
            policy,
            canonicalizationAlgorithm,
            digestAlgorithm,
            signatureAlgorithm);
        DynamicXmlNode? signedInfo =
            XmlSignatureValidator.FindSignatureChild(signature, "SignedInfo");
        if (signedInfo == null)
        {
            return CreateFailure(
                MessageSigningStatus.InvalidRequest,
                "signing.internal_signature_invalid",
                "The generated SignedInfo element is unavailable.");
        }
        byte[] canonicalSignedInfo =
            SignatureCanonicalizer.CanonicalizeExclusive(signedInfo, null);
        byte[] signatureValue = key.SignData(
            canonicalSignedInfo,
            XmlSignatureValidator.GetSignatureHashName(signatureAlgorithm),
            RSASignaturePadding.Pkcs1);
        AddSignatureValue(signature, signatureValue);
        insertion.Node.Children.Add(signature);

        MessageWriteOptions writeOptions = CreateWriteOptions(policy.Limits);
        byte[] signedXml = new GenericXmlNodeWriter(writeOptions).WriteBytes(tree);
        if (signedXml.LongLength > policy.Limits.MaxTotalBytes)
        {
            return CreateFailure(
                MessageSigningStatus.ResourceLimitReached,
                "signing.output_bytes_limit",
                "The signed XML exceeds the configured byte limit.");
        }

        MessageSigningResult result = new MessageSigningResult();
        result.Success = true;
        result.Status = MessageSigningStatus.Signed;
        result.Xml = signedXml;
        result.Signature = CreateGeneratedMetadata(
            references,
            insertion.Path,
            policy,
            canonicalizationAlgorithm,
            signatureAlgorithm);
        if (policy.VerifyAfterSigning)
        {
            ApplyPostSigningVerification(result, policy);
        }
        return result;
    }

    internal static MessageSigningResult? ValidateRequest(
        MessageSigningRequest? request)
    {
        if (request == null)
        {
            return CreateFailure(
                MessageSigningStatus.InvalidRequest,
                "signing.request_missing",
                "A message signing request is required.");
        }
        if (request.Xml == null || request.Limits == null ||
            request.InsertionPath == null || request.ReferencePaths == null ||
            !MessageValidator.LimitsArePositive(request.Limits))
        {
            return CreateFailure(
                MessageSigningStatus.InvalidRequest,
                "signing.request_invalid",
                "Signing XML, paths, and positive resource limits are required.");
        }
        if (request.Xml.LongLength > request.Limits.MaxTotalBytes)
        {
            return CreateFailure(
                MessageSigningStatus.ResourceLimitReached,
                "signing.total_bytes_limit",
                "The source XML exceeds the configured byte limit.");
        }
        if (request.InsertionPath.Value.Length == 0 &&
            request.InsertionPath.Segments.Count == 0)
        {
            return CreateFailure(
                MessageSigningStatus.InvalidRequest,
                "signing.insertion_path_missing",
                "An explicit deterministic signature insertion path is required.");
        }
        if (request.ReferencePaths.Count > request.Limits.MaxCollectionCount)
        {
            return CreateFailure(
                MessageSigningStatus.ResourceLimitReached,
                "signing.reference_limit",
                "The signing reference count exceeds the configured collection limit.");
        }
        return null;
    }

    internal static MessageSigningResult? ValidateAlgorithms(
        string canonicalizationAlgorithm,
        string digestAlgorithm,
        string signatureAlgorithm)
    {
        int canonicalizationStatus =
            XmlSignatureValidator.CanonicalizationPolicyStatus(
                canonicalizationAlgorithm,
                new List<string>());
        int digestStatus = XmlSignatureValidator.DigestPolicyStatus(
            digestAlgorithm,
            new List<string>());
        int signatureStatus = XmlSignatureValidator.SignaturePolicyStatus(
            signatureAlgorithm,
            new List<string>());
        if (canonicalizationStatus == 2 || digestStatus == 2 ||
            signatureStatus == 2)
        {
            return CreateFailure(
                MessageSigningStatus.UnsupportedAlgorithm,
                "signing.algorithm_unsupported",
                "A requested signing algorithm is not supported.");
        }
        if (canonicalizationStatus == 1 || digestStatus == 1 ||
            signatureStatus == 1)
        {
            return CreateFailure(
                MessageSigningStatus.UnsafeAlgorithm,
                "signing.algorithm_unsafe",
                "A requested signing algorithm is rejected by secure policy.");
        }
        return null;
    }

    internal static List<SigningReferenceData> BuildReferences(
        DynamicXmlNode tree,
        List<XmlPathData> paths,
        string digestAlgorithm,
        ResourceLimits limits)
    {
        List<SigningReferenceData> references =
            new List<SigningReferenceData>();
        if (paths.Count == 0)
        {
            SigningReferenceData wholeDocument = new SigningReferenceData();
            wholeDocument.Node = tree;
            wholeDocument.Path = XmlSignatureValidator.FindRootPath(tree);
            wholeDocument.Digest = XmlSignatureValidator.CalculateDigest(
                SignatureCanonicalizer.CanonicalizeExclusive(tree, null),
                digestAlgorithm);
            references.Add(wholeDocument);
            return references;
        }

        int index = 0;
        while (index < paths.Count)
        {
            SigningPathSearchResult target = ResolvePath(tree, paths[index]);
            if (target.Count != 1 || target.Node == null)
            {
                references.Clear();
                return references;
            }
            string? identifier = FindIdentifier(target.Node);
            if (identifier == null || identifier.Length == 0)
            {
                references.Clear();
                return references;
            }
            SignatureTargetSearchResult identifierTarget =
                XmlSignatureValidator.ResolveReferenceTarget(
                    "#" + identifier,
                    tree);
            if (identifierTarget.Count != 1 || identifierTarget.Node == null ||
                !object.ReferenceEquals(identifierTarget.Node, target.Node))
            {
                references.Clear();
                return references;
            }
            if (references.Count >= limits.MaxCollectionCount)
            {
                references.Clear();
                return references;
            }
            SigningReferenceData reference = new SigningReferenceData();
            reference.Node = target.Node;
            reference.Uri = "#" + identifier;
            reference.Path = target.Path;
            reference.Digest = XmlSignatureValidator.CalculateDigest(
                SignatureCanonicalizer.CanonicalizeExclusive(target.Node, null),
                digestAlgorithm);
            references.Add(reference);
            index++;
        }
        return references;
    }

    internal static DynamicXmlNode BuildSignatureNode(
        List<SigningReferenceData> references,
        RSA key,
        MessageSigningRequest request,
        string canonicalizationAlgorithm,
        string digestAlgorithm,
        string signatureAlgorithm)
    {
        DynamicXmlNode signature = CreateSignatureElement("Signature");
        NamespaceDeclarationData declaration = new NamespaceDeclarationData();
        declaration.Prefix = "ds";
        declaration.NamespaceUri = Iso20022Namespaces.XmlSignature;
        signature.NamespaceDeclarations.Add(declaration);

        DynamicXmlNode signedInfo = CreateSignatureElement("SignedInfo");
        DynamicXmlNode canonicalization =
            CreateSignatureElement("CanonicalizationMethod");
        AddAttribute(canonicalization, "Algorithm", canonicalizationAlgorithm);
        signedInfo.Children.Add(canonicalization);
        DynamicXmlNode signatureMethod =
            CreateSignatureElement("SignatureMethod");
        AddAttribute(signatureMethod, "Algorithm", signatureAlgorithm);
        signedInfo.Children.Add(signatureMethod);
        int referenceIndex = 0;
        while (referenceIndex < references.Count)
        {
            signedInfo.Children.Add(CreateReferenceNode(
                references[referenceIndex],
                digestAlgorithm));
            referenceIndex++;
        }
        signature.Children.Add(signedInfo);
        AddKeyInformation(signature, key, request);
        return signature;
    }

    internal static DynamicXmlNode CreateReferenceNode(
        SigningReferenceData reference,
        string digestAlgorithm)
    {
        DynamicXmlNode node = CreateSignatureElement("Reference");
        AddAttribute(node, "URI", reference.Uri);
        DynamicXmlNode transforms = CreateSignatureElement("Transforms");
        DynamicXmlNode enveloped = CreateSignatureElement("Transform");
        AddAttribute(
            enveloped,
            "Algorithm",
            SignatureAlgorithmUris.EnvelopedSignature);
        transforms.Children.Add(enveloped);
        DynamicXmlNode canonical = CreateSignatureElement("Transform");
        AddAttribute(
            canonical,
            "Algorithm",
            SignatureAlgorithmUris.ExclusiveCanonicalization);
        transforms.Children.Add(canonical);
        node.Children.Add(transforms);
        DynamicXmlNode digestMethod = CreateSignatureElement("DigestMethod");
        AddAttribute(digestMethod, "Algorithm", digestAlgorithm);
        node.Children.Add(digestMethod);
        DynamicXmlNode digestValue = CreateSignatureElement("DigestValue");
        digestValue.Children.Add(CreateTextNode(
            BinaryLexical.FormatBase64(reference.Digest)));
        node.Children.Add(digestValue);
        return node;
    }

    internal static void AddKeyInformation(
        DynamicXmlNode signature,
        RSA key,
        MessageSigningRequest request)
    {
        DynamicXmlNode keyInfo = CreateSignatureElement("KeyInfo");
        if (request.KeyName.Length > 0)
        {
            DynamicXmlNode keyName = CreateSignatureElement("KeyName");
            keyName.Children.Add(CreateTextNode(request.KeyName));
            keyInfo.Children.Add(keyName);
        }
        if (request.IncludeCertificate && request.Certificate != null)
        {
            DynamicXmlNode x509Data = CreateSignatureElement("X509Data");
            DynamicXmlNode certificate =
                CreateSignatureElement("X509Certificate");
            certificate.Children.Add(CreateTextNode(
                BinaryLexical.FormatBase64(
                    request.Certificate.Export(X509ContentType.Cert))));
            x509Data.Children.Add(certificate);
            keyInfo.Children.Add(x509Data);
        }
        else
        {
            RSAParameters parameters = key.ExportParameters(false);
            DynamicXmlNode keyValue = CreateSignatureElement("KeyValue");
            DynamicXmlNode rsaKeyValue = CreateSignatureElement("RSAKeyValue");
            DynamicXmlNode modulus = CreateSignatureElement("Modulus");
            modulus.Children.Add(CreateTextNode(
                BinaryLexical.FormatBase64(parameters.Modulus ?? [])));
            DynamicXmlNode exponent = CreateSignatureElement("Exponent");
            exponent.Children.Add(CreateTextNode(
                BinaryLexical.FormatBase64(parameters.Exponent ?? [])));
            rsaKeyValue.Children.Add(modulus);
            rsaKeyValue.Children.Add(exponent);
            keyValue.Children.Add(rsaKeyValue);
            keyInfo.Children.Add(keyValue);
        }
        signature.Children.Add(keyInfo);
    }

    internal static void AddSignatureValue(
        DynamicXmlNode signature,
        byte[] value)
    {
        DynamicXmlNode signatureValue =
            CreateSignatureElement("SignatureValue");
        signatureValue.Children.Add(CreateTextNode(
            BinaryLexical.FormatBase64(value)));
        signature.Children.Insert(1, signatureValue);
    }

    internal static DynamicXmlNode CreateSignatureElement(string localName)
    {
        DynamicXmlNode node = new DynamicXmlNode();
        node.Kind = XmlNodeKind.Element;
        node.Name.NamespaceUri = Iso20022Namespaces.XmlSignature;
        node.Name.LocalName = localName;
        node.Name.Prefix = "ds";
        return node;
    }

    internal static DynamicXmlNode CreateTextNode(string value)
    {
        DynamicXmlNode node = new DynamicXmlNode();
        node.Kind = XmlNodeKind.Text;
        node.Value = value;
        return node;
    }

    internal static void AddAttribute(
        DynamicXmlNode node,
        string localName,
        string value)
    {
        XmlAttributeData attribute = new XmlAttributeData();
        attribute.Name.LocalName = localName;
        attribute.Value = value;
        attribute.IsSpecified = true;
        node.Attributes.Add(attribute);
    }

    internal static SigningPathSearchResult ResolvePath(
        DynamicXmlNode tree,
        XmlPathData requested)
    {
        SigningPathSearchResult result = new SigningPathSearchResult();
        XmlPathTracker path = new XmlPathTracker();
        int index = 0;
        while (index < tree.Children.Count)
        {
            FindPathMatch(tree.Children[index], requested, path, result);
            index++;
        }
        return result;
    }

    internal static void FindPathMatch(
        DynamicXmlNode node,
        XmlPathData requested,
        XmlPathTracker path,
        SigningPathSearchResult result)
    {
        if (node.Kind != XmlNodeKind.Element)
        {
            return;
        }
        path.EnterElement(node.Name, node.Location);
        XmlPathData current = path.CapturePath();
        if (SigningPathMatches(requested, current))
        {
            result.Count++;
            if (result.Node == null)
            {
                result.Node = node;
                result.Path = current;
            }
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            FindPathMatch(node.Children[index], requested, path, result);
            index++;
        }
        path.ExitElement();
    }

    internal static bool SigningPathMatches(
        XmlPathData requested,
        XmlPathData current)
    {
        if (requested.Segments.Count > 0)
        {
            if (requested.Segments.Count != current.Segments.Count)
            {
                return false;
            }
            int segmentIndex = 0;
            while (segmentIndex < requested.Segments.Count)
            {
                QualifiedNameData expected = requested.Segments[segmentIndex];
                QualifiedNameData actual = current.Segments[segmentIndex];
                if (expected.LocalName != actual.LocalName ||
                    expected.NamespaceUri != actual.NamespaceUri)
                {
                    return false;
                }
                segmentIndex++;
            }
            return true;
        }
        return requested.Value == current.Value ||
            requested.Value == CreateLocalPath(current);
    }

    internal static string CreateLocalPath(XmlPathData path)
    {
        System.Text.StringBuilder display = new System.Text.StringBuilder();
        int index = 0;
        while (index < path.Segments.Count)
        {
            display.Append('/');
            display.Append(path.Segments[index].LocalName);
            index++;
        }
        return display.ToString();
    }

    internal static string? FindIdentifier(DynamicXmlNode node)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            XmlAttributeData attribute = node.Attributes[index];
            bool isIdentifier = attribute.Name.NamespaceUri.Length == 0 &&
                (attribute.Name.LocalName == "Id" ||
                    attribute.Name.LocalName == "ID" ||
                    attribute.Name.LocalName == "id");
            if (!isIdentifier)
            {
                isIdentifier = attribute.Name.NamespaceUri == Iso20022Namespaces.Xml &&
                    attribute.Name.LocalName == "id";
            }
            if (isIdentifier)
            {
                return attribute.Value;
            }
            index++;
        }
        return null;
    }

    internal static MessageWriteOptions CreateWriteOptions(ResourceLimits limits)
    {
        MessageWriteOptions options = new MessageWriteOptions();
        options.FormattingMode = XmlFormattingMode.Compact;
        options.UnknownContentMode = UnknownContentMode.Preserve;
        options.Limits = limits;
        options.WriteXmlDeclaration = false;
        options.WriteByteOrderMark = false;
        options.PreservePrefixes = true;
        options.PreserveLexicalValues = true;
        return options;
    }

    internal static XmlSignatureResult CreateGeneratedMetadata(
        List<SigningReferenceData> references,
        XmlPathData insertionPath,
        MessageSigningRequest request,
        string canonicalizationAlgorithm,
        string signatureAlgorithm)
    {
        XmlSignatureResult signature = new XmlSignatureResult();
        signature.Path = PlaceAndNameEvaluator.AppendPath(
            insertionPath,
            CreateSignatureElement("Signature").Name,
            false,
            0);
        signature.CanonicalizationAlgorithm = canonicalizationAlgorithm;
        signature.SignatureAlgorithm = signatureAlgorithm;
        signature.Status = SignatureValidationStatus.Valid;
        signature.CertificateStatus = request.Certificate == null ?
            CertificateTrustStatus.NotPresent :
            CertificateTrustStatus.NotEvaluated;
        int index = 0;
        while (index < references.Count)
        {
            SignatureReferenceResult reference = new SignatureReferenceResult();
            reference.Uri = references[index].Uri;
            reference.TargetPath = SupplementaryDataValidator.CopyPath(
                references[index].Path);
            reference.DigestAlgorithm = request.DigestAlgorithm.Length == 0 ?
                SignatureAlgorithmUris.Sha256 : request.DigestAlgorithm;
            reference.ExpectedDigest.Value = references[index].Digest;
            reference.ExpectedDigest.Encoding = BinaryEncodingKind.Base64;
            reference.ExpectedDigest.LexicalValue =
                BinaryLexical.FormatBase64(references[index].Digest);
            reference.CalculatedDigest.Value = references[index].Digest;
            reference.CalculatedDigest.Encoding = BinaryEncodingKind.Base64;
            reference.CalculatedDigest.LexicalValue =
                reference.ExpectedDigest.LexicalValue;
            reference.Status = SignatureReferenceStatus.Valid;
            reference.ContainedInMessage = true;
            signature.References.Add(reference);
            index++;
        }
        return signature;
    }

    internal static void ApplyPostSigningVerification(
        MessageSigningResult result,
        MessageSigningRequest request)
    {
        SignatureValidationRequest verificationRequest =
            new SignatureValidationRequest();
        verificationRequest.Xml = result.Xml;
        verificationRequest.Limits = request.Limits;
        verificationRequest.TrustPolicy.UseSystemTrustStore = false;
        verificationRequest.TrustPolicy.CheckCertificateRevocation = false;
        bool certificateIncluded = request.IncludeCertificate &&
            request.Certificate != null;
        verificationRequest.TrustPolicy.RequireTrustedCertificate =
            certificateIncluded;
        if (certificateIncluded && request.Certificate != null)
        {
            verificationRequest.TrustPolicy.TrustedCertificates.Add(
                request.Certificate.Export(X509ContentType.Cert));
        }
        SignatureValidationResult verification =
            new XmlSignatureValidator().ValidateSignatures(verificationRequest);
        if (verification.Signatures.Count > 0)
        {
            result.Signature = verification.Signatures[0];
        }
        if (verification.Success && verification.Valid)
        {
            return;
        }
        result.Success = false;
        result.Status = verification.Status ==
            SignatureValidationStatus.ResourceLimitReached ?
            MessageSigningStatus.ResourceLimitReached :
            MessageSigningStatus.VerificationFailed;
        result.ErrorCode = "signing.verification_failed";
        result.ErrorMessage = verification.ErrorMessage.Length > 0 ?
            verification.ErrorMessage :
            "Post-signing verification did not accept the generated signature.";
        int index = 0;
        while (index < verification.Diagnostics.Count)
        {
            result.Diagnostics.Add(verification.Diagnostics[index]);
            index++;
        }
    }

    internal static MessageSigningResult CreateLocationFailure(
        int count,
        XmlPathData path,
        string code,
        string message)
    {
        MessageSigningResult result = CreateFailure(
            MessageSigningStatus.InsertionLocationNotFound,
            code,
            message);
        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = code;
        diagnostic.RuleIdentifier = "xmldsig.signing.location";
        diagnostic.Severity = ValidationSeverity.Error;
        diagnostic.Stage = ValidationStage.Signature;
        diagnostic.Kind = count > 1 ?
            DiagnosticKind.Cardinality : DiagnosticKind.MissingContent;
        diagnostic.Message = message;
        diagnostic.Path = SupplementaryDataValidator.CopyPath(path);
        diagnostic.ExpectedConstraint = "Exactly one contained signature insertion element.";
        result.Diagnostics.Add(diagnostic);
        return result;
    }

    internal static MessageSigningResult CreateFailure(
        MessageSigningStatus status,
        string code,
        string message)
    {
        MessageSigningResult result = new MessageSigningResult();
        result.Status = status;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
