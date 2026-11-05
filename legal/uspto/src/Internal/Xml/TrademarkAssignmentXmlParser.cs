namespace uspto;

internal class TrademarkAssignmentParty
{
    internal string? Name = null;
    internal string? EntityType = null;
    internal string? Nationality = null;
    internal TrademarkAddress Address = new TrademarkAddress();
    internal string? ExecutionDateText = null;
    internal global::System.DateTimeOffset ExecutionDate = default(global::System.DateTimeOffset);
    internal bool HasExecutionDate = false;
    internal global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

internal class TrademarkAssignmentCorrespondent
{
    internal string? Name = null;
    internal TrademarkAddress Address = new TrademarkAddress();
    internal global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

internal class TrademarkAssignmentProperty
{
    internal string? PropertyIdentifier = null;
    internal string? PropertyType = null;
    internal string? SerialNumber = null;
    internal string? RegistrationNumber = null;
    internal string? InternationalRegistrationNumber = null;
    internal string? MarkText = null;
    internal global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

internal class TrademarkAssignment
{
    internal string? ReelNumber = null;
    internal string? FrameNumber = null;
    internal string? ReelAndFrameNumber = null;
    internal int PageCount = 0;
    internal bool HasPageCount = false;
    internal string? Conveyance = null;
    internal string? LastUpdatedDateText = null;
    internal global::System.DateTimeOffset LastUpdatedDate = default(global::System.DateTimeOffset);
    internal bool HasLastUpdatedDate = false;
    internal string? RecordedDateText = null;
    internal global::System.DateTimeOffset RecordedDate = default(global::System.DateTimeOffset);
    internal bool HasRecordedDate = false;
    internal string? ReceivedDateText = null;
    internal global::System.DateTimeOffset ReceivedDate = default(global::System.DateTimeOffset);
    internal bool HasReceivedDate = false;
    internal string? MailedDateText = null;
    internal global::System.DateTimeOffset MailedDate = default(global::System.DateTimeOffset);
    internal bool HasMailedDate = false;
    internal string? PurgeIndicator = null;
    internal TrademarkAssignmentParty[] Assignors = new TrademarkAssignmentParty[0];
    internal TrademarkAssignmentParty[] Assignees = new TrademarkAssignmentParty[0];
    internal TrademarkAssignmentCorrespondent? Correspondent = null;
    internal TrademarkAssignmentProperty[] Properties = new TrademarkAssignmentProperty[0];
    internal global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

internal class TrademarkAssignmentXmlRecord
{
    internal long RecordNumber = 0L;
    internal string SourcePath = "/";
    internal string ReelAndFrameKey = string.Empty;
    internal TrademarkAssignment Assignment = new TrademarkAssignment();
    internal string RawXml = string.Empty;
    internal global::System.Collections.Generic.Dictionary<string, string> RawValues = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
    internal PatentDataProvenance Provenance = new PatentDataProvenance();
}

internal class TrademarkAssignmentXmlBatch
{
    internal TrademarkAssignmentXmlRecord[] Records = new TrademarkAssignmentXmlRecord[0];
    internal global::System.Collections.Generic.Dictionary<string, TrademarkAssignmentXmlRecord> RecordsByReelAndFrame = new global::System.Collections.Generic.Dictionary<string, TrademarkAssignmentXmlRecord>(global::System.StringComparer.Ordinal);
    internal int Count = 0;
}

internal static class TrademarkAssignmentXmlParser
{
    internal static TrademarkAssignmentXmlRecord ParseRecord(XmlRecord record, PatentDataProvenance provenance)
    {
        if (record == null)
        {
            throw new ValidationException("A trademark assignment XML record is required.", "record", "REQUIRED", null);
        }

        if (provenance == null)
        {
            throw new ValidationException("Trademark assignment XML provenance is required.", "provenance", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(record.RawXml))
        {
            throw new ValidationException("The trademark assignment XML record must retain its raw subtree.", "RawXml", "REQUIRED", null);
        }

        global::System.Xml.XmlElement root = LoadAssignmentRoot(record.RawXml);
        if (!AssignmentNameMatches(root, new string[] { "trademark-assignment", "trademarkAssignment", "assignment" }))
        {
            throw TrademarkAssignmentFailure("The XML record is not a trademark assignment.", record.Path, record);
        }

        global::System.Xml.XmlElement? transaction = FindAssignmentDirectChild(root, new string[] { "assignment-record", "assignmentRecord", "transaction", "transaction-record" });
        if (transaction == null)
        {
            transaction = root;
        }

        TrademarkAssignment assignment = ParseAssignmentTransaction(root, transaction, record);
        string reel = RequireAssignmentText(assignment.ReelNumber, "The trademark assignment reel number is missing.", record.Path + "/assignment-record/reel-no", record);
        string frame = RequireAssignmentText(assignment.FrameNumber, "The trademark assignment frame number is missing.", record.Path + "/assignment-record/frame-no", record);
        PatentDataProvenance copied = OdpJsonParser.CopyProvenance(provenance);
        copied.Source = DataProvenance.Bulk;
        copied.IsAuthoritative = true;

        TrademarkAssignmentXmlRecord result = new TrademarkAssignmentXmlRecord();
        result.RecordNumber = record.RecordNumber;
        result.SourcePath = record.Path;
        result.ReelAndFrameKey = reel + "/" + frame;
        result.Assignment = assignment;
        result.RawXml = record.RawXml;
        result.Provenance = copied;
        CaptureAssignmentRawValues(root, "/" + root.LocalName, result.RawValues);
        return result;
    }

    internal static TrademarkAssignmentXmlBatch FlattenRecords(XmlRecord[] records, PatentDataProvenance provenance)
    {
        if (records == null)
        {
            throw new ValidationException("Trademark assignment XML records are required.", "records", "REQUIRED", null);
        }

        if (provenance == null)
        {
            throw new ValidationException("Trademark assignment XML provenance is required.", "provenance", "REQUIRED", null);
        }

        TrademarkAssignmentXmlBatch result = new TrademarkAssignmentXmlBatch();
        result.Records = new TrademarkAssignmentXmlRecord[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            TrademarkAssignmentXmlRecord parsed = ParseRecord(records[index], provenance);
            if (result.RecordsByReelAndFrame.ContainsKey(parsed.ReelAndFrameKey))
            {
                throw TrademarkAssignmentFailure("The trademark assignment XML batch contains a duplicate reel/frame key.", records[index].Path, records[index]);
            }

            result.Records[index] = parsed;
            result.RecordsByReelAndFrame.Add(parsed.ReelAndFrameKey, parsed);
        }

        result.Count = result.Records.Length;
        return result;
    }

    internal static global::System.Xml.XmlElement LoadAssignmentRoot(string xml)
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
            throw new ParseException("The trademark assignment XML record has no root element.", DocumentFormat.Xml, "trademark assignment XML", "/", 0L, false, 0, false, null);
        }

        return document.DocumentElement;
    }

    internal static TrademarkAssignment ParseAssignmentTransaction(global::System.Xml.XmlElement root, global::System.Xml.XmlElement transaction, XmlRecord record)
    {
        TrademarkAssignment result = new TrademarkAssignment();
        result.ReelNumber = ReadAssignmentWithFallback(transaction, root, new string[] { "reel-no", "reel-number", "reelNumber", "reel" });
        result.FrameNumber = ReadAssignmentWithFallback(transaction, root, new string[] { "frame-no", "frame-number", "frameNumber", "frame" });
        result.ReelAndFrameNumber = ReadAssignmentWithFallback(transaction, root, new string[] { "reel-frame", "reel-and-frame-number", "reelAndFrameNumber" });
        if (result.ReelAndFrameNumber == null && result.ReelNumber != null && result.FrameNumber != null)
        {
            result.ReelAndFrameNumber = result.ReelNumber + "/" + result.FrameNumber;
        }

        string? pages = ReadAssignmentWithFallback(transaction, root, new string[] { "page-count", "page-total-quantity", "pageCount" });
        if (pages != null)
        {
            int pageCount = PatentAssignmentXmlParser.ParseNonNegativeInteger(pages);
            if (pageCount < 0)
            {
                throw TrademarkAssignmentFailure("The trademark assignment page count is invalid.", record.Path + "/assignment-record/page-count", record);
            }

            result.PageCount = pageCount;
            result.HasPageCount = true;
        }

        result.Conveyance = ReadAssignmentWithFallback(transaction, root, new string[] { "conveyance-text", "conveyance", "conveyanceText" });
        result.PurgeIndicator = ReadAssignmentWithFallback(transaction, root, new string[] { "purge-indicator", "purgeIndicator" });
        ApplyAssignmentDate(result, ReadAssignmentWithFallback(transaction, root, new string[] { "last-update-date", "last-updated-date", "lastUpdateDate" }), 1);
        ApplyAssignmentDate(result, ReadAssignmentWithFallback(transaction, root, new string[] { "recorded-date", "recordation-date", "recordedDate" }), 2);
        ApplyAssignmentDate(result, ReadAssignmentWithFallback(transaction, root, new string[] { "received-date", "assignment-received-date", "receivedDate" }), 3);
        ApplyAssignmentDate(result, ReadAssignmentWithFallback(transaction, root, new string[] { "mailed-date", "assignment-mailed-date", "mailedDate" }), 4);
        result.Assignors = ParseAssignmentParties(root, new string[] { "assignors", "assignor-bag" }, new string[] { "assignor", "assignor-data" }, true);
        result.Assignees = ParseAssignmentParties(root, new string[] { "assignees", "assignee-bag" }, new string[] { "assignee", "assignee-data" }, false);
        global::System.Xml.XmlElement? correspondent = FindAssignmentDirectChild(root, new string[] { "correspondent", "correspondence-address" });
        if (correspondent != null)
        {
            result.Correspondent = ParseAssignmentCorrespondent(correspondent);
        }

        result.Properties = ParseTrademarkProperties(root);
        CaptureAssignmentUnknown(transaction, result.UnknownFields, new string[]
        {
            "reel-no", "reel-number", "reelNumber", "reel", "frame-no", "frame-number", "frameNumber", "frame", "reel-frame", "reel-and-frame-number", "reelAndFrameNumber",
            "page-count", "page-total-quantity", "pageCount", "conveyance-text", "conveyance", "conveyanceText", "purge-indicator", "purgeIndicator",
            "last-update-date", "last-updated-date", "lastUpdateDate", "recorded-date", "recordation-date", "recordedDate", "received-date", "assignment-received-date", "receivedDate",
            "mailed-date", "assignment-mailed-date", "mailedDate"
        });
        return result;
    }

    internal static void ApplyAssignmentDate(TrademarkAssignment target, string? text, int field)
    {
        TsdrDateValue parsed = TsdrXmlParser.ParseDate(text);
        if (field == 1)
        {
            target.LastUpdatedDateText = parsed.Text;
            target.LastUpdatedDate = parsed.Value;
            target.HasLastUpdatedDate = parsed.HasValue;
        }
        else if (field == 2)
        {
            target.RecordedDateText = parsed.Text;
            target.RecordedDate = parsed.Value;
            target.HasRecordedDate = parsed.HasValue;
        }
        else if (field == 3)
        {
            target.ReceivedDateText = parsed.Text;
            target.ReceivedDate = parsed.Value;
            target.HasReceivedDate = parsed.HasValue;
        }
        else
        {
            target.MailedDateText = parsed.Text;
            target.MailedDate = parsed.Value;
            target.HasMailedDate = parsed.HasValue;
        }
    }

    internal static TrademarkAssignmentParty[] ParseAssignmentParties(global::System.Xml.XmlElement root, string[] wrapperNames, string[] itemNames, bool assignor)
    {
        global::System.Xml.XmlElement[] elements = FindAssignmentCollection(root, wrapperNames, itemNames);
        TrademarkAssignmentParty[] result = new TrademarkAssignmentParty[elements.Length];
        int index;
        for (index = 0; index < elements.Length; index += 1)
        {
            TrademarkAssignmentParty party = new TrademarkAssignmentParty();
            party.Name = ReadAssignmentText(elements[index], new string[] { "name", "party-name", "assignor-name", "assignee-name" });
            party.EntityType = ReadAssignmentText(elements[index], new string[] { "entity-type", "entity-type-code", "legal-entity-type" });
            party.Nationality = ReadAssignmentText(elements[index], new string[] { "nationality", "nationality-country", "citizenship" });
            party.Address = ParseTrademarkAssignmentAddress(elements[index]);
            if (assignor)
            {
                party.ExecutionDateText = ReadAssignmentText(elements[index], new string[] { "execution-date", "assignment-execution-date", "executionDate" });
                TsdrDateValue parsed = TsdrXmlParser.ParseDate(party.ExecutionDateText);
                party.ExecutionDate = parsed.Value;
                party.HasExecutionDate = parsed.HasValue;
            }

            CaptureAssignmentUnknown(elements[index], party.UnknownFields, new string[]
            {
                "name", "party-name", "assignor-name", "assignee-name", "entity-type", "entity-type-code", "legal-entity-type", "nationality", "nationality-country", "citizenship",
                "execution-date", "assignment-execution-date", "executionDate", "address", "address-1", "address-2", "address-line-one", "address-line-two", "city", "state", "state-or-province",
                "postcode", "postal-code", "zip", "country", "country-code", "country-name"
            });
            result[index] = party;
        }

        return result;
    }

    internal static TrademarkAssignmentCorrespondent ParseAssignmentCorrespondent(global::System.Xml.XmlElement element)
    {
        TrademarkAssignmentCorrespondent result = new TrademarkAssignmentCorrespondent();
        result.Name = ReadAssignmentText(element, new string[] { "name", "correspondent-name" });
        result.Address = ParseTrademarkAssignmentAddress(element);
        CaptureAssignmentUnknown(element, result.UnknownFields, new string[] { "name", "correspondent-name", "address", "address-1", "address-2", "address-line-one", "address-line-two", "city", "state", "state-or-province", "postcode", "postal-code", "zip", "country", "country-code", "country-name" });
        return result;
    }

    internal static TrademarkAddress ParseTrademarkAssignmentAddress(global::System.Xml.XmlElement party)
    {
        global::System.Xml.XmlElement source = party;
        global::System.Xml.XmlElement? nested = FindAssignmentDirectChild(party, new string[] { "address", "postal-address", "assignee-address" });
        if (nested != null)
        {
            source = nested;
        }

        TrademarkAddress result = new TrademarkAddress();
        result.Line1 = ReadAssignmentText(source, new string[] { "address-1", "address-line-one", "line-1", "address1" });
        result.Line2 = ReadAssignmentText(source, new string[] { "address-2", "address-line-two", "line-2", "address2" });
        result.City = ReadAssignmentText(source, new string[] { "city" });
        result.StateOrProvince = ReadAssignmentText(source, new string[] { "state", "state-or-province", "region" });
        result.PostalCode = ReadAssignmentText(source, new string[] { "postcode", "postal-code", "zip" });
        result.CountryCode = ReadAssignmentText(source, new string[] { "country-code" });
        result.CountryName = ReadAssignmentText(source, new string[] { "country-name" });
        string? country = ReadAssignmentText(source, new string[] { "country" });
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

        return result;
    }

    internal static TrademarkAssignmentProperty[] ParseTrademarkProperties(global::System.Xml.XmlElement root)
    {
        global::System.Xml.XmlElement[] elements = FindAssignmentCollection(root, new string[] { "properties", "property-bag", "trademark-properties" }, new string[] { "property", "trademark-property" });
        TrademarkAssignmentProperty[] result = new TrademarkAssignmentProperty[elements.Length];
        int index;
        for (index = 0; index < elements.Length; index += 1)
        {
            global::System.Xml.XmlElement element = elements[index];
            TrademarkAssignmentProperty property = new TrademarkAssignmentProperty();
            property.PropertyIdentifier = ReadAssignmentText(element, new string[] { "property-identifier", "property-number" });
            property.PropertyType = ReadAssignmentText(element, new string[] { "property-type", "property-type-code" });
            property.SerialNumber = ReadAssignmentText(element, new string[] { "serial-number", "trademark-serial-number" });
            property.RegistrationNumber = ReadAssignmentText(element, new string[] { "registration-number", "trademark-registration-number" });
            property.InternationalRegistrationNumber = ReadAssignmentText(element, new string[] { "international-registration-number", "international-registration-no" });
            property.MarkText = ReadAssignmentText(element, new string[] { "mark", "mark-text", "mark-identification", "word-mark" });
            if (property.PropertyIdentifier == null)
            {
                property.PropertyIdentifier = property.SerialNumber;
                if (property.PropertyIdentifier == null)
                {
                    property.PropertyIdentifier = property.RegistrationNumber;
                }
            }

            CaptureAssignmentUnknown(element, property.UnknownFields, new string[]
            {
                "property-identifier", "property-number", "property-type", "property-type-code", "serial-number", "trademark-serial-number", "registration-number", "trademark-registration-number",
                "international-registration-number", "international-registration-no", "mark", "mark-text", "mark-identification", "word-mark"
            });
            result[index] = property;
        }

        return result;
    }

    internal static global::System.Xml.XmlElement[] FindAssignmentCollection(global::System.Xml.XmlElement root, string[] wrapperNames, string[] itemNames)
    {
        global::System.Collections.Generic.List<global::System.Xml.XmlElement> result = new global::System.Collections.Generic.List<global::System.Xml.XmlElement>();
        foreach (global::System.Xml.XmlNode node in root.ChildNodes)
        {
            if (node.NodeType != global::System.Xml.XmlNodeType.Element)
            {
                continue;
            }

            global::System.Xml.XmlElement element = (global::System.Xml.XmlElement)node;
            if (AssignmentNameMatches(element, itemNames))
            {
                result.Add(element);
            }
            else if (AssignmentNameMatches(element, wrapperNames))
            {
                foreach (global::System.Xml.XmlNode childNode in element.ChildNodes)
                {
                    if (childNode.NodeType == global::System.Xml.XmlNodeType.Element && AssignmentNameMatches((global::System.Xml.XmlElement)childNode, itemNames))
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

    internal static global::System.Xml.XmlElement? FindAssignmentDirectChild(global::System.Xml.XmlElement element, string[] names)
    {
        foreach (global::System.Xml.XmlNode node in element.ChildNodes)
        {
            if (node.NodeType == global::System.Xml.XmlNodeType.Element && AssignmentNameMatches((global::System.Xml.XmlElement)node, names))
            {
                return (global::System.Xml.XmlElement)node;
            }
        }

        return null;
    }

    internal static string? ReadAssignmentWithFallback(global::System.Xml.XmlElement primary, global::System.Xml.XmlElement fallback, string[] names)
    {
        string? result = ReadAssignmentText(primary, names);
        if (result == null && primary != fallback)
        {
            result = ReadAssignmentText(fallback, names);
        }

        return result;
    }

    internal static string? ReadAssignmentText(global::System.Xml.XmlElement element, string[] names)
    {
        global::System.Xml.XmlElement? child = FindAssignmentDirectChild(element, names);
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

    internal static bool AssignmentNameMatches(global::System.Xml.XmlElement element, string[] names)
    {
        string normalized = NormalizeAssignmentName(element.LocalName);
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            if (normalized.Equals(NormalizeAssignmentName(names[index]), global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static string NormalizeAssignmentName(string value)
    {
        global::System.Text.StringBuilder result = new global::System.Text.StringBuilder();
        int index;
        for (index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if (global::System.Char.IsLetterOrDigit(character))
            {
                result.Append(global::System.Char.ToLowerInvariant(character));
            }
        }

        return result.ToString();
    }

    internal static string RequireAssignmentText(string? value, string message, string path, XmlRecord record)
    {
        if (global::System.String.IsNullOrWhiteSpace(value))
        {
            throw TrademarkAssignmentFailure(message, path, record);
        }

        return value;
    }

    internal static void CaptureAssignmentUnknown(global::System.Xml.XmlElement element, global::System.Collections.Generic.Dictionary<string, string> destination, string[] knownNames)
    {
        foreach (global::System.Xml.XmlNode node in element.ChildNodes)
        {
            if (node.NodeType != global::System.Xml.XmlNodeType.Element)
            {
                continue;
            }

            global::System.Xml.XmlElement child = (global::System.Xml.XmlElement)node;
            if (!AssignmentNameMatches(child, knownNames))
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

    internal static void CaptureAssignmentRawValues(global::System.Xml.XmlElement element, string path, global::System.Collections.Generic.Dictionary<string, string> destination)
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

            CaptureAssignmentRawValues(child, childPath, destination);
        }
    }

    internal static ParseException TrademarkAssignmentFailure(string message, string path, XmlRecord record)
    {
        return new ParseException(message, DocumentFormat.Xml, "trademark assignment XML", path, 0L, false, record.StartLineNumber, record.HasLineInfo, null);
    }
}
