namespace uspto;

internal static class PatentsViewClassificationParser
{
    internal static PatentsViewCpcClassificationResponse ParseCpcResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache, string collectionName, string level)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = PatentsViewPatentParser.ValidateResponseRoot(document.RootElement, collectionName);
            PatentDataProvenance provenance = PatentsViewPatentParser.CreatePatentsViewProvenance(responseMetadata, collectionName, fromCache);
            JsonFieldValue records = root.ReadArray(collectionName);
            PatentsViewCpcClassification[] classifications = new PatentsViewCpcClassification[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                classifications[index] = ParseCpcRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance, level);
            }

            PatentsViewCpcClassificationResponse result = new PatentsViewCpcClassificationResponse();
            result.Classifications = classifications;
            result.Metadata = PatentsViewPatentParser.ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, collectionName, provenance);
            return result;
        }
    }

    internal static PatentsViewIpcClassificationResponse ParseIpcResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = PatentsViewPatentParser.ValidateResponseRoot(document.RootElement, "ipcr");
            PatentDataProvenance provenance = PatentsViewPatentParser.CreatePatentsViewProvenance(responseMetadata, "ipcr", fromCache);
            JsonFieldValue records = root.ReadArray("ipcr");
            PatentsViewIpcClassification[] classifications = new PatentsViewIpcClassification[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                classifications[index] = ParseIpcRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PatentsViewIpcClassificationResponse result = new PatentsViewIpcClassificationResponse();
            result.Classifications = classifications;
            result.Metadata = PatentsViewPatentParser.ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, "ipcr", provenance);
            return result;
        }
    }

    internal static PatentsViewUspcClassificationResponse ParseUspcResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache, string collectionName, string level)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = PatentsViewPatentParser.ValidateResponseRoot(document.RootElement, collectionName);
            PatentDataProvenance provenance = PatentsViewPatentParser.CreatePatentsViewProvenance(responseMetadata, collectionName, fromCache);
            JsonFieldValue records = root.ReadArray(collectionName);
            PatentsViewUspcClassification[] classifications = new PatentsViewUspcClassification[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                classifications[index] = ParseUspcRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance, level);
            }

            PatentsViewUspcClassificationResponse result = new PatentsViewUspcClassificationResponse();
            result.Classifications = classifications;
            result.Metadata = PatentsViewPatentParser.ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, collectionName, provenance);
            return result;
        }
    }

    internal static PatentsViewWipoClassificationResponse ParseWipoResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = PatentsViewPatentParser.ValidateResponseRoot(document.RootElement, "wipo");
            PatentDataProvenance provenance = PatentsViewPatentParser.CreatePatentsViewProvenance(responseMetadata, "wipo", fromCache);
            JsonFieldValue records = root.ReadArray("wipo");
            PatentsViewWipoClassification[] classifications = new PatentsViewWipoClassification[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                classifications[index] = ParseWipoRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PatentsViewWipoClassificationResponse result = new PatentsViewWipoClassificationResponse();
            result.Classifications = classifications;
            result.Metadata = PatentsViewPatentParser.ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, "wipo", provenance);
            return result;
        }
    }

    internal static PatentsViewCpcClassification ParseCpcRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance, string level)
    {
        PatentsViewPatentParser.ValidateRecordObject(element, path, "CPC classification");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewCpcClassification classification = new PatentsViewCpcClassification();
        classification.Level = level;
        classification.Class = PatentsViewPatentParser.ReadOptionalText(reader, "cpc_class");
        classification.ClassIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "cpc_class_id");
        classification.Subclass = PatentsViewPatentParser.ReadOptionalText(reader, "cpc_subclass");
        classification.SubclassIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "cpc_subclass_id");
        classification.Group = PatentsViewPatentParser.ReadOptionalText(reader, "cpc_group");
        classification.GroupIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "cpc_group_id");
        if (global::System.String.Equals(level, "class", global::System.StringComparison.Ordinal))
        {
            classification.ClassificationIdentifier = classification.ClassIdentifier;
            classification.Class = classification.ClassIdentifier;
            classification.Title = PatentsViewPatentParser.ReadOptionalText(reader, "cpc_class_title");
            ApplyCpcDates(reader, classification, "cpc_class_first_seen_date", "cpc_class_last_seen_date");
            ApplyCpcMetrics(reader, classification, "cpc_class");
        }
        else if (global::System.String.Equals(level, "subclass", global::System.StringComparison.Ordinal))
        {
            classification.ClassificationIdentifier = classification.SubclassIdentifier;
            classification.Subclass = classification.SubclassIdentifier;
            classification.Title = PatentsViewPatentParser.ReadOptionalText(reader, "cpc_subclass_title");
            ApplyCpcDates(reader, classification, "cpc_subclass_first_seen_date", "cpc_subclass_last_seen_date");
            ApplyCpcMetrics(reader, classification, "cpc_subclass");
        }
        else if (global::System.String.Equals(level, "group", global::System.StringComparison.Ordinal))
        {
            classification.ClassificationIdentifier = classification.GroupIdentifier;
            classification.Group = classification.GroupIdentifier;
            classification.Title = PatentsViewPatentParser.ReadOptionalText(reader, "cpc_group_title");
        }
        else
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView CPC level is invalid.", path);
        }

        classification.Provenance = OdpJsonParser.CopyProvenance(provenance);
        PatentsViewPatentParser.CopyUnknownFields(reader.CaptureUnknownFields(CpcKnownFields(level)), classification.AdditionalFields);
        return classification;
    }

    internal static PatentsViewIpcClassification ParseIpcRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        PatentsViewPatentParser.ValidateRecordObject(element, path, "IPC classification");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewIpcClassification classification = new PatentsViewIpcClassification();
        classification.ClassificationIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "ipc_id");
        classification.Section = PatentsViewPatentParser.ReadOptionalText(reader, "ipc_section");
        classification.Class = PatentsViewPatentParser.ReadOptionalText(reader, "ipc_class");
        classification.Subclass = PatentsViewPatentParser.ReadOptionalText(reader, "ipc_subclass");
        classification.MainGroup = PatentsViewPatentParser.ReadOptionalText(reader, "ipc_main_group");
        classification.Subgroup = PatentsViewPatentParser.ReadOptionalText(reader, "ipc_subgroup");
        classification.DataSource = PatentsViewPatentParser.ReadOptionalText(reader, "ipc_classification_data_source");
        classification.ClassificationValue = PatentsViewPatentParser.ReadOptionalText(reader, "ipc_classification_value");
        classification.SymbolPosition = PatentsViewPatentParser.ReadOptionalText(reader, "ipc_symbol_position");
        classification.ClassificationLevel = PatentsViewPatentParser.ReadOptionalText(reader, "ipc_class_level");
        classification.ClassificationStatus = PatentsViewPatentParser.ReadOptionalText(reader, "ipc_class_status");
        ApplyIpcOptionalValues(reader, classification);
        classification.Provenance = OdpJsonParser.CopyProvenance(provenance);
        PatentsViewPatentParser.CopyUnknownFields(reader.CaptureUnknownFields(IpcKnownFields()), classification.AdditionalFields);
        return classification;
    }

    internal static PatentsViewUspcClassification ParseUspcRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance, string level)
    {
        PatentsViewPatentParser.ValidateRecordObject(element, path, "USPC classification");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewUspcClassification classification = new PatentsViewUspcClassification();
        classification.Level = level;
        classification.MainClass = PatentsViewPatentParser.ReadOptionalText(reader, "uspc_mainclass");
        classification.MainClassIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "uspc_mainclass_id");
        classification.Subclass = PatentsViewPatentParser.ReadOptionalText(reader, "uspc_subclass");
        classification.SubclassIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "uspc_subclass_id");
        if (global::System.String.Equals(level, "mainclass", global::System.StringComparison.Ordinal))
        {
            classification.ClassificationIdentifier = classification.MainClassIdentifier;
            classification.MainClass = classification.MainClassIdentifier;
            classification.Title = PatentsViewPatentParser.ReadOptionalText(reader, "uspc_mainclass_title");
            ApplyUspcDates(reader, classification);
            ApplyUspcMetrics(reader, classification);
        }
        else if (global::System.String.Equals(level, "subclass", global::System.StringComparison.Ordinal))
        {
            classification.ClassificationIdentifier = classification.SubclassIdentifier;
            classification.Subclass = classification.SubclassIdentifier;
            classification.Title = PatentsViewPatentParser.ReadOptionalText(reader, "uspc_subclass_title");
        }
        else
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView USPC level is invalid.", path);
        }

        classification.Provenance = OdpJsonParser.CopyProvenance(provenance);
        PatentsViewPatentParser.CopyUnknownFields(reader.CaptureUnknownFields(UspcKnownFields(level)), classification.AdditionalFields);
        return classification;
    }

    internal static PatentsViewWipoClassification ParseWipoRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        PatentsViewPatentParser.ValidateRecordObject(element, path, "WIPO classification");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewWipoClassification classification = new PatentsViewWipoClassification();
        classification.ClassificationIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "wipo_id");
        classification.SectorTitle = PatentsViewPatentParser.ReadOptionalText(reader, "sector_title");
        classification.FieldTitle = PatentsViewPatentParser.ReadOptionalText(reader, "field_title");
        JsonFieldValue sequence = reader.ReadInt32("wipo_sequence");
        PatentsViewPatentParser.ValidateOptionalValue(sequence, "The PatentsView WIPO sequence is invalid.");
        if (sequence.IsPresent && !sequence.IsNull)
        {
            classification.Sequence = sequence.Int32Value;
            classification.HasSequence = true;
        }

        classification.Provenance = OdpJsonParser.CopyProvenance(provenance);
        PatentsViewPatentParser.CopyUnknownFields(reader.CaptureUnknownFields(new string[] { "wipo_id", "sector_title", "field_title", "wipo_sequence" }), classification.AdditionalFields);
        return classification;
    }

    internal static void ApplyCpcDates(JsonFieldReader reader, PatentsViewCpcClassification classification, string firstName, string lastName)
    {
        JsonFieldValue first = reader.ReadDate(firstName);
        JsonFieldValue last = reader.ReadDate(lastName);
        PatentsViewPatentParser.ValidateOptionalValue(first, "The PatentsView CPC first-seen date is invalid.");
        PatentsViewPatentParser.ValidateOptionalValue(last, "The PatentsView CPC last-seen date is invalid.");
        if (first.IsPresent && !first.IsNull)
        {
            classification.FirstSeenDate = first.DateValue;
            classification.HasFirstSeenDate = true;
        }

        if (last.IsPresent && !last.IsNull)
        {
            classification.LastSeenDate = last.DateValue;
            classification.HasLastSeenDate = true;
        }
    }

    internal static void ApplyCpcMetrics(JsonFieldReader reader, PatentsViewCpcClassification classification, string prefix)
    {
        JsonFieldValue years = reader.ReadInt32(prefix + "_years_active");
        JsonFieldValue patents = reader.ReadInt32(prefix + "_num_patents");
        JsonFieldValue assignees = reader.ReadInt32(prefix + "_num_assignees");
        JsonFieldValue inventors = reader.ReadInt32(prefix + "_num_inventors");
        ValidateMetric(years, "CPC years active");
        ValidateMetric(patents, "CPC patent count");
        ValidateMetric(assignees, "CPC assignee count");
        ValidateMetric(inventors, "CPC inventor count");
        if (years.IsPresent && !years.IsNull)
        {
            classification.YearsActive = years.Int32Value;
            classification.HasYearsActive = true;
        }

        if (patents.IsPresent && !patents.IsNull)
        {
            classification.PatentCount = patents.Int32Value;
            classification.HasPatentCount = true;
        }

        if (assignees.IsPresent && !assignees.IsNull)
        {
            classification.AssigneeCount = assignees.Int32Value;
            classification.HasAssigneeCount = true;
        }

        if (inventors.IsPresent && !inventors.IsNull)
        {
            classification.InventorCount = inventors.Int32Value;
            classification.HasInventorCount = true;
        }
    }

    internal static void ApplyIpcOptionalValues(JsonFieldReader reader, PatentsViewIpcClassification classification)
    {
        JsonFieldValue date = reader.ReadDate("ipc_action_date");
        JsonFieldValue sequence = reader.ReadInt32("ipc_sequence");
        PatentsViewPatentParser.ValidateOptionalValue(date, "The PatentsView IPC action date is invalid.");
        PatentsViewPatentParser.ValidateOptionalValue(sequence, "The PatentsView IPC sequence is invalid.");
        if (date.IsPresent && !date.IsNull)
        {
            classification.ActionDate = date.DateValue;
            classification.HasActionDate = true;
        }

        if (sequence.IsPresent && !sequence.IsNull)
        {
            classification.Sequence = sequence.Int32Value;
            classification.HasSequence = true;
        }
    }

    internal static void ApplyUspcDates(JsonFieldReader reader, PatentsViewUspcClassification classification)
    {
        JsonFieldValue first = reader.ReadDate("uspc_mainclass_first_seen_date");
        JsonFieldValue last = reader.ReadDate("uspc_mainclass_last_seen_date");
        PatentsViewPatentParser.ValidateOptionalValue(first, "The PatentsView USPC first-seen date is invalid.");
        PatentsViewPatentParser.ValidateOptionalValue(last, "The PatentsView USPC last-seen date is invalid.");
        if (first.IsPresent && !first.IsNull)
        {
            classification.FirstSeenDate = first.DateValue;
            classification.HasFirstSeenDate = true;
        }

        if (last.IsPresent && !last.IsNull)
        {
            classification.LastSeenDate = last.DateValue;
            classification.HasLastSeenDate = true;
        }
    }

    internal static void ApplyUspcMetrics(JsonFieldReader reader, PatentsViewUspcClassification classification)
    {
        JsonFieldValue years = reader.ReadInt32("uspc_mainclass_years_active");
        JsonFieldValue patents = reader.ReadInt32("uspc_mainclass_num_patents");
        JsonFieldValue assignees = reader.ReadInt32("uspc_mainclass_num_assignees");
        JsonFieldValue inventors = reader.ReadInt32("uspc_mainclass_num_inventors");
        ValidateMetric(years, "USPC years active");
        ValidateMetric(patents, "USPC patent count");
        ValidateMetric(assignees, "USPC assignee count");
        ValidateMetric(inventors, "USPC inventor count");
        if (years.IsPresent && !years.IsNull)
        {
            classification.YearsActive = years.Int32Value;
            classification.HasYearsActive = true;
        }

        if (patents.IsPresent && !patents.IsNull)
        {
            classification.PatentCount = patents.Int32Value;
            classification.HasPatentCount = true;
        }

        if (assignees.IsPresent && !assignees.IsNull)
        {
            classification.AssigneeCount = assignees.Int32Value;
            classification.HasAssigneeCount = true;
        }

        if (inventors.IsPresent && !inventors.IsNull)
        {
            classification.InventorCount = inventors.Int32Value;
            classification.HasInventorCount = true;
        }
    }

    internal static void ValidateMetric(JsonFieldValue value, string fieldDescription)
    {
        PatentsViewPatentParser.ValidateOptionalValue(value, "The PatentsView " + fieldDescription + " is invalid.");
        if (value.IsPresent && !value.IsNull && value.Int32Value < 0)
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView " + fieldDescription + " cannot be negative.", value.Path);
        }
    }

    internal static string[] CpcKnownFields(string level)
    {
        if (global::System.String.Equals(level, "class", global::System.StringComparison.Ordinal))
        {
            return new string[] { "cpc_class_id", "cpc_class", "cpc_class_title", "cpc_class_first_seen_date", "cpc_class_last_seen_date", "cpc_class_years_active", "cpc_class_num_patents", "cpc_class_num_assignees", "cpc_class_num_inventors" };
        }

        if (global::System.String.Equals(level, "subclass", global::System.StringComparison.Ordinal))
        {
            return new string[] { "cpc_subclass_id", "cpc_subclass", "cpc_subclass_title", "cpc_class_id", "cpc_class", "cpc_subclass_first_seen_date", "cpc_subclass_last_seen_date", "cpc_subclass_years_active", "cpc_subclass_num_patents", "cpc_subclass_num_assignees", "cpc_subclass_num_inventors" };
        }

        return new string[] { "cpc_group_id", "cpc_group", "cpc_group_title", "cpc_class_id", "cpc_class", "cpc_subclass_id", "cpc_subclass" };
    }

    internal static string[] IpcKnownFields()
    {
        return new string[] { "ipc_id", "ipc_section", "ipc_class", "ipc_subclass", "ipc_main_group", "ipc_subgroup", "ipc_classification_data_source", "ipc_classification_value", "ipc_symbol_position", "ipc_class_level", "ipc_class_status", "ipc_action_date", "ipc_sequence" };
    }

    internal static string[] UspcKnownFields(string level)
    {
        if (global::System.String.Equals(level, "mainclass", global::System.StringComparison.Ordinal))
        {
            return new string[] { "uspc_mainclass_id", "uspc_mainclass", "uspc_mainclass_title", "uspc_mainclass_first_seen_date", "uspc_mainclass_last_seen_date", "uspc_mainclass_years_active", "uspc_mainclass_num_patents", "uspc_mainclass_num_assignees", "uspc_mainclass_num_inventors" };
        }

        return new string[] { "uspc_subclass_id", "uspc_subclass", "uspc_subclass_title", "uspc_mainclass_id", "uspc_mainclass" };
    }
}
