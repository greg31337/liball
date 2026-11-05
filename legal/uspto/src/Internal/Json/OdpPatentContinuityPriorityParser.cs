namespace uspto;

internal static class OdpPatentContinuityPriorityParser
{
    internal static PatentContinuity ParseContinuity(string json, string expectedApplicationNumber, HttpResponseMetadata responseMetadata)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP patent continuity response is required.", "json", "REQUIRED", null);
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
            string sourceName = "ODP patent continuity retrieval";
            JsonFieldReader root = new JsonFieldReader(document.RootElement, "$");
            JsonFieldValue count = root.ReadInt32("count");
            if (!count.IsPresent || !count.IsValid || count.IsNull || count.Int32Value != 1)
            {
                throw ContinuityPriorityFailure("The ODP patent continuity response must contain exactly one application.", count.Path, sourceName);
            }

            JsonFieldValue records = root.ReadArray("patentFileWrapperDataBag");
            if (!records.IsPresent || !records.IsValid || records.IsNull || records.ArrayItems.Length != 1)
            {
                throw ContinuityPriorityFailure("The ODP patent continuity response has an invalid application collection.", records.Path, sourceName);
            }

            if (records.ArrayItems[0].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw ContinuityPriorityFailure("The ODP patent continuity application record is invalid.", records.ArrayItemPaths[0], sourceName);
            }

            JsonFieldReader record = new JsonFieldReader(records.ArrayItems[0], records.ArrayItemPaths[0]);
            string? requestIdentifier = ReadRequestIdentifier(root, responseMetadata.RequestIdentifier, sourceName);
            requestIdentifier = ReadRequestIdentifier(record, requestIdentifier, sourceName);
            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            PatentContinuity result = ParseContinuityRecord(record, expectedApplicationNumber, requestIdentifier, provenance, sourceName);
            JsonUnknownField[] unknown = root.CaptureUnknownFields(new string[] { "count", "patentFileWrapperDataBag", "requestIdentifier" });
            MergeUnknownFields(result.UnknownFields, unknown, "response.");
            return result;
        }
    }

    internal static PatentContinuity ParseContinuityRecord(JsonFieldReader record, string expectedApplicationNumber, string? requestIdentifier, PatentDataProvenance provenance, string sourceName)
    {
        string applicationNumber = ReadRequestedApplicationNumber(record, expectedApplicationNumber, sourceName);
        PatentContinuity result = new PatentContinuity();
        result.ApplicationNumber = applicationNumber;
        result.Parents = ParseParentBag(record, sourceName);
        result.Children = ParseChildBag(record, sourceName);
        result.Count = result.Parents.Length + result.Children.Length;
        result.RequestIdentifier = requestIdentifier;
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = record.CaptureUnknownFields(new string[] { "applicationNumberText", "parentContinuityBag", "childContinuityBag", "requestIdentifier" });
        MergeUnknownFields(result.UnknownFields, unknown, string.Empty);
        result.Graph = ContinuityGraphBuilder.Build(new PatentContinuity[] { result });
        return result;
    }

    internal static PatentParentContinuity[] ParseParentBag(JsonFieldReader record, string sourceName)
    {
        JsonFieldValue bag = record.ReadArray("parentContinuityBag");
        ValidateOptionalValue(bag, "The ODP parent-continuity collection is invalid.", sourceName);
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentParentContinuity[0];
        }

        PatentParentContinuity[] parents = new PatentParentContinuity[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            if (bag.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw ContinuityPriorityFailure("The ODP parent-continuity entry is invalid.", bag.ArrayItemPaths[index], sourceName);
            }

            parents[index] = ParseParent(bag.ArrayItems[index], bag.ArrayItemPaths[index], sourceName);
        }

        return parents;
    }

    internal static PatentParentContinuity ParseParent(global::System.Text.Json.JsonElement element, string path, string sourceName)
    {
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentParentContinuity parent = new PatentParentContinuity();
        parent.ParentApplicationNumber = ReadOptionalCanonicalApplication(reader, "parentApplicationNumberText", sourceName);
        parent.ChildApplicationNumber = ReadOptionalCanonicalApplication(reader, "childApplicationNumberText", sourceName);
        parent.ParentPatentNumber = ReadOptionalString(reader, "parentPatentNumber", sourceName);
        parent.RelationshipCode = ReadOptionalString(reader, "claimParentageTypeCode", sourceName);
        parent.RelationshipDescription = ReadOptionalString(reader, "claimParentageTypeCodeDescriptionText", sourceName);
        parent.ParentApplicationStatusDescription = ReadOptionalString(reader, "parentApplicationStatusDescriptionText", sourceName);
        JsonFieldValue status = ReadWholeInt32(reader, "parentApplicationStatusCode");
        ValidateOptionalValue(status, "The ODP parent application status code is invalid.", sourceName);
        if (status.IsPresent && !status.IsNull)
        {
            if (status.Int32Value < 0)
            {
                throw ContinuityPriorityFailure("The ODP parent application status code cannot be negative.", status.Path, sourceName);
            }

            parent.ParentApplicationStatusCode = status.Int32Value;
            parent.HasParentApplicationStatusCode = true;
        }

        JsonFieldValue filingDate = ReadAliasedDate(reader, new string[] { "parentApplicationFilingDate", "parentFilingDate" });
        ValidateOptionalValue(filingDate, "The ODP parent application filing date is invalid.", sourceName);
        if (filingDate.IsPresent && !filingDate.IsNull)
        {
            parent.ParentApplicationFilingDate = filingDate.DateValue;
            parent.HasParentApplicationFilingDate = true;
        }

        JsonFieldValue firstInventor = ReadIndicator(reader, "firstInventorToFileIndicator");
        ValidateOptionalValue(firstInventor, "The ODP parent-continuity first-inventor-to-file indicator is invalid.", sourceName);
        if (firstInventor.IsPresent && !firstInventor.IsNull)
        {
            parent.IsFirstInventorToFile = firstInventor.BooleanValue;
            parent.HasIsFirstInventorToFile = true;
        }

        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownParentFields());
        MergeUnknownFields(parent.UnknownFields, unknown, string.Empty);
        return parent;
    }

    internal static PatentChildContinuity[] ParseChildBag(JsonFieldReader record, string sourceName)
    {
        JsonFieldValue bag = record.ReadArray("childContinuityBag");
        ValidateOptionalValue(bag, "The ODP child-continuity collection is invalid.", sourceName);
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentChildContinuity[0];
        }

        PatentChildContinuity[] children = new PatentChildContinuity[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            if (bag.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw ContinuityPriorityFailure("The ODP child-continuity entry is invalid.", bag.ArrayItemPaths[index], sourceName);
            }

            children[index] = ParseChild(bag.ArrayItems[index], bag.ArrayItemPaths[index], sourceName);
        }

        return children;
    }

    internal static PatentChildContinuity ParseChild(global::System.Text.Json.JsonElement element, string path, string sourceName)
    {
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentChildContinuity child = new PatentChildContinuity();
        child.ParentApplicationNumber = ReadOptionalCanonicalApplication(reader, "parentApplicationNumberText", sourceName);
        child.ChildApplicationNumber = ReadOptionalCanonicalApplication(reader, "childApplicationNumberText", sourceName);
        child.ChildPatentNumber = ReadOptionalString(reader, "childPatentNumber", sourceName);
        child.RelationshipCode = ReadOptionalString(reader, "claimParentageTypeCode", sourceName);
        child.RelationshipDescription = ReadOptionalString(reader, "claimParentageTypeCodeDescriptionText", sourceName);
        child.ChildApplicationStatusDescription = ReadOptionalString(reader, "childApplicationStatusDescriptionText", sourceName);
        JsonFieldValue status = ReadWholeInt32(reader, "childApplicationStatusCode");
        ValidateOptionalValue(status, "The ODP child application status code is invalid.", sourceName);
        if (status.IsPresent && !status.IsNull)
        {
            if (status.Int32Value < 0)
            {
                throw ContinuityPriorityFailure("The ODP child application status code cannot be negative.", status.Path, sourceName);
            }

            child.ChildApplicationStatusCode = status.Int32Value;
            child.HasChildApplicationStatusCode = true;
        }

        JsonFieldValue filingDate = ReadAliasedDate(reader, new string[] { "childApplicationFilingDate", "childFilingDate" });
        ValidateOptionalValue(filingDate, "The ODP child application filing date is invalid.", sourceName);
        if (filingDate.IsPresent && !filingDate.IsNull)
        {
            child.ChildApplicationFilingDate = filingDate.DateValue;
            child.HasChildApplicationFilingDate = true;
        }

        JsonFieldValue firstInventor = ReadIndicator(reader, "firstInventorToFileIndicator");
        ValidateOptionalValue(firstInventor, "The ODP child-continuity first-inventor-to-file indicator is invalid.", sourceName);
        if (firstInventor.IsPresent && !firstInventor.IsNull)
        {
            child.IsFirstInventorToFile = firstInventor.BooleanValue;
            child.HasIsFirstInventorToFile = true;
        }

        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownChildFields());
        MergeUnknownFields(child.UnknownFields, unknown, string.Empty);
        return child;
    }

    internal static PatentForeignPriorityList ParseForeignPriorities(string json, string expectedApplicationNumber, HttpResponseMetadata responseMetadata)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP foreign-priority response is required.", "json", "REQUIRED", null);
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
            string sourceName = "ODP patent foreign-priority retrieval";
            JsonFieldReader root = new JsonFieldReader(document.RootElement, "$");
            JsonFieldValue count = root.ReadInt32("count");
            if (!count.IsPresent || !count.IsValid || count.IsNull || count.Int32Value != 1)
            {
                throw ContinuityPriorityFailure("The ODP foreign-priority response must contain exactly one application.", count.Path, sourceName);
            }

            JsonFieldValue records = root.ReadArray("patentFileWrapperDataBag");
            if (!records.IsPresent || !records.IsValid || records.IsNull || records.ArrayItems.Length != 1)
            {
                throw ContinuityPriorityFailure("The ODP foreign-priority response has an invalid application collection.", records.Path, sourceName);
            }

            if (records.ArrayItems[0].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw ContinuityPriorityFailure("The ODP foreign-priority application record is invalid.", records.ArrayItemPaths[0], sourceName);
            }

            JsonFieldReader record = new JsonFieldReader(records.ArrayItems[0], records.ArrayItemPaths[0]);
            string? requestIdentifier = ReadRequestIdentifier(root, responseMetadata.RequestIdentifier, sourceName);
            requestIdentifier = ReadRequestIdentifier(record, requestIdentifier, sourceName);
            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            PatentForeignPriorityList result = ParseForeignPriorityRecord(record, expectedApplicationNumber, requestIdentifier, provenance, sourceName);
            JsonUnknownField[] unknown = root.CaptureUnknownFields(new string[] { "count", "patentFileWrapperDataBag", "requestIdentifier" });
            MergeUnknownFields(result.UnknownFields, unknown, "response.");
            return result;
        }
    }

    internal static PatentForeignPriorityList ParseForeignPriorityRecord(JsonFieldReader record, string expectedApplicationNumber, string? requestIdentifier, PatentDataProvenance provenance, string sourceName)
    {
        string applicationNumber = ReadRequestedApplicationNumber(record, expectedApplicationNumber, sourceName);
        PatentForeignPriorityList result = new PatentForeignPriorityList();
        result.ApplicationNumber = applicationNumber;
        result.Claims = ParseForeignPriorityBag(record, sourceName);
        result.Count = result.Claims.Length;
        result.RequestIdentifier = requestIdentifier;
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = record.CaptureUnknownFields(new string[] { "applicationNumberText", "foreignPriorityBag", "requestIdentifier" });
        MergeUnknownFields(result.UnknownFields, unknown, string.Empty);
        return result;
    }

    internal static PatentForeignPriorityClaim[] ParseForeignPriorityBag(JsonFieldReader record, string sourceName)
    {
        JsonFieldValue bag = record.ReadArray("foreignPriorityBag");
        ValidateOptionalValue(bag, "The ODP foreign-priority collection is invalid.", sourceName);
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentForeignPriorityClaim[0];
        }

        PatentForeignPriorityClaim[] claims = new PatentForeignPriorityClaim[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            if (bag.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw ContinuityPriorityFailure("The ODP foreign-priority entry is invalid.", bag.ArrayItemPaths[index], sourceName);
            }

            claims[index] = ParseForeignPriorityClaim(bag.ArrayItems[index], bag.ArrayItemPaths[index], sourceName);
        }

        return claims;
    }

    internal static PatentForeignPriorityClaim ParseForeignPriorityClaim(global::System.Text.Json.JsonElement element, string path, string sourceName)
    {
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentForeignPriorityClaim claim = new PatentForeignPriorityClaim();
        claim.OfficeName = ReadAliasedString(reader, new string[] { "ipOfficeName", "foreignPriorityCountryName", "countryName" }, sourceName);
        claim.CountryCode = ReadAliasedString(reader, new string[] { "ipOfficeCode", "countryCode", "foreignPriorityCountryCode" }, sourceName);
        claim.ApplicationNumber = ReadAliasedString(reader, new string[] { "applicationNumberText", "foreignApplicationNumberText" }, sourceName);
        JsonFieldValue filingDate = ReadAliasedDate(reader, new string[] { "filingDate", "foreignPriorityDate" });
        ValidateOptionalValue(filingDate, "The ODP foreign-priority filing date is invalid.", sourceName);
        if (filingDate.IsPresent && !filingDate.IsNull)
        {
            claim.FilingDate = filingDate.DateValue;
            claim.HasFilingDate = true;
        }

        JsonFieldValue sequence = ReadAliasedWholeInt32(reader, new string[] { "priorityClaimSequenceNumber", "prioritySequenceNumber", "foreignPrioritySequenceNumber", "sequenceNumber" });
        ValidateOptionalValue(sequence, "The ODP foreign-priority sequence is invalid.", sourceName);
        if (sequence.IsPresent && !sequence.IsNull)
        {
            if (sequence.Int32Value < 0)
            {
                throw ContinuityPriorityFailure("The ODP foreign-priority sequence cannot be negative.", sequence.Path, sourceName);
            }

            claim.PrioritySequenceNumber = sequence.Int32Value;
            claim.HasPrioritySequenceNumber = true;
        }

        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownForeignPriorityFields());
        MergeUnknownFields(claim.UnknownFields, unknown, string.Empty);
        return claim;
    }

    internal static string ReadRequestedApplicationNumber(JsonFieldReader record, string expectedApplicationNumber, string sourceName)
    {
        JsonFieldValue applicationNumber = record.ReadString("applicationNumberText");
        if (!applicationNumber.IsPresent || !applicationNumber.IsValid || applicationNumber.IsNull)
        {
            throw ContinuityPriorityFailure("The ODP response application number is missing or invalid.", applicationNumber.Path, sourceName);
        }

        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(applicationNumber.StringValue ?? string.Empty);
        if (!parsed.IsValid || !global::System.String.Equals(parsed.CanonicalNumber, expectedApplicationNumber, global::System.StringComparison.Ordinal))
        {
            throw ContinuityPriorityFailure("The ODP response does not match the requested application.", applicationNumber.Path, sourceName);
        }

        return parsed.CanonicalNumber;
    }

    internal static string? ReadOptionalCanonicalApplication(JsonFieldReader reader, string name, string sourceName)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptionalValue(value, "The ODP continuity application number is invalid.", sourceName);
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(value.StringValue ?? string.Empty);
        if (!parsed.IsValid)
        {
            throw ContinuityPriorityFailure("The ODP continuity application number is invalid.", value.Path, sourceName);
        }

        return parsed.CanonicalNumber;
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name, string sourceName)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptionalValue(value, "The ODP continuity or priority field is invalid.", sourceName);
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static string? ReadAliasedString(JsonFieldReader reader, string[] names, string sourceName)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadString(names[index]);
            ValidateOptionalValue(value, "The ODP continuity or priority field is invalid.", sourceName);
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

    internal static JsonFieldValue ReadWholeInt32(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadInt32(name);
        if (value.IsValid || !value.IsPresent || value.IsNull)
        {
            return value;
        }

        JsonFieldValue raw = reader.ReadRaw(name);
        string? text = null;
        if (raw.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Number)
        {
            text = raw.ElementValue.GetRawText();
        }
        else if (raw.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.String)
        {
            text = raw.ElementValue.GetString();
        }

        if (text == null)
        {
            return value;
        }

        string normalized = text.Trim();
        int dot = normalized.IndexOf('.');
        if (dot <= 0 || dot == normalized.Length - 1)
        {
            return value;
        }

        int index;
        for (index = dot + 1; index < normalized.Length; index += 1)
        {
            if (normalized[index] != '0')
            {
                return value;
            }
        }

        JsonIntegerParseResult parsed = JsonFieldReader.ParseSignedInteger(normalized.Substring(0, dot));
        if (parsed.IsValid && parsed.Value >= global::System.Int32.MinValue && parsed.Value <= global::System.Int32.MaxValue)
        {
            value.Int32Value = (int)parsed.Value;
            value.Int64Value = parsed.Value;
            value.IsValid = true;
        }

        return value;
    }

    internal static JsonFieldValue ReadAliasedWholeInt32(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = ReadWholeInt32(reader, names[index]);
            if (value.IsPresent)
            {
                return value;
            }
        }

        return ReadWholeInt32(reader, names[0]);
    }

    internal static JsonFieldValue ReadAliasedDate(JsonFieldReader reader, string[] names)
    {
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

    internal static JsonFieldValue ReadIndicator(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadBoolean(name);
        if (value.IsValid || !value.IsPresent)
        {
            return value;
        }

        JsonFieldValue text = reader.ReadString(name);
        if (!text.IsValid || text.IsNull || text.StringValue == null)
        {
            return value;
        }

        string normalized = text.StringValue.Trim();
        if (normalized.Equals("Y", global::System.StringComparison.OrdinalIgnoreCase) || normalized.Equals("YES", global::System.StringComparison.OrdinalIgnoreCase))
        {
            value.BooleanValue = true;
            value.IsValid = true;
        }
        else if (normalized.Equals("N", global::System.StringComparison.OrdinalIgnoreCase) || normalized.Equals("NO", global::System.StringComparison.OrdinalIgnoreCase))
        {
            value.BooleanValue = false;
            value.IsValid = true;
        }

        return value;
    }

    internal static string? ReadRequestIdentifier(JsonFieldReader reader, string? fallback, string sourceName)
    {
        JsonFieldValue value = reader.ReadString("requestIdentifier");
        ValidateOptionalValue(value, "The ODP response request identifier is invalid.", sourceName);
        if (value.IsPresent && !value.IsNull)
        {
            return value.StringValue;
        }

        return fallback;
    }

    internal static void MergeUnknownFields(global::System.Collections.Generic.Dictionary<string, string> destination, JsonUnknownField[] fields, string collisionPrefix)
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

    internal static void ValidateOptionalValue(JsonFieldValue value, string message, string sourceName)
    {
        if (!value.IsValid)
        {
            throw ContinuityPriorityFailure(message, value.Path, sourceName);
        }
    }

    internal static string[] KnownParentFields()
    {
        return new string[]
        {
            "firstInventorToFileIndicator",
            "parentApplicationStatusCode",
            "parentPatentNumber",
            "parentApplicationStatusDescriptionText",
            "parentApplicationFilingDate",
            "parentFilingDate",
            "parentApplicationNumberText",
            "childApplicationNumberText",
            "claimParentageTypeCode",
            "claimParentageTypeCodeDescriptionText"
        };
    }

    internal static string[] KnownChildFields()
    {
        return new string[]
        {
            "childApplicationStatusCode",
            "parentApplicationNumberText",
            "childApplicationNumberText",
            "childApplicationStatusDescriptionText",
            "childApplicationFilingDate",
            "childFilingDate",
            "firstInventorToFileIndicator",
            "childPatentNumber",
            "claimParentageTypeCode",
            "claimParentageTypeCodeDescriptionText"
        };
    }

    internal static string[] KnownForeignPriorityFields()
    {
        return new string[]
        {
            "ipOfficeName",
            "foreignPriorityCountryName",
            "countryName",
            "ipOfficeCode",
            "countryCode",
            "foreignPriorityCountryCode",
            "filingDate",
            "foreignPriorityDate",
            "applicationNumberText",
            "foreignApplicationNumberText",
            "priorityClaimSequenceNumber",
            "prioritySequenceNumber",
            "foreignPrioritySequenceNumber",
            "sequenceNumber"
        };
    }

    internal static ParseException ContinuityPriorityFailure(string message, string path, string sourceName)
    {
        return new ParseException(message, DocumentFormat.Json, sourceName, path, 0L, false, 0, false, null);
    }
}
