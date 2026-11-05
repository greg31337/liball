using System;
using System.Collections.Generic;

namespace gltf;

internal class ExtensionSchemaValidator
{
    internal ExtensionSchemaCatalog Catalog = new ExtensionSchemaCatalog();
    internal ExtensionFallback Fallback = new ExtensionFallback();

    internal GltfExtensionResult ApplySchemaValidation(
        GltfExtensionContext context,
        GltfExtensionResult result,
        ExtensionRegistry registry)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (result == null)
        {
            throw new ArgumentNullException("result");
        }

        if (registry == null)
        {
            throw new ArgumentNullException("registry");
        }

        GltfExtensionDescriptor? descriptor = registry.FindDescriptor(context.Name);
        if (descriptor == null || descriptor.SchemaResources.Count == 0)
        {
            return result;
        }

        if (context.Json == null && context.TypedData != null)
        {
            return result;
        }

        string? entrySchema = FindEntrySchemaName(descriptor, context.ObjectLocation);
        if (entrySchema == null)
        {
            return result;
        }

        Draft4SchemaValidator validator = CreateFamilyValidator(descriptor);
        Draft4SchemaValidation validation = validator.ValidateJson(context.Json, entrySchema);
        AppendFailures(context, result, validation);
        return result;
    }

    internal Draft4SchemaValidator CreateFamilyValidator(GltfExtensionDescriptor descriptor)
    {
        Draft4SchemaValidator result = new Draft4SchemaValidator();
        int index = 0;
        while (index < descriptor.SchemaResources.Count)
        {
            string name = descriptor.SchemaResources[index];
            result.RegisterSchemaDocument(
                name,
                Catalog.GetExtensionSchema(descriptor.Name, name));
            index++;
        }

        return result;
    }

    internal string? FindEntrySchemaName(
        GltfExtensionDescriptor descriptor,
        string objectLocation)
    {
        string locationName = objectLocation + "." + descriptor.Name + ".schema.json";
        string? exact = FindSchemaName(descriptor.SchemaResources, locationName);
        if (exact != null)
        {
            return exact;
        }

        int separatorIndex = objectLocation.LastIndexOf('.');
        if (separatorIndex >= 0)
        {
            string leafName = objectLocation.Substring(separatorIndex + 1)
                + "." + descriptor.Name + ".schema.json";
            exact = FindSchemaName(descriptor.SchemaResources, leafName);
            if (exact != null)
            {
                return exact;
            }
        }

        string genericName = descriptor.Name + ".schema.json";
        exact = FindSchemaName(descriptor.SchemaResources, genericName);
        if (exact != null)
        {
            return exact;
        }

        string suffix = "." + descriptor.Name + ".schema.json";
        string? suffixMatch = null;
        int suffixCount = 0;
        int index = 0;
        while (index < descriptor.SchemaResources.Count)
        {
            string name = descriptor.SchemaResources[index];
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                suffixMatch = name;
                suffixCount++;
            }

            index++;
        }

        if (suffixCount == 1)
        {
            return suffixMatch;
        }

        if (descriptor.SchemaResources.Count == 1)
        {
            return descriptor.SchemaResources[0];
        }

        return null;
    }

    internal string? FindSchemaName(List<string> names, string candidate)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (string.Equals(names[index], candidate, StringComparison.OrdinalIgnoreCase))
            {
                return names[index];
            }

            index++;
        }

        return null;
    }

    internal void AppendFailures(
        GltfExtensionContext context,
        GltfExtensionResult result,
        Draft4SchemaValidation validation)
    {
        string extensionPointer = Fallback.CreateExtensionPointer(context);
        int index = 0;
        while (index < validation.Failures.Count)
        {
            Draft4SchemaFailure failure = validation.Failures[index];
            GltfValidationIssue issue = new GltfValidationIssue();
            issue.Code = GetIssueCode(failure.Keyword);
            issue.Severity = GltfValidationSeverity.Error;
            issue.Message = failure.Message;
            issue.JsonPointer = extensionPointer + failure.InstancePointer;
            result.Diagnostics.Add(issue);
            index++;
        }

        if (!validation.IsValid)
        {
            result.Status = GltfOperationStatus.Invalid;
        }
    }

    internal string GetIssueCode(string keyword)
    {
        if (keyword == "type")
        {
            return GltfIssueCodes.TypeMismatch;
        }

        if (keyword == "required")
        {
            return GltfIssueCodes.UndefinedProperty;
        }

        if (keyword == "dependencies" || keyword == "dependentRequired")
        {
            return GltfIssueCodes.UnsatisfiedDependency;
        }

        if (keyword == "enum" || keyword == "const")
        {
            return GltfIssueCodes.ValueNotInList;
        }

        if (keyword == "multipleOf")
        {
            return GltfIssueCodes.ValueMultipleOf;
        }

        if (keyword == "pattern")
        {
            return GltfIssueCodes.PatternMismatch;
        }

        if (keyword == "uniqueItems")
        {
            return GltfIssueCodes.DuplicateElements;
        }

        if (keyword == "minItems" || keyword == "maxItems"
            || keyword == "additionalItems")
        {
            return GltfIssueCodes.ArrayLengthNotInList;
        }

        if (keyword == "additionalProperties")
        {
            return GltfIssueCodes.UnexpectedProperty;
        }

        if (keyword == "anyOf" || keyword == "oneOf" || keyword == "not")
        {
            return GltfIssueCodes.OneOfMismatch;
        }

        if (keyword == "$ref")
        {
            return GltfIssueCodes.UnresolvedReference;
        }

        return GltfIssueCodes.ValueNotInRange;
    }
}
