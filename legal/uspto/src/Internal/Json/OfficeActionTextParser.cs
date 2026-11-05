namespace uspto;

internal class OfficeActionDateValue
{
    internal global::System.DateTimeOffset Value = default(global::System.DateTimeOffset);
    internal bool HasValue = false;
}

internal class OfficeActionIntegerValue
{
    internal int Value = 0;
    internal bool HasValue = false;
}

internal static class OfficeActionTextParser
{
    internal static OfficeActionTextResponse Parse(string json, SearchRequest request, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An Office Action text response is required.", "json", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("An Office Action text search request is required.", "request", "REQUIRED", null);
        }

        if (request.OffsetPage == null)
        {
            throw new ValidationException("Office Action page coordinates are required.", "OffsetPage", "REQUIRED", null);
        }

        if (responseMetadata == null)
        {
            throw new ValidationException("Office Action response metadata is required.", "responseMetadata", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            if (document.RootElement.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw Failure("The Office Action text response must be a JSON object.", "$");
            }

            JsonFieldReader root = new JsonFieldReader(document.RootElement, "$");
            JsonFieldValue response = root.ReadObject("response");
            if (!response.IsValid)
            {
                throw Failure("The Office Action text response envelope is invalid.", response.Path);
            }

            JsonFieldReader payload = root;
            bool hasEnvelope = false;
            if (response.IsPresent)
            {
                if (response.IsNull || !response.HasElementValue)
                {
                    throw Failure("The Office Action text response envelope is invalid.", response.Path);
                }

                payload = new JsonFieldReader(response.ElementValue, response.Path);
                hasEnvelope = true;
            }

            JsonFieldValue total = payload.ReadInt64("numFound");
            if (!total.IsPresent || !total.IsValid || total.IsNull || total.Int64Value < 0L)
            {
                throw Failure("The Office Action text response has an invalid total.", total.Path);
            }

            JsonFieldValue start = payload.ReadInt64("start");
            if (!start.IsPresent || !start.IsValid || start.IsNull || start.Int64Value < 0L)
            {
                throw Failure("The Office Action text response has an invalid start offset.", start.Path);
            }

            JsonFieldValue documents = payload.ReadArray("docs");
            if (!documents.IsPresent || !documents.IsValid || documents.IsNull)
            {
                throw Failure("The Office Action text response has an invalid document collection.", documents.Path);
            }

            if (documents.ArrayItems.Length != 0 && (start.Int64Value > total.Int64Value || documents.ArrayItems.Length > total.Int64Value - start.Int64Value))
            {
                throw Failure("The Office Action text page extends beyond the reported total.", documents.Path);
            }

            string? requestIdentifier = ResolveRequestIdentifier(root, payload, hasEnvelope, responseMetadata);
            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            provenance.DatasetIdentifier = "oa_actions";
            provenance.DatasetVersion = "v1";
            OfficeActionTextRecord[] records = new OfficeActionTextRecord[documents.ArrayItems.Length];
            int index;
            for (index = 0; index < documents.ArrayItems.Length; index += 1)
            {
                records[index] = ParseRecord(documents.ArrayItems[index], documents.ArrayItemPaths[index], provenance);
            }

            OfficeActionTextResponse result = new OfficeActionTextResponse();
            result.Request = request;
            result.Records = records;
            result.Count = records.Length;
            result.Total = total.Int64Value;
            result.HasTotal = true;
            result.Start = start.Int64Value;
            result.Page.Offset = start.Int64Value;
            result.Page.Limit = request.OffsetPage.Limit;
            result.RequestIdentifier = requestIdentifier;
            result.Provenance = provenance;
            result.RawResponse = rawResponse;
            result.IsComplete = records.Length == 0 || start.Int64Value + records.Length >= total.Int64Value;
            CaptureResponseUnknown(root, payload, hasEnvelope, result);
            return result;
        }
    }

    internal static OfficeActionTextRecord ParseRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw Failure("The Office Action text record must be a JSON object.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        OfficeActionTextRecord result = new OfficeActionTextRecord();
        result.Identifier = ReadOptionalString(reader, "id");
        result.ApplicationNumbers = ReadStringArray(reader, "patentApplicationNumber");
        result.PatentNumbers = ReadStringArray(reader, "patentNumber");
        result.BodyText = ReadStringArray(reader, "bodyText");
        result.InventionTitles = ReadStringArray(reader, "inventionTitle");
        result.ApplicationTypeCategories = ReadStringArray(reader, "applicationTypeCategory");
        result.ExaminerEmployeeNumbers = ReadStringArray(reader, "examinerEmployeeNumber");
        result.WorkGroups = ReadStringArray(reader, "workGroup");
        result.TechnologyCenters = ReadStringArray(reader, "techCenter");
        result.NationalClasses = ReadStringArray(reader, "nationalClass");
        result.NationalSubclasses = ReadStringArray(reader, "nationalSubclass");
        result.LegacyDocumentCodeIdentifiers = ReadStringArray(reader, "legacyDocumentCodeIdentifier");
        result.ObsoleteDocumentIdentifiers = ReadStringArray(reader, "obsoleteDocumentIdentifier");
        ApplyRecordIntegers(reader, result);
        ApplyRecordDates(reader, result);
        ApplySections(reader, result);
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        CaptureRecordUnknown(reader, result);
        return result;
    }

    internal static void ApplyRecordIntegers(JsonFieldReader reader, OfficeActionTextRecord result)
    {
        OfficeActionIntegerValue status = ReadOptionalInteger(reader, "applicationStatusNumber");
        result.ApplicationStatusNumber = status.Value;
        result.HasApplicationStatusNumber = status.HasValue;
        OfficeActionIntegerValue confirmation = ReadOptionalInteger(reader, "patentApplicationConfirmationNumber");
        result.ApplicationConfirmationNumber = confirmation.Value;
        result.HasApplicationConfirmationNumber = confirmation.HasValue;
        OfficeActionIntegerValue customer = ReadOptionalInteger(reader, "customerNumber");
        result.CustomerNumber = customer.Value;
        result.HasCustomerNumber = customer.HasValue;
        OfficeActionIntegerValue artUnit = ReadOptionalInteger(reader, "groupArtUnitNumber");
        result.GroupArtUnitNumber = artUnit.Value;
        result.HasGroupArtUnitNumber = artUnit.HasValue;
    }

    internal static void ApplyRecordDates(JsonFieldReader reader, OfficeActionTextRecord result)
    {
        OfficeActionDateValue filing = ReadOptionalDate(reader, "filingDate");
        result.FilingDate = filing.Value;
        result.HasFilingDate = filing.HasValue;
        OfficeActionDateValue effective = ReadOptionalDate(reader, "effectiveFilingDate");
        result.EffectiveFilingDate = effective.Value;
        result.HasEffectiveFilingDate = effective.HasValue;
        OfficeActionDateValue withdrawn = ReadOptionalDate(reader, "applicationDeemedWithdrawnDate");
        result.ApplicationDeemedWithdrawnDate = withdrawn.Value;
        result.HasApplicationDeemedWithdrawnDate = withdrawn.HasValue;
        OfficeActionDateValue grant = ReadOptionalDate(reader, "grantDate");
        result.GrantDate = grant.Value;
        result.HasGrantDate = grant.HasValue;
        OfficeActionDateValue submission = ReadOptionalDate(reader, "submissionDate");
        result.SubmissionDate = submission.Value;
        result.HasSubmissionDate = submission.HasValue;
        OfficeActionDateValue modified = ReadOptionalDate(reader, "lastModifiedTimestamp");
        result.LastModifiedAt = modified.Value;
        result.HasLastModifiedAt = modified.HasValue;
    }

    internal static void ApplySections(JsonFieldReader reader, OfficeActionTextRecord result)
    {
        OfficeActionTextSection sections = result.Sections;
        sections.OfficeActionIdentifiers = ReadStringArray(reader, "sections.officeActionIdentifier");
        sections.ApplicationNumbers = ReadStringArray(reader, "sections.patentApplicationNumber");
        sections.SummaryText = ReadStringArray(reader, "sections.summaryText");
        sections.Section101RejectionText = ReadStringArray(reader, "sections.section101RejectionText");
        sections.Section101RejectionFormParagraphText = ReadStringArray(reader, "sections.section101RejectionFormParagraphText");
        sections.Section102RejectionText = ReadStringArray(reader, "sections.section102RejectionText");
        sections.Section102RejectionFormParagraphText = ReadStringArray(reader, "sections.section102RejectionFormParagraphText");
        sections.Section103RejectionText = ReadStringArray(reader, "sections.section103RejectionText");
        sections.Section103RejectionFormParagraphText = ReadStringArray(reader, "sections.section103RejectionFormParagraphText");
        sections.Section102And103RejectionText = ReadStringArray(reader, "sections.section102AndOr103RejectionText");
        sections.Section102And103RejectionFormParagraphText = ReadStringArray(reader, "sections.section102AndOr103RejectionFormParagraphText");
        sections.Section112RejectionText = ReadStringArray(reader, "sections.section112RejectionText");
        sections.Section112RejectionFormParagraphText = ReadStringArray(reader, "sections.section112RejectionFormParagraphText");
        sections.WithdrawalRejectionText = ReadStringArray(reader, "sections.withdrawalRejectionText");
        sections.TerminalDisclaimerStatusText = ReadStringArray(reader, "sections.terminalDisclaimerStatusText");
        sections.ExaminerEmployeeNumbers = ReadStringArray(reader, "sections.examinerEmployeeNumber");
        sections.GroupArtUnitNumbers = ReadStringArray(reader, "sections.groupArtUnitNumber");
        sections.TechnologyCenterNumbers = ReadStringArray(reader, "sections.techCenterNumber");
        sections.WorkGroupNumbers = ReadStringArray(reader, "sections.workGroupNumber");
        sections.NationalClasses = ReadStringArray(reader, "sections.nationalClass");
        sections.NationalSubclasses = ReadStringArray(reader, "sections.nationalSubclass");
        OfficeActionDateValue filing = ReadOptionalDate(reader, "sections.filingDate");
        sections.FilingDate = filing.Value;
        sections.HasFilingDate = filing.HasValue;
        OfficeActionDateValue grant = ReadOptionalDate(reader, "sections.grantDate");
        sections.GrantDate = grant.Value;
        sections.HasGrantDate = grant.HasValue;
        OfficeActionDateValue submission = ReadOptionalDate(reader, "sections.submissionDate");
        sections.SubmissionDate = submission.Value;
        sections.HasSubmissionDate = submission.HasValue;
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        if (!value.IsValid)
        {
            throw Failure("An Office Action text field is invalid.", value.Path);
        }

        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static string[] ReadStringArray(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadRaw(name);
        if (!value.IsValid)
        {
            throw Failure("An Office Action text field is invalid.", value.Path);
        }

        if (!value.IsPresent || value.IsNull)
        {
            return new string[0];
        }

        global::System.Collections.Generic.List<string> items = new global::System.Collections.Generic.List<string>();
        if (value.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Array)
        {
            int index = 0;
            foreach (global::System.Text.Json.JsonElement item in value.ElementValue.EnumerateArray())
            {
                string? text = ReadTextElement(item);
                if (text == null)
                {
                    throw Failure("An Office Action text array entry is invalid.", value.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]");
                }

                items.Add(text);
                index += 1;
            }
        }
        else
        {
            string? text = ReadTextElement(value.ElementValue);
            if (text == null)
            {
                throw Failure("An Office Action text field is invalid.", value.Path);
            }

            items.Add(text);
        }

        return items.ToArray();
    }

    internal static string? ReadTextElement(global::System.Text.Json.JsonElement element)
    {
        if (element.ValueKind == global::System.Text.Json.JsonValueKind.String)
        {
            return element.GetString();
        }

        if (element.ValueKind == global::System.Text.Json.JsonValueKind.Number || element.ValueKind == global::System.Text.Json.JsonValueKind.True || element.ValueKind == global::System.Text.Json.JsonValueKind.False)
        {
            return element.GetRawText();
        }

        return null;
    }

    internal static OfficeActionIntegerValue ReadOptionalInteger(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadInt32(name);
        if (!value.IsValid)
        {
            throw Failure("An Office Action numeric field is invalid.", value.Path);
        }

        OfficeActionIntegerValue result = new OfficeActionIntegerValue();
        if (value.IsPresent && !value.IsNull)
        {
            result.Value = value.Int32Value;
            result.HasValue = true;
        }

        return result;
    }

    internal static OfficeActionDateValue ReadOptionalDate(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadRaw(name);
        if (!value.IsValid)
        {
            throw Failure("An Office Action date field is invalid.", value.Path);
        }

        OfficeActionDateValue result = new OfficeActionDateValue();
        if (!value.IsPresent || value.IsNull)
        {
            return result;
        }

        global::System.Text.Json.JsonElement dateElement = value.ElementValue;
        if (dateElement.ValueKind == global::System.Text.Json.JsonValueKind.Array)
        {
            bool found = false;
            foreach (global::System.Text.Json.JsonElement item in dateElement.EnumerateArray())
            {
                if (!found)
                {
                    dateElement = item;
                    found = true;
                }
            }

            if (!found || dateElement.ValueKind == global::System.Text.Json.JsonValueKind.Null)
            {
                return result;
            }
        }

        JsonDateParseResult parsed = JsonFieldReader.ParseDateElement(dateElement);
        if (!parsed.IsValid)
        {
            throw Failure("An Office Action date field is invalid.", value.Path);
        }

        result.Value = parsed.Value;
        result.HasValue = true;
        return result;
    }

    internal static string? ResolveRequestIdentifier(JsonFieldReader root, JsonFieldReader payload, bool hasEnvelope, HttpResponseMetadata responseMetadata)
    {
        string? result = responseMetadata.RequestIdentifier;
        JsonFieldValue payloadIdentifier = payload.ReadString("requestIdentifier");
        if (!payloadIdentifier.IsValid)
        {
            throw Failure("The Office Action request identifier is invalid.", payloadIdentifier.Path);
        }

        if (payloadIdentifier.IsPresent && !payloadIdentifier.IsNull)
        {
            result = payloadIdentifier.StringValue;
        }

        if (hasEnvelope)
        {
            JsonFieldValue rootIdentifier = root.ReadString("requestIdentifier");
            if (!rootIdentifier.IsValid)
            {
                throw Failure("The Office Action request identifier is invalid.", rootIdentifier.Path);
            }

            if (rootIdentifier.IsPresent && !rootIdentifier.IsNull)
            {
                result = rootIdentifier.StringValue;
            }
        }

        return result;
    }

    internal static void CaptureRecordUnknown(JsonFieldReader reader, OfficeActionTextRecord result)
    {
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownRecordFields());
        int index;
        for (index = 0; index < unknown.Length; index += 1)
        {
            if (unknown[index].Name.StartsWith("sections.", global::System.StringComparison.Ordinal))
            {
                result.Sections.UnknownFields[unknown[index].Name.Substring("sections.".Length)] = unknown[index].RawJson;
                result.HasSections = true;
            }
            else
            {
                result.UnknownFields[unknown[index].Name] = unknown[index].RawJson;
            }
        }

        string[] sectionNames = KnownSectionFields();
        for (index = 0; index < sectionNames.Length && !result.HasSections; index += 1)
        {
            JsonFieldValue sectionValue = reader.ReadRaw(sectionNames[index]);
            result.HasSections = sectionValue.IsPresent;
        }
    }

    internal static void CaptureResponseUnknown(JsonFieldReader root, JsonFieldReader payload, bool hasEnvelope, OfficeActionTextResponse result)
    {
        JsonUnknownField[] payloadUnknown = payload.CaptureUnknownFields(new string[] { "numFound", "start", "docs", "requestIdentifier" });
        int index;
        for (index = 0; index < payloadUnknown.Length; index += 1)
        {
            result.UnknownFields[payloadUnknown[index].Name] = payloadUnknown[index].RawJson;
        }

        if (hasEnvelope)
        {
            JsonUnknownField[] envelopeUnknown = root.CaptureUnknownFields(new string[] { "response", "requestIdentifier" });
            for (index = 0; index < envelopeUnknown.Length; index += 1)
            {
                result.UnknownFields["envelope." + envelopeUnknown[index].Name] = envelopeUnknown[index].RawJson;
            }
        }
    }

    internal static string[] KnownRecordFields()
    {
        string[] recordFields = new string[]
        {
            "id",
            "patentApplicationNumber",
            "patentNumber",
            "bodyText",
            "inventionTitle",
            "applicationTypeCategory",
            "applicationStatusNumber",
            "patentApplicationConfirmationNumber",
            "customerNumber",
            "groupArtUnitNumber",
            "examinerEmployeeNumber",
            "workGroup",
            "techCenter",
            "nationalClass",
            "nationalSubclass",
            "legacyDocumentCodeIdentifier",
            "obsoleteDocumentIdentifier",
            "filingDate",
            "effectiveFilingDate",
            "applicationDeemedWithdrawnDate",
            "grantDate",
            "submissionDate",
            "lastModifiedTimestamp"
        };
        string[] sectionFields = KnownSectionFields();
        string[] result = new string[recordFields.Length + sectionFields.Length];
        global::System.Array.Copy(recordFields, 0, result, 0, recordFields.Length);
        global::System.Array.Copy(sectionFields, 0, result, recordFields.Length, sectionFields.Length);
        return result;
    }

    internal static string[] KnownSectionFields()
    {
        return new string[]
        {
            "sections.officeActionIdentifier",
            "sections.patentApplicationNumber",
            "sections.summaryText",
            "sections.section101RejectionText",
            "sections.section101RejectionFormParagraphText",
            "sections.section102RejectionText",
            "sections.section102RejectionFormParagraphText",
            "sections.section103RejectionText",
            "sections.section103RejectionFormParagraphText",
            "sections.section102AndOr103RejectionText",
            "sections.section102AndOr103RejectionFormParagraphText",
            "sections.section112RejectionText",
            "sections.section112RejectionFormParagraphText",
            "sections.withdrawalRejectionText",
            "sections.terminalDisclaimerStatusText",
            "sections.examinerEmployeeNumber",
            "sections.groupArtUnitNumber",
            "sections.techCenterNumber",
            "sections.workGroupNumber",
            "sections.nationalClass",
            "sections.nationalSubclass",
            "sections.filingDate",
            "sections.grantDate",
            "sections.submissionDate"
        };
    }

    internal static ParseException Failure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "Office Action text search", path, 0L, false, 0, false, null);
    }
}
