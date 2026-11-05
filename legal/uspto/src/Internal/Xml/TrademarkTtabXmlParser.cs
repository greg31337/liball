namespace uspto;

internal class TrademarkTtabAddress
{
    internal string? Identifier = null;
    internal string? TypeCode = null;
    internal string? Name = null;
    internal string? OrganizationName = null;
    internal TrademarkAddress Address = new TrademarkAddress();
    internal global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

internal class TrademarkTtabProperty
{
    internal string? Identifier = null;
    internal string? SerialNumber = null;
    internal string? RegistrationNumber = null;
    internal string? MarkText = null;
    internal global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

internal class TrademarkTtabParty
{
    internal string? Identifier = null;
    internal string? RoleCode = null;
    internal string? Name = null;
    internal string? OrganizationName = null;
    internal TrademarkTtabProperty[] Properties = new TrademarkTtabProperty[0];
    internal TrademarkTtabAddress[] Addresses = new TrademarkTtabAddress[0];
    internal global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

internal class TrademarkTtabDocumentReference
{
    internal string? Identifier = null;
    internal int EntryNumber = 0;
    internal bool HasEntryNumber = false;
    internal string? Name = null;
    internal string? Title = null;
    internal string? Category = null;
    internal string? MediaType = null;
    internal string? UriText = null;
    internal global::System.Uri? Uri = null;
    internal string? FilingDateText = null;
    internal global::System.DateTimeOffset FilingDate = default(global::System.DateTimeOffset);
    internal bool HasFilingDate = false;
    internal global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

internal class TrademarkTtabDecision
{
    internal string? Identifier = null;
    internal string? TypeCode = null;
    internal string? Outcome = null;
    internal string? Text = null;
    internal string? IssueDateText = null;
    internal global::System.DateTimeOffset IssueDate = default(global::System.DateTimeOffset);
    internal bool HasIssueDate = false;
    internal TrademarkTtabDocumentReference[] Documents = new TrademarkTtabDocumentReference[0];
    internal global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

internal class TrademarkTtabEvent
{
    internal string? Identifier = null;
    internal string? Code = null;
    internal string? TypeCode = null;
    internal string? Text = null;
    internal string? DueDateText = null;
    internal global::System.DateTimeOffset DueDate = default(global::System.DateTimeOffset);
    internal bool HasDueDate = false;
    internal string? EventDateText = null;
    internal global::System.DateTimeOffset EventDate = default(global::System.DateTimeOffset);
    internal bool HasEventDate = false;
    internal TrademarkTtabDocumentReference[] Documents = new TrademarkTtabDocumentReference[0];
    internal global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

internal class TrademarkTtabProceeding
{
    internal string? Number = null;
    internal string? TypeCode = null;
    internal string? StatusCode = null;
    internal string? FilingDateText = null;
    internal global::System.DateTimeOffset FilingDate = default(global::System.DateTimeOffset);
    internal bool HasFilingDate = false;
    internal string? StatusUpdateDateText = null;
    internal global::System.DateTimeOffset StatusUpdateDate = default(global::System.DateTimeOffset);
    internal bool HasStatusUpdateDate = false;
    internal string? EmployeeNumber = null;
    internal string? InterlocutoryAttorneyName = null;
    internal string? LocationCode = null;
    internal string? DayInLocation = null;
    internal string? ChargeToLocationCode = null;
    internal string? ChargeToEmployeeName = null;
    internal TrademarkTtabParty[] Parties = new TrademarkTtabParty[0];
    internal TrademarkTtabEvent[] Events = new TrademarkTtabEvent[0];
    internal TrademarkTtabDecision[] Decisions = new TrademarkTtabDecision[0];
    internal TrademarkTtabDocumentReference[] Documents = new TrademarkTtabDocumentReference[0];
    internal global::System.Collections.Generic.Dictionary<string, string> UnknownFields = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
}

internal class TrademarkTtabXmlRecord
{
    internal long RecordNumber = 0L;
    internal string SourcePath = "/";
    internal string RootName = string.Empty;
    internal bool IsTyped = false;
    internal string? SchemaVersion = null;
    internal string? ActionKeyCode = null;
    internal string? TransactionDateText = null;
    internal global::System.DateTimeOffset TransactionDate = default(global::System.DateTimeOffset);
    internal bool HasTransactionDate = false;
    internal TrademarkTtabProceeding? Proceeding = null;
    internal string RawXml = string.Empty;
    internal global::System.Collections.Generic.Dictionary<string, string> RawValues = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.Ordinal);
    internal PatentDataProvenance Provenance = new PatentDataProvenance();
}

internal class TrademarkTtabXmlBatch
{
    internal TrademarkTtabXmlRecord[] Records = new TrademarkTtabXmlRecord[0];
    internal global::System.Collections.Generic.Dictionary<string, TrademarkTtabXmlRecord> RecordsByProceedingNumber = new global::System.Collections.Generic.Dictionary<string, TrademarkTtabXmlRecord>(global::System.StringComparer.Ordinal);
    internal int Count = 0;
    internal int TypedCount = 0;
    internal int GenericCount = 0;
}

internal static class TrademarkTtabXmlParser
{
    internal static TrademarkTtabXmlRecord ParseRecord(XmlRecord record, PatentDataProvenance provenance)
    {
        ValidateTtabInput(record, provenance);
        global::System.Xml.XmlElement root = LoadTtabRoot(record.RawXml);
        global::System.Xml.XmlElement? proceedingElement = ResolveTtabProceeding(root);
        PatentDataProvenance copied = OdpJsonParser.CopyProvenance(provenance);
        copied.Source = DataProvenance.Bulk;
        copied.IsAuthoritative = true;

        TrademarkTtabXmlRecord result = new TrademarkTtabXmlRecord();
        result.RecordNumber = record.RecordNumber;
        result.SourcePath = record.Path;
        result.RootName = root.LocalName;
        result.RawXml = record.RawXml;
        result.Provenance = copied;
        result.SchemaVersion = ReadTtabSchemaVersion(root);
        result.ActionKeyCode = ReadTtabMetadata(root, new string[] { "action-key-code", "actionKeyCode", "extract-type" });
        result.TransactionDateText = ReadTtabMetadata(root, new string[] { "transaction-date", "transactionDate", "extract-date" });
        TsdrDateValue transactionDate = TsdrXmlParser.ParseDate(result.TransactionDateText);
        result.TransactionDate = transactionDate.Value;
        result.HasTransactionDate = transactionDate.HasValue;
        CaptureTtabRawValues(root, "/" + root.LocalName, result.RawValues);

        if (proceedingElement != null)
        {
            result.Proceeding = ParseTtabProceeding(proceedingElement, record);
            result.IsTyped = true;
        }

        return result;
    }

    internal static TrademarkTtabXmlBatch FlattenRecords(XmlRecord[] records, PatentDataProvenance provenance)
    {
        if (records == null)
        {
            throw new ValidationException("Trademark TTAB XML records are required.", "records", "REQUIRED", null);
        }

        if (provenance == null)
        {
            throw new ValidationException("Trademark TTAB XML provenance is required.", "provenance", "REQUIRED", null);
        }

        TrademarkTtabXmlBatch result = new TrademarkTtabXmlBatch();
        result.Records = new TrademarkTtabXmlRecord[records.Length];
        int index;
        for (index = 0; index < records.Length; index += 1)
        {
            TrademarkTtabXmlRecord parsed = ParseRecord(records[index], provenance);
            result.Records[index] = parsed;
            if (parsed.IsTyped && parsed.Proceeding != null)
            {
                string number = parsed.Proceeding.Number ?? string.Empty;
                if (result.RecordsByProceedingNumber.ContainsKey(number))
                {
                    throw TtabFailure("The trademark TTAB XML batch contains a duplicate proceeding number.", records[index].Path, records[index]);
                }

                result.RecordsByProceedingNumber.Add(number, parsed);
                result.TypedCount += 1;
            }
            else
            {
                result.GenericCount += 1;
            }
        }

        result.Count = result.Records.Length;
        return result;
    }

    internal static void ValidateTtabInput(XmlRecord record, PatentDataProvenance provenance)
    {
        if (record == null)
        {
            throw new ValidationException("A trademark TTAB XML record is required.", "record", "REQUIRED", null);
        }

        if (provenance == null)
        {
            throw new ValidationException("Trademark TTAB XML provenance is required.", "provenance", "REQUIRED", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(record.RawXml))
        {
            throw new ValidationException("The trademark TTAB XML record must retain its raw subtree.", "RawXml", "REQUIRED", null);
        }
    }

    internal static global::System.Xml.XmlElement LoadTtabRoot(string xml)
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
            throw new ParseException("The trademark TTAB XML record has no root element.", DocumentFormat.Xml, "trademark TTAB XML", "/", 0L, false, 0, false, null);
        }

        return document.DocumentElement;
    }

    internal static global::System.Xml.XmlElement? ResolveTtabProceeding(global::System.Xml.XmlElement root)
    {
        if (TtabNameMatches(root, new string[] { "proceeding-entry", "proceeding", "ttab-proceeding", "case" }))
        {
            return root;
        }

        if (TtabNameMatches(root, new string[] { "ttab-proceedings", "proceedings", "proceeding-information", "ttab-feed", "transaction" }))
        {
            return FindTtabFirstDescendant(root, new string[] { "proceeding-entry", "proceeding", "ttab-proceeding", "case" });
        }

        return null;
    }

    internal static TrademarkTtabProceeding ParseTtabProceeding(global::System.Xml.XmlElement element, XmlRecord record)
    {
        TrademarkTtabProceeding result = new TrademarkTtabProceeding();
        result.Number = ReadTtabText(element, new string[] { "number", "proceeding-number", "proceedingNumber", "case-number" });
        if (global::System.String.IsNullOrWhiteSpace(result.Number))
        {
            throw TtabFailure("The trademark TTAB proceeding number is missing.", record.Path + "/number", record);
        }

        result.TypeCode = ReadTtabText(element, new string[] { "type-code", "proceeding-type-code", "proceedingTypeCode", "type" });
        result.StatusCode = ReadTtabText(element, new string[] { "status-code", "proceeding-status-code", "statusCode", "status" });
        result.EmployeeNumber = ReadTtabText(element, new string[] { "employee-number", "interlocutory-attorney-number", "employeeNumber" });
        result.InterlocutoryAttorneyName = ReadTtabText(element, new string[] { "interlocutory-attorney-name", "interlocutoryAttorneyName", "attorney-name" });
        result.LocationCode = ReadTtabText(element, new string[] { "location-code", "locationCode" });
        result.DayInLocation = ReadTtabText(element, new string[] { "day-in-location", "date-in-location", "dayInLocation" });
        result.ChargeToLocationCode = ReadTtabText(element, new string[] { "charge-to-location-code", "chargeToLocationCode" });
        result.ChargeToEmployeeName = ReadTtabText(element, new string[] { "charge-to-employee-name", "chargeToEmployeeName" });
        ApplyTtabProceedingDate(result, ReadTtabText(element, new string[] { "filing-date", "proceeding-filing-date", "filingDate" }), 1);
        ApplyTtabProceedingDate(result, ReadTtabText(element, new string[] { "status-update-date", "status-date", "statusUpdateDate" }), 2);
        result.Parties = ParseTtabParties(element);
        result.Events = ParseTtabEvents(element);
        result.Decisions = ParseTtabDecisions(element);
        result.Documents = ParseTtabDocuments(element, new string[] { "document-information", "document-references", "documents", "docket-entries" });
        CaptureTtabUnknown(element, result.UnknownFields, new string[]
        {
            "number", "proceeding-number", "proceedingNumber", "case-number", "type-code", "proceeding-type-code", "proceedingTypeCode", "type",
            "filing-date", "proceeding-filing-date", "filingDate", "employee-number", "interlocutory-attorney-number", "employeeNumber",
            "interlocutory-attorney-name", "interlocutoryAttorneyName", "attorney-name", "location-code", "locationCode", "day-in-location", "date-in-location", "dayInLocation",
            "charge-to-location-code", "chargeToLocationCode", "charge-to-employee-name", "chargeToEmployeeName", "status-update-date", "status-date", "statusUpdateDate",
            "status-code", "proceeding-status-code", "statusCode", "status", "party-information", "parties", "party-bag", "prosecution-history", "events", "event-history",
            "decision-information", "decisions", "decision-history", "document-information", "document-references", "documents", "docket-entries"
        });
        return result;
    }

    internal static void ApplyTtabProceedingDate(TrademarkTtabProceeding target, string? text, int field)
    {
        TsdrDateValue parsed = TsdrXmlParser.ParseDate(text);
        if (field == 1)
        {
            target.FilingDateText = parsed.Text;
            target.FilingDate = parsed.Value;
            target.HasFilingDate = parsed.HasValue;
        }
        else
        {
            target.StatusUpdateDateText = parsed.Text;
            target.StatusUpdateDate = parsed.Value;
            target.HasStatusUpdateDate = parsed.HasValue;
        }
    }

    internal static TrademarkTtabParty[] ParseTtabParties(global::System.Xml.XmlElement proceeding)
    {
        global::System.Xml.XmlElement[] elements = FindTtabCollection(proceeding, new string[] { "party-information", "parties", "party-bag" }, new string[] { "party", "proceeding-party" });
        TrademarkTtabParty[] result = new TrademarkTtabParty[elements.Length];
        int index;
        for (index = 0; index < elements.Length; index += 1)
        {
            global::System.Xml.XmlElement element = elements[index];
            TrademarkTtabParty party = new TrademarkTtabParty();
            party.Identifier = ReadTtabText(element, new string[] { "identifier", "party-identifier", "partyIdentifier" });
            party.RoleCode = ReadTtabText(element, new string[] { "role-code", "party-role-code", "roleCode", "role" });
            party.Name = ReadTtabText(element, new string[] { "name", "party-name", "partyName" });
            party.OrganizationName = ReadTtabText(element, new string[] { "orgname", "organization-name", "organizationName", "company-name" });
            party.Properties = ParseTtabProperties(element);
            party.Addresses = ParseTtabAddresses(element);
            CaptureTtabUnknown(element, party.UnknownFields, new string[]
            {
                "identifier", "party-identifier", "partyIdentifier", "role-code", "party-role-code", "roleCode", "role", "name", "party-name", "partyName",
                "orgname", "organization-name", "organizationName", "company-name", "property-information", "properties", "property-bag", "address-information", "addresses", "address-bag"
            });
            result[index] = party;
        }

        return result;
    }

    internal static TrademarkTtabProperty[] ParseTtabProperties(global::System.Xml.XmlElement party)
    {
        global::System.Xml.XmlElement[] elements = FindTtabCollection(party, new string[] { "property-information", "properties", "property-bag" }, new string[] { "property", "trademark-property" });
        TrademarkTtabProperty[] result = new TrademarkTtabProperty[elements.Length];
        int index;
        for (index = 0; index < elements.Length; index += 1)
        {
            global::System.Xml.XmlElement element = elements[index];
            TrademarkTtabProperty property = new TrademarkTtabProperty();
            property.Identifier = ReadTtabText(element, new string[] { "identifier", "property-identifier", "propertyIdentifier" });
            property.SerialNumber = ReadTtabText(element, new string[] { "serial-number", "trademark-serial-number", "serialNumber" });
            property.RegistrationNumber = ReadTtabText(element, new string[] { "registration-number", "trademark-registration-number", "registrationNumber" });
            property.MarkText = ReadTtabText(element, new string[] { "mark-text", "mark", "markText", "word-mark" });
            CaptureTtabUnknown(element, property.UnknownFields, new string[] { "identifier", "property-identifier", "propertyIdentifier", "serial-number", "trademark-serial-number", "serialNumber", "registration-number", "trademark-registration-number", "registrationNumber", "mark-text", "mark", "markText", "word-mark" });
            result[index] = property;
        }

        return result;
    }

    internal static TrademarkTtabAddress[] ParseTtabAddresses(global::System.Xml.XmlElement party)
    {
        global::System.Xml.XmlElement[] elements = FindTtabCollection(party, new string[] { "address-information", "addresses", "address-bag" }, new string[] { "proceeding-address", "address", "correspondence-address" });
        TrademarkTtabAddress[] result = new TrademarkTtabAddress[elements.Length];
        int index;
        for (index = 0; index < elements.Length; index += 1)
        {
            global::System.Xml.XmlElement element = elements[index];
            TrademarkTtabAddress address = new TrademarkTtabAddress();
            address.Identifier = ReadTtabText(element, new string[] { "identifier", "address-identifier", "addressIdentifier" });
            address.TypeCode = ReadTtabText(element, new string[] { "type-code", "address-type-code", "typeCode" });
            address.Name = ReadTtabText(element, new string[] { "name", "correspondent-name", "contact-name" });
            address.OrganizationName = ReadTtabText(element, new string[] { "orgname", "organization-name", "company-name" });
            address.Address.Line1 = ReadTtabText(element, new string[] { "address-1", "address-line-one", "address1", "street" });
            address.Address.Line2 = ReadTtabText(element, new string[] { "address-2", "address-line-two", "address2" });
            address.Address.City = ReadTtabText(element, new string[] { "city" });
            address.Address.StateOrProvince = ReadTtabText(element, new string[] { "state", "state-or-province", "region" });
            address.Address.PostalCode = ReadTtabText(element, new string[] { "postcode", "postal-code", "zip" });
            address.Address.CountryCode = ReadTtabText(element, new string[] { "country", "country-code" });
            CaptureTtabUnknown(element, address.UnknownFields, new string[] { "identifier", "address-identifier", "addressIdentifier", "type-code", "address-type-code", "typeCode", "name", "correspondent-name", "contact-name", "orgname", "organization-name", "company-name", "address-1", "address-line-one", "address1", "street", "address-2", "address-line-two", "address2", "city", "state", "state-or-province", "region", "postcode", "postal-code", "zip", "country", "country-code" });
            result[index] = address;
        }

        return result;
    }

    internal static TrademarkTtabEvent[] ParseTtabEvents(global::System.Xml.XmlElement proceeding)
    {
        global::System.Xml.XmlElement[] elements = FindTtabCollection(proceeding, new string[] { "prosecution-history", "events", "event-history" }, new string[] { "prosecution-entry", "event", "history-entry" });
        TrademarkTtabEvent[] result = new TrademarkTtabEvent[elements.Length];
        int index;
        for (index = 0; index < elements.Length; index += 1)
        {
            global::System.Xml.XmlElement element = elements[index];
            TrademarkTtabEvent item = new TrademarkTtabEvent();
            item.Identifier = ReadTtabText(element, new string[] { "identifier", "entry-number", "event-identifier", "eventIdentifier" });
            item.Code = ReadTtabText(element, new string[] { "code", "entry-code", "event-code", "eventCode" });
            item.TypeCode = ReadTtabText(element, new string[] { "type-code", "entry-type-code", "event-type-code", "typeCode" });
            item.Text = ReadTtabText(element, new string[] { "history-text", "event-text", "description", "title" });
            ApplyTtabEventDate(item, ReadTtabText(element, new string[] { "due-date", "response-due-date", "dueDate" }), 1);
            ApplyTtabEventDate(item, ReadTtabText(element, new string[] { "date", "event-date", "entry-date", "eventDate" }), 2);
            item.Documents = ParseTtabDocuments(element, new string[] { "document-information", "document-references", "documents", "attachments" });
            if (item.Documents.Length == 0 && HasTtabDocumentFields(element))
            {
                TrademarkTtabDocumentReference direct = ParseTtabDocument(element);
                direct.Identifier = ReadTtabText(element, new string[] { "document-identifier", "document-id", "documentIdentifier" });
                item.Documents = new TrademarkTtabDocumentReference[] { direct };
            }

            CaptureTtabUnknown(element, item.UnknownFields, new string[] { "identifier", "entry-number", "event-identifier", "eventIdentifier", "code", "entry-code", "event-code", "eventCode", "type-code", "entry-type-code", "event-type-code", "typeCode", "due-date", "response-due-date", "dueDate", "date", "event-date", "entry-date", "eventDate", "history-text", "event-text", "description", "title", "document-information", "document-references", "documents", "attachments", "document-identifier", "document-id", "document-number", "document-name", "document-title", "document-category", "media-type", "mime-type", "document-uri", "document-url", "download-uri", "filing-date", "document-date" });
            result[index] = item;
        }

        return result;
    }

    internal static void ApplyTtabEventDate(TrademarkTtabEvent target, string? text, int field)
    {
        TsdrDateValue parsed = TsdrXmlParser.ParseDate(text);
        if (field == 1)
        {
            target.DueDateText = parsed.Text;
            target.DueDate = parsed.Value;
            target.HasDueDate = parsed.HasValue;
        }
        else
        {
            target.EventDateText = parsed.Text;
            target.EventDate = parsed.Value;
            target.HasEventDate = parsed.HasValue;
        }
    }

    internal static TrademarkTtabDecision[] ParseTtabDecisions(global::System.Xml.XmlElement proceeding)
    {
        global::System.Xml.XmlElement[] elements = FindTtabCollection(proceeding, new string[] { "decision-information", "decisions", "decision-history" }, new string[] { "decision", "board-decision", "decision-entry" });
        TrademarkTtabDecision[] result = new TrademarkTtabDecision[elements.Length];
        int index;
        for (index = 0; index < elements.Length; index += 1)
        {
            global::System.Xml.XmlElement element = elements[index];
            TrademarkTtabDecision item = new TrademarkTtabDecision();
            item.Identifier = ReadTtabText(element, new string[] { "identifier", "decision-identifier", "decisionIdentifier" });
            item.TypeCode = ReadTtabText(element, new string[] { "type-code", "decision-type-code", "decisionTypeCode", "type" });
            item.Outcome = ReadTtabText(element, new string[] { "outcome", "decision-outcome", "disposition", "result" });
            item.Text = ReadTtabText(element, new string[] { "decision-text", "text", "description", "title" });
            item.IssueDateText = ReadTtabText(element, new string[] { "issue-date", "decision-date", "date", "issueDate" });
            TsdrDateValue issueDate = TsdrXmlParser.ParseDate(item.IssueDateText);
            item.IssueDate = issueDate.Value;
            item.HasIssueDate = issueDate.HasValue;
            item.Documents = ParseTtabDocuments(element, new string[] { "document-information", "document-references", "documents", "attachments" });
            if (item.Documents.Length == 0 && HasTtabDocumentFields(element))
            {
                TrademarkTtabDocumentReference direct = ParseTtabDocument(element);
                direct.Identifier = ReadTtabText(element, new string[] { "document-identifier", "document-id", "documentIdentifier" });
                item.Documents = new TrademarkTtabDocumentReference[] { direct };
            }

            CaptureTtabUnknown(element, item.UnknownFields, new string[] { "identifier", "decision-identifier", "decisionIdentifier", "type-code", "decision-type-code", "decisionTypeCode", "type", "outcome", "decision-outcome", "disposition", "result", "decision-text", "text", "description", "title", "issue-date", "decision-date", "date", "issueDate", "document-information", "document-references", "documents", "attachments", "document-identifier", "document-id", "document-number", "document-name", "document-title", "document-category", "media-type", "mime-type", "document-uri", "document-url", "download-uri", "filing-date", "document-date" });
            result[index] = item;
        }

        return result;
    }

    internal static TrademarkTtabDocumentReference[] ParseTtabDocuments(global::System.Xml.XmlElement parent, string[] wrapperNames)
    {
        global::System.Xml.XmlElement[] elements = FindTtabCollection(parent, wrapperNames, new string[] { "document", "document-reference", "docket-entry", "attachment" });
        TrademarkTtabDocumentReference[] result = new TrademarkTtabDocumentReference[elements.Length];
        int index;
        for (index = 0; index < elements.Length; index += 1)
        {
            result[index] = ParseTtabDocument(elements[index]);
        }

        return result;
    }

    internal static TrademarkTtabDocumentReference ParseTtabDocument(global::System.Xml.XmlElement element)
    {
        TrademarkTtabDocumentReference result = new TrademarkTtabDocumentReference();
        result.Identifier = ReadTtabText(element, new string[] { "document-identifier", "document-id", "identifier", "documentIdentifier" });
        string? numberText = ReadTtabText(element, new string[] { "document-number", "entry-number", "sequence-number", "documentNumber" });
        if (numberText != null)
        {
            int parsedNumber = PatentAssignmentXmlParser.ParseNonNegativeInteger(numberText);
            if (parsedNumber >= 0)
            {
                result.EntryNumber = parsedNumber;
                result.HasEntryNumber = true;
            }
        }

        result.Name = ReadTtabText(element, new string[] { "document-name", "name", "file-name", "documentName" });
        result.Title = ReadTtabText(element, new string[] { "document-title", "title", "description", "documentTitle" });
        result.Category = ReadTtabText(element, new string[] { "document-category", "category", "type-code", "documentType" });
        result.MediaType = ReadTtabText(element, new string[] { "media-type", "mime-type", "content-type", "mediaType" });
        result.UriText = ReadTtabText(element, new string[] { "document-uri", "document-url", "download-uri", "url", "uri" });
        result.Uri = TsdrJsonParser.ParseUri(result.UriText);
        result.FilingDateText = ReadTtabText(element, new string[] { "filing-date", "document-date", "date", "filingDate" });
        TsdrDateValue filingDate = TsdrXmlParser.ParseDate(result.FilingDateText);
        result.FilingDate = filingDate.Value;
        result.HasFilingDate = filingDate.HasValue;
        CaptureTtabUnknown(element, result.UnknownFields, new string[] { "document-identifier", "document-id", "identifier", "documentIdentifier", "document-number", "entry-number", "sequence-number", "documentNumber", "document-name", "name", "file-name", "documentName", "document-title", "title", "description", "documentTitle", "document-category", "category", "type-code", "documentType", "media-type", "mime-type", "content-type", "mediaType", "document-uri", "document-url", "download-uri", "url", "uri", "filing-date", "document-date", "date", "filingDate" });
        return result;
    }

    internal static bool HasTtabDocumentFields(global::System.Xml.XmlElement element)
    {
        return FindTtabDirectChild(element, new string[] { "document-identifier", "document-id", "document-number", "document-name", "document-title", "document-uri", "document-url", "download-uri" }) != null;
    }

    internal static string? ReadTtabSchemaVersion(global::System.Xml.XmlElement root)
    {
        int index;
        for (index = 0; index < root.Attributes.Count; index += 1)
        {
            global::System.Xml.XmlAttribute attribute = root.Attributes[index];
            if (TtabTextNameMatches(attribute.LocalName, new string[] { "version", "schema-version", "dtd-version" }))
            {
                string value = attribute.Value.Trim();
                if (value.Length != 0)
                {
                    return value;
                }
            }
        }

        return ReadTtabMetadata(root, new string[] { "version-no", "version-number", "schema-version", "dtd-version" });
    }

    internal static string? ReadTtabMetadata(global::System.Xml.XmlElement root, string[] names)
    {
        global::System.Xml.XmlElement? element = FindTtabFirstDescendant(root, names);
        if (element != null)
        {
            string value = element.InnerText.Trim();
            if (value.Length != 0)
            {
                return value;
            }
        }

        int index;
        for (index = 0; index < root.Attributes.Count; index += 1)
        {
            global::System.Xml.XmlAttribute attribute = root.Attributes[index];
            if (TtabTextNameMatches(attribute.LocalName, names))
            {
                string value = attribute.Value.Trim();
                if (value.Length != 0)
                {
                    return value;
                }
            }
        }

        return null;
    }

    internal static string? ReadTtabText(global::System.Xml.XmlElement element, string[] names)
    {
        global::System.Xml.XmlElement? child = FindTtabDirectChild(element, names);
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

    internal static global::System.Xml.XmlElement[] FindTtabCollection(global::System.Xml.XmlElement parent, string[] wrapperNames, string[] itemNames)
    {
        global::System.Collections.Generic.List<global::System.Xml.XmlElement> result = new global::System.Collections.Generic.List<global::System.Xml.XmlElement>();
        foreach (global::System.Xml.XmlNode node in parent.ChildNodes)
        {
            if (node.NodeType != global::System.Xml.XmlNodeType.Element)
            {
                continue;
            }

            global::System.Xml.XmlElement element = (global::System.Xml.XmlElement)node;
            if (TtabNameMatches(element, itemNames))
            {
                result.Add(element);
            }
            else if (TtabNameMatches(element, wrapperNames))
            {
                foreach (global::System.Xml.XmlNode childNode in element.ChildNodes)
                {
                    if (childNode.NodeType == global::System.Xml.XmlNodeType.Element && TtabNameMatches((global::System.Xml.XmlElement)childNode, itemNames))
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

    internal static global::System.Xml.XmlElement? FindTtabDirectChild(global::System.Xml.XmlElement element, string[] names)
    {
        foreach (global::System.Xml.XmlNode node in element.ChildNodes)
        {
            if (node.NodeType == global::System.Xml.XmlNodeType.Element && TtabNameMatches((global::System.Xml.XmlElement)node, names))
            {
                return (global::System.Xml.XmlElement)node;
            }
        }

        return null;
    }

    internal static global::System.Xml.XmlElement? FindTtabFirstDescendant(global::System.Xml.XmlElement element, string[] names)
    {
        foreach (global::System.Xml.XmlNode node in element.ChildNodes)
        {
            if (node.NodeType != global::System.Xml.XmlNodeType.Element)
            {
                continue;
            }

            global::System.Xml.XmlElement child = (global::System.Xml.XmlElement)node;
            if (TtabNameMatches(child, names))
            {
                return child;
            }

            global::System.Xml.XmlElement? nested = FindTtabFirstDescendant(child, names);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    internal static bool TtabNameMatches(global::System.Xml.XmlElement element, string[] names)
    {
        return TtabTextNameMatches(element.LocalName, names);
    }

    internal static bool TtabTextNameMatches(string value, string[] names)
    {
        string normalized = NormalizeTtabName(value);
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            if (normalized.Equals(NormalizeTtabName(names[index]), global::System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    internal static string NormalizeTtabName(string value)
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

    internal static void CaptureTtabUnknown(global::System.Xml.XmlElement element, global::System.Collections.Generic.Dictionary<string, string> destination, string[] knownNames)
    {
        foreach (global::System.Xml.XmlNode node in element.ChildNodes)
        {
            if (node.NodeType != global::System.Xml.XmlNodeType.Element)
            {
                continue;
            }

            global::System.Xml.XmlElement child = (global::System.Xml.XmlElement)node;
            if (!TtabNameMatches(child, knownNames))
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

    internal static void CaptureTtabRawValues(global::System.Xml.XmlElement element, string path, global::System.Collections.Generic.Dictionary<string, string> destination)
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

            CaptureTtabRawValues(child, childPath, destination);
        }
    }

    internal static ParseException TtabFailure(string message, string path, XmlRecord record)
    {
        return new ParseException(message, DocumentFormat.Xml, "trademark TTAB XML", path, 0L, false, record.StartLineNumber, record.HasLineInfo, null);
    }
}
