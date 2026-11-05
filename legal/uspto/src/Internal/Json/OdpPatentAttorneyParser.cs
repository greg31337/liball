namespace uspto;

internal static class OdpPatentAttorneyParser
{
    internal static PatentAttorneyRecord Parse(string json, string expectedApplicationNumber, HttpResponseMetadata responseMetadata)
    {
        if (json == null)
        {
            throw new ValidationException("An ODP patent attorney response is required.", "json", "REQUIRED", null);
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
                throw AttorneyFailure("The ODP patent attorney response must contain exactly one application.", count.Path);
            }

            JsonFieldValue records = root.ReadArray("patentFileWrapperDataBag");
            if (!records.IsPresent || !records.IsValid || records.IsNull || records.ArrayItems.Length != 1)
            {
                throw AttorneyFailure("The ODP patent attorney response has an invalid application collection.", records.Path);
            }

            if (records.ArrayItems[0].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw AttorneyFailure("The ODP patent attorney application record is invalid.", records.ArrayItemPaths[0]);
            }

            JsonFieldReader record = new JsonFieldReader(records.ArrayItems[0], records.ArrayItemPaths[0]);
            string? requestIdentifier = ReadRequestIdentifier(root, responseMetadata.RequestIdentifier);
            requestIdentifier = ReadRequestIdentifier(record, requestIdentifier);
            PatentDataProvenance provenance = OdpJsonParser.CreateProvenance(responseMetadata, requestIdentifier);
            PatentAttorneyRecord result = ParseApplicationRecord(record, expectedApplicationNumber, requestIdentifier, provenance);
            JsonUnknownField[] unknown = root.CaptureUnknownFields(new string[] { "count", "patentFileWrapperDataBag", "requestIdentifier" });
            MergeUnknown(result.UnknownFields, unknown, "response.");
            return result;
        }
    }

    internal static PatentAttorneyRecord ParseApplicationRecord(JsonFieldReader record, string expectedApplicationNumber, string? requestIdentifier, PatentDataProvenance provenance)
    {
        JsonFieldValue applicationNumber = record.ReadString("applicationNumberText");
        if (!applicationNumber.IsPresent || !applicationNumber.IsValid || applicationNumber.IsNull)
        {
            throw AttorneyFailure("The ODP patent attorney application number is missing or invalid.", applicationNumber.Path);
        }

        PatentApplicationNumberResult parsed = PatentIdentifierParser.ParseApplicationNumber(applicationNumber.StringValue ?? string.Empty);
        if (!parsed.IsValid || !global::System.String.Equals(parsed.CanonicalNumber, expectedApplicationNumber, global::System.StringComparison.Ordinal))
        {
            throw AttorneyFailure("The ODP patent attorney response does not match the requested application.", applicationNumber.Path);
        }

        PatentAttorneyRecord result = new PatentAttorneyRecord();
        result.ApplicationNumber = parsed.CanonicalNumber;
        JsonFieldValue attorneyData = record.ReadObject("recordAttorney");
        ValidateOptional(attorneyData, "The ODP patent attorney record is invalid.");
        if (attorneyData.IsPresent && !attorneyData.IsNull)
        {
            ParseRecordData(result, new JsonFieldReader(attorneyData.ElementValue, attorneyData.Path));
        }

        PatentCorrespondence[] wrapperCorrespondence = ParseWrapperCorrespondence(record);
        result.Correspondence = CombineCorrespondence(result.Correspondence, wrapperCorrespondence);
        result.Count = result.Attorneys.Length;
        result.RequestIdentifier = requestIdentifier;
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        JsonUnknownField[] unknown = record.CaptureUnknownFields(new string[] { "applicationNumberText", "recordAttorney", "correspondenceAddressBag", "requestIdentifier" });
        MergeUnknown(result.UnknownFields, unknown, string.Empty);
        return result;
    }

    internal static void ParseRecordData(PatentAttorneyRecord result, JsonFieldReader record)
    {
        PatentAttorney[] powers = ParsePractitionerCollection(record, "powerOfAttorneyBag", true);
        PatentAttorney[] attorneys = ParsePractitionerCollection(record, "attorneyBag", false);
        result.Attorneys = CombineAttorneys(powers, attorneys);
        result.Correspondence = ParseCustomerCorrespondence(record);
        JsonUnknownField[] unknown = record.CaptureUnknownFields(new string[] { "customerNumberCorrespondenceData", "powerOfAttorneyBag", "attorneyBag" });
        MergeUnknown(result.UnknownFields, unknown, "recordAttorney.");
    }

    internal static PatentAttorney[] ParsePractitionerCollection(JsonFieldReader record, string fieldName, bool isPowerOfAttorney)
    {
        JsonFieldValue bag = record.ReadArray(fieldName);
        ValidateOptional(bag, "The ODP patent practitioner collection is invalid.");
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentAttorney[0];
        }

        PatentAttorney[] attorneys = new PatentAttorney[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            if (bag.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw AttorneyFailure("The ODP patent practitioner entry is invalid.", bag.ArrayItemPaths[index]);
            }

            attorneys[index] = ParsePractitioner(bag.ArrayItems[index], bag.ArrayItemPaths[index], isPowerOfAttorney);
        }

        return attorneys;
    }

    internal static PatentAttorney ParsePractitioner(global::System.Text.Json.JsonElement element, string path, bool isPowerOfAttorney)
    {
        JsonFieldReader reader = new JsonFieldReader(element, path);
        PatentAttorney attorney = new PatentAttorney();
        attorney.Person.FirstName = ReadOptionalString(reader, "firstName");
        attorney.Person.MiddleName = ReadOptionalString(reader, "middleName");
        attorney.Person.LastName = ReadOptionalString(reader, "lastName");
        attorney.Person.PreferredName = ReadOptionalString(reader, "preferredName");
        attorney.Person.NamePrefix = ReadOptionalString(reader, "namePrefix");
        attorney.Person.NameSuffix = ReadOptionalString(reader, "nameSuffix");
        attorney.Person.FullName = BuildFullName(attorney.Person);
        attorney.RegistrationNumber = ReadOptionalString(reader, "registrationNumber");
        attorney.PractitionerCategory = ReadOptionalString(reader, "registeredPractitionerCategory");
        attorney.CountryCode = ReadOptionalString(reader, "countryCode");
        string? organizationName = ReadAliasedString(reader, new string[] { "organizationName", "organizationStandardName" });
        if (organizationName != null)
        {
            attorney.Organization = new PatentOrganization();
            attorney.Organization.Name = organizationName;
            attorney.Organization.StandardName = organizationName;
        }

        attorney.Addresses = ParseAddressCollection(reader, "attorneyAddressBag");
        attorney.Telecommunications = ParseTelecommunicationCollection(reader, "telecommunicationAddressBag");
        ApplyActiveStatus(attorney, reader);
        attorney.IsPowerOfAttorney = isPowerOfAttorney;
        attorney.HasIsPowerOfAttorney = true;
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownPractitionerFields());
        MergeUnknown(attorney.UnknownFields, unknown, string.Empty);
        return attorney;
    }

    internal static void ApplyActiveStatus(PatentAttorney attorney, JsonFieldReader reader)
    {
        JsonFieldValue value = reader.ReadString("activeIndicator");
        ValidateOptional(value, "The ODP patent practitioner active status is invalid.");
        if (!value.IsPresent || value.IsNull || value.StringValue == null)
        {
            return;
        }

        attorney.Status = value.StringValue;
        string normalized = value.StringValue.Trim();
        attorney.IsActive = normalized.Equals("ACTIVE", global::System.StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Y", global::System.StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("YES", global::System.StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("TRUE", global::System.StringComparison.OrdinalIgnoreCase)
            || normalized == "1";
        attorney.HasIsActive = true;
    }

    internal static PatentCorrespondence[] ParseCustomerCorrespondence(JsonFieldReader record)
    {
        JsonFieldValue value = record.ReadObject("customerNumberCorrespondenceData");
        ValidateOptional(value, "The ODP patent correspondence record is invalid.");
        if (!value.IsPresent || value.IsNull)
        {
            return new PatentCorrespondence[0];
        }

        JsonFieldReader reader = new JsonFieldReader(value.ElementValue, value.Path);
        string? customerNumber = ReadOptionalString(reader, "patronIdentifier");
        string? organizationName = ReadAliasedString(reader, new string[] { "organizationStandardName", "organizationName" });
        string? docketNumber = ReadAliasedString(reader, new string[] { "docketNumber", "attorneyDocketNumber" });
        PatentAddress[] addresses = ParseAddressCollection(reader, "powerOfAttorneyAddressBag");
        PatentTelecommunication[] telecommunications = ParseTelecommunicationCollection(reader, "telecommunicationAddressBag");
        JsonUnknownField[] unknown = reader.CaptureUnknownFields(new string[] { "patronIdentifier", "organizationStandardName", "organizationName", "docketNumber", "attorneyDocketNumber", "powerOfAttorneyAddressBag", "telecommunicationAddressBag" });
        if (addresses.Length == 0)
        {
            PatentCorrespondence emptyAddress = CreateCustomerCorrespondence(customerNumber, organizationName, docketNumber, new PatentAddress(), telecommunications, unknown);
            return new PatentCorrespondence[] { emptyAddress };
        }

        PatentCorrespondence[] correspondence = new PatentCorrespondence[addresses.Length];
        int index;
        for (index = 0; index < addresses.Length; index += 1)
        {
            correspondence[index] = CreateCustomerCorrespondence(customerNumber, organizationName, docketNumber, addresses[index], telecommunications, unknown);
        }

        return correspondence;
    }

    internal static PatentCorrespondence[] ParseWrapperCorrespondence(JsonFieldReader record)
    {
        JsonFieldValue bag = record.ReadArray("correspondenceAddressBag");
        ValidateOptional(bag, "The ODP patent correspondence address collection is invalid.");
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentCorrespondence[0];
        }

        PatentCorrespondence[] correspondence = new PatentCorrespondence[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            if (bag.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw AttorneyFailure("The ODP patent correspondence address is invalid.", bag.ArrayItemPaths[index]);
            }

            JsonFieldReader reader = new JsonFieldReader(bag.ArrayItems[index], bag.ArrayItemPaths[index]);
            PatentCorrespondence item = new PatentCorrespondence();
            item.Address = ParseAddress(bag.ArrayItems[index], bag.ArrayItemPaths[index]);
            item.CustomerNumber = ReadAliasedString(reader, new string[] { "customerNumber", "patronIdentifier" });
            item.DocketNumber = ReadAliasedString(reader, new string[] { "docketNumber", "attorneyDocketNumber" });
            if (item.Address.NameLineOne != null || item.Address.NameLineTwo != null)
            {
                item.Recipient.Organization = new PatentOrganization();
                item.Recipient.Organization.Name = item.Address.NameLineOne;
                item.Recipient.Organization.StandardName = item.Address.NameLineOne;
            }

            JsonUnknownField[] unknown = reader.CaptureUnknownFields(KnownWrapperCorrespondenceFields());
            MergeUnknown(item.UnknownFields, unknown, string.Empty);
            correspondence[index] = item;
        }

        return correspondence;
    }

    internal static PatentCorrespondence CreateCustomerCorrespondence(string? customerNumber, string? organizationName, string? docketNumber, PatentAddress address, PatentTelecommunication[] telecommunications, JsonUnknownField[] unknown)
    {
        PatentCorrespondence correspondence = new PatentCorrespondence();
        correspondence.CustomerNumber = customerNumber;
        correspondence.DocketNumber = docketNumber;
        correspondence.Address = address;
        correspondence.Telecommunications = telecommunications;
        if (organizationName != null)
        {
            correspondence.Recipient.Organization = new PatentOrganization();
            correspondence.Recipient.Organization.Name = organizationName;
            correspondence.Recipient.Organization.StandardName = organizationName;
        }

        MergeUnknown(correspondence.UnknownFields, unknown, string.Empty);
        return correspondence;
    }

    internal static PatentAddress[] ParseAddressCollection(JsonFieldReader reader, string fieldName)
    {
        JsonFieldValue bag = reader.ReadArray(fieldName);
        ValidateOptional(bag, "The ODP patent attorney address collection is invalid.");
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentAddress[0];
        }

        PatentAddress[] addresses = new PatentAddress[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            if (bag.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw AttorneyFailure("The ODP patent attorney address is invalid.", bag.ArrayItemPaths[index]);
            }

            addresses[index] = ParseAddress(bag.ArrayItems[index], bag.ArrayItemPaths[index]);
        }

        return addresses;
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

    internal static PatentTelecommunication[] ParseTelecommunicationCollection(JsonFieldReader reader, string fieldName)
    {
        JsonFieldValue bag = reader.ReadArray(fieldName);
        ValidateOptional(bag, "The ODP patent telecommunications collection is invalid.");
        if (!bag.IsPresent || bag.IsNull)
        {
            return new PatentTelecommunication[0];
        }

        PatentTelecommunication[] telecommunications = new PatentTelecommunication[bag.ArrayItems.Length];
        int index;
        for (index = 0; index < bag.ArrayItems.Length; index += 1)
        {
            if (bag.ArrayItems[index].ValueKind != global::System.Text.Json.JsonValueKind.Object)
            {
                throw AttorneyFailure("The ODP patent telecommunications entry is invalid.", bag.ArrayItemPaths[index]);
            }

            JsonFieldReader telecom = new JsonFieldReader(bag.ArrayItems[index], bag.ArrayItemPaths[index]);
            PatentTelecommunication item = new PatentTelecommunication();
            item.Number = ReadAliasedString(telecom, new string[] { "telecommunicationNumber", "telephoneNumber", "number" });
            item.Extension = ReadAliasedString(telecom, new string[] { "extensionNumber", "extension" });
            item.TypeCode = ReadAliasedString(telecom, new string[] { "telecomTypeCode", "telecommunicationTypeCode", "typeCode" });
            telecommunications[index] = item;
        }

        return telecommunications;
    }

    internal static PatentAttorney[] CombineAttorneys(PatentAttorney[] first, PatentAttorney[] second)
    {
        PatentAttorney[] combined = new PatentAttorney[first.Length + second.Length];
        global::System.Array.Copy(first, 0, combined, 0, first.Length);
        global::System.Array.Copy(second, 0, combined, first.Length, second.Length);
        return combined;
    }

    internal static PatentCorrespondence[] CombineCorrespondence(PatentCorrespondence[] first, PatentCorrespondence[] second)
    {
        PatentCorrespondence[] combined = new PatentCorrespondence[first.Length + second.Length];
        global::System.Array.Copy(first, 0, combined, 0, first.Length);
        global::System.Array.Copy(second, 0, combined, first.Length, second.Length);
        return combined;
    }

    internal static string? BuildFullName(PatentPerson person)
    {
        global::System.Collections.Generic.List<string> parts = new global::System.Collections.Generic.List<string>();
        AddNamePart(parts, person.NamePrefix);
        AddNamePart(parts, person.FirstName);
        AddNamePart(parts, person.MiddleName);
        AddNamePart(parts, person.LastName);
        AddNamePart(parts, person.NameSuffix);
        if (parts.Count == 0)
        {
            return person.PreferredName;
        }

        return global::System.String.Join(" ", parts);
    }

    internal static void AddNamePart(global::System.Collections.Generic.List<string> parts, string? value)
    {
        if (!global::System.String.IsNullOrWhiteSpace(value))
        {
            parts.Add(value ?? string.Empty);
        }
    }

    internal static string? ReadOptionalString(JsonFieldReader reader, string name)
    {
        JsonFieldValue value = reader.ReadString(name);
        ValidateOptional(value, "The ODP patent attorney field is invalid.");
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
            ValidateOptional(value, "The ODP patent attorney field is invalid.");
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
        ValidateOptional(value, "The ODP patent attorney request identifier is invalid.");
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
            throw AttorneyFailure(message, value.Path);
        }
    }

    internal static string[] KnownPractitionerFields()
    {
        return new string[]
        {
            "firstName", "middleName", "lastName", "preferredName", "namePrefix", "nameSuffix", "registrationNumber",
            "registeredPractitionerCategory", "countryCode", "organizationName", "organizationStandardName", "attorneyAddressBag",
            "telecommunicationAddressBag", "activeIndicator"
        };
    }

    internal static string[] KnownWrapperCorrespondenceFields()
    {
        return new string[]
        {
            "nameLineOneText", "nameLineTwoText", "addressLineOneText", "addressLineTwoText", "addressLineThreeText", "addressLineFourText",
            "cityName", "city", "geographicRegionName", "stateName", "regionName", "geographicRegionCode", "countryOrStateCode",
            "ictStateCode", "stateCode", "postalCode", "zipCode", "countryCode", "ictCountryCode", "countryName",
            "postalAddressCategory", "emailAddress", "customerNumber", "patronIdentifier", "docketNumber", "attorneyDocketNumber"
        };
    }

    internal static ParseException AttorneyFailure(string message, string path)
    {
        return new ParseException(message, DocumentFormat.Json, "ODP patent attorney retrieval", path, 0L, false, 0, false, null);
    }
}
