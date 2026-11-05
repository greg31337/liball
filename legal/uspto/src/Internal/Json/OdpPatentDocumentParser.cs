namespace uspto;

internal static class OdpPatentDocumentParser
{
    internal static PatentDocumentList Parse(string json, string expectedApplicationNumber, HttpResponseMetadata responseMetadata)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP patent document response is required.", "json", "REQUIRED", null);
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
            JsonFieldValue bag = root.ReadArray("documentBag");
            if (!bag.IsPresent || !bag.IsValid || bag.IsNull)
            {
                throw DocumentFailure("The ODP patent document response has an invalid document collection.", bag.Path);
            }

            JsonFieldValue count = root.ReadInt32("count");
            ValidateOptional(count, "The ODP patent document response count is invalid.");
            if (count.IsPresent && !count.IsNull && (count.Int32Value < 0 || count.Int32Value != bag.ArrayItems.Length))
            {
                throw DocumentFailure("The ODP patent document response count does not match its collection.", count.Path);
            }

            JsonFieldValue requestIdentifier = root.ReadString("requestIdentifier");
            ValidateOptional(requestIdentifier, "The ODP patent document request identifier is invalid.");
            string? resolvedRequestIdentifier = responseMetadata.RequestIdentifier;
            if (requestIdentifier.IsPresent && !requestIdentifier.IsNull)
            {
                resolvedRequestIdentifier = requestIdentifier.StringValue;
            }

            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, resolvedRequestIdentifier);
            PatentDocument[] documents = new PatentDocument[bag.ArrayItems.Length];
            int index;
            for (index = 0; index < bag.ArrayItems.Length; index += 1)
            {
                documents[index] = ParseDocument(bag.ArrayItems[index], bag.ArrayItemPaths[index], expectedApplicationNumber);
            }

            PatentDocumentList result = new PatentDocumentList();
            result.ApplicationNumber = expectedApplicationNumber;
            result.Documents = documents;
            result.Count = documents.Length;
            result.RequestIdentifier = resolvedRequestIdentifier;
            result.Provenance = provenance;
            JsonUnknownField[] unknown = root.CaptureUnknownFields(new string[] { "documentBag", "count", "requestIdentifier" });
            for (index = 0; index < unknown.Length; index += 1)
            {
                result.UnknownFields[unknown[index].Name] = unknown[index].RawJson;
            }

            return result;
        }
    }

    internal static PatentDocument ParseDocument(global::System.Text.Json.JsonElement element, string path, string expectedApplicationNumber)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw DocumentFailure("The ODP patent document entry is invalid.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        JsonFieldValue applicationNumber = reader.ReadString("applicationNumberText");
        ValidateOptional(applicationNumber, "The ODP patent document application number is invalid.");
        if (applicationNumber.IsPresent && !applicationNumber.IsNull)
        {
            PatentApplicationNumberResult parsedNumber = PatentIdentifierParser.ParseApplicationNumber(applicationNumber.StringValue ?? string.Empty);
            if (!parsedNumber.IsValid || !global::System.String.Equals(parsedNumber.CanonicalNumber, expectedApplicationNumber, global::System.StringComparison.Ordinal))
            {
                throw DocumentFailure("The ODP patent document does not match the requested application.", applicationNumber.Path);
            }
        }

        PatentDocument document = new PatentDocument();
        document.ApplicationNumber = expectedApplicationNumber;
        document.DocumentIdentifier = ReadOptionalString(reader, "documentIdentifier");
        document.Code = ReadOptionalString(reader, "documentCode");
        document.Description = ReadOptionalString(reader, "documentCodeDescriptionText");
        document.DirectionCategory = ReadOptionalString(reader, "documentDirectionCategory");
        ApplyDocumentDates(document, reader);
        document.DownloadAlternatives = ParseDownloadAlternatives(reader);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownDocumentFields());
        int index;
        for (index = 0; index < unknown.Length; index += 1)
        {
            document.UnknownFields[unknown[index].Name] = unknown[index].RawJson;
        }

        return document;
    }

    internal static void ApplyDocumentDates(PatentDocument document, JsonFieldReader reader)
    {
        JsonFieldValue official = ReadAliasedDate(reader, new string[] { "officialDate" });
        ValidateOptional(official, "The ODP patent document official date is invalid.");
        if (official.IsPresent && !official.IsNull)
        {
            document.OfficialDate = official.DateValue;
            document.HasOfficialDate = true;
        }

        JsonFieldValue filed = ReadAliasedDate(reader, new string[] { "documentFilingDate", "filingDate" });
        ValidateOptional(filed, "The ODP patent document filing date is invalid.");
        if (filed.IsPresent && !filed.IsNull)
        {
            document.FiledAt = filed.DateValue;
            document.HasFiledAt = true;
        }

        JsonFieldValue created = ReadAliasedDate(reader, new string[] { "documentCreateDateTime", "documentCreationDateTime", "createDateTime" });
        ValidateOptional(created, "The ODP patent document creation time is invalid.");
        if (created.IsPresent && !created.IsNull)
        {
            document.CreatedAt = created.DateValue;
            document.HasCreatedAt = true;
        }

        JsonFieldValue modified = ReadAliasedDate(reader, new string[] { "documentLastModifiedDateTime", "lastModifiedDateTime" });
        ValidateOptional(modified, "The ODP patent document last-modified time is invalid.");
        if (modified.IsPresent && !modified.IsNull)
        {
            document.LastModifiedAt = modified.DateValue;
            document.HasLastModifiedAt = true;
        }
    }

    internal static PatentDocumentDownloadAlternative[] ParseDownloadAlternatives(JsonFieldReader document)
    {
        JsonFieldValue bag = document.ReadArray("downloadOptionBag");
        ValidateOptional(bag, "The ODP patent document download collection is invalid.");
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentDocumentDownloadAlternative[0];
        }

        PatentDocumentDownloadAlternative[] alternatives = new PatentDocumentDownloadAlternative[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            global::System.Text.Json.JsonElement item = bag.ArrayItems[index];
            if (item.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw DocumentFailure("The ODP patent document download alternative is invalid.", bag.ArrayItemPaths[index]);
            }

            alternatives[index] = ParseDownloadAlternative(item, bag.ArrayItemPaths[index]);
        }

        return alternatives;
    }

    internal static PatentDocumentDownloadAlternative ParseDownloadAlternative(global::System.Text.Json.JsonElement element, string path)
    {
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentDocumentDownloadAlternative alternative = new PatentDocumentDownloadAlternative();
        alternative.MimeTypeIdentifier = ReadAliasedString(reader, new string[] { "mimeTypeIdentifier", "mimeType", "mediaType" });
        alternative.MediaType = NormalizeMediaType(alternative.MimeTypeIdentifier);
        alternative.Format = ResponseReader.DetectDocumentFormat(alternative.MediaType);
        alternative.FileName = ReadAliasedString(reader, new string[] { "fileName", "documentName" });
        string? location = ReadAliasedString(reader, new string[] { "downloadUrl", "documentURI", "downloadURI", "fileDownloadURI" });
        if (location != null)
        {
            if (!global::System.Uri.IsWellFormedUriString(location, global::System.UriKind.Absolute))
            {
                throw DocumentFailure("The ODP patent document download URI is invalid.", FindPresentPath(reader, new string[] { "downloadUrl", "documentURI", "downloadURI", "fileDownloadURI" }));
            }

            alternative.DownloadUri = new global::System.Uri(location);
        }

        JsonFieldValue pages = ReadAliasedInt32(reader, new string[] { "pageTotalQuantity", "pageCount", "pageQuantity" });
        ValidateOptional(pages, "The ODP patent document page count is invalid.");
        if (pages.IsPresent && !pages.IsNull)
        {
            if (pages.Int32Value < 0)
            {
                throw DocumentFailure("The ODP patent document page count cannot be negative.", pages.Path);
            }

            alternative.PageCount = pages.Int32Value;
            alternative.HasPageCount = true;
        }

        JsonFieldValue size = ReadAliasedInt64(reader, new string[] { "documentSizeQuantity", "fileSize", "sizeQuantity", "documentSize" });
        ValidateOptional(size, "The ODP patent document size is invalid.");
        if (size.IsPresent && !size.IsNull)
        {
            if (size.Int64Value < 0L)
            {
                throw DocumentFailure("The ODP patent document size cannot be negative.", size.Path);
            }

            alternative.Size = size.Int64Value;
            alternative.HasSize = true;
        }

        if (alternative.Format == DocumentFormat.Unknown && alternative.DownloadUri != null)
        {
            alternative.Format = DetectUriFormat(alternative.DownloadUri);
        }

        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownDownloadFields());
        int index;
        for (index = 0; index < unknown.Length; index += 1)
        {
            alternative.UnknownFields[unknown[index].Name] = unknown[index].RawJson;
        }

        return alternative;
    }

    internal static JsonFieldValue ReadAliasedDate(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadDate(names[index]);
            if (value.IsPresent)
            {
                if (!value.IsValid && !value.IsNull)
                {
                    ApplyOffsetDateNormalization(value, reader, names[index]);
                }

                return value;
            }
        }

        return reader.ReadDate(names[0]);
    }

    internal static void ApplyOffsetDateNormalization(JsonFieldValue value, JsonFieldReader reader, string name)
    {
        JsonFieldValue text = reader.ReadString(name);
        if (!text.IsValid || text.StringValue == null)
        {
            return;
        }

        string source = text.StringValue.Trim();
        int signIndex = source.Length - 5;
        if (signIndex >= 19 && (source[signIndex] == '+' || source[signIndex] == '-'))
        {
            string normalized = source.Substring(0, signIndex + 3) + ":" + source.Substring(signIndex + 3);
            JsonDateParseResult parsed = JsonFieldReader.ParseFlexibleDate(normalized);
            if (parsed.IsValid)
            {
                value.DateValue = parsed.Value;
                value.IsValid = true;
            }
        }
    }

    internal static string? ReadAliasedString(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadString(names[index]);
            ValidateOptional(value, "The ODP patent document field is invalid.");
            if (value.IsPresent)
            {
                if (value.IsNull)
                {
                    return null;
                }

                return value.StringValue;
            }
        }

        return null;
    }

    internal static JsonFieldValue ReadAliasedInt32(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadInt32(names[index]);
            if (value.IsPresent)
            {
                return value;
            }
        }

        return reader.ReadInt32(names[0]);
    }

    internal static JsonFieldValue ReadAliasedInt64(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadInt64(names[index]);
            if (value.IsPresent)
            {
                return value;
            }
        }

        return reader.ReadInt64(names[0]);
    }

    internal static string FindPresentPath(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadRaw(names[index]);
            if (value.IsPresent)
            {
                return value.Path;
            }
        }

        return reader.Path;
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptional(value, "The ODP patent document field is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static string? NormalizeMediaType(string? identifier)
    {
        if (identifier == null)
        {
            return null;
        }

        string normalized = identifier.Trim().ToLowerInvariant();
        int parameter = normalized.IndexOf(';');
        if (parameter >= 0)
        {
            normalized = normalized.Substring(0, parameter).Trim();
        }

        switch (normalized)
        {
            case "pdf":
                return UsptoMediaTypes.Pdf;
            case "xml":
                return UsptoMediaTypes.Xml;
            case "zip":
                return UsptoMediaTypes.Zip;
            case "json":
                return UsptoMediaTypes.Json;
            case "txt":
            case "text":
                return UsptoMediaTypes.Text;
            case "tif":
            case "tiff":
                return "image/tiff";
            case "jpg":
            case "jpeg":
                return "image/jpeg";
            case "png":
                return "image/png";
            default:
                if (normalized.IndexOf('/') > 0)
                {
                    return normalized;
                }

                return null;
        }
    }

    internal static DocumentFormat DetectUriFormat(global::System.Uri uri)
    {
        string path = uri.AbsolutePath;
        if (path.EndsWith(".pdf", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Pdf;
        }

        if (path.EndsWith(".xml", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Xml;
        }

        if (path.EndsWith(".zip", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Zip;
        }

        if (path.EndsWith(".json", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Json;
        }

        if (path.EndsWith(".txt", global::System.StringComparison.OrdinalIgnoreCase))
        {
            return DocumentFormat.Text;
        }

        return DocumentFormat.Unknown;
    }

    internal static void ValidateOptional(JsonFieldValue value, string message)
    {
        if (!value.IsValid)
        {
            throw DocumentFailure(message, value.Path);
        }
    }

    internal static string[] KnownDocumentFields()
    {
        return new string[]
        {
            "applicationNumberText",
            "officialDate",
            "documentFilingDate",
            "filingDate",
            "documentCreateDateTime",
            "documentCreationDateTime",
            "createDateTime",
            "documentLastModifiedDateTime",
            "lastModifiedDateTime",
            "documentIdentifier",
            "documentCode",
            "documentCodeDescriptionText",
            "documentDirectionCategory",
            "downloadOptionBag"
        };
    }

    internal static string[] KnownDownloadFields()
    {
        return new string[]
        {
            "mimeTypeIdentifier",
            "mimeType",
            "mediaType",
            "downloadUrl",
            "documentURI",
            "downloadURI",
            "fileDownloadURI",
            "fileName",
            "documentName",
            "pageTotalQuantity",
            "pageCount",
            "pageQuantity",
            "documentSizeQuantity",
            "fileSize",
            "sizeQuantity",
            "documentSize"
        };
    }

    internal static ParseException DocumentFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "ODP patent document listing", path, 0L, false, 0, false, null);
    }
}
