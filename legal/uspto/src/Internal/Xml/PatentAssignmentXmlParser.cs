namespace uspto;

internal class PatentAssignmentFlatRecord
{
    internal long RecordNumber = 0L;
    internal string SourcePath = "/";
    internal string ReelAndFrameKey = string.Empty;
    internal PatentAssignment Transaction = new PatentAssignment();
    internal global::System.Collections.Generic.Dictionary<string, string> RawValues = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
    internal string RawXml = string.Empty;
    internal PatentDataProvenance Provenance = new PatentDataProvenance();
}

internal class PatentAssignmentXmlBatch
{
    internal PatentAssignmentFlatRecord[] Records = new PatentAssignmentFlatRecord[0];
    internal global::System.Collections.Generic.Dictionary<string, PatentAssignmentFlatRecord> RecordsByReelAndFrame = new global::System.Collections.Generic.Dictionary<string, PatentAssignmentFlatRecord>(global::System.StringComparer.Ordinal);
    internal int Count = 0;
}

internal static class PatentAssignmentXmlParser
{
    internal static PatentAssignmentFlatRecord ParseRecord(XmlRecord record, PatentDataProvenance provenance)
    {
        if (record == null)
        {
            throw new ValidationException("A generic patent assignment XML record is required.", "record", "REQUIRED", null);
        }

        if (provenance == null)
        {
            throw new ValidationException("Patent assignment bulk provenance is required.", "provenance", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(record.RawXml))
        {
            throw new ValidationException("The patent assignment XML record must retain its raw subtree.", "RawXml", "REQUIRED", null);
        }

        global::System.Xml.XmlElement root = LoadRecordRoot(record.RawXml);
        if (!NameMatches(root, new string[] { "patent-assignment", "patentAssignment", "assignment" }))
        {
            throw AssignmentFailure("The XML record is not a patent assignment.", record.Path, record);
        }

        global::System.Xml.XmlElement? transactionElement = FindFirstDirectChild(root, new string[] { "assignment-record", "assignmentRecord", "transaction", "transaction-record" });
        if (transactionElement == null)
        {
            transactionElement = root;
        }

        PatentAssignment assignment = ParseTransaction(root, transactionElement, record);
        string reel = RequireText(assignment.ReelNumber, "The patent assignment reel number is missing.", record.Path + "/assignment-record/reel-no", record);
        string frame = RequireText(assignment.FrameNumber, "The patent assignment frame number is missing.", record.Path + "/assignment-record/frame-no", record);

        PatentAssignmentFlatRecord result = new PatentAssignmentFlatRecord();
        result.RecordNumber = record.RecordNumber;
        result.SourcePath = record.Path;
        result.ReelAndFrameKey = reel + "/" + frame;
        result.Transaction = assignment;
        result.RawXml = record.RawXml;
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        CaptureRawValues(root, "/" + root.LocalName, result.RawValues);
        return result;
    }

    internal static PatentAssignmentXmlBatch FlattenRecords(XmlRecord[] records, PatentDataProvenance provenance)
    {
        if (records == null)
        {
            throw new ValidationException("Patent assignment XML records are required.", "records", "REQUIRED", null);
        }

        if (provenance == null)
        {
            throw new ValidationException("Patent assignment bulk provenance is required.", "provenance", "REQUIRED", null);
        }

        PatentAssignmentXmlBatch result = new PatentAssignmentXmlBatch();
        result.Records = new PatentAssignmentFlatRecord[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            PatentAssignmentFlatRecord parsed = ParseRecord(records[index], provenance);
            if (result.RecordsByReelAndFrame.ContainsKey(parsed.ReelAndFrameKey))
            {
                throw AssignmentFailure("The patent assignment XML batch contains a duplicate reel/frame key.", records[index].Path, records[index]);
            }

            result.Records[index] = parsed;
            result.RecordsByReelAndFrame.Add(parsed.ReelAndFrameKey, parsed);
        }

        result.Count = result.Records.Length;
        return result;
    }

    internal static global::System.Xml.XmlElement LoadRecordRoot(string xml)
    {
        global::System.Xml.XmlReaderSettings settings = new global::System.Xml.XmlReaderSettings();
        settings.DtdProcessing = global::System.Xml.DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        global::System.Xml.XmlDocument document = new global::System.Xml.XmlDocument();
        document.XmlResolver = null;
        using (global::System.IO.StringReader input = new global::System.IO.StringReader(xml))
        using (global::System.Xml.XmlReader reader = global::System.Xml.XmlReader.Create(input, settings))
        {
            document.Load(reader);
        }

        if (document.DocumentElement == null)
        {
            throw new ParseException("The patent assignment XML record has no root element.", DocumentFormat.Xml, null, "/", 0L, false, 0, false, null);
        }

        return document.DocumentElement;
    }

    internal static PatentAssignment ParseTransaction(global::System.Xml.XmlElement root, global::System.Xml.XmlElement transactionElement, XmlRecord record)
    {
        PatentAssignment result = new PatentAssignment();
        result.ReelNumber = ReadTextWithRootFallback(transactionElement, root, new string[] { "reel-no", "reelNumber", "reel-number", "reel" });
        result.FrameNumber = ReadTextWithRootFallback(transactionElement, root, new string[] { "frame-no", "frameNumber", "frame-number", "frame" });
        result.ReelAndFrameNumber = ReadTextWithRootFallback(transactionElement, root, new string[] { "reel-frame", "reelAndFrameNumber", "reel-frame-number" });
        if (result.ReelAndFrameNumber == null && result.ReelNumber != null && result.FrameNumber != null)
        {
            result.ReelAndFrameNumber = result.ReelNumber + "/" + result.FrameNumber;
        }

        result.Conveyance = ReadTextWithRootFallback(transactionElement, root, new string[] { "conveyance-text", "conveyanceText", "conveyance" });
        result.AttorneyDocketNumber = ReadTextWithRootFallback(transactionElement, root, new string[] { "attorney-docket-number", "attorneyDocketNumber", "docket-number" });
        ApplyPageCount(result, ReadTextWithRootFallback(transactionElement, root, new string[] { "page-count", "pageTotalQuantity", "page-total-quantity" }), record);
        ApplyImageAvailability(result, ReadTextWithRootFallback(transactionElement, root, new string[] { "image-available-indicator", "imageAvailableStatusCode", "image-available-status-code" }), record);
        ApplyDocumentLocation(result, ReadTextWithRootFallback(transactionElement, root, new string[] { "assignment-document-location-uri", "assignmentDocumentLocationURI", "document-location-uri" }), record);
        ApplyTransactionDates(result, transactionElement, root, record);
        result.Assignors = ParseAssignorCollection(root, record);
        result.Assignees = ParseAssigneeCollection(root);
        result.Correspondents = ParseCorrespondentCollection(root, new string[] { "correspondents", "correspondence-addresses" }, new string[] { "correspondent", "correspondence-address" });
        if (result.Correspondents.Length == 0)
        {
            global::System.Xml.XmlElement? singleCorrespondent = FindFirstDirectChild(root, new string[] { "correspondent", "correspondence-address" });
            if (singleCorrespondent != null)
            {
                result.Correspondents = new PatentAssignmentCorrespondent[] { ParseCorrespondent(singleCorrespondent) };
            }
        }

        global::System.Xml.XmlElement? domestic = FindFirstDirectChild(root, new string[] { "domestic-representative", "domesticRepresentative" });
        if (domestic != null)
        {
            result.DomesticRepresentative = ParseCorrespondent(domestic);
        }

        result.Properties = ParsePropertyCollection(root);
        CaptureUnknownDirect(transactionElement, result.UnknownFields, new string[]
        {
            "reel-no", "reelNumber", "reel-number", "reel", "frame-no", "frameNumber", "frame-number", "frame", "reel-frame", "reelAndFrameNumber", "reel-frame-number",
            "conveyance-text", "conveyanceText", "conveyance", "attorney-docket-number", "attorneyDocketNumber", "docket-number", "page-count", "pageTotalQuantity", "page-total-quantity",
            "image-available-indicator", "imageAvailableStatusCode", "image-available-status-code", "assignment-document-location-uri", "assignmentDocumentLocationURI", "document-location-uri",
            "received-date", "assignment-received-date", "assignmentReceivedDate", "recorded-date", "recordation-date", "assignmentRecordedDate", "mailed-date", "assignment-mailed-date", "assignmentMailedDate"
        });
        return result;
    }

    internal static void ApplyPageCount(PatentAssignment assignment, string? text, XmlRecord record)
    {
        if (text == null)
        {
            return;
        }

        int parsed = ParseNonNegativeInteger(text);
        if (parsed < 0)
        {
            throw AssignmentFailure("The patent assignment page count is invalid.", record.Path + "/assignment-record/page-count", record);
        }

        assignment.PageCount = parsed;
        assignment.HasPageCount = true;
    }

    internal static int ParseNonNegativeInteger(string text)
    {
        string value = text.Trim();
        if (value.Length == 0)
        {
            return -1;
        }

        int result = 0;
        int index;
        for (index = 0; index < value.Length; index += 1)
        {
            int digit = value[index] - '0';
            if (digit < 0 || digit > 9 || result > (global::System.Int32.MaxValue - digit) / 10)
            {
                return -1;
            }

            result = (result * 10) + digit;
        }

        return result;
    }

    internal static void ApplyImageAvailability(PatentAssignment assignment, string? text, XmlRecord record)
    {
        if (text == null)
        {
            return;
        }

        string value = text.Trim();
        if (value.Equals("Y", global::System.StringComparison.OrdinalIgnoreCase) || value.Equals("YES", global::System.StringComparison.OrdinalIgnoreCase) || value.Equals("TRUE", global::System.StringComparison.OrdinalIgnoreCase) || value == "1")
        {
            assignment.IsImageAvailable = true;
            assignment.HasIsImageAvailable = true;
            return;
        }

        if (value.Equals("N", global::System.StringComparison.OrdinalIgnoreCase) || value.Equals("NO", global::System.StringComparison.OrdinalIgnoreCase) || value.Equals("FALSE", global::System.StringComparison.OrdinalIgnoreCase) || value == "0")
        {
            assignment.IsImageAvailable = false;
            assignment.HasIsImageAvailable = true;
            return;
        }

        throw AssignmentFailure("The patent assignment image-availability value is invalid.", record.Path + "/assignment-record/image-available-indicator", record);
    }

    internal static void ApplyDocumentLocation(PatentAssignment assignment, string? text, XmlRecord record)
    {
        if (text == null)
        {
            return;
        }

        if (!global::System.Uri.IsWellFormedUriString(text, global::System.UriKind.Absolute))
        {
            throw AssignmentFailure("The patent assignment document location is invalid.", record.Path + "/assignment-record/assignment-document-location-uri", record);
        }

        assignment.DocumentUri = new global::System.Uri(text);
    }

    internal static void ApplyTransactionDates(PatentAssignment assignment, global::System.Xml.XmlElement transactionElement, global::System.Xml.XmlElement root, XmlRecord record)
    {
        string? received = ReadTextWithRootFallback(transactionElement, root, new string[] { "received-date", "assignment-received-date", "assignmentReceivedDate" });
        if (received != null)
        {
            assignment.ReceivedDate = ParseRequiredDate(received, record.Path + "/assignment-record/received-date", record);
            assignment.HasReceivedDate = true;
        }

        string? recorded = ReadTextWithRootFallback(transactionElement, root, new string[] { "recorded-date", "recordation-date", "assignmentRecordedDate" });
        if (recorded != null)
        {
            assignment.RecordedDate = ParseRequiredDate(recorded, record.Path + "/assignment-record/recorded-date", record);
            assignment.HasRecordedDate = true;
        }

        string? mailed = ReadTextWithRootFallback(transactionElement, root, new string[] { "mailed-date", "assignment-mailed-date", "assignmentMailedDate" });
        if (mailed != null)
        {
            assignment.MailedDate = ParseRequiredDate(mailed, record.Path + "/assignment-record/mailed-date", record);
            assignment.HasMailedDate = true;
        }
    }

    internal static global::System.DateTimeOffset ParseRequiredDate(string text, string path, XmlRecord record)
    {
        JsonDateParseResult parsed = JsonFieldReader.ParseFlexibleDate(text.Trim());
        if (!parsed.IsValid)
        {
            throw AssignmentFailure("The patent assignment XML date is invalid.", path, record);
        }

        return parsed.Value;
    }

    internal static PatentAssignmentAssignor[] ParseAssignorCollection(global::System.Xml.XmlElement root, XmlRecord record)
    {
        global::System.Xml.XmlElement[] elements = FindCollectionItems(root, new string[] { "assignors", "assignor-bag", "assignorBag" }, new string[] { "assignor", "assignor-data" });
        PatentAssignmentAssignor[] result = new PatentAssignmentAssignor[elements.Length];
        int index;
        for (index = 0; index < elements.Length; index += 1)
        {
            PatentAssignmentAssignor assignor = new PatentAssignmentAssignor();
            assignor.Name = ReadDirectText(elements[index], new string[] { "name", "assignor-name", "assignorNameText" });
            string? executionDate = ReadDirectText(elements[index], new string[] { "execution-date", "executionDate", "assignment-execution-date" });
            if (executionDate != null)
            {
                assignor.ExecutionDate = ParseRequiredDate(executionDate, record.Path + "/assignors/assignor/execution-date", record);
                assignor.HasExecutionDate = true;
            }

            CaptureUnknownDirect(elements[index], assignor.UnknownFields, new string[] { "name", "assignor-name", "assignorNameText", "execution-date", "executionDate", "assignment-execution-date" });
            result[index] = assignor;
        }

        return result;
    }

    internal static PatentAssignmentAssignee[] ParseAssigneeCollection(global::System.Xml.XmlElement root)
    {
        global::System.Xml.XmlElement[] elements = FindCollectionItems(root, new string[] { "assignees", "assignee-bag", "assigneeBag" }, new string[] { "assignee", "assignee-data" });
        PatentAssignmentAssignee[] result = new PatentAssignmentAssignee[elements.Length];
        int index;
        for (index = 0; index < elements.Length; index += 1)
        {
            PatentAssignmentAssignee assignee = new PatentAssignmentAssignee();
            assignee.Name = ReadDirectText(elements[index], new string[] { "name", "assignee-name", "assigneeNameText" });
            assignee.Address = ParseAddress(elements[index]);
            CaptureUnknownDirect(elements[index], assignee.UnknownFields, KnownPartyFields());
            result[index] = assignee;
        }

        return result;
    }

    internal static PatentAssignmentCorrespondent[] ParseCorrespondentCollection(global::System.Xml.XmlElement root, string[] wrapperNames, string[] itemNames)
    {
        global::System.Xml.XmlElement[] elements = FindCollectionItems(root, wrapperNames, itemNames);
        PatentAssignmentCorrespondent[] result = new PatentAssignmentCorrespondent[elements.Length];
        int index;
        for (index = 0; index < elements.Length; index += 1)
        {
            result[index] = ParseCorrespondent(elements[index]);
        }

        return result;
    }

    internal static PatentAssignmentCorrespondent ParseCorrespondent(global::System.Xml.XmlElement element)
    {
        PatentAssignmentCorrespondent result = new PatentAssignmentCorrespondent();
        result.Name = ReadDirectText(element, new string[] { "name", "correspondent-name", "correspondentNameText" });
        result.Address = ParseAddress(element);
        CaptureUnknownDirect(element, result.UnknownFields, KnownPartyFields());
        return result;
    }

    internal static PatentAddress ParseAddress(global::System.Xml.XmlElement party)
    {
        global::System.Xml.XmlElement? address = FindFirstDirectChild(party, new string[] { "address", "assignee-address", "postal-address" });
        global::System.Xml.XmlElement source = party;
        if (address != null)
        {
            source = address;
        }

        PatentAddress result = new PatentAddress();
        result.NameLineOne = ReadDirectText(source, new string[] { "name-line-one", "nameLineOneText" });
        result.NameLineTwo = ReadDirectText(source, new string[] { "name-line-two", "nameLineTwoText" });
        result.AddressLineOne = ReadDirectText(source, new string[] { "address-1", "address-line-one", "addressLineOneText" });
        result.AddressLineTwo = ReadDirectText(source, new string[] { "address-2", "address-line-two", "addressLineTwoText" });
        result.AddressLineThree = ReadDirectText(source, new string[] { "address-3", "address-line-three", "addressLineThreeText" });
        result.AddressLineFour = ReadDirectText(source, new string[] { "address-4", "address-line-four", "addressLineFourText" });
        result.City = ReadDirectText(source, new string[] { "city", "city-name", "cityName" });
        result.GeographicRegionName = ReadDirectText(source, new string[] { "state-name", "region-name", "geographicRegionName" });
        result.GeographicRegionCode = ReadDirectText(source, new string[] { "state", "state-code", "geographicRegionCode" });
        result.PostalCode = ReadDirectText(source, new string[] { "postcode", "postal-code", "zip-code", "postalCode" });
        result.CountryCode = ReadDirectText(source, new string[] { "country-code", "countryCode" });
        result.CountryName = ReadDirectText(source, new string[] { "country-name", "countryName" });
        string? country = ReadDirectText(source, new string[] { "country" });
        if (country != null)
        {
            if (country.Length == 2 && result.CountryCode == null)
            {
                result.CountryCode = country;
            }
            else if (result.CountryName == null)
            {
                result.CountryName = country;
            }
        }

        result.PostalAddressCategory = ReadDirectText(source, new string[] { "postal-address-category", "postalAddressCategory" });
        result.EmailAddress = ReadDirectText(source, new string[] { "email-address", "emailAddress" });
        return result;
    }

    internal static PatentAssignmentProperty[] ParsePropertyCollection(global::System.Xml.XmlElement root)
    {
        global::System.Xml.XmlElement[] elements = FindCollectionItems(root, new string[] { "properties", "property-bag", "propertyBag" }, new string[] { "property", "patent-property" });
        PatentAssignmentProperty[] result = new PatentAssignmentProperty[elements.Length];
        int index;
        for (index = 0; index < elements.Length; index += 1)
        {
            result[index] = ParseProperty(elements[index]);
        }

        return result;
    }

    internal static PatentAssignmentProperty ParseProperty(global::System.Xml.XmlElement element)
    {
        PatentAssignmentProperty result = new PatentAssignmentProperty();
        result.PropertyIdentifier = ReadDirectText(element, new string[] { "property-identifier", "property-number", "document-identifier" });
        result.PropertyType = ReadDirectText(element, new string[] { "property-type", "property-type-code", "property-category" });
        result.ApplicationNumber = ReadDirectText(element, new string[] { "application-number", "application-number-text", "applicationNumberText" });
        result.PatentNumber = ReadDirectText(element, new string[] { "patent-number", "patent-number-text", "patentNumber" });
        result.PublicationNumber = ReadDirectText(element, new string[] { "publication-number", "publication-number-text", "publicationNumber" });
        result.Title = ReadDirectText(element, new string[] { "title", "invention-title", "property-title" });

        global::System.Xml.XmlElement? identifier = FindFirstDirectChild(element, new string[] { "document-id", "document-identifier" });
        if (identifier != null)
        {
            string? documentNumber = ReadDirectText(identifier, new string[] { "doc-number", "document-number", "number" });
            string? kind = ReadDirectText(identifier, new string[] { "kind", "kind-code", "document-kind" });
            if (result.PropertyIdentifier == null)
            {
                result.PropertyIdentifier = documentNumber;
            }

            if (result.PropertyType == null)
            {
                result.PropertyType = kind;
            }

            AddDocumentIdentifierFields(result, identifier);
        }

        CaptureUnknownDirect(element, result.UnknownFields, new string[]
        {
            "property-identifier", "property-number", "document-identifier", "property-type", "property-type-code", "property-category", "application-number", "application-number-text", "applicationNumberText",
            "patent-number", "patent-number-text", "patentNumber", "publication-number", "publication-number-text", "publicationNumber", "title", "invention-title", "property-title", "document-id"
        });
        return result;
    }

    internal static void AddDocumentIdentifierFields(PatentAssignmentProperty property, global::System.Xml.XmlElement identifier)
    {
        string? country = ReadDirectText(identifier, new string[] { "country", "country-code" });
        string? number = ReadDirectText(identifier, new string[] { "doc-number", "document-number", "number" });
        string? kind = ReadDirectText(identifier, new string[] { "kind", "kind-code", "document-kind" });
        string? date = ReadDirectText(identifier, new string[] { "date", "document-date" });
        AddUnknownValue(property.UnknownFields, "document-id.country", country);
        AddUnknownValue(property.UnknownFields, "document-id.doc-number", number);
        AddUnknownValue(property.UnknownFields, "document-id.kind", kind);
        AddUnknownValue(property.UnknownFields, "document-id.date", date);
    }

    internal static void AddUnknownValue(global::System.Collections.Generic.Dictionary<string, string> values, string name, string? value)
    {
        if (value != null)
        {
            values[name] = value;
        }
    }

    internal static string[] KnownPartyFields()
    {
        return new string[]
        {
            "name", "assignee-name", "assigneeNameText", "correspondent-name", "correspondentNameText", "address", "assignee-address", "postal-address",
            "name-line-one", "nameLineOneText", "name-line-two", "nameLineTwoText", "address-1", "address-line-one", "addressLineOneText", "address-2", "address-line-two", "addressLineTwoText",
            "address-3", "address-line-three", "addressLineThreeText", "address-4", "address-line-four", "addressLineFourText", "city", "city-name", "cityName", "state-name", "region-name",
            "geographicRegionName", "state", "state-code", "geographicRegionCode", "postcode", "postal-code", "zip-code", "postalCode", "country-code", "countryCode", "country-name", "countryName",
            "country", "postal-address-category", "postalAddressCategory", "email-address", "emailAddress"
        };
    }

    internal static global::System.Xml.XmlElement[] FindCollectionItems(global::System.Xml.XmlElement root, string[] wrapperNames, string[] itemNames)
    {
        global::System.Collections.Generic.List<global::System.Xml.XmlElement> result = new global::System.Collections.Generic.List<global::System.Xml.XmlElement>();
        foreach (global::System.Xml.XmlNode node in root.ChildNodes)
        {
            if (node.NodeType != global::System.Xml.XmlNodeType.Element)
            {
                continue;
            }

            global::System.Xml.XmlElement element = (global::System.Xml.XmlElement)node;
            if (NameMatches(element, itemNames))
            {
                result.Add(element);
            }
            else if (NameMatches(element, wrapperNames))
            {
                foreach (global::System.Xml.XmlNode childNode in element.ChildNodes)
                {
                    if (childNode.NodeType == global::System.Xml.XmlNodeType.Element && NameMatches((global::System.Xml.XmlElement)childNode, itemNames))
                    {
                        result.Add((global::System.Xml.XmlElement)childNode);
                    }
                }
            }
        }

        global::System.Xml.XmlElement[] values = new global::System.Xml.XmlElement[result.Count];
        result.CopyTo(values, 0);
        return values;
    }

    internal static global::System.Xml.XmlElement? FindFirstDirectChild(global::System.Xml.XmlElement element, string[] names)
    {
        foreach (global::System.Xml.XmlNode node in element.ChildNodes)
        {
            if (node.NodeType == global::System.Xml.XmlNodeType.Element && NameMatches((global::System.Xml.XmlElement)node, names))
            {
                return (global::System.Xml.XmlElement)node;
            }
        }

        return null;
    }

    internal static string? ReadTextWithRootFallback(global::System.Xml.XmlElement primary, global::System.Xml.XmlElement root, string[] names)
    {
        string? value = ReadDirectText(primary, names);
        if (value == null && primary != root)
        {
            value = ReadDirectText(root, names);
        }

        return value;
    }

    internal static string? ReadDirectText(global::System.Xml.XmlElement element, string[] names)
    {
        global::System.Xml.XmlElement? child = FindFirstDirectChild(element, names);
        if (child == null)
        {
            return null;
        }

        string value = child.InnerText.Trim();
        if (value.Length == 0)
        {
            return null;
        }

        return value;
    }

    internal static bool NameMatches(global::System.Xml.XmlElement element, string[] names)
    {
        string normalized = NormalizeName(element.LocalName);
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            if (normalized.Equals(NormalizeName(names[index]), global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static string NormalizeName(string name)
    {
        global::System.Text.StringBuilder result = new global::System.Text.StringBuilder();
        int index;
        for (index = 0; index < name.Length; index += 1)
        {
            char character = name[index];
            if (global::System.Char.IsLetterOrDigit(character))
            {
                result.Append(global::System.Char.ToLowerInvariant(character));
            }
        }

        return result.ToString();
    }

    internal static string RequireText(string? value, string message, string path, XmlRecord record)
    {
        if (global::System.String.IsNullOrWhiteSpace(value))
        {
            throw AssignmentFailure(message, path, record);
        }

        return value;
    }

    internal static void CaptureUnknownDirect(global::System.Xml.XmlElement element, global::System.Collections.Generic.Dictionary<string, string> destination, string[] knownNames)
    {
        foreach (global::System.Xml.XmlNode node in element.ChildNodes)
        {
            if (node.NodeType != global::System.Xml.XmlNodeType.Element)
            {
                continue;
            }

            global::System.Xml.XmlElement child = (global::System.Xml.XmlElement)node;
            if (!NameMatches(child, knownNames))
            {
                string name = child.LocalName;
                int suffix = 2;
                while (destination.ContainsKey(name))
                {
                    name = child.LocalName + "[" + suffix.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
                    suffix += 1;
                }

                destination.Add(name, child.InnerText);
            }
        }
    }

    internal static void CaptureRawValues(global::System.Xml.XmlElement element, string path, global::System.Collections.Generic.Dictionary<string, string> destination)
    {
        foreach (global::System.Xml.XmlAttribute attribute in element.Attributes)
        {
            if (!attribute.NamespaceURI.Equals("http://www.w3.org/2000/xmlns/", global::System.StringComparison.Ordinal))
            {
                destination[path + "/@" + attribute.LocalName] = attribute.Value;
            }
        }

        global::System.Collections.Generic.List<global::System.Xml.XmlElement> children = new global::System.Collections.Generic.List<global::System.Xml.XmlElement>();
        foreach (global::System.Xml.XmlNode node in element.ChildNodes)
        {
            if (node.NodeType == global::System.Xml.XmlNodeType.Element)
            {
                children.Add((global::System.Xml.XmlElement)node);
            }
        }

        if (children.Count == 0)
        {
            destination[path] = element.InnerText;
            return;
        }

        int index;
        for (index = 0; index < children.Count; index += 1)
        {
            global::System.Xml.XmlElement child = children[index];
            int occurrence = 1;
            int total = 0;
            int siblingIndex;
            for (siblingIndex = 0; siblingIndex < children.Count; siblingIndex += 1)
            {
                if (children[siblingIndex].LocalName.Equals(child.LocalName, global::System.StringComparison.Ordinal))
                {
                    total += 1;
                    if (siblingIndex < index)
                    {
                        occurrence += 1;
                    }
                }
            }

            string childPath = path + "/" + child.LocalName;
            if (total > 1)
            {
                childPath += "[" + occurrence.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            }

            CaptureRawValues(child, childPath, destination);
        }
    }

    internal static ParseException AssignmentFailure(string message, string path, XmlRecord record)
    {
        return new ParseException(message, DocumentFormat.Xml, null, path, 0L, false, record.StartLineNumber, record.HasLineInfo, null);
    }
}
