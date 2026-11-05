using System;
using System.Collections.Generic;
using System.Text.Json;

namespace diameter;

internal static class JsonDictionarySchema
{
    internal const int CurrentVersion = 1;

    internal static readonly string[] RootProperties = new string[]
    {
        "schemaVersion", "name", "version", "dependencies", "vendors",
        "applications", "types", "enumValues", "avps", "commands"
    };

    internal static readonly string[] VendorProperties = new string[]
    {
        "id", "name"
    };

    internal static readonly string[] ApplicationProperties = new string[]
    {
        "id", "vendorId", "name", "isAccounting"
    };

    internal static readonly string[] TypeProperties = new string[]
    {
        "name", "vendorId", "baseType", "parentTypeName", "parentTypeVendorId",
        "fixedDataLength"
    };

    internal static readonly string[] EnumValueProperties = new string[]
    {
        "name", "value", "typeName", "typeVendorId", "avpCode", "avpVendorId"
    };

    internal static readonly string[] AvpProperties = new string[]
    {
        "code", "vendorId", "name", "typeName", "baseType", "requiredFlags",
        "prohibitedFlags", "fixedDataLength", "groupedRules", "allowUnlistedAvps"
    };

    internal static readonly string[] CommandProperties = new string[]
    {
        "code", "applicationId", "name", "isRequest", "requiredFlags",
        "prohibitedFlags", "rules", "allowUnlistedAvps"
    };

    internal static readonly string[] RuleProperties = new string[]
    {
        "avpCode", "avpVendorId", "avpName", "minimumCount", "maximumCount",
        "fixedHeadPosition", "orderingGroup"
    };
}

internal static class JsonDictionaryLoader
{
    internal static DiameterDictionaryModule LoadDictionaryModule(string? json)
    {
        if (json == null)
        {
            throw CreateSchemaError("$", "A JSON dictionary document is required.");
        }

        using (JsonDocument document = JsonDocument.Parse(json))
        {
            return ParseRoot(document.RootElement);
        }
    }

    internal static DiameterDictionaryModule ParseRoot(JsonElement root)
    {
        RequireObject(root, "$", JsonDictionarySchema.RootProperties);
        int schemaVersion = ReadRequiredInt32(root, "schemaVersion", "$");
        if (schemaVersion != JsonDictionarySchema.CurrentVersion)
        {
            throw CreateSchemaError(
                "$.schemaVersion",
                "Only JSON dictionary schema version 1 is supported.");
        }

        DiameterDictionaryModule module = new DiameterDictionaryModule();
        module.Name = ReadRequiredString(root, "name", "$");
        module.Version = ReadOptionalString(root, "version", "$", string.Empty);
        ReadDependencies(root, module);
        ReadVendors(root, module);
        ReadApplications(root, module);
        ReadTypes(root, module);
        ReadEnumValues(root, module);
        ReadAvps(root, module);
        ReadCommands(root, module);
        return module;
    }

    internal static void ReadDependencies(
        JsonElement root,
        DiameterDictionaryModule module)
    {
        JsonElement array = ReadOptionalArray(root, "dependencies", "$");
        if (array.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }

        int index = 0;
        foreach (JsonElement item in array.EnumerateArray())
        {
            string path = CreateArrayPath("$.dependencies", index);
            module.Dependencies.Add(ReadStringValue(item, path));
            index = index + 1;
        }
    }

    internal static void ReadVendors(JsonElement root, DiameterDictionaryModule module)
    {
        JsonElement array = ReadOptionalArray(root, "vendors", "$");
        if (array.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }

        int index = 0;
        foreach (JsonElement item in array.EnumerateArray())
        {
            string path = CreateArrayPath("$.vendors", index);
            RequireObject(item, path, JsonDictionarySchema.VendorProperties);
            DiameterVendorDefinition vendor = new DiameterVendorDefinition();
            vendor.Id = ReadRequiredUInt32(item, "id", path);
            vendor.Name = ReadRequiredString(item, "name", path);
            module.Vendors.Add(vendor);
            index = index + 1;
        }
    }

    internal static void ReadApplications(
        JsonElement root,
        DiameterDictionaryModule module)
    {
        JsonElement array = ReadOptionalArray(root, "applications", "$");
        if (array.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }

        int index = 0;
        foreach (JsonElement item in array.EnumerateArray())
        {
            string path = CreateArrayPath("$.applications", index);
            RequireObject(item, path, JsonDictionarySchema.ApplicationProperties);
            DiameterApplicationDefinition application = new DiameterApplicationDefinition();
            application.Id = ReadRequiredUInt32(item, "id", path);
            application.VendorId = ReadOptionalUInt32(item, "vendorId", path, 0U);
            application.Name = ReadRequiredString(item, "name", path);
            application.IsAccounting = ReadOptionalBoolean(
                item,
                "isAccounting",
                path,
                false);
            module.Applications.Add(application);
            index = index + 1;
        }
    }

    internal static void ReadTypes(JsonElement root, DiameterDictionaryModule module)
    {
        JsonElement array = ReadOptionalArray(root, "types", "$");
        if (array.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }

        int index = 0;
        foreach (JsonElement item in array.EnumerateArray())
        {
            string path = CreateArrayPath("$.types", index);
            RequireObject(item, path, JsonDictionarySchema.TypeProperties);
            DiameterTypeDefinition type = new DiameterTypeDefinition();
            type.Name = ReadRequiredString(item, "name", path);
            type.VendorId = ReadOptionalUInt32(item, "vendorId", path, 0U);
            type.BaseType = ReadRequiredBaseType(item, "baseType", path);
            type.ParentTypeName = ReadOptionalString(
                item,
                "parentTypeName",
                path,
                string.Empty);
            type.ParentTypeVendorId = ReadOptionalUInt32(
                item,
                "parentTypeVendorId",
                path,
                0U);
            type.FixedDataLength = ReadOptionalInt32(
                item,
                "fixedDataLength",
                path,
                -1);
            if (type.FixedDataLength < -1)
            {
                throw CreateSchemaError(
                    path + ".fixedDataLength",
                    "A fixed data length must be -1 or a nonnegative integer.");
            }

            module.Types.Add(type);
            index = index + 1;
        }
    }

    internal static void ReadEnumValues(
        JsonElement root,
        DiameterDictionaryModule module)
    {
        JsonElement array = ReadOptionalArray(root, "enumValues", "$");
        if (array.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }

        int index = 0;
        foreach (JsonElement item in array.EnumerateArray())
        {
            string path = CreateArrayPath("$.enumValues", index);
            RequireObject(item, path, JsonDictionarySchema.EnumValueProperties);
            DiameterEnumValueDefinition value = new DiameterEnumValueDefinition();
            value.Name = ReadRequiredString(item, "name", path);
            value.Value = ReadRequiredInt32(item, "value", path);
            value.TypeName = ReadOptionalString(item, "typeName", path, string.Empty);
            value.TypeVendorId = ReadOptionalUInt32(item, "typeVendorId", path, 0U);
            value.AvpCode = ReadOptionalUInt32(item, "avpCode", path, 0U);
            value.AvpVendorId = ReadOptionalUInt32(item, "avpVendorId", path, 0U);
            if (string.IsNullOrWhiteSpace(value.TypeName) && value.AvpCode == 0U)
            {
                throw CreateSchemaError(
                    path,
                    "An enum value requires a typeName or a nonzero avpCode.");
            }

            module.EnumValues.Add(value);
            index = index + 1;
        }
    }

    internal static void ReadAvps(JsonElement root, DiameterDictionaryModule module)
    {
        JsonElement array = ReadOptionalArray(root, "avps", "$");
        if (array.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }

        int index = 0;
        foreach (JsonElement item in array.EnumerateArray())
        {
            string path = CreateArrayPath("$.avps", index);
            RequireObject(item, path, JsonDictionarySchema.AvpProperties);
            DiameterAvpDefinition avp = new DiameterAvpDefinition();
            avp.Code = ReadRequiredUInt32(item, "code", path);
            avp.VendorId = ReadOptionalUInt32(item, "vendorId", path, 0U);
            avp.Name = ReadRequiredString(item, "name", path);
            avp.TypeName = ReadRequiredString(item, "typeName", path);
            avp.BaseType = ReadRequiredBaseType(item, "baseType", path);
            avp.RequiredFlags = ReadOptionalAvpFlags(
                item,
                "requiredFlags",
                path);
            avp.ProhibitedFlags = ReadOptionalAvpFlags(
                item,
                "prohibitedFlags",
                path);
            avp.FixedDataLength = ReadOptionalInt32(
                item,
                "fixedDataLength",
                path,
                -1);
            if (avp.FixedDataLength < -1)
            {
                throw CreateSchemaError(
                    path + ".fixedDataLength",
                    "A fixed data length must be -1 or a nonnegative integer.");
            }

            avp.AllowUnlistedAvps = ReadOptionalBoolean(
                item,
                "allowUnlistedAvps",
                path,
                true);
            avp.GroupedRules = ReadOptionalRules(item, "groupedRules", path);
            module.Avps.Add(avp);
            index = index + 1;
        }
    }

    internal static void ReadCommands(JsonElement root, DiameterDictionaryModule module)
    {
        JsonElement array = ReadOptionalArray(root, "commands", "$");
        if (array.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }

        int index = 0;
        foreach (JsonElement item in array.EnumerateArray())
        {
            string path = CreateArrayPath("$.commands", index);
            RequireObject(item, path, JsonDictionarySchema.CommandProperties);
            DiameterCommandDefinition command = new DiameterCommandDefinition();
            command.Code = ReadRequiredUInt32(item, "code", path);
            command.ApplicationId = ReadRequiredUInt32(item, "applicationId", path);
            command.Name = ReadRequiredString(item, "name", path);
            command.IsRequest = ReadRequiredBoolean(item, "isRequest", path);
            command.RequiredFlags = ReadOptionalMessageFlags(
                item,
                "requiredFlags",
                path);
            command.ProhibitedFlags = ReadOptionalMessageFlags(
                item,
                "prohibitedFlags",
                path);
            command.AllowUnlistedAvps = ReadOptionalBoolean(
                item,
                "allowUnlistedAvps",
                path,
                true);
            command.Rules = ReadOptionalRules(item, "rules", path);
            module.Commands.Add(command);
            index = index + 1;
        }
    }

    internal static List<DiameterAvpRuleDefinition> ReadOptionalRules(
        JsonElement parent,
        string propertyName,
        string parentPath)
    {
        List<DiameterAvpRuleDefinition> result = new List<DiameterAvpRuleDefinition>();
        JsonElement array = ReadOptionalArray(parent, propertyName, parentPath);
        if (array.ValueKind == JsonValueKind.Undefined)
        {
            return result;
        }

        string arrayPath = parentPath + "." + propertyName;
        int index = 0;
        foreach (JsonElement item in array.EnumerateArray())
        {
            string path = CreateArrayPath(arrayPath, index);
            RequireObject(item, path, JsonDictionarySchema.RuleProperties);
            DiameterAvpRuleDefinition rule = new DiameterAvpRuleDefinition();
            rule.AvpCode = ReadRequiredUInt32(item, "avpCode", path);
            rule.AvpVendorId = ReadOptionalUInt32(item, "avpVendorId", path, 0U);
            rule.AvpName = ReadRequiredString(item, "avpName", path);
            rule.MinimumCount = ReadOptionalInt32(item, "minimumCount", path, 0);
            rule.MaximumCount = ReadOptionalInt32(item, "maximumCount", path, 1);
            rule.FixedHeadPosition = ReadOptionalInt32(
                item,
                "fixedHeadPosition",
                path,
                -1);
            rule.OrderingGroup = ReadOptionalInt32(item, "orderingGroup", path, -1);
            ValidateRule(rule, path);
            result.Add(rule);
            index = index + 1;
        }

        return result;
    }

    internal static void ValidateRule(DiameterAvpRuleDefinition rule, string path)
    {
        if (rule.MinimumCount < 0)
        {
            throw CreateSchemaError(
                path + ".minimumCount",
                "A minimum count must be nonnegative.");
        }

        if (rule.MaximumCount < -1 ||
            (rule.MaximumCount >= 0 && rule.MaximumCount < rule.MinimumCount))
        {
            throw CreateSchemaError(
                path + ".maximumCount",
                "A maximum count must be -1 or at least the minimum count.");
        }

        if (rule.FixedHeadPosition < -1)
        {
            throw CreateSchemaError(
                path + ".fixedHeadPosition",
                "A fixed-head position must be -1 or nonnegative.");
        }

        if (rule.OrderingGroup < -1)
        {
            throw CreateSchemaError(
                path + ".orderingGroup",
                "An ordering group must be -1 or nonnegative.");
        }
    }

    internal static DiameterAvpBaseType ReadRequiredBaseType(
        JsonElement parent,
        string propertyName,
        string parentPath)
    {
        JsonElement value = ReadRequiredProperty(parent, propertyName, parentPath);
        string path = parentPath + "." + propertyName;
        string name = ReadStringValue(value, path);
        if (string.Equals(name, "OctetString", StringComparison.OrdinalIgnoreCase))
        {
            return DiameterAvpBaseType.OctetString;
        }

        if (string.Equals(name, "Integer32", StringComparison.OrdinalIgnoreCase))
        {
            return DiameterAvpBaseType.Integer32;
        }

        if (string.Equals(name, "Integer64", StringComparison.OrdinalIgnoreCase))
        {
            return DiameterAvpBaseType.Integer64;
        }

        if (string.Equals(name, "Unsigned32", StringComparison.OrdinalIgnoreCase))
        {
            return DiameterAvpBaseType.Unsigned32;
        }

        if (string.Equals(name, "Unsigned64", StringComparison.OrdinalIgnoreCase))
        {
            return DiameterAvpBaseType.Unsigned64;
        }

        if (string.Equals(name, "Float32", StringComparison.OrdinalIgnoreCase))
        {
            return DiameterAvpBaseType.Float32;
        }

        if (string.Equals(name, "Float64", StringComparison.OrdinalIgnoreCase))
        {
            return DiameterAvpBaseType.Float64;
        }

        if (string.Equals(name, "Grouped", StringComparison.OrdinalIgnoreCase))
        {
            return DiameterAvpBaseType.Grouped;
        }

        throw CreateSchemaError(path, "The AVP base type name is not supported.");
    }

    internal static DiameterMessageFlags ReadOptionalMessageFlags(
        JsonElement parent,
        string propertyName,
        string parentPath)
    {
        JsonElement value = ReadOptionalProperty(parent, propertyName, parentPath);
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            return DiameterMessageFlags.None;
        }

        string path = parentPath + "." + propertyName;
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw CreateSchemaError(path, "Message flags must be a JSON array of names.");
        }

        DiameterMessageFlags result = DiameterMessageFlags.None;
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            string itemPath = CreateArrayPath(path, index);
            string name = ReadStringValue(item, itemPath);
            if (string.Equals(name, "None", StringComparison.OrdinalIgnoreCase))
            {
                result = result | DiameterMessageFlags.None;
            }
            else if (string.Equals(name, "Request", StringComparison.OrdinalIgnoreCase))
            {
                result = result | DiameterMessageFlags.Request;
            }
            else if (string.Equals(name, "Proxyable", StringComparison.OrdinalIgnoreCase))
            {
                result = result | DiameterMessageFlags.Proxyable;
            }
            else if (string.Equals(name, "Error", StringComparison.OrdinalIgnoreCase))
            {
                result = result | DiameterMessageFlags.Error;
            }
            else if (string.Equals(
                name,
                "PotentiallyRetransmitted",
                StringComparison.OrdinalIgnoreCase))
            {
                result = result | DiameterMessageFlags.PotentiallyRetransmitted;
            }
            else
            {
                throw CreateSchemaError(itemPath, "The message flag name is not supported.");
            }

            index = index + 1;
        }

        return result;
    }

    internal static DiameterAvpFlags ReadOptionalAvpFlags(
        JsonElement parent,
        string propertyName,
        string parentPath)
    {
        JsonElement value = ReadOptionalProperty(parent, propertyName, parentPath);
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            return DiameterAvpFlags.None;
        }

        string path = parentPath + "." + propertyName;
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw CreateSchemaError(path, "AVP flags must be a JSON array of names.");
        }

        DiameterAvpFlags result = DiameterAvpFlags.None;
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            string itemPath = CreateArrayPath(path, index);
            string name = ReadStringValue(item, itemPath);
            if (string.Equals(name, "None", StringComparison.OrdinalIgnoreCase))
            {
                result = result | DiameterAvpFlags.None;
            }
            else if (string.Equals(name, "VendorSpecific", StringComparison.OrdinalIgnoreCase))
            {
                result = result | DiameterAvpFlags.VendorSpecific;
            }
            else if (string.Equals(name, "Mandatory", StringComparison.OrdinalIgnoreCase))
            {
                result = result | DiameterAvpFlags.Mandatory;
            }
            else if (string.Equals(name, "Protected", StringComparison.OrdinalIgnoreCase))
            {
                result = result | DiameterAvpFlags.Protected;
            }
            else
            {
                throw CreateSchemaError(itemPath, "The AVP flag name is not supported.");
            }

            index = index + 1;
        }

        return result;
    }

    internal static string ReadRequiredString(
        JsonElement parent,
        string propertyName,
        string parentPath)
    {
        JsonElement value = ReadRequiredProperty(parent, propertyName, parentPath);
        return ReadStringValue(value, parentPath + "." + propertyName);
    }

    internal static string ReadOptionalString(
        JsonElement parent,
        string propertyName,
        string parentPath,
        string defaultValue)
    {
        JsonElement value = ReadOptionalProperty(parent, propertyName, parentPath);
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            return defaultValue;
        }

        return ReadStringValue(value, parentPath + "." + propertyName);
    }

    internal static string ReadStringValue(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw CreateSchemaError(path, "A nonempty JSON string is required.");
        }

        string? result = value.GetString();
        if (string.IsNullOrWhiteSpace(result))
        {
            throw CreateSchemaError(path, "A nonempty JSON string is required.");
        }

        return result;
    }

    internal static uint ReadRequiredUInt32(
        JsonElement parent,
        string propertyName,
        string parentPath)
    {
        JsonElement value = ReadRequiredProperty(parent, propertyName, parentPath);
        return ReadUInt32Value(value, parentPath + "." + propertyName);
    }

    internal static uint ReadOptionalUInt32(
        JsonElement parent,
        string propertyName,
        string parentPath,
        uint defaultValue)
    {
        JsonElement value = ReadOptionalProperty(parent, propertyName, parentPath);
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            return defaultValue;
        }

        return ReadUInt32Value(value, parentPath + "." + propertyName);
    }

    internal static uint ReadUInt32Value(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            throw CreateSchemaError(path, "An unsigned 32-bit JSON integer is required.");
        }

        string text = value.GetRawText();
        if (text.Length == 0)
        {
            throw CreateSchemaError(path, "An unsigned 32-bit JSON integer is required.");
        }

        uint result = 0U;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                throw CreateSchemaError(path, "An unsigned 32-bit JSON integer is required.");
            }

            uint digit = (uint)(character - '0');
            if (result > (uint.MaxValue - digit) / 10U)
            {
                throw CreateSchemaError(path, "The unsigned integer exceeds UInt32 range.");
            }

            result = result * 10U + digit;
            index = index + 1;
        }

        return result;
    }

    internal static int ReadRequiredInt32(
        JsonElement parent,
        string propertyName,
        string parentPath)
    {
        JsonElement value = ReadRequiredProperty(parent, propertyName, parentPath);
        return ReadInt32Value(value, parentPath + "." + propertyName);
    }

    internal static int ReadOptionalInt32(
        JsonElement parent,
        string propertyName,
        string parentPath,
        int defaultValue)
    {
        JsonElement value = ReadOptionalProperty(parent, propertyName, parentPath);
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            return defaultValue;
        }

        return ReadInt32Value(value, parentPath + "." + propertyName);
    }

    internal static int ReadInt32Value(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            throw CreateSchemaError(path, "A signed 32-bit JSON integer is required.");
        }

        string text = value.GetRawText();
        bool negative = text.Length > 0 && text[0] == '-';
        int index = negative ? 1 : 0;
        if (index >= text.Length)
        {
            throw CreateSchemaError(path, "A signed 32-bit JSON integer is required.");
        }

        long magnitude = 0L;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                throw CreateSchemaError(path, "A signed 32-bit JSON integer is required.");
            }

            magnitude = magnitude * 10L + (long)(character - '0');
            if ((!negative && magnitude > int.MaxValue) ||
                (negative && magnitude > 2147483648L))
            {
                throw CreateSchemaError(path, "The signed integer exceeds Int32 range.");
            }

            index = index + 1;
        }

        if (negative && magnitude == 2147483648L)
        {
            return int.MinValue;
        }

        if (negative)
        {
            return -(int)magnitude;
        }

        return (int)magnitude;
    }

    internal static bool ReadRequiredBoolean(
        JsonElement parent,
        string propertyName,
        string parentPath)
    {
        JsonElement value = ReadRequiredProperty(parent, propertyName, parentPath);
        return ReadBooleanValue(value, parentPath + "." + propertyName);
    }

    internal static bool ReadOptionalBoolean(
        JsonElement parent,
        string propertyName,
        string parentPath,
        bool defaultValue)
    {
        JsonElement value = ReadOptionalProperty(parent, propertyName, parentPath);
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            return defaultValue;
        }

        return ReadBooleanValue(value, parentPath + "." + propertyName);
    }

    internal static bool ReadBooleanValue(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (value.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        throw CreateSchemaError(path, "A JSON boolean is required.");
    }

    internal static JsonElement ReadOptionalArray(
        JsonElement parent,
        string propertyName,
        string parentPath)
    {
        JsonElement value = ReadOptionalProperty(parent, propertyName, parentPath);
        if (value.ValueKind != JsonValueKind.Undefined &&
            value.ValueKind != JsonValueKind.Array)
        {
            throw CreateSchemaError(
                parentPath + "." + propertyName,
                "A JSON array is required.");
        }

        return value;
    }

    internal static JsonElement ReadRequiredProperty(
        JsonElement parent,
        string propertyName,
        string parentPath)
    {
        JsonElement result = ReadOptionalProperty(parent, propertyName, parentPath);
        if (result.ValueKind == JsonValueKind.Undefined)
        {
            throw CreateSchemaError(
                parentPath + "." + propertyName,
                "The required property is missing.");
        }

        return result;
    }

    internal static JsonElement ReadOptionalProperty(
        JsonElement parent,
        string propertyName,
        string parentPath)
    {
        JsonElement result = default(JsonElement);
        bool found = false;
        foreach (JsonProperty property in parent.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.Ordinal))
            {
                if (found)
                {
                    throw CreateSchemaError(
                        parentPath + "." + propertyName,
                        "The property is duplicated.");
                }

                found = true;
                result = property.Value;
            }
        }

        return result;
    }

    internal static void RequireObject(
        JsonElement value,
        string path,
        string[] allowedProperties)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw CreateSchemaError(path, "A JSON object is required.");
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            bool allowed = false;
            int index = 0;
            while (index < allowedProperties.Length)
            {
                if (string.Equals(
                    property.Name,
                    allowedProperties[index],
                    StringComparison.Ordinal))
                {
                    allowed = true;
                    break;
                }

                index = index + 1;
            }

            if (!allowed)
            {
                throw CreateSchemaError(
                    path + "." + property.Name,
                    "The property is not defined by JSON dictionary schema version 1.");
            }
        }
    }

    internal static string CreateArrayPath(string path, int index)
    {
        return path + "[" + index + "]";
    }

    internal static DiameterDictionaryException CreateSchemaError(
        string path,
        string message)
    {
        return new DiameterDictionaryException(
            "JSON dictionary error at " + path + ": " + message);
    }
}
