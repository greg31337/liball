using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace iso20022;

/// <summary>Calculates deterministic byte, canonical XML, and semantic message checksums.</summary>
public class MessageChecksum : IMessageChecksumCalculator
{
    /// <summary>Initializes a message checksum calculator.</summary>
    public MessageChecksum()
    {
    }

    /// <summary>Calculates one bounded checksum and compares it with an expected value when supplied.</summary>
    /// <param name="request">The message representation, checksum mode, scope, algorithm, and limits.</param>
    /// <returns>The stable checksum result.</returns>
    public ChecksumResult CalculateChecksum(ChecksumRequest? request)
    {
        ChecksumResult? requestFailure = ValidateRequest(request);
        if (requestFailure != null)
        {
            return requestFailure;
        }
        ChecksumRequest policy = request ?? new ChecksumRequest();
        string algorithm = NormalizeAlgorithm(policy.Algorithm);
        if (algorithm.Length == 0)
        {
            return CreateFailure(
                policy,
                ChecksumStatus.UnsupportedAlgorithm,
                "checksum.algorithm_unsupported",
                "Checksum processing supports SHA-256, SHA-384, and SHA-512.");
        }

        bool hasXml = policy.Xml.Length > 0;
        bool hasDocument = policy.Document != null;
        bool hasModel = policy.Model != null;
        int representationCount = (hasXml ? 1 : 0) +
            (hasDocument ? 1 : 0) + (hasModel ? 1 : 0);
        if (representationCount != 1)
        {
            return CreateFailure(
                policy,
                ChecksumStatus.InvalidInput,
                "checksum.input_ambiguous",
                "Exactly one XML, generic document, or semantic model representation is required.");
        }
        if (hasXml && policy.Xml.LongLength > policy.Limits.MaxTotalBytes)
        {
            return CreateFailure(
                policy,
                ChecksumStatus.ResourceLimitReached,
                "checksum.input_bytes_limit",
                "The checksum input exceeds the configured byte limit.");
        }

        bool arbitraryModel = hasModel &&
            policy.Model as DynamicXmlNode == null &&
            policy.Model as MessageDocument == null;
        if (arbitraryModel)
        {
            return CalculateSemanticModelChecksum(policy, algorithm);
        }

        DynamicXmlNode? tree = CreateInputTree(policy);
        if (tree == null)
        {
            return CreateFailure(
                policy,
                ChecksumStatus.InvalidInput,
                "checksum.content_missing",
                "The supplied message representation does not contain generic XML content.");
        }

        ChecksumResult result = CreateResult(policy, algorithm);
        int duplicateStatus = FindDuplicateKeys(tree, policy, result);
        if (duplicateStatus == 2)
        {
            return CreateFailure(
                policy,
                ChecksumStatus.ResourceLimitReached,
                "checksum.duplicate_key_limit",
                "Duplicate-key evaluation exceeded a configured collection limit.");
        }
        if (duplicateStatus == 1 && policy.RejectDuplicateKeys)
        {
            result.Success = false;
            result.Status = ChecksumStatus.DuplicateKey;
            result.ErrorCode = "checksum.duplicate_key";
            result.ErrorMessage =
                "Duplicate semantic key values prevent checksum completion.";
            return result;
        }

        DynamicXmlNode? scoped = SelectScope(tree, policy.Scope);
        if (scoped == null)
        {
            return CreateFailure(
                policy,
                ChecksumStatus.InvalidInput,
                "checksum.body_ambiguous",
                "Message-body scope requires exactly one contained Document element.");
        }
        byte[] representation = CreateRepresentation(scoped, policy);
        if (representation.LongLength > policy.Limits.MaxTotalBytes)
        {
            return CreateFailure(
                policy,
                ChecksumStatus.ResourceLimitReached,
                "checksum.representation_bytes_limit",
                "The normalized checksum representation exceeds the configured byte limit.");
        }

        CompleteResult(result, representation, algorithm, policy.ExpectedChecksum);
        return result;
    }

    internal static ChecksumResult? ValidateRequest(ChecksumRequest? request)
    {
        if (request == null)
        {
            ChecksumRequest missing = new ChecksumRequest();
            return CreateFailure(
                missing,
                ChecksumStatus.InvalidInput,
                "checksum.request_missing",
                "A checksum request is required.");
        }
        if (request.Xml == null || request.ExpectedChecksum == null ||
            request.KeyPaths == null || request.Limits == null ||
            request.Algorithm == null ||
            !MessageValidator.LimitsArePositive(request.Limits))
        {
            return CreateFailure(
                request,
                ChecksumStatus.InvalidInput,
                "checksum.request_invalid",
                "Checksum inputs, collections, algorithm, and positive limits are required.");
        }
        if (request.Scope != ChecksumScope.WholeMessage &&
            request.Scope != ChecksumScope.MessageBody)
        {
            return CreateFailure(
                request,
                ChecksumStatus.InvalidInput,
                "checksum.scope_invalid",
                "A whole-message or message-body checksum scope is required.");
        }
        if (request.EqualityMode != MessageEqualityMode.Byte &&
            request.EqualityMode != MessageEqualityMode.CanonicalXml &&
            request.EqualityMode != MessageEqualityMode.SemanticModel)
        {
            return CreateFailure(
                request,
                ChecksumStatus.UnsupportedMode,
                "checksum.mode_unsupported",
                "A byte, canonical XML, or semantic-model checksum mode is required.");
        }
        if (request.KeyPaths.Count > request.Limits.MaxCollectionCount)
        {
            return CreateFailure(
                request,
                ChecksumStatus.ResourceLimitReached,
                "checksum.key_path_limit",
                "The checksum key-path count exceeds the configured collection limit.");
        }
        int index = 0;
        while (index < request.KeyPaths.Count)
        {
            XmlPathData? path = request.KeyPaths[index];
            if (path == null ||
                (path.Value.Length == 0 && path.Segments.Count == 0))
            {
                return CreateFailure(
                    request,
                    ChecksumStatus.InvalidInput,
                    "checksum.key_path_invalid",
                    "Every semantic key path must be present and non-empty.");
            }
            index++;
        }
        return null;
    }

    internal static string NormalizeAlgorithm(string algorithm)
    {
        if (string.Equals(algorithm, "SHA-256", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(algorithm, "SHA256", StringComparison.OrdinalIgnoreCase))
        {
            return "SHA-256";
        }
        if (string.Equals(algorithm, "SHA-384", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(algorithm, "SHA384", StringComparison.OrdinalIgnoreCase))
        {
            return "SHA-384";
        }
        if (string.Equals(algorithm, "SHA-512", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(algorithm, "SHA512", StringComparison.OrdinalIgnoreCase))
        {
            return "SHA-512";
        }
        return string.Empty;
    }

    internal static ChecksumResult CalculateSemanticModelChecksum(
        ChecksumRequest request,
        string algorithm)
    {
        if (request.EqualityMode != MessageEqualityMode.SemanticModel)
        {
            return CreateFailure(
                request,
                ChecksumStatus.UnsupportedMode,
                "checksum.model_mode_unsupported",
                "A generated model can only be hashed in semantic-model mode.");
        }
        if (request.KeyPaths.Count > 0)
        {
            return CreateFailure(
                request,
                ChecksumStatus.UnsupportedMode,
                "checksum.model_key_paths_unsupported",
                "XML key paths cannot be evaluated against an opaque generated model.");
        }
        object? model = request.Model;
        if (model == null)
        {
            return CreateFailure(
                request,
                ChecksumStatus.InvalidInput,
                "checksum.model_missing",
                "A semantic model is required.");
        }
        JsonSerializerOptions options = new JsonSerializerOptions();
        options.IncludeFields = true;
        options.WriteIndented = false;
        byte[] representation = JsonSerializer.SerializeToUtf8Bytes(
            model,
            model.GetType(),
            options);
        if (representation.LongLength > request.Limits.MaxTotalBytes)
        {
            return CreateFailure(
                request,
                ChecksumStatus.ResourceLimitReached,
                "checksum.model_bytes_limit",
                "The semantic model representation exceeds the configured byte limit.");
        }
        ChecksumResult result = CreateResult(request, algorithm);
        CompleteResult(result, representation, algorithm, request.ExpectedChecksum);
        return result;
    }

    internal static DynamicXmlNode? CreateInputTree(ChecksumRequest request)
    {
        if (request.Xml.Length > 0)
        {
            MessageReadOptions options = CreateReadOptions(
                request.Limits,
                request.EqualityMode);
            return new GenericXmlNodeReader(options)
                .ReadDocumentFromBytes(request.Xml, null);
        }
        if (request.Document != null)
        {
            MessageWriteOptions options = CreatePreservingWriteOptions(
                request.Limits);
            return XmlMessageWriter.BuildDocumentTree(request.Document, options);
        }
        DynamicXmlNode? node = request.Model as DynamicXmlNode;
        if (node == null)
        {
            MessageDocument? document = request.Model as MessageDocument;
            if (document != null)
            {
                MessageWriteOptions options = CreatePreservingWriteOptions(
                    request.Limits);
                return XmlMessageWriter.BuildDocumentTree(document, options);
            }
            return null;
        }
        if (node.Kind == XmlNodeKind.Document)
        {
            return CanonicalXmlNodeNormalizer.CloneNode(node);
        }
        DynamicXmlNode tree = new DynamicXmlNode();
        tree.Kind = XmlNodeKind.Document;
        tree.Children.Add(CanonicalXmlNodeNormalizer.CloneNode(node));
        return tree;
    }

    internal static MessageReadOptions CreateReadOptions(
        ResourceLimits limits,
        MessageEqualityMode mode)
    {
        MessageReadOptions options = new MessageReadOptions();
        options.NamespaceMode = NamespaceMode.Strict;
        options.UnknownContentMode = UnknownContentMode.Preserve;
        options.Limits = limits;
        bool preserveXmlDetails = mode != MessageEqualityMode.SemanticModel;
        options.PreserveComments = preserveXmlDetails;
        options.PreserveProcessingInstructions = preserveXmlDetails;
        options.PreserveWhitespace = preserveXmlDetails;
        options.PreserveLexicalValues = preserveXmlDetails;
        return options;
    }

    internal static MessageWriteOptions CreatePreservingWriteOptions(
        ResourceLimits limits)
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

    internal static DynamicXmlNode? SelectScope(
        DynamicXmlNode tree,
        ChecksumScope scope)
    {
        if (scope == ChecksumScope.WholeMessage)
        {
            return tree;
        }
        List<DynamicXmlNode> bodies = new List<DynamicXmlNode>();
        CollectBodyElements(tree, bodies);
        return bodies.Count == 1 ? bodies[0] : null;
    }

    internal static void CollectBodyElements(
        DynamicXmlNode node,
        List<DynamicXmlNode> bodies)
    {
        if (node.Kind == XmlNodeKind.Element &&
            node.Name.LocalName == "Document")
        {
            bodies.Add(node);
            return;
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            CollectBodyElements(node.Children[index], bodies);
            index++;
        }
    }

    internal static byte[] CreateRepresentation(
        DynamicXmlNode scoped,
        ChecksumRequest request)
    {
        if (request.EqualityMode == MessageEqualityMode.Byte &&
            request.Scope == ChecksumScope.WholeMessage &&
            request.Xml.Length > 0)
        {
            byte[] bytes = new byte[request.Xml.Length];
            Array.Copy(request.Xml, bytes, bytes.Length);
            return bytes;
        }
        if (request.EqualityMode == MessageEqualityMode.Byte)
        {
            return new GenericXmlNodeWriter(
                CreatePreservingWriteOptions(request.Limits))
                .WriteBytes(scoped);
        }
        DynamicXmlNode prepared = request.EqualityMode ==
            MessageEqualityMode.SemanticModel ?
            CreateSemanticNode(scoped) :
            CanonicalXmlNodeNormalizer.CloneNode(scoped);
        DynamicXmlNode normalized = prepared.Kind == XmlNodeKind.Document ?
            CanonicalXmlNodeNormalizer.NormalizeDocument(prepared) :
            CanonicalXmlNodeNormalizer.NormalizeTopElement(prepared);
        MessageWriteOptions options = CreatePreservingWriteOptions(request.Limits);
        options.PreservePrefixes = false;
        options.PreserveLexicalValues = false;
        return new GenericXmlNodeWriter(options).WriteBytes(normalized);
    }

    internal static DynamicXmlNode CreateSemanticNode(DynamicXmlNode source)
    {
        DynamicXmlNode result = new DynamicXmlNode();
        result.Kind = source.Kind == XmlNodeKind.CData ?
            XmlNodeKind.Text : source.Kind;
        result.Name = XmlPathTracker.CopyName(source.Name);
        result.Value = source.Value;
        result.IsNil = source.IsNil;
        result.IsEmptyElement = false;
        result.Location = XmlPathTracker.CopyLocation(source.Location);
        result.Attributes = GenericXmlNodeWriter.CopyAttributes(source.Attributes);
        result.NamespaceDeclarations =
            GenericXmlNodeWriter.CopyDeclarations(source.NamespaceDeclarations);
        int index = 0;
        while (index < source.Children.Count)
        {
            DynamicXmlNode child = source.Children[index];
            bool ignored = child.Kind == XmlNodeKind.Comment ||
                child.Kind == XmlNodeKind.ProcessingInstruction ||
                ((child.Kind == XmlNodeKind.Text ||
                    child.Kind == XmlNodeKind.CData) &&
                    IsXmlWhitespaceOnly(child.Value));
            if (!ignored)
            {
                result.Children.Add(CreateSemanticNode(child));
            }
            index++;
        }
        return result;
    }

    internal static bool IsXmlWhitespaceOnly(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!BinaryLexical.IsXmlWhitespace(value[index]))
            {
                return false;
            }
            index++;
        }
        return value.Length > 0;
    }

    internal static int FindDuplicateKeys(
        DynamicXmlNode tree,
        ChecksumRequest request,
        ChecksumResult result)
    {
        int pathIndex = 0;
        bool duplicateFound = false;
        int occurrenceCount = 0;
        while (pathIndex < request.KeyPaths.Count)
        {
            List<string> values = new List<string>();
            List<XmlPathData> paths = new List<XmlPathData>();
            List<SourceLocation> locations = new List<SourceLocation>();
            XmlPathTracker tracker = new XmlPathTracker();
            CollectKeyValues(
                tree,
                request.KeyPaths[pathIndex],
                tracker,
                values,
                paths,
                locations);
            occurrenceCount = occurrenceCount + values.Count;
            if (occurrenceCount > request.Limits.MaxCollectionCount)
            {
                return 2;
            }
            List<string> reported = new List<string>();
            int valueIndex = 0;
            while (valueIndex < values.Count)
            {
                string value = values[valueIndex];
                if (CountValue(values, value) > 1 &&
                    !ContainsValue(reported, value))
                {
                    duplicateFound = true;
                    reported.Add(value);
                    result.DuplicateKeyPaths.Add(
                        SupplementaryDataValidator.CopyPath(paths[valueIndex]));
                    if (!request.RedactDuplicateKeys)
                    {
                        result.DuplicateKeys.Add(value);
                    }
                    else
                    {
                        result.DuplicateKeysRedacted = true;
                    }
                    AddDuplicateDiagnostic(
                        result,
                        paths[valueIndex],
                        locations[valueIndex],
                        value,
                        request);
                }
                valueIndex++;
            }
            pathIndex++;
        }
        return duplicateFound ? 1 : 0;
    }

    internal static void CollectKeyValues(
        DynamicXmlNode node,
        XmlPathData requested,
        XmlPathTracker tracker,
        List<string> values,
        List<XmlPathData> paths,
        List<SourceLocation> locations)
    {
        if (node.Kind == XmlNodeKind.Document)
        {
            int documentIndex = 0;
            while (documentIndex < node.Children.Count)
            {
                CollectKeyValues(
                    node.Children[documentIndex],
                    requested,
                    tracker,
                    values,
                    paths,
                    locations);
                documentIndex++;
            }
            return;
        }
        if (node.Kind != XmlNodeKind.Element)
        {
            return;
        }
        tracker.EnterElement(node.Name, node.Location);
        XmlPathData current = tracker.CapturePath();
        if (ChecksumPathMatches(requested, current))
        {
            StringBuilder value = new StringBuilder();
            AppendDescendantText(node, value);
            values.Add(value.ToString());
            paths.Add(current);
            locations.Add(XmlPathTracker.CopyLocation(node.Location));
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            CollectKeyValues(
                node.Children[index],
                requested,
                tracker,
                values,
                paths,
                locations);
            index++;
        }
        tracker.ExitElement();
    }

    internal static bool ChecksumPathMatches(
        XmlPathData requested,
        XmlPathData current)
    {
        if (requested.Segments.Count > 0)
        {
            if (requested.Segments.Count != current.Segments.Count)
            {
                return false;
            }
            int index = 0;
            while (index < requested.Segments.Count)
            {
                QualifiedNameData expected = requested.Segments[index];
                QualifiedNameData actual = current.Segments[index];
                if (expected.LocalName != actual.LocalName ||
                    expected.NamespaceUri != actual.NamespaceUri)
                {
                    return false;
                }
                index++;
            }
            return true;
        }
        return requested.Value == current.Value ||
            requested.Value == CreateLocalPath(current);
    }

    internal static string CreateLocalPath(XmlPathData path)
    {
        StringBuilder value = new StringBuilder();
        int index = 0;
        while (index < path.Segments.Count)
        {
            value.Append('/');
            value.Append(path.Segments[index].LocalName);
            index++;
        }
        return value.ToString();
    }

    internal static void AppendDescendantText(
        DynamicXmlNode node,
        StringBuilder value)
    {
        if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
        {
            value.Append(node.Value);
            return;
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            AppendDescendantText(node.Children[index], value);
            index++;
        }
    }

    internal static int CountValue(List<string> values, string expected)
    {
        int count = 0;
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == expected)
            {
                count++;
            }
            index++;
        }
        return count;
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

    internal static void AddDuplicateDiagnostic(
        ChecksumResult result,
        XmlPathData path,
        SourceLocation location,
        string value,
        ChecksumRequest request)
    {
        if (result.Diagnostics.Count >= request.Limits.MaxDiagnosticCount)
        {
            return;
        }
        ValidationDiagnostic diagnostic = new ValidationDiagnostic();
        diagnostic.Code = "checksum.duplicate_key";
        diagnostic.RuleIdentifier = "integrity.semantic_key.unique";
        diagnostic.Severity = request.RejectDuplicateKeys ?
            ValidationSeverity.Error : ValidationSeverity.Warning;
        diagnostic.Stage = ValidationStage.Semantic;
        diagnostic.Kind = DiagnosticKind.Conflict;
        diagnostic.Message = "A semantic key value occurs more than once.";
        diagnostic.Path = SupplementaryDataValidator.CopyPath(path);
        diagnostic.Location = XmlPathTracker.CopyLocation(location);
        diagnostic.OffendingValueRedacted = request.RedactDuplicateKeys;
        diagnostic.OffendingValue = request.RedactDuplicateKeys ?
            string.Empty : value;
        diagnostic.ExpectedConstraint =
            "Each selected semantic key value must be unique.";
        result.Diagnostics.Add(diagnostic);
    }

    internal static void CompleteResult(
        ChecksumResult result,
        byte[] representation,
        string algorithm,
        byte[] expected)
    {
        byte[] hash = CalculateHash(representation, algorithm);
        result.Checksum.Value = hash;
        result.Checksum.Encoding = BinaryEncodingKind.Hexadecimal;
        result.Checksum.LexicalValue = BinaryLexical.FormatHexadecimal(hash);
        result.Success = true;
        result.HasExpectedChecksum = expected.Length > 0;
        if (!result.HasExpectedChecksum)
        {
            result.Status = ChecksumStatus.Calculated;
            return;
        }
        result.MatchesExpected = expected.Length == hash.Length &&
            CryptographicOperations.FixedTimeEquals(expected, hash);
        result.Status = result.MatchesExpected ?
            ChecksumStatus.Matched : ChecksumStatus.Mismatched;
    }

    internal static byte[] CalculateHash(byte[] value, string algorithm)
    {
        if (algorithm == "SHA-384")
        {
            return SHA384.HashData(value);
        }
        if (algorithm == "SHA-512")
        {
            return SHA512.HashData(value);
        }
        return SHA256.HashData(value);
    }

    internal static ChecksumResult CreateResult(
        ChecksumRequest request,
        string algorithm)
    {
        ChecksumResult result = new ChecksumResult();
        result.Scope = request.Scope;
        result.EqualityMode = request.EqualityMode;
        result.Algorithm = algorithm;
        return result;
    }

    internal static ChecksumResult CreateFailure(
        ChecksumRequest request,
        ChecksumStatus status,
        string code,
        string message)
    {
        ChecksumResult result = CreateResult(request, request.Algorithm);
        result.Status = status;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        result.HasExpectedChecksum = request.ExpectedChecksum != null &&
            request.ExpectedChecksum.Length > 0;
        return result;
    }
}
