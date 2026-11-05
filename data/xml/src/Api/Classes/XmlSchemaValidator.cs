namespace xml;

/// <summary>Provides the public XML Schema validator implementation.</summary>
public class XmlSchemaValidator : IXmlSchemaValidator
{
    /// <summary>Creates an XML Schema validator.</summary>
    public XmlSchemaValidator()
    {
    }

    /// <summary>Validates an XML document with a schema set.</summary>
    public XmlSchemaValidationResult ValidateXmlSchema(XmlDocument document, XmlSchemaSet schemaSet, XmlSchemaValidationOptions options)
    {
        Guard.NotNull(document, "document");
        Guard.NotNull(schemaSet, "schemaSet");
        Guard.NotNull(options, "options");
        CompiledXmlSchemaSet compiled = RequireCompiledXmlSchemaValidationSet(schemaSet);
        return AssessPublicXmlSchemaDocument(document, schemaSet, compiled, options);
    }

    /// <summary>Validates XML events with a schema set.</summary>
    public XmlSchemaValidationResult ValidateXmlSchemaEvents(XmlEventBatch events, XmlSchemaSet schemaSet, XmlSchemaValidationOptions options)
    {
        Guard.NotNull(events, "events");
        Guard.NotNull(schemaSet, "schemaSet");
        Guard.NotNull(options, "options");
        CompiledXmlSchemaSet compiled = RequireCompiledXmlSchemaValidationSet(schemaSet);
        XmlEvent[] source = events.Events == null ? [] : events.Events;
        if (events.Offset < 0 || events.Count < 0 || events.Offset > source.Length - events.Count)
        {
            throw new System.ArgumentOutOfRangeException("events");
        }

        XmlEvent[] selected = new XmlEvent[events.Count];
        System.Array.Copy(source, events.Offset, selected, 0, events.Count);
        XmlDocument metadata = CreateXmlSchemaEventMetadata(events, selected);
        XmlDocument document = new TreeBuilder(metadata, false).BuildXmlEventTree(selected);
        return AssessPublicXmlSchemaDocument(document, schemaSet, compiled, options);
    }

    internal CompiledXmlSchemaSet RequireCompiledXmlSchemaValidationSet(XmlSchemaSet schemaSet)
    {
        CompiledXmlSchemaSet? compiled = schemaSet.CompiledSchemaSet;
        object? handle = compiled == null ? schemaSet.CompiledSet : compiled;
        if (compiled == null && (!schemaSet.IsImmutable || handle == null))
        {
            throw new System.ArgumentException("Schema validation requires an immutable compiled schema set.", "schemaSet");
        }

        if (handle == null)
        {
            throw new System.ArgumentException("Schema validation requires an immutable compiled schema set.", "schemaSet");
        }

        return (CompiledXmlSchemaSet)handle;
    }

    internal XmlSchemaValidationResult AssessPublicXmlSchemaDocument(XmlDocument document, XmlSchemaSet schemaSet, CompiledXmlSchemaSet compiled, XmlSchemaValidationOptions options)
    {
        XmlSchemaAssessmentEngine engine = new XmlSchemaAssessmentEngine(compiled, options, new XmlDefaultResolver());
        XmlSchemaAssessmentResult assessment = engine.AssessXmlSchemaDocument(document);
        XmlSchemaValidationResult result = new XmlSchemaValidationResult();
        result.Document = document;
        result.SchemaSet = schemaSet;
        result.Options = options;
        result.Validity = assessment.Validity;
        result.TypeAnnotations = assessment.TypeAnnotations;
        result.Diagnostics = assessment.Diagnostics;
        result.Succeeded = assessment.Succeeded;
        return result;
    }

    internal XmlDocument CreateXmlSchemaEventMetadata(XmlEventBatch events, XmlEvent[] selected)
    {
        XmlDocument metadata = new XmlDocument();
        XmlReaderState? state = events.ReaderState;
        if (state != null)
        {
            metadata.BaseUri = state.BaseUri;
            metadata.DocumentUri = state.BaseUri;
            XmlParserOptions? parserOptions = state.Options;
            if (parserOptions != null)
            {
                metadata.Version = parserOptions.Version;
            }
        }

        if (metadata.BaseUri == null)
        {
            int index = 0;
            while (index < selected.Length && metadata.BaseUri == null)
            {
                SourceRange? range = selected[index].Range;
                SourceLocation? location = range == null ? null : range.Start;
                if (location != null)
                {
                    metadata.BaseUri = location.SourceUri;
                    metadata.DocumentUri = location.SourceUri;
                }

                index = index + 1;
            }
        }

        return metadata;
    }
}
