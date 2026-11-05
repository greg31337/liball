using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace iso20022;

/// <summary>Discovers and verifies contained XML signatures under explicit trust and algorithm policy.</summary>
public class XmlSignatureValidator : IXmlSignatureValidator
{
    /// <summary>Initializes an XML signature validator.</summary>
    public XmlSignatureValidator()
    {
    }

    /// <summary>Validates every applicable signature in one exact raw XML message.</summary>
    /// <param name="request">The raw XML, trust policy, and resource limits.</param>
    /// <returns>The stable signature validation result.</returns>
    public SignatureValidationResult ValidateSignatures(
        SignatureValidationRequest? request)
    {
        SignatureValidationResult? requestFailure = ValidateRequest(request);
        if (requestFailure != null)
        {
            return requestFailure;
        }
        SignatureValidationRequest policy = request ??
            new SignatureValidationRequest();
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

        SignatureValidationResult result = new SignatureValidationResult();
        result.Success = true;
        result.Valid = true;
        result.Status = SignatureValidationStatus.NotPresent;
        XmlSignatureValidationState state = new XmlSignatureValidationState();
        state.Request = policy;
        state.Result = result;
        state.Tree = tree;
        List<SignatureNodeContext> signatures =
            new List<SignatureNodeContext>();
        XmlPathTracker path = new XmlPathTracker();
        int rootIndex = 0;
        while (state.Continue && rootIndex < tree.Children.Count)
        {
            CollectSignatures(tree.Children[rootIndex], path, signatures, state);
            rootIndex++;
        }
        if (!state.Continue)
        {
            return result;
        }
        if (signatures.Count == 0)
        {
            if (policy.RequireSignature)
            {
                result.Valid = false;
                AddDiagnostic(
                    state,
                    "signature.required_missing",
                    "The selected trust policy requires an XML signature.",
                    new XmlPathData(),
                    new SourceLocation(),
                    DiagnosticKind.MissingContent,
                    "At least one contained XML Signature element.");
            }
            return result;
        }

        int signatureIndex = 0;
        while (state.Continue && signatureIndex < signatures.Count)
        {
            if (policy.ValidateAllSignatures || signatureIndex == 0)
            {
                ValidateSignature(signatures[signatureIndex], state);
            }
            else
            {
                XmlSignatureResult skipped = CreateSignatureResult(
                    signatures[signatureIndex]);
                state.Result.Signatures.Add(skipped);
            }
            signatureIndex++;
        }
        if (state.Continue && result.Valid)
        {
            result.Status = SignatureValidationStatus.Valid;
        }
        return result;
    }

    internal static SignatureValidationResult? ValidateRequest(
        SignatureValidationRequest? request)
    {
        if (request == null)
        {
            return CreateFailure(
                SignatureValidationStatus.Unknown,
                "signature.request_missing",
                "A signature validation request is required.");
        }
        if (request.Xml == null)
        {
            return CreateFailure(
                SignatureValidationStatus.Unknown,
                "signature.xml_invalid",
                "The signature XML byte collection must not be null.");
        }
        if (request.TrustPolicy == null || request.Limits == null ||
            !MessageValidator.LimitsArePositive(request.Limits) ||
            request.TrustPolicy.TrustedCertificates == null ||
            request.TrustPolicy.AllowedCanonicalizationAlgorithms == null ||
            request.TrustPolicy.AllowedDigestAlgorithms == null ||
            request.TrustPolicy.AllowedSignatureAlgorithms == null)
        {
            return CreateFailure(
                SignatureValidationStatus.Unknown,
                "signature.policy_invalid",
                "Signature trust policy collections and positive resource limits are required.");
        }
        if (request.Xml.LongLength > request.Limits.MaxTotalBytes)
        {
            return CreateFailure(
                SignatureValidationStatus.ResourceLimitReached,
                "signature.total_bytes_limit",
                "The signature XML input exceeds the configured byte limit.");
        }
        return null;
    }

    internal static void CollectSignatures(
        DynamicXmlNode node,
        XmlPathTracker path,
        List<SignatureNodeContext> signatures,
        XmlSignatureValidationState state)
    {
        if (!state.Continue || node.Kind != XmlNodeKind.Element)
        {
            return;
        }
        path.EnterElement(node.Name, node.Location);
        if (node.Name.NamespaceUri == Iso20022Namespaces.XmlSignature &&
            node.Name.LocalName == "Signature")
        {
            if (signatures.Count >= state.Request.Limits.MaxCollectionCount)
            {
                SetResourceFailure(
                    state,
                    "signature.collection_limit",
                    "The discovered signature count exceeds the configured collection limit.");
            }
            else
            {
                SignatureNodeContext context = new SignatureNodeContext();
                context.Node = node;
                context.Path = path.CapturePath();
                signatures.Add(context);
            }
        }
        int index = 0;
        while (state.Continue && index < node.Children.Count)
        {
            CollectSignatures(node.Children[index], path, signatures, state);
            index++;
        }
        path.ExitElement();
    }

    internal static void ValidateSignature(
        SignatureNodeContext context,
        XmlSignatureValidationState state)
    {
        XmlSignatureResult signature = CreateSignatureResult(context);
        state.Result.Signatures.Add(signature);
        DynamicXmlNode? signedInfo = FindSignatureChild(context.Node, "SignedInfo");
        DynamicXmlNode? signatureValue =
            FindSignatureChild(context.Node, "SignatureValue");
        if (signedInfo == null || signatureValue == null)
        {
            FailSignature(
                signature,
                state,
                SignatureValidationStatus.Malformed,
                "signature.markup_malformed",
                "An XML Signature requires SignedInfo and SignatureValue elements.",
                context);
            return;
        }

        DynamicXmlNode? canonicalization =
            FindSignatureChild(signedInfo, "CanonicalizationMethod");
        DynamicXmlNode? signatureMethod =
            FindSignatureChild(signedInfo, "SignatureMethod");
        string? canonicalizationAlgorithm =
            FindAttributeValue(canonicalization, "Algorithm");
        string? signatureAlgorithm =
            FindAttributeValue(signatureMethod, "Algorithm");
        if (canonicalizationAlgorithm == null || signatureAlgorithm == null)
        {
            FailSignature(
                signature,
                state,
                SignatureValidationStatus.Malformed,
                "signature.algorithm_missing",
                "SignedInfo requires canonicalization and signature algorithm identifiers.",
                context);
            return;
        }
        signature.CanonicalizationAlgorithm = canonicalizationAlgorithm;
        signature.SignatureAlgorithm = signatureAlgorithm;
        int canonicalizationStatus = CanonicalizationPolicyStatus(
            canonicalizationAlgorithm,
            state.Request.TrustPolicy.AllowedCanonicalizationAlgorithms);
        if (canonicalizationStatus != 0 ||
            (canonicalization != null && canonicalization.Children.Count > 0))
        {
            FailAlgorithm(
                signature,
                state,
                canonicalizationStatus == 1 ?
                    SignatureValidationStatus.UnsafeAlgorithm :
                    SignatureValidationStatus.UnsupportedAlgorithm,
                canonicalizationAlgorithm,
                context);
            return;
        }
        int signatureAlgorithmStatus = SignaturePolicyStatus(
            signatureAlgorithm,
            state.Request.TrustPolicy.AllowedSignatureAlgorithms);
        if (signatureAlgorithmStatus != 0)
        {
            FailAlgorithm(
                signature,
                state,
                signatureAlgorithmStatus == 1 ?
                    SignatureValidationStatus.UnsafeAlgorithm :
                    SignatureValidationStatus.UnsupportedAlgorithm,
                signatureAlgorithm,
                context);
            return;
        }

        List<DynamicXmlNode> references =
            FindSignatureChildren(signedInfo, "Reference");
        if (references.Count == 0 ||
            references.Count > state.Request.Limits.MaxCollectionCount)
        {
            if (references.Count > state.Request.Limits.MaxCollectionCount)
            {
                SetResourceFailure(
                    state,
                    "signature.reference_limit",
                    "The signature reference count exceeds the configured collection limit.");
            }
            else
            {
                FailSignature(
                    signature,
                    state,
                    SignatureValidationStatus.Malformed,
                    "signature.reference_missing",
                    "SignedInfo requires at least one Reference element.",
                    context);
            }
            return;
        }

        bool referencesValid = true;
        int referenceIndex = 0;
        while (state.Continue && referenceIndex < references.Count)
        {
            if (!ValidateReference(
                references[referenceIndex],
                context,
                signature,
                state))
            {
                referencesValid = false;
            }
            referenceIndex++;
        }
        if (!state.Continue)
        {
            return;
        }
        if (!referencesValid)
        {
            signature.Status = DetermineReferenceFailureStatus(
                signature.References);
            state.Result.Valid = false;
            state.Result.Status = signature.Status;
            return;
        }

        IsoBinaryValue? signatureBytes = BinaryLexical.ParseBase64(
            XmlMessageReader.CollectElementText(signatureValue).AsSpan());
        if (signatureBytes == null || signatureBytes.Value.Length == 0)
        {
            FailSignature(
                signature,
                state,
                SignatureValidationStatus.Malformed,
                "signature.value_malformed",
                "SignatureValue must contain a valid non-empty base64 value.",
                context);
            return;
        }
        SignatureKeyMaterial keyMaterial = ReadKeyMaterial(context.Node, state);
        if (keyMaterial.Malformed)
        {
            DisposeKeyMaterial(keyMaterial);
            FailSignature(
                signature,
                state,
                SignatureValidationStatus.Malformed,
                "signature.key_info_malformed",
                "The XML Signature key information is malformed.",
                context);
            return;
        }
        if (keyMaterial.Key == null)
        {
            DisposeKeyMaterial(keyMaterial);
            FailSignature(
                signature,
                state,
                SignatureValidationStatus.UnsupportedAlgorithm,
                "signature.key_unsupported",
                "No supported RSA verification key is available.",
                context);
            return;
        }
        if (keyMaterial.Key.KeySize < 2048)
        {
            DisposeKeyMaterial(keyMaterial);
            FailSignature(
                signature,
                state,
                SignatureValidationStatus.UnsafeAlgorithm,
                "signature.key_unsafe",
                "RSA signature verification requires a key of at least 2048 bits.",
                context);
            return;
        }

        byte[] canonicalSignedInfo =
            SignatureCanonicalizer.CanonicalizeExclusive(signedInfo, null);
        bool signatureValid = keyMaterial.Key.VerifyData(
            canonicalSignedInfo,
            signatureBytes.Value,
            GetSignatureHashName(signatureAlgorithm),
            RSASignaturePadding.Pkcs1);
        SetCertificateIdentity(signature, keyMaterial, state.Request);
        signature.CertificateStatus = EvaluateCertificateTrust(
            keyMaterial.Certificate,
            state.Request.TrustPolicy);
        DisposeKeyMaterial(keyMaterial);
        if (!signatureValid)
        {
            FailSignature(
                signature,
                state,
                SignatureValidationStatus.Invalid,
                "signature.value_invalid",
                "The cryptographic SignatureValue does not verify over canonical SignedInfo.",
                context);
            return;
        }
        if (state.Request.TrustPolicy.RequireTrustedCertificate &&
            signature.CertificateStatus != CertificateTrustStatus.Trusted)
        {
            FailSignature(
                signature,
                state,
                SignatureValidationStatus.CertificateUntrusted,
                "signature.certificate_untrusted",
                "The signing certificate does not satisfy the selected trust policy.",
                context);
            return;
        }

        signature.Status = SignatureValidationStatus.Valid;
    }

    internal static bool ValidateReference(
        DynamicXmlNode referenceNode,
        SignatureNodeContext signatureContext,
        XmlSignatureResult signature,
        XmlSignatureValidationState state)
    {
        SignatureReferenceResult reference = new SignatureReferenceResult();
        signature.References.Add(reference);
        string? uri = FindAttributeValue(referenceNode, "URI");
        reference.Uri = uri ?? string.Empty;
        DynamicXmlNode? digestMethod =
            FindSignatureChild(referenceNode, "DigestMethod");
        DynamicXmlNode? digestValue =
            FindSignatureChild(referenceNode, "DigestValue");
        string? digestAlgorithm = FindAttributeValue(digestMethod, "Algorithm");
        if (digestAlgorithm == null || digestValue == null)
        {
            return FailReference(
                reference,
                state,
                SignatureReferenceStatus.Malformed,
                "signature.reference_malformed",
                "A Reference requires DigestMethod and DigestValue.",
                signatureContext);
        }
        reference.DigestAlgorithm = digestAlgorithm;
        int digestStatus = DigestPolicyStatus(
            digestAlgorithm,
            state.Request.TrustPolicy.AllowedDigestAlgorithms);
        if (digestStatus != 0)
        {
            return FailReference(
                reference,
                state,
                digestStatus == 1 ?
                    SignatureReferenceStatus.UnsafeAlgorithm :
                    SignatureReferenceStatus.UnsupportedAlgorithm,
                digestStatus == 1 ?
                    "signature.reference_algorithm_unsafe" :
                    "signature.reference_algorithm_unsupported",
                "The reference digest algorithm is not permitted.",
                signatureContext);
        }

        int transformStatus = ValidateTransforms(referenceNode);
        if (transformStatus != 0)
        {
            return FailReference(
                reference,
                state,
                SignatureReferenceStatus.UnsupportedAlgorithm,
                "signature.reference_transform_unsupported",
                "Only enveloped-signature followed by exclusive canonicalization is supported.",
                signatureContext);
        }
        bool enveloped = HasEnvelopedTransform(referenceNode);
        SignatureTargetSearchResult target = ResolveReferenceTarget(
            reference.Uri,
            state.Tree);
        if (target.Count < 0)
        {
            reference.ContainedInMessage = false;
            return FailReference(
                reference,
                state,
                state.Request.TrustPolicy.AllowExternalReferences ?
                    SignatureReferenceStatus.UnsupportedAlgorithm :
                    SignatureReferenceStatus.ExternalReferenceRejected,
                state.Request.TrustPolicy.AllowExternalReferences ?
                    "signature.reference_external_unsupported" :
                    "signature.reference_external_rejected",
                "External signature references are not dereferenced by this validator.",
                signatureContext);
        }
        reference.ContainedInMessage = true;
        if (target.Count == 0 || target.Node == null)
        {
            return FailReference(
                reference,
                state,
                SignatureReferenceStatus.MissingTarget,
                "signature.reference_target_missing",
                "The same-document reference target does not exist.",
                signatureContext);
        }
        if (target.Count > 1)
        {
            return FailReference(
                reference,
                state,
                SignatureReferenceStatus.AmbiguousTarget,
                "signature.reference_target_ambiguous",
                "The same-document reference identifier is duplicated.",
                signatureContext);
        }
        reference.TargetPath = SupplementaryDataValidator.CopyPath(target.Path);
        IsoBinaryValue? expected = BinaryLexical.ParseBase64(
            XmlMessageReader.CollectElementText(digestValue).AsSpan());
        if (expected == null || expected.Value.Length == 0)
        {
            return FailReference(
                reference,
                state,
                SignatureReferenceStatus.Malformed,
                "signature.reference_digest_malformed",
                "DigestValue must contain valid non-empty base64 content.",
                signatureContext);
        }
        reference.ExpectedDigest = expected;
        byte[] canonicalTarget = SignatureCanonicalizer.CanonicalizeExclusive(
            target.Node,
            enveloped ? signatureContext.Node : null);
        byte[] calculated = CalculateDigest(canonicalTarget, digestAlgorithm);
        reference.CalculatedDigest.Value = calculated;
        reference.CalculatedDigest.Encoding = BinaryEncodingKind.Base64;
        reference.CalculatedDigest.LexicalValue =
            BinaryLexical.FormatBase64(calculated);
        if (!CryptographicOperations.FixedTimeEquals(
            expected.Value,
            calculated))
        {
            return FailReference(
                reference,
                state,
                SignatureReferenceStatus.DigestMismatch,
                "signature.reference_digest_mismatch",
                "The calculated reference digest does not match DigestValue.",
                signatureContext);
        }
        reference.Status = SignatureReferenceStatus.Valid;
        return true;
    }

    internal static int ValidateTransforms(DynamicXmlNode reference)
    {
        DynamicXmlNode? transforms = FindSignatureChild(reference, "Transforms");
        if (transforms == null)
        {
            return 2;
        }
        List<DynamicXmlNode> transformNodes =
            FindSignatureChildren(transforms, "Transform");
        if (transformNodes.Count == 0 || transformNodes.Count > 2)
        {
            return 2;
        }
        bool canonical = false;
        int index = 0;
        while (index < transformNodes.Count)
        {
            string? algorithm = FindAttributeValue(transformNodes[index], "Algorithm");
            if (algorithm == SignatureAlgorithmUris.EnvelopedSignature)
            {
                if (index != 0)
                {
                    return 2;
                }
            }
            else if (algorithm == SignatureAlgorithmUris.ExclusiveCanonicalization &&
                index == transformNodes.Count - 1 &&
                transformNodes[index].Children.Count == 0)
            {
                canonical = true;
            }
            else
            {
                return 2;
            }
            index++;
        }
        return canonical ? 0 : 2;
    }

    internal static SignatureValidationStatus DetermineReferenceFailureStatus(
        List<SignatureReferenceResult> references)
    {
        int index = 0;
        while (index < references.Count)
        {
            if (references[index].Status ==
                SignatureReferenceStatus.UnsafeAlgorithm)
            {
                return SignatureValidationStatus.UnsafeAlgorithm;
            }
            index++;
        }
        index = 0;
        while (index < references.Count)
        {
            if (references[index].Status ==
                SignatureReferenceStatus.UnsupportedAlgorithm)
            {
                return SignatureValidationStatus.UnsupportedAlgorithm;
            }
            index++;
        }
        return SignatureValidationStatus.ReferenceInvalid;
    }

    internal static bool HasEnvelopedTransform(DynamicXmlNode reference)
    {
        DynamicXmlNode? transforms = FindSignatureChild(reference, "Transforms");
        if (transforms == null)
        {
            return false;
        }
        List<DynamicXmlNode> values = FindSignatureChildren(transforms, "Transform");
        return values.Count > 0 &&
            FindAttributeValue(values[0], "Algorithm") ==
                SignatureAlgorithmUris.EnvelopedSignature;
    }

    internal static SignatureTargetSearchResult ResolveReferenceTarget(
        string uri,
        DynamicXmlNode tree)
    {
        SignatureTargetSearchResult result = new SignatureTargetSearchResult();
        if (uri.Length == 0)
        {
            result.Node = tree;
            result.Count = 1;
            result.Path = FindRootPath(tree);
            return result;
        }
        if (uri[0] != '#' || uri.Length == 1 || uri.StartsWith("#xpointer("))
        {
            result.Count = -1;
            return result;
        }
        string identifier = uri.Substring(1);
        XmlPathTracker path = new XmlPathTracker();
        int index = 0;
        while (index < tree.Children.Count)
        {
            FindIdentifierTarget(tree.Children[index], identifier, path, result);
            index++;
        }
        return result;
    }

    internal static void FindIdentifierTarget(
        DynamicXmlNode node,
        string identifier,
        XmlPathTracker path,
        SignatureTargetSearchResult result)
    {
        if (node.Kind != XmlNodeKind.Element)
        {
            return;
        }
        path.EnterElement(node.Name, node.Location);
        if (ElementHasIdentifier(node, identifier))
        {
            result.Count++;
            if (result.Node == null)
            {
                result.Node = node;
                result.Path = path.CapturePath();
            }
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            FindIdentifierTarget(node.Children[index], identifier, path, result);
            index++;
        }
        path.ExitElement();
    }

    internal static bool ElementHasIdentifier(
        DynamicXmlNode node,
        string identifier)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            XmlAttributeData attribute = node.Attributes[index];
            bool identifierName = attribute.Name.NamespaceUri.Length == 0 &&
                (attribute.Name.LocalName == "Id" ||
                    attribute.Name.LocalName == "ID" ||
                    attribute.Name.LocalName == "id");
            if (!identifierName)
            {
                identifierName = attribute.Name.NamespaceUri == Iso20022Namespaces.Xml &&
                    attribute.Name.LocalName == "id";
            }
            if (identifierName && attribute.Value == identifier)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static XmlPathData FindRootPath(DynamicXmlNode tree)
    {
        int index = 0;
        while (index < tree.Children.Count)
        {
            DynamicXmlNode child = tree.Children[index];
            if (child.Kind == XmlNodeKind.Element)
            {
                return SignatureCanonicalizerPath(child.Name);
            }
            index++;
        }
        return new XmlPathData();
    }

    internal static XmlPathData SignatureCanonicalizerPath(QualifiedNameData name)
    {
        return PlaceAndNameEvaluator.AppendPath(
            new XmlPathData(),
            name,
            false,
            0);
    }

    internal static SignatureKeyMaterial ReadKeyMaterial(
        DynamicXmlNode signature,
        XmlSignatureValidationState state)
    {
        SignatureKeyMaterial material = new SignatureKeyMaterial();
        DynamicXmlNode? keyInfo = FindSignatureChild(signature, "KeyInfo");
        DynamicXmlNode? certificateNode = keyInfo == null ? null :
            FindSignatureDescendant(keyInfo, "X509Certificate");
        if (certificateNode != null &&
            state.Request.TrustPolicy.AllowEmbeddedCertificates)
        {
            IsoBinaryValue? certificateBytes = BinaryLexical.ParseBase64(
                XmlMessageReader.CollectElementText(certificateNode).AsSpan());
            if (certificateBytes == null ||
                !DerCertificateValidator.IsCertificate(certificateBytes.Value))
            {
                material.Malformed = true;
                return material;
            }
            material.CertificateBytes = certificateBytes.Value;
            material.Certificate = X509CertificateLoader.LoadCertificate(
                certificateBytes.Value);
            material.Key = material.Certificate.GetRSAPublicKey();
            return material;
        }
        if (keyInfo != null)
        {
            DynamicXmlNode? rsaKeyValue =
                FindSignatureDescendant(keyInfo, "RSAKeyValue");
            if (rsaKeyValue != null)
            {
                ReadRsaKeyValue(rsaKeyValue, material);
                return material;
            }
        }
        if (state.Request.TrustPolicy.TrustedCertificates.Count > 0)
        {
            byte[] trusted = state.Request.TrustPolicy.TrustedCertificates[0];
            if (trusted == null || !DerCertificateValidator.IsCertificate(trusted))
            {
                material.Malformed = true;
                return material;
            }
            material.CertificateBytes = trusted;
            material.Certificate = X509CertificateLoader.LoadCertificate(trusted);
            material.Key = material.Certificate.GetRSAPublicKey();
        }
        return material;
    }

    internal static void ReadRsaKeyValue(
        DynamicXmlNode rsaKeyValue,
        SignatureKeyMaterial material)
    {
        DynamicXmlNode? modulusNode =
            FindSignatureChild(rsaKeyValue, "Modulus");
        DynamicXmlNode? exponentNode =
            FindSignatureChild(rsaKeyValue, "Exponent");
        if (modulusNode == null || exponentNode == null)
        {
            material.Malformed = true;
            return;
        }
        IsoBinaryValue? modulus = BinaryLexical.ParseBase64(
            XmlMessageReader.CollectElementText(modulusNode).AsSpan());
        IsoBinaryValue? exponent = BinaryLexical.ParseBase64(
            XmlMessageReader.CollectElementText(exponentNode).AsSpan());
        if (modulus == null || exponent == null ||
            modulus.Value.Length < 256 || modulus.Value.Length > 1024 ||
            exponent.Value.Length == 0 || exponent.Value.Length > 4)
        {
            material.Malformed = true;
            return;
        }
        RSAParameters parameters = new RSAParameters();
        parameters.Modulus = modulus.Value;
        parameters.Exponent = exponent.Value;
        material.Key = RSA.Create();
        material.Key.ImportParameters(parameters);
    }

    internal static CertificateTrustStatus EvaluateCertificateTrust(
        X509Certificate2? certificate,
        SignatureTrustPolicy policy)
    {
        if (certificate == null)
        {
            return CertificateTrustStatus.NotPresent;
        }
        if (!policy.RequireTrustedCertificate)
        {
            return CertificateTrustStatus.NotEvaluated;
        }
        SignatureValidationTimeResult time = GetValidationTime(policy.ValidationTime);
        if (!time.Success)
        {
            return CertificateTrustStatus.Invalid;
        }
        DateTime notBefore = certificate.NotBefore.ToUniversalTime();
        DateTime notAfter = certificate.NotAfter.ToUniversalTime();
        if (time.Value < notBefore)
        {
            return CertificateTrustStatus.NotYetValid;
        }
        if (time.Value > notAfter)
        {
            return CertificateTrustStatus.Expired;
        }
        if (policy.TrustedCertificates.Count == 0 &&
            !policy.UseSystemTrustStore)
        {
            return CertificateTrustStatus.Untrusted;
        }

        List<X509Certificate2> anchors = new List<X509Certificate2>();
        int anchorIndex = 0;
        while (anchorIndex < policy.TrustedCertificates.Count)
        {
            byte[] anchorBytes = policy.TrustedCertificates[anchorIndex];
            if (anchorBytes == null ||
                !DerCertificateValidator.IsCertificate(anchorBytes))
            {
                DisposeCertificates(anchors);
                return CertificateTrustStatus.Invalid;
            }
            anchors.Add(X509CertificateLoader.LoadCertificate(anchorBytes));
            anchorIndex++;
        }
        bool trusted;
        CertificateTrustStatus failureStatus = CertificateTrustStatus.Untrusted;
        using (X509Chain chain = new X509Chain())
        {
            chain.ChainPolicy.RevocationMode = policy.CheckCertificateRevocation ?
                X509RevocationMode.Offline : X509RevocationMode.NoCheck;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.VerificationTime = time.Value;
            chain.ChainPolicy.DisableCertificateDownloads = true;
            if (anchors.Count > 0)
            {
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                anchorIndex = 0;
                while (anchorIndex < anchors.Count)
                {
                    chain.ChainPolicy.CustomTrustStore.Add(anchors[anchorIndex]);
                    anchorIndex++;
                }
            }
            trusted = chain.Build(certificate);
            if (!trusted)
            {
                failureStatus = ReadChainFailureStatus(chain);
            }
        }
        DisposeCertificates(anchors);
        return trusted ? CertificateTrustStatus.Trusted : failureStatus;
    }

    internal static CertificateTrustStatus ReadChainFailureStatus(X509Chain chain)
    {
        int index = 0;
        while (index < chain.ChainStatus.Length)
        {
            X509ChainStatusFlags status = chain.ChainStatus[index].Status;
            if ((status & X509ChainStatusFlags.Revoked) != 0)
            {
                return CertificateTrustStatus.Revoked;
            }
            if ((status & X509ChainStatusFlags.NotTimeValid) != 0)
            {
                return CertificateTrustStatus.Expired;
            }
            index++;
        }
        return CertificateTrustStatus.Untrusted;
    }

    internal static SignatureValidationTimeResult GetValidationTime(
        IsoDateTimeValue? supplied)
    {
        SignatureValidationTimeResult result = new SignatureValidationTimeResult();
        if (supplied == null)
        {
            result.Success = true;
            result.Value = DateTime.UtcNow;
            return result;
        }
        string? lexical = DateTimeLexical.FormatDateTime(supplied);
        IsoDateValue date = supplied.Date;
        IsoTimeValue time = supplied.Time;
        if (lexical == null || !date.HasTimezone || date.Year < 1 ||
            date.Year > 9999 || date.Month < 1 || date.Month > 12 ||
            date.Day < 1 || date.Day > DateTime.DaysInMonth(date.Year, date.Month) ||
            time.Hour < 0 || time.Hour > 23 || time.Minute < 0 ||
            time.Minute > 59 || time.Second < 0 || time.Second > 59)
        {
            return result;
        }
        DateTime value = new DateTime(
            date.Year,
            date.Month,
            date.Day,
            time.Hour,
            time.Minute,
            time.Second,
            DateTimeKind.Utc);
        value = value.AddMinutes(-date.TimezoneOffsetMinutes);
        result.Success = true;
        result.Value = value;
        return result;
    }

    internal static void SetCertificateIdentity(
        XmlSignatureResult signature,
        SignatureKeyMaterial material,
        SignatureValidationRequest request)
    {
        if (material.Certificate == null)
        {
            return;
        }
        if (request.RedactCertificateIdentifiers)
        {
            signature.CertificateIdentifierRedacted = true;
            return;
        }
        signature.CertificateIdentifier = "sha256:" +
            material.Certificate.GetCertHashString(HashAlgorithmName.SHA256);
    }

    internal static void DisposeKeyMaterial(SignatureKeyMaterial material)
    {
        if (material.Key != null)
        {
            material.Key.Dispose();
        }
        if (material.Certificate != null)
        {
            material.Certificate.Dispose();
        }
    }

    internal static void DisposeCertificates(List<X509Certificate2> certificates)
    {
        int index = 0;
        while (index < certificates.Count)
        {
            certificates[index].Dispose();
            index++;
        }
    }

    internal static byte[] CalculateDigest(byte[] content, string algorithm)
    {
        if (algorithm == SignatureAlgorithmUris.Sha1)
        {
            return SHA1.HashData(content);
        }
        if (algorithm == SignatureAlgorithmUris.Sha384)
        {
            return SHA384.HashData(content);
        }
        if (algorithm == SignatureAlgorithmUris.Sha512)
        {
            return SHA512.HashData(content);
        }
        return SHA256.HashData(content);
    }

    internal static HashAlgorithmName GetSignatureHashName(string algorithm)
    {
        if (algorithm == SignatureAlgorithmUris.RsaSha1)
        {
            return HashAlgorithmName.SHA1;
        }
        if (algorithm == SignatureAlgorithmUris.RsaSha384)
        {
            return HashAlgorithmName.SHA384;
        }
        if (algorithm == SignatureAlgorithmUris.RsaSha512)
        {
            return HashAlgorithmName.SHA512;
        }
        return HashAlgorithmName.SHA256;
    }

    internal static int CanonicalizationPolicyStatus(
        string algorithm,
        List<string> allowed)
    {
        if (algorithm != SignatureAlgorithmUris.ExclusiveCanonicalization)
        {
            return 2;
        }
        if (allowed.Count > 0 && !ContainsValue(allowed, algorithm))
        {
            return 1;
        }
        return 0;
    }

    internal static int DigestPolicyStatus(
        string algorithm,
        List<string> allowed)
    {
        bool supported = algorithm == SignatureAlgorithmUris.Sha1 ||
            algorithm == SignatureAlgorithmUris.Sha256 ||
            algorithm == SignatureAlgorithmUris.Sha384 ||
            algorithm == SignatureAlgorithmUris.Sha512;
        if (!supported)
        {
            return 2;
        }
        if (allowed.Count == 0)
        {
            return algorithm == SignatureAlgorithmUris.Sha1 ? 1 : 0;
        }
        return ContainsValue(allowed, algorithm) ? 0 : 1;
    }

    internal static int SignaturePolicyStatus(
        string algorithm,
        List<string> allowed)
    {
        bool supported = algorithm == SignatureAlgorithmUris.RsaSha1 ||
            algorithm == SignatureAlgorithmUris.RsaSha256 ||
            algorithm == SignatureAlgorithmUris.RsaSha384 ||
            algorithm == SignatureAlgorithmUris.RsaSha512;
        if (!supported)
        {
            return 2;
        }
        if (allowed.Count == 0)
        {
            return algorithm == SignatureAlgorithmUris.RsaSha1 ? 1 : 0;
        }
        return ContainsValue(allowed, algorithm) ? 0 : 1;
    }

    internal static bool ContainsValue(List<string> values, string expected)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == expected)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static DynamicXmlNode? FindSignatureChild(
        DynamicXmlNode? parent,
        string localName)
    {
        if (parent == null)
        {
            return null;
        }
        int index = 0;
        while (index < parent.Children.Count)
        {
            DynamicXmlNode child = parent.Children[index];
            if (child.Kind == XmlNodeKind.Element &&
                child.Name.NamespaceUri == Iso20022Namespaces.XmlSignature &&
                child.Name.LocalName == localName)
            {
                return child;
            }
            index++;
        }
        return null;
    }

    internal static List<DynamicXmlNode> FindSignatureChildren(
        DynamicXmlNode parent,
        string localName)
    {
        List<DynamicXmlNode> result = new List<DynamicXmlNode>();
        int index = 0;
        while (index < parent.Children.Count)
        {
            DynamicXmlNode child = parent.Children[index];
            if (child.Kind == XmlNodeKind.Element &&
                child.Name.NamespaceUri == Iso20022Namespaces.XmlSignature &&
                child.Name.LocalName == localName)
            {
                result.Add(child);
            }
            index++;
        }
        return result;
    }

    internal static DynamicXmlNode? FindSignatureDescendant(
        DynamicXmlNode parent,
        string localName)
    {
        int index = 0;
        while (index < parent.Children.Count)
        {
            DynamicXmlNode child = parent.Children[index];
            if (child.Kind == XmlNodeKind.Element &&
                child.Name.NamespaceUri == Iso20022Namespaces.XmlSignature &&
                child.Name.LocalName == localName)
            {
                return child;
            }
            DynamicXmlNode? found = FindSignatureDescendant(child, localName);
            if (found != null)
            {
                return found;
            }
            index++;
        }
        return null;
    }

    internal static string? FindAttributeValue(
        DynamicXmlNode? node,
        string localName)
    {
        if (node == null)
        {
            return null;
        }
        int index = 0;
        while (index < node.Attributes.Count)
        {
            XmlAttributeData attribute = node.Attributes[index];
            if (attribute.Name.NamespaceUri.Length == 0 &&
                attribute.Name.LocalName == localName)
            {
                return attribute.Value;
            }
            index++;
        }
        return null;
    }

    internal static XmlSignatureResult CreateSignatureResult(
        SignatureNodeContext context)
    {
        XmlSignatureResult result = new XmlSignatureResult();
        result.Path = SupplementaryDataValidator.CopyPath(context.Path);
        string? identifier = FindAttributeValue(context.Node, "Id");
        if (identifier != null)
        {
            result.Identifier = identifier;
        }
        return result;
    }

    internal static bool FailReference(
        SignatureReferenceResult reference,
        XmlSignatureValidationState state,
        SignatureReferenceStatus status,
        string code,
        string message,
        SignatureNodeContext context)
    {
        reference.Status = status;
        AddDiagnostic(
            state,
            code,
            message,
            context.Path,
            context.Node.Location,
            status == SignatureReferenceStatus.ExternalReferenceRejected ?
                DiagnosticKind.Security : DiagnosticKind.ValidationFailure,
            "A unique contained reference with permitted transforms and a matching digest.");
        return false;
    }

    internal static void FailAlgorithm(
        XmlSignatureResult signature,
        XmlSignatureValidationState state,
        SignatureValidationStatus status,
        string algorithm,
        SignatureNodeContext context)
    {
        FailSignature(
            signature,
            state,
            status,
            status == SignatureValidationStatus.UnsafeAlgorithm ?
                "signature.algorithm_unsafe" :
                "signature.algorithm_unsupported",
            "The XML Signature algorithm is not permitted: " + algorithm,
            context);
    }

    internal static void FailSignature(
        XmlSignatureResult signature,
        XmlSignatureValidationState state,
        SignatureValidationStatus status,
        string code,
        string message,
        SignatureNodeContext context)
    {
        signature.Status = status;
        state.Result.Valid = false;
        state.Result.Status = status;
        AddDiagnostic(
            state,
            code,
            message,
            context.Path,
            context.Node.Location,
            status == SignatureValidationStatus.CertificateUntrusted ||
                status == SignatureValidationStatus.UnsafeAlgorithm ?
                DiagnosticKind.Security : DiagnosticKind.ValidationFailure,
            "A well-formed signature using permitted algorithms, references, and trusted key material.");
    }

    internal static void AddDiagnostic(
        XmlSignatureValidationState state,
        string code,
        string message,
        XmlPathData path,
        SourceLocation location,
        DiagnosticKind kind,
        string expected)
    {
        if (state.Result.Diagnostics.Count >=
            state.Request.Limits.MaxDiagnosticCount)
        {
            SetResourceFailure(
                state,
                "signature.diagnostic_limit",
                "The signature diagnostics exceed the configured limit.");
            return;
        }
        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = code;
        diagnostic.RuleIdentifier = "xmldsig.core.validation";
        diagnostic.Severity = ValidationSeverity.Error;
        diagnostic.Stage = ValidationStage.Signature;
        diagnostic.Kind = kind;
        diagnostic.Message = message;
        diagnostic.Path = SupplementaryDataValidator.CopyPath(path);
        diagnostic.Location = XmlPathTracker.CopyLocation(location);
        diagnostic.ExpectedConstraint = expected;
        state.Result.Diagnostics.Add(diagnostic);
        state.Result.Valid = false;
    }

    internal static void SetResourceFailure(
        XmlSignatureValidationState state,
        string code,
        string message)
    {
        state.Continue = false;
        state.Result.Success = false;
        state.Result.Valid = false;
        state.Result.Status = SignatureValidationStatus.ResourceLimitReached;
        state.Result.ErrorCode = code;
        state.Result.ErrorMessage = message;
    }

    internal static SignatureValidationResult CreateFailure(
        SignatureValidationStatus status,
        string code,
        string message)
    {
        SignatureValidationResult result = new SignatureValidationResult();
        result.Status = status;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
