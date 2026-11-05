namespace uspto;

internal static class TsdrTransactionParser
{
    internal static TrademarkLastUpdateResult ParseLastUpdateJson(string json, string requestedIdentifier, TrademarkIdentifierResult expectedIdentifier, HttpResponseMetadata metadata, string? rawResponse)
    {
        ValidateLastUpdateInputs(json, expectedIdentifier, metadata);
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            global::System.Text.Json.JsonElement root = document.RootElement;
            if (root.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw ParseFailure("The TSDR last-update JSON root must be an object.", "$", DocumentFormat.Json);
            }

            TrademarkLastUpdateResult result = CreateLastUpdateResult(requestedIdentifier, expectedIdentifier, metadata, DocumentFormat.Json, rawResponse);
            TsdrJsonElementValue updates = TsdrJsonParser.Find(root, new string[] { "caseUpdateInfo", "CaseUpdateInfo" }, "$");
            global::System.Collections.Generic.List<TrademarkCaseUpdateInfo> parsed = new global::System.Collections.Generic.List<TrademarkCaseUpdateInfo>();
            if (updates.IsPresent)
            {
                if (updates.Element.ValueKind != global::System.Text.Json.JsonValueKind.Array)
                {
                    throw ParseFailure("The TSDR last-update entries must be an array.", updates.Path, DocumentFormat.Json);
                }

                int index = 0;
                foreach (global::System.Text.Json.JsonElement item in updates.Element.EnumerateArray())
                {
                    if (item.ValueKind != global::System.Text.Json.JsonValueKind.Object)
                    {
                        throw ParseFailure("A TSDR last-update entry must be an object.", updates.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]", DocumentFormat.Json);
                    }

                    parsed.Add(ParseJsonUpdate(item, updates.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]"));
                    index += 1;
                }
            }

            result.Updates = parsed.ToArray();
            TsdrJsonParser.CaptureUnknown(root, new string[] { "caseUpdateInfo", "CaseUpdateInfo" }, result.UnknownFields);
            result.IsComplete = true;
            return result;
        }
    }

    internal static TrademarkLastUpdateResult ParseLastUpdateXml(string xml, string requestedIdentifier, TrademarkIdentifierResult expectedIdentifier, HttpResponseMetadata metadata, string? rawResponse, UsptoLimits limits)
    {
        ValidateLastUpdateInputs(xml, expectedIdentifier, metadata);
        byte[] bytes = global::System.Text.Encoding.UTF8.GetBytes(xml);
        using (global::System.IO.MemoryStream input = new global::System.IO.MemoryStream(bytes, false))
        {
            TsdrXmlElement root = TsdrXmlParser.ReadTree(input, limits);
            TrademarkLastUpdateResult result = CreateLastUpdateResult(requestedIdentifier, expectedIdentifier, metadata, DocumentFormat.Xml, rawResponse);
            global::System.Collections.Generic.List<TsdrXmlElement> elements = new global::System.Collections.Generic.List<TsdrXmlElement>();
            if (TsdrXmlParser.NameMatches(root.Name, new string[] { "CaseUpdateInfo" }))
            {
                elements.Add(root);
            }
            else
            {
                TsdrXmlParser.Collect(root, new string[] { "CaseUpdateInfo" }, elements);
            }

            global::System.Collections.Generic.List<TrademarkCaseUpdateInfo> updates = new global::System.Collections.Generic.List<TrademarkCaseUpdateInfo>();
            foreach (TsdrXmlElement element in elements)
            {
                updates.Add(ParseXmlUpdate(element));
            }

            result.Updates = updates.ToArray();
            TsdrDocumentXmlParser.CaptureUnknown(root, new string[] { "CaseUpdateInfo" }, result.UnknownFields);
            result.IsComplete = true;
            return result;
        }
    }

    internal static TrademarkMultiStatusResult ParseMultiStatusJson(string json, string[] requestedIdentifiers, TrademarkIdentifierResult[] expectedIdentifiers, HttpResponseMetadata metadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("A TSDR multi-status JSON response is required.", "json", "REQUIRED", null);
        }

        if (requestedIdentifiers == null || expectedIdentifiers == null || requestedIdentifiers.Length != expectedIdentifiers.Length || expectedIdentifiers.Length == 0)
        {
            throw new ValidationException("Matching multi-status request identifiers are required.", "expectedIdentifiers", "INVALID_IDENTIFIERS", null);
        }

        if (metadata == null)
        {
            throw new ValidationException("TSDR multi-status response metadata is required.", "metadata", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            global::System.Text.Json.JsonElement root = document.RootElement;
            if (root.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw ParseFailure("The TSDR multi-status JSON root must be an object.", "$", DocumentFormat.Json);
            }

            TrademarkMultiStatusResult result = new TrademarkMultiStatusResult();
            result.RequestedIdentifiers = CopyStrings(requestedIdentifiers);
            result.CanonicalIdentifiers = CanonicalIdentifiers(expectedIdentifiers);
            result.IdentifierKind = expectedIdentifiers[0].Kind;
            result.MissedIdentifiers = TsdrJsonParser.ReadStringArray(root, new string[] { "missedElements", "MissedElements" }, "$", false);
            ApplyMultiMetadata(result, root);
            result.Provenance = TsdrDocumentXmlParser.CreateProvenance(metadata, "TSDR multi-case status");
            result.RawResponse = rawResponse;

            global::System.Collections.Generic.List<int> available = AvailableIdentifierIndexes(requestedIdentifiers, expectedIdentifiers, result.MissedIdentifiers);
            TsdrJsonElementValue transactions = TsdrJsonParser.Find(root, new string[] { "transactionList", "TransactionList" }, "$");
            global::System.Collections.Generic.List<TrademarkCaseStatus> cases = new global::System.Collections.Generic.List<TrademarkCaseStatus>();
            if (transactions.IsPresent)
            {
                if (transactions.Element.ValueKind != global::System.Text.Json.JsonValueKind.Array)
                {
                    throw ParseFailure("The TSDR transaction list must be an array.", transactions.Path, DocumentFormat.Json);
                }

                if (transactions.Element.GetArrayLength() > available.Count)
                {
                    throw ParseFailure("The TSDR transaction list exceeds the requested case count.", transactions.Path, DocumentFormat.Json);
                }

                int transactionIndex = 0;
                foreach (global::System.Text.Json.JsonElement transaction in transactions.Element.EnumerateArray())
                {
                    int expectedIndex = available[transactionIndex];
                    TrademarkCaseStatus parsed = TsdrJsonParser.ParseStatus(transaction.GetRawText(), requestedIdentifiers[expectedIndex], expectedIdentifiers[expectedIndex], metadata, null);
                    parsed.Provenance.DatasetIdentifier = "TSDR multi-case status";
                    cases.Add(parsed);
                    transactionIndex += 1;
                }
            }

            result.Cases = cases.ToArray();
            TsdrJsonParser.CaptureUnknown(root, new string[] { "transactionList", "TransactionList", "oversized", "Oversized", "missedElements", "MissedElements", "size", "Size" }, result.UnknownFields);
            result.IsComplete = true;
            return result;
        }
    }

    internal static TrademarkCaseUpdateInfo ParseJsonUpdate(global::System.Text.Json.JsonElement source, string path)
    {
        TrademarkCaseUpdateInfo result = new TrademarkCaseUpdateInfo();
        result.Name = TsdrJsonParser.ReadString(source, new string[] { "name", "Name" }, path, false);
        result.Value = TsdrJsonParser.ReadString(source, new string[] { "value", "Value" }, path, false);
        TsdrJsonElementValue caseData = TsdrJsonParser.Find(source, new string[] { "caseData", "CaseData" }, path);
        if (caseData.IsPresent)
        {
            if (caseData.Element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw ParseFailure("TSDR case update data must be an object.", caseData.Path, DocumentFormat.Json);
            }

            ApplyJsonCaseData(result, caseData.Element, caseData.Path);
        }

        TsdrJsonParser.CaptureUnknown(source, new string[] { "caseData", "CaseData", "name", "Name", "value", "Value" }, result.UnknownFields);
        return result;
    }

    internal static void ApplyJsonCaseData(TrademarkCaseUpdateInfo result, global::System.Text.Json.JsonElement caseData, string path)
    {
        ApplyUpdateDate(result, TsdrXmlParser.ParseDate(TsdrJsonParser.ReadString(caseData, new string[] { "lastModifiedDate" }, path, false)), 1);
        ApplyJsonSectionDate(result, caseData, path, new string[] { "caseStatusData" }, 2);
        ApplyJsonSectionDate(result, caseData, path, new string[] { "caseProsecutionData" }, 3);
        ApplyJsonSectionDate(result, caseData, path, new string[] { "caseDocsData" }, 4);
    }

    internal static void ApplyJsonSectionDate(TrademarkCaseUpdateInfo result, global::System.Text.Json.JsonElement caseData, string path, string[] names, int field)
    {
        TsdrJsonElementValue section = TsdrJsonParser.Find(caseData, names, path);
        if (section.IsPresent && section.Element.ValueKind == global::System.Text.Json.JsonValueKind.Object)
        {
            ApplyUpdateDate(result, TsdrXmlParser.ParseDate(TsdrJsonParser.ReadString(section.Element, new string[] { "lastModifiedDate" }, section.Path, false)), field);
        }
    }

    internal static TrademarkCaseUpdateInfo ParseXmlUpdate(TsdrXmlElement source)
    {
        TrademarkCaseUpdateInfo result = new TrademarkCaseUpdateInfo();
        result.Name = TsdrDocumentXmlParser.ReadDirectText(source, new string[] { "Name" });
        result.Value = TsdrDocumentXmlParser.ReadDirectText(source, new string[] { "Value" });
        TsdrXmlElement? caseData = TsdrDocumentXmlParser.FindDirect(source, new string[] { "CaseData" });
        if (caseData != null)
        {
            ApplyUpdateDate(result, TsdrXmlParser.ParseDate(TsdrDocumentXmlParser.ReadDirectText(caseData, new string[] { "LastModifiedDate" })), 1);
            ApplyXmlSectionDate(result, caseData, new string[] { "CaseStatusData" }, 2);
            ApplyXmlSectionDate(result, caseData, new string[] { "CaseProsecutionData" }, 3);
            ApplyXmlSectionDate(result, caseData, new string[] { "CaseDocsData" }, 4);
        }

        TsdrDocumentXmlParser.CaptureUnknown(source, new string[] { "CaseData", "Name", "Value" }, result.UnknownFields);
        return result;
    }

    internal static void ApplyXmlSectionDate(TrademarkCaseUpdateInfo result, TsdrXmlElement caseData, string[] names, int field)
    {
        TsdrXmlElement? section = TsdrDocumentXmlParser.FindDirect(caseData, names);
        if (section != null)
        {
            ApplyUpdateDate(result, TsdrXmlParser.ParseDate(TsdrDocumentXmlParser.ReadDirectText(section, new string[] { "LastModifiedDate" })), field);
        }
    }

    internal static void ApplyUpdateDate(TrademarkCaseUpdateInfo result, TsdrDateValue value, int field)
    {
        switch (field)
        {
            case 1:
                result.LastModifiedDateText = value.Text;
                result.LastModifiedDate = value.Value;
                result.HasLastModifiedDate = value.HasValue;
                break;
            case 2:
                result.StatusLastModifiedDateText = value.Text;
                result.StatusLastModifiedDate = value.Value;
                result.HasStatusLastModifiedDate = value.HasValue;
                break;
            case 3:
                result.ProsecutionLastModifiedDateText = value.Text;
                result.ProsecutionLastModifiedDate = value.Value;
                result.HasProsecutionLastModifiedDate = value.HasValue;
                break;
            case 4:
                result.DocumentsLastModifiedDateText = value.Text;
                result.DocumentsLastModifiedDate = value.Value;
                result.HasDocumentsLastModifiedDate = value.HasValue;
                break;
        }
    }

    internal static void ApplyMultiMetadata(TrademarkMultiStatusResult result, global::System.Text.Json.JsonElement root)
    {
        JsonFieldValue oversized = TsdrJsonParser.ReadBoolean(root, new string[] { "oversized", "Oversized" }, "$");
        if (!oversized.IsValid)
        {
            throw ParseFailure("The TSDR oversized indicator is invalid.", oversized.Path, DocumentFormat.Json);
        }

        if (oversized.IsPresent)
        {
            result.IsOversized = oversized.BooleanValue;
            result.HasIsOversized = true;
        }

        JsonFieldValue size = TsdrJsonParser.ReadInteger(root, new string[] { "size", "Size" }, "$");
        if (!size.IsValid)
        {
            throw ParseFailure("The TSDR transaction size is invalid.", size.Path, DocumentFormat.Json);
        }

        if (size.IsPresent)
        {
            result.Size = size.Int32Value;
            result.HasSize = true;
        }
    }

    internal static global::System.Collections.Generic.List<int> AvailableIdentifierIndexes(string[] requestedIdentifiers, TrademarkIdentifierResult[] expectedIdentifiers, string[] missedIdentifiers)
    {
        global::System.Collections.Generic.List<int> result = new global::System.Collections.Generic.List<int>();
        int index;
        for (index = 0; index < expectedIdentifiers.Length; index += 1)
        {
            bool missed = false;
            int missedIndex;
            for (missedIndex = 0; missedIndex < missedIdentifiers.Length; missedIndex += 1)
            {
                string value = missedIdentifiers[missedIndex];
                if (global::System.String.Equals(value, requestedIdentifiers[index], global::System.StringComparison.OrdinalIgnoreCase) || global::System.String.Equals(value, expectedIdentifiers[index].CanonicalNumber, global::System.StringComparison.OrdinalIgnoreCase) || global::System.String.Equals(value, expectedIdentifiers[index].IdentifierValue, global::System.StringComparison.OrdinalIgnoreCase))
                {
                    missed = true;
                }
            }

            if (!missed)
            {
                result.Add(index);
            }
        }

        return result;
    }

    internal static string[] CopyStrings(string[] source)
    {
        string[] result = new string[source.Length];
        global::System.Array.Copy(source, result, source.Length);
        return result;
    }

    internal static string[] CanonicalIdentifiers(TrademarkIdentifierResult[] source)
    {
        string[] result = new string[source.Length];
        int index;
        for (index = 0; index < source.Length; index += 1)
        {
            result[index] = source[index].CanonicalNumber;
        }

        return result;
    }

    internal static TrademarkLastUpdateResult CreateLastUpdateResult(string requestedIdentifier, TrademarkIdentifierResult expectedIdentifier, HttpResponseMetadata metadata, DocumentFormat format, string? rawResponse)
    {
        TrademarkLastUpdateResult result = new TrademarkLastUpdateResult();
        result.RequestedIdentifier = requestedIdentifier;
        result.CanonicalIdentifier = expectedIdentifier.CanonicalNumber;
        result.Provenance = TsdrDocumentXmlParser.CreateProvenance(metadata, "TSDR case last update");
        result.RawFormat = format;
        result.RawResponse = rawResponse;
        return result;
    }

    internal static void ValidateLastUpdateInputs(string content, TrademarkIdentifierResult expectedIdentifier, HttpResponseMetadata metadata)
    {
        if (content == null)
        {
            throw new ValidationException("A TSDR last-update response is required.", "content", "REQUIRED", null);
        }

        if (expectedIdentifier == null || !expectedIdentifier.IsValid)
        {
            throw new ValidationException("A valid expected trademark identifier is required.", "expectedIdentifier", "INVALID_IDENTIFIER", null);
        }

        if (metadata == null)
        {
            throw new ValidationException("TSDR last-update response metadata is required.", "metadata", "REQUIRED", null);
        }
    }

    internal static ParseException ParseFailure(string message, string path, DocumentFormat format)
    {
        return new ParseException(message, format, "TSDR transaction", path, 0L, false, 0, false, null);
    }
}
