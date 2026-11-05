namespace uspto;

internal static class PatentsViewTextParser
{
    internal static PatentsViewClaimResponse ParseClaimResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache, string collectionName, bool isPublication)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = PatentsViewPatentParser.ValidateResponseRoot(document.RootElement, collectionName);
            PatentDataProvenance provenance = PatentsViewPatentParser.CreatePatentsViewProvenance(responseMetadata, collectionName, fromCache);
            JsonFieldValue records = root.ReadArray(collectionName);
            PatentsViewClaim[] claims = new PatentsViewClaim[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                claims[index] = ParseClaimRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance, isPublication);
            }

            PatentsViewClaimResponse result = new PatentsViewClaimResponse();
            result.Claims = claims;
            result.Metadata = PatentsViewPatentParser.ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, collectionName, provenance);
            return result;
        }
    }

    internal static PatentsViewSummaryResponse ParseSummaryResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache, string collectionName, bool isPublication)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = PatentsViewPatentParser.ValidateResponseRoot(document.RootElement, collectionName);
            PatentDataProvenance provenance = PatentsViewPatentParser.CreatePatentsViewProvenance(responseMetadata, collectionName, fromCache);
            JsonFieldValue records = root.ReadArray(collectionName);
            PatentsViewSummary[] summaries = new PatentsViewSummary[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                summaries[index] = ParseSummaryRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance, isPublication);
            }

            PatentsViewSummaryResponse result = new PatentsViewSummaryResponse();
            result.Summaries = summaries;
            result.Metadata = PatentsViewPatentParser.ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, collectionName, provenance);
            return result;
        }
    }

    internal static PatentsViewDescriptionResponse ParseDescriptionResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache, string collectionName, bool isPublication)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = PatentsViewPatentParser.ValidateResponseRoot(document.RootElement, collectionName);
            PatentDataProvenance provenance = PatentsViewPatentParser.CreatePatentsViewProvenance(responseMetadata, collectionName, fromCache);
            JsonFieldValue records = root.ReadArray(collectionName);
            PatentsViewDescription[] descriptions = new PatentsViewDescription[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                descriptions[index] = ParseDescriptionRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance, isPublication);
            }

            PatentsViewDescriptionResponse result = new PatentsViewDescriptionResponse();
            result.Descriptions = descriptions;
            result.Metadata = PatentsViewPatentParser.ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, collectionName, provenance);
            return result;
        }
    }

    internal static PatentsViewDrawingDescriptionResponse ParseDrawingResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache, string collectionName, bool isPublication)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = PatentsViewPatentParser.ValidateResponseRoot(document.RootElement, collectionName);
            PatentDataProvenance provenance = PatentsViewPatentParser.CreatePatentsViewProvenance(responseMetadata, collectionName, fromCache);
            JsonFieldValue records = root.ReadArray(collectionName);
            PatentsViewDrawingDescription[] drawings = new PatentsViewDrawingDescription[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                drawings[index] = ParseDrawingRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance, isPublication);
            }

            PatentsViewDrawingDescriptionResponse result = new PatentsViewDrawingDescriptionResponse();
            result.DrawingDescriptions = drawings;
            result.Metadata = PatentsViewPatentParser.ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, collectionName, provenance);
            return result;
        }
    }

    internal static PatentsViewClaim ParseClaimRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance, bool isPublication)
    {
        PatentsViewPatentParser.ValidateRecordObject(element, path, "claim");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewClaim claim = new PatentsViewClaim();
        ApplyTextIdentifier(reader, claim, isPublication);
        claim.ClaimNumber = PatentsViewPatentParser.ReadOptionalText(reader, "claim_number");
        claim.Text = PatentsViewPatentParser.ReadOptionalText(reader, "claim_text");
        claim.DependsOnClaim = PatentsViewPatentParser.ReadOptionalText(reader, "claim_dependent");
        JsonFieldValue sequence = reader.ReadInt32("claim_sequence");
        PatentsViewPatentParser.ValidateOptionalValue(sequence, "The PatentsView claim sequence is invalid.");
        if (sequence.IsPresent && !sequence.IsNull)
        {
            if (sequence.Int32Value < 0)
            {
                throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView claim sequence cannot be negative.", sequence.Path);
            }

            claim.Sequence = sequence.Int32Value;
            claim.HasSequence = true;
        }

        JsonFieldValue exemplary = reader.ReadBoolean("exemplary");
        PatentsViewPatentParser.ValidateOptionalValue(exemplary, "The PatentsView exemplary-claim indicator is invalid.");
        if (exemplary.IsPresent && !exemplary.IsNull)
        {
            claim.IsExemplary = exemplary.BooleanValue;
            claim.HasIsExemplary = true;
        }

        claim.Provenance = OdpJsonParser.CopyProvenance(provenance);
        PatentsViewPatentParser.CopyUnknownFields(reader.CaptureUnknownFields(new string[] { "patent_id", "document_number", "claim_sequence", "claim_number", "claim_text", "exemplary", "claim_dependent" }), claim.AdditionalFields);
        return claim;
    }

    internal static PatentsViewSummary ParseSummaryRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance, bool isPublication)
    {
        PatentsViewPatentParser.ValidateRecordObject(element, path, "brief summary");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewSummary summary = new PatentsViewSummary();
        ApplySummaryIdentifier(reader, summary, isPublication);
        summary.Text = PatentsViewPatentParser.ReadOptionalText(reader, "summary_text");
        summary.Provenance = OdpJsonParser.CopyProvenance(provenance);
        PatentsViewPatentParser.CopyUnknownFields(reader.CaptureUnknownFields(new string[] { "patent_id", "document_number", "summary_text" }), summary.AdditionalFields);
        return summary;
    }

    internal static PatentsViewDescription ParseDescriptionRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance, bool isPublication)
    {
        PatentsViewPatentParser.ValidateRecordObject(element, path, "detailed description");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewDescription description = new PatentsViewDescription();
        ApplyDescriptionIdentifier(reader, description, isPublication);
        description.Text = PatentsViewPatentParser.ReadOptionalText(reader, "description_text");
        JsonFieldValue length = reader.ReadInt32("description_length");
        PatentsViewPatentParser.ValidateOptionalValue(length, "The PatentsView description length is invalid.");
        if (length.IsPresent && !length.IsNull)
        {
            if (length.Int32Value < 0)
            {
                throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView description length cannot be negative.", length.Path);
            }

            description.Length = length.Int32Value;
            description.HasLength = true;
        }

        description.Provenance = OdpJsonParser.CopyProvenance(provenance);
        PatentsViewPatentParser.CopyUnknownFields(reader.CaptureUnknownFields(new string[] { "patent_id", "document_number", "description_text", "description_length" }), description.AdditionalFields);
        return description;
    }

    internal static PatentsViewDrawingDescription ParseDrawingRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance, bool isPublication)
    {
        PatentsViewPatentParser.ValidateRecordObject(element, path, "drawing description");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewDrawingDescription drawing = new PatentsViewDrawingDescription();
        ApplyDrawingIdentifier(reader, drawing, isPublication);
        drawing.Text = PatentsViewPatentParser.ReadOptionalText(reader, "draw_desc_text");
        JsonFieldValue sequence = reader.ReadInt32("draw_desc_sequence");
        PatentsViewPatentParser.ValidateOptionalValue(sequence, "The PatentsView drawing-description sequence is invalid.");
        if (sequence.IsPresent && !sequence.IsNull)
        {
            if (sequence.Int32Value < 1)
            {
                throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView drawing-description sequence must be positive.", sequence.Path);
            }

            drawing.Sequence = sequence.Int32Value;
            drawing.HasSequence = true;
        }

        drawing.Provenance = OdpJsonParser.CopyProvenance(provenance);
        PatentsViewPatentParser.CopyUnknownFields(reader.CaptureUnknownFields(new string[] { "patent_id", "document_number", "draw_desc_sequence", "draw_desc_text" }), drawing.AdditionalFields);
        return drawing;
    }

    internal static void ApplyTextIdentifier(JsonFieldReader reader, PatentsViewClaim claim, bool isPublication)
    {
        if (isPublication)
        {
            claim.DocumentNumber = PatentsViewPatentParser.ReadOptionalText(reader, "document_number");
        }
        else
        {
            claim.PatentIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "patent_id");
        }
    }

    internal static void ApplySummaryIdentifier(JsonFieldReader reader, PatentsViewSummary summary, bool isPublication)
    {
        if (isPublication)
        {
            summary.DocumentNumber = PatentsViewPatentParser.ReadOptionalText(reader, "document_number");
        }
        else
        {
            summary.PatentIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "patent_id");
        }
    }

    internal static void ApplyDescriptionIdentifier(JsonFieldReader reader, PatentsViewDescription description, bool isPublication)
    {
        if (isPublication)
        {
            description.DocumentNumber = PatentsViewPatentParser.ReadOptionalText(reader, "document_number");
        }
        else
        {
            description.PatentIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "patent_id");
        }
    }

    internal static void ApplyDrawingIdentifier(JsonFieldReader reader, PatentsViewDrawingDescription drawing, bool isPublication)
    {
        if (isPublication)
        {
            drawing.DocumentNumber = PatentsViewPatentParser.ReadOptionalText(reader, "document_number");
        }
        else
        {
            drawing.PatentIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "patent_id");
        }
    }
}
