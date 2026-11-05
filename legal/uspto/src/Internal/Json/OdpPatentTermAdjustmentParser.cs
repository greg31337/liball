namespace uspto;

internal class PatentAdjustmentNumberValue
{
    internal bool IsPresent = false;
    internal bool IsNull = false;
    internal bool IsValid = false;
    internal string Path = "$";
    internal double Value = 0.0;
}

internal static class OdpPatentTermAdjustmentParser
{
    internal static PatentTermAdjustment Parse(string json, string expectedApplicationNumber, HttpResponseMetadata responseMetadata)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP patent-term-adjustment response is required.", "json", "REQUIRED", null);
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
                throw AdjustmentFailure("The ODP patent-term-adjustment response must contain exactly one application.", count.Path);
            }

            JsonFieldValue records = root.ReadArray("patentFileWrapperDataBag");
            if (!records.IsPresent || !records.IsValid || records.IsNull || records.ArrayItems.Length != 1)
            {
                throw AdjustmentFailure("The ODP patent-term-adjustment response has an invalid application collection.", records.Path);
            }

            if (records.ArrayItems[0].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw AdjustmentFailure("The ODP patent-term-adjustment application record is invalid.", records.ArrayItemPaths[0]);
            }

            JsonFieldReader record = new JsonFieldReader(records.ArrayItems[0], records.ArrayItemPaths[0]);
            string? requestIdentifier = ReadRequestIdentifier(root, responseMetadata.RequestIdentifier);
            requestIdentifier = ReadRequestIdentifier(record, requestIdentifier);
            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            PatentTermAdjustment result = ParseApplicationRecord(record, expectedApplicationNumber, requestIdentifier, provenance);
            JsonUnknownField[] unknown = root.CaptureUnknownFields(new string[] { "count", "patentFileWrapperDataBag", "requestIdentifier" });
            MergeUnknown(result.UnknownFields, unknown, "response.");
            return result;
        }
    }

    internal static PatentTermAdjustment ParseApplicationRecord(JsonFieldReader record, string expectedApplicationNumber, string? requestIdentifier, PatentDataProvenance provenance)
    {
        JsonFieldValue applicationNumber = record.ReadString("applicationNumberText");
        if (!applicationNumber.IsPresent || !applicationNumber.IsValid || applicationNumber.IsNull)
        {
            throw AdjustmentFailure("The ODP patent-term-adjustment application number is missing or invalid.", applicationNumber.Path);
        }

        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(applicationNumber.StringValue ?? string.Empty);
        if (!parsed.IsValid || !global::System.String.Equals(parsed.CanonicalNumber, expectedApplicationNumber, global::System.StringComparison.Ordinal))
        {
            throw AdjustmentFailure("The ODP patent-term-adjustment response does not match the requested application.", applicationNumber.Path);
        }

        PatentTermAdjustment result = new PatentTermAdjustment();
        result.ApplicationNumber = parsed.CanonicalNumber;
        JsonFieldValue data = record.ReadObject("patentTermAdjustmentData");
        ValidateOptional(data, "The ODP patent-term-adjustment data is invalid.");
        if (data.IsPresent && !data.IsNull)
        {
            ParseAdjustmentData(result, new JsonFieldReader(data.ElementValue, data.Path));
        }

        result.Count = result.History.Length;
        result.RequestIdentifier = requestIdentifier;
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = record.CaptureUnknownFields(new string[] { "applicationNumberText", "patentTermAdjustmentData", "requestIdentifier" });
        MergeUnknown(result.UnknownFields, unknown, string.Empty);
        return result;
    }

    internal static void ParseAdjustmentData(PatentTermAdjustment result, JsonFieldReader data)
    {
        JsonFieldValue total = data.ReadInt32("adjustmentTotalQuantity");
        ValidateOptional(total, "The ODP patent-term-adjustment total is invalid.");
        if (total.IsPresent && !total.IsNull)
        {
            result.AdjustmentTotalDays = total.Int32Value;
            result.HasAdjustmentTotalDays = true;
        }

        JsonFieldValue aDelay = data.ReadInt32("aDelayQuantity");
        ValidateOptional(aDelay, "The ODP patent-term-adjustment A delay is invalid.");
        if (aDelay.IsPresent && !aDelay.IsNull)
        {
            result.ADelayDays = aDelay.Int32Value;
            result.HasADelayDays = true;
        }

        JsonFieldValue bDelay = data.ReadInt32("bDelayQuantity");
        ValidateOptional(bDelay, "The ODP patent-term-adjustment B delay is invalid.");
        if (bDelay.IsPresent && !bDelay.IsNull)
        {
            result.BDelayDays = bDelay.Int32Value;
            result.HasBDelayDays = true;
        }

        JsonFieldValue cDelay = data.ReadInt32("cDelayQuantity");
        ValidateOptional(cDelay, "The ODP patent-term-adjustment C delay is invalid.");
        if (cDelay.IsPresent && !cDelay.IsNull)
        {
            result.CDelayDays = cDelay.Int32Value;
            result.HasCDelayDays = true;
        }

        JsonFieldValue applicantDelay = data.ReadInt32("applicantDayDelayQuantity");
        ValidateOptional(applicantDelay, "The ODP patent-term-adjustment applicant delay is invalid.");
        if (applicantDelay.IsPresent && !applicantDelay.IsNull)
        {
            result.ApplicantDelayDays = applicantDelay.Int32Value;
            result.HasApplicantDelayDays = true;
        }

        ApplyDecimalTotals(result, data);
        result.History = ParseHistory(data);
        JsonUnknownField[] unknown = data.CaptureUnknownFields(KnownAdjustmentFields());
        MergeUnknown(result.UnknownFields, unknown, "adjustment.");
    }

    internal static void ApplyDecimalTotals(PatentTermAdjustment result, JsonFieldReader data)
    {
        PatentAdjustmentNumberValue nonOverlapping = ReadAliasedNumber(data, new string[] { "nonOverlappingDayDelayQuantity", "nonOverlappingDayQuantity" });
        ValidateNumber(nonOverlapping, "The ODP patent-term-adjustment non-overlapping delay is invalid.");
        if (nonOverlapping.IsPresent && !nonOverlapping.IsNull)
        {
            result.NonOverlappingDelayDays = nonOverlapping.Value;
            result.HasNonOverlappingDelayDays = true;
        }

        PatentAdjustmentNumberValue overlapping = ReadAliasedNumber(data, new string[] { "overlappingDayDelayQuantity", "overlappingDayQuantity" });
        ValidateNumber(overlapping, "The ODP patent-term-adjustment overlapping delay is invalid.");
        if (overlapping.IsPresent && !overlapping.IsNull)
        {
            result.OverlappingDelayDays = overlapping.Value;
            result.HasOverlappingDelayDays = true;
        }

        PatentAdjustmentNumberValue officeAdjustment = ReadAliasedNumber(data, new string[] { "ipOfficeAdjustmentDelayQuantity", "ipOfficeAdjustmentDayQuantity" });
        ValidateNumber(officeAdjustment, "The ODP patent-term-adjustment office adjustment is invalid.");
        if (officeAdjustment.IsPresent && !officeAdjustment.IsNull)
        {
            result.IpOfficeAdjustmentDelayDays = officeAdjustment.Value;
            result.HasIpOfficeAdjustmentDelayDays = true;
        }
    }

    internal static PatentTermAdjustmentHistoryEntry[] ParseHistory(JsonFieldReader data)
    {
        JsonFieldValue bag = data.ReadArray("patentTermAdjustmentHistoryDataBag");
        ValidateOptional(bag, "The ODP patent-term-adjustment history collection is invalid.");
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentTermAdjustmentHistoryEntry[0];
        }

        PatentTermAdjustmentHistoryEntry[] history = new PatentTermAdjustmentHistoryEntry[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            if (bag.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw AdjustmentFailure("The ODP patent-term-adjustment history entry is invalid.", bag.ArrayItemPaths[index]);
            }

            history[index] = ParseHistoryEntry(bag.ArrayItems[index], bag.ArrayItemPaths[index]);
        }

        return history;
    }

    internal static PatentTermAdjustmentHistoryEntry ParseHistoryEntry(global::System.Text.Json.JsonElement element, string path)
    {
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentTermAdjustmentHistoryEntry entry = new PatentTermAdjustmentHistoryEntry();
        entry.Description = ReadOptionalString(reader, "eventDescriptionText");
        entry.Code = ReadAliasedString(reader, new string[] { "ptaPTECode", "ptaPteCode" });
        JsonFieldValue eventDate = reader.ReadDate("eventDate");
        ValidateOptional(eventDate, "The ODP patent-term-adjustment history date is invalid.");
        if (eventDate.IsPresent && !eventDate.IsNull)
        {
            entry.EventDate = eventDate.DateValue;
            entry.HasEventDate = true;
        }

        JsonFieldValue applicantDelay = reader.ReadInt32("applicantDayDelayQuantity");
        ValidateOptional(applicantDelay, "The ODP patent-term-adjustment history applicant delay is invalid.");
        if (applicantDelay.IsPresent && !applicantDelay.IsNull)
        {
            entry.ApplicantDelayDays = applicantDelay.Int32Value;
            entry.HasApplicantDelayDays = true;
        }

        JsonFieldValue officeDelay = reader.ReadInt32("ipOfficeDayDelayQuantity");
        ValidateOptional(officeDelay, "The ODP patent-term-adjustment history office delay is invalid.");
        if (officeDelay.IsPresent && !officeDelay.IsNull)
        {
            entry.IpOfficeDelayDays = officeDelay.Int32Value;
            entry.HasIpOfficeDelayDays = true;
        }

        ApplyHistorySequences(entry, reader);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "eventDate", "applicantDayDelayQuantity", "eventDescriptionText", "eventSequenceNumber", "ipOfficeDayDelayQuantity", "originatingEventSequenceNumber", "ptaPTECode", "ptaPteCode" });
        MergeUnknown(entry.UnknownFields, unknown, string.Empty);
        return entry;
    }

    internal static void ApplyHistorySequences(PatentTermAdjustmentHistoryEntry entry, JsonFieldReader reader)
    {
        PatentAdjustmentNumberValue sequence = ReadNumber(reader, "eventSequenceNumber");
        ValidateNumber(sequence, "The ODP patent-term-adjustment event sequence is invalid.");
        if (sequence.IsPresent && !sequence.IsNull)
        {
            entry.EventSequenceNumber = sequence.Value;
            entry.HasEventSequenceNumber = true;
        }

        PatentAdjustmentNumberValue originating = ReadNumber(reader, "originatingEventSequenceNumber");
        ValidateNumber(originating, "The ODP patent-term-adjustment originating sequence is invalid.");
        if (originating.IsPresent && !originating.IsNull)
        {
            entry.OriginatingEventSequenceNumber = originating.Value;
            entry.HasOriginatingEventSequenceNumber = true;
        }
    }

    internal static PatentAdjustmentNumberValue ReadNumber(JsonFieldReader reader, string name)
    {
        JsonFieldValue raw = reader.ReadRaw(name);
        PatentAdjustmentNumberValue result = new PatentAdjustmentNumberValue();
        result.IsPresent = raw.IsPresent;
        result.IsNull = raw.IsNull;
        result.Path = raw.Path;
        if (!raw.IsPresent || raw.IsNull)
        {
            result.IsValid = raw.IsValid;
            return result;
        }

        if (raw.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Number)
        {
            double value = raw.ElementValue.GetDouble();
            if (global::System.Double.IsFinite(value))
            {
                result.Value = value;
                result.IsValid = true;
            }
        }

        return result;
    }

    internal static PatentAdjustmentNumberValue ReadAliasedNumber(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            PatentAdjustmentNumberValue value = ReadNumber(reader, names[index]);
            if (value.IsPresent)
            {
                return value;
            }
        }

        return ReadNumber(reader, names[0]);
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptional(value, "The ODP patent-term-adjustment field is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static string? ReadAliasedString(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadString(names[index]);
            ValidateOptional(value, "The ODP patent-term-adjustment field is invalid.");
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

    internal static string? ReadRequestIdentifier(JsonFieldReader reader, string? fallback)
    {
        JsonFieldValue value = reader.ReadString("requestIdentifier");
        ValidateOptional(value, "The ODP patent-term-adjustment request identifier is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            return value.StringValue;
        }

        return fallback;
    }

    internal static void MergeUnknown(global::System.Collections.Generic.Dictionary<string, string> destination, JsonUnknownField[] fields, string collisionPrefix)
    {
        int index;
        for (index = 0; index < fields.Length; index += 1)
        {
            string name = fields[index].Name;
            if (destination.ContainsKey(name))
            {
                name = collisionPrefix + name;
            }

            destination[name] = fields[index].RawJson;
        }
    }

    internal static void ValidateOptional(JsonFieldValue value, string message)
    {
        if (!value.IsValid)
        {
            throw AdjustmentFailure(message, value.Path);
        }
    }

    internal static void ValidateNumber(PatentAdjustmentNumberValue value, string message)
    {
        if (!value.IsValid)
        {
            throw AdjustmentFailure(message, value.Path);
        }
    }

    internal static string[] KnownAdjustmentFields()
    {
        return new string[]
        {
            "adjustmentTotalQuantity", "aDelayQuantity", "bDelayQuantity", "cDelayQuantity", "applicantDayDelayQuantity",
            "nonOverlappingDayDelayQuantity", "nonOverlappingDayQuantity", "overlappingDayDelayQuantity", "overlappingDayQuantity",
            "ipOfficeAdjustmentDelayQuantity", "ipOfficeAdjustmentDayQuantity", "patentTermAdjustmentHistoryDataBag"
        };
    }

    internal static ParseException AdjustmentFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "ODP patent-term-adjustment retrieval", path, 0L, false, 0, false, null);
    }
}
