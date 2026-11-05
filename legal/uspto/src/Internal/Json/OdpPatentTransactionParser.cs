namespace uspto;

internal static class OdpPatentTransactionParser
{
    internal static PatentTransactionHistory Parse(string json, string expectedApplicationNumber, HttpResponseMetadata responseMetadata)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP patent transaction response is required.", "json", "REQUIRED", null);
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
                throw TransactionFailure("The ODP patent transaction response must contain exactly one application.", count.Path);
            }

            JsonFieldValue records = root.ReadArray("patentFileWrapperDataBag");
            if (!records.IsPresent || !records.IsValid || records.IsNull || records.ArrayItems.Length != 1)
            {
                throw TransactionFailure("The ODP patent transaction response has an invalid application collection.", records.Path);
            }

            JsonFieldValue requestIdentifier = root.ReadString("requestIdentifier");
            ValidateOptional(requestIdentifier, "The ODP patent transaction request identifier is invalid.");
            string? resolvedRequestIdentifier = responseMetadata.RequestIdentifier;
            if (requestIdentifier.IsPresent && !requestIdentifier.IsNull)
            {
                resolvedRequestIdentifier = requestIdentifier.StringValue;
            }

            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, resolvedRequestIdentifier);
            return ParseRecord(records.ArrayItems[0], records.ArrayItemPaths[0], expectedApplicationNumber, resolvedRequestIdentifier, provenance);
        }
    }

    internal static PatentTransactionHistory ParseRecord(global::System.Text.Json.JsonElement element, string path, string expectedApplicationNumber, string? requestIdentifier, PatentDataProvenance provenance)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw TransactionFailure("The ODP patent transaction application record is invalid.", path);
        }

        JsonFieldReader record = new JsonFieldReader(element, path);
        JsonFieldValue applicationNumber = record.ReadString("applicationNumberText");
        if (!applicationNumber.IsPresent || !applicationNumber.IsValid || applicationNumber.IsNull)
        {
            throw TransactionFailure("The ODP patent transaction application number is missing or invalid.", applicationNumber.Path);
        }

        PatentApplicationNumberResult parsedNumber = PatentIdentifierParser.ParseApplicationNumber(applicationNumber.StringValue ?? string.Empty);
        if (!parsedNumber.IsValid || !global::System.String.Equals(parsedNumber.CanonicalNumber, expectedApplicationNumber, global::System.StringComparison.Ordinal))
        {
            throw TransactionFailure("The ODP patent transaction response does not match the requested application.", applicationNumber.Path);
        }

        PatentTransactionHistory history = new PatentTransactionHistory();
        history.ApplicationNumber = parsedNumber.CanonicalNumber;
        history.Events = ParseEvents(record);
        history.Count = history.Events.Length;
        history.RequestIdentifier = requestIdentifier;
        history.Provenance = OdpJsonParser.CopyProvenance(provenance);
        ApplyHistoryLastModified(history, record);
        JsonUnknownField[] unknown = record.CaptureUnknownFields(new string[] { "applicationNumberText", "eventDataBag", "lastIngestionDateTime", "lastModifiedDateTime", "requestIdentifier" });
        int index;
        for (index = 0; index < unknown.Length; index += 1)
        {
            history.UnknownSections[unknown[index].Name] = unknown[index].RawJson;
        }

        return history;
    }

    internal static PatentTransaction[] ParseEvents(JsonFieldReader record)
    {
        JsonFieldValue bag = record.ReadArray("eventDataBag");
        ValidateOptional(bag, "The ODP patent transaction event collection is invalid.");
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentTransaction[0];
        }

        PatentTransaction[] events = new PatentTransaction[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            global::System.Text.Json.JsonElement item = bag.ArrayItems[index];
            if (item.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw TransactionFailure("The ODP patent transaction event is invalid.", bag.ArrayItemPaths[index]);
            }

            events[index] = ParseEvent(item, bag.ArrayItemPaths[index]);
        }

        return events;
    }

    internal static PatentTransaction ParseEvent(global::System.Text.Json.JsonElement element, string path)
    {
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentTransaction transaction = new PatentTransaction();
        JsonFieldValue sequence = ReadSequence(reader);
        ValidateOptional(sequence, "The ODP patent transaction event sequence is invalid.");
        if (sequence.IsPresent && !sequence.IsNull)
        {
            if (sequence.Int32Value < 0)
            {
                throw TransactionFailure("The ODP patent transaction event sequence cannot be negative.", sequence.Path);
            }

            transaction.EventSequenceNumber = sequence.Int32Value;
            transaction.HasEventSequenceNumber = true;
        }

        JsonFieldValue eventDate = reader.ReadDate("eventDate");
        ValidateOptional(eventDate, "The ODP patent transaction event date is invalid.");
        if (eventDate.IsPresent && !eventDate.IsNull)
        {
            transaction.EventDate = eventDate.DateValue;
            transaction.HasEventDate = true;
        }

        transaction.EventCode = ReadOptionalString(reader, "eventCode");
        transaction.Description = ReadOptionalString(reader, "eventDescriptionText");
        transaction.Source = ReadEventSource(reader);
        JsonFieldValue lastModified = ReadEventLastModified(reader);
        ValidateOptional(lastModified, "The ODP patent transaction last-modified time is invalid.");
        if (lastModified.IsPresent && !lastModified.IsNull)
        {
            transaction.LastModifiedAt = lastModified.DateValue;
            transaction.HasLastModifiedAt = true;
        }

        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownEventFields());
        int index;
        for (index = 0; index < unknown.Length; index += 1)
        {
            transaction.UnknownFields[unknown[index].Name] = unknown[index].RawJson;
        }

        return transaction;
    }

    internal static JsonFieldValue ReadSequence(JsonFieldReader reader)
    {
        JsonFieldValue value = reader.ReadInt32("eventSequenceNumber");
        if (value.IsPresent)
        {
            return value;
        }

        return reader.ReadInt32("sequenceNumber");
    }

    internal static string? ReadEventSource(JsonFieldReader reader)
    {
        string[] names = new string[] { "eventSource", "eventSourceText", "eventSourceCategory", "eventSourceCode", "source" };
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadString(names[index]);
            ValidateOptional(value, "The ODP patent transaction event source is invalid.");
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

    internal static JsonFieldValue ReadEventLastModified(JsonFieldReader reader)
    {
        string[] names = new string[] { "eventLastModifiedDateTime", "lastModifiedDateTime", "eventLastModifiedDate", "lastModifiedDate" };
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadDate(names[index]);
            if (value.IsPresent)
            {
                return value;
            }
        }

        return reader.ReadDate(names[0]);
    }

    internal static void ApplyHistoryLastModified(PatentTransactionHistory history, JsonFieldReader record)
    {
        JsonFieldValue value = record.ReadDate("lastIngestionDateTime");
        if (!value.IsPresent)
        {
            value = record.ReadDate("lastModifiedDateTime");
        }

        ValidateOptional(value, "The ODP patent transaction history last-modified time is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            history.LastModifiedAt = value.DateValue;
            history.HasLastModifiedAt = true;
            history.Provenance.LastUpdatedAt = value.DateValue;
            history.Provenance.HasLastUpdatedAt = true;
        }
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptional(value, "The ODP patent transaction field is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static void ValidateOptional(JsonFieldValue value, string message)
    {
        if (!value.IsValid)
        {
            throw TransactionFailure(message, value.Path);
        }
    }

    internal static string[] KnownEventFields()
    {
        return new string[]
        {
            "eventSequenceNumber",
            "sequenceNumber",
            "eventDate",
            "eventCode",
            "eventDescriptionText",
            "eventSource",
            "eventSourceText",
            "eventSourceCategory",
            "eventSourceCode",
            "source",
            "eventLastModifiedDateTime",
            "lastModifiedDateTime",
            "eventLastModifiedDate",
            "lastModifiedDate"
        };
    }

    internal static ParseException TransactionFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "ODP patent transaction retrieval", path, 0L, false, 0, false, null);
    }
}
