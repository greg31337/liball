namespace uspto;

internal class OfficeActionRejectionBooleanValue
{
    internal bool Value = false;
    internal bool HasValue = false;
}

internal class OfficeActionRejectionIntegerValue
{
    internal int Value = 0;
    internal bool HasValue = false;
}

internal static class OfficeActionRejectionParser
{
    internal static OfficeActionRejectionResponse Parse(string json, SearchRequest request, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An Office Action rejection response is required.", "json", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("An Office Action rejection search request is required.", "request", "REQUIRED", null);
        }

        if (request.OffsetPage == null)
        {
            throw new ValidationException("Office Action rejection page coordinates are required.", "OffsetPage", "REQUIRED", null);
        }

        if (responseMetadata == null)
        {
            throw new ValidationException("Office Action rejection response metadata is required.", "responseMetadata", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            if (document.RootElement.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw Failure("The Office Action rejection response must be a JSON object.", "$");
            }

            JsonFieldReader root = new JsonFieldReader(document.RootElement, "$");
            JsonFieldValue envelope = root.ReadObject("response");
            if (!envelope.IsValid)
            {
                throw Failure("The Office Action rejection response envelope is invalid.", envelope.Path);
            }

            JsonFieldReader payload = root;
            bool hasEnvelope = false;
            if (envelope.IsPresent)
            {
                if (envelope.IsNull || !envelope.HasElementValue)
                {
                    throw Failure("The Office Action rejection response envelope is invalid.", envelope.Path);
                }

                payload = new JsonFieldReader(envelope.ElementValue, envelope.Path);
                hasEnvelope = true;
            }

            JsonFieldValue total = payload.ReadInt64("numFound");
            if (!total.IsPresent || !total.IsValid || total.IsNull || total.Int64Value < 0L)
            {
                throw Failure("The Office Action rejection response has an invalid total.", total.Path);
            }

            JsonFieldValue start = payload.ReadInt64("start");
            if (!start.IsPresent || !start.IsValid || start.IsNull || start.Int64Value < 0L)
            {
                throw Failure("The Office Action rejection response has an invalid start offset.", start.Path);
            }

            JsonFieldValue documents = payload.ReadArray("docs");
            if (!documents.IsPresent || !documents.IsValid || documents.IsNull)
            {
                throw Failure("The Office Action rejection response has an invalid document collection.", documents.Path);
            }

            if (documents.ArrayItems.Length != 0 && (start.Int64Value > total.Int64Value || documents.ArrayItems.Length > total.Int64Value - start.Int64Value))
            {
                throw Failure("The Office Action rejection page extends beyond the reported total.", documents.Path);
            }

            string? requestIdentifier = ResolveRejectionRequestIdentifier(root, payload, hasEnvelope, responseMetadata);
            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            provenance.DatasetIdentifier = "oa_rejections";
            provenance.DatasetVersion = "v2";
            OfficeActionRejection[] rejections = new OfficeActionRejection[documents.ArrayItems.Length];
            int index;
            for (index = 0; index < documents.ArrayItems.Length; index += 1)
            {
                rejections[index] = ParseRejection(documents.ArrayItems[index], documents.ArrayItemPaths[index], provenance);
            }

            OfficeActionRejectionResponse result = new OfficeActionRejectionResponse();
            result.Request = request;
            result.Rejections = rejections;
            result.Count = rejections.Length;
            result.Total = total.Int64Value;
            result.HasTotal = true;
            result.Start = start.Int64Value;
            result.Page.Offset = start.Int64Value;
            result.Page.Limit = request.OffsetPage.Limit;
            result.RequestIdentifier = requestIdentifier;
            result.Provenance = provenance;
            result.RawResponse = rawResponse;
            result.IsComplete = start.Int64Value >= total.Int64Value || start.Int64Value + rejections.Length >= total.Int64Value;
            CaptureRejectionResponseUnknown(root, payload, hasEnvelope, result);
            return result;
        }
    }

    internal static OfficeActionRejection ParseRejection(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw Failure("The Office Action rejection record must be a JSON object.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        OfficeActionRejection result = new OfficeActionRejection();
        result.Identifier = ReadRejectionString(reader, "id");
        result.ApplicationNumber = ReadRejectionString(reader, "patentApplicationNumber");
        result.LegacyDocumentCodeIdentifier = ReadRejectionString(reader, "legacyDocumentCodeIdentifier");
        result.ActionTypeCategory = ReadRejectionString(reader, "actionTypeCategory");
        result.LegalSectionCode = ReadRejectionString(reader, "legalSectionCode");
        result.GroupArtUnitNumber = ReadRejectionString(reader, "groupArtUnitNumber");
        result.NationalClass = ReadRejectionString(reader, "nationalClass");
        result.NationalSubclass = ReadRejectionString(reader, "nationalSubclass");
        result.ParagraphNumber = ReadRejectionString(reader, "paragraphNumber");
        result.ObsoleteDocumentIdentifier = ReadRejectionString(reader, "obsoleteDocumentIdentifier");
        result.CreateUserIdentifier = ReadRejectionString(reader, "createUserIdentifier");
        result.ClaimNumbers = ReadClaimNumbers(reader);
        ApplyRejectionDates(reader, result);
        ApplyRejectionFlags(reader, result);
        ApplyRejectionCounts(reader, result);
        ApplyPrecedentialIndicators(reader, result);
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownRejectionFields());
        MergeRejectionUnknown(result.UnknownFields, unknown, string.Empty);
        return result;
    }

    internal static void ApplyRejectionDates(JsonFieldReader reader, OfficeActionRejection result)
    {
        JsonFieldValue submission = reader.ReadDate("submissionDate");
        if (!submission.IsValid)
        {
            throw Failure("The Office Action rejection submission date is invalid.", submission.Path);
        }

        if (submission.IsPresent && !submission.IsNull)
        {
            result.SubmissionDate = submission.DateValue;
            result.HasSubmissionDate = true;
        }

        JsonFieldValue created = reader.ReadDate("createDateTime");
        if (!created.IsValid)
        {
            throw Failure("The Office Action rejection creation time is invalid.", created.Path);
        }

        if (created.IsPresent && !created.IsNull)
        {
            result.CreatedAt = created.DateValue;
            result.HasCreatedAt = true;
        }
    }

    internal static void ApplyRejectionFlags(JsonFieldReader reader, OfficeActionRejection result)
    {
        OfficeActionRejectionBooleanValue section101 = ReadRejectionBoolean(reader, "hasRej101");
        result.HasSection101Rejection = section101.Value;
        result.HasSection101RejectionState = section101.HasValue;
        OfficeActionRejectionBooleanValue section102 = ReadRejectionBoolean(reader, "hasRej102");
        result.HasSection102Rejection = section102.Value;
        result.HasSection102RejectionState = section102.HasValue;
        OfficeActionRejectionBooleanValue section103 = ReadRejectionBoolean(reader, "hasRej103");
        result.HasSection103Rejection = section103.Value;
        result.HasSection103RejectionState = section103.HasValue;
        OfficeActionRejectionBooleanValue section112 = ReadRejectionBoolean(reader, "hasRej112");
        result.HasSection112Rejection = section112.Value;
        result.HasSection112RejectionState = section112.HasValue;
        OfficeActionRejectionBooleanValue doublePatenting = ReadRejectionBoolean(reader, "hasRejDP");
        result.HasDoublePatentingRejection = doublePatenting.Value;
        result.HasDoublePatentingRejectionState = doublePatenting.HasValue;
    }

    internal static void ApplyRejectionCounts(JsonFieldReader reader, OfficeActionRejection result)
    {
        OfficeActionRejectionIntegerValue maximum = ReadRejectionInteger(reader, "cite103Max");
        result.MaximumSection103CitationCount = maximum.Value;
        result.HasMaximumSection103CitationCount = maximum.HasValue;
        OfficeActionRejectionIntegerValue single = ReadRejectionInteger(reader, "cite103EQ1");
        result.SingleCitationSection103Indicator = single.Value;
        result.HasSingleCitationSection103Indicator = single.HasValue;
        OfficeActionRejectionIntegerValue many = ReadRejectionInteger(reader, "cite103GT3");
        result.MoreThanThreeCitationsSection103Indicator = many.Value;
        result.HasMoreThanThreeCitationsSection103Indicator = many.HasValue;
        OfficeActionRejectionIntegerValue closing = ReadRejectionInteger(reader, "closingMissing");
        result.ClosingMissingIndicator = closing.Value;
        result.HasClosingMissingIndicator = closing.HasValue;
        OfficeActionRejectionIntegerValue mismatch = ReadRejectionInteger(reader, "rejectFormMissmatch");
        result.RejectionFormMismatchIndicator = mismatch.Value;
        result.HasRejectionFormMismatchIndicator = mismatch.HasValue;
        OfficeActionRejectionIntegerValue paragraph = ReadRejectionInteger(reader, "formParagraphMissing");
        result.FormParagraphMissingIndicator = paragraph.Value;
        result.HasFormParagraphMissingIndicator = paragraph.HasValue;
        OfficeActionRejectionIntegerValue header = ReadRejectionInteger(reader, "headerMissing");
        result.HeaderMissingIndicator = header.Value;
        result.HasHeaderMissingIndicator = header.HasValue;
    }

    internal static void ApplyPrecedentialIndicators(JsonFieldReader reader, OfficeActionRejection result)
    {
        OfficeActionRejectionBooleanValue bilski = ReadRejectionBoolean(reader, "bilskiIndicator");
        result.BilskiIndicator = bilski.Value;
        result.HasBilskiIndicator = bilski.HasValue;
        OfficeActionRejectionBooleanValue mayo = ReadRejectionBoolean(reader, "mayoIndicator");
        result.MayoIndicator = mayo.Value;
        result.HasMayoIndicator = mayo.HasValue;
        OfficeActionRejectionBooleanValue alice = ReadRejectionBoolean(reader, "aliceIndicator");
        result.AliceIndicator = alice.Value;
        result.HasAliceIndicator = alice.HasValue;
        OfficeActionRejectionBooleanValue myriad = ReadRejectionBoolean(reader, "myriadIndicator");
        result.MyriadIndicator = myriad.Value;
        result.HasMyriadIndicator = myriad.HasValue;
        OfficeActionRejectionBooleanValue allowed = ReadRejectionBoolean(reader, "allowedClaimIndicator");
        result.AllowedClaimIndicator = allowed.Value;
        result.HasAllowedClaimIndicator = allowed.HasValue;
    }

    internal static string[] ReadClaimNumbers(JsonFieldReader reader)
    {
        JsonFieldValue value = reader.ReadRaw("claimNumberArrayDocument");
        if (!value.IsPresent || value.IsNull)
        {
            return new string[0];
        }

        global::System.Collections.Generic.List<string> claims = new global::System.Collections.Generic.List<string>();
        if (value.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Array)
        {
            int index = 0;
            foreach (global::System.Text.Json.JsonElement item in value.ElementValue.EnumerateArray())
            {
                AddClaimText(claims, item, value.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]");
                index += 1;
            }
        }
        else
        {
            AddClaimText(claims, value.ElementValue, value.Path);
        }

        return claims.ToArray();
    }

    internal static void AddClaimText(global::System.Collections.Generic.List<string> claims, global::System.Text.Json.JsonElement item, string path)
    {
        string? text = null;
        if (item.ValueKind == global::System.Text.Json.JsonValueKind.String)
        {
            text = item.GetString();
        }
        else if (item.ValueKind == global::System.Text.Json.JsonValueKind.Number)
        {
            text = item.GetRawText();
        }

        if (text == null)
        {
            throw Failure("An Office Action rejection claim number is invalid.", path);
        }

        string[] parts = text.Split(',', global::System.StringSplitOptions.None);
        int index;
        for (index = 0; index < parts.Length; index += 1)
        {
            string claim = parts[index].Trim();
            if (claim.Length != 0)
            {
                claims.Add(claim);
            }
        }
    }

    internal static string? ReadRejectionString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        if (!value.IsValid)
        {
            throw Failure("An Office Action rejection field is invalid.", value.Path);
        }

        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static OfficeActionRejectionBooleanValue ReadRejectionBoolean(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadBoolean(name);
        if (!value.IsValid)
        {
            throw Failure("An Office Action rejection indicator is invalid.", value.Path);
        }

        OfficeActionRejectionBooleanValue result = new OfficeActionRejectionBooleanValue();
        if (value.IsPresent && !value.IsNull)
        {
            result.Value = value.BooleanValue;
            result.HasValue = true;
        }

        return result;
    }

    internal static OfficeActionRejectionIntegerValue ReadRejectionInteger(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadInt32(name);
        if (!value.IsValid)
        {
            throw Failure("An Office Action rejection numeric indicator is invalid.", value.Path);
        }

        OfficeActionRejectionIntegerValue result = new OfficeActionRejectionIntegerValue();
        if (value.IsPresent && !value.IsNull)
        {
            result.Value = value.Int32Value;
            result.HasValue = true;
        }

        return result;
    }

    internal static string? ResolveRejectionRequestIdentifier(JsonFieldReader root, JsonFieldReader payload, bool hasEnvelope, HttpResponseMetadata responseMetadata)
    {
        string? result = responseMetadata.RequestIdentifier;
        JsonFieldValue payloadIdentifier = payload.ReadString("requestIdentifier");
        if (!payloadIdentifier.IsValid)
        {
            throw Failure("The Office Action rejection request identifier is invalid.", payloadIdentifier.Path);
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
                throw Failure("The Office Action rejection request identifier is invalid.", rootIdentifier.Path);
            }

            if (rootIdentifier.IsPresent && !rootIdentifier.IsNull)
            {
                result = rootIdentifier.StringValue;
            }
        }

        return result;
    }

    internal static void CaptureRejectionResponseUnknown(JsonFieldReader root, JsonFieldReader payload, bool hasEnvelope, OfficeActionRejectionResponse result)
    {
        JsonUnknownField[] payloadUnknown = payload.CaptureUnknownFields(new string[] { "numFound", "start", "docs", "requestIdentifier" });
        MergeRejectionUnknown(result.UnknownFields, payloadUnknown, string.Empty);
        if (hasEnvelope)
        {
            JsonUnknownField[] envelopeUnknown = root.CaptureUnknownFields(new string[] { "response", "requestIdentifier" });
            MergeRejectionUnknown(result.UnknownFields, envelopeUnknown, "envelope.");
        }
    }

    internal static void MergeRejectionUnknown(global::System.Collections.Generic.Dictionary<string, string> destination, JsonUnknownField[] fields, string prefix)
    {
        int index;
        for (index = 0; index < fields.Length; index += 1)
        {
            destination[prefix + fields[index].Name] = fields[index].RawJson;
        }
    }

    internal static string[] KnownRejectionFields()
    {
        return new string[]
        {
            "id",
            "patentApplicationNumber",
            "legacyDocumentCodeIdentifier",
            "actionTypeCategory",
            "legalSectionCode",
            "groupArtUnitNumber",
            "nationalClass",
            "nationalSubclass",
            "paragraphNumber",
            "obsoleteDocumentIdentifier",
            "createUserIdentifier",
            "claimNumberArrayDocument",
            "submissionDate",
            "createDateTime",
            "hasRej101",
            "hasRej102",
            "hasRej103",
            "hasRej112",
            "hasRejDP",
            "cite103Max",
            "cite103EQ1",
            "cite103GT3",
            "closingMissing",
            "rejectFormMissmatch",
            "formParagraphMissing",
            "headerMissing",
            "bilskiIndicator",
            "mayoIndicator",
            "aliceIndicator",
            "myriadIndicator",
            "allowedClaimIndicator"
        };
    }

    internal static ParseException Failure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "Office Action rejection search", path, 0L, false, 0, false, null);
    }
}
