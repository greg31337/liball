namespace uspto;

internal class PatentFileWrapperBulkRecord
{
    internal string? ApplicationNumber = null;
    internal DocumentFormat Format = DocumentFormat.Unknown;
    internal string? SourceName = null;
    internal string RawSource = string.Empty;
    internal string NormalizedJson = string.Empty;
    internal PatentDossierOfficialData Official = new PatentDossierOfficialData();
    internal PatentDataProvenance Provenance = new PatentDataProvenance();
}

internal static class PatentFileWrapperBulkParser
{
    internal static PatentFileWrapperBulkRecord ParseJsonRecord(JsonRecord record, PatentDataProvenance provenance)
    {
        if (record == null)
        {
            throw new ValidationException("A generic JSON record is required.", "record", "REQUIRED", null);
        }

        if (provenance == null)
        {
            throw new ValidationException("Patent bulk provenance is required.", "provenance", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(record.RawJson))
        {
            throw new ValidationException("The generic JSON record must retain its raw source.", "RawJson", "REQUIRED", null);
        }

        string path = "$[" + record.RecordNumber.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
        if (record.RecordNumber <= 0L)
        {
            path = "$";
        }

        return ParseNormalizedJson(record.RawJson, path, DocumentFormat.Json, null, record.RawJson, provenance);
    }

    internal static PatentFileWrapperBulkRecord ParseXmlRecord(XmlRecord record, PatentDataProvenance provenance)
    {
        if (record == null)
        {
            throw new ValidationException("A generic XML record is required.", "record", "REQUIRED", null);
        }

        if (provenance == null)
        {
            throw new ValidationException("Patent bulk provenance is required.", "provenance", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(record.RawXml))
        {
            throw new ValidationException("The generic XML record must retain its raw subtree.", "RawXml", "REQUIRED", null);
        }

        string normalizedJson = ConvertXmlRecord(record.RawXml);
        return ParseNormalizedJson(normalizedJson, record.Path, DocumentFormat.Xml, null, record.RawXml, provenance);
    }

    internal static PatentFileWrapperBulkRecord ParseNormalizedJson(string json, string path, DocumentFormat format, string? sourceName, string rawSource, PatentDataProvenance provenance)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            global::System.Text.Json.JsonElement element = ResolveRecordElement(document.RootElement, path);
            string resolvedPath = ResolveRecordPath(document.RootElement, path);
            JsonFieldReader reader = new JsonFieldReader(element, resolvedPath);
            JsonFieldValue applicationNumberValue = reader.ReadString("applicationNumberText");
            if (!applicationNumberValue.IsPresent || !applicationNumberValue.IsValid || applicationNumberValue.IsNull)
            {
                throw ParseFailure("The Patent File Wrapper bulk record has no valid application number.", format, sourceName, applicationNumberValue.Path);
            }

            PatentApplicationNumberResult parsedNumber = PatentIdentifierParser.ParseApplicationNumber(applicationNumberValue.StringValue ?? string.Empty);
            if (!parsedNumber.IsValid || parsedNumber.CanonicalNumber == null)
            {
                throw ParseFailure("The Patent File Wrapper bulk application number is invalid.", format, sourceName, applicationNumberValue.Path);
            }

            string applicationNumber = parsedNumber.CanonicalNumber;
            string? requestIdentifier = OdpPatentAssociatedDocumentParser.ReadRequestIdentifier(reader, null);
            PatentDataProvenance copiedProvenance = OdpJsonParser.CopyProvenance(provenance);
            copiedProvenance.Source = provenance.Source;
            copiedProvenance.IsAuthoritative = true;

            PatentDossierOfficialData official = new PatentDossierOfficialData();
            official.Application = OdpPatentApplicationParser.ParseWrapper(element, resolvedPath, applicationNumber, copiedProvenance);
            official.Transactions = OdpPatentTransactionParser.ParseRecord(element, resolvedPath, applicationNumber, requestIdentifier, copiedProvenance);
            official.Documents = ParseDocuments(reader, applicationNumber, requestIdentifier, copiedProvenance);
            official.AssociatedDocuments = OdpPatentAssociatedDocumentParser.ParseRecord(reader, applicationNumber, requestIdentifier, copiedProvenance);
            official.Continuity = OdpPatentContinuityPriorityParser.ParseContinuityRecord(reader, applicationNumber, requestIdentifier, copiedProvenance, "Patent File Wrapper bulk record");
            official.ForeignPriorities = OdpPatentContinuityPriorityParser.ParseForeignPriorityRecord(reader, applicationNumber, requestIdentifier, copiedProvenance, "Patent File Wrapper bulk record");
            official.Assignments = OdpPatentAssignmentParser.ParseRecord(reader, applicationNumber, requestIdentifier, copiedProvenance);
            official.Attorneys = OdpPatentAttorneyParser.ParseApplicationRecord(reader, applicationNumber, requestIdentifier, copiedProvenance);
            official.TermAdjustment = OdpPatentTermAdjustmentParser.ParseApplicationRecord(reader, applicationNumber, requestIdentifier, copiedProvenance);

            PatentFileWrapperBulkRecord result = new PatentFileWrapperBulkRecord();
            result.ApplicationNumber = applicationNumber;
            result.Format = format;
            result.SourceName = sourceName;
            result.RawSource = rawSource;
            result.NormalizedJson = json;
            result.Official = official;
            result.Provenance = copiedProvenance;
            return result;
        }
    }

    internal static global::System.Text.Json.JsonElement ResolveRecordElement(global::System.Text.Json.JsonElement root, string path)
    {
        if (root.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("A Patent File Wrapper bulk record must be a JSON object.", DocumentFormat.Json, null, path);
        }

        JsonFieldReader reader = new JsonFieldReader(root, path);
        JsonFieldValue applicationNumber = reader.ReadString("applicationNumberText");
        if (applicationNumber.IsPresent)
        {
            return root;
        }

        JsonFieldValue records = reader.ReadArray("patentFileWrapperDataBag");
        if (!records.IsPresent || !records.IsValid || records.IsNull || records.ArrayItems.Length != 1 || records.ArrayItems[0].ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw ParseFailure("A Patent File Wrapper bulk envelope must contain exactly one object record.", DocumentFormat.Json, null, records.Path);
        }

        return records.ArrayItems[0];
    }

    internal static string ResolveRecordPath(global::System.Text.Json.JsonElement root, string path)
    {
        JsonFieldReader reader = new JsonFieldReader(root, path);
        JsonFieldValue applicationNumber = reader.ReadString("applicationNumberText");
        if (applicationNumber.IsPresent)
        {
            return path;
        }

        return path + ".patentFileWrapperDataBag[0]";
    }

    internal static PatentDocumentList ParseDocuments(JsonFieldReader reader, string applicationNumber, string? requestIdentifier, PatentDataProvenance provenance)
    {
        JsonFieldValue bag = reader.ReadArray("documentBag");
        OdpPatentDocumentParser.ValidateOptional(bag, "The Patent File Wrapper bulk document collection is invalid.");
        PatentDocument[] documents = new PatentDocument[0];
        if (bag.IsPresent && !bag.IsNull)
        {
            documents = new PatentDocument[bag.ArrayItems.Length];
            int index;
            for (index = 0; index < bag.ArrayItems.Length; index += 1)
            {
                documents[index] = OdpPatentDocumentParser.ParseDocument(bag.ArrayItems[index], bag.ArrayItemPaths[index], applicationNumber);
            }
        }

        PatentDocumentList result = new PatentDocumentList();
        result.ApplicationNumber = applicationNumber;
        result.Documents = documents;
        result.Count = documents.Length;
        result.RequestIdentifier = requestIdentifier;
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        return result;
    }

    internal static string ConvertXmlRecord(string xml)
    {
        global::System.Xml.XmlReaderSettings settings = new global::System.Xml.XmlReaderSettings();
        settings.DtdProcessing = global::System.Xml.DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        global::System.Xml.XmlDocument document = new global::System.Xml.XmlDocument();
        document.XmlResolver = null;
        using (global::System.IO.StringReader text = new global::System.IO.StringReader(xml))
        using (global::System.Xml.XmlReader reader = global::System.Xml.XmlReader.Create(text, settings))
        {
            document.Load(reader);
        }

        if (document.DocumentElement == null)
        {
            throw ParseFailure("The Patent File Wrapper XML record has no root element.", DocumentFormat.Xml, null, "/");
        }

        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        using (global::System.Text.Json.Utf8JsonWriter writer = new global::System.Text.Json.Utf8JsonWriter(output))
        {
            WriteXmlObject(writer, document.DocumentElement);
        }

        return global::System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    internal static void WriteXmlObject(global::System.Text.Json.Utf8JsonWriter writer, global::System.Xml.XmlElement element)
    {
        writer.WriteStartObject();
        global::System.Xml.XmlElement[] children = ReadElementChildren(element);
        foreach (global::System.Xml.XmlAttribute attribute in element.Attributes)
        {
            if (attribute.NamespaceURI.Equals("http://www.w3.org/2000/xmlns/", global::System.StringComparison.Ordinal) || attribute.LocalName.Equals("nil", global::System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string attributeName = NormalizeXmlName(attribute.LocalName);
            if (!ContainsChildName(children, attributeName))
            {
                writer.WriteString(attributeName, attribute.Value);
            }
        }

        bool[] written = new bool[children.Length];
        int index;
        for (index = 0; index < children.Length; index += 1)
        {
            if (written[index])
            {
                continue;
            }

            string name = NormalizeXmlName(children[index].LocalName);
            global::System.Collections.Generic.List<global::System.Xml.XmlElement> matching = new global::System.Collections.Generic.List<global::System.Xml.XmlElement>();
            int otherIndex;
            for (otherIndex = index; otherIndex < children.Length; otherIndex += 1)
            {
                if (!written[otherIndex] && NormalizeXmlName(children[otherIndex].LocalName).Equals(name, global::System.StringComparison.Ordinal))
                {
                    written[otherIndex] = true;
                    matching.Add(children[otherIndex]);
                }
            }

            writer.WritePropertyName(name);
            if (name.EndsWith("Bag", global::System.StringComparison.Ordinal))
            {
                WriteXmlBag(writer, matching, name);
            }
            else if (matching.Count > 1)
            {
                writer.WriteStartArray();
                foreach (global::System.Xml.XmlElement item in matching)
                {
                    WriteXmlValue(writer, item);
                }

                writer.WriteEndArray();
            }
            else
            {
                WriteXmlValue(writer, matching[0]);
            }
        }

        writer.WriteEndObject();
    }

    internal static void WriteXmlBag(global::System.Text.Json.Utf8JsonWriter writer, global::System.Collections.Generic.List<global::System.Xml.XmlElement> occurrences, string bagName)
    {
        writer.WriteStartArray();
        string itemName = bagName.Substring(0, bagName.Length - 3);
        foreach (global::System.Xml.XmlElement occurrence in occurrences)
        {
            global::System.Xml.XmlElement[] children = ReadElementChildren(occurrence);
            bool wrapped = children.Length > 0;
            int index;
            for (index = 0; index < children.Length; index += 1)
            {
                string childName = NormalizeXmlName(children[index].LocalName);
                bool itemMatches = childName.Equals(itemName, global::System.StringComparison.OrdinalIgnoreCase)
                    || childName.Equals(itemName + "Data", global::System.StringComparison.OrdinalIgnoreCase)
                    || childName.Equals(itemName + "Record", global::System.StringComparison.OrdinalIgnoreCase)
                    || childName.Equals(itemName + "Item", global::System.StringComparison.OrdinalIgnoreCase);
                if (!itemMatches)
                {
                    wrapped = false;
                }
            }

            if (wrapped)
            {
                for (index = 0; index < children.Length; index += 1)
                {
                    WriteXmlValue(writer, children[index]);
                }
            }
            else
            {
                WriteXmlValue(writer, occurrence);
            }
        }

        writer.WriteEndArray();
    }

    internal static void WriteXmlValue(global::System.Text.Json.Utf8JsonWriter writer, global::System.Xml.XmlElement element)
    {
        if (IsNilElement(element))
        {
            writer.WriteNullValue();
            return;
        }

        global::System.Xml.XmlElement[] children = ReadElementChildren(element);
        if (children.Length == 0)
        {
            writer.WriteStringValue(element.InnerText);
            return;
        }

        WriteXmlObject(writer, element);
    }

    internal static global::System.Xml.XmlElement[] ReadElementChildren(global::System.Xml.XmlElement element)
    {
        global::System.Collections.Generic.List<global::System.Xml.XmlElement> children = new global::System.Collections.Generic.List<global::System.Xml.XmlElement>();
        foreach (global::System.Xml.XmlNode child in element.ChildNodes)
        {
            if (child.NodeType == global::System.Xml.XmlNodeType.Element)
            {
                children.Add((global::System.Xml.XmlElement)child);
            }
        }

        global::System.Xml.XmlElement[] result = new global::System.Xml.XmlElement[children.Count];
        children.CopyTo(result, 0);
        return result;
    }

    internal static bool ContainsChildName(global::System.Xml.XmlElement[] children, string name)
    {
        int index;
        for (index = 0; index < children.Length; index += 1)
        {
            if (NormalizeXmlName(children[index].LocalName).Equals(name, global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsNilElement(global::System.Xml.XmlElement element)
    {
        foreach (global::System.Xml.XmlAttribute attribute in element.Attributes)
        {
            if (attribute.LocalName.Equals("nil", global::System.StringComparison.OrdinalIgnoreCase))
            {
                string value = attribute.Value.Trim();
                return value.Equals("true", global::System.StringComparison.OrdinalIgnoreCase) || value == "1";
            }
        }

        return false;
    }

    internal static string NormalizeXmlName(string name)
    {
        if (global::System.String.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        bool allUpper = name.Equals(name.ToUpperInvariant(), global::System.StringComparison.Ordinal);
        string source = allUpper ? name.ToLowerInvariant() : name;
        global::System.Text.StringBuilder normalized = new global::System.Text.StringBuilder();
        bool capitalize = false;
        int index;
        for (index = 0; index < source.Length; index += 1)
        {
            char character = source[index];
            if (character == '-' || character == '_' || character == ' ' || character == '.')
            {
                capitalize = normalized.Length > 0;
            }
            else if (capitalize)
            {
                normalized.Append(global::System.Char.ToUpperInvariant(character));
                capitalize = false;
            }
            else
            {
                normalized.Append(character);
            }
        }

        if (normalized.Length > 0)
        {
            normalized[0] = global::System.Char.ToLowerInvariant(normalized[0]);
        }

        return normalized.ToString();
    }

    internal static ParseException ParseFailure(string message, DocumentFormat format, string? sourceName, string path)
    {
        return new ParseException(message, format, sourceName, path, 0L, false, 0, false, null);
    }
}
