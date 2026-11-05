using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class StructuralValidator
{
    internal Draft4SchemaValidator Schemas = new Draft4SchemaValidator();
    internal GltfJsonWriter Writer = new GltfJsonWriter();

    internal GltfValidationReport ValidateParsedStructure(GltfJsonReadResult json)
    {
        if (json == null)
        {
            throw new ArgumentNullException("json");
        }

        Draft4SchemaValidation validation = Schemas.ValidateJson(
            json.Root,
            "glTF.schema.json");
        return CreateStructuralReport(validation, json);
    }

    internal GltfValidationReport ValidateModelStructure(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        JsonObject projected = ProjectDocumentPresence(document);
        Draft4SchemaValidation validation = Schemas.ValidateJson(
            projected,
            "glTF.schema.json");
        return CreateStructuralReport(validation, null);
    }

    internal JsonObject ProjectDocumentPresence(GltfDocument document)
    {
        GltfWriteOptions options = new GltfWriteOptions();
        options.Validate = false;
        options.PreserveExplicitDefaults = true;
        options.PreserveUnknownMembers = true;
        return Writer.WriteDocument(document, options);
    }

    internal GltfValidationReport CreateStructuralReport(
        Draft4SchemaValidation validation,
        GltfJsonReadResult? json)
    {
        GltfValidationReport report = new GltfValidationReport();
        int index = 0;
        while (index < validation.Failures.Count)
        {
            Draft4SchemaFailure failure = validation.Failures[index];
            GltfValidationIssue issue = new GltfValidationIssue();
            issue.Code = GetStructuralIssueCode(failure.Keyword);
            issue.Severity = GltfValidationSeverity.Error;
            issue.Message = failure.Message;
            issue.JsonPointer = failure.InstancePointer;
            if (json != null)
            {
                int byteOffset = FindNearestByteOffset(json, failure.InstancePointer);
                if (byteOffset >= 0)
                {
                    issue.HasByteOffset = true;
                    issue.ByteOffset = byteOffset;
                }
            }

            report.Issues.Add(issue);
            index++;
        }

        report.IsValid = validation.IsValid;
        return report;
    }

    internal string GetStructuralIssueCode(string keyword)
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

    internal int FindNearestByteOffset(GltfJsonReadResult json, string pointer)
    {
        string candidate = pointer;
        while (true)
        {
            int tokenIndex = 0;
            while (tokenIndex < json.Tokens.Count)
            {
                GltfJsonToken token = json.Tokens[tokenIndex];
                if (token.JsonPointer == candidate)
                {
                    return token.ByteOffset;
                }

                tokenIndex++;
            }

            if (candidate.Length == 0)
            {
                return -1;
            }

            int separatorIndex = candidate.LastIndexOf('/');
            candidate = separatorIndex <= 0
                ? ""
                : candidate.Substring(0, separatorIndex);
        }
    }
}
