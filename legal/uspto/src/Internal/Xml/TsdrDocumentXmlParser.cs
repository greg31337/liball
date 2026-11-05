namespace uspto;

internal static class TsdrDocumentXmlParser
{
    internal static TrademarkDocumentCollection ParseCollection(string xml, string requestedIdentifier, TrademarkIdentifierResult expectedIdentifier, HttpResponseMetadata metadata, string? rawResponse, UsptoLimits limits)
    {
        ValidateInputs(xml, expectedIdentifier, metadata, limits);
        byte[] bytes = global::System.Text.Encoding.UTF8.GetBytes(xml);
        using (global::System.IO.MemoryStream input = new global::System.IO.MemoryStream(bytes, false))
        {
            TsdrXmlElement root = TsdrXmlParser.ReadTree(input, limits);
            TrademarkDocumentCollection result = new TrademarkDocumentCollection();
            result.RequestedIdentifier = requestedIdentifier;
            result.CanonicalIdentifier = expectedIdentifier.CanonicalNumber;
            result.IdentifierKind = expectedIdentifier.Kind;
            result.RawFormat = DocumentFormat.Xml;
            result.RawResponse = rawResponse;
            result.Provenance = CreateProvenance(metadata, "TSDR case documents");
            global::System.Collections.Generic.List<TsdrXmlElement> elements = new global::System.Collections.Generic.List<TsdrXmlElement>();
            CollectDocuments(root, elements);
            global::System.Collections.Generic.List<TrademarkDocument> documents = new global::System.Collections.Generic.List<TrademarkDocument>();
            foreach (TsdrXmlElement element in elements)
            {
                documents.Add(ParseDocumentElement(element, requestedIdentifier, expectedIdentifier.CanonicalNumber, null, metadata, null));
            }

            result.Documents = documents.ToArray();
            CaptureUnknown(root, new string[] { "Document", "Documents", "DocumentBag", "Identifier", "CaseStatus", "Assignments", "ProsecutionHistoryEvents" }, result.UnknownFields);
            result.IsComplete = true;
            return result;
        }
    }

    internal static TrademarkDocument ParseDocument(string xml, string requestedIdentifier, TrademarkIdentifierResult expectedIdentifier, string documentIdentifier, HttpResponseMetadata metadata, string? rawResponse, UsptoLimits limits)
    {
        ValidateInputs(xml, expectedIdentifier, metadata, limits);
        byte[] bytes = global::System.Text.Encoding.UTF8.GetBytes(xml);
        using (global::System.IO.MemoryStream input = new global::System.IO.MemoryStream(bytes, false))
        {
            TsdrXmlElement root = TsdrXmlParser.ReadTree(input, limits);
            TsdrXmlElement? document = null;
            if (TsdrXmlParser.NameMatches(root.Name, new string[] { "Document" }))
            {
                document = root;
            }
            else
            {
                document = TsdrXmlParser.FindFirst(root, new string[] { "Document" });
            }

            if (document == null)
            {
                throw new ParseException("The TSDR document response does not contain document metadata.", DocumentFormat.Xml, "TSDR document metadata", "/Document", 0L, false, 0, false, null);
            }

            return ParseDocumentElement(document, requestedIdentifier, expectedIdentifier.CanonicalNumber, documentIdentifier, metadata, rawResponse);
        }
    }

    internal static TrademarkDocument ParseDocumentElement(TsdrXmlElement source, string requestedIdentifier, string canonicalIdentifier, string? expectedDocumentIdentifier, HttpResponseMetadata metadata, string? rawResponse)
    {
        TrademarkDocument result = new TrademarkDocument();
        result.RequestedCaseIdentifier = requestedIdentifier;
        result.CanonicalCaseIdentifier = canonicalIdentifier;
        result.DocumentIdentifier = ReadDirectText(source, new string[] { "DocumentIdentifier", "DocumentId", "DocId", "Identifier" });
        if (!global::System.String.IsNullOrWhiteSpace(expectedDocumentIdentifier))
        {
            result.DocumentIdentifier = expectedDocumentIdentifier;
        }

        result.SerialReferenceNumber = ReadDirectText(source, new string[] { "SerialReferenceNumber" });
        TsdrXmlElement? type = FindDirect(source, new string[] { "Type", "DocumentType" });
        if (type != null)
        {
            result.Type = ParseType(type);
        }

        ApplyDocumentDate(result, TsdrXmlParser.ParseDate(ReadDirectText(source, new string[] { "MailDate" })), 1);
        ApplyDocumentDate(result, TsdrXmlParser.ParseDate(ReadDirectText(source, new string[] { "ScanDate" })), 2);
        ApplyDocumentDate(result, TsdrXmlParser.ParseDate(ReadDirectText(source, new string[] { "Timestamp", "TimeStamp" })), 3);
        result.BatchIdentifier = ReadDirectText(source, new string[] { "BatchId", "BatchIdentifier" });
        result.Source = ReadDirectText(source, new string[] { "Source" });
        result.Pages = ParsePages(source);
        result.DocumentTypeName = ReadDirectText(source, new string[] { "DocTypeName", "DocumentTypeName" });
        result.UrlPaths = ReadUrlPaths(source);
        result.Provenance = CreateProvenance(metadata, "TSDR case document");
        result.RawFormat = DocumentFormat.Xml;
        result.RawResponse = rawResponse;
        CaptureUnknown(source, new string[] { "DocumentIdentifier", "DocumentId", "DocId", "Identifier", "SerialReferenceNumber", "Type", "DocumentType", "MailDate", "ScanDate", "Timestamp", "TimeStamp", "BatchId", "BatchIdentifier", "Source", "Pages", "Page", "DocTypeName", "DocumentTypeName", "UrlPathList", "UrlPaths", "UrlPath" }, result.UnknownFields);
        return result;
    }

    internal static TrademarkDocumentType ParseType(TsdrXmlElement source)
    {
        TrademarkDocumentType result = new TrademarkDocumentType();
        result.Name = ReadDirectText(source, new string[] { "Name" });
        result.Description = ReadDirectText(source, new string[] { "Description" });
        result.CategoryName = ReadDirectText(source, new string[] { "CategoryName" });
        result.CategoryDescription = ReadDirectText(source, new string[] { "CategoryDescription" });
        result.ExtendedDescription = ReadDirectText(source, new string[] { "ExtendedDescription" });
        CaptureUnknown(source, new string[] { "Name", "Description", "CategoryName", "CategoryDescription", "ExtendedDescription" }, result.UnknownFields);
        return result;
    }

    internal static TrademarkDocumentPage[] ParsePages(TsdrXmlElement source)
    {
        global::System.Collections.Generic.List<TsdrXmlElement> elements = new global::System.Collections.Generic.List<TsdrXmlElement>();
        foreach (TsdrXmlElement child in source.Children)
        {
            if (TsdrXmlParser.NameMatches(child.Name, new string[] { "Page" }))
            {
                elements.Add(child);
            }
            else if (TsdrXmlParser.NameMatches(child.Name, new string[] { "Pages" }))
            {
                if (FindDirect(child, new string[] { "PageNumber", "Location", "HashDigestId", "HashDigestIdentifier" }) != null)
                {
                    elements.Add(child);
                }
                else
                {
                    TsdrXmlParser.Collect(child, new string[] { "Page" }, elements);
                }
            }
        }

        global::System.Collections.Generic.List<TrademarkDocumentPage> pages = new global::System.Collections.Generic.List<TrademarkDocumentPage>();
        foreach (TsdrXmlElement element in elements)
        {
            TrademarkDocumentPage page = new TrademarkDocumentPage();
            int pageNumber = TsdrXmlParser.ParseNonnegativeInteger(ReadDirectText(element, new string[] { "PageNumber" }));
            if (pageNumber > 0)
            {
                page.PageNumber = pageNumber;
                page.HasPageNumber = true;
            }

            page.Location = ReadDirectText(element, new string[] { "Location" });
            page.HashDigestIdentifier = ReadDirectText(element, new string[] { "HashDigestId", "HashDigestIdentifier" });
            page.SerialNumber = ReadDirectText(element, new string[] { "SerialNumber" });
            TsdrDateValue scanDate = TsdrXmlParser.ParseDate(ReadDirectText(element, new string[] { "ScanDateTime", "ScanDate" }));
            page.ScanDateText = scanDate.Text;
            page.ScanDate = scanDate.Value;
            page.HasScanDate = scanDate.HasValue;
            page.DocumentTypeName = ReadDirectText(element, new string[] { "DocTypeName", "DocumentTypeName" });
            page.CmsUri = ParseCmsUri(ReadDirectText(element, new string[] { "CmsUrl", "CmsUri" }));
            CaptureUnknown(element, new string[] { "PageNumber", "Location", "HashDigestId", "HashDigestIdentifier", "Document", "SerialNumber", "ScanDateTime", "ScanDate", "DocTypeName", "DocumentTypeName", "CmsUrl", "CmsUri" }, page.UnknownFields);
            pages.Add(page);
        }

        return pages.ToArray();
    }

    internal static void ApplyDocumentDate(TrademarkDocument target, TsdrDateValue value, int field)
    {
        switch (field)
        {
            case 1:
                target.MailDateText = value.Text;
                target.MailDate = value.Value;
                target.HasMailDate = value.HasValue;
                break;
            case 2:
                target.ScanDateText = value.Text;
                target.ScanDate = value.Value;
                target.HasScanDate = value.HasValue;
                break;
            case 3:
                target.TimestampText = value.Text;
                target.Timestamp = value.Value;
                target.HasTimestamp = value.HasValue;
                break;
        }
    }

    internal static string[] ReadUrlPaths(TsdrXmlElement source)
    {
        global::System.Collections.Generic.List<string> values = new global::System.Collections.Generic.List<string>();
        foreach (TsdrXmlElement child in source.Children)
        {
            if (TsdrXmlParser.NameMatches(child.Name, new string[] { "UrlPath" }))
            {
                AddTextValue(child, values);
            }
            else if (TsdrXmlParser.NameMatches(child.Name, new string[] { "UrlPathList", "UrlPaths" }))
            {
                if (child.Children.Count == 0)
                {
                    AddTextValue(child, values);
                }
                else
                {
                    foreach (TsdrXmlElement item in child.Children)
                    {
                        AddTextValue(item, values);
                    }
                }
            }
        }

        return values.ToArray();
    }

    internal static void AddTextValue(TsdrXmlElement source, global::System.Collections.Generic.List<string> values)
    {
        string value = ElementText(source);
        if (value.Length > 0)
        {
            values.Add(value);
        }
    }

    internal static global::System.Uri? ParseCmsUri(string? text)
    {
        global::System.Uri? parsed = TsdrJsonParser.ParseUri(text);
        if (parsed == null)
        {
            return null;
        }

        return SecretRedactor.RedactUri(parsed, new string[0]);
    }

    internal static PatentDataProvenance CreateProvenance(HttpResponseMetadata metadata, string datasetIdentifier)
    {
        PatentDataProvenance result = new PatentDataProvenance();
        result.Source = DataProvenance.Tsdr;
        result.SourceUri = metadata.RequestUri;
        result.DatasetIdentifier = datasetIdentifier;
        result.RequestIdentifier = metadata.RequestIdentifier;
        result.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.HasRetrievedAt = true;
        result.IsAuthoritative = true;
        ProvenanceLabeler.AddPatentField(result, "*", result);
        return result;
    }

    internal static void CollectDocuments(TsdrXmlElement source, global::System.Collections.Generic.List<TsdrXmlElement> results)
    {
        bool isDocument = TsdrXmlParser.NameMatches(source.Name, new string[] { "Document" });
        if (TsdrXmlParser.NameMatches(source.Name, new string[] { "Documents" }) && FindDirect(source, new string[] { "DocumentIdentifier", "DocumentId", "DocId", "SerialReferenceNumber", "MailDate", "DocTypeName" }) != null)
        {
            isDocument = true;
        }

        if (isDocument)
        {
            results.Add(source);
            return;
        }

        foreach (TsdrXmlElement child in source.Children)
        {
            CollectDocuments(child, results);
        }
    }

    internal static TsdrXmlElement? FindDirect(TsdrXmlElement source, string[] names)
    {
        foreach (TsdrXmlElement child in source.Children)
        {
            if (TsdrXmlParser.NameMatches(child.Name, names))
            {
                return child;
            }
        }

        return null;
    }

    internal static string? ReadDirectText(TsdrXmlElement source, string[] names)
    {
        TsdrXmlElement? element = FindDirect(source, names);
        if (element == null)
        {
            return null;
        }

        string value = ElementText(element);
        if (value.Length == 0)
        {
            return null;
        }

        return value;
    }

    internal static void CaptureUnknown(TsdrXmlElement source, string[] knownNames, global::System.Collections.Generic.Dictionary<string, string> target)
    {
        foreach (TsdrXmlElement child in source.Children)
        {
            if (!TsdrXmlParser.NameMatches(child.Name, knownNames))
            {
                target[child.Name] = ElementText(child);
            }
        }
    }

    internal static string ElementText(TsdrXmlElement source)
    {
        global::System.Text.StringBuilder value = new global::System.Text.StringBuilder();
        string direct = source.Text.Trim();
        if (direct.Length > 0)
        {
            value.Append(direct);
        }

        foreach (TsdrXmlElement child in source.Children)
        {
            string nested = ElementText(child);
            if (nested.Length > 0)
            {
                if (value.Length > 0)
                {
                    value.Append(' ');
                }

                value.Append(nested);
            }
        }

        return value.ToString();
    }

    internal static void ValidateInputs(string xml, TrademarkIdentifierResult expectedIdentifier, HttpResponseMetadata metadata, UsptoLimits limits)
    {
        if (xml == null)
        {
            throw new ValidationException("A TSDR document XML response is required.", "xml", "REQUIRED", null);
        }

        if (expectedIdentifier == null || !expectedIdentifier.IsValid)
        {
            throw new ValidationException("A valid expected trademark identifier is required.", "expectedIdentifier", "INVALID_IDENTIFIER", null);
        }

        if (metadata == null)
        {
            throw new ValidationException("TSDR document response metadata is required.", "metadata", "REQUIRED", null);
        }

        if (limits == null)
        {
            throw new ValidationException("TSDR document XML limits are required.", "limits", "REQUIRED", null);
        }
    }
}
