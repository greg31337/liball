using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace iso20022;

/// <summary>Validates raw XML and generic message documents against exact registered XSD closures.</summary>
public class XsdValidator : IXsdValidator
{
    internal ISchemaRegistry Registry;

    /// <summary>Initializes an XSD validator using the supplied local schema registry.</summary>
    /// <param name="registry">The registry used for exact schema selection and compilation.</param>
    /// <exception cref="ArgumentNullException">Thrown when the registry is missing.</exception>
    public XsdValidator(ISchemaRegistry registry)
    {
        if (registry == null)
        {
            throw new ArgumentNullException("registry");
        }

        Registry = registry;
    }

    /// <summary>Validates a raw XML stream against one exact registered schema key.</summary>
    /// <param name="input">The raw XML stream.</param><param name="selector">The exact schema registry key.</param><param name="options">The registry, compilation, validation, and resource policy.</param><returns>The validation result with stable diagnostics and schema provenance.</returns>
    public SchemaCompilationResult ValidateXmlStream(
        Stream? input,
        SchemaArtifact? selector,
        SchemaRegistryOptions? options)
    {
        if (input == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.validation.input_missing",
                "A raw XML input stream is required for XSD validation.");
        }

        SchemaCompilationResult compiled = Registry.CompileSchema(selector, options);
        if (!compiled.Success || !compiled.Found || compiled.CompiledSchemas == null ||
            options == null)
        {
            return compiled;
        }

        BoundedArtifactCopyResult copied = SchemaImporter.CopySchemaInput(
            input,
            options.Limits.MaxTotalBytes,
            options);
        if (!copied.Success)
        {
            SetValidationFailure(
                compiled,
                "schema.validation.input_limit",
                "The raw XML input exceeds the configured total-byte limit.");
            return compiled;
        }

        XsdDocumentLocationMap locations = new XsdDocumentLocationMap();
        return ValidateBytes(copied.Bytes, null, locations, compiled, options);
    }

    /// <summary>Validates a generic message document against one exact registered schema key.</summary>
    /// <param name="document">The generic message document.</param><param name="selector">The exact schema registry key.</param><param name="options">The registry, compilation, validation, and resource policy.</param><returns>The validation result with stable diagnostics and schema provenance.</returns>
    public SchemaCompilationResult ValidateMessageDocument(
        MessageDocument? document,
        SchemaArtifact? selector,
        SchemaRegistryOptions? options)
    {
        if (document == null)
        {
            return SchemaImporter.CreateSchemaFailure(
                "schema.validation.document_missing",
                "A generic message document is required for XSD validation.");
        }

        SchemaCompilationResult compiled = Registry.CompileSchema(selector, options);
        if (!compiled.Success || !compiled.Found || compiled.CompiledSchemas == null ||
            options == null)
        {
            return compiled;
        }

        MessageWriteOptions writeOptions = CreateWriteOptions(options);
        DynamicXmlNode? tree = XmlMessageWriter.PrepareTree(document, writeOptions);
        if (tree == null)
        {
            SetValidationFailure(
                compiled,
                "schema.validation.document_content_missing",
                "The generic message document does not contain XML content.");
            return compiled;
        }

        XsdDocumentLocationMap locations = new XsdDocumentLocationMap();
        XmlPathTracker paths = new XmlPathTracker();
        CollectDocumentLocations(tree, paths, locations);
        byte[] bytes = new GenericXmlNodeWriter(writeOptions).WriteBytes(tree);
        if (bytes.LongLength > options.Limits.MaxTotalBytes)
        {
            SetValidationFailure(
                compiled,
                "schema.validation.input_limit",
                "The serialized message document exceeds the configured total-byte limit.");
            return compiled;
        }

        return ValidateBytes(
            bytes,
            document.Location.SourceName,
            locations,
            compiled,
            options);
    }

    internal static SchemaCompilationResult ValidateBytes(
        byte[] bytes,
        string? sourceName,
        XsdDocumentLocationMap locations,
        SchemaCompilationResult compiled,
        SchemaRegistryOptions options)
    {
        SchemaArtifact? selected = compiled.RootSchema;
        if (selected == null || compiled.CompiledSchemas == null)
        {
            SetValidationFailure(
                compiled,
                "schema.validation.schema_missing",
                "XSD validation requires one selected compiled schema closure.");
            return compiled;
        }

        EnsureDiagnosticMetadata(compiled, selected.BaseUri);
        XsdValidationState state = new XsdValidationState();
        state.Result = compiled;
        state.Options = options;
        state.Schema = selected;
        state.DocumentLocations = locations;
        state.SourceName = sourceName;
        XmlReaderSettings settings = CreateValidationSettings(
            compiled.CompiledSchemas,
            bytes.LongLength,
            state);
        using (MemoryStream input = new MemoryStream(bytes, false))
        using (XmlReader reader = XmlReader.Create(input, settings))
        {
            state.Reader = reader;
            bool hasNode = reader.Read();
            while (state.Continue && hasNode)
            {
                RecordReaderNode(state);
                if (state.Continue)
                {
                    hasNode = reader.Read();
                }
            }
        }

        if (state.HasValidationError && compiled.ErrorCode.Length == 0)
        {
            SetValidationFailure(
                compiled,
                "schema.validation.invalid",
                "The XML content does not satisfy the selected XSD closure.");
        }

        return compiled;
    }

    internal static XmlReaderSettings CreateValidationSettings(
        XmlSchemaSet schemas,
        long maximumCharacters,
        XsdValidationState state)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.CheckCharacters = true;
        settings.CloseInput = true;
        settings.ConformanceLevel = ConformanceLevel.Auto;
        settings.IgnoreComments = true;
        settings.IgnoreProcessingInstructions = true;
        settings.IgnoreWhitespace = false;
        settings.MaxCharactersFromEntities = 0;
        settings.MaxCharactersInDocument = maximumCharacters;
        settings.Schemas = schemas;
        settings.ValidationType = ValidationType.Schema;
        settings.ValidationFlags =
            XmlSchemaValidationFlags.ProcessIdentityConstraints |
            XmlSchemaValidationFlags.ReportValidationWarnings;
        settings.ValidationEventHandler += (sender, arguments) =>
        {
            RecordValidationDiagnostic(state, arguments);
        };
        return settings;
    }

    internal static void RecordReaderNode(XsdValidationState state)
    {
        XmlReader? reader = state.Reader;
        if (reader == null)
        {
            return;
        }

        XmlNodeType nodeType = reader.NodeType;
        if (nodeType == XmlNodeType.Element)
        {
            state.ElementCount++;
            if (reader.Depth + 1 > state.Options.Limits.MaxDepth ||
                reader.AttributeCount > state.Options.Limits.MaxAttributesPerElement ||
                state.ElementCount > state.Options.Limits.MaxElementCount)
            {
                FailResourceLimit(state);
                return;
            }

            EnsureChildDepth(state, reader.Depth);
            if (reader.Depth > 0)
            {
                int parentDepth = reader.Depth - 1;
                state.ChildElementCounts[parentDepth]++;
                if (state.ChildElementCounts[parentDepth] >
                    state.Options.Limits.MaxCollectionCount)
                {
                    FailResourceLimit(state);
                    return;
                }
            }

            state.ChildElementCounts[reader.Depth] = 0;
            int attributeIndex = 0;
            while (attributeIndex < reader.AttributeCount)
            {
                string value = reader.GetAttribute(attributeIndex);
                if (value.Length > state.Options.Limits.MaxTextLength)
                {
                    FailResourceLimit(state);
                    return;
                }

                attributeIndex++;
            }

            state.Paths.EnterElement(
                CreateReaderName(reader),
                CreateReaderLocation(reader, state.SourceName));
            if (reader.IsEmptyElement)
            {
                state.Paths.ExitElement();
            }
        }
        else if (nodeType == XmlNodeType.EndElement)
        {
            if (state.Paths.GetPathDepth() > 0)
            {
                state.Paths.ExitElement();
            }
        }
        else if ((nodeType == XmlNodeType.Text ||
            nodeType == XmlNodeType.CDATA ||
            (nodeType >= XmlNodeType.Whitespace &&
            nodeType <= XmlNodeType.SignificantWhitespace)) &&
            reader.Value.Length > state.Options.Limits.MaxTextLength)
        {
            FailResourceLimit(state);
        }
    }

    internal static void RecordValidationDiagnostic(
        XsdValidationState state,
        ValidationEventArgs arguments)
    {
        if (state.Result.DiagnosticCodes.Count >=
            state.Options.Limits.MaxDiagnosticCount)
        {
            state.Continue = false;
            state.HasValidationError = true;
            SetValidationFailure(
                state.Result,
                "schema.validation.diagnostic_limit",
                "XSD validation exceeded the configured diagnostic limit.");
            return;
        }

        string code = "schema.validation.warning";
        if (arguments.Severity == XmlSeverityType.Error)
        {
            code = "schema.validation.error";
            state.HasValidationError = true;
        }

        XmlPathData path = CaptureDiagnosticPath(state);
        SourceLocation location = CreateDiagnosticLocation(state, arguments, path);
        state.Result.DiagnosticCodes.Add(code);
        state.Result.DiagnosticMessages.Add(arguments.Message);
        state.Result.DiagnosticLocations.Add(location);
        state.Result.DiagnosticPaths.Add(path);
        state.Result.DiagnosticSchemaUris.Add(state.Schema.BaseUri);
    }

    internal static XmlPathData CaptureDiagnosticPath(XsdValidationState state)
    {
        XmlReader? reader = state.Reader;
        bool appended = false;
        if (reader != null && reader.NodeType == XmlNodeType.Element &&
            state.Paths.GetPathDepth() == reader.Depth)
        {
            state.Paths.EnterElement(
                CreateReaderName(reader),
                CreateReaderLocation(reader, state.SourceName));
            appended = true;
        }

        XmlPathData path = state.Paths.CapturePath();
        if (appended)
        {
            state.Paths.ExitElement();
        }

        return path;
    }

    internal static SourceLocation CreateDiagnosticLocation(
        XsdValidationState state,
        ValidationEventArgs arguments,
        XmlPathData path)
    {
        SourceLocation? documentLocation = FindDocumentLocation(
            state.DocumentLocations,
            path.Value);
        if (documentLocation != null)
        {
            return XmlPathTracker.CopyLocation(documentLocation);
        }

        SourceLocation location = new SourceLocation();
        location.SourceName = state.SourceName;
        if (arguments.Exception.LineNumber > 0)
        {
            location.HasLineInformation = true;
            location.LineNumber = arguments.Exception.LineNumber;
            location.ColumnNumber = arguments.Exception.LinePosition;
        }

        return location;
    }

    internal static SourceLocation? FindDocumentLocation(
        XsdDocumentLocationMap locations,
        string path)
    {
        int index = 0;
        while (index < locations.Paths.Count)
        {
            if (locations.Paths[index] == path)
            {
                return locations.Locations[index];
            }

            index++;
        }

        return null;
    }

    internal static void CollectDocumentLocations(
        DynamicXmlNode node,
        XmlPathTracker paths,
        XsdDocumentLocationMap locations)
    {
        bool entered = false;
        if (node.Kind == XmlNodeKind.Element)
        {
            paths.EnterElement(node.Name, node.Location);
            XmlPathData path = paths.CapturePath();
            locations.Paths.Add(path.Value);
            locations.Locations.Add(XmlPathTracker.CopyLocation(node.Location));
            entered = true;
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            CollectDocumentLocations(node.Children[index], paths, locations);
            index++;
        }

        if (entered)
        {
            paths.ExitElement();
        }
    }

    internal static QualifiedNameData CreateReaderName(XmlReader reader)
    {
        QualifiedNameData name = new QualifiedNameData();
        name.NamespaceUri = reader.NamespaceURI;
        name.LocalName = reader.LocalName;
        name.Prefix = reader.Prefix;
        return name;
    }

    internal static SourceLocation CreateReaderLocation(
        XmlReader reader,
        string? sourceName)
    {
        SourceLocation location = new SourceLocation();
        location.SourceName = sourceName;
        IXmlLineInfo line = (IXmlLineInfo)reader;
        if (line.HasLineInfo())
        {
            location.HasLineInformation = true;
            location.LineNumber = line.LineNumber;
            location.ColumnNumber = line.LinePosition;
        }

        return location;
    }

    internal static void EnsureChildDepth(XsdValidationState state, int depth)
    {
        while (state.ChildElementCounts.Count <= depth)
        {
            state.ChildElementCounts.Add(0);
        }

        int removeIndex = state.ChildElementCounts.Count - 1;
        while (removeIndex > depth)
        {
            state.ChildElementCounts.RemoveAt(removeIndex);
            removeIndex--;
        }
    }

    internal static void FailResourceLimit(XsdValidationState state)
    {
        state.Continue = false;
        state.HasValidationError = true;
        SetValidationFailure(
            state.Result,
            "schema.validation.resource_limit",
            "The XML content exceeds a configured validation resource limit.");
    }

    internal static void EnsureDiagnosticMetadata(
        SchemaCompilationResult result,
        string schemaUri)
    {
        while (result.DiagnosticPaths.Count < result.DiagnosticCodes.Count)
        {
            result.DiagnosticPaths.Add(new XmlPathData());
        }

        while (result.DiagnosticSchemaUris.Count < result.DiagnosticCodes.Count)
        {
            result.DiagnosticSchemaUris.Add(schemaUri);
        }
    }

    internal static MessageWriteOptions CreateWriteOptions(
        SchemaRegistryOptions options)
    {
        MessageWriteOptions writeOptions = new MessageWriteOptions();
        writeOptions.FormattingMode = XmlFormattingMode.Compact;
        writeOptions.UnknownContentMode = UnknownContentMode.Preserve;
        writeOptions.Limits = options.Limits;
        writeOptions.WriteXmlDeclaration = false;
        writeOptions.WriteByteOrderMark = false;
        writeOptions.PreservePrefixes = true;
        writeOptions.PreserveLexicalValues = true;
        return writeOptions;
    }

    internal static void SetValidationFailure(
        SchemaCompilationResult result,
        string errorCode,
        string errorMessage)
    {
        result.Success = false;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
    }
}
