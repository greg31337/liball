namespace uspto;

internal static class OdpPatentAssignmentParser
{
    internal static PatentAssignmentHistory Parse(string json, string expectedApplicationNumber, HttpResponseMetadata responseMetadata)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP patent assignment response is required.", "json", "REQUIRED", null);
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
                throw AssignmentFailure("The ODP patent assignment response must contain exactly one application.", count.Path);
            }

            JsonFieldValue records = root.ReadArray("patentFileWrapperDataBag");
            if (!records.IsPresent || !records.IsValid || records.IsNull || records.ArrayItems.Length != 1)
            {
                throw AssignmentFailure("The ODP patent assignment response has an invalid application collection.", records.Path);
            }

            if (records.ArrayItems[0].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw AssignmentFailure("The ODP patent assignment application record is invalid.", records.ArrayItemPaths[0]);
            }

            JsonFieldReader record = new JsonFieldReader(records.ArrayItems[0], records.ArrayItemPaths[0]);
            string? requestIdentifier = ReadRequestIdentifier(root, responseMetadata.RequestIdentifier);
            requestIdentifier = ReadRequestIdentifier(record, requestIdentifier);
            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            PatentAssignmentHistory result = ParseRecord(record, expectedApplicationNumber, requestIdentifier, provenance);
            JsonUnknownField[] unknown = root.CaptureUnknownFields(new string[] { "count", "patentFileWrapperDataBag", "requestIdentifier" });
            MergeUnknownFields(result.UnknownFields, unknown, "response.");
            return result;
        }
    }

    internal static PatentAssignmentHistory ParseRecord(JsonFieldReader record, string expectedApplicationNumber, string? requestIdentifier, PatentDataProvenance provenance)
    {
        JsonFieldValue applicationNumber = record.ReadString("applicationNumberText");
        if (!applicationNumber.IsPresent || !applicationNumber.IsValid || applicationNumber.IsNull)
        {
            throw AssignmentFailure("The ODP patent assignment application number is missing or invalid.", applicationNumber.Path);
        }

        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(applicationNumber.StringValue ?? string.Empty);
        if (!parsed.IsValid || !global::System.String.Equals(parsed.CanonicalNumber, expectedApplicationNumber, global::System.StringComparison.Ordinal))
        {
            throw AssignmentFailure("The ODP patent assignment response does not match the requested application.", applicationNumber.Path);
        }

        PatentAssignmentHistory result = new PatentAssignmentHistory();
        result.ApplicationNumber = parsed.CanonicalNumber;
        result.Assignments = ParseAssignmentBag(record);
        result.Count = result.Assignments.Length;
        result.RequestIdentifier = requestIdentifier;
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = record.CaptureUnknownFields(new string[] { "applicationNumberText", "assignmentBag", "requestIdentifier" });
        MergeUnknownFields(result.UnknownFields, unknown, string.Empty);
        return result;
    }

    internal static PatentAssignment[] ParseAssignmentBag(JsonFieldReader record)
    {
        JsonFieldValue value = record.ReadRaw("assignmentBag");
        if (!value.IsPresent || value.IsNull)
        {
            return new PatentAssignment[0];
        }

        if (value.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Object)
        {
            return new PatentAssignment[] { ParseAssignment(value.ElementValue, value.Path) };
        }

        if (value.ElementValue.ValueKind != global::System.Text.Json.JsonValueKind.Array)
        {
            throw AssignmentFailure("The ODP patent assignment collection is invalid.", value.Path);
        }

        PatentAssignment[] assignments = new PatentAssignment[value.ElementValue.GetArrayLength()];
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in value.ElementValue.EnumerateArray())
        {
            string path = value.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            if (item.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw AssignmentFailure("The ODP patent assignment entry is invalid.", path);
            }

            assignments[index] = ParseAssignment(item, path);
            index += 1;
        }

        return assignments;
    }

    internal static PatentAssignment ParseAssignment(global::System.Text.Json.JsonElement element, string path)
    {
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentAssignment assignment = new PatentAssignment();
        assignment.ReelNumber = ReadOptionalString(reader, "reelNumber");
        assignment.FrameNumber = ReadOptionalString(reader, "frameNumber");
        assignment.ReelAndFrameNumber = ReadOptionalString(reader, "reelAndFrameNumber");
        if (assignment.ReelAndFrameNumber == null && assignment.ReelNumber != null && assignment.FrameNumber != null)
        {
            assignment.ReelAndFrameNumber = assignment.ReelNumber + "/" + assignment.FrameNumber;
        }

        assignment.Conveyance = ReadAliasedString(reader, new string[] { "conveyanceText", "conveyance" });
        assignment.AttorneyDocketNumber = ReadAliasedString(reader, new string[] { "attorneyDocketNumber", "docketNumber" });
        ApplyAssignmentValues(assignment, reader);
        ApplyAssignmentDates(assignment, reader);
        assignment.Assignors = ParseAssignors(reader);
        assignment.Assignees = ParseAssignees(reader);
        assignment.Correspondents = ParseCorrespondents(reader);
        assignment.DomesticRepresentative = ParseDomesticRepresentative(reader);
        assignment.Properties = ParseProperties(reader);
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownAssignmentFields());
        MergeUnknownFields(assignment.UnknownFields, unknown, string.Empty);
        return assignment;
    }

    internal static void ApplyAssignmentValues(PatentAssignment assignment, JsonFieldReader reader)
    {
        JsonFieldValue pages = reader.ReadInt32("pageTotalQuantity");
        ValidateOptional(pages, "The ODP patent assignment page count is invalid.");
        if (pages.IsPresent && !pages.IsNull)
        {
            if (pages.Int32Value < 0)
            {
                throw AssignmentFailure("The ODP patent assignment page count cannot be negative.", pages.Path);
            }

            assignment.PageCount = pages.Int32Value;
            assignment.HasPageCount = true;
        }

        JsonFieldValue image = ReadIndicator(reader, "imageAvailableStatusCode");
        ValidateOptional(image, "The ODP patent assignment image-availability value is invalid.");
        if (image.IsPresent && !image.IsNull)
        {
            assignment.IsImageAvailable = image.BooleanValue;
            assignment.HasIsImageAvailable = true;
        }

        string? location = ReadAliasedString(reader, new string[] { "assignmentDocumentLocationURI", "documentLocationURI", "assignmentDocumentUri" });
        if (location != null)
        {
            if (!global::System.Uri.IsWellFormedUriString(location, global::System.UriKind.Absolute))
            {
                throw AssignmentFailure("The ODP patent assignment document location is invalid.", FindPresentPath(reader, new string[] { "assignmentDocumentLocationURI", "documentLocationURI", "assignmentDocumentUri" }));
            }

            assignment.DocumentUri = new global::System.Uri(location);
        }
    }

    internal static void ApplyAssignmentDates(PatentAssignment assignment, JsonFieldReader reader)
    {
        JsonFieldValue received = ReadAliasedDate(reader, new string[] { "assignmentReceivedDate", "receivedDate" });
        ValidateOptional(received, "The ODP patent assignment received date is invalid.");
        if (received.IsPresent && !received.IsNull)
        {
            assignment.ReceivedDate = received.DateValue;
            assignment.HasReceivedDate = true;
        }

        JsonFieldValue recorded = ReadAliasedDate(reader, new string[] { "assignmentRecordedDate", "recordedDate", "recordationDate" });
        ValidateOptional(recorded, "The ODP patent assignment recorded date is invalid.");
        if (recorded.IsPresent && !recorded.IsNull)
        {
            assignment.RecordedDate = recorded.DateValue;
            assignment.HasRecordedDate = true;
        }

        JsonFieldValue mailed = ReadAliasedDate(reader, new string[] { "assignmentMailedDate", "mailedDate" });
        ValidateOptional(mailed, "The ODP patent assignment mailed date is invalid.");
        if (mailed.IsPresent && !mailed.IsNull)
        {
            assignment.MailedDate = mailed.DateValue;
            assignment.HasMailedDate = true;
        }
    }

    internal static PatentAssignmentAssignor[] ParseAssignors(JsonFieldReader assignment)
    {
        JsonFieldValue bag = assignment.ReadArray("assignorBag");
        ValidateOptional(bag, "The ODP patent assignment assignor collection is invalid.");
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentAssignmentAssignor[0];
        }

        PatentAssignmentAssignor[] assignors = new PatentAssignmentAssignor[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            if (bag.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw AssignmentFailure("The ODP patent assignment assignor is invalid.", bag.ArrayItemPaths[index]);
            }

            JsonFieldReader reader = new JsonFieldReader(bag.ArrayItems[index], bag.ArrayItemPaths[index]);
            PatentAssignmentAssignor assignor = new PatentAssignmentAssignor();
            assignor.Name = ReadAliasedString(reader, new string[] { "assignorName", "assignorNameText", "name" });
            JsonFieldValue executionDate = ReadAliasedDate(reader, new string[] { "executionDate", "assignmentExecutionDate" });
            ValidateOptional(executionDate, "The ODP patent assignment execution date is invalid.");
            if (executionDate.IsPresent && !executionDate.IsNull)
            {
                assignor.ExecutionDate = executionDate.DateValue;
                assignor.HasExecutionDate = true;
            }

            JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "assignorName", "assignorNameText", "name", "executionDate", "assignmentExecutionDate" });
            MergeUnknownFields(assignor.UnknownFields, unknown, string.Empty);
            assignors[index] = assignor;
        }

        return assignors;
    }

    internal static PatentAssignmentAssignee[] ParseAssignees(JsonFieldReader assignment)
    {
        JsonFieldValue bag = assignment.ReadArray("assigneeBag");
        ValidateOptional(bag, "The ODP patent assignment assignee collection is invalid.");
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentAssignmentAssignee[0];
        }

        PatentAssignmentAssignee[] assignees = new PatentAssignmentAssignee[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            if (bag.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw AssignmentFailure("The ODP patent assignment assignee is invalid.", bag.ArrayItemPaths[index]);
            }

            JsonFieldReader reader = new JsonFieldReader(bag.ArrayItems[index], bag.ArrayItemPaths[index]);
            PatentAssignmentAssignee assignee = new PatentAssignmentAssignee();
            assignee.Name = ReadAliasedString(reader, new string[] { "assigneeNameText", "assigneeName", "name" });
            JsonFieldValue address = reader.ReadObject("assigneeAddress");
            ValidateOptional(address, "The ODP patent assignment assignee address is invalid.");
            if (address.IsPresent && !address.IsNull)
            {
                assignee.Address = ParseAddress(address.ElementValue, address.Path);
            }

            JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "assigneeNameText", "assigneeName", "name", "assigneeAddress" });
            MergeUnknownFields(assignee.UnknownFields, unknown, string.Empty);
            assignees[index] = assignee;
        }

        return assignees;
    }

    internal static PatentAddress ParseAddress(global::System.Text.Json.JsonElement element, string path)
    {
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentAddress address = new PatentAddress();
        address.NameLineOne = ReadOptionalString(reader, "nameLineOneText");
        address.NameLineTwo = ReadOptionalString(reader, "nameLineTwoText");
        address.AddressLineOne = ReadOptionalString(reader, "addressLineOneText");
        address.AddressLineTwo = ReadOptionalString(reader, "addressLineTwoText");
        address.AddressLineThree = ReadOptionalString(reader, "addressLineThreeText");
        address.AddressLineFour = ReadOptionalString(reader, "addressLineFourText");
        address.City = ReadAliasedString(reader, new string[] { "cityName", "city" });
        address.GeographicRegionName = ReadAliasedString(reader, new string[] { "geographicRegionName", "stateName", "regionName" });
        address.GeographicRegionCode = ReadAliasedString(reader, new string[] { "geographicRegionCode", "countryOrStateCode", "ictStateCode", "stateCode" });
        address.PostalCode = ReadAliasedString(reader, new string[] { "postalCode", "zipCode" });
        address.CountryCode = ReadAliasedString(reader, new string[] { "countryCode", "ictCountryCode" });
        address.CountryName = ReadOptionalString(reader, "countryName");
        address.PostalAddressCategory = ReadOptionalString(reader, "postalAddressCategory");
        address.EmailAddress = ReadOptionalString(reader, "emailAddress");
        return address;
    }

    internal static PatentAssignmentCorrespondent[] ParseCorrespondents(JsonFieldReader assignment)
    {
        JsonFieldValue value = assignment.ReadRaw("correspondenceAddress");
        if (!value.IsPresent || value.IsNull)
        {
            return new PatentAssignmentCorrespondent[0];
        }

        if (value.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Object)
        {
            return new PatentAssignmentCorrespondent[] { ParseCorrespondent(value.ElementValue, value.Path) };
        }

        if (value.ElementValue.ValueKind != global::System.Text.Json.JsonValueKind.Array)
        {
            throw AssignmentFailure("The ODP patent assignment correspondence address is invalid.", value.Path);
        }

        PatentAssignmentCorrespondent[] correspondents = new PatentAssignmentCorrespondent[value.ElementValue.GetArrayLength()];
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in value.ElementValue.EnumerateArray())
        {
            string path = value.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            if (item.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw AssignmentFailure("The ODP patent assignment correspondent is invalid.", path);
            }

            correspondents[index] = ParseCorrespondent(item, path);
            index += 1;
        }

        return correspondents;
    }

    internal static PatentAssignmentCorrespondent ParseCorrespondent(global::System.Text.Json.JsonElement element, string path)
    {
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentAssignmentCorrespondent correspondent = new PatentAssignmentCorrespondent();
        correspondent.Name = ReadAliasedString(reader, new string[] { "correspondentNameText", "name", "nameLineOneText" });
        correspondent.Address = ParseAddress(element, path);
        JsonFieldValue lastIngested = reader.ReadDate("lastIngestionDateTime");
        ValidateOptional(lastIngested, "The ODP patent assignment correspondent ingestion time is invalid.");
        if (lastIngested.IsPresent && !lastIngested.IsNull)
        {
            correspondent.LastIngestedAt = lastIngested.DateValue;
            correspondent.HasLastIngestedAt = true;
        }

        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownCorrespondentFields());
        MergeUnknownFields(correspondent.UnknownFields, unknown, string.Empty);
        return correspondent;
    }

    internal static PatentAssignmentCorrespondent? ParseDomesticRepresentative(JsonFieldReader assignment)
    {
        JsonFieldValue value = assignment.ReadObject("domesticRepresentative");
        ValidateOptional(value, "The ODP patent assignment domestic representative is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return null;
        }

        return ParseCorrespondent(value.ElementValue, value.Path);
    }

    internal static PatentAssignmentProperty[] ParseProperties(JsonFieldReader assignment)
    {
        JsonFieldValue value = ReadAliasedRaw(assignment, new string[] { "propertyBag", "patentPropertyBag", "assignmentPropertyBag" });
        if (!value.IsPresent || value.IsNull)
        {
            return new PatentAssignmentProperty[0];
        }

        if (value.ElementValue.ValueKind == global::System.Text.Json.JsonValueKind.Object)
        {
            return new PatentAssignmentProperty[] { ParseProperty(value.ElementValue, value.Path) };
        }

        if (value.ElementValue.ValueKind != global::System.Text.Json.JsonValueKind.Array)
        {
            throw AssignmentFailure("The ODP patent assignment property collection is invalid.", value.Path);
        }

        PatentAssignmentProperty[] properties = new PatentAssignmentProperty[value.ElementValue.GetArrayLength()];
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in value.ElementValue.EnumerateArray())
        {
            string path = value.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]";
            if (item.ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw AssignmentFailure("The ODP patent assignment property is invalid.", path);
            }

            properties[index] = ParseProperty(item, path);
            index += 1;
        }

        return properties;
    }

    internal static PatentAssignmentProperty ParseProperty(global::System.Text.Json.JsonElement element, string path)
    {
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentAssignmentProperty property = new PatentAssignmentProperty();
        property.PropertyIdentifier = ReadAliasedString(reader, new string[] { "propertyIdentifier", "propertyNumber", "propertyNumberText", "documentIdentifier" });
        property.PropertyType = ReadAliasedString(reader, new string[] { "propertyType", "propertyTypeCode", "propertyCategory" });
        property.ApplicationNumber = ReadAliasedString(reader, new string[] { "applicationNumberText", "applicationNumber" });
        property.PatentNumber = ReadAliasedString(reader, new string[] { "patentNumber", "patentNumberText" });
        property.PublicationNumber = ReadAliasedString(reader, new string[] { "publicationNumber", "publicationNumberText" });
        property.Title = ReadAliasedString(reader, new string[] { "inventionTitle", "title", "propertyTitle" });
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownPropertyFields());
        MergeUnknownFields(property.UnknownFields, unknown, string.Empty);
        return property;
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptional(value, "The ODP patent assignment field is invalid.");
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
            ValidateOptional(value, "The ODP patent assignment field is invalid.");
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

    internal static JsonFieldValue ReadAliasedRaw(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadRaw(names[index]);
            if (value.IsPresent)
            {
                return value;
            }
        }

        return reader.ReadRaw(names[0]);
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

    internal static string? ReadRequestIdentifier(JsonFieldReader reader, string? fallback)
    {
        JsonFieldValue value = reader.ReadString("requestIdentifier");
        ValidateOptional(value, "The ODP patent assignment request identifier is invalid.");
        if (value.IsPresent && !value.IsNull)
        {
            return value.StringValue;
        }

        return fallback;
    }

    internal static string FindPresentPath(JsonFieldReader reader, string[] names)
    {
        int index;
        for (index = 0; index < names.Length; index += 1)
        {
            JsonFieldValue value = reader.ReadRaw(names[index]);
            if (value.IsPresent)
            {
                return value.Path;
            }
        }

        return reader.Path + "." + names[0];
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

    internal static void ValidateOptional(JsonFieldValue value, string message)
    {
        if (!value.IsValid)
        {
            throw AssignmentFailure(message, value.Path);
        }
    }

    internal static string[] KnownAssignmentFields()
    {
        return new string[]
        {
            "reelNumber", "frameNumber", "reelAndFrameNumber", "pageTotalQuantity", "imageAvailableStatusCode",
            "assignmentDocumentLocationURI", "documentLocationURI", "assignmentDocumentUri",
            "assignmentReceivedDate", "receivedDate", "assignmentRecordedDate", "recordedDate", "recordationDate", "assignmentMailedDate", "mailedDate",
            "conveyanceText", "conveyance", "attorneyDocketNumber", "docketNumber", "assignorBag", "assigneeBag", "correspondenceAddress",
            "domesticRepresentative", "propertyBag", "patentPropertyBag", "assignmentPropertyBag"
        };
    }

    internal static string[] KnownCorrespondentFields()
    {
        return new string[]
        {
            "correspondentNameText", "name", "nameLineOneText", "nameLineTwoText", "addressLineOneText", "addressLineTwoText",
            "addressLineThreeText", "addressLineFourText", "cityName", "city", "geographicRegionName", "stateName", "regionName",
            "geographicRegionCode", "countryOrStateCode", "ictStateCode", "stateCode", "postalCode", "zipCode", "countryCode",
            "ictCountryCode", "countryName", "postalAddressCategory", "emailAddress", "lastIngestionDateTime"
        };
    }

    internal static string[] KnownPropertyFields()
    {
        return new string[]
        {
            "propertyIdentifier", "propertyNumber", "propertyNumberText", "documentIdentifier", "propertyType", "propertyTypeCode",
            "propertyCategory", "applicationNumberText", "applicationNumber", "patentNumber", "patentNumberText", "publicationNumber",
            "publicationNumberText", "inventionTitle", "title", "propertyTitle"
        };
    }

    internal static ParseException AssignmentFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "ODP patent assignment retrieval", path, 0L, false, 0, false, null);
    }
}
