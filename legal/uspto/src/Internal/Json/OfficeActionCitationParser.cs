namespace uspto;

internal static class OfficeActionCitationParser
{
    internal static OfficeActionCitationResponse Parse(string json, SearchRequest request, HttpResponseMetadata responseMetadata, string? rawResponse)
    {
        if (json == null)
        {
            throw new ValidationException("An Office Action citation response is required.", "json", "REQUIRED", null);
        }

        if (request == null)
        {
            throw new ValidationException("An Office Action citation search request is required.", "request", "REQUIRED", null);
        }

        if (request.OffsetPage == null)
        {
            throw new ValidationException("Office Action citation page coordinates are required.", "OffsetPage", "REQUIRED", null);
        }

        if (responseMetadata == null)
        {
            throw new ValidationException("Office Action citation response metadata is required.", "responseMetadata", "REQUIRED", null);
        }

        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            if (document.RootElement.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw Failure("The Office Action citation response must be a JSON object.", "$");
            }

            JsonFieldReader root = new JsonFieldReader(document.RootElement, "$");
            JsonFieldValue envelope = root.ReadObject("response");
            if (!envelope.IsValid)
            {
                throw Failure("The Office Action citation response envelope is invalid.", envelope.Path);
            }

            JsonFieldReader payload = root;
            bool hasEnvelope = false;
            if (envelope.IsPresent)
            {
                if (envelope.IsNull || !envelope.HasElementValue)
                {
                    throw Failure("The Office Action citation response envelope is invalid.", envelope.Path);
                }

                payload = new JsonFieldReader(envelope.ElementValue, envelope.Path);
                hasEnvelope = true;
            }

            JsonFieldValue total = payload.ReadInt64("numFound");
            if (!total.IsPresent || !total.IsValid || total.IsNull || total.Int64Value < 0L)
            {
                throw Failure("The Office Action citation response has an invalid total.", total.Path);
            }

            JsonFieldValue start = payload.ReadInt64("start");
            if (!start.IsPresent || !start.IsValid || start.IsNull || start.Int64Value < 0L)
            {
                throw Failure("The Office Action citation response has an invalid start offset.", start.Path);
            }

            JsonFieldValue documents = payload.ReadArray("docs");
            if (!documents.IsPresent || !documents.IsValid || documents.IsNull)
            {
                throw Failure("The Office Action citation response has an invalid document collection.", documents.Path);
            }

            if (documents.ArrayItems.Length != 0 && (start.Int64Value > total.Int64Value || documents.ArrayItems.Length > total.Int64Value - start.Int64Value))
            {
                throw Failure("The Office Action citation page extends beyond the reported total.", documents.Path);
            }

            string? requestIdentifier = ResolveRequestIdentifier(root, payload, hasEnvelope, responseMetadata);
            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            provenance.DatasetIdentifier = "oa_citations";
            provenance.DatasetVersion = "v2";
            OfficeActionCitation[] citations = new OfficeActionCitation[documents.ArrayItems.Length];
            int index;
            for (index = 0; index < documents.ArrayItems.Length; index += 1)
            {
                citations[index] = ParseCitation(documents.ArrayItems[index], documents.ArrayItemPaths[index], provenance);
            }

            OfficeActionCitationResponse result = new OfficeActionCitationResponse();
            result.Request = request;
            result.Citations = citations;
            result.Count = citations.Length;
            result.Total = total.Int64Value;
            result.HasTotal = true;
            result.Start = start.Int64Value;
            result.Page.Offset = start.Int64Value;
            result.Page.Limit = request.OffsetPage.Limit;
            result.RequestIdentifier = requestIdentifier;
            result.Provenance = provenance;
            result.RawResponse = rawResponse;
            result.IsComplete = start.Int64Value >= total.Int64Value || start.Int64Value + citations.Length >= total.Int64Value;
            CaptureResponseUnknown(root, payload, hasEnvelope, result);
            return result;
        }
    }

    internal static OfficeActionCitation ParseCitation(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        if (element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            throw Failure("The Office Action citation record must be a JSON object.", path);
        }

        JsonFieldReader reader = new JsonFieldReader(element, path);
        OfficeActionCitation result = new OfficeActionCitation();
        result.Identifier = ReadOptionalString(reader, "id");
        result.ApplicationNumber = ReadOptionalString(reader, "patentApplicationNumber");
        result.ActionTypeCategory = ReadOptionalString(reader, "actionTypeCategory");
        result.LegalSectionCode = ReadOptionalString(reader, "legalSectionCode");
        result.ReferenceIdentifier = ReadOptionalString(reader, "referenceIdentifier");
        result.ParsedReferenceIdentifier = ReadOptionalString(reader, "parsedReferenceIdentifier");
        result.GroupArtUnitNumber = ReadOptionalString(reader, "groupArtUnitNumber");
        result.WorkGroup = ReadOptionalString(reader, "workGroup");
        result.TechnologyCenter = ReadOptionalString(reader, "techCenter");
        result.ParagraphNumber = ReadOptionalString(reader, "paragraphNumber");
        result.CreateUserIdentifier = ReadOptionalString(reader, "createUserIdentifier");
        result.ObsoleteDocumentIdentifier = ReadOptionalString(reader, "obsoleteDocumentIdentifier");
        result.CitedIdentifier = CitedIdentifierNormalizer.NormalizeCitedIdentifier(result.ReferenceIdentifier, result.ParsedReferenceIdentifier, null, null, null, false, false);
        ApplyIndicators(reader, result);
        JsonFieldValue created = reader.ReadDate("createDateTime");
        if (!created.IsValid)
        {
            throw Failure("The Office Action citation creation time is invalid.", created.Path);
        }

        if (created.IsPresent && !created.IsNull)
        {
            result.CreatedAt = created.DateValue;
            result.HasCreatedAt = true;
        }

        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownCitationFields());
        MergeUnknown(result.UnknownFields, unknown, string.Empty);
        return result;
    }

    internal static void ApplyIndicators(JsonFieldReader reader, OfficeActionCitation result)
    {
        JsonFieldValue applicant = reader.ReadBoolean("applicantCitedExaminerReferenceIndicator");
        ValidateBoolean(applicant);
        if (applicant.IsPresent && !applicant.IsNull)
        {
            result.ApplicantCitedExaminerReferenceIndicator = applicant.BooleanValue;
            result.HasApplicantCitedExaminerReferenceIndicator = true;
        }

        JsonFieldValue examiner = reader.ReadBoolean("examinerCitedReferenceIndicator");
        ValidateBoolean(examiner);
        if (examiner.IsPresent && !examiner.IsNull)
        {
            result.ExaminerCitedReferenceIndicator = examiner.BooleanValue;
            result.HasExaminerCitedReferenceIndicator = true;
        }

        JsonFieldValue officeAction = reader.ReadBoolean("officeActionCitationReferenceIndicator");
        ValidateBoolean(officeAction);
        if (officeAction.IsPresent && !officeAction.IsNull)
        {
            result.OfficeActionCitationReferenceIndicator = officeAction.BooleanValue;
            result.HasOfficeActionCitationReferenceIndicator = true;
        }
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        if (!value.IsValid)
        {
            throw Failure("An Office Action citation field is invalid.", value.Path);
        }

        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return value.StringValue;
    }

    internal static void ValidateBoolean(JsonFieldValue value)
    {
        if (!value.IsValid)
        {
            throw Failure("An Office Action citation indicator is invalid.", value.Path);
        }
    }

    internal static string? ResolveRequestIdentifier(JsonFieldReader root, JsonFieldReader payload, bool hasEnvelope, HttpResponseMetadata responseMetadata)
    {
        string? result = responseMetadata.RequestIdentifier;
        JsonFieldValue payloadIdentifier = payload.ReadString("requestIdentifier");
        if (!payloadIdentifier.IsValid)
        {
            throw Failure("The Office Action citation request identifier is invalid.", payloadIdentifier.Path);
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
                throw Failure("The Office Action citation request identifier is invalid.", rootIdentifier.Path);
            }

            if (rootIdentifier.IsPresent && !rootIdentifier.IsNull)
            {
                result = rootIdentifier.StringValue;
            }
        }

        return result;
    }

    internal static void CaptureResponseUnknown(JsonFieldReader root, JsonFieldReader payload, bool hasEnvelope, OfficeActionCitationResponse result)
    {
        JsonUnknownField[] payloadUnknown = payload.CaptureUnknownFields(new string[] { "numFound", "start", "docs", "requestIdentifier" });
        MergeUnknown(result.UnknownFields, payloadUnknown, string.Empty);
        if (hasEnvelope)
        {
            JsonUnknownField[] envelopeUnknown = root.CaptureUnknownFields(new string[] { "response", "requestIdentifier" });
            MergeUnknown(result.UnknownFields, envelopeUnknown, "envelope.");
        }
    }

    internal static void MergeUnknown(global::System.Collections.Generic.Dictionary<string, string> destination, JsonUnknownField[] fields, string prefix)
    {
        int index;
        for (index = 0; index < fields.Length; index += 1)
        {
            destination[prefix + fields[index].Name] = fields[index].RawJson;
        }
    }

    internal static string[] KnownCitationFields()
    {
        return new string[]
        {
            "id",
            "patentApplicationNumber",
            "actionTypeCategory",
            "legalSectionCode",
            "referenceIdentifier",
            "parsedReferenceIdentifier",
            "groupArtUnitNumber",
            "workGroup",
            "techCenter",
            "paragraphNumber",
            "applicantCitedExaminerReferenceIndicator",
            "examinerCitedReferenceIndicator",
            "officeActionCitationReferenceIndicator",
            "createUserIdentifier",
            "createDateTime",
            "obsoleteDocumentIdentifier"
        };
    }

    internal static ParseException Failure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "Office Action citation search", path, 0L, false, 0, false, null);
    }
}
