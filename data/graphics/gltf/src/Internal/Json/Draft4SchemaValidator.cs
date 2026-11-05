using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace gltf;

internal class Draft4SchemaValidator
{
    internal CoreSchemaCatalog CoreSchemas = new CoreSchemaCatalog();
    internal Dictionary<string, JsonObject> Schemas = new Dictionary<string, JsonObject>();
    internal JsonPointerBuilder Pointers = new JsonPointerBuilder();
    internal List<string> ActiveFrames = new List<string>();

    internal Draft4SchemaValidator()
    {
        int index = 0;
        while (index < CoreSchemas.SchemaNames.Count)
        {
            string name = CoreSchemas.SchemaNames[index];
            Schemas.Add(name, CoreSchemas.GetCoreSchema(name));
            index++;
        }
    }

    internal void RegisterSchemaDocument(string name, JsonObject schema)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (schema == null)
        {
            throw new ArgumentNullException("schema");
        }

        string normalizedName = CoreSchemas.GetSchemaName(name);
        if (normalizedName.Length == 0)
        {
            throw new ArgumentException("A schema name is required.", "name");
        }

        if (Schemas.ContainsKey(normalizedName))
        {
            Schemas[normalizedName] = schema;
        }
        else
        {
            Schemas.Add(normalizedName, schema);
        }
    }

    internal Draft4SchemaValidation ValidateJson(
        JsonNode? instance,
        string schemaName)
    {
        if (schemaName == null)
        {
            throw new ArgumentNullException("schemaName");
        }

        string normalizedName = CoreSchemas.GetSchemaName(schemaName);
        if (!Schemas.ContainsKey(normalizedName))
        {
            throw new ArgumentException(
                $"Schema '{schemaName}' is not registered.",
                "schemaName");
        }

        ActiveFrames.Clear();
        Draft4SchemaValidation result = new Draft4SchemaValidation();
        EvaluateSchema(
            instance,
            Schemas[normalizedName],
            normalizedName,
            "",
            "",
            result);
        return result;
    }

    internal void EvaluateSchema(
        JsonNode? instance,
        JsonObject schema,
        string schemaName,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        string frame = $"{schemaName}#{schemaPointer}|{instancePointer}";
        if (ActiveFrames.Contains(frame))
        {
            return;
        }

        ActiveFrames.Add(frame);
        if (schema.ContainsKey("$ref") && schema["$ref"] != null)
        {
            EvaluateReference(
                instance,
                ReadKeyword(schema, "$ref").GetValue<string>(),
                schemaName,
                schemaPointer,
                instancePointer,
                result);
            ActiveFrames.RemoveAt(ActiveFrames.Count - 1);
            return;
        }

        EvaluateComposition(
            instance,
            schema,
            schemaName,
            schemaPointer,
            instancePointer,
            result);
        EvaluateEnumAndConstant(instance, schema, schemaPointer, instancePointer, result);
        if (!EvaluateType(instance, schema, schemaPointer, instancePointer, result))
        {
            ActiveFrames.RemoveAt(ActiveFrames.Count - 1);
            return;
        }

        if (instance != null)
        {
            JsonValueKind kind = instance.GetValueKind();
            if (kind == JsonValueKind.Number)
            {
                EvaluateNumericConstraints(
                    instance,
                    schema,
                    schemaPointer,
                    instancePointer,
                    result);
            }
            else if (kind == JsonValueKind.String)
            {
                EvaluateStringConstraints(
                    instance,
                    schema,
                    schemaPointer,
                    instancePointer,
                    result);
            }
            else if (kind == JsonValueKind.Array)
            {
                EvaluateArrayConstraints(
                    instance.AsArray(),
                    schema,
                    schemaName,
                    schemaPointer,
                    instancePointer,
                    result);
            }
            else if (kind == JsonValueKind.Object)
            {
                EvaluateObjectConstraints(
                    instance.AsObject(),
                    schema,
                    schemaName,
                    schemaPointer,
                    instancePointer,
                    result);
            }
        }

        ActiveFrames.RemoveAt(ActiveFrames.Count - 1);
    }

    internal void EvaluateReference(
        JsonNode? instance,
        string reference,
        string sourceSchemaName,
        string sourceSchemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        int fragmentIndex = reference.IndexOf('#');
        string documentPart = fragmentIndex < 0
            ? reference
            : reference.Substring(0, fragmentIndex);
        string fragment = fragmentIndex < 0
            ? ""
            : reference.Substring(fragmentIndex + 1);
        string targetName = documentPart.Length == 0
            ? sourceSchemaName
            : CoreSchemas.GetSchemaName(documentPart);
        if (!Schemas.ContainsKey(targetName))
        {
            AddFailure(
                result,
                "$ref",
                instancePointer,
                Pointers.AppendProperty(sourceSchemaPointer, "$ref"),
                $"Referenced schema '{reference}' is not registered.");
            return;
        }

        JsonNode? target = ResolveSchemaFragment(Schemas[targetName], fragment);
        if (target == null || target.GetValueKind() != JsonValueKind.Object)
        {
            AddFailure(
                result,
                "$ref",
                instancePointer,
                Pointers.AppendProperty(sourceSchemaPointer, "$ref"),
                $"Referenced schema location '{reference}' does not resolve to an object schema.");
            return;
        }

        string targetPointer = fragment.Length == 0
            ? ""
            : Uri.UnescapeDataString(fragment);
        EvaluateSchema(
            instance,
            target.AsObject(),
            targetName,
            targetPointer,
            instancePointer,
            result);
    }

    internal JsonNode? ResolveSchemaFragment(JsonObject root, string fragment)
    {
        if (fragment.Length == 0)
        {
            return root;
        }

        string pointer = Uri.UnescapeDataString(fragment);
        if (!pointer.StartsWith("/", StringComparison.Ordinal))
        {
            return null;
        }

        string[] segments = pointer.Substring(1).Split('/');
        JsonNode? current = root;
        int segmentIndex = 0;
        while (segmentIndex < segments.Length && current != null)
        {
            string segment = segments[segmentIndex].Replace("~1", "/").Replace("~0", "~");
            if (current.GetValueKind() == JsonValueKind.Object)
            {
                JsonObject currentObject = current.AsObject();
                if (!currentObject.ContainsKey(segment))
                {
                    return null;
                }

                current = currentObject[segment];
            }
            else if (current.GetValueKind() == JsonValueKind.Array)
            {
                int arrayIndex = ParseArrayIndex(segment);
                JsonArray currentArray = current.AsArray();
                if (arrayIndex < 0 || arrayIndex >= currentArray.Count)
                {
                    return null;
                }

                current = currentArray[arrayIndex];
            }
            else
            {
                return null;
            }

            segmentIndex++;
        }

        return current;
    }

    internal int ParseArrayIndex(string value)
    {
        if (value.Length == 0)
        {
            return -1;
        }

        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }

            if (result > (int.MaxValue - (character - '0')) / 10)
            {
                return -1;
            }

            result = result * 10 + character - '0';
            index++;
        }

        return result;
    }

    internal void EvaluateComposition(
        JsonNode? instance,
        JsonObject schema,
        string schemaName,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        if (schema.ContainsKey("allOf") && schema["allOf"] != null)
        {
            JsonArray allOf = ReadKeyword(schema, "allOf").AsArray();
            int index = 0;
            while (index < allOf.Count)
            {
                JsonNode? candidate = allOf[index];
                if (candidate != null && candidate.GetValueKind() == JsonValueKind.Object)
                {
                    EvaluateSchema(
                        instance,
                        candidate.AsObject(),
                        schemaName,
                        Pointers.AppendIndex(Pointers.AppendProperty(schemaPointer, "allOf"), index),
                        instancePointer,
                        result);
                }

                index++;
            }
        }

        if (schema.ContainsKey("anyOf") && schema["anyOf"] != null)
        {
            JsonArray anyOf = ReadKeyword(schema, "anyOf").AsArray();
            int validCount = CountValidCandidates(
                instance,
                anyOf,
                schemaName,
                Pointers.AppendProperty(schemaPointer, "anyOf"),
                instancePointer);
            if (validCount == 0)
            {
                AddFailure(
                    result,
                    "anyOf",
                    instancePointer,
                    Pointers.AppendProperty(schemaPointer, "anyOf"),
                    "The value does not satisfy any allowed schema.");
            }
        }

        if (schema.ContainsKey("oneOf") && schema["oneOf"] != null)
        {
            JsonArray oneOf = ReadKeyword(schema, "oneOf").AsArray();
            int validCount = CountValidCandidates(
                instance,
                oneOf,
                schemaName,
                Pointers.AppendProperty(schemaPointer, "oneOf"),
                instancePointer);
            if (validCount != 1)
            {
                AddFailure(
                    result,
                    "oneOf",
                    instancePointer,
                    Pointers.AppendProperty(schemaPointer, "oneOf"),
                    $"The value satisfies {validCount} schemas instead of exactly one.");
            }
        }

        if (schema.ContainsKey("not") && schema["not"] != null
            && ReadKeyword(schema, "not").GetValueKind() == JsonValueKind.Object)
        {
            Draft4SchemaValidation candidateResult = new Draft4SchemaValidation();
            EvaluateSchema(
                instance,
                ReadKeyword(schema, "not").AsObject(),
                schemaName,
                Pointers.AppendProperty(schemaPointer, "not"),
                instancePointer,
                candidateResult);
            if (candidateResult.IsValid)
            {
                AddFailure(
                    result,
                    "not",
                    instancePointer,
                    Pointers.AppendProperty(schemaPointer, "not"),
                    "The value satisfies a forbidden schema.");
            }
        }

        EvaluateConditional(
            instance,
            schema,
            schemaName,
            schemaPointer,
            instancePointer,
            result);
    }

    internal void EvaluateConditional(
        JsonNode? instance,
        JsonObject schema,
        string schemaName,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        if (!schema.ContainsKey("if") || schema["if"] == null
            || ReadKeyword(schema, "if").GetValueKind() != JsonValueKind.Object)
        {
            return;
        }

        Draft4SchemaValidation condition = new Draft4SchemaValidation();
        EvaluateSchema(
            instance,
            ReadKeyword(schema, "if").AsObject(),
            schemaName,
            Pointers.AppendProperty(schemaPointer, "if"),
            instancePointer,
            condition);
        string branchName = condition.IsValid ? "then" : "else";
        if (schema.ContainsKey(branchName) && schema[branchName] != null
            && ReadKeyword(schema, branchName).GetValueKind() == JsonValueKind.Object)
        {
            EvaluateSchema(
                instance,
                ReadKeyword(schema, branchName).AsObject(),
                schemaName,
                Pointers.AppendProperty(schemaPointer, branchName),
                instancePointer,
                result);
        }
    }

    internal int CountValidCandidates(
        JsonNode? instance,
        JsonArray candidates,
        string schemaName,
        string candidatesPointer,
        string instancePointer)
    {
        int validCount = 0;
        int index = 0;
        while (index < candidates.Count)
        {
            JsonNode? candidate = candidates[index];
            if (candidate != null && candidate.GetValueKind() == JsonValueKind.Object)
            {
                Draft4SchemaValidation candidateResult = new Draft4SchemaValidation();
                EvaluateSchema(
                    instance,
                    candidate.AsObject(),
                    schemaName,
                    Pointers.AppendIndex(candidatesPointer, index),
                    instancePointer,
                    candidateResult);
                if (candidateResult.IsValid)
                {
                    validCount++;
                }
            }

            index++;
        }

        return validCount;
    }

    internal void EvaluateEnumAndConstant(
        JsonNode? instance,
        JsonObject schema,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        if (schema.ContainsKey("enum") && schema["enum"] != null)
        {
            JsonArray values = ReadKeyword(schema, "enum").AsArray();
            bool found = false;
            int index = 0;
            while (index < values.Count)
            {
                if (JsonValuesEqual(instance, values[index]))
                {
                    found = true;
                    break;
                }

                index++;
            }

            if (!found)
            {
                AddFailure(
                    result,
                    "enum",
                    instancePointer,
                    Pointers.AppendProperty(schemaPointer, "enum"),
                    "The value is not one of the allowed enum values.");
            }
        }

        if (schema.ContainsKey("const")
            && !JsonValuesEqual(instance, schema["const"]))
        {
            AddFailure(
                result,
                "const",
                instancePointer,
                Pointers.AppendProperty(schemaPointer, "const"),
                "The value does not equal the required constant.");
        }
    }

    internal bool EvaluateType(
        JsonNode? instance,
        JsonObject schema,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        if (!schema.ContainsKey("type") || schema["type"] == null)
        {
            return true;
        }

        JsonNode typeNode = ReadKeyword(schema, "type");
        bool matches = false;
        if (typeNode.GetValueKind() == JsonValueKind.String)
        {
            matches = MatchesType(instance, typeNode.GetValue<string>());
        }
        else if (typeNode.GetValueKind() == JsonValueKind.Array)
        {
            JsonArray types = typeNode.AsArray();
            int index = 0;
            while (index < types.Count)
            {
                if (types[index] != null
                    && MatchesType(instance, ReadArrayItem(types, index).GetValue<string>()))
                {
                    matches = true;
                    break;
                }

                index++;
            }
        }

        if (!matches)
        {
            AddFailure(
                result,
                "type",
                instancePointer,
                Pointers.AppendProperty(schemaPointer, "type"),
                "The value has a type that is not allowed by the schema.");
        }

        return matches;
    }

    internal bool MatchesType(JsonNode? instance, string typeName)
    {
        if (instance == null)
        {
            return typeName == "null";
        }

        JsonValueKind kind = instance.GetValueKind();
        if (typeName == "object")
        {
            return kind == JsonValueKind.Object;
        }

        if (typeName == "array")
        {
            return kind == JsonValueKind.Array;
        }

        if (typeName == "string")
        {
            return kind == JsonValueKind.String;
        }

        if (typeName == "boolean")
        {
            return kind == JsonValueKind.True || kind == JsonValueKind.False;
        }

        if (typeName == "number")
        {
            return kind == JsonValueKind.Number;
        }

        if (typeName == "integer")
        {
            return kind == JsonValueKind.Number && IsIntegerNumber(instance);
        }

        return false;
    }

    internal void EvaluateNumericConstraints(
        JsonNode instance,
        JsonObject schema,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        double value = ReadJsonNumber(instance);
        if (schema.ContainsKey("multipleOf") && schema["multipleOf"] != null)
        {
            double multiple = ReadJsonNumber(ReadKeyword(schema, "multipleOf"));
            double quotient = value / multiple;
            double distance = Math.Abs(quotient - Math.Round(quotient));
            double tolerance = Math.Max(1.0, Math.Abs(quotient)) * 0.000000000001;
            if (multiple <= 0.0 || distance > tolerance)
            {
                AddFailure(
                    result,
                    "multipleOf",
                    instancePointer,
                    Pointers.AppendProperty(schemaPointer, "multipleOf"),
                    $"The number is not a multiple of {multiple}.");
            }
        }

        if (schema.ContainsKey("minimum") && schema["minimum"] != null)
        {
            double minimum = ReadJsonNumber(ReadKeyword(schema, "minimum"));
            bool exclusive = ReadBooleanKeyword(schema, "exclusiveMinimum");
            if ((!exclusive && value < minimum) || (exclusive && value <= minimum))
            {
                AddFailure(
                    result,
                    exclusive ? "exclusiveMinimum" : "minimum",
                    instancePointer,
                    Pointers.AppendProperty(
                        schemaPointer,
                        exclusive ? "exclusiveMinimum" : "minimum"),
                    $"The number is below its allowed minimum of {minimum}.");
            }
        }

        if (schema.ContainsKey("maximum") && schema["maximum"] != null)
        {
            double maximum = ReadJsonNumber(ReadKeyword(schema, "maximum"));
            bool exclusive = ReadBooleanKeyword(schema, "exclusiveMaximum");
            if ((!exclusive && value > maximum) || (exclusive && value >= maximum))
            {
                AddFailure(
                    result,
                    exclusive ? "exclusiveMaximum" : "maximum",
                    instancePointer,
                    Pointers.AppendProperty(
                        schemaPointer,
                        exclusive ? "exclusiveMaximum" : "maximum"),
                    $"The number is above its allowed maximum of {maximum}.");
            }
        }

        if (schema.ContainsKey("exclusiveMinimum") && schema["exclusiveMinimum"] != null
            && ReadKeyword(schema, "exclusiveMinimum").GetValueKind() == JsonValueKind.Number)
        {
            double minimum = ReadJsonNumber(ReadKeyword(schema, "exclusiveMinimum"));
            if (value <= minimum)
            {
                AddFailure(
                    result,
                    "exclusiveMinimum",
                    instancePointer,
                    Pointers.AppendProperty(schemaPointer, "exclusiveMinimum"),
                    $"The number must be greater than {minimum}.");
            }
        }

        if (schema.ContainsKey("exclusiveMaximum") && schema["exclusiveMaximum"] != null
            && ReadKeyword(schema, "exclusiveMaximum").GetValueKind() == JsonValueKind.Number)
        {
            double maximum = ReadJsonNumber(ReadKeyword(schema, "exclusiveMaximum"));
            if (value >= maximum)
            {
                AddFailure(
                    result,
                    "exclusiveMaximum",
                    instancePointer,
                    Pointers.AppendProperty(schemaPointer, "exclusiveMaximum"),
                    $"The number must be less than {maximum}.");
            }
        }
    }

    internal bool ReadBooleanKeyword(JsonObject schema, string keyword)
    {
        return schema.ContainsKey(keyword)
            && schema[keyword] != null
            && (ReadKeyword(schema, keyword).GetValueKind() == JsonValueKind.True
                || ReadKeyword(schema, keyword).GetValueKind() == JsonValueKind.False)
            && ReadKeyword(schema, keyword).GetValue<bool>();
    }

    internal void EvaluateStringConstraints(
        JsonNode instance,
        JsonObject schema,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        string value = instance.GetValue<string>();
        int length = CountUnicodeCharacters(value);
        if (schema.ContainsKey("minLength") && schema["minLength"] != null)
        {
            long minimum = ReadKeyword(schema, "minLength").GetValue<long>();
            if (length < minimum)
            {
                AddFailure(
                    result,
                    "minLength",
                    instancePointer,
                    Pointers.AppendProperty(schemaPointer, "minLength"),
                    $"The string length is below {minimum}.");
            }
        }

        if (schema.ContainsKey("maxLength") && schema["maxLength"] != null)
        {
            long maximum = ReadKeyword(schema, "maxLength").GetValue<long>();
            if (length > maximum)
            {
                AddFailure(
                    result,
                    "maxLength",
                    instancePointer,
                    Pointers.AppendProperty(schemaPointer, "maxLength"),
                    $"The string length exceeds {maximum}.");
            }
        }

        if (schema.ContainsKey("pattern") && schema["pattern"] != null)
        {
            string pattern = ReadKeyword(schema, "pattern").GetValue<string>();
            if (!Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant))
            {
                AddFailure(
                    result,
                    "pattern",
                    instancePointer,
                    Pointers.AppendProperty(schemaPointer, "pattern"),
                    $"The string does not match the required pattern '{pattern}'.");
            }
        }
    }

    internal int CountUnicodeCharacters(string value)
    {
        int count = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (char.IsHighSurrogate(value[index])
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                index += 2;
            }
            else
            {
                index++;
            }

            count++;
        }

        return count;
    }

    internal void EvaluateArrayConstraints(
        JsonArray instance,
        JsonObject schema,
        string schemaName,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        EvaluateArrayLengths(instance, schema, schemaPointer, instancePointer, result);
        EvaluateUniqueItems(instance, schema, schemaPointer, instancePointer, result);
        if (!schema.ContainsKey("items") || schema["items"] == null)
        {
            return;
        }

        JsonNode items = ReadKeyword(schema, "items");
        if (items.GetValueKind() == JsonValueKind.Object)
        {
            int itemIndex = 0;
            while (itemIndex < instance.Count)
            {
                EvaluateSchema(
                    instance[itemIndex],
                    items.AsObject(),
                    schemaName,
                    Pointers.AppendProperty(schemaPointer, "items"),
                    Pointers.AppendIndex(instancePointer, itemIndex),
                    result);
                itemIndex++;
            }
        }
        else if (items.GetValueKind() == JsonValueKind.Array)
        {
            EvaluateTupleItems(
                instance,
                items.AsArray(),
                schema,
                schemaName,
                schemaPointer,
                instancePointer,
                result);
        }
    }

    internal void EvaluateArrayLengths(
        JsonArray instance,
        JsonObject schema,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        if (schema.ContainsKey("minItems") && schema["minItems"] != null)
        {
            long minimum = ReadKeyword(schema, "minItems").GetValue<long>();
            if (instance.Count < minimum)
            {
                AddFailure(
                    result,
                    "minItems",
                    instancePointer,
                    Pointers.AppendProperty(schemaPointer, "minItems"),
                    $"The array contains fewer than {minimum} items.");
            }
        }

        if (schema.ContainsKey("maxItems") && schema["maxItems"] != null)
        {
            long maximum = ReadKeyword(schema, "maxItems").GetValue<long>();
            if (instance.Count > maximum)
            {
                AddFailure(
                    result,
                    "maxItems",
                    instancePointer,
                    Pointers.AppendProperty(schemaPointer, "maxItems"),
                    $"The array contains more than {maximum} items.");
            }
        }
    }

    internal void EvaluateUniqueItems(
        JsonArray instance,
        JsonObject schema,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        if (!ReadBooleanKeyword(schema, "uniqueItems"))
        {
            return;
        }

        int firstIndex = 0;
        while (firstIndex < instance.Count)
        {
            int secondIndex = firstIndex + 1;
            while (secondIndex < instance.Count)
            {
                if (JsonValuesEqual(instance[firstIndex], instance[secondIndex]))
                {
                    AddFailure(
                        result,
                        "uniqueItems",
                        Pointers.AppendIndex(instancePointer, secondIndex),
                        Pointers.AppendProperty(schemaPointer, "uniqueItems"),
                        $"Array item {secondIndex} duplicates item {firstIndex}.");
                }

                secondIndex++;
            }

            firstIndex++;
        }
    }

    internal void EvaluateTupleItems(
        JsonArray instance,
        JsonArray itemSchemas,
        JsonObject schema,
        string schemaName,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        int itemIndex = 0;
        while (itemIndex < instance.Count && itemIndex < itemSchemas.Count)
        {
            JsonNode? itemSchema = itemSchemas[itemIndex];
            if (itemSchema != null && itemSchema.GetValueKind() == JsonValueKind.Object)
            {
                EvaluateSchema(
                    instance[itemIndex],
                    itemSchema.AsObject(),
                    schemaName,
                    Pointers.AppendIndex(Pointers.AppendProperty(schemaPointer, "items"), itemIndex),
                    Pointers.AppendIndex(instancePointer, itemIndex),
                    result);
            }

            itemIndex++;
        }

        if (instance.Count <= itemSchemas.Count
            || !schema.ContainsKey("additionalItems")
            || schema["additionalItems"] == null)
        {
            return;
        }

        JsonNode additional = ReadKeyword(schema, "additionalItems");
        if ((additional.GetValueKind() == JsonValueKind.True
                || additional.GetValueKind() == JsonValueKind.False)
            && !additional.GetValue<bool>())
        {
            AddFailure(
                result,
                "additionalItems",
                Pointers.AppendIndex(instancePointer, itemSchemas.Count),
                Pointers.AppendProperty(schemaPointer, "additionalItems"),
                "The array contains an additional item that is not allowed.");
        }
        else if (additional.GetValueKind() == JsonValueKind.Object)
        {
            int additionalIndex = itemSchemas.Count;
            while (additionalIndex < instance.Count)
            {
                EvaluateSchema(
                    instance[additionalIndex],
                    additional.AsObject(),
                    schemaName,
                    Pointers.AppendProperty(schemaPointer, "additionalItems"),
                    Pointers.AppendIndex(instancePointer, additionalIndex),
                    result);
                additionalIndex++;
            }
        }
    }

    internal void EvaluateObjectConstraints(
        JsonObject instance,
        JsonObject schema,
        string schemaName,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        EvaluateObjectLengths(instance, schema, schemaPointer, instancePointer, result);
        EvaluateRequiredProperties(instance, schema, schemaPointer, instancePointer, result);
        List<string> matchedProperties = new List<string>();
        EvaluateNamedProperties(
            instance,
            schema,
            schemaName,
            schemaPointer,
            instancePointer,
            matchedProperties,
            result);
        EvaluatePatternProperties(
            instance,
            schema,
            schemaName,
            schemaPointer,
            instancePointer,
            matchedProperties,
            result);
        EvaluateAdditionalProperties(
            instance,
            schema,
            schemaName,
            schemaPointer,
            instancePointer,
            matchedProperties,
            result);
        EvaluatePropertyDependencies(
            instance,
            schema,
            schemaName,
            schemaPointer,
            instancePointer,
            result);
        EvaluateDependentRequired(
            instance,
            schema,
            schemaPointer,
            instancePointer,
            result);
        EvaluatePropertyNames(
            instance,
            schema,
            schemaName,
            schemaPointer,
            instancePointer,
            result);
    }

    internal void EvaluateObjectLengths(
        JsonObject instance,
        JsonObject schema,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        if (schema.ContainsKey("minProperties") && schema["minProperties"] != null)
        {
            long minimum = ReadKeyword(schema, "minProperties").GetValue<long>();
            if (instance.Count < minimum)
            {
                AddFailure(
                    result,
                    "minProperties",
                    instancePointer,
                    Pointers.AppendProperty(schemaPointer, "minProperties"),
                    $"The object contains fewer than {minimum} properties.");
            }
        }

        if (schema.ContainsKey("maxProperties") && schema["maxProperties"] != null)
        {
            long maximum = ReadKeyword(schema, "maxProperties").GetValue<long>();
            if (instance.Count > maximum)
            {
                AddFailure(
                    result,
                    "maxProperties",
                    instancePointer,
                    Pointers.AppendProperty(schemaPointer, "maxProperties"),
                    $"The object contains more than {maximum} properties.");
            }
        }
    }

    internal void EvaluateRequiredProperties(
        JsonObject instance,
        JsonObject schema,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        if (!schema.ContainsKey("required") || schema["required"] == null)
        {
            return;
        }

        JsonArray required = ReadKeyword(schema, "required").AsArray();
        int index = 0;
        while (index < required.Count)
        {
            string name = ReadArrayItem(required, index).GetValue<string>();
            if (!instance.ContainsKey(name))
            {
                AddFailure(
                    result,
                    "required",
                    Pointers.AppendProperty(instancePointer, name),
                    Pointers.AppendIndex(Pointers.AppendProperty(schemaPointer, "required"), index),
                    $"Required property '{name}' is missing.");
            }

            index++;
        }
    }

    internal void EvaluateNamedProperties(
        JsonObject instance,
        JsonObject schema,
        string schemaName,
        string schemaPointer,
        string instancePointer,
        List<string> matchedProperties,
        Draft4SchemaValidation result)
    {
        if (!schema.ContainsKey("properties") || schema["properties"] == null)
        {
            return;
        }

        JsonObject properties = ReadKeyword(schema, "properties").AsObject();
        foreach (KeyValuePair<string, JsonNode?> property in properties)
        {
            if (instance.ContainsKey(property.Key))
            {
                matchedProperties.Add(property.Key);
                if (property.Value != null
                    && property.Value.GetValueKind() == JsonValueKind.Object)
                {
                    EvaluateSchema(
                        instance[property.Key],
                        property.Value.AsObject(),
                        schemaName,
                        Pointers.AppendProperty(
                            Pointers.AppendProperty(schemaPointer, "properties"),
                            property.Key),
                        Pointers.AppendProperty(instancePointer, property.Key),
                        result);
                }
            }
        }
    }

    internal void EvaluatePatternProperties(
        JsonObject instance,
        JsonObject schema,
        string schemaName,
        string schemaPointer,
        string instancePointer,
        List<string> matchedProperties,
        Draft4SchemaValidation result)
    {
        if (!schema.ContainsKey("patternProperties")
            || schema["patternProperties"] == null)
        {
            return;
        }

        JsonObject patterns = ReadKeyword(schema, "patternProperties").AsObject();
        foreach (KeyValuePair<string, JsonNode?> member in instance)
        {
            foreach (KeyValuePair<string, JsonNode?> pattern in patterns)
            {
                if (Regex.IsMatch(member.Key, pattern.Key, RegexOptions.CultureInvariant))
                {
                    if (!matchedProperties.Contains(member.Key))
                    {
                        matchedProperties.Add(member.Key);
                    }

                    if (pattern.Value != null
                        && pattern.Value.GetValueKind() == JsonValueKind.Object)
                    {
                        EvaluateSchema(
                            member.Value,
                            pattern.Value.AsObject(),
                            schemaName,
                            Pointers.AppendProperty(
                                Pointers.AppendProperty(schemaPointer, "patternProperties"),
                                pattern.Key),
                            Pointers.AppendProperty(instancePointer, member.Key),
                            result);
                    }
                }
            }
        }
    }

    internal void EvaluateAdditionalProperties(
        JsonObject instance,
        JsonObject schema,
        string schemaName,
        string schemaPointer,
        string instancePointer,
        List<string> matchedProperties,
        Draft4SchemaValidation result)
    {
        if (!schema.ContainsKey("additionalProperties")
            || schema["additionalProperties"] == null)
        {
            return;
        }

        JsonNode additional = ReadKeyword(schema, "additionalProperties");
        foreach (KeyValuePair<string, JsonNode?> member in instance)
        {
            if (matchedProperties.Contains(member.Key))
            {
                continue;
            }

            if ((additional.GetValueKind() == JsonValueKind.True
                    || additional.GetValueKind() == JsonValueKind.False)
                && !additional.GetValue<bool>())
            {
                AddFailure(
                    result,
                    "additionalProperties",
                    Pointers.AppendProperty(instancePointer, member.Key),
                    Pointers.AppendProperty(schemaPointer, "additionalProperties"),
                    $"Property '{member.Key}' is not allowed.");
            }
            else if (additional.GetValueKind() == JsonValueKind.Object)
            {
                EvaluateSchema(
                    member.Value,
                    additional.AsObject(),
                    schemaName,
                    Pointers.AppendProperty(schemaPointer, "additionalProperties"),
                    Pointers.AppendProperty(instancePointer, member.Key),
                    result);
            }
        }
    }

    internal void EvaluatePropertyDependencies(
        JsonObject instance,
        JsonObject schema,
        string schemaName,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        if (!schema.ContainsKey("dependencies") || schema["dependencies"] == null)
        {
            return;
        }

        JsonObject dependencies = ReadKeyword(schema, "dependencies").AsObject();
        foreach (KeyValuePair<string, JsonNode?> dependency in dependencies)
        {
            if (!instance.ContainsKey(dependency.Key) || dependency.Value == null)
            {
                continue;
            }

            string dependencyPointer = Pointers.AppendProperty(
                Pointers.AppendProperty(schemaPointer, "dependencies"),
                dependency.Key);
            if (dependency.Value.GetValueKind() == JsonValueKind.Array)
            {
                JsonArray names = dependency.Value.AsArray();
                int index = 0;
                while (index < names.Count)
                {
                    string requiredName = ReadArrayItem(names, index).GetValue<string>();
                    if (!instance.ContainsKey(requiredName))
                    {
                        AddFailure(
                            result,
                            "dependencies",
                            Pointers.AppendProperty(instancePointer, requiredName),
                            Pointers.AppendIndex(dependencyPointer, index),
                            $"Property '{dependency.Key}' requires property '{requiredName}'.");
                    }

                    index++;
                }
            }
            else if (dependency.Value.GetValueKind() == JsonValueKind.Object)
            {
                EvaluateSchema(
                    instance,
                    dependency.Value.AsObject(),
                    schemaName,
                    dependencyPointer,
                    instancePointer,
                    result);
            }
        }
    }

    internal void EvaluatePropertyNames(
        JsonObject instance,
        JsonObject schema,
        string schemaName,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        if (!schema.ContainsKey("propertyNames") || schema["propertyNames"] == null
            || ReadKeyword(schema, "propertyNames").GetValueKind() != JsonValueKind.Object)
        {
            return;
        }

        foreach (KeyValuePair<string, JsonNode?> member in instance)
        {
            EvaluateSchema(
                JsonValue.Create(member.Key),
                ReadKeyword(schema, "propertyNames").AsObject(),
                schemaName,
                Pointers.AppendProperty(schemaPointer, "propertyNames"),
                Pointers.AppendProperty(instancePointer, member.Key),
                result);
        }
    }

    internal void EvaluateDependentRequired(
        JsonObject instance,
        JsonObject schema,
        string schemaPointer,
        string instancePointer,
        Draft4SchemaValidation result)
    {
        if (!schema.ContainsKey("dependentRequired")
            || schema["dependentRequired"] == null)
        {
            return;
        }

        JsonObject dependencies = ReadKeyword(schema, "dependentRequired").AsObject();
        foreach (KeyValuePair<string, JsonNode?> dependency in dependencies)
        {
            if (!instance.ContainsKey(dependency.Key) || dependency.Value == null)
            {
                continue;
            }

            JsonArray names = dependency.Value.AsArray();
            int index = 0;
            while (index < names.Count)
            {
                string requiredName = ReadArrayItem(names, index).GetValue<string>();
                if (!instance.ContainsKey(requiredName))
                {
                    AddFailure(
                        result,
                        "dependentRequired",
                        Pointers.AppendProperty(instancePointer, requiredName),
                        Pointers.AppendIndex(
                            Pointers.AppendProperty(
                                Pointers.AppendProperty(schemaPointer, "dependentRequired"),
                                dependency.Key),
                            index),
                        $"Property '{dependency.Key}' requires property '{requiredName}'.");
                }

                index++;
            }
        }
    }

    internal bool JsonValuesEqual(JsonNode? first, JsonNode? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
        }

        JsonValueKind firstKind = first.GetValueKind();
        JsonValueKind secondKind = second.GetValueKind();
        if (firstKind == JsonValueKind.Number && secondKind == JsonValueKind.Number)
        {
            return ReadJsonNumber(first) == ReadJsonNumber(second);
        }

        if (firstKind != secondKind)
        {
            return false;
        }

        if (firstKind == JsonValueKind.String)
        {
            return first.GetValue<string>() == second.GetValue<string>();
        }

        if (firstKind == JsonValueKind.True || firstKind == JsonValueKind.False)
        {
            return first.GetValue<bool>() == second.GetValue<bool>();
        }

        if (firstKind == JsonValueKind.Array)
        {
            JsonArray firstArray = first.AsArray();
            JsonArray secondArray = second.AsArray();
            if (firstArray.Count != secondArray.Count)
            {
                return false;
            }

            int index = 0;
            while (index < firstArray.Count)
            {
                if (!JsonValuesEqual(firstArray[index], secondArray[index]))
                {
                    return false;
                }

                index++;
            }

            return true;
        }

        if (firstKind == JsonValueKind.Object)
        {
            JsonObject firstObject = first.AsObject();
            JsonObject secondObject = second.AsObject();
            if (firstObject.Count != secondObject.Count)
            {
                return false;
            }

            foreach (KeyValuePair<string, JsonNode?> member in firstObject)
            {
                if (!secondObject.ContainsKey(member.Key)
                    || !JsonValuesEqual(member.Value, secondObject[member.Key]))
                {
                    return false;
                }
            }

            return true;
        }

        return true;
    }

    internal bool IsIntegerNumber(JsonNode value)
    {
        double number = ReadJsonNumber(value);
        return double.IsFinite(number) && Math.Floor(number) == number;
    }

    internal double ReadJsonNumber(JsonNode value)
    {
        return double.Parse(
            value.ToJsonString(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture);
    }

    internal JsonNode ReadKeyword(JsonObject schema, string keyword)
    {
        JsonNode? value = schema[keyword];
        if (value == null)
        {
            throw new InvalidOperationException(
                $"Schema keyword '{keyword}' does not contain a value.");
        }

        return value;
    }

    internal JsonNode ReadArrayItem(JsonArray values, int index)
    {
        JsonNode? value = values[index];
        if (value == null)
        {
            throw new InvalidOperationException(
                $"Schema array item {index} does not contain a value.");
        }

        return value;
    }

    internal void AddFailure(
        Draft4SchemaValidation result,
        string keyword,
        string instancePointer,
        string schemaPointer,
        string message)
    {
        Draft4SchemaFailure failure = new Draft4SchemaFailure();
        failure.Keyword = keyword;
        failure.InstancePointer = instancePointer;
        failure.SchemaPointer = schemaPointer;
        failure.Message = message;
        result.Failures.Add(failure);
        result.IsValid = false;
    }
}
