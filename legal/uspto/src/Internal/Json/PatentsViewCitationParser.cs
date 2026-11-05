namespace uspto;

internal static class PatentsViewCitationParser
{
    internal static PatentsViewCitationResponse ParseCitationResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache, string collectionName, string citationType)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = PatentsViewPatentParser.ValidateResponseRoot(document.RootElement, collectionName);
            PatentDataProvenance provenance = PatentsViewPatentParser.CreatePatentsViewProvenance(responseMetadata, collectionName, fromCache);
            JsonFieldValue records = root.ReadArray(collectionName);
            PatentsViewCitation[] citations = new PatentsViewCitation[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                citations[index] = ParseCitationRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance, citationType);
            }

            PatentsViewCitationResponse result = new PatentsViewCitationResponse();
            result.Citations = citations;
            result.Metadata = PatentsViewPatentParser.ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, collectionName, provenance);
            return result;
        }
    }

    internal static PatentsViewRelatedApplicationResponse ParseRelatedApplicationResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache, bool isPublication)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = PatentsViewPatentParser.ValidateResponseRoot(document.RootElement, "rel_app_texts");
            string datasetIdentifier = isPublication ? "pg_rel_app_text" : "g_rel_app_text";
            PatentDataProvenance provenance = PatentsViewPatentParser.CreatePatentsViewProvenance(responseMetadata, datasetIdentifier, fromCache);
            JsonFieldValue records = root.ReadArray("rel_app_texts");
            PatentsViewRelatedApplication[] related = new PatentsViewRelatedApplication[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                related[index] = ParseRelatedApplicationRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance, isPublication);
            }

            PatentsViewRelatedApplicationResponse result = new PatentsViewRelatedApplicationResponse();
            result.RelatedApplications = related;
            result.Metadata = PatentsViewPatentParser.ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, "rel_app_texts", provenance);
            return result;
        }
    }

    internal static PatentsViewCitation ParseCitationRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance, string citationType)
    {
        PatentsViewPatentParser.ValidateRecordObject(element, path, "citation");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewCitation citation = new PatentsViewCitation();
        citation.PatentIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "patent_id");
        citation.DocumentNumber = PatentsViewPatentParser.ReadOptionalText(reader, "document_number");
        citation.SourceResourceReference = PatentsViewPatentParser.ReadOptionalText(reader, "patent");
        citation.CitationType = citationType;
        citation.CitedPatentIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "citation_patent_id");
        citation.CitedDocumentNumber = PatentsViewPatentParser.ReadOptionalText(reader, "citation_document_number");
        citation.CitationNumber = PatentsViewPatentParser.ReadOptionalText(reader, "citation_number");
        citation.CitedResourceReference = PatentsViewPatentParser.ReadOptionalText(reader, "citation_patent");
        citation.CitationCategory = PatentsViewPatentParser.ReadOptionalText(reader, "citation_category");
        citation.CitationName = PatentsViewPatentParser.ReadOptionalText(reader, "citation_name");
        citation.Country = PatentsViewPatentParser.ReadOptionalText(reader, "citation_country");
        citation.KindCode = PatentsViewPatentParser.ReadOptionalText(reader, "citation_wipo_kind");
        citation.ReferenceText = PatentsViewPatentParser.ReadOptionalText(reader, "reference_text");
        ApplyCitationDate(reader, citation);
        ApplyCitationSequence(reader, citation, citationType);
        citation.Provenance = OdpJsonParser.CopyProvenance(provenance);
        PatentsViewPatentParser.CopyUnknownFields(reader.CaptureUnknownFields(CitationKnownFields()), citation.AdditionalFields);
        return citation;
    }

    internal static PatentsViewRelatedApplication ParseRelatedApplicationRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance, bool isPublication)
    {
        PatentsViewPatentParser.ValidateRecordObject(element, path, "related-application text");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewRelatedApplication related = new PatentsViewRelatedApplication();
        if (isPublication)
        {
            related.DocumentNumber = PatentsViewPatentParser.ReadOptionalText(reader, "document_number");
        }
        else
        {
            related.PatentIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "patent_id");
        }

        related.RelatedText = PatentsViewPatentParser.ReadOptionalText(reader, "related_text");
        related.Provenance = OdpJsonParser.CopyProvenance(provenance);
        PatentsViewPatentParser.CopyUnknownFields(reader.CaptureUnknownFields(new string[] { "patent_id", "document_number", "related_text" }), related.AdditionalFields);
        return related;
    }

    internal static void ApplyCitationDate(JsonFieldReader reader, PatentsViewCitation citation)
    {
        JsonFieldValue date = reader.ReadDate("citation_date");
        PatentsViewPatentParser.ValidateOptionalValue(date, "The PatentsView citation date is invalid.");
        if (date.IsPresent && !date.IsNull)
        {
            citation.CitationDate = date.DateValue;
            citation.HasCitationDate = true;
        }
    }

    internal static void ApplyCitationSequence(JsonFieldReader reader, PatentsViewCitation citation, string citationType)
    {
        string fieldName = "citation_sequence";
        if (global::System.String.Equals(citationType, "other", global::System.StringComparison.Ordinal))
        {
            fieldName = "reference_sequence";
        }

        JsonFieldValue sequence = reader.ReadInt32(fieldName);
        PatentsViewPatentParser.ValidateOptionalValue(sequence, "The PatentsView citation sequence is invalid.");
        if (sequence.IsPresent && !sequence.IsNull)
        {
            if (sequence.Int32Value < 0)
            {
                throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView citation sequence cannot be negative.", sequence.Path);
            }

            citation.Sequence = sequence.Int32Value;
            citation.HasSequence = true;
        }
    }

    internal static string[] CitationKnownFields()
    {
        return new string[] { "patent_id", "document_number", "patent", "citation_patent_id", "citation_document_number", "citation_number", "citation_patent", "citation_category", "citation_name", "citation_country", "citation_wipo_kind", "reference_text", "citation_date", "citation_sequence", "reference_sequence" };
    }
}
