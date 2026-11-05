namespace uspto;

internal class PatentsViewFloatingParseResult
{
    internal double Value = 0.0;
    internal bool IsValid = false;
}

internal static class PatentsViewEntityParser
{
    internal static PatentsViewInventorResponse ParseInventorResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = PatentsViewPatentParser.ValidateResponseRoot(document.RootElement, "inventors");
            PatentDataProvenance provenance = PatentsViewPatentParser.CreatePatentsViewProvenance(responseMetadata, "g_inventor_disambiguated", fromCache);
            JsonFieldValue records = root.ReadArray("inventors");
            PatentsViewInventor[] inventors = new PatentsViewInventor[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                inventors[index] = ParseInventorRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PatentsViewInventorResponse result = new PatentsViewInventorResponse();
            result.Inventors = inventors;
            result.Metadata = PatentsViewPatentParser.ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, "inventors", provenance);
            return result;
        }
    }

    internal static PatentsViewAssigneeResponse ParseAssigneeResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = PatentsViewPatentParser.ValidateResponseRoot(document.RootElement, "assignees");
            PatentDataProvenance provenance = PatentsViewPatentParser.CreatePatentsViewProvenance(responseMetadata, "g_assignee_disambiguated", fromCache);
            JsonFieldValue records = root.ReadArray("assignees");
            PatentsViewAssignee[] assignees = new PatentsViewAssignee[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                assignees[index] = ParseAssigneeRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PatentsViewAssigneeResponse result = new PatentsViewAssigneeResponse();
            result.Assignees = assignees;
            result.Metadata = PatentsViewPatentParser.ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, "assignees", provenance);
            return result;
        }
    }

    internal static PatentsViewLocationResponse ParseLocationResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = PatentsViewPatentParser.ValidateResponseRoot(document.RootElement, "locations");
            PatentDataProvenance provenance = PatentsViewPatentParser.CreatePatentsViewProvenance(responseMetadata, "g_location_disambiguated", fromCache);
            JsonFieldValue records = root.ReadArray("locations");
            PatentsViewLocation[] locations = new PatentsViewLocation[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                locations[index] = ParseLocationRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PatentsViewLocationResponse result = new PatentsViewLocationResponse();
            result.Locations = locations;
            result.Metadata = PatentsViewPatentParser.ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, "locations", provenance);
            return result;
        }
    }

    internal static PatentsViewAttorneyResponse ParseAttorneyResponse(string json, PatentsViewQueryRequest request, HttpResponseMetadata responseMetadata, string? rawResponse, bool fromCache)
    {
        using (global::System.Text.Json.JsonDocument document = global::System.Text.Json.JsonDocument.Parse(json))
        {
            JsonFieldReader root = PatentsViewPatentParser.ValidateResponseRoot(document.RootElement, "attorneys");
            PatentDataProvenance provenance = PatentsViewPatentParser.CreatePatentsViewProvenance(responseMetadata, "g_attorney", fromCache);
            JsonFieldValue records = root.ReadArray("attorneys");
            PatentsViewAttorney[] attorneys = new PatentsViewAttorney[records.ArrayItems.Length];
            int index;
            for (index = 0; index < records.ArrayItems.Length; index += 1)
            {
                attorneys[index] = ParseAttorneyRecord(records.ArrayItems[index], records.ArrayItemPaths[index], provenance);
            }

            PatentsViewAttorneyResponse result = new PatentsViewAttorneyResponse();
            result.Attorneys = attorneys;
            result.Metadata = PatentsViewPatentParser.ParseResponseMetadata(root, request, responseMetadata, rawResponse, fromCache, records, "attorneys", provenance);
            return result;
        }
    }

    internal static PatentsViewInventor ParseInventorRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        PatentsViewPatentParser.ValidateRecordObject(element, path, "inventor");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewInventor inventor = new PatentsViewInventor();
        inventor.InventorIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "inventor_id");
        inventor.ResourceReference = PatentsViewPatentParser.ReadOptionalText(reader, "inventor");
        inventor.FirstName = PatentsViewPatentParser.ReadOptionalText(reader, "inventor_name_first");
        inventor.LastName = PatentsViewPatentParser.ReadOptionalText(reader, "inventor_name_last");
        inventor.GenderCode = PatentsViewPatentParser.ReadOptionalText(reader, "inventor_gender_code");
        inventor.LastKnownLocation = ParseLastKnownLocation(reader, "inventor", provenance);
        ApplyInventorDates(reader, inventor);
        ApplyInventorCounts(reader, inventor);
        inventor.ActivityYears = ParseActivityYears(reader, "inventor_years");
        inventor.Provenance = OdpJsonParser.CopyProvenance(provenance);
        PatentsViewPatentParser.CopyUnknownFields(reader.CaptureUnknownFields(InventorKnownFields()), inventor.AdditionalFields);
        return inventor;
    }

    internal static PatentsViewAssignee ParseAssigneeRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        PatentsViewPatentParser.ValidateRecordObject(element, path, "assignee");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewAssignee assignee = new PatentsViewAssignee();
        assignee.AssigneeIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "assignee_id");
        assignee.ResourceReference = PatentsViewPatentParser.ReadOptionalText(reader, "assignee");
        assignee.FirstName = PatentsViewPatentParser.ReadOptionalText(reader, "assignee_individual_name_first");
        assignee.LastName = PatentsViewPatentParser.ReadOptionalText(reader, "assignee_individual_name_last");
        assignee.Organization = PatentsViewPatentParser.ReadOptionalText(reader, "assignee_organization");
        assignee.AssigneeType = PatentsViewPatentParser.ReadOptionalText(reader, "assignee_type");
        assignee.LastKnownLocation = ParseLastKnownLocation(reader, "assignee", provenance);
        ApplyAssigneeDates(reader, assignee);
        ApplyAssigneeCounts(reader, assignee);
        assignee.ActivityYears = ParseActivityYears(reader, "assignee_years");
        assignee.Provenance = OdpJsonParser.CopyProvenance(provenance);
        PatentsViewPatentParser.CopyUnknownFields(reader.CaptureUnknownFields(AssigneeKnownFields()), assignee.AdditionalFields);
        return assignee;
    }

    internal static PatentsViewLocation ParseLocationRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        PatentsViewPatentParser.ValidateRecordObject(element, path, "location");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewLocation location = new PatentsViewLocation();
        location.LocationIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "location_id");
        location.Name = PatentsViewPatentParser.ReadOptionalText(reader, "location_name");
        location.County = PatentsViewPatentParser.ReadOptionalText(reader, "location_county");
        location.CountyFipsCode = PatentsViewPatentParser.ReadOptionalText(reader, "location_county_fips");
        location.State = PatentsViewPatentParser.ReadOptionalText(reader, "location_state");
        location.StateFipsCode = PatentsViewPatentParser.ReadOptionalText(reader, "location_state_fips");
        location.Country = PatentsViewPatentParser.ReadOptionalText(reader, "location_country");
        location.PlaceType = PatentsViewPatentParser.ReadOptionalText(reader, "location_place_type");
        ApplyLocationCoordinates(reader, location, "location_latitude", "location_longitude");
        ApplyLocationCounts(reader, location);
        location.Provenance = OdpJsonParser.CopyProvenance(provenance);
        PatentsViewPatentParser.CopyUnknownFields(reader.CaptureUnknownFields(LocationKnownFields()), location.AdditionalFields);
        return location;
    }

    internal static PatentsViewAttorney ParseAttorneyRecord(global::System.Text.Json.JsonElement element, string path, PatentDataProvenance provenance)
    {
        PatentsViewPatentParser.ValidateRecordObject(element, path, "attorney");
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentsViewAttorney attorney = new PatentsViewAttorney();
        attorney.AttorneyIdentifier = PatentsViewPatentParser.ReadOptionalText(reader, "attorney_id");
        attorney.FirstName = PatentsViewPatentParser.ReadOptionalText(reader, "attorney_name_first");
        attorney.LastName = PatentsViewPatentParser.ReadOptionalText(reader, "attorney_name_last");
        attorney.Organization = PatentsViewPatentParser.ReadOptionalText(reader, "attorney_organization");
        ApplyAttorneyDates(reader, attorney);
        ApplyAttorneyCounts(reader, attorney);
        attorney.Provenance = OdpJsonParser.CopyProvenance(provenance);
        PatentsViewPatentParser.CopyUnknownFields(reader.CaptureUnknownFields(AttorneyKnownFields()), attorney.AdditionalFields);
        return attorney;
    }

    internal static PatentsViewLocation? ParseLastKnownLocation(JsonFieldReader reader, string prefix, PatentDataProvenance provenance)
    {
        string? identifier = PatentsViewPatentParser.ReadOptionalText(reader, prefix + "_lastknown_location");
        string? name = PatentsViewPatentParser.ReadOptionalText(reader, prefix + "_lastknown_city");
        string? state = PatentsViewPatentParser.ReadOptionalText(reader, prefix + "_lastknown_state");
        string? country = PatentsViewPatentParser.ReadOptionalText(reader, prefix + "_lastknown_country");
        JsonFieldValue latitude = reader.ReadString(prefix + "_lastknown_latitude");
        JsonFieldValue longitude = reader.ReadString(prefix + "_lastknown_longitude");
        PatentsViewPatentParser.ValidateOptionalValue(latitude, "The PatentsView last-known latitude is invalid.");
        PatentsViewPatentParser.ValidateOptionalValue(longitude, "The PatentsView last-known longitude is invalid.");
        if (identifier == null && name == null && state == null && country == null && !latitude.IsPresent && !longitude.IsPresent)
        {
            return null;
        }

        PatentsViewLocation location = new PatentsViewLocation();
        location.LocationIdentifier = identifier;
        location.Name = name;
        location.State = state;
        location.Country = country;
        ApplyCoordinateValue(latitude, location, true);
        ApplyCoordinateValue(longitude, location, false);
        location.Provenance = OdpJsonParser.CopyProvenance(provenance);
        return location;
    }

    internal static void ApplyInventorDates(JsonFieldReader reader, PatentsViewInventor inventor)
    {
        JsonFieldValue first = reader.ReadDate("inventor_first_seen_date");
        JsonFieldValue last = reader.ReadDate("inventor_last_seen_date");
        PatentsViewPatentParser.ValidateOptionalValue(first, "The PatentsView inventor first-seen date is invalid.");
        PatentsViewPatentParser.ValidateOptionalValue(last, "The PatentsView inventor last-seen date is invalid.");
        if (first.IsPresent && !first.IsNull)
        {
            inventor.FirstSeenDate = first.DateValue;
            inventor.HasFirstSeenDate = true;
        }

        if (last.IsPresent && !last.IsNull)
        {
            inventor.LastSeenDate = last.DateValue;
            inventor.HasLastSeenDate = true;
        }
    }

    internal static void ApplyInventorCounts(JsonFieldReader reader, PatentsViewInventor inventor)
    {
        JsonFieldValue assignees = reader.ReadInt32("inventor_num_assignees");
        JsonFieldValue patents = reader.ReadInt32("inventor_num_patents");
        JsonFieldValue years = reader.ReadInt32("inventor_years_active");
        ValidateCountValues(assignees, patents, years, "inventor");
        if (assignees.IsPresent && !assignees.IsNull)
        {
            inventor.AssigneeCount = assignees.Int32Value;
            inventor.HasAssigneeCount = true;
        }

        if (patents.IsPresent && !patents.IsNull)
        {
            inventor.PatentCount = patents.Int32Value;
            inventor.HasPatentCount = true;
        }

        if (years.IsPresent && !years.IsNull)
        {
            inventor.YearsActive = years.Int32Value;
            inventor.HasYearsActive = true;
        }
    }

    internal static void ApplyAssigneeDates(JsonFieldReader reader, PatentsViewAssignee assignee)
    {
        JsonFieldValue first = reader.ReadDate("assignee_first_seen_date");
        JsonFieldValue last = reader.ReadDate("assignee_last_seen_date");
        PatentsViewPatentParser.ValidateOptionalValue(first, "The PatentsView assignee first-seen date is invalid.");
        PatentsViewPatentParser.ValidateOptionalValue(last, "The PatentsView assignee last-seen date is invalid.");
        if (first.IsPresent && !first.IsNull)
        {
            assignee.FirstSeenDate = first.DateValue;
            assignee.HasFirstSeenDate = true;
        }

        if (last.IsPresent && !last.IsNull)
        {
            assignee.LastSeenDate = last.DateValue;
            assignee.HasLastSeenDate = true;
        }
    }

    internal static void ApplyAssigneeCounts(JsonFieldReader reader, PatentsViewAssignee assignee)
    {
        JsonFieldValue inventors = reader.ReadInt32("assignee_num_inventors");
        JsonFieldValue patents = reader.ReadInt32("assignee_num_patents");
        JsonFieldValue years = reader.ReadInt32("assignee_years_active");
        ValidateCountValues(inventors, patents, years, "assignee");
        if (inventors.IsPresent && !inventors.IsNull)
        {
            assignee.InventorCount = inventors.Int32Value;
            assignee.HasInventorCount = true;
        }

        if (patents.IsPresent && !patents.IsNull)
        {
            assignee.PatentCount = patents.Int32Value;
            assignee.HasPatentCount = true;
        }

        if (years.IsPresent && !years.IsNull)
        {
            assignee.YearsActive = years.Int32Value;
            assignee.HasYearsActive = true;
        }
    }

    internal static void ApplyAttorneyDates(JsonFieldReader reader, PatentsViewAttorney attorney)
    {
        JsonFieldValue first = reader.ReadDate("attorney_first_seen_date");
        JsonFieldValue last = reader.ReadDate("attorney_last_seen_date");
        PatentsViewPatentParser.ValidateOptionalValue(first, "The PatentsView attorney first-seen date is invalid.");
        PatentsViewPatentParser.ValidateOptionalValue(last, "The PatentsView attorney last-seen date is invalid.");
        if (first.IsPresent && !first.IsNull)
        {
            attorney.FirstSeenDate = first.DateValue;
            attorney.HasFirstSeenDate = true;
        }

        if (last.IsPresent && !last.IsNull)
        {
            attorney.LastSeenDate = last.DateValue;
            attorney.HasLastSeenDate = true;
        }
    }

    internal static void ApplyAttorneyCounts(JsonFieldReader reader, PatentsViewAttorney attorney)
    {
        JsonFieldValue inventors = reader.ReadInt32("attorney_num_inventors");
        JsonFieldValue patents = reader.ReadInt32("attorney_num_patents");
        JsonFieldValue years = reader.ReadInt32("attorney_years_active");
        ValidateCountValues(inventors, patents, years, "attorney");
        if (inventors.IsPresent && !inventors.IsNull)
        {
            attorney.InventorCount = inventors.Int32Value;
            attorney.HasInventorCount = true;
        }

        if (patents.IsPresent && !patents.IsNull)
        {
            attorney.PatentCount = patents.Int32Value;
            attorney.HasPatentCount = true;
        }

        if (years.IsPresent && !years.IsNull)
        {
            attorney.YearsActive = years.Int32Value;
            attorney.HasYearsActive = true;
        }
    }

    internal static void ApplyLocationCounts(JsonFieldReader reader, PatentsViewLocation location)
    {
        JsonFieldValue assignees = reader.ReadInt32("location_num_assignees");
        JsonFieldValue patents = reader.ReadInt32("location_num_patents");
        JsonFieldValue inventors = reader.ReadInt32("location_num_inventors");
        ValidateCountValues(assignees, patents, inventors, "location");
        if (assignees.IsPresent && !assignees.IsNull)
        {
            location.AssigneeCount = assignees.Int32Value;
            location.HasAssigneeCount = true;
        }

        if (patents.IsPresent && !patents.IsNull)
        {
            location.PatentCount = patents.Int32Value;
            location.HasPatentCount = true;
        }

        if (inventors.IsPresent && !inventors.IsNull)
        {
            location.InventorCount = inventors.Int32Value;
            location.HasInventorCount = true;
        }
    }

    internal static void ValidateCountValues(JsonFieldValue first, JsonFieldValue second, JsonFieldValue third, string entityName)
    {
        PatentsViewPatentParser.ValidateOptionalValue(first, "A PatentsView " + entityName + " count is invalid.");
        PatentsViewPatentParser.ValidateOptionalValue(second, "A PatentsView " + entityName + " count is invalid.");
        PatentsViewPatentParser.ValidateOptionalValue(third, "A PatentsView " + entityName + " count is invalid.");
        if ((first.IsPresent && !first.IsNull && first.Int32Value < 0) || (second.IsPresent && !second.IsNull && second.Int32Value < 0) || (third.IsPresent && !third.IsNull && third.Int32Value < 0))
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("A PatentsView " + entityName + " count cannot be negative.", first.Path);
        }
    }

    internal static PatentsViewEntityActivityYear[] ParseActivityYears(JsonFieldReader reader, string fieldName)
    {
        JsonFieldValue values = reader.ReadArray(fieldName);
        PatentsViewPatentParser.ValidateOptionalValue(values, "The PatentsView activity-year collection is invalid.");
        PatentsViewEntityActivityYear[] result = new PatentsViewEntityActivityYear[values.ArrayItems.Length];
        int index;
        for (index = 0; index < values.ArrayItems.Length; index += 1)
        {
            PatentsViewPatentParser.ValidateRecordObject(values.ArrayItems[index], values.ArrayItemPaths[index], "activity year");
            JsonFieldReader item = new JsonFieldReader(values.ArrayItems[index], values.ArrayItemPaths[index]);
            JsonFieldValue year = item.ReadInt32("year");
            JsonFieldValue count = item.ReadInt32("num_patents");
            PatentsViewPatentParser.ValidateOptionalValue(year, "The PatentsView activity year is invalid.");
            PatentsViewPatentParser.ValidateOptionalValue(count, "The PatentsView activity patent count is invalid.");
            PatentsViewEntityActivityYear activity = new PatentsViewEntityActivityYear();
            if (year.IsPresent && !year.IsNull)
            {
                activity.Year = year.Int32Value;
                activity.HasYear = true;
            }

            if (count.IsPresent && !count.IsNull)
            {
                if (count.Int32Value < 0)
                {
                    throw PatentsViewPatentParser.ParsePatentSearchFailure("The PatentsView activity patent count cannot be negative.", count.Path);
                }

                activity.PatentCount = count.Int32Value;
                activity.HasPatentCount = true;
            }

            result[index] = activity;
        }

        return result;
    }

    internal static void ApplyLocationCoordinates(JsonFieldReader reader, PatentsViewLocation location, string latitudeName, string longitudeName)
    {
        JsonFieldValue latitude = reader.ReadString(latitudeName);
        JsonFieldValue longitude = reader.ReadString(longitudeName);
        PatentsViewPatentParser.ValidateOptionalValue(latitude, "The PatentsView latitude is invalid.");
        PatentsViewPatentParser.ValidateOptionalValue(longitude, "The PatentsView longitude is invalid.");
        ApplyCoordinateValue(latitude, location, true);
        ApplyCoordinateValue(longitude, location, false);
    }

    internal static void ApplyCoordinateValue(JsonFieldValue field, PatentsViewLocation location, bool isLatitude)
    {
        if (!field.IsPresent || field.IsNull)
        {
            return;
        }

        PatentsViewFloatingParseResult parsed = ParseFloatingText(field.StringValue);
        if (!parsed.IsValid || (isLatitude && (parsed.Value < -90.0 || parsed.Value > 90.0)) || (!isLatitude && (parsed.Value < -180.0 || parsed.Value > 180.0)))
        {
            throw PatentsViewPatentParser.ParsePatentSearchFailure("A PatentsView geographic coordinate is invalid.", field.Path);
        }

        if (isLatitude)
        {
            location.Latitude = parsed.Value;
            location.HasLatitude = true;
        }
        else
        {
            location.Longitude = parsed.Value;
            location.HasLongitude = true;
        }
    }

    internal static PatentsViewFloatingParseResult ParseFloatingText(string? text)
    {
        PatentsViewFloatingParseResult result = new PatentsViewFloatingParseResult();
        if (text == null)
        {
            return result;
        }

        string value = text.Trim();
        if (value.Length == 0)
        {
            return result;
        }

        int sign = 1;
        int index = 0;
        if (value[0] == '-' || value[0] == '+')
        {
            sign = value[0] == '-' ? -1 : 1;
            index = 1;
        }

        double number = 0.0;
        bool hasDigit = false;
        while (index < value.Length && value[index] >= '0' && value[index] <= '9')
        {
            number = (number * 10.0) + (value[index] - '0');
            hasDigit = true;
            index += 1;
        }

        if (index < value.Length && value[index] == '.')
        {
            index += 1;
            double place = 0.1;
            while (index < value.Length && value[index] >= '0' && value[index] <= '9')
            {
                number += (value[index] - '0') * place;
                place /= 10.0;
                hasDigit = true;
                index += 1;
            }
        }

        int exponent = 0;
        int exponentSign = 1;
        if (index < value.Length && (value[index] == 'e' || value[index] == 'E'))
        {
            index += 1;
            if (index < value.Length && (value[index] == '-' || value[index] == '+'))
            {
                exponentSign = value[index] == '-' ? -1 : 1;
                index += 1;
            }

            bool hasExponentDigit = false;
            while (index < value.Length && value[index] >= '0' && value[index] <= '9' && exponent <= 308)
            {
                exponent = (exponent * 10) + (value[index] - '0');
                hasExponentDigit = true;
                index += 1;
            }

            if (!hasExponentDigit)
            {
                return result;
            }
        }

        if (!hasDigit || index != value.Length || exponent > 308)
        {
            return result;
        }

        number = sign * number * global::System.Math.Pow(10.0, exponentSign * exponent);
        if (global::System.Double.IsNaN(number) || global::System.Double.IsInfinity(number))
        {
            return result;
        }

        result.Value = number;
        result.IsValid = true;
        return result;
    }

    internal static string[] InventorKnownFields()
    {
        return new string[] { "inventor_id", "inventor", "inventor_name_first", "inventor_name_last", "inventor_gender_code", "inventor_lastknown_city", "inventor_lastknown_state", "inventor_lastknown_country", "inventor_lastknown_latitude", "inventor_lastknown_longitude", "inventor_lastknown_location", "inventor_first_seen_date", "inventor_last_seen_date", "inventor_num_assignees", "inventor_num_patents", "inventor_years_active", "inventor_years" };
    }

    internal static string[] AssigneeKnownFields()
    {
        return new string[] { "assignee_id", "assignee", "assignee_individual_name_first", "assignee_individual_name_last", "assignee_organization", "assignee_type", "assignee_lastknown_city", "assignee_lastknown_state", "assignee_lastknown_country", "assignee_lastknown_latitude", "assignee_lastknown_longitude", "assignee_lastknown_location", "assignee_first_seen_date", "assignee_last_seen_date", "assignee_num_inventors", "assignee_num_patents", "assignee_years_active", "assignee_years" };
    }

    internal static string[] LocationKnownFields()
    {
        return new string[] { "location_id", "location_name", "location_county", "location_county_fips", "location_state", "location_state_fips", "location_country", "location_place_type", "location_latitude", "location_longitude", "location_num_assignees", "location_num_patents", "location_num_inventors" };
    }

    internal static string[] AttorneyKnownFields()
    {
        return new string[] { "attorney_id", "attorney_name_first", "attorney_name_last", "attorney_organization", "attorney_first_seen_date", "attorney_last_seen_date", "attorney_num_inventors", "attorney_num_patents", "attorney_years_active" };
    }
}
