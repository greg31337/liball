namespace uspto;

internal static class OdpPatentAssociatedDocumentParser
{
    internal static PatentAssociatedDocumentList Parse(string json, string expectedApplicationNumber, HttpResponseMetadata responseMetadata)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP associated-document response is required.", "json", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(expectedApplicationNumber))
        {
            throw new ValidationException("An expected application number is required.", "expectedApplicationNumber", "REQUIRED", null);
        }

        if (responseMetadata == null)
        {
            throw new ValidationException("ODP response metadata is required.", "responseMetadata", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = new JsonFieldReader(document.RootElement, "$");
            JsonFieldValue count = root.ReadInt32("count");
            if (!count.IsPresent || !count.IsValid || count.IsNull || count.Int32Value != 1)
            {
                throw AssociatedDocumentFailure("The ODP associated-document response must contain exactly one application.", count.Path);
            }

            JsonFieldValue records = root.ReadArray("patentFileWrapperDataBag");
            if (!records.IsPresent || !records.IsValid || records.IsNull || records.ArrayItems.Length != 1)
            {
                throw AssociatedDocumentFailure("The ODP associated-document response has an invalid application collection.", records.Path);
            }

            global::System.Text.Json.JsonElement recordElement = records.ArrayItems[0];
            string recordPath = records.ArrayItemPaths[0];
            if (recordElement.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw AssociatedDocumentFailure("The ODP associated-document application record is invalid.", recordPath);
            }

            JsonFieldReader record = new JsonFieldReader(recordElement, recordPath);
            string? requestIdentifier = ReadRequestIdentifier(root, responseMetadata.RequestIdentifier);
            requestIdentifier = ReadRequestIdentifier(record, requestIdentifier);
            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            PatentAssociatedDocumentList result = ParseRecord(record, expectedApplicationNumber, requestIdentifier, provenance);
            JsonUnknownField[] unknown = root.CaptureUnknownFields(new string[] { "count", "patentFileWrapperDataBag", "requestIdentifier" });
            int index;
            for (index = 0; index < unknown.Length; index += 1)
            {
                string name = unknown[index].Name;
                if (result.UnknownFields.ContainsKey(name))
                {
                    name = "response." + name;
                }

                result.UnknownFields[name] = unknown[index].RawJson;
            }

            return result;
        }
    }

    internal static PatentAssociatedDocumentList ParseRecord(JsonFieldReader record, string expectedApplicationNumber, string? requestIdentifier, PatentDataProvenance provenance)
    {
        JsonFieldValue applicationNumber = record.ReadString("applicationNumberText");
        if (!applicationNumber.IsPresent || !applicationNumber.IsValid || applicationNumber.IsNull)
        {
            throw AssociatedDocumentFailure("The ODP associated-document application number is missing or invalid.", applicationNumber.Path);
        }

        PatentApplicationNumberResult parsedNumber = PatentIdentifierParser.ParseApplicationNumber(applicationNumber.StringValue ?? string.Empty);
        if (!parsedNumber.IsValid || !global::System.String.Equals(parsedNumber.CanonicalNumber, expectedApplicationNumber, global::System.StringComparison.Ordinal))
        {
            throw AssociatedDocumentFailure("The ODP associated-document response does not match the requested application.", applicationNumber.Path);
        }

        PatentAssociatedDocumentList result = new PatentAssociatedDocumentList();
        result.ApplicationNumber = parsedNumber.CanonicalNumber;
        result.PublicationFiles = ParseFileCollection(record, "pgpubDocumentMetaData", "PreGrantPublication");
        result.GrantFiles = ParseFileCollection(record, "grantDocumentMetaData", "Grant");
        result.Count = result.PublicationFiles.Length + result.GrantFiles.Length;
        result.RequestIdentifier = requestIdentifier;
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = record.CaptureUnknownFields(new string[] { "applicationNumberText", "pgpubDocumentMetaData", "grantDocumentMetaData", "requestIdentifier" });
        int index;
        for (index = 0; index < unknown.Length; index += 1)
        {
            result.UnknownFields[unknown[index].Name] = unknown[index].RawJson;
        }

        return result;
    }

    internal static PatentFileMetadata[] ParseFileCollection(JsonFieldReader record, string fieldName, string category)
    {
        JsonFieldValue value = record.ReadRaw(fieldName);
        if (!value.IsPresent || value.IsNull)
        {
            return new PatentFileMetadata[0];
        }

        if (value.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Object)
        {
            return new PatentFileMetadata[] { ParseFileItem(value.ElementValue, value.Path, category) };
        }

        if (value.ElementValue.ValueKind != global::System.Text.Json.JsonValueKind.Array)
        {
            throw AssociatedDocumentFailure("The ODP associated-document file metadata is invalid.", value.Path);
        }

        PatentFileMetadata[] files = new PatentFileMetadata[value.ElementValue.GetArrayLength()];
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in value.ElementValue.EnumerateArray())
        {
            string itemPath = value.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            files[index] = ParseFileItem(item, itemPath, category);
            index += 1;
        }

        return files;
    }

    internal static PatentFileMetadata ParseFileItem(global::System.Text.Json.JsonElement element, string path, string category)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw AssociatedDocumentFailure("The ODP associated-document file metadata entry is invalid.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentFileMetadata file = new PatentFileMetadata();
        file.Category = category;
        file.ProductIdentifier = ReadOptionalString(reader, "productIdentifier");
        file.ArchiveFileName = ReadOptionalString(reader, "zipFileName");
        file.XmlFileName = ReadOptionalString(reader, "xmlFileName");
        file.MediaType = ReadOptionalString(reader, "fileMediaType");
        file.DocumentIdentifier = ReadDocumentIdentifier(reader, category);
        string? location = ReadOptionalString(reader, "fileLocationURI");
        if (location != null)
        {
            if (!global::System.Uri.IsWellFormedUriString(location, global::System.UriKind.Absolute))
            {
                throw AssociatedDocumentFailure("The ODP associated-document file location is invalid.", path + ".fileLocationURI");
            }

            file.LocationUri = new global::System.Uri(location);
        }

        JsonFieldValue created = ReadFileDate(reader);
        ValidateOptional(created, "The ODP associated-document file creation time is invalid.");
        if (created.IsPresent && !created.IsNull)
        {
            file.CreatedAt = created.DateValue;
            file.HasCreatedAt = true;
        }

        JsonFieldValue size = reader.ReadInt64("fileSize");
        ValidateOptional(size, "The ODP associated-document file size is invalid.");
        if (size.IsPresent && !size.IsNull)
        {
            if (size.Int64Value < 0L)
            {
                throw AssociatedDocumentFailure("The ODP associated-document file size cannot be negative.", size.Path);
            }

            file.Size = size.Int64Value;
            file.HasSize = true;
        }

        file.Format = ResponseReader.DetectDocumentFormat(file.MediaType);
        if (file.Format == DocumentFormat.Unknown && HasZipLocation(file))
        {
            file.Format = DocumentFormat.Zip;
        }
        else if (file.Format == DocumentFormat.Unknown && HasXmlLocation(file))
        {
            file.Format = DocumentFormat.Xml;
        }

        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownFileFields());
        int index;
        for (index = 0; index < unknown.Length; index += 1)
        {
            file.UnknownFields[unknown[index].Name] = unknown[index].RawJson;
        }

        return file;
    }

    internal static string? ReadDocumentIdentifier(JsonFieldReader reader, string category)
    {
        string? value = ReadOptionalString(reader, "documentIdentifier");
        if (value != null)
        {
            return value;
        }

        if (global::System.String.Equals(category, "Grant", global::System.StringComparison.Ordinal))
        {
            value = ReadOptionalString(reader, "patentNumber");
            if (value == null)
            {
                value = ReadOptionalString(reader, "patentNumberText");
            }

            return value;
        }

        value = ReadOptionalString(reader, "publicationNumber");
        if (value == null)
        {
            value = ReadOptionalString(reader, "publicationNumberText");
        }

        return value;
    }

    internal static string? ReadRequestIdentifier(JsonFieldReader reader, string? fallback)
    {
        JsonFieldValue value = reader.ReadString("requestIdentifier");
        ValidateOptional(value, "The ODP associated-document request identifier is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            return value.StringValue;
        }

        return fallback;
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptional(value, "The ODP associated-document field is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static JsonFieldValue ReadFileDate(JsonFieldReader reader)
    {
        JsonFieldValue value = reader.ReadDate("fileCreateDateTime");
        if (value.IsValid || !value.IsPresent || value.IsNull)
        {
            return value;
        }

        JsonFieldValue text = reader.ReadString("fileCreateDateTime");
        if (!text.IsValid || text.StringValue == null)
        {
            return value;
        }

        string source = text.StringValue.Trim();
        if (source.Length >= 19 && source[4] == '-' && source[7] == '-' && source[10] == ':')
        {
            string normalized = source.Substring(0, 10) + "T" + source.Substring(11);
            JsonDateParseResult parsed = JsonFieldReader.ParseFlexibleDate(normalized);
            if (parsed.IsValid)
            {
                value.DateValue = parsed.Value;
                value.IsValid = true;
            }
        }

        return value;
    }

    internal static bool HasZipLocation(PatentFileMetadata file)
    {
        if (file.ArchiveFileName != null && file.ArchiveFileName.EndsWith(".zip", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return file.LocationUri != null && file.LocationUri.AbsolutePath.EndsWith(".zip", global::System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool HasXmlLocation(PatentFileMetadata file)
    {
        if (file.XmlFileName != null && file.ArchiveFileName == null && file.XmlFileName.EndsWith(".xml", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return file.LocationUri != null && file.LocationUri.AbsolutePath.EndsWith(".xml", global::System.StringComparison.OrdinalIgnoreCase);
    }

    internal static void ValidateOptional(JsonFieldValue value, string message)
    {
        if (!value.IsValid)
        {
            throw AssociatedDocumentFailure(message, value.Path);
        }
    }

    internal static string[] KnownFileFields()
    {
        return new string[]
        {
            "zipFileName",
            "productIdentifier",
            "fileLocationURI",
            "fileCreateDateTime",
            "xmlFileName",
            "fileMediaType",
            "fileSize",
            "documentIdentifier",
            "publicationNumber",
            "publicationNumberText",
            "patentNumber",
            "patentNumberText"
        };
    }

    internal static ParseException AssociatedDocumentFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "ODP patent associated-document retrieval", path, 0L, false, 0, false, null);
    }
}
