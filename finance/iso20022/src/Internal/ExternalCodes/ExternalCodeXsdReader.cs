using System;
using System.Text;
using System.Xml;

namespace iso20022;

internal static class ExternalCodeXsdReader
{
    internal const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";
    internal const string ExternalCodeNamespace = "urn:iso:std:iso:20022:tech:xsd:externalcodeset";

    internal static ExternalCodeCatalog? ReadCatalog(
        byte[] content,
        ExternalCodeImportRequest request,
        ExternalCodeArtifactOpenResult opened,
        ExternalCodeImportResult result)
    {
        ExternalCodeXsdReadState state = new ExternalCodeXsdReadState();
        state.Request = request;
        state.Result = result;
        state.Catalog.Artifact = opened.Artifact;
        state.Catalog.Edition = ExternalCodeSetImporter.ResolveEdition(request, opened.EntryName);
        state.Catalog.PublicationDate = opened.Artifact.PublicationDate;
        state.Catalog.SourceFormat = "XSD";
        state.Catalog.SchemaVersion = request.Artifact.Coordinate.Version;
        if (state.Catalog.Edition.Length == 0)
        {
            FailRead(
                state,
                "external-code.edition_missing",
                "The external-code edition is not declared and could not be inferred from the XSD entry name.");
            return null;
        }

        MessageReadOptions options = new MessageReadOptions();
        options.Limits = request.Limits;
        options.PreserveWhitespace = false;
        using (BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromBytes(content, options))
        {
            while (state.Success && reader.ReadNext())
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                XmlNodeType nodeType = reader.GetNodeType();
                if (nodeType == XmlNodeType.Element)
                {
                    ReadStartElement(reader, state);
                    if (state.Success && reader.GetIsEmptyElement())
                    {
                        ReadEndElement(reader.GetLocalName(), reader.GetNamespaceUri(), reader.GetDepth(), state);
                    }
                }
                else if (nodeType == XmlNodeType.Text || nodeType == XmlNodeType.CDATA)
                {
                    ReadTextValue(reader.GetValue(), state);
                }
                else if (nodeType == XmlNodeType.EndElement)
                {
                    ReadEndElement(reader.GetLocalName(), reader.GetNamespaceUri(), reader.GetDepth(), state);
                }
            }
        }

        if (!state.Success)
        {
            return null;
        }

        if (!state.SawSchema || state.CurrentCodeSet != null || state.CurrentCodeValue != null ||
            state.Catalog.CodeSets.Count == 0)
        {
            FailRead(
                state,
                "external-code.xsd.structure",
                "The XSD does not contain a complete external-code schema.");
            return null;
        }

        ExternalCodeSetImporter.AddDiagnostic(
            result,
            request,
            "external-code.xsd.validity_unavailable",
            "The official XSD does not declare activation, deactivation, or removal dates; those normalized fields remain absent.");
        return state.Catalog;
    }

    internal static void ReadStartElement(
        BoundedXmlReader reader,
        ExternalCodeXsdReadState state)
    {
        string localName = reader.GetLocalName();
        string namespaceUri = reader.GetNamespaceUri();
        int depth = reader.GetDepth();
        if (depth == 0)
        {
            ReadSchemaElement(reader, state);
            return;
        }

        if (namespaceUri == XmlSchemaNamespace && localName == "simpleType")
        {
            ReadSimpleTypeElement(reader, state);
            return;
        }

        if (namespaceUri == XmlSchemaNamespace && localName == "restriction")
        {
            ReadRestrictionElement(reader, state);
            return;
        }

        if (namespaceUri == XmlSchemaNamespace && localName == "minLength")
        {
            ReadMinimumLengthElement(reader, state);
            return;
        }

        if (namespaceUri == XmlSchemaNamespace && localName == "maxLength")
        {
            ReadMaximumLengthElement(reader, state);
            return;
        }

        if (namespaceUri == XmlSchemaNamespace && localName == "pattern")
        {
            ReadPatternElement(reader, state);
            return;
        }

        if (namespaceUri == XmlSchemaNamespace && localName == "enumeration")
        {
            ReadEnumerationElement(reader, state);
            return;
        }

        if (namespaceUri == XmlSchemaNamespace && localName == "documentation")
        {
            ReadDocumentationElement(reader, state);
            return;
        }

        if (namespaceUri == ExternalCodeNamespace)
        {
            ReadMetadataElement(localName, depth, state);
        }
    }

    internal static void ReadSchemaElement(
        BoundedXmlReader reader,
        ExternalCodeXsdReadState state)
    {
        if (state.SawSchema || reader.GetLocalName() != "schema" ||
            reader.GetNamespaceUri() != XmlSchemaNamespace)
        {
            FailRead(
                state,
                "external-code.xsd.root",
                "The external-code XSD root must be an XML Schema schema element.");
            return;
        }

        string targetNamespace = ReadAttributeValue(reader, "targetNamespace");
        if (targetNamespace != ExternalCodeNamespace)
        {
            FailRead(
                state,
                "external-code.xsd.namespace",
                "The external-code XSD target namespace is not the official external-code namespace.");
            return;
        }

        state.SawSchema = true;
        state.Catalog.SchemaIdentifier = targetNamespace;
        string schemaVersion = ReadAttributeValue(reader, "version");
        if (schemaVersion.Length > 0)
        {
            state.Catalog.SchemaVersion = schemaVersion;
        }
    }

    internal static void ReadSimpleTypeElement(
        BoundedXmlReader reader,
        ExternalCodeXsdReadState state)
    {
        if (!state.SawSchema || state.CurrentCodeSet != null)
        {
            FailRead(
                state,
                "external-code.xsd.simple_type",
                "External-code XSD simple types must be top-level and non-nested.");
            return;
        }

        string identifier = ReadAttributeValue(reader, "name");
        if (identifier.Length == 0 || identifier.Length > state.Request.Limits.MaxTextLength ||
            !state.CodeSetIdentifiers.Add(identifier))
        {
            FailRead(
                state,
                "external-code.xsd.definition_identifier",
                "An XSD code-set identifier is empty, duplicated, or too long.");
            return;
        }

        if (state.CodeSetIdentifiers.Count > state.Request.Limits.MaxSchemaCount)
        {
            FailRead(
                state,
                "external-code.xsd.schema_limit",
                "The XSD exceeds the configured code-set limit.");
            return;
        }

        state.CurrentCodeSet = new ExternalCodeSet();
        state.CurrentCodeSet.Identifier = identifier;
    }

    internal static void ReadRestrictionElement(
        BoundedXmlReader reader,
        ExternalCodeXsdReadState state)
    {
        if (state.CurrentCodeSet == null || state.CurrentCodeValue != null)
        {
            return;
        }

        string baseType = ReadAttributeValue(reader, "base");
        if (baseType != "xs:string" && baseType != "xsd:string" && baseType != "string")
        {
            FailRead(
                state,
                "external-code.xsd.restriction",
                "Each external-code XSD simple type must restrict the XML Schema string type.");
        }
    }

    internal static void ReadMinimumLengthElement(
        BoundedXmlReader reader,
        ExternalCodeXsdReadState state)
    {
        if (state.CurrentCodeSet == null || state.CurrentCodeValue != null)
        {
            return;
        }

        IntegerLexicalResult parsed = IntegerLexical.ParseInt32(
            ReadAttributeValue(reader, "value").AsSpan());
        if (!parsed.Success || parsed.Value < 0)
        {
            FailRead(
                state,
                "external-code.xsd.minimum_length",
                "An XSD minimum-length facet is not a non-negative integer.");
            return;
        }

        state.CurrentCodeSet.HasMinimumLength = true;
        state.CurrentCodeSet.MinimumLength = parsed.Value;
    }

    internal static void ReadMaximumLengthElement(
        BoundedXmlReader reader,
        ExternalCodeXsdReadState state)
    {
        if (state.CurrentCodeSet == null || state.CurrentCodeValue != null)
        {
            return;
        }

        IntegerLexicalResult parsed = IntegerLexical.ParseInt32(
            ReadAttributeValue(reader, "value").AsSpan());
        if (!parsed.Success || parsed.Value < 0)
        {
            FailRead(
                state,
                "external-code.xsd.maximum_length",
                "An XSD maximum-length facet is not a non-negative integer.");
            return;
        }

        state.CurrentCodeSet.HasMaximumLength = true;
        state.CurrentCodeSet.MaximumLength = parsed.Value;
    }

    internal static void ReadPatternElement(
        BoundedXmlReader reader,
        ExternalCodeXsdReadState state)
    {
        if (state.CurrentCodeSet != null && state.CurrentCodeValue == null)
        {
            state.CurrentCodeSet.Pattern = ReadAttributeValue(reader, "value");
        }
    }

    internal static void ReadEnumerationElement(
        BoundedXmlReader reader,
        ExternalCodeXsdReadState state)
    {
        if (state.CurrentCodeSet == null || state.CurrentCodeValue != null)
        {
            FailRead(
                state,
                "external-code.xsd.enumeration",
                "An XSD code enumeration occurs outside a code-set restriction or is nested.");
            return;
        }

        string value = ReadAttributeValue(reader, "value");
        if (value.Length == 0 || value.Length > state.Request.Limits.MaxTextLength)
        {
            FailRead(
                state,
                "external-code.xsd.code_value",
                "The XSD code value '" + value + "' is empty or too long within code set '" +
                state.CurrentCodeSet.Identifier + "'.");
            return;
        }

        if (state.CurrentCodeSet.Values.Count >= state.Request.Limits.MaxCollectionCount)
        {
            FailRead(
                state,
                "external-code.xsd.code_limit",
                "An XSD code set exceeds the configured collection limit.");
            return;
        }

        state.CurrentCodeValue = new ExternalCodeValue();
        state.CurrentCodeValue.Value = value;
    }

    internal static void ReadDocumentationElement(
        BoundedXmlReader reader,
        ExternalCodeXsdReadState state)
    {
        string source = ReadAttributeValue(reader, "source");
        if (source == "Name")
        {
            BeginCapture(ExternalCodeXsdCaptureKind.Name, reader.GetDepth(), state);
        }
        else if (source == "Definition")
        {
            BeginCapture(ExternalCodeXsdCaptureKind.Definition, reader.GetDepth(), state);
        }
    }

    internal static void ReadMetadataElement(
        string localName,
        int depth,
        ExternalCodeXsdReadState state)
    {
        if (localName == "RegistrationStatus")
        {
            BeginCapture(ExternalCodeXsdCaptureKind.RegistrationStatus, depth, state);
        }
        else if (state.CurrentCodeValue != null && localName == "Requestor")
        {
            BeginCapture(ExternalCodeXsdCaptureKind.Requester, depth, state);
        }
        else if (state.CurrentCodeValue != null && localName == "LastUpdatedDate")
        {
            BeginCapture(ExternalCodeXsdCaptureKind.LastUpdatedDate, depth, state);
        }
        else if (state.CurrentCodeValue != null && localName == "CreationDate")
        {
            BeginCapture(ExternalCodeXsdCaptureKind.CreationDate, depth, state);
        }
        else if (state.CurrentCodeValue != null && localName == "ReplacedBy")
        {
            BeginCapture(ExternalCodeXsdCaptureKind.ReplacedBy, depth, state);
        }
        else if (state.CurrentCodeValue != null && localName == "AdditionalInformation")
        {
            BeginCapture(ExternalCodeXsdCaptureKind.AdditionalInformation, depth, state);
        }
    }

    internal static void BeginCapture(
        ExternalCodeXsdCaptureKind kind,
        int depth,
        ExternalCodeXsdReadState state)
    {
        if (state.CaptureKind != ExternalCodeXsdCaptureKind.None)
        {
            FailRead(
                state,
                "external-code.xsd.metadata",
                "The external-code XSD contains nested metadata values.");
            return;
        }

        state.CaptureKind = kind;
        state.CaptureDepth = depth;
        state.CaptureText.Clear();
    }

    internal static void ReadTextValue(string text, ExternalCodeXsdReadState state)
    {
        if (state.CaptureKind == ExternalCodeXsdCaptureKind.None)
        {
            return;
        }

        if (state.CaptureText.Length > state.Request.Limits.MaxTextLength - text.Length)
        {
            FailRead(
                state,
                "external-code.xsd.text_limit",
                "An external-code XSD metadata value exceeds the configured text limit.");
            return;
        }

        state.CaptureText.Append(text);
    }

    internal static void ReadEndElement(
        string localName,
        string namespaceUri,
        int depth,
        ExternalCodeXsdReadState state)
    {
        if (state.CaptureKind != ExternalCodeXsdCaptureKind.None && depth == state.CaptureDepth)
        {
            ApplyCapturedValue(state);
        }

        if (namespaceUri == XmlSchemaNamespace && localName == "enumeration")
        {
            if (state.CurrentCodeSet == null || state.CurrentCodeValue == null)
            {
                FailRead(
                    state,
                    "external-code.xsd.enumeration",
                    "An XSD code enumeration is incomplete.");
                return;
            }

            state.CurrentCodeSet.Values.Add(state.CurrentCodeValue);
            state.CurrentCodeValue = null;
        }
        else if (namespaceUri == XmlSchemaNamespace && localName == "simpleType")
        {
            if (state.CurrentCodeSet == null || state.CurrentCodeValue != null)
            {
                FailRead(
                    state,
                    "external-code.xsd.simple_type",
                    "An XSD code-set definition is incomplete.");
                return;
            }

            if (state.CurrentCodeSet.HasMinimumLength && state.CurrentCodeSet.HasMaximumLength &&
                state.CurrentCodeSet.MinimumLength > state.CurrentCodeSet.MaximumLength)
            {
                FailRead(
                    state,
                    "external-code.xsd.facets",
                    "An XSD code-set definition has inconsistent length facets.");
                return;
            }

            state.Catalog.CodeSets.Add(state.CurrentCodeSet);
            state.CurrentCodeSet = null;
        }
    }

    internal static void ApplyCapturedValue(ExternalCodeXsdReadState state)
    {
        string value = state.CaptureText.ToString().Trim();
        ExternalCodeXsdCaptureKind kind = state.CaptureKind;
        state.CaptureKind = ExternalCodeXsdCaptureKind.None;
        state.CaptureDepth = -1;
        state.CaptureText.Clear();
        if (state.CurrentCodeValue != null)
        {
            ApplyCodeValueMetadata(kind, value, state);
            return;
        }

        if (state.CurrentCodeSet == null)
        {
            return;
        }

        if (kind == ExternalCodeXsdCaptureKind.Name)
        {
            state.CurrentCodeSet.Name = value;
        }
        else if (kind == ExternalCodeXsdCaptureKind.Definition)
        {
            state.CurrentCodeSet.Definition = value;
        }
        else if (kind == ExternalCodeXsdCaptureKind.RegistrationStatus)
        {
            state.CurrentCodeSet.RegistrationStatus = value;
        }
    }

    internal static void ApplyCodeValueMetadata(
        ExternalCodeXsdCaptureKind kind,
        string value,
        ExternalCodeXsdReadState state)
    {
        ExternalCodeValue? codeValue = state.CurrentCodeValue;
        if (codeValue == null)
        {
            return;
        }

        if (kind == ExternalCodeXsdCaptureKind.Name)
        {
            codeValue.Name = value;
        }
        else if (kind == ExternalCodeXsdCaptureKind.Definition)
        {
            codeValue.Definition = value;
        }
        else if (kind == ExternalCodeXsdCaptureKind.RegistrationStatus)
        {
            codeValue.RegistrationStatus = value;
        }
        else if (kind == ExternalCodeXsdCaptureKind.Requester)
        {
            codeValue.Requester = value;
        }
        else if (kind == ExternalCodeXsdCaptureKind.LastUpdatedDate)
        {
            codeValue.LastUpdatedDate = ReadDateValue(value, "last-updated", state);
        }
        else if (kind == ExternalCodeXsdCaptureKind.CreationDate)
        {
            codeValue.CreationDate = ReadDateValue(value, "creation", state);
        }
        else if (kind == ExternalCodeXsdCaptureKind.ReplacedBy)
        {
            codeValue.ReplacedBy = value;
        }
        else if (kind == ExternalCodeXsdCaptureKind.AdditionalInformation)
        {
            codeValue.AdditionalInformation = value;
        }
    }

    internal static IsoDateValue? ReadDateValue(
        string value,
        string fieldName,
        ExternalCodeXsdReadState state)
    {
        IsoDateValue? date = DateTimeLexical.ParseDate(value.AsSpan());
        if (date == null)
        {
            FailRead(
                state,
                "external-code.xsd.date",
                "An external-code " + fieldName + " date is not a valid XSD date.");
        }

        return date;
    }

    internal static string ReadAttributeValue(BoundedXmlReader reader, string localName)
    {
        string value = string.Empty;
        if (reader.MoveToFirstAttribute())
        {
            bool more = true;
            while (more)
            {
                if (reader.GetLocalName() == localName && reader.GetNamespaceUri().Length == 0)
                {
                    value = reader.GetValue();
                    more = false;
                }
                else
                {
                    more = reader.MoveToNextAttribute();
                }
            }

            reader.MoveBackToElement();
        }

        return value;
    }

    internal static void FailRead(
        ExternalCodeXsdReadState state,
        string errorCode,
        string errorMessage)
    {
        state.Success = false;
        ExternalCodeSetImporter.SetImportError(state.Result, errorCode, errorMessage);
    }
}
